# Specification Analysis Report: 005 - Authentication and Bootstrap Administration

**Result:** pass
**Stage:** analyze, 2026-10-26.
**Assessed revision:** `e6b136dc44be36f8b60f238c68c37110191a923f`.
**Branch / checkpoint:** `feature/005-authentication-bootstrap`,
[draft PR #8](https://github.com/Stonefly-Labs/Kofte/pull/8).

## Scope and authority

Read-only consistency analysis of [spec.md](spec.md), [plan.md](plan.md) and
[tasks.md](tasks.md) against the adopted
[constitution v2.0.1](../../.specify/memory/constitution.md), canonical product
documents and inventory 005. Supporting evidence includes [research](research.md),
[data model](data-model.md), [identity-v1](contracts/identity-v1.md),
[operator-v1](contracts/operator-v1.md), [future guide](quickstart.md),
both reviewed checklists and
[Accepted ADR 0007](../../docs/adr/0007-authentication-bootstrap-boundary.md).

| Assessed artifact | SHA-256 |
| --- | --- |
| `spec.md` | `321ed0ee2b04da4c060c5a6d498e6cc795bbe10d88e02f34affb113afc395d75` |
| `plan.md` | `28cd58fd796ada5d915e7a3ae74709f67a93cd19ca511287066ca9570e764696` |
| `tasks.md` | `1429ceea187b6fedc55b15aa1524418cf7b05919e76684814bd75059b57dd059` |

All assessed inputs, supporting planning artifacts, approved ADR, constitution
and reviewer markers remain byte-for-byte unchanged from the assessed revision.
The project driver persists this report, its ledger outcome and current progress
documentation; no remediation is performed. `.specify/extensions.yml` is absent,
so no before/after analysis hook is configured.

This is design/task coverage, **not behavioral coverage, implementation,
dependency adoption, runtime proof, feature acceptance or MVP completion**.
All 100 implementation tasks remain unchecked. The author checklist remains
16/16 and the explicitly assisted reviewer checklist remains 36/36.

## Findings

| ID | Category | Severity | Location(s) | Summary | Recommendation |
| --- | --- | --- | --- | --- | --- |
| None | All six detection passes | None | Core artifacts and supporting design | No actionable duplication, ambiguity, underspecification, constitutional conflict, coverage gap or ordering/terminology inconsistency identified. | Proceed only to the separate implementation stage with its existing adoption and evidence gates. |

No unresolved critical/high issue or constitutional blocker remains.
No remediation edits are proposed or applied.

## Coverage summary

All 22 functional requirements and seven success criteria describe buildable
work; no post-launch business KPI is included in the denominator. The focused
tasks below identify actual work and independent acceptance obligations,
not merely the blanket final reconciliation task.

| Requirement key | Has task? | Task IDs | Notes |
| --- | --- | --- | --- |
| FR-001 | Yes | T002, T004, T023, T026, T031, T037, T039 | Maintained provider-neutral OIDC with real configured HTTPS protocol and browser evidence. |
| FR-002 | Yes | T023, T025-T026, T028, T030-T031, T037, T064 | Independent wrong trust/binding/replay cases; no session or cookie on rejected/uncertain evidence. |
| FR-003 | Yes | T012, T024, T027, T030, T038, T077, T079 | Exact issuer/subject mapping, UNIQUE conflict resolution, profile changes and fresh restarts. |
| FR-004 | Yes | T032, T034-T036, T039, T046, T054, T056, T060, T066, T068, T074 | Minimal UI/direct/adapter identity equality, safe display and cache isolation. |
| FR-005 | Yes | T018-T021, T032, T056, T061-T064, T066 | Explicit public inventory, fallback denial, protected test consumer and preserved anonymous health. |
| FR-006 | Yes | T019, T032, T046, T056, T059, T062, T064, T068 | Independent 401/403/503/error-shape assertions; no API provider HTML/challenge redirect. |
| FR-007 | Yes | T009-T010, T019, T024, T032, T054, T056, T061-T063 | Host-established actor only; first-admin designation does not grant an unrelated action. |
| FR-008 | Yes | T016, T019, T023, T025, T028-T031, T033-T034, T057, T059, T064-T067 | Fresh protected reference, CSRF/origin/callback binding, redirect/cookie and two-user cache boundaries. |
| FR-009 | Yes | T017, T029, T058-T060, T063, T065, T067, T083, T085 | Finite idle/absolute bounds, terminal revocation, renewal race and next-request denial. |
| FR-010 | Yes | T041-T048, T052, T077 | Stopped exact production nomination, private expiring authorization and no first-arrival/dev grant. |
| FR-011 | Yes | T012, T043, T045, T049-T053, T085 | One owner-local designation/consumption/event; twenty contenders and actual crash/restart evidence. |
| FR-012 | Yes | T017, T043-T044, T048, T051, T082, T085, T091, T095 | Permanent consumed closure, pending-only replacement and no restore/IdP-loss reenrollment shortcut. |
| FR-013 | Yes | T007-T008, T071, T073-T076, T078, T090 | Explicit local mode/environment/exposure matrix and persistent visible development label. |
| FR-014 | Yes | T014, T017, T024, T029, T065, T072-T073, T077 | Separate namespace/mode/epoch and persisted dev-to-OIDC rejection without profile-based merge. |
| FR-015 | Yes | T008, T019-T020, T023, T026, T030, T037, T050, T058-T059, T082, T084-T086, T090 | Bounded truthful configuration/outage/uncertainty failures, no success-shaped fallback. |
| FR-016 | Yes | T011-T017, T021-T022, T038, T051, T079, T081-T082, T084-T085 | Explicit exact format-2 prepare/upgrade and complete-set recovery preserve host/module state. |
| FR-017 | Yes | T015, T020, T025, T030, T044-T045, T049-T050, T059, T082, T087-T089 | Durable owner-local events and safe correlated rejection/uncertainty; recording/capacity failure tested. |
| FR-018 | Yes | T019-T020, T031-T034, T046, T048, T054, T062, T066, T089 | Non-enumerating errors and independent useful/private channel captures. |
| FR-019 | Yes | T006-T008, T014, T016, T026, T028-T029, T033-T034, T037, T044, T057, T064, T083, T087-T088 | Frozen numeric corpus, finite egress/input/state/rate budgets, private secrets and bounded pruning. |
| FR-020 | Yes | T035-T036, T039, T047, T054, T060, T067, T074, T078, T090 | Actual all-state keyboard/focus/announcement, width/text/axe/timing and safe return evidence. |
| FR-021 | Yes | T009-T010, T032, T056, T061, T063, T068-T069 | Minimal immutable actor-v1, versioned HTTP/types and separately compiled trusted consumer. |
| FR-022 | Yes | T001, T003-T006, T022, T040, T055, T070, T078, T092-T100 | Story evidence joins independent protocol/provider/browser/privacy and retained full local/actual CI gates. |
| SC-001 | Yes | T038-T039, T065, T069, T094 | Production login/sign-out and exact stable identity on both stores. |
| SC-002 | Yes | T048-T053, T085 | Twenty eligible HTTP contenders, three fresh restarts, one consumed designation/event and no additional grant. |
| SC-003 | Yes | T023, T037, T048, T056-T057, T061-T069 | Negative trust/authentication/authority before effects across direct, adapted and consumer paths. |
| SC-004 | Yes | T071-T072, T076-T078 | Admitted local development only; no exposed/production authentication or bootstrap inheritance. |
| SC-005 | Yes | T039, T067, T078, T090 | Actual all-state accessibility and five-second post-return outcome on the documented profile. |
| SC-006 | Yes | T025, T038, T050-T052, T079, T083-T086, T091 | Exact committed identity/security/module preservation through real interruption, upgrade and restore. |
| SC-007 | Yes | T054, T088-T091, T094 | Useful correlated channel canaries and delivered public correction recipes, not manual DB edits. |

### Story and scenario alignment

| Scenarios / source | Concrete acceptance tasks and observations |
| --- | --- |
| US1.1-5, `spec.md:77-108` | T023-T025/T037-T040: real browser code flow and exact parity, concurrent stable mapping/profile/issuer/restart cases, independently signed negatives, cancellation/outage and corrected fresh login. |
| US2.1-5, `spec.md:109-142` | T041-T042/T048-T055: absent/nominee/expired/replaced/replayed ceremony, delivered private-file enrollment, twenty-way contention, real commit barriers, three restarts and permanent closure. |
| US3.1-5, `spec.md:143-175` | T056-T058/T061-T070: direct/adapter/consumer equality and denial, Origin/CSRF, durable sign-out/expiry/renewal, two-user isolation and unchanged aggregate health. |
| US4.1-3, `spec.md:176-202` | T071-T078: actual local selection and visible mode, startup/exposure matrix and persisted production-transition rejection. |
| US5.1-4, `spec.md:203-235` | T079-T092: exact clean/v1/v2 ownership, interrupted preparation/restore quarantine, explicit reconciliation, key/capacity/retention/channel captures and accessible bounded retry. |

All 22 scenarios have concrete independently assessable task obligations,
not just story labels. The complete per-scenario mapping in
`tasks.md:382-407` agrees with the future guide's acceptance matrix.
The nine public recipes each have a named scenario owner and standalone plus
`all` execution on both providers (T039/T053/T069/T078/T091/T094).
Missing observations/capture/cleanup must fail; browser evidence stays independent.

## Ordering and cross-artifact consistency

All 100 IDs are unique and sequential. The 242 explicit dependency edges
resolve to existing earlier IDs; topological sorting includes all 100 tasks
with no cycle or forward reference. T001 is the sole root. The 22 parallel
candidates remain conditional on their own prerequisites and separate files/
owned fixtures, not authorization to overlap shared installations or stages.
Required tests precede dependent implementation; initial missing-behavior
failures are explicitly distinguished from tests executed before fixtures exist.

| Design decision | Task ownership / consistency assessment |
| --- | --- |
| R1 confidential OIDC | T002/T004/T023/T026/T028/T030-T031/T037 preserve the maintained code+PKCE baseline and independently verified trust. |
| R2 exact adapters | T033-T034/T046/T060/T064/T066/T074 preserve fixed upstreams, individual cookies and direct/adapted parity. Unsupported Authorization is rejected locally, not silently stripped into valid cookie authentication. |
| R3 durable tickets | T016/T025/T029-T030/T058-T059/T063/T065/T083 preserve one login commit before cookie publication and no second `ITicketStore` commit. |
| R4 actor/default denial | T009-T010/T018-T019/T032/T056-T057/T061-T064/T068 retain self scope, explicit public protocols and no administrator universal allow. |
| R5 nominated ceremony | T041-T055 cover private non-atomic file delivery separately from atomic database enrollment and permanent consumed state. |
| R6 platform format 2 | T011-T017/T021-T022/T079/T084 retain original migration bytes and module mappings/history while explicitly validating the new inventory. |
| R7 recovery reconciliation | T080-T086/T091/T095 preserve complete-set verification, restored-copy quarantine, new epoch/keys and historical uncertainty. |
| R8 isolated development | T071-T078 retain all-loopback explicit selection, distinct cookies/namespace and no production designation. |
| R9 finite boundaries | T006-T008/T014/T016/T026/T028-T029/T033/T037/T057/T064/T083/T088 cover exact inputs, network trust, time/rate/size/state bounds and one-over refusals. |
| R10 safe retained evidence | T015/T020/T025/T049-T050/T054/T059/T082/T087-T089 retain owner-local events, immutable installation evidence and explicit recording/capture failure. |
| R11 accessible journey | T035-T036/T047/T060/T074/T090 preserve all identity states, safe destinations and measured accessibility/timing. |
| R12 independent evidence | T003-T006/T022/T039/T053/T061/T069/T078/T091-T100 cover real fixtures, separate consumers and exact final local/CI observations. |

The one-second local work, four-second callback, three-second combined egress,
4.5-second adapter and five-second UI envelopes are nested constraints, not
additive deadline allowances. Enrollment rates admit the required twenty
contenders. Original preparation/startup/cleanup limits remain obligations.
T006/T022/T037/T057/T064/T088 require actual boundary/effect assertions;
planning bounds are not claimed as measured results.

The eight identity entities, host-only owner transaction, minimal actor,
strict exact schema versions and separate key custody agree across the model,
contracts and tasks. Ordinary serving never initializes/migrates or repairs a
lost ring. Development setup and fresh owned fixtures perform explicit stopped
preparation (T075); initialization is not hidden in the serving supervisor.
Restored snapshots cannot prove historical non-enrollment: unknown history
stays locked, recovered consumption wins and old-instance fencing remains an
operator responsibility.

Historical Proposed/next-checklist labels describe their planning checkpoint.
ADR 0007's later Approval section is current authority for design approval;
the reviewed checklist and task prerequisites explicitly resolve that precedence.
This is not a new approval blocker or permission to adopt unchecked dependencies.
The task file's analyze-next text remains its generation history, not a reason
to rewrite approved inputs after recording this analysis.

## Constitution alignment issues

**None identified.** All twelve principles and applicable delivery/migration
gates were assessed; no complexity exception or scope waiver is introduced.

| Principle | Applicable invariant and task evidence |
| --- | --- |
| I | Portable Köfte identity on empty/composed hosts; no required vendor/module/Hoard/cloud, T021/T026/T036/T074/T095. |
| II, III | Minimal versioned actor, VSA/native DI, dedicated test-owned source/consumer, reviewed actual package closure and no mediator/mapper, T002-T010/T019-T021/T061/T096/T099. |
| IV | Host schema/file only; exact v1/v2 physical/history validation and retained collision/UNIQUE/63-byte/path/module-EF suites, T011-T017/T022/T079-T085/T093/T097. |
| V | One owner-local login/enrollment/event commit, real uncertainty barriers and honest DB/filesystem partial effects; no cross-module ACID, T025/T030/T044-T045/T050/T052/T082/T084-T085. |
| VI | Exact OIDC trust, deny-by-default actor/authority, nominated non-repeatable bootstrap, limited development, finite egress and safe evidence; T018-T020/T023-T031/T041-T089. |
| VII | Actor-v1 is a future integration contract, not caller proof or an MCP bypass; T010/T061/T068. MCP runtime/auth/tools remain 007/013. |
| VIII | T087 is bounded identity housekeeping, not a durable job/event system. Resources/jobs/providers/realtime and their acceptance remain later numbered features. |
| IX | Minimal existing design-system UI, all states and actual keyboard/width/text/axe/timing evidence, T035-T036/T047/T060/T074/T090. No remote frontend loading/full shell. |
| X | Pinned portable tools, explicit local versus production TLS/keys, stopped migration/complete restore and documented responsibilities, T002/T016-T017/T075/T079-T085/T095-T098. |
| XI | Independent issuer/actor/browser, real SQLite/PostgreSQL, exact state/crash/channel observations, retained complete local and actual CI gates, T003-T006/T022/T039/T049-T054/T079-T098. |
| XII / governance | Exact inventory 005 scope, approved design versus adoption, original branch/draft PR and later convergence/review/explicit human acceptance, T001/T095/T099-T100. |

Granular teams/resource permissions (006), MCP (007/013), full shell (008),
general audit administration (009), reference module (020), deployment/HA/
Kubernetes (022) and release (023-024) remain separate obligations. Their
runtime tests are inapplicable to this slice, not waived for MVP.

## Unmapped tasks and metrics

**Unmapped tasks: none.** Each task has an explicit FR trace, applicable
principles and concrete path/work. Setup, evidence and publishing tasks support
required delivery rather than creating unrelated product scope.

| Metric | Result |
| --- | --- |
| Functional requirements / buildable outcomes | 22 / 7 |
| Requirement/outcome coverage | 29/29, 100% |
| Stories / acceptance scenarios covered | 5/5 / 22/22 |
| Design decisions / constitutional principles assessed | 12/12 / 12/12 |
| Tasks / unchecked / parallel candidates | 100 / 100 / 22 |
| Explicit dependency edges / cycles / invalid references | 242 / 0 / 0 |
| Actionable ambiguities / duplications / underspecifications | 0 / 0 / 0 |
| Coverage / inconsistency / constitutional findings | 0 / 0 / 0 |
| Critical / high / medium / low findings | 0 / 0 / 0 / 0 |

The explicit-path prerequisite command succeeded. Read-only structural checks
verified IDs, dependencies, coverage-row inventories, reviewer counts and
unchanged SHA-256 input identities; semantic coverage was assessed directly
above, not inferred from matching IDs. Publication repository/Python/whitespace
commands and exact results are recorded in the same PR. No authentication
product test/build is run or claimed by this documentation-only stage.

## Next action and evidence limits

**Implement is next in a separate invocation on this same branch/draft PR.**
Retain exact dependency-adoption review, both-provider/protocol/consumer/browser/
privacy proof, all story checkpoints and complete local/actual CI reconciliation.
Convergence, final review and explicit numbered-feature acceptance remain later.
No remediation approval is needed because no issue/edit is proposed.

The inherited 004 failed local gate and deferred PostgreSQL observation, plus
005's earlier specification-head private-diagnostics CI failure, remain failed/
undiagnosed historical evidence. Later green observations do not diagnose or
erase them. Analysis supplies no new runtime result, release waiver, PR readiness,
intermediate merge request or permission to begin a second stage.
