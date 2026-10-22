using System.Diagnostics;
using System.Text.Json;
using Kofte.Platform.Modules.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Kofte.Infrastructure.Storage.Migrations;

internal static class ModuleMigrationRunner
{
    internal static Task<StorageResult> MigrateAsync<TContext>(ModuleStorageBinding binding,
        Func<DbContextOptions, ModuleStorageBinding, TContext> factory, IStorageMaintenanceScope scope,
        string? targetMigration, CancellationToken token) where TContext : AssignedDbContext =>
        StorageDiagnostics.RunAsync(StorageOperation.Migrate, binding.Provider, binding.Owner,
            OperationBudget.Migration, token, async (budget, mutation, correlation) =>
            {
                if (scope is not MaintenanceScope owned)
                    throw new StorageException(StorageCode.RequestInvalid);
                if (!binding.TryBeginMigration()) throw new StorageException(StorageCode.Busy);
                try
                {
                    await owned.VerifyAsync(binding.Registry.Installation, budget);
                    using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(budget.Token, scope.Lifetime);
                    await binding.VerifyAsync(lifetime.Token);
                    await using var context = await binding.CreateContextAsync(factory, lifetime.Token);
                    var before = (await context.Database.GetAppliedMigrationsAsync(lifetime.Token)).ToArray();
                    var assembly = context.GetService<IMigrationsAssembly>();
                    var known = assembly.Migrations.Keys.ToArray();
                    var (_, registration) = await binding.Registry.ReadBindingAsync(binding.Owner, budget);
                    if (registration.Checkpoint != before.LastOrDefault() ||
                        registration.MigrationState == "none" && before.Length != 0)
                        throw new StorageException(StorageCode.ReconciliationRequired);
                    var checkpointModel = before.LastOrDefault() is { } checkpoint &&
                        assembly.Migrations.TryGetValue(checkpoint, out var checkpointType)
                        ? assembly.CreateMigration(checkpointType, context.Database.ProviderName!).TargetModel : null;
                    await AssignedStoreVerifier.VerifyAsync(context, checkpointModel, lifetime.Token);
                    var target = targetMigration is null ? known.LastOrDefault() :
                        context.GetService<IMigrationsAssembly>().FindMigrationId(targetMigration);
                    if (target is null || target.Length > 150 || target.Any(c => c is < ' ' or > '~'))
                        throw new StorageException(StorageCode.RequestInvalid);
                    var intent = false;
                    try
                    {
                        mutation.PossibleCommit();
                        binding.MigrationScope = owned;
                        binding.Refresh(await binding.Registry.SetMigrationStateAsync(binding.Assignment, owned,
                            "in_progress", target, before.LastOrDefault(), budget));
                        context.IsMigrationContext = true;
                        intent = true;
                        mutation.Committed();
                        var expected = MigrationBoundaryValidator.Preflight(context, before, target);
                        await owned.VerifyAsync(binding.Registry.Installation, budget);
                        if (binding.Provider == "sqlite")
                            await RefuseAbandonedLockAsync(context, lifetime.Token);
                        await context.GetService<IMigrator>().MigrateAsync(target, lifetime.Token);
                        await owned.VerifyAsync(binding.Registry.Installation, budget);
                        var applied = (await context.Database.GetAppliedMigrationsAsync(lifetime.Token)).ToArray();
                        if (!applied.SequenceEqual(expected, StringComparer.Ordinal))
                            throw new StorageException(StorageCode.ReconciliationRequired);
                        MigrationBoundaryValidator.ValidateModel(context.GetService<IDesignTimeModel>().Model, binding);
                        var targetModel = assembly.CreateMigration(assembly.Migrations[target],
                            context.Database.ProviderName!).TargetModel;
                        await AssignedStoreVerifier.VerifyAsync(context, targetModel, lifetime.Token);
                        await binding.VerifyAsync(lifetime.Token, allowMigration: true);
                        binding.Refresh(await binding.Registry.SetMigrationStateAsync(binding.Assignment, owned,
                            "current", target, applied.LastOrDefault(), budget));
                        context.IsMigrationContext = false;
                        return new StorageResult.Ready(StorageOperation.Migrate, binding.Assignment, correlation,
                            mutation.Durability, StorageDiagnostics.Diagnostic(StorageCode.Ready));
                    }
                    catch
                    {
                        if (intent)
                        {
                            try
                            {
                                budget.Check();
                                binding.Refresh(await binding.Registry.SetMigrationStateAsync(binding.Assignment, owned,
                                    "recovery_required", target, before.LastOrDefault(), budget));
                            }
                            catch
                            {
                                WriteIncompleteReconciliation(binding, correlation);
                            }
                        }
                        context.IsMigrationContext = false;
                        throw;
                    }
                }
                finally { binding.EndMigration(); }
            });

    private static void WriteIncompleteReconciliation(ModuleStorageBinding binding, string correlation)
    {
        Activity.Current?.AddEvent(new ActivityEvent("storage_migration_reconciliation_incomplete",
            tags: new ActivityTagsCollection
            {
                ["storage.code"] = "storage_reconciliation_required",
                ["storage.guidance"] = "inspect_preserved_target"
            }));
        Console.Error.WriteLine(JsonSerializer.Serialize(new
        {
            eventCode = "storage_reconciliation_required",
            component = "storage",
            operation = "migrate",
            provider = binding.Provider,
            outcome = "reconciliationrequired",
            moduleId = binding.Owner.Value,
            correlationId = correlation
        }));
    }

    private static async Task RefuseAbandonedLockAsync(AssignedDbContext context, CancellationToken token)
    {
        var connection = context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 1;
        command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsLock'";
        if (await command.ExecuteScalarAsync(token) is not 1L) return;
        command.CommandText = "SELECT count(*) FROM __EFMigrationsLock";
        if (await command.ExecuteScalarAsync(token) is not 0L)
            throw new StorageException(StorageCode.ReconciliationRequired);
    }
}
