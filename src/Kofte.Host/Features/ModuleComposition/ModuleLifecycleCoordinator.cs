using Kofte.Infrastructure.Composition;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Host.Features.ModuleComposition;

public sealed record ModuleDependencyExport(ModuleId Owner, string ContractId, SemanticVersion Version, Type ServiceType);

public sealed class ModuleLifecycleCoordinator
{
    private sealed class Resources(ModuleId owner, Func<bool> admit) : ModuleAttemptResources(owner)
    {
        public Stack<Func<CancellationToken, ValueTask>> Cleanups { get; } = new();
        public bool Closed { get; set; }
        public override void RegisterCleanup(Func<CancellationToken, ValueTask> cleanup)
        {
            ArgumentNullException.ThrowIfNull(cleanup);
            if (Closed || Cleanups.Count >= 32 || !admit())
                throw new CompositionBoundaryException("composition_cleanup_incomplete");
            Cleanups.Push(cleanup);
        }
    }

    private sealed class Registration(ModuleId owner, ModuleConfiguration configuration, string attempt)
        : ModuleRegistrationContext(owner, configuration, attempt);
    private sealed class RuntimeContext(ModuleId owner, ModuleConfiguration configuration, string attempt, Resources resources)
        : ModuleRuntimeContext(owner, configuration, attempt, resources);
    private sealed class Initialization(ModuleId owner, ModuleConfiguration configuration, string attempt, Resources resources)
        : ModuleInitializationContext(owner, configuration, attempt, resources);
    private sealed class Activation(ModuleId owner, ModuleConfiguration configuration, string attempt, Resources resources,
        IModuleStorage? storage, IEnumerable<ModuleDependencyBinding> dependencies,
        IEnumerable<ContributionReservation> contributions)
        : ModuleActivationContext(owner, configuration, attempt, resources, storage, dependencies, contributions);

    private sealed class OwnerServices(IServiceProvider provider, IReadOnlyCollection<Type> allowed)
        : IServiceProvider, IServiceProviderIsService
    {
        public HashSet<object> Resolved { get; } = new(ReferenceEqualityComparer.Instance);
        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IServiceProvider) || serviceType == typeof(IServiceProviderIsService)) return this;
            if (!Allowed(serviceType)) return null;
            var service = provider.GetService(serviceType);
            if (service is not null) Resolved.Add(service);
            if (serviceType.IsConstructedGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                service is System.Collections.IEnumerable instances)
                foreach (var instance in instances)
                    if (instance is not null) Resolved.Add(instance);
            return service;
        }
        public bool IsService(Type type) => Allowed(type) || type == typeof(IServiceProvider) ||
            type == typeof(IServiceProviderIsService);
        private bool Allowed(Type type) => allowed.Contains(type) ||
            type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
            allowed.Contains(type.GetGenericArguments()[0]);
    }

    private sealed class OwnerRuntime(ModuleId owner, AsyncServiceScope scope, OwnerServices services, Resources resources)
    {
        public ModuleId Owner { get; } = owner;
        public AsyncServiceScope Scope { get; } = scope;
        public OwnerServices Services { get; } = services;
        public Resources Resources { get; } = resources;
        public ModuleRuntimeInstance? Instance { get; set; }
        public bool ContainerOwnsRuntime { get; set; }
        public bool Entered { get; set; }
        public bool Stopped { get; set; }
    }

    private readonly ResolvedComposition composition;
    private readonly IReadOnlyDictionary<ModuleId, ModuleDeployment> deployments;
    private readonly IReadOnlyDictionary<ModuleId, ModuleConfiguration> configurations;
    private readonly CompositionAttempt attempt;
    private readonly CompositionDiagnostics diagnostics;
    private readonly CompositionCallbackRunner callbacks;
    private readonly IReadOnlyList<ModuleDependencyExport> exports;
    private readonly Func<CancellationToken, Task> ensureAuthority;
    private readonly TimeProvider clock;
    private readonly List<OwnerRuntime> runtimes = new();
    private bool registered, initialized, migrated, cleaned, storageObserved;
    private readonly SemaphoreSlim healthGate = new(1, 1);
    private bool healthUnstopped;
    public CompositionHealth Health { get; }

    public ModuleLifecycleCoordinator(ResolvedComposition composition,
        IReadOnlyDictionary<ModuleId, ModuleDeployment> deployments,
        IReadOnlyDictionary<ModuleId, ModuleConfiguration> configurations,
        CompositionAttempt attempt, CompositionDiagnostics diagnostics,
        IReadOnlyList<ModuleDependencyExport>? exports = null,
        Func<CancellationToken, Task>? ensureAuthority = null, TimeProvider? clock = null)
    {
        (this.composition, this.deployments, this.configurations, this.attempt, this.diagnostics) =
            (composition, deployments.ToDictionary(p => p.Key, p => p.Value),
                configurations.ToDictionary(p => p.Key, p => p.Value), attempt, diagnostics);
        callbacks = new(attempt);
        this.ensureAuthority = ensureAuthority ?? (_ => Task.CompletedTask);
        this.clock = clock ?? TimeProvider.System;
        Health = new(composition, attempt.TraceId, this.clock);
        this.exports = Array.AsReadOnly((exports ?? composition.Contributions
            .Where(c => c.Kind == "backend-service" && c.Mode == ContributionMode.Executable)
            .Select(c => new ModuleDependencyExport(c.Owner, c.Owner.Value + "." + c.LocalName, c.Version,
                deployments[c.Owner].Claims.Single(claim => claim.Kind == "backend-service" &&
                    claim.LocalName == c.LocalName).ServiceType!)).ToArray()).Take(129).ToArray());
    }

    public async Task RegisterAsync(IServiceCollection services, CancellationToken token)
    {
        if (registered || cleaned) throw Boundary();
        attempt.EnterPhase(CompositionStage.Registered);
        var declared = new HashSet<Type>();
        foreach (var owner in composition.Order)
            foreach (var type in deployments[owner].OwnedServiceTypes)
                if (!AllowedType(type) || !declared.Add(type)) throw RegistrationFailed();
        if (exports.Count > 128 || exports.Any(e => !composition.Enabled.Contains(e.Owner) ||
            !deployments[e.Owner].OwnedServiceTypes.Contains(e.ServiceType)) ||
            exports.Select(e => (e.Owner, e.ContractId)).Distinct().Count() != exports.Count)
            throw RegistrationFailed();
        foreach (var owner in composition.Order)
        {
            await VerifyAuthorityAsync(token);
            var deployment = deployments[owner];
            var before = services.ToArray();
            try
            {
                await callbacks.RunAsync(CompositionWork.Registrar, _ =>
                {
                    attempt.EnterOwnerStage(owner, CompositionStage.Registered);
                    Health.TransitionOwner(owner, ModuleLifecycleStage.Registered, ModuleLifecycleOutcome.Pending,
                        ModuleObservationCode.Registered);
                    deployment.ConfigureServices?.Invoke(services, new Registration(owner, configurations[owner], attempt.TraceId));
                    return ValueTask.CompletedTask;
                }, token, owner);
                if (services.Count < before.Length || !before.Select((d, i) => ReferenceEquals(d, services[i])).All(v => v))
                    throw RegistrationFailed();
                for (var index = before.Length; index < services.Count; index++)
                {
                    var descriptor = services[index];
                    if (descriptor.IsKeyedService || descriptor.Lifetime == ServiceLifetime.Singleton ||
                        !deployment.OwnedServiceTypes.Contains(descriptor.ServiceType) ||
                        before.Any(d => d.ServiceType == descriptor.ServiceType))
                        throw RegistrationFailed();
                    var implementation = descriptor.ImplementationType;
                    var factory = descriptor.ImplementationFactory;
                    if (implementation is null && factory is null) throw RegistrationFailed();
                    services[index] = ServiceDescriptor.Describe(descriptor.ServiceType, provider =>
                    {
                        var scoped = new OwnerServices(provider, deployment.OwnedServiceTypes);
                        return factory is not null ? factory(scoped) : ActivatorUtilities.CreateInstance(scoped, implementation!);
                    }, descriptor.Lifetime);
                }
                attempt.CompleteOwnerStage(owner, CompositionStage.Registered);
                Health.TransitionOwner(owner, ModuleLifecycleStage.Registered, ModuleLifecycleOutcome.Completed,
                    ModuleObservationCode.Registered);
            }
            catch (CompositionUnstoppedException) { throw; }
            catch (Exception error)
            {
                Health.TransitionOwner(owner, ModuleLifecycleStage.Registered, ModuleLifecycleOutcome.Failed,
                    ModuleObservationCode.Failed);
                services.Clear();
                foreach (var descriptor in before) services.Add(descriptor);
                if (error is CompositionBoundaryException boundary && boundary.Code is "composition_deadline_exceeded" or
                    "composition_cancelled" or "composition_output_failed" or "composition_output_limit")
                    throw;
                throw RegistrationFailed();
            }
            await VerifyAuthorityAsync(token);
            diagnostics.Emit(CompositionStage.Registered, CompositionOutcome.Passed, "composition_progress", owner);
        }
        await VerifyAuthorityAsync(token);
        registered = true;
        attempt.CompletePhase(CompositionStage.Registered);
        Health.Transition(ModuleLifecycleStage.Registered);
    }

    public async Task InitializeAsync(IServiceProvider provider, CancellationToken token)
    {
        if (!registered || initialized || cleaned) throw Boundary();
        attempt.EnterPhase(CompositionStage.Initialized);
        foreach (var owner in composition.Order)
        {
            await VerifyAuthorityAsync(token);
            try
            {
                var deployment = deployments[owner];
                var scope = provider.CreateAsyncScope();
                var services = new OwnerServices(scope.ServiceProvider, deployment.OwnedServiceTypes);
                var resources = new Resources(owner, attempt.AdmitCleanup);
                var runtime = new OwnerRuntime(owner, scope, services, resources);
                runtimes.Add(runtime);
                await callbacks.RunAsync(CompositionWork.Initialize, async cancellation =>
                {
                    runtime.Instance = deployment.CreateRuntime?.Invoke(services,
                        new RuntimeContext(owner, configurations[owner], attempt.TraceId, resources))
                        ?? throw new InvalidOperationException();
                    runtime.ContainerOwnsRuntime = services.Resolved.Contains(runtime.Instance.Runtime);
                    if (runtime.ContainerOwnsRuntime != (runtime.Instance.Ownership == ModuleRuntimeOwnership.Container))
                        throw new InvalidOperationException();
                    runtime.Entered = true;
                    attempt.EnterOwnerStage(owner, CompositionStage.Initialized);
                    Health.TransitionOwner(owner, ModuleLifecycleStage.Initialized, ModuleLifecycleOutcome.Pending,
                        ModuleObservationCode.Initialized);
                    await runtime.Instance.Runtime.InitializeAsync(new Initialization(owner, configurations[owner],
                        attempt.TraceId, resources), cancellation);
                    attempt.CompleteOwnerStage(owner, CompositionStage.Initialized);
                    Health.TransitionOwner(owner, ModuleLifecycleStage.Initialized, ModuleLifecycleOutcome.Completed,
                        ModuleObservationCode.Initialized);
                }, token, owner);
            }
            catch (CompositionUnstoppedException) { throw; }
            catch (CompositionBoundaryException error) when (error.Code is "composition_deadline_exceeded" or
                "composition_cancelled" or "composition_output_failed" or "composition_output_limit")
            { throw; }
            catch (Exception)
            {
                Health.TransitionOwner(owner, ModuleLifecycleStage.Initialized, ModuleLifecycleOutcome.Failed,
                    ModuleObservationCode.Failed);
                attempt.FailPhase(CompositionStage.Initialized, CompositionOutcome.Failed, "composition_initialization_failed");
                throw new CompositionBoundaryException("composition_initialization_failed");
            }
            await VerifyAuthorityAsync(token);
            diagnostics.Emit(CompositionStage.Initialized, CompositionOutcome.Passed, "composition_progress", owner);
        }
        await VerifyAuthorityAsync(token);
        initialized = true;
        attempt.CompletePhase(CompositionStage.Initialized);
        Health.Transition(ModuleLifecycleStage.Initialized);
    }

    public void MarkMigrated()
    {
        var completed = attempt.CompletedOwnerStages;
        if (!initialized || migrated || cleaned || storageObserved &&
            composition.Order.Any(owner => !completed.Contains(new CompositionOwnerStage(owner, CompositionStage.Migrated))))
            throw Boundary();
        migrated = true;
        attempt.CompletePhase(CompositionStage.Migrated);
        Health.Transition(ModuleLifecycleStage.Migrated);
    }

    public void ObserveStorage(CompositionStorageProgress progress)
    {
        if (!initialized || migrated || cleaned || !composition.Enabled.Contains(progress.Owner) ||
            !Enum.IsDefined(progress.Transition) ||
            progress.Result is { } result && result.ModuleId != progress.Owner.Value)
            throw Boundary();
        if (progress.Transition == CompositionStorageTransition.Entered)
        {
            storageObserved = true;
            attempt.EnterOwnerStage(progress.Owner, CompositionStage.Migrated);
            Health.TransitionOwner(progress.Owner, ModuleLifecycleStage.Migrated, ModuleLifecycleOutcome.Pending,
                ModuleObservationCode.Migrated);
            return;
        }
        if (progress.Transition == CompositionStorageTransition.Completed)
        {
            if (progress.Result is not (null or StorageResult.Ready or StorageResult.NoStorage))
                throw Boundary();
            attempt.CompleteOwnerStage(progress.Owner, CompositionStage.Migrated);
            Health.TransitionOwner(progress.Owner, ModuleLifecycleStage.Migrated, ModuleLifecycleOutcome.Completed,
                ModuleObservationCode.Migrated);
            diagnostics.Emit(CompositionStage.Migrated, CompositionOutcome.Passed, "composition_progress", progress.Owner,
                storageCorrelation: progress.Result?.CorrelationId,
                durability: progress.Result?.Durability.ToString().ToLowerInvariant());
            return;
        }
        if (progress.Result is null or StorageResult.Ready or StorageResult.NoStorage)
            throw Boundary();
        var failure = new CompositionFailure("composition_reconciliation_required",
            CompositionOutcome.ReconciliationRequired, 4, progress.Result);
        attempt.FailPhase(CompositionStage.Migrated, failure.Outcome, failure.Code);
        Health.TransitionOwner(progress.Owner, ModuleLifecycleStage.Migrated, ModuleLifecycleOutcome.Failed,
            ModuleObservationCode.ReconciliationRequired);
        diagnostics.Emit(CompositionStage.Migrated, failure.Outcome, failure.Code, progress.Owner,
            storageCorrelation: progress.Result?.CorrelationId,
            durability: progress.Result?.Durability.ToString().ToLowerInvariant());
    }

    public async Task StartAsync(IReadOnlyDictionary<ModuleId, IModuleStorage> stores, CancellationToken token)
    {
        if (!migrated || attempt.Mode != CompositionMode.Serve || cleaned) throw Boundary();
        attempt.EnterPhase(CompositionStage.Started);
        foreach (var runtime in runtimes)
        {
            await VerifyAuthorityAsync(token);
            try
            {
                await callbacks.RunAsync(CompositionWork.Start, async cancellation =>
                {
                    var prerequisites = composition.Dependencies.Where(d => d.Owner.Equals(runtime.Owner))
                        .Select(d => d.Prerequisite).ToHashSet();
                    var bindings = exports.Where(e => prerequisites.Contains(e.Owner)).Select(e =>
                    {
                        var prerequisite = runtimes.Single(r => r.Owner.Equals(e.Owner));
                        return new ModuleDependencyBinding(e.Owner, e.ContractId, e.Version,
                            prerequisite.Services.GetService(e.ServiceType) ?? throw new InvalidOperationException());
                    }).ToArray();
                    attempt.EnterOwnerStage(runtime.Owner, CompositionStage.Started);
                    Health.TransitionOwner(runtime.Owner, ModuleLifecycleStage.Enabled, ModuleLifecycleOutcome.Pending,
                        ModuleObservationCode.Enabled);
                    await runtime.Instance!.Runtime.StartAsync(new Activation(runtime.Owner, configurations[runtime.Owner],
                        attempt.TraceId, runtime.Resources, stores.GetValueOrDefault(runtime.Owner), bindings,
                        composition.Contributions.Where(c => c.Owner.Equals(runtime.Owner))), cancellation);
                    attempt.CompleteOwnerStage(runtime.Owner, CompositionStage.Started);
                    Health.TransitionOwner(runtime.Owner, ModuleLifecycleStage.Enabled, ModuleLifecycleOutcome.Completed,
                        ModuleObservationCode.Enabled);
                }, token, runtime.Owner);
            }
            catch (CompositionUnstoppedException) { throw; }
            catch (CompositionBoundaryException error) when (error.Code is "composition_deadline_exceeded" or
                "composition_cancelled" or "composition_output_failed" or "composition_output_limit")
            { throw; }
            catch (Exception)
            {
                Health.TransitionOwner(runtime.Owner, ModuleLifecycleStage.Enabled, ModuleLifecycleOutcome.Failed,
                    ModuleObservationCode.Failed);
                attempt.FailPhase(CompositionStage.Started, CompositionOutcome.Failed, "composition_activation_failed");
                throw new CompositionBoundaryException("composition_activation_failed");
            }
            await VerifyAuthorityAsync(token);
            diagnostics.Emit(CompositionStage.Started, CompositionOutcome.Passed, "composition_progress", runtime.Owner);
        }
        attempt.CompletePhase(CompositionStage.Started);
        await SweepAsync(token);
        await VerifyAuthorityAsync(token);
        attempt.Publish(Health.Publish);
        diagnostics.Emit(CompositionStage.Published, CompositionOutcome.Passed, "composition_ready");
        diagnostics.BeginSteadyState();
    }

    public async Task SweepAsync(CancellationToken token)
    {
        await healthGate.WaitAsync(token);
        try
        {
            if (healthUnstopped || cleaned) throw Boundary();
            await SweepCoreAsync(token);
        }
        catch (CompositionUnstoppedException) { healthUnstopped = true; throw; }
        finally { healthGate.Release(); }
    }

    private async Task SweepCoreAsync(CancellationToken token)
    {
        await VerifyAuthorityAsync(token);
        attempt.EnterPhase(CompositionStage.Health);
        diagnostics.Emit(CompositionStage.Health, CompositionOutcome.Progress, "composition_progress");
        var previous = Health.Snapshot;
        await callbacks.RunAsync(CompositionWork.HealthSweep, async sweepToken =>
        {
            foreach (var runtime in runtimes)
            {
                await VerifyAuthorityAsync(sweepToken);
                ModuleHealthResult result;
                try
                {
                    result = await callbacks.RunAsync(CompositionWork.Health, async cancellation =>
                    {
                        attempt.EnterOwnerStage(runtime.Owner, CompositionStage.Health);
                        var observed = await runtime.Instance!.Runtime.CheckHealthAsync(cancellation)
                            ?? throw new InvalidOperationException();
                        attempt.CompleteOwnerStage(runtime.Owner, CompositionStage.Health);
                        return observed;
                    }, sweepToken, runtime.Owner);
                }
                catch (CompositionUnstoppedException) { throw; }
                catch (CompositionBoundaryException) { throw; }
                catch (Exception) { result = new(ModuleHealth.Failed, ModuleHealthReason.Unavailable); }
                await VerifyAuthorityAsync(sweepToken);
                Health.Observe(runtime.Owner, result);
            }
        }, token);
        await VerifyAuthorityAsync(token);
        var current = Health.Snapshot;
        foreach (var observation in current.Modules.Where(m => m.DesiredEnabled))
        {
            var before = previous.Modules.Single(m => m.Owner.Equals(observation.Owner));
            if (before.OwnHealth == observation.OwnHealth && before.EffectiveHealth == observation.EffectiveHealth) continue;
            var code = observation.EffectiveHealth switch
            {
                ModuleHealth.Healthy => "composition_ready",
                ModuleHealth.Degraded => "composition_health_degraded",
                ModuleHealth.Failed => "composition_health_failed",
                _ => "composition_health_unknown"
            };
            diagnostics.Emit(CompositionStage.Health, observation.EffectiveHealth == ModuleHealth.Healthy
                    ? CompositionOutcome.Passed : CompositionOutcome.Failed, code, observation.Owner,
                observation.AffectedPrerequisites.Take(8).ToArray());
        }
        attempt.CompletePhase(CompositionStage.Health);
    }

    public async Task ObserveAsync(CancellationToken token)
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), clock, token);
            await SweepAsync(token);
            diagnostics.Emit(CompositionStage.Health, Health.IsReady ? CompositionOutcome.Passed : CompositionOutcome.Failed,
                Health.IsReady ? "composition_ready" : "composition_health_unknown");
        }
    }

    public async Task CleanupAsync()
    {
        if (cleaned) return;
        cleaned = true;
        Health.Withdraw();
        attempt.BeginCleanup();
        var incomplete = false;
        var failures = 0;
        diagnostics.Emit(CompositionStage.Stopping, CompositionOutcome.Progress, "composition_progress");
        foreach (var runtime in runtimes.AsEnumerable().Reverse())
        {
            var before = failures;
            runtime.Resources.Closed = true;
            Health.TransitionOwner(runtime.Owner, ModuleLifecycleStage.Stopping, ModuleLifecycleOutcome.Pending,
                ModuleObservationCode.Stopped);
            if (runtime.Entered && !runtime.Stopped)
            {
                runtime.Stopped = true;
                await CleanupActionAsync(CompositionWork.Stop, async cancellation =>
                {
                    attempt.EnterOwnerStage(runtime.Owner, CompositionStage.Stopping);
                    await runtime.Instance!.Runtime.StopAsync(cancellation);
                    attempt.CompleteOwnerStage(runtime.Owner, CompositionStage.Stopping);
                }, runtime.Owner);
            }
            attempt.EnterOwnerStage(runtime.Owner, CompositionStage.Cleanup);
            var beforeResources = failures;
            while (runtime.Resources.Cleanups.TryPop(out var cleanup))
                await CleanupActionAsync(CompositionWork.Cleanup, cleanup, runtime.Owner);
            if (!runtime.ContainerOwnsRuntime && runtime.Instance is { } instance)
                await CleanupActionAsync(CompositionWork.Cleanup, _ => DisposeRuntimeAsync(instance.Runtime), runtime.Owner);
            await CleanupActionAsync(CompositionWork.Cleanup, _ => runtime.Scope.DisposeAsync(), runtime.Owner);
            if (failures == beforeResources) attempt.CompleteOwnerStage(runtime.Owner, CompositionStage.Cleanup);
            if (failures == before)
            {
                attempt.EnterOwnerStage(runtime.Owner, CompositionStage.Stopped);
                attempt.CompleteOwnerStage(runtime.Owner, CompositionStage.Stopped);
            }
            diagnostics.Emit(CompositionStage.Stopped, failures != before ? CompositionOutcome.Incomplete : CompositionOutcome.Passed,
                failures != before ? "composition_cleanup_incomplete" : "composition_progress", runtime.Owner);
            Health.TransitionOwner(runtime.Owner, ModuleLifecycleStage.Stopped,
                failures != before ? ModuleLifecycleOutcome.Incomplete : ModuleLifecycleOutcome.Completed,
                failures != before ? ModuleObservationCode.CleanupIncomplete : ModuleObservationCode.Stopped);
        }
        Health.Transition(ModuleLifecycleStage.Stopped);
        if (incomplete)
        {
            attempt.FailPhase(CompositionStage.Cleanup, CompositionOutcome.Incomplete, "composition_cleanup_incomplete");
            throw new CompositionBoundaryException("composition_cleanup_incomplete");
        }
        attempt.CompletePhase(CompositionStage.Stopped);
        return;

        async Task CleanupActionAsync(CompositionWork operation, Func<CancellationToken, ValueTask> cleanup, ModuleId owner)
        {
            try { await callbacks.RunAsync(operation, cleanup, owner: owner); }
            catch (CompositionUnstoppedException) { throw; }
            catch (Exception) { incomplete = true; failures++; }
        }
    }

    private static ValueTask DisposeRuntimeAsync(IModuleRuntime runtime)
    {
        if (runtime is IAsyncDisposable asyncDisposable) return asyncDisposable.DisposeAsync();
        if (runtime is IDisposable disposable) disposable.Dispose();
        return ValueTask.CompletedTask;
    }

    private async Task VerifyAuthorityAsync(CancellationToken token)
    {
        try
        {
            await callbacks.RunAsync(CompositionWork.ResolveStorage,
                cancellation => new ValueTask(ensureAuthority(cancellation)), token);
        }
        catch
        {
            Health.Withdraw();
            throw;
        }
    }

    private static bool AllowedType(Type type) => type != typeof(IServiceProvider) &&
        type != typeof(IServiceScopeFactory) && !typeof(IHostedService).IsAssignableFrom(type) &&
        !typeof(IStorageRegistry).IsAssignableFrom(type) && !typeof(IStorageMaintenance).IsAssignableFrom(type) &&
        !typeof(IStorageMaintenanceScope).IsAssignableFrom(type) &&
        !typeof(IModuleStorage).IsAssignableFrom(type) && type.Assembly != typeof(ModuleId).Assembly &&
        type.Assembly != typeof(Kofte.Infrastructure.Storage.ModuleStorageBinding).Assembly &&
        type.Assembly != typeof(ModuleLifecycleCoordinator).Assembly &&
        !(type.Namespace?.StartsWith("Microsoft.", StringComparison.Ordinal) ?? false) &&
        !(type.Namespace?.StartsWith("System", StringComparison.Ordinal) ?? false);

    private static CompositionBoundaryException Boundary() => new("composition_observation_invalid");
    private CompositionBoundaryException RegistrationFailed()
    {
        attempt.FailPhase(CompositionStage.Registered, CompositionOutcome.Failed, "composition_registration_failed");
        return new("composition_registration_failed");
    }
}
