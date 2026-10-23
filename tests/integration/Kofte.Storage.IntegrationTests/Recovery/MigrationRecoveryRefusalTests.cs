using System.Reflection;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Providers;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Recovery;

public abstract class MigrationRecoveryRefusalTests
{
    protected abstract string Provider { get; }

    [Theory]
    [InlineData("column")]
    [InlineData("index")]
    [InlineData("primary")]
    [InlineData("default")]
    [InlineData("view")]
    [InlineData("check")]
    [InlineData("collation")]
    public async Task RecoveryPreservesInconsistentPhysicalEffectsAndDurableIntent(string corruption)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync(Provider, token);
        var owner = RegistryFixture.Module("org.example.recovery-refusal");
        StorageAssignment assignment;
        await using (var scope = await fixture.Maintenance.AcquireAsync(token))
            assignment = Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(owner, scope, token)).Assignment;
        const string phase = "migration_committed";
        await StorageCrashWorker.KillAtAsync(StorageCrashWorker.Start(Assembly.GetExecutingAssembly(), Provider,
            "migrate", phase, owner.Id.Value, child => ProvisioningCrashTests.Configure(fixture, child)), phase, token);
        using var budget = new OperationBudget(OperationBudget.Register, token);
        await using var connection = Provider == "sqlite"
            ? StorageInstallation.OpenSqlite(Path.Combine(fixture.Root!, "modules", assignment.AllocationKey, "module.db"), budget)
            : await fixture.Installation.OpenPlatformAsync(budget);
        var prefix = Provider == "sqlite" ? "" : $"\"{assignment.AllocationKey}\".";
        await using var command = connection.CreateCommand();
        command.CommandText = corruption switch
        {
            "column" => $"ALTER TABLE {prefix}\"Records\" ADD COLUMN \"Unexpected\" TEXT",
            "index" => $"CREATE INDEX \"Unexpected\" ON {prefix}\"Records\" (\"Payload\")",
            "view" => $"CREATE VIEW {prefix}\"Unexpected\" AS SELECT * FROM {prefix}\"Records\"",
            "check" when Provider == "postgres" => $"ALTER TABLE {prefix}\"Records\" ADD CHECK (length(\"Payload\")>0)",
            "collation" when Provider == "postgres" => $"ALTER TABLE {prefix}\"Records\" ALTER COLUMN \"Payload\" TYPE text COLLATE \"C\"",
            "check" or "collation" => $"""
                ALTER TABLE Records RENAME TO OriginalRecords;
                CREATE TABLE Records (Id INTEGER NOT NULL PRIMARY KEY,
                  Payload TEXT NOT NULL {(corruption == "check" ? "CHECK(length(Payload)>0)" : "COLLATE NOCASE")});
                INSERT INTO Records SELECT * FROM OriginalRecords;
                DROP TABLE OriginalRecords
                """,
            "default" when Provider == "postgres" => $"ALTER TABLE {prefix}\"Records\" ALTER COLUMN \"Payload\" SET DEFAULT 'unexpected'",
            "default" => """
                ALTER TABLE Records RENAME TO OriginalRecords;
                CREATE TABLE Records (Id INTEGER NOT NULL PRIMARY KEY, Payload TEXT NOT NULL DEFAULT 'unexpected');
                INSERT INTO Records SELECT * FROM OriginalRecords;
                DROP TABLE OriginalRecords
                """,
            _ when Provider == "postgres" => $"ALTER TABLE {prefix}\"Records\" DROP CONSTRAINT \"PK_Records\"",
            _ => """
                ALTER TABLE Records RENAME TO OriginalRecords;
                CREATE TABLE Records (Id INTEGER NOT NULL, Payload TEXT NOT NULL);
                INSERT INTO Records SELECT * FROM OriginalRecords;
                DROP TABLE OriginalRecords
                """
        };
        await command.ExecuteNonQueryAsync(token);
        command.CommandText = $"INSERT INTO {prefix}\"Records\" (\"Id\",\"Payload\") VALUES (1,'preserved-refusal')";
        await command.ExecuteNonQueryAsync(token);
        var before = await StorageInspection.ReadAsync(Provider, fixture.Root, fixture.RuntimeConnection, owner.Id, token);
        var bytes = Provider == "sqlite" ? await File.ReadAllBytesAsync(
            Path.Combine(fixture.Root!, "modules", assignment.AllocationKey, "module.db"), token) : null;
        await using var maintenance = await fixture.Maintenance.AcquireAsync(token);
        var result = await ModuleMigrationRecovery.RecoverAsync(fixture.Registry, owner.Id,
            ModuleFixture.AlphaFactory, maintenance, token);
        Assert.Equal(StorageOutcome.ReconciliationRequired, result.Outcome);
        Assert.Equal(before, await StorageInspection.ReadAsync(Provider, fixture.Root, fixture.RuntimeConnection, owner.Id, token));
        command.CommandText = $"SELECT \"Payload\" FROM {prefix}\"Records\" WHERE \"Id\"=1";
        Assert.Equal("preserved-refusal", await command.ExecuteScalarAsync(token));
        if (bytes is not null)
            Assert.Equal(bytes, await File.ReadAllBytesAsync(
                Path.Combine(fixture.Root!, "modules", assignment.AllocationKey, "module.db"), token));
    }
}

[Trait("Provider", "sqlite"), Trait("Suite", "recovery")]
public sealed class SqliteMigrationRecoveryRefusalTests : MigrationRecoveryRefusalTests
{ protected override string Provider => "sqlite"; }
[Trait("Provider", "postgres"), Trait("Suite", "recovery")]
public sealed class PostgresMigrationRecoveryRefusalTests : MigrationRecoveryRefusalTests
{ protected override string Provider => "postgres"; }
