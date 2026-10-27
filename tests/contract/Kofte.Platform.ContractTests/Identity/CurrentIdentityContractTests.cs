using System.Net;
using System.Text.Json;
using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Platform.ContractTests.Identity;

public abstract class CurrentIdentityContractTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task EveryRequestProjectsTheFreshDurableProfileAndDesignationIntoApiAndReadOnlyActor()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var csrf = await fixture.ProductContextAsync(Token);
        using var login = fixture.ProductLoginRequest("/identity", csrf);
        using var start = await fixture.SendProductAsync(login, Token);
        using var completed = await fixture.CompleteProductAsync(start, Token);
        Assert.Equal(303, (int)completed.StatusCode);
        var user = Assert.Single((await fixture.SnapshotAsync(Token)).Users);
        await fixture.SeedCurrentProfileAndDesignationAsync(Token);
        foreach (var path in new[] { "/api/identity/v1/me", "/fixture-actor" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            using var response = await fixture.SendProductAsync(request, Token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var text = await response.Content.ReadAsStringAsync(Token);
            using var body = JsonDocument.Parse(text);
            Assert.Equal(user.Id.ToString("D"), body.RootElement.GetProperty("userId").GetString());
            Assert.Equal("oidc", body.RootElement.GetProperty("identityMode").GetString());
            Assert.True(body.RootElement.GetProperty("isInitialAdministrator").GetBoolean());
            if (path == "/api/identity/v1/me")
            {
                Assert.Equal("<changed-profile>", body.RootElement.GetProperty("displayName").GetString());
                Assert.DoesNotContain("<changed-profile>", text, StringComparison.Ordinal);
            }
        }
        using (var ungrantedRequest = new HttpRequestMessage(HttpMethod.Get, "/fixture-ungranted"))
        using (var ungranted = await fixture.SendProductAsync(ungrantedRequest, Token))
            Assert.Equal(HttpStatusCode.Forbidden, ungranted.StatusCode);
        await fixture.RevokeCurrentAsync(Token);
        using var revokedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
        using var revoked = await fixture.SendProductAsync(revokedRequest, Token);
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        using var absentActorRequest = new HttpRequestMessage(HttpMethod.Get, "/fixture-actor");
        using var absentActor = await fixture.SendProductAsync(absentActorRequest, Token);
        Assert.Equal(HttpStatusCode.Unauthorized, absentActor.StatusCode);
    }

    [Fact]
    public async Task DeliveredCurrentIdentityHasOnlyTheRequiredSelfFieldsAndNeverChallengesAnonymousClients()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        using (var anonymous = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me"))
        using (var response = await fixture.SendProductAsync(anonymous, Token))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(response.Headers.Location);
        }
        var csrf = await fixture.ProductContextAsync(Token);
        using var request = fixture.ProductLoginRequest("/identity", csrf);
        using var start = await fixture.SendProductAsync(request, Token);
        using var completed = await fixture.CompleteProductAsync(start, Token);
        Assert.Equal(303, (int)completed.StatusCode);
        var user = Assert.Single((await fixture.SnapshotAsync(Token)).Users);
        using var currentRequest = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
        using var current = await fixture.SendProductAsync(currentRequest, Token);
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        Assert.Equal("no-store", current.Headers.CacheControl!.ToString());
        using var body = JsonDocument.Parse(await current.Content.ReadAsByteArrayAsync(Token));
        Assert.Equal(["displayName", "identityMode", "isInitialAdministrator", "userId", "version"],
            body.RootElement.EnumerateObject().Select(item => item.Name).Order().ToArray());
        Assert.Equal("1", body.RootElement.GetProperty("version").GetString());
        Assert.Equal(user.Id.ToString("D"), body.RootElement.GetProperty("userId").GetString());
        Assert.Equal(user.DisplayName, body.RootElement.GetProperty("displayName").GetString());
        Assert.Equal("oidc", body.RootElement.GetProperty("identityMode").GetString());
        Assert.False(body.RootElement.GetProperty("isInitialAdministrator").GetBoolean());
        await fixture.CorruptCurrentTicketAsync(Token);
        using var invalidRequest = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
        using var invalid = await fixture.SendProductAsync(invalidRequest, Token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, invalid.StatusCode);
        Assert.Null(invalid.Headers.Location);
        using var failure = JsonDocument.Parse(await invalid.Content.ReadAsByteArrayAsync(Token));
        Assert.Equal("identity_unavailable", failure.RootElement.GetProperty("code").GetString());
        Assert.False(failure.RootElement.TryGetProperty("userId", out _));
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteCurrentIdentityContractTests : CurrentIdentityContractTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresCurrentIdentityContractTests : CurrentIdentityContractTests
{
    protected override string Provider => "postgres";
}
