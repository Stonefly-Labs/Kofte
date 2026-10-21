# Köfte — Product Vision

> **Status:** Foundational draft v0.1 | **Date:** 2026-10-20  
> **Brand:** **Köfte** in all human-facing language; `kofte` only for technical identifiers.

## Vision statement

**Köfte is the internal developer platform you assemble, not the platform you must conform to.** A beautiful, extensible self-hosted home for developer services, teams, platform resources and automations—usable from its web UI, APIs and an MCP-enabled coding assistant.

## The future we want

A platform engineer installs a small Köfte host, connects identity, creates teams, and enables only the capabilities the organization needs. Developers can find a service, see who owns it, request a resource, launch a guarded workflow and inspect progress in seconds. The same operations are available consistently in the web console and through IDE assistants using the platform's MCP server.

As organizational needs evolve, operators install optional modules rather than fork the platform. A community author can add a new resource provider, bespoke UI screens, background jobs, events, permissions and MCP tools with a supported public contract. An organization can use a direct infrastructure executor, delegate to CI pipelines, or do neither. Hoard can be integrated as a module by choice, but Köfte remains independently useful.

## Positioning

- **Category:** open-source, composable internal developer platform.
- **For:** platform teams that value customization, operational control and low-friction adoption; development teams that need a single coherent place to work.
- **Against:** one-off internal portals, hard-to-maintain glue, and IDPs that hard-code a stack or hide extension seams.
- **Promise:** start small, compose freely, extend deeply, and keep the developer workflow in familiar tools.

## Personas and jobs to be done

| Persona | Job to be done | Value |
|---|---|---|
| Application developer | Discover existing services, owners, environments and allowed actions; invoke workflows via browser or IDE | Fewer handoffs and less context switching |
| Platform engineer | Define paved roads and permissions, register modules, standardize repeatable actions | Consistency without limiting local needs |
| Module author | Package a feature spanning UI/API/data/events/providers/MCP using stable contracts | Ecosystem opportunity without core forks |
| Security / operations | Determine who can invoke which action, inspect audit history and system health | Governability and trustworthy operation |
| Engineering leader | Understand service ownership and adoption across teams | Transparency without requiring a monolithic platform rollout |

## Experience pillars

### 1. A fast, delightful portal

Search-first navigation; legible service/project/resource pages; fast client transitions; keyboard-friendly commands; consistent empty/loading/error states; polished visual design and accessible components. Great UX is part of the extensibility contract, not cosmetic work added last.

### 2. A platform made of modules

Every major optional capability is a module. Modules declare their IDs, versions, dependencies, capabilities and migration needs; operators can enable, disable and configure them. Extensions can bring UI, HTTP endpoints, background handlers, events, workflow steps, permissions, resource kinds, provider adapters and MCP tools. Platform-owned contracts remain stable and secure.

### 3. One permission model across every interface

Whether a user clicks a button, calls REST or invokes an MCP tool from a coding assistant, the underlying operation checks the same effective permissions, team/resource scope and audit policy. No MCP bypass paths.

### 4. Declarative where useful, conventional where clear

Keep human concepts intuitive—teams, services, projects, environments—while making extensible resources declarative, versioned and reconciled through lifecycle/status transitions as appropriate. Avoid forcing every entity through a generic key-value resource abstraction.

### 5. Scale by composition, not compulsory infrastructure

A lightweight installation works locally with SQLite and minimal prerequisites. A multi-team deployment can use PostgreSQL and Kubernetes. Optional modules integrate providers, secrets backends, CI/CD and IaC tooling without mandating one stack.

## Signature journeys

1. **Discover:** A developer searches Köfte for a service, sees ownership and available environments, and navigates to supported actions.
2. **Request:** A developer submits a validated, declarative resource request; Köfte authorizes it, stores intent and shows job/status progress.
3. **Extend:** A platform engineer installs a module; Köfte verifies identity/compatibility, allocates storage, applies module migrations and exposes the contribution in the UI and API.
4. **Use from an IDE:** A coding assistant discovers permitted Köfte MCP tools and fetches a service or starts a guarded action on behalf of an authenticated user.
5. **Choose infrastructure workflow:** A provider module executes directly or generates/delegates work to a pipeline, then reports normalized operation status.
6. **Add Hoard:** An administrator enables the separately maintained Hoard integration module, gaining package caching views/actions inside Köfte without altering Köfte core.

## Differentiation

- **Level-3 modularity** spanning backend, UI, workflow, events, provider model, permissions and MCP—not merely dashboard plugins.
- **Early assistant integration** as a first-class access surface, with secure module-contributed tools.
- **Small-to-serious hosting** with real database isolation: SQLite database *per module*; PostgreSQL schema *per module*.
- **Non-opinionated infrastructure automation** with both direct and pipeline-based fulfillment.
- **Clear extension story** that allows Hoard and other independently hosted capabilities to coexist with native modules.

## Release horizons

### Horizon 1 — prove the platform (MVP)

Core entity/permission model, portal shell, module lifecycle, module isolation and migrations, sample module, early MCP read/actions, basic resource catalog, durable jobs, an event contract, operational diagnostics, SQLite and PostgreSQL, and K8s deployment guidance.

### Horizon 2 — grow the ecosystem

Module distribution experience, richer workflow adapters, additional auth/provider integrations, external MCP-server federation patterns, templates, resource reconciliation improvements, and optionally Hoard's module integration.

### Horizon 3 — advance governance and reach

Multi-organization tenancy, more extensive policy/approval/secret integrations, richer observability/analytics, advanced workflow backends, curated ecosystem, and a possible AI assistant built on proven MCP capabilities.

**Horizon items are directional, not delivery commitments.**

## Non-goals and product boundaries

Köfte is not required to be a source-code forge, package registry, IaC engine, Kubernetes-only management UI, workflow SaaS, chat assistant, or all-in-one observability platform. Those capabilities can integrate or emerge as modules if they add value.

It will not promise transactional atomicity across separate module databases, arbitrary untrusted in-process plugin execution, or seamless portability of every cloud-specific provider feature.

## How we will know it works

An independent developer can implement and install a module without editing Köfte core; a team can use the same permissioned operation via UI, REST and MCP; admins can identify the owner and state of every resource; workflows recover from restarts; upgrades do not accidentally share or collide on module data; deployment can run without Azure-specific dependencies.

## The one-line test

**Does this change help Köfte stay composable, trustworthy, pleasant to use and independently useful?** If not, reconsider whether it belongs in the core.

---

See [`product-charter.md`](product-charter.md) for approved scope and [`architecture.md`](architecture.md) for implementation contracts. Use this vision when writing feature specifications and prioritizing the roadmap.
