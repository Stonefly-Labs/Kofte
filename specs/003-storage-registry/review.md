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
