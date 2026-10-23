using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Npgsql;

namespace Kofte.Storage.IntegrationTests.Registry;

[Trait("Provider", "postgres")]
public sealed class PostgresRegistryFormatTests
{
    [Fact]
    public async Task StartupAndUpgradeBothRejectUnaccountedNonModuleSchemaWithoutAdoption()
    {
        await using var fixture = await RegistryFixture.CreateAsync("postgres",
            TestContext.Current.CancellationToken, upgrade: false);
        await using var scope = await fixture.Maintenance.AcquireAsync(TestContext.Current.CancellationToken);
        await using (var provisioner = new NpgsqlConnection(fixture.ProvisioningConnection))
        {
            await provisioner.OpenAsync(TestContext.Current.CancellationToken);
            await using var create = new NpgsqlCommand("CREATE SCHEMA orphan_boundary", provisioner)
            { CommandTimeout = 3 };
            await create.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
        await Assert.ThrowsAsync<StorageException>(() => StorageReconciliation.ValidateStartupAsync(
            fixture.Installation, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<StorageException>(() => fixture.Maintenance.UpgradeAsync(
            scope, TestContext.Current.CancellationToken));
        using var budget = new OperationBudget(OperationBudget.Resolve, TestContext.Current.CancellationToken);
        await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
        Assert.Equal(0L, await RegistryMigrationRunner.ScalarAsync(connection,
            "SELECT count(*) FROM pg_catalog.pg_tables WHERE schemaname='kofte_system'", budget));
        Assert.Equal(1L, await RegistryMigrationRunner.ScalarAsync(connection,
            "SELECT count(*) FROM pg_catalog.pg_namespace WHERE nspname='orphan_boundary'", budget));
    }

    [Fact]
    public async Task ExplicitV0UpgradeCommitsOneOwnedVersionAndRetryDoesNotChangeIt()
    {
        await using var fixture = await RegistryFixture.CreateAsync("postgres",
            TestContext.Current.CancellationToken, upgrade: false);
        await using var scope = await fixture.Maintenance.AcquireAsync(TestContext.Current.CancellationToken);
        await fixture.Maintenance.UpgradeAsync(scope, TestContext.Current.CancellationToken);
        using var budget = new OperationBudget(OperationBudget.Register, TestContext.Current.CancellationToken);
        await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
        var runner = new RegistryMigrationRunner(fixture.Installation);
        var before = await runner.ReadInstallationAsync(connection, budget);
        Assert.Equal(1, before.Version);
        Assert.Equal("postgresql", before.Provider);
        Assert.NotEqual(Guid.Empty, before.Id);
        Assert.Equal(0, before.Revision);
        await fixture.Maintenance.UpgradeAsync(scope, TestContext.Current.CancellationToken);
        Assert.Equal(before, await runner.ReadInstallationAsync(connection, budget));
        Assert.Equal(1L, await RegistryMigrationRunner.ScalarAsync(connection,
            "SELECT count(*) FROM kofte_system.__kofte_registry_migrations", budget));
        await fixture.AssertInventoryAsync(0, 0, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PopulatedV0AndV1BadHistoryArePreservedRatherThanAdoptedOrRepaired()
    {
        await using var fixture = await RegistryFixture.CreateAsync("postgres",
            TestContext.Current.CancellationToken, upgrade: false);
        await using var scope = await fixture.Maintenance.AcquireAsync(TestContext.Current.CancellationToken);
        using var budget = new OperationBudget(OperationBudget.Register, TestContext.Current.CancellationToken);
        await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
        await RegistryMigrationRunner.ExecuteAsync(connection,
            "CREATE TABLE kofte_system.unowned (value TEXT); INSERT INTO kofte_system.unowned VALUES ('preserved')", budget);
        await Assert.ThrowsAsync<StorageException>(() => fixture.Maintenance.UpgradeAsync(
            scope, TestContext.Current.CancellationToken));
        Assert.Equal("preserved", await RegistryMigrationRunner.ScalarAsync(connection,
            "SELECT value FROM kofte_system.unowned", budget));
        await RegistryMigrationRunner.ExecuteAsync(connection, "DROP TABLE kofte_system.unowned", budget);
        await fixture.Maintenance.UpgradeAsync(scope, TestContext.Current.CancellationToken);
        await RegistryMigrationRunner.ExecuteAsync(connection,
            "UPDATE kofte_system.__kofte_registry_migrations SET sha256=repeat('0',64)", budget);
        await Assert.ThrowsAsync<StorageException>(() => fixture.Maintenance.UpgradeAsync(
            scope, TestContext.Current.CancellationToken));
        Assert.Equal(new string('0', 64), await RegistryMigrationRunner.ScalarAsync(connection,
            "SELECT sha256 FROM kofte_system.__kofte_registry_migrations", budget));
        Assert.Equal(0L, await RegistryMigrationRunner.ScalarAsync(connection,
            "SELECT registry_revision FROM kofte_system.__kofte_installation", budget));
    }
}
