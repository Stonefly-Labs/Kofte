# Final Review: 001 - Repository Bootstrap and Developer Experience

**Result:** pass
**Date:** 2026-10-21
**Reviewed revision:** `4f708c98ccf81597914556bbe89db399ec7cf6cf`
**Constitution:** v2.0.1
**Stage:** review only; not numbered-feature acceptance or merge authorization

## Disposition and method

The independent `spec-review` agent found no high-confidence implementation
defect, constitutional conflict or unrequested scope expansion. The caller
completed the agent's outstanding execution/provenance checks below before
recording this passing disposition. All 17 functional requirements, six success
criteria and 15 acceptance scenarios have concrete code/test evidence within
approved bootstrap scope. No remediation task is needed.

The reviewer read the canonical inputs, constitution, spec, plan, all task phases,
research, data model, contracts, quickstart, ADRs and implementation/convergence
evidence. It inspected actual host/storage/recovery, web/adapter/status,
shared-contract, test, supervisor, verifier, dependency and workflow source;
checkboxes and earlier passing summaries were not treated as behavioral proof.
The reviewer was read-only and could not execute commands or query GitHub.
The caller owns the fresh commands, raw-report reconciliation and actual CI
inspection below. No extension hooks are configured; the bundled multi-stage
workflow was not invoked.

This is a working module-free .NET/Next.js bootstrap, not merely reserved
directories or repository conventions. Conversely, reserved modules, SDKs,
workers and Helm are not implemented features. Configured assistant MCP tools
are developer tooling, not a product MCP server or proof of connectivity.

## Findings

| Severity | Finding | Disposition |
| --- | --- | --- |
| None | No high-confidence acceptance defect or constitutional violation found | Pass; no implementation remediation |

`tasks.md` remains unchanged, with all T001-T064 evidenced and SHA-256
`da856b91d37eb20e2cc5659d010830b4864f01a18a02d2e978c986d260d47f83`.
No task or empty remediation phase was appended. Product source, tests,
configuration, locks, `spec.md` and `plan.md` are unchanged by this review.

## Functional-requirement evidence

Paths are relative to repository root. Source/test citations identify inspected
behavior; actual fresh execution is recorded separately below.

| Requirement | Implementation and concrete test evidence |
| --- | --- |
| FR-001 - Minimal working host/web/shared package | `src/Kofte.Host/Program.cs:17-51` composes native DI and the selected store without modules; `apps/web/app/page.tsx:3-29` renders the honest bootstrap; `packages/bootstrap-contract/src/status.ts:7-42` has real consumers. `tests/e2e/bootstrap.spec.ts:3-20` exercises the actual page. |
| FR-002 - Contributor guide | `docs/development/local-development.md` covers exact tools/restore, explicit init, one start command, stop/restart, recovery, privacy, PostgreSQL, named checks and corrective guidance. `quickstart.md` retains the validation contract. T059 is human-attested, not an independently observed contributor exercise. |
| FR-003 - Explicit persistent fail-closed startup | `scripts/dev.py:139-184,294-366` preflights/builds/probes and owns cleanup. `src/Kofte.Host/Bootstrap/SqlitePlatformStore.cs:10-73` separates init from open-existing. `tests/integration/Kofte.Host.IntegrationTests/StoreCommandTests.cs:11-48` and `scripts/tests/test_dev.py:94-181` exercise refusal and failed startup. |
| FR-004 - Truthful bounded fresh UI status | `apps/web/features/bootstrap/useBootstrapStatus.ts:23-115` enforces ten-second attempts, thirty-second refresh-start cadence, cancellation and stale-tab behavior; `BootstrapStatus.tsx:7-49` renders states/retry. Hook regressions at `useBootstrapStatus.test.tsx:20-153` and `tests/e2e/recovery.spec.ts:4-99` cover ordering, failure and recovery. |
| FR-005 - Independent live/ready | `src/Kofte.Host/Features/Bootstrap/HealthEndpoints.cs:16-59` handles exact routes, startup, no-store and endpoint budget; `HealthHandler.cs:12-44` freshly probes with a three-second budget. `tests/unit/Kofte.Host.Tests/Bootstrap/HealthTests.cs:17-66` and `tests/integration/Kofte.Host.IntegrationTests/HealthEndpointTests.cs:12-63` cover actual status behavior. |
| FR-006 - Runtime-generated OpenAPI | `src/Kofte.Host/Features/Bootstrap/BootstrapOpenApi.cs:10-168` generates strict native schemas and anonymous policy; `Program.cs:46-51` limits documentation to Development. `tests/contract/bootstrap.test.ts:47-58`, `tests/integration/Kofte.Host.IntegrationTests/OpenApiTests.cs:12-79` and `scripts/check-contract.mjs:10-45` check complete runtime equality, non-Development absence and type/baseline drift. |
| FR-007 - Actual providers and platform ownership | `src/Kofte.Host/Bootstrap/SqlitePlatformStore.cs:7-84` uses the fixed file and fresh transaction probe; `UnixStorePaths.cs:12-70` validates canonical components, ownership and file/link types. `tests/integration/Kofte.Host.IntegrationTests/PostgresFixture.cs:33-163` provisions newly owned resources and a limited `kofte_system` role. That project's `SqliteStoreTests.cs:23-75` and `PostgresStoreTests.cs:37-66` round-trip real data. |
| FR-008 - Narrow anonymous surface | `src/Kofte.Host/Features/Bootstrap/HealthEndpoints.cs:16-43` allowlists routes and empty invalid/unknown/unsupported responses. `apps/web/app/backend/health/ready/route.ts:9-81` is fixed, server-only and credential-free. `tests/contract/bootstrap.test.ts:60-109` and `adapter.test.ts:26-52` cover negative access/inputs. No fake identity, domain/admin/fault/MCP operation ships. |
| FR-009 - Strict configuration/exposure | `src/Kofte.Host/Bootstrap/BootstrapConfiguration.cs:23-96` rejects unknown keys, invalid profiles/URLs/roots and unsupported provider selection; no fallback exists. `apps/web/features/bootstrap/backendConfiguration.ts:3-52` restricts matching loopback targets. `tests/unit/Kofte.Host.Tests/Bootstrap/ConfigurationTests.cs:9-66` and `scripts/tests/test_dev.py:14-64` exercise invalid and unsafe configurations. |
| FR-010 - Private diagnostics and optional telemetry | `src/Kofte.Host/Bootstrap/BootstrapDiagnostics.cs:19-49` allowlists codes/correlation; `BootstrapTelemetry.cs:15-33` supplies vendor-neutral telemetry with opt-in export; `Program.cs:33-35` clears framework log providers. `tests/unit/Kofte.Host.Tests/Bootstrap/HealthTests.cs:70-93`, `tests/contract/privacy.test.ts:9-43`, `tests/e2e/privacy.spec.ts:4-48` and `scripts/runtime_support.py:104-159` cover safe fields, actual output capture and canary scanning. |
| FR-011 - Accessible responsive bootstrap | `apps/web/features/bootstrap/BootstrapStatus.tsx:22-48` preserves semantic/textual status and keyboard focus; `apps/web/app/globals.css:17-89` handles responsive/reduced-motion behavior. `tests/e2e/accessibility.spec.ts:4-90` checks all states, keyboard, viewport, axe and reflow; `BootstrapStatus.test.tsx:15-87` checks meaningful announcements. Manual completion is separately attested. |
| FR-012 - Repeatable named checks | `scripts/verify.py:33-75,776-891` requires workspace scripts and concrete native/web/shared/Python commands with nonempty native selections. `scripts/tests/test_verify.py:39-74,147-198` checks missing tools/scripts and real child failures. Fresh complete execution passes below. |
| FR-013 - Fail-closed CI | `.github/workflows/product-checks.yml:3-103` runs ten named common local suites on PR/main with read-only permissions and pinned dependencies; repository CI is separate. `tests/integration/tooling/test_failure_propagation.py:217-337` exercises actual test/format/build/unavailable-PG/missing-Chromium failures. Actual job/report evidence is independently inspected below. |
| FR-014 - Pinned reviewed dependencies | `global.json:1-4`, `Directory.Packages.props:2-35`, `package.json:12-18`, `.npmrc:1-5`, locks, image digest and action SHAs pin the baseline. `scripts/check_dependencies.py:169-410` reconciles identities/graphs/archives/notices; `docs/development/dependencies.md` records adoption/provenance/obligations. Fresh quality reconciliation passes; no prohibited mandatory dependency was found. |
| FR-015 - Honest compatibility/recovery | No application schema, EF model or migration history is shipped. `src/Kofte.Host/Bootstrap/SqliteStoreRecovery.cs:88-212` verifies backup metadata/integrity and guards replacement; `tests/integration/Kofte.Host.IntegrationTests/PostgresRecovery.cs:99-180` uses matching image tools and owned targets. That project's `StoreRecoveryTests.cs:34-183` exercises actual restore/refusal. Future module migrations are not claimed proven. |
| FR-016 - Non-destructive stop/retry and guarded cleanup | `src/Kofte.Host/Bootstrap/StoreLease.cs:11-42`, `StoreCommand.cs:10-30` and `SqliteStoreRecovery.cs:215-225` require exclusive ownership and explicit destructive confirmation. `tests/integration/Kofte.Host.IntegrationTests/PostgresFixture.cs:184-224` revalidates exact teardown ownership; `PostgresOwnershipTests.cs:9-37` and `StoreRecoveryTests.cs:66-93` preserve outside/existing resources on refusal. |
| FR-017 - Honest docs/handoff | README, contributor/local/workflow guides distinguish running bootstrap from future features, production readiness and acceptance. `implementation.md:554-570` preserves actual human attestations and reference limitations. This report and the driver advance only review, leaving explicit numbered-feature acceptance outstanding. |

The dedicated .NET test projects use normal SDK inclusion and project references,
not production-linked test source:
`tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj:1-13`,
`tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj:1-15`.
Production `src/Kofte.Host/Kofte.Host.csproj:1-12` contains no linked tests or
compensating test exclusions. `Kofte.slnx:1-5` includes the host and those two
test projects; the source ownership correction is preserved.

## Acceptance-scenario and success-criterion evidence

| Scenario | Concrete inspected coverage |
| --- | --- |
| US1.1 | Actual supervisor readiness in `scripts/dev.py:294-322`; branded ready-page smoke in `tests/e2e/bootstrap.spec.ts:3-20`. |
| US1.2 | Module/cloud-free default in `BootstrapConfiguration.cs:39-77`; real SQLite round trip in `SqliteStoreTests.cs:23-43`. |
| US1.3 | Exact persistent fixtures and released ports across owned cycles in `tests/e2e/startup.spec.ts:130-284`; non-overwriting init/reconnect in `SqliteStoreTests.cs:23-43`. |
| US1.4 | Missing/wrong tools, occupied ports, partial launch and interrupt in `scripts/tests/test_dev.py:55-181`; documented corrective retry. |
| US2.1 | Concrete common commands in `scripts/verify.py:61-75,787-891` and `.github/workflows/product-checks.yml:22-59,92-103`; fresh local and actual CI evidence below. |
| US2.2 | Owned real PostgreSQL in `PostgresFixture.cs:33-163`; limited-role persistence in `PostgresStoreTests.cs:37-66`; refusal in `PostgresOwnershipTests.cs:9-37`. |
| US2.3 | Green baselines and exact named failure validation in `tests/integration/tooling/test_failure_propagation.py:162-337`; all five fresh expected failures verified below. |
| US2.4 | Locked restores in `scripts/verify.py:65-69`; exact graph/archive reconciliation in `scripts/check_dependencies.py:169-350`; fresh execution leaves locks/source unchanged. |
| US3.1 | Real selected-store loss/no fallback in `StoreFailureTests.cs:92-137`; direct/adapter parity in `tests/contract/adapter.test.ts:55-88`; visible outage in `tests/e2e/recovery.spec.ts:4-30`. |
| US3.2 | Transfer/body timeout and stale ordering in `useBootstrapStatus.test.tsx:36-66,91-153`; actual stop/restart/hang/retry in `tests/e2e/recovery.spec.ts:32-99`. |
| US3.3 | Restricted-user/path/type failures in `StoreFailureTests.cs:13-81`; credentials/privilege refusal in `:92-137`; protected restoration in `StoreRecoveryTests.cs:34-183`. |
| US3.4 | HTTP canaries in `tests/contract/privacy.test.ts:9-43`; UI/console in `tests/e2e/privacy.spec.ts:4-48`; supervisor capture in `scripts/tests/test_dev_output.py:27-64`; runtime scan in `scripts/runtime_support.py:134-159`. |
| US4.1 | Complete generated document equality and negative route/input policy in `tests/contract/bootstrap.test.ts:47-109`; both-provider non-Development documentation absence in `OpenApiTests.cs:34-79`. |
| US4.2 | All-state keyboard/focus/viewport/axe/reflow in `tests/e2e/accessibility.spec.ts:4-90`; announcements in `BootstrapStatus.test.tsx:15-87`; separately attested human T056. |
| US4.3 | Honest no-features text in `apps/web/app/page.tsx:15-29`; actual browser assertion in `tests/e2e/bootstrap.spec.ts:11-13`. |

Unqualified native test basenames above are inside
`tests/integration/Kofte.Host.IntegrationTests/`; bootstrap web basenames are
inside `apps/web/features/bootstrap/`.

| Success criterion | Evidence and limits |
| --- | --- |
| SC-001 | Supervisor preflight/actual-ready deadline is under five minutes (`scripts/dev.py:25,139-184,294-366`). T059 records human completion of the guide/reference exercise; this review did not observe an unfamiliar contributor or invent reference-machine measurements. |
| SC-002 | `startup.spec.ts:225-284` checks ten warm starts, usable page under two seconds and exact fixtures; `scripts/verify.py:712-760` validates samples. All ten fresh samples pass below on the actual macOS machine, not substituted reference hardware. T059 remains separately attested. |
| SC-003 | All ten fresh local automated groups and all 170 named steps pass; all ten assessed-source CI jobs and matching ordered steps pass. The harness verifies five exact deliberate nonzero outcomes, not silent skips. |
| SC-004 | Exact ten-/thirty-second unit assertions and real store/host/hanging-response browser journeys pass, including stale-response rejection, safe retry and recovery. |
| SC-005 | All-state keyboard/focus/text/viewport/reflow/axe and announcement checks pass. T056 is human-confirmed; automation is not a blanket manual WCAG claim. |
| SC-006 | Fresh real-provider persistence/recovery and seeded HTTP/UI/console/host/web/supervisor privacy checks pass. Raw captures and seeded values are not retained in committed reports. |

## Constitutional assessment

| Principle | Applicable evidence or explicit later-spec boundary |
| --- | --- |
| I | MIT, Köfte branding, module-free host and no compulsory Hoard/cloud dependency preserved. |
| II | Small host, no invented module lifecycle/registry/sandbox/hot-loading claim. Identity, contribution and collision proofs remain 002-004 and extension specs. |
| III | Native DI/direct handler, local production slice, demonstrated shared contract, reviewed dependency graph and physically test-project-owned .NET source. |
| IV | Actual platform SQLite canonical-file and PostgreSQL reserved-schema ownership checked. Module allocator collisions/concurrency/63-byte naming, registry constraints, EF contexts and schema-local migrations remain 003; no module or application table exists to migrate here. |
| V | No cross-module tables, connection reuse or ACID promise. No domain side effect requires outbox/inbox/compensation in this bootstrap. |
| VI | Narrow anonymous diagnostic surface, strict configuration/input/path privacy and owned destructive guards apply. OIDC, first-admin and actor/team/resource authorization/audit remain 005/006/009; future domain routes receive no exemption. |
| VII | Product MCP identity, collisions, authenticated discovery, invocation authorization and destructive safety remain 007/013; configured developer tools do not satisfy them. |
| VIII | No domain resources, events, jobs, providers or subscriptions. Their status ownership, replay/retry/cancellation/restart and SignalR obligations remain later specs; health polling is not durable/realtime work. |
| IX | Mandated minimal frontend baseline and truthful accessible states with automated and separately attested human evidence. Full shell and trusted extension SDK remain 008/014. |
| X | Native local SQLite, real disposable PostgreSQL, pinned/configured tools, fresh probes, optional OTel and guarded recovery. Kubernetes/TLS/production packaging remain 022 and related security specs, not current readiness claims. |
| XI | Fresh unit, actual-provider, contract, browser, recovery, privacy, startup and failure-propagation evidence. Reference module, cross-team/MCP/job and Kubernetes release proofs remain 020/022/024, not falsely passed. |
| XII / governance | Approved 001 scope, MVP 001-024, no post-MVP expansion. One stage on original branch/draft PR; explicit human feature acceptance remains outstanding. |

ADR 0002's recorded human approval and ADR 0003's source-ownership correction
are preserved. No unresolved bootstrap design/scope dispute was identified.

## Fresh caller-owned verification

The default shell exposed the older SDK/Node/npm, so the caller selected already
available, previously checksum-verified isolated tools: SDK 10.0.402, runtime
10.0.13, Node 24.22.0 and npm 11.19.1. No tool installation, global configuration
change or relaxed pin was needed. Actual execution: Darwin 25.5.0 arm64,
18 logical CPUs, 68,719,476,736 memory bytes, Python 3.15.0.

| Exact command | Actual outcome |
| --- | --- |
| `SPECIFY_FEATURE_DIRECTORY=specs/001-repository-bootstrap .specify/scripts/bash/check-prerequisites.sh --json --require-spec --require-tasks --include-tasks` | Passed; resolved recorded directory/spec/tasks. |
| `python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10` | Passed, exit 0; all ten groups and 170 named steps. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed; 100 tests. |
| `git diff --check` | Passed. |

Every child of these fresh ignored `artifacts/verification/` reports was
reconciled: reviewed revision above, `scope: all`, `completeGate: true`,
`outcome: passed`, all step outcomes passed:

| Group | Safe report basename | Steps |
| --- | --- | ---: |
| quality | `quality-none-a7bf38244fbb4f5daacd8e54b9780270.json` | 17 |
| unit | `unit-none-ffc38ebb49fe477496c5f2a03d8780cf.json` | 8 |
| integration / SQLite | `integration-sqlite-460cc37f81a04c5099cd3494d105bdb4.json` | 5 |
| integration / PostgreSQL | `integration-postgres-2d4593ad3a3b4a9997f4d3b2ae95f730.json` | 6 |
| recovery | `recovery-both-b7157e916f874a48b947571b37b2c481.json` | 32 |
| contract | `contract-both-14b6a41a0cca4f6a8660d90866ae0b06.json` | 30 |
| browser | `browser-none-fd09beec655e400bad67717bd336d308.json` | 16 |
| privacy | `privacy-both-35f55b9288e84e7199bf11fc68ed9001.json` | 30 |
| startup | `startup-none-070b107dd3b741b38a95475f7c33e14a.json` | 8 |
| failure-propagation | `failure-propagation-none-97532d1caf2e4e71afb8c5f2d36bc9b2.json` | 18 |

The ten validated startup observations all have `fixtureMatches: true`;
readiness is 0.364664-0.423371 seconds and page usability 0.023974-0.059548
seconds. These are actual built-runtime warm samples, excluding initial
acquisition/build, not the Ubuntu reference exercise.
The five deliberate test/formatter/build/unavailable-PG/missing-Chromium
children each exit 1 and their enclosing exact-failure checks pass.
Owned runtime stop, restoration, output scans and provider cleanup pass.
Post-run Git inspection showed only the intended review-begin ledger change;
locked restores and generation introduced no tracked source/lock drift.

### Actual assessed-source CI

Independently queried completed/successful runs:
[repository 39648667413](https://github.com/Stonefly-Labs/Kofte/actions/runs/39648667413)
and [product 39648667363](https://github.com/Stonefly-Labs/Kofte/actions/runs/39648667363).
All ten product jobs succeeded. Their ten uploaded safe reports were downloaded
and inspected: all 170 named steps pass, with identical ordered step names per
suite/provider to the fresh local run. Individual CI reports remain honestly
`scope: targeted`, `completeGate: false`, not a fabricated single all invocation.

Both runs identify head `4f708c98ccf81597914556bbe89db399ec7cf6cf`;
the reports identify synthetic PR merge
`1ee086a84b6941c48f42be4fd32f21e47ea30f70`. Its actual parents were verified
through GitHub: base `5e72fcc61724db6b2f68c53987ccb64f3debad72` and that exact
feature head. Uploaded environment evidence is Linux x86_64, four logical
CPUs and approximately 16.76 GB memory, not invented equivalence to the
specified reference machine. Publication-head checks are recorded in PR #4.

This closes the reviewer's initially in-progress fresh-gate, no-drift and live-CI
evidence requests. Historical implementation/convergence evidence remains
unchanged and is not relabeled as new execution. Final stage-diff repository
checks are also included in the PR handoff.

## Human evidence, limits and next gate

T056/T059 remain human-confirmed complete. `implementation.md:554-570` records:
"ok bro 56 and 59 are done and marked complete, just need to commit".
No granular manual observations, unfamiliar-contributor details or additional
reference measurements were supplied or invented by this review.
Automated accessibility is not a complete manual WCAG assessment; ordinary
macOS/CI timings are not equivalent-reference-hardware measurements.

No mandatory check is left failed/unavailable in the fresh automated gate.
This is not a dedicated release security audit, production certification,
proof of later module/MCP/authorization/Kubernetes behavior or MVP completion.

**Next invocation: accept.** Explicit human acceptance of **spec 001** has not
been requested or granted by this review. The earlier ADR/task approvals,
checked tasks, passing automation and this disposition are not that decision.
Do not create `acceptance.md`, complete accept, mark the feature PR ready or
request merge in this invocation. Continue on
`feature/001-specify-repository-bootstrap` and the same
[draft PR #4](https://github.com/Stonefly-Labs/Kofte/pull/4).
