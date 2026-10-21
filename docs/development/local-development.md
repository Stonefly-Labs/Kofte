# Local bootstrap development

**Status:** Spec 001 implementation is complete. Local SQLite/host/web,
owned PostgreSQL verification, guarded store recovery and generated Development
OpenAPI and fixture-owning named verification have final local/CI evidence and
human-confirmed manual tasks. Convergence, final review and explicit human
feature acceptance remain separate, unexecuted stages.
This is not a feature-acceptance or production guide.
See the [implementation evidence](../../specs/001-repository-bootstrap/implementation.md).

## Prerequisites and explicit restore

Supported targets are macOS 15+ arm64 and Ubuntu 24.04 x64. Native Windows is not
promised. Install .NET SDK **10.0.402** (runtime **10.0.13**), Node **24.22.0**
with npm **11.19.1**, and Python 3.11+. The supervisor checks exact product tool
versions; it does not acquire tools or dependencies. See the
[dependency review and retained notices](dependencies.md).

Verify the versions in the same shell that will start the stack:

```bash
dotnet --list-sdks
dotnet --version
node --version
npm --version
```

Installing a pinned tool elsewhere does not select it in this shell. Select its
installation through your version manager or `PATH` first; keep `DOTNET_ROOT`
consistent with a user-local .NET installation. Do not change `global.json` or
the locks to accommodate older tools. `dotnet --list-sdks` works even when the
SDK selected by `global.json` is missing.

From the repository root, explicitly restore the reviewed locks:

```bash
dotnet restore Kofte.slnx --locked-mode
npm ci
```

Browser checks additionally need the pinned Playwright Chromium runtime:

```bash
npm exec --workspace tests/e2e -- playwright install chromium
```

Initial downloads are not a warm startup. For offline use, first restore the
exact locks and browser runtime while online. Do not use forced/legacy peer
resolution, unsupported tool roll-forward or floating package versions.

## Initialize, start, stop and restart

Initialization is separate, explicit and non-overwriting:

```bash
dotnet build Kofte.slnx --no-restore -c Release -warnaserror
dotnet run --project src/Kofte.Host --no-build -c Release -- --store init
python3 scripts/dev.py
```

Open **http://127.0.0.1:3000**. The attached supervisor builds already-restored
host/web assets, starts the host then web, and emits `local_ready` only after the
actual host and fixed web adapter report usable storage. Startup is bounded to
300 seconds. Ctrl-C stops only owned child process groups; existing port
occupants are never killed. Repeat `python3 scripts/dev.py` to restart.

Persistent SQLite data is `data/platform/platform.db`; directories are private
to the current owner and the database is owner read/write only. Normal startup
opens an existing store, never creates missing data, switches providers or resets
it. Stop/retry/restart do not delete data. No application/fixture table, migration
history or registry is shipped. A host-held exclusive operation lock prevents
concurrent initialization; the lock file remains in place after clean shutdown.

## Stopped-host SQLite recovery

Stop the supervisor/host before any backup, restore or reset. These operations
take the same exclusive store lease as the host and refuse an active owner.
Store commands read the process environment, **not `.env`**; explicitly set
`KOFTE_DATA_ROOT` when operating on a non-default root. Never shell-source `.env`.

Back up to a new canonical private destination:

```bash
dotnet run --project src/Kofte.Host --no-build -c Release -- --store backup --output "$(pwd -P)/backups/platform-before-change.db"
```

The provider backup API validates the source and reopens/checks its output.
Keep the resulting file and its adjacent `.metadata.json` together, unchanged.
The metadata records backup-format and source/output digests, not an application
schema version. Existing destinations, non-private paths, links, non-regular
files, sidecars or invalid backup metadata are refused. An interrupted pair
without both valid files is unusable; preserve the original store and choose
a new backup destination rather than treating a partial file as a backup.

Prefer restoring into a new private root while retaining the original:

```bash
KOFTE_DATA_ROOT="$(pwd -P)/artifacts/restored-store" dotnet run --project src/Kofte.Host --no-build -c Release -- --store restore --input "$(pwd -P)/backups/platform-before-change.db"
```

Validate that root before selecting it for normal startup. Replacing an existing
destination additionally requires a **fresh separate backup of that exact
destination** and explicit destructive confirmation:

```bash
dotnet run --project src/Kofte.Host --no-build -c Release -- --store backup --output "$(pwd -P)/backups/current-before-replacement.db"
dotnet run --project src/Kofte.Host --no-build -c Release -- --store restore --input "$(pwd -P)/backups/platform-before-change.db" --previous-backup "$(pwd -P)/backups/current-before-replacement.db" --confirm-replace-platform-data
```

A stale backup does not satisfy that guard. If corruption prevents backing up
the current store, retain it untouched and restore a verified backup into a fresh
root instead. No automatic repair, downgrade or overwrite is attempted.

Reset is **optional and destructive**, never a troubleshooting prerequisite:
`--store reset --confirm-delete-platform-data` deletes only the fixed platform
database and validated SQLite sidecars. It keeps the root and persistent lock;
later creation requires separate explicit init. Unconfirmed/unknown commands
fail nonzero. There are no shipped application tables, schema versions or
migrations to upgrade; future schema-bearing specs owe their own migration and
forward-recovery evidence.

## Configuration and privacy

Optionally copy `.env.example` to ignored `.env`. The supervisor parses values
as data, without shell evaluation/interpolation; explicit environment values win.
Unknown Köfte keys, invalid flags/addresses and unsupported profiles fail.
`KOFTE_DATA_ROOT` must be an absolute canonical path without symlink components;
on macOS, `/private/tmp` is canonical whereas `/tmp` and `/var` are symlink aliases.

Defaults are `local-sqlite`, host `http://127.0.0.1:5080`, web
`http://127.0.0.1:3000`, and a fixed loopback `KOFTE_BACKEND_ORIGIN`.
When changing ports, set host/web to distinct free ports. The backend origin
defaults to the selected host's loopback address and port; an explicit value
must match that listener, not an unrelated local service. Embedded URL credentials, paths,
queries and fragments are refused. Broader binding needs
`KOFTE_ALLOW_UNSAFE_EXPOSURE=true` plus a loopback backend origin; it emits a
warning and remains unsupported for production.
An explicit non-loopback IP host binding also retains its selected loopback
backend listener; wildcard IPv4 bindings require an IPv4 loopback destination.

The supervisor sets Development and disables Next telemetry for its children.
`OTEL_EXPORTER_OTLP_ENDPOINT` is optional trusted-operator configuration; no
collector/cloud service is required. Do not put credentials in command arguments,
browser-public variables or checked-in files. Raw child/framework output is not
forwarded by the supervisor; failures emit safe event/component/correlation
fields, not connection strings, paths or exception dumps.

## Current diagnostics and page

`GET /health/live` is storage-independent. `GET /health/ready` performs a fresh
selected-store probe and returns 200 ready/storage or 503 unavailable/storage.
Health payloads contain only version `1`, state, component and a lowercase
32-hex correlation ID. Responses are no-store. Health body/query input is an
empty 400; unsupported methods are empty 405 and unknown/case-mismatched paths
are empty 404.

`GET /openapi/v1.json` is generated from native endpoint/type metadata with the
strict bootstrap schema, anonymous applicability and no-store response contracts.
It declares API version `1.0.0` and OpenAPI `3.1.1`; it is not a separately served
static copy. All documentation methods are absent outside Development. Generation
parity checks normalize only the actual owned listener and the semantically
equivalent omitted/empty root security list, not paths, schemas or responses.

The browser uses only `/backend/health/ready`. This server-only fixed adapter
forwards no caller credentials/headers and validates status, content type and
payload before returning a minimal response. Its total deadline is five seconds;
invalid/failed upstream data becomes an explicit correlated unavailable 503.
There is no wildcard proxy or HTTP store mutation interface.

The Köfte page has honest bootstrap/no-features text, polite loading/ready/
unavailable/stale status and keyboard retry with stable focus. Attempts expire
after ten seconds including body processing; active-page refresh starts every
30 seconds. Hiding/blurring cancels and marks status stale; returning immediately
revalidates. Retry cannot reset data or start duplicate concurrent requests.
Each manual or automatic attempt schedules the next refresh from its own start;
a retry overlapping the old polling boundary cannot skip a refresh.

## Disposable PostgreSQL verification

Local SQLite startup needs no Docker. PostgreSQL verification needs a local
Unix-socket Docker Engine/Compose and this explicitly acquired immutable image:

```bash
docker pull postgres:17@sha256:a63c1ea3f6a3a5a1621f97d2aba3b30faecfab4224c1b0000cc85ced57bb1939
KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-trait Provider=postgres
```

Tests never pull implicitly or use in-memory PostgreSQL. Each fixture records
unique Compose resource IDs/run labels, a newly created unpredictable database
and role OIDs/ownership comments. The application role has no server-superuser,
database/role-creation or bypass-RLS attributes; it owns only `kofte_system`.
Native readiness checks selected-provider connectivity, ownership and privileges
on fresh connections, without creating a SQLite fallback.

An optional secret `KOFTE_POSTGRES_CONNECTION` may identify a **loopback
PostgreSQL 17 administrative test service** with create-new-database/role rights.
Its existing database is never adopted or dropped; the explicit disposable flag
does not confer ownership. Recovery/outage fixtures still require their own
local pinned-image service and matching image tools, not a shared external
target. Never place connection strings in command arguments, reports or `.env`
files that may be committed.

Fixture-only dump/restore uses matching `pg_dump`/`pg_restore` 17 in the owned
image, private verified backup pairs and a newly owned empty database. A host
lease, exclusive advisory operation lease and connection admission guard prevent
concurrent use. Existing/nonempty targets and corrupt/incompatible backups are
refused, not replaced. Cleanup revalidates exact database/role/container/volume/
network ownership; missing proof preserves resources and fails explicitly.
These are verification fixtures, not production database administration commands.

## Focused checks currently available

Run the repository checks and the smallest affected product commands:

```bash
python3 scripts/check_repository.py
python3 -m unittest discover -s scripts/tests -v
git diff --check
dotnet format Kofte.slnx --verify-no-changes --no-restore
dotnet build Kofte.slnx --no-restore -c Release -warnaserror
dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-trait Provider=sqlite
npm run format:check
npm run lint --workspaces --if-present
npm run typecheck --workspaces --if-present
npm run contract:check
npm run test:unit --workspace packages/bootstrap-contract
npm run test:unit --workspace apps/web
npm run build --workspace apps/web
npm run test --workspace tests/e2e -- startup.spec.ts
```

.NET test source lives physically inside its test project, not in production
vertical slices or linked from production folders. The integration tests use
owned private roots/ports and include a real child host. Shared contract
generation is explicit:
`npm run generate --workspace packages/bootstrap-contract`.

The startup test owns two private built-asset host/web cycles, unpredictable
test-only fixture values, free ports and process groups. It verifies exact
fixture persistence and released ports after each stop. Individual readiness/
navigation durations and boolean fixture comparisons are retained in ignored
`tests/e2e/test-results/` JSON, without fixture values or local paths.
`BOOTSTRAP_WARM_STARTS=10` selects ten cycles and the wrapper defaults to ten.
Each sample must have readiness within the 285-second work budget, usable page
within two seconds and exact fixture persistence. Passing on a different machine
does not change the required reference environment.
Ordinary macOS results do not establish Ubuntu reference timing or contributor
acceptance.

## Named verification suites

Prefer the wrapper below to invoking runtime npm tests directly: those tests
require an owned host/web pair, private control directory/run ID and, for privacy,
seeded canaries. They must not target production or developer data.

```bash
python3 scripts/verify.py --suite quality
python3 scripts/verify.py --suite unit
python3 scripts/verify.py --suite integration --provider sqlite
python3 scripts/verify.py --suite integration --provider postgres --disposable-postgres
python3 scripts/verify.py --suite recovery --provider both --disposable-postgres
python3 scripts/verify.py --suite contract --provider both --disposable-postgres
python3 scripts/verify.py --suite browser
python3 scripts/verify.py --suite privacy --provider both --disposable-postgres
python3 scripts/verify.py --suite startup --warm-starts 10
python3 scripts/verify.py --suite failure-propagation --disposable-postgres
python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10
```

All first enforce exact tool versions and required workspace scripts. Quality
runs every ordered child command in the spec's
[quickstart](../../specs/001-repository-bootstrap/quickstart.md#required-local-and-ci-verification),
including repository/Python/whitespace, locked restores, .NET format/build,
Prettier/ESLint/TypeScript, generated contract, workspace builds and dependency
inventory. The remaining exact test selectors are:

| Suite | Underlying runner |
| --- | --- |
| unit | `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --minimum-expected-tests 1`; web and shared-contract `test:unit`; complete Python discovery |
| integration, each provider | `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-trait Provider=PROVIDER --minimum-expected-tests 1` |
| recovery, each provider | Same integration project with `--filter-query "/[(Suite=recovery)&(Provider=PROVIDER)]" --minimum-expected-tests 1`, then `npm run test --workspace tests/e2e -- recovery.spec.ts --workers=1 --output PRIVATE_DIRECTORY` |
| contract, each provider | Same integration project with `--filter-class "*OpenApiTests" --filter-trait Provider=PROVIDER --minimum-expected-tests 1`, then `npm run test --workspace tests/contract -- --no-file-parallelism bootstrap.test.ts adapter.test.ts` |
| browser | `npm run test --workspace tests/e2e -- accessibility.spec.ts bootstrap.spec.ts recovery.spec.ts` against owned SQLite; one configured worker |
| privacy, each provider | `npm run test --workspace tests/contract -- --no-file-parallelism privacy.test.ts`; E2E `privacy.spec.ts --workers=1 --output PRIVATE_DIRECTORY`; then `python3 scripts/run_python_tests.py --pattern test_dev_output.py` |
| startup | `npm run test --workspace tests/e2e -- startup.spec.ts --output tests/e2e/test-results/verify-startup-UUID`, with `BOOTSTRAP_WARM_STARTS` set to the requested 2-10 cycles |
| failure-propagation | `tests/integration/tooling/test_failure_propagation.py` exercises the actual wrapper in an isolated no-hardlinks checkout, never the working checkout |

`PROVIDER` is the requested `sqlite` or `postgres`, never a substitute fallback.
Each owned runtime builds the Release solution and web first, then launches
the native host DLL and local `next start` on private roots/free loopback ports.
Test-only filesystem controls stop/restart only owned processes, hold/restore
the exact SQLite file or pause/resume the exact owned PG service. There is no
deployed fault/admin HTTP route. Runtime buffers are transient and scanned for
seeded credentials, paths and stacks before disposal; capture/scanner/control/
cleanup failures fail the suite. Only safe named results are persisted.

The required script inventory is explicit, so `--if-present` cannot silently
remove coverage:

| Workspace | Required scripts |
| --- | --- |
| root | `format:check`, `contract:check`, `dependencies:check` |
| `apps/web` | `build`, `lint`, `typecheck`, `test:unit`, `start` |
| `packages/bootstrap-contract` | `generate`, `build`, `lint`, `typecheck`, `test:unit` |
| `tests/contract`, `tests/e2e` | `lint`, `typecheck`, `test` |

Safe ignored reports under `artifacts/verification/` retain revision, environment,
tool versions, start/end/duration, suite, exact commands, outcomes and safe
failure references. Environment evidence includes actual physical `memoryBytes`
alongside OS/architecture/CPU count; unavailable/invalid measurements fail
instead of supplying a made-up reference size. Failed browser commands may
retain only whitelisted source basenames and positive line/column locations;
dependency failures retain an allowlisted checker code, never raw messages,
configuration or stack paths. Startup reports also retain a validated `startup-observations`
step with the exact configured cycles and each numeric readiness/page duration
and boolean fixture comparison; missing/malformed/linked samples fail rather
than becoming fabricated timing evidence. A targeted pass remains targeted.
Only a successful `all`
run can set `completeGate: true`; this denotes automated coverage, not human
acceptance. The ten isolated Ubuntu 24.04 CI jobs invoke the same named suites,
with pinned actions/toolchain/image, explicit Chromium acquisition, read-only
permissions and only safe verification JSON uploaded. A CI job is not claimed
green until its actual run is inspected.

Failure propagation first requires green quality/PG/browser baselines, then
independently injects failing unittest, formatting and build changes, unavailable
loopback PG and missing Chromium. It requires the exact named nonzero stage
for each, preserves sanitized reports, isolates HOME/XDG directories and cleans
only its owned clone. A missing baseline prerequisite is a failure, not a skip.

## Troubleshooting and outstanding work

`tool_missing` / `tool_version_invalid`: the safe `component` names the failed
product tool (`dotnet`, `node` or `npm`). Install/select the exact baseline in
the current shell using the version checks above, then restore explicitly.
No raw tool output or installation paths are logged. `port_unavailable`: select distinct free ports or stop the
known owner yourself; the supervisor will not take it over. `build_failed`:
run the named host/web build directly to diagnose the local code/tool issue,
keeping any raw output out of reports. `storage_missing` / `storage_invalid`:
check the selected canonical root, ownership and permissions. Initialize only an
absent store; existing/corrupt data is never overwritten. `store_busy`: stop the
known host/operation before retrying. An unknown operation fails `command_invalid`.

Focused checks do not establish the all-suite/CI gates. Do not delete data to
bypass a failure. Human WCAG review, both supported OS evidence, the ten-start
Ubuntu reference exercise and
unfamiliar-contributor evidence remain required. Automated browser/axe results
are not manual accessibility acceptance. Authentication, modules, MCP and
Kubernetes/production deployment belong to later specifications.

The human has confirmed T056/T059 complete; the recorded attestation and actual
automated/reference limitations are in the implementation report. This does not
replace later feature review/acceptance or make ordinary CI a reference machine.
