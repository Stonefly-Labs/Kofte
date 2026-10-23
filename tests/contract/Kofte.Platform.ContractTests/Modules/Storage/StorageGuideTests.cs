using Kofte.TestSupport;

namespace Kofte.Platform.ContractTests.Modules.Storage;

public abstract class StorageGuideTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task AllExecutesEveryNamedPositiveNegativeAndRecoveryExampleWithItsOwnAssertions()
    {
        var token = TestContext.Current.CancellationToken;
        await using var scope = await StorageProviderScope.CreateAsync(Provider, token);
        var result = await StorageConsumerHarness.RunAsync(scope, "all", token);
        Assert.Equal(StorageScenarioCatalog.Scenarios, result.Scenarios.Keys);
        Assert.All(result.Scenarios.Values, count => Assert.True(count > 0));
        Assert.Equal(result.Assertions, result.Scenarios.Values.Sum());
        Assert.Equal(3, result.Observations["same-id-retry"].ProcessRestarts);
        Assert.Equal(6, result.Observations["same-id-retry"].RetainedAssignments);
        Assert.Equal(6, result.Observations["same-id-retry"].RetainedSentinels);
        Assert.Equal(1, result.Observations["collision"].RegisteredOwnerConflicts);
        Assert.Equal(1, result.Observations["collision"].RetainedAssignments);
        Assert.Equal(1, result.Observations["collision"].RetainedSentinels);
        Assert.Equal(1, result.Observations["collision"].PreservedInventories);
        Assert.Equal(1, result.Observations["collision"].UnallocatedContenders);
        Assert.Equal(1, result.Observations["collision"].UnownedTargetsPreserved);
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteStorageGuideTests : StorageGuideTests
{ protected override string Provider => "sqlite"; }
[Trait("Provider", "postgres")]
public sealed class PostgresStorageGuideTests : StorageGuideTests
{ protected override string Provider => "postgres"; }
