namespace Kofte.Platform.Modules.Manifest;

public sealed class ModuleDescription
{
    public string SchemaVersion { get; }
    public ModuleId Id { get; }
    public SemanticVersion Version { get; }
    public string DisplayName { get; }
    public VersionRange HostContract { get; }
    public IReadOnlyList<CapabilityDeclaration> Capabilities { get; }
    public IReadOnlyList<DependencyDeclaration> Dependencies { get; }
    public IReadOnlyList<PermissionDeclaration> Permissions { get; }
    public StorageRequirements Storage { get; }

    internal ModuleDescription(ModuleId id, SemanticVersion version, string displayName,
        VersionRange hostContract, IEnumerable<CapabilityDeclaration> capabilities,
        IEnumerable<DependencyDeclaration> dependencies, IEnumerable<PermissionDeclaration> permissions,
        StorageRequirements storage)
    {
        SchemaVersion = "1";
        Id = id;
        Version = version;
        DisplayName = displayName;
        HostContract = hostContract;
        Capabilities = Array.AsReadOnly(capabilities.ToArray());
        Dependencies = Array.AsReadOnly(dependencies.ToArray());
        Permissions = Array.AsReadOnly(permissions.ToArray());
        Storage = storage;
    }
}

public sealed class CapabilityDeclaration
{
    public string Kind { get; }
    public SemanticVersion ContractVersion { get; }
    internal CapabilityDeclaration(string kind, SemanticVersion version) => (Kind, ContractVersion) = (kind, version);
}

public sealed class DependencyDeclaration
{
    public ModuleId Id { get; }
    public VersionRange VersionRange { get; }
    internal DependencyDeclaration(ModuleId id, VersionRange range) => (Id, VersionRange) = (id, range);
}

public sealed class PermissionDeclaration
{
    public string Name { get; }
    public string Description { get; }
    internal PermissionDeclaration(string name, string description) => (Name, Description) = (name, description);
}

public sealed class StorageRequirements
{
    public bool Relational { get; }
    internal StorageRequirements(bool relational) => Relational = relational;
}
