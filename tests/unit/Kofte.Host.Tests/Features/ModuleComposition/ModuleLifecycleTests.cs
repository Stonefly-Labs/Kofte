using Kofte.Host.Features.ModuleComposition;
using System.Diagnostics;
using Kofte.Infrastructure.Composition;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kofte.Host.Tests.Features.ModuleComposition;

public sealed class ModuleLifecycleTests
{
    [Fact]
    public async Task HealthSweepsAreSerializedAndObservationWaitsTenSecondsAfterThePreviousSweep()
    {
        var calls = 0;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var next = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var runtime = new HealthRuntime(async token =>
        {
            var call = Interlocked.Increment(ref calls);
            if (call == 2)
            {
                entered.TrySetResult();
                await release.Task.WaitAsync(token);
            }
            if (call == 4) next.TrySetResult(Stopwatch.GetTimestamp());
            return new(ModuleHealth.Healthy);
        });
        var coordinator = Create([new(HostCompositionTestData.Manifest("org.test.a"),
            createRuntime: (_, _) => new(runtime, ModuleRuntimeOwnership.Attempt))], record: record =>
        {
            if (calls >= 4 && record.Stage == "health" && record.Owner is null &&
                record.Code == "composition_ready" && record.Outcome == "passed")
                completed.TrySetResult();
        });
        IServiceCollection services = new ServiceCollection();
        await coordinator.RegisterAsync(services, TestContext.Current.CancellationToken);
        await using var provider = services.BuildServiceProvider();
        await coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken);
        coordinator.MarkMigrated();
        await coordinator.StartAsync(new Dictionary<ModuleId, IModuleStorage>(), TestContext.Current.CancellationToken);
        var first = coordinator.SweepAsync(TestContext.Current.CancellationToken);
        await entered.Task.WaitAsync(TestContext.Current.CancellationToken);
        var second = coordinator.SweepAsync(TestContext.Current.CancellationToken);
        await Task.Delay(20, TestContext.Current.CancellationToken);
        Assert.Equal(2, calls);
        release.TrySetResult();
        await Task.WhenAll(first, second);
        Assert.Equal(3, calls);
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        var started = Stopwatch.GetTimestamp();
        var observing = coordinator.ObserveAsync(stop.Token);
        try
        {
            var observed = await next.Task.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
            Assert.True(Stopwatch.GetElapsedTime(started, observed) >= TimeSpan.FromSeconds(9.99));
            await completed.Task.WaitAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            stop.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => observing);
            await coordinator.CleanupAsync();
        }
    }

    [Fact]
    public async Task PartialActualStorageProgressCannotCompleteTheWholeMigrationBarrier()
    {
        CompositionAttempt? captured = null;
        var coordinator = Create(Catalog([]), capture: attempt => captured = attempt);
        var token = TestContext.Current.CancellationToken;
        var services = new ServiceCollection();
        await coordinator.RegisterAsync(services, token);
        await using var provider = services.BuildServiceProvider();
        await coordinator.InitializeAsync(provider, token);
        var first = ModuleId.TryParse("org.test.a", out var a) ? a : throw new InvalidOperationException();
        var later = ModuleId.TryParse("org.test.b", out var b) ? b : throw new InvalidOperationException();
        coordinator.ObserveStorage(new(first, CompositionStorageTransition.Entered));
        coordinator.ObserveStorage(new(first, CompositionStorageTransition.Completed));
        coordinator.ObserveStorage(new(later, CompositionStorageTransition.Entered));
        Assert.Throws<CompositionBoundaryException>(coordinator.MarkMigrated);
        var attempt = Assert.IsType<CompositionAttempt>(captured);
        Assert.Equal([first, later], attempt.EnteredOwnerStages
            .Where(entry => entry.Stage == CompositionStage.Migrated).Select(entry => entry.Owner));
        Assert.Equal([first], attempt.CompletedOwnerStages
            .Where(entry => entry.Stage == CompositionStage.Migrated).Select(entry => entry.Owner));
        Assert.False(attempt.Publication);
        coordinator.ObserveStorage(new(later, CompositionStorageTransition.Completed));
        coordinator.MarkMigrated();
        await coordinator.CleanupAsync();
    }

    [Fact]
    public async Task WholePhaseBarriersAndReverseCleanupIncludePartiallyEnteredRuntime()
    {
        var events = new List<string>();
        var catalog = Catalog(events, failStart: true);
        CompositionAttempt? captured = null;
        var coordinator = Create(catalog, capture: value => captured = value);
        var attempt = Assert.IsType<CompositionAttempt>(captured);
        var services = new ServiceCollection();
        await coordinator.RegisterAsync(services, TestContext.Current.CancellationToken);
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        await coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken);
        events.Add("verify-all");
        coordinator.MarkMigrated();
        var failure = await Assert.ThrowsAsync<CompositionBoundaryException>(() =>
            coordinator.StartAsync(new Dictionary<ModuleId, IModuleStorage>(), TestContext.Current.CancellationToken));
        Assert.Equal("composition_activation_failed", failure.Code);
        Assert.Equal(CompositionStage.Started, attempt.Phase);
        Assert.Equal(CompositionOutcome.Failed, attempt.Outcome);
        Assert.Equal(["org.test.a", "org.test.b"], attempt.EnteredOwnerStages
            .Where(entry => entry.Stage == CompositionStage.Started).Select(entry => entry.Owner.Value));
        Assert.Equal(["org.test.a"], attempt.CompletedOwnerStages
            .Where(entry => entry.Stage == CompositionStage.Started).Select(entry => entry.Owner.Value));
        Assert.Equal(2, attempt.CompletedOwnerStages.Count(entry => entry.Stage == CompositionStage.Initialized));
        Assert.DoesNotContain(attempt.EnteredOwnerStages, entry => entry.Stage == CompositionStage.Migrated);
        var beforeCleanup = attempt.EnteredOwnerStages;
        Assert.Throws<NotSupportedException>(() =>
            Assert.IsAssignableFrom<IList<CompositionOwnerStage>>(beforeCleanup).Clear());
        Assert.False(coordinator.Health.IsReady);
        await coordinator.CleanupAsync();
        Assert.Equal(CompositionStage.Stopped, attempt.Phase);
        Assert.Equal(CompositionOutcome.Failed, attempt.Outcome);
        Assert.Equal("composition_activation_failed", attempt.FailureCode);
        Assert.Equal(["org.test.b", "org.test.a"], attempt.CompletedOwnerStages
            .Where(entry => entry.Stage == CompositionStage.Stopping).Select(entry => entry.Owner.Value));
        Assert.DoesNotContain(beforeCleanup, entry => entry.Stage == CompositionStage.Stopping);
        Assert.Equal(["register:a", "register:b", "initialize:a", "initialize:b", "verify-all",
            "start:a", "start:b", "stop:b", "cleanup:b:2", "cleanup:b:1", "dispose:b",
            "stop:a", "cleanup:a:2", "cleanup:a:1", "dispose:a"], events);
        Assert.Throws<CompositionBoundaryException>(() => coordinator.MarkMigrated());
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(16, false)]
    [InlineData(16, true)]
    public async Task ActualOwnerResourcesAccept32And512AndRefuse33And513WithoutLosingCleanup(
        int owners, bool overflow)
    {
        var attempted = 0;
        var accepted = 0;
        var stopped = 0;
        var cleanups = new List<string>();
        var catalog = Enumerable.Range(0, owners).Select(index =>
            new ModuleDeployment(HostCompositionTestData.Manifest($"org.test.module{index:D2}"),
                createRuntime: (_, _) => new(new ResourceRuntime(context =>
                {
                    var count = overflow && index == owners - 1 ? 33 : 32;
                    for (var action = 0; action < count; action++)
                    {
                        var label = $"{index}:{action}";
                        attempted++;
                        context.Resources.RegisterCleanup(_ =>
                        {
                            cleanups.Add(label);
                            return ValueTask.CompletedTask;
                        });
                        accepted++;
                    }
                }, () => stopped++), ModuleRuntimeOwnership.Attempt))).ToArray();
        CompositionAttempt? captured = null;
        var coordinator = Create(catalog, capture: value => captured = value);
        var attempt = Assert.IsType<CompositionAttempt>(captured);
        IServiceCollection services = new ServiceCollection();
        await coordinator.RegisterAsync(services, TestContext.Current.CancellationToken);
        await using var provider = services.BuildServiceProvider();
        if (overflow)
            Assert.Equal("composition_initialization_failed", (await Assert.ThrowsAsync<CompositionBoundaryException>(() =>
                coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken))).Code);
        else await coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken);
        Assert.Equal(owners * 32, accepted);
        Assert.Equal(owners * 32 + (overflow ? 1 : 0), attempted);
        Assert.Equal(accepted, attempt.CleanupRegistrations);
        Assert.Equal(owners, attempt.EnteredOwnerStages.Count(entry => entry.Stage == CompositionStage.Initialized));
        Assert.Equal(owners - (overflow ? 1 : 0),
            attempt.CompletedOwnerStages.Count(entry => entry.Stage == CompositionStage.Initialized));
        await coordinator.CleanupAsync();
        await coordinator.CleanupAsync();
        Assert.Equal(owners, stopped);
        Assert.Equal(Enumerable.Range(0, owners).Reverse().SelectMany(owner =>
            Enumerable.Range(0, 32).Reverse().Select(action => $"{owner}:{action}")), cleanups);
    }

    [Fact]
    public async Task InitialHealthGatesPublicationAndContainerOwnedRuntimeIsDisposedOnlyByScope()
    {
        var events = new List<string>();
        var catalog = Catalog(events, containerOwned: true);
        var coordinator = Create(catalog);
        var services = new ServiceCollection();
        await coordinator.RegisterAsync(services, TestContext.Current.CancellationToken);
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        await coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken);
        coordinator.MarkMigrated();
        await coordinator.StartAsync(new Dictionary<ModuleId, IModuleStorage>(), TestContext.Current.CancellationToken);
        Assert.True(coordinator.Health.IsReady);
        Assert.Equal(["health:a", "health:b"], events.Where(e => e.StartsWith("health:", StringComparison.Ordinal)));
        await coordinator.CleanupAsync();
        Assert.False(coordinator.Health.IsReady);
        Assert.Equal(1, events.Count(e => e == "dispose:a"));
        Assert.Equal(1, events.Count(e => e == "dispose:b"));
    }

    [Theory]
    [InlineData(ModuleHealth.Unknown, "composition_health_unknown")]
    [InlineData(ModuleHealth.Degraded, "composition_health_degraded")]
    [InlineData(ModuleHealth.Failed, "composition_health_failed")]
    public async Task UnhealthyInitialSweepCannotPublishEitherTheAttemptOrTheHealthSnapshot(
        ModuleHealth health, string code)
    {
        var catalog = new[] { new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
            createRuntime: (_, _) => new(new ActivationRuntime(_ => { }, health), ModuleRuntimeOwnership.Attempt)) };
        CompositionAttempt? captured = null;
        var coordinator = Create(catalog, capture: value => captured = value);
        var attempt = Assert.IsType<CompositionAttempt>(captured);
        IServiceCollection services = new ServiceCollection();
        await coordinator.RegisterAsync(services, TestContext.Current.CancellationToken);
        await using var provider = services.BuildServiceProvider();
        await coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken);
        coordinator.MarkMigrated();
        var failure = await Assert.ThrowsAsync<CompositionBoundaryException>(() =>
            coordinator.StartAsync(new Dictionary<ModuleId, IModuleStorage>(), TestContext.Current.CancellationToken));
        Assert.Equal(code, failure.Code);
        Assert.False(attempt.Publication);
        Assert.False(coordinator.Health.Snapshot.Published);
        Assert.False(coordinator.Health.IsReady);
        await coordinator.CleanupAsync();
    }

    [Fact]
    public async Task PartialInitializationStopsOnlyEnteredOwnersAndNeverConstructsTheLaterOwner()
    {
        var events = new List<string>();
        var laterFactories = 0;
        var catalog = Catalog(events, failInitialize: true).Append(new ModuleDeployment(
            HostCompositionTestData.Manifest("org.test.c"),
            configureServices: (_, _) => events.Add("register:c"),
            createRuntime: (_, _) =>
            {
                laterFactories++;
                return new(new Runtime("c", events, false), ModuleRuntimeOwnership.Attempt);
            })).ToArray();
        var coordinator = Create(catalog);
        IServiceCollection services = new ServiceCollection();
        await coordinator.RegisterAsync(services, TestContext.Current.CancellationToken);
        await using var provider = services.BuildServiceProvider();
        var failure = await Assert.ThrowsAsync<CompositionBoundaryException>(() =>
            coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken));
        Assert.Equal("composition_initialization_failed", failure.Code);
        Assert.False(coordinator.Health.IsReady);
        var failed = coordinator.Health.Snapshot.Modules.Single(module => module.Owner.Value == "org.test.b");
        Assert.Equal(ModuleLifecycleStage.Initialized, failed.Stage);
        Assert.Equal(ModuleLifecycleOutcome.Failed, failed.Outcome);
        Assert.Equal("inspect_preserved_state", failed.Guidance);
        Assert.Equal(ModuleLifecycleStage.Registered,
            coordinator.Health.Snapshot.Modules.Single(module => module.Owner.Value == "org.test.c").Stage);
        await coordinator.CleanupAsync();
        await coordinator.CleanupAsync();
        Assert.Equal(0, laterFactories);
        Assert.Equal(["register:a", "register:b", "register:c", "initialize:a", "initialize:b",
            "stop:b", "cleanup:b:2", "cleanup:b:1", "dispose:b",
            "stop:a", "cleanup:a:2", "cleanup:a:1", "dispose:a"], events);
    }

    [Fact]
    public async Task FailedStopStillRunsLifoCleanupExactlyOnceAndLeavesReadinessWithdrawn()
    {
        var events = new List<string>();
        var catalog = new[] { new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
            createRuntime: (_, _) => new(new Runtime("a", events, false, failStop: true),
                ModuleRuntimeOwnership.Attempt)) };
        var coordinator = Create(catalog);
        IServiceCollection services = new ServiceCollection();
        await coordinator.RegisterAsync(services, TestContext.Current.CancellationToken);
        await using var provider = services.BuildServiceProvider();
        await coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken);
        coordinator.MarkMigrated();
        await coordinator.StartAsync(new Dictionary<ModuleId, IModuleStorage>(), TestContext.Current.CancellationToken);
        Assert.True(coordinator.Health.IsReady);
        var failure = await Assert.ThrowsAsync<CompositionBoundaryException>(coordinator.CleanupAsync);
        Assert.Equal("composition_cleanup_incomplete", failure.Code);
        Assert.False(coordinator.Health.IsReady);
        await coordinator.CleanupAsync();
        Assert.Equal(["stop:a", "cleanup:a:2", "cleanup:a:1", "dispose:a"],
            events.Where(value => value.StartsWith("stop:", StringComparison.Ordinal) ||
                value.StartsWith("cleanup:", StringComparison.Ordinal) || value.StartsWith("dispose:", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task DuplicateServiceOwnershipRejectsBeforeEitherRegistrarRuns()
    {
        var registrars = 0;
        var catalog = new[] { "a", "b" }.Select(name => new ModuleDeployment(
            HostCompositionTestData.Manifest("org.test." + name),
            ownedServiceTypes: [typeof(Runtime)],
            configureServices: (_, _) => registrars++)).ToArray();
        var coordinator = Create(catalog);
        IServiceCollection services = new ServiceCollection();
        var failure = await Assert.ThrowsAsync<CompositionBoundaryException>(() =>
            coordinator.RegisterAsync(services, TestContext.Current.CancellationToken));
        Assert.Equal("composition_registration_failed", failure.Code);
        Assert.Equal(0, registrars);
        Assert.Empty(services);
    }

    [Fact]
    public async Task OwnerScopesDisposeAfterTheirRuntimeAndTheHostProviderDisposesLast()
    {
        var events = new List<string>();
        var catalog = new[] { "a", "b" }.Select(name =>
        {
            var serviceType = name == "a" ? typeof(ScopeA) : typeof(ScopeB);
            return new ModuleDeployment(HostCompositionTestData.Manifest("org.test." + name),
                ownedServiceTypes: [serviceType],
                configureServices: (services, _) => services.AddScoped(serviceType,
                    _ => name == "a" ? new ScopeA(events) : new ScopeB(events)),
                createRuntime: (services, _) =>
                {
                    services.GetRequiredService(serviceType);
                    return new(new Runtime(name, events, false), ModuleRuntimeOwnership.Attempt);
                });
        }).ToArray();
        var coordinator = Create(catalog);
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(_ => new HostResource(events));
        await coordinator.RegisterAsync(services, TestContext.Current.CancellationToken);
        var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        try
        {
            _ = provider.GetRequiredService<HostResource>();
            await coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken);
            await coordinator.CleanupAsync();
            Assert.Equal(["initialize:a", "initialize:b",
                "stop:b", "cleanup:b:2", "cleanup:b:1", "dispose:b", "scope:b",
                "stop:a", "cleanup:a:2", "cleanup:a:1", "dispose:a", "scope:a"], events);
        }
        finally { await provider.DisposeAsync(); }
        Assert.Equal("provider", events[^1]);
    }

    [Theory]
    [InlineData("singleton")]
    [InlineData("remove")]
    [InlineData("replace")]
    [InlineData("undeclared")]
    [InlineData("hosted")]
    public async Task RegistrationAuditRejectsBeforeFactoriesAndDoesNotMutateHostDescriptors(string attack)
    {
        var factoryCalls = 0;
        var owned = typeof(Runtime);
        var catalog = new[] { new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
            ownedServiceTypes: [owned],
            configureServices: (services, _) =>
            {
                if (attack == "remove") services.RemoveAt(0);
                else if (attack == "replace") services[0] = ServiceDescriptor.Singleton(new HostSentinel());
                else if (attack == "singleton") services.AddSingleton<Runtime>();
                else if (attack == "undeclared") services.AddTransient<HostSentinel>();
                else services.AddSingleton<Microsoft.Extensions.Hosting.IHostedService, Hosted>();
            }, createRuntime: (_, _) => { factoryCalls++; throw new InvalidOperationException(); }) };
        var coordinator = Create(catalog);
        IServiceCollection services = new ServiceCollection();
        var sentinel = ServiceDescriptor.Singleton(new HostSentinel());
        services.Add(sentinel);
        var failure = await Assert.ThrowsAsync<CompositionBoundaryException>(() =>
            coordinator.RegisterAsync(services, TestContext.Current.CancellationToken));
        Assert.Equal("composition_registration_failed", failure.Code);
        Assert.Equal(0, factoryCalls);
        Assert.Same(sentinel, services[0]);
        Assert.Single(services);
    }

    [Fact]
    public async Task RuntimeFactoryCannotResolveGlobalAuthorityOrOtherOwnerServices()
    {
        var deployment = new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
            ownedServiceTypes: [typeof(Runtime)],
            createRuntime: (provider, _) =>
            {
                Assert.Null(provider.GetService<IStorageRegistry>());
                Assert.Null(provider.GetService<HostSentinel>());
                Assert.Same(provider, provider.GetService<IServiceProvider>());
                return new(new Runtime("a", [], false), ModuleRuntimeOwnership.Attempt);
            });
        var coordinator = Create([deployment]);
        var services = new ServiceCollection();
        services.AddSingleton<HostSentinel>();
        await coordinator.RegisterAsync(services, TestContext.Current.CancellationToken);
        await using var provider = services.BuildServiceProvider();
        await coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken);
        await coordinator.CleanupAsync();
    }

    [Fact]
    public async Task NativeConstructorInjectionCanEnumerateOwnServicesButNotHostServices()
    {
        var catalog = new[] { new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
            ownedServiceTypes: [typeof(IOperations), typeof(OperationSet)],
            configureServices: (services, _) =>
            {
                services.AddScoped<IOperations, Operations>();
                services.AddScoped<IOperations, Operations>();
                services.AddScoped<OperationSet>();
            },
            createRuntime: (services, _) =>
            {
                Assert.Equal(2, services.GetRequiredService<OperationSet>().Count);
                Assert.Null(services.GetService<IEnumerable<HostSentinel>>());
                return new(new Runtime("a", [], false), ModuleRuntimeOwnership.Attempt);
            }) };
        var coordinator = Create(catalog);
        IServiceCollection services = new ServiceCollection();
        services.AddSingleton<HostSentinel>();
        await coordinator.RegisterAsync(services, TestContext.Current.CancellationToken);
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        await coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken);
        await coordinator.CleanupAsync();
    }

    [Fact]
    public async Task ContainerRuntimeResolvedThroughItsOwnedEnumerableIsDisposedOnlyByTheScope()
    {
        var events = new List<string>();
        var catalog = new[] { new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
            ownedServiceTypes: [typeof(RuntimeA)],
            configureServices: (services, _) => services.AddScoped(_ => new RuntimeA(events)),
            createRuntime: (services, _) => new(services.GetServices<RuntimeA>().Single(), ModuleRuntimeOwnership.Container)) };
        var coordinator = Create(catalog);
        IServiceCollection services = new ServiceCollection();
        await coordinator.RegisterAsync(services, TestContext.Current.CancellationToken);
        await using var provider = services.BuildServiceProvider();
        await coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken);
        await coordinator.CleanupAsync();
        Assert.Equal(1, events.Count(item => item == "dispose:a"));
    }

    [Theory]
    [InlineData(10, 117)]
    [InlineData(10, 118)]
    [InlineData(10, 128)]
    [InlineData(253, 128)]
    public async Task DeclaredPrerequisiteOperationIsBoundFromItsOwnScopeWithItsAcceptedVersion(
        int ownerLength, int localLength)
    {
        var activated = false;
        var owner = ownerLength == 10 ? "org.test.a" :
            string.Join(".", new string('a', 63), new string('b', 63), new string('c', 63), new string('d', 61));
        var local = new string('s', localLength);
        var alphaManifest = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(
            HostCompositionTestData.Manifest(owner)).Replace("\"capabilities\":[]",
                "\"capabilities\":[{\"kind\":\"backend\",\"contractVersion\":\"1.0.0\"}]", StringComparison.Ordinal));
        var alpha = new ModuleDeployment(alphaManifest,
            claims: [new(owner, "backend-service", local, "1.0.0",
                ContributionMode.Executable, ServiceType: typeof(IOperations))],
            ownedServiceTypes: [typeof(IOperations)],
            configureServices: (services, _) => services.AddScoped<IOperations, Operations>(),
            createRuntime: (_, _) => new(new Runtime("a", [], false), ModuleRuntimeOwnership.Attempt));
        var beta = new ModuleDeployment(HostCompositionTestData.Manifest("org.test.b", owner),
            createRuntime: (provider, _) =>
            {
                Assert.Null(provider.GetService<IOperations>());
                return new(new ActivationRuntime(context =>
                {
                    var binding = Assert.Single(context.Dependencies);
                    Assert.Equal(owner, binding.Owner.Value);
                    Assert.Equal(owner + "." + local, binding.ContractId);
                    Assert.Equal(ownerLength + 1 + localLength, binding.ContractId.Length);
                    Assert.Equal(1, binding.ContractVersion);
                    Assert.Equal("1.0.0", binding.Version.ToString());
                    Assert.Equal("owner-only-operation", Assert.IsType<Operations>(binding.Operations).Read());
                    activated = true;
                }), ModuleRuntimeOwnership.Attempt);
            });
        var coordinator = Create([beta, alpha]);
        IServiceCollection services = new ServiceCollection();
        await coordinator.RegisterAsync(services, TestContext.Current.CancellationToken);
        await using var provider = services.BuildServiceProvider();
        await coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken);
        coordinator.MarkMigrated();
        await coordinator.StartAsync(new Dictionary<ModuleId, IModuleStorage>(), TestContext.Current.CancellationToken);
        Assert.True(activated);
        Assert.True(coordinator.Health.IsReady);
        await coordinator.CleanupAsync();
    }

    [Fact]
    public async Task InvalidAttemptOwnershipCannotDoubleDisposeAnActuallyContainerOwnedRuntime()
    {
        var events = new List<string>();
        var deployment = new ModuleDeployment(HostCompositionTestData.Manifest("org.test.a"),
            ownedServiceTypes: [typeof(Runtime)],
            configureServices: (services, _) => services.AddScoped(_ => new Runtime("a", events, false)),
            createRuntime: (provider, _) => new(provider.GetRequiredService<Runtime>(), ModuleRuntimeOwnership.Attempt));
        var coordinator = Create([deployment]);
        IServiceCollection services = new ServiceCollection();
        await coordinator.RegisterAsync(services, TestContext.Current.CancellationToken);
        await using var provider = services.BuildServiceProvider();
        await Assert.ThrowsAsync<CompositionBoundaryException>(() =>
            coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken));
        await coordinator.CleanupAsync();
        Assert.Equal(["dispose:a"], events);
    }

    [Theory]
    [InlineData(65, 1, true)]
    [InlineData(128, 1, true)]
    [InlineData(128, 2, false)]
    [InlineData(128, 8, true)]
    public async Task CompleteExecutableCatalogSurvivesRegistrationBindingStartAndReadiness(
        int servicesPerOwner, int roots, bool dependent)
    {
        var expected = new Dictionary<string, object>(StringComparer.Ordinal);
        var entered = new List<string>();
        var catalog = Enumerable.Range(0, roots).Select(root =>
        {
            var owner = $"org.test.root{root}";
            var types = Enumerable.Range(root * servicesPerOwner, servicesPerOwner)
                .Select(ExportType).ToArray();
            var claims = types.Select((type, index) => new ContributionClaim(owner, "backend-service", $"service{index}",
                Mode: ContributionMode.Executable, ServiceType: type)).ToArray();
            var operations = types.Select((type, index) => Activator.CreateInstance(type, root * servicesPerOwner + index)!)
                .ToArray();
            for (var index = 0; index < operations.Length; index++)
                expected.Add($"{owner}.service{index}", operations[index]);
            var manifest = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(
                HostCompositionTestData.Manifest(owner)).Replace("\"capabilities\":[]",
                    "\"capabilities\":[{\"kind\":\"backend\",\"contractVersion\":\"1.0.0\"}]", StringComparison.Ordinal));
            return new ModuleDeployment(manifest, claims, ownedServiceTypes: types,
                configureServices: (services, _) =>
                {
                    for (var index = 0; index < types.Length; index++)
                    {
                        var operation = operations[index];
                        services.AddScoped(types[index], _ => operation);
                    }
                },
                createRuntime: (_, _) => new(new ActivationRuntime(context =>
                {
                    Assert.Empty(context.Dependencies);
                    Assert.Equal(servicesPerOwner, context.Contributions.Count);
                    entered.Add(owner);
                }), ModuleRuntimeOwnership.Attempt));
        }).ToList();
        if (dependent)
            catalog.Add(new(HostCompositionTestData.Manifest("org.test.dependent",
                Enumerable.Range(0, roots).Select(root => $"org.test.root{root}").ToArray()),
                createRuntime: (provider, _) => new(new ActivationRuntime(context =>
                {
                    Assert.Equal(expected.Count, context.Dependencies.Count);
                    Assert.Equal(expected.Keys.Order(StringComparer.Ordinal),
                        context.Dependencies.Select(binding => binding.ContractId).Order(StringComparer.Ordinal));
                    foreach (var binding in context.Dependencies)
                    {
                        Assert.Equal(binding.Owner.Value + ".", binding.ContractId[..(binding.Owner.Value.Length + 1)]);
                        Assert.Equal(1, binding.ContractVersion);
                        Assert.Equal("1.0.0", binding.Version.ToString());
                        Assert.Same(expected[binding.ContractId], binding.Operations);
                        Assert.Null(provider.GetService(binding.Operations.GetType()));
                    }
                    Assert.Equal(Enumerable.Range(0, expected.Count),
                        context.Dependencies.Select(binding => ((IExportOperation)binding.Operations).Read()).Order());
                    entered.Add(context.Owner.Value);
                }), ModuleRuntimeOwnership.Attempt)));
        var coordinator = Create(catalog.ToArray());
        IServiceCollection services = new ServiceCollection();
        var token = TestContext.Current.CancellationToken;
        await coordinator.RegisterAsync(services, token);
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        { ValidateScopes = true, ValidateOnBuild = true });
        await coordinator.InitializeAsync(provider, token);
        coordinator.MarkMigrated();
        await coordinator.StartAsync(new Dictionary<ModuleId, IModuleStorage>(), token);
        Assert.True(coordinator.Health.IsReady);
        Assert.Equal(catalog.Count, entered.Count);
        Assert.Equal(catalog.Count, coordinator.Health.Snapshot.Modules.Count(module =>
            module.EffectiveHealth == ModuleHealth.Healthy));
        if (dependent) Assert.Equal("org.test.dependent", entered[^1]);
        await coordinator.CleanupAsync();
        Assert.False(coordinator.Health.IsReady);
    }

    private static Type ExportType(int index)
    {
        var marker = typeof(ExportEnd);
        do
        {
            marker = (index % 2 == 0 ? typeof(ExportZero<>) : typeof(ExportOne<>)).MakeGenericType(marker);
            index /= 2;
        } while (index != 0);
        return typeof(ExportOperation<>).MakeGenericType(marker);
    }
    private sealed class ExportEnd;
    private sealed class ExportZero<T>;
    private sealed class ExportOne<T>;
    private interface IExportOperation { int Read(); }
    private sealed class ExportOperation<T>(int value) : IExportOperation
    {
        public int Read() => value;
    }

    [Theory]
    [InlineData(129, 1)]
    [InlineData(128, 9)]
    public void ExecutableOverflowIsRejectedBeforeAnyRegistrarOrRuntime(int perOwner, int owners)
    {
        var callbacks = 0;
        var catalog = Enumerable.Range(0, owners).Select(root =>
        {
            var count = root == 8 ? 1 : perOwner;
            var owner = $"org.test.root{root}";
            var types = Enumerable.Range(root * perOwner, count).Select(ExportType).ToArray();
            var manifest = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(
                HostCompositionTestData.Manifest(owner)).Replace("\"capabilities\":[]",
                    "\"capabilities\":[{\"kind\":\"backend\",\"contractVersion\":\"1.0.0\"}]", StringComparison.Ordinal));
            return new ModuleDeployment(manifest, types.Select((type, index) =>
                new ContributionClaim(owner, "backend-service", $"service{index}",
                    Mode: ContributionMode.Executable, ServiceType: type)), ownedServiceTypes: types,
                configureServices: (_, _) => callbacks++,
                createRuntime: (_, _) => { callbacks++; throw new InvalidOperationException(); });
        }).ToArray();
        var resolution = CompositionResolver.Resolve(catalog,
            Enumerable.Range(0, owners).Select(root => new ModuleSelection($"org.test.root{root}", true)),
            new("1.0.0"), TestContext.Current.CancellationToken);
        var rejected = Assert.IsType<CompositionResolution.Rejected>(resolution);
        Assert.Contains(rejected.Diagnostics, diagnostic => diagnostic.Code == "composition_input_limit");
        Assert.Equal(0, callbacks);
    }

    [Theory]
    [InlineData("registration")]
    [InlineData("initialization")]
    [InlineData("activation")]
    [InlineData("health")]
    public async Task LostAuthorityBlocksTheNextPhaseAndWithdrawsPreviouslyPublishedReadiness(string phase)
    {
        var events = new List<string>();
        var lost = phase == "registration";
        var coordinator = Create(Catalog(events), _ => lost
            ? throw new InvalidOperationException("session-lost") : Task.CompletedTask);
        IServiceCollection services = new ServiceCollection();
        if (phase == "registration")
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                coordinator.RegisterAsync(services, TestContext.Current.CancellationToken));
            Assert.Empty(events);
            await coordinator.CleanupAsync();
            return;
        }
        await coordinator.RegisterAsync(services, TestContext.Current.CancellationToken);
        await using var provider = services.BuildServiceProvider();
        lost = phase == "initialization";
        if (lost)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken));
            Assert.DoesNotContain(events, e => e.StartsWith("initialize:", StringComparison.Ordinal));
        }
        else
        {
            await coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken);
            coordinator.MarkMigrated();
            lost = phase == "activation";
            if (lost)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    coordinator.StartAsync(new Dictionary<ModuleId, IModuleStorage>(), TestContext.Current.CancellationToken));
                Assert.DoesNotContain(events, e => e.StartsWith("start:", StringComparison.Ordinal));
            }
            else
            {
                await coordinator.StartAsync(new Dictionary<ModuleId, IModuleStorage>(), TestContext.Current.CancellationToken);
                Assert.True(coordinator.Health.IsReady);
                var previousCalls = events.Count(e => e.StartsWith("health:", StringComparison.Ordinal));
                lost = true;
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    coordinator.SweepAsync(TestContext.Current.CancellationToken));
                Assert.Equal(previousCalls, events.Count(e => e.StartsWith("health:", StringComparison.Ordinal)));
            }
        }
        Assert.False(coordinator.Health.IsReady);
        await coordinator.CleanupAsync();
    }

    [Fact]
    public async Task HealthyServingContinuesBoundedAuthorityChecksAfterStartupBudgetExpires()
    {
        var clock = new Clock();
        var checks = 0;
        var coordinator = Create(Catalog([]), _ => { checks++; return Task.CompletedTask; }, clock);
        IServiceCollection services = new ServiceCollection();
        await coordinator.RegisterAsync(services, TestContext.Current.CancellationToken);
        await using var provider = services.BuildServiceProvider();
        await coordinator.InitializeAsync(provider, TestContext.Current.CancellationToken);
        coordinator.MarkMigrated();
        await coordinator.StartAsync(new Dictionary<ModuleId, IModuleStorage>(), TestContext.Current.CancellationToken);
        var previous = checks;
        clock.Advance(TimeSpan.FromMinutes(2));
        await coordinator.SweepAsync(TestContext.Current.CancellationToken);
        Assert.True(checks > previous);
        Assert.True(coordinator.Health.IsReady);
        await coordinator.CleanupAsync();
    }

    private static ModuleDeployment[] Catalog(List<string> events, bool failStart = false, bool containerOwned = false,
        bool failInitialize = false) =>
        new[] { "a", "b" }.Select(name => new ModuleDeployment(
            HostCompositionTestData.Manifest("org.test." + name, name == "b" ? ["org.test.a"] : []),
            ownedServiceTypes: [name == "a" ? typeof(RuntimeA) : typeof(RuntimeB)],
            configureServices: (services, context) =>
            {
                events.Add("register:" + name);
                if (containerOwned) services.AddScoped(name == "a" ? typeof(RuntimeA) : typeof(RuntimeB),
                    _ => name == "a" ? new RuntimeA(events) : new RuntimeB(events, failStart));
            },
            createRuntime: (provider, _) => new(containerOwned
                ? (IModuleRuntime)provider.GetRequiredService(name == "a" ? typeof(RuntimeA) : typeof(RuntimeB))
                : new Runtime(name, events, failStart && name == "b", failInitialize && name == "b"),
                containerOwned ? ModuleRuntimeOwnership.Container : ModuleRuntimeOwnership.Attempt))).ToArray();

    private static ModuleLifecycleCoordinator Create(ModuleDeployment[] catalog,
        Func<CancellationToken, Task>? ensureAuthority = null, TimeProvider? clock = null,
        Action<CompositionAttempt>? capture = null, Action<CompositionRecord>? record = null)
    {
        var descriptions = catalog.Select(d => Assert.IsType<ManifestValidationResult.Accepted>(
            ManifestValidator.Validate(d.Manifest.Span, new("1.0.0"))).Description).ToArray();
        var composition = Assert.IsType<CompositionResolution.Accepted>(CompositionResolver.Resolve(catalog,
            descriptions.Select(d => new ModuleSelection(d.Id.Value, true)), new("1.0.0"))).Composition;
        var diagnostics = new CompositionDiagnostics(CompositionMode.Serve, "none", record ?? (_ => { }), clock);
        var attempt = new CompositionAttempt(diagnostics, clock: clock);
        capture?.Invoke(attempt);
        return new(composition, descriptions.Select((d, i) => (d.Id, Deployment: catalog[i])).ToDictionary(
            p => p.Id, p => p.Deployment),
            descriptions.ToDictionary(d => d.Id, d => new ModuleConfiguration(d.Id, "{}"u8)),
            attempt, diagnostics, ensureAuthority: ensureAuthority, clock: clock);
    }

    private sealed class Clock : TimeProvider
    {
        private long time;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => time;
        public void Advance(TimeSpan elapsed) => time += elapsed.Ticks;
    }

    private sealed class HostSentinel;
    private sealed class HostResource(List<string> events) : IDisposable
    {
        public void Dispose() => events.Add("provider");
    }
    private abstract class ScopeResource(List<string> events, string owner) : IDisposable
    {
        public void Dispose() => events.Add("scope:" + owner);
    }
    private sealed class ScopeA(List<string> events) : ScopeResource(events, "a");
    private sealed class ScopeB(List<string> events) : ScopeResource(events, "b");
    private sealed class ResourceRuntime(Action<ModuleInitializationContext> initialize, Action stop) : IModuleRuntime
    {
        public ValueTask InitializeAsync(ModuleInitializationContext context, CancellationToken token)
        {
            initialize(context);
            return ValueTask.CompletedTask;
        }
        public ValueTask StartAsync(ModuleActivationContext context, CancellationToken token) => ValueTask.CompletedTask;
        public ValueTask<ModuleHealthResult> CheckHealthAsync(CancellationToken token) =>
            ValueTask.FromResult(new ModuleHealthResult(ModuleHealth.Healthy));
        public ValueTask StopAsync(CancellationToken token)
        {
            stop();
            return ValueTask.CompletedTask;
        }
    }
    private sealed class HealthRuntime(Func<CancellationToken, ValueTask<ModuleHealthResult>> check) : IModuleRuntime
    {
        public ValueTask InitializeAsync(ModuleInitializationContext context, CancellationToken token) => ValueTask.CompletedTask;
        public ValueTask StartAsync(ModuleActivationContext context, CancellationToken token) => ValueTask.CompletedTask;
        public ValueTask<ModuleHealthResult> CheckHealthAsync(CancellationToken token) => check(token);
        public ValueTask StopAsync(CancellationToken token) => ValueTask.CompletedTask;
    }
    private interface IOperations { string Read(); }
    private sealed class Operations : IOperations
    {
        public string Read() => "owner-only-operation";
    }
    private sealed class OperationSet(IEnumerable<IOperations> operations)
    {
        public int Count { get; } = operations.Count();
    }
    private sealed class ActivationRuntime(Action<ModuleActivationContext> activate,
        ModuleHealth health = ModuleHealth.Healthy) : IModuleRuntime
    {
        public ValueTask InitializeAsync(ModuleInitializationContext context, CancellationToken token) => ValueTask.CompletedTask;
        public ValueTask StartAsync(ModuleActivationContext context, CancellationToken token)
        {
            activate(context);
            return ValueTask.CompletedTask;
        }
        public ValueTask<ModuleHealthResult> CheckHealthAsync(CancellationToken token) =>
            ValueTask.FromResult(new ModuleHealthResult(health));
        public ValueTask StopAsync(CancellationToken token) => ValueTask.CompletedTask;
    }
    private sealed class Hosted : Microsoft.Extensions.Hosting.IHostedService
    {
        public Task StartAsync(CancellationToken token) => Task.CompletedTask;
        public Task StopAsync(CancellationToken token) => Task.CompletedTask;
    }

    private sealed class RuntimeA(List<string> events) : Runtime("a", events, false);
    private sealed class RuntimeB(List<string> events, bool failStart) : Runtime("b", events, failStart);
    private class Runtime(string name, List<string> events, bool failStart, bool failInitialize = false, bool failStop = false)
        : IModuleRuntime, IAsyncDisposable
    {
        public ValueTask InitializeAsync(ModuleInitializationContext context, CancellationToken token)
        {
            events.Add("initialize:" + name);
            context.Resources.RegisterCleanup(_ => { events.Add("cleanup:" + name + ":1"); return ValueTask.CompletedTask; });
            context.Resources.RegisterCleanup(_ => { events.Add("cleanup:" + name + ":2"); return ValueTask.CompletedTask; });
            if (failInitialize) throw new InvalidOperationException("private-initialization-error");
            return ValueTask.CompletedTask;
        }
        public ValueTask StartAsync(ModuleActivationContext context, CancellationToken token)
        {
            events.Add("start:" + name);
            if (failStart) throw new InvalidOperationException("private-error");
            return ValueTask.CompletedTask;
        }
        public ValueTask<ModuleHealthResult> CheckHealthAsync(CancellationToken token)
        {
            events.Add("health:" + name);
            return ValueTask.FromResult(new ModuleHealthResult(ModuleHealth.Healthy));
        }
        public ValueTask StopAsync(CancellationToken token)
        {
            events.Add("stop:" + name);
            if (failStop) throw new InvalidOperationException("private-stop-error");
            return ValueTask.CompletedTask;
        }
        public ValueTask DisposeAsync() { events.Add("dispose:" + name); return ValueTask.CompletedTask; }
    }
}
