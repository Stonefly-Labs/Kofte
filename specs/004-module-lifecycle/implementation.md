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
