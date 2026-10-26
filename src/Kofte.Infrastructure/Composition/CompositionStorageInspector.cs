using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Allocation;
using Kofte.Infrastructure.Storage.Migrations;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Composition;

public sealed partial class CompositionStorageSession
{
    public async Task InspectAsync(IReadOnlyList<ModuleDescription> enabled,
        IReadOnlyList<ModuleStorageRecipe> recipes, bool serving, CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (enabled.Count > 16 || recipes.Count > 32 || enabled.DistinctBy(owner => owner.Id).Count() != enabled.Count)
            throw new CompositionStorageException("composition_recipe_invalid");
        var owners = enabled.ToArray();
        var declarations = recipes.ToArray();
        ValidateRecipes(owners, declarations);
        using var budget = new OperationBudget(TimeSpan.FromSeconds(30), token);
        await VerifyAuthorityAsync(budget);
        var inventory = await ReadInventoryAsync(budget);
        if (inventory.RegistryVersion == 0 && owners.Any(owner => owner.Storage.Relational))
            throw new StorageException(StorageCode.VersionUnsupported);
        var persisted = inventory.Allocations.ToDictionary(row => row.ModuleId, StringComparer.Ordinal);
        var targets = new Dictionary<(ModuleId, string), string>();
        var prepareRequired = false;
        foreach (var owner in owners)
        {
            budget.Check();
            persisted.TryGetValue(owner.Id.Value, out var row);
            if (row is not null && row.Relational != owner.Storage.Relational)
                throw new CompositionStorageException("composition_storage_reconciliation_required");
            if (!owner.Storage.Relational) continue;
            foreach (var recipe in declarations.Where(recipe => recipe.Owner.Equals(owner.Id)))
            {
                var selectedProvider = recipe.Provider == installation.Provider;
                var key = selectedProvider && row is not null ? row.AllocationKey! : StorageAllocator.Allocate(owner.Id);
                if (selectedProvider && row is null && inventory.Allocations.Any(other => other.AllocationKey == key))
                    throw new CompositionStorageException("composition_storage_reconciliation_required");
                var boundary = new ModuleStorageModelBoundary(owner.Id, recipe.Provider,
                    recipe.Provider == "postgresql" ? key : null);
                IReadOnlyList<string> expected;
                try
                {
                    expected = await recipe.InspectAsync(boundary,
                        selectedProvider && row is not null ? row.History.Select(item => item.Id).ToArray() : [],
                        budget.Token);
                }
                catch (Exception error) when (error is StorageException or ArgumentException or
                    InvalidOperationException or NotSupportedException)
                {
                    throw new CompositionStorageException("composition_recipe_invalid");
                }
                targets.Add((owner.Id, recipe.Provider), expected[^1]);
                if (!selectedProvider) continue;
                if (row is null)
                {
                    prepareRequired = true;
                    continue;
                }
                var registry = new StorageRegistry(installation);
                await using (var binding = await registry.BindAsync(owner.Id, budget.Token))
                    await recipe.VerifyPhysicalAsync(binding, budget.Token);
                if (row.MigrationState != "current" ||
                    !row.History.Select(item => item.Id).SequenceEqual(expected, StringComparer.Ordinal) ||
                    row.MigrationTarget != expected[^1])
                    prepareRequired = true;
            }
        }
        await VerifyAuthorityAsync(budget);
        if (serving && prepareRequired) throw new CompositionStorageException("composition_prepare_required");
        inspectedOwners = owners;
        inspectedRecipes = declarations;
        inspectedTargets = targets;
    }

    private static void ValidateRecipes(IReadOnlyList<ModuleDescription> owners,
        IReadOnlyList<ModuleStorageRecipe> recipes)
    {
        var byId = owners.ToDictionary(owner => owner.Id);
        var pairs = new HashSet<(ModuleId, string)>();
        foreach (var recipe in recipes)
        {
            if (!byId.TryGetValue(recipe.Owner, out var owner) || !owner.Storage.Relational ||
                recipe.ContractVersion != 1 || recipe.Provider is not ("sqlite" or "postgresql") ||
                recipe.TargetMigration is not { Length: > 0 and <= 150 } ||
                recipe.TargetMigration.Any(c => c is < ' ' or > '~') ||
                !pairs.Add((recipe.Owner, recipe.Provider)))
                throw new CompositionStorageException("composition_recipe_invalid");
        }
        foreach (var owner in owners.Where(owner => owner.Storage.Relational))
            if (!pairs.Contains((owner.Id, "sqlite")) || !pairs.Contains((owner.Id, "postgresql")))
                throw new CompositionStorageException("composition_recipe_invalid");
    }

    private async Task<RecoveryInventory> ReadInventoryAsync(OperationBudget budget)
    {
        await using (var platform = installation.Provider == "sqlite"
            ? RecoverySetVerifier.OpenReadOnly(installation.PlatformPath) : await installation.OpenPlatformAsync(budget))
        {
            var runner = new RegistryMigrationRunner(installation);
            if (!await runner.HasRegistryAsync(platform, budget))
            {
                await runner.ValidateV0Async(platform, budget);
                return new(Guid.Empty, installation.Provider, 0, 0, []);
            }
        }
        return await RecoverySetVerifier.ReadInventoryAsync(installation, budget);
    }
}
