CREATE TABLE kofte_system.__kofte_installation (
    singleton_key INTEGER PRIMARY KEY CHECK (singleton_key = 1),
    installation_id UUID NOT NULL UNIQUE CHECK (installation_id <> '00000000-0000-0000-0000-000000000000'),
    provider TEXT NOT NULL CHECK (provider = 'postgresql'),
    registry_version INTEGER NOT NULL CHECK (registry_version = 1),
    registry_revision BIGINT NOT NULL CHECK (registry_revision >= 0),
    created_at_utc TIMESTAMPTZ NOT NULL
);
CREATE TABLE kofte_system.__kofte_registry_migrations (
    version INTEGER PRIMARY KEY CHECK (version = 1),
    sha256 TEXT NOT NULL CHECK (sha256 ~ '^[0-9a-f]{64}$'),
    applied_at_utc TIMESTAMPTZ NOT NULL
);
CREATE TABLE kofte_system.__kofte_module_registrations (
    module_id TEXT COLLATE "C" PRIMARY KEY
      CHECK (octet_length(module_id) BETWEEN 5 AND 253 AND
        module_id ~ '^[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?(\.[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?){2,}$'),
    relational BOOLEAN NOT NULL,
    provider TEXT NOT NULL CHECK (provider = 'postgresql'),
    allocation_key TEXT COLLATE "C" UNIQUE CHECK (allocation_key IS NULL OR
      (octet_length(allocation_key) <= 63 AND allocation_key ~ '^m_[a-z0-9_]+_[0-9a-f]{32}$')),
    allocator_version INTEGER CHECK (allocator_version IS NULL OR allocator_version = 1),
    allocation_id UUID UNIQUE CHECK (allocation_id IS NULL OR allocation_id <> '00000000-0000-0000-0000-000000000000'),
    state TEXT NOT NULL CHECK (state IN ('no_storage', 'reserved', 'ready', 'reconciliation_required')),
    migration_state TEXT NOT NULL CHECK (migration_state IN ('none', 'in_progress', 'current', 'recovery_required')),
    migration_target TEXT CHECK (migration_target IS NULL OR migration_target ~ '^[ -~]{1,150}$'),
    migration_checkpoint TEXT CHECK (migration_checkpoint IS NULL OR migration_checkpoint ~ '^[ -~]{1,150}$'),
    last_outcome_code TEXT CHECK (last_outcome_code IS NULL OR last_outcome_code IN
      ('storage_busy', 'storage_conflict', 'storage_target_reserved', 'storage_path_rejected',
       'storage_owner_mismatch', 'storage_missing', 'storage_corrupt', 'storage_incomplete',
       'storage_reconciliation_required', 'storage_version_unsupported', 'storage_privilege_denied',
       'storage_unavailable', 'storage_deadline_exceeded', 'storage_cancelled', 'storage_request_invalid')),
    revision BIGINT NOT NULL CHECK (revision > 0),
    CHECK ((NOT relational AND allocation_key IS NULL AND allocator_version IS NULL AND
            allocation_id IS NULL AND state = 'no_storage' AND migration_state = 'none' AND
            migration_target IS NULL AND migration_checkpoint IS NULL) OR
           (relational AND allocation_key IS NOT NULL AND allocator_version IS NOT NULL AND
            allocation_id IS NOT NULL AND state <> 'no_storage'))
);
CREATE FUNCTION kofte_system.__kofte_registration_change() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP = 'DELETE' THEN RAISE EXCEPTION 'storage_owner_immutable'; END IF;
    IF TG_OP = 'UPDATE' AND (
        ROW(NEW.module_id, NEW.relational, NEW.provider, NEW.allocation_key, NEW.allocator_version, NEW.allocation_id)
        IS DISTINCT FROM ROW(OLD.module_id, OLD.relational, OLD.provider, OLD.allocation_key, OLD.allocator_version, OLD.allocation_id)
        OR NEW.revision <> OLD.revision + 1) THEN RAISE EXCEPTION 'storage_owner_immutable'; END IF;
    UPDATE kofte_system.__kofte_installation SET registry_revision = registry_revision + 1 WHERE singleton_key = 1;
    RETURN NEW;
END $$;
CREATE TRIGGER __kofte_registration_change BEFORE INSERT OR UPDATE OR DELETE
ON kofte_system.__kofte_module_registrations FOR EACH ROW EXECUTE FUNCTION kofte_system.__kofte_registration_change();
CREATE FUNCTION kofte_system.__kofte_installation_change() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    IF TG_OP = 'DELETE' OR
       ROW(NEW.singleton_key, NEW.installation_id, NEW.provider, NEW.registry_version, NEW.created_at_utc)
       IS DISTINCT FROM ROW(OLD.singleton_key, OLD.installation_id, OLD.provider, OLD.registry_version, OLD.created_at_utc)
       OR NEW.registry_revision <> OLD.registry_revision + 1 THEN RAISE EXCEPTION 'storage_installation_immutable'; END IF;
    RETURN NEW;
END $$;
CREATE TRIGGER __kofte_installation_change BEFORE UPDATE OR DELETE ON kofte_system.__kofte_installation
FOR EACH ROW EXECUTE FUNCTION kofte_system.__kofte_installation_change();
