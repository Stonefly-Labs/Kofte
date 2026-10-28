using System.Collections.Concurrent;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Kofte.TestSupport.Identity;

public enum IdTokenMutation
{
    None, WrongIssuer, WrongAudience, WrongSignature, WrongAlgorithm,
    Expired, Future, MissingNonce, WrongNonce, Unsigned, WrongKey,
    IssuerCase, IssuerTrailingSlash
}

public sealed class OidcIssuer : IAsyncDisposable
{
    private sealed record Authorization(string Challenge, string Nonce, string RedirectUri, DateTimeOffset Expires);

    private readonly RSA signingKey = RSA.Create(2048);
    private readonly RSA foreignKey = RSA.Create(2048);
    private readonly ConcurrentDictionary<string, Authorization> codes = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<string> observations = new();
    private readonly X509Certificate2 authority;
    private readonly X509Certificate2 serverCertificate;
    private readonly CancellationTokenSource lifetime;
    private WebApplication? application;
    private int tokenRequests;
    private bool disposed;

    private OidcIssuer(CancellationToken token)
    {
        lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
        lifetime.CancelAfter(TimeSpan.FromSeconds(120));
        using var caKey = RSA.Create(2048);
        var caRequest = new CertificateRequest("CN=Kofte test-only identity CA", caKey,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        caRequest.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign, true));
        authority = caRequest.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(1));
        using var tlsKey = RSA.Create(2048);
        var tlsRequest = new CertificateRequest("CN=localhost", tlsKey,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName("localhost");
        names.AddIpAddress(IPAddress.Loopback);
        tlsRequest.CertificateExtensions.Add(names.Build());
        tlsRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        tlsRequest.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new("1.3.6.1.5.5.7.3.1") }, true));
        using var certificate = tlsRequest.Create(authority, DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddHours(12), RandomNumberGenerator.GetBytes(16));
        serverCertificate = certificate.CopyWithPrivateKey(tlsKey);
    }

    public Uri Issuer { get; private set; } = null!;
    public string ClientId { get; } = "kofte-identity-fixture";
    public string ClientSecret { get; private set; } = Base64Url(RandomNumberGenerator.GetBytes(32));
    public void ReplaceClientSecret() => ClientSecret = Base64Url(RandomNumberGenerator.GetBytes(32));
    public Uri? RegisteredRedirectUri { get; set; }
    public string Subject { get; set; } = "synthetic-subject";
    public string? DisplayName { get; set; }
    public bool IncludeAdministratorClaims { get; set; }
    public IdTokenMutation Mutation { get; set; }
    public bool CancelAuthorization { get; set; }
    public bool PublishSigningKey { get; set; } = true;
    public Func<string, CancellationToken, Task>? BeforeResponse { get; set; }
    public Func<string, HttpContext, IResult?>? ResponseOverride { get; set; }
    public int TokenRequests => Volatile.Read(ref tokenRequests);
    public string? IssuedIdToken { get; private set; }
    public IReadOnlyList<string> Observations => observations.ToArray();
    public string AuthorityCertificatePem => authority.ExportCertificatePem();
    internal X509Certificate2 ServerCertificate => serverCertificate;

    public static async Task<OidcIssuer> CreateAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var fixture = new OidcIssuer(token);
        try
        {
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Production"
            });
            builder.Configuration.Sources.Clear();
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MaxRequestBodySize = 8192;
                options.Limits.MaxRequestLineSize = 16_384;
                options.Limits.MaxRequestHeadersTotalSize = 16_384;
                options.Listen(IPAddress.Loopback, 0, listener => listener.UseHttps(fixture.serverCertificate));
            });
            fixture.application = builder.Build();
            fixture.application.Use(async (context, next) =>
            {
                context.Response.Headers.CacheControl = "no-store";
                await next(context);
            });
            fixture.application.MapGet("/.well-known/openid-configuration", (Delegate)fixture.DiscoveryAsync);
            fixture.application.MapGet("/jwks", (Delegate)fixture.KeysAsync);
            fixture.application.MapGet("/authorize", (Delegate)fixture.AuthorizeAsync);
            fixture.application.MapPost("/token", (Delegate)fixture.ExchangeAsync);
            await fixture.application.StartAsync(fixture.lifetime.Token);
            fixture.Issuer = new Uri(fixture.application.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single() + "/");
            using var probe = fixture.CreateClient();
            using var response = await probe.GetAsync("/.well-known/openid-configuration", fixture.lifetime.Token);
            response.EnsureSuccessStatusCode();
            fixture.observations.Clear();
            return fixture;
        }
        catch
        {
            await fixture.DisposeAsync();
            throw;
        }
    }

    public HttpClientHandler CreateHandler() => new()
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        ServerCertificateCustomValidationCallback = (_, certificate, _, errors) => ValidateCertificate(authority, certificate, errors)
    };

    public SocketsHttpHandler CreateSocketsHandler() => CreateSocketsHandler(authority);

    internal static SocketsHttpHandler CreateSocketsHandler(X509Certificate2 authority) => new()
    {
        SslOptions = new()
        {
            RemoteCertificateValidationCallback = (_, certificate, _, errors) => ValidateCertificate(authority, certificate, errors)
        }
    };

    private static bool ValidateCertificate(X509Certificate2 authority, X509Certificate? certificate, SslPolicyErrors errors)
    {
        if (certificate is null || (errors & (SslPolicyErrors.RemoteCertificateNameMismatch |
            SslPolicyErrors.RemoteCertificateNotAvailable)) != 0) return false;
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(authority);
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        using var leaf = new X509Certificate2(certificate);
        return chain.Build(leaf);
    }

    public HttpClient CreateClient() => new(CreateHandler())
    {
        BaseAddress = Issuer,
        Timeout = TimeSpan.FromSeconds(5)
    };

    private async Task ObserveAsync(string operation, CancellationToken token)
    {
        observations.Enqueue(operation);
        if (BeforeResponse is { } before) await before(operation, token);
    }

    private async Task<IResult> DiscoveryAsync(HttpContext context)
    {
        await ObserveAsync("discovery", context.RequestAborted);
        if (ResponseOverride?.Invoke("discovery", context) is { } overridden) return overridden;
        return Results.Json(new
        {
            issuer = Issuer.AbsoluteUri,
            authorization_endpoint = new Uri(Issuer, "authorize").AbsoluteUri,
            token_endpoint = new Uri(Issuer, "token").AbsoluteUri,
            jwks_uri = new Uri(Issuer, "jwks").AbsoluteUri,
            response_types_supported = new[] { "code" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" },
            token_endpoint_auth_methods_supported = new[] { "client_secret_post" },
            code_challenge_methods_supported = new[] { "S256" },
            scopes_supported = new[] { "openid" }
        });
    }

    private async Task<IResult> KeysAsync(HttpContext context)
    {
        await ObserveAsync("jwks", context.RequestAborted);
        if (ResponseOverride?.Invoke("jwks", context) is { } overridden) return overridden;
        var key = signingKey.ExportParameters(false);
        return Results.Json(new
        {
            keys = PublishSigningKey ? new[]
            {
                new { kty = "RSA", use = "sig", kid = "fixture-key", alg = "RS256",
                    n = Base64Url(key.Modulus!), e = Base64Url(key.Exponent!) }
            } : []
        });
    }

    private async Task<IResult> AuthorizeAsync(HttpContext context)
    {
        await ObserveAsync("authorization", context.RequestAborted);
        var query = context.Request.Query;
        if (!Scalars(query, "response_type", "scope", "client_id", "redirect_uri",
            "code_challenge", "code_challenge_method", "nonce", "state") ||
            query["response_type"] != "code" || query["client_id"] != ClientId ||
            !query["scope"].ToString().Split(' ').Contains("openid", StringComparer.Ordinal) ||
            RegisteredRedirectUri is null || query["redirect_uri"] != RegisteredRedirectUri.AbsoluteUri ||
            query["code_challenge_method"] != "S256" ||
            query["code_challenge"].ToString() is not { Length: 43 } challenge ||
            challenge.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '_')) ||
            string.IsNullOrEmpty(query["nonce"]) || string.IsNullOrEmpty(query["state"]))
            return OAuthFailure("invalid_request");
        if (CancelAuthorization)
            return Results.Redirect(QueryHelpers.AddQueryString(RegisteredRedirectUri.AbsoluteUri,
                new Dictionary<string, string?> { ["error"] = "access_denied", ["state"] = query["state"] }));
        var code = Base64Url(RandomNumberGenerator.GetBytes(32));
        if (!codes.TryAdd(code, new(challenge, query["nonce"].ToString(),
            RegisteredRedirectUri.AbsoluteUri, DateTimeOffset.UtcNow.AddMinutes(5))))
            throw new InvalidOperationException("identity_fixture_code_collision");
        return Results.Redirect(QueryHelpers.AddQueryString(RegisteredRedirectUri.AbsoluteUri,
            new Dictionary<string, string?> { ["code"] = code, ["state"] = query["state"] }));
    }

    private async Task<IResult> ExchangeAsync(HttpContext context)
    {
        Interlocked.Increment(ref tokenRequests);
        await ObserveAsync("token", context.RequestAborted);
        if (ResponseOverride?.Invoke("token", context) is { } overridden) return overridden;
        if (!context.Request.HasFormContentType) return OAuthFailure("invalid_request");
        IFormCollection form;
        try { form = await context.Request.ReadFormAsync(context.RequestAborted); }
        catch (InvalidDataException) { return OAuthFailure("invalid_request"); }
        catch (BadHttpRequestException) { return OAuthFailure("invalid_request"); }
        if (!Scalars(form, "grant_type", "code", "client_id", "client_secret", "redirect_uri", "code_verifier") ||
            form["grant_type"] != "authorization_code" || form["client_id"] != ClientId ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(form["client_secret"].ToString()),
                Encoding.UTF8.GetBytes(ClientSecret)))
            return OAuthFailure("invalid_client");
        if (!codes.TryRemove(form["code"].ToString(), out var authorization) ||
            authorization.Expires <= DateTimeOffset.UtcNow ||
            form["redirect_uri"] != authorization.RedirectUri ||
            form["code_verifier"].ToString() is not { Length: >= 43 and <= 128 } verifier ||
            verifier.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('-' or '.' or '_' or '~')) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(authorization.Challenge),
                Encoding.ASCII.GetBytes(Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))))))
            return OAuthFailure("invalid_grant");
        IssuedIdToken = Sign(authorization.Nonce);
        return Results.Json(new
        {
            token_type = "Bearer",
            access_token = Base64Url(RandomNumberGenerator.GetBytes(32)),
            expires_in = 300,
            id_token = IssuedIdToken
        });
    }

    private string Sign(string nonce)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var claims = new Dictionary<string, object>
        {
            ["iss"] = Mutation switch
            {
                IdTokenMutation.WrongIssuer => "https://wrong-issuer.invalid/",
                IdTokenMutation.IssuerCase => Issuer.AbsoluteUri.ToUpperInvariant(),
                IdTokenMutation.IssuerTrailingSlash => Issuer.AbsoluteUri.TrimEnd('/'),
                _ => Issuer.AbsoluteUri
            },
            ["aud"] = Mutation == IdTokenMutation.WrongAudience ? "wrong-audience" : ClientId,
            ["sub"] = Subject,
            ["iat"] = now,
            ["nbf"] = Mutation == IdTokenMutation.Future ? now + 3600 : now - 5,
            ["exp"] = Mutation == IdTokenMutation.Expired ? now - 3600 : now + 300,
            ["nonce"] = Mutation == IdTokenMutation.WrongNonce ? "wrong-nonce" : nonce
        };
        if (Mutation == IdTokenMutation.MissingNonce) claims.Remove("nonce");
        if (DisplayName is { } display) claims["name"] = display;
        if (IncludeAdministratorClaims)
        {
            claims["email"] = "administrator@example.invalid";
            claims["email_verified"] = true;
            claims["roles"] = new[] { "Administrator" };
            claims["isInitialAdministrator"] = true;
        }
        var algorithm = Mutation switch
        {
            IdTokenMutation.WrongAlgorithm => "RS384",
            IdTokenMutation.Unsigned => "none",
            _ => "RS256"
        };
        var unsigned = Base64Url(JsonSerializer.SerializeToUtf8Bytes(
            new { alg = algorithm, kid = Mutation == IdTokenMutation.WrongKey ? "foreign-key" : "fixture-key", typ = "JWT" })) +
            "." + Base64Url(JsonSerializer.SerializeToUtf8Bytes(claims));
        if (Mutation == IdTokenMutation.Unsigned) return unsigned + ".";
        var key = Mutation == IdTokenMutation.WrongSignature ? foreignKey : signingKey;
        return unsigned + "." + Base64Url(key.SignData(Encoding.ASCII.GetBytes(unsigned),
            algorithm == "RS256" ? HashAlgorithmName.SHA256 : HashAlgorithmName.SHA384, RSASignaturePadding.Pkcs1));
    }

    private static bool Scalars(IEnumerable<KeyValuePair<string, StringValues>> values, params string[] required)
    {
        var entries = values.ToDictionary(item => item.Key, item => item.Value, StringComparer.Ordinal);
        return required.All(key => entries.TryGetValue(key, out var value) && value.Count == 1);
    }

    private static IResult OAuthFailure(string code) => Results.Json(new { error = code }, statusCode: 400);
    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        if (application is not null)
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await application.StopAsync(stop.Token);
            await application.DisposeAsync();
        }
        await lifetime.CancelAsync();
        lifetime.Dispose();
        serverCertificate.Dispose();
        authority.Dispose();
        signingKey.Dispose();
        foreignKey.Dispose();
        disposed = true;
    }
}
