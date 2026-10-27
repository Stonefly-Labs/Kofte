# Köfte documentation

## Read in this order

1. [Product charter](product-charter.md): mandate, boundaries and required outcomes.
2. [Product vision](product-vision.md): audience and experience.
3. [Technical architecture](architecture.md): architecture invariants and recommendations.
4. [Master spec inventory](master-spec-inventory.md): exact MVP boundary and ordered prompts.
5. [Constitution](../.specify/memory/constitution.md): adopted product principles and v2.0.0's human-directed workflow amendment; the [constitution prompt](constitution-prompt.md) remains its exact generation input.

These are the canonical product inputs. The constitution governs delivery; its [one-feature/one-branch amendment](adr/0001-feature-delivery-lifecycle.md) records the explicit human correction, now human-merged with accepted spec 001. A conflict with canonical inputs must be surfaced for a human decision and documented amendment, not silently resolved by an assistant.

The [technology-stack exploration](kofte-technology-stack-architecture.md) is historical/proposed background. In particular, its external MCP/Hoard MVP claims, cross-module transaction suggestions and alternative directory tree do **not** override the canonical architecture and inventory.

## Working in the repository

**Current Spec Kit checkpoint: 005 / implement in progress, T001–T014 complete.**
The [implementation evidence](../specs/005-authentication-bootstrap/implementation.md)
records reviewed OIDC adoption (403 npm / 87 NuGet), independent HTTPS/process
fixtures and corpora, strict configuration/public actor-v1, and both-provider
format-2 migration with exact physical verification and real data/history
preservation. The deadline blocker is resolved without increasing limits:
native SQLite execution/cancellable PostgreSQL and metadata/admission now
pass the 95-case focused both-provider identity/registry/lease gate.
There are 86 unchecked tasks; no working login, operator preparation,
session/enrollment UI or identity recovery is claimed.
T015 transactional security evidence is next in the same stage/branch/draft PR,
not converge or acceptance.
The
[requirements-quality checklist](../specs/005-authentication-bootstrap/checklists/quality.md)
satisfies 36/36 criteria after explicitly requested assisted review, with
per-item evidence and no unresolved requirements gap or new human decision.
The same [draft PR #8](https://github.com/Stonefly-Labs/Kofte/pull/8) continues;
the [100 implementation tasks](../specs/005-authentication-bootstrap/tasks.md)
now trace all five stories, requirements/outcomes/scenarios and design decisions
to explicit dependencies, actual paths and mandatory independent validation.
The read-only [analysis](../specs/005-authentication-bootstrap/analysis.md)
passes with concrete coverage of all 29 requirements/outcomes and 22 scenarios,
100 mapped unchecked tasks, 242 acyclic dependency edges and no actionable or
constitutional finding at the historical analysis checkpoint.
Specification, planning artifacts, approved ADR and both reviewed checklists
remain unchanged; only T001's implementation marker is now checked.
No product/dependency/runtime proof or feature acceptance is introduced;
no intermediate merge is requested.
See the [current workflow record](development/spec-workflow.md#invocation-and-current-progress).

- [Contributing](../CONTRIBUTING.md): branch, change, checks, commit, push, PR and human merge.
- [Assistant tooling](development/assistant-tooling.md): shared MCP servers, trust and smoke checks.
- [Spec workflow](development/spec-workflow.md): state, stage gates, recovery and acceptance.
- [Local bootstrap](development/local-development.md): current SQLite/host/web startup, focused checks and unfinished gates.
- [Dependency adoption](development/dependencies.md): exact reviewed graph, compatibility, provenance, licenses and retained notices.
- [ADRs](adr/README.md): record consequential decisions.

Feature specs live in root **`specs/`** to match the installed Spec Kit scripts,
rather than a second `docs/specs/` tree. Spec 001 has completed specify, clarify,
plan, reviewer-owned requirements-quality review, tasks and read-only analysis.
Those artifacts establish design coverage, not runtime proof or acceptance.
[ADR 0002](adr/0002-bootstrap-development-baseline.md) records the human-approved
technical baseline. Implementation now includes reviewed locked dependencies,
host/web foundations, actual SQLite/PostgreSQL persistence and failure checks,
guarded backup/restore, generated Development OpenAPI, local startup/restart and
browser/axe evidence. [ADR 0003](adr/0003-dotnet-test-source-ownership.md)
records the human correction: .NET test source physically belongs to its test
project, never a production slice or source link. The
[implementation report](../specs/001-repository-bootstrap/implementation.md)
records the final complete local gate, all ten passing Ubuntu CI jobs and
170 matching named local/CI steps. All 64 implement tasks are evidenced,
including supported targets and human-confirmed T056/T059. Implement and
[converge](../specs/001-repository-bootstrap/convergence.md) are complete.
Convergence assessed all 38 requirements/acceptance scenarios, twelve plan
decisions, 64 tasks and twelve principles, with no actionable gap or appended
task. The independent [final review](../specs/001-repository-bootstrap/review.md)
also passes with no acceptance defect, source/test coverage of all 38
requirements/acceptance scenarios and a fresh 170-step complete local gate
reconciled with actual assessed-head CI. Spec 001 now has
[explicit human acceptance](../specs/001-repository-bootstrap/acceptance.md),
recorded separately from review and prior task attestations on its original
branch/PR. [PR #4](https://github.com/Stonefly-Labs/Kofte/pull/4) is human-merged,
with its accepted ledger and evidence on main. Modules, SDKs, workers
and Helm remain reserved. Bootstrap acceptance is not MVP or production readiness.

**002 / specify, clarify, plan, checklist, tasks and analyze are complete** on `feature/002-platform-contracts`. The following is the design-stage history, not current implementation status: the
[draft specification](../specs/002-platform-contracts/spec.md) and
[author checklist](../specs/002-platform-contracts/checklists/requirements.md)
cover minimal platform contracts, immutable reverse-DNS identity, versioned
manifests, compatibility and safe diagnostics. All 16 author criteria are
satisfied; clarification found all ten ambiguity categories clear, with zero
questions and no spec/checklist changes. This is not the later human-owned
quality review. The [plan](../specs/002-platform-contracts/plan.md),
[research](../specs/002-platform-contracts/research.md),
[data model](../specs/002-platform-contracts/data-model.md),
[contracts](../specs/002-platform-contracts/contracts/validation.md) and
[future guide](../specs/002-platform-contracts/quickstart.md) choose strict JSON,
SemVer range rules, immutable descriptions and safe bounded validation.
The [quality checklist](../specs/002-platform-contracts/checklists/quality.md) was
generated unchecked, then evaluated at the reviewer's explicit request: all 36
requirements-quality criteria are satisfied with evidence and no blocking gap.
[ADR 0004](adr/0004-module-manifest-contract.md) is now human-approved for
implementation, resolving the design-approval attention item. At that design
checkpoint, adoption/runtime and human/reference evidence remained future work;
the subsequent explicit amendments and actual implementation evidence are below.
The [48-task list](../specs/002-platform-contracts/tasks.md) has
explicit dependencies, 17 parallel markers and full requirement/scenario
traceability, including consumer/schema/privacy/bootstrap and human/reference
gates. The read-only [analysis](../specs/002-platform-contracts/analysis.md)
passes with all 21 requirements/outcomes and 14 scenarios covered, no unmapped
task and no actionable or constitutional finding.

**002 / original implementation checkpoint is complete.**
The pure library and test-owned consumers implement the approved design; the
[author guide](../specs/002-platform-contracts/quickstart.md) is executable.
[Implementation evidence](../specs/002-platform-contracts/implementation.md)
records focused tests, unchanged dependency identities, deterministic cultures,
bounded safe failures, observers and retry. The human-directed ADR amendment
removes fixed Ubuntu/CPU/RAM; actual available-workstation timing satisfies T044.
The subsequent human-directed author-guide amendment removes the manual timed
exercise; seven actual automated guide/example/error/corrected-retry checks
satisfy amended T045. All 48 tasks are evidenced, with no human-study claim.
No module runtime or feature acceptance is claimed. The next invocation keeps
this feature branch/draft PR; there is no intermediate merge.

**002 / first convergence completed with gaps (historical checkpoint).**
The [assessment](../specs/002-platform-contracts/convergence.md) covers all 35
requirements/outcomes/scenarios, twelve plan decisions and 48 original tasks.
It appends T049 for one HIGH partial acceptance-evidence gap: missing documented
public boundary cases and complete 100-run/two-consumer/two-culture ordered
multi-diagnostic proof. Existing named unit and both-provider contract suites
pass, but do not cover those omissions. No product defect is demonstrated;
no product/test/spec/plan change was made. Follow-up convergence, review and
explicit human acceptance remain required on the same draft PR.

**002 / implement closes T049; follow-up converge now passes.**
The [remediation evidence](../specs/002-platform-contracts/implementation.md#convergence-remediation-t049)
records all documented public limit/range recipes and ordered multi-diagnostic
cases, including exact 32/33 truncation and recursively reordered object fields.
The complete corpus runs 100 times per public consumer per real `en-US`/`tr-TR`
culture; corrected retry remains covered. All 49 tasks and focused
quality/unit/both-provider contract gates pass. No production code, schema,
contract rule, dependency or broader runtime changed. This closes the implement
task, not review or human-owned feature acceptance.

The [follow-up convergence](../specs/002-platform-contracts/convergence.md)
assesses all 35 requirements/outcomes/scenarios, twelve plan decisions, all
49 tasks and twelve constitutional principles against current source/tests and
actual evidence. F1 is closed: the complete independent public corpus, including
ordered multi-errors and truncation, runs 100 times per consumer per real
culture. No actionable gap remains; no task or product code is changed.
Fresh named unit and both-provider contract suites pass all 44 targeted steps.
The original gaps assessment is preserved as history.

**002 / final review now passes.** The independent
[review](../specs/002-platform-contracts/review.md) maps all 35 requirements/
outcomes/scenarios, twelve plan decisions, all 49 tasks and twelve principles
to actual source/tests. No defect, remediation or unresolved design decision
remains. Fresh quality/unit/both-provider contracts pass 62 targeted steps;
actual assessed-head CI passes all eleven contexts and 177 product steps.
No new local all-suite or human-usability claim is made. Review is not itself
human acceptance.

**002 / explicit human acceptance is now recorded.** The
[acceptance record](../specs/002-platform-contracts/acceptance.md) quotes
**"spec is fully human approved"** in response to the passing review and
numbered-feature handoff. Scope and limitations remain unchanged; no new
product or measurement is claimed. The same
[PR #5](https://github.com/Stonefly-Labs/Kofte/pull/5) is now human-merged.
Its accepted ledger and all five acceptance evidence hashes were verified on
updated main before starting 003. Acceptance itself did not start it.

**003 / implement is complete** on `feature/003-storage-registry` and
[draft PR #6](https://github.com/Stonefly-Labs/Kofte/pull/6).
The human explicitly approved [ADR 0005](adr/0005-module-storage-ownership.md)
for implementation. Reviewed dependencies, storage contracts, registry,
physical provisioning and real both-provider registration/constraint/process
race evidence are now recorded in the
[implementation report](../specs/003-storage-registry/implementation.md).
The resumed implementation adds actual before/after-commit process death,
explicit verified abandoned-lock recovery, bounded worker cleanup, all-nine-case
public examples and captured seven-outcome privacy proof. The named verifier now
includes storage integration/recovery/privacy and public consumers alongside
the existing bootstrap gates. The implementation checkpoint marked all 78
original tasks complete; its complete local 200-step gate and eleven assessed-head
CI contexts pass.
The subsequent [convergence](../specs/003-storage-registry/convergence.md)
checks all 40 requirements/outcomes/scenarios, fourteen plan decisions, 78
original tasks and twelve principles. It found two HIGH partial public-example
gaps: three process restarts with retained data in `same-id-retry`, and the
forced distinct-registered-ID conflict in `collision`. Core integration proof
exists; no storage-engine defect is demonstrated. Only T079-T080 are appended;
all original tasks and product/test code were preserved by convergence.
The [T079-T080 implementation increment](../specs/003-storage-registry/implementation.md#convergence-remediation-t079-t080)
now executes three fresh processes, checking exact Alpha/Beta assignments and
retained sentinels after each, plus a forced registered-owner UNIQUE collision
with preserved assignment/data/inventory and no contender allocation. The
unowned-target refusal remains. Both standalone and `all` public harnesses now
require those observations; all 80 tasks are evidenced.
The [follow-up convergence](../specs/003-storage-registry/convergence.md) now
passes after assessing all 40 requirements/outcomes/scenarios, fourteen plan
decisions, 80 tasks and twelve principles against source/tests. Both prior
public-example findings are closed, with zero actionable gaps; the task file
and product/test source remain unchanged. Actual assessed-head CI passes eleven
contexts and 200 product steps. **Review is next** on this same draft PR,
not executed by convergence. The original gaps assessment is preserved as
history; neither passing converge nor checked tasks constitute feature acceptance.
The subsequent [final review](../specs/003-storage-registry/review.md) records
**changes**: one MEDIUM finding identifies absent regressions for changed-allocator
retry of an existing mapping and persisted no-storage-to-relational refusal.
Production code appears correct; no storage-engine defect is demonstrated.
T081 is appended for both-provider preservation assertions, retaining all 80
historical task markers and earlier reports. Prior public F1/F2 remain closed.
Fresh quality/unit/both-provider integration/recovery/contract/privacy checks
pass 157 targeted steps; actual assessed-head CI passes eleven contexts and
200 product steps. Existing passes cannot establish absent assertions.
That review selected implement without a new approval or merge prerequisite.
The [T081 implementation](../specs/003-storage-registry/implementation.md#final-review-remediation-t081)
now passes all four inherited real-provider cases and detects controlled
behavioral mutations, preserving exact assignments/data/revisions/inventory.
All 81 tasks are evidenced; quality/unit/integration gates pass 46 targeted steps
after an unchanged PostgreSQL rerun of two initial deadline failures. Production,
APIs and dependencies are unchanged.
The [current convergence](../specs/003-storage-registry/convergence.md) now passes
across all 40 requirements/outcomes/scenarios, fourteen plan decisions, all
81 tasks and twelve principles. Actual assertions close R1/T081; prior public
F1/F2 remain closed, with zero actionable gaps and no appended task.
Tasks, product/test code, approved intent, historical review and checklists are
unchanged. That convergence selected review on the same draft PR.
The [follow-up final review](../specs/003-storage-registry/review.md) now records
**changes** for one MEDIUM implementation finding R2: explicit registry-upgrade
mutations and rejected boundaries omit required correlated operation records.
Existing CLI success/generic failure summaries do not supply the direct API's
complete evidence. R1/T081 and prior public F1/F2 are closed. Exactly T082 is
appended unchecked for private bounded telemetry/CLI projection and real
both-provider captures, preserving all 81 historical task bytes/markers, code,
approved intent and earlier reports. No storage-isolation failure or new design
decision is demonstrated. That review selected implement without accepting the feature.
The [T082 implementation](../specs/003-storage-registry/implementation.md#final-review-remediation-t082)
now emits private correlated upgrade/failed-acquisition records, fixed guidance
and truthful durability; the CLI projects allowlisted worker outcomes without
forwarding raw output. Both-provider capture and host cases cover success,
idempotence and refused/uncertain upgrades. All 82 tasks were evidenced at that
implementation checkpoint, which selected follow-up converge.
The [current convergence](../specs/003-storage-registry/convergence.md) now
records **gaps** after checking all 40 requirements/outcomes/scenarios, fourteen
plan decisions, 82 tasks and twelve principles. R2/T082 and prior T079-T081 are
closed; one MEDIUM partial finding F3 reproduces a rejected public recovery
inspection with zero correlated records/spans/counters. T083 is appended for
recovery-set boundary/mutation diagnostics and real-provider captures.
All prior task bytes, product/test code, approved intent and historical reports
are preserved. That checkpoint selected implement on the same draft PR, with
review and explicit human feature acceptance still separate.
The [T083 increment](../specs/003-storage-registry/implementation.md#convergence-remediation-t083)
now instruments public recovery inspection/verification/manifest writes through
the shared private emitter, preserving return/exception contracts and authority.
Validated provider context, explicit bounded pre-metadata `unknown` and truthful
manifest durability have twelve inherited real-provider capture regressions.
All 83 tasks are evidenced; historical convergence/review and checklists remain
unchanged. That implementation checkpoint selected follow-up converge.
The [convergence after T083](../specs/003-storage-registry/convergence.md) now
**passes**: actual source/assertions cover all 40 requirements/outcomes/scenarios,
fourteen plan decisions, 83 tasks and twelve principles with zero actionable
gaps. F3/T083 and earlier T079-T082 are closed. Tasks are byte-for-byte unchanged;
product/test code, approved intent, historical review and reviewer checklists
remain untouched. **Review is next** on the same draft PR, before explicit
human acceptance, not an intermediate merge.
The [final review after T083](../specs/003-storage-registry/review.md) now
**passes** across all 40 requirements/outcomes/scenarios, fourteen plan
decisions, 83 tasks and twelve principles. F1/F2, R1/R2 and F3 are closed;
no finding or remediation task remains. Tasks and reviewer-owned checklists
are byte-for-byte unchanged, no product/test source or approved intent changes,
and the complete prior review remains verbatim.
**Explicit human acceptance of numbered feature 003 is next**, separately
on the same draft PR. No feature acceptance, readiness, merge request or
004 work is implied by review.
The [explicit human acceptance](../specs/003-storage-registry/acceptance.md)
now records **"this human accesspts 003"** for numbered feature 003, with the
passing review's scope and limits retained. Product/test code, tasks,
reviewed checklists and prior evidence remain unchanged; no new product
measurement or usability study is claimed. The same
[PR #6](https://github.com/Stonefly-Labs/Kofte/pull/6) was human-merged.
The accepted ledger and all five acceptance hashes were verified on updated
main before 004 began; acceptance itself did not start the next feature.

**004 / specify, clarify, plan, checklist, tasks and analyze are complete** on `feature/004-module-lifecycle`.
The [draft specification](../specs/004-module-lifecycle/spec.md) and
[16/16 author checklist](../specs/004-module-lifecycle/checklists/requirements.md)
define five stories, 20 scenarios, 20 requirements and six outcomes for trusted
composition/lifecycle, safe preflight/migrations, data-preserving disable and
health/recovery diagnostics. The exact inventory input and project template
remain authoritative. Clarification found all ten requirements categories clear,
with zero questions; the spec and 16/16 author checklist remain unchanged.
The [coverage assessment](development/spec-workflow.md#invocation-and-current-progress)
distinguishes requirements from the then-open planning choices. The
[plan](../specs/004-module-lifecycle/plan.md),
[research](../specs/004-module-lifecycle/research.md),
[data model](../specs/004-module-lifecycle/data-model.md),
[author contract](../specs/004-module-lifecycle/contracts/composition-v1.md),
[operator contract](../specs/004-module-lifecycle/contracts/operator-v1.md) and
[future guide](../specs/004-module-lifecycle/quickstart.md) now resolve those
choices: explicit trusted composition, bounded whole-set preflight, offline
preparation, supervised lifecycle and private current-attempt health.
[ADR 0006](adr/0006-module-composition-lifecycle.md) is now **human-approved for
implementation**, recording **"yeah approved"** without changing its design,
historical planning artifacts or ledger. Both constitutional design checks
pass, not runtime tests or dependency adoption. The
[quality checklist](../specs/004-module-lifecycle/checklists/quality.md) is now
reviewed at the reviewer's explicit request: **36/36 criteria satisfied**
with per-item evidence and no blocking requirements gap or new human decision.
Spec, plan and 16/16 author checklist are unchanged; actual DI dependency
adoption is separately reviewed; complete provider/process/privacy proof remains
an implementation gate.
The [80-task list](../specs/004-module-lifecycle/tasks.md) now maps all five
stories, 20 scenarios, 20 requirements, six outcomes and twelve plan decisions
to explicit dependencies and path-specific implementation/validation.
Safe preflight precedes startup within P1; real provider migration/preservation,
process termination, privacy, all public examples and retained baseline/full
gates remain mandatory. Generated tasks were initially unstarted; design and
reviewed checklist artifacts remain unchanged.
The read-only [analysis](../specs/004-module-lifecycle/analysis.md) passes:
26/26 requirements/outcomes, 20/20 scenarios and all 80 tasks are covered,
with 118 acyclic dependency edges and no actionable or constitutional finding.
The analysis retains its historical assessed inputs. **The original implementation completed all 80 tasks**
on the same branch and
[draft PR #7](https://github.com/Stonefly-Labs/Kofte/pull/7).
The [implementation report](../specs/004-module-lifecycle/implementation.md)
records the reviewed DI edge, immutable contracts, deterministic resolution,
owned strict harnesses, public no-storage/relational lifecycle, explicit
interrupted-owner recovery and three exact preservation cycles per provider.
All eleven public scenarios now include corrections, current health, actual
owned termination and private diagnostic channels. Only evidenced tasks are
marked. All 80 tasks are evidenced at assessed code head `267efc8`:
ten complete local suite/provider reports and all 286 steps pass, alongside
eleven CI contexts and 286 CI steps. Every standalone/provider `all` command,
strict observation and isolated failure exercise is reconciled in the
[final evidence](../specs/004-module-lifecycle/implementation.md#final-complete-gate-and-implementation-handoff).
Historical failures remain distinct; fixture-only budget/workload repairs do
not relax product deadlines, assertions or floors. That checkpoint selected
convergence; the PR stays draft.

The historical [004 convergence assessment](../specs/004-module-lifecycle/convergence.md)
records **gaps** across all 46 requirements/outcomes/scenarios, twelve plan
decisions, 80 original tasks and twelve principles. One HIGH partial finding
reproduces the actual configured host's missing explicit disabled-state operator
output, despite a correct in-memory snapshot. Exactly T081 is appended unchecked;
all original task bytes/markers, product/test code, approved intent, historical
reports and reviewed checklists remained unchanged. That selected implement
on the same draft PR #7, followed separately by convergence/review and explicit
human acceptance. No intermediate merge is requested.

The [T081 increment](../specs/004-module-lifecycle/implementation.md#convergence-remediation-t081)
now delivers the missing private disabled records, derived from the existing
snapshot for explicit/omitted deployments and validated against the copied
enabled-owner set. Both-provider actual-host/strict-harness regressions require
fresh attempts, zero disabled effects and unchanged anonymous privacy.
All 81 then-existing tasks are implemented; that checkpoint selected follow-up
convergence on draft PR #7.
Historical convergence/analysis and reviewed checklists remain untouched.

**Historical 004 / follow-up converge: gaps.**
The [current assessment](../specs/004-module-lifecycle/convergence.md) verifies
F1/T081's actual disabled observations, but valid service names can exceed the
host-generated dependency-binding limit and fail activation (HIGH F2).
Recurring expected-success cleanup failures remain causally unexplained
(MEDIUM F3): broad integration passes 102/104, unchanged isolated PostgreSQL
cases 5/5. Exactly two tasks are appended under Phase 10, preserving all 81
prior tasks, historical evidence and product/test source.

**Historical 004 / T082-T083 implementation checkpoint.**
The [increment](../specs/004-module-lifecycle/implementation.md#convergence-remediation-t082-t083)
retains accepted dependency identity bounds through actual public binding/
Start, and reproduces/fixes normal-stop failure with incomplete HTTP requests
on both providers. An internal 500-ms drain preserves external cleanup/stop
limits; fixed private causal codes retain intentional incomplete/uncertain
refusal and anonymous privacy. All 83 tasks are implemented. Historical
failures and causal limits remain explicit; follow-up convergence, final review
and explicit human acceptance remain separate on the same draft PR #7.

**Historical 004 / converge after T082-T083: gaps.**
The [assessment](../specs/004-module-lifecycle/convergence.md) checks all 46
requirements/outcomes/scenarios, twelve plan decisions, 83 tasks and twelve
principles. F1/T081 and F2/T082 are closed. HIGH F4 demonstrates narrower
runtime service-count limits rejecting valid compositions; MEDIUM F3 retains
two normal host-disposal failures in fresh 104/106 broad integration.
The HTTP-stop regression passes but does not close the distinct disposal
boundary. Only Phase 11/T084-T085 are appended; prior tasks, product/test
source, approved intent, reviewed checklists and historical reports are
preserved. Continue on the same draft PR #7, without an intermediate merge.

**Historical 004 / implement checkpoint (T084-T085).**
The [report](../specs/004-module-lifecycle/implementation.md#convergence-remediation-t084-t085)
records complete executable counts through actual Start/readiness: 128 per
owner and 1,024 enabled total, plus a strict public 127-prerequisite/129-total
example. A deterministic actual-builder regression proves unused default JSON
watchers survived source clearing; disabling reload before construction
removes them. Corrected both-provider composition passes 106/106 with unchanged
cleanup budgets, incomplete/uncertain refusal, durable inventory and owned exit.
Failed historical/diagnostic runs and complete-gate reconciliation remain
explicit. Follow-up converge, review and explicit human acceptance remain
separate stages on the same draft PR #7.

**Historical 004 / converge after T084-T085: pass.**
The [assessment](../specs/004-module-lifecycle/convergence.md) covers all 46
requirements/outcomes/scenarios, twelve plan decisions, 85 tasks and twelve
principles with zero actionable gaps. F1-F4 are closed with fresh focused
execution and inspected assessed-head CI. Task bytes, approved intent,
reviewed checklists, product/test code and historical evidence are unchanged.
Final review and explicit human acceptance remain separate on the same
[draft PR #7](https://github.com/Stonefly-Labs/Kofte/pull/7), without an
intermediate merge or later-feature work.

**Historical 004 / final review: changes; implement T086 was next.**
The [review](../specs/004-module-lifecycle/review.md) covers all 46
requirements/outcomes/scenarios, twelve plan decisions, 85 existing tasks and
twelve principles. HIGH R1 reproduces valid disabled-catalog prerequisite
metadata being rejected by the enabled-edge protocol bound in 16/16 actual
public-host probe cases. Prior F1-F4 remain closed. Exactly T086 is appended
unchecked, preserving all prior task bytes/markers, approved intent, reviewed
checklists and product/test source. Acceptance is blocked until remediation,
convergence and passing review on the same draft PR #7. No new design approval,
readiness, intermediate merge or later-feature work is introduced.

**Historical 004 / T086 implement complete; investigation deferred.**
The [increment](../specs/004-module-lifecycle/implementation.md#final-review-remediation-t086)
corrects all-catalog diagnostic metadata admission to 2,048 prerequisites while
preserving the 1,024 enabled-edge bound. Public resolution and actual
both-provider validate/serve cover full disabled catalogs, both selection forms
and an unrelated enabled owner, including exact disabled observations,
unchanged inventory, readiness and verified clean stop. All eleven code-head
CI contexts pass; a separate local complete-gate PostgreSQL
migration-interruption failure remains unexplained despite isolated/local recovery passes.
The human explicitly directed documenting its eight-pass/one-failure snapshot
and deferring investigation as non-blocking for this implementation checkpoint.
The [facts and revisit guidance](../specs/004-module-lifecycle/implementation.md#human-directed-deferral-and-implementation-handoff)
preserve the failed local result and unknown cause. T086 is complete;
follow-up converge is next on draft PR #7. Historical review, approved intent
and reviewed checklists remain unchanged; no acceptance or merge is requested.

**Historical 004 / converge passes after T086.** The
[assessment](../specs/004-module-lifecycle/convergence.md) covers all 46
requirements/outcomes/scenarios, twelve decisions, 86 tasks and twelve
principles with no new actionable gaps. Review R1 is closed by actual bounded
protocol/public-host behavior; prior F1-F4 stay closed. No task or product/test
code changes. The deferred PostgreSQL anomaly remains undiagnosed, with its
failed local full-suite result preserved; this is not a new full-gate pass.
**Review is next** on draft PR #7, before explicit human feature acceptance.
No PR readiness or intermediate merge is requested.

**Historical 004 / final review passes after T086.** The
[review](../specs/004-module-lifecycle/review.md) assesses all 46 requirements/
outcomes/scenarios, twelve decisions, 86 tasks and twelve principles.
R1/T086 and prior F1-F4 are closed; no new implementation defect or task remains.
The human-deferred PostgreSQL observation remains undiagnosed and the latest
local complete attempt stays failed/interrupted at 109/111 executed steps.
Review does not replace that gate or waive release evidence.
**Explicit human acceptance of numbered feature 004 is next**, separately on
the same draft PR #7 with this limitation disclosed; no readiness or merge
is requested.

**Current 004 / explicitly human-accepted.** The
[acceptance record](../specs/004-module-lifecycle/acceptance.md) preserves
**"bro this human fugging accepts"** after the passing review and disclosed
deferred PostgreSQL limitation. Review/tasks/checklists remain unchanged;
the failed local complete attempt stays failed and its cause unknown.
The same PR #7 was returned to draft after a SQLite CI failure, despite
unchanged passing local reproduction. The
[failure record](../specs/004-module-lifecycle/implementation.md#post-acceptance-sqlite-ci-failure-and-diagnostic-correction)
retains the unknown cause and bounded failure-diagnostic correction.
The [full-matrix follow-up](../specs/004-module-lifecycle/implementation.md#full-matrix-ci-follow-up)
also records a later PostgreSQL privacy failure and duplicated baseline timeout.
That historical handoff required all eleven latest-head checks to finish
successfully. PR #7 is now human-merged, and the accepted ledger plus all five
acceptance hashes were verified on updated main before 005 began. Acceptance
and merge are not new runtime results, release waivers or MVP completion.

**005 / specify, clarify and plan are complete; checklist is next** on
`feature/005-authentication-bootstrap`. The
[draft specification](../specs/005-authentication-bootstrap/spec.md) and
[16/16 built-in author checklist](../specs/005-authentication-bootstrap/checklists/requirements.md)
cover production OIDC, safe first-admin bootstrap, stable identity,
browser/API parity, development-only login and safe recovery. Five stories,
22 scenarios, 22 requirements and seven outcomes describe future acceptance
evidence. Clarification found all ten requirements categories clear, with zero
questions and no spec/checklist changes; author criteria remain 16/16. The
[coverage assessment](development/spec-workflow.md#invocation-and-current-progress)
preserves the requirements assessment. The
[plan](../specs/005-authentication-bootstrap/plan.md),
[research](../specs/005-authentication-bootstrap/research.md),
[data model](../specs/005-authentication-bootstrap/data-model.md),
[identity contract](../specs/005-authentication-bootstrap/contracts/identity-v1.md),
[operator contract](../specs/005-authentication-bootstrap/contracts/operator-v1.md)
and [future guide](../specs/005-authentication-bootstrap/quickstart.md)
now propose the code+PKCE/session/actor/enrollment and platform/recovery design.
Both constitutional design gates pass; the spec/16 author criteria are unchanged.
[ADR 0007](adr/0007-authentication-bootstrap-boundary.md) is now
**human-approved for implementation** by **"ok, I approve it"**.
Historical planning artifacts/ledger remain unchanged; recording this decision
executes no stage. No product source/package, task list, reviewer-owned
checklist or authentication runtime proof is introduced. Next checklist
generation leaves its markers unchecked; stages continue on the same
[draft PR #8](https://github.com/Stonefly-Labs/Kofte/pull/8) without
intermediate merge or feature acceptance.

The following specify-through-analyze paragraphs preserve historical checkpoints,
including their then-Proposed ADR and unstarted-task statements.

**003 / specify is complete** on `feature/003-storage-registry`.
The [draft requirements](../specs/003-storage-registry/spec.md) use the exact
inventory prompt and resolved project template. Five stories, 18 scenarios,
16 functional requirements and six outcomes cover immutable storage allocation,
real uniqueness/concurrency protection, per-schema/per-file ownership and
migration history, interruption/recovery, least privilege and safe diagnostics.
The [author checklist](../specs/003-storage-registry/checklists/requirements.md)
satisfies all 16 criteria.

**003 / clarify is complete** on the same branch and
[draft PR #6](https://github.com/Stonefly-Labs/Kofte/pull/6).
All ten ambiguity categories are clear at requirements level; zero questions
were asked or answered, and the specification and author checklist are unchanged
(16/16 before and after, with no regressions or unchecked items).
Allocator, contracts, budgets, provisioning credentials and migration/recovery
mechanisms remain planning/ADR decisions, including explicit reconciliation
of the bootstrap PostgreSQL CREATE restriction. No clarification hooks are
configured. No implementation, reviewer-owned quality review or feature
acceptance was performed by clarification.

**003 / plan is complete** on that same branch/draft PR.
The [plan](../specs/003-storage-registry/plan.md),
[research](../specs/003-storage-registry/research.md),
[data model](../specs/003-storage-registry/data-model.md),
[storage contract](../specs/003-storage-registry/contracts/storage-v1.md),
[migration/recovery contract](../specs/003-storage-registry/contracts/migrations-and-recovery.md)
and [future guide](../specs/003-storage-registry/quickstart.md) propose
immutable allocator-v1 mappings plus real uniqueness/locking, explicit physical
ownership, owner-bound EF models/history/migrations and stopped complete-set
recovery. Offline PostgreSQL provisioning is separate from the unchanged
restricted runtime credential; SQLite retains private per-module files and the
existing stopped-host lease. Pre-research and post-design constitutional checks
pass at design level; planning hooks remain absent.
[ADR 0005](adr/0005-module-storage-ownership.md) is Proposed, not approved.
No product command, package adoption, runtime/provider proof, tasks or feature
acceptance was added by planning.

**003 / checklist is complete** on the same branch/draft PR.
The [quality checklist](../specs/003-storage-registry/checklists/quality.md)
was generated with 36 unchecked criteria, then evaluated and marked at the
reviewer's explicit request. All 36 requirements-quality criteria are satisfied
with per-item evidence; no blocking gap or constitutional conflict remains.
Storage/permission boundaries, migrations/recovery, budgets/privacy, real-provider
acceptance and explicit MCP/UI applicability are covered. Proposed ADR 0005
still needs human approval before implementation; dependency adoption and
actual runtime proof remain later gates.

**003 / tasks is complete** on the same branch/draft PR.
The [78-task list](../specs/003-storage-registry/tasks.md) contains six setup,
eight foundational, 13 US1, eight US2, twelve US3, sixteen US4, seven US5 and
eight cross-cutting evidence tasks, all unchecked. Explicit dependencies and
24 parallel markers preserve all 16 requirements, six outcomes and 18 scenarios,
including real uniqueness/process races, assigned EF models/history/migrations,
crash/deadline/privacy observations and executable complete-set recovery.
US1 is the smallest useful internal increment, not full feature/MVP completion.
At the tasks checkpoint, analyze was next; task generation created no product
code, package adoption, runtime proof, later-stage report or human acceptance.

**003 / analyze is complete** on the same branch/draft PR.
The read-only [analysis](../specs/003-storage-registry/analysis.md) passes:
all 22 requirements/outcomes and 18 scenarios have concrete task coverage,
all 78 tasks are mapped, and no actionable or constitutional finding remains.
The task graph has 130 valid earlier-ID edges and 24 parallel markers; all
tasks remain unchecked. The analyzed requirements/design and both reviewed
checklists are unchanged. **Implement is next**, but explicit human ADR 0005
approval remains required before implementation. No dependency adoption,
product/runtime proof, ADR approval or human feature acceptance is inferred.
