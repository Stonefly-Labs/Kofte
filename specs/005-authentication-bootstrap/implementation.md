# Implementation Evidence: 005 - Authentication and Bootstrap Administration

**Result:** blocked at T002, before dependency adoption.
**Stage / date:** implement, 2026-10-26.
**Assessed baseline:** `6df9eebdcfa6b273126b0c51c272d1b2c2f953e6`.
**Branch / directory:** `feature/005-authentication-bootstrap` /
`specs/005-authentication-bootstrap`.
**Checkpoint:** [draft PR #8](https://github.com/Stonefly-Labs/Kofte/pull/8).

## T001: prerequisites and integration boundaries

T001 is complete: reviewed the approved specification, plan, research, complete
task list, model, both versioned contracts, future guide, passing analysis,
canonical inputs and constitution 2.0.1. ADR 0007's later explicit approval
**"ok, I approve it"** governs its historical Proposed planning statements.
This is design approval, not approval of unchecked packages or feature 005.

The worktree began clean on its recorded branch, local/remote feature heads
matched, and PR #8 was open/draft. Updated `origin/main` was `3e41681`, the
human merge of accepted 004. This invocation continues 005; it does not create
a new branch or re-run a completed stage. The driver began implement exactly
once. `.specify/extensions.yml` is absent: no before/after implementation hook
is configured.

| Read-only checklist gate | Total | Checked | Unchecked | Result |
| --- | --- | --- | --- | --- |
| `checklists/requirements.md` | 16 | 16 | 0 | Pass |
| `checklists/quality.md` | 36 | 36 | 0 | Pass |

Neither checklist nor any approved design artifact was changed. The existing
ignore files cover local secrets, dependencies, .NET/Next outputs and browser
artifacts; no new package publishing, Docker build or chart is introduced.

Accepted 001-004 remain the integration boundary: retain anonymous aggregate
health, strict unknown routes, identical empty/composed enforcement, stopped
maintenance authority, exact platform inventories, immutable module assignments,
provider isolation and migration-1 bytes. Identity must add verified format 2
and recovery-v2, not relax old validation. First-admin designation is not a
permission grant. No auth route, migration, fixture bypass or package is
partially wired while its prerequisites are blocked.

### Actual available profile

The initial default `dotnet --version` failed: global.json requests 10.0.402,
but the default installation exposes only 10.0.203. The existing isolated
`artifacts/toolchain/dotnet-10.0.402` installation resolves this tooling issue:
with its directory on PATH and DOTNET_ROOT set consistently, `dotnet --version`
returns **10.0.402**, and `dotnet --list-runtimes` lists both
Microsoft.AspNetCore.App and Microsoft.NETCore.App **10.0.13**.
No SDK download, global installation, version change or roll-forward was used.

Observed profile: macOS 26.5 arm64, Python 3.15.0, Node 24.22.0,
npm 11.19.1, Docker server 29.8.2; the existing npm restore is present.
No host, issuer, browser, PostgreSQL fixture or other runtime helper was started.
Microsoft Learn/Context7 MCP are unavailable; no uncertain library API was
implemented and no MCP result is claimed.

### Preserved failure baseline

The earlier 005 specification-head `composition-integration-postgres` /
`private-diagnostics` failure (`composition_cleanup_incomplete` /
`composition_consumer_failed`) remains undiagnosed. Its
[failed run](https://github.com/Stonefly-Labs/Kofte/actions/runs/40325294237)
is not repaired by this checkpoint. Feature 004's failed/interrupted local
complete gate and human-deferred PostgreSQL observation remain limitations,
not a passing new complete gate or release waiver.

At inspection, baseline analysis-head
[repository checks](https://github.com/Stonefly-Labs/Kofte/actions/runs/40404584695)
passed. In its separate
[product run](https://github.com/Stonefly-Labs/Kofte/actions/runs/40404584613),
quality, unit, browser and startup passed; six other contexts were still
in progress. This is an observation of existing code, not new identity proof
or a claim that the complete matrix passed. GitHub reports no required checks.

## T002: partial candidate review; adoption refused

Official NuGet nuspecs and actual `.nupkg` archives were fetched without adding
a package reference, restoring a candidate or changing the reviewed inventory.
The net10.0 candidate closure adds nine identities:

| Candidate | Version | Declared license / target | Advertised source |
| --- | --- | --- | --- |
| Microsoft.AspNetCore.Authentication.OpenIdConnect | 10.0.13 | MIT / net10.0 | dotnet/dotnet `4099f83977f4d29604a9aeb971fb0a7bc1d5f699` |
| Microsoft.IdentityModel.Protocols.OpenIdConnect | 8.19.2 | MIT / net10.0 | AzureAD source revision below |
| Microsoft.IdentityModel.Protocols | 8.19.2 | MIT / net10.0 | Same |
| System.IdentityModel.Tokens.Jwt | 8.19.2 | MIT / net10.0 | Same |
| Microsoft.IdentityModel.Tokens | 8.19.2 | MIT / net10.0 | Same |
| Microsoft.IdentityModel.JsonWebTokens | 8.19.2 | MIT / net10.0 | Same |
| Microsoft.IdentityModel.Logging | 8.19.2 | MIT / net10.0 | Same |
| Microsoft.IdentityModel.Abstractions | 8.19.2 | MIT / net10.0 | Same |
| Microsoft.Bcl.Cryptography | 10.0.2 | MIT / net10.0 | dotnet/dotnet `44525024595742ebe09023abe709df51de65009b` |

IdentityModel's Logging.Abstractions/DI minimums are 8.0.0; the existing central
10.0.13 pins would apply. This metadata walk is **not a restored actual graph**
or permission to adopt additional minimum versions. The reviewed existing union
remains 403 npm / 78 NuGet identities.

Every candidate archive contains `.signature.p7s`, and its libraries include
net10.0 assets. Signature **presence is not cryptographic verification**:
no new `dotnet nuget verify --all`, Linux signature result, locked restore,
build or compatibility result is claimed.
The original OIDC `THIRD-PARTY-NOTICES.TXT` SHA-256 is
`307d014f65d8482314f1400ddeae7a0cbabb96c2207bcc77f6233cc10588e5d9`;
Bcl.Cryptography's notice SHA-256 is the already observed
`6d15e10a101c6bfff2ab4429ed061bf76c456fc4b23ad6b03e0d0f8377148a21`.
The seven IdentityModel archives contain no named license/notice file;
their nuspec MIT declaration alone does not complete original-source review.
No package notice or provenance record was added to the adopted inventory.

Both current official NuGet vulnerability-feed pages were inspected:
base `2026-10-19T23:33:13.4608684Z` and update
`2026-10-26T11:34:12.9215033Z`. Their IdentityModel/JWT advisory ranges affect
older versions, not 8.19.2; no OIDC/Bcl.Cryptography entry was present.
This bounded feed observation is not a complete adoption/audit pass.

### Blocking evidence and correction

All seven IdentityModel nuspecs advertise repository
`https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet`
at revision `25d90ed3f48854036d444541a049089ccd198707`.
Independent attempts could not verify that exact source:

- Official GitHub tree at that revision: **HTTP 404**.
- Raw GitHub `LICENSE.txt` and alternate `LICENSE` at that revision:
  **HTTP 404**.
- `gh api` commit lookup: **HTTP 403**, organization SAML enforcement.
  An alternate official tag-content lookup was also denied by SAML.

These results establish **unresolved source provenance/access**, not a claim
that the package is malicious, that its advertised commit does not exist, or
that every source/license route has been disproven. No credentials, SSO
authorization URL or private source is included in this report.

T002 requires original provenance/license review before adoption. It remains
unchecked and blocks every later task through its explicit prerequisites.
Do not replace the version, weaken checks, claim archive signature presence
as verification or continue with a hand-written protocol implementation.
Restore authorized access to the advertised upstream revision, or obtain
independently verifiable official exact-revision source/license evidence.
Then resume this same active implement stage with the driver, finish T002's
archive/signature/advisory/actual-graph/lock/inventory gates and proceed in task
order. A different candidate requires an explicitly reviewed plan-compatible
adoption decision, not a silent fallback.

## Acceptance and constitutional evidence matrix

No user story or behavioral requirement is implemented by this checkpoint.
The complete independent acceptance obligations remain in the unchanged
[guide matrix](quickstart.md#acceptance-matrix) and task traces.

| Intent | Required implementation/evidence | Current result |
| --- | --- | --- |
| US1.1-5; FR-001-004/008/015/019/021; SC-001/003 | T004/T023-T040: independent HTTPS OIDC, exact mapping, direct/adapter/UI parity, trust/budget rejection and fresh restarts. | Not built or executed; blocked by T002. |
| US2.1-5; FR-010-012/017; SC-002/006 | T041-T055: delivered nomination, twenty contenders, atomic commit/uncertainty and three restarts on both providers. | Not built or executed; no designation or enrollment claim. |
| US3.1-5; FR-005-009/018/021; SC-001/003 | T018-T021/T056-T070: exact actor/default denial, CSRF, session/revocation, safe health and adapter/contract evidence. | Not built or executed; existing host behavior unchanged. |
| US4.1-3; FR-013/014; SC-004 | T071-T078: explicit loopback development, visible mode and persisted production isolation. | Not built or executed; no fallback added. |
| US5.1-4; FR-015-020; SC-005-007 | T011-T022/T079-T092: both-provider format/recovery/key/capacity/privacy and actual accessible failure/retry evidence. | Not built or executed; no storage or key mutation. |
| FR-022; all outcomes; principles I-XII | T093-T100: fail-closed suite inventories, all delivered public recipes, complete local/actual CI and final traceability. | Pending, not waived by this documentation checkpoint. |

Principles I/II/IV/V/VI/VII/VIII/IX/X/XI/XII preserve the approved feature scope,
ownership, default denial, honest transactions, independent evidence and later
MVP boundaries. Principle III/VI's dependency provenance gate is the actual
blocker. No product test, permission engine, MCP/job/realtime runtime, full
shell, deployment profile or constitutional exception is introduced.

## Checkpoint verification and handoff

Executed for this documentation/ledger checkpoint:

| Command / check | Actual result |
| --- | --- |
| `SPECIFY_FEATURE_DIRECTORY=specs/005-authentication-bootstrap .specify/scripts/bash/check-prerequisites.sh --json --require-tasks --include-tasks` | Passed; selected the recorded absolute feature directory and required inputs. |
| `python3 scripts/check_repository.py` | Passed; assets, documentation links, MCP parity and blocked driver state valid. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed: 121 tests, zero failures, 2.552 seconds. |
| `git diff --check` | Passed. |
| `SPECIFY_FEATURE_DIRECTORY=specs/005-authentication-bootstrap python3 .github/skills/drive-speckit/scripts/state.py status` | Confirmed 005 / implement blocked on its original branch, with the exact T002 reason. |
| Read-only baseline preservation assertions | Passed: 100 original task descriptions, exactly T001 checked; ten design/review artifacts, approved ADR, constitution and dependency inputs unchanged. Historical ledger entries/features/constitution unchanged; only active implement state added. |
| `gh pr checks 8 --required --json name,state,link,workflow` | GitHub reports no required checks for this branch. |

Product build/protocol/provider/browser suites are not applicable to these
documentation/ledger-only changes and are **not run as new authentication
evidence**. The blocked dependency review does not supply any missing
behavioral or cryptographic verification. Publication retains the same draft
PR; its latest-head GitHub checks are observed separately in the PR body,
not substituted for identity implementation.

Only T001 is complete; T002-T100 remain unchecked. Historical ledger entries,
analysis, approved inputs, ADR and both reviewer gates are preserved.
The driver records **implement blocked**, not complete; no converge or other
stage is run. Keep this original branch and draft PR #8. Resume implement only
after resolving T002. No feature acceptance, readiness or merge is requested.
