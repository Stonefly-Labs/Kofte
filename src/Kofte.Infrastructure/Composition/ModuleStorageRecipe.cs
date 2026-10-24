using System.Data.Common;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace Kofte.Infrastructure.Composition;

public abstract class ModuleStorageRecipe
{
    public ModuleId Owner { get; }
    public string Provider { get; }
    public int ContractVersion { get; }
    public string TargetMigration { get; }

    private ModuleStorageRecipe(ModuleId owner, string provider, string target, int version)
        => (Owner, Provider, TargetMigration, ContractVersion) = (owner, provider, target, version);

    public static ModuleStorageRecipe Create<TContext>(ModuleId owner, string provider, string targetMigration,
        Func<DbContextOptions, ModuleStorageModelBoundary, TContext> factory, int contractVersion = 1)
        where TContext : AssignedDbContext
    {
        ArgumentNullException.ThrowIfNull(factory);
        return new TypedRecipe<TContext>(owner, provider, targetMigration, factory, contractVersion);
    }

    internal abstract Task<IReadOnlyList<string>> InspectAsync(ModuleStorageModelBoundary boundary,
        IReadOnlyList<string> applied, CancellationToken token);
    internal abstract Task VerifyPhysicalAsync(ModuleStorageBinding binding, CancellationToken token);
    internal abstract Task<StorageResult> MigrateAsync(ModuleStorageBinding binding,
        IStorageMaintenanceScope scope, CancellationToken token);

    private sealed class TypedRecipe<TContext>(ModuleId owner, string provider, string target,
        Func<DbContextOptions, ModuleStorageModelBoundary, TContext> factory, int version)
        : ModuleStorageRecipe(owner, provider, target, version) where TContext : AssignedDbContext
    {
        internal override async Task<IReadOnlyList<string>> InspectAsync(ModuleStorageModelBoundary boundary,
            IReadOnlyList<string> applied, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            await using DbConnection connection = Provider == "postgresql" ? new NpgsqlConnection() : new SqliteConnection();
            var options = new DbContextOptionsBuilder();
            if (Provider == "postgresql")
                options.UseNpgsql(connection, contextOwnsConnection: false, configure =>
                    configure.MigrationsHistoryTable("__EFMigrationsHistory", boundary.Schema));
            else
                options.UseSqlite(connection, contextOwnsConnection: false, configure =>
                    configure.MigrationsHistoryTable("__EFMigrationsHistory"));
            options.ReplaceService<IModelCacheKeyFactory, AssignedModelCacheKeyFactory>()
                .ReplaceService<IMigrationsAssembly, AssignedMigrationsAssembly>();
            await using var context = factory(options.Options, boundary);
            if (context.GetType().Assembly != typeof(TContext).Assembly ||
                !ReferenceEquals(context.Boundary, boundary) ||
                !ReferenceEquals(context.Database.GetDbConnection(), connection) ||
                context.GetService<IMigrationsAssembly>() is not AssignedMigrationsAssembly ||
                context.GetService<IModelCacheKeyFactory>() is not AssignedModelCacheKeyFactory)
                throw new StorageException(StorageCode.OwnerMismatch);
            MigrationBoundaryValidator.ValidateModel(context.GetService<IDesignTimeModel>().Model, boundary);
            var expected = MigrationBoundaryValidator.Preflight(context, applied, TargetMigration);
            if (applied.LastOrDefault() is { } checkpoint)
            {
                var assembly = context.GetService<IMigrationsAssembly>();
                MigrationBoundaryValidator.ValidateModel(
                    assembly.CreateMigration(assembly.Migrations[checkpoint], context.Database.ProviderName!).TargetModel,
                    boundary);
            }
            token.ThrowIfCancellationRequested();
            return expected;
        }

        internal override async Task VerifyPhysicalAsync(ModuleStorageBinding binding, CancellationToken token)
        {
            using var budget = new OperationBudget(OperationBudget.Resolve, token);
            await using var context = await AssignedContextFactory.CreateAsync(binding,
                (options, assigned) => factory(options, new ModuleStorageModelBoundary(assigned)), budget);
            var applied = (await context.Database.GetAppliedMigrationsAsync(budget.Token)).ToArray();
            var assembly = context.GetService<IMigrationsAssembly>();
            var checkpoint = applied.LastOrDefault();
            if (checkpoint is not null && !assembly.Migrations.ContainsKey(checkpoint))
                throw new StorageException(StorageCode.ReconciliationRequired);
            var model = checkpoint is null ? null :
                assembly.CreateMigration(assembly.Migrations[checkpoint], context.Database.ProviderName!).TargetModel;
            if (model is not null) MigrationBoundaryValidator.ValidateModel(model, binding);
            await AssignedStoreVerifier.VerifyAsync(context, model, budget, budget.Token);
        }

        internal override Task<StorageResult> MigrateAsync(ModuleStorageBinding binding,
            IStorageMaintenanceScope scope, CancellationToken token) =>
            binding.MigrateAsync((options, assigned) => factory(options, new ModuleStorageModelBoundary(assigned)),
                scope, TargetMigration, token);
    }
}
