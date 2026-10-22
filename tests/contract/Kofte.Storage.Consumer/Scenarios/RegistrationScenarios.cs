using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.Consumer.Modules.Alpha;
using Kofte.Storage.Consumer.Modules.Beta;
using Kofte.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Kofte.Storage.Consumer.Scenarios;

internal static class RegistrationScenarios
{
    internal static int Assertions { get; private set; }
    internal static async Task<int> RunAsync(StorageInstallation installation, IStorageMaintenanceScope scope,
        string scenario, CancellationToken token)
    {
        IStorageRegistry registry = new StorageRegistry(installation);
        var owner = Module("org.example.public-registration-" + scenario, scenario != "no-storage");
        if (scenario == "no-storage")
        {
            Require(StorageResultChecks.Check(await registry.RegisterAsync(owner, scope, token)) is StorageResult.NoStorage);
            Require(StorageResultChecks.Check(await registry.ResolveAsync(owner.Id, token)) is StorageResult.NoStorage);
            return 2;
        }
        throw new InvalidOperationException("storage_scenario_invalid");
    }

    private static readonly string[] RetryOwners = ["org.example.public-retry-alpha", "org.example.public-retry-beta"];

    internal static async Task<StorageExampleEvidence> RetryAsync(StorageInstallation installation, CancellationToken token)
    {
        var registry = new StorageRegistry(installation);
        var maintenance = new StorageMaintenance(installation);
        var expected = new List<StorageAssignmentSnapshot>();
        await using (var scope = await maintenance.AcquireAsync(token))
        {
            foreach (var owner in RetryOwners)
            {
                var module = Module(owner);
                Ready(await registry.RegisterAsync(module, scope, token));
                await using var binding = await registry.BindAsync(module.Id, token);
                if (owner == RetryOwners[0])
                {
                    Ready(await binding.MigrateAsync(AlphaStorage.Create, scope, cancellationToken: token));
                    await using var context = await binding.CreateContextAsync(AlphaStorage.Create, token);
                    context.Records.Add(new Modules.Alpha.Record { Id = 1, Payload = "retry-alpha-preserved" });
                    await context.SaveChangesAsync(token);
                }
                else
                {
                    Ready(await binding.MigrateAsync(BetaStorage.Create, scope, cancellationToken: token));
                    await using var context = await binding.CreateContextAsync(BetaStorage.Create, token);
                    context.Records.Add(new Modules.Beta.Record { Id = 1, Payload = "retry-beta-preserved" });
                    await context.SaveChangesAsync(token);
                }
                var first = Ready(await registry.ResolveAsync(module.Id, token)).Assignment;
                Require(first.Owner.Equals(module.Id) && first.Provider == installation.Provider);
                expected.Add(StorageAssignmentSnapshot.From(first));
                var conflict = StorageResultChecks.Check(await registry.RegisterAsync(
                    Module(owner, relational: false), scope, token));
                Require(conflict.Outcome == StorageOutcome.Rejected &&
                    conflict.Diagnostics.Single().Code == "storage_conflict");
            }
        }
        var processes = new HashSet<int>();
        var assignments = 0;
        var sentinels = 0;
        for (var restart = 1; restart <= 3; restart++)
        {
            var observed = await StorageRestartProcess.RunAsync(typeof(Program).Assembly,
                installation.Provider == "sqlite" ? "sqlite" : "postgres", restart, Program.ConfigureChild, token);
            Require(processes.Add(observed.ProcessId));
            foreach (var pair in expected.Zip(observed.Assignments))
            {
                Require(pair.First == pair.Second);
                assignments++;
            }
            Require(observed.RetainedSentinels == 2);
            sentinels += observed.RetainedSentinels;
        }
        return new(processes.Count, assignments, sentinels, 0, 0, 0, 0);
    }

    internal static async Task<StorageRestartResult> ProbeRestartAsync(StorageInstallation installation,
        string restart, CancellationToken token)
    {
        var registry = new StorageRegistry(installation);
        var maintenance = new StorageMaintenance(installation);
        var assignments = new List<StorageAssignmentSnapshot>();
        var sentinels = 0;
        await using var scope = await maintenance.AcquireAsync(token);
        foreach (var owner in RetryOwners)
        {
            var changed = Module(owner, version: $"{restart}.1.0", display: "Renamed Köfte consumer " + restart);
            var retried = Ready(await registry.RegisterAsync(changed, scope, token)).Assignment;
            var resolved = Ready(await registry.ResolveAsync(changed.Id, token)).Assignment;
            Require(StorageAssignmentSnapshot.From(retried) == StorageAssignmentSnapshot.From(resolved));
            await using var binding = await registry.BindAsync(changed.Id, token);
            if (owner == RetryOwners[0])
            {
                await using var context = await binding.CreateContextAsync(AlphaStorage.Create, token);
                Require((await context.Records.SingleAsync(token)).Payload == "retry-alpha-preserved");
            }
            else
            {
                await using var context = await binding.CreateContextAsync(BetaStorage.Create, token);
                Require((await context.Records.SingleAsync(token)).Payload == "retry-beta-preserved");
            }
            sentinels++;
            assignments.Add(StorageAssignmentSnapshot.From(resolved));
        }
        return new(Environment.ProcessId, assignments.ToArray(), sentinels);
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

    internal static StorageResult.Ready Ready(StorageResult result)
    {
        var ready = StorageResultChecks.Check(result) as StorageResult.Ready ??
            throw new InvalidOperationException("storage_consumer_not_ready");
        Assertions++;
        return ready;
    }

    internal static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException("storage_consumer_assertion_failed");
        Assertions++;
    }
}
