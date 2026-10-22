# Tasks: 002 - Platform Contracts and Module Identity

**Input**: [spec.md](spec.md), [plan.md](plan.md), [research.md](research.md),
[data-model.md](data-model.md), [validation contract](contracts/validation.md),
[acceptance corpus](contracts/acceptance-corpus.md), schema/examples and
[quickstart.md](quickstart.md).
**Prerequisites**: The author checklist is satisfied (16/16); the reviewer
explicitly authorized evaluation of the requirements-quality checklist (36/36,
with evidence). [ADR 0004](../../docs/adr/0004-module-manifest-contract.md)
is Accepted: **"adr 0004 is approved"**. Earlier design/checklist statements
that it is Proposed are historical, not an outstanding approval gate.
Passing read-only analysis is still required before implementation.
**Constitution**: Adopted v2.0.1; applicable principles I-XII are mapped below.
**Status**: Implement complete. All 48 tasks have evidence in
[implementation.md](implementation.md). The human-directed ADR 0004 amendment
removes T044's fixed Ubuntu/CPU/RAM profile; actual available-machine measurements
satisfy T044. The subsequent author-guide amendment replaces T045's manual timed
exercise with actual automated guide/correction/retry proof. No human exercise
is claimed. Converge is next on the same draft PR; it is not executed here.
No feature acceptance is claimed. Historical stage-generation notes below
describe the original task-list checkpoint, not current task status.

**Tests**: Spec-required and applicable constitutional tests are mandatory.
Use independently expected cases for introduced behavior, not implementation
constants, copied validation logic or tests of trivial accessors.
**Organization**: Setup and shared foundations precede story work. The three
P1 stories are ordered by their implementation dependencies: US2 identity,
US3 compatibility/declarations, then US1 complete public author validation.
US4 (P2) strengthens failure/privacy/budget evidence. This avoids publishing a
partially implemented validator that accepts declarations it has not checked.
US1 remains the first useful author-facing increment, not feature/MVP completion.

## Format and Paths

Each task has an unchecked checkbox, sequential ID, optional `[P]`, and an
original-spec `[USn]` label for story work. `Prerequisites: none` means no earlier
task, not permission to bypass the stage/analysis gate. Other prerequisites are
explicit direct task IDs; transitive prerequisites also apply. `[P]` permits
parallel execution only after those prerequisites finish and only across
non-overlapping files. It never authorizes parallel Spec Kit stages.

Production code belongs in `src/Kofte.Platform/Modules/Manifest/`, namespace
`Kofte.Platform.Modules.Manifest`. All .NET test/fixture source belongs inside
its own SDK-included project under `tests/`, never in or linked from production.
The author console is test-owned, not a product installer or the spec 020
reference module. Listed future paths are instructions, not claims they exist.
Keep the existing module-free host, routes, stores, frontend and reserved
module/SDK/worker/Helm directories unchanged except for the planned host project
reference. Do not add MediatR, AutoMapper or a new runtime package.

## Phase 1: Setup and Shared Prerequisites

- [x] T001 Verify the passing `specs/002-platform-contracts/analysis.md`, both reviewed checklists, Accepted `docs/adr/0004-module-manifest-contract.md` and recorded feature branch before implementation; start `specs/002-platform-contracts/implementation.md` with actual gate evidence, scope and outstanding guide/timing obligations. Block on an unresolved analysis/design finding rather than treating this task list as approval. Prerequisites: none. (FR-014/015; C:III/XI/XII)
- [x] T002 Create `src/Kofte.Platform/Kofte.Platform.csproj`, `tests/unit/Kofte.Platform.Tests/Kofte.Platform.Tests.csproj`, `tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj` and `tests/contract/Kofte.Manifest.Consumer/Kofte.Manifest.Consumer.csproj`, and add them to `Kofte.slnx`. Reuse existing `net10.0`, nullable/warnings-as-errors/deterministic settings and centrally pinned test tooling; author console references only the public library, contract tests reference host/library, and host references library through `src/Kofte.Host/Kofte.Host.csproj` without changing startup. No internals access, linked test source, production test package or new module runtime. Prerequisites: T001. (FR-001/013/015; C:II/III/X/XI)
- [x] T003 Generate/review each new project's `packages.lock.json` after project-manifest changes; reconcile the exact direct/transitive graph, provenance, licenses, retained notices and adoption decision in `docs/development/dependencies.md` and `docs/development/dependency-inventory.json` using the existing dependency tooling. Reuse runtime `System.Text.Json`, xUnit and Ajv 8.20.0; unknown or unacceptable obligations block adoption even without a new runtime package. Prove `dotnet restore Kofte.slnx --locked-mode` and the dependency check preserve reviewed locks/evidence; do not change pins merely to use a locally installed version. Prerequisites: T002. (FR-015; C:III/VI/X/XI)
- [x] T004 [P] Establish independently expected corpus data/recipes in `tests/contract/Kofte.Platform.ContractTests/Modules/Manifest/ManifestCorpus.cs` from `contracts/acceptance-corpus.md`, its C01-C34 variants, the full range table and limit recipes. Reuse the spec's exact schema/examples/raw duplicate fixture with explicit test-data copying or byte-parity checks; do not copy production source or derive expectations from the validator. Preserve original malformed/duplicate/encoding bytes; keep generated canaries transient and clean up only owned fixture paths. Prerequisites: T003. (FR-002/010-013; SC-002/003/005/006; C:III/VI/XI)

## Phase 2: Foundational Boundaries

- [x] T005 [P] Add focused contract-envelope/diagnostic tests in `tests/unit/Kofte.Platform.Tests/Modules/Manifest/ValidationResultTests.cs`: disjoint accepted/rejected cases, no partial descriptor, immutable nonempty rejection list, fixed safe messages/known paths and context-before-manifest/field/numeric-index/code ordering with exact 32/33-error behavior. Establish failure before the implementation below; do not test accessor constants alone. Prerequisites: T003. (FR-010/011/012; C:III/VI/XI)
- [x] T006 Implement `ManifestDiagnostic.cs` and `ManifestValidationResult.cs` under `src/Kofte.Platform/Modules/Manifest/`, carrying the model constraints verbatim: "Exactly one of `Accepted(Description)` or `Rejected(Diagnostics)`", "Never partial descriptor plus errors. Rejected has 1-32 immutable diagnostics; accepted has no rejection diagnostics" and "Stable safe machine identity `(origin,path,code)`; known paths/bounded indices, no raw untrusted values or exception details." Implement the contract's complete fixed category/corrective-message table, known-path construction, ordering and first-31-plus-`diagnostics_truncated` behavior, without caller-supplied message text or operational output. Prerequisites: T005. (FR-002/010-012; C:III/VI/XI)
- [x] T007 [P] Define `ModuleDescription.cs`, declaration value types and `ManifestValidationContext.cs` under `src/Kofte.Platform/Modules/Manifest/` with internal accepted-description construction and get-only fields; preserve "Complete accepted interpretation only; constructor internal; immutable object graph with private collection copies. All required JSON fields present. No installation/team/status authority." Context preserves "Required `HostContractVersion` string; optional `PreviousModuleId` string" and "Supplied explicitly; invalid/missing context is a rejection, no environment/product defaults. Prior identity compared ordinally after validation." Never expose a mutable accepted graph or success factory that bypasses the validator. Grammar/field validation is completed by the story tasks before public acceptance exists. Prerequisites: T003. (FR-001/004/009-011; C:II/III/VI)
- [x] T008 [P] Add a public-consumer test harness in `tests/contract/Kofte.Platform.ContractTests/Modules/Manifest/ConsumerHarness.cs` with bounded subprocess stdin/stdout/stderr, explicit context/culture, owned timeout/cleanup and fixed safe failure reporting. Support both library-only author process and host-facing public calls without duplicating rules, exposing host internals, persisting raw output or launching the host for pure checks. Prerequisites: T004. (FR-001/011-013; SC-003/005; C:III/VI/X/XI)
- [x] T009 Document the implementation gate/command and evidence matrix in `specs/002-platform-contracts/implementation.md`: new project selectors, public versus structural corpus, story checkpoints, automated guide SC-001 under the human-directed amendment, actual measured SC-004 and preserved bootstrap/CI requirements. Record the actual project/lock boundary review; do not label future commands or unimplemented test harnesses passed. Prerequisites: T006, T007, T008. (FR-013-015; SC-001-006; C:X/XI/XII)

**Checkpoint**: Reviewed project/dependency boundaries, safe result contract and
test harness foundations exist. This checkpoint is not a working manifest
validator. Both P1 value-rule stories must finish before US1 wires public
acceptance; no stub, permissive default or partial-success implementation is valid.

## Phase 3: User Story 2 - Keep Module Identity Stable and Unambiguous (Priority: P1)

**Goal**: Use one canonical immutable ID across metadata/releases and reject
aliases or implicit rename attempts without allocating registry/storage state.
**Independent Test**: Run the identity/update unit selector below; once the
public pipeline is wired, exercise the same cases through both consumers.
No running host, database or installed module is required.

### Required Validation

- [x] T010 [P] [US2] Add canonical identity cases in `tests/unit/Kofte.Platform.Tests/Modules/Manifest/ModuleIdTests.cs`: valid letters/digits/interior hyphens, 3+ labels, lengths 63/64 and total 253/254 using corpus recipes; reject uppercase, padded/embedded whitespace, Unicode/lookalikes, leading/trailing dots/hyphens, empty labels, paths and punctuation without normalization. Independently establish ordinal equality, not DNS/publisher ownership. Prerequisites: T009. (US2.2; FR-003/004; SC-002; C:II/III/VI/XI)
- [x] T011 [P] [US2] Add update/immutability cases in `tests/unit/Kofte.Platform.Tests/Modules/Manifest/IdentityUpdateTests.cs`: same ID with changed Unicode display/release succeeds, changed candidate ID rejects, invalid prior ID has its context diagnostic, and caller mutation cannot change previously accepted identity, declarations or diagnostics. Use observable behavior/collection mutation attempts, not setter-reflection snapshots; public-result cases may stay failing until US1 assembly. Prerequisites: T009. (US2.1/3; FR-004/010/011; SC-002/005; C:II/III/VI/XI)

### Implementation

- [x] T012 [US2] Implement canonical `src/Kofte.Platform/Modules/Manifest/ModuleId.cs` with the model constraint "At least three labels, each 1-63 characters; total at most 253; lowercase letters/digits/interior hyphens, no normalization. Immutable, ordinal identity." Use bounded scans, require letter/digit label endpoints and preserve exact accepted text; no trimming, case conversion, Unicode conversion, DNS or physical-name derivation. Prerequisites: T010. (US2.2; FR-003/004; C:II/III/VI)
- [x] T013 [US2] Implement explicit previous-ID validation/comparison in `src/Kofte.Platform/Modules/Manifest/IdentityValidation.cs` with `previous_id_invalid` at context `/previousModuleId` and `identity_changed` at manifest `/id`, only after applicable grammar checks. Compare ordinally, retain candidate identity across display/release changes and preserve the model boundary: "It is not an installed upgrade, version downgrade policy, migration, uniqueness reservation or author-ownership claim." Prerequisites: T011, T012. (US2.1/3; FR-003/004/010/011; C:II/III/VI)
- [x] T014 [US2] Complete defensive-copy/read-only behavior for the description/declarations and rejected diagnostics in `src/Kofte.Platform/Modules/Manifest/ModuleDescription.cs` and `ManifestValidationResult.cs`; prove no caller-owned array/list or later validation can mutate an earlier result. Do not add a public path to construct an accepted unvalidated identity. Prerequisites: T011, T012, T013. (US2.1; FR-004/010/011; C:II/III/VI)
- [x] T015 [US2] Add the public identity/update subset in `tests/contract/Kofte.Platform.ContractTests/Modules/Manifest/IdentityContractTests.cs` using independent C09-C12, identity-limit and mutation expectations through the common consumer harness. Cover malformed prior IDs, changed ID versus changed display/release and retained accepted results; these test-first public cases remain pending until T032 builds the complete pipeline, never a helper-only story pass. Prerequisites: T014. (US2.1-3; FR-003/004/013; SC-002; C:III/XI)

**Checkpoint**: Canonical identity and update-comparison components are
implemented and their test cases are prepared. Execution/acceptance requires
T032's complete build and public-pipeline/immutable-result evidence; no earlier
selector pass is assumed while other public types/components are unfinished.

## Phase 4: User Story 3 - Understand Compatibility and Capability Failures (Priority: P1)

**Goal**: Distinguish invalid versions/ranges/context from real incompatibility
and invalid declarations without installed-dependency or runtime claims.
**Independent Test**: Fixed SemVer/range/declaration expectations run without
host/storage; T032 later proves the same observable decisions publicly.

### Required Validation

- [x] T016 [P] [US3] Add `tests/unit/Kofte.Platform.Tests/Modules/Manifest/SemanticVersionTests.cs` and `VersionRangeTests.cs` with the full independent contract boundary table: SemVer precedence chain, huge bounded numeric components, prerelease numeric/alphanumeric ordering/leading zeros, build-insensitive equality, exact/two-bound inclusivity, adjacent/equal/reversed endpoints and same-core prerelease opt-in. Reject malformed versions, one-sided/shorthand/disjunction ranges, unsupported operators and extra whitespace; test version 128/129 and defensive range 512/513 limits without assuming every in-budget string is valid. Prerequisites: T015. (US3.1/2; FR-005/006/008; SC-002/006; C:II/III/XI)
- [x] T017 [P] [US3] Add `tests/unit/Kofte.Platform.Tests/Modules/Manifest/DeclarationTests.cs` for all nine kinds, supported `1.0.0+build` versus unsupported prerelease/`1.1.0`, duplicate kind despite different versions, invalid/duplicate/self/absent dependency cases, unique module-owned permission metadata and storage true/false versus physical selectors. Include explicit null/empty/invalid host context and invalid prior context without environment/product defaults; complete public assertions at T032. Prerequisites: T015. (US3.2-4; FR-005-010/012; SC-002/006; C:II/IV-VII/XI)

### Implementation

- [x] T018 [US3] Implement `src/Kofte.Platform/Modules/Manifest/SemanticVersion.cs` with the model constraint "Full SemVer 2.0.0; 128-character maximum; no numeric overflow. Build excluded from precedence. No public mutator." Preserve source/parsed core/prerelease/build data, compare numeric text by bounded length/digits and nonnumeric text ordinally, and never use `System.Version`, Int32/Int64 conversion or backtracking regex. Prerequisites: T016. (US3.1/2; FR-005/006/007; C:II/III/VI)
- [x] T019 [US3] Implement `src/Kofte.Platform/Modules/Manifest/VersionRange.cs` with the model constraint "One documented grammar, nonempty interval and explicit prerelease eligibility. No dependency resolver. Immutable." Accept only a full exact version or lower (`>`/`>=`) then upper (`<`/`<=`) comparator with exactly one ASCII space, no normalization and at most 512 ASCII characters; reject reversed/equal-exclusive intervals, admit equal-inclusive, apply same-core prerelease opt-in and ignore build metadata for membership. Prerequisites: T018. (US3.1/2; FR-005/006/008; C:II/III)
- [x] T020 [US3] Implement capability/dependency checks in `src/Kofte.Platform/Modules/Manifest/DeclarationValidation.cs`, carrying "One per supported kind; initial supported version precedence `1.0.0`, no prerelease. No implementation/handler/type name." and "Canonical target identity, unique per target, not self. Absence/cycles/install compatibility deliberately unassessed." Support exactly backend/frontend/resources/providers/events/workflows/jobs/permissions/mcp-tools, duplicate by kind/target ID independent of versions, canonical dependency IDs/ranges and retained absent dependencies; never resolve or activate them. Prerequisites: T017, T019. (US3.3/4; FR-005/007/008; C:II/III/VI/VII)
- [x] T021 [US3] Implement permission/storage metadata validation in `src/Kofte.Platform/Modules/Manifest/MetadataValidation.cs`, preserving "Local ASCII name unique within module; identity is tuple `(ModuleId,Name)`. Descriptive, not granted or bound to a team/resource." and "Both true/false valid; never a schema, path, credential, connection or cross-module request." Require local name length 1-64 and `[a-z][a-z0-9]*(?:[.-][a-z][a-z0-9]*)*`, description 1-256 Unicode scalars, not whitespace-only and no C0/C1 controls/unpaired surrogates; preserve ownership without inventing a global permission string or physical allocation. Prerequisites: T017, T019. (US3.4; FR-009/010/012; C:III-VI)
- [x] T022 [US3] Implement `src/Kofte.Platform/Modules/Manifest/CompatibilityValidation.cs` with explicit host/prior context checks and host membership only after earlier phases pass: null/empty/missing host is `host_context_required`, malformed host is `host_context_invalid`, malformed range is `range_invalid`, valid unsatisfied range is `host_incompatible`. Keep schema `1`, published host contract `1.0.0`, module release and capability versions distinct; compare other explicit valid host versions mathematically without certifying runtime compatibility. Prerequisites: T017, T019, T020, T021. (US3.1/2; FR-005/006/010/011; SC-006; C:II/III/VI)
- [x] T023 [US3] Add the public boundary/declaration subset in `tests/contract/Kofte.Platform.ContractTests/Modules/Manifest/CompatibilityContractTests.cs` using the shared independent range table and C13-C28/context variants through the common harness. Assert invalid syntax/context versus valid incompatibility, all kinds/versions, semantic duplicates and absent dependency preservation; keep execution pending until T032 rather than claiming an incomplete public assembly builds or defaults to compatibility. Prerequisites: T022. (US3.1-4; FR-005-009/013; SC-002/006; C:III/XI)

**Checkpoint**: Required identity/version/declaration components are ready for
the complete public pipeline. No host endpoint, plugin SDK, dependency resolver
or permission grant has been introduced. Public US3 evidence remains T032.

## Phase 5: User Story 1 - Describe a Module Without Guessing Host Rules (Priority: P1)

**Goal**: Validate a complete module declaration through the small public
contract; report usable shape/semantic/context failures and deterministic results.
**Independent Test**: A library-only author process and host-facing public
consumer validate minimal/full/raw/invalid fixtures with no host process, module
installation or database. Schema checks assert only shared structural decisions.

### Required Validation

- [x] T024 [P] [US1] Add `tests/unit/Kofte.Platform.Tests/Modules/Manifest/ManifestParsingTests.cs` for complete/minimal declarations, required fields and types at every closed object, unsupported schema stop, malformed/truncated/comment/trailing-comma/extra-root documents, repeated decoded keys at every object depth and original-byte encoding failures. Include escaped `id` aliases, BOM, malformed UTF-8 and unpaired surrogates; schema/version/unknown-field errors must not produce an accepted partial description. Prerequisites: T023. (US1.1-3; FR-001/002/010/012; SC-002; C:II/III/VI/XI)
- [x] T025 [P] [US1] Add `tests/contract/module-manifest.test.ts` using existing Ajv's Draft 2020-12 entry point offline to compile the exact spec schema and verify structural C01-C34 variants where applicable, both examples and independent length/enum/shape cases. Explicitly preserve schema-valid semantic rejections; do not compare Ajv internal diagnostics with public codes, parse raw duplicate fixtures into last-wins objects, fetch schemas or copy the production grammar into TypeScript. Prerequisites: T023. (US1.1-3; FR-001/002/013; SC-002/006; C:III/VI/XI)
- [x] T026 [P] [US1] Add `tests/contract/Kofte.Platform.ContractTests/Modules/Manifest/PublicConsumerTests.cs` for every C01-C34 variant and range boundary through both public consumers, all exposed descriptor fields, version domains, metadata ownership, original-byte failures and schema/semantic divergence. Assert exact decisions and ordered `(origin,path,code)` including fixed multi-error/phase-precedence cases, not expectations generated by library helpers. Prerequisites: T023. (US1.1-4; US2.1-3; US3.1-4; FR-001-014; SC-002/003/006; C:II/III/VI/XI)

### Implementation

- [x] T027 [US1] Implement `src/Kofte.Platform/Modules/Manifest/ManifestReader.cs` using bounded forward `Utf8JsonReader` scans, strict UTF-8/scalar checks and per-object decoded-key tracking before materializing descriptions: single object, no BOM/comments/trailing comma/extra root, case-sensitive known fields and all nine required declarations. Enforce byte 65,536 before decoding, depth 8 with root depth 1, collections 9/64/128 before materializing and first fatal token failure in input order; never reflect an unknown key in a diagnostic path or silently truncate input. Prerequisites: T024. (US1.2/3; FR-002/010/012; C:III/VI)
- [x] T028 [US1] Implement `src/Kofte.Platform/Modules/Manifest/ManifestValidator.cs` with public `Validate(ReadOnlySpan<byte> utf8Json, ManifestValidationContext context)` and the full read/shape/semantic/context/compatibility pipeline. Wire T012-T022 rules, schema-unsupported early stop, known-parent missing/unknown field locations, applicable-only checks and fixed sorted/truncated diagnostics; accepted output is constructed only after every phase succeeds. Require display name 1-128 Unicode scalars, not whitespace-only, no C0/C1 controls/unpaired surrogate; preserve Köfte and supplementary Unicode. Empty declaration arrays and explicit storage false are valid. Convert only known input/parser failures to safe rejection; unexpected defects propagate rather than become invalid-input camouflage or success. Prerequisites: T014, T022, T024, T026, T027. (US1.1-4; US2.1-3; US3.1-4; FR-001-012; C:I-III/VI)
- [x] T029 [US1] Implement `tests/contract/Kofte.Manifest.Consumer/Program.cs`: bounded raw stdin (at most 65,537 bytes), required `--host-contract` and optional `--previous-id`, no file-path argument or environment fallback. Reference only the public library; stdout is exactly accepted status or rejected status with safe ordered diagnostics, not descriptor/input echo. Exit 0 for accepted, 2 for rejected/invalid invocation and 1 for operational IO/internal failure; fixed safe failure codes only, no raw argument/exception/path, output files or broad catch-and-success fallback. Prerequisites: T028. (US1.1/2; FR-001/010-014; C:III/VI/X/XI)
- [x] T030 [US1] Complete the host-facing consumer and corpus integration in `tests/contract/Kofte.Platform.ContractTests/Modules/Manifest/PublicConsumerTests.cs` and `ConsumerHarness.cs`; prove its actual host project-reference direction, public-only library usage and equivalent decisions without starting a host, reaching into internals or implementing alternate validation. Ensure original-byte fixtures and exact schema/example copies stay in parity with the authoritative spec artifacts. Prerequisites: T025, T026, T029. (US1.1/4; FR-001/011/013/015; SC-002/003/006; C:II/III/XI)
- [x] T031 [US1] Add `tests/contract/Kofte.Platform.ContractTests/Modules/Manifest/DeterminismTests.cs` and harness culture support to execute the complete corpus 100 times per consumer under each real `en-US`/`tr-TR` culture, explicitly configuring and proving the culture inside the author process and host-facing consumer. Assert every decision and ordered diagnostic identity, reordered JSON object properties, numeric collection ordering and no invariant-globalization substitute; process count is not the metric. Prerequisites: T030. (US1.4; FR-010/011/013; SC-003; C:III/VI/XI)
- [x] T032 [US1] Build the solution and run the exact focused unit/contract/schema commands from `specs/002-platform-contracts/quickstart.md`, including nonzero minimum expected .NET tests; record actual commands/results, counts, cultures and all US1.1-4, US2.1-3 and US3.1-4 acceptance mappings in `specs/002-platform-contracts/implementation.md`. Exercise all immutable/update/context/declaration tests now publicly; do not call helper-only, schema-only or fixture-definition evidence complete acceptance. Prerequisites: T031. (FR-001-014; SC-002/003/006; C:X/XI/XII)

**Checkpoint**: The first useful author-facing increment is public and complete
for US1-3, including valid full metadata and incompatible/invalid inputs.
It remains unaccepted and incomplete as a feature: US4's exact-limit, safe-output,
no-side-effect and retry observations plus guide/timing and full bootstrap/CI
gates are still mandatory.

## Phase 6: User Story 4 - Correct Rejected Input Without Unsafe Side Effects (Priority: P2)

**Goal**: Return bounded safe corrective information and retry without disclosure,
execution, external access, allocation or contamination of earlier results.
**Independent Test**: Exact/one-over, canary, observer and rejection-then-valid
fixtures run against the actual library and author process, with owned resources
and source dependency review rather than a schema-only no-effects claim.

### Required Validation

- [x] T033 [P] [US4] Add `tests/unit/Kofte.Platform.Tests/Modules/Manifest/ManifestBudgetTests.cs` using independent recipes: bytes 65,536/65,537 including multibyte text, depth 8/9 (shape rejection versus earlier depth failure), capabilities 9/10, dependencies 64/65, permissions 128/129, display 128/129 and description 256/257 scalars, permission-name 64/65, version 128/129 and range 512/513 ASCII. Test all corresponding codes/known locations, exact 32/33 diagnostic behavior and simultaneous fatal/shape/semantic/compatibility precedence; no limit may truncate declarations into acceptance. Prerequisites: T032. (US4.2; FR-002/009-012; SC-002/004/005; C:III/VI/XI)
- [x] T034 [P] [US4] Add `tests/contract/Kofte.Platform.ContractTests/Modules/Manifest/SafeOutputTests.cs` with unpredictable transient secret/control markers in known fields, unknown keys, versions/context and invalid invocation; capture library/author stdout/stderr, require zero raw markers/injected lines/manifest/stack/filesystem/argument echoes and bounded fixed safe messages/paths. Exercise actual malformed invocation, bounded-stdin and operational IO failure exits, not just a successful process; do not persist malicious raw fixtures/output in reports. Prerequisites: T032. (US4.1/2; FR-010-014; SC-005; C:III/VI/XI)
- [x] T035 [P] [US4] Add `tests/contract/Kofte.Platform.ContractTests/Modules/Manifest/SideEffectTests.cs` with owned local request/process/filesystem observers and isolated non-writable module data: submit `$ref`/URL, hooks/type-loading, physical schema/path/credential selectors and malformed inputs, and observe zero outbound requests/module activations/registry writes/module files or schemas/host-setting changes. Review actual library dependencies/source for DNS, reflection loading, IO and telemetry; do not infer isolation from an unknown-field rejection or present trusted plugins as sandboxed. Bound and clean up only owned observers/processes. Prerequisites: T032. (US4.2; FR-009/012/015; SC-005; C:II-VI/X/XI)
- [x] T036 [P] [US4] Add `tests/contract/Kofte.Platform.ContractTests/Modules/Manifest/RetryTests.cs`: follow every rejection class with unchanged valid M under the same explicit context, retain earlier accepted descriptions, and prove no reset/reinstall/cache/registry cleanup or mutation is needed. Include separate consumer invocations and repeat the fixed simultaneous-error cases; process isolation alone must not mask state retained by repeated in-process calls. Prerequisites: T032. (US4.3; FR-004/010-012; SC-005; C:III/VI/XI)

### Implementation

- [x] T037 [US4] Close any bounded-read/diagnostic defect exposed by T033 in `src/Kofte.Platform/Modules/Manifest/ManifestReader.cs`, `ManifestValidator.cs` and `ManifestDiagnostic.cs`, retaining the exact contract budgets, first-fatal/phase ordering and deterministic truncation. If these tests already pass, record that evidence without gratuitous code changes. Unexpected internal defects must not be swallowed as ordinary parse rejection. Prerequisites: T033. (US4.2; FR-002/010-012; C:III/VI)
- [x] T038 [US4] Close any output/side-effect/retry defect exposed by T034-T036 in `src/Kofte.Platform/Modules/Manifest/` and `tests/contract/Kofte.Manifest.Consumer/Program.cs`, with focused regression evidence and the existing fixed error paths. Keep library IO/telemetry absent, caller failures explicit, accepted state immutable and adapter operational failures nonzero; do not add a logger/framework, broad success fallback, remote fetch or data-reset remedy. If no defect exists, retain code and record the observed evidence. Prerequisites: T034, T035, T036, T037. (US4.1-3; FR-004/009-012/015; C:II-VI)
- [x] T039 [US4] Add `tests/contract/Kofte.Platform.ContractTests/Modules/Manifest/ManifestPerformanceTests.cs` for 100 consecutive representative initialized valid/invalid/over-budget calls, including byte/depth failures, using monotonic per-call timing and independent one-second assertions. Record only safe case identifiers/counts/durations and actual revision/SDK/runtime/OS/architecture/CPU/memory metadata; warm initialization is excluded, not cold author-process startup. Do not substitute p95 or total average for individual checks. Per the human-directed ADR amendment, no fixed resource profile or matching benchmark machine is required. Prerequisites: T038. (US4.2; FR-002/012; SC-004; C:VI/X/XI)
- [x] T040 [US4] Execute the complete focused platform unit/contract/schema tests, offline consumer calls and observed privacy/side-effect/retry/performance fixtures; map all US4.1-3 and SC-004/005 evidence in `specs/002-platform-contracts/implementation.md`. Retain actual available-machine timing/environment evidence for T044; record observer/source-review method and zero observed effects without claiming arbitrary hostile-code isolation. Prerequisites: T039. (FR-002/009-014; SC-002-005; C:II/VI/X/XI/XII)

**Checkpoint**: Every introduced failure category has bounded public behavior
and a clean correction path. SC-001 requires real automated guide execution and
SC-004 requires actual measured timing; checked source files alone do not supply
either, and neither approves the numbered feature.

## Final Phase: Cross-Cutting Evidence and Documentation

- [x] T041 Add focused fail-closed inventory/runner regressions in `scripts/tests/test_verify.py` for the new required platform unit project, public .NET contract project, author consumer and structural test file: deleting/skipping a required target, running zero tests, failing a consumer or timing out must fail its named suite and preserve safe reports/child exit status. Retain all existing required scripts/files, both-provider fixtures and deliberate failure checks; do not broaden unrelated verifier behavior. Prerequisites: T040. (FR-013/015; SC-006; C:III/VI/X/XI)
- [x] T042 Wire new commands and required files/projects into `scripts/verify.py` unit/contract/quality paths and the existing `.github/workflows/product-checks.yml` jobs as needed; build the author process before use, run platform tests with minimum expected counts, include the schema file in the existing contract runner and retain the real SQLite/PostgreSQL host/adapter checks. Execute T041 and actual named suites; missing projects/scripts, zero tests, unavailable prerequisites and child failures must not be optional `--if-present` success or dropped from the report. No separate CI-only implementation. Prerequisites: T041. (FR-001/013/015; SC-002/003/005/006; C:III/X/XI)
- [x] T043 [P] Turn `specs/002-platform-contracts/quickstart.md` into an accurate executable author guide, and update `README.md`, `CONTRIBUTING.md`, `docs/README.md`, `docs/architecture.md`, `docs/development/local-development.md` and affected dependency/contract docs to match actual behavior. Execute the documented minimal/compatibility/prior-ID/identity-error/corrected-retry commands and exit/output expectations; explain schema versus complete validation, supported version combinations, strict range grammar, breaking-change review/source rollback, non-authoritative permission/storage metadata and trusted/not-installed/not-sandboxed boundaries. Do not rewrite earlier historical ledger evidence. Prerequisites: T042. (FR-001/003-009/013-015; SC-001/006; C:I-X/XII)
- [x] T044 [P] Record actual SC-004 timing in `specs/002-platform-contracts/implementation.md` on an available supported developer workstation or CI runner with pinned SDK/runtime: initialize first, then measure every one of 100 representative checks including oversized/deep failure below one second. Retain safe per-case durations and actual source revision/SDK/runtime/OS/architecture/CPU/memory. Following the explicit human-directed ADR 0004 amendment, no exact Ubuntu/CPU/RAM profile, dedicated hardware, VM sizing or matching cross-machine benchmark is required. Prerequisites: T042. (FR-002/012; SC-004; C:X/XI/XII)
- [x] T045 Execute the documented author-guide example, identity and compatibility failures and corrected retries through the actual public author consumer, and record commands, safe outputs and exit results in `specs/002-platform-contracts/implementation.md`. Under the explicit human-directed ADR 0004 author-guide amendment, automation supplies SC-001; no unfamiliar-human exercise, ten-minute target or elapsed-time attestation is required or claimed. Keep host edits/external service contact absent and retain public-consumer/retry tests. This proof is not feature acceptance. Prerequisites: T043. (FR-014; SC-001; C:XI/XII)
- [x] T046 Execute the final documented gate, including `python3 scripts/check_repository.py`, `python3 -m unittest discover -s scripts/tests -v`, `git diff --check`, `python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10`, and inspect actual assessed-head Ubuntu CI/check reports. Record commands/results/revision/counts in `specs/002-platform-contracts/implementation.md`; include locked restore, .NET format/build, existing web lint/type/build/browser, new unit/schema/public contracts, actual SQLite/PostgreSQL health/persistence/recovery, privacy/startup/failure propagation and all new mandatory named steps. For a docs/state-only amendment, retain prior complete product-gate evidence only after proving its production/test/tooling/dependency inputs unchanged; rerun repository checks and inspect publication-head CI. Unavailable infrastructure or failed/skipped/empty checks block, not pass; protect only owned fixtures/processes. Prerequisites: T044, T045. (FR-001-015; SC-002-006; C:III/VI/IX-XII)
- [x] T047 Reconcile `specs/002-platform-contracts/implementation.md` against source/tests and all 15 FRs, six SCs, 14 acceptance scenarios and twelve principles using the coverage tables below; distinguish actual automated guide/provider/CI results and available-machine timing, including limitations and the explicit absence of human-usability/time claims. Verify the public library has no host/ASP.NET/EF/IO dependency, the author consumer has no host/internal dependency, module-free startup/routes/stores are preserved and no later-spec runtime slipped in. Do not check a task whose required proof is absent. Prerequisites: T046. (FR-001-015; SC-001-006; C:I-XII)
- [x] T048 Complete only the active implement stage through the driver with `SPECIFY_FEATURE_DIRECTORY=specs/002-platform-contracts`, include `tasks.md`, `implementation.md`, docs and `.specify/drive-state.json` in the same reviewed commit, push immediately and update the existing draft PR #5 using its template/exact evidence. State converge is next but do not execute it, final review or acceptance; do not mark ready/request merge/merge/close/auto-merge/delete the branch. If a required gate is unavailable, block the active stage and publish safe partial evidence without falsely completing this task. Prerequisites: T047. (FR-014/015; C:XI/XII)

## Dependencies and Execution Order

```text
T001 -> T002 -> T003
T003 -> T004 -> T008
T003 -> T005 -> T006
T003 -> T007
T006 + T007 + T008 -> T009
T009 -> US2 T010-T015
US2 -> US3 T016-T023
US3 -> US1 T024-T032
US1 -> US4 T033-T040
US4 -> T041 -> T042
T042 -> T043 -> T045
T042 -> T044
T044 + T045 -> T046 -> T047 -> T048
```

Each task's direct prerequisites are authoritative; the phase ranges summarize
their internal graph. All referenced IDs are earlier, and there are no cycles.
P1 dependency order is deliberate: foundational identity/range/declaration rules
must exist before US1 publishes complete acceptance. No story needs real
storage, an activated module, HTTP, UI or MCP to run its focused public tests.
US4 consumes the complete pipeline; the final existing-foundation gate still
uses real provider/browser fixtures rather than mocks.

### Parallel examples by story

| Ready boundary | Non-overlapping work permitted |
| --- | --- |
| After T003 | T004 corpus, T005 result tests and T007 description/context types; T008 follows T004 and T006 follows T005 |
| US2 after T009 | T010 identity grammar tests and T011 update/immutability tests |
| US3 after T015 | T016 SemVer/range tests and T017 declaration/context tests |
| US3 after T017/T019 | T020 capability/dependency validation and T021 permission/storage validation, different files |
| US1 after T023 | T024 parser tests, T025 Ajv tests and T026 public-consumer cases |
| US4 after T032 | T033 budgets, T034 output, T035 side effects and T036 retry fixtures |
| After T042 | T043 guide/docs and T044 available-machine measurement; T045 needs the finished guide |

There are 17 `[P]` markers. Lack of a marker does not waive an actual prerequisite;
do not concurrently modify shared model, harness, report or verifier files.
Serialize evidence-report writes even when test execution is independent.

### Incremental delivery

1. Resolve analysis/adoption gates and implement the minimal shared boundaries.
2. Complete US2/US3 components, then US1's fully validated author-facing path.
   Run its public/structural/identity/declaration checkpoint, not just happy-path M.
3. Complete US4's negative, side-effect, retry and timing evidence; preserve
   all existing host/web/provider behavior and fail-closed verification.
4. Obtain actual automated guide/timing proof and the full assessed-source local/CI gate,
   then hand off only implementation. Converge/review/accept remain later stages.

US1 is an internal first deliverable after its P1 dependencies, never permission
to omit US2-4 or call this the MVP. MVP remains exactly inventory 001-024.

## Constitutional Coverage and Scope

### Requirements and acceptance

| Requirement | Implementing/validating tasks | Observable acceptance |
| --- | --- | --- |
| FR-001 public contract/schema/declarations | T002-T009, T024-T032, T043 | US1.1-4; minimal/full descriptor, independent consumers/schema |
| FR-002 strict parse/budgets | T004-T006, T024-T028, T033/T037/T039/T040 | US1.2/3, US4.2; raw-byte, phase and exact-limit failures |
| FR-003 canonical identity | T010/T012/T015/T024/T028/T032 | US2.2; no aliases, exact length/scalar distinction |
| FR-004 immutable identity/update | T007/T011-T015/T028/T032/T036 | US2.1/3, US4.3; explicit prior identity and immutable graph |
| FR-005 version domains/syntax | T016-T023/T026/T028/T032/T043 | US3.1/2; invalid syntax versus valid explicit versions |
| FR-006 explicit host compatibility | T016-T019/T022/T023/T026/T028-T032 | US3.1/2; entire range/prerelease/build boundary matrix |
| FR-007 capability kinds/version/uniqueness | T017/T020/T026/T028/T032 | US3.3; nine kinds, version support and duplicate identity |
| FR-008 declared dependencies | T016-T020/T026/T028/T032 | US3.4; malformed/duplicate/self reject, absent retained |
| FR-009 permission/storage metadata | T007/T017/T021/T024/T028/T033-T035 | US1.1, US3.4, US4.2; descriptive ownership/no physical selectors |
| FR-010 complete result/safe errors | T005-T008/T024-T040 | US1.2-4, US4.1-3; disjoint outputs and fixed ordered identities |
| FR-011 deterministic independent retry | T008/T011/T014/T026/T030-T032/T036/T040 | US1.4, US2.1, US4.3; repeated cultures/consumers and retained results |
| FR-012 bounded no-effects/privacy | T006/T008/T024/T027-T029/T033-T040 | US4.1-3; captures/observers/source review, not schema inference |
| FR-013 schema/public corpus parity | T004/T008/T025/T026/T030-T032/T041-T043 | US1.1/4, US3.1-4; original-byte and schema-valid semantic cases |
| FR-014 author/compatibility/rollback docs | T009/T029/T032/T040/T043/T045/T047/T048 | Executed guide/expected failures/corrected retries and trust distinction, no human-study claim |
| FR-015 unchanged bootstrap/adoption | T001-T003/T035/T041-T048 | Real providers, health/startup/UX, current-head CI and no new authority |

| Success criterion | Required tasks/evidence |
| --- | --- |
| SC-001 | T043/T045; actual automated guide/example/error/corrected-retry execution, safe output/exit codes, no host edits/external services; no human exercise or elapsed-time attestation |
| SC-002 | T004/T010-T032/T033/T040/T042/T046; independently expected corpus with zero false acceptance |
| SC-003 | T008/T026/T030-T032/T042/T046; 100 corpus runs per consumer per real locale |
| SC-004 | T033/T037/T039/T040/T044/T046; each initialized call below one second on an available supported machine with actual measured environment metadata, no fixed hardware profile |
| SC-005 | T011/T014/T033-T040/T042/T046; zero seeded disclosures/effects and every rejection's corrected retry |
| SC-006 | T016-T032/T041-T043/T046; complete public compatibility boundaries and unchanged real-provider foundation |

T032 must explicitly cover **US1.1-4, US2.1-3 and US3.1-4** (11 scenarios);
T040 covers **US4.1-3** (three scenarios). T047 reconciles all 14, without
substituting requirement counts for actual behavior evidence.

### Principles and inapplicability

| Principles | Tasks and boundary |
| --- | --- |
| I, XII | T001/T009/T043/T047/T048 preserve Köfte branding, 002-only intent, one feature branch/draft PR, later acceptance and exact MVP boundary |
| II, III | T002-T032/T047 establish minimal shared public contracts, identity/versioning, native calls, dedicated SDK-included tests and reviewed locks; no speculative runtime/mediator |
| IV, V | T017/T021/T028/T035/T043/T046/T047 prove metadata has no allocation/access/transaction authority; later registry/lifecycle obligations are retained |
| VI | T005/T006/T008/T024-T040/T041/T042 enforce explicit context, safe failure, bounded offline data handling and observer evidence without granting permissions |
| VII | T017/T020/T028/T043/T047 declare `mcp-tools` only; shared protected-operation authorization and actual MCP remain 007/013 |
| VIII | T017/T020/T028/T043/T047 declare resources/providers/events/workflows/jobs, not state machines, delivery/execution or SignalR |
| IX | T028/T043/T046 preserve Unicode author metadata and current browser/UX behavior; no new page, extension loader or UI states |
| X, XI | T003-T048 provide offline portability, focused negative/contracts, actual both-provider bootstrap regression, fail-closed local/CI, author and measured timing proof |

There is no new database/migration here: new PostgreSQL schema allocation,
63-byte/reserved/UNIQUE/concurrency protections, schema-local history and immutable
mapping, SQLite separate module/platform files/restricted canonical paths and
traversal protection belong to 003/004. No cross-module tables/shared connections,
ACID or SQLite `ATTACH` contract is allowed. T046 retains real bootstrap-provider
proof; it does not pretend to establish future module isolation.

There is no new identity/team/resource operation, REST/UI/MCP route, tool,
audit store, provider call, event/job runtime or frontend extension. Therefore
new cross-team denied-tool/parity tests, job restart/cancellation, resource/event
contracts, UI-state/accessibility journeys, module migration/recovery and
Kubernetes/full reference-module smoke are inapplicable to this parser.
Their later numbered specs still owe them; existing bootstrap browser, recovery
and provider checks remain mandatory. A valid manifest neither authorizes nor
trusts/installs/enables code, resolves dependencies, proves publisher identity or
sandboxes trusted in-process/frontend contributions. No Azure, Hoard, broker,
OpenTofu or other cloud/IaC dependency is introduced.

## Completion and Human Handoff

Every completed implementation task needs its actual source/test/command evidence
in `implementation.md`. Unavailable required guide, timing, tool, provider or CI proof
is a specific blocker, never an assumed pass. Reviewed quality checkboxes,
Accepted ADR 0004 and this generated list do not prove runtime behavior.

This invocation generates **tasks only**. The next invocation performs read-only
**analyze**; do not create its report or execute implementation here.
During the later implement stage, T048 records completion with all task evidence
before that final checkbox and ledger transition are published together.
Converge, final review and explicit feature acceptance are separate later stages.
Passing tests, checked tasks, author attestations, autopilot or a prior merge
never constitute numbered-feature acceptance.

Keep `feature/002-platform-contracts` and draft PR #5 through those stages.
Commit artifacts/ledger together, push every commit immediately and update that
PR, without an intermediate merge request. Only passing final review and an
explicit affirmative human acceptance decision permit mark-ready and a clickable
human merge request. Never merge, close, enable auto-merge or delete the branch.

## Phase 7: Convergence

The first convergence assessment found one partial acceptance-evidence gap.
The original 48 task markers and implementation checkpoint above are historical;
they do not discharge this newly identified work. See [convergence.md](convergence.md).

- [ ] T049 Complete the independently expected public corpus in `tests/contract/Kofte.Platform.ContractTests/Modules/Manifest/ManifestCorpus.cs` and its harness/tests with the omitted recipes from `specs/002-platform-contracts/contracts/acceptance-corpus.md`: exact/one-over byte and depth limits, dependency/permission counts, display/permission/version/range lengths, the documented reversed range `>=2.0.0 <1.0.0`, and collected-error/32-versus-33-diagnostic ordering/truncation cases. Reuse the existing recipe expectations, preserve original bytes and distinguish shape/semantic rejection from budget failure; do not derive expectations from production helpers or change contract limits. Extend expected-result representation as needed for multiple ordered diagnostics. Execute every documented corpus case 100 times through each public consumer in both real `en-US` and `tr-TR` cultures, retaining corrected retry and exact ordered identities; helper-only unit tests, structural schema tests and one-off public checks do not replace this evidence. Update `specs/002-platform-contracts/implementation.md` and affected progress docs with exact commands/results and coverage, run the required repository and focused named unit/contract gates, and reconcile unchanged broader gate inputs/current-head CI before completing implement. No product-code change is required unless a new case exposes a defect. Prerequisites: T004, T023, T026, T031, T033, T040, T046, T047. HIGH; per SC-003/006, FR-013 and plan: complete independent public corpus (partial; F1).
