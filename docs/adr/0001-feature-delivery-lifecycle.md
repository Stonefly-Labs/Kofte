# 0001: One branch and PR for a complete numbered feature

**Status:** Accepted by explicit human workflow direction; constitutional amendment pending feature PR merge
**Date:** 2026-10-20
**Spec:** [001 - Repository bootstrap](../../specs/001-repository-bootstrap/spec.md)

## Context

The initial driver and constitution incorrectly required a new branch and human
merge after every Spec Kit step. That would split a single unfinished feature
across PRs and prompted a premature merge request after 001/specify.

The human corrected that policy explicitly:

> we stay on the same branch until it's finished, we don't merge after each speckit step

The correction concerns delivery lifecycle, not product direction, architecture,
MVP scope or permission to bypass requirements-quality and acceptance decisions.

## Decision

Each numbered spec uses one fresh main-based branch and one draft PR for its
entire specify-through-accept workflow. Each driver invocation still executes
exactly one logical stage, records evidence and commits/pushes to that branch.
The next stage continues there without merging or creating another PR.

After passing final review and explicit human feature acceptance, the assistant
marks the PR ready and requests human merge. Only that merge unlocks a new
numbered spec on a fresh branch from updated main. The standalone constitution
task keeps its own merge boundary before feature 001.

The state helper rejects a different branch for subsequent stages of an existing
feature. GitHub/merge checks remain the driver's responsibility. Existing
stage-named branches are retained, not renamed: feature 001 continues on
`feature/001-specify-repository-bootstrap` in
[PR #4](https://github.com/Stonefly-Labs/Kofte/pull/4).

## Alternatives

- Per-stage branches and merges: rejected by the human; a step is not a feature.
- An unattended full-roadmap run: not authorized; one stage per invocation and
  existing human clarification, quality-review and acceptance gates remain.

## Consequences

The driver, publishing skill, contributor/assistant instructions, project template
overrides and progress docs must agree on the feature lifecycle. Constitution
v2.0.0 removes the mandatory per-step merge gate; the major version follows its
amendment policy. No product principle or explicit acceptance requirement changes.

No ledger reset, schema change or history rewrite is needed. Earlier stage notes
and artifact hashes remain historical evidence under the old policy. Subsequent
stages use the same recorded feature branch. A ledger with conflicting historical
feature branches requires human reconciliation rather than silent reassignment.
Verification must cover same-branch continuation across reloads and remediation,
rejection of a different stage branch, and a fresh branch for the next spec.

This correction does not execute clarify or any other product stage. Reverting it
would require another explicit human workflow decision and aligned amendment,
not silently restoring the rejected merge gates.

## Approval

The quoted human instruction authorizes this workflow correction for the active
feature immediately. The documented constitutional amendment is carried in the
same [draft feature PR #4](https://github.com/Stonefly-Labs/Kofte/pull/4) and becomes
the shared baseline when that completed feature is human-merged. No feature
acceptance or merge approval was given by this correction.
