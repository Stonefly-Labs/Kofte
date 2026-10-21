using Kofte.Host.Bootstrap;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Kofte.Host.IntegrationTests;

public sealed class TempRoot : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(
        OperatingSystem.IsMacOS() ? "/private/tmp" : System.IO.Path.GetTempPath(),
        "kofte-test-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(Path)) Directory.Delete(Path, true);
    }
}

public sealed class SqliteStoreTests
{
    [Fact]
    public async Task ExplicitInitPersistsFixturesAcrossNewConnectionsAndProbes()
    {
        using var root = new TempRoot();
        var store = new SqlitePlatformStore(root.Path);
        await store.InitializeAsync();
        await using (var connection = store.Open())
        {
            using var command = connection.CreateCommand();
            command.CommandText = "CREATE TABLE bootstrap_fixture (id TEXT PRIMARY KEY, payload TEXT NOT NULL); INSERT INTO bootstrap_fixture VALUES ($id, $payload)";
            command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
            command.Parameters.AddWithValue("$payload", "opaque-fixture");
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
        }
        var restarted = new SqlitePlatformStore(root.Path);
        await restarted.ProbeAsync(TestContext.Current.CancellationToken);
        await using var reopened = restarted.Open();
        using var read = reopened.CreateCommand();
        read.CommandText = "SELECT payload FROM bootstrap_fixture";
        Assert.Equal("opaque-fixture", await read.ExecuteScalarAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<BootstrapException>(() => restarted.InitializeAsync());
    }

    [Fact]
    public async Task StartupNeverCreatesMissingOrReplacesCorruptFiles()
    {
        using var root = new TempRoot();
        var store = new SqlitePlatformStore(root.Path);
        Assert.Throws<BootstrapException>(() => store.Open());
        Assert.False(Directory.Exists(root.Path));
        await store.InitializeAsync();
        File.WriteAllText(store.DatabasePath, "credential-canary-corrupt-store");
        await Assert.ThrowsAnyAsync<Exception>(() => store.ProbeAsync(TestContext.Current.CancellationToken));
        Assert.Equal("credential-canary-corrupt-store", File.ReadAllText(store.DatabasePath));
    }

    [Fact]
    public async Task LockFailureAndEscapedPathsPreserveExistingData()
    {
        using var root = new TempRoot();
        using var outside = new TempRoot();
        var store = new SqlitePlatformStore(root.Path);
        await store.InitializeAsync();
        await using (var connection = store.Open())
        {
            using var command = connection.CreateCommand();
            command.CommandText = "BEGIN EXCLUSIVE";
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);
            await Assert.ThrowsAnyAsync<Exception>(() => store.ProbeAsync(TestContext.Current.CancellationToken));
        }
        await store.ProbeAsync(TestContext.Current.CancellationToken);
        Directory.CreateDirectory(outside.Path);
        File.WriteAllText(System.IO.Path.Combine(outside.Path, "sentinel"), "unchanged");
        var linkedRoot = System.IO.Path.Combine(root.Path, "escape");
        Directory.CreateSymbolicLink(linkedRoot, outside.Path);
        await Assert.ThrowsAsync<BootstrapException>(() => new SqlitePlatformStore(linkedRoot).InitializeAsync());
        Assert.Equal("unchanged", File.ReadAllText(System.IO.Path.Combine(outside.Path, "sentinel")));
    }
}
