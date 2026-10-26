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
    public ValueTask DisposeAsync() => lifecycle.DisposeAsync();
}
