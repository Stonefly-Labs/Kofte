using Kofte.Platform.Modules.Lifecycle;
using Kofte.Platform.Modules.Manifest;
using Microsoft.Extensions.DependencyInjection;

namespace Kofte.Platform.Modules.Composition;

internal static class CompositionCopies
{
    internal static IReadOnlyList<T> Bounded<T>(IEnumerable<T> source, int limit)
    {
        ArgumentNullException.ThrowIfNull(source);
        var copy = new List<T>();
        using var iterator = source.GetEnumerator();
        while (copy.Count <= limit && iterator.MoveNext()) copy.Add(iterator.Current);
        return copy.AsReadOnly();
    }
}

public sealed class ModuleDeployment
{
    private readonly byte[] manifest;
    public int ContractVersion { get; }
    public ReadOnlyMemory<byte> Manifest => manifest.ToArray();
    internal ReadOnlySpan<byte> ManifestBytes => manifest;
    public IReadOnlyList<ContributionClaim> Claims { get; }
    public IReadOnlyList<ModuleConfigurationField> ConfigurationFields { get; }
    public IReadOnlyList<Type> OwnedServiceTypes { get; }
    public IModuleConfigurationValidator? ConfigurationValidator { get; }
    public Action<IServiceCollection, ModuleRegistrationContext>? ConfigureServices { get; }
    public Func<IServiceProvider, ModuleRuntimeContext, ModuleRuntimeInstance>? CreateRuntime { get; }

    public ModuleDeployment(ReadOnlySpan<byte> manifest,
        IEnumerable<ContributionClaim>? claims = null,
        IEnumerable<ModuleConfigurationField>? configurationFields = null,
        IEnumerable<Type>? ownedServiceTypes = null,
        IModuleConfigurationValidator? configurationValidator = null,
        Action<IServiceCollection, ModuleRegistrationContext>? configureServices = null,
        Func<IServiceProvider, ModuleRuntimeContext, ModuleRuntimeInstance>? createRuntime = null,
        int contractVersion = 1)
    {
        this.manifest = manifest[..Math.Min(manifest.Length, 65_537)].ToArray();
        Claims = CompositionCopies.Bounded(claims ?? [], 128);
        ConfigurationFields = CompositionCopies.Bounded(configurationFields ?? [], 128);
        OwnedServiceTypes = CompositionCopies.Bounded(ownedServiceTypes ?? [], 128);
        ConfigurationValidator = configurationValidator;
        ConfigureServices = configureServices;
        CreateRuntime = createRuntime;
        ContractVersion = contractVersion;
    }
}

public sealed record ModuleSelection(string? Id, bool Enabled);

public enum ContributionMode { Declaration, Executable }

public sealed record ContributionClaim(string? Owner, string? Kind, string? LocalName,
    string? ContractVersion = "1.0.0", ContributionMode Mode = ContributionMode.Declaration,
    string? Method = null, string? Route = null, Type? ServiceType = null);

public sealed class ContributionReservation
{
    public ModuleId Owner { get; }
    public string Kind { get; }
    public string LocalName { get; }
    public SemanticVersion Version { get; }
    public ContributionMode Mode { get; }
    public string? Method { get; }
    public string? Route { get; }
    public string? PermissionDescription { get; }
    internal ContributionReservation(ModuleId owner, ContributionClaim claim, SemanticVersion version,
        string? route, string? permissionDescription) =>
        (Owner, Kind, LocalName, Version, Mode, Method, Route, PermissionDescription) =
        (owner, claim.Kind!, claim.LocalName!, version, claim.Mode, claim.Method, route, permissionDescription);
}

public sealed record CompositionDependency(ModuleId Owner, ModuleId Prerequisite, VersionRange VersionRange);

public sealed class ResolvedComposition
{
    // Process-local identity only: not a settings digest, host admission token or authority.
    internal object Fingerprint { get; } = new();
    public int ContractVersion => 1;
    public IReadOnlyList<ModuleDescription> Deployed { get; }
    public IReadOnlyList<ModuleId> Enabled { get; }
    public IReadOnlyList<ModuleId> Disabled { get; }
    public IReadOnlyList<ModuleId> Order { get; }
    public IReadOnlyList<CompositionDependency> Dependencies { get; }
    public IReadOnlyList<ContributionReservation> Contributions { get; }

    internal ResolvedComposition(IEnumerable<ModuleDescription> deployed, IEnumerable<ModuleId> enabled,
        IEnumerable<ModuleId> disabled, IEnumerable<ModuleId> order,
        IEnumerable<CompositionDependency> dependencies, IEnumerable<ContributionReservation> contributions)
    {
        Deployed = Array.AsReadOnly(deployed.ToArray());
        Enabled = Array.AsReadOnly(enabled.ToArray());
        Disabled = Array.AsReadOnly(disabled.ToArray());
        Order = Array.AsReadOnly(order.ToArray());
        Dependencies = Array.AsReadOnly(dependencies.ToArray());
        Contributions = Array.AsReadOnly(contributions.ToArray());
    }
}

public abstract class CompositionResolution
{
    public int ContractVersion => 1;
    private CompositionResolution() { }
    public sealed class Accepted : CompositionResolution
    {
        public ResolvedComposition Composition { get; }
        internal Accepted(ResolvedComposition composition) => Composition = composition;
    }
    public sealed class Rejected : CompositionResolution
    {
        public IReadOnlyList<CompositionDiagnostic> Diagnostics { get; }
        internal Rejected(IReadOnlyList<CompositionDiagnostic> diagnostics) => Diagnostics = diagnostics;
    }
}
