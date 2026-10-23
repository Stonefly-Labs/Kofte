using System.Globalization;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kofte.Infrastructure.Storage.Migrations;

public static class ModuleMigrationRecovery
{
    public static Task<StorageResult> RecoverAsync<TContext>(StorageRegistry registry, ModuleId owner,
        Func<DbContextOptions, ModuleStorageBinding, TContext> factory, IStorageMaintenanceScope scope,
        CancellationToken cancellationToken = default) where TContext : AssignedDbContext =>
        StorageDiagnostics.RunAsync(StorageOperation.Recover, registry.Installation.Provider, owner,
            OperationBudget.Migration, cancellationToken, async (budget, mutation, correlation) =>
            {
                if (scope is not MaintenanceScope owned) throw new StorageException(StorageCode.RequestInvalid);
                await owned.VerifyAsync(registry.Installation, budget);
                var (metadata, record) = await registry.ReadBindingAsync(owner, budget, migrating: true);
                if (record.MigrationState is not ("in_progress" or "recovery_required") || record.Target is null)
                    throw new StorageException(StorageCode.RequestInvalid);
                await using var binding = new ModuleStorageBinding(registry, record.Assignment(metadata));
                binding.MigrationScope = owned;
                string? appliedCheckpoint;
                await using (var context = await AssignedContextFactory.CreateAsync(binding, factory, budget,
                    allowMigration: true))
                {
                    context.IsMigrationContext = true;
                    var applied = (await context.Database.GetAppliedMigrationsAsync(budget.Token)).ToArray();
                    var assembly = context.GetService<IMigrationsAssembly>();
                    var known = assembly.Migrations.Keys.ToArray();
                    var beforeCount = record.Checkpoint is null ? 0 : Array.IndexOf(known, record.Checkpoint) + 1;
                    var targetCount = Array.IndexOf(known, record.Target) + 1;
                    if (record.Checkpoint is not null && beforeCount == 0 || targetCount == 0 ||
                        applied.Length < beforeCount || applied.Length > targetCount ||
                        !applied.SequenceEqual(known.Take(applied.Length), StringComparer.Ordinal))
                        throw new StorageException(StorageCode.ReconciliationRequired);
                    MigrationBoundaryValidator.Preflight(context, applied, record.Target);
                    foreach (var id in applied)
                    {
                        var migration = assembly.CreateMigration(assembly.Migrations[id], context.Database.ProviderName!);
                        MigrationBoundaryValidator.ValidateModel(migration.TargetModel, binding);
                        foreach (var operation in migration.UpOperations)
                            MigrationBoundaryValidator.ValidateOperation(operation, binding);
                    }
                    appliedCheckpoint = applied.LastOrDefault();
                    var model = appliedCheckpoint is null ? null :
                        assembly.CreateMigration(assembly.Migrations[appliedCheckpoint], context.Database.ProviderName!).TargetModel;
                    await AssignedStoreVerifier.VerifyAsync(context, model, budget, budget.Token);
                    await owned.VerifyAsync(registry.Installation, budget);
                    if (binding.Provider == "sqlite") await ClearVerifiedLockAsync(context, budget, mutation);
                }
                mutation.PossibleCommit();
                binding.Refresh(await registry.SetMigrationStateAsync(binding.Assignment, owned,
                    appliedCheckpoint is null ? "none" : "current", appliedCheckpoint, appliedCheckpoint, budget));
                mutation.Committed();
                binding.EndMigration();
                if (appliedCheckpoint == record.Target)
                    return new StorageResult.Ready(StorageOperation.Recover, binding.Assignment, correlation,
                        mutation.Durability, StorageDiagnostics.Diagnostic(StorageCode.Ready));
                return await ModuleMigrationRunner.ExecuteAsync(binding, factory, owned, record.Target,
                    budget, mutation, correlation);
            });

    private static async Task ClearVerifiedLockAsync(AssignedDbContext context, OperationBudget budget,
        MutationEvidence mutation)
    {
        var connection = context.Database.GetDbConnection();
        await using var transaction = await connection.BeginTransactionAsync(budget.Token);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = budget.CommandSeconds(sqlite: true);
        command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsLock'";
        if (await command.ExecuteScalarAsync(budget.Token) is not 1L) return;
        await AssignedStoreVerifier.VerifySqliteDefinitionAsync(connection, "__EFMigrationsLock", budget, budget.Token,
            transaction);
        command.CommandText = "PRAGMA table_info('__EFMigrationsLock')";
        await using (var columns = await command.ExecuteReaderAsync(budget.Token))
        {
            if (!await columns.ReadAsync(budget.Token) || columns.GetString(1) != "Id" ||
                columns.GetString(2) != "INTEGER" || columns.GetInt32(3) != 1 ||
                !columns.IsDBNull(4) || columns.GetInt32(5) != 1 ||
                !await columns.ReadAsync(budget.Token) || columns.GetString(1) != "Timestamp" ||
                columns.GetString(2) != "TEXT" || columns.GetInt32(3) != 1 ||
                !columns.IsDBNull(4) || columns.GetInt32(5) != 0 ||
                await columns.ReadAsync(budget.Token))
                throw new StorageException(StorageCode.ReconciliationRequired);
        }
        command.CommandText = "SELECT Id,Timestamp FROM __EFMigrationsLock";
        string timestamp;
        await using (var rows = await command.ExecuteReaderAsync(budget.Token))
        {
            if (!await rows.ReadAsync(budget.Token)) return;
            if (rows.GetInt32(0) != 1 ||
                !DateTimeOffset.TryParse(rows.GetString(1), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out _))
                throw new StorageException(StorageCode.ReconciliationRequired);
            timestamp = rows.GetString(1);
            if (await rows.ReadAsync(budget.Token)) throw new StorageException(StorageCode.ReconciliationRequired);
        }
        budget.Check();
        command.CommandText = "DELETE FROM __EFMigrationsLock WHERE Id=1 AND Timestamp=@timestamp";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "timestamp";
        parameter.Value = timestamp;
        command.Parameters.Add(parameter);
        mutation.PossibleCommit();
        if (await command.ExecuteNonQueryAsync(budget.Token) != 1)
            throw new StorageException(StorageCode.ReconciliationRequired);
        await transaction.CommitAsync(budget.Token);
        mutation.Committed();
        StorageDiagnostics.Checkpoint("abandoned_lock_cleared", context.Storage.Provider, context.Storage.Owner);
    }
}
