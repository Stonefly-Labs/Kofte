# Implementation Plan: 005 - Authentication and Bootstrap Administration

**Rework gate (2026-10-30):** The [approved scope amendment](../../docs/adr/0007-authentication-bootstrap-boundary.md#human-approved-scope-amendment-local-accounts-and-first-login-ownership)
requires ASP.NET Core Identity, production local/OIDC accounts and first-login
ownership. This plan is the historical OIDC/Development design, not an execution
plan for that amendment. The [revised spec](spec.md) now defines the 005/006
delivery boundary; setup-token lifecycle, persistence/transaction integration
and versioned contracts/migration still require replanning and reviewed evidence
before the blocked implementation can resume. Specification rework does not
revise this detailed historical plan or prove store compatibility.

**Branch**: `feature/005-authentication-bootstrap` | **Date**: 2026-10-26 | **Spec**: [spec.md](spec.md)
**Input**: `specs/005-authentication-bootstrap/spec.md`
**Constitution**: Adopted v2.0.1
**Status**: Historical planning-stage design. ADR 0007 subsequently received
explicit human approval, and T001–T095 now implement the delivered interfaces;
see [current evidence](implementation.md#t094-t095-complete-public-catalog-and-delivered-documentation)
and the [runtime guide](quickstart.md). Final gates and feature acceptance are
incomplete. Proposed/unbuilt/next-stage statements below retain the original
planning assessment, not current approval or runtime status.

## Summary

Establish host-owned identity using ASP.NET Core's confidential OIDC
authorization-code flow with PKCE, a durable server-side session, and exact
issuer/subject mapping. The browser and direct API use the same opaque
HttpOnly session cookie; this slice neither issues API tokens nor accepts an
IdP ID/access token as a Köfte API credential. A minimal versioned actor
contract carries trusted identity, not permissions.

An offline, stopped operator ceremony nominates an exact production identity
and delivers expiring one-time authorization through a private file.
Authenticated enrollment atomically consumes that authorization, designates
one initial administrator and records the security event. Development identity
is explicitly enabled, loopback-only, separately namespaced and never eligible.

Preserve the existing two-process local topology through narrowly enumerated
Next.js authentication/identity adapters, not a generic reverse proxy. Production
uses a configured HTTPS public origin; an operator-managed ingress may route
the same paths directly to .NET. Health remains anonymous and unchanged.
Both composed and empty hosts use the same identity pipeline.

Source inspection exposed two necessary compatibility changes, not permission
to relax the old protections: bootstrap health middleware currently rejects
every new route, and registry/recovery verification permits exactly the format-1
table inventory. Replace the blanket route rule with explicit endpoint policies;
add a verified platform format 2 and recovery format 2 rather than silently
accepting extra tables or editing format-1 migration bytes.

Consequential choices are **Proposed
[ADR 0007](../../docs/adr/0007-authentication-bootstrap-boundary.md)**.
Human approval is required before implementation. Checklist generation/review,
tasks, analysis, implementation and explicit numbered-feature acceptance remain
separate stages on this branch/draft PR.

## Technical Context

**Language/Version**: Retain .NET SDK 10.0.402, C# / `net10.0`, runtime 10.0.13;
Node 24.22.0/npm 11.19.1, Next.js 16.4.2, React 19.3.1 and TypeScript 6.0.3.
**Primary Dependencies**: Plan `Microsoft.AspNetCore.Authentication.OpenIdConnect`
10.0.13, framework cookie authentication, antiforgery, Data Protection, native
DI and existing SQLite/Npgsql/OpenTelemetry. The OIDC package and actual
IdentityModel closure require exact archive/provenance/license/advisory review,
central pins, locks and inventory reconciliation before adoption. No restore,
package installation, new version approval, auth SaaS or mediator is performed
by planning.
**Storage**: Host-owned `kofte_system` on PostgreSQL or existing
`platform/platform.db` on SQLite; explicit versioned SQL migrations/history
extend the existing host runner. No identity tables in a module or `public`.
Protected Data Protection keys live in a separate private operator-managed
directory, outside the database recovery-set root.
**Testing**: Existing xUnit projects, real provider fixtures, independently
implemented test-owned OIDC issuer, separately compiled actor consumer,
Vitest/contract tests and Playwright/axe. Extend existing named suites; see
[quickstart](quickstart.md) for exact commands and required observations.
**Target Platform**: Existing macOS arm64 and Ubuntu x64 tool profiles; native
local SQLite needs no IdP/Docker in explicit development mode. Production OIDC
is tested under a non-development host environment and HTTPS fixture on both
existing persistence profiles. This does not add remote production PostgreSQL,
HA, Kubernetes packaging or a general production-deployment profile; those
remain 022. Production OIDC can protect a self-hosted SQLite installation
behind an operator-managed HTTPS ingress.
**Project Type**: Host-local identity slices and infrastructure persistence,
minimal common actor contract, existing Next.js application; no new production
assembly, optional identity module or production test endpoint.
**Performance Goals**: SC-002: twenty simultaneous eligible enrollments and
three fresh host restarts per real provider. SC-005: actionable UI within five
seconds after provider return, measured separately from user/provider login.
**Constraints**: Fixed [version-1 budgets](contracts/identity-v1.md#finite-budgets);
four-second callback processing, one-second local identity operations, finite
idle/absolute session lifetimes; no implicit network/commit retry. Preserve
the inherited startup and maintenance envelopes, with preparation capped at
120 seconds and no new serving migration.
**Scale/Scope**: One installation, one configured production trust at a time,
at most 10,000 users/50,000 active sessions and 256 pending login attempts.
Initial administrator is a seed for 006, never a universal allow bit. Teams,
machine credentials, MCP authorization/server, general audit administration,
full UI shell and deployment/release proof remain 006/007/009/008/022-024.

**Human-directed verification amendment (2026-10-29):** The
[ADR 0007 amendment](../../docs/adr/0007-authentication-bootstrap-boundary.md#human-directed-verification-scope-amendment-installation-session-capacity)
removes the 50,000-session boundary tests and associated corpus cases, not the
runtime cap. Do not run or replace that proof or claim verified scalability
at the boundary. All other validation and feature acceptance gates remain.

**Execution scheduling continuation (2026-10-30):** Run the remaining exact
complete command on a disposable GitHub-hosted Ubuntu runner, independently of
the ordinary ten-job CI matrix, rather than burden the shared Mac. Record the
actual profile and retain the interrupted final Mac attempts as incomplete.
This changes execution placement, not test content, deadlines, provider
coverage, the session-capacity amendment or human feature acceptance. The
earlier passing Mac full gate remains evidence only for its recorded source.

## Constitution Check

**Pre-research assessment:** accepted 001-004 source and the clarified spec
support this stage. No canonical conflict was found. In particular, host-local
SQL is established platform persistence, not an exemption for module EF
contexts/history; module ownership and existing privilege rules remain intact.
**Post-design assessment:** the completed research/model/contracts/guide and
Proposed ADR were rechecked against every principle below. All pass at design
level; this is neither dependency/runtime proof nor human ADR approval.

| Principles | Design evidence / decision | Before research | After design |
| --- | --- | --- | --- |
| I, XII: brand, optional modules, exact MVP/dependencies | Core identity on empty host; Köfte text; FR-001/021/022; accepted 001-004 and explicit future exclusions. | Pass | Pass |
| II, III: versioned contracts, slices, native DI, trusted code | R1-R4, actor-v1 and same registration in both host builders; test source only in test projects; no mediator/mapper, plugin loader or sandbox claim. | Pass | Pass |
| IV, V: owner storage, immutable registry, local transactions | R5-R7; platform format 2 retains module allocator/markers/history, exact inventories and complete sets; one owner-local enrollment transaction, no module reads/ACID. | Pass | Pass |
| VI, VII: identity, default deny, integrations, MCP | Exact OIDC trust, opaque revocable sessions, explicit nomination, anti-CSRF, bounded egress and diagnostics. Actor is not permission; no new MCP path or credential issuance. | Pass, scoped | Pass, scoped |
| VIII: resources/events/jobs/realtime | No runtime added; bounded request housekeeping is identity maintenance, not a job/event delivery guarantee. | Pass, N/A runtimes | Pass, N/A runtimes |
| IX: accessible UI, trusted frontend | R2/R11; minimal identity states reuse the existing system; keyboard/focus/320px/200%/axe/timing evidence; no frontend extension loading. | Pass | Pass |
| X, XI: portability, migration/recovery, evidence | R6-R12; both real stores, protocol fixture, consumer/browser/privacy evidence and retained complete gates; limits and historic failures explicit. | Pass | Pass |
| Governance: traceability, ADR, licensing, human gates | Proposed ADR 0007; R1-R12 trace below; exact dependency adoption and human ADR/checklist/acceptance remain gates; same draft PR #8. | Pass | Pass |

No complexity exception waives a MUST. Neither this plan nor an implementation
test may weaken an issuer check, bootstrap non-repeatability, provider requirement
or existing deadline to get a passing result.

## Implementation delivery sequencing

The frozen full US3 request corpus includes positive Development login.
Consequently, T064 explicitly requires T074's Development endpoint/adapter
delivery. T071/T072 test roots may start after completed US2 (T055), alongside
remaining US3 work, rather than waiting on T070's full-corpus-dependent gate.
All P1, Development, both-provider and final evidence remains mandatory; this
corrects delivery ordering, not scope, security rules or acceptance requirements.

## Project Structure

### Documentation (this feature)

```text
specs/005-authentication-bootstrap/
  spec.md
  checklists/requirements.md
  plan.md
  research.md
  data-model.md
  contracts/identity-v1.md
  contracts/operator-v1.md
  quickstart.md
docs/adr/0007-authentication-bootstrap-boundary.md
```

`checklists/quality.md`, `tasks.md`, analysis and runtime/review/acceptance reports
are later outputs, not generated by this stage.

### Source Code (planned repository paths)

```text
src/Kofte.Platform/Identity/                    # actor-v1 only
src/Kofte.Host/Features/Identity/
  Login/  CurrentUser/  SignOut/  Enrollment/  Development/
  IdentityRegistration.cs                     # both host builders
  IdentityConfiguration.cs                    # strict configuration
src/Kofte.Infrastructure/Identity/             # host-owner persistence/tickets
src/Kofte.Infrastructure/Storage/Registry/     # verified platform format 2
src/Kofte.Infrastructure/Storage/Recovery/     # versioned complete-set verifier
src/Kofte.Host/Features/IdentityMaintenance/    # admitted stopped operator commands
src/Kofte.Host/Features/Bootstrap/              # scoped health guards/OpenAPI
src/Kofte.Host/Features/ModuleComposition/      # shared pipeline + safe IPC projection
src/Kofte.Host/Bootstrap/                      # config/recovery compatibility
apps/web/features/identity/                    # state UI, exact-path server adapter
apps/web/app/auth/                             # explicit login/callback routes
apps/web/app/api/identity/v1/                   # explicit API handlers
packages/bootstrap-contract/                   # regenerate OpenAPI + identity types
tests/unit/Kofte.Host.Tests/Features/Identity/
tests/integration/Kofte.Host.IntegrationTests/Identity/
tests/integration/Kofte.Storage.IntegrationTests/Identity/
tests/integration/Kofte.TestSupport/Identity/   # issuer, crashes, owned fixtures
tests/contract/Kofte.Platform.ContractTests/Identity/
tests/contract/Kofte.Identity.Consumer/         # separate test-owned actor consumer
tests/contract/identity.test.ts
tests/e2e/identity.spec.ts
scripts/verify.py                              # require new cases in existing suites
```

**Structure Decision**: Only genuinely shared actor types enter Platform.
Token claims, credential handling, DbConnections, tickets and bootstrap data
never enter a module contract. Provider SQL/adapters stay in Infrastructure;
use-case handlers and enforcement remain host-local. Tests/migrations belonging
to test modules compile from their own test project, never production slices.

## Design, Migration and Recovery

### Requirement-to-design mapping

| Decision / coverage | Design and owner |
| --- | --- |
| R1: FR-001-003/015/019; US1; SC-001/003 | Built-in confidential OIDC code+PKCE, exact configured trust, no token passthrough; bounded durable attempt consumed with user/session commit. |
| R2: FR-004/008/019/020; US1/3/5.4 | Fixed same-origin browser paths through exact Next adapters, optional direct ingress; no arbitrary proxy, browser token storage or trust in forwarded identity/origin headers. |
| R3: FR-003/004/008/009/014/015; US1/3 | Database-backed cookie ticket store; protected opaque reference, fresh store validation per request, idle/absolute expiry, confirmed revocation and mode/session epochs. |
| R4: FR-005-007/021; US3; SC-003 | Actor-v1; explicit public endpoint inventory, fallback deny, 401/403/503; minimal first-admin designation grants no future action permission. |
| R5: FR-010-012/017; US2; SC-002 | Stopped nominated one-time ceremony; twenty-way compare-and-consume in one owner transaction with unique designation and durable record. No public reopen. |
| R6: FR-003/011/016; US5.1/2; SC-006 | Additive format-2 SQL/history, owner constraints and exact physical validation; shared storage authority, explicit prepare, no migration on serve. |
| R7: FR-009/012/016; US2.5/5.1/2; SC-006 | Version-2 complete recovery sets; mandatory restored-copy quarantine/reconciliation, revoke sessions/pending ceremonies, never infer historical bootstrap safety from a stale backup. |
| R8: FR-013/014; US4; SC-004 | Required development environment + explicit mode/opt-in + all-loopback origins/listeners; fixed selected test identities, no production bootstrap or profile-value merge. |
| R9: FR-015/019; US1.4/5, edge cases | Strict allowlisted configuration, secret references/private key ring, HTTPS origin and provider endpoint/network allowlists, finite admission/time/rate/size budgets. |
| R10: FR-017/018; US5.3; SC-007 | Owner-local durable creation/enrollment records; fixed structured request evidence, truthful durability and safe logs/spans/metrics/IPC; explicit retention/capacity refusal. |
| R11: FR-004/009/013/020; US5.4; SC-005 | Minimal sign-in/current-user/sign-out/enrollment UI, safe local return allowlist, accessible failure/retry, no cached protected identity. |
| R12: FR-022; all stories and SCs | Independent issuer/consumer, real both-provider races/crashes/migrations/recovery, browser parity/a11y/privacy and retained full gates; historical failures never erased. |

### Preparation, runtime and recovery

1. Validate static identity configuration before binding serving endpoints in
   both empty and composed paths. Provider outage never selects development.
   Maintenance commands use explicit operation-specific configuration instead
   of requiring a live IdP. Unknown `KOFTE_*` values still fail; deliberately
   extend the existing configuration allowlists and worker forwarding.
2. While stopped, take the accepted exclusive installation scope. An explicit
   registry upgrade handles table-free format 0 first; `--identity prepare`
   then accepts only verified format 1/2, validates the entire installation,
   appends provider-specific format-2 history and creates identity state/key
   material under its separate private ownership. All schema changes commit
   locally, preserve exact module mappings/data/revisions and are supervised.
3. Ordinary startup accepts only the identity-ready current format; it does
   not create tables, repair migrations or regenerate missing key material.
   Verify exact tables/columns/constraints/history, module inventory and key
   availability. A verified prior format returns `identity_prepare_required`;
   corrupt/unknown formats fail explicitly.
4. OIDC validates evidence before host mapping. User creation, durable
   `user_created`, attempt consumption and new session persist together.
   Concurrent same-identity creation reconciles the database UNIQUE constraint,
   not an email check or unchecked application race. Return no session cookie
   for an uncertain transaction.
5. Enrollment checks current production actor, exact nominee, expiring token,
   installation/generation and unconsumed singleton while locked. Designation,
   consumed ceremony, revision and durable record commit together. A lost
   acknowledgement is uncertain, not success; a fresh authenticated query
   reconciles committed state without issuing another designation.
6. Every protected request checks the server-side session, current mode/epoch,
   expiry, user and designation. Revocation is not cached past a request.
   Provider outage does not invalidate otherwise valid local sessions, but
   identity-store or key verification failure returns unavailable, not a
   cached successful actor. Sign-out success requires known durable revocation.
7. Complete-set restore is a stopped, explicit operation, not direct copying
   into a running installation. A version-2 set includes the full platform
   store/dump, all registered module artifacts and identity security metadata.
   Data Protection/client/certificate secrets are not ordinary backup artifacts;
   reconciliation provisions a new private key ring, advances the session
   epoch and invalidates all restored sessions/attempts/pending ceremonies.
   Keep consumed enrollment closed. An older snapshot cannot establish
   whether enrollment happened after it: no rollback-proof claim, automatic
   reenrollment or exposure until explicit operator reconciliation.

Details, table invariants, recovery compatibility and correction paths are in
[data model](data-model.md) and [operator contract](contracts/operator-v1.md).
No Down migration, old-binary downgrade of format 2, cross-module transaction,
reset-by-deletion or automatic recovery of a lost IdP subject is supported.

## Validation and Operational Handoff

[Quickstart](quickstart.md) maps every US1.1-US5.4 scenario, FR-001-022 and
SC-001-007 to independent required observations and existing named suite
commands. It is a future validation guide, not a claim those commands deliver
identity today. Preserve the existing health schema, aggregate-only disclosure,
strict unknown-route handling, private configuration, module lifecycle/storage
authority and all applicable 001-004 negative tests. Replace only obsolete
blanket route/format expectations with explicit equivalent safeguards.

Planning runs repository/Python/whitespace checks and semantic artifact review.
No local product check can demonstrate nonexistent authentication. Prior head
`dac20c1` has an observed green 11-context CI matrix; the earlier 005
specification-head PostgreSQL failure and 004 failed local attempt remain
historical failures with undiagnosed causes, not waived or diagnosed by this
plan. Record new-head CI separately in the same PR.

The next stage is **checklist**, generated unchecked for reviewer evaluation.
ADR 0007 is Proposed, not human-approved. No tasks, product adoption, checklist
marking, feature acceptance, PR readiness or merge request occurs here.

## Complexity Tracking and Decisions

| Proposed complexity or decision | Need and simpler alternative | ADR / gate |
| --- | --- | --- |
| Durable session/attempt store instead of cookie-only claims | Immediate local revocation, single-use attempt and restart-safe identity; stateless tickets cannot meet next-request sign-out without a revocation store. | ADR 0007 Proposed; exact framework integration tested before adoption. |
| Explicit Next adapters plus optional direct ingress | Preserve the working native local stack without a new compulsory edge service; broad proxying or browser bearer tokens expand trust unnecessarily. | ADR 0007 Proposed; parity/header/redirect tests required. |
| Platform/recovery format 2 | Existing exact format-1 verifiers reject new tables; accepting arbitrary extras or mutating old migration hashes weakens ownership. | ADR 0007 Proposed; both-provider upgrade/restore/old-version refusal required. |
| Offline nominee-bound ceremony | First arrival, email matching and a reusable config token are unsafe; a production password system is unnecessary. | ADR 0007 Proposed; human review before implementation. |
| OIDC NuGet dependency and protected private key ring | Reuse maintained protocol/cookie/CSRF primitives, not custom OIDC/cryptography; no mandatory vendor key service. | Exact closure/license/provenance review and human ADR approval before implementation. |
