using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Storage.Consumer.Scenarios;

internal static class RegistrationScenarios
{
    internal static async Task<int> RunAsync(StorageInstallation installation, IStorageMaintenanceScope scope,
        string scenario, CancellationToken token)
    {
        IStorageRegistry registry = new StorageRegistry(installation);
        var owner = Module("org.example.public-registration", scenario != "no-storage");
        if (scenario == "no-storage")
        {
            Require(await registry.RegisterAsync(owner, scope, token) is StorageResult.NoStorage);
            Require(await registry.ResolveAsync(owner.Id, token) is StorageResult.NoStorage);
            return 2;
        }
        var first = Ready(await registry.RegisterAsync(owner, scope, token)).Assignment;
        Require(first.Owner.Equals(owner.Id) && first.Provider == installation.Provider);
        if (scenario == "corrected-retry")
        {
            var conflict = await registry.RegisterAsync(Module(owner.Id.Value, relational: false), scope, token);
            Require(conflict.Outcome == StorageOutcome.Rejected &&
                conflict.Diagnostics.Single().Code == "storage_conflict");
        }
        var changed = Module(owner.Id.Value, version: "2.0.0", display: "Renamed Köfte consumer");
        var retry = Ready(await registry.RegisterAsync(changed, scope, token)).Assignment;
        var resolved = Ready(await registry.ResolveAsync(owner.Id, token)).Assignment;
        Require(first.AllocationId == retry.AllocationId && first.AllocationKey == retry.AllocationKey &&
            first.InstallationId == retry.InstallationId && first.Revision == retry.Revision);
        Require(first.AllocationId == resolved.AllocationId && first.AllocationKey == resolved.AllocationKey);
        return scenario == "corrected-retry" ? 4 : 3;
    }

    internal static ModuleDescription Module(string id, bool relational = true, string version = "1.0.0",
        string display = "Köfte independent consumer")
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            id,
            version,
            displayName = display,
            hostContract = "1.0.0",
            capabilities = Array.Empty<object>(),
            dependencies = Array.Empty<object>(),
            permissions = Array.Empty<object>(),
            storage = new { relational }
        });
        return ManifestValidator.Validate(bytes, new ManifestValidationContext("1.0.0"))
            is ManifestValidationResult.Accepted accepted ? accepted.Description :
            throw new InvalidOperationException("storage_consumer_manifest_invalid");
    }

    internal static StorageResult.Ready Ready(StorageResult result) =>
        result as StorageResult.Ready ?? throw new InvalidOperationException("storage_consumer_not_ready");

    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException("storage_consumer_assertion_failed");
    }
}
