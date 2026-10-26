using Kofte.Composition.TestModule;
using Kofte.Infrastructure.Composition;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Storage.IntegrationTests.Composition;

public abstract class CompositionUpgradePreservationTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task ExplicitSingleOwnerUpgradeAdvancesOnlyItsTargetAndRetainsDisabledOwnerOriginalState()
    {
        await using var fixture = await RelationalCompositionFixture.CreateAsync(Provider);
        await fixture.PrepareAndWriteAsync(StorageRelease.Initial);
        var original = await fixture.InventoryAsync();
        var assignments = await fixture.AssignmentsAsync();
        var owner = StorageRecipes.Descriptions[0];
        var recipes = StorageRecipes.ForRelease().Where(recipe => recipe.Owner.Equals(owner.Id)).ToArray();
        await using (var prepare = await CompositionStorageSession.OpenPrepareAsync(fixture.Installation, fixture.Token))
        {
            await prepare.InspectAsync([owner], recipes, serving: false, fixture.Token);
            var binding = Assert.Single(await prepare.PrepareAsync([owner], recipes, fixture.Token)).Value;
            Assert.Equal([RelationalCompositionFixture.AlphaOriginal],
                await StorageRecipes.ReadAlphaAsync(binding, token: fixture.Token));
        }
        var upgraded = await fixture.InventoryAsync();
        Assert.Equal(original.InstallationId, upgraded.InstallationId);
        Assert.Equal(original.RegistryRevision + 2, upgraded.RegistryRevision);
        Assert.Equal(original.Allocations.Where(row => row.ModuleId != owner.Id.Value).ToArray().ToJson(),
            upgraded.Allocations.Where(row => row.ModuleId != owner.Id.Value).ToArray().ToJson());
        var alpha = Assert.Single(upgraded.Allocations, row => row.ModuleId == owner.Id.Value);
        Assert.Equal([StorageRecipes.AlphaInitial, StorageRecipes.AlphaCurrent],
            alpha.History.Select(migration => migration.Id));
        var after = await fixture.AssignmentsAsync();
        foreach (var (id, before) in assignments)
        {
            var current = after[id];
            Assert.Equal(before.Owner, current.Owner);
            Assert.Equal(before.Provider, current.Provider);
            Assert.Equal(before.InstallationId, current.InstallationId);
            Assert.Equal(before.AllocationId, current.AllocationId);
            Assert.Equal(before.AllocationKey, current.AllocationKey);
            Assert.Equal(before.AllocatorVersion, current.AllocatorVersion);
            Assert.Equal(before.Revision + (id.Equals(owner.Id) ? 2 : 0), current.Revision);
        }
        var registry = new StorageRegistry(fixture.Installation);
        await using (var beta = await registry.BindAsync(StorageRecipes.Descriptions[1].Id, fixture.Token))
            Assert.Equal([RelationalCompositionFixture.BetaOriginal],
                await StorageRecipes.ReadBetaAsync(beta, StorageRelease.Initial, fixture.Token));
        var currentSnapshot = await fixture.SnapshotAsync();
        await using (var repeated = await CompositionStorageSession.OpenPrepareAsync(fixture.Installation, fixture.Token))
        {
            await repeated.InspectAsync([owner], recipes, serving: false, fixture.Token);
            Assert.Single(await repeated.PrepareAsync([owner], recipes, fixture.Token));
        }
        Assert.Equal(currentSnapshot, await fixture.SnapshotAsync());
        await using (var serving = await CompositionStorageSession.OpenReadAsync(fixture.Installation, fixture.Token))
        {
            await serving.InspectAsync([owner], recipes, serving: true, fixture.Token);
            var binding = Assert.Single(await serving.VerifyAsync([owner], recipes, fixture.Token)).Value;
            Assert.Equal([RelationalCompositionFixture.AlphaOriginal],
                await StorageRecipes.ReadAlphaAsync(binding, token: fixture.Token));
        }
        Assert.Equal(currentSnapshot, await fixture.SnapshotAsync());
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteCompositionUpgradePreservationTests : CompositionUpgradePreservationTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresCompositionUpgradePreservationTests : CompositionUpgradePreservationTests
{
    protected override string Provider => "postgres";
}
