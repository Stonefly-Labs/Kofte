# Research: Revised 005 Account and Ownership Design

**Date:** 2026-10-30. **Status:** Planning decisions, not implementation proof.
**Input:** Revised [specification](spec.md), clarified without questions at
`f10351a`. Decisions A1-A12 below supersede the old design, not its evidence.
Historical R1-R12 retain their original meaning in the
[pre-rework snapshot](https://github.com/Stonefly-Labs/Kofte/blob/f10351a599191a5931b05a719cd8d291eba839fb/specs/005-authentication-bootstrap/research.md).
The human approved the architecture direction; these exact technical choices
remain proposed for design review under ADR 0007.

## Evidence and limits

Read the actual platform migration runner, frozen native-catalog fingerprints,
`IdentityStore`, `DeadlineStore`, preparation/recovery code, mode-bound account
and session stores, `ValidatedLogin`, cookie bridge and actor-v1. In particular:

- `IdentityStore.cs:47-70` clips the whole owner mutation to one second and
  owns its commit; manager/store calls cannot create independent commits.
- `DeadlineStore.cs:39-99,130-190,237-255` reserves cleanup, uses SQLite-owned
  handles with native busy waits disabled, interrupts VM work and retries only
  identified known-not-committed boundaries. PostgreSQL uses cancellable commands
  and unpooled connection cleanup.
- `RegistryMigrationRunner.cs:17-52,166-180` and
  `RegistryPhysicalFormat.cs:11-27` recognize exact formats/history, not merely
  a table-name prefix. Old format-1/2 migration bytes and fingerprints stay frozen.
- `IdentityTicketStore.cs:54-87,104-125` attaches an already committed session
  and checks durable state on each request. Preserve that boundary, not a second
  Identity cookie/session issuer.

One bounded research agent inspected official Identity/EF source using Microsoft
Learn and Context7, including shared-framework membership and EF customization;
parent research used the connected Microsoft Learn search/fetch tools
for SQLite's documented synchronous execution and automatic busy retries.
Context7 resolved ASP.NET Core and was queried at available version `10.0.12`;
its returned session/sign-in API snippets did not establish the requested
Identity integration or the exact pinned `10.0.13` behavior. Do not treat that
response, default-branch source, or API package metadata as pinned runtime proof.
Official sources below establish extension points; implementation must prove
the actual locked framework/store integration. No package was installed, code
prototype run, or production/50,000-session capacity proof performed.

## A1 - Evaluate EF stores first; select the existing bounded executor

**Decision:** Use framework account managers with a narrow, coordinator-bound
custom persistence adapter on both providers. Put the ASP.NET Identity adapter
in the existing Web Host; infrastructure keeps framework-neutral account records
and the existing provider executor. No new assembly or identity database.

**Rationale:** Standard EF Identity stores can participate in a caller-owned
transaction. `UserOnlyStore.AutoSaveChanges=false` can defer persistence to an
explicit coordinator flush; their default table names/model are customizable.
Neither SQL ownership nor reviewed manual migrations alone disqualifies EF.
However, disabling automatic saves does not replace EF's Microsoft.Data.Sqlite
command/busy path with the existing native executor, provide its cleanup reserve,
or prove the one-second total under contention. Mixing EF's managed commands
with native transaction management is not the documented shared-transaction path.
The existing executor already supplies the required local transaction boundary.
One adapter for each required store interface, with provider SQL below it, is
smaller than a new EF execution provider or different account semantics per DB.

**Alternatives considered:** A shared EF context/explicit transaction is viable
in less restrictive applications and could be reconsidered only with proof of
the unchanged deadline and cleanup contract. Separate default Identity commits
split account/session/Owner/evidence atomicity. A new SQLite identity file,
custom EF provider, or new account manager is rejected. This selection is a
design decision, not a claim that every possible EF solution is impossible.

Sources: [custom stores][stores], [EF model customization][model],
[EF transactions][transactions], [SQLite async][sqlite-async],
[SQLite errors/timeouts][sqlite-errors], [EF user store][ef-store].

## A2 - Adopt account management, not just a password hasher

**Decision:** Register `AddIdentityCore<IdentityAccount>()`, the custom
`IUserStore<IdentityAccount>`, `AddSignInManager()` and a dedicated
`DataProtectorTokenProvider<IdentityAccount>` for password reset.
Implement `IUserPasswordStore`, `IUserSecurityStampStore`, `IUserLockoutStore`
and `IUserLoginStore` alongside `IUserStore`. Keep registration in the Host's
existing ASP.NET Core framework boundary; do not adopt the EF Identity package.

Use unmodified `UserManager` creation/password/change/reset/login-association
operations, validators, normalizer, hasher and lockout mechanics.
Use `SignInManager.CheckPasswordSignInAsync(..., lockoutOnFailure:true)` for
password authentication/fresh local proof, never `PasswordSignInAsync`,
`ExternalLoginSignInAsync`, Identity cookie issuance or `MapIdentityApi`.
The existing committed-ticket bridge alone publishes the application cookie.

**Rationale:** The check-only sign-in path applies framework lockout and may
persist failure counts, reset counts or a password rehash, but does not publish
a cookie. All such writes must use the same scoped owner transaction.
Expected credential denial commits required lockout/security evidence and no
session; exceptions roll back. Never turn a storage failure into bad credentials.
Record any failed store write in the owner scope and refuse its commit even if
a framework check ignores an update result during rehash; manager success alone
is not proof that all underlying writes succeeded.
No email, role, claims, phone, passkey or two-factor store is needed by these
selected calls. Disable corresponding confirmation/2FA features; custom session
claims come from durable state, not the default Identity principal factory.

**Alternatives considered:** Bare `CheckPasswordAsync` without lockout sequencing
misses account behavior; rebuilding managers violates approved intent. Default
Identity API endpoints add unwanted bearer/registration surfaces. Razor UI,
an external identity server and Identity roles are unnecessary.

Sources: [Identity services][registration], [UserManager][manager],
[SignInManager][signin], [token provider][reset-provider].

## A3 - One provider-independent user with explicit methods

**Decision:** Users have stable GUIDs and production/development context, not an
OIDC/local user type. Identity's username is the local login name when present;
external-only users get private reserved `kofte-external-<GUID-N>` names.
Development uses the separately reserved `kofte-development-<GUID-N>` form.
Neither generated name is a usable local credential. The first explicit local
association may replace the generated name; thereafter the local name remains
reserved to that user, including after password removal. No rename API in 005.

Local names are 3-64 ASCII letters/digits/`.`/`_`/`-`, without surrounding
whitespace or either reserved prefix (case-insensitive). Use Identity's invariant
normalizer and a unique normalized-name index; reject rather than trim.
Passwords are 12-128 UTF-16 code units, valid Unicode without controls, at most
512 UTF-8 bytes; spaces are allowed and values are never trimmed/normalized.
Configure framework length validation without composition requirements, Identity
V3 hashing at 210,000 iterations, and reject oversized input before hashing.
Five failed checks lock the local credential for fifteen minutes.
All local password entry points share that account lockout. Account disablement
instead denies every method; neither mechanism changes Owner provenance.

**Rationale:** Standard manager behavior plus finite input/work policy avoids
email-as-identity, duplicate user types and provider-derived permissions.
Verify measured hash/rehash and concurrency performance under the retained
one-second transaction, including hash work; no off-budget background commit or
deadline increase is authorized if proof fails.

**Alternatives considered:** Email matching, implicit passwords for external
users, profile-based merges, custom hashing and production Development mode
are rejected. Username change, MFA and breach-list services are not added.

## A4 - Named connections do not redefine external identity

**Decision:** Persist immutable connection GUID, kind, exact issuer where
applicable, enabled state and checked trust revision. Labels are descriptive.
Maintain globally unique exact production `(issuer,subject)` ownership and
separate explicit connection-login bindings. Identity's `LoginProvider` is the
connection GUID and `ProviderKey` is the exact subject. Two connections at the
same issuer cannot attach the same external identity to different users.
No new connection auto-links even when its issuer/subject already exists.

**Rationale:** Connection bindings allow multiple clients/connections without
silently retargeting old associations. An issuer/kind cannot be changed under an
existing connection ID. Disable/retire it and explicitly associate a new one.
Security-relevant config changes require stopped reconciliation and advance the
installation session epoch (conservative all-session invalidation); label/secret
rotation alone does not. No cached positive session admission survives that check.

**Alternatives considered:** A single global provider or `(connection,subject)`
uniqueness alone leaves coexistence/duplicate-issuer ownership gaps. SAML gets
only a future versioned verified-method seam, not a parser/package/flow now.

## A5 - Setup admission is not authentication

**Decision:** Stopped authorization privately writes a random 256-bit setup
token (43 unpadded base64url characters), with a fifteen-minute lifetime.
Store only its installation/generation-bound digest. Protected submission
creates a five-minute setup admission, clipped to token expiry, bound to an
opaque Secure HttpOnly cookie and pre-login browser binding.

Multiple valid admissions can contend while the token is pending. The first
eligible local/OIDC completion locks the singleton, checks current generation/
epoch/admission and authenticates before atomically creating/admitting the user,
consuming attempt/admission/token, setting Owner, storing session and evidence.
Invalidate every other admission by the consumed singleton. Losing unknown users
are never committed. Failed authentication does not consume setup authority.
Replacement/disable before consumption invalidates every old admission/attempt.

**Rationale:** This implements the approved first-login rule without public
first-arrival promotion or a token that itself authenticates. Twenty contenders,
mixed methods and three restarts remain required on both providers.

**Alternatives considered:** Nomination is superseded; separate enrollment after
publishing an ordinary session splits the required commit. No token in URLs,
console output, rendered HTML, analytics or browser storage.

## A6 - Password reset uses framework tokens plus durable consumption

**Decision:** Use the installation-bound Data Protection provider with a
dedicated reset-provider name and fifteen-minute token lifetime.
`GeneratePasswordResetTokenAsync` supplies the private token; retain only a
digest, user, stamp/generation and expiry in one pending authorization per user.
A stopped private-file operator command delivers it without SMTP. Public reset
requests return the same explicit operator-assistance instruction without account
lookup, promised email or an invented delivery queue.

Redemption requires an already-local enabled production account, exact pending
digest, framework token validation and `ResetPasswordAsync`. Credential/stamp
update, reset consumption, account security-version increment, session/proof
invalidation and security record commit together. No automatic login.
Successful reset clears local failed-count/lockout through framework APIs in
that commit; it does not enable an administratively disabled account.
Password change similarly uses fresh local proof and `ChangePasswordAsync`;
invalid old-password outcomes use framework failed-attempt accounting.

**Rationale:** A Data Protection token binds user/purpose/time/security stamp,
but has no durable used bit. Stamp rotation alone is not concurrent one-use
proof without the owner transaction. Explicit consumption also supports
replacement and honest delivery/commit uncertainty. Private operator access is
trusted recovery authority, never password retrieval or an impersonation API.

**Alternatives considered:** Stateless reset without transactional consumption,
logged links, compulsory mail vendors and reset-created OIDC-only passwords
are rejected. General administrative recovery/invitation workflows remain 006.

## A7 - Fresh proof and explicit association

**Decision:** A check-only local password challenge or OIDC reauthentication
creates a five-minute, single-use, session/user/security-version-bound proof.
OIDC requests `prompt=login`, `max_age=0` and verifies signed fresh `auth_time`
and the exact existing association; a prompt parameter alone is not proof.
Adding another OIDC method binds a second protocol attempt to that proof,
current session, target user and connection. Adding local credentials requires
fresh existing-account proof plus the new valid password/name.

**Rationale:** `UserManager.AddLoginAsync`/password APIs run only after both
proofs and conflict checks. Removal requires fresh proof and at least one
remaining enabled usable method. A network outage is not removal permission.
Successful credential/association mutation invalidates prior sessions/proofs,
including the current one; the UI deliberately asks for a new login.

**Alternatives considered:** An old cookie, matching email or provider outage
is insufficient. Cross-user linking/merge and removal of the final method fail
without effects. Providers unable to supply required fresh evidence fail safely;
an independently available existing local method may prove control.

## A8 - Version the actor and deny account admission by default

**Decision:** HTTP/actor v2 uses production/development context and
`isInitialOwner`, not an extended v1 mode enum. Retain delivered v1 contract files
and type definitions; retire their HTTP operations with a safe 410 and remove
the v1 actor-service registration. Identity-aware consumers must adopt v2;
requiring the retired DI service fails boundedly through existing lifecycle/
service-resolution handling, never an anonymous or fabricated OIDC actor.

Define a minimal versioned trusted account service and authorization port.
Default authorization always denies post-setup admission/disablement; 006 later
supplies registered policy. Authority comes from the current host actor or a
future 006-verified durable onboarding grant/initiator, never command-supplied
actor/permission fields. External proof is a host-issued request-scoped handle.
No public admission/admin endpoint or browser privilege flag.
Owner provenance alone satisfies no policy. Owner bootstrap uses its separate,
narrow setup admission, not a temporary generic permission.

**Rationale:** This keeps 005 independently deliverable and gives 006/007 one
explicit integration boundary without inventing their workflows/RBAC now.
Trusted test-owned consumers exercise permitted and denied policy implementations;
none ship a bypass. No cross-module authorization transaction is promised.

**Alternatives considered:** Treating local as OIDC, adding an enum value to a
closed v1 contract, or converting all initial Owners to an allow-all role is
rejected.

## A9 - Evolve platform/recovery formats without rewriting history

**Decision:** Append provider-specific migration 3 for platform format 3 /
identity schema 2. Keep migration 1/2 bytes/digests and old format fingerprints.
Preserve all users, exact associations, consumed legacy Owner ID/provenance,
installation ID and module data/assignments/history/revision sums.
Import external-only users without passwords. Require explicit legacy issuer to
connection mapping; preserve retired issuer associations under disabled archived
connections. Erase old sessions/attempts/admission windows and unconsumed nominee/
token material, advance epoch/generation, and record the upgrade transactionally.

Recovery-v3 is format-3 only; actual formats 1/2 retain their exact readers.
Hash the complete deterministic security snapshot, including new tables.
Restores remain stopped/quarantined until explicit history reconciliation,
fresh protected keys, session/proof/reset/setup invalidation and safe activation.
Preserve consumed ownership; uncertain historical ownership stays recovery-locked.

**Rationale:** The delivered store is an upgrade source, not a disposable
prototype. Key-file effects remain separately reconciled, not falsely DB-atomic.
No Down migration, OS-copy rollback detection or historical-owner replacement
is claimed.

**Alternatives considered:** Editing migration 2, relaxing inventories, second
identity DBs, automatic live migration and restore-as-fresh bootstrap are rejected.

## A10 - Retain budgets, privacy and the existing destination finding

**Decision:** Retain one-second local operations including manager work,
four-second OIDC completion, 4.5-second adapter and five-second actionable UI;
existing startup/acquisition/maintenance limits and 120-second preparation remain.
No fresh clock/retry budget inside a manager/store call. Sessions retain
30-minute idle/eight-hour absolute, eight/user and the untested 50,000 runtime cap.
Keep existing input/egress/cache/rate/event bounds and specify new finite account
inputs in v2. Fix forbidden DNS/connected-peer admission *even inside an allowed
CIDR*, with IPv4-mapped/mixed-answer coverage and approved private fixtures.

**Rationale:** F1/T101 remains a real review defect. F2/T102 remains an
undiagnosed SQLite enrollment-setup 503 before key damage; a changed flow does
not diagnose it. Preserve its exact phase/assertion evidence and require
source-specific regression/qualification under T103, not a passing rerun.

**Alternatives considered:** Longer deadlines, periodic stamp checks,
positive auth caches, sensitive diagnostics or the prohibited installation
session-capacity test are not available tradeoffs.

## A11 - Keep Next.js and focused test ownership

**Decision:** Extend the existing identity page, enumerated adapters, generated
contract and dedicated unit/integration/consumer/browser projects. All .NET
test source stays under its owning `tests/` project with normal SDK inclusion.
Keep 320px/200% text, keyboard/focus/announcement, a11y and actual latency proof.

**Rationale:** Local/setup/reset/association states need real UI/protocol
evidence, not a Razor scaffold, fake principal or hidden test route.
Local-only needs no IdP/Docker; real PostgreSQL and full gates use owned hosted/
disposable infrastructure, not heavy suites on the shared Mac.

**Alternatives considered:** New test-linked production slices, new projects,
generic catch-all proxies, a full 006 administration UI or a new form library
expand scope without necessity.

## A12 - Exact dependency and implementation admission gates

**Decision:** Retain current manifest/lock pins, including the recorded
Playwright exception. Prefer the Host's existing shared-framework Identity
assemblies, with no EF Identity package. Before adoption, verify actual reference/
runtime assembly closure, exact manager/token semantics and direct/transitive
licenses/provenance/advisories/locks/notices through existing tooling.

**Rationale:** Public docs/source and this design do not certify the exact
installed build. Required first integration proof exercises actual managers,
five store interfaces, concurrency stamps, lockout/rehash/failure writes, reset
replay, single owner commit, cookie publication and one-second contention/
cleanup on both providers. A failure blocks adoption; it cannot be hidden behind
longer deadlines or a fallback account implementation.

**Alternatives considered:** Installing packages to plan, inferring package
necessity from API-reference metadata, or calling historical green reports
new-architecture evidence is rejected.

All planning choices are resolved at proposal level. Runtime/dependency proof,
human design/quality review, refreshed tasks/analysis and implementation are
later gates, not unanswered product questions or claimed results.

[stores]: https://learn.microsoft.com/aspnet/core/security/authentication/identity-custom-storage-providers?view=aspnetcore-10.0
[model]: https://learn.microsoft.com/aspnet/core/security/authentication/customize-identity-model?view=aspnetcore-10.0
[transactions]: https://learn.microsoft.com/ef/core/saving/transactions
[sqlite-async]: https://learn.microsoft.com/dotnet/standard/data/sqlite/async
[sqlite-errors]: https://learn.microsoft.com/dotnet/standard/data/sqlite/database-errors
[ef-store]: https://github.com/dotnet/aspnetcore/blob/main/src/Identity/EntityFrameworkCore/src/UserOnlyStore.cs
[registration]: https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/IdentityServiceCollectionExtensions.cs
[manager]: https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Extensions.Core/src/UserManager.cs
[signin]: https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Core/src/SignInManager.cs
[reset-provider]: https://github.com/dotnet/aspnetcore/blob/main/src/Identity/Core/src/DataProtectorTokenProvider.cs
