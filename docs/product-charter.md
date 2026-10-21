# Köfte — Product Charter

> **Status:** Foundational draft v0.1  
> **Date:** 2026-10-20  
> **Project:** Köfte (human-facing spelling always includes **ö**; technical identifiers may use `kofte`)  
> **License:** MIT (project intent; confirm repository LICENSE at initialization)

## 1. Mandate

Build **Köfte**, an open-source, self-hostable, composable internal developer platform (IDP) that gives organizations a coherent developer experience without forcing a fixed infrastructure stack, cloud vendor, authentication provider, or set of platform features. Start with a useful lightweight core and let administrators and community developers assemble capabilities as installable modules.

The **platform is the product**; infrastructure provisioning, package caching, catalogs, deployment integrations, and similar capabilities are optional modules, not mandatory assumptions. **Hoard** remains an independent project that can optionally be hosted as a Köfte module.

## 2. Problem

Platform teams repeatedly integrate catalogs, service ownership, environments, workflows, templates, access policies, toolchains, and operational views into fragile, bespoke portals. Existing IDPs may be too prescriptive, difficult to extend deeply, or costly to operate for smaller teams. Developers increasingly expect platform capabilities to be accessible both in a browser and from IDE-based assistants.

## 3. Audience and stakeholders

- **Primary:** platform engineers / administrators who operate and configure Köfte; application developers who consume capabilities; community module authors.
- **Secondary:** engineering leaders, security teams, SRE/operations, and infrastructure specialists.
- **Initial tenancy:** multiple teams within one installation/organization; model boundaries so true multi-organization tenancy can be introduced later.

## 4. Mission and differentiators

1. **Composability by design.** Keep the host small; modules contribute UI, backend operations, resource types, events, workflow actions, providers, permissions, and MCP tools.
2. **Lightweight-to-enterprise progression.** Support a small self-hosted installation without forcing Kubernetes or external managed services, while treating Kubernetes as a first-class production target.
3. **Hybrid domain/resource model.** Combine recognizable platform concepts (teams, projects, services, environments) with declarative, versioned resource definitions.
4. **Extensible delivery integrations.** Support direct execution and CI/CD-orchestrated workflows via optional provider modules; do not hardwire OpenTofu, Terraform, or a single pipeline provider.
5. **Assistant-native access.** Ship an MCP server **early in the MVP**; expose authorized capabilities as discoverable tools and allow vetted modules to contribute tools or integrate external MCP servers.
6. **Portable foundations.** Provider-neutral runtime, pluggable authentication, built-in authorization contracts, PostgreSQL or SQLite storage strategy, and no compulsory Azure dependency.
7. **Excellent UX.** A fast, polished responsive web interface, with the extension surfaces and design system treated as product APIs.

## 5. Outcomes and success measures

These are **proposed initial targets**, to be refined once implementation starts; they are not existing performance claims.

| Outcome | Proposed validation signal |
|---|---|
| A developer can accomplish common platform tasks unaided | End-to-end task tests in UI and MCP for discover/list/create/status workflows |
| Platform engineers can extend without patching the core | An independently packaged example module adds API, UI, permission, and MCP tool |
| A small installation remains approachable | Documented local startup with SQLite, simple auth option, and minimal services |
| A team installation is maintainable | Repeatable PostgreSQL + Kubernetes deployment, health checks, upgrades, backups |
| Platform boundaries prevent cross-team leakage | Negative authorization/integration tests for team-scoped resources and tool calls |
| Extensibility does not destabilize the host | Module validation, version compatibility tests, and module lifecycle diagnostics |

## 6. MVP scope

**Core platform**
- Host API and modern web console; identity integration and platform permission enforcement.
- Team/project/service ownership model and basic platform resource catalog (with extension-defined types).
- Module manifest, discovery/registration, installation/configuration, compatibility checks, and lifecycle contracts.
- Level-3 extension points: backend endpoints/handlers, frontend navigation/pages/widgets, events, workflow actions, providers, permissions, and MCP tools.
- Declarative resource definitions with validation, lifecycle/status, and auditable changes.
- Lightweight **durable** background job/workflow primitives with retries, idempotency, and observable state.
- Early MCP server exposing core discovery/read operations and safe, permissioned actions; plugin-supplied tool registration.
- Per-team scoping, basic roles/permissions, audit trail, structured telemetry and diagnostics.
- SQLite for local/small deployments and PostgreSQL for shared installations, with explicit per-module isolation rules.
- Kubernetes deployment assets, plus a documented straightforward non-Kubernetes local path.

**Reference implementations (thin, not mandatory platform dependencies)**
- Example module covering a full vertical slice, UI, event, permission and MCP tool.
- At least one example external provider integration and one example declarative resource.
- Illustrate how optional **Hoard** hosting would work without coupling the Köfte core to Hoard.

## 7. Explicitly outside the first MVP

- Full cross-organization multi-tenancy, billing or marketplace monetization.
- A built-in AI chat assistant/agent experience (MCP interfaces arrive first).
- A mandatory infrastructure-as-code runtime or opinionated cloud provisioner.
- A comprehensive enterprise workflow designer or full temporal orchestration engine.
- A hosted SaaS control plane as the required operating model.
- Supporting every database, IdP, build system, source host or cloud provider at launch.
- Guaranteed isolation of hostile third-party code **inside** the host process; external modules require an explicit trust/security model.

## 8. Product principles / guardrails

- **Core contracts over core features:** preferentially ship extension interfaces and working examples.
- **No mandatory vendor lock-in:** abstractions must be honest about capabilities and limitations.
- **Security isn't optional:** default-deny actions, scoped authorization, secret redaction, audit and tool-level permission checks.
- **Vertical slices first:** implement features end-to-end; reserve shared layers for true cross-cutting platform concerns.
- **Operational simplicity:** avoid adding brokers, workflow engines or extra databases until the use case warrants them.
- **Compatibility is a promise:** version public module manifests, APIs, events, resource schemas and MCP tool contracts.
- **Community-friendly licensing:** prefer MIT/Apache-2.0/BSD compatible dependencies, review transitive obligations, and avoid surprise commercial licenses.

## 9. Major constraints and agreed architecture decisions

- **Name:** Köfte in people-facing documents; `kofte` for CLI/package/repository/URL identifiers.
- **Backend:** .NET, modular monolith plus independently hosted integrations where warranted; vertical slice architecture within modules.
- **UI:** Next.js + TypeScript with a cohesive high-quality component system.
- **Database:** PostgreSQL uses **one named schema per module**, and a registry must reserve/collision-check names; SQLite uses **one database file per module**.
- **Messaging:** in-process events initially, with an evolution path to durable/out-of-process transport; module contract must not depend on a specific broker.
- **Realtime:** ASP.NET Core SignalR by default rather than Azure Web PubSub as a required service.
- **Infrastructure modules:** support both direct execution and delegated CI/CD orchestration.
- **MCP:** not deferred; must be usable early enough to shape platform and extension APIs.
- **Deployment:** Kubernetes first-class; portable and local deployments also supported.

## 10. Risks and mitigation

| Risk | Mitigation |
|---|---|
| Plugin API changes become breaking churn | Contract versioning, example modules, compatibility matrix, integration tests |
| Modules collide on storage identifiers or resources | Immutable module IDs, centrally allocated schema/file names, startup preflight validation |
| MCP creates an expanded attack surface | Explicit tool exposure policies, authorization at invocation, least-privilege scopes, audit, consent/confirmation for dangerous operations |
| Multi-database operations imply false atomicity | No distributed transaction guarantee; outbox/inbox, idempotency, compensation where required |
| Host becomes too complex for individual operators | Minimal profile and optional external services; platform capabilities are additive |
| Embedded third-party modules jeopardize stability | Trusted in-process modules only; out-of-process adapters/MCP for less-trusted capabilities |

## 11. Delivery sequence and acceptance gates

1. **Foundation:** repo, architecture contracts, app shell, local storage, identity/authorization, module identifiers and lifecycle.
2. **MCP-first vertical slice:** core resource query through UI/API/MCP, one sample module extending all surfaces, tool-level authorization and audit.
3. **Useful MVP:** service/project/team catalog, declarative resources, lightweight workflows, background jobs, events, notifications and provider examples.
4. **Operability:** PostgreSQL schema registry and upgrades, SQLite module database management, observability, Kubernetes packaging, release/security processes.
5. **Ecosystem:** external MCP integration, richer module distribution, third-party sample apps, and optional Hoard integration.

**MVP exit criteria:** documented self-host installation; functional API/UI/MCP path; demonstrable independently developed module; tested per-module database isolation; authorization/audit regression suite; reproducible deployment/upgrade; no mandatory proprietary cloud components.

## 12. Governance and decisions to resolve

Record consequential choices as Architecture Decision Records (ADRs). Open items: distribution/hosting format for untrusted modules, module migration/rollback policy, specific initial auth providers, exact minimal workflow persistence mechanism, module UI loading/bundling mechanism, and first-class resource API schema. None should be silently treated as finalized.

## 13. Spec Kit usage

Treat this charter as the boundary for `/speckit.constitution` and feature planning. Features are independently specifiable slices; each `/speckit.specify` request should identify user value, module/core ownership, extension points, RBAC impact, data storage and migration, MCP surface, testing, and operational implications. Pair this with [`product-vision.md`](product-vision.md) and [`architecture.md`](architecture.md).
