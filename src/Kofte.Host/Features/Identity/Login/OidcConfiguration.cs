using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Kofte.Host.Features.Identity.Login;

internal static class OidcConfiguration
{
    internal const string ProviderScheme = "kofte.oidc.v1";
    internal const string SessionScheme = "kofte.session.v1";

    internal static void Apply(OpenIdConnectOptions options, IdentityProviderConfiguration provider,
        Uri publicOrigin, OidcBackchannel backchannel, TimeProvider? cacheClock = null)
    {
        options.SignInScheme = SessionScheme;
        options.ClientId = provider.ClientId;
        options.ClientSecret = provider.ClientSecret ?? throw new OidcProviderException(true);
        options.CallbackPath = "/auth/oidc/callback";
        options.RemoteSignOutPath = PathString.Empty;
        options.SignedOutCallbackPath = PathString.Empty;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.ResponseMode = OpenIdConnectResponseMode.Query;
        options.AuthenticationMethod = OpenIdConnectRedirectBehavior.RedirectGet;
        options.UsePkce = true;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.MapInboundClaims = false;
        options.ClaimActions.Clear();
        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = false;
        options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
        options.DisableTelemetry = true;
        options.RequireHttpsMetadata = true;
        options.Backchannel = backchannel.Client;
        var metadata = new BoundedConfiguration(provider, backchannel.Client, cacheClock ?? TimeProvider.System);
        options.ConfigurationManager = metadata;
        options.RemoteAuthenticationTimeout = TimeSpan.FromMinutes(5);
        options.NonceCookie.Name = "__Host-kofte-nonce.";
        options.CorrelationCookie.Name = "__Host-kofte-correlation.";
        foreach (var cookie in new[] { options.NonceCookie, options.CorrelationCookie })
        {
            cookie.Path = "/";
            cookie.Domain = null;
            cookie.HttpOnly = true;
            cookie.SecurePolicy = CookieSecurePolicy.Always;
            cookie.SameSite = SameSiteMode.Lax;
            cookie.Expiration = TimeSpan.FromMinutes(5);
        }
        options.ProtocolValidator.RequireNonce = true;
        options.ProtocolValidator.RequireTimeStampInNonce = true;
        options.ProtocolValidator.NonceLifetime = TimeSpan.FromMinutes(5);
        options.ProtocolValidator.RequireState = true;
        // The native handler validates protected state/correlation, then clears protocol state.
        options.ProtocolValidator.RequireStateValidation = false;
        options.TokenValidationParameters = new()
        {
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeyValidator = (key, _, _) => metadata.IsCurrentKey(key),
            TryAllIssuerSigningKeys = false,
            ValidateIssuer = true,
            ValidIssuer = provider.Issuer,
            IssuerValidator = (issuer, _, _) => string.Equals(issuer, provider.Issuer, StringComparison.Ordinal)
                ? issuer : throw new SecurityTokenInvalidIssuerException("identity_issuer_refused"),
            ValidateAudience = true,
            RequireAudience = true,
            ValidAudience = provider.ClientId,
            IgnoreTrailingSlashWhenValidatingAudience = false,
            AudienceValidator = (audiences, _, _) =>
            {
                var values = audiences.ToArray();
                return values.Length == 1 && string.Equals(values[0], provider.ClientId, StringComparison.Ordinal);
            },
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(60),
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            IncludeTokenOnFailedValidation = false,
            LogValidationExceptions = false
        };
        options.UseSecurityTokenValidator = false;
        options.TokenHandler = new SignedTokenHandler { MapInboundClaims = false, MaximumTokenSizeInBytes = 16_384 };
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            context.ProtocolMessage.RedirectUri = new Uri(publicOrigin, options.CallbackPath.Value!).AbsoluteUri;
            return Task.CompletedTask;
        };
        options.Events.OnMessageReceived = context =>
        {
            var query = context.Request.Query;
            string[] fields = ["code", "state", "iss", "session_state", "error", "error_description", "error_uri"];
            var hasCode = query.TryGetValue("code", out var code);
            var hasError = query.TryGetValue("error", out var error);
            if (context.Request.Method != "GET" || query.Any(pair =>
                !fields.Contains(pair.Key, StringComparer.Ordinal) || pair.Value.Count != 1) ||
                !query.TryGetValue("state", out var state) || string.IsNullOrEmpty(state[0]) ||
                hasCode == hasError || hasCode && string.IsNullOrEmpty(code[0]) ||
                hasError && string.IsNullOrEmpty(error[0]) ||
                !hasError && (query.ContainsKey("error_description") || query.ContainsKey("error_uri")) ||
                query.TryGetValue("iss", out var issuer) && issuer[0] != provider.Issuer)
                context.Fail("identity_request_invalid");
            return Task.CompletedTask;
        };
    }

    private sealed class SignedTokenHandler : JsonWebTokenHandler
    {
        public override Task<TokenValidationResult> ValidateTokenAsync(string token, TokenValidationParameters parameters)
        {
            // The OIDC code-flow handler otherwise clears RequireSignedTokens for a TLS token response.
            var signed = parameters.Clone();
            signed.RequireSignedTokens = true;
            return base.ValidateTokenAsync(token, signed);
        }
    }

    private sealed class BoundedConfiguration(IdentityProviderConfiguration provider, HttpClient client,
        TimeProvider clock) :
        IConfigurationManager<OpenIdConnectConfiguration>
    {
        private readonly SemaphoreSlim gate = new(1, 1);
        private readonly object sync = new();
        private OpenIdConnectConfiguration? cached;
        private long loadedAt;
        private long refreshAt;
        private bool refresh;
        private bool refreshCooldown;

        public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancel)
        {
            await gate.WaitAsync(cancel);
            try
            {
                lock (sync)
                {
                    if (cached is not null && !refresh &&
                        clock.GetElapsedTime(loadedAt) < TimeSpan.FromMinutes(15))
                        return cached;
                    if (cached is null && refreshCooldown &&
                        clock.GetElapsedTime(refreshAt) < TimeSpan.FromMinutes(1))
                        throw new OidcProviderException(true);
                    if (refreshCooldown) refreshAt = clock.GetTimestamp();
                    cached = null;
                    refresh = false;
                }
                using var metadata = OidcBackchannel.Parse(await client.GetByteArrayAsync(provider.MetadataAddress, cancel));
                var root = metadata.RootElement;
                if (Text(root, "issuer") != provider.Issuer ||
                    Text(root, "authorization_endpoint") != provider.AuthorizationEndpoint.OriginalString ||
                    Text(root, "token_endpoint") != provider.TokenEndpoint.OriginalString ||
                    Text(root, "jwks_uri") != provider.JwksUri.OriginalString ||
                    !Contains(root, "response_types_supported", "code") ||
                    !Contains(root, "code_challenge_methods_supported", "S256") ||
                    !Contains(root, "id_token_signing_alg_values_supported", "RS256") ||
                    !Contains(root, "token_endpoint_auth_methods_supported", "client_secret_post"))
                    throw new OidcProviderException(false);
                using var keys = OidcBackchannel.Parse(await client.GetByteArrayAsync(provider.JwksUri, cancel));
                if (!keys.RootElement.TryGetProperty("keys", out var list) ||
                    list.ValueKind != JsonValueKind.Array || list.GetArrayLength() is < 1 or > 16)
                    throw new OidcProviderException(false);
                var configuration = new OpenIdConnectConfiguration
                {
                    Issuer = provider.Issuer,
                    AuthorizationEndpoint = provider.AuthorizationEndpoint.OriginalString,
                    TokenEndpoint = provider.TokenEndpoint.OriginalString,
                    JwksUri = provider.JwksUri.OriginalString
                };
                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var key in list.EnumerateArray())
                {
                    if (Text(key, "kty") != "RSA" ||
                        key.TryGetProperty("use", out _) && Text(key, "use") != "sig" ||
                        key.TryGetProperty("alg", out _) && Text(key, "alg") != "RS256")
                        continue;
                    var id = Text(key, "kid");
                    if (id.Length == 0 || !ids.Add(id) ||
                        new[] { "d", "p", "q", "dp", "dq", "qi", "oth" }.Any(name => key.TryGetProperty(name, out _)))
                        throw new OidcProviderException(false);
                    RSAParameters parameters;
                    try
                    {
                        parameters = new()
                        {
                            Modulus = WebEncoders.Base64UrlDecode(Text(key, "n")),
                            Exponent = WebEncoders.Base64UrlDecode(Text(key, "e"))
                        };
                        if (parameters.Modulus.Length is < 256 or > 1024 || parameters.Exponent.Length is < 1 or > 8)
                            throw new OidcProviderException(false);
                        using var rsa = RSA.Create(parameters);
                        if (rsa.KeySize is < 2048 or > 8192) throw new OidcProviderException(false);
                    }
                    catch (Exception error) when (error is FormatException or CryptographicException or ArgumentException)
                    {
                        throw new OidcProviderException(false);
                    }
                    configuration.SigningKeys.Add(new RsaSecurityKey(parameters) { KeyId = id });
                }
                if (configuration.SigningKeys.Count == 0) throw new OidcProviderException(false);
                lock (sync)
                {
                    cached = configuration;
                    loadedAt = clock.GetTimestamp();
                    refreshAt = loadedAt;
                    refreshCooldown = false;
                }
                return configuration;
            }
            finally { gate.Release(); }
        }

        public void RequestRefresh()
        {
            lock (sync)
            {
                if (cached is null || clock.GetElapsedTime(refreshAt) < TimeSpan.FromMinutes(1))
                    return;
                refreshAt = clock.GetTimestamp();
                refresh = true;
                refreshCooldown = true;
            }
        }

        internal bool IsCurrentKey(SecurityKey key)
        {
            lock (sync)
                return cached is not null && !refresh && clock.GetElapsedTime(loadedAt) < TimeSpan.FromMinutes(15) &&
                    cached.SigningKeys.Contains(key);
        }

        private static string Text(JsonElement root, string name) =>
            root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value) &&
            value.ValueKind == JsonValueKind.String && value.GetString() is { } text
                ? text : throw new OidcProviderException(false);

        private static bool Contains(JsonElement root, string name, string expected) =>
            root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array &&
            value.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.String && item.GetString() == expected);
    }
}
