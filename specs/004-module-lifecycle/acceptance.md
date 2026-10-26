# Human acceptance: module composition and lifecycle

**Spec:** 004 | **Decision date:** 2026-10-25 (repository working date; 2026-10-26 UTC)
**Decision:** accepted
**Branch:** `feature/004-module-lifecycle`
**PR:** [#7](https://github.com/Stonefly-Labs/Kofte/pull/7)
**Passing review checkpoint:** `a6736679b5351014d488e10b472a80828cba4484`

## Explicit human decision

After the assistant reported that feature 004's final review passed with no
new implementation defects, linked the existing draft PR, retained the
undiagnosed PostgreSQL failure and failed local gate, and identified explicit
human acceptance as the next stage, the human replied:

> bro this human fugging accepts

This affirmative response accepts numbered feature **004** in the context of
its passing [review](review.md), [implementation evidence](implementation.md),
[convergence](convergence.md), approved scope and disclosed limitations.
The quotation is preserved exactly here and in the driver's
`complete --approval` argument. Acceptance is not inferred from silence,
autopilot, task markers, ADR approval, the earlier acceptance request or a merge.

## Accepted scope and evidence

Acceptance covers trusted compiled module discovery and private desired
configuration; deterministic whole-set identity/dependency/contribution/
configuration/storage preflight; native DI and ordered lifecycle barriers;
explicit stopped owner-local preparation with read-only normal serving;
data-preserving disable/re-enable; current own/dependency health; finite
supervised attempts and owned exit verification; and bounded private
correlated diagnostics with aggregate-only anonymous health.
Independent public author/host consumers and the executable guide prove
applicable behavior on both real providers without introducing future runtimes.

The review maps all 20 functional requirements, six outcomes, 20 acceptance
scenarios, twelve plan decisions, T001-T086 and twelve constitutional principles
to actual source and meaningful assertions. Review R1/T086 and prior
F1-F4/T081-T085 are closed. Both checklists are complete; no new implementation
defect, remediation task or within-scope design decision remains.
The review, all task bytes/markers and reviewer-owned checklists are unchanged.

Reviewed evidence retains 124 focused passing cases and independently
reconciled exact-fix CI: eleven contexts and 286 targeted product steps,
including 24 public guide commands and 24 strict observation checks.
On this acceptance invocation, all eleven actual PR contexts also pass at the
passing-review checkpoint above. These are existing results, not newly run
product checks. This decision/documentation stage adds no runtime measurement,
new local all-suite, human usability exercise or investigation.
Required repository checks accompany publication.

## Retained failed check and other limits

**Acceptance does not make the latest complete local attempt pass.**
It remains failed/interrupted at 109/111 executed steps across six reports,
all `completeGate: false`. The expressly human-deferred PostgreSQL case is
`PostgresCompositionAuthorityTests.KilledLaterMigrationRetainsEarlierCommitAndOriginalDataUntilExplicitAcceptedRecovery`.
Its original failing assertion and cause remain unavailable and undiagnosed.
No rerun, investigation, test suppression, assertion/deadline relaxation,
scheduling explanation or harmlessness conclusion is introduced by acceptance.

The [human-directed fact record](implementation.md#human-directed-deferral-and-implementation-handoff)
and passing review retain exact reports/correlation, the eight-pass/one-failure
snapshot across overlapping selections and revisit guidance. Narrower and CI
passes do not replace the failed full-local result. This limitation qualifies
FR-010/015/020, US3.3, SC-004/006 and principle XI; accepting feature 004 with
that disclosed limitation is not a general release waiver. If the case fails
again or relevant migration/recovery work begins, capture the exact failing
assertion and bounded safe phase evidence before attributing a cause.

In-process modules remain trusted code, not a hostile-code sandbox. Changes
to selection require stopped processes/restart, not hot unload or uninstall.
Storage ownership uses accepted immutable PostgreSQL schema/SQLite file
assignments and owner-local migrations, not cross-module ACID or global
rollback. Uncertain effects remain preserved and require explicit recovery.
Finite supervision does not promise control of arbitrary escaped descendants.

Actor/team authorization, product MCP/jobs/events/realtime, frontend extensions,
production deployment, the full reference module and release gates remain
later obligations. No anonymous lifecycle-management interface is added.
Acceptance does not claim production readiness or MVP completion and does not
waive remaining numbered-feature or release evidence.

## Delivery boundary

The decision, ledger and progress docs remain on the original feature branch
and PR. Explicit acceptance permits marking PR #7 ready and requesting its
**human merge**. It does not permit assistant merge, closure, auto-merge or
branch deletion.

Spec 005 may begin only in a later invocation after the human merge puts this
accepted ledger and evidence on updated `origin/main`. This invocation creates
no 005 branch, artifact or active stage.
