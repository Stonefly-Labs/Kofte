# Final review: platform contracts and module identity

**Result:** pass

**Spec:** 002 | **Review date:** 2026-10-21 (repository working date)
**Assessed head:** `286713c552a40c462bb5e631ca7ac0a893a5edf0`
**Branch:** `feature/002-platform-contracts`
**PR:** [#5](https://github.com/Stonefly-Labs/Kofte/pull/5), still draft

## Decision and scope

The read-only `spec-review` agent inspected actual implementation, tests,
contracts, schema/examples, dependency/project boundaries, verifier/CI and
approved artifacts. The caller executed the checks below and reconciled actual
assessed-head CI. All 15 functional requirements, six amended success criteria,
14 acceptance scenarios, twelve plan decisions, T001-T049 and twelve
constitutional principles have evidence. No high-confidence acceptance defect,
unresolved design conflict, scope expansion or missing acceptance proof remains.
No remediation task is appended; `tasks.md` is unchanged.

T049 closes the prior public-corpus gap in executable tests, including exact
boundaries, reversed ranges, ordered multi-errors, recursive property reordering,
phase precedence and 32/33-diagnostic truncation. Every corpus case enters the
100-repeat, two-public-consumer, two-real-culture checks.

Accepted ADR 0004 and its explicit human amendments govern this review:
T044 permits measured supported workstations/CI runners rather than fixed
Ubuntu/CPU/RAM, and amended SC-001/T045 requires automated guide/correction/retry
proof rather than a manual timed author exercise. Superseded requirements are
not silently reinstated or claimed satisfied by a human study.

**Review is not acceptance.** Explicit human acceptance of feature 002 is still
required in a later invocation. No acceptance artifact, approval, ready action,
merge request or next numbered feature is produced here.

## Source and evidence notation

Paths below are relative to these prefixes; line references describe the
assessed head, not later documentation-only checkpoint lines.

| Prefix | Path |
| --- | --- |
| `P/` | `src/Kofte.Platform/Modules/Manifest/` |
| `U/` | `tests/unit/Kofte.Platform.Tests/Modules/Manifest/` |
| `C/` | `tests/contract/Kofte.Platform.ContractTests/Modules/Manifest/` |
| `F/` | `specs/002-platform-contracts/` |
| `Author` | `tests/contract/Kofte.Manifest.Consumer/Program.cs` |

The reviewer read all thirteen production manifest files and corresponding
dedicated unit/public-contract tests rather than relying on checked tasks or
implementation summaries. It was read-only and did not execute checks.
Execution evidence in this report belongs to the caller or inspected CI.

## Functional requirement coverage

| Requirement | Actual implementation | Behavioral evidence |
| --- | --- | --- |
| FR-001 public contract/schema/declarations | `P/ManifestValidator.cs:6-55`; `P/ModuleDescription.cs:3-57`; closed `F/contracts/module-manifest.schema.json:1-151`; dependency-free platform project. | `C/PublicConsumerTests.cs:9-57`: both public consumers, all descriptor fields/version domains, exact fixture bytes and project direction. |
| FR-002 strict input/closed shape/budgets | `P/ManifestReader.cs:26-94,111-130`; `P/ManifestValidator.cs:15-30,57-97`: original bytes, UTF-8/BOM/scalars, decoded duplicate keys, syntax/depth/collection and typed-shape gates. | `U/ManifestParsingTests.cs:10-111`; `U/ManifestBudgetTests.cs:18-112`; original-byte/public boundaries `C/ManifestCorpus.cs:26-191`. |
| FR-003 canonical identity | `P/ModuleId.cs:10-39`: lowercase ASCII reverse-DNS labels, ordinal identity, no normalization; display metadata remains separate. | `U/ModuleIdTests.cs:7-40`; public identity variants/limits `C/ManifestCorpus.cs:75-96`; repeated corpus. |
| FR-004 immutable identity/update comparison | `P/IdentityValidation.cs:5-17`; internally constructed, get-only, defensively copied `P/ModuleDescription.cs:3-57`. No registry/allocator claim. | `U/IdentityUpdateTests.cs:9-33`; `C/IdentityContractTests.cs:6-16`; retained outcomes in `C/PublicConsumerTests.cs:60-77` and `C/RetryTests.cs:8-24`. |
| FR-005 separate version domains | `P/SemanticVersion.cs:24-80`; `P/VersionRange.cs:19-49`; schema dispatch and capability version validation; `F/contracts/validation.md:8-16,57-99`. | `U/SemanticVersionTests.cs:7-40`; `U/VersionRangeTests.cs:7-43`; public syntax, range and huge-number cases `C/ManifestCorpus.cs:46-125`. |
| FR-006 explicit host compatibility | `P/CompatibilityValidation.cs:5-13` rejects missing/invalid context; `P/VersionRange.cs:19-49` implements interval/inclusivity/prerelease/build rules; validator `:45-50` defers compatibility until earlier gates pass. | `U/VersionRangeTests.cs:7-43`; public reversed/huge/boundary cases `C/ManifestCorpus.cs:50,114-119,250-259`; `C/CompatibilityContractTests.cs:6-16`. |
| FR-007 capability declarations | `P/DeclarationValidation.cs:7-30`: exactly nine approved kinds, kind-based duplicates and syntax versus unsupported-version errors. No activation. | `U/DeclarationTests.cs:18-33`; `C/PublicConsumerTests.cs:22-41`; corpus unknown/duplicate/version and 9/10-count cases. |
| FR-008 dependencies without resolution | `P/DeclarationValidation.cs:32-52`: canonical IDs/ranges, self/duplicate rejection; absent valid targets preserved without installed-set lookup or resolution. | `C/ManifestCorpus.cs:57-59,97-100`; full dependency preservation `C/PublicConsumerTests.cs:34-36`. |
| FR-009 descriptive permissions/storage | `P/MetadataValidation.cs:7-49`; enclosing immutable description owns permission declarations; storage is only a relational-need boolean, never a selector or grant. | `U/DeclarationTests.cs:35-63`; public name/description/selector cases `C/ManifestCorpus.cs:60-61,85-88,101-102,180-186`; observers `C/SideEffectTests.cs:33-57`. |
| FR-010 complete outcomes/safe diagnostics | Disjoint copied outcomes `P/ManifestValidationResult.cs:3-24`; fixed messages/order/truncation `P/ManifestDiagnostic.cs:12-95`; complete descriptor constructed only at validator `:52-54`. | `U/ValidationResultTests.cs:21-43`; `U/ManifestBudgetTests.cs:89-112`; independently ordered lists `C/ManifestCorpus.cs:193-245`; public truncation/retry. |
| FR-011 determinism/corrected retry | Ordinal comparisons, invariant numeric paths, immutable results and no mutable per-call cache or ambient defaults. | `C/DeterminismTests.cs:10-52`: actual asserted cultures and 100 decisions per consumer/case; `C/RetryTests.cs:8-24`: each rejection corrected without contaminating retained state. |
| FR-012 bounded private data-only operation | Production manifest code has no filesystem/network/DNS/process/loader/database/configuration calls. `Author:34-76` bounds stdin and emits only allowlisted outcomes. | `C/SafeOutputTests.cs:10-104`: unpredictable canaries, captured output, invalid invocation/oversized/unreadable input; `C/SideEffectTests.cs:12-66`: observed requests, activation, sentinels/files and dependency references; budget/timing/retry tests. |
| FR-013 independent corpus/structural distinction | `F/contracts/acceptance-corpus.md:5-110`; public adapters share the validator, not duplicated rules. Expected corpus outcomes do not call production parsing helpers; malformed bytes remain bytes. | `C/ManifestCorpus.cs:26-259`; `C/PublicConsumerTests.cs:9-57`; offline structural tests `tests/contract/module-manifest.test.ts:1-129`. |
| FR-014 executable correction guide | `F/quickstart.md:17-84,143-196`; public contract/output and breaking-change/trust rules; Accepted ADR 0004. | Seven actual automated guide/example/error/corrected-retry results `F/implementation.md:281-304`, supplemented by public adapter/error/retry tests. Not human-usability evidence. |
| FR-015 preserve bootstrap/dependency boundary | Host adds only the platform project reference; `src/Kofte.Host/Program.cs:14-69` retains module-free store/health/OpenAPI startup. New platform/author projects add no external runtime package; adoption `docs/development/dependencies.md:10-20`. | Public project/fixture assertions; mandatory retained provider/browser/startup/recovery wiring `scripts/verify.py:803-930`; fresh both-provider contracts and assessed-head full CI below. |

## Acceptance scenarios and success criteria

| Scenario | Source/test coverage |
| --- | --- |
| US1.1 complete valid description, no activation | Validator `:31-54`; public minimal/full descriptor assertions and side-effect observers. |
| US1.2 malformed/missing/type/duplicate rejection | Parsing tests `:10-67,85-111`; raw C03/C04/C07/C08/C34 corpus cases. |
| US1.3 unsupported schema/unknown fields | Validator `:15-30,62-69`; parsing tests `:60-82`; C05/C06/C29/C33. |
| US1.4 independent repeated/culture-stable outcomes | Determinism tests `:10-39`; harness complete-list comparison `:34-38`; recursive reversal/ordered corpus `:193-245`. |
| US2.1 release/display changes preserve identity | Identity update tests `:9-33`; retained-result and collection-mutation assertions. |
| US2.2 reject noncanonical IDs without normalization | `ModuleId.cs:10-39`; unit/public invalid variants and exact limits. |
| US2.3 explicit prior-ID mismatch | Identity validation `:5-17`; C11/C12; public identity tests. |
| US3.1 host inclusivity/prerelease/build boundaries | Range implementation and complete public range table `:250-259`; precedence chain/huge comparisons `:103-119`. |
| US3.2 syntax/context versus incompatibility | Explicit compatibility context, validator phase gates, C13/C15-C20 and phase fixtures `:123-136`. |
| US3.3 invalid/duplicate/unsupported capabilities | Declaration validation `:10-30`; C21-C23; public compatibility tests. |
| US3.4 invalid/duplicate/self dependencies; absent valid targets | Declaration validation `:32-52`; C24-C26/malformed cases; full description assertions. |
| US4.1 private useful correction | Fixed diagnostic messages; output canaries/controls/invalid invocation; C32/C33. |
| US4.2 budgets/selectors/hooks cause no effects | Reader/budget checks, public boundary recipes `:139-191`, observed side effects and initialized timing. |
| US4.3 clean corrected retry | Every current rejection enters `C/RetryTests.cs:8-24`; retained immutable results and public truncation retry. |

| Amended outcome | Evidence and limits |
| --- | --- |
| SC-001 automated author guide | Seven actual CLI example/error/correction/retry results in implementation `:281-304`; executable quickstart `:43-84`. No manual timed exercise or usability inference. |
| SC-002 independent complete corpus | C01-C34 variants, public range/precedence/huge-number/budget/ordered-error cases with explicit expected decisions; separate schema expectations. |
| SC-003 100 repeats/two consumers/two cultures | `C/DeterminismTests.cs:7-39`, harness `:23-38,93-110`, `Author:26-32,43-68` execute and compare every complete ordered outcome under actual `en-US` and `tr-TR`. Fresh mandatory public-contract selection passes. |
| SC-004 individual bounded timing | `C/ManifestPerformanceTests.cs:12-48` initializes then individually times 100 checks. Actual supported-workstation metadata and durations in implementation `:187-220`; recorded maximum 0.031 ms. Fresh contract selection passes; no new per-sample measurements are invented here. |
| SC-005 privacy/no effects/retry | Canary/output, actual IO failure, owned observers, production/dependency inspection and every-rejection correction evidence above. Not universal egress instrumentation or sandbox proof. |
| SC-006 compatibility/preserved foundation | Public reversed/equal/inclusive/exclusive/prerelease/build/huge comparisons; fresh real SQLite/PostgreSQL OpenAPI/HTTP/adapter checks and all existing bootstrap CI suites. |

Exact/one-over public evidence includes 65,536 original bytes, depth 8,
capability/dependency/permission counts 9/64/128, ID label/total lengths 63/253,
version/range lengths 128/512, display/description scalar lengths 128/256 and
permission-name length 64. The defensive range budget does not imply every
in-budget string is valid grammar. Diagnostic tests distinguish exactly 32
errors from first-31-plus-marker truncation at 33 and assert
fatal -> schema -> shape -> semantic/context -> compatibility precedence.

## Plan, task and constitutional coverage

| Plan decision | Assessment |
| --- | --- |
| P01 minimal library/test ownership | Dependency-free platform, public-only author consumer, dedicated normal-SDK test projects and fixture copying; no test source in production. |
| P02 pins/adoption | Existing tool/central pins retained; new project locks and dependency inventory reconcile without a new external runtime package. |
| P03 strict offline JSON/schema | Original-byte reader, closed shape pipeline, Draft 2020-12 and offline structural validation; no manifest-reference resolution. |
| P04 immutable identity/prior context | Canonical value, explicit update comparison and copied immutable graph; mutation tests. |
| P05 bounded SemVer/ranges | Decimal-text comparisons avoid numeric overflow; exact/two-bound grammar and prerelease rules match the approved contract. |
| P06 version domains/vocabulary | Schema, host, module and capability versions stay distinct; nine approved kinds. |
| P07 descriptive metadata | No resolver, grant, activation or physical storage authority. |
| P08 outcomes/diagnostics | Complete disjoint results, fixed private messages, numeric/path/code ordering and exact deterministic truncation. |
| P09 pure bounded validation | No production IO/ambient configuration; canaries, observers, retry and timing evidence. |
| P10 public/structural corpus | Independent expected outcomes, actual cultures and complete repeated corpus including T049. |
| P11 preserved fail-closed bootstrap | Required inventory/nonempty native selections, author-before-contract ordering and retained real-provider suites in local/CI wrapper. |
| P12 guide/honest delivery | Approved amendments and actual guide/timing records; review, acceptance and human merge remain separate. |

| Tasks | Evidence assessed beyond checked markers |
| --- | --- |
| T001-T004 | Implementation entry/design approval `:13-30`, solution/project/locks/adoption, complete independent corpus and exact copied-byte assertions. |
| T005-T009 | Result/descriptor/context/diagnostic code and tests; harness `:40-110` bounds output/deadline/owned child cleanup; implementation command matrix `:32-148`. |
| T010-T015 | Canonical identity/update/immutability code and unit/public tests mapped above. |
| T016-T023 | Version/range/context/declaration/metadata code and independent public/unit boundaries above. |
| T024-T028 | Parsing unit tests `:10-111`, exact offline schema tests, public descriptors/corpus, original-byte reader and complete-only validator. |
| T029-T032 | Test-owned author adapter and public harness/project assertions; complete repeated real-culture corpus. |
| T033-T040 | Unit/public budgets, canaries/unreadable-input/output capture, observed effects, corrected retry, recognized diagnostic-path provenance and initialized performance. |
| T041-T042 | Fail-closed verifier regression tests `scripts/tests/test_verify.py:225-273,386-442`; wrapper `scripts/verify.py:58-74,803-878`. |
| T043-T045 | Executable/current docs, actual amended workstation timings and seven amended automated guide results. |
| T046-T048 | Historical complete local gate/input reconciliation in implementation `:244-279,381-411`; fresh targeted/current-head CI below; ledger reaches review, not acceptance. |
| T049 | Public boundaries `C/ManifestCorpus.cs:139-191`, collected/reversed/truncated errors `:193-245`, phases `:123-136`, all included in repeated corpus and corrected retry. |

| Principle | Applicability/evidence or explicit later-runtime exclusion |
| --- | --- |
| I product/brand | Portable Köfte library/Unicode metadata; no mandatory optional module or Hoard. |
| II modularity/trust | Stable manifest and nine declaration categories; lifecycle/loading/installed collisions remain later work; no sandbox claim. |
| III slices/dependencies | Cohesive manifest helpers, demonstrably shared minimal contracts, dedicated test projects, no MediatR/AutoMapper. |
| IV storage | Only relational need is described. Registry allocation/concurrency/reserved names, schema-local migrations and canonical separate SQLite files remain 003/004. Existing provider checks preserve bootstrap, not module isolation. |
| V transactions | No durable effect, tables/shared connection or cross-module ACID promise; outbox/compensation inapplicable to this pure validator. |
| VI safety/authorization | Explicit bounded private validation; no protected operation/grant. Actor/team/resource authorization remains later specs, not a parser bypass. |
| VII MCP | `mcp-tools` is metadata only; actual collision/discovery/invocation policy and product server remain 007/013. Developer MCP configuration is not product implementation. |
| VIII resources/jobs/realtime | Declaration categories only; status, durability, jobs/providers and SignalR runtimes are out of scope. |
| IX frontend | No new page/loader/permission state. Readable corrections/Unicode apply; unchanged bootstrap browser/accessibility CI remains relevant. |
| X portability/operations | No service prerequisite after restore; pinned tools, explicit failures/retry/version/change/rollback guidance and preserved provider/CI evidence. |
| XI evidence | Mandatory unit/public/schema/culture/privacy/effect/retry/timing checks; later module-isolation/auth/MCP/job/reference-module/Kubernetes release proof is not demanded of 002. |
| XII scope/traceability | Only 002 delivered after accepted 001; MVP remains 001-024; ADR/task approvals are not feature acceptance. |

## Exact caller-executed verification

Checks ran against assessed head above with only the active-review ledger edit.
The default shell initially could not select pinned SDK 10.0.402 because its
global installation was 10.0.203. The caller selected the existing isolated
pinned SDK/runtime and Node/npm installations, confirmed 10.0.402/10.0.13,
24.22.0/11.19.1, and did not change repository pins. This prerequisite failure
is not a passing test or product defect.

| Command | Actual result |
| --- | --- |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed, 103 tests. |
| `git diff --check` | Passed. |
| `python3 scripts/verify.py --suite quality` | Exit 0; all 18 named steps passed, 36.673 s. |
| `python3 scripts/verify.py --suite unit` | Exit 0; all 10 named steps passed, 8.916 s. |
| `python3 scripts/verify.py --suite contract --provider both --disposable-postgres` | Exit 0; all 34 named steps passed, 87.842 s, including owned real-provider cleanup/output scans. |

Quality executed locked restores, `dotnet format Kofte.slnx
--verify-no-changes --no-restore`, `dotnet build Kofte.slnx --no-restore -c
Release -warnaserror`, `npm run format:check`, `npm run lint --workspaces
--if-present`, `npm run typecheck --workspaces --if-present`, `npm run
contract:check`, `npm run build --workspaces --if-present` and `npm run
dependencies:check`; every child passed. Mandatory workspace/project inventory
prevents `--if-present` from silently omitting required commands.

Unit executed both host/platform native test projects with
`--minimum-expected-tests 1`, both web/bootstrap-contract unit commands and
Python discovery. Contract built the author consumer, ran the complete public
contract test project with a nonempty-selection requirement and
`npm run test --workspace tests/contract -- module-manifest.test.ts`,
then retained actual SQLite/PostgreSQL native OpenAPI and HTTP/adapter fixtures.
The public contract source executes both real-culture theory cases; the safe
wrapper reports command outcomes, not per-test counts, so no new individual
.NET test count is claimed.

All **62 fresh targeted steps** passed. Safe ignored report identities under
`artifacts/verification/` are:

- `quality-none-1eff9f56b6d54b889ceaa51296a6088a.json`
- `unit-none-a08f977fcecb478db1678cf3bfbd7b6d.json`
- `contract-both-f01e22df7bdf4d689df47f7e9183585a.json`

Each has the assessed revision, a populated end time and only passing step
outcomes. Actual local environment: Darwin 25.5.0 arm64, 18 logical CPUs,
68,719,476,736 bytes physical memory, Python 3.15.0. These targeted checks are
not a newly executed local `--suite all` gate.

## Actual assessed-head CI

[Product checks 39704980011](https://github.com/Stonefly-Labs/Kofte/actions/runs/39704980011)
and [Repository checks 39704980016](https://github.com/Stonefly-Labs/Kofte/actions/runs/39704980016)
both succeeded for assessed feature head `286713c...`.
The caller downloaded and inspected all ten actual product verification reports:

| Suite | Passing named steps |
| --- | ---: |
| quality | 18 |
| unit | 10 |
| integration-sqlite | 5 |
| integration-postgres | 6 |
| recovery | 32 |
| contract | 34 |
| browser | 16 |
| privacy | 30 |
| startup | 8 |
| failure-propagation | 18 |
| Total | 177 |

All eleven GitHub contexts, including repository checks, are green. Report
revision `f46dc03466745271dd7b0ea6bc0a7f5364acb2ac` is the actual synthetic
merge, not the feature SHA. GitHub's commit API confirms parents
`c30f9f8d27025b22afe37d7b46194a05f0c8b18b` (main) and the assessed feature
head. All report steps passed with pinned tools on actual Ubuntu x64 runners.
Startup retains ten exact observations with matching fixture values; failure
propagation includes all five deliberate failure cases and their baselines.

Historical complete local evidence/input reconciliation remains in
`implementation.md`; it is not relabeled as fresh T049 execution. Current T049
proof is the inspected source, fresh mandatory public-contract execution and
actual assessed-head CI.

## Findings, limits and handoff

No defect, missing check or unresolved within-scope product decision remains.
No product/test/schema/spec/plan/task/checklist/dependency/tooling file is
changed by review. Publication adds this report, progress documentation and the
driver transition only.

Two public consumers intentionally share one validator: this proves public
consumption and independent expected outcomes, not independent algorithms.
Owned side-effect observers plus production-source inspection are not a
hostile-plugin sandbox or universal egress probe. Initialized timing is not
cold-start or universal hardware performance; automated guide proof is not
unfamiliar-human usability. Registration uniqueness, dependency resolution,
module storage/migrations, authorization, actual MCP execution, UI extensions,
production readiness and MVP completion remain unclaimed future obligations.

**Next stage:** accept, requiring an explicit human decision about numbered
feature 002 and these limits. Keep the same branch and draft PR. Stop after
publishing review; do not execute acceptance or request merge.
