# Convergence after T086: 004 - Module Composition and Lifecycle

**Result:** pass

**Stage:** converge only; begun 2026-10-25T22:11:30+00:00.
**Assessed revision:** `697c3331e0c18846a8c619da68aa9b4e7956618c`.
**Branch / checkpoint:** `feature/004-module-lifecycle`,
[draft PR #7](https://github.com/Stonefly-Labs/Kofte/pull/7).
**Intent:** `spec.md`, `plan.md` and all 86 tasks, governed by constitution
v2.0.1. Referenced contracts/model explain the approved design; checked tasks,
implementation reports and CI are evidence, not substitutes for source assessment.

## Decision

**Converged: no new actionable gap remains in the specified implementation.**
Final-review R1/T086 is closed by the corrected bounded protocol, public
resolution assertions and actual both-provider compiled-host validation/serving.
Prior F1-F4/T081-T085 remain closed against the resulting implementation.
All 20 functional requirements, six buildable outcomes, 20 acceptance
scenarios, twelve plan decisions, 86 tasks and twelve principles were assessed.
No task is appended: `tasks.md` remains byte-for-byte unchanged.

**The separately deferred PostgreSQL observation remains deferred, not fixed.**
The human-directed disposition in
[implementation.md](implementation.md#human-directed-deferral-and-implementation-handoff)
permits progression without investigating that observation now. The local
complete attempt remains failed/interrupted, 109/111 executed steps; its
original assertion and cause remain unknown. This convergence does not rerun
that case, diagnose it, claim a passing new local all-suite, disable a test
or waive later review/release gates. The frozen eight-pass/one-failure snapshot
is not a statistical flakiness claim. Review must retain this limitation.

**Review is next**, separately on this same branch/draft PR. This assessment
does not perform final review, accept numbered feature 004, mark ready, request
merge or begin 005. The entire previous convergence report is preserved
verbatim below.

## Findings summary

| ID   | Gap type | Severity | Source                   | Evidence                                                                                      | Remaining work                                                           |
| ---- | -------- | -------- | ------------------------ | --------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------ |
| None | None     | None     | Complete inventory below | Current source/assertions, fresh focused execution and independently reconciled fix-commit CI | No new remediation task; review separately with the deferred limitation. |

Checked: **46/46** FR/outcome/scenario items, **12/12** plan decisions,
**86/86** tasks and **12/12** principles.
New findings by type: missing 0, partial 0, contradicts 0, unrequested 0.
New findings by severity: CRITICAL 0, HIGH 0, MEDIUM 0, LOW 0.
The known deferred observation is not silently converted into passing evidence
or reopened as an investigation task contrary to the human's direction.

## R1/T086 closure and retained corrections

`src/Kofte.Host/Features/ModuleComposition/CompositionProtocol.cs:10-39`
now copies at most 2,049 prerequisite metadata entries and refuses overflow
beyond the derived 32-deployment-by-64-dependency maximum of 2,048.
`CompositionSupervisor.CatalogPrerequisites` and `CompositionWorker` use the
same all-catalog source. This diagnostic allowlist is not the enabled graph:
`CompositionResolver`/`CompositionGraph` retain 32 deployed, 16 enabled,
accepted manifest limits and the separate 1,024-edge cap.

Protocol related identities remain limited to eight per frame. A prerequisite
outside the deployed catalog is permitted only as related evidence for a
rejected `composition_dependency_missing` diagnostic, not as a runtime owner
or health dependency. Attempt/snapshot/mode/field/owner checks, copied
collections, frame/record budgets and forged-identity refusal remain intact.
`CompositionDiagnosticTests.cs:19-68` tests 1/1,024/1,025/2,048 identities,
source mutation, round-trip evidence, forged identities and bounded infinite
enumeration stopping at exactly 2,049.

`CompositionLimitTests.cs:14-40` independently resolves the five structural
catalogs with both omitted and explicit-false selections, requiring exact
disabled/order sets, zero enabled edges and zero callbacks.
`CompositionProviderTests.cs:13-105` runs matching fixed compiled catalogs
through actual validate/serve on both providers: 1,024 and 1,025 prerequisites,
1,025 plus an unrelated enabled root, 32 disabled deployments/2,048 identities
and 31 disabled plus one enabled/1,984 identities. Both selection forms require
all 40 command/serving combinations, exact disabled records, aggregate ready,
unchanged complete inventory, empty stderr and verified root/descendant exit.
Disabled validators/registrars/factories throw if invoked; mixed catalogs
also require the independent no-storage computation result 8.
Fresh execution passes all ten provider cases with zero skips.

F1/T081 remains closed by `CompositionPreflight` projecting current explicit/
omitted disabled observations and `BootScenarios.DisabledMissingAsync`
requiring them before publication, with zero disabled effects.
F2/T082 and F4/T084 remain closed by full owner-qualified names and complete
128-per-owner/1,024-total exports in `ModuleLifecycleCoordinator` and copied
activation bindings in `ModuleLifecycle.cs`. Actual lifecycle tests require
exact operations/versions/identities through Start/readiness at 65/128/256/1,024
bindings; the independent public boot requires all 127 prerequisite operations.
F3/T083/T085 remains closed by the internal 500-ms HTTP drain and disabled
unused JSON reload watchers before worker-builder creation, with causal
regressions and unchanged incomplete/uncertain refusal. The unrelated deferred
migration-interruption observation is not attributed to those historical causes.

## Complete intent assessment

### Requirements, outcomes and all twenty scenarios

| Intent                                        | Current source and discriminating evidence                                                                                                                                                                                                                                                                                                                                                                                                 |
| --------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| FR-001/002; US1.1-4; SC-001                   | Explicit `ModuleCompositionHost`/compiled catalog, strict selection and resolver; `BootScenarios` requires actual v0/v1 empty endpoints, independent service results, disabled zero effects and missing-implementation refusal. T086 additionally proves valid full disabled catalogs reach actual readiness unchanged.                                                                                                                    |
| FR-003/004; US2.1-3; SC-002                   | Accepted manifest/range validation, ordinal Kahn frontier, bounded SCC/diagnostic ordering and explicit overflow; `CompositionResolverTests`, `CompositionLimitTests` and the frozen public corpus compare exact external orders/rejections across 100 repetitions, ten permutations and two real cultures.                                                                                                                                |
| FR-005/006/007; US2.1-4                       | `CompositionPreflight`, `ModuleSelectionReader`, owner-only configuration validation and `ContributionValidator` enforce whole-set admission before callbacks, declared field/type guidance, owner/version/capability/route collisions and unsupported execution. Real preflight/recipe tests require zero factories/writes and exact inventory equality on refusal.                                                                       |
| FR-008/011; US3.1/2, US5.1; SC-004            | `ModuleLifecycleCoordinator` audits native DI and completes all-registration/all-initialization/storage/start/initial-health barriers before publication. Per-owner storage progress is entered/completed at actual boundaries; lifecycle/progress tests and public migration/boot assertions reject premature readiness or preparation activation.                                                                                        |
| FR-009/010; US3.1-4                           | `CompositionStorageSession`, inspector, preparation and verification retain accepted authority, whole-inventory/all-recipe preflight, owner-bound bindings and assigned migrations. `MigrationScenarios`, recipe/authority/recovery tests assert exact histories, no serving writes, earlier commits and explicit uncertain-owner recovery; no-storage receives no fabricated binding. Deferred local observation remains as stated above. |
| FR-012/013; US4.1-4; SC-003                   | `PreservationScenarios` executes three complete fresh-process cycles/provider and compares every original assignment/revision, row/history and unrelated inventory after every leg. Selection tests refuse dependent-disable without cascade; upgrade preservation tests isolate an explicitly requested owner's change.                                                                                                                   |
| FR-014; US5.1/2; SC-005                       | `CompositionHealth` atomically derives current-attempt own/effective health, individual 30-second freshness and transitive impact; serialized coordinator sweeps retain callback/sweep/schedule bounds. Observation/health tests and actual HTTP `HealthScenarios` preserve independent roots, aggregate schema and no author callback per request.                                                                                        |
| FR-015/016; US3.3/4, US4.4, US5.3; SC-004/005 | Attempt/callback runner, reverse partial-entry Stop/LIFO cleanup, worker and process-group supervisor retain clipped monotonic budgets, included reserves and verified exit/refusal. `StopScenarios` exercises seven uncooperative boundaries, actual tracked descendants, cancellation and authority loss; correction requires fresh verified attempts and explicit migration recovery.                                                   |
| FR-017/018; US5.4; SC-006                     | Fixed-code diagnostics, bounded frames/record rates and safe identity/field allowlists retain actionable current correlation/guidance/durability. `DiagnosticScenarios` asserts useful records/events/spans, bounded dimensions and zero seeded leaks across six channels. `HealthHandler` projects only existing aggregate fields and exposes no lifecycle-control route.                                                                 |
| FR-019/020; all stories; SC-001-006           | Separate author/consumer projects use public contracts and eleven executable scenarios. C#/Python result validation rejects every omitted/decremented mandatory observation. Verifier named targets/minima retain accepted provider/recovery/privacy/browser/startup/failure gates; exact fix-commit CI and fresh focused evidence are distinguished from the failed local attempt.                                                        |

### Twelve plan decisions and every existing task

| Decision                            | Present mechanism and task coverage                                                                                                                                                      |
| ----------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| R1 compiled discovery               | Copied inert catalog, private selection, no disabled execution; T003/T006/T009/T028-T029/T033-T036/T081/T086.                                                                            |
| R2 deterministic bounded resolution | Accepted manifests/ranges, stable SCC/order/rejections and distinct diagnostic-metadata/graph bounds; T015-T021/T025-T026/T086.                                                          |
| R3 owner configuration              | Strict immutable original-byte snapshot, declared owner fields and supervised validator; T007-T009/T017/T022/T052.                                                                       |
| R4 contribution ownership           | Capability/owner/version/local identity/route checks, inert future execution and complete admitted counts; T006/T018/T023-T024/T059/T064/T082/T084.                                      |
| R5 native DI and barriers           | Descriptor audit, one provider/owner scopes, explicit prerequisite operations and reverse partial-entry disposal; T007/T027-T031/T037/T044/T051/T053/T082/T084.                          |
| R6 read-only/offline separation     | Held read/exclusive authority, whole-set inventory/recipe checks and no provisioning credentials in serving; T013-T014/T024/T038-T044.                                                   |
| R7 preserved stores/recovery        | Accepted bindings/history, original-state cycles, earlier commits and deliberate complete-set/forward recovery; T039-T057/T067.                                                          |
| R8 fresh health                     | Atomic current-attempt own/effective/disabled observations, individual expiry and snapshot-only aggregate HTTP; T058-T059/T062-T066/T081.                                                |
| R9 bounded supervision              | Monotonic clipped totals, cooperative/termination reserves, process ownership and explicit refusal; T004/T012/T024/T032/T039/T041/T053/T060/T065/T068/T083/T085/T086.                    |
| R10 private diagnostics             | Fixed records, copied framed allowlists, bounded useful telemetry and truthful storage links; T010-T012/T061-T062/T065/T069-T071/T081/T083/T086.                                         |
| R11 independent public proof        | Separate compiled authors, frozen expectations, exact operation bindings and strict measured scenarios; T003-T005/T015/T025/T028/T034/T045-T046/T054/T066-T070/T074/T081-T082/T084/T086. |
| R12 retained evidence               | Verifier inventories/floors, exact local/CI distinctions, delivered guides and explicit deferred limitation; T073-T080/T083/T085/T086.                                                   |

All task groups were assessed against resulting behavior, not checkbox claims:
T001-T005 approval/dependency/project/owned-harness boundaries;
T006-T014 copied contracts/configuration/diagnostics/budgets/storage inspection;
T015-T026 graph/settings/contribution/no-effect rejection;
T027-T036 native DI/public empty/valid/disabled/missing startup;
T037-T049 assigned prepare/verify/migration/authority/recovery;
T050-T057 exact preservation/selection/reverse cleanup/owner upgrade;
T058-T072 health/HTTP/fault/correction/private captures/strict observations;
T073-T080 verifier/documentation/full-evidence/publication obligations;
T081-T085 historical corrective chains; and T086 all-catalog protocol admission.
The full-gate history and human-directed limitation remain explicit evidence,
not newly satisfied by task markers or narrower runs.

### Twelve constitutional principles

| Principle | Applicable invariant and retained scope                                                                                                                                                    |
| --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| I         | MIT/brand, portable empty core and no mandatory Hoard/cloud/IaC.                                                                                                                           |
| II        | Versioned trusted composition, complete validation, disabled preservation and honest health/failure; no sandbox or hot-unload promise.                                                     |
| III       | Native DI/direct handlers, existing minimal assemblies, reviewed DI edge/locks and test source in dedicated test projects.                                                                 |
| IV        | Accepted immutable registry/UNIQUE/collision/schema/file/path/marker/history mechanisms reused and tested on actual providers.                                                             |
| V         | Owner-local commits survive later failures; explicit recovery, no cross-store ACID or automatic irreversible startup work.                                                                 |
| VI        | Installation-only authority, owner-scoped settings/services/stores, stripped serving credentials and safe operator/public channels.                                                        |
| VII       | Namespaced inert MCP reservations; unsupported execution refuses. Authenticated tool runtimes remain 007/013.                                                                              |
| VIII      | Ephemeral attempts/health, not durable jobs/events/resources/realtime; those later obligations remain unimplemented, not waived.                                                           |
| IX        | No lifecycle page or remote code; existing aggregate schema, web adapter and browser/accessibility gates retained.                                                                         |
| X         | Pinned supported local/Linux tools, real providers, finite owned supervision and explicit operational/recovery limitations.                                                                |
| XI        | Actual public/process/provider assertions and exact evidence; failed/unavailable evidence never relabeled. The deferred anomaly is retained for review, not a general test/release waiver. |
| XII       | 004-only assessed intent, approved ADR/reviewer markers unchanged, one stage/branch/draft PR and separate review/explicit acceptance/human merge.                                          |

## Fresh execution and independently verified retained evidence

Commands used the existing installed SDK 10.0.402/runtime 10.0.13, Node
24.22.0/npm 11.19.1 and owned disposable PostgreSQL on macOS arm64:

```bash
export SPECIFY_FEATURE_DIRECTORY=specs/004-module-lifecycle
export DOTNET_ROOT="$PWD/artifacts/toolchain/dotnet-10.0.402"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_PROCESSOR_COUNT=4 DOTNET_CLI_TELEMETRY_OPTOUT=1
export KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true
```

The initial ambient `dotnet --version` could not resolve the pinned SDK
(only 10.0.203 at its default location); selecting the already installed
repository-documented SDK resolved it. No tool/package was installed and no
pin was changed.

| Exact fresh command                                                                                                                                                                                             | Result                                                                                                          |
| --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-class '*CompositionDiagnosticTests*' --minimum-expected-tests 50`                                                           | Passed 50/50, zero skipped, 530ms summary.                                                                      |
| `dotnet test --project tests/unit/Kofte.Platform.Tests --no-restore -c Release --filter-class '*CompositionLimitTests*' --minimum-expected-tests 29`                                                            | Passed 29/29, zero skipped, 2.618s summary.                                                                     |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-method '*DisabledCatalogPrerequisiteMetadata*' --minimum-expected-tests 10`                            | Passed 10/10, zero skipped, 42.137s summary; both providers and all 40 validate/serve combinations.             |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionConsumerHarnessTests*' --filter-class '*CompositionCorpus*' --minimum-expected-tests 35` | Passed 35/35, zero skipped, 2.633s summary; strict omission/decrement refusal and repeated real-culture corpus. |

These **124 passing focused test cases** do not constitute a new full local
gate or a rerun/investigation of the deferred migration-interruption case.

Independently queried and downloaded
[Product checks 40232738175](https://github.com/Stonefly-Labs/Kofte/actions/runs/40232738175)
and queried
[Repository checks 40232738167](https://github.com/Stonefly-Labs/Kofte/actions/runs/40232738167)
confirm all eleven contexts pass at actual fix commit
`c749acc22345e4ab868dfc84e30c599208d953bd`. Present product/test/verifier
source and dependency/project manifests are unchanged from that commit.
The commit API verifies synthetic test checkout
`81a5b26b9da97e1b1f30859a6c30a947a82181c1` has parents updated main
`3a5deff380655e72eb6b08ebc1ef694ed5676f4a` and that exact fix commit.
All ten downloaded safe reports name that checkout and independently reconcile:
quality 20, unit 14, integration SQLite 9/PostgreSQL 12, recovery 43, contract
101, browser 16, privacy 41, startup 12 and failure propagation 18:
**286/286 targeted steps**, including **24 actual composition guide commands
and 24 strict observation checks**. Every CI report is `completeGate: false`.
No synthetic checkout is a human merge or a passing local all-suite.

The original failed local report was reopened only to verify the documented
facts: `integration-postgres-fb12cfa1e3354655abd558c697f88410.json`,
`composition-integration-postgres`, exit 2, 216.981s,
correlation `ceef1e00d59e499d8ea7a5a2bbc40af8`.
No new assertion/cause is inferred or investigation performed.
Current publication-head automatic CI is separate; exact publication checks
and their observed outcomes are recorded in the PR.

## Persistence and handoff

No `.specify/extensions.yml` exists; no before/after convergence hook is
configured. Began once on the verified clean original branch/open draft PR,
after recorded implementation completion, with explicit feature-directory context.
Task SHA-256 remains
`1edad3eee4b8e81d8490bc12591d65e9497517a1dc08fab9c98764ecdc595bda`;
all 86 markers and every task byte are unchanged.
Spec, plan, approved design, reviewed checklists, implementation/history,
product/test/verifier source, dependencies and generated contracts are unchanged.
Only this report, current progress documentation and the driver checkpoint
are published together. Complete only **converge / pass**, selecting **review**.
Passing final review and explicit human acceptance remain separate requirements;
retain the deferred observation and do not request an intermediate merge.

---

# Convergence after T084-T085: 004 - Module Composition and Lifecycle

**Result:** pass

**Stage:** converge only; begun 2026-10-25T19:44:46+00:00.
**Assessed revision:** `9d872a5e0c1ce4598f362e5148a21c32d74283ba`.
**Branch / checkpoint:** `feature/004-module-lifecycle`,
[draft PR #7](https://github.com/Stonefly-Labs/Kofte/pull/7).
**Intent:** `spec.md`, `plan.md` and all 85 existing tasks, governed by
constitution v2.0.1. Referenced model/contracts explain the approved decisions;
implementation reports, checked markers and CI are evidence, not new intent.

## Decision

**Converged: no actionable gap remains in the specified scope.** F1/T081
(disabled output), F2/T082 (qualified service identities), F4/T084 (complete
executable counts) and F3/T083/T085 (normal stop/disposal) are closed by actual
source, discriminating assertions and current execution evidence.

All 20 functional requirements, six buildable outcomes, 20 acceptance
scenarios, twelve plan decisions, 85 tasks and twelve principles were assessed.
There are **zero missing, partial, contradicting or unrequested findings**;
severity counts are CRITICAL 0, HIGH 0, MEDIUM 0 and LOW 0.
`tasks.md` remains **byte-for-byte unchanged**: no empty convergence phase,
new task, rewritten description or checkbox change.

**Review is next**, in a separate invocation on this same branch/draft PR.
This assessment does not execute final review, accept numbered feature 004,
mark the PR ready, request an intermediate merge or begin 005.
The entire previous convergence report is preserved verbatim below, including
its failed commands and historical findings.

## Findings summary

| ID   | Gap type | Severity | Source                   | Evidence                                                                | Remaining work                                 |
| ---- | -------- | -------- | ------------------------ | ----------------------------------------------------------------------- | ---------------------------------------------- |
| None | None     | None     | Complete inventory below | Current source/assertions, fresh focused execution and assessed-head CI | None identified; proceed separately to review. |

### Closure of the remaining findings

**F4/T084: admitted counts now survive actual lifecycle.**
`ModuleLifecycleCoordinator.cs:100-120` lazily copies at most 1,025 exports
before enforcing 1,024 enabled total and 128 per owner. It retains complete
owner-qualified identities, versions and service types; Start supplies the
prerequisites' actual scoped operations (`:292-305`).
`ModuleLifecycle.cs:104-108` copies up to 1,024 activation bindings and refuses
the first overflow. Module-dependency counts are not service-export counts.

The actual lifecycle theory in
`tests/unit/Kofte.Host.Tests/Features/ModuleComposition/ModuleLifecycleTests.cs:513-590`
checks 65/128 services from one prerequisite, 256 across independent roots
and 1,024 across eight prerequisites. It requires registration, Initialize,
Start, fresh healthy readiness and reverse cleanup, comparing every binding's
identity/version/operation instance and denying cross-owner DI lookup.
The two one-over cases (`:610-637`) reject before any registrar/runtime.
`CompositionLifecycleContractTests.cs:17-44` proves immutable copied bindings
and refusal after exactly 1,025 entries of an infinite source.

The separately compiled public author exports 127 Alpha services, alongside
Beta/NoStorage's two exports. Beta checks every binding and operation during
supervised Start. `BootScenarios.cs:79-89` requires actual boundary identity
and `prerequisiteServiceComparisons: 127`, rather than declaration acceptance
or a positive generic count. Fresh both-provider integration executes that
public example; C#/Python strict observations also require the evidence.
Fresh 33/33 harness regressions reject omission/decrement of every required
field for standalone and `all` results. No count is truncated or valid input
narrowed to conceal the original defect.

**F3/T083/T085: normal cleanup now has causal correction and passing evidence.**
`CompositionWorker.cs:263-273` disables unused default JSON reload watchers
before slim-builder construction, then clears default configuration sources.
The actual-worker-builder regression in `CompositionBudgetTests.cs:15-36`
proves that no watcher has been initialized and disposes the actual host
through the unchanged bounded callback runner. The implementation report
retains its discriminating red/green evidence and the observed pre-DI
file-provider disposal boundary; an isolated successful retry alone is not
the causal argument.

The 500-ms internal HTTP drain remains inside the one-second cleanup action.
`CompositionAttempt`/`CompositionCallbackRunner` retain the original work,
cooperative-cleanup and termination reserves. `CompositionWorker` still
reports private causal cleanup codes and terminal incomplete refusal;
`CompositionSupervisor` still verifies owned exit rather than equating
cancelled waiting or root exit with complete termination.

Fresh broad integration passes **106/106**, zero skipped, including both
providers' three-cycle preservation, named corrections, incomplete HTTP
request stop and deliberate uncooperative/incomplete refusals, plus actual
PostgreSQL authority loss and tracked-descendant exit. Exact original stores,
history and inventories remain asserted. Assessed-head local complete-gate
and Linux CI evidence also pass as distinguished below. This closes the
current scoped cause/evidence gap without claiming that every historical
failed invocation had the same cause or that future OS termination is guaranteed.

F1/T081 remains closed by real explicit/omitted disabled records under fresh
attempts, zero disabled effects and protocol tampering/omission refusal.
F2/T082 remains closed by complete owner-qualified names, including a
128-character local name and maximum-length accepted owner, through actual
Start/readiness and the independent public example.

## Complete intent assessment

| Inventory                  | Assessed                                           | Result                                                                                         |
| -------------------------- | -------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| Functional requirements    | 20/20                                              | Current source and positive/negative assertions satisfy the scoped obligations.                |
| Buildable success criteria | 6/6                                                | Determinism, preservation, bounded failure/correction and private useful output are evidenced. |
| Acceptance scenarios       | 20/20                                              | All five stories' four scenarios assessed, not inferred from checked tasks.                    |
| Plan decisions             | 12/12                                              | R1-R12 implemented within the approved boundaries.                                             |
| Existing tasks             | 85/85                                              | Original tasks and every corrective chain assessed against resulting behavior.                 |
| Constitution principles    | 12/12                                              | Applicable invariants checked; later-feature duties remain scoped, not waived.                 |
| Findings by type           | missing 0; partial 0; contradicts 0; unrequested 0 | No appended task.                                                                              |
| Findings by severity       | CRITICAL 0; HIGH 0; MEDIUM 0; LOW 0                | No unresolved finding.                                                                         |

### Requirements, scenarios and outcomes

| Intent                                        | Current source and discriminating assertions                                                                                                                                                                                                                                                                                                               |
| --------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| FR-001/002; US1.1-4; SC-001                   | Explicit `ModuleCompositionHost`/selection/catalog and resolver/worker; `BootScenarios` exercises real empty v0/v1 endpoints, independent operations, disabled/omitted observations, missing implementation and unchanged full inventories. F1/F2/F4 are closed as above.                                                                                  |
| FR-003/004; US2.1-3; SC-002                   | Accepted version ranges, ordinal Kahn frontier, SCC diagnostics and bounded rejection in resolver/graph/diagnostic types; resolver/limit tests and the frozen public corpus compare independently expected orders/rejections across 100 repetitions, ten permutations, two actual cultures and author/host paths.                                          |
| FR-005/006/007; US2.1-4                       | Strict original-byte selection and owner-only settings; contribution capability/version/owner/route reservations and unsupported execution; complete read-only physical/model/recipe preflight. Validation/preflight/recipe tests require zero mutating callbacks and exact unchanged inventories on refusal.                                              |
| FR-008/011; US3.1/2, US5.1; SC-004            | Audited native registration, owner scopes and coordinator whole-phase barriers; actual per-owner migration progress and initial fresh health precede atomic publication. Lifecycle/progress tests and public migration/boot examples reject partial completion/readiness.                                                                                  |
| FR-009/010; US3.1-4                           | Storage session/inspector/preparation/verification reuse accepted registry, binding and assigned migrations under held authority. Real migration/recipe/authority/recovery cases verify exact current histories, no serving writes, retained earlier commits and explicit uncertain-owner recovery. No-storage has no fabricated binding/migration.        |
| FR-012/013; US4.1-4; SC-003                   | `PreservationScenarios` performs three complete fresh-process cycles per provider, comparing original assignments, rows, history/revision and unrelated state after each leg. Invalid dependent-disable refuses without cascade; separate owner-only upgrade tests verify only its requested changes.                                                      |
| FR-014; US5.1/2; SC-005                       | Atomic current-attempt own/effective samples, individual 30-second freshness, serialized bounded sweeps and transitive impact/independent roots in `CompositionHealth`/coordinator. Health/observation tests and actual HTTP examples prove unchanged aggregate schema and no author callback on requests.                                                 |
| FR-015/016; US3.3/4, US4.4, US5.3; SC-004/005 | Monotonic clipped budgets, authority checks, reverse partial-entry/LIFO cleanup, owned-process supervision and explicit reconciliation. Stop/correction/authority tests exercise seven ignoring-token boundaries, cancellation, real session loss, separate tracked-descendant proof and corrected fresh attempts. F3 is closed without weakening refusal. |
| FR-017/018; US5.4; SC-006                     | Fixed-code records, current admission/attempt tokens, framed owner/field allowlists, useful guidance and storage durability links; six-channel privacy captures and bounded metrics/output tests require both safe evidence and zero seeded leaks. Bootstrap HTTP remains aggregate with no lifecycle-control route.                                       |
| FR-019/020; all stories; SC-001-006           | Separate public author/consumer projects, eleven executable scenarios, C#/Python strict semantic observations and omission rejection; named verifier inventories retain foundation provider/recovery/privacy/browser/startup/failure gates and documented local/CI evidence. No full-reference-module or release claim.                                    |

### Plan decisions and all tasks

| Decision                            | Current mechanism and task coverage                                                                                                                                                         |
| ----------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| R1 compiled discovery               | Copied inert explicit catalog, private selection and no disabled constructors/callbacks; T003/T006/T009/T028-T029/T033-T036/T081.                                                           |
| R2 deterministic bounded resolution | Accepted manifests/ranges, SCCs, ordinal frontier and bounded ordered diagnostics; T015-T021/T025-T026.                                                                                     |
| R3 owner configuration              | One strict immutable admitted snapshot, known field/type guidance and supervised pure validator; T007-T009/T017/T022/T052.                                                                  |
| R4 honest contribution ownership    | Capability/owner/version/global route and local identity checks; inert future categories; complete executable counts; T006/T018/T023-T024/T059/T064/T082/T084.                              |
| R5 native DI/phase barriers         | Descriptor audit, one provider, owner scopes/restricted services, versioned complete prerequisite bindings and partial-entry cleanup; T007/T027-T031/T037/T044/T051/T053/T082/T084.         |
| R6 read-only/offline separation     | Complete inventory/all-recipe checks before mutation, retained read/exclusive authority, no provisioner credential in serving; T013-T014/T024/T038-T044.                                    |
| R7 preserved stores/recovery        | Accepted assignments/current histories, exact original-state cycles and explicit interrupted/full-set recovery; T039-T057/T067.                                                             |
| R8 fresh health                     | Current own/effective/disabled observations, individual expiry and snapshot-only aggregate health; T058-T059/T062-T066/T081.                                                                |
| R9 actual bounded supervision       | Included reserves, sequential owned cleanup and verified process termination/refusal; T004/T012/T024/T032/T039/T041/T053/T060/T065/T068/T083/T085.                                          |
| R10 useful private diagnostics      | Fixed records, allowlisted frames, correlation/durability and bounded safe telemetry; T010-T012/T061-T062/T065/T069-T071/T081/T083.                                                         |
| R11 independent public proof        | Separate assemblies, frozen expectations and real typed operations with mandatory strict measured observations; T003-T005/T015/T025/T028/T034/T045-T046/T054/T066-T070/T074/T081-T082/T084. |
| R12 retained evidence baseline      | Required verifier inventories, complete gate and actual CI, truthful failed-run history and delivered docs; T073-T080/T083/T085.                                                            |

| Task set  | Resulting implementation/evidence assessed                                                                                                                               |
| --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| T001-T005 | Existing approved ADR/analysis/reviewed checklist, separately reviewed DI edge/locks/notices, dedicated assemblies, owned fixtures and strict harness.                   |
| T006-T014 | Copied public configuration/lifecycle/observation types, strict private file, record/protocol/budget primitives and read-only actual inventory/model inspection.         |
| T015-T026 | Independent graph/limits/cultures, safe settings/claims and supervised whole-set no-effect validation/correction.                                                        |
| T027-T036 | Audited native DI, public operations, empty/valid/disabled/missing boots and whole-set healthy publication.                                                              |
| T037-T049 | Actual both-provider clean/additive/current assigned migrations, all-recipe barrier, contention/session loss/killed later owner and explicit recovery/serve guide.       |
| T050-T057 | Exact three-cycle preservation, coherent dependent selection, reverse/partial cleanup and separate requested owner upgrade.                                              |
| T058-T072 | Current/stale/dependency health, aggregate HTTP, actual bounded fault/correction/exit, useful six-channel private captures and strict result refusal.                    |
| T073-T080 | Existing named targets/minima, all public-guide commands, retained complete local/CI and scope/dependency/ownership documentation; historical failed runs remain failed. |
| T081-T082 | Actual disabled projection and full qualified identity boundaries, including independently consumed public evidence.                                                     |
| T083-T085 | HTTP drain/private cleanup boundary plus complete executable counts, demonstrated unused-watcher correction and current successful broad provider/process evidence.      |

### Constitutional assessment

| Principle      | Applicable evidence / preserved boundary                                                                                                                                              |
| -------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| I              | MIT/brand and portable module-free core retained; no mandatory Hoard/cloud/IaC.                                                                                                       |
| II             | Versioned trusted module contracts, complete validation/lifecycle, preserved disable data and honest failure/degraded states; no sandbox/hot-unload claim.                            |
| III            | Native DI/direct handlers, minimal existing assemblies and dedicated test-project source; reviewed dependency edge/locks/licenses retained.                                           |
| IV             | Accepted immutable registry/UNIQUE/collision/schema/file/path/physical-marker/history mechanisms reused and exercised on both actual providers.                                       |
| V              | Owner-local earlier commits and interrupted intents survive later failure; explicit recovery, no cross-store ACID or automatic irreversible startup effects.                          |
| VI             | Installation-only authority, owner settings/services/store, stripped serving credentials and safe useful private/public telemetry boundaries.                                         |
| VII            | Inert namespaced MCP declarations/collision validation; executable tools rejected. Authentication/tool operations remain 007/013, not waived.                                         |
| VIII           | Ephemeral lifecycle observations, not durable jobs/events/resources/realtime. Those runtimes and delivery/actor obligations remain later features.                                    |
| IX             | No lifecycle page or remote JavaScript; existing aggregate schema/web adapter/browser/accessibility evidence retained.                                                                |
| X              | Supported pinned local/Linux tooling, both providers, finite supervision and explicit recovery/operator responsibilities; no mandatory deployment platform.                           |
| XI             | Actual provider/process/public/privacy assertions, focused corrective regressions and independently inspected retained full-gate/CI evidence. Unverifiable termination stays failure. |
| XII/governance | 004 scope only, original branch/draft PR, unchanged approved intent/reviewer markers/tasks, one stage and separate review/explicit human acceptance/human merge.                      |

## Fresh verification and retained full-gate evidence

Commands use the existing repository-local .NET SDK 10.0.402/runtime 10.0.13,
Node 24.22.0/npm 11.19.1 on macOS arm64. PostgreSQL is explicitly opted-in,
fixture-owned disposable infrastructure, never developer/production storage:

```bash
export DOTNET_ROOT="$PWD/artifacts/toolchain/dotnet-10.0.402"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_PROCESSOR_COUNT=4 DOTNET_CLI_TELEMETRY_OPTOUT=1
export KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true
```

| Exact fresh command                                                                                                                                                        | Result                                                                                                                                                                                                 |
| -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `python3 scripts/verify.py --suite quality`                                                                                                                                | Passed all 20 steps, 34.015s: locked restore/build, .NET format, Prettier/ESLint/TypeScript, contract generation and dependency reconciliation included.                                               |
| `python3 scripts/verify.py --suite unit`                                                                                                                                   | Passed all 14 steps, 34.828s, including actual host/platform composition boundaries and retained unit suites.                                                                                          |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 106`               | Passed 106/106, zero failed/skipped, exit 0; test summary 3m 14.403s. Includes both providers' public boot/count, migration, preservation, correction, cleanup, authority, recovery and privacy cases. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionConsumerHarnessTests*' --minimum-expected-tests 33` | Passed 33/33, zero failed/skipped, exit 0; 848ms. Every mandatory standalone/`all` observation is omission/decrement tested.                                                                           |

Fresh safe reports are
`artifacts/verification/quality-none-dac13f26011749468bbf7c066f79c412.json`
and `unit-none-182bdb2547184edd87aada262055074d.json`, both assessed at
`9d872a5e0c1ce4598f362e5148a21c32d74283ba`, targeted/`completeGate: false`.
This convergence does **not** claim a newly executed complete local all-suite,
browser/startup gate or human usability study.

The ten actual local reports named in the T084-T085 implementation increment
were independently reopened: all **286 steps pass**, each `completeGate: true`.
They accurately record precommit base `004b34b` plus the tested working-tree
increment, not an assertion that the old commit contains its corrections.

Actual assessed-head [Product checks 40207096791](https://github.com/Stonefly-Labs/Kofte/actions/runs/40207096791)
and [Repository checks 40207096786](https://github.com/Stonefly-Labs/Kofte/actions/runs/40207096786)
pass all eleven contexts at `9d872a5e0c1ce4598f362e5148a21c32d74283ba`.
All ten safe product artifacts were independently downloaded and reconciled:
quality 20, unit 14, integration SQLite 9/PostgreSQL 12, recovery 43, contract
101, browser 16, privacy 41, startup 12 and failure propagation 18;
**286/286 passed steps**. These CI reports remain targeted/`completeGate: false`,
not a combined local gate.

Their test checkout `83ff2a94b9fd1af571a49ad33084abe7df0ee920` has verified
parents main `3a5deff380655e72eb6b08ebc1ef694ed5676f4a` and the exact assessed
head. This is GitHub's synthetic test checkout, not a human merge.
Contract evidence includes **24 actual guide commands plus 24 strict
observation checks**, including both providers' sequential `all`:
389,419 SQLite / 389,424 PostgreSQL assertions, all eleven scenarios.
Each requires 127 prerequisite operations, two explicit/two omitted disabled
observations, 84,000 deterministic comparisons, three preservation cycles,
six fresh processes/attempts, twelve original assignment/data/history
comparisons, six unrelated comparisons, five named corrections, seven verified
timeout root exits, one tracked-descendant exit and six zero-leak channel checks.
No historical failed invocation is erased, diagnosed by inference or retried
into success by this assessment.

## Persistence and handoff

No `.specify/extensions.yml` exists; no before/after convergence hook is
configured. The clean original branch/open draft PR and recorded completed
implementation were verified before beginning converge exactly once.
Every downstream command explicitly selects `specs/004-module-lifecycle`.

The unchanged task SHA-256 remains
`66dd9ff25d731ebf7de7de942040148af9d72d19286202028a1ea1207fabf9b3`;
all 85 markers remain checked. Approved spec/plan/research/model/contracts/
guide/analysis/ADR, implementation evidence and both reviewed checklists
are unchanged. Product/test/verifier source, dependencies and generated
contracts are untouched. Only this report, progress documentation and the
driver checkpoint are published together after required repository/Python/
whitespace/format checks; their exact outcomes are recorded in the PR.

Complete only **converge / pass**, selecting **review** for a separate
invocation on the same feature branch/draft PR. Final review and explicit
human acceptance of numbered feature 004 remain required. No self-acceptance,
PR readiness, merge request, later stage or new feature is implied.

---

# Convergence after T082-T083: 004 - Module Composition and Lifecycle

**Result:** gaps

**Stage:** converge only; begun 2026-10-25T12:55:02+00:00.
**Assessed revision:** `74074f9b6bac009403e3a0d7c5bf5ca6987feb90`.
**Branch / checkpoint:** `feature/004-module-lifecycle`,
[draft PR #7](https://github.com/Stonefly-Labs/Kofte/pull/7).
**Intent:** `spec.md`, `plan.md` and all 83 existing tasks, governed by
constitution v2.0.1. Contracts/model explain their bounds; checked markers,
historical reports and CI cannot substitute for actual behavior.

## Decision

**F1/T081 and F2/T082 are closed.** Explicit and omitted disabled owners
reach the actual private operator channel. Full owner-qualified binding names
now preserve inclusive accepted owner/local-name lengths through actual Start
and readiness, including the independent public module.

**F3 remains open after T083.** The demonstrated incomplete-HTTP-request
shutdown correction and private causal records are real, but fresh broad
integration still fails two ordinary expected-success paths. Their new safe
records identify **host disposal**, not HTTP stop, as the failing boundary.
No underlying exception, deadline or fixture/scheduling cause is established.

**New F4** reproduces accepted executable contribution counts failing later
registration or activation: global exports are capped at 128 and prerequisite
service bindings at 64, below the admitted composition contribution limits.
Append only **T084-T085 under Phase 11**, high priority first. **Implement
is next**, in a separate invocation on this same branch/draft PR.

No application/test/verifier source, dependency, generated contract, approved
intent, reviewer checklist or implementation history changes here. The entire
pre-stage convergence report is preserved verbatim below.

## Findings

| ID | Gap type | Severity | Source | Evidence | Remaining work |
| --- | --- | --- | --- | --- | --- |
| F4 | partial | HIGH | FR-004/006/008/019/020; US1.2; SC-001; R4/R5/R11; T007/T016/T018/T023/T028/T030/T034 | Public resolution accepts 65 prerequisite exports, but activation fails; 129 total executable exports across two owners fail registration despite accepted per-owner/global contribution bounds. | T084: preserve complete bounded exports/bindings through actual lifecycle and public regression evidence. |
| F3 | partial | MEDIUM | FR-012/013/015/016/017/020 evidence; US4.4/US5.3; SC-003/005; R9/R12; T060/T068/T075/T078/T083 | Fresh both-provider integration passes 104/106; SQLite preservation and PostgreSQL correction fail expected-success exit with private `composition_host_dispose_incomplete`. | T085: establish and correct the remaining disposal cause without relaxing cleanup, deadlines or evidence. |

### F4: admitted contribution counts do not survive executable composition

The source admits **128 claims per owner / 1,024 enabled total** in
`src/Kofte.Platform/Modules/Composition/CompositionResolver.cs:115-117`,
consistent with plan Technical Context, T018/T023 and the composition contract's
admission table. Backend service identities, service types and ownership are
validated by `ContributionValidator`; required dependency identities and service
exports are different quantities.

`src/Kofte.Host/Features/ModuleComposition/ModuleLifecycleCoordinator.cs:101-105`
materializes default executable exports with `Take(129)`, then `:116-119`
rejects more than 128 exports for the entire composition. Separately,
`src/Kofte.Platform/Modules/Lifecycle/ModuleLifecycle.cs:104-107` copies only
65 bindings and rejects more than 64. The coordinator supplies every declared
prerequisite service export during Start (`:294-300`), so a single valid
prerequisite with 65 executable services violates that constructor's limit.
Neither narrower runtime bound appears in approved admission intent.

A session-only .NET executable used public platform/host lifecycle contracts,
native DI, two no-storage owners, unique closed generic service types, valid
manifests/claims and ordinary immediately completing healthy callbacks. It
resolved, registered, initialized, marked the no-storage boundary, started,
checked readiness and reversed cleanup for each fresh composition:

| Alpha exports | Beta exports | Beta requires Alpha | Resolver accepted | Actual ready | Failure |
| --- | --- | --- | --- | --- | --- |
| 64 | 0 | Yes | Yes | Yes; Beta receives exactly 64 bindings | None |
| 65 | 0 | Yes | Yes | No; Beta Start is not entered | `composition_activation_failed` |
| 128 | 0 | No | Yes | Yes | None |
| 128 | 1 | No | Yes | No; registration refuses | `composition_registration_failed` |

All cases stay within two enabled owners, one dependency edge, at most 128
claims/types per owner and at most 129 total contributions. No provider,
private API, repository source change, raw output fault or injected timeout is
needed. This is independent of F2's now-correct name length and F3's process
cleanup failures. The initial probe used an incorrect `name` manifest field
and was rejected before lifecycle; that run is not reproduction evidence.
The corrected `displayName` fixture produces the table above.

The probe exits 0 after reporting all observations; that means the diagnostic
experiment completed, not that the two failing compositions passed. T084 must
preserve complete identities/versions/operations under approved finite bounds,
not truncate service lists, narrow public inputs or merely reject later with
a more descriptive message. Persistent regressions belong to implement.

### F3: HTTP stop is corrected; normal host disposal still fails

The fresh broad command has **106 tests, 104 passed, two failed, zero skipped**:

| Test / public scenario | Actual result |
| --- | --- |
| `SqliteCompositionPreservationTests.ThreeStoppedSelectionCyclesPreserveExactOriginalStoresAndUseFreshProcesses` / `disable-reenable` | Expected exit 0, actual 1; `composition_cleanup_incomplete_composition_host_dispose_incomplete`; 7.574s. |
| `PostgresCompositionDeadlineTests.PublicCorrectionsRequireFreshAttemptsAndExplicitUncertainMigrationRecovery` / `failure-correction` | Expected exit 0, actual 1; same private causal code; 25.800s. |

These are unchanged required examples, not deliberate cleanup-refusal cases.
`CompositionWorker.cs:189-190` identifies bounded `app.DisposeAsync()` after
the host-stop action; `:269` applies the internal 500-ms graceful stop budget.
`CompositionCallbackRunner` still gives each cleanup action at most one second.
The safe boundary does **not** distinguish a disposal exception from an
uncompleted callback/deadline or establish why either occurred. No scheduling,
database, double-disposal, data-loss or false-termination diagnosis is asserted.

Both `NormalStopWithAnOpenIncompleteHttpRequestPreservesInventoryAndVerifiesExit`
provider cases pass in this same broad run. The PostgreSQL lost-serving-session
case also passes, retaining correlated `composition_storage_dispose_incomplete`,
final incomplete refusal, actual exit, no reacquisition and fresh explicit
retry. Thus T083 supplies a genuine targeted correction and useful privacy-safe
evidence; it does not establish closure of all normal-stop failures.
No unchanged isolated rerun is used to erase the two new failures.

The original failed local all-suite and successful narrowed reconciliation
remain accurately recorded in `implementation.md`. Passing assessed-head CI
is useful evidence but does not resolve these new same-head local failures.
T085 addresses the remaining scoped operation/evidence gap; it does not repeat
T083's already-proven incomplete-request correction or authorize broader scope.

## Complete intent assessment

| Inventory | Assessed | Result |
| --- | --- | --- |
| Functional requirements | 20/20 | F4 qualifies valid executable composition; F3 qualifies current lifecycle/evidence completeness. |
| Buildable success criteria | 6/6 | SC-001 retains F4; SC-003/005 retain F3's preservation/correction completion limitation; SC-004/006 refusal/privacy assertions remain required. |
| Acceptance scenarios | 20/20 | Five stories, four scenarios each; US1.2 retains F4 and US4.4/US5.3 retain F3. |
| Plan decisions | 12/12 | R4/R5/R11 retain F4; R9/R12 retain F3; no architecture or scope dispute. |
| Existing tasks | 83/83 | Original T001-T080 plus all corrective tasks assessed regardless of checkboxes. |
| Constitution principles | 12/12 | Applicable ownership/trust/authority/evidence constraints checked; no separate constitutional contradiction. |
| Findings by type | missing 0; partial 2; contradicts 0; unrequested 0 | F4 new, F3 remains open. |
| Findings by severity | CRITICAL 0; HIGH 1; MEDIUM 1; LOW 0 | Two scoped implementation tasks, not new design decisions. |

### Requirements, scenarios and outcomes

| Intent | Actual source and discriminating assertions | Assessment |
| --- | --- | --- |
| FR-001/002; US1.1-4; SC-001 | `ModuleCompositionHost`, copied explicit selection/catalog, resolver/preflight/worker; `BootScenarios` real v0/v1 endpoints, public operations, explicit/omitted disabled observations, zero disabled callbacks/inventory effects and missing-implementation refusal. | F1 closed; valid composed service-count boundary remains F4. |
| FR-003/004; US2.1-3; SC-002 | `CompositionResolver`, graph SCC/ordinal frontier and diagnostic ordering; resolver/limit tests, frozen public corpus and `ValidationScenarios` run independent expected decisions 100 times across ten permutations, two real cultures and author/host paths. | Pure resolution exists; its accepted contribution counts do not survive runtime under F4. |
| FR-005/006/007; US2.1-4 | Strict original-byte selection/settings, known field tokens, claim ownership/routes/unsupported execution, complete storage/recipe preflight; invalid scenarios and both-provider preflight tests compare exact unchanged inventory and zero mutating callbacks. | No additional preflight gap; F4 is later executable materialization, not a request to reduce valid inputs. |
| FR-008/011; US3.1/2, US5.1; SC-001/004 | Audited native DI, owner scopes, coordinator whole-phase barriers, partial-entry tracking, verified storage and fresh initial health before whole-set publication; lifecycle tests and actual public services/migrations. | F2's four owner/local boundaries pass; F4 prevents valid larger executable sets from becoming ready. |
| FR-009/010; US3.1-4 | `CompositionStorageSession` inspection/preparation/read verification reuses accepted registry/bind/migrate authority; provider migration/recipe/progress/authority/recovery tests prove exact histories, no serving writes, earlier commits and explicit recovery. | No additional storage-ownership gap; current cleanup evidence is qualified by F3. |
| FR-012/013; US4.1-4; SC-003 | Copied persisted selection, exact assignment/data/history/unrelated-state comparisons after each of three fresh-process cycles; dependency-invalid disable and explicit owner-only upgrade assertions. | No missing preservation assertion; fresh SQLite public cycles fail final expected-success cleanup under F3. |
| FR-014; US5.1/2; SC-005 | Current-attempt atomic own/effective health, individual 30-second freshness, serialized sweeps, transitive impact/independent roots; health/observation tests and actual aggregate HTTP assertions. | No additional gap; anonymous requests do not invoke module code. |
| FR-015/016; US3.3/4, US4.4, US5.3; SC-004/005 | Monotonic clipped budgets, partial-entry reverse cleanup, owned process-group/root/descendant verification, seven ignoring-token boundaries, cancellation, authority and named correction examples. | New safe host-disposal failures keep F3 open; termination/refusal assertions and product bounds are retained. |
| FR-017/018; US5.4; SC-006 | Generated attempts, fixed structured records, admitted frames/owners/fields, bounded output/labels and private captures; anonymous aggregate schema and route exclusions. | T083 causal records work without raw exception/settings/path output; no new privacy/authority gap identified. |
| FR-019/020; all scenarios; SC-001-006 | Separate public author/consumer projects, strict C#/Python mandatory observations/omission rejection, eleven guide scenarios and retained named quality/provider/recovery/privacy/browser/startup/failure gates. | F4 lacks boundary lifecycle support; F3's failures must remain failures despite successful historical/CI evidence. |

### Every task, plan decision and principle

| Task set | Current implementation/evidence assessed |
| --- | --- |
| T001-T005 | Reviewed ADR 0006/analysis/checklist gate, existing reviewed DI edge/locks/notices, separate test assemblies, owned provider/process fixtures and strict result harness. |
| T006-T014 | Copied public/configuration/lifecycle/observation contracts, strict private selection, safe record/protocol/attempt limits and complete read-only storage/model/recipe inspection. F4 qualifies T007's derived binding bound. |
| T015-T026 | Independent expected graph/limits/cultures, settings/claims, deterministic diagnostics and actual supervised zero-effect preflight. F4 qualifies accepted count-to-runtime consistency, not missing pure limits. |
| T027-T036 | Native registration audit, owner-scoped public operations, actual empty/dependency/disabled/missing boots and atomic healthy readiness. F4 qualifies runtime exports/bindings; F2's identity fix is verified. |
| T037-T049 | Actual both-provider assigned clean/additive/current migrations, all-recipe barriers, contention/lease loss/killed later-owner recovery, retained earlier data and explicit stopped recovery/serve guide. |
| T050-T057 | Exact three-cycle preservation, invalid dependent disable, reverse partial cleanup and separate explicit upgrade. F3 qualifies fresh completion of SQLite's process cycle example. |
| T058-T072 | Current/stale/dependency health and HTTP privacy, actual process budgets/exit/correction, six-channel privacy, strict omitted-evidence refusals and truthful operational limitations. F3 qualifies ordinary correction/cleanup completeness. |
| T073-T080 | Existing verifier inventories/minima and mandatory public-guide observations retain foundation gates and documented local/CI reconciliation. Original failed complete attempts remain failed; F3 prevents fresh blanket success. |
| T081 | Actual disabled observations and enabled-owner protocol checks; both-provider public boot and strict observation omissions close F1. |
| T082 | Exact owner-qualified public service identities survive 117/118/128-character locals and maximum owner length; independent public boundary evidence closes F2. F4 is a distinct count-composition defect. |
| T083 | Real incomplete-request HTTP-stop regression and fixed private cleanup boundaries are present and pass; new host-disposal evidence prevents complete closure of F3. |

R1-R3 retain explicit compiled discovery, bounded deterministic whole-set
resolution and owner-only settings. R4/R5 retain supported contribution/DI/
phase boundaries, but their complete valid executable counts need T084.
R6/R7 retain stopped owner-local preparation, immutable stores and explicit
recovery. R8 retains current dependency-aware health; R9's actual cleanup and
R12's regression evidence retain T085. R10's safe correlated boundaries and
R11's independent public proof exist, with F4's additional boundary obligation.

Principle I retains MIT licensing, Köfte branding, technical `kofte` identities,
optional modules and no mandatory Hoard. II/III preserve trusted versioned
module contracts, native DI, module-local ownership and dedicated test-project
source. IV/V preserve schema/file/registry/physical ownership and honest
owner-local commits. VI preserves installation-only authority, owner contexts
and safe output. VII-IX retain inert future MCP/frontend/job/event/realtime
categories and unchanged aggregate UI/HTTP contracts; their later
authorization/accessibility/release obligations are not waived. X retains both
real providers, reproducible pinned tooling and no mandatory cloud/deployment
service. XI requires honest provider/process evidence, including both current
failures, and focused regressions before closure. XII retains 004 scope,
approved intent and no MVP-completion claim; the driver adds append-only tasks,
one branch/draft PR and separate review/explicit human acceptance. No new
mandatory service, later-feature runtime or unrequested implementation is identified.

## Fresh commands and limitations

The default shell's `dotnet --version` initially failed (exit 155): only SDK
10.0.203 was selected there. The already installed repository-local SDK was
then selected, without installing dependencies or changing pins:

```bash
export DOTNET_ROOT="$PWD/artifacts/toolchain/dotnet-10.0.402"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_PROCESSOR_COUNT=4
export KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true
```

Actual target: macOS arm64, .NET SDK 10.0.402/runtime 10.0.13,
Node 24.22.0/npm 11.19.1; PostgreSQL uses only owned disposable fixtures.

| Exact command | Result |
| --- | --- |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-class '*ModuleComposition*' --minimum-expected-tests 100` | Passed 136/136, zero skipped; 10.665s. Includes T082's actual boundary Start/readiness cases. |
| `dotnet test --project tests/unit/Kofte.Platform.Tests --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 80` | Passed 87/87, zero skipped; 2.554s. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionConsumerHarnessTests*' --filter-class '*CompositionBootProviderContractTests*' --minimum-expected-tests 35` | Passed 39/39, zero skipped; 30.106s. Strict omission checks plus six actual both-provider boot cases. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 106` | **Failed**, exit 2; 104/106 passed, two host-disposal failures, zero skipped; 3m 14.705s. Includes passing both-provider incomplete-request regression and intentional authority/refusal cases. |
| `python3 scripts/check_repository.py` | Passed: repository assets, local links, MCP parity and driver-state validity. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed 119/119. |
| `git diff --check` | Passed. |

The session-only public probe above was additionally executed against these
projects. It creates no repository test/source and establishes the F4
observations, not provider/whole-gate success. No fresh complete all-suite,
human usability, production or MVP claim is made.

`gh pr view 7` and `gh run view 40171283911` independently confirm all eleven
assessed-head contexts successful: ten product jobs in
[40171283911](https://github.com/Stonefly-Labs/Kofte/actions/runs/40171283911)
and repository checks in
[40171283885](https://github.com/Stonefly-Labs/Kofte/actions/runs/40171283885),
for exact branch head `74074f9b6bac009403e3a0d7c5bf5ca6987feb90`.
Those successful CI invocations do not establish the cause of the two fresh
local failures or supply the absent service-count behavior. New publication
checks are recorded in the PR rather than retroactively attributed to this head.

## Persistence and handoff

The pre-stage task file has SHA-256
`2263a9f592bde7f7c76870daaadb9088e1916e085ce08a0ef4575463463dc3cf`.
Every byte and all 83 checked markers are preserved; only Phase 11's unchecked
T084-T085 are appended. Approved spec/plan, both reviewer/author checklists,
analysis, contracts and historical implementation/convergence evidence remain
unchanged. The historical task status header describes its implementation
checkpoint; it is not rewritten by this append-only stage.

No `.specify/extensions.yml` exists; no before/after convergence hook applies.
The driver completes only **converge / gaps**, selecting **implement T084-T085**
for the next separate invocation on the same draft PR #7. Review, explicit
human acceptance and any eventual human merge remain later gates; no
intermediate merge or readiness is requested.

---

# Historical Convergence before T082-T083

The complete pre-stage report follows verbatim.

# Follow-up Convergence: 004 - Module Composition and Lifecycle

**Result:** gaps

**Stage:** converge only; begun 2026-10-25T04:49:37+00:00.
**Assessed revision:** `5bef3486813f9cd5a4d467f77c7098593f5954d4`.
**Branch / checkpoint:** `feature/004-module-lifecycle`,
[draft PR #7](https://github.com/Stonefly-Labs/Kofte/pull/7).
**Intent:** `spec.md`, `plan.md` and all 81 existing tasks, governed by adopted
constitution v2.0.1. Checked tasks and historical reports are not behavioral
proof; approved contracts explain, rather than expand, that intent.

## Current decision

**F1 / T081 is closed.** The actual configured host now reports disabled
owners explicitly through its private operator channel, including explicitly
false and omitted deployments. Current source, actual both-provider public
boots and strict omission/tampering assertions support that conclusion.

Two partial gaps remain. **F2 (HIGH)** reproducibly prevents activation of an
otherwise accepted dependency composition at valid contribution-name lengths.
**F3 (MEDIUM)** is a recurring, unresolved provider/process evidence-reliability
gap: fresh broad integration fails ordinary expected-success cleanup, while
unchanged isolation passes. Its product-versus-fixture cause is not established.
Append exactly **T082-T083 / Phase 10**. The next invocation is **implement**,
not review, acceptance or another action in this stage.

No product/test/verifier source, dependency, generated contract, approved
spec/plan/ADR, reviewer-owned checklist or historical implementation evidence
changes here. The complete previous convergence report is preserved below.

## Current findings

| ID | Gap type | Severity | Source | Evidence | Remaining work |
| --- | --- | --- | --- | --- | --- |
| F2 | partial | HIGH | FR-006/008/019/020; US1.2; SC-001; plan R4/R5/R11; T018/T023/T028/T030/T034 | Accepted executable local names are up to 128 characters; the host prefixes the owner, then constructs a public binding capped at 128 combined characters. An accepted 118-character local name under `org.test.a` fails activation. | T082: compose valid dependency identities without silently narrowing approved inputs; add public-boundary activation regressions. |
| F3 | partial | MEDIUM | SC-006; plan R12; FR-020 evidence; T060/T068/T075/T078 | Fresh both-provider composition integration passes 102/104; PostgreSQL `disabled-missing` and `bounded-stop` each return `composition_cleanup_incomplete` where normal exit is required. Unchanged isolated PostgreSQL cases pass 5/5. Similar unexplained failures were retained in prior stages. | T083: establish safe causal evidence, fix the scoped product or fixture/scheduling cause, and reconcile the affected accepted gates without weakening cleanup or deadlines. |

### F2: individually valid identities do not compose into a valid binding

- `src/Kofte.Platform/Modules/Composition/ContributionValidator.cs:36-39`
  admits backend-service local names through the inclusive 128-character
  ASCII-name limit. Owner identity is validated separately, and reservations
  retain owner and local name separately.
- `src/Kofte.Host/Features/ModuleComposition/ModuleLifecycleCoordinator.cs:101-105`
  generates default executable exports with
  `ContractId = c.Owner.Value + "." + c.LocalName`.
- The same coordinator at `:291-299` constructs a public
  `ModuleDependencyBinding` from each prerequisite export during Start.
- `src/Kofte.Platform/Modules/Lifecycle/ModuleLifecycle.cs:80-88`
  rejects a binding contract ID longer than 128 characters. Thus the host's
  own generated ID can violate the public constructor after successful
  resolution, registration and initialization.
- Approved `contracts/composition-v1.md:60-63,105` requires explicit versioned
  prerequisite bindings and permits inclusive local IDs of 128 characters.
  T018 explicitly requires that boundary, and T028/T030 require actual public
  dependency behavior, not merely accepted declarations.

A session-only executable probe used public deployment/resolution/lifecycle
contracts, native DI and two no-storage runtimes; there was no provider,
private API, modified repository source or timeout fault. Alpha
(`org.test.a`) exported one scoped public service; Beta (`org.test.b`)
declared Alpha as its compatible enabled prerequisite. The probe resolved,
registered, initialized, marked the no-storage migration boundary, started,
checked readiness and cleaned each fresh composition. Both callbacks were
ordinary immediately completing implementations. With the same valid owner,
only the ASCII local-name length changed:

```json
{"localNameLength":117,"bindingIdLength":128,"accepted":true,"ready":true,"failure":null}
{"localNameLength":118,"bindingIdLength":129,"accepted":true,"ready":false,"failure":"composition_activation_failed"}
{"localNameLength":128,"bindingIdLength":139,"accepted":true,"ready":false,"failure":"composition_activation_failed"}
```

The executable exited 0 after reporting all three observations; that means
the probe completed, **not** that the two failing compositions passed.
This deterministic failure is independent of F3's provider/process failures.
No storage loss, disabled execution, sandbox or authorization defect is
demonstrated. The fix must retain accepted owner/local-name limits,
versioning, ownership and collision safety; truncation or silently reducing
the accepted local-name length is not closure. Persist focused public-contract
regressions, including the combined boundary and longer valid owners, during
T082 rather than adding code in convergence.

### F3: repeated cleanup failures still lack a causal explanation

The fresh broad integration run failed two of 104 tests, with zero skips:

| Test / scenario | Observed result |
| --- | --- |
| `PostgresCompositionProviderTests.OwnedProviderBootPreservesInventoryAndExecutesOnlyItsIntendedPublicModules("disabled-missing")` | Expected exit 0, actual exit 1; `composition_cleanup_incomplete`; 9.914s. |
| `PostgresCompositionDeadlineTests.PublicFaultMatrixVerifiesActualExitBudgetsPartialCleanupAndOwnedDescendants` / `bounded-stop` | Expected exit 0, actual exit 1; `composition_cleanup_incomplete`; 37.020s. |

The safe consumer event was
`composition_test_command_exit_expected_0_actual_1_composition_cleanup_incomplete`.
After that command finished, the unchanged PostgreSQL provider/deadline
selection passed **5/5**, zero skipped. Passing isolation does not erase the
failed broad run or establish that its cleanup refusal was incorrect.
The first convergence below retained the same failure category in PostgreSQL
preservation, and the T081 implementation increment retained SQLite
preservation/PostgreSQL aggregate failures. There is now enough repeated
evidence to require a scoped investigation rather than another unqualified
rerun recommendation.

The assessment inspected worker/coordinator cleanup, supervisor termination,
owned-process support and public scenario assertions. The fixed safe terminal
code does not identify which cleanup operation or fixture condition caused
these unexpected failures. There is **no established contention diagnosis,
proven product timeout bug, false refusal, storage loss or verified fix**.
F3 is MEDIUM/partial evidence reliability, not a claim that the core fail-closed
cleanup behavior is absent. T083 must distinguish and correct the actual
product or harness cause, retain failed evidence, and prove both normal cleanup
and intentional incomplete/uncertain refusals under the original bounds.
Do not normalize retries, remove assertions, reduce test floors or widen
product deadlines to obtain green output.

## Current coverage

| Inventory | Checked | Result |
| --- | --- | --- |
| Functional requirements | 20/20 | F1 closed; F2 affects public executable contribution/dependency behavior; F3 qualifies FR-020 evidence, not a proven cleanup-contract violation. |
| Buildable success criteria | 6/6 | F2 affects SC-001; F3 leaves SC-006 evidence reliability incomplete. |
| Acceptance scenarios | 20/20 | All five stories' four scenarios inspected; valid public dependency activation US1.2 remains partial under F2. |
| Plan decisions | 12/12 | R4/R5/R11 retain F2; R12 retains F3. No new architecture or scope dispute. |
| Existing tasks | 81/81 | All original tasks and corrective T081 assessed regardless of markers; two remaining obligations appended without rewriting history. |
| Constitution principles | 12/12 | Applicable invariants inspected; no separate constitutional contradiction identified. Later-feature obligations remain scoped, not waived. |
| Findings by type | missing 0; partial 2; contradicts 0; unrequested 0 | T082-T083 only. |
| Findings by severity | CRITICAL 0; HIGH 1; MEDIUM 1; LOW 0 | F2 and F3 respectively. |

### Requirement, scenario and outcome evidence

| Intent | Current implementation/assertion evidence | Assessment |
| --- | --- | --- |
| FR-001/002; US1.1-4; SC-001 | Selection/catalog/resolver/preflight and worker; `BootScenarios`, both-provider actual boots, missing implementation, empty profiles and exact inventories. | F1 closed; F2 leaves the valid public dependency boundary incomplete. |
| FR-003/004; US2.1-3; SC-002 | Resolver/graph/diagnostic limits and exact corpus; permutations, cultures, SCCs, ranges and disabled prerequisites. | No additional gap identified. |
| FR-005/006/007; US2.1-4 | Owner configuration/claim/recipe/complete-inventory barriers and invalid public scenarios. | Local contribution validation exists; derived executable binding incompatibility is F2. |
| FR-008/011; US3.1/2, US5.1; SC-004 | Native registration, coordinator phase barriers, no partial readiness, fresh health and actual migration scenarios. | F1 closed; F2 fails a valid dependent at Start rather than publishing false readiness. |
| FR-009/010; US3.1-4 | Storage inspection/preparation/session/verification and actual clean/current/additive/no-storage/authority/interruption/recovery assertions. | No additional gap identified; no new whole-gate claim. |
| FR-012/013; US4.1-4; SC-003 | Selection exclusion, immutable assignments, exact original data/history cycles, dependent-disable rejection and owner-only upgrades. | F1 closed; recurring normal-stop evidence limitation retained under F3. |
| FR-014; US5.1/2; SC-005 | Current own/effective health, serialized sweeps/freshness and snapshot-only aggregate HTTP; health/HTTP/public assertions. | No additional gap identified. |
| FR-015/016; US3.3/4, US4.4, US5.3; SC-004/005 | Callback/attempt budgets, authority watching, reverse partial-entry cleanup, owned process-group termination and public fault/correction scenarios. | Behaviors/assertions present; F3 requires causal reconciliation of unexpected success-path cleanup failures, not relaxed refusals. |
| FR-017/018; US5.4; SC-006 | Diagnostics/protocol/admission, fixed guidance, bounded output/labels, private captures and unchanged anonymous schemas. | F1 closed with admitted enabled-owner consistency; F3 requires safe causal evidence, never raw exceptions/paths/settings. |
| FR-019/020; all scenarios; SC-001-006 | Separate public module/consumer assemblies, eleven strict scenarios, runner minima and retained foundation gates. | F2 needs public activation-boundary regression; F3 prevents claiming fresh broad success. |

### Plan, task and constitutional evidence

All original task ranges were inspected against the same source/assertion
areas recorded in the historical matrix below, not accepted from checkboxes.
T001-T014's boundary/tooling/contracts, T015-T026's resolution/preflight,
T027-T036's native DI/public boots, T037-T049's storage/recovery,
T050-T057's preservation, T058-T072's health/fault/privacy/correction and
T073-T080's gates/documentation remain represented. F2 qualifies
T018/T023/T028/T030/T034; F3 qualifies the current completeness of
T060/T068/T075/T078 evidence.

T081 was independently traced through `CompositionPreflight`'s existing
disabled snapshot projection, copied enabled-owner admission in
`CompositionProtocol`, actual explicit/omitted host observations before
publication, fresh attempts, zero disabled effects and unchanged anonymous
health. The C# strict scenario catalog and Python verifier both require the
new observations and reject missing/decremented evidence. It closes F1
without introducing callbacks, storage authority or a new public field.

R1-R3 and R6-R10 retain compiled discovery, bounded deterministic resolution,
owner settings, stopped maintenance/read-only serve separation, exact
preservation/recovery, current health, finite actual supervision and private
diagnostics. R4/R5/R11's executable identity/public proof gap is F2.
R12's retained regression/evidence obligation is qualified by F3.

Principles I-III retain the module-free portable host, honest versioned trusted
contracts, native DI and dedicated test-project ownership; IV-V retain
schema/file/registry isolation, owner-local commits and explicit recovery;
VI retains private authority, scoped services and safe output; VII-IX keep
future MCP/jobs/events/realtime/frontend work inert and aggregate UI unchanged;
X-XI require honest provider/process evidence, including F3's limitations;
XII retains approved intent, append-only tasks, one branch/draft PR and
separate explicit human acceptance. No later-feature runtime or mandatory
cloud dependency is introduced or waived.

## Fresh verification

Commands selected the existing isolated .NET SDK 10.0.402/runtime 10.0.13,
with `DOTNET_CLI_TELEMETRY_OPTOUT=1`, `DOTNET_PROCESSOR_COUNT=4` and
`KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`. No dependency, product deadline,
assertion, provider or runner floor was changed.

| Exact command | Observed result |
| --- | --- |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-class '*ModuleComposition*' --minimum-expected-tests 100` | Passed 133/133, zero skipped; 10.595s. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionBootProviderContractTests*' --minimum-expected-tests 6` | Passed 6/6, zero skipped, both-provider empty/valid/disabled-missing actual boots; 26.448s. |
| `dotnet test --project tests/unit/Kofte.Platform.Tests --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 40` | Passed 83/83, zero skipped; 2.565s. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionConsumerHarnessTests*' --minimum-expected-tests 20` | Passed 33/33, zero skipped; 0.582s. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 100` | **Failed**, exit 2; 102/104 passed, two PostgreSQL cleanup failures, zero skipped; 3m 55.936s. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*CompositionProviderTests' --filter-class '*CompositionDeadlineTests' --filter-trait Provider=postgres --minimum-expected-tests 4` | Unchanged isolated rerun passed 5/5, zero skipped; 2m 08.919s. Does not replace the failed broad result. |

An initial guessed boot selector,
`dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionBootContractTests*' --filter-method '*PublicDisabledDiscoveryAndMissingImplementationUseActualHost*' --minimum-expected-tests 2`,
matched **zero tests**, exit 8. It is not passing evidence; the corrected
six-case provider-class command above was then executed.

The no-storage public binding probe separately reproduced F2 as described
above. No fresh complete all-suite is claimed. At assessment,
[product CI 40163203981](https://github.com/Stonefly-Labs/Kofte/actions/runs/40163203981)
for the assessed revision remained in progress; quality/unit/startup/browser/
privacy were successful, and remaining jobs were not claimed passed.
Repository CI 40163203980 was successful. Historical CI and local results are
retained as historical evidence, never substituted for this stage's failed run.

## Current persistence and handoff

The complete pre-stage `tasks.md` byte prefix has SHA-256
`f1aa919bdafbd1237dbd29701d467ad004fae584c3955a52ede4d49cdbc3a76c`.
All 81 existing checked markers remain intact; only T082-T083 are appended
unchecked under Phase 10. The driver records only **converge / gaps**, with
this report, task append and progress documentation in the same publication.
**Implement T082-T083 is next** on the original branch and draft PR #7.
Follow-up convergence, evidence-based final review and explicit human acceptance
remain separate. No intermediate merge, PR readiness or acceptance is requested.
No `.specify/extensions.yml` exists; no convergence hook is configured.

---

# Historical First Convergence: 004

The original assessment follows verbatim, including its then-current finding,
task state, checks and handoff.

# Convergence: 004 - Module Composition and Lifecycle

**Result:** gaps

**Stage:** converge only; begun 2026-10-25T03:15:15+00:00.
**Assessed revision:** `faa4dfe4a6744eae8e7c4098b92682929ac958bb`.
**Branch / checkpoint:** `feature/004-module-lifecycle`,
[draft PR #7](https://github.com/Stonefly-Labs/Kofte/pull/7).
**Intent:** `spec.md`, `plan.md` and all 80 original tasks, governed by adopted
constitution v2.0.1. Contracts/model/guide explain those obligations; historical
completion markers and implementation reports are not proof by themselves.

## Decision and assessment boundaries

The implementation substantially delivers the approved composition, lifecycle,
storage, preservation, health and private-diagnostic behavior. It is **not yet
converged**: the actual configured host does not expose explicit disabled
observations to its trusted operator channel. One HIGH partial finding F1
appends exactly T081 under Phase 9, without changing any original task byte
or marker. The next invocation is **implement**, not review or acceptance.

The assessment inspected the platform composition/lifecycle contracts and
resolver, host selection/preflight/DI/coordinator/health/supervision/projection,
infrastructure inspection/recipes/preparation/current verification, independent
module/consumer scenarios, actual assertions, verifier wiring and delivered
documentation. All original tasks were assessed regardless of their checked
state. No unrelated feature, product-direction change, new dependency or later
runtime is required by this finding.

The initial worktree was clean on the recorded branch, local and remote heads
agreed, and PR #7 was open/draft. Updated `origin/main` was inspected; this
continuation of unfinished 004 needs no intermediate merge. Explicit feature
context was supplied to the prerequisite command and driver. The original
implementation stage is recorded complete; converge was begun exactly once.
No `.specify/extensions.yml` exists, so no pre/post convergence hook is configured.

## Findings

| ID | Gap type | Severity | Source | Evidence | Remaining work |
| --- | --- | --- | --- | --- | --- |
| F1 | partial | HIGH | FR-008/012/018/020; US1.3; SC-001/006; plan R1/R8/R10; T052/T062/T069/T074 | Disabled state is correctly modeled in `CompositionHealth.Snapshot`, but the configured worker never projects it. Actual disabled owners emit only `discovered/passed`; the private record schema carries no disabled observation. | T081: wire bounded explicit disabled observations into the actual trusted operator output and require them in both-provider public/strict-harness regressions, preserving zero callbacks/stores and anonymous privacy. |

### F1: the disabled observation stops at an in-memory model

- `src/Kofte.Host/Features/ModuleComposition/CompositionHealth.cs:73-75`
  constructs `DesiredEnabled = false`, `ModuleLifecycleOutcome.Disabled` and
  `ModuleObservationCode.Disabled`. The required state already exists; this is
  not a request for a new lifecycle state machine or administration endpoint.
- `CompositionPreflight.cs:31-43` records every validated deployed identity as
  `discovered/passed`, irrespective of desired enablement.
- `CompositionWorker.cs:83-137` creates the coordinator and runs its lifecycle;
  its private output consists of diagnostic records, not the detailed snapshot.
  `CompositionSupervisor.cs:158-223` validates/projects those records rather
  than supplying an independent disabled-state observation.
- `CompositionDiagnostics.cs:10-55` has no disabled stage/outcome or desired-state
  field. Neither `CompositionProtocol` nor its parent projection transports the
  existing `ModuleObservation`. Once discovered, disabled owners produce no
  additional observation identifying them explicitly as disabled.
- `tests/unit/Kofte.Host.Tests/Features/ModuleComposition/CompositionObservationTests.cs:11-40`
  verifies a directly constructed snapshot. The actual public
  `BootScenarios.DisabledMissingAsync` at
  `tests/contract/Kofte.Composition.Consumer/Scenarios/BootScenarios.cs:112-165`
  verifies zero author callbacks, unchanged inventory and missing-implementation
  refusal, but never requires explicit disabled operator evidence. Those are
  valuable tests of different obligations, not closure of US1.3/FR-008.

An isolated runtime probe used the **existing Release deployment executable**
and its compiled `BootDisabled` catalog, a fresh private SQLite fixture
initialized through the delivered `--store init` command, and a private
selection with Alpha/Beta disabled and the no-storage module enabled. It
started `dotnet tests/contract/Kofte.Composition.Consumer/bin/Release/net10.0/Kofte.Composition.Consumer.dll --deployment`
with `COMPOSITION_TEST_CATALOG=BootDisabled`,
observed real `/health/ready`, sent SIGTERM to its exact owned parent and
captured the complete sanitized operator output through verified normal exit.
No production/test source, dependency or supported command was changed for
this probe; its temporary fixture was cleaned up.

Observed sanitized result:

```json
{
  "ready": true,
  "exit": 0,
  "stderrEmpty": true,
  "terminal": "passed",
  "disabledOwners": 2,
  "recordsPerDisabledOwner": [1, 1],
  "disabledOwnerStages": ["discovered"],
  "explicitDisabledObservation": false
}
```

Each disabled owner's sole record has `code: composition_progress`,
`outcome: passed` and `guidance: observe_current_attempt`. There is no disabled
code/outcome, desired-state field or snapshot frame. An operator must infer
disabled intent from absent later callbacks rather than receive the explicit
observation promised by the feature. This is a core acceptance-observability
gap, hence HIGH/partial; it demonstrates neither execution of disabled code,
data loss nor a constitutional storage/authorization violation.

## Inventory coverage

| Inventory | Checked | Result |
| --- | --- | --- |
| Functional requirements | 20/20 | F1 affects the explicit disabled projection in FR-008/012/018/020; no other actionable gap identified. |
| Buildable success criteria | 6/6 | Existing behavior/evidence assessed; F1 leaves the delivered observation/evidence incomplete in SC-001/006. |
| Acceptance scenarios | 20/20 | All five stories' four scenarios assessed; F1 affects US1.3. |
| Plan decisions | 12/12 | R1/R8/R10 retain F1; no design dispute or requested new architecture. |
| Original tasks | 80/80 | T052/T062/T069/T074/T079 overstate completeness of disabled operator evidence; all original bytes/markers preserved. |
| Constitution principles | 12/12 | Applicable invariants assessed; later-feature duties remain scoped, not waived. No additional constitutional finding. |
| Findings by type | missing 0; partial 1; contradicts 0; unrequested 0 | One actionable task. |
| Findings by severity | CRITICAL 0; HIGH 1; MEDIUM 0; LOW 0 | T081, Phase 9. |

### Requirements, scenarios and outcomes

| Intent | Actual source/assertion evidence | Assessment |
| --- | --- | --- |
| FR-001/002; US1.1-4; SC-001 | `ModuleCompositionHost`, `ModuleSelectionReader`, `CompositionResolver`, `CompositionWorker`; `BootScenarios`, `CompositionProviderTests`, `StorageCompatibilityTests`. Empty v0/v1 boots, public Alpha/Beta behavior, missing/duplicate/disabled selection and exact inventories. | Boot and zero-disabled-effect behavior present; explicit disabled projection is F1. |
| FR-003/004; US2.1-3; SC-002 | `CompositionGraph`/`CompositionDiagnostic`; `CompositionResolverTests`, `CompositionLimitTests`, `FrozenCompositionCorpus`, `ValidationScenarios.DeterministicAsync`. Accepted range rules, ordinal Kahn ordering, SCCs, exact bounded diagnostics and actual en-US/tr-TR repetition/permutation comparisons. | No additional actionable gap. |
| FR-005/006/007; US2.1-4 | `CompositionPreflight`, `ModuleConfigurationValidator`, `ContributionValidator`, `CompositionStorageInspector`, `ModuleStorageRecipe`; selection/configuration/claim/preflight/recipe assertions and `ValidationScenarios.InvalidAsync`. Full barrier, known field guidance, unsupported execution, route overlap/reservations and unchanged provider inventory. | No additional actionable gap. |
| FR-008/011; US3.1/2, US5.1; SC-004 | `ModuleLifecycleCoordinator`, worker phase barriers and `CompositionHealth.Publish`; `ModuleLifecycleTests`, `CompositionProgressTests`, `MigrationScenarios` and initial-health publication refusals. Registration/Initialize/verification/Start/health precede whole-set readiness. | Enabled phase/publication behavior present; disabled observation is F1. |
| FR-009/010; US3.1-4 | `CompositionStorageSession`, inspector/preparation/verification, assigned recipes and retained registry boundaries; migration/recipe/authority/recovery assertions and `MigrationFailureScenarios`. Both-provider actual histories, no-storage absence, current serving without revision writes and explicit uncertain-write recovery. | No additional actionable gap. |
| FR-012/013; US4.1-4; SC-003 | Selection snapshots, disabled exclusion and immutable persisted bindings; `PreservationScenarios`, preservation/selection/upgrade tests. Three actual cycles/six fresh processes and attempts per provider compare exact original assignments, records, history and unrelated state; separate single-owner upgrade. | Preservation/coherent selection present; explicit disabled operator observation is F1. |
| FR-014; US5.1/2; SC-005 | `CompositionHealth`, serialized coordinator sweeps and `HealthHandler`; observation/health/HTTP tests and `HealthScenarios`. Failed > unknown > degraded > healthy, individual freshness, transitive impact, independent roots and snapshot-only aggregate HTTP. | No additional actionable gap. |
| FR-015/016; US3.3/4, US4.4, US5.3; SC-004/005 | Attempt/callback budgets, authority watcher, process-group supervisor, reverse entered-resource cleanup; `StopScenarios`, `CorrectionScenarios`, authority/deadline/recovery/stop tests. Actual ignoring-token exit, separate tracked-descendant proof, no concurrent abandoned cleanup, no lease reacquisition and explicit recovery before fresh retry. | No additional actionable gap. |
| FR-017/018; US5.4; SC-006 | Diagnostics/protocol/admission/supervisor and unchanged bootstrap endpoints; `DiagnosticScenarios`, protocol/privacy/HTTP assertions. Useful fixed-code records, current attempts, original storage links, bounded labels/frames/output and private captures; no anonymous control/inventory route. | Existing channels remain private; transporting the required disabled observation is F1, not permission to add public detail. |
| FR-019/020; all scenarios; SC-001-006 | Separate `Kofte.Composition.TestModule`/consumer assemblies; all eleven strict public scenarios and verifier targets, retained actual foundation/CI reports and current assertions. Production has no test-module dependency. | Existing evidence is substantial but does not test F1; T081 must strengthen the actual public observation contract. |

### Plan decisions and all original tasks

| Plan decision | Current implementation/evidence | Assessment |
| --- | --- | --- |
| R1 compiled discovery | Copied `ModuleDeployment`, explicit public deployment entry, disabled callback exclusion, no scanning/downloading. | F1: disabled model not projected. |
| R2 bounded deterministic resolution | Resolver/graph/diagnostic implementations; exact limits, permutations, cultures and independent expectations. | No additional gap. |
| R3 owner configuration | Strict private file and immutable admission snapshot; known field/type/author validator checks. | No additional gap. |
| R4 honest contribution ownership | Capability/owner/version/route/permission checks; executable future categories refused. | No additional gap. |
| R5 DI and phase barriers | Descriptor audit, owner scopes/restricted services, explicit prerequisite exports, partial-entry ownership and whole-set publication. | No additional gap. |
| R6 read-only/offline separation | Complete inventory/model/operation preflight, retained read/exclusive authority, no serving provisioner credential. | No additional gap. |
| R7 preservation/recovery | Exact mapping/data/history cycles and explicit interruption/full-set recovery. | No additional gap. |
| R8 current health/observations | Current-attempt immutable own/effective samples and freshness; real aggregate HTTP. | F1: disabled observation transport incomplete. |
| R9 actual bounded supervision | Monotonic caps/reserves, parent/worker admission, real process-group exit and incomplete-refusal evidence. | No additional gap. |
| R10 useful private diagnostics | Fixed-code records, validated bounded IPC, spans/events/metric labels and safe output failures. | F1: disabled state omitted from actual operator output. |
| R11 independent public proof | Separate author library/consumer; frozen corpus, public examples and strict omission/tampering checks. | Existing assertions do not detect F1; strengthened by T081. |
| R12 retained baseline | Actual complete historical local gate and independently downloaded assessed-head CI reports; fresh focused checks below. | Passing existing checks does not close F1. |

| Original task range | Assessment evidence |
| --- | --- |
| T001-T005 | Recorded ADR/checklist/analysis gates; DI dependency provenance/locks/notices; separate assemblies, owned fixtures and strict scenario catalog/harness. |
| T006-T014 | Copied contracts, immutable owner configuration/resources/observations; private selection, protocol, attempt budgets and actual complete-inventory/model inspection assertions. |
| T015-T026 | Resolver/graph/claims/settings implementation and independent exact public corpus; negative no-effect/full-inventory and bound/correction assertions. |
| T027-T036 | Audited DI/coordinator/supervised configured entry; actual empty/public dependency/missing boots, typed public service and zero disabled-callback evidence. |
| T037-T049 | Assigned Alpha/Beta models/migrations, prepare/serve separation, current-target verification and actual provider/contention/interruption/recovery examples. |
| T050-T057 | Three complete preservation cycles/provider, dependent-disable refusal, reverse/LIFO/at-most-once cleanup and owner-only additive upgrade. T052's explicit operator projection remains partial under F1. |
| T058-T072 | Current health/freshness and HTTP, actual timeout/cancellation/authority/cleanup/privacy captures and five corrected examples. T062/T069 do not connect/assert disabled snapshots in actual operator output: F1. |
| T073-T080 | Named-runner targets/minima, all eleven actual public scenarios/strict results, retained complete local/CI evidence, dependency/ownership/privacy documentation and original stage publication. T074/T079's complete observation/evidence claims are qualified by F1. |

### Constitutional assessment

| Principle | Current evidence and applicability |
| --- | --- |
| I | Empty portable host retained; Köfte branding; no mandatory Hoard/cloud/IaC dependency. |
| II | Versioned composition/lifecycle, manifest/dependency/contribution checks, preserved disable data and honest failure/degraded state. F1 is an incomplete explicit disabled observation, not a new trust/sandbox promise. |
| III | Native DI/direct coordination, small existing shared assemblies and dedicated test-project source; actual DI graph/license/notice review retained. |
| IV | Accepted immutable registry, UNIQUE/collision/path/marker/schema-local history rules reused; real SQLite/PostgreSQL inspection/migration/preservation assertions. |
| V | Owner-local commits survive later failure; retained intents and explicit recovery; no cross-module rollback/ACID or irreversible automatic business work API. |
| VI | Private operator selection/records, scoped owner services/settings/store, credential stripping, safe telemetry and no anonymous lifecycle-control route. Detailed output must remain private when F1 is fixed. |
| VII | Namespaced inert MCP declarations/collision checks only; executable tools refused. Authenticated tools remain 007/013, not implemented or waived here. |
| VIII | Ephemeral attempts, not durable jobs/events/workflows/resources/realtime; later runtime/delivery obligations remain assigned to their features. |
| IX | No new visual/admin journey or remote JavaScript; retained bootstrap schema, web adapter and actual browser/accessibility CI evidence. |
| X | Supported pinned local/CI environments, both providers, finite supervision, explicit forward/full-set recovery and honest cleanup limits. |
| XI | Real provider/process/public/privacy assertions and retained foundation checks; F1 demonstrates why a passing suite is not complete requirement evidence. |
| XII/governance | 004 only, original branch/draft PR, append-only tasks, approved intent/reviewer markers preserved, one stage and explicit later human acceptance/merge. |

## Verification and retained evidence

The fresh local environment is Darwin 25.5.0 arm64 / macOS 26.5, 18 logical
CPUs and 64 GiB RAM, Python 3.15.0, .NET SDK 10.0.402/runtime 10.0.13,
Node 24.22.0/npm 11.19.1 and Docker Engine 29.8.2. Commands explicitly select
the existing isolated SDK and use `DOTNET_PROCESSOR_COUNT=4`,
`DOTNET_CLI_TELEMETRY_OPTOUT=1` and disposable PostgreSQL opt-in. No pin, product
budget, assertion, provider or test floor is changed.

The product commands below used this existing toolchain/environment:

```bash
export DOTNET_ROOT="$PWD/artifacts/toolchain/dotnet-10.0.402"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_PROCESSOR_COUNT=4 KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true
```

| Exact fresh command | Observed result |
| --- | --- |
| `python3 scripts/verify.py --suite quality` | Passed all 20 steps, including locked restore/build, .NET format, Prettier, ESLint, TypeScript, contract generation and dependency reconciliation; 30.337 seconds. |
| `python3 scripts/verify.py --suite unit` | Passed all 14 steps, including focused composition and retained host/platform/web/contract/Python units; 32.768 seconds. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 100` | Passed 104/104, zero skipped, both providers; 3m 24.121s. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 60` | **Failed**, exit 2: 83/84 passed, zero skipped; 11m 20.140s. PostgreSQL's `PublicSelectionChangesPreserveOriginalsAcrossThreeCompleteFreshProcessCycles` reported `composition_cleanup_incomplete`. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionMigrationContractTests*' --filter-method '*PublicSelectionChangesPreserveOriginalsAcrossThreeCompleteFreshProcessCycles' --minimum-expected-tests 2` | Isolated reproduction passed 2/2 on SQLite/PostgreSQL, zero skipped; 27.609s. No code/configuration/budget/assertion changes between runs. |
| `python3 scripts/check_repository.py` | Passed repository assets, links, MCP parity and driver validation. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed all 119 tests. |
| `git diff --check` | Passed. |
| `npm run format:check` | Passed all configured files. |

Safe named reports are
`artifacts/verification/quality-none-dfd8790f6fd94af696018ac1fd7833c1.json`
and `unit-none-0b45ee3814cc439aaabfeebac14147e5.json`.
These targeted runs do not claim a fresh complete all-suite.

The fresh broad contract failure is retained, **not converted into a passing
run by the isolated rerun**. Its only failure was the PostgreSQL preservation
case above, with safe event code
`composition_test_command_exit_expected_0_actual_1_composition_cleanup_incomplete`.
The other 83 contracts, including both providers' sequential `all`, passed.
The exact failing preservation method then passed unchanged on both providers
with no other product test process running. The underlying cause was not
established; this assessment does not invent a contention diagnosis, a new
product defect or a broader passing local gate. This execution limitation is
separate from reproducible F1. Retain it when implementing T081 and rerun the
accepted provider/process gates; investigate if it recurs without relaxing
deadlines, cleanup assertions or test floors.

Actual assessed-head [product CI 40147785351](https://github.com/Stonefly-Labs/Kofte/actions/runs/40147785351)
and [repository CI 40147785387](https://github.com/Stonefly-Labs/Kofte/actions/runs/40147785387)
passed all eleven contexts at branch head `faa4dfe4a6744eae8e7c4098b92682929ac958bb`.
All ten safe product artifacts were independently downloaded and reconciled:
**286 passed steps**, expected suite/provider sets, actual synthetic merge
revision `125654b70cf2169d13309e208e751d3b2c3eb9cc`, **24 executed Release guide
commands plus 24 strict semantic observation records**. Each CI report correctly
says `scope: targeted`, `completeGate: false`; these independent jobs are not
misreported as a new combined local all-suite.

Both CI `all` results contain all eleven cases and 389,385 / 389,390 assertions.
Each provider has 84,000 deterministic comparisons, three cycles/six fresh
processes and attempts, twelve original assignment/data/history comparisons,
six unrelated comparisons, five corrections, seven verified timeout root exits,
one tracked-descendant exit and six private-channel/zero-leak comparisons.
The `disabled-missing` observation set contains callback/inventory/missing-owner
checks but no explicit disabled operator observation, consistent with F1.
The historical complete local 286-step run remains in `implementation.md`;
it was not rerun or rewritten by convergence.

## Persistence and handoff

Only T081 is appended. The complete original `tasks.md` byte prefix has SHA-256
`d09285e4a8c2cbf8a6eb467ad9322935635af797d9f16fd4c56fc236bf357063`.
All 80 original markers stay checked; T081 stays unchecked. Approved spec,
plan/research/model/contracts/guide, ADR, both reviewed checklists and historical
analysis/implementation reports remain unchanged. Product/test/verifier source,
dependencies/locks and generated contracts remain unchanged.

The driver records only **converge / gaps**, with this report and task append in
the same publication as its ledger and current progress documentation.
**Implement T081 is next**, separately on the original branch and draft PR #7.
Then run follow-up convergence, final evidence-based review and explicit human
acceptance. No next stage, new design approval, feature acceptance, PR readiness
or intermediate merge is authorized by this assessment.
