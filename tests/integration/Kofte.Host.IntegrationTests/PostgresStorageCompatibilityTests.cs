using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.TestSupport;
using Npgsql;
using Xunit;

namespace Kofte.Host.IntegrationTests;

[Trait("Provider", "postgres")]
public sealed class PostgresStorageCompatibilityTests
{
    [Fact]
    public async Task ModuleFreeOwnedPostgresKeepsV0AndExplicitV1StartupWithoutProvisioner()
    {
        var token = TestContext.Current.CancellationToken;
        await using var database = await PostgresResources.CreateFromEnvironmentAsync(token);
        using var unusedRoot = new TempRoot();
        await using (var host = await StartupFixture.StartAsync(unusedRoot.Path, token, postgres: database.ConnectionString))
        {
            await StorageCompatibilityTests.AssertCoreSurfaceAsync(host, token);
        }
        var installation = StorageInstallation.PostgreSql(database.ConnectionString);
        await StorageReconciliation.ValidateStartupAsync(installation, token);
        var upgrade = await StartupFixture.RunExitAsync(unusedRoot.Path, ["--registry", "upgrade"], token,
            postgres: database.ConnectionString);
        Assert.Equal(0, upgrade.ExitCode);
        StorageCompatibilityTests.AssertUpgradeRecord(upgrade.Output, unusedRoot.Path,
            "storage_ready", "ready", "retry_same_request", "postgresql");
        await using (var host = await StartupFixture.StartAsync(unusedRoot.Path, token, postgres: database.ConnectionString))
        {
            await StorageCompatibilityTests.AssertCoreSurfaceAsync(host, token);
        }
        Assert.Equal(0, (await StartupFixture.RunExitAsync(unusedRoot.Path,
            ["--registry", "upgrade"], token, postgres: database.ConnectionString)).ExitCode);
        Assert.False(Directory.Exists(unusedRoot.Path));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RefusedOwnedPostgresUpgradeProjectsSafeCauseAndPreservesMetadata(bool unsupported)
    {
        var token = TestContext.Current.CancellationToken;
        await using var database = await PostgresResources.CreateFromEnvironmentAsync(token);
        using var unusedRoot = new TempRoot();
        var installation = StorageInstallation.PostgreSql(database.ConnectionString);
        var maintenance = new StorageMaintenance(installation);
        await using (var scope = await maintenance.AcquireAsync(token))
            await maintenance.UpgradeAsync(scope, token);
        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand(unsupported ? """
            ALTER TABLE kofte_system.__kofte_installation DISABLE TRIGGER __kofte_installation_change;
            ALTER TABLE kofte_system.__kofte_installation DROP CONSTRAINT __kofte_installation_registry_version_check;
            UPDATE kofte_system.__kofte_installation SET registry_version=9;
            """ : "UPDATE kofte_system.__kofte_registry_migrations SET sha256=repeat('0',64)", connection);
        await command.ExecuteNonQueryAsync(token);
        command.CommandText = """
            SELECT row_to_json(i)::text || row_to_json(h)::text
            FROM kofte_system.__kofte_installation i CROSS JOIN kofte_system.__kofte_registry_migrations h
            """;
        var before = await command.ExecuteScalarAsync(token);
        var result = await StartupFixture.RunExitAsync(unusedRoot.Path, ["--registry", "upgrade"], token,
            postgres: database.ConnectionString);
        Assert.Equal(1, result.ExitCode);
        StorageCompatibilityTests.AssertUpgradeRecord(result.Output, unusedRoot.Path,
            unsupported ? "storage_version_unsupported" : "storage_corrupt",
            unsupported ? "failed" : "reconciliationrequired",
            unsupported ? "upgrade_supported_binary" : "restore_complete_set", "postgresql");
        Assert.DoesNotContain(database.ConnectionString, result.Output);
        Assert.DoesNotContain(new NpgsqlConnectionStringBuilder(database.ConnectionString).Password!, result.Output);
        Assert.Equal(before, await command.ExecuteScalarAsync(token));
        Assert.False(Directory.Exists(unusedRoot.Path));
    }
}
