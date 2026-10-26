# Final Review: Module Composition and Lifecycle

**Result:** pass

**Date:** 2026-10-25
**Assessed head:** `4893c72a8ee219a84c451073e2f5639fcd29f16b`
**Branch:** `feature/004-module-lifecycle`
**Scope:** all 20 functional requirements, six outcomes, 20 acceptance scenarios,
twelve plan decisions, 86 tasks and twelve constitutional principles.

The read-only `spec-review` agent inspected current source and discriminating
assertions against approved intent. **No new high-confidence implementation
defect or remediation task remains.** Review R1/T086 and prior F1-F4/T081-T085
are closed. The caller independently queried exact-code CI, downloaded all ten
safe reports and reconciled their outcomes and revision attribution.

**This is a substantive review pass, not an all-checks-green conclusion.**
The latest complete local attempt remains **failed/interrupted at 109/111
executed steps**. Its PostgreSQL migration-interruption failure is undiagnosed
and expressly human-deferred. Neither focused passes nor CI replace that
failed result or establish its cause. The residual limitation qualifies
FR-010/015/020, US3.3, SC-004/006 and principle XI.

Only review is executed. All 86 task bytes/markers, approved intent,
reviewer-owned checklists, product/test/verifier source, dependencies and prior
implementation/convergence reports remain unchanged. The complete previous
review is preserved verbatim below. **Explicit human acceptance of numbered
feature 004 is next**, separately on the same draft PR #7 with this limitation
disclosed. No acceptance, readiness, merge request or 005 work is authorized.

## Historical finding closure after T086

Paths in the tables use these prefixes:
**H** = `src/Kofte.Host/Features/ModuleComposition/`;
**P** = `src/Kofte.Platform/Modules/Composition/`;
**I** = `src/Kofte.Infrastructure/Composition/`;
**UH** = `tests/unit/Kofte.Host.Tests/Features/ModuleComposition/`;
**UP** = `tests/unit/Kofte.Platform.Tests/Modules/Composition/`;
**S** = `tests/integration/Kofte.Storage.IntegrationTests/Composition/`;
**C** = `tests/contract/Kofte.Composition.Consumer/Scenarios/`;
**A** = `tests/contract/Kofte.Composition.TestModule/`.

| Finding | Current behavior and discriminating evidence | Decision |
| --- | --- | --- |
| R1/T086: disabled prerequisite metadata shared enabled-edge budget | H`CompositionProtocol.cs:10,36-38` independently admits 2,048 catalog identities and checks the 2,049th overflow sentinel. P`CompositionResolver.cs:114-129` and P`CompositionGraph.cs:15-26` retain the separate enabled-only 1,024-edge cap. UH`CompositionDiagnosticTests.cs:19-68` asserts 1/1,024/1,025/2,048 boundaries, copied inputs, genuine missing-prerequisite transport, forgery refusal and bounded enumeration. | Closed; no published input limit reduced. |
| R1 actual compiled-host parity | S`CompositionProviderTests.cs:13-101` covers five catalogs, both selection forms and both real providers, requiring readiness, exact disabled observations, unchanged inventory, empty stderr and verified exit. `tests/contract/Kofte.Composition.Consumer/DeploymentProgram.cs:206-216,233-260` makes disabled author execution fail the scenario. | Closed by actual host behavior, not resolver acceptance alone. |
| F1/T081: absent disabled operator output | H`CompositionPreflight.cs:77-86`; C`BootScenarios.cs:120-181`; UH`CompositionDiagnosticTests.cs:142-170`: both false/omitted records, fresh attempts, zero disabled effects and contradictory-frame refusal. | Closed. |
| F2/T082: qualified service-name truncation boundary | H`ModuleLifecycleCoordinator.cs:100-105,293-305`; `src/Kofte.Platform/Modules/Lifecycle/ModuleLifecycle.cs:78-89`; UH`ModuleLifecycleTests.cs:445-490`: full owner/local identity, actual binding/invocation/Start/readiness at accepted maxima. | Closed, without hashing or truncation. |
| F4/T084: runtime narrowed service/binding counts | H`ModuleLifecycleCoordinator.cs:100-120`; `ModuleLifecycle.cs:104-108`; UH`ModuleLifecycleTests.cs:513-637`: complete 65/128/256/1,024 sets and one-over refusal. A`Modules/BootModules.cs:241-260` and C`BootScenarios.cs:79-89` require all 127 public prerequisite operations. | Closed; dependency count is not executable-service count. |
| F3/T083/T085: demonstrated normal-stop causes | H`CompositionWorker.cs:263-280` disables unused JSON reload watchers before builder construction and bounds HTTP drain to 500ms. UH`CompositionBudgetTests.cs:15-36` and S`CompositionProviderTests.cs:104-139` distinguish the fixes. C`StopScenarios.cs:174-223` retains incomplete/uncertain refusal. | Closed for demonstrated causes only; not a diagnosis of the deferred migration observation. |

H`CompositionProtocol.cs:99-141` retains current attempt/mode/snapshot,
known-owner checks, restricted missing-prerequisite evidence, eight related
IDs, fixed guidance and disabled-state consistency. No framing, copied
allowlist, storage, cleanup or privacy guarantee is weakened.

## Complete requirement and acceptance mapping

“Supported” means actual implementation/assertions plus the attributed
execution evidence below, not a claim that every local check passed.

| Requirement; scenarios | Source and meaningful acceptance evidence |
| --- | --- |
| FR-001; US1.1/1.3 | H`ModuleCompositionHost.cs:8-17`, H`CompositionPreflight.cs:77-86`; C`BootScenarios.cs:14-42,120-181`, S`CompositionProviderTests.cs:13-101`: usable empty core, both disabled forms, zero activation and unchanged inventory. Supported. |
| FR-002; US1.4/US2.1 | P`CompositionResolver.cs:39-113`; UP`CompositionResolverTests.cs:88-106`, C`ValidationScenarios.cs:99-115`, C`BootScenarios.cs:184-207`: invalid/duplicate/missing deployment never becomes empty success. Supported. |
| FR-003; US2.2/2.3 | P`CompositionGraph.cs:15-97`; UP`CompositionResolverTests.cs:110-138`: missing/disabled/incompatible distinctions, accepted ranges and exact cycle members. Supported. |
| FR-004; US2.3 | P`CompositionResolver.cs:39-63`, P`CompositionDiagnostic.cs:57-81`; UP`CompositionLimitTests.cs:14-213`, C`ValidationScenarios.cs:19-79`: deterministic order, finite bounds, ordered truncation and repeated real cultures. Supported. |
| FR-005; US2.1/2.4 | H`CompositionPreflight.cs:22-86`, H`CompositionWorker.cs:51-94`, I`CompositionStorageInspector.cs:13-77`; S`CompositionPreflightTests.cs:40-70,143-287`: whole-set rejection before factories/allocation, exact inventory equality. Supported. |
| FR-006; US2.4 | P`ContributionValidator.cs:25-141`; UP`ContributionClaimTests.cs:32-151`: ownership/version/capability, reserved/overlapping routes, permission ownership and future-execution refusal. Supported. |
| FR-007; US2.4/US5.4 | H`ModuleSelectionReader.cs:45-206`, H`ModuleConfigurationValidator.cs:11-74`; UH`ModuleSelectionTests.cs:12-170`, UH`ModuleConfigurationTests.cs:13-84`: strict private copied settings and bounded sanitized validation. Supported. |
| FR-008; US1.2/US3.1/US5.1 | H`ModuleLifecycleCoordinator.cs:108-330`, H`CompositionWorker.cs:95-135`; UH`ModuleLifecycleTests.cs:73-138`, S`CompositionProgressTests.cs:18-110`: actual entry/completion, phase barriers and owner progress. Supported. |
| FR-009; US3.1/3.2 | I`CompositionStorageInspector.cs:30-94`, I`ModuleStorageRecipe.cs:43-101`; S`CompositionMigrationTests.cs:20-56,139-156`, C`MigrationScenarios.cs:87-99`: exact assignment/history and zero no-storage allocation/binding. Supported. |
| FR-010; US3.1/3.3/3.4 | I`CompositionStorageSession.cs:29-104`, I`CompositionStoragePreparation.cs:10-62`, I`CompositionStorageVerification.cs:10-40`; S`CompositionMigrationTests.cs:62-136`, C`StopScenarios.cs:132-171`, C`MigrationFailureScenarios.cs:16-117`: stopped exclusive preparation, read-only serving, no replay/revision rewrite and explicit recovery. Deferred failed-check limitation retained. |
| FR-011; US3.3/US5.1 | H`ModuleLifecycleCoordinator.cs:319-330`, H`CompositionHealth.cs:118-137`; UH`ModuleLifecycleTests.cs:194-236`, C`BootScenarios.cs:58-66`: held Start/unhealthy initial sweeps cannot publish readiness. Supported. |
| FR-012; US4.1 | H`ModuleSelectionReader.cs:14-37`, H`CompositionPreflight.cs:77-86`; C`PreservationScenarios.cs:14-53`: three full fresh-process cycles compare original assignments/revisions/rows/history and unrelated state. Supported. |
| FR-013; US4.2/4.3 | P`CompositionGraph.cs:19-26`, I`CompositionStorageInspector.cs:30-33`; C`PreservationScenarios.cs:24-68`, S`CompositionPreflightTests.cs:266-287`, S`CompositionUpgradePreservationTests.cs:13-66`: exact reuse, no cascade and owner-only explicit upgrade. Supported. |
| FR-014; US5.1/5.2 | H`CompositionHealth.cs:34-85,103-137`, H`ModuleLifecycleCoordinator.cs:334-404`; UH`CompositionHealthTests.cs:15-73`, C`HealthScenarios.cs:63-139`: 30s freshness, transitive impact, independent roots and callback-free aggregate HTTP reads. Supported. |
| FR-015; US4.4/US5.3 | H`ModuleLifecycleCoordinator.cs:407-482`, H`CompositionSupervisor.cs:89-170`; UH`ModuleLifecycleTests.cs:100-138,275-350`, C`CorrectionScenarios.cs:25-96`, C`MigrationFailureScenarios.cs:16-117`: reverse partial-entry/LIFO cleanup, preserved stores and fresh reconciled attempts. Deferred failed-check limitation retained. |
| FR-016; US3.4/US5.3 | H`CompositionAttempt.cs:143-205`, H`CompositionCallbackRunner.cs:26-60`, H`CompositionSupervisor.cs:65-170`; UH`CompositionBudgetTests.cs:39-178`, C`StopScenarios.cs:13-96,101-171,202-223`: clipped budgets, seven uncooperative boundaries, tracked descendant exit and explicit unverified-exit refusal. Supported. |
| FR-017; US5.4 | H`CompositionDiagnostics.cs:95-154`, H`CompositionProtocol.cs:63-141`; UH`CompositionDiagnosticTests.cs:19-68,173-350`, C`DiagnosticScenarios.cs:17-225`: useful bounded records, framing/rate refusal, real correlated telemetry and zero canary leaks. Supported. |
| FR-018; US5.4 | H`CompositionSupervisor.cs:245-275`, H`CompositionWorker.cs:34-38`, `src/Kofte.Host/Features/Bootstrap/HealthHandler.cs:15-46`; `tests/integration/Kofte.Host.IntegrationTests/CompositionHealthEndpointTests.cs:57-153`, C`HealthScenarios.cs:105-139`: installation authority, stripped provisioning secrets, unchanged anonymous schema and absent management routes. Supported. |
| FR-019; US1.2 | P`CompositionContracts.cs:19-111`, `src/Kofte.Platform/Modules/Lifecycle/ModuleLifecycle.cs:22-110`; A`Modules/BootModules.cs:32-84,241-260`, C`BootScenarios.cs:69-110`: independently compiled public operations and complete versioned dependency bindings. Supported. |
| FR-020; all 20 scenarios | `scripts/verify.py:1057-1249`, `tests/contract/Kofte.Platform.ContractTests/Composition/CompositionConsumerHarnessTests.cs:35-112`, `scripts/tests/test_verify.py:122-195`: mandatory guide cases/observations and malformed/omitted/decremented/zero-case refusal. Targeted CI supported; latest complete local gate remains failed/interrupted. |
| SC-001 | C`BootScenarios.cs:14-207`, UH`ModuleLifecycleTests.cs:451-490,518-590`, S`CompositionProviderTests.cs:13-101`: real empty/public/disabled behavior through admitted maxima. |
| SC-002 | A`FrozenCompositionCorpus.cs:40-98`, C`ValidationScenarios.cs:19-79`: literal independent expectations, 100 repetitions by ten permutations by two real cultures through both public paths. |
| SC-003 | C`PreservationScenarios.cs:22-53`, S`CompositionUpgradePreservationTests.cs:13-66`: three complete cycles/provider, exact original comparisons and separate explicit-upgrade exception. |
| SC-004 | C`StopScenarios.cs:22-223`, UH`CompositionBudgetTests.cs:39-178`: outcome/budget/exit/refusal, never timeout-as-termination. Deferred migration-interruption failure remains contrary check evidence. |
| SC-005 | C`CorrectionScenarios.cs:25-96`, C`MigrationFailureScenarios.cs:16-117`, C`HealthScenarios.cs:63-115`: named correction, fresh attempt, explicit recovery and current impact. |
| SC-006 | C`DiagnosticScenarios.cs:103-152`: six actual captured channels, useful observations, bounded labels and zero leaks; attributed CI retains browser/startup/failure gates. No passing latest complete-local claim. |

## Design, task and constitutional reconciliation

Plan decision IDs below are distinct from historical review finding R1.

| Plan decisions | Current implementation/evidence |
| --- | --- |
| R1 compiled discovery; R2 deterministic resolution | P`CompositionContracts.cs:19-49`, H`ModuleCompositionHost.cs:8-17`, P`CompositionResolver.cs:39-140`, P`CompositionGraph.cs:15-97`: copied inert catalog, private selection, ordinal graph and separate metadata/edge limits. |
| R3 owner configuration; R4 contribution claims | H`ModuleSelectionReader.cs:14-37,45-206`, H`ModuleConfigurationValidator.cs:11-74`, P`ContributionValidator.cs:25-141`; configuration and contribution negatives above. |
| R5 native DI/barriers | H`ModuleLifecycleCoordinator.cs:108-230,282-330,407-482`; UH`ModuleLifecycleTests.cs:100-138,299-350,451-637`: descriptor audit, one provider, owner scopes, complete bindings and reverse disposal. |
| R6 prepare/serve authority; R7 preservation/recovery | I`CompositionStorageSession.cs:29-104`, I`CompositionStorageInspector.cs:13-94`, I`CompositionStoragePreparation.cs:31-59`; C`PreservationScenarios.cs:22-68`, C`MigrationFailureScenarios.cs:16-117`. Deferred observation retained. |
| R8 health; R9 supervision | H`CompositionHealth.cs:34-85,103-162`, H`CompositionAttempt.cs:143-205`, H`CompositionSupervisor.cs:89-170`; freshness/termination assertions above. |
| R10 observations; R11 independent proof | H`CompositionDiagnostics.cs:95-154`, H`CompositionProtocol.cs:19-141`; separate author/consumer projects, literal corpus and A`Modules/BootModules.cs:32-84,241-260`. |
| R12 retained baseline | `scripts/verify.py:1057-1249`, `.github/workflows/product-checks.yml:13-65`; historical full success, targeted CI and latest failed local attempt remain separately attributed. |

| Tasks assessed | Resulting evidence inspected |
| --- | --- |
| T001-T005 | Approved ADR/dependency adoption, separate public projects and strict owned harness (`docs/development/dependencies.md:22-47`; `tests/integration/Kofte.TestSupport/CompositionConsumerHarness.cs:11-103`). |
| T006-T014 | Copied contracts, private configuration, bounded diagnostics/attempts and read-only storage inspection. |
| T015-T026 | Independent corpus, exact graph/limit/configuration/contribution negatives and whole-set zero-effect preflight. |
| T027-T036 | Native DI/barriers and actual empty/valid/disabled/missing compiled-host boots. |
| T037-T049 | Assigned clean/additive/current preparation, progress, authority, interruption and explicit recovery. Latest failed-check limitation qualifies the verification claim. |
| T050-T057 | Exact three-cycle original-data preservation, dependent-disable refusal, complete-set recovery and owner-only upgrades. |
| T058-T072 | Current health/HTTP, correction, actual termination, bounded private captures and strict observations. |
| T073-T077 | Verifier selectors/minima/guide commands, retained browser/startup/failure gates and truthful docs. |
| T078-T080 | Historical full-gate/report/publication evidence; markers do not relabel the latest local failure. Current caller owns report/ledger/docs/publication checks. |
| T081-T086 | Actual behavior and focused discriminating regressions in the closure table; no remaining task or new remediation. |

| Principle | Assessment |
| --- | --- |
| I | Real optional empty host, Köfte branding and no mandatory Hoard/cloud/IaC. |
| II | Stable versioned composition, whole-set validation, disable preservation and honest trusted-plugin/health semantics. |
| III | Native DI/direct handlers, reviewed DI-abstractions edge and test source in dedicated test projects; no production-linked tests, mediator or mapper. |
| IV | Accepted immutable registry/schema/file/marker/history paths reused. PostgreSQL UNIQUE/63-byte enforcement and assigned model/history remain tested on real providers. |
| V | Earlier owner commits remain real; no global rollback or cross-owner table/transaction contract. Explicit complete-set recovery remains separate. |
| VI | Installation-only authority, owner settings/services/stores, provisioning secrets stripped from serving and private diagnostics/aggregate-only anonymity. |
| VII | Inert namespaced MCP claims/collisions checked; executable tools refuse. Authenticated MCP remains 007/013. |
| VIII | Attempts/health are ephemeral, not durable jobs/events/resources or realtime runtime. Later obligations excluded, not waived. |
| IX | No new lifecycle page or remote frontend code. Existing aggregate schema and attributed browser/accessibility checks retained. |
| X | Both real providers, pinned portable tools, explicit stopped preparation, finite supervision and documented recovery responsibilities. No unsupported Windows/escaped-descendant guarantee. |
| XI | Meaningful public/provider/process/privacy assertions and attributed evidence. Deferred failed local check remains failed/undiagnosed, not replaced by CI. |
| XII | Scope remains 004/approved ADR 0006 on one branch/draft PR; review, acceptance and human merge remain distinct. |

No new business REST/UI/MCP/job/provider/realtime operation requires actor/team
authorization parity here; absence of anonymous lifecycle administration is
tested instead. Future authentication/MCP/jobs, full reference-module,
Kubernetes and post-MVP work are not demanded or claimed complete.

## Independently attributed execution evidence

No new product suite is needed for this report/ledger/docs-only stage.
The preceding convergence's exact focused commands/results remain historical:
protocol 50/50, limit 29/29, actual both-provider compiled-host 10/10 and strict
harness/corpus 35/35, **124 passing cases**, zero skips. The provider selection
requires all 40 validate/serve combinations and verified owned exit.
These are not new executions by this review.

The caller freshly queried
[Product run 40232738175](https://github.com/Stonefly-Labs/Kofte/actions/runs/40232738175)
and [Repository run 40232738167](https://github.com/Stonefly-Labs/Kofte/actions/runs/40232738167):
all eleven contexts pass at exact fix commit
`c749acc22345e4ab868dfc84e30c599208d953bd`.
`gh run download 40232738175 --pattern 'verification-*' --dir <session-files>/review-ci`
freshly retrieves ten safe reports outside the repository. JSON reconciliation
requires every report and step to pass and every `completeGate` to remain false:
quality 20, unit 14, SQLite integration 9, PostgreSQL integration 12, recovery 43,
contract 101, browser 16, privacy 41, startup 12 and failure propagation 18:
**286/286 targeted steps**, including 24 guide commands and 24 strict
observation checks.

Every report names checkout `81a5b26b9da97e1b1f30859a6c30a947a82181c1`.
It is absent from the local object database; the caller verified its parents
through GitHub's Git commit API instead: main
`3a5deff380655e72eb6b08ebc1ef694ed5676f4a` plus exact fix `c749acc...`.
A source/test/script/workflow/dependency/project-manifest diff against the
assessed head is empty. The existing assessed-head docs-only CI was still
running when first queried; it is not substituted for exact-fix evidence.
Publication-head automatic checks are separate from this historical evidence.

Fresh review-stage checks:

| Exact command | Observed result |
| --- | --- |
| `python3 scripts/check_repository.py` | Passed assets, links, MCP parity and running review state; completed state rechecked before publication. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed 119/119, 3.013s. |
| `npm run format:check` | Passed all configured files. |
| `git diff --check` | Passed. |

`git diff --cached --check` is also required before publication. Protected
artifact comparison confirms unchanged intent/tasks/checklists and the exact
historical-review suffix. No new full local/provider/browser gate is claimed.

## Deferred failed observation and acceptance boundary

Exact case:
`PostgresCompositionAuthorityTests.KilledLaterMigrationRetainsEarlierCommitAndOriginalDataUntilExplicitAcceptedRecovery`.
The safe local report
`integration-postgres-fb12cfa1e3354655abd558c697f88410.json` records
`composition-integration-postgres`, exit 2, duration 216.981s and failure
correlation `ceef1e00d59e499d8ea7a5a2bbc40af8`.
The failed/interrupted complete attempt passed 109/111 executed steps across
six reports, all `completeGate: false`; it did not pass the full gate.

The [explicit human direction and fact record](implementation.md#human-directed-deferral-and-implementation-handoff)
retain eight passes/one failure in nine overlapping selections, not nine
pipeline reruns or a statistical failure rate. **The original failing
assertion and cause remain unavailable.** Isolated/local-recovery/CI passes
neither diagnose the failure nor establish harmlessness.
No broad integration/recovery/all-suite run or investigation of that case is
performed in this review, respecting the documented human direction.

This is an unresolved verification observation, not a demonstrated new
implementation defect with a safe established fix. No speculative remediation
task, scheduling explanation, assertion/deadline relaxation or test suppression
is introduced. Progression with documented deferral is not a general waiver
of later acceptance/release evidence. If the case fails again or relevant
migration/recovery work begins, capture the exact failing assertion and safe
phase evidence before assigning a cause.

**Next stage: accept**, in a separate invocation. Present this passing
substantive review **and the failed/undiagnosed local limitation** for explicit
human acceptance of numbered feature 004. Silence, autopilot, checked tasks,
the earlier acceptance request and a passing review are not acceptance.
Keep the existing PR draft; do not request an intermediate merge.

---

# Historical final review before T086 (preserved verbatim)

# Final Review: Module Composition and Lifecycle

**Result:** changes

**Date:** 2026-10-25
**Assessed head:** `55d2f735c63e5a671ba82e3dfc6a98e6258aaa52`
**Branch:** `feature/004-module-lifecycle`
**Scope:** all 20 functional requirements, six outcomes, 20 acceptance scenarios,
twelve plan decisions, 85 existing tasks and twelve constitutional principles.

The read-only `spec-review` assessment inspected actual implementation and
meaningful assertions, not task markers alone. The caller independently
reproduced its one HIGH finding through a separately compiled public-host
consumer. Prior convergence F1-F4 remain closed; **review R1 is a distinct
disabled-catalog admission defect**. Green existing checks do not cover it.

Only review is executed. Exactly unchecked **T086** is appended; all 85 prior
task bytes/markers, approved intent, reviewed checklists, implementation and
convergence evidence, and product/test/verifier source remain unchanged.
**Implement T086 is next**, separately on the same draft PR. Acceptance was
requested before this missing prerequisite review; the finding now blocks a
passing review and feature acceptance. No acceptance record, readiness,
intermediate merge or later feature is authorized.

## Finding R1: disabled dependencies consume an enabled-graph protocol limit

**Severity:** HIGH. Valid disabled deployments can prevent an otherwise valid
empty or independently selected composition from validating or serving.
No data loss, disabled execution or authorization bypass is demonstrated.

| Evidence | Observed behavior |
| --- | --- |
| `src/Kofte.Host/Features/ModuleComposition/CompositionSupervisor.cs:23-28,230-234` | Collects prerequisite identities from every deployed valid manifest, including disabled owners, before worker admission. |
| `src/Kofte.Host/Features/ModuleComposition/CompositionProtocol.cs:35-37` | Copies at most 1,025 identities and refuses more than 1,024 with `composition_observation_invalid`. |
| `src/Kofte.Host/Features/ModuleComposition/CompositionWorker.cs:41-44` | Uses the same all-catalog prerequisite construction. |
| `src/Kofte.Platform/Modules/Composition/CompositionResolver.cs:114-129` | Correctly resolves dependency edges only for enabled owners. |
| `contracts/composition-v1.md:112-116`; `plan.md:151-154` | Admit 32 deployed modules and 64 dependencies per manifest; the separate 1,024-edge limit applies to the enabled graph. |

The smallest reproduced case deploys 17 distinct compatible no-storage modules.
The first sixteen each declare 64 distinct absent prerequisite identities; the
seventeenth declares one further identity. With every deployment disabled,
there are **1,025 catalog prerequisite identities but zero enabled edges**.
The public resolver accepts this composition. The actual host rejects it
before launching a worker because diagnostic metadata incorrectly shares the
enabled-edge limit. An unrelated enabled no-storage module does not avoid it.

### Caller-executed reproduction

A session-only `ReviewProbe.csproj` references the real `Kofte.Host` project,
constructs ordinary public `ModuleDeployment`/`ModuleSelection` inputs, calls
`CompositionResolver.Resolve`, and invokes actual `ModuleCommand.RunAsync`
in validate and serve modes. It supplies private selection files and an
isolated absent data root; no repository source or real installation is edited.
The normalized command is
`dotnet run --project <session-files>/review-probe/ReviewProbe.csproj -c Release`.
The project and captured output remain session-local, not production assets.

| Catalog case | Deployed | Distinct disabled prerequisites | Enabled edges | Result for omitted and explicit-false selections, validate and serve |
| --- | --- | --- | --- | --- |
| First overflowing metadata union | 17 disabled | 1,025 | 0 | Resolver accepted; actual host exit 1, `composition_observation_invalid`. |
| Unrelated enabled owner | 17 disabled + 1 enabled | 1,025 | 0 | Same failure. |
| Maximum disabled catalog | 32 disabled | 2,048 | 0 | Same failure. |
| Maximum catalog with enabled owner | 31 disabled + 1 enabled | 1,984 | 0 | Same failure. |

**16/16 cases reproduce the defect**, with zero registration/runtime callbacks
and no created data root. Probe exit 0 means the expected defect was reproduced,
**not** that composition succeeded. The session-only probe emits two CA1416
warnings for Unix permission APIs; it ran on supported macOS, not Windows.
No provider operation or worker was reached. Both-provider successful readiness,
preservation and verified clean stop for these inputs remain **unverified and
required after remediation**, not inferred from this pre-admission reproduction.

### Required remediation

T086 must reconcile parent/worker diagnostic prerequisite admission without
reducing published limits. Either retain all-catalog metadata with its actual
bounded 2,048 maximum and 2,049 overflow sentinel, or consistently collect
declarations from enabled source owners only. Preserve the separate 1,024
enabled-edge bound, copied inputs, identity checks and forged-related-owner
refusal. If filtering, do not filter prerequisite targets by presence/enabled
state or derive the list only from an accepted graph: missing and disabled
prerequisites of enabled owners still need transportable rejection diagnostics.

Regressions must distinguish 1,024/1,025 metadata identities, cover the full
32-deployment bound, both disabled forms and an unrelated enabled owner, and
compare public resolution with actual compiled-host validate/serve on both
providers. Require readiness, correct disabled observations, zero disabled
callbacks/store effects, exact inventory and verified clean stop. Preserve
genuine overflow, missing-prerequisite and forged-frame negatives.

No new product decision, design approval or constitutional amendment is needed.

## Requirement and acceptance coverage

Paths below use these prefixes:

- **H:** `src/Kofte.Host/Features/ModuleComposition/`
- **P:** `src/Kofte.Platform/Modules/Composition/`
- **I:** `src/Kofte.Infrastructure/Composition/`
- **UH:** `tests/unit/Kofte.Host.Tests/Features/ModuleComposition/`
- **UP:** `tests/unit/Kofte.Platform.Tests/Modules/Composition/`
- **C:** `tests/contract/Kofte.Composition.Consumer/Scenarios/`
- **S:** `tests/integration/Kofte.Storage.IntegrationTests/Composition/`
- **A:** `tests/contract/Kofte.Composition.TestModule/`

All twenty scenarios are mapped below. R1 qualifies otherwise supported
empty/disabled and bounded-admission coverage; it does not erase existing proof.

| Requirement; scenarios | Actual implementation | Meaningful assertion evidence |
| --- | --- | --- |
| FR-001; US1.1/1.3 | H`ModuleCompositionHost.cs:10-17`, H`CompositionPreflight.cs:77-86` | C`BootScenarios.cs:14-42,120-207`: endpoints, explicit/omitted disabled observations, zero effects. **Partial: R1.** |
| FR-002; US1.4/US2.1 | P`CompositionResolver.cs:39-113` | UP`CompositionResolverTests.cs:88-106`, C`ValidationScenarios.cs:99-115`, C`BootScenarios.cs:184-207`: malformed, duplicate, incompatible and absent implementation refusals. |
| FR-003; US2.2/2.3 | P`CompositionGraph.cs:15-97` | UP`CompositionResolverTests.cs:110-138`: missing/disabled/range/prerelease/build semantics and distinct SCC membership. |
| FR-004; US2.3 | P`CompositionResolver.cs:39-63`, P`CompositionGraph.cs:81-97`, P`CompositionDiagnostic.cs:57-81` | UP`CompositionLimitTests.cs:15-175`, UP`CompositionDiagnosticTests.cs:86-151`, C`ValidationScenarios.cs:19-79`: bounds, literal expectations, repeated culture decisions and truncation. **Partial downstream: R1.** |
| FR-005; US2.1/2.4 | H`CompositionPreflight.cs:22-86`, H`CompositionWorker.cs:51-94`, I`CompositionStorageInspector.cs:16-77` | S`CompositionPreflightTests.cs:40-70,143-287`: invalid recipes, incomplete/foreign/missing ownership and declaration changes refuse without mutation. |
| FR-006; US2.4 | P`ContributionValidator.cs:25-141` | UP`ContributionClaimTests.cs:32-134`: owner/version/capability, route reservations/overlap, permission ownership and unsupported execution. |
| FR-007; US2.4/US5.4 | H`ModuleSelectionReader.cs:44-206`, H`ModuleConfigurationValidator.cs:14-74` | UH`ModuleSelectionTests.cs:13-170`, UH`ModuleConfigurationTests.cs:13-84`, UH`CompositionPreflightTests.cs:125-194`: private bounded copied settings, no environment fallback, safe guidance. |
| FR-008; US1.2/US3.1/US5.1 | H`ModuleLifecycleCoordinator.cs:108-330`, H`CompositionWorker.cs:95-135` | UH`ModuleLifecycleTests.cs:73-138`, S`CompositionProgressTests.cs:18-116`: actual owner work, phase barriers and honest migration progress. |
| FR-009; US3.1/3.2 | I`CompositionStorageInspector.cs:16-94`, I`ModuleStorageRecipe.cs:41-101` | S`CompositionMigrationTests.cs:20-56,139-156`, S`CompositionRecipeTests.cs:96-114`: actual assignments/history, no-storage absence and wrong-owner refusal. |
| FR-010; US3.1/3.3/3.4 | I`CompositionStorageSession.cs:29-104`, I`CompositionStoragePreparation.cs:10-62`, I`CompositionStorageVerification.cs:10-40` | S`CompositionMigrationTests.cs:62-136`, S`CompositionAuthorityTests.cs:24-140`, C`MigrationFailureScenarios.cs:16-117`: current verification, contention, earlier commits and explicit recovery. |
| FR-011; US3.3/US5.1 | H`ModuleLifecycleCoordinator.cs:319-330`, H`CompositionHealth.cs:118-137` | UH`ModuleLifecycleTests.cs:194-236`, C`BootScenarios.cs:58-66`, C`MigrationFailureScenarios.cs:29-41`: held Start/unhealthy initial sweeps never publish readiness. |
| FR-012; US4.1 | H`ModuleSelectionReader.cs:16-37`, H`CompositionPreflight.cs:77-86` | C`PreservationScenarios.cs:14-53`, UH`CompositionObservationTests.cs:11-40`: fresh attempts and exact preservation. |
| FR-013; US4.2/4.3 | P`CompositionGraph.cs:19-26`, I`CompositionStorageInspector.cs:30-33` | C`PreservationScenarios.cs:24-68`, S`CompositionPreflightTests.cs:266-287`, S`CompositionUpgradePreservationTests.cs:13-66`: coherent selection and requested-owner-only upgrade. |
| FR-014; US5.2 | H`CompositionHealth.cs:34-85`, H`ModuleLifecycleCoordinator.cs:334-404` | UH`CompositionHealthTests.cs:15-83`, C`HealthScenarios.cs:63-137`: current own/effective/transitive health, independent roots and expiry without HTTP-triggered callbacks. |
| FR-015; US4.4/US5.3 | H`ModuleLifecycleCoordinator.cs:407-482`, H`CompositionSupervisor.cs:89-170` | UH`ModuleLifecycleTests.cs:100-138,275-350`, C`StopScenarios.cs:22-223`, C`MigrationFailureScenarios.cs:16-117`: reverse partial-entry/LIFO cleanup, actual exit and explicit reconciliation. |
| FR-016; US3.4/US5.3 | H`CompositionAttempt.cs:143-205`, H`CompositionCallbackRunner.cs:26-60`, H`CompositionSupervisor.cs:65-170` | UH`CompositionBudgetTests.cs:39-178`, C`StopScenarios.cs:22-96`: clipped totals, ignoring-token boundaries and independently verified descendants. |
| FR-017; US5.4 | H`CompositionDiagnostics.cs:95-154`, H`CompositionProtocol.cs:63-143` | UH`CompositionDiagnosticTests.cs:20-350`, C`DiagnosticScenarios.cs:17-225`: correlated/durable useful records, framing refusal, real spans/events/metrics and captured privacy. |
| FR-018; US5.4 | H`CompositionSupervisor.cs:245-275`, H`CompositionWorker.cs:34-38`; `src/Kofte.Host/Features/Bootstrap/HealthHandler.cs:15-49` | C`HealthScenarios.cs:116-139`; `tests/integration/Kofte.Host.IntegrationTests/CompositionHealthEndpointTests.cs:57-153`: stripped authority, aggregate-only schema, absent management routes and rejected direct worker. |
| FR-019; US1.2 | P`CompositionContracts.cs:19-111`; `src/Kofte.Platform/Modules/Lifecycle/ModuleLifecycle.cs:15-110` | A`Modules/BootModules.cs:32-84,250-285`, C`BootScenarios.cs:69-90`: separately compiled public operations and exact dependency bindings, without production-to-test references. |
| FR-020; all twenty scenarios | `scripts/verify.py:1057-1187`; executable guide/contracts | `tests/contract/Kofte.Platform.ContractTests/Composition/CompositionConsumerHarnessTests.cs:48-71`; `scripts/tests/test_verify.py:122-195`: mandatory observations, omission/decrement and malformed/zero-test refusal. **Missing disabled-catalog interaction: R1.** |

| Outcome | Actual acceptance evidence |
| --- | --- |
| SC-001 | C`BootScenarios.cs:14-207`, UH`ModuleLifecycleTests.cs:451-490,518-590`: empty/public/disabled behavior and exact complete service identities/counts. **Qualified by R1.** |
| SC-002 | C`ValidationScenarios.cs:19-79`, A`FrozenCompositionCorpus.cs:40-98`: literal independent expectations, 100 repetitions, ten permutations, two actual cultures and two public paths. |
| SC-003 | C`PreservationScenarios.cs:14-53`, C`MigrationScenarios.cs:187-217`: three complete original-data cycles, fresh processes/attempts and exact assignments/data/history/unrelated state. |
| SC-004 | C`StopScenarios.cs:22-223`, UH`CompositionBudgetTests.cs:39-178`: clipped budgets, actual root/descendant exits and honest incomplete/unverified refusal. |
| SC-005 | C`CorrectionScenarios.cs:25-96`, C`MigrationFailureScenarios.cs:16-117`, C`HealthScenarios.cs:63-115`: five named corrections, explicit uncertain-write recovery and fresh health. |
| SC-006 | C`DiagnosticScenarios.cs:103-152`, `scripts/verify.py:1185-1249`: six actual captured channels, useful bounded evidence and retained aggregate/browser/startup contracts. |

## Plan, tasks and constitutional reconciliation

All twelve decisions and 85 existing tasks were assessed. Plan decision IDs
below are distinct from the review finding R1.

| Plan decision | Mechanism and task coverage |
| --- | --- |
| R1 compiled discovery | Explicit host/catalog and independent author assembly; T003/T006/T009/T028-T036/T081. Review R1 qualifies disabled-catalog admission. |
| R2 deterministic bounds | Resolver/graph/ordered diagnostics/frozen corpus; T015-T021/T025-T026. Review R1 exposes a narrower downstream protocol bound. |
| R3 copied owner configuration | Selection/settings validation; T007-T009/T017/T022/T052. |
| R4 honest contributions | Owner/version/collision checks and complete exports; T006/T018/T023-T024/T059/T064/T082/T084. |
| R5 native DI/barriers | Descriptor audit, ordinary owner scopes, complete versioned bindings and partial-entry cleanup; T007/T027-T031/T037/T044/T051/T053/T082/T084. |
| R6 preflight/preparation | Complete inventory/recipes and retained read/exclusive authority; T013-T014/T024/T038-T044. |
| R7 preservation/recovery | Immutable assignments/history, interrupted intents and explicit recovery; T039-T057/T067. |
| R8 fresh health | Own/effective samples, expiry and aggregate projection; T058-T059/T062-T066/T081. |
| R9 bounded supervision | Owned exit/refusal, HTTP drain/disposal and included reserves; T004/T012/T024/T032/T039/T041/T053/T060/T065/T068/T083/T085. Review R1 prevents admission before this work. |
| R10 safe observations | Fixed private records, framed allowlists and bounded metrics; T010-T012/T061-T062/T065/T069-T071/T081/T083. Review R1 requires a correct metadata allowance. |
| R11 independent evidence | Actual public operations and strict semantic observations; T003-T005/T015/T025/T028/T034/T045-T046/T054/T066-T070/T074/T081-T082/T084. Add R1 interaction coverage. |
| R12 retained gates | Inventories/selectors, complete-gate history and actual CI; T073-T080/T083/T085. Green existing coverage does not close R1. |

Complete task reconciliation: T001-T005 approval/dependency/project/fixture/harness;
T006-T014 copied contracts/configuration/protocol/budgets/storage inspection;
T015-T026 resolver/claims/preflight/public negatives (**partial at R1**);
T027-T036 actual DI/public boots (**partial at R1**); T037-T049 both-provider
preparation/current verification/interruption/recovery; T050-T057 exact
preservation/coherent disable/cleanup; T058-T072 health/HTTP/fault/correction/
privacy/strict evidence; T073-T080 verifier/guides/retained gates/docs;
T081-T085 prior corrections below. No marker is treated as proof of an untested
combination. T086 is the sole newly appended unchecked remediation.

| Principle | Assessment |
| --- | --- |
| I product/brand | Portable optional host and naming preserved; review R1 violates valid optional/disabled composition. |
| II modular extensibility | Stable contracts, validated lifecycle and honest trust; review R1 is an admission defect. |
| III vertical slices | Native DI/minimal shared contracts and test-project-owned test source; no mediator/mapper dependency. |
| IV storage ownership | Reuses immutable registry, actual schema/file ownership and schema-local/per-file histories; no allocation change. |
| V transaction honesty | Earlier owner commits remain real; interrupted writes need explicit recovery, not implied global rollback. |
| VI safety | Private operator authority/configuration/output and anonymous aggregate-only health; no new business authorization surface. |
| VII MCP | Claims/collision metadata only; executable authenticated MCP belongs to later specs. |
| VIII durable work | Lifecycle observations are ephemeral; no durable resource/job/event/realtime implementation is claimed. |
| IX frontend | No new visual lifecycle journey/remote code; retained accessible bootstrap status remains covered. |
| X portability/operations | Both providers and bounded operations remain required; review R1 blocks valid startup. |
| XI evidence | Real provider/process/public/privacy assertions exist; new interaction coverage is required for R1. |
| XII scope/traceability | No future-feature requirement or scope expansion; review, acceptance and human merge remain distinct gates. |

Inherited ownership checks include `StorageAllocator.cs:11-26`, PostgreSQL
`001_registry.sql:20-23`, `StorageRegistry.Binding.cs:17-29` and
`AssignedContextFactory.cs:20-43`. No cross-module table or transaction contract
is introduced.

## Prior convergence findings remain closed

- **F1/T081:** C`BootScenarios.cs:145-181` requires actual explicit/omitted
  disabled observations; UH`CompositionDiagnosticTests.cs:119-146` rejects
  contradictory frames. Review R1 is a larger disabled-catalog metadata bound.
- **F2/T082:** UH`ModuleLifecycleTests.cs:451-490` and C`BootScenarios.cs:82-85`
  preserve full accepted owner-qualified identities through actual Start.
- **F4/T084:** UH`ModuleLifecycleTests.cs:518-637` covers 65/128 prerequisite
  services, 256 independent exports, 1,024 exports across eight prerequisites,
  and one-over refusal. The public example requires all 127 prerequisite
  operations across 129 total exports.
- **F3/T083/T085:** H`CompositionWorker.cs:263-280` retains 500-ms internal HTTP
  drain within unchanged cleanup limits; S`CompositionProviderTests.cs:13-45`
  and UH`CompositionBudgetTests.cs:15-36` distinguish incomplete-request stop
  from unused watcher disposal. Intentional incomplete/lost-authority refusal
  remains asserted. No blanket cause for every historical failure is claimed.

## Executed checks and evidence boundaries

Fresh local commands at assessed head, using the existing .NET 10.0.402
toolchain, Node 24.22.0/npm 11.19.1 and `DOTNET_PROCESSOR_COUNT=4`:

| Exact command | Result |
| --- | --- |
| `python3 scripts/verify.py --suite quality` | Passed 20/20 steps, 33.358s; report `quality-none-757490801a1a42d6921493f44da26812.json`. |
| `python3 scripts/verify.py --suite unit` | Passed 14/14 steps, 36.969s; report `unit-none-188e7afaa4f547129c645b9d1dd07b1c.json`. |

The caller queried actual [Product checks 40228205187](https://github.com/Stonefly-Labs/Kofte/actions/runs/40228205187)
and [Repository checks 40228205211](https://github.com/Stonefly-Labs/Kofte/actions/runs/40228205211):
all eleven contexts passed at exact assessed head. All ten safe product
artifacts were independently downloaded and their actual passed steps and
public observations reconciled, not inferred from green labels.

| Exact assessed-head CI command | Result |
| --- | --- |
| `python3 scripts/verify.py --suite quality` | Passed 20/20 steps. |
| `python3 scripts/verify.py --suite unit` | Passed 14/14 steps. |
| `python3 scripts/verify.py --suite integration --provider sqlite` | Passed 9/9 steps. |
| `python3 scripts/verify.py --suite integration --provider postgres --disposable-postgres` | Passed 12/12 steps. |
| `python3 scripts/verify.py --suite recovery --provider both --disposable-postgres` | Passed 43/43 steps. |
| `python3 scripts/verify.py --suite contract --provider both --disposable-postgres` | Passed 101/101 steps. |
| `python3 scripts/verify.py --suite browser` | Passed 16/16 steps. |
| `python3 scripts/verify.py --suite privacy --provider both --disposable-postgres` | Passed 41/41 steps. |
| `python3 scripts/verify.py --suite startup --warm-starts 10` | Passed 12/12 steps. |
| `python3 scripts/verify.py --suite failure-propagation --disposable-postgres` | Passed 18/18 steps, including five deliberately failed child exercises whose expected/observed outcomes both equal `failed`. |

These **286/286 targeted CI steps** have `completeGate: false`. Their synthetic
checkout `618ff04ffd852604b41715f3f08a0216d0eb436b` has verified parents main
`3a5deff380655e72eb6b08ebc1ef694ed5676f4a` and assessed head
`55d2f735c63e5a671ba82e3dfc6a98e6258aaa52`; it is not a human merge.

The contract artifact contains 24 actual successful guide commands and 24
strict observation checks. Both sequential `all` runs cover eleven scenarios:
389,419 SQLite/389,424 PostgreSQL assertions; all 127 prerequisite operations;
two explicit/two omitted disabled observations; 84,000 deterministic comparisons
per provider; three original-state cycles, six fresh processes/attempts, twelve
original assignment/data/history and six unrelated-state comparisons; five
named corrections; seven verified timeout root exits and one tracked descendant;
six real diagnostic channels with six zero-leak comparisons. None covers R1.

Prior convergence's 106/106 composition and 33/33 strict-harness checks, and
the retained 286-step complete local gate, remain historical evidence. **No new
complete local/provider/browser gate or human usability study is claimed.**
Historical failures remain failed and their diagnostic limitations are retained.
Final report/ledger repository checks and publication-head CI outcomes belong
to the PR's separate publication evidence.

## Human handoff and limitations

Authentication/bootstrap administration, centralized team/resource authorization,
executable MCP, frontend extensions, durable settings/audit/events/jobs/realtime,
the complete reference module and production/Kubernetes deployment remain later
features. Trusted in-process modules are not sandboxed or hot-unloadable.
Configured developer tools and reserved directories are not runtime capabilities.
Passing an individual feature would not mean production readiness or MVP completion.

**Acceptance is blocked by review R1.** Implement T086 in a separate invocation,
then converge and review again on the original branch/draft PR. A passing review
must precede explicit human feature acceptance and any readiness/merge request.
This checkpoint neither repairs the product nor records acceptance.
