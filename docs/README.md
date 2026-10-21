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

**002 / specify, clarify and plan are complete** on `feature/002-platform-contracts`: the
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
[ADR 0004](adr/0004-module-manifest-contract.md) remains Proposed. No product
code or module runtime is added. Checklist is next on the same draft PR, with
human-owned quality review and no intermediate merge or inferred acceptance.
