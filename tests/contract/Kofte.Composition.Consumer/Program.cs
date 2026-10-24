using System.Diagnostics;
using System.Text.Json;
using Kofte.Composition.Consumer;
using Kofte.Composition.Consumer.Scenarios;
using Kofte.TestSupport;

if (args is ["--composition-fixture-descendant"])
{
    await Task.Delay(Timeout.InfiniteTimeSpan, CancellationToken.None);
    return 1;
}

if (args.FirstOrDefault() == "--deployment")
    return await DeploymentProgram.RunAsync(args[1..]);
if (args.FirstOrDefault() == "--modules-worker")
    return await DeploymentProgram.RunAsync(args);

var code = "composition_scenario_required";
string? provider = null;
string? scenario = null;
if (args is ["--provider", var selectedProvider, "--scenario", var selectedScenario])
{
    try
    {
        provider = CompositionScenarioCatalog.RequireProvider(selectedProvider);
        scenario = CompositionScenarioCatalog.RequireScenario(selectedScenario);
        code = "composition_scenarios_unimplemented";
    }
    catch (InvalidOperationException error) when (error.Message is
        "composition_test_provider_required" or "composition_scenario_required")
    {
        code = error.Message;
    }
}
if (provider is not null && scenario is not null)
{
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(580));
    try
    {
        var completed = new List<ScenarioEvidence>();
        foreach (var name in scenario == "all" ? CompositionScenarioCatalog.Scenarios : [scenario])
        {
            var evidence = name switch
            {
                "invalid-composition" => await ValidationScenarios.InvalidAsync(provider, deadline.Token),
                "deterministic-corpus" => await ValidationScenarios.DeterministicAsync(deadline.Token),
                "empty-boot" => await BootScenarios.EmptyAsync(provider, deadline.Token),
                "valid-dependency" => await BootScenarios.DependencyAsync(provider, deadline.Token),
                "disabled-missing" => await BootScenarios.DisabledMissingAsync(provider, deadline.Token),
                "ordered-migration" => await MigrationScenarios.OrderedAsync(provider, deadline.Token),
                "disable-reenable" => await PreservationScenarios.CyclesAsync(provider, deadline.Token),
                "health-impact" => await HealthScenarios.ImpactAsync(provider, deadline.Token),
                "bounded-stop" => await StopScenarios.BoundedAsync(provider, deadline.Token),
                "failure-correction" => await CorrectionScenarios.CorrectAsync(provider, deadline.Token),
                "private-diagnostics" => await DiagnosticScenarios.PrivateAsync(provider, deadline.Token),
                _ => throw new ScenarioFailure("composition_scenario_required")
            };
            evidence.Complete();
            completed.Add(evidence);
        }
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            eventCode = "composition_scenarios_passed",
            attempt = ActivityTraceId.CreateRandom().ToHexString(),
            provider,
            scenario,
            status = "passed",
            assertions = completed.Sum(evidence => evidence.Assertions),
            scenarios = completed.ToDictionary(evidence => evidence.Name,
                evidence => new { assertions = evidence.Assertions, observations = evidence.Observations }, StringComparer.Ordinal)
        }));
        return 0;
    }
    catch (ScenarioFailure error) { code = error.Code; }
    catch (Exception) { code = "composition_test_execution_failed"; }
}
Console.Error.WriteLine(JsonSerializer.Serialize(new
{
    schemaVersion = 1,
    eventCode = code,
    attempt = ActivityTraceId.CreateRandom().ToHexString(),
    provider,
    scenario,
    status = "failed",
    assertions = 0
}));
return 1;
