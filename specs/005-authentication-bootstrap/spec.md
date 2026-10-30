# Feature Specification: 005 - Authentication and Bootstrap Administration

**Feature Branch**: `feature/005-authentication-bootstrap`
**Feature Directory**: `specs/005-authentication-bootstrap`
**Created**: 2026-10-26
**Revised**: 2026-10-30
**Status**: Draft
**Constitution**: 2.0.1; principles I-XII assessed below
**Rework input**: Human-approved production local/OIDC accounts using ASP.NET Core
Identity, one canonical user/session boundary, and first-login ownership guarded
by a one-time installation setup token. The human authorized this specification
rework and alignment with existing 006 after the proposed boundary was explained.
The [ADR amendment](../../docs/adr/0007-authentication-bootstrap-boundary.md#human-approved-scope-amendment-local-accounts-and-first-login-ownership)
records those decisions.
**Authority and progress**: This revision supersedes the original OIDC-only,
exact-person-nominated requirements. It specifies intended behavior, not delivered
behavior or feature acceptance. The old plan, contracts, reviewer checklist,
analysis, tasks and runtime reports remain historical until explicitly reconciled.
Implementation stays blocked for clarification/design/review gates; this rewrite
does not rewind or complete a recorded driver stage.

**Original input (historical)**: Exact fenced inventory 005 generation prompt:

```text
Specify Köfte feature 005: Authentication and bootstrap administration. Scope: MVP, required for initial release. Read docs/product-charter.md, docs/product-vision.md, docs/architecture.md and the project constitution first; do not invent product requirements that conflict with them.

Outcome: Add pluggable OIDC login for production, clearly limited development login, user identity mapping, safe first-admin bootstrap and session/API auth boundaries.

Deliver a user-centered, implementation-agnostic feature specification (not a coding task list) with actors, primary use cases, user journeys, external behavior and explicit functional requirements. Specify observable UI/API/MCP/module interfaces that apply; module ownership and tenant/team permission boundaries; database ownership and migration semantics where applicable; configuration and failure/recovery paths; audit/telemetry; security abuse cases; compatibility/versioning; and acceptance scenarios in Given/When/Then form. Exclude irrelevant interfaces explicitly rather than pretending every feature needs all of them.

Acceptance criteria to include and refine: Anonymous denial, configured OIDC integration test, bootstrap non-repeatability, logged-in UI/API identity consistency.

Respect these invariants: .NET modular monolith using VSA, Next.js frontend, early MCP with shared permissions and business operations, level-3 extensibility, PostgreSQL named schema per module with collision-safe immutable registry allocation, SQLite file per module, no cross-module ACID guarantee, portable self-host/Kubernetes deployments, provider-neutral SignalR, and no MediatR. No hard dependency on Hoard, OpenTofu or Azure. Preserve the Köfte umlaut in user-facing text.

Dependencies / integration assumptions: 001,004. Identify any genuine ambiguity as an open question, without silently changing established decisions. Provide independently verifiable acceptance tests; do not pull post-MVP features into MVP scope.
```

## Scope and Dependencies

**Inventory 005 is an MVP foundation slice**, not a complete authorization system,
identity-management product or production-readiness claim. Its value is that an
operator can choose local accounts, OIDC or both, protect initial ownership with
a setup token, and let a user enter and leave Köfte with one trustworthy identity
across the browser and host. Production local accounts must work without an
external identity provider; they are not the development identity selector.

**Prerequisites:** accepted 001 supplies the host/web, configuration, telemetry
and verification foundations; accepted 004 supplies trusted module composition
and lifecycle. Their accepted 002 identity contracts and 003 storage ownership,
migration and recovery rules remain binding. Before this feature began, PR #7
was verified human-merged, and 004's accepted ledger plus all five acceptance
artifact hashes were verified on updated `origin/main`. Its undiagnosed,
human-deferred historical PostgreSQL failure remains a limitation, not a passing
complete local gate or release waiver.

**Owner and actors:** authentication, internal user identities, login sessions,
bootstrap state and the minimal security record belong to the host's core
identity feature. Optional modules consume the host's authenticated actor rather
than maintaining competing user identities or reading identity tables.
Actors are the installation operator, prospective initial Owner, ordinary
signed-in user, anonymous/forged client and trusted module author. The installation
is one organization with future multi-team scoping; login grants no team
membership, resource access or automatic administration outside the admitted
initial-ownership ceremony.

**Included:** production local-account and provider-neutral OpenID Connect (OIDC)
sign-in, independently selectable or explicitly enabled together; named
authentication connections; stable internal users with explicitly associated
login methods; local credential creation/change/reset and lockout mechanics;
explicit development-only sign-in; setup-token-admitted first-login ownership;
shared browser session/current-identity/API boundaries; sign-out/expiry;
accessible minimal identity UI; safe migration/recovery and correlated security
evidence on both storage providers.

**005/006 delivery boundary:** This rework uses the existing inventory 006,
not a new or renumbered feature.

| 005 owns | Existing 006 owns |
| -------- | ----------------- |
| Canonical users, local credentials, external associations, authentication connections and shared sessions. | Owner-led user administration, invitations/onboarding workflows, account disablement decisions and their registered authorization. |
| Local account creation during admitted initial setup; self-service credential change/reset and explicit login-method association. Account primitives accept only trusted, authorized admission, never a request-supplied privilege flag. | Authorization to add users after setup, issue invitations, associate access with teams, manage memberships and assign roles/permissions. |
| One immutable initial-Owner designation, consumed setup state and a versioned actor/account integration boundary. | Interpretation of that designation as centrally registered Owner permissions, including team/resource scope and positive/negative authorization tests. |

Before 006, an existing admitted user can authenticate and manage their own
credentials. An unknown local or OIDC identity cannot self-register after setup
or gain access merely by knowing the login URL. The later invitation/onboarding
workflow authorizes account admission through 005's boundary; a matching email
or an external administrator claim is not admission. There is no temporary
anonymous registration endpoint or universal Owner bypass to bridge the stages.
005 can be accepted without delivering the 006 administration screens/workflows;
the complete Owner-led onboarding/RBAC journey is delivered by 006.

**Excluded:** unrestricted public registration, automatic account association or
user merging, user-directory synchronization, multiple organizations,
additional non-OIDC providers including SAML (030), teams/roles/granular resource
authorization and administration workflows (006), MCP server/client authorization
flows and tools (007/013), full UI shell (008), general audit browsing/export
(009), module settings administration (015),
machine/service-principal credential issuance, jobs/events/realtime, frontend
extensions, module marketplace, AI chat and external MCP federation.
The initial-Owner designation is an installation-level seed for 006, not a bypass
for future registered permissions. Existing offline maintenance authority does
not become a public login or lifecycle-control interface.

**Sources:** [charter](../../docs/product-charter.md),
[vision](../../docs/product-vision.md),
[architecture](../../docs/architecture.md),
[inventory](../../docs/master-spec-inventory.md),
[constitution](../../.specify/memory/constitution.md) and accepted 001-004
artifacts. No canonical conflict has been identified. Product assumptions and
remaining design choices are distinguished at the end.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Enter Köfte through an enabled login method (Priority: P1)

As a user, I can sign in with an admitted local account or a configured OIDC
connection and see one consistent Köfte identity, regardless of login method.
My external provider's credentials stay with that provider.

**Why this priority**: Every subsequent user-facing feature needs a trustworthy
actor, and the production path must not depend on a development shortcut.
**Independent Test**: Exercise local-only, OIDC-only and coexistence profiles
against each supported store, using an isolated real OIDC provider where enabled.
Observe matching browser/API identity and rejection at disabled/trust boundaries.

**Acceptance Scenarios**:

1. **Given** a valid production profile and an admitted user with an enabled
   local or OIDC login method, **When** that user completes login, **Then**
   Köfte establishes one authenticated internal identity, displays its safe
   summary, and reports the same identity through the authenticated current-user API.
2. **Given** an existing local credential or exact external identity association,
   **When** that user signs in again after restart, a profile change or an
   explicitly authorized addition of another login method, **Then** the same
   internal user and unchanged initial-Owner designation are returned.
3. **Given** different local/external identities with the same email/display
   name, or the same subject at distinct issuers, **When** login is attempted,
   **Then** no association, admission or authority is inferred from that match;
   separately admitted users remain distinct.
4. **Given** invalid, expired, replayed, unsolicited or wrongly addressed login
   evidence, **When** it reaches the login boundary, **Then** no authenticated
   session or privileged identity is created; the user gets a safe retry state
   and the operator gets a correlated rejection.
5. **Given** provider outage, cancellation or invalid configuration, **When**
   login is attempted or the host starts, **Then** a bounded sanitized refusal
   gives safe corrective guidance without redirect loops or automatic fallback.
   An independently enabled local method may be chosen deliberately, but never
   creates a fallback credential, account association or Owner grant.
6. **Given** local-only, OIDC-only or explicit coexistence configuration,
   **When** enabled and disabled methods are invoked, **Then** only enabled
   methods authenticate; local-only needs no external provider and production
   local login retains production transport/cookie/key protections.
7. **Given** consumed setup and an unknown identity without trusted onboarding
   admission, **When** it attempts local registration or valid OIDC login,
   **Then** no active account, session or Owner designation is created; admitted
   existing users can still sign in without a new invitation.
8. **Given** a renamed, disabled or changed authentication connection, **When**
   old and new login evidence is presented, **Then** no existing association is
   silently retargeted or reinterpreted; affected sessions follow the documented
   invalidation policy and no new identity inherits the old user's authority.

### User Story 2 - Claim initial ownership through protected setup (Priority: P1)

As the operator, I can privately authorize initial setup without nominating a
person. The first successful local or OIDC login in that admitted setup becomes
Owner, rather than the first unauthorised visitor to a public installation.

**Why this priority**: Accidental or repeatable bootstrap would compromise every
later administration boundary.
**Independent Test**: On fresh stores, privately authorize setup with a one-time
token, race eligible local/OIDC logins, interrupt completion and restart; inspect
one durable Owner designation, consumed token/setup state and security record.

**Acceptance Scenarios**:

1. **Given** an unclaimed installation without valid setup admission, **When**
   a visitor signs in or submits a setup request, **Then** no Owner is granted
   and public responses disclose no setup token or other user's identity.
2. **Given** valid one-time-token admission bound to the initiating setup session,
   **When** its first successful eligible local or OIDC login commits, **Then**
   that internal user becomes the single initial Owner and the token/setup
   authority is consumed in the same commit, with durable security evidence.
3. **Given** missing, wrong, expired, replaced, foreign-session or replayed setup
   material, or a development login, **When** ownership is attempted, **Then**
   no claim or privileged session is created and the rejection reveals no token.
4. **Given** eligible concurrent local/OIDC attempts or interruption around the
   ownership commit, **When** callers retry or the host restarts, **Then** one
   Owner, consumed authority and security record persist together, or none do;
   losing contenders gain neither Owner authority nor unapproved active accounts,
   and an uncertain result is never reported as known success.
5. **Given** consumed ownership, including a migrated legacy designation,
   **When** configuration/token setup is replayed, the host restarts/upgrades,
   provider configuration changes or the Owner is unavailable, **Then**
   ownership does not reopen and the same designated user ID remains.
6. **Given** admitted setup with failed/cancelled login or token replacement
   before ownership is committed, **When** the user retries, **Then** only
   current unexpired admission can be used, authentication is still required,
   and a replaced token or another browser cannot inherit the setup session.

### User Story 3 - Maintain my account without rebuilding identity (Priority: P1)

As an admitted user, I can manage my own local credentials and explicitly
associate an enabled login method without changing who I am or receiving
another user's access.

**Why this priority**: Production local accounts need a safe lifecycle, not
only a password check; coexistence must not become an account-takeover shortcut.
**Independent Test**: Create the initial local account through protected setup,
change/reset credentials, challenge lockout and association boundaries, and
observe the same user through both enabled methods on each supported store.

**Acceptance Scenarios**:

1. **Given** admitted initial local setup or trusted account admission from the
   later onboarding boundary, **When** local credentials are established,
   **Then** credential policy and account-identifier uniqueness are enforced
   and one stable user results; a forged admission request creates no account.
2. **Given** an authenticated local user and fresh proof of control, **When**
   the password is changed, **Then** the old password stops working and prior
   sessions fail their next protected request unless explicitly replaced by a
   newly established session; the stable user and Owner designation do not change.
3. **Given** an existing local account and privately delivered, account-bound,
   expiring one-use reset authorization, **When** a valid reset completes,
   **Then** only that account's credential changes, the authorization is
   consumed and old sessions stop on their next protected request. Reset
   requests do not reveal whether the account exists or require a specific
   mail service.
4. **Given** missing, forged, expired, replayed or another account's reset
   authorization, **When** a reset is attempted, **Then** credentials, sessions
   and ownership do not change; an OIDC-only user gains no implicit password.
5. **Given** an authenticated user with fresh proof of account control and proof
   of the additional local/external login method, **When** explicit association
   is requested, **Then** only an unassigned method may be attached to that same
   user; an association owned by another user is refused, never merged.
   Removing a method cannot silently leave the account without a usable method.
6. **Given** repeated invalid local credentials or local credential lockout,
   **When** local login is attempted, **Then** bounded failure/lockout rules
   apply without revealing account existence, granting a session or bypassing
   the restriction through another local entry point. Documented recovery
   preserves the user ID.

### User Story 4 - Keep browser and API access within the same boundary (Priority: P1)

As a user or module author, I can rely on the host's identity rather than
browser-supplied claims, and I can tell a missing login from denied authority.

**Why this priority**: A login screen alone does not protect operations or prevent
an adapter/module from becoming an alternate privilege path.
**Independent Test**: Invoke the current-identity boundary through the browser
and directly, then exercise anonymous, forged, cross-origin, expired and logged-out
requests and a test-owned protected operation.

**Acceptance Scenarios**:

1. **Given** no valid authentication, **When** a client requests current identity
   or a protected test operation directly or through the web adapter, **Then**
   the host refuses with unauthenticated semantics and no identity/protected data;
   non-browser API clients receive no provider HTML or automatic login redirect.
2. **Given** a valid ordinary login, **When** the browser and API request current
   identity, **Then** they agree on internal user, production/development context
   and initial-Owner designation; spoofed headers, request fields or changed profile claims cannot
   select another user or grant administrator/team/resource authority.
3. **Given** missing required authority or a cross-origin state-changing browser
   request, **When** a protected action is attempted, **Then** it is refused
   before effects, even if a UI control is hidden or the requester is signed in.
4. **Given** an active session, **When** its user signs out or its admitted
   lifetime expires, **Then** that session cannot access protected data on the
   next request, stale UI identity is cleared and re-entry requires login;
   sign-out does not claim to terminate the external provider's session.
5. **Given** authentication enforcement is enabled, **When** an anonymous probe
   checks existing aggregate liveness/readiness, **Then** the documented safe
   health contract remains usable without identity, module details or secrets;
   protected responses cannot be reused from another user's cache.
6. **Given** credential reset/change, account disablement through authorized
   future administration, session revocation or a security-relevant trust change,
   **When** an affected old session next requests protected access, **Then**
   it is refused using fresh durable state, not a periodic positive-authentication
   cache; unrelated users' identities and consumed ownership are not changed.
   Until 006, Owner designation alone cannot invoke unavailable administration.

### User Story 5 - Develop locally without a production backdoor (Priority: P2)

As a contributor, I can use explicitly selected local identities without a live
external provider while seeing clearly that this is not production login.

**Why this priority**: Local accessibility must not compromise the production
identity or bootstrap guarantees.
**Independent Test**: Enable the development mode in an isolated local profile,
compare web/API identity, then repeat with non-development, non-loopback and
production-store transitions.

**Acceptance Scenarios**:

1. **Given** explicitly enabled development login in the admitted local
   development profile, **When** a contributor selects a configured test
   identity, **Then** browser and API agree on its development-marked identity
   and the UI visibly labels the mode.
2. **Given** production/non-development configuration or non-loopback exposure,
   including the older unsafe-local-exposure opt-in, **When** development login
   is selected or called, **Then** the unsafe configuration or login is refused;
   the opt-in cannot override this identity restriction.
3. **Given** persisted development users/sessions, **When** the installation
   starts in production or a production identity has matching profile values,
   **Then** development authentication and authority are not accepted or merged
   into production users; development enrollment cannot consume or grant the
   production initial-Owner designation. Production local accounts remain
   a separate secure login method, not renamed development users.

### User Story 6 - Operate and recover identity safely (Priority: P2)

As an operator, I can diagnose login/bootstrap failures and maintain identity
storage without disclosing secrets or silently resetting administrator state.

**Why this priority**: An authentication foundation must remain safe through
routine failures, upgrades and recovery, not only its first successful login.
**Independent Test**: Upgrade/reopen both real stores, interrupt identity changes,
capture public/private diagnostics and follow documented correction/retry paths.

**Acceptance Scenarios**:

1. **Given** an existing accepted installation and optional module data,
   **When** identity storage is explicitly prepared/upgraded and reopened,
   **Then** the host owns only its identity data; prior assignments/data remain
   unchanged, and normal serving does not silently perform new migrations.
2. **Given** unavailable/corrupt identity storage or interrupted identity/session
   persistence, **When** login, ownership/credential change or current-identity
   verification requires it, **Then** the operation fails closed with a truthful bounded
   unavailable/uncertain result; corrected explicit recovery preserves committed
   mappings and consumed bootstrap state without a new administrator.
3. **Given** successful/rejected login, bootstrap, expiry or sign-out,
   **When** security evidence and user-visible responses are inspected, **Then**
   each outcome is correlated and safely classified; credentials, tokens,
   login codes and session material stay within their required protected
   authentication exchange, never logs, traces, metrics, unrelated redirects,
   rendered pages or other users' responses. Private configuration and
   unnecessary profile data are not disclosed.
4. **Given** an interrupted or failed login and keyboard-only use at narrow
   width or enlarged text, **When** the user retries or signs out, **Then**
   loading, signed-out, signed-in, development and failure states remain
   understandable and operable without losing a safe local return destination.
5. **Given** a delivered identity store with stable users, exact external
   associations and consumed or pending nominated bootstrap, **When** the
   versioned account-model upgrade is explicitly prepared, **Then** stable IDs,
   associations, consumed ownership and unrelated module data survive; old
   mode-bound sessions are invalidated explicitly. Unconsumed legacy nomination
   material cannot authorize the new flow; fresh setup admission is required
   only if ownership was never consumed.

### Edge Cases

- Duplicate callbacks, multiple browser tabs, concurrent credential creation and
  mixed local/OIDC first-Owner attempts must not create duplicate users/privilege.
- Issuer/subject are trusted identity keys; missing or malformed keys, renamed
  provider labels, reused email, mixed-case/display variants and changed provider
  configuration must not silently relink accounts. Local credentials and
  external associations are login methods, not separate kinds of internal user.
- Expired or foreign login state, callback replay, session fixation, forged
  proxy/identity headers, credential-type confusion and browser request forgery
  must not authenticate a different user or cause protected effects.
- Return destinations outside the admitted local application, malformed redirect
  values and caller-controlled provider/discovery locations must be rejected.
- Provider discovery/signing-key failures or changes must not accept unverifiable
  evidence, contact arbitrary caller-selected destinations or enable dev fallback.
- Disconnection around a commit distinguishes rejection, known completion and
  uncertainty; retries reconcile durable state rather than assume failure.
- Expired/abandoned bootstrap authorization must permit explicit operator
  replacement before completion without reopening a consumed ceremony.
- Setup token possession alone, a stolen/foreign setup-session binding, reset
  authorization or an external administrator claim must not grant ownership.
- Invalid local passwords, unknown accounts, identifier collisions, concurrent
  resets, repeated guesses and attempts during lockout need safe bounded denial;
  credential reset/change cannot leave old sessions valid until a periodic check.
- Explicit method association must prove control, reject an already-owned
  association and avoid both email auto-linking and removal of the final usable
  method. Reset must not add local credentials to an OIDC-only user.
- Invitations/admission from 006 cannot be represented by an untrusted request
  flag or bypass 005's account uniqueness, credential or session protections.
- Secret rotation, restart and profile changes must have documented session
  effects; identity mappings and consumed bootstrap remain stable.
- Restoring an older backup can roll back security state. Restore must keep
  bootstrap disabled by default and require operator reconciliation of identities,
  sessions and administrator state before exposure; no rollback-proof claim.
- Disabled optional modules, empty composition, unknown routes and safe aggregate
  health retain their documented boundaries. Authentication does not grant
  anonymous module management, storage maintenance or debug disclosure.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The host MUST support production local-account login and
  operator-configured, standards-conforming OIDC without requiring a particular
  vendor, cloud or optional module. Local-only operation MUST require no external
  identity service. External provider credentials stay with that provider;
  only validated evidence for an enabled method establishes a user.
- **FR-002**: OIDC login MUST validate configured issuer, intended recipient,
  integrity, validity period and binding to the initiating browser/attempt; reject replay,
  unsolicited/forged evidence and incompatible credential types before creating
  a session. Client-supplied identity/privilege claims are not authoritative.
- **FR-003**: Every admitted local credential and exact verified external
  issuer/subject association MUST resolve to one stable opaque internal user,
  including concurrency/restart. Each login method has at most one owner;
  multiple methods may refer to the same user only through explicit association.
  Profile/connection-label changes MUST NOT change the user key. Matching email,
  display name or provider claims MUST NOT merge users or create associations.
- **FR-004**: The browser and authenticated current-user API MUST expose the same
  minimal internal identity, safe display information, production/development
  context and initial-Owner designation, without credentials or other users'
  data. Production method choice MUST NOT define a different internal user type.
- **FR-005**: Protected host operations MUST reject missing/invalid authentication
  by default. Only explicitly documented non-sensitive public surfaces, including
  necessary login entry/callbacks and existing aggregate health, may be anonymous;
  anonymous does not mean exempt from input validation or protocol checks.
- **FR-006**: API authentication failure MUST use unauthenticated semantics
  (HTTP 401), distinct from authenticated insufficient authority (403), invalid
  input and temporary dependency unavailability. API clients MUST NOT receive
  provider login HTML/redirects in place of an operation result.
- **FR-007**: Authentication MUST supply the same trusted actor to web/API and
  module operations; requests cannot select an actor through body/header/profile
  overrides. Login and initial-Owner designation MUST NOT manufacture team
  membership or bypass future registered action/team/resource checks.
- **FR-008**: Browser authentication MUST resist session fixation, credential
  theft through browser-readable session material, cross-site request forgery
  and unauthorized cross-origin credential use. Sensitive identity responses
  MUST NOT be shared-cached or exposed through unsafe redirect destinations.
- **FR-009**: Sessions MUST have documented finite idle/absolute lifetimes and
  safe renewal/restart/secret-rotation behavior. Local sign-out and expiry MUST
  invalidate the affected session for its next protected request and clear
  stale browser identity; provider-wide sign-out is not promised.
- **FR-010**: Initial ownership MUST require explicit operator-authorized,
  expiring one-time installation setup-token admission and successful local or
  OIDC authentication bound to that setup session. The first eligible login to
  commit wins without prior nomination of a person. Anonymous arrival, token
  possession alone, email/role claims and development login MUST NOT qualify.
- **FR-011**: The initial user/account changes, exactly one installation-wide
  Owner designation, consumed setup-token authority, resulting session and
  required security record MUST commit together in host-owned storage before
  authenticated success is published. Concurrent local/OIDC attempts,
  interrupted replies and restart/retry MUST NOT create a second initial Owner,
  partial committed ownership or replayable authority. Losing contenders MUST
  NOT activate accounts outside trusted onboarding admission.
- **FR-012**: Consumed initial ownership MUST remain closed across ordinary restart,
  upgrade, token replacement, connection changes, configuration replay and Owner
  unavailability. Explicit operator correction/recovery MUST be documented;
  no public bootstrap reset,
  next-login promotion or silent deletion/re-creation is permitted.
- **FR-013**: Development login MUST require explicit opt-in, a development
  environment and loopback-only exposure, with a persistent visible mode label.
  Non-development/exposed combinations MUST fail closed; the existing unsafe
  exposure switch MUST NOT enable development identity.
- **FR-014**: Development identity keys, sessions and any local test authority
  MUST remain distinguishable from production identity. They MUST NOT authenticate,
  inherit authority, merge by profile values or complete production bootstrap
  after a profile change.
- **FR-015**: Invalid or incomplete authentication configuration MUST cause a
  bounded actionable refusal, not an insecure default. Provider/storage outages,
  rejected/cancelled login and verification uncertainty MUST produce truthful
  retry/error states without granting fallback access or redirecting indefinitely.
- **FR-016**: Identity/session/bootstrap persistence MUST use host-owned storage
  and the accepted ownership, explicit preparation/migration and complete-set
  recovery boundaries on both supported providers. Upgrade/restart MUST preserve
  committed stable user IDs, exact external associations, consumed ownership
  and unrelated module assignments/data. Upgrade from the delivered identity
  format MUST preserve a consumed legacy designation and its user ID, retire
  unconsumed legacy nomination material and explicitly invalidate old mode-bound
  sessions rather than reinterpret them. Old migrations/inventory checks MUST
  NOT be edited or weakened to admit the new format.
- **FR-017**: Sensitive identity changes and rejected authentication/authority
  attempts MUST produce correlated, sanitized security evidence. Successful
  user creation, credential/association changes and initial ownership MUST leave
  durable owner-local security records alongside their state changes. General audit query/export
  remains 009; diagnostic failure MUST NOT turn a failed change into success.
- **FR-018**: Public errors MUST avoid account/setup-claimant enumeration; private
  diagnostics MUST classify operation, outcome and corrective guidance without
  raw credentials, tokens, login codes, session/one-time authorization material,
  private paths/configuration, exception dumps or unnecessary profile data.
  Metrics MUST NOT use user/subject/session values as labels.
- **FR-019**: Login, credential/reset/setup inputs, pending attempts, sessions
  and external requests MUST have finite admitted sizes, lifetimes,
  rate/request budgets and timeouts. External requests MUST contact only
  configured trusted destinations, reject forbidden metadata/link-local targets
  at DNS-answer and connected-peer admission even inside an allowed network,
  and retain normal TLS validation and deliberately approved private-IdP support.
  Operator guidance MUST cover production TLS, proxy/origin trust and secrets.
- **FR-020**: The minimal identity UI MUST present loading, signed-out, signed-in,
  expired, setup-unclaimed/consumed, credential/reset, development and safe
  failure states, preserve only safe local return destinations, and support
  keyboard operation, clear focus/error announcements,
  narrow layouts and enlarged text without exposing protected stale content.
- **FR-021**: The host MUST document a versioned minimal actor/current-identity
  and account-admission contract for trusted consumers and future 006/007
  integration. Authentication method/connection metadata MUST NOT bind a user
  permanently to one production provider or replace central authorization.
  Changed mode-bound actor semantics require explicit version/compatibility
  treatment, not silent reinterpretation of the delivered contract. No new MCP
  tool, job, realtime endpoint or client-credential issuance is required in this slice,
  and none may acquire identity through a future bypass.
- **FR-022**: Delivery MUST include independent configured-provider login,
  negative trust/access tests, real both-provider persistence/concurrency/
  migration/recovery evidence, browser/API parity and accessibility checks,
  captured privacy evidence and the retained applicable 001-004 regression gates.
  Deferred/unavailable checks MUST remain explicit, never relabeled as passes.
- **FR-023**: Operators MUST be able to select local-only, OIDC-only or explicit
  coexistence with named trusted connections. Disabled methods MUST refuse
  authentication; renamed/reconfigured connections MUST NOT reinterpret existing
  associations. Local production accounts MUST retain production TLS, cookie and
  protected-key requirements, not development exemptions. Outage MUST NOT
  automatically enable another method, create credentials or grant ownership.
- **FR-024**: Local account mechanics MUST enforce documented identifier
  uniqueness, credential policy, protected non-recoverable password storage,
  bounded failed-attempt/lockout behavior and safe credential change after fresh
  proof of control. Creation is limited to protected initial setup or trusted
  authorized admission; credentials MUST NOT be returned or logged.
- **FR-025**: Local credential recovery MUST require privately delivered,
  account-bound, expiring one-use reset authorization with non-enumerating
  request/results and no mandatory external mail service. Reset/change MUST
  preserve the internal user and Owner designation, invalidate prior sessions
  for their next protected request and reject replay/wrong-account authorization.
  Reset MUST NOT add credentials to an OIDC-only account.
- **FR-026**: Association/removal of an enabled login method MUST be explicit,
  self-scoped and require fresh proof of control; adding a method also requires
  proof of that method. An already-associated method MUST NOT be reassigned to
  another user or merge accounts. Removal MUST NOT leave the account without
  a usable method; provider outage alone MUST NOT authorize association.
- **FR-027**: After initial ownership, unknown local/OIDC identities MUST NOT
  gain active accounts or sessions without trusted onboarding admission.
  Existing admitted users need no new invitation to sign in. The account
  boundary MUST support 006's authorized invitation/user-administration workflow
  without trusting browser-supplied admission or granting team/role/resource
  permissions itself. Those workflows and granular authorization remain 006,
  not hidden 005 endpoints or a temporary unrestricted Owner bypass.
- **FR-028**: Setup authorization MUST be delivered only through the protected
  installation-operator boundary, kept out of public/logged channels, expire
  within a documented finite lifetime and bind to the initiating setup session
  through local/OIDC completion. Before ownership is consumed, explicit
  replacement MUST invalidate old token/admission material; afterward no
  generation/replacement/recovery operation may reopen ownership.
- **FR-029**: Session validation MUST use fresh durable account/session/security
  state on every protected request. Credential change/reset, authorized account
  disablement, revocation and security-relevant trust changes MUST invalidate
  affected sessions by their next protected request. Periodic credential-stamp
  checks or cross-request positive caches MUST NOT weaken this guarantee.

### Interfaces and Permission Parity

| Surface | Observable boundary |
| ------- | ------------------- |
| Web | Enabled local/OIDC sign-in, protected initial setup, self-service credential/reset/association states, safe identity summary and local sign-out; not the later administration console. |
| Host/API | The same login, setup and self-scoped account/session operations with explicit error semantics; route/serialization details are planning contracts. |
| Trusted module | Read host-established internal actor through a versioned minimal contract, never directly query identity tables or trust client-selected claims. A test-owned consumer proves parity without shipping a new product module. |
| Bootstrap/operator | Private one-time setup-token admission followed by bound successful local/OIDC login; one atomic initial-Owner claim, not public first-arrival promotion or a general administration API. |
| 006 integration | Versioned canonical-user, initial-Owner and authorized account-admission boundary. 006 owns invitation/administration and team/RBAC decisions; 005 enforces account/credential/session invariants. |
| Health | Existing aggregate liveness/readiness stays explicitly anonymous and non-sensitive; new identity details are excluded. Development contract publication retains environment restrictions. |
| MCP/jobs/providers/realtime | No runtime surface added. Future 006/007/013/017/018/019 operations must consume the same actor and centrally enforced permission/scope, not session material or a new identity store. |

Current-user, credential and association operations are self-scoped, not
caller-selected user/team administration. Reset/setup have narrowly scoped
one-use authority, not an ordinary authenticated actor or bearer API credential.
Until 006 supplies granular authorization, unrelated admin/resource operations
remain unavailable/denied; the designation is not a
universal allow flag. Existing restricted maintenance commands retain their
separate operator boundary, not browser-derived authority.

### Data Ownership and Compatibility

Core identity records belong to the host-owned platform store, separate from
optional module data: PostgreSQL's reserved platform schema or the separate
platform SQLite file. This feature creates no module-owned user table or shared
module identity database. Any future relational module still requires its own
immutable registered ID/assignment, collision-safe 63-byte-safe PostgreSQL schema
and schema-local context/history, or canonical restricted-root SQLite file;
no `public`, traversal, `ATTACH`, cross-module joins or registry reassignment.

Identity migrations and their history MUST stay within host ownership and use
explicit stopped preparation and accepted installation authority, not migrations
on ordinary request/login/serving. Both fresh installation and upgrade from the
accepted foundation must be demonstrated. Initial ownership, admitted
account/session changes and their durable security record use one owner-local
transaction; no cross-module ACID or global
rollback is claimed. The host maintains stable user keys rather than letting
optional modules rederive identity from external claims.

The delivered identity format is also an upgrade source, not a disposable
prototype. A new versioned migration must preserve stable user IDs, exact
external associations and consumed bootstrap/designated user, plus all module
assignments/data/history. It must explicitly retire old sessions and unconsumed
nomination material. Do not edit historical migration bytes, relax exact physical
inventories, manufacture local credentials or reassign a user based on email.
No new credential is inferred from old development or OIDC-only records.

Unsupported/corrupt states fail closed, with documented forward
recovery or explicit compatible restore, never silent data loss or bootstrap
reset. Backup/restore includes identity records and security state within the
accepted complete installation set. Secret/session-key handling and stale-backup
risks must be explicit; restoring a backup does not guarantee detection of every
historical privilege rollback or immediate upstream identity revocation.

### Experience, Security and Operations

Login is deliberately small but accessible. Users can distinguish enabled methods,
protected setup, cancelled login, lost/expired session, generic credential/reset
failure, provider/storage unavailability and a development identity without raw
errors or account enumeration. UI state clears on refusal;
hidden controls and friendly messages never replace host enforcement.
Accessibility follows WCAG 2.2 AA for the introduced journey, including keyboard,
focus, announced state changes, 320-CSS-pixel width and 200% text enlargement.

Production local and OIDC profiles require configured trusted origins,
production TLS, protected cookies/keys and documented secrets/proxy handling;
local credentials do not inherit development relaxations. No caller controls
provider discovery locations or login return origins. Invalid provider response, unknown
signing material, identity-store failure and unavailable configuration fail closed.
Existing valid sessions may remain usable during provider outage only within
their admitted local lifetime; no immediate provider-wide revocation or offline
acceptance of new unverifiable login evidence is implied.

Security records distinguish attempted, rejected, committed and uncertain
outcomes. Fixed safe codes and correlation permit investigation without recording
secrets or unnecessary personal data. Failed login/authorization attempts receive
bounded security diagnostics; owner-local durable user/enrollment changes are not
dependent on installing the later audit service. Retention/access policy, admitted
budgets and specific secret/session mechanisms require planning evidence.
Necessary protocol callbacks and protected session transport are not themselves
leaks, but sensitive values must not propagate into rendered pages, application
return destinations, referrers, shared caches or diagnostic captures.

Operations guidance covers enabled connections, private setup-token issuance/
delivery/expiry/replacement, initial local-account creation or OIDC login,
self-service credential/reset/association operations, sign-out/session limits,
secret rotation, explicit upgrade, complete-set backup/restore and Owner/IdP loss.
Reset delivery must have a documented private operator-assisted path without
requiring an external mail vendor; it does not let the operator read passwords
or silently assume the user's identity. General invitation delivery and
administrative account recovery workflows remain 006. Recovery must never
recommend opening anonymous administration, weakening issuer validation or
enabling development login in production. Trusted in-process modules are not
sandboxed against hostile code; a minimal actor contract is not such a promise.

### Key Entities *(if applicable)*

- **Authentication connection**: Operator-selected stable connection identity,
  enabled local/OIDC method, trusted origins/provider and secret references.
  A connection's label is not an external identity key or permission grant.
- **External association**: Verified issuer/subject linked explicitly to one
  canonical user through an admitted connection; profile values are descriptive,
  not keys. Development identity is a separate namespace.
- **Internal user**: Stable opaque host-owned identity with minimal display data;
  independent of local versus OIDC login, not an implicit team member or
  permission grant.
- **Local credential**: Account-bound secret verifier and finite failure/lockout
  state; no recoverable password or implicit credential for an OIDC-only user.
- **Credential/reset authorization**: Narrowly scoped proof for a self-service
  account change, bound to one user and finite/one-use where appropriate; not
  initial ownership or invitation authorization.
- **Account admission**: Trusted authorization for initial setup or later 006
  onboarding, distinct from a login assertion, email match or client-supplied flag.
- **Login attempt/session**: Bounded attempt binding and subsequently established
  local authenticated continuity with finite lifetime and explicit invalidation.
- **Setup admission**: Operator-authorized, expiring one-time installation token
  and bound setup session; unconfigured, pending, replaced/expired and consumed
  are distinct from successful authentication.
- **Initial-Owner designation**: Durable installation-wide designation attached
  atomically to the first successfully authenticated eligible user; immutable
  consumed bootstrap provenance, not a bypass for future 006 permissions.
- **Security record**: Host-owned bounded operation/outcome/correlation and minimal
  actor reference where known; no secret, raw token or profile dump.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user completes production sign-in in local-only, OIDC-only and
  coexistence profiles, observes the same identity in browser and direct
  authenticated access, and signs out on
  both supported persistence profiles; every tested re-entry retains the same
  user despite restart, changed display/email values or an explicitly associated
  alternative method. Every disabled-method attempt is refused.
- **SC-002**: Across at least 20 simultaneous eligible initial-ownership attempts and
  three subsequent fresh host restarts on each supported persistence profile,
  exactly one initial Owner, consumed token/setup authority and durable security
  record remain. Contenders include both local and OIDC identities when both are
  enabled; all unauthorized, losing, replay and later attempts produce zero
  additional Owner grants or unapproved active accounts.
- **SC-003**: Every tested anonymous, forged, expired, wrong-recipient,
  replayed, cross-origin or insufficient-authority request is denied before
  protected data/effects, with consistent browser/direct-access outcomes.
- **SC-004**: Development identities work only in the explicit local profile;
  every tested exposed/non-development/profile-change case admits zero
  development authentications or production administrator grants.
- **SC-005**: Every introduced identity state is keyboard-operable at narrow
  width and enlarged text with no serious/critical automated accessibility
  finding. After final local login/setup input or trusted provider return,
  signed-in or actionable failure state appears within five seconds on a
  documented local test profile;
  human/provider interaction time is measured separately, not called app latency.
- **SC-006**: Clean install, foundation and delivered-identity upgrades,
  interrupted change and corrected recovery preserve all committed
  identity/bootstrap and unrelated module data
  on both supported profiles; old mode-bound sessions are refused after the
  account-model upgrade and consumed ownership never reopens. Uncertain attempts
  never claim known completion.
- **SC-007**: All exercised identity/enrollment outcomes have correlated safe
  evidence, and captured public/private channels disclose zero seeded secret/
  token/authorization canaries outside their required protected authentication
  exchange, and zero unnecessary other-user profile information.
  The documented sign-in/setup/credential/sign-out/correction journeys execute
  against delivered interfaces without an unrecorded manual database edit.
- **SC-008**: Every tested credential creation/change/reset, lockout and explicit
  method-association flow preserves the canonical user and initial designation;
  replay/wrong-account/auto-link attempts have zero credential or authority
  effects, and old sessions are denied on their first request after a committed
  credential change/reset.
- **SC-009**: After setup, every tested unadmitted local/OIDC identity is refused
  account/session creation. Existing users retain sign-in, and Owner designation
  alone authorizes zero unavailable team/RBAC/user-administration operations.

### Required Validation

These are required future evidence, **not checks performed by specification**.
.NET test source lives physically inside its dedicated project under `tests/`,
never in production slices or linked from production projects.
FR-022 applies to the entire matrix. References below target this revision;
historical reports retain their original scenario/requirement meanings.

| Acceptance coverage | Required evidence |
| ------------------- | ----------------- |
| US1.1-6/8; FR-001-004/015/019/023; SC-001/003/005 | Local-only, OIDC-only and coexistence on real SQLite/PostgreSQL; isolated real OIDC protocol interaction, local password verification and browser/direct identity parity, not fabricated principals alone. Exercise disabled methods, exact association uniqueness, changed labels/trust, issuer/recipient/integrity/time/attempt/replay/key/discovery failures, cancellation/outage and deliberate independent method choice. |
| US2.1-6; FR-010-012/017/028; SC-002/006/007 | Both real stores: private issuance, invalid/expired/replaced/foreign-bound/replayed setup authorization, eligible local/OIDC races with at least 20 contenders, commit-boundary interruption and three fresh host restarts. Independently inspect one Owner, consumed token/setup state, session and owner-local security record; losing contenders gain no unapproved active account. Token possession without successful authentication must fail. |
| US3.1-6; FR-024-026/029; SC-008 | Actual local creation, policy/uniqueness/lockout, fresh-proof credential change, private reset delivery/redemption/expiry/replay/wrong-account and explicit method association/removal on both stores. Prove immutable user/Owner identity, no email auto-link/merge, no reset-created password on OIDC-only accounts, final-method protection and next-request invalidation through actual protected requests. Cover concurrent duplicate creation/reset/association and rollback/security evidence. |
| US4.1-6; FR-005-009/021/029; SC-001/003/009 | Host/web/current-user contracts and a separately compiled trusted test consumer; direct/forwarded anonymous and forged access, ordinary versus Owner actor, denied protected operation, CSRF/origin checks, sign-out/idle/absolute expiry, fresh state after credential/security changes, private caches and unchanged health. Version old/new actor and account-admission compatibility explicitly; no production test-only privilege endpoint. |
| US1.7; US3.1; US4.6; FR-007/021/027; SC-009 | Unknown local/OIDC login and forged admission after setup fail with no new active user/session. A trusted test consumer exercises the future authorized account-admission boundary and denied counterpart without shipping 006 UI/RBAC or accepting caller-supplied authorization. Existing users can re-enter; initial Owner cannot bypass missing administration permissions. |
| US5.1-3; FR-013-014; SC-004 | Positive local development and negative environment/bind/origin/unsafe-exposure cases; persisted dev identities/sessions cannot authenticate or claim production ownership, including against production local credentials. |
| US6.1/2/5; FR-003/011/012/015/016; SC-006 | Real clean install, accepted-foundation and delivered-identity upgrades, exact host/module inventory and migration ownership, corrupt/unavailable stores and interruption/complete-set recovery. Seed consumed and unconsumed legacy states; compare stable user IDs, exact external mappings, module data/history/assignments and consumed Owner user ID. Prove old-session/nomination retirement, no credential manufacture, no migration-byte edits and no consumed-ownership reset. |
| US6.3; FR-017-019/028; SC-007 | Browser/host responses, redirects, logs, traces, metrics and durable security-record captures with password/setup/reset/session canaries and truthful commit/uncertainty assertions. Controlled DNS/connected-peer cases must reject forbidden-but-allowlisted and mixed destinations without contacting metadata services; retain approved private fixtures and normal TLS. Exercise finite local/external input/rate/deadline limits. |
| US6.4 and UI parts of US1-5; FR-020; SC-005/007 | Playwright local/OIDC/setup/credential/reset/association/identity/sign-out/expiry/cancel/failure/retry/dev journeys; post-input/return timing, keyboard/focus/announcements, narrow width, enlarged text and automated accessibility. Record actual profiles/results, not an invented user study. |

Retain applicable manifest/storage/composition, module-free bootstrap, health,
web adapter/OpenAPI and complete local/CI gates. Authentication intentionally
adds routes and protected semantics, so downstream contracts/guides and prior
blanket route restrictions must be deliberately reconciled in plan/implementation,
not blindly retained or broadly removed. The safe health contract is retained.
Record exact commands, failures/unavailable checks and all requirement/outcome/
scenario evidence at implementation.

The existing prohibition on testing the 50,000-session boundary remains binding;
do not run, substitute or claim that proof. Preserve existing finite local/
external/maintenance budgets when replanning, including the approved one-second
local persistence envelope. Moving to a standard account framework does not
authorize retries, periodic invalidation or longer deadlines to mask contention.
Preserve review F1/F2 and unchecked T101-T103 until the new plan/tasks explicitly
reconcile the destination-admission defect and pre-fault SQLite enrollment failure.
Historical green reports and the rewritten requirements do not diagnose or close them.

No MCP/event/job/realtime contract or reference-module end-to-end behavior is
introduced here; their tests are inapplicable to this slice, not waived for
007/013/016-020. Full Kubernetes/deployment/reference-module/release proof remains
022-024; this slice requires local both-profile smoke evidence and production
authentication configuration/trust tests, not a claim those releases already ship.
If relevant migration/recovery work encounters 004's deferred failure, capture
the exact assertion and bounded phase evidence before attributing its cause.

## Assumptions and Constitutional Review

**Locked intent:** The human approved ASP.NET Core Identity for account management,
maintained external-protocol handlers, production local-only/OIDC-only/coexistence,
canonical users/shared sessions, explicit associations without email auto-linking,
and setup-token-protected first-login ownership. This is adopted architecture
context, not a package/API/store selection by this specification. Next.js remains
the UI; SAML is a future extension only. Production OIDC and limited development
identity remain required by constitution VI; approved local production accounts
supplement OIDC rather than replacing it or weakening bootstrap safety.

The human then approved reworking 005 against the existing 006 boundary:
005 owns account/authentication/initial-ownership mechanics; 006 owns authorized
invitations, user administration, teams and RBAC. No 006 stage is executed here.

**Requirements-level defaults for clarification:** There is one installation
with multiple teams later, not multiple organizations. After protected initial
setup, new accounts require trusted onboarding admission; valid OIDC proof is
authentication, not an invitation. Existing admitted users need no repeated
invitation. Local password lockout constrains local credential attempts across
all password entry points; it is distinct from an administratively disabled
account, which denies all methods. No automatic method fallback is permitted.
Reset has private operator-assisted delivery without requiring SMTP or a vendor;
general invitation/directory/admin workflows remain 006. Self-service explicit
association proves both existing-account and new-method control; cross-user
merge is excluded. These defaults remain visible for a separate clarification
review and do not invent additional human decisions.

**Planning choices, not unresolved product scope:** exact credential/identifier
policy and normalization, finite lockout rules, reset/setup token custody,
generation/expiry/delivery/session binding/replacement and concurrency behavior;
named connection trust identity and change invalidation; current-user/actor and
account-admission versioned fields; routes, browser/session mechanisms, key
rotation, durations/sizes/rates/timeouts, security-record retention/access and
the new versioned migration/recovery design. These must be explicit and testable
before implementation, within existing finite budgets.

Evaluate standard Identity persistence first against host ownership, strict
physical inventories, explicit migrations, transactional user/session/security
evidence and the one-second SQLite boundary. A narrow persistence adapter is
an alternative only with evidence, not permission to rebuild account managers.
Do not silently choose EF stores, packages, tables, public signatures, token
transport or mail infrastructure here. Dependency provenance/license/lock and
supported-version review remain required.

**Evidence lifecycle:** Original author/reviewer/design/runtime reports and
T001-T103 keep their recorded old-scope meaning. The revised author checklist
assesses this document only. Clarification, replanning, changed quality review,
task reconciliation and cross-artifact analysis must precede implementation;
do not reset driver history or retick old quality markers to manufacture them.
Implementation, convergence, final review and explicit feature acceptance remain
separate later work on the same feature branch/draft PR.

| Principle | Requirement/application |
| --------- | ----------------------- |
| I - Product/brand | Core identity works without optional modules; Köfte prose, `kofte` identifiers; no Hoard dependency. |
| II - Modularity | FR-007/016/021 retain trusted module lifecycle and a minimal shared actor, not competing login systems or a sandbox. |
| III - VSA | Host-owned identity slices, native DI/direct handlers and minimal shared contracts; dedicated test-project ownership; no MediatR/AutoMapper or speculative layering. Actual dependency/license review remains implementation. |
| IV - Storage | FR-003/011/016 retain real-provider host/module separation, accepted registry/history/path rules and explicit upgrade from the delivered identity format, not a second account database. |
| V - Transactions | Initial account/session/Owner/consumption/security evidence share the host-owner commit; credential/reset/association changes require honest owner-local effects, never cross-module ACID or automatic global rollback. |
| VI - Identity/safety | FR-001-029 require production local/OIDC, separate limited development, safe non-repeatable first-login ownership, no auto-link/open admission, next-request invalidation, default denial and bounded secret/egress protections. 006 still owns central permissions. |
| VII - MCP | FR-021 provides an actor integration boundary, not an anonymous tool/server; 007/013 still require the same identity/permission model and actual protocol tests. |
| VIII - Durable work/realtime | No jobs/events/providers/SignalR runtime added; persistent setup/account/session outcomes are local identity state, not a workflow engine or reliable-event guarantee. |
| IX - Frontend | FR-020/SC-005 require minimal accessible consistent login states in the existing web foundation; full 008 design system and trusted build-time extensions remain later. |
| X - Portability | Both supported stores, production local-only without an IdP, vendor-neutral OIDC and explicit production/development protections; no mandatory Azure/Kubernetes/mail/identity-server service. |
| XI - Evidence | FR-022, SC-001-009 and Required Validation map all 34 scenarios and 29 requirements to real-store/local/protocol/browser/privacy/regression evidence; no inherited failed check or prohibited capacity boundary becomes a pass. |
| XII - Traceability | Approved 005 amendment and explicit handoff to existing 006, accepted 001/004 prerequisites, later-feature exclusions, preserved stage history and separate review/acceptance; not MVP completion. |
