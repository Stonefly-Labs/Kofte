using Kofte.TestSupport;

namespace Kofte.Composition.Consumer.Scenarios;

internal sealed class ScenarioFailure(string code) : Exception(code)
{
    internal string Code { get; } = code;
}

internal sealed class ScenarioEvidence
{
    private readonly Dictionary<string, int> observations;
    internal string Name { get; }
    public int Assertions { get; private set; }
    public IReadOnlyDictionary<string, int> Observations => observations.AsReadOnly();

    internal ScenarioEvidence(string name)
    {
        Name = name;
        observations = CompositionScenarioCatalog.RequiredObservations(name).Keys
            .ToDictionary(key => key, _ => 0, StringComparer.Ordinal);
    }

    internal void Require(bool condition, string code)
    {
        if (!condition) throw new ScenarioFailure(code);
        Assertions = checked(Assertions + 1);
    }

    internal void Observed(string name, int count = 1)
    {
        if (count < 0 || !observations.ContainsKey(name))
            throw new ScenarioFailure("composition_test_observation_invalid");
        observations[name] = checked(observations[name] + count);
    }

    internal void Complete()
    {
        Require(Assertions > 0 && observations.All(pair =>
                pair.Value >= CompositionScenarioCatalog.RequiredObservations(Name)[pair.Key]),
            "composition_test_observation_missing");
    }
}
