using Kofte.TestSupport;

namespace Kofte.Platform.ContractTests.Composition;

[Trait("Feature", "composition")]
public abstract class CompositionMigrationContractTests(ITestOutputHelper output)
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task ActualPublicCommandsPrepareUpgradeAndServeWithoutRewritingHistoryOrOriginalData()
    {
        var provider = Provider;
        var execution = await CompositionConsumerHarness.InvokeAsync(
            ["--provider", provider, "--scenario", "ordered-migration"], TestContext.Current.CancellationToken);
        output.WriteLine(execution.Stdout);
        output.WriteLine(execution.Stderr);
        var result = CompositionConsumerHarness.RequireSuccess(execution, provider, "ordered-migration");
        foreach (var (name, minimum) in CompositionScenarioCatalog.RequiredObservations("ordered-migration"))
            Assert.True(result.Observations["ordered-migration"][name] >= minimum);
    }

    [Fact]
    public async Task PublicSelectionChangesPreserveOriginalsAcrossThreeCompleteFreshProcessCycles()
    {
        var provider = Provider;
        var execution = await CompositionConsumerHarness.InvokeAsync(
            ["--provider", provider, "--scenario", "disable-reenable"], TestContext.Current.CancellationToken);
        output.WriteLine(execution.Stdout);
        output.WriteLine(execution.Stderr);
        var result = CompositionConsumerHarness.RequireSuccess(execution, provider, "disable-reenable");
        foreach (var (name, minimum) in CompositionScenarioCatalog.RequiredObservations("disable-reenable"))
            Assert.Equal(minimum, result.Observations["disable-reenable"][name]);
    }

    [Trait("Provider", "sqlite"), Trait("Suite", "recovery")]
    public sealed class SqliteCompositionMigrationContractTests(ITestOutputHelper output) : CompositionMigrationContractTests(output)
    {
        protected override string Provider => "sqlite";
    }

    [Trait("Provider", "postgres"), Trait("Suite", "recovery")]
    public sealed class PostgresCompositionMigrationContractTests(ITestOutputHelper output) : CompositionMigrationContractTests(output)
    {
        protected override string Provider => "postgres";
    }
}
