# Implementation Plan: 001 - Repository Bootstrap and Developer Experience

**Branch**: `feature/001-specify-repository-bootstrap` | **Date**: 2026-10-20 | **Spec**: [spec.md](spec.md)
**Input**: `specs/001-repository-bootstrap/spec.md`
**Constitution**: v2.0.0; product principles unchanged from adopted v1.0.0
**Status**: Design complete; no product implementation or feature acceptance

## Summary

A new contributor starts a truthful, accessible Köfte page and a real .NET host
using one documented local command after explicit setup. A maintainer verifies
the same checks locally and in CI, including real SQLite/PostgreSQL persistence,
HTTP contracts, browser behavior and safe recovery (US1-4, FR-001-017).

Build a small ASP.NET Core host and Next.js app, sharing only bootstrap contract
schema/types. A Python supervisor owns native child processes; containers are
only a PostgreSQL test dependency. No application tables, module registry,
identity, MCP, jobs, catalogs or production deployment are introduced. Inventory
001 remains unfinished until implementation, review and explicit human acceptance.

## Technical Context

**Language/Version**: C# / .NET 10 LTS, SDK 10.0.402, runtime 10.0.13;
Node 24.22.0 LTS / npm 11.19.1; Next.js 16.4, React 19.3, TypeScript 6.0.
Exact secondary package patches and compatible peers are pinned during adoption,
with locked restores; see [research R1](research.md#r1-supported-reproducible-toolchain).

**Primary Dependencies**: ASP.NET Core native DI/health/OpenAPI,
Microsoft.Data.Sqlite, Npgsql, OpenTelemetry; Tailwind 4 and minimal
shadcn/Radix/Lucide UI; xUnit, Vitest/Testing Library, Playwright/axe,
OpenAPI TypeScript generation and JSON-schema contract validation.
License/provenance review of the real direct/transitive graph is mandatory before
adoption. No MediatR, AutoMapper, cloud SDK, generic repository or mediator.

**Storage**: Real platform SQLite file `data/platform/platform.db`;
PostgreSQL 17 disposable database with `kofte_system` schema. No shipped tables
or migrations. Fixture ownership and recovery are specified in [data-model.md](data-model.md).

**Testing**: Repository Python checks, .NET unit/integration tests, HTTP contract
tests, frontend unit tests and Playwright Chromium. Named suites and exact
future commands are in [quickstart.md](quickstart.md); none ran during planning.

**Target Platform**: Local macOS 15+ (arm64) and Ubuntu 24.04 (x64);
Linux CI runs all automated suites. Native Windows is not claimed in 001.
Reference timing environment: Ubuntu 24.04 x64, 4 logical CPU cores, 8 GiB RAM,
SSD, installed supported tools, restored dependencies, built applications,
loopback network, no unrelated load. Record actual CPU/runner metadata in evidence;
this describes a measurement target, not a measured result. Kubernetes and
production packaging remain 022/024.

**Project Type**: Platform-owned modular-monolith composition root plus web shell
and repository developer tooling. Host bootstrap features remain local vertical
slices; no optional modules or speculative platform/infrastructure assemblies.

**Performance Goals**: SC-001 ready within 5 minutes after setup; SC-002 usable
page within 2 seconds of navigation in 10 warm starts; FR-004/SC-004 each status
attempt ends within 10 seconds, refresh at most every 30 seconds, outage visible
within 40 seconds. Measure these on the reference environment.

**Constraints**: Loopback defaults; no business records or fake identity;
no secret/path/stack disclosure; no provider fallback or implicit reset;
bounded database/request/startup timeouts and owned-process cleanup.
WCAG 2.2 AA applicable content, 320-1440 CSS px, keyboard/focus/announcements,
zero serious/critical automated accessibility findings (FR-011, SC-005).

**Scale/Scope**: One developer installation, two runtime processes, one
platform store, no tenant/team entities. PostgreSQL is a verification profile,
not a supported shared production deployment. Exclusions/deferred constitutional
proof remain those in the specification; no post-MVP work.

## Constitution Check

**GATE: Complete before research and re-check after design.**
For each row record applicable principle IDs, evidence and pass/block, or justified
N/A. A complexity note cannot waive a MUST; unresolved conflicts block progression.

| Principles | Design evidence / decision | Before research | After design |
| --- | --- | --- | --- |
| I, XII: Köfte brand, optional modules, exact inventory/MVP scope and dependencies | FR-001/004/017; module-free skeleton and explicit exclusions; no acceptance/MVP claim | Pass: approved 001 scope | Pass: this plan, contracts and docs preserve scope |
| II, III: Modular monolith, immutable IDs, versioned/collision-checked level-3 contracts, safe lifecycle, trusted code, module-local slices and direct DI; no MediatR/AutoMapper | One composition root, slice-local handlers/tests, one genuinely shared contract package. Identity/lifecycle/extension proofs N/A here: no modules; deferred 002-004/014/020 | Pass: no speculative SDK needed | Pass: source map below; no sandbox or hot-load claim |
| IV, V: Persisted registry, SQL-safe 63-byte/reserved/UNIQUE/concurrent schema allocation; module-owned context/history; canonical SQLite file per module/platform; no cross-module tables/ACID | FR-007/015/016 and data model. Platform file/schema only; fixture round trips/recovery on both providers. Module allocators/EF contexts/history N/A until 003; no cross-module operation | Pass: ownership retained, registry excluded | Pass: restricted-root checks, explicit init, no app schema or false module proof |
| VI, VII: OIDC/bootstrap, deny-by-default actor/team/resource permissions, shared REST/UI/MCP/jobs/providers/realtime operations, safe tools, audit and integration controls | FR-008-010; exact anonymous diagnostic allowlist, fixed readiness adapter, safe logs. OIDC, team policy, MCP/tool audit N/A: no domain action; deferred 005-007/009/013 | Pass: no privileged anonymous capability | Pass: HTTP/config contracts and negative tests; no fake actor |
| VIII: Versioned resources and separate status; durable events/jobs, retries, idempotency, cancellation/recovery; optional providers; SignalR/poll fallback | N/A: no resources, jobs, events, external integrations or subscriptions in 001; 012/016-019 remain required. Browser health polling is not a job/realtime contract | Pass: explicit exclusion | Pass: no invented persistence/workflow or provider dependency |
| IX: Accessible consistent UI/states; versioned trusted build/deployment-time frontend extensions, no remote hot-load safety claims | FR-004/011; UI state/accessibility contract, required browser/manual evidence; full design system/extensions N/A until 008/014 | Pass: measurable scope already specified | Pass: bounded states, fixed-origin data, narrow UI baseline |
| X, XI: Portable profiles, config/secrets, telemetry, backup/restore/upgrade; required real-provider, contract, Playwright, negative/recovery and smoke evidence | FR-002-007/009-016; native SQLite and real PostgreSQL tests, opt-in OTLP, explicit backups and mandatory suites; Kubernetes/reference-module release proof deferred 020/022/024 | Pass: relevant evidence categories identified | Pass: quickstart and coverage matrix require evidence, not mock/skip success |
| Governance: Canonical traceability, recommendations versus locked rules, ADRs, license review, human review/acceptance gates; one branch/PR per feature with no intermediate merge | ADR 0002 proposed; R1-R6 resolve design choices; locked graph/license review before adoption. Same branch/draft PR #4, one stage only | Pass: no canonical conflict found before research | Pass: no unresolved design conflict; checklist and explicit acceptance still human-owned |

These are **design-conformance** results, not proof of implemented behavior.
The pre-research assessment used the already clarified spec and constitution;
the post-design assessment checks this plan and its linked artifacts. No MUST
is waived. No before/after plan hooks are configured.

## Project Structure

### Documentation (this feature)

```text
specs/001-repository-bootstrap/
  spec.md
  plan.md
  research.md
  quickstart.md
  data-model.md
  contracts/
    bootstrap.openapi.json
    development-and-ui.md
  checklists/requirements.md  # existing author review, not the human quality gate
```

### Source Code (repository root)

These are **future implementation paths**, not scaffolding created by plan:

```text
Kofte.slnx, global.json, Directory.Build.props, Directory.Packages.props
package.json, package-lock.json, .nvmrc, .editorconfig
src/Kofte.Host/
  Program.cs
  Features/Bootstrap/         # health endpoints/handlers and safe responses
    Tests/                   # linked into Kofte.Host.Tests; excluded from host build
  Bootstrap/                 # configuration, provider probes, store CLI, diagnostics
tests/unit/Kofte.Host.Tests/
tests/integration/Kofte.Host.IntegrationTests/
apps/web/
  app/page.tsx, app/layout.tsx, app/globals.css
  app/backend/health/ready/route.ts
  features/bootstrap/        # status component, hook/guard and adjacent tests
  components/ui/button.tsx    # minimal shadcn/Radix source, retained notices
  next.config.ts
packages/bootstrap-contract/
  openapi.json                # versioned baseline matching planned HTTP contract
  src/generated.ts            # generated, drift-checked TypeScript types
tests/contract/               # runtime/OpenAPI/anonymous surface tests
tests/e2e/                    # Playwright, axe, timing and fault/recovery journeys
scripts/dev.py                # owned native process lifecycle and readiness
scripts/verify.py             # named required suites, isolation and evidence
deploy/docker/compose.test.yml
.github/workflows/product-checks.yml
docs/development/local-development.md
docs/development/dependencies.md
```

**Structure Decision**: Bootstrap is a host-owned slice, not a fake feature module.
Host-local configuration/storage/diagnostic helpers do not justify separate
platform or infrastructure assemblies. The contract workspace has two concrete
consumers (web and HTTP contract tests) and exports no domain/module interfaces.
Integration tests stay in the existing test category; slice-unit source remains
adjacent to its behavior with explicit project inclusion/exclusion. Existing
`packages/ui`, `packages/module-sdk`, `modules` and Helm placeholders remain
reserved. Do not generate tasks or implement any path during planning.

## Design, Migration and Recovery

### Ownership and requirement-to-design map

| Requirements | Owner and design | Acceptance evidence |
| --- | --- | --- |
| FR-001-003, US1 | Host/web and `dev.py`; explicit restore/init, one start command, preflight, bounded readiness and owned-child cleanup | Startup suite, missing tools/ports, failed child and warm restart tests; SC-001/002 |
| FR-004/011, US3.1-2/US4.2-3 | Web bootstrap status state machine; fixed readiness adapter; honest empty state and accessible retry | Vitest plus Playwright/axe/manual keyboard at 320/768/1440; SC-002/004/005 |
| FR-005/006/008, US4.1 | Host health slice and versioned HTTP contract; no other product endpoints or permissive identity | HTTP success/503/OpenAPI parity and absent/denied route/method tests |
| FR-007/015/016, US1.3/US2.2/US3.3 | Bootstrap adapters/store CLI; platform-only namespace, explicit init/reset, canonical root, isolated fixtures | Both-provider round trip, reconnect/restart, backup/restore, refusal and cleanup tests; SC-006 |
| FR-009/010, US3.3-4 | Validated environment/loopback config, bounded timeouts, allowlisted diagnostics and opt-in OTel | Configuration, secret-canary and outage tests across responses/UI/captured logs; SC-004/006 |
| FR-012-014, US2 | Locked graph and named verification suites, same commands in required CI jobs | Clean/locked restore, lint/format/types/build/tests, controlled failure propagation; SC-003 |
| FR-017 | README, contributor/local/dependency docs and stage ledger | Fresh-checkout guide exercise, evidence review, no exaggerated feature claims |

### Process, exposure and contracts

The supervisor validates tools and ports before launching any child, builds from
restored dependencies, starts the host then web, and declares success only after
both answer the expected contracts. The complete attempt is bounded to 300 seconds.
An early exit/unready deadline returns nonzero and terminates only owned
children/process groups. Ctrl-C stops both without deleting files or volumes.
It never kills a process merely because it occupies the desired port.

Host `127.0.0.1:5080` and web `127.0.0.1:3000` are defaults. Broader **bind**
exposure requires explicit opt-in and an unsafe-local-use warning, not a claim
of production support. Backend adapter destination remains loopback regardless.
Only the three documented host GET routes are anonymous. OpenAPI is enabled in
Development only, with explicit anonymous security metadata and no interactive
admin/testing surface. Unknown routes/methods are absent/denied with sanitized
responses. Domain authorization defaults remain closed for future features.
See [HTTP contract](contracts/bootstrap.openapi.json) and
[development/UI contract](contracts/development-and-ui.md).

Requests use generated/validated trace correlation; responses disable caching.
Liveness is independent of storage. Readiness opens a fresh selected-provider
connection with a 3-second database budget and 5-second endpoint budget; it cannot
reuse a last-success flag. SQLite checks writable access/transaction availability;
PostgreSQL checks connectivity and platform-schema privileges. Actual read/write
persistence is additionally demonstrated by integration fixtures, not assumed
from a `SELECT 1`. A storage failure is 503, never healthy with details hidden.

### Platform store and failure/recovery

First setup explicitly invokes the host's **local CLI mode** `--store init`;
there is no HTTP initialization endpoint. Ordinary host startup opens an existing
store and refuses a missing, corrupt, incompatible or inaccessible target rather
than implicitly recreating it. Initialization never overwrites existing data.
CLI backup/restore/reset require the host stopped and an exclusive owned lock.
Restore refuses a nonempty target unless a separate explicit destructive
confirmation authorizes replacement after backup. Reset also requires a
separate confirmation; the supervisor never invokes it.

SQLite uses the fixed `platform/platform.db` suffix under a canonical absolute,
restricted data root. Reject traversal, symlink/reparse escapes and non-regular
files before access; verify parent directories during creation, protect the root
from other writers and test escaped/missing/unwritable/locked/corrupt targets.
Do not accept a caller-selected database filename or attach another module file.
Local trusted code is not sandboxed; path checks do not promise hostile-process
filesystem isolation. Default rollback journaling avoids an unneeded WAL backup
contract. Tests cover consistent stop-before-copy/restore using the provider's
backup support and test-owned records.

PostgreSQL tests start a uniquely named Compose project or use an explicitly
designated loopback disposable test service. The harness first creates a **new**
randomly named database, provisions `kofte_system`, then uses a role limited to
that owned database/schema. It never drops a supplied existing database.
Cleanup is restricted to identifiers the current run created and recorded.
An explicit disposable-test opt-in is necessary but insufficient without this
ownership proof. Never use `public`, the developer's SQLite root, or a fallback
provider. Backup/restore tests use `pg_dump`/`pg_restore` against newly created
owned databases and verify the same fixture after restoration.

There are no shipped application tables, migration version or dependency-ordered
module migrations in 001. Reject corrupt or unsupported provider state rather
than claiming a compatible application-schema upgrade. Both-provider tests cover
unchanged-store restart and safe rejection of corrupt/incompatible state; they **do not** prove
future module clean-install/upgrade/allocator behavior. Back up before dependency
upgrades, validate supported file/server compatibility, and prefer restoring a
verified backup with a compatible binary or explicit forward recovery. No automatic
downgrade, migration, database replacement or promised cross-module atomicity.

### Telemetry and safety

Allowlisted structured fields: timestamp, level, event code, safe component,
provider label, contract version and trace ID. Emit bounded failure codes such as
`configuration_invalid`, `storage_unavailable` and `startup_timeout`, with safe
corrective guidance in the contributor guide. Do not log raw exception messages,
paths, headers, bodies, tokens, SQL parameters or connection strings. Framework,
Npgsql, Next development output and supervisor child-output handling must also
be configured/checked so their default logs cannot bypass this rule.

OpenTelemetry traces/metrics are local-capable with OTLP export opt-in, never
automatic remote discovery. Export endpoints are trusted operator configuration,
not request-controlled; credentials remain secret configuration. No domain
mutation occurs, so immutable audit persistence is N/A until 009. No external
integration, background job, event delivery, resource concurrency or subscription
is implemented; their policy/retry/durability controls remain later-spec work.

## Validation and Operational Handoff

All commands in [quickstart.md](quickstart.md) are an **implementation target**.
Only repository checks apply to this documentation-only stage. The future
`verify.py` wrapper must expose each child command/result and preserve nonzero
failure, with no unavailable suite counted as success.

| Suite / environment | Required coverage | Evidence mapping |
| --- | --- | --- |
| `quality`; pinned tools, no server | Repository checks, formatting, ESLint, TypeScript, .NET analyzers, contract/type generation drift, all workspace/host builds, license/provenance inventory | US2.1/2.4; FR-001/012-014/017; SC-003 |
| `unit`; no network services | Configuration validation, state/response guards, timeout/cancellation/stale-result handling, safe error mapping, supervisor/cleanup ownership | US1.4/US3.2-4; FR-003-005/008-010/016 |
| `integration --provider sqlite`; real temp files | Read/write, fresh-connection readiness, stop/restart, lock/unwritable/corrupt/missing store, traversal/symlinks, initialization/reset/restore guards | US1.2-3/US3.3; FR-005/007/015/016; SC-006 |
| `integration --provider postgres --disposable-postgres`; real PG17 | Read/write and reconnect/restart; wrong credentials/outage and recovery, schema ownership/privileges, no provider fallback, target/cleanup refusal | US2.2/US3.1/3.3; FR-005/007/015/016; SC-006 |
| `recovery --provider both --disposable-postgres` | Non-destructive retry, confirmed reset limited to owned roots, both-provider fixture backup/restore, incompatible-state refusal | US1.3/US3.3; FR-015/016; SC-006 |
| `contract`; live host in both profiles | All allowlisted response/status/version/cache/security shapes match generated OpenAPI and canonical baseline; unknown routes/methods, malformed inputs, no admin/test/MCP/mutation surface | US4.1; FR-005/006/008/010 |
| `browser`; built web/host, installed Chromium | Healthy/loading/empty/offline/503/hang/retry/recovery, 30s freshness and 10s deadline, stale response ordering; keyboard/focus/status announcements, 320/768/1440 widths, axe | US1.1/US3.1-2/US4.2-3; FR-004/011; SC-004/005 |
| `privacy`; real providers and browser | Seed credential-like canaries into connection/request/error cases; scan captured response, page, console, framework/host/supervisor logs; correlated sanitized guidance | US3.4; FR-009/010; SC-006 |
| `startup --warm-starts 10`; reference environment | Follow fresh-checkout guide, missing tools/ports and child failures, one command to real readiness under 300s; 10 warm starts, page under 2s, preserve fixture every restart | US1.1-4; FR-001-003/009/017; SC-001/002 |
| `failure-propagation`; isolated temporary checkout | Deliberately fail a test, formatter and build; disable required PG/browser dependency; prove wrapper and CI command result fails with stage name | US2.3; FR-012/013; SC-003 |

CI runs the repository job plus named product jobs for quality, unit, both real
providers, recovery, contracts, browser, privacy, startup and failure propagation.
Reuse suite commands rather than silently diverging local/CI coverage. Keep fault
injection in test processes, never deployed HTTP controls. Isolate each job's
ports/files/Compose project/database and upload safe named reports on failure.
Missing tools/services/browsers fail the required job. Pin action SHAs/images;
do not grant write credentials to pull-request tests.

Required manual handoff: a contributor follows only the local guide; a reviewer
checks keyboard/focus/announcements, applicable WCAG 2.2 AA criteria and readability
(automated axe is not sufficient), and reviews timing/privacy/provider evidence.
The guide identifies OS, CPU/RAM, revision, pins, exact commands/results and known
limitations. Record final coverage in `implementation.md`, not as premature pass
markers here. The human-owned quality checklist, tasks and analysis all precede
implementation on later invocations; convergence/review/acceptance remain separate.

## Complexity Tracking and Decisions

| Proposed complexity or unresolved choice | Need and simpler alternative | ADR / human decision |
| --- | --- | --- |
| Small owned-process supervisor | Two native services need bounded readiness/failure propagation; shell `&` is insufficient; Aspire/full containers unnecessary | Proposed ADR 0002; R2 |
| Shared bootstrap contract package | Web static types and live HTTP contract tests consume the same baseline; no UI/module SDK added | Proposed ADR 0002; R4 |
| Thin provider probes before EF modules | No application schema exists; empty contexts/migrations add no value; later module EF obligations unchanged | Proposed ADR 0002; R3 |

No complexity exception or unresolved constitutional/design dispute is requested.
Secondary dependency pin/license adoption is an explicit implementation gate, not
an unanswered product question. ADR 0002 remains proposed pending human review.
