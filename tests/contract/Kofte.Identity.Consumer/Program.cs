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
if (code == "identity_scenario_unimplemented" &&
    scenario is "oidc-login" or "trust-refusal" or "nominated-enrollment" or "enrollment-race" or
        "session-boundary" or "development-isolation")
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
    try
    {
        var observations = scenario switch
        {
            "oidc-login" or "trust-refusal" => await LoginScenarios.RunAsync(scenario, provider!, timeout.Token),
            "session-boundary" => await SessionScenarios.RunAsync(provider!, timeout.Token),
            "development-isolation" => await DevelopmentScenarios.RunAsync(provider!, timeout.Token),
            _ => await EnrollmentScenarios.RunAsync(scenario, provider!, timeout.Token)
        };
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
        code = error.Message is "test_identity_preparation_failed" or "identity_fixture_web_exited" or
            "identity_fixture_browser_failed" or "identity_fixture_browser_required" or
            "identity_browser_initial_state" or "identity_browser_signed_in" or
            "identity_browser_rejects_untrusted_tls" or "identity_browser_authority_invalid" or
            "identity_browser_trust_schema_changed" or "identity_browser_authority_import_failed" or
            "identity_browser_private_trust_required" or "identity_browser_current_unavailable" or
            "identity_browser_owned_fixture_required" or "identity_browser_opaque_origin_refused" ||
            error.Message == "identity_browser_enrollment_acknowledgement_not_lost" ||
            error.Message == "identity_browser_session_controls_incomplete" ||
            error.Message is "identity_browser_cleanup_unverified" or "identity_browser_ownership_required" ||
            error.Message is "identity_scenario_native_nominee_login" or
                "identity_scenario_first_arrival_has_no_designation" or
                "identity_scenario_disabled_ceremony_refused" or "identity_scenario_stopped_private_nomination" or
                "identity_scenario_response_bound" or "identity_scenario_receipt_invalid" or
                "identity_scenario_contender_rejected" or "identity_scenario_enrollment_not_private" or
                "identity_scenario_twenty_admitted_one_winner_nineteen_conflicts" or
                "identity_scenario_confirmed_protected_enrollment" or
                "identity_scenario_atomic_exact_designation_consumption_event" or
                "identity_scenario_designation_grants_no_unrelated_action" or
                "identity_scenario_fresh_designation_invalid" or
                "identity_scenario_browser_lost_ack_fresh_designation_no_retry" or
                "identity_scenario_idempotent_upgrade_prepare_preserves_consumption" ||
            error.Message is "identity_fixture_operator_failed" or "identity_fixture_child_exited" or
                "identity_fixture_browser_context_failed" or "identity_fixture_private_delivery_invalid" ||
            Regex.IsMatch(error.Message, @"\Aidentity_scenario_(fresh_process|restart_replay_closed)_[1-3]\z") ||
            error.Message is "identity_scenario_session_actor_parity" or
                "identity_scenario_session_designation_is_not_permission" or
                "identity_scenario_session_ungranted_action_denied" or
                "identity_scenario_session_nominee_login" or
                "identity_scenario_session_explicit_designation" or
                "identity_scenario_session_fresh_designation" or
                "identity_scenario_session_anonymous_refusal" or
                "identity_scenario_session_native_login" or
                "identity_scenario_session_current_identity" or
                "identity_scenario_session_separately_compiled_actor" or
                "identity_scenario_session_mixed_credentials_refused" or
                "identity_scenario_session_csrf_origin_no_effects" or
                "identity_scenario_session_confirmed_revocation_racing_renewal" or
                "identity_scenario_session_next_request_denied" or
                "identity_scenario_session_control_invalid" or
                "identity_scenario_session_browser_expiry_revocation_uncertainty_two_user" ||
            Regex.IsMatch(error.Message, @"\Aidentity_browser_(page|trust)_line_[1-9][0-9]{0,3}\z") ||
            Regex.IsMatch(error.Message, @"\Aidentity_browser_stage_[a-z_]{1,40}\z") ||
            Regex.IsMatch(error.Message, @"\Aidentity_scenario_development_[a-z_]{1,80}\z") ||
            Regex.IsMatch(error.Message, @"\Aidentity_development_startup_case_[a-z0-9_-]{1,80}\z") ||
            Regex.IsMatch(error.Message, @"\Aidentity_fixture_development_browser_failed(?:_line_[1-9][0-9]{0,3})?\z") ||
            Regex.IsMatch(error.Message, @"\Aidentity_fixture_child_readiness_(?:no_response|http_[1-5][0-9]{2}|transport_[A-Za-z]{1,40})(?:_(?:configured|owner_opened|built|mapped|serving|unknown))?\z") ||
            error.Message is "identity_fixture_development_browser_required" or
                "identity_fixture_development_browser_report_invalid" or "identity_fixture_development_browser_incomplete"
                ? error.Message : "identity_scenario_failed";
        if (code == "identity_scenario_failed" && developmentFailureLine is { } line)
            code = "identity_scenario_development_line_" + line;
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
