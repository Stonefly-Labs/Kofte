# Implementation Plan: 003 - Storage Registry and Module Isolation

**Branch**: `feature/003-storage-registry` | **Date**: 2026-10-21 | **Spec**: [spec.md](spec.md)
**Input**: `specs/003-storage-registry/spec.md`
**Constitution**: Adopted v2.0.1
**Status**: Design only; no storage implementation, dependency adoption or human acceptance.

## Summary

Give each validated module identity one immutable, durably unique storage
assignment, without letting manifest text choose physical targets. A platform
registry in `kofte_system` or the existing separate SQLite platform file owns
reservation, provisioning and reconciliation. Modules own their EF contexts,
tables and schema-local/per-file migration histories. Registration is trusted,
offline installation maintenance; resolution is available to the running host.
The zero-module bootstrap and pure manifest validator remain usable.

Use deterministic allocator v1 plus actual database constraints, the existing
installation maintenance lock, verified physical ownership markers and
fail-closed restart recovery. A separate offline PostgreSQL provisioner creates
schemas; **the existing runtime role never receives database CREATE**.
[Proposed ADR 0005](../../docs/adr/0005-module-storage-ownership.md) records these
consequential choices. It needs human approval before implementation, not before
publication of this design checkpoint.

## Technical Context

**Language/Version**: Existing .NET SDK 10.0.402/runtime 10.0.13, `net10.0`,
nullable C#, deterministic builds and warnings-as-errors. Python 3.11+ for
existing verification. No frontend baseline change.

**Primary Dependencies**: Retain Microsoft.Data.Sqlite 10.0.13 and Npgsql 10.0.3.
Propose EF Core/Relational/Sqlite 10.0.13 and
Npgsql.EntityFrameworkCore.PostgreSQL 10.0.3 for actual module-owned models and
migrations, not an invented ORM. EF Design 10.0.13 is development-only if needed
for scaffolding; no global `dotnet-ef` installation is required by the guide.
Official package metadata supports this version combination. All added direct
and transitive packages, native SQLite assets, notices, exact locks and tool
provenance must pass the existing dependency review **before adoption**.
No new package is installed or approved by this plan; see [research](research.md).

**Storage**: PostgreSQL 17, retaining the owned disposable image/digest and
loopback fixture restrictions; platform schema `kofte_system`, schema per module.
SQLite: existing `platform/platform.db` plus
`modules/<allocator-key>/module.db` under the private canonical installation
root. Explicit `DELETE` journal mode, `synchronous=FULL`, foreign keys on,
private sidecars, no shared cache/pool/`ATTACH`; no network filesystem support.

**Testing**: Existing xUnit projects/runner plus an integration project dedicated
to infrastructure and an independent storage consumer under `tests/contract`.
Real providers, separate processes, forced collisions, crash-phase recovery,
EF model/history isolation, public-consumer privacy and executable guide cases.
Wire into existing named `unit`, `integration`, `contract`, `recovery`, `privacy`
and `quality` suites/CI, not a parallel test framework.

**Target Platform**: Existing macOS 15+ arm64 and Ubuntu 24.04 x64. Local SQLite
needs no Docker, Kubernetes or cloud. PostgreSQL tests retain explicitly owned
disposable targets; production profile/deployment design remains 022.

**Project Type**: Minimal provider-neutral platform contracts; one justified
`Kofte.Infrastructure` adapter assembly for registry/provisioning/EF binding;
host composition and local maintenance integration. No module runtime, new
HTTP/UI/MCP surface or product feature module.

**Performance Goals**: Preserve SC-001-006, particularly 20 repetitions of ten
concurrent callers, three restarts, two independent module consumers/provider,
zero unauthorized writes/disclosures and successful documented recovery.
Record actual revision, pinned tools, OS/architecture and machine conditions;
no fixed CPU/RAM benchmark or manual timed exercise.

**Constraints**: Resolution 3 seconds; maintenance lock wait 2 seconds;
registration/provisioning/registry upgrade 30 seconds; one module migration or
recovery verification 120 seconds, including lock acquisition and cleanup.
Provider command/connect limits never exceed remaining budget. Details,
SQLite synchronous-call supervision and diagnostic caps are in
[storage-v1](contracts/storage-v1.md). No universal deadline promise for a
kernel-blocked disk; such infrastructure is unsupported, not a passing test.

**Scale/Scope**: One installation, one explicitly selected provider, canonical
002 IDs and immutable storage need. Installation-wide ownership is not
team/resource authorization. Module lifecycle/dependency ordering is 004,
identity/policy 005/006, MCP 007/013, durable audit 009, reference module 020
and deployment/automated whole-installation backup tooling 022.

## Constitution Check

**Pre-research gate: pass.** The clarified spec, accepted 001/002 baseline and
canonical sources establish the binding invariants before technical research.
No requirement waiver or canonical conflict is needed. **Post-design gate:
pass at design level**, not behavioral evidence. Human ADR approval, reviewed
requirements-quality checklist and actual implementation evidence remain gates.
`.specify/extensions.yml` is absent: no before/after planning hook is configured.

| Principles | Design evidence / decision | Before research | After design |
| --- | --- | --- | --- |
| I, XII: Brand, optional modules, exact inventory/dependencies | Only 003; accepted/merged 001/002; Köfte prose and technical `kofte`; zero-module host retained. | Pass | Pass: summary, scope, same feature branch/draft PR #6 |
| II, III: Minimal modular contracts, immutable IDs, native DI, trusted code, test ownership | Reuse 002 identity/validated descriptions; versioned storage contract; adapters not added to pure parser; tests physically in their test projects. No MediatR/AutoMapper, loader or hostile-code sandbox. | Pass | Pass: source layout, storage-v1 contract |
| IV, V: Registry, SQL-safe 63-byte/reserved/UNIQUE/concurrency protection, schema/file/history isolation, honest transactions | Persist immutable allocator output; database uniqueness; exclusive maintenance; physical ownership tokens; module-local EF contexts/history; no cross-module access/ACID. | Pass | Pass: data model, allocator, migrations and recovery contracts |
| VI, VII: Authority, least privilege, safe diagnostics, consistent future adapters | Trusted offline mutations only; separate provisioning credential; owner-bound resolution; bounded allowlisted telemetry. No public operation, role or tool before 005/006/007. | Pass; new user/MCP policy N/A | Pass: capability binding, no credential exposure, future parity retained |
| VIII: Durable work and provider neutrality | Durable provisioning intent and explicit recovery, not a job/event/workflow runtime. Resource controllers, events, jobs and SignalR are not introduced. | Pass; later runtimes N/A | Pass: state transitions and finite retry, no exactly-once/global rollback |
| IX: Accessible UI/trusted frontend | No new visual journey or frontend SDK; existing bootstrap/web/browser behavior retained. | Pass; new UI N/A | Pass: regression matrix; no invented UI/accessibility result |
| X, XI: Portability, config, telemetry, migrations, recovery, real evidence | Both real providers; current target/fixture safeguards; explicit upgrade, stopped recovery and legacy-command refusal; public/negative/crash/concurrency evidence required. | Pass | Pass: quickstart and acceptance matrix below |
| Governance: ADRs, licensing, traceability, human review | Proposed ADR, no package adoption/quality checklist/task marking; one stage, one branch/PR; no intermediate merge or self-acceptance. | Pass | Pass: research/ADR, ledger and publication checkpoint |

## Project Structure

### Documentation (this feature)

```text
specs/003-storage-registry/
  spec.md
  plan.md
  research.md
  data-model.md
  quickstart.md
  contracts/storage-v1.md
  contracts/migrations-and-recovery.md
  checklists/requirements.md
docs/adr/0005-module-storage-ownership.md
```

Quality checklist, tasks, analysis, implementation and acceptance are later
stages, deliberately not generated here.

### Source Code (repository root; future implementation paths)

```text
src/Kofte.Platform/Modules/Storage/             # Provider-neutral versioned types
src/Kofte.Infrastructure/
  Kofte.Infrastructure.csproj
  Storage/
    Registry/                                 # Register/resolve, durable state
    Registry/Migrations/                      # Versioned provider-specific SQL
    Allocation/                               # Allocator v1 and reserved rules
    Postgres/                                 # Registry/provisioning/lock adapter
    Sqlite/                                   # Canonical paths and file adapter
    Migrations/                               # Assigned EF context/migration binding
src/Kofte.Host/
  Program.cs                                  # Composition; existing routes unchanged
  Bootstrap/                                  # Shared safety helpers/lease delegation
  Features/StorageMaintenance/                # Explicit local registry commands
tests/unit/Kofte.Platform.Tests/Modules/Storage/
tests/unit/Kofte.Host.Tests/Storage/
tests/integration/Kofte.Storage.IntegrationTests/
  Kofte.Storage.IntegrationTests.csproj
  Registry/
  Providers/
  Recovery/
  Modules/Alpha/                              # Own contexts and migrations
  Modules/Beta/                               # Own contexts and migrations
tests/contract/Kofte.Platform.ContractTests/Modules/Storage/
tests/contract/Kofte.Storage.Consumer/         # Independent public-API process
tests/integration/Kofte.Host.IntegrationTests/ # Existing owned fixture extension
scripts/verify.py                              # Required projects and named suites
scripts/tests/                                # Verification fail-closed regressions
```

**Structure Decision**: A module author must not reference the web host for
provider integration, nor make the pure `Kofte.Platform` manifest parser depend
on EF. One infrastructure assembly is therefore a demonstrated common adapter
boundary, not a speculative layer. Move/reuse the existing Unix path/lease
mechanism there as necessary, preserving host delegation and behavior; do not
duplicate it. Existing fixture resource ownership can be factored into a
test-only helper project if cross-project reuse requires it; do not copy cleanup
logic or place tests/helpers in production. No runtime module/worker/SDK project.

## Design, Migration and Recovery

| Requirements | Owner and design | Observable verification |
| --- | --- | --- |
| FR-001/002/009/016 | Platform contract v1 consumes validated 002 descriptions, separates offline registry authority from bound module access; no-storage is explicit. | US1.1/3/4, US3.1/3; two public consumers and forged-owner rejection |
| FR-003/004/005 | Registry stores allocator version/output, UUID ownership token and binary/ordinal unique module/location keys. Exclusive maintenance and transactional conflict classification; forced candidate seam only in tests. | US1.2/4, US2.1-4; direct constraint bypass and reopen consistency checks |
| FR-006/008 | Registry-only relative SQLite locator, exclusive create, canonical component/link/sidecar checks; ready stores open read-write without create, verify marker and integrity. | US3.3/4; missing/corrupt store and every path/alias case leave targets unchanged |
| FR-007/011/012 | Commit reservation before physical work; ownership marker before ready; distinct incomplete/reconciliation states. PostgreSQL physical DDL/marker transactional; SQLite has explicit non-atomic registry/file boundary. | US4.1/2; kill before/after every durable boundary and resolve/retry after restart |
| FR-005/009/010/013 | Offline provisioning credential, runtime schema owner without database CREATE; assigned EF model/cache/history/migration binding; installation lock held throughout migration. | US3.1/2, US4.2/3; denied provisioning, schema literal/foreign-target rejection and one-module upgrade |
| FR-010/015 | Explicit v0 table-free to registry v1 upgrade; future versions blocked. Legacy platform-only backup/restore/reset safely refused for registry installations. Stopped coherent recovery set, no automated destructive adoption. | US4.3/4; interrupted upgrade, fresh recovery, stale/mixed set and unsafe reset refusal |
| FR-013/014/016 | Explicit provider/root/credential sources; bounded outcomes, safe records and existing health projections; no raw paths/provider exceptions. | US4.2, US5.1/2; captured secrets/control characters, outage/cancellation and corrected retry |

The [data model](data-model.md) defines constraints and durable state. The
[storage contract](contracts/storage-v1.md) fixes allocation, authority, budgets
and outcome semantics. The [migration/recovery contract](contracts/migrations-and-recovery.md)
specifies the particularly important EF schema binding, SQLite creation gap,
bootstrap upgrade and operational compatibility rules.

Registration/migration is **offline**: PostgreSQL takes the exclusive form of
the existing `0x6b6f667465000001` advisory lease; the serving host keeps its
existing shared lease. SQLite takes the existing `.platform.lock` exclusively;
the serving host keeps its existing exclusive lease. Thus live hosts block
maintenance rather than racing it. Independent maintenance processes serialize
or return bounded busy. Later 004 can compose these operations during startup
before serving, without inventing a new global migration runner in 003.

No dependency order is inferred here: each module migration runs only under a
caller-held installation scope; the general dependency graph/enablement runner
belongs to 004. A migration failure never enables a module or silently clears
its history. Ordinary module data transactions stay in that module's store.

## Validation and Operational Handoff

All new product commands/cases in [quickstart](quickstart.md) are **future
implementation targets**, not executed planning evidence. Repository checks
run for this checkpoint are reported in the PR.

| Acceptance coverage | Required evidence and category |
| --- | --- |
| US1.1-4; SC-001/003 | Both-provider integration/public consumers: distinct Alpha/Beta and platform stores, no-storage has zero stores, unchanged assignment/data over three restarts and display/release changes; reject provider/identity/location/storage-need mutation |
| US2.1-4; SC-002/005 | 20 x ten independent callers for same and distinct IDs, including independent processes; allow bounded busy then retry, inspect final single-owner rows and actual objects; forced collision/reserved/unowned candidate, direct duplicate writes and corrupted mapping reopen |
| US3.1/2; SC-001 | Identically named module tables and history; clean install and Alpha-only additive upgrade, Beta and platform untouched; same context type/different assignment model-cache test, real history and generated operation inspection, installation-lock race |
| US3.3/4; SC-003/005 | Traversal, absolute/foreign schema/path, symlink ancestor/file/sidecar, hard link/non-regular target, forged/replayed/other-owner scope, missing/corrupt ready store; assert zero forbidden-target changes and no implicit creation |
| US4.1/2; SC-004 | Crash immediately before/after reservation, object/marker commit, ready commit, migration start/completion and upgrade commit; lost response, denied privilege, lock/outage/cancellation and abandoned EF lock; bounded truthful result and ownership-verified retry/block |
| US4.3/4; SC-004/006 | Real v0 bootstrap upgrade on both providers, v1 idempotence, future/corrupt version block, failed upgrade/data retention; stopped full-set restore and mixed/stale set refusal; legacy backup/restore/reset cannot orphan mappings |
| US5.1/2; SC-005/006 | Public result/log/span/metric/stdout/stderr capture with credential/private-path/payload/control-character canaries on every outcome; fixed guidance and corrected retry without deleting unrelated data |
| FR-016; SC-006 | Execute documented examples through independent consumers on both providers; retain pure manifest, host health/OpenAPI/web/startup/browser behavior and owned fixture restrictions; no new endpoint or permissions |
| Principles III/X/XI/XII | Required projects/tests cannot be skipped or select zero tests; reviewed locks/licenses; actual revision/tools/environment/elapsed evidence; full gate before feature acceptance, not inferred from this plan |

## Complexity Tracking and Decisions

| Proposed complexity or unresolved choice | Need and simpler alternative | ADR / human decision |
| --- | --- | --- |
| One infrastructure assembly and small EF assignment binder | Modules need provider adapters without referencing host or contaminating pure contracts. Model defaults/history alone do not bind migration operations. | Proposed ADR 0005; approval required before implementation |
| Separate offline PostgreSQL provisioner | Runtime cannot create schemas and must retain that restriction. Granting it database CREATE or relaxing the probe would contradict FR-013. | Proposed ADR 0005; no constitutional amendment |
| Ownership markers, durable phases and explicit recovery set | SQLite registry/file creation is not atomic; hashes cannot prove ownership and copying only the registry can orphan data. Blind adoption/global rollback rejected. | Proposed ADR 0005; actual crash/recovery proof still required |

No unresolved technical placeholder or canonical conflict remains. Proposed
choices are not human approval; reviewer-owned quality review, tasks, analysis
and implementation remain subsequent invocations on the same draft PR.
