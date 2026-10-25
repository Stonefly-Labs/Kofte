using System.Text.Json;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Microsoft.Extensions.DependencyInjection;

namespace Kofte.Composition.TestModule.Modules;

public interface IAlphaOperations { int Calculate(int input); }
public interface IBetaOperations { int Calculate(int input); }
public interface INoStorageOperations { int Calculate(int input); }
public interface IAdditionalOperations { int Read(); }

public static class BootModules
{
    public const string Alpha = "org.example.alpha";
    public const string Beta = "org.example.beta";
    public const string NoStorage = "org.example.no-storage";
    public static string AlphaServiceName { get; } = new('s', 128);
    private static readonly Type[] additionalServices = Enumerable.Range(1, 126).Select(index =>
    {
        var marker = typeof(int);
        for (var bit = 0; bit < 7; bit++)
            marker = ((index & (1 << bit)) == 0 ? typeof(Zero<>) : typeof(One<>)).MakeGenericType(marker);
        return typeof(AdditionalOperations<>).MakeGenericType(marker);
    }).ToArray();
    public static IReadOnlyList<string> Owners { get; } = Array.AsReadOnly(new[] { Alpha, Beta, NoStorage });

    private static readonly ModuleConfigurationField alphaSeed = new("seed", ["seed"], ModuleSettingType.Number, true);
    private static readonly ModuleConfigurationField betaSeed = new("seed", ["seed"], ModuleSettingType.Number, true);
    private static readonly ModuleConfigurationField noStorageSeed = new("seed", ["seed"], ModuleSettingType.Number, true);

    public static IReadOnlyList<ModuleDeployment> Catalog(bool holdBeta)
    {
        AuthorProbe.HoldBeta = holdBeta;
        return
        [
            new(Manifest(Beta, Alpha), [Claim(Beta, typeof(IBetaOperations))], [betaSeed],
                [typeof(IBetaOperations)], new BetaValidator(),
                (services, context) =>
                {
                    AuthorProbe.Record(Beta, "register", attemptId: context.AttemptId);
                    var seed = Seed(context.Configuration, betaSeed);
                    services.AddScoped<IBetaOperations>(_ => new BetaOperations(seed, context.AttemptId));
                },
                (services, context) =>
                {
                    if (services.GetService(typeof(IAlphaOperations)) is not null)
                        throw new InvalidOperationException("composition_author_cross_owner_di");
                    AuthorProbe.Record(Beta, "cross-owner-denied", attemptId: context.AttemptId);
                    return new(new BetaRuntime((BetaOperations)services.GetRequiredService<IBetaOperations>(),
                        context.AttemptId), ModuleRuntimeOwnership.Attempt);
                }),
            new(Manifest(Alpha), [Claim(Alpha, typeof(IAlphaOperations)), .. additionalServices.Select((type, index) =>
                new ContributionClaim(Alpha, "backend-service", $"service{index + 1:D3}",
                    Mode: ContributionMode.Executable, ServiceType: type))], [alphaSeed],
                [typeof(IAlphaOperations), typeof(AlphaRuntime), .. additionalServices], new AlphaValidator(),
                (services, context) =>
                {
                    AuthorProbe.Record(Alpha, "register", attemptId: context.AttemptId);
                    var seed = Seed(context.Configuration, alphaSeed);
                    services.AddScoped<IAlphaOperations>(_ => new AlphaOperations(seed, context.AttemptId));
                    services.AddScoped<AlphaRuntime>(provider =>
                        new(provider.GetRequiredService<IAlphaOperations>(), context.AttemptId));
                    for (var index = 0; index < additionalServices.Length; index++)
                    {
                        var value = index + 1;
                        services.AddScoped(additionalServices[index], _ =>
                            Activator.CreateInstance(additionalServices[value - 1], value)!);
                    }
                },
                (services, _) => new(services.GetRequiredService<AlphaRuntime>(), ModuleRuntimeOwnership.Container)),
            new(Manifest(NoStorage), [Claim(NoStorage, typeof(INoStorageOperations))], [noStorageSeed],
                [typeof(INoStorageOperations), typeof(NoStorageRuntime)], new NoStorageValidator(),
                (services, context) =>
                {
                    AuthorProbe.Record(NoStorage, "register", attemptId: context.AttemptId);
                    var seed = Seed(context.Configuration, noStorageSeed);
                    services.AddTransient<INoStorageOperations>(_ => new NoStorageOperations(seed, context.AttemptId));
                    services.AddScoped<NoStorageRuntime>(provider =>
                        new(provider.GetRequiredService<INoStorageOperations>(), context.AttemptId));
                },
                (services, _) => new(services.GetRequiredService<NoStorageRuntime>(), ModuleRuntimeOwnership.Container))
        ];
    }

    private static ContributionClaim Claim(string owner, Type service) =>
        new(owner, "backend-service", owner == Alpha ? AlphaServiceName : "operations",
            Mode: ContributionMode.Executable, ServiceType: service);

    private static byte[] Manifest(string owner, string? dependency = null) =>
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            id = owner,
            version = "1.0.0",
            displayName = "Köfte public author fixture",
            hostContract = "1.0.0",
            capabilities = new[] { new { kind = "backend", contractVersion = "1.0.0" } },
            dependencies = dependency is null ? [] : new[] { new { id = dependency, versionRange = ">=1.0.0 <2.0.0" } },
            permissions = Array.Empty<object>(),
            storage = new { relational = false }
        });

    private static int Seed(ModuleConfiguration configuration, ModuleConfigurationField field) =>
        configuration.TryGetValue(field, out var value) && value.TryGetInt32(out var seed) && seed is >= 1 and <= 20
            ? seed : throw new InvalidOperationException("composition_author_seed_invalid");

    private abstract class SeedValidator(string owner, ModuleConfigurationField field) : IModuleConfigurationValidator
    {
        public IEnumerable<ModuleConfigurationFailure> Validate(ModuleConfiguration configuration, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            AuthorProbe.Record(owner, "validate");
            return configuration.TryGetValue(field, out var value) && value.TryGetInt32(out var seed) && seed is >= 1 and <= 20
                ? [] : [new(field, ModuleSettingFailureReason.Constraint)];
        }
    }
    private sealed class AlphaValidator() : SeedValidator(Alpha, alphaSeed);
    private sealed class BetaValidator() : SeedValidator(Beta, betaSeed);
    private sealed class NoStorageValidator() : SeedValidator(NoStorage, noStorageSeed);

    private sealed class Zero<T>;
    private sealed class One<T>;
    private sealed class AdditionalOperations<T>(int value) : IAdditionalOperations
    {
        public int Read() => value;
    }

    private sealed class AlphaOperations : IAlphaOperations
    {
        private readonly int seed;
        private readonly string attempt;
        public AlphaOperations(int seed, string attempt)
        {
            (this.seed, this.attempt) = (seed, attempt);
            AuthorProbe.Record(Alpha, "service-constructor", attemptId: attempt);
        }
        public int Calculate(int input)
        {
            var result = checked(seed + input * 3);
            AuthorProbe.Record(Alpha, "compute", result, attempt);
            return result;
        }
    }

    private sealed class BetaOperations : IBetaOperations
    {
        private readonly int seed;
        private readonly string attempt;
        public IAlphaOperations? Prerequisite { get; set; }
        public BetaOperations(int seed, string attempt)
        {
            (this.seed, this.attempt) = (seed, attempt);
            AuthorProbe.Record(Beta, "service-constructor", attemptId: attempt);
        }
        public int Calculate(int input)
        {
            var result = checked((Prerequisite ?? throw new InvalidOperationException("composition_author_binding_missing"))
                .Calculate(input) * 2 + seed);
            AuthorProbe.Record(Beta, "compute", result, attempt);
            return result;
        }
    }

    private sealed class NoStorageOperations : INoStorageOperations
    {
        private readonly int seed;
        private readonly string attempt;
        public NoStorageOperations(int seed, string attempt)
        {
            (this.seed, this.attempt) = (seed, attempt);
            AuthorProbe.Record(NoStorage, "service-constructor", attemptId: attempt);
        }
        public int Calculate(int input)
        {
            var result = checked(seed - input);
            AuthorProbe.Record(NoStorage, "compute", result, attempt);
            return result;
        }
    }

    private abstract class Runtime : IModuleRuntime, IAsyncDisposable
    {
        protected readonly string Owner;
        protected readonly string Attempt;
        protected Runtime(string owner, string attempt)
        {
            (Owner, Attempt) = (owner, attempt);
            AuthorProbe.Record(owner, "runtime-constructor", attemptId: attempt);
        }
        public ValueTask InitializeAsync(ModuleInitializationContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            AuthorProbe.Record(Owner, "initialize", attemptId: context.AttemptId);
            context.Resources.RegisterCleanup(_ =>
            {
                AuthorProbe.Record(Owner, "cleanup", attemptId: Attempt);
                return ValueTask.CompletedTask;
            });
            return ValueTask.CompletedTask;
        }
        public abstract ValueTask StartAsync(ModuleActivationContext context, CancellationToken token);
        protected void Started(ModuleActivationContext context)
        {
            if (context.Storage is not null) throw new InvalidOperationException("composition_author_unexpected_storage");
            AuthorProbe.Record(Owner, "no-storage", attemptId: context.AttemptId);
            AuthorProbe.Record(Owner, "start", attemptId: context.AttemptId);
        }
        public ValueTask<ModuleHealthResult> CheckHealthAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            AuthorProbe.Record(Owner, "health", attemptId: Attempt);
            return ValueTask.FromResult(new ModuleHealthResult(ModuleHealth.Healthy));
        }
        public ValueTask StopAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            AuthorProbe.Record(Owner, "stop", attemptId: Attempt);
            return ValueTask.CompletedTask;
        }
        public ValueTask DisposeAsync()
        {
            AuthorProbe.Record(Owner, "dispose", attemptId: Attempt);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class AlphaRuntime(IAlphaOperations operations, string attempt) : Runtime(Alpha, attempt)
    {
        public override ValueTask StartAsync(ModuleActivationContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (context.Dependencies.Count != 0) throw new InvalidOperationException("composition_author_unexpected_binding");
            _ = operations.Calculate(5);
            Started(context);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class BetaRuntime(BetaOperations operations, string attempt) : Runtime(Beta, attempt)
    {
        public override async ValueTask StartAsync(ModuleActivationContext context, CancellationToken token)
        {
            if (context.Dependencies.Count != 127 || context.Dependencies.Any(binding =>
                binding.Owner.Value != Alpha || binding.ContractVersion != 1 || binding.Version.ToString() != "1.0.0"))
                throw new InvalidOperationException("composition_author_binding_count_invalid");
            for (var index = 1; index <= 126; index++)
            {
                var exported = context.Dependencies.Single(binding => binding.ContractId == $"{Alpha}.service{index:D3}");
                if (exported.Operations is not IAdditionalOperations additional || additional.Read() != index)
                    throw new InvalidOperationException("composition_author_binding_operation_invalid");
            }
            var binding = context.Dependencies.Single(binding => binding.ContractId == Alpha + "." + AlphaServiceName);
            if (binding.Owner.Value != Alpha || binding.ContractId != Alpha + "." + AlphaServiceName ||
                binding.ContractVersion != 1 || binding.Version.ToString() != "1.0.0" ||
                binding.Operations is not IAlphaOperations alpha)
                throw new InvalidOperationException("composition_author_binding_invalid");
            operations.Prerequisite = alpha;
            AuthorProbe.Record(Beta, "dependency-bound", attemptId: context.AttemptId);
            AuthorProbe.Record(Beta, "boundary-dependency-bound", binding.ContractId.Length, context.AttemptId);
            AuthorProbe.Record(Beta, "complete-dependencies-bound", context.Dependencies.Count, context.AttemptId);
            _ = operations.Calculate(5);
            if (AuthorProbe.HoldBeta)
            {
                AuthorProbe.Record(Beta, "start-held", attemptId: context.AttemptId);
                await AuthorProbe.WaitForReleaseAsync(token);
            }
            Started(context);
        }
    }

    private sealed class NoStorageRuntime(INoStorageOperations operations, string attempt) : Runtime(NoStorage, attempt)
    {
        public override ValueTask StartAsync(ModuleActivationContext context, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (context.Dependencies.Count != 0) throw new InvalidOperationException("composition_author_unexpected_binding");
            _ = operations.Calculate(5);
            Started(context);
            return ValueTask.CompletedTask;
        }
    }
}
