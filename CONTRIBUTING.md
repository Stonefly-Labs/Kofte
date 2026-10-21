# Contributing to Köfte

Welcome! This is currently **repository setup only**. Product implementation starts through the numbered Spec Kit workflow; this bootstrap does not complete spec 001.

## Prerequisites

- Git and [GitHub CLI](https://cli.github.com/), authenticated for this repository.
- Python **3.11+** for repository checks and the Spec Kit state helper (standard library only).
- A supported Node.js **22 LTS or newer supported LTS** with `npx` for local MCP servers.
- GitHub Copilot CLI or VS Code with Copilot for the optional assistant workflow.

.NET, container tooling, package-manager choices and runnable application commands belong to spec 001. Do not install a product toolchain just to edit this repository.

## Branch → change → commit → push → PR

Each new task or numbered feature starts on a **fresh branch from current `origin/main`**. Never work directly on `main`, base unrelated work on an unmerged feature branch, or reuse a merged/closed PR branch. All Spec Kit stages for an unfinished feature continue on that feature's original branch and one draft PR.

```bash
git status --short
git fetch origin
git switch main
git pull --ff-only origin main
git switch -c chore/short-description
```

Start only with a clean worktree; preserve unrelated changes and ask before moving them. Use `feature/`, `bugfix/`, `chore/`, `docs/`, `refactor/` or `test/` according to intent. For a numbered spec, use e.g. `feature/001-repository-bootstrap`; the standalone constitution task uses `chore/constitution`. Existing unfinished feature branches keep their names even if they include a stage.

An explicitly user-provided current task branch can be used after checking its base and PR state. Resume an open task only on its original branch, not a different fresh task branch.

Make a focused change, update **README.md and related docs when affected**, and run:

```bash
python3 scripts/check_repository.py
python3 -m unittest discover -s scripts/tests -v
git diff --check
```

CI fetches full history and checks the actual PR-base-to-head diff (or the push
range), rather than treating a shallow merge checkout as an entirely new tree.

Once product tools exist, also run the smallest relevant tests, formatting, lint and type/build checks required by that feature. Do not replace failed checks with a success claim.

Use an intent-based commit such as `chore: bootstrap repository workflow`. Assistant-authored commits include:

```text
Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

**Push after every commit**, including follow-up fixes:

```bash
git push -u origin HEAD
gh pr create --base main --title "chore: bootstrap repository workflow" --body-file /path/to/pr-body.md
```

Use the [PR template](.github/pull_request_template.md). Check for an existing open PR for the branch before creating one; update it for follow-up commits. Put temporary PR bodies outside the repository.

**Never merge, enable auto-merge, or close a PR as an assistant.** Do not delete its branch. A human owns disposition, acceptance and merge. Request a merge with a clickable link, `Please merge [#N](PR_URL)`, only when the task or entire feature is complete. An unfinished Spec Kit feature stays in one draft PR; report its next stage or blocker instead of requesting merge.

## Spec-driven changes

Read the [canonical docs](docs/README.md), then use [drive-speckit](.github/skills/drive-speckit/SKILL.md). It advances **one stage per invocation on the same feature branch and PR**, without merging between steps. Do not run the complete Spec Kit workflow as an unattended batch.

Feature artifacts live at **`specs/NNN-short-name/`**, independent of branch naming. The tracked driver ledger is `.specify/drive-state.json`; `.specify/feature.json` remains an ignored local pointer.

Acceptance requires an explicit human decision after a passing review; neither all checked tasks nor an assistant review alone makes a feature accepted. Only then is the feature PR ready for human merge. The next numbered spec starts from updated main after that merge.

## Scope and decisions

Preserve Köfte in human-facing text; use `kofte` in technical identifiers. Keep the host small, use module-local vertical slices, and preserve the storage and authorization invariants in the architecture docs. Record consequential choices with the [ADR template](docs/adr/README.md).

Report bugs with reproduction steps and propose features with actors, outcomes, roadmap placement and acceptance criteria. Do not put secrets, private customer data or exploitable vulnerability details in public issues.
