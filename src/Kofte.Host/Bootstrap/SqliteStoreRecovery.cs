using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;

namespace Kofte.Host.Bootstrap;

public sealed partial class SqlitePlatformStore
{
    private sealed record BackupManifest(string Format, string SourceSha256, string BackupSha256);
    private const string BackupFormat = "kofte-sqlite-backup-v1";
    private static readonly JsonSerializerOptions ManifestOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true
    };

    private static string RecoveryPath(string value)
    {
        if (value.Split(Path.DirectorySeparatorChar).Contains(".."))
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        try
        {
            var path = Path.GetFullPath(value);
            if (Path.GetFileName(path).Length == 0)
                throw new BootstrapException(BootstrapError.ConfigurationInvalid);
            return path;
        }
        catch (ArgumentException) { throw new BootstrapException(BootstrapError.ConfigurationInvalid); }
    }

    private static void PrivateDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        UnixStorePaths.ValidateRoot(path, allowMissing: true);
        Directory.CreateDirectory(path, UnixStorePaths.DirectoryMode);
        UnixStorePaths.ValidateRoot(path, allowMissing: false);
    }

    private static void CreatePrivateFile(string path)
    {
        if (OperatingSystem.IsWindows())
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        using var file = new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            UnixCreateMode = UnixStorePaths.FileMode
        });
    }

    private static string Hash(string path)
    {
        UnixStorePaths.ValidateFile(path);
        using var file = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(file));
    }

    private static SqliteConnection OpenRecoveryFile(string path, bool readOnly)
    {
        UnixStorePaths.ValidateRoot(Path.GetDirectoryName(path)!, allowMissing: false);
        UnixStorePaths.ValidateFile(path);
        if (readOnly) ValidateSqliteHeader(path);
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite,
            Pooling = false,
            ForeignKeys = true,
            DefaultTimeout = 1
        }.ToString());
        try
        {
            connection.Open();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static void CheckIntegrity(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA quick_check";
        using var result = command.ExecuteReader();
        if (!result.Read() || result.GetString(0) != "ok" || result.Read())
            throw new BootstrapException(BootstrapError.StorageInvalid);
    }

    private static void NoSidecars(string path)
    {
        foreach (var suffix in new[] { "-journal", "-wal", "-shm" })
            if (UnixStorePaths.ValidateFile(path + suffix, allowMissing: true))
                throw new BootstrapException(BootstrapError.StoreBusy);
    }

    private static void RejectModuleEvidence(string root)
    {
        var modules = Path.Combine(root, "modules");
        UnixStorePaths.ValidateRoot(modules, allowMissing: true);
        if (Directory.Exists(modules) && Directory.EnumerateFileSystemEntries(modules).Any())
            throw new BootstrapException(BootstrapError.StorageInvalid);
    }

    private static void RequireLegacyStore(SqliteConnection connection)
    {
        CheckIntegrity(connection);
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version";
        if (command.ExecuteScalar() is not 0L)
            throw new BootstrapException(BootstrapError.StorageInvalid);
        command.CommandText = """
            SELECT name FROM sqlite_master
            WHERE lower(name) GLOB '__kofte_*' OR lower(name) GLOB '__efmigrations*'
            """;
        if (command.ExecuteScalar() is not null)
            throw new BootstrapException(BootstrapError.StorageInvalid);
    }

    private static void RejectRecoveryLocationEvidence(string path)
    {
        var directory = Path.GetDirectoryName(path)!;
        RejectModuleEvidence(directory);
        var root = Path.GetFileName(directory) == "platform" ? Path.GetDirectoryName(directory)! : directory;
        RejectModuleEvidence(root);
        var platform = Path.Combine(root, "platform", "platform.db");
        UnixStorePaths.ValidateRoot(Path.GetDirectoryName(platform)!, allowMissing: true);
        if (UnixStorePaths.ValidateFile(platform, allowMissing: true))
        {
            NoSidecars(platform);
            using var existing = OpenRecoveryFile(platform, readOnly: true);
            RequireLegacyStore(existing);
        }
    }

    private static BackupManifest ValidateBackup(string path)
    {
        UnixStorePaths.ValidateRoot(Path.GetDirectoryName(path)!, allowMissing: false);
        UnixStorePaths.ValidateFile(path);
        var metadata = path + ".metadata.json";
        UnixStorePaths.ValidateFile(metadata);
        BackupManifest manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<BackupManifest>(File.ReadAllText(metadata), ManifestOptions)
                ?? throw new BootstrapException(BootstrapError.StorageInvalid);
        }
        catch (JsonException) { throw new BootstrapException(BootstrapError.StorageInvalid); }
        if (manifest.Format != BackupFormat ||
            !Regex.IsMatch(manifest.SourceSha256 ?? "", @"\A[0-9a-f]{64}\z") ||
            !Regex.IsMatch(manifest.BackupSha256 ?? "", @"\A[0-9a-f]{64}\z") ||
            Hash(path) != manifest.BackupSha256)
            throw new BootstrapException(BootstrapError.StorageInvalid);
        NoSidecars(path);
        RejectRecoveryLocationEvidence(path);
        using var connection = OpenRecoveryFile(path, readOnly: true);
        RequireLegacyStore(connection);
        return manifest;
    }

    public void Backup(string output)
    {
        var destination = RecoveryPath(output);
        var metadata = destination + ".metadata.json";
        using var lease = StoreLease.Acquire(Root);
        RejectModuleEvidence(Root);
        RejectRecoveryLocationEvidence(destination);
        NoSidecars(DatabasePath);
        using var source = Open();
        RequireLegacyStore(source);
        var sourceHash = Hash(DatabasePath);
        PrivateDirectory(Path.GetDirectoryName(destination)!);
        if (UnixStorePaths.ValidateFile(destination, allowMissing: true) ||
            UnixStorePaths.ValidateFile(metadata, allowMissing: true))
            throw new BootstrapException(BootstrapError.DestinationExists);
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        var temporaryMetadata = temporary + ".metadata.json";
        try
        {
            CreatePrivateFile(temporary);
            using (var target = OpenRecoveryFile(temporary, readOnly: false))
            {
                source.BackupDatabase(target);
                CheckIntegrity(target);
            }
            var manifest = new BackupManifest(BackupFormat, sourceHash, Hash(temporary));
            if (Hash(DatabasePath) != sourceHash)
                throw new BootstrapException(BootstrapError.StoreBusy);
            CreatePrivateFile(temporaryMetadata);
            File.WriteAllText(temporaryMetadata, JsonSerializer.Serialize(manifest, ManifestOptions));
            File.Move(temporary, destination, overwrite: false);
            File.Move(temporaryMetadata, metadata, overwrite: false);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
            if (File.Exists(temporaryMetadata)) File.Delete(temporaryMetadata);
        }
    }

    public void Restore(string input, bool confirmReplacement = false, string? previousBackup = null)
    {
        var backup = RecoveryPath(input);
        UnixStorePaths.ValidateRoot(Root, allowMissing: true);
        ValidateBackup(backup);
        if (backup == DatabasePath)
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        PrivateDirectory(Root);
        using var lease = StoreLease.Acquire(Root);
        RejectModuleEvidence(Root);
        var directory = Path.GetDirectoryName(DatabasePath)!;
        PrivateDirectory(directory);
        NoSidecars(DatabasePath);
        var existing = UnixStorePaths.ValidateFile(DatabasePath, allowMissing: true);
        string? previousHash = null;
        if (existing)
        {
            using (var target = OpenRecoveryFile(DatabasePath, readOnly: true))
                RequireLegacyStore(target);
            if (!confirmReplacement || previousBackup is null)
                throw new BootstrapException(BootstrapError.ConfirmationRequired);
            var prior = RecoveryPath(previousBackup);
            if (prior == DatabasePath)
                throw new BootstrapException(BootstrapError.ConfirmationRequired);
            previousHash = ValidateBackup(prior).SourceSha256;
            if (Hash(DatabasePath) != previousHash)
                throw new BootstrapException(BootstrapError.ConfirmationRequired);
        }
        else if (confirmReplacement || previousBackup is not null)
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        var temporary = Path.Combine(directory, ".restore-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            CreatePrivateFile(temporary);
            using (var source = OpenRecoveryFile(backup, readOnly: true))
            using (var target = OpenRecoveryFile(temporary, readOnly: false))
            {
                source.BackupDatabase(target);
                CheckIntegrity(target);
            }
            UnixStorePaths.ValidateRoot(directory, allowMissing: false);
            UnixStorePaths.ValidateFile(DatabasePath, allowMissing: !existing);
            if (existing && Hash(DatabasePath) != previousHash)
                throw new BootstrapException(BootstrapError.StoreBusy);
            File.Move(temporary, DatabasePath, overwrite: existing);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public void Reset(bool confirmed)
    {
        if (!confirmed) throw new BootstrapException(BootstrapError.ConfirmationRequired);
        using var lease = StoreLease.Acquire(Root);
        RejectModuleEvidence(Root);
        UnixStorePaths.ValidateRoot(Path.GetDirectoryName(DatabasePath)!, allowMissing: false);
        NoSidecars(DatabasePath);
        if (UnixStorePaths.ValidateFile(DatabasePath, allowMissing: true))
        {
            using var existing = OpenRecoveryFile(DatabasePath, readOnly: true);
            RequireLegacyStore(existing);
        }
        var files = new[] { DatabasePath, DatabasePath + "-journal", DatabasePath + "-wal", DatabasePath + "-shm" }
            .Where(path => UnixStorePaths.ValidateFile(path, allowMissing: true)).ToArray();
        if (files.Length == 0) throw new BootstrapException(BootstrapError.StorageMissing);
        foreach (var file in files) File.Delete(file);
    }
}
