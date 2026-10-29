using System.Diagnostics;
using System.Text.Json;
using Kofte.Identity.Consumer;
using Kofte.Identity.Consumer.Scenarios;
using Kofte.TestSupport.Identity;

if (args is ["--identity-fixture-worker"]) return await FixtureProgram.RunAsync();
if (args is ["--modules-worker", "serve"]) return await BootstrapProgram.RunAsync(args);

string? scenario = null;
string? provider = null;
int? developmentFailureLine = null;
var code = "identity_scenario_required";
if (args.Length == 4)
{
    var values = new Dictionary<string, string>(StringComparer.Ordinal);
    for (var i = 0; i < args.Length; i += 2)
    {
        if (args[i] is not ("--scenario" or "--provider") || !values.TryAdd(args[i], args[i + 1]))
        {
            values.Clear();
            break;
        }
    }
    if (values.TryGetValue("--scenario", out var selectedScenario) &&
        values.TryGetValue("--provider", out var selectedProvider))
    {
        try
        {
            scenario = IdentityScenarioCatalog.RequireScenario(selectedScenario);
            provider = IdentityScenarioCatalog.RequireProvider(selectedProvider);
            code = "identity_scenario_unimplemented";
        }
        catch (InvalidOperationException error) when (error.Message is
            "identity_scenario_required" or "storage_test_provider_required")
        {
            code = error.Message;
        }
    }
}
if (code == "identity_scenario_unimplemented")
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(scenario == "all" ? 600 : 120));
    try
    {
        IdentityConsumerHarness.RequireSources(Kofte.TestSupport.OwnedProcesses.FindRepository());
        var completed = new Dictionary<string, object>(StringComparer.Ordinal);
        var assertions = 0;
        foreach (var selected in scenario == "all" ? IdentityScenarioCatalog.Scenarios : [scenario!])
        {
            using var individual = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token);
            individual.CancelAfter(TimeSpan.FromSeconds(120));
            var observations = selected switch
            {
                "oidc-login" or "trust-refusal" => await LoginScenarios.RunAsync(selected, provider!, individual.Token),
                "session-boundary" => await SessionScenarios.RunAsync(provider!, individual.Token),
                "development-isolation" => await DevelopmentScenarios.RunAsync(provider!, individual.Token),
                "upgrade-preservation" or "interrupted-recovery" or "private-evidence" =>
                    await OperationsScenarios.RunAsync(selected, provider!, individual.Token),
                _ => await EnrollmentScenarios.RunAsync(selected, provider!, individual.Token)
            };
            if (!observations.SequenceEqual(IdentityScenarioCatalog.RequiredObservations(selected)))
                throw new InvalidOperationException("identity_consumer_result_invalid");
            assertions += observations.Count;
            completed.Add(selected, new { assertions = observations.Count, observations });
        }
        var output = JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            eventCode = "identity_scenarios_passed",
            attempt = ActivityTraceId.CreateRandom().ToHexString(),
            provider,
            scenario,
            status = "passed",
            assertions,
            scenarios = completed
        });
        IdentityConsumerHarness.RequireSuccess(new(0, output, ""), provider!, scenario!);
        Console.Out.WriteLine(output);
        return 0;
    }
    catch (Exception error)
    {
        if (scenario == "development-isolation")
        {
            const string source = "tests/contract/Kofte.Identity.Consumer/Scenarios/DevelopmentScenarios.cs";
            var sourceLines = File.ReadLines(Path.Combine(Kofte.TestSupport.OwnedProcesses.FindRepository(), source)).Count();
            developmentFailureLine = new StackTrace(error, true).GetFrames()
                .Where(frame => frame.GetFileName()?.Replace('\\', '/').EndsWith("/" + source, StringComparison.Ordinal) == true)
                .Select(frame => frame.GetFileLineNumber())
                .Where(line => line > 0 && line <= sourceLines)
                .Select(line => (int?)line).FirstOrDefault();
        }
        code = IdentityScenarioCatalog.IsSafeFailureCode(error.Message)
            ? error.Message : "identity_scenario_failed";
        if (code == "identity_scenario_failed" && developmentFailureLine is { } line)
            code = "identity_scenario_development_line_" + line;
        var fixtureFrame = new System.Diagnostics.StackTrace(error, true).GetFrames()
            .FirstOrDefault(frame => Path.GetFileName(frame.GetFileName()) == "LoginCommitFixture.Operations.cs");
        if (code == "identity_scenario_failed" && fixtureFrame is not null &&
            fixtureFrame.GetFileLineNumber() > 0)
            code = "identity_scenario_operations_fixture_line_" + fixtureFrame.GetFileLineNumber()
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (code == "identity_scenario_failed" && scenario is
            "upgrade-preservation" or "interrupted-recovery" or "private-evidence" or "all")
        {
            const string source = "tests/contract/Kofte.Identity.Consumer/Scenarios/OperationsScenarios.cs";
            var sourceLines = File.ReadLines(Path.Combine(Kofte.TestSupport.OwnedProcesses.FindRepository(), source)).Count();
            var failureLine = new StackTrace(error, true).GetFrames()
                .Where(frame => frame.GetFileName()?.Replace('\\', '/').EndsWith("/" + source, StringComparison.Ordinal) == true)
                .Select(frame => frame.GetFileLineNumber()).FirstOrDefault(value => value > 0 && value <= sourceLines);
            if (failureLine > 0) code = "identity_scenario_operations_line_" + failureLine;
        }
    }
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
