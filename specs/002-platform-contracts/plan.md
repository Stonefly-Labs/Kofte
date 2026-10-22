# Implementation Plan: 002 - Platform Contracts and Module Identity

**Branch**: `feature/002-platform-contracts` | **Date**: 2026-10-21 | **Spec**: [spec.md](spec.md)
**Input**: `specs/002-platform-contracts/spec.md`
**Constitution**: Adopted v2.0.1
**Status**: Planning checkpoint; no product implementation or feature acceptance.

**Implementation amendment (2026-10-21):** The human removed SC-004's fixed
Ubuntu/CPU/RAM benchmark profile; see
[ADR 0004](../../docs/adr/0004-module-manifest-contract.md#human-directed-benchmark-amendment).
Available supported workstations/CI runners with actual measured metadata are
valid evidence. Earlier design-only statements remain historical.

**Subsequent author-guide amendment:** The human removed the unfamiliar-author
timed exercise. SC-001 is now automated guide/example/error/corrected-retry proof;
see [ADR 0004](../../docs/adr/0004-module-manifest-contract.md#human-directed-author-guide-amendment).
No human usability/time claim or feature acceptance is inferred.

## Summary

Give authors and host consumers one small, offline .NET contract for interpreting
module manifests before any code, storage or permission is activated. Implement
strict JSON input, immutable reverse-DNS identity, SemVer 2.0.0 comparisons,
explicit host compatibility, nine contribution categories, descriptive dependency/
permission/storage metadata and bounded safe rejection (FR-001-015).

The design adds one justified `Kofte.Platform` library, not a plugin runtime or
general dispatch framework. A standalone author consumer and host-facing contract
tests use its same public validator. JSON Schema 2020-12 defines the structural
document; documented semantic/contextual checks supply the rest. Initial schema
`1`, host contract `1.0.0` and capability contracts `1.0.0` are distinct version
domains. [Proposed ADR 0004](../../docs/adr/0004-module-manifest-contract.md)
records these choices; planning does not turn it into human approval.

## Technical Context

**Language/Version**: Existing `net10.0`, C# supplied by SDK 10.0.402/runtime
10.0.13; nullable analysis, warnings-as-errors and deterministic builds remain.
No frontend change; the accepted Node/Next.js/TypeScript baseline stays untouched.

**Primary Dependencies**: Runtime `System.Text.Json` and standard collections;
native direct calls, no MediatR/AutoMapper, new runtime NuGet package or schema
engine. Retain the centrally pinned xUnit 4.0.2/test SDK 18.11.0/runner 4.0.1 in
new dedicated test projects. New projects still need reviewed lock files and an
updated dependency inventory before implementation is declared complete; no
dependency is adopted by this plan. See [research](research.md).

**Storage**: None. Input bytes, explicit context and immutable results are
transient. No registry, connection, filesystem selection, DbContext, migration,
schema/table, backup format or cross-module transaction.

**Testing**: Existing xUnit runner for platform unit tests and public contract
consumers; the existing pinned Ajv 8.20.0 in `tests/contract` can check this
Draft 2020-12 schema offline with its `ajv/dist/2020.js` entry point. Reuse it,
do not install a second schema engine. Corpus, locale, budgets, safe-output and
side-effect checks must run locally and in the existing named verifier/CI.
Exact planned commands and evidence are in [quickstart](quickstart.md).

**Target Platform**: Existing macOS 15+ arm64 and Ubuntu 24.04 x64. The library
needs neither Docker nor a running host; existing SQLite/PostgreSQL bootstrap
regressions retain their owned fixtures. No Kubernetes deployment change.

**Project Type**: Platform library and a narrow author-validation console
consumer under `tests/contract`; no HTTP, UI, MCP or installed module surface.

**Performance Goals**: SC-004's every-check limit is one second after
initialization, for 100 consecutive valid/invalid/over-budget checks. SC-003
requires 100 corpus repetitions in each of two consumers and two locales.
SC-001 requires executable automated guide/correction/retry evidence, not a
timed human exercise.

**Constraints**: Maximum 65,536 input bytes, depth 8, nine capabilities, 64
dependencies, 128 permissions, 32 returned diagnostics; precise string/range
limits and rejection precedence are in [the public contract](contracts/validation.md).
No secrets/raw values, external resolution, loading or mutable accepted result.

**Scale/Scope**: One manifest and explicit context per call. 002 depends only on
accepted/merged 001. Runtime composition/storage/auth/MCP and all 025-038 work
remain excluded. The author console is a proof/guide consumer, not a module
installer, product CLI platform or packaged reference module.

## Constitution Check

**Pre-research gate: pass.** Scope review against the accepted bootstrap, spec
and canonical sources found no conflict requiring a waiver. **Post-design gate:
pass at design level**, with the evidence below. Neither gate proves runtime
behavior. No mandatory/optional planning hooks are configured:
`.specify/extensions.yml` is absent.

| Principles | Design evidence / decision | Before research | After design |
| --- | --- | --- | --- |
| I, XII: Brand, optional modules, exact inventory scope and dependency | Köfte prose, technical `kofte`; only 002; accepted/merged 001; original branch/draft PR. No optional module or post-MVP dependency. | Pass | Pass: summary, exclusions, ADR 0004 |
| II, III: Minimal modular contracts, immutable IDs, versioning, slices, trusted code, native DI and test ownership | Share only identity/manifest/result types and pure validator. One demonstrated common library; no lifecycle or speculative runtime interfaces. No MediatR/AutoMapper. | Pass | Pass: public contract, data model and concrete test-project paths |
| IV, V: Registry, isolated storage/migrations and honest transactions | No storage effects or physical identifiers. Later 003 owes SQL-safe 63-byte/reserved-name/UNIQUE/concurrency enforcement and permanent mappings; SQLite separate module/platform files and canonical paths; no shared connection/table access/ACID. | Pass; allocation/migration N/A here | Pass: `storage.relational` is a need, never an allocation |
| VI, VII: Authorization parity, safety and early MCP | Local data validation grants no permission/trust; no new remote endpoint. Bound untrusted input and never echo values. MCP category is not execution. OIDC, central policy and audit remain 005/006/009; MCP runtime remains 007/013. | Pass; auth/runtime N/A here | Pass: explicit context, safe codes/paths, corpus and no-output library |
| VIII: Resources, durable events/jobs, providers and SignalR | Declaration categories only; no resource state, event delivery, job persistence, provider calls or subscriptions. Those runtimes remain 012/016-019. | Pass; runtime N/A here | Pass: closed capability vocabulary, no service/handler hooks |
| IX: Accessible UI and trusted frontend composition | No new page or frontend SDK. Author diagnostics are readable fixed text; Unicode display metadata is preserved. No remote hot-load claim. Existing browser regressions retained. | Pass; new UI N/A here | Pass: console contract and unchanged bootstrap routes |
| X, XI: Portable profiles, observability, recovery and applicable evidence | Offline after restore; no mandatory cloud/service. Unit, schema/consumer, privacy, budget and retry evidence required. Existing real-provider/HTTP/browser tests protect bootstrap; new allocation, migration, auth and release proof belong to later specs. | Pass | Pass: quickstart, corpus, requirement/scenario mapping below |
| Governance: Traceability, ADR/license review, human gates and serialized delivery | Existing spec is clarified, not accepted; one stage only. ADR remains Proposed. Later reviewer checklist is not generated/marked. No package adoption without review. | Pass | Pass: research, ADR, ledger checkpoint and same draft PR #5 |

## Project Structure

### Documentation (this feature)

```text
specs/002-platform-contracts/
  spec.md
  plan.md
  research.md
  data-model.md
  quickstart.md
  checklists/requirements.md
  contracts/
    module-manifest.schema.json
    validation.md
    acceptance-corpus.md
    examples/minimal.json
    examples/full.json
    examples/duplicate-key.txt
```

`quality.md`, tasks, analysis, implementation and acceptance reports belong to
later stages and are deliberately absent.

### Source Code (repository root; future implementation paths)

```text
src/Kofte.Platform/
  Kofte.Platform.csproj
  Modules/Manifest/           # Public immutable types and validator; local helpers
tests/unit/Kofte.Platform.Tests/
  Kofte.Platform.Tests.csproj
  Modules/Manifest/           # Grammar, invariants, limits, ordering and retry
tests/contract/Kofte.Platform.ContractTests/
  Kofte.Platform.ContractTests.csproj
  Modules/Manifest/           # Public corpus, host consumer, locale/budget checks
tests/contract/Kofte.Manifest.Consumer/
  Kofte.Manifest.Consumer.csproj
  Program.cs                 # Independent author consumer; public library only
tests/contract/
  module-manifest.test.ts     # Existing Ajv, structural corpus only
scripts/verify.py             # Wire new projects into unit/contract/quality
```

**Structure Decision**: All .NET tests/consumer-fixture source physically belongs
under its own project in `tests/`; never include/link source from production
slices. The host may reference `Kofte.Platform`, but has no manifests to load:
prove the host dependency direction through contract tests referencing the host
and library, without new startup invocation, configuration or endpoint.
Add the projects to the existing solution during implementation. No
`Kofte.Infrastructure`, worker, module SDK package, runtime loader or separate
versioning framework is justified.

## Design, Migration and Recovery

| Requirements | Owner/design | Observable verification |
| --- | --- | --- |
| FR-001/013/014 | Platform owns schema `1` and public types. Shared structural/semantic corpus; independent console plus host-facing public consumer. No copied validation rules. | US1.1/4; schema parity and author exercise |
| FR-002 | Strict bounded UTF-8 JSON, no BOM/comments/trailing commas/repeated decoded keys; closed objects, required explicit collections/storage. | US1.2/3; exact/one-over budgets, unknown keys and raw duplicate fixtures |
| FR-003/004 | Immutable `ModuleId`; ordinal canonical reverse-DNS grammar; explicit prior-ID comparison. No normalization or registry uniqueness assertion. | US2.1-3; long-label/total-length boundaries, aliases and update matrix |
| FR-005/006 | SemVer 2.0.0, separately versioned schema/host/capabilities; small Köfte-only exact/two-bound range grammar, explicit prerelease opt-in. No context defaults. | US3.1/2; independent boundary expectations including build metadata |
| FR-007/008/009 | Closed versioned kinds, unique identities, dependencies not resolved, permission `(moduleId,name)` ownership, storage need only. | US3.3/4; every kind, duplicate-by-ID regardless of version, unsafe physical fields |
| FR-010/011 | Disjoint accepted/rejected results; defensive immutable collection copies; staged validation and fixed ordering/truncation. | US1.4/US4.3; two consumers/locales, mutation attempts, reject-then-retry |
| FR-012 | No IO, telemetry emission, code loading or reflection-based type activation in library. Raw names/values/exceptions never enter diagnostics. | US4.1/2; canaries, forbidden side effects, offline execution and process/output checks |
| FR-015 | No changes to host startup, health, OpenAPI, storage or mandatory infrastructure; lock/inventory review for new projects. | Both-provider existing foundation and affected build/format/dependency gates |

The [data model](data-model.md) defines transient relationships, immutable outputs
and state transitions. [Validation contract](contracts/validation.md) is
normative for this proposed design; schema checks are intentionally a subset,
not a separate acceptance authority. Its offline schema URI is not fetched.

There is no database clean-install, upgrade, installation lock, migration order
or rollback to implement in 002. Revert authored manifests or library/schema
source as ordinary source control; retain rejected original input privately and
retry corrected bytes. Never reset bootstrap data as a parser remedy. Future
registry and lifecycle specs must establish their own real-provider migration/
backup/forward-recovery evidence; validation neither creates nor renames data.

Breaking grammar, required-field, capability, budget or diagnostic-identity
changes require a reviewed schema/host-contract compatibility decision and ADR
update, not silent reinterpretation. Old raw manifests remain untouched.
Permission metadata has no grant or scope-enforcement effect. No frontend
loading/accessibility states, concurrency tokens, secret store, outbox,
cancellation lease or tool-safety execution policy is invented without a runtime.

## Validation and Operational Handoff

All commands in [quickstart](quickstart.md) for new projects are **future
implementation targets**, not commands executed by planning. Existing repository
checks are executed for this checkpoint and reported in its PR.

| Acceptance coverage | Required evidence |
| --- | --- |
| US1.1-4; SC-002/003 | Structural corpus via existing Ajv and semantic corpus via both public consumers; repeated keys checked from original bytes, not reparsed fixtures; deterministic ordered `(origin,path,code)` across `en-US`/`tr-TR`, 100 repetitions each |
| US2.1-3; SC-002 | ID label lengths 63/64, total lengths 253/254, all alias/path/Unicode cases, changed display/release without ID change, prior-ID mismatch, immutable outputs |
| US3.1-4; SC-002/006 | Every range boundary/operator, prerelease opt-in and build case, malformed/empty ranges, each supported kind and contract version, duplicate/self/absent dependencies; installed sets explicitly not inspected |
| US4.1-3; SC-004/005 | Every limit at/one-over, malformed/deep/large input, fixed max diagnostics, value/key/control-character canaries, no outbound requests/code activation/module-owned files or schemas, corrected retry without state |
| US1-4; SC-001/004 | Automated author-guide validation, identity/compatibility failures and accepted corrections; no timed human exercise. Separately, available supported workstation/CI runner with actual revision/SDK/runtime/OS/architecture/CPU/memory, no fixed resource profile; 100 initialized checks each below 1 second, not aggregate/p95 or cold process startup |
| FR-015; SC-006 | Named unit/contract plus existing real SQLite/PostgreSQL, health, startup and browser suites; wiring into existing quality/CI/fail-closed verifier, no zero-test or skipped-project success |
| Principles III/X/XI/XII | Dedicated test source, reviewed locks/dependency graph, no generated/local artifacts; exact command/outcome/revision reports and no invented runtime/human proof |

Schema-only errors differ from the public validator's stable diagnostic taxonomy;
the contract tests assert the shared **structural decision**, not Ajv's internal
message or ordering. Semantically invalid schema-valid cases must still reject
through both consumers. Bounded input and an adversarial corpus constrain CPU
work; no regex with unbounded backtracking may be used for the production
version/range/identity parser. Slow or unavailable evidence blocks implementation
completion rather than becoming a default pass.

## Complexity Tracking and Decisions

| Proposed choice | Need and simpler alternative | ADR / human decision |
| --- | --- | --- |
| One shared platform library | Both author and host consumers need identical pure rules without host dependencies; keeping them in the web host defeats FR-001/013. No additional runtime abstractions. | ADR 0004, Proposed |
| Small hand-written version/range parser | Explicit limited grammar and bounded decimal-text comparison avoid a new package and accidental npm/NuGet compatibility. Requires thorough independently expected boundaries. | ADR 0004 and research R2 |
| Author console under tests/contract | Demonstrates an external public consumer and reproducible guide without a product CLI/endpoint; shares implementation, not parsing rules. | ADR 0004 |

No constitutional violation, unresolved material clarification or canonical
conflict remains. The next stage is **checklist**, including the human-owned
requirements-quality gate; it is not executed here. ADR approval, runtime
evidence and explicit numbered-feature acceptance are not inferred.
