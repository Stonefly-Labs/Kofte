using System.Data.Common;
using Kofte.Infrastructure.Storage.Allocation;
using Kofte.Infrastructure.Storage.Postgres;
using Kofte.Infrastructure.Storage.Sqlite;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;

namespace Kofte.Infrastructure.Storage.Registry;

public sealed partial class StorageRegistry : IStorageRegistry
{
    internal StorageInstallation Installation { get; }
    private readonly RegistryMigrationRunner database;
    private readonly Func<ModuleId, string> allocator;

    public StorageRegistry(StorageInstallation installation) : this(installation, StorageAllocator.Allocate) { }

    internal StorageRegistry(StorageInstallation installation, Func<ModuleId, string> allocator)
    {
        Installation = installation;
        database = new(installation);
        this.allocator = allocator;
    }

    public Task<StorageResult> RegisterAsync(ModuleDescription module, IStorageMaintenanceScope scope,
        CancellationToken cancellationToken = default) =>
        StorageDiagnostics.RunAsync(StorageOperation.Register, Installation.Provider, module.Id,
            OperationBudget.Register, cancellationToken, async (budget, mutation, correlation) =>
            {
                if (scope is not MaintenanceScope owned) throw new StorageException(StorageCode.RequestInvalid);
                await owned.VerifyAsync(Installation, budget);
                await using var connection = await Installation.OpenPlatformAsync(budget);
                var metadata = await database.ReadInstallationAsync(connection, budget);
                var record = await database.FindAsync(connection, module.Id, budget);
                if (record is null)
                {
                    var key = module.Storage.Relational ? allocator(module.Id) : null;
                    if (key is not null) StorageAllocator.Validate(key);
                    record = new(module.Id, module.Storage.Relational, Installation.Provider, key,
                        key is null ? null : 1, key is null ? null : Guid.NewGuid(),
                        key is null ? "no_storage" : "reserved", "none", null, null, null, 1);
                    record.Validate(metadata);
                    await ReserveAsync(connection, record, owned, budget, mutation);
                }
                else
                {
                    record.Validate(metadata);
                    if (record.Relational != module.Storage.Relational)
                        throw new StorageException(StorageCode.Conflict);
                }
                if (!record.Relational)
                    return new StorageResult.NoStorage(StorageOperation.Register, Installation.Provider, module.Id,
                        correlation, mutation.Durability, StorageDiagnostics.Diagnostic(StorageCode.NoStorage));
                if (record.MigrationState is "in_progress" or "recovery_required")
                    throw new StorageException(StorageCode.ReconciliationRequired);
                await owned.VerifyAsync(Installation, budget);
                if (record.State != "ready")
                {
                    if (Installation.Provider == "postgresql")
                        await new PostgresStorageProvisioner(Installation).ProvisionAsync(metadata, record, budget, mutation);
                    else
                        await new SqliteStorageProvisioner(Installation).ProvisionAsync(metadata, record, budget, mutation);
                    await owned.VerifyAsync(Installation, budget);
                    mutation.PossibleCommit();
                    StorageDiagnostics.Checkpoint("ready_before_commit", Installation.Provider, module.Id);
                    await using var transition = RegistryMigrationRunner.Command(connection, $"""
                        UPDATE {database.Registrations} SET state='ready', revision=revision+1
                        WHERE module_id=@owner AND revision=@revision RETURNING revision
                        """, budget, null, ("owner", record.Owner.Value), ("revision", record.Revision));
                    if (await transition.ExecuteScalarAsync(budget.Token) is not long revision ||
                        revision != checked(record.Revision + 1))
                        throw new StorageException(StorageCode.ReconciliationRequired);
                    mutation.Committed();
                    StorageDiagnostics.Checkpoint("ready_committed", Installation.Provider, module.Id);
                    record = await database.FindAsync(connection, module.Id, budget) ??
                        throw new StorageException(StorageCode.Corrupt);
                    if (record.State != "ready") throw new StorageException(StorageCode.Incomplete);
                }
                await VerifyPhysicalAsync(metadata, record, budget);
                return Ready(metadata, record, StorageOperation.Register, correlation, mutation.Durability);
            });

    private async Task ReserveAsync(DbConnection connection, RegistrationRecord record, MaintenanceScope scope,
        OperationBudget budget, MutationEvidence mutation)
    {
        await using var transaction = connection is SqliteConnection sqlite
            ? sqlite.BeginTransaction(deferred: false) : await connection.BeginTransactionAsync(budget.Token);
        var sql = $"""
            INSERT INTO {database.Registrations}
            (module_id,relational,provider,allocation_key,allocator_version,allocation_id,state,migration_state,revision)
            VALUES (@owner,@relational,@provider,
            """;
        if (record.Relational)
            await RegistryMigrationRunner.ExecuteAsync(connection,
                sql + "@key,1,@allocation,'reserved','none',1)", budget, transaction,
                ("owner", record.Owner.Value), ("relational", true), ("provider", record.Provider),
                ("key", record.Key!), ("allocation", record.AllocationId!.Value));
        else
            await RegistryMigrationRunner.ExecuteAsync(connection,
                sql + "NULL,NULL,NULL,'no_storage','none',1)", budget, transaction,
                ("owner", record.Owner.Value), ("relational", false), ("provider", record.Provider));
        await scope.VerifyAsync(Installation, budget);
        mutation.PossibleCommit();
        StorageDiagnostics.Checkpoint("reservation_before_commit", Installation.Provider, record.Owner);
        await transaction.CommitAsync(budget.Token);
        mutation.Committed();
        StorageDiagnostics.Checkpoint("reservation_committed", Installation.Provider, record.Owner);
    }

    public Task<StorageResult> ResolveAsync(ModuleId owner, CancellationToken cancellationToken = default) =>
        StorageDiagnostics.RunAsync(StorageOperation.Resolve, Installation.Provider, owner, OperationBudget.Resolve,
            cancellationToken, async (budget, _, correlation) =>
            {
                await using var connection = await Installation.OpenPlatformAsync(budget);
                var metadata = await database.ReadInstallationAsync(connection, budget);
                var record = await database.FindAsync(connection, owner, budget) ??
                    throw new StorageException(StorageCode.RequestInvalid);
                record.Validate(metadata);
                if (!record.Relational)
                    return new StorageResult.NoStorage(StorageOperation.Resolve, Installation.Provider, owner,
                        correlation, StorageDurability.Unchanged, StorageDiagnostics.Diagnostic(StorageCode.NoStorage));
                if (record.State == "reserved") throw new StorageException(StorageCode.Incomplete);
                if (record.State != "ready" || record.MigrationState is "in_progress" or "recovery_required")
                    throw new StorageException(StorageCode.ReconciliationRequired);
                await VerifyPhysicalAsync(metadata, record, budget);
                return Ready(metadata, record, StorageOperation.Resolve, correlation, StorageDurability.Unchanged);
            });

    internal async Task VerifyPhysicalAsync(InstallationRecord metadata, RegistrationRecord record, OperationBudget budget)
    {
        record.Validate(metadata);
        if (Installation.Provider == "postgresql")
            await new PostgresStorageProvisioner(Installation).VerifyAsync(metadata, record, budget);
        else await new SqliteStorageProvisioner(Installation).VerifyAsync(metadata, record, budget);
    }

    private static StorageResult Ready(InstallationRecord metadata, RegistrationRecord record,
        StorageOperation operation, string correlation, StorageDurability durability) =>
        new StorageResult.Ready(operation, record.Assignment(metadata), correlation, durability,
            StorageDiagnostics.Diagnostic(StorageCode.Ready));
}
