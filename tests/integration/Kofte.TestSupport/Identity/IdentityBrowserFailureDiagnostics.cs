using System.Text.Json;
using System.Text.RegularExpressions;

namespace Kofte.TestSupport.Identity;

public sealed record IdentityBrowserProfilePhaseObservation(
    string Phase, int Launch, int ElapsedMilliseconds);

public sealed record IdentityBrowserFailureObservation(
    string FailureCode, string[] FailureCodes, string LastStage, string? ProfilePhase, int? ProfileLaunch,
    int? ProfileElapsedMilliseconds, string? SourceFile, int? SourceLine,
    bool TimedOut, int? ExitCode, bool OutputObserved, bool CleanupObserved,
    IdentityBrowserProfilePhaseObservation[] ProfileTimeline, bool ProfileTimelineTruncated);

public static class IdentityBrowserFailureDiagnostics
{
    private static readonly HashSet<string> Stages = new(StringComparer.Ordinal)
    {
        "identity_browser_stage_initial_launch",
        "identity_browser_stage_initial_browser_ready",
        "identity_browser_stage_untrusted_tls_refused",
        "identity_browser_stage_initial_browser_closed",
        "identity_browser_stage_private_authority_installed",
        "identity_browser_stage_trusted_browser_ready",
        "identity_browser_stage_page_active",
        "identity_browser_stage_loading_signed_out_accessible",
        "identity_browser_stage_signed_out_capture_verified",
        "identity_browser_stage_cancelled_retry_accessible",
        "identity_browser_stage_failed_retry_accessible",
        "identity_browser_stage_unavailable_retry_accessible",
        "identity_browser_stage_corrected_login_accessible",
        "identity_browser_stage_private_capture_verified",
        "identity_browser_stage_failure_boundaries_verified"
    };

    private static readonly HashSet<string> Failures = new(StringComparer.Ordinal)
    {
        "identity_browser_owned_fixture_required",
        "identity_browser_private_trust_required",
        "identity_browser_authority_invalid",
        "identity_browser_ownership_required",
        "identity_browser_cleanup_unverified",
        "identity_browser_trust_schema_changed",
        "identity_browser_authority_import_failed",
        "identity_browser_private_canary_invalid",
        "identity_browser_private_handoff_invalid",
        "identity_browser_private_cache_invalid",
        "identity_browser_private_identity_invalid",
        "identity_browser_private_action_capture_invalid",
        "identity_browser_privacy_capture_failed",
        "identity_browser_capture_response_headers_failed",
        "identity_browser_capture_response_body_failed",
        "identity_browser_capture_identity_body_failed",
        "identity_browser_capture_context_body_failed",
        "identity_browser_capture_handoff_body_failed",
        "identity_browser_capture_page_body_failed",
        "identity_browser_capture_response_aborted",
        "identity_browser_capture_browser_context_fetch_aborted",
        "identity_browser_capture_current_identity_fetch_aborted",
        "identity_browser_capture_identity_document_aborted",
        "identity_browser_capture_identity_action_fetch_aborted",
        "identity_browser_capture_callback_document_aborted",
        "identity_browser_capture_response_failed",
        "identity_browser_capture_csrf_invalid",
        "identity_browser_capture_closed",
        "identity_browser_capture_limit_exceeded",
        "identity_browser_privacy_capture_incomplete",
        "identity_browser_privacy_capture_missing",
        "identity_browser_privacy_disclosure",
        "identity_browser_privacy_scanner_failed",
        "identity_browser_private_canary_export_failed",
        "identity_browser_rejects_untrusted_tls",
        "identity_browser_visible_keyboard_focus",
        "identity_browser_narrow_enlarged_reflow",
        "identity_browser_assert_text_failed",
        "identity_browser_assert_attribute_failed",
        "identity_browser_assert_focus_failed",
        "identity_browser_assert_visibility_failed",
        "identity_browser_assert_count_failed",
        "identity_browser_assert_equal_failed",
        "identity_browser_assert_match_failed",
        "identity_browser_assert_latency_failed",
        "identity_browser_action_timeout",
        "identity_browser_failure_unclassified",
        "identity_browser_failure_boundaries_failed"
    };

    private static readonly Dictionary<string, string> ProfilePhases = new(StringComparer.Ordinal)
    {
        ["launch_started"] = "identity_browser_profile_launch_started",
        ["context_created"] = "identity_browser_profile_context_created",
        ["browser_cdp_opened"] = "identity_browser_profile_browser_cdp_opened",
        ["browser_owner_observed"] = "identity_browser_profile_browser_owner_observed",
        ["untrusted_refused"] = "identity_browser_profile_untrusted_refused",
        ["initial_close_started"] = "identity_browser_profile_initial_close_started",
        ["initial_close_finished"] = "identity_browser_profile_initial_close_finished",
        ["trust_database_opened"] = "identity_browser_profile_trust_database_opened",
        ["trust_database_written"] = "identity_browser_profile_trust_database_written",
        ["trust_database_closed"] = "identity_browser_profile_trust_database_closed",
        ["owned_close_started"] = "identity_browser_profile_owned_close_started",
        ["owned_context_close_completed"] = "identity_browser_profile_owned_context_close_completed",
        ["owned_context_close_failed"] = "identity_browser_profile_owned_context_close_failed",
        ["owned_context_close_pending"] = "identity_browser_profile_owned_context_close_pending",
        ["owned_context_close_pending_after_exit"] = "identity_browser_profile_owned_context_close_pending_after_exit",
        ["owned_termination_started"] = "identity_browser_profile_owned_termination_started",
        ["owned_kill_started"] = "identity_browser_profile_owned_kill_started",
        ["owned_process_exit_observed"] = "identity_browser_profile_owned_process_exit_observed",
        ["owned_context_closed_event"] = "identity_browser_profile_owned_context_closed_event",
        ["owned_browser_disconnected_event"] = "identity_browser_profile_owned_browser_disconnected_event",
        ["owned_close_verified_pending_protocol"] = "identity_browser_profile_owned_close_verified_pending_protocol",
        ["owned_inventory_invalid"] = "identity_browser_profile_owned_inventory_invalid",
        ["owned_profile_changed"] = "identity_browser_profile_owned_profile_changed",
        ["owned_process_birth_changed"] = "identity_browser_profile_owned_process_birth_changed",
        ["owned_process_command_changed"] = "identity_browser_profile_owned_process_command_changed",
        ["owned_process_parent_changed"] = "identity_browser_profile_owned_process_parent_changed",
        ["owned_process_group_unverified"] = "identity_browser_profile_owned_process_group_unverified",
        ["owned_close_finished"] = "identity_browser_profile_owned_close_finished",
        ["trusted_journey_started"] = "identity_browser_profile_trusted_journey_started",
        ["development_journey_started"] = "identity_browser_profile_development_journey_started",
        ["development_selection_started"] = "identity_browser_profile_development_selection_started",
        ["development_selection_finished"] = "identity_browser_profile_development_selection_finished",
        ["sign_out_started"] = "identity_browser_profile_sign_out_started",
        ["retry_started"] = "identity_browser_profile_retry_started",
        ["retry_loading_observed"] = "identity_browser_profile_retry_loading_observed",
        ["retry_finished"] = "identity_browser_profile_retry_finished",
        ["signed_in_accessibility_started"] = "identity_browser_profile_signed_in_accessibility_started",
        ["signed_in_accessibility_finished"] = "identity_browser_profile_signed_in_accessibility_finished",
        ["loading_accessibility_started"] = "identity_browser_profile_loading_accessibility_started",
        ["loading_accessibility_finished"] = "identity_browser_profile_loading_accessibility_finished",
        ["signed_out_accessibility_started"] = "identity_browser_profile_signed_out_accessibility_started",
        ["signed_out_accessibility_finished"] = "identity_browser_profile_signed_out_accessibility_finished",
        ["capture_snapshot_started"] = "identity_browser_profile_capture_snapshot_started",
        ["capture_cookies_read"] = "identity_browser_profile_capture_cookies_read",
        ["capture_page_read"] = "identity_browser_profile_capture_page_read",
        ["capture_storage_read"] = "identity_browser_profile_capture_storage_read",
        ["capture_responses_drained"] = "identity_browser_profile_capture_responses_drained",
        ["capture_snapshot_finished"] = "identity_browser_profile_capture_snapshot_finished",
        ["capture_request_failed"] = "identity_browser_profile_capture_request_failed",
        ["capture_network_aborted"] = "identity_browser_profile_capture_network_aborted",
        ["capture_network_length_mismatch"] = "identity_browser_profile_capture_network_length_mismatch",
        ["capture_network_incomplete_encoding"] = "identity_browser_profile_capture_network_incomplete_encoding",
        ["capture_network_failure_unclassified"] = "identity_browser_profile_capture_network_failure_unclassified",
        ["capture_context_requested"] = "identity_browser_profile_capture_context_requested",
        ["capture_context_response_started"] = "identity_browser_profile_capture_context_response_started",
        ["capture_context_body_finished"] = "identity_browser_profile_capture_context_body_finished",
        ["capture_context_body_failed"] = "identity_browser_profile_capture_context_body_failed",
        ["capture_identity_requested"] = "identity_browser_profile_capture_identity_requested",
        ["capture_identity_response_started"] = "identity_browser_profile_capture_identity_response_started",
        ["capture_identity_body_finished"] = "identity_browser_profile_capture_identity_body_finished",
        ["capture_identity_body_failed"] = "identity_browser_profile_capture_identity_body_failed",
        ["capture_body_resource_missing"] = "identity_browser_profile_capture_body_resource_missing",
        ["capture_body_target_closed"] = "identity_browser_profile_capture_body_target_closed",
        ["client_window_blurred"] = "identity_browser_profile_client_window_blurred",
        ["client_context_fetch_aborted"] = "identity_browser_profile_client_context_fetch_aborted",
        ["client_identity_fetch_aborted"] = "identity_browser_profile_client_identity_fetch_aborted",
        ["client_window_focused"] = "identity_browser_profile_client_window_focused",
        ["client_document_hidden"] = "identity_browser_profile_client_document_hidden",
        ["client_document_visible"] = "identity_browser_profile_client_document_visible",
        ["journey_failure_observed"] = "identity_browser_profile_journey_failure_observed",
        ["capture_cleanup_started"] = "identity_browser_profile_capture_cleanup_started",
        ["capture_cleanup_finished"] = "identity_browser_profile_capture_cleanup_finished",
        ["capture_cleanup_failed"] = "identity_browser_profile_capture_cleanup_failed"
    };

    public static IdentityBrowserFailureObservation Project(ProcessOutput result, bool timedOut)
    {
        var stage = "identity_browser_stage_unobserved";
        string? profilePhase = null;
        int? profileLaunch = null, profileElapsed = null;
        var cleanupObserved = false;
        var timeline = new List<IdentityBrowserProfilePhaseObservation>();
        var timelineTruncated = false;
        foreach (var line in result.Stdout.Split('\n', StringSplitOptions.TrimEntries))
        {
            if (Stages.Contains(line)) stage = line;
            if (line == "identity_browser_stage_process_closed") cleanupObserved = true;
            if (line.Length > 512 || !line.StartsWith('{')) continue;
            try
            {
                using var document = JsonDocument.Parse(line, new() { MaxDepth = 2 });
                var value = document.RootElement;
                if (value.ValueKind != JsonValueKind.Object) continue;
                var properties = value.EnumerateObject().Select(property => property.Name).ToArray();
                if (properties.Length != 4 ||
                    !properties.ToHashSet(StringComparer.Ordinal).SetEquals(
                        ["eventCode", "phase", "launch", "elapsedMilliseconds"]) ||
                    value.GetProperty("eventCode").ValueKind != JsonValueKind.String ||
                    value.GetProperty("eventCode").GetString() != "identity_browser_profile_phase" ||
                    value.GetProperty("phase").ValueKind != JsonValueKind.String ||
                    !ProfilePhases.TryGetValue(value.GetProperty("phase").GetString()!, out var knownPhase) ||
                    value.GetProperty("launch").ValueKind != JsonValueKind.Number ||
                    !value.GetProperty("launch").TryGetInt32(out var launch) || launch is < 1 or > 2 ||
                    value.GetProperty("elapsedMilliseconds").ValueKind != JsonValueKind.Number ||
                    !value.GetProperty("elapsedMilliseconds").TryGetInt32(out var elapsed) ||
                    elapsed is < 0 or > 60000) continue;
                profilePhase = knownPhase;
                profileLaunch = launch;
                profileElapsed = elapsed;
                if (timeline.Count == 64)
                {
                    timeline.RemoveAt(16);
                    timelineTruncated = true;
                }
                timeline.Add(new(knownPhase, launch, elapsed));
            }
            catch (JsonException) { }
        }
        var codes = result.Stderr.Split('\n', StringSplitOptions.TrimEntries)
            .Where(Failures.Contains).Distinct(StringComparer.Ordinal).Take(2).ToArray();
        if (codes.Length == 0) codes = ["identity_browser_failure_boundaries_failed"];
        var location = Regex.Match(result.Stderr,
            @"(?m)^(identity\.failures|identity\.production|identity\.spec|identityBrowser|identityAccessibility|identityPrivacyCapture|identityCanaryExport)\.ts:([1-9][0-9]{0,3}):[1-9][0-9]{0,4}\r?$",
            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
        return new(codes[0], codes, stage, profilePhase, profileLaunch, profileElapsed,
            location.Success ? location.Groups[1].Value + ".ts" : null,
            location.Success ? int.Parse(location.Groups[2].Value,
                System.Globalization.CultureInfo.InvariantCulture) : null,
            timedOut, result.ExitCode is >= -1 and <= 255 ? result.ExitCode : null,
            result.Stdout.Length != 0 || result.Stderr.Length != 0, cleanupObserved,
            timeline.ToArray(), timelineTruncated);
    }

    public static string DescribePlaywrightFailure(ProcessOutput result)
    {
        var evidence = new List<string>();
        var console = new List<string>();
        if (System.Text.Encoding.UTF8.GetByteCount(result.Stdout) is > 0 and <= 1_000_000)
        {
            try
            {
                using var document = JsonDocument.Parse(result.Stdout, new() { MaxDepth = 32 });
                Collect(document.RootElement);
            }
            catch (JsonException) { }
        }
        return DescribeFailure(new(result.ExitCode, string.Join('\n', console), string.Join('\n', evidence)), timedOut: false);

        void Collect(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Array)
                foreach (var item in value.EnumerateArray()) Collect(item);
            else if (value.ValueKind == JsonValueKind.Object)
                foreach (var property in value.EnumerateObject())
                {
                    if (property.Name == "stdout" && property.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in property.Value.EnumerateArray())
                            if (item.ValueKind == JsonValueKind.Object &&
                                item.EnumerateObject().Count() == 1 &&
                                item.TryGetProperty("text", out var text) &&
                                text.ValueKind == JsonValueKind.String &&
                                text.GetString() is { Length: <= 513 } line)
                                console.Add(line);
                        continue;
                    }
                    if (property.Name is "message" or "stack" && property.Value.ValueKind == JsonValueKind.String)
                    {
                        var text = property.Value.GetString()!;
                        foreach (Match matcher in Regex.Matches(text,
                            @"\b(toHaveText|toContainText|toHaveAttribute|toBeFocused|toBeVisible|toHaveCount|toBe|toEqual|toMatch|toBeLessThan)\(",
                            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)))
                        {
                            var code = matcher.Groups[1].Value switch
                            {
                                "toHaveText" or "toContainText" => "identity_browser_assert_text_failed",
                                "toHaveAttribute" => "identity_browser_assert_attribute_failed",
                                "toBeFocused" => "identity_browser_assert_focus_failed",
                                "toBeVisible" => "identity_browser_assert_visibility_failed",
                                "toHaveCount" => "identity_browser_assert_count_failed",
                                "toBe" or "toEqual" => "identity_browser_assert_equal_failed",
                                "toMatch" => "identity_browser_assert_match_failed",
                                "toBeLessThan" => "identity_browser_assert_latency_failed",
                                _ => throw new InvalidOperationException("identity_browser_matcher_unknown")
                            };
                            if (!evidence.Contains(code)) evidence.Add(code);
                        }
                        foreach (Match code in Regex.Matches(text, @"\bidentity_browser_[a-z_]{1,80}\b",
                            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)))
                            if (Failures.Contains(code.Value) && !evidence.Contains(code.Value))
                                evidence.Add(code.Value);
                        var location = Regex.Match(text,
                            @"\b(identity\.spec|identityPrivacyCapture|identityAccessibility|identityCanaryExport)\.ts:[1-9][0-9]{0,3}:[1-9][0-9]{0,4}\b",
                            RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
                        if (location.Success && !evidence.Contains(location.Value)) evidence.Add(location.Value);
                    }
                    else Collect(property.Value);
                }
        }
    }

    public static string DescribeFailure(ProcessOutput result, bool timedOut)
    {
        var observed = Project(result, timedOut);
        var diagnostic = JsonSerializer.Serialize(new
        {
            eventCode = "identity_browser_protocol_observation",
            browserDeadlineExceeded = observed.TimedOut,
            browserExitCode = observed.ExitCode,
            browserOutputObserved = observed.OutputObserved,
            lastBrowserStage = observed.LastStage,
            lastBrowserProfilePhase = observed.ProfilePhase,
            profileLaunch = observed.ProfileLaunch,
            profileElapsedMilliseconds = observed.ProfileElapsedMilliseconds,
            failureCode = observed.FailureCode,
            failureCodes = observed.FailureCodes,
            sourceFile = observed.SourceFile,
            sourceLine = observed.SourceLine,
            cleanupObserved = observed.CleanupObserved,
            profileTimeline = observed.ProfileTimeline.Select(phase => new
            {
                phase = phase.Phase,
                launch = phase.Launch,
                elapsedMilliseconds = phase.ElapsedMilliseconds
            }).ToArray(),
            profileTimelineTruncated = observed.ProfileTimelineTruncated
        });
        Console.Error.WriteLine(diagnostic);
        // Keep the closed codes in the exception too: transient xUnit projections retain these.
        return string.Join(" ", observed.FailureCodes.Concat(new[]
        {
            observed.LastStage, observed.ProfilePhase,
            observed.SourceFile is null ? null : $"{observed.SourceFile}:{observed.SourceLine}",
            timedOut ? "identity_browser_deadline_exceeded" : null
        }.OfType<string>())) + Environment.NewLine + string.Join(Environment.NewLine,
            observed.ProfileTimeline.Select(phase =>
                $"{phase.Phase}:{phase.Launch}:{phase.ElapsedMilliseconds}"));
    }
}
