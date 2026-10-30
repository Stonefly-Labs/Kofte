# Feature Specification: 005 - Authentication and Bootstrap Administration

**Feature Branch**: `feature/005-authentication-bootstrap`
**Feature Directory**: `specs/005-authentication-bootstrap`
**Created**: 2026-10-26
**Status**: Draft
**Constitution**: 2.0.1; principles I-XII assessed below
**Scope rework (2026-10-30):** The human approved ASP.NET Core Identity,
production local/OIDC accounts and first-successful-login ownership with
owner-led onboarding/RBAC. The [ADR amendment](../../docs/adr/0007-authentication-bootstrap-boundary.md#human-approved-scope-amendment-local-accounts-and-first-login-ownership)
records the decisions, including the subsequently approved one-time installation
setup token for initial-claim admission.
**Implementation is blocked.** The requirements/scenarios below retain the
original OIDC-only baseline, including its now-superseded exclusions and
nominated bootstrap; they must be reconciled and reviewed before new code.
No existing checklist, analysis or acceptance result certifies the new scope.

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
operator can connect a supported identity provider, deliberately establish the
first administrator, and let a user enter and leave Köfte with one trustworthy
identity across the browser and host.

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
Actors are the installation operator, nominated first administrator, ordinary
signed-in user, anonymous/forged client and trusted module author. The installation
is one organization with future multi-team scoping; login grants no team
membership, resource access or automatic administration.

**Included:** configurable provider-neutral OpenID Connect (OIDC) sign-in;
explicit development-only sign-in; durable external-to-internal identity mapping;
deliberate, one-time first-admin enrollment; browser session, current-identity and
API access boundaries; sign-out/expiry; accessible minimal identity UI; safe
failure/recovery and correlated security evidence on both storage providers.

**Excluded:** local production passwords, account registration/password recovery,
account linking/merging, user-directory synchronization, multiple organizations,
additional non-OIDC providers (030), teams/roles/granular resource authorization
(006), MCP server/client authorization flows and tools (007/013), full UI shell
(008), general audit browsing/export (009), module settings administration (015),
machine/service-principal credential issuance, jobs/events/realtime, frontend
extensions, module marketplace, AI chat and external MCP federation.
The first-admin designation is an installation-level seed for 006, not a bypass
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

### User Story 1 - Enter Köfte with a trusted identity (Priority: P1)

As a user, I can sign in through my installation's configured provider and see
who I am in Köfte without handing provider credentials to the portal.

**Why this priority**: Every subsequent user-facing feature needs a trustworthy
actor, and the production path must not depend on a development shortcut.
**Independent Test**: Use an isolated configured OIDC provider and the browser
against each supported store; observe successful login and matching host
identity, then challenge the provider trust boundary.

**Acceptance Scenarios**:

1. **Given** a valid production OIDC configuration and an anonymous browser,
   **When** a user completes provider login, **Then** Köfte establishes one
   authenticated internal identity, displays its safe identity summary, and
   reports the same identity through the authenticated current-user API.
2. **Given** an existing external issuer/subject mapping, **When** that user
   signs in again after host restart or a display-name/email change, **Then**
   the same internal user is returned with unchanged administrative designation.
3. **Given** two external subjects with the same email/display name or the same
   subject at different trusted issuers, **When** they sign in, **Then** their
   internal identities remain distinct and neither inherits the other's access.
4. **Given** invalid, expired, replayed, unsolicited or wrongly addressed login
   evidence, **When** it reaches the login boundary, **Then** no authenticated
   session or privileged identity is created; the user gets a safe retry state
   and the operator gets a correlated rejection.
5. **Given** a provider outage, user cancellation or invalid production
   configuration, **When** login is attempted or the host starts, **Then** no
   development/anonymous fallback grants access; a bounded, sanitized failure
   identifies the corrective action without an endless redirect loop.

### User Story 2 - Deliberately establish the first administrator (Priority: P1)

As the installation operator, I can nominate and enroll the first administrator
without making the first visitor, email match or fastest login privileged.

**Why this priority**: Accidental or repeatable bootstrap would compromise every
later administration boundary.
**Independent Test**: On fresh stores, explicitly authorize a nominated verified
identity, race eligible enrollments, interrupt completion and restart; inspect
the durable designation and consumed bootstrap state.

**Acceptance Scenarios**:

1. **Given** an uninitialized installation without an operator-authorized
   bootstrap ceremony, **When** any user logs in or submits a bootstrap request,
   **Then** nobody becomes administrator and public responses disclose no
   bootstrap credential or other user's identity.
2. **Given** an explicit operator-authorized ceremony bound to a nominated
   external issuer/subject and a valid login as that identity, **When** its
   one-time authorization is completed, **Then** exactly that internal user is
   durably designated first administrator and the ceremony is consumed.
3. **Given** that ceremony, **When** another identity, forged client, expired
   authorization or replay attempts enrollment, **Then** no designation changes;
   the rejection does not reveal the nominee or usable authorization material.
4. **Given** concurrent authorized attempts or interruption before/after the
   enrollment commit, **When** callers retry or the host restarts, **Then**
   exactly one designation and consumed state persist together, or neither
   commits; no second administrator is minted and no success is reported for
   an uncertain result.
5. **Given** completed bootstrap, **When** configuration is replayed, users sign
   in, the host upgrades/restarts or the administrator is unavailable, **Then**
   automatic bootstrap stays closed; recovery requires documented explicit
   operator action, not deleting identity data or promoting the next login.

### User Story 3 - Keep browser and API access within the same boundary (Priority: P1)

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
   identity, **Then** they agree on internal user, identity mode and first-admin
   designation; spoofed headers, request fields or changed profile claims cannot
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

### User Story 4 - Develop locally without a production backdoor (Priority: P2)

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
   production first-admin designation.

### User Story 5 - Operate and recover identity safely (Priority: P2)

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
   persistence, **When** login, enrollment or current-identity verification
   requires it, **Then** the operation fails closed with a truthful bounded
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

### Edge Cases

- Duplicate callbacks, multiple browser tabs, simultaneous first login for one
  subject and concurrent enrollment must not create duplicate users/privilege.
- Issuer/subject are trusted identity keys; missing or malformed keys, renamed
  provider labels, reused email, mixed-case/display variants and changed provider
  configuration must not silently relink accounts.
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

- **FR-001**: The host MUST support production login through operator-configured,
  standards-conforming OIDC without requiring a particular vendor, cloud,
  optional module or local production password. Provider credentials stay with
  the provider; only validated authentication evidence establishes a user.
- **FR-002**: Login MUST validate configured issuer, intended recipient, integrity,
  validity period and binding to the initiating browser/attempt; reject replay,
  unsolicited/forged evidence and incompatible credential types before creating
  a session. Client-supplied identity/privilege claims are not authoritative.
- **FR-003**: The host MUST durably map the verified external issuer/subject pair
  to one stable opaque internal user, including concurrent first login and
  restart. Profile changes MUST NOT change that key; distinct external identities
  MUST NOT merge automatically through email, display name or provider label.
- **FR-004**: The browser and authenticated current-user API MUST expose the same
  minimal internal identity, safe display information, identity mode and local
  first-admin designation, without provider tokens or other users' data.
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
  overrides. Login and first-admin designation MUST NOT manufacture team
  membership or bypass future registered action/team/resource checks.
- **FR-008**: Browser authentication MUST resist session fixation, credential
  theft through browser-readable session material, cross-site request forgery
  and unauthorized cross-origin credential use. Sensitive identity responses
  MUST NOT be shared-cached or exposed through unsafe redirect destinations.
- **FR-009**: Sessions MUST have documented finite idle/absolute lifetimes and
  safe renewal/restart/secret-rotation behavior. Local sign-out and expiry MUST
  invalidate the affected session for its next protected request and clear
  stale browser identity; provider-wide sign-out is not promised.
- **FR-010**: First-admin enrollment MUST be disabled without explicit operator
  authorization, bound to a nominated verified external issuer/subject and
  guarded by expiring one-time authorization. First arrival, email match,
  arbitrary profile claims and a development login MUST NOT qualify.
- **FR-011**: Successful enrollment MUST atomically persist exactly one initial
  administrator designation and consumed bootstrap state in the same owner
  store. Concurrency, interrupted replies and restart/retry MUST NOT create
  additional administrators, partial committed enrollment or replayable authority.
- **FR-012**: Completed enrollment MUST remain closed across ordinary restart,
  upgrade, configuration replay and administrator unavailability. Explicit
  operator correction/recovery MUST be documented; no public bootstrap reset,
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
  committed users, consumed enrollment and unrelated module assignments/data.
- **FR-017**: Sensitive identity changes and rejected authentication/authority
  attempts MUST produce correlated, sanitized security evidence. Successful
  user creation and administrator enrollment MUST leave durable owner-local
  security records alongside their state changes. General audit query/export
  remains 009; diagnostic failure MUST NOT turn a failed change into success.
- **FR-018**: Public errors MUST avoid account/nominee enumeration; private
  diagnostics MUST classify operation, outcome and corrective guidance without
  raw credentials, tokens, login codes, session/one-time authorization material,
  private paths/configuration, exception dumps or unnecessary profile data.
  Metrics MUST NOT use user/subject/session values as labels.
- **FR-019**: Login and callback inputs, pending attempts, sessions and external
  provider requests MUST have finite admitted sizes, lifetimes, rate/request
  budgets and timeouts. Only configured trusted provider destinations may be
  contacted; operator guidance MUST cover TLS, proxy/origin trust and secrets.
- **FR-020**: The minimal identity UI MUST present loading, signed-out, signed-in,
  expired, development and safe failure states, preserve only safe local return
  destinations, and support keyboard operation, clear focus/error announcements,
  narrow layouts and enlarged text without exposing protected stale content.
- **FR-021**: The host MUST document a versioned minimal actor/current-identity
  contract for trusted consumers and future 006/007 integration. No new MCP tool,
  job, realtime endpoint or client-credential issuance is required in this slice,
  and none may acquire identity through a future bypass.
- **FR-022**: Delivery MUST include independent configured-provider login,
  negative trust/access tests, real both-provider persistence/concurrency/
  migration/recovery evidence, browser/API parity and accessibility checks,
  captured privacy evidence and the retained applicable 001-004 regression gates.
  Deferred/unavailable checks MUST remain explicit, never relabeled as passes.

### Interfaces and Permission Parity

| Surface | Observable boundary |
| ------- | ------------------- |
| Web | Minimal sign-in entry, safe provider handoff/return, identity/mode summary and local sign-out; not the later full console shell. |
| Host/API | Login protocol boundaries, authenticated current identity and sign-out with explicit error semantics; route/serialization details are planning contracts. |
| Trusted module | Read host-established internal actor through a versioned minimal contract, never directly query identity tables or trust client-selected claims. A test-owned consumer proves parity without shipping a new product module. |
| Bootstrap/operator | Explicit protected installation ceremony bound to the nominated verified identity; neither a general anonymous administration API nor automatic first-login promotion. |
| Health | Existing aggregate liveness/readiness stays explicitly anonymous and non-sensitive; new identity details are excluded. Development contract publication retains environment restrictions. |
| MCP/jobs/providers/realtime | No runtime surface added. Future 006/007/013/017/018/019 operations must consume the same actor and centrally enforced permission/scope, not session material or a new identity store. |

Current-user and sign-out operations are scoped to the calling session/user,
not a caller-specified user/team. Bootstrap authorizes only its nominated
installation administrator. Until 006 supplies granular authorization, unrelated
admin/resource operations remain unavailable/denied; the designation is not a
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
accepted foundation must be demonstrated. Enrollment state and its durable
security record use one owner-local transaction; no cross-module ACID or global
rollback is claimed. The host maintains stable user keys rather than letting
optional modules rederive identity from external claims.

Data model/version changes must preserve issuer/subject uniqueness and consumed
bootstrap. Unsupported/corrupt states fail closed, with documented forward
recovery or explicit compatible restore, never silent data loss or bootstrap
reset. Backup/restore includes identity records and security state within the
accepted complete installation set. Secret/session-key handling and stale-backup
risks must be explicit; restoring a backup does not guarantee detection of every
historical privilege rollback or immediate upstream identity revocation.

### Experience, Security and Operations

Login is deliberately small but accessible. Users can distinguish cancelled
login, lost/expired session, provider/storage unavailability and a development
identity without raw errors or account enumeration. UI state clears on refusal;
hidden controls and friendly messages never replace host enforcement.
Accessibility follows WCAG 2.2 AA for the introduced journey, including keyboard,
focus, announced state changes, 320-CSS-pixel width and 200% text enlargement.

Production requires configured trusted origins/provider, TLS for non-local
traffic and documented secrets/proxy handling. No caller controls provider
discovery locations or login return origins. Invalid provider response, unknown
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

Operations guidance covers initial configuration, enrollment, correction of an
unconsumed ceremony, sign-out/session limits, secret rotation, explicit upgrade,
complete-set backup/restore and administrator/IdP loss. Recovery must never
recommend opening anonymous administration, weakening issuer validation or
enabling development login in production. Trusted in-process modules are not
sandboxed against hostile code; a minimal actor contract is not such a promise.

### Key Entities *(if applicable)*

- **Authentication configuration**: Operator-selected identity mode, trusted
  provider/origins and secret references; no user-controlled provider selection.
- **External identity**: Validated issuer and subject in a production or explicitly
  separate development namespace; profile values are descriptive, not keys.
- **Internal user**: Stable opaque host-owned identity with minimal display data;
  not an implicit team member or permission grant.
- **Login attempt/session**: Bounded attempt binding and subsequently established
  local authenticated continuity with finite lifetime and explicit invalidation.
- **Bootstrap ceremony**: Operator-authorized, expiring, single-use enrollment for
  a nominated verified external identity; unconfigured, pending and consumed are
  distinct from a user's current login state.
- **Initial administrator designation**: Durable installation-level seed attached
  to exactly one enrolled internal user; future roles/permissions remain 006.
- **Security record**: Host-owned bounded operation/outcome/correlation and minimal
  actor reference where known; no secret, raw token or profile dump.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user completes the configured production sign-in, observes the
  same identity in browser and direct authenticated access, and signs out on
  both supported persistence profiles; every tested re-entry retains the same
  user despite restart or changed display/email values.
- **SC-002**: Across at least 20 simultaneous eligible enrollment attempts and
  three subsequent fresh host restarts, exactly one initial administrator and
  one consumed ceremony remain; all unauthorized, replay and later enrollment
  attempts produce zero additional designations.
- **SC-003**: Every tested anonymous, forged, expired, wrong-recipient,
  replayed, cross-origin or insufficient-authority request is denied before
  protected data/effects, with consistent browser/direct-access outcomes.
- **SC-004**: Development identities work only in the explicit local profile;
  every tested exposed/non-development/profile-change case admits zero
  development authentications or production administrator grants.
- **SC-005**: Every introduced identity state is keyboard-operable at narrow
  width and enlarged text with no serious/critical automated accessibility
  finding. After the trusted login return is received, signed-in or actionable
  failure state appears within five seconds on a documented local test profile;
  human/provider interaction time is measured separately, not called app latency.
- **SC-006**: Clean install, foundation upgrade, interrupted change and corrected
  recovery preserve all committed identity/bootstrap and unrelated module data
  on both supported profiles; uncertain attempts never claim known completion.
- **SC-007**: All exercised identity/enrollment outcomes have correlated safe
  evidence, and captured public/private channels disclose zero seeded secret/
  token/authorization canaries outside their required protected authentication
  exchange, and zero unnecessary other-user profile information.
  The documented sign-in/enroll/sign-out/correction journeys execute against
  delivered interfaces without an unrecorded manual database edit.

### Required Validation

These are required future evidence, **not checks performed by specification**.
.NET test source lives physically inside its dedicated project under `tests/`,
never in production slices or linked from production projects.

| Acceptance coverage | Required evidence |
| ------------------- | ----------------- |
| US1.1-5; FR-001-004/015/019; SC-001/003/005 | Real protocol interaction with an isolated configured OIDC provider, browser redirect/return and direct identity verification, not a fabricated authenticated principal alone. Exercise issuer/recipient/integrity/time/attempt/replay/key/discovery failures, cancellation, outage and corrected retry. |
| US2.1-5; FR-010-012/017; SC-002/006/007 | Both real providers: unconfigured/mismatched/expired/replayed enrollment, at least 20 simultaneous eligible attempts, interruptions at commit boundaries and three fresh host restarts. Independently inspect one durable designation, consumed state and owner-local security record; preserve no-success-on-uncertainty proof. |
| US3.1-5; FR-005-009/021; SC-001/003 | Host/web-adapter/current-user contracts and a separately compiled test-owned module consumer; direct/forwarded anonymous and forged requests, ordinary versus admin identity, denied protected operation, request forgery/origin checks, sign-out/idle/absolute expiry, private-cache and unchanged aggregate health cases. No production test-only privilege endpoint. |
| US4.1-3; FR-013-014; SC-004 | Positive local dev login and negative environment/bind/origin/unsafe-exposure cases; persisted development identity/session versus production identity and bootstrap after restart/profile change. |
| US5.1-2; FR-003/011/012/015/016; SC-006 | Real SQLite/PostgreSQL clean install and upgrade from accepted stores, migration/history ownership, concurrent identity uniqueness, interruption, unavailable/corrupt storage and explicit complete-set recovery; compare exact prior module assignment/data sentinels and consumed bootstrap. No in-memory provider substitutes. |
| US5.3; FR-017-019; SC-007 | Captures of browser/host responses, redirects, logs, traces, metrics and durable security records; secret/identity canaries, safe category/correlation and truthful commit/uncertainty assertions. Invalid inputs and provider request/rate/time budgets require focused negative tests. |
| US5.4 and UI parts of US1/3/4; FR-020; SC-005/007 | Playwright login/identity/sign-out/expiry/cancel/failure/retry/dev-mode journeys, timing after provider return, keyboard/focus/announcements, narrow width, enlarged text and automated accessibility checks. Record actual profile and results; do not invent a user study. |

Retain applicable manifest/storage/composition, module-free bootstrap, health,
web adapter/OpenAPI and complete local/CI gates. Authentication intentionally
adds routes and protected semantics, so downstream contracts/guides and prior
blanket route restrictions must be deliberately reconciled in plan/implementation,
not blindly retained or broadly removed. The safe health contract is retained.
Record exact commands, failures/unavailable checks and all requirement/outcome/
scenario evidence at implementation.

No MCP/event/job/realtime contract or reference-module end-to-end behavior is
introduced here; their tests are inapplicable to this slice, not waived for
007/013/016-020. Full Kubernetes/deployment/reference-module/release proof remains
022-024; this slice requires local both-profile smoke evidence and production
authentication configuration/trust tests, not a claim those releases already ship.
If relevant migration/recovery work encounters 004's deferred failure, capture
the exact assertion and bounded phase evidence before attributing its cause.

## Assumptions and Constitutional Review

**Requirements-level defaults:** one operator-configured production OIDC trust
configuration is sufficient to prove pluggability without requiring several
providers simultaneously or a vendor-specific product. Internal identity keys
are issuer/subject, not email. The operator explicitly nominates the initial
administrator and authorizes enrollment; there is no auto-admin claim mapping,
production development shortcut or self-service relinking. Local browser session
access is required; separate machine credential issuance is not. Host-owned
minimal security records satisfy this slice without pulling general 009 audit
administration forward. These defaults are visible for the separate clarification
review and do not represent invented human approval.

**Planning choices, not unresolved product decisions:** exact provider fixture/
library, login/session flow and protection, API credential representation,
current-user/actor versioned fields, route/origin topology, enrollment ceremony
delivery, secret references/rotation, finite durations/sizes/rates/timeouts,
security-record retention/access and host storage migration design. Consequential
choices require an ADR and constitutional checks; no library, algorithm, table
layout or public signature is adopted by this specification.

| Principle | Requirement/application |
| --------- | ----------------------- |
| I - Product/brand | Core identity works without optional modules; Köfte prose, `kofte` identifiers; no Hoard dependency. |
| II - Modularity | FR-007/016/021 retain trusted module lifecycle and a minimal shared actor, not competing login systems or a sandbox. |
| III - VSA | Host-owned identity slices, native DI/direct handlers and minimal shared contracts; dedicated test-project ownership; no MediatR/AutoMapper or speculative layering. Actual dependency/license review remains implementation. |
| IV - Storage | FR-003/011/016 and Data Ownership retain real-provider host/module separation, accepted registry/history/path rules and explicit migration evidence. |
| V - Transactions | Enrollment/designation/security record are one host-owner transaction; no cross-module reads/ACID or automatic global rollback. |
| VI - Identity/safety | FR-001-019 require production OIDC, limited development, safe non-repeatable bootstrap, default denial, secret protection and bounded trusted external requests; granular 006 remains a separate obligation. |
| VII - MCP | FR-021 provides an actor integration boundary, not an anonymous tool/server; 007/013 still require the same identity/permission model and actual protocol tests. |
| VIII - Durable work/realtime | No jobs/events/providers/SignalR runtime added; persistent enrollment/session outcomes are local identity state, not a workflow engine or reliable-event guarantee. |
| IX - Frontend | FR-020/SC-005 require minimal accessible consistent login states in the existing web foundation; full 008 design system and trusted build-time extensions remain later. |
| X - Portability | Both supported stores, provider-neutral OIDC, explicit local versus production TLS/secrets/recovery; no mandatory Azure/Kubernetes/managed identity service. |
| XI - Evidence | FR-022, SC-001-007 and Required Validation map all 22 scenarios and requirements to real provider/protocol/browser/privacy/regression evidence; no inherited failed check becomes passing. |
| XII - Traceability | Inventory 005 only, accepted 001/004 prerequisites, explicit later-feature exclusions, one stage/branch/PR and subsequent human review/acceptance; not MVP completion. |
