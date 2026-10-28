using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Kofte.TestSupport;

public sealed class CompositionProcess : IAsyncDisposable
{
    private readonly OwnedProcess root;
    private readonly string canary;
    private readonly List<OwnedProcess> descendants = [];
    private readonly List<(Process Process, int Group)> reportedDescendants = [];
    private bool disposed;
    private readonly Task<string>? observedOutput;
    private readonly object outputGate = new();
    private readonly System.Text.StringBuilder liveOutput = new();

    internal CompositionProcess(OwnedProcess root, string canary, bool observeOutput = false)
    {
        (this.root, this.canary) = (root, canary);
        observedOutput = observeOutput ? CaptureAsync() : null;
    }

    public bool HasExited => ExitVerified || root.HasExited;
    public int RootProcessId => root.Id;
    public bool ExitVerified { get; private set; }
    public bool DescendantExitsVerified { get; private set; }
    public int TrackedDescendantCount => descendants.Count + reportedDescendants.Count;
    public string OutputSnapshot
    {
        get
        {
            lock (outputGate)
            {
                var value = liveOutput.ToString();
                if (value.Contains(canary, StringComparison.Ordinal))
                    throw new InvalidOperationException("composition_fixture_output_not_private");
                return value;
            }
        }
    }

    private async Task<string> CaptureAsync()
    {
        using var reader = new StreamReader(root.StandardOutput, leaveOpen: true);
        var buffer = new char[1024];
        int count;
        while ((count = await reader.ReadAsync(buffer)) != 0)
            lock (outputGate)
            {
                if (liveOutput.Length + count > 65_536)
                    throw new InvalidOperationException("composition_fixture_output_limit");
                liveOutput.Append(buffer, 0, count);
            }
        return OutputSnapshot;
    }

    public async Task<ProcessOutput> ResultAsync()
    {
        var result = await root.ResultAsync();
        if (observedOutput is not null) result = result with { Stdout = await observedOutput };
        RequirePrivate(result);
        return result;
    }

    public void TrackFixtureDescendant(OwnedProcess descendant)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (descendants.Contains(descendant) || ReferenceEquals(root, descendant))
            throw new InvalidOperationException("composition_descendant_already_owned");
        descendants.Add(descendant);
    }

    internal void TrackAdmittedChild(Process child)
    {
        var group = GetProcessGroup(root.Id);
        if (HasExited || child.HasExited || child.Id == root.Id || group <= 0 ||
            GetProcessGroup(child.Id) != group || reportedDescendants.Any(item => item.Process.Id == child.Id))
            throw new InvalidOperationException("composition_descendant_ownership_unverified");
        reportedDescendants.Add((child, group));
    }

    public void TrackAuthorDescendant(CompositionFixture.CompositionAuthorTrace trace)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (HasExited || OperatingSystem.IsWindows())
            throw new InvalidOperationException("composition_descendant_ownership_unverified");
        var records = trace.Records;
        var record = records.Single(item => item.Phase == "descendant");
        if (record.Worker == root.Id || record.Value == root.Id || record.Value == record.Worker ||
            records.Any(item => item.Worker != record.Worker || item.Attempt != record.Attempt) ||
            GetProcessGroup(record.Worker) != record.Worker || GetProcessGroup(record.Value) != record.Worker ||
            reportedDescendants.Any(item => item.Process.Id == record.Value))
            throw new InvalidOperationException("composition_descendant_ownership_unverified");
        var child = Process.GetProcessById(record.Value);
        if (child.HasExited)
        {
            child.Dispose();
            throw new InvalidOperationException("composition_descendant_ownership_unverified");
        }
        reportedDescendants.Add((child, record.Worker));
    }

    public async Task<ProcessOutput> WaitAsync(CancellationToken token)
    {
        await root.WaitAsync(token);
        var result = await ResultAsync();
        ExitVerified = root.HasExited;
        if (descendants.Any(process => !process.HasExited) || reportedDescendants.Any(item => !item.Process.HasExited))
            throw new InvalidOperationException("composition_descendants_still_running");
        foreach (var descendant in descendants) await descendant.ResultAsync();
        DescendantExitsVerified = true;
        RequirePrivate(result);
        return result;
    }

    public async Task StopAsync()
    {
        await root.StopAsync(graceful: true, gracefulTimeout: TimeSpan.FromSeconds(15));
        ExitVerified = root.HasExited;
        foreach (var descendant in descendants.AsEnumerable().Reverse()) await descendant.StopAsync();
        foreach (var (child, group) in reportedDescendants.AsEnumerable().Reverse())
        {
            if (!child.HasExited)
            {
                if (GetProcessGroup(child.Id) != group)
                    throw new InvalidOperationException("composition_descendant_ownership_unverified");
                child.Kill(entireProcessTree: true);
                using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await child.WaitForExitAsync(budget.Token);
            }
        }
        DescendantExitsVerified = descendants.All(process => process.HasExited) &&
            reportedDescendants.All(item => item.Process.HasExited);
        if (!ExitVerified || !DescendantExitsVerified)
            throw new InvalidOperationException("composition_exit_unverified");
        RequirePrivate(await ResultAsync());
    }

    private void RequirePrivate(ProcessOutput output)
    {
        if (output.Stdout.Contains(canary, StringComparison.Ordinal) ||
            output.Stderr.Contains(canary, StringComparison.Ordinal))
            throw new InvalidOperationException("composition_fixture_output_not_private");
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        await StopAsync();
        foreach (var descendant in descendants.AsEnumerable().Reverse()) await descendant.DisposeAsync();
        foreach (var (child, _) in reportedDescendants) child.Dispose();
        await root.DisposeAsync();
        disposed = true;
    }

    [DllImport("libc", EntryPoint = "getpgid", SetLastError = true)]
    private static extern int GetProcessGroup(int processId);
}
