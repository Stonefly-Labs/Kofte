using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Kofte.TestSupport;

public sealed record ProcessOutput(int ExitCode, string Stdout, string Stderr);

public static class OwnedProcesses
{
    public static string FindRepository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Kofte.slnx")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("test_repository_missing");
    }

    public static void ClearSensitiveEnvironment(ProcessStartInfo start)
    {
        foreach (var key in start.Environment.Keys.Where(key =>
            key.StartsWith("KOFTE_", StringComparison.Ordinal) ||
            key.StartsWith("OTEL_", StringComparison.Ordinal)).ToArray())
            start.Environment.Remove(key);
    }

    public static OwnedProcess Start(ProcessStartInfo start, bool captureOutput = true) =>
        new(Process.Start(start) ?? throw new InvalidOperationException("test_process_start_failed"), captureOutput);

    public static async Task<ProcessOutput> RunAsync(ProcessStartInfo start, TimeSpan timeout,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await using var child = Start(start);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(timeout);
        return await child.WaitAsync(budget.Token);
    }
}

public sealed class OwnedProcess : IAsyncDisposable
{
    private readonly Process process;
    private readonly Task<string>? output;
    private readonly Task<string> error;
    private Task? stop;
    private bool disposed;

    internal OwnedProcess(Process process, bool captureOutput)
    {
        this.process = process;
        output = captureOutput ? ReadBoundedAsync(process.StandardOutput) : null;
        error = ReadBoundedAsync(process.StandardError);
    }

    public bool HasExited => process.HasExited;
    public int Id => process.Id;
    public Stream StandardOutput => process.StandardOutput.BaseStream;
    public StreamWriter StandardInput => process.StandardInput;

    private async Task<string> ReadBoundedAsync(StreamReader reader)
    {
        var buffer = new char[4096];
        var text = new StringBuilder();
        int count;
        while ((count = await reader.ReadAsync(buffer)) != 0)
        {
            if (text.Length + count > 1_000_000)
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                throw new InvalidOperationException("test_process_output_limit");
            }
            text.Append(buffer, 0, count);
        }
        return text.ToString();
    }

    public async Task<ProcessOutput> WaitAsync(CancellationToken token)
    {
        await process.WaitForExitAsync(token);
        return await ResultAsync();
    }

    public async Task<ProcessOutput> ResultAsync()
    {
        if (!process.HasExited) throw new InvalidOperationException("test_process_still_running");
        await Task.WhenAll(output ?? Task.FromResult(""), error);
        return new(process.ExitCode, output is null ? "" : await output, await error);
    }

    public Task StopAsync(bool graceful = false, TimeSpan? gracefulTimeout = null) =>
        stop ??= StopCoreAsync(graceful, gracefulTimeout ?? TimeSpan.FromSeconds(5));

    private async Task StopCoreAsync(bool graceful, TimeSpan gracefulTimeout)
    {
        if (gracefulTimeout <= TimeSpan.Zero || gracefulTimeout > TimeSpan.FromSeconds(15))
            throw new InvalidOperationException("test_process_stop_budget_invalid");
        if (!process.HasExited && graceful && !OperatingSystem.IsWindows())
        {
            if (Signal(process.Id, 15) != 0 && !process.HasExited)
                process.Kill(entireProcessTree: true);
            using var grace = new CancellationTokenSource(gracefulTimeout);
            try { await process.WaitForExitAsync(grace.Token); }
            catch (OperationCanceledException) when (grace.IsCancellationRequested) { }
        }
        if (!process.HasExited) process.Kill(entireProcessTree: true);
        using var forced = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        await process.WaitForExitAsync(forced.Token);
        await ResultAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        try { await StopAsync(); }
        finally
        {
            process.Dispose();
            disposed = true;
        }
    }

    [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static extern int Signal(int processId, int signal);
}
