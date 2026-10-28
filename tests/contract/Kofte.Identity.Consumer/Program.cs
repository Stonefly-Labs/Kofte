using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kofte.Identity.Consumer;
using Kofte.Identity.Consumer.Scenarios;
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
if (code == "identity_scenario_unimplemented" && scenario is "oidc-login" or "trust-refusal")
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
    try
    {
        var observations = await LoginScenarios.RunAsync(scenario, provider!, timeout.Token);
        Console.Out.WriteLine(JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            eventCode = "identity_scenario_completed",
            attempt = ActivityTraceId.CreateRandom().ToHexString(),
            provider,
            scenario,
            status = "passed",
            assertions = observations.Count,
            observations
        }));
        return 0;
    }
    catch (Exception error) when (error is InvalidOperationException or OperationCanceledException or
        System.Net.Http.HttpRequestException or JsonException)
    {
        code = error.Message is "test_identity_preparation_failed" or "identity_fixture_web_exited" or
            "identity_fixture_browser_failed" or "identity_fixture_browser_required" or
            "identity_browser_initial_state" or "identity_browser_signed_in" or
            "identity_browser_rejects_untrusted_tls" or "identity_browser_authority_invalid" or
            "identity_browser_trust_schema_changed" or "identity_browser_authority_import_failed" or
            "identity_browser_private_trust_required" or "identity_browser_current_unavailable" or
            "identity_browser_owned_fixture_required" or "identity_browser_opaque_origin_refused" ||
            error.Message is "identity_browser_cleanup_unverified" or "identity_browser_ownership_required" ||
            Regex.IsMatch(error.Message, @"\Aidentity_browser_(page|trust)_line_[1-9][0-9]{0,3}\z") ||
            Regex.IsMatch(error.Message, @"\Aidentity_browser_stage_[a-z_]{1,40}\z")
                ? error.Message : "identity_scenario_failed";
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
