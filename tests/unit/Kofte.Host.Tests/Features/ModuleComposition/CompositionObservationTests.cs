using Kofte.Host.Features.ModuleComposition;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Xunit;

namespace Kofte.Host.Tests.Features.ModuleComposition;

public sealed class CompositionObservationTests
{
    [Fact]
    public void ReenabledAttemptCannotReuseOldHealthAndDisabledOwnersRemainExplicit()
    {
        ModuleDeployment[] catalog = [new(HostCompositionTestData.Manifest("org.test.a"))];
        var enabled = Assert.IsType<CompositionResolution.Accepted>(CompositionResolver.Resolve(
            catalog, [new("org.test.a", true)], new("1.0.0"), TestContext.Current.CancellationToken)).Composition;
        var disabled = Assert.IsType<CompositionResolution.Accepted>(CompositionResolver.Resolve(
            catalog, [new("org.test.a", false)], new("1.0.0"), TestContext.Current.CancellationToken)).Composition;
        var original = new CompositionHealth(enabled, new string('a', 32));
        original.Observe(enabled.Order[0], new(ModuleHealth.Healthy));
        original.Publish();
        original.Withdraw();
        var stopped = original.Snapshot;
        var absent = new CompositionHealth(disabled, new string('b', 32));
        absent.Publish();
        var observation = Assert.Single(absent.Snapshot.Modules);
        Assert.False(observation.DesiredEnabled);
        Assert.Equal(ModuleLifecycleOutcome.Disabled, observation.Outcome);
        Assert.Equal(ModuleHealth.Unknown, observation.OwnHealth);
        Assert.Equal(CompositionAvailability.Empty, absent.Snapshot.Availability);
        var fresh = new CompositionHealth(enabled, new string('c', 32));
        Assert.False(fresh.IsReady);
        Assert.False(fresh.Snapshot.Published);
        Assert.Equal(ModuleHealth.Unknown, Assert.Single(fresh.Snapshot.Modules).OwnHealth);
        Assert.Throws<CompositionBoundaryException>(fresh.Publish);
        fresh.Observe(enabled.Order[0], new(ModuleHealth.Healthy));
        fresh.Publish();
        Assert.True(fresh.IsReady);
        Assert.NotEqual(stopped.AttemptId, fresh.Snapshot.AttemptId);
        Assert.Equal(stopped.AttemptId, original.Snapshot.AttemptId);
        Assert.False(original.IsReady);
    }

    [Fact]
    public void FreshnessIsIndividualAndRecomputedWhileASweepIsStuck()
    {
        var clock = new Clock();
        var health = Create(clock);
        health.Observe(HostCompositionTestData.Id("org.test.a"), new(ModuleHealth.Healthy));
        clock.Advance(5);
        health.Observe(HostCompositionTestData.Id("org.test.b"), new(ModuleHealth.Healthy));
        health.Observe(HostCompositionTestData.Id("org.test.c"), new(ModuleHealth.Healthy));
        health.Publish();
        Assert.True(health.IsReady);
        clock.Advance(25);
        var snapshot = health.Snapshot;
        Assert.False(health.IsReady);
        Assert.Equal(ModuleHealth.Unknown, snapshot.Modules.Single(m => m.Owner.Value == "org.test.a").OwnHealth);
        Assert.Equal(ModuleHealth.Healthy, snapshot.Modules.Single(m => m.Owner.Value == "org.test.b").OwnHealth);
        Assert.Equal(ModuleHealth.Unknown, snapshot.Modules.Single(m => m.Owner.Value == "org.test.b").EffectiveHealth);
        Assert.Equal(ModuleHealth.Healthy, snapshot.Modules.Single(m => m.Owner.Value == "org.test.c").EffectiveHealth);
        Assert.Equal(["org.test.a"], snapshot.Modules.Single(m => m.Owner.Value == "org.test.b")
            .AffectedPrerequisites.Select(i => i.Value));
    }

    [Fact]
    public void FailedDominatesUnknownThenDegradedAndPublicationIsWholeSet()
    {
        var health = Create(new Clock());
        health.Observe(HostCompositionTestData.Id("org.test.a"), new(ModuleHealth.Failed));
        health.Observe(HostCompositionTestData.Id("org.test.b"), new(ModuleHealth.Unknown));
        health.Observe(HostCompositionTestData.Id("org.test.c"), new(ModuleHealth.Degraded));
        Assert.Throws<CompositionBoundaryException>(health.Publish);
        Assert.False(health.Snapshot.Published);
        Assert.Equal(ModuleHealth.Failed, health.Snapshot.Modules.Single(m => m.Owner.Value == "org.test.b").EffectiveHealth);
        foreach (var id in new[] { "org.test.a", "org.test.b", "org.test.c" })
            health.Observe(HostCompositionTestData.Id(id), new(ModuleHealth.Healthy));
        health.Publish();
        Assert.True(health.IsReady);
        health.Withdraw();
        Assert.False(health.IsReady);
        Assert.False(health.Snapshot.Published);
    }

    [Fact]
    public void ConcurrentAuthorityWithdrawalCannotBeOverwrittenByAnInFlightHealthObservation()
    {
        var clock = new Clock();
        var health = Create(clock);
        foreach (var id in new[] { "org.test.a", "org.test.b", "org.test.c" })
            health.Observe(HostCompositionTestData.Id(id), new(ModuleHealth.Healthy));
        health.Publish();
        clock.OnTimestamp = health.Withdraw;
        health.Observe(HostCompositionTestData.Id("org.test.a"), new(ModuleHealth.Healthy));
        Assert.False(health.IsReady);
        Assert.False(health.Snapshot.Published);
    }

    [Fact]
    public void WithdrawnAttemptCannotPublishEvenWithFreshHealthyObservations()
    {
        var health = Create(new Clock());
        foreach (var id in new[] { "org.test.a", "org.test.b", "org.test.c" })
            health.Observe(HostCompositionTestData.Id(id), new(ModuleHealth.Healthy));
        health.Withdraw();
        Assert.Throws<CompositionBoundaryException>(health.Publish);
        Assert.False(health.IsReady);
    }

    private static CompositionHealth Create(Clock clock)
    {
        ModuleDeployment[] catalog = [new(HostCompositionTestData.Manifest("org.test.b", "org.test.a")),
            new(HostCompositionTestData.Manifest("org.test.a")), new(HostCompositionTestData.Manifest("org.test.c"))];
        var composition = Assert.IsType<CompositionResolution.Accepted>(CompositionResolver.Resolve(catalog,
            catalog.Select(d => new ModuleSelection(
                Assert.IsType<Kofte.Platform.Modules.Manifest.ManifestValidationResult.Accepted>(
                    Kofte.Platform.Modules.Manifest.ManifestValidator.Validate(d.Manifest.Span, new("1.0.0")))
                    .Description.Id.Value, true)), new("1.0.0"))).Composition;
        return new(composition, new string('a', 32), clock);
    }

    private sealed class Clock : TimeProvider
    {
        private long time;
        public Action? OnTimestamp { get; set; }
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp()
        {
            var action = OnTimestamp;
            OnTimestamp = null;
            action?.Invoke();
            return time;
        }
        public void Advance(int seconds) => time += TimeSpan.FromSeconds(seconds).Ticks;
    }
}
