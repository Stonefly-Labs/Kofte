# Migration and Recovery Contract

**Status**: Human-approved 003 design; implementation is incomplete, not
accepted. See [actual evidence and remaining work](../implementation.md).
Assigned contexts/migrations and stopped whole-set exercises have focused
both-provider proof. Supported DDL is currently limited to assigned
create-table/primary-key and add-column operations with int/text columns;
raw SQL, transaction suppression, foreign/reserved/unknown targets and other
operations/annotations fail closed before pending SQL. Killed-migration/
abandoned-lock recovery, the durable crash matrix, complete worker/deadline
proof and executable public recovery examples remain incomplete. Do not delete
EF lock rows or fabricate history to bypass that block. Remaining sections
define required behavior, not proof that every obligation has passed.

## Installation scope and host integration

Keep the accepted `.platform.lock` exclusive host lease on SQLite and
`PostgresPlatformStore.OperationLeaseKey = 0x6b6f667465000001` shared host
advisory lease. All initialization/registration/provisioning/migration/recovery
uses the exclusive form of that same lease. A running host blocks maintenance.
Mutations occur before serving or while stopped; no new live administration
endpoint is added. Timeout/cancellation cannot bypass the lock.

Reuse the same underlying lock across moved infrastructure helpers and host
delegation. Acquire once per maintenance scope; internal nested operations use
that scope, not independently competing/reentrant leases. Hold the PostgreSQL
dedicated non-pooled connection until all transactions/worker activity finish.
Loss of that session cancels further work; never reacquire and continue silently.
SQLite's local lease file must not be removed/replaced as a recovery trick.

## Registry v0 to v1

Add explicit local maintenance syntax `--registry upgrade` for both existing
profiles. This is an explicit maintenance command, separate from `--store init`.
SQLite init still creates a table-free bootstrap v0; host startup accepts
valid module-free v0/v1 but never upgrades either automatically.
PostgreSQL keeps its existing owned fixture bootstrap provisioning.
Initialization must refuse leftover module evidence even if the platform file
is missing; creating a new registry must not orphan or reassign old stores.

Under exclusive maintenance, validate target/root/role ownership and inspect
version/tables before any write. Only the accepted table-free v0 is an
initialization source. Versioned provider-specific registry SQL migrations
create metadata, ID/allocation uniqueness, state checks and platform-local
`__kofte_registry_migrations` history. Script execution, digest/version history
and metadata commit together, without invoking EF's module migration lock.
SQLite sets `user_version=1` in that transaction. PostgreSQL metadata
is its registry format authority. Unknown pre-existing application tables,
missing/inconsistent metadata, incompatible/newer version or malformed history
block; never drop or reinterpret them.
Registry failures project onto the existing sanitized bootstrap health
categories/readiness behavior; no raw allocation or new HTTP schema is added.

The upgrade is idempotent only for a fully consistent v1. Failure/kill before
commit leaves v0; lost acknowledgement requires rereading actual version/history.
Failure after a committed upgrade is not an excuse to rerun v0 initialization.
Test both sides of commit and preserve bootstrap file contents/ownership
metadata. No `EnsureCreated` shortcut or fictitious applied EF history.

Old binaries cannot safely use/restore registry-bearing data. Deployment must
not roll back binaries against format 1 without an explicit compatible version
or complete pre-upgrade recovery set. Supported recovery is forward with a
compatible binary or restoration of the complete stopped pre-change set.
No automatic down migration or destructive retry.

## Registration and physical creation

1. Verify validated manifest, immutable existing declaration and provider; obtain
   exclusive installation scope. No-storage writes an identity-only row.
2. For a new relational ID, generate/check candidate, reserve ID/key/token in a
   platform-local transaction, then commit. Constraint conflicts are explicit.
3. Verify candidate has no pre-existing unowned target. PostgreSQL creates schema
   and marker in one transaction with the offline provisioner, correct runtime
   ownership and PUBLIC grants removed. It does not use `IF NOT EXISTS`.
4. SQLite exclusively creates its private module directory/file, checks links/
   sidecars, initializes DELETE/FULL/foreign keys and commits the marker locally.
   Never use create-if-missing mode on retry of an existing ready allocation.
5. Reopen/verify physical marker, provider, location and integrity; commit ready
   in the platform registry. Return a host-bound assignment only after this.

Crash before reservation commit leaves no registration; after reservation but
before creation can safely create the same absent target. PostgreSQL physical
transaction rollback leaves an absent target; a matching committed marker can
finish the original reservation. SQLite crash after file/directory creation
but before a valid marker is **ambiguous**: preserve the object and block.
Do not infer ownership from an empty file, directory name, timestamp or hash.
This explicit reconciliation block satisfies truthful recovery; no automatic
deletion/adoption is promised. Matching physical evidence after a lost final
response permits completing only the same immutable allocation.

Existing ready resolution is read/verify only: no allocation regeneration,
object creation, state repair or fallback. A missing/corrupt/foreign store or
interrupted migration gives explicit failure and requires maintenance.

## Module-owned EF boundary

The small infrastructure binding accepts a host-issued assignment, a
module-owned context factory and provider-specific migration set. It creates
the connection/options internally; module input has no schema/path override.
The factory must return the expected owner-bound context, and cannot mutate a
different assignment into the binding.

For PostgreSQL, apply `HasDefaultSchema(assignedSchema)`, explicit matching
object schemas and `MigrationsHistoryTable("__EFMigrationsHistory", assignedSchema)`.
Cache keys include context type, provider, assigned schema and design-time
flag. For SQLite, use only assigned `module.db`, no schema argument/`ATTACH`,
and history in that file. Validate the final model, not merely default options;
an explicit foreign table/view/sequence/FK principal schema is rejected.
Reserve `__kofte_storage_owner`, migration history and provider migration-lock
objects against normal module application mappings.

**A model default is not a migration binder.** The initial contract provides
assignment-aware `Migration`/`ModelSnapshot` bases and an `IMigrationsAssembly` adapter which
binds the instance before reading `UpOperations`, `DownOperations` or
`TargetModel`, and binds its `ModelSnapshot` before the snapshot model is read.
Module-owned migrations and snapshots use the assigned schema
for every schema-bearing operation/model object. Separate provider context
types/migration sets remain in the owning module/test project. Use public EF
service interfaces rather than depending on provider-internal history classes.

Preflight **all pending migrations** before executing any of them: target model,
operation schemas (including nested table constraints/FK principal schemas,
rename old/new schema, indexes/sequences and data operations), reserved object
names and provider annotations. Support only explicitly recognized, locally
bound operation kinds; unknown/provider-extension operations, transaction
suppression, raw `SqlOperation`, unbound snapshots or another schema are
rejected before writes. Do not rewrite generated SQL strings or treat
`search_path` as isolation. The first proof includes create-table plus additive
upgrade; expansion of allowed operations needs focused provider tests.
Trusted code can still bypass the adapter directly; this is not SQL sandboxing.

Record migration intent in registry before execution, verify owner/history,
run under the same installation scope, then verify actual model/history before
recording current checkpoint. No general module discovery, dependency sort,
enablement or cross-module batch transaction is added; 004 owns orchestration.
On failure keep the original mapping, data and actual history; mark or retain
recovery-needed. Restart compares actual module history with durable intent,
not a presumed all-or-nothing registry/module commit.

EF's SQLite `__EFMigrationsLock` can survive process death. A surviving lock
row is not proof another process is alive and must not trigger infinite wait.
After acquiring the exclusive installation lease, stopping all owned workers,
verifying marker/history/integrity and inspecting the incomplete migration,
the documented explicit recovery action may clear **only the assigned store's**
abandoned EF lock. Never clear it during ordinary resolution/retry, delete a
hot journal, or advance history by hand. Resume a verified unapplied migration
or block for forward repair/full-set recovery if physical effects are uncertain.
Tests must actually kill a migrating process and demonstrate this path.

## Backup/restore and legacy command guard

Registry and module stores are one **recovery set**, not one transaction.
Automated whole-installation backup orchestration remains 022. The current slice
provides documented stopped procedures plus validation and test-owned execution.

Existing `--store backup`, `restore` and confirmed `reset` continue working for
genuine v0 bootstrap stores only. They must refuse source/destination registry
metadata/history, unsupported/corrupt format or module-directory evidence,
including a missing/reset platform file with leftover module stores. Inspect
both backup input and target before restore. A valid legacy backup is not
permission to replace a registry installation. Never delete a registry file
merely because `--confirm-delete-platform-data` was supplied. Refusal is a
deliberate documented storage-maintenance compatibility change, without a
health/OpenAPI/UI contract change.

### Stopped SQLite procedure

Stop host/maintenance children; acquire exclusive installation lease for the
whole snapshot. Verify complete registry/physical inventory and integrity, no
unowned module directories, all owner tokens and migration checkpoints.
Close/checkpoint owned connections and require a clean non-hot rollback journal
state. Do not copy a live database or discard a sidecar. Copy the platform and
every allocated module file through the provider backup API into a **new**
private recovery directory; include the relative inventory, versions, tokens,
lengths and SHA-256 hashes in a complete recovery manifest. Release scope only
after output validation. Preserve originals and partial copies on failure;
incomplete manifests are not backups.

Restore preferably into a fresh owned root in stopped recovery mode, validating
all members/hashes/markers/history and safe paths before selecting that root
for startup. This is an explicit recovery operation, not automatic live
relocation. A verifier rejects extra/missing modules, mixed snapshots, invalid
sidecars or unsupported versions. In-place replacement requires a fresh complete
backup of the exact current target plus explicit operator intent; never splice
an old registry into newer module files.

### Stopped PostgreSQL procedure

Stop host/maintenance, hold exclusive installation advisory lock, validate owned
database/roles/schema inventory and version/checkpoints, then take a complete
database custom-format dump including `kofte_system` and all assigned schemas,
markers and history. Record inventory/checkpoints and dump digest in the
recovery manifest; keep credentials/absolute paths out. Do not export just the
platform schema or imply that independently timed module dumps are coherent.
Restore to a fresh explicitly owned disposable target for proof, retaining the
source. Re-establish the documented role ownership/grants with offline authority;
verify restored tokens/history and runtime's lack of database CREATE before
using it. Existing harness database/OID/comment resource checks remain mandatory,
not bypassed because a dump has a matching installation UUID.

### Recovery limitations

Hashes/UUIDs detect inconsistency, not malicious operator forgery. A complete
older backup chosen explicitly cannot reveal a later lost snapshot without
external evidence; document the chosen recovery point/data-loss responsibility.
Mixed/stale replacement evidence must fail. Registry-only reset, live snapshots,
cross-provider conversion, automatic rename/adoption/deletion and global rollback
are not supported. Any ambiguous target stays preserved and non-ready.

## Proof obligations

For both real providers, interrupt before/after reservation, physical marker
commit, ready commit, registry upgrade and module migration/history commit.
Include SQLite pre-marker creation and abandoned EF lock, PostgreSQL lost
session, known busy/deadline/cancellation, denied provisioner and corrected retry.
Observe actual persisted state and outside-target sentinel hashes, not just a
mock return code. Two consumers must prove identical logical table/history names,
one-module upgrade, model-cache separation and rejection of foreign literal
migration schemas before any SQL. Execute stopped recovery and legacy refusal
examples; preserve zero-module host and existing contract/browser regressions.
