using Kofte.Host.Bootstrap;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;

namespace Kofte.Storage.IntegrationTests.Registry;

[Trait("Provider", "sqlite")]
public sealed class RegistryFormatTests
{
    [Fact]
    public async Task ExplicitUpgradeIsAtomicIdempotentAndHasRealHistory()
    {
        using var root = new RegistryRoot();
        var bootstrap = new SqlitePlatformStore(root.Path);
        await bootstrap.InitializeAsync();
        var installation = StorageInstallation.Sqlite(root.Path);
        var maintenance = new StorageMaintenance(installation);
        await using var scope = await maintenance.AcquireAsync(TestContext.Current.CancellationToken);
        await maintenance.UpgradeAsync(scope, TestContext.Current.CancellationToken);
        using var budget = new OperationBudget(OperationBudget.Register, TestContext.Current.CancellationToken);
        await using var connection = await installation.OpenPlatformAsync(budget);
        var runner = new RegistryMigrationRunner(installation);
        var before = await runner.ReadInstallationAsync(connection, budget);
        Assert.Equal(1, before.Version);
        Assert.Equal("sqlite", before.Provider);
        Assert.NotEqual(Guid.Empty, before.Id);
        Assert.Equal(0, before.Revision);
        await maintenance.UpgradeAsync(scope, TestContext.Current.CancellationToken);
        Assert.Equal(before, await runner.ReadInstallationAsync(connection, budget));
        Assert.False(Directory.Exists(System.IO.Path.Combine(root.Path, "modules")));
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM __kofte_registry_migrations";
        Assert.Equal(1L, await command.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        await bootstrap.ProbeAsync(TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData("PRAGMA user_version=2")]
    [InlineData("CREATE TABLE unknown_data (payload TEXT); INSERT INTO unknown_data VALUES ('preserved')")]
    public async Task UnsupportedOrPopulatedV0NeverGetsReinterpreted(string mutation)
    {
        using var root = new RegistryRoot();
        var bootstrap = new SqlitePlatformStore(root.Path);
        await bootstrap.InitializeAsync();
        using (var connection = bootstrap.Open())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = mutation;
            command.ExecuteNonQuery();
        }
        var before = await File.ReadAllBytesAsync(bootstrap.DatabasePath, TestContext.Current.CancellationToken);
        var maintenance = new StorageMaintenance(StorageInstallation.Sqlite(root.Path));
        await using var scope = await maintenance.AcquireAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<StorageException>(() =>
            maintenance.UpgradeAsync(scope, TestContext.Current.CancellationToken));
        Assert.Equal(before, await File.ReadAllBytesAsync(bootstrap.DatabasePath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task V1ConstraintsRejectDirectDuplicateIdsKeysAndTokensAndKeepOriginalRecord()
    {
        using var root = new RegistryRoot();
        var bootstrap = new SqlitePlatformStore(root.Path);
        await bootstrap.InitializeAsync();
        var maintenance = new StorageMaintenance(StorageInstallation.Sqlite(root.Path));
        await using var scope = await maintenance.AcquireAsync(TestContext.Current.CancellationToken);
        await maintenance.UpgradeAsync(scope, TestContext.Current.CancellationToken);
        using var connection = bootstrap.Open();
        const string key = "m_alpha_00000000000000000000000000000000";
        const string token = "00000000-0000-0000-0000-000000000001";
        void Insert(string id, string candidate, string allocation)
        {
            using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO __kofte_module_registrations
                (module_id,relational,provider,allocation_key,allocator_version,allocation_id,state,migration_state,revision)
                VALUES (@id,1,'sqlite',@key,1,@allocation,'reserved','none',1)
                """;
            command.Parameters.AddWithValue("id", id);
            command.Parameters.AddWithValue("key", candidate);
            command.Parameters.AddWithValue("allocation", allocation);
            command.ExecuteNonQuery();
        }
        Insert("org.example.alpha", key, token);
        Assert.Equal(19, Assert.Throws<SqliteException>(() => Insert("org.example.alpha",
            key.Replace("alpha", "other"), Guid.NewGuid().ToString())).SqliteErrorCode);
        Assert.Equal(19, Assert.Throws<SqliteException>(() =>
            Insert("org.example.beta", key, Guid.NewGuid().ToString())).SqliteErrorCode);
        Assert.Equal(19, Assert.Throws<SqliteException>(() =>
            Insert("org.example.beta", key.Replace("alpha", "other"), token)).SqliteErrorCode);
        foreach (var invalid in new[] { "org..alpha", "org.-bad.alpha", "org.bad-.alpha", "org.two", "org.example.alpha." })
            Assert.Equal(19, Assert.Throws<SqliteException>(() =>
                Insert(invalid, key.Replace("alpha", "other"), Guid.NewGuid().ToString())).SqliteErrorCode);
        using var count = connection.CreateCommand();
        count.CommandText = "SELECT count(*) FROM __kofte_module_registrations";
        Assert.Equal(1L, count.ExecuteScalar());
    }
}

internal sealed class RegistryRoot : IDisposable
{
    internal string Path { get; } = System.IO.Path.Combine(
        OperatingSystem.IsMacOS() ? "/private/tmp" : System.IO.Path.GetTempPath(),
        "kofte-storage-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
    }
}
