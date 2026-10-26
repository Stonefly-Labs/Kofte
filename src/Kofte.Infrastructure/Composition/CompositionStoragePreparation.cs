using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Composition;

public sealed partial class CompositionStorageSession
{
    public async Task<IReadOnlyDictionary<ModuleId, ModuleStorageBinding>> PrepareAsync(
        IReadOnlyList<ModuleDescription> enabled, IReadOnlyList<ModuleStorageRecipe> recipes,
        CancellationToken token = default, Action<CompositionStorageProgress>? progress = null)
    {
        BeginOperation(enabled, recipes);
        if (maintenance is null) throw new CompositionStorageException("composition_recipe_invalid");
        await InspectAsync(enabled, recipes, serving: false, token);
        using var budget = new OperationBudget(TimeSpan.FromSeconds(30), token);
        await EnsureAuthorityAsync(token);
        var before = await ReadInventoryAsync(budget);
        var registry = new StorageRegistry(installation);
        foreach (var owner in enabled)
        {
            token.ThrowIfCancellationRequested();
            await VerifyAuthorityAsync(budget);
            progress?.Invoke(new(owner.Id, CompositionStorageTransition.Entered));
            if (before.RegistryVersion == 0 && !owner.Storage.Relational)
            {
                progress?.Invoke(new(owner.Id, CompositionStorageTransition.Completed));
                continue;
            }
            var registered = await registry.RegisterAsync(owner, maintenance, token);
            if (registered is not (StorageResult.Ready or StorageResult.NoStorage))
            {
                progress?.Invoke(new(owner.Id, CompositionStorageTransition.Failed, registered));
                throw new CompositionStorageException("composition_storage_reconciliation_required", registered);
            }
            if (!owner.Storage.Relational)
            {
                progress?.Invoke(new(owner.Id, CompositionStorageTransition.Completed, registered));
                continue;
            }
            var binding = await registry.BindAsync(owner.Id, token);
            bindings.Add(owner.Id, binding);
            var recipe = recipes.Single(recipe => recipe.Owner.Equals(owner.Id) && recipe.Provider == installation.Provider);
            var old = before.Allocations.SingleOrDefault(row => row.ModuleId == owner.Id.Value);
            if (old is { MigrationState: "current" } &&
                old.MigrationTarget == inspectedTargets[(owner.Id, installation.Provider)])
            {
                await recipe.VerifyPhysicalAsync(binding, token);
                progress?.Invoke(new(owner.Id, CompositionStorageTransition.Completed, registered));
                continue;
            }
            var result = await recipe.MigrateAsync(binding, maintenance, token);
            if (result is not StorageResult.Ready)
            {
                progress?.Invoke(new(owner.Id, CompositionStorageTransition.Failed, result));
                throw new CompositionStorageException("composition_storage_reconciliation_required", result);
            }
            progress?.Invoke(new(owner.Id, CompositionStorageTransition.Completed, result));
        }
        return PublishedBindings();
    }
}
