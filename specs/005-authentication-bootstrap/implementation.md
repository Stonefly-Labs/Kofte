# Implementation Evidence: 005 - Authentication and Bootstrap Administration

**Result:** in progress; original source-provenance blocker resolved, T002 adoption pending.
**Stage / date:** implement, 2026-10-26.
**Assessed baseline:** `6df9eebdcfa6b273126b0c51c272d1b2c2f953e6`.
**Branch / directory:** `feature/005-authentication-bootstrap` /
`specs/005-authentication-bootstrap`.
**Checkpoint:** [draft PR #8](https://github.com/Stonefly-Labs/Kofte/pull/8).

The initial blocked assessment below is retained as history. The
[source-provenance follow-up](#t002-follow-up-source-provenance-resolved)
resolves that blocker and resumes the original implement stage; it does not
complete T002, adopt a package or implement authentication.

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

## Initial blocked checkpoint verification and handoff

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

At that initial checkpoint, only T001 was complete; T002-T100 remained unchecked.
Historical ledger entries, analysis, approved inputs, ADR and both reviewer gates are preserved.
The driver records **implement blocked**, not complete; no converge or other
stage is run. Keep this original branch and draft PR #8. Resume implement only
after resolving T002. No feature acceptance, readiness or merge is requested.

## T002 follow-up: source provenance resolved

**Date / baseline:** 2026-10-26 / `88c68c6ebc19ee4214aaf2eba08c85def6cdcb5b`.
**Scope:** Investigate and resolve the reported provenance/license blocker,
not perform the remaining dependency adoption or the whole 005 implementation.

### Root cause and authoritative evidence

Anonymous access to the public repository succeeds. Its commit API returns
422, **"No commit found"**, for
`25d90ed3f48854036d444541a049089ccd198707`; the revision's license remains 404.
Authenticated lookup separately reports organization SAML enforcement (403).
The latter is not a prerequisite for this resolution and no credential,
scope, organizational authorization or SSO setting was changed.

The official NuGet symbol archives supply the missing repository context.
Every investigated net10.0 PDB's SourceLink maps `/_/*` to:

```text
https://raw.githubusercontent.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet-ghsa-97f9-wg23-8r98/25d90ed3f48854036d444541a049089ccd198707/*
```

That is a security-advisory fork, not the public repository in the nuspec.
Its exact revision/license also returns 404; this does not establish whether
the fork was deleted or is private. The public
[official advisory](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/security/advisories/GHSA-97f9-wg23-8r98)
identifies 8.19.2 as a fixed 8.x release. Its SAML vulnerability is separate
from GitHub's SAML access-control error; no SAML package/protocol is introduced.

The official [8.19.2 release](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/releases/tag/8.19.2)
points to public commit `ee19952f21d5c170a778bdca901b67076a1cd8f8`.
The official 8.19.1 tag points to
`3dba1997da46a3fbc60ce6d7e94c15ca867f9571`.
Both commits' original `LICENSE.txt` files are accessible and byte-identical
Microsoft MIT licenses, SHA-256
`cba03f5387b05405e56b688376421acae396136af4b5116738dc6e5160f87ddd`.
No license was inferred merely from the package name or current default branch.

### Exact compiled-source correspondence

A one-off, package-free .NET 10 metadata reader outside the worktree inspected
the seven official `.nupkg`/`.snupkg` pairs without executing their assemblies.
For each selected net10.0 assembly:

1. Its CodeView GUID, timestamp and age match the portable PDB.
2. Its PE checksum entry matches SHA-256 of that PDB with its 20-byte ID
   zeroed, as required by the official
   [Portable PDB checksum specification](https://github.com/dotnet/runtime/blob/main/docs/design/specs/PE-COFF.md#portable-pdb-checksum).
   A raw-PDB hash is not this checksum; no failed raw comparison is counted.
3. Every PDB document's original SHA-256 matches source from the exact
   official public revisions above or its cryptographically bound embedded
   generated source. Public checkout bytes require LF-to-CRLF conversion,
   preserving BOMs and all other bytes/encoding; no whitespace, code or
   arbitrary-content normalization is permitted.

| Exact IdentityModel 8.19.2 candidate | Verified net10.0 documents | `.nupkg` SHA-256 | `.snupkg` SHA-256 |
| --- | --- | --- | --- |
| Microsoft.IdentityModel.Abstractions | 11/11 | `bcd18aeaee95f92d0c21a61383eb129111785d48ccd4af9c2ae4756bd69108bb` | `b95b71d389fa5704bdafd075089f0782d977e13129a5523b0166a92fef63c811` |
| Microsoft.IdentityModel.JsonWebTokens | 27/27 | `8fd6a2d0dae0cad0ec7d08888a3a7521be234465fd960444401a04e975bd48e7` | `da24aa73192e0f5bf1e57f7a25738292e6a338227a722f5dc1b09ffe2124e38e` |
| Microsoft.IdentityModel.Logging | 14/14 | `8daf0f01850dc257c8ae42fa10eb99ed28197d127020e4461bf1130c3d41b511` | `4ea980808c91ea3e8edd9cfda8cac9bcf605cdff95546f450e627a131541b998` |
| Microsoft.IdentityModel.Protocols | 25/25 | `b2800311442c3a9d47cadb75a8ba3e8ce39eabbbb2ec8eed50e5a88177905c56` | `d6cda9ae8ff5ecfec09466303d7189082cc3abadddbb39c4e0056fcd00004278` |
| Microsoft.IdentityModel.Protocols.OpenIdConnect | 27/27 | `b92acde71946f925e5c60e0d002154d854a50b7577858a2376153c95473af3ba` | `eb10b808765a5d39a078d6eac5ef5858deaa19ff6c3c440d2a127a634a1f4fe7` |
| Microsoft.IdentityModel.Tokens | 157/157 | `3ce8b74b370adfe881df5d38c17621de8a150f519b96dc1351ff8a323fff5185` | `ec460a4c98371c3831c148893667dc84fe10761a1b16a0303027c4e7102163d1` |
| System.IdentityModel.Tokens.Jwt | 14/14 | `904a9f1225a4a2d7c3503a619f432c32a84e73cdaa40b5e56e8e57620e7cfe2a` | `b2f91c13520d9f10e3993351e942505165d7425f5d6ab88fc3de88a29e60aedf` |

**All 275 document entries pass**, including repeated shared inputs:
259 correspond to public 8.19.2 source and fifteen to embedded generated
source. The remaining input,
`src/Microsoft.IdentityModel.Tokens/AppContextSwitches.cs`, matches the
immutable public 8.19.1 revision, not the 8.19.2 tag. Its expected original
CRLF SHA-256 is
`920074a86f0f8976c0bf642e0f377fcad4c23f8105d2c77ac69d0b40471583f9`.
Its public LF bytes have SHA-256
`57854ee6bcfeac85b34fa9e506b55f62c6b4fab5b7db012f0eaf80674a460465`.
This verifies that exact unchanged compile input; **no 8.19.1 package is
selected, installed or substituted**. Merely equating the 8.19.2 tag with
the advertised private-fork commit would have missed this difference.

This evidence resolves the original candidate's source/license assessment.
It does not prove an identical entire private repository, cover other target
frameworks, reproduce Microsoft's build or waive signature/runtime checks.
The one-off reader compiled with SDK 10.0.402 with zero warnings/errors;
its input/output and downloaded public evidence remain outside the worktree.
No downloaded source was built or copied into the product.

### Actual cryptographic signature verification

The already reviewed immutable Linux SDK image was not locally cached;
it was pulled by its exact approved digest. With only the public candidate
evidence mounted read-only, this inner command executed:

```bash
dotnet nuget verify --all /evidence/*.nupkg
```

Image:
`mcr.microsoft.com/dotnet/sdk@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa`,
platform `linux/amd64`, normal online revocation/trust checks.
**Passed: all nine archives, exit 0, zero warnings/errors.** Each reports
Microsoft Corporation author and NuGet.org repository signatures. This is
cryptographic verification, not the earlier observation of `.signature.p7s`.

Besides the seven rows above, the verified archives are:

- OIDC 10.0.13: SHA-256
  `317e2f65bff5b70cd586a548b521c482388c02d8d85e09426291cce4dd53e276`.
- Bcl.Cryptography 10.0.2: SHA-256
  `f8e62d716b1dd6a65c11769d61e038b7afa9c800e0d403d00b1a4a52d3f4d363`.

No host credentials/source were mounted, certificate installed, trust check
disabled or signature warning accepted. The owned verification container
exited and was removed automatically.

### Current handoff and verification

The driver's original **005 / implement** stage is resumed, not completed;
the recorded branch, feature directory, start time and history are retained.
No second stage/run, version decision, ADR amendment or checklist change
occurred. The pre-execution prerequisite command passes, reviewer gates
remain 16/16 and 36/36, and no extension hooks are configured.

**Only T001 is complete.** T002's actual restored graph, central pins/package
reference, locks/inventory/notice reconciliation, current advisory audit and
compatibility validation remain implementation work. T003-T100 must continue
to honor that prerequisite. No package, product/test code, migration, runtime
behavior, authentication proof or feature acceptance is introduced here.
Earlier failed checks and the initial blocked evidence above remain historical
failures, not converted into passes by this investigation.

| Follow-up check actually executed | Result |
| --- | --- |
| `python3 scripts/check_repository.py` | Passed: repository assets, documentation links, MCP parity and resumed driver state. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed: 121 tests, zero failures, 2.235 seconds. |
| `git diff --check` | Passed. |
| `SPECIFY_FEATURE_DIRECTORY=specs/005-authentication-bootstrap .specify/scripts/bash/check-prerequisites.sh --json --require-tasks --include-tasks` | Passed: recorded feature directory and required inputs. |
| `SPECIFY_FEATURE_DIRECTORY=specs/005-authentication-bootstrap python3 .github/skills/drive-speckit/scripts/state.py status` | Confirmed 005 / implement running, original branch/directory/start time, no blocker reason. |
| Evidence/baseline assertions | Passed: all sixteen candidate/symbol archive hashes match this report; all 275 bound source entries match; tasks, approved design/review artifacts, dependency inputs and historical ledger entries remain unchanged. |
| New product build/protocol/provider/browser tests | Not run: no product or adopted dependency changes; no authentication behavior verified. |

Publication and latest-head CI outcomes are recorded in the same draft PR #8.
No readiness change or merge is requested.
