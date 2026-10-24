using Kofte.TestSupport;

namespace Kofte.Platform.ContractTests.Composition;

[Trait("Feature", "composition")]
public abstract class CompositionPrivacyContractTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task IndependentPrivateDiagnosticsRequiresRealChannelsAndSafeUsefulBoundaryEvidence()
    {
        var execution = await CompositionConsumerHarness.InvokeAsync(
            ["--provider", Provider, "--scenario", "private-diagnostics"], TestContext.Current.CancellationToken);
        var result = CompositionConsumerHarness.RequireSuccess(execution, Provider, "private-diagnostics");
        foreach (var (name, minimum) in CompositionScenarioCatalog.RequiredObservations("private-diagnostics"))
            Assert.True(result.Observations["private-diagnostics"][name] >= minimum);
    }
}

[Trait("Provider", "sqlite"), Trait("Suite", "privacy")]
public sealed class SqliteCompositionPrivacyContractTests : CompositionPrivacyContractTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres"), Trait("Suite", "privacy")]
public sealed class PostgresCompositionPrivacyContractTests : CompositionPrivacyContractTests
{
    protected override string Provider => "postgres";
}
