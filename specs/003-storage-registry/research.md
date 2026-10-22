# Research: 003 - Storage Registry and Module Isolation

**Date**: 2026-10-21
**Status**: Planning decisions, not installed dependencies or behavioral proof.

Official provider documentation and package metadata were consulted for this
stage. Microsoft Learn and Context7 MCP tools are not connected in this session;
official web documentation is the fallback, not fabricated MCP evidence.
The research agent verified package/dependency metadata and provider guidance;
the planning pass separately checked PostgreSQL **17**, SQLite limitations/WAL
and the public EF migration APIs. No private repository content was submitted
to public research services.

## R1 - Reuse the bootstrap rather than create a competing store

**Decision**: Keep the existing SQLite platform path and PostgreSQL
`kofte_system`, owned target/role checks and installation lease. Add explicit
registry initialization/upgrade, never automatic host-startup migration.
Separate provider-neutral contracts from EF/provider implementations in one
infrastructure assembly. Reuse the accepted 002 `ModuleId` and validated
`ModuleDescription`; do not parse IDs/manifests again.

**Rationale**: Existing `SqlitePlatformStore`, `PostgresPlatformStore`,
`UnixStorePaths`, `StoreLease`, `SqliteStoreRecovery` and `PostgresFixture`
already encode private-file, fixture-ownership and runtime privilege invariants.
These are integration constraints, not replaceable examples.

**Alternatives**: A new platform file breaks recovery/startup compatibility;
putting EF in the pure manifest library adds effects/dependencies to an
otherwise offline contract; module references to the web host invert ownership.

## R2 - Persist allocation; combine readable hashing with real uniqueness

**Decision**: Allocator v1 produces
`m_<slug-up-to-24>_<first-32-lower-hex-of-SHA256(canonical-ID-UTF8)>`.
Replace each dot/hyphen in the canonical ID with `_`, collapse runs, trim `_`,
then take the first 24 ASCII characters and trim trailing `_`; use `module` only
if empty. Maximum length is 59 ASCII bytes. PostgreSQL uses that key as schema;
SQLite uses `modules/<key>/module.db`. Persist the output/version permanently.
Separate UUID allocation/installation tokens prove consistency, not the hash.
Database unique constraints on ID and canonical physical key are mandatory.

**Rationale**: Accepted IDs are ASCII lowercase, up to 253 characters; Unicode
display text is never input to naming. Names fit PostgreSQL's 63-byte limit
without truncation by the server. A 128-bit hash suffix reduces accidental
collision but **does not authorize reuse**. Reserved namespace checks, existing
object checks, exclusive maintenance and durable constraints still decide.

**Alternatives**: Display-name slug is mutable/ambiguous; hash-only uniqueness
is not protection; automatic suffix search/remapping breaks fail-closed
collision semantics; regenerating on resolution makes upgrades unsafe.

**Sources**: [PostgreSQL 17 identifiers](https://www.postgresql.org/docs/17/sql-syntax-lexical.html#SQL-SYNTAX-IDENTIFIERS);
accepted [002 identity](../../src/Kofte.Platform/Modules/Manifest/ModuleId.cs);
canonical [ownership rules](../../docs/architecture.md#61-postgresql-one-named-schema-per-module).

## R3 - Preserve runtime PostgreSQL restrictions with offline provisioning

**Decision**: Runtime remains `NOSUPERUSER`, `NOCREATEDB`, `NOCREATEROLE`,
`NOREPLICATION`, `NOBYPASSRLS`, without database CREATE. A distinct offline role
has CONNECT, database CREATE and permission to `SET ROLE` to the runtime owner;
no superuser or CREATEROLE is needed during application provisioning. Role setup
is an operator/owned-fixture responsibility. The provisioner creates the assigned
schema `AUTHORIZATION` runtime owner, revokes PUBLIC access, then uses
`SET LOCAL ROLE` for marker/table creation. Objects are runtime-owned without
relying on implicit default privileges.

**Rationale**: PostgreSQL distinguishes database CREATE (new schemas) from
schema CREATE (objects inside an existing schema). The bootstrap's explicit
negative CREATE check therefore stays correct. Verify both connections refer
to the same owned database and expected roles before writes; a remote/existing
unowned database remains forbidden by the current test profile.

**Alternatives**: Granting runtime database CREATE or bypassing its probe weakens
the accepted baseline. A permanent superuser connection in host DI is unnecessary.
Dedicated per-module login isolation can be added later; shared trusted runtime
credentials are an ownership convention, not a hostile-code sandbox.

**Sources**: [PostgreSQL 17 CREATE SCHEMA](https://www.postgresql.org/docs/17/sql-createschema.html),
[GRANT](https://www.postgresql.org/docs/17/sql-grant.html),
[SET ROLE](https://www.postgresql.org/docs/17/sql-set-role.html).

## R4 - One installation lock across durable phases

**Decision**: PostgreSQL maintenance uses a dedicated non-pooled session holding
the exclusive existing advisory key through registry writes, physical creation
and module migration. Host shared locks conflict with it. SQLite reuses the
existing exclusive `flock` lease and requires a stopped host. A bounded
nonblocking acquisition loop returns busy after two seconds. Within the lock,
registry reservations use explicit transactions (`BEGIN IMMEDIATE` on SQLite).

**Rationale**: A transaction advisory lock alone ends at the first commit, too
early for registry/file/migration phases. A session lock survives transaction
rollback but is released when its session ends. Independent connections and
processes must be tested; process-local semaphores are insufficient.
Do not release a connection that owns the PostgreSQL lock back to a pool.

**Alternatives**: Per-module locks complicate global backup/upgrade coordination;
an in-memory lock misses separate processes; retaining a transaction across
SQLite file work suggests false atomicity and holds a database write lock too long.

**Sources**: [PostgreSQL advisory locks](https://www.postgresql.org/docs/17/explicit-locking.html#ADVISORY-LOCKS),
[SQLite transactions](https://www.sqlite.org/lang_transaction.html),
[Microsoft.Data.Sqlite transactions](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions).

## R5 - Explicit physical ownership and conservative interruption recovery

**Decision**: Commit a reservation with installation/module/allocation tokens
before physical work. PostgreSQL schema creation and its marker commit in one
transaction; mark registry ready only after verifying them. SQLite exclusively
creates the registered file, initializes a marker in a local transaction, then
marks registry ready separately. A matching marker authorizes finishing the
same reservation, not adopting a different file. A pre-marker SQLite crash
leaves ambiguous physical evidence: block reconciliation and preserve it.

**Rationale**: There is no transaction spanning the registry and module files.
An empty or plausible file is not proof that this reservation created it.
Conversely, losing the final response does not prove the commit failed.
Ready resolution never creates missing objects and verifies marker, integrity,
registry/provider version and migration state.

**Alternatives**: `CREATE SCHEMA IF NOT EXISTS`, `ReadWriteCreate`, automatic
cleanup of an empty file or trusting the allocator hash can adopt/delete
unrelated data. A generic distributed transaction or durable job engine is
outside this slice and unnecessary.

**Sources**: [CREATE SCHEMA existing-object behavior](https://www.postgresql.org/docs/17/sql-createschema.html),
[SQLite atomic commit](https://www.sqlite.org/atomiccommit.html),
[SQLite WAL](https://www.sqlite.org/wal.html).

## R6 - SQLite provider limits and honest finite budgets

**Decision**: Explicit rollback-journal `DELETE`/`FULL` baseline, private root
0700/files 0600, foreign keys on, one connection per caller, no pooling/shared
cache. Reuse no-link/single-hard-link/regular-file checks for all ancestors,
stores and sidecars. Open ready stores in `ReadWrite`, never create mode.
SQLite provider calls are synchronous, with one-second busy/command limits
clamped to the remaining operation budget. Long offline SQLite work runs in an
owned maintenance worker process; the supervisor bounds termination/reaping
inside the published deadline and treats killed work as incomplete.

**Rationale**: Microsoft.Data.Sqlite async methods execute synchronously;
`CancellationToken` alone cannot enforce an in-flight deadline. Its implicit
transaction commands use `DefaultTimeout`; explicit commands have their own
timeout. SQLite supports no schemas, so each module gets a real file and
unqualified module-local table/history names. EF may otherwise enable WAL on
creation; explicitly configure/check the chosen journal mode and verify it after
migration. Do not delete a hot journal or abandoned EF lock as generic retry.

**Alternatives**: WAL is viable but adds checkpoint/sidecar recovery obligations
without a demonstrated throughput need for this slice. Copying a live database
file or using `ATTACH` does not provide a consistent cross-module backup.
Network filesystems, hostile same-user filesystem mutation and native Windows
support are not promised. Canonical checks do not sandbox privileged local code.

**Sources**: [SQLite async limitations](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async),
[timeouts and error retries](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/database-errors),
[EF SQLite limitations and abandoned migration lock](https://learn.microsoft.com/en-us/ef/core/providers/sqlite/limitations),
[WAL tradeoffs](https://www.sqlite.org/wal.html).

## R7 - Bind EF models, history AND migration operations

**Decision**: Module-owned provider-specific contexts/migration sets use one
assigned binding. PostgreSQL specifies `HasDefaultSchema`, schema-local history
and schema-qualified model objects; cache keys include context type, provider,
assigned schema and design-time flag. SQLite uses the assigned file with no
schema simulation. A small assigned-migration base and public
`IMigrationsAssembly.CreateMigration` adapter bind owner/provider/schema
**before** EF evaluates operations/target models. Module migration operations,
target model and snapshot must use that binding, not scaffolded literal schemas.
Preflight the complete pending set and reject foreign/unknown/unbound operations
before executing any SQL; no arbitrary raw SQL in this initial binding contract.

**Rationale**: Changing `search_path` or only the history table does not relocate
schema literals generated into `Up`/`BuildTargetModel`. Dynamic models also
require a non-default cache key. These are actual isolation risks; acceptance
tests must use the same context type against multiple assignments and force a
foreign-schema migration. The binder is not general discovery/dependency ordering.
Provider-specific snapshots avoid treating SQLite rebuilds as PostgreSQL DDL.

**Alternatives**: SQL string replacement is unsafe and provider-fragile;
hardcoded allocator-derived schema in every migration makes stored older
mappings unusable; `EnsureCreated` bypasses real migration histories. A general
module installer or support for arbitrary custom migration SQL belongs to a
separately reviewed contract, not a silent extension here.

**Sources**: [Dynamic model/cache key](https://learn.microsoft.com/en-us/ef/core/modeling/dynamic-model),
[migration history](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/history-table),
[multiple providers](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers),
[IMigrationsAssembly.CreateMigration](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.migrations.imigrationsassembly.createmigration?view=efcore-10.0),
[Migration operation/target-model API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.migrations.migration?view=efcore-10.0).

## R8 - Versioned bootstrap upgrade and stopped recovery

**Decision**: Registry format 1 upgrades only an owned, valid, table-free
bootstrap v0 store; versioned provider-specific SQL initialization creates
metadata/registry and records its script digest/version history in the same
local transaction without dropping existing content. Registry initialization
does not invoke EF's separate module migration lock. Recognized v1 is
idempotent; unknown tables/versions or inconsistent ownership block. Host startup
can still probe table-free v0 with no modules, but does not initialize/migrate it.
Legacy platform-only backup/restore/reset refuses registry-bearing installations
and any destination with module evidence. Use a documented stopped, coherent
registry-plus-all-module recovery set; no new automated global backup product.

**Rationale**: A copied registry can point at newer/older/missing stores.
Installation/allocation tokens, complete inventory, content hashes and migration
checkpoints must agree. A coherent older whole-installation backup is an
explicit recovery-point choice, not a magically detectable violation; mixed or
stale replacement evidence is rejected. Fresh-target restore retains originals.

**Alternatives**: Silent downgrade/drop-recreate, registry-only reset and adopting
leftover module files lose ownership. Cross-provider conversion, live relocation
and auto-rollback are excluded. Forward recovery or restoring the full verified
pre-change set is explicit and must be tested.

## R9 - Dependency candidate compatibility, not adoption

**Decision**: Candidate Microsoft.EntityFrameworkCore, Relational and Sqlite
10.0.13 align with existing Microsoft.Data.Sqlite/runtime 10.0.13.
Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 declares EF/Relational
`>=10.0.4 && <11.0.0` and Npgsql `>=10.0.3`, compatible with those pins.
EF Design 10.0.13 is development-only if required. Prefer the existing SQLite
native dependency graph, not a second bundle/version acquired implicitly.

**Rationale**: These published versions avoid a floating/latest or preview
dependency. Microsoft EF packages and Npgsql are permissively licensed upstream,
but that is not full transitive/native license approval. Implementation must
review actual resolved graph, signatures/provenance/advisories, retained notices
and locked restore on supported targets before using it.

**Alternatives**: Raw SQL alone cannot demonstrate the required module-owned EF
contexts/migrations; adding another ORM or test database substitute is unnecessary.

**Sources**: [EF Core 10.0.13](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore/10.0.13),
[Relational](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Relational/10.0.13),
[Sqlite](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Sqlite/10.0.13),
[Design](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design/10.0.13),
[Npgsql EF 10.0.3](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL/10.0.3).

## Research conclusion

All planning unknowns have a concrete decision and rejected alternative.
There is no unresolved material clarification or constitutional conflict.
ADR approval, real dependency adoption and every runtime/provider/migration/
deadline/recovery assertion remain human or implementation gates, not research
results. In particular, successful upstream APIs do not prove this proposed
assignment binder or recovery procedure works.
