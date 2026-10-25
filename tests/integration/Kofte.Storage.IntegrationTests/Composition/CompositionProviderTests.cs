using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Composition;

public abstract class CompositionProviderTests(ITestOutputHelper output)
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task NormalStopWithAnOpenIncompleteHttpRequestPreservesInventoryAndVerifiesExit()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync(Provider, token);
        await fixture.WriteSelectionAsync("""{"schemaVersion":"1","modules":[]}"""u8.ToArray(), token);
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        await using var process = fixture.StartBoot(CompositionTestCatalog.EmptyBoot);
        using var http = new HttpClient { BaseAddress = fixture.HostUrl };
        using var ready = CancellationTokenSource.CreateLinkedTokenSource(token);
        ready.CancelAfter(TimeSpan.FromSeconds(15));
        while (true)
        {
            ready.Token.ThrowIfCancellationRequested();
            Assert.False(process.HasExited);
            try
            {
                using var response = await http.GetAsync("/health/ready", ready.Token);
                if (response.StatusCode == HttpStatusCode.OK) break;
            }
            catch (HttpRequestException) { }
            await Task.Delay(15, ready.Token);
        }
        using var request = new TcpClient();
        await request.ConnectAsync(fixture.HostUrl.Host, fixture.HostUrl.Port, token);
        await request.GetStream().WriteAsync("GET /health/live HTTP/1.1\r\nHost: localhost\r\n"u8.ToArray(), token);
        var started = Stopwatch.GetTimestamp();
        await process.StopAsync();
        var result = await process.ResultAsync();
        output.WriteLine(result.Stdout);
        Assert.Equal(0, result.ExitCode);
        Assert.True(process.ExitVerified && process.DescendantExitsVerified);
        Assert.True(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(15));
        Assert.Empty(result.Stderr);
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
    }

    [Theory]
    [InlineData("empty-boot")]
    [InlineData("valid-dependency")]
    [InlineData("disabled-missing")]
    public async Task OwnedProviderBootPreservesInventoryAndExecutesOnlyItsIntendedPublicModules(string scenario)
    {
        var execution = await CompositionConsumerHarness.InvokeAsync(
            ["--provider", Provider, "--scenario", scenario], TestContext.Current.CancellationToken);
        output.WriteLine(execution.Stdout);
        output.WriteLine(execution.Stderr);
        var result = CompositionConsumerHarness.RequireSuccess(execution, Provider, scenario);
        foreach (var (name, minimum) in CompositionScenarioCatalog.RequiredObservations(scenario))
            Assert.True(result.Observations[scenario][name] >= minimum);
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteCompositionProviderTests(ITestOutputHelper output) : CompositionProviderTests(output)
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresCompositionProviderTests(ITestOutputHelper output) : CompositionProviderTests(output)
{
    protected override string Provider => "postgres";
}
