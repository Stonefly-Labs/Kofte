# Identity Operator v2: Connections, Setup, Credentials and Recovery

**Status:** Proposed design; the commands/configuration below are not delivered.
Delivered [operator-v1](operator-v1.md) and current local-development instructions
retain their old scope. Implementation must update the public guide/consumer,
strict CLI/configuration allowlists, worker IPC and owned verification together.
Authority remains the existing admitted supervisor/exclusive stopped installation,
not a browser Owner, raw worker or new remote administration endpoint.

## Configuration version 2

`KOFTE_AUTH_CONFIG_FILE` continues naming an absolute private regular no-symlink
UTF-8 strict JSON file, at most 32 KiB, no duplicate/unknown fields.
No implicit production/development fallback or fallback credential.

| Field | Contract |
| --- | --- |
| `version` | Exactly `"2"`; v1 serving fails with explicit upgrade/configuration guidance, never reinterpretation. |
| `profile` | `production` or `development`. Profile changes require stopped reconciliation. |
| `publicOrigin` | Exact HTTPS origin for **all production methods**; development's admitted exact loopback HTTP web origin only. |
| Key fields | Retain v1 `keyRingDirectory`, encryption certificate/private-key references and optional retained decryption pairs with their existing file bounds/ownership/rotation behavior. Certificate protection mandatory for production local **and** OIDC. |
| `connections` | Production-only array: at most one local and eight OIDC entries, at least one enabled. Each has stable canonical GUID `id`, immutable `kind`, safe label and explicit enabled state. |
| Local entry | Only id/kind/label/enabled; no plaintext password, account, reset, owner or relaxed security fields. |
| OIDC entry | V1 provider's exact issuer/metadata/authorization/token/JWKS trust, client ID/private secret reference and endpoint/network allowlists per connection; separate exact v2 callback. No permissive TLS/PAR/UserInfo/redirect fallback. |
| `development` | Development-only enabled opt-in and one to eight synthetic keys/display names; no production connections/credential/Owner option. |

All private file/key/client-secret bounds, no-symlink/owner checks, one configured
public origin, private loopback backend/host/web and production ingress duties
remain. `KOFTE_ALLOW_UNSAFE_EXPOSURE` cannot enable Development authentication.
No newly supported remote DB, multi-instance/HA or trusted-forwarded-header
profile. Startup refuses missing/changed trust, unsupported methods and unknown
config rather than contact arbitrary destinations or auto-upgrade.

Connection ID/kind/issuer never retarget an association. Labels can change
without identity/session effects. Secret/certificate rotation with retained
decryptors follows v1 custody rules; trusted-origin/client/endpoints/network/
enablement changes require explicit reconciliation and global session epoch
invalidation. An issuer change needs a different ID and explicit association.
Disabled/archived associations remain owned, not reassigned or auto-linked.

## Stopped command grammar

```text
--identity prepare [--legacy-connections-file ABSOLUTE_PRIVATE_FILE]
--identity setup authorize --authorization-file NEW_PRIVATE_FILE
--identity setup disable-pending
--identity password-reset authorize --account-file ABSOLUTE_PRIVATE_FILE --authorization-file NEW_PRIVATE_FILE
--identity reconcile-configuration
--identity revoke-sessions
--identity maintain
--identity reconcile-restore --decision-file ABSOLUTE_PRIVATE_FILE
```

Secret values never appear in arguments/output. Old nominated
`bootstrap authorize/disable-pending` commands are retired with explicit safe
upgrade guidance, never aliased to a new setup token.
Every command preserves existing finite supervisor/worker/lease/cleanup rules,
total 120-second envelope and operation-specific secret subset validation.
No live host/foreign scope/direct worker/lost lease/unverified descendant cleanup
can authorize effects. Output stays fixed safe outcome/correlation/durability
and corrective code, with no private paths, account names/subjects or token.

### Prepare and migrate

Verify installation/provider and exact known old format/history/catalog/module
inventory before writing. Registry-v0 needs the existing explicit registry
upgrade; format-1/2 requires explicit preparation to format 3, not serving repair.
Keep old migration bytes/fingerprints; append migration 3 and its digest, validate
the final 17-table inventory and exact original module/registry/user evidence.

For format 2, the private legacy map is strict JSON:
`{version:"1",mappings:[{issuer,connectionId}]}`; at most 10,000 entries / 32 MiB,
enough for every admitted legacy issuer at its maximum length.
Require exactly one mapping per stored legacy production issuer and distinct
stable IDs; configured active entries must match the exact issuer. Unconfigured
historical issuers become archived disabled connections, with no synthesized
trust/secret or provider contact. Missing/conflicting/extra maps refuse before
effects. No subjects/email/role/user reassignment field. Format 1 needs no map.
Current verified format 3 prepare is idempotent and rejects a legacy map rather
than silently apply a new mapping. This bounded file is not the 32-KiB config.

Preserve user IDs/exact external ownership/consumed legacy Owner and all original
immutable security events. Generate reserved external/development usernames and
fresh stamps but **no password**. Retire old sessions/attempts/windows and pending
nominee/authorization, advance epoch/generation and record upgrade.
Fresh setup is available only for verified never-consumed ownership.
Serving reads/verifies ready format 3 and usable keys; never migrates or
regenerates missing/foreign keys. Interrupted DB/key effects remain explicitly
gated and reconciled, not falsely filesystem-atomic.

### Authorize protected initial setup

Production only, identity-ready, pending/disabled and never consumed.
Generate random 256-bit token, fifteen-minute expiry, installation/generation-
bound digest. Create/write/flush a new private owner-only no-symlink output file
before committing matching authority; never overwrite a file or print a token.
Private output-file bounds are 4 KiB for setup and 8 KiB for reset delivery.
Read actual current state before replacement; consumed/recovery-locked refuses.

DB and file delivery are not atomic. A failed/uncertain write/commit reports
truthful unchanged/uncertain state and preserves evidence; do not assume file
existence proves active authority. Explicit stopped replacement invalidates the
old generation and all its browser admissions/attempts. Output cleanup is exact
operator-owned file cleanup, not broad deletion. Do not mint replacement after
consumption because a response was lost.

The operator privately delivers the token without nominating a person.
User opens `/identity`, submits it to the protected setup admission form, then
creates local credentials or deliberately completes an enabled OIDC login.
Token admission alone creates no authenticated session or Owner.
Only the first eligible successful owner transaction wins; token/setup/attempt/
account/Owner/session/evidence commit together. Losing unadmitted accounts do
not survive. Clear the setup cookie after known claim/closure; securely handle
transient delivery files under operator policy. Never put tokens in URLs,
console, support attachments, rendered/prepopulated pages or repository files.

`disable-pending` invalidates unconsumed token/admission only, with evidence.
Restart, new configuration, another token, provider change, Owner loss and restore
never reopen consumed ownership. There is no Owner replacement command.

### Private operator-assisted local password reset

The public UI explicitly says operator assistance is required; its generic
request response does not imply a queue/email was delivered or identify users.
The operator verifies the requester out of band and uses a private strict
account input `{version:"1",userId}` (canonical GUID; <=4 KiB).
Only an enabled production user with an existing local credential and enabled
local method qualifies. This is trusted offline recovery authority, not a way
to create a password for an OIDC-only user, read an old password or sign in as
the user. General admin recovery workflows/permissions remain 006.

Generate a framework reset token with dedicated installation-bound purpose/
provider and fifteen-minute lifetime. The new private authorization file contains
strict `{version:"2",authorization}`; no URL/account/profile or plaintext password.
Commit pending digest/user/stamp/generation/expiry plus event; at most one
pending reset/user. Replacement invalidates the prior digest without silently
changing the credential, stamp or existing sessions at issuance.
Use the same explicit file/commit uncertainty handling as setup authorization.

Deliver the file/token privately. User enters local name/token/new password in
the protected reset form; framework validation, credential/stamp/security version,
consumption, old-session/proof invalidation and event commit together.
No automatic login; re-entry uses the new password and same internal user.
Wrong/expired/replayed/other-account tokens have identical public failure and
zero credential/session/Owner effects. Successful reset clears local failed-count
and lockout through framework APIs in that same transaction; it never enables a disabled
account or production Development profile.

### Session/configuration/maintenance correction

Retain explicit `revoke-sessions`, key rotation/loss and reconciliation duties.
Epoch change makes old tickets fail on their next protected request.
Configuration reconciliation preserves exact canonical mappings/consumed Owner,
invalidates attempts/setup/proofs/resets and does not auto-associate new
connections. Label changes and ordinary rotatable secret contents are excluded
from trust identity. Missing decryptors refuse; no blind ring overwrite.

Maintenance removes only bounded expired/terminal non-installation state,
at most 128 rows/category/command with exact dependent-order/FK checks and safe
diagnostics. Installation records/Owner/external ownership are not swept.
Repeat explicitly for more work; no unbounded request sweep or deadline increase.

## Complete-set recovery and compatibility

Recovery `kofte-storage-recovery-v3` accepts platform 3, with the
[complete fourteen-table security summary](../data-model.md#keys-complete-set-recovery-and-failure).
Keep exact v1/format-1 and v2/format-2 reader behavior, all module artifacts/
hashes and real restored-row/catalog/history verification. Full platform backup
includes password verifiers/security records; treat it as private sensitive data.
Never add raw key/config/setup/reset delivery files to the recovery set.

Restore remains stopped quarantine with an out-of-snapshot activation marker.
Decision input stays bound to exact recovery-set/installation/manifest hash;
v2 decision uses `priorOwnership:known-consumed|known-never-owned|not-established`
instead of silently reinterpreting v1's nominated terminology.
Supported legacy decision parsing remains explicit for old recovery sets.
Reconcile against trusted external history, invalidate every restored session/
attempt/setup/proof/reset/window, provision fresh protected keys and record the
decision before exposure. Interrupted intent/journal/key/DB/activation boundaries
remain gated and resumable.

Preserve verified consumed Owner/user/mappings/events/modules; contrary
never-owned claims cannot override consumed evidence. Uncertain history or
unrecoverable consumed user remains recovery-locked. Prior credentials may be
rolled back by a valid snapshot; operators must reconcile/reset affected accounts
before exposure where security cannot be established. This is not rollback-proof
history, copied-instance revocation, provider-wide logout or old-instance fencing.
Forward upgrade/recovery or compatible complete restore only, no Down migration.

## Private evidence

Extend existing sanitized diagnostic/IPC allowlists for local/setup/reset/proof/
association/upgrade outcomes. Required state changes and security records share
one owner transaction. Preserve original immutable legacy events/provenance.
No usernames/issuer/subject/password/token/private filename/HTTP payload/exception
dump in public errors/logs/traces/metrics/IPC; metrics use fixed categories only.
Retain existing bounded capacity/retention, canary-capture and failed-sink rules.
Unknown commit never becomes a known success or a bad-password fallback.
F1 forbidden destination and F2 pre-key-fault setup 503 remain open obligations,
not closed by introducing new command names.
