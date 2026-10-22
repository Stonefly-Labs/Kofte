using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Kofte.Infrastructure.Storage;

internal static class StorageDiagnostics
{
    private static readonly Meter Meter = new("Kofte.Storage", "1.0.0");
    private static readonly Counter<long> Operations = Meter.CreateCounter<long>("kofte.storage.operations");
    private static readonly ActivitySource Traces = new("Kofte.Storage", "1.0.0");
    private static readonly HashSet<string> FailureCodes = Enum.GetValues<StorageCode>()
        .Where(code => code is not (StorageCode.Ready or StorageCode.NoStorage))
        .Select(code => Diagnostic(code).Code).ToHashSet(StringComparer.Ordinal);

    internal static bool IsFailureCode(string? code) => code is null || FailureCodes.Contains(code);

    internal static StorageDiagnostic Diagnostic(StorageCode code) => code switch
    {
        StorageCode.Ready => new("storage_ready", "Assigned storage is verified.", "retry_same_request"),
        StorageCode.NoStorage => new("storage_no_storage", "The module declares no relational storage.", "retry_same_request"),
        StorageCode.Busy => new("storage_busy", "Installation maintenance is busy.", "retry_same_request"),
        StorageCode.Conflict => new("storage_conflict", "The request conflicts with immutable ownership.", "correct_configuration"),
        StorageCode.TargetReserved => new("storage_target_reserved", "The allocation is outside the module namespace.", "correct_configuration"),
        StorageCode.PathRejected => new("storage_path_rejected", "The assigned filesystem boundary is invalid.", "inspect_preserved_target"),
        StorageCode.OwnerMismatch => new("storage_owner_mismatch", "Physical ownership does not match the registry.", "inspect_preserved_target"),
        StorageCode.Missing => new("storage_missing", "Assigned storage is missing; no replacement was created.", "restore_complete_set"),
        StorageCode.Corrupt => new("storage_corrupt", "Storage consistency verification failed.", "restore_complete_set"),
        StorageCode.Incomplete => new("storage_incomplete", "Provisioning has not completed.", "retry_same_request"),
        StorageCode.ReconciliationRequired => new("storage_reconciliation_required", "Preserved storage requires explicit reconciliation.", "inspect_preserved_target"),
        StorageCode.VersionUnsupported => new("storage_version_unsupported", "This binary cannot use the stored format.", "upgrade_supported_binary"),
        StorageCode.PrivilegeDenied => new("storage_privilege_denied", "Required offline privileges are unavailable.", "correct_configuration"),
        StorageCode.Unavailable => new("storage_unavailable", "The selected storage provider is unavailable.", "correct_configuration"),
        StorageCode.DeadlineExceeded => new("storage_deadline_exceeded", "The operation deadline expired.", "inspect_preserved_target"),
        StorageCode.Cancelled => new("storage_cancelled", "The operation was cancelled; inspect any committed work.", "inspect_preserved_target"),
        _ => new("storage_request_invalid", "The trusted storage request is invalid.", "correct_configuration")
    };

    internal static StorageResult Failure(StorageCode code, StorageOperation operation, string provider,
        ModuleId? owner, string correlation, StorageDurability durability) =>
        new StorageResult.Failure(code switch
        {
            StorageCode.Busy => StorageOutcome.Busy,
            StorageCode.Incomplete => StorageOutcome.Incomplete,
            StorageCode.ReconciliationRequired or StorageCode.Missing or StorageCode.Corrupt or
                StorageCode.OwnerMismatch => StorageOutcome.ReconciliationRequired,
            StorageCode.Conflict or StorageCode.TargetReserved or StorageCode.PathRejected or
                StorageCode.RequestInvalid => StorageOutcome.Rejected,
            _ => StorageOutcome.Failed
        }, operation, provider, owner, correlation, durability, Diagnostic(code));

    internal static async Task<StorageResult> RunAsync(StorageOperation operation, string provider,
        ModuleId? owner, TimeSpan duration, CancellationToken cancellation,
        Func<OperationBudget, MutationEvidence, string, Task<StorageResult>> action)
    {
        using var trace = Traces.StartActivity("storage." + operation.ToString().ToLowerInvariant());
        var correlation = trace?.TraceId.ToHexString() ?? ActivityTraceId.CreateRandom().ToHexString();
        using var budget = new OperationBudget(duration, cancellation);
        var mutation = new MutationEvidence();
        StorageResult result;
        try
        {
            budget.Check();
            result = await action(budget, mutation, correlation);
        }
        catch (StorageException error)
        {
            result = Failure(error.Code, operation, provider, owner, correlation, mutation.Durability);
        }
        catch (StorePathException error)
        {
            result = Failure(error.Code == StorePathError.StorageMissing ? StorageCode.Missing :
                error.Code == StorePathError.StoreBusy ? StorageCode.Busy : StorageCode.PathRejected,
                operation, provider, owner, correlation, mutation.Durability);
        }
        catch (OperationCanceledException)
        {
            result = Failure(budget.CallerCancelled ? StorageCode.Cancelled : StorageCode.DeadlineExceeded,
                operation, provider, owner, correlation, mutation.Durability);
        }
        catch (PostgresException error)
        {
            var code = error.SqlState switch
            {
                PostgresErrorCodes.InsufficientPrivilege => StorageCode.PrivilegeDenied,
                PostgresErrorCodes.UniqueViolation => StorageCode.Conflict,
                PostgresErrorCodes.LockNotAvailable => StorageCode.Busy,
                _ => StorageCode.Unavailable
            };
            result = Failure(code, operation, provider, owner, correlation, mutation.Durability);
        }
        catch (SqliteException error)
        {
            var code = error.SqliteErrorCode switch
            {
                5 or 6 => StorageCode.Busy,
                19 => StorageCode.Conflict,
                11 or 26 => StorageCode.Corrupt,
                _ => StorageCode.Unavailable
            };
            result = Failure(code, operation, provider, owner, correlation, mutation.Durability);
        }
        catch (Exception)
        {
            result = Failure(StorageCode.Unavailable, operation, provider, owner, correlation, mutation.Durability);
        }
        var operationName = operation.ToString().ToLowerInvariant();
        var outcome = result.Outcome.ToString().ToLowerInvariant();
        Operations.Add(1, new("operation", operationName), new("provider", provider), new("outcome", outcome));
        trace?.SetTag("storage.operation", operationName);
        trace?.SetTag("storage.provider", provider);
        trace?.SetTag("storage.outcome", outcome);
        trace?.SetTag("storage.module_id", owner?.Value);
        trace?.SetTag("storage.code", result.Diagnostics[0].Code);
        if (result is StorageResult.Failure) trace?.SetStatus(ActivityStatusCode.Error);
        Console.Error.WriteLine(JsonSerializer.Serialize(new
        {
            eventCode = result.Diagnostics[0].Code,
            component = "storage",
            operation = operationName,
            provider,
            outcome,
            moduleId = owner?.Value,
            correlationId = correlation
        }));
        return result;
    }
}

internal sealed class MutationEvidence
{
    internal StorageDurability Durability { get; private set; } = StorageDurability.Unchanged;
    internal void PossibleCommit() => Durability = StorageDurability.Uncertain;
    internal void Committed() => Durability = StorageDurability.Committed;
}
