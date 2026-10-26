using System.Text.Json;
using Kofte.Composition.TestModule;
using Kofte.Infrastructure.Composition;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Composition;

public abstract class CompositionMigrationTests
{
    protected abstract string Provider { get; }

    [Theory]
    [InlineData(StorageRelease.Initial)]
    [InlineData(StorageRelease.Current)]
    public async Task CleanReleaseCreatesExactOwnerHistoriesWithoutBusinessWrites(StorageRelease release)
    {
        await using var fixture = await RelationalCompositionFixture.CreateAsync(Provider);
        var token = fixture.Token;
        var before = await fixture.InventoryAsync();
        Assert.Empty(before.Allocations);
        var descriptions = StorageRecipes.Descriptions;
        Assert.Equal([StorageRecipes.AlphaOwner, StorageRecipes.BetaOwner, StorageRecipes.NoStorageOwner],
            descriptions.Select(owner => owner.Id.Value));
        Assert.Equal(descriptions[0].Id, Assert.Single(descriptions[1].Dependencies).Id);
        var recipes = StorageRecipes.ForRelease(release);
        await using (var session = await CompositionStorageSession.OpenPrepareAsync(fixture.Installation, token))
        {
            await session.InspectAsync(descriptions, recipes, serving: false, token);
            var bindings = await session.PrepareAsync(descriptions, recipes, token);
            Assert.Equal([descriptions[0].Id, descriptions[1].Id], bindings.Keys);
            Assert.Empty(await StorageRecipes.ReadAlphaAsync(bindings[descriptions[0].Id], release, token));
            Assert.Empty(await StorageRecipes.ReadBetaAsync(bindings[descriptions[1].Id], release, token));
            foreach (var description in descriptions.Take(2))
            {
                var binding = bindings[description.Id];
                Assert.Equal(description.Id, binding.Owner);
                Assert.Equal(fixture.Installation.Provider, binding.Provider);
                Assert.Equal(fixture.Installation.Provider == "postgresql"
                    ? (await RelationalCompositionFixture.AssignmentAsync(binding, token)).AllocationKey : null,
                    binding.Schema);
            }
        }
        var after = await fixture.InventoryAsync();
        Assert.Equal(before.InstallationId, after.InstallationId);
        Assert.Equal(before.Provider, after.Provider);
        Assert.Equal(1, after.RegistryVersion);
        Assert.Equal(before.RegistryRevision + 9, after.RegistryRevision);
        AssertRelease(after, release, revision: 4);
        var noStorage = Assert.Single(after.Allocations, row => row.ModuleId == StorageRecipes.NoStorageOwner);
        Assert.Equal(new RecoveryAllocation(StorageRecipes.NoStorageOwner, false, null, null, null, null,
            "no_storage", "none", null, null, 1, []).ToJson(), noStorage.ToJson());
        Assert.IsType<StorageResult.NoStorage>(await new StorageRegistry(fixture.Installation)
            .ResolveAsync(descriptions[2].Id, token));
    }

    [Fact]
    public async Task AdditiveReleasePreservesEveryAssignmentAndOriginalOwnerSentinel()
    {
        await using var fixture = await RelationalCompositionFixture.CreateAsync(Provider);
        await fixture.PrepareAndWriteAsync(StorageRelease.Initial);
        var before = await fixture.InventoryAsync();
        var originalAssignments = await fixture.AssignmentsAsync();
        var recipes = StorageRecipes.ForRelease();
        await using (var session = await CompositionStorageSession.OpenPrepareAsync(fixture.Installation, fixture.Token))
        {
            await session.InspectAsync(StorageRecipes.Descriptions, recipes, serving: false, fixture.Token);
            var bindings = await session.PrepareAsync(StorageRecipes.Descriptions, recipes, fixture.Token);
            Assert.Equal([RelationalCompositionFixture.AlphaOriginal],
                await StorageRecipes.ReadAlphaAsync(bindings[StorageRecipes.Descriptions[0].Id], token: fixture.Token));
            Assert.Equal([RelationalCompositionFixture.BetaOriginal],
                await StorageRecipes.ReadBetaAsync(bindings[StorageRecipes.Descriptions[1].Id], token: fixture.Token));
            await StorageRecipes.WriteAlphaAsync(bindings[StorageRecipes.Descriptions[0].Id],
                new(22, "alpha-after-upgrade", "alpha-note"), token: fixture.Token);
            await StorageRecipes.WriteBetaAsync(bindings[StorageRecipes.Descriptions[1].Id],
                new(33, "beta-after-upgrade", "beta-label"), token: fixture.Token);
        }
        var after = await fixture.InventoryAsync();
        AssertRelease(after, StorageRelease.Current, revision: 6);
        Assert.Equal(before.InstallationId, after.InstallationId);
        Assert.Equal(before.RegistryRevision + 4, after.RegistryRevision);
        Assert.Equal(before.Allocations.Single(row => !row.Relational).ToJson(),
            after.Allocations.Single(row => !row.Relational).ToJson());
        var assignments = await fixture.AssignmentsAsync();
        foreach (var (owner, original) in originalAssignments)
        {
            AssertAssignment(original, assignments[owner], original.Revision + 2);
            var oldRow = before.Allocations.Single(row => row.ModuleId == owner.Value);
            var upgraded = after.Allocations.Single(row => row.ModuleId == owner.Value);
            Assert.Equal(oldRow.AllocationKey, upgraded.AllocationKey);
            Assert.Equal(oldRow.AllocationId, upgraded.AllocationId);
            Assert.Equal(oldRow.AllocatorVersion, upgraded.AllocatorVersion);
            Assert.Equal(oldRow.MarkerVersion, upgraded.MarkerVersion);
            Assert.Equal(oldRow.History.ToJson(), upgraded.History.Take(1).ToArray().ToJson());
        }
        await using var read = await CompositionStorageSession.OpenReadAsync(fixture.Installation, fixture.Token);
        await read.InspectAsync(StorageRecipes.Descriptions, recipes, serving: true, fixture.Token);
        var current = await read.VerifyAsync(StorageRecipes.Descriptions, recipes, fixture.Token);
        Assert.Equal([RelationalCompositionFixture.AlphaOriginal, new(22, "alpha-after-upgrade", "alpha-note")],
            await StorageRecipes.ReadAlphaAsync(current[StorageRecipes.Descriptions[0].Id], token: fixture.Token));
        Assert.Equal([RelationalCompositionFixture.BetaOriginal, new(33, "beta-after-upgrade", "beta-label")],
            await StorageRecipes.ReadBetaAsync(current[StorageRecipes.Descriptions[1].Id], token: fixture.Token));
    }

    [Fact]
    public async Task CurrentPrepareAndServingAliasesDoNotRewriteAnyStoreOrAssignment()
    {
        await using var fixture = await RelationalCompositionFixture.CreateAsync(Provider);
        await fixture.PrepareAndWriteAsync(StorageRelease.Current);
        var before = await fixture.SnapshotAsync();
        var inventory = (await fixture.InventoryAsync()).ToJson();
        var assignments = await fixture.AssignmentsAsync();
        var aliases = StorageRecipes.ForRelease(aliases: true);
        await using (var prepare = await CompositionStorageSession.OpenPrepareAsync(fixture.Installation, fixture.Token))
        {
            await prepare.InspectAsync(StorageRecipes.Descriptions, aliases, serving: false, fixture.Token);
            var bindings = await prepare.PrepareAsync(StorageRecipes.Descriptions, aliases, fixture.Token);
            await fixture.AssertOriginalSentinelsAsync(bindings, StorageRelease.Current);
        }
        Assert.Equal(before, await fixture.SnapshotAsync());
        await using (var serve = await CompositionStorageSession.OpenReadAsync(fixture.Installation, fixture.Token))
        {
            await serve.InspectAsync(StorageRecipes.Descriptions, aliases, serving: true, fixture.Token);
            var bindings = await serve.VerifyAsync(StorageRecipes.Descriptions, aliases, fixture.Token);
            await fixture.AssertOriginalSentinelsAsync(bindings, StorageRelease.Current);
            foreach (var (owner, assignment) in assignments)
                AssertAssignment(assignment, await RelationalCompositionFixture.AssignmentAsync(bindings[owner],
                    fixture.Token), assignment.Revision);
        }
        Assert.Equal(inventory, (await fixture.InventoryAsync()).ToJson());
        Assert.Equal(before, await fixture.SnapshotAsync());
    }

    [Fact]
    public async Task NoStorageOnlyV0PrepareAndServeNeedNoRegistryUpgradeOrBinding()
    {
        await using var fixture = await RelationalCompositionFixture.CreateAsync(Provider, upgrade: false);
        var before = await fixture.SnapshotAsync();
        var owners = new[] { StorageRecipes.Descriptions[2] };
        await using (var prepare = await CompositionStorageSession.OpenPrepareAsync(fixture.Installation, fixture.Token))
        {
            await prepare.InspectAsync(owners, [], serving: false, fixture.Token);
            Assert.Empty(await prepare.PrepareAsync(owners, [], fixture.Token));
        }
        await using (var serve = await CompositionStorageSession.OpenReadAsync(fixture.Installation, fixture.Token))
        {
            await serve.InspectAsync(owners, [], serving: true, fixture.Token);
            Assert.Empty(await serve.VerifyAsync(owners, [], fixture.Token));
        }
        Assert.Equal(before, await fixture.SnapshotAsync());
    }

    private static void AssertRelease(RecoveryInventory inventory, StorageRelease release, long revision)
    {
        Assert.Equal(3, inventory.Allocations.Count);
        foreach (var (owner, initial, current) in new[]
        {
            (StorageRecipes.AlphaOwner, StorageRecipes.AlphaInitial, StorageRecipes.AlphaCurrent),
            (StorageRecipes.BetaOwner, StorageRecipes.BetaInitial, StorageRecipes.BetaCurrent)
        })
        {
            var row = Assert.Single(inventory.Allocations, row => row.ModuleId == owner);
            Assert.True(row.Relational);
            Assert.Equal("ready", row.State);
            Assert.Equal("current", row.MigrationState);
            Assert.Equal(release == StorageRelease.Initial ? initial : current, row.MigrationTarget);
            Assert.Equal(row.MigrationTarget, row.MigrationCheckpoint);
            Assert.Equal(revision, row.Revision);
            Assert.Equal(1, row.AllocatorVersion);
            Assert.Equal(1, row.MarkerVersion);
            Assert.NotEqual(Guid.Empty, row.AllocationId);
            Assert.False(string.IsNullOrWhiteSpace(row.AllocationKey));
            Assert.Equal(release == StorageRelease.Initial
                ? new[] { new RecoveryMigration(initial, "10.0.13") }
                : [new RecoveryMigration(initial, "10.0.13"), new(current, "10.0.13")], row.History);
        }
    }

    private static void AssertAssignment(StorageAssignment expected, StorageAssignment actual, long revision)
    {
        Assert.Equal(expected.ContractVersion, actual.ContractVersion);
        Assert.Equal(expected.Owner, actual.Owner);
        Assert.Equal(expected.Provider, actual.Provider);
        Assert.Equal(expected.InstallationId, actual.InstallationId);
        Assert.Equal(expected.AllocationId, actual.AllocationId);
        Assert.Equal(expected.AllocationKey, actual.AllocationKey);
        Assert.Equal(expected.AllocatorVersion, actual.AllocatorVersion);
        Assert.Equal(revision, actual.Revision);
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteCompositionMigrationTests : CompositionMigrationTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresCompositionMigrationTests : CompositionMigrationTests
{
    protected override string Provider => "postgres";
}

internal sealed class RelationalCompositionFixture(StorageProviderScope resources) : IAsyncDisposable
{
    internal static readonly AlphaSentinel AlphaOriginal = new(11, "alpha-original-sentinel", null);
    internal static readonly BetaSentinel BetaOriginal = new(11, "beta-original-sentinel", null);
    internal StorageInstallation Installation { get; } = resources.Postgres is null
        ? StorageInstallation.Sqlite(resources.DataRoot)
        : StorageInstallation.PostgreSql(resources.Postgres.ConnectionString,
            resources.Postgres.ProvisioningConnectionString);
    internal CancellationToken Token => resources.Token;

    internal static async Task<RelationalCompositionFixture> CreateAsync(string provider, bool upgrade = true)
    {
        var fixture = new RelationalCompositionFixture(
            await StorageProviderScope.CreateAsync(provider, TestContext.Current.CancellationToken));
        try
        {
            if (upgrade)
            {
                var maintenance = new StorageMaintenance(fixture.Installation);
                await using var scope = await maintenance.AcquireAsync(fixture.Token);
                await maintenance.UpgradeAsync(scope, fixture.Token);
            }
            return fixture;
        }
        catch { await fixture.DisposeAsync(); throw; }
    }

    internal Task<string> SnapshotAsync() => CompositionStoreSnapshot.CaptureAsync(resources, Token);

    internal async Task<RecoveryInventory> InventoryAsync()
    {
        await using var scope = await new StorageMaintenance(Installation).AcquireAsync(Token);
        return await RecoverySetVerifier.InspectStoppedAsync(Installation, scope, Token);
    }

    internal async Task PrepareAndWriteAsync(StorageRelease release)
    {
        var recipes = StorageRecipes.ForRelease(release);
        await using var session = await CompositionStorageSession.OpenPrepareAsync(Installation, Token);
        await session.InspectAsync(StorageRecipes.Descriptions, recipes, serving: false, Token);
        var bindings = await session.PrepareAsync(StorageRecipes.Descriptions, recipes, Token);
        await StorageRecipes.WriteAlphaAsync(bindings[StorageRecipes.Descriptions[0].Id], AlphaOriginal, release, Token);
        await StorageRecipes.WriteBetaAsync(bindings[StorageRecipes.Descriptions[1].Id], BetaOriginal, release, Token);
    }

    internal async Task AssertOriginalSentinelsAsync(IReadOnlyDictionary<ModuleId, ModuleStorageBinding> bindings,
        StorageRelease release)
    {
        Assert.Equal([AlphaOriginal],
            await StorageRecipes.ReadAlphaAsync(bindings[StorageRecipes.Descriptions[0].Id], release, Token));
        Assert.Equal([BetaOriginal],
            await StorageRecipes.ReadBetaAsync(bindings[StorageRecipes.Descriptions[1].Id], release, Token));
        Assert.False(bindings.ContainsKey(StorageRecipes.Descriptions[2].Id));
    }

    internal async Task<IReadOnlyDictionary<ModuleId, StorageAssignment>> AssignmentsAsync()
    {
        var registry = new StorageRegistry(Installation);
        var result = new Dictionary<ModuleId, StorageAssignment>();
        foreach (var owner in StorageRecipes.Descriptions.Take(2))
            result.Add(owner.Id, Assert.IsType<StorageResult.Ready>(await registry.ResolveAsync(owner.Id, Token)).Assignment);
        return result;
    }

    internal static async Task<StorageAssignment> AssignmentAsync(ModuleStorageBinding binding, CancellationToken token) =>
        Assert.IsType<StorageResult.Ready>(await binding.ResolveAsync(token)).Assignment;

    public ValueTask DisposeAsync() => resources.DisposeAsync();
}

internal static class CompositionEvidenceJson
{
    internal static string ToJson<T>(this T value) => JsonSerializer.Serialize(value);
}
