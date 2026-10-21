using System.Runtime.InteropServices;

namespace Kofte.Host.Bootstrap;

public sealed class StoreLease : IDisposable
{
    private readonly FileStream stream;

    private StoreLease(FileStream stream) => this.stream = stream;

    public static StoreLease Acquire(string root)
    {
        if (OperatingSystem.IsWindows())
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        UnixStorePaths.ValidateRoot(root, allowMissing: false);
        var path = Path.Combine(root, ".platform.lock");
        UnixStorePaths.ValidateFile(path, allowMissing: true);
        var stream = new FileStream(path, new FileStreamOptions
        {
            Mode = System.IO.FileMode.OpenOrCreate,
            Access = FileAccess.ReadWrite,
            Share = FileShare.ReadWrite,
            UnixCreateMode = UnixStorePaths.FileMode
        });
        if (Flock(stream.SafeFileHandle.DangerousGetHandle().ToInt32(), 2 | 4) == 0)
            return new StoreLease(stream);
        stream.Dispose();
        throw new BootstrapException(BootstrapError.StoreBusy);
    }

    public void Dispose() => stream.Dispose();

    [DllImport("libc", EntryPoint = "flock", SetLastError = true)]
    private static extern int Flock(int descriptor, int operation);
}
