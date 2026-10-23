using System.Reflection;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Providers;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Recovery;

[Trait("Provider", "sqlite"), Trait("Suite", "recovery")]
public sealed class AbandonedLockRefusalTests
{
    [Theory]
    [InlineData("shape")]
    [InlineData("row")]
    [InlineData("history")]
    public async Task UnknownRealAbandonedLockOrHistoryIsPreservedWithoutClearingAnything(string corruption)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync("sqlite", token);
        var owner = RegistryFixture.Module("org.example.abandoned-refusal");
        StorageAssignment assignment;
        await using (var scope = await fixture.Maintenance.AcquireAsync(token))
            assignment = Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(owner, scope, token)).Assignment;
        const string phase = "migration_sql_executing";
        await StorageCrashWorker.KillAtAsync(StorageCrashWorker.Start(Assembly.GetExecutingAssembly(), "sqlite",
            "migrate", phase, owner.Id.Value, child => ProvisioningCrashTests.Configure(fixture, child)), phase, token);
        var path = Path.Combine(fixture.Root!, "modules", assignment.AllocationKey, "module.db");
        using var budget = new OperationBudget(OperationBudget.Register, token);
        using var connection = StorageInstallation.OpenSqlite(path, budget);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM __EFMigrationsLock WHERE Id=1";
        Assert.Equal(1L, command.ExecuteScalar());
        command.CommandText = corruption switch
        {
            "shape" => "ALTER TABLE __EFMigrationsLock ADD COLUMN Unknown TEXT",
            "row" => "UPDATE __EFMigrationsLock SET Timestamp='not-a-timestamp'",
            _ => """
                CREATE TABLE IF NOT EXISTS __EFMigrationsHistory
                  (MigrationId TEXT NOT NULL PRIMARY KEY, ProductVersion TEXT NOT NULL);
                INSERT INTO __EFMigrationsHistory VALUES ('unknown-migration','10.0.13')
                """
        };
        command.ExecuteNonQuery();
        var before = await File.ReadAllBytesAsync(path, token);
        var original = await StorageInspection.ReadAsync("sqlite", fixture.Root, null, owner.Id, token);
        await using var scope2 = await fixture.Maintenance.AcquireAsync(token);
        Assert.Equal(StorageOutcome.ReconciliationRequired, (await ModuleMigrationRecovery.RecoverAsync(
            fixture.Registry, owner.Id, ModuleFixture.AlphaFactory, scope2, token)).Outcome);
        Assert.Equal(before, await File.ReadAllBytesAsync(path, token));
        Assert.Equal(original, await StorageInspection.ReadAsync("sqlite", fixture.Root, null, owner.Id, token));
        command.CommandText = "SELECT count(*) FROM __EFMigrationsLock WHERE Id=1";
        Assert.Equal(1L, command.ExecuteScalar());
    }
}
