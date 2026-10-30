# Identity Operator v1: Configuration, Preparation and Recovery

**Status:** Delivered by 005 implement; both-provider public recipes are qualified
in [T094 evidence](../implementation.md#t094-t095-complete-public-catalog-and-delivered-documentation).
Final complete gates and feature acceptance remain separate.
**Authority:** Existing admitted stopped-installation maintenance, not browser
administrator status, a web endpoint or an arbitrary worker invocation.

**Scope rework (2026-10-30):** The
[approved amendment](../../../docs/adr/0007-authentication-bootstrap-boundary.md#human-approved-scope-amendment-local-accounts-and-first-login-ownership)
replaces nominated bootstrap as the target with first-login ownership and adds
production local accounts. This remains the delivered old operator contract.
The initial-claim safeguard is unanswered; a setup token is recommended, not
approved. No new configuration/command or automatic fallback owner exists.

## Strict configuration and production trust

The environment key `KOFTE_AUTH_CONFIG_FILE` is admitted by the
bootstrap/worker allowlists. It names an absolute private regular no-symlink
file, at most 32 KiB, strict UTF-8 JSON object with no duplicate/unknown fields.
Serving requires an explicit mode/configuration; there is no implicit
anonymous/development fallback. Existing development supervisor/owned test
fixtures must generate an explicit local configuration, without production
credentials. Operation-specific stopped commands validate their needed subset
without requiring live provider connectivity.

Configuration version 1:

| Field | Requirement |
| --- | --- |
| `version` | String `"1"`. |
| `mode` | Exactly `oidc` or `development`. |
| `publicOrigin` | Exact HTTPS origin in OIDC; exact configured loopback web HTTP origin in development. No userinfo/path/query/fragment. |
| `keyRingDirectory` | Absolute separate private owned directory outside the managed database recovery root; no symlink, traversal or shared writable location. Installation/purpose-bound marker required. |
| `keyEncryptionCertificateFile`, `keyEncryptionPrivateKeyFile` | Referenced private certificate/key in OIDC; certificate protection is mandatory there. Optional only in explicit local development. No cloud service requirement. |
| `keyDecryptionCertificates` | Optional rotation-only array of one to seven retained `{ "certificateFile": "...", "privateKeyFile": "..." }` pairs, in addition to the current encryption pair. Same private-file/path/64-KiB/RSA requirements; duplicate certificates and unknown fields are refused. Does not change provider trust or the session epoch. |
| `provider` | Required only for OIDC: exact issuer/metadata/authorization destinations, client ID, private `clientSecretFile`, exact backchannel endpoint and network allowlists. No caller-selected authority/provider. |
| `development` | Required only for development: `enabled:true`, one to eight synthetic configured identity keys/display names. No arbitrary email/subject request input or administrator flag. |

All referenced secret files are absolute, regular, owner-only, not symlinked
and bounded (4 KiB client secret, 64 KiB certificate/key). Validate readability
and key/certificate relationship without logging content or filenames. Never
put secrets in arguments, repository `.env`, generated OpenAPI or PR evidence.
Only safe field-category codes are emitted for bad config.

005 retains loopback internal host/web/backend listeners; it does not repurpose
`KOFTE_ALLOW_UNSAFE_EXPOSURE` into a production deployment/trusted-proxy profile.
OIDC public entry requires an operator-managed HTTPS ingress to those private
listeners. All development origins/listeners must be loopback and Development
environment must be explicit; the unsafe-exposure switch cannot override it.
Non-development + development mode, exposed development, OIDC over public HTTP,
missing secret/key or malformed config fail before protected serving.

Production provider trust requires HTTPS with normal certificate validation,
exact issuer (including meaningful path/trailing slash), client audience,
RS256 signature/key, expiry/nonce/state/browser binding and code+PKCE.
The baseline confidential client uses provider-supported client-secret token
authentication; document and test the handler's delivered method. Reject a
provider requiring an unsupported flow rather than weakening verification.
Do not derive identity from `preferred_username`, email, groups, role or name.

Metadata issuer and authorization/token/JWKS destinations must match configured
exact endpoints and approved networks. Validate DNS resolution and connected
destination; reject redirects, userinfo, fragments, metadata-service/link-local
targets and unapproved address ranges. Private organization IdPs require
explicit operator network approval; a trusted operator is not sandboxed.
Test fixture TLS trust is injected only by test-owned hosting/transport, never
a production "skip TLS" flag. No arbitrary URL fetch, insecure signing-key
fallback, UserInfo or hidden PAR request. Honor the finite request/byte/time/
cache limits in [identity-v1](identity-v1.md#finite-budgets).

Public absolute redirects derive from `publicOrigin`; forwarded headers do not
select identity/redirect trust. The minimal deployment supports one application
instance/key ring, not HA or a shared remote database profile. Existing
`test-postgres` disposable ownership/credential restrictions remain unchanged.
Broader deployment/remote database/HA configuration belongs to 022.

## Stopped command grammar

These commands are delivered host CLI capabilities. Use the built host DLL path from the local
guide. Secret values never appear in command output/arguments.

```text
--identity prepare
--identity bootstrap authorize --nominee-file ABSOLUTE_PRIVATE_FILE --authorization-file NEW_PRIVATE_FILE
--identity bootstrap disable-pending
--identity reconcile-configuration
--identity revoke-sessions
--identity maintain
--identity reconcile-restore --decision-file ABSOLUTE_PRIVATE_FILE
```

Every command uses the existing supervisor/admitted child, exclusive stopped
lease, verified installation/provider identity, finite total 120-second
maintenance envelope and safe bounded outcome projection. Verify actual child/
descendant termination before releasing authority or retrying; no raw child
output or private file paths enter the supervisor IPC. Normal serving never
inherits offline provisioning authority. A direct/internal worker invocation,
foreign scope, live host, lost lease or uncertain cleanup fails explicitly.
Existing 003 acquisition/operation maxima remain clipped by the parent budget.

### Prepare and upgrade

`prepare` first verifies registry format 1 or an already complete format 2.
Table-free v0 requires explicit existing `--registry upgrade` before this
command; do not silently bootstrap/repair it. Preflight full installation
ownership, exact known old schema/history, module inventory and key-directory
ownership before writes.

Provider-specific migration 2 is transactional in the host store, appends its
digest/history and creates the eight identity tables/constraints.
Initial metadata and disabled bootstrap commit locally; existing installation
ID, module mappings/allocator markers/revision sum and module data/history
stay exact. Repeated prepare of verified current state is idempotent.
It never resets consumed bootstrap or switches persisted identity mode.

Key preparation is a separate protected filesystem effect, not falsely atomic
with the database. Validate the intended ring first, create only in an explicitly
owned new location, and record/reconcile each completed side. A crash after
DB commit but before key readiness leaves serving blocked; repeated explicit
prepare verifies the committed schema and completes the owned ring, never
recreates identity rows. A wrong/missing/foreign existing ring requires explicit
reconciliation, not blind overwrite.

Normal serving verifies prepared schema/physical inventory/history and
usable key material read-only before accepting protected requests. It cannot
run registry/identity migrations, auto-create missing tables, repair corruption,
reset bootstrap or silently regenerate lost keys. Supported prior format
returns `identity_prepare_required`; incompatible/corrupt state fails closed.
Both the empty host builder and composition worker integrate the same checks.

### Nominate and enroll

The nominee file is strict private JSON `{version:"1",issuer:string,subject:string}`.
Values are exact production identity keys learned/verified by the operator,
not an email/address label. No real nominee/example personal data is committed.
Authorization requires existing identity-ready store, OIDC mode and never-
consumed bootstrap. Replace only an unconsumed ceremony, increment generation,
then store a digest bound to installation/generation/nominee and fifteen-minute
expiry.

Generate a cryptographically random 256-bit secret and write it to a new
owner-only no-symlink authorization file. Output only fixed outcome/correlation;
never print the secret, nominee or file path. File delivery is not atomic with
the database: create/write/flush the private file before committing its digest;
if commit/delivery is uncertain, report uncertainty, keep the evidence and use
explicit stopped replacement to invalidate that generation. Do not claim
successful delivery merely because a file exists. A known committed digest
with intact authorized file is reconciled by explicit operator inspection.

The operator securely delivers this file/secret to the nominee outside public
Köfte surfaces. The nominee logs in as that exact OIDC identity and submits
the secret only through the protected enrollment form/API. One owner
transaction persists designation/consumption/event; delete the transient file
securely under operator policy after completion/expiry. Do not put it in a
return URL or prefill/render it into an HTML page.

`disable-pending` erases unconsumed pending authority under the same lease;
it cannot remove/reopen consumed state. Replaying config/authorization files,
upgrades/restart or an absent administrator never creates another initial admin.
Concurrent eligible attempts must admit twenty contenders while committing
exactly one designation/consumption/event. A disconnected reply is uncertain;
verify `/me` and operator state on a fresh attempt, never mint a replacement.

### Sessions, mode and keys

`revoke-sessions` advances epoch and durably revokes local sessions/attempts,
with an operator security record. The next protected request must reject all
old tickets. `reconcile-configuration` explicitly acknowledges a stopped mode
or issuer/client/public-origin/network-trust change; revoke old sessions/attempts
and unconsumed pending authorization before updating the trusted-configuration
digest/mode. Startup refuses an unacknowledged change. The digest excludes
display values and rotatable secret contents. Preserve user mappings and
consumed production enrollment; matching
development profiles do not become OIDC identities. No command grants a
development user production designation.

Persist the valid private key ring across normal restart, including readable
retired keys needed by unexpired cookies/state. Default framework key rotation
(90-day key lifetime) is not session revocation. Rotate certificate/client
secrets through private references with documented overlap: retain old key
decryptors until the longest active session/attempt is invalidated/expired;
client-secret replacement affects future code exchange, not existing local
session identity. A leaked session requires revocation/epoch change, not just
generating another key.

The current encryption pair protects newly generated keys. Explicit retained
pairs decrypt existing keys only; they never become the default encryptor.
Keep every decryptor required by the retained ring, even after its certificate
or protected sessions expire: opening verifies **all** retained keys. There is
no automatic key deletion, certificate retirement or rewrapping. Removing a
needed reference refuses startup without repairing or replacing the ring.
Existing single-pair configuration and PEM interpretation are unchanged.
Stopped revocation/maintenance still validate their operation-specific subset
when private decryption material is unavailable.

Lost/undecryptable key ring fails closed. Stop, revoke old sessions/attempts,
explicitly provision a new owned protected ring and restart; users sign in
again, mappings/designation stay stable. Do not delete identity data.

## Recovery and compatibility

Recovery format `kofte-storage-recovery-v2` serves platform format 2; retain
v1 handling only for actual format 1. Preserve exact all-module artifact
inventory/hashes and full platform DB/PG dump. V2 adds the private
[identity security summary](../data-model.md#key-material-and-recovery-model);
its verification reads actual restored data/constraints/history.
Do not broaden v1 to accept arbitrary identity/key files. Platform-only legacy
backup/reset remains refused for any registry/module/identity installation.

Keys/client/certificate secrets are deliberately outside ordinary recovery
artifacts. Restore invalidates all sessions rather than promise key continuity;
document separate operator secret custody. Full durable users/designation/
security records remain in the platform artifact. Normal startup validates
the known key ring rather than treating secret loss as a new installation.

Restore v2 into a fresh stopped quarantine target via the accepted complete-set
workflow. Do not overwrite a running installation or expose a restored copy
before reconciliation. The restore path durably writes an
`identity-restore-required` control marker in the separately owned key directory,
outside the recovered snapshot, before activation; startup refuses that marker.
`reconcile-restore` verifies manifest/actual ownership, acknowledges prior
security history from the private operator decision file, advances epoch,
invalidates restored sessions/attempts/admission/pending ceremony, establishes
fresh protected key material and records the decision before removing the
quarantine gate. The decision file is strict private JSON with `version:"1"`,
`recoverySetId`, `installationId`, `expectedManifestSha256` and
`priorEnrollment` (`known-consumed`, `known-never-enrolled` or
`not-established`). These bind the
decision to the exact verified recovery point; no nominee or replacement
administrator field is accepted. Historical uncertainty cannot be resolved by
claiming the snapshot was fresh. Interrupted reconciliation leaves it gated
and resumable.
No attacker/ordinary browser can submit this decision.

Preserve verified consumed enrollment exactly. Only an explicit
`known-never-enrolled` decision grounded in operator-held history may leave a
verified unconsumed installation disabled for later deliberate nomination;
it cannot override a recovered consumed row/event. This is trusted operator
attestation, not rollback-proof evidence supplied by the snapshot. If history
is not established or a historical consumed designation cannot be recovered,
set `recovery_locked`, not "fresh pending".
The operator must reconcile against trusted external history; 005 provides no
generic admin reassignment/account-link/reset command. If the original IdP
identity is lost, recover that same issuer/subject at the IdP or obtain a
separately reviewed recovery design. Refuse exposure where committed authority
cannot be reconciled safely. Do not claim the snapshot can detect every
historical rollback or immediately revoke a copied installation.

Direct OS/database copying bypasses the supported restore marker and is outside
the authority guarantee; operators must fence the old instance and keep the
copy offline. Document this limitation explicitly rather than claim rollback-
proof storage. A v1 recovery point requires explicit upgrade/reconciliation
before identity serving. Old binaries must refuse platform format 2; rollback
uses a compatible complete recovery point, never Down migration/table deletion.

## Evidence, retention and privacy

Each login/callback/session/enrollment/operator boundary emits a generated
correlation and fixed operation/outcome/corrective code with honest
`unchanged`, `committed` or `uncertain` durability. Composed and empty hosts
share this emitter; extend the allowlisted composition IPC projection for
safe identity categories instead of dumping raw worker console output.

Owner-local user creation/enrollment/operator security changes require durable
events in the same transaction. Retain these for the installation's identity
lifetime; session mutation records for 30 days. External diagnostic retention
is a restricted operator responsibility, default 30 days with bounded rotation.
No public log/audit browsing/export surface is added. Capacity/high-water/
pruning/failure semantics are fixed in the data model.

Allow only generated internal user references in restricted durable records
where needed; public errors and metric dimensions contain none. Do not collect
raw HTTP URLs/queries/bodies, auth headers/cookies, OIDC responses, state/code/
PKCE/nonce, one-time authorization, private paths/config, exception messages or
unnecessary profile data. Apply this to .NET/Next/ingress/access logs,
OpenTelemetry HTTP instrumentation and browser artifacts; use explicit
sanitized spans, not blanket protocol capture. Required exchange values may
travel only within their protected authentication/session exchange.

Captures seed canaries and independently inspect responses/redirects/pages,
logs, traces, metrics, IPC and durable events. Keep raw transient test buffers
private, scan then discard; screenshots/HAR/traces must not persist protocol
secrets. A sink/capture failure is a failed check. Diagnostic failure never
changes a failed/uncertain mutation into a reported success.
