# Validation Guide: Revised 005 Accounts and Initial Ownership

**Status:** Future implementation/acceptance guide, not delivered new behavior.
No v2 command, recipe, migration or passing runtime result is created by planning.
Current old-scope operation remains documented in the
[local guide](../../docs/development/local-development.md);
the [original quickstart](https://github.com/Stonefly-Labs/Kofte/blob/f10351a599191a5931b05a719cd8d291eba839fb/specs/005-authentication-bootstrap/quickstart.md)
and [implementation record](implementation.md) retain historical proof.
The [plan](plan.md), [data model](data-model.md) and proposed
[identity](contracts/identity-v2.md) /
[operator](contracts/operator-v2.md) /
[account-admission](contracts/account-admission-v1.md) contracts are the target.

## Prerequisites and safety

Use the existing pinned .NET SDK 10.0.402/runtime 10.0.13, Node 24.22.0/npm
11.19.1, Python and locked restores. Do not install an IdP/toolchain/product
package just to read this guide. Keep current Playwright pin/exception and
dependency/license/notice gates. Exact framework/store compatibility is a
first implementation gate, not established by public documentation.

Own every target/root/process/port/key/certificate. SQLite uses native private
roots; PostgreSQL uses only the existing pinned disposable fixture, never an
operator database. Real OIDC uses a test-owned HTTPS issuer/CA with signed
protocol evidence and no production TLS bypass. Browser evidence requires real
host/web/issuer processes, not fabricated principals. Keep private captures
outside Git; do not copy credentials or verifier databases into PR evidence.

**Do not run heavy product/Docker suites on the shared Mac.** Full/provider
qualification belongs on an owned disposable/hosted runner. Never run, replace
or claim the 50,000-session boundary test. Hosted Linux does not establish a
final-source macOS qualification. Preserve all retained deadlines/assertions,
owned cleanup and actual failed observations.

## First implementation admission proof

Before broad account implementation, use the actual locked shared-framework
assemblies with the proposed five-interface store adapter. On each real provider,
prove framework create/find/name normalization, check-only login/lockout/rehash,
password change/reset/stamp rotation and external login add/remove all enlist in
one coordinator transaction. Assert account/session/Owner/consumption/evidence
atomicity; no manager/store commit or early cookie; rollback on required-event
failure, ignored framework rehash-update failure and unknown-commit handling.
Measure the unchanged one-second local
budget including hash work, contention, cancellation and cleanup, and the
four-second OIDC budget. An API/source fit alone does not pass.

Use existing owning projects, not a new prototype app or production-linked test:

```bash
dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj --configuration Release --no-restore --filter FullyQualifiedName~Identity
dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj --configuration Release --no-restore --filter FullyQualifiedName~Identity
dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj --configuration Release --no-restore --filter FullyQualifiedName~Identity
```

These are **post-implementation** selections, not commands run for this plan.
Provider fixtures must own their targets; PostgreSQL selection needs the
documented disposable admission environment. Require named new tests to exist/
execute and zero skips; the filter alone cannot prove revised coverage.
Failure blocks store adoption; do not lengthen deadlines, bypass managers or
quietly fall back to another authentication implementation.

## Operator and browser journeys after implementation

Use the existing built Host DLL path/launch environment from the local guide;
the following argument sets are proposed operator-v2, not current CLI support.
Private files below mean actual operator-owned absolute paths, never committed
example credentials. Do not pass a token/password as a command argument.

1. Prepare strict v2 production configuration and persistent protected keys with
   local-only, OIDC-only or coexistence. Stop the owned installation; explicitly
   upgrade registry-v0 if needed and run `--identity prepare`. For delivered
   format 2, include `--legacy-connections-file` with exact private issuer mapping.
   Verify format-3 inventory/history and unchanged module/user/Owner evidence,
   not merely command exit. No password is manufactured for old external users.
2. Before any ownership has been consumed, run
   `--identity setup authorize --authorization-file` with a **new private file**.
   Deliver privately, start the owned host/web behind required production HTTPS,
   open `/identity`, submit the token and complete local creation or selected
   OIDC login. Observe one matching browser/API actor with `isInitialOwner:true`,
   one consumed ceremony/event and no cookie before commit.
3. Sign out/re-enter with either explicitly associated enabled method; compare
   exact stable user ID. Attempt unknown post-setup local/OIDC admission, forged
   flags/references and Owner-only administration. All must fail without a new
   account or unavailable 006 permissions. Test each disabled method explicitly.
4. Change the local password using fresh proof; verify the old password and
   **the very next protected request from every old session** are refused.
   Exercise five failed attempts/fifteen-minute lockout across all local proof
   entry points; independently enabled OIDC is not automatic fallback.
5. Request reset instructions; observe the same operator-assistance response
   for existing/unknown/ineligible names, with no promised mail/queue.
   Operator verifies requester privately and runs
   `--identity password-reset authorize --account-file ... --authorization-file ...`.
   Deliver privately, redeem with the new password, re-enter as the same user;
   verify replay/wrong-account/expiry/replacement and old-session denial.
6. Freshly reauthenticate, explicitly add the other enabled method, then sign
   in again. Require original-account plus new-method proof, reject another
   user's association/email match and final-usable-method removal.
7. Exercise stopped trust/secret/key reconciliation, session revocation and
   complete-set restore/quarantine correction through documented commands.
   Consumed Owner never reopens. Lost IdP/Owner access has no new-owner shortcut;
   unresolved history remains locked and general administration remains 006.

The public consumer below must automate these actual journeys with independent
observations and no undocumented database patch. Testing can seed/corrupt owned
fixtures through private test controls, never a shipped endpoint/bypass flag.

## Runnable verification commands after implementation

These existing runner entry points must be updated to require new v2 inventories;
unchanged old-source passes are not new-scope evidence.

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

Quality retains .NET format/build/warnings, web formatting/lint/type/build,
locked dependency/provenance/license/notice checks, generated contract checking
and repository/tooling tests. Regenerate deliberately changed OpenAPI/types with
`npm run generate --workspace packages/bootstrap-contract`, then
`npm run contract:check`; health bootstrap-v1 must stay unchanged.
Do not install/restore unnecessarily before actual missing-dependency failures
or approved manifest changes.

The disposable hosted complete entry remains:

```bash
gh workflow run product-checks.yml --ref feature/005-authentication-bootstrap -f suite=complete
```

It executes the exact all-mode command in one job; ordinary `suite=all` and PR
trigger retain the independent ten-job matrix. Record exact source/OS/commands/
named steps/multiplicity/reports and actual CI outcomes; no list labeled green
without results, passing rerun as diagnosis, missing class, skip/retry/flaky
browser result, relaxed bounds or unverified cleanup.

## Proposed version-2 public recipe catalog

Extend the existing separately compiled `Kofte.Identity.Consumer`, not product
modules. Require explicit `--contract-version 2` in the consumer and owning
verifier/harness; old binaries/catalogs must fail, not produce a false v2 pass.
These **target commands are not executable against current old consumer code**.
After locked restore, Release/production web builds and pinned Chromium setup:

```bash
dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --contract-version 2 --scenario local-accounts --provider sqlite
dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --contract-version 2 --scenario all --provider sqlite
KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --contract-version 2 --scenario all --provider postgres
```

| Required recipe | Independent observations |
| --- | --- |
| `local-accounts` | Real production local-only/coexistence creation/login/change/lockout; stable identity, disabled-method/TLS/development refusal. |
| `oidc-login` | Real named issuer discovery/authorize/PKCE/token/JWKS, exact stable identity, profile changes and explicit connection bindings. |
| `trust-refusal` | Wrong issuer/audience/signature/time/state/nonce/PKCE, unsolicited/replayed callbacks, forbidden-but-allowlisted DNS/peer and corrected fresh retry. |
| `setup-ownership` | Actual private stopped token delivery, browser admission and local/OIDC claim; token alone and foreign/expired/replaced admission fail. |
| `ownership-race` | At least twenty eligible mixed-method contenders, exact one Owner/account/session/consumption/event, no losing unknown accounts; commit barriers and three fresh restarts. |
| `credential-reset` | Actual private operator-assisted delivery, one-use account-bound reset/change, same ID and next-request session invalidation; no external-only password. |
| `method-association` | Fresh original/new proof, same canonical user, cross-user/auto-link/replay/final-method refusal and security-version invalidation. |
| `account-admission` | Default-denied Owner/ordinary/forged reference, test-host-only authorized continuation, unique/idempotent account effects and no role/team grant. |
| `actor-compatibility` | Separately compiled actor-v2/direct/adapter equality; explicit v1 endpoint/DI refusal, no local-as-OIDC coercion. |
| `session-boundary` | Fresh durable account/stamp/epoch/trust, 401/403/503, CSRF, sign-out/expiry, renewal/security races and two-user cache isolation. |
| `development-isolation` | Explicit synthetic success and visible labeling; every non-development/exposed/profile-change refusal, no production authority. |
| `upgrade-preservation` | Real format-1/2-to-3/idempotent preparation, exact user/external/Owner/module/history preservation and old ticket/nomination retirement. |
| `interrupted-recovery` | Real prepare/credential/claim commit and private-file/key barriers, uncertainty/quarantine/history-bound complete restore correction. |
| `private-evidence` | Seeded password/setup/reset/session/protocol canaries, every public/private channel, truthful diagnostics/events/capacity/sink/cleanup failures. |

Run all fourteen individually **and** `all` on each provider, comparing exact
ordered observations and version, not exit/count alone. Keep existing owning
recipe/harness timeouts (120s individual/600s aggregate), bounded inputs and
fixture cleanup; share owned fixture setup where safe, never relax assertions/
deadlines to accommodate extra recipes. Old nominated recipe names are retired
explicitly and historical reports are not rewritten.

## Acceptance matrix

Suite keys: **I** real-provider integration; **C** direct/adapter/separate-consumer
contract; **B** actual browser/a11y; **R** recovery/crash; **P** privacy capture;
**F** retained failure-propagation. Both real stores apply to persistence rows.
FR-022 applies to every row, and SC-007 to captured security evidence.

| Scenario | FR / SC | Required observation and suites |
| --- | --- | --- |
| US1.1 | FR-001/004/023; SC-001 | Enabled local/OIDC/coexistence identity equality and real credential/protocol proof. I/C/B |
| US1.2 | FR-003/004; SC-001/006 | Same exact user after restart/profile change/explicit association. I/C/B |
| US1.3 | FR-003/026; SC-003/008 | Equal profile/different key never merges/admits; exact issuer/subject ownership. I/C |
| US1.4 | FR-002/008/019; SC-003 | Independent invalid/replayed/unsolicited evidence; no session/effect. I/C/B |
| US1.5 | FR-015/019/023; SC-003/005 | Outage/cancel/config bounded refusal; no redirect loop/automatic fallback. I/B/F |
| US1.6 | FR-001/023; SC-001 | Each profile works, disabled methods refuse; local has production TLS/keys. I/C/B |
| US1.7 | FR-027; SC-009 | Unknown post-setup local/OIDC users create no active account/session. I/C/B |
| US1.8 | FR-003/023/029; SC-003/008 | Label/disable/trust changes do not retarget; affected old sessions fail. I/C |
| US2.1 | FR-010/018/028; SC-002/003 | No admission means no Owner; no claimant/token disclosure. I/C/P |
| US2.2 | FR-010/011/017/028; SC-002 | Actual private issue/bound login, one atomic account/Owner/consumption/session/event. I/C/B |
| US2.3 | FR-010/012/014/028; SC-002/004 | Wrong/expired/replaced/foreign/replayed/dev material has zero privileged effects. I/C/P |
| US2.4 | FR-011/015/017; SC-002/006 | Twenty mixed contenders, real commit/reply barriers, three restarts, exact singleton and no losing unadmitted accounts. I/C/R |
| US2.5 | FR-012/016; SC-002/006 | Config/token/restart/provider/Owner-loss replay preserves consumed legacy/new user ID. I/C/R |
| US2.6 | FR-015/028; SC-002/003 | Failed/cancelled login and replacement preserve only current unexpired browser admission. I/C/B |
| US3.1 | FR-024/027; SC-008/009 | Protected/trusted local creation, normalization uniqueness/policy; forged admission denied. I/C |
| US3.2 | FR-024/029; SC-008 | Fresh change preserves ID/Owner; old password and next protected old-session request fail. I/C/B |
| US3.3 | FR-025/029; SC-008 | Private reset delivery, account/expiry/one-use, non-enumeration/no vendor and immediate old-session denial. I/C/B/P |
| US3.4 | FR-025; SC-003/008 | Invalid/replay/other-user reset changes no credential/session/Owner; no OIDC-only password. I/C/P |
| US3.5 | FR-026/029; SC-008 | Dual fresh proof, unassigned-only add, no merge/final-method removal, old sessions denied. I/C/B |
| US3.6 | FR-019/024; SC-008 | Shared bounded failed-count/lockout across local entry points, safe denial/recovery. I/C/B |
| US4.1 | FR-005/006/008; SC-003 | Anonymous/forged/bearer/mixed direct/adapter deny, no HTML/provider challenge. C/B |
| US4.2 | FR-004/007/021; SC-001/003 | Exact actor-v2/UI/API identity/context/Owner parity; spoofed claims grant nothing. C/B |
| US4.3 | FR-005/007/008; SC-003/009 | Ordinary/Owner insufficient authority and CSRF/origin denial before effects. I/C/B |
| US4.4 | FR-009/029; SC-003 | Durable sign-out/idle/absolute/renewal race next-request denial; stale UI cleared. I/C/B |
| US4.5 | FR-005/008/018; SC-003 | Unchanged aggregate health/methods, private caching/two-user SSR isolation. C/B/P |
| US4.6 | FR-027/029; SC-008/009 | Reset/change/test-authorized disable/revoke/trust each deny next request; no Owner bypass. I/C |
| US5.1 | FR-013/014/020; SC-004/005 | Explicit synthetic login and persistent Development label/parity. C/B |
| US5.2 | FR-013/015; SC-004 | Environment/listener/origin/backend/unsafe-exposure negatives refuse. I/C |
| US5.3 | FR-014/016; SC-004/006 | Persisted dev users/tickets never become production local/OIDC/Owner. I/C/R |
| US6.1 | FR-016; SC-006 | Clean/foundation/delivered upgrade, exact host/module ownership/history, read-only serve. I/C/R |
| US6.2 | FR-011/015/016; SC-006 | Store/corruption/key/lease/commit interruptions and corrected recovery, no replacement Owner. I/C/R/F |
| US6.3 | FR-017/018/019; SC-007 | Safe classified correlated outcomes in all channels; zero seeded secret/profile leakage, failing sinks fail check. I/C/P |
| US6.4 | FR-020; SC-005/007 | All introduced states at 320px/200% text, keyboard/focus/announcements, zero serious/critical a11y, <=5s final-input/return result. B |
| US6.5 | FR-003/012/016; SC-006 | Exact legacy users/external/consumed Owner/modules retained; old sessions/nomination retired, no fabricated password. I/C/R |

Numeric boundary corpus covers new/retained admitted limits except the forbidden
installation-session-capacity proof. No secret-bearing screenshots/HAR/traces;
private transient buffers are scanned then discarded. Require actual measured
times/counts/bytes and no late effect after timeout, not summary totals.

## Retained failures and stop conditions

F1/T101 requires forbidden DNS/connected-peer rejection inside encompassing
approved CIDRs, including mapped/mixed cases, without contacting metadata
services. F2/T102 preserves run `41098819181`'s exact **pre-key-damage**
SQLite enrollment setup 503 versus expected 200; diagnose its phase/root cause,
then require the equivalent new setup/key-custody regression. A passing rerun,
renamed flow or old green report does not close it. T103 requires changed-source
complete qualification and traceability. If 004's deferred failure recurs,
capture its exact phase/assertion before attributing cause.

Plan completion is not implementation, current checklist approval or acceptance.
The driver remains blocked pending human design/changed quality review, task
reconciliation and analysis; no implementation or next numbered feature runs
from this guide. Later implement/converge/review/accept remain separate on the
same branch/draft PR.
