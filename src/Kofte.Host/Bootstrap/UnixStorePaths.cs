using Paths = Kofte.Infrastructure.Storage.UnixStorePaths;
using Kofte.Infrastructure.Storage;

namespace Kofte.Host.Bootstrap;

internal static class UnixStorePaths
{
    internal const UnixFileMode DirectoryMode = Paths.DirectoryMode;
    internal const UnixFileMode FileMode = Paths.FileMode;

    internal static void ValidateRoot(string root, bool allowMissing)
    {
        try { Paths.ValidateRoot(root, allowMissing); }
        catch (StorePathException error) { throw Translate(error); }
    }

    internal static bool ValidateFile(string path, bool allowMissing = false)
    {
        try { return Paths.ValidateFile(path, allowMissing); }
        catch (StorePathException error) { throw Translate(error); }
    }

    internal static BootstrapException Translate(StorePathException error) => new(error.Code switch
    {
        StorePathError.ConfigurationInvalid => BootstrapError.ConfigurationInvalid,
        StorePathError.StorageMissing => BootstrapError.StorageMissing,
        StorePathError.StorageInvalid => BootstrapError.StorageInvalid,
        StorePathError.StoreBusy => BootstrapError.StoreBusy,
        _ => BootstrapError.StorageUnavailable
    });
}
