using System.Net;
using Kofte.Host.Bootstrap;
using Npgsql;
using Xunit;

namespace Kofte.Host.IntegrationTests;

[Trait("Provider", "postgres")]
public sealed class PostgresStoreTests
{
    [Fact]
    public async Task OrdinaryFixturesShareOnlyTheOwnedServiceAndDisposeTheirOwnDatabaseAndRoles()
    {
        var token = TestContext.Current.CancellationToken;
        await using var first = await PostgresFixture.CreateFromEnvironmentAsync(token);
        await using var second = await PostgresFixture.CreateFromEnvironmentAsync(token);
        var firstConnection = new NpgsqlConnectionStringBuilder(first.ConnectionString);
        var secondConnection = new NpgsqlConnectionStringBuilder(second.ConnectionString);
        var firstProvisioner = new NpgsqlConnectionStringBuilder(first.ProvisioningConnectionString);
        Assert.Equal(firstConnection.Port, secondConnection.Port);
        Assert.True(firstConnection.Database != secondConnection.Database &&
            firstConnection.Username != secondConnection.Username);
        await first.DisposeAsync();
        await new PostgresPlatformStore(second.ConnectionString).ProbeAsync(token);
        await second.ValidateOwnershipAsync(token);
        await using var connection = new NpgsqlConnection(second.ConnectionString);
        await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand("""
            SELECT NOT EXISTS (SELECT 1 FROM pg_catalog.pg_database WHERE datname=$1)
              AND NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname=ANY($2))
            """, connection);
        command.Parameters.AddWithValue(firstConnection.Database!);
        command.Parameters.AddWithValue(new[] { firstConnection.Username!, firstProvisioner.Username! });
        Assert.True(await command.ExecuteScalarAsync(token) is true);
    }

    [Fact]
    public async Task ActualHostReprobesSchemaPrivilegesWithoutSqliteFallback()
    {
        await using var database = await PostgresFixture.CreateFromEnvironmentAsync(TestContext.Current.CancellationToken);
        using var unusedSqliteRoot = new TempRoot();
        await using var host = await StartupFixture.StartAsync(unusedSqliteRoot.Path,
            TestContext.Current.CancellationToken, postgres: database.ConnectionString);
        Assert.False(Directory.Exists(unusedSqliteRoot.Path));
        await database.SetSchemaCreationAsync(allowed: false, TestContext.Current.CancellationToken);
        try
        {
            using var ready = await host.Client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
            using var live = await host.Client.GetAsync("/health/live", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
            Assert.Equal(HttpStatusCode.OK, live.StatusCode);
            Assert.False(Directory.Exists(unusedSqliteRoot.Path));
        }
        finally
        {
            await database.SetSchemaCreationAsync(allowed: true, TestContext.Current.CancellationToken);
        }
        using var recovered = await host.Client.GetAsync("/health/ready", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
    }

    [Fact]
    public async Task OwnedLimitedRoleRoundTripsOnFreshConnectionsAndProbes()
    {
        await using var fixture = await PostgresFixture.CreateFromEnvironmentAsync(TestContext.Current.CancellationToken);
        var key = Guid.NewGuid().ToString("N");
        var payload = Guid.NewGuid().ToString("N");
        await using (var connection = new NpgsqlConnection(fixture.ConnectionString))
        {
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            await using var create = new NpgsqlCommand(
                "CREATE TABLE kofte_system.bootstrap_fixture (run_id text NOT NULL, fixture_key text PRIMARY KEY, payload text NOT NULL)", connection);
            await create.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            await using var write = new NpgsqlCommand(
                "INSERT INTO kofte_system.bootstrap_fixture VALUES ($1, $2, $3)", connection);
            write.Parameters.AddWithValue(fixture.RunId);
            write.Parameters.AddWithValue(key);
            write.Parameters.AddWithValue(payload);
            await write.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
        await new PostgresPlatformStore(fixture.ConnectionString).ProbeAsync(TestContext.Current.CancellationToken);
        await using var reopened = new NpgsqlConnection(fixture.ConnectionString);
        await reopened.OpenAsync(TestContext.Current.CancellationToken);
        await using var read = new NpgsqlCommand(
            "SELECT run_id, payload FROM kofte_system.bootstrap_fixture WHERE fixture_key=$1", reopened);
        read.Parameters.AddWithValue(key);
        await using var row = await read.ExecuteReaderAsync(TestContext.Current.CancellationToken);
        Assert.True(await row.ReadAsync(TestContext.Current.CancellationToken));
        Assert.True(row.GetString(0) == fixture.RunId && row.GetString(1) == payload,
            "The exact run-owned fixture must survive a fresh connection.");
    }
}
