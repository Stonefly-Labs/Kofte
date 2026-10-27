using System.Net;
using System.Diagnostics;
using System.Text.Json;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.Bootstrap;
using Kofte.Host.Features.ModuleComposition;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Kofte.Platform.Modules.Manifest;
using Kofte.TestSupport.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Kofte.Host.IntegrationTests;

[Trait("Provider", "sqlite")]
public sealed class CompositionHealthEndpointTests
{
    [Fact]
    public async Task ConfiguredServingOutlivesMaintenanceCeilingAndStopsWithinItsOwnReserve()
    {
        var token = TestContext.Current.CancellationToken;
        if (OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        using var root = new TempRoot();
        using var configuration = new TempRoot();
        var store = new SqlitePlatformStore(root.Path);
        await store.InitializeAsync();
        Directory.CreateDirectory(configuration.Path, UnixStorePaths.DirectoryMode);
        var path = Path.Combine(configuration.Path, "modules.json");
        await using (var file = new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            UnixCreateMode = UnixStorePaths.FileMode
        }))
            await file.WriteAsync("""{"schemaVersion":"1","modules":[]}"""u8.ToArray(), token);
        using var identity = await StartupFixture.PrepareIdentityAsync(root.Path, token);
        var before = await File.ReadAllBytesAsync(store.DatabasePath, token);
        await using var host = await StartupFixture.StartAsync(root.Path, token, modulesFile: path,
            preparedIdentity: identity);
        var started = Stopwatch.GetTimestamp();
        await Task.Delay(TimeSpan.FromSeconds(121), token);
        using var ready = await host.Client.GetAsync("/health/ready", token);
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);
        Assert.True(Stopwatch.GetElapsedTime(started) > TimeSpan.FromSeconds(120));
        var stopping = Stopwatch.GetTimestamp();
        await host.StopAsync();
        Assert.True(Stopwatch.GetElapsedTime(stopping) < TimeSpan.FromSeconds(15));
        var output = await host.SafeOutputAsync();
        Assert.True(IdentityDiagnosticOutput.IsSafeServingOutput(output.Stderr));
        var records = output.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonSerializer.Deserialize<JsonElement>(line)).ToArray();
        Assert.Contains(records, record => record.GetProperty("stage").GetString() == "published");
        Assert.Equal("cleanup", records[^1].GetProperty("stage").GetString());
        Assert.Equal("passed", records[^1].GetProperty("outcome").GetString());
        Assert.Equal(before, await File.ReadAllBytesAsync(store.DatabasePath, token));
    }

    [Fact]
    public async Task AnonymousHealthUsesFreshAggregateAndStoreProbeWithoutAuthorCallbacksOrInventory()
    {
        var token = TestContext.Current.CancellationToken;
        var manifest = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            id = "org.private.module",
            version = "1.0.0",
            displayName = "Private",
            hostContract = "1.0.0",
            capabilities = Array.Empty<object>(),
            dependencies = Array.Empty<object>(),
            permissions = Array.Empty<object>(),
            storage = new { relational = false }
        });
        var callbacks = 0;
        var resolution = Assert.IsType<CompositionResolution.Accepted>(CompositionResolver.Resolve(
            [new(manifest, createRuntime: (_, _) => { callbacks++; throw new InvalidOperationException(); })],
            [new("org.private.module", true)], new("1.0.0"), token));
        var clock = new Clock();
        var health = new CompositionHealth(resolution.Composition, new string('a', 32), clock);
        var probe = new Probe();
        var builder = WebApplication.CreateSlimBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddSingleton<IPlatformStore>(probe);
        builder.Services.AddSingleton(health);
        builder.Services.AddSingleton<HealthHandler>();
        await using var app = builder.Build();
        app.MapBootstrapHealth();
        await app.StartAsync(token);
        using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
        await Check("/health/live", HttpStatusCode.OK, "live", "host");
        await Check("/health/ready", HttpStatusCode.ServiceUnavailable, "unavailable", "host");
        Assert.True(ModuleId.TryParse("org.private.module", out var owner));
        health.Observe(owner, new(ModuleHealth.Healthy));
        health.Publish();
        await Check("/health/ready", HttpStatusCode.OK, "ready", "storage");
        clock.Advance(30);
        await Check("/health/ready", HttpStatusCode.ServiceUnavailable, "unavailable", "host");
        probe.Fail = true;
        await Check("/health/ready", HttpStatusCode.ServiceUnavailable, "unavailable", "storage");
        await Check("/health/live", HttpStatusCode.OK, "live", "host");
        Assert.Equal(4, probe.Calls);
        Assert.Equal(0, callbacks);
        foreach (var path in new[] { "/modules", "/api/kofte/modules", "/health/modules" })
        {
            using var response = await client.GetAsync(path, token);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        await app.StopAsync(token);
        return;

        async Task Check(string path, HttpStatusCode status, string state, string component)
        {
            using var response = await client.GetAsync(path, token);
            Assert.Equal(status, response.StatusCode);
            Assert.Equal("no-store", response.Headers.CacheControl?.ToString());
            var json = await response.Content.ReadAsStringAsync(token);
            Assert.DoesNotContain("org.private", json);
            using var body = JsonDocument.Parse(json);
            Assert.Equal(["component", "correlationId", "state", "version"],
                body.RootElement.EnumerateObject().Select(p => p.Name).Order().ToArray());
            Assert.Equal(state, body.RootElement.GetProperty("state").GetString());
            Assert.Equal(component, body.RootElement.GetProperty("component").GetString());
        }
    }

    [Fact]
    public async Task ActualValidateCommandIsAdmittedReadOnlyAndDirectWorkersAreRejected()
    {
        var token = TestContext.Current.CancellationToken;
        using var root = new TempRoot();
        Assert.Equal(0, (await StartupFixture.RunExitAsync(root.Path, ["--store", "init"], token)).ExitCode);
        Assert.Equal(0, (await StartupFixture.RunExitAsync(root.Path, ["--registry", "upgrade"], token)).ExitCode);
        var store = new SqlitePlatformStore(root.Path);
        var before = await File.ReadAllBytesAsync(store.DatabasePath, token);
        var result = await StartupFixture.RunExitAsync(root.Path, ["--modules", "validate"], token);
        Assert.True(result.ExitCode == 0, result.Output);
        var records = result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonDocument.Parse(line)).ToArray();
        try
        {
            Assert.Contains(records, record => record.RootElement.GetProperty("stage").GetString() == "admission");
            Assert.Equal("passed", records[^1].RootElement.GetProperty("outcome").GetString());
            Assert.Equal("cleanup", records[^1].RootElement.GetProperty("stage").GetString());
            Assert.Single(records.Select(record => record.RootElement.GetProperty("attempt").GetString()).Distinct());
        }
        finally { foreach (var record in records) record.Dispose(); }
        Assert.Equal(before, await File.ReadAllBytesAsync(store.DatabasePath, token));
        var direct = await StartupFixture.RunExitAsync(root.Path, ["--modules-worker", "serve"], token);
        Assert.Equal(2, direct.ExitCode);
        Assert.Contains("composition_admission_invalid", direct.Output);
        Assert.Equal(2, (await StartupFixture.RunExitAsync(root.Path, ["--modules", "unknown"], token)).ExitCode);
    }

    private sealed class Probe : IPlatformStore
    {
        public bool Fail { get; set; }
        public int Calls { get; private set; }
        public Task ProbeAsync(CancellationToken token)
        {
            Calls++;
            if (Fail) throw new BootstrapException(BootstrapError.StorageUnavailable);
            return Task.CompletedTask;
        }
    }
    private sealed class Clock : TimeProvider
    {
        private long now;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => now;
        public void Advance(int seconds) => now += TimeSpan.FromSeconds(seconds).Ticks;
    }
}
