# 0004: Minimal module manifest and validation contract

**Status:** Accepted
**Date:** 2026-10-21
**Spec:** [002 - Platform contracts and module identity](../../specs/002-platform-contracts/spec.md)

## Context

Module authors and the host need identical identity/compatibility rules without
running the host or granting module authority. Spec 002 assigns serialization,
range semantics, budgets, initial version domains and package boundaries to
planning. Registry allocation, lifecycle, authorization and executable extension
SDKs remain later numbered features. The accepted bootstrap stays module-free.

## Decision

Use one dependency-light `Kofte.Platform` library for immutable descriptive
types and a pure bounded validator, with no loader, dispatch framework, storage
or host implementation dependency. .NET test source stays physically in its
dedicated test projects. A console under `tests/contract` demonstrates independent
author consumption, not a product installation CLI or new endpoint.

Use strict UTF-8 JSON, closed JSON Schema 2020-12 schema `1`, initial host
contract `1.0.0` and nine capability categories with contract version `1.0.0`.
Schema-only success is structural, not contextual acceptance. Immutable canonical
reverse-DNS IDs follow the already specified ASCII/DNS-sized grammar.

Use SemVer 2.0.0 precedence and a deliberately small Köfte-only range grammar:
exact version or lower/upper comparator pair. Prereleases require explicit
same-core prerelease opt-in; build metadata does not affect precedence.
Bounded decimal-text comparison avoids numeric overflow and new package adoption.
Use existing runtime `System.Text.Json`, test-workspace Ajv and xUnit.

Publish exact byte/depth/collection/string/output budgets, stable safe diagnostics
and independently expected structural/semantic fixtures together. No input text,
exception, path, external reference or executable hook is output or activated.
Permission names are module-local descriptive identities; `storage.relational`
is a need, never a registry allocation.

The [plan](../../specs/002-platform-contracts/plan.md),
[research](../../specs/002-platform-contracts/research.md) and
[public contract](../../specs/002-platform-contracts/contracts/validation.md)
give the exact rules and alternatives. No constitutional amendment is required.

## Alternatives

YAML and a runtime schema engine add dependencies without needed behavior.
Broad npm/NuGet range support or `System.Version` would introduce different or
incomplete semantics. Hosting shared types in the web assembly couples authors
to runtime/storage dependencies. Speculative module lifecycle/provider/MCP
interfaces preempt later features. A validation upload endpoint introduces
authority and an attack surface not required by this local author operation.

## Consequences

The small range parser requires thorough independent boundary tests; the schema
intentionally cannot detect raw duplicate keys, semantic object-identity
uniqueness or host compatibility. Both public consumers must agree and schema
parity must be checked offline. Project locks/dependency inventories still need
review during implementation even without a new runtime package.

The initial closed vocabulary/rules are a compatibility promise: incompatible
grammar, field, budget or diagnostic-identity changes require reviewed version/
schema decisions, not reinterpretation. No database or migration changes occur;
rollback is source/manifest management. Future 003/004 still owe persistent
collision-safe mappings, isolated real-provider storage and lifecycle checks.
Later protected adapters still owe shared actor/team/resource permissions/audit.

## Approval

Human decision on 2026-10-21: **"adr 0004 is approved"**.
This explicitly approves the ADR's design choices for implementation on
[draft PR #5](https://github.com/Stonefly-Labs/Kofte/pull/5), including strict JSON,
the restricted version-range/prerelease rules, initial version domains, validation
budgets and the minimal shared library with a test-owned author consumer.

This resolves the design-approval attention item from the requirements-quality
review. It does not approve an unresolved dependency graph, establish runtime,
reference-timing or unfamiliar-author evidence, accept numbered feature 002 or
the MVP, or authorize a merge. Those gates remain mandatory; the feature PR stays
draft and the next recorded stage remains tasks. No implementation is performed
by recording this decision.

## Human-directed benchmark amendment

On 2026-10-21 the human explicitly directed: **"bro that ubuntu benchmark you're
trying to hit, we don't have the hardware, so we have to losen that restriction
way up"**.

Remove the fixed Ubuntu 24.04 x64 / 2-vCPU / 8-GiB benchmark requirement for
**spec 002**. SC-004 may run on any available supported developer workstation
or CI runner, with the actual source revision, pinned SDK/runtime, OS,
architecture, CPU and memory recorded. No exact CPU count, RAM size, dedicated
benchmark host, VM sizing or matching cross-machine performance is required.
Retain initialization followed by 100 representative individually timed calls,
including invalid/oversized/deep input, each below one second. This preserves
bounded-failure evidence without inaccessible hardware becoming a delivery gate.

The spec, plan, tasks, guide and implementation report are updated together.
Earlier analysis and reviewer-owned checklist evidence describe the earlier
requirement at their checkpoint; they are preserved, not silently re-marked.
The existing macOS measurements can satisfy the amended environment requirement;
they are not relabeled Ubuntu measurements. This does not waive unfamiliar-human
SC-001, change spec 001's historical evidence, accept spec 002, or authorize merge.

## Human-directed author-guide amendment

After the manual T045 exercise was explained, the human explicitly directed:
**"brah, that's dumn, I don't want to do that"**.

Remove the unfamiliar-author exercise, ten-minute completion target and human
elapsed-time attestation as implementation requirements for **spec 002**.
Replace SC-001/T045 with automated execution of the documented public author
consumer: validate the example, observe the documented identity/compatibility
failures, correct them and verify accepted retry, without host edits or external
services. Existing public-consumer/retry tests remain mandatory.

This changes the requirement; it does **not** assert that a human exercise
occurred or that automation proves unfamiliar-user usability or completion time.
The earlier benchmark amendment retained the former SC-001 at its checkpoint;
this later explicit direction supersedes that condition only. The available-
machine timing policy, all other product checks, separate convergence/review,
explicit human feature acceptance and human-only merge remain unchanged.

Update the spec, plan, research, tasks, corpus/guide and implementation/progress
docs together. Preserve earlier analysis, reviewer-owned checklist markings and
ledger entries as historical evidence, not as current claims of a human study.
No constitution principle, package, runtime API, schema or migration changes.
