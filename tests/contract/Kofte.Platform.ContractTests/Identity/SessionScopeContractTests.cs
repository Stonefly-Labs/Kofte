using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Platform.ContractTests.Identity;

public abstract class SessionScopeContractTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActualEntryPathsKeepDirectAndAdaptedUsersRequestScopedAndRefuseThePreviousEpoch(bool composed)
    {
        await using var fixture = await IdentityFixture.CreateAsync(Provider, Token);
        fixture.WriteConfiguration(JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = "1",
            mode = "development",
            publicOrigin = fixture.WebUrl.GetLeftPart(UriPartial.Authority),
            keyRingDirectory = fixture.KeyRingDirectory,
            development = new
            {
                enabled = true,
                identities = new[] { new { key = "synthetic" }, new { key = "second" } }
            }
        }));
        foreach (var arguments in new[] { new[] { "--registry", "upgrade" }, ["--identity", "prepare"] })
        {
            await using var command = fixture.Start(arguments);
            Assert.Equal(0, (await command.WaitAsync(fixture.Storage.Token)).ExitCode);
        }
        var firstCookies = new CookieContainer();
        var secondCookies = new CookieContainer();
        using var first = new HttpClient(new HttpClientHandler
        { AllowAutoRedirect = false, CookieContainer = firstCookies })
        { BaseAddress = fixture.WebUrl };
        using var second = new HttpClient(new HttpClientHandler
        { AllowAutoRedirect = false, CookieContainer = secondCookies })
        { BaseAddress = fixture.WebUrl };
        Guid firstId, secondId;
        string[] previousCookies;
        await using (var host = fixture.Start([], composed: composed))
        await using (var web = fixture.StartDevelopmentWeb())
        {
            await ReadyAsync(first, host, web);
            firstId = await SelectAsync(fixture, first, "synthetic");
            using (var anonymous = await second.GetAsync("/api/identity/v1/me", Token))
                await AssertUnauthenticatedAsync(anonymous);
            secondId = await SelectAsync(fixture, second, "second");
            Assert.NotEqual(firstId, secondId);
            var concurrent = await Task.WhenAll(CurrentAsync(fixture, first), CurrentAsync(fixture, second));
            Assert.Equal(new[] { firstId, secondId }, concurrent);
            await fixture.RunDevelopmentContractsAsync(Token);
            using (var caller = new HttpClient { BaseAddress = fixture.WebUrl })
            using (var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me"))
            {
                request.Headers.Add("X-User-Id", firstId.ToString("D"));
                request.Headers.Add("X-Initial-Administrator", "true");
                using var response = await caller.SendAsync(request, Token);
                await AssertUnauthenticatedAsync(response);
            }
            previousCookies =
            [
                firstCookies.GetCookieHeader(fixture.WebUrl),
                secondCookies.GetCookieHeader(fixture.WebUrl)
            ];
            Assert.All(previousCookies, value => Assert.Contains("kofte-dev-session=", value, StringComparison.Ordinal));
            await web.StopAsync();
            await host.StopAsync();
        }
        await using (var command = fixture.Start(["--identity", "revoke-sessions"]))
            Assert.Equal(0, (await command.WaitAsync(fixture.Storage.Token)).ExitCode);
        await using (var host = fixture.Start([], composed: composed))
        await using (var web = fixture.StartDevelopmentWeb())
        {
            using var readiness = new HttpClient { BaseAddress = fixture.WebUrl };
            await ReadyAsync(readiness, host, web);
            using var replay = new HttpClient(new HttpClientHandler { UseCookies = false, AllowAutoRedirect = false });
            foreach (var cookie in previousCookies)
                foreach (var origin in new[] { fixture.HostUrl, fixture.WebUrl })
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(origin, "/api/identity/v1/me"));
                    request.Headers.Add("Cookie", cookie);
                    using var response = await replay.SendAsync(request, Token);
                    await AssertUnauthenticatedAsync(response);
                }
            Assert.Equal(firstId, await SelectAsync(fixture, first, "synthetic"));
            Assert.Equal(secondId, await SelectAsync(fixture, second, "second"));
            await web.StopAsync();
            await host.StopAsync();
        }
        Assert.True(fixture.AllExitsVerified);
    }

    private async Task ReadyAsync(HttpClient client, IdentityProcess host, IdentityProcess web)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(Token);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        while (true)
        {
            Assert.False(host.HasExited);
            Assert.False(web.HasExited);
            try
            {
                using var response = await client.GetAsync("/api/identity/v1/browser-context", deadline.Token);
                if (response.StatusCode == HttpStatusCode.OK) return;
            }
            catch (HttpRequestException) { }
            await Task.Delay(25, deadline.Token);
        }
    }

    private async Task<Guid> SelectAsync(IdentityFixture fixture, HttpClient client, string key)
    {
        using var browser = await client.GetAsync("/api/identity/v1/browser-context", Token);
        Assert.Equal(HttpStatusCode.OK, browser.StatusCode);
        using var context = JsonDocument.Parse(await browser.Content.ReadAsByteArrayAsync(Token));
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/development-session")
        { Content = JsonContent.Create(new { identityKey = key }) };
        request.Headers.Add("Origin", fixture.WebUrl.GetLeftPart(UriPartial.Authority));
        request.Headers.Add("X-Kofte-CSRF", context.RootElement.GetProperty("csrfToken").GetString());
        using var response = await client.SendAsync(request, Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
        var id = body.RootElement.GetProperty("userId").GetGuid();
        Assert.Equal(id, await CurrentAsync(fixture, client));
        return id;
    }

    private async Task<Guid> CurrentAsync(IdentityFixture fixture, HttpClient client)
    {
        Guid? id = null;
        foreach (var origin in new[] { fixture.HostUrl, fixture.WebUrl })
        {
            using var response = await client.GetAsync(new Uri(origin, "/api/identity/v1/me"), Token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
            using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
            Assert.Equal("1", body.RootElement.GetProperty("version").GetString());
            Assert.Equal("development", body.RootElement.GetProperty("identityMode").GetString());
            Assert.False(body.RootElement.GetProperty("isInitialAdministrator").GetBoolean());
            var actual = body.RootElement.GetProperty("userId").GetGuid();
            Assert.NotEqual(Guid.Empty, actual);
            if (id is { } previous) Assert.Equal(previous, actual);
            id = actual;
        }
        return id ?? throw new InvalidOperationException("identity_fixture_current_user_missing");
    }

    private async Task AssertUnauthenticatedAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        Assert.Null(response.Headers.Location);
        using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
        Assert.Equal("identity_unauthenticated", body.RootElement.GetProperty("code").GetString());
        Assert.False(body.RootElement.TryGetProperty("userId", out _));
    }
}

[Trait("Provider", "sqlite")]
[Collection("identity-deadlines")]
public sealed class SqliteSessionScopeContractTests : SessionScopeContractTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Collection("identity-deadlines")]
public sealed class PostgresSessionScopeContractTests : SessionScopeContractTests
{
    protected override string Provider => "postgres";
}
