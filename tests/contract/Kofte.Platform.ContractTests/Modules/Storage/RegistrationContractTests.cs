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
