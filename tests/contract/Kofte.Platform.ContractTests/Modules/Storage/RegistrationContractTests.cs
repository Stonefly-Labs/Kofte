using Kofte.TestSupport;

namespace Kofte.Platform.ContractTests.Modules.Storage;

public abstract class RegistrationContractTests
{
    protected abstract string Provider { get; }

    [Theory]
    [InlineData("no-storage")]
    [InlineData("same-id-retry")]
    [InlineData("corrected-retry")]
    [InlineData("collision")]
    public async Task IndependentConsumerUsesOnlyPublicRegistrationContract(string scenario)
    {
        await using var scope = await StorageProviderScope.CreateAsync(Provider, TestContext.Current.CancellationToken);
        var result = await StorageConsumerHarness.RunAsync(scope, scenario, TestContext.Current.CancellationToken);
        Assert.True(result.Assertions >= 2);
        if (scenario == "same-id-retry")
        {
            var observed = result.Observations[scenario];
            Assert.Equal(3, observed.ProcessRestarts);
            Assert.Equal(6, observed.RetainedAssignments);
            Assert.Equal(6, observed.RetainedSentinels);
        }
        if (scenario == "collision")
        {
            var observed = result.Observations[scenario];
            Assert.Equal(1, observed.RegisteredOwnerConflicts);
            Assert.Equal(1, observed.RetainedAssignments);
            Assert.Equal(1, observed.RetainedSentinels);
            Assert.Equal(1, observed.PreservedInventories);
            Assert.Equal(1, observed.UnallocatedContenders);
            Assert.Equal(1, observed.UnownedTargetsPreserved);
        }
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteRegistrationContractTests : RegistrationContractTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresRegistrationContractTests : RegistrationContractTests
{
    protected override string Provider => "postgres";
}
