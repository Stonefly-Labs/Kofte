using Kofte.Platform.Modules.Manifest;

namespace Kofte.Infrastructure.Storage.Migrations;

public sealed class ModuleStorageModelBoundary
{
    public ModuleId Owner { get; }
    public string Provider { get; }
    public string? Schema { get; }
    internal ModuleStorageBinding? Binding { get; }

    internal ModuleStorageModelBoundary(ModuleId owner, string provider, string? schema)
        => (Owner, Provider, Schema) = (owner, provider, schema);

    internal ModuleStorageModelBoundary(ModuleStorageBinding binding)
        => (Owner, Provider, Schema, Binding) = (binding.Owner, binding.Provider, binding.Schema, binding);
}
