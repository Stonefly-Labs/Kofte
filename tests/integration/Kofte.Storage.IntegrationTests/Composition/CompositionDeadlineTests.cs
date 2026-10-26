using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Composition;

[Trait("Feature", "composition")]
public abstract class CompositionDeadlineTests(ITestOutputHelper output)
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task PublicFaultMatrixVerifiesActualExitBudgetsPartialCleanupAndOwnedDescendants()
    {
        var execution = await CompositionConsumerHarness.InvokeAsync(
            ["--provider", Provider, "--scenario", "bounded-stop"], TestContext.Current.CancellationToken);
        output.WriteLine(execution.Stdout);
        output.WriteLine(execution.Stderr);
        var result = CompositionConsumerHarness.RequireSuccess(execution, Provider, "bounded-stop");
        foreach (var (name, minimum) in CompositionScenarioCatalog.RequiredObservations("bounded-stop"))
            Assert.True(result.Observations["bounded-stop"][name] >= minimum);
    }

    [Fact]
    public async Task PublicCorrectionsRequireFreshAttemptsAndExplicitUncertainMigrationRecovery()
    {
        var execution = await CompositionConsumerHarness.InvokeAsync(
            ["--provider", Provider, "--scenario", "failure-correction"], TestContext.Current.CancellationToken);
        output.WriteLine(execution.Stdout);
        output.WriteLine(execution.Stderr);
        var result = CompositionConsumerHarness.RequireSuccess(execution, Provider, "failure-correction");
        foreach (var (name, minimum) in CompositionScenarioCatalog.RequiredObservations("failure-correction"))
            Assert.True(result.Observations["failure-correction"][name] >= minimum);
    }
}

[Trait("Provider", "sqlite")]
[Trait("Suite", "recovery")]
public sealed class SqliteCompositionDeadlineTests(ITestOutputHelper output) : CompositionDeadlineTests(output)
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Trait("Suite", "recovery")]
public sealed class PostgresCompositionDeadlineTests(ITestOutputHelper output) : CompositionDeadlineTests(output)
{
    protected override string Provider => "postgres";
}
