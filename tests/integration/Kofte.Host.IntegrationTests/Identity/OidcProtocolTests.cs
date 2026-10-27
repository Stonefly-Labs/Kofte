using System.Net;
using System.Security.Claims;
using System.Text.Json.Nodes;
using Kofte.Host.Features.Identity;
using Kofte.Host.Features.Identity.Login;
using Kofte.TestSupport;
using Kofte.TestSupport.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

[Trait("Provider", "sqlite")]
public sealed class OidcProtocolTests
{
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task NativeHandlerValidatesRealCodePkceAndNonceBeforeTheLocalTicketSeam()
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        issuer.DisplayName = "Synthetic display";
        await using var fixture = await ProtocolHost.CreateAsync(issuer, Token);
        using var response = await fixture.LoginAsync(Token);
        Assert.Equal(HttpStatusCode.SeeOther, response.StatusCode);
        Assert.Equal("/identity", response.Headers.Location!.OriginalString);
        Assert.Equal(new[] { issuer.Issuer.AbsoluteUri, "synthetic-subject", "Synthetic display" },
            fixture.Validated.Single());
        Assert.Equal(1, issuer.TokenRequests);
        Assert.Equal(new[] { "discovery", "jwks", "authorization", "token" }, issuer.Observations);
        AssertNoSession(response);
    }

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
    public async Task IndependentlyInvalidEvidenceNeverReachesTicketSeamAndFreshCorrectedLoginWorks(
        IdTokenMutation mutation)
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        await using var fixture = await ProtocolHost.CreateAsync(issuer, Token);
        issuer.Mutation = mutation;
        using (var response = await fixture.LoginAsync(Token))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Empty(fixture.Validated);
            AssertNoSession(response);
        }
        issuer.Mutation = IdTokenMutation.None;
        using var corrected = await fixture.LoginAsync(Token);
        Assert.Equal(HttpStatusCode.SeeOther, corrected.StatusCode);
        Assert.Single(fixture.Validated);
        AssertNoSession(corrected);
    }

    [Theory]
    [InlineData("state")]
    [InlineData("correlation")]
    [InlineData("nonce")]
    [InlineData("pkce")]
    [InlineData("cancelled")]
    public async Task IndependentBrowserAndCodeBindingsCannotBeBypassed(string fault)
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        await using var fixture = await ProtocolHost.CreateAsync(issuer, Token);
        issuer.CancelAuthorization = fault == "cancelled";
        var callback = await fixture.AuthorizeAsync(Token, fault == "pkce");
        if (fault == "state")
        {
            var fields = QueryHelpers.ParseQuery(new Uri(new Uri("https://unused.invalid"), callback).Query);
            fields["state"] = "unprotected-state";
            callback = QueryHelpers.AddQueryString("/auth/oidc/callback", fields);
        }
        if (fault is "correlation" or "nonce") fixture.ClearCookies(fault);
        using var response = await fixture.CallbackAsync(callback, Token);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(fixture.Validated);
        AssertNoSession(response);
        if (fault is "state" or "correlation" or "cancelled") Assert.Equal(0, issuer.TokenRequests);
    }

    [Fact]
    public async Task MissingSigningKeysRefuseWithoutATokenCallAndCorrectionRequiresFreshDiscovery()
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        await using var fixture = await ProtocolHost.CreateAsync(issuer, Token);
        issuer.PublishSigningKey = false;
        using (var failure = await fixture.StartAsync(Token))
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, failure.StatusCode);
            AssertNoSession(failure);
        }
        Assert.Empty(fixture.Validated);
        Assert.Equal(0, issuer.TokenRequests);
        issuer.PublishSigningKey = true;
        using var corrected = await fixture.LoginAsync(Token);
        Assert.Equal(HttpStatusCode.SeeOther, corrected.StatusCode);
        Assert.Single(fixture.Validated);
    }

    [Theory]
    [InlineData("?code=unsolicited")]
    [InlineData("?state=unsolicited")]
    [InlineData("?state=unsolicited&code=first&code=second")]
    [InlineData("?state=first&state=second&code=unsolicited")]
    [InlineData("?state=unsolicited&code=first&error=access_denied")]
    [InlineData("?state=unsolicited&code=&error=access_denied")]
    [InlineData("?state=unsolicited&code=first&error_description=untrusted")]
    [InlineData("?state=unsolicited&code=first&id_token=untrusted")]
    [InlineData("?state=unsolicited&code=first&iss=https%3A%2F%2Fwrong.invalid")]
    public async Task UnsolicitedAmbiguousOrImplicitCallbacksCannotTriggerProviderOrTicket(string query)
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        await using var fixture = await ProtocolHost.CreateAsync(issuer, Token);
        using var response = await fixture.CallbackAsync("/auth/oidc/callback" + query, Token);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(fixture.Validated);
        Assert.Empty(issuer.Observations);
        AssertNoSession(response);
    }

    [Fact]
    public async Task ARealCompletedProtocolCallbackCannotBeReplayedEvenWithItsOriginalCookies()
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        await using var fixture = await ProtocolHost.CreateAsync(issuer, Token);
        var callback = await fixture.AuthorizeAsync(Token);
        var originalCookies = fixture.CookieHeader(callback);
        using var first = await fixture.CallbackAsync(callback, Token);
        Assert.Equal(HttpStatusCode.SeeOther, first.StatusCode);
        using var replayed = await fixture.CallbackAsync(callback, Token, originalCookies);
        Assert.Equal(HttpStatusCode.Unauthorized, replayed.StatusCode);
        Assert.Single(fixture.Validated);
        Assert.Equal(2, issuer.TokenRequests);
        AssertNoSession(replayed);
    }

    [Theory]
    [InlineData("outage")]
    [InlineData("redirect")]
    public async Task TokenFailuresAreNotRetriedOrFollowedAndFreshCorrectedLoginWorks(string fault)
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        await using var fixture = await ProtocolHost.CreateAsync(issuer, Token);
        issuer.ResponseOverride = (operation, _) => operation != "token" ? null : fault == "outage"
            ? Results.StatusCode(503) : Results.Redirect(new Uri(issuer.Issuer, "token").AbsoluteUri);
        using (var response = await fixture.LoginAsync(Token))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            AssertNoSession(response);
        }
        Assert.Empty(fixture.Validated);
        Assert.Equal(1, issuer.TokenRequests);
        issuer.ResponseOverride = null;
        using var corrected = await fixture.LoginAsync(Token);
        Assert.Equal(HttpStatusCode.SeeOther, corrected.StatusCode);
        Assert.Single(fixture.Validated);
        Assert.Equal(2, issuer.TokenRequests);
    }

    [Fact]
    public async Task ActualCallbackDisconnectCancelsProviderAndNeverReachesTicketSeam()
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        await using var fixture = await ProtocolHost.CreateAsync(issuer, Token);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        issuer.BeforeResponse = async (operation, token) =>
        {
            if (operation != "token") return;
            entered.SetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            finally { cancelled.SetResult(); }
        };
        var callback = await fixture.AuthorizeAsync(Token);
        using var disconnected = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var exchange = fixture.CallbackAsync(callback, disconnected.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        await disconnected.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => exchange);
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        await fixture.CallbackFinished.Task.WaitAsync(TimeSpan.FromSeconds(5), Token);
        Assert.Empty(fixture.Validated);
        Assert.Equal(1, issuer.TokenRequests);
    }

    [Theory]
    [InlineData("metadata-issuer")]
    [InlineData("metadata-token-endpoint")]
    [InlineData("metadata-client-method")]
    [InlineData("keys-malformed")]
    [InlineData("keys-private")]
    [InlineData("keys-duplicate")]
    [InlineData("keys-seventeen")]
    [InlineData("keys-sixteen")]
    public async Task IndependentlyMalformedDiscoveryAndKeysFailClosedBeforeAnyToken(string fault)
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        using var providerClient = issuer.CreateClient();
        var metadata = JsonNode.Parse(await providerClient.GetStringAsync("/.well-known/openid-configuration", Token))!;
        var keys = JsonNode.Parse(await providerClient.GetStringAsync("/jwks", Token))!;
        switch (fault)
        {
            case "metadata-issuer": metadata["issuer"] = issuer.Issuer.AbsoluteUri.ToUpperInvariant(); break;
            case "metadata-token-endpoint": metadata["token_endpoint"] = "https://wrong.invalid/token"; break;
            case "metadata-client-method": metadata["token_endpoint_auth_methods_supported"] = new JsonArray("client_secret_basic"); break;
            case "keys-malformed": keys["keys"]![0]!["n"] = "not-a-valid-modulus"; break;
            case "keys-private": keys["keys"]![0]!["d"] = "synthetic-private-component"; break;
            case "keys-duplicate": keys["keys"]!.AsArray().Add(keys["keys"]![0]!.DeepClone()); break;
            case "keys-seventeen":
            case "keys-sixteen":
                for (var index = 1; index < (fault == "keys-sixteen" ? 16 : 17); index++)
                {
                    var key = keys["keys"]![0]!.DeepClone();
                    key["kid"] = "independent-key-" + index;
                    keys["keys"]!.AsArray().Add(key);
                }
                break;
        }
        issuer.ResponseOverride = (operation, _) => operation switch
        {
            "discovery" => Results.Text(metadata.ToJsonString(), "application/json"),
            "jwks" => Results.Text(keys.ToJsonString(), "application/json"),
            _ => null
        };
        await using var fixture = await ProtocolHost.CreateAsync(issuer, Token);
        using var response = await fixture.StartAsync(Token);
        Assert.Equal(fault == "keys-sixteen" ? HttpStatusCode.Found : HttpStatusCode.ServiceUnavailable,
            response.StatusCode);
        Assert.Empty(fixture.Validated);
        Assert.Equal(0, issuer.TokenRequests);
        AssertNoSession(response);
    }

    private static void AssertNoSession(HttpResponseMessage response) =>
        Assert.DoesNotContain(response.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : [],
            cookie => !cookie.StartsWith("__Host-kofte-nonce.", StringComparison.Ordinal) &&
                !cookie.StartsWith("__Host-kofte-correlation.", StringComparison.Ordinal));

    // This seam exercises the real maintained protocol handler, not session persistence.
    // Full product-route/owner-commit evidence is additionally required by T025/T030/T037.
    private sealed class ProtocolHost : IAsyncDisposable
    {
        private readonly OwnedTestRoot root = new();
        private readonly CookieContainer cookies = new();
        private readonly Uri publicOrigin = new("https://kofte.identity.test");
        private WebApplication application = null!;
        private HttpClient client = null!;
        private HttpClient providerClient = null!;
        internal List<string?[]> Validated { get; } = [];
        internal TaskCompletionSource CallbackFinished { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal static async Task<ProtocolHost> CreateAsync(OidcIssuer issuer, CancellationToken token)
        {
            var host = new ProtocolHost { providerClient = issuer.CreateClient() };
            try
            {
                Directory.CreateDirectory(host.root.Path);
                var provider = new IdentityProviderConfiguration(issuer.Issuer.AbsoluteUri,
                    new(issuer.Issuer, ".well-known/openid-configuration"),
                    new(issuer.Issuer, "authorize"), new(issuer.Issuer, "token"), new(issuer.Issuer, "jwks"),
                    issuer.ClientId, "/test-owned-unused-reference", issuer.ClientSecret,
                    new[] { ".well-known/openid-configuration", "authorize", "token", "jwks" }
                        .Select(path => new Uri(issuer.Issuer, path)).ToArray(),
                    [System.Net.IPNetwork.Parse("127.0.0.0/8")]);
                var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
                {
                    EnvironmentName = "Production",
                    ContentRootPath = host.root.Path
                });
                builder.Configuration.Sources.Clear();
                builder.Logging.ClearProviders();
                builder.WebHost.ConfigureKestrel(server => server.Listen(IPAddress.Loopback, 0));
                builder.Services.AddHttpContextAccessor();
                builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
                builder.Services.AddSingleton(services => new OidcBackchannel(provider,
                    services.GetRequiredService<IHttpContextAccessor>(), issuer.CreateSocketsHandler()));
                builder.Services.AddAuthentication().AddCookie(OidcConfiguration.SessionScheme)
                    .AddOpenIdConnect(OidcConfiguration.ProviderScheme, _ => { });
                builder.Services.AddOptions<OpenIdConnectOptions>(OidcConfiguration.ProviderScheme)
                    .Configure<OidcBackchannel>((options, backchannel) =>
                    {
                        OidcConfiguration.Apply(options, provider, host.publicOrigin, backchannel);
                        options.Events.OnTicketReceived = context =>
                        {
                            var principal = context.Principal ?? throw new InvalidOperationException("fixture_principal_missing");
                            host.Validated.Add([principal.FindFirstValue("iss"),
                                principal.FindFirstValue("sub"), principal.FindFirstValue("name")]);
                            context.HandleResponse();
                            context.Response.StatusCode = 303;
                            context.Response.Headers.Location = "/identity";
                            return Task.CompletedTask;
                        };
                        options.Events.OnRemoteFailure = context =>
                        {
                            context.HandleResponse();
                            context.Response.StatusCode = 401;
                            return Task.CompletedTask;
                        };
                    });
                host.application = builder.Build();
                host.application.Use(async (context, next) =>
                {
                    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
                    timeout.CancelAfter(TimeSpan.FromSeconds(4));
                    context.RequestAborted = timeout.Token;
                    try { await next(context); }
                    catch (OidcProviderException) { context.Response.StatusCode = 503; }
                    finally
                    {
                        if (context.Request.Path == "/auth/oidc/callback") host.CallbackFinished.TrySetResult();
                    }
                });
                host.application.UseAuthentication();
                host.application.MapGet("/start", (HttpContext context) => context.ChallengeAsync(
                    OidcConfiguration.ProviderScheme, new AuthenticationProperties { RedirectUri = "/identity" }));
                await host.application.StartAsync(token);
                host.client = new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
                {
                    BaseAddress = new(host.application.Services.GetRequiredService<IServer>()
                        .Features.Get<IServerAddressesFeature>()!.Addresses.Single()),
                    Timeout = TimeSpan.FromSeconds(6)
                };
                issuer.RegisteredRedirectUri = new(host.publicOrigin, "/auth/oidc/callback");
                return host;
            }
            catch { await host.DisposeAsync(); throw; }
        }

        internal Task<HttpResponseMessage> StartAsync(CancellationToken token) => SendAsync("/start", token);
        internal async Task<HttpResponseMessage> LoginAsync(CancellationToken token) =>
            await CallbackAsync(await AuthorizeAsync(token), token);

        internal async Task<string> AuthorizeAsync(CancellationToken token, bool wrongPkce = false)
        {
            using var start = await StartAsync(token);
            Assert.Equal(HttpStatusCode.Found, start.StatusCode);
            var authorization = start.Headers.Location!.AbsoluteUri;
            if (wrongPkce)
            {
                var uri = new Uri(authorization);
                var fields = QueryHelpers.ParseQuery(uri.Query);
                fields["code_challenge"] = new string('a', 43);
                authorization = QueryHelpers.AddQueryString(uri.GetLeftPart(UriPartial.Path),
                    fields.Select(pair => new KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues>(
                        pair.Key, pair.Value)));
            }
            using var response = await providerClient.GetAsync(authorization, token);
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            return response.Headers.Location!.PathAndQuery;
        }

        internal Task<HttpResponseMessage> CallbackAsync(string callback, CancellationToken token,
            string? originalCookies = null) => SendAsync(callback, token, originalCookies);

        internal string CookieHeader(string callback) => cookies.GetCookieHeader(new Uri(publicOrigin, callback));

        internal void ClearCookies(string fragment)
        {
            foreach (Cookie cookie in cookies.GetAllCookies())
                if (cookie.Name.Contains(fragment, StringComparison.Ordinal)) cookie.Expired = true;
        }

        private async Task<HttpResponseMessage> SendAsync(string path, CancellationToken token,
            string? originalCookies = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            var cookie = originalCookies ?? CookieHeader(path);
            if (cookie.Length != 0) request.Headers.Add("Cookie", cookie);
            var response = await client.SendAsync(request, token);
            if (response.Headers.TryGetValues("Set-Cookie", out var values))
                foreach (var value in values) cookies.SetCookies(new Uri(publicOrigin, path), value);
            return response;
        }

        public async ValueTask DisposeAsync()
        {
            client?.Dispose();
            providerClient?.Dispose();
            if (application is not null)
            {
                using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await application.StopAsync(stop.Token);
                await application.DisposeAsync();
            }
            root.Dispose();
        }
    }
}
