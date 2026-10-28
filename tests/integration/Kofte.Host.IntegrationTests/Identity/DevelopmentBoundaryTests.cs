using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

public abstract class DevelopmentBoundaryTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ActualStartupRefusesEveryDevelopmentEnvironmentAndExposureCaseWithoutChangingPreparedState(
        bool composed, bool unsafeExposure)
    {
        await using var fixture = await IdentityFixture.CreateAsync(Provider, Token);
        await DevelopmentExposure.RunAsync(fixture, composed, unsafeExposure, Token);
    }
}

[Trait("Provider", "sqlite")]
[Collection("identity-deadlines")]
public sealed class SqliteDevelopmentBoundaryTests : DevelopmentBoundaryTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Collection("identity-deadlines")]
public sealed class PostgresDevelopmentBoundaryTests : DevelopmentBoundaryTests
{
    protected override string Provider => "postgres";
}
