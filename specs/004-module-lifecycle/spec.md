# Feature Specification: 004 - Module Composition and Lifecycle

**Feature Branch**: `feature/004-module-lifecycle`
**Feature Directory**: `specs/004-module-lifecycle`
**Created**: 2026-10-22
**Status**: Draft; specify stage only, not implemented or human-accepted
**Input**: Exact feature description extracted by the driver's `prompt` command:

```text
Specify Köfte feature 004: Module composition and lifecycle. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Support trusted in-process .NET module discovery, dependency ordering/cycle detection, registration, configuration, migrations, enable/disable and health/degraded states.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: No optional module boot, test module boots, invalid/duplicate modules fail closed; startup diagnostics; disabling preserves data.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 002,003. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

**Constitution**: Adopted v2.0.1; principles I-XII are mapped below.

## Scope and Dependencies

**Purpose**: An operator can run Köfte without optional modules, deliberately
enable a trusted deployed module and its prerequisites, and see why a composition
is ready, disabled, degraded or blocked. A module author can participate through
public contracts without modifying platform internals. Disabling or recovering
a module must not discard its data or manufacture successful startup.

- **Inventory**: 004, required within MVP 001-024. This invocation specifies
  requirements only, not a runtime, implementation plan or accepted feature.
- **Prerequisites**: Accepted, merged 001 supplies the module-free host and
  development/verification baseline. Accepted, merged 002 supplies canonical
  identity, strict manifests and version/range semantics. Accepted, merged 003
  supplies immutable storage assignments, assigned migration boundaries,
  explicit maintenance authority, bounded supervision and recovery.
  PR #6's accepted ledger and all five acceptance hashes were verified on main.
- **Owners and actors**: The platform owns composition decisions, lifecycle
  coordination and installation-level observations. A trusted installation
  operator selects deployed modules and supplies their configuration. Trusted
  module authors own module behavior, settings validation, health reporting,
  data and migrations. Application users receive no installation authority
  merely by belonging to a team or knowing a module ID.
- **Included**: Trusted deployed-module discovery; explicit enable/disable
  intent; manifest and deployed-set compatibility; dependency order/cycle
  checks; configuration and contribution validation; registration,
  initialization, assigned migrations and activation; health/degraded/failure
  observations; safe stopped/restarted transitions and test-owned module proof.
- **Excluded**: Downloading/installing arbitrary packages, a marketplace or
  trust approval service; hostile-code isolation or hot-unload; public lifecycle
  administration routes/tools/pages; user authentication and centralized
  team/resource authorization (005/006); operational MCP and UI SDKs (007/014);
  the durable audit product (009), settings/secret service (015), event/job/
  workflow/realtime runtimes (016-018), full reference module (020),
  installation/upgrade UX (021) and production deployment profiles (022).
  Module-ID renaming, data deletion/uninstall, live relocation, cross-provider
  conversion and automatic cross-module rollback are not introduced.
  External MCP federation, Hoard and AI chat remain post-MVP.
- **Transition boundary**: Installation configuration determines desired
  enablement. A controlled stop/restart is sufficient to apply changes;
  live reconfiguration, automatic dependency installation and implicit
  cascading disable are not promised. These scope assumptions are explicit
  below rather than an implied hot-loading design.
- **Sources**: [Charter](../../docs/product-charter.md),
  [vision](../../docs/product-vision.md),
  [architecture](../../docs/architecture.md#5-module-model-and-lifecycle),
  [inventory 004](../../docs/master-spec-inventory.md#004--module-composition-and-lifecycle-mvp),
  [constitution](../../.specify/memory/constitution.md),
  [accepted manifest contract](../002-platform-contracts/contracts/validation.md)
  and [accepted storage scope](../003-storage-registry/acceptance.md).
  Architecture's illustrative YAML is not a replacement for the accepted
  strict manifest. No conflicting canonical requirement was identified.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Start only the intended trusted modules (Priority: P1)

An operator starts an empty installation or selects trusted deployed modules.
Only the selected valid composition becomes available; an absent or disabled
module is never silently substituted with apparent success.

**Why this priority**: A small host and deliberate activation are the product's
foundation, before any particular optional capability.
**Independent Test**: Start the unchanged empty profile and a test-owned
public-contract module with a prerequisite; independently observe selected
identities, lifecycle completion and module-owned behavior.

**Acceptance Scenarios**:

1. **Given** no optional module is enabled, **When** Köfte starts, **Then**
   the existing core health/status behavior remains usable, the composition
   reports zero active optional modules and no module store is allocated.
2. **Given** a trusted deployed test module and compatible enabled prerequisite,
   **When** the operator starts that selection, **Then** both activate in
   dependency order and the author's independently consumed public contract
   proves the test module's behavior without modifying platform internals.
3. **Given** a deployed module is explicitly disabled, **When** discovery and
   startup occur, **Then** its disabled intent is observable to the operator,
   its executable lifecycle callbacks/contributions are not activated and its
   storage is not provisioned or migrated merely because it was discovered.
4. **Given** an enabled identity has no trusted deployed implementation,
   **When** startup validates the selection, **Then** it reports that identity
   as missing, does not fall back to an empty composition and is not ready.

### User Story 2 - Reject an invalid composition before activation (Priority: P1)

An operator receives a bounded explanation of invalid identity, dependencies,
configuration or contribution ownership before the platform starts a partial
or ambiguous enabled set.

**Why this priority**: Ordering alone is insufficient if conflicting modules
can publish behavior or cause storage changes before validation.
**Independent Test**: Submit independently specified valid and invalid graph
fixtures in different discovery orders; compare expected decisions, ordering,
diagnostics and absence of platform-invoked lifecycle/storage side effects.

**Acceptance Scenarios**:

1. **Given** malformed or incompatible manifests, or two deployed definitions
   with the same immutable ID, **When** preflight runs, **Then** it rejects the
   composition with safe identity/reason evidence and invokes no registration,
   initialization, migration or activation callbacks.
2. **Given** an enabled module requires a missing, disabled or incompatible
   dependency, **When** the deployed set is resolved, **Then** it identifies
   the dependent and unsatisfied dependency/range and does not silently enable,
   install or substitute another module version.
3. **Given** a self-cycle or multi-module dependency cycle, **When** the same
   graph is evaluated in different discovery orders, **Then** it is rejected
   with a stable bounded cycle explanation and no affected module activates.
4. **Given** invalid module configuration or conflicting contribution
   identities, **When** preflight validates them, **Then** it rejects each
   supported negative case without publishing the conflict, returning settings
   values or mutating module storage.

### User Story 3 - Coordinate safe initialization and migrations (Priority: P1)

An operator starts a valid composition without hand-ordering its modules or
bypassing the accepted storage maintenance boundary.

**Why this priority**: Module readiness must reflect real initialization and
owner-local migration completion, not just successful discovery.
**Independent Test**: Observe lifecycle traces and real owner-local histories
for dependent test modules on both supported storage profiles, then inject
failure and contention at meaningful durable boundaries.

**Acceptance Scenarios**:

1. **Given** a valid dependency chain with pending owner-local migrations,
   **When** admitted maintenance and startup run, **Then** prerequisites
   migrate before dependents, each stage's prerequisites complete before that
   stage, and no module is advertised enabled/ready before required work ends.
2. **Given** an enabled module declares no relational storage, **When**
   composition runs, **Then** its non-storage behavior can activate without
   allocating a module store, fabricating an assignment or executing module
   database migrations.
3. **Given** a migration fails after an earlier module committed work,
   **When** startup stops, **Then** dependent activation is blocked, the failed
   stage and reconciliation guidance are reported, committed owner-local
   history/data remain intact and no cross-module rollback is claimed.
4. **Given** another process holds incompatible installation authority,
   **When** a composition requests maintenance, **Then** existing bounded
   contention/ownership rules apply and no second unsupervised migration or
   loss-of-lease continuation occurs.

### User Story 4 - Disable and re-enable without losing data (Priority: P1)

An operator changes a valid module selection through a controlled restart and
can later restore the module with its same identity, data and migration history.

**Why this priority**: Enablement is operational availability, not permission
to delete or rename a module's owned resources.
**Independent Test**: Write distinguishable data through two test modules;
disable/restart/re-enable one and compare exact assignments, histories and
both modules' data across three complete cycles.

**Acceptance Scenarios**:

1. **Given** a module with persisted data and no enabled dependents,
   **When** the operator disables it and restarts, **Then** its contributions
   are unavailable while its exact storage assignment, data and history remain.
2. **Given** that disabled module is re-enabled with compatible metadata,
   **When** it starts again, **Then** it uses the persisted assignment/data,
   does not replay already applied migrations and is not treated as a new owner.
3. **Given** a still-enabled module requires the module being disabled,
   **When** the proposed selection is evaluated, **Then** it rejects the
   invalid selection before activation, preserves existing durable data and
   requires an explicit coherent correction rather than cascading silently.
4. **Given** an active dependency graph, **When** the owned host is stopped,
   **Then** started lifecycle resources are stopped/disposed in
   dependency-safe reverse order within documented bounds, without deleting
   stores or treating the previous observed health as current after restart.

### User Story 5 - Diagnose health, failure and a corrected retry (Priority: P2)

An operator distinguishes requested enablement from actual availability and
receives safe evidence sufficient to correct a blocked/degraded composition.

**Why this priority**: Clear failure and retry evidence makes composition
operable without implying that trusted plugins are isolated or always stoppable.
**Independent Test**: Inject configuration, initialization, migration and health
failures, cancellation and timeout; capture private records and public aggregate
status, correct the cause and repeat through a new correlated attempt.

**Acceptance Scenarios**:

1. **Given** startup work is incomplete, **When** status is observed, **Then**
   it distinguishes desired selection, current stage and unverified health;
   aggregate readiness does not report successful composition prematurely.
2. **Given** a module reports degraded or failed/unavailable health after
   activation, **When** observations are refreshed, **Then** the operator
   can distinguish these states, affected dependents are not falsely healthy
   and independent module observations remain identifiable.
3. **Given** a failed, timed-out or cancelled attempt, **When** the operator
   corrects its cause and restarts, **Then** a new correlated attempt succeeds
   only after ownership/recovery verification; uncertain prior work stays
   blocked until explicitly reconciled rather than retried blindly.
4. **Given** seeded secrets/private paths in settings or a callback failure,
   **When** startup/health diagnostics and aggregate status are captured,
   **Then** safe stage/outcome/correlation/guidance remain observable without
   raw settings, credentials, exception text, paths or detailed anonymous
   module inventory, and no unauthenticated lifecycle-control surface exists.

### Edge Cases

- Empty selection, all deployed modules disabled, missing enabled implementation,
  duplicate IDs despite different display names/versions, and identity changes.
- Unmet version ranges, prerelease/build comparisons, disabled prerequisites,
  direct/indirect cycles, diamond dependencies and independent roots.
- Collision between modules or reserved host contributions; the same local
  permission name under two different owners is not itself a collision.
- Unknown/unsupported executable contribution kind, mismatch between declared
  capabilities and actual contributions, invalid settings and missing secrets.
- A discovery failure, registration exception, partially initialized graph,
  interrupted owner-local migration, lost maintenance authority and cancellation.
- Restart following failure, stale status from a previous attempt, unhealthy
  prerequisites, an uncooperative trusted callback and cleanup failure.
- Missing/corrupt/foreign storage, changed display metadata and attempted
  no-storage-to-relational changes; accepted 003 refusals remain authoritative.
- Redaction failure or inability to persist required diagnostics, without
  converting a failed composition to success or deleting recovery evidence.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Köfte MUST boot with no enabled optional modules and preserve its
  existing core readiness/status and storage behavior; discovering a disabled
  module MUST NOT activate its behavior or create/migrate its store.
- **FR-002**: The platform MUST discover only explicitly trusted deployed
  implementations and associate each with exactly one validated immutable
  module identity. Missing enabled implementations, duplicate identities and
  invalid/unsupported manifests MUST reject the configured composition rather
  than be skipped or replaced with a successful empty set.
- **FR-003**: Enabled dependencies MUST be explicitly available, enabled and
  compatible under accepted 002 version/range rules. Resolution MUST detect
  direct/indirect cycles, never download dependencies, silently enable disabled
  ones or choose a different version than the deployed selection.
- **FR-004**: Equivalent deployed selections and configuration MUST produce the
  same dependency-valid order and ordered bounded validation diagnostics
  independently of discovery order. The public contract MUST state a stable
  tie rule, input/work/diagnostic bounds and explicit overflow rejection.
- **FR-005**: Manifest, selected-set, configuration, contribution and existing
  storage-mapping preflight MUST finish before platform-invoked registration,
  initialization, provisioning/migration or contribution publication. Invalid
  preflight MUST leave module stores/registry ownership unchanged. This is a
  platform invocation guarantee, not prevention of arbitrary trusted-code effects.
- **FR-006**: Contribution validation MUST verify owner, kind, supported contract
  and stable identity, rejecting conflicts with host-reserved or another owner's
  contribution before publication. Routes, permission definitions, resource
  kinds and tool identities MUST be collision-checked when contributed.
  Namespaced identities and accepted permission ownership MUST be preserved;
  metadata categories alone MUST NOT grant authority or invent later runtimes.
  An unsupported executable contribution MUST be rejected explicitly.
- **FR-007**: Each enabled module MUST receive only its explicitly scoped
  configuration through its declared validation boundary. Invalid or missing
  required settings MUST block startup with safe field/reason guidance; raw
  values and secrets MUST NOT appear in observations. No general settings/
  secret-management product is required by this contract.
- **FR-008**: The platform MUST expose ordered lifecycle progress covering
  discovery, validation, registration, initialization, migration and enablement,
  with explicit disabled, blocked, failed and health/degraded observations.
  Prerequisites MUST satisfy the required stage before a dependent can proceed;
  a stage MUST NOT be represented as successful before it actually completes.
- **FR-009**: Storage-using modules MUST obtain verified owner-only assignments
  and migrations through accepted 003 contracts and maintenance authority.
  Canonical IDs, persisted mappings, actual ownership verification and
  schema-local/per-file history MUST be preserved. No-storage modules MUST NOT
  receive fabricated assignments or have database migrations invoked.
- **FR-010**: Module migrations MUST run in dependency order under existing
  exclusive admitted maintenance/supervision rules, not privileged ordinary
  serving. Concurrent attempts, lost authority, incomplete history and physical
  inconsistencies MUST fail closed using 003's retry/reconciliation semantics;
  applied owner-local migrations MUST NOT be replayed on an ordinary restart.
- **FR-011**: Only a completely preflight-valid composition whose required
  initialization/migrations/startup checks completed may become ready.
  A failed startup MUST NOT expose a partially ready configured module set or
  silently degrade to zero modules. Already committed module data remain real
  even when the composition is not ready; readiness is not a cross-module
  transaction or compensation guarantee.
- **FR-012**: Enable/disable intent MUST be explicit installation configuration
  that survives restart and remains separate from observed state. Applying a
  coherent disabled selection through controlled restart MUST withdraw that
  module's active contributions without deleting or reallocating its identity,
  data, migration history or storage mapping.
- **FR-013**: A proposed selection that leaves an enabled module with a disabled
  prerequisite MUST be rejected before activation. Re-enablement MUST revalidate
  current configuration/compatibility/ownership and reuse preserved stores;
  no implicit cascading disable, identity remapping or automatic repair is allowed.
- **FR-014**: Module observations MUST distinguish enabled/healthy, degraded,
  failed/unavailable and unknown/in-progress health, identify affected dependency
  relationships and not claim that failed dependents are healthy. Aggregate
  availability and detailed diagnostic health MUST be distinguishable; observations
  MUST identify their current attempt/freshness rather than reuse stale success.
- **FR-015**: On failure or stop, the platform MUST clean up only runtime
  resources started by that attempt in dependency-safe reverse order, preserve
  durable state and report incomplete cleanup. Corrected retries MUST use a new
  attempt/correlation and verify or explicitly reconcile uncertain prior effects;
  they MUST NOT rerun irreversible work blindly or claim automatic global rollback.
- **FR-016**: Discovery/evaluation, lifecycle work, health observation and cleanup
  MUST have documented finite input/work/time bounds and cancellation/failure
  semantics. Startup must fit the existing host startup envelope; 003 acquisition,
  resolution and maintenance deadlines MUST NOT be weakened. A timeout MUST NOT
  claim an uncooperative callback stopped without observable termination evidence.
- **FR-017**: Composition mutations, lifecycle transitions and rejected boundaries
  MUST emit bounded operator-visible structured records with safe module/stage,
  outcome, attempt correlation and actionable correction/retry/reconciliation
  guidance. Counts/timings MUST use bounded metric dimensions. Settings, secret
  values, physical paths, raw module exceptions and high-cardinality identities
  MUST NOT become telemetry labels or unsanitized output. These are observable
  audit-relevant records, not the future durable audit service.
- **FR-018**: Lifecycle authority MUST remain installation-operator/trusted-host
  authority, not a manifest capability, module permission declaration or team
  membership grant. Detailed inventory/state MUST remain in the trusted operator
  channel; existing anonymous health/status MUST expose only safe aggregate
  information. No new unauthenticated lifecycle-control endpoint is allowed.
- **FR-019**: A module author MUST be able to supply a test-owned deployed module,
  settings validation, owner-local behavior and lifecycle/health observations
  through versioned public contracts without depending on platform internals.
  Declarations of future level-3 categories MUST remain distinguishable from
  actually executable host support; all accepted 002 metadata semantics remain.
- **FR-020**: The delivered guide and automated evidence MUST demonstrate empty
  boot, valid module/dependency boot, each invalid composition category,
  both-provider migration/disable/re-enable preservation, safe failure/correction
  and health diagnostics, retaining accepted 001-003 regression gates. A test
  module is not the full reference module or MVP release evidence.

### Interfaces and Permission Parity

The applicable interfaces are the trusted installation's deployment/configuration
entry point, the public module-author contract, owner-local storage/migration
contracts and operator-visible lifecycle observations. Settings are consumed,
not administered as a new settings product. Discovery metadata cannot itself
execute a lifecycle operation, select storage paths or confer operator authority.

No new user-facing REST administration, lifecycle UI, MCP tool, job, provider or
realtime surface is introduced. Authentication/authorization is not yet available
for such administration; exposing it anonymously would be a bypass, not progress.
Future interfaces must reuse these operations and enforce the same actor,
registered permission and team/resource policy, not parallel lifecycle logic.
Module registration does not authorize later business operations. The existing
public bootstrap status remains minimal; detailed module inventory/settings and
failure context belong only to the trusted operator channel.

### Data Ownership and Compatibility

Desired enablement/configuration and observed lifecycle attempts are
platform-owned installation concerns, not team-owned business data. The
authoritative configuration survives restart; observed success must be
re-established. No new physical settings/state format is selected here.
Module business records and migrations remain owned by their immutable module ID.

PostgreSQL retains a separate platform schema and one assigned named schema,
context and migration history per storage-using module, including 63-byte safe
identifiers, reserved namespaces and actual UNIQUE/concurrency protection.
SQLite retains a separate platform file and one private physical file per module
under validated canonical paths; traversal/aliasing, shared module files and
`ATTACH` ownership substitutes remain prohibited. An unrelated dependency's store
is not exposed as a module connection merely because the graph contains an edge.

No cross-module table access or atomic transaction is promised. Earlier
owner-local migration commits survive a later failure. Explicit 003 recovery and
stopped complete-set backup/restore boundaries remain; disabling is neither
uninstall nor deletion. Unsupported contract versions, declarations or persisted
ownership changes must fail explicitly, never normalize into another identity.
Module distribution formats, exact hook signatures, state persistence format,
contribution catalog representation and bounded scheduling are planning decisions,
not new manifest grammar or permission-string encoding in this specification.

### Experience, Security and Operations

Operators must distinguish an empty valid composition, disabled module, in-progress
stage, blocked preflight, failed startup and degraded runtime health without
reading raw exceptions. Each failure identifies a safe corrective action and
whether inspection/reconciliation is required before retry. A missing signal is
unknown, not healthy. Record attempt boundaries and preserve partial evidence.

Trusted in-process code is not sandboxed; the host cannot promise safe hot-unload
or protection from an administrator-approved malicious module. No network package
fetch, arbitrary remote frontend code, mandatory cloud, Kubernetes or IaC service
is required for local composition. Existing OIDC/authorization/MCP/UI/jobs and
production deployment obligations remain assigned to their inventory features.
No new visual journey means no new visual accessibility/performance claim; retain
the bootstrap's tested loading/error/status/accessibility behavior.

### Key Entities *(if applicable)*

- **Deployed module definition**: Trusted implementation plus accepted immutable
  descriptor; identity/version/capabilities are not execution authority.
- **Desired module selection**: Installation-owned enabled IDs and scoped settings,
  authoritative across restart, separate from observed state.
- **Resolved composition**: Validated enabled set, dependency edges and stable
  execution order, or bounded rejection evidence.
- **Contribution identity**: Owner, category, stable name and supported contract;
  colliding publication is rejected without redefining accepted namespaces.
- **Lifecycle attempt**: One correlated startup/stop/retry with per-module stage,
  outcome and cleanup/reconciliation evidence.
- **Module health observation**: Current-attempt health/availability, safe reason
  and freshness; desired enablement is not proof of health.
- **Assigned module store**: Existing 003 owner-bound mapping/history/data,
  preserved on disable, retry and failed composition.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The empty profile and an independently authored module with a
  prerequisite both reach their documented usable state without platform-internal
  edits; the disabled-module case activates zero callbacks/contributions/stores.
- **SC-002**: Each graph fixture produces its independently specified valid order
  or ordered rejection in 100 repeated evaluations across ten discovery-order
  permutations; no invalid fixture publishes a contribution or invokes a
  platform-controlled mutating lifecycle stage.
- **SC-003**: Across three complete disable/restart/re-enable cycles in each
  supported storage profile, every original assignment, sentinel record and
  already-applied history remains unchanged except an explicitly requested
  owner-local upgrade; unrelated module data changes zero times.
- **SC-004**: Every injected failure, cancellation, timeout and cleanup-refusal case
  reports its correct stage/outcome within the documented total budget, with
  zero falsely ready compositions, unreported uncertain effects or falsely
  reported terminated workers.
- **SC-005**: All documented correction/retry examples succeed after only the
  named correction, through a new correlation, without deleting preserved
  ownership/data or requiring a hidden platform repair; degraded/unavailable
  observations identify the affected module/dependents.
- **SC-006**: All captured operator, aggregate-status and telemetry channels in
  the negative corpus expose zero seeded secrets/raw settings/private paths or
  raw callback exceptions while retaining each expected safe diagnostic;
  existing foundation acceptance cases continue to pass.

### Required Validation

The evidence below is required at implementation/review, not claimed by this
specification. Dedicated .NET test source must live in its own test project under
`tests/`, never in or linked from production slices.

| Acceptance coverage | Required evidence |
| --- | --- |
| US1.1-4; FR-001/002/019; SC-001 | Real owned host empty/valid/disabled/missing-selection startup; separately compiled test-owned module consuming public contracts; meaningful module-owned behavior and absence-of-callback/store assertions. |
| US2.1-4; FR-003-007; SC-002 | Independent expected graph/diagnostic corpus: chain/diamond/disconnected roots, malformed/duplicate IDs, missing/disabled/out-of-range/prerelease dependencies, cycles, settings failures and contribution collisions/unsupported kinds. Count platform-invoked effects and prove zero preflight mutation; test documented bounds and one-over rejection. |
| US3.1-4; FR-008-011/016; SC-004 | Actual SQLite and PostgreSQL lifecycle/migration integration, owner-local histories/sentinels, dependency-order observations, no-storage negatives, concurrent process maintenance contention and lost-authority/interruption/refusal evidence. Retain 003's real constraints, model/path/ownership and bounded supervision tests. |
| US4.1-4; FR-012/013/015; SC-003 | Both-provider controlled process restart/disable/re-enable cycles, exact persisted mappings/history/data and unrelated-store comparisons, dependency guard and reverse owned-resource cleanup observations. No mere positive count substitutes for retained data. |
| US5.1-4; FR-014-018; SC-004-006 | Injected configuration/initialization/migration/health/cancellation/timeout/cleanup faults, corrected retry, current-attempt status and dependency impact; actual record/span/event/metric/output captures with seeded privacy canaries and meaningful safe-record assertions. |
| FR-019/020; SC-001/005/006 | Public author/host consumer contract tests, executable examples and retained 001-003 quality/unit/contract/real-provider/recovery/privacy/browser/startup/failure-propagation gates with exact outcomes and environment. |

No new user-facing HTTP/MCP/job/event/realtime operation exists to test for
interface authorization parity here. Prove absence of an unauthenticated
lifecycle mutation surface and preserve existing aggregate HTTP contracts;
actual actor/team leakage, denied tool calls, event delivery and restart-safe
jobs remain mandatory when 005-007/013/016-018 introduce those operations.
Retain existing Playwright status/accessibility journeys; a new lifecycle UI
test suite is inapplicable without a new lifecycle page. Production deployment
smoke and the full separately packaged level-3 reference module remain 020/022/
024 release gates, not waived or falsely completed by a test-owned module.

## Assumptions and Constitutional Review

**Locked intent**: Canonical module IDs and accepted manifest/range semantics;
trusted in-process modules; dependency/collision/configuration preflight;
immutable module-owned storage and honest transaction/recovery boundaries;
data-preserving disable; explicit failure/health; portable module-free host;
one recorded stage and explicit later human acceptance.

**Explicit scope assumptions**: Operators select trusted already-deployed code
and apply enable/disable changes through controlled restart. All declared
dependencies are required, as the accepted manifest has no optional-dependency
flag. The platform rejects a newly invalid configured composition rather than
silently serving a subset; later independent runtime degradation remains
observable. There is no implicit installation or cascading disable.

**Planning choices, not adopted implementation**: Discovery/packaging mechanism,
versioned lifecycle/contribution signatures, configuration/state representation,
deterministic tie rule, finite admitted graph/diagnostic budgets, per-stage
deadlines within existing total envelopes, health freshness policy and exact
cleanup supervision. Planning must document these choices and material tradeoffs
in an ADR without weakening locked requirements or extending later-feature scope.
No material requirements question requires an invented human answer at this stage.

| Principle | Requirement mapping or explicit applicability |
| --- | --- |
| I | FR-001/020 and scope preserve module-free portable Köfte and no Hoard dependency. |
| II | FR-002-008/011-014/019 provide stable versioned composition, collisions, lifecycle and data-preserving disable; trust is explicit, not sandboxing/hot-unload. |
| III | FR-019/020 require minimal public author contracts and independent test ownership; native DI/direct slices, reviewed dependencies and no MediatR/AutoMapper remain binding at plan/implementation. |
| IV | FR-005/009/010/012/013 and data ownership retain actual provider isolation, immutable mappings, safe names/paths and local histories with real integration evidence. |
| V | FR-010/011/015 preserve committed local effects and explicit reconciliation, not shared connections or cross-module ACID/global rollback. |
| VI | FR-006/007/017/018 validate authority/settings/collisions and private diagnostics. No installation grant from metadata/team membership; later actor/team/resource enforcement is not bypassed by a new public route. |
| VII | Future MCP contribution identity/compatibility is reserved and collision-checked when contributed; FR-006/018/019 do not implement or defer the 007/013 authenticated MCP operations. |
| VIII | Lifecycle observations are not domain resources, durable jobs/events/workflows or SignalR; those future contracts retain persistence, replay and permission obligations. |
| IX | No new visual surface; retained bootstrap UX/accessibility applies, with future trusted frontend registration and no arbitrary remote JavaScript execution. |
| X | FR-001/007/016-020 require reproducible local operation, honest budgets, configuration/privacy/recovery and diagnostics, without mandatory cloud/Kubernetes services. |
| XI | Required Validation maps every scenario and outcome to real provider/public/process/privacy and retained foundation evidence; no unrun check is a pass. Later reference-module/deployment gates remain explicit. |
| XII | 004 depends on accepted merged 002/003, remains inside MVP 001-024 and does not implement later feature runtimes or accept itself. Clarification, planning, quality review and subsequent gates remain separate. |
