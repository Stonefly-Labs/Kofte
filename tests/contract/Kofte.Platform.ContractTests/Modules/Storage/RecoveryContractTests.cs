using Kofte.TestSupport;

namespace Kofte.Platform.ContractTests.Modules.Storage;

public abstract class RecoveryContractTests
{
    protected abstract string Provider { get; }

    [Theory]
    [InlineData("interrupted-provisioning")]
    [InlineData("stopped-restore")]
    public async Task IndependentConsumerExecutesRealStoppedRecoveryAndInterruptedOwnership(string scenario)
    {
        var token = TestContext.Current.CancellationToken;
        await using var scope = await StorageProviderScope.CreateAsync(Provider, token);
        var result = await StorageConsumerHarness.RunAsync(scope, scenario, token);
        Assert.Equal(scenario, Assert.Single(result.Scenarios).Key);
        Assert.True(result.Scenarios[scenario] > 0);
    }
}

[Trait("Provider", "sqlite"), Trait("Suite", "recovery")]
public sealed class SqliteRecoveryContractTests : RecoveryContractTests
{ protected override string Provider => "sqlite"; }
[Trait("Provider", "postgres"), Trait("Suite", "recovery")]
public sealed class PostgresRecoveryContractTests : RecoveryContractTests
{ protected override string Provider => "postgres"; }
