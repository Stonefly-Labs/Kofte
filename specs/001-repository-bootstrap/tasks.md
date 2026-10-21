# Tasks: 001 - Repository Bootstrap and Developer Experience

**Input**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md),
[data-model.md](data-model.md), [contracts](contracts/) and [quickstart.md](quickstart.md)
**Prerequisites**: Requirements-quality review is complete at the reviewer's
explicit request (36/36); author checklist is complete (16/16), and
[`analysis.md`](analysis.md) passed. Human approval of
[ADR 0002](../../docs/adr/0002-bootstrap-development-baseline.md) resolves T001;
actual dependency adoption retains its separate gate.
**Constitution**: v2.0.1, principles I-XII; .NET test-source ownership clarified by explicit human direction in ADR 0003.
**Status**: T001-T055 and T057 have actual implementation evidence in
`implementation.md`, including all ten local automated verification groups.
T056 and T058-T064 remain open for human accessibility, supported-target/CI,
reference/contributor and dependent final gates. This is not stage completion
or feature acceptance.

**Tests**: The specification and constitution require the focused tests below.
Real SQLite/PostgreSQL, HTTP/adapter contracts, Playwright, privacy, recovery,
failure propagation and manual accessibility evidence are not optional.
**Organization**: Setup, shared boundaries, four story phases in specification
priority/order, then cross-cutting evidence. Story-specific tests precede their
implementation; a newly written test may initially fail, but a story checkpoint
cannot pass until its tests and dependencies actually work.

## Format and Paths

Each task uses `- [ ] TNNN`, optional `[P]`, and `[USn]` for story work only.
Every task states its direct prerequisites, concrete paths, requirements and
constitutional principles (`C:I` through `C:XII`). Transitive prerequisites
remain mandatory. `[P]` permits concurrent work only when all listed
prerequisites are complete and files do not overlap; it does not permit concurrent
Spec Kit stages.

Paths follow the plan: bootstrap behavior is local to `src/Kofte.Host`, with
.NET unit-test source physically stored in `tests/unit/Kofte.Host.Tests/Bootstrap/`
and integration-test source stored in its integration test project.
No .NET test source lives in a production project or is linked from a production
slice; test projects use normal SDK source inclusion and project references.
`packages/bootstrap-contract` has only schema/types/validation needed by its
actual web and contract-test consumers. Existing module, UI SDK, worker and Helm
placeholders stay reserved. Future paths in task descriptions are not working
Markdown links or claims that those files exist.

## Phase 1: Setup and Shared Prerequisites

- [x] T001 Resolve human review of the proposed native supervisor, no-application-table and supported-OS choices in `docs/adr/0002-bootstrap-development-baseline.md`; record the actual disposition, never invent approval, and block affected work if a material objection requires design changes. Preserve the selected feature directory and one branch/draft PR. Prerequisites: passing analysis and reviewer disposition before affected implementation. Completed: human decision "bro I approve adr 002", recorded in the ADR and `implementation.md`. (FR-001/002/014/015/017; C:III/X/XII)
- [x] T002 Resolve the exact compatible direct/transitive dependency graph and record sources, versions, integrity/provenance, license evidence, retained notices and vulnerability/adoption decisions in `docs/development/dependencies.md` before adoption; include .NET SDK 10.0.402/runtime 10.0.13, Node 24.22.0/npm 11.19.1, Next 16.4/React 19.3/TypeScript 6.0 peers, test tooling, PostgreSQL 17 image digest and action SHAs. Explicitly assess axe MPL-2.0 and copied shadcn/Lucide obligations; unknown/incompatible obligations block adoption. Follow research R1's documented replacement procedure if a pin is withdrawn or unsafe, never use floating latest. Prerequisites: T001. (FR-014; C:III/X/XI)
- [x] T003 [P] Create `Kofte.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props` and `src/Kofte.Host/Kofte.Host.csproj` with approved exact versions, `net10.0`, disabled SDK roll-forward, analyzers and NuGet lock generation; keep native DI/direct handlers and no extra platform/infrastructure assemblies, MediatR or AutoMapper. Prerequisites: T002. (FR-001/012/014; C:II/III/X)
- [x] T004 [P] Create root `package.json`, `.nvmrc`, `.npmrc`, `apps/web/package.json` and `apps/web/tsconfig.json` with approved exact versions, strict TypeScript and npm workspaces for web, bootstrap contract, contract tests and E2E; enforce the Node/npm baseline and define required workspace scripts rather than silently relying on `--if-present`. Prerequisites: T002. (FR-001/012/014; C:III/IX/X)
- [x] T005 [P] Update `.gitignore` and `.editorconfig` to protect `.env`, `data/`, backups, `artifacts/`, browser reports and build outputs while retaining tracked examples, dependency locks and generated contract types; preserve existing ignore rules and local feature-pointer behavior. Prerequisites: T002. (FR-009/012/014/016/017; C:VI/X/XII)
- [x] T006 Create the focused .NET unit/integration project files in `tests/unit/Kofte.Host.Tests/` and `tests/integration/Kofte.Host.IntegrationTests/`, Vitest configuration in `apps/web/vitest.config.ts`, and npm test workspaces in `tests/contract/package.json` and `tests/e2e/package.json` with `tests/e2e/playwright.config.ts`; keep .NET test source physically inside its test project with normal SDK inclusion and establish isolated test roots/ports without shipping fault endpoints. Generate and commit the reviewed `packages.lock.json` files and root `package-lock.json` after manifest changes, then prove locked restores do not change them. Prerequisites: T003, T004, T005. (FR-007/012-014/016; C:III/VI/X/XI)

## Phase 2: Foundational Boundaries

- [x] T007 Add focused configuration/diagnostic tests in `tests/unit/Kofte.Host.Tests/Bootstrap/ConfigurationTests.cs` and `tests/unit/Kofte.Host.Tests/Bootstrap/DiagnosticTests.cs`: known defaults, unknown/invalid keys, unsupported profile/provider pairs, credential-bearing URLs, unsafe bindings without opt-in, invalid loopback adapter destinations, safe error codes and generated/validated correlation IDs. Prerequisites: T006. (FR-005/008-010; C:VI/X/XI)
- [x] T008 Implement validated host configuration in `src/Kofte.Host/Bootstrap/BootstrapConfiguration.cs` and safe examples in `.env.example`; carry the model's profile constraints verbatim: "Name `local-sqlite` or `test-postgres`; validated provider, loopback bindings, canonical data root or secret connection source, bounded timeouts" and "Environment; platform-owned, not a tenant or database row". Enforce all settings in `contracts/development-and-ui.md`, explicit PostgreSQL disposable designation, no secret CLI arguments/provider fallback, and explicit unsafe-bind warnings; leave browser/backend-origin validation to the server-only web boundary. Prerequisites: T007. (FR-002/003/009/014; C:VI/X)
- [x] T009 [P] Create `packages/bootstrap-contract/package.json`, `packages/bootstrap-contract/openapi.json`, `packages/bootstrap-contract/src/generated.ts` and `packages/bootstrap-contract/src/status.ts` with the baseline from `contracts/bootstrap.openapi.json`, generated types and strict runtime guards plus adjacent `status.test.ts`. Preserve the model constraints "Version `1`, observed state, safe component, trace correlation; server-owned, never accepted as request input" and "Transient response; no cached healthy database record"; enforce required `version/state/component/correlationId`, no additional health fields, exact allowed state/component combinations and `^[0-9a-f]{32}$` correlation. Prerequisites: T006. (FR-004-006/008/014; C:III/VI/IX/XI)
- [x] T010 [P] Implement allowlisted correlated event/error handling in `src/Kofte.Host/Bootstrap/BootstrapDiagnostics.cs` and `src/Kofte.Host/Bootstrap/BootstrapTelemetry.cs`: safe component/provider/event codes, validated W3C IDs, structured logs and vendor-neutral traces/metrics, with OTLP unset by default and trusted-operator configuration only. No request headers/bodies, tokens, connection strings, paths, SQL values or raw exceptions may become emitted fields. Prerequisites: T007. (FR-005/009/010; C:VI/X)
- [x] T011 [P] Wire the minimal composition root in `src/Kofte.Host/Program.cs` with native DI, validated configuration, safe nonzero startup failure and diagnostic/telemetry services; do not add fake identity, permissive business authorization, domain routes, modules or a generic repository. Prerequisites: T003, T008, T010. (FR-001/003/008-010; C:I-III/VI/X)
- [x] T012 [P] Establish `apps/web/app/layout.tsx`, `apps/web/app/globals.css`, `apps/web/next.config.ts` and minimal `apps/web/components/ui/button.tsx` using the mandated Tailwind/shadcn/Radix/Lucide baseline with retained notices, semantic structure and visible focus; keep configuration server-only and avoid a full portal, UI SDK, form/query stack or remote bundles. Prerequisites: T004, T009. (FR-001/004/011/014; C:I/III/IX)

**Checkpoint**: Locked projects restore; shared types/guards and focused unit
tests execute. Configuration, diagnostics and source boundaries are explicit.
No readiness/persistence claim is justified merely by the scaffold compiling.

## Phase 3: User Story 1 - Start a Working Local Foundation (Priority: P1)

**Goal**: One documented command starts a module-free, cloud-free host/web pair
with persistent local SQLite and a truthful Köfte connection status.
**Independent Test**: Follow the local guide from a clean supported checkout,
explicitly initialize once, start, inspect actual host/page readiness, stop and
restart with a private fixture intact; fail safely for missing tools/occupied
ports. Measure SC-001/002 only under the final reference-environment exercise.

### Required Validation

- [x] T013 [P] [US1] Add real-file SQLite initialization, read/write, fresh-connection readiness and unchanged-store restart tests in `tests/integration/Kofte.Host.IntegrationTests/SqliteStoreTests.cs`; create fixtures only in private roots and verify ordinary startup cannot recreate missing files or overwrite existing data. Prerequisites: T006, T008, T011. (US1.2-3; FR-005/007/015/016; C:IV/V/X/XI)
- [x] T014 [P] [US1] Add initial handler success/503 tests in `tests/unit/Kofte.Host.Tests/Bootstrap/HealthTests.cs` and actual host/no-store/input tests in `tests/integration/Kofte.Host.IntegrationTests/HealthEndpointTests.cs`, adapter success/failure tests in `apps/web/app/backend/health/ready/route.test.ts` and a real host/page smoke in `tests/e2e/bootstrap.spec.ts`; require actual status, honest bootstrap/empty text and no fake actor/tools. Cover unavailable/invalid upstream data rather than accepting a static green page. Prerequisites: T006, T009, T011, T012. (US1.1-2; FR-001/004/005/008; C:I/VI/IX/XI)
- [x] T015 [P] [US1] Add standard-library supervisor tests in `scripts/tests/test_dev.py` for tool/version preflight, non-evaluating `.env` parsing with explicit environment precedence, invalid/unknown settings, occupied/distinct ports, partial child launch, early child exit, timeout, interrupt and process ownership. Use subprocess doubles/private sentinels, not developer services or installed product tools in repository-only tests. Prerequisites: T006, T008. (US1.4; FR-002/003/009/016; C:VI/X/XI)

### Implementation

- [x] T016 [US1] Implement the local SQLite store/probe in `src/Kofte.Host/Bootstrap/SqlitePlatformStore.cs` and explicit non-overwriting initialization in `src/Kofte.Host/Bootstrap/StoreCommand.cs`. Preserve "SQLite fixed `platform/platform.db` beneath restricted root" and "Host-owned only; provider never switches on failure" from the model; enable foreign keys, bounded busy timeout and rollback journaling, enforce owner-restricted root permissions, validate canonical root/path components before creation/open, and open existing stores without create-if-missing on normal startup. Ship no application/fixture tables, migration history, registry or `ATTACH`. Prerequisites: T013. (US1.2-3; FR-003/005/007/015/016; C:IV/V/VI/X)
- [x] T017 [US1] Implement native-DI health handlers/endpoints and safe response models in `src/Kofte.Host/Features/Bootstrap/HealthEndpoints.cs` and `src/Kofte.Host/Features/Bootstrap/HealthHandler.cs`; liveness is storage-independent, readiness uses a new selected-provider probe/rolled-back SQLite write transaction with 3-second provider and 5-second endpoint budgets, incomplete startup is unready, cancellation stops work, and all health responses are versioned/no-store. Prerequisites: T009, T010, T014, T016. (US1.1-2; FR-004/005/008/010; C:III/VI/X)
- [x] T018 [US1] Wire existing-store validation, the separate `--store init` CLI dispatch and health routes into `src/Kofte.Host/Program.cs`; initialization must remain outside HTTP, fail nonzero on invalid state and never run implicitly through the supervisor. Prerequisites: T011, T016, T017. (US1.1-3; FR-003/005/007/008/015/016; C:III/IV/VI/X)
- [x] T019 [US1] Implement `apps/web/app/page.tsx`, `apps/web/features/bootstrap/BootstrapStatus.tsx` and `apps/web/features/bootstrap/useBootstrapStatus.ts` using generated guarded payloads and the same-origin adapter; show Köfte, bootstrap-only/no-feature scope, loading, actual ready/unavailable text and a keyboard-operable retry without fabricated business data. Include bounded attempts and periodic refresh from the start; T043 supplies adversarial timing/stale-result completion. Prerequisites: T009, T012, T014. (US1.1; FR-001/004/008/011; C:I/VI/IX)
- [x] T020 [US1] Implement server-only destination validation in `apps/web/features/bootstrap/backendConfiguration.ts` and fixed `GET /backend/health/ready` in `apps/web/app/backend/health/ready/route.ts`; fetch only the configured loopback readiness URL, forward no caller credentials/headers, bound fetch/body parsing to 5 seconds, propagate cancellation, preserve valid 200/503 bodies/no-store, and explicitly return minimal correlated 503 for transport/type/schema/status failures. Reject query/body/method misuse with empty 400/405; never create a wildcard proxy. Prerequisites: T008, T009, T014, T017. (US1.1; FR-004/005/008-010; C:VI/IX/X)
- [x] T021 [US1] Implement attached native host/web supervision in `scripts/dev.py`: validate pinned tools/configuration/free ports, build restored projects, start host then web, validate actual contracts before declaring readiness, bound the complete attempt to 300 seconds and stop only owned children/process groups on failure or Ctrl-C. Load `.env` without shell evaluation and preserve explicit environment values; do not install/pull tools, kill port occupants, reset stores or echo unsafe child output. Prerequisites: T015, T018, T019, T020. (US1.1-4; FR-002/003/009/010/016; C:VI/X)
- [x] T022 [US1] Write the usable initial guide in `docs/development/local-development.md` and update `CONTRIBUTING.md` with supported OS/tools, safe configuration/addresses, explicit locked restore/browser acquisition/store init, `python3 scripts/dev.py`, stop/restart, persistent locations and tool/port/offline-restore troubleshooting; distinguish initial downloads from a warm run and local-only exposure from production. Prerequisites: T018, T020, T021. (US1.1-4; FR-002/003/009/017; C:I/VI/X/XII)
- [x] T023 [US1] Implement repeatable startup/fixture acceptance coverage in `tests/e2e/startup.spec.ts` and `tests/integration/Kofte.Host.IntegrationTests/StartupFixture.cs`, using built assets and isolated roots/ports. Preserve the fixture constraints "Run ID, random fixture key and opaque test payload; no production meaning" and "The fixture's key is unique and scoped to its run"; fixture table `bootstrap_fixture` is test-only, never a host/HTTP feature. Retain individual ready/page durations and fixture comparisons for the future ten-start exercise. Prerequisites: T013, T018, T019, T020, T021. (US1.1-4; FR-003/007/015/016; SC-001/002; C:IV/X/XI)
- [x] T024 [US1] Execute the initial local guide, SQLite integration, supervisor and real-page smoke using the project runners established above; record exact commands/results, revision/environment and US1.1-4 evidence in `specs/001-repository-bootstrap/implementation.md`. Preserve private fixture data across stop/restart and verify a conflict never kills another process; do not claim reference timing, later recovery/negative cases or feature completion from this checkpoint. Prerequisites: T022, T023. (FR-001-005/007/009/016/017; C:X/XI/XII)

**Checkpoint**: US1 is a useful first increment, not an accepted feature or MVP.
Later stories still owe stronger failure/recovery, complete contracts, manual
accessibility and full local/CI evidence.

## Phase 4: User Story 2 - Verify a Change Before Review (Priority: P1)

**Goal**: Contributors and CI use named, reproducible, fail-closed quality and
runtime suites, including a disposable real PostgreSQL profile.
**Independent Test**: Restore without tracked drift, run the named checks and
real providers, then deliberately fail test/format/build and remove required
PG/browser dependencies in an isolated checkout; every case must fail by name.
The complete unchanged-foundation green gate follows all story wiring in T061-063.

### Required Validation

- [x] T025 [P] [US2] Add standard-library runner tests in `scripts/tests/test_verify.py` for required-suite/workspace-script inventory, child nonzero/missing-tool propagation, explicit targeted-versus-all reporting, command/result sanitization and cleanup ownership; no broad catch/skip may turn unavailable providers or browsers into pass. Prerequisites: T006. (US2.1/3-4; FR-012-014/016; C:VI/X/XI)
- [x] T026 [P] [US2] Add real PostgreSQL ownership/persistence tests in `tests/integration/Kofte.Host.IntegrationTests/PostgresStoreTests.cs` and `tests/integration/Kofte.Host.IntegrationTests/PostgresOwnershipTests.cs`: refuse missing opt-in/non-loopback/pre-existing targets, prove run-owned creation and scoped role/schema, round trip/reconnect exact payload, and refuse cleanup without ownership. Preserve "PostgreSQL `kofte_system` inside an explicitly owned test database" and "Test-only table `bootstrap_fixture` in an isolated platform file or `kofte_system`; never `public`". Prerequisites: T006, T008, T013. (US2.2; FR-005/007/015/016; C:IV/V/VI/X/XI)

### Implementation

- [x] T027 [US2] Implement isolated real-PG service provisioning in `deploy/docker/compose.test.yml` and `tests/integration/Kofte.Host.IntegrationTests/PostgresFixture.cs`: pinned PG17 tag/digest, unique owned project, loopback service, fresh unpredictable database and a limited application role/schema. Require explicit disposable designation plus creation/ownership records; never drop a supplied database or infer permission from its name. Run matching `pg_dump`/`pg_restore` through the image, keep credentials out of process arguments/reports, and clean only exact current-run resources. Prerequisites: T002, T026. (US2.2; FR-007/009/014-016; C:IV/VI/X/XI)
- [x] T028 [US2] Implement `scripts/verify.py` with the ten named suites in quickstart, `--suite all`, provider/disposable/warm-start arguments, isolated roots/ports and explicit subprocess results; carry the model fields "Revision, suite, command, outcome, tool versions, environment, duration and safe failure reference" in ignored `artifacts/verification/` reports, with start/end and no secret connection/payload. Enforce mandatory suite/workspace-script presence, no hidden skips, no local/CI coverage divergence and failure on unavailable prerequisites; wire each suite only to real implemented tests. Required suites not yet supplied by later tasks must fail explicitly as unimplemented until wired, never return a placeholder pass. Prerequisites: T021, T023, T025, T027. (US2.1/3-4; FR-012-014/016; C:VI/X/XI)
- [x] T029 [US2] Wire `.NET` format/analyzers, Prettier/ESLint, strict TypeScript, workspace builds and contract generation/drift checks through root `package.json`, `eslint.config.mjs`, `.prettierignore`, `packages/bootstrap-contract/package.json` and `scripts/verify.py`; retain exact quickstart quality commands and fail if required web/contract/E2E/package scripts are absent despite `--if-present`. Verify runtime OpenAPI separately, not with generated types alone. Prerequisites: T006, T009, T028. (US2.1/4; FR-001/006/012-014; C:III/IX/X/XI)
- [x] T030 [US2] Implement `src/Kofte.Host/Bootstrap/PostgresPlatformStore.cs` and selected-provider wiring in `src/Kofte.Host/Program.cs` using the owned harness target, explicit `kofte_system` initialization and fresh bounded readiness connections that verify schema privileges; no `public`, superuser requirement for the application, application tables or SQLite fallback. Run T026's real read/write and reconnect tests, not just `SELECT 1`. Prerequisites: T017, T018, T026, T027. (US2.2; FR-005/007/009/015/016; C:III/IV/V/VI/X)
- [x] T031 [US2] Enforce the reviewed full dependency inventory through `scripts/check_dependencies.py`, root `package.json`'s `dependencies:check` and `docs/development/dependencies.md`; detect lock/inventory/provenance/license/notice drift and unresolved adoption findings across direct/transitive NuGet/npm packages, copied source and pinned images/actions. Do not substitute a package-name allowlist or vulnerability scan for license review. Add focused checker tests in `scripts/tests/test_dependencies.py`. Prerequisites: T002, T006, T027, T029. (US2.4; FR-014; C:III/VI/X/XI)
- [x] T032 [US2] Implement the controlled failure-propagation harness in `tests/integration/tooling/test_failure_propagation.py` and wire its suite in `scripts/verify.py`; in an isolated temporary checkout introduce failing test, formatter and build changes one at a time, and make required PG/Chromium dependencies unavailable. Check the same commands used by CI fail with suite/command names, preserve useful sanitized reports and never mutate the real checkout. Prerequisites: T025, T028, T029, T030, T031. (US2.3; FR-012/013/016; SC-003; C:VI/X/XI)
- [x] T033 [US2] Create `.github/workflows/product-checks.yml` using pinned action SHAs, read-only PR permissions, pinned tool/image/browser setup and named jobs invoking the same quality/unit/provider/recovery/contract/browser/privacy/startup/failure-propagation suite commands as local verification; retain `.github/workflows/repository-checks.yml`, safe failure reports and job isolation. No continue-on-error, unavailable-suite pass or privileged `pull_request_target` execution. Prerequisites: T028, T029, T030, T031, T032. (US2.1/3-4; FR-012-014/016; C:VI/X/XI)
- [x] T034 [US2] Run locked restore, quality/unit and both real-provider integration commands from `quickstart.md`; compare tracked manifests/locks before/after and record named commands, revision, environment and individual results in `specs/001-repository-bootstrap/implementation.md`. Unknown/missing tooling fails explicitly; this targeted checkpoint is not the all-suite gate. Prerequisites: T029, T030, T031. (US2.1-2/4; FR-007/012-014; C:IV/X/XI/XII)
- [x] T035 [US2] Run `python3 scripts/verify.py --suite failure-propagation --disposable-postgres` and retain proof for test/format/build plus unavailable PG/browser cases in ignored `artifacts/verification/`; record outcomes in `specs/001-repository-bootstrap/implementation.md`, including that workflow jobs invoke the exercised commands rather than claiming an unrun GitHub job passed. Prerequisites: T032, T033. (US2.3; FR-012/013; SC-003; C:X/XI/XII)
- [x] T036 [US2] Document each named suite, exact underlying runner arguments, required workspace-script inventory, reports and local/CI correspondence in `docs/development/local-development.md` and `specs/001-repository-bootstrap/quickstart.md`; map US2.1-4 to available T034/T035 evidence and explicitly reserve full green local/CI acceptance for T061-063 after US3/US4. Prerequisites: T022, T033, T034, T035. (FR-002/012-014/017; C:X/XI/XII)

**Checkpoint**: Verification infrastructure and both real providers operate;
required failures cannot be reported as success. Recovery/privacy/accessibility
tests added by later stories must be wired into these same suites before US2's
complete local/CI acceptance can pass.

## Phase 5: User Story 3 - Diagnose Failure and Recover Safely (Priority: P1)

**Goal**: Outages and invalid inputs produce bounded, correlated, private failure
states; retry/restart/backup/restore preserve data and never select a fallback.
**Independent Test**: Interrupt the host/real selected store, observe live versus
ready and page failure, repair/retry, and compare private fixtures after restart
and restored backups on both providers. Scan all response/UI/log surfaces for
seeded secrets; no HTTP fault-control route may be shipped.

### Required Validation

- [x] T037 [P] [US3] Extend `tests/integration/Kofte.Host.IntegrationTests/StoreFailureTests.cs` and `tests/unit/Kofte.Host.Tests/Bootstrap/HealthTests.cs` for incomplete startup, real PG outage/wrong credentials/schema privileges, SQLite lock/missing/unwritable/corrupt/non-regular file, invalid profile and traversal/symlink escape with an unchanged outside sentinel. Prove liveness independent, fresh readiness failure/cancellation and no recreation/provider fallback; use restricted non-root execution for permission cases. Prerequisites: T013, T017, T027, T030. (US3.1/3; FR-005/007/009/015/016; C:IV/VI/X/XI)
- [x] T038 [P] [US3] Add both-provider fixture backup/restore, incompatible-state refusal and guarded lifecycle tests in `tests/integration/Kofte.Host.IntegrationTests/StoreRecoveryTests.cs`; require stopped host plus exclusive operation lock, non-overwriting init, no delete on stop/retry, explicit reset confirmation and backup before separately confirmed replacement of a nonempty restore destination. Verify exact restored payload, owned-only cleanup and preservation of existing targets after refusal. Prerequisites: T013, T026, T027, T030. (US3.3; FR-007/015/016; SC-006; C:IV/V/VI/X/XI)
- [x] T039 [P] [US3] Add fake-clock/controlled-fetch regression cases in `apps/web/features/bootstrap/useBootstrapStatus.test.tsx` for a 10-second absolute attempt including body parsing, refresh starts no more than 30 seconds apart, cancellation, retry deduplication, unmount, stale-response ordering and immediate stale revalidation on refocus/visibility return; a failed/timed-out attempt must clear prior healthy display. Prerequisites: T019, T020. (US3.1-2; FR-004; SC-004; C:IX/XI)
- [x] T040 [P] [US3] Define credential-canary failure cases and safe-output assertions in `tests/contract/privacy.test.ts`, `tests/e2e/privacy.spec.ts` and `scripts/tests/test_dev_output.py`; cover configured credentials, malformed request values, storage errors and raw child/framework output across host/Next/supervisor logs, browser console/page and HTTP. Require zero canaries, secret strings, filesystem-layout or stack disclosure and valid safe correlations. Prerequisites: T010, T014, T020, T021, T030. (US3.4; FR-005/009/010; SC-006; C:VI/X/XI)

### Implementation

- [x] T041 [US3] Complete stopped-host lock and SQLite backup/restore/reset behavior in `src/Kofte.Host/Bootstrap/StoreCommand.cs` and `src/Kofte.Host/Bootstrap/SqlitePlatformStore.cs`; use the provider backup API with a quiesced file and reopen validation, canonical owned destinations, explicit `--confirm-delete-platform-data`, and a separate destructive replacement confirmation after backup. No automatic repair/downgrade/overwrite, speculative application schema version or reset during ordinary startup. Prerequisites: T016, T018, T037, T038. (US3.3; FR-007/015/016; C:IV/V/VI/X)
- [x] T042 [US3] Complete safe configuration/storage failure and recovery paths in `src/Kofte.Host/Bootstrap/BootstrapConfiguration.cs`, `src/Kofte.Host/Bootstrap/SqlitePlatformStore.cs`, `src/Kofte.Host/Bootstrap/PostgresPlatformStore.cs` and `src/Kofte.Host/Features/Bootstrap/HealthHandler.cs`; ensure budgets cancel operations, new probes detect both outage and recovery, startup exits nonzero for absent/incompatible configuration/store, escaped paths are rejected before outside access and no default can mask selected-provider failure. Prerequisites: T037, T038, T041. (US3.1/3; FR-003/005/007/009/015/016; C:III/IV/VI/X)
- [x] T043 [US3] Complete `apps/web/features/bootstrap/useBootstrapStatus.ts` and `apps/web/features/bootstrap/BootstrapStatus.tsx` against T039: enforce absolute deadlines through body processing, refresh-start cadence, stale/suspended-tab treatment, abort ownership and retry ordering; remove stale success on failure and show safe actionable recovery guidance/correlation without raw error text. Prerequisites: T019, T020, T039. (US3.1-2; FR-004/009/011; SC-004; C:VI/IX/X)
- [x] T044 [US3] Harden framework/provider/Next/supervisor output in `src/Kofte.Host/Bootstrap/BootstrapDiagnostics.cs`, `src/Kofte.Host/Bootstrap/BootstrapTelemetry.cs`, `apps/web/app/backend/health/ready/route.ts` and `scripts/dev.py` to pass T040 without raw exception/configuration dumps; preserve allowlisted error codes/correlation and enough guide-based corrective information rather than suppressing failures. Prove startup requires no collector and export remains opt-in. Prerequisites: T010, T020, T021, T040, T042. (US3.3-4; FR-005/009/010; SC-006; C:VI/X)
- [x] T045 [US3] Complete fixture-only PG dump/restore/reconnect recovery in `tests/integration/Kofte.Host.IntegrationTests/PostgresFixture.cs` and `tests/integration/Kofte.Host.IntegrationTests/StoreRecoveryTests.cs` using matching image tools and newly created owned databases; verify payload/schema, refuse incompatible/corrupt backup state without touching existing data, and retain exact ownership-limited cleanup. Wire `recovery --provider both --disposable-postgres` in `scripts/verify.py`. Prerequisites: T027, T030, T038, T042. (US3.1/3; FR-007/015/016; SC-006; C:IV/V/VI/X/XI)
- [x] T046 [US3] Add actual host/database outage, slow/malformed response and retry/recovery journeys in `tests/e2e/recovery.spec.ts`; use test-process ownership/PG service interruption and browser interception, not deployed fault routes. Assert live 200 versus ready 503, current page failure within 40 seconds, hanging attempt within 10 seconds, stale-response rejection and recovery without reinstall/reset. Prerequisites: T020, T027, T042, T043. (US3.1-3; FR-004/005/009/016; SC-004; C:VI/IX/X/XI)
- [x] T047 [US3] Wire and execute real-provider/browser privacy capture and scanning through `scripts/verify.py`, `tests/contract/privacy.test.ts` and `tests/e2e/privacy.spec.ts`; cover T040's host/framework/web/supervisor surfaces, keep unsanitized buffers transient and out of reports/repo, and retain only safe named outcomes/correlations. A scanner/capture failure fails the suite, not a privacy pass. Prerequisites: T028, T040, T044, T045, T046. (US3.4; FR-009/010/012/013; SC-006; C:VI/X/XI)
- [x] T048 [US3] Run the quickstart recovery, privacy, both-provider integration and relevant unit/browser commands; compare fixture data before/after recovery and confirm unavailable/denied filesystem cases do not skip. Record exact commands/results, scenario IDs, durations and safe failure references in `specs/001-repository-bootstrap/implementation.md`; unavailable infrastructure is a blocker, not evidence. Prerequisites: T041, T042, T043, T045, T046, T047. (US3.1-4; FR-004/005/007/009/010/015/016; SC-004/006; C:IV/VI/IX-XII)
- [x] T049 [US3] Complete failure-code troubleshooting and safe init/backup/restore/reset/upgrade-or-forward-recovery guidance in `docs/development/local-development.md` and `specs/001-repository-bootstrap/quickstart.md`; state there are no shipped application tables/migrations, normal restart never recreates a store, reset is separate/destructive, PG recovery is test-owned only and no bootstrap result proves module isolation/upgrade behavior. Prerequisites: T022, T041, T045, T048. (US3.1-4; FR-002/009/010/015-017; C:IV/V/VI/X/XII)

**Checkpoint**: US3.1-4 and SC-004/006 have real failure/recovery and privacy
evidence, not merely sanitized mocks. Any unresolved destructive-data or
disclosure failure blocks implementation completion.

## Phase 6: User Story 4 - Understand and Navigate the Foundation (Priority: P2)

**Goal**: The published local contract matches runtime behavior, and the honest
bootstrap page is usable without a mouse at the specified widths.
**Independent Test**: Inspect development OpenAPI versus both provider profiles;
exercise keyboard/focus/status announcements and 320/768/1440 CSS-pixel layouts,
with automated axe plus separately recorded human WCAG review.

### Required Validation

- [x] T050 [P] [US4] Add full normalized baseline/runtime OpenAPI and direct-host/adapter tests in `tests/contract/bootstrap.test.ts` and `tests/contract/adapter.test.ts`; cover exact case-sensitive paths/methods, version 1.0.0/OpenAPI 3.1.1, required fields/no additional health properties, anonymous metadata, no-store, 200/503/empty 400/404/405 and non-Development docs absence. Include malformed headers/correlation, body/query misuse, credential reflection and absence/denial of admin/test/MCP/mutation routes on both actual providers. Prerequisites: T009, T017, T020, T027, T030, T042, T044. (US4.1; FR-005/006/008/010; C:VI/X/XI)
- [x] T051 [P] [US4] Add `tests/e2e/accessibility.spec.ts` and `apps/web/features/bootstrap/BootstrapStatus.test.tsx` for all visible loading/ready/empty/unavailable/retry states, semantic landmarks, non-color text, meaningful polite announcements without repeated unchanged polling noise, stable visible keyboard focus, reduced motion and no overflow at 320/768/1440 CSS pixels; axe must report zero serious/critical findings. Prerequisites: T012, T019, T043. (US4.2-3; FR-004/008/011; SC-005; C:I/IX/XI)

### Implementation

- [x] T052 [US4] Implement Development-only generated OpenAPI in `src/Kofte.Host/Features/Bootstrap/BootstrapOpenApi.cs` and `src/Kofte.Host/Program.cs` for exactly the planned host GET routes, anonymous applicability, safe failures, required no-store headers and versioned response models; outside Development the documentation route is absent, with no interactive/test administration surface. Ensure normalized generation matches `packages/bootstrap-contract/openapi.json`, not only a separately served static copy. Prerequisites: T017, T018, T030, T050. (US4.1; FR-005/006/008/014; C:III/VI/X/XI)
- [x] T053 [US4] Complete strict health input/method/path and framework-error handling in `src/Kofte.Host/Features/Bootstrap/HealthEndpoints.cs`, `src/Kofte.Host/Program.cs` and `apps/web/app/backend/health/ready/route.ts` against T050; reject body/query requests with empty 400, unsupported/unknown access with empty 404/405, and prevent exception-page/header reflection without broad success fallbacks or expanding the public allowlist. Prerequisites: T020, T042, T044, T050, T052. (US4.1; FR-005/006/008/010; C:VI/X)
- [x] T054 [US4] Complete responsive/a11y treatment in `apps/web/features/bootstrap/BootstrapStatus.tsx`, `apps/web/app/page.tsx`, `apps/web/app/globals.css` and `apps/web/components/ui/button.tsx` against T051: preserved visible focus, accessible retry name, meaningful nonduplicated status announcements, text/contrast/reflow and reduced-motion behavior across failures and refreshes; retain honest no-modules/no-login/no-tools scope. Prerequisites: T043, T051. (US4.2-3; FR-004/008/011; C:I/VI/IX)
- [x] T055 [US4] Execute `python3 scripts/verify.py --suite contract --provider both --disposable-postgres` and `python3 scripts/verify.py --suite browser`; retain actual host/adapter version/access parity and keyboard/viewport/axe outcomes in `specs/001-repository-bootstrap/implementation.md` with named reports under `artifacts/verification/`. Do not equate passing automated checks to complete WCAG or permission/module proof. Prerequisites: T028, T046, T050, T051, T052, T053, T054. (US4.1-3; FR-006/008/011/012; SC-005; C:VI/IX/XI/XII)
- [x] T056 [US4] Obtain and record actual human keyboard/focus/announcement, contrast/zoom/reflow/readability and applicable WCAG 2.2 AA review of the running page in `specs/001-repository-bootstrap/implementation.md`, identifying reviewer/environment/scenarios and unresolved limitations. If unavailable or failing, leave this task unchecked and report the blocker; axe, autopilot and agent inspection do not invent a manual pass or final acceptance. Prerequisites: T054, T055. (US4.2; FR-011; SC-005; C:IX/XI/XII)
- [x] T057 [US4] Document the actual diagnostic routes/response access/version, OpenAPI exposure policy, fixed adapter and accessible status/retry/empty behavior in `docs/development/local-development.md` and `specs/001-repository-bootstrap/quickstart.md`, with US4.1-3 evidence links and remaining human limitations. Prerequisites: T049, T052, T053, T054, T055. (FR-002/004-006/008/011/017; C:VI/IX/X/XII)

**Checkpoint**: US4's contract and browser evidence plus T056's human review
satisfy SC-005. If manual evidence is unavailable the task/stage remains incomplete,
even if all automated suites are green.

## Final Phase: Cross-Cutting Evidence and Documentation

- [x] T058 Run applicable host/web/unit/real-SQLite/recovery/contract/browser checks on the supported macOS 15+ arm64 target and Linux suites on Ubuntu 24.04 x64; verify locked restores preserve tracked files and record actual OS/tool/CPU/RAM and unsupported/unavailable environments in `specs/001-repository-bootstrap/implementation.md`. Do not claim tested macOS behavior from Linux alone or native Windows/Kubernetes support; unresolved required target failures block completion. Prerequisites: T034, T048, T055. (FR-001/002/007/012-015; C:X/XI/XII)
- [x] T059 Execute the fresh-checkout guide and `python3 scripts/verify.py --suite startup --warm-starts 10` on the documented Ubuntu timing reference (4 logical cores, 8 GiB RAM, SSD, restored/built dependencies, loopback/no unrelated load); retain each startup-within-300-seconds, usable-page-within-2-seconds result and preserved private fixture. Obtain actual unfamiliar-contributor guide evidence, distinguish acquisition/build/navigation timing and record environment/revision/limitations in `specs/001-repository-bootstrap/implementation.md`; unavailable reference/contributor evidence is not pass. Prerequisites: T023, T024, T049, T054, T057. (US1.1-4; FR-001-004/017; SC-001/002; C:IX/X/XI/XII)
- [ ] T060 Update `README.md`, `CONTRIBUTING.md`, `docs/README.md`, `docs/architecture.md`, `docs/development/local-development.md`, `docs/development/dependencies.md` and `specs/001-repository-bootstrap/quickstart.md` to describe only the implemented startup/configuration/commands/store/recovery/security limits, exact adopted pins and evidence. Preserve proposed/unresolved decisions, later module/EF/auth/MCP/Kubernetes obligations and all post-MVP exclusions; no spec/MVP/production-ready claim. Prerequisites: T031, T036, T049, T057, T058, T059. (FR-002/009/012/014-017; C:I-XII)
- [ ] T061 Execute the final local gate `python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10`, including every exact quality/child command in quickstart; also run `python3 scripts/check_repository.py`, `python3 -m unittest discover -s scripts/tests -v` and `git diff --check` on the final intended diff. Capture individual suite/command/results, not only wrapper exit, in ignored `artifacts/verification/` and `specs/001-repository-bootstrap/implementation.md`; resolve failed/missing mandatory checks, then re-run affected checks after fixes. Prerequisites: T035, T048, T055, T056, T058, T059, T060. (FR-001-017; SC-001-006; C:IV/VI/IX-XII)
- [ ] T062 Inspect actual required GitHub Actions results for the implementation revision through `.github/workflows/product-checks.yml` and `.github/workflows/repository-checks.yml`; record run URLs, command/job names, results and local/CI coverage parity in `specs/001-repository-bootstrap/implementation.md`. A missing/inaccessible/failed run is not green CI; fix task-caused failures and immediately push each follow-up commit without merging/closing. Prerequisites: T033, T061. (US2.1/3; FR-012/013/017; SC-003; C:X/XI/XII)
- [ ] T063 Review final acceptance coverage in `specs/001-repository-bootstrap/implementation.md`, mapping every US scenario, FR-001-017, SC-001-006 and applicable constitutional principle to actual commands/results/manual evidence and limitations; verify no test fixture table, fault/admin route, fake identity, module SDK, secret, local artifact or unrelated future feature ships. Leave unproved tasks unchecked and block rather than laundering unavailable checks through a report. Prerequisites: T024, T036, T048, T056, T058, T059, T061, T062. (FR-008/014-017; C:I-XII)
- [ ] T064 Reconcile verified task markers and evidence in `specs/001-repository-bootstrap/tasks.md`, `specs/001-repository-bootstrap/implementation.md` and `docs/development/spec-workflow.md`; preserve one-stage delivery and the stable feature branch/draft PR, with required checks re-run after any final relevant edits. Only a fully evidenced implementation may complete its ledger transition; converge, final review and explicit human acceptance remain later invocations, never tasks auto-run here. Prerequisites: T063. (FR-017; C:XI/XII)

## Dependencies and Execution Order

All direct task prerequisites are written in their descriptions. No dependency
is implied merely by line order; T001 has the external analysis/reviewer gate.
Every other prerequisite points to an earlier numbered task. Required tests must
be authored before dependent behavior and pass before the applicable checkpoint.

```text
passing analysis + actual ADR review disposition
  -> T001 -> T002 -> {T003, T004, T005} -> T006
  -> shared configuration/contracts/diagnostics/composition (T007-T012)
  -> US1 tests and local implementation (T013-T024)
  -> US2 provider/verification infrastructure (T025-T036)
  -> US3 recovery/privacy/timing completion (T037-T049)
  -> US4 contract/accessibility completion (T050-T057)
  -> platform/timing/docs/local+CI evidence (T058-T064)
```

This diagram is an incremental delivery summary, not an instruction to serialize
independent tasks: T009/T010, for example, can proceed as their own prerequisites
become ready. US2's runner tests can also start before US1 finishes. The minimal
story dependency graph is **US1 -> US2 infrastructure -> US3 -> US4 final
verification**: US2 supplies real PostgreSQL/harnesses needed for US3; US4's
complete contracts/a11y exercise US3's final state machine. Most US4 test design
can proceed sooner, but its acceptance cannot. US2's all-suites-green acceptance
depends on all runtime stories and T061/T062, not on a partially wired runner.
There is no circular task dependency.

### Parallel Execution Examples

These are ready-task examples, not a request to launch agents or a workflow.
Shared-file changes outside the listed groups remain serialized.

| Scope | Safe parallel group | Ready only after |
| --- | --- | --- |
| Setup | T003 .NET manifests; T004 npm manifests; T005 ignore/editor rules | T002 |
| Foundation | T009 contract workspace; T010 host diagnostics | T006 and T007 as applicable |
| Foundation | T011 host composition; T012 web foundation | Each task's listed prerequisites |
| US1 | T013 SQLite integration tests; T014 health/adapter/page tests; T015 supervisor tests | T006/T008/T009/T011/T012 as applicable |
| US2 | T025 runner unit tests; T026 PG ownership/provider tests | T006/T008/T013 as applicable |
| US3 | T037 store failures; T038 recovery tests; T039 status regression tests; T040 privacy tests | Each task's listed prerequisites; disjoint new test files |
| US4 | T050 full HTTP/adapter contract tests; T051 accessibility/component tests | Each task's listed prerequisites |

Tasks without `[P]` must not be assumed safe to overlap; host composition,
store CLI, supervisor, runner, status component and shared guides are edited by
multiple successive tasks. Never run independent suites against shared default
ports/data; even concurrent test jobs own separate roots/databases/processes.

### Implementation Strategy

Deliver the **US1 local feedback loop first** as an internal increment. It is not
the platform MVP or permission to skip other stories. Then establish US2's real
PG and fail-closed runner, close US3's adverse/recovery/privacy paths, and complete
US4's public-contract/accessibility evidence. Finish with reference timing, both
supported platforms, actual local/CI results and human manual review.

All four stories and required final evidence belong to feature 001's acceptance
scope. The overall Köfte MVP remains inventory **001-024**, not this foundation.

## Constitutional Coverage and Scope

### Requirement and Scenario Coverage

| Requirement / outcome | Task coverage / evidence |
| --- | --- |
| FR-001 host/web/minimal package | T003/T004/T006/T009/T011/T012/T018-T021; T024/T058/T061 |
| FR-002 complete contributor guide | T008/T022/T036/T049/T057/T059/T060 |
| FR-003 explicit safe single start | T015-T018/T021/T023/T024/T042/T059 |
| FR-004 truthful bounded/fresh status | T009/T014/T017/T019/T020/T039/T043/T046/T051/T054/T059 |
| FR-005 live/ready/privacy/budgets | T007/T009/T010/T013/T014/T017/T030/T037/T042/T050/T053 |
| FR-006 OpenAPI/runtime parity | T009/T029/T050/T052/T053/T055/T057 |
| FR-007 actual providers/containment | T013/T016/T023/T026/T027/T030/T037/T038/T041/T045/T048/T058 |
| FR-008 public allowlist/no identity | T011/T014/T019/T020/T050/T052-T055/T063 |
| FR-009 configuration/exposure/refusal | T007/T008/T015/T020-T022/T027/T037/T040/T042/T044/T049 |
| FR-010 correlated/private telemetry | T007/T010/T017/T020/T037/T040/T044/T047/T048/T050/T053 |
| FR-011 responsive/accessibility | T012/T019/T039/T043/T051/T054-T057/T059 |
| FR-012 focused commands and tests | T006/T013-T015/T025-T029/T032-T036/T037-T040/T046-T048/T050/T051/T055/T058/T061 |
| FR-013 same fail-closed CI gate | T025/T028/T029/T032/T033/T035/T047/T061/T062 |
| FR-014 supported pins/license/provenance | T001-T006/T009/T027/T029/T031/T034/T058/T060/T063 |
| FR-015 no app schema/safe lifecycle | T001/T013/T016/T018/T026/T030/T037/T038/T041/T042/T045/T049/T058 |
| FR-016 owned cleanup/reset/data safety | T005/T013/T015/T016/T018/T021/T023/T025-T028/T032/T037/T038/T041/T045/T048/T049/T063 |
| FR-017 truthful docs/handoff | T022/T024/T034-T036/T049/T055-T064 |
| US1.1-4; SC-001/002 | T013-T024; final guide/reference ten-start proof T058/T059 |
| US2.1-4; SC-003 | T025-T036; complete local/CI proof T061/T062 |
| US3.1-4; SC-004/006 | T037-T049; final cross-surface/provider proof T061/T063 |
| US4.1-3; SC-005 | T050-T057, including human T056; final T061/T063 |

### Principles and Justified Exclusions

| Principle | Applicable task evidence / retained boundary |
| --- | --- |
| I Product/brand | T011/T012/T019/T022/T054/T060/T063: Köfte, self-hosted module-free start, no Hoard dependency. |
| II Modular monolith/trust | T003/T011/T018/T060/T063: one host and no fake module/lifecycle/SDK, plugin sandbox or remote hot-loading claim. Manifest/lifecycle/collision proof belongs to 002/004. |
| III Vertical slices/dependencies | T001-T004/T006/T009-T011/T017/T029/T031: feature-local production handlers, .NET test-project-owned source with mirrored feature organization, direct DI, minimal actual contract consumers, no MediatR/AutoMapper, graph/license review. |
| IV Module-owned storage | T013/T016/T026/T027/T030/T037/T038/T041/T045/T048: actual platform-only file/schema, path containment and recovery. Registry immutable allocation, 63-byte/reserved/UNIQUE/concurrent module schema names, module EF contexts/history and physical file-per-module proof require 002-004; no false bootstrap claim. |
| V Honest transactions | T016/T023/T026/T038/T041/T045/T049: only platform-owned test fixtures, no cross-module joins/connection sharing/ACID. No cross-module operation requires outbox/inbox or compensation in 001. |
| VI Identity/safety | T007/T008/T010/T011/T015/T020/T027/T037/T040-T044/T047/T050/T053/T063: closed public surface, fixed loopback fetch, no fake actor, owned cleanup and private diagnostics. OIDC/admin/team authorization and mutation audit are 005/006/009; no relevant team record or business operation exists to simulate. |
| VII Early MCP | T019/T050/T060/T063: no fake MCP endpoint/tools; authenticated discovery remains 007 and domain permission/operation parity 013. External federation/chat stay post-MVP. |
| VIII Durable work/realtime | T060/T063: no resources, events, jobs, providers or subscriptions, so no pretend event contracts/leases/retries/idempotency/SignalR proof; required later in 012/016-019. Browser readiness polling is not a durable job or realtime contract. |
| IX Frontend | T012/T019/T020/T039/T043/T046/T051/T054-T056/T059: mandated minimal UI baseline, honest states, bounded current status and automated/manual accessibility. Full design system/trusted extension SDK remain 008/014; no remotely loaded code. |
| X Portability/operations | T002-T008/T010/T013-T036/T037-T049/T057-T062: reproducible native SQLite/local, real PG verification, telemetry without collector, safe lifecycle and both supported targets. No required cloud, broker, IaC, Kubernetes or Docker Desktop. Production TLS/auth/backup/upgrade packaging and Kubernetes smoke remain 022/024. |
| XI Evidence | All focused validation tasks plus T024/T034/T035/T048/T055/T056/T058/T059/T061-T063: positive/negative, both actual providers, contracts, browser, recovery, privacy, timings and no unavailable-as-pass. No schema ships, so module migration clean-install/upgrade evidence is deferred to schema-bearing 003 and later modules, not waived. Standalone reference-module/release proof remains 020/024. |
| XII Traceability/governance | T001/T022/T024/T036/T049/T057/T060-T064: requirement/scenario mapping, honest exclusions, ADR/human decisions, one feature branch/PR and explicit later acceptance. |

## Completion and Human Handoff

**Task counts**: 64 total; setup 6, foundation 6, US1 12, US2 12, US3 13,
US4 8, cross-cutting 7. The marked parallel groups above are opportunities only.

Task generation advanced **tasks -> analyze** only; the subsequent analysis
passed. Implementation initially blocked at T001, then resumed after the actual
human ADR approval recorded above. T002's exact graph/license/provenance adoption
is next; no product runtime result is claimed. No before/after implementation
hooks are configured.

Implementation must record exact commands/results, revision/environment,
scenario/FR/SC/constitutional coverage and remaining limitations in
`implementation.md`; reports/local data stay ignored. T001's human design review,
dependency adoption and T056/T059 manual evidence must not be fabricated.
An unavailable mandatory check or manual criterion leaves the applicable task
unchecked and implementation incomplete.

Commit stage artifacts and `.specify/drive-state.json` together on
`feature/001-specify-repository-bootstrap`, push every commit immediately and
update the same [draft PR #4](https://github.com/Stonefly-Labs/Kofte/pull/4).
Do not request an intermediate merge or mark the PR ready. Converge, final
evidence-based review and explicit human acceptance are later separate stages;
checked tasks, successful tests, autopilot or a prior merge are not acceptance.
Only that explicit decision after passing review authorizes ready-for-human-merge
handoff; assistants never merge, close, auto-merge or delete the branch.
