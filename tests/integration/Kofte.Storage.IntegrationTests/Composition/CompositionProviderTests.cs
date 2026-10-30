using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Kofte.TestSupport;
using Kofte.TestSupport.Identity;

namespace Kofte.Storage.IntegrationTests.Composition;

public abstract class CompositionProviderTests(ITestOutputHelper output)
{
    protected abstract string Provider { get; }

    [Theory]
    [InlineData(CompositionTestCatalog.DisabledPrerequisitesAtLimit, 16, false)]
    [InlineData(CompositionTestCatalog.DisabledPrerequisitesOverLimit, 17, false)]
    [InlineData(CompositionTestCatalog.DisabledPrerequisitesWithEnabled, 17, true)]
    [InlineData(CompositionTestCatalog.DisabledPrerequisitesMaximum, 32, false)]
    [InlineData(CompositionTestCatalog.DisabledPrerequisitesMaximumWithEnabled, 31, true)]
    public async Task DisabledCatalogPrerequisiteMetadataPreservesReadinessInventoryAndCleanExit(
        CompositionTestCatalog catalog, int disabled, bool enabled)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync(Provider, token);
        await fixture.SeedUnrelatedNoStorageAsync(token);
        await fixture.PrepareIdentityAsync(token);
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        var owners = Enumerable.Range(0, disabled).Select(index => $"org.example.disabled{index}").ToArray();
        foreach (var explicitFalse in new[] { false, true })
        {
            var selected = owners.Where(_ => explicitFalse).Select(owner => (Owner: owner, Enabled: false)).ToList();
            if (enabled) selected.Add(("org.example.no-storage", true));
            await fixture.WriteSelectionAsync(JsonSerializer.SerializeToUtf8Bytes(new
            {
                schemaVersion = "1",
                modules = selected.Select(selection =>
                    new { id = selection.Owner, enabled = selection.Enabled, settings = new { seed = 13 } })
            }), token);
            await using (var validation = fixture.Start("validate", catalog))
            {
                var result = await validation.WaitAsync(token);
                output.WriteLine(result.Stdout);
                Assert.Equal(0, result.ExitCode);
                Assert.True(validation.ExitVerified && validation.DescendantExitsVerified);
                Assert.Empty(result.Stderr);
                var records = Records(result.Stdout);
                Assert.Contains(records, record => record.GetProperty("stage").GetString() == "validated" &&
                    record.GetProperty("outcome").GetString() == "passed");
                Assert.DoesNotContain(records, record => record.GetProperty("stage").GetString() is
                    "registered" or "initialized" or "migrated" or "started" or "published");
            }
            Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
            await using var process = fixture.StartBoot(catalog, observeAuthor: enabled);
            using var http = new HttpClient { BaseAddress = fixture.HostUrl };
            using var ready = CancellationTokenSource.CreateLinkedTokenSource(token);
            ready.CancelAfter(TimeSpan.FromSeconds(15));
            while (true)
            {
                ready.Token.ThrowIfCancellationRequested();
                Assert.False(process.HasExited, process.OutputSnapshot);
                try
                {
                    using var response = await http.GetAsync("/health/ready", ready.Token);
                    if (response.StatusCode == HttpStatusCode.OK) break;
                }
                catch (HttpRequestException) { }
                await Task.Delay(15, ready.Token);
            }
            await process.StopAsync();
            var stopped = await process.ResultAsync();
            output.WriteLine(stopped.Stdout);
            Assert.Equal(0, stopped.ExitCode);
            Assert.True(process.ExitVerified && process.DescendantExitsVerified);
            Assert.True(IdentityDiagnosticOutput.IsSafeServingOutput(stopped.Stderr));
            var host = Records(stopped.Stdout);
            Assert.Contains(host, record => record.GetProperty("stage").GetString() == "published" &&
                record.GetProperty("code").GetString() == "composition_ready");
            foreach (var owner in owners)
            {
                var own = host.Where(record => record.GetProperty("owner").GetString() == owner).ToArray();
                Assert.NotEmpty(own);
                Assert.All(own, record => Assert.Equal("discovered", record.GetProperty("stage").GetString()));
                Assert.Single(own, record => record.GetProperty("outcome").GetString() == "disabled" &&
                    record.GetProperty("code").GetString() == "composition_disabled");
            }
            Assert.Equal("cleanup", host[^1].GetProperty("stage").GetString());
            Assert.Equal("passed", host[^1].GetProperty("outcome").GetString());
            if (enabled)
            {
                await fixture.AuthorTrace.CompleteAsync(token);
                Assert.Contains(fixture.AuthorTrace.Records, record =>
                    record.Owner == "org.example.no-storage" && record.Phase == "compute" && record.Value == 8);
                Assert.DoesNotContain(fixture.AuthorTrace.Records, record => owners.Contains(record.Owner));
            }
            Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
        }

        static JsonElement[] Records(string text) =>
            text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line =>
            {
                using var document = JsonDocument.Parse(line);
                return document.RootElement.Clone();
            }).ToArray();
    }

    [Fact]
    public async Task NormalStopWithAnOpenIncompleteHttpRequestPreservesInventoryAndVerifiesExit()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync(Provider, token);
        await fixture.WriteSelectionAsync("""{"schemaVersion":"1","modules":[]}"""u8.ToArray(), token);
        await fixture.PrepareIdentityAsync(token);
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
        Assert.True(IdentityDiagnosticOutput.IsSafeServingOutput(result.Stderr));
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
