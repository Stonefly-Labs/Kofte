# Contributing to Köfte

Welcome! **Spec 001's bootstrap is explicitly human-accepted** and
[PR #4](https://github.com/Stonefly-Labs/Kofte/pull/4) is human-merged.
The [acceptance record](specs/001-repository-bootstrap/acceptance.md) preserves
the decision and limits; the [review](specs/001-repository-bootstrap/review.md)
records source/test coverage and the complete local gate.
The local host/web, real-provider verification, guarded recovery and generated
contract do not introduce feature modules or prove production readiness.
Spec 002 now implements the [approved manifest/identity contract](docs/adr/0004-module-manifest-contract.md)
in `Kofte.Platform` with dedicated unit/public-consumer/schema tests. Follow the
[executable author guide](specs/002-platform-contracts/quickstart.md); no host,
module loading or database is needed after restore. The
[implementation report](specs/002-platform-contracts/implementation.md) records
actual evidence. The human-directed benchmark amendment allows measured available
workstations/CI runners, without fixed Ubuntu/CPU/RAM. T044 has actual local
evidence. The subsequent human-directed author-guide amendment replaces T045's
manual timed exercise with actual automated guide/correction/retry proof.
The original implementation checkpoint is complete.
[Convergence](specs/002-platform-contracts/convergence.md) found one HIGH partial
evidence gap: documented boundaries and repeated multi-diagnostic cases are
missing from the complete public-consumer corpus. Append-only T049 must close
that gap; implement is next on the same draft PR. No product defect or human
usability study, feature acceptance or permission to chain stages is inferred.
The author/quality checklists remain 16/16 and 36/36, and
[analysis](specs/002-platform-contracts/analysis.md) passes. Continue on the same
feature branch and draft PR #5; module runtimes remain future work.
See the [current local guide](docs/development/local-development.md).

## Prerequisites

- Git and [GitHub CLI](https://cli.github.com/), authenticated for this repository.
- Python **3.11+** for repository checks and the Spec Kit state helper (standard library only).
- For the runnable bootstrap: .NET SDK **10.0.402**, Node.js **24.22.0** and npm
  **11.19.1**, selected in the shell used to start or verify the stack.
- GitHub Copilot CLI or VS Code with Copilot for the optional assistant workflow.

See the [local guide](docs/development/local-development.md) for exact setup,
locked restores and commands. Docker Engine/Compose and the pinned PostgreSQL
image are needed for PostgreSQL verification, not ordinary SQLite startup.
Documentation-only edits do not require installing the product toolchain;
the optional MCP tooling has its own requirements in the assistant guide.

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

Also run the smallest relevant .NET/web tests, formatting, lint and type/build
checks in the [local guide](docs/development/local-development.md). Use its
fixture-owning named verification suites for runtime checks; focused results do
not replace the complete all-suite, CI and human gates. Do not replace failed
checks with a success claim.

### .NET test source ownership

Store .NET test source inside the .NET test project that compiles it, under
`tests/unit/` or `tests/integration/`. Production projects and their vertical
slices must not contain test classes. Test projects may mirror the tested
module/feature structure, but must not link test source from `src/` or a
production module. See [ADR 0003](docs/adr/0003-dotnet-test-source-ownership.md).

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
