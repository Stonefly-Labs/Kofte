# Local bootstrap development

**Status:** Spec 001 implementation is complete. Local SQLite/host/web,
owned PostgreSQL verification, guarded store recovery and generated Development
OpenAPI and fixture-owning named verification have final local/CI evidence and
human-confirmed manual tasks.
[Convergence](../../specs/001-repository-bootstrap/convergence.md) passed with
no actionable gaps or appended tasks.
[Final review](../../specs/001-repository-bootstrap/review.md) also passes with
no acceptance defect and a fresh complete 170-step local gate reconciled with
actual assessed-head CI.
[Explicit human acceptance](../../specs/001-repository-bootstrap/acceptance.md)
is now recorded for spec 001's bootstrap scope and limits. Its original
[PR #4](https://github.com/Stonefly-Labs/Kofte/pull/4) is human-merged.
Spec 002's [author guide](../../specs/002-platform-contracts/quickstart.md)
now runs the pure manifest validator and test-owned console, offline after
restore. Its implementation stage is complete, not feature acceptance.
The subsequent [convergence](../../specs/002-platform-contracts/convergence.md)
public/repeated-corpus gap is now addressed by the
[T049 implementation increment](../../specs/002-platform-contracts/implementation.md#convergence-remediation-t049).
Documented limits, reversed ranges and ordered multi-error/truncation cases
now run 100 times per public consumer in both real cultures, with corrected
retry retained. Focused quality/unit/both-provider contract gates pass.
Follow-up [convergence](../../specs/002-platform-contracts/convergence.md) now
passes with zero actionable gaps and no task/product change. Fresh named unit
and both-provider contract suites pass all 44 targeted steps, including the
complete repeated corpus and actual SQLite/PostgreSQL HTTP/OpenAPI/adapter
fixtures. The [final review](../../specs/002-platform-contracts/review.md)
now passes with no acceptance defect: actual source/tests cover all 35
requirements/outcomes/scenarios, twelve plan decisions, 49 tasks and twelve
principles. Fresh quality/unit/both-provider contract checks pass 62 targeted
steps; actual assessed-head CI passes all eleven contexts and 177 product
steps. No new local all-suite run or bootstrap behavior change is asserted.
[Explicit human acceptance](../../specs/002-platform-contracts/acceptance.md)
is now recorded separately from review. The same
[PR #5](https://github.com/Stonefly-Labs/Kofte/pull/5) is human-merged;
acceptance adds no bootstrap behavior or new product measurement.
Spec 003's **implementation stage is complete**, with explicit human approval of
[ADR 0005](../adr/0005-module-storage-ownership.md).
The [implementation report](../../specs/003-storage-registry/implementation.md)
records actual registry/provider races, assigned EF boundaries, killed workers,
explicit migration recovery, bounded cleanup and captured privacy evidence.
The [validation guide](../../specs/003-storage-registry/quickstart.md)
includes all nine executable public examples and actual stopped provider
backup/restore, with safe legacy refusals. Named suites now include the storage
projects. The implementation checkpoint marked all 78 original tasks complete;
its complete local 200-step gate and eleven assessed-head CI contexts pass.
No feature acceptance is inferred.
The subsequent [convergence](../../specs/003-storage-registry/convergence.md)
appends T079-T080 for two HIGH partial public-example gaps: the standalone/`all`
`same-id-retry` case lacks its promised three process restarts and retained-data
checks, and `collision` does not yet demonstrate a distinct-registered-ID
candidate conflict. Existing integration tests cover the core behavior; current
positive public-case counts do not prove these omitted observations.
No product/test code or supported command changed in convergence.
The [T079-T080 implementation increment](../../specs/003-storage-registry/implementation.md#convergence-remediation-t079-t080)
now executes three fresh processes after releasing maintenance, verifying both
exact assignments and original sentinels after every restart. The collision
example now includes a fixture-forced real registered-owner UNIQUE conflict,
unchanged assignment/data/inventory and no contender allocation, while retaining
unowned-target refusal. Standalone and `all` result validation requires those
observations, not only assertion totals. All 80 tasks are evidenced.
The [follow-up convergence](../../specs/003-storage-registry/convergence.md)
now passes: all 40 requirements/outcomes/scenarios, fourteen plan decisions,
80 tasks and twelve principles were assessed with zero actionable gaps.
Both prior public-example findings are closed; tasks and product/test source
remain unchanged. Actual assessed-head CI passes eleven contexts and 200
product steps. **Review is next** on this same feature branch/draft PR; no
final review, feature acceptance or changed supported command is inferred.
The subsequent [final review](../../specs/003-storage-registry/review.md) records
**changes** for one MEDIUM evidence gap: absent changed-allocator persisted-owner
retry and no-storage-to-relational refusal regressions. Production code appears
correct; no storage-engine failure or supported-command change is demonstrated.
Exactly T081 is appended, preserving all 80 historical markers and earlier
reports; prior public F1/F2 remain closed. Fresh quality/unit/both-provider
integration/recovery/contract/privacy gates pass 157 targeted steps and actual
assessed-head CI passes eleven contexts/200 product steps.
That review selected implement without a credential, new approval or merge.
The [T081 evidence](../../specs/003-storage-registry/implementation.md#final-review-remediation-t081)
now records both inherited regressions: four passing real-provider cases,
controlled mutation failures and unchanged assignments/data/revisions/inventory.
All 81 tasks are evidenced. Quality/unit/integration gates pass 46 targeted steps
after an unchanged PostgreSQL rerun of two initial deadline failures. Exact
focused commands and the initial failure are retained in the report. Production,
APIs, dependencies and supported commands are unchanged.
The [current convergence](../../specs/003-storage-registry/convergence.md) now
passes across all 40 requirements/outcomes/scenarios, fourteen plan decisions,
81 tasks and twelve principles. R1/T081 and prior public F1/F2 are closed, with
zero actionable gaps and no appended task. Tasks, product/test code, approved
intent and the historical review remain unchanged. That convergence selected
review on the same draft PR.
The [follow-up final review](../../specs/003-storage-registry/review.md) now records
**changes** for one MEDIUM finding R2: explicit registry-upgrade mutations and
rejected boundaries bypass required correlated storage records. Existing CLI
success/generic failure summaries are present but do not supply complete direct
API evidence. R1/T081 and public F1/F2 are closed. Exactly T082 is appended
unchecked for safe direct-API/CLI projection and real both-provider captures;
all 81 historical task bytes/markers, code, supported commands and previous
reports remain unchanged at that review checkpoint.
The [T082 increment](../../specs/003-storage-registry/implementation.md#final-review-remediation-t082)
now emits correlated upgrade/failed-acquisition records and safely projects
worker outcomes in the CLI. Both-provider capture/host regressions cover commits,
unchanged retries, rejected scopes, unsupported/corrupt formats and sanitized
commit-boundary failures. All 82 tasks were evidenced at that implementation
checkpoint, which selected follow-up converge.
The [current convergence](../../specs/003-storage-registry/convergence.md)
now records **gaps**: R2/T082 and prior T079-T081 are closed, but one MEDIUM
partial finding F3 reproduces a rejected public recovery inspection with zero
correlated records/spans/counters. T083 is appended for recovery-set boundary/
mutation diagnostics and real-provider captures, preserving all original task
bytes, product/test code, supported commands and historical evidence.
That checkpoint selected implement on the same draft PR, not acceptance or a new
design/merge prerequisite. Recovery refusals still preserve data; this finding
does not demonstrate a storage-isolation or recovery-corruption failure.
The [T083 increment](../../specs/003-storage-registry/implementation.md#convergence-remediation-t083)
now records public recovery inspection/verification/manifest-write boundaries,
with real-provider private captures and unchanged exception contracts.
All 83 tasks are evidenced. Historical convergence/review and reviewer-owned
checklists are preserved; that implementation checkpoint selected converge.
The [convergence after T083](../../specs/003-storage-registry/convergence.md)
now **passes** after checking all 40 requirements/outcomes/scenarios, fourteen
plan decisions, 83 tasks and twelve principles against actual source/assertions.
F3/T083 and prior T079-T082 are closed, with zero actionable gaps and no appended
task. Tasks are byte-for-byte unchanged; product/test code, supported commands,
approved intent, historical review and reviewer checklists remain untouched.
**Review is next** on the same draft PR, before explicit human acceptance;
this checkpoint neither accepts the feature nor requests an intermediate merge.
The [final review after T083](../../specs/003-storage-registry/review.md) now
**passes** across all 40 requirements/outcomes/scenarios, fourteen plan
decisions, 83 tasks and twelve principles. F1/F2, R1/R2 and F3 are closed,
with no remaining finding or remediation task. Tasks and reviewer-owned
checklists remain byte-for-byte unchanged; product/test code, approved intent
and supported commands are untouched. Historical review remains verbatim.
**Explicit human acceptance of numbered feature 003 is next**, separately
on the same draft PR. Review does not accept 003, mark ready, request merge
or begin 004.
The [explicit human acceptance](../../specs/003-storage-registry/acceptance.md)
now records **"this human accesspts 003"** for numbered feature 003 with its
existing review scope and limits. Supported commands, product/test code,
tasks, reviewed checklists and prior evidence are unchanged. No new local
product suite, usability study or runtime measurement is claimed by this
decision-only stage. The same
[PR #6](https://github.com/Stonefly-Labs/Kofte/pull/6) was human-merged.
The accepted ledger and all five acceptance hashes were verified on updated
main before 004 began; acceptance itself did not start the next feature.

**004 / specify, clarify and plan are complete**, not a module loader implementation.
The [draft requirements](../../specs/004-module-lifecycle/spec.md) and
[16/16 author checklist](../../specs/004-module-lifecycle/checklists/requirements.md)
cover empty/public-test-module boot, preflight, dependency-ordered migrations,
data-preserving disable/re-enable and safe health/failure diagnostics.
Supported commands, product code and dependencies remain unchanged; future
validation is not an executed product check. Clarification found no material
requirements ambiguity, asked zero questions and left the spec and 16/16 author
checklist unchanged. The [plan](../../specs/004-module-lifecycle/plan.md),
versioned author/operator contracts and
[future validation guide](../../specs/004-module-lifecycle/quickstart.md) now
propose a compiled trusted catalog, private selection, whole-set preflight,
explicit stopped preparation and supervised serving/health.
`KOFTE_MODULES_FILE`, `--modules` commands and composition test projects in
those artifacts **do not exist yet**. Existing commands/toolchain remain
unchanged. [ADR 0006](../adr/0006-module-composition-lifecycle.md) is now
**human-approved for implementation** by **"yeah approved"**; dependency adoption
and runtime evidence remain future gates. No next stage was executed.
**Checklist is next**, generated unchecked
for reviewer-owned evaluation, on `feature/004-module-lifecycle` and
[draft PR #7](https://github.com/Stonefly-Labs/Kofte/pull/7).

For the implemented storage examples, follow the
[003 executable guide](../../specs/003-storage-registry/quickstart.md).
Only trusted composition receives `StorageRegistry`/`StorageMaintenance`;
modules receive owner-only bindings. `ModuleMigrationRecovery.RecoverAsync`
is an explicit stopped-maintenance operation, not an ordinary resolve repair.
Run synchronous maintenance through the admitted owned-worker supervisor.
Registry upgrade has a 30-second total, module maintenance 120 seconds,
including cleanup; acquire/resolve limits are two/three seconds. Losing a
PostgreSQL lock session cancels the scope rather than reacquiring it.
`--registry upgrade` retains its `registry_upgraded` success summary and emits
a separate safe `operation: upgrade` record with provider, stable code/outcome,
generated correlation, fixed guidance and durability. The supervisor drains raw
worker stdout/stderr and projects only allowlisted exit identities. Its durability
is conservatively `uncertain` once a child may write, even for success; direct
API records distinguish actual `committed` upgrades and `unchanged` v1 retries.
No failure summary proves rollback, and no upgrade result contains a module assignment.
`RecoverySetVerifier` records use `operation: recover` and null module identity.
Provider context is trusted installation metadata or a fully validated manifest;
before either is available, diagnostics explicitly use the bounded `unknown`
label instead of copying untrusted provider text. Inspection/verification and
pre-write refusals are `unchanged`; a created but interrupted manifest is
`uncertain`; completed serialization/flush/close is `committed`. This describes
the manifest write, not an installation-wide recovery transaction. Pure digest
and comparison helpers do not independently log. Preserve partial files on
failure; existing `StorageException`, cancellation and exclusive-create
`IOException` behavior remains, with no raw exception attached to telemetry.
Only ready carries an assignment; retry busy boundedly, correct rejected
configuration, and inspect incomplete/reconciliation evidence without deleting
files, locks or history. Complete-set restores require a fresh owned target.
Choosing an older whole-set point requires accepting its data loss; hashes and
UUIDs cannot reveal malicious forgery or an unknown newer lost backup.
JSON logs, spans and bounded operation/provider/outcome metrics are observable
audit-relevant records, not spec 009's durable audit runtime. Ownership is not a
hostile-plugin sandbox or future actor/team/resource authorization.

The following paragraph preserves the pre-implementation design-stage history.
Spec 003 now has [draft storage requirements](../../specs/003-storage-registry/spec.md),
a completed author checklist, zero-question clarification and a
[proposed plan](../../specs/003-storage-registry/plan.md). Its
[future validation guide](../../specs/003-storage-registry/quickstart.md)
describes commands/cases that are **not implemented yet**.
[ADR 0005](../adr/0005-module-storage-ownership.md) proposes offline provisioning,
explicit bootstrap upgrade, assigned EF migrations and stopped full-set recovery;
it is not human-approved. The
[quality checklist](../../specs/003-storage-registry/checklists/quality.md) now
satisfies all 36 requirements-quality criteria after explicit reviewer
authorization, with no blocking gap. The
[78-task list](../../specs/003-storage-registry/tasks.md) now defines the
unstarted work and actual evidence obligations for all five stories.
The read-only [analysis](../../specs/003-storage-registry/analysis.md) now passes
with 22/22 requirements/outcomes and 18/18 scenarios covered, all 78 tasks mapped
and no actionable or constitutional finding. All analyzed inputs and markers
remain unchanged. Implement is next on the same draft PR, but explicit human
ADR 0005 approval is required before implementation.
ADR approval, dependency adoption and actual provider proof remain later gates.
No registry/module storage, new command or changed startup/recovery behavior
is implemented by this analysis stage.
Spec 002 timing now uses an available supported workstation/CI runner with
actual metadata, not a fixed Ubuntu/CPU/RAM profile, per the human's ADR amendment.
The subsequent author-guide amendment removes the manual timed exercise and
requires real automated guide/error/corrected-retry checks instead; no human
usability or elapsed-time claim is inferred.
It adds no host route, module loading or store change to this bootstrap.
This is not a feature-acceptance or production guide.
See the [implementation evidence](../../specs/001-repository-bootstrap/implementation.md).

The existing named `unit` suite now requires the platform unit project. `contract`
builds the author consumer, runs public .NET corpus/culture/privacy/retry/timing
tests and offline schema tests before retaining all real-provider HTTP checks.
`quality` builds/formats the expanded solution and checks structural TypeScript.
Missing required platform files/projects or zero selected .NET tests fail closed.
CI invokes these same commands; there is no separate CI-only validator.
Browser-enabled Ubuntu CI jobs use the official HTTPS Ubuntu archive in place
of the runner's slow Azure HTTP mirror. They still install the same Playwright
OS prerequisites and pinned Chromium; package signatures and browser checks
are not bypassed.

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
it. Stop/retry/restart do not delete data. Fresh format-0 initialization has no
application/fixture table, migration history or registry. A host-held exclusive operation lock prevents
concurrent initialization; the lock file remains in place after clean shutdown.

On the 003 feature branch, initialization still creates genuine table-free
format 0. The separate `--registry upgrade` maintenance command establishes
format 1; normal serving never initializes or migrates it automatically.
The host remains module-free, and no public registration command is provided.
Do not put `KOFTE_POSTGRES_PROVISIONING_CONNECTION` in serving configuration:
the elevated connection is accepted only by owned offline maintenance.

## Stopped-host SQLite recovery

The following legacy procedures apply **only to genuine table-free format 0**.
Once registry/history/module evidence exists, including leftover module stores
beside a missing platform file, init/platform-only backup/restore/reset must
refuse rather than orphan data. Restore inspects source and destination; even
confirmed reset does not waive the storage guard. Preserve the original
installation and use the complete-set procedure only after its remaining
[implementation evidence](../../specs/003-storage-registry/implementation.md)
is complete; the new verifier is not an automatic backup product.

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
dotnet build tests/contract/Kofte.Storage.Consumer --no-restore -c Release -warnaserror
npm run test --workspace tests/e2e -- startup.spec.ts
```

.NET test source lives physically inside its test project, not in production
vertical slices or linked from production folders. The integration tests use
owned private roots/ports and include a real child host. Shared contract
generation is explicit:
`npm run generate --workspace packages/bootstrap-contract`.

The startup test owns two private built-asset host/web cycles, unpredictable
test-only fixture values, free ports and process groups. Before timing starts,
the public storage consumer provisions assigned Alpha/Beta stores and real
migrations; the restart sentinel lives in Alpha's `Records`, not an unknown
table in the table-free-v0 platform database. It verifies exact
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

Contract verification first runs the public C# contract project with the
selected provider environment. PostgreSQL requires explicit disposable opt-in
before any child; single-provider runs exclude the other provider's trait
without excluding provider-neutral manifest/harness tests. Startup verification
also builds `Kofte.Storage.Consumer` before its timed browser exercise.

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
