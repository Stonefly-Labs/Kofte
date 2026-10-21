# Feature Specification: [NNN - FEATURE NAME]

**Feature Branch**: `[typed-task-branch]`
**Feature Directory**: `specs/[NNN-short-name]`
**Created**: [DATE]
**Status**: Draft
**Input**: User description: "$ARGUMENTS"
**Constitution**: [Version and applicable principles from .specify/memory/constitution.md]

## Scope and Dependencies

- Inventory entry, MVP/post-MVP boundary and explicit exclusions.
- Required interfaces/dependencies, distinguishing prerequisites from later integrations.
- Core/module owner, actors and team/resource boundaries.
- Canonical sources and open questions; do not silently settle conflicting requirements.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - [Title] (Priority: P1)

[Actor, user-visible goal and value. Add independently testable stories as needed.]

**Why this priority**: [Rationale]
**Independent Test**: [Observable proof of this story, not an MVP-complete claim]

**Acceptance Scenarios**:

1. **Given** [authorized scope/state], **When** [action], **Then** [observable result].
2. **Given** [denied scope or failure], **When** [action], **Then** [safe denial/recovery].

### Edge Cases

[Boundary inputs, permission failures, collisions, unavailable dependencies and recovery.]

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST [observable capability and verifiable outcome].

### Interfaces and Permission Parity

[Applicable UI/REST/MCP/jobs/providers/realtime behavior, registered permission and
actor/team/resource scope. The same operation must have the same authorization;
state explicit reasons for inapplicable surfaces.]

### Data Ownership and Compatibility

[Module identity, versioned contracts/resources/events and desired/observed status.
For persistence: immutable registry mapping, PostgreSQL schema/context/history per
module with 63-byte/reserved-name/UNIQUE/concurrency protection; SQLite physical file
per module plus separate platform file, canonical paths and traversal rejection.
No cross-module table access or atomicity. State migration, backup and rollback/
forward-recovery requirements, or explain inapplicability.]

### Experience, Security and Operations

[Loading/empty/failure/permission states, measurable accessibility/performance;
input validation, secret handling, audit/telemetry; durable retries/idempotency/
cancellation/restart recovery where applicable. Describe trusted module/frontend
boundaries, portable deployment/configuration and diagnostics without choosing
unresolved implementation details.]

### Key Entities *(if applicable)*

- **[Entity]**: [Ownership, scope, relationships and lifecycle.]

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: [Technology-agnostic, independently measurable user outcome.]

### Required Validation

[Explicitly require applicable constitution XI tests: focused unit/regression;
real PostgreSQL and SQLite isolation/migration integration; module/REST/event/MCP
contracts; Playwright journeys; negative authorization/collision/path/tool/restart
cases; deployment smoke and reference-module proof at their release gates.
Map acceptance scenarios to evidence and explain every inapplicable category.
.NET test source belongs physically to its dedicated test project, not the
production project or vertical slice it tests.]

## Assumptions and Constitutional Review

[List assumptions and unresolved decisions; distinguish locked invariants from
recommendations/ADRs. Map applicable principles I-XII to requirements or justify
inapplicability. No MediatR/AutoMapper, mandatory vendor/IaC/Hoard dependency,
untrusted in-process sandbox claims or external MCP/AI-chat MVP expansion.]
