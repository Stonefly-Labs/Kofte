using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Identity;

internal enum IdentityAction
{
    Startup, Prepare, ReconcileConfiguration, RevokeSessions, BrowserContext, Login, Callback,
    Current, SignOut, Enrollment, DevelopmentSession, Maintain, ReconcileRestore, OwnerMutation, Access,
    BootstrapAuthorize, BootstrapDisablePending
}

internal enum IdentityDiagnosticOutcome
{
    Ready, RequestInvalid, Unauthenticated, Forbidden, EnrollmentUnavailable, AdmissionExceeded,
    Unavailable, OutcomeUncertain, PrepareRequired, ReconciliationRequired, KeysInvalid, Cancelled, ConfigurationInvalid
}

internal sealed record IdentityDiagnosticRecord(int Version, string EventCode, string Component,
    string Operation, string Mode, string Provider, string Outcome, string CorrelationId,
    string Guidance, string Durability, string? Retention = null);

internal static class IdentityDiagnostics
{
    private static readonly AsyncLocal<IdentityObservation?> Ambient = new();
    private static readonly DiagnosticListener Phases = new("Kofte.Identity");
    private static readonly ActivitySource Activities = new("Kofte.Identity", "1.0.0");
    private static readonly Meter Meter = new("Kofte.Identity", "1.0.0");
    private static readonly Counter<long> Outcomes = Meter.CreateCounter<long>("kofte.identity.operations");
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        AllowDuplicateProperties = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        MaxDepth = 2
    };
    internal static IdentityObservation? Current => Ambient.Value;

    internal static IdentityObservation Begin(IdentityAction action, string mode, string provider,
        TimeSpan duration, CancellationToken token, Action<IdentityDiagnosticRecord>? sink = null)
    {
        _ = ActionName(action);
        ValidateContext(mode, provider);
        var observation = new IdentityObservation(action, mode, provider, duration, token,
            sink ?? Write, Ambient.Value);
        Ambient.Value = observation;
        return observation;
    }

    internal static void Restore(IdentityObservation current, IdentityObservation? previous)
    {
        if (!ReferenceEquals(Ambient.Value, current)) throw Invalid();
        Ambient.Value = previous;
    }

    internal static async Task<T> ObserveAsync<T>(IdentityAction action, string mode, string provider,
        OperationBudget budget, Func<Task<T>> operation)
    {
        budget.Check();
        using var owned = Current is null ? Begin(action, mode, provider, budget.Remaining, budget.Token) : null;
        try { return await operation(); }
        catch (Exception error)
        {
            owned?.Fail(error);
            throw;
        }
    }

    internal static void Checkpoint(string phase)
    {
        if (!IsPhase(phase)) throw Invalid();
        Current?.Phase(phase);
        if (Phases.IsEnabled("identity.phase")) Phases.Write("identity.phase", new { phase });
    }

    private static bool IsPhase(string phase) => phase is "login_before_commit" or "login_committed" or
        "enrollment_before_commit" or "enrollment_committed" or "operator_before_commit" or
        "operator_committed" or "bootstrap_file_created" or "bootstrap_file_flushed" or
        "restore_quarantined" or "restore_before_commit" or "restore_committed";

    internal static string ActionName(IdentityAction action) => action switch
    {
        IdentityAction.Startup => "startup",
        IdentityAction.Prepare => "prepare",
        IdentityAction.BootstrapAuthorize => "bootstrap_authorize",
        IdentityAction.BootstrapDisablePending => "bootstrap_disable_pending",
        IdentityAction.ReconcileConfiguration => "reconcile_configuration",
        IdentityAction.RevokeSessions => "revoke_sessions",
        IdentityAction.BrowserContext => "browser_context",
        IdentityAction.Login => "login",
        IdentityAction.Callback => "callback",
        IdentityAction.Current => "current",
        IdentityAction.SignOut => "sign_out",
        IdentityAction.Enrollment => "enrollment",
        IdentityAction.DevelopmentSession => "development_session",
        IdentityAction.Maintain => "maintain",
        IdentityAction.ReconcileRestore => "reconcile_restore",
        IdentityAction.OwnerMutation => "owner_mutation",
        IdentityAction.Access => "access",
        _ => throw Invalid()
    };

    internal static (string Code, string Outcome, string Guidance) Rule(IdentityDiagnosticOutcome outcome) => outcome switch
    {
        IdentityDiagnosticOutcome.Ready => ("identity_completed", "ready", "none"),
        IdentityDiagnosticOutcome.RequestInvalid => ("identity_request_invalid", "rejected", "correct_request"),
        IdentityDiagnosticOutcome.Unauthenticated => ("identity_unauthenticated", "rejected", "start_fresh_login"),
        IdentityDiagnosticOutcome.Forbidden => ("identity_forbidden", "rejected", "refresh_browser_context"),
        IdentityDiagnosticOutcome.EnrollmentUnavailable =>
            ("identity_enrollment_unavailable", "rejected", "contact_operator"),
        IdentityDiagnosticOutcome.AdmissionExceeded => ("identity_admission_exceeded", "rejected", "retry_later"),
        IdentityDiagnosticOutcome.Unavailable => ("identity_unavailable", "unavailable", "retry_explicitly"),
        IdentityDiagnosticOutcome.OutcomeUncertain => ("identity_outcome_uncertain", "uncertain", "inspect_preserved_state"),
        IdentityDiagnosticOutcome.PrepareRequired => ("identity_prepare_required", "rejected", "stop_and_prepare"),
        IdentityDiagnosticOutcome.ReconciliationRequired =>
            ("identity_reconciliation_required", "rejected", "stop_and_reconcile"),
        IdentityDiagnosticOutcome.KeysInvalid => ("identity_keys_invalid", "rejected", "inspect_preserved_keys"),
        IdentityDiagnosticOutcome.Cancelled => ("identity_cancelled", "cancelled", "retry_explicitly"),
        IdentityDiagnosticOutcome.ConfigurationInvalid =>
            ("identity_configuration_invalid", "rejected", "correct_configuration"),
        _ => throw Invalid()
    };

    internal static byte[] Serialize(IdentityDiagnosticRecord record)
    {
        Validate(record);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(record, Json);
        if (bytes.Length > 1024) throw Invalid();
        return bytes;
    }

    internal static IdentityDiagnosticRecord Parse(ReadOnlySpan<byte> bytes)
    {
        try
        {
            if (bytes.Length is < 1 or > 1024) throw Invalid();
            var record = JsonSerializer.Deserialize<IdentityDiagnosticRecord>(bytes, Json) ?? throw Invalid();
            Validate(record);
            return record;
        }
        catch (JsonException) { throw Invalid(); }
    }

    internal static void Validate(IdentityDiagnosticRecord record)
    {
        ValidateContext(record.Mode, record.Provider);
        var warning = record.EventCode == "identity_event_capacity_warning";
        if (record.Version != 1 || record.Component != "identity" ||
            !Enum.GetValues<IdentityAction>().Any(action => ActionName(action) == record.Operation) ||
            record.CorrelationId is not { Length: 32 } correlation || !correlation.All(char.IsAsciiHexDigitLower) ||
            record.Durability is not ("unchanged" or "committed" or "uncertain") ||
            record.Outcome == "ready" && record.Durability == "uncertain" ||
            (warning ? record.Outcome != "warning" || record.Guidance != "contact_operator" ||
                record.Retention is not ("installation" or "session") :
                record.Retention is not null || !Enum.GetValues<IdentityDiagnosticOutcome>().Any(outcome =>
                    Rule(outcome) == (record.EventCode, record.Outcome, record.Guidance))))
            throw Invalid();
    }

    private static void ValidateContext(string mode, string provider)
    {
        if (mode is not ("oidc" or "development" or "unknown") ||
            provider is not ("sqlite" or "postgresql" or "unknown")) throw Invalid();
    }

    internal static void Write(IdentityDiagnosticRecord record) =>
        Console.Error.WriteLine(System.Text.Encoding.UTF8.GetString(Serialize(record)));

    internal static void Count(IdentityDiagnosticRecord record)
    {
        Validate(record);
        Outcomes.Add(1, new("operation", record.Operation), new("mode", record.Mode),
            new("provider", record.Provider), new("outcome", record.Outcome));
    }

    internal static void Trace(IdentityDiagnosticRecord record, string phase)
    {
        Validate(record);
        if (phase != "outcome" && !IsPhase(phase)) throw Invalid();
        var context = new ActivityContext(ActivityTraceId.CreateFromString(record.CorrelationId),
            ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded);
        using var activity = Activities.StartActivity("identity." + phase, ActivityKind.Internal, context);
        activity?.SetTag("identity.operation", record.Operation);
        activity?.SetTag("identity.mode", record.Mode);
        activity?.SetTag("identity.provider", record.Provider);
        activity?.SetTag("identity.outcome", record.Outcome);
        activity?.SetTag("identity.code", record.EventCode);
        activity?.SetTag("identity.guidance", record.Guidance);
        activity?.SetTag("identity.durability", record.Durability);
    }

    internal static InvalidOperationException Invalid() => new("identity_diagnostic_invalid");
}

internal sealed class IdentityObservation : IDisposable
{
    private readonly IdentityAction action;
    private readonly string mode, provider;
    private readonly Action<IdentityDiagnosticRecord> sink;
    private readonly IdentityObservation? previous;
    private readonly MutationEvidence evidence = new();
    private IdentityDiagnosticOutcome outcome = IdentityDiagnosticOutcome.Ready;
    private bool published, disposed;
    private int warnings, phases;

    internal IdentityObservation(IdentityAction action, string mode, string provider,
        TimeSpan duration, CancellationToken token, Action<IdentityDiagnosticRecord> sink, IdentityObservation? previous)
    {
        (this.action, this.mode, this.provider, this.sink, this.previous) = (action, mode, provider, sink, previous);
        Budget = new(duration, token);
    }

    internal string CorrelationId { get; } = Guid.NewGuid().ToString("N");
    internal OperationBudget Budget { get; }
    internal IdentityDiagnosticOutcome Outcome => outcome;
    internal StorageDurability Durability => evidence.Durability;
    internal void Select(IdentityDiagnosticOutcome value)
    {
        _ = IdentityDiagnostics.Rule(value);
        outcome = value;
    }
    internal void PossibleCommit() => evidence.PossibleCommit();
    internal void Committed() => evidence.Committed();
    internal void Phase(string phase)
    {
        if (published || ++phases > 32) throw IdentityDiagnostics.Invalid();
        var phaseOutcome = outcome == IdentityDiagnosticOutcome.Ready &&
            evidence.Durability == StorageDurability.Uncertain ? IdentityDiagnosticOutcome.OutcomeUncertain : outcome;
        IdentityDiagnostics.Trace(Record(phaseOutcome), phase);
    }

    internal void Fail(Exception error) => outcome = error switch
    {
        IdentityStateException state => state.Outcome,
        IdentityEventCapacityException => IdentityDiagnosticOutcome.AdmissionExceeded,
        OperationCanceledException => Budget.CallerCancelled
            ? IdentityDiagnosticOutcome.Cancelled : IdentityDiagnosticOutcome.Unavailable,
        StorageException { Code: StorageCode.ReconciliationRequired } => IdentityDiagnosticOutcome.ReconciliationRequired,
        InvalidOperationException { Message: "identity_keys_invalid" } => IdentityDiagnosticOutcome.KeysInvalid,
        _ => IdentityDiagnosticOutcome.Unavailable
    };

    internal void Warning(string retention)
    {
        if (published || ++warnings > 8) throw IdentityDiagnostics.Invalid();
        var record = new IdentityDiagnosticRecord(1, "identity_event_capacity_warning", "identity", IdentityDiagnostics.ActionName(action),
            mode, provider, "warning", CorrelationId, "contact_operator",
            evidence.Durability.ToString().ToLowerInvariant(), retention);
        IdentityDiagnostics.Validate(record);
        sink(record);
    }

    internal void Publish(IdentityDiagnosticOutcome value)
    {
        if (published) return;
        outcome = value;
        var record = Record(outcome);
        published = true;
        IdentityDiagnostics.Validate(record);
        sink(record);
        IdentityDiagnostics.Count(record);
        IdentityDiagnostics.Trace(record, "outcome");
    }

    private IdentityDiagnosticRecord Record(IdentityDiagnosticOutcome value)
    {
        var (code, result, guidance) = IdentityDiagnostics.Rule(value);
        return new(1, code, "identity", IdentityDiagnostics.ActionName(action), mode, provider, result,
            CorrelationId, guidance, evidence.Durability.ToString().ToLowerInvariant());
    }

    public void Dispose()
    {
        if (disposed) return;
        try { Publish(outcome); }
        finally
        {
            disposed = true;
            IdentityDiagnostics.Restore(this, previous);
            Budget.Dispose();
        }
    }
}

internal sealed class IdentityStateException(IdentityDiagnosticOutcome outcome)
    : Exception(IdentityDiagnostics.Rule(outcome).Code)
{
    internal IdentityDiagnosticOutcome Outcome { get; } = outcome;
}
