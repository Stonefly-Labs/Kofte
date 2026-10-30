using Kofte.Host.Bootstrap;
using Npgsql;
using System.Text.Json.Nodes;
using Xunit;

namespace Kofte.Host.IntegrationTests;

[Trait("Provider", "sqlite")]
[Trait("Suite", "recovery")]
public sealed class StoreRecoveryTests
{
    private static void WriteFixture(SqlitePlatformStore store, string key, string payload, bool create = false)
    {
        using var connection = store.Open();
        using var command = connection.CreateCommand();
        command.CommandText = create
            ? "CREATE TABLE bootstrap_fixture (fixture_key TEXT PRIMARY KEY, payload TEXT NOT NULL); INSERT INTO bootstrap_fixture VALUES ($key,$payload)"
            : "UPDATE bootstrap_fixture SET payload=$payload WHERE fixture_key=$key";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$payload", payload);
        command.ExecuteNonQuery();
    }

    private static bool Matches(SqlitePlatformStore store, string key, string payload)
    {
        using var connection = store.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT payload FROM bootstrap_fixture WHERE fixture_key=$key";
        command.Parameters.AddWithValue("$key", key);
        return command.ExecuteScalar() is string actual && actual == payload;
    }

    [Fact]
    public async Task VerifiedBackupsRestoreNewRootsAndGuardExistingData()
    {
        using var root = new TempRoot();
        using var backups = new TempRoot();
        using var restoredRoot = new TempRoot();
        var store = new SqlitePlatformStore(root.Path);
        await store.InitializeAsync();
        var key = Guid.NewGuid().ToString("N");
        var original = Guid.NewGuid().ToString("N");
        var changed = Guid.NewGuid().ToString("N");
        WriteFixture(store, key, original, create: true);
        var first = Path.Combine(backups.Path, "first.db");
        var current = Path.Combine(backups.Path, "current.db");
        store.Backup(first);
        WriteFixture(store, key, changed);
        Assert.Equal(BootstrapError.ConfirmationRequired,
            Assert.Throws<BootstrapException>(() => store.Restore(first)).Code);
        Assert.True(Matches(store, key, changed));
        Assert.Equal(BootstrapError.ConfirmationRequired,
            Assert.Throws<BootstrapException>(() => store.Restore(first, true, first)).Code);
        Assert.True(Matches(store, key, changed));
        store.Backup(current);
        store.Restore(first, confirmReplacement: true, previousBackup: current);
        Assert.True(Matches(store, key, original));
        var restored = new SqlitePlatformStore(restoredRoot.Path);
        restored.Restore(first);
        Assert.True(Matches(restored, key, original));
        Assert.Equal(BootstrapError.DestinationExists,
            Assert.Throws<BootstrapException>(() => store.Backup(first)).Code);
    }

    [Fact]
    public async Task ActiveHostsInvalidBackupsAndUnconfirmedResetPreserveData()
    {
        using var root = new TempRoot();
        using var backups = new TempRoot();
        var store = new SqlitePlatformStore(root.Path);
        await store.InitializeAsync();
        var key = Guid.NewGuid().ToString("N");
        var payload = Guid.NewGuid().ToString("N");
        WriteFixture(store, key, payload, create: true);
        var backup = Path.Combine(backups.Path, "platform.db");
        store.Backup(backup);
        using (StoreLease.Acquire(root.Path))
        {
            Assert.Equal(BootstrapError.StoreBusy, Assert.Throws<BootstrapException>(() =>
                store.Backup(Path.Combine(backups.Path, "blocked.db"))).Code);
            Assert.Equal(BootstrapError.StoreBusy, Assert.Throws<BootstrapException>(() => store.Restore(backup)).Code);
            Assert.Equal(BootstrapError.StoreBusy, Assert.Throws<BootstrapException>(() => store.Reset(true)).Code);
        }
        Assert.False(File.Exists(Path.Combine(backups.Path, "blocked.db")));
        Assert.Equal(BootstrapError.ConfirmationRequired,
            Assert.Throws<BootstrapException>(() => store.Reset(false)).Code);
        File.WriteAllText(backup, "invalid-backup-canary");
        Assert.Equal(BootstrapError.StorageInvalid, Assert.Throws<BootstrapException>(() => store.Restore(backup)).Code);
        Assert.True(Matches(store, key, payload));
        store.Reset(true);
        Assert.False(File.Exists(store.DatabasePath));
        Assert.True(File.Exists(Path.Combine(root.Path, ".platform.lock")));
        Assert.Throws<BootstrapException>(() => store.Open());
    }
}

[Trait("Provider", "postgres")]
[Trait("Suite", "recovery")]
public sealed class PostgresRecoveryTests
{
    [Fact]
    public async Task MatchingToolsRestoreExactFixtureIntoOnlyANewOwnedDatabase()
    {
        var token = TestContext.Current.CancellationToken;
        var disposable = Environment.GetEnvironmentVariable("KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE") == "true";
        await using var source = await PostgresFixture.CreateAsync(disposable, token: token);
        await using var target = await source.CreateRecoveryTargetAsync(token);
        Assert.NotEqual(source.RunId, target.RunId);
        Assert.NotEqual(source.ConnectionString, target.ConnectionString);
        using var backups = new TempRoot();
        var key = Guid.NewGuid().ToString("N");
        var payload = Guid.NewGuid().ToString("N");
        await using (var connection = new NpgsqlConnection(source.ConnectionString))
        {
            await connection.OpenAsync(token);
            await using var create = new NpgsqlCommand("""
                CREATE TABLE kofte_system.bootstrap_fixture (
                  run_id text NOT NULL, fixture_key text PRIMARY KEY, payload text NOT NULL)
                """, connection);
            await create.ExecuteNonQueryAsync(token);
            await using var write = new NpgsqlCommand(
                "INSERT INTO kofte_system.bootstrap_fixture VALUES ($1,$2,$3)", connection);
            write.Parameters.AddWithValue(source.RunId);
            write.Parameters.AddWithValue(key);
            write.Parameters.AddWithValue(payload);
            await write.ExecuteNonQueryAsync(token);
        }
        var backup = Path.Combine(backups.Path, "platform.dump");
        await source.BackupAsync(backup, token);
        await target.RestoreAsync(backup, token);
        Assert.True(await source.DatabaseExistsAsync(token));
        await source.ValidateOwnershipAsync(token);
        await new PostgresPlatformStore(target.ConnectionString).ProbeAsync(token);
        Assert.Equal("postgres_restore_target_not_empty",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => target.RestoreAsync(backup, token))).Message);
        var metadataPath = backup + ".metadata.json";
        var metadata = await File.ReadAllTextAsync(metadataPath, token);
        var incompatible = JsonNode.Parse(metadata) ?? throw new InvalidOperationException("test_backup_metadata_missing");
        incompatible["Major"] = 16;
        await File.WriteAllTextAsync(metadataPath, incompatible.ToJsonString(), token);
        Assert.Equal("postgres_backup_invalid",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => target.RestoreAsync(backup, token))).Message);
        await File.WriteAllTextAsync(metadataPath, metadata, token);
        await File.AppendAllTextAsync(backup, "corrupt-backup", token);
        Assert.Equal("postgres_backup_invalid",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => target.RestoreAsync(backup, token))).Message);
        await using var reopened = new NpgsqlConnection(target.ConnectionString);
        await reopened.OpenAsync(token);
        await using (var privileges = new NpgsqlCommand(
            "SELECT has_database_privilege(current_user,current_database(),'CREATE')", reopened))
            Assert.Equal(false, await privileges.ExecuteScalarAsync(token));
        await using var read = new NpgsqlCommand(
            "SELECT run_id,payload FROM kofte_system.bootstrap_fixture WHERE fixture_key=$1", reopened);
        read.Parameters.AddWithValue(key);
        await using var row = await read.ExecuteReaderAsync(token);
        Assert.True(await row.ReadAsync(token));
        Assert.True(row.GetString(0) == source.RunId && row.GetString(1) == payload,
            "Restore and refused replacement must preserve the exact private fixture.");
    }

    [Fact]
    public async Task RunningHostsAndConcurrentOperationsRefuseBackupWithoutCreatingOutput()
    {
        var token = TestContext.Current.CancellationToken;
        await using var database = await PostgresFixture.CreateAsync(
            Environment.GetEnvironmentVariable("KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE") == "true", token: token);
        using var sqliteRoot = new TempRoot();
        using var backups = new TempRoot();
        var backup = Path.Combine(backups.Path, "blocked.dump");
        await using (var host = await StartupFixture.StartAsync(sqliteRoot.Path, token,
            postgres: database.ConnectionString))
        {
            Assert.Equal("postgres_host_must_stop",
                (await Assert.ThrowsAsync<InvalidOperationException>(() => database.BackupAsync(backup, token))).Message);
            Assert.False(File.Exists(backup));
        }
        await using (await database.AcquireRecoveryAsync(token))
        {
            Assert.Equal("postgres_operation_busy",
                (await Assert.ThrowsAsync<InvalidOperationException>(() => database.BackupAsync(backup, token))).Message);
            Assert.False(File.Exists(backup));
        }
        Assert.Equal("postgres_registry_requires_complete_recovery_set",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => database.BackupAsync(backup, token))).Message);
        Assert.False(File.Exists(backup));
        Assert.False(Directory.Exists(sqliteRoot.Path));
    }
}
