using System.Text.Json;
using System.Diagnostics;
using System.Reflection;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Composition.TestModule.Modules;

public enum ModuleFault
{
    None, ConfigurationGate, FailInitialize, FailStart, FailHealth, IgnoreValidator, IgnoreRegistrar,
    IgnoreInitialize, IgnoreStart, IgnoreHealth, IgnoreStop, IgnoreCleanup, CancelInitialize,
    CleanupOverflow, FailCleanup
}

public static class RelationalModules
{
    private static readonly ModuleConfigurationField secret = new("secret", ["secret"], ModuleSettingType.String);
    private static readonly ModuleConfigurationField permit = new("permit", ["permit"], ModuleSettingType.Boolean, required: true);

    public static IReadOnlyList<ModuleDeployment> Catalog(StorageRelease release, ModuleFault fault = ModuleFault.None) =>
        StorageRecipes.Descriptions.Reverse().Select(owner => new ModuleDeployment(
            Manifest(owner), configurationFields: fault == ModuleFault.ConfigurationGate && owner.Id.Value == StorageRecipes.BetaOwner
                ? [secret, permit] : [secret], configurationValidator: new Validator(owner.Id.Value,
                    owner.Id.Value == StorageRecipes.BetaOwner ? fault : ModuleFault.None),
            configureServices: (_, context) =>
            {
                AuthorProbe.Record(owner.Id.Value, "register", attemptId: context.AttemptId);
                if (owner.Id.Value == StorageRecipes.BetaOwner && fault == ModuleFault.IgnoreRegistrar)
                {
                    Raw(context.Configuration);
                    Thread.Sleep(Timeout.Infinite);
                }
            },
            createRuntime: (_, context) => new(new Runtime(owner.Id.Value, release, context.AttemptId,
                owner.Id.Value == StorageRecipes.BetaOwner ? fault : ModuleFault.None, context.Configuration),
                ModuleRuntimeOwnership.Attempt))).ToArray();

    private static string Raw(ModuleConfiguration configuration)
    {
        var value = configuration.TryGetValue(secret, out var setting)
            ? setting.GetString()! : "composition-author-private-fault-canary";
        Console.WriteLine(value);
        Console.Error.WriteLine(value);
        return value;
    }

    private static byte[] Manifest(ModuleDescription owner) => JsonSerializer.SerializeToUtf8Bytes(new
    {
        schemaVersion = "1",
        id = owner.Id.Value,
        version = owner.Version.ToString(),
        displayName = owner.DisplayName,
        hostContract = "1.0.0",
        capabilities = Array.Empty<object>(),
        dependencies = owner.Dependencies.Select(dependency => new
        {
            id = dependency.Id.Value,
            versionRange = ">=1.0.0 <2.0.0"
        }),
        permissions = Array.Empty<object>(),
        storage = new { relational = owner.Storage.Relational }
    });

    private sealed class Validator(string owner, ModuleFault fault) : IModuleConfigurationValidator
    {
        public IEnumerable<ModuleConfigurationFailure> Validate(ModuleConfiguration configuration, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            AuthorProbe.Record(owner, "validate");
            if (fault == ModuleFault.IgnoreValidator)
            {
                Raw(configuration);
                Thread.Sleep(Timeout.Infinite);
            }
            if (fault == ModuleFault.ConfigurationGate && configuration.TryGetValue(permit, out var allowed) &&
                !allowed.GetBoolean())
                return [new(permit, ModuleSettingFailureReason.Constraint)];
            return [];
        }
    }

    private sealed class Runtime : IModuleRuntime, IAsyncDisposable
    {
        private readonly string owner, attempt;
        private readonly StorageRelease release;
        private readonly ModuleFault fault;
        private readonly ModuleConfiguration configuration;
        private Process? descendant;

        internal Runtime(string owner, StorageRelease release, string attempt, ModuleFault fault,
            ModuleConfiguration configuration)
        {
            (this.owner, this.release, this.attempt, this.fault, this.configuration) =
                (owner, release, attempt, fault, configuration);
            AuthorProbe.Record(owner, "runtime-constructor", attemptId: attempt);
        }

        public async ValueTask InitializeAsync(ModuleInitializationContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            AuthorProbe.Record(owner, "initialize", attemptId: context.AttemptId);
            context.Resources.RegisterCleanup(async _ =>
            {
                AuthorProbe.Record(owner, "cleanup", attemptId: attempt);
                if (fault == ModuleFault.FailCleanup) throw new InvalidOperationException(Raw(configuration));
                if (fault == ModuleFault.IgnoreCleanup)
                    await Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None);
            });
            if (fault == ModuleFault.CleanupOverflow)
                for (var index = 0; index < 32; index++)
                    context.Resources.RegisterCleanup(_ => ValueTask.CompletedTask);
            if (fault == ModuleFault.FailInitialize) throw new InvalidOperationException(Raw(configuration));
            if (fault == ModuleFault.IgnoreInitialize)
                await Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None);
            if (fault == ModuleFault.CancelInitialize)
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
        }

        public async ValueTask StartAsync(ModuleActivationContext context, CancellationToken token)
        {
            AuthorProbe.Record(owner, "start", attemptId: attempt);
            if (fault == ModuleFault.FailStart) throw new InvalidOperationException(Raw(configuration));
            if (fault == ModuleFault.IgnoreStart)
            {
                var start = new ProcessStartInfo("dotnet")
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };
                start.ArgumentList.Add(Assembly.GetEntryAssembly()!.Location);
                start.ArgumentList.Add("--composition-fixture-descendant");
                foreach (var key in start.Environment.Keys.Where(key => key.StartsWith("KOFTE_", StringComparison.Ordinal) ||
                    key.StartsWith("OTEL_", StringComparison.Ordinal) || key.StartsWith("COMPOSITION_", StringComparison.Ordinal)).ToArray())
                    start.Environment.Remove(key);
                descendant = Process.Start(start) ?? throw new InvalidOperationException("composition_author_child_start_failed");
                AuthorProbe.Record(owner, "descendant", descendant.Id, attempt);
                await Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None);
            }
            if (owner == StorageRecipes.NoStorageOwner)
            {
                if (context.Storage is not null) throw new InvalidOperationException("composition_author_unexpected_storage");
                AuthorProbe.Record(owner, "no-storage", attemptId: attempt);
            }
            else
            {
                if (context.Storage is not ModuleStorageBinding binding || !binding.Owner.Equals(context.Owner))
                    throw new InvalidOperationException("composition_author_storage_owner_invalid");
                var count = owner == StorageRecipes.AlphaOwner
                    ? (await StorageRecipes.ReadAlphaAsync(binding, release, token)).Count
                    : (await StorageRecipes.ReadBetaAsync(binding, release, token)).Count;
                AuthorProbe.Record(owner, "compute", count, attempt);
            }
        }

        public async ValueTask<ModuleHealthResult> CheckHealthAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            AuthorProbe.Record(owner, "health", attemptId: attempt);
            if (fault == ModuleFault.IgnoreHealth)
                await Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None);
            if (fault == ModuleFault.FailHealth)
            {
                Raw(configuration);
                return new(ModuleHealth.Failed, ModuleHealthReason.Unavailable);
            }
            return new(ModuleHealth.Healthy);
        }

        public async ValueTask StopAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            AuthorProbe.Record(owner, "stop", attemptId: attempt);
            if (fault == ModuleFault.IgnoreStop)
                await Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None);
        }

        public ValueTask DisposeAsync()
        {
            AuthorProbe.Record(owner, "dispose", attemptId: attempt);
            if (descendant is not null)
            {
                if (!descendant.HasExited) throw new InvalidOperationException("composition_author_child_exit_unverified");
                descendant.Dispose();
            }
            return ValueTask.CompletedTask;
        }
    }
}
