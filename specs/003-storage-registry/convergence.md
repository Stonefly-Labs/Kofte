# Convergence: 003 - Storage Registry and Module Isolation

**Result:** pass

**Date:** 2026-10-22 (UTC).
**Stage:** Follow-up converge after T081; assessment only.
**Assessed source:** `6f6af46f12383471dc241fd322a65f66b0ea297e`.
**Branch / PR:** `feature/003-storage-registry` /
[draft PR #6](https://github.com/Stonefly-Labs/Kofte/pull/6).
**Next stage:** Review, separately on this same branch/draft PR.

## Current method and findings

Executed the installed `speckit-converge` skill with
`SPECIFY_FEATURE_DIRECTORY=specs/003-storage-registry`. Its single prerequisite
invocation used `--json --require-spec --require-tasks --include-tasks` and
resolved the recorded directory. The preceding implement ledger entry covers
all 81 tasks. No before/after hooks are configured: `.specify/extensions.yml`
is absent.

The specification, plan and every task are the intent inventory, governed by
constitution v2.0.1. Referenced contracts explain those obligations; historical
reports, checked boxes and green CI do not substitute for actual implementation
and assertion inspection. Assessment covers contracts, registry SQL/versioning,
allocation/provisioning, owner binding/EF, maintenance supervision, recovery,
diagnostics, host/legacy compatibility, owned fixtures/public consumers and
verification wiring. No later-feature requirement or branch-diff-derived intent
is introduced.

| Findings | Missing | Partial | Contradicts | Unrequested |
| --- | --- | --- | --- | --- |
| Actionable | 0 | 0 | 0 | 0 |

**Severity counts:** CRITICAL 0, HIGH 0, MEDIUM 0, LOW 0.
Checked **40 requirements/outcomes/scenarios** (16 FRs, six buildable SCs,
18 acceptance scenarios), **14 plan decisions**, **81 tasks** and **12
constitutional principles**. No actionable gap, constitutional violation or
unresolved within-scope design decision remains. The implementation satisfies
the assessed spec, plan and tasks; this is not final review or human acceptance.

## Final-review R1 / T081: closed

Both new facts live in the dedicated
`tests/integration/Kofte.Storage.IntegrationTests/Registry/RegistrationTests.cs`
and are inherited by its SQLite and PostgreSQL classes. This is real-provider
coverage, not two provider labels on a substitute.

| Obligation | Actual assertions and supporting mechanism | Decision |
| --- | --- | --- |
| FR-004; T015/T016/T081; US1.2; SC-003 | `ChangedAllocatorIsNeverInvokedForPersistedOwnerAndPreservesInventoryAcrossReopen` persists Alpha/Beta and distinguishable sentinels, releases maintenance and reopens the same installation. A counted changed allocator returns a different valid candidate. Changed-metadata registration requires Ready/Unchanged and **zero allocator calls**. Registration/resolution snapshots compare all eight assignment fields; Beta and both original sentinels remain intact. Installation revision and the complete stopped inventory compare exactly. | Closed |
| FR-002; T016/T081; US1.3/4 | `PersistedNoStorageCannotBecomeRelationalAndPreservesInventoryAcrossReopen` persists a no-storage owner and an unrelated relational owner/sentinel, releases maintenance and reopens. The relational repeat must be Rejected/Unchanged with `storage_conflict` / `correct_configuration`, zero allocator calls and continued NoStorage resolution. Inventory inspection verifies false/no_storage/null key/token/allocator fields, unchanged row/installation revisions and no extra module objects; unrelated assignment/data remain intact. | Closed |
| Evidence quality; principles IV/XI | `StorageAssignmentSnapshot.From` copies contract/owner/provider/installation/allocation/key/allocator/revision. `RecoverySetVerifier.InspectStoppedAsync` requires the held installation scope, reads every sorted registry row, verifies physical ownership/history and exact schema/file inventory, and includes each row revision. Deterministic structural serialization compares the inventories, not collection references. Existing `RegistrySentinel` writes/reads actual original stores. | Sufficient preservation assertions |

Production `StorageRegistry.RegisterAsync` reads the persisted record before
considering allocation and rejects storage-need mismatches symmetrically.
`RegistryRecords` and both SQL scripts retain immutable need/key/provider/token
and revision rules. T081 proves these existing behaviors; no runtime change or
public allocator override is needed. The implementation report's historical
four-case mutation red/green evidence is retained, not rerun or relabeled here.
A fresh execution below independently passes all four provider cases.

Prior F1/T079 and F2/T080 remain closed. Actual public `RegistrationScenarios`
writes Alpha/Beta through assigned contexts, releases maintenance and runs three
fresh admitted/reaped processes; each compares exact assignments and reads
original sentinels after changed metadata. `CollisionScenario` forces a distinct
registered contender through fixture-only `StorageCollisionFixture`, requires
the real UNIQUE rejection, unchanged assignment/sentinel/full inventory and no
contender allocation, and retains separate unowned-target refusal.
`StorageExampleEvidence`, standalone/`all` harnesses and their negative
observation tests prevent positive assertion counts from hiding either omission.

## Complete current coverage

Infrastructure paths below are relative to `src/Kofte.Infrastructure/Storage/`;
integration tests are in `tests/integration/Kofte.Storage.IntegrationTests/`.
Public modules/scenarios and their harness are in `tests/contract/`.

| Intent | Inspected implementation and assertion evidence |
| --- | --- |
| FR-001 | Pure versioned `StorageContracts`; trusted registry/maintenance versus issued owner-only binding; public construction/authority negatives and maintenance configuration tests. |
| FR-002/004 | Stored-record-first registration, explicit no-storage and immutable SQL/record validation; both T081 regressions now complement exact restart, conflicting-need and public retry assertions. |
| FR-003 | Real ID/key/token constraints and exclusive lease; direct invalid-write tests, twenty-by-ten same/distinct-ID independent connection/process races and public forced-collision preservation. |
| FR-005 | ASCII byte-bounded allocator/actual-key checks, dedicated platform and runtime-owned module schemas, PUBLIC revocation, owner markers and assigned local migration history; actual role/object/model tests. |
| FR-006 | Canonical private roots/ancestors, one-link regular files and sidecars, exclusive create and existing-file opens; actual-open traversal/alias/link/hard-link/nonregular/corruption negatives preserve the other store. |
| FR-007/008 | Reservation/physical/ready commits, marker/version/provider/integrity validation and ready-only resolution; actual durable-boundary kills, verified original-allocation retry, ambiguous preserved blocks and missing-ready refusal. |
| FR-009 | Assigned connection/model/cache/history/assembly/snapshot binding, complete pending-set preflight and physical verification; independent Alpha/Beta contexts plus foreign/stale/disposed/replaced-connection/unsupported-operation negatives. |
| FR-010 | Explicit transactional v0/v1 migration runner with digest/history/version checks; real format/upgrade-kill tests and migration history/physical-effect/abandoned-lock recovery/refusal assertions. |
| FR-011 | Monotonic 3/2/30/120-second limits, finite calls/probes, cancelled lost session and admitted worker cleanup; actual elapsed deadlines, live-host contention, cancellation and observer-failure tests. |
| FR-012 | Separate registry/physical commits and module-local contexts/transactions; Alpha-only upgrade and recovery compare actual history/effects, without shared-owner connections or global rollback promises. |
| FR-013 | Explicit provider/root/credential sources, distinct offline PostgreSQL provisioner and unchanged runtime no-CREATE posture; owned fixture identity, denied/missing/mismatched authority, outage and corrected-retry tests. |
| FR-014 | Seven disjoint outcomes, fixed diagnostics/correlation, bounded records/spans/events/metric labels; captures assert all seven results and no seeded credential/path/payload disclosure across public and worker channels. |
| FR-015 | Complete inventory/revision/marker/history/hash verifier, actual SQLite backup/PostgreSQL custom dump and fresh restore; mixed/stale/missing/extra/unsafe/legacy source/destination/reset refusals preserve originals. |
| FR-016 | Nine executable both-provider public examples, semantic observations and retained module-free host/health/OpenAPI/HTTP/web/browser gates. No loader, new public endpoint or authorization bypass. |
| SC-001; US3.1/2 | Independent module contexts, identical logical tables/history and real Alpha-only additive upgrade retain Beta data/model/history and reject concurrent maintenance. |
| SC-002; US2.1-4 | Full twenty-round ten-caller/process matrix, direct database constraints and deterministic registered/reserved/unowned collision cases inspect actual ownership/inventory. |
| SC-003; US1.1-4 | Distinct storage/no-storage, exact three-process restart/data proof, missing-ready refusal and the two now-executed T081 immutable-registration cases. |
| SC-004; US4.1-3 | Provisioning/migration/upgrade commit kills, actual native SQLite deadline supervision, outage/contention/cancellation/session-loss cleanup and ownership-verified recovery or explicit preserved block. |
| SC-005; US3.3/4, US5.1 | Forbidden owner/path/model/migration negatives preserve outside targets; all-seven-outcome captured privacy checks require no seeded disclosure. |
| SC-006; US4.4, US5.2 | Public install/retry/upgrade/recovery corpus, actual full-set fresh-target restores, distinct corrected retry preserving unrelated data and fail-closed foundation verification. |

| Plan decisions | Current implementation/evidence |
| --- | --- |
| D01 pure contracts/one adapter; D02 reviewed dependencies/test ownership | Provider-free platform contracts, justified infrastructure and native DI; reviewed exact pins/locks/notices and normally included dedicated tests. No EF Design/tool/mediator adoption. |
| D03 immutable allocator; D04 real versioned registry | Allocator, immutable SQL, record/version/digest checks, direct constraints, stored retries and new allocator-evolution regression. |
| D05 reused lease; D06 separate offline authority; D07 private SQLite boundary | Shared installation lease/scope, non-pooled verified PostgreSQL session and restricted roles; canonical paths/exclusive creation/actual-open negatives. |
| D08 durable phases; D09 assigned EF; D10 migration checkpoint/recovery | Actual commit barriers/markers, assigned factory/cache/assembly/preflight and physical verifier, explicit history/effect/abandoned-lock recovery with refusal tests. |
| D11 finite supervision; D12 explicit upgrade/unchanged serving | Monotonic budget/admitted worker supervisor and actual deadlines; strict host dispatch/configuration, no automatic migration, v0/v1 and legacy guards. |
| D13 coherent recovery; D14 safe diagnostics/examples/existing gates | Actual provider backups/fresh restores, complete-set verification, captured telemetry, semantic public corpus and mandatory suite inventories/selectors. |

| Every task assessed | Current evidence/disposition |
| --- | --- |
| T001-T006 | Explicit ADR approval, reviewed dependency graph, actual projects/solution, fail-closed selectors and shared owned provider/process fixtures. |
| T007-T014 | Public contracts/results, budget/scope/path/lease mechanisms, real registry scripts/version runner, initialization/constraint and maintenance configuration tests. |
| T015-T027 | Allocator boundaries and durable registration/restart/no-storage/binding/compatibility/public examples; T081 now supplies both missing T015/T016 assertions and qualifies the former T026 completeness claim. |
| T028-T035 | Full connection/process race matrices, direct/forced constraints, bounded conflicts, actual stored mappings and public guidance; T080 retains the required public registered-owner proof. |
| T036-T047 | Assigned model/cache/history/pending preflight, physical/actual-open verification, independent integration/public modules and Alpha-only upgrade. Narrow supported DDL is documented, not silently broadened. |
| T048-T063 | Real killed processes/deadlines/lost leases, explicit upgrade/legacy refusal, verified migration recovery and actual complete provider backup/restore plus executable public guide cases. Shared support satisfies behavior without unused duplicate files. |
| T064-T070 | Seven bounded results/captured channels, real corrected retry, complete nine-example corpus and honest operational/trust/retry documentation. |
| T071-T078 | Required verifier/CI inventories/selectors and negative Python tests, reviewed locks/notices, directly related docs, historical full local gate and actual assessed-head CI. No new local all-suite claim. |
| T079-T080 | Required public restart/data and forced registered-owner collision observations, strengthened standalone/`all` gates and retained negative harness tests. |
| T081 | Both inherited facts inspected and all four real-provider cases freshly pass; exact preservation assertions close R1 without product/API/dependency changes. |

All twelve principles were checked: I portable module-free Köfte/brand; II
minimal versioned trusted contracts; III native DI, dedicated test ownership
and reviewed licensing; IV immutable unique schema/file/history ownership; V
honest local transactions/reconciliation; VI least privilege, boundary checks
and private diagnostics; VII no new MCP surface or future authorization bypass;
VIII durable storage intent, not a job/event/realtime runtime; IX no new visual
journey and retained frontend regression gates; X portable configuration,
upgrade/budgets/recovery; XI actual provider/consumer/failure evidence; XII
approved 003 scope, append-only convergence and one-stage human-owned delivery.
Later module/auth/MCP/job/deployment/reference-module requirements remain later
work, not missing 003 implementation or waived MVP gates.

## Current verification and limits

Commands selected existing .NET SDK 10.0.402 and Node 24.22.0/npm 11.19.1,
with inherited PostgreSQL runtime/provisioning connections unset:

```bash
export DOTNET_ROOT="$PWD/artifacts/toolchain/dotnet-10.0.402"
export PATH="$DOTNET_ROOT:$(dirname "$(fnm exec --using=24.22.0 node -p 'process.execPath')"):$PATH"
unset KOFTE_POSTGRES_CONNECTION KOFTE_POSTGRES_PROVISIONING_CONNECTION
```

| Exact command | Observed result |
| --- | --- |
| `dotnet build tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj --no-restore -c Release --nologo -v quiet` | Passed; zero warnings/errors. |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore -c Release --filter-method '*ChangedAllocatorIsNeverInvokedForPersistedOwnerAndPreservesInventoryAcrossReopen' --filter-method '*PersistedNoStorageCannotBecomeRelationalAndPreservesInventoryAcrossReopen' --minimum-expected-tests 4` | Passed all four cases, zero skipped; 15.326 seconds. |
| `python3 scripts/verify.py --suite unit` | Passed all 11 named steps; 6.070 seconds. |
| `python3 scripts/verify.py --suite contract --provider both --disposable-postgres` | Passed all 37 named steps, including both-provider standalone and complete public storage examples, semantic harness regressions and retained manifest/HTTP/OpenAPI checks; 207.475 seconds. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed all 111 tests. |
| `git diff --check` | Passed. |

The two fresh safe reports identify assessed head
`6f6af46f12383471dc241fd322a65f66b0ea297e`, Darwin 25.5.0 arm64,
18 logical CPUs, 64 GiB RAM and Python 3.15.0. They pass **48 targeted named
steps**, separately from the four focused provider cases, and retain
`completeGate: false`. No new local full integration, recovery, privacy,
browser, quality or `--suite all` run is claimed. Wider evidence below is
actual assessed-head CI, not substituted local execution.

Actual assessed-head [product CI run 39843780526](https://github.com/Stonefly-Labs/Kofte/actions/runs/39843780526)
and [repository CI run 39843780395](https://github.com/Stonefly-Labs/Kofte/actions/runs/39843780395)
are successful for `6f6af46f12383471dc241fd322a65f66b0ea297e`; all eleven
contexts are SUCCESS. All ten actual safe product reports were downloaded and
inspected: **200/200 steps pass** (quality 19, unit 11, integration SQLite
7/PostgreSQL 9, recovery 38, contract 37, browser 16, privacy 36, startup 9,
failure-propagation 18). Their exact synthetic merge is
`54627cad96dc5ee62b2478abd36a8a825b50ea57`; GitHub commit provenance verifies
parents main `66f0e4a950bc3590cdb3a25f1622afbd13a0c226` and assessed head
`6f6af46f12383471dc241fd322a65f66b0ea297e`. Each report honestly remains
`scope: targeted`, `completeGate: false`; their sum is not a new local all-suite.

The initial PostgreSQL timing failures and unchanged passing reruns in the
T081 implementation report remain historical and do not establish a root cause
or eliminate timing sensitivity. No budget/assertion/gate is weakened here.
Other limits remain: trusted code is not sandboxed; assigned DDL is deliberately
narrow; ambiguous evidence stays blocked; recovery is stopped, complete-set
and fresh-target; choosing an older point entails operator-owned data loss.
No production readiness, human usability study or MVP completion is claimed.

## Current disposition

No task is appended and no empty convergence phase is created. `tasks.md`
remains byte-for-byte unchanged at SHA-256
`9d737096a2e927be6737eedbe8526460a7b8292a8cc377743b30a09aa115cec4`.
Spec, plan, contracts, implementation report, historical review, checklists,
product/test code, dependencies and approved design remain unchanged.
Only this report, directly related progress docs and the helper ledger advance.
**Review is next**, not executed here; 003 acceptance remains explicitly human
and separate. Keep the original PR draft, without an intermediate merge request.

## Previous follow-up convergence (historical, before final-review R1/T081)

The following assessment is preserved verbatim as historical evidence. Its
80-task completeness claims were subsequently qualified by final-review R1;
the current assessment above checks all 81 tasks and closes that evidence gap.

**Result:** pass

**Date:** 2026-10-22 (UTC).
**Stage:** Follow-up converge only; no product/test implementation, final review
or feature acceptance.
**Assessed source:** `151d75ff4223b760500e6392ae9cd8b555cc49bd`.
**Branch / PR:** `feature/003-storage-registry` /
[draft PR #6](https://github.com/Stonefly-Labs/Kofte/pull/6).
**Next stage:** Review, in a separate invocation on this same branch/draft PR.

## Follow-up method and findings

Executed the installed `speckit-converge` skill with the recorded
`SPECIFY_FEATURE_DIRECTORY=specs/003-storage-registry`; the prerequisite command
`--json --require-spec --require-tasks --include-tasks` resolved that directory.
The preceding implement ledger entry covers the current 80-task file.
`.specify/extensions.yml` is absent, so no before/after convergence hooks apply.

The specification, plan and **every existing task**, governed by constitution
v2.0.1, are the intent inventory. Referenced contracts explain the planned
boundaries; checked boxes, the earlier report and passing CI are not substituted
for inspecting source and assertions. Assessment covers current registry SQL,
allocation/provisioning, binding/EF, maintenance supervision, recovery, diagnostics,
host integration, test-owned fixtures/consumers and named verification wiring.
It does not infer work from a branch diff or add later-feature requirements.

| Findings | Missing | Partial | Contradicts | Unrequested |
| --- | --- | --- | --- | --- |
| Actionable | 0 | 0 | 0 | 0 |

**Severity counts:** CRITICAL 0, HIGH 0, MEDIUM 0, LOW 0.
Checked **40 requirements/outcomes/scenarios** (16 FRs, six buildable SCs,
18 acceptance scenarios), **14 plan decisions**, **80 tasks** and **12
constitutional principles**. No constitutional violation or unresolved
within-scope design decision was identified.

### Prior findings closed by actual public behavior

Paths below are relative to `tests/` unless prefixed otherwise.

| Finding / remediation | Current source and actual assertion evidence | Decision |
| --- | --- | --- |
| F1 / T079; FR-004/016, SC-003/006, US1.2 | `contract/Kofte.Storage.Consumer/Scenarios/RegistrationScenarios.cs` writes distinguishable Alpha/Beta records through their public assigned contexts, captures all eight assignment fields, and disposes contexts/bindings/maintenance before launching children. Three sequential `StorageRestartProcess.RunAsync` calls reopen the same installation; each admitted worker re-registers changed display/release metadata, resolves both assignments and reads both original records without rewriting them. Snapshots are compared exactly; the result requires three distinct non-parent process IDs, six assignment comparisons and six retained reads. `integration/Kofte.TestSupport/StorageRestartProcess.cs` uses the existing admitted supervisor, bounded output and strict result parsing; each child is reaped before returning. Conflicting-need refusal remains. | Closed; real restarted consumers, not three registry objects or asserted totals. |
| F2 / T080; FR-003/016, SC-002/006, US2.3 | `contract/Kofte.Storage.Consumer/Scenarios/CollisionScenario.cs` registers/migrates an original owner and writes its sentinel. `integration/Kofte.TestSupport/StorageCollisionFixture.cs` requires a distinct contender and supplies the existing key through the internal allocator seam; the unchanged registry executes the real INSERT/UNIQUE constraint. The example requires `storage_conflict`, `correct_configuration`, unchanged durability and elapsed time below 30 seconds. Exact original assignment, sentinel and complete stopped registry/physical inventory match before/after; contender resolution proves no alternate allocation. The separate unowned occupied-target/no-adoption case still executes. | Closed; forced registered-owner conflict, not only unowned-target refusal. |
| Standalone and `all` gate; T017/T029/T035/T068/T079/T080 | `StorageConsumerHarness`, `StorageExampleEvidence.Read`, consumer supervisor and both `RegistrationContractTests`/`StorageGuideTests` require the semantic observations. `StorageConsumerHarnessTests.PositiveAssertionTotalsWithoutRequiredObservationsCannotPass` first accepts a complete result, then removes observations or decrements each required field and requires failure despite 100 assertions per case. Both providers execute the standalone cases and full nine-example corpus. | Closed; green counts cannot hide either missing example again. |

The only forced-allocation access is fixture-owned infrastructure arrangement.
Public Alpha/Beta implementations use public binding/EF contracts; there is no
production candidate override or changed runtime rule. Separate core restart/
collision integration tests remain, but are not used to stand in for the
required public examples.

## Current requirement, outcome and acceptance assessment

Production paths are relative to `src/Kofte.Infrastructure/Storage/`.
Integration test names refer to
`tests/integration/Kofte.Storage.IntegrationTests/`; public examples and their
independent harness live in `tests/contract/`.

| Intent | Current implementation and assertion coverage | Assessment |
| --- | --- | --- |
| FR-001 | Versioned pure `StorageContracts.cs`; trusted registry/maintenance versus owner-only `ModuleStorageBinding`, public metadata/forged-binding checks and maintenance-only configuration. | Satisfied |
| FR-002 | `StorageRegistry` and both real registry scripts persist one immutable need/owner with explicit no-storage. Registration/public tests inspect distinct allocations and zero no-storage objects. | Satisfied |
| FR-003 | Real ID/key/token UNIQUE constraints, immutable triggers, exclusive maintenance, direct bypass tests and 20-by-ten same/distinct-ID connection/process races. Public forced collision now preserves the registered owner. | Satisfied |
| FR-004 | Stored key/version/token/revision, not regenerated names; harmless metadata retries, changed allocator test, exact process-restart snapshots and conflicting-need refusal. | Satisfied |
| FR-005 | Bounded ASCII allocator/actual-key validation, PostgreSQL platform schema, separate runtime-owned module schemas/markers, PUBLIC revocation and assigned local history. Constraint/role/model tests inspect actual objects. | Satisfied |
| FR-006 | `ModuleStorePaths`/`UnixStorePaths`, private regular one-link files, ancestors/sidecars, exclusive creation and existing-file opens. Alias/link/hard-link/nonregular/corruption negatives preserve other-store bytes. | Satisfied |
| FR-007 | Separate reservation/physical/ready commits and owner markers. Actual killed-process phases retain incomplete evidence; verified retry completes only the original allocation, ambiguous pre-marker state stays preserved/blocked. | Satisfied |
| FR-008 | Registry/version/provider/marker/integrity verification at resolve/bind/open; missing/corrupt/foreign ready stores fail without replacement creation or fallback. | Satisfied |
| FR-009 | Assigned options/final model/cache/history/migrations/snapshots, complete pending-set preflight, command/transaction revalidation and independent Alpha/Beta models. Foreign, stale, disposed, replacement-connection and unsupported-operation negatives reject. | Satisfied |
| FR-010 | Explicit transactional v0-to-v1 runner with script digest/version/history consistency; upgrade kills, v1 retry, populated/future/corrupt refusal, durable migration intent and verified forward/abandoned-lock recovery. | Satisfied |
| FR-011 | Shared monotonic 3/2/30/120-second budgets, finite provider timeouts/25-ms probes, Busy-only external retry, cancelled lost lease and admitted worker termination/reaping within total limits. Deadline/observer/session-loss tests assert cleanup and truthful outcomes. | Satisfied |
| FR-012 | Module-local contexts/transactions, separate registry/physical commits and explicit recovery comparing intent/history/physical effects; no cross-module atomicity or connection contract. | Satisfied |
| FR-013 | Explicit provider/root/private credential configuration, separate offline authority and unchanged runtime no-CREATE restrictions; denied/missing/mismatched/outage/correction cases use owned real fixtures. | Satisfied |
| FR-014 | Seven disjoint outcomes, fixed bounded safe diagnostics, generated correlation, structured records/spans/events and bounded metric labels. Actual privacy captures cover results, logs, telemetry and worker/console channels with canaries. | Satisfied |
| FR-015 | Complete inventory/marker/history/revision/hash validation, actual SQLite backup/PostgreSQL dump and fresh-target restore. Mixed/stale/unsafe/member/legacy reset/restore failures preserve originals. | Satisfied |
| FR-016 | All nine public examples now execute their prescribed behavior, including T079/T080; existing pure manifest, host/HTTP/OpenAPI/web/browser regressions remain in the same verifier/CI. No loader or public administration surface. | Satisfied |
| SC-001 | Two independent public module contexts/provider, identical logical tables/history, actual writes/reads and Alpha-only additive upgrade retaining Beta/model/history/data. | Satisfied |
| SC-002 | Full twenty-round, ten-caller same/distinct-ID connection/process matrix; actual inventories and direct/forced collision preservation, now also in the public collision example. | Satisfied |
| SC-003 | Three actual restarted processes retain both exact assignments and original data; no-storage creates no store and missing-ready tests reject silent recreation. | Satisfied |
| SC-004 | Provisioning/migration/upgrade before/after-commit kills, native SQLite callback, real total deadlines, lease loss, cancellation and explicit verified recovery or preserved block. | Satisfied |
| SC-005 | Reserved/foreign/path/alias/forged-owner and migration preflight negatives plus seven-outcome captured privacy checks require zero forbidden changes/disclosures. | Satisfied |
| SC-006 | Executable install/retry/isolated-upgrade/complete-set-restore/bootstrap-upgrade examples on both providers, fail-closed named gates and actual assessed-head CI. No human-usability or production-readiness claim. | Satisfied |

| Scenarios | Actual acceptance evidence |
| --- | --- |
| US1.1-4 | `RegistrationTests`, `RestartTests`, real immutable constraints and public registration cases: distinct storage, exact restarted data, no-storage and rejected need/owner/provider/location changes. |
| US2.1-4 | `RegistrationConcurrencyTests`, `AllocationCollisionTests`, `RegistryConstraintTests` and public collision: same-ID convergence, distinct ownership, forced/reserved/unowned refusal, direct constraints and inconsistent-reopen rejection. |
| US3.1-4 | `ModuleMigrationTests`, `AssignedModelTests`, `StorageBoundaryTests` and public module cases: local clean install/history, Alpha-only upgrade/maintenance contention, foreign/path/binding refusal and no missing/corrupt-store replacement. |
| US4.1-4 | Provisioning/migration crash, upgrade/deadline/lease-loss/recovery refusal and complete-set tests plus public interrupted/restore cases: truthful incomplete state, bounded failure, compatible upgrade and original ownership/data after fresh restore. |
| US5.1-2 | `StoragePrivacyTests`, `StorageDiagnosticsTests`, `CorrectedRetryTests` and public corrected retry: all channels safe/correlated, distinct successful retry, original reservation and unrelated owner/data/history retained. |

## Plan decisions, every task and constitutional constraints

| Plan decision | Current source/evidence |
| --- | --- |
| D01 Minimal pure contracts/one adapter/trusted composition | Platform contracts, infrastructure binding and public consumers; no host reference required by module-author infrastructure API. |
| D02 Reviewed exact EF/provider graph and test ownership | Central pins/locks, reviewed inventory/notices/provenance, dedicated normally included test source/contexts/migrations. No EF Design/tool adoption. |
| D03 Immutable bounded allocator v1 | `StorageAllocator`, database key constraints, boundary tests, stored-key retries and forced candidate tests. |
| D04 Versioned platform registry and real constraints | Embedded PostgreSQL/SQLite SQL, `RegistryMigrationRunner`, metadata/history/digest/revision and real constraint/upgrade/corruption assertions. |
| D05 Reused exclusive installation lease | Shared `StoreLease`, non-pooled PostgreSQL session, `MaintenanceScope`; foreign/disposed/cancelled/contention/session-loss tests. |
| D06 Separate offline PostgreSQL authority | `PostgresIdentity`/`PostgresStorageProvisioner`, owned role/OID/comment fixture checks, runtime negative privileges and correction tests. |
| D07 Canonical private SQLite file ownership | `ModuleStorePaths`, `SqliteStorageProvisioner`, shared Unix safety, actual-open link/alias/sidecar/missing-store negatives. |
| D08 Explicit marker/reservation/physical/ready phases | `StorageOwnerMarker`/`StorageRegistry`, killed durable-boundary matrices and original-allocation retry/preserved blocks. |
| D09 Assigned EF models/cache/history/pending preflight | `Migrations/` factory/assembly/snapshot/interceptors/validator/verifier and independent public/integration contexts. |
| D10 Durable migration checkpoint and deliberate recovery | `ModuleMigrationRunner`/`ModuleMigrationRecovery`, history-bearing transaction kills and unknown lock/history/physical refusal. |
| D11 Shared finite budgets and supervised synchronous work | `OperationBudget`/`StorageMaintenanceProcess`, host supervisor/worker, real deadline and observer-failure cleanup tests. |
| D12 Explicit upgrade/unchanged serving/legacy guards | Host maintenance dispatch/configuration, startup reconciliation and bootstrap recovery guards with v0/v1/unknown syntax/destructive-command regressions. |
| D13 Stopped coherent full-set recovery | `RecoverySetVerifier`, actual provider backup/dump/fresh restore, manifest/inventory/marker/checkpoint/sentinel and mixed/stale refusal assertions. |
| D14 Safe diagnostics and executable examples in existing gates | `StorageDiagnostics`, captured privacy/public corpus, semantic restart/collision gates and verifier/CI mandatory inventories/selectors. |

| Tasks assessed | Evidence / disposition |
| --- | --- |
| T001-T006 | Exact human ADR approval; reviewed dependency graph; actual adapter/test projects, fail-closed scenario/provider selectors and reused owned fixture/process resources. |
| T007-T014 | Public contract shapes, budget/scope unit tests, reused lease/path implementation, both registry scripts/version runner, real initialization/constraints and maintenance-only configuration. |
| T015-T027 | Allocator boundaries, durable registration/restart/no-storage/marker/binding, zero-module compatibility and executable author examples. T017's former public omission is closed by T079. |
| T028-T035 | Complete independent connection/process race matrix, direct/forced constraints, known conflict/busy outcomes, stored mapping checks and public guidance. T029/T035's former omission is closed by T080. |
| T036-T047 | Assigned model/cache/history/pending-operation isolation, physical verification, both independent module implementations and all boundary/upgrade/guide assertions. |
| T048-T063 | Actual durable-boundary kills, real deadlines/session loss, supervision/verified recovery, explicit upgrade/legacy guards, complete provider backups/fresh restores and public recovery examples. Shared test-support `SqliteRecoveryExercise` and `StorageGuideTests`/`RecoveryContractTests` implement the planned behavior without duplicate guide files. |
| T064-T070 | Seven bounded public results and captured diagnostic channels, distinct corrected retry preserving data, full nine-case corpus and safe operation/retry documentation. T068 now also requires the missing public observations. |
| T071-T078 | Existing verifier/CI inventories/selectors and fail-closed Python regressions; reviewed locks/notices, coherent progress/operational docs and actual original complete-local plus current assessed-head CI evidence. Historical local results are not relabeled current. |
| T079-T080 | Actual independent public three-restart/data and distinct-registered-owner collision observations, strengthened standalone/`all` harnesses, negative harness regressions and executed both-provider examples; closure detailed above. |

All **T001-T080** are included, not merely unchecked or most recent tasks.
Consolidated registry coordination in `RegistryMigrationRunner`, reused
test-support recovery and a narrow documented DDL allowlist satisfy behavior
without requiring unused duplicate planned files or unrequested general EF
operation support.

| Principle | Evidence / applicability |
| --- | --- |
| I Product/brand | Köfte prose, module-free portable host, no Hoard dependency. |
| II Modularity | Canonical 002 identities, small versioned storage contracts and owner binding; no module loader or hostile-code isolation promise. |
| III Slices/test ownership/licensing | Native DI, justified adapter, test source in owning test projects, reviewed pinned dependencies; no MediatR/AutoMapper or production-linked test source. |
| IV Storage | Immutable database uniqueness and concurrency, safe actual schema keys, separate private files, assigned tables/history and real-provider proof. |
| V Transactions | Explicit distinct durable commits, module-local transactions and verified reconciliation; no cross-module ACID guarantee. |
| VI Safety | Separate provisioner, restricted runtime, owner/path/physical refusal and safe captured diagnostics. Future actor/team/resource policy is not bypassed. |
| VII MCP | No new server/tool/discovery surface; future same-operation authorization remains later work. |
| VIII Durable work | Durable storage intent/checkpoints only, not an event/job/audit/realtime runtime or exactly-once promise. |
| IX Frontend | No new visual journey/extension; existing web/browser/accessibility regressions remain wired and pass assessed-head CI. |
| X Portability/operations | SQLite without cloud/Kubernetes/Docker, owned PostgreSQL, explicit configuration/upgrades/budgets and stopped recovery responsibilities. |
| XI Evidence | Real providers, constraints/races/crashes/deadlines/privacy and public consumer assertions; omitted public proof is now supplied, not waived. |
| XII Scope/governance | Approved 003 intent/ADR, all tasks assessed, append-only/no-empty-phase semantics, original draft PR, one converge stage only and no self-acceptance. |

## Follow-up verification and limits

Fresh local checks selected the existing .NET SDK 10.0.402 and Node
24.22.0/npm 11.19.1, with inherited PostgreSQL runtime/provisioning settings
unset. The reports record Darwin 25.5.0 arm64, 18 logical CPUs, 64 GiB RAM and
Python 3.15.0. PostgreSQL uses only the existing owned disposable fixture;
no tool, package or restore lock was changed.

```bash
export DOTNET_ROOT="$PWD/artifacts/toolchain/dotnet-10.0.402"
export PATH="$DOTNET_ROOT:$(dirname "$(fnm exec --using=24.22.0 node -p 'process.execPath')"):$PATH"
unset KOFTE_POSTGRES_CONNECTION KOFTE_POSTGRES_PROVISIONING_CONNECTION
```

| Exact command | Observed result |
| --- | --- |
| `python3 scripts/verify.py --suite unit` | Passed all 11 named steps, including platform/host/web units and Python tests; 9.810 seconds. |
| `python3 scripts/verify.py --suite contract --provider both --disposable-postgres` | Passed all 37 named steps, including both-provider standalone/`all` public storage examples, semantic harness regressions, manifest corpus/schema and native host/OpenAPI/HTTP/web/output checks; 270.956 seconds. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed all 111 tests. |
| `git diff --check` | Passed. |

The two fresh safe verification JSON reports identify assessed head
`151d75ff4223b760500e6392ae9cd8b555cc49bd`; both are targeted passes,
**48 named steps** total. No new local complete `--suite all`, integration,
recovery, privacy, browser or quality-suite run is claimed by convergence.
Their wider evidence below is actual assessed-head CI, not fabricated local
execution or a replacement for inspecting the assertions.

The actual [product CI run 39819229115](https://github.com/Stonefly-Labs/Kofte/actions/runs/39819229115)
and [repository CI run 39819229361](https://github.com/Stonefly-Labs/Kofte/actions/runs/39819229361)
both identify assessed feature head `151d75ff4223b760500e6392ae9cd8b555cc49bd`
and completed successfully. All eleven PR contexts pass.
Downloaded all ten safe product JSON artifacts and verified every report and
all **200 named steps** passed: quality 19, unit 11, integration SQLite 7,
integration PostgreSQL 9, recovery 38, contract 37, browser 16, privacy 36,
startup 9 and failure-propagation 18.
Their reported revision `3bb5894570420e628c3caa94764b05aada214672` is the
actual synthetic merge; the GitHub commit API confirms its parents are updated
main `66f0e4a950bc3590cdb3a25f1622afbd13a0c226` and the assessed feature head.
These are actual matrix runs with `scope: targeted`/`completeGate: false`,
not a newly claimed complete local `--suite all` run.

`tasks.md` is **byte-for-byte unchanged**:
SHA-256 `ba34dada5adbc45b979dfa3210f2ba1c0df76362983c83817d9b9fc163092fdd`.
No new task or empty convergence header is appended. Spec, plan, contracts,
checklists, ADR, implementation evidence, product/test source, packages, locks
and verification configuration remain unchanged by this stage.

Supported limits remain explicit: trusted in-process code is not sandboxed;
assigned DDL is the documented int/text create-table/primary-key/add-column
subset; ambiguous SQLite pre-marker/journal/unknown lock/history/physical
evidence is preserved and blocked; restore requires a stopped complete set and
fresh owned target. An older chosen recovery point has operator-owned data
loss; UUIDs/hashes do not detect malicious forgery or an unknown newer lost
backup. No universal kernel-blocked-disk deadline, installation-wide ACID,
automatic backup product, later module/policy/MCP/job/deployment functionality,
human-usability study, production readiness or MVP completion is inferred.

**Converged — the implementation satisfies the spec, plan, and tasks.**
Review is next, not executed here. Keep PR #6 draft through final review and
explicit numbered-feature human acceptance; no merge is requested.

## First convergence (historical gaps checkpoint)

The following assessment and its evidence are preserved unchanged. Its gaps
and implement handoff describe the earlier source, not the current result above.

**Result:** gaps

**Date:** 2026-10-22 (UTC).
**Stage:** Converge only; no product/test implementation or feature acceptance.
**Assessed source:** `85e566b055451d6ef0342011f598e4a721484e40`.
**Branch / PR:** `feature/003-storage-registry` /
[draft PR #6](https://github.com/Stonefly-Labs/Kofte/pull/6).
**Next stage:** Implement T079-T080 on this same branch/PR, then a separate
follow-up convergence. Do not proceed to final review or acceptance yet.

## Method and scope

Executed the installed `speckit-converge` skill with
`SPECIFY_FEATURE_DIRECTORY=specs/003-storage-registry` and the prerequisite
command `--json --require-spec --require-tasks --include-tasks`.
The context resolved to this recorded directory. No before/after convergence
hooks are configured: `.specify/extensions.yml` is absent.

Intent comes from the specification, plan and all 78 original tasks, governed
by constitution v2.0.1. Referenced design contracts explain the plan's required
public examples; the implementation report and green checks are evidence to
inspect, not replacement requirements. Assessment examines current source,
actual assertions, public consumers, provider fixtures and verification wiring,
not branch-diff size or checked task markers.

Checked **40 requirements/outcomes/scenarios**: 16 FRs, six buildable SCs and
18 acceptance scenarios. Also checked **14 plan decisions**, **78 original
tasks** and **twelve constitutional principles**. The two partial public-example
gaps affect FR-016, SC-006 and their task/plan coverage. The corresponding core
restart and forced-collision behavior has separate integration evidence; this
report does not allege a demonstrated storage-engine defect.

## Findings

| ID | Gap type | Severity | Source | Evidence | Remaining work |
| --- | --- | --- | --- | --- | --- |
| F1 | partial | HIGH | FR-016; SC-006; US1.2 / SC-003 public example; T017/T068; plan public registration/restart validation | `contracts/storage-v1.md:189` requires changed metadata and three restarts preserving mapping/data. `tests/contract/Kofte.Storage.Consumer/Scenarios/RegistrationScenarios.cs:14-39` registers, changes metadata and immediately retries/resolves through the same registry. It neither writes a sentinel nor restarts a process. `RegistrationContractTests.cs:15-18` invokes this once and only checks an assertion total; `StorageGuideTests.cs:10-17` checks scenario names/counts, not the missing restart observations. | T079: make standalone and `all` public examples perform three genuine restarts with retained module data and exact assignment checks on both providers. |
| F2 | partial | HIGH | FR-016; SC-006; US2.3 / SC-002 public example; T029/T035/T068; plan forced-collision/public-consumer proof | `contracts/storage-v1.md:190` and T029 require a forced distinct-ID candidate conflict preserving an existing registered owner. `tests/contract/Kofte.Storage.Consumer/Scenarios/CollisionScenario.cs:16-58` creates an unowned occupied object for one candidate, then registers that same ID and expects reconciliation-required. No original registered module or second distinct ID participates. The public harness again accepts names/positive counts. | T080: add the distinct-registered-owner collision through fixture-only arrangement, asserting real conflict, unchanged original assignment/data/inventory and no alternate allocation; retain the existing unowned-object refusal. |

Evidence paths without a repository prefix in the findings table are relative
to this feature, or to
`tests/contract/Kofte.Platform.ContractTests/Modules/Storage/` for the named
contract test files.

**Gap counts:** missing 0, partial 2, contradicts 0, unrequested 0.
**Severity counts:** CRITICAL 0, HIGH 2, MEDIUM 0, LOW 0.
No additional constitutional implementation violation or unresolved design/scope
decision was identified within this assessment. Green existing checks do not
close either gap: their public-example assertions omit the promised behavior.

F1 is not absence of restart protection: integration
`Registry/RestartTests.cs:17-57` performs three real child-process restarts,
checks immutable assignment and reads both sentinels. F2 is not absence of
durable collision protection: integration
`Registry/AllocationCollisionTests.cs:11-37` forces distinct IDs to one key
and checks conflict plus preserved owner/data/inventory. Both paths are under
`tests/integration/Kofte.Storage.IntegrationTests/`. They do not make the
separately prescribed public examples execute those behaviors.

## Requirement and outcome assessment

Source paths below are relative to `src/Kofte.Infrastructure/Storage/` unless
otherwise stated. Test paths are relative to
`tests/integration/Kofte.Storage.IntegrationTests/`; public evidence lives in
the independent `tests/contract/Kofte.Storage.Consumer` and its contract harness.

| Intent | Current implementation and actual assertion coverage | Assessment |
| --- | --- | --- |
| FR-001 | Pure version-1 contracts in `src/Kofte.Platform/Modules/Storage/StorageContracts.cs`; trusted registry/maintenance and owner-only `ModuleStorageBinding`; public binding/configuration negatives. | Supported |
| FR-002 | `Registry/StorageRegistry.cs`, `RegistryRecords.cs` and both registry SQL scripts; registration/no-storage/conflicting-need tests and final row/object inventories. | Supported |
| FR-003 | Actual ID/key/token UNIQUE constraints, immutable triggers and exclusive maintenance; `RegistryConstraintTests`, forced collision tests and complete process/connection race matrix. | Supported; public collision recipe separately partial in F2 |
| FR-004 | Persisted keys/tokens read rather than regenerated; metadata retry, three process restarts, stale binding and original-reservation recovery assertions. | Supported; public retry recipe separately partial in F1 |
| FR-005 | Safe bounded allocator, platform-local PostgreSQL registry/history, separate runtime-owned schema/marker, PUBLIC revocation and assigned table/history validation. | Supported |
| FR-006 | `Sqlite/ModuleStorePaths.cs`, private regular one-link files and ancestors/sidecars, exclusive creation/existing-file opens; link/alias/missing/corrupt and recovery-set negatives. | Supported |
| FR-007 | Reservation, physical marker and ready commits remain distinct; actual process-death matrices and preserved ambiguous pre-marker reconciliation blocks. | Supported |
| FR-008 | Registry/provider/version/physical-marker/integrity verification at resolution and actual open; missing ready targets are not recreated. | Supported |
| FR-009 | Assigned context/options/cache/migration assembly/snapshot and final-model/pending-operation validation; independent Alpha/Beta models and forged/foreign/disposed/stale binding negatives. | Supported |
| FR-010 | Explicit transactional v0-to-v1 upgrade, script digest/history consistency, future/corrupt refusal, migration intent/checkpoint recovery and legacy binary/data incompatibility guards. | Supported |
| FR-011 | Monotonic budgets, bounded lease probes/provider calls, Busy-only external retries, lost-session cancellation and supervised native 30/120-second termination/reaping assertions. | Supported |
| FR-012 | Module-local EF transactions and separate registry/physical commits; explicit recovery compares durable intent, real history and physical effects instead of assuming global rollback. | Supported |
| FR-013 | Explicit owned provider/root configuration, separate offline PostgreSQL authority, unchanged runtime no-database-CREATE checks, outage/privilege/cancellation and corrected-retry tests. | Supported |
| FR-014 | Seven disjoint outcomes, fixed safe bounded diagnostics, correlated structured records/spans and bounded metric labels; actual result/log/span/event/metric/stdout/stderr/worker capture with canaries. | Supported |
| FR-015 | Complete provider backup/dump plus manifest/inventory/hash/marker/history verification, fresh-target restore and non-destructive mixed/stale/legacy refusal assertions. | Supported |
| FR-016 | Independent public consumer, nine named cases and unchanged module-free bootstrap/manifest/HTTP/web checks exist, but two named examples do not execute their specified observations. | **Partial: F1/F2, T079/T080** |
| SC-001 | Integration and independent public Alpha/Beta clean installs/write/read and Alpha-only additive upgrade; actual rows, models and isolated histories inspected. | Supported |
| SC-002 | Twenty rounds of ten same/distinct-ID callers for independent connections and processes on each provider; persisted ownership/object inventories and direct/forced constraint checks. | Supported; public recipe gap F2 remains |
| SC-003 | Three real process restarts, metadata-change retry, no-storage inventory and missing-ready refusal are asserted by registry integration tests. | Supported; public recipe gap F1 remains |
| SC-004 | Before/after durable commit kills, SQLite pre-marker/native callback cases, lost advisory session, real total deadlines, forward/abandoned-lock recovery and explicit preserved blocks. | Supported |
| SC-005 | Reserved/path/alias/foreign-owner and physical-preflight negatives assert preservation; seven-outcome privacy captures assert zero canary/private-output disclosures. | Supported |
| SC-006 | Existing complete local/CI evidence and executable install/upgrade/restore are present; named retry/collision examples remain incomplete despite positive corpus totals. | **Partial: F1/F2, T079/T080** |

| Acceptance scenario | Current evidence |
| --- | --- |
| US1.1 | Registration inventory, distinct platform/Alpha/Beta objects; module migration and public clean-install assertions. |
| US1.2 | Metadata retry and `RestartTests` check stored identity/data through three processes; F1 remains for the separate public example. |
| US1.3 | Registration and public no-storage results; integration verifies zero module objects. |
| US1.4 | Conflicting need and immutable constraint tests; foreign/stale binding and provider/configuration rejection. |
| US2.1 | Same-ID independent-connection/process races, Busy-only retry and final single-allocation inventories. |
| US2.2 | Distinct-ID race rounds and final distinct object/owner counts. |
| US2.3 | Integration forced distinct-ID/reserved/unowned candidates preserve owners; public occupied-target case exists, but F2 remains for its distinct-ID example. |
| US2.4 | Direct duplicate ID/key/token and invalid-state writes fail against actual constraints; inconsistent reopen is rejected. |
| US3.1 | Identically named `Records`/history in independently owned contexts on both providers; clean-install rows/history inspected. |
| US3.2 | Alpha additive upgrade retains its row, Beta assignment/model/history unchanged; contender cannot acquire maintenance. |
| US3.3 | Whole-pending-set preflight and factory/binding negatives; actual-open path/sidecar/ancestor/link/hard-link negatives preserve the other store. |
| US3.4 | Missing/corrupt/mismatched ready-store resolution/open fails with no replacement creation or provider fallback. |
| US4.1 | Provisioning and migration process-death matrices inspect persisted states and retry only verified original evidence. |
| US4.2 | Real outage/privilege/live-lease/cancellation/session-loss tests; native deadlines retain scope until worker termination/reap. |
| US4.3 | Real bootstrap upgrade, before/after commit kill, consistent retry and future/populated/corrupt version refusal. |
| US4.4 | Actual provider backup/dump/fresh restore; complete/mixed/stale/member/checkpoint/legacy refusal and retained sentinels. |
| US5.1 | Captured seven-outcome structured records, results, spans/events, metrics and console/worker channels with fixed safe guidance. |
| US5.2 | Real provider/authority correction keeps original reservation, unrelated owner rows/history and distinct retry correlation. |

## Plan decisions and original task assessment

| Decision | Implementation / verification |
| --- | --- |
| D01 Minimal versioned public contracts, trusted composition and one adapter | Pure platform package; infrastructure reference boundary; owner binding and independent public consumers, no loader or new public endpoint. |
| D02 Reviewed supported EF/provider dependency closure and test ownership | Exact centrally pinned locks/inventory/notices; dedicated test projects with normal SDK inclusion and no production-linked tests. |
| D03 Immutable allocator-v1 keys, safe actual bytes and reserved targets | `Allocation/StorageAllocator.cs`; unit boundaries, forced candidate/direct constraint tests and stored-key retry. |
| D04 Versioned platform-only registry, real constraints and metadata consistency | Both embedded SQL scripts, `RegistryMigrationRunner`, immutable records and actual upgrade/constraint/corrupt-reopen tests. |
| D05 Reused exclusive installation lease and lost-session behavior | `MaintenanceScope`, shared `StoreLease`, host delegation and verified PostgreSQL advisory probe; contention/loss tests. |
| D06 Separate offline PostgreSQL provisioning, restricted runtime | `PostgresIdentity`/`PostgresStorageProvisioner`; actual role/schema/grant and denied/corrected authority checks. |
| D07 Canonical private SQLite file ownership and explicit provider posture | `ModuleStorePaths`/`SqliteStorageProvisioner`/`UnixStorePaths`; physical alias/sidecar/integrity negatives and no implicit creation. |
| D08 Ownership markers plus explicit reservation/physical/ready phases | `StorageOwnerMarker`/`StorageRegistry`; actual killed boundaries and verified retry/preserved pre-marker blocks. |
| D09 Assigned EF models/cache/history/migrations and whole-set preflight | `Migrations/` binders/interceptors/validator/verifier; same-context/different-assignment/provider cache and preflight-before-any-write tests. |
| D10 Durable migration intent/checkpoint and deliberate abandoned-lock recovery | `ModuleMigrationRunner`/`ModuleMigrationRecovery`; real history-bearing transaction kills and unknown lock/history/physical refusal. |
| D11 Shared finite budgets and owned synchronous-worker supervision | `OperationBudget`/`StorageMaintenanceProcess`, host maintenance worker/supervisor; real 3/2/30/120-second and observer/cancellation cleanup assertions. |
| D12 Explicit bootstrap upgrade, unchanged serving and legacy guards | Host storage maintenance dispatch/configuration and bootstrap guards; v0/v1 startup, unknown syntax, source/destination and leftover-module refusal. |
| D13 Stopped coherent recovery sets and fresh owned restoration | `Recovery/`, test-owned SQLite backup and PostgreSQL full dump/restore; actual inventory/hash/marker/checkpoint/sentinel verification. |
| D14 Safe observable outcomes and complete executable public examples in existing gates | Diagnostics/privacy, verifier inventory/selectors and nine-case harness exist; **public retry/collision observations are partial (F1/F2)**. |

Every original task was assessed, including checked tasks. These ranges account
for all **T001-T078** without treating their markers as proof:

| Tasks | Assessed evidence / remaining work |
| --- | --- |
| T001-T006 | Explicit ADR approval, reviewed dependency records, actual project graph, provider/scenario selectors and reusable owned fixture/process resources. |
| T007-T014 | Versioned disjoint contracts, budgets/scopes, both real registry schemas/constraints/upgrade and maintenance-only configuration. |
| T015-T027 | Allocation/registration/restart/no-storage/binding/bootstrap source and tests; **T017's prescribed public recipe needs T079**. |
| T028-T035 | Complete connection/process race and direct/forced constraint assertions; **T029/T035's public distinct-ID collision needs T080**. |
| T036-T047 | Assigned EF infrastructure, independent integration/public Alpha/Beta models/migrations, physical isolation and negative preflight/open checks. |
| T048-T063 | Real process-death/deadline/recovery tests, owned supervision, v0 upgrade, legacy guards, complete provider backups/fresh restores and executable recovery examples. `StorageGuideTests`/`RecoveryContractTests` provide the guide coverage instead of a duplicate planned `RecoveryGuideTests` file. |
| T064-T070 | Actual diagnostic/telemetry/privacy/corrected-retry coverage; **T068's complete public corpus needs both T079 and T080**, not merely positive per-case counts. |
| T071-T078 | Existing named local/CI gates, fail-closed verifier regressions, dependency/notices/docs and historical implementation evidence/publication are present. Their green results cannot supply omitted public-example assertions. |

Moving shared registry execution into `RegistryMigrationRunner` rather than
duplicating planned provider-store files is a justified consolidation, not
missing behavior. Test-only recovery exercises and module contexts remain
under their dedicated projects. Narrow supported DDL is explicitly documented;
the plan permits rejecting unrecognized operations rather than implementing
every EF operation. No later lifecycle, module, policy, MCP, job or backup
product has been smuggled into this slice.

## Constitution assessment

| Principle | Current evidence / applicability |
| --- | --- |
| I Product/brand | Köfte prose, module-free host, portable optional-module foundation; no Hoard dependency. |
| II Modularity | Canonical 002 identities and minimal storage contracts/bindings, not a runtime loader or hostile-code sandbox. |
| III Vertical slices/test ownership/licensing | Native DI, pure contracts/one justified adapter, physically test-owned contexts and reviewed exact dependencies; no MediatR/AutoMapper. |
| IV Storage | Real immutable UNIQUE/concurrency enforcement, safe schema keys, physical files/paths and module-local model/history plus real-provider tests. |
| V Transactions | Separate registry/physical commits and explicit recovery; no cross-module access contract or global ACID claim. |
| VI Safety | Separate provisioning authority, preserved runtime restrictions, owner/path/physical negatives and safe captured diagnostics; future actor/team/resource policy is not bypassed. |
| VII MCP | No new tool/server/discovery surface; future shared authorization obligations remain later work. |
| VIII Durable work | Durable storage intent/reconciliation only; no job/event/audit/realtime runtime or exactly-once effects claim. |
| IX Frontend | No new UI journey; existing host/web/browser/axe regressions remain wired and have actual CI evidence. |
| X Portability/operations | Explicit local SQLite/owned PostgreSQL, finite maintenance, versioned upgrades and complete stopped recovery with documented responsibilities. |
| XI Evidence | Real provider/constraint/race/restart/crash/deadline/privacy assertions and actual local/CI results exist. F1/F2 preserve the remaining public-example evidence obligations instead of waiving them. |
| XII Scope/governance | Approved ADR, fixed 003 intent, append-only tasks and one convergence stage on the original draft PR; no later feature or self-acceptance. |

## Verification and provenance

Fresh focused checks select the existing task-local .NET SDK 10.0.402 and
Node 24.22.0/npm 11.19.1, with inherited runtime/provisioning connections unset.
PostgreSQL uses the existing fixture-owned disposable target, not an arbitrary
developer database. No package/tool version, restore lock or product source
is changed by convergence.

```bash
export DOTNET_ROOT="$PWD/artifacts/toolchain/dotnet-10.0.402"
export PATH="$DOTNET_ROOT:$(dirname "$(fnm exec --using=24.22.0 node -p 'process.execPath')"):$PATH"
unset KOFTE_POSTGRES_CONNECTION KOFTE_POSTGRES_PROVISIONING_CONNECTION
```

| Check | Observed result |
| --- | --- |
| `python3 scripts/verify.py --suite unit` | Passed, eleven named steps, including Python tests. |
| `python3 scripts/verify.py --suite integration --provider both --disposable-postgres` | Passed, seven SQLite and nine PostgreSQL named steps, including actual restart, forced collision, race, crash and deadline assertions. |
| `python3 scripts/verify.py --suite contract --provider both --disposable-postgres` | Passed, 37 named steps, including independent public consumers and existing host/web contracts; F1/F2 remain despite this result. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed, 111 tests. |
| `git diff --check` | Passed. |

All four fresh safe reports identify assessed source `85e566b`, passing
outcomes/steps and `completeGate: false`: **64 targeted steps**, not a new
complete gate. UTC interval: 2026-10-22 16:19:38.518758 through
16:31:04.361302. Per-report elapsed seconds: unit 9.404, SQLite integration
218.000, PostgreSQL integration 221.716 and contract 236.532. Actual environment
is Darwin 25.5.0 arm64, 18 logical CPUs, 68,719,476,736 bytes physical memory
and Python 3.15.0, using existing local tools/fixture infrastructure. These are
observed machine conditions, not a fixed reference-machine benchmark.

Independently queried actual final-implementation-head CI:
[product run 39810548678](https://github.com/Stonefly-Labs/Kofte/actions/runs/39810548678)
and [repository run 39810548621](https://github.com/Stonefly-Labs/Kofte/actions/runs/39810548621)
pass all eleven contexts at `85e566b`. Downloaded all ten safe product artifacts
and checked every step: **200 passed**, with suite counts quality 19, unit 11,
SQLite integration 7, PostgreSQL integration 9, recovery 38, contract 37,
browser 16, privacy 36, startup 9 and failure-propagation 18.
Every report records merge `2bbe2e5794e17ddc30a3336568416af0e5a339a3`;
the GitHub commit API confirms parents main
`66f0e4a950bc3590cdb3a25f1622afbd13a0c226` and assessed feature head
`85e566b055451d6ef0342011f598e4a721484e40`.
Each CI report is scoped (`completeGate: false`); do not mislabel it a complete
local gate. The prior complete local result remains historical implementation
evidence; convergence does not claim a new local all-suite run.

## Append-only outcome and limits

Appended exactly **T079 and T080** under **Phase 8: Convergence**.
The original 59,760 task bytes, all IDs/order/markers and all earlier phases
are preserved. Their pre-append SHA-256 is
`0d87be6dc62362fcd52134cbcb3d327fd4d2510fb1bb3c002d43dca42f624d4c`.
The original implementation report and both reviewed checklists remain
unchanged; their historical implement evidence hashes matched at entry.
`spec.md` and `plan.md` remain unchanged, with SHA-256 respectively
`39db5abdb6d52662dc30f3b82918d4c1d5e945401e46666a3ca9a7a3ee0defef`
and `4bd71c5b4d243ac46ff0296f39b37446f135e4009ad57ce4f2dd613ec3cb3c46`.
No application or test code, dependencies, approved contract or ADR changed.

Supported limits remain narrow assigned int/text DDL, trusted in-process code,
preserved ambiguous reconciliation blocks and stopped complete-set fresh-target
recovery. UUID/hash consistency is not malicious-forgery protection or proof
that a newer lost backup does not exist. No universal kernel-blocked-disk
deadline, cross-module atomicity, automatic backup product, production readiness
or MVP completion is claimed.

Convergence outcome is **gaps**; the driver routes the **next invocation**
to implement T079-T080. After actual remediation, run convergence again before
final review and explicit numbered-feature human acceptance. Keep PR #6 draft;
no merge or acceptance is requested.
