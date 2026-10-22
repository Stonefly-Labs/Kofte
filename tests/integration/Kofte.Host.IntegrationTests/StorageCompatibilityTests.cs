using Kofte.Host.Bootstrap;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Host.Features.StorageMaintenance;
using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace Kofte.Host.IntegrationTests;

[Trait("Provider", "sqlite")]
public sealed class StorageCompatibilityTests
{
    [Fact]
    public async Task ModuleFreeV0AndExplicitlyUpgradedV1KeepHealthWithoutAutomaticWrites()
    {
        using var root = new TempRoot();
        var bootstrap = new SqlitePlatformStore(root.Path);
        await bootstrap.InitializeAsync();
        var before = await File.ReadAllBytesAsync(bootstrap.DatabasePath, TestContext.Current.CancellationToken);
        await using (var host = await StartupFixture.StartAsync(root.Path, TestContext.Current.CancellationToken))
        {
            using var ready = await host.Client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
            Assert.True(ready.IsSuccessStatusCode);
        }

        Assert.Equal(before, await File.ReadAllBytesAsync(bootstrap.DatabasePath, TestContext.Current.CancellationToken));
        var upgraded = await StartupFixture.RunExitAsync(root.Path, ["--registry", "upgrade"],
            TestContext.Current.CancellationToken);
        Assert.Equal(0, upgraded.ExitCode);
        Assert.Contains("registry_upgraded", upgraded.Output);
        using (var diagnostic = JsonDocument.Parse(upgraded.Output))
            Assert.Matches(@"\A[0-9a-f]{32}\z",
                diagnostic.RootElement.GetProperty("correlationId").GetString()!);
        var installation = StorageInstallation.Sqlite(root.Path);
        await StorageReconciliation.ValidateStartupAsync(installation, TestContext.Current.CancellationToken);
        before = await File.ReadAllBytesAsync(bootstrap.DatabasePath, TestContext.Current.CancellationToken);
        await using (var host = await StartupFixture.StartAsync(root.Path, TestContext.Current.CancellationToken))
        {
            using var ready = await host.Client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
            Assert.True(ready.IsSuccessStatusCode);
        }
        Assert.Equal(before, await File.ReadAllBytesAsync(bootstrap.DatabasePath, TestContext.Current.CancellationToken));
        Assert.Equal(0, (await StartupFixture.RunExitAsync(root.Path,
            ["--registry", "upgrade"], TestContext.Current.CancellationToken)).ExitCode);
    }

    [Fact]
    public async Task UnknownRegistrySyntaxAndUnsupervisedWorkerFailClosed()
    {
        using var root = new TempRoot();
        await new SqlitePlatformStore(root.Path).InitializeAsync();
        foreach (var args in new[] { new[] { "--registry", "register" }, ["--registry-worker", "upgrade"] })
        {
            var result = await StartupFixture.RunExitAsync(root.Path, args, TestContext.Current.CancellationToken);
            Assert.Equal(1, result.ExitCode);
            Assert.Contains("command_invalid", result.Output);
        }
    }

    [Fact]
    public async Task IncompatibleRegistryNeverStartsOrGetsAutomaticallyRepaired()
    {
        using var root = new TempRoot();
        var store = new SqlitePlatformStore(root.Path);
        await store.InitializeAsync();
        using (var connection = store.Open())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA user_version=9";
            command.ExecuteNonQuery();
        }
        var before = await File.ReadAllBytesAsync(store.DatabasePath, TestContext.Current.CancellationToken);
        var result = await StartupFixture.RunExitAsync(root.Path, [], TestContext.Current.CancellationToken);
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("storage_invalid", result.Output);
        Assert.Equal(before, await File.ReadAllBytesAsync(store.DatabasePath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MissingMaintenanceExecutableIsSanitized()
    {
        using var root = new TempRoot();
        var start = new ProcessStartInfo(Path.Combine(root.Path, "missing-worker-private-canary"))
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        var error = await Assert.ThrowsAsync<BootstrapException>(() =>
            MaintenanceSupervisor.RunOwnedAsync(start, "test", TimeSpan.FromSeconds(1),
                TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken));
        Assert.Equal(BootstrapError.StorageUnavailable, error.Code);
        Assert.DoesNotContain("canary", error.ToString());
        Assert.DoesNotContain(root.Path, error.ToString());
    }

    [Fact]
    public async Task CancellingAStartedOwnedProcessReapsItWithinTheCleanupReserve()
    {
        if (OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        using var root = new TempRoot();
        Directory.CreateDirectory(root.Path, Kofte.Host.Bootstrap.UnixStorePaths.DirectoryMode);
        var pidFile = Path.Combine(root.Path, "owned-worker.pid");
        var start = new ProcessStartInfo("/bin/sh")
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add(
            """IFS= read -r admission; printf '%s' $$ > "$1"; exec sleep 20""");
        start.ArgumentList.Add("maintenance-cancellation-test");
        start.ArgumentList.Add(pidFile);
        using var cancellation = new CancellationTokenSource();
        var worker = MaintenanceSupervisor.RunOwnedAsync(start, "test", TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(1), cancellation.Token);
        try
        {
            using var observation = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            while (!File.Exists(pidFile)) await Task.Delay(10, observation.Token);
            var started = Stopwatch.GetTimestamp();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => worker);
            Assert.True(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(2));
            var pid = int.Parse(await File.ReadAllTextAsync(pidFile, TestContext.Current.CancellationToken),
                System.Globalization.CultureInfo.InvariantCulture);
            try
            {
                using var process = Process.GetProcessById(pid);
                Assert.True(process.HasExited);
            }
            catch (ArgumentException) { }
        }
        finally
        {
            cancellation.Cancel();
            try { await worker; }
            catch (Exception error) when (error is OperationCanceledException or BootstrapException) { }
        }
    }

    [Fact]
    public async Task UnconfirmedPipeDrainHasFiniteCleanupAndCannotReportSuccess()
    {
        if (OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        using var root = new TempRoot();
        Directory.CreateDirectory(root.Path, Kofte.Host.Bootstrap.UnixStorePaths.DirectoryMode);
        var pidFile = Path.Combine(root.Path, "owned-pipe-holder.pid");
        var start = new ProcessStartInfo("/bin/sh")
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add(
            """IFS= read -r admission; sleep 20 & child=$!; printf '%s' "$child" > "$1"; exit 0""");
        start.ArgumentList.Add("maintenance-drain-test");
        start.ArgumentList.Add(pidFile);
        var started = Stopwatch.GetTimestamp();
        try
        {
            var error = await Assert.ThrowsAsync<BootstrapException>(() =>
                MaintenanceSupervisor.RunOwnedAsync(start, "test", TimeSpan.FromMilliseconds(750),
                    TimeSpan.FromMilliseconds(250), TestContext.Current.CancellationToken));
            Assert.Equal(BootstrapError.StorageUnavailable, error.Code);
            Assert.True(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(3));
        }
        finally
        {
            if (File.Exists(pidFile))
            {
                var pid = int.Parse(await File.ReadAllTextAsync(pidFile, TestContext.Current.CancellationToken),
                    System.Globalization.CultureInfo.InvariantCulture);
                using var owned = Process.GetProcessById(pid);
                if (!owned.HasExited) owned.Kill(entireProcessTree: true);
                using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await owned.WaitForExitAsync(cleanup.Token);
            }
        }
    }
}
