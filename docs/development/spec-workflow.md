# One stage at a time with `drive-speckit`

The repository already has GitHub Spec Kit skills, scripts and templates. The
project-specific `drive-speckit` skill coordinates them without replacing upstream
assets or running a fleet. **Creating this driver does not execute product work.**

## Invocation and current progress

**Current checkpoint:** 003 / specify, clarify, plan, checklist and tasks are complete on
`feature/003-storage-registry`, originally created from updated `origin/main`,
and the same [draft PR #6](https://github.com/Stonefly-Labs/Kofte/pull/6).
The exact inventory prompt and resolved project spec template produced
[spec.md](../../specs/003-storage-registry/spec.md) and
[checklists/requirements.md](../../specs/003-storage-registry/checklists/requirements.md).
Five independently testable stories, 18 acceptance scenarios, 16 functional
requirements and six outcomes cover durable ownership, real uniqueness/
concurrency protection, isolated schemas/files and migration history,
interrupted provisioning/recovery and safe diagnostics. All 16 built-in author
criteria are satisfied, with no material unresolved question or specification
hook.

The installed clarification skill resolved the recorded feature directory
explicitly and found all ten ambiguity categories clear at requirements level:
functional scope, domain/data, interaction/UX, nonfunctional quality,
integration/dependencies, edge cases/failures, constraints/tradeoffs,
terminology, completion signals and miscellaneous/placeholders. Zero questions
were asked or answered, and no human decisions were invented.
The specification and author checklist remain unchanged: **16/16 to 16/16**,
with no newly passing items, regressions or unchecked criteria.
The driver completion note records category-to-requirement evidence.
No before/after clarification hooks are configured.
The [plan](../../specs/003-storage-registry/plan.md),
[research](../../specs/003-storage-registry/research.md),
[data model](../../specs/003-storage-registry/data-model.md),
[storage contract](../../specs/003-storage-registry/contracts/storage-v1.md),
[migration/recovery contract](../../specs/003-storage-registry/contracts/migrations-and-recovery.md)
and [future validation guide](../../specs/003-storage-registry/quickstart.md)
now resolve those technical choices. Proposed allocator v1 persists bounded
keys with real database uniqueness and exclusive maintenance. Ownership markers
and durable phases support truthful interruption recovery; assigned EF models,
cache keys, history and migration operations preserve the physical boundary.
Offline PostgreSQL provisioning uses a separate credential; the normal host
still lacks database CREATE and keeps existing fixture/runtime safety.
SQLite keeps private per-module files and the stopped-host lease. Explicit
bootstrap upgrade and stopped complete-set recovery avoid registry-only reset.
Pre-research and post-design constitution checks pass at design level, with
no planning hooks. [ADR 0005](../adr/0005-module-storage-ownership.md) remains
Proposed and requires human approval before implementation.
Planning created no product code, dependency adoption, runtime proof,
reviewer-owned quality checklist, tasks or human acceptance.

The [quality checklist](../../specs/003-storage-registry/checklists/quality.md)
has now been generated with 36 unchecked requirements-quality criteria.
The reviewer explicitly selected **"assisted"** when asked whether the agent
should assess and mark the checklist with per-item evidence, highlighting human
attention items. Under that authorization, all 36 criteria are assessed and
satisfied with evidence and no blocking requirements-quality or constitutional
finding. All criteria trace to the specification and cover storage/permission
boundaries, migration/recovery, operability/privacy, acceptance measurability
and explicit MCP/UI applicability. No material scope question was needed.
No before/after checklist hooks are configured.

Human attention remains **Proposed ADR 0005**: immutable allocation, offline
PostgreSQL provisioning authority, assigned migration boundaries/restrictions,
finite budgets and stopped complete-set recovery. Checklist assessment does
not approve it. Dependency graph/license/provenance/native-asset/lock review
and actual both-provider concurrency, migration, interruption, deadline,
privacy and executable-recovery proof remain implementation gates.
Spec, plan, research, data model, contracts, future guide and the 16/16 author
checklist were unchanged by checklist evaluation.

The [tasks stage](../../specs/003-storage-registry/tasks.md) now provides
**78 unstarted, path-specific tasks**: six setup, eight foundational, 13 US1,
eight US2, twelve US3, sixteen US4, seven US5 and eight final evidence tasks.
Each has explicit prerequisites and requirement/principle mapping; all five
stories, 16 functional requirements, six outcomes and 18 scenarios have
implementation and validation coverage. Twenty-four parallel markers apply
only after dependencies and on distinct files/owned fixtures.
The list preserves real UNIQUE/CHECK and independent-process race evidence,
immutable retry/no-storage/restarts, assigned EF model/history/migration and
negative-boundary checks, every durable interruption/deadline, stopped
complete-set restore, legacy refusal, captured privacy and executable public
examples. Existing module-free host/manifest/HTTP/web/browser/full local/CI
checks remain mandatory. US1 is the first internal increment, not accepted
003 or MVP completion.
Task generation used the explicit recorded directory and resolved project
override. No before/after tasks hooks are configured; every checkbox is
unchecked, and requirements/design/checklists remain unchanged. No product
code, dependency adoption, runtime proof, analysis report, ADR approval or
feature acceptance was created. **Analyze is next** on this same branch/draft
PR. Passing analysis and explicit human ADR 0005 approval are required before
implementation. Do not merge between stages.

**Completed prerequisite:** 002 / explicitly human-accepted on the original branch
and PR #5. After the passing review and numbered-feature acceptance handoff, the
human stated **"spec is fully human approved"**. The
[acceptance record](../../specs/002-platform-contracts/acceptance.md) preserves
the exact decision, reviewed scope and limits. The driver records it through
`complete --approval`; no approval is inferred from tasks, ADRs or CI.
Acceptance leaves product/tests/tasks/design/checklists/review evidence unchanged.
The same [PR #5](https://github.com/Stonefly-Labs/Kofte/pull/5) was subsequently
human-merged. Its accepted ledger and all five acceptance evidence hashes were
verified on updated main before beginning 003. No assistant merge, close,
auto-merge or branch deletion occurred. Acceptance itself did not create the
003 branch, directory or active stage.

The completed review checkpoint follows. The independent
[review](../../specs/002-platform-contracts/review.md) traces all 15 functional
requirements, six amended outcomes, 14 acceptance scenarios, twelve plan
decisions, all 49 tasks and twelve principles to actual source/tests. No
acceptance defect, remediation or unresolved within-scope design decision
remains. Fresh quality/unit/both-provider contract checks pass all 62 targeted
steps; all eleven assessed-head CI contexts and all 177 product-report steps
pass. The synthetic merge parents were verified against main and the assessed
feature head. Review leaves product/tests/tasks/design/checklists unchanged
and does not claim a new local all-suite run or human-usability study.
Review selected accept, requiring an explicit human feature decision; that
decision was not performed by review. The separate completed acceptance is
recorded above. No intermediate merge occurred.

The preceding convergence/remediation checkpoint follows. The
[remediation evidence](../../specs/002-platform-contracts/implementation.md#convergence-remediation-t049)
records the documented public boundaries, reversed range, recursively reordered
collected errors and exact 32/33-diagnostic truncation. Every corpus case runs
100 times through each public consumer in both real cultures. All 49 tasks
and focused quality/unit/both-provider contract gates pass, with no production
code, contract-rule or dependency change. The
[follow-up assessment](../../specs/002-platform-contracts/convergence.md) checks
all 35 requirements/outcomes/scenarios, twelve plan decisions, all 49 tasks and
twelve principles with zero actionable gaps. The task file is byte-for-byte
unchanged, and fresh named unit/both-provider contract suites pass all 44
targeted steps. Review was selected next by convergence; neither review nor
explicit human acceptance was performed by convergence. The first convergence
report is preserved below the current passing assessment as historical gap
evidence. The original
implementation checkpoint remains in
[implementation.md](../../specs/002-platform-contracts/implementation.md).
The human-directed ADR 0004 amendment removed T044's fixed Ubuntu/CPU/RAM
benchmark profile; available supported workstation timing now satisfies it.
The subsequent explicit author-guide amendment removes T045's manual timed
exercise; actual automated guide/example/error/corrected-retry checks supply
amended SC-001. All 48 original task markers were completed at that checkpoint,
without claiming a human study; T049 now supplies F1's missing public proof.
The first implementation selected converge; that assessment selected implement,
and the remediation selected the fresh passing converge invocation. It selected
review, whose completed result is above. Do not infer feature acceptance or
request an intermediate merge. Earlier stage descriptions below are historical.

In Copilot, reload project skills with `/skills reload`, then ask:

```text
Use /drive-speckit to perform the next stage.
```

The constitution stage generated `.specify/memory/constitution.md` v1.0.0 from
the exact fenced prompt in `docs/constitution-prompt.md` and was human-merged.
The next stage, **001 / specify**, produced
[`spec.md`](../../specs/001-repository-bootstrap/spec.md) and its
[`checklists/requirements.md`](../../specs/001-repository-bootstrap/checklists/requirements.md)
using the exact fenced inventory prompt. Its ledger transition is a completed
checkpoint on the unfinished feature's branch. **001 / clarify** is also complete:
all ten ambiguity categories were clear at requirements level, with zero questions
asked or answered. The specification needed no changes, and its author checklist
was revalidated at **16/16 to 16/16**, with no newly passing items, regressions or
unchecked criteria. The ledger completion note records the category-to-requirement
evidence. No clarification hooks were configured. At clarification, exact toolchain
versions, orchestration, reference environment and route/schema details were
reserved for planning, not unanswered product questions. **001 / plan is now complete**:
[`plan.md`](../../specs/001-repository-bootstrap/plan.md),
[`research.md`](../../specs/001-repository-bootstrap/research.md),
[`data-model.md`](../../specs/001-repository-bootstrap/data-model.md),
[`contracts/`](../../specs/001-repository-bootstrap/contracts/) and
[`quickstart.md`](../../specs/001-repository-bootstrap/quickstart.md)
define supported baselines, native local host/web startup, platform-only storage,
minimal safe diagnostics, accessible status states and the required validation.
The pre-research and post-design constitution checks pass at design level;
no implementation behavior is claimed. No planning hooks were configured.
[Proposed ADR 0002](../adr/0002-bootstrap-development-baseline.md) records the
consequential choices. **001 / checklist is now complete**:
[`checklists/quality.md`](../../specs/001-repository-bootstrap/checklists/quality.md)
was generated with 36 unchecked requirements-quality criteria covering
permission boundaries, storage, MCP applicability, UX and operability, with
traceability to the spec and supporting design. The reviewer then explicitly
requested agent-assisted evaluation. All 36 criteria were assessed and marked
satisfied with per-item evidence; no blocking requirements-quality gap was found.
The checklist surfaces proposed ADR 0002 for human review, exact dependency/license
adoption gates and actual product/manual evidence as remaining attention items,
not as completed work. This standard, risk-focused requirements review is not
implementation verification, ADR approval, feature acceptance or the MVP release
gate. No checklist hooks were configured; no tasks or product artifacts were
created during checklist. **001 / tasks is now complete**:
[`tasks.md`](../../specs/001-repository-bootstrap/tasks.md) contains 64 unstarted,
path-specific tasks: six setup, six foundational, 12 for US1, 12 for US2, 13 for
US3, eight for US4 and seven cross-cutting tasks. Each states its prerequisites,
requirement and constitutional traceability. The list preserves all 17 functional
requirements, six measurable outcomes, real-provider/recovery/privacy/contract/
browser/failure-propagation checks, safe parallel groups and human manual gates.
US1 is the first internal increment, not feature or MVP completion; the full
green local/CI gate follows all four stories. Task generation used the resolved
project override and explicit recorded feature directory; no before/after tasks
hooks were configured. No task was executed or checked, and no product file or
analysis report was created. **001 / analyze is now complete**:
[`analysis.md`](../../specs/001-repository-bootstrap/analysis.md) records a passing
read-only consistency, quality and constitutional review of the specification,
plan, tasks and supporting design. All 23 requirements/outcomes and 15 acceptance
scenarios have concrete task coverage; all 64 tasks are mapped, sequential and
unchecked, with no invalid numbered prerequisite/cycle or actionable finding.
The report preserves the reviewed checklists, proposed ADR/dependency-adoption
gates and actual manual/product evidence obligations. No analysis hooks were
configured. The analyzed inputs and implementation markers remain unchanged;
the report is design/task evidence, not runtime proof or human acceptance.
**001 / implement is now complete**, after resolving T001's human approval gate. Its
[`implementation.md`](../../specs/001-repository-bootstrap/implementation.md)
records passing entry/checklist gates, the initial unavailable reviewer and the
subsequent explicit decision: "bro I approve adr 002". ADR 0002 is Accepted,
T001 is complete and the driver resumed the same active stage. The later
implementation checkpoint records the complete dependency review and adopted
locks, host/web/configuration/telemetry foundations, private SQLite persistence,
actual HTTP checks and initial local/browser evidence. The human-directed
[.NET source ownership correction](../adr/0003-dotnet-test-source-ownership.md)
keeps tests physically in their .NET test projects; constitution v2.0.1 and the
affected plan/task paths agree. The later increment implements real PostgreSQL,
guarded recovery, generated Development OpenAPI and fail-closed named
verification. Both-provider controlled outage/privacy, full HTTP/adapter
contracts and all-state browser/axe checks have actual local evidence.
All ten local automated groups now pass, including controlled failures and
ten validated private-fixture starts. T001-T055 and T057 are evidenced.
Actual Ubuntu CI subsequently exposed dependency-evidence and 200%-text reflow
failures. Both were corrected without weakening their checks; all ten jobs
passed at `6cab683`, and supported-target T058 is now evidenced.
The human subsequently confirmed T056 and T059 complete and checked both task
markers. The confirmation is recorded verbatim in the implementation report;
no additional reviewer observations or reference measurements are invented.
The original implement run completed all 64 tasks, including final documentation,
the fresh complete local gate, actual CI inspection and explicit coverage of
all 15 scenarios, 17 functional requirements, six success criteria and twelve
constitutional principles. The final source has 170 passing named steps locally
and in CI. A macOS zombie-group probe race discovered by the final run was
reproduced and corrected without weakening cleanup or performance bounds.
That implement invocation selected **converge**, without executing it.
A commit checkpoint was not a reason to stop the active implement stage.
**001 / converge is now complete**:
[`convergence.md`](../../specs/001-repository-bootstrap/convergence.md)
records zero actionable missing/partial/contradicting/unrequested gaps after
assessing all 17 functional requirements, six success criteria, 15 acceptance
scenarios, twelve plan decisions, all 64 tasks and twelve constitutional
principles against current source, tests and actual evidence. Fresh focused
unit, real SQLite/PostgreSQL integration, both-provider HTTP/adapter contract
and browser checks passed all 65 named steps; dependency reconciliation passed
for 403 npm and 69 NuGet identities. These are targeted checks, not a newly
executed all-suite gate or human reference exercise. Actual assessed-head
repository CI and all ten product jobs also passed. `tasks.md` remains
byte-for-byte unchanged, with no empty convergence phase or product edits.
No convergence hooks are configured. The driver selects **review**;
final review is not executed by convergence, and feature acceptance is not
inferred from its passing result. **001 / review is now complete**:
[`review.md`](../../specs/001-repository-bootstrap/review.md) records the
independent read-only `spec-review` assessment of actual source/tests against
all 17 functional requirements, six success criteria, 15 acceptance scenarios
and twelve constitutional principles. No acceptance defect, constitutional
conflict or scope expansion was identified; no remediation task was appended.
The caller closed the reviewer's execution-evidence requests with a fresh
complete local gate: all ten groups and all 170 named steps pass, including
ten validated persistent-fixture warm starts and five exact deliberate failure
checks. Actual assessed-head CI and its ten uploaded reports also pass with
identical ordered suite coverage; synthetic merge parents were verified.
The report preserves human T056/T059 attestations and reference-machine/manual
accessibility limitations without inventing observations. Product source,
tests, configuration, locks, spec, plan and tasks remain unchanged by review.
The review driver selected **accept**, not accepted; no human feature decision
or acceptance artifact was created by review. **001 / accept is now complete**:
after the assistant explained the exact numbered-feature scope and limits and
linked the passing review, the human replied **"yeah bro, I accept"**.
[`acceptance.md`](../../specs/001-repository-bootstrap/acceptance.md) preserves
that exact affirmative decision and its context. The helper records it through
`--approval`, completes only accept and marks feature 001 accepted. Required
review/task/checklist evidence remains unchanged; no new product or reference
measurements are invented. Production readiness, future feature obligations
and MVP completion are explicitly excluded.
All stages remained on `feature/001-specify-repository-bootstrap` and the same
[PR #4](https://github.com/Stonefly-Labs/Kofte/pull/4),
**without an intermediate merge**. PR #4 was subsequently human-merged;
its accepted ledger and all five acceptance evidence hashes were verified on
updated `origin/main` before beginning the next numbered feature. Acceptance
itself did not create a spec 002 branch, directory or active stage.

**002 / specify is now complete** on the fresh
`feature/002-platform-contracts` branch from updated `origin/main`.
The driver extracted the exact 002 fenced inventory prompt and the installed
`speckit-specify` skill used the resolved project spec override to produce
[`spec.md`](../../specs/002-platform-contracts/spec.md) and its
[`checklists/requirements.md`](../../specs/002-platform-contracts/checklists/requirements.md).
Four independently testable stories, 14 acceptance scenarios, 15 functional
requirements and six measurable outcomes define minimal public contracts,
canonical immutable identity, versioned manifests, host compatibility,
capabilities/dependencies, non-authoritative permission/storage metadata and
bounded safe validation. All twelve constitutional principles have explicit
applicability or later-runtime justification. All 16 built-in author criteria
are satisfied; no material question or before/after specification hook remains.
Serialization, concrete contract versions/range rules, limits and packaging are
reserved for planning. No product implementation, plan, reviewer-owned quality
checklist, runtime proof or human acceptance is claimed.

**002 / clarify is now complete** on that same branch and
[draft PR #5](https://github.com/Stonefly-Labs/Kofte/pull/5).
The installed `speckit-clarify` skill resolved the recorded explicit feature
directory and assessed the specification against the constitution and canonical
sources. **No critical ambiguities detected worth formal clarification.**
Zero questions were asked or answered; no human decision or clarification
session was invented. No specification section was changed. Revalidation of the
built-in author checklist remains **16/16 to 16/16**, with no newly passing
items, regressions or unchecked criteria; its content is unchanged. This does
not evaluate or mark the later reviewer-owned quality checklist.

| Ambiguity category | Status | Requirements-level evidence in spec 002 |
| --- | --- | --- |
| Functional scope and behavior | Clear | Scope, actors and exclusions; US1-4; FR-001/014/015 distinguish declaration validation from activation. |
| Domain and data model | Clear | Key Entities, FR-003-009 and Data Ownership define canonical identity, immutable update comparison, version domains and transient metadata; no registry lifecycle is claimed. |
| Interaction and UX flow | Clear | US1-4 and Experience specify accept/reject, safe correction/retry and valid empty lists; no new web journey or loading/focus state applies. |
| Non-functional quality attributes | Clear | FR-002/010-012 and SC-001-006 define bounded execution, deterministic offline behavior, privacy, side-effect safety and measurable author/performance evidence. |
| Integration and external dependencies | Clear | Accepted/merged 001 is the prerequisite; Interfaces and FR-005/006/013-015 distinguish schema/semantic checks, explicit compatibility context, consumer proof and later runtimes. |
| Edge cases and failure handling | Clear | Edge Cases, US1.2-3/US2.2-3/US3.1-4/US4.1-3 and FR-002/008/010-012 cover malformed/duplicate/over-budget input, compatibility boundaries, safe diagnostics and uncontaminated retry. |
| Constraints and tradeoffs | Clear | Locked versus undecided and principles I-XII preserve minimal contracts, trust, storage/permission boundaries, portability and the numbered release scope. |
| Terminology and consistency | Clear | FR-003-009, Data Ownership and Key Entities separate module ID/display name, schema/module/host/capability versions and declared versus installed capabilities/dependencies. |
| Completion signals | Clear | Four stories, 14 Given/When/Then scenarios, FR-001-015, SC-001-006 and Required Validation provide independently verifiable acceptance and future evidence obligations. |
| Miscellaneous and placeholders | Clear | No unresolved clarification marker or material unquantified requirement; remaining choices are explicitly assigned to planning. |

Serialization/field names, precise range/prerelease rules, initial supported
schema/host/capability versions, numeric budgets, reference conditions and
package/invocation design remain planning work, not unanswered product-policy
questions. No before/after clarification hooks are configured
(`.specify/extensions.yml` is absent). No plan, tasks, product code, package
installation, runtime proof or feature acceptance was produced by clarification.

**002 / plan is now complete** on the same branch and
[draft PR #5](https://github.com/Stonefly-Labs/Kofte/pull/5).
The installed planning skill used the explicit recorded directory and resolved
project template. Its setup output's `BRANCH` value is the feature-directory
basename, not a branch change; actual Git remains `feature/002-platform-contracts`.
The [plan](../../specs/002-platform-contracts/plan.md),
[research](../../specs/002-platform-contracts/research.md),
[data model](../../specs/002-platform-contracts/data-model.md),
[contracts](../../specs/002-platform-contracts/contracts/validation.md) and
[future quickstart](../../specs/002-platform-contracts/quickstart.md) define strict
JSON/schema `1`, distinct initial host/capability contract versions `1.0.0`,
immutable canonical identity, a small SemVer range grammar, explicit context,
non-authoritative declarations and bounded deterministic safe rejection.
The machine-readable schema, examples and independently expected corpus
distinguish structural from semantic/contextual acceptance. One minimal shared
library and test-owned public consumers avoid a new endpoint/runtime/package.

Pre-research and post-design constitutional gates pass at design level, with all
requirements/scenarios mapped to future evidence. Official standards/.NET web
sources were used because Microsoft Learn/Context7 MCP tools are not connected.
No planning hooks are configured. [ADR 0004](../adr/0004-module-manifest-contract.md)
is Proposed pending human review, not an implementation approval. No product
source, dependency adoption, runtime proof, quality-checklist marking or feature
acceptance was added. **Checklist is next** on the same draft PR, including its
human-owned review gate; no intermediate merge or next stage is executed here.

**002 / checklist is now complete** on the same branch and
[draft PR #5](https://github.com/Stonefly-Labs/Kofte/pull/5).
The installed `speckit-checklist` skill resolved the recorded explicit feature
directory and canonical checklist template. It generated
[`checklists/quality.md`](../../specs/002-platform-contracts/checklists/quality.md)
with 36 unchecked, sequential, specification-traced criteria. This standard,
risk-focused requirements review covers contract completeness/clarity/consistency,
identity/version boundaries, permission/storage/MCP applicability, author UX,
bounded diagnostics, privacy, operations, measurable acceptance and human gates.
No checklist hooks are configured.

The reviewer explicitly requested: **"bro you rn through the checklist and tell
me if anything needs my attention.  that's how we'll do checklists from now on"**.
That request authorized evaluation and marking in this stage. All 36 criteria
were assessed against the spec, plan, data model, public contract, schema,
examples, independent corpus, quickstart, research, proposed ADR and constitution;
each satisfied marker has its own evidence. No blocking requirements-quality
gap, canonical conflict or constitutional waiver was identified.

Human attention remains on **Proposed ADR 0004 before implementation**,
especially strict JSON/closed schema, exact-or-two-bound range syntax and
same-core prerelease opt-in, initial version domains, budgets and minimal
library/test-owned author consumer. The unfamiliar-author timed exercise and
reference-environment timing require later actual evidence, not this assessment.
Project locks/inventory, public consumers, corpus/privacy/side-effect tests and
bootstrap preservation likewise remain implementation obligations. Evaluation
authorization is not ADR approval, feature acceptance or a merge request.
The spec/design/schema/examples/corpus and built-in author checklist are unchanged;
no tasks, product code or runtime proof was generated. **Tasks is next** on the
same branch/draft PR, without an intermediate merge. It was not executed here.

**Subsequent ADR 0004 approval**: On 2026-10-21 the human explicitly stated
**"adr 0004 is approved"**. [ADR 0004](../adr/0004-module-manifest-contract.md)
is now Accepted for its design choices, resolving the checklist's design-approval
attention item. Earlier planning/checklist artifacts and ledger notes retain their
historical Proposed/pending-review statements; this later explicit decision is
the current authority for ADR status. Their requirements, markers and historical
evidence hashes are unchanged. The approval does not adopt unreviewed dependencies,
provide runtime/manual evidence, accept feature 002 or authorize merge.
Recording it does not execute another stage: **tasks remains next**, with no
active stage, on the same feature branch/draft PR.

**002 / tasks is now complete** on `feature/002-platform-contracts` and the same
[draft PR #5](https://github.com/Stonefly-Labs/Kofte/pull/5). The installed
`speckit-tasks` skill used the explicit recorded feature directory and resolved
project override to generate
[`tasks.md`](../../specs/002-platform-contracts/tasks.md): **48 unstarted tasks**,
with four setup, five foundational, six US2 identity, eight US3 compatibility,
nine US1 public-author-validation, eight US4 safe-correction and eight
cross-cutting tasks. An interrupted tasks run was resumed on its recorded branch
after verifying that the partial task list and progress-doc changes belonged to
that run; its original start and prior evidence were preserved. The three P1
stories are ordered by actual component
dependencies (US2, US3, US1), followed by P2 US4; US1 remains the first useful
author-facing increment, not feature or MVP completion.

Every task has a concrete path, direct earlier-task prerequisites and requirement/
principle traceability; the list maps all 15 FRs, six SCs, 14 scenarios and twelve
principles. Seventeen parallel markers are conditional on completed dependencies
and non-overlapping files. Mandatory evidence covers original-byte/schema/public
corpus parity, identity immutability, full SemVer/range/declaration boundaries,
two consumers and real cultures with 100 repetitions, exact budgets, private
output, observed zero side effects, clean retry, fail-closed verifier wiring and
unchanged real-provider/browser/startup behavior. Actual unfamiliar-author and
specified reference-timing proof remain explicit implementation gates.

Accepted ADR 0004 resolves design approval, not dependency adoption or runtime/
human evidence. The previous spec/design/schema/examples/corpus, both reviewed
checklists and ADR remain unchanged. No before/after tasks hooks are configured.
No task is executed/checked, product package installed or analysis/implementation/
acceptance report created. The ledger completes tasks only and selects
**analyze** on the same branch/draft PR, without an intermediate merge.

**002 / analyze is now complete** on the same branch and
[draft PR #5](https://github.com/Stonefly-Labs/Kofte/pull/5).
The installed `speckit-analyze` skill resolved the explicit recorded feature
directory and performed a read-only consistency, quality and constitutional
review of the spec, plan, tasks and supporting design. Its
[`analysis.md`](../../specs/002-platform-contracts/analysis.md) records
**Result: pass** with all 15 functional requirements, six measurable outcomes
and 14 acceptance scenarios mapped to concrete work. All 48 unchecked tasks
are mapped, path-specific and dependency-ordered; 17 conditional parallel
markers remain. No actionable ambiguity, harmful duplication, missing coverage,
invalid prerequisite/cycle or constitutional conflict was identified.

Both reviewed checklists and all prior spec 002 evidence hashes are unchanged.
The Accepted ADR 0004 resolves the earlier historical Proposed-status statements;
it does not provide runtime or dependency-adoption proof. Existing solution,
project settings, contract workspace and verifier/CI boundaries were inspected
for feasible integration, not executed as an unbuilt parser. No before/after
analysis hooks are configured. The spec, plan, tasks, research, model, contracts,
schema/examples/corpus, checklists and ADR remain unchanged.

The driver completes analyze only and selects **implement**. Reviewed locks/
inventory, actual public/schema/locale/privacy/effect/retry checks, fail-closed
verifier wiring and preserved full bootstrap local/current-head CI remain
implementation obligations. Actual unfamiliar-author and specified Ubuntu
reference-timing evidence are mandatory, not inferred from this pass. No task
was checked, product code added or feature accepted; no later stage is executed
and no intermediate merge or mark-ready is requested.

The human-directed [workflow correction](../adr/0001-feature-delivery-lifecycle.md)
removes per-step merge gates. Its constitution v2.0.0 amendment travels with this
feature; product principles and explicit human acceptance remain unchanged.

Spec 001 is broader than repository setup: it requires a working API/UI
skeleton and test/local profiles. Specification, clarification, planning,
checklist review, task generation and analysis add no product implementation or human
feature acceptance. Quickstart commands were planning targets at those earlier
stages; the current local guide now identifies implemented commands and links
their actual evidence. Their existence alone is not passing evidence.
The checked requirements
checklist records the specification author's review only; `quality.md` is a
separate human-owned review gate. Its generation did not approve any item.
The reviewer's subsequent explicit request authorized that checklist invocation's
agent-assisted evaluation and evidence-backed marking. Without such a request,
generation would leave the stage blocked for reviewer completion. This completed
requirements review permitted task generation; the subsequent separate read-only
analysis passed. Implementation must honor the remaining ADR, adoption and actual
evidence gates; neither analysis nor eventual task completion is automatic final
acceptance.

Project-specific templates in `.specify/templates/overrides/` carry the constitution
into specification, planning and tasks through the installed resolver. They require
explicit applicability/traceability, pre/post-design checks and mandatory relevant
validation, rather than upstream generic optional-test examples. Upstream template
sources remain unchanged; amendment review must keep the project overrides aligned.

## Sequence and evidence

| Stage | Gate / durable output |
| --- | --- |
| constitution | Versioned constitution, resolved placeholders, required template consistency |
| specify | `spec.md`, `checklists/requirements.md` |
| clarify | Resolved ambiguity recorded in `spec.md`, or a no-ambiguity completion note |
| plan | `plan.md`, `research.md`, `quickstart.md`, plus applicable data model/contracts |
| checklist | Reviewed `checklists/quality.md`; no unchecked quality criteria before advancing |
| tasks | Numbered dependency-ordered `tasks.md` |
| analyze | Read-only cross-artifact review, saved as `analysis.md`; no unresolved blocking findings |
| implement | Completed tasks, updated docs, actual checks and `implementation.md` |
| converge | Append-only gap tasks if needed and `convergence.md`; no product edits |
| review | Requirement/acceptance evidence, exact checks and findings in `review.md` |
| accept | Explicit human decision in `acceptance.md` and recorded approval |

Analysis, convergence and review reports include `**Result:** pass`, `gaps` (converge)
or `changes` (review). Reports must substantiate that result. Both convergence gaps
and scoped review defects return the **next invocation** to implement, then converge
and review again. Design or scope disputes block instead of silently changing intent.

An invocation never performs the next stage as well. A stage that needs answers,
fixes or longer implementation can remain active across invocations; it does not
advance until its evidence and gates pass. Spec 024 is the MVP release gate;
025–038 require explicit authorization to proceed with post-MVP work.

## One feature, one branch, one PR

Start a numbered feature on one fresh typed branch from updated main. Keep that
branch and a single draft PR through specify, clarify, plan, checklist, tasks,
analyze, implement, converge, review and explicit acceptance. Each invocation
still executes only one stage, publishes artifacts and ledger together, and
pushes every commit immediately.

Completed stages are durable branch checkpoints, **not merge gates**. On the next
invocation, inspect the same branch/PR and begin the next recorded stage there.
Use `resume` only for a running/blocked stage, not one already complete. An open
draft PR is expected and does not block the next stage. Preserve existing branch
names even if they contain an earlier stage name.

Do not mark the feature PR ready or request merge until passing final review and
explicit human acceptance. Only the next numbered spec waits for this completed
feature PR to be human-merged with its accepted state on current `origin/main`.
The standalone constitution task likewise must be merged before feature 001.
A prematurely merged or closed-unmerged unfinished feature requires human
reconciliation, not a replacement stage PR.

The helper enforces recorded feature-branch continuity, but is **not** a GitHub
permissions or merge enforcement system; the skill owns Git/PR checks and actual
skill execution. Historical notes/hashes are not rewritten to hide the earlier
per-stage policy; ADR 0001 records the explicit correction.

Feature directories are immutable `specs/NNN-short-name/` paths. Set
`SPECIFY_FEATURE_DIRECTORY` explicitly on each downstream Spec Kit command.
Branch names do not select the feature. `.specify/feature.json` is only a local
ignored pointer, not the durable progress ledger.

## State helper

From repository root:

```bash
python3 .github/skills/drive-speckit/scripts/state.py status
python3 .github/skills/drive-speckit/scripts/state.py prompt
```

`status` is read-only. `prompt` emits only the current constitution/specify fenced
prompt. The driver, not the user, normally performs these mutations:

```bash
python3 .github/skills/drive-speckit/scripts/state.py begin \
  --branch chore/constitution
python3 .github/skills/drive-speckit/scripts/state.py complete \
  --branch chore/constitution --note "Versioned constitution and templates reviewed"
```

Only first specify adds `--directory specs/001-repository-bootstrap`. Mutations
require the actual current branch from `git branch --show-current`. The helper
does not itself create/switch branches, invoke skills, run tests, push or open PRs.

Each completed history entry records stage, feature, branch, timestamp, outcome,
decision note and SHA-256 hashes of required evidence. These hashes describe the
evidence **at that stage**, not a perpetual immutability rule: later clarification
and convergence legitimately change artifacts. Compare changes when reviewing;
never treat hashes as proof that requirements work.

Writes use a short exclusive lock and atomic replacement. Only one driver should
operate on a checkout. Never reset the ledger or edit it to skip a stage. Corrupt,
missing or contradictory state must be reconciled from Git and artifacts.

## Recovery and acceptance

On an interrupted run, inspect its recorded branch and partial artifacts before
resuming. Use `block --branch BRANCH --note "Specific blocker"` for failure, and
`resume --branch BRANCH` only after resolving it. A stale `.specify/drive-state.json.lock`
may be removed only after verifying no writer is active. Do not overwrite partial work.

After a merge conflict, preserve chronological history and reconcile active/next
stage with the actual merged artifacts; do not union two conflicting active runs.
Changed requirements invalidate affected downstream decisions: block, get an explicit
human-approved rework plan, and record its rationale rather than forging completed
history or continuing from stale analysis.

For acceptance, the driver presents the passing review and asks whether the human
accepts **this numbered feature**. It records the exact affirmative decision with
`complete --approval "Human decision"` and links the evidence in `acceptance.md`.
Silence, autopilot, "continue", task checkboxes or a previous stage merge are not
acceptance. Record that decision on the same feature branch, mark its existing PR
ready, and request human merge. Only that completed feature PR's human merge lets
the next numbered spec begin.
