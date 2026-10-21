# Köfte documentation

## Read in this order

1. [Product charter](product-charter.md): mandate, boundaries and required outcomes.
2. [Product vision](product-vision.md): audience and experience.
3. [Technical architecture](architecture.md): architecture invariants and recommendations.
4. [Master spec inventory](master-spec-inventory.md): exact MVP boundary and ordered prompts.
5. [Constitution](../.specify/memory/constitution.md): adopted product principles and v2.0.0's human-directed workflow amendment; the [constitution prompt](constitution-prompt.md) remains its exact generation input.

These are the canonical product inputs. The constitution governs delivery; its [one-feature/one-branch amendment](adr/0001-feature-delivery-lifecycle.md) records the explicit human correction and awaits the completed feature's merge. A conflict with canonical inputs must be surfaced for a human decision and documented amendment, not silently resolved by an assistant.

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
task. Spec 001 remains unaccepted on its original branch and draft PR, without
an intermediate merge. Final review is next; explicit human acceptance remains
a separate gate. Modules, SDKs, workers and Helm remain reserved.
