# Feature Specification: 003 - Storage Registry and Module Isolation

**Feature Branch**: `feature/003-storage-registry`
**Feature Directory**: `specs/003-storage-registry`
**Created**: 2026-10-21
**Status**: Draft; specify stage only, not implemented or human-accepted
**Input**: Exact feature description extracted by the driver's `prompt` command:

```text
Specify Köfte feature 003: Storage registry and module isolation. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Implement platform-owned registry and module database provisioner: dedicated PostgreSQL named schemas allocated collision-safely, and one SQLite file per module, plus separate platform database/file.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Persist mapping, unique constraints, registration concurrency handling, protected reserved names and paths; integration tests on PG and SQLite.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 002. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

**Constitution**: Adopted v2.0.1; principles I-XII are mapped below.

## Scope and Dependencies

**Purpose**: An operator can give each storage-using module a durable, exclusive
storage location, and a module author can use that location without guessing
physical names or touching another module's data. Concurrent installation,
restart and interrupted provisioning must not change ownership or lose data.

- **Inventory**: 003, required within MVP 001-024. This invocation specifies
  behavior only; a specification is not runtime proof or feature acceptance.
- **Prerequisites**: Human-accepted, merged 001 provides the module-free bootstrap,
  guarded recovery and owned real-provider verification. Human-accepted, merged
  002 provides canonical immutable module IDs, validated manifests and explicit
  relational-storage needs. Its accepted ledger and evidence are on main.
- **Owner and actors**: The platform owns registry records, physical allocation,
  provisioning and safe resolution. Operators configure installation storage and
  execute documented local maintenance. Module authors consume an assigned
  module-local storage contract and own their contexts/migrations. The trusted
  host composition layer supplies validated identity, not an untrusted user.
- **Included**: Durable module-to-storage mappings; safe concurrent registration;
  platform-only registry persistence; isolated PostgreSQL schemas and SQLite
  files; ownership-aware provisioning/resolution; registry versioning/migrations;
  minimal module context/migration integration and test-owned consumer proof;
  bounded failures, restart reconciliation and recovery guidance.
- **Excluded**: Module discovery/loading, dependency ordering and the general
  lifecycle/migration orchestrator (004); user authentication, teams and
  centralized authorization (005/006); public administration endpoints, UI and
  MCP tools (007/008/013/021); settings/secret services (015); durable event/job
  runtimes (016/017); a packaged reference module (020); production deployment
  profiles and automated installation-wide backup orchestration (022).
  Cross-provider data conversion, live storage relocation, module-ID renaming,
  automatic deletion/uninstall and arbitrary adoption of existing storage are
  not part of this slice. External MCP, Hoard and AI chat remain post-MVP.
- **Boundary**: Registry ownership is installation-wide, not a team record or a
  substitute for row/resource authorization. Trusted in-process modules and
  privileged operators are not sandboxed by schemas or files.
- **Sources**: [Charter](../../docs/product-charter.md),
  [vision](../../docs/product-vision.md), [architecture](../../docs/architecture.md),
  [inventory 003](../../docs/master-spec-inventory.md#003--storage-registry-and-module-isolation-mvp),
  [constitution](../../.specify/memory/constitution.md) and
  [accepted manifest contract](../../docs/adr/0004-module-manifest-contract.md).
  No conflicting canonical requirement was identified. A separate PostgreSQL
  platform schema satisfies platform separation; a second PostgreSQL server or
  database is not required.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Give a module stable, exclusive storage (Priority: P1)

An operator registers two storage-using modules and restarts Köfte. Each module
keeps its own storage and data, even after display-name or release-version changes.

**Why this priority**: Persistent ownership is the foundation for later module
installation, upgrades and recovery.
**Independent Test**: Register two validated module identities on each supported
provider, write distinct sentinel records through their assigned stores, restart
and resolve both mappings from persisted state.

**Acceptance Scenarios**:

1. **Given** an initialized platform registry and a valid relational-storage
   declaration, **When** the trusted host registers a module, **Then** it receives
   one persisted module-owned allocation distinct from platform storage; another
   module receives a different allocation, and no module data enters the registry.
2. **Given** existing mappings and module data, **When** the host restarts or a
   module changes its display name/release version and re-registers the same ID,
   **Then** it receives the identical stored allocation with its data intact;
   resolution does not regenerate physical names from current naming rules.
3. **Given** a validated module declaring no relational storage, **When** it is
   registered, **Then** no module schema/file is created and storage resolution
   reports no allocation, rather than falling back to the platform store.
4. **Given** a registered module and a candidate attempting to change that
   registration's identity, provider or physical location, **When** it is applied,
   **Then** the change is rejected without moving, aliasing or deleting data;
   a distinct ID is a distinct registration, not a rename.

### User Story 2 - Register safely despite races and collisions (Priority: P1)

An operator can run supported concurrent registration without duplicate ownership
or accidental reuse, and can distinguish a busy installation from a name conflict.

**Why this priority**: A name generator alone cannot protect persistent data from
collisions or independent registering processes.
**Independent Test**: Race independent callers/connections against the same real
registry, including forced physical-name collisions, and inspect all persisted
records and created storage.

**Acceptance Scenarios**:

1. **Given** simultaneous equivalent requests for one canonical module ID,
   **When** they register through independent callers, **Then** at most one
   mapping/allocation is created; callers either converge on that mapping or
   receive a bounded, explicitly retryable busy/conflict outcome, and retry
   resolves the same allocation.
2. **Given** distinct IDs with similar readable names, **When** concurrent
   registration runs, **Then** each successful registration has a distinct
   normalized allocation; neither overwrite nor shared ownership occurs.
3. **Given** distinct IDs forced to the same physical candidate, a reserved name,
   or an existing unowned schema/file, **When** registration runs, **Then** it
   fails closed with corrective guidance and leaves the existing owner/data
   unchanged; it does not silently pick an alias, adopt or delete the object.
4. **Given** duplicate-ID or duplicate-allocation writes that bypass the normal
   allocator, **When** persistence is attempted, **Then** durable uniqueness
   constraints reject them, and a corrupt/inconsistent mapping found on reopen
   is not served as a valid allocation.

### User Story 3 - Use only the assigned module boundary (Priority: P1)

A module author obtains its storage assignment through the public module-facing
contract and applies its own model and migrations without selecting a schema,
file or another module's migration history.

**Why this priority**: Correct registration is useful only if normal data and
migration paths retain the same ownership.
**Independent Test**: Two test-owned module consumers use identically named tables
and independent migration histories on both providers, then update one module.

**Acceptance Scenarios**:

1. **Given** two assigned modules with the same logical table/history names,
   **When** each applies a clean-install migration and writes/queries data through
   its assigned context, **Then** tables and migration history stay within each
   module's schema or physical file, separate from platform state.
2. **Given** existing data/history in both modules, **When** one module upgrades
   under the installation maintenance lock, **Then** only that module's model,
   data and history change, and a concurrent conflicting maintenance attempt
   cannot migrate the same storage simultaneously.
3. **Given** a module declaration/caller attempting a schema override, traversal,
   absolute path, linked path, platform target or another module's assignment,
   **When** registration or resolution checks it, **Then** it rejects the request
   before touching that target, including SQLite sidecars and linked ancestors.
4. **Given** a missing, corrupt, unowned or mismatched assigned store,
   **When** an existing ready mapping is resolved, **Then** resolution fails
   explicitly without creating a replacement empty store or falling back to
   another module/platform/provider.

### User Story 4 - Recover an interrupted installation without guessing (Priority: P1)

An operator can identify incomplete provisioning or migration, correct the cause
and retry safely rather than mistaking an allocated name for usable storage.

**Why this priority**: Registry persistence and external storage creation do not
form one universally atomic transaction.
**Independent Test**: Interrupt controlled provisioning/migration phases on each
provider, restart, inspect durable state, then execute the documented retry or
forward-recovery procedure.

**Acceptance Scenarios**:

1. **Given** interruption after reservation or physical creation but before
   readiness is recorded, **When** the host restarts, **Then** the allocation
   remains identifiable as incomplete and is not returned as ready; an
   ownership-verified retry finishes the same allocation or reports an actionable
   reconciliation failure without adopting unrelated data.
2. **Given** unavailable storage, denied provisioning privileges, an occupied
   maintenance lock or cancellation, **When** the operation runs, **Then** it ends
   within the documented operation deadline with a truthful outcome and safe
   recovery guidance; failure/cancellation is not reported as success.
3. **Given** an existing accepted bootstrap store or earlier supported registry
   version, **When** the documented registry upgrade runs with exclusive
   maintenance protection, **Then** existing platform data and mappings remain
   intact; a failed or unsupported upgrade prevents registry use and provides
   tested rollback or forward-recovery instructions.
4. **Given** backed-up registry and module stores, **When** a documented
   stopped-installation restore is exercised, **Then** original module ownership
   and sentinel data are retained; a mismatched/stale restore or platform reset
   cannot silently reassign, erase or orphan module data.

### User Story 5 - Diagnose storage problems without disclosing secrets (Priority: P2)

An operator can identify the affected module, operation and safe next action
without exposing credentials, module data or sensitive paths to users.

**Why this priority**: Useful diagnostics must not weaken the ownership boundary
or mislead operators about safe retry.
**Independent Test**: Capture results, logs and telemetry for success, contention,
collision, path rejection, provider failure and corrected retry using seeded
secrets and untrusted control characters.

**Acceptance Scenarios**:

1. **Given** successful, rejected or failed storage operations, **When**
   diagnostics are inspected, **Then** they identify a stable outcome, safe
   canonical module ID when valid, operation, correlation and retry/reconciliation
   guidance without raw credentials, connection strings, absolute private paths,
   module payloads, injected log lines or stack traces.
2. **Given** a corrected configuration after a failed attempt, **When** the
   operator follows the documented retry, **Then** the assigned module becomes
   usable without deleting its data or resetting other modules; diagnostics
   distinguish the successful retry from the previous failure.

### Edge Cases

- Accepted 002 identity limits, near-identical IDs, long IDs and display names
  containing Unicode must not become ambiguous or unsafe physical identifiers.
- Reserved PostgreSQL names include `public`, `information_schema`, `pg_*`,
  `kofte_*` and additional platform-owned names; platform provisioning is distinct
  from prohibited module allocation. Identifier length is measured in bytes.
- Candidate-name collisions must be forced in tests, not dismissed because the
  chosen hash makes accidental collisions unlikely.
- Same-ID retries with conflicting storage needs are not equivalent requests;
  changing an existing storage declaration must not implicitly remove/move data.
- Files, directories, SQLite sidecars, symlinked ancestors and hard-linked
  database files can alias or escape storage despite harmless-looking text.
  A missing ready store is different from a never-provisioned reservation.
- Parallel callers, separate registering processes, lock timeout, cancellation,
  lost connectivity and process termination can occur around every durable
  provisioning boundary. A failed response does not prove nothing was persisted.
- Existing unowned objects, stale recovery copies, future registry versions and
  inconsistent records must not trigger opportunistic adoption or destructive
  cleanup. Operator inspection does not authorize automatic data deletion.
- A zero-module host remains usable. Platform registry schema migration is not
  permission to discover, load, enable or upgrade arbitrary module code.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The platform MUST provide a minimal versioned registration and
  resolution contract over canonical identities and validated storage needs from
  002. Only trusted host composition/maintenance callers may register or inspect
  installation allocations. Raw manifest metadata MUST NOT grant authority,
  choose credentials or select physical storage.
- **FR-002**: The platform MUST persist one identity record per canonical module
  ID and at most one assigned relational location for that ID in the configured
  provider. A no-storage declaration MUST create no module store and MUST have
  an explicit no-allocation result. Conflicting repeat declarations MUST fail
  without changing existing ownership.
- **FR-003**: The registry MUST enforce durable uniqueness for canonical module
  IDs and normalized physical allocations through database UNIQUE constraints,
  not just application checks or hashes. Separate callers/processes MUST use
  locking or transactional conflict handling. Equivalent same-ID retries MUST
  converge on one mapping; conflicting IDs/candidates MUST fail closed.
- **FR-004**: Persisted identity-to-location mappings MUST remain immutable across
  retries, restart, display-name/version changes and allocator implementation
  changes. Existing mappings MUST be read, not regenerated. Implicit renaming,
  provider switching, relocation, deletion or reassignment MUST be rejected.
- **FR-005**: PostgreSQL MUST keep platform registry tables/history in a dedicated
  platform-owned schema and each relational module's tables/history in its own
  assigned named schema. Module allocations MUST be normalized, SQL-safe,
  deterministic for new IDs under the documented allocator, at most 63 bytes,
  and outside reserved/platform-owned namespaces. Collision checks MUST include
  existing database objects; hashing alone MUST NOT authorize ownership.
- **FR-006**: SQLite MUST keep platform state in a separate platform database
  file and each relational module in one exclusive physical database file under
  a restricted configured data root. Registry-controlled canonical resolution
  MUST reject traversal, path escape, unsafe links/aliases, non-regular files
  and unsafe sidecars/ancestors before access. Modules MUST NOT select file
  paths or share the platform/another module's file.
- **FR-007**: Provisioning MUST distinguish reserved/incomplete, usable and
  failed/reconciliation-required outcomes durably. A mapping MUST NOT be reported
  usable until physical storage and ownership are verified. Interruption MUST
  preserve enough evidence to retry the same allocation safely; pre-existing
  unowned or mismatched objects MUST NOT be adopted, overwritten or removed.
- **FR-008**: Resolving an existing usable mapping MUST verify the configured
  provider, registry compatibility and assigned store's presence/ownership before
  use. Missing/corrupt/inconsistent storage MUST fail explicitly, not be silently
  recreated, reallocated or replaced by an in-memory/platform/other-provider store.
- **FR-009**: A module-facing assignment MUST carry its immutable owner and
  provider-specific boundary without accepting caller-selected physical targets.
  Normal module context/migration integration MUST bind both tables and migration
  history to the assigned PostgreSQL schema or SQLite file. No shared SQLite
  database, simulated schema, `ATTACH` contract or cross-module connection/table
  access may substitute for isolation.
- **FR-010**: Registry persistence MUST have an explicit schema version,
  initialization and supported upgrade path from 001's table-free bootstrap.
  Registry and module-local migrations MUST execute under installation maintenance
  protection and preserve existing data/history. Unsupported newer versions,
  migration failures and incompatible application rollback MUST fail closed with
  documented, tested rollback or forward recovery, never automatic drop/recreate.
- **FR-011**: Registration/provisioning MUST be idempotent with respect to the
  immutable ID and compatible storage need. Lock waits, provider calls, retries
  and cancellation MUST have documented finite budgets, chosen during planning.
  Busy/retryable, rejected, incomplete and terminal failure outcomes MUST be
  distinct; callers MUST NOT be promised a transaction spanning registry state
  and separate module files or a failed operation having no durable effects.
- **FR-012**: Module data updates MUST remain module-local transactions.
  Registry state transitions MAY use their own local transactions, but no
  cross-module joins, shared connections or distributed ACID guarantee may
  become an extension contract. Recovery MUST use explicit reconciliation of
  ownership/state rather than an implied global rollback.
- **FR-013**: Configuration MUST explicitly identify the provider, platform
  store and trusted data root/connection source. Provisioning privileges and
  ordinary runtime access MUST be documented separately with least privilege;
  the existing bootstrap credential restrictions MUST NOT be silently broadened.
  Invalid configuration, inadequate privileges and provider outage MUST fail
  visibly without selecting another provider or requiring Kubernetes/cloud
  services for local SQLite operation.
- **FR-014**: Operations MUST expose structured, correlated, bounded diagnostics
  for allocation, provisioning, resolution, contention and migration/recovery.
  Mutations and failed boundary checks MUST emit operator-visible audit-relevant
  records identifying outcome and safe module/operation context. Credentials,
  connection strings, raw invalid input, module data, private absolute paths and
  exception dumps MUST NOT appear in returned diagnostics or telemetry.
- **FR-015**: Backup/restore guidance MUST include the registry, module stores,
  migration histories and their consistency relationship. A stopped-installation
  recovery MUST preserve mappings and data; mismatched ownership/version evidence
  MUST block use. Existing platform-only backup/reset commands MUST be updated or
  safely refused when they would lose registry references or invalidate module
  ownership. Neither reset nor retry may silently delete module data.
- **FR-016**: The registration/resolution contract MUST have documented examples
  for clean install, no storage, same-ID retry, collision, path rejection,
  interrupted provisioning and corrected retry, with public test-owned consumers
  on both real providers. The host MUST still run without optional modules;
  existing bootstrap health, web and pure manifest-validation behavior MUST
  remain compatible except explicitly versioned storage/maintenance changes.

### Interfaces and Permission Parity

The applicable interface is a minimal platform-owned module storage contract,
consumed by trusted composition and local maintenance, plus existing operational
health/diagnostic paths. It is not a public registry browser or upload endpoint.
Module authors receive only their assignment through host binding; a module ID
supplied by a remote user is not proof of ownership.

No new UI, REST administration route, MCP tool, background job, event bus,
provider adapter or realtime subscription is introduced. Consequently there is
no new public actor/team/permission name to invent before 005/006. Future
adapters must authorize the same underlying operation using actor, registered
permission and team/resource scope; they must not expose raw allocations or
credentials through discovery. Module data still owes its own team/resource
authorization when those operations exist. Schema/file separation alone does
not enforce that policy.

### Data Ownership and Compatibility

The platform registry owns identity, provider, immutable allocation, provisioning
state and registry format/version metadata. Modules own their data, models,
contexts and migration history. Registry records MUST NOT hold module tables or
module payloads. A release version is metadata, not a new storage owner.

PostgreSQL platform and module schemas may share a database, but never a module
schema or migration-history namespace. SQLite uses separate physical files,
including a distinct platform file. Absolute module paths and credential-bearing
connection strings are not portable registry identities or manifest fields.
Any interpretation of a stored relative locator remains confined to its trusted
installation root; root relocation is not an automatic feature.

The registry's versioned persistence and module-facing contract are distinct
from 002's manifest schema/module/host-contract versions. Unsupported formats
must be diagnosed before writes. Minimal real module migration proof is in scope
using test-owned contexts; the dependency-ordered general runner and module
enable/disable are 004, and user-driven upgrade UX is 021.

Backup/recovery evidence must cover platform-plus-module ownership, not just
successful copying of a registry file. Safe refusal is required where the
existing platform-only tool cannot preserve those relationships. Cross-provider
conversion, automatic down-migration and live multi-module snapshots are not
promised; supported forward-recovery paths must be explicit.

### Experience, Security and Operations

Operators must distinguish no registered modules, no-storage declarations,
in-progress reservation, ready storage, contention, rejection and recovery-needed
states in safe contract results and diagnostics. A finite timeout is not a
successful installation. Corrected retry must not require deleting unrelated
modules or resetting the installation.

No new visual journey is introduced, so new keyboard, responsive-layout,
loading-animation or browser accessibility targets do not apply; existing
bootstrap UI/health regressions remain in scope. Operation budgets and the
maximum documented allocation/diagnostic sizes are planning decisions and must
be published before implementation tests are finalized.

Abuse coverage includes raw SQL/path-like inputs, reserved names, forced
collisions, linked/aliased paths, duplicate identity, forged assignments and
secret-seeded failures. Schemas/files enforce normal platform ownership, not
a hostile-code sandbox when trusted modules share process credentials.
Installation maintenance must not quietly demand superuser runtime access.
Structured records provide current audit-relevant evidence; a centralized
durable audit product remains 009.

### Key Entities

- **Module identity**: Canonical immutable ID from accepted 002; not a display
  name, physical identifier, user identity or team.
- **Registry entry**: Platform-owned durable identity record, storage need,
  provider/allocation when applicable and truthful provisioning state.
- **Storage assignment**: Owner-bound resolution of one module's schema/file;
  not caller-selected configuration or authority over another module.
- **Provisioning outcome**: Correlated success, busy, rejection or incomplete/
  recovery-required state with safe retry guidance.
- **Migration history**: Versioned platform or module-local applied changes,
  physically owned by the same schema/file as the model they describe.
- **Recovery set**: Registry and corresponding module stores/history whose
  ownership/versions must remain consistent through a stopped restore.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: For two independently consumed sample modules on each supported
  storage profile, 100% of clean-install, write/read and one-module upgrade cases
  retain distinct ownership and sentinel data; zero module data rows or module
  migration-history entries appear in platform or other-module storage.
- **SC-002**: Across 20 repetitions of ten simultaneous registration attempts
  using independent callers, same-ID retries converge on exactly one allocation;
  distinct-ID success never shares an allocation. Every forced collision or
  duplicate bypass is rejected without changing existing data.
- **SC-003**: Across three full restarts and display-name/release-version changes,
  100% of existing assignments remain identical and readable. No-storage modules
  create zero module stores, and missing ready storage never becomes silent
  empty-data success.
- **SC-004**: Every defined interruption/failure point has an automated
  restart-and-recovery case that either restores the original assignment/data
  or returns an explicit actionable block; zero incomplete allocations are
  reported usable. Contention, outage and cancellation terminate within their
  published budgets on the available supported test environment.
- **SC-005**: Every reserved-target, traversal/link/alias, forged-owner and
  secret-seeded rejection case produces zero unauthorized target changes and
  zero seeded-secret/raw-private-path disclosures in captured output/telemetry.
- **SC-006**: Automated execution of the documented install, retry, isolated
  upgrade and stopped-restore examples succeeds on both supported storage
  profiles, including the supported bootstrap upgrade. All applicable existing
  foundation/manifest checks remain green, without requiring a manual timed
  usability exercise or claiming production/MVP readiness.

### Required Validation

These are future acceptance obligations, not checks performed by specification.
Tests must fail if ownership or failure semantics are violated; mocks alone do
not prove storage or migration behavior.

| Evidence | Requirement and acceptance coverage |
| --- | --- |
| Focused unit/boundary tests | FR-001-008/011/014; US1.3-4, US2.3, US3.3, US5.1; canonical identity reuse, allocation byte/reserved limits, forced collisions, conflicting declarations, safe paths, outcome/budget and redaction rules. |
| Real PostgreSQL and SQLite integration | FR-002-010/012/013; US1.1-4, US2.1-4, US3.1-4; independent connections/processes, database constraints, platform/module separation, same logical table/history names, single-module upgrade, restart and sentinel preservation. |
| Module-facing public contract | FR-001/009/016; US1.1/3, US3.1-4, US5.2; two test-owned consumers verify owner-bound assignment and consistent outcomes, not private implementation shortcuts or a runtime loader. |
| Controlled interruption/recovery | FR-007/008/010-013/015; US4.1-4; inject interruption at each durable provisioning phase and migration failure, exercise lock contention, cancellation/outage, bootstrap upgrade, unsupported version, stopped backup/restore and unsafe platform-reset refusal. |
| Abuse/privacy observation | FR-003/005/006/009/014; US2.3-4, US3.3, US5.1; force collisions and invalid persistence, attempt forbidden targets/links/aliases/forged assignments, capture all results and operational output with seeded markers. |
| Documentation and compatibility | FR-013-016; US4.2-4, US5.2; automate published examples, verify actual operation deadlines/recovery instructions, retain existing module-free host/web/health/manifest checks and update versioned contracts when intentionally changed. |

The concurrency matrix must include independent registering processes as well as
callers within one process, with all outputs and final persisted ownership
asserted. Repetition counts do not replace deterministic forced-collision and
crash-phase coverage. Record the actual source, pinned toolchain, OS/architecture
and available machine/runtime conditions; no dedicated fixed CPU/RAM benchmark
host or human elapsed-time attestation is required.

.NET test source must physically belong to its dedicated test project under
`tests/`, using normal inclusion, never production slices or linked production
source. Test-only modules/contexts prove this boundary without adding a
first-party feature module or preempting reference module 020.

New REST/event/MCP contract suites, denied tool/team tests, Playwright journeys,
durable job restart tests, Kubernetes deployment smoke and the packaged
reference-module proof are inapplicable to this internal storage slice because
those surfaces/runtimes are not introduced. Existing regression suites remain
applicable; later numbered features and release gate 024 still owe their full
interface parity, authorization, deployment and end-to-end evidence.

## Assumptions and Constitutional Review

**Assumptions**: One installation uses one explicitly selected provider at a time.
Canonical identity/manifest validation comes from accepted 002, not a new parser.
Trusted local composition is the only registration authority until protected
administration surfaces exist. Tests may supply inert module contexts/migrations
without loading module code. No-storage registration does not promise a later
storage-need conversion workflow; conflicting changes fail safely.

**Locked versus undecided**: Persistent immutable mapping, unique constraints,
concurrent conflict handling, byte-safe reserved-name protection, canonical
per-file isolation and honest recovery are binding. Allocator/hash/length
choices, exact registry fields/status names, versioned contract shape, budgets,
maintenance-lock mechanism, provider-specific migration strategy, provisioning
credential separation and recovery-set verification belong to planning and
consequential ADRs. Existing bootstrap PostgreSQL credentials intentionally lack
database CREATE privilege: planning must reconcile schema provisioning with that
baseline explicitly, not silently broaden credentials or remove fixture safety.
No material requirements question or canonical conflict is left unresolved by
this draft; the separate clarify stage still must assess it.

| Principle | Applicability and requirement evidence |
| --- | --- |
| I - Product/brand | Portable optional-module foundation; Köfte prose; FR-013/016 preserve zero-module operation and no Hoard dependency. |
| II - Modularity | 002 identity/declarations feed a minimal storage contract (FR-001/009/016); general lifecycle/loading remains 004 and no sandbox/hot-unload promise is added. |
| III - Vertical slices | Platform-only shared registry concern; test source remains in dedicated projects; no mediator/mapping framework or speculative application/module scaffold is authorized. |
| IV - Storage | FR-002-010 enforce immutable mappings, real UNIQUE/concurrency protection, PostgreSQL schema/history separation and SQLite physical-file/path boundaries. |
| V - Transactions | FR-007/011/012 and US4 require truthful partial-state reconciliation, module-local transactions and no distributed ACID/shared-access contract. |
| VI - Safety | FR-001/006/009/013/014 constrain authority, ownership, credentials and diagnostics; future user/team/resource authorization is not replaced by file/schema isolation. |
| VII - MCP | No tool/server surface is needed here; 007/013 must consume shared authorized operations and must not expose storage as a bypass. |
| VIII - Durable work | Durable allocation recovery is FR-007/011; resources/jobs/events/realtime remain later features, not newly invented runtimes. |
| IX - Frontend | No new visual journey; existing bootstrap UI is preserved by FR-016. No arbitrary hot-loaded frontend or new accessibility claim is introduced. |
| X - Portability | FR-010/013-016 cover both providers, explicit configuration, privileges, upgrades, recovery and operational output without mandatory Kubernetes/cloud services. |
| XI - Evidence | SC-001-006 and Required Validation require real providers, constraints/concurrency, migration, crash/restart, abuse, privacy and actual public-consumer proof; later release evidence is not waived. |
| XII - Scope | Exact 003 input, dependencies, ownership, exclusions and traceability above; only specify is executed, with no implementation, acceptance or post-MVP expansion. |
