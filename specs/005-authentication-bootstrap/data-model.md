# Data Model: Host Identity v1 / Platform Format 2

**Status:** Proposed; no tables or migrations delivered by planning.
**Owner:** Host core identity in `kofte_system` (PostgreSQL) or the existing
separate `platform/platform.db` (SQLite), never an optional module.

**Rework gate (2026-10-30):** This is the original mode-bound model. The
[approved scope amendment](../../docs/adr/0007-authentication-bootstrap-boundary.md#human-approved-scope-amendment-local-accounts-and-first-login-ownership)
requires provider-independent users, optional local/external login methods and
atomic first-login ownership. New storage/migration design is not yet approved;
do not reinterpret these rows or reset consumed bootstrap to implement it.

## Physical ownership and compatibility

Keep the three accepted registry tables, original migration-1 digest, existing
installation ID, module registration revisions and assignments unchanged.
Append a provider-specific migration 2 to `__kofte_registry_migrations`;
advance the installation `registry_version` and SQLite `user_version` to 2.
Do not rename tables, reinterpret module versions or edit migration-1 bytes.
Identity schema version 1 is a component of platform format 2.

The exact format-2 table inventory is the three registry tables plus the eight
identity tables below. Validate their actual columns/types/nullability/PK/FK/
UNIQUE/CHECK/index definitions and the two ordered migration digests; reject
unexpected tables, views, triggers, routines, defaults or generated objects.
PostgreSQL tables/indexes/history stay in `kofte_system`; SQLite preserves
private root/file/sidecar checks and foreign keys, without `ATTACH`.
Module contexts/history/markers/allocator version remain unchanged.

Use provider-specific SQL in the existing platform migration mechanism rather
than introducing another migration framework. This is host-local platform
persistence; it does not relax module-owned EF Core requirements. Explicit
preparation holds the accepted exclusive stopped-installation scope and
validates the complete inventory before writing. Serving never migrates.

## Common values

IDs are nonempty random UUIDs, rendered canonical lowercase `D` form publicly.
Times are UTC instants; decisions use the host clock, with documented clock
synchronization and at most 60 seconds OIDC evidence skew. All counters are
nonnegative checked 64-bit integers; overflow refuses the operation.
Persist enum values as fixed bounded strings with database CHECK constraints.
All SQL values are parameters; physical identifiers are fixed host constants.

External identity keys are `(mode, issuer, subject)`, compared byte-for-byte,
case-sensitively: PostgreSQL deterministic `C` collation / SQLite BINARY.
No trimming, lowercasing, URL-path normalization, email matching or provider-label
remapping. Production issuer is the exact configured validated OIDC issuer;
subject is a nonempty at-most-255-byte printable ASCII OIDC `sub`. Issuer is
at most 2,048 UTF-8 bytes. Unsupported values fail before persistence.
Development issuer is the fixed separate `kofte:development:v1` namespace,
with a configured synthetic subject, never a production issuer.

## Entities and constraints

### `__kofte_identity_metadata`

Singleton key 1; installation ID FK to the accepted installation; identity
schema version 1; checked `security_revision`; random nonempty `session_epoch`;
active identity mode; trusted-configuration digest; last explicit
reconciliation time.

Every identity/security mutation updates the revision in its owner transaction.
Do not increment the accepted registry's module revision sum for identity
changes. Session epoch changes invalidate all previously issued tickets/attempts;
switching mode or issuer/client/public-origin trust requires explicit stopped
configuration reconciliation, never request-driven conversion. The trust digest
excludes display text and rotatable secret values; their ordinary changes do
not relink users. Startup rejects an unacknowledged trust change.
Missing/duplicate/mismatched metadata is corruption, not a fresh-install signal.

### `__kofte_users`

`user_id` PK; identity mode; optional safe display name; created/updated UTC.
At most 10,000 users; no password, email identity key, external privileges,
team memberships or implicit administrator field.

Display text is optional and at most 128 Unicode scalar values after rejecting
controls; render as escaped text. Display changes may update this value only,
never user ID/mode/access. No account linking or user deletion endpoint in 005.

### `__kofte_external_identities`

PK/UNIQUE `(mode, issuer, subject)`; `user_id` UNIQUE FK to users with matching
mode; created UTC. A user has exactly one mapping in this slice; no cross-mode
or profile-based merge. Validate complete user/mapping correspondence at
startup/maintenance; database constraints prevent concurrent duplicate mapping.

Concurrent first login selects/inserts under an owner transaction. A known
UNIQUE conflict is reconciled by reading the exact committed key, never by
catching every storage failure as "existing user". Preserve its established
designation. Unknown commit outcome yields no session/cookie/success response.

### `__kofte_login_attempts`

Random attempt ID PK; digest of random browser binding; mode; exact configured
issuer/client configuration digest; session epoch; admitted local return path;
created/expiry UTC; state `pending` or `consumed`; consumed UTC nullable.

Do not store raw authorization code, PKCE verifier, provider token, raw state
or nonce. Framework protected state/correlation/nonce handle the protocol
exchange; bind the local attempt ID into protected authentication properties.
Pending expires after five minutes; at most 256 installation-wide and eight
per browser binding. Expired/consumed rows can be pruned in bounded batches.

`pending -> consumed` is a compare-and-update in the same transaction that
creates/maps the user and stores the session. A cancelled/invalid/timed-out
exchange never creates an authenticated ticket. A consumed attempt cannot be
reused even with otherwise valid evidence; restart does not resurrect it.

### `__kofte_sessions`

Session-reference digest PK; user ID FK; mode; copied session epoch; encrypted
minimal local authentication ticket; created/last-seen UTC; idle/absolute
expiry UTC; state `active`, `revoked` or `expired`; terminal UTC nullable.
Generate a new 256-bit random reference for every new session; never reuse a
pre-login reference. Store no raw reference, provider token/claims or CSRF token.
Use domain-separated SHA-256 over random references, not password hashing.
Ticket protection uses an installation/purpose-bound Data Protection protector.

At most eight active sessions per user and 50,000 installation-wide; overflow
refuses new sessions, never silently revokes a different user/session.
The [human-directed verification amendment](../../docs/adr/0007-authentication-bootstrap-boundary.md#human-directed-verification-scope-amendment-installation-session-capacity)
removes installation-wide session boundary testing, not this runtime policy;
that boundary is not claimed verified.
Idle lifetime is 30 minutes; absolute lifetime eight hours. Activity can
advance idle expiry to `min(now + 30 minutes, absolute expiry)` but cannot
extend absolute expiry. Cookie lifetime cannot override these server limits.

Every protected request retrieves/validates the row, current user/mode/epoch,
times and current designation. No cross-request positive authentication cache.
Renewal uses compare-and-update so it cannot resurrect a revoked session.
`active -> revoked/expired` is terminal. Sign-out success requires confirmed
revocation and its owner-local record; uncertainty returns 503, clears local
identity and never claims known server revocation. No provider-wide logout.

### `__kofte_bootstrap`

Exactly one row, key 1. Fields: installation ID; state `disabled`, `pending`,
`consumed` or `recovery_locked`; checked ceremony generation; optional nominee
issuer/subject, random authorization digest and expiry; optional initial user
ID UNIQUE FK; consumed UTC; consumed generation. A database CHECK enforces
valid nullable-field combinations. Initial user must be a production user.

Initial prepared state is `disabled`, with no nominee/token/designation.
`disabled -> pending` and `pending -> pending` replacement require stopped
operator authorization; replacement increments generation and invalidates the
old secret. Pending expires after fifteen minutes, without becoming an
automatic eligibility grant.

`pending -> consumed` requires authenticated exact production nominee and
constant-time match of a domain-separated digest bound to installation and
generation. Atomically set the initial user/consumed time, erase usable pending
nomination/token fields, increment revision and append the durable enrollment
record. Row lock/CAS (PostgreSQL) or immediate write transaction (SQLite), the
singleton and UNIQUE constraints are all enforced. Twenty eligible contenders
produce one committed transition; later contenders get non-enumerating conflict.
The initial designation is derived from this row, not cookie/IdP claims.

`consumed` never returns to pending/disabled through login, config, upgrades,
ordinary restart or available commands. `recovery_locked` blocks enrollment
after uncertain historical restore/reconciliation; it does not nominate a
replacement initial administrator. It may retain the verified recovered
initial user. Missing historical designation requires a separately reviewed
recovery decision, not deleting this row.

### `__kofte_identity_events`

Random event ID PK; checked security revision; event code from a fixed
allowlist; UTC; generated 32-lowercase-hex correlation; optional internal user
ID FK; outcome `committed`; retention class `installation` or `session`.
No external issuer/subject/profile, raw session/authorization reference,
tokens, paths, provider response or exception text.

`user_created`, `initial_administrator_enrolled`, and explicit operator
security-reconciliation records are immutable installation-retained records.
Session created/revoked/observed-expired records are retained for 30 days.
Required records share the changed state's transaction. A rejected/outage/
uncertain operation instead emits bounded sanitized diagnostics; it cannot
manufacture a committed durable event.

Capacity: at most 1,000,000 installation-retained and 200,000 session records.
Warn at 90%; refuse a required mutation if its record cannot be stored.
Prune only expired session-class records in batches of at most 128, within
the enclosing budget; an explicit stopped maintenance command can continue
bounded pruning. Never silently trim installation evidence or omit a required
event. No query/export/browser administration API; 009 may later evolve this
contract with explicit migration/retention decisions.

### `__kofte_identity_admission`

Fixed-category window/key digest PK; UTC window start/expiry; checked count.
Contains only bounded hashed browser/peer keys, never raw IP, identity, token
or arbitrary metric dimension. Enforce identity-v1 rate limits transactionally
across restart; at most 4,096 live windows. Reject at capacity, prune expired
windows in at-most-128-row batches. Request diagnostics remain available when
the identity store itself fails; no success-shaped rate-limit fallback.

## Transaction and failure invariants

- User + exact external mapping + `user_created` + attempt consumption + new
  session/event are one host-store commit. Existing-user login leaves mapping
  and initial designation stable.
- The validated-login coordinator constructs the minimal local ticket and
  commits that entire unit before publishing a cookie. The request-local
  ticket-store bridge returns the newly generated reference only for that
  known committed session; `ITicketStore.StoreAsync` must not independently
  recreate it or split mapping/evidence into an earlier OIDC-event commit.
  Never place raw reference/verified external evidence in caller-controlled
  ticket fields. Contract tests cover cookie-publication failure, concurrent
  mapping resolution and no cookie on an uncertain commit.
- Enrollment singleton + designation + consumed state + revision + immutable
  event are one host-store commit, not a cross-module transaction.
- Validate store authority, constraints and current state before effects.
  Retry only a specifically identified known-not-committed conflict within the
  original budget; never retry an uncertain commit automatically.
- Crash tests synchronize real child processes immediately before commit,
  after durable commit and before response. Reopen independently to distinguish
  no commit, exactly one commit and uncertain acknowledgement. No count-only
  assertion substitutes for exact original IDs/data/security records.
- Session/runtime writes use the normal host lease/identity transaction, not
  elevated maintenance credentials. Schema/preparation/operator changes require
  the accepted exclusive stopped scope. No module DbConnection or table access.

## Key material and recovery model

The separately owned persistent key ring is installation/purpose-bound and
certificate-protected in OIDC mode; local development may use private
owner-only filesystem protection. Missing/foreign/undecryptable key material
fails closed rather than creating replacement keys on serve. Normal framework
rotation in a valid ring retains usable retired keys and does not revoke
sessions; explicit session-epoch change does. Client/certificate secrets
are private referenced files, not identity DB columns or recovery metadata.

Recovery-v2 adds an identity summary to the existing exact complete-set
manifest: identity schema version, security revision, session epoch, bootstrap
state/consumed generation, counts and a SHA-256 digest of the deterministic
identity security snapshot. This is private backup metadata, not health/API
output. Verify the actual restored rows/constraints/history, not only the
manifest's self-description. The full platform DB/dump already contains all
identity rows; do not back up only the summary.

Accept recovery-v1 only for actual platform format 1, recovery-v2 only for
format 2. Preserve exact platform/all-module artifact inventories and hashes.
Old binaries reject format 2. A v1 restore requires explicit upgrade before
identity serving; it is never proof bootstrap was historically unused.

Restore into stopped quarantine. Before exposure, explicit reconciliation
invalidates restored tickets/attempts/admission windows and pending ceremony,
advances epoch, provisions new protected keys and records its operator decision.
Retain committed users/mappings/events/designation and unrelated module data.
An explicit operator decision that historical enrollment never occurred may
leave verified unconsumed state disabled for a later deliberate nomination;
it cannot override any recovered consumed state/event. If history is unknown
or a consumed designation cannot be recovered, keep `recovery_locked`; no
next-login promotion. Forward recovery or a compatible complete restore is
supported, not Down migration or rollback-proof security.
