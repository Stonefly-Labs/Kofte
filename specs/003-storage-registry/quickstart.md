# Validation Guide: 003 Storage Registry

**Status**: Executable guide for the completed implementation stage, not feature
acceptance. [ADR 0005](../../docs/adr/0005-module-storage-ownership.md) is
human-approved and its dependency graph is reviewed. The
[implementation report](implementation.md) identifies actual executed commands
and passing local/CI evidence. All nine public examples, actual interrupted
provisioning and stopped provider-backed restore now execute. Only recorded
results count as evidence; the guide does not grant feature acceptance.
Existing startup/recovery guidance remains in the
[local guide](../../docs/development/local-development.md).

## Executed registry checks

After selecting the pinned SDK and restoring locked dependencies, the focused
registry suite runs actual SQLite and explicitly owned disposable PostgreSQL:

```bash
export KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true
dotnet build tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj --no-restore -c Release
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore -c Release --filter-class '*SqliteRegistrationTests' --filter-class '*PostgresRegistrationTests' --filter-class '*RegistryConstraintTests' --filter-class '*RegistryFormatTests' --minimum-expected-tests 1
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore -c Release --filter-class '*RegistrationConcurrencyTests' --minimum-expected-tests 8
```

The race selector runs all 20-by-ten same/distinct-ID rounds on both providers,
with independent connections and barrier-started independent processes.
It checks actual rows, schema/file inventories and owner markers after each
round. Provider unavailability is a failure, not a skip. These focused checks
do not replace the complete named/full/CI gates.

## Prerequisites

Use the existing supported macOS 15+ arm64 or Ubuntu 24.04 x64 environment,
.NET SDK 10.0.402/runtime 10.0.13, Node 24.22.0/npm 11.19.1 and Python 3.11+.
PostgreSQL evidence uses only the existing explicitly owned disposable
PostgreSQL 17 fixture/image; Docker is not required for SQLite.
[ADR 0005](../../docs/adr/0005-module-storage-ownership.md) has explicit
human approval and the EF graph/licenses/provenance/notices are reviewed.
Use the committed exact locks; do not change pins to accommodate an older shell.

Restore the reviewed dependencies explicitly:

```bash
dotnet restore Kofte.slnx --locked-mode
npm ci
dotnet build Kofte.slnx --no-restore -c Release -warnaserror
```

## Bootstrap upgrade and zero-module compatibility

In a new canonical private local data root with the host stopped, initialize
the existing table-free bootstrap store, then explicitly upgrade it:

```bash
dotnet run --project src/Kofte.Host --no-build -c Release -- --store init
dotnet run --project src/Kofte.Host --no-build -c Release -- --registry upgrade
python3 scripts/dev.py
```

Expected: v0-to-v1 transaction establishes registry/history/installation ID;
repeated upgrade is idempotent, no module store appears, and existing host/web/
health behavior works. Stop the supervisor before further maintenance. A running
host causes bounded busy, not forced shutdown. An untouched valid v0 store must
still support the zero-module bootstrap without automatic migration.

The CLI retains the `registry_upgraded` success summary and emits a separate
safe upgrade record containing provider, stable code/outcome, generated
correlation, fixed guidance and conservative durability. Raw worker output is
discarded. Direct `UpgradeAsync` keeps its `Task`/safe-exception contract and
null module identity; its records distinguish committed v0 upgrade from
unchanged v1 retry and preserve possible-commit uncertainty on failure.
See [diagnostic semantics](contracts/storage-v1.md#outcomes-and-diagnostics).

PostgreSQL initialization uses the fixture-owned database/runtime role plus a
distinct offline provisioner supplied privately to the maintenance child.
Never paste credentials into source, CLI arguments, logs or the PR. The normal
host must retain its negative database CREATE/role privilege checks and must
not receive the elevated setting. Missing/denied provisioning credentials return
an explicit failure; fixing them permits retry against the same reservation.

## Independent public consumer examples

The console belongs to `tests/contract`, not the product runtime.
Its harness creates explicitly owned fixtures and passes private installation
configuration through the child environment. The console itself accepts only
named scenario/provider selectors, not arbitrary targets. Running it without
that owned configuration deliberately fails. The parent rejects missing
scenarios, unavailable providers, zero assertions or mismatched outcomes.

The implemented registration examples can be invoked without copying any
credential or absolute target into command arguments:

```bash
dotnet build tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj --no-restore -c Release
dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*SqliteRegistrationContractTests' --minimum-expected-tests 4
KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*PostgresRegistrationContractTests' --minimum-expected-tests 4
```

These four cases per provider are no-storage, same-ID retry with immutable-need
conflict and three fresh processes, corrected privilege/outage retry, and both
registered-owner collision and occupied/unowned-target preservation.
The retry case uses public Alpha/Beta contexts to write distinguishable sentinels,
releases maintenance before each child launch, and changes display/release metadata
on each restart. Every child re-registers/resolves both exact assignments and
reads both original records without reinitializing or rewriting them.
Execute the complete `all` corpus through fresh owned fixtures:

```bash
dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*SqliteStorageGuideTests' --minimum-expected-tests 1
KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*PostgresStorageGuideTests' --minimum-expected-tests 1
```

These invoke `Kofte.Storage.Consumer --provider sqlite --scenario all` and
`Kofte.Storage.Consumer --provider postgres --scenario all` with privately
injected owned configuration. Do not copy fixture credentials or targets into
arguments. Each consumer supervises an admitted maintenance child, rejects
nonzero exit/invalid output, and returns bounded JSON with measured per-scenario
assertion counts. Its bounded `observations` result additionally requires three
distinct restarted processes, six exact-assignment comparisons and six retained
sentinel reads. Collision observations require the real registered-owner conflict,
exact assignment, retained data, unchanged complete registry/physical inventory,
no alternate/shared contender allocation and separate unowned-target preservation.
The forced candidate is arranged only by the test-support fixture through the
existing internal seam; public module contexts gain no internal or physical
override. Missing or incomplete observations fail standalone and `all`, even
with positive assertion totals. `all` requires all seven actual diagnostic outcomes and every
named case in
[storage-v1](contracts/storage-v1.md#required-public-examples), never skip
negative/recovery examples as optional. Missing configuration, Docker or
provider availability is a failure, not a skip.

Trusted composition uses the same installation instance for the registry and
maintenance scope. The executable module examples under
`tests/contract/Kofte.Storage.Consumer/Modules/` supply provider-specific
contexts, assignment-aware migrations and bound snapshots. Their pattern is:

```csharp
var registry = new StorageRegistry(installation);
var maintenance = new StorageMaintenance(installation);
await using var scope = await maintenance.AcquireAsync(token);
await maintenance.UpgradeAsync(scope, token);
var registered = await registry.RegisterAsync(description, scope, token);
if (registered is not StorageResult.Ready) throw new InvalidOperationException("storage_not_ready");
await using var binding = await registry.BindAsync(description.Id, token);
var migrated = await binding.MigrateAsync(AlphaStorage.Create, scope, cancellationToken: token);
if (migrated is not StorageResult.Ready) throw new InvalidOperationException("storage_not_current");
await using var context = await binding.CreateContextAsync(AlphaStorage.Create, token);
```

Use the existing sample's `AlphaStorage.Create` and description, not a copied
assignment as a capability. Supported DDL is create-table/primary-key and
add-column with int/text columns. Unrecognized operations, raw SQL migrations,
transaction suppression and unbound/foreign snapshots are rejected before any
pending migration SQL. Ordinary module transactions remain module-local.

| Scenario | Expected observed result |
| --- | --- |
| Clean install/no storage | Alpha/Beta have separate schema/file and history, matching markers and distinct sentinels; no-storage creates no store |
| Same-ID retry | Display/release changes and three restarts preserve exact stored assignment/data |
| Concurrency/collision | 20 x ten same/distinct-ID attempts across independent callers/processes; bounded busy may retry, final ownership unique; forced collision/direct duplicate writes fail |
| Boundary rejection | Traversal, links/aliases/sidecars, absolute/foreign target, forged/replayed binding and missing/corrupt store cause no outside-target writes/empty-store success |
| Isolated upgrade | Alpha-only additive migration preserves rows; Beta/platform unchanged; actual model/history and migration operations stay assigned |
| Interrupted provisioning | Every durable crash phase recovers same allocation or returns explicit preserved reconciliation block; ambiguous SQLite pre-marker file is not adopted/deleted |
| Corrected retry/privacy | Fix outage/privilege failure without reset; bounded truthful diagnostics reveal no seeded credential/private path/payload/control characters |
| Stopped restore | Complete set restores matching tokens/history/sentinels; mixed/stale set and registry-only reset/restore are refused |

Both public contract test consumers must independently exercise the result/
binding contract, not reach through internal test seams to manufacture success.
Forced candidate and crash seams are test-only infrastructure controls.

## Named verification

Use the existing named suites and their fixture ownership; do not introduce
a competing runner or manually point tests at a developer's database:

```bash
python3 scripts/verify.py --suite unit
python3 scripts/verify.py --suite integration --provider both --disposable-postgres
python3 scripts/verify.py --suite contract --provider both --disposable-postgres
python3 scripts/verify.py --suite recovery --provider both --disposable-postgres
python3 scripts/verify.py --suite privacy --provider both --disposable-postgres
python3 scripts/verify.py --suite quality
python3 scripts/verify.py --suite all --disposable-postgres
```

Expected: required new projects/cases execute locally and in CI with actual
real-provider assertions. Missing Docker/provider/project or zero tests is a
failure, never a skip-shaped pass. Keep the existing manifest, host/OpenAPI,
web/browser, startup and failure-propagation regressions. Before final feature
acceptance, run the existing complete `--suite all` gate and inspect assessed-head
CI; focused checks during implementation do not replace it.

## Recovery exercise

Use only stopped, owned fixtures and the procedure in
[migrations-and-recovery](contracts/migrations-and-recovery.md#backuprestore-and-legacy-command-guard).
The consumer's `stopped-restore` case executes that actual procedure,
including provider backup/dump, a complete inventory/hash manifest, fresh-target
restore, runtime privilege checks and subsequent reads. Handwritten manifest
constants or restoring only a registry file do not prove it.

For SQLite preserve platform plus every module file/marker/history and reject
unsafe sidecars/links. For PostgreSQL preserve the entire database recovery set,
role ownership/grants and fixture target validation. Do not imply an installation
transaction, automatic backup product or safe live snapshot.

Test legacy `--store backup`, `restore` and confirmed `reset` as refusal cases
once registry/module evidence exists; verify every original sentinel and mapping
is unchanged. Those commands retain their v0 behavior only. A missing platform
file alongside module stores cannot justify reset/reinitialization.

For an interrupted migration, do not delete module data, a hot journal or
arbitrary EF lock rows. Obtain exclusive installation maintenance, verify the
same assignment/history, then exercise only the documented owner-bound abandoned
lock recovery or forward repair/full-set restoration:

```csharp
await using var scope = await maintenance.AcquireAsync(token);
var recovered = await ModuleMigrationRecovery.RecoverAsync(
    registry, description.Id, AlphaStorage.Create, scope, token);
if (recovered is not StorageResult.Ready) throw new InvalidOperationException("storage_recovery_blocked");
```

Run supported synchronous SQLite maintenance in an admitted owned child using
`StorageMaintenanceProcess.RunAsync` and `AdmitAsync`, as the executable
consumer does. The supervisor owns the child until completion or targeted
kill/reap, within 120 seconds including two seconds reserved for cleanup.
Host registry upgrade uses the same supervision with a 30-second total.
Scope acquisition is bounded to two seconds; ordinary resolution/open has
one shared three-second budget. Arbitrary trusted user code is not sandboxed.

Recovery checks the original intent, known applied prefix, physical tables,
columns/types/nullability/defaults, primary keys and unsupported objects before
clearing a recognized assigned-store lock. Unknown history, lock shape/rows
or partial effects are preserved and blocked. Read-only physical verification
does not silently consume an ambiguous SQLite rollback journal.
Unknown/newer registry
formats block startup use rather than downgrade themselves. Restore is
fresh-target-only: existing targets, stale/mixed manifests and registry-only
legacy replacement are refused. An intentionally older complete recovery point
is the operator's explicit data-loss responsibility; UUIDs/hashes cannot detect
malicious forgery or an unknown newer lost backup.

## Evidence to retain

Record actual source revision; SDK/runtime, OS/architecture, CPU/memory and
available runtime conditions; named case counts and elapsed per-operation
deadlines; provider/image identity; exact commands/results; final registry,
schema/file/history isolation and sentinel assertions; crash-phase outcomes;
sanitized log/span/metric captures and executable guide/recovery coverage.
Never retain credentials, private absolute paths, module payloads or raw
provider exception dumps in reports.

Repository gates for each changed stage remain:

```bash
python3 scripts/check_repository.py
python3 -m unittest discover -s scripts/tests -v
git diff --check
```

The implementation report records actual results separately from these
instructions. Passing examples are not convergence, human acceptance or
production-readiness evidence.
