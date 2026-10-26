using Kofte.Platform.Modules.Manifest;

namespace Kofte.Host.Features.ModuleComposition;

public sealed class CompositionUnstoppedException : Exception
{
    public string Code { get; }
    public CompositionWork Work { get; }
    public ModuleId? Owner { get; }
    internal CompositionUnstoppedException(string code, CompositionWork work, ModuleId? owner) : base(code) =>
        (Code, Work, Owner) = (code, work, owner);
}

public sealed class CompositionCallbackRunner(CompositionAttempt attempt)
{
    public async Task RunAsync(CompositionWork work, Func<CancellationToken, ValueTask> callback,
        CancellationToken token = default, ModuleId? owner = null)
    {
        await RunAsync(work, async cancellation =>
        {
            await callback(cancellation);
            return true;
        }, token, owner);
    }

    public async Task<T> RunAsync<T>(CompositionWork work, Func<CancellationToken, ValueTask<T>> callback,
        CancellationToken token = default, ModuleId? owner = null)
    {
        var cap = attempt.Clip(work);
        if (cap <= TimeSpan.Zero)
            throw new CompositionBoundaryException("composition_deadline_exceeded");
        using var deadline = new CancellationTokenSource(cap);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token, deadline.Token);
        var pending = Task.Run(async () => await callback(budget.Token), CancellationToken.None);
        try { return await pending.WaitAsync(budget.Token); }
        catch (OperationCanceledException)
        {
            if (!pending.IsCompleted && token.IsCancellationRequested && !deadline.IsCancellationRequested)
            {
                // Cancellation is a request; drain only inside the original callback deadline.
                try { await pending.WaitAsync(deadline.Token); }
                catch (OperationCanceledException) when (pending.IsCompleted || deadline.IsCancellationRequested) { }
            }
            var code = token.IsCancellationRequested ? "composition_cancelled" : "composition_deadline_exceeded";
            if (!pending.IsCompleted)
            {
                attempt.FailPhase(attempt.Phase, token.IsCancellationRequested
                    ? CompositionOutcome.Cancelled : CompositionOutcome.Deadline, code);
                throw new CompositionUnstoppedException(code, work, owner);
            }
            throw new CompositionBoundaryException(code);
        }
    }
}
