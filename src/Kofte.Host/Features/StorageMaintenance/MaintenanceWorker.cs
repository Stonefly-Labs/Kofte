using Kofte.Host.Bootstrap;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Host.Features.StorageMaintenance;

public static class MaintenanceWorker
{
    internal const string AdmissionKey = StorageMaintenanceProcess.AdmissionKey;
    internal const int BusyExit = 40, InvalidExit = 41, UnavailableExit = 42;

    public static async Task<int> ExecuteAsync(string[] args)
    {
        try { await RunAsync(args); return 0; }
        catch (StorageException error) { return error.Code == StorageCode.Busy ? BusyExit : InvalidExit; }
        catch (StorePathException error) { return error.Code == StorePathError.StoreBusy ? BusyExit : InvalidExit; }
        catch (BootstrapException) { throw; }
        catch { return UnavailableExit; }
    }

    public static async Task RunAsync(string[] args, CancellationToken token = default)
    {
        if (args is not ["--registry-worker", "upgrade"])
            throw new BootstrapException(BootstrapError.CommandInvalid);
        try { await StorageMaintenanceProcess.AdmitAsync(token); }
        catch (StorageException) { throw new BootstrapException(BootstrapError.CommandInvalid); }

        var config = StorageMaintenanceConfiguration.FromEnvironment();
        var installation = config.CreateInstallation();
        var maintenance = new Infrastructure.Storage.StorageMaintenance(installation);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromSeconds(27));
        await using var scope = await maintenance.AcquireAsync(budget.Token);
        await Infrastructure.Storage.Registry.StorageReconciliation.ValidateStartupAsync(installation, budget.Token);
        await maintenance.UpgradeAsync(scope, budget.Token);
    }
}
