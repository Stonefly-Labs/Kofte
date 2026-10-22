using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;

namespace Kofte.Infrastructure.Storage.Sqlite;

internal sealed class SqliteStorageProvisioner(StorageInstallation installation)
{
    internal async Task ProvisionAsync(InstallationRecord metadata, RegistrationRecord record,
        OperationBudget budget, MutationEvidence mutation)
    {
        var paths = new ModuleStorePaths(installation, record.Key!);
        if (paths.Exists())
        {
            await VerifyAsync(metadata, record, budget);
            return;
        }
        if (record.State != "reserved") throw new StorageException(StorageCode.Missing);
        mutation.PossibleCommit();
        paths.Create();
        using var connection = StorageInstallation.OpenSqlite(paths.DatabasePath, budget, allowOwnedEmpty: true);
        await RegistryMigrationRunner.ExecuteAsync(connection,
            "PRAGMA journal_mode=DELETE; PRAGMA synchronous=FULL; PRAGMA foreign_keys=ON", budget);
        using var transaction = connection.BeginTransaction(deferred: false);
        await StorageOwnerMarker.CreateAsync(installation, connection, transaction, metadata, record, budget);
        budget.Check();
        StorageDiagnostics.Checkpoint("physical_before_commit", installation.Provider, record.Owner);
        transaction.Commit();
        mutation.Committed();
        StorageDiagnostics.Checkpoint("physical_committed", installation.Provider, record.Owner);
        await VerifyAsync(metadata, record, budget);
    }

    internal async Task VerifyAsync(InstallationRecord metadata, RegistrationRecord record, OperationBudget budget)
    {
        var paths = new ModuleStorePaths(installation, record.Key!);
        if (!paths.Exists()) throw new StorageException(StorageCode.Missing);
        using (var file = File.OpenRead(paths.DatabasePath))
        {
            var header = new byte[16];
            if (file.Length < 100 || file.Read(header) != header.Length ||
                !header.AsSpan().SequenceEqual("SQLite format 3\0"u8))
                throw new StorageException(StorageCode.ReconciliationRequired);
        }
        using var connection = StorageInstallation.OpenSqlite(paths.DatabasePath, budget, readOnly: true);
        if (await RegistryMigrationRunner.ScalarAsync(connection, "PRAGMA integrity_check", budget) is not "ok" ||
            await RegistryMigrationRunner.ScalarAsync(connection, "PRAGMA journal_mode", budget) is not "delete" ||
            await RegistryMigrationRunner.ScalarAsync(connection, "PRAGMA synchronous", budget) is not 2L ||
            await RegistryMigrationRunner.ScalarAsync(connection, "PRAGMA foreign_keys", budget) is not 1L)
            throw new StorageException(StorageCode.Corrupt);
        if (await RegistryMigrationRunner.ScalarAsync(connection,
            "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='__kofte_storage_owner'", budget) is not 1L)
            throw new StorageException(StorageCode.ReconciliationRequired);
        await StorageOwnerMarker.VerifyAsync(installation, connection, metadata, record, budget);
    }
}
