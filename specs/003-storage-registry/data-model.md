# Data Model: 003 - Storage Registry and Module Isolation

**Status**: Proposed registry format 1; no database/model is created by planning.
See [storage-v1](contracts/storage-v1.md) for public behavior and
[migration/recovery](contracts/migrations-and-recovery.md) for physical handling.

## Version domains

Storage API contract `1`, registry format integer `1`, allocator integer `1`,
ownership marker integer `1` and recovery manifest `kofte-storage-recovery-v1`
are distinct from 002 manifest schema `1`, host contract `1.0.0`, module release
and module-owned EF migration IDs. Never infer one from another.

## Platform-owned persistence

PostgreSQL tables and platform migration history live only in `kofte_system`.
SQLite equivalents live only in `platform/platform.db`. Use separate
provider-specific registry migrations, with explicit local transactions and
binary/ordinal identifier comparisons. No module payload belongs in these rows.

### InstallationMetadata (exactly one row)

| Field | Invariant |
| --- | --- |
| `singleton_key` | Constant 1, primary key and CHECK |
| `installation_id` | UUID, immutable random installation identity |
| `provider` | `postgresql` or `sqlite`, fixed for this installation |
| `registry_version` | Integer 1; reject unknown/newer values before writes |
| `registry_revision` | Nonnegative 64-bit counter, increment with registry mutations |
| `created_at_utc` | UTC timestamp, not ownership authority |

SQLite `PRAGMA user_version=1` must agree with metadata; v0 has no application
tables/history or installation ID. PostgreSQL recognizes v0 only from accepted
bootstrap ownership plus an empty platform application schema. Do not use a
missing metadata row as permission to initialize an otherwise populated store.

### ModuleRegistrations

| Field | Invariant |
| --- | --- |
| `module_id` | Primary/UNIQUE canonical 002 ID, 5-253 ASCII characters |
| `relational` | Immutable boolean storage need |
| `provider` | Installation provider; never caller-selected per registration |
| `allocation_key` | Null for no-storage, otherwise immutable canonical allocator output |
| `allocator_version` | Null for no-storage, otherwise 1 at new allocation |
| `allocation_id` | Null for no-storage, otherwise immutable random UUID, UNIQUE |
| `state` | `no_storage`, `reserved`, `ready`, `reconciliation_required` |
| `migration_state` | `none`, `in_progress`, `current`, `recovery_required` |
| `migration_target` | Null or bounded module migration ID, at most 150 ASCII characters |
| `migration_checkpoint` | Null or last verified migration ID, same bound |
| `last_outcome_code` | Null or allowlisted safe diagnostic code, never exception/input |
| `revision` | Positive 64-bit optimistic state-transition revision |

Constraints: UNIQUE canonical `allocation_key` for non-null values, plus UNIQUE
`allocation_id`; a separate unique identity constraint must reject direct
duplicate writes even when allocator/lock checks are bypassed. PostgreSQL uses
explicit deterministic/binary collation for these ASCII keys; SQLite uses
`COLLATE BINARY`. CHECK relational/state/null consistency, key grammar/byte cap,
provider vocabulary and nonnegative/valid revisions. Application validation
rechecks canonical identity, reserved names and metadata agreement on reopen.
Do not persist absolute paths, credentials, display names or module release as
ownership keys. The physical location is schema `allocation_key`, or exactly
`modules/<allocation_key>/module.db`; it is not a separately mutable locator.

No-storage rows have allocation fields and migration target/checkpoint null and
`state=no_storage`, `migration_state=none`. Same-ID relational need changes are
rejected; false-to-true is not silently treated as installation.

### Platform migration history

Provider-specific registry SQL migration versions, script SHA-256 digests and
application timestamps reside in `kofte_system.__kofte_registry_migrations` or
the platform file's table of that name. A small platform-owned migration runner
executes the versioned SQL and inserts its history row in the same local
transaction; it does not invoke EF's module migration/lock machinery.
Bootstrap-to-v1 initialization is explicit and transactionally establishes its
own history; no `EnsureCreated` shortcut or fabricated applied EF history.
Later unsupported versions block rather than being down-migrated automatically.

## Physical module ownership marker

Each allocated schema/file contains one platform-reserved
`__kofte_storage_owner` table with a singleton row:

| Field | Invariant |
| --- | --- |
| `marker_version` | Integer 1 |
| `installation_id` | Matches platform metadata |
| `module_id` | Matches registry canonical owner |
| `allocation_id` | Matches the exact immutable reservation |
| `provider` | Matches registry/provider adapter |
| `allocation_key` | Matches persisted physical key |

This table is storage infrastructure metadata, not a module payload or registry
replacement. Modules cannot map/drop it through the normal binding. Its reserved
name, module history and EF's provider lock table are excluded from application
models. A marker alone without its registry row never authorizes adoption.
PostgreSQL also verifies schema owner and privilege posture; SQLite verifies
actual path/type/ownership/link count, header and integrity.

## Module-owned entities and histories

Alpha and Beta are inert **test-owned** consumers, not installed feature modules.
Both define `Records` with independently distinguishable sentinel data and
`__EFMigrationsHistory`; Alpha v2 adds a column while preserving rows. Their
contexts, provider-specific migration sets and snapshots physically live in
their test projects. No FK, join or EF navigation crosses module/platform stores.
SQLite has no assigned schema; PostgreSQL binds every model and migration object
to the persisted schema, including FK principal schemas and history.

## Durable state transitions

```text
absent + relational=false -> no_storage
absent + relational=true  -> reserved
reserved + verified matching physical owner -> ready
reserved + absent target -> provision same allocation, then ready
reserved + ambiguous/unowned/mismatched target -> reconciliation_required
ready + missing/corrupt/mismatched target -> reconciliation_required
reconciliation_required + verified corrected original target -> ready
```

The last transition requires explicit maintenance retry and full verification;
resolution cannot repair state or create storage. None of these transitions
changes module ID/provider/key/allocation token. A no-storage row cannot turn
into a relational row. There is no `deleted`, auto-reassigned or renamed state.

Migration substate moves `none/current -> in_progress -> current` while holding
exclusive installation maintenance. Record intent before EF writes; checkpoint
only after verifying actual module history. Interruption/failure gives
`recovery_required`, either persisted by the surviving worker/supervisor or
inferred from durable `in_progress` on reopen. Do not serve a module assignment
as usable while migration is incomplete. Inspect actual history/marker on retry;
an uncertain commit is not a guarantee of rollback.

## Transaction and uniqueness boundaries

Reservation and each state/revision update are platform-local transactions.
PostgreSQL physical schema/marker creation is one provider-local transaction
under the installation session lock. Registry ready follows verification.
SQLite file creation, marker transaction and platform ready commit are separate
durable boundaries; lock coordination does not turn them into one transaction.

Module EF migration transactions stay in their assigned schema/file; no shared
connection is exposed to other modules. Registry and module commits are not an
extension-level global ACID guarantee, even if PostgreSQL could technically
combine some statements. Constraint violations must be classified by known
constraint identity, not raw provider text; unexpected errors are explicit.

## Recovery-set metadata

The stopped recovery manifest records format, installation/provider/registry
version and revision, recovery-set UUID, complete module inventory, each
allocation/marker/migration state, relative artifact name, length and SHA-256.
It contains no credential or absolute path. Include registry, module files or
database dump, histories and ownership markers. A missing member, unlisted
module object, mismatched revision/checkpoint/hash or unknown format blocks use.

Hashes detect a mixed/corrupt copy, not hostile administrator forgery or the
existence of a newer backup elsewhere. Replacing a live recovery target requires
stopped operation and fresh evidence of that exact pre-change target; prefer
fresh owned destinations preserving originals. Explicit whole-set recovery to
an older chosen checkpoint is not module reassignment or a silent downgrade.
