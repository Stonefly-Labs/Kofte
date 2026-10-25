using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Lifecycle;

namespace Kofte.Host.Features.ModuleComposition;

public enum CompositionMode { Validate, Prepare, Serve }
public enum CompositionStage
{
    Input, Admission, Discovered, Validated, Registered, Initialized, Migrated,
    Started, Health, Published, Stopping, Stopped, Cleanup
}
public enum CompositionOutcome { Progress, Passed, Rejected, Busy, Failed, Deadline, Cancelled, ReconciliationRequired, Incomplete, Disabled }

public class CompositionBoundaryException : Exception
{
    public string Code { get; }
    public CompositionBoundaryException(string code) : base(CompositionDiagnostics.RequireCode(code)) => Code = code;
}

public sealed class CompositionRecord
{
    public int Version { get; }
    public string Attempt { get; }
    public string Mode { get; }
    public string Stage { get; }
    public string? Owner { get; }
    public string Code { get; }
    public string Outcome { get; }
    public string Guidance { get; }
    public long ElapsedMilliseconds { get; }
    public string Provider { get; }
    public IReadOnlyList<string> RelatedIds { get; }
    public string? StorageCorrelation { get; }
    public string? Durability { get; }
    public string? FieldToken { get; }
    public string? FieldReason { get; }

    public CompositionRecord(int version, string attempt, string mode, string stage, string? owner, string code,
        string outcome, string guidance, long elapsedMilliseconds, string provider,
        IReadOnlyList<string> relatedIds, string? storageCorrelation = null, string? durability = null,
        string? fieldToken = null, string? fieldReason = null)
    {
        if (relatedIds is null || relatedIds.Count > 8)
            throw new CompositionBoundaryException("composition_observation_invalid");
        (Version, Attempt, Mode, Stage, Owner, Code, Outcome, Guidance, ElapsedMilliseconds, Provider,
            StorageCorrelation, Durability) =
            (version, attempt, mode, stage, owner, code, outcome, guidance, elapsedMilliseconds, provider,
                storageCorrelation, durability);
        RelatedIds = Array.AsReadOnly(relatedIds.ToArray());
        (FieldToken, FieldReason) = (fieldToken, fieldReason);
    }
}

public sealed class CompositionDiagnostics
{
    public const string SourceName = "Kofte.Composition";
    private static readonly ActivitySource Activities = new(SourceName);
    private static readonly Meter Meter = new(SourceName);
    private static readonly Counter<long> Records = Meter.CreateCounter<long>("kofte.composition.records");
    private static readonly Histogram<double> Elapsed = Meter.CreateHistogram<double>("kofte.composition.elapsed", "ms");
    private readonly Action<CompositionRecord> write;
    private readonly TimeProvider clock;
    private readonly long started;
    private readonly ActivityContext context;
    private long windowStarted;
    private int count, steadyCount;
    private bool steady, terminal;

    public string Attempt { get; }
    public CompositionMode Mode { get; }
    public string Provider { get; }

    public CompositionDiagnostics(CompositionMode mode, string provider, Action<CompositionRecord> write,
        TimeProvider? clock = null, string? attemptId = null)
    {
        if (!Enum.IsDefined(mode) || provider is not ("sqlite" or "postgresql" or "none" or "unknown"))
            throw new CompositionBoundaryException("composition_observation_invalid");
        ArgumentNullException.ThrowIfNull(write);
        (Mode, Provider, this.write, this.clock) = (mode, provider, write, clock ?? TimeProvider.System);
        if (attemptId is not null && !IsToken(attemptId))
            throw new CompositionBoundaryException("composition_observation_invalid");
        Attempt = attemptId ?? ActivityTraceId.CreateRandom().ToHexString();
        context = new(ActivityTraceId.CreateFromString(Attempt.AsSpan()), ActivitySpanId.CreateRandom(),
            ActivityTraceFlags.Recorded);
        started = this.clock.GetTimestamp();
        windowStarted = started;
    }

    public CompositionRecord Emit(CompositionStage stage, CompositionOutcome outcome,
        string code, ModuleId? owner = null, IReadOnlyList<ModuleId>? related = null,
        string? storageCorrelation = null, string? durability = null, bool isTerminal = false,
        string? fieldToken = null, string? fieldReason = null)
    {
        RequireCode(code);
        ValidateFieldEvidence(StageName(stage), OutcomeName(outcome), code, owner?.Value, fieldToken, fieldReason);
        if (!Enum.IsDefined(stage) || !Enum.IsDefined(outcome) || related?.Count > 8 ||
            storageCorrelation is not null && !IsToken(storageCorrelation) ||
            durability is not null && durability is not ("unchanged" or "committed" or "uncertain") ||
            (storageCorrelation is null) != (durability is null))
            throw new CompositionBoundaryException("composition_observation_invalid");
        if (terminal) throw new CompositionBoundaryException("composition_observation_invalid");
        if (!isTerminal)
        {
            if (!steady && count >= 256) throw new CompositionBoundaryException("composition_output_limit");
            if (steady)
            {
                if (clock.GetElapsedTime(windowStarted) >= TimeSpan.FromMinutes(1))
                {
                    windowStarted = clock.GetTimestamp();
                    steadyCount = 0;
                }
                if (steadyCount >= 64) throw new CompositionBoundaryException("composition_output_limit");
                steadyCount++;
            }
            else count++;
        }
        else terminal = true;
        var record = new CompositionRecord(1, Attempt, ModeName(Mode), StageName(stage), owner?.Value,
            code, OutcomeName(outcome), Guidance(code),
            Math.Max(0, (long)clock.GetElapsedTime(started).TotalMilliseconds), Provider,
            Array.AsReadOnly((related ?? []).Select(id => id.Value).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToArray()), storageCorrelation, durability, fieldToken, fieldReason);
        try { write(record); }
        catch (Exception error) when (error is IOException or ObjectDisposedException or
            InvalidOperationException or UnauthorizedAccessException or JsonException)
        {
            throw new CompositionBoundaryException("composition_output_failed");
        }
        using var activity = Activities.StartActivity("composition." + record.Stage, ActivityKind.Internal, context);
        activity?.SetTag("composition.code", record.Code);
        activity?.SetTag("composition.guidance", record.Guidance);
        activity?.SetTag("composition.outcome", record.Outcome);
        activity?.AddEvent(new ActivityEvent("composition.transition", tags: new ActivityTagsCollection
        {
            { "composition.code", record.Code },
            { "composition.guidance", record.Guidance },
            { "composition.outcome", record.Outcome }
        }));
        var tags = new TagList
        {
            { "mode", record.Mode }, { "stage", record.Stage },
            { "provider", record.Provider }, { "outcome", record.Outcome }
        };
        Records.Add(1, tags);
        Elapsed.Record(record.ElapsedMilliseconds, tags);
        return record;
    }

    public void BeginSteadyState()
    {
        if (terminal || steady) throw new CompositionBoundaryException("composition_observation_invalid");
        steady = true;
        windowStarted = clock.GetTimestamp();
    }

    internal static bool IsToken(string? text) => text is { Length: 32 } &&
        text.All(value => value is >= '0' and <= '9' or >= 'a' and <= 'f');

    internal static void ValidateFieldEvidence(string stage, string outcome, string code, string? owner,
        string? fieldToken, string? fieldReason)
    {
        if ((fieldToken is null) != (fieldReason is null) || fieldToken is not null &&
            (owner is null || stage != "validated" || outcome != "rejected" || code != "composition_setting_invalid" ||
                fieldReason is not ("required" or "type" or "unknown" or "constraint")))
            throw new CompositionBoundaryException("composition_observation_invalid");
        if (fieldToken is null) return;
        try { _ = new ModuleConfigurationField(fieldToken, [fieldToken], ModuleSettingType.String); }
        catch (ArgumentException) { throw new CompositionBoundaryException("composition_observation_invalid"); }
    }

    internal static string RequireCode(string code) => code switch
    {
        "composition_progress" or "composition_ready" or "composition_disabled" or "composition_configuration_invalid" or
        "composition_admission_invalid" or "composition_observation_invalid" or "composition_output_limit" or
        "composition_output_failed" or "composition_input_limit" or "composition_contract_unsupported" or
        "composition_manifest_rejected" or "composition_module_duplicate" or
        "composition_implementation_missing" or "composition_dependency_missing" or
        "composition_dependency_disabled" or "composition_dependency_incompatible" or
        "composition_dependency_cycle" or "composition_setting_invalid" or
        "composition_contribution_conflict" or "composition_contribution_unsupported" or
        "composition_storage_rejected" or "composition_prepare_required" or
        "composition_registration_failed" or "composition_initialization_failed" or
        "composition_activation_failed" or "composition_health_degraded" or "composition_health_failed" or
        "composition_health_unknown" or "composition_deadline_exceeded" or "composition_cancelled" or
        "composition_host_stop_incomplete" or "composition_host_dispose_incomplete" or
        "composition_services_dispose_incomplete" or "composition_storage_dispose_incomplete" or
        "composition_cleanup_incomplete" or "composition_reconciliation_required" or
        "composition_diagnostics_truncated" => code,
        _ => throw new ArgumentOutOfRangeException(nameof(code), "composition_code_invalid")
    };

    internal static string Guidance(string code) => code switch
    {
        "composition_progress" => "observe_current_attempt",
        "composition_ready" => "none",
        "composition_disabled" => "edit_selection_while_stopped",
        "composition_prepare_required" => "prepare_while_stopped",
        "composition_storage_rejected" or "composition_reconciliation_required" or
            "composition_host_stop_incomplete" or "composition_host_dispose_incomplete" or
            "composition_services_dispose_incomplete" or "composition_storage_dispose_incomplete" or
            "composition_cleanup_incomplete" => "inspect_preserved_state",
        "composition_health_degraded" or "composition_health_failed" or
            "composition_health_unknown" => "inspect_current_health",
        "composition_output_failed" or "composition_output_limit" or "composition_observation_invalid" =>
            "restore_private_diagnostics",
        "composition_deadline_exceeded" or "composition_cancelled" => "verify_owned_exit_before_retry",
        "composition_admission_invalid" => "use_supervised_entry",
        _ => "correct_selection_and_restart"
    };

    internal static string ModeName(CompositionMode mode) => mode.ToString().ToLowerInvariant();
    internal static string StageName(CompositionStage stage) => stage.ToString().ToLowerInvariant();
    internal static string OutcomeName(CompositionOutcome outcome) => outcome.ToString().ToLowerInvariant();

    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        AllowDuplicateProperties = false,
        MaxDepth = 8
    };
}
