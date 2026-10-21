# Köfte — Master Spec Inventory and `/speckit.specify` Prompts

> **Status:** Implementation planning v0.1 · **Date:** 2026-10-20  
> **Companion docs:** `product-charter.md`, `product-vision.md`, `architecture.md`  
> **Usage:** Run `/speckit.constitution` with `constitution-prompt.md` **first**, then execute `/speckit.specify` in the dependency order below. Each prompt is individually copy/paste-ready.  
> **Brand:** Köfte for prose/UI; `kofte` for identifiers, filenames, and CLI.

## Scope boundary — authoritative

**MVP = specs 001–024 (inclusive).** A release is **not MVP-complete** until spec 024 passes the release gate; implementing isolated scaffolding or an MCP hello-world does not qualify. Specs **025–038 are POST-MVP** and are explicitly not blockers to shipping MVP. This roadmap refines sequencing but does not silently alter the MVP scope in the charter. Resolve deviations with an ADR and update both docs.

The goal is a *portable self-hosted composable internal developer platform*, **not** a required hosted SaaS, built-in AI chat, mandatory OpenTofu engine, or a generic workflow suite. Hoard is an **optional future module**, never a baked-in dependency.

### Implementation invariants across every spec

- **Full Level-3 modules:** backend, frontend, resource kinds/providers, events, workflows, permissions and MCP tools; first-party core capabilities use the same patterns where practical.
- **Modular monolith + VSA:** feature endpoints/handlers/validation co-located; .NET test source in dedicated test projects with mirrored module/feature ownership, never inside production projects; truly shared contracts in a minimal core; no MediatR or paid/encumbered mediator dependency.
- **PostgreSQL:** dedicated named schema **for every module**, including module migration history. Immutable module-ID→schema mapping in a platform-owned registry; byte-limited safe names; unique constraint and concurrent collision protection; never `public`.
- **SQLite:** **one physical file per module**, plus separate platform registry DB; no shared SQLite database, schema simulation or `ATTACH`-based cross-module design.
- **Transactions:** module-local transactions only. Across modules use versioned operations/events, persisted outbox/inbox, idempotency and compensation as needed; do not claim cross-database atomicity.
- **MCP is EARLY MVP (007)** and evolves along with core catalog/resource features (013), with a single auth and domain operation path; plugin tool names are namespaced and collision-checked. External MCP federation follows after MVP.
- **UI:** polished, accessible Next.js + TypeScript + Tailwind + shadcn/ui + Radix + Lucide, with deliberate loading/error/empty states and versioned trusted build-time plugin integration.
- **Deployment:** frictionless local SQLite and team PostgreSQL; Kubernetes is first-class; no hard Azure/cloud vendor dependency. SignalR for portable realtime.
- **Trust:** in-process .NET plugins are trusted, not sandboxed; do not claim secure arbitrary module or dynamic UI hot loading.
- **Security:** scope every action by actor, team, resource and registered permission across REST/UI/MCP/jobs; audit sensitive changes; never log secrets.

## Spec Kit workflow

The repository's [`drive-speckit` workflow](development/spec-workflow.md) automates
selection and one stage per invocation on the same branch/PR for each feature,
with tracked progress, explicit final acceptance and human merge only at feature
completion. Repository-only setup is preparation for spec 001, not
completion of its working API/UI skeleton. The prompts below remain authoritative.

1. Create the constitution from `constitution-prompt.md`.
2. Place foundation documents in repo `docs/` and tell the coding assistant to treat them as canonical product context.
3. For each numbered spec below, paste the prompt into `/speckit.specify`; review the generated spec's acceptance criteria and unresolved questions before `/speckit.plan`, `/speckit.tasks` and implementation.
4. Keep each spec independently testable, but run integration/release gates regularly. If an implementation decision changes a locked invariant, stop and make an explicit ADR and documentation update.
5. Dependency references are **must-exist interfaces**, not always strict implementation serialization. Begin 007 MCP as soon as identity/permission contracts are usable; add catalog tools incrementally.

## Ordered summary

| ID | Scope | Deliverable | Depends on |
|---|---|---|---|
| 001 | **MVP** | Repository bootstrap and developer experience | None |
| 002 | **MVP** | Platform contracts and module identity | 001 |
| 003 | **MVP** | Storage registry and module isolation | 002 |
| 004 | **MVP** | Module composition and lifecycle | 002,003 |
| 005 | **MVP** | Authentication and bootstrap administration | 001,004 |
| 006 | **MVP** | Teams and centralized authorization | 004,005 |
| 007 | **MVP** | Early MCP server and native discovery tools | 002,005,006; evolve with 011,012 |
| 008 | **MVP** | UI shell and design system | 001,005 |
| 009 | **MVP** | Auditing and observability foundations | 005,006,007 |
| 010 | **MVP** | API conventions and vertical slices | 001,004,006 |
| 011 | **MVP** | Service/project catalog and ownership | 003,006,008,010 |
| 012 | **MVP** | Declarative resources and typed kinds | 011,010 |
| 013 | **MVP** | MCP domain tools and plugin tool SDK | 007,011,012 |
| 014 | **MVP** | Frontend extension SDK | 004,006,008 |
| 015 | **MVP** | Module settings and secrets references | 004,006,008,009 |
| 016 | **MVP** | Module events and reliable side effects | 003,004,010 |
| 017 | **MVP** | Durable jobs and lightweight workflows | 003,006,009,016 |
| 018 | **MVP** | SignalR notifications and operation progress | 008,012,017 |
| 019 | **MVP** | Provider integration SDK | 012,015,017 |
| 020 | **MVP** | Reference module full vertical demonstration | 004,013,014,016,017,019 |
| 021 | **MVP** | Module install, upgrade and compatibility UX | 004,014,015,020 |
| 022 | **MVP** | Local and production deployment profiles | 001,003,017,021 |
| 023 | **MVP** | Security, supply-chain and resilience hardening | all MVP specs |
| 024 | **MVP** | MVP end-to-end acceptance and release | 001–023 |
| 025 | Post-MVP | External MCP servers and federation | 013,023 |
| 026 | Post-MVP | Hoard optional module integration | 020,025 optional |
| 027 | Post-MVP | Source control and CI/CD connectors | 019 |
| 028 | Post-MVP | OpenTofu/Terraform module orchestration | 019,017 |
| 029 | Post-MVP | Module distribution and trust registry | 021,023 |
| 030 | Post-MVP | Additional identity and data providers | 005,003 |
| 031 | Post-MVP | Workflow engine adapters | 017 |
| 032 | Post-MVP | Search, discovery and advanced catalog | 011,012 |
| 033 | Post-MVP | Operational extension management | 021,025,029 |
| 034 | Post-MVP | Multi-organization isolation | 006,032 |
| 035 | Post-MVP | Approvals, policy as code and governance | 012,017,034 |
| 036 | Post-MVP | AI assistant atop MCP | 013,025,035 |
| 037 | Post-MVP | Advanced multi-cluster and HA operations | 022,031 |
| 038 | Post-MVP | Hosted control plane and ecosystem experience | 029,034,037 |

---

## Detailed specs and paste-ready prompts

## MVP-A: Foundation and early feedback

### 001 — Repository bootstrap and developer experience [MVP]

**Purpose / deliverable:** Set up the .NET modular host, Next.js TypeScript web app, shared packages, tests, CI, reproducible local stack, linting, formatting, OpenAPI and health endpoint. Document prerequisites and single-command local start.

**Dependencies:** None.  
**Acceptance focus:** Working API/UI skeleton, SQLite local run, PostgreSQL test profile, green automated build and smoke tests.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 001: Repository bootstrap and developer experience. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Set up the .NET modular host, Next.js TypeScript web app, shared packages, tests, CI, reproducible local stack, linting, formatting, OpenAPI and health endpoint. Document prerequisites and single-command local start.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Working API/UI skeleton, SQLite local run, PostgreSQL test profile, green automated build and smoke tests.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: None. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 002 — Platform contracts and module identity [MVP]

**Purpose / deliverable:** Implement the minimal host contracts, immutable reverse-DNS module IDs, capability/version declarations, compatibility ranges, validation errors and manifest schema.

**Dependencies:** 001.  
**Acceptance focus:** Manifest parser and validator rejects malformed IDs, duplicate capabilities and incompatible host versions with actionable diagnostics.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 002: Platform contracts and module identity. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Implement the minimal host contracts, immutable reverse-DNS module IDs, capability/version declarations, compatibility ranges, validation errors and manifest schema.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Manifest parser and validator rejects malformed IDs, duplicate capabilities and incompatible host versions with actionable diagnostics.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 001. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 003 — Storage registry and module isolation [MVP]

**Purpose / deliverable:** Implement platform-owned registry and module database provisioner: dedicated PostgreSQL named schemas allocated collision-safely, and one SQLite file per module, plus separate platform database/file.

**Dependencies:** 002.  
**Acceptance focus:** Persist mapping, unique constraints, registration concurrency handling, protected reserved names and paths; integration tests on PG and SQLite.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 003: Storage registry and module isolation. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Implement platform-owned registry and module database provisioner: dedicated PostgreSQL named schemas allocated collision-safely, and one SQLite file per module, plus separate platform database/file.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Persist mapping, unique constraints, registration concurrency handling, protected reserved names and paths; integration tests on PG and SQLite.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 002. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 004 — Module composition and lifecycle [MVP]

**Purpose / deliverable:** Support trusted in-process .NET module discovery, dependency ordering/cycle detection, registration, configuration, migrations, enable/disable and health/degraded states.

**Dependencies:** 002,003.  
**Acceptance focus:** No optional module boot, test module boots, invalid/duplicate modules fail closed; startup diagnostics; disabling preserves data.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 004: Module composition and lifecycle. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Support trusted in-process .NET module discovery, dependency ordering/cycle detection, registration, configuration, migrations, enable/disable and health/degraded states.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: No optional module boot, test module boots, invalid/duplicate modules fail closed; startup diagnostics; disabling preserves data.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 002,003. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 005 — Authentication and bootstrap administration [MVP]

**Purpose / deliverable:** Add pluggable OIDC login for production, clearly limited development login, user identity mapping, safe first-admin bootstrap and session/API auth boundaries.

**Dependencies:** 001,004.  
**Acceptance focus:** Anonymous denial, configured OIDC integration test, bootstrap non-repeatability, logged-in UI/API identity consistency.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 005: Authentication and bootstrap administration. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Add pluggable OIDC login for production, clearly limited development login, user identity mapping, safe first-admin bootstrap and session/API auth boundaries.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Anonymous denial, configured OIDC integration test, bootstrap non-repeatability, logged-in UI/API identity consistency.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 001,004. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 006 — Teams and centralized authorization [MVP]

**Purpose / deliverable:** Implement teams, memberships, baseline roles and module-registered granular permissions; resource/team scoped authorization shared across web/API/MCP/jobs.

**Dependencies:** 004,005.  
**Acceptance focus:** Positive/negative scope tests, denial by default, register permission namespaces without collisions, multi-team isolation.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 006: Teams and centralized authorization. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Implement teams, memberships, baseline roles and module-registered granular permissions; resource/team scoped authorization shared across web/API/MCP/jobs.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Positive/negative scope tests, denial by default, register permission namespaces without collisions, multi-team isolation.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 004,005. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 007 — Early MCP server and native discovery tools [MVP]

**Purpose / deliverable:** Expose authenticated MCP endpoint early with health/capabilities and permission-filtered read-only discovery tools (modules, teams or service catalog as available); route execution through application handlers.

**Dependencies:** 002,005,006; evolve with 011,012.  
**Acceptance focus:** MCP inspector/client connects; auth, permission and schema tests; tools versioned and auditable; no alternate business logic.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 007: Early MCP server and native discovery tools. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Expose authenticated MCP endpoint early with health/capabilities and permission-filtered read-only discovery tools (modules, teams or service catalog as available); route execution through application handlers.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: MCP inspector/client connects; auth, permission and schema tests; tools versioned and auditable; no alternate business logic.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 002,005,006; evolve with 011,012. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 008 — UI shell and design system [MVP]

**Purpose / deliverable:** Build responsive Next.js App Router shell with Tailwind, shadcn/ui, accessible Radix primitives, Lucide, layouts, navigation, search affordance, skeletons, error states, dark mode and consistent forms.

**Dependencies:** 001,005.  
**Acceptance focus:** Keyboard/a11y tests, fast route transitions, loading/error/empty states, no mock-only navigation in shipped features.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 008: UI shell and design system. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Build responsive Next.js App Router shell with Tailwind, shadcn/ui, accessible Radix primitives, Lucide, layouts, navigation, search affordance, skeletons, error states, dark mode and consistent forms.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Keyboard/a11y tests, fast route transitions, loading/error/empty states, no mock-only navigation in shipped features.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 001,005. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```


## MVP-B: Core platform capabilities

### 009 — Auditing and observability foundations [MVP]

**Purpose / deliverable:** Add structured logs, OpenTelemetry, correlation IDs, health/readiness, audit envelope recording actor/team/action/target/result/source with retention configuration.

**Dependencies:** 005,006,007.  
**Acceptance focus:** Sensitive values redacted; audit captures denied operations, REST/MCP provenance and module lifecycle; trace crossing boundaries.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 009: Auditing and observability foundations. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Add structured logs, OpenTelemetry, correlation IDs, health/readiness, audit envelope recording actor/team/action/target/result/source with retention configuration.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Sensitive values redacted; audit captures denied operations, REST/MCP provenance and module lifecycle; trace crossing boundaries.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 005,006,007. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 010 — API conventions and vertical slices [MVP]

**Purpose / deliverable:** Codify VSA feature layout: endpoint, handler, request/response, validation, authorization and persistence co-located; .NET tests stored in dedicated test projects; OpenAPI, error codes, pagination, concurrency, idempotency where required.

**Dependencies:** 001,004,006.  
**Acceptance focus:** Sample real feature and contract tests prove uniform API conventions; no MediatR dependency.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 010: API conventions and vertical slices. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Codify VSA feature layout: endpoint, handler, request/response, validation, authorization and persistence co-located; .NET tests stored in dedicated test projects; OpenAPI, error codes, pagination, concurrency, idempotency where required.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Sample real feature and contract tests prove uniform API conventions; no MediatR dependency.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 001,004,006. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 011 — Service/project catalog and ownership [MVP]

**Purpose / deliverable:** Introduce teams, projects and software services, metadata, ownership, search/filter and CRUD with per-team authorization, REST and navigable UI.

**Dependencies:** 003,006,008,010.  
**Acceptance focus:** Cross-team leakage tests; lifecycle and ownership constraints; database migration support in both providers.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 011: Service/project catalog and ownership. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Introduce teams, projects and software services, metadata, ownership, search/filter and CRUD with per-team authorization, REST and navigable UI.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Cross-team leakage tests; lifecycle and ownership constraints; database migration support in both providers.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 003,006,008,010. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 012 — Declarative resources and typed kinds [MVP]

**Purpose / deliverable:** Define versioned resource-kind registration, metadata/spec/status model, desired/observed states, validation, team scope, status transitions and change history.

**Dependencies:** 011,010.  
**Acceptance focus:** Create/read/update/list/reconcile intent through API and UI; optimistic concurrency and version compatibility tests.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 012: Declarative resources and typed kinds. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Define versioned resource-kind registration, metadata/spec/status model, desired/observed states, validation, team scope, status transitions and change history.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Create/read/update/list/reconcile intent through API and UI; optimistic concurrency and version compatibility tests.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 011,010. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 013 — MCP domain tools and plugin tool SDK [MVP]

**Purpose / deliverable:** Extend MCP to search catalog, inspect kinds/resources and run narrowly scoped permitted operations; expose module tool registration with namespace/version, JSON schemas, cancellation and budgets.

**Dependencies:** 007,011,012.  
**Acceptance focus:** Plugin-owned tools appear when enabled; tool name collisions blocked; permissions match HTTP; destructive actions are explicitly gated.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 013: MCP domain tools and plugin tool SDK. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Extend MCP to search catalog, inspect kinds/resources and run narrowly scoped permitted operations; expose module tool registration with namespace/version, JSON schemas, cancellation and budgets.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Plugin-owned tools appear when enabled; tool name collisions blocked; permissions match HTTP; destructive actions are explicitly gated.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 007,011,012. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 014 — Frontend extension SDK [MVP]

**Purpose / deliverable:** Build versioned frontend extension manifest/registry supporting nav, page slots, widgets and forms as trusted build/deployment-time packages, with module enablement and permission filtering.

**Dependencies:** 004,006,008.  
**Acceptance focus:** An independent plugin adds a real routed page and nav entry; disabled plugin removed; missing modules fail gracefully.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 014: Frontend extension SDK. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Build versioned frontend extension manifest/registry supporting nav, page slots, widgets and forms as trusted build/deployment-time packages, with module enablement and permission filtering.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: An independent plugin adds a real routed page and nav entry; disabled plugin removed; missing modules fail gracefully.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 004,006,008. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 015 — Module settings and secrets references [MVP]

**Purpose / deliverable:** Implement typed, validated module configuration (global/team where applicable), secure secret-reference patterns, settings UI and audit without storing raw secrets in logs.

**Dependencies:** 004,006,008,009.  
**Acceptance focus:** Config errors diagnosed, scope enforced, secret values masked, local and container configuration documented.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 015: Module settings and secrets references. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Implement typed, validated module configuration (global/team where applicable), secure secret-reference patterns, settings UI and audit without storing raw secrets in logs.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Config errors diagnosed, scope enforced, secret values masked, local and container configuration documented.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 004,006,008,009. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 016 — Module events and reliable side effects [MVP]

**Purpose / deliverable:** Implement versioned in-process events and durable per-module outbox/inbox for reliability-critical integrations, retries and idempotent handlers; no cross-module table access.

**Dependencies:** 003,004,010.  
**Acceptance focus:** Events survive restart; duplicate delivery harmless; faulty subscriber cannot lose producer transaction; SQLite/PG integration tests.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 016: Module events and reliable side effects. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Implement versioned in-process events and durable per-module outbox/inbox for reliability-critical integrations, retries and idempotent handlers; no cross-module table access.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Events survive restart; duplicate delivery harmless; faulty subscriber cannot lose producer transaction; SQLite/PG integration tests.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 003,004,010. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 017 — Durable jobs and lightweight workflows [MVP]

**Purpose / deliverable:** Implement persisted async jobs, state transitions, retries/backoff, leases, restart recovery, idempotency, cancellation, progress and pluggable module workflow steps; separate worker optional.

**Dependencies:** 003,006,009,016.  
**Acceptance focus:** Crash/restart recovery, concurrent worker test, per-team security, inspect/cancel job API and UI.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 017: Durable jobs and lightweight workflows. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Implement persisted async jobs, state transitions, retries/backoff, leases, restart recovery, idempotency, cancellation, progress and pluggable module workflow steps; separate worker optional.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Crash/restart recovery, concurrent worker test, per-team security, inspect/cancel job API and UI.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 003,006,009,016. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 018 — SignalR notifications and operation progress [MVP]

**Purpose / deliverable:** Add provider-neutral SignalR events for job and resource changes, reconnect/replay fallback to polling, team-filtered subscription and accessible toast/activity surfaces.

**Dependencies:** 008,012,017.  
**Acceptance focus:** Unauthorized team never receives updates; reconnect and polling tests; no Azure-only dependency.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 018: SignalR notifications and operation progress. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Add provider-neutral SignalR events for job and resource changes, reconnect/replay fallback to polling, team-filtered subscription and accessible toast/activity surfaces.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Unauthorized team never receives updates; reconnect and polling tests; no Azure-only dependency.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 008,012,017. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 019 — Provider integration SDK [MVP]

**Purpose / deliverable:** Define versioned provider adapters for external systems, credentials/config contract, capability discovery, execution result and errors, with direct execution and external CI/CD dispatch patterns.

**Dependencies:** 012,015,017.  
**Acceptance focus:** Mock/reference provider works with resource intent + jobs and permissions; no mandatory cloud/IaC runtime.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 019: Provider integration SDK. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Define versioned provider adapters for external systems, credentials/config contract, capability discovery, execution result and errors, with direct execution and external CI/CD dispatch patterns.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Mock/reference provider works with resource intent + jobs and permissions; no mandatory cloud/IaC runtime.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 012,015,017. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 020 — Reference module full vertical demonstration [MVP]

**Purpose / deliverable:** Produce independently packaged sample module with manifest, own DB schema/file, backend routes, UI page, resource kind, permission, events, durable job, provider integration and MCP tool.

**Dependencies:** 004,013,014,016,017,019.  
**Acceptance focus:** A real user installs it, uses UI/REST/MCP, disables/re-enables it, upgrades it; one command runs acceptance suite against PG+SQLite.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 020: Reference module full vertical demonstration. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Produce independently packaged sample module with manifest, own DB schema/file, backend routes, UI page, resource kind, permission, events, durable job, provider integration and MCP tool.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: A real user installs it, uses UI/REST/MCP, disables/re-enables it, upgrades it; one command runs acceptance suite against PG+SQLite.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 004,013,014,016,017,019. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```


## MVP-C: Release, security and deployment

### 021 — Module install, upgrade and compatibility UX [MVP]

**Purpose / deliverable:** Implement admin-facing module inventory, dependency/compatibility diagnostics, install/upgrade/migrate enable/disable operations; package trust is explicit and documented.

**Dependencies:** 004,014,015,020.  
**Acceptance focus:** Safe failure on collisions/version incompatibility; upgrade rehearsal, rollback guidance and migration backup gates.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 021: Module install, upgrade and compatibility UX. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Implement admin-facing module inventory, dependency/compatibility diagnostics, install/upgrade/migrate enable/disable operations; package trust is explicit and documented.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Safe failure on collisions/version incompatibility; upgrade rehearsal, rollback guidance and migration backup gates.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 004,014,015,020. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 022 — Local and production deployment profiles [MVP]

**Purpose / deliverable:** Deliver documented low-friction local install with SQLite and shared/team Postgres profile; OCI images; Kubernetes-first Helm assets, probes, resource settings, persistent volumes and secure configuration.

**Dependencies:** 001,003,017,021.  
**Acceptance focus:** Fresh install smoke tests for local, PG and K8s; safe upgrade and migration runbook.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 022: Local and production deployment profiles. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Deliver documented low-friction local install with SQLite and shared/team Postgres profile; OCI images; Kubernetes-first Helm assets, probes, resource settings, persistent volumes and secure configuration.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Fresh install smoke tests for local, PG and K8s; safe upgrade and migration runbook.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 001,003,017,021. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 023 — Security, supply-chain and resilience hardening [MVP]

**Purpose / deliverable:** Threat model host/plugin trust boundaries, auth/session/CSRF/CORS, MCP SSRF/egress, path traversal, file perms, rate limits, dependency pinning/licensing, backup restore and recovery.

**Dependencies:** all MVP specs.  
**Acceptance focus:** Negative security matrix passes; trusted in-process code caveat explicit; restore test for module-owned stores; CI vulnerability/license checks.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 023: Security, supply-chain and resilience hardening. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Threat model host/plugin trust boundaries, auth/session/CSRF/CORS, MCP SSRF/egress, path traversal, file perms, rate limits, dependency pinning/licensing, backup restore and recovery.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Negative security matrix passes; trusted in-process code caveat explicit; restore test for module-owned stores; CI vulnerability/license checks.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: all MVP specs. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 024 — MVP end-to-end acceptance and release [MVP]

**Purpose / deliverable:** Publish documented MVP acceptance suite, API/MCP/UI contract checks, reference module walk-through, demo data, onboarding docs, versioned artifacts and release checklist.

**Dependencies:** 001–023.  
**Acceptance focus:** All charter MVP requirements verified; one complete end-to-end journey in SQLite and PG, Kubernetes deploy smoke and reliable recovery.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 024: MVP end-to-end acceptance and release. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Publish documented MVP acceptance suite, API/MCP/UI contract checks, reference module walk-through, demo data, onboarding docs, versioned artifacts and release checklist.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: All charter MVP requirements verified; one complete end-to-end journey in SQLite and PG, Kubernetes deploy smoke and reliable recovery.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 001–023. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```


## POST-MVP-1: Ecosystem expansion and integrations

> **MVP RELEASE BOUNDARY — everything below this point is excluded from MVP.**

### 025 — External MCP servers and federation [POST-MVP]

**Purpose / deliverable:** Allow approved plugin-declared external MCP servers with identity propagation or service credentials, namespace collisions, egress allowlist, timeouts, tool policy and provenance.

**Dependencies:** 013,023.  
**Acceptance focus:** Disabled/failing/untrusted servers isolated; no tool impersonation; explicit operator approval.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 025: External MCP servers and federation. Scope: POST-MVP, not required for MVP release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Allow approved plugin-declared external MCP servers with identity propagation or service credentials, namespace collisions, egress allowlist, timeouts, tool policy and provenance.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Disabled/failing/untrusted servers isolated; no tool impersonation; explicit operator approval.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 013,023. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 026 — Hoard optional module integration [POST-MVP]

**Purpose / deliverable:** Package Hoard as opt-in module/external service bridge with catalog discoverability, authenticated routes, module settings and MCP tools when suitable.

**Dependencies:** 020,025 optional.  
**Acceptance focus:** Core Köfte installs without Hoard; Hoard version mismatch does not break host.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 026: Hoard optional module integration. Scope: POST-MVP, not required for MVP release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Package Hoard as opt-in module/external service bridge with catalog discoverability, authenticated routes, module settings and MCP tools when suitable.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Core Köfte installs without Hoard; Hoard version mismatch does not break host.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 020,025 optional. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 027 — Source control and CI/CD connectors [POST-MVP]

**Purpose / deliverable:** Implement GitHub and other repository/build adapters as optional providers, webhooks with replay safety and team-scoped credentials.

**Dependencies:** 019.  
**Acceptance focus:** Demonstrate repo/service linking and CI job run status, clear token scopes.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 027: Source control and CI/CD connectors. Scope: POST-MVP, not required for MVP release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Implement GitHub and other repository/build adapters as optional providers, webhooks with replay safety and team-scoped credentials.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Demonstrate repo/service linking and CI job run status, clear token scopes.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 019. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 028 — OpenTofu/Terraform module orchestration [POST-MVP]

**Purpose / deliverable:** Offer optional IaC provisioner with direct run or delegated pipeline execution; remote backend state owned by configured backend, never assume Köfte relational DB is a safe state store.

**Dependencies:** 019,017.  
**Acceptance focus:** Plan/apply approval, locks, redaction, state recovery, external runner model documented.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 028: OpenTofu/Terraform module orchestration. Scope: POST-MVP, not required for MVP release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Offer optional IaC provisioner with direct run or delegated pipeline execution; remote backend state owned by configured backend, never assume Köfte relational DB is a safe state store.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Plan/apply approval, locks, redaction, state recovery, external runner model documented.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 019,017. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 029 — Module distribution and trust registry [POST-MVP]

**Purpose / deliverable:** Add signed/versioned packages, discovery/index, provenance and policy-based approval with extension compatibility checks.

**Dependencies:** 021,023.  
**Acceptance focus:** Untrusted package cannot silently activate; migration/rollback contracts and reproducible builds.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 029: Module distribution and trust registry. Scope: POST-MVP, not required for MVP release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Add signed/versioned packages, discovery/index, provenance and policy-based approval with extension compatibility checks.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Untrusted package cannot silently activate; migration/rollback contracts and reproducible builds.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 021,023. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 030 — Additional identity and data providers [POST-MVP]

**Purpose / deliverable:** Add selected IdP integrations and optional additional relational providers after migration contract maturity.

**Dependencies:** 005,003.  
**Acceptance focus:** Provider conformance suite and module DB isolation guarantees maintained.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 030: Additional identity and data providers. Scope: POST-MVP, not required for MVP release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Add selected IdP integrations and optional additional relational providers after migration contract maturity.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Provider conformance suite and module DB isolation guarantees maintained.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 005,003. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 031 — Workflow engine adapters [POST-MVP]

**Purpose / deliverable:** Add optional advanced workflow engines (e.g. durable orchestration) behind stable job contracts for long-running/complex flows.

**Dependencies:** 017.  
**Acceptance focus:** Existing workflows still function unchanged, idempotent migration and observability.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 031: Workflow engine adapters. Scope: POST-MVP, not required for MVP release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Add optional advanced workflow engines (e.g. durable orchestration) behind stable job contracts for long-running/complex flows.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Existing workflows still function unchanged, idempotent migration and observability.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 017. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 032 — Search, discovery and advanced catalog [POST-MVP]

**Purpose / deliverable:** Add full-text cross-resource search indexing, catalog relationship graph, dependency views, ownership insights and extensible metadata policies.

**Dependencies:** 011,012.  
**Acceptance focus:** Permission-filtered indexing, accurate freshness behavior, scale/load tests.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 032: Search, discovery and advanced catalog. Scope: POST-MVP, not required for MVP release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Add full-text cross-resource search indexing, catalog relationship graph, dependency views, ownership insights and extensible metadata policies.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Permission-filtered indexing, accurate freshness behavior, scale/load tests.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 011,012. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 033 — Operational extension management [POST-MVP]

**Purpose / deliverable:** Runtime remote connector configuration, richer module diagnostics, operator policies and safe rollback tooling; arbitrary frontend hot-loading remains separately reviewed.

**Dependencies:** 021,025,029.  
**Acceptance focus:** Failure-isolated external connectors and auditable admin workflow.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 033: Operational extension management. Scope: POST-MVP, not required for MVP release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Runtime remote connector configuration, richer module diagnostics, operator policies and safe rollback tooling; arbitrary frontend hot-loading remains separately reviewed.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Failure-isolated external connectors and auditable admin workflow.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 021,025,029. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```


## POST-MVP-2: Enterprise reach and intelligent experiences

### 034 — Multi-organization isolation [POST-MVP]

**Purpose / deliverable:** Extend team-first model with explicit organizations, tenant isolation, identity mapping, resource and permission migration.

**Dependencies:** 006,032.  
**Acceptance focus:** Automated tenant boundary security tests and migration plan from single org.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 034: Multi-organization isolation. Scope: POST-MVP, not required for MVP release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Extend team-first model with explicit organizations, tenant isolation, identity mapping, resource and permission migration.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Automated tenant boundary security tests and migration plan from single org.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 006,032. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 035 — Approvals, policy as code and governance [POST-MVP]

**Purpose / deliverable:** Support policy checks, change approvals, environment guardrails, catalog scorecards and auditable exceptions.

**Dependencies:** 012,017,034.  
**Acceptance focus:** Deny/allow test matrix across UI, API, MCP, workflows, providers.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 035: Approvals, policy as code and governance. Scope: POST-MVP, not required for MVP release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Support policy checks, change approvals, environment guardrails, catalog scorecards and auditable exceptions.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Deny/allow test matrix across UI, API, MCP, workflows, providers.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 012,017,034. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 036 — AI assistant atop MCP [POST-MVP]

**Purpose / deliverable:** Introduce optional interactive assistant orchestrating permissioned MCP tools, human approvals and evaluation/observability, with no privileged backdoor.

**Dependencies:** 013,025,035.  
**Acceptance focus:** Tool-use audit, prompt-injection defenses, cross-team scope tests and human approval for high-impact actions.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 036: AI assistant atop MCP. Scope: POST-MVP, not required for MVP release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Introduce optional interactive assistant orchestrating permissioned MCP tools, human approvals and evaluation/observability, with no privileged backdoor.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Tool-use audit, prompt-injection defenses, cross-team scope tests and human approval for high-impact actions.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 013,025,035. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 037 — Advanced multi-cluster and HA operations [POST-MVP]

**Purpose / deliverable:** Add scale-out worker scheduling, external broker drivers, disaster recovery patterns, advanced K8s multi-cluster and multi-region profiles.

**Dependencies:** 022,031.  
**Acceptance focus:** Chaos/failover tests and explicit consistency/SLO contracts.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 037: Advanced multi-cluster and HA operations. Scope: POST-MVP, not required for MVP release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Add scale-out worker scheduling, external broker drivers, disaster recovery patterns, advanced K8s multi-cluster and multi-region profiles.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Chaos/failover tests and explicit consistency/SLO contracts.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 022,031. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

### 038 — Hosted control plane and ecosystem experience [POST-MVP]

**Purpose / deliverable:** Explore optional managed hosting, organization billing/quotas and community marketplace only after self-hosted portability proves durable.

**Dependencies:** 029,034,037.  
**Acceptance focus:** Self-host deployment remains fully supported; no SaaS dependency in host core.

**Paste into `/speckit.specify`:**

```text
Specify Köfte feature 038: Hosted control plane and ecosystem experience. Scope: POST-MVP, not required for MVP release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Explore optional managed hosting, organization billing/quotas and community marketplace only after self-hosted portability proves durable.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Self-host deployment remains fully supported; no SaaS dependency in host core.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 029,034,037. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

## MVP release gates / definition of done

**Release must prove all of the following** (not merely document them):

1. Fresh local install operates using **separate SQLite database files** for platform and every enabled module.
2. Fresh team install on PostgreSQL allocates **distinct named module schemas** with persisted collision-safe registry, migration tables pinned correctly and concurrent registration test coverage.
3. A developer can register, validate, enable, upgrade and disable a trusted module; dependencies and permission collisions produce actionable failures.
4. Authorized users can create/manage teams, projects, services and typed declarative resources using responsive accessible UI and REST; unauthorized cross-team requests are rejected.
5. A real MCP client authenticates, discovers authorized core/module tools, performs an authorized action and receives denied results for out-of-scope actions; no independent bypass implementation.
6. A standalone reference plugin delivers backend slice, UI extension, resource kind, provider, event, persisted job, registered permission and MCP tool; works under both DB profiles.
7. Workflows/jobs survive restart, retry safely and emit team-scoped progress over SignalR with a non-realtime fallback.
8. Audit logs and tracing let an operator connect UI/API/MCP action to resulting module work without exposing secrets.
9. Kubernetes deployment and low-friction non-K8s install both work with documented secrets, storage, backups, diagnostics and upgrades.
10. CI checks unit/integration/contract/E2E, schema collision/path traversal, auth scope, MCP abuse and dependency/license posture.

## Non-goals kept outside MVP

External MCP federation, Hoard integration, commercial module marketplace, arbitrary remote React hot-loading, full multi-organization tenancy, embedded AI assistant, elaborate visual workflow designer, production OpenTofu provisioner, additional database engines, and hosted billing/control-plane operations.

## Implementation notes / unresolved ADR prompts

Record separate ADRs for schema-name allocation hash size, module binary packaging and trust, frontend build-time extension discovery, MCP transport and login model, module migration ordering and failure recovery, outbox/inbox placement, worker concurrency, provider dispatch, package signing and data backup consistency. An ADR may refine implementation but **must not erase a product invariant** without explicit product-doc change.
