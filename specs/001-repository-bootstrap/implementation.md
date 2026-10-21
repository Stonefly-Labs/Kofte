# Implementation Evidence: 001 - Repository Bootstrap

**Stage status:** Blocked on required human evidence after all ten local automated
groups passed. Real-provider recovery, generated contracts, controlled browser/
privacy checks and named verification are implemented. Human accessibility/
reference-contributor and final target/CI gates remain; full implementation is
not complete.
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

## Published foundation checkpoint - 2026-10-21 (`2f4c9e0`)

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

## Recovery and verification increment - 2026-10-21

This continues the **same implement stage**, rather than treating T001 or the
published T024 checkpoint as completion. Measurements below were made on the
intended working tree after `2f4c9e0`; that base revision appears in the local
reports and is not claimed to identify the unpublished source snapshot.
Actual environment: macOS **26.5 (25F71), arm64**, 18 logical CPUs, 64 GiB RAM,
Python 3.15.0, session-local .NET SDK 10.0.402/runtime 10.0.13,
Node 24.22.0/npm 11.19.1, Docker 29.8.2/Compose 5.5.0 and pinned Chromium.
This is supported macOS evidence, **not** the Ubuntu 4-core/8-GiB/SSD reference.

### Implemented boundaries and actual regressions

- Real PostgreSQL 17 fixtures use the immutable reviewed image, explicit
  disposable opt-in, local Docker context, unique recorded resources and newly
  created database/role OIDs and ownership comments. Missing proof refuses
  cleanup. The restricted app role and `kofte_system` round-trip/reconnect exact
  opaque fixtures; wrong credentials, revoked schema privileges and actual
  service pause yield unready without a SQLite fallback, while liveness remains
  available. Host restart/fixture cleanup resume only the owned service.
- SQLite backup uses its provider backup API, private verified backup/metadata
  pairs and stopped-host leases. New-root restore and separately confirmed
  replacement with a fresh destination backup preserve exact fixtures.
  Unconfirmed reset/replacement, stale backup, corrupt metadata/archive, active
  lease, zero/truncated file, hardlinks/FIFO, traversal/symlinks and restricted
  non-root file/directory permissions fail without changing outside data.
  No application schema version, empty migration or automatic repair is invented.
- Matching-image PG dump/restore uses an exclusive advisory operation lease and
  connection admission guard. A running host/concurrent operation, nonempty
  target, incompatible major metadata or corrupt archive is refused; restore
  succeeds only in a newly owned empty schema. This is test-only infrastructure,
  not a production database command or mutation endpoint.
- Native Development OpenAPI is generated from endpoint/type metadata and fully
  compared against the baseline, normalizing only the verified owned listener
  and omitted/empty root security. Production/Staging documentation is absent
  for GET/POST/HEAD on both actual providers. Real HTTP/adapter tests enforce
  exact schemas, anonymous applicability, no-store, 200/503/empty 400/404/405,
  actual GET bodies and no credential reflection/admin/test/MCP routes.
- A failing fake-clock regression exposed a polling cadence bug: a manual
  attempt overlapping the old fixed interval could defer the next start to
  39 seconds. Refresh now schedules 30 seconds from each actual attempt start.
  Absolute 10-second body deadlines, cancellation, late results, stale/refocus
  behavior and nonduplicated live announcements have focused regressions.
- Owned runtime controls and capture are test-only. Both-provider browser
  outages/restarts recover without reset; hung/malformed requests cannot retain
  stale healthy display. Privacy scans cover caller/configuration canaries,
  HTTP/UI/browser console and host/Next/supervisor output, with raw buffers
  transient and capture/control/scanner failures propagated. All-state axe,
  keyboard/focus, reduced-motion and 200% text zoom pass at 320/768/1440 widths;
  this does not constitute a human WCAG review.
- Dependency enforcement found a real candidate-versus-actual graph gap:
  15 Microsoft.Extensions transitive identities resolved at 10.0.0 rather than
  reviewed 10.0.13. Explicit central transitive pins and regenerated locks now
  use the already reviewed versions. Two existing British-spelled `LICENCE`
  notices were reconciled against pinned archives. The checker passes
  **403 npm / 69 NuGet identities**, including provenance/edges/notices and
  image/action pins; this is not a newly claimed signature or vulnerability audit.

All .NET source added here belongs to its dedicated unit/integration project;
no production vertical slice contains or links test source. The shipped host
still has no fixture table, domain module, fake identity, fault/admin route,
module SDK or speculative EF model.

### Named targeted evidence

Every command below returned zero with all named child outcomes passed.
Reports are ignored local artifacts; their identifiers are evidence references,
not Markdown links to committed files. Targeted reports correctly retain
`completeGate: false`.

| Exact command | Observed result / report beneath `artifacts/verification/` |
| --- | --- |
| `python3 scripts/verify.py --suite quality` | Passed every quickstart child in 18.840s; `quality-none-cacce89ce5e04a6c84820105c483712d.json` |
| `python3 scripts/verify.py --suite unit` | Passed native/web/shared-contract/Python units; `unit-none-c8ddad79a0564efc841d78cac8c705da.json` |
| `python3 scripts/verify.py --suite integration --provider both --disposable-postgres` | Both real-provider groups passed; `integration-sqlite-ae696463a7654a85a0edaa982cfd8b95.json`, `integration-postgres-89fd54c912a342a88d1ad87bf621eba3.json` |
| `python3 scripts/verify.py --suite recovery --provider both --disposable-postgres` | Native and actual browser/store/host recovery, capture and cleanup all passed in 119.600s; `recovery-both-58b8cbc91fb945c8aae5009514386d44.json` |
| `python3 scripts/verify.py --suite contract --provider both --disposable-postgres` | Actual host/web HTTP/schema/input/503 parity passed in 21.676s; `contract-both-f2f40dae5e0543499367492bd34ce3f8.json`. Later policy selector correction exercises non-Development tests per provider rather than allowing PG cases into SQLite-only checks |
| `python3 scripts/verify.py --suite browser` | Actual status/recovery plus three-width all-state axe/focus/zoom passed in 46.994s; `browser-none-e16a1f91af3f4f1ea7d0a051f4f49a45.json` |
| `python3 scripts/verify.py --suite privacy --provider both --disposable-postgres` | Both-provider HTTP/browser/log scanning and real supervisor failure capture passed in 19.033s; `privacy-both-7b30131349724990a16e9311af2364f7.json` |
| `python3 scripts/verify.py --suite startup --warm-starts 10` | Ten actual private-fixture warm cycles passed in 7.761s; `startup-none-31d3072b84c7439b9e3a43a7e1677c64.json`. This earlier run recorded samples but preceded explicit per-sample 2s enforcement/CI report retention; final rerun is required below |
| `python3 scripts/verify.py --suite contract --provider sqlite` | Passed without disposable opt-in after provider/class selector intersection was corrected; no PostgreSQL required |
| `python3 scripts/check_repository.py`; `python3 -m unittest discover -s scripts/tests -v`; `git diff --check` | Passed directly after the targeted increment; 87 Python tests at that point |

Locked .NET restore and `npm ci` ran through quality; before/after SHA-1
comparisons of all four dependency lockfiles were identical:
`8caa24c59ac38ce7aecbe353d5dd7bf1945e17b7` (npm),
`dd0326782cb06592fd4f3d3e0ab254346b55b7f5` (host),
`82c36a9723a214eafebd395f10c6eb044cdca8d4` (unit),
`014ce868fc3ce8556d591d4bf811c6554b96c7e7` (integration).
These are drift comparisons, not cryptographic provenance/signature claims.

The T032 harness previously demonstrated all five actual controlled failures in
98.8s (`failure-propagation-none-20e4f5f0126d4e1cb9df9a80f0a816a8.json`).
A later attempt correctly failed its initially unformatted baseline instead
of laundering that as successful failure propagation. Final formatted-source
failure/all runs and actual implementation-revision GitHub results must be
recorded separately; earlier green evidence is not silently substituted.

### Acceptance coverage and remaining gates

| Scope | Actual coverage / limit |
| --- | --- |
| US1.1-4; FR-001-004/007/009/016/017 | Real native host/web supervisor, validated configuration, persistence/restart and port/tool ownership failures; original guide exercise plus repeatable warm fixture cycles. Reference timing/unfamiliar-contributor review still required |
| US2.1-4; FR-012-014 | Named fail-closed runners, locked restores, full dependency review enforcement and isolated controlled failures; workflow invokes those same commands. Targeted local results are not final local/CI acceptance |
| US3.1-4; FR-003-005/007/009/010/015/016 | Both actual providers, live-vs-ready outage, guarded backup/restore/reset, cancellation/deadlines, browser retry and complete transient privacy capture; no provider fallback or recreation |
| US4.1-3; FR-004-006/008/010/011 | Generated full-contract equality, exact public access/input policy, actual 503 adapter parity, responsive all-state axe/keyboard/focus/announcement regressions. T056 human review remains open |
| SC-001/002 | Reproducible private-fixture startup/navigation samples on actual macOS; no Ubuntu reference or unfamiliar-contributor pass inferred |
| SC-003/004/006 | Actual named failure propagation, provider/browser recovery and privacy/data-containment evidence; final unchanged-source all/CI gate still applies |
| SC-005 | Automated accessibility evidence only; no human acceptance or complete WCAG claim |
| Constitution I-XII | Minimal module-free scope/direct handlers; reviewed exact dependencies; provider-local ownership/leases and no cross-module transaction claim; no fake authorization/MCP bypass; accessible honest shell; safe observability/recovery; truthful evidence and one original draft feature PR |

T056 requires a real human's running-page keyboard/focus/announcements,
contrast/zoom/reflow/readability and applicable WCAG 2.2 AA review. T059 requires
the stated Ubuntu reference plus an actual unfamiliar contributor following the
guide. These cannot be supplied by agent inspection, axe, macOS timing or an
ordinary differently sized CI runner. T058 additionally needs actual Ubuntu
target results. Their dependent T060-T064 and the implement ledger transition
remain incomplete until the mandatory evidence exists. No converge, final
review, feature acceptance or merge is authorized by this checkpoint.

### Stable-source local automated all invocation

`python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10`
**passed, exit 0**, after the provider-specific native OpenAPI selector,
per-sample startup thresholds and safe CI sample retention corrections.
All ten required groups ran their actual child commands; none was skipped or
reported as an unavailable-provider pass. The following per-group reports have
`scope: all`, `completeGate: true` and `outcome: passed`; the CLI success plus
**all ten outcomes**, not one isolated flag/report, establish this automated run.
It does not satisfy the separately required human/reference gates or T061's
still-unmet human prerequisites.

| Group | Seconds | Report beneath `artifacts/verification/` |
| --- | --- | --- |
| quality | 17.914 | `quality-none-9882f8d797a44f9b8260b62a54b1b7bb.json` |
| unit | 3.680 | `unit-none-5676c844d5b149f6a8083bd94ea112e5.json` |
| integration SQLite | 1.822 | `integration-sqlite-4245c79941a942b6b0795474fa6304a7.json` |
| integration PostgreSQL | 25.802 | `integration-postgres-1ab901fc9a2045b587708be8ec33af6b.json` |
| recovery both | 120.925 | `recovery-both-d4307ee2554a490188c6a64839db1b93.json` |
| contract both | 31.356 | `contract-both-c77bcebae6034580a77b4fa9a6c4d029.json` |
| browser | 47.013 | `browser-none-9438c62974154bf1befca8562f24c177.json` |
| privacy both | 20.037 | `privacy-both-fba0dada069d44959df3e2d58389bb27.json` |
| startup ten cycles | 6.810 | `startup-none-c550694e7af9422389243452a9f3d896.json` |
| failure propagation | 145.731 | `failure-propagation-none-1bb38ba4f0b544ba8e4dfbc11baf518d.json` |

Quality includes every exact quickstart child, including locked .NET/npm
restores, format/analyzers/build, all mandatory workspace lint/type/build,
generation drift and full dependency enforcement. Native/web/shared-contract
units, both providers' native integration and Production/Staging policies,
real outage/host restart, privacy scanning and zero serious/critical axe results
all passed. Failure propagation first passed its real baselines, then observed
the expected named nonzero test/formatter/build/missing-PG/missing-Chromium
commands in its separate checkout. This supersedes the earlier failed-baseline
attempt without erasing it.

Startup's uploaded report now includes a validated `startup-observations` step
with all ten exact safe samples; no fixture payload/path/credential is retained.
Rounded below to six decimal places for readability:

| Cycle | Ready seconds | Usable-page seconds | Exact fixture match |
| --- | --- | --- | --- |
| 1 | 0.360152 | 0.055045 | true |
| 2 | 0.356393 | 0.026019 | true |
| 3 | 0.353572 | 0.023400 | true |
| 4 | 0.350248 | 0.023373 | true |
| 5 | 0.353557 | 0.022698 | true |
| 6 | 0.350491 | 0.022575 | true |
| 7 | 0.350081 | 0.023269 | true |
| 8 | 0.347935 | 0.023443 | true |
| 9 | 0.368297 | 0.024999 | true |
| 10 | 0.364911 | 0.023741 | true |

These are actual built-asset macOS samples, **not** the Ubuntu reference,
unfamiliar-contributor or fresh acquisition/build timing gate. The independent
repository commands were rerun directly after documentation updates:
`python3 scripts/check_repository.py` passed;
`python3 -m unittest discover -s scripts/tests -v` passed **93 tests**;
`git diff --check` passed. Explicit ignore checks cover `.env`, local database,
backups, verification artifacts and browser results.

T001-T055 and T057 are now checked. T056, T058-T064 stay unchecked.
Actual implementation-revision GitHub CI has not yet run at this local checkpoint;
it must be inspected after publication, not inferred from the workflow file.
The human-input request for actual T056/T059 observations returned unavailable;
no review, reference/contributor pass or acceptance was fabricated. The driver
therefore records the same original implement run as blocked, preserving its
start time and draft PR. Independent publication/CI work continues; a future
resume requires the missing evidence, not another begin or replacement branch.

### First implementation-revision CI inspection

Published/pushed `a222c90894b039fc3831f206a3a1d96c1c5c4f1b` immediately after
commit, retaining draft PR #4. Actual
[repository run 39629016793](https://github.com/Stonefly-Labs/Kofte/actions/runs/39629016793)
**passed**. Actual
[product run 39629017094](https://github.com/Stonefly-Labs/Kofte/actions/runs/39629017094)
**failed**, rather than being inferred green from local results:

| Job | Actual outcome |
| --- | --- |
| unit, integration-sqlite, integration-postgres, recovery, contract, privacy, startup | Passed on Ubuntu 24.04 x64 runners; actual reports record Linux x86_64, four logical CPUs and Python 3.12.3 |
| quality | Failed only `dependency-review` / `npm run dependencies:check`; all preceding quality children passed |
| failure-propagation | Correctly failed its non-green quality baseline at the same dependency check; not a controlled-failure pass |
| browser | Failed its browser test command; owned control/process/store restoration, output scan and cleanup passed |

Safe verification JSON was inspected from every uploaded artifact. Raw runtime
logs/screenshots were not uploaded or added to this repository. HTTP/2 log
retrieval initially failed with a transport cancellation; HTTP/1 retry succeeded.
CI fixes remain implementation work: do not waive the dependency gate or
increase/skip browser acceptance to manufacture green. Actual RAM reporting and
safe source-location-only test diagnostics are being completed for target
evidence without disclosing raw exceptions or configuration.

The Linux dependency failure was reproduced with a Linux/x64/glibc npm install
in an isolated Linux helper (the helper itself is arm64, not a native x64 runtime
test). Three existing platform packages lacked retained LICENSE hashes, and a
cold cache lacked the immutable oxide WASM parent proving bundled identities.
Their already reviewed archive integrities/license decisions were preserved;
three verified notice hashes were added, and explicit pinned evidence-cache
preparation now precedes strict validation. The isolated checker then passed
(`dependency-linux-quality-none-42e949fc124441debd8ecf8f7672fcb0.json`), while
the original-omission diagnostic retains `notice_unreviewed`
(`dependency-linux-quality-none-69f97f7b49f146efb4396bf37cd6d33f.json`).
No provider/browser acceptance or Ubuntu-native runtime result is inferred from
that dependency-only helper.

Safe failure reports now retain only allowlisted checker codes and whitelisted
browser source basename/line/column tuples; no raw errors, expected/received
values, private path prefixes or screenshots. Physical RAM is measured
explicitly for later target evidence. Focused Python coverage now passes
**99 tests**. The narrow text-zoom regression additionally checks both declared
generic platform font fallbacks; all three controlled-state viewport cases
passed locally. The actual Linux browser assertion remains to be identified
from the next safe diagnostic run, not guessed or waived.

Follow-up `7c3b399` was committed/pushed immediately. Its
[repository checks passed](https://github.com/Stonefly-Labs/Kofte/actions/runs/39632825522);
[product run 39632825423](https://github.com/Stonefly-Labs/Kofte/actions/runs/39632825423)
still failed quality/failure-baseline/browser while the other seven jobs passed.
The new safe evidence narrows dependency review to
`dependency_evidence_missing_or_invalid` and the browser failure to
`accessibility.spec.ts:86:11`: the 320px/200%-text reflow assertion. The
source-location diagnostic exposed that real cross-platform failure without
persisting its raw assertion/stack.

Bootstrap text now allows emergency wrapping, and the retry button has zero
intrinsic minimum width and a 100% maximum width, preserving content and text
scaling rather than hiding overflow or weakening the assertion. Local
`python3 scripts/verify.py --suite browser`, web lint/typecheck and whole
format checks pass after this correction. Linux CI must still prove it.
Actual follow-up CI hardware reports four logical CPUs and roughly 16.77
billion physical memory bytes, **not** the eight-GiB reference; T059 remains
unproved regardless of startup results.

The remaining dependency failure was reproduced on fresh, case-sensitive
Docker storage in an isolated Ubuntu x64 SDK helper: NuGet installs
`microsoft.applicationinsights.nuspec`, but the archive calls it
`Microsoft.ApplicationInsights.nuspec`. Reading the archive's case from the
installed directory failed on Linux; the earlier macOS-backed bind mount masked
it. The checker now uses NuGet's canonical lowercase installed filename while
retaining exact installed/archive metadata comparison. Missing or nonregular
metadata fails explicitly; tampering remains rejected. No dependency, license,
inventory decision or integrity requirement changed.

The new case-sensitive regression failed before the correction and passes
afterward. Parent verification now passes **100 Python tests**, the repository
checker, `git diff --check` and every child of
`python3 scripts/verify.py --suite quality`. The helper's actual
`dotnet restore Kofte.slnx --locked-mode`, `npm ci` and
`npm run dependencies:check` all passed with fresh native helper caches,
reconciling 403 npm / 69 NuGet identities. Safe targeted report
`dependency-diagnostic-none-f9a72c0347b34d22b402787d4f9282e1.json` records the
base `7c3b399` plus checker overlay SHA-256
`643d03da29cad7bd85a1bd988c1b05c15e618b87b9ebb5ac0e86c3445f96156a`.
This is dependency-only Docker-helper evidence, **not** full quality,
native Ubuntu CI, reference-hardware or feature-acceptance evidence.

### Corrected implementation-revision CI and supported targets

Published/pushed `6cab683e585814c560bf04378bc1465564d79479` immediately.
[Repository checks 39634960843](https://github.com/Stonefly-Labs/Kofte/actions/runs/39634960843)
and [product checks 39634960839](https://github.com/Stonefly-Labs/Kofte/actions/runs/39634960839)
**passed**. All ten product jobs completed successfully; all ten uploaded safe
JSON reports and their child outcomes were inspected. The actual Ubuntu
dependency review, full browser including the formerly failing 200%-text
assertion, and controlled-failure baselines now pass. Earlier failures above
remain historical evidence, not erased or waived.

The run's PR head is `6cab683`; reports correctly identify the tested GitHub
PR merge snapshot `6aae5a5d0f12f15e2dd5a9420cf5d97e6d8940a8`. GitHub's commit
API confirms its two parents are main
`5e72fcc61724db6b2f68c53987ccb64f3debad72` and the exact published head
`6cab683e585814c560bf04378bc1465564d79479`; no head-only checkout is invented.
Each job's runner label was verified as `ubuntu-24.04`. Actual reports record
Linux x86_64, four logical CPUs, physical RAM
**16,759,308,288-16,770,748,416 bytes**, Python 3.12.3, .NET SDK 10.0.402,
Node 24.22.0 and npm 11.19.1. Kernels were `6.17.0-1022-azure` or
`7.0.0-1017-azure`. Hosted-runner labels do not introduce an Azure deployment
dependency.

The jobs invoke the named local verification suites, not CI-only substitutes.
Durations below are measured suite durations, excluding tool setup:

| Job / suite | Actual result | Seconds |
| --- | --- | --- |
| quality | All quality children, locked restores and dependency enforcement passed | 56.009 |
| unit | Native/web/shared-contract/Python tests passed | 15.860 |
| integration-sqlite | Real SQLite checks passed | 15.768 |
| integration-postgres | Real owned PG17 checks passed | 21.523 |
| recovery | Both-provider guarded recovery and real browser/store/host journeys passed | 147.880 |
| contract | Both-provider runtime generation, policy and HTTP/adapter parity passed | 48.589 |
| browser | Status/recovery, all-state axe/keyboard/focus and narrow enlarged-text reflow passed | 67.160 |
| privacy | Both-provider actual runtime canary scans passed | 40.910 |
| startup | Ten actual warm private-fixture cycles passed | 34.747 |
| failure-propagation | Green baselines followed by the expected actual controlled failures passed | 174.231 |

Every uploaded report remains `scope: targeted`, `completeGate: false`.
Together they establish the ten-job Ubuntu result, not a fabricated local
`--suite all` invocation or satisfaction of unavailable human prerequisites.
The startup report retains all ten validated observations, rounded here to
six decimal places; every cycle preserved its exact private fixture:

| Cycle | Ready seconds | Usable-page seconds |
| --- | --- | --- |
| 1 | 1.420130 | 0.187577 |
| 2 | 0.952114 | 0.084909 |
| 3 | 0.969674 | 0.075012 |
| 4 | 0.963160 | 0.100484 |
| 5 | 0.974997 | 0.078195 |
| 6 | 0.958160 | 0.073383 |
| 7 | 0.969143 | 0.079776 |
| 8 | 0.983076 | 0.074961 |
| 9 | 0.964390 | 0.072711 |
| 10 | 0.970944 | 0.076394 |

Together with the actual supported macOS arm64 all-suite evidence and the
corrected source's local quality/browser/100-Python-test passes, this satisfies
**T058**. Local locked restores left tracked manifests/locks unchanged and the
published worktree clean. No native Windows/Kubernetes result is claimed.
This hardware is **not** the required eight-GiB timing reference, and no
unfamiliar contributor or human WCAG review was supplied. T056, T059 and
dependent T060-T064 therefore remain unchecked. The same implement run stays
blocked solely on genuine human/reference evidence and its dependent final
gates; no converge, final review, feature acceptance or merge transition occurs.

### Human-confirmed manual task checkpoint - 2026-10-21

Reporting human **@christopherhouse** confirmed:

> ok bro 56 and 59 are done and marked complete, just need to commit

The human's worktree edits check T056 and T059. This checkpoint preserves those
edits and records the completion attestation; it does not substitute agent
inspection for human review. No additional per-scenario reviewer notes,
contributor details or reference-machine measurements accompanied the
confirmation, so none are invented or inferred from the earlier macOS/CI runs.

The original implement run resumes on its original branch and draft PR.
T060-T064 remain unchecked and have not been executed by this commit request.
Their final documentation, all-suite/CI verification and evidence reconciliation
remain separate work. This confirmation is not explicit acceptance of spec 001,
a completed implement transition, authorization to run converge or a merge
request.

Commit-only validation passed: `python3 scripts/check_repository.py`,
`python3 -m unittest discover -s scripts/tests -v` (**100 tests**) and
`git diff --check`. Product suites were not rerun locally for these
documentation/task/state-only changes; T061's final all-suite remains pending.

### Final implementation reconciliation - 2026-10-21

The human explicitly directed continuation through the rest of implement.
The original running stage, branch and draft PR are retained; no second
begin, converge, review or acceptance is being run.

**T060 is complete.** README, contributor/docs index, architecture, local guide,
dependency adoption and quickstart were reconciled against the implemented
bootstrap and its exact pins. The guides distinguish installed versus selected
tools, explicit restore/init, provider-owned recovery, local-only public
diagnostics, unavailable future auth/modules/MCP/Kubernetes features and
human-reported evidence from measured automation. Dependency guidance now also
records the case-sensitive NuGet metadata correction; no adoption decision or
lockfile changed.

US1.4's final check reproduced the human's opaque tool-preflight failure:
all three product-tool failures were incorrectly labeled `supervisor`.
The existing regression now fails before the fix for wrong-version/nonzero
and missing-tool cases, then passes with the actual fixed allowlisted tool
name. Raw tool output remains suppressed. The default shell's actual CLI
refusal now reports `tool_version_invalid` / `dotnet`, without its raw SDK
error or installation path. `python3 -m unittest discover -s scripts/tests -v`
passes **100 tests**; the repository checker and `git diff --check` pass.
This small correctness fix preserves exact version enforcement rather than
weakening it. Its final all-suite and actual CI results are recorded below
after execution, not inferred from these focused checks.

The owned manual-review supervisor was stopped before final verification;
its persistent developer SQLite store was retained. Verification owns its own
private roots/ports and does not use or reset that store.
