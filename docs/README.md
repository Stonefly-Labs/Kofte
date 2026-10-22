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
product or measurement is claimed. The same PR #5 is ready for human merge.
That merge, with accepted ledger/evidence on main, is required before a later
invocation may begin 003. Acceptance does not start it.
