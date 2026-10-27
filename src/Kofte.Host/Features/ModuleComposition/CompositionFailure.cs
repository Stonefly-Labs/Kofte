using Kofte.Host.Bootstrap;
using Kofte.Host.Features.Identity;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Composition;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Host.Features.ModuleComposition;

internal sealed record CompositionFailure(string Code, CompositionOutcome Outcome, int Exit, StorageResult? Storage = null)
{
    internal static CompositionFailure From(Exception error) => error switch
    {
        ModuleSelectionException => Rejected("composition_configuration_invalid"),
        IdentityConfigurationException => Rejected("composition_configuration_invalid"),
        IdentityStateException { Outcome: IdentityDiagnosticOutcome.PrepareRequired } =>
            Rejected("composition_prepare_required"),
        IdentityStateException or InvalidOperationException { Message: "identity_keys_invalid" } =>
            new("composition_reconciliation_required", CompositionOutcome.ReconciliationRequired, 4),
        BootstrapException { Code: BootstrapError.StoreBusy } => new("composition_storage_rejected", CompositionOutcome.Busy, 3),
        BootstrapException => Rejected("composition_configuration_invalid"),
        CompositionStorageException { Code: "composition_prepare_required" } => Rejected("composition_prepare_required"),
        CompositionStorageException { Code: "composition_storage_reconciliation_required" } storage =>
            new("composition_reconciliation_required", CompositionOutcome.ReconciliationRequired, 4, storage.StorageFailure),
        CompositionStorageException storage => storage.StorageFailure is { } result
            ? StorageError(result) : Rejected("composition_storage_rejected"),
        StorageException storage => StorageCodeError(storage.Code),
        StorePathException storage => storage.Code == StorePathError.StoreBusy
            ? new("composition_storage_rejected", CompositionOutcome.Busy, 3)
            : Rejected("composition_storage_rejected"),
        CompositionBoundaryException { Code: "composition_deadline_exceeded" } =>
            new("composition_deadline_exceeded", CompositionOutcome.Deadline, 1),
        CompositionBoundaryException { Code: "composition_cancelled" } or OperationCanceledException =>
            new("composition_cancelled", CompositionOutcome.Cancelled, 1),
        CompositionBoundaryException { Code: "composition_cleanup_incomplete" } =>
            new("composition_cleanup_incomplete", CompositionOutcome.Incomplete, 1),
        CompositionBoundaryException boundary when boundary.Code is "composition_registration_failed" or
            "composition_initialization_failed" or "composition_activation_failed" or "composition_health_degraded" or
            "composition_health_failed" or "composition_health_unknown" =>
            new(boundary.Code, CompositionOutcome.Failed, 1),
        CompositionBoundaryException boundary when boundary.Code is "composition_output_failed" or
            "composition_output_limit" or "composition_observation_invalid" =>
            new(boundary.Code, CompositionOutcome.Failed, 1),
        CompositionBoundaryException boundary => Rejected(boundary.Code),
        _ => new("composition_storage_rejected", CompositionOutcome.Failed, 1)
    };

    private static CompositionFailure StorageError(StorageResult result)
    {
        var failure = result.Outcome switch
        {
            StorageOutcome.Busy => new("composition_storage_rejected", CompositionOutcome.Busy, 3),
            StorageOutcome.Incomplete or StorageOutcome.ReconciliationRequired =>
                new("composition_reconciliation_required", CompositionOutcome.ReconciliationRequired, 4),
            StorageOutcome.Rejected => Rejected("composition_storage_rejected"),
            _ => new CompositionFailure("composition_storage_rejected", CompositionOutcome.Failed, 1)
        };
        return failure with { Storage = result };
    }

    private static CompositionFailure StorageCodeError(StorageCode code) => code switch
    {
        StorageCode.Busy => new("composition_storage_rejected", CompositionOutcome.Busy, 3),
        StorageCode.Incomplete or StorageCode.ReconciliationRequired =>
            new("composition_reconciliation_required", CompositionOutcome.ReconciliationRequired, 4),
        StorageCode.DeadlineExceeded => new("composition_deadline_exceeded", CompositionOutcome.Deadline, 1),
        StorageCode.Cancelled => new("composition_cancelled", CompositionOutcome.Cancelled, 1),
        StorageCode.Unavailable => new("composition_storage_rejected", CompositionOutcome.Failed, 1),
        _ => Rejected("composition_storage_rejected")
    };
    private static CompositionFailure Rejected(string code) => new(code, CompositionOutcome.Rejected, 2);
}
