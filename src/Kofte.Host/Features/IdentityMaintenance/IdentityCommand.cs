using Kofte.Host.Bootstrap;
using Kofte.Host.Features.Identity;
using Kofte.Host.Features.StorageMaintenance;

namespace Kofte.Host.Features.IdentityMaintenance;

internal static class IdentityCommand
{
    internal static IdentityOperation Parse(string[] args, string prefix)
    {
        if (args.Length == 4 && args[0] == prefix && args[1] == "reconcile-restore" &&
            args[2] == "--decision-file" && PrivatePath(args[3]))
            return IdentityOperation.ReconcileRestore;
        if (args.Length == 7 && args[0] == prefix && args[1] == "bootstrap" && args[2] == "authorize" &&
            args[3] == "--nominee-file" && PrivatePath(args[4]) &&
            args[5] == "--authorization-file" && PrivatePath(args[6]))
            return IdentityOperation.BootstrapAuthorize;
        if (args.Length == 3 && args[0] == prefix && args[1] == "bootstrap" && args[2] == "disable-pending")
            return IdentityOperation.BootstrapDisablePending;
        if (args.Length != 2 || args[0] != prefix)
            throw new BootstrapException(BootstrapError.CommandInvalid);
        return args[1] switch
        {
            "prepare" => IdentityOperation.Prepare,
            "reconcile-configuration" => IdentityOperation.ReconcileConfiguration,
            "revoke-sessions" => IdentityOperation.RevokeSessions,
            "maintain" => IdentityOperation.Maintain,
            _ => throw new BootstrapException(BootstrapError.CommandInvalid)
        };
    }

    internal static async Task RunAsync(string[] args, CancellationToken token = default)
    {
        var operation = Parse(args, "--identity");
        var configuration = StorageMaintenanceConfiguration.FromEnvironment();
        var name = operation switch
        {
            IdentityOperation.BootstrapAuthorize => "bootstrap-authorize",
            IdentityOperation.BootstrapDisablePending => "bootstrap-disable-pending",
            _ => args[1]
        };
        await IdentityMaintenanceSupervisor.RunAsync(configuration, name, args[1..], token);
    }

    private static bool PrivatePath(string path) => path.Length is > 0 and <= 4096 &&
        !path.Any(char.IsControl) && Path.IsPathFullyQualified(path) && path == Path.GetFullPath(path);
}
