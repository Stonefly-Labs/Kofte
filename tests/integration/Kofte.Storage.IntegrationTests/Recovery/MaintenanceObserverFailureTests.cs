using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Recovery;

[Trait("Provider", "sqlite"), Trait("Suite", "recovery")]
public sealed class MaintenanceObserverFailureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectedOwnedWorkerOutputFailsPromptlySanitizesAndReapsTheWriter(bool synchronous)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync("sqlite", token, upgrade: false);
        var start = StorageCrashWorker.Start(Assembly.GetExecutingAssembly(), "sqlite", "upgrade",
            "registry_before_commit", "org.example.output-failure",
            child => ProvisioningCrashTests.Configure(fixture, child));
        start.Environment["KOFTE_STORAGE_WORKER_HOLD"] = "true";
        var started = Stopwatch.GetTimestamp();
        var error = await Assert.ThrowsAsync<StorageException>(() => StorageMaintenanceProcess.RunAsync(
            start, token, (reader, cancellation) => synchronous
                ? throw new JsonException("/private/credential-canary")
                : RejectAsync(reader, cancellation)));
        Assert.Equal(StorageCode.Unavailable, error.Code);
        Assert.DoesNotContain("credential-canary", error.Message);
        Assert.True(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(5));
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        await fixture.Maintenance.UpgradeAsync(scope, token);
    }

    private static async Task RejectAsync(StreamReader reader, CancellationToken token)
    {
        Assert.NotNull(await reader.ReadLineAsync(token));
        throw new InvalidOperationException("/private/credential-canary");
    }
}
