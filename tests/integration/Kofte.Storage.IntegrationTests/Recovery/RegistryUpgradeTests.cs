using System.Reflection;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Recovery;

public abstract class RegistryUpgradeTests
{
    protected abstract string Provider { get; }

    [Theory]
    [InlineData("registry_before_commit")]
    [InlineData("registry_committed")]
    public async Task KilledUpgradeIsAtomicAndLostAcknowledgementRetriesTheSameInstallation(string phase)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync(Provider, token, upgrade: false);
        var start = StorageCrashWorker.Start(Assembly.GetExecutingAssembly(), Provider, "upgrade", phase,
            "org.example.upgrade", child => ProvisioningCrashTests.Configure(fixture, child));
        await StorageCrashWorker.KillAtAsync(start, phase, token);
        using var budget = new OperationBudget(OperationBudget.Register, token);
        await using var platform = await fixture.Installation.OpenPlatformAsync(budget);
        var runner = new RegistryMigrationRunner(fixture.Installation);
        var version = Convert.ToInt32(await RegistryMigrationRunner.ScalarAsync(platform, Provider == "sqlite"
            ? "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='__kofte_installation'"
            : "SELECT count(*) FROM pg_catalog.pg_tables WHERE schemaname='kofte_system' AND tablename='__kofte_installation'", budget));
        Assert.Equal(phase == "registry_before_commit" ? 0 : 1, version);
        var before = version == 1 ? await runner.ReadInstallationAsync(platform, budget) : null;
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        await fixture.Maintenance.UpgradeAsync(scope, token);
        var after = await runner.ReadInstallationAsync(platform, budget);
        if (before is not null) Assert.Equal(before, after);
        await fixture.Maintenance.UpgradeAsync(scope, token);
        Assert.Equal(after, await runner.ReadInstallationAsync(platform, budget));
        Assert.IsType<StorageResult.NoStorage>(await fixture.Registry.RegisterAsync(
            RegistryFixture.Module("org.example.stateless", relational: false), scope, token));
        await fixture.AssertInventoryAsync(1, 0, token);
    }
}

[Trait("Provider", "sqlite"), Trait("Suite", "recovery")]
public sealed class SqliteRegistryUpgradeTests : RegistryUpgradeTests
{ protected override string Provider => "sqlite"; }
[Trait("Provider", "postgres"), Trait("Suite", "recovery")]
public sealed class PostgresRegistryUpgradeTests : RegistryUpgradeTests
{ protected override string Provider => "postgres"; }
