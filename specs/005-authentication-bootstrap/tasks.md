# Tasks: 005 - Authentication and Bootstrap Administration

**Input**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md),
[data-model.md](data-model.md), [identity-v1](contracts/identity-v1.md),
[operator-v1](contracts/operator-v1.md) and [quickstart.md](quickstart.md).
**Branch / directory**: `feature/005-authentication-bootstrap` /
`specs/005-authentication-bootstrap`; continue on draft PR #8.
**Prerequisites**: Accepted 001-004; ADR 0007 is **Accepted for implementation**
by its later Approval section, despite historical Proposed planning text.
Author checklist 16/16 and explicitly assisted reviewer checklist 36/36 pass.
Passing **analyze is still required before implementation**; no analysis or
implementation is performed by generating this file.
**Constitution**: 2.0.1, principles I-XII; no exception or amendment.

**Tests**: Constitutional/spec-required tests are mandatory. Use actual owned
SQLite/PostgreSQL and independent OIDC/browser/consumer evidence, not mocked
principals, in-memory providers, production-derived expected values or summary
counts. Record initial red results for introduced tests and actual green
results after wiring; never claim a test ran before its fixture existed.
**Organization**: Setup, shared blocking foundations, US1-US3 (P1), US4-US5
(P2), then cross-cutting evidence. The secure US1 increment precedes enrollment;
later stories extend it without temporarily publishing an unprotected path.

## Format and Paths

- Every task is initially unchecked. `[P]` means separate files can be worked
  after **all listed prerequisites**; it neither authorizes another stage nor
  implies all marked tasks can run together.
- `Depends: none` is an explicit root. All other `Depends` lists are direct
  prerequisites; transitive prerequisites apply. Order alone creates no edge.
- Paths named below are implementation targets, not claims those files exist
  today. Reuse existing helpers; proposed filenames may be consolidated within
  the approved feature directories without losing a requirement or test.
- The full T064 corpus includes positive Development login. Its T074 delivery
  prerequisite is explicit: T071/T072 may begin after T055 while US3 continues,
  rather than semantically requiring Development to exist before its own
  prerequisites can start. No Development case or final evidence is waived.
- All .NET tests/fixture/consumer source physically belongs to its dedicated
  project under `tests/`, using normal SDK inclusion, never production slices
  or source links. Platform receives only actor-v1; host handlers use native
  DI/direct calls, and persistence stays Infrastructure-local.
- Each task carries requirements and constitutional principle IDs. Model
  quotations are binding constraints, not permission to omit the remaining
  fields/invariants in the linked model. Use its complete schema, not a subset.
- Commands in checkpoints and the final gate are future execution obligations,
  not checks performed by task generation. Keep raw secrets/test captures out
  of source, reports, browser artifacts and PRs.

## Phase 1: Setup and Shared Prerequisites

- [x] T001 Reconcile approved ADR 0007, reviewed checklist evidence and accepted 001-004 integration boundaries in `specs/005-authentication-bootstrap/implementation.md`; establish the implementation evidence matrix, actual tool/profile inventory and unchanged historical failure baseline without rewriting intent. (Depends: none; FR-022; I/X/XI/XII.)
- [x] T002 Review the exact `Microsoft.AspNetCore.Authentication.OpenIdConnect` 10.0.13 archive and actual IdentityModel/direct/transitive closure for provenance, licenses, notices, supported-version compatibility and advisories before adoption; update `Directory.Packages.props`, `src/Kofte.Host/Kofte.Host.csproj`, affected `packages.lock.json`, `docs/development/dependency-inventory.json` and `docs/development/dependencies.md` through existing restore/inventory tooling. Retain original package notices and update `docs/development/third-party-notices.md` only for applicable newly adopted source obligations, preserving its checker reconciliation. Resolve unacceptable findings explicitly, not with an unreviewed substitute. (Depends: T001; FR-001/022; III/VI/X/XI.)
- [x] T003 Create the test-only separately compiled `tests/contract/Kofte.Identity.Consumer/Kofte.Identity.Consumer.csproj` and `Program.cs`, wire `Kofte.slnx` and existing test project references/locks, and define a fail-closed scenario dispatcher with no production reference to the consumer or deployed test bypass. Its actor probe consumes public Platform types, not identity tables or externally asserted principal objects. (Depends: T002; FR-007/021/022; II/III/VI/XI.)
- [x] T004 [P] Implement `tests/integration/Kofte.TestSupport/Identity/OidcIssuer.cs` with owned HTTPS/test CA, independently signed discovery/JWKS/authorization/code/token exchanges, single-use codes, PKCE/nonce/client checks and request observations. Confine injected TLS trust and issuer controls to test-owned hosting; use no production skip-TLS flag. (Depends: T002; FR-001/002/019/022; VI/X/XI.)
- [x] T005 [P] Implement `tests/integration/Kofte.TestSupport/Identity/IdentityFixture.cs`, `IdentityProcess.cs` and `IdentityCommitBarrier.cs` using existing owned roots/processes/provider scopes, private phase admission and verified child/descendant cleanup; support real commit barriers and fresh hosts without exposing fixture controls in product routes. (Depends: T002; FR-011/016/022; III/IV/V/XI.)
- [x] T006 [P] Freeze independent request/response, signed-evidence and numeric-boundary expectations in `tests/contract/Kofte.Platform.ContractTests/Identity/IdentityCorpus.cs` and `tests/contract/identity-corpus.ts`; enumerate every identity-v1/operator-v1 limit and exact route/credential/redirect rule, with at-limit/one-over, expected status/effects and safe evidence. Do not derive expected decisions from production validation. (Depends: T002; FR-002/005-009/018/019/021/022; VI/XI.)

## Phase 2: Foundational Boundaries

- [x] T007 [P] Add strict configuration tests in `tests/unit/Kofte.Host.Tests/Features/Identity/IdentityConfigurationTests.cs` for required explicit mode, duplicate/unknown JSON fields, all secret/path/origin/listener combinations, trust-digest changes, operation-specific offline validation and unknown `KOFTE_*` rejection. (Depends: T002; FR-013-015/019; VI/X/XI.)
- [x] T008 Implement `src/Kofte.Host/Features/Identity/IdentityConfiguration.cs` and extend `src/Kofte.Host/Bootstrap/BootstrapConfiguration.cs` plus existing maintenance/composition environment allowlists with only `KOFTE_AUTH_CONFIG_FILE`. Enforce "strict UTF-8 JSON object with no duplicate/unknown fields", "at most 32 KiB", secret bounds "4 KiB client secret, 64 KiB certificate/key", "one to eight synthetic configured identity keys/display names", owner-only regular absolute no-symlink references, exact HTTPS OIDC/loopback development origins and no implicit fallback. (Depends: T007; FR-013-015/019; VI/X.)
- [x] T009 [P] Add actor-v1 shape/absence/mode/immutability tests in `tests/contract/Kofte.Platform.ContractTests/Identity/ActorContractTests.cs` and the consumer's `ActorProbe.cs`; test a nonempty user ID, the two admitted modes and no credential/storage/permission payload. (Depends: T003; FR-007/021; II/III/VI/XI.)
- [x] T010 Implement only immutable `AuthenticatedActorV1`, `IdentityMode` and request-scoped read-only `ICurrentActorV1` in `src/Kofte.Platform/Identity/ActorContracts.cs`: "UserId (nonempty Guid)", "IdentityMode (`Oidc` or `Development`)" and "IsInitialAdministrator (bool)"; null means no established actor, not verification-unavailable fallback. Initial designation grants no unrelated action. (Depends: T009; FR-005/007/021; II/III/VI/VII.)
- [x] T011 [P] Add real-provider schema/prepare tests in `tests/integration/Kofte.Storage.IntegrationTests/Identity/IdentityFormatTests.cs` for exact three-plus-eight table inventories, constraints/collations/history and v0/v1/v2/unknown/corrupt handling; snapshot accepted migration-1 bytes, installation/module IDs, allocation/markers/history/data and registry revision sum. (Depends: T005; FR-003/016/022; IV/V/X/XI.)
- [x] T012 Add provider-specific migration 2 under `src/Kofte.Infrastructure/Storage/Registry/Migrations/Postgres/002_identity.sql` and `Sqlite/002_identity.sql`, with all eight model tables, required columns/null combinations, PK/FK/UNIQUE/CHECK/index definitions and deterministic `C`/BINARY keys. Preserve migration-1 bytes; use "IDs are nonempty random UUIDs", "Times are UTC instants", "All counters are nonnegative checked 64-bit integers; overflow refuses the operation" and fixed bounded enum strings. Format 2 appends history and advances registry/SQLite user version, not module revisions. (Depends: T011; FR-003/011/016; III/IV/V.)
- [x] T013 Extend `src/Kofte.Infrastructure/Storage/Registry/RegistryMigrationRunner.cs`, `RegistryRecords.cs`, `StorageReconciliation.cs` and `src/Kofte.Infrastructure/Composition/CompositionStorageInspector.cs` with explicit exact v1/v2 verification, including registration validation's existing installation-version-1 guard. Check actual columns/types/nullability/PK/FK/UNIQUE/CHECK/indexes and ordered digests; reject extra tables/views/triggers/routines/defaults/generated objects. Preserve module allocator/marker version 1, constraints, schema-local EF history, privilege/path/sidecar rules and exact platform-version-1 verification instead of accepting arbitrary extras. (Depends: T012; FR-016/022; II/IV/V/X/XI.)
- [x] T014 Implement owner-local metadata/admission transactions in `src/Kofte.Infrastructure/Identity/IdentityStore.cs` and `IdentityAdmission.cs` using normal host lease/parameterized values/fixed identifiers. Metadata has "Singleton key 1", "identity schema version 1", checked revision, nonempty epoch, mode/trust digest and explicit reconciliation; every security mutation advances that revision, never module revision sum. Admission stores "Fixed-category window/key digest PK", UTC window/expiry and checked count, only bounded hashed browser/peer keys; "at most 4,096 live windows" and "at-most-128-row batches", with no capacity/store-failure success fallback. (Depends: T008, T013; FR-015-019; IV/V/VI.)
- [x] T015 Implement transactional evidence in `src/Kofte.Infrastructure/Identity/IdentityEventStore.cs`: "Random event ID PK", checked revision, allowlisted code, UTC, "generated 32-lowercase-hex correlation", optional internal user FK, "outcome `committed`" and "retention class `installation` or `session`". Retain required creation/enrollment/operator events immutably for installation lifetime; session events 30 days, capacities "1,000,000 installation-retained and 200,000 session records", "Warn at 90%" and refuse required mutations if recording fails. No raw external identity/profile/token/path data or rejected operation represented as a committed event. (Depends: T014; FR-017-019; IV/V/VI/X/XI.)
- [x] T016 Implement installation/purpose-bound persistent Data Protection configuration in `src/Kofte.Host/Features/Identity/IdentityKeys.cs`, with owner-only separate ring outside recovery root, mandatory OIDC certificate protection, verified certificate/private-key relationship and normal retained-key rotation. Missing/foreign/undecryptable keys block serving; explicit prepare reconciles partial DB/filesystem effects, never claiming a distributed transaction or regenerating lost keys on serve. (Depends: T008, T014; FR-008/009/015/016/019; V/VI/X.)
- [x] T017 Add admitted stopped `prepare`, `reconcile-configuration` and `revoke-sessions` operations in `src/Kofte.Host/Features/IdentityMaintenance/IdentityCommand.cs`, `IdentityMaintenanceWorker.cs` and `IdentityMaintenanceSupervisor.cs`, reusing existing maintenance process/lease authority and deliberate worker dispatch. Prepare verifies v1/v2, requires separate v0 registry upgrade, initializes disabled bootstrap and owned keys idempotently. Reconciliation/epoch changes revoke prior sessions/attempts/unconsumed authority, record operator decisions and preserve mappings/consumed designation. Keep total 120-second and inherited clipped operation/cleanup budgets, no live-IdP prerequisite, direct-worker bypass or raw IPC. (Depends: T013, T015, T016; FR-009/012/014-019; IV/V/VI/X.)
- [x] T018 [P] Add endpoint/CSRF/fallback-policy tests in `tests/integration/Kofte.Host.IntegrationTests/Identity/IdentityPolicyTests.cs` for exact health/OpenAPI rules, public protocol inventory, default-denied operation, wrong methods/query/body, exact Origin/token-pair and callback-specific binding. Protect empty and composed hosting; do not mistake public metadata for unvalidated anonymous execution. (Depends: T005, T006, T010; FR-005-008/021/022; II/VI/XI.)
- [x] T019 Implement shared policies/antiforgery/safe result mapping in `src/Kofte.Host/Features/Identity/IdentityRegistration.cs`, `IdentityRequestBoundary.cs` and `IdentityProblem.cs`; make `src/Kofte.Host/Features/Bootstrap/HealthEndpoints.cs` guards health-scoped without relaxing unknown-route/method/input denial. Explicitly validate JSON/form mutations with `ValidateRequestAsync`, exact Origin/Fetch Metadata, refuse Authorization even with cookies, and keep protected API 401/403/503 distinct from deliberate OIDC challenge. No first-admin universal allow. (Depends: T008, T010, T018; FR-005-008/015/018/019/021; II/III/VI.)
- [x] T020 Implement fixed sanitized identity diagnostics in `src/Kofte.Host/Features/Identity/IdentityDiagnostics.cs`, `src/Kofte.Infrastructure/Identity/IdentityDiagnostics.cs` and extend `src/Kofte.Host/Features/ModuleComposition/CompositionProtocol.cs`/`CompositionSupervisor.cs` allowlisted projection. Emit generated correlation, safe operation/outcome/correction and truthful unchanged/committed/uncertain durability, "at most 1 KiB/event" and fixed bounded phase/metric labels, never raw exception/HTTP/query/cookie/provider/nominee data. Keep diagnostics available on store refusal; sink failure cannot report a successful mutation. (Depends: T015, T019; FR-015/017-019; II/VI/X/XI.)
- [x] T021 Wire identical identity configuration, prepared-format/key validation, services and middleware into `src/Kofte.Host/Program.cs` and `src/Kofte.Host/Features/ModuleComposition/CompositionWorker.cs`/`ModuleCompositionHost.cs`. Verify startup read-only within inherited envelopes, distinguish `identity_prepare_required`, reject unacknowledged trust changes/quarantine and avoid requiring discovery during startup. Ordinary runtime must not inherit stopped provisioning authority. (Depends: T017, T019, T020; FR-005/013-016/019; II/IV/VI/X.)
- [x] T022 Execute foundation unit and both-provider integration gates with `tests/integration/Kofte.Storage.IntegrationTests/Identity/IdentityFoundationTests.cs` covering physical tampering, overflow, event rollback, admission limits, scope/live-host/lost-lease refusal, prepare idempotence and real child termination. Record exact commands/results and preserved snapshots in `specs/005-authentication-bootstrap/implementation.md`; do not continue with insecure or silently skipped foundation tests. (Depends: T004, T006, T021; FR-015-019/022; IV/V/VI/X/XI.)

**Checkpoint**: Exact versioned owner storage, safe configuration/key/authority,
default denial and useful diagnostics are tested on both stores. No unfinished
login or enrollment handler is published merely to get a health check green.

## Phase 3: User Story 1 - Enter Köfte with a Trusted Identity (Priority: P1)

**Goal**: Real configured production OIDC establishes a stable local identity,
with a safe browser return, matching direct API and bounded rejection/retry.
**Independent Test**: Own HTTPS issuer/host/web processes on each real store;
complete code+PKCE, compare exact `/me` and UI identity, change profiles/restart,
then reject independently signed invalid evidence and provider failures.

### Required Validation

- [x] T023 [P] [US1] Add actual protocol positive/negative tests in `tests/integration/Kofte.Host.IntegrationTests/Identity/OidcProtocolTests.cs` using independent issuer/corpus: wrong exact issuer/audience/signature/key/algorithm/time/nonce/state/PKCE, missing/malformed keys, unsolicited/replayed/duplicate callbacks, cancellation, outage and corrected fresh login, with zero unauthorized tickets/effects. (Depends: T022; FR-001/002/008/015/019/022; VI/XI.)
- [x] T024 [P] [US1] Add real-store mapping tests in `tests/integration/Kofte.Storage.IntegrationTests/Identity/IdentityMappingTests.cs` for concurrent same-key first login, exact case-sensitive distinct keys, changed display/email, same profiles/different subjects, same subject/sequential trusted issuers and three fresh restarts; compare exact IDs/designation/events, not counts alone. (Depends: T022; FR-003/004/007/014/016/022; IV/V/VI/XI.)
- [x] T025 [P] [US1] Add framework ticket-bridge transaction regressions in `tests/contract/Kofte.Platform.ContractTests/Identity/LoginCommitContractTests.cs`: user/mapping/creation event/attempt/session commit together before cookie, known UNIQUE resolution only, failed cookie publication and before/after-commit unknown acknowledgement. Prove `ITicketStore.StoreAsync` cannot create a second or split commit and no cookie is issued on uncertainty. (Depends: T003, T022; FR-002/003/008/015/017/022; V/VI/XI.)

### Implementation

- [x] T026 [US1] Configure maintained OIDC in `src/Kofte.Host/Features/Identity/Login/OidcConfiguration.cs` and bounded `OidcBackchannel.cs`: confidential code+PKCE, query callback, RS256, explicit exact issuer/audience/time/nonce/state, no claim remapping/token saving/UserInfo/PAR. Enforce approved HTTPS endpoint/DNS/connected-network allowlists, metadata issuer, no redirect/retry, request/byte/cache/key-refresh limits and delivered client-secret token method; fail closed if a required flow/key cannot fit the original four-second callback budget. (Depends: T008, T016, T023; FR-001/002/015/019; VI/X.)
- [x] T027 [US1] Implement exact user/external mapping in `src/Kofte.Infrastructure/Identity/IdentityUserStore.cs`: keys "(mode, issuer, subject)", "compared byte-for-byte, case-sensitively", subject "nonempty at-most-255-byte printable ASCII", issuer "at most 2,048 UTF-8 bytes", one mapping/user with matching mode and UNIQUE FK. Users have "at most 10,000 users" and display "optional and at most 128 Unicode scalar values after rejecting controls"; no email/password/role key. Reconcile only known UNIQUE conflicts against the exact committed key, preserving established designation. (Depends: T015, T024; FR-003/004/007/014; IV/V/VI.)
- [x] T028 [US1] Implement durable attempts in `src/Kofte.Infrastructure/Identity/IdentityLoginAttempts.cs`: random PK, browser-binding digest, mode/trust digest/epoch, admitted local return, UTC creation/expiry, "state `pending` or `consumed`; consumed UTC nullable". "Pending expires after five minutes; at most 256 installation-wide and eight per browser binding"; protected framework properties carry local attempt binding, never raw code/verifier/state/nonce/token columns. Compare-and-consume only within the authenticated session transaction. (Depends: T014, T023; FR-002/008/019; V/VI.)
- [x] T029 [US1] Implement server tickets in `src/Kofte.Infrastructure/Identity/IdentitySessionStore.cs` and host `Login/IdentityTicketStore.cs`: digest PK/user FK/mode/epoch/protected minimal ticket/timestamps, "state `active`, `revoked` or `expired`; terminal UTC nullable", new "256-bit random reference", domain-separated SHA-256 and no raw reference/provider claims. Enforce "at most eight active sessions per user and 50,000 installation-wide", "Idle lifetime is 30 minutes; absolute lifetime eight hours", renewal clipped to absolute expiry and CAS against terminal revocation; no eviction or cross-request positive cache. (Depends: T015, T016, T025; FR-003/008/009/014/015/019; IV/V/VI.)
- [x] T030 [US1] Implement `src/Kofte.Host/Features/Identity/Login/ValidatedLogin.cs` to construct minimal local tickets and coordinate one owner commit for exact mapping/user/event, attempt consumption and session/event. Bind the committed reference to the request-local ticket-store bridge, publish a cookie only after known commit and never silently retry an uncertain outcome. Preserve local one-second work and enclosing callback budget; do not trust caller ticket fields. (Depends: T026, T027, T028, T029; FR-002/003/008/015/017/019; III/V/VI.)
- [x] T031 [US1] Deliver browser-context, POST login and GET callback in `src/Kofte.Host/Features/Identity/Login/LoginEndpoints.cs` and `IdentityRegistration.cs`; apply framework CSRF pair/origin and independent callback binding, canonical return allowlist exactly `/` or `/identity`, default omitted path `/identity`, safe failures and refreshed identity-bound antiforgery. Set exact production/development-distinct cookie purposes and no-store/no-cache/no-referrer headers; reject unsafe/encoded returns instead of normalizing them. (Depends: T019, T030; FR-001/002/008/015/018-020; III/VI/IX.)
- [x] T032 [US1] Deliver self-only current-user handler/actor projection in `src/Kofte.Host/Features/Identity/CurrentUser/CurrentUserEndpoints.cs` and extend `src/Kofte.Host/Features/Bootstrap/BootstrapOpenApi.cs`/`packages/bootstrap-contract/src/identity.ts`. Read current durable session/user/mode/epoch/designation each request; required v1 fields are version, canonical nonempty user UUID, nullable escaped display name, identity mode and first-admin bool only. Regenerate OpenAPI/types with existing tooling; health stays bootstrap-v1 compatible. (Depends: T010, T029, T031; FR-003-007/009/018/021; II/III/VI.)
- [x] T033 [P] [US1] Add adapter tests in `apps/web/features/identity/identityAdapter.test.ts` for fixed upstream/method paths, local credential confusion rejection, approved cookie/header counts, individual Set-Cookie preservation, content/byte/deadline limits, manual redirect allowlists and uncertain lost mutation response; frozen expectations cannot reuse the production parser. (Depends: T006, T022; FR-004-006/008/015/018/019; VI/XI.)
- [x] T034 [US1] Implement `apps/web/features/identity/identityAdapter.ts`, explicit `apps/web/app/auth/login/route.ts`, `auth/oidc/callback/route.ts`, `api/identity/v1/browser-context/route.ts` and `api/identity/v1/me/route.ts`. Forward only admitted inputs to fixed server-only backend; reject unsupported/mixed Authorization locally, never forward it or proxy caller URLs/headers. Preserve separate Set-Cookie and safe Location, stream with finite limits/manual redirects/no retries and maximum 4.5-second deadline. Leave readiness adapter unchanged. (Depends: T031, T032, T033; FR-004-008/015/018/019/021; VI/IX.)
- [x] T035 [P] [US1] Add state/render tests in `apps/web/features/identity/IdentityPanel.test.tsx` for loading, signed-out, signed-in, safe provider cancellation/failure/retry, no stale identity, escaped display, safe return and CSRF-token renewal after identity changes. Test keyboard/focus/announcements without claiming real browser/protocol proof. (Depends: T006, T022; FR-004/008/015/018/020; IX/XI.)
- [x] T036 [US1] Implement `apps/web/app/identity/page.tsx`, `apps/web/features/identity/IdentityPanel.tsx` and `useIdentity.ts` with non-personal shell/no-store fetches, small accessible sign-in/identity/failure journey and safe return continuation. Keep session material out of JS/storage/SSR/cache/analytics; clear identity on failed verification, use user-directed retry and existing design primitives, not the later full shell. (Depends: T034, T035; FR-004/008/015/018/020; I/VI/IX.)
- [x] T037 [US1] Run delivered OIDC trust/egress/refusal cases in `tests/integration/Kofte.Host.IntegrationTests/Identity/OidcProtocolTests.cs` and `OidcBudgetTests.cs` on both stores, including configured private-network approval, DNS/destination changes, discovery/JWKS/token/key failure, exactly bounded requests/bytes/time/rates and no effect after timeout. Verify existing valid local sessions survive provider outage only within admitted lifetimes; no fallback/redirect loop or TLS bypass. (Depends: T026, T031, T032, T034; FR-001/002/015/018/019/022; VI/X/XI.)
- [x] T038 [US1] Execute concurrent mapping, profile-change, sequential-issuer explicit reconciliation and three fresh process restart cases in `tests/integration/Kofte.Storage.IntegrationTests/Identity/IdentityMappingTests.cs`; independently inspect exact original user/mapping/mode/designation/security state and zero unauthorized extra grants per provider. (Depends: T017, T024, T030, T032; FR-003/004/007/014/016/022; IV/V/VI/XI.)
- [x] T039 [US1] Implement/run public `oidc-login` and `trust-refusal` recipes in `tests/contract/Kofte.Identity.Consumer/Scenarios/LoginScenarios.cs` and actual browser redirect/return assertions in `tests/e2e/identity.spec.ts`. Exercise owned HTTPS OIDC on both stores with ordinary users and compare UI/direct/adapter/consumer identity; do not substitute a fabricated principal or summary count. (Depends: T003, T036, T037, T038; FR-001-004/015/021/022; VI/IX/XI.)
- [x] T040 [US1] Record US1.1-US1.5, SC-001/003 and preliminary SC-005 observations with exact integration/contract/browser commands, profile and failures in `specs/005-authentication-bootstrap/implementation.md`; verify protected missing-auth behavior and safe health remain intact before building enrollment. (Depends: T025, T039; FR-022; VI/XI/XII.)

**Checkpoint**: Real production OIDC works without optional modules, stable
identity survives profile changes/restarts, and invalid trust fails safely.
This is the first independently demonstrable slice, not completion of 005
or permission to skip other P1 stories.

## Phase 4: User Story 2 - Deliberately Establish the First Administrator (Priority: P1)

**Goal**: Stopped operator nomination plus the exact production nominee's
protected one-time submission commits exactly one durable initial designation.
**Independent Test**: No-ceremony login grants nothing; delivered nomination
and twenty eligible HTTP contenders on both stores produce one atomic
designation/consumed generation/event or no commit, including crash/reply
uncertainty and three fresh host restarts.

### Required Validation

- [x] T041 [P] [US2] Add bootstrap transition/endpoint tests in `tests/integration/Kofte.Host.IntegrationTests/Identity/EnrollmentTests.cs` for disabled/pending/consumed/recovery-locked, exact nominee, expiry/replacement, wrong token/identity/mode, CSRF, unknown fields and non-enumerating 403/409/503. Verify ordinary login, profile/email/claims and first arrival never designate an administrator. (Depends: T040; FR-010-012/014/015/018; V/VI/XI.)
- [x] T042 [P] [US2] Add stopped nomination/private-file authority tests in `tests/integration/Kofte.Storage.IntegrationTests/Identity/IdentityNominationTests.cs` for strict nominee JSON/private paths, 256-bit secret delivery, live/foreign/unadmitted scope, pending replacement/disable, consumed refusal and file-write/commit uncertainty without printing secret/nominee/path. (Depends: T040; FR-010-012/015-019; IV/V/VI/XI.)

### Implementation

- [x] T043 [US2] Implement `src/Kofte.Infrastructure/Identity/IdentityBootstrapStore.cs` over "Exactly one row, key 1", state "`disabled`, `pending`, `consumed` or `recovery_locked`", checked generation, conditional nominee/digest/expiry and "initial user ID UNIQUE FK", consumed time/generation with valid null CHECK combinations. "Initial user must be a production user"; prepared state is disabled, pending lasts fifteen minutes and consumed never returns to pending/disabled. Preserve recovery-locked historical uncertainty, not a replacement nominee. (Depends: T041, T042; FR-010-012/014/016; IV/V/VI.)
- [x] T044 [US2] Add `bootstrap authorize`/`bootstrap disable-pending` to `src/Kofte.Host/Features/IdentityMaintenance/IdentityCommand.cs` and `IdentityMaintenanceWorker.cs`. Require strict private nominee `{version:"1",issuer,subject}`, exact verified production keys, one fresh owner-only authorization file and "256-bit base64url unpadded secret, exactly 43 characters". Write/flush before committing its bound digest, increment replacement generation, erase unconsumed pending authority on disable and report non-atomic delivery truthfully; no consumed reset. (Depends: T017, T043; FR-010-012/015-019; V/VI/X.)
- [x] T045 [US2] Implement `src/Kofte.Host/Features/Identity/Enrollment/EnrollFirstAdministrator.cs`: verify durable current OIDC actor, exact nominee, installation/generation/expiry and constant-time domain-separated digest under PostgreSQL row lock/CAS or SQLite immediate transaction. Commit designation/consumption/revision/immutable event together, erase usable pending nomination/token fields and return success only on known commit; preserve one-second local budget and twenty admitted contenders. (Depends: T030, T043; FR-010-012/015/017/019; III/IV/V/VI.)
- [x] T046 [US2] Deliver POST `/api/identity/v1/first-administrator` in `src/Kofte.Host/Features/Identity/Enrollment/EnrollmentEndpoints.cs` and `apps/web/app/api/identity/v1/first-administrator/route.ts` using the shared policies/adapter. Accept only `{authorization:string}`, reject caller nominee/user/role and mixed credentials, enforce exact Origin/CSRF, safe 403/409 and truthful uncertain response; re-fetch `/me` for designation. (Depends: T034, T044, T045; FR-004-008/010/011/015/018/021; III/VI/IX.)
- [x] T047 [US2] Add `apps/web/features/identity/EnrollmentForm.tsx` and focused `EnrollmentForm.test.tsx` for signed-in nominated enrollment without nominee discovery, password-style explicit secret entry, accessible progress/rejection/retry and fresh current identity. Never prefill/render/store authorization, place it in URLs or automatically retry a lost response. (Depends: T036, T046; FR-010/015/018/020; VI/IX.)
- [x] T048 [US2] Execute no-ceremony, ordinary/other-user/development/forged, expired/replaced/replayed authorization and consumed-state cases in `tests/integration/Kofte.Host.IntegrationTests/Identity/EnrollmentTests.cs` against both providers through delivered boundaries; independently compare exact unchanged designation/ceremony/revisions/events and no public nominee/history differences. (Depends: T041, T046; FR-010-014/018/022; V/VI/XI.)
- [x] T049 [US2] Implement/run `tests/integration/Kofte.Host.IntegrationTests/Identity/EnrollmentConcurrencyTests.cs` with at least twenty simultaneously eligible HTTP submissions on each real store. Verify request admission allows all contenders, exactly one known committed transition/designation/event, safe losers and exact internal user/generation/revision correspondence; do not lower contention or bypass CSRF/rate enforcement. (Depends: T046; FR-011/017/019/022; SC-002; IV/V/VI/XI.)
- [x] T050 [US2] Implement real child before-commit/after-durable-commit/before-response barriers in `tests/integration/Kofte.Storage.IntegrationTests/Identity/EnrollmentCrashTests.cs` using the owned fixture; kill exact owned processes, reopen independently and verify neither-or-one atomic state/event with no reported success on lost acknowledgement. Corrected retry reconciles committed state, never mints another designation. (Depends: T005, T045, T046; FR-011/015-017/022; SC-002/006; IV/V/XI.)
- [x] T051 [US2] Add/run `tests/integration/Kofte.Storage.IntegrationTests/Identity/EnrollmentRestartTests.cs` for three fresh post-race host restarts, original config/authorization replay, idempotent upgrade/prepare and unavailable administrator. Compare exact designated user/consumed generation/event and unchanged unrelated module data; every later enrollment remains closed. (Depends: T049, T050; FR-011/012/016/022; SC-002/006; IV/V/VI/XI.)
- [x] T052 [US2] Exercise actual authorization-file write/flush and DB before/after-commit interruption in `tests/integration/Kofte.Storage.IntegrationTests/Identity/IdentityNominationTests.cs`. Distinguish missing, delivered, known committed and uncertain sides; explicit stopped replacement invalidates only unconsumed generations. Existing file or retry exit code alone is not delivery/commit proof. (Depends: T042, T044; FR-010-012/015-017/022; V/VI/XI.)
- [x] T053 [US2] Implement/run delivered `nominated-enrollment` and `enrollment-race` examples in `tests/contract/Kofte.Identity.Consumer/Scenarios/EnrollmentScenarios.cs`; use actual stopped command/private file, signed-in protected completion, twenty contenders and three restarts with named exact observations for both providers, individually and later under `all`. (Depends: T003, T047, T048, T051, T052; FR-010-012/017/022; SC-002/006/007; VI/IX/XI.)
- [x] T054 [US2] Add browser/API enrollment privacy and uncertainty capture assertions in `tests/e2e/identity.spec.ts` and `tests/contract/Kofte.Platform.ContractTests/Identity/EnrollmentContractTests.cs`; verify fresh designation query, no raw secret/nominee/provider payload outside necessary exchange, no browser automatic retry and no ungranted operation becomes allowed after enrollment. (Depends: T047, T050, T053; FR-004/007/010/011/015/018/020/022; VI/IX/XI.)
- [x] T055 [US2] Run focused integration/recovery/contract/browser selections and record all US2.1-US2.5, twenty-way/three-restart and exact transaction observations in `specs/005-authentication-bootstrap/implementation.md`. Retain failures and explicit IdP-loss/no-reset limitation; successful enrollment is not granular authorization or human feature acceptance. (Depends: T054; FR-022; V/VI/XI/XII.)

**Checkpoint**: Deliberate non-repeatable enrollment, real concurrency/crash
semantics and public recipes are evidenced on both providers. No general
administrator reassignment or anonymous reset route exists.

## Phase 5: User Story 3 - Keep Browser and API Access Within the Same Boundary (Priority: P1)

**Goal**: One trusted actor/credential and default-denied authority across
direct, adapted and test-owned module access, with durable local sign-out.
**Independent Test**: Compare exact actor and `/me` values; deny anonymous,
forged/mixed/cross-origin/ungranted actions before effects, revoke/expire and
verify next-request denial, while anonymous safe health remains unchanged.

### Required Validation

- [x] T056 [P] [US3] Add independent direct/adapter/actor parity cases in `tests/contract/Kofte.Platform.ContractTests/Identity/IdentityParityTests.cs` and `tests/contract/identity.test.ts`: ordinary/admin exact identity, 401 versus 403/503, bearer/ID-token/mixed-cookie refusal, spoofed identity/proxy/header/body/profile fields and ungranted action denial even for the initial administrator. (Depends: T055; FR-004-008/018/021/022; II/VI/XI.)
- [ ] T057 [P] [US3] Extend `tests/integration/Kofte.Host.IntegrationTests/Identity/IdentityRequestTests.cs` with exact method/path/query/body/duplicate/unknown-field, return/redirect/cookie/header/input bounds, CSRF pair renewal, missing/null/foreign Origin and cross-site Fetch Metadata cases. Assert zero protected effects and distinguish legitimate OIDC GET callback binding from ordinary mutation Origin checks. (Depends: T055; FR-005-008/018/019/022; VI/XI.)
- [ ] T058 [P] [US3] Add `tests/integration/Kofte.Storage.IntegrationTests/Identity/IdentitySessionTests.cs` covering controlled-clock idle/absolute boundaries, renew/revoke races, next-request fresh designation, wrong-mode/epoch, restart/key rotation/loss and revocation-record/commit failures. Wall-clock effect deadlines remain real; no positive authentication cache or renewed terminal session. (Depends: T055; FR-008/009/014-019/022; V/VI/XI.)

### Implementation

- [ ] T059 [US3] Implement `src/Kofte.Host/Features/Identity/SignOut/SignOutEndpoints.cs` and owner-local revocation in `src/Kofte.Infrastructure/Identity/IdentitySessionStore.cs`; self-session only, current session + Origin/CSRF, terminal revocation/event in one transaction, 204 only on confirmed commit, 401 for invalid/expired, 503 on uncertainty. Expire local cookie without promising provider-wide logout or known server revocation after a lost commit response. (Depends: T019, T029, T058; FR-006/008/009/015/017; III/V/VI.)
- [ ] T060 [US3] Add exact sign-out route `apps/web/app/api/identity/v1/sign-out/route.ts` and update `apps/web/features/identity/useIdentity.ts`/`IdentityPanel.tsx` for sign-out/expiry/uncertain stale-state clearing, identity-bound CSRF refresh and explicit re-entry. No optimistic known-revocation result, persisted identity, automatic mutation retry or external provider logout claim. (Depends: T034, T036, T059; FR-004/008/009/015/020; VI/IX.)
- [x] T061 [US3] Implement test-owned current-actor and ungranted-action probes in `tests/contract/Kofte.Identity.Consumer/ActorProbe.cs` and `Scenarios/ActorScenarios.cs`, hosted only by the test-owned composition. Compare public actor-v1 to `/me` without identity-table access, deny the unrelated action for ordinary and initial-admin actors and never deploy that route/permission shortcut in production. (Depends: T010, T056; FR-005/007/021/022; II/III/VI/VII/XI.)
- [ ] T062 [US3] Complete independent API/adapter error and policy assertions in `tests/contract/Kofte.Platform.ContractTests/Identity/IdentityParityTests.cs` against the shared `src/Kofte.Host/Features/Identity/IdentityRequestBoundary.cs`: fixed problem schema/status, unsupported/mixed credential 401, authenticated insufficient-authority 403 and unavailable verification 503, no redirect/provider HTML or success-shaped null actor. Fix any directly exposed parity defect in those shared boundaries. (Depends: T056, T057, T059, T061; FR-005-008/015/018/021/022; VI/XI.)
- [ ] T063 [US3] Exercise current session/user/mode/epoch/designation retrieval in `tests/contract/Kofte.Platform.ContractTests/Identity/SessionContractTests.cs` for empty and composed hosts, direct and adapted paths. Confirm metadata/epoch revocation is effective on the very next protected request and initial designation neither grants module controls nor supplies offline maintenance authority; correct request-scoped wiring in `IdentityRegistration.cs` where needed. (Depends: T021, T058, T061, T062; FR-005/007/009/014/021/022; II/VI/XI.)
- [ ] T064 [US3] Execute the entire independent HTTP/cookie/Origin/CSRF/redirect corpus from `tests/contract/identity-corpus.ts` through both paths and real stores using `tests/contract/identity.test.ts`; observe exact Allow/404/405/400/error/cache headers, individual cookies and no unknown route/control bypass, not only expected status strings. (Depends: T057, T062, T063, T074; FR-005-008/018/019/022; VI/XI.)
- [ ] T065 [US3] Run both-provider controlled-clock and real-process session tests in `tests/integration/Kofte.Storage.IntegrationTests/Identity/IdentitySessionTests.cs`, including ordinary retained-key rotation, explicit epoch/client-trust changes, unavailable/corrupt row/key and renewal racing confirmed sign-out. Verify exact terminal rows/events and next-request denial without changing admitted lifetimes. (Depends: T058, T059, T063; FR-008/009/014-017/022; VI/X/XI.)
- [ ] T066 [US3] Add two-user no-store/SSR/adapter isolation tests in `tests/e2e/identity.spec.ts` and `tests/contract/identity.test.ts`; switch/logout/expire identities and ensure stale protected content never flashes or appears for another user. Retain exact anonymous health and Development-only OpenAPI assertions in existing `tests/integration/Kofte.Host.IntegrationTests/HealthEndpointTests.cs`/`OpenApiTests.cs` without broad route whitelisting. (Depends: T060, T064; FR-004/005/008/009/018/022; VI/IX/XI.)
- [ ] T067 [US3] Execute browser sign-out/idle/absolute expiry, denied action, invalid Origin/CSRF and uncertain-response journeys in `tests/e2e/identity.spec.ts` against actual host/web/issuer. Assert user-visible stale state clears and retry requires legitimate login, while the host rejects the old credential on the next direct and adapted request. (Depends: T060, T065, T066; FR-005-009/015/020/022; VI/IX/XI.)
- [ ] T068 [US3] Complete independent C#/TypeScript shape/version tests in `tests/contract/Kofte.Platform.ContractTests/Identity/IdentityHttpContractTests.cs`, `tests/contract/identity.test.ts` and `packages/bootstrap-contract/src/identity.test.ts`; verify required/nullable values, safe additive response tolerance versus strict requests, regenerated identity OpenAPI/types and unchanged health shape, never synthesize missing identity. Run `npm run generate --workspace packages/bootstrap-contract` and `npm run contract:check`. (Depends: T032, T046, T059, T064; FR-004/006/018/021/022; II/VI/XI.)
- [ ] T069 [US3] Implement/run public `session-boundary` in `tests/contract/Kofte.Identity.Consumer/Scenarios/SessionScenarios.cs`, combining exact direct/adapter/actor equality, 401/403, CSRF, finite expiry, durable sign-out and renewal race with independent named observations for each real provider. (Depends: T003, T061, T065, T067, T068; FR-004-009/021/022; SC-001/003; VI/XI.)
- [ ] T070 [US3] Run focused unit/integration/contract/browser/privacy gates and record all US3.1-US3.5 observations, original safe-health compatibility and next-request revocation evidence in `specs/005-authentication-bootstrap/implementation.md`; note no immediate upstream IdP revocation or general permission engine was promised. (Depends: T069; FR-022; VI/IX/XI/XII.)

**Checkpoint**: All P1 stories have delivered interfaces and independent
evidence. This is an incremental demo boundary, not permission to accept 005
without local development, safe operation/recovery and complete verification.

## Phase 6: User Story 4 - Develop Locally Without a Production Backdoor (Priority: P2)

**Goal**: Explicit selected synthetic development identity works locally, is
visibly marked and cannot become production authentication or enrollment.
**Independent Test**: Positive local UI/API/actor parity; negative full
environment/exposure matrix and persisted dev-to-OIDC transition on both stores.

### Required Validation

- [ ] T071 [P] [US4] Add development environment/opt-in/listener/origin/backend/unsafe-switch matrix in `tests/unit/Kofte.Host.Tests/Features/Identity/DevelopmentIdentityTests.cs`; missing/foreign mode/config and non-loopback combinations reject before serving, and the endpoint is 404 outside admitted development. (Depends: T055; FR-013-015/019/022; VI/XI.)
- [ ] T072 [P] [US4] Add real-store development-to-production regressions in `tests/integration/Kofte.Storage.IntegrationTests/Identity/DevelopmentIsolationTests.cs` for persisted user/ticket, equal profile values, explicit trust reconciliation/restart and production pending/consumed bootstrap preservation; no user merging or dev enrollment. (Depends: T055; FR-003/010/013/014/016/022; IV/V/VI/XI.)

### Implementation

- [ ] T073 [US4] Implement `src/Kofte.Host/Features/Identity/Development/DevelopmentEndpoints.cs` using configured synthetic `identityKey` only and the same owner transaction/session/CSRF/admission/evidence primitives. Enforce "Development issuer is the fixed separate `kofte:development:v1` namespace" and matching mode throughout, separate `kofte-dev-session` cookie, no arbitrary email/subject/admin input and zero production enrollment or automatic fallback. (Depends: T030, T031, T071, T072; FR-003/010/013-015/017/019; III/VI.)
- [ ] T074 [US4] Add `apps/web/app/api/identity/v1/development-session/route.ts` and `apps/web/features/identity/DevelopmentIdentitySelector.tsx` with exact adapter contract, configured synthetic choices and persistent visible Development label. Extend component tests for selection, clear stale state and safe denial without exposing production identity or private configuration. (Depends: T034, T060, T073; FR-004/013/014/018/020; I/VI/IX.)
- [ ] T075 [US4] Update explicit local configuration generation/forwarding in `scripts/dev.py`, `scripts/runtime_support.py` and owned test fixtures plus `scripts/tests/test_dev.py`/applicable existing supervisor tests. Keep initialization a separate explicit stopped recipe (`--store init`, `--registry upgrade`, `--identity prepare`); the serving supervisor never creates/migrates stores or repairs a missing key ring. Owned fresh test fixtures perform that same admitted preparation before serving. Preserve two-process topology, ownership/exposure restrictions, cleanup and all existing failure propagation; no auto-adoption/repair of a foreign root. (Depends: T017, T073; FR-013-016/019/022; VI/X/XI.)
- [ ] T076 [US4] Run the entire negative exposure/environment matrix against actual startup and direct/adapted development endpoint in `tests/integration/Kofte.Host.IntegrationTests/Identity/DevelopmentBoundaryTests.cs`; prove unsafe-local-exposure opt-in never overrides identity restrictions and production source contains no TLS/test principal bypass. (Depends: T071, T074, T075; FR-013-015/019/022; SC-004; VI/X/XI.)
- [ ] T077 [US4] Execute both-provider persisted-development/profile-equality/config-reconciliation/restart cases in `tests/integration/Kofte.Storage.IntegrationTests/Identity/DevelopmentIsolationTests.cs`; reject old dev tickets at the next production request and compare exact original users/mappings/designation/events. A dev selection cannot consume pending production authority or inherit a consumed designation. (Depends: T017, T072, T073, T076; FR-003/010/013/014/016/022; SC-004/006; IV/VI/XI.)
- [ ] T078 [US4] Implement/run `development-isolation` in `tests/contract/Kofte.Identity.Consumer/Scenarios/DevelopmentScenarios.cs` and development Playwright cases in `tests/e2e/identity.spec.ts`; record US4.1-US4.3, visible mode/UI/API/actor equality and all negative transitions in `specs/005-authentication-bootstrap/implementation.md` after focused unit/integration/contract/browser gates. (Depends: T003, T074, T077; FR-013/014/020/022; SC-004/005; VI/IX/XI.)

**Checkpoint**: Ordinary local development needs no real IdP/Docker or
production secret. Its convenience creates no production identity/authority
and preserves the accepted exposure/fixture restrictions.

## Phase 7: User Story 5 - Operate and Recover Identity Safely (Priority: P2)

**Goal**: Explicit preparation, trustworthy private evidence and stopped
complete-set recovery preserve identity/module state without reopening bootstrap.
**Independent Test**: Both real providers clean/v1/current prepare, physical
tamper/crash/unavailable state, restored-copy quarantine and correction;
capture all channels and execute accessible bounded failure/retry journeys.

### Required Validation

- [ ] T079 [P] [US5] Extend `tests/integration/Kofte.Storage.IntegrationTests/Identity/IdentityUpgradeTests.cs` with real clean v0-to-registry-v1-to-identity-v2, accepted v1 upgrade and current idempotent prepare, old/unknown/corrupt format/history/object refusals, read-only serve and exact module/data/marker/allocator/history/revision preservation. Use independent pre-upgrade accepted-format fixtures; never rewrite original migrations. (Depends: T078; FR-003/012/016/022; SC-006; IV/V/X/XI.)
- [ ] T080 [P] [US5] Add recovery-v1/v2 manifest/actual-state/quarantine/decision tests in `tests/integration/Kofte.Storage.IntegrationTests/Identity/IdentityRecoveryTests.cs`: exact artifacts/hashes/security snapshot, format mismatch, altered constraints/data/summary, missing module artifact, stale bootstrap history, unknown decision fields/binding and interrupted reconciliation; legacy platform-only/reset remains refused. (Depends: T078; FR-009/011/012/015/016/022; SC-006; IV/V/VI/XI.)

### Implementation

- [ ] T081 [US5] Extend `src/Kofte.Infrastructure/Storage/Recovery/RecoveryManifest.cs`/`RecoverySetVerifier.cs` and `src/Kofte.Infrastructure/Identity/IdentityRecoverySnapshot.cs` with explicit `kofte-storage-recovery-v2` only for platform format 2; v1 remains only actual format 1. Include schema/security revision/epoch/bootstrap consumed generation/counts/deterministic SHA-256 security snapshot and full platform/all-module artifacts; compare actual restored constraints/history/rows, not manifest self-description. Never include private keys/secrets or relax exact inventories. (Depends: T013, T079, T080; FR-012/016/018; IV/V/VI/X.)
- [ ] T082 [US5] Implement restored-copy gating and `reconcile-restore` in `src/Kofte.Host/Features/IdentityMaintenance/IdentityRestore.cs`, `IdentityCommand.cs` and affected `src/Kofte.Host/Bootstrap/SqliteStoreRecovery.cs`/complete-set provider paths. Write `identity-restore-required` outside the recovered snapshot before activation; require strict private decision version/recoverySetId/installationId/expectedManifestSha256/priorEnrollment (`known-consumed`, `known-never-enrolled`, `not-established`). Revoke restored sessions/attempts/admission/pending authority, advance epoch, establish fresh protected ring and commit decision before clearing gate; retain users/mappings/events/designation. Preserve recovered consumed state; unknown/lost consumed history stays `recovery_locked`, not a second nominee. (Depends: T017, T021, T081; FR-009/011/012/015-017/019; IV/V/VI/X.)
- [ ] T083 [P] [US5] Add real key-loss/foreign/undecryptable ring, valid retained-key rotation, certificate overlap, client-secret replacement and explicit stopped correction tests in `tests/integration/Kofte.Host.IntegrationTests/Identity/IdentityKeyTests.cs`. Verify no serve-time replacement, epoch/next-request effects, stable mapping/consumed designation, and separation of key custody from ordinary database backups. (Depends: T078; FR-008/009/012/015/016/019/022; VI/X/XI.)
- [ ] T084 [US5] Add real prepare/DB-commit/key-ready barriers and scope/deadline/termination regressions in `tests/integration/Kofte.Storage.IntegrationTests/Identity/IdentityPreparationCrashTests.cs`; interrupt each boundary, verify physical/authority state independently and complete only explicit owned retry. Preserve 120-second/inherited budgets and test live host, foreign/lost lease, direct worker and unverified descendant cleanup refusal without releasing authority early. (Depends: T005, T079, T082; FR-015/016/019/022; SC-006; IV/V/X/XI.)
- [ ] T085 [US5] Execute actual complete v1/v2 backup/restore on both providers in `tests/integration/Kofte.Storage.IntegrationTests/Identity/IdentityRecoveryTests.cs`: stopped quarantine blocks exposure, exact manifest-bound decisions preserve committed security/module state, consumed evidence overrides `known-never-enrolled`, unknown history remains locked and interruption stays resumable. V1 requires explicit upgrade/reconciliation, old binaries reject v2, platform-only reset is refused and no Down/deletion/reassignment recipe is used. (Depends: T080, T082, T083, T084; FR-009/011/012/015/016/022; SC-006; IV/V/VI/X/XI.)
- [ ] T086 [US5] Execute login/enrollment/current-user/session store-unavailable/corrupt/record-failure and commit uncertainty cases in `tests/integration/Kofte.Host.IntegrationTests/Identity/IdentityFailureTests.cs` using actual owned stores and safe effect observations. Verify bounded 503, no cached successful actor/new cookie, no success for uncertain mutation, corrected explicit recovery and no extra designation; capture exact inherited PostgreSQL failure assertion/phase if it recurs instead of assigning a speculative cause. (Depends: T050, T065, T085; FR-003/009/011/012/015-017/022; SC-006; V/VI/XI.)
- [ ] T087 [US5] Implement stopped bounded `maintain` in `src/Kofte.Host/Features/IdentityMaintenance/IdentityCommand.cs` and `src/Kofte.Infrastructure/Identity/IdentityMaintenance.cs`, reusing admission/event cleanup helpers: only expired session-class evidence and terminal attempts/admission windows in at-most-128 batches within the original operation budget. Never trim installation evidence, silently evict active sessions, change a consumed ceremony or add an unbounded sweep/background job. (Depends: T014, T015, T017; FR-009/012/017/019; V/VI/X.)
- [ ] T088 [US5] Add/run boundary/retention/failure tests in `tests/integration/Kofte.Storage.IntegrationTests/Identity/IdentityCapacityTests.cs` for every stored capacity/rate/window/expiry at limit and one over, ninety-percent warning, 30-day session-record pruning, checked counter overflow and durable-record failure rolling back its owner mutation. Reopen stores to prove rate/admission limits survive restart and maintenance preserves installation records/other users. (Depends: T006, T029, T043, T086, T087; FR-009/011/015/017/019/022; V/VI/XI.)
- [ ] T089 [US5] Capture all exercised success/reject/expiry/sign-out/operator/uncertain outcomes in `tests/integration/Kofte.Storage.IntegrationTests/Identity/IdentityPrivacyTests.cs`, `tests/contract/privacy.test.ts` and `tests/e2e/privacy.spec.ts`. Independently scan responses/redirects/pages/caches, .NET/Next/ingress-access logs, spans/metrics, composition/maintenance IPC and durable events for seeded secret/identity canaries outside necessary protected exchange; assert useful fixed correlation/outcome/durability, low-cardinality labels and recording/sink failure. Keep transient buffers private, scan then discard; fail on absent/broken capture, never persist raw protocol HAR/traces/screenshots. (Depends: T020, T054, T066, T078, T086, T088; FR-017-019/022; SC-007; VI/X/XI.)
- [ ] T090 [US5] Complete actual Playwright/axe evidence for every loading/signed-out/signed-in/expired/development/cancelled/unavailable/uncertain/retry state in `tests/e2e/identity.spec.ts` and `tests/e2e/accessibility.spec.ts`. Verify keyboard/visible focus/announcements, 320-CSS-pixel width, 200% text, no serious/critical axe findings and actionable result within five seconds after trusted return; report actual profile and provider/user time separately. Preserve only safe local destinations; do not invent human study evidence. (Depends: T036, T047, T067, T078, T086; FR-004/009/013/015/018/020/022; SC-005/007; I/VI/IX/XI.)
- [ ] T091 [US5] Implement/run `upgrade-preservation`, `interrupted-recovery` and `private-evidence` recipes in `tests/contract/Kofte.Identity.Consumer/Scenarios/OperationsScenarios.cs` through delivered stopped/HTTP interfaces and both owning provider harnesses; require exact before/after identities/designation/module data/history, real interruption and safe captures, not manual DB editing or only zero exit. Include documented administrator/IdP-loss refusal and stale-copy limitations. (Depends: T003, T085, T088, T089; FR-003/009/011/012/015-019/022; SC-006/007; IV/V/VI/XI.)
- [ ] T092 [US5] Run focused real-provider integration/recovery/privacy/browser/failure-propagation gates and record all US5.1-US5.4 plus key/capacity/retention/restore decisions and exact commands/results in `specs/005-authentication-bootstrap/implementation.md`. Preserve historical failed/incomplete results and OS-copy/old-instance fencing/no rollback-proof administrator-state limitations; documentation cannot turn unavailable evidence into a pass. (Depends: T079, T083, T084, T086, T090, T091; FR-022; IV/V/VI/IX/X/XI/XII.)

**Checkpoint**: Both providers prove explicit preparation/upgrade/restore and
exact state preservation. All introduced UI states, safe useful evidence and
truthful correction paths have actual results, not just design assertions.

## Final Phase: Cross-Cutting Evidence and Documentation

- [ ] T093 Extend required identity source/project/case/observation inventories in `scripts/verify.py` and `scripts/tests/test_verify.py`, and existing `.github/workflows/product-checks.yml` suite selection where needed. Include identity unit/provider integration/recovery/contract/browser/privacy in existing named suites; fail on missing source, fixture, observation, scanner, skip or unverified cleanup instead of silently omitting it. Preserve all applicable 001-004 suites/deadlines/minima and existing CI contexts. (Depends: T040, T055, T070, T078, T092; FR-022; XI/XII.)
- [ ] T094 Execute every public consumer scenario individually and `all` for both providers through `tests/integration/Kofte.TestSupport/Identity/IdentityConsumerHarness.cs` and `tests/contract/Kofte.Platform.ContractTests/Identity/IdentityGuideTests.cs`; validate the nine named scenario catalogs and exact observations against `specs/005-authentication-bootstrap/quickstart.md`. Add harness negative tests for absent/truncated/duplicate observations, nonzero child, timeout, missing source and cleanup/capture failure; browser/a11y results stay independent. (Depends: T039, T053, T069, T078, T091, T093; FR-022; SC-001-007; VI/XI.)
- [ ] T095 Convert `specs/005-authentication-bootstrap/quickstart.md` to tested delivered recipes and update `README.md`, `CONTRIBUTING.md`, `docs/README.md`, `docs/architecture.md`, `docs/development/local-development.md` and `docs/development/spec-workflow.md` for actual identity/setup/API changes. Document HTTPS/ingress/proxy/network trust, strict config/private secret delivery, nomination/replacement, session limits/revocation/key overlap, explicit upgrade, complete-set quarantine/reconciliation, retention/capacity and IdP-loss/OS-copy/old-instance fencing/rollback limitations; distinguish future 006/007/008/009/022-024 and human acceptance. (Depends: T094; FR-001-022; I/VI/IX/X/XII.)
- [ ] T096 Reconcile actual final dependency/lock/archive/license/advisory/notices evidence in `docs/development/dependencies.md`, `docs/development/dependency-inventory.json` and applicable `docs/development/third-party-notices.md`/retained package notices with the adopted source; run existing dependency and generated-contract verification, resolving introduced drift without unnecessary version changes. No unreviewed product or test package may enter through fixtures. (Depends: T002, T093, T095; FR-001/021/022; III/VI/X/XI.)
- [ ] T097 Run the complete `python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10` gate and required repository/Python/whitespace checks; save only safe exact results and assessed code/profile in `specs/005-authentication-bootstrap/implementation.md`. Require actual both-provider/new identity evidence plus retained manifest/storage/composition/empty-host/health/OpenAPI/browser/startup/failure gates, including ten empty warm starts, without suppression, relaxed deadline, substituted rerun or false complete-gate claim. Diagnose newly encountered in-scope failures with exact assertions; historical deferral is no waiver for a new failure. (Depends: T094, T095, T096; FR-022; SC-001-007; X/XI/XII.)
- [ ] T098 Verify actual assessed-head GitHub CI contexts/reports from `.github/workflows/product-checks.yml` and repository checks, compare named identity/retained steps and profile coverage with local evidence, and record exact runs/commits/results/limitations in `specs/005-authentication-bootstrap/implementation.md`. A local subset, workflow definition or historical green matrix cannot replace this observation. (Depends: T097; FR-022; X/XI/XII.)
- [ ] T099 Reconcile all 22 FRs, seven SCs, 22 scenarios, twelve R decisions and principles I-XII to delivered source/assertions/exact results in `specs/005-authentication-bootstrap/implementation.md`; inspect the intended diff for scope, secrets/generated artifacts, test ownership, shared actor/default denial, immutable module isolation and truthful partial effects. Keep reviewer-owned checklist markers/approved intent unchanged and record any unresolved blocker rather than checking it away. (Depends: T098; FR-001-022; I-XII.)
- [ ] T100 Update current implementation progress in `README.md` and `docs/development/spec-workflow.md`, reconcile task markers only with actual evidence and publish implementation artifacts plus ledger together on this same branch/draft PR using `ship-change`. Complete only the active implement stage when its checks genuinely pass; push immediately after each commit and verify same-PR status. Converge, review and explicit numbered-feature acceptance are later invocations; no readiness/merge/close/auto-merge/branch deletion. (Depends: T099; FR-022; XI/XII.)

## Dependencies and Execution Order

Every direct edge is in its task's `Depends` clause. Setup has one root:
T001 -> T002. T003-T007 then prepare independent test/fixture surfaces.
T008-T022 establish shared boundaries before any story checkpoint.

```text
Setup T001-T006
  -> Foundation T007-T022
  -> US1 T023-T040 (P1: real trusted login, stable identity)
  -> US2 T041-T055 (P1: nominated atomic first administrator)
  -> US3 T056-T070 (P1: all access/session/actor boundaries)
  -> US4 T071-T078 (P2: explicit local development and isolation)
  -> US5 T079-T092 (P2: operations, recovery, privacy, accessible failures)
  -> Cross-cutting T093-T100 (all-suite, actual CI, final evidence)
```

Story ordering is intentional: enrollment needs a real verified production
session; cross-interface/session verification exercises enrolled and ordinary
actors; development proves it cannot inherit that production state; operations
then exercises the complete delivered system. Tests are independently assessable
per story on private installations even though they reuse earlier foundations.
US1 is the first demo, **all five stories are required for 005**.

Within a phase, tests precede dependent implementation; tests may initially
fail to compile or run because the approved behavior does not exist yet.
Acceptance execution follows all relevant wiring. Numeric bounds use controlled
clocks only for logical expiry; effects/cleanup use actual processes and
wall-clock limits. Test-only barriers never become deployed flags/endpoints.

### Genuine parallel examples

| Scope      | Parallel work after prerequisites                                                                                       | Join / constraint                                                          |
| ---------- | ----------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------- |
| Setup      | T004 issuer, T005 process fixtures, T006 frozen corpus and T007 config tests after T002                                 | Different source files; serialize project/lock changes in T003.            |
| Foundation | T009 actor contracts after T003, T011 physical tests after T005, and T018 boundary tests after its listed prerequisites | T010/T012/T019 consume them; no simultaneous shared-file edits.            |
| US1        | T023 protocol tests, T024 mapping tests, T025 bridge tests, T033 adapter tests and T035 component tests after T022      | Each owns distinct files; T030/T034/T036 join dependent work.              |
| US2        | T041 HTTP/bootstrap transitions and T042 stopped private-file authority after T040                                      | T043 joins; later actual store contenders run within one isolated fixture. |
| US3        | T056 parity, T057 request/CSRF and T058 session tests after T055                                                        | T059-T069 join; shared contract/Playwright files are serialized.           |
| US4        | T071 environment tests and T072 persisted-isolation tests after T070                                                    | T073 joins before delivered dev route/UI.                                  |
| US5        | T079 upgrade, T080 recovery tests and T083 keys after T078                                                              | T081/T082 join upgrade/recovery; T085 also requires keys/crash proof.      |

`[P]` marks 22 candidates across these conditional waves. It is not permission
to run all fixtures together against one provider installation or to spawn a
registered multi-agent workflow.

## Validation Commands and Evidence Rules

Use exact pinned tool selection/locked restore in the local guide. This stage
does not install packages or execute the following product commands. During
implementation, use the smallest relevant named suite at each story checkpoint;
combine selectors where the existing runner supports them and keep both real
providers for any persistence claim.

```bash
python3 scripts/check_repository.py
python3 -m unittest discover -s scripts/tests -v
git diff --check
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

Quality includes existing .NET format/build/warnings, web
Prettier/ESLint/type/build, dependency/notice/lock and generated contract checks;
regenerate with `npm run generate --workspace packages/bootstrap-contract` and
verify `npm run contract:check`. Do not use a successful formatting check as
protocol or provider proof. Product checks with new cases are implementation
obligations; documentation-only task generation runs the repository gate only.

The consumer catalog is exactly `oidc-login`, `trust-refusal`,
`nominated-enrollment`, `enrollment-race`, `session-boundary`,
`development-isolation`, `upgrade-preservation`, `interrupted-recovery` and
`private-evidence`. Deliver and actually validate the guide's command:

```bash
dotnet run --project tests/contract/Kofte.Identity.Consumer -c Release --no-build -- --scenario all --provider sqlite
```

Each scenario must also run individually on both stores through its owning
harness; PostgreSQL remains the disposable wrapper's responsibility. Missing
source/fixture/observations/scanner/verified cleanup is failure, not a skip.

Preserve the known 004 failed/interrupted local gate and undiagnosed deferred
PostgreSQL observation, plus 005's earlier specification-head private-diagnostics
failure. Capture exact assertion and phase evidence if encountered; no causation
or waiver is inferred from later green CI. Newly executed checks have their own
actual results; do not erase an initial failure by listing only a rerun.

## Constitutional Coverage and Scope

### Requirement and outcome coverage

| Intent | Implementation / independently required evidence                                                                              |
| ------ | ----------------------------------------------------------------------------------------------------------------------------- |
| FR-001 | T002/T004/T026/T031; actual T023/T037/T039/T094.                                                                              |
| FR-002 | T026/T028/T030/T031; independent T023/T025/T037/T064.                                                                         |
| FR-003 | T012/T027/T030; exact concurrency/profile/issuer/restart T024/T038/T077/T079.                                                 |
| FR-004 | T032/T034/T036/T046/T060/T074; equality/schema/cache T039/T054/T056/T066/T068.                                                |
| FR-005 | T019/T021/T032; direct/adapter/module/health T018/T056/T061-T064/T066.                                                        |
| FR-006 | T019/T032/T046/T059; fixed status/no HTML T056/T062/T064/T068.                                                                |
| FR-007 | T010/T019/T032; spoof/default-denial/initial-admin T024/T054/T056/T061-T063.                                                  |
| FR-008 | T016/T019/T028-T031/T034/T059; binding/CSRF/cookie/cache T023/T025/T033/T057/T064-T067.                                       |
| FR-009 | T017/T029/T059/T060; controlled expiry/races/rotation/recovery T058/T063/T065/T067/T083/T085.                                 |
| FR-010 | T043-T047; no ceremony/nominee/dev/expiry/replacement T041/T042/T048/T052/T077.                                               |
| FR-011 | T012/T043/T045; twenty-way/crash/reply/restart atomicity T049-T051/T053/T085.                                                 |
| FR-012 | T017/T043/T044/T082; permanent closure/lost admin/stale restore T048/T051/T085/T091/T095.                                     |
| FR-013 | T008/T073-T075; full exposure/environment/UI T007/T071/T076/T078/T090.                                                        |
| FR-014 | T014/T017/T029/T073; persisted namespace/trust/no-grant T024/T065/T072/T077.                                                  |
| FR-015 | T008/T019/T020/T026/T030/T059/T082; no fallback/uncertainty T023/T037/T050/T058/T084-T086/T090.                               |
| FR-016 | T012-T017/T021/T081/T082; exact ownership/clean/upgrade/crash/recovery T011/T022/T038/T051/T079/T084/T085.                    |
| FR-017 | T015/T020/T030/T044/T045/T059/T082/T087; durable owner commit/capacity/correlation T025/T049/T050/T088/T089.                  |
| FR-018 | T019/T020/T031/T032/T034/T046; non-enumeration/private channels T048/T054/T062/T066/T089.                                     |
| FR-019 | T008/T014/T016/T026/T028/T029/T034/T044/T087; all numeric/input/egress/rate/retention bounds T006/T022/T037/T057/T064/T088.   |
| FR-020 | T036/T047/T060/T074; all state/return/keyboard/focus/timing evidence T035/T039/T054/T067/T078/T090.                           |
| FR-021 | T010/T032/T061/T068; separate consumer/public contract T009/T056/T063/T069.                                                   |
| FR-022 | T001/T003-T006/T093-T100; all story checkpoints and actual local/CI results, never source-presence-only proof.                |
| SC-001 | T038/T039/T065/T069/T094: production login/sign-out, exact identity/profile/restart both stores.                              |
| SC-002 | T048-T053/T085: twenty eligible contenders, three fresh restarts, one exact consumed designation/event and zero other grants. |
| SC-003 | T023/T037/T048/T056/T061-T069: negative trust/access before effects on direct/adapted/module boundaries.                      |
| SC-004 | T071/T072/T076-T078: admitted development only, no production authentication/merge/grant.                                     |
| SC-005 | T039/T067/T078/T090: actual post-return <= five seconds and all-state keyboard/320px/200%/axe evidence.                       |
| SC-006 | T025/T038/T050-T052/T079/T083-T086/T091: exact committed security/module state across real interruptions/upgrade/restore.     |
| SC-007 | T054/T088-T091/T094: useful safe channel captures and delivered correction recipes, no manual database edit.                  |

### Every acceptance scenario

| Scenario | Concrete acceptance task(s)                                                                                |
| -------- | ---------------------------------------------------------------------------------------------------------- |
| US1.1    | T039: actual browser code flow and UI/direct/adapter/consumer equality.                                    |
| US1.2    | T024/T038: concurrent first login, changed profile and three fresh restarts preserve exact ID/designation. |
| US1.3    | T024/T038: distinct subject/issuer despite equal profiles, explicit trust reconciliation.                  |
| US1.4    | T023/T025/T037: independently signed invalid/replayed/unsolicited evidence, no unauthorized ticket.        |
| US1.5    | T037/T039: cancelled/outage/config/key/budget refusal, corrected fresh login, no fallback/loop.            |
| US2.1    | T041/T048: unconfigured ceremony and ordinary login produce no designation/disclosure.                     |
| US2.2    | T053/T054: stopped private delivery and exact signed-in nominee commit state/event.                        |
| US2.3    | T048/T052/T077: wrong identity/mode/forgery/expired/replaced/replay, unchanged authority.                  |
| US2.4    | T049-T051/T053: twenty eligible HTTP contenders, real commit barriers, uncertainty and three restarts.     |
| US2.5    | T051/T085/T091: consumed stays closed across replay/upgrade/admin loss/stale restore.                      |
| US3.1    | T056/T062/T064/T069: direct/adapter/module anonymous/forged/bearer/mixed refusal, no login HTML.           |
| US3.2    | T039/T056/T061/T063/T069: exact actor/identity/mode/designation parity, no override/granted permission.    |
| US3.3    | T057/T061/T064/T067: first admin still denied ungranted action; Origin/CSRF checked before effects.        |
| US3.4    | T058/T065/T067/T069: durable sign-out/finite expiry/races/epoch and next-request stale-state denial.       |
| US3.5    | T064/T066/T068: unchanged anonymous aggregate health and separate-user no-store/SSR isolation.             |
| US4.1    | T074/T078: configured synthetic selection, exact parity, persistent Development label.                     |
| US4.2    | T071/T076: environment/listener/origin/backend/exposure switch all fail closed.                            |
| US4.3    | T072/T077/T078: persisted dev-to-OIDC restart rejects old tickets, no merge or production bootstrap.       |
| US5.1    | T011/T022/T079: clean/upgrade/idempotent prepare, read-only serve and exact original ownership/data.       |
| US5.2    | T084-T086/T091: corrupt/unavailable/partial store/key/restore effects, explicit corrected recovery.        |
| US5.3    | T088/T089/T091: all outcome/channel captures, durable evidence/capacity/retention/sink failure.            |
| US5.4    | T090: actual accessible all-state failure/retry/safe return and post-return timing.                        |

### Research/design decisions and principles

| Design decision                    | Task ownership                                                    |
| ---------------------------------- | ----------------------------------------------------------------- |
| R1 maintained confidential OIDC    | T002/T004/T023/T026/T028/T030/T031/T037.                          |
| R2 exact same-origin adapters      | T033/T034/T046/T060/T064/T066/T074.                               |
| R3 durable server tickets          | T016/T025/T029/T030/T058/T059/T063/T065/T083.                     |
| R4 explicit actor/default denial   | T009/T010/T018/T019/T032/T056/T057/T061-T064/T068.                |
| R5 nominated atomic ceremony       | T041-T055.                                                        |
| R6 platform format 2               | T011-T017/T021/T022/T079/T084.                                    |
| R7 recovery-v2 reconciliation      | T080-T086/T091/T095.                                              |
| R8 isolated development            | T071-T078.                                                        |
| R9 finite input/network/secrets    | T006-T008/T014/T016/T026/T028/T029/T033/T037/T057/T064/T083/T088. |
| R10 bounded retained safe evidence | T015/T020/T025/T049/T050/T054/T059/T082/T087-T089.                |
| R11 accessible minimal journey     | T035/T036/T047/T060/T074/T090.                                    |
| R12 independent delivered evidence | T003-T006/T022/T039/T053/T061/T069/T078/T091-T100.                |

| Principles      | Required implementation/evidence                                                                                                                                                                                                                                                                                        |
| --------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| I               | T036/T074/T090/T095/T099: Köfte brand, empty host, optional modules, no Hoard.                                                                                                                                                                                                                                          |
| II, III         | T003/T009/T010/T019-T021/T061/T099: minimal public actor, versioned contracts, same composed/empty pipeline, VSA/native DI, test-owned source, reviewed dependencies, no MediatR/AutoMapper.                                                                                                                            |
| IV, V           | T011-T017/T022/T024-T030/T043-T052/T079-T088: exact immutable assignments, dedicated files/schemas/history, real providers, owner-local atomicity and truthful filesystem/DB partial effects. Retain reserved/63-byte/collision/UNIQUE/concurrency/path/EF negative suites in T093/T097; do not invent a new allocator. |
| VI              | Configuration/protocol/session/enrollment/default-denial/egress/privacy tasks throughout; T056-T069 prove shared actor not permission and no cross-interface bypass.                                                                                                                                                    |
| VII             | T010/T061/T068 provide future shared actor-v1 only. MCP transport/tools/authorization are 007/013; their runtime tests are inapplicable here, not waived.                                                                                                                                                               |
| VIII            | T087 bounded housekeeping is not a durable job/event system. No resources/provider/job/realtime runtime is added; their contracts/replay/restart tests belong to later specs.                                                                                                                                           |
| IX              | T035/T036/T047/T060/T074/T090: minimal existing design-system journey, all states, actual accessibility/timing; no arbitrary remote frontend loading or full 008 shell.                                                                                                                                                 |
| X, XI           | T002/T004/T005/T011/T017/T075/T079-T098: both real stores, private owned TLS/keys/processes, exact migration/restore, executed public recipes, full local and actual CI checks with limitations.                                                                                                                        |
| XII, governance | T001/T095/T099/T100: exact 005 scope, approved ADR versus dependency adoption, future MVP 001-024 and post-MVP 025-038 preserved, one branch/draft PR, human-only acceptance/merge.                                                                                                                                     |

No new optional module, machine credential issuer, local production password,
account linking, granular team/resource permission engine, general audit
query/export, MCP/job/event/SignalR surface, full UI shell, remote database/HA,
Kubernetes packaging or reference module is added. Their acceptance is not
claimed. Kubernetes/reference-module/full release proofs remain 020/022-024;
this feature requires both-profile local evidence and non-development OIDC
trust tests, not completion of those later specs. Trusted modules are not
sandboxed and a private operator/network configuration is not a hostile-code
isolation guarantee.

## Completion and Human Handoff

Task generation creates **100 unchecked tasks**: setup 6, foundation 16,
US1 18, US2 15, US3 15, US4 8, US5 14 and cross-cutting 8.
Its next separate stage is **analyze**, not implement or feature acceptance.
No extension hooks are configured; `.specify/extensions.yml` is absent.

Implement must record exact commands/results, assessed source/environment,
acceptance mappings, actual durable effects and unavailable/failed limitations
in `implementation.md`. Only evidence closes a task; never alter the
reviewer-owned checklists to get through a gate.

Publish each stage's artifacts and ledger together, pushing every commit
immediately to the original feature branch/draft PR. After implementation,
converge and final evidence-based review remain separate invocations.
Explicit human acceptance of numbered feature 005 is required before readiness
and a human merge request. Checked tasks, passing tests, autopilot, silence
or a prior merge do not accept this feature. Never merge/close/auto-merge
or delete the branch; do not request an intermediate merge.
