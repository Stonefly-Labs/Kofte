using System.ComponentModel;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Storage;

public static class StorageMaintenanceProcess
{
    public const string AdmissionKey = "KOFTE_MAINTENANCE_WORKER_TOKEN";
    private const int BusyExit = 40, InvalidExit = 41;

    public static Task RunAsync(ProcessStartInfo start, CancellationToken cancellationToken = default,
        Func<StreamReader, CancellationToken, Task>? observeOutput = null)
    {
        var admission = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        start.Environment[AdmissionKey] = admission;
        return RunOwnedAsync(start, admission, OperationBudget.Migration, TimeSpan.FromSeconds(2),
            cancellationToken, observeOutput);
    }

    public static async Task AdmitAsync(CancellationToken cancellationToken = default)
    {
        var expected = Environment.GetEnvironmentVariable(AdmissionKey);
        Environment.SetEnvironmentVariable(AdmissionKey, null);
        if (expected is null || expected.Length != 64 ||
            expected.Any(c => !char.IsAsciiHexDigitLower(c)) || !Console.IsInputRedirected)
            throw new StorageException(StorageCode.RequestInvalid);
        using var budget = new OperationBudget(OperationBudget.Acquire, cancellationToken);
        var supplied = await Console.In.ReadLineAsync(budget.Token);
        if (supplied is null || supplied.Length != expected.Length ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(supplied)))
            throw new StorageException(StorageCode.RequestInvalid);
    }

    internal static async Task RunOwnedAsync(ProcessStartInfo start, string admission,
        TimeSpan totalBudget, TimeSpan cleanupReserve, CancellationToken cancellationToken,
        Func<StreamReader, CancellationToken, Task>? observeOutput = null)
    {
        if (totalBudget <= cleanupReserve || totalBudget > OperationBudget.Migration ||
            cleanupReserve <= TimeSpan.Zero || start.UseShellExecute ||
            !start.RedirectStandardInput || !start.RedirectStandardOutput || !start.RedirectStandardError)
            throw new StorageException(StorageCode.RequestInvalid);
        cancellationToken.ThrowIfCancellationRequested();
        var started = Stopwatch.GetTimestamp();
        TimeSpan Remaining() => TimeSpan.FromTicks(Math.Max(0,
            (totalBudget - Stopwatch.GetElapsedTime(started)).Ticks));
        using var process = new Process { StartInfo = start };
        try
        {
            if (!process.Start()) throw new StorageException(StorageCode.Unavailable);
        }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException or IOException)
        { throw new StorageException(StorageCode.Unavailable); }

        using var drainLifetime = new CancellationTokenSource(Remaining());
        async Task ObserveAsync()
        {
            if (observeOutput is null) await DrainAsync(process.StandardOutput, drainLifetime.Token);
            else await observeOutput(process.StandardOutput, drainLifetime.Token);
        }
        var stdout = ObserveAsync();
        var stderr = DrainAsync(process.StandardError, drainLifetime.Token);
        var drained = Task.WhenAll(stdout, stderr);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(TimeSpan.FromTicks(Math.Max(0, (Remaining() - cleanupReserve).Ticks)));
        try
        {
            await process.StandardInput.WriteLineAsync(admission.AsMemory(), budget.Token);
            process.StandardInput.Close();
            var exited = process.WaitForExitAsync(budget.Token);
            var observing = new List<Task> { exited, stdout, stderr };
            while (observing.Count != 0)
            {
                var completed = await Task.WhenAny(observing).WaitAsync(budget.Token);
                await completed;
                observing.Remove(completed);
            }
            if (process.ExitCode != 0)
                throw new StorageException(process.ExitCode switch
                {
                    BusyExit => StorageCode.Busy,
                    InvalidExit => StorageCode.RequestInvalid,
                    _ => StorageCode.Unavailable
                });
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { throw new StorageException(StorageCode.DeadlineExceeded); }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException or IOException)
        { throw new StorageException(StorageCode.Unavailable); }
        catch (Exception error) when (error is not (StorageException or OperationCanceledException))
        { throw new StorageException(StorageCode.Unavailable); }
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
                if (Remaining() <= TimeSpan.Zero) throw new StorageException(StorageCode.Unavailable);
            }
            catch (Exception)
            {
                // An unreaped child owns its lease; never replay an uncertain write.
                throw new StorageException(StorageCode.Unavailable);
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
