# Human acceptance: platform contracts and module identity

**Spec:** 002 | **Decision date:** 2026-10-21 (repository working date)
**Decision:** accepted
**Branch:** `feature/002-platform-contracts`
**PR:** [#5](https://github.com/Stonefly-Labs/Kofte/pull/5)
**Passing review checkpoint:** `709e69798ff60b26629dfaed5ad276a2e9b5e0e3`

## Explicit human decision

After the assistant reported that spec 002's final review passed with no
outstanding defect or remediation and identified explicit human acceptance of
spec 002 as the next stage, the human replied:

> spec is fully human approved

This affirmative response accepts numbered feature **002**, in the context of
its passing [review](review.md), [implementation evidence](implementation.md),
[convergence](convergence.md) and approved scope. It is not inferred from
checked tasks, ADR approval, silence, a general continuation or a prior merge.
The driver records this exact quotation through `complete --approval`.

## Accepted scope and evidence

Acceptance covers the minimal public platform contracts, strict bounded
manifest parser/validator and schema, canonical immutable module identity,
version/capability/dependency declarations, host compatibility and private,
deterministic actionable diagnostics. Permission and storage metadata remain
descriptive, not grants or physical selectors. The host remains module-free.

The passing review maps all 15 functional requirements, six amended success
criteria, 14 acceptance scenarios, twelve plan decisions, T001-T049 and twelve
constitutional principles to actual source/tests. Both checklists are complete;
no remediation or within-scope design decision remains. The complete public
corpus includes T049's boundaries, ordered multi-errors, phase precedence and
truncation, repeated 100 times per consumer per actual culture.

Review records 62 freshly passing targeted quality/unit/both-provider contract
steps and 177 passing assessed-head CI product steps. Its publication checkpoint
also passed all eleven GitHub contexts:
[Product checks](https://github.com/Stonefly-Labs/Kofte/actions/runs/39707328523)
and [Repository checks](https://github.com/Stonefly-Labs/Kofte/actions/runs/39707328515).
Those are existing reviewed results, not product checks newly run by acceptance.
Acceptance changes no product source, tests, tasks, design, dependency or review
evidence; its required repository checks accompany publication.

## Retained limits

The explicit ADR 0004 amendments remain authoritative: initialized timing uses
actual supported workstations/CI, not fixed Ubuntu/CPU/RAM or a universal
performance guarantee. Automated guide/correction/retry proof is not an
unfamiliar-human usability or elapsed-time study.

Two public consumers share the validator and independent expected outcomes;
they are not independent validation algorithms. Owned side-effect observations
and source inspection are not universal egress instrumentation or a hostile
plugin sandbox.

Registration uniqueness, dependency resolution, module storage allocation and
migrations, authorization runtimes, product MCP execution, UI extensions,
production readiness and MVP completion remain future obligations. Accepting
002 does not accept those capabilities or waive later release gates.

## Delivery boundary

All stages remain on the original branch and PR. This decision permits marking
PR #5 ready and requesting its **human merge**, not merging, closing, enabling
auto-merge or deleting the branch. Spec 003 may begin only in a later invocation
after that human merge puts accepted ledger/evidence on updated `origin/main`.
No spec 003 branch, artifact or active stage is created by acceptance.
