using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Composition;

[Trait("Feature", "composition")]
public abstract class CompositionPrivacyTests(ITestOutputHelper output)
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task ActualSixChannelPublicCaptureExcludesPrivateInputsAndRequiresUsefulFailureEvidence()
    {
        var execution = await CompositionConsumerHarness.InvokeAsync(
            ["--provider", Provider, "--scenario", "private-diagnostics"], TestContext.Current.CancellationToken);
        output.WriteLine(execution.Stdout);
        output.WriteLine(execution.Stderr);
        var result = CompositionConsumerHarness.RequireSuccess(execution, Provider, "private-diagnostics");
        foreach (var (name, minimum) in CompositionScenarioCatalog.RequiredObservations("private-diagnostics"))
            Assert.True(result.Observations["private-diagnostics"][name] >= minimum);
    }
}

[Trait("Provider", "sqlite")]
[Trait("Suite", "privacy")]
public sealed class SqliteCompositionPrivacyTests(ITestOutputHelper output) : CompositionPrivacyTests(output)
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Trait("Suite", "privacy")]
public sealed class PostgresCompositionPrivacyTests(ITestOutputHelper output) : CompositionPrivacyTests(output)
{
    protected override string Provider => "postgres";
}
