using Kofte.TestSupport;

namespace Kofte.Platform.ContractTests.Modules.Storage;

public abstract class ModuleContractTests
{
    protected abstract string Provider { get; }

    [Theory]
    [InlineData("clean-install")]
    [InlineData("isolated-upgrade")]
    [InlineData("path-rejection")]
    public async Task IndependentModuleConsumersUseOnlyAssignedPublicStorage(string scenario)
    {
        var token = TestContext.Current.CancellationToken;
        await using var scope = await StorageProviderScope.CreateAsync(Provider, token);
        var result = await StorageConsumerHarness.RunAsync(scope, scenario, token);
        Assert.Equal(Provider, result.Provider);
        Assert.Equal(scenario, result.Scenario);
        Assert.True(result.Assertions >= 8);
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteModuleContractTests : ModuleContractTests
{ protected override string Provider => "sqlite"; }
[Trait("Provider", "postgres")]
public sealed class PostgresModuleContractTests : ModuleContractTests
{ protected override string Provider => "postgres"; }
