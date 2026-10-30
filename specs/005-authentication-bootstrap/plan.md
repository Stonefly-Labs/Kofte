# Implementation Plan: 005 - Accounts, Authentication and Initial Ownership

**Branch:** `feature/005-authentication-bootstrap` | **Revised:** 2026-10-30
**Input:** `specs/005-authentication-bootstrap/spec.md`
**Constitution:** 2.0.1
**Status:** Revised design proposal; Phase 0/1 complete, implementation blocked.

The human approved Identity/local+OIDC/first-login ownership and the 005/006
boundary, not these exact store/policy/contracts or feature acceptance.
The [ADR amendment](../../docs/adr/0007-authentication-bootstrap-boundary.md#revised-technical-design-proposal---2026-10-30)
records the proposed choices. The
[original plan](https://github.com/Stonefly-Labs/Kofte/blob/f10351a599191a5931b05a719cd8d291eba839fb/specs/005-authentication-bootstrap/plan.md)
and old R1-R12 retain their historical scope; checked tasks/runtime reports do
not certify this design. Do not rewind the driver: **005 / implement / blocked**
remains its scheduling guard until design/quality/tasks/analysis are reconciled.

## Summary

Use ASP.NET Core Identity's account managers, validators, normalizer, password
hasher, lockout and reset provider with native OIDC handlers, one canonical
internal user and the existing durable opaque-cookie session boundary.
Production local-only, OIDC-only and coexistence have identical transport/key
protection. A private one-time setup token admits a browser-bound ceremony;
the first eligible authenticated local/OIDC commit creates one initial Owner.

Evaluate standard EF stores first, then select narrow store adapters over the
existing bounded owner executor: EF can customize its model/share a transaction,
but its managed SQLite execution does not supply the retained native deadline/
cleanup path. Do not rebuild account managers. Append platform format 3 /
identity schema 2 migration and recovery-v3; preserve old migration bytes, exact
inventories, users/external identities/consumed ownership and module data.
Existing 006 still owns invitations/user administration/teams/RBAC.

## Technical Context

**Language/Version:** Existing pins: .NET SDK 10.0.402/runtime 10.0.13; Node
24.22.0/npm 11.19.1; Next 16.4.2, React 19.3.1, TypeScript 6.0.3.
Verified in `global.json`, `Directory.Packages.props` and npm manifests; no changes.

**Primary Dependencies:** Host's `Microsoft.AspNetCore.App` already contains
Identity Core/Stores and ASP.NET Identity assemblies. Use `AddIdentityCore`,
`AddSignInManager`, custom `IUserStore` and the Data Protection reset provider;
no EF Identity package, Razor scaffold, identity server, SMTP or SAML package.
Retain OIDC 10.0.13, existing EF/Npgsql/SQLite and current web/test dependencies.
Exact locked assembly behavior, provenance/licenses/advisories/locks/notices
remain mandatory implementation adoption checks, not a new package approval.

**Storage:** Host `kofte_system` / existing `platform/platform.db`. One owner
transaction/executor; no optional identity module, cross-module access or
second account database. Platform format 3 has three registry plus fourteen
identity tables. Preserve format-1/2 migration bytes and physical fingerprints.

**Testing:** Existing repository/Python checks for this documentation stage.
Future existing quality/unit/integration/contract/recovery/browser/privacy/
startup/failure-propagation verifier suites; both real stores and an isolated
HTTPS OIDC issuer; no fake-principal-only acceptance. See [quickstart](quickstart.md).

**Target Platform:** Existing local/self-hosted host and Next.js; native SQLite,
owned disposable PostgreSQL verification, operator-managed HTTPS ingress.
No new HA/remote-database/deployment profile; 022-024 retain deployment/release.

**Project Type:** Core host vertical slices, minimal versioned Platform
contracts, provider persistence in Infrastructure and existing web adapters.
In-process modules are trusted, not sandboxed.

**Performance Goals:** Preserve one-second local operation including manager
work/commit/cleanup; four-second OIDC completion, 4.5-second adapter, actionable
UI within five seconds after final input/return. Preserve existing startup,
acquisition and 120-second preparation bounds; human/IdP time is separate.

**Constraints:** No cookie before known commit; fresh durable validation on
every protected request; no periodic positive authentication cache; no automatic
retry on uncertain commit; no open registration/email auto-link/Owner bypass.
The 50,000-session boundary must not be tested, substituted or claimed proved.
No heavy local product/Docker suite on the shared Mac.

**Scale/Scope:** One organization/installation, later multiple teams; 10,000
users, eight sessions/user, unchanged 50,000 runtime session cap (unverified).
One local and up to eight named OIDC connections; separate limited Development.
Six stories/34 scenarios, FR-001-029, SC-001-009; no 006 workflow/RBAC or SAML.

## Constitution Check

**Gate:** Baseline assessed before design; rechecked against all Phase 0/1
artifacts afterward. Passes below are **design-level**, not runtime qualification,
human approval, a quality-checklist certification or a waiver of later gates.

| Principles | Design evidence / decision | Before research | After design |
| --- | --- | --- | --- |
| I, XII: brand, optional modules, inventory/MVP/dependencies | Canonical docs/revised spec; A3/A8/A11; core works without modules, 005/006 boundary and 001-024 scope retained. | Pass | Pass |
| II, III: modularity, versioned contracts, VSA/native DI, trusted code | A1/A2/A8/A11; existing Host/Infrastructure/Platform projects, actor-v2, dedicated tests; no MediatR/AutoMapper or new speculative assembly. | Pass | Pass |
| IV, V: registry/schema/file ownership and honest ACID | A1/A9, data model; unchanged allocator/63-byte/schema-local module histories/files; format-3 owner-local commit, no cross-module join/connection/ACID. | Pass | Pass |
| VI, VII: OIDC/bootstrap, default denial, shared policy/tools/audit | A2/A4-A8/A10; protected first-login setup, production-local parity, fresh sessions, deny-default account port. MCP/jobs are future consumers, no runtime surface now. | Pass | Pass |
| VIII: durable work/realtime | Local account/attempt/security state is transactional, not a job/event-delivery engine. Jobs/outbox/realtime/resources are N/A to this slice; later specs retain guarantees. | Pass; justified N/A | Pass; justified N/A |
| IX: accessible UI/trusted extensions | A11 and browser matrix; existing UI, 320px/200% text, keyboard/announcements/a11y/timing; no remote frontend extension. | Pass | Pass |
| X, XI: portability/operations/real evidence | A9-A12; both stores, explicit upgrade/quarantine/keys, vendor-free local, real protocol/browser/privacy matrix. Kubernetes/reference-module/release proof stays 022-024. | Pass | Pass |
| Governance: traceability/ADRs/licenses/human review/one branch | Approved scope separated from proposed A1-A12; same branch/draft PR, unchanged ledger history/checklists/tasks/runtime evidence; review/adoption/acceptance remain human-owned. | Pass | Pass |

No unresolved product ambiguity or constitutional conflict was found. Failure
of the exact-runtime compatibility/deadline proof blocks implementation adoption;
it never permits weakened gates, a custom account framework or self-approval.

## Project Structure

### Documentation (this feature)

```text
specs/005-authentication-bootstrap/
  spec.md                         # unchanged revised requirements
  plan.md
  research.md                     # A1-A12, not historical R1-R12
  data-model.md
  quickstart.md                   # future validation, not delivered new commands
  contracts/identity-v1.md         # preserved delivered old contract
  contracts/operator-v1.md         # preserved delivered old contract
  contracts/identity-v2.md
  contracts/operator-v2.md
  contracts/account-admission-v1.md
  tasks.md                        # historical T001-T103; later reconciliation
```

### Source Code (repository root; proposed changes, not files created by plan)

```text
src/Kofte.Host/Features/Identity/
  Accounts/                       # IdentityAccount, scoped store adapter/coordinator
  Credentials/                    # local login/change/reset/fresh proof slices
  Setup/                          # admission and atomic claim
  Associations/                   # explicit self-scoped add/remove
  Login/                          # named OIDC handlers, existing committed-ticket bridge
  CurrentUser/                    # actor/current identity v2
src/Kofte.Infrastructure/Identity/ # records/SQL, setup/proof/reset/account operations/recovery
src/Kofte.Infrastructure/Storage/Registry/Migrations/{Sqlite,Postgres}/003_accounts.sql
src/Kofte.Platform/Identity/       # minimal actor-v2/admission-v1 contracts
apps/web/app/identity/             # extend existing UI/states
apps/web/app/                     # individually enumerated v2/auth route adapters
packages/bootstrap-contract/       # generated identity-v2 plus unchanged health
tests/unit/Kofte.Host.Tests/
tests/unit/Kofte.Platform.Tests/
tests/integration/Kofte.Storage.IntegrationTests/Identity/
tests/integration/Kofte.Host.IntegrationTests/Identity/
tests/integration/Kofte.TestSupport/Identity/
tests/contract/Kofte.Platform.ContractTests/Identity/
tests/contract/Kofte.Identity.Consumer/
tests/e2e/                        # existing browser/fixture ownership
scripts/verify.py                 # reconcile exact named v2 inventories later
```

**Structure decision:** Keep ASP.NET types/DI in the Web Host so Infrastructure
does not acquire an ASP.NET dependency. The adapter implements framework storage,
not another manager. No test source in production or linked from production.
Update directly dependent host/composition, CLI supervisor, contract generation,
web allowlists and verifier consumers in implementation; do not broaden them.

## Design, Migration and Recovery

### Account/store/session coordination

The scoped adapter requires a live coordinator-owned executor/budget; a store
call without that scope fails explicitly. Setters update the account object;
Create/Update/login/lockout writes use that same transaction, concurrency checks
and no commit. Honor cancellation even where a manager supplies no caller token
by checking the enclosing budget. Never capture scoped managers in singletons.
Record failed store writes in the owner scope; commit must reject that scope
even if a framework rehash/check path ignores a returned update failure.

The coordinator validates operation-specific authority and fresh current state,
runs manager calls, stores mandatory state/evidence, commits once, then attaches
the committed session through the existing bridge. A check-only password denial
may commit failed-count/lockout evidence **only**; a concurrency/update failure
or later rejected setup/association rolls back all tentative effects. Do not
commit arbitrary partial manager mutations because an `IdentityResult` failed.
Framework password rehash can write and rotate stamps; detect that change,
advance account security version and invalidate old sessions. Successful fresh
reauthentication rotates the current session and binds its single-use proof to
the replacement, so a rehash cannot leave a proof attached to a revoked cookie.

All protected operations recheck durable session, account enabled/context,
security version/stamp, global epoch, applicable connection trust, expiry and
Owner provenance. Lockout denies new local password proof, not independently
valid OIDC sessions. Trust reconciliation conservatively revokes all sessions.
Password/reset/association changes revoke all prior sessions; no auto-login.
These rules apply equally through direct host, Next adapter and trusted actors.

### Requirement-to-design ownership

| Requirements | Decision / owner / contract |
| --- | --- |
| FR-001/002/023 | A2/A4; Host local/OIDC handlers; identity-v2 and operator-v2 named connections, production transport parity. |
| FR-003/004 | A3/A4/A8; canonical users/external ownership and current identity/actor-v2. |
| FR-005/006/007 | A8; exact anonymous inventory, safe 401/403, actor-v2 and default-deny account policy; no 006 bypass. |
| FR-008/009/029 | A5/A7/A10; browser binding, antiforgery, opaque ticket bridge, fresh durable security checks and retained lifetimes. |
| FR-010/011/012/028 | A5/A9; stopped private setup delivery and single host-owner claim/consumption/session/evidence transaction. |
| FR-013/014 | A3/A8/A11; separate Development context/connection/cookie, strict environment/loopback checks, no production authority. |
| FR-015/019 | A1/A4/A10; strict configuration, finite work, truthful outcomes and forbidden DNS/connected-peer rejection. |
| FR-016 | A9; format-3 explicit stopped migration, exact inventories/history, recovery-v3 and preserved legacy ownership/modules. |
| FR-017/018 | A6/A9/A10; transactional credential/association/Owner evidence, bounded safe diagnostics/canary captures. |
| FR-020 | A11; minimal accessible setup/credential/reset/association/login/error states and safe returns. |
| FR-021/027 | A8; versioned actor/current-user and account-admission-v1, future 006 policy, no open post-setup admission. |
| FR-022 | A11/A12; independent both-provider/protocol/browser/consumer/privacy and retained regressions. |
| FR-024/025 | A2/A3/A6; framework local policy/lockout/change/reset, private one-use delivery and next-request invalidation. |
| FR-026 | A4/A7; explicit dual proof, unique ownership and final-usable-method protection. |

### Explicit migration and recovery

Follow [data model](data-model.md#migration-and-compatibility) and
[operator-v2](contracts/operator-v2.md), not automatic schema creation.
Stopped preparation validates format-1/2 authority/catalog/history and legacy
issuer mapping, applies historical migrations where needed and then migration 3,
updates exact history/version/fingerprints and commits preservation evidence.
Current prepare is idempotent and serving verifies format 3 read-only.
Format-1 upgrade has no historical user to invent; format-2 import retains all
production/development users but adds no password or cross-context association.

Consumed legacy ownership stays consumed with its exact user ID/time/generation
and original event. Unconsumed nominee/token authority is erased; later setup
requires fresh private admission. Old tickets/attempts are deleted, not relabeled
as v2. Keys remain separately protected/reconciled; no filesystem/database ACID.
Recovery-v3 requires the exact complete set plus actual security snapshot
verification. Prior v1/v2 sets retain their readers but require explicit upgrade
and history reconciliation before serve. Unknown history stays locked.
No Down migration, owner reassignment, old-instance fencing or rollback-proof
OS-copy guarantee is introduced. Broader workflows/recovery administration is 006.

## Validation and Operational Handoff

[Quickstart](quickstart.md#acceptance-matrix) maps every one of 34 scenarios,
29 FRs and nine SCs to actual future commands/observations. Its fourteen v2 public
recipes and `all` must be implemented and independently inventoried, not silently
substituted by the old catalog. First prove exact framework store/transaction/
deadline compatibility on both providers; then qualify all current source.

Retain F1/T101 forbidden destination correction, F2/T102 pre-key-damage SQLite
503 diagnosis and T103 changed-source full qualification. Preserve failed run
`41098819181` and historical green scope. No heavy product suite runs as part of
this documentation stage; hosted Linux evidence never means final macOS proof.
The prohibited installation session-capacity boundary stays excluded.

Run repository/Python/whitespace checks for this stage; do not generate tasks,
retick reviewer checklists, change product packages/code or invoke implement.
Next is human design/changed quality review, then explicit task reconciliation
and cross-artifact analysis on this branch/draft PR. Convergence/final review/
explicit human acceptance remain later. No intermediate merge.

## Complexity Tracking and Decisions

| Proposed complexity or unresolved choice | Need and simpler alternative | ADR / human decision |
| --- | --- | --- |
| Five-interface native Identity store adapter | Preserve the existing one-second native executor and one owner commit; standard EF stores were evaluated and can customize/enlist, but do not supply this execution path. No custom manager. | ADR 0007 proposed technical amendment; exact-runtime proof before adoption. |
| Actor/HTTP v2 and explicit connection bindings | Closed v1 mode cannot represent provider-independent local/OIDC users; no fake OIDC enum or implicit association. | Approved versioning requirement; proposed exact contracts. |
| Format 3 / recovery-v3 | Exact delivered format-2 inventory/history and legacy consumed authority are compatibility promises; not disposable tables. | Approved preservation requirement; proposed migration design. |
| Setup/proof/reset/idempotency records | Explicit one-use/concurrency/replay/uncertainty needs durable state, not independent cookie/token grants. | Approved setup/account boundary; proposed lifetimes/transport. |

No unresolved planning choice remains. Proposed decisions still require review;
runtime compatibility, dependency approval and evidence remain unperformed gates.
