using Kofte.Infrastructure.Storage.Allocation;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Storage.Sqlite;

internal sealed class ModuleStorePaths(StorageInstallation installation, string key)
{
    internal string DirectoryPath => Path.Combine(installation.Root!, "modules", key);
    internal string DatabasePath => Path.Combine(DirectoryPath, "module.db");

    internal bool Exists()
    {
        ValidateAncestors(allowMissing: true);
        return UnixStorePaths.ValidateFile(DatabasePath, allowMissing: true);
    }

    internal void ValidateAncestors(bool allowMissing)
    {
        StorageAllocator.Validate(key);
        if (installation.Root is null) throw new StorageException(StorageCode.RequestInvalid);
        UnixStorePaths.ValidateRoot(installation.Root, allowMissing: false);
        UnixStorePaths.ValidateRoot(Path.Combine(installation.Root, "modules"), allowMissing);
        UnixStorePaths.ValidateRoot(DirectoryPath, allowMissing);
        foreach (var suffix in new[] { "-journal", "-wal", "-shm" })
            UnixStorePaths.ValidateFile(DatabasePath + suffix, allowMissing: true);
    }

    internal void Create()
    {
        if (OperatingSystem.IsWindows()) throw new StorageException(StorageCode.PathRejected);
        ValidateAncestors(allowMissing: true);
        if (Directory.Exists(DirectoryPath) || File.Exists(DatabasePath))
            throw new StorageException(StorageCode.ReconciliationRequired);
        Directory.CreateDirectory(Path.Combine(installation.Root!, "modules"), UnixStorePaths.DirectoryMode);
        Directory.CreateDirectory(DirectoryPath, UnixStorePaths.DirectoryMode);
        StorageDiagnostics.Checkpoint("sqlite_directory_created", installation.Provider);
        ValidateAncestors(allowMissing: false);
        using (var file = new FileStream(DatabasePath, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.ReadWrite,
            Share = FileShare.None,
            UnixCreateMode = UnixStorePaths.FileMode
        })) { }
        StorageDiagnostics.Checkpoint("sqlite_file_created", installation.Provider);
    }
}
