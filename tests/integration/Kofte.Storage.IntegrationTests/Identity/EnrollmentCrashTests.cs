using System.Net;
using System.Net.Http.Json;
using Kofte.TestSupport.Identity;

namespace Kofte.Storage.IntegrationTests.Identity;

public abstract class EnrollmentCrashTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("enrollment_before_commit", false)]
    [InlineData("enrollment_committed", true)]
    public async Task ExactOwnedChildLossPreservesNeitherOrOneAtomicDesignationWithoutAcknowledgedSuccess(
        string phase, bool committed)
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var authorization = await fixture.NominateProductAsync(Token);
        var anonymous = await fixture.ProductContextAsync(Token);
        using var login = fixture.ProductLoginRequest("/identity", anonymous);
        using var start = await fixture.SendProductAsync(login, Token);
        using var callback = await fixture.CompleteProductAsync(start, Token);
        Assert.Equal(303, (int)callback.StatusCode);
        var before = await fixture.SnapshotAsync(Token);
        var pending = await fixture.BootstrapAsync(Token);
        var crash = await fixture.CrashEnrollmentAsync(phase, authorization, Token);
        Assert.True(crash.AcknowledgementLost && crash.ExitVerified);
        var reopened = await fixture.SnapshotAsync(Token);
        var bootstrap = await fixture.BootstrapAsync(Token);
        Assert.Equal(before.Users, reopened.Users);
        Assert.Equal(before.Mappings, reopened.Mappings);
        Assert.Equal(before.RegistryRevision, reopened.RegistryRevision);
        Assert.Equal(before.Epoch, reopened.Epoch);
        if (committed)
        {
            Assert.Equal("consumed", bootstrap.State);
            Assert.Equal(Assert.Single(before.Users).Id, bootstrap.InitialUser);
            Assert.Equal(pending.Generation, bootstrap.Generation);
            Assert.Equal(pending.Generation, bootstrap.ConsumedGeneration);
            Assert.NotNull(bootstrap.ConsumedAt);
            Assert.Null(bootstrap.Digest);
            Assert.Null(bootstrap.Issuer);
            Assert.Null(bootstrap.Subject);
            Assert.Null(bootstrap.Expires);
            var record = Assert.Single(reopened.Events, value => !before.Events.Contains(value));
            Assert.Equal("initial_administrator_enrolled", record.Code);
            Assert.Equal(bootstrap.InitialUser, record.UserId);
            Assert.Equal(reopened.Revision, record.Revision);
        }
        else
        {
            Assert.Equal(pending, bootstrap);
            Assert.Equal(before.Events, reopened.Events);
        }
        await fixture.RestartProductAsync(Token);
        using var retry = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/first-administrator")
        {
            Content = JsonContent.Create(new { authorization })
        };
        retry.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
        retry.Headers.Add("X-Kofte-CSRF", await fixture.ProductContextAsync(Token));
        using var response = await fixture.SendProductAsync(retry, Token);
        Assert.Equal(committed ? HttpStatusCode.Conflict : HttpStatusCode.OK, response.StatusCode);
        var final = await fixture.SnapshotAsync(Token);
        Assert.Equal("consumed", final.BootstrapState);
        Assert.Single(final.Events, value => value.Code == "initial_administrator_enrolled");
        if (committed) Assert.Equal(reopened.Events, final.Events);
    }
}

[Trait("Provider", "sqlite")]
[Collection("identity-deadlines")]
public sealed class SqliteEnrollmentCrashTests : EnrollmentCrashTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Collection("identity-deadlines")]
public sealed class PostgresEnrollmentCrashTests : EnrollmentCrashTests
{
    protected override string Provider => "postgres";
}
