using System.Net;
using System.Text.Json;
using Kofte.Composition.TestModule.Modules;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.Bootstrap;
using Kofte.Host.Features.ModuleComposition;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Kofte.TestSupport;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kofte.Composition.Consumer.Scenarios;

internal static class HealthScenarios
{
    internal static async Task<ScenarioEvidence> ImpactAsync(string provider, CancellationToken token)
    {
        var evidence = new ScenarioEvidence("health-impact");
        await using var fixture = await CompositionFixture.CreateAsync(provider, token);
        token = fixture.Storage.Token;
        await fixture.SeedUnrelatedNoStorageAsync(token);
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        var control = new HealthControl();
        var catalog = HealthModules.Catalog(control);
        var resolution = CompositionResolver.Resolve(catalog,
            HealthModules.Owners.Select(owner => new ModuleSelection(owner, true)), new("1.0.0"), token);
        evidence.Require(resolution is CompositionResolution.Accepted, "composition_test_health_graph_rejected");
        var composition = ((CompositionResolution.Accepted)resolution).Composition;
        var descriptions = catalog.Select(deployment =>
            ((ManifestValidationResult.Accepted)ManifestValidator.Validate(deployment.Manifest.Span, new("1.0.0"))).Description).ToArray();
        var clock = new Clock();
        var diagnostics = new CompositionDiagnostics(CompositionMode.Serve, provider == "postgres" ? "postgresql" : "sqlite",
            _ => { }, clock);
        var coordinator = new ModuleLifecycleCoordinator(composition,
            descriptions.Select((owner, index) => (owner.Id, Deployment: catalog[index]))
                .ToDictionary(pair => pair.Id, pair => pair.Deployment),
            descriptions.ToDictionary(owner => owner.Id, owner => new ModuleConfiguration(owner.Id, "{}"u8)),
            new CompositionAttempt(diagnostics, token, clock), diagnostics, clock: clock);
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        IPlatformStore store = fixture.Storage.Postgres is { } postgres
            ? new PostgresPlatformStore(postgres.ConnectionString) : new SqlitePlatformStore(fixture.Storage.DataRoot);
        builder.Services.AddSingleton(store);
        builder.Services.AddSingleton(coordinator.Health);
        builder.Services.AddSingleton<HealthHandler>();
        await coordinator.RegisterAsync(builder.Services, token);
        await using var app = builder.Build();
        app.MapBootstrapHealth();
        await coordinator.InitializeAsync(app.Services, token);
        coordinator.MarkMigrated();
        await app.StartAsync(token);
        using var http = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        try
        {
            await CheckAsync(ready: false);
            await coordinator.StartAsync(new Dictionary<ModuleId, IModuleStorage>(), token);
            foreach (var own in new[] { ModuleHealth.Healthy, ModuleHealth.Degraded, ModuleHealth.Failed, ModuleHealth.Unknown })
            {
                control.Alpha = own;
                await coordinator.SweepAsync(token);
                var snapshot = coordinator.Health.Snapshot;
                var alpha = snapshot.Modules.Single(module => module.Owner.Value == BootModules.Alpha);
                evidence.Require(alpha.OwnHealth == own && alpha.EffectiveHealth == own &&
                    alpha.AttemptId == snapshot.AttemptId, "composition_test_own_health_misreported");
                evidence.Observed("ownStateComparisons");
                var gamma = snapshot.Modules.Single(module => module.Owner.Value == HealthModules.Gamma);
                evidence.Require(gamma.OwnHealth == ModuleHealth.Healthy && gamma.EffectiveHealth == own &&
                    gamma.AffectedPrerequisites.Select(owner => owner.Value).SequenceEqual(
                        own == ModuleHealth.Healthy ? [] : new[] { BootModules.Alpha, BootModules.Beta }),
                    "composition_test_transitive_health_not_propagated");
                evidence.Observed("transitiveImpactComparisons");
                evidence.Require(snapshot.Modules.Single(module => module.Owner.Value == BootModules.NoStorage)
                    .EffectiveHealth == ModuleHealth.Healthy, "composition_test_independent_root_hidden");
                evidence.Observed("independentRootComparisons");
                await CheckAsync(own == ModuleHealth.Healthy);
            }
            control.Alpha = ModuleHealth.Healthy;
            await coordinator.SweepAsync(token);
            clock.Advance(5);
            foreach (var owner in composition.Enabled.Where(owner => owner.Value != BootModules.Alpha))
                coordinator.Health.Observe(owner, new(ModuleHealth.Healthy));
            clock.Advance(25);
            var stale = coordinator.Health.Snapshot;
            evidence.Require(stale.Modules.Single(module => module.Owner.Value == BootModules.Alpha).OwnHealth == ModuleHealth.Unknown &&
                stale.Modules.Single(module => module.Owner.Value == HealthModules.Gamma).OwnHealth == ModuleHealth.Healthy &&
                stale.Modules.Single(module => module.Owner.Value == HealthModules.Gamma).EffectiveHealth == ModuleHealth.Unknown &&
                stale.Modules.Single(module => module.Owner.Value == BootModules.NoStorage).EffectiveHealth == ModuleHealth.Healthy,
                "composition_test_individual_freshness_not_recomputed");
            evidence.Observed("freshnessComparisons");
            await CheckAsync(ready: false);
            foreach (var path in new[] { "/modules", "/api/kofte/modules", "/health/modules" })
            {
                using var response = await http.GetAsync(path, token);
                evidence.Require(response.StatusCode == HttpStatusCode.NotFound,
                    "composition_test_anonymous_lifecycle_route_exposed");
            }
            evidence.Observed("anonymousPrivacyComparisons");
        }
        finally
        {
            await coordinator.CleanupAsync();
            await app.StopAsync(token);
        }
        evidence.Require(before == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
            "composition_test_health_mutated_installation");
        return evidence;

        async Task CheckAsync(bool ready)
        {
            var callbacks = control.Checks;
            foreach (var endpoint in new[] { "live", "ready" })
            {
                using var response = await http.GetAsync("/health/" + endpoint, token);
                var available = endpoint == "live" || ready;
                evidence.Require(response.StatusCode == (available ? HttpStatusCode.OK : HttpStatusCode.ServiceUnavailable) &&
                    response.Headers.CacheControl?.NoStore == true, "composition_test_health_http_status_wrong");
                using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token));
                var root = body.RootElement;
                evidence.Require(root.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal)
                        .SetEquals(["version", "state", "component", "correlationId"]) &&
                    root.GetProperty("state").GetString() == (available ? endpoint : "unavailable") &&
                    root.GetProperty("component").GetString() == (endpoint == "live" || !ready ? "host" : "storage") &&
                    root.GetProperty("correlationId").GetString() is { Length: 32 },
                    "composition_test_health_http_schema_or_privacy_wrong");
            }
            evidence.Require(callbacks == control.Checks, "composition_test_http_executed_author_health");
            evidence.Observed("aggregateResponseComparisons");
        }
    }

    private sealed class Clock : TimeProvider
    {
        private long now;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => now;
        internal void Advance(int seconds) => now += TimeSpan.FromSeconds(seconds).Ticks;
    }
}
