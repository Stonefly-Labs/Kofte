using System.Net;
using System.Text.Json;
using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Platform.ContractTests.Identity;

public abstract class LoginEndpointContractTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task DeliveredBrowserContextHasOnlyModeAndAntiforgeryWithPrivateBoundedContinuity()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/browser-context");
        using var response = await fixture.SendProductAsync(request, Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertPrivate(response);
        using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
        var body = document.RootElement;
        Assert.Equal(["csrfToken", "identityMode", "version"],
            body.EnumerateObject().Select(item => item.Name).Order().ToArray());
        Assert.Equal("1", body.GetProperty("version").GetString());
        Assert.Equal("oidc", body.GetProperty("identityMode").GetString());
        Assert.NotEmpty(body.GetProperty("csrfToken").GetString()!);
        var cookies = response.Headers.GetValues("Set-Cookie").ToArray();
        Assert.Equal(2, cookies.Length);
        Assert.All(cookies, cookie =>
        {
            var attributes = cookie.Split(';').Skip(1).Select(value => value.Trim().ToLowerInvariant()).ToArray();
            Assert.Contains("secure", attributes);
            Assert.Contains("httponly", attributes);
            Assert.Contains("samesite=lax", attributes);
            Assert.Contains("path=/", attributes);
            Assert.DoesNotContain(attributes, value => value.StartsWith("domain=", StringComparison.Ordinal));
        });
        var state = await fixture.SnapshotAsync(Token);
        Assert.Empty(state.Users);
        Assert.Empty(state.Sessions);
        Assert.Empty(state.Attempts);
    }

    [Fact]
    public async Task DeliveredMutationRequiresThePairAndExactOriginBeforeAnyAttemptOrProtocolEgress()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var csrf = await fixture.ProductContextAsync(Token);
        foreach (var (origin, token, includeCookies) in new[]
        {
            (false, csrf, true), (true, "forged-token", true), (true, csrf, false)
        })
        {
            using var request = fixture.ProductLoginRequest("/identity", token, origin);
            using var response = await fixture.SendProductAsync(request, Token, includeCookies);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            await AssertProblemAsync(response, "identity_forbidden", Token);
        }
        var state = await fixture.SnapshotAsync(Token);
        Assert.Empty(state.Attempts);
        Assert.Empty(state.Users);
        Assert.Empty(state.Sessions);
        Assert.Equal(0, fixture.ProductProviderRequests);
    }

    [Fact]
    public async Task DeliveredLoginRejectsUnknownDuplicateAndNoncanonicalReturnsInsteadOfNormalizing()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var csrf = await fixture.ProductContextAsync(Token);
        foreach (var path in new[]
        {
            "https://foreign.invalid", "//foreign.invalid", "/identity/", "/Identity",
            "/%69dentity", "/identity?x=1", "/identity#x", "/\\foreign.invalid"
        })
        {
            using var request = fixture.ProductLoginRequest(path, csrf);
            using var response = await fixture.SendProductAsync(request, Token);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            await AssertProblemAsync(response, "identity_request_invalid", Token);
        }
        foreach (var fields in new[]
        {
            new[] { new KeyValuePair<string, string>("returnPath", "/identity"),
                new KeyValuePair<string, string>("returnPath", "/") },
            [new("returnPath", "/identity"), new("userId", "caller-controlled")]
        })
        {
            using var request = fixture.ProductLoginRequest("/identity", csrf);
            request.Content = new FormUrlEncodedContent(fields);
            using var response = await fixture.SendProductAsync(request, Token);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        Assert.Empty((await fixture.SnapshotAsync(Token)).Attempts);
        Assert.Equal(0, fixture.ProductProviderRequests);
    }

    [Theory]
    [InlineData(null, "/identity")]
    [InlineData("/", "/")]
    public async Task DeliveredNativeLoginCompletesOnlyAtItsMappedCallbackAndRefreshesTheIdentityBoundToken(
        string? returnPath, string expectedReturn)
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var anonymousToken = await fixture.ProductContextAsync(Token);
        using var login = fixture.ProductLoginRequest(returnPath, anonymousToken);
        using var start = await fixture.SendProductAsync(login, Token);
        Assert.True((int)start.StatusCode == 303, fixture.ProductFailure);
        AssertPrivate(start);
        using var completed = await fixture.CompleteProductAsync(start, Token);
        Assert.True((int)completed.StatusCode == 303, fixture.ProductFailure);
        Assert.Equal(expectedReturn, completed.Headers.Location!.OriginalString);
        AssertPrivate(completed);
        Assert.Contains(completed.Headers.GetValues("Set-Cookie"),
            cookie => cookie.StartsWith("__Host-kofte-session=", StringComparison.Ordinal));
        var state = await fixture.SnapshotAsync(Token);
        var user = Assert.Single(state.Users);
        Assert.Equal(user.Id, Assert.Single(state.Sessions).UserId);
        Assert.Equal("consumed", Assert.Single(state.Attempts).State);
        var authenticatedToken = await fixture.ProductContextAsync(Token);
        Assert.False(string.Equals(anonymousToken, authenticatedToken, StringComparison.Ordinal));
        using var stale = fixture.ProductLoginRequest("/", anonymousToken);
        using var rejected = await fixture.SendProductAsync(stale, Token);
        Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
    }

    [Fact]
    public async Task NativeCorrelationAndNonceCannotReplaceTheIndependentDurableBrowserBinding()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var csrf = await fixture.ProductContextAsync(Token);
        using var request = fixture.ProductLoginRequest("/identity", csrf);
        using var start = await fixture.SendProductAsync(request, Token);
        Assert.Equal(303, (int)start.StatusCode);
        using var callback = await fixture.ProductCallbackWithoutBrowserBindingAsync(start, Token);
        Assert.Equal(HttpStatusCode.Unauthorized, callback.StatusCode);
        var state = await fixture.SnapshotAsync(Token);
        Assert.Equal("pending", Assert.Single(state.Attempts).State);
        Assert.Empty(state.Users);
        Assert.Empty(state.Sessions);
    }

    [Fact]
    public async Task DeliveredCallbackTargetLimitIsMeasuredBeforeProtocolHandlingAtExactlySixteenKiB()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        const string prefix = "/auth/oidc/callback?state=";
        foreach (var (size, expected) in new[] { (16_384, 401), (16_385, 400) })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, prefix + new string('a', size - prefix.Length));
            using var response = await fixture.SendProductAsync(request, Token, includeCookies: false);
            Assert.Equal(expected, (int)response.StatusCode);
            AssertPrivate(response);
        }
        Assert.Empty((await fixture.SnapshotAsync(Token)).Users);
    }

    [Fact]
    public async Task DeliveredBoundaryRejectsDuplicateAndTooManyAuthCookiesBeforeIssuingContinuity()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        foreach (var header in new[]
        {
            "__Host-kofte-browser=a; __Host-kofte-browser=b",
            string.Join("; ", Enumerable.Range(0, 17).Select(index => "__Host-kofte-correlation." + index + "=a"))
        })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/browser-context");
            request.Headers.Add("Cookie", header);
            using var response = await fixture.SendProductAsync(request, Token, includeCookies: false);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.False(response.Headers.Contains("Set-Cookie"));
        }
        Assert.Empty((await fixture.SnapshotAsync(Token)).Attempts);
    }

    [Fact]
    public async Task DeliveredCallbackRejectsUnsolicitedReplayAndCredentialConfusionWithoutLoginEffects()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        using (var unsolicited = new HttpRequestMessage(HttpMethod.Get, "/auth/oidc/callback?state=forged&code=forged"))
        using (var rejected = await fixture.SendProductAsync(unsolicited, Token))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
            await AssertProblemAsync(rejected, "identity_unauthenticated", Token);
        }
        using (var confused = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/browser-context"))
        {
            confused.Headers.TryAddWithoutValidation("Authorization", "Bearer caller-controlled");
            using var rejected = await fixture.SendProductAsync(confused, Token);
            Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        }
        var csrf = await fixture.ProductContextAsync(Token);
        using var login = fixture.ProductLoginRequest("/identity", csrf);
        using var start = await fixture.SendProductAsync(login, Token);
        using var completed = await fixture.CompleteProductAsync(start, Token);
        Assert.Equal(303, (int)completed.StatusCode);
        var before = await fixture.SnapshotAsync(Token);
        using var replay = await fixture.ReplayProductCallbackAsync(Token);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        AssertPrivate(replay);
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(before.Sessions.Select(item => item.Digest), after.Sessions.Select(item => item.Digest));
        Assert.Equal(before.Events, after.Events);
    }

    private static void AssertPrivate(HttpResponseMessage response)
    {
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        Assert.Equal("no-cache", response.Headers.Pragma.ToString());
        Assert.Equal(["no-referrer"], response.Headers.GetValues("Referrer-Policy"));
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, string code, CancellationToken token)
    {
        AssertPrivate(response);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token));
        Assert.Equal(code, document.RootElement.GetProperty("code").GetString());
        Assert.Matches("^[0-9a-f]{32}$", document.RootElement.GetProperty("correlationId").GetString()!);
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteLoginEndpointContractTests : LoginEndpointContractTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresLoginEndpointContractTests : LoginEndpointContractTests
{
    protected override string Provider => "postgres";
}
