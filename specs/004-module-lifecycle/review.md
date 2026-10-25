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
