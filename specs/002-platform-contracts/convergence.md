# Convergence: 002 - Platform Contracts and Module Identity

**Result:** gaps
**Outcome:** tasks_appended
**Date:** 2026-10-22
**Assessed revision:** `b9d23ffca2f89b7292c1822a4a6318794bacffe3`
**Stage:** converge only; not implementation, final review or human acceptance

## Scope and method

Executed the installed `speckit-converge` after the recorded completed implement
stage, on the original feature branch and open draft PR #5. With
`SPECIFY_FEATURE_DIRECTORY=specs/002-platform-contracts`, ran once:

```bash
.specify/scripts/bash/check-prerequisites.sh --json --require-spec --require-tasks --include-tasks
```

It resolved the recorded directory and required artifacts. Intent comes from
`spec.md`, `plan.md` and every existing task, governed by constitution v2.0.1.
Linked model, schema, corpus, contract, guide, ADR and implementation evidence
explain that intent; they do not add scope. Applied both explicit human-directed
amendments: actual available-machine timing, and automated guide/corrected-retry
proof instead of a timed human exercise. Historical checklist/analysis/ledger
statements are not new human evidence or current requirements.

Inspected all thirteen production manifest source files, dedicated unit/public
contract tests, author adapter, structural tests, project/solution settings,
dependency decisions, verifier regressions/wiring and CI. Assessment is bounded
to the plan/task touchpoints and unchanged bootstrap integration surfaces.
Checkboxes and the prior report were not used as behavioral proof. No extension
hooks are configured. No product source, test, schema, spec or plan is changed.

## Convergence findings

| ID | Gap type | Severity | Source | Evidence | Remaining work |
| --- | --- | --- | --- | --- | --- |
| F1 | partial | HIGH | SC-003/006, FR-013; T004/023/026/031; plan public corpus | `ManifestCorpus.cs:82-105` supplies ID limits and only over-budget byte/depth/capability cases, not the complete documented limit recipes. Its range table at lines 110-122 omits the documented reversed interval. `DeterminismTests.cs:22-32` repeats only `Cases()`. `PublicConsumerTests.cs:60-89` checks multi-error/truncation once, separately from that repeated corpus. | T049: complete the independently expected public corpus and execute all documented cases, including ordered multi-diagnostics, 100 times per consumer per real culture. |

This is a **required acceptance-evidence gap**, not a demonstrated parser defect.
For example, the reversed range is covered only by helper-level
`VersionRangeTests.cs:28-43`, while SC-006 requires documented boundaries through
the public contract. `ManifestBudgetTests` covers exact/one-over recipes in
process, and the structural tests cover some limits, but neither establishes
SC-003's full two-consumer/two-culture repeated proof. Existing passing tests
therefore do not close F1. Do not weaken the corpus or criteria to make it pass.

| Metric | Assessed / result |
| --- | --- |
| Functional requirements | 15/15, FR-001 through FR-015 |
| Buildable success criteria | 6/6, SC-001 through SC-006, including amendments |
| Acceptance scenarios | 14/14: US1.1-4, US2.1-3, US3.1-4, US4.1-3 |
| Requirements / acceptance criteria total | 35 assessed, not 35 fully evidenced |
| Plan decisions | 12/12, P01-P12 below |
| Existing tasks, regardless of checkbox | 48/48, T001-T048 |
| Constitutional principles | 12/12, with later-spec applicability explained |
| Findings by type | missing 0; partial 1; contradicts 0; unrequested 0 |
| Findings by severity | critical 0; high 1; medium 0; low 0 |
| Tasks appended | 1: T049 under Phase 7; all original bytes preserved |

## Requirement and acceptance assessment

Paths are relative to repository root. Production names below refer to
`src/Kofte.Platform/Modules/Manifest/`; tests live in their dedicated projects.

| Intent | Current behavior and evidence |
| --- | --- |
| FR-001 | Minimal public library, closed schema and exact examples expose all declared fields without host internals. `PublicConsumerTests.FullDescriptorExposesAllVersionDomainsAndNonAuthoritativeMetadata` checks the complete accepted description. |
| FR-002 | `ManifestReader` budgets bytes before decoding, checks strict UTF-8/scalars, decoded keys, depth/collections before materialization; `ManifestValidator.Shape` requires closed typed objects and stops on unsupported schema. Parsing/budget tests cover malformed and exact/one-over inputs; public repeat completeness remains F1. |
| FR-003 | `ModuleId` performs bounded canonical ASCII scans, label/total limits and ordinal equality, with no normalization. Unit/public identity tests include aliases, Unicode, paths and exact length pairs. |
| FR-004 | Internal constructors, read-only defensive copies and `IdentityValidation` preserve identity across metadata/releases and reject explicit prior-ID changes. Mutation/retry tests retain earlier results and overwrite caller bytes. |
| FR-005/006 | `SemanticVersion` compares bounded decimal text and SemVer prereleases without overflow; `VersionRange` enforces exact/two-bound syntax, inclusivity and same-core prerelease opt-in. Context is explicit. Unit/public boundary tests pass, but the documented reversed interval lacks public proof (F1). |
| FR-007 | `DeclarationValidation` recognizes nine kinds, accepts stable `1.0.0` precedence with build metadata, rejects unknown/unsupported versions and duplicates by kind independently of version. All kinds are exercised. |
| FR-008 | Dependencies require canonical IDs/ranges, reject duplicate/self references and preserve absent targets. No installed-set lookup, resolver, cycle or activation assertion exists. |
| FR-009 | `MetadataValidation` checks local permission grammar/uniqueness/scalar descriptions. Ownership is containment in the module, not a grant. Closed `storage.relational` accepts true/false and rejects physical selectors. |
| FR-010 | Disjoint immutable result variants prevent partial acceptance. Fixed diagnostics use known paths, phase/origin/schema/numeric-index/code ordering and first-31-plus-marker truncation. One-off multi-error checks pass; repeated full-corpus ordering remains F1. |
| FR-011 | No validation state persists between calls. Independent consumers, real cultures, reordered properties and retained-result/retry tests pass for supplied cases. Omitted documented repeated cases prevent full evidence (F1). |
| FR-012 | Production code has no IO, DNS/network, process, assembly activation, environment, telemetry, host or database service calls. Strict bounded parsing, fixed output, transient canaries and owned observers support no-effects/privacy behavior; this is not a plugin sandbox. |
| FR-013 | Exact original schema/examples/raw fixtures, independent public expectations and Ajv structural checks distinguish schema success from complete acceptance. The documented corpus exists, but public/repeated coverage is incomplete (F1). |
| FR-014 | Executable guide documents setup, safe exits/diagnostics, correction/retry, compatibility and source rollback. It distinguishes validation from trust/install/permissions and identifies breaking-change review. Amended SC-001 has seven recorded actual CLI checks. |
| FR-015 | Host adds only its library reference; startup/routes/stores/web remain module-free. Reviewed locks introduce no new external identity. Fresh real SQLite/PostgreSQL HTTP/OpenAPI/adapter checks and assessed-head CI pass. |
| SC-001 | Amended automated guide proof is recorded with exact arguments, stdin, exits and corrective retries in `implementation.md`; no human study or elapsed-time attestation is inferred. |
| SC-002 | Independent C01-C34 variants, helper boundary tests and public/schema decisions pass. The wider explicit public corpus proof is incomplete as described in F1, not evidence of false acceptance. |
| SC-003 | The repeated tests actually set/assert real `en-US`/`tr-TR` and compare 100 decisions per supplied input per consumer. Missing recipes and repeated multi-diagnostics leave this criterion partial (F1). |
| SC-004 | `ManifestPerformanceTests` measures each of 100 initialized minimal/full/invalid/oversized/deep calls below one second. Recorded actual supported-machine metadata and individual timings satisfy the amended hardware policy; fresh contract execution passes. |
| SC-005 | Captured library/adapter outputs, unpredictable canaries, actual unreadable-input failure, owned filesystem/network/assembly observers and corrected retries pass. Source review corroborates absence of allocation/configuration effects; no future module isolation is claimed. |
| SC-006 | Existing public compatibility cases and both-provider bootstrap checks pass. Every documented boundary is not yet demonstrated publicly; reversed-range coverage is helper-only (F1). |

| Scenarios | Concrete checks assessed |
| --- | --- |
| US1.1-3 | Full/minimal public descriptors; strict raw/typed/closed/schema parsing; all original-byte fixtures and structural/semantic divergence. |
| US1.4 | Two-consumer real-culture repeated tests and property/numeric-index ordering; complete prescribed coverage remains F1. |
| US2.1-3 | Identity/update public cases, canonical exact limits, copied immutable collections and retained accepted identity across later calls. |
| US3.1/2 | Independent inclusive/exclusive/prerelease/build membership expectations and distinct malformed/context/incompatibility diagnostics; omitted public reversed-range proof is F1. |
| US3.3/4 | Supported/unknown/unsupported/duplicate capabilities; malformed/duplicate/self dependencies and valid absent dependency preservation. |
| US4.1-3 | Canary/output/IO failures, strict budgets, closed physical/executable/reference fields, observed no effects, every supplied rejection followed by accepted retry without reset. |

## Plan decisions and constitutional constraints

| Decision | Current evidence / result |
| --- | --- |
| P01 - One minimal library and dedicated test ownership | Seven-project solution, public-only author reference, host-facing contract project; normal SDK test inclusion, no linked production test source. |
| P02 - Pinned supported tools and reviewed dependencies | Existing SDK/runtime/Node/npm pins and new reviewed project locks; union remains 403 npm/69 NuGet identities. No new runtime package. |
| P03 - Strict JSON and offline closed structural schema | Reader/shape pipeline, Draft 2020-12 schema and existing Ajv; no schema fetch or alternate permissive serializer. |
| P04 - Canonical immutable identity and explicit prior context | `ModuleId`, `IdentityValidation`, defensive descriptions and independent identity/update tests. |
| P05 - Bounded SemVer and small range grammar | Decimal-text comparison, exact/two-bound intervals, same-core prerelease opt-in and independent helper/public expectations; public coverage gap F1. |
| P06 - Distinct version domains and capability vocabulary | Schema `1`, explicit published host `1.0.0`, module release and nine stable capability versions are separate; no inferred host default. |
| P07 - Non-authoritative dependency/permission/storage metadata | Validated descriptions without lookup, authorization grant or physical allocation; schema rejects selectors/hooks. |
| P08 - Disjoint immutable outcomes and safe ordered errors | Internal constructors, copied read-only collections, fixed messages, applicable phase checks and deterministic truncation. |
| P09 - Bounded pure execution, privacy and corrected retry | Reader budgets, output/observer/retry tests and source/dependency inspection; no shared mutable validation state. |
| P10 - Complete independent schema/public corpus and real cultures | Consumers share the validator, not duplicated rules; existing corpus passes. Complete documented public/repeated corpus is partial (F1/T049). |
| P11 - Preserve bootstrap and fail-closed local/CI wiring | Required target inventory, nonempty native test policy, author build before contracts, safe child failures/timeouts, unchanged real-provider jobs. |
| P12 - Executable guide, honest evidence and human-owned delivery | Accepted ADR/amendments, actual guide/timing records, version/breaking/source-rollback guidance, same branch/draft PR and separate final acceptance. |

All twelve principles were assessed. I-III preserve brand, narrow shared
contracts, immutable versioned identity, native calls, reviewed dependencies and
test ownership. IV/V prohibit allocation/physical selection and cross-module
access/atomicity; real module registries/migrations remain 003/004. VI applies
through bounded safe data handling, not a new identity/permission service. VII's
MCP declaration is not tool execution; common protected adapters remain 007/013.
VIII's resource/event/job/provider/realtime runtimes are deliberately absent.
IX introduces no UI journey or remote frontend loader; existing browser CI
remains applicable. X/XI retain portability and actual focused/provider/CI
evidence, but F1 must be resolved before claiming complete feature evidence.
XII/governance preserve amended approved intent, exact MVP scope, one stage and
explicit human acceptance. No implementation MUST contradiction, scope dispute
or unrelated runtime addition was found; evidence is not waived.

## Every existing task assessed

| Tasks | Source/decision/execution evidence and limitation |
| --- | --- |
| T001-T003 | Recorded analysis/checklists and Accepted ADR entry gates; actual seven-project graph, reviewed locks/adoption and recorded locked restore/dependency reconciliation. |
| T004 | Original-byte fixtures and independently expected C01-C34/range recipes exist; complete prescribed public limit/range corpus is partial (F1). |
| T005-T009 | Result tests, fixed immutable errors, defensive model/context, bounded process harness and evidence matrix. |
| T010-T015 | Canonical ID grammar, exact limits, explicit prior comparison, immutable graph and public identity cases. |
| T016-T022 | SemVer/range/metadata/context tests and pure implementations, including all nine kinds, invalid versus incompatible and no resolver/grant. |
| T023 | Public compatibility subset exists and passes; the full documented reversed-range proof is missing from that subset (F1). |
| T024-T025 | Strict original-byte/nested-shape tests and exact offline Ajv schema tests, including independent structural/semantic expectations. |
| T026-T031 | Complete public pipeline, descriptor parity, safe author exits, actual host-reference direction and real-culture repetition exist; prescribed complete-corpus and repeated multi-error proof is partial (F1). |
| T032-T033 | Recorded builds/focused tests and current passing named suites; exact/one-over unit budgets and phase/truncation checks, not substituted for public repetition. |
| T034-T040 | Safe output/actual IO failures, owned observers, retry, bounded-reader provenance regression and actual initialized individual timings. |
| T041-T042 | Inventory/missing-file/child-failure/timeout/nonempty-selection regressions and required named unit/contract steps; current same-command CI. |
| T043-T045 | Executable guide, exact safe documented outcomes, actual workstation timing and seven amended automated guide cases; no human-study claim. |
| T046-T047 | Recorded full 177-step local gate and exact-head eleven-check CI; fresh targeted proof below. Their broad completeness claim is qualified by the newly discovered F1. |
| T048 | Completed implement history and publication on original draft PR, no later-stage or acceptance action. This convergence appends work instead of rewriting prior markers/history. |

## Fresh verification and retained evidence

Selected already installed SDK **10.0.402**, Node **24.22.0** and npm **11.19.1**;
the initial shell selected older tools. No install, pin change or global
configuration mutation was needed.

| Exact command | Observed result |
| --- | --- |
| `python3 scripts/verify.py --suite unit` | Passed, exit 0; all 10 named steps, including platform/host/web/shared/Python units. |
| `python3 scripts/verify.py --suite contract --provider both --disposable-postgres` | Passed, exit 0; all 34 named steps, author/public/schema checks and actual SQLite/PostgreSQL HTTP/OpenAPI/adapter fixtures, scans and owned cleanup. |
| `python3 scripts/check_repository.py` | Passed; repository assets, links, MCP parity and driver state valid. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed; 103 tests. |
| `git diff --check` | Passed; no whitespace errors. |
| `gh run view 39698523107 --json headSha,status,conclusion,url` | Completed/success at assessed SHA `b9d23ffca2f89b7292c1822a4a6318794bacffe3`; all ten product contexts inspected on PR #5. |
| `gh run view 39698522974 --json headSha,status,conclusion,url` | Completed/success at the same assessed SHA; repository checks. |

The local reports identify that assessed revision and have no failed steps.
They remain ignored, with no raw child log, secret, fixture data or build output
published. These 44 fresh named steps are targeted evidence, **not** a new
all-suite run. The implementation report's prior 177-step complete local gate
and assessed-head CI are retained as actual historical executions. Passing
existing selections does not prove coverage of cases those selections omit.
The unit run spanned `2026-10-22T00:39:19Z` to `00:39:30Z`; contract spanned
`00:39:30Z` to `00:40:46Z`. Both reports have `completeGate: false`.
An explicit byte-prefix check against the assessed task file passed: original
T001-T048 bytes unchanged and exactly one new unchecked T049. Final publication
checks and publication-head CI outcomes are recorded in the draft PR after push.

## Append-only outcome and handoff

Appended **T049 only**, under **Phase 7: Convergence**, preserving the original
task bytes and markers. No spec/plan/checklist/analysis/implementation history or
product/test/schema source is rewritten. The driver completes converge with
`--outcome gaps`, selecting **implement for the next invocation**. It does not
execute that remediation, review or acceptance now.

Keep the same branch and [draft PR #5](https://github.com/Stonefly-Labs/Kofte/pull/5).
After implementation closes T049, another converge and final evidence-based
review are still required before explicit human acceptance. No merge or ready
request, production readiness or MVP completion is implied.
