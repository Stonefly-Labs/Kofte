# Tasks: [NNN - FEATURE NAME]

**Input**: Design documents from `specs/[NNN-short-name]/`
**Prerequisites**: Approved spec/plan, research and applicable data model/contracts;
human-reviewed quality checklist and passing analysis before implementation.
**Constitution**: [Version and applicable principles from .specify/memory/constitution.md]

**Tests**: Applicable constitutional and spec-required tests are mandatory, not
optional examples. Include only focused cases proving introduced/fixed behavior.
**Organization**: Group by independently testable user story, with explicit
dependencies and acceptance/requirement traceability.

## Format and Paths

- Use `- [ ] T001 [P] [US1] Description with exact path and requirement/principle IDs`.
- `[P]` means genuinely independent work on different files, not implied stage concurrency.
- Use the approved plan's actual module-local paths, not generic backend/frontend trees.
- Put .NET test-source tasks inside dedicated .NET test projects under `tests/`;
  never store test source in production projects or link it from production slices.
- List prerequisites explicitly; do not infer dependencies from list order alone.
- Replace the sample tasks below with feature-specific work; omit inapplicable work
  with an explanation rather than silently skipping a required gate.

## Phase 1: Setup and Shared Prerequisites

- [ ] T001 [Feature-specific prerequisites/configuration in exact paths; traceability]

## Phase 2: Foundational Boundaries

- [ ] T002 [Applicable module identity/lifecycle, storage, permission and contract prerequisites; paths and requirement/principle IDs]

**Checkpoint**: [Concrete evidence that shared prerequisites are ready]

## Phase 3: User Story 1 - [Title] (Priority: P1)

**Goal**: [Approved user outcome]
**Independent Test**: [Observable acceptance evidence, not MVP-complete status]

### Required Validation

- [ ] T003 [US1] [Focused positive/negative tests with exact paths, requirement IDs and commands; establish regression failure before the fix where applicable]

### Implementation

- [ ] T004 [US1] [Module-local vertical slice and applicable UI/REST/MCP/job/provider/realtime wiring; exact paths, dependencies and requirement/principle IDs]

**Checkpoint**: [Acceptance mapping and actual evidence required for this story]

[Repeat story phases in approved priority/dependency order.]

## Final Phase: Cross-Cutting Evidence and Documentation

- [ ] T005 [Applicable real PostgreSQL/SQLite clean-install, upgrade and isolation tests; registry UNIQUE/concurrent/reserved/63-byte collisions, canonical file/path traversal and recovery; exact paths/commands and IV/V mapping]
- [ ] T006 [Applicable authorization parity, denied tools, contract/version/collision, Playwright UI states/accessibility, event/job retries/cancellation/restart and deployment/reference-module proof; exact paths/commands and II/VI-XI mapping]
- [ ] T007 [Update README and affected operational/configuration/backup/restore/rollback docs; run quickstart and required repository/product checks; exact paths and acceptance mapping]

## Dependencies and Execution Order

[Task-level dependencies, blocking prerequisites and story independence. Include
only genuinely independent parallel groups. Tests precede dependent implementation
where regression/test-first work applies; acceptance checks follow all relevant wiring.]

## Constitutional Coverage and Scope

[Map requirements/stories and applicable principles I-XII to task IDs and evidence.
Explain inapplicable test/interface/storage categories. Preserve optional providers,
MVP 001-024, trusted module/frontend boundaries, shared permissions and module-local
transactions; do not introduce MediatR/AutoMapper or mandatory cloud/IaC/Hoard.
ADRs decide open technology choices, not silently override requirements.]

## Completion and Human Handoff

[Require exact commands/results and acceptance mapping in implementation.md.
One stage per invocation; converge/review/accept remain later stages. Commit stage
artifacts and ledger together and push every commit on the same feature branch/draft PR.
Do not request merge between stages. Hand off for human merge only after passing
final review and explicit feature acceptance.
Checked tasks, successful tests or a merged PR never constitute final spec acceptance.]
