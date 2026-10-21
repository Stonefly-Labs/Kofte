# Köfte — Spec Kit Constitution Prompt

> **How to use:** Commit `docs/product-charter.md`, `docs/product-vision.md`, `docs/architecture.md`, and `docs/master-spec-inventory.md`; then paste the entire block below into `/speckit.constitution`. This file is a **prompt to generate/update the constitution**, not itself the generated `.specify/memory/constitution.md`.

```text
Create or update the Köfte project constitution for GitHub Spec Kit. Before writing, read and reconcile these canonical repository documents:
- docs/product-charter.md (product mandate and MVP scope)
- docs/product-vision.md (audience, outcomes and experience)
- docs/architecture.md (architecture invariants and recommendations)
- docs/master-spec-inventory.md (feature rollout sequence and exact MVP boundary: specs 001–024)

Write a concise but enforceable constitution with numbered, testable, non-negotiable principles and a governance/amendment process. Explain what is REQUIRED versus a recommended technology choice and use normative MUST / MUST NOT / SHOULD. Include objective review gates for specifications, plans, tasks, PRs, migrations and releases. Do not merely repeat the docs or turn the constitution into an enormous product specification. All implementation decisions must be traceable and contradictions must be surfaced, not silently resolved.

Mandate these principles:

1. PRODUCT AND BRAND: The product is Köfte, an MIT-licensed, composable, portable, self-hostable open-source internal developer platform. Always write Köfte with its umlaut in human-visible naming; use kofte in technical IDs/repo/CLI identifiers. The platform hosts optional modules; Hoard is an optional future integration, never a required core dependency.

2. MODULAR MONOLITH / LEVEL-3 EXTENSIBILITY: The initial ASP.NET Core/.NET host is modular, with stable versioned plugin/module contracts, immutable globally namespaced module IDs, manifest validation, dependency checks, safe lifecycle, version compatibility and collision rejection. Extensions can supply backend features, frontend registration, typed resources/providers, events, workflow steps, permissions and MCP tools. In-process .NET plugin code is trusted, not securely sandboxed; do not promise arbitrary code isolation, hot-unload or remote frontend code execution.

3. VERTICAL SLICE ARCHITECTURE: Place feature handlers, endpoints, validation, authorization and tests close to each feature within module boundaries; keep truly common contracts in minimal shared platform packages. Avoid ceremonial layers and unnecessary abstraction. Do not use MediatR or dependencies with unacceptable licensing or unexpected commercial charges. Prefer native .NET DI/direct handler invocation and independently testable slices.

4. DATABASE OWNERSHIP (HARD INVARIANT): PostgreSQL requires one dedicated named schema per module, separate host-owned platform schema, and each module's owned EF Core DbContext and migration history mapped to its schema. Allocate SQL-safe names via immutable module ID -> physical schema registry, enforce PostgreSQL 63-byte identifier limit, reserved-name rules, persisted mapping, database UNIQUE constraint, and concurrent registration locking/conflict handling. No module tables in public or another module schema. SQLite requires ONE SEPARATE PHYSICAL DATABASE FILE PER MODULE and separate platform file; registry resolves safe canonical paths, no user-controlled traversal. No shared SQLite module DB, fake schemas or ATTACH-based cross-module ownership. Explicitly test both database providers and migration paths.

5. TRANSACTIONAL BOUNDARIES: Module-local atomic transactions only; no implicit ACID across modules or SQLite files. No direct cross-module tables or joins as extension contracts. Use versioned module APIs, durable outbox/inbox as needed, idempotent handlers, retries, and compensation/reconciliation for workflows.

6. SECURITY AND IDENTITY: Pluggable authentication (OIDC for production); central deny-by-default authorization with actor/team/resource scope and registered permission names. Enforce identical authorization in UI-facing REST, MCP, jobs, providers and realtime subscriptions. First-admin bootstrap is explicit and safe. Validate all inputs, limit secrets exposure, audit sensitive changes and failed authorization, and protect external integrations against SSRF/egress and supply-chain risks.

7. MCP FIRST CLASS AND EARLY: Deliver a functioning authenticated MCP server early in MVP with discover/read tools; evolve it as core features emerge. Modules may register versioned tools with namespaced identity, structured input/output and conflict validation. MCP must invoke the SAME domain operations and authorization used by APIs, and may not be a privileged bypass. External MCP federation and embedded AI chat are POST-MVP.

8. RESOURCE AND JOB MODEL: Combine conventional teams/projects/services with versioned declarative resource kinds and desired/observed status. Lightweight persisted jobs/workflow abstraction must provide durable state, idempotency, retries, cancellation, observability and restart recovery; optional infrastructure engines remain provider modules. OpenTofu or any cloud provider is NOT a mandatory core dependency. SignalR provides provider-neutral realtime with resilient reconnection/polling fallback.

9. FRONTEND EXPERIENCE: Build a fast, responsive, attractive, accessible Next.js/React/TypeScript UI using a consistent design system (Tailwind, shadcn/ui, Radix primitives, Lucide) and deliberate loading, empty, failure and permission states. Support versioned trusted build/deployment-time plugin page/nav/widget extension points at MVP; reject any claim that arbitrary remote JS plugins are safely hot-loadable without separate security design. Keep UI and REST/MCP capability consistent.

10. DEPLOYABILITY AND PORTABILITY: Make both low-friction local/self-hosted SQLite and shared PostgreSQL deployments viable, with Kubernetes as a first-class target. No unconditional Azure or other vendor dependency. Require reproducible dev environments, documented config/secret sources, backup/restore guidance, health/readiness checks, structured logs, OpenTelemetry, versioned interfaces, upgrade/migration safety, and CI validation.

11. TESTING AS A RELEASE GATE: Unit tests for core invariants; integration tests on real PostgreSQL and SQLite; contract tests for module, REST, events and MCP; Playwright E2E for key journeys; security tests for cross-team leaks, schema and module ID collisions, SQLite path traversal, denied tool execution, and restart-safe jobs. The MVP cannot ship without a separate reference module proving backend + frontend + DB isolation + typed resource + event + workflow/job + permission + provider + MCP end to end and without green deployment smoke tests.

12. SCOPE DISCIPLINE: The MVP is exactly the capabilities represented by specs 001–024 in docs/master-spec-inventory.md; specs 025+ are later phases. New post-MVP work cannot become a required MVP dependency casually. Distinguish locked decisions from architecture proposals; track unresolved tradeoffs in ADRs and ensure specs declare dependencies, actor-visible behavior, permissions, data boundaries, errors, acceptance tests and rollback/migration implications where applicable.

GOVERNANCE: Specify how amendments are proposed, reviewed and versioned; what constitutes a breaking constitutional change; how to reconcile conflicts with charter, vision, architecture and roadmap; and what PR/release checklists must verify. Use explicit version, adoption date, last amended date, and change history. If initial adoption cannot be dated automatically, use today's repository working date, not invented history. Ensure templates for /speckit.specify, /speckit.plan and /speckit.tasks consistently incorporate these invariants. Name any conflicts/questions for human resolution instead of changing locked architectural decisions.
```

## Following the constitution

Generate `001` first. Begin `007` early as soon as 002/005/006 establish stable interfaces; iterate its native tool set after specs 011–013. Never treat an isolated MCP hello-world as proof that plugin tool isolation and permission enforcement work.
