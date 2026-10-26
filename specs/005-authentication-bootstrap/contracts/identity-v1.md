# Identity v1: Browser, HTTP and Trusted Actor Contract

**Status:** Proposed for 005; not implemented or a token-issuance contract.
**Version:** HTTP `/api/identity/v1`; JSON `version: "1"`; actor-v1.
**Owner:** Host identity; browser adapters are transports, not authenticators.

## Origin and credential boundary

One configured public origin. In OIDC mode it is HTTPS, without credentials,
path, query or fragment. Native local development may use its exact loopback
HTTP web origin only. The .NET backend remains a fixed loopback destination;
005 does not add a remote database/listener or trusted-forwarded-header profile.
Production ingress can terminate TLS and route the enumerated paths directly
to .NET; otherwise exact Next handlers forward them. The same contract applies.

The session cookie contains only a protected opaque server-session reference,
never provider tokens, externally asserted permissions or browser-readable
session material. OIDC cookie name `__Host-kofte-session`, Path `/`, no Domain,
Secure, HttpOnly, SameSite=Lax. Explicit loopback HTTP development uses a
different `kofte-dev-session` cookie, HttpOnly/SameSite=Lax/Path `/`, never
accepted by OIDC mode. Protocol correlation/nonce and antiforgery cookies
use separate bounded purpose-bound names; production cookies are Secure.
The GET/query callback permits Lax correlation without requiring cross-site
POST cookies. A fresh session replaces pre-login continuity after validation.

Direct API clients may use the same cookie exchange/session as the browser;
no separate login grant or bearer token is issued. An `Authorization` header,
including one presented alongside a valid cookie, is an unsupported credential
and receives 401. An ID token is not a Köfte API token.

All identity responses, errors and protocol redirects set `Cache-Control:
no-store`, `Pragma: no-cache`, `Referrer-Policy: no-referrer` and an appropriate
content type. No shared/SSR/service-worker cache or localStorage identity.
No permissive CORS credential policy. Requests cannot choose actor, provider,
backend, authority, callback origin or a different user.

## Endpoint inventory

| Method/path | Access and input | Result |
| --- | --- | --- |
| `GET /api/identity/v1/browser-context` | Explicitly public, no query/body. Bounded pre-login browser/antiforgery continuity; no user/enrollment status. | 200 `{version:"1", identityMode:"oidc" or "development", csrfToken:string}`; purpose-bound antiforgery cookie. |
| `POST /auth/login` | Public protocol entry; exact Origin + antiforgery pair; form fields `returnPath` and `__RequestVerificationToken` only. OIDC mode only. | 303 to the configured authorization endpoint, with necessary code+PKCE/state/nonce protocol parameters. |
| `GET /auth/oidc/callback` | Public protocol endpoint, bounded standard OIDC query; no body. Valid protected state/browser binding, nonce/code/issuer/token required. Not a general anonymous action. | Validated commit sets cookie and 303 to admitted local return path. Invalid/cancelled/unavailable exchange returns fixed safe failure/retry without a session or raw provider error. |
| `POST /api/identity/v1/development-session` | Explicitly public only in admitted development mode; Origin + antiforgery; JSON `{identityKey:string}` from configured synthetic keys only. | 200 safe current-identity body + development cookie. Outside development: 404, never login fallback. |
| `GET /api/identity/v1/me` | Valid current session; no query/body. | 200 `CurrentIdentityV1`, or 401/503. |
| `POST /api/identity/v1/sign-out` | Current session + Origin/antiforgery; empty body. No caller-selected session/user. | 204 only after confirmed durable revocation; expire cookie. Invalid/expired session: 401. Uncertain storage result: 503, clear local UI without claiming confirmed revocation. |
| `POST /api/identity/v1/first-administrator` | Current OIDC session + Origin/antiforgery; JSON `{authorization:string}` containing the one-time secret. No nominee/user/role fields. | 200 `{version:"1",status:"enrolled",correlationId:string}` after confirmed atomic enrollment. Re-fetch `/me` for current designation. |
| `GET /health/live`, `GET /health/ready` | Explicitly anonymous; preserve accepted method/query/body rules and bootstrap-v1 JSON. | Existing 200/503 aggregate contract, no identity/module/nominee details. |
| `GET /openapi/v1.json` | Existing Development-only publication. | Regenerated health + identity contracts; no secrets, live identity or test endpoints. |

`/identity` is the minimal web page, not an unguarded identity API. Other local
return paths are `/` only in v1. Canonical allowlist is exactly `/` and
`/identity`: reject absolute/scheme-relative URLs, backslashes, encoded
separators, fragments, query-bearing or unknown destinations. Invalid caller
input receives 400; do not silently normalize it into a new destination.
Default omitted returnPath is `/identity`, explicitly documented.

Unsupported methods produce 405 with exact Allow; HEAD/OPTIONS are not automatic
mutation bypasses. Unknown paths stay 404. Explicit health guards must not
intercept/reject valid auth/API routes, nor broadly open other routes.
Authentication fallback policy denies protected endpoints; explicit public
metadata is reserved for the inventory above. API failures never challenge
OIDC/return provider HTML. Only deliberate `/auth/login` initiates a challenge.

## CSRF and browser binding

Use framework `IAntiforgery.GetAndStoreTokens` for the browser-context response,
a separate HttpOnly antiforgery cookie and configured `X-Kofte-CSRF` request
header (or the named login form field). JS holds only the request token in
memory; it cannot read the session cookie. After login/identity change fetch
a fresh token pair. Do not reuse an anonymous identity-bound request token
as the signed-in token.

Explicitly call `ValidateRequestAsync` before every introduced browser
mutation, including JSON endpoints; do not assume form-binding middleware
automatically protects JSON. Require exact configured Origin and reject
missing/foreign/null Origin for these browser mutations; reject cross-site
Fetch Metadata where present. Non-browser callers follow the same rule and
cannot bypass the token pair. Do not trust `X-Forwarded-*`, identity headers or
caller body fields. The OIDC GET callback uses its independent protocol binding
rather than an Origin requirement that would reject a legitimate provider.

## Successful identity and errors

`CurrentIdentityV1`:

```json
{
  "version": "1",
  "userId": "00000000-0000-4000-8000-000000000001",
  "displayName": null,
  "identityMode": "oidc",
  "isInitialAdministrator": false
}
```

The UUID above is illustrative, not an actual fixture/user. Fields are required;
displayName is nullable escaped safe text. No issuer, subject, email, role,
permission, session ID, token, bootstrap nominee/state or other user's identity.
Clients may ignore future additive response fields but never synthesize
missing/invalid required fields; request bodies reject unknown/duplicate fields.
Breaking changes require a new major contract; shared OpenAPI/types and
independent C#/TypeScript consumers verify the delivered shape.

JSON API failure is `application/problem+json` with fixed `type`/`title`,
numeric `status`, safe `code` and generated `correlationId`. No raw detail,
request URL/query, exception or account-specific reason. Codes:

| Status | Safe code / meaning |
| --- | --- |
| 400 | `identity_request_invalid`: malformed/oversized contract input or invalid local return. |
| 401 | `identity_unauthenticated`: absent/forged/expired/revoked/wrong-mode or unsupported credential. |
| 403 | `identity_forbidden`: authenticated insufficient authority, invalid Origin/CSRF or non-nominee enrollment. No nominee disclosure. |
| 409 | `identity_enrollment_unavailable`: absent/expired/replaced/consumed ceremony or concurrent loser; no distinction revealing nominee/history. |
| 429 | `identity_admission_exceeded`: finite rate/capacity admission, bounded Retry-After where a window is known. |
| 503 | `identity_unavailable` or `identity_outcome_uncertain`: dependencies/recording unavailable or commit acknowledgement unknown; never report known success. |

Callback failures may 303 only to fixed local `/identity` with an allowlisted
outcome code and generated correlation, or return the bounded safe error when
binding is invalid. Never copy code/state/token/error_description into that
destination. Reject unsolicited callback before any session/protected effect.

## Next.js adapter contract

Enumerate the endpoint/method pairs above individually; no path supplied by a
caller, catch-all forwarding, arbitrary outbound URL or generic SSR fetch.
Read the server-only fixed backend configuration, never a request header.
Retain existing anonymous readiness adapter behavior separately.

Forward only the required Content-Type, exact Origin, CSRF header and admitted
cookie names (session/antiforgery/framework correlation/nonce with bounded
prefix/count/bytes). Never forward Authorization, Host, forwarded headers,
user claims or arbitrary cookies. Response propagation permits only the
contract's status/content type/safe body, required individual Set-Cookie values
and validated Location. Do not merge multiple Set-Cookie headers.

The login redirect may target only the configured authorization endpoint;
callback completion only the admitted same-origin local destination.
Use manual redirects, streaming/byte limits and a deadline shorter than the
UI envelope. No automatic redirect following, retry or error-body reflection.
On a lost mutation response return `identity_outcome_uncertain`, not a
fabricated logout/enrollment result. Safe diagnostics exclude request/response
secrets and browser/provider payloads. Direct-host and adapter parity are
independently exercised.

## Trusted actor-v1

Planned Platform types: immutable `AuthenticatedActorV1` with `UserId` (nonempty
Guid), `IdentityMode` (`Oidc` or `Development`) and
`IsInitialAdministrator` (bool); request-scoped read-only `ICurrentActorV1.Actor`.
Null denotes no established actor. Verification failure/unavailability aborts
protected request processing, not a success-shaped anonymous actor fallback.

The host populates this value from its verified store and current session.
Modules never receive ticket material, provider claims or a database handle.
This is a trusted in-process boundary, not a sandbox or a transferable signed
credential. Future jobs/MCP must establish and authorize an actor at their own
entry, not deserialize this object from a caller as proof.

The initial designation grants no team/resource/registered permission. Current
identity and sign-out scope to self; enrollment requires the exact nominee and
authorization. Unrelated actions remain denied until 006 defines permissions.
A separately compiled test-owned consumer verifies equality with `/me` and
denial of ungranted actions, including for the designated administrator.
No test-only endpoint or permission bypass ships in production.

## Finite budgets

These are design acceptance bounds, not measurements. Tests use a controlled
clock for lifetime boundaries, real wall-clock deadlines for effects, and
one-over-limit cases. Do not lengthen inherited startup/maintenance budgets.

| Boundary | Limit and refusal |
| --- | --- |
| Login continuity | Pending five minutes; 256 attempts/installation, eight/browser; nonce/state binding expires with the attempt. |
| Local session | 30-minute idle/eight-hour absolute; eight active/user, 50,000 total; 10,000 users. Refuse new capacity, no silent eviction of another session. |
| Enrollment | 256-bit base64url unpadded secret, exactly 43 characters; one pending ceremony, fifteen minutes. |
| Callback/local work | Four seconds callback processing including store/egress; one second local `/me`, session/enrollment transaction work. Adapter maximum 4.5 seconds; actionable UI within five seconds after provider return. UI navigations/provider interaction measured separately. |
| Egress | At most three backchannel requests/callback (discovery, JWKS, token), two seconds each clipped to three seconds combined and the remaining callback budget. No automatic HTTP redirect/retry; at most one admitted key refresh. Fail closed when an unknown key cannot fit. |
| Provider metadata/keys | 256 KiB each, sixteen signing keys, eight configured exact endpoint destinations, no UserInfo/PAR calls. Warm cache at most fifteen minutes; unknown-key refresh at most once/minute, no use of expired/unverifiable material to admit login. |
| HTTP input | Callback request target 16 KiB; auth form/JSON bodies 8 KiB; token response 64 KiB, individual ID token 16 KiB; no compressed/unbounded response expansion. Headers/cookies total 16 KiB and at most sixteen admitted auth cookies. |
| Identity/display | Issuer 2,048 UTF-8 bytes; nonempty printable ASCII subject 255 bytes; display 128 Unicode scalar values; request field/path counts fixed by the contract. |
| Start/callback rate | Login starts 60/minute/installation and ten/minute/browser; callbacks 120/minute/installation and twenty/minute/browser. |
| Authenticated rate | Enrollment forty/minute/session and 200/minute/installation (admits twenty contenders); other identity requests 1,200/minute/installation and 120/minute/session or pre-login browser binding. Peer limits use actual transport peer, never forged forwarding headers. |
| Stored limits | At most 4,096 live admission windows; event capacities/retention and at-most-128-row pruning in the data model. No unbounded request-driven sweeping. |
| Security diagnostics | One bounded terminal outcome plus bounded phase codes, at most 1 KiB/event; fixed operation/outcome/mode/provider-category labels, no user/subject/session values as metric labels. |

Static configuration and required prepared store/key checks complete within
existing host startup budgets; provider discovery is lazy/bounded rather than
silently blocking startup indefinitely. Provider outage may leave existing
locally valid sessions usable only within these finite lifetimes. Local
sign-out/epoch revocation is checked on the next request; upstream IdP revocation
is not promised to propagate immediately.

## UI state contract

Use existing design tokens/components. `/identity` renders a non-personal shell,
then loads browser-context/current-user with no-store. State is loading,
signed-out, signed-in, expired, visible development, or safe failure with
correlation/retry. A failed/expired identity check removes stale signed-in
content; no SSR user snapshot flashes after logout or another login.

Login/enrollment/sign-out controls work with keyboard, visible focus and
announced progress/errors. The secret is an explicit password-style input
for its necessary protected enrollment exchange, never a URL, stored form
value, analytics event or server-rendered page value. Development identity
selection is configured synthetic data and prominently labeled. Returning
from OIDC preserves only the admitted local path. Retry is user-directed,
not an endless provider redirect loop.

Require 320-CSS-pixel width, 200% text, keyboard/focus/announcement evidence,
zero serious/critical axe findings and the measured five-second post-return
envelope for signed-in or actionable failure. Provider interaction time is
reported separately; no human usability study is invented.
