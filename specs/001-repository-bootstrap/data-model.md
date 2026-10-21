# Data Model: 001 - Platform-Only Bootstrap

**Status**: Design only; no database/schema implementation

**Requirements**: FR-005/007/009/010/014-016; US1.3, US2.2, US3

## Persistent scope

There are **no shipped application tables**, entities, EF DbContexts, migration
history or module allocation registry in 001. Configuration is operator-owned
environment input, status is observed transient data and verification reports
are ignored artifacts. No user/team/resource/module data or relationship exists.

| Concept | Fields and invariants | Storage / owner |
| --- | --- | --- |
| Development profile | Name `local-sqlite` or `test-postgres`; validated provider, loopback bindings, canonical data root or secret connection source, bounded timeouts | Environment; platform-owned, not a tenant or database row |
| Bootstrap status | Version `1`, observed state, safe component, trace correlation; server-owned, never accepted as request input | Transient response; no cached healthy database record |
| Platform store | SQLite fixed `platform/platform.db` beneath restricted root, or PostgreSQL `kofte_system` inside an explicitly owned test database | Host-owned only; provider never switches on failure |
| Test fixture | Run ID, random fixture key and opaque test payload; no production meaning | Test-only table `bootstrap_fixture` in an isolated platform file or `kofte_system`; never `public` |
| Verification result | Revision, suite, command, outcome, tool versions, environment, duration and safe failure reference | `artifacts/verification/`; ignored and never a feature-acceptance record |

The fixture's key is unique and scoped to its run; tests compare exact payload
after read/write, restart/reconnect and restored backup. Fixture tables are
created/dropped by tests, not host startup and not HTTP. Do not ship them as a
hidden domain schema or require developer stores to contain fixture data.

## Provider rules

**SQLite**: Explicit init creates the file only at the fixed suffix. Ordinary
startup uses read/write-open, not create-if-missing. A new selected-provider
connection is probed on every readiness check; a bounded write transaction is
rolled back without application data changes. A locked file can be temporarily
unready without being corrupt. Foreign keys enabled; rollback journal initially.
Resolve/validate the data root and each existing path component; reject traversal,
symlink/reparse escapes and non-regular files before modifying data. Restrict
root ownership/permissions. A configurable trusted root is not an untrusted
module-supplied filename. No `ATTACH`, module file or cross-database join.

**PostgreSQL**: Verification harness creates a fresh unpredictable database and
records ownership before DDL. Explicit init creates `kofte_system` there.
The application role is scoped to this database/schema and need not be a server
superuser. Readiness requires real connectivity and appropriate schema usage/
creation privileges; fixture tests prove actual read/write persistence.
Use schema-qualified fixture DDL and SQL. Never allocate a module schema,
use `public` or infer concurrent module isolation from this test.

An externally provided test service requires explicit disposable-test designation,
loopback and create-new-database capability. Neither that flag nor a database-name
prefix permits deletion of pre-existing data. Cleanup uses the current run's
created-object ownership record, including exact database and Compose project;
missing ownership proof fails closed. Every provider test gets its own root or
database. No connection string or test payload appears in reports.

## Lifecycle and recovery

Store states are `absent -> explicitly initialized -> usable`, with failure
states `unavailable` (lock, outage, access) and `incompatible` (corrupt/unsupported
state). Ordinary startup from `absent` or `incompatible` fails safely. Returning
from an outage requires a fresh successful probe, not replacing the store.

Initialization never overwrites. Backup/restore/reset require stopped host and
exclusive store-operation lock. SQLite backup uses the provider backup API with
a quiesced file; verify reopening before treating it as usable. PostgreSQL tests
dump/restore fixture schema/data into a newly owned database with compatible
server/client tools. Restoration to existing data requires backup and separate
explicit destructive confirmation; tests normally use empty destinations.
Non-reset stop/retry never deletes persistent state.

No application schema version is introduced, so there is no schema upgrade/
downgrade or registry migration to run. Test unchanged-store compatibility across
restarts and reject corrupt/incompatible state without automatic repair.
Future schema-bearing specs must add versioned provider-specific migrations and
their clean-install/upgrade/rollback or forward-recovery evidence.

## Status transitions

Host process: `starting -> live`, with readiness independently
`unavailable -> ready -> unavailable -> ready` as dependency state changes.
Liveness does not query the database. Configuration errors stop startup with a
safe error/nonzero exit; no unconfigured host reports readiness.

Browser: `loading -> ready | unavailable`; retry/refresh begins a bounded attempt.
The last successful state cannot outlive the next failed/timed-out attempt.
Stale/aborted responses cannot overwrite the newest attempt. See the
[UI contract](contracts/development-and-ui.md) for timing and announcements.
