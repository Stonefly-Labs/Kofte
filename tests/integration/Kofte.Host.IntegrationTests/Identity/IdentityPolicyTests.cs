using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.Bootstrap;
using Kofte.Host.Features.Identity;
using Kofte.TestSupport.Identity;
using Kofte.TestSupport;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

[Trait("Provider", "sqlite")]
public sealed class IdentityPolicyTests
{
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SharedBoundaryPreservesExactHealthAndDeniesUntrustedOrUnregisteredExecution()
    {
        await using var host = await PolicyHost.CreateAsync(Token);
        foreach (var (method, path, status, allow) in new[]
        {
            (HttpMethod.Get, "/health/live", 200, (string?)null),
            (HttpMethod.Get, "/health/live?private=canary", 400, null),
            (HttpMethod.Head, "/health/live", 405, "GET"),
            (HttpMethod.Options, "/health/live", 405, "GET"),
            (HttpMethod.Get, "/Health/live", 404, null),
            (HttpMethod.Get, "/health/live/", 404, null),
            (HttpMethod.Get, "/api/identity/v1/me", 401, null),
            (HttpMethod.Get, "/api/identity/v1/me?userId=canary", 400, null),
            (HttpMethod.Head, "/api/identity/v1/me", 405, "GET"),
            (HttpMethod.Post, "/api/identity/v1/me", 405, "GET"),
            (HttpMethod.Get, "/api/identity/v1/Me", 404, null),
            (HttpMethod.Get, "/not-an-operation", 404, null),
            (HttpMethod.Get, "/test-owned/protected", 401, null),
            (HttpMethod.Get, "/test-owned/illegally-public", 404, null)
        })
        {
            using var request = new HttpRequestMessage(method, path);
            using var response = await host.Client.SendAsync(request, Token);
            Assert.Equal(status, (int)response.StatusCode);
            if (allow is not null) Assert.Equal([allow], response.Content.Headers.Allow);
            if (status == 401) await AssertProblemAsync(response, "identity_unauthenticated", Token);
            if (path.StartsWith("/api/", StringComparison.Ordinal))
            {
                Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
                Assert.Equal("no-cache", response.Headers.Pragma.ToString());
                Assert.Equal(["no-referrer"], response.Headers.GetValues("Referrer-Policy"));
            }
        }
        using var authorization = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/browser-context");
        authorization.Headers.TryAddWithoutValidation("Authorization", "Bearer private-canary");
        using var refused = await host.Client.SendAsync(authorization, Token);
        Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        await AssertProblemAsync(refused, "identity_unauthenticated", Token);
    }

    [Fact]
    public async Task RealFrameworkPairAndExactOriginAreRequiredBeforeEveryJsonMutation()
    {
        await using var host = await PolicyHost.CreateAsync(Token);
        using var browser = await host.Client.GetAsync("/api/identity/v1/browser-context", Token);
        Assert.Equal(HttpStatusCode.OK, browser.StatusCode);
        var json = await browser.Content.ReadFromJsonAsync<JsonElement>(Token);
        var token = json.GetProperty("csrfToken").GetString()!;
        Assert.NotEmpty(token);
        foreach (var (origin, csrf, site, expected) in new[]
        {
            ((string?)null, token, "same-origin", 403),
            ("null", token, "same-origin", 403),
            ("https://foreign.invalid", token, "same-origin", 403),
            (host.Origin + "/", token, "same-origin", 403),
            (host.Origin, (string?)null, "same-origin", 403),
            (host.Origin, "private-forgery", "same-origin", 403),
            (host.Origin, token, "cross-site", 403),
            (host.Origin, token, "same-origin", 204)
        })
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/development-session")
            { Content = JsonContent.Create(new { identityKey = "synthetic" }) };
            if (origin is not null) request.Headers.TryAddWithoutValidation("Origin", origin);
            if (csrf is not null) request.Headers.TryAddWithoutValidation("X-Kofte-CSRF", csrf);
            request.Headers.TryAddWithoutValidation("Sec-Fetch-Site", site);
            using var response = await host.Client.SendAsync(request, Token);
            Assert.Equal(expected, (int)response.StatusCode);
            if (expected == 403) await AssertProblemAsync(response, "identity_forbidden", Token);
            Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
            Assert.Equal("no-cache", response.Headers.Pragma.ToString());
            Assert.Equal(["no-referrer"], response.Headers.GetValues("Referrer-Policy"));
        }
        Assert.Equal(1, host.Mutations);
        using var withoutCookie = new HttpClient(new HttpClientHandler { UseCookies = false })
        { BaseAddress = host.Client.BaseAddress };
        using var missingPair = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/development-session")
        { Content = JsonContent.Create(new { identityKey = "synthetic" }) };
        missingPair.Headers.TryAddWithoutValidation("Origin", host.Origin);
        missingPair.Headers.TryAddWithoutValidation("X-Kofte-CSRF", token);
        using var rejected = await withoutCookie.SendAsync(missingPair, Token);
        Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
        Assert.Equal(1, host.Mutations);
    }

    [Fact]
    public async Task ConfiguredHttpsIngressKeepsSecureAntiforgeryAcrossTheFixedHttpBackend()
    {
        await using var host = await PolicyHost.CreateAsync(Token, oidc: true);
        using var browser = await host.Client.GetAsync("/api/identity/v1/browser-context", Token);
        Assert.Equal(HttpStatusCode.OK, browser.StatusCode);
        var cookie = Assert.Single(browser.Headers.GetValues("Set-Cookie"));
        Assert.Equal("__Host-kofte-antiforgery", cookie[..cookie.IndexOf('=')]);
        var attributes = cookie.Split(';').Skip(1).Select(item => item.Trim().ToLowerInvariant()).ToArray();
        Assert.Contains("secure", attributes);
        Assert.Contains("httponly", attributes);
        Assert.Contains("path=/", attributes);
        Assert.DoesNotContain(attributes, item => item.StartsWith("domain=", StringComparison.Ordinal));
        var json = await browser.Content.ReadFromJsonAsync<JsonElement>(Token);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/auth/login")
        { Content = new FormUrlEncodedContent(new Dictionary<string, string>()) };
        request.Headers.TryAddWithoutValidation("Origin", host.Origin);
        request.Headers.TryAddWithoutValidation("Cookie", cookie[..cookie.IndexOf(';')]);
        request.Headers.TryAddWithoutValidation("X-Kofte-CSRF", json.GetProperty("csrfToken").GetString());
        request.Headers.TryAddWithoutValidation("X-Forwarded-Proto", "http");
        using var response = await host.Client.SendAsync(request, Token);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, host.Mutations);
    }

    [Fact]
    public async Task CallbackUsesItsOwnBindingRatherThanBrowserMutationOriginOrCsrf()
    {
        await using var host = await PolicyHost.CreateAsync(Token);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/auth/oidc/callback?state=opaque&code=opaque");
        request.Headers.TryAddWithoutValidation("Origin", "https://provider.invalid");
        using var response = await host.Client.SendAsync(request, Token);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(["required"], response.Headers.GetValues("X-Test-Protocol-Binding"));
        await AssertProblemAsync(response, "identity_forbidden", Token);
        Assert.Equal(0, host.Mutations);
    }

    private static async Task AssertProblemAsync(HttpResponseMessage response, string code, CancellationToken token)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        var text = await response.Content.ReadAsStringAsync(token);
        Assert.DoesNotContain("canary", text, StringComparison.Ordinal);
        using var json = JsonDocument.Parse(text);
        Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
        Assert.Equal((int)response.StatusCode, json.RootElement.GetProperty("status").GetInt32());
        Assert.Matches("^[0-9a-f]{32}$", json.RootElement.GetProperty("correlationId").GetString()!);
        Assert.Equal(["code", "correlationId", "status", "title", "type"],
            json.RootElement.EnumerateObject().Select(item => item.Name).Order().ToArray());
    }

    private sealed class PolicyHost : IAsyncDisposable
    {
        private readonly IdentityFixture fixture;
        private readonly IdentityConfiguration configuration;
        private readonly WebApplication app;
        internal HttpClient Client { get; }
        internal string Origin => configuration.PublicOrigin.OriginalString;
        internal int Mutations { get; private set; }

        private PolicyHost(IdentityFixture fixture, IdentityConfiguration configuration, WebApplication app)
        {
            (this.fixture, this.configuration, this.app) = (fixture, configuration, app);
            Client = new(new HttpClientHandler { AllowAutoRedirect = false })
            { BaseAddress = fixture.HostUrl, Timeout = TimeSpan.FromSeconds(5) };
        }

        internal static async Task<PolicyHost> CreateAsync(CancellationToken token, bool oidc = false)
        {
            var fixture = await IdentityFixture.CreateAsync("sqlite", token);
            IdentityConfiguration? configuration = null;
            WebApplication? app = null;
            try
            {
                var bootstrap = BootstrapConfiguration.Parse(new Dictionary<string, string?>
                {
                    ["KOFTE_DATA_ROOT"] = fixture.Storage.DataRoot,
                    ["KOFTE_HOST_URL"] = fixture.HostUrl.AbsoluteUri,
                    ["KOFTE_BACKEND_ORIGIN"] = fixture.HostUrl.AbsoluteUri,
                    ["KOFTE_WEB_URL"] = fixture.WebUrl.AbsoluteUri
                }, fixture.PrivateDirectory);
                var value = new JsonObject
                {
                    ["version"] = "1",
                    ["mode"] = oidc ? "oidc" : "development",
                    ["publicOrigin"] = oidc ? "https://kofte.invalid" : fixture.WebUrl.GetLeftPart(UriPartial.Authority),
                    ["keyRingDirectory"] = fixture.KeyRingDirectory
                };
                if (oidc) ConfigureOidc(value, fixture);
                else value["development"] = new JsonObject
                {
                    ["enabled"] = true,
                    ["identities"] = new JsonArray(new JsonObject { ["key"] = "synthetic" })
                };
                configuration = IdentityConfiguration.Parse(JsonSerializer.SerializeToUtf8Bytes(value),
                    bootstrap, "Development");
                var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
                { EnvironmentName = "Development" });
                builder.Logging.ClearProviders();
                builder.WebHost.UseUrls(fixture.HostUrl.AbsoluteUri);
                builder.Services.AddSingleton(bootstrap);
                builder.Services.AddSingleton<IPlatformStore>(new SqlitePlatformStore(fixture.Storage.DataRoot));
                builder.Services.AddSingleton<HealthHandler>();
                builder.Services.AddDataProtection().PersistKeysToFileSystem(new(fixture.KeyRingDirectory));
                builder.Services.AddIdentityBoundary(configuration);
                app = builder.Build();
                var host = new PolicyHost(fixture, configuration, app);
                app.MapBootstrapHealth();
                app.UseIdentityBoundary();
                app.MapGet("/api/identity/v1/browser-context", (HttpContext context, IAntiforgery antiforgery) =>
                    Results.Json(new { csrfToken = antiforgery.GetAndStoreTokens(context).RequestToken }))
                    .AllowAnonymous();
                app.MapPost("/api/identity/v1/development-session", async (HttpContext context) =>
                {
                    if (!await IdentityRequestBoundary.ValidateMutationAsync(context)) return;
                    host.Mutations++;
                    context.Response.StatusCode = 204;
                }).AllowAnonymous();
                app.MapPost("/auth/login", () =>
                {
                    host.Mutations++;
                    return Results.NoContent();
                }).AllowAnonymous();
                app.MapGet("/auth/oidc/callback", async (HttpContext context) =>
                {
                    context.Response.Headers["X-Test-Protocol-Binding"] = "required";
                    await IdentityProblem.WriteAsync(context, IdentityFailure.Forbidden);
                }).AllowAnonymous();
                app.MapGet("/api/identity/v1/me", () => Results.NoContent())
                    .RequireAuthorization(IdentityRegistration.CurrentIdentityPolicy);
                app.MapGet("/test-owned/protected", () => Results.NoContent());
                app.MapGet("/test-owned/illegally-public", () => Results.NoContent()).AllowAnonymous();
                await app.StartAsync(token);
                return host;
            }
            catch
            {
                if (app is not null) await app.DisposeAsync();
                configuration?.Dispose();
                await fixture.DisposeAsync();
                throw;
            }
        }

        private static void ConfigureOidc(JsonObject value, IdentityFixture fixture)
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest("CN=Kofte test-only policy encryption",
                key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5),
                DateTimeOffset.UtcNow.AddDays(1));
            var certificateFile = Path.Combine(fixture.PrivateDirectory, "certificate.pem");
            var keyFile = Path.Combine(fixture.PrivateDirectory, "private-key.pem");
            var secretFile = Path.Combine(fixture.PrivateDirectory, "client-secret");
            PrivateTestFiles.WriteNew(certificateFile, Encoding.ASCII.GetBytes(certificate.ExportCertificatePem()));
            PrivateTestFiles.WriteNew(keyFile, Encoding.ASCII.GetBytes(key.ExportPkcs8PrivateKeyPem()));
            PrivateTestFiles.WriteNew(secretFile, RandomNumberGenerator.GetBytes(32).Select(item => (byte)('a' + item % 26)).ToArray());
            value["keyEncryptionCertificateFile"] = certificateFile;
            value["keyEncryptionPrivateKeyFile"] = keyFile;
            value["provider"] = new JsonObject
            {
                ["issuer"] = "https://offline-issuer.invalid",
                ["metadataAddress"] = "https://offline-issuer.invalid/discovery",
                ["authorizationEndpoint"] = "https://offline-issuer.invalid/authorize",
                ["tokenEndpoint"] = "https://offline-issuer.invalid/token",
                ["jwksUri"] = "https://offline-issuer.invalid/keys",
                ["clientId"] = "test-client",
                ["clientSecretFile"] = secretFile,
                ["allowedEndpoints"] = new JsonArray("https://offline-issuer.invalid/discovery",
                    "https://offline-issuer.invalid/authorize", "https://offline-issuer.invalid/token",
                    "https://offline-issuer.invalid/keys"),
                ["allowedNetworks"] = new JsonArray("203.0.113.0/24")
            };
        }

        public async ValueTask DisposeAsync()
        {
            await app.StopAsync();
            await app.DisposeAsync();
            Client.Dispose();
            configuration.Dispose();
            await fixture.DisposeAsync();
        }
    }
}
