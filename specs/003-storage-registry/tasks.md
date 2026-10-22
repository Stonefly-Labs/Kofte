# Tasks: 003 - Storage Registry and Module Isolation

**Input**: Design documents from `specs/003-storage-registry/`: spec, plan,
research, data model, storage v1 and migration/recovery contracts, future guide
and reviewer-authorized requirements-quality checklist.
**Prerequisites**: Accepted/merged 001 and 002; reviewed 16/16 author and 36/36
quality criteria. Passing analyze and explicit human approval of Proposed
ADR 0005 are required **before implementation**, not inferred from this list.
**Constitution**: Adopted v2.0.1, principles I-XII.
**Status**: All 80 tasks are evidenced, including the original six named gates, both
published public `all` commands, the complete local 200-step gate and eleven
passing assessed-head CI contexts. T079-T080 now close the first convergence's
public restart/collision gaps with standalone and `all` observations on both
providers; fresh remediation checks are in `implementation.md`.
Implement is complete; follow-up converge is next,
not performed here. This is not feature acceptance.

**Tests**: The specification and constitution explicitly require these tests.
Use actual PostgreSQL 17 and SQLite, not substitutes. Write story cases before
dependent implementation; after necessary API/fixture scaffolding compiles,
prove the behavioral regression where applicable, not merely a compilation
failure. Final story checks require passing results, not test-file creation.
**Organization**: Setup, shared foundations, five story increments and final
cross-cutting evidence. Stories are independently verifiable, but their
prerequisites are not falsely independent.

## Format and Paths

- Task lines use an unchecked checkbox, sequential ID, optional `[P]`, story
  label only within a story, exact repository paths and requirement/principle IDs.
- Every task declares `Depends:`; `none` means no task prerequisite. The separate
  analyze-stage entry gate still applies to every implementation task.
- `[P]` means different-file work can run after its stated prerequisites, not
  permission to run a later Spec Kit stage or skip a gate.
- All .NET tests, sample contexts, migrations and fixture helpers physically
  belong to dedicated projects under `tests/`, using normal SDK inclusion.
  No linked production-slice test source or product feature module is allowed.
- New paths below are planned implementation targets, not files created during
  tasks generation. Reuse existing host safety/fixture/verification mechanisms.
- `storage-v1.md` and `migrations-and-recovery.md` below mean the files under
  `specs/003-storage-registry/contracts/`. Data constraints quoted in tasks come
  from `data-model.md`; they are binding inputs, not implementation discretion.

## Phase 1: Setup and Shared Prerequisites

- [X] T001 Obtain explicit human approval or requested revisions for `docs/adr/0005-module-storage-ownership.md` and record the exact decision in `specs/003-storage-registry/implementation.md`; block implementation while Proposed, and route material design changes through human review/re-analysis rather than silently editing approved intent. Depends: none. Trace: FR-001-016; principles III, IV, X, XII/governance.
- [X] T002 Review the candidate EF Core/Relational/Sqlite 10.0.13 and Npgsql EF 10.0.3 dependency closure before adoption in `docs/development/dependencies.md` and `docs/development/third-party-notices.md`: verify direct/transitive licenses, provenance/advisories, supported-target compatibility, native SQLite assets and exact versions; add EF Design only if actually needed and development-only. Depends: T001. Trace: FR-009/010/013; principles III, VI, X.
- [X] T003 Create only the justified adapter/test projects and references in `src/Kofte.Infrastructure/Kofte.Infrastructure.csproj`, `tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj`, `tests/contract/Kofte.Storage.Consumer/Kofte.Storage.Consumer.csproj`, `tests/integration/Kofte.TestSupport/Kofte.TestSupport.csproj` and `Kofte.slnx`; update `Directory.Packages.props`, affected project references and `packages.lock.json` files with reviewed exact packages, retaining pure `Kofte.Platform`, normal test inclusion, nullable/warnings-as-errors and locked restore. Depends: T002. Trace: FR-001/009/013/016; principles II, III, X.
- [X] T004 [P] Set up actual storage-test discovery and provider selectors in `tests/integration/Kofte.Storage.IntegrationTests/StorageTestEnvironment.cs` and `tests/integration/Kofte.Storage.IntegrationTests/Usings.cs`; use the existing xUnit runner conventions, explicit unavailable-provider failure and test-owned cancellation/resource scopes, not skip-shaped success. Depends: T003. Trace: FR-013/016, SC-001/004; principles III, XI.
- [X] T005 [P] Set up the independent public-process harness in `tests/contract/Kofte.Platform.ContractTests/Modules/Storage/StorageConsumerHarness.cs` and selector/result handling in `tests/contract/Kofte.Storage.Consumer/Program.cs`; accept only named inert scenarios and provider selectors, fail on unknown/missing scenario, missing provider, zero assertions or bad exit/result, and keep credentials/targets out of arguments and output. Depends: T003. Trace: FR-001/014/016, SC-006; principles VI, XI.
- [X] T006 Extract reusable owned provider/process resources into `tests/integration/Kofte.TestSupport/PostgresResources.cs` and `tests/integration/Kofte.TestSupport/OwnedProcesses.cs`, delegating from `tests/integration/Kofte.Host.IntegrationTests/PostgresFixture.cs` and its existing partials without copying cleanup logic; extend fixtures with separate offline provisioner and fresh recovery targets while retaining loopback/image/database-OID/role-OID/comment checks, original-resource preservation and targeted cleanup. Depends: T004, T005. Trace: FR-013/015/016; principles III, VI, X, XI.

## Phase 2: Foundational Boundaries

- [X] T007 Define version-1 `IStorageRegistry`, `IModuleStorage`, `IStorageMaintenance`, immutable `StorageAssignment` and disjoint `StorageResult` in `src/Kofte.Platform/Modules/Storage/`; reuse validated 002 `ModuleDescription`/`ModuleId`, separate trusted registry/maintenance from owner-only module access, expose no physical/credential override, and implement all seven outcomes, stable codes/guidance and durable-state uncertainty from `storage-v1.md` (only ready carries a usable assignment). Depends: T003. Trace: FR-001/002/007-009/011/014/016; principles II, IV-VII.
- [X] T008 [P] Add focused budget/cancellation and installation-scope regression tests in `tests/unit/Kofte.Host.Tests/Storage/OperationBudgetTests.cs` and `tests/unit/Kofte.Host.Tests/Storage/MaintenanceScopeTests.cs`; cover monotonic remaining-budget clamping, cancellation before/after possible commit, 3/2/30/120-second limits, at-most-50-ms lock probes, no zero/infinite timeout and no early scope release or silent lock-session reacquisition. Depends: T007. Trace: FR-010/011/013, SC-004; principles V, VI, X, XI.
- [X] T009 Implement common operation budgets and exclusive maintenance scope in `src/Kofte.Infrastructure/Storage/OperationBudget.cs` and `src/Kofte.Infrastructure/Storage/MaintenanceScope.cs`; move/reuse `src/Kofte.Host/Bootstrap/UnixStorePaths.cs` and `src/Kofte.Host/Bootstrap/StoreLease.cs` safety mechanisms through host delegation, retain SQLite `.platform.lock` and PostgreSQL advisory key `0x6b6f667465000001`, shared-serving versus exclusive-maintenance behavior, non-pooled session ownership and cancellation on lease loss. Depends: T008. Trace: FR-006/010-013/016; principles III-V, X.
- [X] T010 Implement platform-local registry records, validation and versioned SQL-runner coordination in `src/Kofte.Infrastructure/Storage/Registry/RegistryRecords.cs` and `src/Kofte.Infrastructure/Storage/Registry/RegistryMigrationRunner.cs`; keep contract/registry/allocator/marker/recovery/module versions distinct. Enforce metadata constraints `singleton_key`: "Constant 1, primary key and CHECK"; `installation_id`: "UUID, immutable random installation identity"; `provider`: "`postgresql` or `sqlite`, fixed for this installation"; `registry_version`: "Integer 1; reject unknown/newer values before writes"; `registry_revision`: "Nonnegative 64-bit counter, increment with registry mutations"; `created_at_utc`: "UTC timestamp, not ownership authority"; use canonical binary/ordinal comparisons and no payload/absolute-path/credential ownership fields. Depends: T007, T009. Trace: FR-002-005/010/012/013; principles IV, V, X.
- [X] T011 [P] Add PostgreSQL registry format-1 SQL and transactional history execution in `src/Kofte.Infrastructure/Storage/Registry/Migrations/Postgres/001_registry.sql` and `src/Kofte.Infrastructure/Storage/Postgres/PostgresRegistryStore.cs`, only in `kofte_system`; enforce `"Primary/UNIQUE canonical 002 ID, 5-253 ASCII characters"`, `"Immutable boolean storage need"`, `"Installation provider; never caller-selected per registration"`, `"Null for no-storage, otherwise immutable canonical allocator output"`, `"Null for no-storage, otherwise 1 at new allocation"` and `"Null for no-storage, otherwise immutable random UUID, UNIQUE"`, including actual UNIQUE ID/key/token constraints, key grammar/byte/reserved validation and deterministic collation. Depends: T010. Trace: FR-002-005/010; principles IV, V, XI.
- [X] T012 Add equivalent SQLite registry SQL and execution in `src/Kofte.Infrastructure/Storage/Registry/Migrations/Sqlite/001_registry.sql` and `src/Kofte.Infrastructure/Storage/Sqlite/SqliteRegistryStore.cs`, only in `platform/platform.db`, with `COLLATE BINARY`, `BEGIN IMMEDIATE` and metadata/`user_version=1` agreement; enforce the same ID/need/provider/key/version/token constraints as T011 plus `state`: "`no_storage`, `reserved`, `ready`, `reconciliation_required`"; `migration_state`: "`none`, `in_progress`, `current`, `recovery_required`"; `migration_target`: "Null or bounded module migration ID, at most 150 ASCII characters"; `migration_checkpoint`: "Null or last verified migration ID, same bound"; `last_outcome_code`: "Null or allowlisted safe diagnostic code, never exception/input"; `revision`: "Positive 64-bit optimistic state-transition revision". Apply these state/revision constraints to PostgreSQL in its adapter as well; no-storage allocation fields/targets/checkpoints stay null, state `no_storage`, migration state `none`. Depends: T011. Trace: FR-002-004/007/010-012; principles IV, V, XI.
- [X] T013 Add real both-provider schema/upgrade tests in `tests/integration/Kofte.Storage.IntegrationTests/Registry/RegistryFormatTests.cs` and finish transactional consistency checks in `src/Kofte.Infrastructure/Storage/Registry/RegistryMigrationRunner.cs`: accept only owned table-free v0 or fully consistent v1, persist script SHA-256/version/timestamp in platform-local `__kofte_registry_migrations`, reject unknown tables, missing metadata, bad history/version/provider/revisions and mismatched SQLite pragma, and prove atomic/idempotent initialization without `EnsureCreated` or fabricated EF history. Depends: T006, T012. Trace: FR-003/008/010/013, US4.3; principles IV, V, X, XI.
- [X] T014 Wire maintenance-only configuration parsing in `src/Kofte.Host/Features/StorageMaintenance/StorageMaintenanceConfiguration.cs` and serving-mode rejection in `src/Kofte.Host/Bootstrap/BootstrapConfiguration.cs`, with regressions in `tests/unit/Kofte.Host.Tests/Storage/StorageConfigurationTests.cs`; only owned maintenance children receive `KOFTE_POSTGRES_PROVISIONING_CONNECTION`, runtime must lack database CREATE/admin flags, both connections must identify the same owned database/expected roles, and missing provisioner must not break ordinary resolution or zero-module startup. Depends: T006, T009. Trace: FR-001/013/016, US4.2; principles VI, X.

**Checkpoint**: Reviewed locks and actual discovered test harnesses; versioned
contracts, common safety/lease/budget mechanics, real registry constraints and
explicit v0/v1 initialization are available. T008/T013/T014 pass on their
applicable targets. No module loader, public registry endpoint or automated
whole-installation backup product has been added.

## Phase 3: User Story 1 - Give a module stable, exclusive storage (Priority: P1)

**Goal**: Persist immutable exclusive assignments or explicit no-storage and keep
data across restart and harmless module metadata changes.
**Independent Test**: On each real provider, register Alpha/Beta and no-storage,
write distinct sentinels, restart three times and resolve the exact stored
allocations. Reject declaration/provider/owner/location changes without data loss.

### Required Validation

- [X] T015 [P] [US1] Add allocator boundary/regression cases in `tests/unit/Kofte.Host.Tests/Storage/StorageAllocatorTests.cs`: canonical 002 minimum/maximum and near-identical IDs, ignored Unicode display names, dot/hyphen collapse/trimming, 24-character slug/32-hex SHA-256 suffix, 59-byte generated cap, actual 63-byte validation, SQL/uppercase/non-ASCII/reserved/platform-name rejection and changed allocator behavior never renaming persisted keys. Depends: T014, T013. Trace: FR-001/004/005, US1.2/4; principles IV, XI.
- [X] T016 [P] [US1] Add actual register/resolve/no-storage/immutable-retry tests in `tests/integration/Kofte.Storage.IntegrationTests/Registry/RegistrationTests.cs` and `tests/integration/Kofte.Storage.IntegrationTests/Registry/RestartTests.cs`; assert separate platform/Alpha/Beta objects, zero no-storage objects, exact assignment/token/data retention through three full process restarts and display/release changes, no-storage-to-relational conflict and explicit owner/provider/location mutation rejection. Depends: T014, T013. Trace: FR-002/004/007/008/013, SC-001/003, US1.1-4; principles IV, V, XI.
- [X] T017 [P] [US1] Add independent public-contract registration cases in `tests/contract/Kofte.Platform.ContractTests/Modules/Storage/RegistrationContractTests.cs` and the `no-storage`/`same-id-retry` scenarios in `tests/contract/Kofte.Storage.Consumer/Scenarios/RegistrationScenarios.cs`; consume only public contracts, validate explicit no-allocation and immutable owner/provider metadata with zero credentials/private paths, and fail rather than fake success until implementation exists. Depends: T005, T013, T014. Trace: FR-001/002/004/016, US1.1-4, SC-003/006; principles II, VI, XI.

### Implementation

- [X] T018 [US1] Implement allocator v1 and actual-key validation in `src/Kofte.Infrastructure/Storage/Allocation/StorageAllocator.cs`: canonical-ID-only `m_<slug>_<hash32>` rules exactly as `storage-v1.md`, reserved exact/prefix/platform checks, quoted identifiers and parameterized values; internal forced-candidate test seam only, no production physical override/suffix search. Depends: T015. Trace: FR-003-005, US1.1/2/4; principles IV, VI.
- [X] T019 [US1] Implement reserve/read/no-storage and compatible retry in `src/Kofte.Infrastructure/Storage/Registry/StorageRegistry.cs`; use stored keys/version/tokens, immutable need/provider/identity and optimistic transitions, require one caller-held installation scope, commit reservation before physical work and return truthful incomplete states without a global transaction/fallback. Depends: T016, T017, T018. Trace: FR-001-004/007/008/011/012, US1.1-4; principles II, IV-VI.
- [X] T020 [US1] Implement PostgreSQL provision/verify in `src/Kofte.Infrastructure/Storage/Postgres/PostgresStorageProvisioner.cs`: separate offline credential, verified database/role posture, schema plus owner marker in one transaction, `AUTHORIZATION` runtime owner, PUBLIC revoked and `SET LOCAL ROLE` for runtime-owned objects; no `IF NOT EXISTS` adoption or runtime database CREATE. Depends: T019. Trace: FR-005/007/008/013, US1.1/2; principles IV-VI, X.
- [X] T021 [P] [US1] Implement SQLite exclusive physical creation and verification in `src/Kofte.Infrastructure/Storage/Sqlite/SqliteStorageProvisioner.cs` and `src/Kofte.Infrastructure/Storage/Sqlite/ModuleStorePaths.cs`: only `modules/<allocation_key>/module.db`, private directories 0700/files 0600, verified ancestors/regular one-link files/sidecars, `DELETE`/`FULL`, foreign keys on, no pooling/shared cache/`ATTACH`, and never create-if-missing for ready access. Depends: T019. Trace: FR-006-008/013, US1.1/2; principles IV-VI, X.
- [X] T022 [US1] Implement shared physical-marker validation in `src/Kofte.Infrastructure/Storage/Registry/StorageOwnerMarker.cs` and connect both provisioners: `__kofte_storage_owner` singleton obeys `"Integer 1"`, `"Matches platform metadata"`, `"Matches registry canonical owner"`, `"Matches the exact immutable reservation"`, `"Matches registry/provider adapter"` and `"Matches persisted physical key"` for marker version/installation/module/allocation/provider/key; marker without registry authority never permits adoption. Depends: T020, T021. Trace: FR-004/007/008, US1.1/2; principles IV, V.
- [X] T023 [US1] Implement ready-only resolution in `src/Kofte.Infrastructure/Storage/Registry/StorageRegistry.cs`; verify provider/version/registry consistency, actual marker/schema ownership or SQLite header/integrity/paths and migration state, with existing-file mode and no regeneration, creation, repair or other-store/provider fallback; only maintenance may complete durable ready state. Depends: T022. Trace: FR-004/007/008/013, US1.1-4; principles IV-VI.
- [X] T024 [US1] Issue owner-only accessor bindings in `src/Kofte.Infrastructure/Storage/ModuleStorageBinding.cs` using `src/Kofte.Platform/Modules/Storage/` types; copied assignment metadata cannot mint access, actual use revalidates installation/owner/token/revision/target and disposed/expired state, and ordinary module DI receives no registry/maintenance service or arbitrary-ID resolver. Depends: T023. Trace: FR-001/008/009, US1.1/4; principles II, IV, VI.
- [X] T025 [US1] Add minimal host composition/reference wiring in `src/Kofte.Host/Program.cs` and `src/Kofte.Host/Features/StorageMaintenance/StorageServices.cs`, with zero-module v0/v1 regressions in `tests/integration/Kofte.Host.IntegrationTests/StorageCompatibilityTests.cs`; retain existing health/OpenAPI/web behavior, no automatic init/upgrade, no new endpoint or optional module loading, and safe projection of incompatible/missing registry state. Depends: T024. Trace: FR-008/010/013/016, US1.1-3, SC-006; principles I, II, IX, X.
- [X] T026 [US1] Execute T015-T017 and `StorageCompatibilityTests` on their real providers via the existing test runner; record exact selectors/results, final object/row inventory, three restart observations and immutable/no-storage assertions in `specs/003-storage-registry/implementation.md`; a failure or zero tests keeps this story incomplete. Depends: T025. Trace: FR-001-008/013/016, SC-001/003, US1.1-4; principles IV, XI.
- [X] T027 [US1] Document executable registration/no-storage/retry examples and binding limits in `specs/003-storage-registry/quickstart.md` and `specs/003-storage-registry/contracts/storage-v1.md`, distinguishing supported trusted access from hostile-code isolation and noting that full EF clean-install proof follows US3. Depends: T026. Trace: FR-001/004/009/016, US1.1-4; principles II, VI, X, XII.

**Checkpoint**: US1.1-4 and SC-003 have actual both-provider evidence.
This is the first useful internal increment, not complete 003 or the MVP:
concurrency hardening, complete EF/migration, recovery and privacy proof remain.

## Phase 4: User Story 2 - Register safely despite races and collisions (Priority: P1)

**Goal**: Independent processes cannot duplicate/steal an assignment, and busy is
distinguishable from conflicting/unowned storage.
**Independent Test**: Run 20 repetitions of ten concurrent same-ID attempts and
20 of ten distinct-ID attempts on each provider, with in-process independent
connections and separate processes. Inspect final rows and physical ownership;
bounded busy retries must converge. Force collisions and bypass allocator checks.

### Required Validation

- [X] T028 [P] [US2] Add deterministic start-barrier races and process-worker cases in `tests/integration/Kofte.Storage.IntegrationTests/Registry/RegistrationConcurrencyTests.cs` and `tests/integration/Kofte.Storage.IntegrationTests/Registry/RegistrationWorker.cs`; execute the full 20-by-ten same/distinct-ID matrix for independent connections and processes, bounded retry after busy, final exactly-one same-ID allocation and zero shared distinct-ID owners/objects, not response-code-only assertions. Depends: T026. Trace: FR-003/004/011, US2.1/2, SC-002; principles IV, V, XI.
- [X] T029 [P] [US2] Add forced candidate/reserved/unowned target tests in `tests/integration/Kofte.Storage.IntegrationTests/Registry/AllocationCollisionTests.cs` and the public `collision` case in `tests/contract/Kofte.Storage.Consumer/Scenarios/CollisionScenario.cs`; force distinct IDs to one key through test-only infrastructure control and prove untouched existing owner/sentinel/permissions plus no suffix/adoption/deletion on either provider. Depends: T026. Trace: FR-003/005-007, US2.2/3, SC-002/005; principles IV, VI, XI.
- [X] T030 [P] [US2] Add direct duplicate-ID/key/token persistence attempts, invalid state/null/key/revision records and corrupt-reopen tests in `tests/integration/Kofte.Storage.IntegrationTests/Registry/RegistryConstraintTests.cs`; bypass allocator and application checks to prove actual database UNIQUE/CHECK constraints and explicit refusal to serve inconsistent persisted mappings on both providers. Depends: T026. Trace: FR-002-005/008/010, US2.4, SC-002/005; principles IV, XI.

### Implementation

- [X] T031 [US2] Harden transactional conflict handling in `src/Kofte.Infrastructure/Storage/Registry/StorageRegistry.cs` and `src/Kofte.Infrastructure/Storage/Postgres/PostgresRegistryStore.cs`: classify known constraint identities/SQLSTATE without raw exception text, reread same-ID equivalent outcomes, reject conflicting need/key/owner, retain exclusive session lease across all commits and fail explicitly on lost lease. Depends: T028, T029, T030. Trace: FR-002-005/007/011, US2.1-4; principles IV-VI.
- [X] T032 [P] [US2] Harden SQLite independent-writer handling in `src/Kofte.Infrastructure/Storage/Sqlite/SqliteRegistryStore.cs` and `src/Kofte.Infrastructure/Storage/Sqlite/SqliteStorageProvisioner.cs`: local `BEGIN IMMEDIATE` reservation, known busy/constraint classification within the same remaining budget, exclusive-create conflict preservation, and at most one physical allocation despite uncertain responses. Depends: T028, T029, T030. Trace: FR-002-004/006/007/011, US2.1-4; principles IV-VI.
- [X] T033 [US2] Make durable reopen checks and actual-target collision checks exhaustive in `src/Kofte.Infrastructure/Storage/Registry/RegistryRecords.cs` and `src/Kofte.Infrastructure/Storage/Allocation/StorageAllocator.cs`; reject malformed/noncanonical/reserved or metadata-inconsistent stored keys before use, but never require a valid old stored key to equal today's allocator output. Depends: T031, T032. Trace: FR-003-005/007/008, US2.3/4; principles IV, VI.
- [X] T034 [US2] Execute the complete T028-T030 matrix on both real providers, retaining repeat/process/connection counts, each final registry/object inventory, outside-owner sentinel checks and measured busy/retry bounds in `specs/003-storage-registry/implementation.md`; do not replace forced collisions/direct writes with hash-probability arguments. Depends: T033. Trace: FR-003-008/011, US2.1-4, SC-002/005; principles IV, XI.
- [X] T035 [US2] Publish truthful busy/conflict/collision retry examples in `specs/003-storage-registry/quickstart.md` and `specs/003-storage-registry/contracts/storage-v1.md`, keeping production physical overrides unavailable and preserving installed allocation identity through corrected retry. Depends: T034. Trace: FR-003/004/011/016, US2.1-4; principles IV, VI, X.

**Checkpoint**: US2.1-4 and SC-002 pass with real constraints and independent
process evidence; all original owners/data remain intact.

## Phase 5: User Story 3 - Use only the assigned module boundary (Priority: P1)

**Goal**: Normal module contexts, models, history and pending migrations stay
inside the host-issued owner assignment, including upgrades and rejected targets.
**Independent Test**: Two test-owned public module consumers use `Records` and
`__EFMigrationsHistory` on each provider. Clean-install and upgrade only Alpha;
verify Beta/platform are unchanged. Reject all foreign targets before writes.

### Required Validation

- [X] T036 [P] [US3] Add both-provider context/model/history/cache/migration isolation tests in `tests/integration/Kofte.Storage.IntegrationTests/Providers/ModuleMigrationTests.cs` and `tests/integration/Kofte.Storage.IntegrationTests/Providers/AssignedModelTests.cs`; require same logical tables/history, same context type against different assignments, provider/design-time cache separation, clean install and additive Alpha-only upgrade preserving all sentinels, plus exclusive migration contention. Depends: T026. Trace: FR-005/006/009/010/012, US3.1/2, SC-001; principles III-V, XI.
- [X] T037 [P] [US3] Add boundary/negative preflight tests in `tests/integration/Kofte.Storage.IntegrationTests/Providers/StorageBoundaryTests.cs` and `tests/contract/Kofte.Platform.ContractTests/Modules/Storage/BindingContractTests.cs`: foreign/forged/replayed/disposed binding, context factory owner switch, literal foreign schemas, reserved tables, unbound snapshots, raw SQL/unknown operations/suppressed transactions, missing/corrupt/unowned targets and traversal/absolute/link/sidecar/hard-link/non-regular aliases, with original target bytes/rows unchanged. Depends: T026. Trace: FR-006/008-010, US3.3/4, SC-005; principles IV, VI, XI.

### Implementation

- [X] T038 [US3] Implement assigned context/options and final-model verification in `src/Kofte.Infrastructure/Storage/Migrations/AssignedContextFactory.cs` and `src/Kofte.Infrastructure/Storage/Migrations/AssignedModelCacheKeyFactory.cs`; internally choose the verified connection, bind PostgreSQL table/view/sequence/FK principal schemas/default/history and provider/schema/context/design-time cache keys, use no SQLite schema simulation, and reserve owner/history/provider-lock objects against application mappings. Depends: T036, T037. Trace: FR-005/006/008/009, US3.1/3/4; principles II, IV, VI.
- [X] T039 [US3] Implement assignment-aware migration/snapshot bases and public `IMigrationsAssembly` binding in `src/Kofte.Infrastructure/Storage/Migrations/AssignedMigration.cs`, `src/Kofte.Infrastructure/Storage/Migrations/AssignedModelSnapshot.cs` and `src/Kofte.Infrastructure/Storage/Migrations/AssignedMigrationsAssembly.cs`; bind before operation/target/snapshot model evaluation, keep provider-specific sets, and use public EF services rather than internal history APIs or SQL text substitution. Depends: T038. Trace: FR-009/010, US3.1-3; principles III, IV.
- [X] T040 [US3] Preflight the entire pending set in `src/Kofte.Infrastructure/Storage/Migrations/MigrationBoundaryValidator.cs`: inspect target models and all schema/object-bearing/nested operations, FK principals, rename old/new targets, indexes/sequences/data/annotations; permit only recognized locally bound operations and reject foreign/unknown/raw `SqlOperation`/transaction suppression/unbound snapshot before any pending SQL, not after an earlier valid migration has written. Depends: T039. Trace: FR-005/009/010/012, US3.1-3, SC-005; principles IV-VI.
- [X] T041 [US3] Implement owner-bound migration execution in `src/Kofte.Infrastructure/Storage/Migrations/ModuleMigrationRunner.cs`; hold one installation scope, persist intent first, verify actual marker/history, run module-local migration, recheck SQLite DELETE/FULL/foreign-key/sidecar posture and record current checkpoint only after verification; retain recovery-needed on failure/uncertain commit with no enablement/general dependency runner. Depends: T040. Trace: FR-007/009-012, US3.1/2; principles IV, V, VIII, X.
- [X] T042 [P] [US3] Implement Alpha's provider-specific sample contexts, migrations and bound snapshots under `tests/integration/Kofte.Storage.IntegrationTests/Modules/Alpha/`; use `Records`, distinct sentinels, module-local history and an additive v2 column that preserves rows, no production test source or cross-owner relationship. Depends: T040. Trace: FR-009/010/012, US3.1/2, SC-001; principles III-V, XI.
- [X] T043 [P] [US3] Implement Beta's independent provider-specific contexts, migrations and bound snapshots under `tests/integration/Kofte.Storage.IntegrationTests/Modules/Beta/`; deliberately reuse Alpha's logical table/history names with distinguishable sentinels and v1 history, proving Beta stays unchanged by Alpha v2. Depends: T040. Trace: FR-009/010/012, US3.1/2, SC-001; principles III-V, XI.
- [X] T044 [US3] Finish actual-open revalidation and path/alias/missing-store rejection in `src/Kofte.Infrastructure/Storage/ModuleStorageBinding.cs` and `src/Kofte.Infrastructure/Storage/Sqlite/ModuleStorePaths.cs`, exercising T037's zero-write corpus; normal resolution/open is read-verify only, forged descriptors/revisions cannot authorize another store, no absent ready store is recreated and no sidecar/ancestor is silently repaired. Depends: T038, T037. Trace: FR-006/008/009, US3.3/4, SC-003/005; principles IV, VI.
- [X] T045 [US3] Add independent public module implementations under `tests/contract/Kofte.Storage.Consumer/Modules/Alpha/` and `tests/contract/Kofte.Storage.Consumer/Modules/Beta/`, plus `clean-install`, `isolated-upgrade` and `path-rejection` in `tests/contract/Kofte.Storage.Consumer/Scenarios/ModuleScenarios.cs`; the consumer and `tests/contract/Kofte.Platform.ContractTests/Modules/Storage/ModuleContractTests.cs` independently use public binding/EF APIs, assert actual rows/history/models and forbidden-target preservation, and do not import integration-test implementations or production internals to manufacture acceptance. Depends: T041, T042, T043, T044. Trace: FR-001/009/010/016, US3.1-4, SC-001/005/006; principles II-IV, VI, XI.
- [X] T046 [US3] Execute both-provider T036/T037 and public-consumer cases, inspect actual tables/history and generated pending operations, and retain cache separation, preflight-before-any-write, Alpha-only upgrade and forbidden-target assertions in `specs/003-storage-registry/implementation.md`. Depends: T045. Trace: FR-005/006/008-012/016, US3.1-4, SC-001/003/005; principles IV-VI, XI.
- [X] T047 [US3] Make assignment/model/migration author examples executable in `specs/003-storage-registry/quickstart.md` and `specs/003-storage-registry/contracts/migrations-and-recovery.md`; document supported operation limits, schema-bound snapshots/history, ordinary module-local transactions and no hostile-code sandbox/general lifecycle runner. Depends: T046. Trace: FR-009/010/012/016, US3.1-4; principles II-V, X.

**Checkpoint**: US3.1-4 and SC-001 pass on real providers through both public
consumers. A correct model default alone does not count as migration isolation.

## Phase 6: User Story 4 - Recover an interrupted installation without guessing (Priority: P1)

**Goal**: Every interrupted phase is truthful and preserves ownership; explicit
upgrade and stopped whole-set recovery work without registry-only data loss.
**Independent Test**: Kill owned workers around each durable boundary, restart
and inspect original assignment/data. Exercise bounded failures, v0 upgrade,
abandoned EF-lock recovery, actual full-set restore and unsafe legacy refusals.

### Required Validation

- [X] T048 [P] [US4] Add controlled real-process crash/lost-response cases in `tests/integration/Kofte.Storage.IntegrationTests/Recovery/ProvisioningCrashTests.cs` and `tests/integration/Kofte.Storage.IntegrationTests/Recovery/MigrationCrashTests.cs`; instrument internal test-only barriers immediately before/after reservation, physical object/marker, ready, migration/history and registry-upgrade commits, including SQLite directory/file-before-marker and PostgreSQL lost session. Inspect actual rows/files/history/sentinels after restart; no incomplete phase may resolve ready. Depends: T034, T046. Trace: FR-007/008/010-012, US4.1/3, SC-004; principles IV, V, VIII, XI.
- [X] T049 [P] [US4] Add elapsed-budget/cancellation/outage/privilege/live-host contention tests in `tests/integration/Kofte.Storage.IntegrationTests/Recovery/OperationDeadlineTests.cs`; include denied/missing/mismatched provisioner, busy database/lease, synchronous SQLite worker termination/reaping and PostgreSQL lock-session loss, with actual 3/2/30/120-second bounds including cleanup, original assignment/data and no fallback/false success. Depends: T034, T046. Trace: FR-010/011/013, US4.2, SC-004; principles V, VI, X, XI.
- [X] T050 [P] [US4] Add explicit bootstrap/registry upgrade failure and old-binary rollback compatibility cases in `tests/integration/Kofte.Storage.IntegrationTests/Recovery/RegistryUpgradeTests.cs`: owned v0-to-v1, consistent v1 retry, unsupported/corrupt version/history, unknown populated source, kill on both sides of commit and lost acknowledgement; verify preserved contents/ownership and supported forward/full-pre-change-set recovery. Depends: T034, T046. Trace: FR-008/010/015, US4.3, SC-004/006; principles IV, V, X, XI.
- [X] T051 [P] [US4] Add both-provider complete/mixed/stale recovery-set and legacy refusal cases in `tests/integration/Kofte.Storage.IntegrationTests/Recovery/RecoverySetTests.cs` and `tests/integration/Kofte.Host.IntegrationTests/StorageRecoveryCompatibilityTests.cs`; preserve originals, inspect source AND destination, reject missing platform plus leftover modules, extra/missing member/marker/history/hash/revision/unsupported formats/unsafe sidecars, and prove unchanged sentinels after refused init/backup/restore/confirmed reset. Depends: T034, T046. Trace: FR-006/007/010/015, US4.4, SC-004-006; principles IV-VI, X, XI.

### Implementation

- [X] T052 [US4] Implement owned maintenance-worker supervision in `src/Kofte.Host/Features/StorageMaintenance/MaintenanceWorker.cs` and `src/Kofte.Host/Features/StorageMaintenance/MaintenanceSupervisor.cs`; supervisor/worker retain the same installation scope until completion or targeted termination/reap, clamp synchronous SQLite busy/command limits to at most one second and remaining time, reserve cleanup inside total budgets and report incomplete/cancelled uncertainty without leaving a writer or reusing a lost PostgreSQL session. Depends: T048, T049, T014. Trace: FR-007/010-013, US4.1/2; principles V, VI, VIII, X.
- [X] T053 [US4] Implement explicit maintenance retry/reconciliation in `src/Kofte.Infrastructure/Storage/Registry/StorageReconciliation.cs` and connect `StorageRegistry.cs`: finish only the original reservation with verified absence/matching owner, preserve and block ambiguous SQLite pre-marker/unowned/missing/corrupt/mismatched evidence, compare actual migration history with durable intent, and never repair from ordinary resolve or infer rollback from a failed response. Depends: T048, T052. Trace: FR-004/007/008/010-012, US4.1/2; principles IV, V, VIII.
- [X] T054 [US4] Implement explicit assigned-store abandoned SQLite EF-lock recovery in `src/Kofte.Infrastructure/Storage/Migrations/ModuleMigrationRecovery.cs`; after exclusive lease, stopped owned workers, owner/integrity/history and incomplete-migration inspection, clear only that verified store's abandoned lock through a deliberate recovery action; demonstrate actual killed migration, verified unapplied retry or preserved forward/full-set block, never generic lock/journal/history deletion. Depends: T048, T053. Trace: FR-007/009-012/015, US4.1-3; principles IV-VI, X.
- [X] T055 [US4] Add `--registry upgrade` dispatch and sanitized maintenance-child execution in `src/Kofte.Host/Features/StorageMaintenance/RegistryCommand.cs` and `src/Kofte.Host/Program.cs`, connecting the existing explicit `--store init` semantics, v0/v1 runner and safe health categories; no automatic serving migration, raw provider errors, permissive unknown syntax or public registration CLI. Depends: T050, T052, T053. Trace: FR-010/013/014/016, US4.2/3, SC-006; principles VI, X.
- [X] T056 [US4] Guard legacy source/destination and initialization in `src/Kofte.Host/Bootstrap/SqliteStoreRecovery.cs`, `src/Kofte.Host/Bootstrap/SqlitePlatformStore.cs` and `src/Kofte.Host/Bootstrap/StoreCommand.cs`: permit genuine v0 only, refuse registry/history/corrupt/newer/module evidence even after confirmed reset or missing platform store, retain existing valid-v0 backup/restore/replacement safeguards and prevent module orphaning; PostgreSQL guards remain in its owned fixture/recovery delegation. Depends: T051, T055. Trace: FR-007/010/015/016, US4.3/4; principles IV-VI, X.
- [X] T057 [US4] Implement non-destructive recovery-set validation in `src/Kofte.Infrastructure/Storage/Recovery/RecoverySetVerifier.cs` and `src/Kofte.Infrastructure/Storage/Recovery/RecoveryManifest.cs`: enforce `kofte-storage-recovery-v1`, installation/provider/registry versions and revision, recovery-set UUID, complete allocation/marker/migration-state/checkpoint inventory, relative artifact names, length/SHA-256 and safe paths with no credentials/absolute paths; reject extra/missing/mixed/stale replacement evidence and require fresh exact-target backup plus explicit intent for any replacement. Depends: T051, T053. Trace: FR-006-008/010/015, US4.4; principles IV-VI, X.
- [X] T058 [P] [US4] Execute the actual stopped SQLite procedure through `tests/integration/Kofte.Storage.IntegrationTests/Recovery/SqliteRecoveryExercise.cs`: exclusive whole-snapshot lease, verified clean non-hot DELETE state, provider backup API for platform plus every module to a new private directory, computed complete manifest, safe fresh-root restore and sentinel/history/owner verification, preserving originals/partial evidence on failure and rejecting unsafe sidecars/inventory. Depends: T054, T056, T057. Trace: FR-006-010/015, US4.4, SC-004/006; principles IV, V, X, XI.
- [X] T059 [P] [US4] Execute stopped PostgreSQL recovery through `tests/integration/Kofte.Storage.IntegrationTests/Recovery/PostgresRecoveryExercise.cs` and `tests/integration/Kofte.TestSupport/PostgresResources.cs`: exclusive whole-snapshot lease, full custom-format database dump, computed inventory/checkpoints/digest, fresh owned restore target, restored role ownership/grants/markers/history/data and runtime no-database-CREATE checks; retain resource-OID/comment/loopback validation, never platform-only dump or unowned replacement. Depends: T054, T056, T057. Trace: FR-005/008/010/013/015, US4.4, SC-004/006; principles IV-VI, X, XI.
- [X] T060 [US4] Finish public `interrupted-provisioning` and `stopped-restore` cases in `tests/contract/Kofte.Storage.Consumer/Scenarios/RecoveryScenarios.cs` and independent tests in `tests/contract/Kofte.Platform.ContractTests/Modules/Storage/RecoveryContractTests.cs`; execute documented provider backup/dump, computed manifest, fresh restore and restart reads rather than handwritten constants or private result fabrication, and verify mixed/stale/registry-only refusal plus preserved reconciliation outcomes. Depends: T058, T059. Trace: FR-007/008/010/015/016, US4.1-4, SC-004/006; principles IV-VI, XI.
- [X] T061 [US4] Execute all T048-T051 and T058-T060 cases on both providers; retain a before/after durable-boundary matrix, actual elapsed budgets/cancellation cleanup, abandoned-lock results, full-set restoration and legacy-refusal inventories in `specs/003-storage-registry/implementation.md`; every defined phase must recover original ownership/data or explicitly preserve and block. Depends: T060. Trace: FR-007/008/010-013/015, US4.1-4, SC-004/006; principles IV-VI, X, XI.
- [X] T062 [US4] Publish exact stopped upgrade/retry/forward-repair/abandoned-lock/full-set recovery procedures and executable examples in `specs/003-storage-registry/quickstart.md`, `specs/003-storage-registry/contracts/migrations-and-recovery.md` and `docs/development/local-development.md`; state live-host refusal, old-binary incompatibility, chosen older whole-set recovery-point/data-loss responsibility and that UUIDs/hashes do not detect malicious forgery or an unknown newer lost backup. Depends: T061. Trace: FR-010/011/013/015/016, US4.1-4; principles V, VI, X.
- [X] T063 [US4] Re-execute the published recovery/upgrade instructions against fresh owned fixtures via `tests/contract/Kofte.Platform.ContractTests/Modules/Storage/RecoveryGuideTests.cs`, including all deliberate legacy refusal examples and no-storage/zero-module compatibility; correct documentation or implementation rather than weakening expected ownership/failure checks, and record exact guide evidence in `specs/003-storage-registry/implementation.md`. Depends: T062. Trace: FR-010/013/015/016, US4.3/4, SC-006; principles X, XI.

**Checkpoint**: US4.1-4 and SC-004 pass with actual killed processes, bounded
cleanup and executable complete-set recovery. Automatic global backup,
relocation/conversion, destructive adoption and implied global rollback remain
unsupported; an ambiguous preserved reconciliation block is truthful, not ready.

## Phase 7: User Story 5 - Diagnose storage problems without disclosing secrets (Priority: P2)

**Goal**: Safe correlated diagnostics identify outcomes and next actions; corrected
retry succeeds without deleting any existing module data.
**Independent Test**: Capture every result/output/log/span/metric channel with
seeded credentials, private paths, payloads and control characters for success,
busy, conflict, rejection, provider failure, interruption and corrected retry.

### Required Validation

- [X] T064 [P] [US5] Add public result/serialization/privacy assertions in `tests/contract/Kofte.Platform.ContractTests/Modules/Storage/StorageDiagnosticsTests.cs`; verify all seven outcome shapes/stable guidance, at most eight diagnostics, 256-character messages, 64-ASCII code/guidance fields, 4096 UTF-8 result bytes, generated 32-hex W3C correlation, safe optional canonical ID and no raw invalid input or success-shaped unknown error. Depends: T034, T046, T061. Trace: FR-007/011/014, US5.1, SC-005; principles VI, XI.
- [X] T065 [P] [US5] Add actual log/span/metric/stdout/stderr/worker-output captures in `tests/integration/Kofte.Storage.IntegrationTests/Providers/StoragePrivacyTests.cs` and corrected retry tests in `tests/integration/Kofte.Storage.IntegrationTests/Recovery/CorrectedRetryTests.cs`; seed secrets/connection strings/private paths/payloads/control characters across every success/rejection/failure class, assert zero disclosures/unauthorized changes and distinct correlated successful retry preserving all unrelated owners/data. Depends: T034, T046, T061. Trace: FR-011/013/014/016, US5.1/2, SC-005/006; principles VI, X, XI.

### Implementation

- [X] T066 [US5] Complete bounded outcome formatting and correlated telemetry in `src/Kofte.Infrastructure/Storage/StorageDiagnostics.cs`, `src/Kofte.Host/Bootstrap/BootstrapDiagnostics.cs` and `src/Kofte.Host/Bootstrap/BootstrapTelemetry.cs`; use allowlisted fixed messages/codes/guidance and operation/provider/outcome metric labels only, optional validated IDs in logs/spans not metric labels, emit every mutation/rejected boundary audit-relevant record, explicitly map known provider errors and sanitize unexpected terminal failures without broad success/busy fallbacks. Depends: T064, T065. Trace: FR-007/011/014, US5.1; principles VI, VIII, X.
- [X] T067 [US5] Wire safe output end-to-end in `src/Kofte.Host/Features/StorageMaintenance/MaintenanceSupervisor.cs`, `src/Kofte.Host/Features/StorageMaintenance/RegistryCommand.cs` and `tests/contract/Kofte.Storage.Consumer/Scenarios/CorrectedRetryScenario.cs`; do not forward raw child/provider exceptions or elevated configuration, exercise the public `corrected-retry` scenario after actual denied privilege/outage and preserve original reservation/data without reset/deletion. Depends: T066. Trace: FR-011/013/014/016, US5.1/2; principles V, VI, X.
- [X] T068 [US5] Add complete public-corpus parity in `tests/contract/Kofte.Platform.ContractTests/Modules/Storage/StorageGuideTests.cs` and `tests/contract/Kofte.Storage.Consumer/Program.cs`: both independent consumers exercise all nine named examples and all public failure/binding outcomes on both providers, `all` never drops negative/recovery scenarios, and selectors/results/zero assertions/provider failures are fail-closed. Depends: T027, T035, T047, T063, T067. Trace: FR-001/009/014/016, US5.1/2, SC-005/006; principles II, VI, XI.
- [X] T069 [US5] Execute T064/T065/T068 on both providers with actual result/output/telemetry capture and corrected retry, recording safe counts/outcomes and zero canary/private-path disclosures in `specs/003-storage-registry/implementation.md`; never retain raw secrets/provider dumps as evidence. Depends: T068. Trace: FR-011/013/014/016, US5.1/2, SC-005/006; principles VI, X, XI.
- [X] T070 [US5] Document stable outcome/cause/guidance semantics, bounded retry and telemetry/trust limitations in `specs/003-storage-registry/contracts/storage-v1.md` and `docs/development/local-development.md`; distinguish observable audit-relevant records from future durable audit 009 and ownership checks from future actor/team/resource policy. Depends: T069. Trace: FR-001/011/014/016, US5.1/2; principles VI-VIII, X, XII.

**Checkpoint**: US5.1/2 and SC-005 have observed zero-disclosure/zero-forbidden-write
evidence across all channels, and corrected retry is actually usable.

## Final Phase: Cross-Cutting Evidence and Documentation

- [X] T071 Wire required storage projects/cases into existing named suites in `scripts/verify.py` and `.github/workflows/product-checks.yml`: `unit`, both-provider `integration`, `contract`, `recovery`, `privacy` and `quality` must execute the new tests/public consumer without skipping existing steps; pass offline provisioning only to owned maintenance children, build/format all required projects and preserve current image/fixture safety and sanitized reports. Depends: T026, T034, T046, T063, T069. Trace: FR-013/014/016, SC-001-006; principles X, XI.
- [X] T072 Add focused verifier/supervisor regressions in `scripts/tests/test_verify.py`, `scripts/tests/test_dev.py` and `scripts/tests/test_dev_output.py`: missing storage project/scenario/provider, zero selected tests, failed crash/recovery/privacy case, leaked elevated serving setting, child cleanup failure and omitted suite must fail rather than report green; preserve existing failure-propagation/resource ownership. Depends: T071. Trace: FR-011/013/014/016, SC-004-006; principles VI, X, XI.
- [X] T073 Reconcile actual adopted direct/transitive/native dependencies, retained notices and locked restores after the final project graph in `docs/development/dependencies.md`, `docs/development/third-party-notices.md` and affected `packages.lock.json` files; run `python3 scripts/check_dependencies.py` and prove no unreviewed package, incompatible native asset or unnecessary EF Design/tool adoption remains. Depends: T071. Trace: FR-009/010/013, SC-006; principles III, VI, X.
- [X] T074 Update `README.md`, `CONTRIBUTING.md`, `docs/README.md`, `docs/architecture.md`, `docs/development/local-development.md`, `docs/development/spec-workflow.md` and `specs/003-storage-registry/quickstart.md` to match actual implemented APIs/configuration/commands, explicit v0/v1 and legacy refusal changes, stopped recovery responsibilities, supported environments and measured limits; keep all later modules/authorization/MCP/jobs/deployment work and feature acceptance unclaimed. Depends: T070, T072, T073. Trace: FR-001/010/013-016, SC-006; principles I-XII.
- [X] T075 Run the focused named gates `python3 scripts/verify.py --suite unit`, `python3 scripts/verify.py --suite integration --provider both --disposable-postgres`, `python3 scripts/verify.py --suite contract --provider both --disposable-postgres`, `python3 scripts/verify.py --suite recovery --provider both --disposable-postgres`, `python3 scripts/verify.py --suite privacy --provider both --disposable-postgres` and `python3 scripts/verify.py --suite quality`; execute both documented `tests/contract/Kofte.Storage.Consumer` all-scenario commands and retain exact nonzero case counts/results and safe evidence in `specs/003-storage-registry/implementation.md`. Depends: T074. Trace: FR-001-016, SC-001-006; principles III-VI, X, XI.
- [X] T076 Run the complete existing gate `python3 scripts/verify.py --suite all --disposable-postgres` and inspect actual assessed-head CI for all required contexts; retain pure manifest, host health/OpenAPI/HTTP/web/browser/axe, startup, v0 recovery, privacy and failure-propagation regressions as well as new storage evidence in `specs/003-storage-registry/implementation.md`. Report unavailable or failing checks honestly and leave this task open until actual green evidence; focused checks do not replace the full gate. Depends: T075. Trace: FR-013/015/016, SC-001-006; principles I, IX-XI.
- [X] T077 Finish `specs/003-storage-registry/implementation.md` with requirement/scenario/outcome/task/principle mappings, exact commands/results and actual revision, SDK/runtime, OS/architecture, CPU/memory/available conditions, provider/image identity, concurrency/restart/crash/deadline counts and sanitized inventories; account explicitly for all 16 FRs, six SCs, 18 scenarios and documented preserved-block/recovery limitations without inventing a fixed benchmark host or manual usability study. Depends: T076. Trace: FR-001-016, SC-001-006; principles I-XII.
- [X] T078 Run `python3 scripts/check_repository.py`, `python3 -m unittest discover -s scripts/tests -v` and `git diff --check`, review the complete scoped diff/secrets/artifacts, and publish implementation evidence and `.specify/drive-state.json` together on `feature/003-storage-registry` and draft PR #6 with immediate push after each commit; complete only implement when all tasks/evidence pass and hand off converge as the next invocation, not review/acceptance/merge now. Depends: T077. Trace: FR-016, SC-006; principles XI, XII/governance.

## Dependencies and Execution Order

The `Depends:` declarations above are the task-level dependency graph, not an
implied dependency on every prior line. Every reference is to an earlier task;
there are no cycles. All implementation waits for passing **analyze** and T001's
explicit human decision. Dependency approval is before package adoption, not a
retroactive checklist tick.

| Increment | Tasks | Entry prerequisite | Independent exit evidence |
| --- | --- | --- | --- |
| Setup | T001-T006 | Analyze pass; explicit ADR approval at T001 | Reviewed graph, projects and owned harnesses |
| Foundations | T007-T014 | Explicit listed setup dependencies | Contracts, safety/budgets, actual registry schema/init tests |
| US1 / P1 | T015-T027 | T013/T014 | Distinct persisted stores, no-storage, three restarts/immutable retry |
| US2 / P1 | T028-T035 | T026 | Both concurrency matrices, deterministic collisions/direct constraints |
| US3 / P1 | T036-T047 | T026 | Public module contexts/history and Alpha-only upgrade; zero forbidden writes |
| US4 / P1 | T048-T063 | T034 and T046 | All crash/deadline/upgrade/restore/refusal cases on both providers |
| US5 / P2 | T064-T070 | T034, T046 and T061; final corpus waits for story guides | Captured channels and data-preserving corrected retry |
| Complete implementation evidence | T071-T078 | Listed story exits | Named suites, executable examples, full gate/CI, complete mapping |

Story graph:

```text
analyze pass -> T001 human ADR gate -> setup/foundations -> US1
                                                          |-> US2 --|
                                                          |-> US3 --|-> US4
US2 + US3 + US4 durable/failure proof -> US5 -> final gates -> implement handoff
```

US2 and US3 are separately testable after US1, but their implementation tasks
share registry/binding/SQLite files: **serialize conflicting edits**, even when
acceptance work can use separate owned fixtures. Within a story, tests establish
the failing behavior first, implementation follows its named dependencies, then
actual evidence and executable documentation close the increment.

### Parallel examples per story

| Scope | Safe example after prerequisites | Boundary |
| --- | --- | --- |
| Setup | T004 and T005 after T003 | Different integration and public-process harness files |
| US1 | T015/T016/T017; later T020 and T021 | Distinct test files; separate PostgreSQL/SQLite provisioners |
| US2 | T028/T029/T030; later T031 and T032 | Distinct race/collision/constraint tests; distinct adapter files |
| US3 | T036 and T037; later T042 and T043 | Independent test files and separate Alpha/Beta module directories |
| US4 | T048/T049/T050/T051; later T058 and T059 | Separate failure files and owned provider recovery exercises |
| US5 | T064 and T065 | Public result tests versus telemetry/retry integration files |

Marked tasks must still await **all** their prerequisites. T011/T012 are
serialized to keep provider state constraints consistent. No two writers
share a fixture, data root or database.
Separate commits must be pushed immediately; no parallel stage execution.

### Implementation strategy

1. Complete the separate analyze stage, obtain human ADR approval, then establish
   the actual dependency/safety/format foundation.
2. Deliver US1 first as the smallest useful internal increment. It is not full
   storage acceptance, not an accepted feature and not MVP completion.
3. Prove races and assigned migration boundaries through US2/US3, then truthful
   crash/upgrade/full-set recovery and safe diagnosis through US4/US5.
4. Wire all required cases into existing fail-closed suites; execute documented
   examples, complete full local/CI evidence and map every acceptance obligation.
   Continue the same active implement stage until complete or explicitly blocked.

## Constitutional Coverage and Scope

### Requirement and acceptance coverage

| Requirement | Concrete implementation / evidence tasks |
| --- | --- |
| FR-001 trusted versioned contract/authority | T007, T014, T017, T024, T037, T045, T068 |
| FR-002 identity/no-storage/immutable need | T010-T013, T016, T019, T030, T033 |
| FR-003 actual UNIQUE/concurrent allocation | T011-T013, T018, T028-T034 |
| FR-004 persisted immutable ownership | T015-T019, T022-T024, T033, T053 |
| FR-005 PostgreSQL schema/names/history | T011, T018, T020, T022, T029, T036-T046 |
| FR-006 private SQLite file/path/alias boundary | T009, T012, T021, T037, T044, T051, T058 |
| FR-007 truthful durable provisioning state | T007, T019-T023, T048, T052-T054, T061 |
| FR-008 ready verification/no empty fallback | T013, T023, T030, T033, T037, T044, T048-T061 |
| FR-009 owner-bound module/EF access | T024, T036-T047, T054, T068 |
| FR-010 registry/module upgrade/rollback safety | T010-T013, T025, T036-T043, T048-T063 |
| FR-011 finite budgets/idempotent honest outcomes | T008/T009, T019, T028-T035, T049, T052-T054, T064-T070 |
| FR-012 module-local transactions/reconciliation | T019-T023, T036-T047, T048, T053/T054, T061/T062 |
| FR-013 explicit config/least privilege/portability | T002/T003, T006, T014, T020/T021, T025, T049, T055, T059, T071-T076 |
| FR-014 correlated bounded private output | T005, T007, T055, T064-T072 |
| FR-015 coherent recovery/legacy protection | T050/T051, T054, T056-T063, T074-T077 |
| FR-016 executable public examples/bootstrap compatibility | T005/T006, T017, T025-T027, T035, T045-T047, T060/T063, T068, T071-T078 |
| US1.1-4 / SC-003 | T015-T027, T044, T075-T077 |
| US2.1-4 / SC-002 | T028-T035, T075-T077 |
| US3.1-4 / SC-001 | T036-T047, T075-T077 |
| US4.1-4 / SC-004 | T048-T063, T075-T077 |
| US5.1-2 / SC-005 | T029/T030, T037/T044, T064-T070, T075-T077 |
| SC-006 documented examples/existing checks | T027/T035/T047, T060-T063, T068, T071-T078 |

### Principles and inapplicable surfaces

| Principle | Task evidence / applicability |
| --- | --- |
| I Product/brand | T025/T027, T074-T078; zero-module portability, Köfte prose, no Hoard dependency |
| II Modularity | T003/T007, T024/T025, T038-T047; minimal contracts and one justified adapter, no runtime loader |
| III Vertical slices/test ownership/licensing | T002-T006, T036-T045, T073; all test source/contexts/migrations under test projects, native DI, no MediatR/AutoMapper |
| IV Storage | T010-T063; actual constraints, byte/reserved keys, independent processes, assigned schemas/files/history, verified recovery |
| V Transactions | T019-T024, T028-T034, T041, T048-T063; separate durable commits and local transactions, no shared cross-owner connection/global ACID |
| VI Safety | T005/T006, T014, T024, T037-T044, T049, T056-T070, T072/T073; authority/credentials/target rejection/redaction, no sandbox claim |
| VII MCP | No new server/tool/discovery surface. T007/T024/T070/T074 preserve future shared operation and actor/permission/team/resource checks; storage is not an MCP authorization bypass |
| VIII Durable work | T019, T041, T048-T054, T066/T070; durable intent/reconciliation, not a new job/event/audit/realtime runtime or exactly-once guarantee |
| IX Frontend | No new visual journey, SDK or accessibility claim. T025/T074/T076 preserve existing web/browser/axe behavior and trusted build-time extension boundary |
| X Portability/operations | T002-T014, T048-T063, T070-T077; supported targets, explicit config/privileges/upgrades/recovery, no required Docker for SQLite/Kubernetes/cloud/IaC |
| XI Evidence | Story validation/checkpoints and T071-T078; real providers, public consumers, negative/crash/deadline/privacy and fail-closed complete local/CI evidence |
| XII Scope/governance | T001, T027/T035/T047/T062/T070/T074, T077/T078; exact 003 scope and one-stage/one-feature branch/PR, no inferred acceptance |

New REST/event/MCP parity tests, denied user/team/tool authorization cases,
Playwright journeys, durable jobs/events, deployment smoke and packaged
reference-module proof are not introduced here because those interfaces and
runtimes remain 004-024 work. Existing bootstrap/browser/HTTP and manifest
checks **remain mandatory** at T076. The future MVP release/reference-module
gate is neither performed nor waived by this internal slice.

## Completion and Human Handoff

This task list contains **80 tasks**, all evidenced:
six setup, eight foundational,
13 US1, eight US2, twelve US3, sixteen US4, seven US5 and eight final evidence
tasks, plus two convergence remediation tasks. All 18 acceptance scenarios, 16 functional requirements, six outcomes and
twelve principles have explicit task/evidence coverage. Parallel markers do
not change dependencies or authorize concurrent Spec Kit stages.

The tasks-generation and analyze gates are historical and passed; the human
approved ADR 0005 before implementation. The complete local gate and assessed-head
CI pass; exact revisions, results and supported limits are recorded in
`implementation.md`. Completion does not follow from file existence alone.

During implement, retain actual commands/results and acceptance mapping in
`implementation.md`; blocked/unavailable checks remain open and actionable.
Publish safe partial checkpoints as needed, with each commit pushed immediately,
without calling an unfinished stage complete. On completion, publish artifacts
and driver ledger together on `feature/003-storage-registry` and the same draft
PR #6. Converge, final review and explicit numbered-feature acceptance remain
separate later invocations. Only after passing review and explicit acceptance
may the PR become ready for a human merge request. Never merge, close,
enable auto-merge or delete its branch; never infer acceptance from checked
tasks, successful tests or an earlier merge.

## Phase 8: Convergence

- [X] T079 Complete the public `same-id-retry` example in `tests/contract/Kofte.Storage.Consumer/Scenarios/RegistrationScenarios.cs` and its owned process harness under `tests/integration/Kofte.TestSupport/`: write distinguishable module-local sentinel data through public assigned contexts, stop/release maintenance before each of three genuine child-process restarts, re-register with changed display/release metadata and assert the exact installation/owner/provider/key/allocation/revision plus readable original data after every restart on SQLite and PostgreSQL. Strengthen `tests/contract/Kofte.Platform.ContractTests/Modules/Storage/RegistrationContractTests.cs` and `StorageGuideTests.cs` so both standalone and `all` executions require those observations rather than accepting a positive assertion total alone; retain conflicting-need refusal, private configuration, bounded owned-child cleanup and public-only module consumption. Re-execute both documented provider guide commands and record exact results in `specs/003-storage-registry/implementation.md`; do not weaken the prescribed example in `contracts/storage-v1.md:189`. Depends: T017, T063, T068, T075. Trace: FR-004/016, SC-003/006, US1.2; plan: public registration/restart examples; F1 (HIGH, partial).
- [X] T080 Complete the public `collision` example in `tests/contract/Kofte.Storage.Consumer/Scenarios/CollisionScenario.cs`, arranging deterministic test-owned fixture evidence through `tests/integration/Kofte.TestSupport/` so a second distinct canonical ID attempts an already registered owner's physical candidate and the real registry UNIQUE boundary rejects it. Assert the original owner's exact assignment, sentinel data and physical inventory remain unchanged, the second ID gains no alternate/shared allocation, and bounded rejection/guidance are observed on both real providers; preserve the existing unowned-target refusal as a separate assertion. Keep forced-candidate controls fixture-only, with no production override or internal access from public module implementations. Strengthen `tests/contract/Kofte.Platform.ContractTests/Modules/Storage/RegistrationContractTests.cs` and `StorageGuideTests.cs` to require these observations in standalone and `all` runs; execute both documented provider guide commands and record actual results in `specs/003-storage-registry/implementation.md` without weakening `contracts/storage-v1.md:190`. Depends: T029, T034, T035, T068, T075. Trace: FR-003/016, SC-002/006, US2.3; plan: forced-collision/public-example proof; F2 (HIGH, partial).
