# Feature Specification: 001 - Repository Bootstrap and Developer Experience

**Feature Branch**: `feature/001-specify-repository-bootstrap`
**Feature Directory**: `specs/001-repository-bootstrap`
**Created**: 2026-10-20
**Status**: Draft specification; not implemented or accepted
**Constitution**: Product principles from v1.0.0; v2.0.0's human-directed workflow correction applies (see [ADR 0001](../../docs/adr/0001-feature-delivery-lifecycle.md))
**Input**: Exact feature description extracted by the driver's `prompt` command:

```text
Specify Köfte feature 001: Repository bootstrap and developer experience. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Set up the .NET modular host, Next.js TypeScript web app, shared packages, tests, CI, reproducible local stack, linting, formatting, OpenAPI and health endpoint. Document prerequisites and single-command local start.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Working API/UI skeleton, SQLite local run, PostgreSQL test profile, green automated build and smoke tests.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: None. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

## Scope and Dependencies

**Purpose**: A contributor can turn a clean checkout into a working, verifiable
Köfte development environment without assembling an undocumented stack. A
maintainer can distinguish a healthy foundation from a merely successful build.

- **Inventory**: 001, required within MVP 001-024. This specification defines the
  working foundation, not MVP completion or permission to implement later specs.
- **Prerequisites**: No numbered feature dependencies. Existing repository setup,
  adopted constitution, contributor workflow and canonical documents are inputs.
  Documented local tools and network access for initial dependency acquisition
  are environmental prerequisites, not mandatory hosted product services.
- **Owner and actors**: The platform owns the host, web skeleton, development
  profiles and verification workflow. Primary actors are a new contributor
  starting locally and a maintainer reviewing changes; a platform operator
  evaluates diagnostics, persistence and recovery. Module authors need a
  foundation that leaves the documented extension boundaries intact.
- **Included**: Runnable host and web skeleton; minimal genuinely shared
  packages; reproducible local configuration and startup; local SQLite and a
  PostgreSQL verification profile; health/readiness and OpenAPI; focused tests,
  lint/format/type/build checks, CI, prerequisite and recovery documentation.
- **Excluded**: Module manifests/SDK and lifecycle (002/004), storage allocation
  registry and module migrations (003), login/bootstrap administration (005),
  team/permission management (006), MCP server/tools (007), full navigation/design
  system (008), durable audit/observability platform (009), domain API conventions
  beyond bootstrap endpoints (010), catalogs/resources, jobs, providers, realtime,
  reference module, frontend extensions and production packaging (later MVP specs,
  including 022). External MCP federation, Hoard and embedded AI chat remain
  post-MVP. No dummy domain feature is required just to demonstrate startup.
- **Security boundary**: This is a local development/evaluation skeleton, not a
  production-ready or authenticated multi-team service. It contains no user,
  team or resource records or business mutations. Public diagnostic responses
  contain only the non-sensitive bootstrap information described below.
- **Sources**: [Charter](../../docs/product-charter.md),
  [vision](../../docs/product-vision.md), [architecture](../../docs/architecture.md),
  [inventory 001](../../docs/master-spec-inventory.md#001--repository-bootstrap-and-developer-experience-mvp)
  and [constitution](../../.specify/memory/constitution.md). Binding platform
  constraints are retained; proposed libraries and orchestration are not selected
  here. No conflicting canonical requirement was identified.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Start a working local foundation (Priority: P1)

A new contributor follows the repository guide and starts both the web console
and host through one documented command. They see Köfte and a real connection
status rather than a static success page, with no optional modules or cloud account.

**Why this priority**: Every later feature depends on a usable local feedback loop.
**Independent Test**: On a clean supported environment with prerequisites installed,
follow only the guide, start the local profile and inspect the page and diagnostics.

**Acceptance Scenarios**:

1. **Given** a clean checkout, documented prerequisites and example configuration,
   **When** the contributor runs the documented local start command, **Then** the
   host and web console start, the page identifies Köfte and its bootstrap-only
   scope, and it reports the host's actual readiness.
2. **Given** no optional modules, external database, cloud account or Kubernetes,
   **When** the local profile starts, **Then** it uses its own persistent local
   database file and all bootstrap smoke checks pass.
3. **Given** a running local profile and existing platform test data,
   **When** it is stopped and restarted without a reset, **Then** it becomes ready
   and the data remains readable; startup does not recreate or discard it.
4. **Given** a missing prerequisite or conflicting port, **When** startup is
   attempted, **Then** it reports the failed prerequisite/component and corrective
   action, does not claim readiness, and a documented retry succeeds once fixed.

### User Story 2 - Verify a change before review (Priority: P1)

A contributor runs the documented checks locally; a maintainer sees the same
required outcomes in CI, including actual startup and database-backed checks.

**Why this priority**: A passing compilation alone cannot prove a working skeleton.
**Independent Test**: Run the complete documented verification set on an unchanged
checkout, then demonstrate that a failing test, build or formatting check fails CI.

**Acceptance Scenarios**:

1. **Given** the unchanged foundation, **When** local verification and CI run,
   **Then** repository checks, relevant unit/contract tests, both database profiles,
   browser smoke, formatting, linting, type checking and builds pass, with each
   required check identifiable in the results.
2. **Given** the PostgreSQL test profile and a fresh dedicated test database,
   **When** its integration suite runs, **Then** it connects to real PostgreSQL,
   verifies platform persistence and readiness, and removes only its owned test
   resources; an in-memory substitute does not count.
3. **Given** a deliberately failing required check or unavailable required test
   dependency, **When** CI runs, **Then** it reports failure with the check name
   and diagnostic evidence rather than skipping it or returning success.
4. **Given** repeated runs from identical tracked dependency declarations,
   **When** local and CI environments restore and verify the checkout, **Then**
   they use the pinned supported baseline without changing tracked dependency files.

### User Story 3 - Diagnose failure and recover safely (Priority: P1)

A contributor or operator can tell whether the host is running, whether it can
serve requests, and why the web console cannot reach it, without exposing secrets.

**Why this priority**: Misleading success and destructive recovery undermine the foundation.
**Independent Test**: Interrupt the host or test database, observe the affected
surfaces, restore the dependency and follow the documented recovery path.

**Acceptance Scenarios**:

1. **Given** a ready host, **When** its configured database becomes unavailable,
   **Then** readiness reports unavailable, process liveness remains independent,
   and the console does not continue showing a healthy connection.
2. **Given** an unreachable host or a slow response, **When** the console checks
   status, **Then** it exposes a bounded loading state followed by an actionable
   unavailable state and retry; recovery displays current status without a full reinstall.
3. **Given** invalid database configuration or an unwritable local data directory,
   **When** startup is attempted, **Then** a sanitized diagnostic identifies the
   setting or access problem, readiness never succeeds, and no fallback database
   or replacement of existing data occurs.
4. **Given** a failed request carrying a credential-like test value,
   **When** diagnostics are inspected in the page, response and captured logs,
   **Then** none contains the value, connection credentials, filesystem layout
   or a raw stack trace; authorized local logs provide a correlation reference
   and enough sanitized detail to locate the failed component.

### User Story 4 - Understand and navigate the foundation (Priority: P2)

A contributor can inspect the bootstrap contract and use the initial page without
a mouse, while seeing clearly that catalog, identity and module features do not exist yet.

**Why this priority**: Accessible, honest scaffolding prevents false expectations.
**Independent Test**: Inspect the published local API description and exercise the
page in a browser using keyboard navigation and narrow/wide viewports.

**Acceptance Scenarios**:

1. **Given** the running local profile, **When** the contributor opens its
   documented API description, **Then** the available bootstrap routes, response
   shapes, failure states and authentication applicability match actual responses.
2. **Given** a narrow or desktop viewport, **When** the page and retry control are
   used by keyboard, **Then** content remains readable without horizontal overflow,
   focus is visible, status is conveyed without color alone and changes are
   announced accessibly.
3. **Given** no installed modules or domain features, **When** the page loads,
   **Then** it explains that empty state and does not present fabricated services,
   a login identity, available privileged actions or working MCP tools.

### Edge Cases

- A second local start cannot silently take over another process's ports or delete
  its data; it identifies the conflict (US1.4).
- Missing/unsupported prerequisites, offline first-time restores and incomplete
  startup identify the failed step; a warm local run has no mandatory cloud call
  (US1.2/US1.4, US2.4).
- A configured PostgreSQL outage or invalid credentials must not fall back to
  SQLite, and a locked/unwritable SQLite file must not be replaced (US3.1/US3.3).
- Data paths attempting to escape the configured root, including symlink escapes,
  are rejected before any file outside that root is created or modified (FR-007).
- Malformed status requests, unrecognized routes and failure responses never
  reveal secrets or unlock a domain operation (FR-008/FR-010).
- A previously healthy status must be refreshed, not treated as permanent evidence;
  an initial request that never completes must reach the unavailable state (US3.1/US3.2).
- Destructive resets and database test cleanup must not affect a developer's
  existing data or a database that was not explicitly designated for tests (FR-016).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: A clean checkout MUST include an executable host and web skeleton,
  with only demonstrably shared package responsibilities. Both MUST run with no
  optional feature module. The locked baseline is the .NET modular monolith with
  module-local vertical slices and the Next.js/React/TypeScript frontend; this
  requirement does not select new libraries or package boundaries.
- **FR-002**: The contributor guide MUST list supported tools/versions, initial
  restore/setup, configuration, expected addresses, one local start command,
  verification commands, stop/restart, persistence and troubleshooting. Following
  it MUST require no undocumented machine edits, private service or cloud account.
- **FR-003**: The default local start MUST launch the host and web console with
  SQLite and non-destructive persistent storage. Configuration examples MUST
  contain no real secrets. Required setup failures MUST be reported with a nonzero
  command result; a live process alone MUST NOT be reported as a successful start.
- **FR-004**: The web skeleton MUST show the Köfte name, bootstrap scope, empty
  feature state and current host connection/readiness, with loading, ready,
  unavailable and retry behavior. A status attempt MUST finish or time out within
  10 seconds; displayed status MUST be rechecked within 30 seconds and after retry.
- **FR-005**: Documented health interfaces MUST distinguish liveness from readiness.
  A running process can be live while unready. Readiness MUST fail during incomplete
  startup, failed configuration, or inability to use the selected database.
  Responses MUST contain only a coarse state, safe component label and correlation
  reference where relevant, not credentials, paths, dependency internals or stack traces.
- **FR-006**: The local profile MUST provide an OpenAPI description of every
  bootstrap HTTP route, covering success/failure responses and access policy.
  Automated checks MUST detect disagreement with runtime behavior. This is not a
  commitment to the later domain API or module contracts.
- **FR-007**: Storage verification MUST exercise real SQLite and real PostgreSQL
  read/write persistence and restart/reconnection using test-owned records.
  Bootstrap persistent objects, if needed, MUST be platform-owned: a dedicated
  platform SQLite file under a canonical restricted data root and a reserved
  platform PostgreSQL schema, never `public`. Invalid/escaping local paths MUST
  fail before access. No feature-module data or allocation registry is introduced.
- **FR-008**: The only unauthenticated bootstrap HTTP capabilities MUST be
  non-sensitive health/readiness and local development API documentation.
  The web shell is public but contains no identity or team/resource data.
  Other operations MUST be absent or denied server-side; no permissive fake
  identity, test administration route or business mutation may be shipped.
- **FR-009**: Local configuration MUST identify the active profile, addresses,
  storage provider and data location in contributor-facing documentation.
  Invalid values MUST fail with actionable sanitized diagnostics rather than
  silent defaults or provider switching. The default local profile MUST bind to
  loopback; any broader exposure MUST be explicit and documented as unsupported
  for production use until the later security/deployment features exist.
- **FR-010**: Bootstrap requests and startup/storage failures MUST produce
  structured, correlated, secret-redacted diagnostics. The foundation MUST support
  vendor-neutral OpenTelemetry without requiring an external collector to start.
  Diagnostic responses MUST remain minimal; operational logs MUST avoid tokens,
  credential-bearing connection strings, configuration dumps and request bodies.
- **FR-011**: The page MUST meet WCAG 2.2 AA criteria applicable to its content,
  support keyboard access and visible focus, announce status updates, and work
  without horizontal overflow at widths from 320 to 1440 CSS pixels. Its design
  MUST align with the constitution's UI baseline without delivering the full
  design system or extension SDK ahead of their specs.
- **FR-012**: Contributors MUST have documented, repeatable formatting, lint,
  type-check, build and focused test commands for the host, frontend and any
  shared packages. Relevant unit, HTTP contract, real-provider integration and
  Playwright smoke tests MUST verify this feature's positive and failure paths.
- **FR-013**: CI MUST execute those required commands and the repository checks
  for pull requests and main-branch changes. A failing or unavailable mandatory
  check MUST make verification fail; no skipped provider/browser suite may be
  presented as passing. Reports MUST identify commands, outcomes and failed stage.
- **FR-014**: Supported runtime/tool/dependency baselines MUST be pinned in tracked
  configuration/lockfiles as appropriate, restorable without drift and documented.
  New direct/transitive dependencies MUST have recorded license/provenance review.
  No MediatR, AutoMapper or compulsory cloud, broker, Hoard or IaC dependency is allowed.
- **FR-015**: Any bootstrap schema and its initialization/upgrade behavior MUST
  be documented and tested on both providers. Ordinary restart MUST preserve
  existing data, fail safely on incompatible state and identify backup and
  forward-recovery or rollback steps without promising unsafe downgrade.
  If no application schema is needed, documentation MUST say so and distinguish
  isolated test fixtures from shipped tables; no speculative module schema is required.
- **FR-016**: Stop/retry MUST NOT delete persistent data. Reset MUST be a separate,
  explicitly destructive documented action requiring confirmation. Tests MUST
  isolate and clean only their own designated data; PostgreSQL verification MUST
  refuse a target not explicitly designated for disposable testing.
- **FR-017**: README and related contributor/architecture documentation MUST
  describe what actually runs, its local-only security limits, configuration and
  evidence. They MUST NOT claim module isolation, authentication, MCP, production
  readiness, spec acceptance or MVP completion from a bootstrap smoke result.

### Interfaces and Permission Parity

| Surface | Applicable behavior and boundary |
| --- | --- |
| Web UI | Public local bootstrap page and status/retry only; no team context, fabricated actor, protected records or privileged controls (FR-004/008/011). |
| HTTP | Minimal non-sensitive liveness/readiness; development-only OpenAPI access. Exact paths/shapes and documentation exposure controls belong to planning and must satisfy FR-005/006/008/009. UI and direct callers observe the same readiness, not separate success logic. |
| Authorization | No domain operation or registered business permission exists in 001. Every other operation is absent/denied, not implicitly authorized. Shared actor/team/resource policy arrives in 005/006; bootstrap public diagnostics are not a general exemption for future routes. |
| Modules/packages | Host starts with none installed. Only shared responsibilities necessary for the skeleton are included. Manifest/identity, backend/frontend extension contracts, collision checks and trusted packaging belong to 002-004/014/020, not an invented bootstrap SDK. |
| MCP | No endpoint or tools in 001; functioning authenticated discovery is required in 007 and domain parity in 013. A stub returning successful tool responses would be misleading. |
| Jobs/events/providers/realtime | No durable work, event delivery, external provider execution or subscriptions in this feature. Their contracts and corresponding permission/retry/cancellation tests are deferred to their inventory specs, not waived for MVP. |

### Data Ownership and Compatibility

The platform owns bootstrap configuration and any minimal persistent platform state;
there are no tenant, team, catalog or resource entities. Storage tests may create
isolated fixtures but MUST NOT expose a product CRUD/test endpoint to do so.
Provider availability and persistence MUST be demonstrated even if no application
table is needed. Backup/restore instructions cover the actual platform store, with
a tested restoration of its test-owned fixture; they do not claim a complete
multi-module upgrade or backup system.

The following remain **locked constraints for later modules**, not deliverables of
001: immutable namespaced module identity; persisted platform registry mappings;
SQL-safe PostgreSQL schema names bounded to 63 bytes, reserved-name protection,
UNIQUE/concurrency conflict handling, and module-local contexts/migration history;
one canonical restricted-root SQLite physical file per module plus a separate
platform file. Neither provider permits cross-module table access, joins or implied
cross-module ACID; SQLite `ATTACH` is not an isolation substitute. A platform-only
bootstrap test MUST NOT be reported as proof of those module allocator invariants.

Bootstrap HTTP contracts and the checked-in API description MUST identify their
contract version and remain consistent across local and CI profiles. Changing
them requires corresponding contract tests and documentation. Dependency upgrades
require lockfile and compatibility verification. Module/resource/event/MCP
versioning and desired/observed resource status do not yet apply because none is exposed.

### Experience, Security and Operations

The skeleton presents truthful loading, empty and unavailable states; no artificial
login or permission-granted state is needed. Keyboard and browser evidence must
cover retry, focus and status announcements as well as a healthy screenshot.
Malformed input, configuration errors and database outages must not disclose
sensitive details, enable new actions, silently switch storage or erase state.
Local API-documentation access is not permission to expose development tooling on
an untrusted network.

Startup/configuration/storage failures and any rejected access receive sanitized
operational diagnostics and correlation where applicable (FR-008/010). No sensitive
domain changes occur, so an immutable domain audit store is not added here; its
later audit requirements remain in force. No retrying durable job or external
integration exists, so job leases, side-effect idempotency, SSRF controls and
cross-module compensation are not simulated.

Local startup requires neither Kubernetes nor Azure, Hoard, a broker, an external
telemetry service or an IaC engine. Supported local operating environments and the
reference environment for timing measurements must be recorded during planning.
Production TLS/authentication/ingress and full Kubernetes assets remain spec 022;
001 documentation must warn against treating the local profile as production.
Trusted in-process code is not sandboxed, and no remote JavaScript/plugin loading
is introduced.

### Key Entities *(if applicable)*

- **Development profile**: Platform-owned configuration describing local SQLite or
  PostgreSQL verification, required tools, bindings and persistence. It is not a tenant.
- **Bootstrap status**: Observed liveness/readiness and safe diagnostics for the
  current process/dependency state; never a client-controlled declaration of health.
- **Platform store**: Host-owned file/schema, with test-owned fixtures for
  persistence/recovery verification and no module or team records in this feature.
- **Verification result**: A named required check with command, outcome and
  diagnostic evidence for a particular revision/profile; not proof of spec acceptance.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With documented prerequisites and restored dependencies, a contributor
  unfamiliar with the checkout can start the complete local skeleton using one
  command and reach a ready page within 5 minutes on the documented reference
  environment, without an undocumented edit or mandatory hosted account.
- **SC-002**: In 10 consecutive warm local starts on that environment, the initial
  page becomes usable within 2 seconds of navigation once the services are ready;
  all starts report actual readiness and all non-reset restarts preserve test data.
- **SC-003**: Every required check passes on the unchanged foundation locally and
  in CI; deliberate test, formatting and build failures each cause a failed
  verification result rather than a success or unreported skip.
- **SC-004**: For every documented outage/invalid-configuration scenario, the user
  sees an actionable failure instead of success; a hanging status request resolves
  within 10 seconds and a dependency outage is visible within 40 seconds
  (30-second refresh plus bounded request). Recovery succeeds using the guide,
  without data loss or reinstalling the repository.
- **SC-005**: All bootstrap interactions can be completed using a keyboard at the
  documented viewport bounds, with zero serious/critical automated accessibility
  findings and manual confirmation of focus, readable states and status announcements.
- **SC-006**: Captured responses, console output and logs expose zero seeded test
  secrets across the defined failure cases; all persistence/recovery scenarios
  pass independently in both documented data profiles.

### Required Validation

These are **future implementation acceptance obligations**, not checks claimed to
have run during specification. Evidence must identify the revision, environment,
commands/results and scenario/requirement mapping.

| Evidence category | Required coverage / acceptance mapping |
| --- | --- |
| Local startup smoke | Clean-checkout guide, no optional modules/services, prerequisite and port conflicts, stop/restart; US1.1-4, FR-001-003/009/017, SC-001/002. |
| Real-provider integration | Actual SQLite and PostgreSQL round trip, persistence/reconnection, unavailable/wrong credentials, locked/unwritable file, invalid/escaping paths including symlinks, target isolation and cleanup; FR-005/007/015/016, US1.3/US2.2/US3.1/US3.3, SC-006. No in-memory substitution. |
| Initialization/migration and recovery | Test any shipped platform schema on clean install and upgrade, safe incompatible-state failure, non-reset restart, explicit reset guard and backup/restore on both providers; FR-015/016. If no schema ships, document that fact and test provider fixtures without claiming module migration coverage. |
| Focused unit/regression | Configuration validation, status mapping/timeouts, secret sanitization and new logic introduced by 001; relevant failure and success cases under FR-003-005/008-010. Do not add tests solely to repeat framework behavior. |
| HTTP contract / negative access | Documented success/failure statuses and versioned shapes match OpenAPI; no unexpected anonymous mutation/admin/test route; malformed inputs and responses remain sanitized; US4.1, FR-005/006/008/010, SC-006. |
| Playwright / experience | Healthy, loading, empty, unavailable/timeout and retry/recovery journeys; actual status changes, keyboard/focus/announcements, responsive widths and measured navigation; US1.1/US3.1-2/US4.2-3, FR-004/011, SC-002/004/005. |
| Toolchain / CI | Restore without tracked drift, formatting, lint, type checks, builds, relevant tests and deliberately failing check demonstrations; US2.1-4, FR-012-014, SC-003. Record dependency license/provenance review. |
| Telemetry / privacy | Correlated request and startup/storage failure evidence, no collector required for local start, seeded-secret redaction in responses/UI/logs; US3.3-4, FR-009/010, SC-006. |
| Repository / handoff | Each changed stage runs `python3 scripts/check_repository.py`, `python3 -m unittest discover -s scripts/tests -v`, and `git diff --check`; FR-017. Human merge and explicit final feature acceptance remain separate gates. |

**Deferred with reasons**: Cross-team authorization and denied MCP execution need
identity, permissions and MCP (005-007/013). Module manifest contracts, allocator
collisions/concurrency and module schema/file migration isolation need 002-004.
Event contracts, restart-safe jobs and provider controls need 016-019. Independently
packaged reference-module proof belongs to 020/024; production/Kubernetes deployment
smoke belongs to 022/024. None can be marked passed from 001's platform-only tests.
Applicable local deployment smoke and bootstrap HTTP/browser/storage evidence are
required here; all deferred constitutional release evidence remains mandatory at
the relevant inventory gate.

## Assumptions and Constitutional Review

**Assumptions**: The local skeleton has no business data, optional modules or real
identity, making non-sensitive diagnostics its only public host capability.
SQLite is the default development profile; PostgreSQL is a real disposable
verification profile, not a production deployment promise. Initial dependency
downloads may require internet access; subsequent local runtime has no compulsory
external service. Timing bounds exclude first-time dependency acquisition and
are assessed on the reference environment documented in planning.

**Locked versus undecided**: The constitution's .NET/VSA and Next.js/React/TypeScript,
Tailwind/shadcn/ui/Radix/Lucide baseline, native DI/direct handlers, storage ownership,
trust and scope rules are constraints, not newly selected implementation details.
Exact supported versions, package boundaries, test frameworks, local orchestration
(including Aspire versus Compose), package manager, bootstrap route shapes,
configuration keys, any minimal platform schema and telemetry wiring are planning
decisions. Consequential choices require ADRs; proposed architecture libraries
are not automatically adopted. No material unanswered product question prevents
this draft; the separate clarify stage must still evaluate it before planning.

| Principle | Applicability and traceability |
| --- | --- |
| I - Product/brand | Köfte, self-hosting, no optional module or Hoard dependency: FR-001-004/014/017. |
| II - Modularity/trust | Module-free host and no fake SDK/runtime or sandbox promise: FR-001, scope and interface exclusions; lifecycle work remains 002/004. |
| III - Vertical slices | Minimal shared responsibilities, locked native DI/direct handlers, no MediatR/AutoMapper and dependency review: FR-001/012/014; detailed structure belongs to planning. |
| IV - Storage | Real provider/platform ownership and path safety: FR-007/015/016; module registry, allocator concurrency and per-module history are explicitly deferred, not weakened. |
| V - Transactions | Platform-only state; no cross-module joins, shared connection or atomicity claim. Cross-module reliable coordination has no operation to apply to in 001. |
| VI - Identity/safety | Loopback, sanitized diagnostics, no domain data or permissive identity: FR-008-010/016. Real login/team policy and domain audit arrive in 005/006/009. |
| VII - Early MCP | No pretend MCP surface; 007/013 obligations retained in interface/validation exclusions. |
| VIII - Resources/durable/realtime | No resources/jobs/events/providers/subscriptions yet; their ownership, retries, idempotency and restart requirements remain later-spec obligations. |
| IX - Frontend | Truthful states, baseline consistency, accessibility and bounded experience: FR-004/011, SC-002/004/005. No remote frontend loading. |
| X - Portability/operations | Local profile, real shared-provider testing, pins, docs, probes, telemetry and safe recovery: FR-002-007/009/010/014-017; production packaging deferred to 022. |
| XI - Evidence | Focused unit, real-provider/recovery, HTTP contracts, browser and local smoke evidence: FR-007/012/013/015/016 and validation matrix; all later release proofs remain required. |
| XII - Scope/traceability | Inventory ID, actors, bounded scope, scenarios and mapping present; no feature acceptance or MVP-complete claim. Clarify and all later stages use their own invocation on this same branch/draft PR; human merge follows passing review and explicit feature acceptance. |
