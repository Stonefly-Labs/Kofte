using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.StorageMaintenance;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;
using StoreLease = Kofte.Infrastructure.Storage.StoreLease;

namespace Kofte.Storage.IntegrationTests.Recovery;

[CollectionDefinition("maintenance_deadlines", DisableParallelization = true)]
public sealed class MaintenanceDeadlineCollection;

[Trait("Provider", "sqlite")]
[Trait("Suite", "recovery")]
[Collection("maintenance_deadlines")]
public sealed class OperationDeadlineTests
{
    [Theory]
    [InlineData("upgrade", "registry_before_commit", 30)]
    [InlineData("migrate", "migration_sql_executing", 120)]
    public async Task ActualThirtyAndHundredTwentySecondDeadlinesKillReapAndReleaseSynchronousWorkers(
        string operation, string phase, int seconds)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync("sqlite", token, upgrade: operation != "upgrade");
        var owner = RegistryFixture.Module("org.example.deadline");
        if (operation == "migrate")
        {
            await using var scope = await fixture.Maintenance.AcquireAsync(token);
            Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(owner, scope, token));
        }
        var start = StorageCrashWorker.Start(Assembly.GetExecutingAssembly(), "sqlite", operation, phase,
            owner.Id.Value, child => ProvisioningCrashTests.Configure(fixture, child));
        start.Environment["KOFTE_STORAGE_WORKER_HOLD"] = "true";
        var admission = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        start.Environment[StorageMaintenanceProcess.AdmissionKey] = admission;
        var observed = false;
        var started = Stopwatch.GetTimestamp();
        var error = await Assert.ThrowsAsync<StorageException>(() => StorageMaintenanceProcess.RunOwnedAsync(
            start, admission, TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(2), token,
            async (reader, cancellation) =>
            {
                using var value = JsonDocument.Parse(await reader.ReadLineAsync(cancellation) ??
                    throw new InvalidOperationException("storage_worker_phase_missing"));
                Assert.Equal(phase, value.RootElement.GetProperty("phase").GetString());
                observed = true;
                using var probe = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                Assert.Equal(StorageCode.Busy, (await Assert.ThrowsAsync<StorageException>(() =>
                    fixture.Maintenance.AcquireAsync(probe.Token))).Code);
                while (await reader.ReadLineAsync(cancellation) is not null) { }
            }));
        var elapsed = Stopwatch.GetElapsedTime(started);
        Assert.True(observed);
        Assert.Equal(StorageCode.DeadlineExceeded, error.Code);
        Assert.True(elapsed >= TimeSpan.FromSeconds(seconds - 2));
        Assert.True(elapsed < TimeSpan.FromSeconds(seconds));
        await using var released = await fixture.Maintenance.AcquireAsync(token);
        if (operation == "upgrade") await fixture.Maintenance.UpgradeAsync(released, token);
        else Assert.Equal(StorageOutcome.ReconciliationRequired, (await fixture.Registry.ResolveAsync(owner.Id, token)).Outcome);
    }

    [Fact]
    public async Task CancelledOrRefusedWorkerPreservesV0AndReleasesItsLease()
    {
        using var root = new OwnedTestRoot();
        var store = new SqlitePlatformStore(root.Path);
        await store.InitializeAsync();
        var config = StorageMaintenanceConfiguration.Parse(new Dictionary<string, string?>
        { ["KOFTE_DATA_ROOT"] = root.Path }, Directory.GetCurrentDirectory());
        using var connection = store.Open();
        using var transaction = connection.CreateCommand();
        transaction.CommandText = "BEGIN EXCLUSIVE";
        transaction.ExecuteNonQuery();
        using var cancellation = new CancellationTokenSource();
        var started = Stopwatch.GetTimestamp();
        var worker = MaintenanceSupervisor.RunUpgradeAsync(config, cancellation.Token);
        var observedLease = false;
        using var observation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            while (!worker.IsCompleted)
            {
                try { using var probe = StoreLease.Acquire(root.Path); }
                catch (StorePathException error) when (error.Code == StorePathError.StoreBusy)
                { observedLease = true; break; }
                await Task.Delay(10, observation.Token);
            }
            if (observedLease) cancellation.Cancel();
            try
            {
                await worker;
                Assert.Fail("A blocked upgrade must not succeed.");
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
            catch (BootstrapException error)
            {
                Assert.Contains(error.Code,
                    new[] { BootstrapError.StoreBusy, BootstrapError.StorageInvalid, BootstrapError.StorageUnavailable });
            }
        }
        finally
        {
            cancellation.Cancel();
            if (!observedLease)
            {
                try { await worker; }
                catch (Exception error) when (error is OperationCanceledException or BootstrapException) { }
            }
        }
        Assert.True(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(5));
        using (StoreLease.Acquire(root.Path)) { }
        transaction.CommandText = "ROLLBACK";
        transaction.ExecuteNonQuery();
        using var verify = connection.CreateCommand();
        verify.CommandText = "PRAGMA user_version";
        Assert.Equal(0L, verify.ExecuteScalar());
        verify.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table'";
        Assert.Equal(0L, verify.ExecuteScalar());
        await store.ProbeAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task LiveInstallationLeaseProducesBoundedBusyAndNeverStartsASecondWriter()
    {
        using var root = new OwnedTestRoot();
        var store = new SqlitePlatformStore(root.Path);
        await store.InitializeAsync();
        var config = StorageMaintenanceConfiguration.Parse(new Dictionary<string, string?>
        { ["KOFTE_DATA_ROOT"] = root.Path }, Directory.GetCurrentDirectory());
        using var lease = StoreLease.Acquire(root.Path);
        var before = await File.ReadAllBytesAsync(store.DatabasePath, TestContext.Current.CancellationToken);
        var started = Stopwatch.GetTimestamp();
        Assert.Equal(BootstrapError.StoreBusy, (await Assert.ThrowsAsync<BootstrapException>(() =>
            MaintenanceSupervisor.RunUpgradeAsync(config, TestContext.Current.CancellationToken))).Code);
        Assert.True(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(4));
        Assert.Equal(before, await File.ReadAllBytesAsync(store.DatabasePath, TestContext.Current.CancellationToken));
    }
}
