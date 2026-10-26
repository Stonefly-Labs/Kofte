using System.Reflection;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.ModuleComposition;
using Kofte.Platform.Modules.Composition;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Kofte.Host.Tests.Features.ModuleComposition;

public sealed class CompositionBudgetTests
{
    [Fact]
    public async Task ComposedHostDoesNotStartUnusedDefaultConfigurationWatchers()
    {
        var token = TestContext.Current.CancellationToken;
        var composition = Assert.IsType<CompositionResolution.Accepted>(
            CompositionResolver.Resolve([], [], new("1.0.0"), token)).Composition;
        var configuration = BootstrapConfiguration.Parse(new Dictionary<string, string?>(), Directory.GetCurrentDirectory());
        // Exercise the actual private worker builder without adding a public hosting API.
        var worker = typeof(ModuleCompositionHost).Assembly.GetType(
            "Kofte.Host.Features.ModuleComposition.CompositionWorker", throwOnError: true)!;
        var factory = worker.GetMethod("Builder", BindingFlags.NonPublic | BindingFlags.Static)!;
        var builder = Assert.IsType<WebApplicationBuilder>(factory.Invoke(null,
            [configuration, new CompositionHealth(composition, new string('a', 32))]));
        await using var app = builder.Build();
        var content = Assert.IsType<PhysicalFileProvider>(
            app.Services.GetRequiredService<IHostEnvironment>().ContentRootFileProvider);
        // This supported setter refuses changes once a physical watcher has been initialized.
        content.UsePollingFileWatcher = content.UsePollingFileWatcher;
        var attempt = new CompositionAttempt(new(CompositionMode.Serve, "none", _ => { }));
        attempt.BeginCleanup();
        await new CompositionCallbackRunner(attempt).RunAsync(CompositionWork.Cleanup, _ => app.DisposeAsync(), token);
    }

    [Theory]
    [InlineData(CompositionMode.Validate, 45, 35, 10)]
    [InlineData(CompositionMode.Prepare, 120, 105, 15)]
    [InlineData(CompositionMode.Serve, 60, 45, 15)]
    public void NestedOperationsAndCleanupStayInsideSharedMonotonicTotal(
        CompositionMode mode, int total, int work, int reserve)
    {
        var clock = new ManualClock();
        var diagnostics = new CompositionDiagnostics(mode, "none", _ => { }, clock);
        var attempt = new CompositionAttempt(diagnostics, clock: clock);
        Assert.Equal(total, attempt.Total.TotalSeconds);
        Assert.Equal(work, attempt.Work.TotalSeconds);
        Assert.Equal(reserve, attempt.CleanupReserve.TotalSeconds);
        clock.Advance(TimeSpan.FromSeconds(work - 0.125));
        Assert.Equal(TimeSpan.FromSeconds(0.125), attempt.Clip(CompositionWork.MigrateStorage));
        Assert.Equal(TimeSpan.FromSeconds(0.125), attempt.Clip(CompositionWork.Validator));
        clock.Advance(TimeSpan.FromSeconds(0.125));
        Assert.Equal("composition_deadline_exceeded",
            Assert.Throws<CompositionBoundaryException>(() => attempt.Check()).Code);
        attempt.BeginCleanup();
        Assert.False(attempt.Publication);
        Assert.Equal(TimeSpan.FromSeconds(1), attempt.Clip(CompositionWork.Stop));
        clock.Advance(TimeSpan.FromSeconds(reserve - 5));
        Assert.Equal(TimeSpan.Zero, attempt.Clip(CompositionWork.Cleanup));
        Assert.Equal(TimeSpan.FromSeconds(5), attempt.RemainingTermination());
        clock.Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(TimeSpan.Zero, attempt.RemainingTermination());
        Assert.Throws<CompositionBoundaryException>(() => attempt.BeginCleanup());
    }

    [Fact]
    public void CancelledWorkStillRetainsBoundedIndependentCleanupAndNeverClaimsTermination()
    {
        using var cancellation = new CancellationTokenSource();
        var clock = new ManualClock();
        var attempt = new CompositionAttempt(new(CompositionMode.Prepare, "unknown", _ => { }, clock),
            cancellation.Token, clock);
        cancellation.Cancel();
        Assert.Equal("composition_cancelled",
            Assert.Throws<CompositionBoundaryException>(() => attempt.Clip(CompositionWork.Registrar)).Code);
        attempt.BeginCleanup();
        Assert.Equal(TimeSpan.FromSeconds(1), attempt.Clip(CompositionWork.Cleanup));
        Assert.Throws<CompositionBoundaryException>(() => attempt.Clip(CompositionWork.Initialize));
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(TimeSpan.Zero, attempt.Clip(CompositionWork.Cleanup));
        Assert.Equal(TimeSpan.FromSeconds(5), attempt.RemainingTermination());
        Assert.False(attempt.Publication);
    }

    [Fact]
    public void EarlyFailureDoesNotExpandStopReserveIntoUnusedStartupWork()
    {
        var clock = new ManualClock();
        var attempt = new CompositionAttempt(new(CompositionMode.Serve, "none", _ => { }, clock), clock: clock);
        attempt.BeginCleanup();
        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.Equal(TimeSpan.Zero, attempt.Clip(CompositionWork.Stop));
        clock.Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(TimeSpan.Zero, attempt.RemainingTermination());
    }

    [Fact]
    public void AcceptedStorageCapsAndLifecycleCapsAreClippedWithoutExpandingOuterWork()
    {
        var attempt = new CompositionAttempt(new(CompositionMode.Prepare, "sqlite", _ => { }));
        Assert.InRange(attempt.Clip(CompositionWork.MigrateStorage).TotalSeconds, 100, 105);
        Assert.Equal(TimeSpan.FromSeconds(30), attempt.Clip(CompositionWork.RegisterStorage));
        Assert.Equal(TimeSpan.FromSeconds(3), attempt.Clip(CompositionWork.ResolveStorage));
        Assert.Equal(TimeSpan.FromSeconds(2), attempt.Clip(CompositionWork.AcquireStorage));
        Assert.Equal(TimeSpan.FromSeconds(0.25), attempt.Clip(CompositionWork.Validator));
        Assert.Equal(TimeSpan.FromSeconds(1), attempt.Clip(CompositionWork.Registrar));
        Assert.Equal(TimeSpan.FromSeconds(2), attempt.Clip(CompositionWork.Initialize));
        Assert.Equal(TimeSpan.FromSeconds(2), attempt.Clip(CompositionWork.Start));
        Assert.Equal(TimeSpan.FromSeconds(1), attempt.Clip(CompositionWork.Health));
        Assert.Equal(TimeSpan.FromSeconds(20), attempt.Clip(CompositionWork.HealthSweep));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationDrainsCooperativeWorkInsideItsOriginalDeadline(bool completesNormally)
    {
        var testToken = TestContext.Current.CancellationToken;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(testToken);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempt = new CompositionAttempt(new(CompositionMode.Serve, "none", _ => { }));
        var running = new CompositionCallbackRunner(attempt).RunAsync(CompositionWork.ResolveStorage, async token =>
        {
            entered.SetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                observed.SetResult();
                await release.Task;
                if (!completesNormally) throw;
            }
            finally { exited.SetResult(); }
        }, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(3), testToken);
            cancellation.Cancel();
            await observed.Task.WaitAsync(TimeSpan.FromSeconds(3), testToken);
            await Task.WhenAny(running, Task.Delay(50, testToken));
            Assert.False(running.IsCompleted);
            release.SetResult();
            var error = await Assert.ThrowsAsync<CompositionBoundaryException>(() => running);
            Assert.Equal("composition_cancelled", error.Code);
            Assert.True(exited.Task.IsCompletedSuccessfully);
        }
        finally
        {
            release.TrySetResult();
            await exited.Task.WaitAsync(TimeSpan.FromSeconds(3), testToken);
            if (running.IsFaulted) _ = running.Exception;
        }
    }

    [Fact]
    public async Task CancellationDoesNotExtendTheOriginalDeadlineForUncooperativeWork()
    {
        var testToken = TestContext.Current.CancellationToken;
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(testToken);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var attempt = new CompositionAttempt(new(CompositionMode.Validate, "none", _ => { }));
        var running = new CompositionCallbackRunner(attempt).RunAsync(CompositionWork.Validator, async _ =>
        {
            entered.SetResult();
            await release.Task;
        }, cancellation.Token);
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(3), testToken);
            cancellation.Cancel();
            var error = await Assert.ThrowsAsync<CompositionUnstoppedException>(() =>
                running.WaitAsync(TimeSpan.FromSeconds(1), testToken));
            Assert.Equal("composition_cancelled", error.Code);
            Assert.Equal(CompositionWork.Validator, error.Work);
        }
        finally { release.TrySetResult(); }
    }

    private sealed class ManualClock : TimeProvider
    {
        private long now;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => now;
        public void Advance(TimeSpan elapsed) => now += elapsed.Ticks;
    }
}
