# 0005: Durable module storage ownership and offline provisioning

**Status:** Proposed
**Date:** 2026-10-21
**Spec:** [003 - Storage registry and module isolation](../../specs/003-storage-registry/spec.md)

## Context

Accepted 002 provides immutable canonical module identity and relational-storage
needs. Feature 003 must persist collision-safe assignments, isolate module
contexts/history on both providers and recover interrupted provisioning without
guessing ownership. Bootstrap PostgreSQL deliberately denies database CREATE
to the runtime role; existing SQLite recovery copies/resets only platform data.
Neither restriction may be silently weakened when module storage arrives.

## Decision

Propose allocator v1 `m_<24-character ASCII slug>_<32-hex SHA-256 prefix>`,
at most 59 bytes, plus database UNIQUE constraints, reserved/existing-object
checks and installation-wide exclusive maintenance. Persist key/version and
random installation/allocation identity permanently. Schema/file ownership
markers must agree with registry records; hashes alone never authorize reuse.

Use the existing `kofte_system` or separate SQLite platform file for registry
format 1. Modules get their own PostgreSQL schema or
`modules/<allocated-key>/module.db`, never `public`, shared files or `ATTACH`.
Reuse the existing lock: PostgreSQL exclusive advisory maintenance versus
shared host lease, SQLite stopped-host exclusive lease. Commit reservation,
physical ownership and ready state as explicit phases; no distributed ACID
promise. Ambiguous SQLite pre-marker files are preserved and blocked, not
automatically adopted/deleted.

Separate offline PostgreSQL schema-provisioning authority from normal runtime.
The provisioner can CREATE schemas and SET ROLE to the runtime owner; the host
keeps its existing no-database-CREATE/no-superuser/no-role-administration checks.
No elevated credential in serving-host DI, new remote administration surface
or relaxed disposable-target safety. SQLite keeps canonical private roots,
single regular-file ownership, safe sidecars and explicit DELETE/FULL baseline.

Keep `Kofte.Platform` provider-neutral. Add one shared infrastructure adapter
assembly for actual providers and minimal assigned EF integration, without a
module loader or general lifecycle runner. Module-owned provider-specific
contexts/migrations bind model, model-cache key, migration history **and**
operations/target models to the stored assignment. Preflight rejects foreign,
unbound/raw-SQL or unknown operations; model defaults/search paths alone are
insufficient. Test source/contexts/migrations remain inside dedicated projects.

Upgrade table-free bootstrap v0 explicitly and transactionally; no automatic
startup migration/down-migration/drop-recreate. Preserve zero-module v0/v1
host behavior. Legacy platform-only backup/restore/reset safely refuses
registry/module evidence. Document and actually test stopped complete recovery
sets, ownership/version/checkpoint/hash verification and fresh-target restoration.
Automated installation-wide backup orchestration and general module dependency/
lifecycle migration orchestration remain 022 and 004 respectively.

The [plan](../../specs/003-storage-registry/plan.md),
[research](../../specs/003-storage-registry/research.md),
[data model](../../specs/003-storage-registry/data-model.md) and
[contracts](../../specs/003-storage-registry/contracts/storage-v1.md) fix the
proposed behavior, finite budgets and recovery limits. No constitutional
amendment or product scope expansion is required.

## Alternatives

Runtime database CREATE or a retained superuser connection weakens privilege
separation. Hash-only allocation, automatic suffix/remapping and `IF NOT EXISTS`
adoption cannot prove ownership. One SQLite file, simulated schemas, platform
fallback or general cross-module transactions violate storage boundaries.
SQL text substitution, only `HasDefaultSchema`, or global `search_path` does
not reliably bind generated migrations. `EnsureCreated` is not migration proof.
A new broker/job engine, generic installer or automatic snapshot service
preempts later features. WAL adds recovery/checkpoint obligations without a
current throughput requirement; provider timeouts alone cannot cancel SQLite
synchronous work.

## Consequences

Offline provisioning means a running host correctly blocks registration/
upgrade; later lifecycle composition must invoke maintenance before serving.
One infrastructure assembly and a small assigned-migration binder add real
shared capability while retaining a pure manifest library. Versioned candidate
EF dependencies require actual graph/provenance/license/native-asset review and
locks before adoption; the plan is not that approval.

Registry plus separate files is not atomic. Some crash states intentionally
require explicit reconciliation, and ready resolution never repairs missing
data. Full-set stopped recovery, abandoned EF-lock handling, schema-bound
migrations, concurrency across processes, finite cancellation/deadlines and
safe output require both-provider executable proof. Files/schemas/shared
credentials do not sandbox malicious trusted code or privileged operators.
An explicitly chosen complete older recovery point cannot detect an unknown
newer lost backup; mixed/stale replacement evidence still must block.

## Approval

**Not yet approved.** Proposed on the existing
[draft PR #6](https://github.com/Stonefly-Labs/Kofte/pull/6).
Human approval is required before implementation adopts these choices.
Planning completion, later checked tasks or a passing automated check do not
constitute ADR approval, requirements-quality review, numbered-feature
acceptance or permission to merge.
