using Kofte.Host.Features.ModuleComposition;
using Xunit;

namespace Kofte.Host.Tests.Features.ModuleComposition;

public sealed class CompositionAuthorityTests
{
    [Fact]
    public async Task LostAuthorityIsLatchedWithoutAnotherVerificationOrReacquisition()
    {
        var calls = 0;
        var withdrawals = 0;
        var failure = new InvalidOperationException("private-provider-error");
        var authority = new CompositionAuthority(_ => { calls++; throw failure; }, () => withdrawals++);
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
            authority.EnsureAsync(TestContext.Current.CancellationToken)));
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() =>
            authority.EnsureAsync(TestContext.Current.CancellationToken)));
        Assert.Equal(1, calls);
        Assert.Equal(1, withdrawals);
    }

    [Fact]
    public async Task ConcurrentBarriersSerializeUseOfTheSameRetainedSession()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var authority = new CompositionAuthority(async token =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                entered.SetResult();
                await release.Task.WaitAsync(token);
            }
        }, () => throw new InvalidOperationException("unexpected authority loss"));
        var first = authority.EnsureAsync(TestContext.Current.CancellationToken);
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        var second = authority.EnsureAsync(TestContext.Current.CancellationToken);
        Assert.False(second.IsCompleted);
        Assert.Equal(1, calls);
        release.SetResult();
        await Task.WhenAll(first, second);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task PeriodicWatchPerformsRealVerificationAndWithdrawsOnFailure()
    {
        var calls = 0;
        var withdrawals = 0;
        var authority = new CompositionAuthority(_ =>
        {
            if (++calls > 1) throw new InvalidOperationException("session-lost");
            return Task.CompletedTask;
        }, () => withdrawals++);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            authority.WatchAsync(TestContext.Current.CancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
        Assert.Equal(2, calls);
        Assert.Equal(1, withdrawals);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            authority.EnsureAsync(TestContext.Current.CancellationToken));
        Assert.Equal(2, calls);
    }
}
