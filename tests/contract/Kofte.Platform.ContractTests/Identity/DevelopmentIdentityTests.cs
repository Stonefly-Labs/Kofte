using System.Net;
using System.Text;
using System.Text.Json;
using Kofte.Identity.Consumer;
using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Platform.ContractTests.Identity;

public abstract class DevelopmentIdentityTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ConfiguredSelectionCommitsASeparateStableUserAndNativeSessionWithoutAdministratorAuthority()
    {
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token,
            productRoutes: true, development: true, actorProbe: ActorProbe.Observe);
        var anonymous = await fixture.ProductContextAsync(Token);
        var user = await SelectAsync(fixture, anonymous);
        var cookie = fixture.ProductCookieHeader;
        Assert.Contains("kofte-dev-session=", cookie);
        var first = await fixture.SnapshotAsync(Token);
        Assert.Equal(user, Assert.Single(first.Users).Id);
        Assert.Equal("development", Assert.Single(first.Users).Mode);
        var mapping = Assert.Single(first.Mappings);
        Assert.Equal(("development", "kofte:development:v1", "synthetic", user),
            (mapping.Mode, mapping.Issuer, mapping.Subject, mapping.UserId));
        Assert.Equal("disabled", first.BootstrapState);
        Assert.Single(first.Events, value => value.Code == "user_created");
        Assert.Single(first.Events, value => value.Code == "session_created");
        await AssertCurrentAsync(fixture, user, "development", designated: false);
        var authenticated = await fixture.ProductContextAsync(Token);
        Assert.NotEqual(anonymous, authenticated);
        using (var stale = Selection(fixture, anonymous))
        using (var response = await fixture.SendProductAsync(stale, Token))
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(user, await SelectAsync(fixture, authenticated));
        var after = await fixture.SnapshotAsync(Token);
        Assert.Single(after.Users);
        Assert.Single(after.Mappings);
        Assert.Equal(2, after.Sessions.Count);
        Assert.Single(after.Events, value => value.Code == "user_created");
        Assert.Equal(2, after.Events.Count(value => value.Code == "session_created"));
        Assert.NotEqual(cookie, fixture.ProductCookieHeader);
        Assert.Equal(first.RegistryRevision, after.RegistryRevision);
        var oldSession = Assert.Single(first.Sessions).Digest;
        var replacement = Assert.Single(after.Sessions, session => session.Digest != oldSession).Digest;
        using (var signOut = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/sign-out"))
        {
            signOut.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
            signOut.Headers.Add("X-Kofte-CSRF", await fixture.ProductContextAsync(Token));
            using var response = await fixture.SendProductAsync(signOut, Token);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
        var terminal = await fixture.SnapshotAsync(Token);
        Assert.Equal("revoked", terminal.Sessions.Single(session => session.Digest == replacement).State);
        Assert.Equal("active", terminal.Sessions.Single(session => session.Digest == oldSession).State);
        Assert.Equal(0, fixture.ProductProviderRequests);
        Assert.Equal(0, fixture.UngrantedActions);
    }

    [Fact]
    public async Task CallerSelectedIdentityProfileAuthorityAndMalformedJsonCannotCreateDevelopmentSessions()
    {
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token,
            productRoutes: true, development: true);
        var csrf = await fixture.ProductContextAsync(Token);
        var before = await fixture.SnapshotAsync(Token);
        foreach (var body in new[]
        {
            "{}", "[]", "null", "{\"identityKey\":null}", "{\"identityKey\":\"unknown\"}",
            "{\"identityKey\":\"Synthetic\"}", "{\"identityKey\":\"synthetic\",\"identityKey\":\"synthetic\"}",
            "{\"identityKey\":\"synthetic\",\"userId\":\"caller-selected\"}",
            "{\"identityKey\":\"synthetic\",\"isInitialAdministrator\":true}",
            "{\"identityKey\":\"synthetic\",\"email\":\"admin@example.invalid\"}",
            "{\"identityKey\":\"synthetic\",\"displayName\":\"Administrator\"}",
            "{\"identityKey\":\"synthetic\",\"subject\":\"foreign\"}"
        })
        {
            using var request = Selection(fixture, csrf, body);
            using var response = await fixture.SendProductAsync(request, Token);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            Assert.False(response.Headers.Contains("Set-Cookie"));
        }
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(before.Sessions, after.Sessions);
        Assert.Equal(before.Events, after.Events);
        Assert.Equal(before.RegistryRevision, after.RegistryRevision);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualStoppedModeTransitionsPreserveEqualProfileNamespacesAndConsumedProductionAuthority(
        bool consumed)
    {
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token,
            productRoutes: true, actorProbe: ActorProbe.Observe);
        fixture.Protocol.Subject = "synthetic";
        fixture.Protocol.DisplayName = null;
        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Head, HttpMethod.Options })
        {
            using var request = new HttpRequestMessage(method, "/api/identity/v1/development-session");
            using var response = await fixture.SendProductAsync(request, Token);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.False(response.Headers.Contains("Set-Cookie"));
        }
        var authorization = await fixture.NominateProductAsync(Token);
        await LoginAsync(fixture);
        if (consumed)
        {
            using var enrollment = Enrollment(fixture, await fixture.ProductContextAsync(Token), authorization);
            using var response = await fixture.SendProductAsync(enrollment, Token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        var original = await fixture.SnapshotAsync(Token);
        var production = Assert.Single(original.Users);
        var ceremony = await fixture.BootstrapAsync(Token);
        var productionCookie = fixture.ProductCookieHeader;
        await fixture.ReconcileProductModeAsync(development: true, Token);
        await AssertRefusedCookiesAsync(fixture, productionCookie, "__Host-kofte-session=", "kofte-dev-session=");
        var development = await SelectAsync(fixture, await fixture.ProductContextAsync(Token));
        Assert.NotEqual(production.Id, development);
        await AssertCurrentAsync(fixture, development, "development", designated: false);
        var beforeRejected = await fixture.SnapshotAsync(Token);
        using (var enrollment = Enrollment(fixture, await fixture.ProductContextAsync(Token), authorization))
        using (var response = await fixture.SendProductAsync(enrollment, Token))
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(beforeRejected.Events, (await fixture.SnapshotAsync(Token)).Events);
        var developmentCookie = fixture.ProductCookieHeader;
        await fixture.ReconcileProductModeAsync(development: false, Token);
        await AssertRefusedCookiesAsync(fixture, developmentCookie, "kofte-dev-session=", "__Host-kofte-session=");
        await LoginAsync(fixture);
        await AssertCurrentAsync(fixture, production.Id, "oidc", consumed);
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(2, after.Users.Count);
        Assert.Equal(2, after.Mappings.Count);
        Assert.Equal(production.CreatedAt, after.Users.Single(value => value.Id == production.Id).CreatedAt);
        Assert.Null(after.Users.Single(value => value.Id == development).DisplayName);
        Assert.All(after.Mappings, mapping => Assert.Equal("synthetic", mapping.Subject));
        Assert.NotEqual(original.Epoch, after.Epoch);
        Assert.Equal(original.RegistryRevision, after.RegistryRevision);
        Assert.All(original.Events, value => Assert.Contains(value, after.Events));
        var finalCeremony = await fixture.BootstrapAsync(Token);
        Assert.Equal(consumed ? "consumed" : "disabled", finalCeremony.State);
        Assert.Equal(ceremony.InitialUser, finalCeremony.InitialUser);
        Assert.Equal(ceremony.ConsumedAt, finalCeremony.ConsumedAt);
        Assert.Equal(ceremony.ConsumedGeneration, finalCeremony.ConsumedGeneration);
        using (var replay = Enrollment(fixture, await fixture.ProductContextAsync(Token), authorization))
        using (var response = await fixture.SendProductAsync(replay, Token))
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(0, fixture.UngrantedActions);
    }

    private async Task AssertRefusedCookiesAsync(LoginCommitFixture fixture, string cookie, string original, string renamed)
    {
        foreach (var value in new[] { cookie, cookie.Replace(original, renamed, StringComparison.Ordinal) })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
            request.Headers.Add("Cookie", value);
            using var response = await fixture.SendProductAsync(request, Token, includeCookies: false);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(response.Headers.Location);
        }
    }

    private async Task AssertCurrentAsync(LoginCommitFixture fixture, Guid user, string mode, bool designated)
    {
        foreach (var path in new[] { "/api/identity/v1/me", "/fixture-actor" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            using var response = await fixture.SendProductAsync(request, Token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
            Assert.Equal(user, body.RootElement.GetProperty("userId").GetGuid());
            Assert.Equal(mode, body.RootElement.GetProperty("identityMode").GetString());
            Assert.Equal(designated, body.RootElement.GetProperty("isInitialAdministrator").GetBoolean());
        }
        using var ungranted = new HttpRequestMessage(HttpMethod.Get, "/fixture-ungranted");
        using var denied = await fixture.SendProductAsync(ungranted, Token);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    private async Task<Guid> SelectAsync(LoginCommitFixture fixture, string csrf)
    {
        using var request = Selection(fixture, csrf);
        using var response = await fixture.SendProductAsync(request, Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("kofte-dev-session=", cookie);
        var attributes = cookie.Split(';').Skip(1).Select(value => value.Trim().ToLowerInvariant()).ToArray();
        Assert.Contains("httponly", attributes);
        Assert.Contains("samesite=lax", attributes);
        Assert.Contains("path=/", attributes);
        Assert.DoesNotContain("secure", attributes);
        Assert.DoesNotContain(attributes, value => value.StartsWith("domain=", StringComparison.Ordinal));
        using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
        Assert.Equal("1", body.RootElement.GetProperty("version").GetString());
        Assert.Equal("development", body.RootElement.GetProperty("identityMode").GetString());
        Assert.False(body.RootElement.GetProperty("isInitialAdministrator").GetBoolean());
        Assert.Null(body.RootElement.GetProperty("displayName").GetString());
        return body.RootElement.GetProperty("userId").GetGuid();
    }

    private static HttpRequestMessage Selection(LoginCommitFixture fixture, string csrf,
        string body = "{\"identityKey\":\"synthetic\"}")
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/development-session")
        { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        request.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
        request.Headers.Add("X-Kofte-CSRF", csrf);
        return request;
    }

    private static HttpRequestMessage Enrollment(LoginCommitFixture fixture, string csrf, string authorization)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/first-administrator")
        { Content = new StringContent(JsonSerializer.Serialize(new { authorization }), Encoding.UTF8, "application/json") };
        request.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
        request.Headers.Add("X-Kofte-CSRF", csrf);
        return request;
    }

    private async Task LoginAsync(LoginCommitFixture fixture)
    {
        using var login = fixture.ProductLoginRequest("/identity", await fixture.ProductContextAsync(Token));
        using var start = await fixture.SendProductAsync(login, Token);
        using var completed = await fixture.CompleteProductAsync(start, Token);
        Assert.Equal(HttpStatusCode.SeeOther, completed.StatusCode);
    }
}

[Trait("Provider", "sqlite")]
[Collection("identity-deadlines")]
public sealed class SqliteDevelopmentIdentityTests : DevelopmentIdentityTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Collection("identity-deadlines")]
public sealed class PostgresDevelopmentIdentityTests : DevelopmentIdentityTests
{
    protected override string Provider => "postgres";
}
