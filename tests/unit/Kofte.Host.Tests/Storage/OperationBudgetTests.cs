using Kofte.Infrastructure.Storage;
using Xunit;

namespace Kofte.Host.Tests.Storage;

public sealed class OperationBudgetTests
{
    [Fact]
    public void LimitsAndCommandTimeoutsAreFiniteAndClamped()
    {
        using var budget = new OperationBudget(OperationBudget.Register, CancellationToken.None);
        Assert.InRange(budget.CommandSeconds(), 1, 3);
        Assert.Equal(1, budget.CommandSeconds(sqlite: true));
        var before = budget.Remaining;
        Assert.InRange(budget.Remaining, TimeSpan.Zero, before);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationBudget(Timeout.InfiniteTimeSpan, CancellationToken.None));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new OperationBudget(TimeSpan.Zero, CancellationToken.None));
    }

    [Fact]
    public void CancelledAndSubsecondBudgetsNeverReturnAnInfiniteCommandTimeout()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        using var budget = new OperationBudget(OperationBudget.Register, cancelled.Token);
        Assert.Throws<OperationCanceledException>(() => budget.CommandSeconds());
        Assert.True(budget.CallerCancelled);
        using var shortBudget = new OperationBudget(TimeSpan.FromMilliseconds(500), CancellationToken.None);
        Assert.Throws<OperationCanceledException>(() => shortBudget.CommandSeconds());
    }
}
