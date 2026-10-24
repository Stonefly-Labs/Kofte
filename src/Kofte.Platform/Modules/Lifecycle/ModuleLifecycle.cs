using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Platform.Modules.Lifecycle;

public enum ModuleRuntimeOwnership { Container, Attempt }

public sealed class ModuleRuntimeInstance
{
    public IModuleRuntime Runtime { get; }
    public ModuleRuntimeOwnership Ownership { get; }
    /// <summary>Container instances belong to their DI scope; only Attempt instances require separate host disposal.</summary>
    public ModuleRuntimeInstance(IModuleRuntime runtime, ModuleRuntimeOwnership ownership)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        if (!Enum.IsDefined(ownership)) throw new ArgumentOutOfRangeException(nameof(ownership));
        (Runtime, Ownership) = (runtime, ownership);
    }
}

public abstract class ModuleOwnerContext
{
    public int ContractVersion => 1;
    public ModuleId Owner { get; }
    public ModuleConfiguration Configuration { get; }
    public string AttemptId { get; }
    protected ModuleOwnerContext(ModuleId owner, ModuleConfiguration configuration, string attemptId)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(configuration);
        ObservationValidation.Attempt(attemptId);
        if (!owner.Equals(configuration.Owner))
            throw new ArgumentException("Invalid owner context.");
        (Owner, Configuration, AttemptId) = (owner, configuration, attemptId);
    }
}

public abstract class ModuleRegistrationContext : ModuleOwnerContext
{
    protected ModuleRegistrationContext(ModuleId owner, ModuleConfiguration configuration, string attemptId)
        : base(owner, configuration, attemptId) { }
}

public abstract class ModuleAttemptResources
{
    public int ContractVersion => 1;
    public ModuleId Owner { get; }
    protected ModuleAttemptResources(ModuleId owner) => Owner = owner ?? throw new ArgumentNullException(nameof(owner));
    public abstract void RegisterCleanup(Func<CancellationToken, ValueTask> cleanup);
}

public abstract class ModuleRuntimeContext : ModuleOwnerContext
{
    public ModuleAttemptResources Resources { get; }
    protected ModuleRuntimeContext(ModuleId owner, ModuleConfiguration configuration, string attemptId,
        ModuleAttemptResources resources) : base(owner, configuration, attemptId)
    {
        ArgumentNullException.ThrowIfNull(resources);
        if (!owner.Equals(resources.Owner)) throw new ArgumentException("Invalid resource owner.");
        Resources = resources;
    }
}

public abstract class ModuleInitializationContext : ModuleRuntimeContext
{
    protected ModuleInitializationContext(ModuleId owner, ModuleConfiguration configuration, string attemptId,
        ModuleAttemptResources resources) : base(owner, configuration, attemptId, resources) { }
}

public sealed class ModuleDependencyBinding
{
    public int ContractVersion => 1;
    public ModuleId Owner { get; }
    public string ContractId { get; }
    public SemanticVersion Version { get; }
    public object Operations { get; }
    public ModuleDependencyBinding(ModuleId owner, string contractId, SemanticVersion version, object operations)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(version);
        ArgumentNullException.ThrowIfNull(operations);
        if (contractId.Length is < 1 or > 128 || !contractId.All(c =>
            c is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '-'))
            throw new ArgumentException("Invalid dependency contract.");
        (Owner, ContractId, Version, Operations) = (owner, contractId, version, operations);
    }
}

public abstract class ModuleActivationContext : ModuleRuntimeContext
{
    public IModuleStorage? Storage { get; }
    public IReadOnlyList<ModuleDependencyBinding> Dependencies { get; }
    public IReadOnlyList<ContributionReservation> Contributions { get; }
    protected ModuleActivationContext(ModuleId owner, ModuleConfiguration configuration, string attemptId,
        ModuleAttemptResources resources, IModuleStorage? storage,
        IEnumerable<ModuleDependencyBinding> dependencies, IEnumerable<ContributionReservation> contributions)
        : base(owner, configuration, attemptId, resources)
    {
        if (storage is not null && !owner.Equals(storage.Owner)) throw new ArgumentException("Invalid storage owner.");
        var bindings = CompositionCopies.Bounded(dependencies, 64);
        var claims = CompositionCopies.Bounded(contributions, 128);
        if (bindings.Count > 64 || claims.Count > 128 || claims.Any(c => !owner.Equals(c.Owner)))
            throw new ArgumentException("Invalid activation catalog.");
        (Storage, Dependencies, Contributions) = (storage, bindings, claims);
    }
}

public enum ModuleHealth { Unknown, Healthy, Degraded, Failed }
public enum ModuleHealthReason { None, DependencyUnavailable, Unavailable }
public sealed class ModuleHealthResult
{
    public ModuleHealth Health { get; }
    public ModuleHealthReason Reason { get; }
    public ModuleHealthResult(ModuleHealth health, ModuleHealthReason reason = ModuleHealthReason.None)
    {
        if (!Enum.IsDefined(health) || !Enum.IsDefined(reason)) throw new ArgumentException("Invalid health result.");
        (Health, Reason) = (health, reason);
    }
}

/// <summary>
/// Ephemeral lifecycle only: no irreversible business effects. Initialize has no store or prerequisite access.
/// The host stops entered runtimes once, then disposes owner scopes; container-owned instances are not disposed twice.
/// </summary>
public interface IModuleRuntime
{
    ValueTask InitializeAsync(ModuleInitializationContext context, CancellationToken cancellationToken);
    ValueTask StartAsync(ModuleActivationContext context, CancellationToken cancellationToken);
    ValueTask<ModuleHealthResult> CheckHealthAsync(CancellationToken cancellationToken);
    ValueTask StopAsync(CancellationToken cancellationToken);
}
