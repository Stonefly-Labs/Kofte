# Data Model: Account Schema 2 / Platform Format 3

**Status:** Proposed revised design; no migration or tables delivered here.
**Owner:** Host `kofte_system` (PostgreSQL) / existing `platform/platform.db`
(SQLite). [Research A1-A12](research.md) and [plan](plan.md) govern.
The [previous model](https://github.com/Stonefly-Labs/Kofte/blob/f10351a599191a5931b05a719cd8d291eba839fb/specs/005-authentication-bootstrap/data-model.md)
remains the historical format-2 definition, not new-scope evidence.

## Physical ownership and common invariants

Keep the three registry table names, installation ID, module assignments,
allocator markers/revision sum and original history intact. Append reviewed
provider-specific `003_accounts.sql`; platform `registry_version` /
SQLite `user_version` become 3; identity schema becomes 2. Old migration bytes/
digests and format-1/2 fingerprints are immutable. Serving never migrates.

The exact format-3 inventory is three registry tables plus these fourteen:

```text
__kofte_identity_metadata     __kofte_users
__kofte_auth_connections      __kofte_external_identities
__kofte_external_logins       __kofte_login_attempts
__kofte_sessions              __kofte_bootstrap
__kofte_setup_admissions       __kofte_credential_proofs
__kofte_password_resets        __kofte_account_operations
__kofte_identity_events        __kofte_identity_admission
```

Verify actual columns/types/nullability/PK/FK/UNIQUE/CHECK/collations/indexes/
triggers/routines/defaults/generated objects and all three ordered migration
digests. Add independently frozen format-3 native-catalog fingerprints, not
weakened old fingerprints or an allow-all object inventory. SQLite foreign keys,
canonical/private file/sidecar ownership and SQLite-owned header checks remain;
no `ATTACH`, independent live-header file reader or cross-module connection.

IDs are nonempty UUIDs; public canonical lowercase `D`. UTC instants; checked
nonnegative 64-bit counters; overflow refuses rather than wraps. Fixed enums
have DB CHECKs. Exact opaque keys use PostgreSQL deterministic `C` / SQLite
BINARY comparison. All values are parameters; physical names are host constants.
Display is nullable escaped text, at most 128 Unicode scalars, without controls.
Issuer stays at most 2,048 UTF-8 bytes; subject is nonempty printable ASCII,
at most 255 bytes. No email/profile-based identity key.

## Entities and validation

### Metadata and authentication connections

`__kofte_identity_metadata`: singleton 1; installation FK; schema version 2;
checked security revision; session epoch; context `production|development`;
configuration trust digest; reconciliation time. Every committed identity change
updates the owner revision, never the module revision sum. Missing/duplicate/
mismatched metadata is corruption, not new setup.

`__kofte_auth_connections`: connection GUID PK; kind `local|oidc|development`;
immutable exact issuer for OIDC/development; safe label; enabled/retired state;
checked trust revision/digest; creation/update times. At most one local, eight
configured OIDC and one reserved Development connection; at most 10,000 archived
legacy OIDC connections, 10,010 total connection rows, never implicit active trust.
No client secret, certificate/private key or provider token columns.
UNIQUE `(connection_id,kind,issuer)` supports exact binding constraints.
An OIDC connection ID's issuer/kind cannot change. Retired IDs cannot be reused.
Development is a separate context/namespace and never an eligible Owner method.

### Canonical user and local credential

`__kofte_users`: user GUID PK; context; username and normalized username;
nullable local password hash; nullable safe display; `enabled` flag;
framework security/concurrency stamps; checked account/security revisions;
framework failed-count, lockout-enabled and nullable lockout-end; created/updated
times. UNIQUE normalized username (BINARY/C); UNIQUE `(user_id,context)` for
context-enforcing FKs. At most 10,000 users.

Names/password policy is [A3](research.md#a3---one-provider-independent-user-with-explicit-methods).
External-only names use reserved generated prefixes, have no password and
cannot pass local login/reset. The first explicit password association changes
that generated name through UserManager; local names are subsequently reserved
to the same user even after password removal. No public rename/user-delete API.
Development users cannot acquire production credentials or associations.

Password hashes use Identity V3/SHA-512 only, bounded to 1 KiB; admit work factors
from 100,000 through 210,000 before expensive framework verification (weaker
supported hashes require framework rehash), and never persist
plaintext, reversible passwords or password-derived request fingerprints.
Normal new hashes use configured Identity V3 210,000 iterations. Unexpected
hash encodings/work factors fail closed with private corruption/configuration
diagnostics, not an unbounded verifier or invented credential.

Adapter setters mutate the scoped account; Create/Update and association writes
use the current owner transaction. Update compares expected account revision/
concurrency stamp and rotates/increments them. Map only known duplicate/concurrency
outcomes to typed Identity failures; unrelated store errors propagate.
Record a failed store write in the enclosing operation even if a manager
discards its result; that operation cannot commit. Security-stamp change,
credential/association change or account enablement change
advances security version and invalidates affected sessions/proofs. Failed-count/
local lockout changes do not disable independent OIDC authentication.

### Exact external identity ownership and connection login

`__kofte_external_identities`: identity GUID PK; context; exact issuer/subject;
user/context FK; created time. UNIQUE `(context,issuer,subject)` and
UNIQUE `(external_identity_id,user_id)` and
`(external_identity_id,context,issuer,subject,user_id)` for composite FKs;
a verified external identity has one canonical owner across connections.
At most 80,000 owned external identities. Preserve legacy exact key bytes.

`__kofte_external_logins`: connection ID, context, exact subject (`ProviderKey`),
external-identity ID, user ID, kind/issuer and created time.
PK `(connection_id,subject)`; UNIQUE `(user_id,connection_id)`;
FKs to the connection's fixed issuer/kind and external identity's exact
issuer/subject/user/context. One external login per user per connection, up to
eight enabled production external methods/user; disabled archived mappings
remain preserved. At most 80,000 production connection bindings.
Development bindings remain separate, at most one per synthetic user.

Creating a binding requires host-verified protocol evidence plus setup/trusted
admission or explicit existing-user association proof. Existing canonical
issuer/subject ownership never auto-adds a new connection binding and never
merges another user. Label changes are not binding changes.

### Attempt, session and fresh account-control proof

`__kofte_login_attempts`: attempt GUID PK; browser digest; context; connection/
trust revision; epoch; purpose `login|reauthenticate|associate`; optional setup
admission or target user/origin session/proof IDs as required by purpose;
allowlisted return path; created/expiry; pending/consumed state/time.
DB CHECKs enforce the purpose-specific nullable combinations.
No raw code, state, nonce, PKCE verifier or provider tokens. Use framework
protected authentication properties to bind the local attempt ID/purpose.
Five-minute expiry clipped to dependent proof/setup expiry; 256 pending total,
eight/browser. Callback consumes it in the resulting owner transaction.

`__kofte_sessions`: reference digest PK; user/context FK; authentication method/
connection/trust revision; copied epoch/account security version/stamp digest;
protected minimal v2 ticket; created/last-seen/idle/absolute expiry;
active/revoked/expired state and terminal time.
New independent random 256-bit references; no raw reference/provider claims.
Production and Development use different cookie/protection purposes.
Eight active/user and unchanged 50,000 installation runtime cap; 30-minute
idle/eight-hour absolute, no silent eviction or extension past absolute.
The installation session-capacity boundary is expressly **not tested**.

Every protected request checks fresh durable metadata, session, enabled user,
context/version/stamp, connection trust, times and designation. No positive
cross-request cache or periodic stamp admission. Renewal uses conditional update
and cannot resurrect terminal state. Sign-out publishes success only after known
revocation/event commit; uncertainty clears UI without claiming confirmed effect.

`__kofte_credential_proofs`: random proof GUID PK; user FK; current session
digest FK; account security version/stamp digest; proof method/connection;
purpose `account-maintenance`; created/expiry; pending/consumed state/time.
Five minutes, one pending proof/session, at most 256 pending installation-wide.
No browser-readable proof secret: resolve by current session and consume once.
Local proof uses check-only framework authentication; external proof validates
fresh signed `auth_time` for the exact existing association. Successful reauth
replaces/revokes the old session and binds this proof to the newly committed one.

### Setup singleton and admitted setup browser

`__kofte_bootstrap`: singleton 1; installation FK; state
`disabled|pending|consumed|recovery_locked`; generation; nullable token digest/
expiry; nullable initial user/context FK; consumed time/generation; provenance
`local|oidc|legacy-nominated` only when consumed; nullable consumed connection.
Initial context must be production. Strict state/nullable CHECKs.
No nominee issuer/subject or browser-submitted role/owner fields in schema 2.

`disabled -> pending` and pending replacement require stopped authorization;
increment generation, expire old setup admissions and invalidate their attempts.
Token is random 256 bits, fifteen minutes; digest binds installation/generation.
`pending -> consumed` locks/CASes the singleton with authenticated eligible
local/OIDC proof, unexpired browser admission and current trust/epoch.
Set the Owner, consume authority, create/admit user and session and append the
single immutable Owner event in **one commit**. No earlier user/session commit.
Consumption wins over all competing admissions; losing tentative accounts roll
back. Expiry is unusable pending state, not implicit authorization.
Consumed never returns to pending/disabled; recovery_locked never promotes
the next login. Initial designation is provenance, not a universal permission.

`__kofte_setup_admissions`: random reference digest PK; browser-binding digest;
installation/setup-generation/epoch; created/expiry; active/consumed/revoked
state/time. Only an opaque Secure HttpOnly cookie contains the raw reference.
Five-minute lifetime clipped to token expiry; 256 active total, eight/browser.
Admission needs the private token and Origin/CSRF. Token possession is not an
actor/session. One pending generation can have multiple admitted contenders.
Failed/cancelled authentication leaves authority unconsumed until expiry;
replacement/disable/consume invalidates every earlier admission by singleton
generation/state even before bounded cleanup removes terminal rows.

### Password reset and trusted account-operation idempotency

`__kofte_password_resets`: authorization GUID PK; user/context FK; framework
token digest; issuance security-stamp digest; checked generation; created/expiry;
pending/consumed/revoked state/time. UNIQUE token digest and at most one pending
authorization/user. Fifteen minutes, at most 256 pending; replacement revokes
old authority. OIDC-only/development/disabled users are ineligible.
Do not store raw Data Protection reset tokens, passwords or delivery paths.

Issuance writes a private output file and the matching pending digest with
explicit cross-filesystem uncertainty handling. Redemption validates digest,
account/state/expiry and framework token then performs `ResetPasswordAsync`.
Password/stamp/security version, reset consumption, session/proof invalidation
and required event share one commit; clear local failed-count/lockout with the
framework APIs, never administrative disablement. No automatic login. Invalid token/account
or policy input changes no credential/session/Owner state.

`__kofte_account_operations`: operation UUID PK; trusted initiating actor ID
(current actor or future verified onboarding-grant initiator);
allowlisted operation kind; nonsecret intent descriptor; affected user;
result account revision; durable event ID; completion time.
Record only known committed operations in the owner transaction.
At most 1,000,000 installation-retained records, bounded by mandatory event
capacity. No plaintext or hash of password/protocol/reset evidence.
An authorized same-operation/same-nonsecret-intent retry returns the prior result
as already committed without changing credentials; conflicting intent fails.
It never asserts that newly resubmitted password bytes were installed.
Do not replay automatically or use this table to retry unknown commits blindly.

### Security evidence and admission counters

`__kofte_identity_events` keeps immutable old event IDs/codes/revisions/times/
users/correlations/retention. New allowlisted installation events cover account
model upgrade, setup authorization/disable/Owner claim, credential/reset/proof/
association and account-state changes; session-class events remain bounded.
Successful mutations share their owner's transaction. Known denied local
checks commit only failed-attempt/lockout state and its sanitized security event,
never a partial account/association/session. Rejected/uncertain operations emit
bounded safe diagnostics without fabricating committed events.

Retain installation evidence for identity lifetime; session/attempt-class
security records thirty days. Capacity remains 1,000,000 installation /
200,000 expiring records; warn at 90%, refuse required writes at capacity.
Prune expired non-installation records only, at most 128 per category/batch,
inside existing budgets. No general audit query/export surface (009).

`__kofte_identity_admission` keeps fixed category/window/key-digest/count/expiry
rows. Extend exact category CHECKs for setup/local/reset/proof/association
limits in identity-v2. At most 4,096 live windows, bounded pruning; no raw IP/
username/subject, arbitrary label or store-unavailable admission fallback.

Indexes cover normalized-name uniqueness; external ownership/login keys;
user/connection bindings; session user/state/expiry; attempt/admission/proof/
reset expiry and owner; operation ID; event retention. Provider-specific exact
definitions/fingerprints are implemented and independently verified later.

## Migration and compatibility

| Source | Explicit stopped transformation / required preservation |
| --- | --- |
| Registry v0 | Existing explicit registry upgrade first; no table-free auto-repair. |
| Platform 1 | Verify exact v1; apply immutable migration 2 then new 3 in the reviewed preparation boundary. Initialize schema-2 state without inventing historical users/ownership. |
| Platform 2 | Verify exact v2/history/key/installation/module ownership and private complete issuer-to-connection map; then migration 3. |
| Existing users | Keep exact IDs/context/display/times; add generated reserved names, fresh stamps/revisions, enabled legacy admission; null passwords, zero local failures. No dev-to-production conversion. |
| External mappings | Keep exact issuer/subject/user; map each legacy production issuer to one explicitly chosen stable connection ID, archived/disabled when not currently trusted. Preserve separate Development mapping. Missing/conflicting map refuses before writes. |
| Consumed bootstrap | Preserve exact user/time/consumed generation and old event; provenance becomes legacy-nominated, never an available setup grant. |
| Pending/disabled bootstrap | Retire all nominee/token material, advance generation, leave disabled; fresh setup token only if history is genuinely unconsumed. |
| Recovery-locked bootstrap | Keep locked and retain any verified consumed user/provenance; never infer never-owned. |
| Sessions/attempts/windows | Delete obsolete mode-bound rows in migration, advance epoch and record explicit invalidation; never deserialize/relabel them as v2. |
| Module/registry state | Exact installation/module IDs, assignments, revision sums, allocator markers, user data/sentinels and migration histories unchanged except reviewed platform version/history. |
| Already format 3 | Verify exact catalog/history/data invariants; idempotent prepare, no new Owner/token/key reset. |

Migration 3 must rebuild SQLite constrained tables in a foreign-key-safe order
with FK enforcement retained and transactional copies validated before removal.
PostgreSQL alters/copies only platform-owned definitions. Keep legacy event rows
byte/value-equivalent despite necessary checked event-code/constraint evolution.
Validate exact final catalogs, all original history digests and preservation
snapshots; a killed worker yields verified old or new schema, not partial success.
No migration SQL or fingerprint is generated by this planning document.

## Keys, complete-set recovery and failure

Production local and OIDC both require persistent certificate-protected,
installation/purpose-bound keys; Development alone retains its restricted local
profile. Missing/foreign/undecryptable keys fail closed; rotation keeps required
decryptors and does not alone revoke sessions. DB commit and private output/key
file effects are not atomic; durable quarantine/intent and explicit correction
must preserve known committed state instead of blind retry.

Recovery-v3 accepts format 3 only; v1/v2 readers remain format-specific.
Its private summary includes schema/security revision/epoch/context/trust,
connections, counts for all fourteen identity tables, bootstrap provenance/
consumed user/generation and deterministic complete security-snapshot digest.
Hash actual restored rows in fixed table/column/key order and verify catalog/
history; a self-described manifest alone is insufficient. Full DB/dump and exact
module artifacts remain required; private keys/config/setup/reset files stay out.

Restore into stopped quarantine with the existing out-of-snapshot control marker.
Manifest-bound operator reconciliation advances epoch, invalidates restored
sessions/attempts/setup/proofs/resets/windows, provisions fresh protected keys,
records the decision and only then clears activation quarantine. Keep account
IDs/stamps/credential hashes/mappings/events/consumed Owner and all module data.
Restored credentials may reflect an older snapshot; explicitly warn that users
must reconcile/reset affected credentials before exposure where historical
security cannot be established. Snapshot integrity is not rollback-proof history.
Consumed evidence overrides any contrary never-owned attestation.
Unknown/unrecoverable historical ownership remains recovery-locked.

Keep the existing finite preparation/acquisition/cleanup limits and crash
barriers. Retry only known-not-committed boundaries within the original budget.
Unknown commit acknowledgement publishes no cookie/success. Independently reopen
and inspect exact state, not counts alone. Forward recovery or compatible
complete restore is supported; Down migration and automatic owner replacement
are not. F1/F2/T101-T103 remain separate unresolved qualification obligations.
