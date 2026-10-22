using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Kofte.Infrastructure.Storage.Migrations;

namespace Kofte.Infrastructure.Storage.Registry;

public sealed partial class StorageRegistry
{
    public Task<ModuleStorageBinding> BindAsync(ModuleId owner,
        CancellationToken cancellationToken = default) => BindingFailures.RunAsync(async () =>
        {
            using var budget = new OperationBudget(OperationBudget.Resolve, cancellationToken);
            var (metadata, record) = await ReadBindingAsync(owner, budget);
            return new ModuleStorageBinding(this, record.Assignment(metadata));
        });

    internal async Task<(InstallationRecord Metadata, RegistrationRecord Record)> ReadBindingAsync(
        ModuleId owner, OperationBudget budget, bool migrating = false, bool verifyPhysical = true)
    {
        await using var connection = await Installation.OpenPlatformAsync(budget);
        var metadata = await database.ReadInstallationAsync(connection, budget);
        var record = await database.FindAsync(connection, owner, budget) ??
            throw new StorageException(StorageCode.RequestInvalid);
        if (!record.Relational || record.State != "ready")
            throw new StorageException(StorageCode.RequestInvalid);
        if (!migrating && record.MigrationState is "in_progress" or "recovery_required")
            throw new StorageException(StorageCode.ReconciliationRequired);
        if (verifyPhysical) await VerifyPhysicalAsync(metadata, record, budget);
        return (metadata, record);
    }

    internal async Task<StorageAssignment> SetMigrationStateAsync(StorageAssignment assignment,
        MaintenanceScope scope, string state, string? target, string? checkpoint, OperationBudget budget)
    {
        await scope.VerifyAsync(Installation, budget);
        var (metadata, record) = await ReadBindingAsync(assignment.Owner, budget, migrating: true);
        ModuleStorageBinding.Match(assignment, record.Assignment(metadata));
        await using var connection = await Installation.OpenPlatformAsync(budget);
        await using var transaction = await connection.BeginTransactionAsync(budget.Token);
        await RegistryMigrationRunner.ExecuteAsync(connection, $"""
            UPDATE {database.Registrations}
            SET migration_state=@state, migration_target=@target, migration_checkpoint=@checkpoint,
                last_outcome_code=@code, revision=revision+1
            WHERE module_id=@owner AND revision=@revision;
            """, budget, transaction, ("state", state), ("target", (object?)target ?? DBNull.Value),
            ("checkpoint", (object?)checkpoint ?? DBNull.Value),
            ("code", state == "recovery_required" ? "storage_reconciliation_required" : DBNull.Value),
            ("owner", assignment.Owner.Value), ("revision", assignment.Revision));
        await scope.VerifyAsync(Installation, budget);
        await transaction.CommitAsync(budget.Token);
        var updated = await database.FindAsync(connection, assignment.Owner, budget) ??
            throw new StorageException(StorageCode.Corrupt);
        if (updated.Revision != assignment.Revision + 1 || updated.MigrationState != state)
            throw new StorageException(StorageCode.ReconciliationRequired);
        return updated.Assignment(metadata);
    }
}
