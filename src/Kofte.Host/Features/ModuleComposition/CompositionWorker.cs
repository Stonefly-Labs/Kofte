using System.Net;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.Bootstrap;
using Kofte.Host.Features.StorageMaintenance;
using Kofte.Host.Features.Identity;
using Kofte.Infrastructure.Composition;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Host.Features.ModuleComposition;

internal static class CompositionWorker
{
    internal static async Task<int> RunAsync(string[] args, IReadOnlyList<ModuleDeployment> catalog,
        IReadOnlyList<ModuleStorageRecipe> recipes)
    {
        CompositionDiagnostics? diagnostics = null;
        CompositionAdmission? admission = null;
        CompositionAttempt? attempt = null;
        CompositionCallbackRunner? callbacks = null;
        CompositionStorageSession? storage = null;
        CompositionAuthority? authority = null;
        ModuleLifecycleCoordinator? coordinator = null;
        ServiceProvider? provider = null;
        WebApplication? app = null;
        IdentityRuntime? identity = null;
        CompositionFailure? failure = null;
        Task? observing = null, watching = null;
        string? cleanupBoundary = null;
        var served = false;
        using var lifetime = new CancellationTokenSource();
        try
        {
            var mode = ModuleCommand.Mode(args, worker: true);
            using var admissionBudget = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            admission = await CompositionAdmission.ReadAsync(mode, catalog, admissionBudget.Token);
            CompositionProcessGroup.Enter();
            var configuration = StorageMaintenanceConfiguration.FromEnvironment();
            if (mode != CompositionMode.Prepare &&
                Environment.GetEnvironmentVariable("KOFTE_POSTGRES_PROVISIONING_CONNECTION") is not null)
                throw new CompositionBoundaryException("composition_admission_invalid");
            var protocol = new CompositionProtocol(admission.Attempt, admission.Snapshot,
                CompositionSupervisor.CatalogOwners(catalog), mode, CompositionSupervisor.CatalogFields(catalog),
                admission.Selection.Modules.Select(module => module.Owner), CompositionSupervisor.CatalogPrerequisites(catalog),
                admission.Selection.Modules.Where(module => module.Enabled).Select(module => module.Owner));
            diagnostics = new(mode, configuration.Runtime.Profile == "local-sqlite" ? "sqlite" : "postgresql",
                record => protocol.WriteAsync(admission.Events, record, CancellationToken.None).GetAwaiter().GetResult(),
                attemptId: admission.Attempt);
            attempt = new(diagnostics, lifetime.Token);
            callbacks = new(attempt);
            attempt.CompletePhase(CompositionStage.Admission);
            diagnostics.Emit(CompositionStage.Admission, CompositionOutcome.Passed, "composition_progress");
            _ = StopOnParentExitAsync(lifetime);
            var preflight = await CompositionPreflight.ResolveAsync(catalog, admission.Selection, attempt, diagnostics, lifetime.Token);
            if (recipes.Count > 64 || recipes.Any(recipe => !preflight.Deployments.ContainsKey(recipe.Owner)))
                throw new CompositionBoundaryException("composition_storage_rejected");
            var enabled = preflight.Composition.Order.Select(owner =>
                preflight.Composition.Deployed.Single(module => module.Id.Equals(owner))).ToArray();
            var enabledRecipes = recipes.Where(recipe => preflight.Composition.Enabled.Contains(recipe.Owner)).ToArray();
            var installation = configuration.CreateInstallation();
            storage = await callbacks.RunAsync(CompositionWork.AcquireStorage, cancellation =>
                new ValueTask<CompositionStorageSession>(mode == CompositionMode.Prepare
                    ? CompositionStorageSession.OpenPrepareAsync(installation, cancellation)
                    : CompositionStorageSession.OpenReadAsync(installation, cancellation)), lifetime.Token);
            authority = new(cancellation => callbacks.RunAsync(CompositionWork.ResolveStorage,
                heldToken => new ValueTask(storage.EnsureAuthorityAsync(heldToken)), cancellation),
                () => coordinator?.Health.Withdraw());
            await callbacks.RunAsync(CompositionWork.StoragePreflight, cancellation =>
                new ValueTask(storage.InspectAsync(enabled, enabledRecipes, mode == CompositionMode.Serve, cancellation)),
                lifetime.Token);
            // Inspection verifies the PostgreSQL installation and retained exclusive
            // session; a second session's shared-lease probe would contend with it.
            if (mode != CompositionMode.Prepare || installation.Provider != "postgresql")
            {
                IPlatformStore selectedStore = configuration.Runtime.Profile == "local-sqlite"
                    ? new SqlitePlatformStore(configuration.Runtime.DataRoot)
                    : new PostgresPlatformStore(configuration.Runtime.PostgresConnection!);
                await callbacks.RunAsync(CompositionWork.ResolveStorage,
                    cancellation => new ValueTask(selectedStore.ProbeAsync(cancellation)), lifetime.Token);
            }
            await authority.EnsureAsync(lifetime.Token);
            attempt.CompletePhase(CompositionStage.Validated);
            diagnostics.Emit(CompositionStage.Validated, CompositionOutcome.Passed, "composition_progress");
            if (mode != CompositionMode.Validate)
            {
                coordinator = new(preflight.Composition, preflight.Deployments, preflight.Configurations, attempt, diagnostics,
                    ensureAuthority: authority.EnsureAsync);
                IServiceCollection services;
                WebApplicationBuilder? builder = null;
                if (mode == CompositionMode.Serve)
                {
                    builder = Builder(configuration.Runtime, coordinator.Health);
                    services = builder.Services;
                    identity = await callbacks.RunAsync(CompositionWork.ResolveStorage, async cancellation =>
                    {
                        using var budget = new OperationBudget(OperationBudget.Resolve, cancellation);
                        return await IdentityRuntime.OpenAsync(configuration.Runtime, builder.Environment.EnvironmentName,
                            installation, budget, record =>
                                protocol.WriteIdentityAsync(admission.Events, record, CancellationToken.None)
                                    .GetAwaiter().GetResult());
                    }, lifetime.Token);
                    var held = storage.RuntimeAuthority;
                    identity.Register(services, held.FileLease, held.PostgresLease);
                }
                else services = new ServiceCollection();
                await coordinator.RegisterAsync(services, lifetime.Token);
                if (builder is not null)
                {
                    app = await callbacks.RunAsync(CompositionWork.Registrar,
                        _ => ValueTask.FromResult(builder.Build()), lifetime.Token);
                    app.MapBootstrapHealth();
                    app.UseIdentityBoundary();
                    if (app.Environment.IsDevelopment())
                        app.MapOpenApi("/openapi/v1.json").WithName("bootstrapOpenApi").AllowAnonymous();
                    await coordinator.InitializeAsync(app.Services, lifetime.Token);
                }
                else
                {
                    provider = await callbacks.RunAsync(CompositionWork.Registrar, _ => ValueTask.FromResult(
                        services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true })),
                        lifetime.Token);
                    await coordinator.InitializeAsync(provider, lifetime.Token);
                }
                await authority.EnsureAsync(lifetime.Token);
                attempt.EnterPhase(CompositionStage.Migrated);
                var bindings = await callbacks.RunAsync(CompositionWork.MigrateStorage, cancellation =>
                    new ValueTask<IReadOnlyDictionary<Kofte.Platform.Modules.Manifest.ModuleId, Kofte.Infrastructure.Storage.ModuleStorageBinding>>(
                        mode == CompositionMode.Prepare
                            ? storage.PrepareAsync(enabled, enabledRecipes, cancellation, coordinator.ObserveStorage)
                            : storage.VerifyAsync(enabled, enabledRecipes, cancellation, coordinator.ObserveStorage)), lifetime.Token);
                await authority.EnsureAsync(lifetime.Token);
                coordinator.MarkMigrated();
                if (mode == CompositionMode.Serve)
                {
                    await callbacks.RunAsync(CompositionWork.ResolveStorage, cancellation =>
                        new ValueTask(app!.StartAsync(cancellation)), lifetime.Token);
                    await coordinator.StartAsync(bindings.ToDictionary(pair => pair.Key, pair => (IModuleStorage)pair.Value),
                        lifetime.Token);
                    served = true;
                    observing = coordinator.ObserveAsync(lifetime.Token);
                    watching = authority.WatchAsync(lifetime.Token);
                    using var stopping = app!.Services.GetRequiredService<IHostApplicationLifetime>()
                        .ApplicationStopping.Register(lifetime.Cancel);
                    var stopped = Task.Delay(Timeout.InfiniteTimeSpan, lifetime.Token);
                    var completed = await Task.WhenAny(observing, watching, stopped);
                    if (completed == observing) await observing;
                    if (completed == watching) await watching;
                    lifetime.Cancel();
                }
            }
        }
        catch (CompositionUnstoppedException error)
        {
            TerminateUnstopped(error);
        }
        catch (OperationCanceledException) when (served && lifetime.IsCancellationRequested)
        {
        }
        catch (CompositionBoundaryException error) when (error.Code == "composition_cancelled" &&
            served && lifetime.IsCancellationRequested)
        {
        }
        catch (Exception error)
        {
            failure = CompositionFailure.From(error);
            if (error is not CompositionPreflightException && diagnostics is not null && attempt is not null)
            {
                try
                {
                    var owner = coordinator?.Health.Snapshot.Modules.FirstOrDefault(module =>
                        module.Outcome is ModuleLifecycleOutcome.Failed or ModuleLifecycleOutcome.Blocked)?.Owner;
                    coordinator?.Health.Withdraw();
                    diagnostics.Emit(attempt.Phase == CompositionStage.Published ? CompositionStage.Stopping : attempt.Phase,
                        failure.Outcome, failure.Code, owner,
                        storageCorrelation: failure.Storage?.CorrelationId,
                        durability: failure.Storage?.Durability.ToString().ToLowerInvariant());
                }
                catch (Exception projection) { failure = CompositionFailure.From(projection); }
            }
        }

        lifetime.Cancel();
        foreach (var pending in new[] { observing, watching }.OfType<Task>())
        {
            try { await pending; }
            catch (CompositionUnstoppedException error) { TerminateUnstopped(error); }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
            catch (CompositionBoundaryException error) when (error.Code == "composition_cancelled" && lifetime.IsCancellationRequested) { }
            catch (Exception error) { failure ??= CompositionFailure.From(error); }
        }
        try
        {
            if (coordinator is not null) await coordinator.CleanupAsync();
            else attempt?.BeginCleanup();
            if (callbacks is not null)
            {
                if (app is not null)
                {
                    cleanupBoundary = "composition_host_stop_incomplete";
                    await callbacks.RunAsync(CompositionWork.Cleanup, cancellation => new ValueTask(app.StopAsync(cancellation)));
                    cleanupBoundary = "composition_host_dispose_incomplete";
                    await callbacks.RunAsync(CompositionWork.Cleanup, _ => app.DisposeAsync());
                }
                if (provider is not null)
                {
                    cleanupBoundary = "composition_services_dispose_incomplete";
                    await callbacks.RunAsync(CompositionWork.Cleanup, _ => provider.DisposeAsync());
                }
                if (storage is not null)
                {
                    if (identity is not null)
                    {
                        cleanupBoundary = "composition_services_dispose_incomplete";
                        await callbacks.RunAsync(CompositionWork.Cleanup, _ =>
                        {
                            identity.Dispose();
                            return ValueTask.CompletedTask;
                        });
                    }
                    cleanupBoundary = "composition_storage_dispose_incomplete";
                    await callbacks.RunAsync(CompositionWork.Cleanup, _ => storage.DisposeAsync());
                }
            }
        }
        catch (CompositionUnstoppedException error)
        {
            TerminateUnstopped(error);
        }
        catch (Exception)
        {
            failure = new("composition_cleanup_incomplete", CompositionOutcome.Incomplete, 1);
            try { ReportCleanupBoundary(); }
            catch (Exception projection) { failure = CompositionFailure.From(projection); }
        }
        try
        {
            if (failure is not null) attempt?.FailPhase(CompositionStage.Cleanup, failure.Outcome, failure.Code);
            else attempt?.CompletePhase(CompositionStage.Stopped);
            diagnostics ??= new(CompositionMode.Validate, "unknown", ModuleCommand.Write);
            diagnostics.Emit(CompositionStage.Cleanup, failure?.Outcome ?? CompositionOutcome.Passed,
                failure?.Code ?? "composition_progress",
                storageCorrelation: failure?.Storage?.CorrelationId,
                durability: failure?.Storage?.Durability.ToString().ToLowerInvariant(), isTerminal: true);
        }
        catch (Exception) { return 1; }
        finally { admission?.Dispose(); }
        return failure?.Exit ?? 0;

        void ReportCleanupBoundary()
        {
            if (cleanupBoundary is not null)
                diagnostics?.Emit(CompositionStage.Stopping, CompositionOutcome.Incomplete, cleanupBoundary);
        }

        void TerminateUnstopped(CompositionUnstoppedException error)
        {
            try
            {
                ReportCleanupBoundary();
                var stage = error.Work switch
                {
                    CompositionWork.Validator or CompositionWork.Resolver => CompositionStage.Validated,
                    CompositionWork.Registrar => CompositionStage.Registered,
                    CompositionWork.Initialize => CompositionStage.Initialized,
                    CompositionWork.Start => CompositionStage.Started,
                    CompositionWork.Health or CompositionWork.HealthSweep => CompositionStage.Health,
                    CompositionWork.Stop => CompositionStage.Stopping,
                    CompositionWork.Cleanup when error.Owner is not null => CompositionStage.Cleanup,
                    CompositionWork.Cleanup => CompositionStage.Stopping,
                    _ => attempt?.Phase ?? CompositionStage.Admission
                };
                diagnostics?.Emit(stage, error.Code == "composition_cancelled" ? CompositionOutcome.Cancelled :
                    CompositionOutcome.Deadline, error.Code, error.Owner);
                attempt?.FailPhase(CompositionStage.Cleanup, CompositionOutcome.Incomplete, "composition_cleanup_incomplete");
                diagnostics?.Emit(CompositionStage.Cleanup, CompositionOutcome.Incomplete,
                    "composition_cleanup_incomplete", isTerminal: true);
            }
            catch (Exception) { }
            Environment.Exit(1);
        }
    }

    private static WebApplicationBuilder Builder(BootstrapConfiguration config, CompositionHealth health)
    {
        // Disable unused JSON reload watchers before defaults create them; clearing sources is too late.
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            Args = ["--hostBuilder:reloadConfigOnChange=false"]
        });
        builder.Configuration.Sources.Clear();
        builder.Configuration.AddInMemoryCollection();
        builder.Logging.ClearProviders();
        // Leave part of the one-second cleanup action for Kestrel to abort and finish draining.
        builder.Services.Configure<HostOptions>(options => options.ShutdownTimeout = TimeSpan.FromMilliseconds(500));
        builder.Host.UseDefaultServiceProvider(options =>
        {
            options.ValidateScopes = true;
            options.ValidateOnBuild = true;
        });
        var bindings = new List<string> { config.HostUrl.AbsoluteUri };
        if (!BootstrapConfiguration.Loopback(config.HostUrl.Host) &&
            IPAddress.TryParse(config.HostUrl.Host.Trim('[', ']'), out var address) &&
            !address.Equals(IPAddress.Any) && !address.Equals(IPAddress.IPv6Any))
            bindings.Add(config.BackendOrigin.AbsoluteUri);
        builder.WebHost.UseUrls(bindings.ToArray()).UseKestrel(options => options.AddServerHeader = false);
        builder.Services.AddSingleton(config with { PostgresConnection = null });
        IPlatformStore store = config.Profile == "local-sqlite"
            ? new SqlitePlatformStore(config.DataRoot) : new PostgresPlatformStore(config.PostgresConnection!);
        builder.Services.AddSingleton(store);
        builder.Services.AddSingleton(health);
        builder.Services.AddSingleton<HealthHandler>();
        if (builder.Environment.IsDevelopment()) builder.Services.AddBootstrapOpenApi();
        BootstrapTelemetry.Add(builder.Services, config);
        return builder;
    }

    private static async Task StopOnParentExitAsync(CancellationTokenSource lifetime)
    {
        try
        {
            using var input = Console.OpenStandardInput();
            _ = await input.ReadAsync(new byte[1], lifetime.Token);
            lifetime.Cancel();
        }
        catch (Exception) { lifetime.Cancel(); }
    }
}
