# Köfte Constitution

## Core Principles

### I. Product and Brand

Köfte MUST remain an MIT-licensed, composable, portable, self-hostable open-source
internal developer platform. Human-visible names MUST use **Köfte**; technical
identifiers MUST use `kofte`. The host MUST work without optional modules.
Hoard MUST remain an optional future integration, never a core dependency.

### II. Modular Monolith and Level-3 Extensibility

The initial ASP.NET Core/.NET host MUST be a modular monolith with stable,
versioned module contracts. Modules MUST declare immutable globally namespaced
IDs, versions, host compatibility, dependencies and capabilities. Before enabling
a module, the host MUST validate its manifest, dependency order/cycles, storage
mapping and contribution collisions, with actionable failure diagnostics.
Lifecycle operations MUST preserve data on disable and expose failure/degraded states.

Contracts MUST support backend features, frontend registration, typed resources,
providers, events, workflow steps, permissions and MCP tools. In-process .NET
plugins are trusted code, not a security sandbox; the product MUST NOT promise
hostile-code isolation, hot-unload or arbitrary remote frontend execution.

### III. Module-Local Vertical Slices

Handlers, endpoints, validation and authorization MUST stay close to their
feature within module boundaries. .NET test source MUST live in dedicated .NET
test projects, never in production projects or linked from production slices;
test-project folders SHOULD mirror the tested module/feature ownership.
Shared platform packages MUST contain only
demonstrably common contracts or cross-cutting concerns. Slices MUST be independently
testable and use native .NET DI/direct handlers, not MediatR or AutoMapper.
Ceremonial layers and speculative abstractions MUST NOT be introduced.
Dependencies MUST undergo direct/transitive license review and MUST NOT impose
unacceptable license obligations or unexpected commercial charges.

### IV. Module-Owned Storage

PostgreSQL MUST use a dedicated named schema for each module and a separate
host-owned platform schema. Each module MUST own its EF Core DbContext and
migrations, including schema-local migration history. Module tables MUST NOT
reside in `public` or another module's schema.

A platform-owned registry MUST persist immutable module-ID-to-schema mappings.
Allocation MUST enforce SQL-safe normalized names, PostgreSQL's **63-byte**
identifier limit, reserved-name rules, a database UNIQUE constraint and concurrent
registration locking or transactional conflict handling. Hashes alone are not
collision protection. Collisions MUST fail closed; existing mappings MUST NOT
be silently renamed or reused. Reserved names MUST include `public`,
`information_schema`, `pg_*`, `kofte_*` and other platform-owned names.

SQLite MUST use **one separate physical database file per module** and a separate
platform file. The registry MUST resolve canonical paths under a restricted data
root and reject traversal or escape; untrusted paths MUST NOT select storage.
Shared module databases, simulated schemas and `ATTACH`-based cross-module
ownership MUST NOT substitute for file isolation. Both providers and their
migration paths MUST have explicit integration evidence.
These are ownership boundaries, not a sandbox against trusted code sharing
privileged database credentials or filesystem access.

### V. Honest Transaction Boundaries

Atomic transactions MUST remain module-local. Cross-module tables, joins, shared
connections and implied ACID across modules or SQLite files MUST NOT be extension
contracts. Cross-module coordination MUST use versioned APIs/events with explicit
failure semantics. Durable side effects MUST use outbox/inbox or a reliable job
mechanism as needed, idempotent handlers, bounded retries and compensation or
reconciliation. Delivery guarantees MUST describe duplicates/replay honestly;
the platform MUST NOT promise exactly-once effects without proof.

### VI. Identity, Authorization and Integration Safety

Authentication MUST be pluggable, with OIDC for production and explicitly limited
development identity. First-admin bootstrap MUST be explicit, safe and
non-repeatable. Central authorization MUST deny by default and evaluate actor,
registered permission and team/resource scope at every operation.
REST/UI, MCP, jobs, providers and realtime subscriptions MUST enforce the same
policy; hiding UI controls is not authorization.

Inputs MUST be validated, secrets referenced/redacted rather than exposed, and
sensitive changes and failed authorization audited. External integrations MUST
apply SSRF/egress controls, least-privilege credentials, timeouts and request
budgets. Dependency/module adoption MUST assess provenance and supply-chain risk.
Discovery, caches, logs and notifications MUST NOT leak cross-team data.

### VII. Early, First-Class MCP

Spec 007 MUST deliver a functioning authenticated MCP server with discovery/read
tools; domain tools MUST evolve with the catalog/resources through spec 013.
Module tools MUST have namespaced stable identity, versioned contracts, structured
input/output, registered permissions and collision/compatibility validation.
MCP MUST invoke the same domain operations and authorization as REST/UI, authorize
again on invocation, and produce correlated audit/telemetry. Destructive tools
MUST declare and enforce a safety policy, not rely on descriptive text.
External MCP federation and embedded AI chat MUST remain post-MVP.

### VIII. Resources, Durable Work and Realtime

The domain MUST combine conventional teams/projects/services with versioned
declarative resource kinds and separate desired/observed status. Clients MUST NOT
forge controller-owned status. Provider contracts MUST permit optional direct
execution or delegated CI/CD without making OpenTofu, another IaC engine or any
cloud provider mandatory.

Jobs/workflows MUST persist ownership, initiating identity, versioned inputs,
state, attempts and correlation; expose sanitized results/errors; and support
idempotency, bounded retries, cancellation, observability and restart recovery.
Durable events MUST declare delivery/replay semantics. SignalR MUST provide
provider-neutral realtime with scoped subscriptions, resilient reconnection and
HTTP fetch/poll fallback.

### IX. A Consistent, Accessible Frontend

The UI MUST use Next.js, React and TypeScript with a consistent Tailwind,
shadcn/ui, Radix and Lucide design system. Applicable journeys MUST be responsive,
keyboard-accessible and specify measurable performance/accessibility acceptance
criteria plus deliberate loading, empty, failure and permission states.
UI, REST and MCP MUST remain consistent for applicable capabilities.

MVP frontend page/navigation/widget extensions MUST use versioned, trusted
build/deployment-time registration. Arbitrary remotely hot-loaded JavaScript
MUST NOT be presented as safe without a separately reviewed security,
CSP and compatibility design.

### X. Deployability and Portability

Local/self-hosted SQLite and shared PostgreSQL deployments MUST both be viable;
Kubernetes MUST be a first-class target, not a local prerequisite. Azure, Hoard,
cloud-managed services, brokers and external workflow engines MUST NOT be
unconditional core dependencies.

Delivery MUST include reproducible development environments, pinned supported
dependencies, documented configuration/secret sources, health/readiness probes,
structured logs, OpenTelemetry, backup/restore guidance, versioned interfaces,
safe upgrades/migrations and CI validation. Operators MUST receive explicit
persistence, TLS, authentication and recovery responsibilities.

### XI. Evidence Before Release

Applicable invariants MUST have unit tests; storage and migrations MUST be tested
on real PostgreSQL and SQLite, not substitutes. Module, REST, event and MCP
contracts MUST have contract tests, and key journeys MUST have Playwright E2E
tests. Negative/security coverage MUST include cross-team leakage, module-ID and
schema collisions/concurrent registration, SQLite path traversal, denied tool
execution and restart-safe jobs. Changes MUST add focused regression evidence
for the behavior they introduce or fix.

MVP release MUST be blocked without a separately packaged reference module proving
backend, frontend, database isolation on both providers, typed resource, event,
durable workflow/job, permission, provider and MCP end to end. Deployment smoke
tests for the documented local and Kubernetes profiles MUST be green.
Unavailable checks MUST be reported as unavailable, never as passing evidence.

### XII. Scope and Traceability

MVP is exactly specs **001-024**, including the spec 024 release gate.
Specs **025-038**, Hoard integration, external MCP federation and embedded AI chat
are post-MVP and MUST NOT become required MVP dependencies without an explicit
human-approved scope amendment. Repository setup or a passing individual slice
MUST NOT be called MVP completion.

Every decision MUST trace to approved intent. Specs MUST declare dependencies,
actors, observable behavior, applicable interfaces, permissions, data ownership,
errors, acceptance tests and migration/rollback effects, or explain inapplicability.
Locked requirements MUST be distinguished from proposals. Consequential choices
and unresolved tradeoffs MUST be recorded in ADRs, not silently resolved.

## Required Choices and Open Decisions

MUST and MUST NOT are binding. SHOULD denotes a recommendation whose rejection
requires documented rationale and evidence; it never weakens a MUST.
The principles above are requirements, including the prompt-mandated technology
baseline. Exact versions, allocator algorithm/hash length, module packaging,
MCP SDK/transport/auth details, initial OIDC providers, migration/rollback policy,
job persistence implementation and frontend bundling mechanism remain design
decisions for their numbered specs and ADRs.

The architecture's proposed TanStack Query/form libraries, validation library,
Aspire versus Compose, test framework, deployment templating and optional brokers
are not adopted merely by appearing in a stack table. Implementers SHOULD prefer
supported stable versions and the smallest design that satisfies the invariants.

The inventory's explicit MVP boundary controls the charter's broader mentions of
external MCP and illustrative Hoard hosting: neither requires a working MVP
integration. The older stack exploration has no authority to introduce these
dependencies, cross-module atomicity or a competing repository layout.
Any actual conflict with canonical intent MUST follow Governance below.

## Delivery and Review Gates

Every gate MUST record evidence, applicable principle IDs and a reason for each
inapplicable concern; unchecked assertions are not evidence.

| Gate | Required evidence before progression |
| --- | --- |
| Specification | Inventory ID/scope/dependencies; actor journeys and Given/When/Then acceptance; core/module ownership; interface, permission, storage, failure, recovery and operational requirements; explicit tests; unresolved material questions answered. |
| Plan | Constitution check before research and after design; requirement-to-design mapping; versioned contracts; provider/migration and trust boundaries; licensing/ADRs; validation and rollback strategy. A complexity note cannot waive a MUST. |
| Checklist and tasks | Human-reviewed requirements-quality checklist; dependency-ordered, path-specific tasks tracing to requirements/stories and applicable principles; focused positive/negative, both-provider and interface-parity validation tasks. Mandatory tests are not optional template examples. |
| Implementation PR | Fresh typed branch from updated main for a new feature; all its stages continue on that same branch/draft PR. Focused diff and updated README/related docs; exact commands/results; acceptance and constitutional evidence; no secrets/local artifacts; ledger and stage artifacts together; human review/merge only after feature completion. |
| Migration | Proven registry/ownership isolation and schema-local history or per-file mapping; installation lock/dependency order; both-provider clean-install and upgrade tests; collision/failure diagnostics; backup/restore and rollback or forward-recovery evidence. |
| Release | Specs 001-024 explicitly accepted; passing reference-module, contract, authorization, real-provider integration, Playwright, restart/recovery and deployment smoke evidence; license/security review and documented install/upgrade/backup/restore. |

Each changed stage MUST run `python3 scripts/check_repository.py`,
`python3 -m unittest discover -s scripts/tests -v` and `git diff --check`,
plus the smallest applicable product test/lint/format/type/build commands once
that tooling exists. Documentation-only stages do not fabricate product checks.

Delivery MUST follow one recorded Spec Kit stage per invocation, with its
artifacts and ledger published together on **one branch and one PR per numbered
feature**, from specify through acceptance. Intermediate stages MUST NOT require
a merge or a new branch/PR. Every commit MUST be pushed immediately; the feature
PR MUST remain draft until passing final review and explicit human acceptance.
Only then may the assistant mark it ready and request human merge. The next
numbered feature MUST wait for that merge and start from updated main.
Assistants MUST NOT merge, close, enable auto-merge or delete PR branches.
Final feature acceptance MUST quote an explicit human decision; merge, silence,
autopilot and checked tasks are not acceptance. The standalone constitution task
retains its own human merge before the first numbered feature.

## Governance

The human-merged v1.0.0 established this constitution. Amendments become the shared
baseline on human merge. The ratification date below remains the initial
repository working date, not invented prior history.

Amendments MUST be proposed in a focused PR identifying the reason, affected
principles, canonical sources, ADR, compatibility/migration impact and validation.
Reviewers MUST check affected specs, plans, tasks, project template overrides,
instructions and release gates. Human approval and merge are required before
dependent work proceeds. For a workflow-only correction, an explicit human
direction may govern the active feature branch while its recorded amendment
awaits merge; this MUST NOT waive product/security principles or feature
acceptance. Assistants MUST NOT waive rules or self-accept changes.

Constitution versions MUST use semantic versioning: MAJOR for removal,
incompatible redefinition or weakening of a principle/guarantee; MINOR for
compatible new principles or materially expanded obligations; PATCH for
non-semantic clarification. Amendments MUST retain the original ratification date,
update Last Amended and append version/date/rationale to the change history.

The charter supplies mandate, vision supplies outcomes, architecture distinguishes
requirements from recommendations, and inventory supplies sequence/exact MVP scope.
If these sources, a spec or this constitution conflict, the author MUST identify
the exact conflict, stop affected progression and request a human decision.
Resolution MUST record an ADR and explicitly amend every affected canonical
document and this constitution where needed; neither an ADR nor a template alone
overrides a locked rule. Unaffected work may proceed only within its approved stage.

Change history:

- **1.0.0 - 2026-10-20:** Initial constitution from the exact canonical prompt;
  replaces an unadopted scaffold with twelve principles and objective delivery gates.
- **2.0.0 - 2026-10-20:** Human-directed workflow correction: keep one branch/PR
  through all stages of a numbered feature, removing intermediate merge gates.
  Preserve one stage per invocation, human quality/acceptance decisions and
  merge before the next feature. Major version reflects removal of a mandatory
  delivery gate; product principles are unchanged. See
  [ADR 0001](../../docs/adr/0001-feature-delivery-lifecycle.md).
- **2.0.1 - 2026-10-21:** Explicit human clarification of .NET test source
  ownership: source lives with its test project, not in production slices.
  Logical module-local coverage and mandatory tests are unchanged. See
  [ADR 0003](../../docs/adr/0003-dotnet-test-source-ownership.md).

**Version**: 2.0.1 | **Ratified**: 2026-10-20 | **Last Amended**: 2026-10-21
