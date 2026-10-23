using System.Runtime.InteropServices;

namespace Kofte.Infrastructure.Storage;

public sealed class StoreLease : IDisposable
{
    private readonly FileStream stream;

    private StoreLease(FileStream stream) => this.stream = stream;

    public static StoreLease Acquire(string root)
    {
        if (OperatingSystem.IsWindows())
            throw new StorePathException(StorePathError.ConfigurationInvalid);
        UnixStorePaths.ValidateRoot(root, allowMissing: false);
        var path = Path.Combine(root, ".platform.lock");
        UnixStorePaths.ValidateFile(path, allowMissing: true);
        FileStream stream;
        try
        {
            stream = new FileStream(path, new FileStreamOptions
            {
                Mode = System.IO.FileMode.OpenOrCreate,
                Access = FileAccess.ReadWrite,
                Share = FileShare.ReadWrite,
                UnixCreateMode = UnixStorePaths.FileMode
            });
        }
        // .NET's Unix sharing lock can refuse the open before our flock.
        catch (IOException error) when (error.HResult is 11 or 35)
        {
            throw new StorePathException(StorePathError.StoreBusy);
        }
        if (Flock(stream.SafeFileHandle.DangerousGetHandle().ToInt32(), 2 | 4) == 0)
            return new StoreLease(stream);
        var failure = Marshal.GetLastPInvokeError();
        stream.Dispose();
        throw new StorePathException(failure is 11 or 35
            ? StorePathError.StoreBusy : StorePathError.StorageUnavailable);
    }

    public void Dispose() => stream.Dispose();

    [DllImport("libc", EntryPoint = "flock", SetLastError = true)]
    private static extern int Flock(int descriptor, int operation);
}
