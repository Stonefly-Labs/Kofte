# Feature Specification: 002 - Platform Contracts and Module Identity

**Feature Branch**: `feature/002-platform-contracts`
**Feature Directory**: `specs/002-platform-contracts`
**Created**: 2026-10-21
**Status**: Draft; specify stage only, not implemented or human-accepted
**Input**: Exact feature description extracted by the driver's `prompt` command:

```text
Specify Köfte feature 002: Platform contracts and module identity. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Implement the minimal host contracts, immutable reverse-DNS module IDs, capability/version declarations, compatibility ranges, validation errors and manifest schema.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Manifest parser and validator rejects malformed IDs, duplicate capabilities and incompatible host versions with actionable diagnostics.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 001. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

**Constitution**: Adopted v2.0.1; principles I-XII are mapped below.

## Scope and Dependencies

**Purpose**: A module author can describe a module once and learn, before any
module code or storage is activated, whether that description is valid for a
given Köfte host contract. A maintainer can consume the same small, versioned
contract without duplicating identity or compatibility rules.

- **Inventory**: 002, required within MVP 001-024. This stage specifies the
  contracts; it neither implements them nor completes the feature or MVP.
- **Prerequisite**: Human-accepted, merged 001 supplies the runnable module-free
  host/web, supported toolchain and verification workflow. Its acceptance record
  and evidence are present on updated main. No module runtime already exists.
- **Owner and actors**: The platform owns manifest interpretation, canonical
  identity rules, host-contract compatibility and validation diagnostics. Module
  authors own their declarations; platform maintainers consume validated
  descriptions and review contract changes. Operators use sanitized validation
  outcomes to correct configuration, not to grant module trust.
- **Included**: Minimal reusable host/module description and validation
  contracts; a versioned machine-readable manifest schema; immutable reverse-DNS
  identity; module, schema, host-contract and capability versions; declared
  dependencies, permissions and storage needs; deterministic parsing and
  validation; documented valid/invalid examples and consumer compatibility proof.
- **Excluded**: Persistent identity/storage allocation and migration (003);
  discovery, loading, installation, dependency resolution/order/cycles and
  enable/disable lifecycle (004); authentication and authorization services
  (005/006); MCP server/tool execution (007/013); UI shell/extension SDK
  (008/014); durable audit storage (009); resource/event/job/provider runtimes
  (012/016-019); packaged reference module (020); install/upgrade UX and
  deployment assets (021/022). External MCP federation, Hoard and AI chat remain
  post-MVP. Declaring a capability does not implement its later runtime.
- **Security boundary**: Validation is a local author/maintainer operation over
  supplied data, not an unauthenticated upload, administration or discovery
  endpoint. It has no actor/team/resource records or authority to enable code.
  An acceptable manifest is neither publisher verification nor a sandbox.
- **Sources**: [Charter](../../docs/product-charter.md),
  [vision](../../docs/product-vision.md), [architecture](../../docs/architecture.md),
  [inventory 002](../../docs/master-spec-inventory.md#002--platform-contracts-and-module-identity-mvp)
  and [constitution](../../.specify/memory/constitution.md).
  No conflicting canonical requirement was identified.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Describe a module without guessing host rules (Priority: P1)

A module author follows the documented contract to describe a module and check it
against an explicitly supplied host-contract version. They receive either an
accepted description or enough information to correct it without reading host
implementation code.

**Why this priority**: Stable, understandable declarations are prerequisites for
every subsequent module capability.
**Independent Test**: A consumer outside host internals submits documented example
manifests through the public contract and compares results with the schema.

**Acceptance Scenarios**:

1. **Given** a supported manifest schema, canonical identity, valid declarations
   and a compatible host-contract version, **When** an author validates the
   manifest, **Then** validation succeeds and exposes the declared identity,
   versions, capabilities, dependencies, permissions and storage needs without
   executing code or creating storage.
2. **Given** a missing required declaration, wrong value type, malformed document
   or ambiguous repeated field, **When** validation runs, **Then** it fails with
   a stable error category, safe location and corrective guidance; no partially
   valid description is reported as accepted.
3. **Given** an unsupported schema version or undeclared field, **When** validation
   runs, **Then** it rejects the input rather than silently ignoring potentially
   meaningful configuration or interpreting it as another schema version.
4. **Given** the same valid or invalid input and host-contract context, **When**
   two independent consumers validate it repeatedly, **Then** acceptance and
   diagnostic identities/order agree regardless of machine locale or culture.

### User Story 2 - Keep module identity stable and unambiguous (Priority: P1)

A module author chooses one globally namespaced identity, then changes display
metadata or releases a new version without accidentally changing the module's
identity or introducing aliases.

**Why this priority**: Later ownership, permissions and storage must not depend on
mutable names or lossy normalization.
**Independent Test**: Check canonical and malformed identity examples, then compare
updated declarations with an explicitly supplied previously accepted identity.

**Acceptance Scenarios**:

1. **Given** a valid reverse-DNS identity, **When** its display name or module
   release version changes, **Then** its canonical identity stays exactly equal,
   and a validated description cannot be altered in place to impersonate another ID.
2. **Given** an uppercase, whitespace-padded, Unicode, path-like, empty-label,
   overlength or otherwise malformed identity, **When** validation runs, **Then**
   it rejects that identity with an actionable diagnostic rather than trimming,
   lowercasing, transliterating or turning it into a storage identifier.
3. **Given** an update check with a previously accepted identity, **When** a
   candidate declares a different ID, **Then** validation rejects the identity
   change; treating it as a different module requires a distinct future
   registration, not an implicit rename or reuse.

### User Story 3 - Understand compatibility and capability failures (Priority: P1)

A module author or maintainer can determine whether a declaration targets a host
contract they support and can distinguish malformed versions from legitimate
incompatibility before attempting installation.

**Why this priority**: Silent compatibility guesses lead to unsafe activation and
fragile extension contracts.
**Independent Test**: Exercise a documented version/range boundary matrix and
capability/dependency fixtures, with expected results fixed independently of the
validator.

**Acceptance Scenarios**:

1. **Given** a valid supported compatibility range, **When** the supplied host
   contract is exactly at, inside or outside its boundaries, **Then** validation
   applies the documented inclusive/exclusive and prerelease rules and clearly
   accepts or reports host incompatibility.
2. **Given** an invalid semantic version, malformed/unsupported range, or missing
   host-contract version, **When** validation runs, **Then** it reports that
   specific input error rather than assuming a compatible version or conflating
   it with a valid but unsatisfied range.
3. **Given** duplicate capability identities, an unknown capability kind or an
   unsupported capability-contract version, **When** validation runs, **Then**
   it identifies the offending declaration and rejects it; declaring a capability
   twice with different versions does not avoid duplicate detection.
4. **Given** malformed, duplicate or self-referencing declared dependencies,
   **When** validation runs, **Then** it rejects the declaration; **Given** a
   structurally valid dependency on an absent module, **When** manifest-only
   validation runs, **Then** it preserves the dependency without claiming it is
   installed, resolvable or ready.

### User Story 4 - Correct rejected input without unsafe side effects (Priority: P2)

An author or operator receives useful bounded failure information and can retry
with corrected input without losing data, fetching remote content or activating
an untrusted module.

**Why this priority**: Diagnostics must help the author without turning a manifest
parser into an execution, disclosure or resource-exhaustion path.
**Independent Test**: Submit malformed, oversized and credential-seeded inputs;
inspect results, captured diagnostics and observed filesystem/network effects.

**Acceptance Scenarios**:

1. **Given** invalid input containing a credential-like marker or control
   characters in a declaration, **When** validation fails, **Then** results and
   operational output contain no raw marker, manifest dump, stack trace or
   injected log line, while identifying a safe field location and error category.
2. **Given** oversized/deep input or declarations attempting external resolution,
   executable hooks or caller-selected storage paths, **When** validation runs,
   **Then** it returns a bounded rejection without network access, code execution,
   registry writes or files/schemas created on the module's behalf.
3. **Given** a rejected input followed by a corrected valid manifest, **When** an
   author retries against the same host-contract context, **Then** it succeeds
   without reset, reinstall or state left over from the failed attempt.

### Edge Cases

- IDs differing only by case, hidden whitespace, look-alike Unicode or punctuation
  do not become aliases through normalization (US2.2).
- Identity and display-name changes are distinct: a Unicode display name such as
  Köfte remains valid human-facing metadata and never selects permissions or storage.
- Leading/trailing dots, empty labels, separators other than dots, leading/trailing
  label hyphens, and exact versus one-over identity length limits are covered.
- A host product release, manifest schema version, module release version and
  capability-contract version are not interchangeable compatibility inputs.
- Exact range boundaries, adjacent versions, prereleases and build metadata
  follow one documented rule set, not locale-dependent string comparison.
- Duplicate declaration detection uses semantic identity, not display text,
  declaration order or version differences. Repeated document keys cannot mask
  an earlier ID, version or capability.
- Empty capability/dependency/permission lists are allowed where applicable;
  malformed list entries are not equivalent to an empty list.
- A dependency's absence or a transitive dependency cycle is not proven safe by
  manifest-only validation; installed-set validation belongs to 004.
- A malformed input followed by a valid one cannot contaminate the second result;
  excessive diagnostic counts and untrusted text are bounded.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The platform MUST provide a minimal, versioned public contract for
  module descriptions and validation, usable without host internals or a running
  module runtime. It MUST include a machine-readable manifest schema, required
  and optional declarations, their meanings, supported values and representative
  valid/invalid examples. A supported manifest MUST declare its schema version,
  immutable ID, module semantic version, display name, compatible host-contract
  range, capabilities, dependencies, permission definitions and storage needs;
  empty/no-storage declarations MUST be explicit and supported.
- **FR-002**: Parsing MUST reject malformed documents, ambiguous duplicate fields,
  missing required values, incorrect types, unsupported schema versions and
  unknown fields. Only schema-declared fields may affect the result. Input size,
  nesting, collection and diagnostic limits MUST be documented and enforced;
  the exact limits and serialization format belong to planning.
- **FR-003**: A module ID MUST be canonical lowercase ASCII reverse-DNS text with
  at least three dot-separated labels (namespace plus module), no empty labels,
  and only lowercase letters, digits and interior hyphens in each label. Labels
  MUST start/end with a letter or digit and contain at most 63 characters; the
  complete ID MUST contain at most 253 characters. Whitespace, Unicode, path
  separators and other punctuation MUST be rejected, not normalized. Display
  names MAY contain Unicode and MUST NOT be used as identity or authority.
- **FR-004**: Validated module identity MUST remain immutable for the lifetime of
  a description. When a consumer supplies a previously accepted ID for an update
  check, a different candidate ID MUST be rejected as an identity change.
  Changing display metadata or release version MUST NOT change identity.
  Validation MUST NOT allocate, rename or claim uniqueness in a persistent registry.
- **FR-005**: Module release versions and host/capability-contract versions MUST
  have documented semantic-version rules. Manifest schema versioning MUST be
  explicit and distinct from those versions. Invalid or unsupported version/range
  syntax MUST fail explicitly; no version may default to the current host,
  latest available release or an assumed compatible value.
- **FR-006**: Compatibility MUST be evaluated against the explicitly supplied
  host-contract version, not an inferred product or installed dependency version.
  Supported range notation, lower/upper inclusivity, prerelease eligibility and
  build-metadata precedence MUST be documented with boundary examples. Valid
  unsatisfied ranges MUST produce an incompatibility diagnostic distinct from
  malformed ranges. Supported rules and schema/validator behavior MUST agree.
- **FR-007**: Capability declarations MUST identify a supported contribution kind
  and its contract version, covering backend, frontend, resources, providers,
  events, workflows/jobs, permissions and MCP tool contributions as declaration
  categories, not runnable implementations. Duplicate capability identities MUST
  fail even if their versions differ. Unknown kinds and unsupported contract
  versions MUST fail explicitly. Empty capabilities MUST be valid; capability
  metadata MUST NOT grant execution rights or claim sandbox enforcement.
- **FR-008**: Declared dependencies MUST reference canonical module IDs and
  supported version ranges. Invalid entries, duplicate dependency identities and
  self-dependencies MUST fail. Valid dependencies MUST remain visible in an
  accepted description without requiring those modules to be present. Dependency
  resolution, cycles, ordering and installed-version conflicts MUST NOT be
  presented as checked by this manifest-only contract.
- **FR-009**: Permission declarations and storage requirements MUST be validated
  as descriptive metadata with documented names, uniqueness and allowed values.
  Permission references MUST remain attributable to their owning module and MUST
  NOT grant access. Storage declarations MUST express needs, not caller-chosen
  physical schema names, database files, credentials or cross-module table access.
  Detailed permission enforcement and registry allocation remain 006 and 003.
- **FR-010**: Validation MUST return either an accepted complete description or
  an explicit rejected result with no accepted partial descriptor. Rejections
  MUST expose stable machine-readable categories, safe field/item locations and
  corrective messages. Diagnostics MUST distinguish parsing, schema, identity,
  declaration, version and host-incompatibility failures; ordering and truncation
  MUST be deterministic. Failures MUST NOT become warnings followed by success.
- **FR-011**: For identical input and explicit validation context, public consumers
  MUST receive equivalent acceptance decisions and ordered diagnostic identities
  regardless of locale, declaration iteration behavior or prior attempts.
  Corrected input MUST be independently retryable without cleanup or mutation
  of a previous accepted description.
- **FR-012**: Validation MUST treat input as data only: no module loading,
  executable hooks, external reference fetching, DNS lookup, installation,
  storage allocation, migration or changes to host configuration. Invalid or
  over-budget input MUST terminate with an actionable bounded failure. Returned
  and emitted diagnostics MUST avoid raw input values, secrets, unsafe control
  characters, filesystem layout and exception traces.
- **FR-013**: The schema and validation contract MUST have a shared documented
  acceptance corpus, including schema-valid but semantically incompatible inputs.
  Documentation MUST distinguish structural validation from semantic/contextual
  validation; schema-only success MUST NOT imply host compatibility. An external
  contract consumer MUST demonstrate the supported public surface without relying
  on internal host types or copying validation rules.
- **FR-014**: Documentation MUST explain author validation, expected diagnostics,
  correction/retry, compatibility boundaries and the distinction between a valid
  manifest and trusted/installed/enabled code. Contract changes MUST identify
  breaking effects and the supported schema/host-contract combinations; an
  unsupported version MUST fail closed rather than silently reinterpret input.
- **FR-015**: Introducing these contracts MUST preserve 001's module-free startup,
  existing health/readiness and local storage behavior. No public upload,
  installation, administration, business or MCP endpoint, persistence object,
  optional module or new mandatory external service may be added by 002.
  Dependency changes, if needed at implementation, require supported pinned
  versions and direct/transitive license and provenance review.

### Interfaces and Permission Parity

| Surface | Applicable behavior and boundary |
| --- | --- |
| Module/host contracts | Primary author-facing interface: manifest/schema, explicit host-contract and optional prior-ID context, accepted description or structured rejection. No discovery or activation API is implied (FR-001-014). |
| UI and HTTP | No new page, route or manifest-upload interface. Existing bootstrap diagnostics remain unchanged. Author examples and diagnostics are the experience being specified, not the later install/upgrade UI (FR-014/015). |
| Authorization | Local validation grants no registered permission, tenant/team scope, storage ownership or administrative action. Declared permissions are metadata only. Identity/central policy arrive in 005/006; any later REST/UI/MCP/job adapter must apply the same actor/team/resource checks before invoking protected operations. |
| MCP | Declare the capability category only. Tool names, schemas, registration and collision checks belong to 007/013; no endpoint, tool execution, discovery response or auth bypass exists here. |
| Events/jobs/providers/realtime | Declare applicable contribution categories, not dispatch, execution, retries, subscriptions or provider credentials. Detailed contracts and runtimes remain with 016-019 and their authorization gates. |
| Frontend extensions | A declaration is not permission to load remote JavaScript. Versioned trusted build/deployment-time composition remains 014; no bundler or remote loading model is selected. |

### Data Ownership and Compatibility

The platform owns the meaning of the contract; module authors supply manifest
data. A validation result is transient, immutable descriptive data, not an
installed module, team resource or persisted registry entry. Callers explicitly
supply the host-contract version and, for update comparison, prior identity.
No database, data migration, backup or restore operation is introduced; retaining
or reverting an authored manifest is ordinary source/configuration management,
not rollback of installed module data.

Module IDs MUST NOT be reused as physical names. The later registry must preserve
immutable mappings: PostgreSQL named schema per module, module-local context and
migration history, 63-byte safe identifiers, reserved-name protection and database
UNIQUE/concurrent conflict handling; SQLite separate physical file per module
plus platform file, canonical restricted-root paths and traversal rejection.
Those are locked requirements for 003/004, not evidence supplied by a parser.
There is no cross-module table access, shared connection or implied ACID;
`ATTACH` is not a substitute for file isolation.

Manifest schema, module release, host contract and capability contract versions
are separate concepts. Supported schema/contract combinations, range semantics
and invalid examples must be published together. A compatible module release is
not proof of installed dependency compatibility, publisher trust, data migration
safety or executable contribution compatibility. Those later checks cannot be
replaced by a successful declaration check.

### Experience, Security and Operations

Authors receive a clear accepted/rejected result, safe location, stable category
and corrective action rather than an opaque exception or a success-shaped
fallback. Empty optional declaration lists are legitimate, not a loading or
permission-granted state. There is no interactive web journey, so new loading,
focus, responsive-layout and page-accessibility criteria do not apply; existing
bootstrap UX must not regress.

Inputs are untrusted even though eventual in-process modules are trusted code.
Parsing must not execute that code, resolve external references or read secrets.
Safe locations use schema-defined fields and bounded list positions rather than
reflecting arbitrary property names or submitted values. Examples contain no
real credentials. Output/log size and evaluation budgets are explicit; limits
do not silently truncate declarations into a different accepted manifest.

Validation outcome diagnostics must be usable by the caller and any existing
correlation mechanism without requiring a telemetry collector. No durable
sensitive operation or identity-bearing request occurs, so a new audit store is
not warranted. Module installation/authorization auditing remains mandatory in
later specs. Retry means resubmission of corrected data, not an autonomous job;
idempotent side effects, cancellation leases and compensation are inapplicable
because validation performs no external state change.

The contract must work offline once dependencies are restored and must not depend
on a cloud account, Kubernetes, Azure, Hoard, a broker or an IaC engine. A schema
version identifier is not a network location to fetch during validation.

### Key Entities *(if applicable)*

- **Module identity**: Platform-defined immutable reverse-DNS value; stable across
  display-name and release changes, with no storage or permission grant.
- **Module manifest/description**: Author-owned declaration and its validated,
  immutable interpretation, identifying schema/module versions, host compatibility,
  capabilities, dependencies, permissions and storage needs.
- **Host-contract context**: Explicit supported contract version and optional
  prior module ID for comparison; not inferred from the host product version.
- **Capability declaration**: A unique contribution kind/identity with a supported
  contract version; descriptive, not a loaded implementation.
- **Dependency declaration**: Another canonical module ID and supported release
  range; not a dependency resolver or installation record.
- **Validation result/diagnostic**: Complete acceptance or structured rejection,
  with stable categories, safe locations and bounded corrective messages.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An author unfamiliar with host internals can follow only the guide,
  validate a documented module description and correct one identity and one
  compatibility error within 10 minutes after the validation environment is
  ready, without editing the host or contacting an external service.
- **SC-002**: All supported valid examples are accepted and all documented invalid
  identity, schema, capability, dependency and compatibility fixtures are rejected
  with their expected categories; there are zero false acceptances in the boundary
  and abuse corpus.
- **SC-003**: Across 100 repeated runs of the acceptance corpus in at least two
  distinct supported locale settings and two independent consumers, acceptance
  decisions and ordered diagnostic identities agree in every run.
- **SC-004**: On the documented reference environment after initialization, each
  of 100 consecutive representative valid/invalid checks within the documented
  input limits completes within 1 second; oversized/deep inputs reach an explicit
  failure within that same bound rather than hanging or returning success.
- **SC-005**: Every seeded-secret/control-character and side-effect fixture produces
  zero secret disclosures, injected output lines, remote requests, module
  activations or module-owned files/schemas; corrected retry succeeds in every
  recovery fixture without reset.
- **SC-006**: Every documented supported and unsupported compatibility boundary is
  demonstrated through the public contract, and the existing module-free
  foundation retains all affected startup, health and persistence checks with no
  new mandatory infrastructure.

### Required Validation

These are **future implementation acceptance obligations**, not checks executed
or behavior delivered by this specify stage. Evidence must name the revision,
environment, exact commands/results and requirement/scenario mapping. A checked
author requirements checklist does not count as implementation or human acceptance.

| Evidence category | Required coverage / acceptance mapping |
| --- | --- |
| Focused unit/regression | Canonical ID grammar and exact/one-over limits, immutable identity/update comparison, distinct version domains, all range/prerelease/build boundaries, duplicate capabilities/dependencies and safe deterministic results; US1.2-4, US2.1-3, US3.1-4, FR-002-011, SC-002/003. |
| Manifest/schema contracts | Independently expected positive/negative corpus; structural versus contextual rejection; unknown schema/fields/kinds, duplicate keys, missing/wrong types and unsupported contract versions; US1.1-4/US3.1-4, FR-001/002/005-010/013/014, SC-002/006. |
| Public consumer compatibility | Independent consumer of the documented surface and a second consumer through the host-facing contract; no duplicated validator rules or host-internal references. Exercise supported combinations and explicit unsupported-version failure; US1.1/US1.4/US3.1-2, FR-001/005/006/013/014, SC-001/003/006. |
| Security/privacy and isolation | Seeded secrets/control characters; bounded input/depth/collections/diagnostics; external references, hooks, caller-selected physical storage and malformed inputs; no fetching/loading/storage mutation. Corrected retry and independent invocations; US4.1-3, FR-009-012, SC-004/005. |
| Author experience/performance | Follow the guide without host edits, correct identity/compatibility errors, record timing/reference conditions, 100-check bounded response exercise and explicit over-budget failure; US1-4, FR-010-014, SC-001/004. |
| Existing foundation and CI | Smallest relevant 001 startup, health/contract and existing persistence regressions on its actual supported providers when implementation changes shared/runtime behavior; affected build/format/type/lint checks and new focused suites in local/CI verification. No mocked provider may be called real-provider evidence; FR-015, SC-006. |
| Repository/stage handoff | Each changed stage runs `python3 scripts/check_repository.py`, `python3 -m unittest discover -s scripts/tests -v` and `git diff --check`; ledger and artifacts stay together on one feature branch/draft PR. No later stage or acceptance is inferred. |

.NET tests MUST reside physically in their dedicated test projects under `tests/`,
never in production projects or linked from production slices. A contract-consumer
fixture is not the separately packaged full reference module.

**Deferred with reasons**: New real-provider allocation/isolation/migration tests,
schema collisions/concurrent registration and SQLite path traversal require the
003/004 registry/runtime; no persistent store is added here. Installed dependency
cycles, duplicate installed modules and lifecycle recovery belong to 004.
Cross-team denial and REST/UI/MCP operation parity require 005-007/013; only
non-authoritative permission metadata exists here. New Playwright journeys require
an actual UI (008/014/021), while unchanged bootstrap UX retains its existing
coverage. Resource/event/MCP execution contracts and restart-safe jobs require
their later runtimes. Full packaged reference proof and local/Kubernetes release
smoke remain mandatory at 020/022/024, not claimed from manifest acceptance.

## Assumptions and Constitutional Review

**Assumptions**: Reverse-DNS identity is an assigned namespace convention; module
authors are responsible for namespace ownership. Validation performs no DNS
verification and makes no publisher-authenticity claim. The canonical lowercase
ASCII, DNS-sized grammar prevents aliases; display metadata remains Unicode.
Every schema declares its supported capability vocabulary and versions; future
extension categories require a reviewed contract update rather than permissive
unknown values. An explicitly supplied prior ID is sufficient for a pure update
comparison; durable ownership and uniqueness cannot be proven until 003/004.

**Locked versus undecided**: Immutable identity, semantic module versions, explicit
host compatibility, deterministic fail-closed validation, minimal shared
contracts, level-3 declaration coverage and non-authoritative permissions/storage
are requirements. Serialization format, field names, supported range notation,
precise prerelease rules, initial schema/host/capability version numbers, numeric
input budgets, public contract shapes, package/assembly boundaries and a documented
author invocation remain planning decisions. Planning must choose and publish
them with boundary evidence; no competing normative decision or material
requirements ambiguity is left unresolved here. Consequential choices belong
in a proposed ADR where warranted, not an implicit product-policy change.

| Principle | Applicability and evidence obligation |
| --- | --- |
| I - Product and brand | Köfte in human-facing documentation; portable module-free host; no Hoard dependency (FR-014/015). |
| II - Modularity | Stable identity, versions, capabilities/dependencies and actionable validation; no loader or sandbox claim (FR-001-010/012-014). |
| III - Vertical slices | Only demonstrably shared host/module contracts; no speculative runtime abstractions, MediatR or AutoMapper; dedicated test ownership and adoption review (FR-001/013/015; Required Validation). |
| IV - Module storage | Storage needs are metadata, not physical identifiers or allocation. Locked provider/registry/migration invariants remain explicit for 003/004 (FR-009/012/015; Data Ownership). |
| V - Transactions | No state changes or cross-module access/ACID; no fabricated outbox or compensation runtime (FR-009/012; Data Ownership). |
| VI - Authorization and safety | No rights conferred by a valid manifest; deny new remote/admin surfaces, sanitize and bound untrusted input; enforcement remains 005/006 (FR-002/009/010/012/015). |
| VII - Early MCP | MCP declaration category included; functioning authorized discovery/tools remain 007/013, external federation post-MVP (FR-007; interface table). |
| VIII - Durable work/resources | Declare contribution categories, not resource state, job execution or delivery guarantees; no runtime added ahead of 012/016-019 (FR-007/012; interface table). |
| IX - Accessible frontend | No new page or remotely loaded code; readable author diagnostics and no bootstrap UX regression. Full design/extension work remains 008/014 (FR-010/014/015). |
| X - Portability | Offline validation after restore, no new service/cloud dependency; explicit supported combinations, correction/retry and preserved bootstrap behavior (FR-012-015). |
| XI - Evidence | Focused negative/boundary/regression, schema and consumer contracts, determinism, privacy and bounded-execution proof; justified deferred runtime/release checks (Required Validation; SC-001-006). |
| XII - Scope/traceability | Inventory 002 depends on accepted/merged 001; all later runtime and post-MVP features excluded. Specification is not implementation, human acceptance or MVP release (Scope; Required Validation). |
