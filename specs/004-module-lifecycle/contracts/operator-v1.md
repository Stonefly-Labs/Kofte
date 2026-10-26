# Operator, Lifecycle and Observation Contract v1

**Status**: Proposed 004 interface; commands/examples below are future targets.
No new command, configuration key, endpoint or supervisor is implemented by plan.

## Deployment, configuration and authority

The trusted deployment root supplies an explicit compiled catalog to the public
host integration entry point. No assembly filename, network location, executable
type name, storage override or install script is accepted from settings.
The ordinary production catalog is empty unless deliberately composed by an
operator-maintained deployment; the independent test deployment lives in `tests/`.

Optional `KOFTE_MODULES_FILE` selects an existing operator-owned regular file.
Resolve against the explicit working directory once, reject symlink/hard-link/
unsafe-owner/group/world-readable file or unsafe containing directory, and
limit reads before parsing. Do not log the resolved path. An unset variable
means an empty selection; an empty variable, missing explicitly selected file,
bad ownership, malformed UTF-8/JSON or unknown contract fails closed.
Catalogs with no selected entries remain disabled, not implicitly enabled.

```json
{
  "schemaVersion": "1",
  "modules": [
    {"id": "org.example.alpha", "enabled": true, "settings": {}},
    {"id": "org.example.beta", "enabled": false, "settings": {}}
  ]
}
```

This is a **selection** document, not a new manifest grammar. Every entry uses
accepted canonical IDs and explicit boolean enablement. Duplicate decoded JSON
keys/IDs, unknown envelope/entry fields, null defaults or invalid settings reject.
An entry for a nondeployed ID rejects (enabled entries specifically report
missing implementation); omitted deployments are disabled. Disabled settings
are checked only for envelope/size validity, not executed through author
validators; disabled modules receive zero registrar/factory/runtime invocations.
The file is copied once into an immutable attempt snapshot. Operators use
atomic file replacement while stopped; running processes never apply edits.
The parent passes a bounded private snapshot to its admitted child, not a file
path that the child rereads after validation. No secret-bearing command argument,
public settings echo or published settings hash.

Before 015, required secrets may be supplied through the private owner settings
snapshot; this is limited secret consumption, not secret-management/reference
resolution. No ambient fallback, network vault integration or default credential
is added. Keep the file untracked/private and exclude it from artifacts.

## Command and phase behavior

| Invocation / context | Behavior |
| --- | --- |
| Normal empty bootstrap | Existing direct host behavior, empty valid composition, no module allocation; existing v0/v1 platform handling unchanged. |
| `--modules validate` | Read-only composition + inventory/recipe preflight; no registrar/factory/runtime or store mutation. Use owned supervision because author validation can ignore cancellation. May succeed for genuinely new owners admissible for prepare, but does not assert serving readiness. |
| `--modules prepare` | Explicit stopped maintenance. One admitted owned child acquires the existing exclusive scope; validates everything, registers/initializes ephemeral modules, provisions/migrates enabled owners topologically, cleans up and exits. Never starts serving or marks modules enabled. |
| Normal configured serving | Owned parent supervises a serving child of the same explicit deployment executable/catalog. Child uses normal runtime credentials and host lease; repeats preflight/ephemeral stages and verifies already-prepared stores, then starts/publishes the whole healthy set. |
| Controlled stop/restart | Withdraw readiness/publication, reverse entered-module cleanup, verify owned process exit; operator edits desired file, then starts a new attempt. No live disable, cascade, uninstall or data deletion. |

Read-only validate holds the existing host/read lease across storage inspection
(SQLite's exclusive local host lease or PostgreSQL's shared advisory host
lease), so it cannot race maintenance. It may report bounded busy rather than
inspect a running SQLite installation. Prepare acquires exclusive authority
before reading inventory and retains it through its mutation/cleanup lifetime.

`validate`/`prepare` emit safe bounded JSON records and exit 0 only for their
actual successful boundary; 2 for rejected input/invalid composition, 3 for
busy authority, 4 for reconciliation-required, 1 for operational failure/
deadline/cancellation/incomplete cleanup. Normal serving remains running after
startup and exits nonzero on a failed/unverified lifetime. Unknown CLI/worker
arguments or absent admission proof reject; no user-invokable worker bypass.

Preparation requires an already supported platform registry; it never upgrades
v0 automatically. Use accepted `--registry upgrade` explicitly when first
preparing relational owners. Existing missing/unowned/corrupt stores cannot be
adopted as new. A missing *registration* may be planned for explicit prepare;
serving instead reports `composition_prepare_required`. A relational definition
must declare supported owner-bound migration recipes for both SQLite and
PostgreSQL; no-storage definitions declare none. Recipe target is fixed by the
deployed release, never arbitrary user SQL/migration text.

Before module writes, use infrastructure-only read-only inspection to validate
the complete existing inventory, each immutable declaration/mapping/history and
all pending assignment-aware models/operations. An unallocated owner uses a
validated candidate boundary only for structural preflight; it is not a
`StorageAssignment`/binding and cannot open a store. Actual allocation collisions,
markers and lease validity repeat inside accepted 003 immediately before use.
The accepted allocator/UNIQUE constraint remains final authority.

Only after this entire barrier: register services, initialize ephemerally,
`RegisterAsync`/`BindAsync` then migrate in stable dependency order. Skip an
already-current migration after actual history/model verification; ordinary
restart never mutates its registry revision. No-storage completes migration
as not-applicable with no binding/migration call or fabricated assignment.
No module is advertised active during preparation.

For serving, all modules finish Initialize before read-only migration
verification completes; all verification finishes before Start; all Starts
complete topologically before the first healthy sweep and atomic publication.
Start may inspect its owner store and explicitly bound public prerequisite
operations, not cross-module tables or arbitrary cross-owner DI services.
Start/health/Stop have no irreversible business/external effects; business
mutations are explicit module operations after activation, not automatic
startup hooks. The host's HTTP bootstrap allowlist remains in place;
inert route claims do not expose a business endpoint.

## Budgets and supervision

All deadlines use monotonic elapsed time; nested work receives the lesser of
its own cap and the remaining parent work budget. Cleanup reserves are inside,
not added after, the total. No whole-operation automatic replay.

| Boundary | Total cap | Work / reserve and nested constraints |
| --- | --- | --- |
| Pure resolver | 2 s | Input-bounded work; reject deadline/overflow, no callbacks/IO. |
| Read-only validate command | 45 s | 35 s work, 10 s stop/terminate/reap; storage preflight 30 s total. |
| Prepare command | 120 s | 105 s work, 15 s cleanup/terminate/reap; acquire ≤2 s, register ≤30 s each, migration ≤120 s each, all clipped by remaining work. |
| Configured serving startup | 60 s | 45 s work, 15 s cleanup/terminate/reap on failure; read-only storage preflight ≤30 s; existing short core probes ≤3 s. Must also fit the existing whole-stack 285 s work/300 s total budget. |
| Settings validator / registrar | 250 ms / 1 s each | Enabled owners only; a timeout rejects/terminates the owned process, not a continuing callback. |
| Initialize / Start | 2 s each per owner | Same enclosing work cap; large/slow valid sets may fail the deadline rather than expand it. |
| Health callback / sweep | 1 s / 20 s | Sequential; ten-second interval after completion; thirty-second individual freshness; no overlapping sweeps. |
| Stop/dispose | 15 s total | First 10 s cooperative reverse cleanup, last 5 s owned termination/reap; per Stop/cleanup action ≤1 s clipped by remaining cooperative time. |

Accepted 003 resolution/open ≤3 s, acquisition ≤2 s, provisioning/upgrade ≤30 s
and module migration/recovery ≤120 s are unchanged. The composition batch
budget can be **stricter**; it does not allocate 120 s per owner cumulatively.
Insufficient remaining budget fails before entering the next mutation.
Maintained connections/child lifetime retain authority until verified exit.
The configured HTTP host uses a 500-ms internal graceful shutdown timeout so
Kestrel can abort incomplete requests and finish before its unchanged one-second
cleanup-action deadline. This does not extend any author callback, cooperative
reserve, process termination or total budget. In-flight bootstrap requests may
be disconnected during controlled stop; they are not durable business work.

Use accepted admitted-process ownership patterns, with long-lived serving
supervision added in the host (do not apply the existing at-most-120-second
maintenance helper to the entire serving lifetime). The explicit deployment
executable handles its own admitted worker mode and reconstructs the same inert
catalog, not the default empty catalog from another entry assembly. Strip
provisioning credentials from serving children; private snapshots/admission
travel through bounded inherited private channels, never command-line settings.

Callback cancellation is cooperative. At an uncooperative timeout, the parent
must terminate/reap **only its owned process**, with safe incomplete/uncertain
evidence, rather than dispose live resources concurrently or return success.
Use actual exit evidence; `Task.WaitAsync` timeout is not termination.
Root `WaitForExit` alone does not prove descendants exited. Module-spawned
processes are unsupported; owned fixture descendants require explicit tracked
PID/ownership and exit observations. A kernel-blocked/unverifiable termination
is infrastructure failure with incomplete cleanup, never a claimed guarantee.
No name-based killing, arbitrary PID input, abandoned-task retry, deleted lease
file or silent lease reacquisition. Accepted 003 intent/ownership checks block
automatic reuse of uncertain durable work. Pure ephemeral failed startup may
retry only after owned process exit has actually been verified.

## Health and anonymous projection

Operator snapshots separate desired state, stage, own/effective health, safe
dependency impact and current attempt/age. Missing/stale health is unknown;
degraded/failed/unknown enabled owners withdraw aggregate readiness. Independent
roots stay distinguishable in the private snapshot, not the public response.
No unbounded custom health message or settings-derived reason.

Keep the existing OpenAPI/bootstrap contract: `version`, `state`, `component`,
`correlationId`, existing state/component vocabulary and cache-control.
`/health/live` remains live process-only. `/health/ready` still requires the
selected-store probe and now also the immutable current composition snapshot;
return existing unavailable/503 with `component: "host"` for composition failure,
never misuse `"storage"` to imply a module reason. Healthy composition adds no
fields or counts; provider failure retains existing storage projection.
The five-second HTTP deadline/three-second store probe remain; health callbacks
are refreshed by the lifecycle loop, **not** executed by an anonymous request.
The web adapter/status/accessibility contract remains unchanged.

## Structured records and privacy

Each discovered/validated/registered/initialized/migrated/enabled/health/stop
boundary and refusal emits an operator-visible safe record. Parent validates
framed child records against a fixed schema/version, known catalog identity,
phase/outcome enums and current opaque attempt token; cap frames at 4,096 bytes,
256 records/attempt plus a reserved terminal record. Never allocate an
unbounded line buffer. Additional transitions beyond that cap terminate with
an explicit output-limit failure, not silently discarded success. Steady-state
health emits only changes and periodic bounded aggregate summaries, with a
separate 64-record/minute cap and explicit terminal refusal on overflow.
Raw child stdout/stderr, including callback console writes, are drained and
discarded; they are never passed through as diagnostics. This is safe platform
projection for trusted code, not a sandbox against malicious OS/network writes.

Records include version, generated 32-hex attempt trace ID, fixed mode/stage,
canonical owner when valid, stable code/outcome/guidance, elapsed/bounded counts
and storage durability only where actually known. Related dependency IDs are
bounded private fields, never metric labels. Messages are host catalog text;
unknown/raw values/exception/path/SQL/settings are forbidden in records/spans.
Link existing 003 child correlations without relabeling old success as current.
Record every rejected boundary, including malformed config, admission,
acquisition and observation projection failures, even without a module identity.
Required output failure is an operational failure, not successful invisible work.
Infrastructure cleanup failures additionally identify their fixed private
boundary with `composition_host_stop_incomplete`,
`composition_host_dispose_incomplete`, `composition_services_dispose_incomplete`
or `composition_storage_dispose_incomplete`. These nonterminal
`stopping / incomplete` records retain the same attempt and fixed
`inspect_preserved_state` guidance, without an owner, raw exception or new
record fields. The final ownerless `cleanup / incomplete` record remains the
terminal refusal; a boundary record cannot substitute for verified exit.
The composed host disables default JSON reload watchers before constructing
its slim builder. Configuration sources are still cleared for the admitted
snapshot; unused file watchers must not survive that clearing and consume
the one-second host-disposal action. Cleanup/termination budgets and
incomplete/uncertain refusal are unchanged.

After successful composition resolution, every explicitly disabled or omitted
deployed owner emits exactly one additional version-1 record with
`stage: discovered`, `outcome: disabled`, `code: composition_disabled` and
`guidance: edit_selection_while_stopped`, under the current attempt. This is the
existing snapshot's `DesiredEnabled = false` observation, not successful
activation or failed health. It runs through the same private framed protocol,
parent projection, telemetry and record/frame budgets as other records.
The parent and worker copy the admitted enabled-owner set and refuse disabled
claims for those owners, unknown owners, missing identity, contradictory
stage/code/outcome/guidance or attached dependency/settings/storage evidence.
No new record fields, protocol version, authority or anonymous response fields
are introduced. These observations invoke no disabled author callback, allocate
no store and do not assert that later storage preflight or serving succeeded.

Use Activity/Meter and existing telemetry patterns. Metric dimensions only
`mode`, `stage`, `provider` (`sqlite|postgresql|none|unknown`) and fixed `outcome`;
no IDs, correlation, settings fields or local paths. This is observable
audit-relevant evidence, not 009 durable audit persistence.

## Failure, corrected retry and recovery

| Boundary failure | Required evidence / next action |
| --- | --- |
| Invalid manifest/config/graph/claims | Whole-set blocked, zero platform-invoked mutation; correct named safe field/deployment/selection and start a new attempt. |
| Registration/Initialize/Start failure | Withdraw publication/readiness; record stage, clean only entered resources, verify exit before corrected retry; preserve stores. |
| Busy/lost authority | Accepted 003 bounded busy or cancelled/reconciliation outcome; do not reacquire and continue or begin another migration. |
| Partial migration/uncertain commit | Retain actual earlier data/history and failed-owner intent; inspect using explicit 003 recovery/full-set procedures, no blind replay. |
| Degraded/stale/unavailable health | Honest private own/effective state and aggregate unavailable; refresh/correct cause; no implicit disable or migration rerun. |
| Cleanup/output/termination unverified | Explicit incomplete/operational failure and preserved evidence. No next serving process or success until operator verifies safe reconciliation. |

Disabling removes active contributions only on the next coherent restart;
enabled dependents on a disabled prerequisite reject before mutation.
Re-enabling uses original assignments/data/history and revalidates everything.
Preparation never changes disabled-owner rows. Backup/reset/restore retain
accepted 003 stopped complete-set and legacy-command refusal rules. A binary
rollback must understand registry/current histories or use a deliberately
chosen complete prior set; no automatic Down/global rollback is introduced.
