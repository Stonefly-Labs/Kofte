# Follow-up final review: storage registry and module isolation

**Result:** changes

**Spec:** 003 | **Review date:** 2026-10-22 (UTC)

**Assessed head:** `72d45756e451d145c4188552562dca29a0ae8b09`

**Branch/PR:** `feature/003-storage-registry` /
[draft PR #6](https://github.com/Stonefly-Labs/Kofte/pull/6)

## Decision and scope

The read-only `spec-review` assessment inspected actual implementation and
assertions against all **16 functional requirements, six success criteria,
18 acceptance scenarios, fourteen plan decisions, 81 existing tasks and twelve
constitutional principles**. A bounded follow-up reconciled the new finding
with the exact maintenance contract, telemetry vocabulary and CLI behavior.

Prior **R1/T081, F1/T079 and F2/T080 are closed** by meaningful assertions.
One **MEDIUM implementation finding R2** remains: explicit registry-upgrade
mutations and rejected boundaries bypass the required correlated storage
operation records. No storage-isolation failure or unresolved design decision
is demonstrated. Exactly **T082** is appended unchecked; all 81 historical
task bytes/markers are preserved.

Green checks establish the behavior they exercise, not these absent records.
The next invocation is **implement T082**, then separate converge/review and
explicit human acceptance. This review neither implements remediation nor
accepts 003, marks the PR ready, requests merge or begins 004.

## R2 - MEDIUM: registry upgrade lacks complete correlated diagnostics

**Required intent:** `spec.md:312-317` requires structured, correlated, bounded
diagnostics for allocation, provisioning, resolution, contention and
migration/recovery, and says: "Mutations and failed boundary checks MUST emit
operator-visible audit-relevant records identifying outcome and safe
module/operation context." US5.1 (`spec.md:205-209`) requires stable outcome,
operation, correlation and retry/reconciliation guidance.
`contracts/storage-v1.md:150-159` and T066 (`tasks.md:208`) explicitly require
safe structured records for every mutation/rejected boundary. This is current
observable telemetry, not spec 009's future durable audit product.

**Source evidence:**

- `src/Kofte.Infrastructure/Storage/StorageMaintenance.cs:8-16` delegates
  acquisition/upgrade without the correlated diagnostics pipeline.
- `Registry/RegistryMigrationRunner.cs:35-65,94-103` performs the real registry
  DDL, metadata/history write and commit. Its before/after-commit checkpoints
  are optional activity events/listener notifications
  (`StorageDiagnostics.cs:23-28`), not correlated outcome/code records.
- Non-infrastructure scopes are rejected at `StorageMaintenance.cs:13-14`;
  foreign/disposed scopes at `MaintenanceScope.cs:83-86`; unsupported or
  inconsistent installation evidence at
  `Registry/RegistryMigrationRunner.cs:68-91,117-164`. Those paths can fail
  before any checkpoint and do not emit the prescribed upgrade record.
- The existing safe code/guidance/outcome mapping and structured
  record/span/bounded-metric pipeline live in `StorageDiagnostics.cs:30-142`,
  but maintenance does not enter that pipeline.
- `tests/contract/Kofte.Platform.ContractTests/Modules/Storage/StorageDiagnosticsTests.cs:22-36`
  invokes acquisition/upgrade but asserts records only for later
  register/resolve. The inherited seven-outcome
  `tests/integration/Kofte.Storage.IntegrationTests/Providers/StoragePrivacyTests.cs:28-186`
  likewise does not assert upgrade records.

Paths without a prefix above are relative to
`src/Kofte.Infrastructure/Storage/`.

**Caller qualification:** The CLI does emit a correlated success summary
(`src/Kofte.Host/Features/StorageMaintenance/RegistryCommand.cs:15-20`), and
`BootstrapDiagnostics.cs:35-46` emits generic safe host failures. Do not describe
the CLI as completely silent. Neither supplies the direct API's missing
operation/provider/outcome records. Maintenance worker failures are collapsed
to exit categories, and raw stdout/stderr is drained
(`StorageMaintenanceProcess.cs:57-86,121-125`). Adding child output alone
would not make it operator-visible; forwarding arbitrary child output would
violate the privacy boundary.

**Impact:** A supported direct `IStorageMaintenance.UpgradeAsync` can commit
registry state, or reject an ownership/version/consistency boundary, without
the promised correlated operation evidence. This leaves FR-014, US5.1, design
decision D14 and T066 partial despite passing existing suites.

**Scoped remediation:** T082 reuses the existing safe emitter independently of
assignment-bearing `StorageResult`, preserves `UpgradeAsync`'s `Task` and safe
exceptions, and supplies a safe CLI projection plus real both-provider capture
regressions for success and rejected boundaries. `StorageOperation` already
contains Upgrade/Acquire (`src/Kofte.Platform/Modules/Storage/StorageContracts.cs:48`);
no new public vocabulary or design approval is needed. Do not fabricate a module
assignment, claim unchanged durability after a possible commit, weaken budgets,
or expose raw provider/worker output.

**Scope narrowing after review:** A successful lease acquisition is not the
identified registry mutation. Independent successful acquire/dispose records
and a full acquisition matrix are not prerequisites for R2 closure. Contention
and actual failed boundaries still retain FR-014's existing diagnostic
obligation; this finding does not introduce a new durable audit runtime.

## Confirmed closures

| Finding | Actual assertion evidence |
| --- | --- |
| R1/T081, changed allocator | `tests/integration/Kofte.Storage.IntegrationTests/Registry/RegistrationTests.cs:82-125`: persist Alpha/Beta and sentinels, release/reopen maintenance through a counted different valid allocator, change display/release metadata, require Ready/Unchanged, zero allocator calls, exact eight-field assignments, retained data and unchanged installation revision/complete inventory. |
| R1/T081, no-storage declaration | Same file `:128-173`: persisted no-storage cannot become relational; require Rejected/Unchanged, `storage_conflict`/`correct_configuration`, continued NoStorage/null allocation fields, zero allocator calls and unchanged unrelated assignments/data/revisions/inventory. Both provider subclasses inherit both facts. |
| F1/T079 | Public `tests/contract/Kofte.Storage.Consumer/Scenarios/RegistrationScenarios.cs:32-119` and `tests/integration/Kofte.TestSupport/StorageRestartProcess.cs:20-66`: three distinct admitted/reaped non-parent processes, six exact assignment comparisons and six retained sentinel reads, not rewritten data or positive totals alone. |
| F2/T080 | Public `Scenarios/CollisionScenario.cs:17-52` and fixture-only `StorageCollisionFixture.cs:10-16`: real distinct-registered-owner UNIQUE conflict, unchanged assignment/data/full inventory and absent contender; separate unowned-target refusal remains at `:55-124`. |
| Mandatory public evidence | `StorageExampleEvidence.cs:9-35`, contract `RegistrationContractTests.cs:14-35`, `StorageGuideTests.cs:10-26` and `StorageConsumerHarnessTests.cs:117-158` require semantic observations and reject missing/decremented fields. |

Complete snapshots include all eight assignment fields
(`StorageRestartProcess.cs:10-16`); stopped inventory captures sorted registry
rows, revisions, ownership/history and physical inventory
(`Recovery/RecoverySetVerifier.cs:265-377`). T081's controlled red/green
mutations remain historical execution evidence in `implementation.md:67,77-91`,
not newly executed mutations in this review.

## Requirement and acceptance mapping

Prefixes: **P** = `src/Kofte.Infrastructure/Storage/`;
**I** = `tests/integration/Kofte.Storage.IntegrationTests/`;
**C** = `tests/contract/Kofte.Storage.Consumer/`;
**CT** = `tests/contract/Kofte.Platform.ContractTests/Modules/Storage/`;
**S** = `tests/integration/Kofte.TestSupport/`;
**H** = `tests/integration/Kofte.Host.IntegrationTests/`.

| Requirement | Implementation and meaningful assertions | Assessment |
| --- | --- | --- |
| FR-001 | Platform `StorageContracts.cs:5-45`, P`ModuleStorageBinding.cs:20-45`, CT`BindingContractTests.cs:9-19`: versioned trusted contracts; metadata cannot mint bindings or select arbitrary owners. | Supported |
| FR-002 | P`Registry/StorageRegistry.cs:35-55`, both registry SQL constraints, I`Registry/RegistrationTests.cs:18-76,128-173`: canonical identity, immutable storage/no-storage declaration. | Supported; R1 closed |
| FR-003 | Real PostgreSQL/SQLite UNIQUE/CHECK/immutable constraints; P`MaintenanceScope.cs:27-81`, I`Registry/RegistrationConcurrencyTests.cs:18-118`, `RegistryConstraintTests.cs:15-68`. | Supported |
| FR-004 | Stored-row-first P`Registry/StorageRegistry.cs:35-50,111-127`, immutable SQL triggers, I`Registry/RegistrationTests.cs:82-125`, public exact restart snapshots. | Supported; R1 closed |
| FR-005 | P`Allocation/StorageAllocator.cs:11-33`, `PostgresStorageProvisioner.cs:10-79`, `AssignedContextFactory.cs:21-30`; real schema/history/constraint/model negatives. | Supported |
| FR-006 | P`ModuleStorePaths.cs:8-49`, `UnixStorePaths.cs:12-53`, `StorageInstallation.cs:76-113`; I`StorageBoundaryTests.cs:211-268`: separate files, actual-open link/sidecar/ancestor/corrupt/missing refusals preserve Beta. | Supported |
| FR-007 | Durable reservation/physical/ready phases and owner markers; I`ProvisioningCrashTests.cs:15-62`, S`StorageCrashWorker.cs:17-65`: actual phase-driven process kills, preserved ambiguous evidence. | Supported |
| FR-008 | P`Registry/StorageRegistry.cs:111-136`, `Registry/RegistryMigrationRunner.cs:117-164`, `ModuleStorageBinding.cs:54-90,115-132`: verified open/resolve, no replacement-empty fallback. | Supported |
| FR-009 | Assigned factory/cache/history/assembly/snapshot/interceptors, P`MigrationBoundaryValidator.cs:12-151`; I`AssignedModelTests.cs:38-105`, `StorageBoundaryTests.cs:31-145`, independent public contexts. | Supported within approved DDL subset |
| FR-010 | P`Registry/RegistryMigrationRunner.cs:35-164`, `ModuleMigrationRunner.cs:20-98`, `ModuleMigrationRecovery.cs:13-118`; actual format/upgrade/migration kills, recovery and refusal tests. | Storage behavior supported; upgrade telemetry R2 |
| FR-011 | P`OperationBudget.cs:12-45`, `MaintenanceScope.cs:27-111`, `StorageMaintenanceProcess.cs:37-118`; real deadlines/cancellation/lease-loss/observer cleanup. | Supported; diagnostic qualification R2 |
| FR-012 | Separate registry/physical/module commits; I`ModuleMigrationTests.cs:13-63` and recovery verify actual Alpha history/effects while retaining Beta. | Supported; no cross-module ACID |
| FR-013 | P`StorageInstallation.cs:19-45`, `PostgresIdentity.cs:15-70`, real provider posture/configuration and I`CorrectedRetryTests.cs:69-112`. | Supported |
| FR-014 | P`StorageDiagnostics.cs:64-142`, I`Providers/StoragePrivacyTests.cs:28-186`, CT diagnostics/output captures prove captured register/resolve/migration outcomes, not bypassed maintenance. | **Partial: R2** |
| FR-015 | P`Recovery/RecoverySetVerifier.cs:24-204,265-377`, actual SQLite backups/PG dumps/fresh restores, H`StorageRecoveryCompatibilityTests.cs:16-122`: complete-set/source/destination/reset protection. | Supported |
| FR-016 | C scenarios, mandatory CT semantic gates, H`StorageCompatibilityTests.cs:16-77`, retained manifest/HTTP/OpenAPI/web checks. | Supported; diagnostics qualified by R2 |

| Outcome/scenario | Concrete assertion evidence and qualification |
| --- | --- |
| SC-001 | I`ModuleMigrationTests.cs:13-63`, `AssignedModelTests.cs:38-105`, C`Scenarios/ModuleScenarios.cs:20-118`: independent identical logical tables/history, real sentinels, Alpha-only upgrade. |
| SC-002 | I`Registry/RegistrationConcurrencyTests.cs:18-118`: twenty-by-ten connection/process same/distinct-ID matrices, Busy-only retries, exact final inventories and forced collisions. |
| SC-003 | C`Scenarios/RegistrationScenarios.cs:32-119`, S snapshots, I`RestartTests.cs:17-57` and T081: exact durable mappings/data, including changed allocator. |
| SC-004 | Actual provisioning/upgrade/migration kill matrices, I`OperationDeadlineTests.cs:26-145`, `PostgresLeaseLossTests.cs:14-80`, explicit recovery/refusal and cleanup. |
| SC-005 | Actual forbidden-target preservation and seven-outcome privacy captures; absent maintenance records remain R2, not inferred covered. |
| SC-006 | Nine public cases, real provider backup/fresh restore, bootstrap upgrade and retained foundation gates. |
| US1.1 | I`Registry/RegistrationTests.cs:18-56`, independent module/public clean-install assignments and platform separation. |
| US1.2 | Public three-process exact retention plus T081 changed-allocator `RegistrationTests.cs:82-125`. |
| US1.3 | No-storage inventory/public case and T081 `RegistrationTests.cs:128-173`. |
| US1.4 | I`Registry/RegistrationTests.cs:60-76,128-190`, immutable SQL and foreign/stale/configuration rejection. |
| US2.1 | Same-ID independent connection/process matrix, Busy-only retries and final single-owner inventory. |
| US2.2 | Distinct-ID matrix: ten unique keys/tokens and cumulative physical inventory. |
| US2.3 | I`AllocationCollisionTests.cs:11-47`, C`Scenarios/CollisionScenario.cs:17-124`: registered/unowned targets preserved. |
| US2.4 | I`RegistryConstraintTests.cs:15-68,89-109` and format/history refusals reject invalid persisted evidence. |
| US3.1 | I`ModuleMigrationTests.cs:13-55`, independent public Alpha/Beta contexts and real owner-local history. |
| US3.2 | Same test `:35-63`, C`Scenarios/ModuleScenarios.cs:86-118`: Alpha-only change and maintenance conflict refusal. |
| US3.3 | I`StorageBoundaryTests.cs:31-125,211-268`: whole-pending-set preflight and actual-open path/owner/connection negatives. |
| US3.4 | I`Registry/RegistrationTests.cs:200-220` plus missing/corrupt/linked-file refusal without replacement-empty success. |
| US4.1 | I provisioning/migration crash tests and S killed workers: verified retry or preserved block at durable phases. |
| US4.2 | I deadline, corrected-authority/outage, cancellation and lease-loss tests; diagnostics qualification R2. |
| US4.3 | I`RegistryUpgradeTests.cs:17-46`, both format suites and host v0/v1/unsupported compatibility; upgrade records remain R2. |
| US4.4 | I`RecoverySetTests.cs:19-143`, S`PostgresRecoveryExercise.cs:20-128`: actual fresh restore and mixed/stale/member/reset refusal. |
| US5.1 | I privacy/CT diagnostics assert correlated captured operations; **maintenance mutation/boundary records partial, R2**. |
| US5.2 | I`CorrectedRetryTests.cs:18-112`, C`Scenarios/CorrectedRetryScenario.cs:13-84`: preserved reservations/unrelated data and distinct retry correlation. |

## Design, task and constitutional coverage

| Plan decision | Inspected evidence/disposition |
| --- | --- |
| D01 contracts/adapter | Provider-free platform contracts, infrastructure-owned EF binding and owner-only public consumption. |
| D02 dependencies/test ownership | Central pins, normal dedicated test-project inclusion, reviewed graph/notices; no mediator or EF tooling adoption. |
| D03 allocator v1 | Safe byte-bounded stored allocations and T081 changed-allocator regression. |
| D04 registry | Embedded SQL/digest/history/revision validation, direct constraints and real upgrade kills. |
| D05 lease | Shared serving/exclusive maintenance reuse, foreign/disposed/contention/session-loss assertions. |
| D06 PostgreSQL authority | Runtime no-CREATE, separate provisioner identity/SET ROLE and owned fixture verification. |
| D07 SQLite | Canonical private per-owner files, exclusive creation and actual-open alias/sidecar negatives. |
| D08 provisioning | Durable reservation/marker/ready commits and killed-process evidence. |
| D09 assigned EF | Model/cache/history/assembly/snapshot binding and full pending-set preflight/physical verification. |
| D10 migration recovery | Actual history/checkpoints, known-prefix/effect/abandoned-lock recovery and preserved refusal. |
| D11 supervision | Monotonic budgets, admitted workers, real 30/120-second holds and bounded reaping. |
| D12 explicit upgrade | Strict host dispatch, unchanged module-free serving, v0/v1 and legacy guards. |
| D13 stopped recovery | Complete inventory/hash/token/checkpoint verification and actual both-provider backup/restore. |
| D14 diagnostics/examples/gates | Public semantic examples/gates supported; maintenance diagnostic completeness **partial, R2**. |

| Existing tasks | Evidence/disposition |
| --- | --- |
| T001-T006 | Approved ADR, reviewed dependencies/projects, fail-closed selectors and owned real-provider/process fixtures. |
| T007-T014 | Contracts/results, budgets/scopes/paths/shared lease, actual versioned registry SQL and maintenance configuration. |
| T015-T016 | Original regression obligations now include actual T081 inherited assertions; R1 closed. |
| T017-T027 | Registration/provision/marker/ready/binding/module-free compatibility; T079 supplies public restart proof. |
| T028-T035 | Real matrices/constraints/forced collisions and bounded conflicts; T080 supplies registered-owner public proof. |
| T036-T047 | Assigned model/cache/history/preflight, actual-open negatives, independent module migrations/upgrades. |
| T048-T063 | Killed workers/deadlines/lost sessions, explicit recovery, upgrade/legacy protection, real complete-set restore. |
| T064-T065 | Existing result/privacy assertions supported; they do not establish maintenance records. |
| T066 | **Partial R2:** every mutation/rejected-boundary diagnostic obligation omits explicit registry upgrade. |
| T067-T070 | Safe worker/public output, correction/retry, nine cases and docs; qualify broad diagnostic completeness by R2. |
| T071-T074 | Verifier/CI inventories, fail-closed Python tests, dependencies/notices and docs inspected. |
| T075-T078 | Historical execution/publication exists; fresh evidence below is separately scoped and does not cure R2. |
| T079-T080 | Actual semantic restart/collision assertions plus mandatory standalone/`all` checks close F1/F2. |
| T081 | Both inherited facts/four real-provider cases close R1; production behavior unchanged. |

| Principle | Assessment |
| --- | --- |
| I | Portable module-free Köfte; no mandatory Hoard. |
| II | Stable IDs/versioned contracts; trusted code, not sandboxing. Loader/lifecycle deferred. |
| III | Dedicated test projects/native DI/justified adapter and reviewed dependencies. |
| IV | Real uniqueness/concurrency, schema-local histories and separate SQLite files; R1 closed. |
| V | Separate durable commits/reconciliation, no cross-module ACID/shared connection contract. |
| VI | Least privilege/ownership/path/privacy supported; operation-record completeness needs R2. |
| VII | No product MCP runtime; developer MCP configuration is not product implementation. |
| VIII | Storage intent/recovery only, not jobs/events/realtime/exactly-once delivery. |
| IX | No new visual journey; retained bootstrap browser/accessibility/state regression gates apply. |
| X | Supported environments/configuration/upgrade/recovery/budgets; operational visibility qualified by R2. |
| XI | Real-provider assertions and executed evidence; passing suites cannot prove missing records. |
| XII | Approved 003 scope preserved; no later-feature demands or self-acceptance. |

## Fresh execution evidence

The caller selected the **already installed** .NET SDK 10.0.402 and
Node 24.22.0/npm 11.19.1; no toolchain requirement was weakened. Inherited
PostgreSQL runtime/provisioning connections were unset. Named suites own their
private SQLite roots, disposable PostgreSQL fixtures, child processes and cleanup.

| Exact command | Result |
| --- | --- |
| `SPECIFY_FEATURE_DIRECTORY=specs/003-storage-registry .specify/scripts/bash/check-prerequisites.sh --json --require-spec --require-tasks --include-tasks` | Passed; resolved the recorded feature directory. |
| `python3 scripts/verify.py --suite quality` | Passed 19 steps, 27.185 seconds, including locked restores, format/build/lint/type checks and dependency review. |
| `python3 scripts/verify.py --suite unit` | Passed 11 steps, 6.595 seconds. |
| `python3 scripts/verify.py --suite integration --provider sqlite` | Passed 7 steps, 210.919 seconds. |
| `python3 scripts/verify.py --suite integration --provider postgres --disposable-postgres` | Passed 9 steps, 216.568 seconds. |
| `python3 scripts/verify.py --suite recovery --provider both --disposable-postgres` | Passed 38 steps, 378.131 seconds. |
| `python3 scripts/verify.py --suite contract --provider both --disposable-postgres` | Passed 37 steps, 221.866 seconds. |
| `python3 scripts/verify.py --suite privacy --provider both --disposable-postgres` | Passed 36 steps, 71.793 seconds. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed all 111 tests. |
| `npm run format:check` | Passed. |
| `git diff --check` | Passed after correcting new Markdown line-end whitespace. |

All **157 targeted steps pass** on the assessed revision. The seven safe
reports record Darwin 25.5.0 arm64, 18 logical CPUs, 64 GiB physical memory and
Python 3.15.0; all retain `scope: targeted` and `completeGate: false`.
No fresh local `--suite all`, standalone browser/startup/failure-propagation,
human usability/reference-machine exercise or controlled mutation run is claimed.
Both-provider integration includes T081; its prior controlled mutation evidence
remains historical. Existing gates do not contain the missing R2 assertions.

Safe local report inventory under ignored `artifacts/verification/`:

```text
quality-none-33f65c0e60194a7083596afe5a846e7b.json
unit-none-4cf39e2255db4f3e81394bb17615626a.json
integration-sqlite-774c50cfa5494c248c98f94ca1eca3aa.json
integration-postgres-f32d42b5c9904000ab21ee1871cc3a44.json
recovery-both-8588ddc12a01434badb3e6a13e491307.json
contract-both-d43bf138032d41099b42c5061026463b.json
privacy-both-e902e51b1d854534909bfd18bef82ffd.json
```

### Actual assessed-head CI

The actual [product run 39847794129](https://github.com/Stonefly-Labs/Kofte/actions/runs/39847794129)
and [repository run 39847793832](https://github.com/Stonefly-Labs/Kofte/actions/runs/39847793832)
both identify assessed feature head `72d45756e451d145c4188552562dca29a0ae8b09`
and pass all **eleven contexts**. All ten actual uploaded safe JSON reports were
downloaded and inspected: **200/200 steps pass**, not just a workflow summary.

| CI suite | Passed steps |
| --- | --- |
| quality | 19 |
| unit | 11 |
| integration SQLite / PostgreSQL | 7 / 9 |
| recovery | 38 |
| contract | 37 |
| browser | 16 |
| privacy | 36 |
| startup | 9 |
| failure propagation | 18 |

Every report identifies synthetic merge
`1425e692801db7d6eda68781b5a0e9cf3e198cfd`, whose actual GitHub commit
parents were verified as main `66f0e4a950bc3590cdb3a25f1622afbd13a0c226`
and assessed head `72d45756e451d145c4188552562dca29a0ae8b09`.
All reports honestly retain `scope: targeted` and `completeGate: false`.
These are actual assessed-head CI results, not a newly executed local all-suite
or evidence of the absent upgrade diagnostic assertions.

## Preservation, limits and handoff

Only this report, append-only T082, helper ledger and directly related progress
docs change. Product/test source, dependency graph, approved spec/design/ADR,
implementation/convergence reports and both reviewed checklists are unchanged.
All 81 pre-existing task bytes/markers and the complete prior review below
remain historical evidence; T066's old marker is not silently unchecked.

Supported limits remain the narrow int/text create-table/primary-key/add-column
DDL subset; trusted in-process code and supported local filesystems, not hostile
plugin isolation; preserved ambiguous SQLite evidence; stopped complete-set
fresh-target recovery; and operator responsibility for intentionally selecting
an older recovery point. Hashes/UUIDs prove consistency, not authenticity or
knowledge of newer lost backups. Historical T081 PostgreSQL deadline failures
and unchanged passing reruns remain disclosed; their cause or elimination of
timing sensitivity is not established.

Future loaders/lifecycle orchestration, actor/team authorization, product MCP,
jobs/events/SignalR, new UI journeys, Kubernetes, global automated backup and
reference-module release proof are explicitly outside this review's feature
scope, neither demanded nor waived.

**Next:** implement T082 on this same branch/draft PR. No new design approval,
credential or merge prerequisite is identified. Human acceptance is separate
and has not been requested or inferred.

---

## Historical final review before T081

# Final review: storage registry and module isolation

**Result:** changes

**Spec:** 003 | **Review date:** 2026-10-22 (UTC)
**Assessed head:** `2d65cd0ba7314e1e50fc1be56347f3ebdf4bcca9`
**Branch:** `feature/003-storage-registry`
**PR:** [#6](https://github.com/Stonefly-Labs/Kofte/pull/6), still draft
**Next stage:** Implement T081 on this same branch/PR, in a separate invocation.

## Decision and method

The read-only `spec-review` agent inspected approved intent and actual production,
test, public-consumer, fixture, dependency, verifier and CI source. A bounded
follow-up checked both missing cases across existing C#, TypeScript and Python
tests/helpers, not just the named registration tests. The caller independently
executed the checks and verified actual CI artifacts below. The reviewer did not
execute commands or change files/state.

The assessment includes all **40 requirements/outcomes/scenarios** (16 FRs,
six SCs, 18 scenarios), **14 plan decisions**, **T001-T080** and **12 principles**.
Prior public-example findings F1/T079 and F2/T080 are closed. One **MEDIUM
acceptance-evidence finding R1** remains: two explicitly prescribed immutable
registration regressions are absent. Production code appears correct for both;
this is not a demonstrated storage-engine failure or a new design decision.
Passing existing suites cannot establish assertions they do not contain.

Exactly **T081** is appended for the missing evidence. Original task bytes and
markers are retained, not silently reclassified as proof. The earlier passing
convergence and implementation reports are preserved as historical assessments;
their unqualified completeness claims do not override this review.
No product/test code, dependency, contract, requirement, ADR or reviewer-owned
checklist is changed here. No review hooks are configured.

This stage does not implement T081, accept feature 003, mark ready, request merge
or begin 004. Explicit human feature acceptance remains separate after a passing
follow-up converge and review.

## R1: missing immutable-registration regressions

| Severity | Intent | Source/assertion evidence | Impact and remediation |
| --- | --- | --- | --- |
| MEDIUM | FR-002/004; T015/T016; affected completeness claims T026/T077 | `tasks.md:80` requires changed allocator behavior never to rename persisted keys; `:81` requires no-storage-to-relational conflict. `Registry/RegistrationTests.cs:16-76` retries using the ordinary allocator and tests relational-to-no-storage, not the reverse. Allocator unit tests `StorageAllocatorTests.cs:11-53` do not reopen persisted ownership with substituted allocation behavior. Public `RegistrationScenarios.cs:20-24,61-64` has the same directional omission. | A regression regenerating an existing key, or converting a persisted no-storage identity, could escape the claimed evidence. T081 adds two inherited facts, executing on both real providers, with exact mapping/data/revision/full-inventory preservation and zero allocator calls. |

The reviewer checked every existing allocator injection: registration/collision
tests, privacy cases and the fixture-only public collision helper concern new or
distinct owners, invalid candidates or path/privacy failures. None retries the
already persisted owner through a changed allocator. No-storage creation sites
in registration, recovery and privacy tests do not attempt a relational repeat.
A direct inconsistent INSERT refusal is not the missing persisted-repeat case.

These obligations are not superseded: `spec.md:248-260`,
`contracts/storage-v1.md:101-108` and `data-model.md:65-67,124-126` retain immutable
stored mappings and forbid silently converting no-storage to installation.
Accepted ADR 0005 contains no waiver.

`src/Kofte.Infrastructure/Storage/Registry/StorageRegistry.cs:35-50` invokes the
allocator only when no record exists and rejects mismatched relational need
symmetrically. Thus test-only remediation is expected unless new evidence
demonstrates a production defect.

T081 must preserve a relational owner's exact eight-field assignment, sentinel,
row/installation revisions and structurally compared complete inventory across
reopen and changed metadata, while a counted changed allocator remains unused.
It must also reject the persisted no-storage owner's relational repeat with
`storage_conflict`, `correct_configuration` and unchanged durability, preserving
the no-storage/null-allocation record, revisions, inventory and unrelated owners.
Existing owned fixtures/internal seams suffice; no public override is needed.

## Prior public findings: closed

| Finding | Inspected actual evidence |
| --- | --- |
| F1 / T079 | Public `RegistrationScenarios.cs:32-85` migrates Alpha/Beta, writes distinguishable sentinels, captures assignments and releases maintenance before launches. `:87-119` re-registers changed metadata in each fresh process and reads original records without rewriting them. `StorageRestartProcess.cs:10-66` snapshots all eight fields and uses admitted/reaped children with strict parsing. Required observations are three distinct non-parent PIDs, six exact assignment comparisons and six retained reads. |
| F2 / T080 | Public `CollisionScenario.cs:17-54` migrates an owner, writes its sentinel and captures complete inventory. Fixture-only `StorageCollisionFixture.cs:10-16` forces a distinct contender against the registered key; the real INSERT/UNIQUE boundary rejects. Exact assignment, sentinel and full inventory remain unchanged, and contender resolution proves no alternate allocation. Bounded refusal/guidance and separate unowned-target refusal `:57-124` remain. |
| Standalone and `all` gates | `StorageExampleEvidence.cs:9-35`, public contract/guide tests and consumer/harness result parsing require those semantic observations. `StorageConsumerHarnessTests.cs:117-158` accepts a complete result, then rejects omissions and decremented observations despite positive assertion totals. |

The helper and process paths above are under `tests/integration/Kofte.TestSupport/`;
public scenario paths are under `tests/contract/Kofte.Storage.Consumer/Scenarios/`.
These closures do not supply R1's separate missing cases.

## Requirement-to-implementation and assertion coverage

Prefixes identify assessed-head paths; line references are not later checkpoint
line numbers.

| Prefix | Path |
| --- | --- |
| P | `src/Kofte.Infrastructure/Storage/` |
| I | `tests/integration/Kofte.Storage.IntegrationTests/` |
| C | `tests/contract/Kofte.Storage.Consumer/` |
| CT | `tests/contract/Kofte.Platform.ContractTests/Modules/Storage/` |
| S | `tests/integration/Kofte.TestSupport/` |
| U | `tests/unit/Kofte.Host.Tests/Storage/` |
| H | `tests/integration/Kofte.Host.IntegrationTests/` |

“Supported” means inspected implementation and relevant assertions exist, with
actual execution recorded separately. It does not waive R1 or grant acceptance.

| Requirement | Actual implementation and assertion evidence | Disposition |
| --- | --- | --- |
| FR-001 trusted versioned contracts | Platform `Modules/Storage/StorageContracts.cs:5-45`; P`ModuleStorageBinding.cs:20-45`; maintenance-only host composition. CT`BindingContractTests.cs:9-19` checks nonconstructible assignments/bindings and no arbitrary-ID module resolver; U configuration cases separate elevated/serving configuration. | Supported |
| FR-002 one identity/allocation, explicit no-storage | P`Registry/StorageRegistry.cs:35-55`; real SQL uniqueness/null/state constraints; I registration/constraint tests and public no-storage creation. | Partial evidence: false-to-true repeat missing, R1 |
| FR-003 uniqueness/concurrency | PostgreSQL registry SQL `:14-52`, SQLite SQL `:21-90`; P`MaintenanceScope.cs:27-81`. I`Registry/RegistrationConcurrencyTests.cs:18-118` executes twenty-by-ten same/distinct-ID connection/process races with final inventory; direct constraints and public forced collision. | Supported |
| FR-004 persisted immutable mapping | P`Registry/StorageRegistry.cs:35-50,111-127`; immutable SQL triggers; exact restart/sentinel and public T079 assertions. | Partial evidence: substituted-allocator retry missing, R1 |
| FR-005 schema/name/history ownership | P`Allocation/StorageAllocator.cs:11-33`, PostgreSQL provisioner `:10-79`, assigned factory `:21-30`; allocator limits, direct constraints and I module/model tests inspect actual stores/history. | Supported |
| FR-006 private per-module SQLite file | P`Sqlite/ModuleStorePaths.cs:8-49`, `UnixStorePaths.cs:12-53`, installation `:76-113`; I`Providers/StorageBoundaryTests.cs:211-268` rejects missing/linked/hard-linked/nonregular/corrupt/sidecar/ancestor targets while preserving Beta. | Supported |
| FR-007 durable incomplete/ready state | P registry reservation/physical/ready phases and owner markers; I`Recovery/ProvisioningCrashTests.cs:15-62`, S crash worker and public interrupted case actually kill durable phases; ambiguous pre-marker state stays preserved/blocked. | Supported |
| FR-008 verification/no replacement fallback | P registry resolution `:111-136`, registry migration runner `:117-164`, binding `:54-90,115-132`; inconsistent reopen, missing-ready, actual-open corruption/alias/marker/provider negatives. | Supported |
| FR-009 assigned EF boundary | P assigned factory/assembly/cache/history/snapshot and `MigrationBoundaryValidator.cs:12-151`; I assigned-model and boundary tests, full pending-set rejection and independent public Alpha/Beta contexts. | Supported within documented DDL allowlist |
| FR-010 explicit upgrade/protected migrations | P`Registry/RegistryMigrationRunner.cs:35-164`, module migration runner `:20-98`, recovery `:13-118`; both-provider formats, transactional upgrade kills, history-bearing migration kills and physical/lock refusal. | Supported |
| FR-011 finite budgets/truthful uncertainty | P budget `:12-45`, maintenance scope `:27-111`, process supervision `:37-118`; I deadline `:26-145`, lease-loss `:14-80`, observer failure and cancellation-after-intent assertions. | Supported |
| FR-012 module-local transactions/reconciliation | Separate registry/physical/module commits; module runner/recovery compare actual history/effects. Alpha-only upgrades and crash/preflight negatives preserve Beta. No shared connection or cross-module ACID contract. | Supported |
| FR-013 explicit configuration/privileges | P installation `:19-45`, PostgreSQL identity `:15-70`, host maintenance configuration; real provider bootstrap/corrected-authority tests and serving-elevation rejection retain runtime no-CREATE. | Supported |
| FR-014 private bounded diagnostics | P`StorageDiagnostics.cs:12-142`, seven disjoint public results; S result checks, I privacy `:28-186` and CT diagnostics capture result/console/worker/span/event/metric channels with canaries. | Supported |
| FR-015 complete-set recovery/legacy refusal | P`Recovery/RecoverySetVerifier.cs:24-204,265-377`; host SQLite recovery guards; S actual SQLite backup/PostgreSQL dump/fresh restore exercises and H source/destination/reset refusal tests. | Supported |
| FR-016 executable examples/unchanged foundation | C program/scenarios, semantic harness gates and explicit host maintenance dispatch; all nine examples, module-free compatibility and retained manifest/HTTP/OpenAPI/web/browser verification. | Supported; completeness remains qualified by R1 |

## All success criteria and acceptance scenarios

| Outcome | Source/assertion evidence and limits |
| --- | --- |
| SC-001 | I module migration tests and C`Scenarios/ModuleScenarios.cs:20-118`: independent Alpha/Beta stores with identical logical tables/history, actual rows and Alpha-only additive upgrade preserving Beta. |
| SC-002 | Full twenty-round/ten-caller same/distinct-ID connection/process matrix; bounded Busy-only retries, direct UNIQUE/CHECK and registered-owner public collision preservation. |
| SC-003 | Public three-process exact mapping/data proof plus I`Registry/RestartTests.cs:17-57`, no-storage creation and missing-ready refusal. Allocator-evolution evidence is separately incomplete under R1. |
| SC-004 | Provisioning/migration/upgrade kills, native SQLite callback, actual 30/120-second supervision, 3-second outage, 2-second contention, cancellation and lost-session cleanup assertions. |
| SC-005 | Foreign/path/binding and full pending-set preflight negatives preserve forbidden targets; all-seven-outcome captures require no seeded secret/path/data disclosure. |
| SC-006 | Both-provider public install/retry/upgrade/recovery corpus and executable guide, actual backup/dump/fresh restore, retained bootstrap gates. Not a human-usability or production-readiness claim. |

| Scenario | Observable evidence |
| --- | --- |
| US1.1 | I registration/module tests and public clean install inspect distinct module stores plus separate platform. |
| US1.2 | Public retry `RegistrationScenarios.cs:32-119`, exact snapshots and required standalone/`all` observations preserve mappings/data after three process restarts and metadata changes. R1 adds the absent allocator-evolution regression. |
| US1.3 | NoStorage results/resolution and registration inventory show three identities but only two module stores. |
| US1.4 | Immutable SQL/key/delete constraints, owner/binding negatives and conflicting relational-to-no-storage repeat; prescribed false-to-true coverage remains R1. |
| US2.1 | Independent connection/process same-ID race matrix converges to one allocation or bounded Busy, with Busy-only retry. |
| US2.2 | Distinct-ID matrix verifies ten unique keys/tokens and cumulative physical inventory. |
| US2.3 | I allocation-collision and public collision cases preserve registered/reserved/unowned targets without alternate allocation. |
| US2.4 | Direct constraint bypass attempts and both-provider format/reopen corruption tests reject inconsistent records. |
| US3.1 | Independent public/integration contexts and real migrations keep identical logical tables/history physically local. |
| US3.2 | Module migration/public isolated-upgrade assertions change Alpha only and reject conflicting maintenance. |
| US3.3 | I boundary tests reject owner/path/model/connection/migration overrides before forbidden writes; public negatives preserve Beta. |
| US3.4 | Missing/corrupt/foreign ready-store actual-open and marker/provider/revision checks refuse silent recreation. |
| US4.1 | Provisioning/migration kill matrices and public interrupted case expose truthful incomplete state; verified retry or preserved block. |
| US4.2 | Actual outage/privilege/contention/deadline/session-loss/cancellation/observer failures are bounded and preserve committed uncertainty. |
| US4.3 | Both provider format/upgrade-kill tests and host startup/legacy guards preserve supported v0/v1 state and reject unsupported evidence. |
| US4.4 | Actual complete stopped provider restores retain ownership/data; mixed/stale/member/reset/source/destination negatives preserve originals. |
| US5.1 | I privacy and CT diagnostic assertions capture safe correlated seven-outcome records across all declared channels. |
| US5.2 | I corrected retry and C`Scenarios/CorrectedRetryScenario.cs:13-84` preserve original allocation, unrelated owner/data/history and distinct retry correlation. |

## Plan decisions, tasks and principles

| Plan decision | Inspected evidence/disposition |
| --- | --- |
| D01 pure contracts/one adapter/trusted composition | Platform remains provider-free; infrastructure owns providers/EF; public consumers receive owner-only binding. |
| D02 dependencies/test ownership | Central pins/locks, reviewed graph/notices and dedicated normally included tests. No EF Design/tool or mediator adoption. |
| D03 immutable allocator v1 | Bounded allocation/SQL constraints and stored-record implementation; changed-allocator regression remains R1. |
| D04 versioned registry/real constraints | Both SQL scripts, digest/version/history runner, direct constraints and upgrade-kill cases. |
| D05 reused installation lease | Shared lease/scope and host delegation; foreign/disposed/cancelled/contention/session-loss assertions. |
| D06 offline PostgreSQL authority | Provider identity/provisioner, owned fixture OID/comment checks, denied runtime privileges and corrected-authority retry. |
| D07 private canonical SQLite ownership | Path/provisioner/Unix safety and actual-open alias/link/sidecar negatives. |
| D08 durable provisioning phases | Registry/marker/provisioners and real before/after commit kills, with ambiguous pre-marker preservation. |
| D09 assigned EF preflight | Options/model/cache/history/assembly/snapshot checks, full pending set and physical verification, independent public models. |
| D10 migration checkpoint/recovery | Durable intent, transaction/history/effect comparison and explicit recognized-lock/unknown-effect refusal. |
| D11 shared finite budgets/supervision | Monotonic budget, admitted child, actual deadline/cancellation/observer-failure/termination/reaping assertions. |
| D12 explicit upgrade/unchanged serving | Host dispatch/configuration/startup reconciliation plus v0/v1/legacy guards; no automatic initialization/migration. |
| D13 stopped coherent recovery | Manifest/inventory/hash/revision/checkpoint validation, actual provider backups and fresh-target restoration. |
| D14 safe diagnostics/examples in existing gates | Seven-outcome captures, all nine examples, semantic observations and mandatory verifier/CI selectors. |

| Tasks assessed | Evidence/disposition |
| --- | --- |
| T001-T006 | Explicit human ADR approval; dependency/project/solution boundaries, owned provider/process fixtures and fail-closed selectors. |
| T007-T014 | Versioned results/contracts, shared budget/scope/path/lease, real registry scripts/version/history and maintenance-only configuration. |
| T015-T016 | Existing allocator/registration/restart tests inspected; two prescribed cases remain unverified under R1. Checked historical markers do not close them. |
| T017-T027 | Public registration, allocator, durable provision/marker/ready verification, issued binding and module-free compatibility. T079 closes the prior public omission; T026's evidence completeness remains qualified by R1. |
| T028-T035 | Full connection/process races, direct/forced constraints, finite contention, stored mapping/candidate validation and conflict guidance; T080 closes prior public collision omission. |
| T036-T047 | Assigned factory/model/cache/history/migrations, full pending preflight, actual-open negatives and independent integration/public Alpha/Beta models/upgrades. Narrow DDL support is explicit. |
| T048-T063 | Real crash/deadline/session-loss/cleanup, durable reconciliation, guarded explicit upgrade/legacy operations, actual complete provider backup/restore and public recovery examples. Shared support supplies behavior without duplicate planned guide files. |
| T064-T070 | Seven bounded results and captured privacy channels, real corrected retry, full nine-case corpus and supported operational limitations. |
| T071-T078 | Required inventories/selectors and negative Python regressions, reviewed dependencies/docs, historical full-local and actual current-head CI. T077's unqualified completeness is corrected by R1 here; execution/publication is caller-owned. |
| T079-T080 | Closed by actual public restart and distinct-registered-owner collision observations and mandatory semantic gates. |
| New T081 | Appended unchecked for R1 only; no implementation or new execution evidence claimed. |

| Constitution principle | Evidence/applicability |
| --- | --- |
| I product/brand | Köfte, portable module-free host and no mandatory Hoard dependency. |
| II modularity/trust | Immutable canonical identity, minimal contracts and honest trusted in-process boundary; loader/lifecycle remains 004. |
| III slices/tests/licenses | Native DI, justified adapter, dedicated test ownership and reviewed pinned graph; no MediatR/AutoMapper or production-linked test source. |
| IV storage | Actual uniqueness/concurrency, separate schema/file ownership and assigned history supported; two prescribed regression cases remain R1. |
| V transactions | Separate durable commits and explicit reconciliation; no cross-module ACID/shared-connection promise. |
| VI integration safety | Separate elevated authority, restricted runtime, owner/path checks and captured private diagnostics. Future actor/team/resource authorization is not replaced or bypassed. |
| VII MCP | No product MCP interface/tool/discovery added; configured developer tools do not satisfy future product MCP. |
| VIII durable work/realtime | Storage intent/recovery only, not jobs/events/audit/SignalR or exactly-once delivery. |
| IX frontend | No new visual journey/extension; retained web/browser/axe/state regressions remain applicable. |
| X portability/operations | Local SQLite without cloud/Docker/Kubernetes, owned PostgreSQL, explicit upgrades/deadlines/recovery and operator responsibilities. |
| XI evidence | Real providers and fail-closed gates; green suites do not waive R1's missing assertions. |
| XII scope/governance | Approved 003 only; no future feature pulled in, human design/acceptance/merge remain separate, review changes route to later implement. |

## Executed verification and provenance

Fresh local commands selected existing tools, not new installations:

```bash
export DOTNET_ROOT="$PWD/artifacts/toolchain/dotnet-10.0.402"
export PATH="$DOTNET_ROOT:$(dirname "$(fnm exec --using=24.22.0 node -p 'process.execPath')"):$PATH"
unset KOFTE_POSTGRES_CONNECTION KOFTE_POSTGRES_PROVISIONING_CONNECTION
```

Actual reports record .NET 10.0.402, Node v24.22.0/npm 11.19.1,
Darwin 25.5.0 arm64, 18 logical CPUs, 64 GiB physical RAM and Python 3.15.0.
Only existing owned private SQLite/disposable PostgreSQL fixtures were used.
The explicit quality suite performs its normal locked restores; no manifest,
lock, dependency or toolchain version is changed.

| Exact command | Actual result |
| --- | --- |
| `SPECIFY_FEATURE_DIRECTORY=specs/003-storage-registry .specify/scripts/bash/check-prerequisites.sh --json --require-spec --require-tasks --include-tasks` | Passed; resolved the recorded feature directory. |
| `python3 scripts/verify.py --suite quality` | Passed 19 named steps, 24.641 seconds; includes format/build/lint/type/dependency and repository checks. |
| `python3 scripts/verify.py --suite unit` | Passed 11 named steps, 5.676 seconds. |
| `python3 scripts/verify.py --suite integration --provider both --disposable-postgres` | Passed SQLite 7 steps / 205.049 seconds and PostgreSQL 9 steps / 194.321 seconds. |
| `python3 scripts/verify.py --suite recovery --provider both --disposable-postgres` | Passed 38 steps, 365.222 seconds, including actual storage/public recovery and owned browser recovery. |
| `python3 scripts/verify.py --suite contract --provider both --disposable-postgres` | Passed 37 steps, 200.257 seconds, including standalone/`all` examples, semantic harness, manifest and native HTTP/OpenAPI/web checks. |
| `python3 scripts/verify.py --suite privacy --provider both --disposable-postgres` | Passed 36 steps, 68.203 seconds, including storage/public/result/worker/native/browser capture. |

All seven fresh safe JSON reports identify assessed head `2d65cd0...`, with
**157/157 named steps passed**, `scope: targeted` and `completeGate: false`.
No new local `--suite all`, standalone browser, startup or failure-propagation
execution is claimed. Publication repository/Python/whitespace checks are
repeated after artifact/ledger writes and recorded in the PR.

Actual assessed-head [product run 39823017354](https://github.com/Stonefly-Labs/Kofte/actions/runs/39823017354)
and [repository run 39823017532](https://github.com/Stonefly-Labs/Kofte/actions/runs/39823017532)
pass all eleven contexts. The caller downloaded ten actual safe JSON reports
and verified distinct suite/provider inventories, outcomes and all **200 steps**:
quality 19, unit 11, integration SQLite 7/PostgreSQL 9, recovery 38, contract 37,
browser 16, privacy 36, startup 9 and failure-propagation 18.
Each report honestly records targeted scope and `completeGate: false`.
Their synthetic merge `7f4d09e93abd3f2c44c46d16f462f3a142515481` has verified parents
main `66f0e4a950bc3590cdb3a25f1622afbd13a0c226` and assessed feature head
`2d65cd0ba7314e1e50fc1be56347f3ebdf4bcca9`.

Fresh passes establish the existing assertions, not the absent R1 regressions.
Historical complete-local implementation evidence is not relabeled a new
complete run. Publication-head checks are separate from assessed-source evidence.

## Limits and handoff

No unresolved within-scope design decision was found. Retain trusted in-process
code rather than sandboxing; narrow assigned int/text DDL with fail-closed
unsupported operations; deliberate stopped complete-set fresh-target recovery;
preserved ambiguous blocks; and operator responsibility for choosing an older
recovery point. Hashes/UUIDs prove consistency, not authenticity or knowledge of
a newer lost backup.

No loader, user/team authorization, product MCP, durable job/event/audit runtime,
new UI, Kubernetes release profile or separately packaged reference module is
claimed. No automatic relocation/deletion/cross-provider conversion/backup
product, cross-module ACID, production readiness or MVP completion is promised.

The next invocation implements **T081**, then later invocations reconverge and
review before requesting explicit human acceptance of numbered feature 003.
No credential, merge or new approval is needed to perform this scoped test work.
