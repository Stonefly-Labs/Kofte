using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Kofte.TestSupport;

public static class StorageCrashWorker
{
    public static readonly string[] RegistrationPhases = [
        "reservation_before_commit", "reservation_committed", "physical_before_commit",
        "physical_committed", "ready_before_commit", "ready_committed"
    ];
    public static readonly string[] SqliteCreationPhases = ["sqlite_directory_created", "sqlite_file_created"];
    public static readonly string[] MigrationPhases = [
        "migration_in_progress_before_commit", "migration_in_progress_committed",
        "migration_before_commit", "migration_committed",
        "migration_current_before_commit", "migration_current_committed"
    ];
    public static readonly string[] UpgradePhases = ["registry_before_commit", "registry_committed"];

    public static ProcessStartInfo Start(Assembly entry, string provider, string operation, string phase,
        string owner, Action<ProcessStartInfo> configure)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = OwnedProcesses.FindRepository()
        };
        start.ArgumentList.Add(entry.Location);
        foreach (var argument in new[] { "--storage-crash-worker", provider, operation, phase })
            start.ArgumentList.Add(argument);
        configure(start);
        start.Environment["KOFTE_STORAGE_WORKER_OWNER"] = owner;
        return start;
    }

    public static async Task<ProcessOutput> KillAtAsync(ProcessStartInfo start, string phase, CancellationToken token)
    {
        var admission = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
        start.Environment[StorageMaintenanceProcess.AdmissionKey] = admission;
        await using var child = OwnedProcesses.Start(start, captureOutput: false);
        using var reader = new StreamReader(child.StandardOutput);
        using var observation = CancellationTokenSource.CreateLinkedTokenSource(token);
        observation.CancelAfter(TimeSpan.FromSeconds(10));
        await child.StandardInput.WriteLineAsync(admission);
        await child.StandardInput.FlushAsync(observation.Token);
        var line = await reader.ReadLineAsync(observation.Token);
        using var value = JsonDocument.Parse(line ?? throw new InvalidOperationException("storage_worker_phase_missing"));
        if (value.RootElement.GetProperty("eventCode").GetString() != "storage_worker_phase" ||
            value.RootElement.GetProperty("phase").GetString() != phase)
            throw new InvalidOperationException("storage_worker_phase_mismatch");
        await child.StopAsync();
        var output = await child.ResultAsync();
        if (output.ExitCode == 0) throw new InvalidOperationException("storage_worker_was_not_killed");
        return output with { Stdout = line };
    }

    public static async Task<int> RunAsync(string[] args,
        Func<DbContextOptions, ModuleStorageBinding, AssignedDbContext> factory)
    {
        try
        {
            if (args is not ["--storage-crash-worker", "sqlite" or "postgres", var operation, var phase] ||
                !(operation switch
                {
                    "register" => RegistrationPhases.Contains(phase) ||
                        args[1] == "sqlite" && SqliteCreationPhases.Contains(phase),
                    "migrate" => MigrationPhases.Contains(phase) ||
                        args[1] == "sqlite" && phase == "migration_sql_executing",
                    "upgrade" => UpgradePhases.Contains(phase),
                    _ => false
                })) throw new InvalidOperationException("storage_worker_selector_invalid");
            using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(110));
            await StorageMaintenanceProcess.AdmitAsync(budget.Token);
            using var barrier = new StoragePhaseBarrier(phase,
                Environment.GetEnvironmentVariable("KOFTE_STORAGE_WORKER_HOLD") == "true");
            var installation = args[1] == "sqlite"
                ? StorageInstallation.Sqlite(Environment.GetEnvironmentVariable("KOFTE_DATA_ROOT") ??
                    throw new InvalidOperationException("storage_worker_configuration_required"))
                : StorageInstallation.PostgreSql(Environment.GetEnvironmentVariable("KOFTE_POSTGRES_CONNECTION") ??
                    throw new InvalidOperationException("storage_worker_configuration_required"),
                    Environment.GetEnvironmentVariable("KOFTE_POSTGRES_PROVISIONING_CONNECTION"));
            var maintenance = new StorageMaintenance(installation);
            await using var scope = await maintenance.AcquireAsync(budget.Token);
            if (operation == "upgrade") await maintenance.UpgradeAsync(scope, budget.Token);
            else
            {
                var ownerText = Environment.GetEnvironmentVariable("KOFTE_STORAGE_WORKER_OWNER");
                if (!ModuleId.TryParse(ownerText, out var owner)) throw new InvalidOperationException("storage_worker_owner_invalid");
                var registry = new StorageRegistry(installation);
                StorageResult result;
                if (operation == "register") result = await registry.RegisterAsync(Description(owner.Value), scope, budget.Token);
                else
                {
                    await using var binding = await registry.BindAsync(owner, budget.Token);
                    result = await binding.MigrateAsync((options, owned) =>
                    {
                        var context = factory(options, owned);
                        if (context.Database.GetDbConnection() is SqliteConnection sqlite)
                            barrier.ObserveNativeSql(sqlite);
                        return context;
                    }, scope, "202610210001_Initial", budget.Token);
                }
                if (result is not StorageResult.Ready) throw new InvalidOperationException("storage_worker_operation_failed");
            }
            throw new InvalidOperationException("storage_worker_phase_not_reached");
        }
        catch
        {
            Console.Error.WriteLine("""{"eventCode":"storage_worker_failed"}""");
            return 1;
        }
    }

    public static ModuleDescription Description(string owner)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            id = owner,
            version = "1.0.0",
            displayName = "Köfte crash fixture",
            hostContract = "1.0.0",
            capabilities = Array.Empty<object>(),
            dependencies = Array.Empty<object>(),
            permissions = Array.Empty<object>(),
            storage = new { relational = true }
        });
        return ManifestValidator.Validate(bytes, new ManifestValidationContext("1.0.0"))
            is ManifestValidationResult.Accepted accepted ? accepted.Description :
            throw new InvalidOperationException("storage_worker_owner_invalid");
    }
}
