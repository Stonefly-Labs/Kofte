using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Composition;

public abstract class CompositionProviderTests(ITestOutputHelper output)
{
    protected abstract string Provider { get; }

    [Theory]
    [InlineData("empty-boot")]
    [InlineData("valid-dependency")]
    [InlineData("disabled-missing")]
    public async Task OwnedProviderBootPreservesInventoryAndExecutesOnlyItsIntendedPublicModules(string scenario)
    {
        var execution = await CompositionConsumerHarness.InvokeAsync(
            ["--provider", Provider, "--scenario", scenario], TestContext.Current.CancellationToken);
        output.WriteLine(execution.Stdout);
        output.WriteLine(execution.Stderr);
        var result = CompositionConsumerHarness.RequireSuccess(execution, Provider, scenario);
        foreach (var (name, minimum) in CompositionScenarioCatalog.RequiredObservations(scenario))
            Assert.True(result.Observations[scenario][name] >= minimum);
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteCompositionProviderTests(ITestOutputHelper output) : CompositionProviderTests(output)
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresCompositionProviderTests(ITestOutputHelper output) : CompositionProviderTests(output)
{
    protected override string Provider => "postgres";
}
