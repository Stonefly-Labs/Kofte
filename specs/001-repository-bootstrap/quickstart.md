# Quickstart Validation Guide: 001

**Status**: Approved full validation target; the local SQLite/host/web increment
now runs, but the complete named verification/provider/recovery/CI gate remains
under implementation. Use the [current local guide](../../docs/development/local-development.md)
for available commands and [implementation evidence](implementation.md) for actual
results. No commands were run as product verification during planning.

## Prerequisites and explicit setup

Supported targets: Ubuntu 24.04 x64 and macOS 15+ arm64. Install Git,
Python 3.11+, .NET SDK **10.0.402** (runtime 10.0.13), Node **24.22.0** with npm
**11.19.1**. PostgreSQL suites additionally require a working compatible
Engine/Compose installation, the pinned PostgreSQL 17 image, and matching
`pg_dump`/`pg_restore` available through that test image. Docker Desktop is
optional and subject to its own entitlement. Browser suites require the pinned
Playwright Chromium and OS libraries.

On a fresh checkout, follow `docs/development/local-development.md`:

```bash
dotnet --version
node --version
npm --version
dotnet restore Kofte.slnx --locked-mode
npm ci
npm exec --workspace tests/e2e -- playwright install chromium
dotnet run --project src/Kofte.Host --no-restore -- --store init
python3 scripts/dev.py
```

Expected: explicit initialization creates only the canonical platform SQLite
file, never overwriting an existing store. The final **single start command**
builds/starts both services, stays attached, and reports actual readiness within
5 minutes. Open `http://127.0.0.1:3000`: Köfte, bootstrap-only/empty state, current
host readiness and accessible retry. No module, login or cloud setup is needed.
Initial dependency/browser downloads are setup, excluded from warm-start timing.

Inspect host contracts:

```bash
curl --fail http://127.0.0.1:5080/health/live
curl --fail http://127.0.0.1:5080/health/ready
curl --fail http://127.0.0.1:5080/openapi/v1.json
```

Expected: 200 responses matching [bootstrap.openapi.json](contracts/bootstrap.openapi.json),
versioned minimal state/component/correlation, no-store and no sensitive details.
OpenAPI is present only in Development. Ctrl-C stops owned processes; repeating
`python3 scripts/dev.py` preserves the store. No automatic reset or restore.

## Required local and CI verification

Stop the manual dev session first. The runner provisions isolated roots, free
ports and uniquely owned test databases/projects; no suite uses developer data.
For all required product evidence:

```bash
python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10
```

That command includes the repository checks and every named suite below.
Missing tools, provider service or browser must fail, not skip.
Targeted commands isolate a failure; their success is not the complete gate:

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
```

The quality runner must identify at least these child commands/results:

```bash
python3 scripts/check_repository.py
python3 -m unittest discover -s scripts/tests -v
git diff --check
dotnet restore Kofte.slnx --locked-mode
dotnet format Kofte.slnx --verify-no-changes --no-restore
dotnet build Kofte.slnx --no-restore -c Release -warnaserror
npm ci
npm run format:check
npm run lint --workspaces --if-present
npm run typecheck --workspaces --if-present
npm run contract:check
npm run build --workspaces --if-present
npm run dependencies:check
```

The workspace command inventory is validated: `--if-present` accommodates
non-app workspaces, not missing mandatory web/test/package checks. The runner
must fail if a required workspace script is absent. `dependencies:check` checks
the pinned direct/transitive license/provenance inventory, retained notices and
adoption findings, not just declared direct package metadata.
The unit suite includes `dotnet test` on `tests/unit/Kofte.Host.Tests` in Release,
frontend `npm run test:unit --workspace apps/web`, and focused supervisor/runner
Python tests. Integration uses `tests/integration/Kofte.Host.IntegrationTests`;
contract and browser use their named npm workspaces. The implementation records
their exact compatible test-runner arguments and results in reports/CI.

## Acceptance exercises

| Exercise | Procedure and expected evidence |
| --- | --- |
| Fresh local setup / SC-001 | A contributor follows only the guide; after restored prerequisites one command reaches real ready host/web within 300s on the reference machine. Record revision, CPU/RAM, OS, tools, command and elapsed time |
| Ten warm starts / SC-002 | Startup suite retains its private fixture across 10 stop/restart cycles; each ready-page navigation is usable within 2s. Use built web assets for timing; retain individual results, not only an average |
| Outage / SC-004 | Provider fixture interrupts database availability while host remains running. `/health/live` stays 200; readiness becomes 503; visible page stops reporting healthy within 40s. Restore dependency and retry without reinstall/reset |
| Slow/invalid response / SC-004 | Browser test intercepts its fixed readiness request to hang or supply malformed data; attempt ends within 10s, shows safe unavailable state and allows retry. Test superseded responses and stale visibility return |
| Provider integrity / SC-006 | Write/read private fixture on both actual providers, restart/reconnect, backup/restore into isolated targets and compare payload. Lock/unwritable/corrupt/missing targets and invalid credentials fail safely; no fallback/recreation |
| Data containment / FR-007/016 | Traversal/symlink tests create an outside sentinel and prove it unchanged; disposable-target refusal and reset/restore confirmation tests preserve pre-existing data. Cleanup affects only run-owned resources |
| Contract / US4.1 | Compare generated OpenAPI and runtime outcomes with the versioned baseline. Non-Development docs absent; query/body/method/unknown/admin/test/MCP routes do not reveal data or authorize action |
| Privacy / SC-006 | Seed credential canaries into configuration/request/failure paths, capture web/HTTP/framework/host/supervisor outputs and scan for zero canaries, credentials, paths and stack dumps. Retain safe correlated error codes |
| Accessibility / SC-005 | Playwright/axe at 320/768/1440 widths: zero serious/critical findings, no overflow. Human keyboard/focus/announcements, contrast/reflow and applicable WCAG AA review still required; no fabricated manual pass |
| Gate failure / SC-003 | In an isolated temporary checkout, introduce test, formatting and build failures one at a time, and remove required PG/browser availability. Wrapper and CI commands must each fail with the named stage |

Timing reference: Ubuntu 24.04 x64, 4 logical cores, 8 GiB RAM, SSD, loopback,
restored/built dependencies and no unrelated load. Record actual measured machine
details and limitations; CI runner variability does not justify a false pass.

## Safe recovery and local-only limits

Missing tools/ports: repair the named prerequisite/conflict; the runner never
terminates an unknown process. After failure, retry without deleting the store.
For a bad provider setting, correct that setting; no SQLite fallback is allowed.
Locked/inaccessible/corrupt stores remain untouched until their cause is resolved
or a verified backup is explicitly restored.

For SQLite backup with the host stopped, the planned CLI is:

```bash
dotnet run --project src/Kofte.Host --no-build -- --store backup --output artifacts/backups/platform.db
```

Restore uses `--store restore --input artifacts/backups/platform.db` and requires
an empty destination, or explicit destructive replacement confirmation after a
backup. Reset is **separate and destructive**:
`--store reset --confirm-delete-platform-data`; ordinary start/stop/retry never
executes it. Test these guards using temporary roots, never the developer root.
PostgreSQL verification backup/restore is harness-owned and uses fresh databases;
this is not a production database administration/upgrade guide.

Default services bind loopback and expose no business data. Broader bindings
require explicit opt-in and are unsupported for production. No authentication,
module isolation, MCP, production/Kubernetes readiness or MVP completion is
proved by these bootstrap exercises. Record actual implementation outcomes and
limitations in `implementation.md`; review and explicit acceptance happen later.
