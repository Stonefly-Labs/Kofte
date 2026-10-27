using System.Diagnostics;
using System.Text.Json;
using Kofte.Identity.Consumer;
using Kofte.TestSupport.Identity;

if (args is ["--identity-fixture-worker"]) return await FixtureProgram.RunAsync();
if (args is ["--modules-worker", "serve"]) return await BootstrapProgram.RunAsync(args);

string? scenario = null;
string? provider = null;
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
