namespace Kofte.TestSupport.Identity;

public static class IdentityScenarioCatalog
{
    public static IReadOnlyList<string> Scenarios { get; } = Array.AsReadOnly(new[]
    {
        "oidc-login", "trust-refusal", "nominated-enrollment", "enrollment-race",
        "session-boundary", "development-isolation", "upgrade-preservation",
        "interrupted-recovery", "private-evidence"
    });

    public static string RequireScenario(string value) =>
        value == "all" || Scenarios.Contains(value, StringComparer.Ordinal)
            ? value : throw new InvalidOperationException("identity_scenario_required");

    public static string RequireProvider(string value) => StorageScenarioCatalog.RequireProvider(value);

    public static bool IsSafeFailureCode(string code) => IdentityConsumerFailureCodes.IsSafe(code);

    public static IReadOnlyList<string> RequiredObservations(string scenario) =>
        Array.AsReadOnly(scenario switch
        {
            "oidc-login" => LoginObservations(),
            "trust-refusal" => ["missing_auth", "invalid_signature", "no_refused_effects", "unsolicited_refusal",
                .. LoginObservations().Skip(1)],
            "nominated-enrollment" => [.. EnrollmentObservations(false), "native_nominee_login",
                "browser_lost_ack_fresh_designation_no_retry"],
            "enrollment-race" => EnrollmentObservations(true),
            "session-boundary" => ["session_anonymous_refusal", "session_anonymous_refusal", "session_anonymous_refusal",
                "session_native_login", "session_current_identity", "session_actor_parity",
                "session_ungranted_action_denied", "session_separately_compiled_actor",
                "session_mixed_credentials_refused", "session_csrf_origin_no_effects",
                "session_confirmed_revocation_racing_renewal", "session_next_request_denied",
                "session_next_request_denied", "session_browser_expiry_revocation_uncertainty_two_user",
                "session_nominee_login", "session_explicit_designation", "session_fresh_designation",
                "session_actor_parity", "session_designation_is_not_permission"],
            "development-isolation" => DevelopmentObservations(),
            "upgrade-preservation" => ["upgrade_format_one_module_sentinels_history",
                "upgrade_clean_current_identity_prepared", "upgrade_exact_module_inventory_preserved",
                "upgrade_native_user_mapping", "upgrade_explicit_consumed_designation",
                "upgrade_idempotent_exact_identity_history_modules", "upgrade_idp_loss_no_reassignment",
                "upgrade_consumed_nomination_refused"],
            "interrupted-recovery" => ["recovery_prepare_owned_termination",
                "recovery_prepare_explicit_retry_exact_state", "recovery_before_commit_owned_termination",
                "recovery_before_commit_exact_rollback", "recovery_after_commit_owned_termination",
                "recovery_after_commit_exact_consumption", "recovery_complete_set_exact_modules",
                "recovery_quarantine_refuses_exposure", "recovery_reconciliation_owned_termination",
                "recovery_explicit_retry_exact_identity_history_modules",
                "recovery_old_cookie_refused_fresh_login_same_designation",
                "recovery_consumed_reassignment_refused", "recovery_stale_copy_unknown_history_locked"],
            "private-evidence" => ["private_native_login_correlated", "private_rejected_login_unchanged",
                "private_explicit_enrollment_correlated", "private_required_record_failure_rollback",
                "private_confirmed_sign_out_correlated", "private_session_expiry_correlated", "private_operator_result_correlated",
                "private_captured_diagnostics_spans_metrics", "private_all_captured_channels_canary_free"],
            _ => throw new InvalidOperationException("identity_scenario_required")
        });

    private static string[] LoginObservations() =>
        ["missing_auth", "native_login", "current_identity", "public_actor_parity", "fresh_login",
         "stable_mapping_no_designation", "mixed_credentials_refused", "browser_adapter_consumer_parity"];

    private static string[] EnrollmentObservations(bool race) =>
        ["native_nominee_login", "first_arrival_has_no_designation", "disabled_ceremony_refused",
         "stopped_private_nomination", race ? "twenty_admitted_one_winner_nineteen_conflicts" : "confirmed_protected_enrollment",
         "atomic_exact_designation_consumption_event", "fresh_identity_actor_parity",
         "designation_grants_no_unrelated_action", "fresh_process_1", "restart_replay_closed_1",
         "fresh_process_2", "restart_replay_closed_2", "fresh_process_3", "restart_replay_closed_3",
         "idempotent_upgrade_prepare_preserves_consumption"];

    private static string[] DevelopmentObservations()
    {
        var observations = new List<string>();
        foreach (var composed in new[] { false, true })
            foreach (var unsafeExposure in new[] { false, true })
            {
                var prefix = "development_" + (composed ? "composed_" : "empty_") +
                    (unsafeExposure ? "unsafe_" : "safe_");
                observations.AddRange(new[] {
                    "production", "staging", "wrong_environment_case", "conflicting_environment",
                    "missing_configuration", "missing_mode", "unknown_mode", "disabled", "missing_opt_in",
                    "missing_choices", "empty_choices", "caller_authority", "wrong_public_origin",
                    "host_wildcard", "host_ipv6", "host_public", "web_wildcard", "web_ipv6", "web_public",
                    "backend_wildcard", "backend_ipv6", "backend_public"
                }.Select(fault => prefix + "refuses_" + fault));
                observations.Add(prefix + "prepared_state_unchanged");
                observations.Add(prefix + "owned_exits_verified");
            }
        observations.AddRange(["development_browser_explicit_configured_selection",
            "development_browser_session_actor_parity", "development_browser_session_ungranted_action_denied",
            "development_visible_browser_api_actor_parity_no_provider"]);
        foreach (var consumed in new[] { false, true })
        {
            var prefix = consumed ? "development_consumed_" : "development_pending_";
            observations.Add(prefix + "production_login");
            if (consumed) observations.Add(prefix + "explicit_designation");
            foreach (var name in new[] {
                "production_cookie_refused_namespace", "production_cookie_refused_original",
                "production_cookie_refused_renamed", "explicit_configured_selection",
                "equal_profiles_separate_stable_users", "local_session_actor_parity",
                "local_session_ungranted_action_denied", "development_cannot_consume_production_authority",
                "development_cookie_refused_namespace", "development_cookie_refused_original",
                "development_cookie_refused_renamed", "fresh_production_login",
                "exact_mapping_designation_history_preserved", "restored_session_actor_parity",
                consumed ? "restored_session_designation_is_not_permission" : "restored_session_ungranted_action_denied",
                "original_authority_replay_refused"
            }) observations.Add(prefix + name);
        }
        return observations.ToArray();
    }
}
