using Kofte.TestSupport;

namespace Kofte.Platform.ContractTests.Composition;

public abstract class CompositionHealthContractTests(ITestOutputHelper output)
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task IndependentHealthConsumerProvesCurrentOwnTransitiveAndAnonymousHealth()
    {
        var provider = Provider;
        var execution = await CompositionConsumerHarness.InvokeAsync(
            ["--provider", provider, "--scenario", "health-impact"], TestContext.Current.CancellationToken);
        output.WriteLine(execution.Stdout);
        output.WriteLine(execution.Stderr);
        var result = CompositionConsumerHarness.RequireSuccess(execution, provider, "health-impact");
        foreach (var (name, minimum) in CompositionScenarioCatalog.RequiredObservations("health-impact"))
            Assert.True(result.Observations["health-impact"][name] >= minimum);
    }

    [Trait("Provider", "sqlite")]
    public sealed class SqliteCompositionHealthContractTests(ITestOutputHelper output) : CompositionHealthContractTests(output)
    {
        protected override string Provider => "sqlite";
    }

    [Trait("Provider", "postgres")]
    public sealed class PostgresCompositionHealthContractTests(ITestOutputHelper output) : CompositionHealthContractTests(output)
    {
        protected override string Provider => "postgres";
    }
}
