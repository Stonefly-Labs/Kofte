# Implementation Evidence: 005 - Authentication and Bootstrap Administration

**Result:** implement in progress; deadline integration resolved and T001-T014 complete (14/100), no working login yet.
**Stage / date:** implement, 2026-10-26.
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
