# Validation Guide: 004 - Module Composition and Lifecycle

**Status**: T086 corrects disabled-catalog diagnostic metadata admission and all
eleven code-head CI contexts pass. Its implementation checkpoint is complete;
the human explicitly deferred investigating a separate local anomaly after
reviewing eight passes and one failure. See the
[facts and disposition](implementation.md#human-directed-deferral-and-implementation-handoff).
The failed local gate remains failed. Converge is next, before final review and
explicit human acceptance; no product-test rerun or further investigation is
performed for this documentation-only disposition.
Complete accepted
service counts and unused default JSON watcher cleanup are corrected; see
the [increment evidence](implementation.md#convergence-remediation-t084-t085).
All eleven scenarios and their sequential `all` composition are runnable,
including interrupted migration, explicit recovery, correction, health, owned
termination and private diagnostic capture. Historical assessed code head `267efc8`
passes the complete local gate: ten suite/provider reports, all 286 steps and
all 24 guide commands/strict observations. Its eleven CI contexts and 286
product steps also pass. The [implementation report](implementation.md#final-complete-gate-and-implementation-handoff)
preserves earlier failures and that original gate. The
[T081 increment](implementation.md#convergence-remediation-t081)
records its separate historical evidence. The
[T082-T083 increment](implementation.md#convergence-remediation-t082-t083)
records historical red/green and the failed local all-suite attempt, narrowed
reconciliation evidence and historical causal limits. Do not relabel that
attempt as a passing complete local gate.
That assessment added only two remediation tasks, preserving all
existing examples and runtime bounds. Its fresh broad integration passed 104/106:
SQLite preservation and PostgreSQL correction fail ordinary host disposal
with safe `composition_host_dispose_incomplete` evidence, despite passing
incomplete-request stop regressions. Its underlying disposal cause was not
established. Separately, accepted sets with 65 prerequisite service bindings
or 129 total executable exports failed lifecycle due to narrower runtime caps.
The T084-T085 increment preserves the original failures, adds focused
count/watcher red-green evidence and passes corrected both-provider composition
106/106; its report records complete-gate reconciliation. The composed host
disables unused JSON reload watchers before builder construction instead of
retaining them after source clearing. Cleanup/termination limits and
incomplete/uncertain refusal remain unchanged.

## Delivered validation, boot and relational preparation increment

The separately compiled consumer currently supports `invalid-composition`,
`deterministic-corpus`, `empty-boot`, `valid-dependency`, `disabled-missing`,
`ordered-migration`, `disable-reenable`, `failure-correction`, `health-impact`,
`bounded-stop` and `private-diagnostics`
on SQLite and owned disposable PostgreSQL. The three boot examples exercise
real processes, core endpoints and measured independent public module services,
including explicit dependency binding, prepublication unavailability and
disabled zero-callback behavior. Their Alpha/Beta definitions currently declare
no relational storage: history comparisons prove actual assignment/history
absence, not the later relational requirement.
The Alpha service uses the inclusive 128-character local identity; Beta
requires its exact owner-qualified binding, contract/semantic versions and
public operation result during actual supervised Start/readiness. Both the
C# harness and Python verifier require `dependencyIdentityComparisons`;
declaration acceptance or a shortened identity is insufficient.
Alpha additionally exports 126 unique operations: Beta requires all 127 exact
prerequisite bindings, versions and operation results during Start, while Beta
and NoStorage bring the executable total to 129. Both strict harnesses require
`prerequisiteServiceComparisons: 127`. The executable lifecycle retains the
approved 128-per-owner/1,024-total bounds; service bindings are not capped by
the separate module-dependency count.
Disabled deployments remain validated metadata without author callbacks or
store allocation. Their diagnostic prerequisite IDs may reach 2,048 across
32 manifests with 64 dependencies each; that bound does not consume or enlarge
the separate 1,024 enabled-edge limit. Actual both-provider integration covers
omitted/explicit-false full catalogs, an unrelated enabled no-storage module,
usable readiness, exact disabled observations, unchanged inventory and clean
owned exit. These are regressions in the existing compiled-consumer fixtures,
not a new operator command or twelfth public scenario.
The separate relational example uses real owner-local initial/additive
histories and original Alpha/Beta records. It executes validate, explicit stopped
prepare and ordinary serving; current preparation/serving do not replay
migrations or rewrite revisions. An invalid later recipe refuses before writes.
The additive upgrade deliberately interrupts the later owner after the first
owner commits. Automatic retry refuses without mutation; accepted 003
inspection/recovery must reconcile the unfinished owner before a fresh prepare
and serve attempt. Earlier assignments, original records and committed history
remain unchanged. Initialize is ephemeral and repeats on every fresh attempt;
it receives neither store access nor permission for business effects.

Registry format upgrade is a separate explicit stopped-maintenance operation.
Serving credentials have no CREATE authority, and startup never upgrades either
registry or module schemas. Complete-set backup/restore includes disabled
owners; legacy platform-only backup/reset/restore is refused once registry data
exists. Selecting an older complete set is an explicit operator data-loss
decision: restore only into a fresh owned target, never silently compensate a
failed startup. The real recovery tests preserve the newer original source.

The preservation example edits the private selection only after verified stop.
It executes three disabled/re-enabled cycles on each provider, requiring six
distinct supervised roots, six distinct actual workers and six current attempt
IDs. Every leg compares both original assignments/revisions, records, histories
and complete unrelated/platform state. Disabled owners have measured zero
author callbacks, constructors, registrars, runtime, cleanup and migrations.
A still-enabled dependent with its prerequisite disabled refuses unchanged;
there is no cascade, uninstall, hot unload or lifecycle administration endpoint.
An explicitly prepared additive release is a separate exception, not part of
unchanged re-enable: only its selected owner's target/revision may advance.
Stopping reverses entered owners and LIFO cleanup, scopes then provider, once.
Failed cleanup remains a failure, not authorization to erase durable evidence
or assume rollback. Use explicit inspection/forward recovery or a deliberately
selected full-set restore when required.
Configured HTTP shutdown drains gracefully for 500 ms inside the existing
one-second cleanup action, then aborts remaining incomplete requests. It does
not extend module cleanup or the 15-second stop/termination envelope.
Fixed private infrastructure-boundary records distinguish host stop/disposal,
service-provider disposal and storage disposal failures; the final
`composition_cleanup_incomplete` refusal and verified owned exit remain required.

After the Release build, invoke a delivered scenario, for example:

```bash
dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario valid-dependency
KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario disabled-missing
dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario ordered-migration
KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario ordered-migration
dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario disable-reenable
KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario disable-reenable
```

Each example owns its private selection, provider resources, supervised children
and bounded private observation socket. No anonymous business endpoint is added.
`all` executes each standalone scenario, requiring every case and semantic
observation before emitting its single bounded result. Passing examples do not
constitute feature acceptance or production readiness.

Health is current-attempt state, with individual monotonic sample age. Missing
or thirty-second-old samples become unknown before transitive propagation;
severity is failed, unknown, degraded, healthy. Independent roots remain
identifiable. Initial publication requires every enabled owner healthy.
Sweeps are serialized, start ten seconds after the preceding sweep finishes,
and retain the one-second callback/twenty-second sweep caps. Anonymous health
reads only the snapshot and real store probe, never author health callbacks.
It retains `version`, `state`, `component`, `correlationId` and `no-store`;
composition failure projects unavailable/host without an inventory endpoint.

Timeout/cancellation withdraw publication and use the shared cleanup reserve.
Seven uncooperative author boundaries require owned worker termination rather
than concurrent abandoned cleanup. Root and actual fixture-descendant exits
are checked separately; an unverifiable exit is explicitly refused. Trusted
in-process code is not sandboxed, arbitrary escaped descendants cannot be
guaranteed terminated, and plugins must not launch unmanaged external work.
The PostgreSQL example kills only its exact owned serving lease backend and
proves no automatic reacquisition; retry requires verified exit and a fresh
attempt. Storage acquisition reserves time inside the unchanged two-second
host cap to return a classified busy result rather than racing that cap.

Diagnostic capture checks real stdout/stderr, structured/transition events,
spans, fixed-label metrics and HTTP responses, including failure paths and raw
author writes/exceptions. No settings, credentials, unknown keys, physical
paths, inherited baggage or exception text is projected. Safe canonical owners,
missing prerequisites, known field tokens, fixed guidance and original 003
correlation/durability remain useful. Attempt output is capped at 256 records
plus one reserved terminal; steady output is 64/minute. Failed output uses
`restore_private_diagnostics`, unverified cleanup uses `inspect_preserved_state`,
and cancellation/deadline uses `verify_owned_exit_before_retry`. These are
ephemeral observations, not durable audit, settings administration or permission
to repair uncertain migration state.

## Prerequisites and safe setup

Use the [existing local guide](../../docs/development/local-development.md):
macOS 15+ arm64 or Ubuntu 24.04 x64, exact .NET 10.0.402/runtime 10.0.13,
Node 24.22.0/npm 11.19.1, Python 3.11+. Select versions in the invoking shell.
Do not scaffold new product tooling for these examples.
After implementation changes the manifests/locks, restore explicitly:

```bash
dotnet restore Kofte.slnx --locked-mode
npm ci
dotnet build Kofte.slnx --no-restore -c Release -warnaserror
```

PostgreSQL proof requires the existing local disposable-image fixture, explicit
opt-in and matching image tools. Never point scenarios at an existing developer/
production database/root. Fixture owns private directories, free ports,
unpredictable sentinel values and exact process/database/container identities.
Canaries/configuration/credentials stay out of arguments/reports; missing
infrastructure fails, never skips as success or falls back to SQLite.

The consumer is
`tests/contract/Kofte.Composition.Consumer`, referencing the separate
`Kofte.Composition.TestModule` library and only public platform/host/infrastructure
contracts. It owns provider fixtures and the entire prepare/serve/stop sequence.
The production host never gains a reference to test source or a loader path.

## Complete executable guide

The consumer accepts only fixed scenario/provider names. It creates private
configuration and supervised children itself; users never supply SQL, physical
paths, credentials, arbitrary type names or executable callbacks. Example
invocations:

```bash
dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario all
KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario all
```

Output is bounded sanitized JSON with named cases, boolean/exact-comparison
observations, positive meaningful assertion counts and safe outcomes; no
sentinels/settings/path/callback exception text. Missing expected case,
observation or nonpositive assertion fails `all`. Independent expected results
are fixture data, not generated by the resolver under test.

| Scenario               | Required observations, not mere counts                                                                                                                                                                                                            |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `empty-boot`           | Real host/core endpoints usable, zero optional activation/store allocation; retain v0/v1 empty behavior.                                                                                                                                          |
| `valid-dependency`     | Separately compiled Alpha/Beta public module behavior; exact phase/dependency order, own assigned data/history, zero pre-ready publication; no new anonymous business route.                                                                      |
| `disabled-missing`     | Two fresh actual hosts require explicit disabled records for both explicitly false and omitted Alpha/Beta owners, zero disabled callbacks/store effects and unchanged aggregate privacy; missing enabled implementation rejects. |
| `invalid-composition`  | Malformed/duplicate/incompatible manifests, missing/disabled/range/prerelease dependencies, self/multi cycles, unknown settings, mismatched/reserved/overlapping claims, unsupported execution and one-over limits. Zero preflight mutation.      |
| `deterministic-corpus` | Each independently expected graph/order/ordered rejection runs 100 times across ten discovery-order permutations; independent ready roots, chain, diamond, SCCs, diagnostic ordering/truncation and real cultures en-US/tr-TR.                    |
| `ordered-migration`    | Real clean-install/additive-upgrade histories and sentinels on both providers; no-storage has no binding/call; all recipe preflight occurs before first write; verify no pending migration/revision rewrite on serve restart.                     |
| `disable-reenable`     | Three full disable/stop/restart/re-enable/stop cycles, fresh processes each time; compare exact assignments, original records and applied histories, unrelated owner/platform state unchanged. Dependency-invalid disable refuses without writes. |
| `failure-correction`   | Fixed configuration/Initialize/Start/health/migration faults, new-attempt corrected retry, earlier committed state retained, actual explicit recovery when required; every named example succeeds after only its named correction.                |
| `health-impact`        | Own healthy/degraded/failed/unknown, freshness expiry, prerequisite propagation, independent roots, no stale previous-attempt success; aggregate ready/503 projection retains schema and no inventory.                                            |
| `bounded-stop`         | Cancellation/ignoring-token callbacks, maintenance contention/lost authority, reverse partial-entry cleanup, actual killed/reaped owned process, refusal when termination cannot be verified; elapsed work plus cleanup within total caps.        |
| `private-diagnostics`  | Captured stdout/stderr/events/spans/metrics/aggregate responses with seeded settings/credential/path/exception canaries; zero leaks and expected useful refusal/transition/correlation/guidance records, including failed boundaries.             |

## Operator sequence

Supply a trusted compiled deployment and a private untracked
`KOFTE_MODULES_FILE`; see [operator contract](contracts/operator-v1.md).
Initialize the **owned new** platform explicitly using existing setup;
upgrade registry explicitly before relational preparation. The fixture then
executes validate → stopped prepare → ordinary serve → observe → controlled
stop. Preparation never enables modules; serve has no provisioning credential
and never migrates. Do not chain registry upgrade/preparation into ordinary
startup or use developer data for the examples.

Disable/re-enable edits only the private selection file between stopped
attempts. Never delete a module store, registry row, history or lease to make
a case pass. A deliberately interrupted migration must use the accepted
003 explicit recovery workflow or remain blocked. A selected older full-set
restore includes an explicit data-loss decision, not silent repair.

In the private operator output, disabled owners retain metadata discovery and
add `discovered / disabled / composition_disabled` with fixed
`edit_selection_while_stopped` guidance and the current attempt. Do not infer
disabled intent merely from missing callbacks. The `disabled-missing` case
requires two `explicitDisabledObservations`, two `omittedDisabledObservations`
and two `freshDisabledAttempts` on each provider; standalone and `all` results
fail if any required observation is omitted or decremented. No module details
are added to anonymous health, and disabled observations are not activation.

## Required local and CI verification

After implementation wires the cases, the existing named runners are the
authoritative fixture-owning commands, not a parallel ad hoc test runner:

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

Focused .NET selector families to add in the **existing** projects:
`CompositionResolverTests` / `CompositionLifecycleTests` in unit projects;
`CompositionProviderTests` under storage integration with `Provider=sqlite|postgres`;
`CompositionPublicContractTests` in the public C# contract project. New
test-owned projects are built by quality and required by contract/suite manifest
checks, with minimum expected tests/cases; the wrapper must fail if omitted.
Recovery/privacy selectors include actual composition cases, not only old 003
tests. Focused recovery/privacy use a single provider/suite/`Feature=composition`
trait query; xUnit query filters must not be mixed with simple class filters.
The startup gate retains all requested empty-host warm cycles and additionally
runs two real compiled Alpha/Beta/no-storage deployment restarts with the built
web application, exact persisted unrelated fixture and separately validated
startup/page/cleanup observations inside the unchanged 285/300-second envelope.
Exact final paths/selectors and measured results belong in tasks/
implementation evidence, not invented passing output here.

## Acceptance mapping and evidence

| Scenario coverage / requirements / outcomes | Required suite and meaningful assertion                                                                                                                                                                     |
| ------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| US1.1-4; FR-001/002/019; SC-001             | Unit + both-provider integration + public contract: real empty/valid/disabled/missing process boots and independent module behavior/callback/store absence.                                                 |
| US2.1-4; FR-003-007; SC-002                 | Unit + public contract: full expected graph/diagnostic corpus including all bounds, collisions and zero effects, repeated/permuted through author and host consumers.                                       |
| US3.1-4; FR-008-011/016; SC-004             | Both-provider integration/recovery/contract: exact local histories and preflight barrier, dependency order, no-storage, process contention and killed/lost-authority outcomes.                              |
| US4.1-4; FR-012/013/015; SC-003             | Both-provider integration/recovery/public examples: three complete cycles with original assignment/data/history plus unrelated-store comparisons, invalid selection guard and reverse cleanup.              |
| US5.1-4; FR-014-018; SC-004-006             | Unit + both-provider integration/recovery/privacy + public examples: fresh state/dependency impact, corrected retry, current correlations, all fault-stage/time/cleanup cases and actual captured channels. |
| FR-019/020; all outcomes                    | Public contract and named full gate/CI: every future guide case mandatory; retain 001-003 manifest/constraints/ownership/provider/recovery/browser/startup/failure-propagation evidence.                    |

Record exact commands/results, assessed revision/CI contexts, selected supported
environment, monotonic elapsed budgets, process-exit evidence and each
acceptance comparison. Passing unit tests alone does not prove actual provider
effects or a killed process. Baseline historical measurements are not new 004
evidence. No new UI, authorized user/MCP/job/event or deployment operation
exists here; retain existing browser checks while reserving those future
feature/reference-module/Kubernetes release gates explicitly.
