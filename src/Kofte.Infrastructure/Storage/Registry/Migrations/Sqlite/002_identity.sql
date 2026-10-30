DROP TRIGGER __kofte_installation_immutable;
DROP TRIGGER __kofte_registry_insert;
DROP TRIGGER __kofte_registry_update;
CREATE TABLE __kofte_installation_v2 (
    singleton_key INTEGER PRIMARY KEY CHECK (singleton_key = 1),
    installation_id TEXT NOT NULL UNIQUE CHECK (
      length(installation_id) = 36 AND length(replace(installation_id, '-', '')) = 32 AND
      substr(installation_id, 9, 1) = '-' AND substr(installation_id, 14, 1) = '-' AND
      substr(installation_id, 19, 1) = '-' AND substr(installation_id, 24, 1) = '-' AND
      replace(installation_id, '-', '') NOT GLOB '*[^0-9a-f]*' AND
      installation_id <> '00000000-0000-0000-0000-000000000000'),
    provider TEXT NOT NULL CHECK (provider = 'sqlite'),
    registry_version INTEGER NOT NULL CHECK (registry_version = 2),
    registry_revision INTEGER NOT NULL CHECK (typeof(registry_revision) = 'integer' AND registry_revision >= 0),
    created_at_utc TEXT NOT NULL
);
INSERT INTO __kofte_installation_v2
SELECT singleton_key, installation_id, provider, 2, registry_revision, created_at_utc FROM __kofte_installation;
DROP TABLE __kofte_installation;
ALTER TABLE __kofte_installation_v2 RENAME TO __kofte_installation;
CREATE TABLE __kofte_registry_migrations_v2 (
    version INTEGER PRIMARY KEY CHECK (version IN (1, 2)),
    sha256 TEXT NOT NULL CHECK (length(sha256) = 64 AND sha256 NOT GLOB '*[^0-9a-f]*'),
    applied_at_utc TEXT NOT NULL
);
INSERT INTO __kofte_registry_migrations_v2 SELECT * FROM __kofte_registry_migrations;
DROP TABLE __kofte_registry_migrations;
ALTER TABLE __kofte_registry_migrations_v2 RENAME TO __kofte_registry_migrations;
CREATE TRIGGER __kofte_registry_insert AFTER INSERT ON __kofte_module_registrations BEGIN
    UPDATE __kofte_installation SET registry_revision = registry_revision + 1 WHERE singleton_key = 1;
END;
CREATE TRIGGER __kofte_registry_update AFTER UPDATE ON __kofte_module_registrations BEGIN
    UPDATE __kofte_installation SET registry_revision = registry_revision + 1 WHERE singleton_key = 1;
END;
CREATE TRIGGER __kofte_installation_immutable BEFORE UPDATE ON __kofte_installation BEGIN
    SELECT CASE WHEN NEW.singleton_key IS NOT OLD.singleton_key OR NEW.installation_id IS NOT OLD.installation_id OR
      NEW.provider IS NOT OLD.provider OR NEW.registry_version IS NOT OLD.registry_version OR
      NEW.created_at_utc IS NOT OLD.created_at_utc OR NEW.registry_revision <> OLD.registry_revision + 1
      THEN RAISE(ABORT, 'storage_installation_immutable') END;
END;
CREATE TABLE __kofte_identity_metadata (
    singleton_key INTEGER PRIMARY KEY CHECK (singleton_key = 1),
    installation_id TEXT NOT NULL REFERENCES __kofte_installation (installation_id),
    identity_schema_version INTEGER NOT NULL CHECK (identity_schema_version = 1),
    security_revision INTEGER NOT NULL CHECK (typeof(security_revision) = 'integer' AND security_revision >= 0),
    session_epoch TEXT COLLATE BINARY NOT NULL CHECK (
      length(session_epoch) = 36 AND length(replace(session_epoch, '-', '')) = 32 AND
      substr(session_epoch, 9, 1) = '-' AND substr(session_epoch, 14, 1) = '-' AND
      substr(session_epoch, 19, 1) = '-' AND substr(session_epoch, 24, 1) = '-' AND
      replace(session_epoch, '-', '') NOT GLOB '*[^0-9a-f]*' AND
      session_epoch <> '00000000-0000-0000-0000-000000000000'),
    mode TEXT COLLATE BINARY NOT NULL CHECK (mode IN ('oidc', 'development')),
    trust_digest TEXT COLLATE BINARY NOT NULL CHECK (length(trust_digest) = 64 AND trust_digest NOT GLOB '*[^0-9a-f]*'),
    reconciled_at_utc TEXT NOT NULL
);
CREATE TABLE __kofte_users (
    user_id TEXT COLLATE BINARY PRIMARY KEY NOT NULL CHECK (
      length(user_id) = 36 AND length(replace(user_id, '-', '')) = 32 AND
      substr(user_id, 9, 1) = '-' AND substr(user_id, 14, 1) = '-' AND
      substr(user_id, 19, 1) = '-' AND substr(user_id, 24, 1) = '-' AND
      replace(user_id, '-', '') NOT GLOB '*[^0-9a-f]*' AND
      user_id <> '00000000-0000-0000-0000-000000000000'),
    mode TEXT COLLATE BINARY NOT NULL CHECK (mode IN ('oidc', 'development')),
    display_name TEXT CHECK (display_name IS NULL OR (length(display_name) <= 128 AND instr(display_name, char(0)) = 0 AND
      display_name NOT GLOB ('*[' || char(1) || '-' || char(31) || char(127) || '-' || char(159) || ']*'))),
    created_at_utc TEXT NOT NULL,
    updated_at_utc TEXT NOT NULL CHECK (updated_at_utc >= created_at_utc),
    UNIQUE (user_id, mode)
);
CREATE TABLE __kofte_external_identities (
    mode TEXT COLLATE BINARY NOT NULL CHECK (mode IN ('oidc', 'development')),
    issuer TEXT COLLATE BINARY NOT NULL CHECK (length(CAST(issuer AS BLOB)) BETWEEN 1 AND 2048 AND instr(issuer, char(0)) = 0),
    subject TEXT COLLATE BINARY NOT NULL CHECK (length(CAST(subject AS BLOB)) BETWEEN 1 AND 255 AND
      instr(subject, char(0)) = 0 AND subject NOT GLOB '*[^ -~]*'),
    user_id TEXT COLLATE BINARY NOT NULL UNIQUE,
    created_at_utc TEXT NOT NULL,
    PRIMARY KEY (mode, issuer, subject),
    FOREIGN KEY (user_id, mode) REFERENCES __kofte_users (user_id, mode),
    CHECK ((mode = 'development' AND issuer = 'kofte:development:v1') OR
      (mode = 'oidc' AND issuer <> 'kofte:development:v1'))
);
CREATE TABLE __kofte_login_attempts (
    attempt_id TEXT COLLATE BINARY PRIMARY KEY NOT NULL CHECK (
      length(attempt_id) = 36 AND length(replace(attempt_id, '-', '')) = 32 AND
      substr(attempt_id, 9, 1) = '-' AND substr(attempt_id, 14, 1) = '-' AND
      substr(attempt_id, 19, 1) = '-' AND substr(attempt_id, 24, 1) = '-' AND
      replace(attempt_id, '-', '') NOT GLOB '*[^0-9a-f]*' AND
      attempt_id <> '00000000-0000-0000-0000-000000000000'),
    browser_digest TEXT COLLATE BINARY NOT NULL CHECK (length(browser_digest) = 64 AND browser_digest NOT GLOB '*[^0-9a-f]*'),
    mode TEXT COLLATE BINARY NOT NULL CHECK (mode IN ('oidc', 'development')),
    trust_digest TEXT COLLATE BINARY NOT NULL CHECK (length(trust_digest) = 64 AND trust_digest NOT GLOB '*[^0-9a-f]*'),
    session_epoch TEXT COLLATE BINARY NOT NULL CHECK (
      length(session_epoch) = 36 AND length(replace(session_epoch, '-', '')) = 32 AND
      substr(session_epoch, 9, 1) = '-' AND substr(session_epoch, 14, 1) = '-' AND
      substr(session_epoch, 19, 1) = '-' AND substr(session_epoch, 24, 1) = '-' AND
      replace(session_epoch, '-', '') NOT GLOB '*[^0-9a-f]*' AND
      session_epoch <> '00000000-0000-0000-0000-000000000000'),
    return_path TEXT COLLATE BINARY NOT NULL CHECK (return_path IN ('/', '/identity')),
    created_at_utc TEXT NOT NULL,
    expires_at_utc TEXT NOT NULL CHECK (expires_at_utc > created_at_utc),
    state TEXT COLLATE BINARY NOT NULL CHECK (state IN ('pending', 'consumed')),
    consumed_at_utc TEXT,
    CHECK ((state = 'pending' AND consumed_at_utc IS NULL) OR
      (state = 'consumed' AND consumed_at_utc IS NOT NULL AND consumed_at_utc >= created_at_utc))
);
CREATE INDEX __kofte_attempts_browser ON __kofte_login_attempts (browser_digest, state, expires_at_utc);
CREATE INDEX __kofte_attempts_expiry ON __kofte_login_attempts (state, expires_at_utc);
CREATE TABLE __kofte_sessions (
    reference_digest TEXT COLLATE BINARY PRIMARY KEY NOT NULL CHECK (length(reference_digest) = 64 AND reference_digest NOT GLOB '*[^0-9a-f]*'),
    user_id TEXT COLLATE BINARY NOT NULL,
    mode TEXT COLLATE BINARY NOT NULL CHECK (mode IN ('oidc', 'development')),
    session_epoch TEXT COLLATE BINARY NOT NULL CHECK (
      length(session_epoch) = 36 AND length(replace(session_epoch, '-', '')) = 32 AND
      substr(session_epoch, 9, 1) = '-' AND substr(session_epoch, 14, 1) = '-' AND
      substr(session_epoch, 19, 1) = '-' AND substr(session_epoch, 24, 1) = '-' AND
      replace(session_epoch, '-', '') NOT GLOB '*[^0-9a-f]*' AND
      session_epoch <> '00000000-0000-0000-0000-000000000000'),
    protected_ticket BLOB NOT NULL CHECK (typeof(protected_ticket) = 'blob' AND length(protected_ticket) > 0),
    created_at_utc TEXT NOT NULL,
    last_seen_at_utc TEXT NOT NULL CHECK (last_seen_at_utc >= created_at_utc),
    idle_expires_at_utc TEXT NOT NULL CHECK (idle_expires_at_utc > last_seen_at_utc),
    absolute_expires_at_utc TEXT NOT NULL CHECK (absolute_expires_at_utc > created_at_utc AND idle_expires_at_utc <= absolute_expires_at_utc),
    state TEXT COLLATE BINARY NOT NULL CHECK (state IN ('active', 'revoked', 'expired')),
    terminal_at_utc TEXT,
    FOREIGN KEY (user_id, mode) REFERENCES __kofte_users (user_id, mode),
    CHECK ((state = 'active' AND terminal_at_utc IS NULL) OR
      (state IN ('revoked', 'expired') AND terminal_at_utc IS NOT NULL AND terminal_at_utc >= created_at_utc))
);
CREATE INDEX __kofte_sessions_user ON __kofte_sessions (user_id, state, idle_expires_at_utc);
CREATE INDEX __kofte_sessions_expiry ON __kofte_sessions (state, idle_expires_at_utc);
CREATE TABLE __kofte_bootstrap (
    singleton_key INTEGER PRIMARY KEY CHECK (singleton_key = 1),
    installation_id TEXT NOT NULL REFERENCES __kofte_installation (installation_id),
    state TEXT COLLATE BINARY NOT NULL CHECK (state IN ('disabled', 'pending', 'consumed', 'recovery_locked')),
    generation INTEGER NOT NULL CHECK (typeof(generation) = 'integer' AND generation >= 0),
    nominee_issuer TEXT COLLATE BINARY CHECK (nominee_issuer IS NULL OR
      (length(CAST(nominee_issuer AS BLOB)) BETWEEN 1 AND 2048 AND instr(nominee_issuer, char(0)) = 0)),
    nominee_subject TEXT COLLATE BINARY CHECK (nominee_subject IS NULL OR
      (length(CAST(nominee_subject AS BLOB)) BETWEEN 1 AND 255 AND instr(nominee_subject, char(0)) = 0 AND nominee_subject NOT GLOB '*[^ -~]*')),
    authorization_digest TEXT COLLATE BINARY CHECK (authorization_digest IS NULL OR
      (length(authorization_digest) = 64 AND authorization_digest NOT GLOB '*[^0-9a-f]*')),
    expires_at_utc TEXT,
    initial_user_id TEXT COLLATE BINARY UNIQUE,
    initial_user_mode TEXT COLLATE BINARY NOT NULL CHECK (initial_user_mode = 'oidc'),
    consumed_at_utc TEXT,
    consumed_generation INTEGER CHECK (consumed_generation IS NULL OR
      (typeof(consumed_generation) = 'integer' AND consumed_generation > 0 AND consumed_generation <= generation)),
    FOREIGN KEY (initial_user_id, initial_user_mode) REFERENCES __kofte_users (user_id, mode),
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
CREATE TABLE __kofte_identity_events (
    event_id TEXT COLLATE BINARY PRIMARY KEY NOT NULL CHECK (
      length(event_id) = 36 AND length(replace(event_id, '-', '')) = 32 AND
      substr(event_id, 9, 1) = '-' AND substr(event_id, 14, 1) = '-' AND
      substr(event_id, 19, 1) = '-' AND substr(event_id, 24, 1) = '-' AND
      replace(event_id, '-', '') NOT GLOB '*[^0-9a-f]*' AND
      event_id <> '00000000-0000-0000-0000-000000000000'),
    security_revision INTEGER NOT NULL CHECK (typeof(security_revision) = 'integer' AND security_revision >= 0),
    event_code TEXT COLLATE BINARY NOT NULL CHECK (event_code IN (
      'user_created', 'initial_administrator_enrolled', 'bootstrap_authorized', 'bootstrap_disabled',
      'configuration_reconciled', 'sessions_revoked', 'restore_reconciled', 'keys_prepared',
      'session_created', 'session_revoked', 'session_expired')),
    occurred_at_utc TEXT NOT NULL,
    correlation_id TEXT COLLATE BINARY NOT NULL CHECK (length(correlation_id) = 32 AND correlation_id NOT GLOB '*[^0-9a-f]*'),
    user_id TEXT COLLATE BINARY REFERENCES __kofte_users (user_id),
    outcome TEXT COLLATE BINARY NOT NULL CHECK (outcome = 'committed'),
    retention_class TEXT COLLATE BINARY NOT NULL CHECK (retention_class IN ('installation', 'session')),
    CHECK ((event_code IN ('session_created', 'session_revoked', 'session_expired') AND retention_class = 'session') OR
      (event_code NOT IN ('session_created', 'session_revoked', 'session_expired') AND retention_class = 'installation'))
);
CREATE INDEX __kofte_events_retention ON __kofte_identity_events (retention_class, occurred_at_utc);
CREATE TRIGGER __kofte_events_immutable BEFORE UPDATE ON __kofte_identity_events BEGIN
    SELECT RAISE(ABORT, 'identity_event_immutable');
END;
CREATE TRIGGER __kofte_events_keep BEFORE DELETE ON __kofte_identity_events WHEN OLD.retention_class = 'installation' BEGIN
    SELECT RAISE(ABORT, 'identity_event_immutable');
END;
CREATE TABLE __kofte_identity_admission (
    category TEXT COLLATE BINARY NOT NULL CHECK (category IN (
      'login_installation', 'login_browser', 'callback_installation', 'callback_browser',
      'enrollment_installation', 'enrollment_session', 'identity_installation', 'identity_client')),
    window_start_utc TEXT NOT NULL,
    key_digest TEXT COLLATE BINARY NOT NULL CHECK (length(key_digest) = 64 AND key_digest NOT GLOB '*[^0-9a-f]*'),
    expires_at_utc TEXT NOT NULL CHECK (expires_at_utc > window_start_utc),
    count INTEGER NOT NULL CHECK (typeof(count) = 'integer' AND count >= 0),
    PRIMARY KEY (category, window_start_utc, key_digest)
);
CREATE INDEX __kofte_admission_expiry ON __kofte_identity_admission (expires_at_utc);
PRAGMA user_version = 2;
