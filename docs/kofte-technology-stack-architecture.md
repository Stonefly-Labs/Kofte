# Köfte — Technology Stack & Architecture

**Status:** Proposed architecture for review  
**Version:** 0.2  
**Date:** 2026-10-20  
**Project:** Köfte (use `kofte` in code, CLI commands, repository names, URLs, and other ASCII-constrained contexts)

## 1. Product and architecture overview

Köfte is an open-source, self-hostable, composable Internal Developer Platform (IDP) designed for individual developers, small teams, and larger organizations. It offers a unified developer experience for service and software catalogs, declarative resources, self-service templates, infrastructure integrations, lifecycle operations, and Model Context Protocol (MCP) tooling.

The platform starts as a **modular monolith**, organized primarily using **Vertical Slice Architecture (VSA)**. It provides several forms of extensibility: native .NET modules, externally hosted service integrations, resource providers, UI extensions, event handlers, workflow contributions, and external MCP servers. Hoard is an independent application that can be offered as an optional Köfte module; neither project should require the other to operate independently.

### Guiding principles

1. **MIT-licensed platform; permissively licensed default dependencies.** Avoid MediatR, AutoMapper, commercial-license requirements, and similar licensing risks. Verify actual versions and transitive dependencies during implementation.
2. **Composable by design.** Core owns extension contracts, discovery, lifecycle, permissions, and cross-cutting platform behavior. Feature modules own their domains.
3. **No cloud lock-in.** Kubernetes is a first-class target but not a prerequisite; Azure-only services are optional integrations.
4. **Progressive operational complexity.** Run locally with SQLite and minimal services; scale to PostgreSQL, separate workers, and Kubernetes.
5. **MCP early.** Expose a useful MCP server in the first implementation phase and expand tools as platform features arrive.
6. **Declarative resources.** Resource types, schemas, desired state, and observed state are platform concepts, regardless of infrastructure provider.
7. **Isolation by contract and storage boundary.** Modules have independent domain logic, database ownership, and migrations.
8. **Secure by default.** Authentication, authorization, audit, secrets management, and isolation apply equally to UI, REST, MCP, providers, and plugins.

## 2. Stack decisions at a glance

| Area | MVP choice | Beyond MVP |
|---|---|---|
| License | MIT; vetted permissive dependencies | Automated SBOM and supply-chain attestations |
| Backend | .NET 10 LTS, ASP.NET Core Minimal APIs | Scale-out API/worker topology |
| Architecture | Modular monolith, Vertical Slice Architecture | Optional separately hosted capabilities |
| DI and modules | Built-in .NET DI, Scrutor | More sophisticated plugin distribution |
| Validation | FluentValidation | Additional resource policy layers |
| Persistence | EF Core; PostgreSQL or SQLite | Selective Dapper reads, caching |
| PostgreSQL isolation | **Dedicated named schema per module** | Hardened DB roles/ownership boundaries |
| SQLite isolation | **Dedicated database file per module** | Export/import and module-specific backup tooling |
| API | REST/OpenAPI | Additional integration adapters |
| MCP | Official MCP C# SDK, ASP.NET Core integration | Richer resources/prompts, broad remote MCP catalog |
| Events/jobs | Durable relational operations, outbox/inbox, `BackgroundService` | Broker adapters, optional workflow engines |
| Realtime | ASP.NET Core SignalR | Optional distributed backplane |
| Frontend | Next.js, React, TypeScript | Advanced visual editors and plugin experiences |
| Component system | Tailwind CSS, shadcn/ui, Radix UI, Lucide | Design-system extensions |
| Client data | TanStack Query, TanStack Table | Graph views, analytics |
| Forms | React Hook Form, Zod | Form generation from resource schemas |
| Editor | Monaco Editor | Enhanced schema diagnostics/diffs |
| Infrastructure | Kubernetes C# client; initial GitHub and OpenTofu integrations | Argo CD, Flux, Crossplane, additional providers |
| AuthN | ASP.NET Core OIDC/JWT, external identity providers | Additional provider adapters |
| AuthZ | Köfte RBAC + resource-scoped policies | OPA/Casbin integration options |
| Telemetry | OpenTelemetry, .NET logging | Prometheus/Grafana integrations |
| Deployment | Docker Compose; Kubernetes manifests/Helm | Operators and HA configurations |
| Tests | xUnit, Testcontainers, Vitest, React Testing Library, Playwright | Expanded compatibility and performance suites |

**No MediatR or AutoMapper.** Prefer direct handlers, explicit mappings, standard ASP.NET Core middleware/endpoint filters, and well-defined module contracts.

## 3. Logical architecture

```text
                          +-----------------------------+
                          | Browser / CLI / IDE / Agent |
                          +--------------+--------------+
                                         |
                    +--------------------+--------------------+
                    |                                         |
             +------+-------+                         +-------+--------+
             | Next.js UI   |                         | Köfte API Host |
             | React / TS   |                         | REST / MCP    |
             +--------------+                         | SignalR       |
                                                      +-------+--------+
                                                              |
                         +------------------------------------+------------------------+
                         |                        Application modules                  |
                         | Catalog | Teams | Resources | Templates | Operations | ... |
                         |   VSA features + module contracts + permission checks      |
                         +--------------------+---------------------------+------------+
                                              |                           |
                             +----------------+-----------+    +----------+-----------+
                             | Platform infrastructure   |    | Plugin integrations   |
                             | EF / events / jobs / auth  |    | .NET / HTTP / MCP     |
                             +-------------+-------------+    +----------+-----------+
                                           |                             |
                           +---------------+----------+       +----------+----------+
                           | PostgreSQL or SQLite     |       | K8s / Git / IaC     |
                           | Per-module storage       |       | CI/CD / Hoard / ...  |
                           +--------------------------+       +---------------------+
```

These are logical boundaries, **not** all separate processes. The MVP can run one API process, one Next.js process, and a database. Background work may initially run within the API process; a dedicated worker is optional.

## 4. Backend and Vertical Slice Architecture

### Recommended packages

- **ASP.NET Core Minimal APIs:** HTTP endpoints and transport boundary.
- **Built-in OpenAPI support + Scalar (license-verified version):** Interactive HTTP API documentation.
- **Microsoft.Extensions.DependencyInjection:** Primary DI container.
- **Scrutor:** Convenient service/module scanning and registration.
- **FluentValidation:** Request and model validation. Confirm package-level license for selected version.
- **Polly / Microsoft.Extensions.Http.Resilience:** Resilience for outbound calls and providers.
- **System.Text.Json:** Default serialization.
- **OpenTelemetry SDK/exporters:** Traces, metrics, and correlations.
- **Microsoft.Extensions.Logging:** Logging abstraction. Serilog is optional.

### VSA conventions

Each module owns its endpoints, handlers, domain types, validators, EF model/configuration, migrations, events, and internal implementation. Shared code should remain limited to genuinely shared contracts/infrastructure.

```text
Modules/
  Catalog/
    Features/
      RegisterComponent/
        Endpoint.cs
        Request.cs
        Handler.cs
        Validator.cs
      GetComponent/
        Endpoint.cs
        Handler.cs
    Domain/
    Infrastructure/
      CatalogDbContext.cs
      Migrations/
    CatalogModule.cs
```

REST and MCP transport entry points call **the same application handlers**. Do not introduce a mediator dependency merely for the sake of VSA. Use endpoint filters, middleware, decorators, or explicit pipelines as warranted. Prefer explicit C# mapping to a separate mapping framework.

### Module boundaries

- Each module has a stable immutable module ID and a published contract surface.
- Modules must not query another module's internal database tables directly.
- Cross-module communication occurs through application contracts and versioned events.
- Database transaction coordination across modules is possible in PostgreSQL where participating contexts use a compatible shared connection/transaction; use it sparingly and without coupling domain models.
- Prefer durable workflows and events when operations are long-running or cross external systems.

## 5. Persistence — confirmed architectural decisions

**Hard requirements:**

1. **PostgreSQL: every module, including every installed plugin with persistence, owns a dedicated, named schema.**
2. **SQLite: every module, including every installed plugin with persistence, owns a dedicated database file.**
3. Köfte centrally assigns and records storage identities. Plugin developers do **not** freely choose PostgreSQL schema names or SQLite filenames.
4. Each module owns its DbContext, model, migrations, and migration history.
5. Both storage models must be supported by the official module persistence integration unless the plugin clearly declares a provider-specific requirement.

### 5.1 PostgreSQL: named schema per module

Example logical layout:

```text
kofte (PostgreSQL database)
├── core_identity
│   ├── users
│   ├── groups
│   └── roles
├── core_catalog
│   ├── components
│   └── relationships
├── core_operations
│   ├── operations
│   └── events
├── ext_hoard_7c92a6d1
│   └── ...
└── ext_github_9f3b12e4
    └── ...
```

Built-in modules receive reserved, centrally defined `core_*` schemas. Community extensions receive centrally assigned names, such as `ext_<normalized-short-slug>_<hash>`, derived from **immutable, globally namespaced module IDs**, not display names or versions. Example IDs:

```yaml
module:
  id: io.github.example.hoard
  displayName: Hoard
  version: 1.0.0
```

#### Collision-resistant schema assignment

The central **Module Registry** is authoritative:

1. Require a stable globally namespaced module ID (reverse-DNS naming is recommended).
2. Reserve internal namespaces (for example `kofte.core.*`) for first-party modules.
3. Derive a safe ASCII PostgreSQL schema candidate from the module ID and a stable cryptographic digest, subject to PostgreSQL's usual 63-byte identifier limit.
4. Check both the registry and existing database catalog before assigning the name.
5. Store the final assigned name as an **immutable mapping**; never recompute it as the sole lookup on subsequent starts.
6. Enforce unique constraints on `module_id` and `schema_name`; if a rare collision or pre-existing unowned schema is found, deterministically allocate an alternate name and persist it.
7. Reject a conflicting attempt to register the same module ID with incompatible ownership/identity. Upgrades retain the original schema name.
8. Use the allocated schema for EF Core table mappings, migrations, and a **schema-local migration history table**.
9. Deny plugin-authored schema overrides and validate generated migrations for cross-schema writes or destructive operations.

**Security note:** A schema name alone is namespace isolation, not a security boundary. For stronger isolation, use per-plugin database roles and narrowly scoped grants where deployment mode permits. The application must still enforce plugin trust and permission controls. Migration validation alone is not a sandbox against arbitrary trusted in-process .NET code.

### 5.2 SQLite: database file per module

```text
/data/kofte/
├── core/
│   ├── identity.db
│   ├── catalog.db
│   ├── operations.db
│   └── plugins.db
└── extensions/
    ├── ext_hoard_7c92a6d1.db
    └── ext_github_9f3b12e4.db
```

The same Module Registry allocates filenames safely; no plugin-supplied absolute paths or path traversal. A module receives its own connection string, migrations, and migration history. File ownership, backup, cleanup, and uninstall semantics follow the module lifecycle.

**Transaction limitation:** Do not promise a portable ACID transaction spanning separate SQLite files, particularly when WAL is enabled. Use durable cross-module operations/events, outbox/inbox handling, idempotency, and compensations. PostgreSQL coordinated cross-schema transactions remain an exceptional optimization, not a generic module dependency contract.

### 5.3 Persistence integration and lifecycle

Illustrative application contract (API shape subject to design review):

```csharp
services.AddKofteModuleDatabase<CatalogDbContext>(
    moduleId: "kofte.core.catalog");
```

Köfte resolves the active provider, allocated storage identity, EF context configuration, module-specific migrations, and migration history location. Modules shouldn't independently build connection strings or select their own schemas/files.

The registry records:

| Field | Meaning |
|---|---|
| `module_id` | Immutable globally namespaced ID |
| `module_kind` | Built-in or extension |
| `storage_provider` | PostgreSQL or SQLite |
| `storage_identity` | Assigned PostgreSQL schema or SQLite file |
| `migration_version` | Last applied module migration |
| `owner_plugin_id` | Owning extension, where applicable |
| `lifecycle_status` | Registered, active, disabled, removal pending, etc. |
| `provisioning_status` | Storage/migration readiness |

The registry must be bootstrapped before dynamic plugin activation. **Uninstall does not automatically delete module data**; irreversible cleanup is a separate privileged and audited operation. Ensure module backup, restore, and migration failure recovery are documented.

### 5.4 ORM and storage choices

- **Entity Framework Core:** Default ORM, migrations, and unit-of-work semantics.
- **Npgsql EF Core provider:** PostgreSQL support.
- **Microsoft.EntityFrameworkCore.Sqlite:** SQLite support.
- **Dapper:** Optional targeted high-performance read models after profiling.
- **Database provider parity tests:** Mandatory for shared persistence SDK behavior.

## 6. Durable operations, messaging, and real-time updates

MVP uses a lightweight durable execution abstraction implemented with relational persistence rather than requiring a message broker or a fully featured orchestration product.

**MVP components:**

- Durable `Operation` record and explicit state machine, including terminal states.
- Background worker using `BackgroundService` and database-backed claiming/leasing.
- Retry, exponential backoff, timeouts, cancellation, and crash recovery.
- Transactional outbox/inbox as needed for consistency across module boundaries.
- Idempotency keys and operation correlation IDs.
- **SignalR** for live progress/status and notifications; REST polling remains supported.
- Module-defined versioned events and internal subscriptions.

Evaluate **Wolverine** as a potential replacement/implementation of some of these mechanics, but do not make adoption a prerequisite for MVP. Avoid stacking Wolverine, MassTransit, Hangfire, Quartz, and Elsa without clear, separate needs.

**Beyond MVP:** optional RabbitMQ, Azure Service Bus, and other broker transports; advanced durable workflow integration such as Elsa; distributed worker deployments; distributed SignalR backplane when scale-out warrants it.

## 7. Frontend stack and UX architecture

Köfte should feel like modern developer tooling: responsive, searchable, keyboard-friendly, accessible, clear in dense data views, and polished in both light and dark modes.

### MVP libraries

| Technology | Purpose |
|---|---|
| **Next.js App Router** | App framework and routes |
| **React + TypeScript** | Components and typed UI |
| **Tailwind CSS** | Styling and design tokens |
| **shadcn/ui + Radix UI** | Accessible component foundation |
| **Lucide React** | Icons |
| **TanStack Query** | Server-state caching, mutations, invalidation, background refresh |
| **TanStack Table** | Resource inventory tables, filters, sorting, pagination |
| **React Hook Form** | Form state and submission |
| **Zod** | Client schema validation |
| **Monaco Editor** | YAML/JSON resource editing |
| **cmdk** | Command palette |
| **Sonner** | Toasts/notifications |
| **next-themes** | Dark/light theme |
| **Generated OpenAPI TypeScript client** | Typed calls to Köfte REST API |

**Rendering model:** Next.js Server Components for appropriate page shells and initial read-only loads; Client Components for forms, inventories, code editors, and live dashboards; TanStack Query as the client request/cache layer; SignalR for operation updates. The backend always rechecks authorization, regardless of UI visibility.

### Post-MVP UI features

- **React Flow:** Workflow editing and visualization.
- **Cytoscape.js:** Resource/dependency topology graphs.
- **Recharts:** Operational dashboards and metrics.
- **Motion for React:** Purposeful micro-interactions.
- **Monaco Diff Editor:** Proposed-versus-current resource changes.
- **Zustand:** Optional complex client-only state, only where warranted.
- **Virtualization:** Large inventories, if profiling shows a need.

### Resource-driven UI

A registered resource type describes its JSON Schema, capabilities, columns, and optional presentation metadata. Generic UI can render create/edit forms, inventory views, and resource details without a custom React component for every resource. Extensions contribute trusted UI slots and routes through a manifest contract. Start with **manifest-driven extensibility**, not unrestricted runtime remote JavaScript loading.

Illustrative definition:

```yaml
apiVersion: kofte.io/v1alpha1
kind: ResourceDefinition
metadata:
  name: database
spec:
  displayName: Database
  icon: database
  schema: schemas/database.schema.json
  capabilities: [create, read, update, delete]
  ui:
    list:
      columns: [name, provider, status]
    details:
      sections: [overview, configuration, activity]
```

## 8. Declarative resources and schemas

The platform defines versioned resource types with identities, ownership, relationships, desired state, observed state, conditions, metadata, and provider adapters.

```yaml
apiVersion: kofte.io/v1alpha1
kind: Component
metadata:
  name: payments-api
  namespace: commerce
  labels:
    environment: production
spec:
  type: service
  owner: payments-team
  lifecycle: production
  repository: https://github.com/example/payments-api
```

**Technical choices:** YamlDotNet, `System.Text.Json`, **JsonSchema.Net** (or NJsonSchema after evaluation), versioned JSON Schemas, native C# contracts, and a Köfte-native reconciler framework. Kubernetes-inspired conventions do **not** imply requiring Kubernetes or implementing Kubernetes' full API.

Logical workflow:

```text
REST / UI / MCP
  -> authenticate and authorize
  -> validate resource schema
  -> persist desired state
  -> enqueue durable operation
  -> select provider / reconcile
  -> persist observed state and conditions
  -> publish events + SignalR status
```

## 9. Plugin architecture and SDK

### Supported extension types

| Type | MVP commitment | Later evolution |
|---|---|---|
| Built-in modules | Compiled .NET feature modules | More optional modules |
| Native plugins | Trusted in-process .NET assemblies | Package registry, signatures, upgrade tooling |
| External plugins | HTTP/service adapter contracts | Independently hosted plugin runtime |
| Resource providers | First-class provider interface | Richer lifecycle and drift remediation |
| Events | Published module events/subscribers | External transports |
| Workflow contributions | Simple operation handlers | Advanced workflow engines |
| UI | Manifest-driven routes, navigation, slots | Sandboxed/remote UI models |
| MCP | Plugin-contributed tools and external servers | Full registry, resources/prompts |

**Candidate libraries:** Scrutor; McMaster.NETCore.Plugins for native assembly loading; NuGet.Packaging, NuGet.Protocol, and NuGet.Versioning for future packages; YARP for selected external service routing.

**Important:** .NET `AssemblyLoadContext` and plugin loaders offer loading/dependency boundaries, **not sandbox isolation**. In-process plugins are trusted software, and removal/unloading may require restart. External plugins must have appropriately scoped credentials and network permissions.

Example manifest:

```yaml
apiVersion: kofte.io/v1alpha1
kind: Plugin
metadata:
  id: io.github.example.hoard
  name: Hoard
  version: 0.1.0
spec:
  runtime: dotnet
  entrypoint: Hoard.Kofte.Plugin
  contributes:
    - api
    - navigation
    - resources
    - events
    - mcpTools
  permissions:
    - packages:read
    - packages:write
```

Manifest permissions are **requests**, not grants. The host checks administrative approval and resource-level authorization. Storage identity is assigned by the central registry, never trusted from a plugin's desired schema/file name.

### Hoard

Hoard remains an independent open-source package caching solution and may expose an optional Köfte integration module that contributes package management UI, APIs, resources, and MCP tools. Köfte itself should not depend on Hoard.

## 10. MCP architecture — early MVP

Use the **official Model Context Protocol C# SDK**, including ASP.NET Core HTTP server support. Support the platform acting as an MCP client for approved external MCP servers.

### Design requirements

- MCP and REST delegate to the same handlers/business capabilities.
- Every tool call uses the same authenticated identity, permission checks, validation, and audit semantics as other entry points.
- Destructive actions may require explicit approval and are not implicitly exposed to external agents.
- Plugins register MCP contributions through Köfte capability registration; external MCP servers can contribute tools after discovery and approval.
- Namespacing/identity prevents duplicate tool names.
- SSRF protection, destination allowlisting, credential management, and tool timeouts are required for external MCP clients.
- Core tool definitions evolve with modules. MCP should be testable in the platform foundation milestone.

### Initial MCP tool inventory (illustrative names)

```text
kofte_get_platform_info
kofte_list_capabilities
kofte_list_components
kofte_get_component
kofte_register_component
kofte_list_resource_types
kofte_get_resource_schema
kofte_list_resources
kofte_get_resource
kofte_apply_resource
kofte_get_operation
kofte_list_operations
kofte_list_templates
kofte_get_template
kofte_list_plugins
kofte_get_plugin_capabilities
```

Later: MCP resources, prompts, richer administrative tools, tool approval workflows, and an optional in-platform AI assistant. **MCP is one extension surface, not the sole plugin model:** it does not replace frontend routes, resource ownership, lifecycle contracts, or authorization.

## 11. Authentication, authorization, teams, and tenancy

- Use ASP.NET Core authentication and standards-based **OpenID Connect/OAuth 2.0** integrations.
- Initial identity providers: generic OIDC, with Microsoft Entra ID, Keycloak, Authentik, and other standards-based providers as targets.
- Provide secure administrator/bootstrap setup for self-hosted installation; do not require a full first-party identity server in MVP.
- Core authorization supports users, groups, teams, roles, permissions, resource ownership, resource-scoped grants, and plugin-requested capabilities.
- Start with **multi-team** support. Design IDs and tenancy boundaries so multi-organization support is possible later, without claiming it exists in MVP.
- Candidate external policy integrations later: **Casbin.NET** or **Open Policy Agent**, not mandatory runtime dependencies.

## 12. Infrastructure providers and execution

Infrastructure orchestration lives in optional modules. Core declares provider/executor contracts without embedding Terraform/OpenTofu, cloud SDKs, or Kubernetes control planes as mandatory dependencies.

### MVP candidates

1. **Kubernetes:** official Kubernetes C# client; catalog discovery and simple resource/provider operations.
2. **GitHub:** source repositories and initial CI/CD workflow orchestration.
3. **OpenTofu:** initial executor adapter capable of local/isolated execution and external orchestration; no forced backend.
4. **Local executor:** controlled process execution under strict trust, timeout, cancellation, and logging rules.

### Later integrations

GitLab, Azure DevOps, Helm, Argo CD, Flux, Crossplane, Pulumi, Terraform compatibility, provider marketplace, and more Git/CI systems.

### OpenTofu state

Use established OpenTofu state backends in MVP. Köfte-managed state is a potential later capability, likely via OpenTofu-compatible HTTP backend semantics. It must properly implement locking, concurrency control, versioning, sensitive state handling, and recovery; do not imply simply persisting state JSON into Köfte's DB is sufficient.

## 13. Deployment and hosting

### Lightweight local/self-hosted

```text
Browser/IDE -> Next.js -> Köfte API (+ BackgroundService)
                              |
                      SQLite files, one per module
```

Docker Compose may package the Next.js and API services with persistent volumes. A separately managed external identity provider can be configured where needed.

### Multi-team / production

```text
Browser/IDE -> Ingress -> Next.js + Köfte API
                                     |
                           PostgreSQL database
                           (one schema per module)
                                     |
                        Optional separate workers
                                     |
                         External integrations
```

Kubernetes receives first-class manifests/Helm support, but no Kubernetes dependency exists for lightweight installation. SignalR's distributed scale-out/backplane is optional and introduced when running multiple API replicas requires it.

### Operational baseline

Health/readiness probes, HTTPS, structured logs, redacted secrets, graceful shutdown, module migration management, audit events, backup/restore guidance, operation restart recovery, and observability export.

## 14. Testing and developer tooling

| Area | Choice |
|---|---|
| Local orchestration | .NET Aspire AppHost and service defaults |
| Backend tests | xUnit |
| Mocking | NSubstitute |
| API integration | `WebApplicationFactory` |
| Database integration | Testcontainers for PostgreSQL; real temporary SQLite files |
| Frontend unit/component | Vitest and React Testing Library |
| Browser E2E | Playwright |
| Coverage | Coverlet |
| Formatting/linting | `dotnet format`, ESLint, Prettier |
| Typed API client | OpenAPI-generated TypeScript client, e.g. Orval or `openapi-typescript` |
| CI/CD | GitHub Actions |
| Packaging | OCI containers, Docker Compose, Helm |

Critical tests include module ID/schema/file collision attempts, registry migration consistency, PostgreSQL schema-scoped migrations, SQLite file isolation, provider parity, cross-module consistency, native plugin reload behavior, REST/MCP authorization equivalence, external MCP tool approval, and interrupted job recovery.

## 15. Security, dependency licenses, and governance

The Köfte source repository is **MIT licensed**. Default dependencies should be limited to compatible, reviewed permissive licenses (principally MIT, Apache-2.0, BSD-2-Clause, BSD-3-Clause, ISC). Review package **versions, transitive dependencies, distribution obligations, and optional services** before pinning the implementation.

**Exclude MediatR, AutoMapper, and similar packages that introduce unwanted commercial-license or reciprocal-license exposure.** This is an intentional product policy, not a claim that every version of those projects has identical terms.

Baseline controls:

- Authentication and resource-scoped authorization on every API and MCP action.
- Plugin administrative approval, immutable identity, and trusted package policy.
- Central PostgreSQL schema/SQLite file registry and migration checks.
- Audit trail for administrative changes and sensitive resource/provider operations.
- Secret references rather than reusable plaintext credentials in manifests.
- Encryption/key persistence and rotation policy for stored secrets when relevant.
- Network destination controls on external service and MCP connectors.
- Vulnerability and dependency license scans in CI, plus notices/SBOM.
- Clear security model for OpenTofu execution and in-process native plugins.

Later: signature verification, provenance/SLSA-style attestations, external plugin sandboxing, advanced policy evaluation, and stronger per-module database roles.

## 16. Proposed repository structure

```text
kofte/
├── src/
│   ├── backend/
│   │   ├── Kofte.Api/
│   │   ├── Kofte.Worker/
│   │   ├── Kofte.AppHost/
│   │   ├── Kofte.ServiceDefaults/
│   │   ├── Kofte.Core/
│   │   ├── Kofte.Plugin.Abstractions/
│   │   ├── Kofte.Plugin.Hosting/
│   │   ├── Kofte.Infrastructure/
│   │   ├── Kofte.Persistence/
│   │   └── Modules/
│   │       ├── Catalog/
│   │       ├── Teams/
│   │       ├── Resources/
│   │       ├── Templates/
│   │       ├── Operations/
│   │       ├── Plugins/
│   │       └── Mcp/
│   ├── frontend/web/
│   └── cli/                     # Later implementation
├── plugins/
│   ├── kubernetes/
│   ├── github/
│   ├── opentofu/
│   └── hoard/                   # Optional integration
├── sdk/
│   ├── dotnet/
│   └── typescript/
├── tests/
│   ├── unit/
│   ├── integration/
│   ├── contracts/
│   └── e2e/
├── deploy/
│   ├── docker/
│   ├── helm/
│   └── kubernetes/
├── docs/
│   ├── architecture/
│   ├── decisions/
│   ├── development/
│   └── plugins/
└── .github/workflows/
```

## 17. Delivery phases

### Phase 1 — Platform foundation

- API/module scaffolding and VSA conventions.
- SQLite and PostgreSQL support with **file-per-module/schema-per-module** semantics.
- Central module/storage identity registry and collision prevention.
- Core auth/RBAC, telemetry, OpenAPI, and basic UI shell.
- MCP server operational from the start, with foundational tools.
- Unit and integration tests for persistence/provider parity and boundaries.

### Phase 2 — Catalog and declarative resources

- Team ownership, components, resource types, versioned JSON schemas, resource inventory CRUD.
- Generic resource forms and tables, YAML editor, search/filtering.
- Relevant MCP tools developed alongside each feature.

### Phase 3 — Self-service and execution

- Templates and provisioning requests.
- Durable operations, events, worker, retries, status, SignalR.
- Kubernetes, GitHub, and initial OpenTofu provider integrations.
- Provider authorization, credentials/secrets, audit trail.

### Phase 4 — Composable MVP release

- Trusted native plugin activation and lifecycle controls.
- External HTTP/MCP extension registration and approved tool exposure.
- Manifest-driven UI extension points.
- Hoard integration proof of concept as an optional module.
- Docker Compose and Kubernetes installation packages; plugin developer docs.

MCP, security, and module isolation are **cross-cutting from Phase 1**, not postponed until Phase 4.

## 18. Beyond MVP roadmap

- Plugin marketplace, signing, compatibility policies, and automated upgrades.
- Workflow visualization with React Flow; topology graphs with Cytoscape.js.
- GitOps support, drift detection, and remediation.
- Broader infrastructure/CI/CD integration set.
- External event broker adapters and distributed workers.
- Advanced policy integration (OPA/Casbin), and multi-organization tenancy.
- CLI and optional AI assistant.
- Richer MCP resources, prompts, approval UX, and federation patterns.
- Managed OpenTofu state if locking/recovery/security semantics are fully implemented.
- Fine-grained PostgreSQL DB-role isolation and plugin sandboxing.
- Operational analytics, cost visibility, and platform health dashboards.

## 19. Decisions still proposed (not locked)

1. **Workflow implementation:** small Köfte-owned EF Core state machine + `BackgroundService` (recommended) versus Wolverine.
2. **Native plugin packaging:** assembly loading using McMaster.NETCore.Plugins, with explicit process restart constraints, versus a simpler build-time plugin catalog first.
3. **Frontend contribution system:** manifest-driven slots/routes (recommended) rather than unrestricted module federation in MVP.
4. **Resource reconciler:** minimal Köfte-native desired-state reconciliation (recommended) versus CRUD-first/later reconciliation.
5. **OpenTofu state:** established external backends in MVP (recommended) versus building Köfte-managed state prematurely.
6. **Plugin namespace algorithm:** exact digest length, canonicalization rules, collision retry approach, and organizational ownership verification need formal specification, while **central assignment and uniqueness are decided**.
7. **Authentication bootstrap:** precise local admin bootstrap behavior and installation hardening need a dedicated security specification.

## 20. MVP architecture acceptance criteria

- [ ] Install Köfte using Docker Compose without Kubernetes, a paid SaaS account, or a required external message broker.
- [ ] Run against SQLite with a **separate file for each module**, including extensions.
- [ ] Run against PostgreSQL with a **dedicated named schema per module**, including extensions.
- [ ] Centrally allocate schemas/files and reject or safely resolve module identity and storage naming collisions.
- [ ] Run migrations under module ownership without cross-module table drift.
- [ ] Create, retrieve, update, and reconcile resources through REST and the web UI.
- [ ] Perform meaningful catalog/resource operations through the MCP server, with shared authorization and audit behavior.
- [ ] Register and approve an external MCP server and selected contributed tools.
- [ ] Execute a durable operation that survives interruption and exposes live progress plus polling fallback.
- [ ] Load a trusted plugin that contributes module capabilities, a declared permission, and an MCP tool.
- [ ] Demonstrate Hoard as optional while keeping Hoard standalone.
- [ ] Provide tested PostgreSQL and SQLite integrations, resource manifests, plugin SDK documentation, and deployment instructions.
- [ ] Pass dependency license and vulnerability checks with no mandatory commercial-license packages.

---

**Document summary:** Köfte's MVP is a .NET modular-monolith IDP with a modern Next.js developer UI, per-module storage isolation (**PostgreSQL schema / SQLite database file**), declarative resources, provider-driven orchestration, durable operations, pluggable authentication and authorization, trusted/external plugins, and MCP integrated from the outset.
