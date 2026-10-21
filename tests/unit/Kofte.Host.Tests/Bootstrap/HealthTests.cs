using System.Diagnostics;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.Bootstrap;
using Xunit;

namespace Kofte.Host.Tests;

public sealed class HealthTests
{
    [Fact]
    public async Task ProbeTracesUseOnlySafeFieldsWithoutRequestTraceStateOrBaggage()
    {
        Activity? observed = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kofte.Bootstrap",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => observed = activity
        };
        ActivitySource.AddActivityListener(listener);
        using var parent = new Activity("request").SetIdFormat(ActivityIdFormat.W3C);
        parent.TraceStateString = "sensitive=credential-canary";
        parent.AddBaggage("sensitive", "credential-canary");
        parent.Start();
        await new HealthHandler(new Probe()).ReadyAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(observed);
        Assert.Equal(parent.TraceId, observed.TraceId);
        Assert.Null(observed.TraceStateString);
        Assert.Empty(observed.Baggage);
        Assert.All(observed.TagObjects, tag =>
            Assert.Contains(tag.Key, new[] { "component", "event.code" }));
        Assert.DoesNotContain("credential-canary", string.Join(",", observed.TagObjects));
    }

    private sealed class Probe : IPlatformStore
    {
        public bool Fail { get; set; }
        public int Calls { get; private set; }
        public Task ProbeAsync(CancellationToken token)
        {
            Calls++;
            token.ThrowIfCancellationRequested();
            if (Fail) throw new BootstrapException(BootstrapError.StorageUnavailable);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task EachReadinessRequestUsesAFreshProbeAndNeverRetainsSuccess()
    {
        var store = new Probe();
        var handler = new HealthHandler(store);
        Assert.Equal("ready", (await handler.ReadyAsync(CancellationToken.None)).State);
        store.Fail = true;
        Assert.Equal("unavailable", (await handler.ReadyAsync(CancellationToken.None)).State);
        store.Fail = false;
        Assert.Equal("ready", (await handler.ReadyAsync(CancellationToken.None)).State);
        Assert.Equal(3, store.Calls);
    }

    [Fact]
    public async Task CancellationStopsReadinessWork()
    {
        var store = new Probe();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new HealthHandler(store).ReadyAsync(source.Token));
    }
}
