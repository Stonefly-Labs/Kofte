using System.Diagnostics;
using System.Net;
using System.Text;
using Kofte.TestSupport.Identity;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

public abstract class OidcBudgetTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(IdTokenMutation.WrongIssuer)]
    [InlineData(IdTokenMutation.WrongAudience)]
    [InlineData(IdTokenMutation.WrongSignature)]
    [InlineData(IdTokenMutation.WrongAlgorithm)]
    [InlineData(IdTokenMutation.Expired)]
    [InlineData(IdTokenMutation.Future)]
    [InlineData(IdTokenMutation.MissingNonce)]
    [InlineData(IdTokenMutation.WrongNonce)]
    [InlineData(IdTokenMutation.Unsigned)]
    [InlineData(IdTokenMutation.WrongKey)]
    [InlineData(IdTokenMutation.IssuerCase)]
    [InlineData(IdTokenMutation.IssuerTrailingSlash)]
    public async Task DeliveredInvalidEvidenceCannotCreateAUserSessionOrCommittedLoginEvent(
        IdTokenMutation mutation)
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var before = await fixture.SnapshotAsync(Token);
        fixture.Protocol.Mutation = mutation;
        using (var failure = await LoginAsync(fixture))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, failure.StatusCode);
            AssertNoSession(failure);
        }
        var refused = await fixture.SnapshotAsync(Token);
        Assert.Empty(refused.Users);
        Assert.Empty(refused.Mappings);
        Assert.Empty(refused.Sessions);
        Assert.Equal(before.Events, refused.Events);
        Assert.All(refused.Attempts, attempt => Assert.Equal("pending", attempt.State));
        fixture.Protocol.Mutation = IdTokenMutation.None;
        using var corrected = await LoginAsync(fixture);
        Assert.Equal(303, (int)corrected.StatusCode);
        var committed = await fixture.SnapshotAsync(Token);
        var user = Assert.Single(committed.Users);
        Assert.Equal(user.Id, Assert.Single(committed.Mappings).UserId);
        Assert.Equal(user.Id, Assert.Single(committed.Sessions).UserId);
        Assert.False((await CurrentAsync(fixture)).IsInitialAdministrator);
        Assert.Equal(2, fixture.Protocol.TokenRequests);
    }

    [Theory]
    [InlineData("discovery", "outage")]
    [InlineData("discovery", "redirect")]
    [InlineData("discovery", "oversize")]
    [InlineData("discovery", "issuer")]
    [InlineData("discovery", "destination")]
    [InlineData("jwks", "outage")]
    [InlineData("jwks", "oversize")]
    [InlineData("jwks", "missing")]
    [InlineData("token", "outage")]
    [InlineData("token", "redirect")]
    [InlineData("token", "oversize")]
    [InlineData("token", "compressed")]
    public async Task DeliveredProviderFailuresAreBoundedWithoutRetryRedirectOrPersistentIdentity(
        string operation, string fault)
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var before = await fixture.SnapshotAsync(Token);
        fixture.Protocol.ResponseOverride = (selected, context) =>
        {
            if (selected != operation) return null;
            if (fault == "compressed") context.Response.Headers.ContentEncoding = "gzip";
            return fault switch
            {
                "outage" => Results.StatusCode(503),
                "redirect" => Results.Redirect(new Uri(fixture.Protocol.Issuer, "token").AbsoluteUri),
                "oversize" => Results.Text("{}".PadRight(operation == "token" ? 65_537 : 262_145),
                    "application/json", Encoding.UTF8),
                "missing" => Results.Json(new { keys = Array.Empty<object>() }),
                "issuer" => Results.Json(new { issuer = "https://foreign.invalid/" }),
                "destination" => Results.Json(new
                {
                    issuer = fixture.Protocol.Issuer.AbsoluteUri,
                    authorization_endpoint = "https://foreign.invalid/authorize",
                    token_endpoint = "https://foreign.invalid/token",
                    jwks_uri = "https://foreign.invalid/jwks"
                }),
                _ => Results.Text("{}", "application/json")
            };
        };
        using var failure = await LoginAsync(fixture, allowStartRefusal: true);
        Assert.Contains((int)failure.StatusCode, new[] { 401, 503 });
        AssertNoSession(failure);
        var state = await fixture.SnapshotAsync(Token);
        Assert.Empty(state.Users);
        Assert.Empty(state.Mappings);
        Assert.Empty(state.Sessions);
        Assert.Equal(before.Events, state.Events);
        Assert.Equal(1, fixture.Protocol.Observations.Count(value => value == operation));
        Assert.True(fixture.Protocol.TokenRequests <= (operation == "token" ? 1 : 0));
        Assert.True(fixture.Protocol.Observations.Count(value => value is "discovery" or "jwks" or "token") <= 3);
    }

    [Fact]
    public async Task DeliveredBrowserLoginRateRefusesTheEleventhAttemptBeforeProviderOrIdentityEffects()
    {
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token,
            new AdmissionClock(), productRoutes: true);
        for (var index = 0; index < 10; index++)
        {
            fixture.Protocol.Subject = "synthetic-rate-subject-" + index;
            using var response = await LoginAsync(fixture);
            Assert.Equal(303, (int)response.StatusCode);
        }
        var before = await fixture.SnapshotAsync(Token);
        Assert.Equal(10, before.Users.Count);
        Assert.Equal(10, before.Sessions.Count);
        var requests = fixture.Protocol.Observations.Count;
        using var rejected = await LoginAsync(fixture, allowStartRefusal: true);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal(TimeSpan.FromMinutes(1), rejected.Headers.RetryAfter!.Delta);
        Assert.Equal(requests, fixture.Protocol.Observations.Count);
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(before.Attempts, after.Attempts);
        Assert.Equal(before.Events, after.Events);
        Assert.Equal(before.Sessions.Select(value => (value.Digest, value.State, value.UserId, value.Epoch)),
            after.Sessions.Select(value => (value.Digest, value.State, value.UserId, value.Epoch)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeliveredDnsChangesAreReadmittedBeforeTheRealTokenConnection(bool mixedAnswers)
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var before = await fixture.SnapshotAsync(Token);
        var resolutions = 0;
        fixture.ProtocolDns = async (host, token) =>
        {
            Interlocked.Increment(ref resolutions);
            return await Dns.GetHostAddressesAsync(host, token);
        };
        var csrf = await fixture.ProductContextAsync(Token);
        using var request = fixture.ProductLoginRequest("/identity", csrf);
        using var start = await fixture.SendProductAsync(request, Token);
        Assert.True((int)start.StatusCode == 303, fixture.RequestFailureCode);
        Assert.Equal(2, resolutions);
        fixture.ProtocolDns = (_, token) =>
        {
            token.ThrowIfCancellationRequested();
            Interlocked.Increment(ref resolutions);
            return Task.FromResult(mixedAnswers
                ? new[] { IPAddress.Loopback, IPAddress.Parse("203.0.113.7") }
                : new[] { IPAddress.Parse("203.0.113.7") });
        };
        using var refused = await fixture.CompleteProductAsync(start, Token);
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        AssertNoSession(refused);
        Assert.Equal(3, resolutions);
        Assert.Equal(0, fixture.Protocol.TokenRequests);
        Assert.Equal(new[] { "discovery", "jwks", "authorization" }, fixture.Protocol.Observations);
        var state = await fixture.SnapshotAsync(Token);
        Assert.Empty(state.Users);
        Assert.Empty(state.Mappings);
        Assert.Empty(state.Sessions);
        Assert.Equal(before.Events, state.Events);
        fixture.ProtocolDns = null;
        using var corrected = await LoginAsync(fixture);
        Assert.Equal(303, (int)corrected.StatusCode);
        Assert.Single((await fixture.SnapshotAsync(Token)).Users);
    }

    [Fact]
    public async Task DeliveredDiscoveryDeadlineIsIdentifiableWithoutRelaxingLoginOrPublishingIdentity()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var before = await fixture.SnapshotAsync(Token);
        fixture.Protocol.BeforeResponse = (operation, token) => operation == "discovery"
            ? Task.Delay(TimeSpan.FromSeconds(10), token) : Task.CompletedTask;
        var csrf = await fixture.ProductContextAsync(Token);
        using var request = fixture.ProductLoginRequest("/identity", csrf);
        var elapsed = Stopwatch.StartNew();
        using var response = await fixture.SendProductAsync(request, Token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("identity_fixture_request_deadline_exhausted", fixture.RequestFailureCode);
        var refused = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.CompleteProductAsync(response, Token));
        Assert.Equal("identity_fixture_product_challenge_failed:identity_fixture_request_deadline_exhausted",
            refused.Message);
        Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(5));
        AssertNoSession(response);
        var after = await fixture.SnapshotAsync(Token);
        Assert.Empty(after.Users);
        Assert.Empty(after.Mappings);
        Assert.Empty(after.Sessions);
        Assert.Equal(before.Events, after.Events);
        _ = await fixture.ProductContextAsync(Token);
        Assert.Equal("identity_fixture_request_failure_unobserved", fixture.RequestFailureCode);
    }

    [Fact]
    public async Task DeliveredTokenDeadlineHasNoLateCommitAndValidLocalSessionSurvivesProviderOutage()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        fixture.Protocol.BeforeResponse = (operation, token) => operation == "token"
            ? Task.Delay(TimeSpan.FromSeconds(10), token) : Task.CompletedTask;
        var elapsed = Stopwatch.StartNew();
        using (var refused = await LoginAsync(fixture))
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, refused.StatusCode);
            AssertNoSession(refused);
        }
        Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(5));
        Assert.Empty((await fixture.SnapshotAsync(Token)).Sessions);
        fixture.Protocol.BeforeResponse = null;
        using var corrected = await LoginAsync(fixture);
        Assert.Equal(303, (int)corrected.StatusCode);
        var original = await CurrentAsync(fixture);
        var requests = fixture.Protocol.Observations.Count;
        fixture.Protocol.ResponseOverride = (_, _) => Results.StatusCode(503);
        var current = await CurrentAsync(fixture);
        Assert.Equal(original, current);
        Assert.Equal(requests, fixture.Protocol.Observations.Count);
        var committed = await fixture.SnapshotAsync(Token);
        Assert.Single(committed.Users);
        Assert.Single(committed.Sessions);
        Assert.Single(committed.Attempts, attempt => attempt.State == "consumed");
        Assert.Equal(2, fixture.Protocol.TokenRequests);
    }

    [Fact]
    public async Task DeliveredProviderCancellationIsNotALoginAndDoesNotTriggerATokenExchange()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var before = await fixture.SnapshotAsync(Token);
        fixture.Protocol.CancelAuthorization = true;
        using var cancelled = await LoginAsync(fixture);
        Assert.Equal(HttpStatusCode.Unauthorized, cancelled.StatusCode);
        AssertNoSession(cancelled);
        Assert.Equal(0, fixture.Protocol.TokenRequests);
        var state = await fixture.SnapshotAsync(Token);
        Assert.Empty(state.Users);
        Assert.Empty(state.Sessions);
        Assert.Equal(before.Events, state.Events);
    }

    private async Task<HttpResponseMessage> LoginAsync(LoginCommitFixture fixture, bool allowStartRefusal = false)
    {
        var csrf = await fixture.ProductContextAsync(Token);
        using var request = fixture.ProductLoginRequest("/identity", csrf);
        var start = await fixture.SendProductAsync(request, Token);
        if (allowStartRefusal && (int)start.StatusCode != 303) return start;
        try
        {
            Assert.True((int)start.StatusCode == 303, fixture.RequestFailureCode);
            return await fixture.CompleteProductAsync(start, Token);
        }
        finally { start.Dispose(); }
    }

    private async Task<Current> CurrentAsync(LoginCommitFixture fixture)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
        using var response = await fixture.SendProductAsync(request, Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
        return new(body.RootElement.GetProperty("userId").GetGuid(),
            body.RootElement.GetProperty("identityMode").GetString()!,
            body.RootElement.GetProperty("isInitialAdministrator").GetBoolean());
    }

    private static void AssertNoSession(HttpResponseMessage response)
    {
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            Assert.DoesNotContain(cookies, value => value.StartsWith("__Host-kofte-session=", StringComparison.Ordinal));
    }

    private sealed record Current(Guid UserId, string Mode, bool IsInitialAdministrator);

    private sealed class AdmissionClock : TimeProvider
    {
        private readonly DateTimeOffset now = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => new(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, TimeSpan.Zero);
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteOidcBudgetTests : OidcBudgetTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresOidcBudgetTests : OidcBudgetTests
{
    protected override string Provider => "postgres";
}
