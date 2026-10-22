# Storage Contract v1

**Status**: Approved design; implementation stage complete, not human-accepted.
The human explicitly approved [ADR 0005](../../../docs/adr/0005-module-storage-ownership.md).
See [actual implementation and local/CI evidence](../implementation.md).
This document defines the target contract; it does not prove every requirement
is implemented. No public registry endpoint, module loader or authorization
role is introduced by this storage slice.

The executable consumer now implements all nine required examples. Its `all`
result includes actual per-scenario assertion counts, and it refuses a missing
case or any missing outcome in captured diagnostics. The named suites retain
the existing bootstrap/manifest checks and add actual storage integration,
crash/recovery and captured privacy tests; unavailable providers fail explicitly.
The [guide](../quickstart.md) runs both providers without exposing private
configuration in command arguments.
The T079-T080 increment additionally requires bounded public `observations`:
three fresh processes preserve both exact assignments and original sentinels;
a forced distinct-ID UNIQUE conflict preserves the registered owner's assignment,
data and complete inventory, leaves the contender unallocated, and retains
unowned-target refusal. Standalone and `all` reject missing/incomplete observations
even when assertion totals are positive. Test-support-only access to the existing
internal allocator seam adds no production candidate override.

## Authority and public shape

Provider-neutral contracts live in `Kofte.Platform.Modules.Storage`.
EF/provider binding lives in `Kofte.Infrastructure.Storage`.

| Surface | Input and output |
| --- | --- |
| `IStorageRegistry.RegisterAsync` | Validated 002 `ModuleDescription`, installation maintenance scope, cancellation; correlated immutable `StorageResult` |
| `IStorageRegistry.ResolveAsync` | Canonical `ModuleId` from trusted composition, cancellation; current verified assignment, no-storage or explicit failure |
| `IModuleStorage` | Host-bound owner-only accessor; no public `Resolve(otherId)` or caller-selected connection/schema/file |
| `StorageAssignment` | Read-only contract/provider/owner/allocation identity and boundary metadata; host-issued binding required for actual context access |
| `IStorageMaintenance` | Acquire exclusive installation scope, explicit upgrade, provision/retry and module-local migration; unavailable to normal module DI |

The current provider-neutral interface exposes `AcquireAsync` and `UpgradeAsync`.
Explicit registration uses the held scope through `IStorageRegistry`; EF
migration uses the infrastructure-issued owner binding, not extra methods on
the pure maintenance interface. `UpgradeAsync` returns `Task`: successful registry
upgrade has no module assignment to return. Safe `StorageException.Code` reports
maintenance failure; register/resolve return the disjoint `StorageResult`.

A valid description proves syntax/compatibility, **not caller authorization**.
Only the trusted composition/maintenance root receives the registry service.
Module DI receives a binding to its own owner. Assignment descriptions are not
bearer capabilities: copying/constructing fields cannot mint a working binding.
Infrastructure must revalidate owner, installation, allocation token, revision
and target at actual open/migration, reject a foreign/expired/disposed binding,
and expose no API that accepts a caller-chosen physical override.

Returned metadata contains no connection string, credential, absolute path or
module payload. Module-local EF connection construction is internal to the
binding adapter; trusted code can still inspect its own process/connection.
This is supported-use ownership enforcement, **not a sandbox against trusted
code with shared database credentials or OS access**. Future UI/REST/MCP/jobs
must authorize actor/registered permission/team/resource against the same
operation before receiving any protected result.

## Configuration and compatibility

Reuse `KOFTE_PROFILE`, `KOFTE_DATA_ROOT`, `KOFTE_POSTGRES_CONNECTION` and the
existing profile/loopback/owned-fixture checks. The maintenance invocation alone
may read `KOFTE_POSTGRES_PROVISIONING_CONNECTION`; never log it or add it to
serving-host DI/configuration. Serving mode rejects this elevated setting.
The supervisor/verifier explicitly passes it only to owned maintenance children.
Do not introduce default credentials or a production/remote PostgreSQL profile.

Both PostgreSQL connections must identify the same verified owned database;
validate runtime role flags and its lack of database CREATE, plus provisioner
permissions/ability to SET ROLE before physical creation. Missing provisioning
credentials do not prevent ordinary verified resolution or the zero-module host.
They do prevent PostgreSQL schema creation with an explicit privilege outcome.
SQLite initialization retains the current canonical/private-root requirements;
normal startup/open never creates a missing platform store.

Contract v1 returns only recognized provider tags `postgresql`/`sqlite`.
Registry/marker formats must be supported before any write. A configured
provider/root change does not migrate data; reject inconsistent installation
evidence. ID/display/release changes never silently move existing storage.

## Allocation rules

Allocator v1 consumes **only canonical 002 ID bytes**, never display metadata.
Build a lowercase ASCII slug by replacing dot/hyphen with `_`, collapsing runs,
trimming, taking at most 24 characters and trimming a trailing `_`. Empty slug
uses `module`. Append the first 32 lowercase hex SHA-256 characters:

```text
m_<slug>_<hash32>
```

This is at most 59 bytes, matching `m_[a-z0-9_]+_[0-9a-f]{32}`. Always validate
the **actual stored/candidate bytes**, never rely on PostgreSQL truncation.
Reject length above 63 bytes, uppercase/non-ASCII/SQL syntax, reserved exact
`public`/`information_schema`, prefixes `pg_`/`kofte_` and all platform-owned
names. Platform schema provisioning is a separate operation, not an exception
available to modules. Use validated/quoted identifiers, parameterized values.

Persist allocator version/key when reserving. Existing records are read, never
regenerated or compared to today's candidate as an upgrade precondition.
Database UNIQUE constraints protect ID/allocation independently of hashing.
Check actual database schema/file existence too. Same-ID compatible requests
return the stored mapping; conflicting need/provider/target or distinct-ID
candidate collision is rejected. No automatic suffix, alias, rename, adoption
or destructive cleanup. Tests must force candidates through an internal test
seam, never expose a production candidate override.

PostgreSQL physical key is the schema. SQLite physical key resolves only to
`modules/<key>/module.db` under the trusted root. No caller-supplied relative
or absolute locator is accepted. Validate every ancestor/root, canonical
containment, effective owner/private modes, regular files, one hard link and
absence of symlinks/unsafe sidecars before access. Exclusive-create new files;
ready access uses existing-file mode. Revalidate at open; private roots are not
promised safe against malicious same-user/privileged mutation.

## Outcomes and diagnostics

`StorageResult` is disjoint: only `ready` carries a usable assignment;
`no_storage` carries none; every other outcome carries code, operation,
correlation, retry guidance and truthful durable-state indication.

| Outcome | Examples and permitted next action |
| --- | --- |
| `ready` | New verified store or verified same-ID resolution/retry |
| `no_storage` | Persisted non-relational declaration, zero module stores |
| `busy` | Maintenance lease/known provider contention; retry boundedly |
| `rejected` | Invalid/foreign binding, conflicting declaration, reserved/path/collision/unowned target; correct input/configuration, not auto-retry |
| `incomplete` | Reserved/uncertain committed phase; inspect and retry same allocation under maintenance |
| `reconciliation_required` | Missing/corrupt/mismatched ownership, ambiguous pre-marker file, interrupted migration; preserve evidence and follow runbook |
| `failed` | Unavailable provider, denied privilege, unsupported version, expired deadline or cancellation; code distinguishes cause, no success fallback |

Stable codes include `storage_busy`, `storage_conflict`, `storage_target_reserved`,
`storage_path_rejected`, `storage_owner_mismatch`, `storage_missing`,
`storage_corrupt`, `storage_incomplete`, `storage_reconciliation_required`,
`storage_version_unsupported`, `storage_privilege_denied`, `storage_unavailable`,
`storage_deadline_exceeded`, `storage_cancelled` and `storage_request_invalid`.
Codes are versioned contract identities, not provider exception strings.
Use fixed guidance keys `retry_same_request`, `correct_configuration`,
`inspect_preserved_target`, `restore_complete_set` or `upgrade_supported_binary`.
No broad catch that converts an unknown failure into ready/no-storage/busy.
Known exceptions are mapped explicitly; unexpected errors produce sanitized
terminal failure and operator-visible correlation.

One result has at most eight fixed diagnostics, each message at most 256
characters, guidance/code at most 64 ASCII characters and total serialized
result at most 4096 UTF-8 bytes. Canonical module ID is optional on invalid
input; never echo raw invalid text. Correlation is a generated 32-hex W3C trace
ID, not untrusted caller text. Structured records include safe operation,
provider, outcome/code, canonical module ID when valid and correlation only.
Module IDs are not high-cardinality metric labels; counters use bounded
operation/provider/outcome labels. Trace/log payloads never contain connection
strings, absolute paths, SQL, stack traces or module rows.

Every mutation and rejected boundary check emits an audit-relevant structured
record through existing host telemetry conventions. This is observable current
evidence, not the future centralized durable audit product. Public consumer
stdout/stderr, worker output, span/log records and metrics all obey redaction.

Explicit upgrade records use `operation: upgrade`, null module identity and the
existing `storage_ready`/`ready` success vocabulary without constructing a
module result or assignment. Logs and spans also carry fixed guidance and
`unchanged`, `uncertain` or `committed` durability. A direct v0 commit is committed;
a verified v1 retry is unchanged. Errors after a possible commit never claim
rollback. Failed acquisition emits an `acquire` record; successful acquisition
and disposal need no independent records.

The supervised CLI retains its `registry_upgraded` success summary and emits a
safe parent-side upgrade record. Only allowlisted worker exit identities cross
that boundary; raw stdout/stderr remains drained, never forwarded. The parent
conservatively records uncertain durability once the child may write, because
its exit identity does not carry commit evidence. Its generated correlation
identifies the parent record, not a forwarded child record or durable audit entry.

## Deadlines, retries and cancellation

| Operation | Total budget | Nested limits |
| --- | --- | --- |
| Resolve/open | 3 seconds | Provider connect/command at most remaining budget; SQLite command/busy at most 1 second |
| Maintenance acquisition | 2 seconds | Nonblocking lock probe, retry interval at most 50 ms; part of enclosing budget |
| Register/provision/retry/registry upgrade | 30 seconds | PostgreSQL connect/command at most 3 seconds and remaining budget; no automatic replay of uncertain writes |
| Single module migration/recovery-set verification | 120 seconds | Same bounded connect/commands; worker termination/reaping reserved inside total |

No zero/infinite timeouts or unbounded loops. One public attempt does not
automatically retry the whole operation; known lock/SQLite-busy waits consume
the same monotonic deadline. A caller may retry the *same* request with a new
budget after inspecting durable outcome. Cancellation wins before mutation;
after a possible commit return cancelled/incomplete with durable-state
uncertainty, never assert nothing changed.

Microsoft.Data.Sqlite async is synchronous. Bounded commands plus cancellation
checks govern short reads. Long offline migrations/initialization use a
supervised owned maintenance worker where synchronous provider execution cannot
honor cancellation; stop/terminate only that child, reap and report incomplete.
The parent must not abandon a still-writing worker or release its installation
scope early. PostgreSQL likewise retains the non-pooled lock session until
worker completion/termination. Kernel-blocked disks/process termination outside
supported local filesystem conditions are explicit infrastructure failures;
test actual elapsed deadlines on available supported machines.

## Required public examples

The independent consumer accepts inert fixture scenario names, **not arbitrary
SQL/schema/file input**, and emits only sanitized result JSON.

| Example | Required observed behavior on each provider |
| --- | --- |
| `clean-install` | Alpha/Beta register, migrate independently, write/read sentinels |
| `no-storage` | Register false declaration; no assignment or created module object |
| `same-id-retry` | Display/release change and three restarts preserve stored mapping/data |
| `collision` | Forced distinct-ID candidate conflict preserves original owner/data |
| `path-rejection` | Injected traversal/link/foreign-boundary attempt changes no target |
| `interrupted-provisioning` | Kill at each durable boundary; retry original owner or explicit preserved reconciliation block |
| `corrected-retry` | Fix denied/unavailable configuration and retry without reset/deletion |
| `isolated-upgrade` | Alpha history/model/data changes; Beta/platform unchanged |
| `stopped-restore` | Verify and restore a complete set; mixed/stale replacement refused |

These examples are acceptance targets, not a product administration CLI or an
implemented command today. [Quickstart](../quickstart.md) specifies their
execution/evidence path; [migration/recovery](migrations-and-recovery.md)
defines the state-dependent checks.
