using Kofte.Composition.TestModule;
using Kofte.Infrastructure.Composition;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Storage.IntegrationTests.Composition;

public abstract class CompositionRecipeTests
{
    protected abstract string Provider { get; }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LaterUnselectedProviderUnsafeMigrationCannotAllocateOrUpgradeEarlierAuthorOwner(bool upgrading)
    {
        await using var fixture = await RelationalCompositionFixture.CreateAsync(Provider);
        if (upgrading) await fixture.PrepareAndWriteAsync(StorageRelease.Initial);
        var before = await fixture.SnapshotAsync();
        var inventory = (await fixture.InventoryAsync()).ToJson();
        var recipes = StorageRecipes.ForRelease().ToList();
        var unselected = fixture.Installation.Provider == "sqlite" ? "postgresql" : "sqlite";
        recipes.RemoveAll(recipe => recipe.Owner == StorageRecipes.Descriptions[1].Id && recipe.Provider == unselected);
        recipes.Add(ModuleStorageRecipe.Create<UnsafeMigrationContext>(StorageRecipes.Descriptions[1].Id,
            unselected, "202610240001_Initial", (options, boundary) => new(options, boundary)));
        await using (var session = await CompositionStorageSession.OpenPrepareAsync(fixture.Installation, fixture.Token))
        {
            var error = await Assert.ThrowsAsync<CompositionStorageException>(() =>
                session.InspectAsync(StorageRecipes.Descriptions, recipes, serving: false, fixture.Token));
            Assert.Equal("composition_recipe_invalid", error.Code);
        }
        Assert.Equal(inventory, (await fixture.InventoryAsync()).ToJson());
        Assert.Equal(before, await fixture.SnapshotAsync());
        if (upgrading)
        {
            var initial = StorageRecipes.ForRelease(StorageRelease.Initial);
            await using var serve = await CompositionStorageSession.OpenReadAsync(fixture.Installation, fixture.Token);
            await serve.InspectAsync(StorageRecipes.Descriptions, initial, serving: true, fixture.Token);
            var bindings = await serve.VerifyAsync(StorageRecipes.Descriptions, initial, fixture.Token);
            await fixture.AssertOriginalSentinelsAsync(bindings, StorageRelease.Initial);
        }
    }

    [Fact]
    public async Task ServingPendingAdditiveReleaseRefusesWithoutRewritingOriginalSentinelsOrInventory()
    {
        await using var fixture = await RelationalCompositionFixture.CreateAsync(Provider);
        await fixture.PrepareAndWriteAsync(StorageRelease.Initial);
        var before = await fixture.SnapshotAsync();
        var inventory = (await fixture.InventoryAsync()).ToJson();
        var current = StorageRecipes.ForRelease();
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, fixture.Token))
        {
            var error = await Assert.ThrowsAsync<CompositionStorageException>(() =>
                session.InspectAsync(StorageRecipes.Descriptions, current, serving: true, fixture.Token));
            Assert.Equal("composition_prepare_required", error.Code);
        }
        Assert.Equal(inventory, (await fixture.InventoryAsync()).ToJson());
        Assert.Equal(before, await fixture.SnapshotAsync());
        var initial = StorageRecipes.ForRelease(StorageRelease.Initial);
        await using var serve = await CompositionStorageSession.OpenReadAsync(fixture.Installation, fixture.Token);
        await serve.InspectAsync(StorageRecipes.Descriptions, initial, serving: true, fixture.Token);
        await fixture.AssertOriginalSentinelsAsync(await serve.VerifyAsync(StorageRecipes.Descriptions,
            initial, fixture.Token), StorageRelease.Initial);
    }

    [Fact]
    public async Task RelationalV0RefusesWithoutImplicitRegistryUpgradeOrOwnerAllocation()
    {
        await using var fixture = await RelationalCompositionFixture.CreateAsync(Provider, upgrade: false);
        var before = await fixture.SnapshotAsync();
        var recipes = StorageRecipes.ForRelease();
        await using (var session = await CompositionStorageSession.OpenPrepareAsync(fixture.Installation, fixture.Token))
        {
            var error = await Assert.ThrowsAsync<StorageException>(() =>
                session.InspectAsync(StorageRecipes.Descriptions, recipes, serving: false, fixture.Token));
            Assert.Equal(StorageCode.VersionUnsupported, error.Code);
        }
        Assert.Equal(before, await fixture.SnapshotAsync());
    }

    [Fact]
    public async Task TypedBusinessOperationsRefuseAnotherOwnersAcceptedBindingWithoutWriting()
    {
        await using var fixture = await RelationalCompositionFixture.CreateAsync(Provider);
        await fixture.PrepareAndWriteAsync(StorageRelease.Current);
        var before = await fixture.SnapshotAsync();
        var recipes = StorageRecipes.ForRelease();
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, fixture.Token))
        {
            await session.InspectAsync(StorageRecipes.Descriptions, recipes, serving: true, fixture.Token);
            var bindings = await session.VerifyAsync(StorageRecipes.Descriptions, recipes, fixture.Token);
            await Assert.ThrowsAsync<ArgumentException>(() => StorageRecipes.WriteAlphaAsync(
                bindings[StorageRecipes.Descriptions[1].Id], new(99, "wrong-owner", null), token: fixture.Token));
            await Assert.ThrowsAsync<ArgumentException>(() => StorageRecipes.ReadBetaAsync(
                bindings[StorageRecipes.Descriptions[0].Id], token: fixture.Token));
            await fixture.AssertOriginalSentinelsAsync(bindings, StorageRelease.Current);
        }
        Assert.Equal(before, await fixture.SnapshotAsync());
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteCompositionRecipeTests : CompositionRecipeTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresCompositionRecipeTests : CompositionRecipeTests
{
    protected override string Provider => "postgres";
}
