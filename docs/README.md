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
- [ADRs](adr/README.md): record consequential decisions.

Feature specs live in root **`specs/`** to match the installed Spec Kit scripts, rather than a second `docs/specs/` tree. The first [draft specification, 001](../specs/001-repository-bootstrap/spec.md), defines the future working foundation. Clarification found no material requirements ambiguity; its [plan and design artifacts](../specs/001-repository-bootstrap/plan.md) define local startup, minimal contracts and required verification. [ADR 0002](adr/0002-bootstrap-development-baseline.md) records the human-approved technical design baseline, not dependency or feature acceptance. At the reviewer's explicit request, agent-assisted evaluation of the [human-owned quality checklist](../specs/001-repository-bootstrap/checklists/quality.md) satisfied 36/36 criteria with evidence, no blocking requirements gaps and explicit attention items. Task generation produced [64 tasks](../specs/001-repository-bootstrap/tasks.md) covering all four stories with explicit dependencies, requirement/constitution mappings and mandatory validation. The subsequent read-only [analysis](../specs/001-repository-bootstrap/analysis.md) passed: all 23 requirements/outcomes and 15 acceptance scenarios have task coverage, with no unmapped tasks or actionable/constitutional findings. The [implementation entry checkpoint](../specs/001-repository-bootstrap/implementation.md) records the human ADR approval, T001 completion and same-stage resumption on the existing feature branch/draft PR without merging. T002's dependency adoption is next; 63 tasks remain unchecked. Design coverage is not runtime evidence. Dependency-adoption gates, actual product/manual evidence and final human acceptance are not claimed complete. Product commands in the planning guide are future targets; reserved product directories remain placeholders, not implemented components.
