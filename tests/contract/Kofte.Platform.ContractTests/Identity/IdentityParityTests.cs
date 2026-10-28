using System.Net;
using System.Text;
using System.Text.Json;
using Kofte.Identity.Consumer;
using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Platform.ContractTests.Identity;

public abstract class IdentityParityTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ActualHttpsAdapterAndSeparateActorRetainTheSameIdentityThroughNominationAndSignOut()
    {
        await using var fixture = await CreateAsync();
        await LoginAsync(fixture);
        var user = Assert.Single((await fixture.SnapshotAsync(Token)).Users);
        await AssertIdentityAsync(fixture, user.Id, designated: false);
        var authorization = await fixture.NominateProductAsync(Token);
        var csrf = await fixture.ProductContextAsync(Token);
        using (var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/first-administrator")
        {
            Content = new StringContent(JsonSerializer.Serialize(new { authorization }), Encoding.UTF8, "application/json")
        })
        {
            request.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
            request.Headers.Add("X-Kofte-CSRF", csrf);
            using var response = await fixture.SendProductAsync(request, Token, adapted: true);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        await AssertIdentityAsync(fixture, user.Id, designated: true);
        using (var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/sign-out"))
        {
            request.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
            request.Headers.Add("X-Kofte-CSRF", await fixture.ProductContextAsync(Token));
            using var response = await fixture.SendProductAsync(request, Token, adapted: true);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsByteArrayAsync(Token));
            Assert.Contains(response.Headers.GetValues("Set-Cookie"),
                value => value.StartsWith("__Host-kofte-session=;", StringComparison.Ordinal));
        }
        foreach (var adapted in new[] { false, true })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
            using var response = await fixture.SendProductAsync(request, Token, adapted: adapted);
            await AssertProblemAsync(response, 401, "identity_unauthenticated");
        }
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(user, Assert.Single(after.Users));
        Assert.Equal(user.Id, Assert.Single(after.Mappings).UserId);
        Assert.Equal("consumed", after.BootstrapState);
        Assert.Equal("revoked", Assert.Single(after.Sessions).State);
        Assert.Single(after.Events, row => row.Code == "initial_administrator_enrolled");
        Assert.Single(after.Events, row => row.Code == "session_revoked");
    }

    [Fact]
    public async Task ActualNativeAndHttpsAdapterRefuseCredentialAndMutationConfusionWithoutProtectedEffects()
    {
        await using var fixture = await CreateAsync();
        await LoginAsync(fixture);
        var before = await fixture.SnapshotAsync(Token);
        var idToken = fixture.Protocol.IssuedIdToken ??
            throw new InvalidOperationException("identity_fixture_signed_token_required");
        foreach (var adapted in new[] { false, true })
        {
            foreach (var credential in new[] { "Bearer " + idToken, "Basic synthetic-credential" })
                foreach (var includeCookies in new[] { false, true })
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
                    request.Headers.Add("Authorization", credential);
                    using var response = await fixture.SendProductAsync(request, Token, includeCookies, adapted);
                    await AssertProblemAsync(response, 401, "identity_unauthenticated");
                }
            foreach (var origin in new[] { null, "null", "https://foreign.example.invalid" })
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/sign-out");
                request.Headers.Add("X-Kofte-CSRF", await fixture.ProductContextAsync(Token));
                if (origin is not null) request.Headers.Add("Origin", origin);
                using var response = await fixture.SendProductAsync(request, Token, adapted: adapted);
                await AssertProblemAsync(response, 403, "identity_forbidden");
            }
            using (var body = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me")
            {
                Content = new StringContent("{\"userId\":\"caller-selected\",\"isInitialAdministrator\":true}",
                    Encoding.UTF8, "application/json")
            })
            using (var response = await fixture.SendProductAsync(body, Token, adapted: adapted))
                await AssertProblemAsync(response, 400, "identity_request_invalid");
            using (var query = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me?userId=caller-selected"))
            using (var response = await fixture.SendProductAsync(query, Token, adapted: adapted))
                await AssertProblemAsync(response, 400, "identity_request_invalid");
            using (var method = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/me"))
            using (var response = await fixture.SendProductAsync(method, Token, adapted: adapted))
            {
                Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
                Assert.Equal("GET", Assert.Single(response.Content.Headers.Allow));
            }
        }
        await AssertIdentityAsync(fixture, Assert.Single(before.Users).Id, designated: false);
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(before.Events, after.Events);
        Assert.Equal(before.BootstrapState, after.BootstrapState);
        Assert.Equal(before.RegistryRevision, after.RegistryRevision);
        Assert.Equal("active", Assert.Single(after.Sessions).State);
        Assert.Equal(0, fixture.UngrantedActions);
    }

    [Fact]
    public async Task UnknownIdentityPathsAndVersionsRemainEmptyPrivate404sForEveryAdmittedMethod()
    {
        await using var fixture = await CreateAsync();
        await LoginAsync(fixture);
        var before = await fixture.SnapshotAsync(Token);
        foreach (var adapted in new[] { false, true })
            foreach (var path in new[] { IdentityCorpus.Http.Single(value => value.Name == "unknown").Path,
                "/api/identity/v2/me" })
                foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Head, HttpMethod.Options })
                {
                    using var request = new HttpRequestMessage(method, path);
                    using var response = await fixture.SendProductAsync(request, Token, adapted: adapted);
                    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
                    AssertPrivate(response);
                    Assert.Empty(await response.Content.ReadAsByteArrayAsync(Token));
                    Assert.Null(response.Headers.Location);
                    Assert.False(response.Headers.Contains("Set-Cookie"));
                }
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Revision, after.Revision);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(before.Events, after.Events);
    }

    [Fact]
    public async Task WarmedIdentityCannotBecomeAnAnonymousSuccessOrCachedIdentityWhenKeysAreLostThroughEitherPath()
    {
        await using var fixture = await CreateAsync();
        await LoginAsync(fixture);
        await AssertIdentityAsync(fixture, Assert.Single((await fixture.SnapshotAsync(Token)).Users).Id, designated: false);
        var before = await fixture.SnapshotAsync(Token);
        fixture.RemoveCurrentKey();
        foreach (var adapted in new[] { false, true })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
            using var response = await fixture.SendProductAsync(request, Token, adapted: adapted);
            await AssertProblemAsync(response, 503, "identity_unavailable");
        }
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Revision, after.Revision);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(before.Events, after.Events);
        Assert.Equal("active", Assert.Single(after.Sessions).State);
    }

    private Task<LoginCommitFixture> CreateAsync() =>
        LoginCommitFixture.CreateAsync(Provider, Token, productRoutes: true, withBrowser: true,
            actorProbe: ActorProbe.Observe);

    private async Task LoginAsync(LoginCommitFixture fixture)
    {
        var csrf = await fixture.ProductContextAsync(Token);
        using var request = fixture.ProductLoginRequest("/identity", csrf);
        using var start = await fixture.SendProductAsync(request, Token);
        using var completed = await fixture.CompleteProductAsync(start, Token);
        Assert.Equal(HttpStatusCode.SeeOther, completed.StatusCode);
    }

    private async Task AssertIdentityAsync(LoginCommitFixture fixture, Guid expected, bool designated)
    {
        foreach (var adapted in new[] { false, true })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
            request.Headers.Add("X-User-Id", Guid.NewGuid().ToString("D"));
            request.Headers.Add("X-Initial-Administrator", "true");
            request.Headers.Add("X-Forwarded-Host", "foreign.example.invalid");
            using var response = await fixture.SendProductAsync(request, Token, adapted: adapted);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            AssertPrivate(response);
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
            var body = document.RootElement;
            Assert.Equal(new[] { "displayName", "identityMode", "isInitialAdministrator", "userId", "version" },
                body.EnumerateObject().Select(value => value.Name).Order());
            Assert.Equal("1", body.GetProperty("version").GetString());
            Assert.Equal(expected, body.GetProperty("userId").GetGuid());
            Assert.Equal("oidc", body.GetProperty("identityMode").GetString());
            Assert.Equal(designated, body.GetProperty("isInitialAdministrator").GetBoolean());
        }
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/fixture-actor"))
        using (var response = await fixture.SendProductAsync(request, Token))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
            var actor = document.RootElement;
            Assert.Equal(expected, actor.GetProperty("userId").GetGuid());
            Assert.Equal("oidc", actor.GetProperty("identityMode").GetString());
            Assert.Equal(designated, actor.GetProperty("isInitialAdministrator").GetBoolean());
        }
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/fixture-ungranted"))
        using (var response = await fixture.SendProductAsync(request, Token))
        {
            await AssertProblemAsync(response, 403, "identity_forbidden");
            Assert.Equal(0, fixture.UngrantedActions);
        }
    }

    private async Task AssertProblemAsync(HttpResponseMessage response, int status, string code)
    {
        Assert.Equal(status, (int)response.StatusCode);
        AssertPrivate(response);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
        var body = document.RootElement;
        Assert.Equal(new[] { "code", "correlationId", "status", "title", "type" },
            body.EnumerateObject().Select(value => value.Name).Order());
        Assert.Equal(status, body.GetProperty("status").GetInt32());
        Assert.Equal(code, body.GetProperty("code").GetString());
        Assert.Equal("urn:kofte:problem:" + code, body.GetProperty("type").GetString());
        Assert.Matches("^[a-f0-9]{32}$", body.GetProperty("correlationId").GetString()!);
        Assert.Null(response.Headers.Location);
    }

    private static void AssertPrivate(HttpResponseMessage response)
    {
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal("no-cache", Assert.Single(response.Headers.Pragma).Name);
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
    }
}

[Trait("Provider", "sqlite")]
[Collection("identity-deadlines")]
public sealed class SqliteIdentityParityTests : IdentityParityTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Collection("identity-deadlines")]
public sealed class PostgresIdentityParityTests : IdentityParityTests
{
    protected override string Provider => "postgres";
}
