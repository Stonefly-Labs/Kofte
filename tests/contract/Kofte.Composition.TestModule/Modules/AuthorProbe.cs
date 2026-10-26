namespace Kofte.Composition.TestModule.Modules;

public sealed record AuthorObservation(string Owner, string Phase, int Value, string Attempt, int Worker);

public static class AuthorProbe
{
    private static readonly object gate = new();
    private static readonly List<AuthorObservation> records = [];
    private static readonly TaskCompletionSource admitted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static readonly TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private static string attempt = "";

    public static Task Admitted => admitted.Task;
    public static bool HoldBeta { get; set; }

    public static void Record(string owner, string phase, int value = 1, string? attemptId = null)
    {
        lock (gate)
        {
            if (records.Count >= 192) throw new InvalidOperationException("composition_author_probe_limit");
            if (attemptId is not null)
            {
                if (attempt.Length != 0 && attempt != attemptId)
                    throw new InvalidOperationException("composition_author_attempt_changed");
                attempt = attemptId;
            }
            records.Add(new(owner, phase, value, attempt, Environment.ProcessId));
            if (attempt.Length != 0) admitted.TrySetResult();
        }
    }

    public static IReadOnlyList<AuthorObservation> ReadFrom(int offset)
    {
        lock (gate)
            return records.Skip(offset).Select(record => record with { Attempt = attempt }).ToArray();
    }

    public static IReadOnlyList<AuthorObservation> Totals()
    {
        lock (gate)
        {
            var totals = new List<AuthorObservation>();
            foreach (var owner in BootModules.Owners)
            {
                var own = records.Where(record => record.Owner == owner).ToArray();
                foreach (var (phase, count) in new[]
                {
                    ("callback-total", own.Count(record => record.Phase is "validate" or "register" or
                        "service-constructor" or "runtime-constructor" or "initialize" or "start" or "health" or
                        "stop" or "cleanup" or "dispose" or "migration")),
                    ("constructor-total", own.Count(record => record.Phase is "service-constructor" or "runtime-constructor")),
                    ("registrar-total", own.Count(record => record.Phase == "register")),
                    ("runtime-total", own.Count(record => record.Phase is "initialize" or "start" or "health" or "stop" or "dispose")),
                    ("cleanup-total", own.Count(record => record.Phase == "cleanup")),
                    ("migration-total", own.Count(record => record.Phase == "migration"))
                })
                    totals.Add(new(owner, phase, count, attempt, Environment.ProcessId));
            }
            return totals;
        }
    }

    public static Task WaitForReleaseAsync(CancellationToken token) => release.Task.WaitAsync(token);
    public static void Release() => release.TrySetResult();
}
