using System.Reflection;
using System.Text.Json.Nodes;
using Kofte.Host.Bootstrap;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.Consumer.Modules.Alpha;
using Kofte.Storage.Consumer.Modules.Beta;
using Kofte.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Kofte.Storage.Consumer.Scenarios;

internal static class RecoveryScenarios
{
    internal static Task<int> RunAsync(StorageInstallation installation, string scenario, CancellationToken token) =>
        scenario == "interrupted-provisioning" ? InterruptedAsync(installation, token)
            : StoppedRestoreAsync(installation.Provider == "sqlite", token);

    private static async Task<int> InterruptedAsync(StorageInstallation installation, CancellationToken token)
    {
        var sqlite = installation.Provider == "sqlite";
        var provider = sqlite ? "sqlite" : "postgres";
        var root = Environment.GetEnvironmentVariable("KOFTE_DATA_ROOT");
        var runtime = Environment.GetEnvironmentVariable("KOFTE_POSTGRES_CONNECTION");
        var registry = new StorageRegistry(installation);
        var maintenance = new StorageMaintenance(installation);
        var assertions = 0;
        foreach (var phase in StorageCrashWorker.RegistrationPhases.Concat(
            sqlite ? StorageCrashWorker.SqliteCreationPhases : []))
        {
            var owner = RegistrationScenarios.Module("org.example.public-crash-" + phase.Replace('_', '-'));
            var start = StorageCrashWorker.Start(Assembly.GetExecutingAssembly(), provider, "register",
                phase, owner.Id.Value, Program.ConfigureChild);
            await StorageCrashWorker.KillAtAsync(start, phase, token);
            var before = await StorageInspection.ReadAsync(provider, root, runtime, owner.Id, token);
            var resolved = StorageResultChecks.Check(await registry.ResolveAsync(owner.Id, token));
            RegistrationScenarios.Require(resolved.Outcome == (phase switch
            {
                "reservation_before_commit" => StorageOutcome.Rejected,
                "ready_committed" => StorageOutcome.Ready,
                _ => StorageOutcome.Incomplete
            }));
            assertions++;
            var preserved = sqlite && before is not null
                ? Directory.EnumerateFiles(Path.Combine(root!, "modules"), "*", SearchOption.AllDirectories)
                    .ToDictionary(path => path, File.ReadAllBytes)
                : [];
            await using var scope = await maintenance.AcquireAsync(token);
            var retried = StorageResultChecks.Check(await registry.RegisterAsync(owner, scope, token));
            if (sqlite && phase is ("sqlite_directory_created" or "sqlite_file_created" or "physical_before_commit"))
            {
                RegistrationScenarios.Require(retried.Outcome == StorageOutcome.ReconciliationRequired);
                RegistrationScenarios.Require(Directory.Exists(Path.Combine(root!, "modules", before!.Key!)));
                foreach (var item in preserved)
                    RegistrationScenarios.Require(item.Value.SequenceEqual(File.ReadAllBytes(item.Key)));
                assertions += 2 + preserved.Count;
            }
            else
            {
                var actual = RegistrationScenarios.Ready(retried).Assignment;
                RegistrationScenarios.Require(before is null ||
                    actual.AllocationId == before.AllocationId && actual.AllocationKey == before.Key);
                assertions += 2;
            }
        }
        return assertions;
    }

    private static async Task<int> StoppedRestoreAsync(bool sqlite, CancellationToken token)
    {
        if (!sqlite && Environment.GetEnvironmentVariable("KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE") != "true")
            throw new InvalidOperationException("postgres_disposable_new_target_required");
        using var data = new OwnedTestRoot();
        using var backup = new OwnedTestRoot();
        using var targetRoot = new OwnedTestRoot();
        await using var source = sqlite ? null : await PostgresResources.CreateAsync(true, token: token);
        if (sqlite) await new SqlitePlatformStore(data.Path).InitializeAsync();
        var installation = sqlite ? StorageInstallation.Sqlite(data.Path) :
            StorageInstallation.PostgreSql(source!.ConnectionString, source.ProvisioningConnectionString);
        var registry = new StorageRegistry(installation);
        var maintenance = new StorageMaintenance(installation);
        var alpha = RegistrationScenarios.Module("org.example.recovery-alpha");
        var beta = RegistrationScenarios.Module("org.example.recovery-beta");
        StorageAssignment alphaAssignment, betaAssignment;
        await using (var scope = await maintenance.AcquireAsync(token))
        {
            await maintenance.UpgradeAsync(scope, token);
            alphaAssignment = RegistrationScenarios.Ready(await registry.RegisterAsync(alpha, scope, token)).Assignment;
            betaAssignment = RegistrationScenarios.Ready(await registry.RegisterAsync(beta, scope, token)).Assignment;
            await using (var binding = await registry.BindAsync(alpha.Id, token))
            {
                RegistrationScenarios.Ready(await binding.MigrateAsync(AlphaStorage.Create, scope, cancellationToken: token));
                await using var context = await binding.CreateContextAsync(AlphaStorage.Create, token);
                context.Records.Add(new Modules.Alpha.Record { Id = 1, Payload = "recovered-alpha" });
                await context.SaveChangesAsync(token);
            }
            await using (var binding = await registry.BindAsync(beta.Id, token))
            {
                RegistrationScenarios.Ready(await binding.MigrateAsync(BetaStorage.Create, scope, cancellationToken: token));
                await using var context = await binding.CreateContextAsync(BetaStorage.Create, token);
                context.Records.Add(new Modules.Beta.Record { Id = 1, Payload = "recovered-beta" });
                await context.SaveChangesAsync(token);
            }
        }
        var manifest = sqlite ? await SqliteRecoveryExercise.BackupAsync(data.Path, backup.Path, token)
            : await source!.BackupStorageSetAsync(backup.Path, token);
        using var legacyRoot = new OwnedTestRoot();
        using var legacyBackup = new OwnedTestRoot();
        var oldBackup = Path.Combine(legacyBackup.Path, "legacy.db");
        if (sqlite)
        {
            var legacy = new SqlitePlatformStore(legacyRoot.Path);
            await legacy.InitializeAsync();
            legacy.Backup(oldBackup);
            var current = new SqlitePlatformStore(data.Path);
            RefuseLegacy(() => current.Backup(Path.Combine(legacyBackup.Path, "refused.db")));
            RefuseLegacy(() => current.Restore(oldBackup, true, oldBackup));
            RefuseLegacy(() => current.Reset(true));
            RegistrationScenarios.Require(!File.Exists(Path.Combine(legacyBackup.Path, "refused.db")));
        }
        else
        {
            await using var legacy = await source!.CreateRecoveryTargetAsync(token);
            await legacy.BackupAsync(oldBackup, token);
            try
            {
                await source.BackupAsync(Path.Combine(legacyBackup.Path, "refused.dump"), token);
                throw new InvalidOperationException("storage_legacy_backup_not_refused");
            }
            catch (InvalidOperationException error) when (error.Message == "postgres_registry_requires_complete_recovery_set")
            { RegistrationScenarios.Require(true); }
            try
            {
                await source.RestoreAsync(oldBackup, token);
                throw new InvalidOperationException("storage_legacy_restore_not_refused");
            }
            catch (InvalidOperationException error) when (error.Message == "postgres_restore_target_not_empty")
            { RegistrationScenarios.Require(true); }
            RegistrationScenarios.Require(!File.Exists(Path.Combine(legacyBackup.Path, "refused.dump")));
        }
        RegistrationScenarios.Require(manifest.Complete && manifest.Allocations.Count == 2 &&
            manifest.Artifacts.Count == (sqlite ? 3 : 1));
        await using var target = sqlite ? null : await source!.CreateRecoveryTargetAsync(token);
        if (sqlite) await SqliteRecoveryExercise.RestoreFreshAsync(backup.Path, targetRoot.Path, token);
        else await source!.RestoreStorageSetToFreshAsync(backup.Path, target!, token);
        var restored = new StorageRegistry(sqlite ? StorageInstallation.Sqlite(targetRoot.Path) :
            StorageInstallation.PostgreSql(target!.ConnectionString));
        foreach (var expected in new[] { alphaAssignment, betaAssignment })
        {
            var actual = RegistrationScenarios.Ready(await restored.ResolveAsync(expected.Owner, token)).Assignment;
            RegistrationScenarios.Require(actual.AllocationId == expected.AllocationId &&
                actual.AllocationKey == expected.AllocationKey && actual.InstallationId == expected.InstallationId);
        }
        await using (var binding = await restored.BindAsync(alpha.Id, token))
        await using (var context = await binding.CreateContextAsync(AlphaStorage.Create, token))
        {
            RegistrationScenarios.Require((await context.Records.SingleAsync(token)).Payload == "recovered-alpha");
            RegistrationScenarios.Require((await context.Database.GetAppliedMigrationsAsync(token)).Count() == 2);
        }
        await using (var binding = await restored.BindAsync(beta.Id, token))
        await using (var context = await binding.CreateContextAsync(BetaStorage.Create, token))
        {
            RegistrationScenarios.Require((await context.Records.SingleAsync(token)).Payload == "recovered-beta");
            RegistrationScenarios.Require((await context.Database.GetAppliedMigrationsAsync(token)).Count() == 1);
        }
        var path = Path.Combine(backup.Path, RecoveryManifest.FileName);
        var original = await File.ReadAllTextAsync(path, token);
        var mixed = JsonNode.Parse(original)!;
        mixed["registryRevision"] = manifest.RegistryRevision + 1;
        await File.WriteAllTextAsync(path, mixed.ToJsonString(), token);
        using var refused = new OwnedTestRoot();
        try
        {
            if (sqlite) await SqliteRecoveryExercise.RestoreFreshAsync(backup.Path, refused.Path, token);
            else await source!.RestoreStorageSetToFreshAsync(backup.Path, target!, token);
            throw new InvalidOperationException("storage_mixed_restore_not_refused");
        }
        catch (StorageException) { }
        finally { await File.WriteAllTextAsync(path, original, token); }
        RegistrationScenarios.Require(!Directory.Exists(refused.Path));
        try
        {
            if (sqlite) await SqliteRecoveryExercise.RestoreFreshAsync(backup.Path, data.Path, token);
            else await source!.RestoreStorageSetToFreshAsync(backup.Path, source, token);
            throw new InvalidOperationException("storage_existing_restore_not_refused");
        }
        catch (InvalidOperationException error) when (error.Message is "recovery_fresh_target_required" or "postgres_fresh_target_required") { }
        await using (var binding = await registry.BindAsync(alpha.Id, token))
        await using (var context = await binding.CreateContextAsync(AlphaStorage.Create, token))
            RegistrationScenarios.Require((await context.Records.SingleAsync(token)).Payload == "recovered-alpha");
        return 14;
    }

    private static void RefuseLegacy(Action action)
    {
        try { action(); throw new InvalidOperationException("storage_legacy_operation_not_refused"); }
        catch (BootstrapException) { RegistrationScenarios.Require(true); }
    }
}
