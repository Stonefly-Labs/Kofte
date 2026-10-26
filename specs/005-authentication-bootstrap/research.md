# Phase 0 Research: 005 Authentication and Bootstrap

**Date:** 2026-10-26
**Result:** Planning choices resolved; proposal, not adoption or runtime evidence.
**Inputs:** clarified [spec](spec.md), constitution 2.0.1, accepted 001-004,
current host/storage/web source and official references below.

Microsoft Learn and Context7 MCP are not connected in this session; the actual
available server inventory is GitHub/IDE. Bounded external research used official
web documentation as the fallback. One research agent investigated framework/
protocol primitives; the parent inspected repository topology, migration,
recovery, supervision and test contracts. No private source was sent to an
external service, product package installed or registered workflow invoked.
Documentation supports the primitives, not an assertion that this exact
application design is a prebuilt Microsoft/Next.js recipe.

## R1 - Maintained confidential code flow, with explicit trust

**Decision:** Use `Microsoft.AspNetCore.Authentication.OpenIdConnect` in the
existing .NET 10 line with confidential authorization code + PKCE, `openid`
and optional display-only `profile`, query response mode and RS256 ID tokens.
Require exact configured issuer/client audience, valid signature/key/expiry,
nonce, state, initiating-browser correlation and single-use local attempt.
Disable inbound claim remapping, token saving, UserInfo calls and automatic
PAR. The provider must support this documented baseline; supporting every
OIDC variation is not claimed. No ID/access token is accepted at the Köfte API.

**Rationale:** [Microsoft's OIDC guidance][oidc] and [options][options] support
code+PKCE and configurable cookie sign-in. [OIDC Core][core] defines exact
issuer/audience, token and response validation. Preserve these validations
explicitly and test them; do not rely on an assumption that every library
default alone fulfills the application contract. The durable attempt adds a
host-side one-use constraint across concurrent callbacks/restart.

**Alternatives:** Hand-written OIDC/JWT validation is unnecessary risk;
Entra-specific integration defeats provider neutrality. Default
`UseIfAvailable` [PAR behavior][par] is standards-compatible but adds another
metadata-selected request to bound. Explicitly disable PAR for this baseline;
a provider requiring PAR is rejected as unsupported, not silently downgraded.
No mTLS requirement is introduced.

## R2 - Same origin without a new compulsory local service

**Decision:** Exact Next.js route handlers forward only enumerated auth/API
paths to the fixed configured loopback .NET backend. No catch-all URL/header
proxy. An operator-managed HTTPS ingress may route the same paths directly;
both paths obey the same contract. Absolute callback/return URLs use the
configured public origin, not untrusted Host/forwarding headers.

**Rationale:** Existing `apps/web/app/backend/health/ready/route.ts` already
uses a fixed server-only destination and finite deadline. Extend the pattern
in a separate identity adapter with deliberate cookie/redirect handling,
without turning anonymous readiness into an authenticated proxy.
[Microsoft BFF guidance][bff] supports keeping credentials server-side, and
[Next rewrites][rewrites] establish same-origin upstream routing as a supported
primitive. Choosing exact handlers rather than broad rewrites is this
repository's stricter input/header/error contract, not a claim from those docs.

**Alternatives:** A mandatory YARP/edge process changes the accepted native
two-process local setup unnecessarily. Separate browser origins with CORS
cookies add avoidable cross-origin trust. Browser tokens/localStorage expose
credentials to JS and require a new API issuance/delegation design.

## R3 - Durable server-side tickets, not cookie-only claims

**Decision:** Implement framework [ITicketStore][tickets] against the host
identity store. The protected cookie holds an opaque session reference; the
stored ticket is a minimal locally constructed principal, never raw provider
claims/tokens. Every request validates durable session/user/mode/epoch, idle
and absolute expiry, then reads the current initial designation.
Sign-out removes/revokes the durable ticket. Renewal updates only admitted
idle continuity, never extends the absolute bound.
The login coordinator commits mapping/attempt/session before cookie publication;
the ticket-store bridge attaches that already committed, request-bound session
instead of creating a second independent session transaction. See the data
model's transaction invariants; framework callback/store integration needs a
focused contract regression, not a split user/event commit in an OIDC event.

**Rationale:** Cookie-only tickets cannot meet next-request revocation without
adding a revocation store anyway. [Cookie guidance][cookie] and
[Data Protection settings][dp-default] distinguish ticket lifetime, key
retention and rotation. Ordinary key rotation does **not** revoke tickets
while retired keys remain available. Explicit session revocation/epoch
advancement is the security mechanism.

**Alternatives:** A bespoke opaque-cookie authentication handler duplicates
maintained framework behavior. No refresh/access token vault, machine token
service or broad immediate IdP-revocation promise is needed. IdP logout,
back-channel logout and upstream revocation are separate features.

## R4 - Explicit actor and endpoint policy, not implied authorization

**Decision:** Publish only actor-v1 in Platform. Default deny protects current
identity, enrollment and future operations; explicitly list public health,
browser-context and login protocol endpoints. APIs always use 401/403, never
OIDC challenge redirects. Configure explicit cookie redirect-event behavior
for API paths and test it instead of depending on endpoint-shape inference.
CSRF token-pair validation and exact origin checks cover browser mutations;
callback is a separately state/nonce-bound protocol endpoint.

**Rationale:** [.NET 10 API behavior][api-auth] removes cookie login redirects
for recognized APIs, but detection alone is not a stable application contract.
The existing blanket route guard must become health-scoped and endpoint-aware.
[Antiforgery guidance][csrf] explains why HttpOnly/SameSite is not sufficient
authorization for state-changing cookie requests.
Verified [IAntiforgery][antiforgery-api] supports explicit `GetAndStoreTokens`
and `ValidateRequestAsync` outside form binding; configure the header/cookie
pair and invoke validation for JSON mutations. Verified
[cookie events][cookie-events] expose `OnRedirectToLogin` and
`OnRedirectToAccessDenied` for explicit 401/403 behavior. No reliance on inferred
or unverified endpoint metadata API names is needed.

**Alternatives:** Trusting actor headers, body IDs, display claims or
`isInitialAdministrator` as universal permission violates FR-007 and future
006. No speculative permission engine or MCP flow is implemented here.

## R5 - Nominated offline ceremony with atomic consumption

**Decision:** A stopped maintenance command authorizes one exact production
issuer/subject using a 256-bit random one-time secret, private output file and
15-minute expiry. Database stores a domain-separated digest, not the secret.
Only a valid production session for that nominee may complete it. Lock and
compare the singleton; persist consumed ceremony, unique designation, revision
and security record in one transaction. Rejected/replayed callers receive
non-enumerating errors. No automatic or public reopen.

**Rationale:** Existing maintenance admission provides an operator authority
boundary without opening anonymous administration. The host store already
supports local transactions on both providers. Exactly one database
designation/singleton constraint, not a process lock or hash alone, prevents
multiple winners.

**Alternatives:** First visitor, email match, special IdP claim, reusable
environment bootstrap password and development admin are all excluded by
the spec. Extra online enrollment services or a production password system
are unnecessary.

## R6 - Explicit platform format 2, with exact validation

**Decision:** Extend provider-specific host SQL migrations and their digest
history; preserve format-1 bytes and module format/allocator/marker rules.
Format 2 adds host identity tables and exact constraints/physical inventory.
`--identity prepare` requires verified registry format 1/2 under stopped
exclusive authority; table-free v0 needs explicit existing registry upgrade
first. Serving requires prepared format 2 and never migrates.

**Rationale:** `RegistryMigrationRunner` currently validates exactly three
platform tables, one migration and registry/user version 1.
`StorageReconciliation.ValidateStartupAsync` and composition storage
inspection reuse those checks. All affected consumers must distinguish
verified v1/v2 from corruption. Host-owned SQL follows existing platform
practice; module-owned EF contexts/history remain mandatory and untouched.

**Alternatives:** Silently allowing unknown tables/history weakens accepted
ownership. A separate module store creates needless cross-owner coordination
for explicitly host-owned identity. Replacing the existing registry with a
new ORM schema would expand risk/scope.

## R7 - Versioned complete recovery, explicit security reconciliation

**Decision:** Add recovery-v2 for format 2; retain strict recovery-v1 only for
actual v1 stores. Include full platform/module data and an exact identity
security summary. Key/client/certificate secrets remain separately managed,
not ordinary database-backup artifacts. Restore only into stopped quarantine,
reconcile identity state, advance the session epoch, invalidate all restored
sessions/attempts/pending authorization and provision new protected key material.
Never reopen a consumed or historically uncertain bootstrap automatically.

**Rationale:** `RecoverySetVerifier` currently requires registry version 1 and
an exact artifact set (including exactly one PostgreSQL dump). Add an explicit
format branch rather than letting v1 accept arbitrary extras. Ordinary
restarts retain the private key ring; restore deliberately invalidates sessions.
[Data Protection configuration][dp-config] requires explicit persistence and
at-rest protection when choosing a filesystem store. A private
certificate-protected ring is portable, not Azure-dependent.

**Alternatives:** Platform-only backup/reset after module/identity data,
old-binary downgrade and automatic Down migration are unsafe. No snapshot can
prove enrollment did not occur after its capture; rollback-proof historical
administrator state is explicitly not claimed.

## R8 - Development is a separate identity namespace

**Decision:** Require Development environment, explicit `development` mode
and enable flag, plus loopback host/web/backend/public origins and listeners.
Use configured synthetic identity keys, separate namespace/session mode, no
production enrollment and no default administrator. Reject unsafe-exposure
opt-in as an override. Switching to OIDC invalidates all development sessions.

**Rationale:** Existing config permits an explicitly unsafe local exposure
switch; the stricter identity restriction must check the full exposure set
independently. Email/display equality is never an identity merge key.

**Alternatives:** Silent development defaults, testing headers and a deployed
test-auth bypass would defeat production trust.

## R9 - Finite input, network, state and secret boundaries

**Decision:** Freeze admission limits in identity-v1; reject one-over values.
Use one configured public origin/provider, exact trusted metadata endpoint
locations, operator-approved DNS/network destinations and no automatic
backchannel redirects. Bound discovery/JWKS/token response bytes, request
count, timeout and signing-key refresh. Persist private keys across normal
restart; require certificate protection in OIDC mode and explicit rotation/
session-revocation procedures. Secret references are private files, not logged
values or command arguments.

**Rationale:** Existing bootstrap strictly rejects unknown `KOFTE_*`; add only
documented keys and forward them deliberately through worker admission.
Request cancellation alone does not redefine inherited maintenance/startup
envelopes. Metadata-selected endpoints need validation just like initial
authority, not blind handler fetching.

**Alternatives:** Arbitrary authority query parameters, unlimited discovery/
refresh, HTTP production trust and catch/retry-with-development are rejected.
No SSRF control is claimed to sandbox a malicious installation operator.

## R10 - Useful minimal security evidence with explicit capacity

**Decision:** Durable `user_created`, `initial_administrator_enrolled` and
operator security-reconciliation records accompany their owner transaction
and remain for the lifetime of that installation state. Session mutation
records expire after 30 days; external diagnostics use a documented 30-day
restricted operator retention policy. No public query/export is added.
Bound records/labels/retention work and refuse required writes on capacity or
recording failure; do not silently lose an obligatory durable record.

**Rationale:** FR-017 requires durable evidence without installing future 009.
Current telemetry uses explicit sources rather than blanket HTTP body/query
instrumentation. Preserve that pattern, including composed-worker safe IPC:
no raw callbacks, tickets, provider payloads, nominee/profile data or secrets.
Durability is unchanged/committed/uncertain, not a guessed success.

**Alternatives:** A general audit UI/export/worker is premature. Unbounded
event storage, user-valued metric labels, raw middleware exceptions and
suppressed privacy/capture failures violate the approved safety boundary.

## R11 - Small accessible identity journey

**Decision:** Reuse existing web/design primitives for loading, signed-out,
signed-in, expired, development and safe-failure states. Clear identity on
401/503 and after local sign-out; never persist it in shared/SSR/client
caches. Preserve only allowlisted local return paths. Enrollment is a small
signed-in form, not a general admin console.

**Rationale:** FR-020/SC-005 already fix keyboard/focus/announcements, 320px,
200% text, no serious/critical axe findings and five-second post-return
response. The full shell is 008, not needed to prove this journey.

**Alternatives:** A full navigation/permission console expands scope; a bare
redirect-only login with no failure/expiry behavior misses acceptance.

## R12 - Independent evidence through delivered interfaces

**Decision:** A test-owned HTTPS OIDC issuer uses actual discovery, JWKS,
authorization redirect, nonce/state and single-use code redemption with
PKCE/client checks. Generate RSA-signed positive/negative evidence with
independent test fixtures/frozen expectations, not the production mapper or
validation result as oracle. Real browser tests use a real owned server;
in-memory authenticated principals alone are insufficient.

Add one separately compiled test-owned actor consumer and real provider
identity suites, extend named verifier inventories and execute all public
guide recipes. Exact before/after data, twenty enrollment contenders, three
process restarts, real commit barriers, migration/restore and channel canaries
are required. Test-only controls stay in owned fixtures/private IPC, never a
production endpoint/flag.

**Rationale:** [Microsoft integration-testing guidance][integration] supplies
host/fixture primitives, not a packaged end-to-end OIDC issuer recipe. Fixture
composition is an explicit project design. Existing provider-owning verifier,
public consumer and privacy patterns supply the execution boundary.

**Alternatives:** Mocked principals, in-memory databases, count-only assertions
or copying the production result into expected data cannot demonstrate this
feature. A third-party production IdP dependency is unnecessary for portable
tests, but optional real-provider interoperability may supplement the fixture.

## Resolution and evidence limits

All technical choices are resolved at proposal level. Exact package-archive
review, compatibility tests, measured budgets, migrations, protocol/consumer
proof and human ADR approval are implementation/review gates, not unresolved
product questions or results fabricated by research. No tasks or later
reviewer-owned checklist are generated here. No canonical conflict or required
constitutional amendment was identified.

[oidc]: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-oidc-web-authentication?view=aspnetcore-10.0
[options]: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.openidconnect.openidconnectoptions?view=aspnetcore-10.0
[core]: https://openid.net/specs/openid-connect-core-1_0.html
[par]: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.openidconnect.openidconnectoptions.pushedauthorizationbehavior?view=aspnetcore-10.0
[tickets]: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.cookies.iticketstore?view=aspnetcore-10.0
[cookie]: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0
[api-auth]: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/api-endpoint-auth?view=aspnetcore-10.0
[csrf]: https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
[dp-default]: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/default-settings?view=aspnetcore-10.0
[dp-config]: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0
[bff]: https://learn.microsoft.com/en-us/aspnet/core/blazor/security/blazor-web-app-with-oidc?view=aspnetcore-10.0
[rewrites]: https://nextjs.org/docs/app/api-reference/config/next-config-js/rewrites
[integration]: https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
[antiforgery-api]: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.antiforgery.iantiforgery?view=aspnetcore-10.0
[cookie-events]: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.cookies.cookieauthenticationevents?view=aspnetcore-10.0
