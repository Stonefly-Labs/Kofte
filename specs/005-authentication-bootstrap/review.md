# Feature 005 final review

**Result:** changes

**Date:** 2026-10-30

**Assessed feature:** `feature/005-authentication-bootstrap` at
`36a93bf16fd16022abba64e48fd1653fa5f63e28`

**PR:** [#8](https://github.com/Stonefly-Labs/Kofte/pull/8), still draft

**Method:** Read-only `spec-review` assessment of approved intent, actual
implementation and substantive assertions; caller-owned verification of Git,
hosted run provenance and downloaded safe reports. The reviewer executed no
commands and made no edits. This stage does not implement fixes or accept 005.

The implementation is substantial and the historical complete qualification is
valid. Nevertheless, one MEDIUM implementation defect and one unresolved
current qualification failure prevent a passing final review. Append-only
T101-T103 return the **next invocation to implement**, on this same branch/PR.
The original T001-T100 markers and historical reports remain unchanged.

## Findings

### F1 - Forbidden DNS destinations bypass the promised admission rule

**Severity:** MEDIUM. **Confidence:** high.

`src/Kofte.Host/Features/Identity/IdentityConfiguration.cs:289-304` rejects
forbidden addresses for IP-literal endpoint hosts. Its CIDR parser at
`:230-239` does not exclude encompassing forbidden ranges. Meanwhile,
`src/Kofte.Host/Features/Identity/Login/OidcBackchannel.cs:133-140` admits DNS
answers and connected peers through `Allowed`, whose implementation at
`:151-152` checks only configured-network membership, not
`IdentityConfiguration.ForbiddenAddress`.

A configured HTTPS hostname resolving to a forbidden address inside an allowed
CIDR can therefore reach outbound connection admission. Literal-host validation
does not cover this path. This contradicts the DNS/connected-destination and
metadata/link-local exclusions in
[`operator-v1.md:55-59`](contracts/operator-v1.md).
The finding establishes prohibited connection admission, **not** TLS bypass,
successful metadata access, credential disclosure or authentication bypass.
Existing literal-metadata and out-of-allowlist/mixed-DNS assertions do not cover
forbidden-but-allowlisted DNS answers:
`tests/unit/Kofte.Host.Tests/Features/Identity/IdentityConfigurationTests.cs:208`
and
`tests/integration/Kofte.Host.IntegrationTests/Identity/OidcBudgetTests.cs:138-140`.
The existing transport cases are in
`tests/integration/Kofte.Host.IntegrationTests/Identity/OidcBackchannelTests.cs`.
The existing forbidden-address policy itself permits loopback and ordinary
private networks when separately approved; this is not a public-address-only rule.

**T101:** Apply the existing forbidden-address policy, with IPv4-mapped
normalization, before network membership in the shared DNS/connected-peer
predicate. Add controlled IPv4, IPv6, mapped and mixed-answer regressions that
prove refusal before connection without contacting metadata services. Preserve
legitimate approved private/fixture networks, TLS checks and existing budgets.
Trace: FR-019, R9, T026/T037, constitution VI/XI.

### F2 - Current recovery qualification fails before fault injection

**Classification:** blocking failed evidence; the cause is **not diagnosed**.

Actual [run 41098819181](https://github.com/Stonefly-Labs/Kofte/actions/runs/41098819181),
recovery job `124211497414`, safe artifact `12998755111`, records:

- Assessed merge `8b5c448f6063204ab0934a29d23bb658a81194e5`.
- `recovery-sqlite-native` failed with exit **2** after **70.061 seconds**.
- Failing method:
  `Kofte.Host.IntegrationTests.Identity.SqliteIdentityKeyTests.KeyFailureCannotRepairItselfOrReplaceConsumedAuthority`.
- Safe status `expected_200_actual_503`; locations
  `tests/integration/Kofte.Host.IntegrationTests/Identity/IdentityKeyTests.cs:138,23,50`.

The call at line 23 performs enrollment setup; the key damage is injected only
at line 27. The expected-200 assertion at line 138 therefore fails **before**
key loss. This does not demonstrate broken key-loss correction or replacement
of consumed authority. It does prevent current recovery qualification; later
recovery selections did not execute.

**T102:** Preserve the failure and establish the precise theory row,
operation/outcome/durability/budget phase and cause using bounded sanitized
diagnostics in the owned hosted environment. Correct only a demonstrated
product/fixture defect, add its focused regression and restore omitted recovery
coverage. A passing rerun alone is not diagnosis. No retries, skips, weaker
assertions or larger deadlines may substitute for resolution.
Trace: FR-015/016/022, US5.2, SC-006, R12, T083/T092/T098, constitution XI.

**T103:** After both corrections, requalify the resulting source with the existing
hosted complete command and actual CI, reconcile exact coverage and update
implementation evidence/progress. Do not recertify changed source using older
green reports. No product-design or scope amendment is needed for these tasks.

## Independently verified execution evidence

| Exact command / observation | Actual result |
| --- | --- |
| `SPECIFY_FEATURE_DIRECTORY=specs/005-authentication-bootstrap .specify/scripts/bash/check-prerequisites.sh --json --require-spec --require-tasks --include-tasks` | Passed; selected the recorded 005 artifacts. |
| `python3 scripts/check_repository.py` | Passed; repository assets, documentation links, MCP parity and driver state valid. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed all 170 tests, exit 0. |
| `git diff --check` | Passed. |
| Task/report/checklist preservation comparison against assessed HEAD | Passed; all original task bytes preserved, exactly T101-T103 appended unchecked, historical reports and reviewer-owned checklists byte-identical. |
| `python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10` | Historical **pass**, [run 41072051400](https://github.com/Stonefly-Labs/Kofte/actions/runs/41072051400), source `4e16d36f5cd54fd3da3e81d6641ef1d8f0598f4f`. Ten downloaded reports, 313 passed steps, `scope=all`, every `completeGate=true`. Not rerun locally. |
| Prior assessed-head matrix | [41091559018](https://github.com/Stonefly-Labs/Kofte/actions/runs/41091559018) passes; ten downloaded reports, 313 passed steps, revision `0eb7b1959a5c89ff231035d746fd250675d77d6b`. `scope=targeted`, `completeGate=false`, not a second complete command. |
| Structured full/prior-matrix comparison | Passed exact step-name **and multiplicity** comparison; no promotion of targeted scope. |
| Qualified-source comparison | `git diff --stat 4e16d36 HEAD -- src apps packages tests scripts .github` is empty at assessed HEAD. Historical product/test/tooling evidence remains applicable, not erased. |
| Current source provenance | GitHub merge parents are main `3e416813233c9d291eb20cf72d5882dbddf3e1b6` and feature `36a93bf16fd16022abba64e48fd1653fa5f63e28`; feature/merge tree is identical, `1e0451ff8fb3b422362a6f815e5dd31472a53ebf`. |
| Current assessed-head matrix | Run `41098819181` is **not green**: recovery fails as F2. Downloaded quality/unit/browser/startup/SQLite-integration reports pass; remaining contexts were still running at this assessment. No complete/current-matrix pass is inferred. |
| Current repository CI | [41098819197](https://github.com/Stonefly-Labs/Kofte/actions/runs/41098819197) passes at feature `36a93bf`. |

Final changed-document/ledger verification is also recorded in the stage ledger
and PR. No heavy product or Docker suite was restarted on the
shared workstation. Changed-source product qualification belongs to T103, not
this read-only review stage.

## Functional requirements

Paths are relative to the repository. H = `src/Kofte.Host/Features/Identity/`;
I = `src/Kofte.Infrastructure/Identity/`;
W = `apps/web/features/identity/`;
N = `tests/integration/Kofte.Host.IntegrationTests/Identity/`;
S = `tests/integration/Kofte.Storage.IntegrationTests/Identity/`;
C = `tests/contract/Kofte.Platform.ContractTests/Identity/`;
B = `tests/e2e/`.
“Supported” means inspected source/assertions plus historical execution evidence,
not new reviewer execution or human acceptance.

| Requirement | Inspected implementation/assertion evidence | Disposition |
| --- | --- | --- |
| FR-001 | H native OIDC registration/configuration; N protocol cases; B production redirects/login | Supported |
| FR-002 | Protocol admission, durable attempts, `ValidatedLogin`; independent signed mutation/replay/commit assertions | Supported |
| FR-003 | I `IdentityUserStore`; S exact issuer/subject mapping, concurrency/profile/fresh-process cases | Supported |
| FR-004 | Current-user endpoint, actor projection, W adapter/panel; C and B exact parity | Supported |
| FR-005 | Default-denied host policy, explicit public inventory; N policy assertions | Supported |
| FR-006 | Explicit 401/403/503 and suppressed cookie redirects; C HTTP/adapter checks | Supported |
| FR-007 | Immutable actor-v1, request projection, separately compiled consumer and ungranted operations | Supported; no implicit permission |
| FR-008 | Opaque ticket, Origin/CSRF checks, safe returns/no-store; fixation/forgery/cookie assertions | Supported |
| FR-009 | Durable session store, sign-out and fresh verification; C persistence/expiry/scope cases | Historical support; F2 unresolved |
| FR-010 | Stopped exact nomination, expiring authorization, enrollment store/endpoint; N/S assertions | Historical support; F2 setup failure |
| FR-011 | Singleton-locked transaction; twenty HTTP contenders; real before/after-commit termination | Supported |
| FR-012 | Consumed-state preservation, explicit reconciliation, later-attempt and restart refusals | Historical support; current recovery incomplete |
| FR-013 | Strict Development admission, endpoint/visible selector; environment/exposure/unsafe-switch matrix | Supported |
| FR-014 | Separate mapping namespace, session mode/cookies and persisted transition assertions | Supported |
| FR-015 | Finite configuration/provider/store refusal and uncertain-commit behavior without fallback | Historical support; F2 unresolved |
| FR-016 | Host migration 2, exact physical verification, recovery-v2/quarantine, both-provider preservation | Historical support; current recovery incomplete |
| FR-017 | Transactional events and sanitized correlation; required-record rollback/immutable installation assertions | Supported |
| FR-018 | Safe projections/errors, fixed labels and channel canaries; non-enumeration/privacy assertions | Supported |
| FR-019 | Bounded inputs/state/response/time/rate/network admission | **F1: forbidden DNS exclusion missing** |
| FR-020 | W deliberate states; B keyboard/focus/live-region/320px/enlarged-text/axe/retry assertions | Supported |
| FR-021 | Actor-v1, identity-v1/OpenAPI/generated types and separate public consumer | Supported |
| FR-022 | Independent issuer, real providers/processes/browser, public consumer and strict inventories | **F1 regression missing; F2 current qualification failed** |

## Acceptance and outcome coverage

| Scenario | Inspected behavioral evidence / qualification limit |
| --- | --- |
| US1.1 | Production login, direct `/me`, adapter and actor parity |
| US1.2 | Profile changes and fresh-process mapping with unchanged designation |
| US1.3 | Exact issuer/subject separation despite equal profiles |
| US1.4 | Protocol mutations, replay/unsolicited callback refusal, atomic login commit |
| US1.5 | Outage/cancellation/configuration refusal and browser recovery |
| US2.1 | No-ceremony enrollment disabled; ordinary login creates no designation |
| US2.2 | Actual nomination, verified nominee and atomic enrollment; F2 setup failure retained |
| US2.3 | Wrong identity/mode, expiry/replacement/forgery/replay with unchanged state |
| US2.4 | Twenty HTTP contenders, real commit interruption and independent reopen |
| US2.5 | Three fresh restarts, unavailable administrator and preserved consumed authority |
| US3.1 | Anonymous/invalid direct and adapter refusals without login HTML |
| US3.2 | Exact current-user/actor parity and spoofed header/body/profile rejection |
| US3.3 | Ungranted operation, Origin/CSRF denial and zero effects |
| US3.4 | Durable sign-out, exact expiry, renewal races and stale-UI clearing |
| US3.5 | Retained health/OpenAPI behavior and two-user cache/SSR isolation |
| US4.1 | Configured synthetic selection and Development-marked actor/cookie/UI parity |
| US4.2 | Actual environment/listener/origin/backend negative matrix |
| US4.3 | Persisted mode transition, distinct equal-profile users, retained production bootstrap |
| US5.1 | Explicit upgrade/preparation with exact history/schema/module-data preservation |
| US5.2 | Store/key/record failure, interruption/restore and corrected retry; **current F2 incomplete** |
| US5.3 | Correlated captured records/spans/metrics/responses with private canaries |
| US5.4 | Failure/retry/sign-out keyboard/focus/narrow/enlarged-text journeys |
| SC-001 | Actual production login/sign-out, identity parity and mapping/restarts on both stores |
| SC-002 | Twenty eligible requests, exactly one consumed designation/event, three restarts |
| SC-003 | Independent invalid protocol corpus, direct/adapter access, CSRF/insufficient-authority denial |
| SC-004 | Explicit Development positives and exposure/mode-transition negatives |
| SC-005 | Keyboard/focus/announcements, 320px/200% text, axe and post-return bounds; no human study inferred |
| SC-006 | Historical real upgrade/crash/recovery proof; **current F2 prevents renewed qualification** |
| SC-007 | Correlated private captures and public operator recipes; no general audit product inferred |

## Plan, task and constitutional reconciliation

| Decisions | Inspected delivered boundary |
| --- | --- |
| R1-R2 | Confidential code+PKCE, explicit validation/no token saving; enumerated fixed-origin Next routes |
| R3-R4 | Durable minimal tickets and committed-session bridge; default deny, explicit self policy/antiforgery |
| R5-R6 | Offline nomination/atomic singleton consumption; additive format 2 and exact physical/history verification |
| R7-R8 | Recovery-v2/external keys/quarantine/reconciliation; separate limited Development namespace |
| R9 | Most bounded trust/input/state mechanisms supported; **F1 contradicts promised DNS exclusion** |
| R10-R11 | Owner-local durable evidence/retention/maintenance; accessible deliberate identity states |
| R12 | Independent issuer/consumer/providers/processes/browser; **F2 current execution unresolved** |
| T001-T006 | Intent/dependency evidence, independent HTTPS issuer/consumer/corpus and owned processes |
| T007-T022 | Configuration/actor/migrations/transactions/events/keys/maintenance/policy integration |
| T023-T040 | OIDC/mapping/attempts/tickets/commit bridge/adapters/browser; T026/T037 need F1 remediation |
| T041-T055 | Nomination/enrollment, secrecy, contention, interruption and restart |
| T056-T070 | Cross-language/direct/adapter/actor contracts, durable sessions, denial/isolation |
| T071-T078 | Development admission/selector/persisted namespace and mode isolation |
| T079-T092 | Upgrade/recovery/key/failure/privacy/accessibility; F2 affects current recovery evidence |
| T093-T096 | Strict consumer/browser inventories, guides and exact dependency reconciliation |
| T097-T100 | Valid historical complete/source proof; current failure prevents recertification |
| I-III | Brand/module-free portability, minimal host-owned actor contract, direct DI and test-project ownership |
| IV-V | Platform-owned schema/file, preserved module assignments/history and honest owner-local transactions |
| VI | Shared identity/default denial and REST/UI/module parity; **F1 integration-safety defect** |
| VII-VIII | No MCP/resources/jobs/realtime runtime introduced or falsely claimed; future interfaces explicitly excluded |
| IX-X | Accessible introduced UI, real providers, private custody, safe health and operational docs |
| XI | Substantial independent historical proof, but **F1 missing regression and F2 failed current evidence** |
| XII | Feature 005 only, not MVP completion; no scope expansion or self-acceptance |

Filename consolidation is not a gap when delivered assertions cover the behavior.
No new product-design decision is required. Teams/granular authorization 006,
MCP 007/013, full shell 008, general audit 009 and deployment/release 022-024
remain future work.

## Remediation and retained limits

T101 and T102 are independently actionable after their original prerequisites.
T103 depends on both, plus retained dependency/contract reconciliation T096.
Use owned hosted Ubuntu verification, including affected integration/recovery
coverage and the unchanged complete command:

```bash
python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10
```

Do not duplicate focused selections unnecessarily when the actual complete run
contains equivalent evidence. Keep historical failures and scope metadata.
The human-prohibited 50,000-session boundary test is neither run nor replaced;
the runtime cap remains unchanged and unqualified. The reviewed exact Playwright
canary retains its documented risk. Hosted Linux is not final-source macOS proof.
No provider-wide logout, old-instance fencing, rollback-proof administrator
history, production readiness or MVP qualification is inferred.

Both reviewer-owned checklists, approved intent, implementation/convergence
reports and product/test/tooling source remain untouched. This **changes**
review selects implement, not acceptance, readiness or merge.
