using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

[Trait("Provider", "sqlite")]
public sealed class OidcIssuerTests
{
    [Fact]
    public async Task RealHttpsExchangeRequiresClientPkceAndSingleUseCodeAndSignsNonce()
    {
        var token = TestContext.Current.CancellationToken;
        await using var issuer = await OidcIssuer.CreateAsync(token);
        using var client = issuer.CreateClient();
        var callback = new Uri("https://127.0.0.1:4444/auth/oidc/callback");
        issuer.RegisteredRedirectUri = callback;
        using var discovery = await client.GetFromJsonAsync<JsonDocument>(
            "/.well-known/openid-configuration", token);
        Assert.Equal(issuer.Issuer.AbsoluteUri, discovery!.RootElement.GetProperty("issuer").GetString());
        const string verifier = "fixture-verifier-0123456789012345678901234567890123456789";
        var challenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        using var authorization = await client.GetAsync("/authorize?response_type=code&scope=openid" +
            $"&client_id={issuer.ClientId}&redirect_uri={Uri.EscapeDataString(callback.AbsoluteUri)}" +
            $"&code_challenge={challenge}&code_challenge_method=S256&nonce=fixture-nonce&state=fixture-state", token);
        Assert.Equal(HttpStatusCode.Found, authorization.StatusCode);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(
            authorization.Headers.Location!.Query);
        Assert.Equal("fixture-state", query["state"].ToString());
        var fields = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = query["code"].ToString(),
            ["client_id"] = issuer.ClientId,
            ["client_secret"] = issuer.ClientSecret,
            ["redirect_uri"] = callback.AbsoluteUri,
            ["code_verifier"] = verifier
        };
        using var exchanged = await client.PostAsync("/token", new FormUrlEncodedContent(fields), token);
        Assert.Equal(HttpStatusCode.OK, exchanged.StatusCode);
        using var response = JsonDocument.Parse(await exchanged.Content.ReadAsStringAsync(token));
        var jwt = response.RootElement.GetProperty("id_token").GetString()!.Split('.');
        using var claims = JsonDocument.Parse(Decode(jwt[1]));
        Assert.Equal("fixture-nonce", claims.RootElement.GetProperty("nonce").GetString());
        Assert.Equal("synthetic-subject", claims.RootElement.GetProperty("sub").GetString());
        using var keys = await client.GetFromJsonAsync<JsonDocument>("/jwks", token);
        var jwk = keys!.RootElement.GetProperty("keys")[0];
        using var rsa = RSA.Create(new RSAParameters
        {
            Modulus = Decode(jwk.GetProperty("n").GetString()!),
            Exponent = Decode(jwk.GetProperty("e").GetString()!)
        });
        Assert.True(rsa.VerifyData(Encoding.ASCII.GetBytes($"{jwt[0]}.{jwt[1]}"), Decode(jwt[2]),
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        using var replay = await client.PostAsync("/token", new FormUrlEncodedContent(fields), token);
        Assert.Equal(HttpStatusCode.BadRequest, replay.StatusCode);
        Assert.Equal(2, issuer.TokenRequests);
    }

    [Fact]
    public async Task WrongPkceOrClientNeverReceivesTokenAndPrivateCaIsNotGloballyTrusted()
    {
        var token = TestContext.Current.CancellationToken;
        await using var issuer = await OidcIssuer.CreateAsync(token);
        using var ordinary = new HttpClient();
        await Assert.ThrowsAsync<HttpRequestException>(() => ordinary.GetAsync(issuer.Issuer, token));
        using var client = issuer.CreateClient();
        issuer.RegisteredRedirectUri = new Uri("https://127.0.0.1:4444/auth/oidc/callback");
        const string verifier = "fixture-verifier-0123456789012345678901234567890123456789";
        var challenge = Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        foreach (var wrongClient in new[] { true, false })
        {
            using var authorization = await client.GetAsync("/authorize?response_type=code&scope=openid" +
                $"&client_id={issuer.ClientId}&redirect_uri={Uri.EscapeDataString(issuer.RegisteredRedirectUri.AbsoluteUri)}" +
                $"&code_challenge={challenge}&code_challenge_method=S256&nonce=fixture-nonce&state=fixture-state", token);
            Assert.Equal(HttpStatusCode.Found, authorization.StatusCode);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(
                authorization.Headers.Location!.Query);
            using var invalid = await client.PostAsync("/token", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = query["code"].ToString(),
                ["client_id"] = issuer.ClientId,
                ["client_secret"] = wrongClient ? "wrong" : issuer.ClientSecret,
                ["code_verifier"] = wrongClient ? verifier : new string('a', 43),
                ["redirect_uri"] = issuer.RegisteredRedirectUri.AbsoluteUri
            }), token);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
            Assert.DoesNotContain("id_token", await invalid.Content.ReadAsStringAsync(token));
        }
    }

    private static byte[] Decode(string value) =>
        Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') +
            new string('=', (4 - value.Length % 4) % 4));
}
