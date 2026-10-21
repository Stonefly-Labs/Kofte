# Implementation Evidence: 001 - Repository Bootstrap

**Stage status:** In progress; dependency review and foundations implemented,
with initial local SQLite/host/web evidence. Full implementation is not complete.
**Attempt date:** 2026-10-20 (repository working date; ledger uses UTC).
**Original entry revision:** `c26756d88fba805481bf51567389be501a7d7f9f`.
**Branch / PR:** `feature/001-specify-repository-bootstrap`,
[draft PR #4](https://github.com/Stonefly-Labs/Kofte/pull/4).

## Entry Gates

The worktree was clean, HEAD matched the open draft PR, and updated `origin/main`
was `5e72fcc`. The ledger selected `001 / implement`, ready, with no active run.
The driver began that stage once on its recorded branch and explicit directory.
The preceding [analysis](analysis.md) passed; this attempt does not alter it.

The requirements checklists were inspected as read-only gates:

| Checklist | Total | Checked | Unchecked | Status |
| --- | --- | --- | --- | --- |
| requirements.md | 16 | 16 | 0 | PASS |
| quality.md | 36 | 36 | 0 | PASS |

These markers establish requirements-quality review, not implemented behavior,
ADR approval or final feature acceptance. At entry no checklist or task marker
changed; the subsequent human decision authorizes only T001's completion below.
`.specify/extensions.yml` is absent; no before/after implementation hooks are
configured.

## T001 Blocker and Actual Human Resolution

[T001](tasks.md) requires actual human disposition of proposed
[ADR 0002](../../docs/adr/0002-bootstrap-development-baseline.md) before affected
implementation. The choices presented for review were:

- Native .NET/Next.js processes owned by a Python supervisor rather than Aspire
  or full-stack Compose.
- No shipped application tables or empty EF migrations in this bootstrap.
- Supported macOS/Ubuntu targets, without claiming native Windows support.

At initial entry the ADR was **Proposed**, with no affirmative decision.
Inspection of PR #4 then found no comments or reviews containing a disposition.
A human-input request presented these choices and their separate dependency and
feature-acceptance gates, but the reviewer was unavailable. No decision was
received; autopilot and the driver invocation do not supply that approval.

The driver first recorded an actionable blocked reason instead of completing
the stage, published in checkpoint
`62baa76f05fc3b747f6a7f2cb7944c22706daca6`. The human then explicitly answered:
**"bro I approve adr 002"**. This is the actual ADR disposition, not inferred
approval. ADR 0002 is now **Accepted**, and T001 is checked with that evidence.

The driver resumed the same active implement stage on the recorded branch and
draft PR; its original start time and completed-stage history are preserved.
**At that approval checkpoint, T001 was complete and T002-T064 remained unchecked.** Dependency graph,
license/provenance adoption and product/manual evidence remain outstanding.
At that earlier checkpoint no product source, manifests, lockfiles, dependencies, tool installations,
runtime data or ignore-file changes were introduced. Approval of this ADR is not
feature acceptance and does not complete implementation or advance to converge.

## Commands and Evidence

| Command / inspection | Actual outcome |
| --- | --- |
| `git status --short --branch`, `git branch --show-current`, `git fetch origin main`, `git log -1 --oneline origin/main` | Passed; clean recorded feature branch, updated remote main inspected. |
| `gh pr list --head feature/001-specify-repository-bootstrap --state all --json number,state,isDraft,mergedAt,url,headRefOid` | Passed; PR #4 is open/draft, not merged; head matched the inspected revision. |
| `gh pr view 4 --json body,comments,reviews,headRefName,headRefOid` | Passed; no comments/reviews, and the body retains the outstanding T001 human gate. |
| `SPECIFY_FEATURE_DIRECTORY=specs/001-repository-bootstrap python3 .github/skills/drive-speckit/scripts/state.py begin --branch feature/001-specify-repository-bootstrap` | Passed; began only the selected implementation stage. |
| `SPECIFY_FEATURE_DIRECTORY=specs/001-repository-bootstrap .specify/scripts/bash/check-prerequisites.sh --json --require-tasks --include-tasks` | Passed; resolved the recorded absolute feature directory and research, data model, contracts, quickstart and tasks. |
| Driver `block --branch feature/001-specify-repository-bootstrap --note ...`, with `SPECIFY_FEATURE_DIRECTORY=specs/001-repository-bootstrap` | Passed initially; recorded implement/blocked pending the human decision. The later resume below clears that blocker. |
| `python3 scripts/check_repository.py` | Passed; repository assets, local documentation links, MCP parity and driver state are valid. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed; 20 tests. |
| `git diff --check` | Passed; no whitespace errors in the tracked checkpoint diff. |
| `SPECIFY_FEATURE_DIRECTORY=specs/001-repository-bootstrap python3 .github/skills/drive-speckit/scripts/state.py resume --branch feature/001-specify-repository-bootstrap` | Passed after the affirmative human ADR decision; cleared the blocker on the same active implementation run. |
| `SPECIFY_FEATURE_DIRECTORY=specs/001-repository-bootstrap python3 .github/skills/drive-speckit/scripts/state.py status` | Passed after approval; implement/running, empty blocker reason, unchanged original start time and no stage advance. |

At the earlier approval checkpoint product builds, lint/type checks, provider/contract/browser/privacy/recovery/timing and
manual acceptance checks were **not run**: no product implementation or approved
dependency adoption exists. The human ADR gate is now resolved.
No unavailable product check is counted as passing.

Preliminary T002 discovery confirmed the planned .NET SDK/runtime and Node/npm
releases in their official release metadata and inspected public npm/NuGet
candidate versions. This is not a resolved or approved direct/transitive graph.
Installed tools reported .NET SDK 10.0.203, Node 24.10.0 and npm 11.6.1, not the
planned baseline; Docker Engine 29.8.2 and Compose v5.5.0 responded. No tool was then
installed or changed. Exact graph/peer resolution, license/provenance/notices,
image/action pins and locked reproducibility were the next T002 work, before adoption.

## Current implementation checkpoint - 2026-10-21

This continues the original running implement stage after `d54c368`, not a
completed stage or feature. The [dependency review](../../docs/development/dependencies.md)
records the reviewed complete npm/NuGet graphs, actual signatures/integrity,
licenses/notices, compatibility corrections and pinned tool/image/action sources.
Only session-local pinned SDK/Node/browser tools were acquired; no global tool
or trust-store change was made. The required full inventory drift checker is
still future T031 work, not implied by T002's review.

Implemented source includes the locked host/test projects and npm workspaces,
validated configuration, sanitized correlated errors/custom telemetry, shared
generated contract/guards, SQLite explicit non-overwriting initialization and
existing-file probes, native private-path/regular-file/owner checks and operation
lease, minimal live/ready endpoints, fixed web adapter, honest accessible page
and bounded polling/retry/stale-state handling. The attached native supervisor
validates tools/config/ports, builds restored projects, checks actual readiness
and stops only owned child process groups.

Actual focused evidence on macOS arm64:

| Exact command / exercise | Outcome and limit |
| --- | --- |
| `python3 scripts/check_repository.py` | Passed; assets, links, MCP parity and running driver state. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed, 31 tests: 20 driver plus 11 supervisor configuration/preflight/port/failure/interrupt/ownership regressions, without installed product tools. |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore --verbosity minimal` | Passed, 23 tests; source physically belongs to the unit-test project, with no production-source includes. Trace tests exclude incoming request tracestate/baggage from custom telemetry. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --verbosity minimal` | Passed, four real SQLite/child-host tests: initialization/non-overwrite, opaque fixture reconnect, corruption/missing/lock/symlink refusal, live/ready/no-store/case/input/method/privacy and outage/recovery. |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror` | Passed, zero warnings/errors. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet` | Passed after the current C# changes. |
| `dotnet msbuild src/Kofte.Host/Kofte.Host.csproj -getItem:Compile`, `dotnet msbuild tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -getItem:Compile`, `dotnet msbuild tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -getItem:Compile` | Passed evaluated-source checks: 11 host, three unit and three integration compile items, all physically owned by their respective projects; no source links or test files in the host. |
| `dotnet restore Kofte.slnx --locked-mode --verbosity quiet` and `npm ci --quiet` | Passed exact locked restores. npm explicitly left two unapproved lifecycle scripts blocked; builds/tests still passed. |
| `npm run test:unit --workspace apps/web` | Passed, 23 tests across adapter, polling/cancellation/stale ordering and focused semantic/retry behavior. |
| `npm run test:unit --workspace packages/bootstrap-contract` | Passed, two strict status guard cases. |
| `npm run generate --workspace packages/bootstrap-contract` and `npm run contract:check` | Passed; approved baseline and formatted generated TypeScript agree. |
| `npm run typecheck --workspaces --if-present` | Passed for web, shared contract, HTTP contract and E2E workspaces. Explicit Node types are required for TS6 test-workspace checks. |
| `npm run lint --workspaces --if-present` | Passed for all four workspaces. Native ESLint flat configuration avoids the incompatible/vulnerable Next preset documented in dependency review. |
| `npm exec -- eslint scripts/check-contract.mjs eslint.config.mjs` and `npm run format:check` | Passed root-tool lint and formatting including the contract checker. |
| `npm run build --workspace apps/web` | Passed production build; `/` is static and the fixed readiness adapter is dynamic. |
| `npm run test --workspace tests/contract` with an owned running host and explicit isolated `KOFTE_HOST_URL` | Passed, six initial actual HTTP cases; not the complete future OpenAPI/both-provider contract suite. |
| `npm run test --workspace tests/e2e` with an owned host/web pair and explicit isolated addresses | Passed, five Chromium tests: real status/keyboard, axe/overflow at 320/768/1440, and a separately owned two-cycle built-runtime/fixture exercise. No serious/critical axe violations; no human WCAG acceptance claimed. |
| Two complete `python3 scripts/dev.py` cycles with a private initialized root, test-only opaque SQLite fixture and free ports | Passed after the current supervisor/configuration changes. Actual readiness in 2.424s and 2.098s on this non-reference macOS machine; both stops released the ports and preserved the exact fixture. This is not ten-start Ubuntu/reference/contributor evidence. |
| `npm run test --workspace tests/e2e -- startup.spec.ts` | Passed the shipped self-owning built-asset fixture test. Two cycles retained readiness 0.353/0.347s and page navigation/readiness 0.057/0.028s, exact private fixture matches and released ports. Safe JSON observations persisted beneath ignored `tests/e2e/test-results/`; not the Ubuntu/reference or unfamiliar-contributor gate. |
| `git diff --check` | Passed at the focused checkpoint; rerun before publication. |

The local exercise was orchestrated from an untracked session helper, not a
shipped backend fixture feature. Its fixture was created only by test code in
its owned temporary store. All owned processes stopped and private roots were
cleaned. The first browser attempt failed explicitly because Chromium was
missing; after explicit session-local acquisition all four cases passed.
Playwright's first two FFmpeg mirrors had DNS failures; its official fallback
succeeded. No missing dependency was counted as a pass.

A restart exercise exposed a false port-conflict check against TCP TIME_WAIT,
not an active listener. The preflight now matches the servers' address-reuse
semantics; a real recently-closed TCP regression passes while the actual occupied
listener still fails without takeover. The final exercise confirmed both fresh
startup and immediate restart. Startup reserves 15 seconds of the 300-second
attempt for bounded owned-group cleanup; interrupts/deadline signals are deferred
during cleanup rather than leaving children behind.

Configuration checks now prevent an explicit adapter origin from selecting a
different local port/address than the selected host. An omitted origin follows
the selected host port, including explicitly opted-in broad bindings. Host, web
and supervisor regression cases cover both derivation and mismatch refusal;
supervisor preflight also checks any separate loopback binding.

T001-T024 now have focused checkpoint evidence. The initial guide commands,
real SQLite/HTTP/browser cases and actual supervisor stop/restart exercise
operate on private roots/ports, while the shipped startup case makes the
built-asset fixture/timing exercise repeatable without the session helper.
The running implementation ledger has not advanced; T025-T064 remain open.

Outstanding work includes PostgreSQL and test ownership/recovery, guarded
backup/restore/reset, full Development OpenAPI/HTTP parity/privacy cases, named
verification/dependency enforcement/product CI and broader adversarial checks.
Human WCAG review, Ubuntu/reference timing, unfamiliar-contributor evidence and
final feature acceptance are still required. Initial focused results do not
complete those gates or justify advancing to converge.

## Acceptance and Constitutional Mapping

### Human-directed .NET test source correction

On 2026-10-21 the human explicitly required .NET test source to live with its
test project, not the production project it tests. [ADR 0003](../../docs/adr/0003-dotnet-test-source-ownership.md)
records the decision. Configuration, diagnostic and health unit-test sources
were moved to `tests/unit/Kofte.Host.Tests/Bootstrap/`; source links and host
test exclusions were removed. Integration-test source already lives in its
integration test project. The constitution's v2.0.1 clarification, canonical
architecture/inventory, project overrides, plan and future task paths now agree.
Frontend test placement is unchanged. Historical analysis and ledger entries
retain their original evidence rather than being rewritten; behavior and required
coverage are unchanged. This source correction does not finish implementation.

The original entry/approval checkpoint alone demonstrated no US1-US4 acceptance
scenario, FR-001-FR-017 implementation or SC-001-SC-006 runtime outcome. The previous
design/task coverage remains design evidence only. C:III/X/XII apply to the
human-approved T001 choices; C:XI requires honest unavailable evidence. The existing
product, storage, permission and MVP boundaries are unchanged.

The human decision resolves T001 without changing the design. The active stage
has already been resumed; continue the remaining tasks on this same branch and draft PR.
For a later interrupted invocation, inspect ownership and partial output before
using the same-run recovery command:

```bash
SPECIFY_FEATURE_DIRECTORY=specs/001-repository-bootstrap \
  python3 .github/skills/drive-speckit/scripts/state.py resume \
  --branch feature/001-specify-repository-bootstrap
```

Do not begin a second run, advance to converge, mark the PR ready or request a
merge. Dependency adoption, later manual evidence and final explicit human
feature acceptance remain separate gates.
