using System.Runtime.InteropServices;

namespace Kofte.Host.Features.ModuleComposition;

internal static class CompositionProcessGroup
{
    internal static void Enter()
    {
        if (OperatingSystem.IsWindows() || setpgid(0, 0) != 0 || getpgid(Environment.ProcessId) != Environment.ProcessId)
            throw new CompositionBoundaryException("composition_admission_invalid");
    }

    internal static bool IsOwned(int pid) => getpgid(pid) == pid;

    internal static void Kill(int pid)
    {
        if (kill(-pid, 9) != 0 && Marshal.GetLastPInvokeError() != 3)
            throw new CompositionBoundaryException("composition_cleanup_incomplete");
    }

    internal static bool HasExited(int pid)
    {
        if (kill(-pid, 0) == 0) return false;
        if (Marshal.GetLastPInvokeError() == 3) return true;
        throw new CompositionBoundaryException("composition_cleanup_incomplete");
    }

    [DllImport("libc", SetLastError = true)]
    private static extern int setpgid(int pid, int pgid);
    [DllImport("libc", SetLastError = true)]
    private static extern int getpgid(int pid);
    [DllImport("libc", SetLastError = true)]
    private static extern int kill(int pid, int signal);
}
