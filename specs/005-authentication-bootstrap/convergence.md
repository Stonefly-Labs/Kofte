# Convergence: 005 - Authentication and Bootstrap Administration

**Result:** pass
**Date:** 2026-10-30
**Assessed source:** `a914e6b98efd138288dadcb6acc0468f01eac0c7`
**Branch / directory:** `feature/005-authentication-bootstrap` /
`specs/005-authentication-bootstrap`
**Checkpoint:** [draft PR #8](https://github.com/Stonefly-Labs/Kofte/pull/8)

The implementation satisfies the assessed spec, plan and tasks with **zero
actionable convergence findings**. This is a source/assertion/evidence assessment,
not feature acceptance, a new runtime test execution or production qualification.
Review is next on this same branch/draft PR in a separate invocation.

## Assessment method and scope

Executed the installed `speckit-converge` instructions with
`SPECIFY_FEATURE_DIRECTORY=specs/005-authentication-bootstrap` and the exact
prerequisite command:

```bash
SPECIFY_FEATURE_DIRECTORY=specs/005-authentication-bootstrap .specify/scripts/bash/check-prerequisites.sh --json --require-spec --require-tasks --include-tasks
```

It resolved the recorded directory and spec/plan/tasks successfully.
`.specify/extensions.yml` is absent; there are no convergence hooks.
Intent comes from `spec.md`, `plan.md` and every task, governed by constitution
2.0.1. Linked model/contracts, approved ADR 0007 and its explicit verification
amendment clarify those obligations; completion markers and the implementation
report are not substituted for source/assertions.

Inspected the host identity pipeline and endpoints, native OIDC/backchannel,
ticket bridge, owner-local mapping/session/bootstrap/evidence operations,
explicit preparation/key/restore boundaries, Next adapter/state UI, independent
provider/consumer/browser assertions and fail-closed verification orchestration.
Proposed task filenames consolidated into existing test projects are reconciled
below; absent proposed filenames are not assumed to be missing behavior.
No application/test/tooling code, dependency, approved intent or reviewer-owned
checklist is changed by this assessment.

| Inventory | Assessed | Actionable gaps |
| --- | --- | --- |
| Functional requirements | FR-001 through FR-022: 22 | 0 |
| Buildable outcomes | SC-001 through SC-007: 7 | 0 |
| Given/When/Then scenarios | US1: 5; US2: 5; US3: 5; US4: 3; US5: 4; total 22 | 0 |
| Plan decisions | R1 through R12: 12 | 0 |
| Existing tasks | T001 through T100: 100 | 0 |
| Constitutional principles | I through XII: 12, including applicability limits | 0 |

Total requirements/outcomes/scenarios: **51**. Findings by type:
`missing=0`, `partial=0`, `contradicts=0`, `unrequested=0`.
Findings by severity: `CRITICAL=0`, `HIGH=0`, `MEDIUM=0`, `LOW=0`.
No remediation task or empty convergence phase is appended.

## Requirements, outcomes and acceptance coverage

Path keys: **H** = `src/Kofte.Host/Features/Identity/`;
**I** = `src/Kofte.Infrastructure/Identity/`;
**HI** = `tests/integration/Kofte.Host.IntegrationTests/Identity/`;
**SI** = `tests/integration/Kofte.Storage.IntegrationTests/Identity/`;
**C** = `tests/contract/Kofte.Platform.ContractTests/Identity/`;
**P** = `tests/contract/Kofte.Identity.Consumer/Scenarios/`;
**W** = `apps/web/features/identity/`; **E** = `tests/e2e/`.
All paths below refer to delivered source, not proposed task filenames.

| Intent | Source and meaningful assertion inspected | Assessment |
| --- | --- | --- |
| FR-001/FR-002; US1.1/1.4; SC-001/003 | H `Login/OidcConfiguration.cs:17` configures confidential code/PKCE, exact issuer/audience, signed RS256, nonce/correlation and no token saving/UserInfo/PAR. H `Login/ValidatedLogin.cs` binds durable browser attempt and owner commit. HI `OidcProtocolTests.cs` and `OidcBudgetTests.cs` reject independent issuer/audience/signature/algorithm/time/nonce/state/PKCE, unsolicited/replayed/duplicate evidence before users/sessions/events; corrected fresh login succeeds. | Satisfied |
| FR-003; US1.2/1.3; SC-001/006 | I `IdentityUserStore.cs` keys exact mode/issuer/subject, reconciles only the named mapping conflict and never keys on profile. SI `IdentityMappingTests.cs` compares original IDs/mappings/creation events across concurrent first mapping, profile changes, three fresh product processes and sequential reconciled issuers; equal profiles remain distinct. | Satisfied |
| FR-004; US1.1/US3.2; SC-001 | H `CurrentUser/CurrentUserEndpoints.cs` projects five self-only fields from the durable view. C `CurrentIdentityContractTests.cs` verifies exact field inventory, escaped changed profile, fresh designation and consumer actor equality; `IdentityParityTests.cs` joins actual HTTPS Next and direct access. | Satisfied |
| FR-005/FR-006; US3.1/3.5; SC-003 | H `IdentityRegistration.cs` default/fallback policy requires authentication and denies unrelated operations; current identity has an explicit self policy. H `IdentityRequestBoundary.cs` admits exact public methods/paths, retains private 404/405 and rejects Authorization. HI `IdentityPolicyTests.cs`, C `LoginEndpointContractTests.cs` and parity tests distinguish 401/403/503 without provider redirects and retain anonymous aggregate health. | Satisfied |
| FR-007/FR-021; US3.2/3.3; SC-003 | Immutable `src/Kofte.Platform/Identity/ActorContracts.cs`, H request-scoped actor projection and separately compiled consumer `ActorProbe.cs` share established identity only. C `ActorContractTests.cs`, current-identity/parity/session-scope tests reject overrides and deny an ungranted operation even after enrollment; no team/resource grant or deployed test route is introduced. | Satisfied |
| FR-008; US1.4/US3.1/3.3/3.5; SC-003/007 | H request boundary, cookie format/ticket bridge and W `identityAdapter.ts` enforce exact Origin/antiforgery, opaque HttpOnly mode-distinct cookies, bounded individual cookie forwarding, fixed upstream/redirects and no-store/no-referrer. C login-endpoint/login-commit/parity tests assert binding/fixation, exact input limits and refusal before effects. E production journey checks two-user SSR/cache/adapter isolation. | Satisfied |
| FR-009; US3.4; SC-001/003 | I `IdentitySessionStore.cs` freshly verifies mode/epoch/state/lifetimes and commits terminal event with revocation/expiry. H `SignOut/SignOutEndpoints.cs` acknowledges only confirmed revocation. C `SessionPersistenceContractTests.cs` checks just-before/exact idle and absolute boundaries, terminal renewal race, corrupt-ticket 503 and ninth-session no-eviction; `SignOutContractTests.cs` covers actual cookie/key/restart/commit cases. W `useIdentity.ts` clears stale state on loading/refusal and never retries a mutation automatically. | Satisfied |
| FR-010; US2.1/2.2/2.3; SC-002/003 | Stopped `IdentityNomination` and I `IdentityBootstrapStore.cs:51` bind a private 256-bit authorization to exact production nominee/generation/expiry. HI `EnrollmentTests.cs`, SI `IdentityNominationTests.cs` and capacity tests check disabled, wrong nominee/mode/token, expiry/replacement, delivery interruption and non-enumerating refusal. Development cannot qualify. | Satisfied |
| FR-011; US2.2/2.4; SC-002/006 | I `IdentityBootstrapStore.cs:104` verifies the current session inside the owner transaction and commits consumed state/designation/event together. HI `EnrollmentConcurrencyTests.cs:15` releases 20 real eligible HTTP requests, requires one 200/19 conflicts and exact user/generation/event correlation, then three fresh host processes. SI `EnrollmentCrashTests.cs` kills actual before/after-commit owners and checks neither-or-one state, lost acknowledgement and safe explicit retry. | Satisfied |
| FR-012; US2.5; SC-002/006 | Consumed/recovery-locked states have no public reset. SI `EnrollmentRestartTests.cs:22` repeats twenty contenders and three restarts with original module assignment/sentinels/migration history, idempotent preparation and unavailable-original-issuer behavior. Replacement login remains ordinary and stopped renomination refuses. P operations/recovery and manifest-bound restore preserve consumed state or lock unknown history. | Satisfied |
| FR-013/FR-014; US4.1/4.2/4.3; SC-004 | H configuration requires exact Development environment, opt-in and loopback host/web/backend/public origin; fixed development namespace/cookies cannot enroll. HI `DevelopmentBoundaryTests.cs` invokes the actual empty/composed safe/unsafe startup matrix in `DevelopmentExposure.cs`, verifies no listener/state change and owned exit. C `DevelopmentIdentityTests.cs`, P `DevelopmentScenarios.cs` and real browser selection check persisted mode transitions, equal-profile separation and visible Development label. | Satisfied |
| FR-015; US1.5/US5.2; SC-003/005/006 | Strict configuration, H `IdentityDiagnostics.cs`, bounded backchannel and I deadline-backed operations refuse without fallback. HI `OidcBudgetTests.cs` observes actual egress counts/time/DNS readmission and valid local sessions during provider outage. HI `IdentityFailureTests.cs` exercises locked/paused store, lost authority, corrupt ticket, required-record failure and pre/post-commit uncertainty with exact unchanged/reconciled state. E `identity.failures.ts` observes cancellation/outage/corrected retry and real callback-to-result timing. | Satisfied |
| FR-016; US5.1/5.2; SC-006 | I preparation/restore operations use platform ownership and stopped authority; H `IdentityRuntime.cs:28` verifies current prepared format/keys without migrating. SI `IdentityFormatTests.cs` checks original migration digest, exact assignments/module payload/history, rollback and physical tampering. SI recovery and HI preparation/restore/journal/key tests prove old-binary refusal, actual complete-set restore/quarantine, interrupted intent/key/DB boundaries and explicit resumable reconciliation. | Satisfied |
| FR-017/FR-018; US5.3; SC-007 | I `IdentityEventStore.cs` records required owner changes atomically, enforces installation/session retention and refuses capacity/sink failure. I/H diagnostics allowlist bounded codes/guidance/correlation/durability and low-cardinality metrics. SI `IdentityEventTests.cs` asserts immutability, rollback, exact capacity warnings and bounded pruning. P `OperationsScenarios.cs` joins actual rejected-login, enrollment, record-failed/corrected sign-out, expiry and stopped-operator evidence; native/Next/ingress/browser capture requires nonempty channels and canary-free results. | Satisfied |
| FR-019; US1.5/US5.2/5.3; SC-003/007 | H configuration/backchannel, I admission/attempt/session operations and W fixed adapter impose finite bytes, requests, time, rates and lifetimes. HI protocol/backchannel/budget, SI admission/attempt/capacity/event/mapping/deadline and independent HTTP corpus test exact boundaries and rollback. Maintenance prunes at most 128 rows per category without removing installation evidence or reopening enrollment. The explicitly prohibited installation session-capacity proof is excluded, not silently passed. | Satisfied within approved verification amendment |
| FR-020; US5.4; SC-005/007 | W `IdentityPanel.tsx`, `useIdentity.ts`, enrollment and Development controls expose deliberate loading/signed-out/signed-in/expired/failure/retry states. E `identity.spec.ts`, `identity.production.ts`, `identity.failures.ts` and `identityAccessibility.ts` use real browser boundaries, 320 CSS pixels, 200% text, keyboard/visible focus/live announcements, no serious/critical axe findings and actual post-callback result within 5000ms. No human study is claimed. | Satisfied |
| FR-022; all scenarios/outcomes | `scripts/verify.py` requires actual source/project/scenario/class inventories, compares private Playwright discovery/execution with no skip/retry/flaky result, retains provider suites and prevents cancelled partial completion. `IdentityConsumerHarness.cs` validates exact ordered observations, child result and verified descendant exit; harness negatives reject missing/truncated/duplicate/capture/cleanup evidence. Actual complete/current-head reports are reconciled below. | Satisfied |

All **22 scenarios** are explicitly represented above: US1.1-1.5, US2.1-2.5,
US3.1-3.5, US4.1-4.3 and US5.1-5.4. SC-001-007 have buildable assertion
coverage; no post-launch business metric is substituted.

## Every task and plan decision

| Tasks | Delivered work assessed, including validation tasks | Plan decisions |
| --- | --- | --- |
| T001-T006 | Approved integration/adoption evidence, exact locked dependency inventory/notices; separate actor consumer, independent owned HTTPS issuer/process/barrier fixtures and frozen protocol/HTTP/numeric expectations. | R1/R9/R12 |
| T007-T010 | Strict configuration/secret/environment tests and implementation; minimal immutable actor shape and separately compiled consumer. | R4/R8/R9 |
| T011-T017 | Exact format-2 migrations/verification, metadata/admission/event transactions, persistent private purpose-bound keys and admitted stopped prepare/reconcile/revoke. Source and actual physical/retention/key/authority assertions inspected, not task ticks alone. | R3/R6/R9/R10 |
| T018-T022 | Shared endpoint/CSRF/default denial/diagnostics in both hosting paths, read-only prepared startup and real-provider foundation/deadline/process tests. | R4/R6/R9/R10/R12 |
| T023-T040 | Native protocol/egress, stable mapping, durable attempt/session commit bridge, self identity, fixed Next adapters, minimal UI, public login/trust recipes and both-provider/browser evidence. | R1/R2/R3/R4/R9/R11/R12 |
| T041-T055 | Disabled/nominated/consumed transitions, private file delivery, protected form/endpoint, exact twenty-way HTTP race, real commit interruption, three fresh restarts and public enrollment recipes/privacy evidence. | R5/R10/R11/R12 |
| T056-T070 | Independent native/Next/consumer parity, strict request corpus, durable finite sessions/sign-out, ungranted-operation denial, fresh epochs, two-user isolation, generated contract and real browser/session recipe. Proposed session/HTTP test filenames are delivered as C `SessionPersistenceContractTests.cs`, `SignOutContractTests.cs`, `SessionScopeContractTests.cs`, `LoginEndpointContractTests.cs`, `CurrentIdentityContractTests.cs` and existing cross-language tests. | R2/R3/R4/R9/R11/R12 |
| T071-T078 | Explicit synthetic Development selection/config/adapter/UI, actual exposure matrix, persisted mode/authority isolation and supervisor preparation/ownership/cleanup. The matrix is delivered in configuration tests plus HI `DevelopmentBoundaryTests.cs`/test-support `DevelopmentExposure.cs`, not the proposed extra unit filename. | R2/R8/R11/R12 |
| T079-T092 | Clean/upgrade/current format, actual old-binary refusal, recovery-v2 snapshot/quarantine/strict decisions, keys/certificate overlap, crashed preparation/journals, failed required mutations, retention/maintenance/capacity, privacy/accessibility and public operation recipes. Proposed upgrade/preparation/session/privacy files are consolidated into actual SI format/recovery/capacity/event tests, HI preparation/restore/journal/key/failure tests, P operations and E private captures. | R6/R7/R9/R10/R11/R12 |
| T093-T100 | Fail-closed required inventories/native/browser/consumer evidence, all nine recipes individually plus `all` per provider, delivered operator/setup/API docs, reviewed dependencies/generated contracts and actual full-command/current-head CI/traceability/publication. The same draft PR and separate human acceptance boundary are preserved. | R1-R12 |

All twelve plan decisions have delivered implementation and assertions, including
R7's fresh epoch/key custody and stale-backup lock, not just manifest metadata.
Test filename consolidation follows the task file's explicit allowance; no .NET
test source is moved into or linked from production.

## Constitutional assessment

| Principle | Assessed boundary / applicability |
| --- | --- |
| I | Köfte brand, MIT intent, portable module-free identity; no Hoard/cloud requirement. |
| II | One host-established actor; retained module composition/lifecycle/disable behavior; no competing module authentication or sandbox claim. |
| III | Native DI/direct host-local handlers; Platform holds actor contracts only; dedicated test projects and reviewed exact dependencies, no mediator/mapper. |
| IV | Identity is platform-owned; exact original module schema/file assignments, migration history and data are preserved on both stores. Live SQLite header checks use SQLite-owned handles. |
| V | Mapping/attempt/session/event/enrollment are owner-local transactions. DB/file/key partial effects are explicit and retryable only under stopped authority; no cross-module atomicity. |
| VI | Exact OIDC trust, development separation, nominated non-repeatable enrollment, deny-by-default actor/operation boundary, CSRF/egress/diagnostic protections. Detailed team/resource policy remains 006, not an initial-admin bypass. |
| VII | Shared actor-v1 prepares future 006/007; no MCP runtime/tool is added or claimed tested. |
| VIII | No resource/job/event-delivery/realtime runtime. Bounded owner-local retention is not a background workflow or exactly-once delivery claim. |
| IX | Minimal existing Next/React/TypeScript design primitives with deliberate state/focus/reflow/accessibility evidence; no full 008 shell or arbitrary frontend loading. |
| X | Real SQLite/PostgreSQL, private TLS/key/proxy responsibilities, explicit preparation/backup/restore and pinned reproducible tooling. No Kubernetes/cloud deployment qualification claimed. |
| XI | Independent protocol/compiled consumer/real providers/process crashes/browser/capture/retained gates; missing or skipped evidence fails. Reference-module/deployment/release proof remains 020/022-024. |
| XII | Scope remains numbered 005 and the explicit human verification amendment; one branch/draft PR and separate review/acceptance/merge. No spec 006 or post-MVP work is started. |

## Actual verification and provenance

No product or Docker suite was restarted on the shared workstation. This
documentation-only assessment inspected actual existing executions and
downloaded safe reports, rather than representing historical results as fresh
local tests:

| Evidence inspected | Result |
| --- | --- |
| `gh run view 41072051400 --json status,conclusion,headSha,url,jobs` | Completed/success, exact source `4e16d36f5cd54fd3da3e81d6641ef1d8f0598f4f`; required command step passes. |
| `gh run download 41072051400 -n verification-complete-gate -D <session-evidence>` | Ten actual reports; each has the exact revision, `scope=all`, `completeGate=true`, `outcome=passed`; all 313 named steps pass. |
| Complete command in that hosted run: `python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10` | Passed on the recorded Ubuntu/x64 four-CPU/~16-GB profile. Conditional post-failure diagnostics and separate browser-recipe workflow steps are not required all-mode test skips. |
| `gh run download 41091559018 -D <session-evidence>` and `gh pr view 8 --json title,body,baseRefName,headRefName,statusCheckRollup` | Current assessed-head CI: all ten product contexts and repository checks pass. Ten actual targeted reports identify merge `0eb7b1959a5c89ff231035d746fd250675d77d6b`; all 313 named steps pass. |
| Structured reconciliation of downloaded report revisions, unique suite/provider sets, scope/completion flags, every step and name multiplicity | Passed: exact 313-step parity, not a sum promoted into an all-mode certificate. A first local inspection assertion incorrectly expected string `none` for non-provider suites; inspecting the schema showed JSON `null`. Corrected validation passes without altering any report. |
| Assessed merge parents/tree and exact qualified-source comparison | Merge parents are main `3e41681` and feature `a914e6b`; feature/merge tree is `6d517c5a32b160b7ed1f2cb66e9ec62bd21e728a`. Qualified `4e16d36` and assessed feature have identical `src`, `apps`, `packages`, `tests`, `scripts` and `.github` content. |
| Implementation artifact SHA-256 reconciliation | Tasks, implementation report and both reviewed checklists match the completed implement ledger evidence exactly. |

Actual assessed-head native evidence includes SQLite host/storage/composition
**212/240/57** and PostgreSQL **122/212/59**, all zero skipped; public
contracts **265** with eighteen required classes; each provider's ten public
guide cases (nine recipes plus `all`); and two native identity browser cases
per provider. Privacy additionally requires both native browser selections
and provider-qualified captured public journeys. Required class inventories
were inspected along with counts, not inferred from zero exit.
Complete-command startup includes ten actual empty warm cycles and two
configured module cycles; retained failure baselines/injections pass.

Fresh stage checks:

- `python3 scripts/check_repository.py`: passed.
- `python3 -m unittest discover -s scripts/tests -v`: passed, 170 tests in 8.350s.
- `git diff --check`: passed.

These are repository/documentation checks, not new local product qualification.
Product format/build/type/lint/browser/provider checks remain the actual hosted
evidence above; no source changed that would require substituting a new subset.

## Preservation, limitations and handoff

`tasks.md` remains byte-for-byte unchanged, SHA-256
`c65bae2e49bf5b8e9fe86479dd59f338a8ed1ff0ebee895a3a16bc3a1d2d139f`.
All 100 original markers are preserved, with no appended phase/task.
The author/reviewer checklists remain 16/16 and 36/36 with unchanged bytes.
Specification, plan, research, model/contracts, ADR, implementation history and
product/test/tooling source are untouched.

The human-directed prohibition on the 50,000-session boundary test remains
binding: no proof is run, replaced or claimed, and the runtime cap is unchanged.
Final complete qualification is hosted Linux, not a final-source macOS pass.
Historical failed/interrupted Mac and earlier hosted results and unknown causes
remain in the implementation record. No rollback-proof administrator history,
old-instance/OS-copy fencing, provider-wide logout/revocation, general permission
engine, production readiness, human usability study or MVP completion is inferred.

**Next stage: review**, separately on the same draft PR #8. Passing convergence
does not write `review.md`/`acceptance.md`, accept numbered feature 005, mark the
PR ready, request merge or unlock 006. Explicit human feature acceptance and
subsequent human merge remain required.
