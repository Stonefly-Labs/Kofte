using System.Diagnostics;

namespace Kofte.Infrastructure.Storage;

internal sealed class OperationBudget : IDisposable
{
    private readonly long started = Stopwatch.GetTimestamp();
    private readonly TimeSpan duration;
    private readonly CancellationTokenSource cancellation;
    private readonly CancellationToken caller;

    internal static readonly TimeSpan Resolve = TimeSpan.FromSeconds(3);
    internal static readonly TimeSpan Acquire = TimeSpan.FromSeconds(2);
    internal static readonly TimeSpan Register = TimeSpan.FromSeconds(30);
    internal static readonly TimeSpan Migration = TimeSpan.FromSeconds(120);
    internal static readonly TimeSpan ProbeInterval = TimeSpan.FromMilliseconds(25);

    internal OperationBudget(TimeSpan duration, CancellationToken caller)
    {
        if (duration <= TimeSpan.Zero || duration > Migration)
            throw new ArgumentOutOfRangeException(nameof(duration));
        this.duration = duration;
        this.caller = caller;
        cancellation = CancellationTokenSource.CreateLinkedTokenSource(caller);
        cancellation.CancelAfter(duration);
    }

    internal TimeSpan Remaining => TimeSpan.FromTicks(Math.Max(0,
        (duration - Stopwatch.GetElapsedTime(started)).Ticks));
    internal CancellationToken Token => cancellation.Token;
    internal bool CallerCancelled => caller.IsCancellationRequested;

    internal void Check()
    {
        Token.ThrowIfCancellationRequested();
        if (Remaining <= TimeSpan.Zero)
            throw new OperationCanceledException(Token);
    }

    internal int CommandSeconds(bool sqlite = false)
    {
        Check();
        var seconds = (int)Math.Floor(Remaining.TotalSeconds);
        if (seconds < 1) throw new OperationCanceledException(Token);
        return Math.Min(sqlite ? 1 : 3, seconds);
    }

    public void Dispose() => cancellation.Dispose();
}
