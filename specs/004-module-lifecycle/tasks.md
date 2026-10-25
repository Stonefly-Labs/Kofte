# Tasks: 004 - Module Composition and Lifecycle

**Input**: Reviewed design in `specs/004-module-lifecycle/`: specification,
plan, research, data model, composition/operator v1 contracts, validation guide
and 16/16 author plus 36/36 reviewer-authorized quality criteria.
**Prerequisites**: Accepted/merged 001-003; ADR 0006 is already **Accepted for
implementation** under the human's exact **"yeah approved"** decision.
Passing read-only **analyze remains required before implementation**; generation
neither runs it nor requests repeated ADR/checklist approval.
**Constitution**: Adopted v2.0.1, principles I-XII.
**Status**: T082-T083 implementation complete: all 83 tasks evidenced; follow-up converge is next.
The execution evidence is in `implementation.md`. Task completion is not
feature acceptance or MVP completion.

**Tests**: Spec-required and constitutional tests below are mandatory. Write
focused behavioral cases before dependent implementation; establish meaningful
failure once necessary signatures/fixtures compile, not merely missing-type
errors. Real provider/process/public-consumer assertions cannot be replaced by
mock success, positive counts or historical 001-003 results.
**Organization**: Five setup, nine foundational, twelve US2, ten US1, thirteen
US3, eight US4, fifteen US5 and eight cross-cutting tasks. US2 precedes US1
within their shared P1 priority because safe composition validation is a real
startup prerequisite; no permissive temporary resolver is introduced.

## Format and Paths

- Every task has an unchecked marker, sequential ID, exact paths, explicit
  `Depends:` and requirement/scenario/principle traceability.
- `[P]` permits different-file work only after all stated prerequisites pass;
  it never permits running another Spec Kit stage or skipping a safety gate.
- Proposed paths are implementation targets, not files created by generation.
  Reuse existing helpers before adding files; do not scaffold future modules.
- All .NET test/fixture/example source, contexts and migrations belong inside
  dedicated test projects under `tests/`, with ordinary SDK inclusion. Never
  link production slices as tests or reference a test project from production.
- `composition-v1.md` and `operator-v1.md` below mean the contracts in this
  feature's `contracts/`. Quoted entity constraints come verbatim from
  `data-model.md`; contract tables provide their complete numeric bounds.
- All implementation tasks inherit the passing-analysis entry gate. `none`
  means no other task prerequisite, not permission to bypass that gate.

## Phase 1: Setup and Shared Prerequisites

- [X] T001 Verify the completed 004 requirements-quality gate and Accepted ADR 0006 against `specs/004-module-lifecycle/checklists/quality.md`, `docs/adr/0006-module-composition-lifecycle.md` and the later passing `specs/004-module-lifecycle/analysis.md`; open `specs/004-module-lifecycle/implementation.md` with assessed revision/environment, existing approval and acceptance-evidence slots. Block on an actual unresolved analysis/design issue; do not invent approval or ask to approve the same decision again. Depends: none. Trace: FR-001-020; principles XI, XII/governance.
- [X] T002 Review the actual proposed `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.13 edge from `src/Kofte.Platform/Kofte.Platform.csproj` before adoption; record exact direct/transitive identities, compatibility, licenses, provenance/advisories and notice effects in `docs/development/dependencies.md` and `docs/development/third-party-notices.md`. Reuse existing pins; do not add a loader, mediator, mapper or new package version. Depends: T001. Trace: FR-019/020; principles II, III, VI, X.
- [X] T003 Add only the justified DI edge and separately compiled `tests/contract/Kofte.Composition.TestModule/Kofte.Composition.TestModule.csproj` plus `tests/contract/Kofte.Composition.Consumer/Kofte.Composition.Consumer.csproj`; update `Kofte.slnx`, affected test project references and `packages.lock.json` files, retaining nullable/warnings-as-errors, exact central versions and locked restore. The consumer may reference public host/platform/infrastructure contracts; `src/Kofte.Host/Kofte.Host.csproj` must never reference either test assembly. Depends: T002. Trace: FR-019/020; principles II, III, X, XI.
- [X] T004 [P] Extend owned test resources in `tests/integration/Kofte.TestSupport/CompositionProcess.cs` and `tests/integration/Kofte.TestSupport/CompositionFixture.cs`, reusing `OwnedProcesses.cs`, `OwnedTestRoot.cs`, `StorageProviderScope.cs` and `PostgresResources.cs`; own private selection files, exact executable/PIDs/ports/provider targets, tracked fixture descendants, canaries and cleanup. Preserve original resources and fail explicitly for unavailable tools/providers or unverified exit. Depends: T003. Trace: FR-015/016/020, SC-003/004/006; principles IV, VI, X, XI.
- [X] T005 [P] Scaffold the fail-closed public scenario/result harness in `tests/contract/Kofte.Composition.Consumer/Program.cs`, `tests/integration/Kofte.TestSupport/CompositionScenarioCatalog.cs` and `tests/contract/Kofte.Platform.ContractTests/Composition/CompositionConsumerHarness.cs`; admit only the eleven guide scenarios and fixed providers plus `all`, require each named semantic observation, exact expected case set and positive assertions, and reject missing/decremented evidence, malformed output, unexpected stderr, unknown input and nonzero exits. No caller-selected path/SQL/type/credential arguments or internal-access grants. Depends: T003. Trace: FR-019/020, SC-001/005/006; principles II, VI, XI.

## Phase 2: Foundational Boundaries

- [X] T006 Define version-1 copied public deployment, selection, claim and disjoint resolution types in `src/Kofte.Platform/Modules/Composition/`; enforce `ModuleDeployment`: "Contract 1; copied bounded manifest bytes; immutable contribution claims/configuration field definitions; pure settings validator; native DI registrar; runtime factory. Canonical identity exists only after accepted 002 validation. No physical locator or install URL." Enforce `ResolvedComposition`: "Contract 1, copied canonical deployed/enabled/disabled identities, dependency edges and stable topological order; immutable contribution reservation catalog; snapshot fingerprint internal only. Or disjoint rejection diagnostics, never a partial accepted set." Return no settings, binding or minted manifest authority; constructing deployments invokes no author code. Depends: T003. Trace: FR-002-006/019; principles II, III, VI.
- [X] T007 Define version-1 configuration, registrar/runtime/context/cleanup and observation contracts in `src/Kofte.Platform/Modules/Lifecycle/`; enforce `ModuleConfiguration`: "Copied, bounded JSON values accessed only within that owner's settings; typed access/field tokens, no ambient environment, other owners or secret service. Author rules declare required/types/allowed fields and a pure cross-field validator." Define cancellation-aware Initialize/Start/CheckHealth/Stop, host-issued owner contexts, explicit versioned prerequisite bindings, no initialize-store access and no irreversible lifecycle business effects. All public collections copy storage; no module registry/maintenance/global resolver service is exposed. Depends: T006. Trace: FR-007-009/014/015/019; principles II-VII.
- [X] T008 [P] Add strict selection/private-file regressions in `tests/unit/Kofte.Host.Tests/Features/ModuleComposition/ModuleSelectionTests.cs`: unset versus empty/missing variable, invalid UTF-8/BOM/comments/depth, duplicate decoded properties/IDs, unknown envelope/entry keys, nonboolean/null/default coercion, nondeployed selection, unsafe owner/directory, symlink/hard link and bounded original settings bytes. Prove one immutable snapshot, no settings echo and disabled envelope-only validation. Depends: T007. Trace: FR-002/005/007/012/018, US1.3/4, US2.4; principles VI, X, XI.
- [X] T009 Implement bounded `KOFTE_MODULES_FILE` loading in `src/Kofte.Host/Features/ModuleComposition/ModuleSelectionReader.cs`, reusing existing Unix path/ownership helpers without weakening them; enforce `ModuleSelectionDocument`: 'Exact `schemaVersion: "1"` and closed `modules` array. Entry: canonical `id`, explicit boolean `enabled`, closed module-owned `settings` object. No duplicate IDs/properties, unknown top-level fields, comments, BOM or implicit coercion.' Apply 1,048,576-byte/32-entry/depth-8 file and 16,384-original-byte settings caps before materialization; no live reload, ambient settings fallback, selected-file fallback or public settings hash. Depends: T008. Trace: FR-002/004/005/007/012/018; principles II, VI, X.
- [X] T010 [P] Add fixed-code diagnostics/record/protocol tests in `tests/unit/Kofte.Host.Tests/Features/ModuleComposition/CompositionDiagnosticTests.cs`; cover identity-less input/admission/acquisition/projection refusals, one terminal boundary record, unknown enums/versions/owners/attempts, 4,096-byte frames, 256 records plus terminal reserve, 64 steady-state records/minute, raw-output draining and required-output failure. Capture Activity/Meter listeners and forbid settings/path/raw exception/high-cardinality labels rather than just checking serialization constants. Depends: T007. Trace: FR-017/018, SC-006; principles VI, X, XI.
- [X] T011 Implement safe boundary/transition records and bounded private event framing in `src/Kofte.Host/Features/ModuleComposition/CompositionDiagnostics.cs` and `CompositionProtocol.cs`; use generated 32-lowercase-hex attempts, fixed host codes/messages/guidance, current opaque private snapshot/admission tokens, allowlisted child identities and bounded buffers. Project no raw stdout/stderr; required output/limit failures terminate with failure. Metric dimensions are only mode/stage/provider/outcome; link recognized 003 correlation/durability truthfully. Depends: T010. Trace: FR-017/018, SC-006; principles VI, X.
- [X] T012 Add behavior tests in `tests/unit/Kofte.Host.Tests/Features/ModuleComposition/CompositionBudgetTests.cs` and implement attempt/budget primitives in `src/Kofte.Host/Features/ModuleComposition/CompositionAttempt.cs`; enforce "Contract 1; fresh 32-lowercase-hex trace ID; mode; monotonic deadline; phase/outcome; entered/completed module-stage sets; per-owner DI scopes/cleanup registrations; publication latch; safe failure evidence." Prove nested clipping and included reserves for validate 45=35+10 seconds, prepare 120=105+15, startup 60=45+15, resolver 2, validator 0.25, registrar 1, Initialize/Start 2 each, health 1/sweep 20, and stop/dispose 15=10+5 with each Stop/cleanup at most 1 second. Preserve 003 2/3/30/120 caps and whole-stack 285/300 envelope; do not equate timed-out waiting with termination. Depends: T007, T011. Trace: FR-008/015/016, SC-004; principles V, X, XI.
- [X] T013 [P] Add real-provider mutation-free inventory/recipe regressions in `tests/integration/Kofte.Storage.IntegrationTests/Composition/CompositionPreflightTests.cs`; distinguish genuinely unregistered owners from corrupt/missing/foreign persisted mappings, incomplete intents, changed declarations/no-storage-to-relational, wrong owner/provider/target/model/history, invalid later recipe and unowned physical objects. Compare exact full inventory/revisions/data before/after refusal and prove no earlier owner allocation or recipe execution; retain actual database/physical ownership checks. Depends: T004, T007. Trace: FR-005/009/010/013, US2.4, US3.1/3; principles IV-VI, XI.
- [X] T014 Implement infrastructure-only read inspection and recipes in `src/Kofte.Infrastructure/Composition/CompositionStorageInspector.cs` and `ModuleStorageRecipe.cs`, extracting/reusing accepted registry/model/operation/physical verification helpers rather than copying or catching every `ResolveAsync` error as absence. Enforce "Canonical owner; provider; typed assigned context factory; known migration assembly/target; no arbitrary SQL/connection/schema/file. Exactly one supported recipe per supported provider for a relational module; none for no-storage." Read complete inventory under authority; a new-owner structural candidate is not a usable assignment. Never mutate, repair, upgrade, mint access or put EF types in neutral results; repeat actual checks at use. Depends: T013. Trace: FR-005/009/010/013/019; principles III-VI.

**Checkpoint**: Reviewed dependency locks and discoverable owned harnesses,
immutable contracts/configuration, safe output/attempt budgets and read-only
real-provider inspection exist. No permissive resolver, serving readiness,
module-store allocation or future capability runtime is justified by setup.

## Phase 3: User Story 2 - Reject an invalid composition before activation (Priority: P1)

**Goal**: Reject ambiguous/invalid whole compositions before any mutating stage.
**Independent Test**: Fixed externally expected valid/invalid graphs and strict
settings/claim/storage fixtures produce exact orders or ordered refusals with
zero registrar/factory/Initialize/provision/migrate/Start/publication effects.

### Required Validation

- [X] T015 [P] [US2] Freeze independent expected graph/diagnostic fixtures in `tests/contract/Kofte.Platform.ContractTests/Composition/CompositionCorpus.cs` and add `tests/unit/Kofte.Platform.Tests/Modules/Composition/CompositionResolverTests.cs`; include empty/chain/diamond/disconnected roots, invalid disabled manifests, duplicate/malformed/unsupported/incompatible manifests, missing/disabled/incompatible/prerelease/build dependencies, self-dependency as accepted 002 `dependency_self`, multiple SCCs and corrected retries. Expected results must not be generated by the resolver; use real en-US/tr-TR cultures. Depends: T007. Trace: FR-002-005/019, US2.1-3, SC-002; principles II, XI.
- [X] T016 [P] [US2] Add admission/copy/multi-diagnostic regressions in `tests/unit/Kofte.Platform.Tests/Modules/Composition/CompositionLimitTests.cs`: all composition-v1 inclusive bounds and one-over cases, bounded 33rd/17th enumeration, accepted 002 manifest grammar limits, 1,024 edges, eight-related-ID omissions, exact ordered prefix/truncation at 64 diagnostics or 65,536 bytes, cancellation/deadline and no partially accepted overflow. Assert mutation of caller collections cannot change decisions and invalid text never leaks. Depends: T007. Trace: FR-004/005/016/019, US2.1-4, SC-002/006; principles II, VI, XI.
- [X] T017 [P] [US2] Add owner-settings validator regressions in `tests/unit/Kofte.Host.Tests/Features/ModuleComposition/ModuleConfigurationTests.cs`: 128 fields/module, ASCII token at most 64 characters, path at most eight known segments, declared required/type/unknown/constraint guidance, nested unknown keys mapped to known containing fields, disabled validators never called, no cross-owner/environment fallback and invalid settings returning zero mutating effects. An ignoring-token validator must be exercised later through owned-process termination, not an abandoned unit-test thread. Depends: T009, T012. Trace: FR-005/007/016/018, US2.4, SC-002/006; principles VI, XI.
- [X] T018 [P] [US2] Add contribution regressions in `tests/unit/Kofte.Platform.Tests/Modules/Composition/ContributionClaimTests.cs`: wrong owner/kind/version/capability/local name, duplicate claims, reserved host prefixes, route-parameter renaming and literal/parameter overlap, same local permission under different owners accepted, same-owner duplicate rejected and every unsupported executable future category rejected. Cover 128 claims/module, 1,024 enabled total, 128-character ASCII IDs and 256-character canonical routes plus one over. Depends: T007. Trace: FR-005/006/018/019, US2.4, SC-002; principles II, VI, VII, IX, XI.

### Implementation

- [X] T019 [US2] Implement bounded admission/deployed-set validation in `src/Kofte.Platform/Modules/Composition/CompositionResolver.cs`; reuse accepted 002 validators/host context and immutable descriptions unchanged, validate every deployed manifest including disabled ones, and reject invalid/duplicate/unknown-version/missing-selected implementations without fallback or author callback execution. Pure resolution performs no IO/logging/settings validator/storage work. Depends: T015, T016. Trace: FR-002/004/005/019, US2.1, SC-002; principles II, III, VI.
- [X] T020 [US2] Implement required-dependency compatibility, stable topological ordering and bounded cycle diagnostics in `src/Kofte.Platform/Modules/Composition/CompositionGraph.cs`; use accepted `VersionRange` membership directly, ordinal-smallest ready canonical ID and one canonical diagnostic per cyclic SCC with sorted members/explicit omitted count. No downloads, silent enablement, version solving, path enumeration or rejected execution order; enforce graph/work/deadline bounds. Depends: T019. Trace: FR-003/004/016, US2.2/3, SC-002; principles II, X.
- [X] T021 [US2] Implement deterministic safe rejection ordering/truncation in `src/Kofte.Platform/Modules/Composition/CompositionDiagnostic.cs`: phase, canonical-owner/null sentinel, fixed field order/index, related ID, ordinal code; preserve nested 002 field ordering and internally stable invalid-manifest ordering without echoing invalid input/display text/discovery tie-breakers. Reserve the final truncation marker and serialized-byte budget; return no executable subset on rejection. Depends: T020, T016. Trace: FR-004/005/017, US2.1-4, SC-002/006; principles II, VI.
- [X] T022 [US2] Implement supervised enabled-owner field and pure cross-field validation in `src/Kofte.Host/Features/ModuleComposition/ModuleConfigurationValidator.cs`; consume T009 snapshots and T007 owner scopes only, enforce the 250-ms/deadline cap through the owned command process, fixed field/reason catalogs and no disabled author invocation. Do not reflect unknown keys/values or make validation a registration/IO hook. Depends: T017, T021. Trace: FR-005/007/016/018, US2.4, SC-002/004/006; principles VI, X.
- [X] T023 [US2] Implement declarative claims/reservations in `src/Kofte.Platform/Modules/Composition/ContributionValidator.cs`; enforce "Kind, version, stable local identity; declaration/execution mode; routes additionally method and canonical template. Owner must match descriptor. Permissions originate in its accepted permission list." Apply composition-v1 capability mapping, owner-local/version-independent keys, fixed route grammar/methods, normalized parameter/literal overlaps and host `/health`, `/openapi`, `/api/kofte` prefix reservations. Backend lifecycle/DI is the only executable support; metadata never grants authority or a future runtime. Depends: T018, T021. Trace: FR-005/006/018/019, US2.4, SC-002; principles II, VI, VII, IX.
- [X] T024 [US2] Implement the six-phase whole-set preflight and supervised `--modules validate` path in `src/Kofte.Host/Features/ModuleComposition/CompositionPreflight.cs`, `ModuleCommand.cs` and `CompositionSupervisor.cs`; supply the validate-only public `ModuleCompositionHost.RunAsync` entry in `ModuleCompositionHost.cs` and explicit test deployment in `tests/contract/Kofte.Composition.Consumer/DeploymentProgram.cs` before T025 consumes it. Combine admission/manifests/graph, settings, claims/recipes and complete read-only storage inspection before any mutating stage. Hold accepted host/read lease, admit only the same deployment executable/catalog via private channels, revalidate child snapshot identity, drain raw output and terminate/reap on timeout/output failure within 45 seconds; reject worker bypass/unknown CLI. Preserve exit meanings 0/2/3/4/1 and emit identity-less refused-boundary evidence. Depends: T012, T014, T022, T023, T005. Trace: FR-002-007/016-018, US2.1-4, SC-002/004/006; principles II, IV, VI, X.
- [X] T025 [US2] Add required public `invalid-composition` and `deterministic-corpus` scenarios in `tests/contract/Kofte.Composition.Consumer/Scenarios/ValidationScenarios.cs` and `tests/contract/Kofte.Platform.ContractTests/Composition/CompositionPublicContractTests.cs`; run every fixed expected graph/order/ordered rejection 100 times across ten discovery permutations in both real cultures through direct public author-contract and host-consumer paths. Include exact diagnostic/one-over/route/settings cases, callback counters and full provider inventory comparisons proving zero preflight effects, not merely no published route. Depends: T004, T005, T024, T015. Trace: FR-002-007/019/020, US2.1-4, SC-002/006; principles II, IV, VI, XI.
- [X] T026 [US2] Execute US2 unit/public and real-provider preflight selectors through the existing runners; record exact cases/100-by-ten-by-culture observations, original inventory equality, actual command exits/budgets and correction outcomes in `specs/004-module-lifecycle/implementation.md`. Verify malformed/duplicate/disabled input never silently becomes empty-ready; unknown/unavailable/zero-test results fail. Depends: T025. Trace: FR-002-007/016-020, US2.1-4, SC-002/004/006; principles II, IV, VI, XI.

**Checkpoint**: All four US2 scenarios have bounded independent expected
decisions and real no-effect evidence. Validate success means admissible
composition, not completed preparation, serving readiness or acceptance.

## Phase 4: User Story 1 - Start only the intended trusted modules (Priority: P1)

**Goal**: Empty boot stays unchanged; only the explicit valid trusted selection
becomes active, with independent public module behavior.
**Independent Test**: Real empty, public prerequisite/dependent, disabled and
missing-selection process boots observe exact identities/order/behavior and
zero disabled callbacks/allocation. Initially use no-storage modules; US3
supplies the separately required real relational preparation proof.

### Required Validation

- [X] T027 [P] [US1] Add phase/publication/DI lifecycle regressions in `tests/unit/Kofte.Host.Tests/Features/ModuleComposition/CompositionLifecycleTests.cs`: all registrars then all Initialize, read-only migration verification then all Start and fresh health before publication; partial entry marked before callbacks, at-most-once Stop, reverse owner scope disposal, container-owned disposal exactly once, no host/cross-owner registration replacement and no module singleton/hosted service. No rejected/partially healthy set may be ready. Depends: T026. Trace: FR-001/005/008/011/015/019, US1.1-4; principles II, III, VI, XI.
- [X] T028 [P] [US1] Implement independent Alpha/Beta/no-storage author fixtures in `tests/contract/Kofte.Composition.TestModule/Modules/`, using only public contracts, own settings validators and typed public services with distinguishable computation/observations. Constructors/registrars/runtime counters prove disabled zero invocation; dependency behavior uses an explicit versioned public binding, not arbitrary cross-owner DI or platform internals. Keep contexts/migrations for later US3 inside this test project. Depends: T026. Trace: FR-001/002/007/019, US1.2/3, SC-001; principles II, III, VI, XI.

### Implementation

- [X] T029 [US1] Extend T024's public `ModuleCompositionHost.RunAsync` entry and inert catalog integration in `src/Kofte.Host/Features/ModuleComposition/ModuleCompositionHost.cs`, adding the T028 modules to the explicit deployment root in `tests/contract/Kofte.Composition.Consumer/DeploymentProgram.cs`; reconstruct the same copied catalog in admitted workers. Default `BootstrapProgram.RunAsync` supplies an empty catalog; never scan/reflection-load a test assembly, instantiate disabled modules, download a dependency or accept an executable type from settings. Depends: T027, T028. Trace: FR-001/002/019, US1.1-4, SC-001; principles I-III, VI.
- [X] T030 [US1] Implement audited native registration and owner scopes in `src/Kofte.Host/Features/ModuleComposition/ModuleRegistration.cs`; snapshot/audit added descriptor ranges and reject existing-descriptor mutation/removal, reserved replacement, cross-owner services, `IHostedService`, global storage authority and root-owned module singletons. Build one validated provider after all registrars, never a provider per module; support explicit prerequisite bindings and factory-versus-container runtime disposal ownership without double disposal. Depends: T029. Trace: FR-005/008/018/019, US1.2/3; principles II, III, VI.
- [X] T031 [US1] Implement sequential whole-phase barriers/coordinator in `src/Kofte.Host/Features/ModuleComposition/CompositionCoordinator.cs`; all-enabled preflight precedes registration, repeatable ephemeral Initialize precedes owner-store verification, topological Start and fresh successful health precede atomic whole-set publication. Enforce no initialize-store/business effects or partial publication, explicit disabled state, no-storage not-applicable migration with zero recipe/binding calls, and reverse partial-entry Stop/cleanup/scopes/provider ownership. Do not fabricate healthy observations or turn relational `prepare_required` into empty success. Depends: T030, T014. Trace: FR-001/008/009/011/015/019, US1.1-4, SC-001/004; principles II-VI.
- [X] T032 [US1] Extend `src/Kofte.Host/Features/ModuleComposition/CompositionSupervisor.cs` for long-lived configured serving: normal runtime credentials/host lease, same executable/catalog, bounded private snapshot/admission, 60-second startup and included cleanup, actual owned termination/reaping on callback timeout. Never apply maintenance's 120-second limit to a healthy serving lifetime, pass provisioner credentials to serve, abandon live callbacks or treat root exit as descendant proof. Preserve explicit unverified-cleanup refusal. Depends: T031, T012. Trace: FR-011/015/016/018, US1.2/4, SC-004; principles VI, X.
- [X] T033 [US1] Wire normal empty/configured modes and validate dispatch in `src/Kofte.Host/Program.cs`, reusing `src/Kofte.Host/Bootstrap/BootstrapConfiguration.cs`, store probes/leases and existing registry commands; no selection keeps the direct v0/v1 bootstrap unchanged, invalid explicit selection fails and configured relational serving verifies already-prepared history without migration/upgrade. Preserve bootstrap HTTP allowlist and existing CLI handling; no anonymous business/lifecycle endpoint or test-project reference. Depends: T032. Trace: FR-001/002/009-011/018/019, US1.1-4; principles I, IV, VI, IX, X.
- [X] T034 [US1] Add `empty-boot`, `valid-dependency` and `disabled-missing` public cases in `tests/contract/Kofte.Composition.Consumer/Scenarios/BootScenarios.cs`; prove actual core endpoint usability, exact public Alpha/Beta service behavior/order, zero pre-ready publication, no module allocation for empty/disabled/no-storage, and explicit nonready missing implementation. Observations must distinguish selected identities from executed callbacks, not count discovery as activation. Depends: T033, T005. Trace: FR-001/002/008/011/019/020, US1.1-4, SC-001; principles I, II, VI, XI.
- [X] T035 [US1] Add both-provider process boot assertions in `tests/integration/Kofte.Storage.IntegrationTests/Composition/CompositionProviderTests.cs` and preserved v0/v1 empty health/OpenAPI checks in `tests/integration/Kofte.Host.IntegrationTests/StorageCompatibilityTests.cs`; compare complete platform/module object inventories, independent host readiness and no-store counts, and reject malformed/duplicate deployed definitions even when disabled. Run actual owned children and clean only verified owned resources. Depends: T034, T004. Trace: FR-001/002/011/018-020, US1.1-4, SC-001/006; principles I, IV, VI, IX, XI.
- [X] T036 [US1] Execute US1 unit/public/real-provider boot checks and document exact phase/service/inventory observations in `specs/004-module-lifecycle/implementation.md`; update `specs/004-module-lifecycle/quickstart.md` only for commands actually delivered, distinguishing the no-storage internal increment from US3 migration/full 004 proof. Any nonzero/zero-test/unavailable result leaves the story incomplete. Depends: T035. Trace: FR-001/002/019/020, US1.1-4, SC-001/006; principles I, X-XII.

**Checkpoint**: US1.1-4 have real process and independent public behavior
evidence. This is the smallest usable startup increment after safe US2
preflight, not an accepted feature or a substitute for relational US3-5.

## Phase 5: User Story 3 - Coordinate safe initialization and migrations (Priority: P1)

**Goal**: Prepare/verify owner-local stores in dependency order without
privileged serving, early publication or invented cross-module rollback.
**Independent Test**: Both real providers show exact clean-install/additive
history/data, whole-recipe preflight, no-storage absence, preserved earlier
commits and bounded authority/contention/interruption refusals.

### Required Validation

- [X] T037 [P] [US3] Add both-provider lifecycle/migration cases in `tests/integration/Kofte.Storage.IntegrationTests/Composition/CompositionMigrationTests.cs`: clean install, additive upgrade, exact per-phase/dependency ordering, no start/publication during prepare, serve read-only current verification, no no-storage binding/migration and no serving revision rewrite/replayed migration. Assert original data/history/assignments, not positive migration totals. Depends: T036. Trace: FR-008-011/016, US3.1/2, SC-004; principles II, IV, V, XI.
- [X] T038 [P] [US3] Add recipe/preflight refusal cases in `tests/integration/Kofte.Storage.IntegrationTests/Composition/CompositionRecipeTests.cs`: a later invalid owner/model/operation/target, unsupported provider/version, missing one provider recipe, no-storage recipe, corrupt/foreign/missing existing target, registry-v0/unknown format and pending serving upgrade. Compare exact full inventory/revisions/sentinels before/after, proving no earlier module write or automatic registry upgrade/repair. Depends: T036. Trace: FR-005/009/010/013, US3.1-3, SC-004; principles IV-VI, XI.
- [X] T039 [P] [US3] Add real process contention/lease-loss/interruption cases in `tests/integration/Kofte.Storage.IntegrationTests/Composition/CompositionAuthorityTests.cs`; exercise incompatible host/maintenance leases, PostgreSQL lock-session loss and killed migration after an earlier owner committed. Assert bounded busy/reconciliation outcomes, no silent reacquisition/second migration/automatic replay, retained intent/checkpoints and exact prior data; reuse accepted crash barriers/recovery helpers. Depends: T036, T004. Trace: FR-010/011/015/016, US3.3/4, SC-004; principles IV-VI, XI.

### Implementation

- [X] T040 [US3] Supply assignment-aware Alpha/Beta provider contexts, histories and clean/additive migrations in `tests/contract/Kofte.Composition.TestModule/Modules/Alpha/Storage/` and `Modules/Beta/Storage/`; use existing assigned factory/snapshot/migration contracts for both providers, actual own sentinels and exact target versions. No test context/migration lives in production or reaches a prerequisite's tables/connections. Depends: T037, T038. Trace: FR-009/010/019, US3.1/2, SC-003; principles III-V, XI.
- [X] T041 [US3] Implement stopped `--modules prepare` dispatch/admission in `src/Kofte.Host/Features/ModuleComposition/ModuleCommand.cs` and `CompositionPreparation.cs`; acquire one accepted exclusive scope before complete inventory inspection, pass elevated credentials only to the owned prepare child and retain authority until work/cleanup/exit verification. Use 120-second total with 105-second work/15-second reserve, safe fixed exits and no implicit serve/registry upgrade. Depends: T039, T040, T012. Trace: FR-005/008-011/016/018, US3.1/4; principles IV-VI, X.
- [X] T042 [US3] Complete all-pending structural recipe/model/operation verification and assigned execution in `src/Kofte.Infrastructure/Composition/CompositionStorageInspector.cs` and `CompositionStoragePreparation.cs`; before the first write validate every recipe against a non-authoritative candidate or actual mapping, then reuse `RegisterAsync`, `BindAsync` and `ModuleStorageBinding.MigrateAsync` topologically under one exclusive scope. Recheck scope/owner/physical target immediately at use; no direct SQL migration bypass, new allocator/registry format or minted candidate binding. Depends: T041, T014. Trace: FR-005/009/010, US3.1-4; principles III-VI.
- [X] T043 [US3] Implement current-target read verification and binding in `src/Kofte.Infrastructure/Composition/CompositionStorageVerification.cs`; compare actual owner/model/history/target, skip already-current migration without revision writes, report `composition_prepare_required` for admissible unregistered/pending owners in serve and reject broken persisted ownership instead. Enforce assigned-store invariant "Existing immutable installation/allocation/owner/key/allocator identity, revision, migration intent/checkpoint and physical markers/history." No-storage gets no assignment/recipe invocation; never derive new persisted keys or change relational need. Depends: T042. Trace: FR-009/010/013, US3.1/2; principles IV-VI.
- [X] T044 [US3] Wire prepare-versus-serve migration barriers into `src/Kofte.Host/Features/ModuleComposition/CompositionCoordinator.cs` and `CompositionPreparation.cs`: all Initialize before migration, stable dependency order within it, prepare stops/cleans after migration without enablement, serve only verifies before Start and gives each owner its own post-verification accessor. Preserve earlier committed work and 003 durability/correlation on later failure; no global rollback/down/delete or retry loop. Depends: T043. Trace: FR-008-011/015, US3.1-4, SC-004; principles II, IV-VI.
- [X] T045 [US3] Add required public `ordered-migration` evidence in `tests/contract/Kofte.Composition.Consumer/Scenarios/MigrationScenarios.cs`; execute real clean/additive prepare/serve on each provider and assert exact histories, original sentinels, assignments, all-recipe-before-write observations, no-storage absence and unchanged revision/history on ordinary restart. Require semantic observations in the T005 harness. Depends: T044. Trace: FR-008-011/019/020, US3.1/2, SC-003/004; principles II, IV, V, XI.
- [X] T046 [US3] Add a later-owner migration-failure/recovery example in `tests/contract/Kofte.Composition.Consumer/Scenarios/MigrationFailureScenarios.cs`; demonstrate retained earlier commit, nonready dependent, truthful failed-owner intent/durability and explicit accepted 003 inspection/reconciliation before fresh-attempt correction. Do not erase a lock/history/row/store or turn an interrupted write into an automatically admissible new owner. Depends: T045, T039. Trace: FR-010/011/015/020, US3.3/4, SC-004/005; principles IV-VI, XI.
- [X] T047 [US3] Extend actual recovery assertions in `tests/integration/Kofte.Storage.IntegrationTests/Composition/CompositionRecoveryTests.cs`; compare complete stopped-set inventory/owner data through the accepted explicit recovery path, refuse unsafe legacy platform-only backup/reset/restore, preserve disabled/missing/foreign owner checks and require deliberate selection for a prior full-set restore with data-loss responsibility. No automatic compensation is inferred from startup failure. Depends: T046. Trace: FR-009/010/015/020, US3.3/4, SC-004/005; principles IV, V, X, XI.
- [X] T048 [US3] Execute US3 both-provider integration/recovery/public selectors and retained assigned-model/constraint/ownership cases; record exact clean/upgrade/current/failure/contention/lease-loss comparisons, actual process exits and elapsed clipped budgets in `specs/004-module-lifecycle/implementation.md`. Re-run the changed failure path, not just a proxy happy path; missing real PostgreSQL infrastructure remains explicit failure. Depends: T047. Trace: FR-005/008-011/015/016/019/020, US3.1-4, SC-003-005; principles IV-VI, XI.
- [X] T049 [US3] Document delivered validate → explicit stopped prepare → ordinary serve flow and forward/full-set recovery limits in `specs/004-module-lifecycle/quickstart.md`, `docs/development/local-development.md` and `docs/architecture.md`; state runtime CREATE denial, explicit registry upgrade, repeated ephemeral initialization and no migration/business effects during ordinary startup. Include only measured executable examples and truthful unavailable/unsupported limits. Depends: T048. Trace: FR-009/010/015/018/020, US3.1-4; principles IV-VI, X, XII.

**Checkpoint**: US3.1-4 have actual both-provider order/history/authority/failure
evidence. Preparing does not start a module; ready serving never provisions.
Earlier owner-local commits remain real when a later owner fails.

## Phase 6: User Story 4 - Disable and re-enable without losing data (Priority: P1)

**Goal**: Coherent stopped/restarted selection changes preserve exact stores.
**Independent Test**: Three complete fresh-process cycles on each provider
compare every original assignment/sentinel/history and unrelated/platform
inventory; an invalid dependent selection refuses without mutation.

### Required Validation

- [X] T050 [P] [US4] Add both-provider process cycle cases in `tests/integration/Kofte.Storage.IntegrationTests/Composition/CompositionPreservationTests.cs`; prepare/write distinct owner records, then execute three complete disable/stop/restart/re-enable/stop cycles with new actual process/attempt IDs. Compare exact original assignment fields, records, applied history/revision and unrelated/platform state after every leg; assert zero disabled register/provision/migrate/Start calls. Depends: T049. Trace: FR-009/012/013/020, US4.1/2, SC-003; principles IV, V, XI.
- [X] T051 [P] [US4] Add dependent-disable and partial-stop/disposal regressions in `tests/unit/Kofte.Host.Tests/Features/ModuleComposition/CompositionStopTests.cs` and `tests/integration/Kofte.Storage.IntegrationTests/Composition/CompositionSelectionTests.cs`; reject a still-enabled dependent without cascade/writes, revalidate changed metadata/no-storage declarations and compare reverse entered-module Stop/LIFO cleanup/owner scope/provider order, at-most-once disposal and no prior-attempt health reuse. Depends: T049. Trace: FR-012-016, US4.3/4, SC-003/004; principles II-VI, XI.

### Implementation

- [X] T052 [US4] Complete selection/disabled projection and preservation in `src/Kofte.Host/Features/ModuleComposition/CompositionPreflight.cs` and `CompositionCoordinator.cs`; omitted/disabled deployments remain explicit operator observations without author validators/runtime/contribution activation, and preparation never touches their registry rows/history/data. Re-enable repeats current compatibility/configuration/owner checks and exact persisted bindings; invalid dependent selection blocks before writes, not implicit disable/install/repair. Depends: T050, T051. Trace: FR-001/012/013, US4.1-3, SC-003; principles II, IV-VI.
- [X] T053 [US4] Complete reverse entered-resource cleanup in `src/Kofte.Host/Features/ModuleComposition/ModuleAttemptResources.cs` and `CompositionCoordinator.cs`; enforce 32 registrations/owner and 512/attempt with explicit refusal, entry-before-callback, at-most-once Stop after partial Initialize/Start, bounded LIFO within reverse module order, owner scopes after Stop/cleanup and provider last. Withdraw readiness first, never delete durable stores or dispose a container-owned runtime twice; a stuck callback triggers owned termination, not concurrent later cleanup. Depends: T052, T012. Trace: FR-012/015/016, US4.4, SC-004; principles II-VI, X.
- [X] T054 [US4] Add required public `disable-reenable` case in `tests/contract/Kofte.Composition.Consumer/Scenarios/PreservationScenarios.cs`; execute all three real cycles per provider using the private file only between stopped attempts, require exact original Alpha/Beta assignment/data/history and unrelated-state comparisons after each leg, and include dependency-invalid disable plus unchanged rejected selection. Positive restart totals without retained-data observations must fail the harness. Depends: T053, T005. Trace: FR-012/013/019/020, US4.1-4, SC-003; principles II, IV-VI, XI.
- [X] T055 [US4] Exercise explicit additive-release change separately from no-change cycles in `tests/integration/Kofte.Storage.IntegrationTests/Composition/CompositionUpgradePreservationTests.cs`; current re-enable changes no history/revision, an explicitly prepared compatible upgrade advances only its owning target while retaining original mapping/data and unrelated owner state, and incompatible/declaration/identity changes refuse unchanged. Depends: T054. Trace: FR-009/010/012/013, US4.1-3, SC-003; principles IV, V, XI.
- [X] T056 [US4] Execute US4 unit/integration/public cases on both real providers; record each cycle's process/attempt identity, exact original comparisons, reverse cleanup/exit evidence and separate explicit-upgrade exception in `specs/004-module-lifecycle/implementation.md`. An omitted/decremented comparison or unavailable cycle leaves SC-003 unmet. Depends: T055. Trace: FR-012-016/019/020, US4.1-4, SC-003/004; principles IV, V, XI.
- [X] T057 [US4] Update `specs/004-module-lifecycle/quickstart.md` and `docs/development/local-development.md` with the measured stopped private-file edit/restart/re-enable path, no cascade/uninstall/hot-unload, exact data/history preservation, safe stop and explicit recovery/rollback responsibilities. Do not imply a lifecycle administration API/page/tool exists. Depends: T056. Trace: FR-012/013/015/018/020, US4.1-4; principles II, IV-VI, X, XII.

**Checkpoint**: US4.1-4 and SC-003 have exact both-provider fresh-process
preservation and reverse-cleanup evidence, not just three loop iterations.

## Phase 7: User Story 5 - Diagnose health, failure and a corrected retry (Priority: P2)

**Goal**: Honest current-attempt health and safe useful correction/recovery.
**Independent Test**: Inject each named boundary/health/cancellation/timeout/
cleanup fault; assert private state/dependency impact, aggregate status, fresh
retry after only the stated correction, exact budgets/exit and zero canary
leaks while required safe records remain present.

### Required Validation

- [X] T058 [P] [US5] Add health/freshness/dependency regressions in `tests/unit/Kofte.Host.Tests/Features/ModuleComposition/CompositionHealthTests.cs`; own/effective health severity is failed > unknown > degraded > healthy, stale/missing becomes unknown before propagation, healthy dependents cannot conceal unhealthy transitive prerequisites and independent roots remain identifiable. Prove current attempt/individual age, initial all-healthy publication, no overlap, 10-second post-sweep schedule, 1-second callback/20-second sweep and 30-second freshness even during a stalled sweep. Depends: T057. Trace: FR-008/011/014/016, US5.1/2, SC-004/005; principles II, X, XI.
- [X] T059 [P] [US5] Add aggregate HTTP regressions in `tests/integration/Kofte.Host.IntegrationTests/CompositionHealthEndpointTests.cs` and focused `tests/unit/Kofte.Host.Tests/Bootstrap/HealthTests.cs` cases: same response fields/state vocabulary/cache-control, live process unaffected, nonhealthy composition host/503 versus genuine storage/503, 5-second request/3-second store budget and no author health callback executed by requests. Assert zero anonymous inventory/settings/counts and no lifecycle/business mutation route. Depends: T057. Trace: FR-011/014/018, US5.1/2/4, SC-006; principles VI, IX-XI.
- [X] T060 [P] [US5] Add actual process fault/termination cases in `tests/integration/Kofte.Storage.IntegrationTests/Composition/CompositionDeadlineTests.cs`; validator/registrar/Initialize/Start/health/Stop/cleanup ignoring cancellation, cancellation before/during work, partial entry, lost authority and cleanup overflow/failure must report correct stage/outcome within shared totals. Assert root and tracked fixture-descendant exit separately, refusal of unverifiable cleanup and no cleanup/retry concurrent with abandoned work; no arbitrary PID/name kill. Depends: T057, T004. Trace: FR-015/016, US5.3, SC-004; principles V, VI, X, XI.
- [X] T061 [P] [US5] Add actual both-provider record/span/event/metric/stdout/stderr/HTTP captures in `tests/integration/Kofte.Storage.IntegrationTests/Composition/CompositionPrivacyTests.cs`; seed settings, credentials, unknown keys, physical paths and callback exception/output canaries across success and all rejected/mutation/health/stop boundaries, including no valid owner, acquisition/admission/output/projection failure. Assert zero leaks AND each expected code/stage/outcome/guidance/current correlation/storage durability plus bounded labels and event-limit refusal. Depends: T057, T004. Trace: FR-007/017/018/020, US5.4, SC-006; principles VI, X, XI.

### Implementation

- [X] T062 [US5] Implement immutable operator observations/snapshot publication in `src/Kofte.Host/Features/ModuleComposition/CompositionObservations.cs`; enforce `ModuleObservation`: "Canonical owner; desired enabled; stage/outcome; own health, effective health; current attempt; monotonic observation instant/age; bounded affected-prerequisite IDs; fixed code/guidance. No raw author text." Enforce `CompositionSnapshot`: "Current attempt, immutable module observations for trusted operators, overall state, bounded counts, publication flag." Only current instance updates own health; atomically replace snapshots and never persist/export settings/fingerprints or reuse prior success. Depends: T058, T011. Trace: FR-008/011/014/017/018, US5.1/2/4; principles II, VI, X.
- [X] T063 [US5] Implement topological serialized health sweeps and freshness derivation in `src/Kofte.Host/Features/ModuleComposition/CompositionHealth.cs`; apply all T058 bounds/severity/propagation, fixed registered reason tokens, initial healthy gate and immediate stale/nonhealthy readiness withdrawal without deleting data/implicitly disabling/restarting lifecycle. Ignoring-token checks trigger the owned serving supervisor rather than a discarded Task; health state never confers authority. Depends: T062, T060. Trace: FR-011/014-016, US5.1/2, SC-004/005; principles II, VI, X.
- [X] T064 [US5] Wire composition snapshot-only aggregate projection in `src/Kofte.Host/Features/Bootstrap/HealthHandler.cs`, `HealthEndpoints.cs` and `src/Kofte.Host/Features/ModuleComposition/ModuleCompositionHost.cs`; preserve existing `version`, `state`, `component`, `correlationId`, cache-control, public OpenAPI schema/web adapter and store failure behavior, using existing unavailable/host for composition failure. Never invoke module code on anonymous requests or add counts/inventory/control endpoints. Depends: T059, T063. Trace: FR-011/014/018, US5.1/2/4, SC-006; principles VI, IX, X.
- [X] T065 [US5] Complete failure/stop/output/termination handling in `src/Kofte.Host/Features/ModuleComposition/CompositionSupervisor.cs`, `CompositionCoordinator.cs` and `CompositionDiagnostics.cs` against T060/T061: withdraw publication first, cancel/cooperatively clean within reserve, then terminate/reap only owned processes and report incomplete/uncertain when unverifiable. Required diagnostic failure cannot become success; do not silently reacquire leases, delete evidence or retry an uncertain migration. Preserve exact CLI exit categories and identity-less boundary records. Depends: T064, T061. Trace: FR-015-018, US5.3/4, SC-004/006; principles V, VI, X.
- [X] T066 [US5] Add public `health-impact` scenario in `tests/contract/Kofte.Composition.Consumer/Scenarios/HealthScenarios.cs`; exercise own healthy/degraded/failed/unknown, expired current observations, transitive dependent/independent-root distinction and actual aggregate ready/503 responses with unchanged schema/no inventory. Require measured observations in `tests/contract/Kofte.Platform.ContractTests/Composition/CompositionPublicContractTests.cs`, not truthy overall counts. Depends: T065, T005. Trace: FR-014/018-020, US5.1/2/4, SC-005/006; principles II, VI, IX, XI.
- [X] T067 [US5] Add required `failure-correction` scenario in `tests/contract/Kofte.Composition.Consumer/Scenarios/CorrectionScenarios.cs`; each documented configuration/Initialize/Start/health/migration failure succeeds after only its named correction under a new attempt, with verified exit before ephemeral retry and explicit accepted recovery for uncertain writes. Assert original assignments/data/history and earlier commits preserved, no hidden repair and stale success rejected. Depends: T066, T046. Trace: FR-013-016/019/020, US5.3, SC-004/005; principles II, IV-VI, XI.
- [X] T068 [US5] Add public `bounded-stop` scenario in `tests/contract/Kofte.Composition.Consumer/Scenarios/StopScenarios.cs`; execute ignoring-token faults, cancellation, contention/lost authority, reverse partial-entry cleanup and actual killed/reaped child observations within every applicable validate/prepare/start/stop envelope. Include a controlled unverified-termination refusal without orphaning real work; fixture-owned descendant proof is separate from root exit. Depends: T067, T060. Trace: FR-015/016/019/020, US5.3, SC-004; principles V, VI, X, XI.
- [X] T069 [US5] Add required public `private-diagnostics` scenario in `tests/contract/Kofte.Composition.Consumer/Scenarios/DiagnosticScenarios.cs`; capture all guide channels/faults, including raw child console writes, malformed/oversized/wrong-attempt frames, event-rate/attempt limits and output-sink failure. Assert zero seeded leaks and exact useful records/guidance/new correlations, recognized 003 durability links and bounded labels; no manufactured success on omitted evidence. Depends: T068, T061. Trace: FR-017-020, US5.4, SC-006; principles VI, X, XI.
- [X] T070 [US5] Finish scenario/contract result validation in `tests/contract/Kofte.Platform.ContractTests/Composition/CompositionConsumerHarnessTests.cs`; deliberately omit/decrement each guide's mandatory observations, expected scenario/culture/permutation/cycle/exit/capture evidence and prove rejection. Require every standalone and `all` result to agree on semantics while retaining sanitized bounded output. Depends: T069. Trace: FR-019/020, US5.1-4, SC-001-006; principles II, VI, XI.
- [X] T071 [US5] Execute changed health/HTTP unit and both-provider integration/recovery/privacy/public cases; record exact fault stage/outcome/elapsed budgets, current correlation, actual root/descendant exit, corrected-retry and nonleaking safe evidence in `specs/004-module-lifecycle/implementation.md`. Verify original symptoms disappear and success-shaped omissions still fail; report unavailable checks plainly. Depends: T070. Trace: FR-014-020, US5.1-4, SC-004-006; principles VI, X, XI.
- [X] T072 [US5] Document actual observation/health/timeout/retry/privacy behavior and supported trusted-code limitations in `specs/004-module-lifecycle/quickstart.md`, `docs/development/local-development.md` and `docs/architecture.md`; no sandbox/hot-unload/guaranteed descendant termination, no durable audit/settings product, no anonymous detailed inventory and no current observations claimed across restart. Include fixed guidance for failed output/cleanup/reconciliation. Depends: T071. Trace: FR-014-020, US5.1-4; principles II, VI, X, XII.

**Checkpoint**: US5.1-4 and SC-004-006 have actual current-state, private
diagnostic, corrected-retry and owned-process evidence. Useful safe output is
required alongside zero leaks; a timeout alone is not termination.

## Phase 8: Cross-Cutting Evidence and Documentation

- [X] T073 Wire required composition projects/files/scenarios and focused test selectors into `scripts/verify.py`, with omission/zero-test/provider/result regressions in `scripts/tests/test_verify.py` and existing safe report support in `scripts/runtime_support.py` if needed; preserve all old targets/minima and require every composition guide case, not just presence of a scenario name. Quality builds both test-owned assemblies; unit/integration/contract/recovery/privacy execute their real new cases under existing suite ownership. Keep failure propagation and unavailable infrastructure fail-closed. Depends: T072. Trace: FR-019/020, SC-001-006; principles III, VI, X, XI.
- [X] T074 [P] Execute every delivered standalone guide scenario and each provider's `all` in `tests/contract/Kofte.Composition.Consumer/Program.cs`; retain distinct bounded sanitized public-guide reports under `artifacts/verification/` for serial incorporation by T079, including deterministic 100-by-ten-by-culture, three actual preservation cycles/provider, every corrected example, owned exit and private captures. Both exact published `dotnet run` all commands in `quickstart.md` must pass after the Release build; do not substitute internal test-only examples or a full-reference-module claim. Depends: T073. Trace: FR-019/020, SC-001-006; principles II, IV, VI, XI.
- [X] T075 [P] Run the retained and extended quality/unit/both-provider integration/recovery/contract/privacy gates listed below and retain their separate named-runner reports under `artifacts/verification/` for serial incorporation by T079; include actual UNIQUE/concurrency/reserved/63-byte/path/model/history/owner checks, crash/intent/deadline/full-set recovery and independent manifest consumers from 001-003. Do not edit shared implementation evidence concurrently with T074. Existing evidence is a regression baseline, not proof for new behavior; unexpected skips/zero cases/missing PostgreSQL fail. Depends: T073. Trace: FR-001/005/009/010/015/016/019/020, SC-001-006; principles IV-VI, X, XI.
- [X] T076 Run retained browser/accessibility/loading/error/status, warm startup and failure-propagation gates below, including configured module timing within the existing 285/300-second stack envelope; extend only the applicable cases in `scripts/dev.py`, `scripts/tests/test_dev.py` and `tests/e2e/bootstrap.spec.ts` if integration requires them. No new lifecycle UI or generated-contract drift is permitted; preserve real child failure and cleanup reporting. Record actual commands/results in `specs/004-module-lifecycle/implementation.md`. Depends: T074, T075. Trace: FR-001/011/014/016/018/020, SC-004/006; principles I, VI, IX-XI.
- [X] T077 Reconcile delivered interfaces/limits/examples and current progress in `README.md`, `CONTRIBUTING.md`, `docs/README.md`, `docs/architecture.md`, `docs/development/local-development.md`, `docs/development/spec-workflow.md`, `specs/004-module-lifecycle/quickstart.md` and its `contracts/`; preserve historical decisions/evidence and distinguish actual runtime from inert later capabilities. Audit product references/test ownership, runtime credential stripping and ignored private files/artifacts; leave reviewer-owned markers/approved intent unchanged absent explicit authorization. Depends: T076. Trace: FR-018-020, SC-006; principles I-III, VI-XII.
- [X] T078 Execute `python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10` after final wiring, then reconcile every expected local suite/step/provider/scenario and actual exact-head CI context with `specs/004-module-lifecycle/implementation.md`; retain only sanitized bounded evidence. Full-suite success cannot substitute for a missing requirement/observation. Fix only scoped defects, rerun affected checks plus the complete gate when the change warrants it, and leave genuine unavailable/failed gates incomplete. Depends: T077. Trace: FR-020, SC-001-006; principles X, XI.
- [X] T079 Finish requirement/scenario/outcome and principle evidence mapping in `specs/004-module-lifecycle/implementation.md`: exact commands/results, assessed revision/supported target, all 20 scenarios/20 FRs/six SCs, R1-R12 design decisions, all tasks, measured budgets/exit/privacy/preservation comparisons and known limitations. Do not tick a task without its actual evidence, claim new human usability/production/MVP proof or mark reviewer criteria to hide an implementation gap. Depends: T078. Trace: FR-001-020, SC-001-006; principles I-XII.
- [X] T080 Run `python3 scripts/check_repository.py`, `python3 -m unittest discover -s scripts/tests -v` and `git diff --check`; inspect the full intended diff and publish implementation evidence/task markers/driver ledger in the same commit on `feature/004-module-lifecycle`, immediately push and update draft PR #7 using its template with exact check outcomes and limitations. Follow `.github/skills/ship-change/SKILL.md`; keep PR draft, do not merge/close/auto-merge/delete its branch, and hand off to the next separate **converge** invocation only. Depends: T079. Trace: FR-020; principles XI, XII/governance.

Historical checkpoint: T073-T076 had actual passing named-runner evidence on supported Ubuntu CI
at code head `aaba315`; T077 includes the delivered-interface audit and truthful
blocked status. These markers do not declare the local complete gate green:
T078-T080 remain incomplete after the diagnostic local run failed. The resumed
stage has a reproduced fixture-timeout regression/fix and safe public-failure
identities, but complete post-fix local/CI reconciliation is still required.
See the [gate history](implementation.md#blocked-local-gate-and-ci-reconciliation)
and [resumed verification](implementation.md#resumed-verification-fixture-administration-and-safe-public-failures).
The [final reconciliation](implementation.md#final-complete-gate-and-implementation-handoff)
now records a passing complete local gate and exact-code CI at `267efc8`,
all 286 steps and every required guide observation. The failed checkpoints
above remain historical evidence, not the current completion result.

### Required implementation commands

These commands are future task acceptance requirements, **not runs claimed by
task generation**. Use the exact installed supported environment in the guide;
restore only after manifest changes or a missing-dependency failure. After
T003 changes dependencies/projects, validate locked restore, warnings-as-errors
build and format through quality. Targeted T026/T036/T048/T056/T071 runs use
the existing .NET test runner's
`--filter-class`, `--filter-trait Provider=sqlite|postgres` and
`--minimum-expected-tests 1` conventions. The named verifier owns provider
fixtures for broader checks; final gates retain all prior cases. Give each
new composition integration/public test the appropriate provider and
`Suite=recovery|privacy` traits so T073 includes it in the actual named suites.

```bash
python3 scripts/check_repository.py
python3 -m unittest discover -s scripts/tests -v
git diff --check
python3 scripts/verify.py --suite quality
python3 scripts/verify.py --suite unit
python3 scripts/verify.py --suite integration --provider sqlite
python3 scripts/verify.py --suite integration --provider postgres --disposable-postgres
python3 scripts/verify.py --suite contract --provider both --disposable-postgres
python3 scripts/verify.py --suite recovery --provider both --disposable-postgres
python3 scripts/verify.py --suite privacy --provider both --disposable-postgres
python3 scripts/verify.py --suite browser
python3 scripts/verify.py --suite startup --warm-starts 10
python3 scripts/verify.py --suite failure-propagation --disposable-postgres
python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10
```

T074 also executes the guide's two exact Release consumer `--scenario all`
commands with `sqlite` and explicitly opted-in disposable `postgres`, and the
fixed standalone scenarios. No developer/production storage is a test target.

## Dependencies and Execution Order

Every task's `Depends:` is the authoritative direct prerequisite set. IDs
are sequential, all edges point backward and transitive prerequisites are
inherited; phase order alone never supplies a missing edge.

```text
Entry: accepted 001-003 + reviewed quality + Accepted ADR 0006 + passing analyze
T001 → T002 → T003
T003 → T004 (owned fixtures) || T005 (public harness)
T003 → T006 → T007
T007 → T008 → T009
T007 → T010 → T011 → T012 (also T007)
T004 + T007 → T013 → T014
US2 T015-T026 → US1 T027-T036 → US3 T037-T049
  → US4 T050-T057 → US5 T058-T072
T072 → T073 → T074 || T075 → T076 → T077 → T078 → T079 → T080
```

Story graph reflects actual shared runtime ownership: safe rejection (US2) is
required before activation (US1); stopped relational preparation (US3) follows
working composition; preservation (US4) needs real prepared state; diagnostic/
failure proof (US5) exercises all earlier stages. Each story has independent
acceptance checks against these prerequisites, not five unrelated runtimes.

### Genuine parallel examples

| Window | Independent different-file tasks after prerequisites | Join |
| --- | --- | --- |
| Setup | T004 owned provider/process fixtures and T005 public result harness after T003. | Story/process/public checks require both. |
| Foundations | T008 selection tests, T010 diagnostics tests and T013 real storage preflight tests after their explicit prerequisites. | T009/T011/T014 then feed whole-set preflight. |
| US2 | T015 graph corpus, T016 limits, T017 host settings and T018 contribution tests. | Resolver/host validators meet at T024. |
| US1 | T027 host lifecycle/DI tests and T028 independent module library. | Public deployment entry T029. |
| US3 | T037 migration, T038 recipe refusal and T039 process authority tests. | T040/T041 then assigned preparation. |
| US4 | T050 real cycle tests and T051 selection/cleanup tests. | Preservation wiring T052. |
| US5 | T058 health, T059 HTTP projection, T060 actual process deadlines and T061 privacy captures. | T062-T065 implementation and public proof. |
| Cross-cutting | T074 public guide execution and T075 other named gates after T073. | T076 regression acceptance. |

Do not concurrently edit the same file from multiple tasks or share mutable
provider installations, ports, selection snapshots or global capture listeners.
Distinct test fixture lifetimes are required even for `[P]` tasks.

### Incremental implementation strategy

First establish safe complete validation (US2), then the smallest useful
**US1 no-storage empty/public-module startup increment**. It is an internal
MVP-first increment, not feature/MVP acceptance. Add actual assigned preparation
(US3), exact restart preservation (US4), and complete health/failure/privacy
(US5), then execute all guide/regression/full gates. Never temporarily publish
an unsafe subset while later safety tasks remain pending.

## Constitutional Coverage and Scope

### Requirement, scenario and outcome coverage

| Approved intent | Tasks and observable acceptance |
| --- | --- |
| FR-001/002; US1.1-4; SC-001 | T006-T009, T019, T027-T036: real empty/valid/disabled/missing boots, independent public service and exact zero callback/store effects. |
| FR-003/004; US2.1-3; SC-002 | T015/T016/T019-T021/T025-T026: fixed expected orders/rejections, accepted ranges, bounded SCCs/diagnostics, 100 repeats × ten permutations × two real cultures. |
| FR-005-007; US2.1-4 | T008-T009/T013-T026/T038/T042: strict owner settings, contribution grammar/collisions, whole-set physical/recipe preflight and unchanged full inventory on refusal. |
| FR-008-011; US3.1-4; SC-004 | T027/T030-T033/T037-T048: whole-phase barriers, assigned clean/additive/current/no-storage paths, exclusive authority, no serving mutations and retained earlier commits. |
| FR-012/013; US4.1-4; SC-003 | T050-T057: three fresh-process cycles/provider, exact original assignments/sentinels/history/unrelated state, coherent dependent selection and separate explicit upgrade. |
| FR-014; US5.1/2; SC-005 | T058-T059/T062-T064/T066: current-attempt own/effective/fresh health, transitive dependency impact, independent roots and unchanged aggregate HTTP contract. |
| FR-015/016; US3.3/4, US4.4, US5.3; SC-004/005 | T012/T024/T032/T039/T046-T048/T051/T053/T060/T065/T067-T068/T071: partial-entry cleanup, actual exit/total deadlines and explicit reconciliation/fresh correction. |
| FR-017/018; US5.4; SC-006 | T010-T012/T024/T059/T061-T065/T069-T071: safe failed-boundary/transition records and real channel captures, private authority/inventory, no control endpoint or raw output. |
| FR-019/020; all stories; SC-001-006 | T003-T005/T007/T025/T028-T036/T040/T045-T049/T054-T057/T066-T080: separate public authors/host deployment, eleven mandatory executable examples, retained baseline and full local/CI evidence. |

All 20 acceptance scenarios map through the five story rows (four each);
all 20 FRs and six SCs have explicit positive/negative acceptance tasks.
This is task traceability, not the next read-only analysis result.

### Plan decisions

| Decision | Tasks |
| --- | --- |
| R1 compiled discovery | T003/T006/T009/T028-T029/T033-T036. |
| R2 deterministic bounded whole-set resolution | T015-T021/T025-T026. |
| R3 explicit owner configuration | T007-T009/T017/T022/T052. |
| R4 contribution ownership and honest execution | T006/T018/T023/T024/T059/T064. |
| R5 native DI and phase barriers | T007/T027-T031/T037/T044/T051/T053. |
| R6 read-only preflight/offline preparation | T013-T014/T024/T038-T044. |
| R7 preserved mappings/data/history/recovery | T039-T057/T067. |
| R8 fresh dependency-aware health | T058-T059/T062-T066. |
| R9 bounded actual supervision | T004/T012/T024/T032/T039/T041/T053/T060/T065/T068. |
| R10 safe correlated observations | T010-T012/T061-T062/T065/T069-T071. |
| R11 independent public evidence | T003-T005/T015/T025/T028/T034/T045-T046/T054/T066-T070/T074. |
| R12 retained release/evidence baseline | T073-T080. |

### Principles and applicability

| Principles | Applicable tasks / retained invariant |
| --- | --- |
| I | T029/T033-T036/T076-T077: module-free portable Köfte; no mandatory cloud/Hoard/IaC. |
| II, III | T002-T007/T015-T036/T040/T044/T051-T053: minimal versioned contracts, native DI, trusted code, module-local ownership, dedicated .NET tests and no mediator/mapper/loader framework. |
| IV, V | T013-T014/T037-T057/T067/T075: original immutable registry/actual UNIQUE/63-byte/schema/path/marker/history protection, separate files, module-local commits and explicit recovery without global rollback. |
| VI | T007-T012/T017-T024/T030/T032-T033/T059/T061-T071: operator-only authority, owner settings/services/store, credential stripping, no metadata permission grants and safe useful private/public/telemetry channels. |
| VII | T018/T023/T059/T064/T077 reserve versioned MCP metadata/identity and forbid executable/public tool bypass; authenticated operations remain 007/013. |
| VIII | T007/T031/T044/T067/T072/T077 keep lifecycle ephemeral, not durable jobs/events/resources/realtime. Those runtimes and delivery/replay/actor obligations remain 012/016-019. |
| IX | T018/T023/T059/T064/T076-T077 keep frontend metadata inert, aggregate/web schema and existing Playwright/accessibility intact; no new lifecycle UI or remote JavaScript execution. |
| X, XI | T002-T005/T010-T014/T024-T026/T032/T037-T080 require supported portable local/provider/process/privacy/public proof, exact environments/deadlines/results and fail-closed full local/CI gates. |
| XII/governance | T001/T036/T049/T057/T072/T077-T080: 004 only, approved design/reviewer markers preserved, one stage/branch/draft PR, separate convergence/review/explicit acceptance and human merge. |

No new business REST/UI/MCP/job/provider/realtime operation exists to test for
actor/team parity here. Prove absence of anonymous lifecycle mutation and
preserve aggregate contracts; real OIDC/team permissions/denied tools, durable
jobs/events and subscription tests remain mandatory in 005-007/013/016-018.
No new visual journey warrants a new UI suite; existing browser/accessibility
checks stay mandatory. The test-owned module is not spec 020's full packaged
level-3 reference module; Kubernetes/production smoke and final release remain
022/024. Their obligations are excluded from 004, never waived.

## Completion and Human Handoff

Generation leaves every task unchecked and runs no implementation. **Analyze
is next**, separately on the original `feature/004-module-lifecycle` and
draft PR #7; it must assess these artifacts before implementation.

Implementation must record exact commands/results and acceptance/principle
mapping in `implementation.md`, publish its ledger/artifacts together and push
every commit. Converge, evidence-based final review and explicit numbered-spec
human acceptance are later separate stages. Checked tasks, passing tests,
ADR/checklist approval, autopilot or a prior merge never accept feature 004.
Keep the same draft PR, request no intermediate merge and never merge, close,
auto-merge or delete its branch. Only after passing review and explicit human
acceptance may the assistant mark ready and request the human merge.

## Phase 9: Convergence

- [X] T081 Expose explicit disabled desired state through the actual configured host's bounded private operator channel per FR-008/012/018/020, US1.3, plan R1/R8/R10 and T052/T062/T069/T074 (partial, HIGH; convergence F1). `CompositionHealth.Snapshot` already models disabled owners, but `CompositionWorker`/`ModuleLifecycleCoordinator` never project those observations: an actual `BootDisabled` deployment reports each disabled owner only as `discovered/passed`, and `CompositionRecord`/`CompositionProtocol` carry no disabled state. Wire safe current-attempt disabled observations for both explicitly disabled and omitted deployed owners through `src/Kofte.Host/Features/ModuleComposition/`, reusing the existing observation model and preserving versioning, identity allowlists, finite record/frame budgets, fixed guidance, zero disabled callbacks/storage effects and anonymous aggregate privacy. Extend existing host observation/protocol tests and the both-provider `disabled-missing` public scenario/strict harness to require the actual supervised operator output, not only a directly constructed snapshot or test-owned callback totals; add omission/tampering regressions for the new mandatory evidence. Update the delivered guide/operator contract and implementation mapping with exact commands/results, retaining all existing scenarios and accepted provider/process/privacy gates. Depends: T080. Trace: FR-008/012/018-020; US1.3; SC-001/006; principles II, VI, X, XI, XII.

## Phase 10: Convergence

- [X] T082 Make host-generated executable dependency identities valid across the approved owner/local-name input bounds per FR-006/008/019/020, US1.2, plan R4/R5/R11 and T018/T023/T028/T030/T034 (partial, HIGH; convergence F2). `ContributionValidator` admits 128-character local names, but `ModuleLifecycleCoordinator` prefixes the owner and later passes the combined identity to `ModuleDependencyBinding`, which rejects more than 128 characters: under `org.test.a`, local length 117 activates, while accepted lengths 118 and 128 fail with `composition_activation_failed`. Reconcile generation/validation in `src/Kofte.Host/Features/ModuleComposition/ModuleLifecycleCoordinator.cs` and `src/Kofte.Platform/Modules/Lifecycle/ModuleLifecycle.cs` as needed, preserving approved accepted owner/local-name limits, explicit versioning, ownership, collision safety, bounded work and fail-closed diagnostics; do not truncate names or silently narrow valid inputs. Add focused regressions in the existing dedicated host/platform test projects for combined lengths at/over 128, the inclusive 128-character local name and longer valid owners, requiring actual accepted public dependency binding/Start/readiness rather than declaration-only acceptance. Extend the independent public module/consumer example and strict evidence where needed to prove a boundary case without internal access, retaining disabled/privacy/no-storage behavior. Update directly affected contract/guide explanations and implementation mapping, and run applicable accepted quality/unit/provider/public-contract gates with exact results. Depends: T081. Trace: FR-006/008/019/020; US1.2; SC-001; principles II, III, VI, X, XI.
- [X] T083 Establish and correct the scoped cause of recurring unexpected normal-stop cleanup failures per SC-006, plan R12, FR-020 evidence and T060/T068/T075/T078 (partial, MEDIUM; convergence F3). Fresh composition integration passes 102/104: PostgreSQL `disabled-missing` and `bounded-stop` each report `composition_test_command_exit_expected_0_actual_1_composition_cleanup_incomplete`; the unchanged isolated PostgreSQL provider/deadline selection passes 5/5. Prior convergence/implementation also retained unexplained cleanup failures. Investigate `CompositionWorker`/`ModuleLifecycleCoordinator`/`CompositionSupervisor`, existing owned-process/provider support and the failing public scenario assertions to distinguish a product operation/budget defect from fixture, scheduling or resource interference; no causal diagnosis is established yet. Capture the minimum safe causal stage/reason evidence through existing private boundaries where needed, without raw exceptions, paths, settings, unbounded output or anonymous detail. Fix only the demonstrated cause, add a focused regression if behavior changes, and retain exact failed and corrected results in `implementation.md`. Prove ordinary expected-success cleanup and intentional incomplete/uncertain refusal, owned root/descendant termination and unchanged durable inventory; do not hide failures with retries, relax product deadlines/cleanup assertions, remove scenarios or lower floors. Rerun affected both-provider/public/process gates and reconcile the accepted complete gate and actual-head CI when warranted by the fix; unavailable or failing evidence remains explicit. Depends: T081. Trace: FR-015/016/017/020 evidence; US4.4/US5.3; SC-004/006; principles V, VI, X, XI.

## Phase 11: Convergence

- [ ] T084 Preserve executable export and prerequisite-binding counts across the approved composition bounds per FR-004/006/008/019/020, US1.2, SC-001, plan R4/R5/R11 and T007/T016/T018/T023/T028/T030/T034 (partial, HIGH; convergence F4). The resolver accepts 128 contributions per owner and 1,024 enabled total, but `src/Kofte.Host/Features/ModuleComposition/ModuleLifecycleCoordinator.cs` retains only 129 exports and rejects more than 128 globally, while `src/Kofte.Platform/Modules/Lifecycle/ModuleLifecycle.cs` caps activation bindings at 64 service exports rather than required module identities. A public no-storage probe activates 64 Alpha exports for Beta but fails with 65 (`composition_activation_failed`); 128 independent Alpha exports activate, while adding Beta's one valid independent export fails registration. Reconcile bounded export materialization, registration and copied activation contexts with the existing approved contribution/owner limits, preserving complete exact owner-qualified identities, explicit versions, collision/DI ownership and zero partial publication. Do not truncate, silently drop bindings, reduce accepted inputs or bypass preflight. Add focused regressions in the existing dedicated host/platform test projects covering inclusive/one-over effective bounds, multiple prerequisite owners and independent roots, with actual registration/Start/health/readiness and exact complete binding/service comparisons; extend the existing public consumer and strict observations where needed. Keep existing rejection/privacy/no-storage behavior, update directly affected contract/guide explanations and implementation mapping, and run applicable quality/unit/public/provider gates with exact outcomes. Depends: T082. Trace: FR-004/006/008/019/020; US1.2; SC-001; principles II, III, VI, X, XI.
- [ ] T085 Reconcile the remaining normal-stop host-disposal failures using T083's safe causal boundary per FR-012/013/015/016/017/020 evidence, US4.4/US5.3, SC-003/005, plan R9/R12 and T060/T068/T075/T078/T083 (partial, MEDIUM; convergence F3 remains open). Fresh broad composition integration passes 104/106: SQLite `disable-reenable` and PostgreSQL `failure-correction` each require exit 0 but report `composition_cleanup_incomplete_composition_host_dispose_incomplete`. T083's incomplete-request stop regression and intentional lost-authority refusal pass in that same run; their success does not fix this distinct boundary. Investigate `src/Kofte.Host/Features/ModuleComposition/CompositionWorker.cs` at bounded `app.DisposeAsync()`, callback/coordinator/supervisor ownership and the existing public process/provider fixtures to establish the actual disposal operation/exception/deadline or harness interaction, without presuming a scheduling cause. Capture only bounded private causal evidence, fix the demonstrated scoped cause and add a discriminating regression in existing dedicated tests. Preserve the one-second cleanup action, total stop/termination reserve, required diagnostics, fail-closed uncertain/incomplete refusal, exact original data/history/inventory and actual root/descendant exit; no retries-as-success, widened product deadline, removed assertion/scenario or lowered floor. Retain SC-004/006 refusal/privacy assertions. Record failed and corrected evidence in `implementation.md`, rerun affected both-provider preservation/correction/stop/authority gates and reconcile the required complete local gate and actual-head CI without relabeling historical failed invocations. Depends: T083. Trace: FR-012/013/015-017/020 evidence; US4.4/US5.3; SC-003/005; principles V, VI, X, XI.
