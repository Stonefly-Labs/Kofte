using System.Runtime.InteropServices;

namespace Kofte.Host.Bootstrap;

internal static class UnixStorePaths
{
    internal const UnixFileMode DirectoryMode =
        UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    internal const UnixFileMode FileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    private const uint TypeMask = 0xf000, DirectoryType = 0x4000, RegularType = 0x8000;

    internal static void ValidateRoot(string root, bool allowMissing)
    {
        if (!Path.IsPathFullyQualified(root) || root != Path.GetFullPath(root) ||
            root == Path.GetPathRoot(root))
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        var current = Path.GetPathRoot(root)!;
        foreach (var part in root[current.Length..].Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, part);
            var status = Inspect(current);
            if (status is null)
            {
                if (allowMissing) continue;
                throw new BootstrapException(BootstrapError.StorageMissing);
            }
            if ((status.Value.Mode & TypeMask) != DirectoryType)
                throw new BootstrapException(BootstrapError.StorageInvalid);
        }
        if (Inspect(root) is { } owned) RequirePrivate(owned, true);
    }

    internal static bool ValidateFile(string path, bool allowMissing = false)
    {
        var status = Inspect(path);
        if (status is null)
        {
            if (allowMissing) return false;
            throw new BootstrapException(BootstrapError.StorageMissing);
        }
        if ((status.Value.Mode & TypeMask) != RegularType)
            throw new BootstrapException(BootstrapError.StorageInvalid);
        RequirePrivate(status.Value, false);
        return true;
    }

    private static void RequirePrivate((uint Mode, uint Owner) status, bool directory)
    {
        var required = (uint)(directory ? DirectoryMode : FileMode);
        if (status.Owner != GetEffectiveUserId() || (status.Mode & 0xfff) != required)
            throw new BootstrapException(BootstrapError.StorageInvalid);
    }

    private static (uint Mode, uint Owner)? Inspect(string path)
    {
        int result;
        (uint Mode, uint Owner) status;
        if (OperatingSystem.IsMacOS() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
        {
            result = MacLstat(path, out var value);
            status = (value.Mode, value.User);
        }
        else if (OperatingSystem.IsLinux() && RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            result = LinuxLstat(path, out var value);
            status = (value.Mode, value.User);
        }
        else throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        if (result == 0) return status;
        if (Marshal.GetLastPInvokeError() == 2) return null;
        throw new BootstrapException(BootstrapError.StorageUnavailable);
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
