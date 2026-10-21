# Implementation Evidence: 002

**Stage:** implement, blocked on T045; not feature acceptance.
**Branch:** `feature/002-platform-contracts`
**PR:** [draft #5](https://github.com/Stonefly-Labs/Kofte/pull/5)

## Entry gates

The worktree was clean and HEAD matched its remote at
`e7087b6e75ed6aa032163d548a64ee29b9c625fa`. Updated `origin/main` is
an ancestor. The original feature PR is open/draft. The driver began implement
once, using `SPECIFY_FEATURE_DIRECTORY=specs/002-platform-contracts`.
The prerequisite helper resolved that exact directory and all design artifacts.
Analysis passes with no unresolved finding; ADR 0004 is Accepted with the
recorded decision **"adr 0004 is approved"**.

| Read-only checklist | Total | Checked | Unchecked | Result |
| --- | --- | --- | --- | --- |
| requirements.md | 16 | 16 | 0 | pass |
| quality.md | 36 | 36 | 0 | pass |

No `.specify/extensions.yml` exists; no implementation hooks are configured.
The existing ignores cover .NET/Node/build/test/runtime artifacts. No package
is published and no new Docker, Helm or Terraform surface requires an ignore.

## Scope and evidence obligations

Implement only the pure library, public author/host-facing consumers, schema
parity and fail-closed verification. Host startup, endpoints, stores, UI and
reserved future modules/SDKs remain unchanged. Test source belongs under tests.
No parser input is executed, resolved or treated as permission/storage authority.

T044 originally required an unavailable Ubuntu x64 2-vCPU/8-GiB machine.
The human explicitly removed that profile during this implementation; Accepted
[ADR 0004](../../docs/adr/0004-module-manifest-contract.md#human-directed-benchmark-amendment)
records the exact direction. Actual supported-workstation timing now satisfies
T044 without relabeling the machine or relaxing individual bounded checks.
T045 requires an unfamiliar human's timed author exercise; autopilot and
implementer self-review do not satisfy it. Final implementation remains gated on that proof
and the subsequent complete local/current-head CI and coverage reconciliation.

## Commands and checkpoints

Evidence is appended after execution, never inferred from planned commands.

- T001-T003: project setup, generated/reviewed locks, then
  `dotnet restore Kofte.slnx --locked-mode` passed (seven projects).
  `npm run dependencies:check` passed with unchanged 403 npm/69 NuGet identities.
  Pinned SDK/Node/npm were selected from existing isolated installations,
  not replaced with the older shell defaults.
- Result-envelope tests were written first; the initial unit command failed
  compilation because public types did not yet exist (expected red checkpoint).
  The public corpus, immutable result/model foundations and bounded author
  subprocess harness were prepared then; complete public proof followed below.
- The test-owned author adapter exposes optional bounded proof switches
  `--culture` and `--repeat` (1-100). Defaults retain the documented single
  result. This allows actual author-process culture and 100 decisions per input
  to be observed without thousands of cold-process launches. Neither switch
  supplies context or changes validation rules.

## Implemented work and focused results

T001-T044 are implemented and validated as available work; T045-T048 remain
unchecked. All commands used pinned SDK 10.0.402/runtime 10.0.13, Node 24.22.0
and npm 11.19.1 selected in PATH. Assessed source is the working-tree increment
from the entry revision; its publication commit contains this report and ledger.
There is no claim that the entry revision already contained this source.
Final assessed Git tree objects (reproducible with `git write-tree --prefix=PATH/`
after staging those unchanged paths):
`src/Kofte.Platform/` = `b071d5726b86cacf2d00cb303a72261a510a0289`;
`tests/contract/Kofte.Platform.ContractTests/` =
`9c811f25c69a5e691039de58ba705165e598b595`.
These identify the exact production and public/timing proof bytes independently
of the publication commit's evidence-document hash.

| Exact command | Observed result |
| --- | --- |
| `dotnet restore Kofte.slnx --locked-mode` | pass; seven projects, reviewed unchanged external graph |
| `npm run dependencies:check` | pass; 403 npm / 69 NuGet identities; no new license/provenance obligations |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror` | pass; zero warnings/errors |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore` | pass in named quality gate after formatting |
| `dotnet test --project tests/unit/Kofte.Platform.Tests --no-restore -c Release --minimum-expected-tests 1` | pass; final 129 tests, zero failed/skipped |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --minimum-expected-tests 1` | pass; final 15 tests, zero failed/skipped |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --configuration Release -- --minimum-expected-tests 15` | pass; all 15 tests after strengthening both-consumer ordering and exact 32/33-diagnostic retry proof, zero failed/skipped |
| `npm run test --workspace tests/contract -- module-manifest.test.ts` | pass; three structural tests with independent case loops |
| `npm run typecheck --workspace tests/contract` | pass |
| `npm run lint --workspace tests/contract` | pass |
| `python3 -m unittest scripts.tests.test_verify -v` | pass; 36 tests, including new inventory/child-failure/timeout/mandatory-selection regressions |
| `python3 scripts/verify.py --suite unit` | pass; existing host/web/contract/Python and new platform unit steps |
| `python3 scripts/verify.py --suite contract --provider both --disposable-postgres` | pass; public .NET/schema checks plus real SQLite/PostgreSQL native OpenAPI and HTTP/adapter fixtures; 34 named steps |
| `python3 scripts/check_repository.py` | pass |
| `python3 -m unittest discover -s scripts/tests -v` | pass; 103 tests |
| `git diff --check` | pass |

The initial result test was red before production types existed. The inventory
regression was red before verifier wiring existed. Initial build issues in raw
test literals, xUnit 4's parallelization/output-helper/cancellation APIs were
corrected, not suppressed. The first all-suite attempt found Prettier differences
in two new contract-project lock files; their JSON formatting was corrected
without changing package identities/edges. No failed attempt is called passing.
The next all-suite attempt exposed whitespace in the strengthened public test;
the existing .NET formatter corrected it and its verify-only command passed.
That attempt also recorded a privacy-suite `runtime-host-build` failure during
overlapping local builds; the safe runner did not retain raw child output, so no
unverified root cause is asserted. The non-overlapping final gate below supersedes
both failed attempts.
Final source review also found that keys inside an unknown object could
incorrectly regain a declared path, misclassifying an unknown nested collection
or locating its duplicate at a nonexistent declared child. The reader now tracks
recognized-path provenance separately from safe diagnostic paths. A focused unit
regression and two independent public/culture/retry corpus variants prove the
correction. Final focused tests and all 100 timings below were rerun afterward.

Actual zero-test proof:
`dotnet test --project tests/unit/Kofte.Platform.Tests --no-restore -c Release --filter-class '*NoSuchManifestTest*' --minimum-expected-tests 1`
returned **8**, reporting zero selected tests and minimum-policy violation.
The named runner preserves child failure/timeout as nonzero safe reports.
Missing any required platform project, consumer or proof source fails
`platform-inventory` before tools; the author build precedes contract execution.
The existing product CI matrix invokes the same named verifier, so no workflow
change or CI-only implementation was necessary.

### Source and public-contract checkpoints

| Tasks | Delivered evidence |
| --- | --- |
| T001-T009 | Entry/design/checklist gates, seven-project graph and reviewed locks; immutable disjoint result/model; independently expected C01-C34 variant/range corpus, original copied fixtures, bounded subprocess harness and evidence matrix. |
| T010-T015 / US2.1-3 | Canonical ASCII ID with exact 63/64 and 253/254 limits, ordinal equality, alias/whitespace/Unicode/path rejection; explicit prior-ID comparison; same ID across display/release changes; immutable arrays, declaration collections and retained results. Public consumers exercise identity/update/limit cases, not helper-only proof. |
| T016-T023 / US3.1-4 | Full bounded SemVer with overflow-free numeric text, build-insensitive precedence and independent prerelease chain; exact/two-bound grammar, interval/inclusive/prerelease boundary table; explicit missing/invalid host/prior context; all nine kinds and supported/duplicate/unknown/unsupported versions; dependency malformed/duplicate/self rejection and valid absence; local permission/storage metadata. |
| T024-T032 / US1.1-4 | Bounded original-byte forward reader before document materialization; strict UTF-8/scalar/BOM/decoded-key/depth/collection checks; closed typed shapes, schema early stop, applicable semantic/context checks then compatibility; complete public descriptions only on success. Both public consumers check every independent corpus variant; schema uses exact original spec file offline and preserves structural/semantic divergence. |
| T031 / SC-003 | Complete corpus **100 repetitions per consumer per real en-US/tr-TR culture**. Host-facing culture is set/asserted; author process independently constructs/asserts real culture with fixed name/decimal confirmation. Every individual author result is parsed/compared, not just a final status. Reordered objects, context-before-manifest and numeric indices preserve exact ordered identities. |
| T033-T040 / US4.1-3 | Exact/one-over byte/depth/collection/string/32-33-diagnostic recipes; fixed simultaneous fatal/schema/shape/semantic/compatibility precedence; unpredictable transient secret/control canaries; no library output; safe author accepted/rejected/invalid-invocation/actual unreadable-input IO exits; corrected public/in-process retry and retained immutable results; initialized per-call local timing. T037's final path-provenance correction preserves limits and safe known-parent diagnostics; no production-rule relaxation or unrelated T038 change. |
| T041-T043 | Required file/project inventory, mandatory nonempty .NET selections, ordered author build, safe named nonzero failures/timeouts and retained original provider/browser suites. README/contributor/architecture/workflow/dependency/local/contract docs and executable guide now match actual behavior. |

The author and host-facing public consumers call only `ManifestValidator`.
The host's only source change is its project reference; contract tests inspect
that direction. The author project has one library reference and no package,
host or internals reference. No `InternalsVisibleTo`, linked production test
source, new package/runtime SDK, HTTP/UI/MCP route or host startup call was added.

### Side-effect and privacy method

Initialized calls ran with an owned non-writable fixture directory containing
a hashed settings sentinel, a bound loopback TCP request observer and a host
assembly-load observer. Root `$ref`, hook/type/assembly, physical schema/file/
path/connection/credential attempts rejected through actual calls/author
processes. Observed **zero pending network connections, forbidden assembly
activations, new fixture/module files or sentinel changes**. Only the owned
sentinel/directory/listener/processes were cleaned up. Unexpected observer/
cleanup/test errors fail; no personal/production state was touched.

Source/dependency review corroborates the observations: the production library
contains pure input/JSON/collection/version/diagnostic code, no IO, socket/DNS,
process, assembly loading, environment, telemetry, host/ASP.NET/EF or database
calls. Its referenced-assembly check excludes host/EF/ASP.NET/IO/network.
There is no registry/schema allocator or credential/settings service to invoke.
These bounded observations plus source review are **not a sandbox guarantee**
for arbitrary trusted plugins. Later real module storage/authorization/lifecycle
proof remains assigned to its numbered specs.

All canaries are generated transiently; reports retain no malicious raw input,
argument, manifest, secret, filesystem layout or exception dump. Actual author
IO failure exits 1 with `consumer_io_failure`, invalid invocation exits 2 with
`invocation_invalid`, accepted exits 0, and validation rejection exits 2.

### Executed author guide

The documented minimal, compatibility, prior-ID, invalid-ID and corrected-retry
`dotnet run --project tests/contract/Kofte.Manifest.Consumer --no-build -c Release -- ...`
commands were executed against the exact minimal fixture.
Observed exits/codes: **0 accepted; 2 host_incompatible; 2 identity_changed;
2 module_id_invalid; 0 accepted**, respectively; no stderr/default value or
manifest/descriptor echo. This implementer automation proves guide executability,
**not** T045's unfamiliar-human experience/timing.

### Available-machine timing under the human-directed amendment

One initialized measurement run used macOS **26.5**, arm64, **Apple M5 Pro**,
18 logical processors, 68,719,476,736 physical memory bytes, SDK **10.0.402**,
runtime **10.0.13**. The available-memory value reported by .NET agreed.
It measured every call individually using `Stopwatch`, not aggregate/p95;
warm initialization was excluded, not cold console startup. All 100 calls
passed the one-second bound; final maximum **0.031 ms** after the reader correction.
The earlier 0.0206-ms run is superseded, not relabeled as final-source evidence.

Case sequence repeats `minimal, full, invalid-id, oversized, deep` twenty times.
Per-call elapsed milliseconds (index is the position below, starting at zero;
zero is the reported timing resolution, not an inferred missing measurement):

```text
0.0056,0.031,0.0082,0.0003,0.0005,0.0041,0.0122,0.0077,0,0.0004,
0.0043,0.0117,0.0027,0,0.0002,0.0038,0.011,0.0026,0,0.0002,
0.005,0.0115,0.0025,0,0.0003,0.0043,0.0117,0.0025,0,0.0002,
0.0025,0.012,0.0025,0,0.0002,0.0026,0.0127,0.0041,0,0.0002,
0.0026,0.0129,0.0041,0,0.0002,0.0026,0.0126,0.0038,0,0.0002,
0.0026,0.0122,0.0037,0,0.0003,0.004,0.0102,0.0035,0,0.0002,
0.0036,0.0112,0.0025,0,0.0002,0.0034,0.0106,0.0026,0,0.0002,
0.0034,0.0115,0.0025,0,0.0002,0.0025,0.0113,0.0025,0,0.0012,
0.0026,0.0115,0.0023,0,0.0002,0.0026,0.0123,0.0042,0,0.0002,
0.0026,0.011,0.0037,0,0.0002,0.0027,0.0118,0.0035,0,0.0002
```

These actual measurements satisfy the **amended SC-004/T044 environment policy**:
an available supported workstation/CI runner with actual metadata, no fixed
OS/CPU/RAM resource profile or dedicated benchmark hardware. They do not prove
equivalent performance on all machines or claim a fabricated Ubuntu run.
The retained 100 initialized individual checks and one-second bound are unchanged.
Earlier analysis and reviewer-owned checklist text is historical evidence of
the prior design; neither report nor checklist markers were silently rewritten.

## Remaining gates and acceptance mapping

| Outcome / obligation | Current evidence and limit |
| --- | --- |
| FR-001-008, FR-010-013; SC-002/003 | Pure public/schema/identity/version/declaration/context/diagnostic tests and repeated real cultures cover the introduced behavior; source/public checkpoints above map US1.1-4, US2.1-3 and US3.1-4 individually. |
| FR-009/012; SC-005 | Metadata-only validation, safe outputs, bounded malformed/over-budget inputs, observers/source review and retry cover US4.1-3; no arbitrary-plugin sandbox or later isolation claim. |
| FR-014; SC-001 | Executable guide and correction outputs proven; **T045 blocked** on unfamiliar human's actual guide exercise/attestation and elapsed time within ten minutes. |
| FR-002/012; SC-004 | **T044 evidenced** under the explicit human-directed amendment: actual supported-workstation metadata and all 100 individual initialized timings/limits above; no fixed machine or cross-machine equivalence claim. |
| FR-015; SC-006 | New project-reference-only host dependency; locked adoption and actual both-provider contract checks pass. Full local/current-head CI regression evidence is recorded separately below, not substituted for human/timing proof. |
| T046/T047 | Final gate/reconciliation tasks remain unchecked because mandatory T045 is incomplete, even if available automated checks pass. No final constitutional/feature completion is claimed. |
| T048 | Safe checkpoint publication only; no `complete`, converge/review/accept, ready status or merge request. Resume the **same** implement stage/branch/draft PR after gates are supplied. |

Principles I-III/XII: brand, minimal shared contracts, native calls, test ownership,
versioned scope and one feature branch/PR are preserved. IV/V: no physical
storage/transaction authority; registry/migration proof remains 003/004.
VI/VII: validation grants no permission/trust; future protected REST/UI/MCP/jobs
still require common actor/team/resource policy and audit. VIII: declaration
kinds are not resource/event/job/provider/realtime runtimes. IX: no new UI
journey; bootstrap browser/accessibility remains in the regression gate.
X/XI: portable locked tools, independent public/structural/negative proof and
honest unavailable evidence; all applicable final gates remain mandatory.

## Final available local regression checkpoint

`python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10`
returned **0** on the frozen final source. All **ten groups / 177 named steps**
passed, and every report has `completeGate: true`; none was failed, skipped,
empty or substituted. The run spanned **2026-10-21 22:36:50-22:45:54 UTC**.
The reports identify the entry HEAD plus the actual working-tree increment
identified above, not a claim that the entry commit implemented spec 002.

| Group | Named steps | Observed result |
| --- | --- | --- |
| quality | 18 | pass; locked restore, .NET formatting/build, npm restore/format/lint/types, generated contract, workspace builds, dependency review and repository checks |
| unit | 10 | pass; original host/web/contract/Python plus required platform unit project |
| integration / SQLite | 5 | pass; real provider, not a manifest-only mock |
| integration / PostgreSQL | 6 | pass; actual owned disposable provider |
| recovery / both | 32 | pass; native and browser recovery with owned outages/restarts/restoration |
| contract / both | 34 | pass; required author build, public manifest and offline schema plus original native OpenAPI/HTTP/adapter proof |
| browser | 16 | pass; unchanged bootstrap UX/accessibility and owned restart flow |
| privacy / both | 30 | pass; seeded HTTP/browser/runtime capture plus supervisor-output fixture |
| startup | 8 | pass; all ten fresh warm-start cycles matched fixtures; ready 0.4454-0.5912 s, page 0.0269-0.0852 s |
| failure-propagation | 18 | pass; isolated current-source baseline and actual test/formatter/build/PostgreSQL/Chromium failure propagation |

Required `python3 scripts/check_repository.py`,
`python3 -m unittest discover -s scripts/tests -v` (**103 tests**),
`git diff --check` and `git diff --cached --check` also passed independently.
The local generated reports remain ignored; no child log, runtime data,
credential, local configuration or build output is published.

Current-head GitHub checks can only be inspected after pushing this checkpoint.
Their actual assessed SHA, status and durable run links belong in this draft
PR's **Evidence** section after observation; prior analyze-head green checks
are not current implementation evidence. This local gate and any passing CI
still do not supply T045 or satisfy T046-T048's human prerequisites. The driver
is durably **blocked at implement**; resume the same branch/PR after actual
unfamiliar-author attestation, then finish final reconciliation before completing
this stage. Converge, review, acceptance and merge are not executed here.
