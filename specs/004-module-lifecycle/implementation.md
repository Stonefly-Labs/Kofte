# Implementation: 004 - Module Composition and Lifecycle

**Stage:** implement complete, started 2026-10-24; not feature acceptance.
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

### Final verifier wiring and retained gates

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

Checkpoint `79d62aee64c2a88d29161fa8a21f9b753815cccf` was committed and
immediately pushed to the same draft PR. Its actual Ubuntu CI passed repository,
quality, unit, both-provider integration, browser, startup and failure
propagation. Recovery/privacy failed their new focused selectors: xUnit rejects
mixing simple class filters with its query-filter format. A direct invocation
reproduced zero tests/nonzero exit, and the official
[query-filter documentation](https://xunit.net/docs/query-filter-language)
confirms the restriction. Structural wildcard queries also discovered zero
cases in the installed adapter; this was not treated as a successful fix.
The focused suites now use explicit inherited `Feature=composition` traits
with the existing supported provider/suite trait query, without changing
retained selectors or minima. Actual SQLite native privacy selected/passed one
case (18.564 seconds), and public recovery selected/passed four (57.443 seconds).
The 45 verifier regressions passed; exact safe query arguments remain reportable.

The local named SQLite integration gate subsequently passed all nine steps.
The PostgreSQL host integration step passed, but the complete storage step
failed multiple new and retained cases after 451.505 seconds. This is not a
passing PostgreSQL/local-all result. The exact-head Ubuntu PostgreSQL gate did
pass; local failures still require investigation and an honest complete gate.

The final documentation/interface audit identified an evidence gap: retained
warm starts used the empty host, while configured boot was measured only in
the public host examples. The startup suite now additionally boots the real
compiled Alpha/Beta/no-storage deployment with the built web application for
two fresh controlled restarts. It retains the ten original empty-host cycles,
exact unrelated stored fixture, 285-second work/300-second total and two-second
usable-page limits. Private selection is owner-only, ambient test selectors are
stripped, and composition receives its full fifteen-second graceful stop window.
Separate fresh reports require two actual configured observations, including
elapsed startup/page/cleanup total; missing/oversized/invalid observations fail.
Each configured cycle additionally requires actual passed Start records for
all three exact owners and the successful startup publication record before its
report may set `modulesPublished: true`; a merely healthy empty host cannot
satisfy this evidence. Partial/invalid/private output and omitted owner/start/
publication cases have focused regressions.
`python3 scripts/verify.py --suite startup --warm-starts 10` passed all twelve
steps, including both new configured cycles. Full Python tests passed 117;
e2e ESLint/typecheck and formatting passed. No new production behavior, interface,
package or budget was introduced.
The focused Playwright regression also passes for legitimate transient
composition/host 503 before publication; malformed/storage 503 and the original
empty-host refusal remain failures. Final named quality passed all twenty
steps, and startup passed all twelve again after that refinement.

The active diagnostic all-suite run uses the pinned SDK plus
`DOTNET_PROCESSOR_COUNT=4` and `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`.
The workstation still has eighteen logical CPUs/64 GiB, while Docker reports
eighteen CPUs/about 15.7 GiB. The explicit .NET processor-count override bounds
test concurrency on this shared host; it changes no test floor, scenario,
provider, production deadline or cleanup reserve. It is recorded configuration,
not proof of the cause of the earlier broad PostgreSQL failure. That run began
at `720f398`; the subsequent configured-stack verifier addition requires a final
complete run after publication, not relabeling this diagnostic run exact-head.

T077 audit: production project references remain host → infrastructure/platform
and infrastructure → platform, without test projects, linked test source or a
directory loader. The author module project references only public platform/
infrastructure contracts; its executable/fixture projects remain under `tests/`.
`CompositionSupervisor.Start` removes the desired-file path after snapshot
transport and strips PostgreSQL provisioning credentials except in preparation;
the worker independently rejects them in validate/serve. Contracts/guide agree
on private operator authority, actual lifecycle/health bounds, read-only serving,
no-storage absence, explicit owner recovery and unchanged anonymous health.
Runtime stores, sockets, credentials, build/browser outputs and verification
reports remain ignored. Approved spec/plan/research/analysis and both reviewed
checklists are unchanged; only delivered guide/evidence/current-status docs may
change. Final progress statements still await actual complete-gate results.

Subsequent actual `720f398` CI recovery and privacy gates passed, including
both real providers and every focused native/public composition selector.
Its failure-propagation job did not pass: its isolated PostgreSQL baseline
reported one failed new native case,
`PostgresCompositionDeadlineTests.PublicCorrectionsRequireFreshAttemptsAndExplicitUncertainMigrationRecovery`,
after 542.755 seconds for that storage command. Other contexts' PostgreSQL
integration passed. The exact failed method was then invoked locally with
`--filter-method '*PublicCorrectionsRequireFreshAttemptsAndExplicitUncertainMigrationRecovery' --filter-trait Provider=postgres --minimum-expected-tests 1`:
one passed, zero skipped, 37.270 seconds. This is a bounded reproduction attempt,
not proof of a fix or permission to ignore the CI failure. No deadline/test floor
was weakened. That PR run's tested synthetic merge revision is
`77123073ba13e0d1d18580c5112b7d064eb26dd2`; its associated branch head is `720f398`.
The local diagnostic full PostgreSQL storage command subsequently passed at
the recorded four-processor setting in 467.084 seconds, unlike its earlier
broad failure. Cause remains unestablished; final complete local/exact-head
CI reconciliation is still required.

### Blocked local gate and CI reconciliation

The diagnostic invocation
`python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10`
completed with **exit 1**, from 17:56:20 to 18:51:24 UTC on 2026-10-24.
It started at `720f398`, before the final configured-stack wiring at `aaba315`;
it is neither a passing gate nor a final exact-code run. It used the pinned
environment above, `DOTNET_PROCESSOR_COUNT=4` and
`KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`.

| Local named suite      | Observed result                                                                                                                                                               |
| ---------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| quality                | 20 steps passed.                                                                                                                                                              |
| unit                   | 14 steps passed.                                                                                                                                                              |
| integration / sqlite   | Nine steps passed.                                                                                                                                                            |
| integration / postgres | Eleven steps passed; `composition-integration-postgres` failed two native identities listed below. The broader storage command passed in 467.084 seconds.                     |
| recovery / both        | Nine steps passed; `storage-public-recovery-sqlite` failed before subsequent provider/composition work.                                                                       |
| contract / both        | Thirteen steps passed; `composition-public-contracts` failed in 582.370 seconds, before the direct-guide loop.                                                                |
| browser                | Sixteen steps passed.                                                                                                                                                         |
| privacy / both         | Forty-one steps passed.                                                                                                                                                       |
| startup                | Nine old-wiring steps passed; this invocation did not execute the later configured-stack addition. Separate current-wiring twelve-step startup runs passed as recorded above. |
| failure-propagation    | Failed `failure_baseline_not_green`; its isolated PostgreSQL storage baseline failed one native identity.                                                                     |

The PostgreSQL composition step identified
`PostgresCompositionAuthorityTests.KilledLaterMigrationRetainsEarlierCommitAndOriginalDataUntilExplicitAcceptedRecovery`
and
`PostgresCompositionPreflightTests.ExistingPreparedModelAndHistoryMustMatchItsActualRecipe`.
One bounded local invocation of their exact method selectors,
`dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-method '*KilledLaterMigrationRetainsEarlierCommitAndOriginalDataUntilExplicitAcceptedRecovery' --filter-method '*ExistingPreparedModelAndHistoryMustMatchItsActualRecipe' --filter-trait Provider=postgres --minimum-expected-tests 2`,
passed three cases, zero skipped. The failed public recovery selection,
`dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-query '/[(Suite=recovery)&(Provider=sqlite)]' --minimum-expected-tests 1`,
passed six cases, zero skipped, when reproduced separately. Private xUnit XML
was used for bounded diagnostics, not exported into safe reports. Those passing
reproductions establish no causal fix. The public-contract assembly's named
failure reports do not retain failed identities; this diagnostic gap remains
relevant to investigating the broad public failure.

The isolated failure-propagation baseline cloned **`aaba315`**, despite the
outer invocation starting earlier. Its storage command failed after 532.967
seconds with
`PostgresCompositionPreflightTests.NoStorageRecipeIsRejectedWithoutInvokingItsFactory`.
Thus the local failure cannot simply be dismissed as testing only old code.
The safe reports remain under `artifacts/verification/`, including
`integration-postgres-4fb1247de929468c89f43e144466cc71.json`,
`recovery-both-9d0e32cea87b4ecfb11f5bc0e738395c.json`,
`contract-both-cc260560d6464a19864e50c41cb275a2.json` and
`failure-propagation-none-68d1416e2ea84bc6bf54863d58cb1c3a.json`.

In contrast, actual [product CI run 40119771552](https://github.com/Stonefly-Labs/Kofte/actions/runs/40119771552)
for branch code head **`aaba315bbaa8debcdc68238e2fd8996a0aff7436`** passed
all ten named jobs / **286 steps**. Its tested synthetic merge revision,
retained in every report, is
`462bf42a4d938e3650bc77ae0ab7760512bc5052`; it is not the branch SHA.
[Repository checks 40119771559](https://github.com/Stonefly-Labs/Kofte/actions/runs/40119771559)
also passed: eleven actual contexts, not a claim based on queued job names.
The target was Ubuntu 24.04 x86_64, four logical CPUs / 16,766,410,752 bytes
RAM, Python 3.12.3 and the unchanged .NET 10.0.402 / Node 24.22.0 / npm 11.19.1
pins. Downloaded artifacts contain only the workflow's sanitized verification
JSON, retained locally under `artifacts/ci/40119771552/verification-*/`.

| Exact named CI command                                                                    | Observed result                                                                                            |
| ----------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| `python3 scripts/verify.py --suite quality`                                               | Passed 20 steps.                                                                                           |
| `python3 scripts/verify.py --suite unit`                                                  | Passed 14 steps.                                                                                           |
| `python3 scripts/verify.py --suite integration --provider sqlite`                         | Passed nine steps.                                                                                         |
| `python3 scripts/verify.py --suite integration --provider postgres --disposable-postgres` | Passed twelve steps.                                                                                       |
| `python3 scripts/verify.py --suite recovery --provider both --disposable-postgres`        | Passed 43 steps, including both providers' focused composition selectors.                                  |
| `python3 scripts/verify.py --suite contract --provider both --disposable-postgres`        | Passed 101 steps, including 24 actual composition guide commands and 24 separate strict observation steps. |
| `python3 scripts/verify.py --suite browser`                                               | Passed sixteen steps.                                                                                      |
| `python3 scripts/verify.py --suite privacy --provider both --disposable-postgres`         | Passed 41 steps, including both providers' focused native/public composition selectors.                    |
| `python3 scripts/verify.py --suite startup --warm-starts 10`                              | Passed twelve steps, retaining ten empty-host cycles and two configured-module/web cycles.                 |
| `python3 scripts/verify.py --suite failure-propagation --disposable-postgres`             | Passed eighteen steps, including its actual green isolated baseline and negative mutations.                |

T074's eleven standalone scenarios and `all` passed independently on **each**
provider. Both exact published Release commands executed after the build:

```bash
dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario all
KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario all
```

The sanitized `all` results retain **389,385 SQLite / 389,390 PostgreSQL**
assertions; actual command durations were 159.250 / 245.037 seconds. Each
provider independently recorded 84,000 exact decisions across 100 repetitions,
ten permutations, two real cultures and two public consumer paths; three
preservation cycles/six fresh processes; five named corrections/fresh attempts/
verified exits; seven ignoring-token boundaries and verified root exits; and
six private capture channels with six zero-leak comparisons. Mandatory
owner/data/history/unrelated-state, descendant/refusal, elapsed-budget and
useful-record observations remain separately validated, not inferred from
assertion totals. The contract artifact retains distinct command/observation
steps for all 24 executions.

This supports T073-T076's delivered wiring, executable examples and named
supported-CI gates. T077's interface/ownership/credential audit above and
current progress documents now accurately state the block. It does **not**
satisfy T078's complete post-wiring local invocation or erase the failed
local/earlier CI results. No cause is established, including resource pressure;
no production deadline, scenario, test floor or provider requirement was
weakened. No further broad retry was launched without a causal change.

**Resume implementation, not convergence:** obtain safe failed public-test
identities and reproduce the retained PostgreSQL preflight/authority and
public recovery/contracts under their owning named suites; investigate and
correct any scoped root cause. Then execute the required complete post-wiring
local command and reconcile its exact reports with assessed-head CI.
T078-T080 remain unchecked. The driver records a blocked stage on its
original branch/draft PR, preserving the original start time and prior
historical stage evidence.

Blocked-checkpoint publication checks:
`python3 scripts/check_repository.py` passed;
`python3 -m unittest discover -s scripts/tests -v` passed all **117** cases;
`git diff --check` passed. This follow-up changes only progress/evidence,
task markers and driver state, not product/test/tooling code. Private passing
probe XML/logs are discarded; sanitized local/CI reports remain ignored.
Publishing this partial checkpoint does not complete T080, which depends on
the unfinished full-gate and final mapping obligations.

## Acceptance evidence

T027 reuses `ModuleLifecycleTests.cs` rather than creating a second lifecycle
test class. Its initial-health regression failed for all three nonhealthy
states before the fix. Attempt publication now invokes the final health
admission after deadline/mode checks and sets its latch only when admission
succeeds. Public boot and later story evidence remain separate obligations.

The following mapping identifies delivered source and meaningful assertions.
The current final gate is reconciled separately below; historical failed gates
remain failed. Test classes
below live in the dedicated unit, integration or public-contract projects under
`tests/`; their owning named commands are recorded above and in the final gate
report. Task markers follow the actual execution and publication obligations,
not artifact presence.

### Functional requirements

| Requirement | Delivered implementation and discriminating evidence                                                                                                                                                                                                                                 |
| ----------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| FR-001      | `ModuleCompositionHost` retains `RunUncomposedAsync`; `BootScenarios.EmptyAsync` checks actual endpoints and no optional activation, while disabled scenarios count zero callbacks and stores. Retained browser/startup suites remain required.                                      |
| FR-002      | Explicit `ModuleDeployment` catalog, `CompositionPreflight` and `CompositionResolver`; manifest/missing/duplicate cases reject without runtime factories. Public `disabled-missing` and `invalid-composition` exercise actual deployed selection.                                    |
| FR-003      | `CompositionGraph` uses accepted parsed ranges and explicitly selected owners; independent missing/disabled/range/prerelease/self/SCC cases in `CompositionCorpus` and resolver tests reject without enabling or downloading.                                                        |
| FR-004      | Ordinal frontier and bounded SCC/diagnostic ordering in `CompositionGraph`/`CompositionResolver`; `CompositionLimitTests` and both public determinism paths compare independently expected results across repetitions, permutations and real cultures.                               |
| FR-005      | `CompositionPreflight`, read-only `CompositionStorageInspector` and all-recipe boundary precede registration/writes; both-provider `CompositionPreflightTests` compare complete seeded inventory and physical state after refusals.                                                  |
| FR-006      | `ContributionValidator` and native registration ownership audit; `ContributionClaimTests` and `ModuleLifecycleTests` reject reserved/overlapping/wrong-owner/unsupported claims before publication or factories.                                                                     |
| FR-007      | Strict copied owner settings in `ModuleSelectionReader`/`ModuleConfiguration`, declared validators and known field tokens; selection/configuration/preflight tests prove byte/depth/enumeration bounds, no environment fallback and no raw unknown keys/values.                      |
| FR-008      | `CompositionAttempt` and `ModuleLifecycleCoordinator` track entered/completed stages and whole-phase barriers; `ModuleLifecycleTests` and actual migration progress tests reject partial-completion publication.                                                                     |
| FR-009      | Infrastructure `CompositionStorageSession`, inspector, preparation and verification reuse actual 003 bindings; both-provider recipe/migration/preflight tests verify immutable assignments, own histories and no fabricated no-storage binding.                                      |
| FR-010      | Exclusive stopped preparation and read-only serving in `CompositionWorker`; real migration, retained authority, crash/recovery and public `ordered-migration` cases prove no replay/revision writes, no lease reacquisition and preserved earlier commits.                           |
| FR-011      | Initial health admission plus atomic snapshot publication; unhealthy-initial-sweep red/green regression, concurrent withdrawal tests and public boot/health scenarios prove zero premature or partial readiness.                                                                     |
| FR-012      | Strict private desired file is copied per attempt; preservation scenarios edit only between verified exits and compare exact original mappings/data/history after each disabled/re-enabled leg.                                                                                      |
| FR-013      | Resolver selection guard and current storage verification; selection/preservation/upgrade tests reject enabled dependents and declaration/identity changes unchanged, without cascade or remapping.                                                                                  |
| FR-014      | Public `ModuleObservation` and `CompositionHealth` recompute individual freshness and dependency-effective severity; health/observation tests and `health-impact` prove transitive impact, independent roots and stale/current-attempt separation.                                   |
| FR-015      | `ModuleAttemptResources`, coordinator reverse cleanup and supervisor owned exits; stop/lifecycle/budget tests plus correction/recovery scenarios prove entry-before-callback, LIFO, single disposal, preserved data and fresh explicitly corrected attempts.                         |
| FR-016      | Shared monotonic `CompositionAttempt` caps, callback runner and process supervisor; actual deadline tests/public `bounded-stop` distinguish cooperative cancellation from continuing work, verified root/descendant exit and unverified-termination refusal.                         |
| FR-017      | Fixed-code `CompositionDiagnostics`/`CompositionProtocol` and bounded parent projection; diagnostic tests and six-channel captures require useful stage/outcome/guidance/current correlation/003 durability alongside zero leaks, including failed sinks and identity-less refusals. |
| FR-018      | Operator-only file/CLI and private observations; serving child strips provisioning credentials. Snapshot-only `HealthHandler` preserves anonymous schema, host/storage failure distinction and no inventory or lifecycle mutation route.                                             |
| FR-019      | Public lifecycle/configuration/composition contracts plus separately compiled `Kofte.Composition.TestModule` and `Kofte.Composition.Consumer`; native-DI/public guide cases consume those contracts without production references to test code or executing future capabilities.     |
| FR-020      | Eleven standalone guide scenarios and required sequential `all`, strict C#/Python omission/result regressions, and retained named foundation gates. Full local/CI results, not file presence, determine final completion.                                                            |

### All twenty story scenarios

| Scenario | Observable assertion / actual evidence owner                                                                                                                                            |
| -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| US1.1    | `empty-boot`: actual empty host endpoints, zero optional activation/allocation; retains both empty registry formats.                                                                    |
| US1.2    | `valid-dependency`: independent compiled Alpha/Beta services, exact phase/dependency order and no prepublication readiness.                                                             |
| US1.3    | `disabled-missing`: disabled author/registrar/runtime/storage counters remain zero.                                                                                                     |
| US1.4    | `disabled-missing` and public boundary cases: missing enabled implementation rejects, never becomes successful empty composition.                                                       |
| US2.1    | Independent malformed/duplicate/incompatible corpus and process validation compare ordered safe diagnostics and zero mutating callbacks.                                                |
| US2.2    | Missing/disabled/incompatible/prerelease dependency fixtures identify dependent and accepted range without changing selection.                                                          |
| US2.3    | Self/multiple SCC fixtures compare stable canonical cycle diagnostics across all permutations.                                                                                          |
| US2.4    | Configuration/contribution negative corpus and real preflight snapshots prove safe known-token guidance, no publication and unchanged stores.                                           |
| US3.1    | `ordered-migration` and provider migration tests compare actual Alpha/Beta clean/additive histories, stable stage order and all-recipes-before-write barrier.                           |
| US3.2    | No-storage provider/boot cases activate independent behavior with no binding, store/history or recipe invocation.                                                                       |
| US3.3    | Later-owner interruption preserves the earlier commit/original records, blocks dependent serving and unchanged automatic retry, then requires explicit accepted recovery.               |
| US3.4    | Retained-authority/lease-loss/crash cases and `bounded-stop` use actual owned processes/sessions, classified contention, no reacquisition or continuation.                              |
| US4.1    | `disable-reenable`: each of three disabled legs preserves exact original assignment, sentinel, history/revision and unrelated state.                                                    |
| US4.2    | Three fresh re-enabled legs reuse original stores without replay; separate explicitly prepared upgrade advances only its selected owner.                                                |
| US4.3    | Invalid dependent-disable selection refuses unchanged; no cascade, uninstall or hidden correction.                                                                                      |
| US4.4    | Reverse entered-owner Stop and LIFO resources, scope/provider-last disposal, verified exit and fresh next-attempt health.                                                               |
| US5.1    | Boot/public health snapshots show desired/stage/unknown before complete initialization/migration/healthy admission; no falsely ready latch.                                             |
| US5.2    | Own/effective failed > unknown > degraded > healthy, individual expiry and independent roots are asserted by unit tests and `health-impact`.                                            |
| US5.3    | Five named corrections and seven uncooperative boundaries require proper stage/outcome/budget, actual exit, new correlation and explicit recovery for uncertain writes.                 |
| US5.4    | Actual record/event/span/metric/stdout/stderr/HTTP captures require safe diagnostics and zero seeded secrets/paths/exceptions; aggregate routes remain non-mutating and inventory-free. |

### Measurable outcomes

| Outcome | Required measured evidence                                                                                                                                                                                                                |
| ------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SC-001  | Public real-process empty/dependency/disabled scenarios and native provider boots; distinct module services usable, zero disabled effects.                                                                                                |
| SC-002  | Twenty-one independently expected fixtures × 100 repetitions × ten permutations × two cultures × two public consumer paths = 84,000 exact comparisons per full deterministic scenario; invalid fixtures have zero mutating effects.       |
| SC-003  | Three complete cycles per provider, six distinct roots/workers/attempts, exact original Alpha/Beta mappings/data/history and unrelated-state comparisons after every leg; explicit owner-only upgrade is separate.                        |
| SC-004  | Validate 45 s (35+10), prepare 120 s (105+15), startup 60 s (45+15), stop 15 s (10+5); actual fault/exit cases and monotonic clipping tests retain native storage 2/3/30/120 s maxima and whole-stack 285/300 s envelope.                 |
| SC-005  | Configuration/Initialize/Start/health/migration correction examples succeed only after their named correction with fresh attempt and retained stores; migration uncertainty first requires explicit recovery.                             |
| SC-006  | Actual six-channel capture requires zero canary leaks and all useful safe records; retained quality/unit/providers/contracts/recovery/privacy/browser/startup/failure-propagation gates remain mandatory, not inferred from new examples. |

### R1-R12 and constitutional boundaries

| Decision | Delivered evidence                                                                                                                                                                 |
| -------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| R1       | Explicit public deployment entry point, compiled test-module/deployment projects; production project-reference audit finds no test project/source reference.                       |
| R2       | Bounded ordinal graph/diagnostic resolver, independent exact repeated/permuted corpus and one-over-limit refusals.                                                                 |
| R3       | Strict owner-only private desired file, copied snapshot transport and ephemeral current-attempt observations; replacement-after-admission regression.                              |
| R4       | Declarative contribution validation/native ownership audit; unsupported execution refuses and metadata confers no authority.                                                       |
| R5       | One final native provider, per-owner scopes, direct handlers and whole-phase barriers; partial entry, dependency binding and single-disposal regressions.                          |
| R6       | Mutation-free inventory/model preflight, exclusive admitted preparation, credential-stripped ordinary serving; real both-provider unchanged-state assertions.                      |
| R7       | Exact assigned history/current-target verification, three preservation cycles and explicit interrupted-owner/complete-set recovery; earlier commits retained.                      |
| R8       | Serialized topological health, 1/20/10/30-second callback/sweep/interval/freshness behavior, conservative initial admission and unchanged anonymous aggregate contract.            |
| R9       | Owned serving/preparation workers, monotonic nested budgets, cooperative-cancellation drain within original deadline, actual root/fixture-descendant exit and incomplete refusal.  |
| R10      | Fixed codes/field tokens/guidance, bounded validated frames/rates, linked storage durability and actual safe private captures; required sink failure remains failure.              |
| R11      | Independent public module/consumer, all eleven standalone scenarios plus `all`, strict omission/tampering/result guards; no synthetic positive counts substitute for observations. |
| R12      | Existing ten suite groups/minima, both real providers, browser/accessibility/startup/failure propagation and exact-head CI; historical failure evidence preserved.                 |

| Principle | Implementation scope / evidence                                                                                                                                                |
| --------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| I         | Empty optional-module host retained, Köfte branding, reviewed MIT dependencies; no Hoard/cloud prerequisite.                                                                   |
| II        | Versioned immutable catalog/preflight/lifecycle/storage/health, preserved disable; compiled trusted code explicitly not sandboxed or hot-unloaded.                             |
| III       | Module-local slices, minimal common author contracts, native DI/direct handlers and test source exclusively in dedicated `tests/` projects; reviewed exact DI dependency edge. |
| IV        | Accepted registry UNIQUE/concurrent/reserved/63-byte/path/owner/model/history checks and real schema/file-isolated migrations retained.                                        |
| V         | Earlier owner commits remain real; no cross-owner table/connection/transaction or global rollback; explicit forward/full-set recovery responsibility.                          |
| VI        | Installation-owned file/commands, scoped settings and credential stripping; no anonymous administration/business authorization bypass or diagnostic leakage.                   |
| VII       | MCP/tool declarations are validated inert metadata, not a functioning tool runtime or permission grant; future authenticated parity remains required.                          |
| VIII      | Attempts/health are ephemeral observations, not durable jobs/events/resources or exactly-once work; later runtimes not claimed.                                                |
| IX        | No new lifecycle UI/frontend loader; existing anonymous schema, generated contract, adapter, browser/accessibility/loading/error/status remain regression gates.               |
| X         | Existing macOS/Ubuntu targets, both real stores, finite supervised budgets, explicit setup/recovery and safe bounded telemetry; no deployment-production claim.                |
| XI        | Focused red/green races and actual provider/process/public/privacy assertions plus retained complete gates; no full reference-module or MVP release claim.                     |
| XII       | All requirements/scenarios/outcomes and R1-R12 mapped; approved intent/checklists unchanged, one original branch/draft PR, separate converge/review/explicit acceptance.       |

### Task coverage

| Tasks     | Evidence ownership                                                                                                                                                                     |
| --------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| T001-T003 | Entry/checklist/ADR/dependency adoption and locked project/build evidence above.                                                                                                       |
| T004-T005 | Owned fixtures/process captures and strict public consumer omission/tampering harness.                                                                                                 |
| T006-T014 | Immutable shared contracts, attempt/budget/diagnostic/selection/storage preflight foundations and unit/provider red/green matrices.                                                    |
| T015-T026 | US2 independent rejection/determinism/limits/configuration/contribution and unchanged-store evidence.                                                                                  |
| T027-T036 | US1 actual empty/dependency/disabled boots, native DI, phase barriers, initial-health admission and reverse cleanup.                                                                   |
| T037-T049 | US3 both-provider prepare/current/additive/failure/authority/recovery and unchanged ordinary serving.                                                                                  |
| T050-T057 | US4 exact three-cycle preservation, dependency guard, partial stop and explicit isolated upgrade.                                                                                      |
| T058-T072 | US5 health/aggregate/fault/termination/correction/six-channel privacy and executable documentation.                                                                                    |
| T073      | Required-file/project/scenario manifests, focused trait selectors and Python missing/zero/provider/sum/output regressions; current-wiring quality and all owning CI suites pass.       |
| T074-T076 | All 24 standalone/provider `all` executions and strict observations, both providers, browser/startup/privacy and isolated positive/negative exercises pass locally and in CI. |
| T077      | Reference/test-ownership/credential/ignored-artifact and interface/budget/guide audit; current docs distinguish implementation completion from later convergence/review/acceptance. |
| T078      | Exact `267efc8` complete local all-suite: ten reports, 286 steps, `completeGate: true`; eleven exact-code CI contexts / 286 steps and every provider/scenario reconciled below. |
| T079      | All 20 FRs, 20 story scenarios, six SCs, R1-R12, twelve principles and 80 tasks map to actual source/assertions, measured observations, supported targets and retained limitations. |
| T080      | Mandatory repository/Python/diff checks, reviewed scoped documentation/evidence/task/ledger completion transaction and immediate push to the same draft PR; no next stage or acceptance. |

## Resumed verification: fixture administration and safe public failures

Historical repair checkpoint (`cf42d82`): the explicit request to finish resumed the original
004 / implement stage on its existing branch/draft PR. Its original start and
history are preserved; reviewer checklists, approved intent and T001-T077 are
unchanged. T078-T080 remain incomplete pending the complete post-fix gate.

Two new Python regressions first failed because public platform-contract
commands had no native diagnostic collector and isolated failure evidence
dropped their diagnostic fields. The runner now uses the same private bounded
xUnit path for native storage and public platform contracts, including actual
nested `Outer+Derived` identities. Reports retain only source-allowlisted
identities and fixed codes. Unknown/duplicate/canary fields and a mismatched
owning command are rejected on isolated-report revalidation; private XML and
raw output are not retained in public evidence. The focused diagnostics,
runtime and failure-propagation selection passed all **72 tests**; the full
repository Python suite passed all **119 tests**.

A fresh broad PostgreSQL storage run used:

```bash
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-trait Provider=postgres --minimum-expected-tests 1 --report-xunit-xml --report-xunit-xml-filename result.xml --results-directory <owner-private-results>
```

It **failed**, exit 2: **126 total, 125 passed, one failed, zero skipped**,
449.754 seconds. Private XML identified
`PostgresCompositionRecipeTests.LaterUnselectedProviderUnsafeMigrationCannotAllocateOrUpgradeEarlierAuthorOwner`.
The failure stack was fixture disposal/ownership validation calling
`PostgresResources.AdminAsync`, not the recipe assertion: a fresh privileged
connection exceeded the three-second connection cap with
`Npgsql.NpgsqlException` / `Timeout during reading attempt`.

The accepted causal regression is
`PostgresOwnershipTests.FixtureAdministrationUsesItsOwnBudgetAndStillHonorsCallerCancellation`.
It first checks caller cancellation, then pauses only its own disposable
container for four seconds while ownership validation opens a fresh privileged
connection, always resuming that service in `finally`. Before the fix this
failed at fixture administration's three-second cap. After the fix this and
the retained ownership/store cases passed: **six tests, zero skipped**,
28.320 seconds, using a single class-filter command selecting
`PostgresOwnershipTests` and `PostgresStoreTests`. Earlier catalog-lock fault
attempts were discarded because they introduced unrelated cancellation/
catalog behavior; no such catalog manipulation remains in the regression.

The fix is limited to test infrastructure: each private administration
operation has one **30-second linked connection-plus-command budget**, still
clipped by caller cancellation, the outer fixture scope and the retained
60-second cleanup. Exported runtime **and provisioning** connection/query
limits remain explicitly **three seconds**. No production code, lifecycle/
probe/acquisition deadline, scenario, floor or provider requirement changes;
there is no retry. The inherited three-second product limits are not a
reasonable budget for fixture-owned control-plane setup/ownership/cleanup.

The actual workstation configuration remains the pinned SDK/runtime above,
`DOTNET_PROCESSOR_COUNT=4` and explicit disposable PostgreSQL opt-in.
Processor count is recorded configuration, not an established explanation for
every earlier failure. `dotnet format Kofte.slnx --verify-no-changes --no-restore
--verbosity quiet`, `python3 scripts/check_repository.py`, the 119-test Python
suite and `git diff --check` passed. No complete post-fix local all-suite or
exact-candidate CI result is claimed by this checkpoint; earlier SQLite/public
failures are not all causally explained by the fixture defect.
The subsequent `dotnet build Kofte.slnx --no-restore -c Release -warnaserror
--verbosity quiet` also passed with zero warnings/errors; after the documentation
and guarded command lookup update the required repository/Python/diff checks
passed again, with all 119 Python tests.

## Post-repair full gate and isolated contention workload

The complete post-repair command at exact code head
`cf42d829006ab3f60c8b322d4c6144b012ad9469` **failed**, exit 1.
Nine suite/provider reports passed: quality, unit, both integrations, recovery,
contract, browser, privacy and startup. This includes the formerly failing
PostgreSQL composition, SQLite public recovery, broad public contracts and all
24 direct guide commands/strict observations. Failure propagation alone failed
its isolated PostgreSQL baseline at
`PostgresCompositionProviderTests.OwnedProviderBootPreservesInventoryAndExecutesOnlyItsIntendedPublicModules`.
Every local report correctly retains `completeGate: false`; this is not a
successful all-suite run.

Exact-head supported [product CI 40130080126](https://github.com/Stonefly-Labs/Kofte/actions/runs/40130080126)
and [repository CI 40130080124](https://github.com/Stonefly-Labs/Kofte/actions/runs/40130080124)
passed all eleven contexts. All ten product reports share actual synthetic
merge revision `304c454332aab5e05ec0f552ee6d2d88cbadc89d`, not the branch SHA;
their 286 steps, 24 actual Release guide commands and 24 complete observation
records were reconciled. Safe reports remain under
`artifacts/ci/40130080126/verification-*/`. This CI success does not replace
the failed local gate.

Private diagnostic replays retained no raw material in public reports. An
initial manual replay omitted disposable opt-in and was discarded; the
correctly opted-in focused three-case run passed. An isolated broad replay
failed the `disabled-missing` row, while subsequent instrumented broad runs,
twenty serial and twenty four-way copies passed. Those passing diagnostic
runs are not a causal repair or final evidence. Private-only tracing changes
were confined to an ignored disposable checkout and are not product changes.

Capturing through the exact isolated runner, including its nested scratch
environment, exposed a second failing baseline:
`PostgresRegistrationConcurrencyTests.TwentyRoundsOfTenSeparateProcessesKeepActualOwnershipUnique`.
All other 125 cases passed; the fixed failure was
`storage_deadline_exceeded; operation=acquire`, not an ownership collision.
The ten-process contention workload was running alongside unrelated xUnit
collections, including hosted lifecycle fixtures with their own strict budgets.
Its acquisition deadline remained correctly enforced.

The contention class now follows the existing isolated-deadline/telemetry
collection pattern: its **twenty rounds and ten independent connections/
processes remain unchanged**, but its own parallel workload is exclusive of
unrelated test collections. No product code, acquisition deadline, retry
policy, case, assertion or minimum changes. This is verification workload
isolation, not a claim that every historical module boot failure has the
same cause. Fresh broad provider and complete exact-code evidence remain
required before T078-T080 can finish.

The focused post-change command was:

```bash
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-trait Provider=postgres --minimum-expected-tests 1 --report-xunit-xml --report-xunit-xml-filename result.xml --results-directory <owner-private-results>
```

It passed **all 126 cases, zero skipped**, 507.086 seconds. Actual xUnit XML
confirms all **four** unchanged same/distinct connection/process contention
cases belong to `registration_concurrency`; this is not just an attribute
inference. `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity
quiet`, the repository checker, all 119 Python tests and `git diff --check`
also passed. The complete final local gate and exact-candidate CI are still
pending.

## Final complete gate and implementation handoff

T078-T079: the exact published code revision
**`267efc8e6bc3c5a783c3962e66c5aca8b383e7b2`** passed:

```bash
python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10
```

The command exited **0**, from **2026-10-24T23:12:11.644752+00:00** to
**2026-10-25T00:38:04.220314+00:00**. All ten expected suite/provider reports
and **286 steps** passed, and every report records `scope: all`,
the exact assessed revision and **`completeGate: true`**.
There was no concurrent heavy local build/test command. Source and worktree
were clean throughout this run; only this final documentation/evidence/ledger
transaction follows it.

The measured supported local target was macOS 26.5 / Darwin 25.5.0 arm64,
18 logical CPUs and 64 GiB physical memory. The pinned SDK was 10.0.402,
Node 24.22.0, npm 11.19.1 and Python 3.15.0. The explicit
`DOTNET_PROCESSOR_COUNT=4` and disposable PostgreSQL opt-in remain recorded
configuration, not a fixed universal performance profile.

These exact safe reports remain under `artifacts/verification/`:

| Report | Steps | Seconds |
| --- | --- | --- |
| `quality-none-6970e9e626554de6bca2c8b58755eb5b.json` | 20 | 28.921 |
| `unit-none-2978a501992942a9939be1fccea1cc41.json` | 14 | 32.409 |
| `integration-sqlite-1a9d9a0c07fd428f9fe8087518ebd3c7.json` | 9 | 445.889 |
| `integration-postgres-cbb6dc2bb39e4092b98669991ba66210.json` | 12 | 774.920 |
| `recovery-both-b383d7fb3e1a40bfa6f2b016c92df73a.json` | 43 | 1039.915 |
| `contract-both-2474804016af4391bf35e5fbdcda6c79.json` | 101 | 1429.619 |
| `browser-none-d875edfea5ae40f1b104c459d6cd9b43.json` | 16 | 48.276 |
| `privacy-both-c5a354f0b96441d782779836845cf254.json` | 41 | 318.691 |
| `startup-none-a4f663c642064528ae4d540819000f1f.json` | 12 | 34.626 |
| `failure-propagation-none-f9936f71ee654511bf078583bd7fc159.json` | 18 | 999.209 |

Both broad/focused native provider assemblies, real recovery, all public
contracts, generated schema/HTTP adapters, browser/accessibility/status and
private captures passed. All **24** actual published Release composition
commands (eleven standalone plus `all` per provider) have separate successful
strict observation records. In particular:

```bash
dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario all
dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario all
```

SQLite passed **389,385 assertions / 80.222 seconds**; PostgreSQL passed
**389,390 / 189.866 seconds**. Each independently records exactly **84,000**
deterministic comparisons, three complete preservation cycles/six distinct
processes and attempts, twelve original assignment/data/history comparisons,
six unrelated-state comparisons, five corrections, seven verified root exits,
one verified fixture-descendant exit and six private channels/zero-leak
comparisons. These counters were inspected beyond mere minimum acceptance.
No synthetic positive result or missing standalone scenario substitutes for
actual execution.

Startup retains all ten empty-host cycles (maximum observed readiness
**5.286005 seconds**) plus two separate configured-module/built-web cycles.
Both configured cycles prove actual owner Start/publication, exact fixture
data and total elapsed **0.933886 / 1.737759 seconds**, retaining the
285/300-second envelope and existing budgets.
Failure propagation's isolated quality, PostgreSQL and browser baselines all
passed at this same exact revision; PostgreSQL took **836.038 seconds**.
All five intended test/formatter/build/PostgreSQL/Chromium mutations failed
at their responsible commands and were successfully recognized, not skipped
or mistaken for a green baseline.

The same branch head passed eleven supported-CI contexts:
[product run 40141738445](https://github.com/Stonefly-Labs/Kofte/actions/runs/40141738445),
all ten jobs / **286 steps**, and
[repository run 40141738437](https://github.com/Stonefly-Labs/Kofte/actions/runs/40141738437).
All ten safe product reports at
`artifacts/ci/40141738445/verification-*/` record actual synthetic merge revision
**`f48e19825224567a500a3280509159c7db74f7b8`**.
That is the assessed PR merge, not a fabricated branch revision. The supported
Ubuntu 24.04 x86_64 runner has four logical CPUs / approximately 16 GiB,
with the same pinned SDK/Node/npm. Expected suite/provider sets, all 286 steps,
24 Release guide commands and 24 strict observations were reconciled.
CI provider `all` commands passed 389,385 / 389,390 assertions in
161.236 / 250.068 seconds respectively.

T080: required repository checks, all **119** Python tests, whitespace and
applicable formatting pass; final task markers, this mapping and the driver
completion are published together on the original branch/draft PR with the
required co-author and immediate push. The completion records only implement
and selects converge for a **separate invocation**. No extension hooks are
configured. Approved intent, original stage start/history and both reviewed
checklists remain unchanged. No product deadline, provider, observation,
worker/round count, retry rule, assertion or floor was relaxed.

## Limitations and handoff

Implementation is complete with all 80 tasks evidenced. Earlier failed runs,
discarded diagnostic setups and scoped fixes remain recorded, not rewritten
as passes; not every historical transient is attributed to one proven cause.
The current exact-code full local/CI gates pass without weakening product
bounds or inherited assertions.

Trusted code remains unsandboxed; owned-process proof does not guarantee
termination of arbitrary escaped descendants. Observations are ephemeral,
not a durable audit/settings/jobs product; future capabilities remain inert.
This is not a full reference module, new human usability study, production
certification or MVP release. Convergence, final review and explicit human
acceptance are separate later stages on `feature/004-module-lifecycle` and
draft PR #7. **Converge is next; no readiness or merge is requested.**

## Convergence remediation T081

**Stage:** implement increment, begun 2026-10-25T04:27:23+00:00.
**Starting revision:** `c6d587e2a5c7837585a0ee61a0db2a35c5fbbab9`.
**Scope:** the first convergence's F1, not a new numbered feature or acceptance.
The original branch/worktree was clean and the same draft PR #7 open;
updated main was inspected. Checklist gates remain 16/16 and 36/36.
No extension hooks are configured. The supported SDK/runtime/environment
above is reused, with `DOTNET_PROCESSOR_COUNT=4` and explicitly owned disposable
PostgreSQL. No tool, dependency, lock, approved intent or product bound changes.

### Delivered correction and invariants

`CompositionPreflight` projects the existing `CompositionHealth.Snapshot`
after successful composition resolution. Each explicitly false or omitted
deployed owner emits one additional current-attempt private
`discovered / disabled / composition_disabled` record with fixed
`edit_selection_while_stopped` guidance. It uses the actual diagnostics,
framed worker/parent transport, projection and telemetry, not a test-owned
snapshot or inferred absence of later callbacks. This identifies disabled
desired state; it does not assert activation, healthy disabled callbacks,
successful later storage inspection or serving readiness.

`CompositionProtocol` copies the admitted enabled-owner set in both parent
and worker. Disabled frames require a known nonenabled identity, exact
stage/code/outcome/guidance and no dependency/settings/storage evidence.
Unknown/missing/enabled owners and contradictory frames fail closed.
The existing version-1 record/frame schema is unchanged: one fixed code/outcome
is added, not a new settings/snapshot field or public endpoint. All output still
passes the existing 4,096-byte frame, 256-record startup/terminal-reserve and
64-record/minute limits; safe metric dimensions remain fixed.

Neither the lifecycle coordinator nor storage/migration/authority implementation
changes. Disabled author validators/registrars/factories/runtime/cleanup remain
uninvoked, omitted owners remain disabled, and anonymous health retains only its
original aggregate fields. No new privilege, failure fallback, automatic
repair/retry, deadline extension, test-floor reduction or production test hook
is introduced.

### Reproduction and acceptance mapping

The public `disabled-missing` case was strengthened **before** the product fix.
The actual command
`dotnet run --project tests/contract/Kofte.Composition.Consumer --no-restore -c Release -- --provider sqlite --scenario disabled-missing`
failed with exit 1 and safe
`composition_test_disabled_observation_missing`, zero reported assertions.
This reproduces the original actual-host omission, not a missing-type error.
After the fix, the same command passes.

On **each** provider the example now boots two real supervised hosts: Alpha/Beta
explicitly false, then omitted with only the no-storage owner selected.
It checks exactly one disabled record per owner/form before publication,
matching fresh attempts and fixed fields/guidance, no later owner stages,
zero disabled callbacks and unchanged exact inventories, enabled public behavior,
clean stop and the retained explicit missing-implementation refusal.
Each standalone result passes **35 assertions**, with:

| Measured observation | SQLite | PostgreSQL |
| --- | --- | --- |
| `explicitDisabledObservations` | 2 | 2 |
| `omittedDisabledObservations` | 2 | 2 |
| `freshDisabledAttempts` | 2 | 2 |
| `disabledCallbackComparisons` | 2 | 2 |
| `unchangedInventories` | 3 | 3 |
| `missingRefusals` | 1 | 1 |

Both the C# scenario catalog/harness and Python named verifier require the three
new observation fields. Existing per-field omission/decrement/tampering tests
now exercise them for standalone and `all`; new unit regressions verify explicit/
omitted preflight observations and valid/contradictory disabled private frames.
They would not be satisfied by callback totals alone.

| Source obligation | Current evidence |
| --- | --- |
| FR-008/012; US1.3; R1/R8 | Existing disabled model projected by actual preflight/worker/parent; real explicit/omitted boots and fresh identity assertions. |
| FR-017/018; R10; principles VI/X | Validated fixed private frames/records, copied enablement allowlist, unchanged bounds/metrics and actual aggregate HTTP privacy assertions. |
| FR-019/020; SC-001/006; T052/T062/T069/T074 | Independent public consumer, both-provider measured output and inventories, strict mandatory observations/omission tests, delivered guide/operator contract. |
| Principles II/III/IV/V/XI/XII | No disabled execution/storage change, dedicated test-project source, retained provider/process/recovery/privacy proof, original feature branch/draft PR and separate later convergence/review/acceptance. |

### Increment verification

The targeted protocol/preflight command
`dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-class '*CompositionDiagnosticTests' --filter-class '*CompositionPreflightTests' --minimum-expected-tests 1`
passes **53/53**, zero skipped. The SQLite public command above and
`dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario disabled-missing`
both pass with the measured observations above. Disposable PostgreSQL is
explicitly opted in, never a developer/production target.

Fresh `python3 scripts/verify.py --suite quality` passes all **20 steps**,
including warnings-as-errors build, .NET formatting, web format/lint/types,
generated contract and dependency reconciliation.
`python3 scripts/verify.py --suite unit` passes all **14 steps**, including
retained baseline and composition suites. These are targeted reports, not a
new complete local all-suite.

| Exact increment command | Observed result |
| --- | --- |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 100` | **Failed**, exit 2: 103/104 passed, zero skipped, 3m 30.714s. SQLite's `ThreeStoppedSelectionCyclesPreserveExactOriginalStoresAndUseFreshProcesses` returned `composition_cleanup_incomplete`. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*CompositionPreservationTests' --minimum-expected-tests 2` | Unchanged isolated rerun passed 2/2, both providers, zero skipped; 16.925s. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*CompositionConsumerHarnessTests' --filter-class '*CompositionBoot*' --filter-class '*CompositionMigrationContractTests*' --minimum-expected-tests 35` | Passed 53/53, zero skipped; 1m 34.338s. Includes new strict-result omission/decrement checks, actual both-provider boots and original preservation regressions. |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario all` | Passed all eleven scenarios, 389,417 assertions; Python's actual `composition_result` validation also accepted the captured complete result and empty stderr. |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario all` | First attempt failed, exit 1, `composition_cleanup_incomplete`. One unchanged isolated rerun passed all eleven scenarios, 389,422 assertions; strict Python validation accepted the complete result and empty stderr. |
| `dotnet format whitespace --no-restore --verify-no-changes --include tests/contract/Kofte.Composition.Consumer/Scenarios/BootScenarios.cs` | Passed after the final strengthened before-publication assertion. |
| `python3 scripts/check_repository.py` | Passed repository assets, links, MCP parity and driver state. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed all 119 tests. |
| `git diff --check` | Passed. |
| `npm run format:check` | Passed. |

The broad integration failure and first PostgreSQL `all` failure above are
retained, **not relabeled as passing runs by their reruns**. Their safe event
code is `composition_test_command_exit_expected_0_actual_1_composition_cleanup_incomplete`.
The same failure category occurred before this increment in the unchanged
convergence assessment; no causal diagnosis or fix to cleanup is claimed.
Both-provider preservation passed unchanged in isolation, and all eleven
public scenarios now passed independently on both providers with their actual
current output. No deadline, assertion, cleanup reserve or test floor was
changed to produce these results. The execution limitation remains explicit
for follow-up convergence/review.

Current targeted safe reports are
`quality-none-15fc44a7af2d4f23a769fe15a0044d22.json` (20 passed steps,
27.131s) and `unit-none-445f727d19f346a28b2afd23abf42993.json`
(14 passed steps, 32.939s), under ignored `artifacts/verification/`.
Actual original-checkpoint [product CI 40158692080](https://github.com/Stonefly-Labs/Kofte/actions/runs/40158692080)
at `c6d587e` passed all ten jobs. It does not test these uncommitted runtime
changes; no new complete local gate or publication CI result is fabricated.
Unchanged browser/web/startup/failure-propagation suites were not separately
rerun by this increment; the retained complete gate above is historical.

### Increment completion and handoff

T081 is implemented and marked complete; the other 80 task descriptions/markers
are unchanged. The task-file status now says 81 tasks, without rewriting
historical generation/completion evidence. The original implementation report
is preserved verbatim before this appended increment, and the convergence
finding/analysis/reviewer-owned checklists/approved spec/plan/research/model/ADR
are untouched. Only delivered interface/guide/current-progress documentation
changes with the runtime/tests/verifier.

The ledger completes only **implement**, selecting **follow-up converge**
for a separate invocation on the original branch and draft PR #7. This
increment does not rewrite the earlier gaps result, run convergence/review,
accept feature 004, mark the PR ready or request a merge. No pre/post hooks
are configured.

## Convergence remediation T082-T083

This is the next **004 / implement** increment on the original
`feature/004-module-lifecycle` and open draft
[PR #7](https://github.com/Stonefly-Labs/Kofte/pull/7), starting at
`2026-10-25T05:05:00+00:00` from `3617931956f8f6d73b59b0cf64137fda0acde348`.
Updated `origin/main` was verified as its ancestor; no new feature/stage branch
or replacement PR was created. The driver began once with explicit
`SPECIFY_FEATURE_DIRECTORY=specs/004-module-lifecycle`. Requirements remain
16/16 and reviewer-authorized quality criteria 36/36, with zero unchecked.
No pre/post implementation hooks are configured.

The approved specification, plan, research/model, ADR, analysis, convergence
assessment and reviewer-owned checklists are unchanged. This increment fixes
only appended T082-T083; it does not perform another convergence assessment,
final review, acceptance or merge.

### T082: preserve accepted executable dependency identities

The existing host prefixes service local names with their immutable owner.
The focused activation theory reproduced the finding: under the ten-character
`org.test.a`, local length 117 succeeds, but accepted lengths 118 and 128 fail;
the maximum 253-character owner plus local length 128 also fails. The initial
run passed **1/4**, with three `composition_activation_failed` results.

`ModuleDependencyBinding` now preserves legacy explicit identities of at most
128 characters and additionally accepts its **exact owner's prefix plus at
most 128 local characters**. Maximum generated identity length is therefore
382; accepted owner/local bounds, character rules, ownership, semantic/contract
versions, collision safety and generation are unchanged. No identity is
truncated, hashed or silently narrowed.

The corrected host theory passes **4/4**, requiring actual registration,
initialization, migration barrier, dependency binding, Start and readiness.
The dedicated platform lifecycle class passes **6/6**, including inclusive and
one-over local/qualified limits, rejection of another owner's long identity,
and preserved owner/operations/version. Tests remain in dedicated test projects.

The separately compiled Alpha module now exports a 128-character local service
name. Beta requires the full 146-character `org.example.alpha`-qualified binding,
exact identity, contract/semantic versions and actual public operation during
Start. The test-owned author observation `boundary-dependency-bound` is required
by the independent consumer; `dependencyIdentityComparisons` is mandatory in
both strict C# and Python observation catalogs. Existing generic per-field
omission/decrement tests cover it. No-storage, disabled and anonymous privacy
assertions remain in place.

### T083: demonstrate and fix an HTTP shutdown defect

Historical terminal-only failures do not identify their individual failing
cleanup operations. The unchanged historical failures above and in convergence
are preserved; a passing isolated rerun is not a causal explanation.

Four fixed private codes now identify outer infrastructure cleanup boundaries:
`composition_host_stop_incomplete`, `composition_host_dispose_incomplete`,
`composition_services_dispose_incomplete` and
`composition_storage_dispose_incomplete`, all with fixed
`inspect_preserved_state` guidance. They add no raw exception, path, settings,
field, protocol version, anonymous detail or output-budget expansion.
These causal records use **`stopping / incomplete`**, not ownerless `cleanup`:
the supervisor already treats every ownerless cleanup record as terminal.
The final cleanup refusal remains unchanged and terminal. Projection errors
remain failures; no failure is converted to successful cleanup.

An initial investigative broad composition run passed **100/104**, failing
four tests due to two temporary mistakes in this increment: the new author
phase was missing from the test-owned allowlist, and causal records were
incorrectly emitted as ownerless terminal cleanup records. Both were corrected.
The corrected focused provider/deadline/PostgreSQL-authority selection passed
**14/14**, and the corrected broad selection passed **104/104** before the
HTTP drain change. Neither result alone was treated as a cleanup fix.

A deterministic real-process regression then keeps an incomplete HTTP request
open during normal stop. Both SQLite and disposable PostgreSQL fail before
the fix (**0/2**, exit 1), now identifying
`composition_host_stop_incomplete`. Kestrel's graceful shutdown outlasts/races
the outer one-second cleanup action while the request remains incomplete.
The composed HTTP host now sets its internal `HostOptions.ShutdownTimeout`
to **500 ms**, leaving time to abort and finish draining inside that action.
The empty direct bootstrap is unaffected.

The regression passes **2/2** after the fix, requiring exit 0, empty stderr,
verified owned root/descendant exit, elapsed stop below 15 seconds and exact
unchanged durable inventory while the client connection stays open.
The one-second Stop/cleanup action, ten-second cooperative reserve,
five-second termination reserve and 15-second total limit are unchanged.
No retry, removed scenario/assertion, lowered floor or expanded external
deadline is used.

The existing actual PostgreSQL lost-serving-session regression additionally
requires exactly one correlated private `composition_storage_dispose_incomplete`
record with fixed stage/outcome/guidance and no owner. It still requires final
incomplete/uncertain refusal, verified process exit and no lease reacquisition.
Final focused provider/deadline/PostgreSQL-authority evidence passes **16/16**,
zero skipped, including both new normal-stop cases and intentional refusal.

This demonstrates and corrects **one causal normal-stop defect**, not a
retroactive forensic explanation of every historical intermittent failure.
The fixed private boundary evidence makes future occurrences distinguishable
without exposing sensitive details.

### Acceptance and constitutional mapping

| Source obligation | Current evidence |
| --- | --- |
| T082; FR-006/008/019/020; US1.2; SC-001; R4/R5/R11 | Actual host boundary Start/readiness, dedicated public binding limits, independent Alpha/Beta exact qualified identity and mandatory strict measured observation. |
| T083; FR-015/016/017/020; US4.4/US5.3; SC-004/006; R12 | Both-provider incomplete-request red/green, fixed bounded private causal output, unchanged cleanup/termination limits, real lost-authority incomplete refusal, exact inventory and owned-process evidence. |
| Principles II/III/V/VI/X/XI | Preserved owner/version boundaries, dedicated test-project source, real providers and owned supervision, no anonymous/raw diagnostic expansion, no weakened assertions or success-shaped cleanup. |
| Principles I/IV/VII/VIII/IX/XII | Existing architecture/DI/dependency and module/storage boundaries retained, no permission/MCP/business endpoint or new package, delivered guide/contracts/progress updated, historical evidence and human-owned later acceptance preserved. |

### Increment verification

Commands use the existing installed .NET SDK 10.0.402/runtime 10.0.13 on
macOS arm64, Node 24.22.0 and npm 11.19.1. No dependency versions, packages or
lockfiles change. Disposable PostgreSQL is explicitly opted in:

```bash
export DOTNET_ROOT="$PWD/artifacts/toolchain/dotnet-10.0.402"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_PROCESSOR_COUNT=4
export KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true
```

| Exact command / selection | Observed result |
| --- | --- |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-method '*DeclaredPrerequisiteOperationIsBoundFromItsOwnScopeWithItsAcceptedVersion*' --minimum-expected-tests 4` | Red: 1/4 passed, three actual activation failures. Green: 4/4 passed with unchanged accepted owner/local bounds. |
| `dotnet test --project tests/unit/Kofte.Platform.Tests --no-restore -c Release --filter-class '*CompositionLifecycleContractTests*' --minimum-expected-tests 6` | Passed 6/6, including inclusive/one-over qualified/local boundaries and wrong-owner rejection. |
| `dotnet build tests/contract/Kofte.Composition.Consumer --no-restore -c Release -warnaserror` | Passed after the binding change and again after the HTTP drain change. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 100` | Investigative run failed: 100/104 passed, four temporary allowlist/terminal-record mistakes described above. Corrected pre-drain run passed 104/104, zero skipped, 4m 30.417s. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*CompositionProviderTests' --filter-class '*CompositionDeadlineTests' --filter-class '*PostgresCompositionAuthorityTests' --minimum-expected-tests 13` | After correcting the investigative diagnostic mistakes, passed 14/14 before adding the incomplete-request regression. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-method '*NormalStopWithAnOpenIncompleteHttpRequest*' --minimum-expected-tests 2` | Red: 0/2 passed; both actual providers identify HTTP host-stop cleanup failure. Green after the internal drain fix: 2/2 passed. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-class '*CompositionProviderTests' --filter-class '*CompositionDeadlineTests' --filter-class '*PostgresCompositionAuthorityTests' --minimum-expected-tests 16` | Final corrected run passed 16/16, zero skipped, 1m 25.352s, including both-provider normal stop and PostgreSQL lost-authority refusal. |
| `python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10` | Failed, exit 1: eight suite/provider reports passed and two failed; 251/253 executed steps passed. SQLite baseline host integration and SQLite composition public recovery failed with exit 2. All ten reports retain `completeGate: false`; no rerun relabels this attempt. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-trait Provider=sqlite --minimum-expected-tests 1` | Unchanged diagnostic rerun passed 31/31, zero skipped, 2m 07.468s. The safe verifier report exposes no individual failed test, so this does not identify the first failure's cause. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-query '/[(Suite=recovery)&(Provider=sqlite)&(Feature=composition)]' --minimum-expected-tests 4` | The complete gate's SQLite public-recovery command failed, exit 2; safe evidence identifies `ActualPublicCommandsPrepareUpgradeAndServeWithoutRewritingHistoryOrOriginalData` with `composition_consumer_failed`, not its underlying operation. The wrapper adds its existing private xUnit-report arguments. |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario ordered-migration` | Unchanged diagnostic execution passed, 84 actual assertions and all six mandatory observation fields. The subsequent complete-gate public-contract command and standalone SQLite guide scenario also passed. These do not erase the failed recovery invocation. |

The first SQLite integration report is
`integration-sqlite-a773172030a2408a9076d68e4289bee3.json`;
its failed baseline `integration-sqlite` command prevented that report's later
storage/composition steps. Safe failure correlation is
`3eef5d98d2e84ba78989cddb385338da`. The diagnostic rerun changes no source,
assertions, deadlines or fixtures.

The first recovery report is
`recovery-both-2d079572f51a4e84b738a155b5c6c788.json`, safe failure correlation
`2704470b92e44a8bb60bc8d85b2861c1`. It stops before PostgreSQL recovery steps.
That historical-safe report's generic consumer failure is insufficient to
attribute the failed migration example to HTTP shutdown or any other boundary.

The complete attempt's safe report reconciliation is:

| Suite/provider | Report | Passed / executed steps |
| --- | --- | --- |
| quality | `quality-none-cee301d644b84df8bfb1f5eefbc40873.json` | 20/20 |
| unit | `unit-none-b271be2260a1455387531d93563f0f65.json` | 14/14 |
| integration / SQLite | `integration-sqlite-a773172030a2408a9076d68e4289bee3.json` | 6/7; failed |
| integration / PostgreSQL | `integration-postgres-e3790f3a6a4d4d43b6a06ed45327a6d8.json` | 12/12 |
| recovery / both | `recovery-both-2d079572f51a4e84b738a155b5c6c788.json` | 11/12; failed |
| contract / both | `contract-both-9a28d0db419e4e249f242c3d48c359ba.json` | 101/101 |
| browser | `browser-none-459590a1527f42b1a07e1c2c2aa2358b.json` | 16/16 |
| privacy / both | `privacy-both-6c3e4251a89e4dc4903949cdd6242d35.json` | 41/41 |
| startup | `startup-none-b7f1d5fde92d4296a8503167714357a7.json` | 12/12 |
| failure propagation | `failure-propagation-none-695be1d9dd8b49b2800f5945282fdb9c.json` | 18/18 |

All reports are ignored safe artifacts under `artifacts/verification/`, not
committed raw runtime output. Their revision identifies the precommit base
`3617931` plus this tested working-tree increment, not a claim that the old
commit already contained these changes.

The contract report executes **all 24 guide commands** (eleven individual
scenarios plus `all`, on each actual provider) and all 24 strict observation
checks. `valid-dependency` passes **37 assertions** on each provider, measuring
three public-service comparisons, three owner-history comparisons, one phase
order comparison, one prepublication comparison and the new exact dependency
identity comparison. Sequential `all` passes **389,418 assertions on SQLite**
and **389,423 on PostgreSQL**, retaining every original scenario, measured
inventory/process/authority/privacy assertion and strict omission requirement.
Startup proves ten warm original-data cycles and two fresh composed-module
cycles. Browser/accessibility, both-provider captured privacy, owned cleanup
and all isolated failure-propagation injections pass.

Only the two failed suites were rerun unchanged to reconcile their
unexecuted later steps; the already passing suites were not needlessly repeated:

| Exact narrowed command | Observed result |
| --- | --- |
| `python3 scripts/verify.py --suite integration --provider sqlite` | Passed 9/9 steps, 421.892s, including baseline host, full storage and dedicated composition integration. Report `integration-sqlite-8da491e12a8749539ac5b4b3f3884aad.json`. |
| `python3 scripts/verify.py --suite recovery --provider both --disposable-postgres` | Passed 43/43 steps, 968.520s, including both-provider native/public composition recovery, original storage recovery, browser recovery and verified owned cleanup. Report `recovery-both-d9e614d3ea4d4872be2e9f4dfd774f3a.json`. |

These **52 successful targeted steps**, together with the eight previously
passing reports, supply passing evidence for the same **286 named steps**.
This is **not a new passing complete local gate**: both targeted reports also
retain `completeGate: false`, the two original failures remain failures, and
their individual causes remain unknown. No runtime, fixture, deadline,
assertion, test floor or source change occurred between the failed all-suite
attempt and narrowed reconciliation. Do not attribute those failures to the
demonstrated HTTP defect or treat the reruns as causal closure.

Final repository checks after the documentation/task updates:

| Exact command | Observed result |
| --- | --- |
| `python3 scripts/check_repository.py` | Passed assets, local links, MCP parity and driver state. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed 119/119 tests, 2.078s. |
| `git diff --check` | Passed. |

Microsoft Learn/Context7 MCP tools were not connected. Official Microsoft
HostOptions/Kestrel documentation was used as the fallback; compilation against
the installed .NET 10 API and the actual red/green process regressions verify
the selected shutdown option, rather than relying on preview documentation.

### Increment completion and handoff

Only T082/T083 markers are completed; T001-T081 descriptions/markers and all
earlier implementation evidence remain intact. The delivered public/operator
contracts, guide, README and directly affected development/architecture/progress
docs now describe the actual qualified binding, internal drain and safe causal
records. No dependency, migration, configuration key, public endpoint or
permission boundary changes.

The driver completes only **implement**, selecting **follow-up converge** for
a separate invocation on the same branch/draft PR #7. This increment does not
rewrite convergence's gaps result, conduct final review, accept feature 004,
mark the PR ready or request a merge. Historical causal uncertainty and the
failed complete local attempt remain explicit limitations for those later
assessments. Actual new-head CI is inspected after commit/push and recorded in
the PR; no historical CI result is claimed for unpublished changes.

The ledger's implementation-report digest is its historical completion
snapshot, before relocating this increment's final-check paragraph out of
the T081 section. This published checkpoint preserves the earlier report
verbatim; runtime/tests and measured results are unchanged by that editorial
correction.

## Convergence remediation T084-T085

This **004 / implement** increment continues the original
`feature/004-module-lifecycle` and open draft
[PR #7](https://github.com/Stonefly-Labs/Kofte/pull/7), beginning once at
`2026-10-25T13:40:45+00:00` from
`004b34bb3a808b7fdb62f48f8ff8b097974af972`.
Updated `origin/main` (`3a5deff380655e72eb6b08ebc1ef694ed5676f4a`) is an
ancestor. No replacement branch/PR, dependency/lock change or later Spec Kit
stage is part of this invocation. The explicit selected directory remains
`specs/004-module-lifecycle`; requirements are 16/16 and reviewer-authorized
quality criteria 36/36, with zero unchecked. No implementation hooks are
configured. Approved intent, reviewer checklists, historical convergence and
all preceding evidence remain intact.

### T084: complete bounded executable exports and activation copies

The actual host lifecycle count theory initially failed **0/4** at registration
or activation; the public activation-copy theory failed **0/2**. Resolution's
accepted bounds were not preserved: the coordinator copied at most 129 exports
and rejected more than 128 globally, while activation copied only 64 services.

The coordinator now enumerates at most 1,025 exports before refusal, accepts
1,024 enabled total and enforces 128 per owner. Default export generation
remains lazy until that bounded copy. Activation contexts copy up to 1,024
prerequisite service bindings, rejecting at entry 1,025. The separate
module-dependency bound is not a service-export bound. No accepted input is
narrowed or silently truncated; exact owner-qualified identities, versions,
operations, collision checks and native DI ownership remain unchanged.

The corrected host theory passes **4/4**: 65 and 128 services from one
prerequisite, 256 across two independent roots, and 1,024 across eight
prerequisites. Each case requires actual registration, initialization, Start,
health/readiness and cleanup; dependent cases compare every exact identity,
version, operation instance/result and denied cross-owner DI lookup. Two
additional cases reject owner 129/global 1,025 in preflight with zero registrar/
runtime invocations. The platform theory passes **2/2**, additionally proving
copied immutable backing storage and refusal of an infinite source after
exactly 1,025 enumerated bindings.

The separately compiled Alpha example exports its original 128-character
local identity plus 126 additional unique services. Beta verifies all **127**
exact bindings/versions and operations during actual supervised Start;
Beta/NoStorage bring the global executable total to **129**. Alpha retains
one of its 128 owned types for its container-owned runtime. Dedicated host
tests separately prove the full 128/1,024 service limits. The consumer requires
the author observation and reports mandatory
`prerequisiteServiceComparisons: 127`; both C# and Python strict catalogs
require it, including existing per-field omission/decrement rejection tests.
All original disabled/no-storage/privacy and public scenarios remain.

### T085: eliminate unused default file-watcher disposal

The first broad run failed **103/106**: two public boot failures were an
increment-local missing author-phase allowlist entry, subsequently corrected;
PostgreSQL preservation independently failed normal host disposal. The next
broad run failed **105/106**, with SQLite preservation reporting
`composition_cleanup_incomplete_composition_host_dispose_incomplete_composition_deadline_exceeded`.
Twelve isolated SQLite preservation executions passed unchanged, and are
recorded only as diagnostic observations, not causal closure or retries that
turn the broad failure into success.

Temporary local, bounded type/method-only instrumentation established
`CompositionUnstoppedException`, not an ordinary disposal exception. A third
broad run failed **104/106**, now both provider preservation tests. Its
disposal callback had entered, the thread pool had five threads and zero
pending items, and no root DI disposer had begun. This local instrumentation
and its exact scratch JSON were removed before the corrected build and gate;
no reflection-based DI wrapper, raw exception, path, setting or diagnostic
capture ships in production.

Official installed-version framework source explains that boundary:
[Host.DisposeAsync](https://github.com/dotnet/runtime/blob/v10.0.13/src/libraries/Microsoft.Extensions.Hosting/src/Internal/Host.cs)
first disposes its content-root file provider, then DI.
[The slim builder defaults](https://github.com/dotnet/aspnetcore/blob/v10.0.13/src/DefaultBuilder/src/WebApplicationBuilder.cs)
start JSON reload watchers during construction.
[PhysicalFileProvider.Dispose](https://github.com/dotnet/runtime/blob/v10.0.13/src/libraries/Microsoft.Extensions.FileProviders.Physical/src/PhysicalFileProvider.cs)
disposes the retained watcher. Clearing configuration sources afterward does
not remove that watcher. The composed host already rejects default app
configuration in favor of its admitted snapshot, so this resource is unused.
No particular native filesystem scheduling mechanism or historical failure
outside this demonstrated boundary is asserted.

A deterministic dedicated-host regression exercises the actual private worker
builder without adding a public API or an internal-access grant. The public
`PhysicalFileProvider.UsePollingFileWatcher` setter rejects changes once a
watcher exists: it fails **0/1** before the correction, proving the unwanted
watcher survived configuration clearing. The worker now supplies
`--hostBuilder:reloadConfigOnChange=false` to builder construction, before
defaults can create a watcher; sources are still cleared immediately
afterward. The regression passes **1/1**, with no initialized watcher and
actual host disposal through the unchanged bounded cleanup runner.

The corrected, instrumentation-free broad run passes **106/106**, zero
skipped, including both-provider three-cycle preservation, failure correction,
HTTP stop, intentional uncooperative refusal, actual lost PostgreSQL authority,
exact original records/history/inventory and verified owned root/descendant
exit. The one-second cleanup action, ten-second cooperative reserve,
five-second termination reserve and 15-second total bound are unchanged.
Required incomplete/uncertain refusal remains intact. No retry, widened
deadline, scenario/assertion removal or lowered test floor is used.

The consumer's existing safe failure summary now includes a stopping
deadline/cancellation code when the private records actually contain one.
It invents neither an exception classification nor new product record fields.

### Increment verification and acceptance mapping

Commands use the existing .NET SDK 10.0.402/runtime 10.0.13, Node 24.22.0
and npm 11.19.1 on macOS arm64:

```bash
export DOTNET_ROOT="$PWD/artifacts/toolchain/dotnet-10.0.402"
export PATH="$DOTNET_ROOT:$PATH"
export DOTNET_PROCESSOR_COUNT=4 DOTNET_CLI_TELEMETRY_OPTOUT=1
export KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true
```

| Exact command / selection | Observed result |
| --- | --- |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-method '*CompleteExecutableCatalogSurvivesRegistrationBindingStartAndReadiness*' --minimum-expected-tests 4` | Red 0/4; green 4/4 actual count-bound lifecycle cases. |
| `dotnet test --project tests/unit/Kofte.Platform.Tests --no-restore -c Release --filter-method '*ActivationCopiesEveryBoundedPrerequisiteServiceAndRejectsTheFirstOverflow*' --minimum-expected-tests 2` | Red 0/2; green 2/2 copied binding/overflow cases. |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-build --no-restore -c Release --filter-class '*ModuleLifecycleTests' --minimum-expected-tests 25` | Passed 39/39, zero skipped, including both new one-over resolver cases. |
| `dotnet test --project tests/unit/Kofte.Platform.Tests --no-build --no-restore -c Release --filter-class '*CompositionLifecycleContractTests' --minimum-expected-tests 4` | Passed 8/8, zero skipped. |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-method '*ComposedHostDoesNotStartUnusedDefaultConfigurationWatchers*' --minimum-expected-tests 1` | Red 0/1 with initialized watcher; green 1/1 with no watcher and bounded actual disposal. |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --verbosity quiet` | Passed with zero warnings/errors after corrections. One intermediate build correctly rejected a new test's missing xUnit cancellation token; fixed without suppressing the analyzer. |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-build --no-restore -c Release --filter-method '*ExecutableOverflow*' --minimum-expected-tests 2` | An intermediate stale no-build selection ran zero tests and failed exit 8. It is not evidence of success; the rebuilt 39-case lifecycle class above executes both cases. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 106` | Investigative runs failed 103/106 (3m 30.368s), 105/106 (3m 27.447s), and 104/106 (3m 32.049s). Corrected run passed 106/106, zero skipped, 3m 27.852s. |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario valid-dependency` | Passed after the author allowlist correction, requiring all 127 prerequisite operations. |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario disable-reenable` | Passed once plus twelve standalone diagnostic executions before the watcher correction; not used to erase broad failures. |

| Obligation | Evidence |
| --- | --- |
| T084; FR-004/006/008/019/020; US1.2; SC-001; R4/R5/R11 | Exact inclusive/one-over counts, bounded immutable copies, complete actual registration/Start/readiness, independent public 127-prerequisite/129-total example and mandatory strict measured evidence. |
| T085; FR-012/013/015-017/020 evidence; US4.4/US5.3; SC-003/005; R9/R12 | Bounded causal boundary, demonstrated unused watcher red/green, corrected both-provider preservation/correction/stop, original inventory and actual root/descendant exit. |
| SC-004/006; principles V/VI/X/XI | Retained lost-authority/uncooperative/incomplete refusal, private allowlisted diagnostics, no scheduling fallback or retries-as-success, unchanged process/deadline/provider assertions and historical failed evidence. |
| Principles I-IV/VII-IX/XII | Existing modular/DI/type/identity/storage boundaries and test ownership retained; no package, business endpoint, permission/MCP expansion or new design decision; delivered contracts/guide/progress updated, later human acceptance remains separate. |

Microsoft Learn/Context7 tools were not connected. Official versioned framework
source was the fallback; actual installed API compilation and deterministic
red/green tests verify the option and resource effect.

### Complete local gate and handoff

`python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10`
**passed, exit 0: all ten reports and all 286 named steps passed**. Every
report records `completeGate: true`. The previously failed attempts above
remain failures; this is a new complete attempt after the demonstrated
count/watcher corrections, not a narrowed rerun relabeled as complete.

| Suite/provider | Safe report under `artifacts/verification/` | Passed / executed steps |
| --- | --- | --- |
| quality | `quality-none-703f9428767446d3a66df287768e99a0.json` | 20/20 |
| unit | `unit-none-36468f66478f4420a22fdd2b681e5dab.json` | 14/14 |
| integration / SQLite | `integration-sqlite-f8560fe9e6804632b92e8d66efc1c4ad.json` | 9/9 |
| integration / PostgreSQL | `integration-postgres-6929d70b41384120ab3a65d104c46f5c.json` | 12/12 |
| recovery / both | `recovery-both-855f7ed3707846dc8763c5741221bad0.json` | 43/43 |
| contract / both | `contract-both-b2af402c93eb472a9e961f099f22d61f.json` | 101/101 |
| browser | `browser-none-95e670bbd5d0460abc5c62425feeb82f.json` | 16/16 |
| privacy / both | `privacy-both-1e4e622939394eebab2982ac86690cc0.json` | 41/41 |
| startup | `startup-none-a074ba55e8cf43a780eaa67cae211889.json` | 12/12 |
| failure propagation | `failure-propagation-none-9411d31a74ce48ff842222f484982e56.json` | 18/18 |

The report revision is the precommit base `004b34b` plus this tested
working-tree increment, not a claim that the old commit contains the fixes.
All 24 composition guide commands and all 24 strict observation checks pass:
eleven individual scenarios plus sequential `all` on each actual provider.
This includes both-provider mandatory 127-service evidence and sequential
preservation/correction/stop/privacy assertions. SQLite sequential `all`
passes in 79.576s, PostgreSQL in 174.208s. No old failed SQLite sequential
guide CI invocation is erased or retrospectively diagnosed by these passes.
Startup additionally proves ten original-data warm cycles and two fresh
composed-module cycles, with published modules, matching original fixtures,
usable pages and verified cleanup within the unchanged work/total envelope.
Both-provider recovery, captured privacy, browser/accessibility and all
isolated failure-propagation exercises pass.

Only T084/T085 markers are completed; the 83 preceding task descriptions/
markers, approved spec/plan, reviewed checklists and historical convergence
remain unchanged. README, related contributor/development/architecture docs
and delivered contract/guide explanations describe the corrected behavior.
No dependency, migration, configuration input, public endpoint or authority
boundary changes.

The driver completes only **implement** and selects **follow-up converge**,
on the same original branch/draft PR #7 in a separate invocation. It does not
run convergence/final review, accept feature 004, mark ready or request merge.
Actual publication-head CI is inspected after commit/push and recorded in
the PR; no historical CI run is claimed as evidence for unpublished changes.

Final checks after task/documentation updates:

| Exact command | Result |
| --- | --- |
| `python3 scripts/check_repository.py` | Passed repository assets, local links, MCP parity and driver state. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed 119/119, 2.137s. |
| `git diff --check` | Passed. |
| `npm run format:check` | Passed all configured Prettier inputs. |

An integrity comparison additionally proves all 85 task descriptions, the first
83 checked markers and the entire preceding implementation report are
preserved. No approved specification/plan/research/model/analysis/convergence
or reviewer-checklist file changes.

## Final-review remediation T086

**Status:** code fix and focused regressions published; implement is blocked
by the local complete-gate failure below. T086 remains unchecked.

This implement-only increment resumes the original branch and open draft PR #7
from `be29bf24c4d498716bbf89b3329585247d489df4`. The final review's HIGH R1
and its 16/16 independent public-host counterexamples remain unchanged in
`review.md`. Both checklists remain read-only and passing, 16/16 and 36/36.
The active implementation began at `2026-10-25T20:42:03+00:00`; no extension
hooks, dependency changes or new design approval are involved.

### Demonstrated cause and correction

Parent and worker already share the same all-catalog prerequisite collection.
The protocol incorrectly bounded that diagnostic metadata by 1,024 enabled
graph edges. Thirty-two valid deployed manifests can instead declare 2,048
distinct prerequisites, including dependencies of disabled sources; these
create no enabled edges and require no deployed/enabled prerequisite targets.
Only the protocol materialization changes to the derived `32 * 64` maximum
and its 2,049 overflow sentinel. The enabled resolver, parent/worker collection,
32-deployed/16-enabled/64-per-manifest bounds, frame/record limits and
identity restrictions are unchanged. No accepted input is truncated or narrowed.

Dedicated protocol tests round-trip a genuine missing-prerequisite diagnostic
at 1/1,024/1,025/2,048 identities, prove source-array independence and reject
runtime/health identity grants and forged related IDs. An infinite enumerable
is refused after exactly 2,049 entries without requesting the 2,050th.
The existing public resolver cases preserve missing versus disabled prerequisite
refusals and the enabled-edge/per-manifest limits.

Five structural catalog cases independently resolve both omitted and explicit
false selections, with zero enabled edges and no disabled registration callback:
1,024 prerequisites, 1,025, 1,025 plus an unrelated enabled root, all 32 disabled
deployments/2,048 prerequisites, and 31 disabled plus one enabled/1,984
prerequisites. Matching fixed catalogs in the separately compiled consumer run
actual validate and serve on both SQLite and owned disposable PostgreSQL.
Each case exercises both selection forms: 20 successful validation processes
and 20 serving roots, usable aggregate readiness, exact disabled private
observations, verified clean root/descendant exit and complete inventory equality
after each command. Disabled validators/registrars/runtime factories throw if
invoked; successful validation/ready serving therefore cannot hide their
execution. The mixed cases additionally observe the independent no-storage
module's actual public computation (`13 - 5 = 8`) through its existing private
author channel. No new public scenario, endpoint, product test hook or authority
is introduced.

### Discriminating execution

Commands use the existing SDK/runtime and environment documented in the
T084-T085 increment above, with `SPECIFY_FEATURE_DIRECTORY` explicitly set to
`specs/004-module-lifecycle`.

| Exact command / selection | Observed result |
| --- | --- |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-class '*CompositionDiagnosticTests*' --minimum-expected-tests 1` | Red 47/50: 1,025/2,048 metadata is refused as `composition_observation_invalid`; overflow stops at 1,025 instead of 2,049. The overlapping missing-prerequisite test was then parameterized rather than duplicated. |
| `dotnet test --project tests/unit/Kofte.Platform.Tests --no-restore -c Release --filter-class '*CompositionLimitTests*' --minimum-expected-tests 5` | Passed 29/29, including five new accepted structural cases, both selection forms, zero edges/callbacks and existing inclusive/overflow limits. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-method '*DisabledCatalogPrerequisiteMetadata*' --minimum-expected-tests 10` | Red 2/10; all eight over-1,024 cases fail actual validation with `composition_observation_invalid` on both providers (41.624s). Corrected run passes 10/10, zero skipped (39.544s), executing all 40 validate/serve combinations. |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-class '*CompositionDiagnosticTests*' --minimum-expected-tests 50` | Corrected 50/50, zero skipped; missing-prerequisite transport, overflow, copies, framing and identity refusal retained. |
| `python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10` | Failed, exit 1. PostgreSQL composition fails `KilledLaterMigrationRetainsEarlierCommitAndOriginalDataUntilExplicitAcceptedRecovery`; after collecting recovery, the already-failed attempt is gracefully interrupted during contract tests. No passing complete gate. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore -c Release --filter-trait 'Provider=postgres' --filter-method '*KilledLaterMigrationRetainsEarlierCommitAndOriginalDataUntilExplicitAcceptedRecovery' --minimum-expected-tests 1` | Unchanged diagnostic selection passes 1/1, zero skipped (11.978s). This does not diagnose or erase the full-run failure. |

The first complete attempt's PostgreSQL composition step exits 2 after
216.981s; its safe report is
`integration-postgres-fb12cfa1e3354655abd558c697f88410.json`.
It identifies the migration-interruption test above but retains no raw
assertion/exception. That test uses direct storage preparation/recovery and
the existing crash worker, not the changed protocol or new boundary catalogs.
No causal diagnosis, scheduling explanation or fix to that failure is claimed.
The isolated pass and passing code-head CI PostgreSQL job are narrower
evidence, not a successful full local gate.

### Complete-attempt and actual-code-head reconciliation

The stopped local attempt retains **109 passed / 111 executed steps** across
six reports. One actual PostgreSQL test step fails; one contract step records
the deliberate `verification_interrupted` shutdown. Remaining local suites
were not started. Every report has `completeGate: false`; no isolated rerun,
recovery pass or CI result changes those recorded outcomes.

| Local suite/provider | Safe report under `artifacts/verification/` | Passed/executed steps |
| --- | --- | --- |
| quality | `quality-none-46defa32582948da82efc7a75945ff4b.json` | 20/20 |
| unit | `unit-none-62f69e4e3f0e4d8cae5993364b4f51c3.json` | 14/14 |
| integration / SQLite | `integration-sqlite-3eacd2ae3841438eb661c724dcf04010.json` | 9/9 |
| integration / PostgreSQL | `integration-postgres-fb12cfa1e3354655abd558c697f88410.json` | 11/12; migration-interruption failure |
| recovery / both | `recovery-both-06e00f80cb9e48698de4cc90e099a92d.json` | 43/43, including composition recovery |
| contract / both | `contract-both-5f192bef23fd4adb8882dd64c744216e.json` | 12/13; deliberately interrupted |

The first three reports identify precommit base `be29bf2` plus this tested
working tree; subsequent reports identify code checkpoint
`c749acc22345e4ab868dfc84e30c599208d953bd`. Product/test/script source is
unchanged after that checkpoint. This is not a claim that the old base commit
contains the correction.

Actual code checkpoint **`c749acc22345e4ab868dfc84e30c599208d953bd` passes all
eleven GitHub contexts**:
[Product checks 40232738175](https://github.com/Stonefly-Labs/Kofte/actions/runs/40232738175)
and [Repository checks 40232738167](https://github.com/Stonefly-Labs/Kofte/actions/runs/40232738167).
The ten actual downloaded product artifacts independently reconcile to
**286/286 passed targeted steps**, including 24 executed composition guide
commands and 24 strict observation checks. The contract job passes 101/101.
All reports have `completeGate: false`, because CI runs named targeted jobs,
not the single complete local invocation.

| CI suite/provider | Downloaded safe report | Passed/executed steps |
| --- | --- | --- |
| quality | `quality-none-723e5879e4824f6783861f5f1e9afcdc.json` | 20/20 |
| unit | `unit-none-596fb73e63e640fba9e35408fce5cf49.json` | 14/14 |
| integration / SQLite | `integration-sqlite-8520474675184a45833438deda92434d.json` | 9/9 |
| integration / PostgreSQL | `integration-postgres-fe5ad98e79e54ab091584abfb909ea21.json` | 12/12 |
| recovery / both | `recovery-both-9d8c6a67f5064cde9c6a6ff858e45480.json` | 43/43 |
| contract / both | `contract-both-c28dae2e529040859bef03b2c5cc9292.json` | 101/101 |
| browser | `browser-none-55a6b02af8494945880b465230c0c3c1.json` | 16/16 |
| privacy / both | `privacy-both-fc837c7fe9574de68e9579b0d26d82fc.json` | 41/41 |
| startup | `startup-none-ae63bcac992542c9bdd5d963c53ce77a.json` | 12/12 |
| failure propagation | `failure-propagation-none-83779311df7b46b18db61437c189ee7f.json` | 18/18 |

CI checks out synthetic merge `81a5b26b9da97e1b1f30859a6c30a947a82181c1`.
GitHub's commit API verifies its parents are main
`3a5deff380655e72eb6b08ebc1ef694ed5676f4a` and the exact code checkpoint
`c749acc22345e4ab868dfc84e30c599208d953bd`; no unrelated green revision is
substituted. `gh pr checks 7 --required` reports no configured required checks.
The subsequent blocked-state/evidence checkpoint changes only documentation
and the driver ledger; its fresh publication checks are reported separately
in PR #7 rather than being confused with the verified code-head result.

### Acceptance mapping and handoff

FR-001/004/020, US1.1-1.3/US2.2, SC-001/002/006 and plan R1/R2/R9-R12
map to the exact resolver/protocol/public-process boundaries above.
FR-003/017/019 and principles I/II/VI/X/XI/XII retain finite admission,
fail-closed actual diagnostics, private framing, zero disabled effects,
independent public behavior and truthful checked evidence.
The report preserves all historical failed runs and the distinction between
complete local gates, narrower selectors and publication-head CI.

T086 remains unchecked pending complete-gate reconciliation; all preceding 85 task
descriptions/markers, approved intent, original review/convergence and reviewed
checklists are preserved. The composition contract documents the derived
metadata bound; README, guide and related progress docs reflect the increment.
The driver records **blocked implement** on the original branch/draft PR #7.
Resume that stage before converge; do not complete it around the failed local
gate. Passing final review and explicit human acceptance remain separate; no
acceptance, readiness, merge request or later feature is authorized.

### Blocked-checkpoint verification

No `.specify/extensions.yml` exists, so no before/after implementation hook
is configured. Final checks validate the blocked state and documentation,
not a passing complete product gate:

| Exact command | Observed result |
| --- | --- |
| `python3 scripts/check_repository.py` | Passed assets, links, MCP parity and blocked driver state. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed 119/119, 1.975s. |
| `npm run format:check` | Passed. |
| `git diff --check` | Passed. |
| `git diff --cached --check` | Passed before checkpoint publication. |

An integrity comparison preserves all 86 task descriptions and markers,
including unchecked T086, every prior implementation-report byte and the
approved spec/plan/research/model/analysis/convergence/review/checklists.
The follow-up checkpoint changes no product, test, script or dependency source.
