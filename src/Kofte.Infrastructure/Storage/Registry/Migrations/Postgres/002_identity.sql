ALTER TABLE kofte_system.__kofte_installation DROP CONSTRAINT __kofte_installation_registry_version_check;
ALTER TABLE kofte_system.__kofte_installation ADD CONSTRAINT __kofte_installation_registry_version_check CHECK (registry_version = 2) NOT VALID;
ALTER TABLE kofte_system.__kofte_registry_migrations DROP CONSTRAINT __kofte_registry_migrations_version_check;
ALTER TABLE kofte_system.__kofte_registry_migrations ADD CONSTRAINT __kofte_registry_migrations_version_check CHECK (version IN (1, 2));
DROP TRIGGER __kofte_installation_change ON kofte_system.__kofte_installation;
UPDATE kofte_system.__kofte_installation SET registry_version = 2 WHERE singleton_key = 1;
ALTER TABLE kofte_system.__kofte_installation VALIDATE CONSTRAINT __kofte_installation_registry_version_check;
CREATE TRIGGER __kofte_installation_change BEFORE UPDATE OR DELETE ON kofte_system.__kofte_installation
FOR EACH ROW EXECUTE FUNCTION kofte_system.__kofte_installation_change();
CREATE TABLE kofte_system.__kofte_identity_metadata (
    singleton_key INTEGER PRIMARY KEY CHECK (singleton_key = 1),
    installation_id UUID NOT NULL REFERENCES kofte_system.__kofte_installation (installation_id),
    identity_schema_version INTEGER NOT NULL CHECK (identity_schema_version = 1),
    security_revision BIGINT NOT NULL CHECK (security_revision >= 0),
    session_epoch UUID NOT NULL CHECK (session_epoch <> '00000000-0000-0000-0000-000000000000'),
    mode TEXT COLLATE "C" NOT NULL CHECK (mode IN ('oidc', 'development')),
    trust_digest TEXT COLLATE "C" NOT NULL CHECK (trust_digest ~ '^[0-9a-f]{64}$'),
    reconciled_at_utc TIMESTAMPTZ NOT NULL
);
CREATE TABLE kofte_system.__kofte_users (
    user_id UUID PRIMARY KEY CHECK (user_id <> '00000000-0000-0000-0000-000000000000'),
    mode TEXT COLLATE "C" NOT NULL CHECK (mode IN ('oidc', 'development')),
    display_name TEXT CHECK (display_name IS NULL OR (char_length(display_name) <= 128 AND
      display_name !~ '[\u0001-\u001f\u007f-\u009f]')),
    created_at_utc TIMESTAMPTZ NOT NULL,
    updated_at_utc TIMESTAMPTZ NOT NULL CHECK (updated_at_utc >= created_at_utc),
    UNIQUE (user_id, mode)
);
CREATE TABLE kofte_system.__kofte_external_identities (
    mode TEXT COLLATE "C" NOT NULL CHECK (mode IN ('oidc', 'development')),
    issuer TEXT COLLATE "C" NOT NULL CHECK (octet_length(issuer) BETWEEN 1 AND 2048),
    subject TEXT COLLATE "C" NOT NULL CHECK (octet_length(subject) BETWEEN 1 AND 255 AND subject ~ '^[ -~]+$'),
    user_id UUID NOT NULL UNIQUE,
    created_at_utc TIMESTAMPTZ NOT NULL,
    PRIMARY KEY (mode, issuer, subject),
    FOREIGN KEY (user_id, mode) REFERENCES kofte_system.__kofte_users (user_id, mode),
    CHECK ((mode = 'development' AND issuer = 'kofte:development:v1') OR
      (mode = 'oidc' AND issuer <> 'kofte:development:v1'))
);
CREATE TABLE kofte_system.__kofte_login_attempts (
    attempt_id UUID PRIMARY KEY CHECK (attempt_id <> '00000000-0000-0000-0000-000000000000'),
    browser_digest TEXT COLLATE "C" NOT NULL CHECK (browser_digest ~ '^[0-9a-f]{64}$'),
    mode TEXT COLLATE "C" NOT NULL CHECK (mode IN ('oidc', 'development')),
    trust_digest TEXT COLLATE "C" NOT NULL CHECK (trust_digest ~ '^[0-9a-f]{64}$'),
    session_epoch UUID NOT NULL CHECK (session_epoch <> '00000000-0000-0000-0000-000000000000'),
    return_path TEXT COLLATE "C" NOT NULL CHECK (return_path IN ('/', '/identity')),
    created_at_utc TIMESTAMPTZ NOT NULL,
    expires_at_utc TIMESTAMPTZ NOT NULL CHECK (expires_at_utc > created_at_utc),
    state TEXT COLLATE "C" NOT NULL CHECK (state IN ('pending', 'consumed')),
    consumed_at_utc TIMESTAMPTZ,
    CHECK ((state = 'pending' AND consumed_at_utc IS NULL) OR
      (state = 'consumed' AND consumed_at_utc IS NOT NULL AND consumed_at_utc >= created_at_utc))
);
CREATE INDEX __kofte_attempts_browser ON kofte_system.__kofte_login_attempts (browser_digest, state, expires_at_utc);
CREATE INDEX __kofte_attempts_expiry ON kofte_system.__kofte_login_attempts (state, expires_at_utc);
CREATE TABLE kofte_system.__kofte_sessions (
    reference_digest TEXT COLLATE "C" PRIMARY KEY CHECK (reference_digest ~ '^[0-9a-f]{64}$'),
    user_id UUID NOT NULL,
    mode TEXT COLLATE "C" NOT NULL CHECK (mode IN ('oidc', 'development')),
    session_epoch UUID NOT NULL CHECK (session_epoch <> '00000000-0000-0000-0000-000000000000'),
    protected_ticket BYTEA NOT NULL CHECK (octet_length(protected_ticket) > 0),
    created_at_utc TIMESTAMPTZ NOT NULL,
    last_seen_at_utc TIMESTAMPTZ NOT NULL CHECK (last_seen_at_utc >= created_at_utc),
    idle_expires_at_utc TIMESTAMPTZ NOT NULL CHECK (idle_expires_at_utc > last_seen_at_utc),
    absolute_expires_at_utc TIMESTAMPTZ NOT NULL CHECK (absolute_expires_at_utc > created_at_utc AND idle_expires_at_utc <= absolute_expires_at_utc),
    state TEXT COLLATE "C" NOT NULL CHECK (state IN ('active', 'revoked', 'expired')),
    terminal_at_utc TIMESTAMPTZ,
    FOREIGN KEY (user_id, mode) REFERENCES kofte_system.__kofte_users (user_id, mode),
    CHECK ((state = 'active' AND terminal_at_utc IS NULL) OR
      (state IN ('revoked', 'expired') AND terminal_at_utc IS NOT NULL AND terminal_at_utc >= created_at_utc))
);
CREATE INDEX __kofte_sessions_user ON kofte_system.__kofte_sessions (user_id, state, idle_expires_at_utc);
CREATE INDEX __kofte_sessions_expiry ON kofte_system.__kofte_sessions (state, idle_expires_at_utc);
CREATE TABLE kofte_system.__kofte_bootstrap (
    singleton_key INTEGER PRIMARY KEY CHECK (singleton_key = 1),
    installation_id UUID NOT NULL REFERENCES kofte_system.__kofte_installation (installation_id),
    state TEXT COLLATE "C" NOT NULL CHECK (state IN ('disabled', 'pending', 'consumed', 'recovery_locked')),
    generation BIGINT NOT NULL CHECK (generation >= 0),
    nominee_issuer TEXT COLLATE "C" CHECK (nominee_issuer IS NULL OR octet_length(nominee_issuer) BETWEEN 1 AND 2048),
    nominee_subject TEXT COLLATE "C" CHECK (nominee_subject IS NULL OR
      (octet_length(nominee_subject) BETWEEN 1 AND 255 AND nominee_subject ~ '^[ -~]+$')),
    authorization_digest TEXT COLLATE "C" CHECK (authorization_digest IS NULL OR authorization_digest ~ '^[0-9a-f]{64}$'),
    expires_at_utc TIMESTAMPTZ,
    initial_user_id UUID UNIQUE,
    initial_user_mode TEXT COLLATE "C" NOT NULL CHECK (initial_user_mode = 'oidc'),
    consumed_at_utc TIMESTAMPTZ,
    consumed_generation BIGINT CHECK (consumed_generation IS NULL OR (consumed_generation > 0 AND consumed_generation <= generation)),
    FOREIGN KEY (initial_user_id, initial_user_mode) REFERENCES kofte_system.__kofte_users (user_id, mode),
    CHECK ((state = 'pending' AND generation > 0 AND nominee_issuer IS NOT NULL AND nominee_subject IS NOT NULL AND
        authorization_digest IS NOT NULL AND expires_at_utc IS NOT NULL AND initial_user_id IS NULL AND consumed_at_utc IS NULL AND consumed_generation IS NULL) OR
      (state = 'disabled' AND nominee_issuer IS NULL AND nominee_subject IS NULL AND authorization_digest IS NULL AND
        expires_at_utc IS NULL AND initial_user_id IS NULL AND consumed_at_utc IS NULL AND consumed_generation IS NULL) OR
      (state = 'consumed' AND nominee_issuer IS NULL AND nominee_subject IS NULL AND authorization_digest IS NULL AND
        expires_at_utc IS NULL AND initial_user_id IS NOT NULL AND consumed_at_utc IS NOT NULL AND consumed_generation IS NOT NULL) OR
      (state = 'recovery_locked' AND nominee_issuer IS NULL AND nominee_subject IS NULL AND authorization_digest IS NULL AND
        expires_at_utc IS NULL AND ((initial_user_id IS NULL AND consumed_at_utc IS NULL AND consumed_generation IS NULL) OR
        (initial_user_id IS NOT NULL AND consumed_at_utc IS NOT NULL AND consumed_generation IS NOT NULL))))
);
CREATE TABLE kofte_system.__kofte_identity_events (
    event_id UUID PRIMARY KEY CHECK (event_id <> '00000000-0000-0000-0000-000000000000'),
    security_revision BIGINT NOT NULL CHECK (security_revision >= 0),
    event_code TEXT COLLATE "C" NOT NULL CHECK (event_code IN (
      'user_created', 'initial_administrator_enrolled', 'bootstrap_authorized', 'bootstrap_disabled',
      'configuration_reconciled', 'sessions_revoked', 'restore_reconciled', 'keys_prepared',
      'session_created', 'session_revoked', 'session_expired')),
    occurred_at_utc TIMESTAMPTZ NOT NULL,
    correlation_id TEXT COLLATE "C" NOT NULL CHECK (correlation_id ~ '^[0-9a-f]{32}$'),
    user_id UUID REFERENCES kofte_system.__kofte_users (user_id),
    outcome TEXT COLLATE "C" NOT NULL CHECK (outcome = 'committed'),
    retention_class TEXT COLLATE "C" NOT NULL CHECK (retention_class IN ('installation', 'session')),
    CHECK ((event_code IN ('session_created', 'session_revoked', 'session_expired') AND retention_class = 'session') OR
      (event_code NOT IN ('session_created', 'session_revoked', 'session_expired') AND retention_class = 'installation'))
);
CREATE INDEX __kofte_events_retention ON kofte_system.__kofte_identity_events (retention_class, occurred_at_utc);
CREATE FUNCTION kofte_system.__kofte_event_change() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP = 'UPDATE' OR OLD.retention_class = 'installation' THEN RAISE EXCEPTION 'identity_event_immutable'; END IF;
    RETURN OLD;
END $$;
CREATE TRIGGER __kofte_events_keep BEFORE UPDATE OR DELETE ON kofte_system.__kofte_identity_events
FOR EACH ROW EXECUTE FUNCTION kofte_system.__kofte_event_change();
CREATE TABLE kofte_system.__kofte_identity_admission (
    category TEXT COLLATE "C" NOT NULL CHECK (category IN (
      'login_installation', 'login_browser', 'callback_installation', 'callback_browser',
      'enrollment_installation', 'enrollment_session', 'identity_installation', 'identity_client')),
    window_start_utc TIMESTAMPTZ NOT NULL,
    key_digest TEXT COLLATE "C" NOT NULL CHECK (key_digest ~ '^[0-9a-f]{64}$'),
    expires_at_utc TIMESTAMPTZ NOT NULL CHECK (expires_at_utc > window_start_utc),
    count BIGINT NOT NULL CHECK (count >= 0),
    PRIMARY KEY (category, window_start_utc, key_digest)
);
CREATE INDEX __kofte_admission_expiry ON kofte_system.__kofte_identity_admission (expires_at_utc);
