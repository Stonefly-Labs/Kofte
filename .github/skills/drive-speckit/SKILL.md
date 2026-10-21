---
name: drive-speckit
description: Advance Köfte's Spec Kit inventory by exactly one logical stage per invocation, beginning with the constitution and persisting progress through human review and acceptance. Use when asked to drive Spec Kit or advance the next roadmap step; do not run merely because the skill is being installed or edited.
---

# Drive one Spec Kit stage

You orchestrate the **installed local Spec Kit skills**, not a multi-agent workflow
engine. Read `.github/copilot-instructions.md`, `CONTRIBUTING.md` and
`docs/development/spec-workflow.md`. Do not assume that editing this skill is a
request to execute it.

## 1. Establish the Git gate before any stage writes

- Inspect the worktree, current branch, `origin/main`, and PR state using `git`/`gh`.
  Never disturb unrelated edits or carry them onto another branch.
- Read `.specify/drive-state.json` and run:

  ```bash
  python3 .github/skills/drive-speckit/scripts/state.py status
  ```

- If the ledger has **no active stage** and the current completed task/stage branch
  has an **open PR**, stop and request its human merge. Do not start the next stage
  even if its ledger entry is ready. An unfinished active stage may resume on its
  original branch and open/draft PR to resolve blockers; that is not a new stage.
- Before any new stage, inspect the latest completed history entry's branch PR
  with `gh pr list --head BRANCH --state all --json state,mergedAt,url,headRefOid`.
  A completed entry must be present on current `origin/main`, and its stage PR
  must be merged. An open, absent or closed-unmerged stage PR blocks progression.
  Reconcile rejected progress on a human-authorized branch; do not treat closure
  as acceptance. The initial empty history needs no previous-stage PR.
- For a new stage, create a **fresh typed branch from updated origin/main**.
  Use `chore/constitution` or `feature/NNN-STAGE-short-name` with a unique suffix
  if the name was previously used. The spec directory stays stable across stages.
- If the ledger has an active running/blocked stage, do not infer it succeeded.
  Resume only its recorded branch/open task, inspect partial output, resolve any
  reason, and use `resume --branch BRANCH`. Never create a second concurrent run.
  A dirty worktree from that same interrupted run is resumable only after verifying
  ownership. An unexpected dirty state blocks execution.
- If GitHub cannot be queried, stop with the error; do not assume a PR was merged.

## 2. Select intent and begin exactly once

Read canonical docs and the current constitution. The helper returns:

- `constitution` until its generation is complete. Extract the **exact** fenced
  text from `docs/constitution-prompt.md` with the `prompt` command.
- Otherwise, the **lowest unaccepted numbered inventory entry**, initially 001.
  For `specify`, `prompt` returns that entry's exact fenced prompt. Do not use the
  whole inventory, rewrite the prompt, renumber the feature or skip to a later one.
- Other stages come from recorded progress, not artifact existence. Existing files
  with no ledger entry require reconciliation with the human, not auto-completion.
  This includes an already populated constitution with no recorded generation;
  do not overwrite it simply because the ledger still says pending.
- `post-mvp-paused` stops after 024. Use `--include-post-mvp` only when the human
  explicitly authorizes post-MVP work; never silently expand the release boundary.

Before writes, record `begin --branch BRANCH`. Only the first specify stage adds
`--directory specs/NNN-short-name`. For downstream stages, set
`SPECIFY_FEATURE_DIRECTORY` to that **recorded** path on **each command**.
Do not derive feature context from branch names or trust a stale feature.json.

To invoke a stage, load the corresponding skill using the host's skill mechanism;
if unavailable, read its exact `.github/skills/speckit-STAGE/SKILL.md` and execute
the instructions. Pass the extracted prompt as that skill's user input, not a
literal `$ARGUMENTS`. Actually execute it; merely printing a slash command is not
execution. Inspect mandatory hooks before invocation and block any hook that would
merge/close PRs, publish secrets, run later stages or violate repository policy.

## 3. Execute the selected stage only

| Stage | Work and completion evidence |
| --- | --- |
| constitution | Execute `speckit-constitution` with the exact constitution prompt. Reconcile canonical docs; update its required templates. No unresolved placeholders; explicit version/dates/governance. Do not start 001 in this invocation. |
| specify | Execute `speckit-specify` with the exact inventory prompt and explicit directory. Produce `spec.md` and its `checklists/requirements.md`. Do not initialize the app. |
| clarify | Execute `speckit-clarify`; ask material questions with the host's user-input tool. Record answers in the spec. If no ambiguities exist, record that evidence in the completion note; do not manufacture questions. Unanswered questions block, not skip, the stage. |
| plan | Execute `speckit-plan`. Produce plan, research, quickstart and relevant data model/contracts. Validate constitution gates. No product implementation. |
| checklist | Execute `speckit-checklist` for relevant requirements quality (permissions, storage, MCP, UX and operability only where applicable); place the result in `checklists/quality.md`. Generation leaves items unchecked. Present it to the human reviewer; only assist with evaluation/marking when that reviewer explicitly requests it, as the upstream skill requires. Otherwise block for reviewer completion. Reviewer-owned checks are not implementation task completion; never tick blindly. |
| tasks | Execute `speckit-tasks`; dependency-ordered tasks must trace to approved intent and include meaningful validation. |
| analyze | Execute read-only `speckit-analyze`. Persist its report as `analysis.md` with `**Result:** pass` only when no unresolved critical/high or constitutional issues remain. If it finds blockers, record `block`, not success; remediation requires explicit review and re-analysis. |
| implement | Execute `speckit-implement`; honor its checklist gates. Complete the tasks, update affected docs, and run required checks. Record exact commands/results and acceptance mapping in `implementation.md`; do not claim unavailable checks passed. |
| converge | Execute `speckit-converge` after implementation. Do not edit product code here. Preserve its append-only task semantics; write `convergence.md` with `**Result:** gaps` or `**Result:** pass`. Gaps route the **next invocation** back to implement, not another action now. |
| review | Perform the `spec-review` agent's evidence-based rubric (directly or through the available custom agent for substantial separate review). Inspect actual behavior, checks, requirement/acceptance coverage, docs and scope. Write `review.md` with `**Result:** pass` or `**Result:** changes`, findings and evidence. Append scoped remediation tasks for implementation defects; design/scope disputes block for a human decision. Never self-accept. |
| accept | Present the passing review and remaining limitations and request **explicit human acceptance of this numbered spec**. Only an affirmative response authorizes `acceptance.md` and completion with `--approval` quoting that decision. Silence, autopilot mode, a general "continue", checked tasks and a prior merge do not constitute approval. Decline blocks; no response leaves acceptance incomplete. |

After any stage, inspect its actual output. Do not chain handoff suggestions into
the next stage, even if an upstream skill recommends doing so. Do not invoke
taskstoissues unless separately requested.

## 4. Persist, publish and stop

- `complete --branch BRANCH --note "Evidence and decision"` advances only the
  active stage. Use `--outcome gaps` for converge gaps, `--outcome changes` for
  review remediation, or `--approval "Exact affirmative human decision"` for accept.
- The helper checks artifacts, checklist/task gates, outcome markers and acceptance
  input, and stores hashes as **historical evidence**, not proof of behavioral
  correctness. You must verify semantics yourself.
- On failure use `block --branch BRANCH --note "Actionable reason"`; never mark
  complete, reset the ledger, overwrite partial artifacts or invent a fallback.
  Preserve safe partial work with a draft PR. A denied/cancelled acceptance must
  not be converted into a success.
- Include the ledger and stage artifacts in the same commit, update README/related
  docs if affected, push, and create/update the task PR. Follow the `ship-change`
  skill's publishing checks. Do not store the PR URL in the ledger by adding a
  second bookkeeping commit: query PRs by the recorded branch on the next run.
- A successful local transition is **proposed progress until human-merged**.
  A human merges the stage PR; the next invocation starts from main. Acceptance
  likewise takes effect for later specs only once its PR is merged.
- Handoff with stage, result, next stage/blocker and
  `Please merge [#N](PR_URL)` when ready. **Never merge, close, enable auto-merge
  or delete a PR branch. Stop after this one stage.**
