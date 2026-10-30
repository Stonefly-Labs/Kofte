using System.Diagnostics;
using System.Globalization;

namespace Kofte.TestSupport.Identity;

public sealed class IdentityProcess : IAsyncDisposable
{
    private readonly CompositionProcess lifecycle;

    internal IdentityProcess(OwnedProcess owned, string canary)
    {
        lifecycle = new(owned, canary, observeOutput: true);
    }

    public int ProcessId => lifecycle.RootProcessId;
    public bool HasExited => lifecycle.HasExited;
    public bool ExitVerified => lifecycle.ExitVerified;
    public bool DescendantExitsVerified => lifecycle.DescendantExitsVerified;
    public string OutputSnapshot => lifecycle.OutputSnapshot;
    public Task<ProcessOutput> WaitAsync(CancellationToken token) => lifecycle.WaitAsync(token);
    public Task StopAsync() => lifecycle.StopAsync();
    public void TrackOwnedDescendant(OwnedProcess process) => lifecycle.TrackFixtureDescendant(process);

    public async Task<ProcessOutput> InterruptMaintenanceChildAsync(IdentityCommitBarrier barrier, CancellationToken token)
    {
        if (HasExited || barrier.ObservedProcessId is not > 0 || barrier.ObservedProcessId == ProcessId)
            throw new InvalidOperationException("identity_fixture_child_ownership_invalid");
        var child = Process.GetProcessById(barrier.ObservedProcessId.Value);
        var probe = new ProcessStartInfo("ps")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[] { "-p", child.Id.ToString(CultureInfo.InvariantCulture), "-o", "ppid=" })
            probe.ArgumentList.Add(argument);
        var parent = await OwnedProcesses.RunAsync(probe, TimeSpan.FromSeconds(2), token);
        if (parent.ExitCode != 0 || !int.TryParse(parent.Stdout.Trim(), NumberStyles.None,
                CultureInfo.InvariantCulture, out var owner) || owner != ProcessId || HasExited || child.HasExited)
        {
            child.Dispose();
            throw new InvalidOperationException("identity_fixture_child_ownership_invalid");
        }
        lifecycle.TrackAdmittedChild(child);
        child.Kill(entireProcessTree: true);
        using var cleanup = CancellationTokenSource.CreateLinkedTokenSource(token);
        cleanup.CancelAfter(TimeSpan.FromSeconds(5));
        await child.WaitForExitAsync(cleanup.Token);
        var result = await WaitAsync(cleanup.Token);
        if (!child.HasExited || !ExitVerified || !DescendantExitsVerified)
            throw new InvalidOperationException("identity_fixture_child_exit_unverified");
        return result;
    }

    public ValueTask DisposeAsync() => lifecycle.DisposeAsync();
}
