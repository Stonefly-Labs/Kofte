# One stage at a time with `drive-speckit`

The repository already has GitHub Spec Kit skills, scripts and templates. The
project-specific `drive-speckit` skill coordinates them without replacing upstream
assets or running a fleet. **Creating this driver does not execute product work.**

## Invocation and current progress

In Copilot, reload project skills with `/skills reload`, then ask:

```text
Use /drive-speckit to perform the next stage.
```

The constitution stage generated `.specify/memory/constitution.md` v1.0.0 from
the exact fenced prompt in `docs/constitution-prompt.md` and was human-merged.
The next stage, **001 / specify**, produced
[`spec.md`](../../specs/001-repository-bootstrap/spec.md) and its
[`checklists/requirements.md`](../../specs/001-repository-bootstrap/checklists/requirements.md)
using the exact fenced inventory prompt. Its ledger transition is a completed
checkpoint on the unfinished feature's branch. **001 / clarify** is also complete:
all ten ambiguity categories were clear at requirements level, with zero questions
asked or answered. The specification needed no changes, and its author checklist
was revalidated at **16/16 to 16/16**, with no newly passing items, regressions or
unchecked criteria. The ledger completion note records the category-to-requirement
evidence. No clarification hooks were configured. At clarification, exact toolchain
versions, orchestration, reference environment and route/schema details were
reserved for planning, not unanswered product questions. **001 / plan is now complete**:
[`plan.md`](../../specs/001-repository-bootstrap/plan.md),
[`research.md`](../../specs/001-repository-bootstrap/research.md),
[`data-model.md`](../../specs/001-repository-bootstrap/data-model.md),
[`contracts/`](../../specs/001-repository-bootstrap/contracts/) and
[`quickstart.md`](../../specs/001-repository-bootstrap/quickstart.md)
define supported baselines, native local host/web startup, platform-only storage,
minimal safe diagnostics, accessible status states and the required validation.
The pre-research and post-design constitution checks pass at design level;
no implementation behavior is claimed. No planning hooks were configured.
[Proposed ADR 0002](../adr/0002-bootstrap-development-baseline.md) records the
consequential choices. **Checklist** is next on
`feature/001-specify-repository-bootstrap` and the same
[draft PR #4](https://github.com/Stonefly-Labs/Kofte/pull/4), **without an intermediate merge**.

The human-directed [workflow correction](../adr/0001-feature-delivery-lifecycle.md)
removes per-step merge gates. Its constitution v2.0.0 amendment travels with this
feature; product principles and explicit human acceptance remain unchanged.

Spec 001 is broader than repository setup: it requires a future working API/UI
skeleton and test/local profiles. This specification stage creates no product
code or tasks and does not accept the feature. Clarification and planning likewise
add no product implementation or human approval. All quickstart product commands
are future implementation targets, not available commands or passing evidence.
The checked requirements
checklist records the specification author's review only; the later `quality.md`
is a separate human-owned review gate. The next invocation generates it unchecked
and blocks for reviewer completion unless the human explicitly requests assistance
with evaluation; planning does not pre-approve those checks.

Project-specific templates in `.specify/templates/overrides/` carry the constitution
into specification, planning and tasks through the installed resolver. They require
explicit applicability/traceability, pre/post-design checks and mandatory relevant
validation, rather than upstream generic optional-test examples. Upstream template
sources remain unchanged; amendment review must keep the project overrides aligned.

## Sequence and evidence

| Stage | Gate / durable output |
| --- | --- |
| constitution | Versioned constitution, resolved placeholders, required template consistency |
| specify | `spec.md`, `checklists/requirements.md` |
| clarify | Resolved ambiguity recorded in `spec.md`, or a no-ambiguity completion note |
| plan | `plan.md`, `research.md`, `quickstart.md`, plus applicable data model/contracts |
| checklist | Reviewed `checklists/quality.md`; no unchecked quality criteria before advancing |
| tasks | Numbered dependency-ordered `tasks.md` |
| analyze | Read-only cross-artifact review, saved as `analysis.md`; no unresolved blocking findings |
| implement | Completed tasks, updated docs, actual checks and `implementation.md` |
| converge | Append-only gap tasks if needed and `convergence.md`; no product edits |
| review | Requirement/acceptance evidence, exact checks and findings in `review.md` |
| accept | Explicit human decision in `acceptance.md` and recorded approval |

Analysis, convergence and review reports include `**Result:** pass`, `gaps` (converge)
or `changes` (review). Reports must substantiate that result. Both convergence gaps
and scoped review defects return the **next invocation** to implement, then converge
and review again. Design or scope disputes block instead of silently changing intent.

An invocation never performs the next stage as well. A stage that needs answers,
fixes or longer implementation can remain active across invocations; it does not
advance until its evidence and gates pass. Spec 024 is the MVP release gate;
025–038 require explicit authorization to proceed with post-MVP work.

## One feature, one branch, one PR

Start a numbered feature on one fresh typed branch from updated main. Keep that
branch and a single draft PR through specify, clarify, plan, checklist, tasks,
analyze, implement, converge, review and explicit acceptance. Each invocation
still executes only one stage, publishes artifacts and ledger together, and
pushes every commit immediately.

Completed stages are durable branch checkpoints, **not merge gates**. On the next
invocation, inspect the same branch/PR and begin the next recorded stage there.
Use `resume` only for a running/blocked stage, not one already complete. An open
draft PR is expected and does not block the next stage. Preserve existing branch
names even if they contain an earlier stage name.

Do not mark the feature PR ready or request merge until passing final review and
explicit human acceptance. Only the next numbered spec waits for this completed
feature PR to be human-merged with its accepted state on current `origin/main`.
The standalone constitution task likewise must be merged before feature 001.
A prematurely merged or closed-unmerged unfinished feature requires human
reconciliation, not a replacement stage PR.

The helper enforces recorded feature-branch continuity, but is **not** a GitHub
permissions or merge enforcement system; the skill owns Git/PR checks and actual
skill execution. Historical notes/hashes are not rewritten to hide the earlier
per-stage policy; ADR 0001 records the explicit correction.

Feature directories are immutable `specs/NNN-short-name/` paths. Set
`SPECIFY_FEATURE_DIRECTORY` explicitly on each downstream Spec Kit command.
Branch names do not select the feature. `.specify/feature.json` is only a local
ignored pointer, not the durable progress ledger.

## State helper

From repository root:

```bash
python3 .github/skills/drive-speckit/scripts/state.py status
python3 .github/skills/drive-speckit/scripts/state.py prompt
```

`status` is read-only. `prompt` emits only the current constitution/specify fenced
prompt. The driver, not the user, normally performs these mutations:

```bash
python3 .github/skills/drive-speckit/scripts/state.py begin \
  --branch chore/constitution
python3 .github/skills/drive-speckit/scripts/state.py complete \
  --branch chore/constitution --note "Versioned constitution and templates reviewed"
```

Only first specify adds `--directory specs/001-repository-bootstrap`. Mutations
require the actual current branch from `git branch --show-current`. The helper
does not itself create/switch branches, invoke skills, run tests, push or open PRs.

Each completed history entry records stage, feature, branch, timestamp, outcome,
decision note and SHA-256 hashes of required evidence. These hashes describe the
evidence **at that stage**, not a perpetual immutability rule: later clarification
and convergence legitimately change artifacts. Compare changes when reviewing;
never treat hashes as proof that requirements work.

Writes use a short exclusive lock and atomic replacement. Only one driver should
operate on a checkout. Never reset the ledger or edit it to skip a stage. Corrupt,
missing or contradictory state must be reconciled from Git and artifacts.

## Recovery and acceptance

On an interrupted run, inspect its recorded branch and partial artifacts before
resuming. Use `block --branch BRANCH --note "Specific blocker"` for failure, and
`resume --branch BRANCH` only after resolving it. A stale `.specify/drive-state.json.lock`
may be removed only after verifying no writer is active. Do not overwrite partial work.

After a merge conflict, preserve chronological history and reconcile active/next
stage with the actual merged artifacts; do not union two conflicting active runs.
Changed requirements invalidate affected downstream decisions: block, get an explicit
human-approved rework plan, and record its rationale rather than forging completed
history or continuing from stale analysis.

For acceptance, the driver presents the passing review and asks whether the human
accepts **this numbered feature**. It records the exact affirmative decision with
`complete --approval "Human decision"` and links the evidence in `acceptance.md`.
Silence, autopilot, "continue", task checkboxes or a previous stage merge are not
acceptance. Record that decision on the same feature branch, mark its existing PR
ready, and request human merge. Only that completed feature PR's human merge lets
the next numbered spec begin.
