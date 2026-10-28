# Implementation Evidence: 005 - Authentication and Bootstrap Administration

**Result:** implement in progress; 44/100 complete (T001-T040, T042, T049-T050, T052); US2 core delivered, complete enrollment/browser and later-story evidence remain.
**Stage / date:** implement, 2026-10-26; continued 2026-10-28.
**Assessed baseline:** `6df9eebdcfa6b273126b0c51c272d1b2c2f953e6`.
**Branch / directory:** `feature/005-authentication-bootstrap` /
`specs/005-authentication-bootstrap`.
**Checkpoint:** [draft PR #8](https://github.com/Stonefly-Labs/Kofte/pull/8).

The initial blocked assessment below is retained as history. The
[source-provenance follow-up](#t002-follow-up-source-provenance-resolved)
resolves that blocker and resumes the original implement stage. The later
adoption and setup sections record the actual code increments; authentication
is not yet wired or accepted.

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

## T002: actual dependency adoption

The subsequent implement invocation adopts the reviewed OIDC 10.0.13
reference, seven IdentityModel 8.19.2 pins and Bcl.Cryptography 10.0.2.
Exactly nine identities enter the restored graph; the complete reviewed union
is now 403 npm / 87 NuGet. A separate byte comparison confirms every new
restored `.nupkg` is identical to the source-reviewed, signature-verified
candidate. Original archive notices remain intact and inventory-reconciled;
no source copy or change to the existing source-copy notices is introduced.

| Executed T002 command/check | Actual result |
| --- | --- |
| `dotnet restore Kofte.slnx --force-evaluate` | Passed; locks regenerated through NuGet. |
| `dotnet restore Kofte.slnx --locked-mode` | Passed for all thirteen projects. |
| `dotnet build Kofte.slnx -c Release --no-restore --nologo` | Passed; zero warnings/errors. |
| `dotnet list Kofte.slnx package --vulnerable --include-transitive --format json --no-restore` | Passed; thirteen projects, zero vulnerable direct/transitive packages and no reported audit problems. |
| `python3 scripts/check_dependencies.py --prepare-bundled-evidence` | Passed; complete 403 npm / 87 NuGet inventory, locks, provenance, archive integrity and notices. |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-build --no-restore --nologo` | Passed: 192 tests, zero failures/skips. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet` | Passed. |

All .NET commands selected the existing isolated SDK 10.0.402/runtime 10.0.13.
**T002 is complete.** This adds a package reference, not login, sessions,
enrollment or any other authentication behavior. Dependent work may now begin;
the active implement stage remains incomplete.

## T003-T006: executable independent test setup

The separately compiled identity consumer is in the solution and referenced
only by test projects. Five actual invalid/unimplemented invocation cases
exit nonzero with zero assertions and safe failure JSON; no public scenario
is falsely implemented. Actor probing is the dependent T009 work.

The independent HTTPS issuer has its own ephemeral private CA, RSA signing
key, discovery/JWKS/code/token endpoints, confidential-client checks, PKCE,
nonce-bound independently signed JWTs and consumed authorization codes.
Its CA is trusted only by the fixture-created client handler; an ordinary
client rejects it. Bad issuer/audience/signature/algorithm/time/nonce
variants and safe operation observations are test-owned, not product flags.

The process fixture reuses the accepted owned roots, provider scope and
process/descendant cleanup. Private one-use files admit test children.
A test-owned native .NET startup hook installs an observer on diagnostic
commit phases; atomic private reached/release records avoid touching public
routes or supervisor IPC. No test hook or control reader is referenced by
production. Actual host-store commit/interruption evidence remains dependent
foundation/story work, not a claim made by the fixture readiness probe.

The independent C#/TypeScript corpora freeze route, credential, CSRF, exact
return-path, signed-evidence, capacity, rate, size, lifetime and checked-counter
expectations. These are acceptance inputs, not executed product cases.

| Executed setup check | Actual result |
| --- | --- |
| Initial issuer tests before fixture implementation | Red: missing `OidcIssuer` compilation errors. |
| Initial private barrier tests | Red: non-atomic publication let readers race a still-open reached file; fixed with private write/flush/close/atomic rename. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityFixtureTests\|FullyQualifiedName~OidcIssuerTests' --nologo` | Passed: four tests, zero skips; actual HTTPS exchange, key signature/nonce, client/PKCE/replay refusal, private phase hold/release and verified real child exit before owned-root deletion. |
| `dotnet build Kofte.slnx -c Release --no-restore --nologo` | Passed; fourteen projects, zero warnings/errors. |
| `dotnet restore Kofte.slnx --locked-mode` | Passed; new test project locks reconciled, no new package identity. |
| `./node_modules/.bin/tsc --strict --noEmit --skipLibCheck --types node --target ES2022 --moduleResolution bundler --module esnext tests/contract/identity-corpus.ts` | Passed. |
| `./node_modules/.bin/prettier --check tests/contract/identity-corpus.ts` | Passed. |
| `./node_modules/.bin/eslint tests/contract/identity-corpus.ts` | Passed. |

An attempted `tsc -p tests/contract/tsconfig.json` did not run a type check
because that file does not exist. The succeeding command above uses the
documented package's existing compiler flags and the exact new file.

### Additional published-head CI baseline evidence

Run `40409807625`, integration-postgres job `121877224218`, failed in
`composition-integration-postgres`; host/storage PostgreSQL phases passed.
The preserved artifact identifies
`PostgresCompositionPreflightTests.AdmissibleUnregisteredServingOwnerRequiresPreparationWithoutAllocating`
with storage code `Busy`. Its assessed revision is GitHub's merge revision
`967bd1e3fa4c3493bb3d6ddb901153c11a498e6e`, not new identity implementation.
No retry or diagnosis converts this into a pass; the older private-diagnostics
failure remains separately preserved.

## T007-T010: configuration and public actor foundations

The strict configuration component now requires explicit `oidc` or
`development`, exact public origin/all-loopback internal topology, bounded
UTF-8 JSON with duplicate/unknown-field rejection, private regular canonical
no-symlink secret references and a separate key ring outside recovery data.
Development requires the explicit Development environment and one to eight
exact synthetic identities. OIDC requires exact HTTPS endpoints/network
allowlists, private secret files and a matching RSA PEM certificate/private
key. Neither parsing nor stopped revocation contacts discovery/the IdP.
Revocation validates only its needed static subset when serving secrets are
lost. The trust digest preserves exact issuer/client/network/public-origin
trust and excludes rotatable secret values/display text.

`KOFTE_AUTH_CONFIG_FILE` is the only new bootstrap environment key. Existing
maintenance forwarding, including composed workers, carries it deliberately.
This adds a component, **not startup/serving identity wiring** (T021).
No stopped identity command or HTTP authentication route is delivered yet.

Platform now exposes only the immutable actor-v1, its two-valued mode and
read-only nullable current-actor interface. The separately compiled probe
uses those public types; reflection/construction tests reject credential/
permission payload, mutable properties, an empty user ID or unknown mode.
It is a trusted in-process contract, never deserialized credential proof.

| Executed foundation-component check | Actual result |
| --- | --- |
| Initial configuration and actor tests before production code | Red: missing host identity component and public actor types. |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityConfigurationTests --nologo` | Passed: 35 introduced configuration cases, zero skips. |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --nologo` | Passed: all 227 host unit tests, zero failures/skips. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter FullyQualifiedName~ActorContractTests --nologo` | Passed: three actor cases, zero skips. |
| Four fixture integration cases rerun after shared private-file extraction | Passed: four tests, zero skips. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed: all 121 tests. |
| `git diff --check` | Passed. |

No product schema, operator command, login, enrollment, session policy or web
UI is claimed complete by these component results. The active implement stage
and original draft PR remain open; T011-T100 are still required.

### Publication quality correction and operation-subset regression

The first `python3 scripts/verify.py --suite quality` attempt failed at
`dotnet-format` on introduced test/fixture initializer whitespace. The SDK
formatter corrected those files; Prettier restored the affected contract
lock-file formatting. The SDK solution tool keeps the new consumer at the
solution root, matching existing projects.

The subsequent **full quality suite passed all twenty steps**, including
repository/Python/whitespace checks, locked restore, .NET format/build,
npm restore, Prettier, ESLint, TypeScript, contract generation, workspace
builds and dependency review. This does not claim integration, browser or
complete identity acceptance.

A further focused regression validates bootstrap authorization's offline
subset: it succeeds without the provider client secret, but refuses a
mismatching private key or missing protection certificate. The exact focused
configuration command above now passes **36 cases, zero skips**, and
`dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet`
plus `git diff --check` pass after that addition. It exercises only the
configuration component, not a delivered bootstrap authorization command.

The complete host unit command above was then rerun: **228 tests passed,
zero failures/skips**.

## T011-T013: exact versioned identity storage format

Provider-specific migration 2 creates the eight model tables, deterministic
external keys, matching-mode user/session/designation foreign keys, checked
counters, nullable-state combinations, bounded enums and indexes. A fixed
`initial_user_mode = 'oidc'` FK component prevents a development user from
becoming the production designation. Installation-retained evidence cannot
be updated/deleted; session-class pruning remains later owner-operation work.

The original installation/history version-1 CHECKs forbid a simple append.
SQLite therefore transactionally rebuilds those two metadata tables, restoring
the same final names and accepted triggers; PostgreSQL transactionally evolves
the version constraints and restores the immutable installation trigger.
All existing rows/IDs/timestamps survive. The original migration-1 sources
remain byte-identical:

| Provider | SHA-256 |
| --- | --- |
| SQLite | `5d33f7aaa2c2c1af379af08816fc67dce412f53f538709adb9c233ebc43a4809` |
| PostgreSQL | `c129c9ab66618acd157ac7e4114073228463fa3880dbe1da4f863ce05648de6e` |

`ApplyIdentityFormatAsync` requires the existing owned stopped scope and a
caller-owned transaction. It preflights exact v1/v2, applies migration/history,
verifies unchanged installation identity/module revision total, and leaves
commit to its caller. This allows T017 to include initial identity metadata/
disabled bootstrap in the same store transaction; no operator prepare command
or usable identity key ring is delivered here.

`RegistryPhysicalFormat` compares frozen OID/data-independent native-catalog
fingerprints for each provider/version, not a learned runtime baseline.
SQLite fingerprints actual table/index/trigger/view SQL; PostgreSQL includes
relation/column definitions, types/collations/nullability/defaults/generation,
PK/FK/UNIQUE/CHECK definitions/validation, indexes, internal/explicit trigger
configuration, routines, rules and policies. Actual table inventories and
ordered source digests are also checked separately. Foreign-key integrity and
SQLite integrity/user version are verified. No arbitrary-extra-table fallback
is introduced. Registration validation explicitly admits supported v1/v2 while
module allocator/markers remain version 1. Existing reconciliation/composition
inventory readers already share this runner, so no duplicate path is added.
Recovery-v1 manifest validation still requires actual format 1.

Preflight also refuses unowned objects in table-free v0 *before* allocating
registry state. An independently inserted view remains intact after refusal.

### Real-provider evidence

The new tests exercise actual owned SQLite/PostgreSQL. They preserve exact
installation/registration/allocation/marker/revision snapshots, execute the
existing module-owned EF migrations `202610210001_Initial` and
`202610210002_AddNote`, and verify their populated history plus exact retained
row ID/payload/note after upgrade. They check rollback/idempotence, v0/unknown
formats, foreign scope refusal and the exact eleven-table inventory.
Independent SQL tests exercise case-sensitive keys, cross-mode FK refusal,
Unicode display bounds/controls, invalid nullable states, checked overflow
and installation-event immutability. Individual physical mutations remove
CHECK/FK/UNIQUE/nullability/index guarantees or add a table/view/default/
generated column/trigger/routine, or alter collation/history; each refuses
without repair.

| Command/check actually executed | Result |
| --- | --- |
| Initial `--filter FullyQualifiedName~IdentityFormatTests` before runner implementation | Red: missing `ApplyIdentityFormatAsync` compilation errors. |
| Initial 20-case execution | 18 passed; two preservation assertions used reference equality for `StorageAssignment`; corrected to the existing complete value snapshot, not weakened to counts. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityFormatTests\|FullyQualifiedName~RegistryFormatTests' --nologo` | Passed 44 cases, zero failures/skips, after adding independent physical/native constraint coverage. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityFormatTests&FullyQualifiedName~UnknownVersion\|FullyQualifiedName~IdentityFormatTests&FullyQualifiedName~ForeignStoppedScope' --nologo` | Passed four further cases, zero failures/skips. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityFormatTests.FormatTwoPreserves\|FullyQualifiedName~RegistryConstraintTests\|FullyQualifiedName~StorageMaintenanceDiagnosticsTests\|FullyQualifiedName~CompositionPreflightTests' --nologo` | Passed 74 cases, zero failures/skips; includes the strengthened nonempty EF-history/data preservation and dependent both-provider registry/composition/diagnostics regressions. |
| `shasum -a 256 src/Kofte.Infrastructure/Storage/Registry/Migrations/Sqlite/001_registry.sql src/Kofte.Infrastructure/Storage/Registry/Migrations/Postgres/001_registry.sql` | Passed: the two immutable values above. |

PostgreSQL commands explicitly set
`KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`; test ownership/cleanup is unchanged.
Native catalog measurements used disposable owned databases; the temporary
measurement harness was removed after freezing the reviewed catalog values.
These results do not prove T022's complete foundation gate, working login,
recovery-v2 or a delivered operator preparation workflow.

## Historical blocker: deadline-safe store execution before T014

The approved local identity work budget is **one second**, within the unchanged
four-second callback envelope. The accepted shared `OperationBudget.CommandSeconds`
floors remaining seconds and refuses values below one. Since even a fresh
one-second budget immediately has less than one second remaining, it rejects
its first command; `StorageInstallation.OpenPlatformAsync` uses that helper.
Reusing it would make every identity-store operation fail before useful work.

Two actual disposable probes were executed with:

```text
dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityBudgetProbeTests --nologo
```

The unmodified helper immediately threw `OperationCanceledException`.
The alternative probe allowed a rounded one-second native command, after
250 ms of the same one-second budget, while a separately owned SQLite
connection held `BEGIN EXCLUSIVE`. Although cancellation fired at one second,
the native command returned after **1,317 ms**. Rounding up plus cancellation
therefore does not preserve the approved total budget.

The probes deliberately failed to report those measurements, not to represent
passing feature tests. Their temporary source was removed from the test project
and retained only as a session investigation artifact; no probe is skipped,
made falsely green or included in shipping tests. Both connections/transaction
and their owned root were disposed. Product timeout code is unchanged.

With Learn/Context7 MCP unavailable, official Microsoft documentation confirms
[synchronous SQLite async APIs](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async)
and [automatic busy retries/integer command timeouts](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/database-errors).
Zero means unbounded, not fail-fast. No timeout increase, background orphan
operation, zero-timeout fallback, journal-mode change or speculative native
adapter is adopted.

**Implement is blocked, not complete.** Resolve and independently test a
deadline-safe execution/transaction mechanism under the original one-second
local/four-second callback limits, including real contention, rollback/commit
uncertainty and cleanup, then resume T014 on this branch/PR. This is engineering
work, not a credential, SSO or merge request. T014-T100 (87 tasks) remain
unchecked. No later stage, human acceptance or working authentication is claimed.

### Final blocked-checkpoint verification

After removing the disposable probes, `python3 scripts/verify.py --suite quality`
passed all twenty steps. The rebuilt shipping host unit assembly then passed
`dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-build --no-restore --nologo`:
**228 passed, zero failures/skips**, with neither investigation probe present.
The final native-routine guard avoids trying to render an unexpected aggregate
as a normal function; the fixed fingerprints for legitimate formats are
unchanged. Its explicit function/aggregate refusal check,
`dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~UnexpectedNative --nologo`,
passed two cases, zero skips. Final solution format verification passed.
`python3 scripts/check_repository.py`, the exact required
`python3 -m unittest discover -s scripts/tests -v` (**121 passed**) and
`git diff --check` also passed. These green shipping-code checks do not erase
the failed deadline experiments or complete the blocked identity-store work.

## Deadline correction and T014: metadata/admission transactions

The user explicitly requested fixing the engineering issue and continuing.
The original branch/open draft PR and checklist gates were reverified:
requirements **16/16**, quality **36/36**, both read-only. The same active
implement stage was resumed through the driver after the initial twelve
real-provider deadline regressions passed. No second run, stage completion,
converge or acceptance was recorded.

`DeadlineStore` uses Microsoft.Data.Sqlite's documented native-handle API
through the existing locked SQLitePCLRaw package. No third-party source,
package, journal-mode change, infinite command timeout or detached worker
is introduced. It reuses the existing canonical root/file/sidecar/header
guards, enables/verifies foreign keys and binds each parameter explicitly.
Statements are single and results capped at 128 rows. Native busy waiting
is disabled (native `sqlite3_busy_timeout(0)` means immediate refusal, unlike
ADO.NET `CommandTimeout=0`, which remains prohibited/infinite).
Deadline-clipped 25 ms delays retry only SQLite's documented known-not-committed
prepare/autocommit or busy-commit boundaries, never an arbitrary transaction
body or uncertain outcome. VM progress checks also interrupt long CPU-bound
SQL. No database operation is abandoned in a background task.

PostgreSQL uses cancellable Npgsql operations and the existing immediate
socket-close cancellation configuration. Failed unpooled sessions close
without an uncancellable transaction-dispose rollback round trip.
Fifty milliseconds is reserved **within** the remaining parent budget for
cleanup; the local one-second and enclosing four-second limits are unchanged.
Legacy migration timeout behavior is preserved. These are tested provider
execution bounds, not a hard-real-time guarantee for an uninterruptible
operating-system/kernel stall.

T014 now implements stopped, caller-transaction-owned metadata initialization
and normal-host-lease owner mutations. Exact installation/schema/mode/trust/
epoch values are validated; trust changes require explicit future operator
reconciliation, never implicit request conversion. Every successful mutation
advances the checked security revision once, without changing the module
revision total. Missing/foreign/lost authority and overflow fail explicitly.
SQLite uses immediate write transactions; PostgreSQL locks the metadata row
and verifies the borrowed host backend's actual database/shared advisory lock
from each independent operation connection.

Admission persists only fixed categories, bounded lower-hex digests and UTC
minute windows. All eight categories use the exact approved limits, including
ten login starts/browser. Paired counters and the revision share one commit;
refusal rolls back every earlier effect. Live capacity is 4,096 and expired
cleanup is one at-most-128-row batch, never eviction of live windows or a
store-failure fallback. Twelve real concurrent requests admit exactly ten,
refuse two and retain count/revision ten on both providers.

### Actual red/green and broader verification

Both new test files initially failed compilation because their production
executor/store/types did not exist. The first deadline execution passed 9/11:
two exclusive-lock-before-open cases expected cancellation but the preserved
file/header guard correctly returned `Busy`. The tests now separately prove
fail-closed opening and actual contention after opening; no timeout assertion
was weakened. The subsequent twelve deadline cases passed on both stores.
Metadata/admission then passed together with deadlines: **32/32**.

A broader 93-case attempt passed 92, but the positive one-second PostgreSQL
commit case timed out while unrelated provider fixtures ran concurrently.
Strict wall-clock measurements now use a nonparallel xUnit collection so
unrelated container creation/fsync is not part of the controlled profile;
the one-second assertions and actual concurrent-request exercises remain.
The introduced concurrent-admission test then reproduced a real
`NpgsqlOperationInProgressException` on the borrowed shared host session.
Verification was moved to the independent operation connection, retaining
backend/database/lock ownership checks; both concurrency cases then passed.

| Exact executed command | Result |
| --- | --- |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityDeadlineTests --nologo` | Passed 12 cases, zero failures/skips, before subsequent metadata/concurrency additions. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~ConcurrentRequestsShareTheRealHostLease --nologo` | Initially red on the shared PostgreSQL connection; passed both actual-provider cases after the correction. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~Identity\|FullyQualifiedName~RegistryFormatTests\|FullyQualifiedName~RegistryConstraintTests\|FullyQualifiedName~LeaseLoss' --nologo` | Passed **95 cases**, zero failures/skips, including all 41 deadline/admission cases and dependent physical format, constraints, original populated data/history and lease regressions. |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --nologo` | Passed **228 cases**, zero failures/skips. |

Commands use the existing isolated SDK 10.0.402/runtime 10.0.13; PostgreSQL
commands explicitly set `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`. All
owned provider scopes/connections and test tasks are awaited and cleaned up.
No failing measurement harness or skipped case is shipped.

### Prior-head CI limitation

The previous `d644a5f` push's product run **40420590690** is now observed
failed, not pending or green: integration-postgres, recovery, contract,
privacy and failure-propagation failed; quality, unit, integration-sqlite,
browser and startup passed, as did the separate repository check.
Its preserved integration artifact assesses merge revision
`8263e7d7a7b5b415026104359d997bcf58c3ae21`; host PostgreSQL passed, while
storage PostgreSQL reports `Corrupt` in
`PostgresRecoveryDiagnosticsTests.CompleteRealRecoverySetsAndRefusedBoundariesEmitPrivateCorrelatedRecords`
and
`PostgresRecoveryExercise.CompleteCustomDumpRestoresBothSchemasToFreshOwnedTargetWithoutElevatingRuntime`.
This is actual prior-checkpoint evidence, not a diagnosis or a claim that
the current focused gate repairs full recovery/CI. Other failure causes
remain unestablished. Earlier failures and accepted-feature limitations are
preserved above.

**T001-T014 are complete; T015-T100 (86 tasks) remain unchecked.** Transactional
identity evidence is next, then dependent preparation/keys/policy/serving.
No operator identity command, default-deny identity wiring, login/session,
enrollment, web journey, recovery-v2 or final acceptance is delivered by this
checkpoint.

### Final deadline/T014 checkpoint checks

After the final source, authority/concurrency tests and progress documentation
updates, `python3 scripts/verify.py --suite quality` passed **all 20 steps**.
The exact required `python3 scripts/check_repository.py`,
`python3 -m unittest discover -s scripts/tests -v` (**121 passed**) and
`git diff --check` also passed. The quality gate includes full solution
format/build, locked restore, frontend lint/types/build and dependency review.
No new dependency, public contract, approved limit or feature acceptance
changed. This remains a partial implement checkpoint on the original draft PR.

## T015-T017: transactional evidence, persistent keys and stopped commands

The explicit `/drive-speckit` invocation resumes the original running
`005 / implement` stage on its recorded branch and open draft PR #8.
Checklist gates remain requirements **16/16** and quality **36/36**, read-only.
The interrupted identity event/key/test files were verified as unfinished work
from this same implementation, not adopted from an unrelated task. No stage
completion, new run, convergence or acceptance is recorded.

T015 records fixed allowlisted security events in the owner transaction with
random IDs, checked revision, generated correlation, UTC, optional internal user
and fixed committed outcome/retention. Installation events cannot be updated or
deleted. Session retention is thirty days with one at-most-128-row prune per
mutation. Actual 1,000,000/200,000 capacities warn at 90% and refuse the entire
mutation when full; neither a missing FK nor a later error leaves a committed
event or owner revision behind. The existing native deadline executor is reused.

T016 uses maintained Data Protection with installation/mode/purpose binding,
private owned owner/key artifacts and mandatory actual certificate encryption
for OIDC. Strict bounded JSON/XML, hashes, no-link/owner/mode validation,
old-key retention and read-only validation prevent silently adopting, repairing
or regenerating a ready ring. Warmed providers revalidate disk state before
protect/unprotect. Framework logging is isolated because it can disclose paths,
XML and crypto details. Tests use actual protection/unprotection and framework
rotation, not a replacement cipher or mock principal. Certificate-overlap
operations remain later T083 work.

T017 adds only strict admitted stopped `prepare`, `reconcile-configuration` and
`revoke-sessions` commands. They reuse supervised owned-worker admission and the
exclusive installation scope, with clipped work/acquisition/cleanup inside the
existing 120-second envelope. Full installation preflight precedes changes.
Table-free v0 requires separate registry upgrade. Format-1 preparation commits
format 2, metadata, disabled bootstrap, event and security revision together.
Borrowed `DeadlineStore` wrappers never commit/rollback/dispose the caller's
connection/transaction. Key preparation is a separate filesystem effect,
deliberately completed before that database commit; no distributed atomicity is
claimed. Ready files left by a failed database transaction can be verified on
explicit retry.

A current database can complete its explicitly owned `preparing` ring but cannot
create missing ready keys or adopt an unowned/foreign directory. That boundary
was initially red on both providers and is now covered by permanent command
regressions. Reconciliation explicitly acknowledges changed trust/keys and
advances the epoch, revoking active sessions and pending attempts/authority.
Revocation requires already acknowledged trust. Both preserve user mappings,
consumed first-admin designation, installation ID and registry revision totals.
Direct workers and all stopped commands while a real host lease is held refuse.
The supervisor projects only a validated bounded four-field success receipt;
worker failure/empty stdout is handled through the existing safe exit mapping.
Shared correlated runtime diagnostics/phase projection remain T020.

### PostgreSQL recovery regression: established cause and narrow correction

Published head `d677e82` product run **40426069394** failed integration-postgres,
recovery, contract, privacy and failure-propagation; quality, unit,
integration-sqlite, browser, startup and the separate repository check passed.
Its integration artifact assesses merge revision
`460f22be3e2c461658bca8959bf3201d1f1c57a7`; host PostgreSQL passed and storage
PostgreSQL failed the same two `Corrupt` complete-recovery cases described above.
These are observed failures, not pending checks.

A temporary owned real-PostgreSQL diagnostic reproduced the refusal and captured
source/restored native catalogs privately. The only difference was
`__kofte_module_registrations_module_id_check`: original `BETWEEN` parsing
retained nested `AND` parentheses while actual dump/restore flattened them.
Both checks have identical semantics. PostgreSQL's native
`pg_get_constraintdef(oid, true)` removes this redundant grouping; the entire
source/restored catalogs then compared byte-for-byte equal. The physical verifier
now uses that server deparser and reviewed fresh v1/v2 reference fingerprints.
It still fingerprints full constraint definitions, flags and every other
catalog category; there is no broad parenthesis stripping, omitted constraint,
schema waiver or migration-1 edit. SQLite fingerprints are unchanged.
Temporary diagnostic test source was removed and the shipping test assembly
rebuilt. Existing actual recovery and tamper regressions pass **49/49**.
This fixes the reproduced regression, not every historical CI failure or
unimplemented format-2 recovery. Earlier failed evidence remains unchanged.

### Exact executed checks and red/green evidence

Commands use the existing isolated SDK 10.0.402/runtime 10.0.13; PostgreSQL
commands explicitly set `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`.

| Exact executed command | Result |
| --- | --- |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityEventTests --nologo` | Passed **10**, zero failures/skips. Actual full-capacity warning/refusal, immutable retention, bounded pruning and rollback on both providers. |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityKeysTests --nologo` | Passed **27**, zero failures/skips. Actual certificate encryption, restarts/retained rotation, wrong/missing/foreign/edited keys, links, cancellation and stopped authority. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityPreparationTests --nologo` | Initially **4/6 failed** before commands existed; then **6/6**, expanded **12/12**, and final **14/14 passed**, zero skips. Final cases cover exact consumed designation/mapping, revoked sessions/attempts, lost-key correction, owned partial retry and held real host leases. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~CurrentDatabaseCanResumeItsOwnedPreparingRing --nologo` | Initially **0/2 passed**: current DB could not finish its owned preparing ring. Fixed with existing-only preparation; both are included in final 14/14 above. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~Identity\|FullyQualifiedName~RegistryFormatTests\|FullyQualifiedName~RegistryConstraintTests\|FullyQualifiedName~LeaseLoss' --nologo` | Passed **105**, zero failures/skips, before the deparser correction. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~PostgresRecoveryDiagnosticsTests\|FullyQualifiedName~PostgresRecoveryExercise\|FullyQualifiedName~IdentityFormatTests' --nologo` | Passed **49**, zero failures/skips after the correction: actual backup/restore, private recovery records, both-provider physical/history tampering and preserved populated EF/module state. |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --nologo` | Passed **255**, zero failures/skips; repeated after the final owned-key retry/supervisor correction. |

During implementation, a missing shared diagnostics-helper visibility caused a
compiler failure and was fixed by reusing the existing internal failure mapping.
The first expanded preparation tests attempted internal migration helpers from
the host integration assembly and failed compilation; their setup now uses
independently constructed ADO.NET commands, without new friend-assembly access.
Both failures are resolved, not omitted evidence.

**T001-T017 are complete; T018-T100 (83 tasks) remain unchecked.** Next is the
endpoint/CSRF/fallback-policy foundation, shared diagnostics and identical
read-only prepared serving. No identity HTTP endpoint, working login/session,
enrollment, web journey, recovery-v2, full-matrix pass or acceptance is claimed.
The driver remains the original running implement stage.

After final source/tests and current-progress/operator documentation updates,
`python3 scripts/verify.py --suite quality` passed **all 20 steps**, including
the exact required `python3 scripts/check_repository.py`,
`python3 -m unittest discover -s scripts/tests -v` (**121 passed**) and
`git diff --check`, full solution format/build, locked restores and existing
frontend/dependency checks. No dependency, approved limit or design artifact
changed. This remains a tested partial implementation checkpoint, not a
completed foundation/stage or a claim that latest-head CI has passed.

## In-progress T018–T020: HTTP boundary and correlated private diagnostics

The original **005 / implement** stage, branch, draft PR and reviewed checklists
remain unchanged. T001–T017 are still the only completed tasks. The shared
components below are not yet installed in both actual serving entrypoints;
T018–T020 remain unchecked pending that T021 integration. No production identity
HTTP handler, established session, working login, enrollment, UI or recovery-v2
is delivered by this checkpoint.

The shared boundary admits only exact public protocol routes, retains anonymous
health/Development-only OpenAPI and rejects route aliases, wrong methods,
queries/bodies, Authorization credentials, oversized inputs and unauthorised
anonymous metadata. Default and fallback policies deny unrelated operations;
actor-v1/self access is not first-admin universal permission. POST execution
requires exact configured Origin/Fetch Metadata and actual framework antiforgery
validation. Safe problem JSON has only five fixed fields and generated
correlation; identity/error responses are non-cacheable/no-referrer.
Real Kestrel component tests use framework token pairs and test-owned handlers,
not mock principals or new production endpoints.

The initial component tests were red because production boundary APIs did not
exist. An OIDC-mode test then reproduced HTTP 500 when framework
`CookieSecurePolicy.Always` rejected the approved fixed HTTP backend. The
supported CookieBuilder override now derives the actual Secure flag solely
from validated mode, retaining the Secure/HttpOnly/host-only `__Host-` cookie
without trusting forwarded scheme/host. The actual ingress-hop test passes.
[ADR 0007](../../docs/adr/0007-authentication-bootstrap-boundary.md#implementation-follow-up-fixed-https-ingress-and-private-diagnostics)
records this detail and the official API fallback; Learn/Context7 MCP remain
unavailable. No new dependency or changed production topology is introduced.

Infrastructure diagnostics use strict fixed codes/fields, generated correlation
independent of caller trace identity, at most 1 KiB/event, one terminal outcome,
bounded phase/warning counts and fixed metric dimensions. Sanitized spans share
that generated correlation, without caller baggage. Owner mutations/events,
initialization and key-file effects share the observation's truthful unchanged/
committed/uncertain evidence. Key protection inherits the enclosing deadline
and cancellation instead of starting an uncancellable three-second allowance;
gate waiting is bounded too. A failing terminal sink is not silently retried
or reported successful. No raw exception, credential, query, path, nominee or
profile value becomes a diagnostic field.

Stopped workers now emit the same bounded records rather than the earlier
four-field success receipt. The supervisor rejects unknown/duplicate fields,
wrong context/correlation, malformed/oversized frames and records following the
unique terminal. It projects success only after observed natural exit and
verified cleanup. Missing/malformed/lost acknowledgement remains uncertain.
Real operator tests compare the diagnostic correlation with the committed
database event, distinguish committed preparation from unchanged retry and
preserve the specific key-refusal outcome without printing private paths.
Private composition transport now carries a strict identity-or-composition
union and serializes whole concurrent frame writes. Its identity projection
accepts only validated serve-mode records from the admitted owned worker.
No new identity-specific Platform lifecycle contract is exposed. Actual worker
identity service/sink registration remains T021.

### Reproduced SQLite pre-open contention

The first owner/event/deadline regression run passed 50/51 and reproduced
`Busy` during the managed header read in actual twelve-way SQLite admission.
A deterministic held-header regression also failed with that exact pre-open
refusal. It is before a SQLite handle/transaction or any effect, not an
uncertain commit. DeadlineStore now retries only that classified guard refusal
using existing 25 ms probes and original clipped budget/cleanup reserve.
No file sharing guard, corruption check, transaction replay or deadline changed.
The transient-release, permanent-lock, cancellation, no-late-write and exact
ten-admitted/two-refused SQLite selection then passed **22/22**.

### Actual worker interruption and preserved SQLite journal boundary

Private test-only diagnostic barriers hold the actual admitted maintenance
worker immediately before/after commit. Tests kill that exact reported worker,
await its exit and its supervisor, then inspect the real database and retained
key files. Neither interruption can publish success or automatically replay an
unknown commit. Verified committed preparation retries preserve the one event,
security revision and exact key-file digests.

The first four-case interruption execution passed three: the SQLite
before-commit case incorrectly expected all interrupted state to be immediately
retryable. A repeated diagnostic execution established
`identity_reconciliation_required`, not a missing implementation or quota
failure. SQLite had retained a journal although the database remained format 1.
The accepted read-only physical inventory guard deliberately refuses any such
sidecar. That guard is unchanged: the permanent regression now requires that
specific refusal and byte-for-byte preservation of database/journal/key files.
No journal is deleted, recovery check bypassed or arbitrary failure accepted.
Operator guidance documents the need to inspect preserved recovery state.
This does not claim that every crash residue is automatically recoverable.

The first operator-correlation test compilation also exposed inaccessible
Infrastructure internals and an xUnit assertion diagnostic. The tests now
parse independently expected JSON using public ADO.NET observations, without
new friend-assembly access; both compile errors are resolved.

### Checks executed during this increment

All .NET commands select the existing isolated SDK 10.0.402/runtime 10.0.13.
Both-provider commands set `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`.

| Exact executed command                                                                                                                                                                                                                                                      | Actual result                                                                                                                                                                  |
| --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityAdmissionTests\|FullyQualifiedName~IdentityEventTests\|FullyQualifiedName~IdentityDeadlineTests' --nologo` | Initially **50/51 passed**; SQLite pre-open header-sharing failure reproduced, not waived.                                                                                     |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~TransientHeaderSharingContention --nologo`                                                                          | Initially red with the same `Busy` header-guard stack before the correction.                                                                                                   |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~SqliteIdentityAdmissionTests\|FullyQualifiedName~SqliteIdentityDeadlineTests' --nologo`                            | Passed **22**, zero failures/skips, after the bounded pre-open correction.                                                                                                     |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityDiagnosticsTests\|FullyQualifiedName~CompositionDiagnosticTests\|FullyQualifiedName~IdentityKeysTests' --nologo`                              | Passed **85**, zero failures/skips, including strict transport bounds, mixed concurrent frames, no caller baggage, sink refusal and inherited expired/cancelled key deadlines. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityPreparationTests\|FullyQualifiedName~IdentityPolicyTests' --nologo`                                              | Passed **20**, zero failures/skips, before the later four actual interruption cases were added.                                                                                |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~ActualWorkerTermination --nologo`                                                                                         | Initially **3/4 passed**; established retained-journal refusal described above. Final combined-gate results follow separately.                                                 |

### Published T017-head CI follow-up

Actual pushed head `77feefea5c3d015380f8c45e16e74df8627ca663` now passes
all eleven observed contexts: the separate repository check
([run 40432430645](https://github.com/Stonefly-Labs/Kofte/actions/runs/40432430645))
and quality, unit, integration-sqlite, integration-postgres, recovery, contract,
browser, privacy, startup and failure-propagation
([run 40432430650](https://github.com/Stonefly-Labs/Kofte/actions/runs/40432430650)).
This is new published-head evidence, not a relabeling of historical failures,
a CI result for the current uncommitted increment, a completed implement stage
or proof of unbuilt identity scenarios.

### Combined checkpoint regression results

| Exact executed command                                                                                                                                                                                                                                                                                                                             | Final result                                                                                                                                                        |
| -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~Identity\|FullyQualifiedName~RegistryFormatTests\|FullyQualifiedName~RegistryConstraintTests\|FullyQualifiedName~LeaseLoss\|FullyQualifiedName~MaintenanceObserverFailureTests' --nologo` | Passed **108**, zero failures/skips: both-provider identity/format/constraints, concurrent admission, deadlines, lost authority and rejected worker-output cleanup. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityPreparationTests\|FullyQualifiedName~IdentityPolicyTests\|FullyQualifiedName~HealthEndpointTests' --nologo`                                                                             | Passed **28**, zero failures/skips: all operator/policy cases, four actual interruption cases, and retained actual empty/composed health regressions.               |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --nologo`                                                                                                                                                                                                                                                 | Passed **263**, zero failures/skips.                                                                                                                                |

These are component/foundation regressions, not the remaining T021/T022
serving/foundation gate, independent OIDC journey or the completed implement
stage. Task/checklist markers and driver state remain unchanged.

Final focused publishing checks also passed:
`python3 scripts/check_repository.py`;
`python3 -m unittest discover -s scripts/tests -v` (**121 passed**);
`git diff --check`;
`dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet`;
`dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo`
(zero warnings/errors);
`dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-build --no-restore --nologo`
(**263 passed** again after final diagnostic assertions); and
`npm run format:check`. The changed Markdown was formatted, then historical
implementation evidence was restored byte-for-byte, keeping this report
append-only. Dependencies were not restored/reinstalled and unchanged frontend
runtime/build suites were not rerun for this component/diagnostic-only increment.

## T018-T022: actual serving integration and foundation gate

The same `005 / implement` stage, original branch and draft PR #8 continue.
Requirements **16/16** and quality **36/36** remain read-only review gates.
T018-T022 are now checked, bringing implementation to **22/100**; the stage
itself remains running. No login, session, enrollment or recovery-v2 handler
is mapped merely to obtain a healthy startup.

`IdentityRuntime` is shared by the actual empty and composed serving paths.
It requires explicit validated configuration, exact prepared format 2,
acknowledged installation/mode/trust metadata, usable retained keys and absence
of restored-copy quarantine. Valid v0/v1 receives `identity_prepare_required`.
No startup migration, missing-key regeneration or live discovery is performed.
Configuration rejection has a fixed private correction record even before a
mode can be established. Composition projects the same allowlisted private
identity records through its existing framed union.

The runtime registers the same request policies, persistent protection provider
and owner store against the actual already-held normal SQLite/PostgreSQL host
lease. It acquires no competing host lease and receives no stopped provisioning
authority. Configuration and keys are disposed with their serving lifetime,
including failed startup. Existing storage reconciliation now exposes its
verified installation internally instead of duplicating physical verification.
Unused configuration reload watchers are disabled before builder defaults
create them. Builder creation precedes the existing uncomposed three-second
storage budget; that budget and composition's inherited callback envelopes
are not extended.

Owned serving fixtures now explicitly run stopped registry upgrade/identity
preparation before immutable inventory baselines. Their private configuration
and key roots are outside the recovery data root; production/staging fixtures
use actual certificate-encrypted keys and an unreachable test-only issuer.
Recovery-v1-only scenarios are not blanket-upgraded. A legacy PostgreSQL
platform-only backup still refuses an identity-ready registry rather than
pretending it is a complete recovery set.

Development tooling forwards the explicit configuration path without preparing
user storage. The verification runtime and both browser warm-start fixtures
prepare only their owned installation before measuring serving. The latter
retain exact pre-existing module payloads through empty/composed restarts and
the original readiness/page/total/cleanup limits. Setup guidance documents the
separate commands and owner-only layout.

### Corrections and retained failed evidence

The initial actual-entrypoint tests were red before integration. The first
broad host run passed **65/77**, not all 77. Identity middleware incorrectly
preceded accepted health guards, changing malformed-health error bodies; both
entrypoints now install health guards first, matching the component host.
Other failures occurred during PostgreSQL Docker fixture startup; their exact
engine cause is unestablished. No unrelated shared containers/networks were
removed.

A subsequent serial run passed **76/77**. The remaining 121-second composed
health test parsed identity stderr as composition stdout and captured its
baseline before new explicit preparation. It now prepares before that baseline,
parses only composition stdout and independently validates identity stderr.
Its exact byte-preservation, serving-duration and stop-limit checks remain.
The updated health/startup selection passed **10/12**, including that test,
but both composed production-OIDC cases exhausted their original callback
budgets. The cause was not conclusively diagnosed. A temporary owned-worker
sample did not reproduce the failure and was removed; no probe is shipped.
An unmodified, no-probe eight-case startup run then passed, followed by another
eight-case pass under normal runner scheduling after assigning these actual
startup-envelope measurements to an explicit nonparallel collection. This
controls unrelated fixture scheduling, not product deadlines or assertions.
These results do not prove a hard-real-time guarantee on a contended machine
or erase the earlier timeouts.

The first current SQLite `bounded-stop` recipe exposed one remaining obsolete
empty-stderr assertion in the intentional serving-cleanup failure case. It now
accepts only independently parsed, exact-field, bounded startup/access identity
records; stopped overflow/cancellation cases still require empty stderr.
Both-provider fault/privacy recipes subsequently passed without changing
timeouts, exit checks, canary scans or preserved-storage comparisons.

The changed Python runtime orchestration initially broke a mock that recreated
the store for every setup command. It now models only explicit store init and
checks the ordered upgrade/prepare commands, owner-only configuration and
separate layout. The required 121-test suite passes. An unsupported native
middle-wildcard filter and invalid `startup --provider` invocation were command
selection errors, not executed passing tests; corrected exact commands follow.

### Foundation evidence and task mapping

T022 reuses the already-owned focused test files rather than adding an
`IdentityFoundationTests.cs` aggregator that duplicates their assertions:
`IdentityFormatTests` covers physical/history/constraint tampering and rollback;
`IdentityAdmissionTests` covers checked overflow, exact quotas, foreign/stopped/
lost normal authority and idempotent metadata; `IdentityDeadlineTests` covers
real contention, cancellation and no late writes; `IdentityEventTests` covers
transactional rollback, immutable evidence and capacities.
`IdentityPreparationTests` adds actual admitted worker interruption and stopped
refusal/idempotence, while `IdentityKeysTests` and `IdentityPolicyTests` retain
crypto, ownership, CSRF and default-deny evidence. The serial 77-case host run
executed these host cases; its single unrelated harness failure and correction
are reported above. The new startup matrix checks both actual entrypoints and
providers, retained full storage/key hashes, trust changes, quarantine, missing
keys, cleanup and production serving without provider availability.

All commands below use the existing SDK 10.0.402/runtime 10.0.13; provider
commands opt in with `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`. No dependency
manifest changed and no package restore/install was performed.

| Exact executed command | Result |
| --- | --- |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Passed, zero warnings/errors. |
| `dotnet tests/unit/Kofte.Host.Tests/bin/Release/net10.0/Kofte.Host.Tests.dll -parallelMode none` | Passed **263**, zero failures/skips. |
| `dotnet tests/integration/Kofte.Storage.IntegrationTests/bin/Release/net10.0/Kofte.Storage.IntegrationTests.dll -parallelMode none -class '*IdentityAdmissionTests' -class '*IdentityDeadlineTests' -class '*IdentityEventTests' -class '*IdentityFormatTests'` | Passed **94**, both providers, zero failures/skips. |
| `dotnet tests/integration/Kofte.Storage.IntegrationTests/bin/Release/net10.0/Kofte.Storage.IntegrationTests.dll -parallelMode none -class '*CompositionAuthorityTests' -class '*CompositionProviderTests'` | Passed **25** dependent serving/authority cases, both providers, zero failures/skips. |
| `dotnet tests/integration/Kofte.Host.IntegrationTests/bin/Release/net10.0/Kofte.Host.IntegrationTests.dll -parallelMode none -class '*IdentityStartupTests'` | Passed **8**, both providers/entrypoints/modes, no profiler. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityStartupTests --nologo` | Passed **8** under normal scheduling with the explicit controlled collection, zero failures/skips. |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario private-diagnostics` | Passed **67** assertions, all six capture channels. |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario private-diagnostics` | Passed **67** assertions, all six capture channels. |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario bounded-stop` | Passed **97** assertions, all seven ignoring-token boundaries, actual descendant exit, cancellation, authority and unchanged snapshots. |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario bounded-stop` | Passed **102** assertions, including actual serving-lease loss. |
| `python3 scripts/verify.py --suite startup` | Passed **12** steps, including two empty and two configured browser warm-start cycles with preserved original payloads and unchanged envelopes. |
| `npm run format:check` | Passed. |
| `npm run typecheck --workspace tests/e2e` | Passed. |
| `npm run lint --workspace tests/e2e` | Passed. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed **121** after the explicit-preparation mock correction. |
| `python3 scripts/check_repository.py` | Passed after final progress/task/documentation updates. |
| `git diff --check` | Passed after final progress/task/documentation updates. |

### Published-head CI is separate evidence

The preceding `df95d75` checkpoint's repository run **40437746912** passed.
Product run **40437746923** passed quality, unit, both integration jobs,
browser, startup and recovery when observed. Privacy failed its SQLite public
`private-diagnostics` recipe; failure-propagation failed its PostgreSQL
`bounded-stop` integration baseline before exercising mutations. Their original
underlying causes remain unestablished. Contract was still running when last
queried. The fresh local public recipes above pass, but are not a relabeling of
that CI run, a full failure-propagation execution or a CI result for this new
checkpoint.

**T023-T100 (78 tasks) remain unchecked.** Real OIDC/mapping/attempts/session
commit, enrollment, web journeys, identity recovery and the full final evidence
matrix remain dependent implement work. No converge, review, acceptance or
merge is requested.

## T024/T027: exact transactional user mapping and composition fixture correction

The original **005 / implement** stage remains running on its recorded branch
and draft PR #8. T001–T022, T024 and T027 are complete (**24/100**); all other
task markers and both reviewed checklists are unchanged. No OIDC session,
working login, enrollment, UI, recovery-v2 or later Spec Kit stage is delivered
by this checkpoint.

`IdentityUserStore.ResolveAsync` receives the existing owner mutation; it never
opens or commits a second transaction. It derives mode from verified metadata,
preserves byte-exact issuer/subject keys, rejects invalid UTF-16/control/profile
and byte bounds, and inserts the random local user, unique mapping and required
creation event together. Existing mappings retain ID/mode/creation time and
update display text only. Capacity is 10,000 users across modes; existing users
remain resolvable at capacity. Only the exact mapping-key `ON CONFLICT` target
is reconciled. A real unrelated UNIQUE violation propagates and rolls back,
not an "existing user" fallback. No email, password, provider privilege or
administrator field is introduced.

The shared prepared-state verifier now checks complete user/mapping
correspondence without repairing orphan users. Real SQLite/PostgreSQL cases
cover twelve simultaneous first resolutions, exact issuer-path/subject case
and spaces, equal and changed display/email-shaped profile values, transaction
rollback after mapping creation, ASCII/multibyte 2,048-byte issuers, 255-byte
subjects, 128 astral Unicode scalars, invalid Unicode/controls and one-over
limits. The capacity case seeds 9,999 complete mappings, admits exactly the
10,000th, refuses a new key and still updates that established user.

Three independently started owner processes acquire actual normal host leases,
perform full startup/prepared-state verification and resolve the original key.
They compare exact user ID, creation time, mode, display, consumed designation,
generation/time and immutable creation-event ID. Explicit stopped reconciliation
then changes trusted configuration and mode, keeps distinct replacement/development
users, refuses the wrong development namespace and returns to the original
production user/designation. These are storage-owner process tests, **not an
HTTP/OIDC login or retained-session demonstration**; delivered coordinator/API
evidence remains T023/T025–T026/T028–T040. The worker and dispatcher live only
in the dedicated storage integration test project; no production test route,
fixture switch, new package or migration byte is added.

### Actual red results and corrections

The initial four mapping cases could not compile because `IdentityUser` and
its store did not exist. After implementation they passed **8/8** on the two
providers. The expanded worker initially omitted the required cancellation
token and failed compilation; that test-only error is corrected. The next
expanded run passed **12/14**: both process cases wrongly demanded empty stderr,
although a successful owner mutation emits its required private terminal record.
They now independently require exactly the ten allowed fields, expected fixed
values, generated correlation, committed durability and the 1 KiB bound. No
arbitrary stderr is accepted. The combined storage selection then passed
**108/108**. Subsequent unrelated-UNIQUE, exact-space and multibyte boundary
additions pass the final **18/18** mapping selection.

### Published foundation CI: reproduced preparation omission

Published foundation head `d12fe0d` repository run **40442569234** passed.
Product run **40442569060**, assessing PR merge revision
`687e6708dd5fa3caa90f0371559af089e58cde44`, passed quality, unit, browser, startup
and privacy. Both integration jobs passed their host and non-composition
storage selections, then failed the public `disable-reenable` and
`failure-correction` recipes. Contract additionally failed `ordered-migration`;
recovery reached the same correction failure, and failure-propagation stopped
at its non-green PostgreSQL baseline before mutations.

The exact SQLite `disable-reenable` command reproduced
`composition_test_boot_exited_before_ready`. Source inspection established that
the shared public migration preparation helper never explicitly prepared
identity before its later serving/preservation baselines. It now performs that
owned stopped preparation after the existing module preparation/cleanup proof.
All existing original-ID/data/history/full-inventory, cleanup, correction and
three-cycle assertions remain unchanged. Serving still refuses unprepared
identity; no runtime migration, weakened stderr/deadline/inventory assertion or
complete-set recovery format waiver is introduced. Genuine recovery-v1-only
fixtures and legacy identity-backup refusals remain unchanged.

The focused public contract regression passes **8/8** on both stores, including
the three reproduced recipe families. This closes the reproduced local
omission; it is **not** a relabeling of failed CI, a new full integration/
recovery/failure-propagation run or evidence that a later head's CI passed.

### Exact checkpoint verification

All .NET commands select the existing isolated SDK 10.0.402/runtime 10.0.13.
Real-provider commands set `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`.
No dependency manifest changed and no package restore/install occurred.

| Exact executed command | Result |
| --- | --- |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~Identity --nologo` | Passed **108**, both providers, zero failures/skips, before the final UNIQUE and multibyte additions. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityMappingTests --nologo` | Final **18 passed**, both providers, zero failures/skips; earlier eight- and sixteen-case successes are intermediate evidence only. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~CompositionMigrationContractTests\|FullyQualifiedName~IndependentPublicFailuresRequireTheirNamedCorrectionAndActualOwnedExit' --nologo` | Passed **8**, both providers, zero failures/skips. Actual ordered migration, three disable/re-enable cycles and named failure corrections retain their independent assertions. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityPreparationTests\|FullyQualifiedName~IdentityStartupTests' --nologo` | Passed **28**, both providers and actual empty/composed serving, zero failures/skips. |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Passed, zero warnings/errors. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet` | Passed. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed **121**. |
| `git diff --check` | Passed. |
| `npm run format:check` | Passed. |

**76 tasks remain unchecked.** Continue real OIDC protocol and one-commit
attempt/session/ticket orchestration in this same implement stage. The durable
driver is not completed, accepted or advanced by publishing this checkpoint.

## T023/T026: maintained native OIDC and bounded HTTPS protocol seam

The original **005 / implement** stage remains running on its original branch
and draft PR #8. T001–T024 and T026–T027 are now complete (**26/100**); T025 and
the other 73 remaining tasks are unchecked. Reviewed checklists, approved
intent, migration bytes and the driver stage are unchanged.

`OidcConfiguration.Apply` configures the adopted maintained ASP.NET Core
10.0.13 handler and IdentityModel 8.19.2 validation. It requires confidential
`client_secret_post` code+S256, query callback, exact issuer and single exact
audience, RS256 signature/lifetime with 60-second skew, and native protected
state/correlation/nonce. It clears inbound claim actions/remapping, disables
saved tokens, UserInfo, PAR and telemetry parameters, fixes the callback to
the configured public origin and uses separate Secure/HttpOnly/Lax protocol
cookies. Input admits only scalar standard code/error callbacks with required
state, no duplicate/implicit/mixed fields and an exact response issuer when
provided. The helper is deliberately **not yet registered in product hosts**.

The native handler clears `RequireSignedTokens` for code-flow token responses.
A small `JsonWebTokenHandler` subclass therefore restores that parameter on
its cloned validation input and delegates all cryptographic/token validation
to the maintained implementation. Independently generated `alg:none` and
foreign/RS384 signatures are rejected. No custom JWT authenticator, manually
asserted principal or signature-validation bypass is introduced.

Native ASP.NET Core validates protected state/correlation and clears protocol
state before its protocol-validator call. The protocol validator retains its
framework `RequireStateValidation=false` integration setting; native protected
state is not skipped. Nonce is mandatory. The fixture intercepts
**OnTicketReceived**, after full native validation, not OnTokenValidated,
which occurs before the handler's final nonce/protocol checks.

`OidcBackchannel` requires a current request and allows only the configured
exact HTTPS discovery/JWKS GET and token POST endpoints. Actual DNS answers
and the connected socket peer must belong to configured networks. Standard
TLS hostname/chain validation remains enabled; only the test-owned socket
handler injects trust for its private CA. There is no configuration skip-TLS
flag. Proxy, cookies, automatic redirects/decompression and credentials/
Authorization headers are disabled. HTTP/1.1 non-reused connections and an
explicit per-message connection-attempt guard prevent retry egress.

Request-local accounting allows three backchannel requests, two seconds each,
clipped to three seconds combined and inherited request cancellation.
Responses are streamed with announced/unannounced wire limits, strict UTF-8,
non-duplicate bounded-depth JSON and a separate 16 KiB ID-token cap. No
compressed expansion or response body reflection occurs. Lazy metadata must
match issuer and all configured endpoint URLs and advertise the delivered
code/S256/RS256/client-secret-post flow. Public RSA keys are bounded and
validated; private, malformed, duplicate or excess keys refuse the exchange.

The cache uses monotonic time: fifteen-minute maximum, one admitted refresh
per minute, no background fetch or expired/failed-refresh fallback. A signing
key must still belong to the current unexpired cache at token validation, so
a callback cannot cross the expiry boundary and admit already expired key
material. Unknown-key fallback to every other key is disabled. A required
exchange that cannot fit fails rather than extending the four-second callback.

### Actual independent evidence and limits

`OidcProtocolTests` hosts the real maintained native handler against the owned
HTTPS issuer's independent signing/authorization/token implementation. The
positive exchange observes exact issuer/subject/display only at the
post-protocol seam. Negative cases cover issuer case/trailing slash/audience,
signature/key/algorithm/unsigned/time, missing/wrong nonce, wrong protected
state/correlation/PKCE, malformed/missing/private/duplicate/17th keys, exact
16-key admission, unsolicited/duplicate/mixed/implicit callbacks, preserved
cookie/code replay, provider cancellation/outage/redirect and fresh correction.
An actual disconnected callback propagates cancellation to the provider and
finishes without reaching the ticket seam.

The transport suite exercises actual HTTPS/private-CA rejection, denied
endpoint/network, strict byte/JSON/UTF-8/decompression boundaries, a fourth
request refusal before egress, real per-request/combined cancellation and
controlled exact 59,999/60,000 ms refresh and fifteen-minute cache boundaries.
Expiry and failed revalidation cannot retain an admissible old key.

These are storage-independent native-protocol tests, scheduled under the
existing SQLite trait, **not SQLite/PostgreSQL session or full API evidence**.
The fixture uses test-owned ephemeral Data Protection and intercepts before
framework sign-in: no local user/session/actor or product test endpoint exists
there. It rejects every non-protocol Set-Cookie. Product retained-key,
one-owner-commit/cookie-publication and actual browser/API journeys remain
T025/T028–T040; do not relabel this as a working login or T038 restart proof.

### Actual red results and corrections

The first protocol selection failed compilation because the Login namespace
did not exist. Its first implemented build exposed a test principal-nullability
error. After that correction, the fixture failed because its owned content root
was not created. The next run exposed an empty Cookie header, was stopped
after repeated identical failures, and was corrected to omit absent cookies.
The fixture now explicitly stops its owned Kestrel host within five seconds.
The real positive then passed; the first expanded selection passed **19/19**.
Further protocol and transport cases passed **50/50**, then the expanded
protocol/transport/original-issuer selection passed **62/62**. Actual HTTP
disconnect and unannounced streamed-boundary additions separately passed
**3/3**, yielding **65 distinct passing cases**, not a fabricated single run.
The solution build passed with zero warnings/errors; format verification found
one whitespace error, corrected using the existing formatter and reverified.

Direct source review then found that failed unknown-key refresh could discard
the cache and allow subsequent cold-discovery retries inside its one-minute
refresh bound. The new
`FailedUnknownKeyRefreshCannotTurnIntoUnboundedColdDiscoveryRetries` regression
failed with **no expected exception** and unexpected network activity.
`dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~FailedUnknownKeyRefreshCannotTurnIntoUnboundedColdDiscoveryRetries --nologo`
recorded that actual initial red result. Failed key refresh now preserves its
cooldown and refuses before egress at 59,999 ms, admits revalidation at 60,000 ms,
and consistently uses the cache clock. No expired key is restored. The final
combined selection passed **66/66**, including all prior cases and this
correction; solution build and format verification passed again.

### Exact checkpoint verification

All .NET commands use the existing isolated SDK 10.0.402/runtime 10.0.13. No
package manifest or lock changed; no install or restore occurred.

| Exact executed command | Result |
| --- | --- |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~OidcProtocolTests\|FullyQualifiedName~OidcBackchannelTests\|FullyQualifiedName~OidcIssuerTests' --nologo` | Final **66 passed**, zero failures/skips, 9m47s; earlier **62 passed** before the cancellation/streamed/failed-refresh additions. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~ActualCallbackDisconnectCancelsProvider\|FullyQualifiedName~UnannouncedStreamedBody' --nologo` | **3 passed**, zero failures/skips. |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Passed, zero warnings/errors. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet` | Passed after correcting the reported whitespace. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | **121 passed**. |
| `git diff --check` | Passed. |
| `npm run format:check` | Passed. |

### Published mapping/composition checkpoint CI

Published `cdb19a8` repository run **40445118973** and product run
**40445118966** both completed **success**. All ten product contexts passed:
quality, unit, integration-sqlite, integration-postgres, startup, browser,
recovery, contract, failure-propagation and privacy. This is observed new-head
CI evidence for the preceding mapping/composition checkpoint, not a rewrite
of `d12fe0d` failures or a claim that the new OIDC helper head has passed CI.

Continue the same implement stage with T025 and one-owner-commit attempt/
session/ticket coordination. **74 tasks remain unchecked.** No convergence,
final review, acceptance, ready status or merge is requested.

## T028: durable owner-transaction attempts and protected local binding

The original **005 / implement** stage remains running on its original branch
and draft PR #8. T001–T024 and T026–T028 are complete (**27/100**); 73 tasks
remain unchecked. No stage transition, feature acceptance or merge is requested.

`IdentityLoginAttempts` creates five-minute pending attempts in the caller's
existing owner mutation and returns the actual persisted values on both stores.
It admits at most 256 live pending attempts installation-wide and eight per
browser digest, pruning at most 128 expired/consumed rows. IDs are random UUIDs;
only lowercase SHA-256-shaped browser digests and the exact local return paths
are accepted. No raw protocol credential, state, nonce, verifier or browser
secret is stored. UTC values and stored five-minute correspondence are checked.
The existing user-store ID/UTC parsing is shared without changing its semantics.

Consumption is one compare-and-update matching pending state, browser digest,
mode, trust digest, epoch and the half-open local lifetime. It receives the
same caller-owned mutation as user mapping and never opens or commits a second
transaction. The authenticated session coordinator must call it inside its
future complete login commit; these primitives are deliberately not exposed
through an unfinished product route.

`LoginAttemptBinding` creates framework authentication properties for the
local attempt ID/browser digest. Its reader accepts only canonical nonempty
IDs and an exact constant-time digest match. It does not derive the trusted
return path from incoming properties. The maintained protected-properties
serializer and an actual native OIDC code/PKCE exchange preserve that binding
without exposing it in the authorization URL. The native fixture independently
seeds expected property names and values and still stops before sign-in.
No new friend assembly, public product API, authentication bypass, package,
migration or lock change is introduced.

### Actual red results and corrections

The first attempt test command failed compilation because `IdentityLoginAttempt`
did not exist. Implementation initially missed the storage namespace import
in the shared parsing helper; the corrected combined attempt/mapping selection
passed **34/34**. Expanded races and mode/trust checks pass **40/40** on both
providers: 22 attempt cases plus the existing 18 mapping cases. Tests compare
exact IDs, binding, epoch, mode, times, states and revisions, including eight
concurrent browser starts, 256/257 capacity, exact expiry, bounded cleanup,
twelve simultaneous consumers with one commit, missing/wrong/replayed binding,
changed security context and rollback of consumption/mapping/event together.

Protected-property unit tests were initially red because the binder did not
exist; both now pass. The first native binding-fixture build attempted to expose
an Infrastructure-internal result to Host.IntegrationTests and failed. The
fixture now uses independent framework properties without widening internals.
Its first run reused one mutable challenge-property instance and returned 500
on the second challenge. Copying seed items into fresh per-request properties
corrected the fixture; both actual native positive/binding cases then passed.
The other existing native case passed in that failed run; it was not relabeled
as an all-green result.

### Exact checkpoint verification

Commands select the existing isolated SDK 10.0.402/runtime 10.0.13; both-provider
commands opt in with `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`. No restore or
installation occurred. Required final repository/format checks were executed
after the fixture correction; earlier checkpoint results are not reused.

| Exact executed command | Result |
| --- | --- |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityAttemptTests --nologo` | Initial missing-type compilation failure, before implementation. |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~LoginAttemptBindingTests --nologo` | Initial missing-binder compilation failure, before implementation. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~IdentityAttemptTests\|FullyQualifiedName~IdentityMappingTests' --nologo` | **40 passed**, both providers, zero failures/skips. |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~LoginAttemptBindingTests --nologo` | **2 passed**, zero failures/skips. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~ActualNativeProtectedStateRetainsOnlyTheLocalAttemptAndBrowserBinding\|FullyQualifiedName~NativeHandlerValidatesRealCodePkceAndNonceBeforeTheLocalTicketSeam' --nologo` | Final **2 passed**, zero failures/skips, after the fresh-property correction. |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Passed after the fixture correction, zero warnings/errors. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet` | Passed after the fixture correction. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | **121 passed**. |
| `git diff --check` | Passed. |
| `npm run format:check` | Passed. |

T025/T029/T030 still owe the real session/cookie bridge, one known owner commit
before publication, and uncertain-acknowledgement/publication-failure evidence.
Current component tests are not full product login, stored-session, actor,
browser or retained-session restart evidence.

## T025/T029/T030: atomic native login and durable session bridge

The original **005 / implement** stage remains running on its original branch
and draft PR #8. **T001–T030 are complete (30/100); 70 tasks remain unchecked.**
The historical checkpoints above remain unchanged. No driver completion,
converge, review, feature acceptance or merge is implied.

### Delivered components and actual acceptance evidence

`ValidatedLogin` is called only after the maintained native handler finishes
protocol validation, at `OnTicketReceived`. It verifies the configured exact
issuer/subject shape and protected local browser binding. One existing owner
mutation resolves the exact mapping/user, records required creation evidence,
consumes the durable attempt, and creates the protected minimal session/event.
It preserves the original one-second local work and enclosing callback budget.
Only a returned, known committed mutation can bind the private request-local
reference and call framework sign-in. There is no uncertain-outcome retry.
Existing exact-key UNIQUE reconciliation remains in the mapping helper; the
92-case dependent storage selection includes its eighteen both-provider cases.

`IdentityTicketStore` generates fresh 256-bit base64url references and a
domain-separated SHA-256 digest. The retained encrypted ticket contains only
local UUID/mode and fixed scheme/lifetime properties, not external provider
claims, tokens, roles or the raw reference. Its three framework storage
overloads can return the private committed reference exactly once for the exact
serialized ticket. Unbound, cross-request and reused calls refuse without
writing a second transaction. Retrieval opens fresh owner work, verifies the
protected ticket inside that transaction and sets only request-local current
state after known commit. Corrupt protection rolls back activity renewal and
returns unavailable rather than an anonymous or cached success.

`IdentitySessionStore` enforces eight live sessions/user and 50,000/installation
without eviction, exact thirty-minute idle/eight-hour absolute lifetimes,
renewal clipped to absolute expiry, current user/mode/epoch/designation and
terminal compare-and-update revocation/observed expiry with required events.
It never opens or commits an independent transaction.

The maintained cookie handler normally renews an existing session key during
sign-in. The dedicated cookie manager withholds the older cookie on the exact
new-login paths, so repeat login receives a fresh committed reference.
**Product registration must use this manager and disable native sliding
renewal.** Durable activity renewal happens only during fresh retrieval.
Framework `RemoveAsync` cannot return the confirmed revoke boolean; the later
sign-out endpoint must deliberately distinguish confirmed revocation, absent/
expired session and uncertainty. No sign-out route is claimed here.

The test-owned `LoginCommitFixture` uses actual native OIDC and cookie handlers,
the independent owned HTTPS issuer, real prepared certificate-protected keys,
normal host leases and actual SQLite/PostgreSQL. Its routes are fixture-only;
neither an externally asserted principal nor a product test bypass is added.
Baseline preparation genuinely records `keys_prepared` at security revision 1,
and every exact state/event assertion preserves that baseline.

| Obligation | Evidence in the final thirty-case selection |
| --- | --- |
| T025 one commit before cookie | Exact user/mapping/attempt/session/event IDs, shared revision/correlation and opaque-reference-only cookie; unbound/reused framework stores cannot mutate. |
| T025 failed publication/unknown acknowledgement | Before-commit rollback, after-commit interruption and failed cookie publication yield no session cookie. Four actual callback-owner process-kill cases independently reopen storage after verified owned-child exit. |
| T029 session identity/protection | Independent digest expectation, minimal protected ticket, fresh reference with an existing browser cookie, no persisted raw reference and corrupt-ticket rollback/unavailable response. |
| T029 lifetimes and terminal state | Controlled exact idle and absolute boundaries through actual cookie authentication; repeated activity cannot extend absolute expiry, and concurrent revocation/activity cannot resurrect the session. |
| T029 both capacities | Eight/ninth-user-session refusal preserves earlier sessions/events. A fixture baseline of 49,999 additional valid protected rows reaches exactly 50,000; real next login refuses without eviction, then succeeds after controlled expiry. |
| T030 coordinator | Actual native validated callback invokes the production coordinator/bridge and real framework sign-in; mapping/creation/consumption/session/evidence remain indivisible before publication. |

The global-capacity baseline contains 6,250 complete additional users/mappings,
each with at most eight sessions, and uses actual prepared protection keys.
This is bounded fixture seeding, **not 49,999 OIDC journeys or creation-event
evidence**. Its ninety-second setup budget does not change any product request
budget. Scalar snapshots avoid the operation store's 128-returned-row limit.

The real crash child is admitted by the existing private fixture dispatcher,
uses the original prepared installation and independently acquires the normal
lease/keys. Its private test CA file contains only the owned issuer's public
certificate; hostname and chain verification remain enabled. The parent
verifies the barrier's exact owned PID, kills that process before publication,
observes the genuinely lost HTTP acknowledgement and independently reopens
durable state. No shared Docker resource or unrelated process is terminated.
This is the actual callback owner, not a surrogate transaction.

### Initial red/failed results and corrections

The initial contract command failed compilation before the session/bridge and
coordinator APIs existed. Later fixture additions likewise first failed for
missing probe/clock/crash/capacity APIs. Compilation exposed ambiguous store
lease/path types and missing Hosting imports; explicit aliases/imports corrected
them without widening product access. A nested minimal-handler inference
triggered ASP0016; an explicit `Task` handler replaced it. Existing xUnit
analyzer requirements were followed without suppression.

The first twelve-case actual contract run passed two and failed ten because
its expected revision/event baseline incorrectly assumed preparation left zero.
Source and raw state established the real `keys_prepared`/revision-1 baseline.
Assertions were corrected to preserve that actual baseline; the corrected run
passed twelve. Expanded combined contracts passed 24, then the four real-kill
and two global-capacity cases passed in separate runs. **The final combined
thirty-case run below was executed after all fixture changes**; the earlier
24+4+2 results are not relabeled as that final run.

Initial full format verification found five indentation errors in the new
ticket factory. The existing formatter corrected only that file; full build
and format verification then passed with zero warnings/errors.

### Exact checkpoint verification

Commands select the existing isolated SDK 10.0.402/runtime 10.0.13; both-provider
commands opt in with `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`. No package,
lock, migration, installation or restore change was needed.

| Exact executed command | Result |
| --- | --- |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter FullyQualifiedName~LoginCommitContractTests --nologo` | Initial missing-implementation compile red; initial actual run **2 passed / 10 failed** for the incorrect preparation baseline; corrected **12 passed** before crash expansion. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~LoginCommitContractTests\|FullyQualifiedName~SessionPersistenceContractTests' --nologo` | Earlier combined **24 passed**, before crash/capacity expansion. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter FullyQualifiedName~KillingTheActualNativeCallbackOwner --nologo` | **4 passed**, actual process kills on both providers. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter FullyQualifiedName~InstallationLimitRefusesTheNextSession --nologo` | **2 passed**, actual 50,000/50,001 limit and corrected login after expiry. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~LoginCommitContractTests\|FullyQualifiedName~SessionPersistenceContractTests' --nologo` | **Final 30 passed**, zero failures/skips, 5m33s, after all current source/fixture changes. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~IdentityAdmissionTests\|FullyQualifiedName~IdentityDeadlineTests\|FullyQualifiedName~IdentityEventTests\|FullyQualifiedName~IdentityMappingTests\|FullyQualifiedName~IdentityAttemptTests' --nologo` | **92 passed**, both providers, zero failures/skips, 5m09s. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~OidcIssuerTests\|FullyQualifiedName~ActualNativeProtectedStateRetainsOnlyTheLocalAttemptAndBrowserBinding\|FullyQualifiedName~NativeHandlerValidatesRealCodePkceAndNonceBeforeTheLocalTicketSeam' --nologo` | **4 passed**, zero failures/skips, after shared TLS-fixture changes. |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Passed after all current source additions, zero warnings/errors. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet` | Passed after the targeted formatter correction. |
| `python3 scripts/check_repository.py` | Passed for this checkpoint. |
| `python3 -m unittest discover -s scripts/tests -v` | **121 passed** for this checkpoint. |
| `git diff --check` | Passed for this checkpoint. |
| `npm run format:check` | Passed for this checkpoint. |

### Head-specific CI and remaining work

Published attempts head `591d025` subsequently completed with **all ten product
jobs and repository checks passing**: product run `40451670282`, repository run
`40451670291`. This is not a CI result for the new session/bridge work.
Earlier OIDC head `c8c01c3` product run `40450557540` finished failed:
the PostgreSQL failure-propagation baseline reached its original fifteen-minute
`failure_exercise_timeout`, and `composition-public-contracts` reached its
original fifteen-minute `command_timeout` after platform contracts passed.
Eight other product jobs passed. The underlying causes remain unestablished;
the later green head does not diagnose or erase those failures.

T031 still owes actual product browser-context/login/callback route registration,
CSRF, browser continuity, admission and safe completion. T032 still owes `/me`,
trusted actor projection and generated contracts. The owned bridge host is not
those delivered routes, a module actor consumer, browser UI, full three-restart
T038 evidence, enrollment, sign-out or identity recovery. Continue the same
running stage and original draft PR, without accepting or advancing it.

## T031/T032: registered backend login, self identity and trusted actor

The original **005 / implement** stage continues on its original branch and
draft PR #8. **T001–T032 are complete (32/100); 68 tasks remain unchecked.**
This increment delivers backend endpoints, not the web adapters/UI or complete
US1 journey. No later stage or feature acceptance is inferred.

### Actual delivered behavior

Both empty and composed hosts now register the same maintained native
cookie/OIDC services and exact browser-context, POST login, GET callback and
self-only `/me` endpoints. OIDC is registered only in configured OIDC mode;
the callback has a real exact mapped endpoint so pre-authentication routing
cannot reject it as unknown or broadly open unrelated paths. Only native
post-protocol `OnTicketReceived` invokes the already tested one-owner-commit
coordinator. Deliberate challenge/completion use 303, never an API challenge.
Startup remains offline/read-only: no discovery or implicit preparation.

Browser-context uses framework antiforgery plus a private protected random
browser cookie, mode-specific names/purposes and a five-minute bounded
continuity payload. Login requires exact Origin and a valid framework pair,
accepts only the two form fields, rejects duplicates/unsafe returns and defaults
an omitted return to `/identity`. Only `/` and `/identity` are accepted.
Callbacks additionally require the independent browser digest in protected
native state and the durable pending attempt; native correlation/nonce alone
are insufficient. No raw binding/reference/provider evidence is persisted.
After login, a newly fetched token is bound to the authenticated local identity;
the prior anonymous token cannot authorize a new signed-in mutation.

Shared bounded admission uses installation, browser/session and actual transport
peer digests, never forwarded identity/peer headers. Native callback admission
precedes protocol egress. Cookie input is bounded to sixteen admitted auth
cookies, with duplicate admitted names refused; header/body/target bounds remain.
Kestrel reserves HTTP framing outside the exact sixteen-KiB callback-target
limit, so at-limit requests reach the boundary and one-over requests receive
the safe 400. Identity responses/redirects restore exact no-store/no-cache/
no-referrer headers at response start, including framework-generated responses.
Storage/protection/provider failures remain safe 503 rather than anonymous
success or raw provider output; invalid/replayed binding remains safe 401.

Native authentication must be registered on the **root pipeline**, after the
request/binding/budget middleware. An initial conditional branch caused the
framework to automatically insert native authentication earlier, so the real
callback bypassed the intended request-local budget seam and returned 500.
Restoring explicit root registration fixed the ordering, without an internal
framework flag or an authorization bypass. The cookie manager suppresses
retrieval on health/OpenAPI and new-session publication paths, not POST login:
login must authenticate an existing identity for its current CSRF token, while
callback sign-in must not renew/reuse the old session reference.
Health remains anonymous and does not consult the session store.

Every successful cookie authentication has already verified fresh durable
session/user/mode/epoch/designation. Only that private verified view populates
the read-only request-scoped actor. `/me` requires that actor and returns exactly
version, canonical local user ID, nullable safe display, mode and designation.
Display JSON uses the explicit safe encoder. Fresh profile/designation changes
appear without reissuing a cookie; revocation removes both API identity and
actor on the next request, while corrupt protection returns 503.
A designated administrator still receives 403 for an unrelated ungranted
fixture operation. Fixture profile/designation seeding is **not enrollment
ceremony/endpoint evidence**.

### Shared generated contract and independent consumers

Development OpenAPI now includes the configured mode's delivered identity
operations and explicit self-cookie security alongside unchanged bootstrap-v1
health. All documentation methods remain absent outside Development.
The checked-in Development contract is captured from the actual owned runtime,
normalizing only its unpredictable listener and equivalent empty root security.
The original spec 001 contract remains untouched. `contract:check` now verifies
every original operation/component and version before checking generated types;
identity additions do not require replacing or weakening health definitions.
Object equality is semantic, not JSON property insertion order.
The native parity test independently checks original health definitions,
required self fields and cookie security.

Existing generation produces the shared `CurrentIdentityV1` type. Its separate
runtime guard refuses missing/malformed required fields, zero/noncanonical IDs,
invalid modes/booleans and unsafe/overlong Unicode display values. It admits
additive future response fields without manufacturing defaults. Package changes
are exports/test/typecheck wiring only; dependency versions and locks are unchanged.

Actual capture used the existing `scripts/verify.py` ownership/build/runtime
helpers, with SDK selection as in earlier sections:

```python
import json
import os
from pathlib import Path
from urllib.request import urlopen
import verify
root = Path.cwd()
report = verify.Report(root, 'identity-contract-capture', 'sqlite', 'targeted')
runner = verify.Runner(root, report, dict(os.environ))
try:
    with verify.runtime(runner, 'sqlite', False) as session:
        with urlopen(session.environment['KOFTE_HOST_URL'] + '/openapi/v1.json', timeout=6) as response:
            document = json.load(response)
        document['servers'] = [{'url': 'http://127.0.0.1:5080'}]
        document.setdefault('security', [])
        destination = root / 'packages/bootstrap-contract/openapi.json'
        destination.write_text(json.dumps(document, indent=2, ensure_ascii=False) + '\n')
        print('Captured actual Development health/identity OpenAPI; only owned listener normalized.')
    report.finish(True)
except BaseException:
    report.finish(False)
    raise
```

Executed through `PYTHONPATH=scripts python3` with that exact standard-input
body. All twelve named build/preparation/owned-host/web/start/stop/output-scan
steps passed; no helper remained running or private runtime root was retained.
The actual host and web builds passed, but this capture is not an identity UI test.

### Preserved red/failed results and corrections

The endpoint contract initially failed compilation because product-fixture APIs
did not exist. Fixture extraction initially misplaced a catch block; normal
syntax/format correction restored a zero-warning build. The first actual
twelve endpoint cases passed four and failed eight: framework antiforgery changed
the cache header, and conditional native middleware ran before the explicit
boundary. Exact response-start headers and root authentication corrected both;
expanded sixteen endpoint cases passed, then the combined gate below included
the two exact target-limit cases as well.

The current-user test first returned actual 404 instead of required 401 before
the endpoint existed. The initial typed route hit ASP0016/request-delegate
inference; a real read-only actor dependency made it a typed route handler
without suppression. OpenAPI collection typing was corrected normally.
An expanded twenty-case login/current-user run passed eighteen and failed two
because default HTTP JSON did not HTML-escape display text. An explicit safe
current-user encoder fixed the behavior; the four current-user cases then passed.

The shared TypeScript guard initially failed to import its absent implementation.
Initial contract checking after real capture also failed because native property
insertion order differed, despite Python structural comparison showing every
old operation was identical. Semantic deep equality corrected the checker;
every old operation/component is still required verbatim in meaning.
Generated types, consumer tests, lint and type checking then passed.

### Exact checkpoint verification

All .NET commands use the existing isolated SDK 10.0.402/runtime 10.0.13.
Both-provider commands set `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`.
No install or restore occurred.

| Exact executed command | Result |
| --- | --- |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter FullyQualifiedName~LoginEndpointContractTests --nologo` | Initial missing-fixture compile red; initial **4 passed / 8 failed**, followed by corrected expanded **16 passed**. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter FullyQualifiedName~SqliteCurrentIdentityContractTests --nologo` | Initial actual 404/401 assertion red before endpoint implementation. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~LoginCommitContractTests\|FullyQualifiedName~SessionPersistenceContractTests\|FullyQualifiedName~LoginEndpointContractTests\|FullyQualifiedName~CurrentIdentityContractTests' --nologo` | **52 passed**, both providers, zero failures/skips, 9m23s. Includes eighteen endpoint, four current-identity and thirty earlier bridge/session cases. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter FullyQualifiedName~CurrentIdentityContractTests --nologo` | **Final 4 passed**, after adding the actual designated-but-ungranted 403 assertion; earlier 52-case execution is not relabeled as including that last assertion. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~OpenApiTests\|FullyQualifiedName~IdentityPolicyTests\|FullyQualifiedName~IdentityStartupTests\|FullyQualifiedName~OidcIssuerTests\|FullyQualifiedName~ActualNativeProtectedStateRetainsOnlyTheLocalAttemptAndBrowserBinding\|FullyQualifiedName~NativeHandlerValidatesRealCodePkceAndNonceBeforeTheLocalTicketSeam' --nologo` | **21 passed**, zero failures/skips, actual empty/composed startup on both providers and original issuer/native state regressions. |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Final passed, zero warnings/errors. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet` | Final passed after existing targeted format correction. |
| `npm exec --workspace packages/bootstrap-contract -- vitest run src/identity.test.ts` | Initial missing-implementation import red. |
| `npm run generate --workspace packages/bootstrap-contract` | Passed from captured actual OpenAPI. |
| `npm run contract:check` | Passed after semantic equality correction; all accepted bootstrap operations/components and generated types match. |
| `npm run test:unit --workspace packages/bootstrap-contract` | **4 passed** in two files, including original status tests. |
| `npm run lint --workspace packages/bootstrap-contract` | Passed. |
| `npm run typecheck --workspace packages/bootstrap-contract` | Passed. |
| `npm run lint --workspace apps/web` | Passed. |
| `npm run typecheck --workspace apps/web` | Passed. |
| `npm run format:check` | Passed. |

### CI limitation and remaining scope

The new checkpoint also passed `python3 scripts/check_repository.py`,
`python3 -m unittest discover -s scripts/tests -v` (**121 passed**) and
`git diff --check`. These were executed after its source/docs changes, not
reused from the previous checkpoint.

Published session head `ad21b2b` repository checks passed, but product run
`40455783090` finished **failed**: eight product jobs passed; contract and
failure-propagation failed. Completed job logs were obtained directly from the
GitHub job-logs API while recovery was still running, without printing raw
terminal escapes. `platform-public-contracts` passed in approximately five
minutes before `composition-public-contracts` reached its unchanged fifteen-minute
`command_timeout`. The PostgreSQL failure-propagation baseline reached its
unchanged fifteen-minute `failure_exercise_timeout`, before mutations.
These are not passing new-head checks; underlying causes are unestablished.
Earlier head `591d025` remains an independently observed fully green run,
not a diagnosis or waiver. A bounded current-source execution of the exact
composition selector subsequently passed: **588.596 seconds**, unchanged
900-second limit and minimum expected count of 84, on the local macOS profile.
This is not a reproduction or diagnosis of the failed Linux CI environment.

Next work is T033–T040: fixed same-origin adapters, accessible browser identity,
delivered protocol/deadline/refusal evidence, actual three-restart/API/consumer
and both-provider browser comparison. Enrollment, sign-out, development sessions,
identity recovery and final complete gates remain later tasks. No complete
US1 story, browser identity journey, feature acceptance or merge is claimed.

## T033 and partial T034: bounded Next adapters and runtime verification blocker

The original **005 / implement** stage is now **blocked**, not complete.
**T001–T033 are complete (33/100); 67 tasks remain unchecked.** T034's candidate
source is preserved, but its marker stays unchecked because actual host/Next
parity could not start. No reviewer checklist, approved intent, migration,
dependency/lock file or later stage changed.

### Implemented and focused verification

T033 supplies independently frozen transport expectations, without importing
the production parser. The actual four Next route exports call only fixed
browser-context/login/callback/me destinations. Tests verify local unsupported
credentials and mixed cookie refusal, fixed methods/destinations, stripping
Host/forwarding/identity/unrelated-cookie headers, both cookie namespaces/counts,
individual Set-Cookie with an Expires comma, invalid cookie attributes, manual
redirect destinations, bounded malformed responses, no reflected upstream
errors/extra fields, escaped display, eight-KiB streamed request boundaries,
sixteen-KiB header boundaries and a stalled lost mutation response with no retry.
OIDC-only route absence precedes method/credential handling in development.

Candidate T034 source uses the existing fixed loopback backend derivation.
Node-only routing configuration reads at most 32 KiB from the canonical private
owner file with no-follow/nonblocking open and a verified regular descriptor.
Only public origin/mode/authorization endpoint are projected; client-secret
and key references are not opened. Duplicate/escaped-duplicate configuration
keys, symlinks/public permissions, invalid origins/endpoints and accidental
development mode are independently tested. Host validation/authentication
remains authoritative; this public routing subset is not a second authenticator.

The 4.5-second total adapter deadline covers configuration, body, fetch and
response reading. Both bodies and headers/cookies are byte-bounded, response
cookies remain separate, only configured challenge or exact local completion
redirects are admitted, and all responses are private. Known safe host failures
retain their fixed code/correlation; malformed/lost mutation replies return
uncertain, never a successful session/sign-out/enrollment. No health adapter
behavior changes, dependency, arbitrary proxy or identity UI was introduced.

The first executed adapter command failed module import because the production
implementation did not exist; **zero tests ran**, not a fabricated assertion
red. The first implementation then passed **27 adapter tests**. Lint caught a
missing preserved error cause in configuration error translation; the normal
typed `cause` was retained and remains absent from safe public logs/responses.
Additional boundary/mode/real-file/actual-export cases brought the final focused
source to **41 adapter + 10 configuration tests**, within **78 web unit tests**.
All units, lint, type checking and direct Next production builds pass. Builds
list the four intended dynamic routes alongside unchanged readiness/home routes.
These are not real browser/native-login or live adapter-parity observations.

| Exact executed command | Result |
| --- | --- |
| `npm run test:unit --workspace apps/web -- features/identity/identityAdapter.test.ts` | Initial missing-implementation import failure; zero tests ran. |
| `npm exec -- prettier --write apps/web/features/identity apps/web/app/auth apps/web/app/api/identity/v1` | Passed; existing formatter applied only to new adapter files. |
| `npm run test:unit --workspace apps/web` | Final **78 passed** in five files, including all 51 new adapter/configuration cases and retained bootstrap/readiness tests. |
| `npm run lint --workspace apps/web` | Initial preserved-cause finding corrected; final passed. |
| `npm run typecheck --workspace apps/web` | Passed. |
| `npm run build --workspace apps/web` | Direct builds passed; all four dynamic identity routes compile. |
| `npm run format:check` | Passed. |
| `python3 scripts/check_repository.py` | Passed with the blocked ledger and current docs. |
| `python3 -m unittest discover -s scripts/tests -v` | **121 passed** for this checkpoint. |
| `git diff --check` | Passed. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 84` | Passed through the unchanged owned `verify.Runner`/private XML diagnostics: **588.596s**, original **900s** limit. |

### Actual failed owned-runtime probe: not waived

Two ordinary `verify.Report`/`Runner`/`runtime` SQLite probes attempted actual
prepared host/Next status/header/cookie parity. In both, full host build passed
and the Next build process **exited 0**, but `runtime-web-build` failed with
`verification_input_invalid` during cleanup. Hosting/preparation had not begun:
**none of the sixteen planned HTTP parity observations ran**. Both failed
reports remain under ignored verification artifacts; no live parity or browser
success is claimed.

A separate bounded execution through `verify.OwnedProcesses` reproduced the
post-success exception as **`PermissionError`, errno 1 (`EPERM`) at
`verify.py:450`, `OwnedProcesses.stop`, `os.killpg`**. Only exception type/errno
and source frames were exposed, not raw private output. A no-descendant `true`
child cleaned up normally. Another independently owned Next build also exited
0 and, after checking its exact group had no members, cleaned up successfully.
That later success does not explain or erase the earlier failed cleanups;
an unchanged full runtime retry still failed at the same build-cleanup phase.
No cleanup error was ignored, no owned-process/deadline assertion changed,
and no generic PermissionError catch was added to the existing supervisor.

The driver helper records the actionable blocker: resolve ownership-safe
post-build cleanup without waiving live-descendant checks, rerun the actual
adapter parity probe, finish T034 and then continue T035–T100. The original
draft PR #8 remains the publication checkpoint; no feature acceptance, ready
status, intermediate merge or stage completion is implied.

At the last observation, published route head `f3ef060` had passing repository
checks (`40460031671`) and eight passing product jobs, including PostgreSQL
integration and failure propagation; contract and recovery were still running
in product run `40460031691`. This is not a CI result for the adapter checkpoint,
a diagnosis of the prior timeouts or a completed product matrix.

## T034–T036: verified live adapters and accessible identity entry

The original implement stage was resumed through the driver helper on the same
branch and draft PR. T001–T036 are now complete (36/100); T037–T100 remain
unchecked. This is continuing implementation, not a feature/stage handoff.
Published `f89c1ec` has all eleven CI contexts passing; that observation is not
CI evidence for these newer working-tree changes.

The owned parity probe reproduced the post-success Next-build `EPERM`.
Inspection of that exact group found no members, but the OS timing/cause is
not established. The narrow repair accepts SIGTERM `PermissionError` only
after the leader has exited **and** a signal-zero kernel probe reports the
entire group absent. A live or unverifiable group still fails. Output readers,
budgets, ownership and output scanning are unchanged.

- Initial red: the two new `VerifyTests` selections for absent versus
  present/unverifiable groups produced one error and two subtest failures.
  `python3 -m unittest scripts.tests.test_verify -v` then passed **51 tests**.
- With the normal build and repaired cleanup, the session-owned
  `identity-adapter-runtime.py` passed all thirteen named steps, including
  **sixteen actual host/Next HTTP observations**, cleanup and output scan.
  This owned SQLite/Development probe is adapter parity, not production
  OIDC browser-login evidence.

T035/T036 deliver `/identity` with the existing design primitives, a
non-personal dynamic/no-store shell, parallel bounded fresh context/current-user
checks, an in-memory CSRF token, deliberate native sign-in and only the
reviewed `/` or `/identity` returns. Identity is cleared on failed verification,
expiry, blur/hidden or refresh; returning revalidates it. No session material
is put in JavaScript, storage, SSR or analytics. Fixed callback notices and
validated correlation references never render provider/detail text.
Development is labeled but selection remains T074. Enrollment and sign-out
remain their later tasks.

- Initial UI red was a missing implementation import; no tests ran. Eleven
  state/render cases first passed. A separate safe-reference regression then
  genuinely failed before the validated reference projection was added.
- `npm run test:unit --workspace apps/web` passed **91 tests in six files**,
  including thirteen new identity state/render tests.
- `npm run lint --workspace apps/web`, `npm run typecheck --workspace apps/web`,
  `npm run build --workspace apps/web`, `npm run lint --workspace tests/e2e`,
  `npm run typecheck --workspace tests/e2e` and `npm run format:check` passed.
- `python3 scripts/verify.py --suite browser --provider sqlite` was refused
  by argument validation (exit 2): browser has a fixed provider profile.
  The corrected **`python3 scripts/verify.py --suite browser` passed** all
  nineteen named runtime/build/test/cleanup/scanning steps. Its actual
  discovery includes `identity.spec.ts`: the three new tests observed the
  private non-personal Development shell, real owned host loss/retry within
  five seconds, and 320px/200% reflow, visible keyboard focus and no
  serious/critical axe findings. Existing bootstrap/browser tests also ran.
- `python3 scripts/check_repository.py`,
  `python3 -m unittest discover -s scripts/tests -v` (**123 tests**) and
  `git diff --check` passed before the subsequent .NET test-fixture additions.

The first delivered both-provider `OidcBudgetTests` execution completed with
**52 total, 50 failed, 2 passed, 0 skipped**, in 9m53.783s. All fifty failures
were the test's incorrect `Assert.Empty(events)` baseline: legitimate stopped
preparation already committed `keys_prepared`. The source now compares exact
pre-login event snapshots instead of deleting/ignoring that event. The
corrected matrix and affected crash-commit regressions are executing; neither
T037 nor T038 is claimed complete. The new restart/reconciliation fixture and
tests compiled with `dotnet build Kofte.slnx -c Release --no-restore
-warnaserror --nologo` (zero warnings/errors), but build alone is not their
required real-provider/process evidence. The newer public-recipe source is
also still awaiting its first execution.

## T037–T039 continuation: real Origin correction and unresolved browser lifecycle

The original implement stage continues on its original branch/draft PR.
T001–T036 remain checked (36/100). No browser recipe or later story is claimed
complete, and neither reviewed checklist was changed.

The corrected delivered budget cases exposed a real transport classification
bug: `SocketsHttpHandler` wraps deliberate DNS/network policy refusal in
`HttpRequestException`. The backchannel now preserves that inner policy
classification instead of converting it to provider unavailability. The
optional internal resolver seam defaults to BCL DNS; address/connected-peer
allowlists, cancellation and all original budgets remain intact. Browser-rate
cases now use distinct subjects so the independent eight-session user cap
does not mask the ten-login browser limit. The earlier combined 123-case run
failed six cases; the subsequent targeted run passed 29/30, with a PostgreSQL
browser-context uncertainty. Its unchanged isolated rate case then passed.
A fresh corrected combined protocol/backchannel/budget execution is running;
these partial results are not a full green matrix.

T038's real mapping/restart execution passed twenty cases and sixteen
affected atomic/crash regressions in the preceding continuation. The fixture
uses three fresh actual product-host processes on the retained port, and
stopped provider reconciliation replaces the owned issuer/configuration and
starts a fresh host. This evidence is not substituted for the unfinished
browser recipe or current combined gate.

The first actual SQLite `oidc-login` recipe returned
`identity_browser_opaque_origin_refused`. An owned-ingress boolean observed
the browser's actual `Origin: null` on native POST `/auth/login` under
required `Referrer-Policy: no-referrer`. Null Origin remains refused. The
correction is a Next Server Action in `startLoginAction.ts`, invoked by the
sign-in form's explicit client handler. It admits exactly the two bounded
inputs, forwards actual Origin/credential/Fetch Metadata evidence through the
existing fixed-backend adapter, retains explicit framework antiforgery and
the adapter's 4.5-second limit, publishes validated separate cookies through
typed Next APIs and redirects only after that publication. The framework
redirect is outside catches. The owned HTTPS ingress preserves public Host
for Next's own Origin/Host verification; this never selects the backend.
Pending, refusal and uncertain outcomes are visible, with no automatic
mutation retry or duplicate submission. No authentication contract,
authorization authority, privacy header or TLS/CSRF policy is relaxed.

- `npm run test:unit --workspace apps/web` passed **98 tests in seven files**,
  including the five new action cases and two added submission-state cases.
- `npm run typecheck --workspace apps/web`,
  `npm run lint --workspace apps/web` and
  `npm run build --workspace apps/web` passed. The two web quality commands
  and all 98 units were re-run successfully after the transport edits.
- `npm run typecheck --workspace tests/e2e`,
  `npm run lint --workspace tests/e2e` and `npm run format:check` passed
  again after the read-only cleanup-verification refinement.
- `dotnet build Kofte.slnx -c Release --no-restore -warnaserror --nologo`
  passed again on the final candidate with **zero warnings/errors**.
- `python3 scripts/check_repository.py`,
  `python3 -m unittest discover -s scripts/tests -v` (**123 tests**) and
  `git diff --check` passed before this documentation append.
  `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet`
  initially reported two whitespace findings in changed fixture code.
  A scoped `dotnet format whitespace` correction and full verification passed.
  Full formatting verification, the repository checker and whitespace check
  passed again after the final fixture/documentation edits.

After the Origin correction, real local HTTPS runs reached product
login/callback/current-user, and some reached every safe-return assertion.
**Every complete public recipe still failed.** A runner-only explanation was
initially suspected but is not established: later standalone/native runs also
hung or failed during browser work/closure. A native bounded browser script
now retains the actual assertions and emits only fixed stages/codes/source
locations, never raw browser errors, URLs, cookies or provider payloads.

The speculative force-group/Python-probe supervisor was removed. The fixture
uses normal Playwright close plus read-only, CDP-bound exact-group absence
verification. Present groups and errors other than kernel `ESRCH` fail; no
`EPERM`, `?E` state or elapsed timeout is treated as absence. A real local
owned group retained a PPID-1 `?E` member and both Node/Python signal-zero
probes returned `EPERM`; its cause/quiescence is not established. No cleanup
success is claimed for that group. Failed/unconfirmed browser completion
retains the owned private configuration/profile rather than deleting it.
The distinct earlier Next-build repair still requires an exited leader and
actual kernel absence.

Instead of more unbounded local hypotheses, the existing Ubuntu browser job
now prepares the already-pinned disposable PostgreSQL image and executes:

```bash
dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario oidc-login --provider sqlite
dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario trust-refusal --provider sqlite
dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario oidc-login --provider postgres
dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario trust-refusal --provider postgres
```

These are **new pending executions**, not passing CI or T039/T093 completion.
The job keeps ordinary certificate verification and imports only the owned
CA into its fresh profile; no OS trust, personal profile, certificate-ignore
switch or private artifact upload is introduced. All eleven checks on
published `f89c1ec` passed, but are not evidence for this newer source.

## `6be0ee8` reconciliation: real browser recipes pass; host CI still fails

The verified entry/transport and test candidates were committed and immediately
pushed as `6be0ee8` on the original draft PR #8. No stage transition,
acceptance or merge occurred.

- The complete corrected local command
  `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --no-build --filter 'FullyQualifiedName~OidcBudgetTests|FullyQualifiedName~OidcProtocolTests|FullyQualifiedName~OidcBackchannelTests' --nologo`
  passed **123/123, zero skips**, in **20m08.394s**. Earlier failures are retained.
- Actual [Ubuntu browser job 122614125497](https://github.com/Stonefly-Labs/Kofte/actions/runs/40626971026/job/122614125497)
  passed both named browser verification and the added HTTPS recipe step.
  The completed job's four JSON result records were independently retrieved
  and checked for the exact provider/scenario inventory, passed status and
  distinct named assertion count. `oidc-login` passed **eight observations**
  and `trust-refusal` **eleven observations** on **each** store. These are
  actual direct API/actor/native-handler/Next/browser/TLS/cleanup executions,
  not fabricated principals or an empty summary.
- `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --no-build --filter 'FullyQualifiedName~IdentityPolicyTests' --nologo`
  passed **4/4**, zero skips, in **33.349s**: safe health/missing auth,
  default denial, exact Origin/framework pair and callback binding remain.
- The fresh mapping command
  `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --no-build --filter 'FullyQualifiedName~IdentityMappingTests' --nologo`
  initially passed **19/20**, with SQLite's delivered restart case timing out
  at worker readiness. A separate policy selection overlapped that run;
  causation is **not established**. Safe last-status/transport classification
  was added without changing the existing fifteen-second budget. The exact
  isolated SQLite restart selector then passed **1/1** in **27.790s**.
  The entire twenty-case command, run alone on the current fixture, then
  passed **20/20**, zero skips, in **1m50.205s**. This verifies T038, including
  distinct process IDs/profile/issuer preservation, but does not diagnose or
  waive the prior failed run.

Current markers are T001–T036 plus T038 (**37/100**). T037's local matrix is
green, and actual T039 recipe evidence exists, but their final reconciliation
is held while current-head PostgreSQL host integration CI fails. The safe
report identifies only `integration-postgres`, exit 2, after 245.211s; it does
not identify the failing test. No cause is invented. Other observed passed
contexts include repository, quality, unit, SQLite integration, browser,
privacy and startup; the complete run is not yet green.

The existing source-whitelisted private XML diagnostic path previously
covered storage and public-contract tests but **not host integration**.
It now includes host integration with the same bounded parsing, private-file
cleanup and no raw argument/exception/output retention. The new focused
regression first failed because the host diagnostic was `None`, then passed
after wiring; complete Python discovery passed **124 tests**. The real native
runner was also exercised through `Runner.command` with
`dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*IdentityPolicyTests' --minimum-expected-tests 4`;
it passed with private XML enabled. Full .NET formatting verification,
repository and whitespace checks passed. These diagnostics are to establish
the CI cause, not a claimed repair of the PostgreSQL failure.

### Isolated diagnostic propagation and completed predecessor results

The host diagnostic wiring was committed and immediately pushed as `2f56f35`.
Its repository checks passed, and its browser, startup, quality and unit jobs
passed when observed; the PostgreSQL suite and other longer jobs were still
running. Results from `6be0ee8` are not attributed to this head.

The completed `6be0ee8` product run additionally failed
`composition-public-contracts` at its unchanged **900.044-second** command
limit and the isolated `baseline-postgres` at **900.371 seconds**. The latter
raised `failure_exercise_timeout`; no completed nested report identified the
unfinished operation. The preceding `f89c1ec` product run passed every job.
These failures remain undiagnosed; no product, command or harness deadline
is relaxed, and no failing baseline is accepted as propagation evidence.

Inspection found a directly dependent reporting gap: the isolated harness
still revalidated only storage/contract diagnostics and silently omitted the
new safe host fields. A new focused regression first failed with
`KeyError: failedHostTests`; admitting the same source-known host kind fixes
that omission. It also rejects argument-bearing/duplicate test names,
unknown codes and diagnostics attached to an unrelated command, retaining no
raw canary. `python3 -m unittest discover -s scripts/tests -v` then passed
**125 tests** at that intermediate edit. This repairs reporting only, not the
underlying CI failures; the implement stage remains unfinished.

## T037–T040: US1 reconciled at `2f56f35`

The fresh [product run 40628696184](https://github.com/Stonefly-Labs/Kofte/actions/runs/40628696184)
passed both complete provider integration jobs and browser, privacy, startup,
quality and unit. Repository checks also passed. PostgreSQL's native host,
storage and composition commands passed in **241.783s**, **618.981s** and
**290.846s**, respectively, with their original per-command limits. This is
fresh passing evidence, **not a diagnosis** of `6be0ee8`'s failed host case.

The completed [browser job 122619718900](https://github.com/Stonefly-Labs/Kofte/actions/runs/40628696184/job/122619718900)
again ran all four exact public commands listed above. Its actual JSON records
were retrieved and independently checked for schema/status, exact provider/
scenario inventory and unique named observation counts: **eight login** and
**eleven trust-refusal** observations on **each** store. No raw job output,
credential or browser profile is published.

| Requirement | Actual scoped evidence |
| --- | --- |
| US1.1 | Both actual HTTPS login recipes compare browser UI, native `/me` and separately compiled actor consumer against the same internal user. Ordinary login grants no designation. |
| US1.2 | The current twenty-case mapping matrix verifies exact IDs/designation/profile through three distinct product process restarts per store. |
| US1.3 | That matrix covers equal profiles/distinct exact subjects and sequential explicitly reconciled issuers without identity merging or extra grants. |
| US1.4 | The corrected 123-case local protocol/backchannel/budget gate and both actual trust-refusal recipes verify invalid/replayed/unsolicited evidence, no unauthorized effects and fresh corrected login. |
| US1.5 | Native/configuration/startup and delivered budget cases verify outage/cancellation/configuration refusal, safe retry and no development/anonymous fallback. |
| SC-001, partial | Production sign-in, exact identity and mapping/restart are evidenced; sign-out is still US3 work. |
| SC-003, scoped | Tested missing/mixed credentials, invalid trust, origin/CSRF and default-denied access refuse before effects; later enrollment/expiry/parity cases remain. |
| SC-005, preliminary | The Ubuntu browser suite executes 320px/200% shell, keyboard/focus/axe and actual host-loss actionable failure under five seconds. Production recipes assert signed-in state within five seconds of submission, including the synthetic provider exchange; this is not a separate provider-return measurement or evidence for future UI states. |

Safe health/missing-auth remain verified by the recorded fresh four-case policy
selection and repeated actual recipe observations. T037, T039 and T040 are now
checked, giving **40/100**; enrollment is next within the same implement stage.
The read-only review checklists and approved design remain unchanged.

The complete current run is **not green**. Composition public contracts again
hit the unchanged 900-second limit (**900.077s**). PostgreSQL composition
recovery failed `PublicFaultMatrixVerifiesActualExitBudgetsPartialCleanupAndOwnedDescendants`,
with safe `composition_consumer_failed` / `bounded-stop` evidence. Neither
cause is established or waived.

The current failure-propagation failure **is diagnosed**: the unavailable-store
native command correctly failed, but the strict evidence comparison rejected
the newly appended private XML-report arguments. A focused regression first
reproduced `failure_did_not_reach_expected_command`; the correction admits only
the exact bounded reporting suffix, retaining rejection of wrong provider,
filename or option and all original step/exit/correlation checks. The actual
published nested report now replays successfully as the intended failure.
Full Python discovery passes **126 tests**. Its three real baselines passed,
including PostgreSQL in **895.064s**; the earlier timeout is not erased.
No product or verification deadline changed, and a fresh complete CI run is
still required. The local macOS cleanup and intervening readiness failures
also remain explicit limitations, not a full-feature acceptance.

## US2: nominated enrollment and real interruption evidence

The same **005 / implement / running** stage, branch and draft PR #8 continue.
Only T042, T049, T050 and T052 are newly checked: their required both-provider
authority/file, twenty-contender and real child-interruption observations pass.
The other US2 markers remain conservative while the complete delivered-boundary
matrix, unavailable-administrator/module-data and browser evidence are unfinished.
All approved task text/dependencies, reviewer checklists and driver state remain
unchanged. This is **44/100**, not a completed story or feature.

### Delivered behavior

`IdentityBootstrapStore` validates the complete singleton/null-state contract,
performs stopped pending nomination/replacement/disable, and binds the SHA-256
authorization digest to installation, checked generation and exact issuer/subject.
The private operator command generates 32 random bytes, exclusively creates an
owner-only file, durably flushes it before DB commit and retains partial/delivered
evidence after uncertainty. Live, foreign, unadmitted, consumed and
recovery-locked operations cannot reach delivery or reopen authority.

Protected enrollment revalidates the current durable session/epoch inside the
owner transaction, compares the persisted exact nominee and digest in constant
time, then commits designation/consumption/revision/immutable event together.
It erases pending token/nominee fields, never takes an actor/role from input and
reports success only after known commit. All twenty HTTP contenders pass real
CSRF/admission: exactly one 200, nineteen 409, with exact designated user,
generation, event correlation and revision correspondence. Three fresh process
restarts and idempotent actual registry upgrade/identity prepare preserve this
consumption; the broader T051 unavailable-administrator/module-data case remains.

The exact Next route preserves the protected exchange and safe receipt, with no
redirect following or mutation retry. The masked form never prefills/persists
authorization, clears it before dispatch, aborts on unmount and suppresses late
results for another identity. Refusal restores entry focus; confirmation/lost
reply triggers fresh `/me` and restores heading focus, without declaring unknown
commit as confirmed. Actual native OpenAPI now includes the strict write-only
43-character request, security/headers, receipt and safe conflict; generated
types preserve original health operations/components.

### Real interruption defect and correction

The four private barriers stop the actual maintenance child after file creation,
after durable flush, before DB commit and after durable commit. Ownership is
proved by the admitted barrier PID, actual PPID and matching process group;
the exact tracked child is killed and both child/supervisor exits are verified.
Independent reopening checks each file/DB side, retained evidence and explicit
replacement. Enrollment barriers similarly kill the exact serving child before
or after durable commit and before acknowledgement: neither-or-one atomic state/
event, no acknowledged success, and safe corrected retry.

The pre-commit SQLite operator interruption genuinely reproduced
`identity_reconciliation_required`: startup's inventory check reused the clean
backup opener, which refuses an inactive SQLite crash journal. Startup now uses
its already-open verified platform connection for the same inventory checks.
Clean backup/recovery readers still open through their original sidecar-refusing
path. No journal deletion, backup format relaxation or time-budget change occurs.
The following full 36-case operator matrix and four-case enrollment crash matrix
pass on both stores; all fifteen retained recovery tests additionally pass.

Private-file effects also exposed an invalid premature ready/uncertain trace.
Two genuine red cases now pass: intermediate phases describe uncertainty without
relaxing terminal ready/uncertain rejection. The final known commit may still
report committed; loss/refusal retains truthful uncertain evidence.

### Exact executed evidence and retained failures

Every native command uses SDK 10.0.402/runtime 10.0.13; both-provider commands
explicitly opt into the disposable fixture. Test commands below use the existing
safe runner's private native capture, not public failure XML/provider payload.

| Exact command / selection | Actual result |
| --- | --- |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Passed, zero warnings/errors, including public/browser recipe wiring. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore` | Passed. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*IdentityNominationTests' --minimum-expected-tests 36` | Final full **36 passed**, both providers. Initial foreign extra-fixture startup, SQLite journal and test-only 200ms PostgreSQL acquisition failures are retained; the final live-lease test uses the original normal two-second acquisition, not an increased deadline. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*EnrollmentCrashTests' --minimum-expected-tests 4` | Full **4 passed**, both providers. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*EnrollmentConcurrencyTests' --minimum-expected-tests 2` | **2 passed**, twenty protected eligible contenders and three fresh restarts per provider. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentTests' --minimum-expected-tests 16` | Full **16 passed**, including actual stopped replacement and ticket/commit verification 503. Earlier full both-store 32-case selection timed out at its unchanged 240s command bound; it did not pass. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*RecoverySetTests' --minimum-expected-tests 15` | **15 passed**, retaining clean-backup sidecar refusal after the startup correction. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*OpenApiTests' --minimum-expected-tests 3 --filter-trait Provider=sqlite` | **3 passed**, actual generation/full baseline and absence outside Development. |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore --no-build -c Release --filter-class '*IdentityDiagnosticsTests' --filter-class '*NominationDeliveryTests' --minimum-expected-tests 7` | **7 passed**. |
| `npm run generate --workspace packages/bootstrap-contract` and `npm run contract:check` | Passed after extracting the actual native document, preserving every original health operation. Initial extraction failed because xUnit escapes the public JSON; prior npm success against the old JSON is not generation proof. |
| `npm run test:unit --workspace apps/web -- features/identity` | **82 passed**. |
| `npm run typecheck --workspace apps/web`, `npm run lint --workspace apps/web`, `npm run build --workspace apps/web` | All passed. |
| `npm run typecheck --workspace tests/e2e`, `npm run lint --workspace tests/e2e` | Both passed. Initial Prettier failure for the new browser source was corrected with the existing formatter. |
| `python3 scripts/check_repository.py`, `python3 -m unittest discover -s scripts/tests -v`, `git diff --check` | All passed; Python discovery **126 tests**. |
| `npm run format:check`, `npm run contract:check` | Final full formatting and generated-contract checks passed. |
| `dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario nominated-enrollment --provider sqlite` | Native-only version passed **15 named observations** after an earlier generic failure with unestablished cause. The subsequently expanded actual-browser version **fails**, ending at fixed `identity_browser_stage_submitted`, before enrollment. |
| `dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario enrollment-race --provider sqlite` | Passed its protected twenty-way native race, exact event/state and three-restart observations. |

The new HTTPS enrollment browser checks deliberately enter a wrong authorization,
then drop acknowledgement only after the actual protected commit. They require
fresh designation/no retry, cleared secret/storage, keyboard/focus, 320px/200%,
axe and original five-second UI bounds. They are wired into the existing Ubuntu
browser job alongside individual both-provider public recipes, but are **not
yet passing evidence**: local browser execution stalls before enrollment.
The other required public scenarios and `all` remain fail-closed/unimplemented.

The unavailable-commit selection passed SQLite's two cases, but both PostgreSQL
cases failed during owned Docker startup, not the enrollment test body.
Read-only inspection and an explicitly unique task-owned network-create probe
established **`docker_network_pool_exhausted`** with a responsive engine,
32 networks and 21 running fixture-labeled containers. Labels alone do not prove
this session owns all resources. The probe created nothing and no unrelated/
global cleanup was attempted. Fresh owned CI is required for the missing matrix;
do not reduce contention, extend deadlines or declare PostgreSQL passed.

For preceding committed `06a9faf`, repository checks pass; product run
[40631199021](https://github.com/Stonefly-Labs/Kofte/actions/runs/40631199021)
passed eight jobs but failed contract at the existing 900s composition-public
bound and SQLite composition privacy with safe cancellation/consumer-failure
codes. Neither cause is diagnosed/fixed by enrollment. Those failures do not
validate current source, and earlier failed/readiness/macOS evidence is retained.

**Remaining:** complete US2 matrix and browser evidence, US3 sign-out/session/
actor parity, US4 actual development selection/isolation, US5 recovery-v2/
quarantine/reconciliation, then full local/CI gates and evidence-based review.
No stage transition, human acceptance, PR readiness or merge is requested.

### T051 continuation: populated module and unavailable administrator

`EnrollmentRestartTests` now uses an owned pre-serving fixture callback to
register/migrate a real module through the accepted stopped registry/binding
APIs and persist a nonempty sentinel. No test module code moves to production.
Twenty protected eligible submissions admit exactly one success and nineteen
conflicts with the populated module present, then three distinct serving processes
and actual idempotent registry upgrade/identity prepare preserve exact module
assignment, row/note, EF history, registry revision, user/mapping and immutable
enrollment record.

The original issuer is actually disposed and explicitly reconciled to a fresh
issuer. The prior session is denied; the replacement issuer's ordinary
authenticated user cannot inherit designation, replay the original authorization
or reopen bootstrap with the actual stopped nomination command. The original
consumed user/generation/event and unrelated module remain intact.

The initial test could not compile: the new owned preparation callback was not
implemented, and two test namespace imports were missing. After those fixes,
`dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` passes
without warnings and the exact command
`dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentRestartTests' --minimum-expected-tests 1`
passes its actual SQLite case. No PostgreSQL execution is claimed while local
Docker fixture capacity is exhausted. T051 remains unchecked until that
counterpart passes; the total remains **44/100**.

Extending the passing single-enrollment case to the required twenty simultaneous
submissions initially failed with the runner's sanitized `command_failed`; its
failure report identifies this test but does not establish a cause. The unchanged
case then passed one private diagnostic execution and three further sequential
isolated executions. These later passes are not a diagnosis or claim that a
timing defect was fixed. No contender count or deadline was reduced.

The pushed `68b483f` repository check passes. Its product run `40640319468`
reports failed host cases for SQLite interrupted preparation, PostgreSQL
twenty-way enrollment and PostgreSQL invalid OIDC evidence. The browser suite
itself passes, but the subsequent configured-HTTPS public-recipe step fails;
the complete run's logs are not yet available. These are retained failures, not
passing evidence for this follow-up, and their cause remains under investigation.

### CI continuation: crash-retry compatibility and independent evidence

The completed `68b483f` product run `40640319468` passes quality, unit, privacy,
startup and recovery, but fails host integration on each provider, configured
HTTPS recipes, composition contracts at the original 900-second deadline and
the PostgreSQL failure-propagation baseline. The repository run passes.
`e1e28c6` publishes the populated-module preservation regression on the same
draft PR; its separate repository run passes, and its product run remains
incomplete. Neither run contains the following corrections.

The SQLite preparation crash test reproducibly failed `Assert.NotEqual` at its
old line 59 because explicit stopped retry now correctly succeeds through the
verified existing connection, instead of treating SQLite's inactive crash
journal as a clean backup. Correcting that obsolete expectation does not relax
backup admission: both actual pre/post-commit interruption cases pass with one
event, revision 1, disabled bootstrap, zero users, format 2 and byte-identical
retained keys. All fifteen clean-recovery regressions still pass.

The hosted browser's ordinary SQLite login and trust-refusal recipes pass.
The nominated-enrollment recipe reaches the real form and fails its wrong-secret
refusal announcement assertion at original source line 237. Locally, an owned
HTTPS observation confirms callback 303, current identity/context 200 and actual
wrong-secret enrollment 409; no raw response, credentials, query, path, nominee
or profile is projected. Other local runs expire during browser setup or sign-in.
The original browser-child 45-second bound is unchanged.

The browser recipe now explicitly restores focus after axe's temporary analysis
page, rechecks the signed-in view and independently asserts the real 409
before its UI assertion. Failed UI assertions project only an allowlisted
missing/uncertain/unexpected/unavailable notice category; diagnostic reporting
does not initiate another browser request. Owned fixture diagnostics expose
only protocol request counts, fixed route-category status integers and whether
the child deadline expired. These changes are not yet a passing browser result
or a diagnosis of the remaining refusal-announcement failure.

The host failure also prevented either independent storage integration project
from running, withholding PostgreSQL preservation evidence. Verification now
attempts all three independent projects after a normal failed/zero-case or timed
out child whose cleanup completed, retaining each failed report and rethrowing
the first failure. Cleanup/ownership/tooling failures still stop immediately.
Two focused Python regressions cover continued execution with a nonzero suite
and immediate stop on cleanup failure; the focused 54-test verifier suite passes.
`python3 scripts/check_repository.py`, the full
`python3 -m unittest discover -s scripts/tests -v` (128 tests),
`dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo`,
`dotnet format Kofte.slnx --verify-no-changes --no-restore`, browser-workspace
typecheck/lint, `npm run format:check` and `git diff --check` all pass.
No test count, original timeout, provider requirement or success condition is
weakened. All task markers remain unchanged at **44/100**.

## US2 continuation: scoped announcements and real lost acknowledgement

The published `d3813dd` SQLite integration artifact `12796593019` reports
**passed** for all three independent projects: host integration (210.543s),
storage integration (310.011s) and composition integration (100.495s).
The earlier host failure no longer prevents collecting storage evidence.
This is an assessed-head result, not evidence for the following unpushed edits.
The same head's PostgreSQL artifact `12796914363` fails host integration only
at the twenty-contender test and storage integration only at the populated
twenty-contender/restart test; composition integration passes. Those are the
two timing-sensitive cases receiving collection isolation below. Their failures
are retained, not counted as PostgreSQL preservation evidence. Other final
gates remain outstanding.

The installed Next.js accessibility guide documents its built-in route
announcer; installed source places its accessible alert in an open shadow root.
Both enrollment alerts now resolve within the named **Your identity** region,
instead of the page-wide alert locator. Actual execution then passed the
wrong-secret announcement, secret clearing and focus assertions and reached
the old `route.fetch` interception at source line 240, where it failed before
a successful enrollment response. No product announcement defect was demonstrated.
The speculative matcher-result classifier and its public error allowlist are
removed; fixed source-line and protocol observations remain.

Acknowledgement loss now happens in the **test-owned HTTPS ingress**, after
the real browser's POST has received the adapter's successful, bounded response.
The ingress consumes that response, drops the connection once and records only
that the intended drop occurred. The owning fixture independently requires
this observation before reporting browser success. This avoids replacing the
browser's trusted transport with Playwright's separate API-request transport;
no certificate bypass, retry or production fault-injection interface is added.
The browser must still observe uncertainty, query fresh designation, submit
exactly twice including the prior refusal, clear secret material and satisfy
keyboard/focus, width/text, axe and five-second UI assertions.

One local execution reached and passed all enrollment assertions and the
subsequent safe-return assertions, recording actual enrollment 200, but exceeded
the original 45-second child limit during final browser cleanup. It is
**not a passing complete recipe**. The dedicated enrollment branch now returns
after its own required journey; duplicate repeated-login/safe-return checks
remain in the separately required ordinary-login recipe. Subsequent local
executions still fail earlier in sign-in or native setup. No total deadline,
TLS trust check, cleanup requirement or enrollment success condition is relaxed.
Fresh hosted proof remains required.

The twenty-contender host tests now share the existing exclusive startup
collection, renamed `identity-deadlines`; populated-module storage tests use
the existing storage deadline collection. Their **twenty simultaneous protected
submissions**, one-second production operations and restart assertions are
unchanged. This removes unrelated test scheduling from their timing evidence,
but is not a diagnosis of the earlier PostgreSQL race failure.

Executed after these changes:

| Exact command | Result |
| --- | --- |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Passed, zero warnings/errors. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentConcurrencyTests' --minimum-expected-tests 1` | Passed through the owned verifier. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentRestartTests' --minimum-expected-tests 1` | Owned-verifier attempt failed; unchanged private xUnit diagnostic rerun passed, 38.5s. No intermittent-failure cause is claimed. |
| `dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario nominated-enrollment --provider sqlite` | Still failing overall: preserved old interception failure, final cleanup timeout after complete assertions, and earlier sign-in/native-setup failures. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore` | Passed. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | All 128 passed. |
| `npm run typecheck --workspace tests/e2e`; `npm run lint --workspace tests/e2e`; `npm run format:check`; `git diff --check` | All passed after targeted Prettier correction. |

An initial combined native class selector using `|` failed before executing the
intended selection; the subsequent individual supported selectors above are
the actual evidence. All 44 checked task markers, reviewer-owned checklists and
the implement/running driver state remain unchanged. Continue implementation
on the original branch/draft PR, without a feature-completion or merge claim.

## US2 verified browser/preservation and remaining negative evidence

Assessed correction head `a0c1fb030ba4c94a7d3a5f817c4d08d901e309d2`
has a **passing complete Ubuntu browser job** in product run
`40645635458`, job `122673679859`. All eight actual HTTPS recipe commands pass:

| Exact consumer scenario arguments | SQLite | PostgreSQL |
| --- | --- | --- |
| `--scenario oidc-login --provider PROVIDER` | Passed, 8 named observations. | Passed, 8 named observations. |
| `--scenario trust-refusal --provider PROVIDER` | Passed, 11 named observations. | Passed, 11 named observations. |
| `--scenario nominated-enrollment --provider PROVIDER` | Passed, 17 named observations. | Passed, 17 named observations. |
| `--scenario enrollment-race --provider PROVIDER` | Passed, 15 named observations. | Passed, 15 named observations. |

Each command uses
`dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll`
with the exact arguments above and the named provider substituted. The native
examples observe stopped private nomination, exact atomic designation/event,
three distinct fresh processes, replay refusal, actual repeated preparation and
denied unrelated action. The nominated browser independently completes the
wrong-secret refusal, real commit with deliberately lost ingress acknowledgement,
fresh identity/designation, exactly two submissions/no automatic retry, secret
and browser-storage checks, keyboard/focus, 320px/200%, axe and original UI bound.
The fixture confirms the intended acknowledgement was actually lost and owned
browser cleanup completes. These are passing **hosted** results; earlier local
failures remain failures and their causes are not erased.

The same head's complete PostgreSQL integration artifact `12797783473` passes
all three commands: host integration **234.704s**, storage integration
**583.371s**, composition integration **225.140s**. In particular, both prior
twenty-contender failures now pass in their existing exclusive collections.
Together with SQLite preservation evidence, this closes **T051**: exact
populated-module assignment/data/EF history, consumed generation/designated
user/immutable event, original authorization replay, three fresh processes,
repeated prepare and actual administrator-issuer loss/reconciliation.
The provider-loss limitation still has no reset/reassignment backdoor.

The same head's SQLite integration artifact `12797976924` passes host
integration (216.816s) and composition integration (101.985s), but storage
integration fails (313.438s) only at the enrollment crash regression. This is
not a complete green feature gate. Crash tests now also use the existing
exclusive deadline collection, retaining both real pre/post-commit kills,
their original readiness/operation budgets and all exact durability assertions.
A local diagnostic run still fails both at child readiness; a subsequent
unchanged run passes both in 33.7s. No scheduling or platform root cause is
claimed from those passes. The PostgreSQL failure-propagation baseline also
retains its 900.315s `failure_exercise_timeout`.

### Real development actor and independently signed authority claims

The independent issuer can now include a verified-email claim, Administrator
role, administrator-shaped boolean and administrator display text in an
otherwise valid signed ID token. Existing no-ceremony and exact-nominee matrix
cases use this evidence: it cannot designate an initial administrator or
change the nominated identity. The focused eight-case SQLite selection passes.
No product identity/authorization logic changed.

The test-owned host can also explicitly prepare its normal **Development**
configuration and create a real development user, separate namespaced mapping,
protected durable minimal session and native framework cookie. The new
negative test proves both fresh `/me` and the public actor report that actual
development user, then submits with its legitimate identity-bound CSRF pair.
Enrollment returns non-enumerating **403**, preserving exact bootstrap,
users/mappings/events and registry revision. This is not a forged JSON mode,
mocked principal or positive development-login endpoint/UI claim. Creation is
a fixture-only owner operation, not a deployed route or test bypass in product.
Default OIDC fixture behavior stays unchanged; no OIDC backchannel is registered
for development configuration.

Executed on this increment with the existing isolated SDK:

| Exact command | Result |
| --- | --- |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Passed, zero warnings/errors. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentTests' --filter-method '*OrdinaryProductionLogin*' --filter-method '*EnrollmentChecksCurrentCeremony*' --minimum-expected-tests 8` | All eight passed with independent signed authority/profile/email claims. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentTests' --filter-method '*ARealStoredDevelopmentSession*' --minimum-expected-tests 1` | Passed: real development actor/session, 403 and exact unchanged protected state. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentTests' --minimum-expected-tests 17` | Sixteen passed; existing stopped-replacement case failed. Not an all-green matrix. |
| Same host project with `--filter-class '*SqliteEnrollmentTests' --filter-method '*ActualStoppedReplacement*' --minimum-expected-tests 1` | Unchanged private xUnit diagnostic rerun passed, 19.9s. Across those selections all seventeen cases pass; the full-run failure is retained, not diagnosed. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentCrashTests' --minimum-expected-tests 2` | Initial private readiness failures; unchanged diagnostic rerun passes both real commit-boundary cases, 33.7s. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore` | Passed. |
| `python3 scripts/check_repository.py`; `python3 -m unittest discover -s scripts/tests -v`; `git diff --check` | Passed; 128 Python tests. |

**51/100 tasks are now complete:** T001–T047 and T049–T052. T041 now
includes real development-session and independently signed profile/authority
negatives; T043–T047's delivered core is reconciled with real hosted browser
proof, and T051 has actual both-provider populated-module/admin-loss evidence.
**T048 remains unchecked** until the expanded negative matrix executes on
PostgreSQL. T053–T055 still retain their prerequisite/focused-gate obligations,
despite passing individual public/browser recipes. Complete those final US2
checks, then continue US3 sign-out/session parity, US4 development login and
US5 recovery/final evidence. Driver remains implement/running; no acceptance,
readiness or merge request is made.

## US2 completion: expanded both-provider gates

Published head `f80eee96f17d3c8fe5f2e02ae1f4312ce3c14d50`, product run
`40648526486`, now passes both complete integration jobs and the complete
eight-recipe HTTPS browser job. Artifacts `12799266406` (SQLite) and
`12798554310` (PostgreSQL) independently record:

| Provider   | Host integration | Storage integration | Composition integration |
| ---------- | ---------------- | ------------------- | ----------------------- |
| SQLite     | Passed, 196.885s | Passed, 302.515s    | Passed, 80.191s         |
| PostgreSQL | Passed, 243.245s | Passed, 625.939s    | Passed, 225.411s        |

This includes each store's expanded seventeen-case enrollment matrix,
independently signed authority/profile/email negatives, actual stored
development-actor refusal, full twenty-contender and populated-module restart/
administrator-loss cases, and both actual crash boundaries. No cases,
contention levels, failure requirements or original deadlines were removed.
The local/intervening failures remain recorded; passing hosted execution does
not establish their root causes.

The same head's browser job `122682897465` passes all eight required individual
recipes, retaining the already inspected named observations, real lost
acknowledgement/fresh designation/no retry and owned cleanup. Aggregate `all`
remains an explicit final-gate obligation, not claimed executed here.
Existing generated contracts/component tests and fifteen focused strict
clean-recovery regressions remain the unchanged focused evidence above.
T048 and the prerequisite-gated T053–T055 are now complete: **T001–T055,
55/100**. The production/fixture implementation may consolidate the proposed
test filenames while retaining the specified assertions.

US2.1 traces to the disabled-ceremony and independently signed-claim negatives;
US2.2 to real stopped nomination, native protected completion and exact atomic
user/generation/event; US2.3 to the full identity/mode/CSRF/expiry/replacement/
replay matrices and non-enumerating fixed receipts; US2.4 to both twenty-way
races, owned pre/post-commit kills and browser lost acknowledgement; US2.5 to
three fresh hosts, actual repeated prepare, retained populated module and
unavailable-issuer reconciliation without reopened enrollment. Initial
designation still grants no unrelated operation, and administrator/IdP loss
has no reset shortcut.

The whole feature is not complete. Final run conclusions for this head are
success for quality, unit, both integration jobs, recovery, privacy, browser
and startup; contract and failure-propagation fail. These failures remain
unresolved, not waived. Continue **US3 durable sign-out/session parity** on the
same implement stage/branch/draft PR; do not advance to converge or request
acceptance/merge.

## US3 increment: durable sign-out and live session-boundary evidence

The delivered protected POST `/api/identity/v1/sign-out` admits only the
current session, exact Origin/CSRF pair and empty input. Revocation and its
session event commit in one owner transaction; 204 requires known completion,
invalid/expired sessions receive 401 and unavailable/uncertain work receives
a safe 503. Cookie deletion precedes response-body publication, including
concurrent-refusal paths. It deliberately does not call native `SignOutAsync`
after explicit revocation, which would invoke ticket-store removal and mutate
the owner a second time. No external provider logout is attempted.

The exact Next adapter validates empty 204, propagates individual deletion
cookies and never retries a mutation. The UI immediately removes personal and
enrollment content, distinguishes confirmed/expired/uncertain results, refreshes
browser continuity and requires deliberate login for re-entry. Focus, polling
and explicit identity-check retries cannot silently restore a locally cleared
identity after uncertain sign-out. The original 4.5-second adapter/UI and
one-second local operation limits are unchanged.

A new real-cookie/warmed-key regression initially failed:
`contract-sqlite-dcd00f7b1d8b448aaeec6c65c7d410f1.json`, 11.692s, with only
`LosingWarmedKeysIsUnavailableRatherThanAnAnonymousOrCachedIdentity` failing;
the real stopped-epoch revocation case passed. The .NET 10.0.13
`SecureDataFormat.Unprotect` catches decryption exceptions and returns null,
hiding the existing key guard's failure. A narrow wrapper around the
framework-created cookie format now verifies the retained ring outside that
catch, before and after decoding. Missing keys return safe 503 for both `/me`
and the actor probe, without cached identity, regenerated keys, session/event
mutation or loss of user/mapping state. Forged-cookie decoding remains owned
by the framework.

Actual local evidence on macOS arm64, unchanged pinned tools:

- The initial sign-out positive was red before route delivery. After delivery,
  fourteen SQLite cases passed, then three expiry/race cases passed. The
  expanded nineteen-case selection now passes as one command:
  `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*SqliteSignOutContractTests' --minimum-expected-tests 19`.
  Safe report `contract-sqlite-3fdc056ee9fb4ed2b2dec10d995541ad.json`,
  170.553s. This includes exact idle/absolute expiry, renewal/revocation,
  protected negatives, actual before/after-commit failures, warmed key loss
  and stopped epoch revocation.
- The subsequently added simultaneous-sign-out regression passes separately:
  the same command with `--filter-method '*ConcurrentSignOuts*'` and
  `--minimum-expected-tests 1`; safe report
  `contract-sqlite-0bc6105454e5420ba0af7c49bd248c8e.json`.
  Thus twenty cases are evidenced in two selections, not a claimed combined
  twenty-case rerun. PostgreSQL execution remains pending.
- The complete adapter/IdentityPanel selection passes 77 tests; web and browser
  type/lint checks and the production web build pass. Regenerated OpenAPI/types
  and `npm run contract:check` pass; three actual native OpenAPI publication/
  refusal cases pass. The full Release solution builds with zero warnings.

The separately compiled consumer now supplies the actual test-host actor
callback, checks ungranted-handler nonexecution and implements `session-boundary`.
Its HTTPS browser branch covers controlled idle/absolute boundaries, sixteen
real renewals, protected sign-out, lost acknowledgement, same-cookie direct/
adapted next-request denial, two users, no-store SSR, focus and accessibility.
Twenty private owned zero-byte file phases control only the test fixture;
there is no production clock/login/TLS bypass or control endpoint. The owned
ingress drops only the armed successful sign-out acknowledgement, and the
parent independently requires that observation.

This comprehensive browser evidence is **not green**. The first run failed
after 48.465s with only `identity_fixture_browser_failed` retained. Follow-up
safe phase capture (`browser-sqlite-3809c82f75c84f63b4756f36ea5292ec.json`)
shows the fixed 45-second child deadline, exit 137, last marker
`identity_browser_stage_trust_ready`, zero authorization/token requests and
zero session-control phases. The ingress observed anonymous identity page/
browser context 200 and `/me` 401. This establishes failure before sign-in,
not a key/sign-out root cause or proof that macOS itself is responsible.
Additional fixed phase markers distinguish activation/navigation/body progress;
both SQLite and PostgreSQL recipes are now required by the existing hosted
browser job. No deadline, TLS verification or cleanup requirement was relaxed.

**Progress remains T001–T055, 55/100.** T056–T070 retain full parity/corpus,
rotation/corruption, both-provider and successful public/browser prerequisites.
T071–T100 retain development, operational/recovery and final obligations.
Publishing this increment is not stage completion, acceptance or a merge request.

## US3 follow-up: hosted session proof and actual HTTPS parity

Published `2852371c1fad2d93d0dac3b703a47ae6e3592f6b`, product run
`40655171161`, browser job `122704340561`, passes **all ten** required HTTPS
recipes: five scenarios on each real provider. Completed job logs were
captured privately and independently checked against the exact provider,
scenario and ordered observation lists, without publishing private redirects
or tokens. Both `session-boundary` executions pass nineteen named observations,
including the ordinary and designated separately compiled actor comparisons,
ungranted-action denial, renewal/sign-out race, next-request denial and full
browser expiry/lost-acknowledgement/two-user flow. This supersedes pending
hosted evidence, not the retained local pre-login failure or its unknown cause.

The same head's repository run `40655170711` passes. At the latest observation,
quality, unit, both complete provider integrations, privacy, browser and startup
pass; recovery, contract and failure-propagation are still running.
Neither the full product matrix nor current uncommitted follow-up is called
green from that observation.

### Further independent boundary regressions

The fixture now sends to either native hosting or its fixed owned HTTPS
ingress, which runs the actual production-built Next routes and native host.
It uses the issuer-trusting HTTP client with manual cookies/no redirects;
it rejects arbitrary absolute adapter targets. No browser launch, mock fetch,
test principal, production TLS bypass or production probe is introduced.

Four SQLite parity cases now run together and pass:

- The same real cookie yields exact ordinary and freshly designated identity
  through native `/me`, HTTPS Next `/me` and the separately compiled actor.
  Actual stopped nomination and protected adapted enrollment/sign-out preserve
  the exact user, mapping, consumed ceremony and terminal events.
- The actual independently signed ID token emitted during successful native
  login is refused as an API bearer credential, with and without the cookie.
  Basic credentials, spoofed identity/proxy headers, body/query identity,
  incorrect method and missing/null/foreign Origin cannot authorize effects.
  The token remains memory-only and is never included in public evidence.
- Removing warmed retained keys returns fixed private 503, not anonymous 401
  or cached identity, through both native and actual HTTPS Next requests.
- Unknown identity and version paths return empty private 404 for GET, POST,
  HEAD and OPTIONS, with no redirect/cookie or owner-state changes. The first
  actual test was red (`contract-sqlite-b9bba7297e6444fd88ad9dcfdc340c04.json`).
  A fixed deny-only Next `[...unsupported]` handler now preserves this boundary;
  it never forwards, authenticates or reads configuration. Existing exact
  routes take precedence. The production web build and complete four-case
  selection pass after that correction.

Two additional native SQLite session cases also pass: the original cookie
survives ordinary retained-key rotation across two distinct fresh worker
processes, retaining two keys and exact identity/epoch/events; and a genuine
stored Development cookie cannot authenticate production, even when renamed
and accompanied by spoofed mode/administrator headers. The original valid
production cookie continues to identify its original ordinary user. These are
not the positive Development-login endpoint or complete persisted-mode
transition required later.

The key-aging simulation extracts the existing test-only unit helper, preserves
key material/ID and updates only the stopped fixture's dates/owner digest.
All twenty-seven original key units still pass. No operator repair API or
serving-time key regeneration was added.

### Executed verification and retained failures

All native commands use isolated SDK 10.0.402/runtime 10.0.13. Focused native
test commands run through the existing owned `verify.Runner` with private
xUnit XML and safe failure-name projection. No install or restore occurred.

| Exact command | Result |
| --- | --- |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityParityTests' --minimum-expected-tests 4` | Final four passed together, safe report `contract-sqlite-49a13d272e0144adb4835611c7d3e589.json`. |
| Same project/class with `--filter-method '*ActualNativeAndHttpsAdapterRefuse*' --minimum-expected-tests 1` | Passed after using the actual issued signed ID token, report `contract-sqlite-a87ce792d8da49d297f3b143001e6b8f.json`, 11.371s. |
| Same project with `--filter-class '*SqliteSignOutContractTests' --filter-method '*ActualDevelopmentCookie*' --minimum-expected-tests 1` | Passed, report `contract-sqlite-fbcfaab23fcf4684b8cf61780e829aa7.json`, 5.424s. |
| Original three SQLite parity cases, actual worker-rotation case and twenty-seven key units | Passed in report `contract-sqlite-87abc730a4fc43f2bf780cf67f38aa52.json`, 29.908s; not a claimed combined twenty-two-session-case run. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*PostgresIdentityParityTests' --filter-method '*WarmedIdentityCannotBecome*' --minimum-expected-tests 1` | Failed, report `contract-postgres-efc8e7e2004648ea96f9461b9e75be35.json`, 2.612s, with explicit disposable admission. No new PostgreSQL parity success or exact cause is established. Earlier local network-pool exhaustion remains recorded; no shared resources were pruned. |
| `dotnet build tests/contract/Kofte.Platform.ContractTests --no-restore -c Release -warnaserror --nologo` | Passed, zero warnings/errors. |
| `dotnet format Kofte.slnx --no-restore --verify-no-changes --verbosity minimal` | Passed. |
| `npm run build --workspace apps/web` | Passed with the exact identity routes and deny-only unsupported handler. |
| `npm run typecheck --workspace apps/web`; `npm run lint --workspace apps/web`; `npm run format:check` | Passed. |
| `python3 scripts/check_repository.py`; `python3 -m unittest discover -s scripts/tests -v`; `git diff --check` | Passed; 128 Python tests. |

The initial parity selection failed only its incorrectly spelled expected
enrollment event (`initial_admin_enrolled`); the test was corrected to the
fixed contract's `initial_administrator_enrolled`, then passed. Extracting the
shared fixture initially missed a unit-test namespace import; normal import
correction restored the full solution build. Neither initial failure is a
product outage or passing evidence. The earlier red unknown-route selection
and actual local/hosted failures remain retained.

**Progress remains T001–T055, 55/100.** Full independent TypeScript corpus,
remaining request/session/composed-host coverage and both-provider evidence
still gate T056–T070. Continue implementation on the original branch/draft PR;
there is no stage transition, feature acceptance or merge request.
