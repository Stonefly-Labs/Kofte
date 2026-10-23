using Kofte.Host.Bootstrap;
using Kofte.Infrastructure.Storage;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Xunit;
using StoreLease = Kofte.Host.Bootstrap.StoreLease;
using UnixStorePaths = Kofte.Host.Bootstrap.UnixStorePaths;

namespace Kofte.Host.IntegrationTests;

[Trait("Provider", "sqlite")]
[Trait("Suite", "recovery")]
public sealed class StorageRecoveryCompatibilityTests
{
    [Fact]
    public async Task RegistrySourcesAndDestinationsRejectAllLegacyDestructionIncludingConfirmedReset()
    {
        using var registryRoot = new TempRoot();
        using var legacyRoot = new TempRoot();
        using var backups = new TempRoot();
        var registry = new SqlitePlatformStore(registryRoot.Path);
        var legacy = new SqlitePlatformStore(legacyRoot.Path);
        await registry.InitializeAsync();
        await legacy.InitializeAsync();
        var good = Path.Combine(backups.Path, "legacy.db");
        legacy.Backup(good);
        var maintenance = new StorageMaintenance(StorageInstallation.Sqlite(registryRoot.Path));
        await using (var scope = await maintenance.AcquireAsync(TestContext.Current.CancellationToken))
            await maintenance.UpgradeAsync(scope, TestContext.Current.CancellationToken);
        var before = await File.ReadAllBytesAsync(registry.DatabasePath, TestContext.Current.CancellationToken);
        Assert.Throws<BootstrapException>(() => registry.Backup(Path.Combine(backups.Path, "registry.db")));
        Assert.Throws<BootstrapException>(() => registry.Restore(good, true, good));
        Assert.Throws<BootstrapException>(() => registry.Reset(true));
        await Assert.ThrowsAsync<BootstrapException>(() => registry.InitializeAsync());
        Assert.Equal(before, await File.ReadAllBytesAsync(registry.DatabasePath, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(Path.Combine(backups.Path, "registry.db")));
        Assert.Throws<BootstrapException>(() => legacy.Backup(
            Path.Combine(registryRoot.Path, "platform", "other.db")));
    }

    [Theory]
    [InlineData("PRAGMA user_version=2")]
    [InlineData("CREATE TABLE __kofte_registry_migrations(version INTEGER)")]
    [InlineData("CREATE TABLE __EFMigrationsHistory(MigrationId TEXT)")]
    [InlineData("CREATE TABLE __kofte_storage_owner(marker_version INTEGER)")]
    public async Task UnsupportedHistoryAndOwnerEvidenceRefuseBackupResetAndRestoreWithoutWrites(string mutation)
    {
        using var root = new TempRoot();
        using var legacyRoot = new TempRoot();
        using var backups = new TempRoot();
        var store = new SqlitePlatformStore(root.Path);
        var legacy = new SqlitePlatformStore(legacyRoot.Path);
        await store.InitializeAsync();
        await legacy.InitializeAsync();
        var backup = Path.Combine(backups.Path, "v0.db");
        legacy.Backup(backup);
        using (var connection = store.Open())
        using (var command = connection.CreateCommand())
        {
            command.CommandText = mutation;
            command.ExecuteNonQuery();
        }
        var before = await File.ReadAllBytesAsync(store.DatabasePath, TestContext.Current.CancellationToken);
        Assert.Throws<BootstrapException>(() => store.Backup(Path.Combine(backups.Path, "blocked.db")));
        Assert.Throws<BootstrapException>(() => store.Restore(backup, true, backup));
        Assert.Throws<BootstrapException>(() => store.Reset(true));
        Assert.Equal(before, await File.ReadAllBytesAsync(store.DatabasePath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MissingPlatformDoesNotPermitOrphaningModuleEvidence()
    {
        if (OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        using var root = new TempRoot();
        using var legacyRoot = new TempRoot();
        using var backups = new TempRoot();
        var store = new SqlitePlatformStore(root.Path);
        var legacy = new SqlitePlatformStore(legacyRoot.Path);
        await legacy.InitializeAsync();
        var backup = Path.Combine(backups.Path, "v0.db");
        legacy.Backup(backup);
        Directory.CreateDirectory(root.Path, UnixStorePaths.DirectoryMode);
        Directory.CreateDirectory(Path.Combine(root.Path, "modules"), UnixStorePaths.DirectoryMode);
        var sentinel = Path.Combine(root.Path, "modules", "unowned");
        await File.WriteAllTextAsync(sentinel, "private-preserved", TestContext.Current.CancellationToken);
        File.SetUnixFileMode(sentinel, UnixStorePaths.FileMode);
        await Assert.ThrowsAsync<BootstrapException>(() => store.InitializeAsync());
        Assert.Throws<BootstrapException>(() => store.Backup(Path.Combine(backups.Path, "blocked.db")));
        Assert.Throws<BootstrapException>(() => store.Restore(backup));
        Assert.Throws<BootstrapException>(() => store.Reset(true));
        Assert.Equal("private-preserved", await File.ReadAllTextAsync(sentinel, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(store.DatabasePath));
        using var lease = StoreLease.Acquire(root.Path);
    }

    [Fact]
    public async Task RegistryBearingInputIsNotAuthorizedByOtherwiseValidLegacyMetadata()
    {
        using var sourceRoot = new TempRoot();
        using var backupRoot = new TempRoot();
        using var freshTarget = new TempRoot();
        var source = new SqlitePlatformStore(sourceRoot.Path);
        await source.InitializeAsync();
        var backup = Path.Combine(backupRoot.Path, "disguised.db");
        source.Backup(backup);
        var maintenance = new StorageMaintenance(StorageInstallation.Sqlite(sourceRoot.Path));
        await using (var scope = await maintenance.AcquireAsync(TestContext.Current.CancellationToken))
            await maintenance.UpgradeAsync(scope, TestContext.Current.CancellationToken);
        File.Copy(source.DatabasePath, backup, overwrite: true);
        var metadata = JsonNode.Parse(await File.ReadAllTextAsync(backup + ".metadata.json",
            TestContext.Current.CancellationToken))!;
        using (var file = File.OpenRead(backup))
            metadata["backupSha256"] = Convert.ToHexStringLower(SHA256.HashData(file));
        await File.WriteAllTextAsync(backup + ".metadata.json", metadata.ToJsonString(),
            TestContext.Current.CancellationToken);
        var before = await File.ReadAllBytesAsync(backup, TestContext.Current.CancellationToken);
        Assert.Equal(BootstrapError.StorageInvalid, Assert.Throws<BootstrapException>(() =>
            new SqlitePlatformStore(freshTarget.Path).Restore(backup)).Code);
        Assert.False(Directory.Exists(freshTarget.Path));
        Assert.Equal(before, await File.ReadAllBytesAsync(backup, TestContext.Current.CancellationToken));
    }
}
