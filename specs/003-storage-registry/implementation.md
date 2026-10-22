# Implementation: 003 - Storage Registry and Module Isolation

**Stage:** Implement remediation T079-T080 complete; all 80 tasks evidenced.
The original 78-task checkpoint and its complete local/CI evidence are preserved below.
**Branch / PR:** `feature/003-storage-registry` /
[draft PR #6](https://github.com/Stonefly-Labs/Kofte/pull/6).
**Started:** 2026-10-22 (UTC).
**Completed:** 2026-10-22 (UTC).
**Original verified product revision:** `4eb236a9cfb4196fb385ee7fbff1cf8ffc6017ee`.
**Original assessed CI head:** `92e942efcc1e9033f17c7e24f7b20c53c940f9da`.
The latter changes only CI mirror setup and documentation, not product, test or
verification-script source. The final publication adds stage documentation,
task completion and the driver transition; its checks are recorded in the PR
without a self-referential bookkeeping commit.

## Convergence remediation T079-T080

**Date:** 2026-10-22 (UTC). **Stage:** Implement only, on the original branch
and draft PR. The [first convergence](convergence.md) appended these two HIGH
partial public-example tasks; its historical gaps result is not rewritten by
implementation. Follow-up convergence, final review and explicit human
acceptance remain separate invocations.

The remediation source is the tested worktree based on
`e9e0a7bf06cb3202dff20f0c8bd5b3f84d9418b6`, published together with this report,
all 80 completed task markers and the driver transition. Git blob identities
identify the exact tested consumer sources before the publication commit:

| Source under `tests/` | Tested blob |
| --- | --- |
| `contract/Kofte.Storage.Consumer/Program.cs` | `6b53829216e11aad27b88bc361a38327f4e6d508` |
| `contract/Kofte.Storage.Consumer/Scenarios/RegistrationScenarios.cs` | `0a23d7def9c9b3a1d10ce3c5d24660024d52d59c` |
| `contract/Kofte.Storage.Consumer/Scenarios/CollisionScenario.cs` | `64f46be7f954fed39c1babdd92533307cc5ac191` |
| `integration/Kofte.TestSupport/StorageRestartProcess.cs` | `37e3d7b708ed9724122f3502211df5ddec1f4429` |
| `integration/Kofte.TestSupport/StorageCollisionFixture.cs` | `e5e34907b173247fea1e8e36fb035c6983872d01` |
| `integration/Kofte.TestSupport/StorageExampleEvidence.cs` | `44db6e6367d2915d5ab47aebb851f41e8a031206` |

### Actual observations and acceptance mapping

| Task / intent | Actual public example and required observations |
| --- | --- |
| T079 / F1; FR-004/016, SC-003/006, US1.2 | Public Alpha/Beta assigned contexts migrate and write distinct original records. All bindings, contexts and the setup maintenance scope are disposed before child launches. Three sequential admitted processes reopen the same installation, re-register with changed display/release metadata, resolve both owners and read both original records. Each child releases its own maintenance scope and is reaped before the next starts. Parent comparisons cover contract version, canonical owner, provider, installation UUID, allocation UUID/key, allocator version and revision. Observations require three distinct non-parent process IDs, six exact assignments and six retained sentinel reads. Conflicting-need refusal remains. |
| T080 / F2; FR-003/016, SC-002/006, US2.3 | Register/migrate a real original owner and write/read its module-local sentinel through the public Alpha context. The test-support fixture routes a second distinct ID to that existing candidate using the existing internal allocator seam. Actual registry INSERT encounters the real provider UNIQUE boundary; bounded rejection is `storage_conflict`, `correct_configuration`, unchanged durability, under 30 seconds. Compare the original exact assignment, read the original record, reject resolution of the absent contender and compare complete stopped inventories before/after, including registry revision, rows, physical schemas/files, markers and histories. The existing separate unowned occupied-object refusal and preserved bytes/row/no-marker assertions still execute. |
| T079/T080 public gate; FR-016, SC-006 | Both standalone registration tests and the `all` guide require the semantic observations, not only a positive assertion total. The supervisor and independent contract harness validate the bounded nine-field result including `observations`; missing observations or decrementing any required observation fails even with 100 assertions per case. |

The provider constraint is not fabricated by the fixture: only candidate
selection changes, and the real `StorageRegistry.RegisterAsync` reservation
transaction runs unchanged. The only production-project change grants the
dedicated test-support assembly access to the existing internal constructor.
No production candidate override, registry/provider behavior, package, lock,
module context/migration, approved requirement, ADR or reviewer-owned checklist
changes. All .NET source stays inside its owning test project.

The restart helper reuses `StorageMaintenanceProcess.RunAsync`/`AdmitAsync`,
bounded output and private environment configuration; it does not create a
parallel lifecycle/supervision mechanism. Restart-worker diagnostics are
captured and validated, errors remain fixed safe failures, and no sentinel
payload, credential or private path enters the public result.
Constitution III/IV/V/VI/X/XI/XII remain enforced; other principle/interface
applicability is unchanged from the original mapping below.

### Executed remediation checks

Environment was freshly observed: macOS 26.5 arm64, Apple M5 Pro, 64 GiB RAM,
18 logical CPUs, Python 3.15.0, Docker server 29.8.2; selected .NET SDK
10.0.402 and Node 24.22.0/npm 11.19.1. Provider versions/image and reviewed
dependencies are unchanged from the original environment below. Inherited
runtime/provisioning connection settings were unset. Tests use only the
existing explicitly owned disposable PostgreSQL fixture and private SQLite roots.

```bash
export DOTNET_ROOT="$PWD/artifacts/toolchain/dotnet-10.0.402"
export PATH="$DOTNET_ROOT:$(dirname "$(fnm exec --using=24.22.0 node -p 'process.execPath')"):$PATH"
unset KOFTE_POSTGRES_CONNECTION KOFTE_POSTGRES_PROVISIONING_CONNECTION
```

| Exact command | Observed result |
| --- | --- |
| `dotnet build tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj --no-restore -c Release --nologo -v quiet` | Passed, zero warnings/errors. |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*RegistrationContractTests' --filter-class '*StorageGuideTests' --filter-class '*StorageConsumerHarnessTests' --minimum-expected-tests 35` | Passed all 35, zero skipped, 74.387 seconds. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore` | Passed after correcting the new nested-loop indentation with the existing formatter. |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo -v quiet` | Passed, zero warnings/errors. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*SqliteRegistrationContractTests' --minimum-expected-tests 4` | Passed 4/4, zero skipped, 7.257 seconds. |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*PostgresRegistrationContractTests' --minimum-expected-tests 4` | Passed 4/4, zero skipped, 29.455 seconds. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*SqliteStorageGuideTests' --minimum-expected-tests 1` | Passed 1/1, all nine examples, 5.938 seconds. |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*PostgresStorageGuideTests' --minimum-expected-tests 1` | Passed 1/1, all nine examples, 44.215 seconds. |
| `python3 scripts/verify.py --suite contract --provider both --disposable-postgres` | Passed all 37 named steps, including public contracts and both-provider native OpenAPI/HTTP/host/web/output checks. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed all 111 tests. |
| `git diff --check` | Passed. |

The four separately executed provider selectors are the exact published
standalone/`all` guide commands, not a substitute corpus. Each `all` result still
requires all nine scenarios and seven actual diagnostic outcomes, and now also
the complete restart/collision observations. Synthetic harness regressions
accept a complete valid result first, then independently remove/decrement
required observations and require rejection.

The initial red run built the strengthened contract harness against the
unchanged eight-field consumer and failed all four selected SQLite registration
tests with `storage_consumer_result_invalid`; this demonstrates the new wire gate,
not a claim that those failures individually exercised restart retention.
One intermediate build caught a local declaration-order error; it was corrected.
The later combined 35-test run and the final exact guide commands above passed.

No new local complete `--suite all`, integration, recovery, privacy, unit or
quality-suite run is claimed by this focused remediation. The original complete
local/CI evidence below remains historical, not final-remediation-head evidence.
Publication-head CI is reported in the PR after pushing, without a second
bookkeeping commit. No before/after implement hook is configured:
`.specify/extensions.yml` remains absent.

## Original implementation checkpoint (historical)

## Entry gates and approval

The preceding [analysis](analysis.md) passes. The author checklist is 16/16
and the reviewer-owned quality checklist is 36/36, unchanged. No implementation
hooks are configured: `.specify/extensions.yml` is absent. This resumes the
original implement run, branch and draft PR, not another feature or stage.

The human explicitly selected:

> Approve ADR 0005 for implementation

That authorizes [ADR 0005](../../docs/adr/0005-module-storage-ownership.md)'s
design, not feature acceptance or merge. No additional human approval is
needed to finish the engineering. Converge, independent final review and
explicit numbered-feature acceptance remain separate later stages.

## Dependency review and observed environment

Before adoption, the nine additional NuGet identities were reviewed:
EF Core/Relational/Sqlite/Core/Abstractions/Analyzers 10.0.13,
Microsoft.Extensions.Caching.Memory/Abstractions 10.0.13 and Npgsql EF 10.0.3.
Microsoft archives identify MIT source commit
`4099f83977f4d29604a9aeb971fb0a7bc1d5f699`; Npgsql EF identifies PostgreSQL
License source commit `5e912bf071068a44e5abbdab497b313ba99d3ee1`.
All nine official archives passed networked NuGet signature verification with
the pinned supported Linux SDK, zero warnings/errors. An earlier offline
revocation-warning run was not counted as passing. No EF Design/global tool
or additional native SQLite bundle was adopted.

The locked inventory remains **403 npm / 78 NuGet identities**, including
the previously reviewed SQLitePCLRaw 2.1.13 assets. See
[dependency review](../../docs/development/dependencies.md) and
[retained notices](../../docs/development/third-party-notices.md).
The resumed increment adds no dependency.

Observed workstation: macOS 26.5 arm64, Apple M5 Pro, 64 GiB RAM,
18 logical CPUs, Python 3.15.0 and Docker server 29.8.2.
Executed tools: .NET SDK 10.0.402/runtime 10.0.13, Node 24.22.0/npm 11.19.1,
EF Core 10.0.13 and Npgsql/Npgsql EF 10.0.3.
PostgreSQL uses the existing owned image:
`postgres:17@sha256:a63c1ea3f6a3a5a1621f97d2aba3b30faecfab4224c1b0000cc85ced57bb1939`.
SQLite requires no Docker. These are actual available conditions, not a fixed
benchmark machine or a new supported-platform claim.

The default shell selected an older SDK. The ignored task-local official
10.0.402 arm64 SDK archive matched its reviewed release-metadata SHA-512:
`ee0d379c144ebddef6efe851e1514883d1b695430d13b3fb13e8153f8476e1e5ac7abc42c08668471aaf4d738ff367cf53009845f94d224ba126febe74d00b75`.
Commands below explicitly selected that SDK and the existing pinned Node.
PostgreSQL tests used `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`, with inherited
runtime/provisioning connection settings unset. Only validated test-owned
resources were created, paused, terminated, restored or disposed.

## Implemented behavior and inventory

The pure platform assembly defines version-1 contracts, immutable assignments
and seven disjoint outcomes. Only ready carries a usable assignment.
Infrastructure owns EF/provider dependencies; ordinary modules receive
owner-bound access, not registry/maintenance authority or configurable targets.
All sample contexts, migrations and .NET test source physically belong to
dedicated projects under `tests/`, without linked production test source.

Format-1 platform inventory is `__kofte_installation`,
`__kofte_module_registrations` and `__kofte_registry_migrations`, isolated
inside `kofte_system` or `platform/platform.db`. Registry script hashes,
versions, provider identity and revisions are checked before use. Actual
database constraints enforce canonical identity/key/token uniqueness.
Explicit table-free-v0 upgrade is transactional/idempotent; serving never
initializes or upgrades it automatically.

Each relational allocation has an immutable random token and exact
`__kofte_storage_owner` marker. The two sample modules deliberately reuse
`Records` and `__EFMigrationsHistory` inside separate schemas/files.
Alpha's v2 adds a nullable column, preserves its original row and has two
history entries; Beta keeps its independent row and one-entry history.
No-storage has a registry record but zero allocation/schema/file.
The registry stores no module payloads.

PostgreSQL's separate owned, non-admin offline provisioner creates a
runtime-owned schema/marker transactionally and revokes PUBLIC access.
The runtime never receives database CREATE. SQLite uses exclusive creation,
0700 directories/0600 files, existing-file opens, verified ancestors/sidecars,
DELETE/FULL/foreign-key posture and no pooling/shared cache/ATTACH.
Missing ready targets are never recreated.

Assignments are revalidated at bind, open and command execution. Models,
snapshots, history and the entire pending migration set are bound and
preflighted before module SQL. Unsupported operations, raw migration SQL,
transaction suppression, foreign/reserved objects and forged/stale/disposed
bindings fail closed. Physical verification checks tables/columns/types,
nullability/defaults, primary keys, indexes/constraints and unsupported
objects; unsupported SQLite DDL clauses and PostgreSQL collations are refused.

Registration and module migration use separate durable intent/physical/current
commits, not cross-module or cross-file atomicity. Explicit recovery verifies
the original installation/owner/token, complete known history prefix, recorded
target/checkpoint and physical effects before acknowledging or retrying it.
Only that deliberate action may clear a recognized abandoned SQLite EF lock.
Unknown lock/history/schema evidence stays unchanged and blocked.

Budgets share monotonic remaining time: resolve/open 3 s, acquisition 2 s,
registration/upgrade 30 s, migration/recovery 120 s, with 25 ms lease probes.
Provider timeouts are finite and clamped; Npgsql cancellation adds no unbounded
response grace. The admitted owned worker holds its lease until completion or
targeted termination/reap; the supervisor reserves 2 s inside the original
total for cleanup. Invalid output or observer failure kills/reaps promptly
and exposes only a fixed safe failure, including synchronous observer errors.

## Crash, restart, deadline and recovery evidence

| Matrix | Actual behavior asserted |
| --- | --- |
| Concurrent registration | Each provider runs 20 rounds of ten same-ID and 20 rounds of ten distinct-ID callers, separately for independent connections and barrier-started processes: 1,600 attempts total. Every round inspects registry rows, actual object count and distinct ownership; final same-ID fixtures have 20 allocations and distinct-ID fixtures 200. Compatible busy retry converges. |
| Restart | Three complete child-process restarts per provider retain assignment/token and sentinel data through display/release changes; missing ready targets do not become empty-data success. |
| Provisioning kill | Six before/after reservation, physical and ready commit phases per provider, plus SQLite directory/file-before-marker: 14 killed boundaries. Matching verified state finishes the original allocation; ambiguous SQLite pre-marker files/journals remain byte-preserved and reconciliation-required. |
| Migration kill | Six before/after intent, actual history-bearing migration and current commits per provider, plus SQLite's actual native SQL callback: 13 killed boundaries. Only committed current state resolves ready; verified recovery retries pending work or acknowledges committed history. |
| Upgrade kill | Before/after registry commit on both providers: four killed boundaries; restart observes intact v0 or consistent v1, never half-upgraded state. |
| Native hard deadlines | Actual 30 s upgrade and 120 s synchronous SQLite migration holds, not shortened substitutes. Worker death/reap, lease release and explicit incomplete state are checked before the total limit. |
| Failure/correction | Real 3 s provider outage, 2 s lease contention, cancellation, denied/missing/mismatched offline authority and terminated PostgreSQL advisory session; original assignment/history/data are retained and no session is silently reacquired. |
| Abandoned lock refusal | Actual killed SQLite migration followed by unknown lock shape, timestamp or history; recovery preserves file bytes, registry state and lock. Physical column/index/PK/default/view/check/collation mismatch is refused before mutation. |
| Whole-set restore | Actual stopped SQLite provider backups of all three files, or full PostgreSQL custom dump into a fresh owned target; original Alpha/Beta ownership, rows and histories are read after restore. |
| Recovery refusal | Missing/extra/unsafe/damaged members, digest/length/format/revision/marker/checkpoint/inventory/path mismatch, mixed ownership and existing replacement targets are refused without modifying original data. Legacy platform-only backup/restore/reset is refused for format-1/module evidence. |

The migration commit barrier is attached to the transaction containing the
actual EF history INSERT, not EF's earlier history-table initialization.
Read-only SQLite ownership inspection does not recover/delete an ambiguous
rollback journal merely while deciding whether recovery is safe.

## Public examples and privacy

Both independent public module implementations exercise the closed nine-case
corpus: clean-install, no-storage, same-id-retry, collision, path-rejection,
interrupted-provisioning, corrected-retry, isolated-upgrade and stopped-restore.
The `all` guide requires every case, actual positive per-case assertion counts
and every one of the seven observed outcomes. The parent validates an exact
bounded eight-field result, names/counts/sum and child exit; missing/unknown
scenario/provider, omitted case, zero assertions or malformed output fail.
Recovery and corrected retry use real failures/provider backups, not fabricated
success results. Guide execution is in `StorageGuideTests` and
`RecoveryContractTests`, reusing that public corpus rather than duplicating a
separate planned `RecoveryGuideTests` file.

Both-provider privacy tests seed connection/path/payload/control canaries and
capture actual results, structured logs, spans/events, metrics, stdout/stderr
and worker output across all seven outcomes. They check retained data and
distinct corrected-retry correlation. Codes/guidance are bounded ASCII,
messages are fixed/bounded, result bytes are capped, and metric labels are
only operation/provider/outcome. No raw provider exception, credential,
private target or module payload belongs in committed evidence.

## Executed verification

Earlier checkpoint failures are historical, not successful final evidence:
the unchanged resumed baseline passed 105/105 and the original PostgreSQL
process-race cases passed 2/2 without a speculative race fix. A later
131-case run passed 130 and exposed a negative fixture assuming EF history
already existed at the native early barrier; the fixture was corrected.
The public full run exposed omitted measured assertions in the older module
examples; those now use the shared real assertion counter. Passing results
below come from actual executed commands.

| Exact command / selection | Result |
| --- | --- |
| `python3 scripts/check_repository.py` | Passed |
| `python3 -m unittest discover -s scripts/tests -v` | Passed, 111 tests after the verified-lease correction |
| `python3 scripts/check_dependencies.py` | Passed, 403 npm / 78 NuGet identities |
| `git diff --check` | Passed |
| `dotnet format Kofte.slnx --no-restore --verbosity quiet` | Passed |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo -v quiet` | Passed, zero warnings/errors |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore -c Release --filter-class '*MaintenanceObserverFailureTests' --filter-class '*CorrectedRetryTests' --filter-class '*AbandonedLockRefusalTests' --filter-class '*MigrationCrashTests' --minimum-expected-tests 11` | Passed, 11/11, zero skipped |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore -c Release --filter-class '*MigrationRecoveryRefusalTests' --filter-class '*AbandonedLockRefusalTests' --filter-class '*MigrationCrashTests' --minimum-expected-tests 19` | Passed, 19/19, zero skipped |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*ModuleContractTests' --filter-class '*StorageGuideTests' --minimum-expected-tests 8` | Passed, 8/8, zero skipped |
| `python3 scripts/verify.py --suite unit` | Passed, 11 named steps |
| `python3 scripts/verify.py --suite integration --provider both --disposable-postgres` | Passed, 7 SQLite and 9 PostgreSQL named steps |
| `python3 scripts/verify.py --suite contract --provider both --disposable-postgres` | Passed, 37 named steps |
| `python3 scripts/verify.py --suite recovery --provider both --disposable-postgres` | Passed, 38 named steps |
| `python3 scripts/verify.py --suite privacy --provider both --disposable-postgres` | Passed, 36 named steps |
| `python3 scripts/verify.py --suite quality` | Passed, 19 named steps, including locked restores/format/build/lint/types/dependencies |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*SqliteStorageGuideTests' --minimum-expected-tests 1` | Passed, 1/1 all-corpus guide, zero skipped |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*PostgresStorageGuideTests' --minimum-expected-tests 1` | Passed, 1/1 all-corpus guide, zero skipped |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore -c Release --filter-class '*RegistrationConcurrencyTests' --filter-method '*TwentyRoundsOfTenSeparateProcesses*' --minimum-expected-tests 4` | Passed, all four actual process-race cases after the atomic verified-probe correction |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore -c Release --filter-class '*PostgresLeaseLossTests' --filter-class '*CorrectedRetryTests' --minimum-expected-tests 6` | Passed, 6/6, zero skipped, including the new ownership/contention probe regression |
| `python3 scripts/verify.py --suite all --disposable-postgres` | Passed, all ten suite reports / 200 steps, `completeGate: true` |
| Assessed-head CI at `92e942e` | Passed, all ten product jobs / 200 report steps and repository checks: eleven contexts |

Existing named suites retain manifest, native host health/OpenAPI/HTTP,
web/browser/axe, startup, valid-v0 recovery, privacy and failure-propagation
steps alongside required storage inventory/provider/scenario checks.
No missing provider or zero-test run is skipped into success. An earlier complete
local gate ran from 14:00:35 to 14:28:08 UTC on 2026-10-22: quality 19, unit 11,
SQLite integration 7, PostgreSQL integration 9, recovery 38, contract 37,
browser 16, privacy 36, startup 9 and failure-propagation 18 steps. Its first
three reports record parent `320d565`; later reports record `b787a48`.
The product source was unchanged across that publication. Subsequent diagnostic
and verified-acquisition corrections and their fresh full gate are detailed below.

The first assessed implementation CI run is
[39792182400](https://github.com/Stonefly-Labs/Kofte/actions/runs/39792182400),
with [repository checks](https://github.com/Stonefly-Labs/Kofte/actions/runs/39792182371)
passing; nine of eleven contexts passed overall. Its checkout merge revision was
`818895dc3400615d8756e1c26a89c680f800a2e9`. Both failing contexts reached the
same storage PostgreSQL test command; the existing wrapper discarded the native
failure identity along with private output. A scoped diagnostic extension now
retains only repository-source-allowlisted test symbols and fixed storage codes,
never theory arguments, raw exceptions, paths or payloads. Focused Python
regressions and the then-full 109-test Python suite passed. This improved
diagnostic evidence, not the underlying failure or that checkpoint's passing status.
Console-only extraction at diagnostic head `933cefa` did not retain a native
failure identity. Native storage commands now also generate the installed
xUnit XML report inside a fresh private root, extract only source-allowlisted
failed type/method pairs and fixed codes, then remove the raw report. The
reporting flags are recorded with the private results path redacted.
Missing, invalid, oversized or unrecognized diagnostic reports expose fixed
diagnostic codes without replacing the original failing command. A real
three-case deliberately unavailable-provider run proved failure preservation,
exact source identity extraction and private report cleanup; it is not a
passing product test. The isolated failure-propagation wrapper revalidates
the same allowlists before carrying those fields into its safe parent report.

Structured evidence at `e30fd60` identifies the Linux failure precisely:
`PostgresRegistrationConcurrencyTests.TwentyRoundsOfTenSeparateProcessesKeepActualOwnershipUnique`,
with `storage_deadline_exceeded`. The harness previously announced readiness
before a cold process had initialized its provider. It now admits each worker
after one nonmutating lease acquire/release, then releases all ten workers at
the shared barrier to race fresh independent lease sessions. Registration
rounds, caller counts, uniqueness/inventory assertions, production budgets
and Busy-only retry rules are unchanged; this is not a cold-start performance
claim. Fixed acquire/register/setup failure phase evidence is also retained.
The first warm-up-only local run still failed during acquisition; it is not
counted as a fix. Inspection found that acquisition performed the ownership
query and first advisory-lock probe as separate commands inside the same
two-second budget, introducing another whole-second timeout floor after the
first query. Those now share one statement: a verified ownership predicate
conditionally executes the first probe; invalid ownership returns no lock.
Later contended probes retain the existing Busy behavior. A real-provider
regression verifies acquisition, contention and invalid-owner refusal while
another connection proves the invalid owner did not retain the lock.
No budget or retry classification is relaxed. The corrected four-case process
matrix and six ownership/session/correction cases pass locally. The fresh
complete local gate at `4eb236a9cfb4196fb385ee7fbff1cf8ffc6017ee` also passes
all ten reports / 200 steps, from 15:03:22 to 15:31:03 UTC on 2026-10-22.
Every report records that same revision and `completeGate: true`.

Nine product CI jobs and repository checks pass at `4eb236a`; recovery was
delayed in browser prerequisites. Two recovery attempts were cancelled after
long-running setup metadata; the second completed setup as cancellation took
effect and had begun native recovery, so it is explicitly not passing evidence.
Its retrieved log identifies very slow HTTP Azure Ubuntu archive downloads
(a 7 MB font took nearly eight minutes), not a product assertion failure.
CI now maps that mirror to the official HTTPS Ubuntu archive, preserving the
configured other mirrors, signed apt verification and the exact existing
Playwright/Chromium prerequisite installation. No product pin or check changes.
The corrected assessed-head product run
[39808405984](https://github.com/Stonefly-Labs/Kofte/actions/runs/39808405984)
now passes all ten jobs. Downloaded safe artifacts contain exactly ten reports
and **200 passing steps**: quality 19, unit 11, SQLite integration 7,
PostgreSQL integration 9, recovery 38, contract 37, browser 16, privacy 36,
startup 9 and failure-propagation 18. These are separate named CI suites
(`completeGate: false` individually), collectively matching the complete local
gate; the local all-suite reports have `completeGate: true`.
[Repository checks 39808405951](https://github.com/Stonefly-Labs/Kofte/actions/runs/39808405951)
also pass at the exact same head, `92e942e`: eleven passing contexts.
Every product report records assessed merge revision
`36d84a5f2942df5781432b6ed5b70521c62041bb`; its actual parents were verified as
main `66f0e4a950bc3590cdb3a25f1622afbd13a0c226` and head
`92e942efcc1e9033f17c7e24f7b20c53c940f9da`.
Observed hosted-runner report metadata is Linux x86_64, four logical CPUs,
16,760,340,480 bytes RAM and Python 3.12.3; the workflow selects Ubuntu 24.04
and the same pinned SDK/Node/npm/provider image. No fixed benchmark profile
or human usability study is inferred. The final 111-test Python suite and
repository/whitespace checks also pass. T076-T078 are complete.

## Requirement, scenario, task and principle mapping

| Requirement | Concrete evidence / tasks |
| --- | --- |
| FR-001 | Versioned pure contracts, bound public consumers, denied foreign factory; T007/T014/T017/T024/T037/T045/T068 |
| FR-002 | Actual no-storage/immutable registration and durable constraints; T010-T013/T016/T019/T030/T033 |
| FR-003 | Allocator/candidate/direct UNIQUE tests and complete independent caller/process matrix; T011-T013/T018/T028-T034 |
| FR-004 | Persisted keys/tokens, restart/metadata retry and original-reservation recovery; T015-T019/T022-T024/T033/T053 |
| FR-005 | PostgreSQL object/role/marker/history inventory and Alpha/Beta isolation; T011/T018/T020/T022/T029/T036-T046 |
| FR-006 | SQLite private existing files, alias/ancestor/sidecar negatives and complete backup; T009/T012/T021/T037/T044/T051/T058 |
| FR-007 | Durable phase kill matrix, truthful uncertainty and explicit recovery; T007/T019-T023/T048/T052-T054/T061 |
| FR-008 | Read-only ready validation, corrupt/missing-target refusal and no fallback; T013/T023/T030/T033/T037/T044/T048-T061 |
| FR-009 | Assigned models/options/cache/snapshots/migrations and independent public modules; T024/T036-T047/T054/T068 |
| FR-010 | Explicit v0/v1 upgrade, history preflight, killed migrations and legacy rollback protection; T010-T013/T025/T036-T043/T048-T063 |
| FR-011 | Shared finite budgets, actual native deadline/lease-loss proof and data-preserving corrected retry; T008/T009/T019/T028-T035/T049/T052-T054/T064-T070 |
| FR-012 | Separate durable registry/physical commits and module-local transactions; T019-T023/T036-T047/T048/T053/T054/T061/T062 |
| FR-013 | Owned least-privilege fixture/configuration, actual provisioner/outage negatives and named gates; T002/T003/T006/T014/T020/T021/T025/T049/T055/T059/T071-T076 |
| FR-014 | Public shape/bounds and captured seven-outcome safe diagnostic channels; T005/T007/T055/T064-T072 |
| FR-015 | Actual complete backups/fresh restores, preserved refusals and recovery-point guidance; T050/T051/T054/T056-T063/T074-T077 |
| FR-016 | Nine-case public corpus, zero-module compatibility and retained complete gates; T005/T006/T017/T025-T027/T035/T045-T047/T060/T063/T068/T071-T078 |

| Acceptance scenarios / success criteria | Evidence |
| --- | --- |
| US1.1-4 / SC-003 | Registration/restart/no-storage/immutable conflict and missing-ready-target tests, public registration corpus; T015-T027/T044 |
| US2.1-4 / SC-002 | All eight race cases, forced occupied/reserved/unowned candidates and direct invalid writes; T028-T035 |
| US3.1-4 / SC-001 | Both independent Alpha/Beta implementations, clean/additive upgrade, model/history/cache and forbidden-target negatives; T036-T047 |
| US4.1-4 / SC-004 | All 31 boundary kills (including the native callback), real budgets/lease loss, upgrade and whole-set restore/refusal; T048-T063 |
| US5.1-2 / SC-005 | Seven-outcome captured-channel privacy, result bounds and retained-data correction; T064-T070, boundary negatives |
| SC-006 | Executable public guides, existing host/manifest/browser gates, complete local gate and passing assessed-head CI; T071-T078 |

These rows cover all 18 acceptance scenarios, 16 FRs and six SCs;
coverage mapping is supported by the executed passing gates above, not file existence.

| Constitution | Evidence / applicability |
| --- | --- |
| I | Köfte prose, optional-module-free bootstrap, no Hoard dependency |
| II | Minimal pure contracts, one adapter, owner-only binding, no loader |
| III | Native DI, reviewed licenses, dedicated test projects; no MediatR/AutoMapper |
| IV | Actual immutable UNIQUE registry, schema/file/history isolation and verified recovery |
| V | Module-local transactions, separate durable phases; no cross-module ACID |
| VI | Least privilege, path/ownership/preflight negatives and bounded safe diagnostics; no sandbox claim |
| VII | No MCP surface; future actor/team/resource authorization is neither introduced nor bypassed |
| VIII | Durable intent/reconciliation only; no job/event/audit runtime or exactly-once claim |
| IX | No new UI journey; existing browser/axe regressions remain mandatory |
| X | Existing supported macOS/Linux targets, explicit configuration/maintenance and fresh complete restore |
| XI | Real providers, public consumers, negative/process/crash/deadline/privacy and full local/CI gates |
| XII | Approved ADR, original feature branch/draft PR, one implement stage; no self-acceptance |

## Supported limits and handoff

Supported module DDL is intentionally narrow: create-table/primary-key and
add-column with int/text columns; unknown broader operations fail closed.
Trusted in-process module/factory code is not sandboxed and must cooperate;
owned-worker supervision provides the demonstrated synchronous maintenance
deadline on supported local infrastructure, not a kernel-blocked-disk promise.

Recovery is stopped, complete-set and fresh-target only. Ambiguous evidence
is preserved and blocked, not adopted or deleted. There is no cross-provider
conversion, automatic down-migration, destructive replacement, general lifecycle
runner or automated global backup product. Choosing an older complete recovery
point remains the operator's explicit data-loss responsibility; UUID/hash
consistency does not authenticate malicious forgery or prove that no newer lost
backup exists.

All 78 tasks are evidenced. Complete local and assessed-head CI gates pass;
this report, task accounting and the driver completion transition are published
together on the original feature branch and draft PR.
The only next stage is **converge**, not performed by this invocation.
The PR stays draft: no feature acceptance or merge is requested here.
