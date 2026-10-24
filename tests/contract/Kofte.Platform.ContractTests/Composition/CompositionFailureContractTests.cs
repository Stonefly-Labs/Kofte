using Kofte.TestSupport;

namespace Kofte.Platform.ContractTests.Composition;

public abstract class CompositionFailureContractTests(ITestOutputHelper output)
{
    protected abstract string Provider { get; }

    [Theory]
    [InlineData("failure-correction")]
    [InlineData("bounded-stop")]
    public async Task IndependentPublicFailuresRequireTheirNamedCorrectionAndActualOwnedExit(string scenario)
    {
        var provider = Provider;
        var execution = await CompositionConsumerHarness.InvokeAsync(
            ["--provider", provider, "--scenario", scenario], TestContext.Current.CancellationToken);
        output.WriteLine(execution.Stdout);
        output.WriteLine(execution.Stderr);
        var result = CompositionConsumerHarness.RequireSuccess(execution, provider, scenario);
        foreach (var (name, minimum) in CompositionScenarioCatalog.RequiredObservations(scenario))
            Assert.True(result.Observations[scenario][name] >= minimum);
    }

    [Trait("Provider", "sqlite"), Trait("Suite", "recovery")]
    public sealed class SqliteCompositionFailureContractTests(ITestOutputHelper output) : CompositionFailureContractTests(output)
    {
        protected override string Provider => "sqlite";
    }

    [Trait("Provider", "postgres"), Trait("Suite", "recovery")]
    public sealed class PostgresCompositionFailureContractTests(ITestOutputHelper output) : CompositionFailureContractTests(output)
    {
        protected override string Provider => "postgres";
    }
}
