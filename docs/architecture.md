# Köfte — Technical Architecture

> **Status:** Proposed implementation architecture v0.1  
> **Date:** 2026-10-20  
> **Normative words:** **MUST** = agreed requirement; **SHOULD** = recommended design pending implementation validation; **MAY** = optional.  
> **Naming:** **Köfte** in human-facing UI/docs, `kofte` in technical identifiers.

## 1. Architecture goals

- A portable self-hosted **modular monolith** with deeply extensible module boundaries and paths to external providers/processes.
- **Vertical Slice Architecture (VSA)** for feature implementation inside the host and individual modules; isolate truly shared primitives and contracts.
- An early, secure **MCP server** that reuses domain operations and authorization rather than creating an alternate logic path.
- Minimal viable infrastructure for local/small users; PostgreSQL/Kubernetes-grade operations for team deployments.
- Module-owned data and migrations without name collisions or implied distributed transactions.
- A snappy, consistent, accessible UI with a documented extension model.

## 2. Context / logical components

```mermaid
flowchart TB
    Dev[Developer / Platform Admin]
    IDE[IDE Assistant / MCP Client]
    UI[Next.js Web Console]
    Host[ASP.NET Core Köfte Host API]
    Auth[AuthN Provider + Köfte AuthZ]
    Catalog[Core Teams / Projects / Services / Resources]
    Mod[Registered Feature Modules]
    Jobs[Durable Job / Workflow Runtime]
    MCP[MCP Endpoint / Tool Registry]
    Data[(Module-owned Storage)]
    Outside[External Providers / CI-CD / MCP Servers]
    Dev --> UI --> Host
    IDE --> MCP --> Host
    Host --> Auth
    Host --> Catalog
    Host --> Mod
    Host --> Jobs
    Catalog --> Data
    Mod --> Data
    Jobs --> Data
    Mod --> Outside
    MCP --> Outside
```

The diagram is conceptual: MCP may be hosted in the same ASP.NET Core process; the jobs executor may start in that process or run as a separate worker using shared job contracts. The Next.js server and .NET API are separate runtime components.

## 3. Initial technology stack

The following is a **proposed stack** for implementation, not a claim that every package is already selected. Prefer current supported stable versions during repository initialization, pin versions, and confirm licensing before adoption.

| Layer | Proposed choice | Purpose / rationale |
|---|---|---|
| Core API | ASP.NET Core (.NET current supported LTS), C# | Mature hosting, middleware, DI, testing, performance |
| Application style | Modular monolith, VSA, minimal endpoints/route groups | Independent slices without needless mediator abstraction |
| Frontend | Next.js App Router, React, TypeScript | Polished portal, SSR/server components where useful |
| UI components | Tailwind CSS, shadcn/ui, Radix primitives, Lucide icons | Fast, accessible, customizable visual system |
| Frontend state/data | TanStack Query for interactive server state; Zod + React Hook Form for forms | Optimistic UX, validation, consistent caching |
| Data | EF Core with Npgsql and SQLite providers | Managed relational models/migrations, flexible deployment |
| PostgreSQL | Database with one dedicated **named schema per module** | Ownership/isolation, migrations, permissions |
| SQLite | One **database file per module** | Small installs with physically separated data |
| Realtime | ASP.NET Core SignalR | Provider-neutral push to UI; avoid mandatory Azure Web PubSub |
| Jobs | Custom minimal persisted job abstractions built on hosted workers; evaluate library later | Durable states/retries without forcing heavy orchestrator |
| Events | In-process pub/sub abstraction, durable outbox when side effects require it | Decoupled modules, future transport flexibility |
| MCP | .NET MCP server SDK (verify stable package/API) | Tools and resource discovery for assistants |
| AuthN | ASP.NET Core authentication integration + pluggable OIDC | No required identity vendor; optional local/dev auth |
| AuthZ | Central RBAC/permission service + resource/team scope | Same policy for UI/API/jobs/MCP |
| Validation | FluentValidation or equivalent permissively licensed validation | Validation local to each vertical slice |
| Telemetry | OpenTelemetry, structured logging, health checks | Vendor-neutral traces, logs, metrics |
| API contract | OpenAPI/JSON Schema; typed client generation optional | Stable external/SDK/module experience |
| Local orchestration | .NET Aspire or Docker Compose (assess lowest-friction path) | Reproducible multi-service development |
| Container/deploy | OCI images, Helm/Kustomize templates for Kubernetes | First-class portable K8s operation |
| CI / testing | GitHub Actions, xUnit or equivalent, Playwright, Testcontainers | Contract, integration and UI verification |

**Dependency rule:** Do not introduce MediatR or commercially encumbered mediator/framework dependencies by default. Ordinary DI, direct handlers and small in-house dispatch contracts are sufficient. All dependency licenses should be reviewed in CI.

Spec 001's [human-approved bootstrap design](adr/0002-bootstrap-development-baseline.md)
and [plan](../specs/001-repository-bootstrap/plan.md) narrow these proposals for
the foundation: native local host/web processes, npm workspaces, SQLite and
disposable PostgreSQL verification, with no application tables or empty EF model.
Module-owned EF contexts/migrations remain mandatory when modules arrive.
The bootstrap's native host/web, SQLite store, disposable PostgreSQL fixtures,
guarded recovery and generated Development contract are now implemented;
see the [local guide](development/local-development.md) and
[actual evidence](../specs/001-repository-bootstrap/implementation.md).
Both supported targets have automated evidence; the human has confirmed the
manual/reference tasks. All 64 implementation tasks and the final local/CI
verification are evidenced.
[Convergence](../specs/001-repository-bootstrap/convergence.md) found no
actionable spec/plan/task/constitutional gaps and left the task list unchanged.
[Final review](../specs/001-repository-bootstrap/review.md) also passes:
independent code/test assessment found no acceptance defect, and a fresh complete
local gate passes all 170 named steps with assessed-head CI parity.
[Explicit human acceptance](../specs/001-repository-bootstrap/acceptance.md)
now completes spec 001's agreed bootstrap scope, retaining the documented
limitations. Its original PR is now human-merged with accepted evidence on main.
Spec 002 has a [draft requirements specification](../specs/002-platform-contracts/spec.md)
for minimal platform contracts and module identity; clarification is complete
with no material ambiguity or spec changes. Its [plan](../specs/002-platform-contracts/plan.md)
now proposes a minimal shared library, strict JSON/schema and bounded pure
validation with explicit SemVer compatibility. [ADR 0004](adr/0004-module-manifest-contract.md)
records the human-approved design choices.
Its [quality review](../specs/002-platform-contracts/checklists/quality.md)
now satisfies all 36 requirements-quality criteria after explicit reviewer
authorization, with no blocking gap. The subsequent explicit ADR 0004 approval
resolves that design gate; subsequent evidence and explicit requirement
amendments are tracked below. The [48 tasks](../specs/002-platform-contracts/tasks.md)
now order identity and compatibility components before the complete public
validation path, then safe correction and cross-cutting evidence. All requirements,
outcomes and scenarios are mapped. The read-only
[analysis](../specs/002-platform-contracts/analysis.md) passes with concrete coverage
of all 21 requirements/outcomes and 14 scenarios, no unmapped task and no
actionable or constitutional finding at its analyzed checkpoint.
The current [implementation](../specs/002-platform-contracts/implementation.md)
adds the pure platform library and test-owned author/host-facing consumers,
not a module loader or storage registry. Its
[author guide](../specs/002-platform-contracts/quickstart.md) is executable.
The human-directed ADR amendment accepts actual available-workstation/CI timing,
not a fixed Ubuntu/CPU/RAM profile. The subsequent author-guide amendment replaces
the manual timed exercise with automated guide/correction/retry proof.
The [T049 implementation increment](../specs/002-platform-contracts/implementation.md#convergence-remediation-t049)
closes the prior [convergence](../specs/002-platform-contracts/convergence.md)
public/repeated-corpus evidence gap: documented boundaries, ordered errors and
truncation now run 100 times per consumer per real culture. No parser defect
was exposed, and no production code, schema or architecture decision changed.
Follow-up [convergence](../specs/002-platform-contracts/convergence.md) now
passes with zero actionable gaps after assessing all 35 requirements/outcomes/
scenarios, twelve plan decisions, all 49 tasks and twelve principles. No task,
product code or architecture decision changes in that stage. The independent
[final review](../specs/002-platform-contracts/review.md) now also passes with
source/test evidence for all 35 requirements/outcomes/scenarios, twelve plan
decisions, 49 tasks and twelve principles, and no acceptance defect.
Fresh quality/unit/both-provider contracts pass 62 targeted steps; assessed-head
CI passes all eleven contexts and 177 product steps. No architecture or product
change is made by review.
[Explicit human acceptance](../specs/002-platform-contracts/acceptance.md)
now separately records feature 002's approval with its existing scope and
limitations. The original PR is now human-merged with accepted evidence on main;
no architecture change, new local all-suite or human-usability claim is made
by acceptance.
The host only gains a library project reference; its startup,
routes, stores and web console remain module-free.
Spec 003's [ADR 0005](adr/0005-module-storage-ownership.md) is now explicitly
human-approved for implementation. The
[implementation checkpoint](../specs/003-storage-registry/implementation.md)
adds reviewed EF/provider dependencies, a platform-local registry, immutable
assignments, marker-verified module schemas/files and real both-provider
registration/constraint/process-race evidence. Normal serving remains
module-free. The resumed implementation adds assigned model/history preflight,
actual killed-process recovery, explicit owner-bound abandoned-lock recovery,
admitted maintenance supervision and captured privacy checks. Migration physical
verification rejects unexpected tables/columns/defaults, missing primary keys,
secondary constraints/indexes, generated columns and views/routines/triggers.
The implementation checkpoint marked all 78 original tasks complete; its complete
local 200-step gate and eleven assessed-head CI contexts pass.
The subsequent [convergence](../specs/003-storage-registry/convergence.md)
finds two HIGH partial public-example gaps, not a demonstrated storage-engine
defect or changed architecture decision. T079-T080 require the promised public
three-restart/data retry and distinct-registered-ID collision examples; separate
integration tests already cover the core behavior. No product/test code or
approved design changes during convergence.
The [T079-T080 remediation](../specs/003-storage-registry/implementation.md#convergence-remediation-t079-t080)
now supplies those public observations: three fresh processes retain both exact
assignments and sentinels; the forced registered-owner UNIQUE conflict preserves
assignment/data/inventory and creates no contender allocation. Standalone/`all`
harnesses require measured evidence, and the unowned-target refusal remains.
Only the test-support assembly gains access to the existing internal allocator
seam; no public override, runtime storage rule or dependency is added.
All 80 tasks are evidenced.
The [follow-up convergence](../specs/003-storage-registry/convergence.md) now
passes with zero actionable gaps across all 40 requirements/outcomes/scenarios,
fourteen plan decisions, 80 tasks and twelve principles. It verifies both
public-example findings are closed and leaves tasks, product/test source and
architecture decisions unchanged. Actual assessed-head CI passes eleven contexts
and 200 product steps. Review is next on the same draft PR, followed separately
by explicit human acceptance; convergence does neither and requests no merge.
The subsequent [final review](../specs/003-storage-registry/review.md) records
**changes** for one MEDIUM regression-evidence gap, not a demonstrated runtime
defect or changed architecture decision. T081 must prove that a changed allocator
is not called for a persisted owner and that a persisted no-storage owner cannot
become relational, preserving exact assignments/data/revisions/inventory.
All 80 historical task markers and prior reports are retained; public F1/F2
remain closed. Fresh quality/unit/both-provider integration/recovery/contract/
privacy checks pass 157 targeted steps and actual assessed-head CI passes all
eleven contexts/200 product steps, without supplying the absent regressions.
That review selected implement without a new design approval or merge.
The [T081 regressions](../specs/003-storage-registry/implementation.md#final-review-remediation-t081)
now pass all four real-provider cases, proving zero allocator calls for persisted
owners and immutable no-storage declarations, with exact assignment/data/revision/
inventory preservation. Controlled mutations demonstrate failure detection.
All 81 tasks are evidenced; quality/unit/integration gates pass 46 targeted steps
after an unchanged PostgreSQL rerun of two initial deadline failures. No runtime,
API, dependency or architecture change is introduced.
The [current convergence](../specs/003-storage-registry/convergence.md) now passes
after checking all 40 requirements/outcomes/scenarios, fourteen plan decisions,
81 tasks and twelve principles against actual source/assertions. R1/T081 and
prior public F1/F2 are closed, with zero actionable gaps and no appended task.
Tasks, product/test source, approved design and historical review remain
unchanged. That convergence selected review on the same draft PR.
The [follow-up final review](../specs/003-storage-registry/review.md) now records
**changes** for one MEDIUM implementation finding R2: explicit registry-upgrade
mutations and rejected boundaries omit required correlated storage records.
Existing CLI success/generic failure summaries do not supply the direct API's
complete operation evidence. R1/T081 and prior public F1/F2 are closed.
Exactly T082 is appended for bounded private telemetry/CLI projection and real
both-provider captures, without changing approved architecture, APIs, dependencies,
code or historical task markers. No storage-isolation failure or new design
decision is demonstrated. That review selected implement.
The [T082 increment](../specs/003-storage-registry/implementation.md#final-review-remediation-t082)
now reuses assignment-independent diagnostic emission for explicit upgrades and
failed acquisition. Records include fixed guidance and truthful durability,
without changing `IStorageMaintenance` or manufacturing a module result.
The supervisor maps allowlisted worker exit identities into safe operation
records; raw output remains drained. Both-provider capture/CLI regressions cover
commits, idempotence, boundary refusals and sanitized uncertain failures.
All 82 tasks were evidenced at that implementation checkpoint, which selected
follow-up converge. The [current assessment](../specs/003-storage-registry/convergence.md)
now records **gaps**: R2/T082 and prior T079-T081 are closed, but the public
recovery-set inspection/verification/manifest-write entry points bypass correlated
operation records. A rejected inspection reproduces zero records/spans/counters.
One MEDIUM partial finding F3 appends T083 for the existing FR-014 diagnostic
obligation and capture regressions, not a new durable audit service or design.
That checkpoint selected implement on the same draft PR. All prior task bytes,
product/test code, approved architecture, dependencies and historical reports
remain unchanged; no feature acceptance or intermediate merge is requested.
The [T083 increment](../specs/003-storage-registry/implementation.md#convergence-remediation-t083)
now observes recovery boundaries through the shared emitter without a fabricated
module result, assignment or durable audit runtime. Provider labels come from
trusted installations or fully validated metadata; failures before either use
bounded `unknown`. Manifest durability reflects actual exclusive creation,
completed serialization/flush/close and possible partial writes. Existing
authority, exception contracts, recovery rules and dependencies are unchanged.
All 83 tasks are evidenced with real-provider capture regressions.
That implementation checkpoint selected follow-up converge.
The [convergence after T083](../specs/003-storage-registry/convergence.md) now
**passes** across all 40 requirements/outcomes/scenarios, fourteen plan decisions,
83 tasks and twelve principles. Actual recovery records/captures close F3;
prior T079-T082 remain closed, with zero actionable gaps and no appended task.
Tasks, product/test source, approved architecture, historical review and reviewer
checklists remain unchanged. **Review is next** on the same draft PR, followed
separately by explicit human acceptance; no intermediate merge is requested.
The [final review after T083](../specs/003-storage-registry/review.md) now
**passes** with source/assertion coverage of all 40 requirements/outcomes/
scenarios, fourteen plan decisions, 83 tasks and twelve principles.
F1/F2, R1/R2 and F3 are closed; no defect, remediation task or unresolved
within-scope design decision remains. Tasks, product/test source, approved
architecture and reviewer-owned checklists are unchanged; previous review
evidence is preserved verbatim. **Explicit human acceptance of numbered
feature 003 is next**, separately on the same draft PR. Review neither
accepts the feature nor requests merge or starts 004.
The [explicit human acceptance](../specs/003-storage-registry/acceptance.md)
now records **"this human accesspts 003"** for feature 003 and its documented
scope/limits. No architecture, product/test source, dependency, task, reviewed
checklist or prior evidence changes. Acceptance adds no product measurement,
production-readiness claim or later-scope waiver. The same
[PR #6](https://github.com/Stonefly-Labs/Kofte/pull/6) was human-merged.
Its accepted ledger and all five acceptance hashes were verified on updated
main before 004 began; acceptance itself started no new feature.

**004 / specify, clarify and plan are complete** on `feature/004-module-lifecycle`.
The [draft requirements](../specs/004-module-lifecycle/spec.md) and
[16/16 author checklist](../specs/004-module-lifecycle/checklists/requirements.md)
define trusted module composition, dependency/configuration/contribution
preflight, ordered initialization/migrations, data-preserving restart-controlled
disable/re-enable and honest health/failure observations. They preserve accepted
002/003 contracts and exclude public lifecycle administration, hostile-code
isolation, hot-unload and later extension runtimes. Clarification found all ten
requirements categories clear, with zero questions and no spec/checklist changes;
no design choice or human answer was invented. The
[plan](../specs/004-module-lifecycle/plan.md) and
[Proposed ADR 0006](adr/0006-module-composition-lifecycle.md) now define explicit
compiled discovery, stable bounded whole-set preflight, native DI and separate
stopped preparation versus supervised serving. Assigned migrations/privilege
separation and exact stores remain accepted 003; no anonymous module route or
later capability runtime is added. Current-attempt private health drives only
the existing aggregate readiness contract. Both constitutional design gates
pass, but ADR approval, dependency-edge adoption and actual process/provider/
privacy proof remain future gates. **Checklist is next**, generated unchecked
for reviewer-owned evaluation, on this same branch and
[draft PR #7](https://github.com/Stonefly-Labs/Kofte/pull/7), not implementation
or acceptance.
The following paragraphs
preserve the earlier design-stage history; their Proposed/unstarted statements
do not describe current implementation progress.

Spec 003's [draft specification](../specs/003-storage-registry/spec.md) now
defines the next storage slice: immutable registry ownership, concurrent
collision-safe allocation, isolated module tables/history and crash/recovery
semantics. Clarification now finds all ten ambiguity categories clear, with
zero questions and no spec/checklist changes; the author checklist remains
16/16. Its [plan](../specs/003-storage-registry/plan.md) now proposes immutable
allocator-v1 keys with actual UNIQUE/concurrency checks, verified ownership
markers, assigned EF model/history/migration binding and explicit stopped
complete-set recovery. [Proposed ADR 0005](adr/0005-module-storage-ownership.md)
separates offline PostgreSQL provisioning from the runtime credential: database
CREATE remains denied to the host, and existing owned fixture/runtime checks
remain intact. SQLite retains separate private files and the existing
stopped-host maintenance lease. Bootstrap upgrade is explicit; unsafe legacy
platform-only recovery/reset will be refused once registry/module data exists.
Both constitutional design gates pass; no package or code is adopted.
The [quality checklist](../specs/003-storage-registry/checklists/quality.md)
now passes all 36 requirements-quality criteria under explicit reviewer
authorization, with per-item evidence and no blocking gap or constitutional
conflict. This does not approve ADR 0005 or prove the design: human ADR approval,
dependency adoption and actual both-provider proof remain future gates.
The [78-task list](../specs/003-storage-registry/tasks.md) now orders all five
stories with explicit prerequisites and required both-provider evidence:
durable allocation, process races/constraints, assigned models/history/
migrations, interruption/recovery and safe diagnostics. All tasks are unchecked.
The read-only [analysis](../specs/003-storage-registry/analysis.md) now passes
with concrete coverage of all 22 requirements/outcomes and 18 scenarios,
all 78 tasks mapped and no actionable or constitutional finding.
Requirements/design and task/checklist markers remain unchanged.
Implement is next on the same feature branch/draft PR; explicit human ADR 0005
approval still gates implementation. This analysis adopts no dependency,
changes no architecture decision and proves no runtime behavior.
The broader stack/module/deployment proposals below remain future work, not
installed features or proof of module isolation, production readiness or acceptance.

## 4. Suggested solution/repository layout

```text
kofte/
  apps/
    web/                       # Next.js web console
  src/
    Kofte.Host/                # API + platform composition root + MCP endpoint
    Kofte.Platform/            # minimal core abstractions / contracts
    Kofte.Infrastructure/      # cross-cutting host adapters, storage registries
    Kofte.Modules.Catalog/     # first-party example/core feature module
    Kofte.Worker/              # optional independently hosted jobs process
  modules/
    example-module/            # fully featured reference module
  packages/
    ui/                        # shared design tokens / UI contracts
    module-sdk/                # frontend extension types/SDK
  tests/
    unit/
    integration/
    contract/
    e2e/
  deploy/
    docker/
    helm/
  specs/                       # numbered Spec Kit feature artifacts
  docs/
    product-charter.md
    product-vision.md
    architecture.md
    adr/
```

This is a **starting convention**; avoid prematurely splitting every abstraction into another assembly. Module package boundaries and independently versioned SDKs should follow demonstrated extension needs.

## 5. Module model and lifecycle

### 5.1 Stable identity

Every module MUST declare an immutable, globally namespaced identifier such as `org.example.catalog`, a semantic version, compatible host API range, capabilities, dependency list, permission definitions, and storage requirements. Never use display names as security keys or schema identifiers.

Example **illustrative** manifest (not yet a frozen schema):

```yaml
id: org.example.environments
version: 0.1.0
hostApi: '>=0.1 <1.0'
displayName: Environments
capabilities:
  - api
  - ui
  - resources
  - events
  - workflows
  - permissions
  - mcp-tools
storage:
  relational: true
permissions:
  - environments.read
  - environments.write
```

The YAML above is conceptual background, **not accepted spec 002 input**.
The implemented [schema 1 contract](../specs/002-platform-contracts/contracts/validation.md)
uses strict UTF-8 JSON, explicit collections, `hostContract`, separate versioned
capability objects and `storage.relational` only. Its example/grammar/budgets
are governed by Accepted ADR 0004; schema success alone is not full acceptance.

### 5.2 Extension points (level 3)

- **Backend:** DI registrations, vertical-slice endpoints, handlers, validation, hosted jobs.
- **Frontend:** registered navigation, routes/pages, dashboard/widgets, forms and shared UI conventions.
- **Resources/providers:** type registration, declarative schema, lifecycle/status, validators, provider reconciler/executor.
- **Events:** publish/subscribe versioned integration events, consumer registration, durable delivery policy where required.
- **Workflows:** actions/steps, invocation, persistence, retry, cancellation and compensation hooks.
- **Permissions:** registered permission names, resource scope and policies enforced by the host.
- **MCP:** tool declarations, schemas, descriptions, server-side execution and potentially external-server adapters.

### 5.3 Lifecycle

`discovered -> validated -> registered -> initialized -> migrated -> enabled -> healthy/degraded -> disabled` (with explicit failure states). The runtime MUST validate ID uniqueness, dependency cycles, API compatibility, duplicate permissions/resources/routes/tools, and storage mapping before enabling.

In-process .NET module code is **trusted** code: loading it is not a sandbox. Third-party or less-trusted integrations SHOULD support out-of-process protocols (HTTP, message contracts, remote MCP) with service credentials, authorization, versioning, rate limits and timeouts. Dynamic hot-unload of .NET assemblies is not an MVP promise.

### 5.4 Frontend extension packaging

**Recommended MVP:** build-time or deployment-time registration of trusted frontend packages against a versioned UI SDK and manifest, with an extension registry for navigation/widgets/pages. Do **not** assume arbitrary remote React bundles can be safely and reliably loaded without a separate security/CSP/compatibility design. Advanced runtime UI installation remains an explicit future ADR.

## 6. Relational data ownership (critical invariant)

### 6.1 PostgreSQL: one named schema per module

Each module MUST own its own PostgreSQL named schema; its EF Core context and migrations MUST be pinned to that schema. No plugin may write tables into `public` or another module's schema by convenience. Platform system tables live in a reserved platform schema (for example `kofte_system`).

**Collision-proof schema allocation:**

1. Maintain a host-owned **Module Registry** mapping immutable `module_id` -> allocated physical `schema_name` and status/version.
2. Reserve namespaces, e.g. `kofte_*`, `pg_*`, `information_schema`, `public` and core-managed names. PostgreSQL `pg_` reserved prefixes are especially relevant.
3. Generate a deterministic, bounded, SQL-safe name from module ID, e.g. `m_<readable_slug>_<short_hash>`; enforce PostgreSQL's **63-byte identifier limit**, not just character count. Choose the hash algorithm and collision length as an ADR.
4. Place a **unique database constraint** on normalized allocated schema names; serialize/advisory-lock concurrent registrations or handle conflicts transactionally. Never rely on hashing alone to prevent collisions.
5. Persist mapping permanently. If two modules collide, fail closed with a diagnostic; do not silently reuse or rename an existing schema. Prefer administrator-approved explicit remapping only prior to data creation.
6. Use the stored mapping for schema creation, context mapping, migration history tables, and backups. Do not derive a fresh name at every boot.
7. Test with duplicate IDs, similar slugs, long Unicode identifiers, reserved prefixes, concurrent registration, renames and upgrades.

Schema-per-module is an **ownership boundary**, not a security guarantee when all modules share a superuser DB login. Dedicated credentials/grants MAY be added for stronger isolation.

### 6.2 SQLite: one file per module

Each module MUST have its own SQLite database file managed under a restricted host data directory, e.g. `data/modules/<opaque-module-key>/module.db`. The platform system registry uses its own SQLite database file. File paths MUST come from the registry, not untrusted module-supplied relative paths; check canonical paths to prevent traversal.

Enforce foreign keys and choose SQLite WAL/busy-timeout based on the deployment profile. Each module's DbContext, migrations, backups and recovery act on that **single module file**. Do not claim SQLite schemas, `ATTACH`, or a common shared database as the default module storage strategy. Cross-module joins are not a supported contract.

### 6.3 DbContext, migrations and transactions

- Prefer **module-owned EF Core DbContexts** and module-contained migrations; provider-specific mappings and migrations may be necessary.
- For PostgreSQL, configure both `HasDefaultSchema(...)` and the module migration-history-table schema, and explicitly inspect extension/default schema behavior.
- Migration runner MUST process dependency order, hold an installation lock, produce diagnostics and avoid accidental migration of another module.
- Single-module data updates can be transactional. **Cross-module operations have no guaranteed atomic transaction**, especially across separate SQLite files. Prefer state machines, transactional outbox/inbox per database, idempotency and compensating actions/sagas.
- Do not build distributed transactions or reuse a single DbConnection across DbContexts as a general solution.
- No direct cross-module table access: expose operations/query contracts or versioned read-models instead.

## 7. Application structure: VSA and shared concerns

Implement a slice around one coherent use case:

```text
Modules/Catalog/Features/Services/Create/
  Endpoint.cs
  Request.cs
  Validator.cs
  Handler.cs
  Response.cs

tests/unit/Kofte.Modules.Catalog.Tests/Features/Services/Create/
  CreateTests.cs
```

All .NET test source MUST reside in its dedicated .NET test project, not in the
production project or linked from a production vertical slice. Mirrored module/
feature folders retain logical slice ownership without shipping test source
beside application code. See [ADR 0003](adr/0003-dotnet-test-source-ownership.md).
Frontend colocated tests are unaffected by this .NET-specific rule.

Endpoints SHOULD call feature handlers directly via DI rather than require a mediator library. Domain entities, EF mappings and slice-private helpers stay module-local. Shared platform code is justified only for identity/authorization, module/runtime contracts, data isolation registry, event/job abstractions, telemetry, API error conventions and other truly cross-cutting behavior.

Avoid both a giant `Application` service layer and making everything a generic repository. Integration between modules should use stable interfaces/events, not arbitrary EF navigation properties.

## 8. Domain and resource model

**Conventional core entities:** team, user/member, project, service/component, ownership, environment and resource record. Treat relationships as scoped and permissioned.

**Declarative resource envelope (illustrative):**

```json
{
  "apiVersion": "kofte.dev/v1alpha1",
  "kind": "DatabaseInstance",
  "metadata": {
    "name": "orders-dev",
    "teamId": "team-a",
    "moduleId": "org.example.database"
  },
  "spec": { "provider": "example", "size": "small" }
}
```

Module providers own spec validation and state reconciliation; the host owns common identity, authorization, version/concurrency and audit. Store a separately controlled `status` object and lifecycle transitions. Do not allow clients to forge controller-managed status. Prefer optimistic concurrency, stable resource IDs, and explicit schema/version migration contracts.

## 9. HTTP API and authentication/authorization

- HTTP+JSON OpenAPI API is the authoritative application interface. Use consistent pagination, filtering, idempotency keys for mutations, Problem Details for failures, and optimistic concurrency for state changes.
- Pluggable identity with OIDC integration as a first-class path; optional local development identity is a **development-only** default, not a production security assumption.
- Köfte owns a stable internal subject identity mapping, teams/roles, named permissions, policy checks, service principals and audit records independent of IdP specifics.
- Every actionable endpoint and job invocation checks **principal + action + resource/team scope**. Frontend gating is UX, not security.
- No arbitrary user-provided code execution, pipeline credentials or secrets included in API responses. Use secret references and provider-specific secure stores.

## 10. MCP: early MVP and extension developers

**MCP is a first-class MVP interface, not a later AI add-on.** Start with `list/search/get` platform entities, module/tool discovery and one safe authorized mutation as part of the earliest end-to-end slice.

```mermaid
sequenceDiagram
    participant A as IDE MCP Client
    participant M as Köfte MCP Endpoint
    participant Z as Köfte AuthZ
    participant H as Feature Handler
    participant D as Module Storage
    A->>M: tools/list or tools/call
    M->>Z: authorize identity + tool + resource
    Z-->>M: allow/deny
    M->>H: validated tool input
    H->>D: scoped read/action
    H-->>M: structured result
    M-->>A: tool result + correlation ID
```

### MCP tool contract

- Module tools register namespaced stable IDs (for example `org.example.environments.list`), input/output schemas, permission requirements, read-only/destructive metadata and handler bindings.
- Host checks name uniqueness and version compatibility and filters tool discovery by policy as supported; **always authorize again on invocation**.
- Tools call the **same application handlers** as REST/UI and emit audit, metrics and trace correlation. No unguarded direct database access.
- Support secure remote transport suited to IDE clients; choose SDK/transport and authentication specifics in an early ADR, including token validation, user delegation/service principal behavior, and MCP protocol-version compatibility.
- Destructive tools require explicit safety policy, and optionally a confirmation/approval workflow. Never trust a tool description as a security control.

### Can module authors bring their own MCP servers?

**Yes, by two different patterns:** (A) native contributed tools registered with the Köfte MCP registry, preferred for first-party module actions; (B) external MCP servers managed as separately deployed integrations with explicit allowlists, credentials, namespace collision handling, health checks, timeouts and permission mapping. **External MCP federation/proxying is a later hardening milestone**; do not blindly re-export all tools or assume trust based on the external server's claims.

MCP support does **not** require building a chat assistant into Köfte's own UI.

## 11. Workflow, background jobs, events and providers

### Durable jobs

A durable job MUST record: job ID, owner/team/module, kind, payload reference/version, requested-by identity, state (`queued`, `running`, `succeeded`, `failed`, `cancelled` plus optional `retrying`), attempt count, timestamps, correlation ID and sanitized error/result. Workers must recover abandoned leases after restarts and use bounded retries/backoff, cancellation and idempotent side effects. The storage location for cross-module jobs should be a host-owned system store, avoiding accidental reliance on module tables.

### Workflow contract

Start small: sequence/step state, inputs, outputs, retry policy, progress events and compensating actions where useful. Advanced engines (e.g. Temporal/Durable-style orchestration) can become optional adapters without invalidating persisted workflow contracts.

### Events

Publish typed/versioned **integration events** through host abstractions. In-process dispatch is acceptable for noncritical immediate handlers. For durable cross-module side effects, use an outbox/inbox or reliable job mechanism; explicit *at-least-once* semantics, duplicates and replay must be documented. Do not promise exactly-once delivery.

### Infrastructure/provider modules

Provider interfaces represent `validate/plan/apply/status/cancel` as capability-specific operations. A module may run OpenTofu/Terraform or external tooling **directly**, or submit work to GitHub Actions/Azure DevOps/other CI and report status. The host MUST NOT become the implicit owner of Terraform state. For OpenTofu/Terraform state storage, use supported remote backends as the safe default; a custom backend served by Köfte would be a separately designed feature requiring locking, consistency, security and compatibility testing.

## 12. Notifications and realtime

SignalR handles live job status, resource updates, alerts and activity progress; maintain HTTP fetch/poll as the robust fallback. Authorize subscriptions per team/resource and make every event safe to disclose to its audience. If scale-out later needs it, add a provider-neutral backplane/managed service option rather than making Azure Web PubSub mandatory.

## 13. Deployment profiles

| Profile | Pieces | Intended use |
|---|---|---|
| **Local/lightweight** | Next.js UI, .NET host (+ optional colocated worker), SQLite file per module, internal scheduler | Developer/evaluation/small-team instance |
| **Team/shared** | UI, host API, PostgreSQL with module schemas, independently scalable worker, ingress/TLS | Multi-team deployment |
| **Kubernetes** | OCI containers, secrets, config, persistent database, probes, autoscaling/worker strategy, Helm/Kustomize | First-class production target |

Deployment documentation MUST state persistence/backup responsibilities, TLS setup, auth defaults and security implications. Avoid making Redis, a dedicated message broker, external workflow server, or a cloud-managed realtime service a baseline dependency.

## 14. Observability and operations

- OpenTelemetry tracing across web/API/MCP/jobs/provider calls; consistent correlation IDs.
- Structured logs with user/module/action/job context but no raw secrets or unredacted tokens.
- Health/readiness endpoints and module lifecycle/compatibility diagnostics.
- Module-aware metrics: tool invocations, workflow duration/errors, queue age, registration/migration failures and provider latencies.
- Per-module backup/restore/migrations; explicit upgrade/rollback documentation; PostgreSQL database-wide backup considerations in addition to per-schema ownership.
- Audit trail for privileged admin operations, permissions, resource mutations, sensitive tool calls, module installation and migrations.

## 15. Security boundaries

- Module identity is immutable; signatures/provenance and trusted publishers SHOULD be evaluated before remote/community distribution.
- Modules run with only declared capabilities, but **in-process C# modules are not security-sandboxed**. Treat them as trusted and use external processes for low-trust extensions.
- Separate security for module execution, tool invocation, provider credentials and user actions. Use least privilege and explicit scopes.
- Defensive input validation for HTTP/MCP/providers; prompt injection from MCP descriptions/results must not be able to alter authorization or execute arbitrary privileged actions.
- HTTPS/auth required for non-local MCP exposure; enforce rate limits, request budgets, timeouts and auditing.
- Avoid leaking cross-team data in caching, search indexes, notifications and tool discovery/results.

## 16. Testing strategy and hard acceptance gates

- **Unit:** handlers, authorization rules, schema name allocator, manifest validation, resource transitions.
- **Integration:** PostgreSQL and SQLite migrations; concurrent module registration; restart-safe jobs; outbox/inbox; OIDC/user mapping.
- **Contract:** module API/version compatibility, events, MCP schemas/permissions, resource kind versions.
- **E2E:** Playwright UI and tool invocations against identical underlying operations; denied operations are denied from every entrypoint.
- **Compatibility:** installation with no optional modules; example community module; dependency conflicts; upgrades/rollbacks; K8s deployment smoke test.
- **Security:** ID/schema/path collisions, traversal, duplicate tool names, user/team scope violations, untrusted external MCP tool output, unauthorized destructive operations.

**Mandatory MVP architecture proof:** a standalone example module defines a resource, stores it in its own PostgreSQL schema or SQLite file, surfaces it in the Next.js UI, exposes a REST route and an MCP tool, emits an event/starts a durable job, and honors the same team-scoped permission checks throughout.

## 17. ADR backlog and unresolved design choices

| ADR | Question | Recommended starting position |
|---|---|---|
| ADR-001 | Module ID and storage schema allocator | Immutable reverse-DNS ID, allocated deterministic bounded name + unique registry constraint |
| ADR-002 | Module loading & trust | Trusted in-process backend; external protocol for lower-trust integrations |
| ADR-003 | UI plugin composition | Versioned build/deployment-time frontend packages in MVP |
| ADR-004 | MCP transport/auth/external servers | Early server with native tools; phase external federation separately |
| ADR-005 | Job/workflow persistence | Minimal host-owned durable queue/state tables first |
| ADR-006 | Postgres/SQLite migration parity | Provider-specific migration strategies; no silent shared database |
| ADR-007 | First production auth providers | Pluggable OIDC, explicit bootstrap administrator process |
| ADR-008 | Provider execution model | Direct and CI/CD-delegated adapters; separate IaC state responsibility |
| ADR-009 | External event transport | Abstract from day one; introduce reliable broker only when needed |
| ADR-010 | Module distribution and signature verification | Private packages first, community registry/signing later |

## 18. Recommended Spec Kit decomposition

For `/speckit.specify`, build **small functional slices** with explicit acceptance tests and dependencies:

1. Platform bootstrap, repository conventions and developer environment.
2. Stable module manifests, registry, dependency validation and lifecycle.
3. Named PostgreSQL schema allocation and isolated SQLite file management.
4. Identity mapping, team-scoped authorization and audit fundamentals.
5. Core service/team/project catalog and declarative resource envelope.
6. **MCP endpoint and first native tools** (begin in parallel with the first domain slice).
7. Next.js app shell/design system and UI extension registry.
8. Durable jobs, progress events and SignalR updates.
9. Provider integration SDK and direct vs CI/CD orchestration example.
10. End-to-end example module with backend/UI/resource/events/workflow/permissions/MCP.
11. K8s packaging, operability, telemetry, backups and release compatibility.
12. External MCP connector/federation and eventual Hoard integration (optional follow-on).

For each specification include: purpose/personas, core vs module ownership, endpoint/UI/MCP shape, required permission and scope, data store and migration requirements, events/jobs/retries, failure modes, compatibility, tests and deployment effects.

---

**Source of truth:** [`product-charter.md`](product-charter.md) sets scope; [`product-vision.md`](product-vision.md) sets direction. Where this file says *recommended/proposed*, the engineering team should record a decision before treating it as a locked requirement.
