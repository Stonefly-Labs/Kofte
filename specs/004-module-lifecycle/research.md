# Research: 004 - Module Composition and Lifecycle

**Date**: 2026-10-22
**Status**: Resolved planning choices, not adoption/runtime proof.
**Decision record**: [Proposed ADR 0006](../../docs/adr/0006-module-composition-lifecycle.md).

## Research scope and sources

The clarification stage left implementation choices, not unanswered product
questions. Research assessed discovery, DI/contracts, deterministic resolution,
storage/preparation, supervision, configuration, health and evidence against
accepted 001-003. No material conflict or unresolved design placeholder remains.
Microsoft Learn/Context7 MCP tools are not connected in this session; a bounded
research agent verified official Microsoft web documentation instead. No
fabricated MCP or runtime result is used.

Local evidence:

- `src/Kofte.Platform/Modules/Manifest/ModuleDescription.cs`, `VersionRange.cs`
  and [accepted validation contract](../002-platform-contracts/contracts/validation.md):
  immutable parsed descriptions, explicit required dependencies and category
  metadata; no optional flags, loader, permission grant or executable SDK.
- `src/Kofte.Platform/Modules/Storage/StorageContracts.cs`,
  `src/Kofte.Infrastructure/Storage/Registry/StorageRegistry.Binding.cs`,
  `ModuleStorageBinding.cs`, `Migrations/ModuleMigrationRunner.cs` and
  [003 contracts](../003-storage-registry/contracts/migrations-and-recovery.md):
  actual assignments, revision/marker checks, migration intents and exclusive
  scopes. `ResolveAsync` is not a general "exists or allocate" preflight API.
- `src/Kofte.Host/Program.cs`, `Features/StorageMaintenance/`,
  `src/Kofte.Infrastructure/Storage/StorageMaintenanceProcess.cs`:
  serving acquires the accepted host lease; elevated credentials are isolated
  to admitted children; parent drains child output and reaps its process.
- `src/Kofte.Host/Features/Bootstrap/HealthEndpoints.cs` and `HealthHandler.cs`:
  current anonymous schema is fixed/minimal; startup/health must not expose
  inventory or overload an existing field with module data.
- [Local guide](../../docs/development/local-development.md) and pinned
  `Directory.Packages.props`: existing supported targets, 300-second supervisor
  total/285-second startup work, reviewed native dependencies and named suites.

Official sources checked:

1. [Microsoft plugin tutorial](https://learn.microsoft.com/en-us/dotnet/core/tutorials/creating-app-with-plugin-support):
   AssemblyLoadContext is dependency loading, not hostile-code isolation;
   untrusted code cannot safely run in a trusted process.
2. [DI guidelines](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines)
   and [service lifetimes](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/service-lifetimes):
   native DI, scope/lifetime ownership, avoid building another provider during
   service registration; singleton state must be thread-safe.
3. [Generic host](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host),
   [IHostedService.StopAsync](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostedservice.stopasync?view=net-10.0)
   and [ShutdownTimeout](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.hostoptions.shutdowntimeout?view=net-10.0):
   host-controlled lifecycle ordering and a cancellation deadline, not forced
   termination of a callback. Independent hosted services bypass a module
   coordinator's explicit phase/dependency barriers even when host startup is
   sequential by default; concurrency is not assumed.
4. [Task.WaitAsync](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.waitasync?view=net-10.0)
   and [cooperative cancellation](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads):
   bounded waiting does not prove the source operation stopped; passing a token
   is necessary for cooperation but insufficient for arbitrary code.
5. [Process.Kill](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.kill?view=net-10.0):
   kill is asynchronous; wait/check root exit. Root `WaitForExit`/`HasExited`
   does **not** prove all descendants exited, even with entire-tree kill.

## R1 - Trusted discovery and independent packaging

**Decision**: Explicit compiled catalog built by the deployment composition
root, containing copied manifest bytes/declarations and delegates. A separate
test-owned module library is project-referenced only by its test deployment
executable. The ordinary host defaults to an empty catalog.
**Rationale**: Meets FR-001/002/019 with known code/provenance and no constructor
or runtime factory call for disabled modules. A public host integration entry
point supports independent authors without internal access.
**Alternatives considered**: Directory scanning/reflection/custom
AssemblyLoadContext adds package/dependency/trust policy not needed for 004.
Network package discovery and hot loading preempt later features. Compiled
deployment requires rebuild/redeploy and makes no hot-install promise.

## R2 - Deterministic, bounded whole-set resolution

**Decision**: Reuse 002 validation/range objects unchanged. Kahn topological
ordering chooses the ordinal-smallest canonical ID from each ready frontier.
Use bounded strongly connected components for cycles, with ordinal sorted
members and one canonical cycle diagnostic per component. Diagnostics sort by
phase, owner, field/index, related ID and code, then truncate explicitly.
**Rationale**: Independent discovery order cannot affect decisions; cycle
explanations need not enumerate exponentially many paths. Admission bounds
cover graph size and processing before a caller-controlled enumeration can grow.
**Alternatives considered**: DFS discovery-order output is unstable; exhaustive
cycle enumeration, automatic version selection and dependency installation are
unnecessary or out of scope.

## R3 - Persistent intent, ephemeral observations

**Decision**: Private strict JSON file with explicit contract version,
owner-scoped entries and settings. No file means empty desired selection;
an explicitly configured missing/unreadable file fails. Read/copy once per
attempt; no hot reload. Attempts/health are in-memory and never reused after
restart. No new platform tables.
**Rationale**: Meets restart persistence and separates intent from observed
success without implementing settings/audit products.
**Alternatives considered**: Environment-prefix scraping risks scope leaks and
ambiguous defaults; storing secrets/settings/health in the storage registry
couples unrelated ownership and invents format migrations.

## R4 - Honest contribution support

**Decision**: Versioned declarative contribution catalog, not live route/MCP/
resource registries. Owner-local keys for permission/resource/tool declarations,
normalized global collision keys for reserved route claims. Accepted manifest
permissions remain `(moduleId, localName)` and do not require a permissions
capability. Only backend lifecycle/DI executes in 004; explicit executable
requests for other categories reject before callbacks.
**Rationale**: Collision validation must exist without anonymously exposing
business endpoints or pretending later SDKs work. Metadata-only capabilities
remain valid under 002.
**Alternatives considered**: Treating every declaration as executable breaks
002 semantics; ignoring claims cannot catch collisions; publishing test HTTP
routes introduces an auth bypass and conflicts with the bootstrap allowlist.

## R5 - Native DI and phase barriers

**Decision**: Author contracts use DI abstractions; register after the entire
preflight barrier, audit descriptor ownership, build the final provider once
with scope/build validation, then create ordinary per-owner scopes and initialize
all enabled runtimes topologically. Module services are scoped/transient;
reverse owner-scope disposal preserves dependency-safe ownership without
multiple providers or root-owned module singletons.
No module-owned `IHostedService`, root replacement or self-published endpoint.
Initialize is repeatable ephemeral setup; activation receives owner-bound store
access only after verified migrations. Lifecycle callbacks cannot perform
irreversible business/external effects; actual module operations are explicit
after activation. Track partial-entry cleanup explicitly.
**Rationale**: Prevents the generic host from starting module work outside the
coordinator. Direct DI keeps slices conventional without a plugin framework.
**Alternatives considered**: A second `BuildServiceProvider` during registration,
one container per module or generic mediator adds lifetime/order hazards.
Native DI ownership rules are supported-use checks, not a security sandbox.

## R6 - Preflight and offline preparation

**Decision**: Add a host/infrastructure-only read-only inventory/recipe
inspection adapter. Explicit `--modules prepare` uses one admitted child and
one exclusive 003 scope. Pure config/contribution and all pending model/
operation boundary checks precede the first write. Repeat actual owner checks
at use. Ordinary serving never provisions or migrates.
**Rationale**: `RegisterAsync` mutates; missing registry records must be
distinguished from broken persisted ownership. Existing 003 migrations use
durable intent and cannot serve as a mutation-free whole-composition validator.
**Alternatives considered**: Catching any resolve failure as "new", per-module
validate-then-write or privileged migrations during serving violates FR-005
or accepted 003. Preparation may legitimately time out rather than weaken bounds.

## R7 - Idempotent preservation and recovery

**Decision**: Reuse assigned provider contexts/migration recipes, compare exact
history/target before invoking migrations, preserve disabled owners untouched.
No-storage means no binding or migrations. Incomplete intent/unknown physical
effects require explicit accepted recovery; compatible retries use original
allocation. Restore only an explicitly chosen stopped complete set.
**Rationale**: Earlier module commits are real, no distributed rollback.
Avoid even metadata revision writes for an already-current migration at serve.
**Alternatives considered**: Reset/drop/reallocate, deleting EF locks on normal
restart, rerunning all migrations or inferred global compensation is unsafe.

## R8 - Health and aggregate availability

**Decision**: Sequential topological health sweeps every ten seconds, with one
second per callback, twenty-second sweep maximum and thirty-second freshness.
Desired state, lifecycle, own health and dependency-effective health stay
separate. First healthy sweep gates publication. Any nonhealthy/stale enabled
module makes aggregate readiness unavailable; liveness is still live.
**Rationale**: Conservative availability prevents false success, while detailed
observations preserve independent healthy modules and degraded versus failed.
**Alternatives considered**: Treating silence as healthy, persisting success
across restart or exposing anonymous detailed inventory leaks/misleads.

## R9 - Honest bounded supervision

**Decision**: One owned serving child for a configured composition and one
owned preparation child per explicit command. Reuse admission/process ownership
mechanisms; extend host-level supervision for long-lived serving. Stop waits
and cleanup use a monotonic shared reserve. Callback timeout terminates the
owned process rather than abandoning a live task. No module-owned child
processes are supported by this lifecycle contract; supported fixture-owned
descendants require separate exit evidence, not root exit alone.
**Rationale**: Cancellation is cooperative; synchronous SQLite and trusted
callbacks can ignore it. Normal empty bootstrap remains direct.
**Alternatives considered**: `WaitAsync`, a background thread or shutdown token
alone cannot justify "stopped". Name-based process killing or reacquiring a
lost lease to continue is prohibited. Unverifiable cleanup is a reported block.

## R10 - Safe observations and correlation

**Decision**: Fixed bounded codes/messages/field tokens, generated attempt
trace IDs, one terminal record per boundary plus transition records. Parent
validates bounded child event frames and projects only allowlisted identities;
discard all raw stdout/stderr. No custom module message or exception string
can enter a platform record/label.
**Rationale**: Safe failures must remain observable with useful correction/
reconciliation guidance. Raw output forwarding defeats otherwise safe results.
**Alternatives considered**: Catch-and-empty success, exception logging,
settings-derived labels and a new durable audit DB violate scope/privacy.

## R11 - Public evidence, not internal smoke

**Decision**: Separate module assembly and test deployment executable consume
only public contracts. Require independently frozen graph expectations,
meaningful owner-local behavior, three real restart cycles and corrected retry.
**Rationale**: Internal mocks and positive counts do not prove independent
author use, retained data, actual migrations or no preflight effects.
**Alternatives considered**: Linking test source into production, granting
internals access to the author example, or calling a fixture the full reference
module fails FR-019/020 and test ownership.

## R12 - Retain the existing release/evidence baseline

**Decision**: Integrate focused new cases into existing fixture-owning suites
and require retained 001-003 all-suite/CI/browser/failure gates. No new dependency
version is selected; the DI project edge still needs real adoption/lock review.
**Rationale**: Planning supplies acceptance targets, not measurements, license
approval, reviewer-owned checks or numbered-feature acceptance.
**Alternatives considered**: New parallel test tooling, mock providers or
assuming historical passes apply to a changed runtime gives weaker evidence.
