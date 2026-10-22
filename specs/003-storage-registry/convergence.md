# Convergence: 003 - Storage Registry and Module Isolation

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
