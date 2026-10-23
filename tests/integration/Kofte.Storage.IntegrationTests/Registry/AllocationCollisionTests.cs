using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Storage.IntegrationTests.Registry;

public abstract class AllocationCollisionTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task ForcedCandidateCollisionPreservesOriginalSentinelAndAssignment()
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, TestContext.Current.CancellationToken);
        var alpha = RegistryFixture.Module("org.example.alpha");
        await using var scope = await fixture.Maintenance.AcquireAsync(TestContext.Current.CancellationToken);
        var original = Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(
            alpha, scope, TestContext.Current.CancellationToken)).Assignment;
        await RegistrySentinel.WriteAsync(fixture, original, "outside-owner-preserved");
        var forced = new StorageRegistry(fixture.Installation, _ => original.AllocationKey);
        var result = await forced.RegisterAsync(RegistryFixture.Module("org.example.beta"),
            scope, TestContext.Current.CancellationToken);
        Assert.Equal(StorageOutcome.Rejected, result.Outcome);
        Assert.Equal("storage_conflict", Assert.Single(result.Diagnostics).Code);
        Assert.Equal("outside-owner-preserved", await RegistrySentinel.ReadAsync(fixture, original));
        var resolved = Assert.IsType<StorageResult.Ready>(await fixture.Registry.ResolveAsync(
            alpha.Id, TestContext.Current.CancellationToken)).Assignment;
        Assert.Equal(original.AllocationId, resolved.AllocationId);
        Assert.Equal(original.AllocationKey, resolved.AllocationKey);
        Assert.Equal(original.Revision, resolved.Revision);
        await fixture.AssertInventoryAsync(1, 1, TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("public")]
    [InlineData("kofte_system")]
    [InlineData("pg_catalog")]
    [InlineData("../platform/platform.db")]
    public async Task ForcedReservedOrNoncanonicalCandidateCannotReserveOrCreateAnything(string key)
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, TestContext.Current.CancellationToken);
        await using var scope = await fixture.Maintenance.AcquireAsync(TestContext.Current.CancellationToken);
        var forced = new StorageRegistry(fixture.Installation, _ => key);
        var result = await forced.RegisterAsync(RegistryFixture.Module("org.example.alpha"),
            scope, TestContext.Current.CancellationToken);
        Assert.Equal(StorageOutcome.Rejected, result.Outcome);
        Assert.Equal(StorageDurability.Unchanged, result.Durability);
        await fixture.AssertInventoryAsync(0, 0, TestContext.Current.CancellationToken);
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteAllocationCollisionTests : AllocationCollisionTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresAllocationCollisionTests : AllocationCollisionTests
{
    protected override string Provider => "postgres";
}
