using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Composition;

public sealed partial class CompositionStorageSession
{
    public async Task<IReadOnlyDictionary<ModuleId, ModuleStorageBinding>> VerifyAsync(
        IReadOnlyList<ModuleDescription> enabled, IReadOnlyList<ModuleStorageRecipe> recipes,
        CancellationToken token = default, Action<CompositionStorageProgress>? progress = null)
    {
        BeginOperation(enabled, recipes);
        if (maintenance is not null) throw new CompositionStorageException("composition_recipe_invalid");
        await InspectAsync(enabled, recipes, serving: true, token);
        var registry = new StorageRegistry(installation);
        foreach (var owner in enabled)
        {
            token.ThrowIfCancellationRequested();
            await EnsureAuthorityAsync(token);
            progress?.Invoke(new(owner.Id, CompositionStorageTransition.Entered));
            if (!owner.Storage.Relational)
            {
                progress?.Invoke(new(owner.Id, CompositionStorageTransition.Completed));
                continue;
            }
            var binding = await registry.BindAsync(owner.Id, token);
            bindings.Add(owner.Id, binding);
            var recipe = recipes.Single(recipe => recipe.Owner.Equals(owner.Id) && recipe.Provider == installation.Provider);
            await recipe.VerifyPhysicalAsync(binding, token);
            var resolved = await binding.ResolveAsync(token);
            if (resolved is not StorageResult.Ready)
            {
                progress?.Invoke(new(owner.Id, CompositionStorageTransition.Failed, resolved));
                throw new CompositionStorageException("composition_storage_reconciliation_required", resolved);
            }
            progress?.Invoke(new(owner.Id, CompositionStorageTransition.Completed, resolved));
        }
        return PublishedBindings();
    }
}
