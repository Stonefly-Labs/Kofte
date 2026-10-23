using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;

namespace Kofte.TestSupport;

public sealed record StorageAssignmentSnapshot(int ContractVersion, string Owner, string Provider,
    Guid InstallationId, Guid AllocationId, string AllocationKey, int AllocatorVersion, long Revision)
{
    public static StorageAssignmentSnapshot From(StorageAssignment assignment) =>
        new(assignment.ContractVersion, assignment.Owner.Value, assignment.Provider, assignment.InstallationId,
            assignment.AllocationId, assignment.AllocationKey, assignment.AllocatorVersion, assignment.Revision);
}

public sealed record StorageRestartResult(int ProcessId, StorageAssignmentSnapshot[] Assignments, int RetainedSentinels);

public static class StorageRestartProcess
{
    public static async Task<StorageRestartResult> RunAsync(Assembly entry, string provider, int restart,
        Action<ProcessStartInfo> configure, CancellationToken token)
    {
        StorageScenarioCatalog.RequireProvider(provider);
        if (restart is < 1 or > 3) throw new InvalidOperationException("storage_restart_invalid");
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = OwnedProcesses.FindRepository()
        };
        start.ArgumentList.Add(entry.Location);
        foreach (var argument in new[] { "--storage-restart-worker", provider, restart.ToString(System.Globalization.CultureInfo.InvariantCulture) })
            start.ArgumentList.Add(argument);
        configure(start);
        StorageRestartResult? result = null;
        await StorageMaintenanceProcess.RunAsync(start, token, async (reader, cancellation) =>
        {
            var buffer = new char[4097];
            var length = 0;
            while (length < buffer.Length)
            {
                var read = await reader.ReadAsync(buffer.AsMemory(length), cancellation);
                if (read == 0) break;
                length += read;
            }
            if (length == buffer.Length) throw new InvalidOperationException("storage_restart_output_limit");
            result = JsonSerializer.Deserialize<StorageRestartResult>(new string(buffer, 0, length),
                new JsonSerializerOptions
                {
                    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                    RespectRequiredConstructorParameters = true,
                    RespectNullableAnnotations = true,
                    AllowDuplicateProperties = false
                });
        });
        if (result is null || result.ProcessId <= 0 || result.ProcessId == Environment.ProcessId ||
            result.Assignments.Length != 2 || result.RetainedSentinels != 2)
            throw new InvalidOperationException("storage_restart_result_invalid");
        return result;
    }
}
