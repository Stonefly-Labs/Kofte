using Kofte.TestSupport.Identity;

namespace Kofte.Platform.ContractTests.Identity;

public abstract class IdentityGuideTests
{
    protected abstract string Provider { get; }

    [Theory]
    [InlineData("oidc-login")]
    [InlineData("trust-refusal")]
    [InlineData("nominated-enrollment")]
    [InlineData("enrollment-race")]
    [InlineData("session-boundary")]
    [InlineData("development-isolation")]
    [InlineData("upgrade-preservation")]
    [InlineData("interrupted-recovery")]
    [InlineData("private-evidence")]
    [InlineData("all")]
    public async Task DeliveredRecipeRequiresEveryExactOrderedObservation(string scenario)
    {
        var result = await IdentityConsumerHarness.RunAsync(Provider, scenario, TestContext.Current.CancellationToken);
        var expected = scenario == "all" ? IdentityScenarioCatalog.Scenarios : [scenario];
        Assert.Equal(expected, result.Observations.Keys);
        foreach (var name in expected)
            Assert.Equal(IdentityScenarioCatalog.RequiredObservations(name), result.Observations[name]);
        Assert.Equal(result.Assertions, result.Observations.Values.Sum(values => values.Count));
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteIdentityGuideTests : IdentityGuideTests
{ protected override string Provider => "sqlite"; }

[Trait("Provider", "postgres")]
public sealed class PostgresIdentityGuideTests : IdentityGuideTests
{ protected override string Provider => "postgres"; }

public abstract class IdentityPrivacyGuideTests
{
    protected abstract string Provider { get; }

    [Theory]
    [InlineData("oidc-login")]
    [InlineData("nominated-enrollment")]
    [InlineData("session-boundary")]
    [InlineData("development-isolation")]
    [InlineData("private-evidence")]
    public async Task ActualIdentityJourneysRequireTheirPrivateCapturedEvidence(string scenario)
    {
        var result = await IdentityConsumerHarness.RunAsync(Provider, scenario, TestContext.Current.CancellationToken);
        Assert.Equal(IdentityScenarioCatalog.RequiredObservations(scenario), result.Observations[scenario]);
    }
}

[Trait("Provider", "sqlite"), Trait("Suite", "privacy"), Trait("Feature", "identity")]
public sealed class SqliteIdentityPrivacyGuideTests : IdentityPrivacyGuideTests
{ protected override string Provider => "sqlite"; }

[Trait("Provider", "postgres"), Trait("Suite", "privacy"), Trait("Feature", "identity")]
public sealed class PostgresIdentityPrivacyGuideTests : IdentityPrivacyGuideTests
{ protected override string Provider => "postgres"; }
