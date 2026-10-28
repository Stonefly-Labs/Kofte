using System.Net;
using System.Text;
using System.Text.Json;
using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Platform.ContractTests.Identity;

public abstract class SignOutContractTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task LosingWarmedKeysIsUnavailableRatherThanAnAnonymousOrCachedIdentity()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        await LoginAsync(fixture);
        var before = await fixture.SnapshotAsync(Token);
        fixture.RemoveCurrentKey();
        foreach (var path in new[] { "/api/identity/v1/me", "/fixture-actor" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            using var response = await fixture.SendProductAsync(request, Token);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            using var problem = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
            Assert.Equal("identity_unavailable", problem.RootElement.GetProperty("code").GetString());
            Assert.False(problem.RootElement.TryGetProperty("userId", out _));
        }
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Revision, after.Revision);
        Assert.Equal(before.Events, after.Events);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal("active", Assert.Single(after.Sessions).State);
    }

    [Fact]
    public async Task ActualDevelopmentCookieCannotAuthenticateProductionEvenAfterRenamingItsCookie()
    {
        await using var production = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        await using var development = await LoginCommitFixture.CreateAsync(Provider, Token,
            productRoutes: true, development: true);
        var developmentUser = await development.CreateStoredDevelopmentSessionAsync(Token);
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me"))
        using (var response = await development.SendProductAsync(request, Token))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
            Assert.Equal(developmentUser, body.RootElement.GetProperty("userId").GetGuid());
            Assert.Equal("development", body.RootElement.GetProperty("identityMode").GetString());
        }
        await LoginAsync(production);
        var before = await production.SnapshotAsync(Token);
        var originalCookie = production.ProductCookieHeader;
        var developmentCookie = development.ProductCookieHeader;
        Assert.StartsWith("kofte-dev-session=", developmentCookie);
        foreach (var cookie in new[] { developmentCookie,
            developmentCookie.Replace("kofte-dev-session=", "__Host-kofte-session=", StringComparison.Ordinal) })
            foreach (var path in new[] { "/api/identity/v1/me", "/fixture-actor" })
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                request.Headers.Add("Cookie", cookie);
                request.Headers.Add("X-Identity-Mode", "development");
                request.Headers.Add("X-Initial-Administrator", "true");
                using var response = await production.SendProductAsync(request, Token, includeCookies: false);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.Null(response.Headers.Location);
            }
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me"))
        {
            request.Headers.Add("Cookie", originalCookie);
            using var response = await production.SendProductAsync(request, Token, includeCookies: false);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
            Assert.Equal(Assert.Single(before.Users).Id, body.RootElement.GetProperty("userId").GetGuid());
            Assert.False(body.RootElement.GetProperty("isInitialAdministrator").GetBoolean());
        }
        var after = await production.SnapshotAsync(Token);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(before.Events, after.Events);
        Assert.Equal(before.Epoch, after.Epoch);
        Assert.Equal("active", Assert.Single(after.Sessions).State);
    }

    [Fact]
    public async Task NormalRetainedKeyRotationAndFreshWorkersPreserveTheOriginalCookieAndDurableIdentity()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        await LoginAsync(fixture);
        var before = await fixture.SnapshotAsync(Token);
        var user = Assert.Single(before.Users);
        Assert.Equal(1, fixture.RetainedKeyCount);
        var firstWorker = await fixture.RotateRetainedKeysAsync(Token);
        for (var restart = 0; restart < 2; restart++)
        {
            foreach (var path in new[] { "/api/identity/v1/me", "/fixture-actor" })
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, path);
                using var response = await fixture.SendProductAsync(request, Token);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
                Assert.Equal(user.Id, document.RootElement.GetProperty("userId").GetGuid());
                Assert.Equal("oidc", document.RootElement.GetProperty("identityMode").GetString());
                Assert.False(document.RootElement.GetProperty("isInitialAdministrator").GetBoolean());
            }
            Assert.Equal(2, fixture.RetainedKeyCount);
            if (restart == 0) Assert.NotEqual(firstWorker, await fixture.RestartProductAsync(Token));
        }
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Epoch, after.Epoch);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(before.Events, after.Events);
        Assert.Equal(before.RegistryRevision, after.RegistryRevision);
        Assert.Equal(Assert.Single(before.Sessions).Digest, Assert.Single(after.Sessions).Digest);
        Assert.Equal("active", Assert.Single(after.Sessions).State);
    }

    [Fact]
    public async Task ActualStoppedEpochRevocationDeniesTheNextIdentityAndActorRequestsWithoutLosingUsers()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        await LoginAsync(fixture);
        var before = await fixture.SnapshotAsync(Token);
        await fixture.RevokeProductSessionsAsync(Token);
        foreach (var path in new[] { "/api/identity/v1/me", "/fixture-actor" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            using var response = await fixture.SendProductAsync(request, Token);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        var after = await fixture.SnapshotAsync(Token);
        Assert.NotEqual(before.Epoch, after.Epoch);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(before.BootstrapState, after.BootstrapState);
        Assert.Equal(before.RegistryRevision, after.RegistryRevision);
        Assert.Equal("revoked", Assert.Single(after.Sessions).State);
        Assert.NotNull(Assert.Single(after.Sessions).TerminalAt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IdleAndAbsoluteExpiryRejectSignOutWithoutReportingRevocation(bool absolute)
    {
        var clock = new SessionClock();
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token, clock, productRoutes: true);
        await LoginAsync(fixture);
        var csrf = await fixture.ProductContextAsync(Token);
        var original = Assert.Single((await fixture.SnapshotAsync(Token)).Sessions);
        if (absolute)
            for (var index = 1; index <= 16; index++)
            {
                clock.Now = new(original.CreatedAt.AddMinutes(29 * index));
                using var activity = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
                using var renewed = await fixture.SendProductAsync(activity, Token);
                Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
            }
        clock.Now = new(absolute ? original.AbsoluteExpiresAt : original.IdleExpiresAt);
        using var request = Request(fixture, csrf);
        using var response = await fixture.SendProductAsync(request, Token);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var state = await fixture.SnapshotAsync(Token);
        var expired = Assert.Single(state.Sessions);
        Assert.Equal(original.Digest, expired.Digest);
        Assert.Equal("expired", expired.State);
        Assert.Equal(clock.Now.UtcDateTime, expired.TerminalAt);
        Assert.Equal(original.AbsoluteExpiresAt, expired.AbsoluteExpiresAt);
        Assert.Single(state.Events, row => row.Code == "session_expired");
        Assert.DoesNotContain(state.Events, row => row.Code == "session_revoked");
    }

    [Fact]
    public async Task ConcurrentSignOutsConfirmOneRevocationAndRefuseTheOtherWithoutLosingItsSafeResponse()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        await LoginAsync(fixture);
        var csrf = await fixture.ProductContextAsync(Token);
        using var first = Request(fixture, csrf);
        using var second = Request(fixture, csrf);
        var replies = await Task.WhenAll(fixture.SendProductAsync(first, Token),
            fixture.SendProductAsync(second, Token));
        using var one = replies[0];
        using var two = replies[1];
        Assert.Equal(new[] { 204, 401 }, replies.Select(value => (int)value.StatusCode).Order());
        var refused = replies.Single(value => value.StatusCode == HttpStatusCode.Unauthorized);
        using var problem = JsonDocument.Parse(await refused.Content.ReadAsByteArrayAsync(Token));
        Assert.Equal("identity_unauthenticated", problem.RootElement.GetProperty("code").GetString());
        var state = await fixture.SnapshotAsync(Token);
        Assert.Equal("revoked", Assert.Single(state.Sessions).State);
        Assert.Single(state.Events, row => row.Code == "session_revoked");
    }

    [Fact]
    public async Task CurrentActivityRacingConfirmedSignOutCannotRenewTheTerminalSession()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        await LoginAsync(fixture);
        var csrf = await fixture.ProductContextAsync(Token);
        var cookie = fixture.ProductCookieHeader;
        using var activity = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
        using var signOut = Request(fixture, csrf);
        var responses = await Task.WhenAll(fixture.SendProductAsync(activity, Token),
            fixture.SendProductAsync(signOut, Token));
        using var active = responses[0];
        using var confirmed = responses[1];
        Assert.Contains(active.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized });
        Assert.Equal(HttpStatusCode.NoContent, confirmed.StatusCode);
        using var replay = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
        replay.Headers.Add("Cookie", cookie);
        using var denied = await fixture.SendProductAsync(replay, Token, includeCookies: false);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        var state = await fixture.SnapshotAsync(Token);
        Assert.Equal("revoked", Assert.Single(state.Sessions).State);
        Assert.Single(state.Events, row => row.Code == "session_revoked");
    }

    [Fact]
    public async Task ConfirmedSignOutRevokesOnlyTheCurrentSessionAndReplayedCookieCannotReachIdentityOrActor()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        await LoginAsync(fixture);
        var first = Assert.Single((await fixture.SnapshotAsync(Token)).Sessions);
        await LoginAsync(fixture);
        var csrf = await fixture.ProductContextAsync(Token);
        var cookie = fixture.ProductCookieHeader;
        var before = await fixture.SnapshotAsync(Token);
        var current = Assert.Single(before.Sessions, row => row.Digest != first.Digest);
        var providerRequests = fixture.ProductProviderRequests;
        using var request = Request(fixture, csrf);
        using var response = await fixture.SendProductAsync(request, Token);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync(Token));
        Assert.Null(response.Headers.Location);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Equal("no-cache", Assert.Single(response.Headers.Pragma).Name);
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        Assert.Contains(response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith("__Host-kofte-session=;", StringComparison.Ordinal) &&
                value.Contains("httponly", StringComparison.OrdinalIgnoreCase) &&
                value.Contains("secure", StringComparison.OrdinalIgnoreCase));
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(2, after.Sessions.Count);
        Assert.Equal("active", Assert.Single(after.Sessions, row => row.Digest == first.Digest).State);
        var revoked = Assert.Single(after.Sessions, row => row.Digest == current.Digest);
        Assert.Equal("revoked", revoked.State);
        Assert.NotNull(revoked.TerminalAt);
        var recorded = Assert.Single(after.Events, row => row.Code == "session_revoked");
        Assert.Equal(current.UserId, recorded.UserId);
        Assert.Equal(after.Revision, recorded.Revision);
        Assert.Equal(before.RegistryRevision, after.RegistryRevision);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        foreach (var path in new[] { "/api/identity/v1/me", "/fixture-actor" })
        {
            using var replay = new HttpRequestMessage(HttpMethod.Get, path);
            replay.Headers.Add("Cookie", cookie);
            using var denied = await fixture.SendProductAsync(replay, Token, includeCookies: false);
            Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        }
        Assert.Equal(providerRequests, fixture.ProductProviderRequests);
        Assert.Single((await fixture.SnapshotAsync(Token)).Events, row => row.Code == "session_revoked");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InterruptedRevocationNeverAcknowledgesSuccessAndPreservesTheActualCommitBoundary(bool committed)
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        await LoginAsync(fixture);
        var csrf = await fixture.ProductContextAsync(Token);
        var cookie = fixture.ProductCookieHeader;
        fixture.FailSignOutCommit(committed);
        using var request = Request(fixture, csrf);
        using var response = await fixture.SendProductAsync(request, Token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
        Assert.Equal(503, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Contains(problem.RootElement.GetProperty("code").GetString(),
            new[] { "identity_unavailable", "identity_outcome_uncertain" });
        Assert.Contains(response.Headers.GetValues("Set-Cookie"),
            value => value.StartsWith("__Host-kofte-session=;", StringComparison.Ordinal));
        var state = await fixture.SnapshotAsync(Token);
        Assert.Equal(committed ? "revoked" : "active", Assert.Single(state.Sessions).State);
        Assert.Equal(committed ? 1 : 0, state.Events.Count(row => row.Code == "session_revoked"));
        using var replay = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
        replay.Headers.Add("Cookie", cookie);
        using var actual = await fixture.SendProductAsync(replay, Token, includeCookies: false);
        Assert.Equal(committed ? HttpStatusCode.Unauthorized : HttpStatusCode.OK, actual.StatusCode);
    }

    [Fact]
    public async Task RejectedRequiredRevocationRecordRollsBackItsSessionAndExplicitCorrectionPreservesTheCredential()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        await LoginAsync(fixture);
        var csrf = await fixture.ProductContextAsync(Token);
        var cookie = fixture.ProductCookieHeader;
        var before = await fixture.SnapshotAsync(Token);
        await fixture.SetRevocationRecordingFailureAsync(true, Token);
        using (var request = Request(fixture, csrf))
        using (var response = await fixture.SendProductAsync(request, Token))
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Null(response.Headers.Location);
            using var problem = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
            Assert.Equal("identity_unavailable", problem.RootElement.GetProperty("code").GetString());
        }
        var rejected = await fixture.SnapshotAsync(Token);
        var session = Assert.Single(rejected.Sessions);
        Assert.Equal(Assert.Single(before.Sessions).Digest, session.Digest);
        Assert.Equal("active", session.State);
        Assert.Null(session.TerminalAt);
        Assert.Equal(before.Events, rejected.Events);
        Assert.Equal(before.Users, rejected.Users);
        Assert.Equal(before.Mappings, rejected.Mappings);
        Assert.Equal(before.RegistryRevision, rejected.RegistryRevision);
        await fixture.SetRevocationRecordingFailureAsync(false, Token);
        using var replay = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
        replay.Headers.Add("Cookie", cookie);
        using var corrected = await fixture.SendProductAsync(replay, Token, includeCookies: false);
        Assert.Equal(HttpStatusCode.OK, corrected.StatusCode);
        using var identity = JsonDocument.Parse(await corrected.Content.ReadAsByteArrayAsync(Token));
        Assert.Equal(session.UserId, identity.RootElement.GetProperty("userId").GetGuid());
        Assert.DoesNotContain((await fixture.SnapshotAsync(Token)).Events, row => row.Code == "session_revoked");
    }

    [Theory]
    [InlineData("anonymous", 401)]
    [InlineData("missing-origin", 403)]
    [InlineData("null-origin", 403)]
    [InlineData("foreign-origin", 403)]
    [InlineData("missing-csrf", 403)]
    [InlineData("anonymous-csrf", 403)]
    [InlineData("cross-site", 403)]
    [InlineData("body", 400)]
    [InlineData("query", 400)]
    [InlineData("mixed-credential", 401)]
    [InlineData("method", 405)]
    public async Task RefusedRequestsDoNotRevokeOrRecordSuccessfulRevocation(string fault, int status)
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var anonymousToken = await fixture.ProductContextAsync(Token);
        await LoginAsync(fixture);
        var csrf = await fixture.ProductContextAsync(Token);
        var before = await fixture.SnapshotAsync(Token);
        using var request = Request(fixture, fault == "anonymous-csrf" ? anonymousToken : csrf);
        if (fault == "missing-origin") request.Headers.Remove("Origin");
        if (fault is "null-origin" or "foreign-origin")
        {
            request.Headers.Remove("Origin");
            request.Headers.Add("Origin", fault == "null-origin" ? "null" : "https://foreign.example.invalid");
        }
        if (fault == "missing-csrf") request.Headers.Remove("X-Kofte-CSRF");
        if (fault == "cross-site") request.Headers.Add("Sec-Fetch-Site", "cross-site");
        if (fault == "body") request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        if (fault == "query") request.RequestUri = new("/api/identity/v1/sign-out?session=other", UriKind.Relative);
        if (fault == "mixed-credential") request.Headers.Add("Authorization", "Bearer unsupported");
        if (fault == "method") request.Method = HttpMethod.Get;
        using var response = await fixture.SendProductAsync(request, Token, includeCookies: fault != "anonymous");
        Assert.Equal(status, (int)response.StatusCode);
        if (status == 405) Assert.Equal("POST", Assert.Single(response.Content.Headers.Allow));
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal("active", Assert.Single(after.Sessions).State);
        Assert.Null(Assert.Single(after.Sessions).TerminalAt);
        Assert.Equal(before.Events, after.Events);
        Assert.Equal(before.RegistryRevision, after.RegistryRevision);
    }

    private async Task LoginAsync(LoginCommitFixture fixture)
    {
        var csrf = await fixture.ProductContextAsync(Token);
        using var request = fixture.ProductLoginRequest("/identity", csrf);
        using var start = await fixture.SendProductAsync(request, Token);
        using var complete = await fixture.CompleteProductAsync(start, Token);
        Assert.Equal(HttpStatusCode.SeeOther, complete.StatusCode);
    }

    private static HttpRequestMessage Request(LoginCommitFixture fixture, string csrf)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/sign-out");
        request.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
        request.Headers.Add("X-Kofte-CSRF", csrf);
        return request;
    }

    private sealed class SessionClock : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}

[Trait("Provider", "sqlite")]
[Collection("identity-deadlines")]
public sealed class SqliteSignOutContractTests : SignOutContractTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Collection("identity-deadlines")]
public sealed class PostgresSignOutContractTests : SignOutContractTests
{
    protected override string Provider => "postgres";
}

[CollectionDefinition("identity-deadlines", DisableParallelization = true)]
public sealed class IdentityDeadlineCollection;
