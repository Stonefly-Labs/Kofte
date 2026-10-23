using Kofte.Host.Bootstrap;
using Npgsql;

namespace Kofte.Storage.IntegrationTests;

public sealed class StorageProviderBootstrapTests
{
    [Fact]
    [Trait("Provider", "sqlite")]
    public async Task SqliteUsesAnActualOwnedBootstrapStore()
    {
        await using var scope = await StorageTestEnvironment.CreateAsync("sqlite", TestContext.Current.CancellationToken);
        Assert.Null(scope.Postgres);
        var store = new SqlitePlatformStore(scope.DataRoot);
        await store.ProbeAsync(scope.Token);
        using var connection = store.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check";
        Assert.Equal("ok", await command.ExecuteScalarAsync(scope.Token));
        Assert.True(File.Exists(store.DatabasePath));
    }

    [Fact]
    [Trait("Provider", "postgres")]
    public async Task PostgresUsesAnActualOwnedDatabaseAndSeparateRestrictedProvisioner()
    {
        await using var scope = await StorageTestEnvironment.CreateAsync("postgres", TestContext.Current.CancellationToken);
        var database = Assert.IsType<Kofte.TestSupport.PostgresResources>(scope.Postgres);
        await database.ValidateOwnershipAsync(scope.Token);
        await new PostgresPlatformStore(database.ConnectionString).ProbeAsync(scope.Token);
        Assert.False(Directory.Exists(scope.DataRoot));
        await using var runtime = new NpgsqlConnection(database.ConnectionString);
        await runtime.OpenAsync(scope.Token);
        await using var runtimePrivileges = new NpgsqlCommand("""
            SELECT NOT has_database_privilege(current_user, current_database(), 'CREATE')
              AND NOT rolsuper AND NOT rolcreatedb AND NOT rolcreaterole AND NOT rolreplication AND NOT rolbypassrls
            FROM pg_catalog.pg_roles WHERE rolname=current_user
            """, runtime);
        Assert.Equal(true, await runtimePrivileges.ExecuteScalarAsync(scope.Token));
        await using var offline = new NpgsqlConnection(database.ProvisioningConnectionString);
        await offline.OpenAsync(scope.Token);
        await using var provisionerPrivileges = new NpgsqlCommand("""
            SELECT has_database_privilege(current_user, current_database(), 'CREATE')
              AND NOT rolsuper AND NOT rolcreatedb AND NOT rolcreaterole AND NOT rolreplication AND NOT rolbypassrls
            FROM pg_catalog.pg_roles WHERE rolname=current_user
            """, offline);
        Assert.Equal(true, await provisionerPrivileges.ExecuteScalarAsync(scope.Token));
        Assert.NotEqual(runtime.UserName, offline.UserName);
        Assert.Equal(runtime.Database, offline.Database);
        await using var provision = new NpgsqlCommand($"""
            BEGIN;
            CREATE SCHEMA fixture_offline AUTHORIZATION "{runtime.UserName}";
            SET LOCAL ROLE "{runtime.UserName}";
            CREATE TABLE fixture_offline.fixture (id integer PRIMARY KEY);
            ROLLBACK;
            """, offline);
        await provision.ExecuteNonQueryAsync(scope.Token);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("postgresql")]
    [InlineData("Host=example.com")]
    public void MissingAndUnknownProvidersCannotBecomeSkippedSuccess(string? provider) =>
        Assert.Throws<InvalidOperationException>(() => StorageTestEnvironment.Parse(provider));

    [Fact]
    public async Task CancellationBeforeSetupDoesNotProvision()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            StorageTestEnvironment.CreateAsync("sqlite", cancelled.Token));
    }
}
