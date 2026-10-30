# 0007: Authentication, Sessions and Nominated Bootstrap

**Status:** Accepted for implementation
**Date:** 2026-10-26
**Spec:** [005 - Authentication and bootstrap administration](../../specs/005-authentication-bootstrap/spec.md)

**Current scope authority:** The [2026-10-30 amendment](#human-approved-scope-amendment-local-accounts-and-first-login-ownership)
approves revised account/ownership direction and pauses implementation for
design rework. The original implementation approval and delivered behavior
below do not approve or implement those new semantics.

## Context

Accepted 001-004 provide a module-free host/web, strict routes/configuration,
platform-owned registry, exact storage/recovery inventories and stopped
maintenance authority. They do not implement authentication. Spec 005 requires
provider-neutral production OIDC, stable issuer/subject identity, next-request
session invalidation, nominated non-repeatable enrollment and real protocol/
provider/browser evidence. Authentication is not granular authorization (006),
MCP transport/client authorization (007), general auditing (009) or deployment
packaging (022).

Adding identity tables to the existing format-1 store without a format change
would be rejected by `RegistryMigrationRunner.ReadInstallationAsync` and the
complete-set recovery verifier. Merely adding endpoints would also be rejected
by `HealthEndpoints.MapBootstrapHealth`'s blanket path guard. These are explicit
integration requirements, not reasons to disable the old protections.

## Decision

1. Use maintained ASP.NET Core OIDC code+PKCE and cookie/antiforgery/Data
   Protection primitives. One operator-configured exact HTTPS issuer/client
   trust proves pluggability; no vendor-specific production dependency, local
   production password, account linking or automatic claim-to-admin mapping.
   Persist only verified issuer/subject plus minimal display data; provider
   tokens never become API credentials or enter durable tickets.
2. Use server-side tickets behind a protected HttpOnly opaque-reference cookie.
   Check durable expiry/revocation and current identity mode/epoch on every
   protected request. Browser/direct API share this credential and actor-v1;
   no machine token issuance or bearer acceptance is added. No immediate
   upstream-session revocation or provider-wide sign-out is promised.
3. Preserve one public browser origin. Implement exact Next.js route adapters
   for the enumerated identity/auth paths to the fixed configured host; an
   operator-managed production ingress can instead route them directly.
   Do not accept caller-selected upstreams, forward arbitrary headers/cookies,
   trust client proxy/identity claims or turn the readiness adapter into a
   credential proxy. Public absolute redirects use configured origin/trust.
4. Replace the blanket health path guard only with endpoint-scoped guards,
   explicit anonymous metadata and default-deny protected policies. Keep
   anonymous safe health, environment-restricted OpenAPI, strict methods and
   unknown-route denial. Apply the same registration in composed/empty hosts.
5. Require a stopped installation-authorized nomination of exact production
   issuer/subject and an expiring random one-use secret delivered to a private
   file. The signed-in nominee completes enrollment through a CSRF-protected
   endpoint. Consumption, unique initial designation and immutable security
   record commit atomically in the host store. Configuration replay,
   development identity and administrator unavailability never reopen it.
6. Add provider-specific host SQL format 2 with exact verified tables,
   constraints and append-only migration history; retain format-1 script bytes,
   module allocator/marker rules and module-local EF/history boundaries.
   Preparation is explicit and stopped, never ordinary startup/login.
   Add recovery format 2 rather than loosening the format-1 artifact allowlist.
7. Restore into quarantine under existing exclusive authority. Verify all
   module artifacts and identity security state, revoke restored sessions/
   pending authorization, provision new protected key material and require an
   explicit operator reconciliation before exposure. Preserve consumed
   bootstrap; a stale backup is not proof of historical non-enrollment.
   Lost administrator access requires restoring the same IdP identity or a
   separately reviewed recovery decision, not a second initial administrator.
8. Development requires explicit environment/mode/opt-in and loopback-only
   host/web/public/backend origins, irrespective of the unsafe-exposure flag.
   Its namespace and sessions cannot authenticate in production or consume
   production enrollment. It provides no implicit administrative grant.
9. Adopt the finite budgets, exact endpoint/actor contracts and security-record
   retention/capacity policy in the design artifacts. Require real independent
   OIDC, both-provider races/crashes/recovery, browser parity/a11y/privacy and
   retained complete gates. Review the exact new dependency closure before
   adoption; planning does not approve packages or claim runtime evidence.

The [plan](../../specs/005-authentication-bootstrap/plan.md),
[research](../../specs/005-authentication-bootstrap/research.md),
[identity contract](../../specs/005-authentication-bootstrap/contracts/identity-v1.md)
and [operator contract](../../specs/005-authentication-bootstrap/contracts/operator-v1.md)
are the detailed proposal. No constitutional amendment or scope waiver is
proposed.

## Alternatives

- **Stateless browser tickets / browser-managed bearer tokens:** insufficient
  next-request local revocation or unnecessary browser/token-issuance surface.
- **Accept IdP tokens on the API:** needs a separate resource audience/delegation
  design and is not required by this slice; an ID token is not an API token.
- **Mandatory external BFF/IdP product or new local edge process:** increases
  mandatory services/provider coupling without proving additional 005 value.
- **Hand-written OIDC/JWT validation:** unnecessary cryptographic/protocol risk.
- **First login/email/claim becomes admin:** violates explicit nominated,
  non-repeatable enrollment.
- **Add tables under unchanged format 1 / ignore unknown objects:** breaks
  accepted ownership/physical inventory validation.
- **New identity module database:** adds an artificial module/registry
  dependency for explicitly host-owned identity and complicates local atomicity.
- **Automatic bootstrap reset after restore or IdP loss:** cannot prove
  historical safety and risks minting another administrator.

## Consequences

The host remains small and uses existing services, but gains durable
authentication state and an explicit storage/recovery compatibility boundary.
Old binaries refuse format 2; rollback requires a compatible complete recovery
point, not a Down migration. Native local startup/verification fixtures must
explicitly choose development or configured OIDC rather than get anonymous
access by default. The safe health contract remains unchanged.

Production operators own HTTPS/public origin, provider/network allowlists,
secret references, private certificate-protected key persistence and restore
reconciliation. Local development uses no production credentials. Current
PostgreSQL fixture restrictions remain; remote production profile, HA and
packaging are later deployment work, not promised by this ADR.

Minimal critical security records are retained with installation identity
state; bounded session/request evidence has a finite policy. Capacity and
recording failures are explicit, never silently dropping a required durable
event or pretending an uncertain commit is known. Public APIs never expose
nominee, issuer/subject, secret material or other users.

## Approval

On 2026-10-26, after the explanation of this ADR's OIDC login, server-side
sessions, deliberate one-time first administrator, explicit storage/recovery
upgrades and development-only identity, the human explicitly replied
**"ok, I approve it"**. This approves ADR 0007's design for implementation on
the existing [draft PR #8](https://github.com/Stonefly-Labs/Kofte/pull/8).

This decision does not adopt an unreviewed dependency, evaluate or mark the
reviewer-owned quality checklist, accept numbered feature 005 or authorize
merge. Earlier plan/research/model/contract/guide artifacts and ledger notes
retain their historical Proposed/pending-approval statements; this later
explicit decision is the current authority for ADR status. Those artifacts,
the ledger and evidence hashes remain unchanged. **Checklist remains the next
separate stage** on this same branch/PR; recording approval executes no stage
and does not begin implementation.

## Implementation follow-up: owner-store deadlines

The approved one-second local budget cannot use the existing floored
whole-second timeout helper. Actual SQLite contention also disproved rounding
up with cancellation: Microsoft.Data.Sqlite's synchronous retry loop does not
observe cancellation during execution. Identity therefore uses its
[documented native-handle interoperability](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/interop)
through the already locked SQLitePCLRaw package, not copied provider code,
a new dependency or a changed journal mode.

The narrow owner-local executor retains path/sidecar/header checks, enables
and verifies foreign keys, binds parameters and caps returned rows at 128.
Native busy waiting is disabled; retries use the original remaining budget
only at SQLite's documented known-not-committed boundaries. A VM progress
callback interrupts long SQL. PostgreSQL retains maintained cancellable
Npgsql execution and closes unpooled failed transactions instead of awaiting
an uncancellable rollback round trip. Fifty milliseconds is reserved
**inside**, never added to, the enclosing budget for cleanup.

Metadata/admission serializes writes under SQLite immediate transactions or
the PostgreSQL metadata row lock. The borrowed normal host lease remains
required; PostgreSQL checks its backend/shared lock from the independent
operation connection, avoiding concurrent commands on the shared lease
session. Failure/uncertainty is not success and an uncertain commit is never
automatically retried. Legacy migration paths, public contracts, approved
limits and the original human approval remain unchanged. This implementation
detail does not constitute feature acceptance or end-to-end login evidence.

## Implementation follow-up: fixed HTTPS ingress and private diagnostics

The approved HTTPS ingress forwards to the fixed loopback HTTP backend without
trusting forwarded scheme/host headers. Framework antiforgery's
`CookieSecurePolicy.Always` rejects that backend before issuing a token.
The shared boundary instead uses the supported
[`CookieBuilder.Build`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.http.cookiebuilder.build?view=aspnetcore-10.0)
extension point to set the actual cookie's Secure flag from validated identity
mode. OIDC retains a Secure, HttpOnly, host-only `__Host-` cookie with path `/`;
no request header chooses that flag or changes transport metadata. A real
Kestrel/token-pair regression exercises the fixed HTTP hop. This is a shared
component, not a delivered production login or ingress deployment.

Identity diagnostics use fixed fields/codes, generated correlation independent
of caller trace identity, and at most 1 KiB per event. Metrics have only bounded
operation/mode/provider/outcome labels; sanitized spans use that generated
correlation without caller baggage. Private composition transport admits a
strict identity-or-composition frame and serializes whole concurrent writes;
it adds no identity lifecycle contract to Platform or module API.

Stopped supervisors reproject only validated bounded records after actual
worker exit and cleanup. Missing, malformed or lost acknowledgement cannot be
success and is conservatively uncertain. Key-file effects and database commits
remain separate; real worker-kill tests cover both commit boundaries.
The pre-open SQLite header-sharing retry likewise has no database effects and
uses only the existing deadline. No uncertain transaction is replayed, no file
guard relaxed and no automatic journal removal added.

## Implementation follow-up: explicit certificate overlap

The approved retained-key/certificate rotation requirement uses the maintained
[Data Protection certificate overlap API](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview#unprotect-keys-with-any-certificate-unprotectkeyswithanycertificate).
Version-1 operator configuration adds only optional `keyDecryptionCertificates`:
one to seven strict private certificate/key references, alongside the unchanged
current encryption pair. Explicit references avoid reinterpreting a previously
accepted PEM certificate chain or silently selecting a different encryptor.
All pairs require matching RSA keys of at least 2048 bits and existing bounded
owner-only file/path checks. Duplicate certificates fail closed. Loaded
certificates are disposed together, including partial-load failure.

Only the current certificate encrypts newly generated keys; the current and
retained certificates decrypt existing keys. Secret/certificate rotation does
not change the provider-trust digest, mapping, consumed designation or session
epoch. Every retained key must remain decryptable: expiry alone does not permit
removing a decryptor, and no serve-time replacement, automatic retirement,
rewrapping, new command, cloud dependency or provider-trust relaxation is added.
Explicit session revocation remains the invalidation mechanism. This implements
005's existing rotation requirement, not human acceptance or production readiness.

## Human-directed verification scope amendment: installation session capacity

On 2026-10-29 the human explicitly rejected the 50,000-session boundary test:
**"We will NOT test it."** Remove that installation-wide boundary proof from
both providers' storage and HTTP contract tests, its exclusive population/
snapshot helpers and the C#/TypeScript numeric corpora. It is not a skipped,
deferred or passing test, and no replacement boundary test is required.

The runtime cap remains 50,000 active sessions; this decision changes evidence
scope, not admission policy. No claim of verified behavior or scalability at
that boundary follows. Preserve unrelated per-user, expiry, revocation,
admission, retention, privacy and recovery checks. Earlier executed results
remain historical, including failures. This explicitly supersedes the
all-numeric-boundaries obligation for this one case in the plan, guide and
T006/T088; it does not accept feature 005 or alter other delivery gates.

## Implementation follow-up: pinned Chromium stream-completion repair

On 2026-10-29 actual Linux capture records `net::ERR_ABORTED` without a client
AbortSignal event, even though the UI finishes reading its no-store JSON.
[Playwright issue 42742](https://github.com/microsoft/playwright/issues/42742)
and the merged
[Chromium repair](https://chromium-review.googlesource.com/c/chromium/src/+/8508456)
identify the matching upstream defect: the loader reported cancellation before
recording a completed streaming body, losing its DevTools response resource.
The repair records terminal state before notifying the stream client.

The latest stable Playwright remains 1.64.1 and reproduces this failure. Use
the exact test-only `1.65.0-alpha-2026-10-29` release, not a floating canary tag,
to obtain bundled Chromium 157.0.8103.15 (revision 1252). Its actual
`Browser.getVersion` source revision is
`1aa07b638799e59656559ae6c6649d4b197f555e`; the official Chromium source
contains the repaired ordering. This is a documented exception to the
constitution's stable-version preference, not permission to weaken capture
or change production dependencies. It carries prerelease compatibility risk.
Reassess the exact pin when a supported stable release contains this repair.

Root overrides align Next's optional test peer and axe's test-core peer with
the same exact reviewed release. Leaving the older core alongside the new
runner produced incompatible Page types; the aligned graph type-checks
without casts. Archive integrity, licenses, notices, graph reconciliation
and advisory checks still apply. The checker accepts exact prerelease
versions but continues rejecting ranges and moving tags.

Production bounded readers, no-store responses, abort handling, deadlines,
TLS and ownership/privacy checks remain unchanged. Actual Linux native and
public browser execution must validate this candidate; an upstream report
or a local pass alone is not feature completion.

## Human-approved scope amendment: local accounts and first-login ownership

On 2026-10-30, after the proposed ASP.NET Core Identity/local-account
rearchitecture, the human explicitly directed:

> ok, yeah, I approve. Also, I wanna do first login ownership. e.g. first person to login (either local or oidc) becomes super user, then can invite/add/onboard new users, assign rbac and such

This approves the following product/design direction, not feature 005 acceptance,
a new passing review, implementation completion or PR readiness:

1. Use ASP.NET Core Identity for standard account management, with maintained
   framework handlers for external protocols. Do not invent a password/account
   framework or require an external identity-server product. Keep Next.js UI.
2. Support production local accounts and OIDC with one stable internal user
   identity and shared application session/actor/authorization boundaries.
   The design must support local-only, OIDC-only and explicit coexistence,
   named external connections and optional explicitly associated login methods.
   Never associate accounts by matching email/display text. Development remains
   a separate limited profile, not production local accounts.
3. Replace exact-person nomination as the target bootstrap experience with
   **first successful local or OIDC login claims initial ownership**. All
   supported methods compete for one installation-wide atomic transition, not
   one owner per provider. Development is ineligible. Subsequent logins cannot
   become owners through this ceremony. Restarts, provider changes, owner loss
   and restore must not reopen consumed ownership. Existing consumed bootstrap
   and its user ID must be preserved through migration, not claimed again.
4. The initial Owner/superuser can invite/add/onboard users and assign RBAC.
   These capabilities must use central registered authorization and cannot
   become an unguarded per-endpoint bypass or external-claim-to-admin mapping.
   The approved delivery boundary below assigns account/ownership mechanics to
   005 and owner-led administration/teams/RBAC to existing 006. Exact API
   contracts still require replanning; do not silently implement all of 006 in 005.
5. Keep an extension seam for future SAML using a vetted handler or federation
   bridge, not a bespoke parser. SAML delivery is not an MVP dependency or
   authorized implementation task.
6. Evaluate standard EF Core Identity stores first, against actual platform
   ownership, migrations, transaction/evidence and finite deadline guarantees.
   Narrow custom Identity persistence adapters remain an alternative if needed;
   this is not approval to reimplement Identity managers or credential policy.
   Store selection, exact package adoption and compatibility proof remain open.

### Approved initial-claim safeguard

The initial approval checkpoint left first-claim admission unanswered because
the human was unavailable. On 2026-10-30, the human explicitly resolved it:

> one time setup token is cool

Use a **one-time installation setup token** to authorize a setup session without
nominating a specific local/OIDC person. The first successful local or OIDC
login in admitted setup claims initial ownership. The token is setup admission,
not an authentication credential or an independent administrative grant.
Ownership and token consumption must commit atomically; consumed ownership
cannot be claimed again by replaying or replacing the token.

This resolves the admission choice, not the remaining implementation design.
Token generation, private delivery, expiry, setup-session binding and
concurrent/failure behavior still need explicit design and validation.
**Unrestricted public first-login ownership is not approved.** The constitution's
explicit, safe, non-repeatable bootstrap requirement remains binding; no
constitutional waiver or weakening is proposed.

Reconcile initial claim, local-account creation, invitation/reset delivery and
owner permissions before exposing the new flow. Never turn IdP outage into an
automatic local credential, fallback owner or bootstrap reset.

### Approved 005/006 boundary and specification rework

After the assistant proposed revising 005's account/authentication/initial-Owner
scope while leaving owner-led invitations/user administration/teams/RBAC in
existing 006, and clarified that 006 already has an inventory entry, the human
authorized the work:

> ok yeah, let's do this pls

The [revised specification](../../specs/005-authentication-bootstrap/spec.md)
records that boundary: 005 owns canonical accounts, local credentials, external
associations, self-service credential/reset/association mechanics, shared sessions,
setup-token admission, immutable initial ownership and the versioned integration
boundary. Existing 006 owns trusted onboarding/invitation/user-administration
workflows, account disablement decisions and registered team/role/resource
authorization. No 006 artifact or implementation stage is created here.

Until 006 delivers those workflows, 005 permits protected initial setup and
existing-user authentication/self-service, not open registration or a temporary
Owner bypass. A valid unknown OIDC identity is not automatically admitted after
setup; future authorized onboarding uses 005's account boundary. The complete
Owner-led onboarding/RBAC journey is a 006 deliverable, not a hidden prerequisite
requiring both numbered features to be implemented at once.

The specification now has six stories, 34 scenarios, 29 requirements and nine
outcomes, with independent future-validation mapping. Its built-in author
checklist is re-evaluated; the prior reviewer-owned quality checklist, detailed
design/contracts, analysis, task entries and runtime reports remain historical.
This records specification rework only, not a new technical plan or runtime result.

### Remaining rework and durable pause

Implementation remains paused on `feature/005-authentication-bootstrap` and
draft PR #8. The driver records **005 / implement / blocked** solely as a
scheduling guard: no implementation skill, product command or later stage is
executed by recording this pause. Do not reset prior ledger history or fake
completed specify/clarify/plan/checklist/analyze entries.

The setup-token choice and top-level 005/006 delivery boundary are resolved, and
the specification is rewritten. Its separate clarification assessment on
2026-10-30 finds all ten categories Clear and no critical ambiguity worth a
formal question. Zero questions are asked/answered; the spec and built-in author
checklist remain byte-identical, with 16/16 criteria before and after.
See the [coverage evidence](../development/spec-workflow.md#reworked-005-clarification-assessment---2026-10-30).
This assesses written requirements, not a new runtime or human approval result.

Before implementation resumes, revise research/plan, account/session/actor and
operator contracts, data/migration/recovery design and their validation mapping.
Exact policies, token delivery/binding, persistence choice and integration
contracts remain planning work, not unanswered product decisions to fabricate.
Preserve stable user IDs, exact external mappings, module data/assignments,
consumed ownership and honest partial effects. A versioned migration and
explicit old-session invalidation must replace reinterpretation of mode-bound
rows/tickets; do not edit old migration bytes or loosen inventory checks.
Production local login must retain production TLS, cookies and key protection.

Re-review the changed requirements-quality criteria, reconcile dependency-ordered
tasks and perform cross-artifact analysis before code work. The old checked
checklists, completed tasks and green reports apply only to their recorded
OIDC/Development scope; they do not prove the amended design. Do not uncheck or
tick historical evidence to manufacture a new gate. Preserve review F1/F2 and
unchecked T101-T103 until the rework explicitly reconciles and verifies them.
Subsequent implementation/convergence/review and explicit human acceptance
remain separate; no merge or new numbered feature is authorized.
