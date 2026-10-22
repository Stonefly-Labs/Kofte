using System.Diagnostics;
using System.ComponentModel;
using System.Security.Cryptography;
using Kofte.Host.Bootstrap;

namespace Kofte.Host.Features.StorageMaintenance;

public static class MaintenanceSupervisor
{
    public static async Task RunUpgradeAsync(StorageMaintenanceConfiguration config,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var started = Stopwatch.GetTimestamp();
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
        var remaining = TimeSpan.FromSeconds(30) - Stopwatch.GetElapsedTime(started);
        if (remaining <= TimeSpan.FromSeconds(2))
            throw new BootstrapException(BootstrapError.StorageUnavailable);
        await RunOwnedAsync(start, admission, remaining, TimeSpan.FromSeconds(2),
            cancellationToken);
    }

    internal static async Task RunOwnedAsync(ProcessStartInfo start, string admission,
        TimeSpan totalBudget, TimeSpan cleanupReserve, CancellationToken cancellationToken)
    {
        if (totalBudget <= cleanupReserve || cleanupReserve <= TimeSpan.Zero || start.UseShellExecute ||
            !start.RedirectStandardInput || !start.RedirectStandardOutput || !start.RedirectStandardError)
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        cancellationToken.ThrowIfCancellationRequested();
        var started = Stopwatch.GetTimestamp();
        TimeSpan Remaining() => TimeSpan.FromTicks(Math.Max(0,
            (totalBudget - Stopwatch.GetElapsedTime(started)).Ticks));
        using var process = new Process { StartInfo = start };
        try
        {
            if (!process.Start()) throw new BootstrapException(BootstrapError.StorageUnavailable);
        }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException or IOException)
        { throw new BootstrapException(BootstrapError.StorageUnavailable); }

        using var drainLifetime = new CancellationTokenSource(Remaining());
        // Drain, but never retain or relay provider/worker text.
        var stdout = DrainAsync(process.StandardOutput, drainLifetime.Token);
        var stderr = DrainAsync(process.StandardError, drainLifetime.Token);
        var drained = Task.WhenAll(stdout, stderr);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(TimeSpan.FromTicks(Math.Max(0, (Remaining() - cleanupReserve).Ticks)));
        try
        {
            await process.StandardInput.WriteLineAsync(admission.AsMemory(), budget.Token);
            process.StandardInput.Close();
            await process.WaitForExitAsync(budget.Token);
            await drained.WaitAsync(budget.Token);
            if (process.ExitCode != 0)
                throw new BootstrapException(process.ExitCode switch
                {
                    MaintenanceWorker.BusyExit => BootstrapError.StoreBusy,
                    MaintenanceWorker.InvalidExit => BootstrapError.StorageInvalid,
                    _ => BootstrapError.StorageUnavailable
                });
        }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException or IOException)
        { throw new BootstrapException(BootstrapError.StorageUnavailable); }
        finally
        {
            var remaining = Remaining();
            using var cleanup = new CancellationTokenSource(
                remaining < cleanupReserve ? remaining : cleanupReserve);
            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(cleanup.Token);
                await drained.WaitAsync(cleanup.Token);
                if (Remaining() <= TimeSpan.Zero)
                    throw new BootstrapException(BootstrapError.StorageUnavailable);
            }
            catch (Exception error) when (error is OperationCanceledException or Win32Exception or
                InvalidOperationException or IOException)
            {
                // An unconfirmed child retains its own lease; never claim cleanup or replay its write.
                throw new BootstrapException(BootstrapError.StorageUnavailable);
            }
            finally
            {
                drainLifetime.Cancel();
                _ = drained.ContinueWith(task => _ = task.Exception, CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }
        }
    }

    private static async Task DrainAsync(StreamReader reader, CancellationToken token)
    {
        var buffer = new char[1024];
        while (await reader.ReadAsync(buffer.AsMemory(), token) != 0) { }
    }
}
