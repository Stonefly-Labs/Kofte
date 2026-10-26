using Kofte.Host.Features.ModuleComposition;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Xunit;

namespace Kofte.Host.Tests.Features.ModuleComposition;

public sealed class CompositionHealthTests
{
    [Theory]
    [InlineData(ModuleHealth.Healthy, ModuleLifecycleOutcome.Completed)]
    [InlineData(ModuleHealth.Degraded, ModuleLifecycleOutcome.Failed)]
    [InlineData(ModuleHealth.Unknown, ModuleLifecycleOutcome.Failed)]
    [InlineData(ModuleHealth.Failed, ModuleLifecycleOutcome.Failed)]
    public void CurrentHealthPreservesOwnStateAndTransitiveImpactWithoutMisreportingSuccess(
        ModuleHealth own, ModuleLifecycleOutcome outcome)
    {
        var clock = new Clock();
        var health = Create(clock);
        foreach (var owner in Owners) health.Observe(HostCompositionTestData.Id(owner), new(ModuleHealth.Healthy));
        health.Publish();
        var original = health.Snapshot;
        health.Observe(HostCompositionTestData.Id(Owners[0]), new(own));
        var current = health.Snapshot;
        var prerequisite = current.Modules.Single(module => module.Owner.Value == Owners[0]);
        Assert.Equal(own, prerequisite.OwnHealth);
        Assert.Equal(outcome, prerequisite.Outcome);
        foreach (var owner in Owners.Skip(1).Take(2))
        {
            var dependent = current.Modules.Single(module => module.Owner.Value == owner);
            Assert.Equal(ModuleHealth.Healthy, dependent.OwnHealth);
            Assert.Equal(own, dependent.EffectiveHealth);
            Assert.Equal(own == ModuleHealth.Healthy ? ModuleLifecycleOutcome.Completed : ModuleLifecycleOutcome.Blocked,
                dependent.Outcome);
            Assert.Equal(own == ModuleHealth.Healthy ? [] : owner == Owners[1] ? [Owners[0]] : new[] { Owners[0], Owners[1] },
                dependent.AffectedPrerequisites.Select(value => value.Value));
        }
        Assert.Equal(ModuleHealth.Healthy, current.Modules.Single(module => module.Owner.Value == Owners[3]).EffectiveHealth);
        Assert.Equal(own == ModuleHealth.Healthy, health.IsReady);
        Assert.All(original.Modules, module => Assert.Equal(ModuleHealth.Healthy, module.EffectiveHealth));
    }

    [Fact]
    public void FreshnessExpiresIndividuallyAt30SecondsEvenWithoutACompletedNextSweep()
    {
        var clock = new Clock();
        var health = Create(clock);
        health.Observe(HostCompositionTestData.Id(Owners[0]), new(ModuleHealth.Healthy));
        clock.Advance(5);
        foreach (var owner in Owners.Skip(1)) health.Observe(HostCompositionTestData.Id(owner), new(ModuleHealth.Healthy));
        health.Publish();
        clock.Advance(24);
        Assert.True(health.IsReady);
        clock.Advance(1);
        var snapshot = health.Snapshot;
        Assert.False(health.IsReady);
        Assert.Equal(ModuleHealth.Unknown, snapshot.Modules.Single(module => module.Owner.Value == Owners[0]).OwnHealth);
        Assert.Equal(ModuleHealth.Unknown, snapshot.Modules.Single(module => module.Owner.Value == Owners[2]).EffectiveHealth);
        Assert.Equal(ModuleHealth.Healthy, snapshot.Modules.Single(module => module.Owner.Value == Owners[3]).EffectiveHealth);
        Assert.All(snapshot.Modules, module => Assert.Equal(snapshot.AttemptId, module.AttemptId));
    }

    [Fact]
    public void PublicationRechecksAConcurrentFailedObservationInsteadOfPublishingAnOldHealthySnapshot()
    {
        var clock = new Clock();
        var health = Create(clock);
        foreach (var owner in Owners) health.Observe(HostCompositionTestData.Id(owner), new(ModuleHealth.Healthy));
        clock.OnTimestamp = () => health.Observe(HostCompositionTestData.Id(Owners[0]), new(ModuleHealth.Failed));
        var failure = Assert.Throws<CompositionBoundaryException>(health.Publish);
        Assert.Equal("composition_health_failed", failure.Code);
        Assert.False(health.Snapshot.Published);
        Assert.False(health.IsReady);
    }

    internal static readonly string[] Owners = ["org.test.a", "org.test.b", "org.test.c", "org.test.d"];

    private static CompositionHealth Create(Clock clock)
    {
        ModuleDeployment[] catalog = [
            new(HostCompositionTestData.Manifest(Owners[2], Owners[1])),
            new(HostCompositionTestData.Manifest(Owners[3])),
            new(HostCompositionTestData.Manifest(Owners[1], Owners[0])),
            new(HostCompositionTestData.Manifest(Owners[0]))];
        var composition = Assert.IsType<CompositionResolution.Accepted>(CompositionResolver.Resolve(
            catalog, Owners.Select(owner => new ModuleSelection(owner, true)), new("1.0.0"),
            TestContext.Current.CancellationToken)).Composition;
        return new(composition, new string('a', 32), clock);
    }

    private sealed class Clock : TimeProvider
    {
        private long now;
        public Action? OnTimestamp { get; set; }
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp()
        {
            var action = OnTimestamp;
            OnTimestamp = null;
            action?.Invoke();
            return now;
        }
        public void Advance(int seconds) => now += TimeSpan.FromSeconds(seconds).Ticks;
    }
}
