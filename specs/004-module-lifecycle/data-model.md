# Data Model: 004 - Module Composition and Lifecycle

**Status**: Proposed in-memory/public/configuration model, not implemented.
See [composition](contracts/composition-v1.md) and [operator](contracts/operator-v1.md)
contracts for precise bounds, ordering, authority and failure behavior.

## Entities and ownership

| Entity / owner | Fields and invariants | Relationships / lifetime |
| --- | --- | --- |
| `ModuleDeployment` / trusted deployment root | Contract 1; copied bounded manifest bytes; immutable contribution claims/configuration field definitions; pure settings validator; native DI registrar; runtime factory. Canonical identity exists only after accepted 002 validation. No physical locator or install URL. | One deployed implementation per canonical ID. Disabled definitions are validated metadata, never instantiated. Compiled catalog rebuilt on deployment. |
| `ModuleSelectionDocument` / installation operator | Exact `schemaVersion: "1"` and closed `modules` array. Entry: canonical `id`, explicit boolean `enabled`, closed module-owned `settings` object. No duplicate IDs/properties, unknown top-level fields, comments, BOM or implicit coercion. | One private file snapshot per attempt; absent catalog entries default disabled. Enabled missing deployment rejects. Survives restart outside process memory. |
| `ModuleConfiguration` / one module | Copied, bounded JSON values accessed only within that owner's settings; typed access/field tokens, no ambient environment, other owners or secret service. Author rules declare required/types/allowed fields and a pure cross-field validator. | Passed only to that enabled module's validation/registration/runtime. Unknown/missing/invalid fields reject; no settings serialization in observations. |
| `ResolvedComposition` / platform | Contract 1, copied canonical deployed/enabled/disabled identities, dependency edges and stable topological order; immutable contribution reservation catalog; snapshot fingerprint internal only. Or disjoint rejection diagnostics, never a partial accepted set. | Required dependency edges use accepted `VersionRange`; one order shared by prepare/serve. No automatic enablement/version choice. |
| `ContributionClaim` / immutable owner | Kind, version, stable local identity; declaration/execution mode; routes additionally method and canonical template. Owner must match descriptor. Permissions originate in its accepted permission list. | Unique collision key per kind; host-reserved catalog participates. Metadata does not instantiate later runtimes or grant permissions. |
| `ModuleStorageRecipe` / owning module, infrastructure contract | Canonical owner; provider; typed assigned context factory; known migration assembly/target; no arbitrary SQL/connection/schema/file. Exactly one supported recipe per supported provider for a relational module; none for no-storage. | Host catalog associates recipes explicitly with deployments. Infrastructure preflights models/operations and calls existing 003 bindings under authority. No EF type in neutral composition results. |
| `CompositionAttempt` / platform | Contract 1; fresh 32-lowercase-hex trace ID; mode; monotonic deadline; phase/outcome; entered/completed module-stage sets; per-owner DI scopes/cleanup registrations; publication latch; safe failure evidence. | One prepare/serve attempt per owned process. Stop shares that attempt; corrected restart creates another. Never resumed as successful after process death. |
| `ModuleObservation` / platform | Canonical owner; desired enabled; stage/outcome; own health, effective health; current attempt; monotonic observation instant/age; bounded affected-prerequisite IDs; fixed code/guidance. No raw author text. | Only current runtime instance may update own health; platform derives dependency impact. No observation is authoritative configuration. |
| `CompositionSnapshot` / platform | Current attempt, immutable module observations for trusted operators, overall state, bounded counts, publication flag. | Atomically replaced; readers cannot mutate state. Anonymous projection excludes identities/counts/stages/settings. |
| `Assigned module store` / accepted 003 | Existing immutable installation/allocation/owner/key/allocator identity, revision, migration intent/checkpoint and physical markers/history. | Owned exclusively by immutable module ID. Not a new 004 table. Disable does not delete/update assignment or history; accepted migration may advance revision/checkpoint. |

All public collections copy their backing storage. Result/observation types
cannot be used to mint runtime authority, a storage binding or an accepted
manifest. No raw configuration/fingerprint is published or used as a metric
label. A fingerprint is not a credential and must not include/export secret
material; parent/child compare a process-local opaque snapshot token instead
of publishing a hash of settings.

## Lifecycle state and barriers

Desired state is `enabled` or `disabled`; default for an unselected deployed ID
is disabled. Disabled is not failed, unhealthy or uninstalled.

| Stage / transition | Successful condition | Failure and preservation rule |
| --- | --- | --- |
| discovered → validated | Whole deployed set, enabled graph/settings/claims and read-only storage preflight valid. | Any invalid input yields blocked composition, zero mutating callbacks/store changes. Invalid IDs are never echoed. |
| validated → registered | All enabled registrars completed topologically, descriptor audit passed, one validated provider built. | No publication; dispose only successfully created/registered owned resources. A registrar is not allowed to perform work. |
| registered → initialized | All enabled runtime instances entered/finished ephemeral initialization topologically. | Record partial-entry cleanup; reverse cleanup; no store/business writes are allowed during initialize. |
| initialized → migrated, prepare | Existing verified current history, or assigned provisioning/migration completed in dependency order under one exclusive scope. | Earlier local commits persist; failed/uncertain target blocks, records 003 durability and explicit recovery guidance. |
| initialized → migrated, serve | All target histories/mappings verified already current under host lease; zero mutation. | Unprepared/missing/current-target mismatch is not repaired or treated as ready. |
| migrated → enabled | Serve only: all Start callbacks and fresh health checks completed successfully, then whole-set publication. | No partially ready set; stop/reverse cleanup all entered resources. Prepare never enables. |
| enabled → healthy/degraded/failed/unknown | Fresh own health combined with transitive prerequisite impact. | Unknown/stale/degraded/failed is unavailable aggregate readiness, not a lifecycle rerun or global rollback. |
| enabled/partial → stopping → stopped | Withdraw readiness/publication; reverse entered order and cleanup registrations; process exit verified. | Incomplete cleanup/termination remains explicit; no falsely stopped worker or deleted recovery evidence. |

Each phase is a whole-enabled-set barrier; within it prerequisites precede
dependents. `migrated` on a no-storage module is an explicit not-applicable
completion with zero recipe/binding invocation. Prepare stops after migration
and cleanup; serve repeats ephemeral registration/initialization in a new
process, verifies rather than repeats migration, then starts.

For partial initialization/Start, mark entry **before** invoking author code;
register owned cleanup actions as resources are acquired. Stop is required to
be safe after partial entry and called at most once for that entered runtime.
One final provider creates ordinary owner scopes; module services are scoped/
transient, not root-owned singletons. Dispose owner scopes in reverse module
order after Stop/registered cleanup, then the host provider. Native DI disposes
its created objects once.
The framework must not also dispose a container-owned runtime independently.
If one callback cannot be stopped, terminate the owned process: do not run
later cleanup callbacks concurrently with abandoned work.
Lifecycle callbacks perform no irreversible business/external effects;
owner-local business operations run explicitly after activation. Durable
lifecycle mutation is limited to the admitted, intent-tracked migration path.

## Health derivation

Own health is `unknown`, `healthy`, `degraded` or `failed`. Effective health
is the worst of current own health and enabled transitive prerequisites:
`failed` dominates `unknown`, which dominates `degraded`, then `healthy`.
A stale/missing observation becomes unknown before this calculation.
Record dependency impact separately, so a healthy dependent with a failed
prerequisite is not falsely advertised healthy and an independent root remains
identifiable. A normal callback can return fixed `dependency_unavailable`;
it cannot forge another owner's observation.

Sweep order is topological; refresh at most once concurrently. A completed
sweep schedules the next after ten seconds. Callback cap is one second,
sweep cap twenty seconds, freshness thirty seconds from the actual individual
observation. Expired observations become unknown even if a sweep is stuck.
Initial healthy observations gate readiness; background degradation withdraws
aggregate readiness without deleting data or implicitly disabling modules.
Uncooperative health callbacks terminate the owned serving child, not just a
discarded Task. Synchronous pure resolver work is bounded by input and deadline.

## Persistence, concurrency and compatibility

No new relational table, durable attempt journal, secret store or event/job
queue. The operator file is authoritative across restart; registry format 1
remains unchanged. Runtime read snapshots are atomic in-memory values.
Configuration changes while running are not applied; a controlled new attempt
reads a new immutable snapshot. Private file permissions/ownership and bounded
reading are rechecked; a missing explicitly selected file is an error.

Preparation holds the existing exclusive lease for the entire process work;
serving holds the existing host lease through termination. Never upgrade a
shared lease in place, release scope while a child may still write, silently
reacquire a lost PostgreSQL session or infer that an abandoned SQLite lock is
safe to delete. The parent never starts serving after failed prepare.

Accepted 002 schema 1/host contract 1.0.0 and 003 storage contract/registry/
marker versions remain independent of composition contract 1 and module
release versions. Unknown composition/config/recipe contract versions reject
before callbacks. A new binary must reread persisted assignments and actual
histories; previous observed health has no compatibility authority.
