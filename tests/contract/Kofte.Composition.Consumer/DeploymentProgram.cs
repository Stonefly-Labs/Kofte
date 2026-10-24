using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;
using Kofte.Composition.TestModule;
using Kofte.Composition.TestModule.Modules;
using Kofte.Host.Features.ModuleComposition;
using Kofte.Infrastructure.Composition;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Kofte.TestSupport;

namespace Kofte.Composition.Consumer;

internal static class DeploymentProgram
{
    internal const string Owner = "org.example.validation";
    internal const string DisabledOwner = "org.example.disabled";
    internal static readonly ModuleConfigurationField Secret = new("secret", ["secret"], ModuleSettingType.String, required: true);
    private static readonly ModuleConfigurationField Escaped = new("escaped", ["escaped"], ModuleSettingType.String, required: true);
    private static readonly ModuleConfigurationField Flag = new("flag", ["flag"], ModuleSettingType.Boolean, required: true);
    private static string? workerEventPipe;
    internal static int MutatingCallbacks { get; private set; }
    internal static int DisabledValidators { get; private set; }

    internal static Task<int> RunAsync(string[] args)
    {
        workerEventPipe = args is ["--modules-worker", "validate"]
            ? Environment.GetEnvironmentVariable("KOFTE_COMPOSITION_EVENT_PIPE") : null;
        var value = Environment.GetEnvironmentVariable("COMPOSITION_TEST_CATALOG");
        if (value is not null && (!Enum.TryParse<CompositionTestCatalog>(value, out var parsed) ||
            !Enum.IsDefined(parsed) || parsed.ToString() != value))
            return RefuseCatalogAsync();
        var catalog = value is null ? CompositionTestCatalog.Default : Enum.Parse<CompositionTestCatalog>(value);
        var deployments = Catalog(catalog);
        var recipes = Recipes(catalog);
        return RunDeploymentAsync(args, catalog, deployments, recipes);
    }

    private static async Task<int> RunDeploymentAsync(string[] args, CompositionTestCatalog catalog,
        IReadOnlyList<ModuleDeployment> deployments, IReadOnlyList<ModuleStorageRecipe> recipes)
    {
        if (catalog == CompositionTestCatalog.RelationalCancelBeforeWork && args is ["--modules", "prepare"])
        {
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            return await ModuleCommand.RunAsync(args, deployments, recipes, cancelled.Token);
        }
        using var migrationBarrier = catalog == CompositionTestCatalog.RelationalHoldMigration &&
            args is ["--modules-worker", "prepare"]
            ? new StoragePhaseBarrier("migration_in_progress_committed", hold: true, owner: StorageRecipes.BetaOwner,
                onReached: () => AuthorProbe.Record(StorageRecipes.BetaOwner, "migration"))
            : null;
        return args is ["--modules-worker", "serve" or "prepare"] &&
            (Fault(catalog) is not null || catalog is CompositionTestCatalog.BootDependency or CompositionTestCatalog.BootDisabled or
                CompositionTestCatalog.RelationalInitial or CompositionTestCatalog.RelationalCurrent or
                CompositionTestCatalog.RelationalHoldMigration)
            ? await Scenarios.AuthorProbePump.RunWorkerAsync(args, deployments, recipes)
            : await ModuleCompositionHost.RunAsync(args, deployments, recipes);
    }

    private static async Task<int> RefuseCatalogAsync()
    {
        await Console.Error.WriteLineAsync("""{"eventCode":"composition_test_catalog_invalid","status":"failed"}""");
        return 2;
    }

    internal static CompositionCorpusCase Frozen(string name) =>
        FrozenCompositionCorpus.Cases.Single(item => item.Name == name);

    internal static string? FrozenName(CompositionTestCatalog catalog) => catalog switch
    {
        CompositionTestCatalog.MalformedManifest => "invalid-disabled",
        CompositionTestCatalog.DuplicateManifest => "duplicate",
        CompositionTestCatalog.IncompatibleManifest => "incompatible-host",
        CompositionTestCatalog.MissingDependency => "missing-dependency",
        CompositionTestCatalog.DisabledDependency => "disabled-dependency",
        CompositionTestCatalog.IncompatibleDependency => "incompatible-dependency",
        CompositionTestCatalog.PrereleaseDependency => "prerelease-ineligible",
        CompositionTestCatalog.DependencyCycle => "multiple-scc",
        _ => null
    };

    internal static IReadOnlyList<ModuleDeployment> Catalog(CompositionTestCatalog catalog)
    {
        if (FrozenName(catalog) is { } name) return Frozen(name).Deployments;
        return catalog switch
        {
            CompositionTestCatalog.EmptyBoot => [],
            CompositionTestCatalog.BootDependency => BootModules.Catalog(holdBeta: true),
            CompositionTestCatalog.BootDisabled => BootModules.Catalog(holdBeta: false),
            CompositionTestCatalog.BootMissing => [],
            CompositionTestCatalog.RelationalInitial => RelationalModules.Catalog(StorageRelease.Initial),
            CompositionTestCatalog.RelationalCurrent or CompositionTestCatalog.RelationalInvalidRecipe or
                CompositionTestCatalog.RelationalHoldMigration =>
                RelationalModules.Catalog(StorageRelease.Current),
            _ when Fault(catalog) is { } fault => RelationalModules.Catalog(StorageRelease.Current, fault),
            CompositionTestCatalog.Default => [
                Module(Owner, fields: [Secret], validator: new SecretValidator()),
                Module(DisabledOwner, validator: new DisabledValidator())
            ],
            CompositionTestCatalog.MismatchedClaim => [Route(Owner, "org.example.other", "/items")],
            CompositionTestCatalog.ReservedClaim => [Route(Owner, Owner, "/health")],
            CompositionTestCatalog.CorrectedClaim => [Route(Owner, Owner, "/items")],
            CompositionTestCatalog.OverlappingClaim => [
                Route(Owner, Owner, "/items/{id}"),
                Route("org.example.second", "org.example.second", "/items/current")
            ],
            CompositionTestCatalog.UnsupportedClaim => [Route(Owner, Owner, "/items", ContributionMode.Executable)],
            CompositionTestCatalog.CatalogLimit => Enumerable.Range(0, 33).Select(index => Module($"org.example.m{index}")).ToArray(),
            CompositionTestCatalog.EnabledLimit => Enumerable.Range(0, 17).Select(index => Module($"org.example.m{index}")).ToArray(),
            CompositionTestCatalog.ManifestByteLimit => [Padded(65_537)],
            CompositionTestCatalog.ClaimLimit => [Module(Owner, claims: Enumerable.Range(0, 129)
                .Select(index => new ContributionClaim(Owner, "resource", $"item{index}")), capability: "resources")],
            CompositionTestCatalog.IgnoringValidator => [Module(Owner, fields: [Secret], validator: new IgnoringValidator())],
            CompositionTestCatalog.RequiredOutputFault => [Module(Owner, fields: [Secret], validator: new BrokenEventValidator())],
            CompositionTestCatalog.SnapshotTransport => [Module(Owner, fields: [Secret, Escaped, Flag], validator: new SnapshotValidator())],
            _ => throw new InvalidOperationException("composition_test_catalog_invalid")
        };
    }

    internal static IReadOnlyList<ModuleStorageRecipe> Recipes(CompositionTestCatalog catalog) => catalog switch
    {
        CompositionTestCatalog.RelationalInitial => StorageRecipes.ForRelease(StorageRelease.Initial),
        CompositionTestCatalog.RelationalCurrent or CompositionTestCatalog.RelationalHoldMigration => StorageRecipes.ForRelease(),
        _ when Fault(catalog) is not null => StorageRecipes.ForRelease(),
        CompositionTestCatalog.RelationalInvalidRecipe =>
        [
            .. StorageRecipes.ForRelease().Where(recipe => recipe.Owner.Value == StorageRecipes.AlphaOwner),
            ModuleStorageRecipe.Create<Kofte.Composition.TestModule.Modules.Beta.Storage.SqliteBetaContext>(
                StorageRecipes.Descriptions[1].Id, "sqlite", "missing", (options, boundary) => new(options, boundary)),
            ModuleStorageRecipe.Create<Kofte.Composition.TestModule.Modules.Beta.Storage.PostgresBetaContext>(
                StorageRecipes.Descriptions[1].Id, "postgresql", "missing", (options, boundary) => new(options, boundary))
        ],
        _ => []
    };

    private static ModuleFault? Fault(CompositionTestCatalog catalog) => catalog switch
    {
        CompositionTestCatalog.RelationalConfigurationGate => ModuleFault.ConfigurationGate,
        CompositionTestCatalog.RelationalFailInitialize => ModuleFault.FailInitialize,
        CompositionTestCatalog.RelationalFailStart => ModuleFault.FailStart,
        CompositionTestCatalog.RelationalFailHealth => ModuleFault.FailHealth,
        CompositionTestCatalog.RelationalIgnoreValidator => ModuleFault.IgnoreValidator,
        CompositionTestCatalog.RelationalIgnoreRegistrar => ModuleFault.IgnoreRegistrar,
        CompositionTestCatalog.RelationalIgnoreInitialize => ModuleFault.IgnoreInitialize,
        CompositionTestCatalog.RelationalIgnoreStart => ModuleFault.IgnoreStart,
        CompositionTestCatalog.RelationalIgnoreHealth => ModuleFault.IgnoreHealth,
        CompositionTestCatalog.RelationalIgnoreStop => ModuleFault.IgnoreStop,
        CompositionTestCatalog.RelationalIgnoreCleanup => ModuleFault.IgnoreCleanup,
        CompositionTestCatalog.RelationalCancelBeforeWork => ModuleFault.None,
        CompositionTestCatalog.RelationalCancelInitialize => ModuleFault.CancelInitialize,
        CompositionTestCatalog.RelationalCleanupOverflow => ModuleFault.CleanupOverflow,
        CompositionTestCatalog.RelationalFailCleanup => ModuleFault.FailCleanup,
        _ => null
    };

    internal static byte[] Selection(IEnumerable<ModuleSelection> selected, object? settings = null) =>
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            modules = selected.Select(item => new { id = item.Id, enabled = item.Enabled, settings = settings ?? new { } })
        });

    internal static byte[] Selection(CompositionTestCatalog catalog, string canary)
    {
        if (FrozenName(catalog) is { } name) return Selection(Frozen(name).Selection);
        if (catalog == CompositionTestCatalog.Default)
            return ConfiguredSelection(new { secret = canary }, canary);
        if (catalog == CompositionTestCatalog.CatalogLimit) return Selection([]);
        if (catalog == CompositionTestCatalog.EnabledLimit)
            return Selection(Enumerable.Range(0, 17).Select(index => new ModuleSelection($"org.example.m{index}", true)));
        if (catalog == CompositionTestCatalog.OverlappingClaim)
            return Selection([new(Owner, true), new("org.example.second", true)]);
        return Selection([new(Owner, true)]);
    }

    internal static byte[] ConfiguredSelection(object settings, string canary) =>
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            modules = new[]
            {
                new { id = Owner, enabled = true, settings },
                new { id = DisabledOwner, enabled = false, settings = (object)new { unknown = canary } }
            }
        });

    internal static ModuleDeployment Padded(int length)
    {
        var bytes = Manifest(Owner);
        return new(bytes.Concat(Enumerable.Repeat((byte)' ', length - bytes.Length)).ToArray(),
            configureServices: (_, _) => throw MutatingCallback(), createRuntime: (_, _) => throw MutatingCallback());
    }

    private static ModuleDeployment Route(string owner, string claimOwner, string route,
        ContributionMode mode = ContributionMode.Declaration) =>
        Module(owner, [new(claimOwner, "route", "read", Mode: mode, Method: "GET", Route: route)], capability: "backend");

    private static byte[] Manifest(string id, string? capability = null) =>
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            id,
            version = "1.0.0",
            displayName = "Validation fixture",
            hostContract = "1.0.0",
            capabilities = capability is null ? [] : new[] { new { kind = capability, contractVersion = "1.0.0" } },
            dependencies = Array.Empty<object>(),
            permissions = Array.Empty<object>(),
            storage = new { relational = false }
        });

    private static ModuleDeployment Module(string owner, IEnumerable<ContributionClaim>? claims = null,
        IEnumerable<ModuleConfigurationField>? fields = null, IModuleConfigurationValidator? validator = null,
        string? capability = null) =>
        new(Manifest(owner, capability), claims, fields, configurationValidator: validator,
            configureServices: (_, _) => throw MutatingCallback(), createRuntime: (_, _) => throw MutatingCallback());

    private static Exception MutatingCallback()
    {
        MutatingCallbacks++;
        return new InvalidOperationException("composition_test_mutating_callback");
    }

    private sealed class SecretValidator : IModuleConfigurationValidator
    {
        public IEnumerable<ModuleConfigurationFailure> Validate(ModuleConfiguration configuration, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return configuration.TryGetValue(Secret, out var value) && value.GetString() is { Length: > 0 }
                ? [] : [new(Secret, ModuleSettingFailureReason.Constraint)];
        }
    }

    private sealed class DisabledValidator : IModuleConfigurationValidator
    {
        public IEnumerable<ModuleConfigurationFailure> Validate(ModuleConfiguration configuration, CancellationToken token)
        {
            DisabledValidators++;
            throw new InvalidOperationException("composition_test_disabled_validator");
        }
    }

    private sealed class IgnoringValidator : IModuleConfigurationValidator
    {
        public IEnumerable<ModuleConfigurationFailure> Validate(ModuleConfiguration configuration, CancellationToken token)
        {
            while (true) Thread.SpinWait(4096);
        }
    }

    private sealed class BrokenEventValidator : IModuleConfigurationValidator
    {
        public IEnumerable<ModuleConfigurationFailure> Validate(ModuleConfiguration configuration, CancellationToken token)
        {
            if (workerEventPipe is null || !configuration.TryGetValue(Secret, out var value))
                throw new InvalidOperationException("composition_test_owned_event_pipe_required");
            Console.Out.WriteLine(value.GetString());
            Console.Error.WriteLine(value.GetString());
            using (new AnonymousPipeClientStream(PipeDirection.Out, workerEventPipe)) { }
            return [];
        }
    }

    private sealed class SnapshotValidator : IModuleConfigurationValidator
    {
        public IEnumerable<ModuleConfigurationFailure> Validate(ModuleConfiguration configuration, CancellationToken token)
        {
            var started = Stopwatch.GetTimestamp();
            while (Stopwatch.GetElapsedTime(started) < TimeSpan.FromMilliseconds(100)) Thread.SpinWait(1024);
            token.ThrowIfCancellationRequested();
            var snapshot = configuration.Snapshot();
            var secret = snapshot.GetProperty("secret").GetString();
            var expected = "{ \"secret\" : \"" + secret + "\", \"escaped\" : \"\\u0130\", \"flag\" : true }";
            return secret?.StartsWith("composition-private-", StringComparison.Ordinal) == true &&
                snapshot.GetRawText() == expected ? [] : [new(Secret, ModuleSettingFailureReason.Constraint)];
        }
    }
}
