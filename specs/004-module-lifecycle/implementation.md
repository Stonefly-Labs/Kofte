# Implementation: 004 - Module Composition and Lifecycle

**Stage:** implement, started 2026-10-24; in progress, not feature acceptance.
**Assessed starting revision:** `bac7bdc8c10d7c62394688d3e0364ff422cd3dfb`.
**Branch / checkpoint:** `feature/004-module-lifecycle`,
[draft PR #7](https://github.com/Stonefly-Labs/Kofte/pull/7).

## Entry gates and environment

T001: The existing analysis has `Result: pass` and no unresolved finding.
The built-in author checklist is 16/16 and the explicitly reviewer-authorized
quality checklist is 36/36; neither checklist is changed by implementation.
ADR 0006 is Accepted for implementation under **"yeah approved"**.
The ledger selects 004 / implement on the original branch and open draft PR;
the worktree was clean before `begin`. No extension hooks are configured.
No new approval, acceptance or permission to merge is inferred.

The available workstation is macOS 26.5 arm64, within the macOS 15+ supported
profile. The repository-local reviewed SDK at
`artifacts/toolchain/dotnet-10.0.402` supplies SDK 10.0.402 and .NET/ASP.NET
runtime 10.0.13. Node 24.22.0, npm 11.19.1, Python 3.15.0 and Docker Engine
29.8.2 are available. The initial unqualified `dotnet --version` failed
because the shell selected SDK 10.0.203; no pin was relaxed or tool installed.
Subsequent .NET commands explicitly select the existing isolated SDK.

## Dependency adoption

T002: The exact DI abstractions 10.0.13 archive has MIT/source/notice evidence
matching the reviewed inventory, explicit net10.0 assets and no dependencies
for that target. Official current NuGet vulnerability pages have no entry for
this identity. `docs/development/dependencies.md` records the distinction
between NuGet content hashes and signed-archive hashes, compatibility,
provenance and retained notice obligations before adoption. No new package
identity/version or trust setting is introduced.

T003: added only that justified edge and the separate test-owned
module/deployment projects. Initial consumer explicitly fails as unimplemented;
it cannot report successful examples before semantic scenarios exist.
Generated locks, locked restore, a Release warnings-as-errors solution build
and dependency graph reconciliation passed. The notice extension required
updating its inspected source-notice digest; no package pin changed.

## Execution and verification

Only actually executed commands and observed results will be recorded here.
No future guide command, provider/process observation or prior CI result is
new 004 behavioral evidence.

All .NET commands below use:

```bash
export DOTNET_ROOT="$PWD/artifacts/toolchain/dotnet-10.0.402"
export PATH="$PWD/artifacts/toolchain/dotnet-10.0.402:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
```

| Exact command                                                                                                                                                                                                                                                                                                                                                   | Observed result                                                                                                                                                                                                                                                                                                                                      |
| --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `dotnet restore Kofte.slnx --force-evaluate --verbosity quiet`                                                                                                                                                                                                                                                                                                  | Passed; generated affected locks.                                                                                                                                                                                                                                                                                                                    |
| `dotnet restore Kofte.slnx --locked-mode --verbosity quiet`                                                                                                                                                                                                                                                                                                     | Passed.                                                                                                                                                                                                                                                                                                                                              |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --verbosity quiet`                                                                                                                                                                                                                                                                                | Passed at the setup increment, before subsequent feature source additions.                                                                                                                                                                                                                                                                           |
| `npm run dependencies:check`                                                                                                                                                                                                                                                                                                                                    | Passed: 403 npm / 78 NuGet reviewed identities.                                                                                                                                                                                                                                                                                                      |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionConsumerHarnessTests' --filter-class '*CompositionFixtureTests' --minimum-expected-tests 34`                                                                                                                                             | 34 passed, zero skips; strict harness omission/tampering and owned SQLite selection-file fixtures. Not successful guide scenarios.                                                                                                                                                                                                                   |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-class '*CompositionDiagnosticTests' --filter-class '*ModuleSelectionTests' --filter-class '*CompositionBudgetTests' --minimum-expected-tests 48`                                                                                                                            | 48 passed, zero skips; immutable selection/private-file checks, protocol/privacy/telemetry/output faults and included deadline reserves.                                                                                                                                                                                                             |
| `dotnet test --project tests/unit/Kofte.Platform.Tests --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 1`                                                                                                                                                                                                                       | Final platform increment: 83 passed, zero skips; neutral contracts, resolver/graph/claims, exact count/byte limits and schema-field ordering. Initial increment had 58 passing cases.                                                                                                                                                                |
| `dotnet test --project tests/unit/Kofte.Platform.Tests --no-restore -c Release --minimum-expected-tests 1`                                                                                                                                                                                                                                                      | Final platform increment: 212 passed, zero skips; accepted platform regressions retained. Initial increment had 187 passing cases.                                                                                                                                                                                                                   |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionCorpus*' --minimum-expected-tests 2`                                                                                                                                                                                                     | 2 passed, zero skips; 21 independent expected fixtures, 100 repetitions x 10 discovery permutations x two actual cultures = 42,000 direct public resolver evaluations. Host-consumer path remains outstanding.                                                                                                                                       |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*SqliteCompositionPreflightTests' --minimum-expected-tests 5`                                                                                                                                                                                   | 5 passed, zero skips; real complete-inventory equality for new-owner candidate, invalid later target, missing provider recipe, changed declaration and serving preparation refusal.                                                                                                                                                                  |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*PostgresCompositionPreflightTests' --minimum-expected-tests 5`                                                                                                                                       | 5 passed, zero skips; same real-provider cases on fixture-owned PostgreSQL, with actual ownership and physical inventory checks.                                                                                                                                                                                                                     |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*ModuleMigrationTests' --filter-class '*AssignedModelTests' --filter-class '*StorageBoundaryTests' --minimum-expected-tests 1`                                                                        | 43 passed, zero skips; accepted both-provider migrations, assigned models and boundaries after extracting the non-authoritative model boundary.                                                                                                                                                                                                      |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*SqliteCompositionPreflightTests' --minimum-expected-tests 22`                                                                                                                                                                                  | 22 passed, zero skips; expanded exact snapshot comparisons for metadata, v0/unknown registry, no-storage changes, incomplete intents, foreign/missing/unowned targets and wrong models/history/operations.                                                                                                                                           |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*PostgresCompositionPreflightTests' --minimum-expected-tests 22`                                                                                                                                      | Same 22 cases passed on real fixture-owned PostgreSQL, zero skips.                                                                                                                                                                                                                                                                                   |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-method '*CurrentTargetAlias*' --filter-method '*NoStorageInV0*' --minimum-expected-tests 4`                                                                                                                  | 4 passed, zero skips; actual current-target alias and v0 no-storage serving verification issues only appropriate bindings and changes no registry/data/history.                                                                                                                                                                                      |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-method '*CurrentTargetAlias*' --filter-method '*ExistingPreparedModel*' --filter-method '*AppliedCheckpointModel*' --minimum-expected-tests 8`                                                               | 8 passed, zero skips; current and corrupt actual model/history plus applied-checkpoint cross-schema refusal before issuing any actual context.                                                                                                                                                                                                       |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-method '*CurrentTargetAlias*' --filter-method '*NoStorageInV0*' --filter-method '*UnknownRegistryFormat*' --filter-method '*LostReadAuthority*' --minimum-expected-tests 7`                                  | 7 passed, zero skips; same-session authority on both providers and actual owned PostgreSQL backend termination: no reacquisition, cleanup refuses success, complete physical state preserved.                                                                                                                                                        |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*CompositionPreflightTests' --minimum-expected-tests 51`                                                                                                                                              | 51 passed, zero skips. Final foundation matrix uses actual pre-existing Alpha/Beta clean/additive histories and distinct nonempty business-data sentinels; complete provider inventory/data/physical-state hashes remain identical after every relevant refusal.                                                                                     |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --minimum-expected-tests 1`                                                                                                                                                                                                                                                          | 122 passed, zero skips; all host unit behavior, including retained bootstrap configuration/health and the serialized authority watcher. Further foundation refinements remain separately in progress.                                                                                                                                                |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --minimum-expected-tests 1`                                                                                                                                                                                                                                                          | Final US2 combined gate: 144 passed, zero skips, including streaming selection admission, exact inclusive bounds, real callback attempt tracking and safe known-field guidance.                                                                                                                                                                      |
| `dotnet test --project tests/unit/Kofte.Platform.Tests --no-restore -c Release --minimum-expected-tests 1`                                                                                                                                                                                                                                                      | Final US2 combined gate: 212 passed, zero skips.                                                                                                                                                                                                                                                                                                     |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 1`                                                                                                                                                                 | Final US2 combined gate: 56 passed, zero skips, 68.794 seconds; strict harness/fixtures, frozen corpus, both-provider public scenarios, actual exit/deadline/output guards and safe field metadata.                                                                                                                                                  |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-method '*UnhealthyInitialSweepCannotPublish*' --minimum-expected-tests 3`                                                                                                                                                                                                   | Red regression: all 3 initial unhealthy states reproduced an incorrectly set internal publication latch before health admission.                                                                                                                                                                                                                     |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-class '*ModuleLifecycleTests' --filter-class '*CompositionBudgetTests' --minimum-expected-tests 1`                                                                                                                                                                          | T027 increment: 30 passed, zero skips after publication fix. Unknown/degraded/failed initial sweeps publish neither latch nor health; partial Initialize stops only entered owners once and does not construct a later owner; duplicate service ownership refuses before registrars. Existing phase/dependency/native-DI/cleanup tests remain green. |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-class '*ModuleLifecycleTests' --filter-class '*CompositionBudgetTests' --minimum-expected-tests 31`                                                                                                                                                                         | T027 final increment: 31 passed, zero skips; additionally verifies exact reverse owner Stop/LIFO/runtime disposal/scope disposal, with native host provider disposal last.                                                                                                                                                                           |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-method '*NativeConstructorInjectionCanEnumerate*' --minimum-expected-tests 1`                                                                                                                                                                                               | Red regression: owner-filtered native DI incorrectly refused an `IEnumerable` of the owner's own declared services.                                                                                                                                                                                                                                  |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-class '*ModuleLifecycleTests' --filter-class '*CompositionBudgetTests' --minimum-expected-tests 33`                                                                                                                                                                         | 33 passed, zero skips after allowing only owner-declared service enumerables. Constructor injection resolves both owned registrations, host enumerables remain unavailable, and a container runtime reached through its owned enumerable is disposed exactly once by its scope.                                                                      |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --minimum-expected-tests 152`                                                                                                                                                                                                                                                        | 152 passed, zero skips after the publication and native DI fixes. Focused whitespace formatting verification also passed for all three changed host/test files.                                                                                                                                                                                      |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-class '*StorageCompatibilityTests' --filter-class '*HealthEndpointTests' --minimum-expected-tests 1`                                                                                                            | Initial retained compatibility run: 13 passed / 1 failed / zero skips. The failed fixture connection reported PostgreSQL `57P03` (starting up), before application assertions.                                                                                                                                                                       |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-class '*PostgresStorageCompatibilityTests' --filter-method '*RefusedOwnedPostgresUpgradeProjectsSafeCauseAndPreservesMetadata*' --minimum-expected-tests 2`                                                     | Unchanged targeted rerun: 2 passed, zero skips.                                                                                                                                                                                                                                                                                                      |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-class '*StorageCompatibilityTests' --filter-class '*HealthEndpointTests' --minimum-expected-tests 14`                                                                                                           | 14 passed, zero skips after correcting the owned test image healthcheck to probe TCP instead of the entrypoint's temporary Unix-socket-only initialization server. Retained v0/v1, safe upgrade refusals and anonymous health pass on both providers.                                                                                                |
| `./node_modules/.bin/prettier --check deploy/docker/compose.test.yml`                                                                                                                                                                                                                                                                                           | Passed after the fixture readiness correction.                                                                                                                                                                                                                                                                                                       |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --minimum-expected-tests 154`                                                                                                                                                                                                                                                        | 154 passed, zero skips after preserving selected-but-missing canonical identities only on rejected deployment-boundary records. Forged activation/unknown-owner records remain refused.                                                                                                                                                              |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 77`                                                                                                                                                                | 77 passed, zero skips, 97.897 seconds. All five delivered scenarios and strict harness/probe/worker guards pass; both-provider disabled/missing regressions now retain the actual canonical missing identity.                                                                                                                                        |
| `dotnet restore Kofte.slnx --force-evaluate --verbosity quiet` then `dotnet restore Kofte.slnx --locked-mode --verbosity quiet`                                                                                                                                                                                                                                 | Passed after adding the test-only storage-integration build edge to the composition consumer; no package identity/version added.                                                                                                                                                                                                                     |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-method '*ConfiguredServingOutlivesMaintenanceCeiling*' --minimum-expected-tests 1`                                                                                                                                                                        | 1 passed, zero skips, 121.998 seconds. Actual configured serving stayed ready beyond 120 seconds, stopped within its own 15-second reserve with a passed terminal record, and preserved the platform file exactly. An initial compile error from an incorrect existing initializer overload was corrected before this run.                           |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*CompositionProviderTests' --minimum-expected-tests 6`                                                                                                                                                | 6 passed, zero skips: actual empty/dependency/disabled-missing boots on both providers, with measured public behavior and full inventory comparisons.                                                                                                                                                                                                |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionConsumerHarnessTests' --filter-class '*CompositionFixtureTests' --minimum-expected-tests 34`                                                                                                                                             | 34 passed after extracting the unchanged strict public result harness into shared test support for contract and integration consumers.                                                                                                                                                                                                               |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-class '*StorageCompatibilityTests' --filter-class '*OpenApiTests' --minimum-expected-tests 1`                                                                                                                   | 16 passed, zero skips; v0/v1 empty hosts retain exact anonymous health fields and the complete normalized Development OpenAPI baseline on both providers.                                                                                                                                                                                            |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario valid-dependency`                                                                                                                                                                                                                         | Passed: 35 assertions; 3 real public service comparisons, exact phase order, 3 no-assignment/history comparisons and actual prepublication 503.                                                                                                                                                                                                      |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario disabled-missing`                                                                                                                                                                             | Passed: 14 assertions; zero disabled callbacks, exact missing-owner refusal and 2 unchanged complete inventories.                                                                                                                                                                                                                                    |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionPublicContractTests' --minimum-expected-tests 1`                                                                                                                                               | Initial public validation increment: 10 passed, zero skips, including both providers, real admitted validate and worker/CLI refusal. Complete validate deadline/output/exit-category refinements remain in progress.                                                                                                                                 |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionPublicContractTests' --filter-method '*ValidateBoundary*' --filter-method '*IndependentConsumerRejectsInvalidCompositionWithoutProviderEffects*' --minimum-expected-tests 1 --output Detailed` | Follow-up validation boundary increment: 12 passed, zero skips, 54.408 seconds. Actual both-provider command exits, ignoring-token validator, failed required private output and original snapshot transport passed against nonempty pre-existing v1 registry state.                                                                                 |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionPublicContractTests' --filter-method '*IndependentConsumerRejectsInvalidCompositionWithoutProviderEffects*' --minimum-expected-tests 1 --output Detailed`                                      | Settings-record follow-up: 2 passed, zero skips, 22.910 seconds; 747 assertions/provider. Known owner/field token/reason are exact; unknown root keys and ordinary records expose null/null. The 3/5/4/4/4 refusal groups and 29 nonempty-v1 inventory comparisons remain unchanged.                                                                 |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario invalid-composition`                                                                                                                                                                                                                      | Initial public increment passed: 614 assertions; 3 manifest, 5 dependency, 4 settings, 4 claim and 4 limit refusals; 29 full inventory comparisons, distinct attempts and verified exits.                                                                                                                                                            |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario invalid-composition`                                                                                                                                                                          | Same initial 614-assertion public increment passed against owned PostgreSQL.                                                                                                                                                                                                                                                                         |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario deterministic-corpus`                                                                                                                                                                                                                     | Initial public increment passed: 387,984 assertions; 84,000 exact decisions, 42,000 direct-author and 42,000 actual public host-preflight decisions across 21 frozen fixtures x 100 repetitions x 10 permutations x two real cultures.                                                                                                               |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario deterministic-corpus`                                                                                                                                                                         | Same initial 387,984-assertion, 84,000-decision public increment passed against owned PostgreSQL.                                                                                                                                                                                                                                                    |
| `python3 scripts/check_repository.py`                                                                                                                                                                                                                                                                                                                           | Passed at the foundation increment.                                                                                                                                                                                                                                                                                                                  |
| `python3 -m unittest discover -s scripts/tests -v`                                                                                                                                                                                                                                                                                                              | 111 passed at the foundation increment.                                                                                                                                                                                                                                                                                                              |
| `git diff --check`                                                                                                                                                                                                                                                                                                                                              | Passed at the foundation increment.                                                                                                                                                                                                                                                                                                                  |

The initial resolver scaffold produced seven behavioral failures before the
implemented resolver passed. The early-cleanup budget regression failed before
fixing reserve clipping; cleanup no longer borrows unused startup work time.
The preflight tests first failed to compile because the infrastructure APIs did
not exist, then passed on both real providers. Intermediate compiler/platform
guard errors were corrected rather than suppressed. One earlier command used a
minimum of 42 with only 41 selected passing cases and therefore failed its
minimum policy; the later 48-case combined run is the passing evidence.

Infrastructure structural preflight now uses a separate host-issued
`ModuleStorageModelBoundary`, without a `StorageAssignment`, binding, store
opener, credentials or physical file accessor. Assigned contexts, snapshots and
migrations reuse that model boundary; an unassigned context's `Storage` getter
refuses. The original binding-based context API remains available and the
accepted 43-case provider regressions passed. Complete stopped/read inventory
inspection reuses accepted ownership/history checks, and no registry upgrade,
repair or allocation is attempted by inspection.

The verified foundation and US2 markers now cover T001-T026. The combined
144-host/212-platform/56-public gate and the 51 real-provider preflight cases
satisfy the validation increment, not later lifecycle or acceptance gates.
The final platform increment also reproduced two failures for schema-field
ordering and missing internal fingerprint before correction. Count 64/65 and
serialized byte 65,536/65,537 cases prove the reserved truncation marker and
maximal prefix. The 1,025-edge and aggregate-byte overflows necessarily violate
upstream caps; no impossible accepted composition is claimed.

The shared frozen corpus now lives in the separate test-owned author library;
the contract theory and independently compiled consumer use the same literal
expected decisions, not a second resolver as a host-path proxy. Typed host
rejections retain exact ordered nested diagnostics. Only `invalid-composition`
and `deterministic-corpus` currently succeed; nine guide scenarios and `all`
remain explicitly unimplemented. The remaining host validation-boundary gaps
were closed before marking T024-T026 complete.

### Actual validate boundary follow-up

The independently compiled consumer exercised the same admitted executable on
both real providers. Each guard verifies actual root exit and the native
owned-group terminal proof, one admission without retry, zero runtime phases,
an unchanged complete provider snapshot and completion inside 45 seconds.
Raw stdout/stderr canaries were drained rather than projected.
The final settings-record increment additionally checks exact compiled
owner/field-token/reason metadata; unknown root settings and ordinary records
carry null field metadata rather than reflecting raw keys.

| Boundary                           | Actual exit / fixed code                  | SQLite elapsed ms | PostgreSQL elapsed ms |
| ---------------------------------- | ----------------------------------------- | ----------------- | --------------------- |
| Valid composition                  | 0 / `composition_progress`                | 214               | 441                   |
| Invalid manifest                   | 2 / `composition_manifest_rejected`       | 178               | 200                   |
| Held maintenance authority         | 3 / `composition_storage_rejected`        | 233               | 301                   |
| Owned orphan target                | 4 / `composition_reconciliation_required` | 213               | 525                   |
| Validator ignoring cancellation    | 1 / `composition_cleanup_incomplete`      | 441               | 436                   |
| Broken required private event sink | 1 / `composition_output_failed`           | 224               | 185                   |
| Original snapshot transport        | 0 / `composition_progress`                | 366               | 416                   |

Before its snapshots, the invalid-composition scenario now explicitly upgrades
through the accepted API and registers an unrelated no-storage owner under
exclusive authority. Its 29 unchanged full-inventory comparisons therefore
preserve a nonempty v1 registry, not only an empty installation. Fixture
selectors and targets remain fixed and owned; no arbitrary path, type, SQL,
credential or PID option was introduced.

Foundation mapping: `ModuleSelectionTests` and `CompositionPreflightTests`
cover strict pre-DOM admission, exact byte/entry/depth bounds and immutable
original settings transport (T008-T009). `CompositionDiagnosticTests` plus
actual public output-failure guards cover framed records, terminal reserve,
rate limits, private output and bounded telemetry (T010-T011).
`CompositionBudgetTests` and real resource-registration tests cover included
reserves, phase/outcome/entered-completed sets and 32/33 plus 512/513 cleanup
admission (T012). `ModuleConfigurationTests`, preflight tests and the actual
ignoring-token validator cover owner-only rules, no environment fallback,
two-field constraints and supervised validation (T017/T022). Sealed platform
field/token/path bounds are inherited rather than reimplemented.

T012 proves clipped budget primitives; healthy serving beyond 120 seconds and
the retained whole-stack 285/300-second execution are later T032/T076 gates,
not new elapsed-time evidence here. Per-owner storage-stage tracking awaits
real US3 callbacks and is not fabricated from whole-batch completion.

### US1 public boot increment

T028-T036 now deliver separate Alpha/Beta/no-storage author implementations and
three actual public boot scenarios. Measured results are Alpha 22, Beta 55
through the exact versioned Alpha binding, and no-storage 8. Constructors,
registrars, phase order, initial-health/publication barrier and reverse cleanup
are observed through a fixture-owned private Unix socket, not child console
text or anonymous business routes. The protocol admits fixed owners/phases,
512-byte frames, 256 events, actual worker/attempt identity and verified EOF.
Disabled callbacks produce measured zero totals.

The initial public boot increment passed 55/57 selected cases; its two actual
missing-owner failures reproduced host over-sanitization. The host now preserves
only canonical selected missing IDs at their rejected deployment boundary;
they cannot masquerade as deployed owners during activation or other records.
Two obsolete assertions that boot scenarios remained unimplemented were removed.
The final 77-case public gate passes.

The strict scenario result parser now lives in
`tests/integration/Kofte.TestSupport/CompositionConsumerHarness.cs`, reused by
contract and integration tests instead of introducing a second weaker parser.
The Release consumer has a build-only reference from storage integration; no
production project references a test assembly. The initial formatting check
identified one initializer-layout issue; the existing formatter corrected it.
The three boot examples currently use no relational module stores; exact
history/assignment absence is not substituted for US3's relational evidence.
At that checkpoint, six future scenarios and `all` still failed explicitly.

### US3 preparation, current serving and retained authority increment

T037-T045 now exercise separately compiled Alpha/Beta provider contexts and
initial/additive migrations through actual `validate`, stopped `prepare` and
ordinary serving commands. The public `ordered-migration` example passes on
both providers with 65 assertions each: two exact clean histories, two additive
histories, two original-record comparisons, complete later-invalid-recipe
preflight, physical no-storage absence and two unchanged serving revisions.
Initial serving, current serving and already-current preparation preserve the
entire provider inventory; only explicit additive preparation advances the
owning migration revision. Preparation runs every Initialize before any owner
migration and executes no Start, health or business callback.

Real per-owner infrastructure progress now drives the attempt's entered and
completed migration sets. A later failed factory never completes its owner or
the whole barrier; actual accepted storage results retain their original
correlation and durability. Batch completion alone still invents no owner
progress. Independently parsed canonical IDs reproduced a real defect:
reference comparison skipped recipe inspection and then registered the first
owner before recipe lookup failed. Inspection, preparation and verification
now compare immutable ID values. Both-provider regression cases use distinct
ID instances and reject the later invalid target before any write.

T039 also proves actual read/maintenance contention, bounded PostgreSQL serving
lease loss without reacquisition, a separately killed Beta migration after
Alpha committed, exact retained unrelated business data and explicit accepted
003 recovery. A fresh prepare before recovery refuses with unchanged complete
inventory. A further supervised public-worker interruption case passes on both
providers, retaining Alpha's committed progress and Beta's recovery intent.
Its test-runner execution exposed a relative Unix socket working-directory
assumption; fixture and child now resolve the same private socket without
changing global working directories or using an overlong absolute endpoint.
Complete-set legacy guards and the complete public correction scenario remain
T046-T047 work, not inferred from those interruption cases.

All following commands used the toolchain exports above and
`KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`:

| Exact command                                                                                                                                                                                                                                                                                                                     | Observed result                                                                                                                                                                           |
| --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj --no-build --no-restore -- --filter-class '*CompositionMigrationTests' --filter-class '*CompositionRecipeTests' --minimum-expected-tests 1`                                                                         | 20 passed, zero skips; author component's real clean/additive/recipe matrix, 41.273 seconds. Initial compiled-stub run had eight failures and two passes.                                 |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*CompositionAuthorityTests' --minimum-expected-tests 5`                                                                                                                                                           | Five actual contention/serving-lease-loss/fresh-retry cases passed, zero skips, 22.785 seconds.                                                                                           |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-method '*KilledLaterMigrationRetainsEarlierCommitAndOriginalDataUntilExplicitAcceptedRecovery' --minimum-expected-tests 2`                                                                                               | Both killed-owner/retained-data/explicit-recovery cases passed, zero skips, 25.951 seconds.                                                                                               |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*CompositionProgressTests' --minimum-expected-tests 4`                                                                                                                                                            | Four real-provider progress/failure-evidence cases passed, zero skips, 13.285 seconds, before adding the independently parsed ID regression.                                              |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-class '*ModuleLifecycleTests' --minimum-expected-tests 1`                                                                                                                                                                                     | 28 passed, zero skips, including partial-owner migration barrier refusal.                                                                                                                 |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionMigrationContractTests' --minimum-expected-tests 2`                                                                                                                                                        | Both actual public relational scenarios passed, zero skips, 37.987 seconds; the initial run failed both as explicitly unimplemented.                                                      |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*CompositionProgressTests' --filter-class '*CompositionPreflightTests' --minimum-expected-tests 55`                                                                                                               | 54 passed, one failed, zero skips. The failure was an Npgsql timeout while provisioning the owned fixture, before the applied-checkpoint assertion; no product timeout/retry was relaxed. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-method '*AppliedCheckpointModelCannotReachAnotherSchemaOrIssueAnActualContext' --filter-method '*SupervisedPreparationInterruptsOnlyLaterOwnerAndRequiresExplicitRecoveryBeforeFreshProcess' --minimum-expected-tests 4` | The exact previous checkpoint case passed on both providers; both new supervised cases failed on the fixture socket working-directory defect.                                             |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*CompositionRecoveryTests' --minimum-expected-tests 2`                                                                                                                                                            | After the socket fix, both actual supervised-interruption/recovery cases passed, zero skips, 13.267 seconds.                                                                              |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario ordered-migration`                                                                                                                                                                                          | Passed: 65 assertions with every required named comparison.                                                                                                                               |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario ordered-migration`                                                                                                                                                                                        | Passed: the same 65 real-provider assertions; disposable PostgreSQL explicitly enabled.                                                                                                   |

### US3 final focused gate and complete-set recovery

T046-T049 now have executed evidence. The public additive example interrupts
the actual supervised preparation at Beta's committed migration intent, after
Alpha's current migration commits. It verifies retained original identities,
rows and Alpha history/correlation, refuses a fresh automatic retry unchanged,
then explicitly uses accepted 003 recovery before a new preparation process.
The no-storage example and ordinary current serving remain mutation-free.

Both-provider recovery cases prepare original records, disable both relational
owners, compare the entire stopped inventory, create an actual complete backup,
refuse legacy platform-only operations and deliberately select that older set
after a newer source write. Restore uses a fresh owned target; the restored old
set matches its manifest/data/history and the newer source remains untouched.
PostgreSQL uses real dump/restore and retained runtime privilege checks.

With the same pinned SDK and explicit disposable-PostgreSQL opt-in used above:

| Exact command                                                                                                                                                                                                                                                                   | Observed result                                                                                                                                   |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*CompositionRecoveryTests' --minimum-expected-tests 4`                                                                                                          | Four passed, zero skipped, 29.912 seconds: supervised interruption and complete-set/disabled-owner/legacy guards on both providers.               |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*Composition*' --filter-class '*ModuleMigrationTests' --filter-class '*AssignedModelTests' --filter-class '*StorageBoundaryTests' --minimum-expected-tests 135` | 135 passed, zero skipped, 224.460 seconds. Includes all new composition storage cases and retained assigned-model/migration/ownership boundaries. |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --minimum-expected-tests 155`                                                                                                                                                                        | 155 passed, zero skipped, 1.328 seconds.                                                                                                          |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 77`                                                                                                                          | 77 passed, zero skipped, 148.786 seconds.                                                                                                         |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario ordered-migration`                                                                                                                                        | Passed with 80 assertions, including the interruption/recovery enhancement.                                                                       |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario ordered-migration`                                                                                                                                      | Passed with the same 80 real-provider assertions; disposable PostgreSQL explicitly enabled.                                                       |

An earlier broad invocation used `--filter-class '*Composition*Tests'` and
failed before executing tests with MTP handshake/zero-test exit 5; none of its
chained host/public checks ran. No dependency installation or minimum reduction
was used. The unchanged narrow recovery selector then passed; the documented
`'*Composition*'` selector and all three gates above passed. The initial runner
failure's underlying cause is not established.

The docs distinguish explicit registry upgrade and stopped preparation from
ordinary serving, repeated ephemeral Initialize, denied runtime CREATE and
forward/full-set recovery. They do not promise cross-owner rollback or implicit
reconciliation. Remaining stories and final gates are still incomplete.

### US4 exact preservation, disabled selection and cleanup

T050-T057 are verified. `CompositionPreservationTests` runs the independent
public `disable-reenable` path, not another synthetic loop. Each provider
prepares both real stores and distinct original records, then performs three
complete disabled/re-enabled cycles with edits only while stopped. Six distinct
root PIDs, six distinct actual worker PIDs and six attempt IDs are required.
Every leg compares two original immutable assignment identities/revisions,
two exact row sets, two exact histories and the entire installation snapshot.
Disabled owner records must contain exactly the six explicit zero totals and
no callback. Reverse Stop/cleanup/dispose and actual root exit are checked.
The invalid dependent selection refuses before mutating stages and leaves the
entire original installation unchanged.

The existing `ModuleLifecycleTests` are reused instead of a parallel
`CompositionStopTests` file: they verify reverse partial Initialize/Start entry,
LIFO cleanup, owner scopes/provider-last, 32/512 inclusive registration limits,
33/513 refusal and container-owned runtime at-most-once disposal. A new failed
Stop case proves remaining cleanup still runs exactly once and readiness stays
withdrawn. A new observation case proves disabled owners remain explicit and a
new re-enabled attempt cannot reuse prior healthy samples. Production resources
and coordination remain consolidated in `ModuleLifecycleCoordinator`, rather
than adding the plan's separate coordinator/resources placeholder files.
No production change was necessary for the preservation behavior.

`CompositionSelectionTests` proves both-provider dependent-disable refusal.
The existing `ChangedPersistedRelationalDeclarationIsNotTreatedAsNewOwner` and
`PersistedNoStorageDeclarationCannotBecomeRelational` cases already revalidate
changed declarations and passed in the 135-case US3 gate.
`CompositionUpgradePreservationTests` separately prepares only Alpha's additive
release: Beta's original full allocation/history/revision/data and no-storage
state remain exact. Only Alpha's target and its two migration revision advances
change; repeat prepare and read verification leave the complete snapshot exact.

With the same pinned SDK and explicit disposable-PostgreSQL opt-in:

| Exact command                                                                                                                                                                                                                                                                | Observed result                                                                                                                                                                             |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*CompositionPreservationTests' --filter-class '*CompositionSelectionTests' --filter-class '*CompositionUpgradePreservationTests' --minimum-expected-tests 6` | Six passed, zero skipped, 19.643 seconds.                                                                                                                                                   |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-class '*CompositionObservationTests' --filter-class '*ModuleLifecycleTests' --minimum-expected-tests 34`                                                                                 | 34 passed, zero skipped, 0.499 seconds, including fresh-attempt and failed-Stop cases.                                                                                                      |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --minimum-expected-tests 157`                                                                                                                                                                     | Full host suite: 157 passed, zero skipped, 1.304 seconds.                                                                                                                                   |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionMigrationContractTests' --minimum-expected-tests 4`                                                                                                   | Both migration/recovery and both preservation public cases passed, zero skipped, 46.870 seconds.                                                                                            |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario disable-reenable`                                                                                                                                      | Passed: 131 assertions, 3 complete cycles, 6 distinct processes/attempts, 12 original assignment/data/history comparisons each, 6 unrelated-state comparisons, 1 invalid dependent refusal. |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario disable-reenable`                                                                                                                                    | Passed with the same 131 assertions and exact measured observations on real owned PostgreSQL.                                                                                               |

The initial preservation cases failed while the scenario was explicitly
unimplemented. Initial selection tests incorrectly requested an author socket
EOF before any admitted registrar existed; the probe deliberately releases only
after a host-issued attempt exists. They now use actual command records/exits
and full-state comparison for the preflight refusal, not a nonexistent callback
trace. Initial preservation assertion also mistook deliberate zero-total
observations for callbacks; it now requires the exact six zero totals per
disabled owner and no real callback. These fixture/assertion errors did not
change provider semantics or relax the original evidence requirements.

### US5 health, failures and private observations

The operator snapshot now retains immutable per-owner lifecycle outcomes rather
than reporting every enabled owner completed. The initial own unhealthy cases
reproduced that defect. Healthy dependents are blocked by unhealthy transitive
prerequisites; independent roots remain distinguishable. Publication derives and
checks health inside the compare/exchange update: the concurrent failed-sample
regression previously reproduced publication of an older healthy snapshot.
The coordinator's injected clock proves serialized sweeps and the ten-second
post-sweep delay. Actual Kestrel responses retain the anonymous four-field
schema and never execute author health callbacks; freshness examples explicitly
advance a monotonic fixture clock rather than claiming a real thirty-second wait.

The independent fault catalog executes all seven ignoring-token callbacks,
pre-work/during-Initialize cancellation, cleanup overflow/failure and actual
worker-owned descendant creation. Timeout exceptions retain the actual owner
and work boundary; the worker emits the original deadline before incomplete
cleanup and exits. The supervisor preserves a verified matching child failure
instead of masking it with requested cancellation. Actual root/descendant exit
and a separate deliberately unverified-exit refusal remain distinct evidence.
The PostgreSQL public example now also terminates the exact owned serving
advisory-lock session, not merely a contention case. Its helper is shared with
the existing actual-backend-loss integration test and accepts only an owned
fixture, never arbitrary connection/PID inputs.

The four ephemeral correction pairs change only the named configuration or
fixed deployment fault, preserve the entire installation and require fresh
attempt/root identities. The fifth uses actual interrupted migration and
explicit accepted recovery, retaining the earlier owner commit. Both provider
correction cases passed. Neither ordinary retry nor health creates authority.

`private-diagnostics` installs real Activity/Meter listeners and stdout/stderr
capture around the public supervised command in its isolated consumer process.
It exercises successful preparation/serving, all seven stuck boundaries,
Initialize/Start/health/cleanup faults, unknown settings, invalid identity,
missing prerequisite, held authority, invalid recipe, missing admission and
broken required worker-event output. It captures actual anonymous HTTP replies,
original 003 correlation/durability, safe transition events and fixed metric
labels. Malformed/oversized/wrong-attempt frames and 256-attempt/64-steady limits
use the same public protocol/diagnostic APIs. These are not claims to capture
arbitrary trusted plugin side channels or sandbox its process.

This exposed a missing dependency's canonical diagnostic identity being
silently filtered out. Validated manifest prerequisite IDs are now allowed only
as related IDs on rejected missing-dependency records, never as runtime owners
or health dependencies. Its regression proves copied allowlists and forged
activation/health refusal. A separate red test reproduced absent transition
events; each successful required write now emits one event with only the fixed
code/guidance/outcome. Caller baggage/tracestate and all seeded setting,
credential, unknown-key and physical-path canaries are absent from captures.

New failure-stage projection also exposed duplicate publication after serving
authority loss. A post-publication failure now withdraws readiness and records
stopping, not a second publication. The unchanged actual-backend-loss assertion
then passed with truthful incomplete cleanup and no reacquisition.

All commands use the pinned SDK above and
`KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`:

| Exact command                                                                                                                                                                                                    | Observed result                                                                                                                                                                                                                                             |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionFailureContractTests' --minimum-expected-tests 4`                                         | Three passed / one SQLite bounded-stop failure; both five-example correction cases and PostgreSQL bounded-stop passed at that increment. Not a passing combined gate.                                                                                       |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build --no-restore -c Release -- --provider sqlite --scenario private-diagnostics`                                                          | Passed: 67 assertions, six captured channels, six zero-leak comparisons, three invalid frame refusals, two output-limit refusals, actual broken sink and original storage durability.                                                                       |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-method '*ActualServingSessionLoss*' --minimum-expected-tests 1`                                         | Passed, zero skips, 10.176 seconds after the post-publication failure fix.                                                                                                                                                                                  |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*CompositionPrivacyTests' --filter-class '*CompositionDeadlineTests' --minimum-expected-tests 6` | Five passed / PostgreSQL bounded-stop fixture timed out. Both actual privacy cases and both corrections passed. The new public lease-loss probe incorrectly awaited live output from a nonobserving process; it now uses the existing observed boot helper. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-method '*PublicFaultMatrix*' --minimum-expected-tests 2`                                                | PostgreSQL passed; SQLite exposed an intermittent busy refusal with exit 1 instead of required 3. Under investigation, not hidden as successful contention.                                                                                                 |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build --no-restore -c Release -- --provider sqlite --scenario bounded-stop`                                                                 | Subsequent standalone passed: 96 assertions, all seven fault boundaries, two cancellations, two authority refusals, seven root exits and one actual descendant exit. This does not resolve the intermittent busy failure by itself.                         |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --minimum-expected-tests 166`                                                                                                         | 166 passed, zero skips, 10.552 seconds after isolating global telemetry listeners. An earlier full run reproduced concurrent mutation of the old diagnostic test's List; its test collection now excludes parallel users of the global listeners.           |
| `dotnet test --project tests/unit/Kofte.Platform.Tests --no-restore -c Release --minimum-expected-tests 212`                                                                                                     | 212 passed, zero skips, 2.555 seconds.                                                                                                                                                                                                                      |

The combined use of method and class selectors selected zero tests (exit 8);
the explicit minimum rejected it. Subsequent commands separate these selectors
instead of reducing minima. The broad public run discovered 84 cases, with
83 passing and the intermittent SQLite bounded-stop failing; both actual
`all` cases passed. Its guessed minimum of 85 also failed. Neither result is
reported as a passing combined gate. The named verifier now requires the
observed 84-case both-provider inventory, while preserving earlier minima.
Provider-specific contract classes carry actual Provider traits; filtering a
SQLite-only invocation no longer relies on untagged theory data.

Three real acquisition attempts per retained authority reproduced the SQLite
defect: read-held preparation returned incomplete instead of busy. The host's
two-second acquisition callback raced the native two-second acquisition budget;
the latter's 25-millisecond margin did not reliably leave time to return its
busy result. Composition preparation now requests an internal 1,500-millisecond
acquisition budget, retaining 500 milliseconds for classification. The public
003 acquisition API and host's two-second limit are unchanged; no deadline
was extended. The same repeated contention and public fault matrix then passed:

| Exact command                                                                                                                                                                                                          | Observed result                                                                                                                      |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-method '*ActualCommandsRefuseIncompatible*' --filter-method '*PublicFaultMatrix*' --minimum-expected-tests 6` | Six passed, zero skipped, 32.761 seconds, including both providers' retained-authority repetitions and actual public fault matrices. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*CompositionPrivacyTests' --minimum-expected-tests 2`                                                  | Both passed, zero skipped, 25.885 seconds, with strengthened exact outcome/guidance and fixed-enum label assertions.                 |

T058-T072 now have component/public evidence. Final named regression gates,
retained direct-guide reports, the full local gate and implementation-head CI
remain separate obligations.

### Final verifier wiring and retained gates (in progress)

The verifier inventories the separate test-owned assemblies and all eleven
scenarios, builds them through the solution, and runs focused composition
unit/integration/recovery/privacy/public selectors alongside retained suites.
Every standalone and provider `all` command gets separate command/observation
steps. An independent strict bounded parser rejects missing/decremented
observations, missing cases, wrong providers, booleans/overflow, duplicate fields,
stderr and mismatched assertion totals. Reports retain only sanitized counts
and cases, never raw consumer output.

`python3 -m unittest scripts.tests.test_verify -q` passed 45 tests at the initial
wiring increment. `python3 scripts/check_repository.py` and `git diff --check`
passed. The initial named quality gate passed inventory, pinned versions,
repository/Python checks, whitespace and locked restore, then failed the full
.NET formatter on new files. `dotnet format whitespace Kofte.slnx --no-restore
--verbosity quiet` followed by `dotnet format Kofte.slnx --verify-no-changes
--no-restore --verbosity quiet` passed. The complete named gate still requires
a rerun; no failed/unfinished gate is counted as passing.

The subsequent quality run reached Prettier and correctly rejected five changed
NuGet locks. Formatting those generated JSON files without changing dependency
content resolved it. `python3 scripts/verify.py --suite quality` then passed
all 20 steps, and `python3 scripts/verify.py --suite unit` passed all 14.
Those results precede the cancellation refinement below and are not a claim
that its final complete gate has passed.

The named SQLite integration gate passed retained host/storage integration but
its focused composition step exposed intermittent normal-stop failures in
public boot/preservation/privacy. Standalone examples passed; repeated focused
runs did not reliably pass, so the failures were investigated rather than
treated as successful retries. Safe public failure output was added to the
integration wrappers. The measured focused inventory is 51 cases; the required
minimum remains 50.

Two deterministic regressions reproduced one production race: cancelling the
wait classified a cooperative callback as abandoned before its asynchronous
cancellation response completed. The callback runner now drains cancellation
only inside the original, separately timed callback deadline. It still refuses
and requires owned termination for a callback left running at that deadline;
no callback/outer deadline is extended and cleanup cannot overlap abandoned work.
The test fixture also prematurely killed supervised composition roots after its
generic five-second grace. Composition now selects its documented fifteen-second
stop window; other fixtures keep their unchanged five-second default.

| Exact command                                                                                                                                                                                              | Observed result                                                                                                                                                                                                                          |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-method '*CancellationDrainsCooperativeWork*' --filter-method '*CancellationDoesNotExtend*' --minimum-expected-tests 3` | Before the production fix: two cooperative-drain regressions failed; the uncooperative deadline case passed. An initial test compile correctly enforced xUnit test-cancellation rules, which were corrected without disabling analyzers. |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-class '*CompositionBudgetTests' --filter-class '*ModuleLifecycleTests' --minimum-expected-tests 37`                    | After the production fix: 39 passed, zero skipped, 10.681 seconds.                                                                                                                                                                       |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-trait Provider=sqlite --filter-class '*Composition*' --minimum-expected-tests 50`                 | After both fixes: 51 passed, zero skipped, 63.193 seconds. Earlier combined runs failed with incomplete cleanup or fixture-forced exit 137; they are not passing gates.                                                                  |

The final named provider/public/recovery/privacy and complete local/CI gates
remain pending. T073-T080 are not marked complete from these partial results.
After the cancellation and fixture refinements, the complete named quality
(20 steps) and unit (14 steps) suites passed again. This checkpoint remains
an in-progress implementation, not a completed stage or accepted feature.

## Acceptance evidence

T027 reuses `ModuleLifecycleTests.cs` rather than creating a second lifecycle
test class. Its initial-health regression failed for all three nonhealthy
states before the fix. Attempt publication now invokes the final health
admission after deadline/mode checks and sets its latch only when admission
succeeds. Public boot and later story evidence remain separate obligations.

All 20 functional requirements, 20 story scenarios, six outcomes, R1-R12
design decisions and twelve constitutional principles remain implementation
obligations. Their exact source/assertion/run mapping will be recorded as work
is verified. Unfinished tasks remain unchecked; a partial increment does not
complete implementation or authorize convergence.

## Limitations and handoff

Implementation is not yet complete. Convergence, final review and explicit
human acceptance are separate later stages on this same branch/draft PR.
