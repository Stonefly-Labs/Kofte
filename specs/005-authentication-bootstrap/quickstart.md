# Runtime and Validation Guide: 005 Authentication and Bootstrap

**Status:** Implementation in progress, not accepted or production-ready.
The host, identity page, stopped commands and test-owned consumer are delivered
source, not proposed interfaces. The [implementation record](implementation.md)
identifies exact executed commands, assessed sources and remaining failures;
the command inventory below is not itself a passing complete gate.
[ADR 0007](../../docs/adr/0007-authentication-bootstrap-boundary.md) has explicit
human design approval, separate from feature acceptance.

## Prerequisites and safety

Use exact tools/locked restores from the
[local guide](../../docs/development/local-development.md):
.NET SDK 10.0.402/runtime 10.0.13, Node 24.22.0/npm 11.19.1 and Python 3.11+.
SQLite uses native owned private roots. PostgreSQL evidence requires the
existing pinned disposable fixture and Docker; it must never use an operator's
database. Keep PostgreSQL ownership/credential/CREATE-denial checks intact.

Do not install an IdP, product package or toolchain merely to read this guide.
Use the reviewed OIDC/transitive closure, central pins, locks and notices;
restore through the existing mechanisms rather than introducing new versions.
The test-owned issuer binds an owned loopback HTTPS port with an isolated test
CA, real signed protocol messages and no production certificate-validation
bypass. Browser tests use actual host/web/issuer processes. Synthetic identity/
secret/correlation fixtures are generated per run, never committed credentials.

The explicit operator setup is:

1. Generate a private explicit v1 config with either development mode or exact
   production OIDC trust; use the [operator contract](contracts/operator-v1.md).
2. Stop the owned host. Initialize a genuinely empty store using the existing
   provider workflow, explicitly upgrade registry v0 to v1 where needed, then
   execute `--identity prepare`. Observe verified format 2 and
   private key readiness, not just a zero exit code.
3. Start the native host/web with `KOFTE_AUTH_CONFIG_FILE` set on both required
   processes/forwarded workers. Health stays anonymous; `/me` is 401 until
   deliberate login. This slice does not make `test-postgres` a general
   production database profile.
4. In OIDC mode log in as an ordinary user and verify no initial designation.
   Stop, authorize the exact nominee using private input/output files, restart,
   log in as that nominee and submit the one-time authorization through the
   protected form. No command prints the secret; no manual database edit.
5. Verify `/me`, sign out, retry expired/replayed/other-user authorization and
   exercise documented correction. No anonymous admin endpoint or second
   initial administrator is created.

These steps are exercised through the delivered test-owned
consumer and fixture-owning verifier; do not substitute a mocked principal or
an unexplained private database patch.

## Required local and CI verification

The following existing commands include the identity
projects/cases; do not add an optional skip or let
missing fixture source silently disappear from the required suite inventory.

```bash
python3 scripts/check_repository.py
python3 -m unittest discover -s scripts/tests -v
git diff --check
python3 scripts/verify.py --suite quality
python3 scripts/verify.py --suite unit
python3 scripts/verify.py --suite integration --provider sqlite
python3 scripts/verify.py --suite integration --provider postgres --disposable-postgres
python3 scripts/verify.py --suite recovery --provider both --disposable-postgres
python3 scripts/verify.py --suite contract --provider both --disposable-postgres
python3 scripts/verify.py --suite browser --disposable-postgres
python3 scripts/verify.py --suite privacy --provider both --disposable-postgres
python3 scripts/verify.py --suite startup --warm-starts 10
python3 scripts/verify.py --suite failure-propagation --disposable-postgres
python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10
```

Quality retains exact-version/locked dependency/notice checks, .NET
format/build/warnings, web Prettier/ESLint/type/build, generated contract and
Python/repository checks. Regenerate the existing shared OpenAPI/types with
`npm run generate --workspace packages/bootstrap-contract` after deliberately
adding the identity operations, then `npm run contract:check`.
Health bootstrap-v1 remains byte/shape compatible.

Named focused suites help iteration; final implementation evidence must
reconcile the complete local run and all actual CI contexts at the assessed
head. Record commands/outcomes/environment, not a command list labeled "green".
If 004's known PostgreSQL/private-diagnostics or incomplete-cleanup observation
recurs, capture its exact assertion and phase evidence before attributing a
cause; never remove it, increase a deadline or replace the failure with a rerun.

## Acceptance matrix

All 22 scenarios are covered explicitly below; each row also identifies the
applicable requirements/outcomes. Real-store rows execute on both providers.
Use independent exact IDs, sentinel data, event/revision/history comparisons
and captured effects, not only status codes or positive counts.
FR-022 applies to the complete matrix and retained gates: required independent
protocol, real-provider, browser, contract and privacy evidence cannot be
replaced by a unit-only or partial passing selection.

| Scenarios | Required observation / suite |
| --- | --- |
| US1.1 | Actual browser discovery/authorize/code+PKCE/token/JWKS flow, exact `/me`/UI/consumer identity equality and provider credentials confined to the exchange; integration/contract/browser. FR-001/002/004/021; SC-001/003. |
| US1.2 | Concurrent first login + changed safe profile + three fresh restarts preserve exact internal ID/designation; no email/profile identity key. Integration/contract, FR-003/004/016; SC-001/006. |
| US1.3 | Same profile/different subjects and identical subject across sequential configured issuers remain distinct, with no inherited designation. Explicit trust-change session reconciliation. Integration/contract, FR-003/007/014; SC-001/003. |
| US1.4 | Independent signed wrong-issuer/audience/key/algorithm/time/nonce/state/PKCE cases; unsolicited/replayed/duplicate callbacks and concurrent attempt consumption create no unauthorized session. Integration/contract/browser, FR-002/008/019; SC-003/007. |
| US1.5 | Cancellation, invalid config, provider/discovery/JWKS/token outage, unknown key and response/time/request caps fail boundedly; corrected retry requires a fresh login, no redirect loop/dev fallback. Integration/browser/failure-propagation, FR-015/018/019; SC-003/005/007. |
| US2.1 | Fresh disabled ceremony + ordinary valid login cannot enroll; no public nominee/history/secret disclosure. Integration/contract/privacy, FR-010/018; SC-002/003/007. |
| US2.2 | Delivered stopped nominate/file delivery + exact signed-in nominee + protected submission commits one designation/consumption/event in same store. Integration/contract/browser, FR-010/011/017; SC-002/007. |
| US2.3 | Non-nominee, development, forged, expired, replaced-token and replay attempts leave exact state/events unchanged; safe non-enumerating errors. Integration/contract/privacy, FR-010-014/018; SC-002/003/004. |
| US2.4 | At least twenty simultaneous eligible HTTP contenders; real child crash barriers before commit, after commit and before reply; exactly one durable designation/consumed generation/event or neither, never success on uncertainty; three fresh host restarts. Integration/recovery/contract, FR-011/015/017; SC-002/006/007. |
| US2.5 | Replay original authorization/config, restart/upgrade and unavailable admin cannot reopen. Explicit pending replacement works only before consumption; IdP-loss and stale-backup recovery never nominate another initial admin. Recovery/contract, FR-012/016; SC-002/006. |
| US3.1 | Direct and Next-adapted `/me` plus test-owned operation deny anonymous/forged/bearer/mixed credentials with 401 and no provider HTML/redirect/protected data. Contract/browser, FR-005/006/008; SC-003. |
| US3.2 | UI/direct/adapter/separate consumer compare exact ID/mode/designation; spoofed headers/body/profile cannot select a user or permission. Contract/browser, FR-004/007/021; SC-001/003. |
| US3.3 | Non-granted test-owned action stays 403 even for first admin; cross-origin/null/missing Origin, CSRF pair/type confusion and hidden UI cannot cause effects. No production test route. Integration/contract/browser, FR-005-008; SC-003. |
| US3.4 | Durable sign-out, idle/absolute boundaries, renewal/revocation race, wrong-mode/epoch, key loss and ordinary rotation/restart behavior; next-request denial and stale UI cleared. Integration/contract/browser, FR-008/009/014/015; SC-001/003/004. |
| US3.5 | Exact anonymous health methods/schema, Development-only OpenAPI and unknown-route rules preserved; two-user cache/SSR/adapter isolation. Contract/browser/privacy, FR-005/008/018; SC-003/007. |
| US4.1 | Explicit local synthetic identity selection agrees across UI/API/consumer with persistent visible Development label and no implicit administration. Unit/contract/browser, FR-013/014/020; SC-004/005. |
| US4.2 | Negative environment/listener/origin/backend/exposure-switch combinations reject before serving/login; no deployed test/TLS bypass. Unit/integration/contract, FR-013/015/019; SC-004. |
| US4.3 | Persist dev users/tickets, reconcile to Production OIDC and restart; old tickets denied, equal profiles not merged, production ceremony untouched. Integration/contract, FR-003/010/014; SC-004/006. |
| US5.1 | Real clean prepare, exact v1-to-v2 upgrade and idempotent current prepare; old/unknown/corrupt schema/history refusals, read-only serving, all original module ID/assignments/sentinels/history/revision sums unchanged. Integration/recovery/contract, FR-003/016; SC-006. |
| US5.2 | Store unavailable/corrupt, migration/key preparation interrupted, lost leases, commit uncertainty, complete v1/v2 recovery/restore quarantine and interrupted reconciliation. Corrected public recipe retains committed identities/consumption/module data; no manual DB edit. Integration/recovery/failure-propagation, FR-009/011/012/015/016; SC-006. |
| US5.3 | Capture successful/rejected login/enrollment/session expiry/sign-out and operator actions across public responses/redirects, pages, logs/spans/metrics/IPC/durable events. Seeded canaries excluded outside protected exchange; truthful correlation/durability, record capacity/retention/sink failures and zero other-user profile disclosure. Privacy/contract/integration, FR-017-019; SC-007. |
| US5.4 | Every identity state at 320px/200% text, keyboard/focus/announcements, axe zero serious/critical, safe local return and correction/retry. Measure provider-return to actionable UI <= five seconds on documented actual profile, provider interaction separately. Browser, FR-020; SC-005/007. |

Boundary corpus additionally covers numeric limits in identity-v1 at
limit/one-over except the human-rejected installation session boundary, and
every exact method/path/body/header/cookie/redirect allowlist,
including individual Set-Cookie preservation and no automatic proxy retry.
Record actual request counts/bytes/time and no effects after timeout.
Required audit-capacity failure rolls back its owner mutation; expired record
pruning never deletes installation-retained events.
The [ADR amendment](../../docs/adr/0007-authentication-bootstrap-boundary.md#human-directed-verification-scope-amendment-installation-session-capacity)
removes the 50,000-session proof, exclusive seed helpers and numeric corpus
cases from both providers. It is not skipped or replaced; the runtime cap is
unchanged and is not claimed verified. Historical results remain historical.

## Delivered guide consumer

The separately compiled executable `tests/contract/Kofte.Identity.Consumer`
has an explicit scenario catalog. It owns private host/web/issuer/provider
fixtures and exits nonzero if expected observations are missing, source is
absent, cleanup unverified or a scanner fails. Required scenarios:

| Scenario | Required delivered recipe |
| --- | --- |
| `oidc-login` | Browser code flow, exact current identity/parity and stable profile-change mapping. |
| `trust-refusal` | Independent negative evidence, duplicate/replay and bounded corrected retry. |
| `nominated-enrollment` | Actual stopped nomination/file delivery + protected completion; no config/first-arrival shortcut. |
| `enrollment-race` | Twenty eligible contenders, exact atomic state/event and three process restarts. |
| `session-boundary` | Direct/adapter/consumer parity, 401/403, CSRF, expiry, durable sign-out and revocation race. |
| `development-isolation` | Explicit local success, all exposure/environment refusals and persisted profile transition. |
| `upgrade-preservation` | Clean/v1/v2 preparation plus exact unrelated module sentinel/history ownership. |
| `interrupted-recovery` | Actual commit/prepare barriers, uncertain reply and stopped complete-set restore/reconciliation. |
| `private-evidence` | Useful correlated safe outcomes plus canary scans of all channels and recording failures. |

After locked restore, the Release solution build, the production Next build
and pinned Chromium installation, invoke a specific recipe or `all`:

```bash
dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario oidc-login --provider sqlite
dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario all --provider sqlite
KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario all --provider postgres
```

The owning native guide harness executes all nine individually and `all` for
each provider, requiring every exact ordered observation. PostgreSQL uses only
the pinned owned disposable server, with independent databases and roles;
whole-server failure cases use a dedicated server. Ordinary local development
still needs neither Docker nor a real IdP.

**Executed catalog qualification (T094):** the 77-case
`*IdentityConsumerHarnessTests` selection passes in 4.541s; the ten-case
`*SqliteIdentityGuideTests` and `*PostgresIdentityGuideTests` selections pass
in 492.959s/654.153s with zero skipped and each required class present.
Each covers the nine rows above individually plus `all`, invoking the DLL
through `IdentityConsumerHarness` and comparing every exact ordered observation
against `IdentityScenarioCatalog`, not trusting a zero exit or aggregate count.
The [implementation evidence](implementation.md#t094-t095-complete-public-catalog-and-delivered-documentation)
records exact commands, unchanged bounds, source/profile and safe report.
Browser/a11y qualification is independent; this catalog is not the final gate.

### Earlier implementation evidence (historical)

The pending-task/provider statements below describe those earlier checkpoints.
T092/T094 now qualify both-provider operations and the entire catalog; historical
failures stay failed. Use the delivered recipes above for current execution.

`development-isolation` is now delivered for execution after the existing
Release/Next builds and pinned Chromium setup:
`dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario development-isolation --provider sqlite`.
Its actual SQLite run validates all 88 exposure refusals, visible browser/API/
actor equality and pending/consumed persisted transitions, with 135 independently
checked named observations. Hosted `c5e65f2` also passes the exact frozen catalog
on SQLite and PostgreSQL, closing T078; see [implementation evidence](implementation.md).

Hosted `49ef96e` now also passes the strengthened `session-boundary` command
on each provider with all nineteen ordered observations independently checked.
Two-user SSR/DOM isolation, invalid-Origin/ungranted denial, expiry and
sign-out/uncertainty close T066/T067/T069/T070. Consolidated real format,
preparation and read-only-startup preservation closes T079.
Recovery-v2 backup/actual-state verification has passing SQLite and hosted
`f52150f` PostgreSQL coverage. Separately named test-owned quarantine restore
and strict private decision parsing now pass actual SQLite preservation/binding/
tamper/refusal tests; their new PostgreSQL proof remains pending. The marker
blocks all six ordinary stopped commands. The separate `reconcile-restore`
command now has actual SQLite v1/v2 activation, fresh protected keys, immutable
private decision/epoch/ring intent and killed-worker retry evidence. Historical
uncertainty stays locked; recovered consumed enrollment wins over a contrary
attestation. New PostgreSQL reconciliation and remaining partial-write/key/
public operations recipe evidence are still pending. See the
[delivered command and custody limits](../../docs/development/local-development.md).
Identity-bearing sets continue to refuse the legacy ungated restore path.
Actual SQLite key-custody tests now also cover live missing/foreign/undecryptable
rings, stopped original-custody correction and epoch revocation, actual retained
key/certificate overlap, coordinated client-secret correction and recovery
artifact separation. The optional private `keyDecryptionCertificates` member
retains old decryptors while the separate current pair encrypts new keys;
see the local guide for bounds/custody/retirement limitations. New PostgreSQL
key evidence is pending, so T083 remains unchecked.
Sixteen actual SQLite restore cases now also prove explicit retry after empty/
partially flushed intent, key-inventory and committed-result journals, plus
unchanged refusal of complete conflicting/unknown/invalid pending state.
Live installation-specific exclusive authority is required for partial-scratch
cleanup; staging-key and new PostgreSQL journal proof remain pending.
This progress does not make the remaining operations recipes executable or
turn the non-green full matrix into accepted feature evidence.

The built host also now accepts stopped `--identity maintain` on a prepared
installation: at most 128 expired session-evidence rows, terminal attempts and
expired admission windows per category/invocation. Repeat explicitly for
another batch; live-host refusal and preservation have passing SQLite evidence.
Hosted `20d751c` PostgreSQL host/storage and both-provider recovery also pass
the new cases, closing T087. This does not deliver
the recovery/operations consumer recipes above.

The contract suite must also execute each scenario individually for both real
providers through its owning harness and validate named observations, not
trust a summary count. The new project/source is test-only; no product reference,
auth bypass flag or fixture endpoint is deployed. Browser/a11y timing remains
actual Playwright evidence, not replaced by the consumer's JSON assertions.

## Limits and completion gates

Only prepared configuration and custody permit serving. Production OIDC
requires exact HTTPS trust and a protected client secret; ingress must preserve
the admitted public origin without trusting arbitrary proxy headers. Session
revocation is local, not provider-wide logout. No administrator reassignment,
IdP-loss reset, arbitrary OS-copy detection or old-instance fencing is supplied.
Complete-set backups do not contain private key custody; restored identity is
quarantined until a manifest-bound stopped decision and fresh protected ring
are committed. Unknown consumed history stays locked. See the
[operator contract](contracts/operator-v1.md) and
[local guide](../../docs/development/local-development.md) for private files,
capacities, retention, key/certificate overlap and explicit correction.

Specs 006–009 and 022–024 remain future work; authentication/designation is not
a permission engine, team model, MCP deployment or full audit subsystem.
The current stage remains **implement** on the same branch/draft PR until the
complete local gate, assessed-head CI and traceability genuinely pass.
Converge, final review and explicit human acceptance are separate later stages.
Reviewer-owned checklists are unchanged; no intermediate merge is requested.
