using Kofte.Infrastructure.Storage;
using Kofte.TestSupport;
using Npgsql;

namespace Kofte.Storage.IntegrationTests.Providers;

[Trait("Provider", "postgres"), Trait("Suite", "recovery")]
public sealed class PostgresScopeTests
{
    [Fact]
    public async Task IndependentDatabasesRetainOwnedArchiveToolsAndCleanupCannotStopAnotherScope()
    {
        var token = TestContext.Current.CancellationToken;
        await using var first = await StorageProviderScope.CreateAsync("postgres", token);
        await using var second = await StorageProviderScope.CreateAsync("postgres", token);
        var firstConnection = new NpgsqlConnectionStringBuilder(first.Postgres!.ConnectionString);
        var secondConnection = new NpgsqlConnectionStringBuilder(second.Postgres!.ConnectionString);
        Assert.Equal(firstConnection.Port, secondConnection.Port);
        Assert.NotEqual(firstConnection.Database, secondConnection.Database);
        Assert.NotEqual(firstConnection.Username, secondConnection.Username);
        await first.Postgres.ValidateOwnershipAsync(token);
        await second.Postgres.ValidateOwnershipAsync(token);
        await using (var connection = new NpgsqlConnection(second.Postgres.ConnectionString))
        {
            await connection.OpenAsync(token);
            await using (var active = new NpgsqlCommand("""
                SELECT COUNT(*) FROM pg_catalog.pg_stat_activity
                  WHERE application_name IN (@first,@second)
                """, connection) { CommandTimeout = 3 })
            {
                active.Parameters.AddWithValue("first", "kofte_fixture_admin_" + first.Postgres.RunId);
                active.Parameters.AddWithValue("second", "kofte_fixture_admin_" + second.Postgres.RunId);
                Assert.Equal(2L, await active.ExecuteScalarAsync(token));
            }
            var originalBackends = first.Postgres.AdministrationBackends.ToArray();
            await first.DisposeAsync();
            Assert.Equal(originalBackends, first.Postgres.AdministrationBackends);
            await second.Postgres.ValidateOwnershipAsync(token);
            await using var command = new NpgsqlCommand("""
                SELECT NOT EXISTS (SELECT 1 FROM pg_catalog.pg_database WHERE datname=@database)
                  AND NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname=@role)
                  AND NOT EXISTS (SELECT 1 FROM pg_catalog.pg_stat_activity WHERE application_name=@first)
                  AND EXISTS (SELECT 1 FROM pg_catalog.pg_stat_activity WHERE application_name=@second)
                """, connection) { CommandTimeout = 3 };
            command.Parameters.AddWithValue("database", firstConnection.Database!);
            command.Parameters.AddWithValue("role", firstConnection.Username!);
            command.Parameters.AddWithValue("first", "kofte_fixture_admin_" + first.Postgres.RunId);
            command.Parameters.AddWithValue("second", "kofte_fixture_admin_" + second.Postgres.RunId);
            Assert.True((bool)(await command.ExecuteScalarAsync(token))!);
        }
        var maintenance = new StorageMaintenance(StorageInstallation.PostgreSql(
            second.Postgres.ConnectionString, second.Postgres.ProvisioningConnectionString));
        await using (var scope = await maintenance.AcquireAsync(token))
            await maintenance.UpgradeAsync(scope, token);
        using var archive = new OwnedTestRoot();
        var manifest = await second.Postgres.BackupStorageSetAsync(archive.Path, token);
        Assert.Equal("kofte-storage-recovery-v1", manifest.Format);
        Assert.Single(manifest.Artifacts);
        await second.Postgres.ValidateOwnershipAsync(token);
    }
}
