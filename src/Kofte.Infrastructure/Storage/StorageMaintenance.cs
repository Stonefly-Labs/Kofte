using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Storage;

public sealed class StorageMaintenance(StorageInstallation installation) : IStorageMaintenance
{
    public async Task<IStorageMaintenanceScope> AcquireAsync(CancellationToken cancellationToken = default)
    {
        try { return await MaintenanceScope.AcquireAsync(installation, cancellationToken); }
        catch (Exception error)
        {
            var code = StorageDiagnostics.MaintenanceFailure(StorageOperation.Acquire, installation.Provider,
                error, cancellationToken);
            if (error is StorageException) throw;
            if (error is OperationCanceledException) throw new OperationCanceledException(cancellationToken);
            throw new StorageException(code);
        }
    }

    public Task UpgradeAsync(IStorageMaintenanceScope scope, CancellationToken cancellationToken = default) =>
        StorageDiagnostics.RunMaintenanceAsync(installation.Provider, cancellationToken, async (budget, mutation) =>
        {
            if (scope is not MaintenanceScope owned)
                throw new StorageException(StorageCode.RequestInvalid);
            await new RegistryMigrationRunner(installation).UpgradeAsync(owned, budget, mutation);
        });
}
