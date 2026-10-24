# Implementation Plan: 004 - Module Composition and Lifecycle

**Branch**: `feature/004-module-lifecycle` | **Date**: 2026-10-22 | **Spec**: [spec.md](spec.md)
**Input**: `specs/004-module-lifecycle/spec.md`
**Constitution**: Adopted v2.0.1
**Status**: Planning complete; proposed design, not implemented or human-accepted.

## Summary

Deliver the five approved journeys through an explicitly trusted, compiled
deployment catalog, pure bounded composition resolution, native DI and an
ordered lifecycle coordinator. Operators persist desired selection/settings in
a private versioned file. An explicit stopped `--modules prepare` command
preflights the entire composition before provisioning/migrating through accepted
003 authority. Ordinary serving revalidates and opens already prepared stores;
it never inherits provisioning authority or silently performs migrations.
Enable/disable takes effect on restart, with immutable stores retained.

Public author contracts and test-owned separately compiled modules prove
composition without production references to test code. Detailed state goes to
the trusted operator channel; existing anonymous health retains its schema and
only projects aggregate availability. No lifecycle administration API/UI/MCP,
package downloader, hot-unload, job/event runtime or reference-module shortcut.
Consequential choices are **Proposed [ADR 0006](../../docs/adr/0006-module-composition-lifecycle.md)**,
requiring explicit human approval before implementation, not a scope amendment.

## Technical Context

**Language/Version**: Existing .NET SDK 10.0.402, C# / `net10.0`, runtime
10.0.13; retain Node 24.22.0/npm 11.19.1 and the existing web toolchain.
**Primary Dependencies**: Reuse .NET hosting, System.Text.Json, native DI,
Activity/Meter and existing EF Core 10.0.13/Npgsql 10.0.3 adapters.
The only planned new project dependency edge is DI abstractions 10.0.13
from `Kofte.Platform` for author registration signatures; it is already pinned
and reviewed in the repository, but its project lock/transitive evidence must
be reviewed before adoption. No new dependency version or plugin framework.
**Storage**: Existing platform registry format 1, owner-only PostgreSQL
schemas/context/history or separate SQLite files. Private desired-config file;
attempt/health state is process-local, not a new registry/settings table.
**Testing**: Extend existing unit/contract/real-provider/recovery/privacy suites;
separately compiled test module and executable consumer; retain Playwright and
the fixture-owning all-suite. Exact commands/mapping are in [quickstart](quickstart.md).
**Target Platform**: Existing macOS 15+ arm64 and Ubuntu 24.04 x64 profiles.
SQLite needs no Docker; PostgreSQL tests use the existing owned pinned-image
fixture. Production/Kubernetes packaging remains 022; no native Windows claim.
**Project Type**: Shared minimal author contracts, infrastructure ownership
adapters and host-local composition slices within the modular monolith.
**Performance Goals**: SC-002's 100 evaluations × ten discovery permutations;
three complete process disable/re-enable cycles per provider; every failure
within [the documented envelopes](contracts/operator-v1.md#budgets-and-supervision).
**Constraints**: Pure resolution bounded to 32 deployed/16 enabled modules,
1,024 dependency edges and 1,024 contributions; explicit overflow rejection.
Nonempty composition startup is at most 60 seconds including cleanup, within
the existing 285-second whole-stack work / 300-second total envelope, not an
extension to it. Preparation is a separate at-most-120-second maintenance
attempt. Accepted 003 acquire/resolve/register/migrate maxima remain 2/3/30/120
seconds, clipped by the enclosing remaining budget.
**Scale/Scope**: Installation-operator authority and trusted code, no team
business data or public management interface. Accepted 001-003 dependencies;
MVP 004 only, not later capability execution or spec 024 release evidence.

## Constitution Check

**Pre-research assessment**: The spec and accepted implementation boundaries
permit the selected stage. No locked source conflict was found. The first
column below records that assessment; the second rechecks the concrete design.
Both are design gates, not runtime evidence or ADR approval.

| Principles | Design evidence / decision | Before research | After design |
| --- | --- | --- | --- |
| I, XII: brand, optional modules, scope/dependencies | Empty path unchanged; accepted 002/003; explicit future-runtime exclusions; FR-001/020. | Pass | Pass |
| II, III: modules, versioned contracts, slices/native DI, trust | Compiled catalog, deterministic preflight, owner/collision checks, ordered runtime, reverse cleanup; [composition contract](contracts/composition-v1.md); dedicated test projects, no mediator/mapper. | Pass | Pass |
| IV, V: registry, isolated stores, honest local transactions | 003 allocator/UNIQUE/path/marker/history rules unchanged; exclusive offline preparation and read-only serving verification; no cross-store rollback. [Operator contract](contracts/operator-v1.md). | Pass | Pass |
| VI, VII: authority, permissions, safe MCP and diagnostics | Operator-owned file/CLI only; no anonymous mutation or detailed inventory; capability/permission metadata grants nothing; future MCP/runtime execution unsupported explicitly. OIDC/team enforcement remains 005/006, tools 007/013, durable audit 009. | Pass, scoped | Pass, scoped |
| VIII: durable resources/events/jobs/realtime | Attempts are observations, not jobs or domain resources; no durable side-effect API is added. Runtimes remain 012/016-019, not falsely implemented by metadata. | Pass, N/A runtimes | Pass, N/A runtimes |
| IX: UI/accessibility/trusted frontend | No new page/navigation; retain existing status/accessibility/browser tests. Frontend metadata is inert; executable frontend remains 014. | Pass, scoped | Pass, scoped |
| X, XI: portable operation and evidence | Finite supervised budgets, exact ownership/recovery, safe records, both-provider process/privacy proof and retained full baseline; [guide](quickstart.md). | Pass | Pass |
| Governance: traceability, ADR/licensing/human gates | R1-R12 decisions below, Proposed ADR 0006, unchanged spec/checklists, one branch/draft PR; dependency adoption, quality review and feature acceptance remain human/implementation gates. | Pass | Pass |

No complexity exception waives a MUST. Changes to approved intent require
explicit reconciliation, not silently relaxing bounds or authority.

## Project Structure

### Documentation (this feature)

```text
specs/004-module-lifecycle/
  spec.md
  checklists/requirements.md
  plan.md
  research.md
  data-model.md
  contracts/composition-v1.md
  contracts/operator-v1.md
  quickstart.md
```

`tasks.md`, quality checklist, analysis and implementation/review evidence are
later stages. This stage creates no product source or dependency lock changes.

### Source Code (planned repository paths)

```text
src/Kofte.Platform/Modules/Composition/       # immutable input/result and resolver
src/Kofte.Platform/Modules/Lifecycle/         # versioned author/config/runtime contracts
src/Kofte.Infrastructure/Composition/         # read-only storage snapshot and recipes
src/Kofte.Host/Features/ModuleComposition/    # catalog/config, prepare, coordinator,
                                            # supervisor, observations, health projection
src/Kofte.Host/Program.cs                    # integrate explicit modes/default empty path
src/Kofte.Host/Features/Bootstrap/            # aggregate readiness only; schema unchanged
tests/unit/Kofte.Platform.Tests/Modules/Composition/
tests/unit/Kofte.Host.Tests/Features/ModuleComposition/
tests/integration/Kofte.Storage.IntegrationTests/Composition/
tests/contract/Kofte.Platform.ContractTests/Composition/
tests/contract/Kofte.Composition.TestModule/  # separate library, test source only
tests/contract/Kofte.Composition.Consumer/    # test-owned deployment root/executable
tests/integration/Kofte.TestSupport/         # reuse owned providers/processes/canaries
scripts/verify.py                           # wire required cases into existing suites
```

**Structure Decision**: No speculative production assembly or real feature
module. Small shared public types live in the existing platform assembly;
EF/provider recipes stay in infrastructure. The test executable supplies a
compiled catalog using a public host integration entry point; production
`Kofte.Host` never references the test module, includes its source or loads a
test assembly by scanning a directory. Test contexts/migrations stay in their
test project using ordinary SDK inclusion.

## Design, Migration and Recovery

### Requirement-to-design mapping

| Decision / requirement coverage | Owner and design |
| --- | --- |
| R1: FR-001/002/019; US1; SC-001 | Compiled catalog holds bounded inert manifests/declarations and uninvoked delegates; explicit private selection. No reflection/directory/package discovery, constructors or callbacks for disabled modules. |
| R2: FR-002-005; US2; SC-002 | All deployed manifests validated through accepted 002; enabled graph resolved using its range membership. Kahn ordering with ordinal smallest ready ID; stable SCC cycle diagnostics and sorted/truncated rejection. |
| R3: FR-005/007/012/018 | Strict private config snapshot and explicit author-owned pure validator; no environment/service/file fallback. Config changes require a new restart, never live mutation. |
| R4: FR-005/006/019 | Declarative contribution ownership, supported versions, global route and owner-local identity keys. Permissions copied from accepted descriptors; future categories reserve metadata only. No executable module HTTP route before auth/010. |
| R5: FR-008/011/015/019 | All-enabled preflight barrier; native DI registration audit/build once; phase barriers and dependency order; owner context and no-storage restrictions; publication only after the entire set starts healthy. |
| R6: FR-005/009/010/013 | Read-only inventory/mapping preflight before callbacks or writes, under accepted authority. Explicit preparation uses one exclusive scope and supervised child; serving validates exact prepared history/targets under host lease. |
| R7: FR-009-013/015; US3/4; SC-003 | Existing assigned EF recipes only; skip migrations whose verified history equals target. Disable does not register/update any disabled owner. Checkpoints/data/mappings survive; recovery remains explicit 003. |
| R8: FR-014/016; US5 | Current-attempt periodic health, freshness/propagated dependency impact; health payloads are safe codes, never author-supplied text. Anonymous projection stays aggregate. |
| R9: FR-015/016; SC-004 | Owned process supervisor, monotonic deadlines/reserves, registered compensation cleanup and reverse stop/dispose. Root exit is not proof of descendant termination; uncertain cleanup blocks retry. |
| R10: FR-017/018; SC-006 | Fixed structured boundary/transition records, generated attempts, bounded dimensions, allowlisted IPC projection; raw child output discarded. No durable audit/settings service. |
| R11: FR-019/020; SC-001/002/005/006 | Independent public author/host consumers, frozen expected graph corpus and executable examples, not production test hooks or a full reference module. |
| R12: FR-020; all outcomes/scenarios | Real-provider process/cycle/crash/privacy assertions plus unchanged foundation quality/unit/contract/browser/startup/recovery gates. Unavailable infrastructure fails explicitly. |

### Execution and storage boundaries

1. **Discover/snapshot** inert compiled deployments and the private file once.
   Validate every manifest, including disabled definitions; reject any missing
   enabled ID or duplicate deployed ID. Only enabled modules get settings checks,
   dependency/contribution resolution or later runtime callbacks.
2. **Preflight barrier** completes pure validation and read-only complete
   installation inventory/owner checks before registration/initialization or
   allocation. A genuinely unregistered valid owner is admissible for prepare,
   but missing/corrupt data for an existing owner is not "new". Serving instead
   returns `prepare_required` for an unregistered owner or pending upgrade.
   A new infrastructure read-only inspection adapter is needed: do not turn
   arbitrary `ResolveAsync` failures into absence.
3. **Prepare**, explicitly while stopped: admitted child acquires one exclusive
   003 maintenance scope, revalidates the snapshot/authority and completes
   preflight. Register scoped/transient module services, build once, create
   ordinary per-owner DI scopes and initialize without store/business effects,
   then provision and migrate each owner topologically.
   Reuse `RegisterAsync`, `BindAsync`, `ModuleStorageBinding.MigrateAsync` and
   assignment-aware provider contexts. All pending recipes/models must pass
   structural boundary validation before the first module write; actual owner/
   physical checks repeat immediately at use. No activation/publication occurs.
4. **Serve**, separately: retain the normal host lease, no elevated connection.
   Repeat preflight/DI/initialization in a new process/attempt, rebind exact
   assignments and verify targets/current histories read-only. `migrated` here
   means verified already-current, not running EF migration. Start topologically,
   obtain fresh healthy observations, then publish the immutable active catalog
   and ready state atomically in memory. No request handler invokes module code
   while startup is incomplete.
5. **Stop/retry**: withdraw aggregate readiness/publication first. Stop/dispose
   only resources entered or registered by this attempt in reverse order,
   disposing owner DI scopes after their Stop/cleanup and the host provider last.
   Prepare never deletes durable state during cleanup. Corrected retry is a
   fresh process/correlation and revalidates ownership; interrupted intent,
   abandoned locks or uncertain workers require explicit reconciliation first.

Preparation and serving may each register/initialize the same module in
different process lifetimes. This is explicit contract behavior: initialization
is repeatable ephemeral setup, not an exactly-once migration/business hook.
Owner-store access begins only after migration verification in activation.
No new relational format migration, automatic registry v0 upgrade, Down
migration, data deletion or disabled-owner reallocation is introduced.
`--registry upgrade` remains explicit before the first storage-backed prepare.

No-storage modules have no binding/recipe/migration invocation. Existing
no-storage declarations cannot become relational under the same ID. PostgreSQL
retains offline CREATE authority and runtime's denial of database CREATE;
SQLite retains canonical private files/sidecars and actual ownership markers.
Provider-specific models/histories remain entirely inside the owning module.
Accepted stopped complete-set backup/restore and legacy reset refusals apply
unchanged; failed preparations preserve earlier commits and all recovery
evidence. Forward recovery or an explicitly chosen complete prior recovery
point is supported, never automatic cross-module rollback.

## Validation and Operational Handoff

The [guide](quickstart.md) maps US1.1-5.4, all FR-001-020 and SC-001-006 to
existing named suites and required new public/process assertions. Coverage
must check original assignments/records/history, not merely positive counts.
Failures include one-over bounds, deterministic multi-errors, no preflight
effects, lost leases, killed callbacks, incomplete cleanup and seeded output
canaries. Every executable guide example must be required by the contract suite.

Planning validation is repository/Python/whitespace plus semantic source review;
no product check establishes nonexistent 004 behavior. Later implementation
must record exact selected versions/environment, both real providers and full
baseline/CI evidence. New visual/permission/MCP/job/deployment journeys are
inapplicable here, not waived for their later numbered features.

The next stage is **checklist**, generated unchecked and reviewed by a human
or only evaluated with that reviewer's explicit request. ADR 0006 approval is
required before implementation; this plan cannot approve itself. Continue the
same draft PR #7 with no intermediate merge or feature acceptance.

## Complexity Tracking and Decisions

| Proposed complexity or unresolved choice | Need and simpler alternative | ADR / human decision |
| --- | --- | --- |
| Compiled catalog rather than directory plugins | Satisfies trusted discovery/independent authors without package loader, version probing or download policy; requires operator rebuild/redeployment. | ADR 0006 Proposed; review before implementation. |
| Separate prepare and supervised serve lifetimes | Preserves offline privilege separation and actual timeout termination. A token/WaitAsync alone cannot stop trusted callbacks; live privileged serving violates 003. | ADR 0006 Proposed. |
| Read-only storage/model preflight adapter | Needed to distinguish new owners from broken persisted ownership and validate the entire set before mutations. Reusing `RegisterAsync` as validation would write too early. | Extends infrastructure only; accepted storage contract/format unchanged. |
| DI abstractions project reference | Actual shared author registration signature; no new framework/version. Opaque registration handles or a new container add unnecessary abstraction. | Review exact graph/locks/licenses before adoption; no package installed here. |
