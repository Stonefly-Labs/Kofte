using System.Diagnostics;
using System.Text.Json;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.Bootstrap;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kofte.Host.Tests;

public sealed class HealthTests
{
    [Fact]
    public async Task IncompleteStartupDoesNotProbeOrReportStorageReady()
    {
        var store = new Probe();
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton<IPlatformStore>(store);
        builder.Services.AddSingleton<HealthHandler>();
        await using var app = builder.Build();
        app.MapBootstrapHealth();
        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints).OfType<RouteEndpoint>()
            .Single(endpoint => endpoint.RoutePattern.RawText == "/health/ready");
        var context = new DefaultHttpContext { RequestServices = app.Services };
        context.Request.Method = "GET";
        context.Request.Path = "/health/ready";
        context.RequestAborted = TestContext.Current.CancellationToken;
        await using var body = new MemoryStream();
        context.Response.Body = body;
        var invoke = endpoint.RequestDelegate ?? throw new InvalidOperationException("test_endpoint_delegate_missing");
        await invoke(context);
        body.Position = 0;
        using var payload = await JsonDocument.ParseAsync(body, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(503, context.Response.StatusCode);
        Assert.Equal("unavailable", payload.RootElement.GetProperty("state").GetString());
        Assert.Equal("host", payload.RootElement.GetProperty("component").GetString());
        Assert.Equal(0, store.Calls);
    }

    private sealed class BlockingProbe : IPlatformStore
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Stopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task ProbeAsync(CancellationToken token)
        {
            Started.SetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            finally { Stopped.SetResult(); }
        }
    }

    [Fact]
    public async Task DisconnectCancellationStopsAnAlreadyRunningProbe()
    {
        var store = new BlockingProbe();
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var pending = new HealthHandler(store).ReadyAsync(stop.Token);
        await store.Started.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
        await store.Stopped.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
    }

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
