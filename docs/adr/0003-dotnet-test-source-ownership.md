# 0003: .NET test source belongs to its test project

**Status:** Accepted

**Date:** 2026-10-21

**Spec:** [001 - Repository bootstrap](../../specs/001-repository-bootstrap/spec.md)

## Context

The original VSA illustration and bootstrap plan put .NET unit-test source in
production slice folders and linked it into test projects. The human explicitly
rejected that source layout during implementation.

## Decision

Every .NET test source file lives physically inside its dedicated .NET test
project under `tests/`, never in a production project. Test projects use normal
SDK source inclusion and project references, not links to test files in `src/`
or production modules. Their internal folders may mirror module/feature ownership.

Bootstrap unit tests live in `tests/unit/Kofte.Host.Tests/Bootstrap/`;
provider integration tests remain in `tests/integration/Kofte.Host.IntegrationTests/`.
Production vertical slices remain feature-local. Frontend test colocation is
unchanged. This is a source-organization clarification, not a test-coverage waiver.

## Alternatives

Linking production-folder tests requires explicit host exclusions and permits
tests to enter the wrong assembly accidentally. Keeping a dedicated test project
while storing its source elsewhere does not satisfy the requested ownership.

## Consequences

Remove linked-source includes and host test exclusions, move existing .NET
test files without changing their behavior, and correct the canonical layout,
project templates and bootstrap task paths. Constitutional principle III
clarifies physical .NET source ownership while preserving logical module-local
test coverage. Historical analysis/ledger evidence remains historical.

## Approval

Explicit human direction: **".net test cases go in dotnet test projects and the
source is stored with the test project, not with the project it's testing"**.
Recorded on the same unfinished feature branch and
[draft PR #4](https://github.com/Stonefly-Labs/Kofte/pull/4).
This does not accept feature 001 or waive its remaining implementation gates.
