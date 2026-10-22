using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Storage;

public sealed class StorageMaintenance(StorageInstallation installation) : IStorageMaintenance
{
    public async Task<IStorageMaintenanceScope> AcquireAsync(CancellationToken cancellationToken = default) =>
        await MaintenanceScope.AcquireAsync(installation, cancellationToken);

    public async Task UpgradeAsync(IStorageMaintenanceScope scope, CancellationToken cancellationToken = default)
    {
        if (scope is not MaintenanceScope owned)
            throw new StorageException(StorageCode.RequestInvalid);
        using var budget = new OperationBudget(OperationBudget.Register, cancellationToken);
        await new RegistryMigrationRunner(installation).UpgradeAsync(owned, budget);
    }
}
