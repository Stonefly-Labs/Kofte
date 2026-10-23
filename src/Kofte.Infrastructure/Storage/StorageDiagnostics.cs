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
    private static readonly DiagnosticListener Phases = new("Kofte.Storage");
    private static readonly HashSet<string> FailureCodes = Enum.GetValues<StorageCode>()
        .Where(code => code is not (StorageCode.Ready or StorageCode.NoStorage))
        .Select(code => Diagnostic(code).Code).ToHashSet(StringComparer.Ordinal);

    internal static bool IsFailureCode(string? code) => code is null || FailureCodes.Contains(code);

    internal static void Checkpoint(string phase, string provider, ModuleId? owner = null)
    {
        Activity.Current?.AddEvent(new ActivityEvent("storage." + phase));
        if (Phases.IsEnabled("storage.phase"))
            Phases.Write("storage.phase", new { phase, provider, moduleId = owner?.Value });
    }

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
        new StorageResult.Failure(Outcome(code), operation, provider, owner, correlation, durability, Diagnostic(code));

    private static StorageOutcome Outcome(StorageCode code) => code switch
    {
        StorageCode.Ready => StorageOutcome.Ready,
        StorageCode.NoStorage => StorageOutcome.NoStorage,
        StorageCode.Busy => StorageOutcome.Busy,
        StorageCode.Incomplete => StorageOutcome.Incomplete,
        StorageCode.ReconciliationRequired or StorageCode.Missing or StorageCode.Corrupt or
            StorageCode.OwnerMismatch => StorageOutcome.ReconciliationRequired,
        StorageCode.Conflict or StorageCode.TargetReserved or StorageCode.PathRejected or
            StorageCode.RequestInvalid => StorageOutcome.Rejected,
        _ => StorageOutcome.Failed
    };

    private static StorageCode FailureCode(Exception error, bool callerCancelled) => error switch
    {
        StorageException storage => storage.Code,
        StorePathException path => path.Code switch
        {
            StorePathError.StorageMissing => StorageCode.Missing,
            StorePathError.StoreBusy => StorageCode.Busy,
            _ => StorageCode.PathRejected
        },
        OperationCanceledException => callerCancelled ? StorageCode.Cancelled : StorageCode.DeadlineExceeded,
        PostgresException postgres => postgres.SqlState switch
        {
            PostgresErrorCodes.InsufficientPrivilege => StorageCode.PrivilegeDenied,
            PostgresErrorCodes.UniqueViolation => StorageCode.Conflict,
            PostgresErrorCodes.LockNotAvailable => StorageCode.Busy,
            _ => StorageCode.Unavailable
        },
        SqliteException sqlite => sqlite.SqliteErrorCode switch
        {
            5 or 6 => StorageCode.Busy,
            19 => StorageCode.Conflict,
            11 or 26 => StorageCode.Corrupt,
            _ => StorageCode.Unavailable
        },
        NpgsqlException { InnerException: TimeoutException } => StorageCode.DeadlineExceeded,
        _ => StorageCode.Unavailable
    };

    internal static StorageCode MaintenanceFailure(StorageOperation operation, string provider,
        Exception error, CancellationToken cancellation)
    {
        var code = FailureCode(error, cancellation.IsCancellationRequested);
        using var trace = Traces.StartActivity("storage." + operation.ToString().ToLowerInvariant());
        Record(operation, provider, null, trace?.TraceId.ToHexString() ?? ActivityTraceId.CreateRandom().ToHexString(),
            Outcome(code), Diagnostic(code), StorageDurability.Unchanged, trace);
        return code;
    }

    internal static async Task RunMaintenanceAsync(string provider, CancellationToken cancellation,
        Func<OperationBudget, MutationEvidence, Task> action)
    {
        using var trace = Traces.StartActivity("storage.upgrade");
        var correlation = trace?.TraceId.ToHexString() ?? ActivityTraceId.CreateRandom().ToHexString();
        using var budget = new OperationBudget(OperationBudget.Register, cancellation);
        var mutation = new MutationEvidence();
        var code = StorageCode.Ready;
        try
        {
            budget.Check();
            await action(budget, mutation);
        }
        catch (Exception error)
        {
            code = FailureCode(error, budget.CallerCancelled);
            if (error is StorageException) throw;
            if (error is OperationCanceledException) throw new OperationCanceledException(budget.Token);
            throw new StorageException(code);
        }
        finally
        {
            Record(StorageOperation.Upgrade, provider, null, correlation, Outcome(code), Diagnostic(code),
                mutation.Durability, trace);
        }
    }

    internal static async Task<T> ObserveRecoveryAsync<T>(string? provider, CancellationToken cancellation,
        Func<OperationBudget, MutationEvidence, Action<string>, Task<T>> action)
    {
        var safeProvider = Provider(provider);
        using var trace = Traces.StartActivity("storage.recover");
        var correlation = trace?.TraceId.ToHexString() ?? ActivityTraceId.CreateRandom().ToHexString();
        using var budget = new OperationBudget(OperationBudget.Migration, cancellation);
        var mutation = new MutationEvidence();
        var code = StorageCode.Ready;
        try
        {
            budget.Check();
            return await action(budget, mutation, value => safeProvider = Provider(value));
        }
        catch (Exception error)
        {
            code = FailureCode(error, budget.CallerCancelled);
            throw;
        }
        finally
        {
            Record(StorageOperation.Recover, safeProvider, null, correlation, Outcome(code), Diagnostic(code),
                mutation.Durability, trace);
        }

        static string Provider(string? value) => value is "sqlite" or "postgresql" ? value : "unknown";
    }

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
        catch (Exception error)
        {
            result = Failure(FailureCode(error, budget.CallerCancelled), operation, provider, owner, correlation,
                mutation.Durability);
        }
        Record(operation, provider, owner, correlation, result.Outcome, result.Diagnostics[0], result.Durability, trace);
        return result;
    }

    private static void Record(StorageOperation operation, string provider, ModuleId? owner, string correlation,
        StorageOutcome result, StorageDiagnostic diagnostic, StorageDurability durability, Activity? trace)
    {
        var operationName = operation.ToString().ToLowerInvariant();
        var outcome = result.ToString().ToLowerInvariant();
        Operations.Add(1, new("operation", operationName), new("provider", provider), new("outcome", outcome));
        trace?.SetTag("storage.operation", operationName);
        trace?.SetTag("storage.provider", provider);
        trace?.SetTag("storage.outcome", outcome);
        trace?.SetTag("storage.module_id", owner?.Value);
        trace?.SetTag("storage.code", diagnostic.Code);
        trace?.SetTag("storage.guidance", diagnostic.Guidance);
        trace?.SetTag("storage.durability", durability.ToString().ToLowerInvariant());
        if (result is not (StorageOutcome.Ready or StorageOutcome.NoStorage)) trace?.SetStatus(ActivityStatusCode.Error);
        Console.Error.WriteLine(JsonSerializer.Serialize(new
        {
            eventCode = diagnostic.Code,
            component = "storage",
            operation = operationName,
            provider,
            outcome,
            moduleId = owner?.Value,
            correlationId = correlation,
            guidance = diagnostic.Guidance,
            durability = durability.ToString().ToLowerInvariant()
        }));
    }
}

internal sealed class MutationEvidence
{
    internal StorageDurability Durability { get; private set; } = StorageDurability.Unchanged;
    internal void PossibleCommit() => Durability = StorageDurability.Uncertain;
    internal void Committed() => Durability = StorageDurability.Committed;
}
