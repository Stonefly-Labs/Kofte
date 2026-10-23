using System.Data.Common;
using Kofte.Infrastructure.Storage.Allocation;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Storage.Registry;

internal static class StorageOwnerMarker
{
    internal static string Table(StorageInstallation installation, RegistrationRecord record) =>
        (installation.Provider == "postgresql" ? StorageAllocator.Quote(record.Key!) + "." : "") +
        "__kofte_storage_owner";

    internal static async Task CreateAsync(StorageInstallation installation, DbConnection connection,
        DbTransaction transaction, InstallationRecord metadata, RegistrationRecord record, OperationBudget budget)
    {
        var table = Table(installation, record);
        var uuidType = installation.Provider == "postgresql" ? "UUID" : "TEXT";
        await RegistryMigrationRunner.ExecuteAsync(connection, $"""
            CREATE TABLE {table} (
              singleton_key INTEGER PRIMARY KEY CHECK (singleton_key=1),
              marker_version INTEGER NOT NULL CHECK (marker_version=1),
              installation_id {uuidType} NOT NULL,
              module_id TEXT NOT NULL,
              allocation_id {uuidType} NOT NULL,
              provider TEXT NOT NULL,
              allocation_key TEXT NOT NULL);
            INSERT INTO {table} VALUES (1,1,@installation,@owner,@allocation,@provider,@key)
            """, budget, transaction, ("installation", metadata.Id), ("owner", record.Owner.Value),
            ("allocation", record.AllocationId!.Value), ("provider", record.Provider), ("key", record.Key!));
    }

    internal static async Task VerifyAsync(StorageInstallation installation, DbConnection connection,
        InstallationRecord metadata, RegistrationRecord record, OperationBudget budget)
    {
        await using var command = RegistryMigrationRunner.Command(connection,
            $"SELECT singleton_key,marker_version,installation_id,module_id,allocation_id,provider,allocation_key FROM {Table(installation, record)}",
            budget);
        await using var reader = await command.ExecuteReaderAsync(budget.Token);
        if (!await reader.ReadAsync(budget.Token) || reader.GetInt32(0) != 1 || reader.GetInt32(1) != 1 ||
            reader.GetGuid(2) != metadata.Id || reader.GetString(3) != record.Owner.Value ||
            reader.GetGuid(4) != record.AllocationId || reader.GetString(5) != record.Provider ||
            reader.GetString(6) != record.Key || await reader.ReadAsync(budget.Token))
            throw new StorageException(StorageCode.OwnerMismatch);
    }
}
