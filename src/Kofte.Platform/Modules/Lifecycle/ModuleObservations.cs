using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.Modules.Lifecycle;

public enum ModuleLifecycleStage
{
    Discovered, Validated, Registered, Initialized, Migrated, Enabled, Health, Stopping, Stopped
}
public enum ModuleLifecycleOutcome { Pending, Completed, Disabled, Blocked, Failed, Cancelled, Incomplete }
public enum CompositionAvailability { Empty, Starting, Ready, Unavailable, Stopped }
public enum ModuleObservationCode
{
    None, Disabled, Validated, Registered, Initialized, Migrated, Enabled, Healthy,
    HealthDegraded, HealthFailed, HealthUnknown, DependencyUnavailable, Blocked, Failed,
    Cancelled, DeadlineExceeded, CleanupIncomplete, ReconciliationRequired, Stopped
}

public sealed class ModuleObservation
{
    public int ContractVersion => 1;
    public ModuleId Owner { get; }
    public bool DesiredEnabled { get; }
    public ModuleLifecycleStage Stage { get; }
    public ModuleLifecycleOutcome Outcome { get; }
    public ModuleHealth OwnHealth { get; }
    public ModuleHealth EffectiveHealth { get; }
    public string AttemptId { get; }
    public TimeSpan ObservedElapsed { get; }
    public IReadOnlyList<ModuleId> AffectedPrerequisites { get; }
    public ModuleObservationCode Code { get; }
    public string Guidance => Code switch
    {
        ModuleObservationCode.Disabled => "edit_selection_while_stopped",
        ModuleObservationCode.Healthy or ModuleObservationCode.Enabled => "none",
        ModuleObservationCode.HealthDegraded or ModuleObservationCode.HealthFailed or
            ModuleObservationCode.HealthUnknown or ModuleObservationCode.DependencyUnavailable => "inspect_current_health",
        ModuleObservationCode.Cancelled or ModuleObservationCode.DeadlineExceeded => "verify_owned_exit_before_retry",
        ModuleObservationCode.Failed or ModuleObservationCode.CleanupIncomplete or
            ModuleObservationCode.ReconciliationRequired => "inspect_preserved_state",
        ModuleObservationCode.Blocked => "correct_selection_and_restart",
        _ => "observe_current_attempt"
    };

    public ModuleObservation(ModuleId owner, bool desiredEnabled, ModuleLifecycleStage stage,
        ModuleLifecycleOutcome outcome, ModuleHealth ownHealth, ModuleHealth effectiveHealth,
        string attemptId, TimeSpan observedElapsed, IEnumerable<ModuleId> affectedPrerequisites,
        ModuleObservationCode code)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ObservationValidation.Attempt(attemptId);
        if (!Enum.IsDefined(stage) || !Enum.IsDefined(outcome) || !Enum.IsDefined(ownHealth)
            || !Enum.IsDefined(effectiveHealth) || !Enum.IsDefined(code) || observedElapsed < TimeSpan.Zero)
            throw new ArgumentException("Invalid module observation.");
        var affected = CompositionCopies.Bounded(affectedPrerequisites, 16);
        if (affected.Count > 16 || affected.Any(id => id is null)
            || affected.Distinct().Count() != affected.Count)
            throw new ArgumentException("Invalid prerequisite observations.");
        (Owner, DesiredEnabled, Stage, Outcome, OwnHealth, EffectiveHealth, AttemptId, ObservedElapsed, Code) =
            (owner, desiredEnabled, stage, outcome, ownHealth, effectiveHealth, attemptId, observedElapsed, code);
        AffectedPrerequisites = Array.AsReadOnly(affected.OrderBy(id => id.Value, StringComparer.Ordinal).ToArray());
    }
}

public sealed class CompositionSnapshot
{
    public int ContractVersion => 1;
    public string AttemptId { get; }
    public CompositionAvailability Availability { get; }
    public bool Published { get; }
    public TimeSpan Elapsed { get; }
    public IReadOnlyList<ModuleObservation> Modules { get; }

    public CompositionSnapshot(string attemptId, CompositionAvailability availability, bool published,
        TimeSpan elapsed, IEnumerable<ModuleObservation> modules)
    {
        ObservationValidation.Attempt(attemptId);
        if (!Enum.IsDefined(availability) || elapsed < TimeSpan.Zero)
            throw new ArgumentException("Invalid composition observation.");
        var copy = CompositionCopies.Bounded(modules, 32);
        if (copy.Count > 32 || copy.Any(m => m is null || m.AttemptId != attemptId || m.ObservedElapsed > elapsed)
            || copy.Select(m => m.Owner).Distinct().Count() != copy.Count)
            throw new ArgumentException("Invalid composition observation catalog.");
        (AttemptId, Availability, Published, Elapsed) = (attemptId, availability, published, elapsed);
        Modules = Array.AsReadOnly(copy.OrderBy(m => m.Owner.Value, StringComparer.Ordinal).ToArray());
    }
}

internal static class ObservationValidation
{
    internal static void Attempt(string? attemptId)
    {
        if (attemptId is null || attemptId.Length != 32
            || !attemptId.All(c => c is >= 'a' and <= 'f' or >= '0' and <= '9'))
            throw new ArgumentException("Invalid attempt identity.");
    }
}
