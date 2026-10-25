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
