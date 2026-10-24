using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Host.Features.ModuleComposition;

public sealed class CompositionHealth
{
    private sealed record Observation(ModuleHealth Health, long At);
    private sealed record Lifecycle(ModuleLifecycleStage Stage, ModuleLifecycleOutcome Outcome, ModuleObservationCode Code);
    private sealed record State(IReadOnlyDictionary<ModuleId, Observation> Observations,
        IReadOnlyDictionary<ModuleId, Lifecycle> Owners, bool Published,
        ModuleLifecycleStage Stage, bool Withdrawn = false);
    private readonly ResolvedComposition composition;
    private readonly string attempt;
    private readonly TimeProvider clock;
    private readonly long started;
    private State state = new(new Dictionary<ModuleId, Observation>(), new Dictionary<ModuleId, Lifecycle>(),
        false, ModuleLifecycleStage.Validated);

    public CompositionHealth(ResolvedComposition composition, string attempt, TimeProvider? clock = null)
    {
        if (!CompositionDiagnostics.IsToken(attempt))
            throw new CompositionBoundaryException("composition_observation_invalid");
        (this.composition, this.attempt, this.clock) = (composition, attempt, clock ?? TimeProvider.System);
        started = this.clock.GetTimestamp();
    }

    public bool IsReady => Snapshot.Availability is CompositionAvailability.Ready or CompositionAvailability.Empty;

    public CompositionSnapshot Snapshot => CreateSnapshot(Volatile.Read(ref state));

    private CompositionSnapshot CreateSnapshot(State current)
    {
        var now = clock.GetTimestamp();
        var elapsed = clock.GetElapsedTime(started, now);
        var result = new Dictionary<ModuleId, ModuleObservation>();
        foreach (var owner in composition.Order)
        {
            var sample = current.Observations.GetValueOrDefault(owner);
            var own = sample is null || clock.GetElapsedTime(sample.At, now) >= TimeSpan.FromSeconds(30)
                ? ModuleHealth.Unknown : sample.Health;
            var effective = own;
            var affected = new HashSet<ModuleId>();
            foreach (var dependency in composition.Dependencies.Where(d => d.Owner.Equals(owner)))
            {
                var prerequisite = result[dependency.Prerequisite];
                effective = Worst(effective, prerequisite.EffectiveHealth);
                if (prerequisite.EffectiveHealth != ModuleHealth.Healthy)
                {
                    affected.Add(prerequisite.Owner);
                    affected.UnionWith(prerequisite.AffectedPrerequisites);
                }
            }
            var lifecycle = current.Owners.GetValueOrDefault(owner) ??
                new(ModuleLifecycleStage.Validated, ModuleLifecycleOutcome.Completed, ModuleObservationCode.Validated);
            var healthStage = !current.Withdrawn && (current.Published || sample is not null &&
                lifecycle.Stage == ModuleLifecycleStage.Health);
            var code = !healthStage ? lifecycle.Code : effective switch
            {
                ModuleHealth.Healthy => ModuleObservationCode.Healthy,
                ModuleHealth.Degraded => ModuleObservationCode.HealthDegraded,
                ModuleHealth.Failed => ModuleObservationCode.HealthFailed,
                _ => ModuleObservationCode.HealthUnknown
            };
            var outcome = !healthStage ? lifecycle.Outcome : effective == ModuleHealth.Healthy
                ? ModuleLifecycleOutcome.Completed : own == ModuleHealth.Healthy
                    ? ModuleLifecycleOutcome.Blocked : ModuleLifecycleOutcome.Failed;
            result.Add(owner, new(owner, true, lifecycle.Stage, outcome, own, effective,
                attempt, sample is null ? TimeSpan.Zero : clock.GetElapsedTime(started, sample.At),
                affected, code));
        }
        foreach (var owner in composition.Disabled)
            result.Add(owner, new(owner, false, ModuleLifecycleStage.Discovered, ModuleLifecycleOutcome.Disabled,
                ModuleHealth.Unknown, ModuleHealth.Unknown, attempt, TimeSpan.Zero, [], ModuleObservationCode.Disabled));
        var availability = !current.Published ? CompositionAvailability.Starting :
            composition.Enabled.Count == 0 ? CompositionAvailability.Empty :
            result.Values.Where(m => m.DesiredEnabled).All(m => m.EffectiveHealth == ModuleHealth.Healthy)
                ? CompositionAvailability.Ready : CompositionAvailability.Unavailable;
        if (current.Stage is ModuleLifecycleStage.Stopping or ModuleLifecycleStage.Stopped)
            availability = CompositionAvailability.Stopped;
        return new(attempt, availability, current.Published, elapsed, result.Values);
    }

    internal void Transition(ModuleLifecycleStage stage) =>
        Update(current => current.Withdrawn && stage is not (ModuleLifecycleStage.Stopping or ModuleLifecycleStage.Stopped)
            ? current : current with { Stage = stage });

    internal void TransitionOwner(ModuleId owner, ModuleLifecycleStage stage,
        ModuleLifecycleOutcome outcome, ModuleObservationCode code)
    {
        if (!composition.Enabled.Contains(owner) || !Enum.IsDefined(stage) || !Enum.IsDefined(outcome) ||
            !Enum.IsDefined(code))
            throw new CompositionBoundaryException("composition_observation_invalid");
        Update(current =>
        {
            var owners = current.Owners.ToDictionary(pair => pair.Key, pair => pair.Value);
            owners[owner] = new(stage, outcome, code);
            return current with { Owners = owners };
        });
    }

    public void Observe(ModuleId owner, ModuleHealthResult health)
    {
        if (!composition.Enabled.Contains(owner))
            throw new CompositionBoundaryException("composition_observation_invalid");
        Update(current =>
        {
            var copy = current.Observations.ToDictionary(pair => pair.Key, pair => pair.Value);
            copy[owner] = new(health.Health, clock.GetTimestamp());
            var owners = current.Owners.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!current.Withdrawn)
                owners[owner] = new(ModuleLifecycleStage.Health, ModuleLifecycleOutcome.Completed, ModuleObservationCode.Healthy);
            return current with { Observations = copy, Owners = owners };
        });
    }

    public void Publish()
    {
        Update(current =>
        {
            if (current.Published || current.Withdrawn)
                throw new CompositionBoundaryException("composition_observation_invalid");
            var unavailable = CreateSnapshot(current).Modules.Where(module =>
                module.DesiredEnabled && module.EffectiveHealth != ModuleHealth.Healthy).ToArray();
            if (unavailable.Length != 0)
                throw new CompositionBoundaryException(unavailable.Any(module => module.EffectiveHealth == ModuleHealth.Failed)
                    ? "composition_health_failed" : unavailable.Any(module => module.EffectiveHealth == ModuleHealth.Unknown)
                        ? "composition_health_unknown" : "composition_health_degraded");
            return current with
            {
                Published = true,
                Stage = ModuleLifecycleStage.Enabled,
                Owners = composition.Order.ToDictionary(owner => owner, _ => new Lifecycle(
                    ModuleLifecycleStage.Enabled, ModuleLifecycleOutcome.Completed, ModuleObservationCode.Enabled))
            };
        });
    }

    public void Withdraw() =>
        Update(current => current with { Published = false, Stage = ModuleLifecycleStage.Stopping, Withdrawn = true });

    private void Update(Func<State, State> update)
    {
        State current, next;
        do
        {
            current = Volatile.Read(ref state);
            next = update(current);
        } while (!ReferenceEquals(Interlocked.CompareExchange(ref state, next, current), current));
    }

    private static ModuleHealth Worst(ModuleHealth left, ModuleHealth right) =>
        Rank(left) >= Rank(right) ? left : right;

    private static int Rank(ModuleHealth health) => health switch
    {
        ModuleHealth.Failed => 3,
        ModuleHealth.Unknown => 2,
        ModuleHealth.Degraded => 1,
        _ => 0
    };
}
