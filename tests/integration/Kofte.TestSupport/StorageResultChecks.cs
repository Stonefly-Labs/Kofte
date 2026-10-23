using System.Text.Json;
using System.Text.RegularExpressions;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;

namespace Kofte.TestSupport;

public static class StorageResultChecks
{
    public static T Check<T>(T result) where T : StorageResult
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(result, result.GetType());
        using var json = JsonDocument.Parse(bytes);
        if (bytes.Length > 4096 || result.ContractVersion != 1 ||
            !Enum.IsDefined(result.Outcome) || !Enum.IsDefined(result.Operation) ||
            !Enum.IsDefined(result.Durability) || result.Provider is not ("sqlite" or "postgresql") ||
            !Regex.IsMatch(result.CorrelationId, @"\A[0-9a-f]{32}\z") ||
            result.CorrelationId == new string('0', 32) ||
            result.ModuleId is not null && !ModuleId.TryParse(result.ModuleId, out _) ||
            result.Diagnostics.Count is < 1 or > 8 ||
            json.RootElement.TryGetProperty("Assignment", out _) != (result is StorageResult.Ready))
            throw new InvalidOperationException("storage_public_result_invalid");
        foreach (var diagnostic in result.Diagnostics)
            if (!Regex.IsMatch(diagnostic.Code, @"\A[a-z_]{1,64}\z") ||
                diagnostic.Message.Length is < 1 or > 256 || diagnostic.Message.Any(char.IsControl) ||
                !Regex.IsMatch(diagnostic.Guidance, @"\A[a-z_]{1,64}\z") ||
                diagnostic.Guidance is not ("retry_same_request" or "correct_configuration" or
                    "inspect_preserved_target" or "restore_complete_set" or "upgrade_supported_binary") ||
                (diagnostic.Code is "storage_ready" or "storage_no_storage") !=
                    (result.Outcome is StorageOutcome.Ready or StorageOutcome.NoStorage))
                throw new InvalidOperationException("storage_public_diagnostic_invalid");
        return result;
    }
}
