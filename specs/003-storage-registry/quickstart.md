# Future Validation Guide: 003 Storage Registry

**Status**: Planning artifact, not runnable new product functionality today.
Do not run the proposed registry commands/consumer until implementation adds
them. Current startup/recovery remains in the [local guide](../../docs/development/local-development.md).
This guide is a validation/run guide, not implementation code or proof of a
manual usability exercise.

## Prerequisites

Use the existing supported macOS 15+ arm64 or Ubuntu 24.04 x64 environment,
.NET SDK 10.0.402/runtime 10.0.13, Node 24.22.0/npm 11.19.1 and Python 3.11+.
PostgreSQL evidence uses only the existing explicitly owned disposable
PostgreSQL 17 fixture/image; Docker is not required for SQLite. Review/approve
[ADR 0005](../../docs/adr/0005-module-storage-ownership.md) before implementation.
Resolve the candidate EF graph, licenses/provenance/notices and committed locks
before restoring/adopting it. Do not install tooling just to execute this plan.

Once implemented and dependencies reviewed:

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

PostgreSQL initialization uses the fixture-owned database/runtime role plus a
distinct offline provisioner supplied privately to the maintenance child.
Never paste credentials into source, CLI arguments, logs or the PR. The normal
host must retain its negative database CREATE/role privilege checks and must
not receive the elevated setting. Missing/denied provisioning credentials return
an explicit failure; fixing them permits retry against the same reservation.

## Independent public consumer examples

The proposed console below belongs to `tests/contract`, not the product runtime.
It creates only owned fixtures, accepts scenario/provider selectors rather than
arbitrary storage targets, validates documented results and exits nonzero on a
missing scenario, unavailable provider, zero assertions or mismatched outcome.

```bash
dotnet run --project tests/contract/Kofte.Storage.Consumer --no-build -c Release -- --provider sqlite --scenario all
dotnet run --project tests/contract/Kofte.Storage.Consumer --no-build -c Release -- --provider postgres --scenario all
```

`all` must execute each named case in
[storage-v1](contracts/storage-v1.md#required-public-examples), not skip the
negative/recovery examples as optional.

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

Extend the existing named suites and their fixture ownership; do not introduce
a competing runner or manually point tests at a developer's database:

```bash
python3 scripts/verify.py --suite unit
python3 scripts/verify.py --suite integration --provider both
python3 scripts/verify.py --suite contract --provider both
python3 scripts/verify.py --suite recovery --provider both
python3 scripts/verify.py --suite privacy --provider both
python3 scripts/verify.py --suite quality
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
The consumer's `stopped-restore` case must execute that actual procedure,
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
lock recovery or forward repair/full-set restoration. Unknown/newer registry
formats block startup use rather than downgrade themselves.

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

At this planning checkpoint only the repository/documentation gates apply.
No new provider, migration, runtime deadline, recovery or acceptance evidence
is claimed merely because this guide describes it.
