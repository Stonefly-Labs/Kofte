# Identity v2: Browser, HTTP and Trusted Actor

**Status:** Proposed revised 005 contract; not delivered endpoints.
**Version:** `/api/identity/v2`, JSON `"version":"2"`, actor-v2.
Read [plan](../plan.md), [model](../data-model.md) and
[operator-v2](operator-v2.md). Delivered [identity-v1](identity-v1.md) is preserved,
not silently extended with a local mode.

## Trust and common HTTP rules

Production local and OIDC both require the same configured HTTPS public origin,
certificate-protected installation keys, `__Host-kofte-session` opaque-reference
cookie (Secure/HttpOnly/SameSite=Lax/Path `/`, no Domain) and no-store/no-referrer
responses. Use new v2 ticket/cookie protection purposes and epoch, not old tickets.
Development retains distinct loopback-only cookies/purposes and visible labeling.
Bearer/Authorization headers, including mixed cookie+bearer, remain unsupported
and return 401. No provider token or role becomes a browser/API credential.

All introduced browser mutations validate exact Origin, framework antiforgery
cookie/request pair (`X-Kofte-CSRF` or named form field) and Fetch Metadata.
No missing/null/foreign Origin bypass for API clients. Callback GETs instead
require framework state/nonce/correlation/PKCE and the durable bound attempt.
Methods/paths/body/query/cookie/field sets are exact; unknown/duplicate fields,
encoded alternate paths or unsupported methods fail without effects (400/404/
405 with exact Allow). No catch-all identity proxy or automatic HEAD/OPTIONS.
Return paths are exactly `/` or `/identity`, default `/identity`; reject invalid
input, do not normalize it into a redirect.

All errors are bounded `application/problem+json` with fixed type/title/status/
code and generated correlation ID, never account/provider/token/exception data.
API 401/403 never redirects to a provider. All responses and auth redirects use
`Cache-Control:no-store`, `Pragma:no-cache`, `Referrer-Policy:no-referrer`.
Clear stale signed-in UI after session refusal; no shared/SSR/localStorage cache.

## Endpoint inventory

`{connectionId}` means an exact canonical GUID in the configured enabled OIDC
connection set, not an arbitrary provider URL/scheme. Each expanded route/method
must be registered/forwarded explicitly and collision-checked.

| Method/path | Authority and exact input | Confirmed result |
| --- | --- | --- |
| `GET /api/identity/v2/browser-context` | Public, empty body/query. | 200 version/context, enabled methods `{connectionId,kind,label}`, `setup:"unclaimed" or "closed"`, CSRF request token. No claimant/user/key/issuer details. Closed also covers recovery lock; it is not permission to reopen. |
| `POST /api/identity/v2/setup/admit` | Public + Origin/CSRF; JSON `{setupToken}`. | 200 `{version:"2",status:"setup-admitted"}` and new opaque setup cookie after durable admission; no authenticated session. |
| `POST /api/identity/v2/setup/local-account` | Current bound setup admission + enabled local method + Origin/CSRF; `{userName,password,displayName?,returnPath?}`. | 200 current identity + session cookie after atomic account/Owner/consumption/session/evidence commit. Collision/loser gets generic 409 with no tentative account. |
| `POST /api/identity/v2/local/sign-in` | Public + enabled local method + Origin/CSRF; `{userName,password,returnPath?}`. | 200 current identity + cookie after known commit. An existing admitted user may claim pending setup only with valid bound setup admission. Unknown names never self-register here. |
| `POST /auth/identity/v2/oidc/{connectionId}/start` | Public + Origin/CSRF; form `returnPath?`, `__RequestVerificationToken`. | 303 to that configured authorization endpoint. Bind any valid setup admission into the attempt; request input cannot select claim authority. |
| `POST /auth/identity/v2/oidc/{connectionId}/reauthenticate` | Current session owning that association + Origin/CSRF; same bounded form. | 303 fresh authentication (`prompt=login`, `max_age=0`); attempt binds current user/session. |
| `POST /auth/identity/v2/oidc/{connectionId}/associate` | Current session + unconsumed fresh account-control proof + Origin/CSRF; same bounded form. | 303 separate new-method proof, bound to existing user/session/proof. No normal login-as-another-user substitution. |
| `GET /auth/identity/v2/oidc/{connectionId}/callback` | Public protocol boundary; bounded standard GET/query, empty body; exact connection/state/purpose/attempt. | Login: commit then cookie/303 admitted return. Reauth: replace session and create bound proof then 303. Association: commit binding/invalidation/event then 303 to signed-out identity result. No cookie before known commit. |
| `GET /api/identity/v2/me` | Fresh verified current session, empty body/query. | 200 `CurrentIdentityV2`, else 401/503. |
| `POST /api/identity/v2/sign-out` | Current session + Origin/CSRF, empty body. | 204 only after durable revocation/evidence; expire cookie. Uncertainty is 503, not confirmed sign-out. |
| `POST /api/identity/v2/account/reauthenticate/local` | Current account with local credential + Origin/CSRF; `{password}`. | 204, rotated committed session cookie and five-minute single-use proof bound to replacement session. No proof token in response. |
| `POST /api/identity/v2/account/password/change` | Current local account + Origin/CSRF; `{currentPassword,newPassword}`. | Check-only framework fresh local proof/lockout then `ChangePasswordAsync`; 204 after credential/stamp/version/invalidation/event commit, expire old cookie. |
| `POST /api/identity/v2/local/password-reset/request` | Public + Origin/CSRF; `{userName}` within syntax limits. | Always 202 `{version:"2",status:"operator-assistance-required",correlationId}` without account lookup or promised delivery. User must contact operator privately; no fake email/queue success. |
| `POST /api/identity/v2/local/password-reset/complete` | Public + Origin/CSRF; `{userName,authorization,newPassword}`. | 204 after valid account-bound reset/consumption/invalidation/event commit. No automatic session. |
| `GET /api/identity/v2/account/methods` | Current session, empty body/query. | Own local presence/name and external method connection IDs/labels only; no external subjects/tokens or other users. |
| `POST /api/identity/v2/account/methods/local` | Current session + fresh single-use proof + enabled local method + Origin/CSRF; `{userName,password}`. | 204 after framework password association/version/invalidation/event commit; existing reserved local name must match if previously assigned. |
| `DELETE /api/identity/v2/account/methods/local` | Current session + fresh proof + Origin/CSRF, empty body; another enabled usable method required. | 204 after framework removal/invalidation/event, preserving local-name reservation and user ID. |
| `DELETE /api/identity/v2/account/methods/oidc/{connectionId}` | Current session owns binding + fresh proof + Origin/CSRF, empty body; another usable method required. | 204 after removal/invalidation/event; canonical external ownership remains reserved, no reassignment/merge. |
| `POST /api/identity/v2/development-session` | Explicit admitted Development only + Origin/CSRF; `{identityKey}` from configured synthetic keys. | 200 current identity + distinct dev cookie; otherwise 404, never production fallback/Owner. |
| `GET /health/live`, `GET /health/ready` | Existing explicitly anonymous exact rules. | Unchanged aggregate health/bootstrap-v1; no identity details. |
| `GET /openapi/v1.json` | Existing Development-only document URL. | Document includes deliberate identity-v2 schemas, unchanged health schemas and no secret/live state/test route. |

Local credential routes return 404 when local authentication is disabled;
development never enables them. Public browser context may describe configured
methods/coarse setup state, never token availability, expiry, claimant or counts.
Unknown OIDC users without setup or future trusted admission receive generic
authentication denial, not an active account or temporary Owner privilege.

## Canonical identity and actor-v2

```json
{
  "version": "2",
  "userId": "00000000-0000-4000-8000-000000000001",
  "displayName": null,
  "context": "production",
  "isInitialOwner": false
}
```

Illustrative UUID only. All fields required; display nullable; context exactly
`production|development`. No roles/permissions/provider identity/credentials/
session ID; production authentication method is not a permanent user type.

Platform's immutable `AuthenticatedActorV2` has `UserId` (nonempty Guid),
`AuthenticationContextV2` (`Production=1`, `Development=2`) and
`IsInitialOwner` (bool). `ICurrentActorV2.Actor` is request-scoped/read-only;
null means no established actor, never unavailable-store fallback.
Host creates it from fresh verified durable state and scopes all self-service
operations to it. Do not deserialize it as proof or pass database/ticket material
to modules. 006 interprets Owner provenance through centrally registered policy;
it is not a universal allow flag or an Identity role.

Preserve v1 type definitions/contract documents, but retire each enumerated v1
HTTP operation and old `/auth/login`/callback with safe 410
`identity_contract_retired` after basic request guards. No old callback effects.
Do not register `ICurrentActorV1` or synthesize local as `IdentityMode.Oidc`.
A consumer requiring the retired service must fail explicitly through existing
bounded activation/service-resolution handling before protected effects.
Identity-aware consumers migrate to v2; identity-agnostic module contracts do
not change. Test both separately compiled v1 refusal and v2 parity.

## Credential, setup and proof semantics

Setup uses `__Host-kofte-setup` (Secure/HttpOnly/Lax/Path `/`, no Domain), whose
random reference is digest-stored and bound to the pre-login browser cookie,
installation/generation/epoch and expiry. It grants no actor. Successful claim
consumes authority and rotates into a new independent application session.
Replay, replacement, cancellation, foreign browser and losing races cannot
publish partial account/Owner/session state. Admission without login is not claim.

Local policy is [research A3](../research.md#a3---one-provider-independent-user-with-explicit-methods).
Normalizers/password/lockout APIs remain framework-owned; reset cannot create
a password for an external-only account. Unknown/disabled/locked/bad-password
login has the same public error; use a bounded framework-hasher dummy check for
early denied paths to avoid a simple account-existence timing oracle.
All local password entry points enforce the same durable lockout.
Do not log, normalize, echo or persist raw passwords.

Fresh proofs are session/user/version-bound and consumed once within five
minutes. OIDC fresh proof requires trusted signed fresh `auth_time`, not merely
an accepted prompt. Successful reauth replaces the session, including after a
framework hash upgrade; successful password/reset/association change invalidates
every prior session/proof and deliberately leaves the user signed out.
Reauth/association callbacks must independently revalidate the bound original
session and matching current browser/session cookie, even where ordinary login
callbacks suppress cookie authentication. A switched/expired/revoked session
cannot complete another account's proof or association.
No implicit link, fallback method or final-usable-method removal.

## Errors and finite bounds

Retain v1 safe 400/401/403/429/503 semantics and truthful
`identity_unavailable` / `identity_outcome_uncertain`.
New safe codes: 401 `identity_credentials_invalid`; 409
`identity_setup_unavailable` or `identity_account_change_unavailable`;
400 `identity_password_policy` (public policy, never account reason);
410 `identity_contract_retired`. Reset wrong account/token/expiry/replay all
use the same 400 `identity_reset_invalid`. No distinction identifies a claimant,
user collision, lockout, association owner or reset eligibility.

| Boundary | v2 limit / behavior |
| --- | --- |
| Existing HTTP/egress/session budgets | Retain identity-v1 finite limits: 8-KiB bodies, 16-KiB callback target/headers/cookies, sixteen admitted auth cookies; four-second callback, one-second local work including managers/commit/cleanup; adapter 4.5s, UI 5s; old egress byte/request/cache/time caps and no redirects/retries. |
| Connections | One local, up to eight configured OIDC, separate reserved dev; only enabled methods exposed/accepted. No runtime scheme/authority input. |
| Named-connection egress | At most eight exact configured endpoint destinations/connection, 64 total; a callback uses only its selected connection and retains the original three-request/time/byte caps, never provider fan-out. |
| Local inputs | Name 3-64 ASCII allowed characters/reserved-prefix exclusion; password 12-128 UTF-16 units / <=512 UTF-8 bytes, no controls; new hash 210,000 iterations. |
| Lockout | Five failed checks, fifteen-minute local credential lock; standard recovery/reset preserves user; all password entry points share it. |
| Setup | 256-bit/43-character private token, fifteen minutes; five-minute browser admission clipped to token; 256 active/eight per browser. |
| Attempts / proofs / resets | Five-minute attempt/proof; fifteen-minute reset token; 256 pending per category, eight attempts/browser, one proof/session, one reset/user; reset token <=4 KiB. No unbounded pending rows. |
| New-operation rates | Local login/proof: 60/minute installation, ten/minute browser and actual peer; setup admit/create: 200/minute installation, forty/minute browser (admits twenty contenders); reset requests/completions: thirty/minute installation, five/minute browser/peer; method mutations: thirty/minute installation, ten/minute session. Persistent window enforcement, generic 429. |
| Retained rates / storage | OIDC starts/callbacks and ordinary identity rates unchanged from v1; 4,096 admission windows and data-model event/row/pruning limits. At capacity refuse, never silent eviction/record omission. |

All DNS answers and connected peers must pass both network allowlists and the
forbidden-address policy, including encompassing CIDRs, mapped/mixed addresses;
retain TLS and deliberate approved private providers. No actual metadata-service
probe in tests. Provider outage does not invalidate otherwise locally valid
sessions, create a password or automatically choose another enabled method.

## Next.js and UI

Extend individual allowlisted route adapters and generated types, not generic
forwarding. Preserve bounded streaming/manual redirects/no retries, fixed
server-only backend, admitted headers/cookies, separate Set-Cookie values and
validated Location. Named connection redirects match their exact configured
authorization endpoint; completion stays within admitted local paths.

Use the existing identity page/design primitives for enabled-method choice,
unclaimed/closed setup, secret admission, local creation/login, credentials/
reset instructions/completion, fresh proof/association, signed-in/out/expired,
Development and actionable failure. Token/password inputs are protected explicit
inputs, never prefilled HTML/query/localStorage/analytics. Refresh CSRF after
identity change; clear secrets and stale identity after completion/refusal.
Keyboard/focus/announcements, 320 CSS pixels, 200% text and zero serious/critical
automated a11y findings apply to every introduced state. Measure five-second
post-input/return result separately from human/provider time.
