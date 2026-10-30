using System.Globalization;
using System.Text;
using System.Text.Json;
using Kofte.Host.Bootstrap;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;

namespace Kofte.TestSupport.Identity;

public static class IdentityConsumerFailureCodes
{
    private static readonly HashSet<string> Known = new(StringComparer.Ordinal)
    {
        "identity_scenario_failed", "identity_scenario_required", "identity_scenario_unimplemented",
        "identity_consumer_result_invalid", "test_identity_preparation_failed",
        "identity_fixture_operator_failed", "identity_fixture_child_exited",
        "identity_fixture_browser_failed", "identity_fixture_browser_required",
        "identity_fixture_browser_context_failed", "identity_fixture_private_delivery_invalid",
        "identity_fixture_web_exited", "identity_fixture_development_browser_required",
        "identity_fixture_development_browser_report_invalid", "identity_fixture_development_browser_incomplete",
        "identity_fixture_development_browser_failed",
        "identity_fixture_restore_retry_failed", "identity_fixture_restored_host_failed",
        "identity_fixture_restored_login_failed", "identity_fixture_restored_current_failed",
        "identity_fixture_restored_stale_browser_not_refused",
        "identity_fixture_restore_interruption_invalid", "identity_fixture_restore_decision_invalid",
        "identity_fixture_restored_cleanup_unverified", "identity_fixture_diagnostic_capture_limit",
        "identity_fixture_actor_missing", "identity_fixture_adapter_required", "identity_fixture_authorization_failed",
        "identity_fixture_bootstrap_invalid", "identity_fixture_browser_control_invalid", "identity_fixture_browser_scope_invalid",
        "identity_fixture_callback_acknowledged", "identity_fixture_callback_missing", "identity_fixture_capacity_not_seeded",
        "identity_fixture_challenge_failed", "identity_fixture_child_ownership_invalid", "identity_fixture_commit_interrupted",
        "identity_fixture_cookie_invalid", "identity_fixture_crash_invalid", "identity_fixture_development_required",
        "identity_fixture_development_scope_invalid", "identity_fixture_enrollment_acknowledged", "identity_fixture_event_code_invalid",
        "identity_fixture_key_damage_invalid", "identity_fixture_key_damage_missing", "identity_fixture_lease_failed",
        "identity_fixture_mode_transition_scope_invalid", "identity_fixture_observer_failed", "identity_fixture_owned_host_required",
        "identity_fixture_principal_missing", "identity_fixture_product_authorization_failed", "identity_fixture_product_challenge_failed",
        "identity_fixture_product_required", "identity_fixture_session_cookie_missing", "identity_fixture_signed_token_required",
        "identity_fixture_ticket_missing", "identity_fixture_trust_invalid", "identity_fixture_verified_session_missing",
        "identity_fixture_web_build_required", "identity_fixture_arguments_invalid", "identity_fixture_configuration_invalid",
        "identity_fixture_consumer_missing", "identity_fixture_exit_unverified", "identity_fixture_requires_stopped_processes",
        "identity_fixture_web_build_missing", "identity_fixture_login_fault_scope_invalid", "identity_fixture_rotation_invalid",
        "identity_browser_transport_canaries_invalid", "identity_browser_transport_capture_limit",
        "identity_browser_transport_capture_missing", "identity_browser_transport_disclosure",
        "identity_browser_transport_scanner_failed", "identity_browser_transport_scope_invalid",
        "identity_browser_initial_state", "identity_browser_signed_in", "identity_browser_rejects_untrusted_tls",
        "identity_browser_authority_invalid", "identity_browser_trust_schema_changed",
        "identity_browser_authority_import_failed", "identity_browser_private_trust_required",
        "identity_browser_current_unavailable", "identity_browser_owned_fixture_required",
        "identity_browser_opaque_origin_refused", "identity_browser_enrollment_acknowledgement_not_lost",
        "identity_browser_session_controls_incomplete", "identity_browser_cleanup_unverified",
        "identity_browser_ownership_required", "identity_scenario_response_bound",
        "identity_scenario_receipt_invalid", "identity_scenario_contender_rejected",
        "identity_scenario_enrollment_not_private", "identity_scenario_fresh_designation_invalid",
        "identity_scenario_session_control_invalid"
    };

    private static readonly (string Prefix, string Source)[] SourceLines =
    [
        ("identity_scenario_operations_line_", "tests/contract/Kofte.Identity.Consumer/Scenarios/OperationsScenarios.cs"),
        ("identity_scenario_operations_fixture_line_", "tests/integration/Kofte.TestSupport/Identity/LoginCommitFixture.Operations.cs"),
        ("identity_scenario_development_line_", "tests/contract/Kofte.Identity.Consumer/Scenarios/DevelopmentScenarios.cs")
    ];

    private static readonly string[] StorageFailures =
    [
        "busy", "conflict", "target_reserved", "path_rejected", "owner_mismatch", "missing", "corrupt",
        "incomplete", "reconciliation_required", "version_unsupported", "privilege_denied", "unavailable",
        "deadline_exceeded", "cancelled", "request_invalid"
    ];

    private static readonly string[] CaptureFields =
        ["shape", "component", "provider", "code", "owner", "correlation", "operation", "outcome", "guidance", "durability"];

    private static readonly string[] Commands =
    [
        "registry_upgrade", "prepare", "reconcile_configuration", "revoke_sessions",
        "bootstrap_authorize", "bootstrap_disable_pending", "reconcile_restore", "maintain"
    ];

    private static readonly HashSet<string> StoppedCodes = CreateStoppedCodes();
    private static readonly HashSet<string> ReadinessCodes = CreateReadinessCodes();

    public static bool IsSafe(string code) =>
        Known.Contains(code) || StoppedCodes.Contains(code) || ReadinessCodes.Contains(code) ||
        StorageFailures.Any(name => code == "identity_recipe_storage_" + name) ||
        CaptureFields.Any(name => code == "identity_recipe_storage_capture_" + name) ||
        IdentityScenarioCatalog.Scenarios.SelectMany(IdentityScenarioCatalog.RequiredObservations)
            .Any(name => code == "identity_scenario_" + name) ||
        SourceLines.Any(source => IsSourceLine(code, source.Prefix, source.Source));

    private static bool IsSourceLine(string code, string prefix, string source)
    {
        if (!code.StartsWith(prefix, StringComparison.Ordinal)) return false;
        var digits = code.AsSpan(prefix.Length);
        if (digits.Length is < 1 or > 6 || digits[0] == '0' ||
            !int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out var line))
            return false;
        var path = Path.Combine(OwnedProcesses.FindRepository(), source);
        return line > 0 && File.Exists(path) && line <= File.ReadLines(path).Count();
    }

    private static HashSet<string> CreateStoppedCodes()
    {
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var command in Commands)
        {
            foreach (var action in Enum.GetValues<IdentityAction>())
                foreach (var outcome in Enum.GetValues<IdentityDiagnosticOutcome>()
                    .Where(value => value != IdentityDiagnosticOutcome.Ready))
                    result.Add(StoppedCode(command, IdentityDiagnostics.ActionName(action), IdentityDiagnostics.Rule(outcome).Code));
            foreach (var operation in Enum.GetValues<StorageOperation>())
                foreach (var failure in StorageFailures)
                    result.Add(StoppedCode(command, operation.ToString().ToLowerInvariant(), "storage_" + failure));
            foreach (var failure in Enum.GetValues<BootstrapError>())
                result.Add(StoppedCode(command, "startup", BootstrapDiagnostics.Code(failure)));
        }
        return result;
    }

    private static HashSet<string> CreateReadinessCodes()
    {
        var observations = new[] { "no_response" }
            .Concat(Enum.GetValues<System.Net.HttpStatusCode>().Select(status => "http_" + ((int)status)
                .ToString(CultureInfo.InvariantCulture)))
            .Concat(Enum.GetValues<HttpRequestError>().Select(error => "transport_" + error));
        var result = new HashSet<string>(StringComparer.Ordinal);
        foreach (var observation in observations)
            foreach (var phase in new[] { "configured", "owner_opened", "built", "mapped", "serving", "unknown" })
                result.Add("identity_fixture_child_readiness_" + observation + "_" + phase);
        return result;
    }

    private static string StoppedCode(string command, string operation, string code) =>
        "identity_fixture_operator_" + command + "_" + operation + "_" + code;

    public static string StoppedOperation(IReadOnlyList<string> arguments, ProcessOutput output)
    {
        const string fallback = "identity_fixture_operator_failed";
        var command = arguments.Count >= 2 ? (arguments[0], arguments[1]) switch
        {
            ("--registry", "upgrade") => "registry_upgrade",
            ("--identity", "prepare") => "prepare",
            ("--identity", "reconcile-configuration") => "reconcile_configuration",
            ("--identity", "revoke-sessions") => "revoke_sessions",
            ("--identity", "reconcile-restore") => "reconcile_restore",
            ("--identity", "maintain") => "maintain",
            ("--identity", "bootstrap") when arguments.Count >= 3 && arguments[2] == "authorize" => "bootstrap_authorize",
            ("--identity", "bootstrap") when arguments.Count >= 3 && arguments[2] == "disable-pending" => "bootstrap_disable_pending",
            _ => null
        } : null;
        if (command is null || output.ExitCode == 0 || output.Stdout.Length != 0 ||
            Encoding.UTF8.GetByteCount(output.Stderr) > 65_536)
            return fallback;
        string? failure = null;
        var lines = output.Stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length is < 1 or > 64) return fallback;
        foreach (var line in lines)
        {
            if (Encoding.UTF8.GetByteCount(line) > 1024) return fallback;
            try
            {
                using var document = JsonDocument.Parse(line, new() { AllowDuplicateProperties = false, MaxDepth = 2 });
                var value = document.RootElement;
                string? code = null;
                if (value.ValueKind != JsonValueKind.Object) return fallback;
                if (value.TryGetProperty("component", out var component) && component.GetString() == "identity")
                {
                    var record = IdentityDiagnostics.Parse(Encoding.UTF8.GetBytes(line));
                    string[] expected = ["version", "eventCode", "component", "operation", "mode", "provider",
                        "outcome", "correlationId", "guidance", "durability"];
                    if (!Keys(value, record.EventCode == "identity_event_capacity_warning"
                        ? [.. expected, "retention"] : expected)) return fallback;
                    if (record.EventCode is not ("identity_completed" or "identity_event_capacity_warning"))
                        code = StoppedCode(command, record.Operation, record.EventCode);
                }
                else if (component.GetString() == "storage")
                {
                    if (!Keys(value, ["eventCode", "component", "operation", "provider", "outcome", "moduleId",
                        "correlationId", "guidance", "durability"]) ||
                        value.GetProperty("provider").GetString() is not ("sqlite" or "postgresql" or "unknown") ||
                        !Correlation(value.GetProperty("correlationId")) ||
                        value.GetProperty("durability").GetString() is not ("unchanged" or "committed" or "uncertain") ||
                        value.GetProperty("moduleId").ValueKind != JsonValueKind.Null &&
                        !Kofte.Platform.Modules.Manifest.ModuleId.TryParse(value.GetProperty("moduleId").GetString(), out _))
                        return fallback;
                    var operation = value.GetProperty("operation").GetString();
                    if (!Enum.GetValues<StorageOperation>().Any(item => item.ToString().ToLowerInvariant() == operation))
                        return fallback;
                    var storageCode = Enum.GetValues<StorageCode>().FirstOrDefault(
                        item => StorageDiagnostics.Diagnostic(item).Code == value.GetProperty("eventCode").GetString());
                    var diagnostic = StorageDiagnostics.Diagnostic(storageCode);
                    var outcome = storageCode == StorageCode.Ready ? "ready" :
                        storageCode == StorageCode.NoStorage ? "nostorage" :
                        StorageDiagnostics.Failure(storageCode, StorageOperation.Upgrade, "unknown", null,
                            new string('a', 32), StorageDurability.Unchanged).Outcome.ToString().ToLowerInvariant();
                    if (diagnostic.Code != value.GetProperty("eventCode").GetString() ||
                        diagnostic.Guidance != value.GetProperty("guidance").GetString() ||
                        outcome != value.GetProperty("outcome").GetString()) return fallback;
                    if (storageCode is not (StorageCode.Ready or StorageCode.NoStorage))
                        code = StoppedCode(command, operation!, diagnostic.Code);
                }
                else if (component.GetString() == "host")
                {
                    if (!Keys(value, ["level", "eventCode", "component", "correlationId"]) ||
                        value.GetProperty("level").GetString() != "error" ||
                        !Correlation(value.GetProperty("correlationId"))) return fallback;
                    code = StoppedCode(command, "startup", value.GetProperty("eventCode").GetString()!);
                }
                else return fallback;
                if (code is not null)
                {
                    if (!StoppedCodes.Contains(code)) return fallback;
                    failure ??= code;
                }
            }
            catch (Exception error) when (error is JsonException or InvalidOperationException or
                KeyNotFoundException or FormatException or OverflowException or ArgumentException)
            { return fallback; }
        }
        return failure ?? fallback;
    }

    private static bool Keys(JsonElement value, string[] expected) =>
        value.EnumerateObject().Select(item => item.Name).Order(StringComparer.Ordinal)
            .SequenceEqual(expected.Order(StringComparer.Ordinal));

    private static bool Correlation(JsonElement value) =>
        value.GetString() is { Length: 32 } correlation && correlation.All(char.IsAsciiHexDigitLower);
}
