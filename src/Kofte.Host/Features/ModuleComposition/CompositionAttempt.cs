using Kofte.Platform.Modules.Manifest;

namespace Kofte.Host.Features.ModuleComposition;

public sealed record CompositionOwnerStage(ModuleId Owner, CompositionStage Stage);

public enum CompositionWork
{
    Resolver, Validator, Registrar, Initialize, Start, Health, HealthSweep, StoragePreflight,
    AcquireStorage, ResolveStorage, RegisterStorage, MigrateStorage, Stop, Cleanup
}

public sealed class CompositionAttempt
{
    private readonly TimeProvider clock;
    private readonly CancellationToken cancellation;
    private readonly long started;
    private long cleanupStarted;
    private bool cleaning;
    private bool serving;
    private readonly object progressGate = new();
    private readonly List<CompositionOwnerStage> entered = new(), completed = new();
    private readonly HashSet<CompositionOwnerStage> enteredSet = new(), completedSet = new();
    private readonly HashSet<ModuleId> trackedOwners = new();
    private int cleanupRegistrations;

    public int ContractVersion => 1;
    public string TraceId { get; }
    public CompositionMode Mode { get; }
    public TimeSpan Total { get; }
    public TimeSpan Work { get; }
    public TimeSpan CleanupReserve => Total - Work;
    public bool Publication { get; private set; }
    public CompositionStage Phase { get; private set; } = CompositionStage.Input;
    public CompositionOutcome Outcome { get; private set; } = CompositionOutcome.Progress;
    public string? FailureCode { get; private set; }
    public int CleanupRegistrations { get { lock (progressGate) return cleanupRegistrations; } }
    public IReadOnlyList<CompositionOwnerStage> EnteredOwnerStages
    {
        get { lock (progressGate) return Array.AsReadOnly(entered.ToArray()); }
    }
    public IReadOnlyList<CompositionOwnerStage> CompletedOwnerStages
    {
        get { lock (progressGate) return Array.AsReadOnly(completed.ToArray()); }
    }

    public CompositionAttempt(CompositionDiagnostics diagnostics, CancellationToken cancellation = default,
        TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(diagnostics);
        (TraceId, Mode, this.cancellation, this.clock) =
            (diagnostics.Attempt, diagnostics.Mode, cancellation, clock ?? TimeProvider.System);
        (Total, Work) = Mode switch
        {
            CompositionMode.Validate => (TimeSpan.FromSeconds(45), TimeSpan.FromSeconds(35)),
            CompositionMode.Prepare => (TimeSpan.FromSeconds(120), TimeSpan.FromSeconds(105)),
            CompositionMode.Serve => (TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(45)),
            _ => throw new CompositionBoundaryException("composition_observation_invalid")
        };
        started = this.clock.GetTimestamp();
    }

    public TimeSpan RemainingWork => Positive(Work - clock.GetElapsedTime(started));

    public void EnterPhase(CompositionStage stage)
    {
        if (!Enum.IsDefined(stage)) throw new CompositionBoundaryException("composition_observation_invalid");
        lock (progressGate)
        {
            Phase = stage;
            if (FailureCode is null) Outcome = CompositionOutcome.Progress;
        }
    }

    public void CompletePhase(CompositionStage stage)
    {
        EnterPhase(stage);
        lock (progressGate)
            if (FailureCode is null) Outcome = CompositionOutcome.Passed;
    }

    public void FailPhase(CompositionStage stage, CompositionOutcome outcome, string code)
    {
        if (!Enum.IsDefined(stage) || !Enum.IsDefined(outcome) ||
            outcome is CompositionOutcome.Passed or CompositionOutcome.Progress)
            throw new CompositionBoundaryException("composition_observation_invalid");
        CompositionDiagnostics.RequireCode(code);
        lock (progressGate)
        {
            Phase = stage;
            if (FailureCode is null || outcome == CompositionOutcome.Incomplete)
                (Outcome, FailureCode) = (outcome, code);
        }
    }

    public void EnterOwnerStage(ModuleId owner, CompositionStage stage)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (stage is not (CompositionStage.Discovered or CompositionStage.Validated or CompositionStage.Registered or
            CompositionStage.Initialized or CompositionStage.Migrated or CompositionStage.Started or
            CompositionStage.Health or CompositionStage.Stopping or CompositionStage.Stopped or CompositionStage.Cleanup))
            throw new CompositionBoundaryException("composition_observation_invalid");
        lock (progressGate)
        {
            if (!trackedOwners.Contains(owner) && trackedOwners.Count >= 32)
                throw new CompositionBoundaryException("composition_observation_invalid");
            trackedOwners.Add(owner);
            var entry = new CompositionOwnerStage(owner, stage);
            if (!enteredSet.Add(entry))
            {
                if (stage != CompositionStage.Health)
                    throw new CompositionBoundaryException("composition_observation_invalid");
                return;
            }
            entered.Add(entry);
        }
    }

    public void CompleteOwnerStage(ModuleId owner, CompositionStage stage)
    {
        lock (progressGate)
        {
            var entry = new CompositionOwnerStage(owner, stage);
            if (!enteredSet.Contains(entry))
                throw new CompositionBoundaryException("composition_observation_invalid");
            if (completedSet.Add(entry)) completed.Add(entry);
        }
    }

    internal bool AdmitCleanup()
    {
        lock (progressGate)
        {
            if (cleaning || Mode == CompositionMode.Validate || cleanupRegistrations >= 512) return false;
            cleanupRegistrations++;
            return true;
        }
    }

    public TimeSpan Clip(CompositionWork operation)
    {
        if (cleaning)
        {
            if (operation is not (CompositionWork.Stop or CompositionWork.Cleanup))
                throw new CompositionBoundaryException("composition_observation_invalid");
            return Minimum(Cap(operation), CooperativeRemaining());
        }
        if (serving && operation is CompositionWork.Health or CompositionWork.HealthSweep or CompositionWork.ResolveStorage)
        {
            if (cancellation.IsCancellationRequested)
                throw new CompositionBoundaryException("composition_cancelled");
            return Cap(operation);
        }
        Check();
        return Minimum(Cap(operation), RemainingWork);
    }

    public void Check()
    {
        if (cancellation.IsCancellationRequested)
            throw new CompositionBoundaryException("composition_cancelled");
        if (cleaning || RemainingWork <= TimeSpan.Zero)
            throw new CompositionBoundaryException("composition_deadline_exceeded");
    }

    internal void Publish(Action? publish = null)
    {
        Check();
        if (Mode != CompositionMode.Serve || Publication)
            throw new CompositionBoundaryException("composition_observation_invalid");
        publish?.Invoke();
        Publication = true;
        serving = true;
        CompletePhase(CompositionStage.Published);
    }

    public void BeginCleanup()
    {
        Publication = false;
        if (cleaning) throw new CompositionBoundaryException("composition_observation_invalid");
        cleaning = true;
        cleanupStarted = clock.GetTimestamp();
        EnterPhase(CompositionStage.Stopping);
    }

    public TimeSpan RemainingTermination()
    {
        if (!cleaning) throw new CompositionBoundaryException("composition_observation_invalid");
        return Minimum(TimeSpan.FromSeconds(5), RemainingTotal());
    }

    private TimeSpan CooperativeRemaining()
    {
        var reserve = serving ? TimeSpan.FromSeconds(15) : CleanupReserve;
        return Minimum(Positive(reserve - TimeSpan.FromSeconds(5) - clock.GetElapsedTime(cleanupStarted)),
            Positive(RemainingTotal() - TimeSpan.FromSeconds(5)));
    }

    private TimeSpan RemainingTotal() => serving
        ? Positive(TimeSpan.FromSeconds(15) - clock.GetElapsedTime(cleanupStarted))
        : Minimum(Positive(Total - clock.GetElapsedTime(started)),
            Positive(CleanupReserve - clock.GetElapsedTime(cleanupStarted)));

    public static TimeSpan Cap(CompositionWork operation) => TimeSpan.FromSeconds(operation switch
    {
        CompositionWork.Resolver or CompositionWork.Initialize or CompositionWork.Start or
            CompositionWork.AcquireStorage => 2,
        CompositionWork.Validator => 0.25,
        CompositionWork.Registrar or CompositionWork.Health or CompositionWork.Stop or CompositionWork.Cleanup => 1,
        CompositionWork.HealthSweep => 20,
        CompositionWork.StoragePreflight or CompositionWork.RegisterStorage => 30,
        CompositionWork.ResolveStorage => 3,
        CompositionWork.MigrateStorage => 120,
        _ => throw new CompositionBoundaryException("composition_observation_invalid")
    });

    private static TimeSpan Positive(TimeSpan value) => value > TimeSpan.Zero ? value : TimeSpan.Zero;
    private static TimeSpan Minimum(TimeSpan first, TimeSpan second) => first < second ? first : second;
}
