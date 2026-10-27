# 0007: Authentication, Sessions and Nominated Bootstrap

**Status:** Accepted for implementation
**Date:** 2026-10-26
**Spec:** [005 - Authentication and bootstrap administration](../../specs/005-authentication-bootstrap/spec.md)

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
