using System.Net;
using System.Text.Json;
using Kofte.Host.Bootstrap;
using Xunit;

namespace Kofte.Host.IntegrationTests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task RealHostHasFreshPrivateHealthAndCaseSensitiveMinimalSurface()
    {
        var token = TestContext.Current.CancellationToken;
        using var root = new TempRoot();
        var store = new SqlitePlatformStore(root.Path);
        await store.InitializeAsync();
        await using var host = await StartupFixture.StartAsync(root.Path, token);
        using var ready = await host.Client.GetAsync("/health/ready", token);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.Equal("no-store", ready.Headers.CacheControl!.ToString());
        using var document = JsonDocument.Parse(await ready.Content.ReadAsStringAsync(token));
        Assert.Equal(["component", "correlationId", "state", "version"],
            document.RootElement.EnumerateObject().Select(item => item.Name).Order().ToArray());
        Assert.Equal("ready", document.RootElement.GetProperty("state").GetString());
        Assert.Matches("^[0-9a-f]{32}$", document.RootElement.GetProperty("correlationId").GetString()!);
        foreach (var (method, path, expected) in new[]
        {
            (HttpMethod.Get, "/Health/Ready", HttpStatusCode.NotFound),
            (HttpMethod.Get, "/health/ready?secret=credential-canary", HttpStatusCode.BadRequest),
            (HttpMethod.Post, "/health/ready", HttpStatusCode.MethodNotAllowed),
            (HttpMethod.Head, "/health/ready", HttpStatusCode.MethodNotAllowed),
            (HttpMethod.Get, "/admin/credential-canary", HttpStatusCode.NotFound)
        })
        {
            using var request = new HttpRequestMessage(method, path);
            using var response = await host.Client.SendAsync(request, token);
            Assert.Equal(expected, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsStringAsync(token));
        }
        using var bodyRequest = new HttpRequestMessage(HttpMethod.Get, "/health/live")
        {
            Content = new StringContent("credential-canary")
        };
        using var rejected = await host.Client.SendAsync(bodyRequest, token);
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Empty(await rejected.Content.ReadAsStringAsync(token));
        var original = await File.ReadAllBytesAsync(store.DatabasePath, token);
        File.Delete(store.DatabasePath);
        using var unavailable = await host.Client.GetAsync("/health/ready", token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, unavailable.StatusCode);
        Assert.False(File.Exists(store.DatabasePath));
        using var live = await host.Client.GetAsync("/health/live", token);
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        await File.WriteAllBytesAsync(store.DatabasePath, original, token);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(store.DatabasePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        using var recovered = await host.Client.GetAsync("/health/ready", token);
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
    }
}
