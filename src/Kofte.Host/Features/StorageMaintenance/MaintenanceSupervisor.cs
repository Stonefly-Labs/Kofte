using System.Diagnostics;
using System.Security.Cryptography;
using Kofte.Host.Bootstrap;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Host.Features.StorageMaintenance;

public static class MaintenanceSupervisor
{
    public static async Task RunUpgradeAsync(StorageMaintenanceConfiguration config,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await StorageDiagnostics.RunMaintenanceAsync(config.Runtime.Profile == "local-sqlite" ? "sqlite" : "postgresql",
                cancellationToken, (budget, mutation) => RunUpgradeWorkerAsync(config, budget, mutation));
        }
        catch (StorageException error) { throw Failure(error.Code); }
    }

    private static async Task RunUpgradeWorkerAsync(StorageMaintenanceConfiguration config,
        OperationBudget budget, MutationEvidence mutation)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath ??
            throw new BootstrapException(BootstrapError.ConfigurationInvalid))
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        if (Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(typeof(BootstrapProgram).Assembly.Location);
        else if (typeof(BootstrapProgram).Assembly != System.Reflection.Assembly.GetEntryAssembly())
        {
            start.FileName = Path.Combine(Environment.GetEnvironmentVariable("DOTNET_ROOT") ??
                throw new BootstrapException(BootstrapError.ConfigurationInvalid), "dotnet");
            start.ArgumentList.Add(typeof(BootstrapProgram).Assembly.Location);
        }
        start.ArgumentList.Add("--registry-worker");
        start.ArgumentList.Add("upgrade");
        config.ConfigureChild(start);
        var admission = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        start.Environment[MaintenanceWorker.AdmissionKey] = admission;
        var remaining = budget.Remaining;
        if (remaining <= TimeSpan.FromSeconds(2))
            throw new StorageException(StorageCode.DeadlineExceeded);
        mutation.PossibleCommit();
        await StorageMaintenanceProcess.RunOwnedAsync(start, admission, remaining, TimeSpan.FromSeconds(2),
            budget.Token);
    }

    internal static async Task RunOwnedAsync(ProcessStartInfo start, string admission,
        TimeSpan totalBudget, TimeSpan cleanupReserve, CancellationToken cancellationToken)
    {
        if (totalBudget <= cleanupReserve || cleanupReserve <= TimeSpan.Zero || start.UseShellExecute ||
            !start.RedirectStandardInput || !start.RedirectStandardOutput || !start.RedirectStandardError)
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        try
        {
            await StorageMaintenanceProcess.RunOwnedAsync(start, admission, totalBudget, cleanupReserve,
                cancellationToken);
        }
        catch (StorageException error)
        {
            throw Failure(error.Code);
        }
    }

    private static BootstrapException Failure(StorageCode code) => new(code switch
    {
        StorageCode.Busy => BootstrapError.StoreBusy,
        StorageCode.RequestInvalid or StorageCode.VersionUnsupported or StorageCode.Corrupt or
            StorageCode.OwnerMismatch or StorageCode.PathRejected or StorageCode.ReconciliationRequired =>
            BootstrapError.StorageInvalid,
        _ => BootstrapError.StorageUnavailable
    });
}
