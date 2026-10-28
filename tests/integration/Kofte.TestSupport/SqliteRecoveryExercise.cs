using System.Security.Cryptography;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Microsoft.Data.Sqlite;

namespace Kofte.TestSupport;

public static class SqliteRecoveryExercise
{
    public static async Task<RecoveryManifest> BackupAsync(string sourceRoot, string recoveryRoot,
        CancellationToken token = default)
    {
        var installation = StorageInstallation.Sqlite(sourceRoot);
        var maintenance = new StorageMaintenance(installation);
        await using var scope = await maintenance.AcquireAsync(token);
        var inventory = await RecoverySetVerifier.InspectStoppedAsync(installation, scope, token);
        var identity = inventory.RegistryVersion == 2
            ? await IdentityRecoverySnapshot.CaptureStoppedAsync(installation, scope, token) : null;
        CreateFreshDirectory(recoveryRoot);
        var artifacts = new List<RecoveryArtifact>();
        foreach (var name in inventory.Allocations.Where(row => row.Relational)
            .Select(row => $"modules/{row.AllocationKey}/module.db").Prepend("platform/platform.db"))
        {
            token.ThrowIfCancellationRequested();
            var source = Path.Combine(sourceRoot, name);
            var before = Digest(source);
            BackupFile(source, Path.Combine(recoveryRoot, name));
            if (Digest(source) != before) throw new InvalidOperationException("recovery_source_changed");
            artifacts.Add(await RecoverySetVerifier.DescribeArtifactAsync(recoveryRoot, name, token));
        }
        var manifest = new RecoveryManifest(identity is null ? RecoveryManifest.CurrentFormat : RecoveryManifest.IdentityFormat,
            true, Guid.NewGuid(),
            inventory.InstallationId, inventory.Provider, inventory.RegistryVersion,
            inventory.RegistryRevision, inventory.Allocations, artifacts, identity);
        await RecoverySetVerifier.WriteManifestAsync(recoveryRoot, manifest, token);
        await RecoverySetVerifier.VerifyAsync(recoveryRoot, cancellationToken: token);
        var after = await RecoverySetVerifier.InspectStoppedAsync(installation, scope, token);
        if (!RecoverySetVerifier.SameInventory(manifest, after) ||
            identity is not null && await IdentityRecoverySnapshot.CaptureStoppedAsync(installation, scope, token) != identity)
            throw new InvalidOperationException("recovery_source_changed");
        return manifest;
    }

    public static Task<RecoveryManifest> RestoreFreshAsync(string recoveryRoot, string targetRoot,
        CancellationToken token = default) => RestoreCoreAsync(recoveryRoot, targetRoot, null, token);

    public static Task<RecoveryManifest> RestoreFreshQuarantinedAsync(string recoveryRoot, string targetRoot,
        string keyDirectory, CancellationToken token = default) =>
        RestoreCoreAsync(recoveryRoot, targetRoot, keyDirectory, token);

    private static async Task<RecoveryManifest> RestoreCoreAsync(string recoveryRoot, string targetRoot,
        string? keyDirectory, CancellationToken token)
    {
        var manifest = await RecoverySetVerifier.VerifyAsync(recoveryRoot, cancellationToken: token);
        if (manifest.Provider != "sqlite") throw new InvalidOperationException("recovery_provider_invalid");
        if (manifest.Identity is not null && keyDirectory is null)
            throw new InvalidOperationException("identity_restore_reconciliation_required");
        var quarantine = keyDirectory is null ? null : await IdentityRestoreQuarantine.CreateAsync(
            recoveryRoot, keyDirectory, manifest, targetRoot, token);
        CreateFreshDirectory(targetRoot);
        using var lease = StoreLease.Acquire(targetRoot);
        foreach (var artifact in manifest.Artifacts)
        {
            token.ThrowIfCancellationRequested();
            BackupFile(Path.Combine(recoveryRoot, artifact.Name), Path.Combine(targetRoot, artifact.Name));
            // Backup API outputs can differ physically; manifest hashes describe the restored files.
        }
        var restoredArtifacts = new List<RecoveryArtifact>();
        foreach (var artifact in manifest.Artifacts)
            restoredArtifacts.Add(await RecoverySetVerifier.DescribeArtifactAsync(targetRoot, artifact.Name, token));
        var restored = manifest with { Artifacts = restoredArtifacts };
        await RecoverySetVerifier.WriteManifestAsync(targetRoot, restored, token);
        await RecoverySetVerifier.VerifyAsync(targetRoot, cancellationToken: token);
        if (quarantine is not null) await quarantine.MarkRestoredAsync(restored, token);
        return restored;
    }

    private static string Digest(string path)
    {
        using var file = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(file));
    }

    internal static void CreateFreshDirectory(string path)
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("recovery_platform_unsupported");
        UnixStorePaths.ValidateRoot(path, allowMissing: true);
        if (Directory.Exists(path) || File.Exists(path))
            throw new InvalidOperationException("recovery_fresh_target_required");
        Directory.CreateDirectory(path, UnixStorePaths.DirectoryMode);
        UnixStorePaths.ValidateRoot(path, allowMissing: false);
    }

    private static void BackupFile(string source, string target)
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("recovery_platform_unsupported");
        UnixStorePaths.ValidateRoot(Path.GetDirectoryName(source)!, allowMissing: false);
        UnixStorePaths.ValidateFile(source);
        foreach (var suffix in new[] { "-journal", "-wal", "-shm" })
            if (UnixStorePaths.ValidateFile(source + suffix, allowMissing: true))
                throw new InvalidOperationException("recovery_sidecar_present");
        EnsurePrivateDirectory(Path.GetDirectoryName(target)!);
        UnixStorePaths.ValidateRoot(Path.GetDirectoryName(target)!, allowMissing: false);
        using (var file = new FileStream(target, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.ReadWrite,
            Share = FileShare.None,
            UnixCreateMode = UnixStorePaths.FileMode
        })) { }
        using var from = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = source,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
            DefaultTimeout = 1
        }.ToString());
        using var to = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = target,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false,
            DefaultTimeout = 1
        }.ToString());
        from.Open();
        to.Open();
        from.BackupDatabase(to);
        using var check = to.CreateCommand();
        check.CommandText = "PRAGMA integrity_check";
        if (check.ExecuteScalar() is not "ok") throw new InvalidOperationException("recovery_integrity_invalid");
    }

    private static void EnsurePrivateDirectory(string path)
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("recovery_platform_unsupported");
        if (!Directory.Exists(path))
        {
            EnsurePrivateDirectory(Path.GetDirectoryName(path)!);
            Directory.CreateDirectory(path, UnixStorePaths.DirectoryMode);
        }
        UnixStorePaths.ValidateRoot(path, allowMissing: false);
    }
}
