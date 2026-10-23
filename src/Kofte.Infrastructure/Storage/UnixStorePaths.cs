using System.Runtime.InteropServices;

namespace Kofte.Infrastructure.Storage;

public static class UnixStorePaths
{
    public const UnixFileMode DirectoryMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    public const UnixFileMode FileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    private const uint TypeMask = 0xf000, DirectoryType = 0x4000, RegularType = 0x8000;

    public static void ValidateRoot(string root, bool allowMissing)
    {
        if (!Path.IsPathFullyQualified(root) || root != Path.GetFullPath(root) ||
            root == Path.GetPathRoot(root))
            throw new StorePathException(StorePathError.ConfigurationInvalid);
        var current = Path.GetPathRoot(root)!;
        foreach (var part in root[current.Length..].Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, part);
            var status = Inspect(current);
            if (status is null)
            {
                if (allowMissing) continue;
                throw new StorePathException(StorePathError.StorageMissing);
            }
            if ((status.Value.Mode & TypeMask) != DirectoryType)
                throw new StorePathException(StorePathError.StorageInvalid);
        }
        if (Inspect(root) is { } owned) RequirePrivate(owned, true);
    }

    public static bool ValidateFile(string path, bool allowMissing = false)
    {
        var status = Inspect(path);
        if (status is null)
        {
            if (allowMissing) return false;
            throw new StorePathException(StorePathError.StorageMissing);
        }
        if ((status.Value.Mode & TypeMask) != RegularType || status.Value.Links != 1)
            throw new StorePathException(StorePathError.StorageInvalid);
        RequirePrivate(status.Value, false);
        return true;
    }

    private static void RequirePrivate((uint Mode, uint Owner, ulong Links) status, bool directory)
    {
        var required = (uint)(directory ? DirectoryMode : FileMode);
        if (status.Owner != GetEffectiveUserId() || (status.Mode & 0xfff) != required)
            throw new StorePathException(StorePathError.StorageInvalid);
    }

    private static (uint Mode, uint Owner, ulong Links)? Inspect(string path)
    {
        int result;
        (uint Mode, uint Owner, ulong Links) status;
        if (OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            result = MacLstat(path, out var value);
            status = (value.Mode, value.User, value.Links);
        }
        else if (OperatingSystem.IsLinux() && RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            result = LinuxLstat(path, out var value);
            status = (value.Mode, value.User, value.Links);
        }
        else throw new StorePathException(StorePathError.ConfigurationInvalid);
        if (result == 0) return status;
        if (Marshal.GetLastPInvokeError() == 2) return null;
        throw new StorePathException(StorePathError.StorageUnavailable);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Timespec { public long Seconds, Nanoseconds; }

    // These are the supported OS C ABIs, not .NET's private FileStatus layout.
    [StructLayout(LayoutKind.Sequential)]
    private struct MacStat
    {
        public int Device;
        public ushort Mode, Links;
        public ulong Inode;
        public uint User, Group;
        public int SpecialDevice;
        public Timespec Access, Modification, Change, Birth;
        public long Size, Blocks;
        public int BlockSize;
        public uint Flags, Generation;
        public int Spare;
        public long Reserved0, Reserved1;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LinuxStat
    {
        public ulong Device, Inode, Links;
        public uint Mode, User, Group;
        public int Padding;
        public ulong SpecialDevice;
        public long Size, BlockSize, Blocks;
        public Timespec Access, Modification, Change;
        public long Reserved0, Reserved1, Reserved2;
    }

    [DllImport("libc", EntryPoint = "lstat", SetLastError = true)]
    private static extern int MacLstat([MarshalAs(UnmanagedType.LPUTF8Str)] string path, out MacStat status);

    [DllImport("libc", EntryPoint = "lstat", SetLastError = true)]
    private static extern int LinuxLstat([MarshalAs(UnmanagedType.LPUTF8Str)] string path, out LinuxStat status);

    [DllImport("libc", EntryPoint = "geteuid")]
    private static extern uint GetEffectiveUserId();
}
