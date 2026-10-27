using Kofte.Host.Bootstrap;
using Kofte.Host.Features.Identity;
using Kofte.Host.Features.StorageMaintenance;

namespace Kofte.Host.Features.IdentityMaintenance;

internal static class IdentityCommand
{
    internal static IdentityOperation Parse(string[] args, string prefix)
    {
        if (args.Length != 2 || args[0] != prefix)
            throw new BootstrapException(BootstrapError.CommandInvalid);
        return args[1] switch
        {
            "prepare" => IdentityOperation.Prepare,
            "reconcile-configuration" => IdentityOperation.ReconcileConfiguration,
            "revoke-sessions" => IdentityOperation.RevokeSessions,
            _ => throw new BootstrapException(BootstrapError.CommandInvalid)
        };
    }

    internal static async Task RunAsync(string[] args, CancellationToken token = default)
    {
        _ = Parse(args, "--identity");
        var configuration = StorageMaintenanceConfiguration.FromEnvironment();
        await IdentityMaintenanceSupervisor.RunAsync(configuration, args[1], token);
    }
}
