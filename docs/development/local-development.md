# Local bootstrap development

**Status:** Working SQLite/host/web increment of spec 001. Implementation is
still in progress; this is not a feature-acceptance or production guide.
See the [implementation evidence](../../specs/001-repository-bootstrap/implementation.md).

## Prerequisites and explicit restore

Supported targets are macOS 15+ arm64 and Ubuntu 24.04 x64. Native Windows is not
promised. Install .NET SDK **10.0.402** (runtime **10.0.13**), Node **24.22.0**
with npm **11.19.1**, and Python 3.11+. The supervisor checks exact product tool
versions; it does not acquire tools or dependencies. See the
[dependency review and retained notices](dependencies.md).

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

## Focused checks currently available

Run the repository checks and the smallest affected product commands:

```bash
python3 scripts/check_repository.py
python3 -m unittest discover -s scripts/tests -v
git diff --check
dotnet format Kofte.slnx --verify-no-changes --no-restore
dotnet build Kofte.slnx --no-restore -c Release -warnaserror
dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release
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
`BOOTSTRAP_WARM_STARTS=10` selects ten cycles for the later reference exercise;
ordinary macOS results do not establish Ubuntu reference timing or contributor
acceptance.

The other initial npm HTTP/browser tests require an already-running **isolated**
host/web pair with `KOFTE_HOST_URL` and `KOFTE_WEB_URL` set for those processes.
Do not aim them at production or developer data. The full fixture-owning
verification runner is still outstanding; these commands are not that full gate.

## Troubleshooting and outstanding work

`tool_missing` / `tool_version_invalid`: install/select the exact baseline, then
restore explicitly. `port_unavailable`: select distinct free ports or stop the
known owner yourself; the supervisor will not take it over. `build_failed`:
run the named host/web build directly to diagnose the local code/tool issue,
keeping any raw output out of reports. `storage_missing` / `storage_invalid`:
check the selected canonical root, ownership and permissions. Initialize only an
absent store; existing/corrupt data is never overwritten. `store_busy`: stop the
known host/operation before retrying. An unknown operation fails `command_invalid`.

PostgreSQL, guarded backup/restore/reset, generated Development OpenAPI, complete
contract/privacy/failure-propagation suites, the named verification runner and
product CI are **not complete yet**. Do not run planned recovery/verification
commands as if they existed or delete data to bypass a failure. Human WCAG
review, both supported OS evidence, the ten-start Ubuntu reference exercise and
unfamiliar-contributor evidence remain required. Automated browser/axe results
are not manual accessibility acceptance. Authentication, modules, MCP and
Kubernetes/production deployment belong to later specifications.
