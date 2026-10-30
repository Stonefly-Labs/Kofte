# Account Admission v1: Trusted 006 Integration Boundary

**Status:** Proposed 005 contract, not 006 onboarding/RBAC implementation.
**Version:** account-admission-v1; consumes actor-v2/canonical GUID users.
This is an in-process service, **not an HTTP/MCP endpoint, bearer grant or
authorization bypass**. No browser binder/JSON endpoint exposes these commands.
Trusted modules are not sandboxed; the boundary prevents ordinary caller data
from being mistaken for authority, not hostile in-process code.

## Minimal surface

`IAccountAdmissionV1` has operations to admit a local or verified external account
and set an existing account's enabled state. Read-only results expose only
version `"1"`, user ID, enabled state, checked account revision, safe status
`committed|already-committed`, and correlation. No credentials/session/roles/
team membership are returned. Self-service auth remains identity-v2.

Admission input is an operation UUID, host-issued enrollment handle and optional
opaque future authorization reference. Account-state input is operation UUID,
target user GUID, expected account revision, desired enabled state and optional
authorization reference. There is no submitted actor, `isAuthorized`, role,
Owner flag, email-match or arbitrary SQL/connection field.

The enrollment handle is a random request-scoped host lookup reference, not
proof merely because someone supplies its UUID. It resolves only within the
owning verified continuation to either validated local credential input or
validated exact OIDC evidence, method/connection, purpose and browser/attempt
binding. It cannot be serialized/deserialized as permission or replayed in
another request; consume it once. Raw passwords/evidence stay in the Host's
private bounded operation scope, never shared Platform result objects.
The future 006 adapter resumes this host enrollment continuation after its
invitation/onboarding authorization; 005 adds no hidden registration endpoint.

## Policy and actor establishment

`IAccountAdmissionPolicyV1` evaluates the exact nonsecret operation intent,
current host-established actor (if present), and optional authorization reference.
It returns a typed deny/unavailable or authorized decision with trusted initiating
actor/correlation/intent binding. Its product default **always denies**, including
an initial Owner. No registered 006 policy means unavailable capabilities are
denied, not temporarily allowed. Missing/duplicate policy registration fails
explicitly; no last-registration-wins authority.

When 006 is implemented, its centrally registered policy may authorize a current
actor or resolve/validate its own durable invitation/onboarding grant and
initiating actor. The opaque reference itself, a browser-provided invitation
flag, matching email or an IdP administrator claim is never proof.
The invitation lifecycle, permission names/team/resource policy and delivery/
revocation semantics remain 006's contract, not a grant invented by 005.
005 must invoke that policy at the operation boundary before account effects;
it never reads another module's invitation/team tables or claims cross-module
atomicity. If authorization expires/fails, no account operation proceeds.

Initial protected setup is a separate narrow admission path with its own token/
browser/first-Owner checks; it does not install a permissive generic policy.
005's operator reset is likewise a distinct stopped authority, not this service.

## Account effects and invariants

After authorization, revalidate enrollment/current trust, uniqueness and expected
revision in one Host owner transaction. Use framework UserManager/store calls
and required security evidence; record operation/result idempotency before
commit. Admission creates one canonical user/method and no team/role/Owner grant.
It does not itself issue a browser session; the verified login continuation may
include session creation in that same commit before publishing a cookie.

External proof cannot attach an identity owned by another user, infer a link
from profile/email, retarget a connection or move a development identity.
Existing-user method association uses the separate fresh self-service boundary,
not account-admission-as-merge. Local admission validates framework policy and
unique normalized name without exposing/hash-recording the password.

Disablement denies all methods and immediately invalidates sessions/proofs/
pending resets by security version/stamp. Enablement preserves user/methods/
Owner provenance and does not revive revoked sessions or mint credentials.
Both require the future policy and expected revision; 005 has no admin UI/route.
An initial Owner cannot call them successfully under the default policy.
The immutable initial designation is not removed or reassigned by these actions;
006's mutable authorization roles remain a separate concept.

## Idempotency and failure

Authorize every invocation, including a retry, before disclosing a prior result.
Same operation/actor/nonsecret intent returns its original committed user/revision
and explicit already-committed status, without rerunning credential effects.
Conflicting operation reuse fails. Do not persist a password hash as an input
fingerprint: only the account's framework verifier is stored.
Already-committed never promises newly resubmitted password bytes were installed.

Typed outcomes: forbidden, invalid input, method unavailable, generic conflict,
stale revision, capacity/rate refusal, store unavailable or outcome uncertain.
Log sanitized correlation/phase/durability through the existing emitter;
never use a null actor/success-shaped default on store/policy failure.
No automatic retry after uncertain commit. Exact operation-ID reconciliation
under fresh authorization can recover a confirmed result.
The one-second host-local budget includes policy continuation, manager/state/
commit/cleanup work; no nested fresh budgets or shared transaction with 006.
Long invitation interaction occurs before this bounded account operation.

## Independent proof required

A separately compiled test-owned consumer exercises v2 actor equality plus:
default denial for ordinary/Owner/forged reference; a test-host-only narrowly
authorized continuation using actual local/OIDC evidence; exact user/method/
event/result preservation on duplicate/conflict/failure/unknown commit;
disabled-account next-request denial across both methods; unchanged Owner and
no manufactured role/team authority. Test configuration is not a shipped
endpoint, production bypass flag or arbitrary policy installation mechanism.

006/007 must explicitly adopt this version and actor-v2 during their own planning.
No 006 artifacts, administration workflow or new numbered-feature stage are
created by this contract.
