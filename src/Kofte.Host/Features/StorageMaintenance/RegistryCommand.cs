using Kofte.Host.Bootstrap;
using System.Diagnostics;
using System.Text.Json;

namespace Kofte.Host.Features.StorageMaintenance;

public static class RegistryCommand
{
    public static async Task RunAsync(string[] args, CancellationToken token = default)
    {
        if (args is not ["--registry", "upgrade"])
            throw new BootstrapException(BootstrapError.CommandInvalid);
        var config = StorageMaintenanceConfiguration.FromEnvironment();
        await MaintenanceSupervisor.RunUpgradeAsync(config, token);
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            eventCode = "registry_upgraded",
            component = "storage",
            correlationId = ActivityTraceId.CreateRandom().ToHexString()
        }));
    }
}
