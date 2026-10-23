CREATE TABLE __kofte_installation (
    singleton_key INTEGER PRIMARY KEY CHECK (singleton_key = 1),
    installation_id TEXT NOT NULL UNIQUE CHECK (
      length(installation_id) = 36 AND length(replace(installation_id, '-', '')) = 32 AND
      substr(installation_id, 9, 1) = '-' AND substr(installation_id, 14, 1) = '-' AND
      substr(installation_id, 19, 1) = '-' AND substr(installation_id, 24, 1) = '-' AND
      replace(installation_id, '-', '') NOT GLOB '*[^0-9a-f]*' AND
      installation_id <> '00000000-0000-0000-0000-000000000000'),
    provider TEXT NOT NULL CHECK (provider = 'sqlite'),
    registry_version INTEGER NOT NULL CHECK (registry_version = 1),
    registry_revision INTEGER NOT NULL CHECK (registry_revision >= 0),
    created_at_utc TEXT NOT NULL
);
CREATE TABLE __kofte_registry_migrations (
    version INTEGER PRIMARY KEY CHECK (version = 1),
    sha256 TEXT NOT NULL CHECK (length(sha256) = 64 AND sha256 NOT GLOB '*[^0-9a-f]*'),
    applied_at_utc TEXT NOT NULL
);
CREATE TABLE __kofte_module_registrations (
    module_id TEXT COLLATE BINARY PRIMARY KEY NOT NULL
      CHECK (length(module_id) BETWEEN 5 AND 253 AND module_id NOT GLOB '*[^a-z0-9.-]*'),
    relational INTEGER NOT NULL CHECK (relational IN (0, 1)),
    provider TEXT NOT NULL CHECK (provider = 'sqlite'),
    allocation_key TEXT COLLATE BINARY UNIQUE
      CHECK (allocation_key IS NULL OR
        (length(allocation_key) BETWEEN 36 AND 63 AND
         allocation_key GLOB 'm_*' AND allocation_key NOT GLOB '*[^a-z0-9_]*' AND
         substr(allocation_key, -33, 1) = '_' AND substr(allocation_key, -32) NOT GLOB '*[^0-9a-f]*')),
    allocator_version INTEGER CHECK (allocator_version IS NULL OR allocator_version = 1),
    allocation_id TEXT UNIQUE CHECK (allocation_id IS NULL OR (
      length(allocation_id) = 36 AND length(replace(allocation_id, '-', '')) = 32 AND
      substr(allocation_id, 9, 1) = '-' AND substr(allocation_id, 14, 1) = '-' AND
      substr(allocation_id, 19, 1) = '-' AND substr(allocation_id, 24, 1) = '-' AND
      replace(allocation_id, '-', '') NOT GLOB '*[^0-9a-f]*' AND
      allocation_id <> '00000000-0000-0000-0000-000000000000')),
    state TEXT NOT NULL CHECK (state IN ('no_storage', 'reserved', 'ready', 'reconciliation_required')),
    migration_state TEXT NOT NULL CHECK (migration_state IN ('none', 'in_progress', 'current', 'recovery_required')),
    migration_target TEXT CHECK (migration_target IS NULL OR
      (length(migration_target) BETWEEN 1 AND 150 AND migration_target NOT GLOB '*[^ -~]*')),
    migration_checkpoint TEXT CHECK (migration_checkpoint IS NULL OR
      (length(migration_checkpoint) BETWEEN 1 AND 150 AND migration_checkpoint NOT GLOB '*[^ -~]*')),
    last_outcome_code TEXT CHECK (last_outcome_code IS NULL OR last_outcome_code IN
      ('storage_busy', 'storage_conflict', 'storage_target_reserved', 'storage_path_rejected',
       'storage_owner_mismatch', 'storage_missing', 'storage_corrupt', 'storage_incomplete',
       'storage_reconciliation_required', 'storage_version_unsupported', 'storage_privilege_denied',
       'storage_unavailable', 'storage_deadline_exceeded', 'storage_cancelled', 'storage_request_invalid')),
    revision INTEGER NOT NULL CHECK (revision > 0),
    CHECK ((relational = 0 AND allocation_key IS NULL AND allocator_version IS NULL AND
            allocation_id IS NULL AND state = 'no_storage' AND migration_state = 'none' AND
            migration_target IS NULL AND migration_checkpoint IS NULL) OR
           (relational = 1 AND allocation_key IS NOT NULL AND allocator_version IS NOT NULL AND
            allocation_id IS NOT NULL AND state <> 'no_storage'))
);
CREATE TRIGGER __kofte_module_id_valid BEFORE INSERT ON __kofte_module_registrations BEGIN
    SELECT CASE WHEN (
      WITH RECURSIVE labels(label, rest) AS (
        SELECT '', NEW.module_id || '.'
        UNION ALL
        SELECT substr(rest, 1, instr(rest, '.') - 1), substr(rest, instr(rest, '.') + 1)
        FROM labels WHERE rest <> ''
      )
      SELECT count(*) < 3 OR max(length(label) > 63 OR length(label) < 1 OR
        substr(label, 1, 1) = '-' OR substr(label, -1) = '-')
      FROM labels WHERE label <> '' OR rest <> NEW.module_id || '.'
    ) THEN RAISE(ABORT, 'storage_id_invalid') END;
END;
CREATE TRIGGER __kofte_registration_immutable BEFORE UPDATE ON __kofte_module_registrations BEGIN
    SELECT CASE WHEN NEW.module_id IS NOT OLD.module_id OR NEW.relational IS NOT OLD.relational OR
      NEW.provider IS NOT OLD.provider OR NEW.allocation_key IS NOT OLD.allocation_key OR
      NEW.allocator_version IS NOT OLD.allocator_version OR NEW.allocation_id IS NOT OLD.allocation_id OR
      NEW.revision <> OLD.revision + 1 THEN RAISE(ABORT, 'storage_owner_immutable') END;
END;
CREATE TRIGGER __kofte_registration_keep BEFORE DELETE ON __kofte_module_registrations BEGIN
    SELECT RAISE(ABORT, 'storage_owner_immutable');
END;
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
PRAGMA user_version = 1;
