using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Kofte.Host.Features.Identity;
using Kofte.Host.Features.Identity.Login;
using Kofte.TestSupport.Identity;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

[Trait("Provider", "sqlite")]
public sealed class OidcBackchannelTests
{
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task IssuerPreparesSigningMaterialBeforeTheFirstBudgetedDiscovery()
    {
        using var signing = new RequestGuardRsa();
        using var foreign = new RequestGuardRsa();
        await using var issuer = await OidcIssuer.CreateAsync(Token, signing, foreign);
        signing.RequestsStarted = foreign.RequestsStarted = true;
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        using var backchannel = new OidcBackchannel(Provider(issuer), accessor, issuer.CreateSocketsHandler());
        var options = new OpenIdConnectOptions();
        OidcConfiguration.Apply(options, Provider(issuer), new("https://kofte.identity.test"), backchannel);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(Token);
        deadline.CancelAfter(TimeSpan.FromSeconds(1));
        accessor.HttpContext.RequestAborted = deadline.Token;
        var configuration = await options.ConfigurationManager!.GetConfigurationAsync(deadline.Token);
        Assert.Single(configuration.SigningKeys);
        Assert.Equal(new[] { "discovery", "jwks" }, issuer.Observations);
        Assert.Equal(1, signing.Exports);
        Assert.Equal(1, foreign.Exports);
    }

    private sealed class RequestGuardRsa : RSA
    {
        private readonly RSA key = RSA.Create(2048);
        internal bool RequestsStarted { get; set; }
        internal int Exports { get; private set; }
        public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            if (RequestsStarted)
                throw new InvalidOperationException("identity_fixture_key_material_not_prepared");
            Exports++;
            return key.ExportParameters(includePrivateParameters);
        }
        public override void ImportParameters(RSAParameters parameters) => key.ImportParameters(parameters);
        public override byte[] Encrypt(byte[] data, RSAEncryptionPadding padding) => key.Encrypt(data, padding);
        public override byte[] Decrypt(byte[] data, RSAEncryptionPadding padding) => key.Decrypt(data, padding);
        public override byte[] SignHash(byte[] hash, HashAlgorithmName algorithm, RSASignaturePadding padding) =>
            key.SignHash(hash, algorithm, padding);
        public override bool VerifyHash(byte[] hash, byte[] signature, HashAlgorithmName algorithm,
            RSASignaturePadding padding) => key.VerifyHash(hash, signature, algorithm, padding);
        protected override void Dispose(bool disposing)
        {
            if (disposing) key.Dispose();
            base.Dispose(disposing);
        }
    }

    [Theory]
    [InlineData("untrusted-tls")]
    [InlineData("denied-network")]
    [InlineData("denied-endpoint")]
    public async Task ActualTlsEndpointAndConnectedNetworkChecksCannotBeBypassed(string fault)
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        var provider = Provider(issuer, fault);
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        using var backchannel = new OidcBackchannel(provider, accessor,
            fault == "untrusted-tls" ? null : issuer.CreateSocketsHandler());
        await Assert.ThrowsAsync<OidcProviderException>(() =>
            backchannel.Client.GetByteArrayAsync(provider.MetadataAddress, Token));
        Assert.Empty(issuer.Observations);
    }

    [Theory]
    [InlineData("discovery", 262_144, true)]
    [InlineData("discovery", 262_145, false)]
    [InlineData("jwks", 262_144, true)]
    [InlineData("jwks", 262_145, false)]
    [InlineData("token", 65_536, true)]
    [InlineData("token", 65_537, false)]
    [InlineData("id-token", 16_384, true)]
    [InlineData("id-token", 16_385, false)]
    public async Task WireBytesAndIndividualTokenUseIndependentExactLimits(string operation, int bytes, bool admitted)
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        var provider = Provider(issuer);
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        using var backchannel = new OidcBackchannel(provider, accessor, issuer.CreateSocketsHandler());
        var payload = operation == "id-token" ? "{\"id_token\":\"" + new string('a', bytes) + "\"}" :
            operation == "token" ? "{\"id_token\":\"a\"}".PadRight(bytes) : "{}".PadRight(bytes);
        issuer.ResponseOverride = (_, _) => Results.Text(payload, "application/json", Encoding.UTF8);
        async Task<byte[]> SendAsync()
        {
            using var request = new HttpRequestMessage(operation is "token" or "id-token" ? HttpMethod.Post : HttpMethod.Get,
                operation is "token" or "id-token" ? provider.TokenEndpoint :
                    operation == "jwks" ? provider.JwksUri : provider.MetadataAddress);
            if (request.Method == HttpMethod.Post) request.Content = new FormUrlEncodedContent([]);
            using var response = await backchannel.Client.SendAsync(request, Token);
            return await response.Content.ReadAsByteArrayAsync(Token);
        }
        if (admitted) Assert.Equal(Encoding.UTF8.GetBytes(payload), await SendAsync());
        else await Assert.ThrowsAsync<OidcProviderException>(SendAsync);
        Assert.Single(issuer.Observations);
    }

    [Theory]
    [InlineData("redirect")]
    [InlineData("compressed")]
    [InlineData("duplicate-json")]
    [InlineData("malformed-json")]
    [InlineData("invalid-utf8")]
    public async Task RedirectCompressedAndAmbiguousPayloadsNeverReachTheProtocolParser(string fault)
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        var provider = Provider(issuer);
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        using var backchannel = new OidcBackchannel(provider, accessor, issuer.CreateSocketsHandler());
        issuer.ResponseOverride = (_, context) =>
        {
            if (fault == "redirect") return Results.Redirect(provider.MetadataAddress.AbsoluteUri);
            if (fault == "compressed") context.Response.Headers.ContentEncoding = "gzip";
            return fault switch
            {
                "duplicate-json" => Results.Text("{\"issuer\":\"first\",\"issuer\":\"second\"}", "application/json"),
                "malformed-json" => Results.Text("{malformed", "application/json"),
                "invalid-utf8" => Results.Bytes([123, 34, 120, 34, 58, 34, 255, 34, 125], "application/json"),
                _ => Results.Text("{}", "application/json")
            };
        };
        await Assert.ThrowsAsync<OidcProviderException>(() =>
            backchannel.Client.GetByteArrayAsync(provider.MetadataAddress, Token));
        Assert.Single(issuer.Observations);
    }

    [Theory]
    [InlineData(262_144, true)]
    [InlineData(262_145, false)]
    public async Task UnannouncedStreamedBodyCannotEscapeTheWireLimit(int bytes, bool admitted)
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        var provider = Provider(issuer);
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        using var backchannel = new OidcBackchannel(provider, accessor, issuer.CreateSocketsHandler());
        var payload = Encoding.UTF8.GetBytes("{}".PadRight(bytes));
        issuer.ResponseOverride = (_, _) => Results.Stream(async stream =>
        {
            await stream.WriteAsync(payload, Token);
        }, "application/json");
        if (admitted)
            Assert.Equal(payload, await backchannel.Client.GetByteArrayAsync(provider.MetadataAddress, Token));
        else
            await Assert.ThrowsAsync<OidcProviderException>(() =>
                backchannel.Client.GetByteArrayAsync(provider.MetadataAddress, Token));
        Assert.Single(issuer.Observations);
    }

    [Fact]
    public async Task FourthRequestCannotLeaveTheHostAndANewRequestGetsItsOwnEnvelope()
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        var provider = Provider(issuer);
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        using var backchannel = new OidcBackchannel(provider, accessor, issuer.CreateSocketsHandler());
        for (var index = 0; index < 3; index++)
            await backchannel.Client.GetByteArrayAsync(provider.MetadataAddress, Token);
        await Assert.ThrowsAsync<OidcProviderException>(() =>
            backchannel.Client.GetByteArrayAsync(provider.MetadataAddress, Token));
        Assert.Equal(3, issuer.Observations.Count);
        accessor.HttpContext = new DefaultHttpContext();
        await backchannel.Client.GetByteArrayAsync(provider.MetadataAddress, Token);
        Assert.Equal(4, issuer.Observations.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualNetworkDeadlineIsTwoSecondsPerRequestAndThreeSecondsCombined(bool combined)
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        var provider = Provider(issuer);
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        using var backchannel = new OidcBackchannel(provider, accessor, issuer.CreateSocketsHandler());
        issuer.BeforeResponse = (_, token) => Task.Delay(combined ? 1100 : 2200, token);
        var timer = Stopwatch.StartNew();
        if (combined)
            for (var index = 0; index < 2; index++)
                await backchannel.Client.GetByteArrayAsync(provider.MetadataAddress, Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            backchannel.Client.GetByteArrayAsync(provider.MetadataAddress, Token));
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(4));
        Assert.Equal(combined ? 3 : 1, issuer.Observations.Count);
    }

    [Fact]
    public async Task CancellationIsInheritedBeforeAnyProviderCall()
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        var provider = Provider(issuer);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var accessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext { RequestAborted = cancelled.Token }
        };
        using var backchannel = new OidcBackchannel(provider, accessor, issuer.CreateSocketsHandler());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            backchannel.Client.GetByteArrayAsync(provider.MetadataAddress, Token));
        Assert.Empty(issuer.Observations);
    }

    [Fact]
    public async Task CacheAndKeyRefreshUseExactControlledBoundariesAndNeverKeepExpiredKeysOnFailure()
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        var provider = Provider(issuer);
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        using var backchannel = new OidcBackchannel(provider, accessor, issuer.CreateSocketsHandler());
        var clock = new CacheClock();
        var options = new OpenIdConnectOptions();
        OidcConfiguration.Apply(options, provider, new("https://kofte.identity.test"), backchannel, clock);
        var manager = options.ConfigurationManager!;
        var original = await manager.GetConfigurationAsync(Token);
        Assert.Equal(2, issuer.Observations.Count);
        clock.Advance(TimeSpan.FromMilliseconds(59_999));
        manager.RequestRefresh();
        Assert.Same(original, await manager.GetConfigurationAsync(Token));
        Assert.Equal(2, issuer.Observations.Count);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        manager.RequestRefresh();
        Assert.False(options.TokenValidationParameters.IssuerSigningKeyValidator(original.SigningKeys.Single(), null!,
            options.TokenValidationParameters));
        accessor.HttpContext = new DefaultHttpContext();
        var refreshed = await manager.GetConfigurationAsync(Token);
        Assert.NotSame(original, refreshed);
        Assert.Equal(4, issuer.Observations.Count);
        manager.RequestRefresh();
        Assert.Same(refreshed, await manager.GetConfigurationAsync(Token));
        clock.Advance(TimeSpan.FromMinutes(15));
        Assert.False(options.TokenValidationParameters.IssuerSigningKeyValidator(refreshed.SigningKeys.Single(), null!,
            options.TokenValidationParameters));
        issuer.ResponseOverride = (operation, _) => operation == "discovery" ? Results.StatusCode(503) : null;
        accessor.HttpContext = new DefaultHttpContext();
        await Assert.ThrowsAsync<OidcProviderException>(() => manager.GetConfigurationAsync(Token));
        issuer.ResponseOverride = null;
        accessor.HttpContext = new DefaultHttpContext();
        var corrected = await manager.GetConfigurationAsync(Token);
        Assert.NotSame(refreshed, corrected);
        Assert.True(options.TokenValidationParameters.IssuerSigningKeyValidator(corrected.SigningKeys.Single(), null!,
            options.TokenValidationParameters));
    }

    [Fact]
    public async Task FailedUnknownKeyRefreshCannotTurnIntoUnboundedColdDiscoveryRetries()
    {
        await using var issuer = await OidcIssuer.CreateAsync(Token);
        var provider = Provider(issuer);
        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        using var backchannel = new OidcBackchannel(provider, accessor, issuer.CreateSocketsHandler());
        var clock = new CacheClock();
        var options = new OpenIdConnectOptions();
        OidcConfiguration.Apply(options, provider, new("https://kofte.identity.test"), backchannel, clock);
        var manager = options.ConfigurationManager!;
        await manager.GetConfigurationAsync(Token);
        clock.Advance(TimeSpan.FromMinutes(1));
        manager.RequestRefresh();
        issuer.PublishSigningKey = false;
        accessor.HttpContext = new DefaultHttpContext();
        await Assert.ThrowsAsync<OidcProviderException>(() => manager.GetConfigurationAsync(Token));
        Assert.Equal(4, issuer.Observations.Count);
        issuer.PublishSigningKey = true;
        accessor.HttpContext = new DefaultHttpContext();
        await Assert.ThrowsAsync<OidcProviderException>(() => manager.GetConfigurationAsync(Token));
        clock.Advance(TimeSpan.FromMilliseconds(59_999));
        accessor.HttpContext = new DefaultHttpContext();
        await Assert.ThrowsAsync<OidcProviderException>(() => manager.GetConfigurationAsync(Token));
        Assert.Equal(4, issuer.Observations.Count);
        clock.Advance(TimeSpan.FromMilliseconds(1));
        accessor.HttpContext = new DefaultHttpContext();
        await manager.GetConfigurationAsync(Token);
        Assert.Equal(6, issuer.Observations.Count);
    }

    private static IdentityProviderConfiguration Provider(OidcIssuer issuer, string? fault = null) =>
        new(issuer.Issuer.AbsoluteUri, new(issuer.Issuer, ".well-known/openid-configuration"),
            new(issuer.Issuer, "authorize"), new(issuer.Issuer, "token"), new(issuer.Issuer, "jwks"),
            issuer.ClientId, "/test-owned-unused-reference", issuer.ClientSecret,
            (fault == "denied-endpoint" ? new[] { "jwks" } :
                new[] { ".well-known/openid-configuration", "authorize", "token", "jwks" })
                .Select(path => new Uri(issuer.Issuer, path)).ToArray(),
            [System.Net.IPNetwork.Parse(fault == "denied-network" ? "192.0.2.0/24" : "127.0.0.0/8")]);

    private sealed class CacheClock : TimeProvider
    {
        private long ticks;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => ticks;
        internal void Advance(TimeSpan duration) => ticks = checked(ticks + duration.Ticks);
    }
}
