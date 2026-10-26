using Kofte.Platform.Modules.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kofte.Infrastructure.Storage.Migrations;

public abstract class AssignedModelSnapshot : ModelSnapshot
{
    private ModuleStorageModelBoundary? binding;
    protected string? AssignedSchema => Binding.Schema;
    protected string AssignedProvider => Binding.Provider;
    private ModuleStorageModelBoundary Binding => binding ??
        throw new StorageException(StorageCode.RequestInvalid);

    internal void Bind(ModuleStorageModelBoundary value)
    {
        if (binding is not null && !ReferenceEquals(binding, value))
            throw new StorageException(StorageCode.OwnerMismatch);
        binding = value;
    }
}
