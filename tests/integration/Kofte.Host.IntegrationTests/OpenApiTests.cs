using System.Net;
using System.Text.Json.Nodes;
using Kofte.Host.Bootstrap;
using Xunit;

namespace Kofte.Host.IntegrationTests;

[Trait("Provider", "sqlite")]
public sealed class OpenApiTests
{
    [Fact]
    public async Task GeneratedDevelopmentDocumentMatchesTheCompleteBaseline()
    {
        var token = TestContext.Current.CancellationToken;
        using var root = new TempRoot();
        await new SqlitePlatformStore(root.Path).InitializeAsync();
        await using var host = await StartupFixture.StartAsync(root.Path, token);
        using var response = await host.Client.GetAsync("/openapi/v1.json", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
        var actual = JsonNode.Parse(await response.Content.ReadAsStringAsync(token)) ??
            throw new InvalidOperationException("test_openapi_document_missing");
        var expected = JsonNode.Parse(await File.ReadAllTextAsync(Path.Combine(
            PostgresService.FindRepository(), "packages", "bootstrap-contract", "openapi.json"), token)) ??
            throw new InvalidOperationException("test_openapi_baseline_missing");
        Assert.Equal(1, actual["servers"]?.AsArray().Count);
        Assert.Equal(host.Client.BaseAddress?.GetLeftPart(UriPartial.Authority),
            actual["servers"]?[0]?["url"]?.GetValue<string>());
        actual["servers"] = expected["servers"]?.DeepClone();
        actual["security"] ??= new JsonArray();
        Assert.True(JsonNode.DeepEquals(expected, actual), actual.ToJsonString());
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task EveryDocumentationMethodIsAbsentOutsideDevelopment(string environment)
    {
        var token = TestContext.Current.CancellationToken;
        using var root = new TempRoot();
        await new SqlitePlatformStore(root.Path).InitializeAsync();
        await using var host = await StartupFixture.StartAsync(root.Path, token, environment);
        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Head })
        {
            using var request = new HttpRequestMessage(method, "/openapi/v1.json");
            using var response = await host.Client.SendAsync(request, token);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsStringAsync(token));
        }
    }
}

[Trait("Provider", "postgres")]
public sealed class PostgresOpenApiTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public async Task EveryDocumentationMethodIsAbsentOutsideDevelopment(string environment)
    {
        var token = TestContext.Current.CancellationToken;
        await using var database = await PostgresFixture.CreateAsync(
            Environment.GetEnvironmentVariable("KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE") == "true", token: token);
        using var root = new TempRoot();
        await using var host = await StartupFixture.StartAsync(root.Path, token, environment, database.ConnectionString);
        foreach (var method in new[] { HttpMethod.Get, HttpMethod.Post, HttpMethod.Head })
        {
            using var request = new HttpRequestMessage(method, "/openapi/v1.json");
            using var response = await host.Client.SendAsync(request, token);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Empty(await response.Content.ReadAsStringAsync(token));
        }
        Assert.False(Directory.Exists(root.Path));
    }
}
