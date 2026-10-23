# Human acceptance: storage registry and module isolation

**Spec:** 003 | **Decision date:** 2026-10-22 (repository working date; 2026-10-23 UTC)
**Decision:** accepted
**Branch:** `feature/003-storage-registry`
**PR:** [#6](https://github.com/Stonefly-Labs/Kofte/pull/6)
**Passing review checkpoint:** `aba9b94a084ef86ea3dc83c1b87fa6433af20d3c`

## Explicit human decision

After the assistant reported that spec 003's final review passed with no
remaining findings, linked the existing draft PR and identified explicit
human acceptance of spec 003 as the next stage, the human replied:

> this human accesspts 003

This affirmative response explicitly accepts numbered feature **003**, in the
context of its passing [review](review.md), [implementation evidence](implementation.md),
[convergence](convergence.md) and approved scope. The quotation is preserved
exactly in this record and the driver's `complete --approval` argument.
Acceptance is not inferred from task markers, ADR approval, silence, autopilot,
a general continuation or a prior merge.

## Accepted scope and evidence

Acceptance covers the platform-owned durable registry and collision-safe
immutable module mappings; dedicated PostgreSQL module schemas and
schema-local history; separate private SQLite module files and platform file;
owner-bound EF models/migrations; explicit maintenance authority, provisioning,
upgrade and interrupted-operation recovery; finite supervised budgets;
correlated private diagnostics; coherent stopped complete-set recovery; and
executable public examples with retained bootstrap/manifest compatibility.
Normal serving remains module-free.

The passing review maps all 16 functional requirements, six success criteria,
18 acceptance scenarios, fourteen plan decisions, T001-T083 and twelve
constitutional principles to actual implementation and meaningful assertions.
F1/F2, R1/R2 and F3 are closed by T079-T083. Both checklists are complete;
no remediation task or unresolved within-scope design decision remains.
The review, tasks and reviewer-owned checklists are unchanged by acceptance.

Review records **157 freshly passing targeted named steps** across quality,
unit and both-provider integration/recovery/contract/privacy checks, plus
**200 passing assessed-head CI product steps**. Its publication checkpoint
also passed all eleven automatic PR contexts:
[Product checks 39887386097](https://github.com/Stonefly-Labs/Kofte/actions/runs/39887386097)
and [Repository checks 39887386071](https://github.com/Stonefly-Labs/Kofte/actions/runs/39887386071).
Both actual runs identify the passing-review checkpoint above.
These are existing reviewed results, not product checks newly run by
acceptance. This documentation/decision stage adds no runtime measurement,
new local all-suite, controlled mutation or human usability exercise.
Required repository checks accompany its publication.

## Retained limits

Assigned DDL remains deliberately narrow. In-process code is trusted, not
sandboxed; storage ownership is not actor/team/resource authorization or
protection from hostile code sharing privileged credentials/filesystem access.
Supported local filesystems and PostgreSQL authority assumptions remain.

Ambiguous ownership or migration effects stay preserved and blocked.
Recovery remains stopped, complete-set and fresh-target; selecting an older
coherent recovery point and its data-loss consequences is operator-owned.
Hashes/UUIDs establish consistency, not authenticity or knowledge of a newer
lost backup. No automatic adoption/deletion, live relocation, cross-provider
conversion, global rollback or cross-module ACID guarantee is introduced.
Historical deadline failures and unchanged passing reruns remain disclosed;
acceptance does not establish their root cause or eliminate timing sensitivity.

Module loading/lifecycle orchestration, actor/team authorization, product
MCP/jobs/events/SignalR, new frontend extensions, production deployment,
automated global backup and the packaged reference-module/release gates remain
later obligations. Accepting 003 does not claim production readiness or MVP
completion and does not waive those gates.

## Delivery boundary

The decision, ledger and progress docs remain on the original feature branch
and PR. Explicit acceptance permits marking PR #6 ready and requesting its
**human merge**. It does not permit the assistant to merge, close, enable
auto-merge or delete the branch.

Spec 004 may begin only in a later invocation after the human merge puts this
accepted ledger and evidence on updated `origin/main`. Acceptance does not
create a 004 branch, artifact or active stage.
