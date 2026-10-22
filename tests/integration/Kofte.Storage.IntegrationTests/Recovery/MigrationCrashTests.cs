using System.Reflection;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Modules.Alpha;
using Kofte.Storage.IntegrationTests.Providers;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Kofte.Storage.IntegrationTests.Recovery;

public abstract class MigrationCrashTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task ActualKilledMigrationAndLostCommitAcknowledgementsRecoverOnlyVerifiedHistory()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync(Provider, token);
        var phases = StorageCrashWorker.MigrationPhases.Concat(
            Provider == "sqlite" ? ["migration_sql_executing"] : Array.Empty<string>());
        foreach (var phase in phases)
        {
            var owner = RegistryFixture.Module("org.example.migration-" + phase.Replace('_', '-'));
            await using (var scope = await fixture.Maintenance.AcquireAsync(token))
                Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(owner, scope, token));
            var start = StorageCrashWorker.Start(Assembly.GetExecutingAssembly(), Provider, "migrate", phase,
                owner.Id.Value, child => ProvisioningCrashTests.Configure(fixture, child));
            await StorageCrashWorker.KillAtAsync(start, phase, token);
            var resolved = await fixture.Registry.ResolveAsync(owner.Id, token);
            var ready = phase is "migration_in_progress_before_commit" or "migration_current_committed";
            Assert.Equal(ready ? StorageOutcome.Ready : StorageOutcome.ReconciliationRequired, resolved.Outcome);
            if (phase == "migration_sql_executing")
            {
                using var budget = new OperationBudget(OperationBudget.Resolve, token);
                await using var platform = await fixture.Installation.OpenPlatformAsync(budget);
                var row = await new RegistryMigrationRunner(fixture.Installation).FindAsync(platform, owner.Id, budget);
                using var connection = StorageInstallation.OpenSqlite(
                    Path.Combine(fixture.Root!, "modules", row!.Key!, "module.db"), budget);
                using var locked = connection.CreateCommand();
                locked.CommandText = "SELECT count(*) FROM __EFMigrationsLock WHERE Id=1";
                Assert.Equal(1L, locked.ExecuteScalar());
            }
            await using var maintenance = await fixture.Maintenance.AcquireAsync(token);
            if (!ready)
            {
                var recovered = await ModuleMigrationRecovery.RecoverAsync(fixture.Registry, owner.Id,
                    ModuleFixture.AlphaFactory, maintenance, token);
                Assert.IsType<StorageResult.Ready>(recovered);
            }
            await using var binding = await fixture.Registry.BindAsync(owner.Id, token);
            if (phase == "migration_in_progress_before_commit")
                Assert.IsType<StorageResult.Ready>(await binding.MigrateAsync(ModuleFixture.AlphaFactory,
                    maintenance, "202610210001_Initial", token));
            await using var context = await binding.CreateContextAsync(ModuleFixture.AlphaFactory, token);
            Assert.Single(await context.Database.GetAppliedMigrationsAsync(token));
            var table = binding.Schema is { } schema ? $"\"{schema}\".\"Records\"" : "\"Records\"";
            var insert = "INSERT INTO " + table + " (\"Id\",\"Payload\") VALUES ({0},{1})";
            await context.Database.ExecuteSqlRawAsync(insert, [1, owner.Id.Value], token);
            using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT \"Payload\" FROM " + table + " WHERE \"Id\"=1";
            Assert.Equal(owner.Id.Value, await command.ExecuteScalarAsync(token));
        }
    }
}

[Trait("Provider", "sqlite"), Trait("Suite", "recovery")]
public sealed class SqliteMigrationCrashTests : MigrationCrashTests
{ protected override string Provider => "sqlite"; }
[Trait("Provider", "postgres"), Trait("Suite", "recovery")]
public sealed class PostgresMigrationCrashTests : MigrationCrashTests
{ protected override string Provider => "postgres"; }
