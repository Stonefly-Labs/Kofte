using System.Runtime.ExceptionServices;

namespace Kofte.Host.Features.ModuleComposition;

public sealed class CompositionAuthority(Func<CancellationToken, Task> verifyHeldAuthority, Action withdraw)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private ExceptionDispatchInfo? lost;

    public async Task EnsureAsync(CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            lost?.Throw();
            try { await verifyHeldAuthority(token); }
            catch (Exception error) when (!token.IsCancellationRequested)
            {
                lost = ExceptionDispatchInfo.Capture(error);
                withdraw();
                throw;
            }
        }
        finally { gate.Release(); }
    }

    public async Task WatchAsync(CancellationToken token)
    {
        while (true)
        {
            await EnsureAsync(token);
            await Task.Delay(TimeSpan.FromSeconds(1), token);
        }
    }
}
