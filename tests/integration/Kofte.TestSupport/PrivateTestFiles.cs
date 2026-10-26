using Kofte.Infrastructure.Storage;

namespace Kofte.TestSupport;

public static class PrivateTestFiles
{
    public static void WriteNew(string path, ReadOnlySpan<byte> bytes)
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        UnixStorePaths.ValidateRoot(Path.GetDirectoryName(path)!, allowMissing: false);
        UnixStorePaths.ValidateFile(path, allowMissing: true);
        var temporary = path + ".writing-" + Guid.NewGuid().ToString("N");
        using (var file = new FileStream(temporary, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            UnixCreateMode = UnixStorePaths.FileMode
        }))
        {
            file.Write(bytes);
            file.Flush(flushToDisk: true);
        }
        UnixStorePaths.ValidateFile(temporary);
        File.Move(temporary, path, overwrite: false);
        UnixStorePaths.ValidateFile(path);
    }
}
