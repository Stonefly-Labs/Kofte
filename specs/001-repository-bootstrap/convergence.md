# Convergence: 001 - Repository Bootstrap and Developer Experience

**Result:** pass
**Outcome:** converged
**Date:** 2026-10-21
**Assessed revision:** `3c94de6cc936df78f2111f82971b04244a59e36b`
**Stage:** converge only; not final review or human feature acceptance

## Scope and method

Executed `speckit-converge` after the recorded completed implement stage, using
`SPECIFY_FEATURE_DIRECTORY=specs/001-repository-bootstrap` and:

```bash
.specify/scripts/bash/check-prerequisites.sh --json --require-spec --require-tasks --include-tasks
```

The command resolved this recorded feature directory and its required artifacts.
Intent comes only from `spec.md`, `plan.md` and every task in `tasks.md`, governed
by constitution v2.0.1. Linked contracts, data model, dependency/ADR decisions,
implementation evidence and contributor guides supply supporting detail, not
new scope. No extension hooks are configured.

Inspected current production code, developer tooling, CI and corresponding tests,
not merely checked task markers or the implementation report's coverage claims.
The assessment is bounded to the files/components named by the plan/tasks:
`src/Kofte.Host`, `apps/web`, `packages/bootstrap-contract`, `tests`,
bootstrap scripts/configuration, the PostgreSQL Compose profile, workflows and
their documentation. No branch comparison substitutes for behavior assessment.
Fresh focused checks below supplement the recorded full local/Ubuntu CI evidence.

## Convergence findings

| ID | Gap type | Severity | Source | Evidence | Remaining work |
| --- | --- | --- | --- | --- | --- |
| None | None | None | Assessed inventory below | Current source, tests and actual check outcomes | No actionable convergence task |

| Metric | Checked / result |
| --- | --- |
| Functional requirements | 17/17, FR-001 through FR-017 |
| Buildable success criteria | 6/6, SC-001 through SC-006 |
| User-story acceptance scenarios | 15/15: US1.1-4, US2.1-4, US3.1-4, US4.1-3 |
| Requirements / acceptance criteria total | 38 |
| Plan decisions | 12/12, P01-P12 below |
| Existing tasks, irrespective of checkbox | 64/64, T001-T064 |
| Constitutional principles | 12/12, including justified later-spec applicability |
| Findings by type | missing 0; partial 0; contradicts 0; unrequested 0 |
| Findings by severity | critical 0; high 0; medium 0; low 0 |
| Tasks appended | 0; no empty convergence phase |

## Requirement and acceptance assessment

Paths below are relative to repository root. Test names identify the concrete
checks inspected; fresh execution and historical evidence are distinguished below.

| Intent | Current implementation and evidence |
| --- | --- |
| FR-001 | `Program.cs` composes a single module-free host with native DI; `Features/Bootstrap` owns the health slice. The Next page and narrow shared contract have real web/contract consumers. `Kofte.slnx` includes the host and dedicated test projects, not speculative platform assemblies. |
| FR-002 | `docs/development/local-development.md`, `CONTRIBUTING.md` and `quickstart.md` document exact tools, locked restore, separate initialization, one start command, addresses/configuration, persistence, stop/restart, verification and corrective guidance. T059's human guide attestation remains distinct from automation. |
| FR-003 | `scripts/dev.py` preflights tools/configuration/ports, builds restored projects, owns host/web groups and probes actual readiness. `StoreCommand` makes initialization explicit; `SqlitePlatformStore.OpenCore` never creates a missing database. Supervisor tests cover failed and partial startup. |
| FR-004 | `useBootstrapStatus.ts` enforces a ten-second absolute attempt, thirty-second refresh-start cadence, cancellation, stale-tab revalidation and retry deduplication. `BootstrapStatus.tsx` shows truthful ready/loading/unavailable/stale text and no-feature scope. Hook and browser tests exercise failures and late responses. |
| FR-005 | `HealthEndpoints` separates live/ready, rejects incomplete startup, sets no-store and budgets the endpoint. `HealthHandler` reprobes the selected provider with a three-second budget; real missing-file, lock, privilege and PostgreSQL outage tests preserve live 200 versus ready 503 and recovery. |
| FR-006 | `BootstrapOpenApi` generates Development-only OpenAPI from native metadata with API 1.0.0, OpenAPI 3.1.1, anonymous policy and strict response schemas. Native and HTTP tests compare the entire normalized runtime document; `scripts/check-contract.mjs` checks baseline/type drift. |
| FR-007 | `UnixStorePaths` rejects linked/nonregular/multiply-linked files and unsafe roots before opening; SQLite uses fixed `platform/platform.db`. `PostgresFixture` creates and records a new database, scoped role and `kofte_system`. Actual provider tests round-trip exact fixtures on fresh connections; no shipped module registry or `public` fixture data. |
| FR-008 | `HealthEndpoints` enforces the exact three-route host allowlist with empty invalid/unknown/unsupported responses; documentation is absent outside Development. The fixed web adapter exposes no store mutation. Contract tests exercise absent admin/test/MCP routes and body/query/method misuse. No fake identity/domain operation exists. |
| FR-009 | `BootstrapConfiguration`, supervisor configuration and server-only `backendConfiguration.ts` reject unknown/invalid keys, embedded credentials, unsafe destinations and unsupported provider/profile pairs. Broader binding is explicit and warned. Failures do not switch providers; guide diagnostics identify corrective steps. |
| FR-010 | `BootstrapDiagnostics` emits allowlisted codes and trace correlation without raw exception/configuration output. `BootstrapTelemetry` enables vendor-neutral tracing/metrics with optional OTLP. Host framework log providers are cleared; supervisor streams are drained rather than echoed. Native tests verify safe trace fields without request baggage/tracestate. |
| FR-011 | Semantic page/status, minimal Tailwind/shadcn-style/Radix/Lucide button, visible focus, polite meaningful announcements, responsive text and reduced motion are implemented. Current browser/axe tests cover loading/ready/unavailable at 320/768/1440 and 200%-text reflow. Human T056 is separately attested, not inferred from axe. |
| FR-012 | Root/workspace scripts and `scripts/verify.py` expose actual formatting/lint/types/build/unit/provider/contract/browser commands. Required script inventory and minimum native test count prevent silent empty selections. Fresh unit, provider, contract and browser groups pass. |
| FR-013 | Product CI has ten named jobs invoking the same local suite commands; repository CI runs its three mandatory checks. Both are read-only, with pinned actions and no continue-on-error. The controlled-failure harness requires green baselines, exact named nonzero commands and an isolated source checkout. Current-head CI has eleven successful contexts. |
| FR-014 | `global.json`, central package props, npm manifests/strict engine configuration, three NuGet locks, npm lock, image digest and action SHAs pin the reviewed graph. `check_dependencies.py` reconciles original/restored archives, metadata, graph and notices. Fresh check verifies 403 npm and 69 NuGet identities. No prohibited mandatory dependency was found. |
| FR-015 | No application schema, EF context or migration history ships. SQLite backup metadata/integrity guards and PostgreSQL fixture-only matching-tool backups reject incompatible/corrupt inputs. Store/recovery tests prove unchanged restart, exact restored fixtures and refusal without replacing existing data. Module migrations remain later work. |
| FR-016 | Host/store leases guard SQLite lifecycle commands; reset and replacement need separate confirmations and a current backup. PostgreSQL recovery uses owned-target/advisory/admission guards. Fixtures revalidate exact resource ownership before teardown. Supervisor stop/retry never resets data; refusal tests preserve outside/existing sentinels. |
| FR-017 | README, contributor/local/dependency/architecture/workflow docs identify implemented bootstrap boundaries and actual evidence without claiming authentication, module isolation, MCP, Kubernetes, production readiness, spec acceptance or MVP completion. Historical design-stage text is not treated as a current runtime claim. |
| SC-001 | Explicit one-command supervisor readiness is bounded below 300 seconds, with actual prior supervisor exercises and T059's human-confirmed unfamiliar-contributor/reference guide task in `implementation.md`. Initial acquisition is separate; no undocumented cloud prerequisite is introduced. |
| SC-002 | `startup.spec.ts` measures built-runtime readiness/page usability, checks all exact fixture values after each owned stop/restart and verifies released ports. The wrapper validates exactly ten samples for the full gate. Prior final local/CI ten-cycle evidence is retained; no new reference-machine measurement is claimed. |
| SC-003 | Actual final local all-suite evidence and current-head successful CI support the unchanged foundation. Failure harness injects test/format/build failures and unavailable PG/Chromium, verifies exact failing stages and retains only safe child evidence. Fresh targeted checks are not relabeled a complete gate. |
| SC-004 | Actual selected-store loss, host stop/restart and hanging/malformed response journeys exercise bounded failure, stale-response rejection and retry recovery. Fake-clock hook tests verify the exact ten-/thirty-second deadlines and no retained green state after failure. |
| SC-005 | Current browser checks pass keyboard/focus/textual state, viewport, reflow and zero serious/critical axe gates. T056's explicit human completion attestation supplies manual review, without invented granular observations or a blanket automated WCAG claim. |
| SC-006 | Both-provider recovery preserves exact fixtures; native wrong-credential/startup tests and transient HTTP/browser/runtime/supervisor canary scans cover defined privacy failures. Fresh real-provider/contract/browser output scans pass; historical full privacy/recovery evidence remains in `implementation.md`. |

| Acceptance scenarios | Concrete behavior assessed |
| --- | --- |
| US1.1 | `dev.py` real host/web readiness; `bootstrap.spec.ts` and the current named browser group verify the Köfte/bootstrap-only page. |
| US1.2 | Default native SQLite profile with no module/cloud/Kubernetes dependency; real file/probe and private startup fixtures. |
| US1.3 | `ExplicitInitPersistsFixturesAcrossNewConnectionsAndProbes`, `startup.spec.ts` exact warm-restart fixture checks and non-overwriting init. |
| US1.4 | Supervisor tests for exact missing/wrong tools, occupied ports, partial child launch, early exit, timeout and owned cleanup; documented corrective retry. |
| US2.1 | Named local commands and actual successful repository/product CI; fresh targeted execution below. |
| US2.2 | `OwnedLimitedRoleRoundTripsOnFreshConnectionsAndProbes`, real PostgreSQL readiness/outage/recovery, ownership-refused cleanup and newly owned database provisioning. |
| US2.3 | `test_failure_propagation.py` requires exact nonzero test/formatter/build/unavailable-PG/missing-Chromium outcomes in a separate checkout. |
| US2.4 | Locked graph/manifests, strict baseline checks and reviewed dependency reconciliation; historical no-drift restore evidence and current CI. |
| US3.1 | Real selected-store loss preserves liveness and clears healthy display; current provider/adapter/browser tests observe outage and recovery. |
| US3.2 | Actual stopped-host and hanging/late-response browser journeys plus controlled transfer/body timeout, deduplication and stale-response hook regressions. |
| US3.3 | Restricted-user missing/unwritable/corrupt/link/lock startup tests; wrong PG credentials and privileges; no fallback or replacement; verified recovery fixtures. |
| US3.4 | Native credential errors, privacy HTTP/UI/console and transient host/web/supervisor capture tests; allowlisted diagnostics and correlations. |
| US4.1 | Current both-provider generated OpenAPI equality, direct/adapter 200/503/400/404/405 and Production/Staging documentation-absence tests. |
| US4.2 | Current all-state keyboard/focus/viewport/axe/reflow checks and explicit human T056 attestation, with manual-evidence limitations retained. |
| US4.3 | Page scope copy and browser/component assertions show no fabricated modules, business records, login or tools. |

## Plan and constitutional assessment

| Plan decision | Current evidence |
| --- | --- |
| P01 - Minimal host, local slice, shared contract and test ownership | One native-DI composition root; production health slice; actual web/contract consumers; .NET source physically inside dedicated test projects with ordinary SDK inclusion. |
| P02 - Supported versions/targets and reviewed locked adoption | Exact central/npm/SDK pins, reviewed graph/notices, supported macOS execution and actual Ubuntu CI; native Windows remains unsupported. Accepted ADR 0002 and human-directed ADR 0003 preserve consequential decisions. |
| P03 - Native attached supervision | Tool/config/port preflight, bounded builds/readiness and owned process-group shutdown in `dev.py`; no install, port takeover or store reset. |
| P04 - Canonical platform-only SQLite | Fixed suffix, strict owner/type/path/sidecar checks, explicit init, open-existing mode, foreign keys, rollback journaling and fresh transaction probe. |
| P05 - Disposable real PostgreSQL | Pinned image, loopback service, fresh random database/role, persistent ownership records/comments/OIDs, limited role/schema and exact owned cleanup. |
| P06 - Safe lifecycle/recovery | Stopped-host leases, provider backup APIs/tools, private verified backup pairs, nonempty-target refusal or separately confirmed backed-up SQLite replacement, no unsafe downgrade. |
| P07 - Minimal versioned generated HTTP surface | Native generated Development OpenAPI, exact anonymous routes/statuses/fields/no-store, negative methods/inputs and non-Development absence. |
| P08 - Fixed server-only readiness adapter | Validated matching loopback target, no forwarded caller credentials, strict status/schema/type validation, five-second transfer/body deadline and minimal explicit failure. |
| P09 - Fresh bounded browser state | Ten-second deadline, thirty-second start cadence, cancellation, late-result ordering, suspension/refocus and safe retry. |
| P10 - Accessible honest UI | Minimal mandated design baseline, semantic/textual states, stable keyboard focus, meaningful announcements, responsive/reflow/reduced-motion and separate manual evidence. |
| P11 - Private diagnostics and opt-in telemetry | Allowlisted correlated logs/trace tags, no raw child/framework output, no required collector, deliberate trusted OTLP endpoint. |
| P12 - Fail-closed verification/local-CI parity and operational handoff | Required script/test inventory, ten groups, real fixtures/controls/scans, safe reports, failure-injected isolated clone, read-only CI, complete actual guide and preserved human gates. |

All twelve principles were evaluated, not skipped as a template. I/II/III are
satisfied by branded, module-free, native-DI slice-local code, minimal actual
sharing, dedicated test source and reviewed dependencies. IV/V apply to the
platform file/schema and honest local ownership/transactions; module registries,
contexts/migration history and cross-module coordination are excluded, not
claimed proven. VI applies through closed diagnostic surfaces, configuration/
path/output safety and owned operations; OIDC/team authorization/domain audit
remain 005/006/009. VII's authenticated MCP remains 007/013 with no fake tools.
VIII's resources/events/jobs/providers/SignalR are absent by approved scope.
IX is covered by the current UI and automated/manual evidence. X/XI are covered
by portable local/real-provider/contract/browser/recovery/privacy/CI checks,
documented persistence and supported tools; reference-module/Kubernetes/release
proof remains 020/022/024. XII/governance preserve approved 001 intent, exact MVP
scope, ADRs and one stage/branch/draft PR with separate explicit acceptance.
No MUST violation or unrelated future feature was identified.

## Every existing task assessed

These groups exhaust T001-T064; markers were not used as behavioral proof.
Historical prerequisite approval/evidence tasks are assessed as decisions and
recorded executions, not as newly performed human work.

| Tasks | Current source or execution/decision evidence inspected |
| --- | --- |
| T001-T002 | Accepted ADR 0002 with actual quoted human approval; dependency adoption record, complete inventory/locks/notices and fresh dependency checker. |
| T003-T006 | Solution/SDK/build/central-package files, strict npm workspaces, ignores/editor rules, dedicated native/unit/contract/E2E projects and reviewed locks. |
| T007-T012 | Configuration/diagnostic tests and parsers, exact shared guards, safe telemetry, native composition, Next layout/config/CSS and minimal button. |
| T013-T018 | Actual SQLite init/probe/reconnect/failure tests, live/ready native/HTTP tests, supervisor regressions and explicit CLI/startup wiring. |
| T019-T024 | Actual status hook/component/adapter, supervisor, guide, private startup fixtures and recorded guide/host/page exercises; later adversarial corrections remain in current behavior. |
| T025-T031 | Runner/script/cleanup failure tests, real PostgreSQL ownership/provisioning/probes, named verification, actual quality scripts and graph/archive/notice reconciliation. |
| T032-T036 | Controlled-failure isolated checkout, read-only pinned named CI, recorded locked real-provider/quality/failure executions and documented suite correspondence. |
| T037-T044 | Actual selected-store/permission/path/outage/refusal tests, guarded native recovery, deadline/stale state regressions and private output/telemetry implementations. |
| T045-T049 | Matching-image PG dump/restore with exact fixtures, actual store/host browser controls, transient privacy capture/scanning, passing recorded executions and actionable recovery guide. |
| T050-T057 | Full generated/runtime/adapter contract tests and strict routes; accessible status/reflow/focus/announcement code/tests; actual browser/contract execution and human T056 attestation; documented interfaces. |
| T058-T059 | Supported macOS and Ubuntu automated evidence, ten-start exact-fixture/timing records and explicit human T059 completion. Actual CI hardware is not relabeled the reference machine. |
| T060-T064 | Reconciled guides, final full local gate and actual CI/parity evidence, explicit 15-scenario/23-requirement/12-principle coverage, completed implement ledger and original branch/draft PR handoff. |

## Fresh verification and retained evidence

The default shell initially selected unsupported versions: `dotnet --version`
failed SDK resolution, and Node/npm were 24.10.0/11.6.1. Selected the already
available, previously checksum-verified isolated SDK/Node installations without
installing tools, changing global configuration or relaxing tracked pins.
Selected versions are SDK 10.0.402, Node 24.22.0 and npm 11.19.1.
Current execution is Darwin 25.5.0 arm64, 18 logical CPUs, 68,719,476,736 physical
memory bytes and Python 3.15.0, not the Ubuntu timing reference.

| Exact command | Actual outcome |
| --- | --- |
| `python3 scripts/verify.py --suite unit` | Passed, exit 0; all 8 steps: native/web/shared/Python units and baseline guards. |
| `python3 scripts/verify.py --suite integration --provider both --disposable-postgres` | Passed, exit 0; all 5 SQLite and 6 PostgreSQL steps, with real storage/recovery/failure/ownership cases. |
| `python3 scripts/verify.py --suite contract --provider both --disposable-postgres` | Passed, exit 0; all 30 steps, generated/actual HTTP/adapter policy, real provider outages, output scans and owned cleanup. |
| `python3 scripts/verify.py --suite browser` | Passed, exit 0; all 16 steps, actual healthy/failure/retry/recovery and all-state viewport/keyboard/axe/reflow checks. |
| `python3 scripts/check_dependencies.py` | Passed, exit 0; 403 npm / 69 NuGet reviewed identities and restored artifact/notice reconciliation; not a new signature audit. |

All five fresh safe reports under ignored `artifacts/verification/` were
inspected: **65/65 named steps passed**, `scope: targeted`,
`completeGate: false`, assessed revision above. Reports:

- `unit-none-2865eb4a0a78478592b398a5df35a1a1.json`
- `integration-sqlite-ae30d082d6f44662992579a17035e3c3.json`
- `integration-postgres-00e7e0da4a6248b2bc6b50be268ee6ca.json`
- `contract-both-5bc4e26b7228400dba874ca6d67abdd4.json`
- `browser-none-208296d34bcf4746a7e6f0d59d7e4de0.json`

Also inspected actual assessed-head GitHub run status:
[repository 39646328655](https://github.com/Stonefly-Labs/Kofte/actions/runs/39646328655)
and [product 39646328820](https://github.com/Stonefly-Labs/Kofte/actions/runs/39646328820)
are completed/successful, with every one of the ten product jobs successful.
The prior final full local gate, 170-step local/CI parity, standalone privacy/
recovery/startup/failure-propagation executions and human decisions remain
in [`implementation.md`](implementation.md); they were not fabricated as new
executions in this stage. Repository publication checks are recorded in PR #4.

## Outcome and limitations

Converged: the assessed implementation satisfies the specified bootstrap scope.
`tasks.md` remains byte-for-byte unchanged, SHA-256
`da856b91d37eb20e2cc5659d010830b4864f01a18a02d2e978c986d260d47f83`;
no phase or remediation task was appended. `spec.md`, `plan.md` and product
source/configuration/tests were not edited.

T056/T059 remain explicit human completion attestations, not granular reviewer
notes or invented reference measurements. Automated accessibility is not a full
manual WCAG assessment; macOS/CI timing is not evidence of equivalent reference
hardware. No independent human observations or final acceptance were obtained
by this stage. Production/authentication/module-isolation/MCP/Kubernetes and
later MVP release obligations remain outside 001.

Next invocation: **review**, on `feature/001-specify-repository-bootstrap` and
the same [draft PR #4](https://github.com/Stonefly-Labs/Kofte/pull/4).
No further implement pass is indicated by convergence. Final review must still
assess evidence independently; only a subsequent explicit human acceptance can
authorize a ready-for-merge handoff. Do not merge between stages.
