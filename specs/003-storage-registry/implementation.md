# Implementation: 003 - Storage Registry and Module Isolation

**Stage:** Implement, incomplete and blocked on remaining obligations; not feature acceptance.
**Branch / PR:** `feature/003-storage-registry` /
[draft PR #6](https://github.com/Stonefly-Labs/Kofte/pull/6).
**Started:** 2026-10-22 (UTC).

## Entry gates and human decision

The preceding [analysis](analysis.md) passes. The author checklist remains
16/16 and the reviewer-owned quality checklist remains 36/36, unchanged.
No before/after implementation hooks are configured: `.specify/extensions.yml`
is absent. The driver began this stage on its original clean feature branch.

For T001 the human explicitly selected:

> Approve ADR 0005 for implementation

This approves [ADR 0005](../../docs/adr/0005-module-storage-ownership.md)'s
design for implementation, not dependency adoption, runtime correctness,
feature acceptance or merge. No requirements or design scope changed.

## Dependency adoption and execution environment

T001-T006 have actual evidence. Later tasks remain open unless their complete
acceptance obligations and prerequisites are evidenced, even where substantial
code and passing focused checks already exist. This is an implementation
checkpoint, not a claim that all 78 tasks or the implement stage are complete.

The default shell's SDK selection failed: `dotnet --version` could not find
the pinned SDK 10.0.402 (only system SDK 10.0.203 was selected). The official
10.0.402 macOS arm64 archive was downloaded into ignored task-local tooling
and its SHA-512 matched the already reviewed official release metadata:
`ee0d379c144ebddef6efe851e1514883d1b695430d13b3fb13e8153f8476e1e5ac7abc42c08668471aaf4d738ff367cf53009845f94d224ba126febe74d00b75`.
The explicit task-local `dotnet --version` reports 10.0.402.
The existing pinned Node 24.22.0 installation was located; no product pin was
changed to accommodate the shell.

The nine additional NuGet identities were reviewed **before adoption**:
EF Core/Relational/Sqlite/Core/Abstractions/Analyzers 10.0.13,
Microsoft.Extensions.Caching.Memory/Abstractions 10.0.13 and Npgsql EF 10.0.3.
Microsoft archives identify source commit
`4099f83977f4d29604a9aeb971fb0a7bc1d5f699` under MIT; Npgsql EF identifies
`5e912bf071068a44e5abbdab497b313ba99d3ee1` under PostgreSQL License.
Framework assets and EF dependency ranges admit the selected exact graph.
No EF Design/global tool or additional native SQLite bundle was adopted.
Existing SQLitePCLRaw 2.1.13 assets retain their reviewed provenance/notices.

All nine official signed archives passed the pinned supported Linux SDK's
NuGet signature verification, **nine verified, zero warnings/errors**, with
networked certificate/revocation access. An earlier offline run produced
revocation warnings and was not counted as passing verification.
The official NuGet vulnerability feed and subsequent locked graph audit found
no vulnerable packages. The committed inventory reconciles **403 npm and
78 NuGet identities**; locks, central pins and retained notices are updated.
See [dependency adoption](../../docs/development/dependencies.md) and
[retained notices](../../docs/development/third-party-notices.md).

Observed workstation: **macOS 26.5 arm64, Apple M5 Pro, 64 GiB RAM**;
.NET SDK 10.0.402/runtime 10.0.13, Node 24.22.0/npm 11.19.1,
Docker server 29.8.2 and the existing pinned PostgreSQL 17 fixture image.
SQLite does not require Docker. These are actual execution conditions, not
new supported-target or benchmark claims. Tool archives, credentials, runtime
roots and transient child output are not committed.

## Implemented checkpoint

The pure platform project now owns versioned provider-neutral contracts,
immutable assignments, seven disjoint outcomes and safe diagnostic metadata.
Only ready carries an assignment. Infrastructure owns the provider/EF packages;
test-owned contexts, migrations and samples remain inside dedicated `tests/`
projects under normal SDK inclusion. Host safety facades delegate to shared
Unix path and installation-lease mechanisms rather than copying them.

The registry implements explicit table-free-v0 to format-1 local transactions,
script SHA-256/history verification, SQLite pragma/integrity agreement,
canonical keys and actual ID/key/allocation-token UNIQUE constraints.
Installation registry revision is checked against the sum of registration
revisions. Keys/tokens/storage need are immutable, and ready transition verifies
the exact returned optimistic revision. Unknown platform tables/history,
unaccounted PostgreSQL schemas at v0, empty/unowned SQLite headers and malformed
persisted mappings are not silently adopted.

Registration commits reservation before physical work, persists explicit
no-storage, reuses valid stored allocations across metadata changes and verifies
actual owner markers before ready. PostgreSQL uses a separate offline
non-admin provisioner against the same owned database/run identity, creates
runtime-owned schema/marker transactionally and removes PUBLIC schema access.
The runtime keeps its no-database-CREATE posture. SQLite creates private
module directories/files exclusively; ordinary ready access requires an
existing validated file and never recreates a missing store.

The shared acquisition/operation budgets are 2/3/30/120 seconds, with 25 ms
lock probes and finite provider timeouts. PostgreSQL acquisition retains one
non-pooled session across probes and checks that exact advisory lease at use;
it does not reconnect/reacquire after lease loss. These source properties and
focused checks are **not complete elapsed-deadline/crash-worker proof**.

The bounded assigned EF increment now issues owner-only bindings, internally
chooses connections/history, separates provider/schema/context/design-time
model-cache keys and binds public migration/snapshot services before evaluation.
It preflights the entire pending set, not just the next migration. Actual tests
reject late raw/suppressed SQL, foreign/reserved targets, foreign models/FKs,
renames, provider annotations, unknown operations, unbound snapshots, factory
connection/owner switches and stale/disposed bindings before forbidden writes.
Current supported DDL is deliberately narrow: create-table/primary-key and
add-column with int/text columns. Other operations/annotations fail closed.
Physical post-verification covers tables, columns, types and nullability, not
every provider metadata feature; broader approved obligations remain open.

Host wiring validates genuine v0 or consistent v1 at startup without migrating,
rejects elevated serving configuration and strips the runtime connection from
the serving configuration registered in DI. The explicit `--registry upgrade`
uses an admitted owned child, not a public registration CLI. Legacy SQLite
source/destination/init/reset and PostgreSQL archive guards preserve v1/module
evidence. The recovery verifier checks complete manifest inventory, hashes,
markers, revisions and migration checkpoints. Actual stopped SQLite backup API
and full PostgreSQL custom-dump/fresh-owned-restore exercises retain real
Alpha/Beta rows and EF histories. These test-owned exercises are not a complete
recovery product or the still-unimplemented public recovery consumer scenarios.
The supervisor reserves two seconds for owned-child cleanup within the original
30-second total budget, bounds both reap and pipe drain, and sanitizes process
start/kill failures. Unconfirmed cleanup fails unavailable rather than claiming
success; the child independently retains its maintenance lease while alive.

## Executed checks

Commands below used the explicitly selected pinned toolchain. PostgreSQL cases
used `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`, with inherited
`KOFTE_POSTGRES_CONNECTION` and `KOFTE_POSTGRES_PROVISIONING_CONNECTION` unset
so each test created and disposed only its own validated fixture. This report
contains no connection strings, targets, row payloads or raw exception dumps.

| Exact command or selector | Observed result |
| --- | --- |
| `dotnet restore Kofte.slnx --locked-mode` | Passed |
| `dotnet list Kofte.slnx package --vulnerable --include-transitive --no-restore` | Passed; no vulnerable packages in all eleven projects |
| `python3 scripts/check_dependencies.py` | Passed; 403 npm / 78 NuGet identities |
| `dotnet build tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj --no-restore -c Release --verbosity quiet` | Passed; zero warnings/errors |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore -c Release --filter-class '*SqliteRegistrationTests' --filter-class '*PostgresRegistrationTests' --filter-class '*RegistryConstraintTests' --filter-class '*RegistryFormatTests' --minimum-expected-tests 1` | Passed; 16 tests, zero skipped, 19.607 s |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore -c Release --filter-class '*RegistrationConcurrencyTests' --minimum-expected-tests 8` | Passed; 8 cases, zero skipped, 106.014 s |
| `dotnet build tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj --no-restore --verbosity quiet` | Passed; zero warnings/errors |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore --filter-class '*RestartTests' --filter-class '*AllocationCollisionTests' --filter-class '*StorageTelemetryTests' --minimum-expected-tests 1` | Passed; 14 tests, zero skipped, 30.094 s |
| `dotnet build tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj --no-restore --verbosity quiet` | Passed; zero warnings/errors |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore --filter-class '*StorageConsumerHarnessTests' --filter-class '*RegistrationContractTests' --minimum-expected-tests 26` | Passed; 26 tests, zero skipped, 31.245 s |
| `python3 scripts/check_repository.py` | Passed |
| `python3 -m unittest discover -s scripts/tests -v` | Passed; 103 tests |
| `git diff --check` | Passed |

Subsequent integrated/current-source observations:

| Exact command or selector | Observed result |
| --- | --- |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore -c Release --filter-class '*SqliteRegistrationTests' --filter-class '*PostgresRegistrationTests' --filter-class '*RegistryConstraintTests' --filter-class '*RegistryFormatTests' --filter-class '*RestartTests' --filter-class '*AllocationCollisionTests' --filter-class '*StorageTelemetryTests' --filter-class '*RegistrationConcurrencyTests' --minimum-expected-tests 38` | Passed; 38 tests, zero skipped, 125.836 s |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-build --no-restore --filter-class '*OperationBudgetTests' --filter-class '*MaintenanceScopeTests' --filter-class '*StorageAllocatorTests' --minimum-expected-tests 14` | Passed; 14 tests, zero skipped |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~ModuleMigrationTests\|FullyQualifiedName~AssignedModelTests\|FullyQualifiedName~StorageBoundaryTests' --nologo --verbosity quiet` | Passed; 39 tests, zero skipped, bounded EF increment |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj --no-restore --filter 'FullyQualifiedName~ModuleContractTests\|FullyQualifiedName~BindingContractTests' --nologo --verbosity quiet` | Passed; 7 tests, zero skipped |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~CancellationAfterCommittedIntent\|FullyQualifiedName~EntirePendingSetIsRejected\|FullyQualifiedName~ModuleMigrationTests' --nologo --verbosity quiet` | Passed; 22 tests, zero skipped, overlapping the prior EF selection |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj --no-restore --filter FullyQualifiedName~StorageConfigurationTests --verbosity quiet` | Passed; 6 tests |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~StorageCompatibilityTests\|FullyQualifiedName~StorageRecoveryCompatibilityTests\|FullyQualifiedName~StoreRecoveryTests\|FullyQualifiedName~PostgresRecoveryTests' --verbosity quiet` | Passed; 15 tests |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~RecoverySetTests\|FullyQualifiedName~PostgresRecoveryExercise\|FullyQualifiedName~OperationDeadlineTests' --verbosity quiet` | Passed; 18 tests |
| `npm run format:check` | Passed after formatting the two generated contract lock files |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --verbosity quiet` | Passed; zero warnings/errors |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-build --no-restore -c Release --minimum-expected-tests 1` | Passed; 45 tests |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-build --no-restore -c Release --minimum-expected-tests 1` | Failed; 35/36 passed, one PostgreSQL Staging host exited during startup |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-build --no-restore -c Release --filter-class '*PostgresOpenApiTests' --minimum-expected-tests 1` | Passed; both Staging/Production cases in isolation; this does not erase the broader failure |

Final integrated-source checks after all implementation edits and formatting:

| Exact command | Observed result |
| --- | --- |
| `dotnet format Kofte.slnx --no-restore --verbosity quiet` | Passed |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet` | Passed |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --verbosity quiet` | Passed; zero warnings/errors |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-build --no-restore -c Release --minimum-expected-tests 45` | Passed; 45 tests, zero skipped |
| `dotnet test --project tests/unit/Kofte.Platform.Tests --no-build --no-restore -c Release --minimum-expected-tests 129` | Passed; 129 tests, zero skipped |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-build --no-restore -c Release --minimum-expected-tests 39` | Failed with exit 9: all 37 discovered cases passed, but the guessed minimum was incorrect |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-build --no-restore -c Release --list-tests` | Passed; discovered the complete 37-case inventory, including both PostgreSQL OpenAPI cases and six supervisor/compatibility cases; source comparison confirms no test declarations were removed |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-build --no-restore -c Release --minimum-expected-tests 37` | Passed; all 37 cases, zero skipped, 21.506 s |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore -c Release --minimum-expected-tests 105` | Failed twice: first 103/105 passed with both PostgreSQL process-race cases failing on worker exit 1; diagnostic repeat 104/105 passed with the distinct-ID PostgreSQL process case failing during owned-fixture database cleanup with an Npgsql stream-read timeout. Zero skipped in both runs |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --minimum-expected-tests 48` | Passed separately after the broad storage failures; all 48 cases, zero skipped, 136.110 s |
| `python3 scripts/check_repository.py` | Passed with blocked driver state |
| `python3 -m unittest discover -s scripts/tests -v` | Passed; all 103 tests |
| `python3 scripts/check_dependencies.py` | Passed; 403 npm / 78 NuGet identities |
| `npm run format:check` | Passed |
| `git diff --cached --check` | Passed |

The earlier PostgreSQL Staging startup failure did not reproduce in either
whole-project current-source run; its root cause is not established. The first
repeat still failed its test-count policy and is not reported as a passing gate.
Only after checking discovery and source inventory was the count corrected to 37.

The **full storage integration gate is not passing**. The focused race-matrix
passes above do not erase either later broad-run failure, and passing cases
from separate runs must not be combined into a fabricated 105/105 result.
The first broad run reported 314.348 s; the repeat's runner summary reported
34m 15.728 s while its assembly execution reported 6m 18.120 s. That timing
discrepancy and the underlying worker/cleanup failures are not explained.
No timeout was relaxed, non-busy error retried or cleanup failure suppressed.
The private registration worker now emits a structured allowlisted failure
code, and its parent surfaces that code rather than only asserting exit 0.

The repeat's failed disposal retained one owned PostgreSQL fixture. Before
targeted cleanup, its private ownership record was matched against the exact
container/volume/network inventory, local Unix-socket engine, pinned image,
run labels, database OID/owner/comment and both role OIDs/comments. Only that
verified fixture was removed, and its recorded resources and ownership root
were confirmed gone. This cleanup does not turn the failed test into success.
No raw target, credential, run identity or transient provider output is published.

Review found a migration-cleanup path that created a fresh three-second budget
after the 120-second operation budget. It now reuses the original remaining
budget; cancellation/expiry preserves committed `in_progress` and emits a fixed
correlated secondary log/trace event if reconciliation cannot complete, while
retaining the original failure/cancellation code. Both-provider real
cancellation-after-intent tests verify the persisted row, correlation and
secondary trace event. No new 123-second effective budget is accepted.

The shared-fixture extraction separately passed 12 selected host integration,
7 storage provider-bootstrap and 18 initial public-harness cases, all with zero
skips. Targeted allocator/budget tests passed 13 cases; the held/disposed/foreign
maintenance-scope test passed one case. These earlier observations are not a
substitute for current-source final gates.

### What the observations actually prove

- The registration/format checks exercise both real providers, explicit
  idempotent upgrades, unsupported/populated v0 preservation, bad PostgreSQL
  history, direct duplicate IDs/keys/tokens and invalid IDs/states/nulls/
  revisions/keys. Cancelled-before-mutation calls remain unchanged.
- Each of the eight race cases executes **20 rounds of ten attempts**. Per
  provider, same-ID and distinct-ID rounds run through independent connections
  and separately launched barrier-started processes: **1,600 total attempts,
  including 800 independent processes** across both providers. Every round
  checks registry counts, physical schema/file inventory and ready markers.
  Same-ID rounds leave exactly one owner per round; distinct-ID rounds leave
  ten different allocations. Retries are bounded; no hash-probability argument
  replaces forced collisions or direct constraint tests.
- Three full child-process restarts on each provider preserve the exact Alpha
  allocation/installation/key/revision and the distinct Alpha/Beta sentinel
  rows after harmless display/release changes. Forced collisions preserve
  original owner/sentinel/revision, with no suffix allocation.
- The independent public consumer passes explicit no-storage, immutable
  same-ID retry, corrected declaration retry and occupied/unowned-target
  preservation on both providers. Bad selectors, missing owned configuration,
  zero assertions, invalid exits and malformed/mismatched results fail closed.
  Internal stderr is captured and structurally validated; it is not forwarded
  as unexpected successful-consumer stderr.
- Real stderr, result serialization, Activity spans and Meter measurements
  are captured for successful registration/resolve, rejected conflict/path
  and cancelled registration. Five matching generated correlations are
  checked per provider; bounded metric labels exclude module IDs. Seeded
  display/private-path/payload text and actual runtime credentials are absent
  from these captured channels. This is **not** the complete migration,
  worker, recovery and docs-channel privacy corpus.

An initial public-consumer run found reference `==` comparison of independently
parsed `ModuleId` objects and overly broad intermediate directory creation.
The consumer now uses `ModuleId.Equals` and explicitly creates the private
module parent before its occupied-target fixture; all 26 selected cases pass.
Earlier compilation attempts intersected in-progress EF source and did not
count as verification. No failed check has been relabeled as passing.

## Acceptance mapping and remaining obligations

| Scope | Actual checkpoint evidence | Still required before implementation completion |
| --- | --- | --- |
| FR-001/002/004/007; US1; SC-003 | Public typed results, no-storage, immutable retries, actual marker verification, three process restarts and retained sentinels | Complete owner-binding/host integration corpus and all declared mutation/refusal cases |
| FR-003/005/006/011; US2; SC-002 | Real constraints, actual keys, forced/reserved collisions, full connection/process race matrix | Exhaustive corrupt-reopen/target-permission cases and measured failure/retry bounds |
| FR-009/010/012; US3; SC-001/005 | Real assigned model/cache/history, complete-pending-set rejection, factory/alias/stale binding checks, Alpha-only additive upgrade and independent public module proof | Broader supported operation/provider metadata corpus, migration-worker/deadline integration and exhaustive boundary obligations |
| FR-007/008/010/013/015; US4; SC-004 | Existing-only opens, explicit upgrade, bounded SQLite cancellation-or-provider-refusal, deterministic owned-process reap, stopped whole-set provider recovery and legacy/refusal checks | Every durable-boundary real-process kill phase, guaranteed kill-during-SQL, full deadline/outage/session-loss proofs, abandoned EF-lock recovery and executable public recovery/guide proof |
| FR-014; US5; SC-005 | Direct registration result/log/span/metric capture and public sanitized consumer results | Full seeded provider/EF/worker/recovery/docs corpus and data-preserving corrected configuration retry |
| FR-016; SC-006 | Public named selectors and existing bootstrap resource ownership retained | Named verifier/CI wiring, all documented examples, complete local gate and actual assessed-head CI |
| Principles I-XII | Human design approval; reviewed graph; pure contracts/test ownership; shared safety; local transactions; honest evidence; no new endpoint/loader/MCP bypass or acceptance | Complete remaining story evidence and final constitutional/traceability review |

Convergence, final evidence-based review and explicit human acceptance remain
separate later stages. No feature acceptance, merge permission, production
readiness, complete all-suite gate or assessed-head CI success is claimed.

## Blocked handoff

The ledger records **003 / implement / blocked** on the original branch/PR.
T001-T006 are checked; T007-T078 remain open. Existing passing checks do not
waive their full acceptance obligations or prerequisites. The next invocation
resumes implement rather than starting converge or a new numbered feature.
First resolve the two failed broad PostgreSQL process-race/fixture-cleanup
runs and establish a passing complete storage gate without weakening its
deadlines, provider selection, resource ownership or cleanup assertions.

Owner binding currently creates nested fresh three-second resolve/open budgets;
it still needs a genuinely shared end-to-end budget. Long SQLite EF migration
work remains in-process, not in the supervised migration worker. A surviving
EF migration-lock row is refused, but explicit abandoned-lock recovery is not
implemented. Cancellation preserving committed intent is not equivalent to
killing a worker during real SQL.

Remaining work includes the complete durable crash/acknowledgement/upgrade
boundary corpus, guaranteed kill-during-SQL, provider outage/privilege/lease-loss
and elapsed-deadline evidence, broader assigned-EF and malformed-registry
validation, and complete seeded privacy and corrected-configuration retry.
The public `interrupted-provisioning` and `stopped-restore` scenarios deliberately
fail as unimplemented. Finish those scenarios and executable guide proof, wire
the named verifier and CI, and run complete required local/assessed-head gates
before claiming implementation completion. No checklist was re-evaluated or
feature acceptance requested.
