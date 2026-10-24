# Composition and Author Contract v1

**Status**: Proposed design for 004, not executable today.
**Authority**: [spec](../spec.md), accepted manifest/storage contracts and
[Proposed ADR 0006](../../../docs/adr/0006-module-composition-lifecycle.md).
This contract does not extend manifest schema 1 or grant installation authority.

## Public surfaces and versioning

Namespace `Kofte.Platform.Modules.Composition` owns immutable deployment inputs,
resolution results, contribution claims and deterministic resolution.
`Kofte.Platform.Modules.Lifecycle` owns author configuration/runtime contracts.
All publish `ContractVersion = 1`; unsupported versions reject explicitly.
The only framework dependency planned for the platform library is the existing
pinned DI abstractions package; no ASP.NET Core/EF/provider dependency.

| Planned public surface | Inputs / result and guarantee |
| --- | --- |
| `ModuleDeployment` | Copied manifest bytes, declarative claims/configuration field rules, pure validator, registrar and runtime factory delegates. Constructing a deployment must not construct the module or run a lifecycle callback. |
| `CompositionResolver.Resolve` | Bounded deployment/selection snapshots, explicit accepted 002 host context and fixed host reservation catalog; immutable `Accepted` composition or `Rejected` ordered nonempty safe diagnostics. Pure: no IO/logging/storage/callback execution. |
| `IModuleConfigurationValidator.Validate` | Owner-only immutable settings and declared field tokens; immutable safe code/field failures. Runs in the supervised preflight phase, not inside the pure resolver. No IO, mutation or arbitrary message/path reflection. |
| `ModuleDeployment.ConfigureServices` | `IServiceCollection` plus host-issued `ModuleRegistrationContext`; native registration only, no service resolution/provider build, IO, migration or lifecycle starts. |
| `ModuleDeployment.CreateRuntime` | Owner's native DI scope provider and host-issued owner context; returns one `IModuleRuntime` for the attempt. Construction is after preflight and registration. |
| `IModuleRuntime.InitializeAsync` | Initialization context and cancellation; repeatable ephemeral setup, no owner-store access, contributions, business side effects or prerequisite service reads. |
| `IModuleRuntime.StartAsync` | Activation context and cancellation; owner-only verified store accessor if relational, safe contribution view, own settings and attempt. Prerequisites have already completed required phases. |
| `IModuleRuntime.CheckHealthAsync` | Cancellation; fixed health enum and host-registered safe reason token only, not exception/message text or another module's state. |
| `IModuleRuntime.StopAsync` | Cancellation; safe after partial Initialize/Start entry; stop only resources owned by this instance. At most once per entered instance. |
| `ModuleAttemptResources.RegisterCleanup` | Runtime-owned cleanup delegate registered when a resource is acquired; bounded LIFO actions within reverse module order. Durable stores are not cleanup resources. |
| `ModuleCompositionHost.RunAsync` | Public host integration entry point receiving args and an explicit compiled catalog; test/deployment executable uses it without internal access. Default `BootstrapProgram.RunAsync` supplies an empty catalog. |

Signatures are design targets; implementation must realize the stated types,
input/return semantics and ownership, not invent overloads to bypass preflight.
All async methods accept `CancellationToken`; callbacks run sequentially through
the coordinator, never a module-owned `IHostedService`. `InitializeAsync` and
registration run once in prepare and once in serve, in different attempts.
No callback is an exactly-once durable work API.
Start/health/Stop perform ephemeral lifecycle work and may verify owner-local
state, but must not make irreversible business/external effects. Durable schema
changes belong to admitted migrations; business mutations belong to explicit
module operations after activation, not implicit startup/retry callbacks.

The host issues owner contexts; no module can request a different owner's
configuration/store/maintenance scope by passing a string. `IModuleStorage`
and infrastructure assigned-context helpers remain accepted 003, not arbitrary
physical connection factories. `IStorageRegistry`/`IStorageMaintenance`,
provisioning credentials and composition mutation are never module DI services.
No-storage contexts carry no assignment or usable database accessor.

DI registrations are restricted to explicitly declared module-owned service
types. The host audits the added descriptor range: reject host-reserved
replacement, mutation/removal of existing descriptors, cross-owner service
claims, `IHostedService` and direct global storage/authority registrations.
The author contract additionally forbids root-provider construction; descriptor
inspection alone cannot detect arbitrary code calling `BuildServiceProvider`.
Build once after all registrars with scope and dependency validation.
Create one ordinary DI scope per enabled owner, not one provider/container.
Module-owned services are scoped/transient, not root-owned singletons; dispose
each owner scope after its reverse-order Stop/cleanup. Runtime factories either
resolve a container-owned runtime or explicitly register a factory-created
runtime for disposal, never both. Public prerequisite operations are supplied
as explicit versioned dependency bindings, not arbitrary cross-owner service
resolution or shared context/connection access.
Arbitrary trusted code can violate supported conventions;
these rules and scoped contexts are not hostile-code isolation.

## Whole-set preflight and ordering

Resolution/validation phases, in order:

1. Admission counts/bytes and strict configuration envelope.
2. Every deployed manifest through accepted 002; catalog/selection identities
   and contract support, duplicate/missing enabled implementation.
3. Enabled dependency membership/version checks and graph cycles.
4. Enabled configuration field/type validation and author pure validators.
5. Enabled contribution/recipe declaration ownership/version/collision checks.
6. Infrastructure read-only complete inventory, persisted mapping/history/
   ownership and all pending recipe/model/operation boundary preflight.

Fatal byte/envelope overflow returns one error; later phases run only if their
prerequisites are valid. No registration/Initialize/provisioning/migration or
publication can begin until **all** preflight phases pass. Configuration
validators are explicitly pure validation callbacks, not lifecycle starts.
After preflight, storage owner/lease/physical checks are still repeated at use;
preflight cannot guarantee a malicious privileged external actor will not race.

Use accepted `VersionRange` membership directly, including prerelease endpoint
eligibility and build-metadata-insensitive precedence. All dependencies are
required; no downloads, silent enablement, version solving or fallback.
Kahn's algorithm always selects the ordinal-smallest ready canonical module ID.
For an invalid cyclic graph, bounded SCC analysis emits one diagnostic per
cyclic component, members ordinal-sorted, at most eight IDs per diagnostic with
explicit omitted count. An invalid manifest self-dependency stays accepted
002's `dependency_self`; do not construct an otherwise impossible descriptor.
No execution order is returned from a rejected graph.

## Admission and work limits

| Input/work | Inclusive limit; one over rejects |
| --- | --- |
| Deployed catalog / enabled set | 32 / 16 modules; inspect at most 33/17 entries before refusal |
| Manifest bytes | Accepted 002 65,536 per module, 2,097,152 total; original grammar/depth/collection limits unchanged |
| Selection file | 1,048,576 UTF-8 bytes, 32 entries, depth 8; each settings object at most 16,384 original UTF-8 bytes |
| Settings fields | 128 declared fields/module, fixed ASCII token ≤64 chars and JSON path ≤8 known segments; unknown keys reported only at known containing field |
| Enabled graph | 1,024 edges maximum; per-module 002's 64 dependencies unchanged |
| Contributions | 128/module, 1,024 total enabled; stable local ID ≤128 ASCII chars; canonical route ≤256 ASCII chars |
| Cleanup registrations | 32/module, 512/attempt; refusal is a stage failure, never silently dropped |
| Diagnostics | At most 64 and 65,536 serialized UTF-8 bytes: emit the largest ordered prefix of at most 63 that fits with a reserved final `composition_diagnostics_truncated` marker when either limit would be exceeded |
| Diagnostic text | Fixed code/guidance ≤64 ASCII chars, fixed message ≤256 chars; at most eight canonical related IDs, no raw invalid fields/values |

Pure resolution uses bounded scans/sets and O(V+E) graph traversal plus bounded
ordinal sorting; no path enumeration, unbounded enumerable materialization,
backtracking regex, assembly scan or external resolution. Two-second pure
resolution deadline is additionally clipped by the enclosing attempt; any
overflow/deadline rejects rather than truncates an accepted composition.
An author validator has a 250-ms cap, in the owned process; timeout fails the
attempt and requires supervised termination, not a falsely completed validator.

## Contribution catalog, not future SDK execution

Claims distinguish `declaration` from `executable`; all carry owner/kind/
contract version/local identity. Only the backend lifecycle/DI contract executes
in 004. Accepted 002 categories may be declared without a claim or runnable
implementation. An executable request for route/resource/MCP/frontend/job/
event/workflow/provider/permission enforcement fails `contribution_unsupported`
before callbacks. Actual HTTP route publication and its authorization remain
later 006/010; a test module proves behavior through its public module service
and owner-local data, not a new anonymous route.

| Claim | Collision/ownership rule |
| --- | --- |
| Backend service | `(owner, service identity)`; module-owned types only, no host-reserved replacement. |
| Permission | Exactly descriptor `(moduleId, localName)` and its accepted description; capability metadata does not gate or grant it. Same local name under another owner is valid. |
| Resource/tool/other category declaration | `(kind, owner, localName)`, version precedence supported at 1.0.0; no invented global permission/tool encoding. Different version does not evade identity collision. |
| Route reservation | Global `(HTTP method, canonical template)` including host-reserved paths. Grammar below fixes route-key normalization; no ASP.NET route matcher is exposed. |

Route claims are inert, uppercase fixed methods `GET|POST|PUT|PATCH|DELETE`.
Path begins `/`, has lowercase ASCII literal segments matching
`[a-z][a-z0-9-]*` or a whole `{name}` segment with the same name grammar.
No root-only claim, empty/trailing segment, wildcard, constraint, optional
parameter, percent encoding, dot segment, query or fragment.
Parameter names normalize to `{}`. For the same method, **overlapping**
templates conflict: equal-length segment lists where each pair is equal
literals or either segment is a parameter. Thus `/items/{id}` conflicts with
`/items/{name}` **and** `/items/current`; exact-key equality alone is insufficient.
Host `/health`, `/openapi` and `/api/kofte` prefixes are reserved for all
methods/depths. Normalize before comparing, never let ASP.NET precedence
silently choose an owner. Rich route semantics require a later version.

Claims must have a matching manifest capability except permissions, whose
accepted list is independent. Metadata-only unsupported execution is not
silently activated. Invalid owner/version/kind/name or duplicate local claims
reject; diagnostics never reflect a raw submitted identity.
Route/backend-service claims map to `backend`, resource to `resources`, tool
to `mcp-tools`; the remaining declaration kinds map to their accepted 002
category. Local identities use lowercase dot/hyphen-separated ASCII identifier
segments; permission identities retain 002's stricter length/grammar.

## Results and safe diagnostics

Accepted and Rejected are disjoint. Accepted contains a complete immutable
order/catalog, no settings in printable results and no runtime/storage authority.
Rejected has no executable subset. Diagnostics sort by phase, canonical owner
(invalid/missing owner uses a null sentinel first), fixed field order/index,
related canonical ID, then ordinal code. Duplicate invalid identities do not
use discovery index as a tie-breaker. Per-manifest 002 diagnostics retain their
own field ordering; manifests themselves are ordered by canonical identity or
bounded input digest internally when invalid, never by display text.

Stable composition codes include `composition_input_limit`,
`composition_configuration_invalid`, `composition_contract_unsupported`,
`composition_manifest_rejected`, `composition_module_duplicate`,
`composition_implementation_missing`, `composition_dependency_missing`,
`composition_dependency_disabled`, `composition_dependency_incompatible`,
`composition_dependency_cycle`, `composition_setting_invalid`,
`composition_contribution_conflict`, `composition_contribution_unsupported`,
`composition_storage_rejected`, `composition_prepare_required`,
`composition_registration_failed`, `composition_initialization_failed`,
`composition_activation_failed`, `composition_health_degraded`,
`composition_health_failed`, `composition_health_unknown`,
`composition_deadline_exceeded`, `composition_cancelled`,
`composition_cleanup_incomplete`, `composition_reconciliation_required` and
`composition_diagnostics_truncated`.

Messages/guidance come from a fixed host catalog. Safe settings guidance uses
only a declared field token and reason enum `required|type|unknown|constraint`;
never an unknown supplied key or value. Accepted 002 diagnostics are nested as
allowlisted codes/known paths, not raw input. Storage failures retain their
recognized 003 code/durability and child correlation linked to the new attempt;
never infer rollback or conflate a fresh attempt with an old successful one.
