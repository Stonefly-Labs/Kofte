namespace Kofte.Platform.ContractTests.Identity;

public sealed record IdentityHttpCase(string Name, string Method, string Path, string Credentials,
    string Input, int Status, string? Code, string Effect);
public sealed record IdentityNumericCase(string Name, decimal AtLimit, decimal OneOutside, string Unit,
    string AtLimitOutcome, string OutsideOutcome);
public sealed record IdentityEvidenceCase(string Name, int Status, string Code, string Effect);

public static class IdentityCorpus
{
    public static IReadOnlyList<IdentityHttpCase> Http { get; } = Array.AsReadOnly(new[]
    {
        new IdentityHttpCase("context", "GET", "/api/identity/v1/browser-context", "anonymous", "", 200, null, "csrf_only"),
        new("login", "POST", "/auth/login", "anonymous_csrf_origin", "returnPath=/identity", 303, null, "pending_attempt_only"),
        new("login-default-return", "POST", "/auth/login", "anonymous_csrf_origin", "", 303, null, "return_identity"),
        new("me-anonymous", "GET", "/api/identity/v1/me", "anonymous", "", 401, "identity_unauthenticated", "none"),
        new("me", "GET", "/api/identity/v1/me", "valid_session", "", 200, null, "current_self_only"),
        new("me-forged", "GET", "/api/identity/v1/me", "forged_cookie", "", 401, "identity_unauthenticated", "none"),
        new("me-revoked", "GET", "/api/identity/v1/me", "revoked_cookie", "", 401, "identity_unauthenticated", "none"),
        new("me-wrong-mode", "GET", "/api/identity/v1/me", "wrong_mode_cookie", "", 401, "identity_unauthenticated", "none"),
        new("me-bearer", "GET", "/api/identity/v1/me", "bearer", "", 401, "identity_unauthenticated", "none"),
        new("me-bearer-and-cookie", "GET", "/api/identity/v1/me", "bearer_valid_cookie", "", 401, "identity_unauthenticated", "none"),
        new("me-store-down", "GET", "/api/identity/v1/me", "session_store_unavailable", "", 503, "identity_unavailable", "none"),
        new("sign-out", "POST", "/api/identity/v1/sign-out", "session_csrf_origin", "", 204, null, "confirmed_self_revocation"),
        new("sign-out-uncertain", "POST", "/api/identity/v1/sign-out", "session_csrf_origin", "lost_commit_reply", 503, "identity_outcome_uncertain", "no_revocation_claim"),
        new("enroll", "POST", "/api/identity/v1/first-administrator", "nominee_session_csrf_origin", "valid_authorization", 200, null, "one_designation_event"),
        new("enroll-other-user", "POST", "/api/identity/v1/first-administrator", "other_session_csrf_origin", "valid_authorization", 403, "identity_forbidden", "none"),
        new("enroll-consumed", "POST", "/api/identity/v1/first-administrator", "nominee_session_csrf_origin", "consumed_authorization", 409, "identity_enrollment_unavailable", "none"),
        new("enroll-expired", "POST", "/api/identity/v1/first-administrator", "nominee_session_csrf_origin", "expired_authorization", 409, "identity_enrollment_unavailable", "none"),
        new("enroll-race-loser", "POST", "/api/identity/v1/first-administrator", "nominee_session_csrf_origin", "concurrent_authorization", 409, "identity_enrollment_unavailable", "none"),
        new("development", "POST", "/api/identity/v1/development-session", "development_csrf_origin", "configured_identity_key", 200, null, "development_session_only"),
        new("development-production", "POST", "/api/identity/v1/development-session", "oidc_csrf_origin", "configured_identity_key", 404, null, "none"),
        new("development-unknown-key", "POST", "/api/identity/v1/development-session", "development_csrf_origin", "unknown_identity_key", 400, "identity_request_invalid", "none"),
        new("mutation-missing-csrf", "POST", "/api/identity/v1/sign-out", "session_origin", "", 403, "identity_forbidden", "none"),
        new("mutation-stale-anonymous-csrf", "POST", "/api/identity/v1/sign-out", "session_anonymous_csrf_origin", "", 403, "identity_forbidden", "none"),
        new("mutation-foreign-origin", "POST", "/api/identity/v1/sign-out", "session_csrf_foreign_origin", "", 403, "identity_forbidden", "none"),
        new("mutation-null-origin", "POST", "/api/identity/v1/sign-out", "session_csrf_null_origin", "", 403, "identity_forbidden", "none"),
        new("mutation-missing-origin", "POST", "/api/identity/v1/sign-out", "session_csrf", "", 403, "identity_forbidden", "none"),
        new("mutation-cross-site", "POST", "/api/identity/v1/sign-out", "session_csrf_origin_cross_site", "", 403, "identity_forbidden", "none"),
        new("me-query", "GET", "/api/identity/v1/me?userId=other", "valid_session", "", 400, "identity_request_invalid", "none"),
        new("me-body", "GET", "/api/identity/v1/me", "valid_session", "body", 400, "identity_request_invalid", "none"),
        new("me-head", "HEAD", "/api/identity/v1/me", "valid_session", "", 405, null, "allow_get_only"),
        new("me-options", "OPTIONS", "/api/identity/v1/me", "valid_session", "", 405, null, "allow_get_only"),
        new("sign-out-get", "GET", "/api/identity/v1/sign-out", "valid_session", "", 405, null, "allow_post_only"),
        new("unknown", "GET", "/api/identity/v1/unknown", "anonymous", "", 404, null, "none"),
        new("case-sensitive", "GET", "/API/identity/v1/me", "valid_session", "", 404, null, "none"),
        new("unknown-json", "POST", "/api/identity/v1/first-administrator", "nominee_session_csrf_origin", "{\"authorization\":\"synthetic\",\"userId\":\"other\"}", 400, "identity_request_invalid", "none"),
        new("duplicate-json", "POST", "/api/identity/v1/first-administrator", "nominee_session_csrf_origin", "{\"authorization\":\"synthetic\",\"authorization\":\"synthetic\"}", 400, "identity_request_invalid", "none"),
        new("unrelated-admin-action", "POST", "/ungranted-consumer-action", "initial_administrator", "", 403, "identity_forbidden", "none")
    });

    public static IReadOnlyList<string> AllowedReturns { get; } = Array.AsReadOnly(new[] { "/", "/identity" });
    public static IReadOnlyList<string> RejectedReturns { get; } = Array.AsReadOnly(new[]
    {
        "", "//example.invalid", "https://example.invalid/", "/\\example.invalid", "\\identity",
        "/%2fidentity", "/%5cidentity", "/%69dentity", "/identity?next=/", "/identity#fragment",
        "/unknown", "/Identity", "/identity/", "/./identity", "/other/../identity", " /identity", "/identity\n"
    });

    public static IReadOnlyList<IdentityEvidenceCase> Evidence { get; } = Array.AsReadOnly(new[]
    {
        new IdentityEvidenceCase("valid-code-pkce", 303, "authenticated", "one_owner_commit_before_cookie"),
        new("issuer-case-or-trailing-slash", 303, "identity_unauthenticated", "no_user_or_session_commit"),
        new("wrong-audience", 303, "identity_unauthenticated", "no_user_or_session_commit"),
        new("foreign-signature-or-key", 303, "identity_unauthenticated", "no_user_or_session_commit"),
        new("wrong-algorithm-or-none", 303, "identity_unauthenticated", "no_user_or_session_commit"),
        new("expired-or-future", 303, "identity_unauthenticated", "no_user_or_session_commit"),
        new("missing-or-wrong-nonce", 303, "identity_unauthenticated", "no_user_or_session_commit"),
        new("wrong-pkce", 303, "identity_unauthenticated", "no_user_or_session_commit"),
        new("missing-or-malformed-keys", 303, "identity_unauthenticated", "no_user_or_session_commit"),
        new("unsolicited-missing-state", 400, "identity_request_invalid", "no_provider_call_or_session"),
        new("wrong-state-or-browser", 400, "identity_request_invalid", "no_provider_call_or_session"),
        new("duplicate-callback-fields", 400, "identity_request_invalid", "no_provider_call_or_session"),
        new("replayed-consumed-attempt", 400, "identity_request_invalid", "no_new_session"),
        new("provider-cancelled", 303, "identity_unauthenticated", "no_user_or_session_commit"),
        new("provider-unavailable", 303, "identity_unavailable", "no_user_or_session_commit"),
        new("commit-acknowledgement-lost", 503, "identity_outcome_uncertain", "no_cookie_or_success_claim")
    });

    public static IReadOnlyList<IdentityNumericCase> Limits { get; } = Array.AsReadOnly(new[]
    {
        new IdentityNumericCase("attempt-lifetime", 300_000, 300_001, "ms", "expired", "expired"),
        new("attempts-installation", 256, 257, "rows", "admitted", "429_no_new_attempt"),
        new("attempts-browser", 8, 9, "rows", "admitted", "429_no_new_attempt"),
        new("session-idle", 1_800_000, 1_800_001, "ms", "401_expired", "401_expired"),
        new("session-absolute", 28_800_000, 28_800_001, "ms", "401_expired", "401_expired"),
        new("sessions-user", 8, 9, "rows", "admitted", "429_no_new_session"),
        new("sessions-installation", 50_000, 50_001, "rows", "admitted", "429_no_new_session"),
        new("users", 10_000, 10_001, "rows", "admitted", "429_no_new_user"),
        new("enrollment-secret", 43, 44, "base64url_characters", "admitted", "400_no_enrollment"),
        new("enrollment-randomness", 256, 255, "bits", "admitted", "refused"),
        new("enrollment-lifetime", 900_000, 900_001, "ms", "409_expired", "409_expired"),
        new("eligible-enrollment-race", 20, 21, "contenders", "one_commit_nineteen_conflicts", "not_a_capacity_limit"),
        new("fresh-host-restarts", 3, 2, "processes", "exact_identity_preservation", "insufficient_evidence"),
        new("callback", 4000, 4001, "ms", "deadline_failure", "no_cookie_or_success"),
        new("local-work", 1000, 1001, "ms", "503_deadline", "503_no_success"),
        new("adapter", 4500, 4501, "ms", "503_deadline", "503_no_success"),
        new("post-return-ui", 5000, 5001, "ms", "actionable", "acceptance_failure"),
        new("backchannel-requests", 3, 4, "requests", "admitted", "no_fourth_request"),
        new("backchannel-request", 2000, 2001, "ms", "deadline_failure", "no_cookie"),
        new("backchannel-combined", 3000, 3001, "ms", "deadline_failure", "no_cookie"),
        new("metadata", 262_144, 262_145, "bytes", "admitted_if_valid", "no_login"),
        new("jwks", 262_144, 262_145, "bytes", "admitted_if_valid", "no_login"),
        new("signing-keys", 16, 17, "keys", "admitted_if_valid", "no_login"),
        new("endpoint-destinations", 8, 9, "endpoints", "admitted", "configuration_refused"),
        new("metadata-cache", 900_000, 900_001, "ms", "expired", "expired"),
        new("unknown-key-refresh", 60_000, 59_999, "ms_between", "admitted_once", "no_refresh"),
        new("callback-target", 16_384, 16_385, "bytes", "admitted_if_valid", "400_no_login"),
        new("mutation-body", 8192, 8193, "bytes", "admitted_if_valid", "400_no_mutation"),
        new("token-response", 65_536, 65_537, "bytes", "admitted_if_valid", "no_login"),
        new("id-token", 16_384, 16_385, "bytes", "admitted_if_valid", "no_login"),
        new("headers-and-cookies", 16_384, 16_385, "bytes", "admitted_if_valid", "400_no_mutation"),
        new("auth-cookies", 16, 17, "cookies", "admitted_if_valid", "400_no_mutation"),
        new("issuer", 2048, 2049, "utf8_bytes", "admitted_if_valid", "no_mapping"),
        new("subject", 255, 256, "printable_ascii_bytes", "admitted_if_valid", "no_mapping"),
        new("display", 128, 129, "unicode_scalars", "admitted_if_safe", "no_unsafe_display"),
        new("login-start-installation", 60, 61, "per_minute", "admitted", "429"),
        new("login-start-browser", 10, 11, "per_minute", "admitted", "429"),
        new("callback-installation", 120, 121, "per_minute", "admitted", "429"),
        new("callback-browser", 20, 21, "per_minute", "admitted", "429"),
        new("enrollment-session", 40, 41, "per_minute", "admitted", "429"),
        new("enrollment-installation", 200, 201, "per_minute", "admitted", "429"),
        new("identity-installation", 1200, 1201, "per_minute", "admitted", "429"),
        new("identity-session-or-browser", 120, 121, "per_minute", "admitted", "429"),
        new("admission-windows", 4096, 4097, "live_rows", "admitted", "429"),
        new("pruning-batch", 128, 129, "rows", "bounded", "no_129th_deletion"),
        new("installation-events", 1_000_000, 1_000_001, "rows", "admitted", "503_mutation_rollback"),
        new("session-events", 200_000, 200_001, "rows", "admitted", "503_mutation_rollback"),
        new("installation-event-warning", 900_000, 899_999, "rows", "warning", "below_threshold"),
        new("session-event-warning", 180_000, 179_999, "rows", "warning", "below_threshold"),
        new("session-event-retention", 30, 31, "days", "eligible_for_bounded_pruning", "eligible_for_bounded_pruning"),
        new("diagnostic", 1024, 1025, "bytes", "bounded", "no_oversized_event"),
        new("configuration", 32_768, 32_769, "bytes", "admitted_if_valid", "configuration_refused"),
        new("client-secret", 4096, 4097, "bytes", "admitted_if_valid", "configuration_refused"),
        new("certificate-or-key", 65_536, 65_537, "bytes", "admitted_if_valid", "configuration_refused"),
        new("development-identities", 8, 9, "configured_keys", "admitted", "configuration_refused"),
        new("development-identities-minimum", 1, 0, "configured_keys", "admitted", "configuration_refused"),
        new("maintenance", 120_000, 120_001, "ms", "deadline_failure", "no_lease_release_before_exit"),
        new("key-rotation", 90, 91, "days", "rotation_retains_valid_old_keys", "not_session_revocation"),
        new("checked-counter", 9_223_372_036_854_775_807m, 9_223_372_036_854_775_808m, "integer", "no_overflow", "503_mutation_rollback")
    });
}
