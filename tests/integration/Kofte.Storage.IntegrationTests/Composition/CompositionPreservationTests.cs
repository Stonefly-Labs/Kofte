using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Composition;

public abstract class CompositionPreservationTests(ITestOutputHelper output)
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task ThreeStoppedSelectionCyclesPreserveExactOriginalStoresAndUseFreshProcesses()
    {
        var execution = await CompositionConsumerHarness.InvokeAsync(
            ["--provider", Provider, "--scenario", "disable-reenable"], TestContext.Current.CancellationToken);
        output.WriteLine(execution.Stdout);
        output.WriteLine(execution.Stderr);
        var result = CompositionConsumerHarness.RequireSuccess(execution, Provider, "disable-reenable");
        foreach (var (name, minimum) in CompositionScenarioCatalog.RequiredObservations("disable-reenable"))
            Assert.True(result.Observations["disable-reenable"][name] >= minimum);
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteCompositionPreservationTests(ITestOutputHelper output) : CompositionPreservationTests(output)
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresCompositionPreservationTests(ITestOutputHelper output) : CompositionPreservationTests(output)
{
    protected override string Provider => "postgres";
}
