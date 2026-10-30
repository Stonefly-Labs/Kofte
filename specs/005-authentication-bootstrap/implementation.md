# Implementation Evidence: 005 - Authentication and Bootstrap Administration

**Result:** implement running; 96/100 complete (T001-T096). Actual `81e1805` Linux passes nine product contexts, including the entire propagation proof and its healthy PostgreSQL baseline in 691.368s; only retained composition still times out. The next fixture correction reuses the native parent's owned service across independent consumers, with separate databases/roles and verified child exit before release. Its live-target regression fails before repair and passes afterward; all 86 retained composition cases pass locally in 466.741s under the unchanged 900-second bound. Exact full local qualification at frozen `81e1805` is still running; no complete-gate pass or repaired-source CI pass is claimed. T097-T100 remain unchecked.
**Stage / date:** implement, 2026-10-26; continued 2026-10-30.
**Assessed baseline:** published two-phase warmup correction `81e1805d8c6ae4ab8bfac150454f83b9d4faf706`; actual Linux merge `1f1e0bef73030821b526bfe6d8b9e81e1be85ce7`. The subsequent parent-service reuse correction is distinguished below; earlier assessments retain their original baselines.
**Branch / directory:** `feature/005-authentication-bootstrap` /
`specs/005-authentication-bootstrap`.
**Checkpoint:** [draft PR #8](https://github.com/Stonefly-Labs/Kofte/pull/8).

The initial blocked assessment below is retained as history. The
[source-provenance follow-up](#t002-follow-up-source-provenance-resolved)
resolves that blocker and resumes the original implement stage. The later
adoption and setup sections record the actual code increments; authentication
is now wired but feature 005 is not accepted.

## T097 in progress: current full-gate and assessed-head failures

### Reuse the native parent's service across independent consumers, 2026-10-30

Actual [run 41013543462](https://github.com/Stonefly-Labs/Kofte/actions/runs/41013543462)
at `81e1805` finishes **nine of ten** product contexts successfully, including
both complete integrations, recovery, browser, privacy, startup and propagation.
[Repository run 41013543562](https://github.com/Stonefly-Labs/Kofte/actions/runs/41013543562)
passes. Merge `1f1e0bef73030821b526bfe6d8b9e81e1be85ce7` has verified main
`3e416813233c9d291eb20cf72d5882dbddf3e1b6` and feature
`81e1805d8c6ae4ab8bfac150454f83b9d4faf706` parents.

Complete propagation passes in **1100.081s**
(`failure-propagation-none-94c7da2dec984a848ee46884df16f659.json`).
Healthy quality/PostgreSQL/browser baselines pass in 85.300s/691.368s/88.314s.
The entire PostgreSQL baseline, under its original 900-second bound, contains
native 121/229.307s, storage **212**/290.511s and composition 59/171.194s,
zero skipped. All five actual test/formatter/build/unavailable-PostgreSQL/
missing-Chromium injections and owned cleanup pass. The added preparation
regression increases storage by one case per provider; nothing is omitted.

Contract still fails retained composition after 900.066s
(`contract-both-aecf549da74c4f6d9e16039cbd8d0b21.json`); the overall context
takes 1479.049s. The bounded follow-up independently passes all 24 public
composition invocations in 978.462s
(`composition-diagnostic-both-9c1f5a5973504fe4867bbfa826b259b3.json`).
This preserves the failed required command, not a replacement pass.
The preceding `13d4e9c` assessment also times out at 900.065s, with all
follow-up recipes passing in 957.429s; timing variability is not a waiver.

The public consumer's process-local service reuse still creates a fresh Docker
service for **each independent consumer process**, even when the native owning
assembly already has one. The harness now borrows that parent's service only
for admitted, source-catalogued PostgreSQL scenarios with no supplied external
connection. It passes credentials only through private child environment, never
arguments/results. Existing external-service fixture provisioning creates fresh
databases and application/provisioning roles for every child fixture. Standalone
public commands and invalid/unadmitted selectors retain their existing paths.
No public product command, schema, deadline, retry, case or collection changes.

The parent retains its borrow through actual child stop and bounded output
drain; unverified cleanup cannot release it. Each child verifies/removes only
its own database/roles, and the original parent owner still checks exact empty
server state and Docker ownership before disposal. The contract assembly's
existing nonparallel execution is unchanged.

New `IndependentConsumerBorrowsOwnedServiceWithoutSharingOrLeakingTargets`
observes a real transient child database on the parent's server, then requires
the exact original database/role counts and unchanged parent module snapshot.
Before sharing, the public consumer succeeds but this assertion **fails**:
`composition_consumer_did_not_borrow_owned_service`. After repair the same
command **passes 1/zero skipped, 8.713s**:

```text
dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-method '*IndependentConsumerBorrowsOwnedServiceWithoutSharingOrLeakingTargets' --minimum-expected-tests 1 --output Detailed --show-stdout None --show-stderr None
```

The complete strengthened selection **passes 86/zero skipped in 466.741s**
under the original owned 900-second runner:

```text
dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*Composition*' --output Detailed --show-stdout None --show-stderr None --minimum-expected-tests 86
```

Safe diagnostic report
`composition-shared-parent-complete-both-23d2af78e4d94707a5991541c460767a.json`
takes 466.966s including version checks. It assesses the working-tree correction
over `81e1805`, remains targeted/completeGate false and is not final qualification.
The earlier 85-case selection took 500.139s; the new case strengthens rather
than reduces coverage. Other isolated qualification is active on this macOS
host, so this is not an exclusive-CPU controlled benchmark or a Linux repair
claim.

Focused `dotnet format Kofte.slnx --verify-no-changes --no-restore --include
tests/integration/Kofte.TestSupport/CompositionConsumerHarness.cs
tests/contract/Kofte.Platform.ContractTests/Composition/CompositionFixtureTests.cs`
passes. `dotnet build Kofte.slnx --no-restore -c Release -warnaserror` passes
with zero warnings/errors; all 165 repository Python tests pass. Full
repaired-source local/Linux qualification remains required.

### Separate runtime startup from exclusive warmup, 2026-10-30

Actual [run 41010829036](https://github.com/Stonefly-Labs/Kofte/actions/runs/41010829036)
assesses merge `250825b1a6af9c801c5fbe1db50fd3766c8e911b`, whose verified parents
are main `3e416813233c9d291eb20cf72d5882dbddf3e1b6` and feature
`13d4e9c88577745e975a8e1630f8d9d77b8a7b0b`.
[Repository run 41010828872](https://github.com/Stonefly-Labs/Kofte/actions/runs/41010828872)
passes. Complete independent PostgreSQL integration passes in **613.840s**:
native 121/231.738s, storage 211/230.213s and composition 59/151.724s, zero
skipped (`integration-postgres-3b7f297bd99543c384472afe3082a13c.json`).

Its isolated healthy baseline instead fails in **768.419s**, below 900 seconds.
Native 121 passes in 317.961s and composition 59 passes in 228.672s, but storage
fails `PostgresRegistrationConcurrencyTests.TwentyRoundsOfTenSeparateProcessesKeepActualOwnershipUnique`
with **`storage_busy; operation=acquire`**
(`failure-propagation-none-785b6f14d7404e98bc76949469e7d2d4.json`).
The startup overlap had also overlapped the exclusive **warmup** maintenance
acquisitions, which are outside the intended registration race. That is a
fixture regression, not a reason to lengthen the two-second acquisition budget
or add warmup retries.

The corrected fixture starts all ten fresh processes and validates their
startup messages, prepares each worker's original warmup lease **serially**,
then validates every waiting message before releasing the unchanged registration
barrier. Cold runtime startup overlaps; exclusive pre-race initialization does
not. All twenty rounds, ten actual registration contenders, 400 fresh children
per provider, original retries, 45-second child/60-second round budgets,
exclusive collection, ownership assertions and verified cleanup remain.

New `FreshWorkerWaitsForExplicitPreparationBeforeAcquiringMaintenance` holds
a real parent lease while a fresh child starts, then releases it, explicitly
prepares the child and completes actual registration with exact inventory and
owned exit. The two-provider command below first **fails both cases** with the
same `storage_busy; operation=acquire`; both corrected cases then pass in the
combined selection:

```text
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-method '*FreshWorkerWaitsForExplicitPreparationBeforeAcquiringMaintenance' --max-threads 4 --minimum-expected-tests 2 --output Detailed --show-stdout None --show-stderr None
```

The combined complete relevant selection **passes 6/zero skipped, 103.323s**:

```text
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-method '*FreshWorkerWaitsForExplicitPreparationBeforeAcquiringMaintenance' --filter-method '*TwentyRoundsOfTenSeparateProcessesKeepActualOwnershipUnique' --max-threads 4 --minimum-expected-tests 6 --output Detailed --show-stdout None --show-stderr None
```

The retained PostgreSQL race cases take 30.619s/37.955s; SQLite takes
12.940s/16.390s. These are case timings, not a substituted whole-gate result.
Focused `dotnet format Kofte.slnx --verify-no-changes --no-restore --include
tests/integration/Kofte.Storage.IntegrationTests/Registry/RegistrationConcurrencyTests.cs
tests/integration/Kofte.Storage.IntegrationTests/Registry/RegistrationWorker.cs`
passes; `dotnet build Kofte.slnx --no-restore -c Release -warnaserror` passes
with zero warnings/errors, and all 165 repository Python tests pass.

The earlier independent exact all-suite run at clean published `e371245`
has also finished, exit 1: quality 37.806s, unit 48.780s, SQLite 456.033s,
PostgreSQL 489.619s, recovery 976.022s, browser 76.032s, privacy 418.617s,
startup 12.871s and complete propagation 772.450s pass. Contract fails in
708.925s: `SqliteCompositionPublicContractTests.AllRequiresEveryActualStandaloneScenarioAndItsCompleteSemanticEvidence`
returns `composition_consumer_failed`
(`contract-both-7fa0a7d5b68944ac8f78bcc974ef6504.json`).
All ten reports retain `scope:all` and **completeGate false**. That report
does not identify the inner recipe; the later 85-case pass does not establish
its historical cause or replace the failed full gate.

### Forced port-collision repair and measured worker startup, 2026-10-30

The final `e371245` Linux assessment finishes **nine of ten** product contexts
successfully. Contract report `contract-both-596cca97b40549fab6fcd24b7ac49b80.json`
passes in 2755.418s: public 265/18 required classes in 455.790s, retained
composition 84 in 780.754s, ten identity guides per provider, all twelve
composition invocations per provider, native OpenAPI and HTTP observations,
with zero native skips. No post-failure diagnostic step was needed. Only the
isolated healthy PostgreSQL propagation baseline remains failed at that head.

Actual timing-source [run 41007722188](https://github.com/Stonefly-Labs/Kofte/actions/runs/41007722188)
at `5ee4367` confirms merge `6a4f6f3efe15d2d08b383b408079fdeda4cecf8c`,
with main `3e416813233c9d291eb20cf72d5882dbddf3e1b6` and feature
`5ee43672cfc82f59659274f3eddc3d1c4c8d579f` parents.
[Repository run 41007722220](https://github.com/Stonefly-Labs/Kofte/actions/runs/41007722220)
passes. Completed native timing summaries are measured, not unavailable;
the failed nested baseline retains them after strict revalidation.

| Actual Linux safe report | Evidence |
| --- | --- |
| `integration-postgres-1482e7f3531d449795c476f83ea20233.json` | Passed, 884.191s: native 121 in 315.439s, storage 211 in 365.569s, composition 59 in 202.969s; zero skips. |
| `failure-propagation-none-eb6c2b16f7f849cca60acf28b965ef63.json` | Failed, 1038.854s. Healthy PostgreSQL native 121 passes in 330.072s and storage 211 in 412.408s; composition is interrupted after 157.463s at the unchanged aggregate 900-second bound. |
| `recovery-both-55e77a46fb3b45ee92cec31a3dc28a91.json` | Failed, 439.325s. SQLite native 50 and storage 73 pass. Public `SqliteCompositionMigrationContractTests.ActualPublicCommandsPrepareUpgradeAndServeWithoutRewritingHistoryOrOriginalData`, scenario `ordered-migration`, fails with `composition_configuration_invalid`/`composition_test_command_exit_expected_`; later recovery phases are unexecuted. |

The measured isolated native case sum is 825.221s; identity preparation accounts
for 223.215s and OIDC budgets 173.230s. The storage sum is 601.496s, of which
391.205s is retained non-identity work. These are overlapping case sums, not
command wall time. Source inspection identifies a serial pre-barrier warmup
inside the retained twenty-round, ten-process registration proof: each new
worker is awaited before the next worker is even launched.

The fixture now launches all ten fresh workers first and validates all ten
ready messages before releasing the **same** registration barrier. All twenty
rounds, same/distinct-owner assertions, fresh processes, ten simultaneous
contenders, per-round deadlines, retries, exclusive collection and cleanup remain
unchanged. No provider phase, collection limit or product scheduling changes.
The exact two-case PostgreSQL command before/after is:

```text
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-trait Provider=postgres --filter-method '*TwentyRoundsOfTenSeparateProcessesKeepActualOwnershipUnique' --max-threads 4 --minimum-expected-tests 2 --output Detailed --show-stdout None --show-stderr None
```

Both versions pass 2/zero skipped, executing the same 400 fresh children.
Before: same-owner 40.824s, distinct-owner 45.018s; runner 86.862s.
After: 22.075s/25.191s; runner 48.242s. SQLite's unchanged corresponding
two-case proof also passes, zero skipped, in 26.881s with `--no-build`.
This is measured local setup improvement, not yet proof that the Linux whole
baseline fits its budget. Other isolated qualification is running on the same
18-logical-CPU macOS host; no controlled exclusive-CPU benchmark is claimed.

The configuration rejection also reveals a genuine fixture defect.
`ServingIdentity` allocates and releases its nominal web port; subsequent
`CompositionFixture` host allocation can receive that same ephemeral port.
The product correctly rejects equal host/web ports in `BootstrapConfiguration`.
The new fixture regression supplies that exact candidate through the test
helper's preferred-port argument: before exclusion, actual validation exits 2
instead of 0, deterministically. The fixture now keeps that candidate bound
while allocating one replacement, ensuring a different host port without
retries, released-port races between the two allocations or changed product
validation. It does not claim to reserve the chosen port until child startup.
Historical CI did not retain the actual port pair, so this reproduction is not
claimed as retroactive proof of every prior configuration rejection.

The forced collision plus original migration/preservation cases pass on both
stores: 6/zero skipped in 62.589s
(`composition-port-regression-both-e2ee3448fe0545c48a26dda5f5b19e72.json`).
Two initial diagnostic wrapper invocations fail before executing native tests
because of an incorrect constant/positional arguments; they are not product
failures. The corrected wrapper uses the actual keyword-only runner API.
The full retained composition selection now requires **85**, not 84, including
the new forced-collision case under the same 900-second command bound.
The complete selection now **passes 85/zero skipped** in 500.139s
(`composition-port-complete-both-e5e444675c564f4db8df88e530cd051e.json`,
500.259s including version checks), under the unchanged owned 900-second
runner. This includes every original case and the forced-collision regression;
it is not a full all-suite pass. Whole-solution
`dotnet build Kofte.slnx --no-restore -c Release -warnaserror` passes with zero
warnings/errors. Focused `dotnet format Kofte.slnx --verify-no-changes
--no-restore --include tests/integration/Kofte.TestSupport/CompositionFixture.cs
tests/contract/Kofte.Platform.ContractTests/Composition/CompositionFixtureTests.cs
tests/integration/Kofte.Storage.IntegrationTests/Registry/RegistrationConcurrencyTests.cs`,
repository validation, all 165 Python tests and `git diff --check` pass.
The worker's separate cold-initialization maintenance lease still completes
and is released before its ready message; no registration can begin until
all ten messages pass validation. Full repaired-source local/CI qualification
remains required; engineering continues on the original draft PR.

### Whole propagation verified; isolated Linux baseline measured, 2026-10-30

The exact `python3 scripts/verify.py --suite failure-propagation
--disposable-postgres` passes on clean published `e371245`, exit 0, in
**730.130s** (`failure-propagation-none-ed87d4d127094e8f8dcb479651e21ba2.json`).
Healthy quality/PostgreSQL/browser baselines pass in 46.868s/458.233s/77.843s;
all five test/formatter/build/unavailable-PostgreSQL/missing-Chromium injections
pass in 7.144s/16.639s/32.836s/80.493s/7.022s. Every required ordered negative
phase and owned cleanup is validated. This is a complete **propagation suite**
pass, not a complete all-suite gate. The exact all-suite command has started
separately in an isolated clean checkout of that same published revision,
using the existing pinned toolchain and unchanged selectors/deadlines.

Actual product [run 41004206190](https://github.com/Stonefly-Labs/Kofte/actions/runs/41004206190)
at `e371245` passes eight observed contexts: quality, unit, integration-sqlite,
integration-postgres, recovery, browser, privacy and startup. Contract is still
being observed; failure-propagation fails. Repository
[run 41004206193](https://github.com/Stonefly-Labs/Kofte/actions/runs/41004206193)
passes. The completed unit job's checkout log and Git object verify merge
`b5f38609dd781edcc456c2f61b9ea5c464e46b62`, with parents
`3e416813233c9d291eb20cf72d5882dbddf3e1b6` and
`e371245d41465a47c072dccd24cb33a88f8a5abe`.

The new timeout projection retains the validated actual failed child, rather
than losing its phase. Linux report
`failure-propagation-none-c51a722cb44c4abd9647a19b06bb51b4.json` fails after
1034.753s: quality passes, then the **healthy whole PostgreSQL baseline**
reaches 900.414s. This is not the repaired unavailable-service injection.

| Same three required PostgreSQL phases | Independent Linux context | Isolated Linux propagation baseline |
| --- | --- | --- |
| Native | 121 passed, eight classes, zero skipped; 239.296s | 121 passed, eight classes, zero skipped; 307.677s |
| Storage | 211 passed, eleven classes, zero skipped; 279.869s | 211 passed, eleven classes, zero skipped; 410.793s |
| Composition | 59 passed, zero skipped; 161.550s | Interrupted after 181.474s when the unchanged aggregate parent bound expires; not passed |

The independent integration report is
`integration-postgres-d93226be91ea43ddba9d0f7edade240a.json`, total 680.878s.
The isolated child spends 718.470s in the first two passing commands, leaving
less than 182 seconds for composition. These are different hosted jobs; the
comparison identifies the phase distribution, not a proven mechanism for the
slowdown. The formerly failing invalid-disable case passes in this assessment;
the diagnostic assertion split is not claimed as a behavioral repair.
Actual Linux profile: x86_64, four logical CPUs, 16,760,344,576 bytes RAM,
kernel `7.0.0-1017-azure`, Python 3.12.3, SDK 10.0.402, Node 24.22.0/npm 11.19.1.

The continuing diagnostic now projects native `time` values before discarding
private XML: summed seconds for exact source-selected required class names,
and only a numeric aggregate for every other class. No XML name, parameter,
output or unknown class identifier is retained. Parallel case sums are not
command wall time; comparing both distinguishes test work from build/startup
overhead. Missing, malformed, non-finite, negative or over-one-day case timing
is explicitly unavailable, not guessed as zero. The isolated evidence validator
revalidates exact class sets, finite numeric bounds and aggregate consistency.
Existing `nativeEvidence`, case/class minima, skip rejection, complete-baseline
requirements and every deadline/selector remain unchanged.

Four focused tests fail before the timing projection/revalidation, then pass.
The full repository Python suite passes 165 tests; repository validation and
`git diff --check` pass. No product source, dependency or workflow selection
changes in this timing increment. T097-T100 and the active implement ledger
remain open; engineering continues rather than requesting approval.

### Full local qualification and portable refusal repair, 2026-10-30

The exact `python3 scripts/verify.py --suite all --disposable-postgres
--warm-starts 10` runs to completion on clean, published `0c79825`, without
source changes, competing tests or interruption. It **fails**, exit 1:
nine suite groups pass, but failure-propagation does not. Every report has
`scope: all` and `completeGate: false`; no diagnostic pass replaces this result.

| Actual local safe report | Result |
| --- | --- |
| `quality-none-b70c36752265494388bfcaf47e744ddf.json` | Passed, 35.144s: repository/Python/whitespace, locked restores, complete .NET format/build, Prettier/ESLint/TypeScript, generated contracts, workspace builds and dependency review. |
| `unit-none-4044da6c5bc74458b145172bce344da9.json` | Passed, 48.994s: host 309, platform 223, retained composition host 149/platform 94, web/package/capture/Python; zero native skips. |
| `integration-sqlite-4899d64052f84bf2956128ad656c82c9.json` | Passed, 488.147s: native 211/13 required classes, storage 236/11 classes and composition 57; zero skipped. This includes both historically failed storage methods. |
| `integration-postgres-ccc517c17b594f439c5662630f52be61.json` | Passed, 480.688s: native 121/eight required classes, storage 211/11 classes and composition 59; zero skipped. |
| `recovery-both-ee26a1a49e364c29a9efd90982ba47e1.json` | Passed, 985.489s: both complete native/storage/public/composition/browser recovery selections and owned cleanup/output scans. |
| `contract-both-1ac6d69da28a4536bce6b05aed43fd13.json` | Passed, 1856.512s: public identity/platform 265/18 classes in 295.449s; retained composition 84 in 479.664s; all individual composition recipes plus `all`, identity guides, native OpenAPI and HTTP contracts on both providers; zero native skips. |
| `browser-none-b976311569574fc6b3b66d24c22188f4.json` | Passed, 72.717s: root/Development browser discovery/execution, actual native identity browser facts on both providers, canaries and verified cleanup. |
| `privacy-both-949e10c68f874b8b883ac50e9c01b236.json` | Passed, 443.937s: both-provider native/public/storage/composition/browser captures and strict supervisor scan. |
| `startup-none-378f55c214ca41a0a001d7ffdbb6e333.json` | Passed, 15.542s: ten empty warm starts, composition restarts and actual timing observations. |
| `failure-propagation-none-ea955e2cdad44b628da0b3aefa1b1484.json` | Failed, 1556.013s. Isolated quality baseline 39.308s, entire PostgreSQL baseline 477.687s, browser baseline 78.444s and test/formatter/build injections pass. The unavailable-PostgreSQL injection reaches the unchanged parent 900-second bound at 900.296s. Missing-Chromium injection is not executed. |

The actual Linux product
[run 40992754318](https://github.com/Stonefly-Labs/Kofte/actions/runs/40992754318)
passes **seven of ten** contexts: quality, unit, integration-sqlite, recovery,
browser, privacy and startup. Repository
[run 40992754152](https://github.com/Stonefly-Labs/Kofte/actions/runs/40992754152)
passes. The contract checkout log and fetched merge object verify merge
`a0a260dbab502a15e65d584c3a50c228d988fdcf` with main
`3e416813233c9d291eb20cf72d5882dbddf3e1b6` and feature
`0c7982541f5ab1f617e8a9facb278118602abc2d` parents.

| Actual failed Linux report | Exact failure |
| --- | --- |
| `integration-postgres-dd6cfe01075e4617a72b81867c3b49bb.json` | Native 121/eight classes and storage 211/11 classes pass without skips. Composition fails `PostgresCompositionPreservationTests.ThreeStoppedSelectionCyclesPreserveExactOriginalStoresAndUseFreshProcesses`, scenario `disable-reenable`, with `composition_test_invalid_disable_cascaded_or_executed`; total 679.052s. The old combined assertion does not identify a missing dependency code versus a forbidden activation stage. |
| `contract-both-690b1c33453d40cbb6919fe79b5285e0.json` | Public identity/platform 265/18 classes passes in 541.890s, zero skipped; retained composition reaches 900.063s without completed XML or retained method names; total 1478.896s. |
| `failure-propagation-none-9634a526e45641dfbc4bdc0140724cfd.json` | Isolated quality passes in 131.584s, then the whole PostgreSQL baseline reaches 900.366s; total 1035.305s. This is a different phase from the local unavailable-service injection. |

The local propagation failure now has a concrete transport reproduction.
The old fixture reserves a TCP port by binding without listening. On this
macOS profile, connecting to that owned non-listening socket **times out**
rather than promptly refusing. A one-second real socket regression fails
before the repair. The exact old unavailable child also exceeds its unchanged
900-second parent budget: native fails after 426.330s; storage is interrupted
after another 473.741s
(`integration-postgres-fdbae0dd281345dda67386aa0201a157.json`).
The fixture's separately bounded administration legitimately allows up to
30 seconds; repeated transport blackholes consume the aggregate budget.

The injection now owns a listening socket and a supervised thread that
immediately closes every accepted connection, without reading data or speaking
PostgreSQL. It deterministically refuses the database protocol while retaining
exclusive port ownership; it cannot race with a real service taking a released
port. Teardown closes the listener, joins the worker and explicitly fails on
unverified cleanup or listener failure. No runtime, administration, test,
contender, retry or outer deadline is increased. The real port regression
now passes three consecutive refused connections.

The corrected real child completes in **86.374s**, with all three original
serial commands failing as required: native 15.850s, storage 4.258s and
composition 66.093s
(`integration-postgres-3d072695806942d1b2b8c2ddcfcfb16b.json`).
`checked_evidence` validates the exact ordered commands, nonzero exits and
`command_failed` outcomes against every expected follow-up. This is a passing
**focused negative proof**, not a passing integration suite or complete
propagation gate; the child report correctly remains failed.

Parent timeout handling also stops the child **before** reading its report
and reuses the existing strict evidence validator. A validated failed child
snapshot is retained alongside `failure_exercise_timeout`, never accepted as
a green baseline. A controlled regression first fails for missing evidence,
then passes; private canary fields are still discarded.

The preservation assertion is split into the same two mandatory conditions:
the exact disabled-dependency reason and zero registration/initialization/
migration/start/publication. Missing-reason failures now retain a closed
root-code category separately from forbidden activation. This does not claim
the Linux rejection is repaired. An initial local probe correctly refuses
PostgreSQL because its wrapper stripped the disposable-admission flag
(`composition-preservation-diagnostic-both-bb525f00fa184e0c8744305ea9ebaba7.json`);
that is not a product regression. The corrected explicitly admitted probe
passes both cases, zero skipped
in 14.263s
(`composition-preservation-diagnostic-both-425d49f14c5a4214be466fb8848e74df.json`).
The exact command is `dotnet test --project
tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore
-c Release --filter-method
'*ThreeStoppedSelectionCyclesPreserveExactOriginalStoresAndUseFreshProcesses'
--max-threads 4 --minimum-expected-tests 2`, with disposable PostgreSQL
explicitly admitted.

After a future failed Linux contract job, CI additionally profiles each
existing public composition recipe and `all` through the same strict
600-second helper, within a separate bounded diagnostic window. Only ordinary
safe verifier JSON is uploaded. The original failed context, 900-second
required command and incomplete qualification remain failed; diagnostic
timings do not substitute for the gate.

Post-repair `dotnet build Kofte.slnx --no-restore -c Release -warnaserror`
passes, zero warnings/errors. `dotnet format Kofte.slnx --verify-no-changes
--no-restore --include
tests/contract/Kofte.Composition.Consumer/Scenarios/PreservationScenarios.cs
tests/contract/Kofte.Composition.Consumer/Scenarios/MigrationScenarios.cs`
passes. `python3 scripts/check_repository.py`,
`python3 -m unittest discover -s scripts/tests -v` (**161 tests**, 7.892s),
`git diff --check` and `npx --no-install prettier --check
.github/workflows/product-checks.yml` pass. The exact embedded diagnostic
Python also compiles. Full qualification and actual repaired-source CI remain
required; this repair checkpoint does not complete the implementation stage.

### Continued repair: reuse owned PostgreSQL services, 2026-10-30

The original branch/draft PR, clean worktree and passing 16/16 and 36/36
checklists were verified before resuming the active implementation through
the driver. No extension hooks are configured.

A complete, unchanged `composition-public-contracts` diagnostic execution
passes all 84 selected cases, zero skipped, in 823.858s within its original
900-second command envelope.
Its safe report is
`composition-duration-probe-both-3f0e6666357f44e38fab02c50ad75d45.json`;
private XML is projected only into source-allowlisted method names and numeric
durations, then discarded. The PostgreSQL consumer's `all` case takes
351.171s versus SQLite's 100.822s. This is diagnostic performance evidence,
not the complete local gate or a replacement for the failed Linux command.

The independently executed composition consumer lacked the assembly-owned
service used by its parent test process. Its scenarios therefore repeatedly
started/removed owned servers even during `all`. The consumer now owns one
lazy `PostgresAssemblyFixture` for the whole valid PostgreSQL invocation.
Every scenario still owns a unique database, application/provisioning roles,
ownership records and verified disposal. The owner checks for residual
databases/roles/client sessions and disposes **before** the success envelope.
Ordinary host fixtures and compatibility cases now opt into the existing
assembly-owned service too. Actual service-outage cases continue using
dedicated `CreateAsync` services, not a shared server.

No product source, strength, deadline, retry, contender count, required
selection or serial runner is changed. The new host regression requires a
common service but distinct databases/roles, verifies their removal through
the surviving sibling's catalog connection and proves that sibling remains
healthy. Restoring the original non-sharing helper makes this regression fail
(`native-fixture-sharing-red-postgres-18e115dfaf9c49f281956dfd7b8dff2d.json`);
the repaired helper passes its rebuilt one-case execution in 5.903s.

The complete repaired composition selection passes **84/zero skipped in
551.735s**, with PostgreSQL `all` reduced from 351.171s to **148.248s**
(`fixture-reuse-proof-both-bd8f4f373e784c8b9226dfe86ce8f970.json`).
That combined diagnostic attempt still fails its native PostgreSQL selection:
five methods receive actual 503s instead of expected 200/303/409 responses,
in 368.247s. They are the missing-context/designation enrollment cases,
warmed-store-authority/corrupt-ticket case, recovery-key-custody case and
invalid-delivered-OIDC-evidence case. A subsequent private local-owner phase
trace passes all **121 native cases/zero skipped/eight required classes in
204.650s**. It does not reproduce an unexpected healthy failure and does not
establish the cause of the preceding five failures. The source instrumentation
and hardcoded private output path are removed before qualification/publication.

`dotnet build Kofte.slnx --no-restore -c Release -warnaserror` passes with zero
warnings/errors. Focused `dotnet format Kofte.slnx --verify-no-changes
--no-restore --include` for the four changed C# files passes. After removing all
temporary instrumentation, the rebuilt ordinary-store/ownership/compatibility/
actual-outage selection passes **11/zero skipped in 13.579s**:
`dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-build
--no-restore -c Release --filter-class '*PostgresStoreTests' --filter-class
'*PostgresOwnershipTests' --filter-class '*PostgresStorageCompatibilityTests'
--filter-class '*PostgresFailureTests' --max-threads 4
--minimum-expected-tests 11`.
Its safe report is
`native-fixture-sharing-final-postgres-ae2a67c63edf4d86a8c36aa817038377.json`.
No product source, runner, selector, scheduling or budget change is retained.

The subsequent unchanged-source Linux
[run 40988500306](https://github.com/Stonefly-Labs/Kofte/actions/runs/40988500306)
at `b15f2f3749d376ba3cfa1f7b345c4f2cb3c55ed6` passes **nine of ten** product
contexts, including failure-propagation; repository
[run 40988500274](https://github.com/Stonefly-Labs/Kofte/actions/runs/40988500274)
also passes. The propagation report
`failure-propagation-none-5a9d54cf3d34427a9d951d6545f0f63c.json` passes in
1254.993s: its entire serial PostgreSQL baseline passes in 760.136s
(native 120/zero skipped, 285.013s; storage 211/zero skipped, 315.198s;
composition 59/zero skipped, 159.566s), followed by browser baseline and all
five real failure injections. This disproves an assumption that the earlier
baseline timeout necessarily requires changing serial scheduling or its budget.
Contract still fails: 265 platform/identity cases pass in 535.552s, then the
separate retained composition selection times out at 900.061s
(`contract-both-98e0210b812a4bceac4abb51f73cc0b8.json`).
These runs predate the current service-reuse repair and cannot qualify it.
The exact complete local gate and actual repaired-source CI remain required;
historical storage/native failures are not erased by diagnostic reruns.

Pre-publication `python3 scripts/check_repository.py`,
`python3 -m unittest discover -s scripts/tests -v` (all 159 tests) and
`git diff --check` pass. The original implementation remains running,
T097–T100 remain unchecked, and complete-gate qualification continues rather
than ending at this repair checkpoint.

### Repaired-head qualification and remaining gates, 2026-10-30

The fixture repair below has actual uninstrumented full-selection evidence,
not only its focused red/green regression. Product
[run 40985384074](https://github.com/Stonefly-Labs/Kofte/actions/runs/40985384074)
finishes with **eight passing contexts**: quality, unit, both integrations,
recovery, browser, privacy and startup. Contract and failure-propagation fail.
[Repository run 40985384065](https://github.com/Stonefly-Labs/Kofte/actions/runs/40985384065)
passes. The checkout log and fetched merge object independently verify
`5b41205a76ab85a76c01d1e35b3e3bd844212658` with main
`3e416813233c9d291eb20cf72d5882dbddf3e1b6` and repaired feature
`32d0d91e263cd206037748bf9b997959618cd950` parents.

| Actual Linux safe report / selection | Result |
| --- | --- |
| `integration-sqlite-2f81239f0b4744dfa178bb8193ce8bed.json` | **Passed**, 764.734s. Native 211/zero skipped, 13 required classes, 322.093s; storage 236/zero skipped, 11 required classes, 340.107s; composition 57/zero skipped, 102.288s. |
| `integration-postgres-8c2ce1ed48aa4658bc76611efe6808d5.json` | **Passed**, 1064.119s. Native 120/zero skipped, eight required classes, 376.464s; storage 211/zero skipped, 11 required classes, 438.786s; composition 59/zero skipped, 248.618s. Individual command bounds are unchanged. |
| `contract-both-a91d64041eb04738ac525cb38d03de18.json` | **Failed**, 1422.768s. `platform-public-contracts` passes 265/zero skipped, 18 required classes, 488.947s. The separate retained `composition-public-contracts` reaches its unchanged 900-second bound at 900.055s; no completed XML/assertion is available. |
| `failure-propagation-none-6335444ddca047dda9afe4234bdd983f.json` | **Failed**, 1032.710s. Isolated quality baseline passes in 128.896s; the subsequent whole PostgreSQL integration baseline reaches its inherited parent bound at 900.367s. No browser baseline or injected-failure proof is inferred. |

These are the workflow's exact named commands: `python3 scripts/verify.py
--suite integration --provider sqlite`; `python3 scripts/verify.py --suite
integration --provider postgres --disposable-postgres`; `python3
scripts/verify.py --suite contract --provider both --disposable-postgres`;
and `python3 scripts/verify.py --suite failure-propagation
--disposable-postgres`. The other six product contexts and repository context
also finish successfully at this assessed merge. No passing complete matrix
is claimed.

The exact local `python3 scripts/verify.py --suite all
--disposable-postgres --warm-starts 10` at the repaired head is **not a passing
complete gate**:

| Local safe report | Actual result |
| --- | --- |
| `quality-none-782871dcecec4f4da5d1a76f08c001d4.json` | Passed, 97.915s: repository validator, all 159 Python tests, whitespace, locked restores, complete .NET format/build, Prettier, ESLint, TypeScript, generated contracts, workspace builds and dependency review. |
| `unit-none-ba5482ad218d4e84acb52a43453a330d.json` | Passed, 73.099s, including retained composition, web/package, capture and Python units. |
| `integration-sqlite-ba7f56a9655e473995866c89f6d30b97.json` | Failed, 701.162s. Native passes 211/zero skipped in 279.586s; composition passes 57/zero skipped in 95.955s. Storage fails in 325.176s at `EnrollmentRestartTests.OriginalModuleDataAndConsumedDesignationSurviveRestartsPreparationAndUnavailableAdministrator` and `IdentityEventTests.ExactCapacitiesWarnAtNinetyPercentAndRefuseWithoutCommittingEarlierEffects`. Safe locations include restart-test contender assertion line 61 and event-test first post-seed mutation line 88; no exact actual status/exception is retained, so none is invented. |
| `integration-postgres-16bbfaafdf9f4dd9a24f990e7de85709.json` | Deliberately interrupted for isolated storage diagnosis after the aggregate had already failed; native records `verification_interrupted` at 406.395s, not a completed selection or timeout qualification. Later suites did not execute in this invocation. |

No product or test source changes follow these failures. An isolated two-case
diagnostic selection of the exact SQLite methods passes 2/zero skipped in
16.931s (`identity-storage-isolated-sqlite-0421d91f5e594afbaedd3e3749a51030.json`).
The unchanged full `storage-integration-sqlite` command then passes
236/zero skipped, all 11 required classes, in 311.786s
(`identity-storage-full-probe-sqlite-4bc58d0f4df34f7b959cf52318aaeb60.json`).
The probes privately inspect failing XML only for finite exception types and
numeric/status assertions, then discard it; both return no failing assertions.
These passes narrow reproducibility but neither diagnose nor replace the
failed all-suite execution.

The deferred signing-key work is demonstrably removed from budgeted requests;
the original healthy-login failure no longer prevents either full native CI
selection. That does **not** establish a universal cause for the remaining
storage, composition, service-startup or propagation failures. In particular,
the independently timed successful PostgreSQL integration takes longer than
the isolated parent's 900-second envelope, but this is not a phase-correlated
diagnosis of the timed-out baseline. No retries, deadlines, durability,
contenders, coverage or prohibited installation-session proof are changed.

**Decision:** preserve the verified repair and block the still-incomplete
implement stage on these remaining engineering failures, not on human
approval. T097-T100 stay unchecked; final traceability cannot claim passing
prerequisites. No converge, review, acceptance or merge is requested.

Final documentation/ledger checkpoint checks pass: `python3
scripts/check_repository.py`; `python3 -m unittest discover -s scripts/tests
-v` (159 tests, 10.581s); and `git diff --check`. No extension hooks are
configured. The subsequent checkpoint changes only documentation/driver state,
not the assessed fixture/product/test source. The session-only RSA probe
source is removed; no diagnostic helper remains running. Earlier preserved
Docker resources are not deleted or claimed successfully cleaned up.

### Continued diagnosis: prepare issuer keys before serving, 2026-10-30

The same branch/draft PR and passing 16/16 and 36/36 checklists were verified,
and implement was resumed through the driver. This is engineering work, not
an acceptance request.

Request-correlated temporary probes separated owner mutations, metadata/JWKS
egress and terminal exceptions. In the complete SQLite native selection,
healthy starts spent up to 527.4ms between the JWKS TCP connection and response
headers while their local transactions completed in milliseconds. The fixture
called `RSA.Create(2048)` during construction but first exported its signing
key inside `OidcIssuer.KeysAsync`. The provider defers expensive prime
generation until that export. An isolated twelve-key probe confirmed creation
at 0.001-0.152ms versus first export at 40.514-231.003ms, with subsequent exports
at 0.011-0.201ms. Under load this variable provider setup work consumes the
unchanged one-second login envelope; it is not PostgreSQL commit work.
This identifies a concrete fixture defect, not proof that it explains every
historical storage, fixture-startup or propagation failure.

The repair materializes the signing and wrong-signature keys before the issuer
starts listening and serves JWKS from the prepared public parameters. No
product code, cryptographic strength, request/cleanup deadline, rate, contender
count, retry policy or required selector is changed. The fixture accepts
test-supplied RSA instances solely so a regression can prohibit key export
after preparation. That regression runs real bounded metadata/JWKS discovery,
requires both keys prepared and exact provider observations, and demonstrably
fails without the repair. All temporary runtime/fixture probes were removed;
the uninstrumented projects were rebuilt before qualification.

| Exact check / safe report | Actual result |
| --- | --- |
| `dotnet build tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj --no-restore -c Release -warnaserror` | Passed before red and green runs; zero warnings/errors. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-build --no-restore -c Release --filter-class '*OidcBackchannelTests' --filter-method '*IssuerPreparesSigningMaterialBeforeTheFirstBudgetedDiscovery' --minimum-expected-tests 1` | **Failed before repair**, exit 2, 3.413s; `identity-key-preparation-red-sqlite-c92f7fab6367451ea7703d8674f43214.json`, at the real configuration fetch. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-build --no-restore -c Release --filter-class '*OidcBackchannelTests' --filter-class '*OidcProtocolTests' --filter-class '*OidcBudgetTests' --filter-class '*IdentityFailureTests' --filter-trait Provider=sqlite --max-threads 4 --minimum-expected-tests 1` | **Passed after repair**, 106/zero skipped, 69.807s; `identity-key-preparation-green-sqlite-252e9e48f0624f43aea9e317ff293f37.json`. Includes the guarded-key regression and actual invalid signatures, deadlines, cancellation and corrected logins. |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-build --no-restore -c Release --filter-class '*PostgresIdentityFailureTests' --filter-method '*LostStoreAuthorityOrCorruptTicketCannotServeAWarmedIdentityAndNeedsExplicitCorrection' --filter-trait Provider=postgres --max-threads 4 --minimum-expected-tests 2` | **Passed after repair**, both rows/zero skipped, 32.011s; `identity-prepared-key-postgres-postgres-ff6475e54a8140a992dabe45887f02da.json`. Reaches the actual authority-loss/corruption assertions and correction, unlike the earlier failed initial-login run. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --include tests/integration/Kofte.TestSupport/Identity/OidcIssuer.cs tests/integration/Kofte.Host.IntegrationTests/Identity/OidcBackchannelTests.cs` | Passed. |
| `python3 scripts/check_repository.py`; `python3 -m unittest discover -s scripts/tests -v`; `git diff --check` | Passed; all 159 Python tests. |

These are safe runner projections with private XML discarded, using the
previously recorded pinned macOS arm64 tool profile. They remain targeted,
`completeGate=false`, not the complete local gate or fixed-head CI.

The intermediate instrumented broad selection
`identity-request-phase-broad-both-9f92745d7e024956bee545f28c641b8b.json`
exits 9 with no known failing assertion: its custom 90-case minimum does not
match that subset. It is not a new native assertion failure or qualification.
Full instrumented SQLite selections pass 210/zero skipped, including all
thirteen required classes; these selections are diagnosis only.
The instrumented PostgreSQL selection instead fails before serving, in owned
service startup/cleanup (456.062s); no healthy-request diagnosis is inferred.
The subsequent uninstrumented two-row PostgreSQL pass above uses a fresh
normally provisioned fixture, without relaxing that setup or retrying requests.

At repair publication, the exact complete local gate and actual repaired-head
CI were pending and the ledger was `running`. Their actual results are recorded
above; T097-T100 remain unchecked and no later stage has begun.

### Resumed unchanged-head investigation, 2026-10-29

The original clean branch and open draft PR were verified, both checklist gates
passed (16/16 and 36/36), and the blocked implement stage was resumed through
the driver. No hooks are configured. These new attempts assess `f68dd98` with
existing uninstrumented Release binaries, not a fresh-build or complete-gate
qualification. No product, test, dependency, approved-intent or task-marker
change is retained.

The system `dotnet` initially refused the pinned SDK selection; selecting the
existing `artifacts/toolchain/dotnet-10.0.402` installation resolved that tool
failure without installation or pin changes. Actual tools are SDK 10.0.402 /
runtime 10.0.13, Node 24.22.0, npm 11.19.1 and Python 3.15.0, on macOS arm64,
18 logical CPUs and 64 GiB RAM. Commands below set
`KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true` and use the unchanged owned verifier
runner, private XML projection/disposal and 900-second command bound.

| Exact native command, before the runner's private XML options | Actual result |
| --- | --- |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-build --no-restore -c Release --filter-class '*OidcBudgetTests' --filter-class '*IdentityFailureTests' --filter-not-trait Suite=browser --max-threads 4 --minimum-expected-tests 90` | **Failed**, exit 2, 720.188s. Safe report `identity-healthy-baseline-both-1155cc212ca34dcaa008e644599bcb0d.json`. Eight PostgreSQL method names fail across healthy login/context, recording/commit-failure setup, rate, DNS, invalid evidence and provider-failure cases. Captured codes include exhausted/before-deadline request failure, `postgres_unconfirmed_creation_preserved` and `postgres_container_cleanup_incomplete`. No passing count is inferred from this failed selection. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-build --no-restore -c Release --filter-class '*PostgresIdentityFailureTests' --filter-method '*LostStoreAuthorityOrCorruptTicketCannotServeAWarmedIdentityAndNeedsExplicitCorrection' --filter-trait Provider=postgres --max-threads 4 --minimum-expected-tests 2` | **Failed**, exit 2, 90.400s. Safe report `identity-commit-wait-probe-postgres-a8b6405d0aa847c9b4be283e252aa82e.json`. The failure is expected 303 / actual 503 in the **initial healthy login**, `IdentityFailureTests.cs:237`, before either intended lost-authority/corrupt-ticket mutation. This is not evidence that the outage assertions fail. |

Independent read-only PostgreSQL sampling was restricted to services created
by these attempts. It returned only closed state/wait categories and numeric
durations, never SQL text, identities, credentials or application payloads.
During the broad selection, active `IO/WalSync` samples reached 1,776ms of
transaction age, and `LWLock/WALWrite` reached 508ms. Transaction age is **not**
time spent in that wait. A separate exact-`COMMIT` selection observed only up
to 205ms of query age in `WalSync`; the narrowed attempt observed one 35ms
commit sample and 21 unavailable samples during service startup/shutdown.
These observations do **not** correlate a slow WAL flush to the failed
request, establish a root cause or rule out scheduling/other phases.
The narrowed run is instrumented diagnosis, not qualification.
An independent loopback TLS socket comparison was also inconclusive; no
speculative socket/runtime change is retained.

The broad fixture's explicit cleanup refusal preserved its owned service.
After verifying its exact ID and both run/project ownership labels, the
investigation stopped that container and verified `Running=false`, `exited`.
It did **not** delete the container, volume or uncertain-creation evidence,
and does not claim successful fixture cleanup. No sampler/helper remains
running. Temporary source was removed and the product/test diff is empty.

The previously pending
[product run 40958406127](https://github.com/Stonefly-Labs/Kofte/actions/runs/40958406127)
is now complete: **seven product contexts pass; both integrations and
failure-propagation fail**. Assessed merge
`0cd6f16dc8bb60783c2e00e94e5e300ee15fe44e` has directly verified main
`3e416813233c9d291eb20cf72d5882dbddf3e1b6` and feature
`f68dd98a049ca205e1849074170c6b3636e41023` parents.
[Repository run 40958406343](https://github.com/Stonefly-Labs/Kofte/actions/runs/40958406343)
passes independently.

| Latest Linux safe report | Actual result |
| --- | --- |
| `integration-sqlite-9c728cc1e4b548ecbc5a88d55443fcf3.json` | Failed, 729.921s. Native fails in 301.225s at ordinary-production enrollment login and corrected invalid-evidence login, with `identity_fixture_request_deadline_exhausted`. Retained storage passes 236/zero skipped; composition passes 57/zero skipped. |
| `integration-postgres-bbddd21d272a4f04952eb75f9fab393e.json` | Failed, 1100.359s. Native fails in 390.825s at corrected invalid-evidence login, with `identity_fixture_request_deadline_exhausted`. Retained storage passes 211/zero skipped; composition passes 59/zero skipped. |
| `failure-propagation-none-a2ded7c18559421fa73bb67022f3d52e.json` | Failed, 1035.269s. Isolated quality baseline passes; the subsequent **whole PostgreSQL integration baseline** reaches its inherited 900.365s parent timeout. No successful propagation baseline/injection is inferred. |

The Linux SQLite failures and the pre-session protocol failures in earlier
runs cannot be attributed to PostgreSQL WAL from these local observations.
The newer retained-storage passes also do not diagnose the earlier
acquisition/registration failures.

Evidence-only checkpoint checks: `python3 scripts/check_repository.py`
**passed**; `python3 -m unittest discover -s scripts/tests -v` **passed all
159 tests**; `git diff --check` **passed**. No new product build or complete
all-suite gate was run for this documentation/ledger checkpoint, and neither
failed native selection is substituted for one.

**Decision:** block implement again, not complete it. T097-T100 remain
unchecked. Next engineering work must isolate the healthy request's exhausted
phase and the fixture/acquisition failures, establish a repair without
relaxing durability, deadlines, contenders or required coverage, then rerun the
exact complete local gate and actual fixed-head CI. No approval is requested,
installation-session boundary proof executed, or later stage started.

### Prior complete-gate attempt at 30178eb

The exact command
`python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10`
at published `30178ebf77916be706806d9a02bbede479596108` is **not a passing
complete gate**. Its completed macOS arm64 results are:

| Suite / safe report | Actual result |
| --- | --- |
| Quality `quality-none-3096d7b2a32647babd63529a77faf500.json` | Passed, 37.866s. |
| Unit `unit-none-9dd8055b68aa495f9862b4762e3f8d24.json` | Passed, 50.758s. |
| SQLite integration `integration-sqlite-c589d9de46274cd59a6d32643eb24c78.json` | All three native/storage/composition commands passed, 634.786s. |
| PostgreSQL integration `integration-postgres-d20ce7052ba043419be5462c7495c28f.json` | Native and storage failed; composition passed, 998.595s overall. |
| Recovery `recovery-both-76aa53d01de34644ae7b8b0a27689b92.json` | Both providers, strict browser evidence, capture and cleanup passed, 1185.472s. |
| Contract `contract-both-5bfede2100b14cecb4835448e5973991.json` | Deliberately interrupted for native diagnosis after the aggregate had already failed; `platform-public-contracts` records `verification_interrupted` at 144.842s, not timeout or a completed native assertion. |

No native/Next rebuild occurred during this aggregate. After its PostgreSQL
failures and completed recovery, the verifier received a graceful termination
to permit a controlled diagnostic build. The remaining contract steps and later
browser/privacy/startup/propagation suites were not reached in this attempt.
`completeGate` remains false. The interruption is not evidence that contract
hung, nor a substitute for rerunning the exact complete gate after repair.

The native PostgreSQL command fails in 498.921s at these methods:
`PostgresEnrollmentTests.ARealStoredDevelopmentSessionHasAnActorButCannotEnroll`,
`PostgresIdentityFailureTests.LostStoreAuthorityOrCorruptTicketCannotServeAWarmedIdentityAndNeedsExplicitCorrection`,
`PostgresIdentityKeyTests.ActualDatabaseRecoverySetDoesNotContainSeparatelyRetainedKeyCustody`,
`PostgresIdentityPreparationTests.ActualRestoreCommandResumesARealPartiallyFlushedJournal`
and
`PostgresOidcBudgetTests.DeliveredProviderFailuresAreBoundedWithoutRetryRedirectOrPersistentIdentity`.
Safe evidence includes expected 200/actual 503,
`identity_fixture_browser_context_failed` and
`identity_fixture_request_failure_unobserved`; the provider-failure rows also
require an actual selected issuer observation, not just refusal.
The separate retained storage command fails in 399.786s at
`PostgresRegistrationConcurrencyTests.TwentyRoundsOfTenSeparateProcessesKeepActualOwnershipUnique`
with `storage_deadline_exceeded`, operation `register`. Its twenty rounds,
ten processes, assertions and deadlines remain unchanged. The two failures
have no established common cause.

Actual Ubuntu 24.04 x64/four-logical-CPU
[run 40941594959](https://github.com/Stonefly-Labs/Kofte/actions/runs/40941594959)
is complete: **eight contexts pass; both integration contexts fail**.
The assessed merge `9615d70c98af9d8c354721cc811da2588c2bd806` has directly
verified main `3e416813233c9d291eb20cf72d5882dbddf3e1b6` and feature
`30178ebf77916be706806d9a02bbede479596108` parents. Repository checks in
run 40941595105 pass independently.

| Linux context / safe report | Actual result |
| --- | --- |
| Quality `quality-none-784ff5c9771d4ad8a32b6004ab84640b.json` | Passed, 128.712s. |
| Unit `unit-none-fa617e990e4c427caf8d205399a9f1d3.json` | Passed, 64.302s. |
| SQLite integration `integration-sqlite-e50a46138d9b428e9e8389369fdf72ab.json` | Failed, 719.748s; native failed 303.256s, retained storage 236/zero skipped and composition 57/zero skipped passed. |
| PostgreSQL integration `integration-postgres-91558daf671b494bbeddcccbaf9b3479.json` | Failed, 1049.061s; native failed 374.245s, retained storage 211/zero skipped and composition 59/zero skipped passed. |
| Contract `contract-both-f0fe7b32f327467cacf9bbab84cca8b8.json` | Passed, 2613.983s; platform 265/zero skipped, retained composition 84/zero skipped in 788.633s under the unchanged 900s bound, both ten-row identity catalogs and remaining public HTTP/scanner/cleanup steps passed. |
| Browser `browser-none-288b5110ec43403bbf90f9d129ebc16e.json` | Passed, 145.382s; strict browser 12/zero skipped/four required files and both native providers two/zero skipped each. |
| Recovery `recovery-both-84c3ef9cf24d400396dc92c17baea95a.json` | Passed, 1634.096s; both providers, strict browser inventory, scanners and owned cleanup. |
| Privacy `privacy-both-d788e045d5ed4e76a83b72a12ebad071.json` | Passed, 725.410s; both providers and strict private capture. |
| Startup `startup-none-1b2ce9561d6c45f49cb489a23794441a.json` | Passed, 72.695s; actual empty and composition restart evidence with strict browser completeness. |
| Failure propagation `failure-propagation-none-e9bc8071b6984c4c99f9ce6bf50ffe91.json` | Passed, 1270.045s; actual complete isolated exercise, not the earlier timed-out PostgreSQL baseline. |

Linux SQLite fails
`OidcProtocolTests.IndependentlyInvalidEvidenceNeverReachesTicketSeamAndFreshCorrectedLoginWorks`,
`SqliteEnrollmentTests.ActualStoppedReplacementRefusesTheOriginalSecretAndConsumesOnlyTheNewGeneration`,
`SqliteOidcBudgetTests.DeliveredDiscoveryDeadlineIsIdentifiableWithoutRelaxingLoginOrPublishingIdentity`
and
`SqliteOidcBudgetTests.DeliveredProviderFailuresAreBoundedWithoutRetryRedirectOrPersistentIdentity`.
Safe observations include expected 302/actual 500 and exhausted/before-deadline/
unobserved request reasons. The independent pre-session protocol fixture does
not use the product identity store: these results cannot all be assigned to
database commits. Linux PostgreSQL fails
`PostgresIdentityFailureTests.ActualLockedOrPausedStoreRefusesEveryIdentityOperationAndCorrectionPreservesAuthority`
at its initial healthy expected-303/actual-503 login, not proof that the intended
outage assertion failed.

The older `012ab8a` run 40938967726 remains failed: platform 265/zero skipped
passes in 511.076s, but retained composition reaches 900.054s
(`contract-both-bc95ddf058824d98b2e8de46b5715994.json`, total 1447.479s).
Its isolated PostgreSQL propagation baseline also times out at 900.365s
(`failure-propagation-none-968f20d4e71041a1afe7c92aa2618527.json`,
total 1031.264s); no browser baseline/injection is reached there. The newer
complete contract/propagation passes preserve, rather than diagnose or waive,
those earlier failures.

T097-T100 stay unchecked. Temporary native diagnostics are private experiments,
not deployed behavior or acceptance evidence, and have now been removed.
No positive retry, timeout increase, reduced contender count, arbitrary
serialization or installation-wide session-capacity test is introduced.

### Native deadline investigation and finite fixture diagnostics

Five temporary full PostgreSQL native selections preserved the original
provider/browser filters, four test threads and 900-second command bound.
They used `--no-build` and `DOTNET_PROCESSOR_COUNT=4`; the latter changes the
runtime's reported processor count, not physical CPU affinity. All failed and
all are targeted experiments with `completeGate=false`, not qualification:

| Safe report | Actual result |
| --- | --- |
| `identity-native-phase-probe-postgres-4991564beebb4095a96998bfd50927de.json` | Failed, 376.701s; console projection captured no samples. |
| `identity-native-private-phase-probe-postgres-71f750ef29a44391893f50e2c72bc45f.json` | Failed, 313.248s; the global exception limit omitted the unexpected healthy-context cancellation. |
| `identity-native-context-timing-postgres-ac260444350245938071fc4962d8892a.json` | Failed, 387.596s; context-only capture did not cover the failed operations. |
| `identity-native-terminal-timing-postgres-ac6f6d77bd964d83b9e340fe4518ff2b.json` | Failed, 273.003s; per-request terminal capture located several cancellations at the awaited PostgreSQL commit. |
| `identity-native-store-wait-probe-postgres-809cfa300f2f4eef956c259e0cccf0a0.json` | Failed, 272.313s; only the paused-store method failed, at lines 49/50/63. No unexpected healthy commit stall was captured by the wait observer. |

One healthy browser-context refusal has completed observed database activities
by 21ms, then cancellation at 955ms with 46ms left on the outer request budget,
caller not cancelled and source `DeadlineStore.CommitAsync:115`.
That line awaits `transaction.CommitAsync(work.Token)`; it is not the preceding
external `Check()`. This does **not** establish WAL/fsync delay, a network await
versus an internal driver check, thread scheduling, or a common cause with
registration. Observed database activities had only the closed `other` operation
category; they do not prove which SQL statements completed. The later
read-only wait observer did not resolve this uncertainty.

The temporary test-support file and middleware hook are removed. Actual
uninstrumented Release rebuilds of the host and storage integration projects
pass with zero warnings/errors in 16.140s and 11.840s respectively. Subsequent
commands have no processor-count override and still reproduce failures:

| Uninstrumented command selection / safe report | Actual result |
| --- | --- |
| Host `--filter-class '*PostgresIdentityFailureTests' --filter-class '*OidcProtocolTests' --filter-class '*SqliteOidcBudgetTests' --minimum-expected-tests 35` | Failed, 462.854s; five PostgreSQL failure-test methods fail, including fixture creation at `LoginCommitFixture.cs:117` before the intended product assertions. |
| Storage `--filter-trait Provider=postgres --filter-method '*TwentyRoundsOfTenSeparateProcessesKeepActualOwnershipUnique' --minimum-expected-tests 2` | Failed, 106.986s; both original twenty-by-ten rows report `storage_deadline_exceeded`, operation `acquire`, at the startup barrier. This is distinct from the original full gate's `register` failure. |
| Host `--filter-class '*PostgresIdentityFailureTests' --filter-trait Provider=postgres --filter-method '*LostStoreAuthorityOrCorruptTicketCannotServeAWarmedIdentityAndNeedsExplicitCorrection' --minimum-expected-tests 2` | Failed, 73.707s; both rows fail healthy expected-200/actual-503 assertions at `IdentityFailureTests.cs:115/141`, not only fixture creation. |

These commands use `dotnet test --project` with the respective existing
`tests/integration/Kofte.Host.IntegrationTests` or
`tests/integration/Kofte.Storage.IntegrationTests` project, common
`--no-restore --no-build -c Release --max-threads 4` and the unchanged
900-second runner bound. The first two selections share
`native-uninstrumented-failure-projection-both-c1eb8e7e9b93452c96e141406157817e.json`;
the third is
`native-fixture-start-projection-postgres-73605997099e438ba70bfef23ec00e52.json`.
Private XML is projected then discarded. No passing count is inferred from a
failed selection.

One concrete verifier gap is repaired: the source whitelist previously omitted
PostgreSQL infrastructure codes and source locations outside the identity
subdirectory. `command_diagnostics` now discovers fixed `postgres_*` literals
from dedicated test support, and the private XML projector admits them only
when source-whitelisted. The existing native-report regression now requires a
known assembly-start code/location while rejecting an invented PostgreSQL
canary, private paths and raw framework output. Limits, failure status and
private report cleanup are unchanged. This improves the next failure report;
it does **not** repair the identity or registration failures.

`python3 -m unittest scripts.tests.test_verify.VerifyTests.test_failed_host_command_retains_known_case_without_private_framework_output -v`
passes its focused regression.
`python3 scripts/check_repository.py`,
`python3 -m unittest discover -s scripts/tests -v` (159 tests, 20.605s)
and `git diff --check` pass after the diagnostic change. The full local gate
has not been repeated without a demonstrated repair. The active implement
stage is explicitly blocked on engineering evidence, not waiting for approval.

## T096: final dependency, contract and advisory reconciliation

Actual final-source `npm run dependencies:check` and `npm run contract:check`
pass. The complete reviewed 403 npm / 87 NuGet graph, pins/locks/project edges,
source/archive integrity, license and retained-notice hashes have no drift.
No new product/test dependency, copied source or notice change is introduced;
the existing exact test-only Playwright prerelease exception remains explicit.
No inventory regeneration or package install is necessary.

Fresh `npm audit --json` passes with zero vulnerabilities.
`dotnet list Kofte.slnx package --vulnerable --include-transitive --format json --no-restore`
passes with zero vulnerabilities and all fourteen distinct current solution
projects present. The private parser requires actual successful CLI exit,
no stderr/warnings, exact report shape and no reported packages/findings;
it does not treat an omitted project as a clean audit.
`identity-final-dependencies-none-482394f9a62d4a5ca04c23464a3e753c.json`
passes in 10.193s with .NET 10.0.402, Node 24.22.0 and npm 11.19.1 verified.
This is not a renewed archive-signature audit or passing complete gate.

T094-T095's independent `python3 scripts/check_repository.py`,
`python3 -m unittest discover -s scripts/tests -v` (159 tests, 6.807s) and
`git diff --check` pass. T097-T100 remain open.

The older diagnostic head `5647893` now has an actual failed Linux isolated
propagation baseline: `failure-propagation-none-9a3c093ceb3541d7a508512d44200c6d.json`
at run 40936387880. Quality passes 130.190s; the PostgreSQL aggregate reaches its
unchanged 900-second bound (900.367s) and fails `failure_exercise_timeout`.
No browser baseline or injection is reached. Assessed merge
`0423cab738905c17c6ab4205b1efe9b1f96a9aad` has verified main
`3e416813233c9d291eb20cf72d5882dbddf3e1b6` and feature `5647893` parents.
This does not diagnose the timeout or replace required final-source evidence.

## T094-T095: complete public catalog and delivered documentation

Actual final C# source at `012ab8a` passes the complete strict public catalog:
`contract-both-bfd6ca1c503d4a32a5150642c3d2d64e.json`, macOS arm64/18 logical
CPUs/64 GiB, .NET 10.0.402 with disposable PostgreSQL enabled. The report's
recorded Git revision is `5647893`: it began before the T093 commit, with those
verifier changes present. C# source/binaries were unchanged by T093 or during
execution; no rebuilding interfered with active consumers.

Each command uses `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release`,
followed by the exact selectors below and `--show-stdout None --show-stderr None`.
The verifier adds its private xUnit XML options, validates native counts and
required classes and discards private XML. Original 900-second command bounds,
120-second single recipes and 600-second `all` remain unchanged.

| Exact selector / minimum | Actual result |
| --- | --- |
| `--filter-class '*IdentityConsumerHarnessTests' --minimum-expected-tests 77` | Passed 77, zero skipped; 4.541s. |
| `--filter-class '*SqliteIdentityGuideTests' --minimum-expected-tests 10` | Passed 10, zero skipped, one required class; 492.959s. |
| `--filter-class '*PostgresIdentityGuideTests' --minimum-expected-tests 10` | Passed 10, zero skipped, one required class; 654.153s. |

`IdentityGuideTests` runs the nine quickstart scenarios individually and `all`
per provider. `IdentityConsumerHarness.RequireSuccess` independently compares
every exact ordered observation and total against `IdentityScenarioCatalog`;
zero exit alone is insufficient. Real negative children prove timeout termination,
source/cleanup/capture refusal; malformed/absent/truncated/duplicate/reordered
observations and nonzero/stderr envelopes cannot pass. Browser/a11y evidence
remains independent. The focused run passes in 1151.664s, not a complete gate.

T095 reconciles the quickstart's actually exercised DLL grammar, runtime/HTTP/
actor and stopped operator contract status, README/contributor/docs index,
architecture, local guide and workflow. Strict private config and secret files,
exact HTTPS/ingress/endpoint/network trust, pending-only nomination replacement,
session/revocation/key overlap, explicit format upgrade, complete-set quarantine/
decision reconciliation, retention/capacity and IdP-loss/OS-copy/old-instance/
rollback limits remain explicit. Planning and earlier evidence retain their
historical claims under clear historical labels; approved behavior and
reviewer-owned checklist markers do not change. Permissions/teams/MCP/full UI/
audit/deployment/release remain future 006-009/022-024. No acceptance or later
Spec Kit stage is executed.

## T093: require actual complete browser evidence, not exit status

The existing required source/project/scanner/observation inventories and strict
native XML/class gates already include identity in every applicable named suite.
The remaining root Playwright gap is reproduced: a real Python child exits zero
without running any browser and was incorrectly accepted. Its new regression
fails before the repair.

Every Playwright command now first discovers its actual cases through the pinned
runner's private JSON reporter. Required file/case minima reject omitted source
and empty/partial inventories. Execution must match every discovered case ID,
source location, title and project exactly, with one passing first attempt,
no declared expected failure, skip, retry, flaky result or global error.
Only positive counts enter `browserEvidence`. Missing/malformed/duplicate,
publicly readable, linked, nonregular and oversized reports are fatal. Both
private reports are discarded after verified child cleanup.

Discovery and execution share the **original overall command deadline**;
selection, fixture concurrency, product budgets and all ten CI contexts remain
unchanged. The existing propagation checker recognizes only the exact reporter-
augmented command and independently validates safe browser counts. Its special
missing-Chromium pair still requires the actual nonzero browser command and
the exact subsequent missing-canary refusal.

Actual final-source `python3 scripts/verify.py --suite browser --disposable-postgres`
passes in 100.758s (`browser-none-08c39ad844b94f00a9ee63d1a1da5733.json`):
12 passed/zero skipped/four required files, followed by two native cases and
one required identity class **per real provider**, zero skipped. Strict canary
and transport scanning/owned cleanup also pass. This is macOS arm64, not a
new Linux qualification claim.

`python3 -m unittest scripts.tests.test_verify scripts.tests.test_failure_propagation -v`
passes 82 cases after fixing the success-shaped negative's missing new evidence.
Final `python3 scripts/verify.py --suite quality` passes all 21 steps in
33.560s (`quality-none-fd28c7f5a6714c6b89ced5c793551247.json`), including all
159 Python cases, formats/build/types/dependency/contract checks. New regressions
exercise real zero-exit and report-producing children, inventory substitution,
all non-passing outcomes, privacy/discard and unsafe physical reports.
T093 closes; T094's complete public catalog is now executing. T097-T098 still
require actual full local and assessed-head CI results, including the unresolved
native/composition failures.
Independent final `python3 scripts/check_repository.py`,
`python3 -m unittest discover -s scripts/tests -v` (159 cases, 7.128s) and
`git diff --check` also pass.

The completed earlier `dee39c5` Linux contract now **passes** in 2983.009s:
`contract-both-73a6d3350f8d47719b9eb90f2998ba01.json`, actual run 40930623203.
Platform 265/zero skipped/18 required classes passes 450.281s; retained
composition **84/zero skipped passes 889.447s under the unchanged 900s bound**;
both ten-row identity guide catalogs pass 235.848s/433.563s, each with its
required class. Every standalone retained composition guide and `all`, native
OpenAPI and both-provider HTTP/scanner/cleanup also pass. This is actual Linux
qualification, not a diagnosis of the earlier timeout or the separate failed
local four-processor `failure-correction` comparison. That run remains red
overall because of the recorded SQLite native DNS positive.

## T092: qualified focused operations checkpoint, not a complete-gate pass

The actual passing focused Linux integrations at `d1c2f50` below close T092's
provider join. The newer `dee39c5` operational jobs independently pass:
recovery 1720.656s (`recovery-both-ee9f03d7764e4a2c97458b1d90f41a62.json`),
privacy 732.400s (`privacy-both-6fd9954e939448db96147da751229719.json`),
browser 143.572s (`browser-none-d49af7702b074748a38923ae365700fc.json`), and
complete failure propagation 1203.524s
(`failure-propagation-none-bf9db734bc8343f18e3afa0db1378d11.json`).
These execute the exact named suite commands below with unchanged bounds.
Assessed merge `11026caf9b827909992f5821a76675710b672683` has independently
verified main `3e416813233c9d291eb20cf72d5882dbddf3e1b6` and feature
`dee39c5b47d0e762a25c5ef5e7253a733a6c4d35` parents.

| US5 acceptance | Delivered assertions and actual focused evidence |
| --- | --- |
| US5.1 | Both-provider format/preparation/startup and upgrade-preservation cases preserve original module assignments, history and data; serving remains read-only. The T079 and T080-T085 evidence below records physical checks and real complete-set recovery. |
| US5.2 | `IdentityFailureTests`, key/preparation crash facts and actual interrupted-recovery recipes require bounded truthful refusal, explicit correction, retained mappings and consumed designation. Newer recovery passes native 50/42, storage 73/57 and all retained public/composition selections with zero skips. |
| US5.3 | Real `IdentityPrivacyTests`, public privacy recipes, native browser journeys and runtime channel scanners pass on both providers; required classes and zero native skips are verified. No missing-capture exception or private raw report is published. |
| US5.4 | Actual browser/accessibility and production retry journeys pass, including the corrected keyboard-control readiness race, narrow width, enlarged text, explicit retry/sign-out and safe destinations. Complete failure propagation proves all three baselines and all five injections. |

T083-T088 retain actual key custody/overlap, smaller admission/capacity,
retention, checked-overflow, restore-decision and rollback proofs; the rejected
installation-session boundary remains removed and unverified. Complete-set
backups do not provide OS-copy fencing, invalidate an old independently running
instance, establish missing historical administrator state or make external
systems atomic. Restored unknown history stays locked; operators must fence
old instances and preserve independent protected key custody.

This closes the **specific focused operations checkpoint**, not the newest
SQLite DNS positive, local PostgreSQL composition failure or complete matrix.
Those failures remain failed and must be resolved at T097-T098; this is not
a rerun waiver or a claim that the latest full source passes. T093-T100 remain
open, with verifier completeness next. All earlier evidence below is historical.

## Continued implementation: qualified propagation and remaining native timing

Actual assessed `d1c2f50` Linux
[run 40928353045](https://github.com/Stonefly-Labs/Kofte/actions/runs/40928353045)
finishes with **nine passing contexts** and the one coupled cookie-contract
failure repaired in `dee39c5`. Merge `84326761d92c21085cac92ea0fd0ec3ac6391e5d`
has verified parents main `3e416813233c9d291eb20cf72d5882dbddf3e1b6` and `d1c2f50`.
This is not a completely green matrix or a completed implement stage.

| Exact verifier invocation | Actual result |
| --- | --- |
| `python3 scripts/verify.py --suite integration --provider sqlite` | Passed 740.252s; `integration-sqlite-d575707a447a46589208765b9d5a315a.json`. Native 210 cases/13 required classes, storage 236/11, retained composition 57; zero skipped. |
| `python3 scripts/verify.py --suite integration --provider postgres --disposable-postgres` | Passed 693.494s; `integration-postgres-4c92adcb25224f439cc72e39c6a724ff.json`. Native 120 cases/eight required classes, storage 211/11, composition 59; zero skipped. |
| `python3 scripts/verify.py --suite recovery --provider both --disposable-postgres` | Passed 1599.521s; `recovery-both-09b38086f9e342329e0ed36d50945e10.json`. Both providers' native, storage, standalone public and retained composition selections pass with zero skipped. |
| `python3 scripts/verify.py --suite failure-propagation --disposable-postgres` | Passed 1039.268s; `failure-propagation-none-b39a17611a734f9cbc77a75150b2d309.json`. Actual isolated quality/PostgreSQL/browser baselines **and all five real injections pass**. Missing Chromium and its subsequent missing-canary refusal remain fatal in the child evidence. |

Privacy, browser (including the separate public HTTPS recipes), quality, unit
and startup also pass in that actual run. Its retained contract is not reached
after the coupled foreign-cookie assertion failure; the earlier retained Linux
composition timeout is not thereby resolved. Diagnostic matrix `23d9373`
is explicitly cancelled as superseded, freeing capacity for the repaired head;
it supplies no passing-gate claim.

A temporary exception-only local investigation of the same 106-case/four-thread
native selection fails in 575.424s:
`integration-both-513a160f70ec4b31a512e56e8a21b7d4.json` reports seven distinct
failed methods. Five samples reach the PostgreSQL `DeadlineStore.CommitAsync`
boundary while outer request time remains, with seven to nine runtime workers
and zero or one pending item at observation. This does **not** distinguish its
preflight budget check from an awaited commit, prove database/fsync causality,
or rule out earlier contention. A narrower 26-case PostgreSQL distinction
experiment passes 149.219s/zero skipped, obtaining no failed sample. All temporary
first-chance handlers, dictionaries and fields are removed; the complete
published solution is then rebuilt with zero warnings/errors. No instrumented
pass substitutes for an uninstrumented gate.

Actual `dee39c5` Linux run
[40930623203](https://github.com/Stonefly-Labs/Kofte/actions/runs/40930623203)
still reports one SQLite initial DNS-test challenge as expected 303/actual 503
in `integration-sqlite-7423a50a40c54b5cba9ce21e3536b19c.json`; its storage
236-case and composition 57-case selections pass. That explicit positive
assertion now preserves the same finite request-budget reason as its shared
helper. `CompleteProductAsync` also preserves that reason instead of replacing
it with only a generic challenge error. A real forced-discovery regression
first fails both providers (18.636s), then the two deadline and four DNS
re-admission cases pass (six total, zero skipped, 17.410s):
`KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-method '*DeliveredDiscoveryDeadlineIsIdentifiableWithoutRelaxingLoginOrPublishingIdentity' --filter-method '*DeliveredDnsChangesAreReadmittedBeforeTheRealTokenConnection' --minimum-expected-tests 6 --max-threads 4 --show-stdout None --show-stderr None`.
Safe reports are `integration-both-51344939f971412fb6a1a04f232cde00.json` (red,
deadline-only selection) and `integration-both-a011eb0c37d14ba8bb889bd3bd8deb6e.json`
(green). Resetting the finite reason before each send prevents a previous
failure being attributed to a healthy later request. The regression also
requires a successful follow-up context to clear it. The exact final six-case
command above passes again with zero skipped, 34.264s:
`integration-both-15241e9a52994ef39131ad68ace0a918.json`. This repairs diagnostic
loss, not the intermittent timing root cause.

The complete retained local selection with `DOTNET_PROCESSOR_COUNT=4` and
the same 900-second bound finishes **failed**, not timed out, in 667.391s:
`contract-both-83bfdf0f12c045debb394756b45e4407.json`. It identifies the PostgreSQL
`failure-correction` case, with `composition_test_execution_failed` and
`composition_consumer_failed`; all source-known method symbols have completed
output. Method progress is not unique-case completeness or passing XML.
The earlier 18-processor 84-case pass and this failed comparison establish
neither a scheduler root cause nor a Linux timeout repair. No scheduling,
selection, minimum, product budget or command bound is changed.

Final `python3 scripts/verify.py --suite quality` passes all 21 steps in
37.315s (`quality-none-eca7e194c7a94ed7bf0c4c0e1a73c19d.json`).
`python3 scripts/check_repository.py`, `python3 -m unittest discover -s scripts/tests -v`
(154 cases, 6.746s) and `git diff --check` also pass. T092-T100 remain open;
neither the rejected capacity proof nor a later stage is run.

## Continued implementation: preserve the session-only foreign-cookie contract

Actual assessed `d1c2f50` Linux run
[40928353045](https://github.com/Stonefly-Labs/Kofte/actions/runs/40928353045)
finds a coupled fixture regression before the retained composition selection:
both `ActualDevelopmentCookieCannotAuthenticateProductionEvenAfterRenamingItsCookie`
provider cases fail at the old whole-header prefix assertion. The delivered
Development operation now correctly retains browser-context/CSRF cookies before
its session cookie. The test incorrectly assumed the manually seeded session
was the entire cookie header.

The contract now requires exactly one actual Development session cookie from
that header and exercises both its original and production-renamed form.
All four 401 refusals, preserved real production cookie, unchanged authority/
events and active production session assertions remain unchanged. No context
cookie is discarded from the fixture or product behavior.

The actual exact two-case selection first fails both providers in 22.073s
(`contract-both-9c0af49876b14a71ac79b51d6b604ce0.json`), then passes both with
zero skipped in 38.274s
(`contract-both-d9b666f72ad74989a09f7e5db9e76f12.json`):
`KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-method '*ActualDevelopmentCookieCannotAuthenticateProductionEvenAfterRenamingItsCookie' --minimum-expected-tests 2 --show-stdout None --show-stderr None`.
Both run through the verifier's strict private XML/cleanup handling; the initial
red command additionally uses `--no-build`. `python3 scripts/verify.py --suite quality`
passes all 21 steps after the fix, including format, build, Python, dependencies,
repository and whitespace. Fresh assessed CI is still required; no stage gate
is marked complete.

## Continued implementation: locate the retained Linux contract timeout

The complete retained composition selection passes locally without an extended
deadline: 84 passed, zero skipped, 583.290s in
`contract-both-1d5c5a418b4f44c7baeab8b0ccf15287.json`.
The session-only observer executes
`dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 84 --output Detailed --show-stdout None --show-stderr None --long-running 60 --xunit-diagnostics on`
through the verifier's unchanged 900-second command bound and strict private XML
validation. This is macOS arm64/18 logical CPUs, not Linux qualification or a
root-cause diagnosis. The report's historical head is `376c7a1`; source includes
the subsequently published `d1c2f50` increment.

The completed Linux `376c7a1` contract result is still red:
`contract-both-2ae8f33b9a45436895071f9eac5b84b0.json`, 1419.587s.
Its preceding platform selection passes 265 cases/zero skipped/18 required
classes in 485.518s. Its composition selection times out at 900.056s without
completed XML. Recovery on the same assessed run passes; SQLite integration
and the final intentional-Chromium checker remain the other failed contexts.

The composition command now requests detailed method progress while hiding
test stdout/stderr. On failure only, the existing transient parser projects a
bounded set of completed **source-known method symbols**. No theory arguments,
private output, invented elapsed measurements or success evidence are retained.
A real timed-out Python child regression proves this additional diagnostic
survives cleanup while the original timeout remains fatal and XML absence
cannot become success. Unknown names, duplicate theory rows and late capture
are independently checked. Selection, minima, command/product deadlines and
ordinary capture requirements remain unchanged.

At `d1c2f50` plus this Python-only diagnostic increment,
`python3 scripts/verify.py --suite quality` passes all 21 steps in 40.235s
(`quality-none-203b0c6ec703433a90e2d5099620c57d.json`);
`python3 scripts/check_repository.py`, `python3 -m unittest discover -s scripts/tests -v`
(154 cases, 7.246s) and `git diff --check` pass. Fresh assessed Linux evidence
is still required. T092-T100 and the active implement stage remain open.

## Continued implementation: delivered Development setup and expected Chromium refusal

The published privacy dependency repair `376c7a1` is now qualified on actual
fresh Linux [run 40923555808](https://github.com/Stonefly-Labs/Kofte/actions/runs/40923555808):
`privacy-both-a7150eab2ba64dc7b18b4c3b8ed9b007.json` **passes**, 725.679s.
Both provider public selections pass seven cases including their required
identity privacy class; both native browser selections pass two cases, zero
skipped and their required class. All later runtime/scanner steps pass.
Assessed merge `8a4f909cf51b9d1dbfe13731b42bfbdd7fe68c4f` has verified parents
main `3e416813233c9d291eb20cf72d5882dbddf3e1b6` and the published feature head.
This closes the missing-build investigation, not all implementation gates.

The same actual run's propagation report
`failure-propagation-none-821305793fc44d7a80ef0c1dd339cd18.json` reaches **all**
injections. Its quality/PostgreSQL/browser baselines and test/formatter/build/
unavailable-PostgreSQL injections pass. The final deliberately missing Chromium
fails the real `browser-tests` command, then correctly refuses missing browser
canaries during cleanup. The harness expected only the first failure and
incorrectly rejects the exact second refusal as
`failure_did_not_reach_expected_command`, 1085.219s total.

Only that intentional missing-browser exercise now requires both exact fatal
steps: the original nonzero browser command and the subsequent specific
empty-command/no-child-exit canary refusal, in order, with safe correlations.
An absent/different/extra cleanup failure, capture-only failure or successful
browser is rejected; all ordinary baseline rules remain unchanged. Runtime
capture is still mandatory and fatal. The new checker accepts the actual
retained Linux negative above; this is revalidation, **not** a newly executed
complete propagation gate.

Native positives also now retain a finite test-only observation of the actual
request budget at the existing diagnostic sink: unobserved, caller cancelled,
outer deadline exhausted, or failure while outer time remains. No arbitrary
exception/log payload enters captures. Browser-context and initial-login helper
failures report this code, as does a failed Development browser journey.
A real forced discovery deadline proves exhausted classification, bounded 503
and no published identity/session/event on both providers. A before-outer-deadline
classification does not rule out an earlier nested store deadline.

Two prior affected-class selections fail the manually constructed Development
seed's commit deadline (104/106 and 105/106 passed). That helper bypassed the
delivered mandatory browser-context/CSRF flow and constructed its own ticket
inside a transaction. It now creates the same stored identity using the actual
context and Development HTTP operation, with the original product budgets and
no retry, artificial warm-up or direct database mutation. Both real actor/
enrollment-refusal cases and both strict browser cases pass with this setup.

At `376c7a1` plus this increment:

| Exact executed command | Actual result |
| --- | --- |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-method '*ARealStoredDevelopmentSessionHasAnActorButCannotEnroll' --filter-method '*RealDevelopmentSelectionAndSignOutRequirePrivateBrowserAndTransportEvidence' --filter-method '*DeliveredDiscoveryDeadlineIsIdentifiableWithoutRelaxingLoginOrPublishingIdentity' --minimum-expected-tests 6 --max-threads 4 --show-stdout None --show-stderr None` | Final pass: six cases, zero skipped, 19.767s. An initial temporary-instrumentation removal had a brace error, corrected before this result. |
| `python3 scripts/verify.py --suite quality` | Passed 21 steps, 34.217s; `quality-none-73d0781fe3184caca5488fde18a78e1a.json`. Final C# sources, before the subsequent Python-only injection-checker correction. |
| `python3 -m unittest scripts.tests.test_failure_propagation.FailurePropagationTests.test_missing_chromium_requires_the_real_browser_failure_and_exact_capture_refusal -v` | Initial missing-argument error; final pass including seven refusal mutations/default strictness. |
| `python3 scripts/check_repository.py` | Passed after the injection-checker correction. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed 152 cases, 8.420s after the correction. |
| `git diff --check` | Passed. |

The broader uninstrumented affected-class run remains **103/106 passed**,
zero skipped, 340.914s. It proves an actual PostgreSQL context outer-deadline
exhaustion, another context failure while outer time remains, and an enrollment
409/503 failure. The original seed failure no longer occurs in that run, but
unrelated positives are not diagnosed or waived. A temporary numeric-only
Npgsql listener's subsequent 106/106 pass (428.118s) observes no failure and
establishes no database-time cause; the listener is removed before the final
six-case/quality results and is not published.

Latest published SQLite integration still fails native positives and a storage
restart login in `integration-sqlite-500696e843f54ac7ad98e0a029f57bd6.json`.
Published PostgreSQL integration and browser verification pass. Prior `8cacf8e`
contract report `contract-both-e0f9e5862142412f85c0c48565667de5.json`
fails the retained composition selection at 900.061s (1450.331s total);
its merge has verified main/`8cacf8e` parents. A bounded full-selection local
diagnostic now observes only source-known method progress to identify the slow
work. No command, product deadline or positive assertion is weakened.
T092-T100 remain open, implement remains running, and no rejected capacity
proof is run.

## Continued implementation: build the privacy browser dependency first

The actual `8cacf8e` hosted privacy report
`privacy-both-5bc0a5c5be18470896e7cfdf5f7cd484.json` now identifies four
failed browser-backed scenarios: `oidc-login`, `nominated-enrollment`,
`session-boundary` and `development-isolation`; `private-evidence` does not
fail. Each reports transport capture missing. On a fresh privacy runner,
`execute_suite` ran these public consumers before its first production Next
build. `StartBrowserAsync` requires `.next/BUILD_ID`; its failed setup then
enabled disposal's transport check, masking the missing-build exception.
Browser jobs build Next first, and existing local builds explain the passing
isolated reruns. This is a demonstrated missing producer dependency, not an
assumed Chromium or product-deadline failure.

The privacy suite now builds the actual production web workspace once, after
the PostgreSQL opt-in guard and before either provider's browser-backed public
or native selection. Existing cases, deadlines and strict captures are unchanged.
The existing command-selection regression first fails for zero required builds,
then passes while proving the build precedes both providers' consumers and
native browser selections.
Fixture setup also checks the build prerequisite before enabling browser
transport capture. A missing build must fail with its specific setup error,
not disposal's missing-capture error; successfully started browser fixtures
retain the same strict capture requirements.

At `8cacf8e` plus this verifier dependency correction:

| Exact executed command | Actual result |
| --- | --- |
| `python3 -m unittest scripts.tests.test_verify.VerifyTests.test_contract_and_privacy_select_only_their_real_serial_test_files -v` | Expected initial missing-build failure; final pass. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed 151 cases, 5.787s. |
| `git diff --check` | Passed. |
| `python3 scripts/verify.py --suite quality` | Passed all 21 steps, 49.521s; `quality-none-8f0652e20fec43fba5e38996cbce0ec7.json`. Includes build, formatting, lint, types, generated contracts and dependency review. |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-build --no-restore -c Release --filter-query '/*/*/*/RealDevelopmentSelectionAndSignOutRequirePrivateBrowserAndTransportEvidence' --minimum-expected-tests 2 --max-threads 4` | Controlled missing-build negative: temporarily withheld only `.next/BUILD_ID`; both provider cases fail with exactly `identity_fixture_web_build_required`, neither with transport-capture missing. Transient harness requires those two failures and restores the original build ID in `finally`; probe passes. |

Fresh hosted privacy qualification is still required. The local both-provider
privacy diagnostic `privacy-both-152e6315ce2f48108ea0421ec3b4e0ef.json`
fails after 526.007s: both public privacy selections pass, as does the complete
SQLite profile, but PostgreSQL's native Development journey fails initial
signed-out visibility with real client fetch aborts. The safe profile shows
context headers at 1171ms and client aborts at 1172/1173ms; this does not prove a
specific server failure or revive the repaired upstream EOF bug. Test-support
sources were compiled during that diagnostic, so it is not clean assessed-head
closeout evidence.

Separate pending native login-budget diagnostics are not part of this
checkpoint. Their broader three-class selection executes 106 cases:
104 pass, zero skip, two fail (a PostgreSQL browser-context refusal and a
PostgreSQL Development seed commit deadline), 242.445s. Those failures remain
unresolved; no production budget or positive assertion is weakened.

## Continued implementation: identify the failing public scenario safely

The published keyboard-audit repair is `4691c16`. Repeated hosted privacy
reports identify only the five-row theory method, not which public scenario
failed. The verifier now projects a scenario only when the failed test's exact
source-known method/display name matches a closed catalog entry. An actual
ten-case native report confirmed xUnit's escaped argument framing. Passed
cases, unknown tests/scenarios, extra arguments and trailing private text are
excluded. Existing isolated-child report validation independently checks the
same catalog; no raw display name or new free-text field is retained.

At `4691c16` plus this diagnostic correction, pinned SDK selected:

| Exact executed command | Actual result |
| --- | --- |
| `python3 -m unittest scripts.tests.test_verify.VerifyTests.test_failed_identity_scenario_retains_only_exact_known_theory_arguments scripts.tests.test_failure_propagation.FailurePropagationTests.test_native_failure_evidence_is_allowlisted_again_inside_isolated_baselines -v` | Initial projection regression fails for the missing scenario; final two pass, including isolated-report rejection of unknown/duplicate values. |
| `python3 scripts/verify.py --suite quality` | Passed 21 steps, 32.909s; `quality-none-dd23036dbcd245bd86408f81e2ba694c.json`. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed 151 cases, 5.625s. |
| `git diff --check` | Passed. |

The completed `ce2af5f` isolated propagation attempt
`failure-propagation-none-cc7a3a2014914fb29a8421ffeee950a4.json`
fails the PostgreSQL baseline after 666.449s, not an aggregate timeout:
`PostgresIdentityKeyTests.KeyFailureCannotRepairItselfOrReplaceConsumedAuthority`
expects an initial 303 but receives 503. Storage (211 cases) and composition
(59 cases) pass in that same baseline; fault injections are not admitted.
The separate complete PostgreSQL integration passes in 684.278s,
`integration-postgres-cebf2a566ce240d9b2985c005c9e663c.json`.
No budget cause or whole-stage success is inferred from that independent pass.

## Continued implementation: wait for the keyboard audit's actual control

The upstream repair is published as `ce2af5f`; its exact Linux
[run 40917432573](https://github.com/Stonefly-Labs/Kofte/actions/runs/40917432573)
**passes** named browser verification and all twelve public HTTPS recipes.
`browser-none-90ce79fc652c453589a5ceed7459fc32.json` records both native
providers, each two passed / zero skipped / one required class. Safe consumer
envelopes independently confirm all six browser recipes per provider.
The automatic matrix also passes both complete provider integrations
(SQLite report `integration-sqlite-9f6c4fd6dfab4b46a82813616240cf1b.json`,
607.988s). These results do not retroactively diagnose the older native 503/count
failures.

The parallel automatic browser job passes named verification but fails the
PostgreSQL session recipe at `identityAccessibility.ts:31`, after all twenty
controls and complete context bodies. Its uncertain-sign-out alert is visible
before the refreshed sign-in control exists. The audit focused the heading
and pressed Tab too early, leaving focus on Retry. A new isolated browser
regression reproduces that exact inactive-control assertion. The helper now
waits for control visibility before the unchanged heading/Tab/focus/outline/axe
checks. It does not require semantic enablement: loading-state Retry is
deliberately keyboard-focusable with `aria-disabled="true"`.

At `4949167` plus this readiness correction, pinned SDK selected:

| Exact executed command | Actual result |
| --- | --- |
| `npm exec --workspace tests/e2e -- playwright test accessibility.spec.ts --grep 'identity keyboard audit waits'` | Initially fails the expected inactive-control assertion; passes after visibility synchronization, including an ARIA-disabled focusable control. |
| `python3 scripts/verify.py --suite browser --disposable-postgres` | First increment incorrectly also required enablement and failed the intentional held loading state, 73.953s, `browser-none-44f020f4589442e0963cc7381e6212dc.json`. Removing that overconstraint preserves loading semantics; final gate passes root and both native providers, 84.645s, `browser-none-3a646577de914c98a2d8da4c5a9ec9db.json`. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-query '/*/*/SqliteIdentityPrivacyGuideTests/*' --minimum-expected-tests 1 --max-threads 4` | Passed all five cases, zero skipped, 79.131s. This unchanged isolated run does not diagnose hosted privacy failure. |
| `npm run lint --workspace tests/e2e` | Passed. |
| `npm run typecheck --workspace tests/e2e` | Passed. |
| `npm exec -- prettier --check tests/e2e/accessibility.spec.ts tests/e2e/identityAccessibility.ts` | Passed. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed 150 cases, 5.622s. |
| `git diff --check` | Passed. |

The subsequent `4949167` hosted unit and complete browser jobs pass, including
their public recipes. Its hosted SQLite public privacy selection still fails
`identity_browser_transport_capture_missing` in
`privacy-both-0be97e3f918c4123b6ed1472bca54a1f.json`.
No public privacy/full-gate pass or stage completion is claimed.

## Continued implementation: complete aborted-request test doubles

The published `ce2af5f` automatic unit job exposed seven failures in
`identityActionCapture.unit.ts`: its intentionally aborted request doubles
did not implement the real `Request.failure()` API used by the earlier finite
network diagnostic. The exact four-file capture selection reproduced 31 passed /
seven failed locally. Both shared action-request builders now supply
`net::ERR_ABORTED`; receipt matching, real upstream scanning and fatal failure
semantics are unchanged. The earlier focused sixteen-case check missed these
dependent call sites.

At `ce2af5f` plus this test-double correction, pinned SDK selected:

| Exact executed command | Actual result |
| --- | --- |
| `npm exec -- prettier --check tests/e2e/identityActionCapture.unit.ts` | Passed. |
| `python3 scripts/verify.py --suite unit` | Passed all 16 steps, including the complete 38-case browser capture selection; 54.617s, `unit-none-e1c2b37e467e4415a0e07eed425a38c1.json`. |
| `npm run lint --workspace tests/e2e` | Passed. |
| `npm run typecheck --workspace tests/e2e` | Passed. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed 150 cases, 5.969s. |
| `git diff --check` | Passed. |

The original repaired-head Linux
[run 40917432573](https://github.com/Stonefly-Labs/Kofte/actions/runs/40917432573)
has passed named browser verification and reached its separate public HTTPS
recipes. This is not yet a complete public-recipe pass. The parallel automatic
privacy job still fails the SQLite public consumer with
`identity_browser_transport_capture_missing`; its safe report is
`privacy-both-b17441ee103d46f09e7dc8a6423b53a1.json`.
T092-T100 remain open, and implementation continues.

## Continued implementation: use the upstream Chromium stream repair

The network diagnostic checkpoint is published as
`0cd054a5f27c7d260fd93096f3fd2a0ae1fb31ae`. Actual Linux
[run 40915008183](https://github.com/Stonefly-Labs/Kofte/actions/runs/40915008183)
fails only native SQLite Development capture in
`browser-none-ae1a3032eb494bbe80d80face11c3b63.json`; its production
retry fact passes. After sign-out, context headers arrive at 1047ms,
`net::ERR_ABORTED` at 1049ms and the missing body resource at 1050ms,
without a client AbortSignal event. The UI reaches signed-out. Earlier
context/current-user bodies complete, and root browser checks pass.
PostgreSQL/public recipes do not execute in this failed run.

[Upstream issue 42742](https://github.com/microsoft/playwright/issues/42742)
and the merged
[Chromium change 8508456](https://chromium-review.googlesource.com/c/chromium/src/+/8508456)
identify the same no-store streaming EOF/cancellation defect. The repaired
loader records terminal state before notifying its stream client. Stable
Playwright 1.64.1 still fails; 1.65.0 is not published. The exact test-only
**1.65.0-alpha-2026-10-29** pin provides Chromium 157.0.8103.15 / revision
1252. Its observed `Browser.getVersion` revision is
`1aa07b638799e59656559ae6c6649d4b197f555e`. Official source at that
revision confirms `HandleResult(result)` precedes the client notification;
the file SHA-256 is
`979ce76e70424734af0539e1858345b2e000a422fba5d34170d4748364143047`.
Gitiles retrieval timed out; the official Chromium GitHub mirror provided
that immutable source. Authenticated upstream GitHub reads were SAML-denied;
public official issue/API pages supplied the cited evidence.

The [ADR follow-up](../../docs/adr/0007-authentication-bootstrap-boundary.md#implementation-follow-up-pinned-chromium-stream-completion-repair)
records the stable-version exception and prerelease compatibility risk.
Only three reviewed npm identities change; all archive integrity,
Apache-2.0 notice hashes and graph checks pass, retaining 403 npm / 87
NuGet identities. Exact overrides align Next/axe test peers after the
initial unaligned Page types failed compilation. No cast, production reader
change, relaxed no-store/capture check, deadline extension or skipped case is
used. The new dependency regression first fails for the rejected exact
prerelease, then passes while moving tags/ranges remain forbidden.

At `0cd054a` plus this repair/tooling increment, pinned SDK selected:

| Exact executed command | Actual result |
| --- | --- |
| `python3 -m unittest scripts.tests.test_dependencies -v` | Passed 15 cases after the expected initial exact-prerelease rejection. |
| `npm run typecheck --workspace tests/e2e` | Initially failed with mixed stable/canary Page types; passes after exact peer alignment, without casts. |
| `npm run dependencies:check` | Passed actual installed/archive/lock/notices graph, 403 npm / 87 NuGet. |
| `npm audit --json` | Passed, zero vulnerabilities after final peer alignment. |
| `python3 scripts/verify.py --suite quality` | Passed 21 steps, 35.819s; `quality-none-812f3f3fb2f849a1881b35d28d81b0f3.json`. |
| `python3 scripts/verify.py --suite browser --disposable-postgres` | Passed root/Development and both native providers, 92.565s; `browser-none-34a2cf023ef7411dbec5efeadbd3871a.json`. Each provider: two passed, zero skipped, one required class. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed 150 cases, 5.689s. |
| `git diff --check` | Passed. |

Two earlier isolated reruns also pass: the twelve SQLite provider-fault theory
cases (31.779s) and the PostgreSQL production browser fact (42.856s).
Those unchanged reruns do not diagnose their prior failures. The completed
`092ce94` automatic matrix passes PostgreSQL integration, but SQLite still
fails enrollment's initial expected303/actual503 and provider-request counts.
No full-gate pass is inferred. Matching actual Linux qualification of the
upstream repair and T092-T100 remain outstanding.

## Continued implementation: distinguish network failure from client cancellation

The retry synchronization checkpoint is published as
`07eb672e457cd525fe0b92aaf41889002001a2c4`. Actual Linux
[run 40912819509](https://github.com/Stonefly-Labs/Kofte/actions/runs/40912819509)
still fails both native SQLite browser facts. Its safe report
`browser-none-33bb47b087a5486c881b7cf36d31bb86.json` records Development
context requests at 173ms, headers at 221ms, request failure at 223ms and
missing body resource at 226ms, before selection starts at 272ms. The UI
subsequently reaches signed-in. The production context body also fails at
3.284s. Root browser tests pass; PostgreSQL and public recipes do not execute.
Thus the real retry test gap was not a sufficient capture root-cause repair.

The test observer now categorizes only fixed Chromium abort, length-mismatch,
encoding and unclassified network failures. It also observes the actual client
AbortSignals for context/current-user fetches, returning the original fetch
promise with unchanged arguments. No raw error text is projected; unknown
failures remain fatal. These observations distinguish hypotheses, not a
demonstrated product fix. Capture, TLS, focus, analyzer and deadline guards
remain unchanged.

At `07eb672` plus these observations, with the pinned SDK selected:

| Exact executed command | Actual result |
| --- | --- |
| `node --test tests/e2e/identityPrivacyCapture.unit.ts` | Passed 16 cases, zero skipped; private unknown network text is not projected and failure remains fatal. |
| `python3 scripts/verify.py --suite quality` | Passed 21 steps, 58.750s; `quality-none-7a93833bb45c4e08ac4dd2c3a819ddbb.json`. |
| `python3 scripts/verify.py --suite browser --disposable-postgres` | Failed, 186.243s; `browser-none-e66e59ed3172408eaef29d17176a677c.json`. Root/Development and native SQLite pass. PostgreSQL production fails a text assertion after signed-out capture succeeds; no request/body failure is recorded in that timeline. This is not a both-provider pass or a diagnosed cause. |
| `python3 scripts/check_repository.py` | Passed after documentation reconciliation. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed 149 cases, 5.397s. |
| `git diff --check` | Passed. |

The completed earlier `cb188df` automatic matrix also times out its retained
composition contract selection at 900.054s before later identity contracts;
its recovery job passes. Prior native provider failures and the isolated
propagation-baseline timeout remain unresolved. T092-T100 stay open.

## Continued implementation: await the actual retry cycle

The diagnostic checkpoint was published as
`092ce94e67d18ec00c25c46f6783a3bb9d321e6b`. Its actual Linux
[manual run 40911153756](https://github.com/Stonefly-Labs/Kofte/actions/runs/40911153756)
and [automatic browser job 40911155006](https://github.com/Stonefly-Labs/Kofte/actions/runs/40911155006)
pass Development but fail the production retry journey. The manual report
`browser-none-a134ffd4588d459abc6db4be61515380.json` records a fresh
context/current-user check at 3.463 seconds, context headers at 3.483 seconds,
then request failure and `capture_context_body_failed` /
`capture_body_resource_missing` at 3.485 seconds. There is no intervening
blur/focus/visibility event. The independent automatic timeline records the
same resource-missing/context-abort boundary at 4.711 seconds. This is not a
Chromium launch, TLS or axe-finding failure.

The keyboard-retry loop asserted the same signed-out text that was visible
before Retry, then immediately started the next sign-in. That assertion did
not prove the new verification completed. The loop now holds only the real
context request, proves its fresh loading state and disabled retry affordance,
releases the unchanged request, waits for signed-out/focus and drains the strict
capture before the next deliberate sign-in. This follows the existing initial
loading-state gate; it adds no synthetic response, sleep, ignored abort, legacy
analyzer mode, focus emulation or deadline extension. Fixed retry phase markers
are source-allowlisted. This corrects a demonstrated test synchronization defect;
it does not establish a cause for every earlier Development/native failure.

At `092ce94` plus this synchronization increment, with the existing pinned SDK:

| Exact executed command | Actual result |
| --- | --- |
| `python3 scripts/verify.py --suite quality` | Passed 21 steps, 45.051s; `quality-none-0e176fe5951145008abf35af72d3367f.json`. |
| `python3 scripts/verify.py --suite browser --disposable-postgres` | Passed root/Development and both native providers, 137.155s; `browser-none-769ef5e0d70a40c688b014ffd2c7f202.json`. Each provider passed two cases, zero skipped and one required class. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed 149 cases, 6.276s. |
| `git diff --check` | Passed. |

The earlier `cb188df` automatic assessment additionally fails native SQLite
`DeliveredProviderFailuresAreBoundedWithoutRetryRedirectOrPersistentIdentity`
at its provider-request count assertions (104/106), and three PostgreSQL
enrollment/key positive cases with expected 303 / actual 503. Isolated
PostgreSQL propagation baseline times out at the unchanged 900-second limit,
before injections. These are retained engineering failures, not removed session
capacity tests or a human approval gate. T092-T100 remain open.

## Continued implementation: actual Development capture diagnostics

The published session-test removal checkpoint is
`cb188df10933292ee0f559728a4ee5a9b4ade846`. Actual targeted Linux x64
[run 40908757787](https://github.com/Stonefly-Labs/Kofte/actions/runs/40908757787)
fails the native SQLite Development fact with
`identity_browser_capture_identity_body_failed`. Its production
cancellation/refusal/retry fact passes; PostgreSQL and the twelve later public
recipes do not execute. The automatic PR browser job in
[run 40908717056](https://github.com/Stonefly-Labs/Kofte/actions/runs/40908717056)
independently fails the same Development fact. Its privacy job fails the
public SQLite recipe with `identity_browser_transport_capture_missing`.
Repository checks, quality, unit and startup pass; other product jobs were
still running when inspected. The automatic report's merge revision
`46b86112b242a4c51a0fbef5c3f338c7c6875ca5` has verified parents
`3e416813233c9d291eb20cf72d5882dbddf3e1b6` and `cb188df10933292ee0f559728a4ee5a9b4ade846`.
No complete matrix pass is claimed.

The failed Development path uses a separate Playwright JSON child, not the
instrumented full-Chromium production helper. Its `finally` capture cleanup
could also hide an earlier journey assertion. The test now preserves both
errors, retaining the original as `cause` because the pinned Playwright
serializer does not serialize `AggregateError.errors`. Capture failure still
fails a successful journey. The child now emits the same bounded, closed
request/body and client-lifecycle phases; the native JSON projector validates
those envelopes and classifies assertion names without retaining matcher values.
Only fixed protocol resource-missing/target-closed categories are projected,
never raw browser errors. No request, body, TLS, focus, cancellation, analyzer,
deadline or cleanup guard is waived. This is diagnostic work, not an
established response-capture fix.

Focused regressions first failed (two absent `run` methods; one absent native
matcher/phase projection), then pass:

| Exact executed command | Actual result |
| --- | --- |
| `node --test tests/e2e/identityPrivacyCapture.unit.ts tests/e2e/identityBrowserOwnership.unit.ts` | Passed 22 cases, zero skipped; both errors are preserved and successful journeys still fail missing-body capture. |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore -c Release --filter-query '/*/*/IdentityBrowserFailureDiagnosticsTests/*' --minimum-expected-tests 1` with the pinned SDK selected | Passed 17 cases, zero skipped; known child phases/cause retained, private matcher values and extra-field envelopes rejected. |
| `python3 scripts/verify.py --suite quality` with the pinned SDK selected | Initial increment failed ESLint's unsafe-finally/cause rules; preserving both errors outside `finally` corrects it. Final run passed all 21 steps in 41.936s, `quality-none-c6912748daeb40c8bb8a66e51d7c51b6.json`. |
| `python3 scripts/verify.py --suite browser --disposable-postgres` with the same SDK | Passed root/Development and both native providers, 155.772s, `browser-none-dccede2d2aab4c50857d622d040efb87.json`. Each provider passed two cases, zero skipped and one required class. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed all 149 cases. |
| `git diff --check` | Passed. |

These are local results at `cb188df` plus the diagnostic increment, not a Linux
capture fix or complete-gate pass. T092-T100 remain open.

## Continued implementation: human-directed session-test scope

The human's explicit 2026-10-29 direction **"We will NOT test it"** removes
the installation-wide 50,000-session boundary proof. The subsequent
`/drive-speckit` invocation resumes only the recorded 005 implement stage,
not feature acceptance or later MVP stages. The
[ADR amendment](../../docs/adr/0007-authentication-bootstrap-boundary.md#human-directed-verification-scope-amendment-installation-session-capacity)
records the changed verification obligation; the runtime cap is unchanged.

Removed both shared provider-inherited storage/HTTP boundary methods, their
exclusive population/snapshot helpers and C#/TypeScript corpus entries.
They are not skipped, replaced or reported as passing. Other session, expiry,
revocation, rate, evidence-retention and recovery tests remain. Historical
50,000-session executions below retain their original results.

The contract-project Release build with `--no-restore -warnaserror` passes,
zero warnings/errors. The initial quality command failed `dotnet-version`
because the shell had not selected the repository SDK; selecting the existing
`artifacts/toolchain/dotnet-10.0.402` fixes that invocation without installing
or changing any dependency.

The existing C# browser projector already retained safe phase timings in its
exception, but the Python report reduced them to code names. New regressions
first failed because `hostBrowserPhases` was absent, then pass after preserving
at most 64 source-allowlisted phase/launch/elapsed observations. Invalid types,
out-of-range values, unknown codes, passed/unknown native tests and private
suffixes are rejected. The isolated propagation reader revalidates the same
schema rather than trusting its child report. Raw output is still discarded.

The browser helper now observes fixed blur/focus/visibility categories without
changing focus, cancellation, analyzer mode or deadlines. Capture progress adds
the actual context request/response/body-completion boundaries. These are
diagnostic observations, not a claimed Chromium fix. CI manual dispatch adds
only `suite=browser`; normal PR/main and default manual runs retain all ten
unchanged contexts, commands, pinned tools and limits.

| Exact executed command | Actual result |
| --- | --- |
| `python3 -m unittest scripts.tests.test_runtime_support.RuntimeSupportTests.test_host_browser_timing_retains_only_known_failed_test_phases_and_bounds scripts.tests.test_runtime_support.RuntimeSupportTests.test_host_browser_timing_keeps_early_and_final_evidence_with_a_fixed_limit scripts.tests.test_failure_propagation.FailurePropagationTests.test_host_failure_evidence_is_revalidated_inside_isolated_reports -v` | Initial three errors for absent timing fields; after the correction all three pass. |
| `node --test tests/e2e/identityPrivacyCapture.unit.ts tests/e2e/identityBrowserOwnership.unit.ts` | Passed 20 cases, zero skipped; strict missing-body, abort, framing, ownership and close guards retained. |
| `npm exec -- prettier --write tests/e2e/identityBrowser.ts tests/e2e/identityPrivacyCapture.ts .github/workflows/product-checks.yml` | Passed; formatting unchanged. |
| `python3 scripts/verify.py --suite quality` with the existing pinned SDK selected | Passed all 21 steps, 43.610s; `quality-none-f5079b7085ec4d9391b06d2c72009faa.json`, including 149 Python cases, formatting/build/types/dependency/contract checks. |
| `python3 scripts/verify.py --suite browser --disposable-postgres` with the same SDK | Passed, 153.122s; `browser-none-07591db0ceb144379b8c107f17e247ca.json`. Native SQLite/PostgreSQL each passed two cases, zero skipped, with their required class admitted. |
| `python3 scripts/check_repository.py` | Passed after documentation reconciliation. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed after documentation reconciliation: 149 cases, 6.913s. |
| `git diff --check` | Passed after documentation reconciliation. |

These are local results at `d14764f` plus this scoped working-tree increment;
matching Linux assessment and remaining diagnosis continue. No rejected capacity
test was run. No complete-gate or assessed-head CI pass is inferred.
T092-T100 remain unchecked; the same implementation stage is resumed/running.

## Continued implementation: short private Chromium temporaries

The verifier-owned canary repair was committed and immediately pushed as
`c7f68a786ebc51c935d8df455d926d79d88a8725` on the same branch/draft PR.
Its complete local gate and all actual hosted contexts were assessed with
unchanged source, rather than treating an isolated pass as completion.

### Reproduced Linux launch failure and correction

Hosted root/Development browser tests now pass in 50.969s, with six actual
canaries consumed and fourteen owned transport streams scanned. The hosted
browser job instead fails later: the native SQLite cancellation/refusal/retry
fact reaches only initial Chromium launch, then reports unclassified launch,
unverified cleanup and absent transport capture. This is a different failure
from the repaired export handoff; the job did not reach PostgreSQL browser
selection or its subsequent public recipes.

The full Chromium process creates a singleton Unix socket under inherited
`TMPDIR`. The actual CI scratch-root layout is 74 bytes before Chromium adds its
directory/socket components, exceeding Linux's 108-byte socket address field.
Two isolated, network-disabled, non-root Linux arm64 probes using the pinned
Playwright 1.64.1 image
`mcr.microsoft.com/playwright@sha256:47b510dd9230bf0b9bfc9b20102241e577ea7a99334ab4c3ca19f19770e9b8ea`
reproduce the difference: a 74-byte temporary root refuses launch with the
finite `unix_socket_path_limit` category; a fresh 40-byte private profile-owned
temporary root launches and closes successfully. Each container was restricted
to one CPU/1 GiB and removed after exit. No protocol capture or personal profile
was mounted or published.

`identityBrowser.ts` now creates a fresh canonical short private parent under
the system `/tmp`, then its private profile. Only Chromium inherits that profile
as `TMPDIR`/`TMP`/`TEMP`; the caller's environment is unchanged. The existing
process/session/birth/profile ownership checks and both close protocols still
precede profile removal. The private parent is removed only after verified
profile removal, using non-recursive removal so unexpected contents fail.
Unverified launch/termination still preserves private partial state and fails.
The existing native regression explicitly gives its Node child a long owned
temporary root, rather than relying on a particular checkout length.

A separate isolated Linux run of the **actual changed helper and unchanged
ownership implementation**, with generated test CA/leaf and a loopback HTTPS
server, passed initial untrusted-certificate refusal, stopped private CA import,
fresh trusted navigation, both observed process lifecycles and complete private
profile/parent removal. It retained zero owned profiles and left the long Node
environment unchanged. An initial probe had an unwritable image-default HOME
and failed before that proof; after supplying the same private writable HOME
as the paired probes, it passed. That setup failure is not a product result.
This arm64 container proof does not substitute for the pending x86_64 hosted
assessment of the changed source. No TLS bypass, sandbox/owner relaxation,
deadline increase, package or production source change was introduced.

| Exact executed command | Actual result |
| --- | --- |
| `npm exec -- prettier --write tests/e2e/identityBrowser.ts` | Passed; formatting unchanged. |
| `npm run lint --workspace tests/e2e` | Passed. |
| `npm run typecheck --workspace tests/e2e` | Passed. |
| `dotnet build tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release -warnaserror` | Passed, zero warnings/errors. |
| `node --test tests/e2e/identityBrowserOwnership.unit.ts` | Passed seven cases, zero skipped, including PID reuse/substituted profile/foreign group refusal, verified termination and deferred-close protocol failure. |
| `python3 scripts/verify.py --suite browser --disposable-postgres` | **Passed**, 190.257s; `browser-none-3af0129a5b804fe49513eb7e566f8a72.json`. Six actual canaries consumed; native SQLite and PostgreSQL each passed two cases, zero skipped, including the long-temp regression. |

`python3 scripts/verify.py --suite quality` then passed all 21 steps in
37.802s (`quality-none-75a9e169f1d04a2eade4e303703c471d.json`), including
repository checks, all 147 Python cases, whitespace, locked restores, .NET
format/build, frontend format/lint/types/build, generated contracts and
dependency review. The exact final `python3 scripts/check_repository.py`,
`python3 -m unittest discover -s scripts/tests -v` and `git diff --check`
also pass after documentation-only reconciliation (147 Python cases).

### Complete local gate at published c7f68a7

`python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10`
completed with **exit 1**. All ten suites executed at the exact published head
with fixed source. Every report has `scope: all` and `completeGate: false`.
Profile remains Darwin 25.5.0 arm64, eighteen logical CPUs, 68,719,476,736 memory
bytes, Python 3.15.0 and the pinned product tools; no timing cause is inferred.

| Suite | Actual result / safe report |
| --- | --- |
| Quality | Passed all 21 steps, 48.949s; `quality-none-835e774fde0f48768e5aa02d8bdfd432.json`. |
| Unit | Passed all 16 steps, 52.160s; `unit-none-ec6e55524b424dbaba3ab199e6174b84.json`. |
| SQLite integration | Passed host209/storage237/composition57, zero skipped, 564.641s; `integration-sqlite-90d776898d0142a0bd37d734014b9cce.json`. |
| PostgreSQL integration | Passed host119/storage212/composition59, zero skipped, 774.041s; `integration-postgres-8b76cc9f185e462ca16433feb5acec25.json`. |
| Recovery | Passed both providers' native/storage/public/composition/browser selections, zero skipped, 1039.965s; `recovery-both-242377fcd32a492fa13630ca766937b6.json`. |
| Contract | **Failed** `platform-public-contracts`, 532.288s, in PostgreSQL `ActualStoppedModeTransitionsPreserveEqualProfileNamespacesAndConsumedProductionAuthority`; known `DevelopmentIdentityTests.cs:129,157,178`. No finer status/cause was projected. Composition and identity catalogs did not execute. `contract-both-0413c555b62a4836aad7a62fc3eab72f.json`. |
| Browser | Passed complete root plus both native providers, zero skipped, 141.435s; `browser-none-0a7c5b589aa24e678e742d381aca4f2a.json`. |
| Privacy | **Failed** PostgreSQL public `ActualIdentityJourneysRequireTheirPrivateCapturedEvidence`, 324.204s, at `IdentityGuideTests.cs:51` / `IdentityConsumerHarness.cs:90,99`; no finer cause projected. Earlier SQLite channels/native browser and PostgreSQL storage capture passed; later PostgreSQL selections did not execute. `privacy-both-a065a65e568f47b8829a58503cde73bc.json`. |
| Startup | Passed, including ten empty warm starts, 16.426s; `startup-none-a063553f400a4ec09eedf490c6663780.json`. |
| Failure propagation | **Failed** isolated PostgreSQL baseline, 759.446s, before browser baseline/injections. Storage failed nineteen mapping/maintenance/privacy/recovery facts, including shared fixture/registration locations; host and composition passed. `failure-propagation-none-21dde7cd8ef442da947badd9ade3adba.json`. |

The isolated failure facts are nine `PostgresIdentityMappingTests` methods,
five `PostgresStorageMaintenanceDiagnosticsTests` methods,
`PostgresStoragePrivacyTests.AllSevenResultsConsoleSpansEventsMetricsAndWorkerOutputsExcludeSeededSecrets`
and four `PostgresRecoveryDiagnosticsTests` methods. The safe report retains
their exact names/source locations, including `ModuleFixture.cs:33`,
`RegistrationTests.cs:248,263`, `IdentityAdmissionTests.cs:246` and
`LoginCommitFixture.cs:119,147`. No private arguments, raw exception values or
causal diagnosis are inferred from this broad failure. The passing direct
PostgreSQL selection does not replace the failed isolated baseline.

### Actual c7f68a7 hosted assessment

[Repository checks 40882687236](https://github.com/Stonefly-Labs/Kofte/actions/runs/40882687236)
**passed**;
[Product checks 40882687269](https://github.com/Stonefly-Labs/Kofte/actions/runs/40882687269)
**failed**. All ten safe reports were inspected; no raw protocol logs were
downloaded. Reports assess merge-test revision
`067a30e80491d9ee069e322296f793f04429b6cc`, whose verified parents are main
`3e416813233c9d291eb20cf72d5882dbddf3e1b6` and the published feature head.
Profile: Linux 7.0.0-1017-azure x86_64, four logical CPUs, about 16.76 GB memory,
Python 3.12.3, SDK10.0.402, Node24.22.0/npm11.19.1.

Quality/unit/SQLite integration/recovery/startup passed. Browser failed the
separate native launch described above after its corrected root selection.
PostgreSQL host failed enrollment login with expected303/actual503; storage
failed the 50,000-session capacity fact. Contract timed out in retained
`composition-public-contracts` at 900.057s before identity catalogs. Privacy
failed SQLite public private evidence with `identity_browser_transport_capture_missing`
in 84.979s. Failure propagation failed the isolated storage
`OriginalModuleDataAndConsumedDesignationSurviveRestartsPreparationAndUnavailableAdministrator`
fact; its host119/composition59 passed, zero skipped, but injections did not
execute. These outcomes do not diagnose the remaining native failures.

T092-T100 remain open, and the driver records the failed verification boundary
without completing implement. The Chromium correction must receive its own hosted
assessment; the complete local failures, recurring native 503s, hosted contract
timeout and incomplete propagation baseline are still engineering work. Neither
passing probes nor this documentation advances converge/review/acceptance.

## Continued implementation: verifier-owned browser canaries

The original branch/open draft PR and both completed checklists were verified
again, and the same implement stage resumed. No new approval, stage, branch,
product dependency or deadline was introduced.

### Actual browser failure and correction

The exact browser selection was reproduced before editing. A transient JSON
reporter inspection identified **only `identity.spec.ts:99` as failed**:
`identity_browser_private_canary_export_failed`. The root accessibility,
bootstrap and recovery cases passed. The old location projector accepted their
successful list-reporter declaration lines, so its retained locations were not
evidence that those assertions failed. The earlier failed run outcomes remain
failed, but the root-page attribution below is superseded by this reproduction.
No focus/visibility change or relaxed assertion was needed.

The root verifier runtime had not provided `KOFTE_IDENTITY_PRIVATE_ROOT` for the
delivered Development journey's mandatory export. The browser suite now owns a
separate 0700 export directory, supplies it only to the test process and requires
the actual single-use export after all owned transport processes stop. The
reader checks canonical ownership, regular single-link 0600 file, bounded strict
UTF-8/JSON, duplicate/unknown fields, exact numeric version and unique bounded
canaries. Missing/invalid exports fail; they never become optional capture.
Raw and URI-encoded canaries are added to the existing actual transport-output
scanner before its buffers are scanned and discarded. Only the count is
reported. File content is zeroed and the admitted export is consumed, not
published. Browser diagnostics now retain only failure headings/stack locations,
including the known identity test file, not successful declarations.

| Exact executed command                                                                | Actual result                                                                                                                                                                                                                                                                                                                                    |
| ------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `python3 -m unittest scripts.tests.test_runtime_support scripts.tests.test_verify -v` | Passed 84 cases, most recent 4.443s. Added regressions reject missing/malformed/duplicate/oversized/non-UTF-8/deep input, public permissions, symlink/hardlink/FIFO, replay and raw/encoded disclosure in actual prior transport buffers. The verifier regression proves test-only environment delivery and parent consumption.                  |
| `python3 scripts/verify.py --suite browser --disposable-postgres`                     | **Passed**, 158.676s; safe report `browser-none-d7c974e5187f41ec952bf06591f2a84b.json`. The complete four-file root/Development selection passed in 47.589s, actual export supplied six canaries, and owned cleanup/output scan passed. Native SQLite and PostgreSQL browser selections each passed two cases, zero skipped, in 36.068s/66.681s. |

`python3 scripts/verify.py --suite quality` subsequently passed all 21 steps in
47.219s (`quality-none-ec694e92abf44af3a7f97f4217a5cfe7.json`), after removal of
the temporary native diagnostics. This executes the exact required
`python3 scripts/check_repository.py`,
`python3 -m unittest discover -s scripts/tests -v` and `git diff --check`;
all passed. It also passed locked restores, .NET format/warnings-as-errors
solution build, Prettier/ESLint/TypeScript/workspace builds, generated-contract
and dependency verification. No package or product source changed.

This closes T090 using the actual state coverage below, not just test existence.
The production consumer journeys are unchanged from the earlier **passing owning
both-provider catalogs** in `contract-both-3b51b1b55111486a9e0e9d122664ae29.json`;
their historical initially failed catalogs remain failed.

| State/evidence                                                                                    | Actual assertion and execution                                                                                                                                                                                                                                                                                    |
| ------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Loading, signed-out, cancelled, refused, provider-unavailable and corrected signed-in             | `identity.failures.ts` exercises real HTTPS callbacks through owned issuer/host/Next/ingress; the new owning browser run passes both providers. Each state checks live/atomic announcements, keyboard/visible focus, 320px/200% reflow and no serious/critical axe IDs. Deliberate retry does not resubmit login. |
| Development signed-in/signed-out and actual host-unavailable/retry                                | `identity.spec.ts` now passes in the complete root selection; the independent native Development facts pass each provider and scan actual transport/canary evidence.                                                                                                                                              |
| Idle/absolute expiry, second-user clearing, confirmed sign-out and uncertain lost acknowledgement | The earlier owning `session-boundary` catalogs run `identity.production.ts` via actual session controls on both providers; all nineteen exact observations pass, including the 320px/200%/axe/keyboard assertions around expiry and uncertainty.                                                                  |
| Enrollment refused/uncertain and fresh designation                                                | The earlier owning `nominated-enrollment` catalogs pass all seventeen observations on each provider, including actual lost acknowledgement, protected-input focus, fresh `/me` and no repeated enrollment.                                                                                                        |
| Root loading/ready/unavailable and recovery                                                       | The complete root selection passes its 320/768/1440px axe, 200% fallback/reflow, keyboard, actual store/host loss and stale-attempt cases; no assertion was removed.                                                                                                                                              |

Profile: Darwin 25.5.0 arm64, 18 logical CPUs, 68,719,476,736 memory bytes,
Python 3.15.0, Node 24.22.0/npm 11.19.1, SDK 10.0.402/runtime 10.0.13 and the
pinned Playwright Chromium with an isolated trusted test CA, never personal/OS
trust. The native selection durations above include provider/setup/cleanup;
they are **not** browser-only or human-interaction timings. SC-005's actual
browser assertions measure **under five seconds after the trusted callback
request**; provider exchange before that return and user decision time are not
folded into that measurement. No independent provider-latency benchmark or human
usability-study result is claimed.

### Native investigation remains separate

An initial direct PostgreSQL probe lacked disposable admission and correctly
failed `postgres_disposable_new_target_required` before product execution. It
is not evidence of a product 503 or a passing selection.
With the required owned disposable admission, the complete 29-case
`PostgresOidcBudgetTests` selection failed in 224.949s
(`integration-postgres-c818465103964981a2e70adb79fe01b9.json`).
Its known failures include invalid evidence, DNS readmission and provider
refusals; finite status evidence is `expected_200_actual_503` and
`expected_303_actual_503`. A later twelve-case invalid-evidence selection,
29-case class and original 119-case host selection passed with temporary
finite-category/source-method diagnostics. Those diagnostics were removed;
they changed no deadline or assertion and established **no failure cause**.
These diagnostic passes are not a fix or replacement for the failed selection.

T092-T100 remain open. The next verification must use the unchanged required
complete gate and actual assessed-head CI, preserve previous failed outcomes,
and investigate any remaining failure rather than label it a human blocker.

## Continued implementation: strict bodyless and lost-ack privacy capture

The original branch/open draft PR and both completed checklists were verified,
and the driver resumed the human-requested pause as 005/implement/running.
This continuation does not advance another stage or change the approved
requirements, reviewer-owned checklists, product dependencies or deadlines.
The completed both-provider privacy suite and owning public catalogs now close
T089 and T091. T090 and T092-T100 remain unchecked; the stage is blocked, not
complete.

The actual Development browser failure was Chromium's unavailable body resource
for a legitimate HTTP 204 sign-out. The scanner now requires the exact private
bodyless response framing and successful response completion without calling
`text()` for that status. An observed abort still fails. Six regressions cover
completion, unexpected framing and aborts; both real provider Development facts
now pass.

The production ingress still actually aborts successful enrollment/sign-out
acknowledgements. Before that intentional loss, it captures the complete actual
upstream headers/body into the existing bounded, private, single-use receipt
boundary. Only the exact failed request can consume it: route/status, method,
path, request-body SHA-256, root/ID, freshness, file ownership, response framing
and receipt digest must match. The scanner examines the captured headers/body;
missing/replayed/mismatched receipts and seeded disclosures remain fatal.
The test correlation header is removed before forwarding upstream. No product
endpoint, deployment flag, synthetic success response or ignored abort is added.

The real session journey also exposed two test errors: the deliberately invalid
CSRF literal was too short to be a privacy canary, and the uncertain signed-out
state's first keyboard control is **Sign in**, followed by **Retry identity
check**. Both controls are now verified in that actual order. Status-only
browser probes consume their responses. Unsafe-return probes now await and
scan the actual context/identity bodies before navigating again; a direct
PostgreSQL login reproduction failed drainage before that change and passes
after it.

Hosted unknown-path/version parity failures were independently reproduced on
both providers: the strict transport guard correctly refused an absent ingress
channel because unknown routes generated no access records. The test ingress
now emits a fixed `unknown_identity` operation category, not raw paths, identity
or query values. Both original negative facts then pass without relaxing the
three-channel scanner.

Failure diagnostics project only known codes and finite source locations from
bounded Playwright JSON and production observations. They never retain private
matcher values, raw protocol output or arbitrary error text.

| Exact executed command / selection | Result |
| --- | --- |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror` and `dotnet format Kofte.slnx --verify-no-changes --no-restore` | Passed after the joined capture/ingress changes. |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-build --no-restore -c Release --filter-class '*IdentityBrowserFailureDiagnosticsTests' --minimum-expected-tests 16` | Passed 16, zero skipped. |
| `node --test tests/e2e/identityPrivacyCapture.unit.ts tests/e2e/identityCanaryExport.unit.ts tests/e2e/identityActionCapture.unit.ts tests/e2e/identityBrowserOwnership.unit.ts` | Passed all 35 cases, zero skipped, including actual-body canary, wrong request/route, replay, framing and ownership refusals. |
| `npm run lint --workspace tests/e2e` and `npm run typecheck --workspace tests/e2e` | Both passed. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-build --no-restore -c Release --filter-class '*SqliteIdentityBrowserFailureTests' --filter-display-name '*RealDevelopmentSelectionAndSignOutRequirePrivateBrowserAndTransportEvidence*' --minimum-expected-tests 1` | Passed 1, zero skipped, 7.5s. PostgreSQL counterpart with disposable admission passed 1, zero skipped, 13.5s. |
| `dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario nominated-enrollment --provider sqlite` | Passed all 17 exact observations, including actual lost acknowledgement and fresh designation without retry. |
| Same consumer with `--scenario session-boundary --provider sqlite` | Passed all 19 observations after the keyboard/capture corrections. |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*IdentityGuideTests' --minimum-expected-tests 20` | **Failed:** 17 passed, 3 failed, zero skipped; 943.947s. All ten SQLite rows passed; PostgreSQL `oidc-login`, `all` and `session-boundary` failed. This incremental diagnostic run is not proof of the named 900-second gates. |
| Direct PostgreSQL `oidc-login` and `session-boundary` consumer commands with `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true` | Each subsequently passed its exact 8/19 observations. The unsafe-return drainage failure was reproduced and corrected; the isolated session pass does not establish the earlier session failure's cause or replace the failed catalog. |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*IdentityParityTests' --filter-display-name '*UnknownIdentityPathsAndVersionsRemainEmptyPrivate404sForEveryAdmittedMethod*' --minimum-expected-tests 2` | Initially failed both providers with `identity_browser_transport_capture_missing`; after fixed-category ingress capture, passed 2, zero skipped, 25.439s. |
| `dotnet run --project tests/contract/Kofte.Identity.Consumer -c Release --no-build -- --scenario all --provider sqlite` | The guide's literal command passed all nine exact scenario catalogs and 235 observations. This separate pass does not replace the owning-harness or complete-gate results. |
| `python3 scripts/check_repository.py && python3 -m unittest discover -s scripts/tests -v && git diff --check` | Passed before reconciliation (143 Python cases, 7.308s) and again after final ledger/task/documentation reconciliation (143 cases, 5.707s). |

An inherited `--filter-method` selector initially ran zero tests and exited 8;
the explicit display-name selections above executed the required facts.
The initial complete ten-row SQLite catalog failed three rows before these
fixes; that failure is preserved, not reclassified by later focused passes.

### Exact complete-gate result

`python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10`
completed serially with **exit 1**. Product/test source stayed unchanged throughout
the run. All ten safe reports have `scope: all`, `completeGate: false` and the
baseline above. Durations below are suite durations, not expanded command or
product deadlines. Reports remain ignored under `artifacts/verification/`.

| Suite | Actual result | Safe report |
| --- | --- | --- |
| Quality | Passed all 21 steps, 33.222s, including full build/format/lint/types, dependency and generated-contract checks. | `quality-none-adc5200481ea4a4ab0be4adf14a4e3a1.json` |
| Unit | Passed all 16 steps, 52.781s, including 35 browser helper cases and 143 Python tests. | `unit-none-6ef26d10979646f0a13713834a741e10.json` |
| SQLite integration | Passed, 567.762s: host 209, storage 237 and composition 57 cases; zero skipped. | `integration-sqlite-15d6a68482644e1187a3f40161414a59.json` |
| PostgreSQL integration | Failed, 815.012s: host and composition fail; storage passes 212 cases, zero skipped, 293.993s. | `integration-postgres-6ee1d51737f34d97b9c96e454b745ed0.json` |
| Recovery | Failed, 660.781s: SQLite native/storage/public/composition/browser steps pass; PostgreSQL native key recovery fails before later PostgreSQL steps execute. | `recovery-both-0dcbdc656dbf4c9d87d13acad6400fda.json` |
| Contract | Passed, 2715.759s: native platform 267 and composition 84 cases; each provider's complete ten-row identity catalog passes, zero skipped, in 337.105s/664.531s. Direct/adapted HTTP and all retained composition recipes also pass. | `contract-both-3b51b1b55111486a9e0e9d122664ae29.json` |
| Browser | Failed, 53.890s: retained accessibility/bootstrap/recovery source locations below; runtime cleanup and output scan pass. | `browser-none-044a1070dcc84b628c58ed2d78763674.json` |
| Privacy | Passed both providers, 648.677s: actual storage/public/composition/native identity browser, HTTP/browser and supervisor captures all pass. | `privacy-both-8160747168c44324839e289f80790dcf.json` |
| Startup | Passed, 13.714s, including ten empty warm starts and the retained composition starts/observations. | `startup-none-44ca257cdbae4a24a8b8da80e5bb0f34.json` |
| Failure propagation | Failed, 716.337s: isolated quality and PostgreSQL baselines pass; isolated browser baseline fails with `failure_baseline_not_green`. Injected failure exercises therefore do not execute. | `failure-propagation-none-bdd8be114e7e4ac68d77d3d92c3ef798.json` |

This full contract result establishes every individual recipe plus `all` through
both owning harnesses; the earlier 17/20 diagnostic catalog remains failed.
The operational recipes validate exact 8/13/9 observation catalogs for
upgrade preservation, interrupted recovery and private evidence, including
IdP-loss/no-reassignment and stale-copy/unknown-history refusal. Together with
the completed privacy suite, this closes T089/T091, not T094's unfinished
dependencies or T097's complete gate.

The privacy suite passes 17 SQLite and 21 PostgreSQL native storage cases, seven public storage
cases per provider and both composition privacy selections per provider.
Both native identity browser facts pass on each provider, zero skipped, in
35.588s/52.633s: real Development selection/sign-out and actual cancelled,
refused, unavailable and corrected OIDC callbacks. These join the complete
production login/enrollment/session journeys and private actual .NET/Next/
ingress scans; absent capture, wrong receipts, aborts, disclosure and unverified
cleanup still fail closed. No human usability study is claimed. T090 stays
open because the complete retained accessibility/browser selection fails and
its final profile/SC-005 reconciliation is unfinished.

### Remaining failures and next resume

The direct PostgreSQL host failures are these six source-known facts:

- `PostgresEnrollmentTests.EnrollmentChecksCurrentCeremonyAndExactNomineeWithoutReopeningHistory`
- `PostgresEnrollmentTests.OrdinaryProductionLoginCannotDesignateAndProtectedEnrollmentNeedsAnOperatorCeremony`
- `PostgresIdentityKeyTests.KeyFailureCannotRepairItselfOrReplaceConsumedAuthority`
- `PostgresOidcBudgetTests.DeliveredBrowserLoginRateRefusesTheEleventhAttemptBeforeProviderOrIdentityEffects`
- `PostgresOidcBudgetTests.DeliveredInvalidEvidenceCannotCreateAUserSessionOrCommittedLoginEvent`
- `PostgresOidcBudgetTests.DeliveredProviderFailuresAreBoundedWithoutRetryRedirectOrPersistentIdentity`

The safe host projection records `identity_fixture_browser_context_failed`
at `LoginCommitFixture.cs:507` and `expected_303_actual_503` at actual login/
enrollment assertions, including `OidcBudgetTests.cs:234` and
`IdentityKeyTests.cs:132`. The separate recovery failure is the same key
fact **after retained-custody repair**, at `IdentityKeyTests.cs:40,160`,
with `expected_200_actual_503`. The composition step exits 2 in 187.566s;
its safe projection reports `no_known_failure`, not an established assertion
or diagnosis.

The isolated failure-propagation PostgreSQL baseline subsequently passes
all three commands in 619.530s: host 119, storage 212 and composition 59,
zero skipped, in 224.768s/284.307s/110.237s. This is actual passing nested
baseline evidence within the unchanged limits, **not** replacement of the
failed direct integration/recovery results or proof of their cause.

Both direct and isolated browser baselines retain failures at
`accessibility.spec.ts:5:3`, `bootstrap.spec.ts:3:1` and
`recovery.spec.ts:4:1,32:1,54:1`. The isolated browser suite fails in 52.641s.
These are source locations, not inferred matcher details or a diagnosis.
The previously timed-out PostgreSQL baseline is no longer the current
propagation blocker; the actual current blocker is its failed browser baseline.

Resume **005 / implement** on this same branch/draft PR with `/drive-speckit`
and explicit `SPECIFY_FEATURE_DIRECTORY=specs/005-authentication-bootstrap`.
First reproduce the retained browser assertions and native PostgreSQL
login/context/post-repair actor failures with safe diagnostics; preserve both
failed direct and passing nested evidence. Do not assign resource/timing/key
causes without proof, relax the one-second/product/900-second limits, suppress
cases or treat prior acceptance as a waiver. Finish T090 and T092-T099,
including delivered documentation, dependency reconciliation, a genuinely
passing complete gate, assessed-head CI and final traceability. T100 and
implement completion remain gated on that work. This invocation publishes
only the safe partial checkpoint; converge, review and acceptance do not run.

Prior-head CI was independently assessed at
`6f4e4a7e5f2246011fbe35928af759aa300317b3`:
[product run 40799306697](https://github.com/Stonefly-Labs/Kofte/actions/runs/40799306697)
**failed**. Quality, unit, recovery and startup passed; both integrations,
contract, browser, privacy and failure-propagation failed.
[Repository run 40799306721](https://github.com/Stonefly-Labs/Kofte/actions/runs/40799306721)
passed. Safe reports identify the reproduced missing-ingress negative,
SQLite key/invalid-evidence failures, a PostgreSQL session-capacity setup
failure, retained bootstrap/accessibility/recovery browser failures and the
nested PostgreSQL baseline timeout. These observations remain failures,
not diagnoses or results for this checkpoint's changes.

## Context-clear checkpoint: resume implement

**Historical pause, superseded by the continuation above.**

The human explicitly requested a resumable checkpoint on 2026-10-29 UTC.
This pauses work; it does **not** complete implement or require a new design
approval. The ledger records the pause as `blocked` with an actionable resume
note. **T001-T088 remain checked; T089-T100 remain unchecked.**

After clearing context, invoke **`/drive-speckit`**. Resume this same
`feature/005-authentication-bootstrap` branch and existing draft PR #8, not
main, a new branch, a replacement PR or another numbered feature. Read this
checkpoint, the newer execution details below and `tasks.md`, verify Git/PR
ownership, and use the driver's `resume --branch
feature/005-authentication-bootstrap`. Set
`SPECIFY_FEATURE_DIRECTORY=specs/005-authentication-bootstrap` explicitly on
every downstream command. Do not rerun specify/plan or infer implementation
completion from the existing artifacts.

Remaining work, in order:

1. Finish strict actual browser/privacy/accessibility proof. The existing
   callback fact now passes on SQLite and owned PostgreSQL, including confirmed
   close protocol; all 24 Node cases pass. The complete SQLite guides still
   fail with `identity_scenario_development_line_23` and
   `identity_consumer_failed`, without a finer browser phase. Start with the
   new inherited fact
   `IdentityBrowserFailureTests.RealDevelopmentSelectionAndSignOutRequirePrivateBrowserAndTransportEvidence`
   on SQLite and PostgreSQL: it compiles in the checkpoint quality build but
   has **not** executed. Preserve primary/disposal errors and owned private
   recovery receipts; never weaken capture/ownership guards or use personal
   browser profiles.
2. Execute all nine public recipes individually plus `all` on each provider
   through `IdentityGuideTests`; validate the exact catalog/observations and
   the guide's literal `dotnet run ... --scenario all --provider sqlite`.
   The three operational recipes already pass independently on both stores;
   that is not complete-catalog evidence.
3. Establish the full serial and nested PostgreSQL baseline within the original
   900-second bounds. The complete 119-case host selection passes in 339.057s,
   but the historical aggregate baseline remains failed. The unavailable-store
   exercise now shares and strictly requires all three exact ordered failing
   commands; its new 143-case Python proof is not actual propagation execution.
4. Close T089-T096 only from their actual dependencies/evidence, run the exact
   complete `python3 scripts/verify.py --suite all --disposable-postgres
   --warm-starts 10` plus repository/Python/whitespace checks, observe assessed
   CI, and finish T097-T100 traceability/publication. Preserve all historical
   failures. No deadline, case minimum, contender count or suite is waived.

Use the existing pinned local toolchain when needed:
`DOTNET_ROOT="$PWD/artifacts/toolchain/dotnet-10.0.402"` with that directory
prepended to `PATH`. No new package, provider account, personal secret or human
merge is needed just to resume. Complete **only implement** when genuinely
ready for converge; do not invoke converge, accept, mark the PR ready or request
an intermediate merge.

All retained agents are source-frozen; the browser native window is returned.
No known owned command/server remains active. Failed-case private roots are
intentionally preserved inactive receipts, not source to commit or directories
to remove without their ownership proof. Resume does not depend on retaining
agent IDs or this conversation.

Final checkpoint checks:
`python3 scripts/verify.py --suite quality` passes all 21 steps, including
locked restores, full solution warnings-as-errors build/format, web
build/lint/types/Prettier, generated contract and dependency review
(`quality-none-b419cbad00fb427eb6dc35d262ff3039.json`, 43.068s).
`python3 scripts/verify.py --suite unit` also passes all 16 steps, including
the current host/platform/composition/web/contract suites, all 24 browser
helper cases and 143 Python cases
(`unit-none-4638337b3a184dbf840056b8de7cfabe.json`, 51.038s).
The driver's real `Ledger.resume` also selects 005/implement/running in memory
on the original branch without changing the saved paused ledger; this verifies
the requested same-stage restart rather than merely the presence of files.
Required repository/Python/whitespace checks
pass independently (143 tests, 8.102s). These are targeted checkpoint results,
not the unfinished complete gate or new-head CI.

The latest browser reports are
`identity-browser-confirmed-close-sqlite-sqlite-cdced3adeb974c82a5da5efb3d22a722.json`
and
`identity-browser-confirmed-close-pg-guides-both-324fad39e8d94f5490fd5115a17ddbb4.json`.
The existing actual callback fact passes once per provider in 30.661s/30.662s,
with the unchanged 45-second child limit. The PostgreSQL probe uses a verified
owned local PostgreSQL 17.9 server; it is not an immutable-container-image run.
Its zero database/role/client-backend inventory and server stop are verified.
The same report remains **failed** because the complete ten-case SQLite guide
selection fails in 317.853s with the Development codes above. PostgreSQL's
complete guide selection is **unrun**, not passed. The new Development browser
fact was added afterward and is compiled, not yet actual behavior evidence.

## Continued implementation: closed diagnostics and current operational failures

The following newer executions still assess the dirty worktree based on
`203e0437e90011bf157d6927ddef639f257346d6`; they do not complete implement or
replace any historical failed result. The public consumer and owning harness
now share a finite failure codebook. Stopped-command failures retain only
strictly parsed, known operation/event codes, never private child output.
The verifier derives the corresponding finite operator, recipe and source-line
codes from source, rejecting unknown commands and out-of-range lines.

| Exact executed command / selection | Result |
| --- | --- |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror` | Passed after compiling the shared codebook and latest browser projections. |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-build --no-restore -c Release --filter-class '*IdentityBrowserFailureDiagnosticsTests' --minimum-expected-tests 9` | Passed 9, zero skipped. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*IdentityConsumerHarnessTests' --minimum-expected-tests 77` | Passed 77, zero skipped. |
| Same contract project with `--filter-class '*PostgresIdentityGuideTests' --filter-display-name '*upgrade-preservation*' --minimum-expected-tests 1` | Passed the actual PostgreSQL recipe, zero skipped, 40.345s. |
| Same contract project with `--filter-class '*IdentityGuideTests' --filter-display-name '*interrupted-recovery*' --minimum-expected-tests 2` | Failed both providers, 91.897s; exact closed code `identity_fixture_browser_context_failed`. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-query '/[(Suite=browser)&(Provider=sqlite)]' --minimum-expected-tests 1 --no-build` | Failed, 49.348s; `identity_browser_deadline_exceeded` at `identity_browser_stage_loading_signed_out_accessible`. The unchanged 45-second child limit is not relaxed. |
| `python3 scripts/check_repository.py && python3 -m unittest discover -s scripts/tests -v && git diff --check` | Passed; 141 Python tests, 5.879s, including strict constructed-code/private-source projection. |
| `npm run lint --workspaces --if-present && npm run typecheck --workspaces --if-present && npm run format:check` | All passed after the joined reader/diagnostic fixes. |

Safe reports:
`identity-closed-diagnostics-build-both-05634cba3a214023bdc4c5f3e6241309.json`
and `identity-specific-failure-proof-both-bd075a959cff44499fceaa30cbbd9a71.json`.
The latter is **failed**, despite the independent PostgreSQL upgrade success.
Recovery and browser source investigations continue; T089-T100 remain open.

The subsequent affected warnings-as-errors build and strict
`interrupted-recovery` guide selections now pass independently: SQLite 1 case,
zero skipped, 52.944s; PostgreSQL 1 case, zero skipped, 77.996s, retaining all
13 ordered observations
(`identity-recovery-stale-copy-both-573c4f0b6dfd410facf3e718ee95ba88.json`).
The restored browser must first demonstrate refusal of its stale binding,
then discard those browser cookies before a genuinely fresh login. The recipe
also creates the verified quarantined copy before advancing the original,
then reconciles it afterward; it does not weaken PostgreSQL's source-freshness
guard. These corrections establish both actual restored-copy recipes, not
the still-pending complete privacy/browser/public-catalog or final gates.

The newer browser probes identify distinct real capture failures without
relaxing the child deadline. The pinned Playwright `Response.finished()` can
remain pending after `requestfailed`; reading through the existing bounded
body path removes that hang. The client also now consumes the actual 401
response before navigation, with three focused regressions and a rebuilt
Next application. The initial capture then completes at 14.055s, but the
next real `POST /identity` fetch aborts at 14.079s: Next's external Server
Action redirect hard-navigates without consuming its body. Strict capture
still refuses that unread response. Actual bounded upstream-body capture
through the owned ingress is being implemented; there is no abort exemption
or successful PostgreSQL/browser/transport claim.
Relevant safe reports are
`identity-browser-finished-fix-sqlite-886d7aaa141447219df99bf763987f10.json`
and `identity-browser-completed-body-sqlite-b5894d7c47e34a43b539989d6cdffe4c.json`.
The affected builds, eleven native diagnostic cases and seven Node cases
pass; these are not a successful actual browser journey.

The subsequent actual bounded ingress capture now passes the complete SQLite
callback/scanner journey
(`identity-action-ingress-proof-sqlite-e87d79c0c38e48d5912a1275a99a51d2.json`).
PostgreSQL finishes capture at 17.288s but fails Chromium shutdown within the
unchanged 45-second child limit
(`identity-action-ingress-proof-postgres-2362af24aed442e5d92b2e5daa64db80c.json`).
An unsuccessful pages-first shutdown experiment is reverted, not claimed as
a fix. A guarded owned-process teardown now has four Node regressions; all
21 capture/canary/action/ownership cases pass together with zero skipped.
The verifier requires all four helper/unit sources and executes them in its
existing named unit suite. The 73 focused verifier/failure-propagation tests
also pass after that wiring. Native shutdown and complete public-guide proof
remain in progress; source-only checks cannot close them.

The subsequent owned-stop source check passes **22 Node cases**, including
five ownership regressions, plus affected e2e lint/typecheck/Prettier and
whitespace checks
(`identity-browser-owned-stop-source-final-none-a5440060ca5b437b99fd0855db780a60.json`).
The first actual SQLite owned-stop probe still fails its ownership guard;
disposal also masked the primary failure as missing transport capture
(`identity-browser-owned-stop-native-sqlite-sqlite-9947fba9ef8c472e89bd5adebea6a99f.json`).
An isolated measured Chromium probe confirms that its command/title can change
after termination while cleanup completes
(`identity-browser-owned-chromium-guard-measured-none-f8db8f183e23473cbb20de3b6af56c4c.json`).
The follow-up keeps changed-command processes unsignalled, verifies their exit
passively, preserves primary and disposal failures, and retains private roots
when cleanup is unverified. Fresh native callback and complete guide evidence
for this latest source are still pending; neither process exit alone nor the
source-only pass establishes a successful close protocol.

The subsequent serial PostgreSQL baseline measurement also remains **failed**:
`identity-postgres-baseline-profile-postgres-20e209162ed14b188092a146b1f4ab07.json`,
1108.196s total. It uses the complete existing host, non-composition storage
and composition integration selections with `--no-build`, preserving all
selectors/minima and each 900-second command limit; it is not the nested
900-second baseline or a passing complete gate.
The host phase fails in 580.134s at
`PostgresEnrollmentConcurrencyTests.TwentyEligibleHttpContendersCommitExactlyOneNomineeGenerationAndImmutableEvent`
and
`PostgresIdentityFailureTests.ActualProductLoginCommitRefusalNeverAcknowledgesSuccessAndExplicitRetryKeepsOneMapping`.
Its closed evidence is
`identity_fixture_child_readiness_transport_ConnectionError_owner_opened`
and `expected_303_actual_503`.
Non-composition storage fails in 390.871s at three
`PostgresIdentityMappingTests` cases (native session/process/profile preservation,
Unicode text limits and sequential trust changes) and
`PostgresEnrollmentRestartTests.OriginalModuleDataAndConsumedDesignationSurviveRestartsPreparationAndUnavailableAdministrator`;
the report retains `DeadlineExceeded` and exact source locations.
Composition fails in 137.187s at
`PostgresCompositionMigrationTests.AdditiveReleasePreservesEveryAssignmentAndOriginalOwnerSentinel`
and
`PostgresCompositionRecipeTests.TypedBusinessOperationsRefuseAnotherOwnersAcceptedBindingWithoutWriting`.
No native selection passed, so the success-only per-class timing hook produced
no class profile. These are current assertions requiring diagnosis, not an
assumed CPU/cold-start cause or a waiver based on the earlier focused passes.

Exact reproduction then identified one deterministic dependent-assertion bug:
`EnrollmentRestartTests` still expected the old generic operator failure,
although the shared codebook now preserves the precise consumed-nomination
refusal. It now requires
`identity_fixture_operator_bootstrap_authorize_bootstrap_authorize_identity_enrollment_unavailable`;
the authority/state/module assertions are unchanged. Formatting, the affected
warnings-as-errors build and that original restart test pass on **both
providers**, zero skipped
(`identity-closed-nomination-refusal-both-45e641baef78404c8ee35bc86a27b3f0.json`).
The isolated host failures also pass all three selected cases, and a complete
composition rerun passes 59 cases with safe per-class timing
(`identity-pg-exact-host-repro-postgres-6c9f5914c1b54024881e6e79e2e3be44.json`,
`identity-pg-composition-failure-profile-postgres-7fcd436bec0f4dd9ae6d42f2eb0791b9.json`).
Those reruns do not establish the causes of the other aggregate failures or
replace a passing complete gate. The actual pinned image is `linux/arm64`,
not x64 emulation; the local engine reports 18 CPUs and 16,816,451,584 memory
bytes. Neither observation establishes a performance root cause.

A further complete PostgreSQL host selection runs 119 actual cases in
415.952s but still fails three:
`PostgresIdentityKeyTests.ActualDatabaseRecoverySetDoesNotContainSeparatelyRetainedKeyCustody`
at its enrollment `expected_200_actual_503`,
`PostgresEnrollmentTests.EnrollmentChecksCurrentCeremonyAndExactNomineeWithoutReopeningHistory`
and
`PostgresIdentityFailureTests.LostStoreAuthorityOrCorruptTicketCannotServeAWarmedIdentityAndNeedsExplicitCorrection`.
The latter two retain `identity_fixture_browser_context_failed`.
The failure-safe profile records class times and closed exception types
without raw XML/messages
(`identity-pg-complete-host-profile-postgres-bd406c1c110f4e97adc457c597fed26a.json`).
Its 29 OIDC-budget cases sum to 352.720s; 17 enrollment cases to 259.891s;
10 actual-failure cases to 216.118s; six key cases to 202.963s.
These class sums overlap and are not additive wall-clock phases. Different
failing cases and a lower elapsed time do not establish an environmental
cause or a successful baseline.

The fixture-only administration change uses an independently owned,
one-connection `NpgsqlDataSource` per resource for system-database queries,
instead of repeating physical administration authentication for each setup
statement. It closes before cleanup/refusal checks; target-database, runtime
and provisioner connections remain unpooled. There is no global pool or
changed query, role, durability setting, selector, deadline or minimum.
The existing real two-scope cleanup test now also requires both administration
backends before disposal, then the first absent and the second still present.
The affected format, complete warnings-as-errors solution build, real
`PostgresScopeTests` case and all four existing `PostgresOwnershipTests` pass
(`identity-owned-administration-postgres-f46a07af0416424f960da52e552e782b.json`).
That includes actual backend disposal/isolation, ownership refusals and the
assembly's zero-borrower/no-leftover-backend check, not just connection counts.
This is not a diagnosed cause or a passing aggregate performance result.
Current required repository/whitespace checks and all 141 Python cases pass,
7.724s. The subsequent complete host measurement with the same provider/source
selections and four concurrent fixture collections **passes all 119 cases**,
zero skipped, 339.057s
(`identity-pg-bounded-host-profile-postgres-514d1411de304b74869844d9bd90ae1c.json`).
The application deadlines and twenty-way contenders inside their test are
unchanged. The verifier now caps independent provider fixture collections at
four, never above the reported logical CPU count; unknown CPU count uses one.
The unavailable-PostgreSQL propagation exercise reuses this exact command
builder, preserving the browser-suite exclusion and scheduling options instead
of a stale hand-maintained expected command. Repository/whitespace checks and
all **142 Python tests** then pass, 6.085s, including CPU-bound/selector/minimum
regression coverage. This passing host profile does not replace the required
complete serial/nested baseline or diagnose every historical failure.

Source review then identifies a second tightly coupled propagation mismatch:
the integration suite deliberately executes all three serial phases after a
native failure, while the unavailable-PostgreSQL exercise previously required
exactly one failed phase. The suite and exercise now share the unchanged
three-command builder. Only that injection explicitly requires all three
ordered failures, each with its exact command, nonzero exit, `command_failed`
and valid correlation. Other injections still require exactly their single
expected failure; missing/extra/reordered phases, substituted commands,
missing tools and timeouts are rejected. All **143 Python tests** and required
repository/whitespace checks pass, 9.273s. Actual complete failure-propagation
execution remains required; this source regression is not its result.

## Continued implementation: actual failures, legacy binary and capture inventories

The following are focused executions on the dirty feature worktree based on
`203e0437e90011bf157d6927ddef639f257346d6`, not a newly assessed published head
or a passing complete gate. The implement/running ledger is unchanged.
T080-T082 are now closed against the delivered recovery source and actual
both-provider recovery CI on `203e0437e90011bf157d6927ddef639f257346d6`:
[recovery job 122936525187](https://github.com/Stonefly-Labs/Kofte/actions/runs/40722399761/job/122936525187),
whose success was independently re-observed. The enclosing run failed; this
is evidence for the specific recovery tasks, not a complete-gate pass.
Current full PostgreSQL storage and actual accepted-old-binary results below
add independent current-worktree proof. The subsequent complete host recovery
selection below closes T083-T085; actual failure/capacity evidence then closes
T086/T088. T089-T100 remain open.

| Exact executed command / selection | Result |
| --- | --- |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror` | Passed after joining operations, actual failure, strict capture and legacy-host sources. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-build --no-restore -c Release --filter-class '*SqliteIdentityFailureTests' --minimum-expected-tests 10` | Passed 10, zero skipped; 69.983s. |
| Same command with `'*PostgresIdentityFailureTests'` and `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true` | Passed 10, zero skipped; 124.820s, actual pinned owned image. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*IdentityConsumerHarnessTests' --minimum-expected-tests 40` | Passed 40, zero skipped; 4.129s. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore -c Release --filter-method '*ActualAcceptedPreIdentityBinaryRefusesFormatTwoServingAndUpgradeWithoutChangingAnyState' --minimum-expected-tests 2` with disposable PostgreSQL admission | Passed both providers, zero skipped; 10.904s. |
| Same storage project with `--filter-class '*PostgresScopeTests' --minimum-expected-tests 1` | Passed actual independent databases/roles, borrower cleanup and owned-image archive; 5.720s. |
| `python3 -m unittest scripts.tests.test_verify scripts.tests.test_failure_propagation -v` | Passed 72 tests including named suite routing, nested positive-evidence bounds, safe timeout diagnostics and exact ordered scenario inventory. |
| `npm run dependencies:check` and `npm run contract:check` | Both passed; no new external package/pin/notice or generated-contract drift. |
| `python3 scripts/check_repository.py && python3 -m unittest discover -s scripts/tests -v && git diff --check` | Passed; 140 Python tests, 5.131s. Later source/task edits still require final checks. |

Safe reports:
`identity-failure-boundaries-both-7fefc448564d4af9a65dede1235c50c7.json`,
`identity-legacy-and-capture-both-0919353da74c4e9d8002ba73ba04c732.json`
(build/40 negatives pass; initial legacy selection fails),
`identity-legacy-refusal-both-5ecfc41d40b84461988e6e418ed5ee10.json`,
and `identity-final-dependencies-none-ae6f2690da1b41cfa5edcf6c90a5b411.json`.
The initial legacy failure was an actual fixture admission bug: PostgreSQL
serving received the maintenance-only provisioning environment variable, which
the immutable older parser rightly rejects. Serving now receives runtime
configuration only; maintenance alone receives provisioning authority.
The corrected test builds the actual accepted `3e416813233c9d291eb20cf72d5882dbddf3e1b6`
source in an owned private root with locked dependencies, executes both serving
and registry upgrade against real format-2 state, requires `storage_invalid`
and compares exact security snapshot, allocation/history/revision and populated
module values. It does not substitute copied compatibility logic.

The new failure cases lock the actual SQLite database or pause a dedicated
owned PostgreSQL server for login, current identity, sign-out and enrollment.
Each returns private bounded 503, makes no provider request or durable change,
and recovers only after explicit correction. Real before-/after-login-commit
refusals never acknowledge a new cookie; fresh explicit login retains one
user/mapping and the exact durable event history.

Verification now requires identity source/scanner/project inventories, zero
native skips/failures and source-known class presence before deleting private
XML. Named contract gates require all ten standalone/aggregate guide rows per
provider; named browser/privacy gates include actual native identity journeys.
Actual Next stdout/stderr, bounded useful ingress-access records and .NET
diagnostics are scanned against privately delivered browser canaries and then
discarded; this capture implementation still awaits successful actual browser
execution. Development has no ingress component: its new path requires the two
actual Next/.NET channels; production still requires all three. This latest
Development extension and its extra strict-scanner theory row now compile and
pass the focused negatives; the actual Development browser remains unproved.
Successful capture deletes its private canary file and discards
buffers/XML; unverified browser ownership still retains its private profile
rather than risking deletion of a live process's state.

The full non-composition PostgreSQL storage rerun **still failed**
at 900.061s:
`integration-postgres-e29610fa36744a198a093b5134ee50c3.json`.
It does not establish a new assertion or explain earlier hosted failures.
Server reuse alone is not claimed to fix the baseline. Ownership discovery
now uses bounded, project-filtered Docker API reads on the already admitted
local Unix socket, retaining exact project/run labels and resource-ID checks;
CLI lifecycle/tools are pinned to that same endpoint. Its scope/archive proof
passes. The subsequent full non-composition storage rerun now **passes 212
tests, zero skipped and eleven required identity classes in 832.601s**:
`identity-engine-storage-postgres-9eb5a21a3fcc4305865e2bb0299e03b3.json`.
Its exact command is the earlier full storage selection plus
`--long-running 15 --output Detailed --show-test-results all`; those diagnostic
options do not change selection or deadlines. This is not the complete
host/storage/composition baseline or a replacement for the historical timeout.
A remaining repeated dedicated-server factory was found in the ordinary
registry fixture; its change to independent databases on the assembly-owned
server now passes the complete 19-case PostgreSQL registry namespace in
135.839s. Whole-server outage/native/external fixtures stay dedicated.

### T083-T088: actual keys, preparation, recovery, failures and capacity

`identity-key-preparation-registry-both-60ede811e8eb4f739fca282f4b560458.json`
passes in 357.547s, with all private native results checked before disposal:

| Exact executed selection | Passed / skipped | Seconds |
| --- | --- | --- |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-build --no-restore -c Release --filter-class '*IdentityConsumerHarnessTests' --minimum-expected-tests 41` | 41 / 0 | 4.213 |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-build --no-restore -c Release --filter-namespace Kofte.Storage.IntegrationTests.Registry --filter-trait Provider=postgres --minimum-expected-tests 10` | 19 / 0 | 135.839 |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-query '/[(Suite=recovery)&(Provider=sqlite)]' --minimum-expected-tests 1 --no-build` | 50 / 0; both required key/preparation classes | 77.082 |
| Same host recovery command with `Provider=postgres` and disposable owned-image admission | 42 / 0; both required key/preparation classes | 140.410 |

T083 now has actual both-provider key loss/foreign/undecryptable refusal,
retained-key/certificate overlap, client-secret correction and consumed actor
preservation. T084's delivered tests are
`tests/integration/Kofte.Host.IntegrationTests/Identity/IdentityPreparationTests.cs`,
not the planned storage-project filename. They exercise actual worker
termination at commit and key owner/material/inventory/ready boundaries,
including empty/half-flushed owned inventory; only explicit owned retries
complete. Existing stopped authority/lease/deadline/worker/cleanup safeguards
remain in the executed recovery selection and accepted foundation coverage.
No budget or authority release rule changes.

T085 combines actual both-provider complete-set/quarantine/decision recovery
CI on `203e043`, current host recovery above, full PostgreSQL storage and the
actual accepted-old-binary refusal regression. T086's ten real-store failure
cases per provider are recorded above; no new cause is assigned to historical
hosted 503 failures. T088 uses the actual 57-case SQLite and 57-case pinned-image
PostgreSQL capacity/admission/event runs
(`t088-capacity-sqlite-a5862721bf504cacacca1df60e8ca8a9.json`,
`t088-capacity-postgres-75b3253dfc0f43b09bd247d25fa302be.json`) plus the current
212-case PostgreSQL storage pass. These cover durable limits/reopen, warnings,
retention/maintenance, checked overflow and mutation rollback, not only constants.

The joined solution build and parent formatting pass in
`identity-post-browser-window-both-a7ae6c09430349be947ed8dee7f9e7ef.json`.
Two new closed browser-failure diagnostic regressions and their solution build
also pass in `identity-public-operations-both-6afa97ea816f4881bcc117d7dc6080bd.json`.
That report **fails** the first actual public `upgrade-preservation` selection
on both providers (`identity_consumer_failed`, 74.141s). The other two recipes
were not run by that fail-fast command. T091/T094 remain open; compilable
recipes are not executed proof. Actual Chromium profile trust now has a
passing isolated proof, but the latest actual callback journey still fails;
T089/T090 and the complete local/assessed-head CI gates remain unfinished.

The subsequent coherent build and 43-case strict harness pass in
`identity-browser-and-operations-both-8d245071b5ed45fcbcf77ca08a34b50a.json`.
Its actual callbacks fail on both providers at the accessible loading/signed-out
boundary with `identity_browser_privacy_capture_failed` (39.698s SQLite,
77.139s PostgreSQL). This is not a trusted-TLS refusal: the closed profile phase
records an observed browser owner. The public SQLite upgrade and both-provider
`private-evidence` now pass; PostgreSQL upgrade and both `interrupted-recovery`
cases still fail. The new operator/consumer codebook mismatch was confirmed to
hide useful stopped-child errors; diagnostic correction remains in progress.
No remaining operational recipe is claimed complete from compilation alone.

Current frontend checks: all-workspace TypeScript and 154 web unit tests pass.
The first joined lint/format run failed on the new privacy cleanup code;
after correction, e2e ESLint, full Prettier verification and all four actual
Node reader regression tests pass. These reader tests are now required in the
named unit suite. The strict C# failure projection gains seven additional
primary/capture coexistence cases, still awaiting its next native rebuild.
Two focused Python source-projection/unit-routing regressions pass; the full
updated Python/repository/whitespace checks remain required before publication.

## T001: prerequisites and integration boundaries

T001 is complete: reviewed the approved specification, plan, research, complete
task list, model, both versioned contracts, future guide, passing analysis,
canonical inputs and constitution 2.0.1. ADR 0007's later explicit approval
**"ok, I approve it"** governs its historical Proposed planning statements.
This is design approval, not approval of unchecked packages or feature 005.

The worktree began clean on its recorded branch, local/remote feature heads
matched, and PR #8 was open/draft. Updated `origin/main` was `3e41681`, the
human merge of accepted 004. This invocation continues 005; it does not create
a new branch or re-run a completed stage. The driver began implement exactly
once. `.specify/extensions.yml` is absent: no before/after implementation hook
is configured.

| Read-only checklist gate | Total | Checked | Unchecked | Result |
| --- | --- | --- | --- | --- |
| `checklists/requirements.md` | 16 | 16 | 0 | Pass |
| `checklists/quality.md` | 36 | 36 | 0 | Pass |

Neither checklist nor any approved design artifact was changed. The existing
ignore files cover local secrets, dependencies, .NET/Next outputs and browser
artifacts; no new package publishing, Docker build or chart is introduced.

Accepted 001-004 remain the integration boundary: retain anonymous aggregate
health, strict unknown routes, identical empty/composed enforcement, stopped
maintenance authority, exact platform inventories, immutable module assignments,
provider isolation and migration-1 bytes. Identity must add verified format 2
and recovery-v2, not relax old validation. First-admin designation is not a
permission grant. No auth route, migration, fixture bypass or package is
partially wired while its prerequisites are blocked.

### Actual available profile

The initial default `dotnet --version` failed: global.json requests 10.0.402,
but the default installation exposes only 10.0.203. The existing isolated
`artifacts/toolchain/dotnet-10.0.402` installation resolves this tooling issue:
with its directory on PATH and DOTNET_ROOT set consistently, `dotnet --version`
returns **10.0.402**, and `dotnet --list-runtimes` lists both
Microsoft.AspNetCore.App and Microsoft.NETCore.App **10.0.13**.
No SDK download, global installation, version change or roll-forward was used.

Observed profile: macOS 26.5 arm64, Python 3.15.0, Node 24.22.0,
npm 11.19.1, Docker server 29.8.2; the existing npm restore is present.
No host, issuer, browser, PostgreSQL fixture or other runtime helper was started.
Microsoft Learn/Context7 MCP are unavailable; no uncertain library API was
implemented and no MCP result is claimed.

### Preserved failure baseline

The earlier 005 specification-head `composition-integration-postgres` /
`private-diagnostics` failure (`composition_cleanup_incomplete` /
`composition_consumer_failed`) remains undiagnosed. Its
[failed run](https://github.com/Stonefly-Labs/Kofte/actions/runs/40325294237)
is not repaired by this checkpoint. Feature 004's failed/interrupted local
complete gate and human-deferred PostgreSQL observation remain limitations,
not a passing new complete gate or release waiver.

At inspection, baseline analysis-head
[repository checks](https://github.com/Stonefly-Labs/Kofte/actions/runs/40404584695)
passed. In its separate
[product run](https://github.com/Stonefly-Labs/Kofte/actions/runs/40404584613),
quality, unit, browser and startup passed; six other contexts were still
in progress. This is an observation of existing code, not new identity proof
or a claim that the complete matrix passed. GitHub reports no required checks.

## T002: partial candidate review; adoption refused

Official NuGet nuspecs and actual `.nupkg` archives were fetched without adding
a package reference, restoring a candidate or changing the reviewed inventory.
The net10.0 candidate closure adds nine identities:

| Candidate | Version | Declared license / target | Advertised source |
| --- | --- | --- | --- |
| Microsoft.AspNetCore.Authentication.OpenIdConnect | 10.0.13 | MIT / net10.0 | dotnet/dotnet `4099f83977f4d29604a9aeb971fb0a7bc1d5f699` |
| Microsoft.IdentityModel.Protocols.OpenIdConnect | 8.19.2 | MIT / net10.0 | AzureAD source revision below |
| Microsoft.IdentityModel.Protocols | 8.19.2 | MIT / net10.0 | Same |
| System.IdentityModel.Tokens.Jwt | 8.19.2 | MIT / net10.0 | Same |
| Microsoft.IdentityModel.Tokens | 8.19.2 | MIT / net10.0 | Same |
| Microsoft.IdentityModel.JsonWebTokens | 8.19.2 | MIT / net10.0 | Same |
| Microsoft.IdentityModel.Logging | 8.19.2 | MIT / net10.0 | Same |
| Microsoft.IdentityModel.Abstractions | 8.19.2 | MIT / net10.0 | Same |
| Microsoft.Bcl.Cryptography | 10.0.2 | MIT / net10.0 | dotnet/dotnet `44525024595742ebe09023abe709df51de65009b` |

IdentityModel's Logging.Abstractions/DI minimums are 8.0.0; the existing central
10.0.13 pins would apply. This metadata walk is **not a restored actual graph**
or permission to adopt additional minimum versions. The reviewed existing union
remains 403 npm / 78 NuGet identities.

Every candidate archive contains `.signature.p7s`, and its libraries include
net10.0 assets. Signature **presence is not cryptographic verification**:
no new `dotnet nuget verify --all`, Linux signature result, locked restore,
build or compatibility result is claimed.
The original OIDC `THIRD-PARTY-NOTICES.TXT` SHA-256 is
`307d014f65d8482314f1400ddeae7a0cbabb96c2207bcc77f6233cc10588e5d9`;
Bcl.Cryptography's notice SHA-256 is the already observed
`6d15e10a101c6bfff2ab4429ed061bf76c456fc4b23ad6b03e0d0f8377148a21`.
The seven IdentityModel archives contain no named license/notice file;
their nuspec MIT declaration alone does not complete original-source review.
No package notice or provenance record was added to the adopted inventory.

Both current official NuGet vulnerability-feed pages were inspected:
base `2026-10-19T23:33:13.4608684Z` and update
`2026-10-26T11:34:12.9215033Z`. Their IdentityModel/JWT advisory ranges affect
older versions, not 8.19.2; no OIDC/Bcl.Cryptography entry was present.
This bounded feed observation is not a complete adoption/audit pass.

### Blocking evidence and correction

All seven IdentityModel nuspecs advertise repository
`https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet`
at revision `25d90ed3f48854036d444541a049089ccd198707`.
Independent attempts could not verify that exact source:

- Official GitHub tree at that revision: **HTTP 404**.
- Raw GitHub `LICENSE.txt` and alternate `LICENSE` at that revision:
  **HTTP 404**.
- `gh api` commit lookup: **HTTP 403**, organization SAML enforcement.
  An alternate official tag-content lookup was also denied by SAML.

These results establish **unresolved source provenance/access**, not a claim
that the package is malicious, that its advertised commit does not exist, or
that every source/license route has been disproven. No credentials, SSO
authorization URL or private source is included in this report.

T002 requires original provenance/license review before adoption. It remains
unchecked and blocks every later task through its explicit prerequisites.
Do not replace the version, weaken checks, claim archive signature presence
as verification or continue with a hand-written protocol implementation.
Restore authorized access to the advertised upstream revision, or obtain
independently verifiable official exact-revision source/license evidence.
Then resume this same active implement stage with the driver, finish T002's
archive/signature/advisory/actual-graph/lock/inventory gates and proceed in task
order. A different candidate requires an explicitly reviewed plan-compatible
adoption decision, not a silent fallback.

## Acceptance and constitutional evidence matrix

No user story or behavioral requirement is implemented by this checkpoint.
The complete independent acceptance obligations remain in the unchanged
[guide matrix](quickstart.md#acceptance-matrix) and task traces.

| Intent | Required implementation/evidence | Current result |
| --- | --- | --- |
| US1.1-5; FR-001-004/008/015/019/021; SC-001/003 | T004/T023-T040: independent HTTPS OIDC, exact mapping, direct/adapter/UI parity, trust/budget rejection and fresh restarts. | Not built or executed; blocked by T002. |
| US2.1-5; FR-010-012/017; SC-002/006 | T041-T055: delivered nomination, twenty contenders, atomic commit/uncertainty and three restarts on both providers. | Not built or executed; no designation or enrollment claim. |
| US3.1-5; FR-005-009/018/021; SC-001/003 | T018-T021/T056-T070: exact actor/default denial, CSRF, session/revocation, safe health and adapter/contract evidence. | Not built or executed; existing host behavior unchanged. |
| US4.1-3; FR-013/014; SC-004 | T071-T078: explicit loopback development, visible mode and persisted production isolation. | Not built or executed; no fallback added. |
| US5.1-4; FR-015-020; SC-005-007 | T011-T022/T079-T092: both-provider format/recovery/key/capacity/privacy and actual accessible failure/retry evidence. | Not built or executed; no storage or key mutation. |
| FR-022; all outcomes; principles I-XII | T093-T100: fail-closed suite inventories, all delivered public recipes, complete local/actual CI and final traceability. | Pending, not waived by this documentation checkpoint. |

Principles I/II/IV/V/VI/VII/VIII/IX/X/XI/XII preserve the approved feature scope,
ownership, default denial, honest transactions, independent evidence and later
MVP boundaries. Principle III/VI's dependency provenance gate is the actual
blocker. No product test, permission engine, MCP/job/realtime runtime, full
shell, deployment profile or constitutional exception is introduced.

## Initial blocked checkpoint verification and handoff

Executed for this documentation/ledger checkpoint:

| Command / check | Actual result |
| --- | --- |
| `SPECIFY_FEATURE_DIRECTORY=specs/005-authentication-bootstrap .specify/scripts/bash/check-prerequisites.sh --json --require-tasks --include-tasks` | Passed; selected the recorded absolute feature directory and required inputs. |
| `python3 scripts/check_repository.py` | Passed; assets, documentation links, MCP parity and blocked driver state valid. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed: 121 tests, zero failures, 2.552 seconds. |
| `git diff --check` | Passed. |
| `SPECIFY_FEATURE_DIRECTORY=specs/005-authentication-bootstrap python3 .github/skills/drive-speckit/scripts/state.py status` | Confirmed 005 / implement blocked on its original branch, with the exact T002 reason. |
| Read-only baseline preservation assertions | Passed: 100 original task descriptions, exactly T001 checked; ten design/review artifacts, approved ADR, constitution and dependency inputs unchanged. Historical ledger entries/features/constitution unchanged; only active implement state added. |
| `gh pr checks 8 --required --json name,state,link,workflow` | GitHub reports no required checks for this branch. |

Product build/protocol/provider/browser suites are not applicable to these
documentation/ledger-only changes and are **not run as new authentication
evidence**. The blocked dependency review does not supply any missing
behavioral or cryptographic verification. Publication retains the same draft
PR; its latest-head GitHub checks are observed separately in the PR body,
not substituted for identity implementation.

At that initial checkpoint, only T001 was complete; T002-T100 remained unchecked.
Historical ledger entries, analysis, approved inputs, ADR and both reviewer gates are preserved.
The driver records **implement blocked**, not complete; no converge or other
stage is run. Keep this original branch and draft PR #8. Resume implement only
after resolving T002. No feature acceptance, readiness or merge is requested.

## T002 follow-up: source provenance resolved

**Date / baseline:** 2026-10-26 / `88c68c6ebc19ee4214aaf2eba08c85def6cdcb5b`.
**Scope:** Investigate and resolve the reported provenance/license blocker,
not perform the remaining dependency adoption or the whole 005 implementation.

### Root cause and authoritative evidence

Anonymous access to the public repository succeeds. Its commit API returns
422, **"No commit found"**, for
`25d90ed3f48854036d444541a049089ccd198707`; the revision's license remains 404.
Authenticated lookup separately reports organization SAML enforcement (403).
The latter is not a prerequisite for this resolution and no credential,
scope, organizational authorization or SSO setting was changed.

The official NuGet symbol archives supply the missing repository context.
Every investigated net10.0 PDB's SourceLink maps `/_/*` to:

```text
https://raw.githubusercontent.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet-ghsa-97f9-wg23-8r98/25d90ed3f48854036d444541a049089ccd198707/*
```

That is a security-advisory fork, not the public repository in the nuspec.
Its exact revision/license also returns 404; this does not establish whether
the fork was deleted or is private. The public
[official advisory](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/security/advisories/GHSA-97f9-wg23-8r98)
identifies 8.19.2 as a fixed 8.x release. Its SAML vulnerability is separate
from GitHub's SAML access-control error; no SAML package/protocol is introduced.

The official [8.19.2 release](https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/releases/tag/8.19.2)
points to public commit `ee19952f21d5c170a778bdca901b67076a1cd8f8`.
The official 8.19.1 tag points to
`3dba1997da46a3fbc60ce6d7e94c15ca867f9571`.
Both commits' original `LICENSE.txt` files are accessible and byte-identical
Microsoft MIT licenses, SHA-256
`cba03f5387b05405e56b688376421acae396136af4b5116738dc6e5160f87ddd`.
No license was inferred merely from the package name or current default branch.

### Exact compiled-source correspondence

A one-off, package-free .NET 10 metadata reader outside the worktree inspected
the seven official `.nupkg`/`.snupkg` pairs without executing their assemblies.
For each selected net10.0 assembly:

1. Its CodeView GUID, timestamp and age match the portable PDB.
2. Its PE checksum entry matches SHA-256 of that PDB with its 20-byte ID
   zeroed, as required by the official
   [Portable PDB checksum specification](https://github.com/dotnet/runtime/blob/main/docs/design/specs/PE-COFF.md#portable-pdb-checksum).
   A raw-PDB hash is not this checksum; no failed raw comparison is counted.
3. Every PDB document's original SHA-256 matches source from the exact
   official public revisions above or its cryptographically bound embedded
   generated source. Public checkout bytes require LF-to-CRLF conversion,
   preserving BOMs and all other bytes/encoding; no whitespace, code or
   arbitrary-content normalization is permitted.

| Exact IdentityModel 8.19.2 candidate | Verified net10.0 documents | `.nupkg` SHA-256 | `.snupkg` SHA-256 |
| --- | --- | --- | --- |
| Microsoft.IdentityModel.Abstractions | 11/11 | `bcd18aeaee95f92d0c21a61383eb129111785d48ccd4af9c2ae4756bd69108bb` | `b95b71d389fa5704bdafd075089f0782d977e13129a5523b0166a92fef63c811` |
| Microsoft.IdentityModel.JsonWebTokens | 27/27 | `8fd6a2d0dae0cad0ec7d08888a3a7521be234465fd960444401a04e975bd48e7` | `da24aa73192e0f5bf1e57f7a25738292e6a338227a722f5dc1b09ffe2124e38e` |
| Microsoft.IdentityModel.Logging | 14/14 | `8daf0f01850dc257c8ae42fa10eb99ed28197d127020e4461bf1130c3d41b511` | `4ea980808c91ea3e8edd9cfda8cac9bcf605cdff95546f450e627a131541b998` |
| Microsoft.IdentityModel.Protocols | 25/25 | `b2800311442c3a9d47cadb75a8ba3e8ce39eabbbb2ec8eed50e5a88177905c56` | `d6cda9ae8ff5ecfec09466303d7189082cc3abadddbb39c4e0056fcd00004278` |
| Microsoft.IdentityModel.Protocols.OpenIdConnect | 27/27 | `b92acde71946f925e5c60e0d002154d854a50b7577858a2376153c95473af3ba` | `eb10b808765a5d39a078d6eac5ef5858deaa19ff6c3c440d2a127a634a1f4fe7` |
| Microsoft.IdentityModel.Tokens | 157/157 | `3ce8b74b370adfe881df5d38c17621de8a150f519b96dc1351ff8a323fff5185` | `ec460a4c98371c3831c148893667dc84fe10761a1b16a0303027c4e7102163d1` |
| System.IdentityModel.Tokens.Jwt | 14/14 | `904a9f1225a4a2d7c3503a619f432c32a84e73cdaa40b5e56e8e57620e7cfe2a` | `b2f91c13520d9f10e3993351e942505165d7425f5d6ab88fc3de88a29e60aedf` |

**All 275 document entries pass**, including repeated shared inputs:
259 correspond to public 8.19.2 source and fifteen to embedded generated
source. The remaining input,
`src/Microsoft.IdentityModel.Tokens/AppContextSwitches.cs`, matches the
immutable public 8.19.1 revision, not the 8.19.2 tag. Its expected original
CRLF SHA-256 is
`920074a86f0f8976c0bf642e0f377fcad4c23f8105d2c77ac69d0b40471583f9`.
Its public LF bytes have SHA-256
`57854ee6bcfeac85b34fa9e506b55f62c6b4fab5b7db012f0eaf80674a460465`.
This verifies that exact unchanged compile input; **no 8.19.1 package is
selected, installed or substituted**. Merely equating the 8.19.2 tag with
the advertised private-fork commit would have missed this difference.

This evidence resolves the original candidate's source/license assessment.
It does not prove an identical entire private repository, cover other target
frameworks, reproduce Microsoft's build or waive signature/runtime checks.
The one-off reader compiled with SDK 10.0.402 with zero warnings/errors;
its input/output and downloaded public evidence remain outside the worktree.
No downloaded source was built or copied into the product.

### Actual cryptographic signature verification

The already reviewed immutable Linux SDK image was not locally cached;
it was pulled by its exact approved digest. With only the public candidate
evidence mounted read-only, this inner command executed:

```bash
dotnet nuget verify --all /evidence/*.nupkg
```

Image:
`mcr.microsoft.com/dotnet/sdk@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa`,
platform `linux/amd64`, normal online revocation/trust checks.
**Passed: all nine archives, exit 0, zero warnings/errors.** Each reports
Microsoft Corporation author and NuGet.org repository signatures. This is
cryptographic verification, not the earlier observation of `.signature.p7s`.

Besides the seven rows above, the verified archives are:

- OIDC 10.0.13: SHA-256
  `317e2f65bff5b70cd586a548b521c482388c02d8d85e09426291cce4dd53e276`.
- Bcl.Cryptography 10.0.2: SHA-256
  `f8e62d716b1dd6a65c11769d61e038b7afa9c800e0d403d00b1a4a52d3f4d363`.

No host credentials/source were mounted, certificate installed, trust check
disabled or signature warning accepted. The owned verification container
exited and was removed automatically.

### Current handoff and verification

The driver's original **005 / implement** stage is resumed, not completed;
the recorded branch, feature directory, start time and history are retained.
No second stage/run, version decision, ADR amendment or checklist change
occurred. The pre-execution prerequisite command passes, reviewer gates
remain 16/16 and 36/36, and no extension hooks are configured.

**Only T001 is complete.** T002's actual restored graph, central pins/package
reference, locks/inventory/notice reconciliation, current advisory audit and
compatibility validation remain implementation work. T003-T100 must continue
to honor that prerequisite. No package, product/test code, migration, runtime
behavior, authentication proof or feature acceptance is introduced here.
Earlier failed checks and the initial blocked evidence above remain historical
failures, not converted into passes by this investigation.

| Follow-up check actually executed | Result |
| --- | --- |
| `python3 scripts/check_repository.py` | Passed: repository assets, documentation links, MCP parity and resumed driver state. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed: 121 tests, zero failures, 2.235 seconds. |
| `git diff --check` | Passed. |
| `SPECIFY_FEATURE_DIRECTORY=specs/005-authentication-bootstrap .specify/scripts/bash/check-prerequisites.sh --json --require-tasks --include-tasks` | Passed: recorded feature directory and required inputs. |
| `SPECIFY_FEATURE_DIRECTORY=specs/005-authentication-bootstrap python3 .github/skills/drive-speckit/scripts/state.py status` | Confirmed 005 / implement running, original branch/directory/start time, no blocker reason. |
| Evidence/baseline assertions | Passed: all sixteen candidate/symbol archive hashes match this report; all 275 bound source entries match; tasks, approved design/review artifacts, dependency inputs and historical ledger entries remain unchanged. |
| New product build/protocol/provider/browser tests | Not run: no product or adopted dependency changes; no authentication behavior verified. |

Publication and latest-head CI outcomes are recorded in the same draft PR #8.
No readiness change or merge is requested.

## T002: actual dependency adoption

The subsequent implement invocation adopts the reviewed OIDC 10.0.13
reference, seven IdentityModel 8.19.2 pins and Bcl.Cryptography 10.0.2.
Exactly nine identities enter the restored graph; the complete reviewed union
is now 403 npm / 87 NuGet. A separate byte comparison confirms every new
restored `.nupkg` is identical to the source-reviewed, signature-verified
candidate. Original archive notices remain intact and inventory-reconciled;
no source copy or change to the existing source-copy notices is introduced.

| Executed T002 command/check | Actual result |
| --- | --- |
| `dotnet restore Kofte.slnx --force-evaluate` | Passed; locks regenerated through NuGet. |
| `dotnet restore Kofte.slnx --locked-mode` | Passed for all thirteen projects. |
| `dotnet build Kofte.slnx -c Release --no-restore --nologo` | Passed; zero warnings/errors. |
| `dotnet list Kofte.slnx package --vulnerable --include-transitive --format json --no-restore` | Passed; thirteen projects, zero vulnerable direct/transitive packages and no reported audit problems. |
| `python3 scripts/check_dependencies.py --prepare-bundled-evidence` | Passed; complete 403 npm / 87 NuGet inventory, locks, provenance, archive integrity and notices. |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-build --no-restore --nologo` | Passed: 192 tests, zero failures/skips. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet` | Passed. |

All .NET commands selected the existing isolated SDK 10.0.402/runtime 10.0.13.
**T002 is complete.** This adds a package reference, not login, sessions,
enrollment or any other authentication behavior. Dependent work may now begin;
the active implement stage remains incomplete.

## T003-T006: executable independent test setup

The separately compiled identity consumer is in the solution and referenced
only by test projects. Five actual invalid/unimplemented invocation cases
exit nonzero with zero assertions and safe failure JSON; no public scenario
is falsely implemented. Actor probing is the dependent T009 work.

The independent HTTPS issuer has its own ephemeral private CA, RSA signing
key, discovery/JWKS/code/token endpoints, confidential-client checks, PKCE,
nonce-bound independently signed JWTs and consumed authorization codes.
Its CA is trusted only by the fixture-created client handler; an ordinary
client rejects it. Bad issuer/audience/signature/algorithm/time/nonce
variants and safe operation observations are test-owned, not product flags.

The process fixture reuses the accepted owned roots, provider scope and
process/descendant cleanup. Private one-use files admit test children.
A test-owned native .NET startup hook installs an observer on diagnostic
commit phases; atomic private reached/release records avoid touching public
routes or supervisor IPC. No test hook or control reader is referenced by
production. Actual host-store commit/interruption evidence remains dependent
foundation/story work, not a claim made by the fixture readiness probe.

The independent C#/TypeScript corpora freeze route, credential, CSRF, exact
return-path, signed-evidence, capacity, rate, size, lifetime and checked-counter
expectations. These are acceptance inputs, not executed product cases.

| Executed setup check | Actual result |
| --- | --- |
| Initial issuer tests before fixture implementation | Red: missing `OidcIssuer` compilation errors. |
| Initial private barrier tests | Red: non-atomic publication let readers race a still-open reached file; fixed with private write/flush/close/atomic rename. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityFixtureTests\|FullyQualifiedName~OidcIssuerTests' --nologo` | Passed: four tests, zero skips; actual HTTPS exchange, key signature/nonce, client/PKCE/replay refusal, private phase hold/release and verified real child exit before owned-root deletion. |
| `dotnet build Kofte.slnx -c Release --no-restore --nologo` | Passed; fourteen projects, zero warnings/errors. |
| `dotnet restore Kofte.slnx --locked-mode` | Passed; new test project locks reconciled, no new package identity. |
| `./node_modules/.bin/tsc --strict --noEmit --skipLibCheck --types node --target ES2022 --moduleResolution bundler --module esnext tests/contract/identity-corpus.ts` | Passed. |
| `./node_modules/.bin/prettier --check tests/contract/identity-corpus.ts` | Passed. |
| `./node_modules/.bin/eslint tests/contract/identity-corpus.ts` | Passed. |

An attempted `tsc -p tests/contract/tsconfig.json` did not run a type check
because that file does not exist. The succeeding command above uses the
documented package's existing compiler flags and the exact new file.

### Additional published-head CI baseline evidence

Run `40409807625`, integration-postgres job `121877224218`, failed in
`composition-integration-postgres`; host/storage PostgreSQL phases passed.
The preserved artifact identifies
`PostgresCompositionPreflightTests.AdmissibleUnregisteredServingOwnerRequiresPreparationWithoutAllocating`
with storage code `Busy`. Its assessed revision is GitHub's merge revision
`967bd1e3fa4c3493bb3d6ddb901153c11a498e6e`, not new identity implementation.
No retry or diagnosis converts this into a pass; the older private-diagnostics
failure remains separately preserved.

## T007-T010: configuration and public actor foundations

The strict configuration component now requires explicit `oidc` or
`development`, exact public origin/all-loopback internal topology, bounded
UTF-8 JSON with duplicate/unknown-field rejection, private regular canonical
no-symlink secret references and a separate key ring outside recovery data.
Development requires the explicit Development environment and one to eight
exact synthetic identities. OIDC requires exact HTTPS endpoints/network
allowlists, private secret files and a matching RSA PEM certificate/private
key. Neither parsing nor stopped revocation contacts discovery/the IdP.
Revocation validates only its needed static subset when serving secrets are
lost. The trust digest preserves exact issuer/client/network/public-origin
trust and excludes rotatable secret values/display text.

`KOFTE_AUTH_CONFIG_FILE` is the only new bootstrap environment key. Existing
maintenance forwarding, including composed workers, carries it deliberately.
This adds a component, **not startup/serving identity wiring** (T021).
No stopped identity command or HTTP authentication route is delivered yet.

Platform now exposes only the immutable actor-v1, its two-valued mode and
read-only nullable current-actor interface. The separately compiled probe
uses those public types; reflection/construction tests reject credential/
permission payload, mutable properties, an empty user ID or unknown mode.
It is a trusted in-process contract, never deserialized credential proof.

| Executed foundation-component check | Actual result |
| --- | --- |
| Initial configuration and actor tests before production code | Red: missing host identity component and public actor types. |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityConfigurationTests --nologo` | Passed: 35 introduced configuration cases, zero skips. |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --nologo` | Passed: all 227 host unit tests, zero failures/skips. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter FullyQualifiedName~ActorContractTests --nologo` | Passed: three actor cases, zero skips. |
| Four fixture integration cases rerun after shared private-file extraction | Passed: four tests, zero skips. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed: all 121 tests. |
| `git diff --check` | Passed. |

No product schema, operator command, login, enrollment, session policy or web
UI is claimed complete by these component results. The active implement stage
and original draft PR remain open; T011-T100 are still required.

### Publication quality correction and operation-subset regression

The first `python3 scripts/verify.py --suite quality` attempt failed at
`dotnet-format` on introduced test/fixture initializer whitespace. The SDK
formatter corrected those files; Prettier restored the affected contract
lock-file formatting. The SDK solution tool keeps the new consumer at the
solution root, matching existing projects.

The subsequent **full quality suite passed all twenty steps**, including
repository/Python/whitespace checks, locked restore, .NET format/build,
npm restore, Prettier, ESLint, TypeScript, contract generation, workspace
builds and dependency review. This does not claim integration, browser or
complete identity acceptance.

A further focused regression validates bootstrap authorization's offline
subset: it succeeds without the provider client secret, but refuses a
mismatching private key or missing protection certificate. The exact focused
configuration command above now passes **36 cases, zero skips**, and
`dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet`
plus `git diff --check` pass after that addition. It exercises only the
configuration component, not a delivered bootstrap authorization command.

The complete host unit command above was then rerun: **228 tests passed,
zero failures/skips**.

## T011-T013: exact versioned identity storage format

Provider-specific migration 2 creates the eight model tables, deterministic
external keys, matching-mode user/session/designation foreign keys, checked
counters, nullable-state combinations, bounded enums and indexes. A fixed
`initial_user_mode = 'oidc'` FK component prevents a development user from
becoming the production designation. Installation-retained evidence cannot
be updated/deleted; session-class pruning remains later owner-operation work.

The original installation/history version-1 CHECKs forbid a simple append.
SQLite therefore transactionally rebuilds those two metadata tables, restoring
the same final names and accepted triggers; PostgreSQL transactionally evolves
the version constraints and restores the immutable installation trigger.
All existing rows/IDs/timestamps survive. The original migration-1 sources
remain byte-identical:

| Provider | SHA-256 |
| --- | --- |
| SQLite | `5d33f7aaa2c2c1af379af08816fc67dce412f53f538709adb9c233ebc43a4809` |
| PostgreSQL | `c129c9ab66618acd157ac7e4114073228463fa3880dbe1da4f863ce05648de6e` |

`ApplyIdentityFormatAsync` requires the existing owned stopped scope and a
caller-owned transaction. It preflights exact v1/v2, applies migration/history,
verifies unchanged installation identity/module revision total, and leaves
commit to its caller. This allows T017 to include initial identity metadata/
disabled bootstrap in the same store transaction; no operator prepare command
or usable identity key ring is delivered here.

`RegistryPhysicalFormat` compares frozen OID/data-independent native-catalog
fingerprints for each provider/version, not a learned runtime baseline.
SQLite fingerprints actual table/index/trigger/view SQL; PostgreSQL includes
relation/column definitions, types/collations/nullability/defaults/generation,
PK/FK/UNIQUE/CHECK definitions/validation, indexes, internal/explicit trigger
configuration, routines, rules and policies. Actual table inventories and
ordered source digests are also checked separately. Foreign-key integrity and
SQLite integrity/user version are verified. No arbitrary-extra-table fallback
is introduced. Registration validation explicitly admits supported v1/v2 while
module allocator/markers remain version 1. Existing reconciliation/composition
inventory readers already share this runner, so no duplicate path is added.
Recovery-v1 manifest validation still requires actual format 1.

Preflight also refuses unowned objects in table-free v0 *before* allocating
registry state. An independently inserted view remains intact after refusal.

### Real-provider evidence

The new tests exercise actual owned SQLite/PostgreSQL. They preserve exact
installation/registration/allocation/marker/revision snapshots, execute the
existing module-owned EF migrations `202610210001_Initial` and
`202610210002_AddNote`, and verify their populated history plus exact retained
row ID/payload/note after upgrade. They check rollback/idempotence, v0/unknown
formats, foreign scope refusal and the exact eleven-table inventory.
Independent SQL tests exercise case-sensitive keys, cross-mode FK refusal,
Unicode display bounds/controls, invalid nullable states, checked overflow
and installation-event immutability. Individual physical mutations remove
CHECK/FK/UNIQUE/nullability/index guarantees or add a table/view/default/
generated column/trigger/routine, or alter collation/history; each refuses
without repair.

| Command/check actually executed | Result |
| --- | --- |
| Initial `--filter FullyQualifiedName~IdentityFormatTests` before runner implementation | Red: missing `ApplyIdentityFormatAsync` compilation errors. |
| Initial 20-case execution | 18 passed; two preservation assertions used reference equality for `StorageAssignment`; corrected to the existing complete value snapshot, not weakened to counts. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityFormatTests\|FullyQualifiedName~RegistryFormatTests' --nologo` | Passed 44 cases, zero failures/skips, after adding independent physical/native constraint coverage. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityFormatTests&FullyQualifiedName~UnknownVersion\|FullyQualifiedName~IdentityFormatTests&FullyQualifiedName~ForeignStoppedScope' --nologo` | Passed four further cases, zero failures/skips. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityFormatTests.FormatTwoPreserves\|FullyQualifiedName~RegistryConstraintTests\|FullyQualifiedName~StorageMaintenanceDiagnosticsTests\|FullyQualifiedName~CompositionPreflightTests' --nologo` | Passed 74 cases, zero failures/skips; includes the strengthened nonempty EF-history/data preservation and dependent both-provider registry/composition/diagnostics regressions. |
| `shasum -a 256 src/Kofte.Infrastructure/Storage/Registry/Migrations/Sqlite/001_registry.sql src/Kofte.Infrastructure/Storage/Registry/Migrations/Postgres/001_registry.sql` | Passed: the two immutable values above. |

PostgreSQL commands explicitly set
`KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`; test ownership/cleanup is unchanged.
Native catalog measurements used disposable owned databases; the temporary
measurement harness was removed after freezing the reviewed catalog values.
These results do not prove T022's complete foundation gate, working login,
recovery-v2 or a delivered operator preparation workflow.

## Historical blocker: deadline-safe store execution before T014

The approved local identity work budget is **one second**, within the unchanged
four-second callback envelope. The accepted shared `OperationBudget.CommandSeconds`
floors remaining seconds and refuses values below one. Since even a fresh
one-second budget immediately has less than one second remaining, it rejects
its first command; `StorageInstallation.OpenPlatformAsync` uses that helper.
Reusing it would make every identity-store operation fail before useful work.

Two actual disposable probes were executed with:

```text
dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityBudgetProbeTests --nologo
```

The unmodified helper immediately threw `OperationCanceledException`.
The alternative probe allowed a rounded one-second native command, after
250 ms of the same one-second budget, while a separately owned SQLite
connection held `BEGIN EXCLUSIVE`. Although cancellation fired at one second,
the native command returned after **1,317 ms**. Rounding up plus cancellation
therefore does not preserve the approved total budget.

The probes deliberately failed to report those measurements, not to represent
passing feature tests. Their temporary source was removed from the test project
and retained only as a session investigation artifact; no probe is skipped,
made falsely green or included in shipping tests. Both connections/transaction
and their owned root were disposed. Product timeout code is unchanged.

With Learn/Context7 MCP unavailable, official Microsoft documentation confirms
[synchronous SQLite async APIs](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/async)
and [automatic busy retries/integer command timeouts](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/database-errors).
Zero means unbounded, not fail-fast. No timeout increase, background orphan
operation, zero-timeout fallback, journal-mode change or speculative native
adapter is adopted.

**Implement is blocked, not complete.** Resolve and independently test a
deadline-safe execution/transaction mechanism under the original one-second
local/four-second callback limits, including real contention, rollback/commit
uncertainty and cleanup, then resume T014 on this branch/PR. This is engineering
work, not a credential, SSO or merge request. T014-T100 (87 tasks) remain
unchecked. No later stage, human acceptance or working authentication is claimed.

### Final blocked-checkpoint verification

After removing the disposable probes, `python3 scripts/verify.py --suite quality`
passed all twenty steps. The rebuilt shipping host unit assembly then passed
`dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-build --no-restore --nologo`:
**228 passed, zero failures/skips**, with neither investigation probe present.
The final native-routine guard avoids trying to render an unexpected aggregate
as a normal function; the fixed fingerprints for legitimate formats are
unchanged. Its explicit function/aggregate refusal check,
`dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~UnexpectedNative --nologo`,
passed two cases, zero skips. Final solution format verification passed.
`python3 scripts/check_repository.py`, the exact required
`python3 -m unittest discover -s scripts/tests -v` (**121 passed**) and
`git diff --check` also passed. These green shipping-code checks do not erase
the failed deadline experiments or complete the blocked identity-store work.

## Deadline correction and T014: metadata/admission transactions

The user explicitly requested fixing the engineering issue and continuing.
The original branch/open draft PR and checklist gates were reverified:
requirements **16/16**, quality **36/36**, both read-only. The same active
implement stage was resumed through the driver after the initial twelve
real-provider deadline regressions passed. No second run, stage completion,
converge or acceptance was recorded.

`DeadlineStore` uses Microsoft.Data.Sqlite's documented native-handle API
through the existing locked SQLitePCLRaw package. No third-party source,
package, journal-mode change, infinite command timeout or detached worker
is introduced. It reuses the existing canonical root/file/sidecar/header
guards, enables/verifies foreign keys and binds each parameter explicitly.
Statements are single and results capped at 128 rows. Native busy waiting
is disabled (native `sqlite3_busy_timeout(0)` means immediate refusal, unlike
ADO.NET `CommandTimeout=0`, which remains prohibited/infinite).
Deadline-clipped 25 ms delays retry only SQLite's documented known-not-committed
prepare/autocommit or busy-commit boundaries, never an arbitrary transaction
body or uncertain outcome. VM progress checks also interrupt long CPU-bound
SQL. No database operation is abandoned in a background task.

PostgreSQL uses cancellable Npgsql operations and the existing immediate
socket-close cancellation configuration. Failed unpooled sessions close
without an uncancellable transaction-dispose rollback round trip.
Fifty milliseconds is reserved **within** the remaining parent budget for
cleanup; the local one-second and enclosing four-second limits are unchanged.
Legacy migration timeout behavior is preserved. These are tested provider
execution bounds, not a hard-real-time guarantee for an uninterruptible
operating-system/kernel stall.

T014 now implements stopped, caller-transaction-owned metadata initialization
and normal-host-lease owner mutations. Exact installation/schema/mode/trust/
epoch values are validated; trust changes require explicit future operator
reconciliation, never implicit request conversion. Every successful mutation
advances the checked security revision once, without changing the module
revision total. Missing/foreign/lost authority and overflow fail explicitly.
SQLite uses immediate write transactions; PostgreSQL locks the metadata row
and verifies the borrowed host backend's actual database/shared advisory lock
from each independent operation connection.

Admission persists only fixed categories, bounded lower-hex digests and UTC
minute windows. All eight categories use the exact approved limits, including
ten login starts/browser. Paired counters and the revision share one commit;
refusal rolls back every earlier effect. Live capacity is 4,096 and expired
cleanup is one at-most-128-row batch, never eviction of live windows or a
store-failure fallback. Twelve real concurrent requests admit exactly ten,
refuse two and retain count/revision ten on both providers.

### Actual red/green and broader verification

Both new test files initially failed compilation because their production
executor/store/types did not exist. The first deadline execution passed 9/11:
two exclusive-lock-before-open cases expected cancellation but the preserved
file/header guard correctly returned `Busy`. The tests now separately prove
fail-closed opening and actual contention after opening; no timeout assertion
was weakened. The subsequent twelve deadline cases passed on both stores.
Metadata/admission then passed together with deadlines: **32/32**.

A broader 93-case attempt passed 92, but the positive one-second PostgreSQL
commit case timed out while unrelated provider fixtures ran concurrently.
Strict wall-clock measurements now use a nonparallel xUnit collection so
unrelated container creation/fsync is not part of the controlled profile;
the one-second assertions and actual concurrent-request exercises remain.
The introduced concurrent-admission test then reproduced a real
`NpgsqlOperationInProgressException` on the borrowed shared host session.
Verification was moved to the independent operation connection, retaining
backend/database/lock ownership checks; both concurrency cases then passed.

| Exact executed command | Result |
| --- | --- |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityDeadlineTests --nologo` | Passed 12 cases, zero failures/skips, before subsequent metadata/concurrency additions. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~ConcurrentRequestsShareTheRealHostLease --nologo` | Initially red on the shared PostgreSQL connection; passed both actual-provider cases after the correction. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~Identity\|FullyQualifiedName~RegistryFormatTests\|FullyQualifiedName~RegistryConstraintTests\|FullyQualifiedName~LeaseLoss' --nologo` | Passed **95 cases**, zero failures/skips, including all 41 deadline/admission cases and dependent physical format, constraints, original populated data/history and lease regressions. |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --nologo` | Passed **228 cases**, zero failures/skips. |

Commands use the existing isolated SDK 10.0.402/runtime 10.0.13; PostgreSQL
commands explicitly set `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`. All
owned provider scopes/connections and test tasks are awaited and cleaned up.
No failing measurement harness or skipped case is shipped.

### Prior-head CI limitation

The previous `d644a5f` push's product run **40420590690** is now observed
failed, not pending or green: integration-postgres, recovery, contract,
privacy and failure-propagation failed; quality, unit, integration-sqlite,
browser and startup passed, as did the separate repository check.
Its preserved integration artifact assesses merge revision
`8263e7d7a7b5b415026104359d997bcf58c3ae21`; host PostgreSQL passed, while
storage PostgreSQL reports `Corrupt` in
`PostgresRecoveryDiagnosticsTests.CompleteRealRecoverySetsAndRefusedBoundariesEmitPrivateCorrelatedRecords`
and
`PostgresRecoveryExercise.CompleteCustomDumpRestoresBothSchemasToFreshOwnedTargetWithoutElevatingRuntime`.
This is actual prior-checkpoint evidence, not a diagnosis or a claim that
the current focused gate repairs full recovery/CI. Other failure causes
remain unestablished. Earlier failures and accepted-feature limitations are
preserved above.

**T001-T014 are complete; T015-T100 (86 tasks) remain unchecked.** Transactional
identity evidence is next, then dependent preparation/keys/policy/serving.
No operator identity command, default-deny identity wiring, login/session,
enrollment, web journey, recovery-v2 or final acceptance is delivered by this
checkpoint.

### Final deadline/T014 checkpoint checks

After the final source, authority/concurrency tests and progress documentation
updates, `python3 scripts/verify.py --suite quality` passed **all 20 steps**.
The exact required `python3 scripts/check_repository.py`,
`python3 -m unittest discover -s scripts/tests -v` (**121 passed**) and
`git diff --check` also passed. The quality gate includes full solution
format/build, locked restore, frontend lint/types/build and dependency review.
No new dependency, public contract, approved limit or feature acceptance
changed. This remains a partial implement checkpoint on the original draft PR.

## T015-T017: transactional evidence, persistent keys and stopped commands

The explicit `/drive-speckit` invocation resumes the original running
`005 / implement` stage on its recorded branch and open draft PR #8.
Checklist gates remain requirements **16/16** and quality **36/36**, read-only.
The interrupted identity event/key/test files were verified as unfinished work
from this same implementation, not adopted from an unrelated task. No stage
completion, new run, convergence or acceptance is recorded.

T015 records fixed allowlisted security events in the owner transaction with
random IDs, checked revision, generated correlation, UTC, optional internal user
and fixed committed outcome/retention. Installation events cannot be updated or
deleted. Session retention is thirty days with one at-most-128-row prune per
mutation. Actual 1,000,000/200,000 capacities warn at 90% and refuse the entire
mutation when full; neither a missing FK nor a later error leaves a committed
event or owner revision behind. The existing native deadline executor is reused.

T016 uses maintained Data Protection with installation/mode/purpose binding,
private owned owner/key artifacts and mandatory actual certificate encryption
for OIDC. Strict bounded JSON/XML, hashes, no-link/owner/mode validation,
old-key retention and read-only validation prevent silently adopting, repairing
or regenerating a ready ring. Warmed providers revalidate disk state before
protect/unprotect. Framework logging is isolated because it can disclose paths,
XML and crypto details. Tests use actual protection/unprotection and framework
rotation, not a replacement cipher or mock principal. Certificate-overlap
operations remain later T083 work.

T017 adds only strict admitted stopped `prepare`, `reconcile-configuration` and
`revoke-sessions` commands. They reuse supervised owned-worker admission and the
exclusive installation scope, with clipped work/acquisition/cleanup inside the
existing 120-second envelope. Full installation preflight precedes changes.
Table-free v0 requires separate registry upgrade. Format-1 preparation commits
format 2, metadata, disabled bootstrap, event and security revision together.
Borrowed `DeadlineStore` wrappers never commit/rollback/dispose the caller's
connection/transaction. Key preparation is a separate filesystem effect,
deliberately completed before that database commit; no distributed atomicity is
claimed. Ready files left by a failed database transaction can be verified on
explicit retry.

A current database can complete its explicitly owned `preparing` ring but cannot
create missing ready keys or adopt an unowned/foreign directory. That boundary
was initially red on both providers and is now covered by permanent command
regressions. Reconciliation explicitly acknowledges changed trust/keys and
advances the epoch, revoking active sessions and pending attempts/authority.
Revocation requires already acknowledged trust. Both preserve user mappings,
consumed first-admin designation, installation ID and registry revision totals.
Direct workers and all stopped commands while a real host lease is held refuse.
The supervisor projects only a validated bounded four-field success receipt;
worker failure/empty stdout is handled through the existing safe exit mapping.
Shared correlated runtime diagnostics/phase projection remain T020.

### PostgreSQL recovery regression: established cause and narrow correction

Published head `d677e82` product run **40426069394** failed integration-postgres,
recovery, contract, privacy and failure-propagation; quality, unit,
integration-sqlite, browser, startup and the separate repository check passed.
Its integration artifact assesses merge revision
`460f22be3e2c461658bca8959bf3201d1f1c57a7`; host PostgreSQL passed and storage
PostgreSQL failed the same two `Corrupt` complete-recovery cases described above.
These are observed failures, not pending checks.

A temporary owned real-PostgreSQL diagnostic reproduced the refusal and captured
source/restored native catalogs privately. The only difference was
`__kofte_module_registrations_module_id_check`: original `BETWEEN` parsing
retained nested `AND` parentheses while actual dump/restore flattened them.
Both checks have identical semantics. PostgreSQL's native
`pg_get_constraintdef(oid, true)` removes this redundant grouping; the entire
source/restored catalogs then compared byte-for-byte equal. The physical verifier
now uses that server deparser and reviewed fresh v1/v2 reference fingerprints.
It still fingerprints full constraint definitions, flags and every other
catalog category; there is no broad parenthesis stripping, omitted constraint,
schema waiver or migration-1 edit. SQLite fingerprints are unchanged.
Temporary diagnostic test source was removed and the shipping test assembly
rebuilt. Existing actual recovery and tamper regressions pass **49/49**.
This fixes the reproduced regression, not every historical CI failure or
unimplemented format-2 recovery. Earlier failed evidence remains unchanged.

### Exact executed checks and red/green evidence

Commands use the existing isolated SDK 10.0.402/runtime 10.0.13; PostgreSQL
commands explicitly set `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`.

| Exact executed command | Result |
| --- | --- |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityEventTests --nologo` | Passed **10**, zero failures/skips. Actual full-capacity warning/refusal, immutable retention, bounded pruning and rollback on both providers. |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityKeysTests --nologo` | Passed **27**, zero failures/skips. Actual certificate encryption, restarts/retained rotation, wrong/missing/foreign/edited keys, links, cancellation and stopped authority. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityPreparationTests --nologo` | Initially **4/6 failed** before commands existed; then **6/6**, expanded **12/12**, and final **14/14 passed**, zero skips. Final cases cover exact consumed designation/mapping, revoked sessions/attempts, lost-key correction, owned partial retry and held real host leases. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~CurrentDatabaseCanResumeItsOwnedPreparingRing --nologo` | Initially **0/2 passed**: current DB could not finish its owned preparing ring. Fixed with existing-only preparation; both are included in final 14/14 above. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~Identity\|FullyQualifiedName~RegistryFormatTests\|FullyQualifiedName~RegistryConstraintTests\|FullyQualifiedName~LeaseLoss' --nologo` | Passed **105**, zero failures/skips, before the deparser correction. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~PostgresRecoveryDiagnosticsTests\|FullyQualifiedName~PostgresRecoveryExercise\|FullyQualifiedName~IdentityFormatTests' --nologo` | Passed **49**, zero failures/skips after the correction: actual backup/restore, private recovery records, both-provider physical/history tampering and preserved populated EF/module state. |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --nologo` | Passed **255**, zero failures/skips; repeated after the final owned-key retry/supervisor correction. |

During implementation, a missing shared diagnostics-helper visibility caused a
compiler failure and was fixed by reusing the existing internal failure mapping.
The first expanded preparation tests attempted internal migration helpers from
the host integration assembly and failed compilation; their setup now uses
independently constructed ADO.NET commands, without new friend-assembly access.
Both failures are resolved, not omitted evidence.

**T001-T017 are complete; T018-T100 (83 tasks) remain unchecked.** Next is the
endpoint/CSRF/fallback-policy foundation, shared diagnostics and identical
read-only prepared serving. No identity HTTP endpoint, working login/session,
enrollment, web journey, recovery-v2, full-matrix pass or acceptance is claimed.
The driver remains the original running implement stage.

After final source/tests and current-progress/operator documentation updates,
`python3 scripts/verify.py --suite quality` passed **all 20 steps**, including
the exact required `python3 scripts/check_repository.py`,
`python3 -m unittest discover -s scripts/tests -v` (**121 passed**) and
`git diff --check`, full solution format/build, locked restores and existing
frontend/dependency checks. No dependency, approved limit or design artifact
changed. This remains a tested partial implementation checkpoint, not a
completed foundation/stage or a claim that latest-head CI has passed.

## In-progress T018–T020: HTTP boundary and correlated private diagnostics

The original **005 / implement** stage, branch, draft PR and reviewed checklists
remain unchanged. T001–T017 are still the only completed tasks. The shared
components below are not yet installed in both actual serving entrypoints;
T018–T020 remain unchecked pending that T021 integration. No production identity
HTTP handler, established session, working login, enrollment, UI or recovery-v2
is delivered by this checkpoint.

The shared boundary admits only exact public protocol routes, retains anonymous
health/Development-only OpenAPI and rejects route aliases, wrong methods,
queries/bodies, Authorization credentials, oversized inputs and unauthorised
anonymous metadata. Default and fallback policies deny unrelated operations;
actor-v1/self access is not first-admin universal permission. POST execution
requires exact configured Origin/Fetch Metadata and actual framework antiforgery
validation. Safe problem JSON has only five fixed fields and generated
correlation; identity/error responses are non-cacheable/no-referrer.
Real Kestrel component tests use framework token pairs and test-owned handlers,
not mock principals or new production endpoints.

The initial component tests were red because production boundary APIs did not
exist. An OIDC-mode test then reproduced HTTP 500 when framework
`CookieSecurePolicy.Always` rejected the approved fixed HTTP backend. The
supported CookieBuilder override now derives the actual Secure flag solely
from validated mode, retaining the Secure/HttpOnly/host-only `__Host-` cookie
without trusting forwarded scheme/host. The actual ingress-hop test passes.
[ADR 0007](../../docs/adr/0007-authentication-bootstrap-boundary.md#implementation-follow-up-fixed-https-ingress-and-private-diagnostics)
records this detail and the official API fallback; Learn/Context7 MCP remain
unavailable. No new dependency or changed production topology is introduced.

Infrastructure diagnostics use strict fixed codes/fields, generated correlation
independent of caller trace identity, at most 1 KiB/event, one terminal outcome,
bounded phase/warning counts and fixed metric dimensions. Sanitized spans share
that generated correlation, without caller baggage. Owner mutations/events,
initialization and key-file effects share the observation's truthful unchanged/
committed/uncertain evidence. Key protection inherits the enclosing deadline
and cancellation instead of starting an uncancellable three-second allowance;
gate waiting is bounded too. A failing terminal sink is not silently retried
or reported successful. No raw exception, credential, query, path, nominee or
profile value becomes a diagnostic field.

Stopped workers now emit the same bounded records rather than the earlier
four-field success receipt. The supervisor rejects unknown/duplicate fields,
wrong context/correlation, malformed/oversized frames and records following the
unique terminal. It projects success only after observed natural exit and
verified cleanup. Missing/malformed/lost acknowledgement remains uncertain.
Real operator tests compare the diagnostic correlation with the committed
database event, distinguish committed preparation from unchanged retry and
preserve the specific key-refusal outcome without printing private paths.
Private composition transport now carries a strict identity-or-composition
union and serializes whole concurrent frame writes. Its identity projection
accepts only validated serve-mode records from the admitted owned worker.
No new identity-specific Platform lifecycle contract is exposed. Actual worker
identity service/sink registration remains T021.

### Reproduced SQLite pre-open contention

The first owner/event/deadline regression run passed 50/51 and reproduced
`Busy` during the managed header read in actual twelve-way SQLite admission.
A deterministic held-header regression also failed with that exact pre-open
refusal. It is before a SQLite handle/transaction or any effect, not an
uncertain commit. DeadlineStore now retries only that classified guard refusal
using existing 25 ms probes and original clipped budget/cleanup reserve.
No file sharing guard, corruption check, transaction replay or deadline changed.
The transient-release, permanent-lock, cancellation, no-late-write and exact
ten-admitted/two-refused SQLite selection then passed **22/22**.

### Actual worker interruption and preserved SQLite journal boundary

Private test-only diagnostic barriers hold the actual admitted maintenance
worker immediately before/after commit. Tests kill that exact reported worker,
await its exit and its supervisor, then inspect the real database and retained
key files. Neither interruption can publish success or automatically replay an
unknown commit. Verified committed preparation retries preserve the one event,
security revision and exact key-file digests.

The first four-case interruption execution passed three: the SQLite
before-commit case incorrectly expected all interrupted state to be immediately
retryable. A repeated diagnostic execution established
`identity_reconciliation_required`, not a missing implementation or quota
failure. SQLite had retained a journal although the database remained format 1.
The accepted read-only physical inventory guard deliberately refuses any such
sidecar. That guard is unchanged: the permanent regression now requires that
specific refusal and byte-for-byte preservation of database/journal/key files.
No journal is deleted, recovery check bypassed or arbitrary failure accepted.
Operator guidance documents the need to inspect preserved recovery state.
This does not claim that every crash residue is automatically recoverable.

The first operator-correlation test compilation also exposed inaccessible
Infrastructure internals and an xUnit assertion diagnostic. The tests now
parse independently expected JSON using public ADO.NET observations, without
new friend-assembly access; both compile errors are resolved.

### Checks executed during this increment

All .NET commands select the existing isolated SDK 10.0.402/runtime 10.0.13.
Both-provider commands set `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`.

| Exact executed command                                                                                                                                                                                                                                                      | Actual result                                                                                                                                                                  |
| --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityAdmissionTests\|FullyQualifiedName~IdentityEventTests\|FullyQualifiedName~IdentityDeadlineTests' --nologo` | Initially **50/51 passed**; SQLite pre-open header-sharing failure reproduced, not waived.                                                                                     |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~TransientHeaderSharingContention --nologo`                                                                          | Initially red with the same `Busy` header-guard stack before the correction.                                                                                                   |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~SqliteIdentityAdmissionTests\|FullyQualifiedName~SqliteIdentityDeadlineTests' --nologo`                            | Passed **22**, zero failures/skips, after the bounded pre-open correction.                                                                                                     |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityDiagnosticsTests\|FullyQualifiedName~CompositionDiagnosticTests\|FullyQualifiedName~IdentityKeysTests' --nologo`                              | Passed **85**, zero failures/skips, including strict transport bounds, mixed concurrent frames, no caller baggage, sink refusal and inherited expired/cancelled key deadlines. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityPreparationTests\|FullyQualifiedName~IdentityPolicyTests' --nologo`                                              | Passed **20**, zero failures/skips, before the later four actual interruption cases were added.                                                                                |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~ActualWorkerTermination --nologo`                                                                                         | Initially **3/4 passed**; established retained-journal refusal described above. Final combined-gate results follow separately.                                                 |

### Published T017-head CI follow-up

Actual pushed head `77feefea5c3d015380f8c45e16e74df8627ca663` now passes
all eleven observed contexts: the separate repository check
([run 40432430645](https://github.com/Stonefly-Labs/Kofte/actions/runs/40432430645))
and quality, unit, integration-sqlite, integration-postgres, recovery, contract,
browser, privacy, startup and failure-propagation
([run 40432430650](https://github.com/Stonefly-Labs/Kofte/actions/runs/40432430650)).
This is new published-head evidence, not a relabeling of historical failures,
a CI result for the current uncommitted increment, a completed implement stage
or proof of unbuilt identity scenarios.

### Combined checkpoint regression results

| Exact executed command                                                                                                                                                                                                                                                                                                                             | Final result                                                                                                                                                        |
| -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~Identity\|FullyQualifiedName~RegistryFormatTests\|FullyQualifiedName~RegistryConstraintTests\|FullyQualifiedName~LeaseLoss\|FullyQualifiedName~MaintenanceObserverFailureTests' --nologo` | Passed **108**, zero failures/skips: both-provider identity/format/constraints, concurrent admission, deadlines, lost authority and rejected worker-output cleanup. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityPreparationTests\|FullyQualifiedName~IdentityPolicyTests\|FullyQualifiedName~HealthEndpointTests' --nologo`                                                                             | Passed **28**, zero failures/skips: all operator/policy cases, four actual interruption cases, and retained actual empty/composed health regressions.               |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --nologo`                                                                                                                                                                                                                                                 | Passed **263**, zero failures/skips.                                                                                                                                |

These are component/foundation regressions, not the remaining T021/T022
serving/foundation gate, independent OIDC journey or the completed implement
stage. Task/checklist markers and driver state remain unchanged.

Final focused publishing checks also passed:
`python3 scripts/check_repository.py`;
`python3 -m unittest discover -s scripts/tests -v` (**121 passed**);
`git diff --check`;
`dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet`;
`dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo`
(zero warnings/errors);
`dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-build --no-restore --nologo`
(**263 passed** again after final diagnostic assertions); and
`npm run format:check`. The changed Markdown was formatted, then historical
implementation evidence was restored byte-for-byte, keeping this report
append-only. Dependencies were not restored/reinstalled and unchanged frontend
runtime/build suites were not rerun for this component/diagnostic-only increment.

## T018-T022: actual serving integration and foundation gate

The same `005 / implement` stage, original branch and draft PR #8 continue.
Requirements **16/16** and quality **36/36** remain read-only review gates.
T018-T022 are now checked, bringing implementation to **22/100**; the stage
itself remains running. No login, session, enrollment or recovery-v2 handler
is mapped merely to obtain a healthy startup.

`IdentityRuntime` is shared by the actual empty and composed serving paths.
It requires explicit validated configuration, exact prepared format 2,
acknowledged installation/mode/trust metadata, usable retained keys and absence
of restored-copy quarantine. Valid v0/v1 receives `identity_prepare_required`.
No startup migration, missing-key regeneration or live discovery is performed.
Configuration rejection has a fixed private correction record even before a
mode can be established. Composition projects the same allowlisted private
identity records through its existing framed union.

The runtime registers the same request policies, persistent protection provider
and owner store against the actual already-held normal SQLite/PostgreSQL host
lease. It acquires no competing host lease and receives no stopped provisioning
authority. Configuration and keys are disposed with their serving lifetime,
including failed startup. Existing storage reconciliation now exposes its
verified installation internally instead of duplicating physical verification.
Unused configuration reload watchers are disabled before builder defaults
create them. Builder creation precedes the existing uncomposed three-second
storage budget; that budget and composition's inherited callback envelopes
are not extended.

Owned serving fixtures now explicitly run stopped registry upgrade/identity
preparation before immutable inventory baselines. Their private configuration
and key roots are outside the recovery data root; production/staging fixtures
use actual certificate-encrypted keys and an unreachable test-only issuer.
Recovery-v1-only scenarios are not blanket-upgraded. A legacy PostgreSQL
platform-only backup still refuses an identity-ready registry rather than
pretending it is a complete recovery set.

Development tooling forwards the explicit configuration path without preparing
user storage. The verification runtime and both browser warm-start fixtures
prepare only their owned installation before measuring serving. The latter
retain exact pre-existing module payloads through empty/composed restarts and
the original readiness/page/total/cleanup limits. Setup guidance documents the
separate commands and owner-only layout.

### Corrections and retained failed evidence

The initial actual-entrypoint tests were red before integration. The first
broad host run passed **65/77**, not all 77. Identity middleware incorrectly
preceded accepted health guards, changing malformed-health error bodies; both
entrypoints now install health guards first, matching the component host.
Other failures occurred during PostgreSQL Docker fixture startup; their exact
engine cause is unestablished. No unrelated shared containers/networks were
removed.

A subsequent serial run passed **76/77**. The remaining 121-second composed
health test parsed identity stderr as composition stdout and captured its
baseline before new explicit preparation. It now prepares before that baseline,
parses only composition stdout and independently validates identity stderr.
Its exact byte-preservation, serving-duration and stop-limit checks remain.
The updated health/startup selection passed **10/12**, including that test,
but both composed production-OIDC cases exhausted their original callback
budgets. The cause was not conclusively diagnosed. A temporary owned-worker
sample did not reproduce the failure and was removed; no probe is shipped.
An unmodified, no-probe eight-case startup run then passed, followed by another
eight-case pass under normal runner scheduling after assigning these actual
startup-envelope measurements to an explicit nonparallel collection. This
controls unrelated fixture scheduling, not product deadlines or assertions.
These results do not prove a hard-real-time guarantee on a contended machine
or erase the earlier timeouts.

The first current SQLite `bounded-stop` recipe exposed one remaining obsolete
empty-stderr assertion in the intentional serving-cleanup failure case. It now
accepts only independently parsed, exact-field, bounded startup/access identity
records; stopped overflow/cancellation cases still require empty stderr.
Both-provider fault/privacy recipes subsequently passed without changing
timeouts, exit checks, canary scans or preserved-storage comparisons.

The changed Python runtime orchestration initially broke a mock that recreated
the store for every setup command. It now models only explicit store init and
checks the ordered upgrade/prepare commands, owner-only configuration and
separate layout. The required 121-test suite passes. An unsupported native
middle-wildcard filter and invalid `startup --provider` invocation were command
selection errors, not executed passing tests; corrected exact commands follow.

### Foundation evidence and task mapping

T022 reuses the already-owned focused test files rather than adding an
`IdentityFoundationTests.cs` aggregator that duplicates their assertions:
`IdentityFormatTests` covers physical/history/constraint tampering and rollback;
`IdentityAdmissionTests` covers checked overflow, exact quotas, foreign/stopped/
lost normal authority and idempotent metadata; `IdentityDeadlineTests` covers
real contention, cancellation and no late writes; `IdentityEventTests` covers
transactional rollback, immutable evidence and capacities.
`IdentityPreparationTests` adds actual admitted worker interruption and stopped
refusal/idempotence, while `IdentityKeysTests` and `IdentityPolicyTests` retain
crypto, ownership, CSRF and default-deny evidence. The serial 77-case host run
executed these host cases; its single unrelated harness failure and correction
are reported above. The new startup matrix checks both actual entrypoints and
providers, retained full storage/key hashes, trust changes, quarantine, missing
keys, cleanup and production serving without provider availability.

All commands below use the existing SDK 10.0.402/runtime 10.0.13; provider
commands opt in with `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`. No dependency
manifest changed and no package restore/install was performed.

| Exact executed command | Result |
| --- | --- |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Passed, zero warnings/errors. |
| `dotnet tests/unit/Kofte.Host.Tests/bin/Release/net10.0/Kofte.Host.Tests.dll -parallelMode none` | Passed **263**, zero failures/skips. |
| `dotnet tests/integration/Kofte.Storage.IntegrationTests/bin/Release/net10.0/Kofte.Storage.IntegrationTests.dll -parallelMode none -class '*IdentityAdmissionTests' -class '*IdentityDeadlineTests' -class '*IdentityEventTests' -class '*IdentityFormatTests'` | Passed **94**, both providers, zero failures/skips. |
| `dotnet tests/integration/Kofte.Storage.IntegrationTests/bin/Release/net10.0/Kofte.Storage.IntegrationTests.dll -parallelMode none -class '*CompositionAuthorityTests' -class '*CompositionProviderTests'` | Passed **25** dependent serving/authority cases, both providers, zero failures/skips. |
| `dotnet tests/integration/Kofte.Host.IntegrationTests/bin/Release/net10.0/Kofte.Host.IntegrationTests.dll -parallelMode none -class '*IdentityStartupTests'` | Passed **8**, both providers/entrypoints/modes, no profiler. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityStartupTests --nologo` | Passed **8** under normal scheduling with the explicit controlled collection, zero failures/skips. |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario private-diagnostics` | Passed **67** assertions, all six capture channels. |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario private-diagnostics` | Passed **67** assertions, all six capture channels. |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider sqlite --scenario bounded-stop` | Passed **97** assertions, all seven ignoring-token boundaries, actual descendant exit, cancellation, authority and unchanged snapshots. |
| `dotnet run --project tests/contract/Kofte.Composition.Consumer --no-build -c Release -- --provider postgres --scenario bounded-stop` | Passed **102** assertions, including actual serving-lease loss. |
| `python3 scripts/verify.py --suite startup` | Passed **12** steps, including two empty and two configured browser warm-start cycles with preserved original payloads and unchanged envelopes. |
| `npm run format:check` | Passed. |
| `npm run typecheck --workspace tests/e2e` | Passed. |
| `npm run lint --workspace tests/e2e` | Passed. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed **121** after the explicit-preparation mock correction. |
| `python3 scripts/check_repository.py` | Passed after final progress/task/documentation updates. |
| `git diff --check` | Passed after final progress/task/documentation updates. |

### Published-head CI is separate evidence

The preceding `df95d75` checkpoint's repository run **40437746912** passed.
Product run **40437746923** passed quality, unit, both integration jobs,
browser, startup and recovery when observed. Privacy failed its SQLite public
`private-diagnostics` recipe; failure-propagation failed its PostgreSQL
`bounded-stop` integration baseline before exercising mutations. Their original
underlying causes remain unestablished. Contract was still running when last
queried. The fresh local public recipes above pass, but are not a relabeling of
that CI run, a full failure-propagation execution or a CI result for this new
checkpoint.

**T023-T100 (78 tasks) remain unchecked.** Real OIDC/mapping/attempts/session
commit, enrollment, web journeys, identity recovery and the full final evidence
matrix remain dependent implement work. No converge, review, acceptance or
merge is requested.

## T024/T027: exact transactional user mapping and composition fixture correction

The original **005 / implement** stage remains running on its recorded branch
and draft PR #8. T001–T022, T024 and T027 are complete (**24/100**); all other
task markers and both reviewed checklists are unchanged. No OIDC session,
working login, enrollment, UI, recovery-v2 or later Spec Kit stage is delivered
by this checkpoint.

`IdentityUserStore.ResolveAsync` receives the existing owner mutation; it never
opens or commits a second transaction. It derives mode from verified metadata,
preserves byte-exact issuer/subject keys, rejects invalid UTF-16/control/profile
and byte bounds, and inserts the random local user, unique mapping and required
creation event together. Existing mappings retain ID/mode/creation time and
update display text only. Capacity is 10,000 users across modes; existing users
remain resolvable at capacity. Only the exact mapping-key `ON CONFLICT` target
is reconciled. A real unrelated UNIQUE violation propagates and rolls back,
not an "existing user" fallback. No email, password, provider privilege or
administrator field is introduced.

The shared prepared-state verifier now checks complete user/mapping
correspondence without repairing orphan users. Real SQLite/PostgreSQL cases
cover twelve simultaneous first resolutions, exact issuer-path/subject case
and spaces, equal and changed display/email-shaped profile values, transaction
rollback after mapping creation, ASCII/multibyte 2,048-byte issuers, 255-byte
subjects, 128 astral Unicode scalars, invalid Unicode/controls and one-over
limits. The capacity case seeds 9,999 complete mappings, admits exactly the
10,000th, refuses a new key and still updates that established user.

Three independently started owner processes acquire actual normal host leases,
perform full startup/prepared-state verification and resolve the original key.
They compare exact user ID, creation time, mode, display, consumed designation,
generation/time and immutable creation-event ID. Explicit stopped reconciliation
then changes trusted configuration and mode, keeps distinct replacement/development
users, refuses the wrong development namespace and returns to the original
production user/designation. These are storage-owner process tests, **not an
HTTP/OIDC login or retained-session demonstration**; delivered coordinator/API
evidence remains T023/T025–T026/T028–T040. The worker and dispatcher live only
in the dedicated storage integration test project; no production test route,
fixture switch, new package or migration byte is added.

### Actual red results and corrections

The initial four mapping cases could not compile because `IdentityUser` and
its store did not exist. After implementation they passed **8/8** on the two
providers. The expanded worker initially omitted the required cancellation
token and failed compilation; that test-only error is corrected. The next
expanded run passed **12/14**: both process cases wrongly demanded empty stderr,
although a successful owner mutation emits its required private terminal record.
They now independently require exactly the ten allowed fields, expected fixed
values, generated correlation, committed durability and the 1 KiB bound. No
arbitrary stderr is accepted. The combined storage selection then passed
**108/108**. Subsequent unrelated-UNIQUE, exact-space and multibyte boundary
additions pass the final **18/18** mapping selection.

### Published foundation CI: reproduced preparation omission

Published foundation head `d12fe0d` repository run **40442569234** passed.
Product run **40442569060**, assessing PR merge revision
`687e6708dd5fa3caa90f0371559af089e58cde44`, passed quality, unit, browser, startup
and privacy. Both integration jobs passed their host and non-composition
storage selections, then failed the public `disable-reenable` and
`failure-correction` recipes. Contract additionally failed `ordered-migration`;
recovery reached the same correction failure, and failure-propagation stopped
at its non-green PostgreSQL baseline before mutations.

The exact SQLite `disable-reenable` command reproduced
`composition_test_boot_exited_before_ready`. Source inspection established that
the shared public migration preparation helper never explicitly prepared
identity before its later serving/preservation baselines. It now performs that
owned stopped preparation after the existing module preparation/cleanup proof.
All existing original-ID/data/history/full-inventory, cleanup, correction and
three-cycle assertions remain unchanged. Serving still refuses unprepared
identity; no runtime migration, weakened stderr/deadline/inventory assertion or
complete-set recovery format waiver is introduced. Genuine recovery-v1-only
fixtures and legacy identity-backup refusals remain unchanged.

The focused public contract regression passes **8/8** on both stores, including
the three reproduced recipe families. This closes the reproduced local
omission; it is **not** a relabeling of failed CI, a new full integration/
recovery/failure-propagation run or evidence that a later head's CI passed.

### Exact checkpoint verification

All .NET commands select the existing isolated SDK 10.0.402/runtime 10.0.13.
Real-provider commands set `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`.
No dependency manifest changed and no package restore/install occurred.

| Exact executed command | Result |
| --- | --- |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~Identity --nologo` | Passed **108**, both providers, zero failures/skips, before the final UNIQUE and multibyte additions. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityMappingTests --nologo` | Final **18 passed**, both providers, zero failures/skips; earlier eight- and sixteen-case successes are intermediate evidence only. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~CompositionMigrationContractTests\|FullyQualifiedName~IndependentPublicFailuresRequireTheirNamedCorrectionAndActualOwnedExit' --nologo` | Passed **8**, both providers, zero failures/skips. Actual ordered migration, three disable/re-enable cycles and named failure corrections retain their independent assertions. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~IdentityPreparationTests\|FullyQualifiedName~IdentityStartupTests' --nologo` | Passed **28**, both providers and actual empty/composed serving, zero failures/skips. |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Passed, zero warnings/errors. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet` | Passed. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | Passed **121**. |
| `git diff --check` | Passed. |
| `npm run format:check` | Passed. |

**76 tasks remain unchecked.** Continue real OIDC protocol and one-commit
attempt/session/ticket orchestration in this same implement stage. The durable
driver is not completed, accepted or advanced by publishing this checkpoint.

## T023/T026: maintained native OIDC and bounded HTTPS protocol seam

The original **005 / implement** stage remains running on its original branch
and draft PR #8. T001–T024 and T026–T027 are now complete (**26/100**); T025 and
the other 73 remaining tasks are unchecked. Reviewed checklists, approved
intent, migration bytes and the driver stage are unchanged.

`OidcConfiguration.Apply` configures the adopted maintained ASP.NET Core
10.0.13 handler and IdentityModel 8.19.2 validation. It requires confidential
`client_secret_post` code+S256, query callback, exact issuer and single exact
audience, RS256 signature/lifetime with 60-second skew, and native protected
state/correlation/nonce. It clears inbound claim actions/remapping, disables
saved tokens, UserInfo, PAR and telemetry parameters, fixes the callback to
the configured public origin and uses separate Secure/HttpOnly/Lax protocol
cookies. Input admits only scalar standard code/error callbacks with required
state, no duplicate/implicit/mixed fields and an exact response issuer when
provided. The helper is deliberately **not yet registered in product hosts**.

The native handler clears `RequireSignedTokens` for code-flow token responses.
A small `JsonWebTokenHandler` subclass therefore restores that parameter on
its cloned validation input and delegates all cryptographic/token validation
to the maintained implementation. Independently generated `alg:none` and
foreign/RS384 signatures are rejected. No custom JWT authenticator, manually
asserted principal or signature-validation bypass is introduced.

Native ASP.NET Core validates protected state/correlation and clears protocol
state before its protocol-validator call. The protocol validator retains its
framework `RequireStateValidation=false` integration setting; native protected
state is not skipped. Nonce is mandatory. The fixture intercepts
**OnTicketReceived**, after full native validation, not OnTokenValidated,
which occurs before the handler's final nonce/protocol checks.

`OidcBackchannel` requires a current request and allows only the configured
exact HTTPS discovery/JWKS GET and token POST endpoints. Actual DNS answers
and the connected socket peer must belong to configured networks. Standard
TLS hostname/chain validation remains enabled; only the test-owned socket
handler injects trust for its private CA. There is no configuration skip-TLS
flag. Proxy, cookies, automatic redirects/decompression and credentials/
Authorization headers are disabled. HTTP/1.1 non-reused connections and an
explicit per-message connection-attempt guard prevent retry egress.

Request-local accounting allows three backchannel requests, two seconds each,
clipped to three seconds combined and inherited request cancellation.
Responses are streamed with announced/unannounced wire limits, strict UTF-8,
non-duplicate bounded-depth JSON and a separate 16 KiB ID-token cap. No
compressed expansion or response body reflection occurs. Lazy metadata must
match issuer and all configured endpoint URLs and advertise the delivered
code/S256/RS256/client-secret-post flow. Public RSA keys are bounded and
validated; private, malformed, duplicate or excess keys refuse the exchange.

The cache uses monotonic time: fifteen-minute maximum, one admitted refresh
per minute, no background fetch or expired/failed-refresh fallback. A signing
key must still belong to the current unexpired cache at token validation, so
a callback cannot cross the expiry boundary and admit already expired key
material. Unknown-key fallback to every other key is disabled. A required
exchange that cannot fit fails rather than extending the four-second callback.

### Actual independent evidence and limits

`OidcProtocolTests` hosts the real maintained native handler against the owned
HTTPS issuer's independent signing/authorization/token implementation. The
positive exchange observes exact issuer/subject/display only at the
post-protocol seam. Negative cases cover issuer case/trailing slash/audience,
signature/key/algorithm/unsigned/time, missing/wrong nonce, wrong protected
state/correlation/PKCE, malformed/missing/private/duplicate/17th keys, exact
16-key admission, unsolicited/duplicate/mixed/implicit callbacks, preserved
cookie/code replay, provider cancellation/outage/redirect and fresh correction.
An actual disconnected callback propagates cancellation to the provider and
finishes without reaching the ticket seam.

The transport suite exercises actual HTTPS/private-CA rejection, denied
endpoint/network, strict byte/JSON/UTF-8/decompression boundaries, a fourth
request refusal before egress, real per-request/combined cancellation and
controlled exact 59,999/60,000 ms refresh and fifteen-minute cache boundaries.
Expiry and failed revalidation cannot retain an admissible old key.

These are storage-independent native-protocol tests, scheduled under the
existing SQLite trait, **not SQLite/PostgreSQL session or full API evidence**.
The fixture uses test-owned ephemeral Data Protection and intercepts before
framework sign-in: no local user/session/actor or product test endpoint exists
there. It rejects every non-protocol Set-Cookie. Product retained-key,
one-owner-commit/cookie-publication and actual browser/API journeys remain
T025/T028–T040; do not relabel this as a working login or T038 restart proof.

### Actual red results and corrections

The first protocol selection failed compilation because the Login namespace
did not exist. Its first implemented build exposed a test principal-nullability
error. After that correction, the fixture failed because its owned content root
was not created. The next run exposed an empty Cookie header, was stopped
after repeated identical failures, and was corrected to omit absent cookies.
The fixture now explicitly stops its owned Kestrel host within five seconds.
The real positive then passed; the first expanded selection passed **19/19**.
Further protocol and transport cases passed **50/50**, then the expanded
protocol/transport/original-issuer selection passed **62/62**. Actual HTTP
disconnect and unannounced streamed-boundary additions separately passed
**3/3**, yielding **65 distinct passing cases**, not a fabricated single run.
The solution build passed with zero warnings/errors; format verification found
one whitespace error, corrected using the existing formatter and reverified.

Direct source review then found that failed unknown-key refresh could discard
the cache and allow subsequent cold-discovery retries inside its one-minute
refresh bound. The new
`FailedUnknownKeyRefreshCannotTurnIntoUnboundedColdDiscoveryRetries` regression
failed with **no expected exception** and unexpected network activity.
`dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~FailedUnknownKeyRefreshCannotTurnIntoUnboundedColdDiscoveryRetries --nologo`
recorded that actual initial red result. Failed key refresh now preserves its
cooldown and refuses before egress at 59,999 ms, admits revalidation at 60,000 ms,
and consistently uses the cache clock. No expired key is restored. The final
combined selection passed **66/66**, including all prior cases and this
correction; solution build and format verification passed again.

### Exact checkpoint verification

All .NET commands use the existing isolated SDK 10.0.402/runtime 10.0.13. No
package manifest or lock changed; no install or restore occurred.

| Exact executed command | Result |
| --- | --- |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~OidcProtocolTests\|FullyQualifiedName~OidcBackchannelTests\|FullyQualifiedName~OidcIssuerTests' --nologo` | Final **66 passed**, zero failures/skips, 9m47s; earlier **62 passed** before the cancellation/streamed/failed-refresh additions. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~ActualCallbackDisconnectCancelsProvider\|FullyQualifiedName~UnannouncedStreamedBody' --nologo` | **3 passed**, zero failures/skips. |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Passed, zero warnings/errors. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet` | Passed after correcting the reported whitespace. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | **121 passed**. |
| `git diff --check` | Passed. |
| `npm run format:check` | Passed. |

### Published mapping/composition checkpoint CI

Published `cdb19a8` repository run **40445118973** and product run
**40445118966** both completed **success**. All ten product contexts passed:
quality, unit, integration-sqlite, integration-postgres, startup, browser,
recovery, contract, failure-propagation and privacy. This is observed new-head
CI evidence for the preceding mapping/composition checkpoint, not a rewrite
of `d12fe0d` failures or a claim that the new OIDC helper head has passed CI.

Continue the same implement stage with T025 and one-owner-commit attempt/
session/ticket coordination. **74 tasks remain unchecked.** No convergence,
final review, acceptance, ready status or merge is requested.

## T028: durable owner-transaction attempts and protected local binding

The original **005 / implement** stage remains running on its original branch
and draft PR #8. T001–T024 and T026–T028 are complete (**27/100**); 73 tasks
remain unchecked. No stage transition, feature acceptance or merge is requested.

`IdentityLoginAttempts` creates five-minute pending attempts in the caller's
existing owner mutation and returns the actual persisted values on both stores.
It admits at most 256 live pending attempts installation-wide and eight per
browser digest, pruning at most 128 expired/consumed rows. IDs are random UUIDs;
only lowercase SHA-256-shaped browser digests and the exact local return paths
are accepted. No raw protocol credential, state, nonce, verifier or browser
secret is stored. UTC values and stored five-minute correspondence are checked.
The existing user-store ID/UTC parsing is shared without changing its semantics.

Consumption is one compare-and-update matching pending state, browser digest,
mode, trust digest, epoch and the half-open local lifetime. It receives the
same caller-owned mutation as user mapping and never opens or commits a second
transaction. The authenticated session coordinator must call it inside its
future complete login commit; these primitives are deliberately not exposed
through an unfinished product route.

`LoginAttemptBinding` creates framework authentication properties for the
local attempt ID/browser digest. Its reader accepts only canonical nonempty
IDs and an exact constant-time digest match. It does not derive the trusted
return path from incoming properties. The maintained protected-properties
serializer and an actual native OIDC code/PKCE exchange preserve that binding
without exposing it in the authorization URL. The native fixture independently
seeds expected property names and values and still stops before sign-in.
No new friend assembly, public product API, authentication bypass, package,
migration or lock change is introduced.

### Actual red results and corrections

The first attempt test command failed compilation because `IdentityLoginAttempt`
did not exist. Implementation initially missed the storage namespace import
in the shared parsing helper; the corrected combined attempt/mapping selection
passed **34/34**. Expanded races and mode/trust checks pass **40/40** on both
providers: 22 attempt cases plus the existing 18 mapping cases. Tests compare
exact IDs, binding, epoch, mode, times, states and revisions, including eight
concurrent browser starts, 256/257 capacity, exact expiry, bounded cleanup,
twelve simultaneous consumers with one commit, missing/wrong/replayed binding,
changed security context and rollback of consumption/mapping/event together.

Protected-property unit tests were initially red because the binder did not
exist; both now pass. The first native binding-fixture build attempted to expose
an Infrastructure-internal result to Host.IntegrationTests and failed. The
fixture now uses independent framework properties without widening internals.
Its first run reused one mutable challenge-property instance and returned 500
on the second challenge. Copying seed items into fresh per-request properties
corrected the fixture; both actual native positive/binding cases then passed.
The other existing native case passed in that failed run; it was not relabeled
as an all-green result.

### Exact checkpoint verification

Commands select the existing isolated SDK 10.0.402/runtime 10.0.13; both-provider
commands opt in with `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`. No restore or
installation occurred. Required final repository/format checks were executed
after the fixture correction; earlier checkpoint results are not reused.

| Exact executed command | Result |
| --- | --- |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --filter FullyQualifiedName~IdentityAttemptTests --nologo` | Initial missing-type compilation failure, before implementation. |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-restore --filter FullyQualifiedName~LoginAttemptBindingTests --nologo` | Initial missing-binder compilation failure, before implementation. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~IdentityAttemptTests\|FullyQualifiedName~IdentityMappingTests' --nologo` | **40 passed**, both providers, zero failures/skips. |
| `dotnet test tests/unit/Kofte.Host.Tests/Kofte.Host.Tests.csproj -c Release --no-build --no-restore --filter FullyQualifiedName~LoginAttemptBindingTests --nologo` | **2 passed**, zero failures/skips. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~ActualNativeProtectedStateRetainsOnlyTheLocalAttemptAndBrowserBinding\|FullyQualifiedName~NativeHandlerValidatesRealCodePkceAndNonceBeforeTheLocalTicketSeam' --nologo` | Final **2 passed**, zero failures/skips, after the fresh-property correction. |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Passed after the fixture correction, zero warnings/errors. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet` | Passed after the fixture correction. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | **121 passed**. |
| `git diff --check` | Passed. |
| `npm run format:check` | Passed. |

T025/T029/T030 still owe the real session/cookie bridge, one known owner commit
before publication, and uncertain-acknowledgement/publication-failure evidence.
Current component tests are not full product login, stored-session, actor,
browser or retained-session restart evidence.

## T025/T029/T030: atomic native login and durable session bridge

The original **005 / implement** stage remains running on its original branch
and draft PR #8. **T001–T030 are complete (30/100); 70 tasks remain unchecked.**
The historical checkpoints above remain unchanged. No driver completion,
converge, review, feature acceptance or merge is implied.

### Delivered components and actual acceptance evidence

`ValidatedLogin` is called only after the maintained native handler finishes
protocol validation, at `OnTicketReceived`. It verifies the configured exact
issuer/subject shape and protected local browser binding. One existing owner
mutation resolves the exact mapping/user, records required creation evidence,
consumes the durable attempt, and creates the protected minimal session/event.
It preserves the original one-second local work and enclosing callback budget.
Only a returned, known committed mutation can bind the private request-local
reference and call framework sign-in. There is no uncertain-outcome retry.
Existing exact-key UNIQUE reconciliation remains in the mapping helper; the
92-case dependent storage selection includes its eighteen both-provider cases.

`IdentityTicketStore` generates fresh 256-bit base64url references and a
domain-separated SHA-256 digest. The retained encrypted ticket contains only
local UUID/mode and fixed scheme/lifetime properties, not external provider
claims, tokens, roles or the raw reference. Its three framework storage
overloads can return the private committed reference exactly once for the exact
serialized ticket. Unbound, cross-request and reused calls refuse without
writing a second transaction. Retrieval opens fresh owner work, verifies the
protected ticket inside that transaction and sets only request-local current
state after known commit. Corrupt protection rolls back activity renewal and
returns unavailable rather than an anonymous or cached success.

`IdentitySessionStore` enforces eight live sessions/user and 50,000/installation
without eviction, exact thirty-minute idle/eight-hour absolute lifetimes,
renewal clipped to absolute expiry, current user/mode/epoch/designation and
terminal compare-and-update revocation/observed expiry with required events.
It never opens or commits an independent transaction.

The maintained cookie handler normally renews an existing session key during
sign-in. The dedicated cookie manager withholds the older cookie on the exact
new-login paths, so repeat login receives a fresh committed reference.
**Product registration must use this manager and disable native sliding
renewal.** Durable activity renewal happens only during fresh retrieval.
Framework `RemoveAsync` cannot return the confirmed revoke boolean; the later
sign-out endpoint must deliberately distinguish confirmed revocation, absent/
expired session and uncertainty. No sign-out route is claimed here.

The test-owned `LoginCommitFixture` uses actual native OIDC and cookie handlers,
the independent owned HTTPS issuer, real prepared certificate-protected keys,
normal host leases and actual SQLite/PostgreSQL. Its routes are fixture-only;
neither an externally asserted principal nor a product test bypass is added.
Baseline preparation genuinely records `keys_prepared` at security revision 1,
and every exact state/event assertion preserves that baseline.

| Obligation | Evidence in the final thirty-case selection |
| --- | --- |
| T025 one commit before cookie | Exact user/mapping/attempt/session/event IDs, shared revision/correlation and opaque-reference-only cookie; unbound/reused framework stores cannot mutate. |
| T025 failed publication/unknown acknowledgement | Before-commit rollback, after-commit interruption and failed cookie publication yield no session cookie. Four actual callback-owner process-kill cases independently reopen storage after verified owned-child exit. |
| T029 session identity/protection | Independent digest expectation, minimal protected ticket, fresh reference with an existing browser cookie, no persisted raw reference and corrupt-ticket rollback/unavailable response. |
| T029 lifetimes and terminal state | Controlled exact idle and absolute boundaries through actual cookie authentication; repeated activity cannot extend absolute expiry, and concurrent revocation/activity cannot resurrect the session. |
| T029 both capacities | Eight/ninth-user-session refusal preserves earlier sessions/events. A fixture baseline of 49,999 additional valid protected rows reaches exactly 50,000; real next login refuses without eviction, then succeeds after controlled expiry. |
| T030 coordinator | Actual native validated callback invokes the production coordinator/bridge and real framework sign-in; mapping/creation/consumption/session/evidence remain indivisible before publication. |

The global-capacity baseline contains 6,250 complete additional users/mappings,
each with at most eight sessions, and uses actual prepared protection keys.
This is bounded fixture seeding, **not 49,999 OIDC journeys or creation-event
evidence**. Its ninety-second setup budget does not change any product request
budget. Scalar snapshots avoid the operation store's 128-returned-row limit.

The real crash child is admitted by the existing private fixture dispatcher,
uses the original prepared installation and independently acquires the normal
lease/keys. Its private test CA file contains only the owned issuer's public
certificate; hostname and chain verification remain enabled. The parent
verifies the barrier's exact owned PID, kills that process before publication,
observes the genuinely lost HTTP acknowledgement and independently reopens
durable state. No shared Docker resource or unrelated process is terminated.
This is the actual callback owner, not a surrogate transaction.

### Initial red/failed results and corrections

The initial contract command failed compilation before the session/bridge and
coordinator APIs existed. Later fixture additions likewise first failed for
missing probe/clock/crash/capacity APIs. Compilation exposed ambiguous store
lease/path types and missing Hosting imports; explicit aliases/imports corrected
them without widening product access. A nested minimal-handler inference
triggered ASP0016; an explicit `Task` handler replaced it. Existing xUnit
analyzer requirements were followed without suppression.

The first twelve-case actual contract run passed two and failed ten because
its expected revision/event baseline incorrectly assumed preparation left zero.
Source and raw state established the real `keys_prepared`/revision-1 baseline.
Assertions were corrected to preserve that actual baseline; the corrected run
passed twelve. Expanded combined contracts passed 24, then the four real-kill
and two global-capacity cases passed in separate runs. **The final combined
thirty-case run below was executed after all fixture changes**; the earlier
24+4+2 results are not relabeled as that final run.

Initial full format verification found five indentation errors in the new
ticket factory. The existing formatter corrected only that file; full build
and format verification then passed with zero warnings/errors.

### Exact checkpoint verification

Commands select the existing isolated SDK 10.0.402/runtime 10.0.13; both-provider
commands opt in with `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`. No package,
lock, migration, installation or restore change was needed.

| Exact executed command | Result |
| --- | --- |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter FullyQualifiedName~LoginCommitContractTests --nologo` | Initial missing-implementation compile red; initial actual run **2 passed / 10 failed** for the incorrect preparation baseline; corrected **12 passed** before crash expansion. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~LoginCommitContractTests\|FullyQualifiedName~SessionPersistenceContractTests' --nologo` | Earlier combined **24 passed**, before crash/capacity expansion. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter FullyQualifiedName~KillingTheActualNativeCallbackOwner --nologo` | **4 passed**, actual process kills on both providers. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter FullyQualifiedName~InstallationLimitRefusesTheNextSession --nologo` | **2 passed**, actual 50,000/50,001 limit and corrected login after expiry. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~LoginCommitContractTests\|FullyQualifiedName~SessionPersistenceContractTests' --nologo` | **Final 30 passed**, zero failures/skips, 5m33s, after all current source/fixture changes. |
| `dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~IdentityAdmissionTests\|FullyQualifiedName~IdentityDeadlineTests\|FullyQualifiedName~IdentityEventTests\|FullyQualifiedName~IdentityMappingTests\|FullyQualifiedName~IdentityAttemptTests' --nologo` | **92 passed**, both providers, zero failures/skips, 5m09s. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~OidcIssuerTests\|FullyQualifiedName~ActualNativeProtectedStateRetainsOnlyTheLocalAttemptAndBrowserBinding\|FullyQualifiedName~NativeHandlerValidatesRealCodePkceAndNonceBeforeTheLocalTicketSeam' --nologo` | **4 passed**, zero failures/skips, after shared TLS-fixture changes. |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Passed after all current source additions, zero warnings/errors. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet` | Passed after the targeted formatter correction. |
| `python3 scripts/check_repository.py` | Passed for this checkpoint. |
| `python3 -m unittest discover -s scripts/tests -v` | **121 passed** for this checkpoint. |
| `git diff --check` | Passed for this checkpoint. |
| `npm run format:check` | Passed for this checkpoint. |

### Head-specific CI and remaining work

Published attempts head `591d025` subsequently completed with **all ten product
jobs and repository checks passing**: product run `40451670282`, repository run
`40451670291`. This is not a CI result for the new session/bridge work.
Earlier OIDC head `c8c01c3` product run `40450557540` finished failed:
the PostgreSQL failure-propagation baseline reached its original fifteen-minute
`failure_exercise_timeout`, and `composition-public-contracts` reached its
original fifteen-minute `command_timeout` after platform contracts passed.
Eight other product jobs passed. The underlying causes remain unestablished;
the later green head does not diagnose or erase those failures.

T031 still owes actual product browser-context/login/callback route registration,
CSRF, browser continuity, admission and safe completion. T032 still owes `/me`,
trusted actor projection and generated contracts. The owned bridge host is not
those delivered routes, a module actor consumer, browser UI, full three-restart
T038 evidence, enrollment, sign-out or identity recovery. Continue the same
running stage and original draft PR, without accepting or advancing it.

## T031/T032: registered backend login, self identity and trusted actor

The original **005 / implement** stage continues on its original branch and
draft PR #8. **T001–T032 are complete (32/100); 68 tasks remain unchecked.**
This increment delivers backend endpoints, not the web adapters/UI or complete
US1 journey. No later stage or feature acceptance is inferred.

### Actual delivered behavior

Both empty and composed hosts now register the same maintained native
cookie/OIDC services and exact browser-context, POST login, GET callback and
self-only `/me` endpoints. OIDC is registered only in configured OIDC mode;
the callback has a real exact mapped endpoint so pre-authentication routing
cannot reject it as unknown or broadly open unrelated paths. Only native
post-protocol `OnTicketReceived` invokes the already tested one-owner-commit
coordinator. Deliberate challenge/completion use 303, never an API challenge.
Startup remains offline/read-only: no discovery or implicit preparation.

Browser-context uses framework antiforgery plus a private protected random
browser cookie, mode-specific names/purposes and a five-minute bounded
continuity payload. Login requires exact Origin and a valid framework pair,
accepts only the two form fields, rejects duplicates/unsafe returns and defaults
an omitted return to `/identity`. Only `/` and `/identity` are accepted.
Callbacks additionally require the independent browser digest in protected
native state and the durable pending attempt; native correlation/nonce alone
are insufficient. No raw binding/reference/provider evidence is persisted.
After login, a newly fetched token is bound to the authenticated local identity;
the prior anonymous token cannot authorize a new signed-in mutation.

Shared bounded admission uses installation, browser/session and actual transport
peer digests, never forwarded identity/peer headers. Native callback admission
precedes protocol egress. Cookie input is bounded to sixteen admitted auth
cookies, with duplicate admitted names refused; header/body/target bounds remain.
Kestrel reserves HTTP framing outside the exact sixteen-KiB callback-target
limit, so at-limit requests reach the boundary and one-over requests receive
the safe 400. Identity responses/redirects restore exact no-store/no-cache/
no-referrer headers at response start, including framework-generated responses.
Storage/protection/provider failures remain safe 503 rather than anonymous
success or raw provider output; invalid/replayed binding remains safe 401.

Native authentication must be registered on the **root pipeline**, after the
request/binding/budget middleware. An initial conditional branch caused the
framework to automatically insert native authentication earlier, so the real
callback bypassed the intended request-local budget seam and returned 500.
Restoring explicit root registration fixed the ordering, without an internal
framework flag or an authorization bypass. The cookie manager suppresses
retrieval on health/OpenAPI and new-session publication paths, not POST login:
login must authenticate an existing identity for its current CSRF token, while
callback sign-in must not renew/reuse the old session reference.
Health remains anonymous and does not consult the session store.

Every successful cookie authentication has already verified fresh durable
session/user/mode/epoch/designation. Only that private verified view populates
the read-only request-scoped actor. `/me` requires that actor and returns exactly
version, canonical local user ID, nullable safe display, mode and designation.
Display JSON uses the explicit safe encoder. Fresh profile/designation changes
appear without reissuing a cookie; revocation removes both API identity and
actor on the next request, while corrupt protection returns 503.
A designated administrator still receives 403 for an unrelated ungranted
fixture operation. Fixture profile/designation seeding is **not enrollment
ceremony/endpoint evidence**.

### Shared generated contract and independent consumers

Development OpenAPI now includes the configured mode's delivered identity
operations and explicit self-cookie security alongside unchanged bootstrap-v1
health. All documentation methods remain absent outside Development.
The checked-in Development contract is captured from the actual owned runtime,
normalizing only its unpredictable listener and equivalent empty root security.
The original spec 001 contract remains untouched. `contract:check` now verifies
every original operation/component and version before checking generated types;
identity additions do not require replacing or weakening health definitions.
Object equality is semantic, not JSON property insertion order.
The native parity test independently checks original health definitions,
required self fields and cookie security.

Existing generation produces the shared `CurrentIdentityV1` type. Its separate
runtime guard refuses missing/malformed required fields, zero/noncanonical IDs,
invalid modes/booleans and unsafe/overlong Unicode display values. It admits
additive future response fields without manufacturing defaults. Package changes
are exports/test/typecheck wiring only; dependency versions and locks are unchanged.

Actual capture used the existing `scripts/verify.py` ownership/build/runtime
helpers, with SDK selection as in earlier sections:

```python
import json
import os
from pathlib import Path
from urllib.request import urlopen
import verify
root = Path.cwd()
report = verify.Report(root, 'identity-contract-capture', 'sqlite', 'targeted')
runner = verify.Runner(root, report, dict(os.environ))
try:
    with verify.runtime(runner, 'sqlite', False) as session:
        with urlopen(session.environment['KOFTE_HOST_URL'] + '/openapi/v1.json', timeout=6) as response:
            document = json.load(response)
        document['servers'] = [{'url': 'http://127.0.0.1:5080'}]
        document.setdefault('security', [])
        destination = root / 'packages/bootstrap-contract/openapi.json'
        destination.write_text(json.dumps(document, indent=2, ensure_ascii=False) + '\n')
        print('Captured actual Development health/identity OpenAPI; only owned listener normalized.')
    report.finish(True)
except BaseException:
    report.finish(False)
    raise
```

Executed through `PYTHONPATH=scripts python3` with that exact standard-input
body. All twelve named build/preparation/owned-host/web/start/stop/output-scan
steps passed; no helper remained running or private runtime root was retained.
The actual host and web builds passed, but this capture is not an identity UI test.

### Preserved red/failed results and corrections

The endpoint contract initially failed compilation because product-fixture APIs
did not exist. Fixture extraction initially misplaced a catch block; normal
syntax/format correction restored a zero-warning build. The first actual
twelve endpoint cases passed four and failed eight: framework antiforgery changed
the cache header, and conditional native middleware ran before the explicit
boundary. Exact response-start headers and root authentication corrected both;
expanded sixteen endpoint cases passed, then the combined gate below included
the two exact target-limit cases as well.

The current-user test first returned actual 404 instead of required 401 before
the endpoint existed. The initial typed route hit ASP0016/request-delegate
inference; a real read-only actor dependency made it a typed route handler
without suppression. OpenAPI collection typing was corrected normally.
An expanded twenty-case login/current-user run passed eighteen and failed two
because default HTTP JSON did not HTML-escape display text. An explicit safe
current-user encoder fixed the behavior; the four current-user cases then passed.

The shared TypeScript guard initially failed to import its absent implementation.
Initial contract checking after real capture also failed because native property
insertion order differed, despite Python structural comparison showing every
old operation was identical. Semantic deep equality corrected the checker;
every old operation/component is still required verbatim in meaning.
Generated types, consumer tests, lint and type checking then passed.

### Exact checkpoint verification

All .NET commands use the existing isolated SDK 10.0.402/runtime 10.0.13.
Both-provider commands set `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true`.
No install or restore occurred.

| Exact executed command | Result |
| --- | --- |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter FullyQualifiedName~LoginEndpointContractTests --nologo` | Initial missing-fixture compile red; initial **4 passed / 8 failed**, followed by corrected expanded **16 passed**. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter FullyQualifiedName~SqliteCurrentIdentityContractTests --nologo` | Initial actual 404/401 assertion red before endpoint implementation. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~LoginCommitContractTests\|FullyQualifiedName~SessionPersistenceContractTests\|FullyQualifiedName~LoginEndpointContractTests\|FullyQualifiedName~CurrentIdentityContractTests' --nologo` | **52 passed**, both providers, zero failures/skips, 9m23s. Includes eighteen endpoint, four current-identity and thirty earlier bridge/session cases. |
| `dotnet test tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj -c Release --no-restore --filter FullyQualifiedName~CurrentIdentityContractTests --nologo` | **Final 4 passed**, after adding the actual designated-but-ungranted 403 assertion; earlier 52-case execution is not relabeled as including that last assertion. |
| `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --filter 'FullyQualifiedName~OpenApiTests\|FullyQualifiedName~IdentityPolicyTests\|FullyQualifiedName~IdentityStartupTests\|FullyQualifiedName~OidcIssuerTests\|FullyQualifiedName~ActualNativeProtectedStateRetainsOnlyTheLocalAttemptAndBrowserBinding\|FullyQualifiedName~NativeHandlerValidatesRealCodePkceAndNonceBeforeTheLocalTicketSeam' --nologo` | **21 passed**, zero failures/skips, actual empty/composed startup on both providers and original issuer/native state regressions. |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Final passed, zero warnings/errors. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet` | Final passed after existing targeted format correction. |
| `npm exec --workspace packages/bootstrap-contract -- vitest run src/identity.test.ts` | Initial missing-implementation import red. |
| `npm run generate --workspace packages/bootstrap-contract` | Passed from captured actual OpenAPI. |
| `npm run contract:check` | Passed after semantic equality correction; all accepted bootstrap operations/components and generated types match. |
| `npm run test:unit --workspace packages/bootstrap-contract` | **4 passed** in two files, including original status tests. |
| `npm run lint --workspace packages/bootstrap-contract` | Passed. |
| `npm run typecheck --workspace packages/bootstrap-contract` | Passed. |
| `npm run lint --workspace apps/web` | Passed. |
| `npm run typecheck --workspace apps/web` | Passed. |
| `npm run format:check` | Passed. |

### CI limitation and remaining scope

The new checkpoint also passed `python3 scripts/check_repository.py`,
`python3 -m unittest discover -s scripts/tests -v` (**121 passed**) and
`git diff --check`. These were executed after its source/docs changes, not
reused from the previous checkpoint.

Published session head `ad21b2b` repository checks passed, but product run
`40455783090` finished **failed**: eight product jobs passed; contract and
failure-propagation failed. Completed job logs were obtained directly from the
GitHub job-logs API while recovery was still running, without printing raw
terminal escapes. `platform-public-contracts` passed in approximately five
minutes before `composition-public-contracts` reached its unchanged fifteen-minute
`command_timeout`. The PostgreSQL failure-propagation baseline reached its
unchanged fifteen-minute `failure_exercise_timeout`, before mutations.
These are not passing new-head checks; underlying causes are unestablished.
Earlier head `591d025` remains an independently observed fully green run,
not a diagnosis or waiver. A bounded current-source execution of the exact
composition selector subsequently passed: **588.596 seconds**, unchanged
900-second limit and minimum expected count of 84, on the local macOS profile.
This is not a reproduction or diagnosis of the failed Linux CI environment.

Next work is T033–T040: fixed same-origin adapters, accessible browser identity,
delivered protocol/deadline/refusal evidence, actual three-restart/API/consumer
and both-provider browser comparison. Enrollment, sign-out, development sessions,
identity recovery and final complete gates remain later tasks. No complete
US1 story, browser identity journey, feature acceptance or merge is claimed.

## T033 and partial T034: bounded Next adapters and runtime verification blocker

The original **005 / implement** stage is now **blocked**, not complete.
**T001–T033 are complete (33/100); 67 tasks remain unchecked.** T034's candidate
source is preserved, but its marker stays unchecked because actual host/Next
parity could not start. No reviewer checklist, approved intent, migration,
dependency/lock file or later stage changed.

### Implemented and focused verification

T033 supplies independently frozen transport expectations, without importing
the production parser. The actual four Next route exports call only fixed
browser-context/login/callback/me destinations. Tests verify local unsupported
credentials and mixed cookie refusal, fixed methods/destinations, stripping
Host/forwarding/identity/unrelated-cookie headers, both cookie namespaces/counts,
individual Set-Cookie with an Expires comma, invalid cookie attributes, manual
redirect destinations, bounded malformed responses, no reflected upstream
errors/extra fields, escaped display, eight-KiB streamed request boundaries,
sixteen-KiB header boundaries and a stalled lost mutation response with no retry.
OIDC-only route absence precedes method/credential handling in development.

Candidate T034 source uses the existing fixed loopback backend derivation.
Node-only routing configuration reads at most 32 KiB from the canonical private
owner file with no-follow/nonblocking open and a verified regular descriptor.
Only public origin/mode/authorization endpoint are projected; client-secret
and key references are not opened. Duplicate/escaped-duplicate configuration
keys, symlinks/public permissions, invalid origins/endpoints and accidental
development mode are independently tested. Host validation/authentication
remains authoritative; this public routing subset is not a second authenticator.

The 4.5-second total adapter deadline covers configuration, body, fetch and
response reading. Both bodies and headers/cookies are byte-bounded, response
cookies remain separate, only configured challenge or exact local completion
redirects are admitted, and all responses are private. Known safe host failures
retain their fixed code/correlation; malformed/lost mutation replies return
uncertain, never a successful session/sign-out/enrollment. No health adapter
behavior changes, dependency, arbitrary proxy or identity UI was introduced.

The first executed adapter command failed module import because the production
implementation did not exist; **zero tests ran**, not a fabricated assertion
red. The first implementation then passed **27 adapter tests**. Lint caught a
missing preserved error cause in configuration error translation; the normal
typed `cause` was retained and remains absent from safe public logs/responses.
Additional boundary/mode/real-file/actual-export cases brought the final focused
source to **41 adapter + 10 configuration tests**, within **78 web unit tests**.
All units, lint, type checking and direct Next production builds pass. Builds
list the four intended dynamic routes alongside unchanged readiness/home routes.
These are not real browser/native-login or live adapter-parity observations.

| Exact executed command | Result |
| --- | --- |
| `npm run test:unit --workspace apps/web -- features/identity/identityAdapter.test.ts` | Initial missing-implementation import failure; zero tests ran. |
| `npm exec -- prettier --write apps/web/features/identity apps/web/app/auth apps/web/app/api/identity/v1` | Passed; existing formatter applied only to new adapter files. |
| `npm run test:unit --workspace apps/web` | Final **78 passed** in five files, including all 51 new adapter/configuration cases and retained bootstrap/readiness tests. |
| `npm run lint --workspace apps/web` | Initial preserved-cause finding corrected; final passed. |
| `npm run typecheck --workspace apps/web` | Passed. |
| `npm run build --workspace apps/web` | Direct builds passed; all four dynamic identity routes compile. |
| `npm run format:check` | Passed. |
| `python3 scripts/check_repository.py` | Passed with the blocked ledger and current docs. |
| `python3 -m unittest discover -s scripts/tests -v` | **121 passed** for this checkpoint. |
| `git diff --check` | Passed. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-class '*Composition*' --minimum-expected-tests 84` | Passed through the unchanged owned `verify.Runner`/private XML diagnostics: **588.596s**, original **900s** limit. |

### Actual failed owned-runtime probe: not waived

Two ordinary `verify.Report`/`Runner`/`runtime` SQLite probes attempted actual
prepared host/Next status/header/cookie parity. In both, full host build passed
and the Next build process **exited 0**, but `runtime-web-build` failed with
`verification_input_invalid` during cleanup. Hosting/preparation had not begun:
**none of the sixteen planned HTTP parity observations ran**. Both failed
reports remain under ignored verification artifacts; no live parity or browser
success is claimed.

A separate bounded execution through `verify.OwnedProcesses` reproduced the
post-success exception as **`PermissionError`, errno 1 (`EPERM`) at
`verify.py:450`, `OwnedProcesses.stop`, `os.killpg`**. Only exception type/errno
and source frames were exposed, not raw private output. A no-descendant `true`
child cleaned up normally. Another independently owned Next build also exited
0 and, after checking its exact group had no members, cleaned up successfully.
That later success does not explain or erase the earlier failed cleanups;
an unchanged full runtime retry still failed at the same build-cleanup phase.
No cleanup error was ignored, no owned-process/deadline assertion changed,
and no generic PermissionError catch was added to the existing supervisor.

The driver helper records the actionable blocker: resolve ownership-safe
post-build cleanup without waiving live-descendant checks, rerun the actual
adapter parity probe, finish T034 and then continue T035–T100. The original
draft PR #8 remains the publication checkpoint; no feature acceptance, ready
status, intermediate merge or stage completion is implied.

At the last observation, published route head `f3ef060` had passing repository
checks (`40460031671`) and eight passing product jobs, including PostgreSQL
integration and failure propagation; contract and recovery were still running
in product run `40460031691`. This is not a CI result for the adapter checkpoint,
a diagnosis of the prior timeouts or a completed product matrix.

## T034–T036: verified live adapters and accessible identity entry

The original implement stage was resumed through the driver helper on the same
branch and draft PR. T001–T036 are now complete (36/100); T037–T100 remain
unchecked. This is continuing implementation, not a feature/stage handoff.
Published `f89c1ec` has all eleven CI contexts passing; that observation is not
CI evidence for these newer working-tree changes.

The owned parity probe reproduced the post-success Next-build `EPERM`.
Inspection of that exact group found no members, but the OS timing/cause is
not established. The narrow repair accepts SIGTERM `PermissionError` only
after the leader has exited **and** a signal-zero kernel probe reports the
entire group absent. A live or unverifiable group still fails. Output readers,
budgets, ownership and output scanning are unchanged.

- Initial red: the two new `VerifyTests` selections for absent versus
  present/unverifiable groups produced one error and two subtest failures.
  `python3 -m unittest scripts.tests.test_verify -v` then passed **51 tests**.
- With the normal build and repaired cleanup, the session-owned
  `identity-adapter-runtime.py` passed all thirteen named steps, including
  **sixteen actual host/Next HTTP observations**, cleanup and output scan.
  This owned SQLite/Development probe is adapter parity, not production
  OIDC browser-login evidence.

T035/T036 deliver `/identity` with the existing design primitives, a
non-personal dynamic/no-store shell, parallel bounded fresh context/current-user
checks, an in-memory CSRF token, deliberate native sign-in and only the
reviewed `/` or `/identity` returns. Identity is cleared on failed verification,
expiry, blur/hidden or refresh; returning revalidates it. No session material
is put in JavaScript, storage, SSR or analytics. Fixed callback notices and
validated correlation references never render provider/detail text.
Development is labeled but selection remains T074. Enrollment and sign-out
remain their later tasks.

- Initial UI red was a missing implementation import; no tests ran. Eleven
  state/render cases first passed. A separate safe-reference regression then
  genuinely failed before the validated reference projection was added.
- `npm run test:unit --workspace apps/web` passed **91 tests in six files**,
  including thirteen new identity state/render tests.
- `npm run lint --workspace apps/web`, `npm run typecheck --workspace apps/web`,
  `npm run build --workspace apps/web`, `npm run lint --workspace tests/e2e`,
  `npm run typecheck --workspace tests/e2e` and `npm run format:check` passed.
- `python3 scripts/verify.py --suite browser --provider sqlite` was refused
  by argument validation (exit 2): browser has a fixed provider profile.
  The corrected **`python3 scripts/verify.py --suite browser` passed** all
  nineteen named runtime/build/test/cleanup/scanning steps. Its actual
  discovery includes `identity.spec.ts`: the three new tests observed the
  private non-personal Development shell, real owned host loss/retry within
  five seconds, and 320px/200% reflow, visible keyboard focus and no
  serious/critical axe findings. Existing bootstrap/browser tests also ran.
- `python3 scripts/check_repository.py`,
  `python3 -m unittest discover -s scripts/tests -v` (**123 tests**) and
  `git diff --check` passed before the subsequent .NET test-fixture additions.

The first delivered both-provider `OidcBudgetTests` execution completed with
**52 total, 50 failed, 2 passed, 0 skipped**, in 9m53.783s. All fifty failures
were the test's incorrect `Assert.Empty(events)` baseline: legitimate stopped
preparation already committed `keys_prepared`. The source now compares exact
pre-login event snapshots instead of deleting/ignoring that event. The
corrected matrix and affected crash-commit regressions are executing; neither
T037 nor T038 is claimed complete. The new restart/reconciliation fixture and
tests compiled with `dotnet build Kofte.slnx -c Release --no-restore
-warnaserror --nologo` (zero warnings/errors), but build alone is not their
required real-provider/process evidence. The newer public-recipe source is
also still awaiting its first execution.

## T037–T039 continuation: real Origin correction and unresolved browser lifecycle

The original implement stage continues on its original branch/draft PR.
T001–T036 remain checked (36/100). No browser recipe or later story is claimed
complete, and neither reviewed checklist was changed.

The corrected delivered budget cases exposed a real transport classification
bug: `SocketsHttpHandler` wraps deliberate DNS/network policy refusal in
`HttpRequestException`. The backchannel now preserves that inner policy
classification instead of converting it to provider unavailability. The
optional internal resolver seam defaults to BCL DNS; address/connected-peer
allowlists, cancellation and all original budgets remain intact. Browser-rate
cases now use distinct subjects so the independent eight-session user cap
does not mask the ten-login browser limit. The earlier combined 123-case run
failed six cases; the subsequent targeted run passed 29/30, with a PostgreSQL
browser-context uncertainty. Its unchanged isolated rate case then passed.
A fresh corrected combined protocol/backchannel/budget execution is running;
these partial results are not a full green matrix.

T038's real mapping/restart execution passed twenty cases and sixteen
affected atomic/crash regressions in the preceding continuation. The fixture
uses three fresh actual product-host processes on the retained port, and
stopped provider reconciliation replaces the owned issuer/configuration and
starts a fresh host. This evidence is not substituted for the unfinished
browser recipe or current combined gate.

The first actual SQLite `oidc-login` recipe returned
`identity_browser_opaque_origin_refused`. An owned-ingress boolean observed
the browser's actual `Origin: null` on native POST `/auth/login` under
required `Referrer-Policy: no-referrer`. Null Origin remains refused. The
correction is a Next Server Action in `startLoginAction.ts`, invoked by the
sign-in form's explicit client handler. It admits exactly the two bounded
inputs, forwards actual Origin/credential/Fetch Metadata evidence through the
existing fixed-backend adapter, retains explicit framework antiforgery and
the adapter's 4.5-second limit, publishes validated separate cookies through
typed Next APIs and redirects only after that publication. The framework
redirect is outside catches. The owned HTTPS ingress preserves public Host
for Next's own Origin/Host verification; this never selects the backend.
Pending, refusal and uncertain outcomes are visible, with no automatic
mutation retry or duplicate submission. No authentication contract,
authorization authority, privacy header or TLS/CSRF policy is relaxed.

- `npm run test:unit --workspace apps/web` passed **98 tests in seven files**,
  including the five new action cases and two added submission-state cases.
- `npm run typecheck --workspace apps/web`,
  `npm run lint --workspace apps/web` and
  `npm run build --workspace apps/web` passed. The two web quality commands
  and all 98 units were re-run successfully after the transport edits.
- `npm run typecheck --workspace tests/e2e`,
  `npm run lint --workspace tests/e2e` and `npm run format:check` passed
  again after the read-only cleanup-verification refinement.
- `dotnet build Kofte.slnx -c Release --no-restore -warnaserror --nologo`
  passed again on the final candidate with **zero warnings/errors**.
- `python3 scripts/check_repository.py`,
  `python3 -m unittest discover -s scripts/tests -v` (**123 tests**) and
  `git diff --check` passed before this documentation append.
  `dotnet format Kofte.slnx --verify-no-changes --no-restore --verbosity quiet`
  initially reported two whitespace findings in changed fixture code.
  A scoped `dotnet format whitespace` correction and full verification passed.
  Full formatting verification, the repository checker and whitespace check
  passed again after the final fixture/documentation edits.

After the Origin correction, real local HTTPS runs reached product
login/callback/current-user, and some reached every safe-return assertion.
**Every complete public recipe still failed.** A runner-only explanation was
initially suspected but is not established: later standalone/native runs also
hung or failed during browser work/closure. A native bounded browser script
now retains the actual assertions and emits only fixed stages/codes/source
locations, never raw browser errors, URLs, cookies or provider payloads.

The speculative force-group/Python-probe supervisor was removed. The fixture
uses normal Playwright close plus read-only, CDP-bound exact-group absence
verification. Present groups and errors other than kernel `ESRCH` fail; no
`EPERM`, `?E` state or elapsed timeout is treated as absence. A real local
owned group retained a PPID-1 `?E` member and both Node/Python signal-zero
probes returned `EPERM`; its cause/quiescence is not established. No cleanup
success is claimed for that group. Failed/unconfirmed browser completion
retains the owned private configuration/profile rather than deleting it.
The distinct earlier Next-build repair still requires an exited leader and
actual kernel absence.

Instead of more unbounded local hypotheses, the existing Ubuntu browser job
now prepares the already-pinned disposable PostgreSQL image and executes:

```bash
dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario oidc-login --provider sqlite
dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario trust-refusal --provider sqlite
dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario oidc-login --provider postgres
dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario trust-refusal --provider postgres
```

These are **new pending executions**, not passing CI or T039/T093 completion.
The job keeps ordinary certificate verification and imports only the owned
CA into its fresh profile; no OS trust, personal profile, certificate-ignore
switch or private artifact upload is introduced. All eleven checks on
published `f89c1ec` passed, but are not evidence for this newer source.

## `6be0ee8` reconciliation: real browser recipes pass; host CI still fails

The verified entry/transport and test candidates were committed and immediately
pushed as `6be0ee8` on the original draft PR #8. No stage transition,
acceptance or merge occurred.

- The complete corrected local command
  `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --no-build --filter 'FullyQualifiedName~OidcBudgetTests|FullyQualifiedName~OidcProtocolTests|FullyQualifiedName~OidcBackchannelTests' --nologo`
  passed **123/123, zero skips**, in **20m08.394s**. Earlier failures are retained.
- Actual [Ubuntu browser job 122614125497](https://github.com/Stonefly-Labs/Kofte/actions/runs/40626971026/job/122614125497)
  passed both named browser verification and the added HTTPS recipe step.
  The completed job's four JSON result records were independently retrieved
  and checked for the exact provider/scenario inventory, passed status and
  distinct named assertion count. `oidc-login` passed **eight observations**
  and `trust-refusal` **eleven observations** on **each** store. These are
  actual direct API/actor/native-handler/Next/browser/TLS/cleanup executions,
  not fabricated principals or an empty summary.
- `dotnet test tests/integration/Kofte.Host.IntegrationTests/Kofte.Host.IntegrationTests.csproj -c Release --no-restore --no-build --filter 'FullyQualifiedName~IdentityPolicyTests' --nologo`
  passed **4/4**, zero skips, in **33.349s**: safe health/missing auth,
  default denial, exact Origin/framework pair and callback binding remain.
- The fresh mapping command
  `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test tests/integration/Kofte.Storage.IntegrationTests/Kofte.Storage.IntegrationTests.csproj -c Release --no-restore --no-build --filter 'FullyQualifiedName~IdentityMappingTests' --nologo`
  initially passed **19/20**, with SQLite's delivered restart case timing out
  at worker readiness. A separate policy selection overlapped that run;
  causation is **not established**. Safe last-status/transport classification
  was added without changing the existing fifteen-second budget. The exact
  isolated SQLite restart selector then passed **1/1** in **27.790s**.
  The entire twenty-case command, run alone on the current fixture, then
  passed **20/20**, zero skips, in **1m50.205s**. This verifies T038, including
  distinct process IDs/profile/issuer preservation, but does not diagnose or
  waive the prior failed run.

Current markers are T001–T036 plus T038 (**37/100**). T037's local matrix is
green, and actual T039 recipe evidence exists, but their final reconciliation
is held while current-head PostgreSQL host integration CI fails. The safe
report identifies only `integration-postgres`, exit 2, after 245.211s; it does
not identify the failing test. No cause is invented. Other observed passed
contexts include repository, quality, unit, SQLite integration, browser,
privacy and startup; the complete run is not yet green.

The existing source-whitelisted private XML diagnostic path previously
covered storage and public-contract tests but **not host integration**.
It now includes host integration with the same bounded parsing, private-file
cleanup and no raw argument/exception/output retention. The new focused
regression first failed because the host diagnostic was `None`, then passed
after wiring; complete Python discovery passed **124 tests**. The real native
runner was also exercised through `Runner.command` with
`dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*IdentityPolicyTests' --minimum-expected-tests 4`;
it passed with private XML enabled. Full .NET formatting verification,
repository and whitespace checks passed. These diagnostics are to establish
the CI cause, not a claimed repair of the PostgreSQL failure.

### Isolated diagnostic propagation and completed predecessor results

The host diagnostic wiring was committed and immediately pushed as `2f56f35`.
Its repository checks passed, and its browser, startup, quality and unit jobs
passed when observed; the PostgreSQL suite and other longer jobs were still
running. Results from `6be0ee8` are not attributed to this head.

The completed `6be0ee8` product run additionally failed
`composition-public-contracts` at its unchanged **900.044-second** command
limit and the isolated `baseline-postgres` at **900.371 seconds**. The latter
raised `failure_exercise_timeout`; no completed nested report identified the
unfinished operation. The preceding `f89c1ec` product run passed every job.
These failures remain undiagnosed; no product, command or harness deadline
is relaxed, and no failing baseline is accepted as propagation evidence.

Inspection found a directly dependent reporting gap: the isolated harness
still revalidated only storage/contract diagnostics and silently omitted the
new safe host fields. A new focused regression first failed with
`KeyError: failedHostTests`; admitting the same source-known host kind fixes
that omission. It also rejects argument-bearing/duplicate test names,
unknown codes and diagnostics attached to an unrelated command, retaining no
raw canary. `python3 -m unittest discover -s scripts/tests -v` then passed
**125 tests** at that intermediate edit. This repairs reporting only, not the
underlying CI failures; the implement stage remains unfinished.

## T037–T040: US1 reconciled at `2f56f35`

The fresh [product run 40628696184](https://github.com/Stonefly-Labs/Kofte/actions/runs/40628696184)
passed both complete provider integration jobs and browser, privacy, startup,
quality and unit. Repository checks also passed. PostgreSQL's native host,
storage and composition commands passed in **241.783s**, **618.981s** and
**290.846s**, respectively, with their original per-command limits. This is
fresh passing evidence, **not a diagnosis** of `6be0ee8`'s failed host case.

The completed [browser job 122619718900](https://github.com/Stonefly-Labs/Kofte/actions/runs/40628696184/job/122619718900)
again ran all four exact public commands listed above. Its actual JSON records
were retrieved and independently checked for schema/status, exact provider/
scenario inventory and unique named observation counts: **eight login** and
**eleven trust-refusal** observations on **each** store. No raw job output,
credential or browser profile is published.

| Requirement | Actual scoped evidence |
| --- | --- |
| US1.1 | Both actual HTTPS login recipes compare browser UI, native `/me` and separately compiled actor consumer against the same internal user. Ordinary login grants no designation. |
| US1.2 | The current twenty-case mapping matrix verifies exact IDs/designation/profile through three distinct product process restarts per store. |
| US1.3 | That matrix covers equal profiles/distinct exact subjects and sequential explicitly reconciled issuers without identity merging or extra grants. |
| US1.4 | The corrected 123-case local protocol/backchannel/budget gate and both actual trust-refusal recipes verify invalid/replayed/unsolicited evidence, no unauthorized effects and fresh corrected login. |
| US1.5 | Native/configuration/startup and delivered budget cases verify outage/cancellation/configuration refusal, safe retry and no development/anonymous fallback. |
| SC-001, partial | Production sign-in, exact identity and mapping/restart are evidenced; sign-out is still US3 work. |
| SC-003, scoped | Tested missing/mixed credentials, invalid trust, origin/CSRF and default-denied access refuse before effects; later enrollment/expiry/parity cases remain. |
| SC-005, preliminary | The Ubuntu browser suite executes 320px/200% shell, keyboard/focus/axe and actual host-loss actionable failure under five seconds. Production recipes assert signed-in state within five seconds of submission, including the synthetic provider exchange; this is not a separate provider-return measurement or evidence for future UI states. |

Safe health/missing-auth remain verified by the recorded fresh four-case policy
selection and repeated actual recipe observations. T037, T039 and T040 are now
checked, giving **40/100**; enrollment is next within the same implement stage.
The read-only review checklists and approved design remain unchanged.

The complete current run is **not green**. Composition public contracts again
hit the unchanged 900-second limit (**900.077s**). PostgreSQL composition
recovery failed `PublicFaultMatrixVerifiesActualExitBudgetsPartialCleanupAndOwnedDescendants`,
with safe `composition_consumer_failed` / `bounded-stop` evidence. Neither
cause is established or waived.

The current failure-propagation failure **is diagnosed**: the unavailable-store
native command correctly failed, but the strict evidence comparison rejected
the newly appended private XML-report arguments. A focused regression first
reproduced `failure_did_not_reach_expected_command`; the correction admits only
the exact bounded reporting suffix, retaining rejection of wrong provider,
filename or option and all original step/exit/correlation checks. The actual
published nested report now replays successfully as the intended failure.
Full Python discovery passes **126 tests**. Its three real baselines passed,
including PostgreSQL in **895.064s**; the earlier timeout is not erased.
No product or verification deadline changed, and a fresh complete CI run is
still required. The local macOS cleanup and intervening readiness failures
also remain explicit limitations, not a full-feature acceptance.

## US2: nominated enrollment and real interruption evidence

The same **005 / implement / running** stage, branch and draft PR #8 continue.
Only T042, T049, T050 and T052 are newly checked: their required both-provider
authority/file, twenty-contender and real child-interruption observations pass.
The other US2 markers remain conservative while the complete delivered-boundary
matrix, unavailable-administrator/module-data and browser evidence are unfinished.
All approved task text/dependencies, reviewer checklists and driver state remain
unchanged. This is **44/100**, not a completed story or feature.

### Delivered behavior

`IdentityBootstrapStore` validates the complete singleton/null-state contract,
performs stopped pending nomination/replacement/disable, and binds the SHA-256
authorization digest to installation, checked generation and exact issuer/subject.
The private operator command generates 32 random bytes, exclusively creates an
owner-only file, durably flushes it before DB commit and retains partial/delivered
evidence after uncertainty. Live, foreign, unadmitted, consumed and
recovery-locked operations cannot reach delivery or reopen authority.

Protected enrollment revalidates the current durable session/epoch inside the
owner transaction, compares the persisted exact nominee and digest in constant
time, then commits designation/consumption/revision/immutable event together.
It erases pending token/nominee fields, never takes an actor/role from input and
reports success only after known commit. All twenty HTTP contenders pass real
CSRF/admission: exactly one 200, nineteen 409, with exact designated user,
generation, event correlation and revision correspondence. Three fresh process
restarts and idempotent actual registry upgrade/identity prepare preserve this
consumption; the broader T051 unavailable-administrator/module-data case remains.

The exact Next route preserves the protected exchange and safe receipt, with no
redirect following or mutation retry. The masked form never prefills/persists
authorization, clears it before dispatch, aborts on unmount and suppresses late
results for another identity. Refusal restores entry focus; confirmation/lost
reply triggers fresh `/me` and restores heading focus, without declaring unknown
commit as confirmed. Actual native OpenAPI now includes the strict write-only
43-character request, security/headers, receipt and safe conflict; generated
types preserve original health operations/components.

### Real interruption defect and correction

The four private barriers stop the actual maintenance child after file creation,
after durable flush, before DB commit and after durable commit. Ownership is
proved by the admitted barrier PID, actual PPID and matching process group;
the exact tracked child is killed and both child/supervisor exits are verified.
Independent reopening checks each file/DB side, retained evidence and explicit
replacement. Enrollment barriers similarly kill the exact serving child before
or after durable commit and before acknowledgement: neither-or-one atomic state/
event, no acknowledged success, and safe corrected retry.

The pre-commit SQLite operator interruption genuinely reproduced
`identity_reconciliation_required`: startup's inventory check reused the clean
backup opener, which refuses an inactive SQLite crash journal. Startup now uses
its already-open verified platform connection for the same inventory checks.
Clean backup/recovery readers still open through their original sidecar-refusing
path. No journal deletion, backup format relaxation or time-budget change occurs.
The following full 36-case operator matrix and four-case enrollment crash matrix
pass on both stores; all fifteen retained recovery tests additionally pass.

Private-file effects also exposed an invalid premature ready/uncertain trace.
Two genuine red cases now pass: intermediate phases describe uncertainty without
relaxing terminal ready/uncertain rejection. The final known commit may still
report committed; loss/refusal retains truthful uncertain evidence.

### Exact executed evidence and retained failures

Every native command uses SDK 10.0.402/runtime 10.0.13; both-provider commands
explicitly opt into the disposable fixture. Test commands below use the existing
safe runner's private native capture, not public failure XML/provider payload.

| Exact command / selection | Actual result |
| --- | --- |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Passed, zero warnings/errors, including public/browser recipe wiring. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore` | Passed. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*IdentityNominationTests' --minimum-expected-tests 36` | Final full **36 passed**, both providers. Initial foreign extra-fixture startup, SQLite journal and test-only 200ms PostgreSQL acquisition failures are retained; the final live-lease test uses the original normal two-second acquisition, not an increased deadline. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*EnrollmentCrashTests' --minimum-expected-tests 4` | Full **4 passed**, both providers. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*EnrollmentConcurrencyTests' --minimum-expected-tests 2` | **2 passed**, twenty protected eligible contenders and three fresh restarts per provider. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentTests' --minimum-expected-tests 16` | Full **16 passed**, including actual stopped replacement and ticket/commit verification 503. Earlier full both-store 32-case selection timed out at its unchanged 240s command bound; it did not pass. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*RecoverySetTests' --minimum-expected-tests 15` | **15 passed**, retaining clean-backup sidecar refusal after the startup correction. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*OpenApiTests' --minimum-expected-tests 3 --filter-trait Provider=sqlite` | **3 passed**, actual generation/full baseline and absence outside Development. |
| `dotnet test --project tests/unit/Kofte.Host.Tests --no-restore --no-build -c Release --filter-class '*IdentityDiagnosticsTests' --filter-class '*NominationDeliveryTests' --minimum-expected-tests 7` | **7 passed**. |
| `npm run generate --workspace packages/bootstrap-contract` and `npm run contract:check` | Passed after extracting the actual native document, preserving every original health operation. Initial extraction failed because xUnit escapes the public JSON; prior npm success against the old JSON is not generation proof. |
| `npm run test:unit --workspace apps/web -- features/identity` | **82 passed**. |
| `npm run typecheck --workspace apps/web`, `npm run lint --workspace apps/web`, `npm run build --workspace apps/web` | All passed. |
| `npm run typecheck --workspace tests/e2e`, `npm run lint --workspace tests/e2e` | Both passed. Initial Prettier failure for the new browser source was corrected with the existing formatter. |
| `python3 scripts/check_repository.py`, `python3 -m unittest discover -s scripts/tests -v`, `git diff --check` | All passed; Python discovery **126 tests**. |
| `npm run format:check`, `npm run contract:check` | Final full formatting and generated-contract checks passed. |
| `dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario nominated-enrollment --provider sqlite` | Native-only version passed **15 named observations** after an earlier generic failure with unestablished cause. The subsequently expanded actual-browser version **fails**, ending at fixed `identity_browser_stage_submitted`, before enrollment. |
| `dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario enrollment-race --provider sqlite` | Passed its protected twenty-way native race, exact event/state and three-restart observations. |

The new HTTPS enrollment browser checks deliberately enter a wrong authorization,
then drop acknowledgement only after the actual protected commit. They require
fresh designation/no retry, cleared secret/storage, keyboard/focus, 320px/200%,
axe and original five-second UI bounds. They are wired into the existing Ubuntu
browser job alongside individual both-provider public recipes, but are **not
yet passing evidence**: local browser execution stalls before enrollment.
The other required public scenarios and `all` remain fail-closed/unimplemented.

The unavailable-commit selection passed SQLite's two cases, but both PostgreSQL
cases failed during owned Docker startup, not the enrollment test body.
Read-only inspection and an explicitly unique task-owned network-create probe
established **`docker_network_pool_exhausted`** with a responsive engine,
32 networks and 21 running fixture-labeled containers. Labels alone do not prove
this session owns all resources. The probe created nothing and no unrelated/
global cleanup was attempted. Fresh owned CI is required for the missing matrix;
do not reduce contention, extend deadlines or declare PostgreSQL passed.

For preceding committed `06a9faf`, repository checks pass; product run
[40631199021](https://github.com/Stonefly-Labs/Kofte/actions/runs/40631199021)
passed eight jobs but failed contract at the existing 900s composition-public
bound and SQLite composition privacy with safe cancellation/consumer-failure
codes. Neither cause is diagnosed/fixed by enrollment. Those failures do not
validate current source, and earlier failed/readiness/macOS evidence is retained.

**Remaining:** complete US2 matrix and browser evidence, US3 sign-out/session/
actor parity, US4 actual development selection/isolation, US5 recovery-v2/
quarantine/reconciliation, then full local/CI gates and evidence-based review.
No stage transition, human acceptance, PR readiness or merge is requested.

### T051 continuation: populated module and unavailable administrator

`EnrollmentRestartTests` now uses an owned pre-serving fixture callback to
register/migrate a real module through the accepted stopped registry/binding
APIs and persist a nonempty sentinel. No test module code moves to production.
Twenty protected eligible submissions admit exactly one success and nineteen
conflicts with the populated module present, then three distinct serving processes
and actual idempotent registry upgrade/identity prepare preserve exact module
assignment, row/note, EF history, registry revision, user/mapping and immutable
enrollment record.

The original issuer is actually disposed and explicitly reconciled to a fresh
issuer. The prior session is denied; the replacement issuer's ordinary
authenticated user cannot inherit designation, replay the original authorization
or reopen bootstrap with the actual stopped nomination command. The original
consumed user/generation/event and unrelated module remain intact.

The initial test could not compile: the new owned preparation callback was not
implemented, and two test namespace imports were missing. After those fixes,
`dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` passes
without warnings and the exact command
`dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentRestartTests' --minimum-expected-tests 1`
passes its actual SQLite case. No PostgreSQL execution is claimed while local
Docker fixture capacity is exhausted. T051 remains unchecked until that
counterpart passes; the total remains **44/100**.

Extending the passing single-enrollment case to the required twenty simultaneous
submissions initially failed with the runner's sanitized `command_failed`; its
failure report identifies this test but does not establish a cause. The unchanged
case then passed one private diagnostic execution and three further sequential
isolated executions. These later passes are not a diagnosis or claim that a
timing defect was fixed. No contender count or deadline was reduced.

The pushed `68b483f` repository check passes. Its product run `40640319468`
reports failed host cases for SQLite interrupted preparation, PostgreSQL
twenty-way enrollment and PostgreSQL invalid OIDC evidence. The browser suite
itself passes, but the subsequent configured-HTTPS public-recipe step fails;
the complete run's logs are not yet available. These are retained failures, not
passing evidence for this follow-up, and their cause remains under investigation.

### CI continuation: crash-retry compatibility and independent evidence

The completed `68b483f` product run `40640319468` passes quality, unit, privacy,
startup and recovery, but fails host integration on each provider, configured
HTTPS recipes, composition contracts at the original 900-second deadline and
the PostgreSQL failure-propagation baseline. The repository run passes.
`e1e28c6` publishes the populated-module preservation regression on the same
draft PR; its separate repository run passes, and its product run remains
incomplete. Neither run contains the following corrections.

The SQLite preparation crash test reproducibly failed `Assert.NotEqual` at its
old line 59 because explicit stopped retry now correctly succeeds through the
verified existing connection, instead of treating SQLite's inactive crash
journal as a clean backup. Correcting that obsolete expectation does not relax
backup admission: both actual pre/post-commit interruption cases pass with one
event, revision 1, disabled bootstrap, zero users, format 2 and byte-identical
retained keys. All fifteen clean-recovery regressions still pass.

The hosted browser's ordinary SQLite login and trust-refusal recipes pass.
The nominated-enrollment recipe reaches the real form and fails its wrong-secret
refusal announcement assertion at original source line 237. Locally, an owned
HTTPS observation confirms callback 303, current identity/context 200 and actual
wrong-secret enrollment 409; no raw response, credentials, query, path, nominee
or profile is projected. Other local runs expire during browser setup or sign-in.
The original browser-child 45-second bound is unchanged.

The browser recipe now explicitly restores focus after axe's temporary analysis
page, rechecks the signed-in view and independently asserts the real 409
before its UI assertion. Failed UI assertions project only an allowlisted
missing/uncertain/unexpected/unavailable notice category; diagnostic reporting
does not initiate another browser request. Owned fixture diagnostics expose
only protocol request counts, fixed route-category status integers and whether
the child deadline expired. These changes are not yet a passing browser result
or a diagnosis of the remaining refusal-announcement failure.

The host failure also prevented either independent storage integration project
from running, withholding PostgreSQL preservation evidence. Verification now
attempts all three independent projects after a normal failed/zero-case or timed
out child whose cleanup completed, retaining each failed report and rethrowing
the first failure. Cleanup/ownership/tooling failures still stop immediately.
Two focused Python regressions cover continued execution with a nonzero suite
and immediate stop on cleanup failure; the focused 54-test verifier suite passes.
`python3 scripts/check_repository.py`, the full
`python3 -m unittest discover -s scripts/tests -v` (128 tests),
`dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo`,
`dotnet format Kofte.slnx --verify-no-changes --no-restore`, browser-workspace
typecheck/lint, `npm run format:check` and `git diff --check` all pass.
No test count, original timeout, provider requirement or success condition is
weakened. All task markers remain unchanged at **44/100**.

## US2 continuation: scoped announcements and real lost acknowledgement

The published `d3813dd` SQLite integration artifact `12796593019` reports
**passed** for all three independent projects: host integration (210.543s),
storage integration (310.011s) and composition integration (100.495s).
The earlier host failure no longer prevents collecting storage evidence.
This is an assessed-head result, not evidence for the following unpushed edits.
The same head's PostgreSQL artifact `12796914363` fails host integration only
at the twenty-contender test and storage integration only at the populated
twenty-contender/restart test; composition integration passes. Those are the
two timing-sensitive cases receiving collection isolation below. Their failures
are retained, not counted as PostgreSQL preservation evidence. Other final
gates remain outstanding.

The installed Next.js accessibility guide documents its built-in route
announcer; installed source places its accessible alert in an open shadow root.
Both enrollment alerts now resolve within the named **Your identity** region,
instead of the page-wide alert locator. Actual execution then passed the
wrong-secret announcement, secret clearing and focus assertions and reached
the old `route.fetch` interception at source line 240, where it failed before
a successful enrollment response. No product announcement defect was demonstrated.
The speculative matcher-result classifier and its public error allowlist are
removed; fixed source-line and protocol observations remain.

Acknowledgement loss now happens in the **test-owned HTTPS ingress**, after
the real browser's POST has received the adapter's successful, bounded response.
The ingress consumes that response, drops the connection once and records only
that the intended drop occurred. The owning fixture independently requires
this observation before reporting browser success. This avoids replacing the
browser's trusted transport with Playwright's separate API-request transport;
no certificate bypass, retry or production fault-injection interface is added.
The browser must still observe uncertainty, query fresh designation, submit
exactly twice including the prior refusal, clear secret material and satisfy
keyboard/focus, width/text, axe and five-second UI assertions.

One local execution reached and passed all enrollment assertions and the
subsequent safe-return assertions, recording actual enrollment 200, but exceeded
the original 45-second child limit during final browser cleanup. It is
**not a passing complete recipe**. The dedicated enrollment branch now returns
after its own required journey; duplicate repeated-login/safe-return checks
remain in the separately required ordinary-login recipe. Subsequent local
executions still fail earlier in sign-in or native setup. No total deadline,
TLS trust check, cleanup requirement or enrollment success condition is relaxed.
Fresh hosted proof remains required.

The twenty-contender host tests now share the existing exclusive startup
collection, renamed `identity-deadlines`; populated-module storage tests use
the existing storage deadline collection. Their **twenty simultaneous protected
submissions**, one-second production operations and restart assertions are
unchanged. This removes unrelated test scheduling from their timing evidence,
but is not a diagnosis of the earlier PostgreSQL race failure.

Executed after these changes:

| Exact command | Result |
| --- | --- |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Passed, zero warnings/errors. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentConcurrencyTests' --minimum-expected-tests 1` | Passed through the owned verifier. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentRestartTests' --minimum-expected-tests 1` | Owned-verifier attempt failed; unchanged private xUnit diagnostic rerun passed, 38.5s. No intermittent-failure cause is claimed. |
| `dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario nominated-enrollment --provider sqlite` | Still failing overall: preserved old interception failure, final cleanup timeout after complete assertions, and earlier sign-in/native-setup failures. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore` | Passed. |
| `python3 scripts/check_repository.py` | Passed. |
| `python3 -m unittest discover -s scripts/tests -v` | All 128 passed. |
| `npm run typecheck --workspace tests/e2e`; `npm run lint --workspace tests/e2e`; `npm run format:check`; `git diff --check` | All passed after targeted Prettier correction. |

An initial combined native class selector using `|` failed before executing the
intended selection; the subsequent individual supported selectors above are
the actual evidence. All 44 checked task markers, reviewer-owned checklists and
the implement/running driver state remain unchanged. Continue implementation
on the original branch/draft PR, without a feature-completion or merge claim.

## US2 verified browser/preservation and remaining negative evidence

Assessed correction head `a0c1fb030ba4c94a7d3a5f817c4d08d901e309d2`
has a **passing complete Ubuntu browser job** in product run
`40645635458`, job `122673679859`. All eight actual HTTPS recipe commands pass:

| Exact consumer scenario arguments | SQLite | PostgreSQL |
| --- | --- | --- |
| `--scenario oidc-login --provider PROVIDER` | Passed, 8 named observations. | Passed, 8 named observations. |
| `--scenario trust-refusal --provider PROVIDER` | Passed, 11 named observations. | Passed, 11 named observations. |
| `--scenario nominated-enrollment --provider PROVIDER` | Passed, 17 named observations. | Passed, 17 named observations. |
| `--scenario enrollment-race --provider PROVIDER` | Passed, 15 named observations. | Passed, 15 named observations. |

Each command uses
`dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll`
with the exact arguments above and the named provider substituted. The native
examples observe stopped private nomination, exact atomic designation/event,
three distinct fresh processes, replay refusal, actual repeated preparation and
denied unrelated action. The nominated browser independently completes the
wrong-secret refusal, real commit with deliberately lost ingress acknowledgement,
fresh identity/designation, exactly two submissions/no automatic retry, secret
and browser-storage checks, keyboard/focus, 320px/200%, axe and original UI bound.
The fixture confirms the intended acknowledgement was actually lost and owned
browser cleanup completes. These are passing **hosted** results; earlier local
failures remain failures and their causes are not erased.

The same head's complete PostgreSQL integration artifact `12797783473` passes
all three commands: host integration **234.704s**, storage integration
**583.371s**, composition integration **225.140s**. In particular, both prior
twenty-contender failures now pass in their existing exclusive collections.
Together with SQLite preservation evidence, this closes **T051**: exact
populated-module assignment/data/EF history, consumed generation/designated
user/immutable event, original authorization replay, three fresh processes,
repeated prepare and actual administrator-issuer loss/reconciliation.
The provider-loss limitation still has no reset/reassignment backdoor.

The same head's SQLite integration artifact `12797976924` passes host
integration (216.816s) and composition integration (101.985s), but storage
integration fails (313.438s) only at the enrollment crash regression. This is
not a complete green feature gate. Crash tests now also use the existing
exclusive deadline collection, retaining both real pre/post-commit kills,
their original readiness/operation budgets and all exact durability assertions.
A local diagnostic run still fails both at child readiness; a subsequent
unchanged run passes both in 33.7s. No scheduling or platform root cause is
claimed from those passes. The PostgreSQL failure-propagation baseline also
retains its 900.315s `failure_exercise_timeout`.

### Real development actor and independently signed authority claims

The independent issuer can now include a verified-email claim, Administrator
role, administrator-shaped boolean and administrator display text in an
otherwise valid signed ID token. Existing no-ceremony and exact-nominee matrix
cases use this evidence: it cannot designate an initial administrator or
change the nominated identity. The focused eight-case SQLite selection passes.
No product identity/authorization logic changed.

The test-owned host can also explicitly prepare its normal **Development**
configuration and create a real development user, separate namespaced mapping,
protected durable minimal session and native framework cookie. The new
negative test proves both fresh `/me` and the public actor report that actual
development user, then submits with its legitimate identity-bound CSRF pair.
Enrollment returns non-enumerating **403**, preserving exact bootstrap,
users/mappings/events and registry revision. This is not a forged JSON mode,
mocked principal or positive development-login endpoint/UI claim. Creation is
a fixture-only owner operation, not a deployed route or test bypass in product.
Default OIDC fixture behavior stays unchanged; no OIDC backchannel is registered
for development configuration.

Executed on this increment with the existing isolated SDK:

| Exact command | Result |
| --- | --- |
| `dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` | Passed, zero warnings/errors. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentTests' --filter-method '*OrdinaryProductionLogin*' --filter-method '*EnrollmentChecksCurrentCeremony*' --minimum-expected-tests 8` | All eight passed with independent signed authority/profile/email claims. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentTests' --filter-method '*ARealStoredDevelopmentSession*' --minimum-expected-tests 1` | Passed: real development actor/session, 403 and exact unchanged protected state. |
| `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentTests' --minimum-expected-tests 17` | Sixteen passed; existing stopped-replacement case failed. Not an all-green matrix. |
| Same host project with `--filter-class '*SqliteEnrollmentTests' --filter-method '*ActualStoppedReplacement*' --minimum-expected-tests 1` | Unchanged private xUnit diagnostic rerun passed, 19.9s. Across those selections all seventeen cases pass; the full-run failure is retained, not diagnosed. |
| `dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentCrashTests' --minimum-expected-tests 2` | Initial private readiness failures; unchanged diagnostic rerun passes both real commit-boundary cases, 33.7s. |
| `dotnet format Kofte.slnx --verify-no-changes --no-restore` | Passed. |
| `python3 scripts/check_repository.py`; `python3 -m unittest discover -s scripts/tests -v`; `git diff --check` | Passed; 128 Python tests. |

**51/100 tasks are now complete:** T001–T047 and T049–T052. T041 now
includes real development-session and independently signed profile/authority
negatives; T043–T047's delivered core is reconciled with real hosted browser
proof, and T051 has actual both-provider populated-module/admin-loss evidence.
**T048 remains unchecked** until the expanded negative matrix executes on
PostgreSQL. T053–T055 still retain their prerequisite/focused-gate obligations,
despite passing individual public/browser recipes. Complete those final US2
checks, then continue US3 sign-out/session parity, US4 development login and
US5 recovery/final evidence. Driver remains implement/running; no acceptance,
readiness or merge request is made.

## US2 completion: expanded both-provider gates

Published head `f80eee96f17d3c8fe5f2e02ae1f4312ce3c14d50`, product run
`40648526486`, now passes both complete integration jobs and the complete
eight-recipe HTTPS browser job. Artifacts `12799266406` (SQLite) and
`12798554310` (PostgreSQL) independently record:

| Provider   | Host integration | Storage integration | Composition integration |
| ---------- | ---------------- | ------------------- | ----------------------- |
| SQLite     | Passed, 196.885s | Passed, 302.515s    | Passed, 80.191s         |
| PostgreSQL | Passed, 243.245s | Passed, 625.939s    | Passed, 225.411s        |

This includes each store's expanded seventeen-case enrollment matrix,
independently signed authority/profile/email negatives, actual stored
development-actor refusal, full twenty-contender and populated-module restart/
administrator-loss cases, and both actual crash boundaries. No cases,
contention levels, failure requirements or original deadlines were removed.
The local/intervening failures remain recorded; passing hosted execution does
not establish their root causes.

The same head's browser job `122682897465` passes all eight required individual
recipes, retaining the already inspected named observations, real lost
acknowledgement/fresh designation/no retry and owned cleanup. Aggregate `all`
remains an explicit final-gate obligation, not claimed executed here.
Existing generated contracts/component tests and fifteen focused strict
clean-recovery regressions remain the unchanged focused evidence above.
T048 and the prerequisite-gated T053–T055 are now complete: **T001–T055,
55/100**. The production/fixture implementation may consolidate the proposed
test filenames while retaining the specified assertions.

US2.1 traces to the disabled-ceremony and independently signed-claim negatives;
US2.2 to real stopped nomination, native protected completion and exact atomic
user/generation/event; US2.3 to the full identity/mode/CSRF/expiry/replacement/
replay matrices and non-enumerating fixed receipts; US2.4 to both twenty-way
races, owned pre/post-commit kills and browser lost acknowledgement; US2.5 to
three fresh hosts, actual repeated prepare, retained populated module and
unavailable-issuer reconciliation without reopened enrollment. Initial
designation still grants no unrelated operation, and administrator/IdP loss
has no reset shortcut.

The whole feature is not complete. Final run conclusions for this head are
success for quality, unit, both integration jobs, recovery, privacy, browser
and startup; contract and failure-propagation fail. These failures remain
unresolved, not waived. Continue **US3 durable sign-out/session parity** on the
same implement stage/branch/draft PR; do not advance to converge or request
acceptance/merge.

## US3 increment: durable sign-out and live session-boundary evidence

The delivered protected POST `/api/identity/v1/sign-out` admits only the
current session, exact Origin/CSRF pair and empty input. Revocation and its
session event commit in one owner transaction; 204 requires known completion,
invalid/expired sessions receive 401 and unavailable/uncertain work receives
a safe 503. Cookie deletion precedes response-body publication, including
concurrent-refusal paths. It deliberately does not call native `SignOutAsync`
after explicit revocation, which would invoke ticket-store removal and mutate
the owner a second time. No external provider logout is attempted.

The exact Next adapter validates empty 204, propagates individual deletion
cookies and never retries a mutation. The UI immediately removes personal and
enrollment content, distinguishes confirmed/expired/uncertain results, refreshes
browser continuity and requires deliberate login for re-entry. Focus, polling
and explicit identity-check retries cannot silently restore a locally cleared
identity after uncertain sign-out. The original 4.5-second adapter/UI and
one-second local operation limits are unchanged.

A new real-cookie/warmed-key regression initially failed:
`contract-sqlite-dcd00f7b1d8b448aaeec6c65c7d410f1.json`, 11.692s, with only
`LosingWarmedKeysIsUnavailableRatherThanAnAnonymousOrCachedIdentity` failing;
the real stopped-epoch revocation case passed. The .NET 10.0.13
`SecureDataFormat.Unprotect` catches decryption exceptions and returns null,
hiding the existing key guard's failure. A narrow wrapper around the
framework-created cookie format now verifies the retained ring outside that
catch, before and after decoding. Missing keys return safe 503 for both `/me`
and the actor probe, without cached identity, regenerated keys, session/event
mutation or loss of user/mapping state. Forged-cookie decoding remains owned
by the framework.

Actual local evidence on macOS arm64, unchanged pinned tools:

- The initial sign-out positive was red before route delivery. After delivery,
  fourteen SQLite cases passed, then three expiry/race cases passed. The
  expanded nineteen-case selection now passes as one command:
  `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*SqliteSignOutContractTests' --minimum-expected-tests 19`.
  Safe report `contract-sqlite-3fdc056ee9fb4ed2b2dec10d995541ad.json`,
  170.553s. This includes exact idle/absolute expiry, renewal/revocation,
  protected negatives, actual before/after-commit failures, warmed key loss
  and stopped epoch revocation.
- The subsequently added simultaneous-sign-out regression passes separately:
  the same command with `--filter-method '*ConcurrentSignOuts*'` and
  `--minimum-expected-tests 1`; safe report
  `contract-sqlite-0bc6105454e5420ba0af7c49bd248c8e.json`.
  Thus twenty cases are evidenced in two selections, not a claimed combined
  twenty-case rerun. PostgreSQL execution remains pending.
- The complete adapter/IdentityPanel selection passes 77 tests; web and browser
  type/lint checks and the production web build pass. Regenerated OpenAPI/types
  and `npm run contract:check` pass; three actual native OpenAPI publication/
  refusal cases pass. The full Release solution builds with zero warnings.

The separately compiled consumer now supplies the actual test-host actor
callback, checks ungranted-handler nonexecution and implements `session-boundary`.
Its HTTPS browser branch covers controlled idle/absolute boundaries, sixteen
real renewals, protected sign-out, lost acknowledgement, same-cookie direct/
adapted next-request denial, two users, no-store SSR, focus and accessibility.
Twenty private owned zero-byte file phases control only the test fixture;
there is no production clock/login/TLS bypass or control endpoint. The owned
ingress drops only the armed successful sign-out acknowledgement, and the
parent independently requires that observation.

This comprehensive browser evidence is **not green**. The first run failed
after 48.465s with only `identity_fixture_browser_failed` retained. Follow-up
safe phase capture (`browser-sqlite-3809c82f75c84f63b4756f36ea5292ec.json`)
shows the fixed 45-second child deadline, exit 137, last marker
`identity_browser_stage_trust_ready`, zero authorization/token requests and
zero session-control phases. The ingress observed anonymous identity page/
browser context 200 and `/me` 401. This establishes failure before sign-in,
not a key/sign-out root cause or proof that macOS itself is responsible.
Additional fixed phase markers distinguish activation/navigation/body progress;
both SQLite and PostgreSQL recipes are now required by the existing hosted
browser job. No deadline, TLS verification or cleanup requirement was relaxed.

**Progress remains T001–T055, 55/100.** T056–T070 retain full parity/corpus,
rotation/corruption, both-provider and successful public/browser prerequisites.
T071–T100 retain development, operational/recovery and final obligations.
Publishing this increment is not stage completion, acceptance or a merge request.

## US3 follow-up: hosted session proof and actual HTTPS parity

Published `2852371c1fad2d93d0dac3b703a47ae6e3592f6b`, product run
`40655171161`, browser job `122704340561`, passes **all ten** required HTTPS
recipes: five scenarios on each real provider. Completed job logs were
captured privately and independently checked against the exact provider,
scenario and ordered observation lists, without publishing private redirects
or tokens. Both `session-boundary` executions pass nineteen named observations,
including the ordinary and designated separately compiled actor comparisons,
ungranted-action denial, renewal/sign-out race, next-request denial and full
browser expiry/lost-acknowledgement/two-user flow. This supersedes pending
hosted evidence, not the retained local pre-login failure or its unknown cause.

The same head's repository run `40655170711` passes. At the latest observation,
quality, unit, both complete provider integrations, privacy, browser and startup
pass; recovery, contract and failure-propagation are still running.
Neither the full product matrix nor current uncommitted follow-up is called
green from that observation.

### Further independent boundary regressions

The fixture now sends to either native hosting or its fixed owned HTTPS
ingress, which runs the actual production-built Next routes and native host.
It uses the issuer-trusting HTTP client with manual cookies/no redirects;
it rejects arbitrary absolute adapter targets. No browser launch, mock fetch,
test principal, production TLS bypass or production probe is introduced.

Four SQLite parity cases now run together and pass:

- The same real cookie yields exact ordinary and freshly designated identity
  through native `/me`, HTTPS Next `/me` and the separately compiled actor.
  Actual stopped nomination and protected adapted enrollment/sign-out preserve
  the exact user, mapping, consumed ceremony and terminal events.
- The actual independently signed ID token emitted during successful native
  login is refused as an API bearer credential, with and without the cookie.
  Basic credentials, spoofed identity/proxy headers, body/query identity,
  incorrect method and missing/null/foreign Origin cannot authorize effects.
  The token remains memory-only and is never included in public evidence.
- Removing warmed retained keys returns fixed private 503, not anonymous 401
  or cached identity, through both native and actual HTTPS Next requests.
- Unknown identity and version paths return empty private 404 for GET, POST,
  HEAD and OPTIONS, with no redirect/cookie or owner-state changes. The first
  actual test was red (`contract-sqlite-b9bba7297e6444fd88ad9dcfdc340c04.json`).
  A fixed deny-only Next `[...unsupported]` handler now preserves this boundary;
  it never forwards, authenticates or reads configuration. Existing exact
  routes take precedence. The production web build and complete four-case
  selection pass after that correction.

Two additional native SQLite session cases also pass: the original cookie
survives ordinary retained-key rotation across two distinct fresh worker
processes, retaining two keys and exact identity/epoch/events; and a genuine
stored Development cookie cannot authenticate production, even when renamed
and accompanied by spoofed mode/administrator headers. The original valid
production cookie continues to identify its original ordinary user. These are
not the positive Development-login endpoint or complete persisted-mode
transition required later.

The key-aging simulation extracts the existing test-only unit helper, preserves
key material/ID and updates only the stopped fixture's dates/owner digest.
All twenty-seven original key units still pass. No operator repair API or
serving-time key regeneration was added.

### Executed verification and retained failures

All native commands use isolated SDK 10.0.402/runtime 10.0.13. Focused native
test commands run through the existing owned `verify.Runner` with private
xUnit XML and safe failure-name projection. No install or restore occurred.

| Exact command | Result |
| --- | --- |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityParityTests' --minimum-expected-tests 4` | Final four passed together, safe report `contract-sqlite-49a13d272e0144adb4835611c7d3e589.json`. |
| Same project/class with `--filter-method '*ActualNativeAndHttpsAdapterRefuse*' --minimum-expected-tests 1` | Passed after using the actual issued signed ID token, report `contract-sqlite-a87ce792d8da49d297f3b143001e6b8f.json`, 11.371s. |
| Same project with `--filter-class '*SqliteSignOutContractTests' --filter-method '*ActualDevelopmentCookie*' --minimum-expected-tests 1` | Passed, report `contract-sqlite-fbcfaab23fcf4684b8cf61780e829aa7.json`, 5.424s. |
| Original three SQLite parity cases, actual worker-rotation case and twenty-seven key units | Passed in report `contract-sqlite-87abc730a4fc43f2bf780cf67f38aa52.json`, 29.908s; not a claimed combined twenty-two-session-case run. |
| `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*PostgresIdentityParityTests' --filter-method '*WarmedIdentityCannotBecome*' --minimum-expected-tests 1` | Failed, report `contract-postgres-efc8e7e2004648ea96f9461b9e75be35.json`, 2.612s, with explicit disposable admission. No new PostgreSQL parity success or exact cause is established. Earlier local network-pool exhaustion remains recorded; no shared resources were pruned. |
| `dotnet build tests/contract/Kofte.Platform.ContractTests --no-restore -c Release -warnaserror --nologo` | Passed, zero warnings/errors. |
| `dotnet format Kofte.slnx --no-restore --verify-no-changes --verbosity minimal` | Passed. |
| `npm run build --workspace apps/web` | Passed with the exact identity routes and deny-only unsupported handler. |
| `npm run typecheck --workspace apps/web`; `npm run lint --workspace apps/web`; `npm run format:check` | Passed. |
| `python3 scripts/check_repository.py`; `python3 -m unittest discover -s scripts/tests -v`; `git diff --check` | Passed; 128 Python tests. |

The initial parity selection failed only its incorrectly spelled expected
enrollment event (`initial_admin_enrolled`); the test was corrected to the
fixed contract's `initial_administrator_enrolled`, then passed. Extracting the
shared fixture initially missed a unit-test namespace import; normal import
correction restored the full solution build. Neither initial failure is a
product outage or passing evidence. The earlier red unknown-route selection
and actual local/hosted failures remain retained.

**Progress remains T001–T055, 55/100.** Full independent TypeScript corpus,
remaining request/session/composed-host coverage and both-provider evidence
still gate T056–T070. Continue implementation on the original branch/draft PR;
there is no stage transition, feature acceptance or merge request.

## T056/T061: actual cross-language request and actor comparisons

The independent `tests/contract/identity.test.ts` now runs inside the existing
real native/HTTPS Next parity journey: ten ordinary-identity cases, then a
fresh one-case designated-identity/actor comparison after actual stopped
nomination and protected enrollment. Expectations come from the frozen
TypeScript corpus, the independent issuer and exact persisted user ID, not
production validators or copied response bodies. Both provider classes execute
this same fixture; the new TypeScript execution is locally verified on SQLite,
not yet PostgreSQL.

Actual coverage includes anonymous/forged/wrong-namespace cookies, the actual
signed provider ID token with/without a valid cookie, spoofed identity/proxy/
profile input, exact required identity fields, native separately compiled actor
equality and ungranted-handler nonexecution for ordinary and designated users.
The tests also execute both paths' HEAD/OPTIONS and exact Allow, unknown/case/
trailing-slash paths under GET/POST/HEAD/OPTIONS, missing/null/foreign Origin,
cross-site Fetch Metadata, missing/forged/stale anonymous CSRF pair, body/query
confusion, duplicate/unknown JSON, every frozen rejected return and both allowed
returns' deliberate code+S256 challenge. The sixteen/seventeen/duplicate cookie
boundaries inspect individual cookie attributes; exact eight-KiB/one-byte-over
JSON distinguishes accepted shape/closed ceremony from rejected input.

No mutation is retried and no identity is manufactured from missing fields.
The C# owner comparison still verifies exact user/mapping/ceremony/events and
terminal self-session revocation after the TypeScript probes. The actor probe
consumes only public Platform types; its scenario compares HTTP/public actor
values without identity-table access. No test probe is registered in the
production host.

### Real defects and fixture corrections

The expanded test first exposed Next's generic HTML/cache response for a
wrong-case identity prefix. A fixed narrow `proxy.ts` rejects spelling variants
and trailing slashes in identity/auth namespaces, without forwarding,
authenticating, reading private configuration or changing valid destinations.
Automatic Next trailing-slash redirection is disabled so these noncanonical
requests cannot become redirects before rejection. Canonical health/page
interfaces remain unchanged.

Adding auth-path variants then exposed a separate **native** defect:
GET `/auth/Login` returned authenticated fallback-policy 403, not empty private
404. Wrong-method endpoint rejection metadata does not carry the normal
`RouteEndpoint` used by the prior spelling check. The boundary now rejects
case/trailing variants of its fixed known paths independently of that
metadata, before authentication/policy/mutation. Correct exact paths retain
their original method, credential and Origin checks. The safe failing
observation is native transport 0, corpus row 6, status 403 in
`contract-sqlite-bc8a363005c648d1b3054194e8d2d60b.json`.
All eleven unknown/auth cases now run under four methods through both paths
and pass, rather than weakening the expected status.

The first fixture attempted to parse Vitest JSON from stdout, but this
installed reporter writes its default report elsewhere. Two runs retained
that fixture JSON-parse failure; no successful corpus was claimed from them.
It now explicitly precreates owner-only input, CA and bounded structured
report files inside its owned private root, validates the report and requires
all ten/one tests to pass. The accidentally generated local default report was
deleted. Raw reports, cookies, tokens, private paths and failure payloads are
never published; only source-case/line/route indices can enter the fixture's
failure code. An intermediate Origin projection wrongly added a slash through
`AbsoluteUri`; preserving the exact configured `OriginalString` corrected the
fixture, not the product's exact-Origin rule. Initial TypeScript inference
errors were corrected with ordinary typed header/count collections, not casts.

Published `19ee621`, product run `40657265290`, then failed its native platform
contract step in 545.413s with exactly the eight new SQLite/PostgreSQL HTTPS
parity facts named failed. The contract verifier had not built Next before
those tests; its previous web build occurred only in the later HTTP runtime
phase. The added build-order regression initially errors with `StopIteration`,
then passes after an explicit production web build before native public
contracts. Fixtures now report missing build prerequisites explicitly.
This fixes a demonstrated orchestration omission; successful corrected hosted
execution remains required before assigning every original CI failure to it.

### Verification and remaining obligations

- `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityParityTests' --filter-method '*ActualHttpsAdapterAndSeparateActor*' --minimum-expected-tests 1`
  passes after the full ten-case corpus/native correction:
  `contract-sqlite-33df33d7c8154baf9a2e461f75f0b06c.json`.
  The final complete four-case selection also passes, including those ten
  cases and the fresh designated comparison:
  `contract-sqlite-3b39a298d09945c2aaa4f2bf608e8e62.json`.
  Earlier eight-case/native parity selections also pass in
  `contract-sqlite-b0d0ecab2810475d848912b74efa5bee.json` and
  `contract-sqlite-a06e63d9bcc74211ae26d543db9b6482.json`; these are not
  relabeled as executing the later ten-case source.
- The ordinary owned runtime runs
  `npm run test --workspace tests/contract -- --no-file-parallelism bootstrap.test.ts adapter.test.ts`
  successfully, including original native/Next health/OpenAPI, real body,
  unavailable-store/recovery and cleanup/output scanning. Report
  `contract-sqlite-e972aa30495c41f2aab527736885ee89.json` passes all thirteen
  steps. Its unchanged predecessor
  `contract-sqlite-7f74e070b6c94a179a2c73fc33c32fad.json` fails after the
  web build exits 0 during cleanup (`verification_input_invalid`); the
  unchanged retry does not establish that local cleanup failure's cause.
- Full native solution build, web production build/lint/types, contract
  lint/types, repository checker, 128 Python tests, whitespace and root
  formatting pass. The new build-order assertion extends an existing test,
  not the test count.
- Earlier `2852371` now passes recovery as well as both complete integrations,
  all ten HTTPS recipes, quality/unit/privacy/startup and repository checks.
  Its failure-propagation baseline still times out at the original 900-second
  limit; the safe artifact does not establish the underlying cause. Contract
  was still running at that observation. No deadline is increased.

**T001–T056 and T061 are complete: 57/100.** This closes the required test/probe
additions, not T064's entire independent corpus or the both-provider US3 gate.
T057–T060 and T062–T070 retain their remaining coverage/execution obligations.
T071/T072 test roots now depend on completed T055, while T064 explicitly
depends on T074: the frozen corpus includes positive Development login, so its
endpoint/adapter must exist before the entire corpus can pass. This is a
delivery-sequencing correction, not a waived case or changed product contract.
Continue Development prerequisites alongside US3, on the same implement stage
and draft PR. No acceptance or merge is requested.

## T071–T073: explicit native Development selection and persisted mode isolation

The existing configuration test class now includes all host/web/backend exposure
cases with unsafe opt-in both disabled and enabled. Existing environment, explicit
opt-in, strict configuration and non-loopback refusals remain. Test targets are
consolidated in the existing unit project and the native public-contract project,
as allowed by the task organization rule; all source remains inside `tests/`.
The original positive native selection test fails before endpoint wiring
(`contract-sqlite-539bc65a9ced4d669fbfe3d88e23cc6c.json`); the expanded configuration
class passes in the same report.

`POST /api/identity/v1/development-session` exists only in admitted Development.
It accepts exactly one configured `identityKey`, rejects unknown/duplicate/profile
and authority fields, and reuses exact Origin/framework antiforgery, bounded
browser/installation/peer admission and the one-second owner budget. The fixed
`kofte:development:v1` namespace, user/mapping/session and required events commit
together before the native framework publishes the separate HttpOnly/Lax/path-only
Development cookie. It never uses the provider or grants initial designation.
Current-identity encoding is shared with `/me`, not duplicated.

Initial live tests exposed two fixture/bridge issues, retained in
`contract-sqlite-fd47b2f49921436ea355e3ba29f47019.json` and
`contract-sqlite-9499d0648dc141d8af147fc822372589.json`. A signed-in explicit
selection must authenticate the old request to validate its identity-bound CSRF,
but the standard already-authenticated cookie handler invokes `RenewAsync`,
not `StoreAsync`. The latter alone may consume the known committed new reference.
Development now initializes a fresh DI-provided native cookie handler after the
commit; its cookie reader suppresses the old reference only when a request-local
committed binding exists. This retains the authenticated antiforgery check and
fresh random cookie without permitting arbitrary ticket renewal or a second
storage transaction. The exact framework 10.0.13 handler source was consulted;
no private reflection, custom token format or copied framework implementation.

The mode fixture initially hardcoded the child environment as Production and
then regenerated production key custody on returning from Development. Its
failures remain in the reports above and
`contract-sqlite-4647ef4dd77747d2a2e3d23d093a7bc8.json`. It now reads the configured
child environment and retains separate owned configurations/key custody per mode.
Each transition stops the actual host, explicitly reconciles configuration under
the normal operator and starts a fresh worker on the same installation. Private
roots remain owned until verified worker cleanup. No serving repair is added.

**Actual verification:**

- `contract-sqlite-797134b800f64f19906fb54bfb4e3048.json`: all four native cases pass:
  stable configured selection/fresh cookie and CSRF; twelve malformed or
  caller-authority bodies without protected effects; and actual OIDC→Development→OIDC
  transitions with initially pending and consumed ceremonies. Equal subjects and
  null displays produce distinct exact users/mappings. Both original and renamed
  old cookies fail. Development enrollment is forbidden; consumed user/time/generation
  survive, unconsumed authority is disabled by explicit reconciliation, old enrollment
  cannot replay, original production login returns its original ID/designation,
  creation evidence survives, registry revision is unchanged and ungranted actions
  remain denied.
- `contract-sqlite-a938bac8327041388f6ec683cb472c6f.json`: the final four cases
  also pass with exact replacement-reference sign-out: only the newly committed
  reference becomes revoked and the prior session remains active. All four
  GET/POST/HEAD/OPTIONS Development-path variants return cookie-free 404 in
  production before protocol/CSRF handling.
- `contract-sqlite-8936dd9866d84b3f9454ba688cdef80b.json`: actual owned native
  Development OpenAPI capture and all thirteen runtime/start/cleanup/scan steps pass.
  Only the new operation is inserted into the existing checked-in document;
  unrelated operation bytes remain. `npm run generate --workspace packages/bootstrap-contract`,
  `npm run contract:check` and generated contract type-check pass.
- `contract-sqlite-e5a6a7db0eee4e248acd10733483ebae.json`: retained native login
  atomic-publication/bridge cases and all three existing SQLite OpenAPI cases pass,
  including the complete actual generated baseline and Production/Staging refusal.
- Full Release solution build with warnings as errors, full native format
  verification, repository checker, all 128 Python tests and whitespace pass.
- `contract-postgres-ada0fb37fd704ba18845022d948e5ad0.json`: fresh PostgreSQL
  Development selection fails while creating its owned provider fixture at
  `PostgresService.DockerAsync`, before product requests. The captured generic
  error does not establish the underlying Docker cause. This is not PostgreSQL
  acceptance or permission to waive T077.
- Published `2f3d1e7` repository checks pass. At this observation its quality,
  unit, SQLite integration, startup, browser and privacy jobs pass; PostgreSQL
  integration, recovery, contract and failure-propagation remain running.

**60/100 complete:** T001–T056, T061 and T071–T073. T071/T072 are the added test
tasks, not T076/T077's full actual environment/both-provider acceptance. T074's
exact adapter/accessible configured selector, remaining US3, recovery/operations
and complete local/hosted gates remain. Driver stays 005/implement/running;
reviewer checklists, stage acceptance and the original draft PR are unchanged.

## T058–T060/T074: verified Development selector and session-record refusal

The exact Development adapter accepts only one configured synthetic key, refuses
duplicate/unknown/profile/authority fields and remains 404 in OIDC mode for every
method. Server configuration projects only validated synthetic key/display choices;
it never passes private provider/key configuration or personal identity to the
page. The selector requires explicit choice/submit, restores focus, and retains a
visible Development label during loading, refusal and uncertainty. Display validation
and loopback validation reuse existing guards. Individual Development cookies must
remain host-only, HttpOnly, SameSite=Lax and non-Secure on the admitted HTTP profile.

The shared bounded mutation controller clears the previous identity before either
selection or sign-out. Only known selection success permits fresh `/me` and
identity-bound CSRF retrieval. Denied/uncertain selection and every sign-out result
keep personal/enrollment content cleared across focus, polling and explicit check
retries, without automatically replaying a mutation. Server configuration failures
render fixed safe guidance and a generated reference; logs contain no private cause.

The existing native session and sign-out classes consolidate the proposed T058
test path. They cover exact idle/absolute boundaries, capped renewal, terminal
renew/revoke races, fresh durable designation, actual wrong-mode/epoch rejection,
retained-key rotation/fresh workers, key loss and both commit-acknowledgement sides.
A new required-record regression installs a private fixture-only database insert
refusal for `session_revoked`: delivered sign-out returns 503, session revocation
rolls back with no success event, unrelated state remains, and explicit correction
allows the original credential again. No failure injection or schema exception is
added to product code.

**Actual verification on macOS arm64 with the retained pinned tools:**

- `npm run test:unit --workspace apps/web`: **146 passed** across all eight files,
  including explicit selection, fresh-CSRF use, denied/uncertain switching and no
  stale identity/automatic retry. Initial selector red remains in
  `unit-none-ceecbe9883ad4523ae2350766f9d2f6d.json`.
- Shared `npm run test:unit --workspace packages/bootstrap-contract`: **4 passed**.
  Web/shared/contract/e2e lint and type-check, intended-file Prettier verification
  and `npm run contract:check` pass.
- `contract-sqlite-6ed425c9c92e45b695032821fec61c38.json`: all fifteen owned runtime
  steps pass, including native/production Next builds,
  `npm run test --workspace tests/contract -- --no-file-parallelism bootstrap.test.ts adapter.test.ts`
  and `npm run test --workspace tests/e2e -- identity.spec.ts`. The actual HTTP case
  compares native/adapted stable identity, individual cookie attributes, stale
  anonymous CSRF refusal, fresh authenticated CSRF and confirmed next-request
  sign-out denial. Actual Chromium verifies explicit selection within five seconds,
  API/UI equality, non-personal SSR, HttpOnly/no browser storage, sign-out and the
  retained 320px/200%/keyboard/axe shell. Host restart/store restoration, both owned
  host exits, web exit and private output scanning pass.
- The prior attempt `contract-sqlite-daaf8301864a4f098d8416f818ebe189.json` failed
  `runtime-web-build` with `verification_input_invalid` despite child exit 0.
  An unchanged retry with transient exception-site-only tracing passed; the earlier
  cleanup/input failure is retained, not diagnosed or claimed repaired.
- `contract-sqlite-4a27a41792324dc2b9820460eac9e038.json`: Release solution build
  with warnings as errors passes, then the combined command
  `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*SqliteSignOutContractTests' '*SqliteSessionPersistenceContractTests' --minimum-expected-tests 30`
  passes all thirty cases in 256.073s, including actual required-record refusal.
  PostgreSQL execution of that new regression remains pending T065.
- Final live HTTP assertion verification
  `contract-sqlite-c7ee756c6c5449c5b3e5a5751f159138.json` passes all thirteen
  build/preparation/native-adapter/cleanup/scan steps. Cookie assertions inspect
  individual attributes, not accidental substrings inside an opaque reference.
  Full native format verification, current web lint/type-check, repository checker,
  all 128 Python tests and whitespace also pass.
- Completed `2f3d1e7` product run `40659799413` passes unit, SQLite integration,
  recovery, startup, browser, quality and privacy. PostgreSQL host integration fails
  `DeliveredInvalidEvidenceCannotCreateAUserSessionOrCommittedLoginEvent`;
  composition-public contracts time out at 900.044s; failure-propagation's nested
  PostgreSQL baseline times out at 900.316s. Those safe reports do not establish the
  assertions/causes. Current `ebffa72` repository checks pass; its product run remains
  incomplete, with SQLite integration failing
  `ActualStoppedReplacementRefusesTheOriginalSecretAndConsumesOnlyTheNewGeneration`. No full green matrix is claimed.

T058's test additions, native/web sign-out T059/T060 and Development adapter/UI T074
are now complete: **64/100**, with no checked task depending on an unchecked task.
T057/T062–T070 retain full boundary/corpus/both-provider/story evidence; T075–T078
retain supervisor/environment/both-provider/public-recipe acceptance. Operations,
recovery and final local/hosted gates remain. Driver stays 005/implement/running;
no reviewer checklist, acceptance, readiness or merge decision changes.

## T075: explicit loopback supervisor and actual startup-matrix progress

The local supervisor now rejects non-loopback host/web addresses even when
`KOFTE_ALLOW_UNSAFE_EXPOSURE=true`, and normalizes both `ASPNETCORE_ENVIRONMENT`
and `DOTNET_ENVIRONMENT` to `Development`. The owned verifier uses the same
explicit environment, instead of inheriting a contradictory production setting.
Authentication configuration is forwarded unchanged. Serving still executes
only builds and the two serving children: it does not initialize, migrate,
prepare, generate configuration or repair keys. Owned fresh verification retains
its separate explicit `--store init`, `--registry upgrade`, `--identity prepare`
sequence, private synthetic configuration and separate private key directory.

The initial focused Python run failed eight subcases: six broader-listener
combinations and the two inherited environment conflicts. After correcting the
supervisor/runtime, all **131 Python tests** and the repository checker pass.
`contract-sqlite-888c6c1960a24371894dbb24faddd1c5.json` records thirteen passing
actual native/Next build, preparation, contract, restoration, exit and output-scan
steps while the verifier itself receives `DOTNET_ENVIRONMENT=Production`.

The test-owned startup fixture now supports explicit child launch customization
and shares the existing persistent storage/key snapshot helper. New
`DevelopmentBoundaryTests` exercise 22 invalid configurations through actual
empty/composed startup with unsafe opt-in both false and true: production,
staging, wrong-case/conflicting environment; absent/malformed mode/configuration/
opt-in/choices; injected administrator field; wrong public origin; and
host/web/backend wildcard IPv4, wildcard IPv6 and non-loopback addresses.
Every configuration refuses within the existing five-second limit, exposes no
private fixture path, exits with descendants verified, and releases its listener.
Prepared storage/key snapshots remain identical. No product TLS/principal bypass
or new deadline is introduced.

The first matrix runs failed test assumptions, not an observed admitted invalid
startup: composed bootstrap diagnostics use `code` rather than `eventCode`, and
the initial closed-port HTTP assertion did not consistently throw the assumed
exception. The corrected test checks the exact existing diagnostic schema and
directly proves its owned port can be rebound after refusal.
`integration-sqlite-3c8883c635564d7790099fb4cf3d8435.json` passes the Release
warnings-as-errors solution build and all **eight** selected cases:
four matrix cases covering **88 actual invalid starts**, plus four retained
Development/production startup cases. The earlier failed matrix reports
`integration-sqlite-4d51a683adad47beaed1ce2f6e0ed9eb.json`,
`integration-sqlite-644d56a5fc254785bf7e29e0ebb204a8.json` and
`integration-sqlite-1ec8b6e7fea9426daaecd18b9692ab01.json` remain failed history.

A separate fresh owned Compose creation probe now identifies the local
PostgreSQL blocker as **network-pool exhaustion**, from the daemon's fixed
address-pool failure category. Its owned-resource labels were checked and its
`down --volumes` cleanup returned zero; no existing resource was removed.
This identifies that probe's cause, not every earlier generic Docker failure.
Hosted `ebffa72` PostgreSQL integration subsequently passes, while its SQLite
integration and nested failure-propagation jobs fail; other jobs remain incomplete
at observation. Published `8367771` repository checks pass; its product matrix is
still running, with quality/unit/browser/startup/privacy passed at observation.

T075 is complete: **65/100**. T076 retains complete actual direct/adapted
Development acceptance; T077 retains both-provider transitions, and T078 retains
its complete public recipe. T057/T062-T070 and operations/final gates remain.
Driver stays 005/implement/running on the same draft PR; no stage transition,
human acceptance or merge is requested.

## T057/T062: complete request-test and shared error-policy assertions

The planned request-test additions are consolidated into the existing
`LoginEndpointContractTests`, `IdentityParityTests` and actual TypeScript corpus,
rather than duplicating their fixtures in a new host-integration file.
Both provider subclasses remain wired; this checkpoint executes SQLite, while
T064/T065 retain mandatory full-corpus/both-provider acceptance.

The native login tests now execute **every** independently frozen unsafe return,
not the earlier eight-item subset. New cases reject null, foreign, slash-suffixed
and duplicate Origin, cross-site/duplicate Fetch Metadata and duplicate CSRF
headers before attempts, users, sessions, events or provider egress. Exact
HTTP/1.1 wire header framing verifies admission at 16,384 combined header bytes
and a fixed private 400 one byte over, with no further security revision/event.
The admitted browser-context request legitimately advances admission revision;
the rejected request is compared to that exact admitted snapshot.
Every other GET/POST/PUT/DELETE/HEAD/OPTIONS method across the six production
protocol/API routes returns empty private 405 with exact Allow, no cookies,
redirect, provider access or persistent security mutation.

Retained executed cases cover exact path/case/slash/query/body refusals,
unknown/duplicate JSON/form fields, individual cookie count/attributes and body/
callback-target limits; fresh identity-bound CSRF versus stale/missing pairs;
and successful GET callback's independent protocol binding. Actual native/HTTPS
Next/separately compiled actor comparisons verify ordinary/designated identity,
signed provider-token and mixed-credential 401, authenticated ungranted-action
403, unavailable-key 503, exact five-field problem schema/content type/private
headers/correlation and no redirect, HTML or success-shaped anonymous actor.
The earlier shared native/Next defects and fixes remain in their historical
sections; these additions expose no new product-policy defect.

Actual final command:
`dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*SqliteLoginEndpointContractTests' '*SqliteIdentityParityTests' --minimum-expected-tests 16`
**passes all sixteen cases together**, after a passing Release
warnings-as-errors solution build, in
`contract-sqlite-a44fd6f7289342d1bd02395aa1cb1b18.json`. The focused two new
header cases also pass in `contract-sqlite-f9c3cdf81d0e475abd629b04f8d30807.json`.
Initial failures in `contract-sqlite-2c8fe2f70aca4f82a8161e97a5215d90.json`,
`contract-sqlite-4a88ddb3ce174b9e94a92b2b2719a6b1.json` and
`contract-sqlite-22aff8cf1e6d4fddbf0f697e71a884cd.json` were incorrect fixture
expectations about retained `keys_prepared` evidence and admitted-context
revision. Exact before/after snapshots correct those assumptions; no product
mutation or policy check was removed. Full native format verification passes.

The final repository checker, all **131 Python tests** and whitespace check
pass. Completed `ebffa72` product run `40662173786` now passes contract, recovery,
PostgreSQL integration, unit, quality, browser, startup and privacy; SQLite
integration and failure-propagation remain failed. Current `8367771` adds passing
SQLite integration at observation; the other incomplete jobs are not called
green. Local PostgreSQL network-pool exhaustion still prevents new provider
execution; existing resources remain untouched.

T057/T062 and T075 are now complete: **67/100**, with no checked task depending on
an unchecked prerequisite. T063-T070, T076-T100 remain. Driver stays
005/implement/running on the original draft PR; final review, explicit human
acceptance and merge remain separate.

## T063: actual empty/composed native/Next session scope and epoch

`SessionScopeContractTests` consolidates the proposed session-contract path.
Its SQLite/PostgreSQL subclasses use actual `BootstrapProgram` empty or composed
entry paths, not a manually constructed test application. A test-owned
`StartDevelopmentWeb` fixture starts the already-built production Next process
against the exact owned backend/configuration; it forwards no database credential,
adds no TLS/principal bypass and shares verified process/descendant cleanup.
The existing contract suite already builds production Next before these tests.

Two independent browser cookie containers explicitly select different configured
synthetic identities. Concurrent API requests preserve each exact user ID across
both direct and Next paths, with required mode/version/designation and no-store.
The second anonymous browser cannot inherit the first user's actor; spoofed
identity/administrator headers cannot authenticate an unrelated browser.
Both serving processes stop before the actual admitted `revoke-sessions` command.
Fresh processes then reject both **saved original Cookie headers** on their first
protected requests through both paths. Readiness uses a separate anonymous client,
and replay disables cookie mutation, so clearing a browser cookie cannot masquerade
as server-side revocation. Explicit fresh selection returns each original user ID.

Actual commands/results:

- Release warnings-as-errors solution build passes.
- `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*SqliteSessionScopeContractTests' --minimum-expected-tests 2`
  passes both actual entry-path cases in
  `contract-sqlite-20f77294e531433085961b1a4745f54b.json`, including exact original
  credential replay and verified host/web/command exits. The initial native-only
  prototype passes six startup cases in
  `integration-sqlite-0aae461118924a7aaf9adda68bd529fd.json`; it is replaced, not
  duplicated, by the stronger actual native/Next contract test. The first
  native/Next variant also passes in
  `contract-sqlite-2d7822d71f314094a576b4f02dc288db.json`.
- `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*SqliteCurrentIdentityContractTests' --minimum-expected-tests 2`
  passes fresh durable profile/designation in API/actor, ungranted-action refusal,
  next-request revocation and corrupt-ticket 503.
- `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityPreparationTests' --filter-method '*DirectWorkerCannotAcquireOfflineAuthority' --minimum-expected-tests 1`
  passes the separate admitted stopped-worker boundary.
  Those latter three cases are recorded together in
  `contract-sqlite-0f61e5b6460b4c39aecff061b6bc8f86.json`; the earlier sixteen-case
  parity selection independently verifies ordinary/designated actors and denied
  unrelated action through the production HTTPS boundary.

T063 is complete: **68/100**. T064/T065 retain mandatory full-corpus and
both-provider acceptance; the new PostgreSQL scope counterpart is not claimed
executed locally while Docker address pools are exhausted. No request-scoped
product wiring defect is observed or hidden by a synthetic success fallback.
Current published `1da3483` repository/quality/unit/startup/browser checks pass
at observation. Its privacy job fails the existing SQLite composition consumer
`IndependentPrivateDiagnosticsRequiresRealChannelsAndSafeUsefulBoundaryEvidence`
with generic `composition_consumer_failed`; the safe artifact does not establish
its underlying cause. Remaining incomplete jobs and full feature acceptance
remain open; the driver stays implement/running.

## T064 increment: actual HTTP corpus and Node parser boundary

The independent production TypeScript corpus now executes thirteen ordinary
cases plus its designated actor case, using the existing owned native HTTPS
host, production Next and separately compiled actor. Unsupported-method checks
cover all seven GET/POST/PUT/PATCH/DELETE/HEAD/OPTIONS verbs on the six production
protocol/API routes, excluding each admitted method, with exact Allow, private
empty 405 and no redirect. Unknown/case/version/trailing-slash paths retain
private empty 404 for the same verbs.

New cases refuse ambiguous Origin/Fetch Metadata/CSRF headers without revoking
the current session, reject duplicate/unknown login fields and incorrect
media/query input without a challenge, and independently frame exact
16,384/16,385 combined HTTP header bytes on both transports. The low-level
request helper disables implicit Node headers and explicitly supplies Host
and any nonempty body length. Fixed forwarded fields prevent Next from adding
unmeasured framing; they remain caller data, not trusted identity/routing.
No expected byte limit is calculated from a product validator.

Initial runs are retained in
`contract-sqlite-608d045d48af40e9931f4a76a461c87f.json`,
`contract-sqlite-a16ee1dd14d544a8bbb40a1efa051a20.json` and
`contract-sqlite-46e25da71b3f4672bcea480545571327.json`. Correcting independent
wire framing then exposed the actual Node parser's bare 431 at the admitted
16 KiB boundary in `contract-sqlite-8eecd76425f44ca8b68148e67883d226.json`.
Web dev/start scripts, the verifier and both identity/startup browser fixtures
now explicitly give Node 32 KiB parser headroom. The application limit is
unchanged: exactly 16 KiB succeeds and one byte over returns private 400 with
no cookies. No input limit, deadline, TLS rule or effect assertion is weakened.

Actual final command:
`dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityParityTests' --filter-method '*ActualHttpsAdapterAndSeparateActorRetainTheSameIdentityThroughNominationAndSignOut' --minimum-expected-tests 1`
passes, together with the Release warnings-as-errors build, in
`contract-sqlite-206592cd396245379a9b7131fbacde9b.json`. Its child report requires
all thirteen ordinary and one designated TypeScript cases, not just a successful
test-process exit. Failure projection retains only source-line numbers and
fixed route/header transport/size/status indices, never private assertion text.

The inherited `1da3483` SQLite composition-privacy failure is not reproduced by
the unchanged local public `private-diagnostics` recipe
(`privacy-sqlite-799e8ca1cf184d19bc29e42ce44364b5.json`) or its actual one-case
contract wrapper (`privacy-sqlite-fd51229b770740869189d33c047d7edf.json`);
that wrapper's build passes. The later published `135fe80` hosted privacy job
also passes. None establishes the original failure's cause. The privacy
wrapper now follows its existing sibling contracts by retaining consumer output
in private xUnit capture; the existing verifier projects only validated
source-allowlisted failure codes/scenarios, discarding the private capture.
The former generic `composition_consumer_failed` is not adequate diagnosis.

T064 remains unchecked for its complete Development/both-provider corpus.
Progress remains **68/100**; the local PostgreSQL network-capacity blocker,
unfinished operations and unresolved full-matrix failures are not waived.

Final regression selection adds
`--filter-class '*SqliteLoginEndpointContractTests' '*SqliteIdentityParityTests' '*SqliteSessionScopeContractTests' '*SqliteCompositionPrivacyContractTests' --minimum-expected-tests 19`
to the same contract command: all nineteen cases pass together in
`contract-sqlite-b54ddf46b0484056bd24850f95ac78fb.json`, as does
`dotnet format Kofte.slnx --no-restore --verify-no-changes --verbosity quiet`.
The retained owned runtime's
`npm run test --workspace tests/contract -- --no-file-parallelism bootstrap.test.ts adapter.test.ts`
passes all thirteen build/preparation/native/Next/cleanup/output-scan steps in
`contract-sqlite-2233b1c3998043d1ae9c98c0e3679594.json`.
Its preceding attempt, `contract-sqlite-17fd7fb0e962427abd80ce5b205d862f.json`,
fails web-build cleanup/input handling despite child exit 0. The unchanged retry
does not diagnose that inherited intermittent local failure.
Repository checking, all 131 Python tests, whitespace, contract/e2e lint and
strict types, full web formatting and the public web start-script help pass.

## T064 Development increment: independent corpus in both serving scopes

The existing Development HTTP case now independently exercises both actual
native and Next paths with all seven methods across the API inventory, absent
OIDC/control/case/version/slash routes, exact Allow/404/405/private headers,
missing/foreign/null/ambiguous Origin, cross-site Fetch Metadata, missing/wrong/
stale CSRF, malformed/unknown/duplicate/caller-authority selection fields,
unsupported and mixed credentials, wrong-mode cookie namespaces, exact cookie
attributes, enrollment refusal and confirmed sign-out. It measures the admitted
8,192-byte selection body and refused 8,193-byte body, plus independently framed
16,384/16,385 header bytes. Exact identity fields and values are frozen acceptance
expectations, not the product's response type guard.

Shared independent method/header framing avoids duplicating the production
corpus. The existing private TypeScript execution/report validation is extracted
without changing its 45-second child limit, owner-only report handling, exact
passed-case requirement or sanitized numeric/source-line failure projection.
Both empty/composed Development scope facts invoke the complete selected case
while their owned real host and Next are running. Both provider subclasses
inherit it, so it executes in the native contract phase before the currently
timed-out later composition phase. The TypeScript case retains its original
20-second deadline. Other adapter cases are not claimed covered by that
selection; the complete retained Runtime executes them separately.

The first expanded run is retained in
`contract-sqlite-130dfa4d8ce4439e98ad5e509e7315ab.json`: the production corpus
passes, while the missing-CSRF Development request also omitted valid JSON
media and therefore correctly received 400 instead of the expected 403.
Correcting only that fixture makes the complete Runtime's
`npm run test --workspace tests/contract -- --no-file-parallelism bootstrap.test.ts adapter.test.ts`
pass all thirteen build/preparation/HTTP/cleanup/output-scan steps in
`contract-sqlite-09548dbbb1254b32b5aba455bcde22e6.json`.

The Release warnings-as-errors build and
`dotnet format Kofte.slnx --no-restore --verify-no-changes --verbosity quiet`
pass in `contract-sqlite-7b0e83e60c4f4ec986f26973b8a9f9fa.json`, together with:
`dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityParityTests' '*SqliteSessionScopeContractTests' --filter-method '*ActualHttpsAdapterAndSeparateActorRetainTheSameIdentityThroughNominationAndSignOut' '*ActualEntryPathsKeepDirectAndAdaptedUsersRequestScopedAndRefuseThePreviousEpoch' --minimum-expected-tests 3`.
All three actual facts pass, requiring the thirteen ordinary production cases,
designated actor case and both empty/composed full Development cases.
Final contract lint, strict types, full web formatting, repository checking,
all 131 Python tests and whitespace checking pass.

Published `135fe80` hosted native platform contracts pass on both providers in
713.028 seconds (`contract-both-b517e92a8e314f118d9c9a8a4e69a059.json`).
That includes the session classes, but not this new Development invocation.
Its later composition-public command times out at the unchanged 900-second
limit. Its host integrations fail named enrollment/invalid-evidence facts;
the safe report does not establish their assertion or cause.
Published `dfb26a7` SQLite host integration fails
`DeliveredBrowserLoginRateRefusesTheEleventhAttemptBeforeProviderOrIdentityEffects`,
while storage and composition integrations pass. Its recovery job passes
storage recovery but fails the `bounded-stop` public composition correction
recipe. These are retained failures, not green gates or diagnosed product bugs.
Local PostgreSQL fixture creation remains blocked by network-pool exhaustion.
No deadline, TLS/ownership rule or expected effect is weakened. T064/T076 and
the other remaining acceptance/operations gates stay open at **68/100**;
the driver remains **implement/running**.

## T065: both-provider durable session and stopped trust evidence

The fresh complete command
`dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*SqliteSignOutContractTests' '*SqliteSessionPersistenceContractTests' --minimum-expected-tests 30`
passes all thirty cases in
`contract-sqlite-e68092d307b54948b87d83bda0073bce.json` (449.761 seconds).
The exact controlled 30-minute idle and eight-hour absolute boundaries retain
terminal timestamps and immutable original absolute expiry. Activity cannot
resurrect confirmed revocation. Corrupt tickets and lost live keys return
unavailable instead of a cached actor. Per-user/installation capacity, retained
keys across two fresh workers, actual stopped epoch revocation, wrong-mode
credentials, concurrent sign-out, renewal races, required-event rollback and
before/after-commit interruption remain asserted with exact rows/events.

Both-provider evidence is the completed `135fe80` hosted native platform
selection, not a local PostgreSQL claim: `platform-public-contracts` passes in
`contract-both-b517e92a8e314f118d9c9a8a4e69a059.json` using
`dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-not-class '*Composition*' --minimum-expected-tests 1`.
It includes both provider subclasses of the complete session/sign-out suites.
Both storage integration selections also pass with
`dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-trait Provider=sqlite --filter-not-class '*Composition*' --minimum-expected-tests 1`
and its `Provider=postgres` counterpart, in
`integration-sqlite-52a56acc4584404b9c2efd9e89590f75.json` (318.699 seconds)
and `integration-postgres-4fba340d62a8441a83bd9521beed34b1.json`
(708.933 seconds). Those include actual stopped provider/client-trust replacement,
next-request old-cookie denial, stable prior users/mappings/creation events,
new issuer identity separation and preserved consumed designation/module data.

These are the existing `SessionPersistenceContractTests`,
`SignOutContractTests`, `IdentityMappingTests` and `EnrollmentRestartTests`
consolidated under their dedicated test projects, as permitted by the task
file. A source comparison against the passing hosted revision confirms no
change to these files or production `src/`. New Development corpus wiring is
separately evidenced above and is not inferred executed on PostgreSQL.
T065 is now checked; **69/100 tasks are complete, 31 remain**. The unchanged
lifetimes/deadlines, retained full-matrix failures, original branch/draft PR,
reviewer-owned checklists and implement/running driver are preserved.

## T064 continuation: simultaneous callback-target/header boundaries

The existing thirteen-case ordinary TypeScript corpus now also measures exact
16,384-byte callback targets with independently framed 16,384-byte headers
through both actual paths. An unsolicited invalid protocol state at both
admitted boundaries must reach protocol handling and return private 401, not a
transport/parser refusal. A 16,385-byte target or 16,385-byte header must return
private 400 with `identity_request_invalid`. Every combination forbids cookies
and redirects and requires no-store/no-cache/no-referrer.

Initial reports `contract-sqlite-d7461273aa9b4274bfac83672f0e7889.json` and
`contract-sqlite-b10f6a7b98074876b3fb3709de0dc7ab.json` fail the adapted
16,384-byte target with 414. Private numeric/source-only projection identifies
`identity_independent_target_1_16384_414`. The owned test HTTPS ingress used
Kestrel's smaller default request-line cap, before Next or the application
could enforce the approved limit. That fixture now uses the native host's
16,384-plus-32 request-line framing allowance. Product limits, Node parser
settings, timeouts, TLS trust and cleanup are unchanged. Safe TypeScript failure
projection additionally recognizes only fixed target transport/size/status
indices; it never includes query values or raw assertion text.

`dotnet build Kofte.slnx --no-restore -c Release -warnaserror`,
`dotnet format Kofte.slnx --no-restore --verify-no-changes --verbosity quiet`
and
`dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityParityTests' --filter-method '*ActualHttpsAdapterAndSeparateActorRetainTheSameIdentityThroughNominationAndSignOut' --minimum-expected-tests 1`
all pass in `contract-sqlite-55c900f25e614448b006df3c0a146bfa.json`.
That actual parent fact requires all thirteen ordinary cases and the designated
actor case, including successful actual OIDC/enrollment/sign-out, not merely
the new refused callback requests. Contract strict types and lint also pass.
The new simultaneous PostgreSQL counterpart still needs hosted execution;
T064 remains unchecked and progress stays **69/100**.

The unchanged focused reproduction
`dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteOidcBudgetTests' '*SqliteEnrollmentTests' --filter-method '*DeliveredInvalidEvidenceCannotCreateAUserSessionOrCommittedLoginEvent' '*DeliveredBrowserLoginRateRefusesTheEleventhAttemptBeforeProviderOrIdentityEffects' '*EnrollmentChecksCurrentCeremonyAndExactNomineeWithoutReopeningHistory' '*ActualStoppedReplacementRefusesTheOriginalSecretAndConsumesOnlyTheNewGeneration' --minimum-expected-tests 21`
passes all twenty-one cases in
`integration-sqlite-337e6f1acafb481981a6f690b4968273.json`. These are cases
named by recent hosted failures; this unchanged local pass does not diagnose
those failures or justify removing their deadlines/assertions. No speculative
host scheduling change is made.

## Implementation diagnostics: private native failures retain source locations

The completed `dfb26a7` native platform contracts pass on both providers in
716.073 seconds (`contract-both-66744f6197dd4ba4be26b4413687a0a9.json`).
Its complete PostgreSQL host/storage/composition integration also passes
(256.284/550.030/209.316 seconds). Composition-public contracts still time out
at 900.047 seconds; the isolated whole PostgreSQL baseline times out at
900.315 seconds. The completed `89b8b3a` SQLite integration instead names
three failing enrollment facts, including twenty-way concurrency. Neither
source-known test names alone nor the unchanged local twenty-one-case pass
diagnoses those assertions. No scheduling/deadline/contender change is made.

The existing private bounded xUnit projection now extracts at most 32
repository-relative source locations from known failed native tests. Its
allowlist comes from actual non-generated `.cs` files in the selected test
project; each positive line must be within that file's current line count.
Passed/unknown tests, unknown paths, absolute private prefixes, zero/leading-zero/
out-of-file lines and raw messages/values cannot enter the location fields.
Private XML and capture deletion remain unchanged. The isolated harness
independently revalidates location lists against the exact selected project's
source allowlist, including cardinality and uniqueness, instead of trusting
the child's self-description. Existing diagnostic fields remain compatible.

Three focused tests first fail for the absent projection and then pass after
wiring. An actual temporary native xUnit failure additionally verifies that
the owned child fails, the safe artifact contains only its approved source
location, and its private assertion marker is absent
(`integration-sqlite-ae904fd05afd41069b5d50b4eadce35b.json`). That child failure
is intentional negative evidence, not a passing product test or feature gate.
The temporary test source is removed and
`dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo` passes
again with zero warnings/errors. Repository checking, complete Python discovery
(132 tests) and whitespace pass. This adds actionable evidence for the next
hosted failure, not a retroactive diagnosis or green full matrix. Progress
remains **69/100**, with implement/running and the same draft PR.
## T076-T077: both-provider Development boundary and persisted isolation

The actual hosted evidence now closes these two gates independently of the
unfinished public Development recipe and full-matrix failures.

For T076, `DevelopmentBoundaryTests` executes all 22 invalid configurations
under empty/composed hosting and both unsafe-switch settings: 88 actual startup
refusals per provider. Each refusal verifies its fixed diagnostic, listener
release, verified child/descendant exit, and unchanged prepared storage/keys.
The production identity configuration still requires HTTPS metadata; TLS trust,
synthetic issuer controls and actor probes stay in test projects, not a deployed
test-principal or skip-TLS route. Production Development endpoints are absent.
Actual empty/composed host/Next scope facts run the independent Development
HTTP corpus, including strict caller authority, Origin/CSRF, credential/cookie
namespaces and direct/adapted refusal without effects.

For T077, the consolidated `DevelopmentIdentityTests` uses both real stores,
actual stopped reconciliation and fresh serving workers for OIDC → Development
→ OIDC. Equal subject/display values remain separate exact users/mappings.
Original and renamed old cookies fail at the first protected request in the
new mode. Development cannot enroll with production authority; pending authority
is disabled rather than consumed, while consumed user/generation/timestamp and
all original installation events survive. Production returns its exact original
user/designation and registry revision; no unrelated action is granted.

Inspected actual commands/results:

- Hosted `dfb26a7`, run `40671517373`:
  `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-trait Provider=postgres --minimum-expected-tests 1`
  passes in 256.284s, including the complete PostgreSQL startup matrix.
- Hosted `9244e6b`, run `40675801951`:
  `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-trait Provider=sqlite --minimum-expected-tests 1`
  passes in 243.065s, including the complete SQLite startup matrix.
  Its storage and composition integrations also pass (317.927s/99.787s).
- Hosted `89b8b3a`, run `40674487017`:
  `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-not-class '*Composition*' --minimum-expected-tests 1`
  passes in 722.241s on both providers, including the complete independent
  Development corpus in both actual scopes and the four persisted Development
  cases per provider. The later composition-public phase still times out at
  900.030s; its whole contract job remains failed.

The reports identify actual CI merge revisions
`6441ddf0537079514291247e72595c622bb44257`,
`c66cbbbe516153c20aa060ed23c364187c95e893` and
`07bfa34c8b53edb5968f55dd9ec307dc39e6d9b4`, not just branch heads.
These objects were fetched and compared: production sources, startup matrix and
persisted Development assertions are unchanged. The later fixture-only ingress
framing and safe diagnostic extraction do not change those Development cases.
No new local PostgreSQL success or whole passing matrix is inferred.

T076/T077 are complete: **71/100**, with **29 remaining**. T078's public
`development-isolation` recipe still returns nonzero before implementation;
its intentional initial red is retained as
`contract-sqlite-3362b4847f5246f8ba5b2dbb8e1fa9c7.json`, not a passing product
case. T064 still needs the newest simultaneous production boundary's
PostgreSQL execution. The driver remains **005 / implement / running**.

## T064 complete; T078 public Development recipe increment

The `9244e6b` hosted native platform-contract phase passes in **706.235s**:
`dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --filter-not-class '*Composition*' --minimum-expected-tests 1`.
It executes both providers' actual production native/Next/actor journey,
requiring thirteen ordinary TypeScript cases plus the designated case, including
the simultaneous target/header vector. Both actual empty/composed Development
scope facts also require the complete independent Development case. The safe
artifact is `contract-both-bb3e0925079649dcb785632ba03a43ea.json`, at verified
merge tree `c66cbbbe516153c20aa060ed23c364187c95e893`. The later composition-public
phase still times out at 900.046s. T064 is complete, not the whole contract job.

The new public `development-isolation` recipe adds actual guide-level proof:

- The existing exposure matrix is extracted mechanically into test-owned
  `DevelopmentExposure`, preserving all 22 faults, four scope/unsafe-switch
  combinations, exact diagnostics, five-second exit limits, listener checks,
  unchanged snapshots and verified exits. Its existing xUnit wrapper still
  runs each combination; the public recipe independently runs all 88 refusals.
- A real Development native host, production Next and owned Playwright CLI
  execute the existing selection/sign-out/browser-storage/SSR case. The exact
  user previously observed through `/me` and the separately compiled actor
  must appear in both APIs and the visible browser. The report requires one
  passed case, zero unexpected/flaky/skipped cases and the original 45-second
  owned child bound. No configured external IdP is needed for selection;
  zero provider requests, exact Development namespace and disabled bootstrap
  are checked.
- Independently owned pending and consumed production cases perform stopped
  OIDC → Development → OIDC reconciliation/restarts. They refuse original and
  renamed old cookies, compare equal profiles without merging users, preserve
  exact production mapping/creation and installation events, deny Development
  enrollment and unrelated actions, preserve consumed designation/generation/
  timestamp, and reject original authorization replay.

The dispatcher keeps its existing 120-second recipe bound. The browser workflow
now invokes this recipe on SQLite and PostgreSQL. Safe failure projection admits
only fixed codes, bounded source-line numbers and fixed worker readiness phases;
raw errors, values, prefixes, browser output and native traces stay private.
The native worker emits only configured/owner-opened/built/mapped/serving phase markers to
its already-private capture; a failed readiness check stops the owned child,
then projects its last fixed marker without extending its 15-second deadline.

Actual evidence, with original failures retained:

- Initial missing recipe refuses in
  `contract-sqlite-3362b4847f5246f8ba5b2dbb8e1fa9c7.json`.
  Initial browser wiring also refuses: Playwright's anchored grep selects zero
  cases, then exact Development origin validation rejects a trailing-slash
  fixture value. Only fixture selection/origin construction is corrected;
  no product validator, deadline or TLS rule changes.
- `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteDevelopmentBoundaryTests' --minimum-expected-tests 4`
  passes all four cases / 88 actual refusals in
  `contract-sqlite-03b9692378ef419b8dd4bdb957e96c5f.json`.
  Its following five-case native selection fails one mode-transition fact;
  that whole report remains failed.
- `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*SqliteDevelopmentIdentityTests' '*SqliteIdentityParityTests' --filter-method '*ActualStoppedModeTransitionsPreserveEqualProfileNamespacesAndConsumedProductionAuthority' '*ActualHttpsAdapterAndSeparateActorRetainTheSameIdentityThroughNominationAndSignOut' --minimum-expected-tests 3`
  passes both mode-transition cases and the complete production HTTPS corpus in
  `contract-sqlite-32e3d9e197d24cadb65f40a5f0846c6e.json`.
- `dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario development-isolation --provider sqlite`
  passes in `contract-sqlite-f2a5e51a44a049d3a7ff636bc44ec63e.json`.
  An independent private parent validates the exact output schema, correlation,
  provider/scenario and complete frozen set of **135 unique named observations**,
  not just an assertion count. A prior transient fresh-client diagnostic probe
  is removed before this result; it is not shipped or cited as a fix.
- Earlier `identity_fixture_child_readiness_transport_ConnectionError` failures
  remain failed, including
  `contract-sqlite-f0c9c4a741a04ce68dcfc34b20bb5b62.json` and
  `contract-sqlite-521f2f84f23c404da0c822e47deef3d5.json`. The subsequent passing
  selections do not establish their cause or justify raising a budget.
- The final exact-namespace recipe first reproduces
  `identity_fixture_child_readiness_transport_ConnectionError_owner_opened` in
  `contract-sqlite-dcafb4b8df814157a0b5fe325e833b0c.json`: its child opens the
  owned storage but does not reach serving. Additional fixed built/mapped
  markers refine future evidence without changing the startup or readiness
  deadline. The focused two-case stopped mode-transition command then passes
  in `contract-sqlite-d7a1d02bded142708299d41014f64f71.json`.
- The final public SQLite command with exact Development mapping namespace,
  issuer and subject assertions passes all **135 unique independently frozen
  observations** in `contract-sqlite-bc81a9982fe34424b4810b1951068465.json`.
  Full Release warnings-as-errors build passes with zero warnings/errors.
  These results do not diagnose or erase the retained readiness failure.

T064/T076/T077 bring progress to **72/100**. T078 remains unchecked until the new
public PostgreSQL recipe and its focused gates execute. The earlier full-matrix
integration/readiness/timeouts are not waived; operations and final evidence
remain unfinished on the same draft PR and implement/running stage.

## T068 and T078: generated contracts and complete public Development proof

Published `c5e65f2d2a697a2e04d9982e74293b124a3f0be7`, run `40684022214`,
browser job `122801679572`, passes all twelve actual HTTPS/Development recipes:
six scenarios on each real provider. Its completed log is captured privately.
An independent parent validates the exact eight-field output schema, generated
correlation, provider/scenario, passed outcome and all **135 unique frozen named
Development observations per provider**, not only their count. The actual
tested merge is `0b3f0e0bdf0e15b77335927eac99b80a8dae6c61`, verified against
the checkout log and fetched Git object.

`dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario development-isolation --provider sqlite`
and its `--provider postgres` counterpart both pass. This closes T078/US4.1-3:
all 88 exposure refusals/provider; explicit visible synthetic selection with
exact browser/API/separately compiled actor equality; separate pending/consumed
mode transitions, exact original mapping/history/designation and cookie refusal.
The previously passing focused configuration/storage/HTTP/browser prerequisites
and current unit/quality/startup jobs remain separate evidence. Other jobs are
still running at this observation; no full-matrix pass is inferred.

T068 now also closes from the independent existing current-identity C#,
TypeScript HTTP and package shape assertions: exact required fields/version,
nullable safe display text, both admitted modes, nonempty ID, boolean designation,
safe additive response tolerance and strict mutation requests. The original
health schema is compared structurally, not replaced by a new baseline.
Actual current commands:

- `npm run generate --workspace packages/bootstrap-contract` — passes; generated
  types have no diff.
- `npm run contract:check` — passes; accepted bootstrap operations/components
  and generated types match.
- `npm run test:unit --workspace packages/bootstrap-contract` — all four
  health/identity shape cases pass, including each omitted required field,
  null/additive values and invalid version/mode/text/ID/designation.
- `npm run typecheck --workspace packages/bootstrap-contract` — passes.
- `dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-class '*SqliteCurrentIdentityContractTests' '*SqliteIdentityParityTests' --filter-method '*EveryRequestProjectsTheFreshDurableProfileAndDesignationIntoApiAndReadOnlyActor' '*DeliveredCurrentIdentityHasOnlyTheRequiredSelfFieldsAndNeverChallengesAnonymousClients' '*ActualHttpsAdapterAndSeparateActorRetainTheSameIdentityThroughNominationAndSignOut' --minimum-expected-tests 3`
  — passes all three, including the complete ordinary/designated HTTPS
  TypeScript corpus and authenticated private non-personal SSR assertions.
- `dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*HealthEndpointTests' 'Kofte.Host.IntegrationTests.OpenApiTests' --filter-not-class '*Composition*' --minimum-expected-tests 4`
  — passes all four health/Development publication/Production-Staging absence
  cases. Both native selections are recorded in
  `contract-sqlite-e91860003c464ed3b597969768bdb84a.json`.

The existing consolidated `CurrentIdentityContractTests`, `OpenApiTests` and
HTTPS TypeScript corpus own these assertions; no duplicate project/test file
is introduced just to match a proposed filename.

## T066-T067: strengthened production browser isolation, execution pending

The actual session browser now additionally compares both users against
direct and adapted identity, requires authenticated SSR to contain neither ID,
and observes every subsequent/current document mutation after switching for
the previous user's ID. It uses actual responses, not fabricated identities or
browser storage. Explicit invalid Origin with a genuine current CSRF token and
an ungranted-action request must return private 403 without clearing the valid
user. A new fixed test-only ingress alias reaches the existing test-only native
ungranted probe. Node browser-context API calls trust only the fixture's existing
private CA through `NODE_EXTRA_CA_CERTS`; browser TLS verification and all
production configuration remain unchanged.

Initial local `session-boundary` execution is **failed**, not proof that the
new assertions ran: `contract-sqlite-9739be769b9c4bc2856305f9062cc619.json`
reaches `identity_browser_stage_signed_out` then its original 45-second child
deadline, with zero authorization/token requests and zero session controls.
This is the existing pre-login browser limitation, not an established fault in
the new isolation assertions. No timeout or coverage is weakened. The new
test-only alias is subsequently wired; fresh hosted execution remains required.
Browser/contract lint, type checks and changed-file Prettier all pass. Full
Release warnings-as-errors build and native format verification pass.

Progress is **74/100, 26 remaining**: T066/T067/T069/T070 plus T079-T100.
T078 now unblocks operations/recovery on this same branch and draft PR; the
driver remains **005 / implement / running**, without acceptance or merge.

## T066-T070: strengthened both-provider session/browser evidence

The pending execution above now has actual hosted proof, not a local passing
rerun. Run `40685809468`, browser job `122807740299`, tests published head
`49ef96e47aee5972c420257161205f9f799c8eea` through actual merge
`f50722bbea4a6181c22bd85d10d879c50fe099d5`. Its browser job passes all twelve
public HTTPS recipes. A private independent parent validates the exact
eight-field schema, generated correlation, successful outcome and complete
frozen catalogs: nineteen ordered `session-boundary` observations and 135
unique `development-isolation` observations on **each** real provider.

The exact delivered session commands pass:

```bash
dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario session-boundary --provider sqlite
dotnet tests/contract/Kofte.Identity.Consumer/bin/Release/net10.0/Kofte.Identity.Consumer.dll --scenario session-boundary --provider postgres
```

These execute the strengthened actual browser file, including both users'
direct/adapted identities and non-personal SSR, post-switch DOM mutation
monitoring, genuine current CSRF with invalid Origin, private ungranted-action
refusal, idle and absolute expiry, sixteen activity renewals, terminal old-cookie
refusal, sign-out and lost acknowledgement without automatic retry. The native
parent also verifies exact durable final users/sessions/events; named browser
completion alone is not the storage oracle.

| US3 scenario | Actual evidence |
| --- | --- |
| US3.1 | Direct/adapted anonymous refusals, protected actor refusal and safe fixed problem shapes; no provider HTML/challenge. |
| US3.2 | Ordinary/designated direct/adapted/separately compiled actor equality, two independent users, fresh designation and spoof/credential corpus. |
| US3.3 | Ungranted action denied for ordinary/designated actors; actual browser invalid-Origin/current-CSRF refusal leaves the valid session intact; full independent CSRF/Origin corpus has no protected effects. |
| US3.4 | Actual controlled idle/absolute expiry, renew/revoke race, next-request saved-cookie denial, visible stale-state clearing and explicit re-entry; uncertain acknowledgement is not known revocation. |
| US3.5 | Original health/OpenAPI four-case selection and unchanged generated health contract pass; authenticated SSR/no-store and two-user DOM checks prevent personal cached/SSR reuse. |

Focused prerequisite evidence is retained above: complete thirty-case SQLite
session/sign-out selection; unchanged both-provider native session/storage
gates; current-user/HTTPS three-case and original health/OpenAPI four-case
selections; regenerated contracts and four package shape tests. At this
assessed hosted head, `unit`, `browser`, `privacy`, `quality` and `startup`
also pass. This closes T066/T067/T069/T070, not the complete feature or matrix.
Immediate upstream IdP revocation and a general permission engine remain
outside 005; first-administrator designation grants no module permission.
The earlier local 45-second pre-login browser failure remains failed.

## T079: consolidated real upgrade and read-only preservation

The planned new `IdentityUpgradeTests.cs` would duplicate existing actual
provider cases. Coverage is consolidated in `IdentityFormatTests.cs`,
`IdentityPreparationTests.cs` and `IdentityStartupTests.cs` instead:
independent accepted migration-1 fixtures, clean v0 refusal followed by explicit
registry upgrade and identity preparation, populated two-migration Alpha plus
stateless module preservation, unchanged allocator/owner/marker/revision/data/
history, original migration digest, transactional rollback, current retry,
unknown versions and physical/history/object tampering, and both empty/composed
read-only startup without provider discovery or storage/key changes.

Run `40684022214` tests actual merge
`0b3f0e0bdf0e15b77335927eac99b80a8dae6c61`; downloaded exact provider reports
record these commands and all three integration phases passing:

```bash
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-trait Provider=sqlite --minimum-expected-tests 1
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-trait Provider=sqlite --filter-not-class '*Composition*' --minimum-expected-tests 1
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-trait Provider=postgres --minimum-expected-tests 1
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-trait Provider=postgres --filter-not-class '*Composition*' --minimum-expected-tests 1
```

Host/storage phases take 203.220/269.834 seconds on SQLite and
243.765/535.670 seconds on PostgreSQL. Composition phases also pass in
72.832/195.636 seconds. The relevant three source files and shared
`IdentityFixture` are independently compared unchanged from that tested merge.
Fresh SQLite selections also pass in
`contract-sqlite-b5a24545ed8f4bc38824d8de1b30d3bd.json`:

```bash
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityFormatTests' --minimum-expected-tests 20
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityPreparationTests' '*SqliteIdentityStartupTests' --filter-method '*ExplicitPreparationRequiresRegistryUpgradeAndPreservesCurrentStateOnRetry' '*BothEntryPathsRequireExplicitPreparedIdentityAndServeReadOnlyWithoutDiscovery' '*ProductionOidcStartupNeedsNoDiscoveryAndPreservesPreparedStorageAndEncryptedKeys' --minimum-expected-tests 5
```

These are actual passing minimum selectors; no unextracted total is invented.
The newer `49ef96e` SQLite storage/composition phases also pass
(321.601/101.732 seconds), but its host phase fails one separate enrollment
replacement login at `EnrollmentTests.cs:213` (calling sites 158/181).
An unchanged focused reproduction passes in
`integration-sqlite-4e7b9a5bf67a486d8c8a432c18a0b8ed.json`:

```bash
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteEnrollmentTests' --filter-method '*ActualStoppedReplacementRefusesTheOriginalSecretAndConsumesOnlyTheNewGeneration' --minimum-expected-tests 1
```

This neither diagnoses that callback failure nor makes its hosted run pass.
No bound, assertion, contention or migration byte changes. T079 closes on its
own exact preservation evidence; full integration/final gates remain open.

## T080-T081: recovery-v2 backup and actual security verification in progress

New inherited provider regressions first fail compilation with five genuine
missing-format/summary/snapshot API errors. The implementation then adds
`kofte-storage-recovery-v2` exclusively for actual platform format 2, retaining
recovery-v1 exclusively for actual format 1. V1 serialization still omits
identity; even an explicit `identity:null` on a v1 input remains rejected.
Strict unknown/duplicate/null handling, artifact/directory inventory, all module
assignments/history and physical schema/constraints remain in the verifier.

The private summary contains schema/security revision, epoch, bootstrap state,
consumed generation, six exact row counts and a SHA-256 digest. The digest
streams all eight actual tables in fixed primary-key order, with domain/table/
column and length/null/row/count framing. Protected ticket bytes stream through
an 8-KiB buffer rather than loading complete tickets/events into memory.
Text preflight rejects corrupt oversized cells before materialization.
Metadata ownership, exact physical schema/history, one-to-one mappings,
consumed enrollment-event correspondence and event revision bounds are read
from actual data, not accepted from the manifest.

This scan is explicit stopped recovery work, not added to ordinary startup or
registry inventory. Borrowed read queries reuse the existing bounded connection/
progress handling without introducing an uncancellable transaction rollback.
Both actual test-owned backup producers capture/verify the summary and recheck
source identity state while retaining complete platform/all-module artifacts.
No key/client/certificate secret is added to recovery artifacts or logs.

The new tests use actual prepared databases, a consumed user/mapping/history,
a native session with a protected 24-KiB opaque payload, and a populated
two-migration module. They reject wrong format/digest/counts/consumed history
and actual profile/ticket mutations even after artifact rehashing, with the
same counts and security revision. The initial SQLite runtime failure was a
test seed's non-roundtrip timestamp; correcting its provider-specific stored
UTC representation yields passing real execution without changing product
timestamp parsing.

Until T082 supplies outside-snapshot quarantine and stopped reconciliation,
the existing complete-set restore paths **explicitly refuse identity-bearing
sets before modifying a target**. A new inherited guard regression verifies
that refusal. This is safe partial delivery, not a claim that identity restore
is implemented. Decision, crash/resume and complete recovery acceptance
obligations keep T080/T081/T082/T085 unchecked.

Actual final SQLite regression selection passes in
`recovery-sqlite-2c20f46143034a828768ccba51e97693.json`:

```bash
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityRecoveryTests' '*RecoverySetTests' '*SqliteIdentityAdmissionTests' '*SqliteIdentityFormatTests' --minimum-expected-tests 45
```

It includes the new snapshot/streaming/tamper/restore-refusal cases, retained
complete v1 backup/restore and strict shape cases, physical format preservation
and borrowed-store admission regressions. Earlier narrower SQLite passes are
retained in `recovery-sqlite-e68fb37a0a8a4aa6af3f47f8ef914208.json` and
`recovery-sqlite-94bc26e313e94300861cb40ec3813b9b.json`.

The fresh actual PostgreSQL selection fails during owned fixture creation,
at `IdentityRecoveryTests.cs:27`, before the new product logic:

```bash
KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE=true dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*PostgresIdentityRecoveryTests' --filter-method '*CompleteFormatTwoBackupBindsActualConsumedIdentityAndPopulatedModuleState' --minimum-expected-tests 1
```

`recovery-postgres-9ce5437b63b14e2b9980dd76871d636b.json` retains that failure.
It is not a PostgreSQL pass or independent new diagnosis of the previously
confirmed local network-pool exhaustion. Shared Docker resources are untouched.

Progress is **79/100, 21 remaining**: T080-T100. The original branch/draft PR,
read-only checklists and **005 / implement / running** ledger remain unchanged.

The required repository checker passes, exact Python discovery passes **132
tests**, and `git diff --check` passes. Full Release warnings-as-errors build
and native format verification pass. Task parsing verifies 100 unique tasks,
79 checked and every checked prerequisite satisfied. No dependencies, locks,
original migrations, reviewer-owned checklist or acceptance state changed.

## T087: bounded stopped maintenance, provider proof in progress

The actual stopped-command and provider regression tests precede implementation.
The first build fails on the two missing `IdentityMaintenance` API references
and a nullable test parameter; the latter is corrected in the seed SQL rather
than weakening production parameter typing. An initial namespace-resolution
build error is corrected with an explicit infrastructure type reference.

`--identity maintain` now uses the existing admitted supervisor, exclusive
stopped scope and 120-second/116-second envelope. The worker dispatches it and
its fixed diagnostic action explicitly; unsupported operations cannot fall
through to session revocation. A single native owner transaction reuses the
existing session-evidence, login-attempt and admission cleanup helpers. Each
category deletes at most 128 eligible rows; the command never adds a sweep,
background job or provider request. Session evidence must be session-class and
at least 30 days old. Attempts must be consumed or expired; windows must expire.
The existing login/admission paths now call the extracted same SQL helpers.

The inherited real-provider regression seeds 129 expired session records,
expired pending/consumed attempts, an unexpired consumed attempt, live pending
attempt/windows, recent session evidence, old installation evidence and the
existing consumed designation/user/mapping/active protected session. The first
explicit call removes exactly 384 rows, the second exactly four; only eligible
data disappears. The independent actual security snapshot proves unchanged
epoch, consumed generation/designation and retained live/installation counts.
The identity revision advances transactionally; registry/module authority
does not. Actual supervised-command execution also verifies its safe operation
projection and prepared state. A real held host lease refuses `maintain`
alongside the previous stopped commands without changing metadata.

Final SQLite shared-regression execution passes in
`recovery-sqlite-d67654aacc464863a6786be35aa7ed69.json`:

```bash
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityRecoveryTests' '*RecoverySetTests' '*SqliteIdentityAdmissionTests' '*SqliteIdentityAttemptTests' '*SqliteIdentityEventTests' '*SqliteIdentityFormatTests' --minimum-expected-tests 45
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityPreparationTests' --minimum-expected-tests 2
```

The earlier focused actual SQLite command/batch/held-host proof also passes
in `recovery-sqlite-2a4d07f4d7294771af16e66e946f5afb.json`. That earlier
batch corpus precedes the final additional expired-pending/unexpired-consumed
controls. Full host unit regression execution passes in
`unit-none-9c31d319386a468491a8698346f00c05.json`:

```bash
dotnet test --project tests/unit/Kofte.Host.Tests --no-restore --no-build -c Release --minimum-expected-tests 1
```

Full Release warnings-as-errors build and native format verification pass.
The repository checker, exact Python discovery (132 tests) and whitespace
check pass. These local runs exercise this increment on the `f52150f` baseline,
not the unchanged published tree. New PostgreSQL maintenance execution is
pending; the earlier owned-fixture creation failure remains failed and is not
a pass or a fresh diagnosis. T087 therefore remains unchecked, progress stays
**79/100, 21 remaining**, and the same feature branch/draft PR and running
implement ledger continue unchanged. No dependencies, original migrations,
reviewer-owned checklist or acceptance state changed.

## T080-T082: outside-snapshot quarantine and strict decision binding in progress

The new real-provider restore regression first fails compilation with four
missing quarantine/restore API errors. Actual SQLite execution then refuses
the test's public parent directory. The test is corrected to provide an owned
private parent, matching the real key-directory contract; no product filesystem
permission check is weakened. A test namespace import is also corrected.
Two further genuine missing-decision API errors precede its implementation.

The separately named test-owned complete-set restore paths now support fresh
quarantine targets while retaining legacy identity-bearing restore refusal.
Before target copy/import, a new separately owned private key directory gets a
flushed `identity-restore-required` marker. It retains bounded exact original
manifest bytes, including their SHA-256 binding. Only after actual restored-set
verification does an atomic replacement retain the verified target manifest.
This keeps original recovery-point binding separate from SQLite backup API
outputs' rewritten physical hashes. No keys/secrets enter the snapshot.
Overlapping backup/managed roots and pre-existing key directories are refused.

Strict gate parsing rejects unknown/duplicate fields, changed recovery-set/
installation binding and format. The existing recovery parser is reused, with
bounded exact reads rather than an unbounded async document read. Supported
startup already refuses the marker; all six existing supervised identity
commands now explicitly refuse it before any ordinary mutation/key preparation.
Their actual private state snapshots remain unchanged.

The new private decision reader accepts only string `version:"1"`,
`recoverySetId`, `installationId`, `expectedManifestSha256` and
`priorEnrollment`. It binds canonical IDs and exact original manifest bytes,
not the rewritten target manifest. Only `known-consumed`,
`known-never-enrolled` and `not-established` are admitted. Unknown/duplicate/
missing fields, stale point/binding, unsupported history/version, non-private/
symlinked files, more than 32 KiB and invalid UTF-8 are refused. Parsing alone
does not mutate authority or remove a gate.

The inherited regression executes actual complete-set restore, then compares
the independently read full identity snapshot to the original consumed
user/mapping/event/active-session state. It verifies the only key-directory
entry is the external marker, not copied key material. Five gate faults,
all three valid attestations, eight invalid decisions and actual permission/
symlink/size/encoding refusals are exercised without another database restore.
The actual host regression requires `identity_reconciliation_required` from
prepare, configuration reconciliation, session revocation, maintenance and
both bootstrap commands, with exact unchanged persistent state.

Final focused SQLite execution passes in
`recovery-sqlite-6a7e1d23f37c43568e4d611e07cf4139.json`:

```bash
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityRecoveryTests' --filter-method '*CompleteIdentityRestoreRemainsQuarantinedOutsideTheSnapshotWithItsExactRecoveryPointBinding' --minimum-expected-tests 1
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityPreparationTests' --filter-method '*PendingRestoreCannotBeChangedByOrdinaryStoppedCommands' --minimum-expected-tests 1
```

The preceding retained v1/format/recovery-privacy/read-only startup selection
passes in `recovery-sqlite-9dc180b771b44d2087b027970b14a681.json`:

```bash
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityRecoveryTests' '*RecoverySetTests' '*SqliteRecoveryDiagnosticsTests' '*SqliteIdentityFormatTests' --minimum-expected-tests 45
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityPreparationTests' '*SqliteIdentityStartupTests' --minimum-expected-tests 6
```

Earlier passes remain in `recovery-sqlite-b413599468d54aafbcb6915cda4bdc9a.json`
and `recovery-sqlite-6a6eed4356244dbe94c6be068b650ac3.json`; the initial
private-parent failure remains failed in
`recovery-sqlite-081bce24fda24c82a5548fa121254ba1.json`.
The final focused corpus includes the later private-file and six-command
controls; earlier broad passes are not relabeled as executing those controls.
These runs assess this increment on the `20d751c` baseline.

Fresh protected key provisioning, stopped `reconcile-restore`, durable decision/
event commit, unknown-history locking and interruption/resume are **not yet
delivered**. Neither parser success nor a gated copy clears quarantine or
reopens nomination. New PostgreSQL quarantine execution remains pending.
T080/T081/T082/T085 stay unchecked; progress remains **79/100, 21 remaining**.
No stage, human acceptance or full-matrix pass is inferred.

### Actual hosted f52150f provider evidence and newer failures

Finished PostgreSQL job **122821224128** in run **40689749980** executes actual
merge **`30bcebaa8f29636109774d1c28addefc5ab59414`** with published parent
**`f52150f1624be32e61129917e96cb7653b3ba3ae`**. The tested recovery source blob
`94fc175a4d0970545c5eeda6c7ace2048df8d989` exactly matches that published parent;
it contains the eight new inherited recovery-v2 cases, not this later quarantine
increment. Safe report `integration-postgres-629eb166aad344578201c3f48314545b.json`
passes host/storage/composition phases in **288.375/640.486/235.622 seconds**.
Exact storage selection, covering new and retained real-provider cases:

```bash
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-trait Provider=postgres --filter-not-class '*Composition*' --minimum-expected-tests 1
```

Thus the new f52150f backup/snapshot/tamper/legacy-refusal implementation has
actual PostgreSQL proof; the earlier local fixture-creation failure remains
failed and does not become a pass or a fresh network-pool diagnosis.
The same tested merge's SQLite storage/composition phases pass in
**317.055/97.904 seconds**. Its host phase fails
`SqliteOidcBudgetTests.DeliveredDnsChangesAreReadmittedBeforeTheRealTokenConnection`
at **OidcBudgetTests.cs:153/177**, the initial/corrected-login 303 assertions;
the safe report does not establish unexpected statuses or cause.
This is distinct from the earlier 49ef96e enrollment callback assertion.

New run **40691297842** for `20d751c` passes native browser verification
(`browser-none-a4696593b9bb4507b5b46d1757f1943e.json`) but subsequently fails
public SQLite `enrollment-race` after `oidc-login`, `trust-refusal` and
`nominated-enrollment` pass. Later recipes do not run. No actual inner failure
cause is established by its bounded recipe failure receipt.
Its new maintenance PostgreSQL evidence remains pending. Existing and newer
failed/incomplete full gates are preserved, not waived or weakened.

### T087 closure: actual both-provider maintenance evidence

Run **40691297842** now completes PostgreSQL integration job **122826489540**
and both-provider recovery job **122826489775** successfully. Both assess actual
merge **`c5041de1f62a640d1e12c4300d4d090e95422833`**, whose published parent is
**`20d751cce50db6ce0efc5b607cdf2e66b0dc15b3`**. Actual tested recovery/preparation
source blobs `5d0cbc3455ef66dda62b205661e896d11e923feb` and
`42c05fb71dcf0d9cbaa5755ebb05fef2ffa5b686` exactly match that parent; they include
the new maintenance tests, not this later quarantine increment.

`integration-postgres-3f468ab039614788a92f3a63146ed829.json` passes
host/storage/composition phases in **321.366/699.605/243.035 seconds**.
`recovery-both-785c2cff0fbc4920b4910f5adfed41ff.json` passes actual
SQLite/PostgreSQL storage-recovery phases in **225.341/187.353 seconds**.
Exact executed selectors:

```bash
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore -c Release --filter-trait Provider=postgres --minimum-expected-tests 1
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-trait Provider=postgres --filter-not-class '*Composition*' --minimum-expected-tests 1
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-query '/[(Suite=recovery)&(Provider=sqlite)]' --minimum-expected-tests 1
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore -c Release --filter-query '/[(Suite=recovery)&(Provider=postgres)]' --minimum-expected-tests 1
```

Together with focused local actual SQLite command/lease/batch evidence, these
close T087 on real provider behavior, not source presence or a prior generic
matrix. **80/100 complete, 20 remaining**: T080-T086 and T088-T100.
T080/T081/T082/T085 remain open for complete quarantine/reconciliation/
interruption obligations. The newer browser recipe failure and other historical
failures remain failed; this is not a full-matrix pass or stage completion.
The branch/draft PR, running implement ledger and reviewer-owned markers remain
unchanged. Full Release warnings-as-errors build, native format verification,
repository checker, 132 Python tests and whitespace checks pass for this
increment; no dependency, original migration or acceptance state changed.

## T080-T085: explicit restore reconciliation and durable retry in progress

The next real provider regression first fails compilation with three missing
`IdentityRestoreStore` references. Its initial build failure is retained in
`recovery-sqlite-38abb2b5099244d0bcb0c223c7c45197.json`; direct compilation
confirms those exact errors. The new supervised command then fails before
delivery (`recovery-sqlite-b95691121e1b412a8dd39af35a26f7c8.json`), and the
independent v1 command fails before upgrade/resume support
(`recovery-sqlite-ed3644f6dd5a4d1f92cd59a560ca48ad.json`).

The frozen `--identity reconcile-restore --decision-file ABSOLUTE_PRIVATE_FILE`
grammar is now wired through the original admitted supervisor/116-second
worker/exclusive stopped scope. The outside-snapshot control marker retains
the original exact manifest and a private durable plan: exact decision, trusted
configuration, fresh epoch/ring IDs, validated protected key-file hashes and
independently verified committed snapshot. Atomic flushed state transitions
and explicit immutable-plan checks prevent a different decision/configuration
from replacing an interrupted one. No nominee/replacement-admin field exists.

Fresh protected keys are prepared in an explicitly claimed private sibling
staging ring, with a preselected ring ID. Normal ring opening and ordinary
preparation retain strict inventory/ownership; only this stopped gated path
can resume its owned empty staging directory. Bounded verified copies publish
keys before their owner, with exact partial-file retry checks. The gate remains
through key preparation and database mutation. A private external receipt
replaces it only after independent committed-state and fresh-key verification.
The protected staging ring/receipt remain outside ordinary snapshots for
operator inspection/custody. Completed receipt verification is read-only,
refusing subsequent changed state rather than replaying recovery.

One host-store transaction checks the original full security snapshot/revision/
epoch, revokes active sessions and pending attempts, clears all restored
admission windows/pending nomination, advances epoch/configuration/revision and
appends `restore_reconciled`. It preserves every recovered event instead of
performing implicit retention, plus users/mappings/designation/module state.
The existing revocation helper is shared with ordinary stopped revocation.
Consumed state survives every attestation. Verified unconsumed state permits
disabled only with `known-never-enrolled`; unknown/lost history or existing
`recovery_locked` never reopens nomination. Retry independently proves the
same epoch/revision/record and invalidated authority, not a second mutation.

Recovery-v1 reconciliation explicitly verifies/upgrades the accepted old schema
and complete module inventory. A crash after that upgrade commits but before
the prepared snapshot is journaled is reconciled only against exact initial
format-2 metadata/disabled state/empty identity rows/one `keys_prepared` record
and the frozen configuration. It then persists that independently read
snapshot before the restore decision transaction. Original module allocation,
history and registry revision remain unchanged; no Down migration/reset is used.

Initial crash execution exposes two test-owned phases missing from the barrier
allowlist, then an actual `operator_before_commit` retry refusal
(`recovery-sqlite-498aad89c6f34685a0402a620e3182de.json` and
`recovery-sqlite-9b8a46261acf48769d3f38ef1bc276a7.json`). Only fixed phases/
diagnostic codes are extracted from transient private output. The strict clean
backup reader rejects SQLite's inactive crash journal. Explicit stopped retry
now separately verifies the owned read/write connection, like existing startup;
the clean backup reader's sidecar refusal is not relaxed. The same killed-worker
case then passes. No timeout, contender count or cleanup guarantee changes.

Final expanded SQLite storage selection passes in
`recovery-sqlite-caa0b55285f746738993523fc6395e1b.json`:

```bash
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityRecoveryTests' '*RecoverySetTests' '*SqliteRecoveryDiagnosticsTests' '*SqliteIdentityFormatTests' '*SqliteIdentityAdmissionTests' '*SqliteIdentityAttemptTests' '*SqliteIdentityEventTests' --minimum-expected-tests 45
```

Its six historical-state cases independently execute consumed/disabled/pending/
locked restore, all three attestations, durable attempt/session/admission
invalidation and unchanged retry. The consumed case includes actual complete
module backup/restore and exact original Alpha record/note/ID/registry revision
after identity reconciliation. Original profile/mapping/designation and every
recovered event remain. A different planned epoch is refused. Retained strict
manifest/format/schema/history/private-file/privacy/admission/event cases pass.

Full SQLite preparation/startup and retained host units pass in
`recovery-sqlite-3465ce874d064ac4b5d35334b210133f.json`:

```bash
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityPreparationTests' '*SqliteIdentityStartupTests' --minimum-expected-tests 15
dotnet test --project tests/unit/Kofte.Host.Tests --no-restore --no-build -c Release --minimum-expected-tests 1
```

Actual v2 workers are killed after intent flush, partial key publication,
before database mutation, immediately before/after commit and after verified
result persistence. Exact journaled epoch/ring and every already-published key
byte survive explicit retry; changed decisions are refused both while gated
and after activation. Separate v1 success and committed-upgrade-before-journal
retry pass. Supervisor/root/descendant exits are independently verified.
Earlier narrower passes remain in
`recovery-sqlite-eafafc843dc94303b13b9c50b1688a4f.json`,
`recovery-sqlite-0df7ec5c40504032821cb876da467c65.json`,
`recovery-sqlite-b52bad836a8847f2baf5596e6a203620.json` and
`recovery-sqlite-efe1cfbe000147a18dac096be760b167.json`.
These runs assess this increment on the `f7454ea` baseline, not its unchanged
published source. No native test total is inferred from selector minima.

New PostgreSQL reconciliation, further partial-write/key/certificate/authority
cases and complete public operations/final gates remain pending. T080-T086
stay unchecked; progress remains **80/100, 20 remaining**. No stage completion,
acceptance or full-matrix pass is inferred.

### Actual published f7454ea CI observation

The finished SQLite integration report
`integration-sqlite-fc9b2356d5a042ba800c2bc5757e7af7.json` passes at actual merge
`36bfcd482e6862284b57d9ff02223b3a4d59f15b`. Browser, quality, unit, privacy and
startup jobs also finish successfully in run **40695013815**. PostgreSQL,
recovery and contract jobs are still in progress at this observation; they
cannot establish this later uncommitted reconciliation increment's proof.

The same run's failure-propagation job **122839348700** fails:
`failure-propagation-none-0d6c4c33c8da4777abe23ee1190967fe.json` reports
`failure_exercise_timeout` for nested `baseline-postgres` at **900.315 seconds**
and its enclosing `failure-exercise-harness` at **1024.19 seconds**. No inner
assertion/cause is established. Earlier hosted failures remain historical
failures; a passing browser rerun does not waive them or this non-green matrix.

The actual tested merge's parents include published
`f7454ea9dff155961879d70af3f06702fef2d171`; recovery regression blob
`d2f3f4a5874f6c08aa07cbc80314f49fcba75861` exactly matches that parent.
The PostgreSQL host phase now finishes failed:
`integration-postgres-0c36c93be0684acc9bb95c5af4868d27.json` records
`PostgresEnrollmentTests.ActualStoppedReplacementRefusesTheOriginalSecretAndConsumesOnlyTheNewGeneration`
at **EnrollmentTests.cs:158/181/213**, in **355.43 seconds**. No unexpected
status/cause is retained. The subsequent storage phase did not execute, so it
does not prove or disprove the new quarantine regression.

Full Release warnings-as-errors build, native format verification,
`python3 scripts/check_repository.py`, exact Python discovery (132 tests) and
`git diff --check` pass for this reconciliation increment. Final task validation
finds 100 unique tasks, 80 checked and all checked prerequisites satisfied.
No dependency, original migration, reviewer-owned checklist, ledger or human
acceptance state changed.

## T083: real key custody, certificate overlap and secret correction in progress

Continued implementation on the original feature branch/draft PR. Progress
remains **80/100**; T083 is not closed without actual new PostgreSQL evidence.
No reviewer-owned checklist, task marker, ledger, dependency or migration changed.

The real encrypted-ring overlap regression first fails in
`unit-none-3a2f64599fe84a5a83f27c54087d897b.json`:

```bash
dotnet test --project tests/unit/Kofte.Host.Tests --no-restore --no-build -c Release --filter-class '*IdentityKeysTests' --filter-method '*CertificateOverlapRetainsOldTicketsWhileNewKeysUseTheCurrentCertificate' --minimum-expected-tests 1
```

Implemented optional strict `keyDecryptionCertificates` with one to seven
private certificate/key pairs beside the unchanged primary encryption pair.
The installed framework's `UnprotectKeysWithAnyCertificate` handles retained
decryptors; `ProtectKeysWithCertificate` still selects only the current pair.
The existing 64-KiB/private-file/path/RSA checks apply to every pair, duplicate
certificates fail closed, and all loaded certificates are disposed on success
or partial failure. Rotation does not change the provider-trust digest.
Revocation/maintenance keep their existing secret-independent subset.
This explicit additive field, rationale and custody limits are documented in
the operator contract and ADR 0007; PEM-chain interpretation is unchanged.
Microsoft Learn's .NET 10 Data Protection guidance was actually consulted;
Microsoft Learn/Context7 MCP tools are not connected in this session.

Strict configuration/real-ring unit tests then pass in
`unit-none-11dc65c66af149b9a1df3ee95b4ae293.json`:

```bash
dotnet test --project tests/unit/Kofte.Host.Tests --no-restore --no-build -c Release --filter-class '*IdentityKeysTests' '*IdentityConfigurationTests' --minimum-expected-tests 50
```

Cases prove old/new protected tickets survive retained-certificate rotation and
restart, missing required decryptors cannot repair a ring, seven retained pairs
are accepted/eight refused, duplicate/unmatched/shared-readable pairs fail,
and explicit revocation can still load configuration after secret loss.
Existing purpose/installation/mode separation, unreadable/foreign/missing/
corrupt/path/private-mode/no-serve-repair cases remain passing.

New inherited `IdentityKeyTests` execute **actual** native OIDC, delivered
nomination/enrollment, real protected requests and stopped operator commands.
The initial SQLite class selection fails only at the new backup test's
incorrect root-only artifact assertion
(`integration-sqlite-2f551266fa074d4b97fc31c3856033b8.json`, line 116).
The test now compares every actual recursive relative artifact path with the
exact manifest inventory; no recovery validation is weakened.

The corrected isolated backup test then fails during worker restart/readiness
in `integration-sqlite-b7cb6a2d75ba433ea7092c6b481b6364.json` and
`integration-sqlite-434931f6f2d04661be60410f06228575.json`.
Transient private XML was scanned only for allowlisted codes/known source
locations. The latter identifies `BackupProductAsync`'s post-backup
`StartWorkerAsync` path; no unexpected status or underlying cause was retained.
The unchanged isolated case later passes in
`integration-sqlite-70249699929445fcb0dfb45fb2d88a75.json`, not proof that those
readiness failures were fixed.

The final **entire six-case SQLite key class and full host unit suite pass**
in `integration-sqlite-a26e2555a5c2438b8bbff475d211a616.json`:

```bash
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityKeyTests' --minimum-expected-tests 6
dotnet test --project tests/unit/Kofte.Host.Tests --no-restore --no-build -c Release --minimum-expected-tests 1
```

Actual live key loss, foreign ownership and hash-consistent undecryptable RSA
ciphertext return 503, issue no new cookie and leave damaged files unchanged.
Exact original custody is restored only while stopped under independently
verified exclusive scope; the same consumed actor works again without changed
mapping/epoch/designation. Actual `revoke-sessions` then advances epoch and
rejects the next protected request. Real expired retained-key rotation with a
new primary certificate and explicit old decryptor creates the second key and
preserves the consumed actor across another worker restart.

The issuer actually changes its accepted client secret: old-process future
code exchange is rejected, while the established actor remains valid. Stopped
private-file correction/restart makes future exchange succeed with the same
user ID, original mapping/creation time, epoch, keys and consumed designation.
The complete actual database backup has exactly the recursive manifest
artifacts and no protected ring/owner/private certificate/client-secret files;
the original encrypted custody and enrolled actor remain unchanged.

The six-case PostgreSQL attempt fails before product execution in
`integration-postgres-c57f7349ecfc4ed4af30733fd7d68dfa.json`.
A narrowed actual fresh-fixture diagnostic preserves
`postgres_docker_command_failed` at `PostgresService.StartAsync`'s Compose-up
boundary (`PostgresService.cs:148`) in
`integration-postgres-1057f7fafb9f418aba40b8630448cfee.json`. It does not expose
raw Docker errors, prove a product failure, or re-diagnose the previously
independently observed network-pool exhaustion. Already-installed native
PostgreSQL server/dump tools are unavailable. No shared Docker resources or
address pools were removed/changed and no unrelated service was adopted.
T083 and the remaining twenty tasks stay open pending full actual obligations.

All local reports assess this uncommitted increment on the `fa4390e` baseline,
not unchanged published `fa4390e` source. No native test total is inferred
from selector minima; the final class has three failure-theory rows and three
facts. No full-matrix, production-readiness or feature-acceptance claim is made.

### Additional published recovery observation

Finished f7454ea recovery report
`recovery-both-792b2c5c6ed74b6bb1e1910062e7b13d.json` passes both native provider
steps at previously verified merge `36bfcd482e6862284b57d9ff02223b3a4d59f15b`.
This proves that published quarantine/decision source, not later reconciliation
or certificate changes. Published fa4390e SQLite host/storage integration now
passes in `integration-sqlite-96f3e712d6a84df3bbdae47800b08377.json` at merge
`e73d602ee74f4f29ed572b249ee5a9133e60f619`; exact parent/source verification and
the new PostgreSQL reconciliation result remain to be observed.

Subsequent exact verification fetches PR 8's tested merge and confirms parents
`3e416813233c9d291eb20cf72d5882dbddf3e1b6` and published
`fa4390ebcdc130a57469721da8f58d9c09d1410a`.
`IdentityRestoreStore.cs` blob `8cca81ed1ca536a17a7e2926d19b73ed41772888`
matches both. The finished PostgreSQL integration report
`integration-postgres-5a99389fe7e1402e9cd4bda60233d42b.json` passes host/storage
integration, establishing actual published v1/v2 reconciliation coverage, not
this later certificate increment. Both native recovery steps also pass in
`recovery-both-47817cd05bac4c5db3a911a10b1b3f3f.json`, but the subsequent
`storage-public-recovery-postgres` step fails at 178.713 seconds without a
retained inner cause. Failure propagation again times out `baseline-postgres`
at 900.315 seconds in
`failure-propagation-none-d37bd7da9be54c1fa602591bb5987485.json`. Neither
successful integration nor the unchanged later native pass waives those
failures or closes remaining restore/authority/public-operations obligations.

An additional focused unit observation explicitly checks the newly generated
encrypted key's embedded current certificate, not only successful decryption
with both certificates present. It passes in
`unit-none-878733a871214befbf7f13d303fac0e9.json` using the initial single-method
command above. Final Release warnings-as-errors build, native format
verification, repository checker, 132 Python tests and whitespace checks pass.

## T084: real partially flushed reconciliation journal recovery in progress

Continued after publishing `1c6448ba985b899e779c6cca93bf492800fe26c5` to the same
draft PR 8; no voluntary implementation handoff or stage advance. Progress
remains **80/100**. T080-T086 and T088-T100 are still unchecked.

Added actual maintenance-worker barriers after creating the empty pending
intent file and after physically writing/flushing half of the intent,
key-inventory and committed-result journal. These are bounded existing
DiagnosticListener checkpoints, with the same test-only nonce/startup-hook/
worker admission and verified root/descendant termination. No deployed bypass,
larger timeout or changed database migration is introduced.

The initial test runs
`recovery-sqlite-e765c85429c649069e216cf7c9daea13.json` and
`recovery-sqlite-f6e0a3aa3b5e4e4ba22ca659cc019074.json` expose the test's exact
`JsonException` assertion, not the core retry: the framework throws a derived
reader exception. After correcting that assertion to `ThrowsAny`, the actual
old incomplete-journal refusal is retained and the regression genuinely fails
at the explicit retry-success assertion in
`recovery-sqlite-18db4e5d517240f8b1c99210b80da858.json`:

```bash
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityPreparationTests' --filter-method '*ActualRestoreCommandResumesARealPartiallyFlushedJournal' --minimum-expected-tests 3
```

Implemented narrow explicit retry in `IdentityRestoreQuarantine.ResumeAsync`.
It now requires the actual installation/scope, verifies live authority before
any cleanup/promotion and preserves the exact current gate fingerprint.
Pending bytes remain private, regular, bounded and completely read. The
maintained non-final-block UTF-8 reader / `JsonDocument.TryParseValue`
distinguishes a genuinely incomplete object from complete semantic corruption
or invalid syntax. Empty/unfinished pending scratch alone may be removed,
after reverified authority and an unchanged-byte reread, with a fixed correlated
diagnostic phase. Complete state still goes through every original strict
schema/manifest/decision/configuration/monotonic-plan check and atomic
promotion. Unknown fields, changed binding and invalid syntax never get
silently replaced. Neither keys nor database sidecars are deleted.

The writer splits and flushes the real journal for honest interruption
boundaries; its file/operation bounds and atomic final rename remain unchanged.
An interrupted first intent has no durably committed plan/published keys/
database effect and is safely recomputed. Once intent is durable, its exact
decision/epoch/ring and published keys remain fixed. An interrupted result
write after the database commit is independently verified rather than
replayed, keeping one revision/event. Official .NET documentation for
`JsonDocument.TryParseValue` was actually consulted; no package was added.

The complete **sixteen-case** v1/v2 SQLite restore selection passes in
`recovery-sqlite-ab3d2a38431144d1a3eb1453aa8c0e0e.json`:

```bash
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityPreparationTests' --filter-method '*Restore*' --minimum-expected-tests 16
```

It includes the prior nine actual v1/v2/committed-upgrade/six-boundary cases,
four genuine empty/half-flushed journal kills and three complete unknown/
binding/syntax refusal-and-trusted-correction cases. Every damaged complete
journal and current gate remains byte-identical after refusal. Corrected
explicit retry preserves previously frozen IDs and all published key bytes;
completed receipt retry remains read-only. Changed decisions still fail when
an intent is durable, and after activation. All children/descendants exit.

Retained actual recovery/security/private-diagnostic and host units pass in
`recovery-sqlite-d3ba4ff7c8574ed9a981b6886f411612.json`:

```bash
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityRecoveryTests' '*SqliteRecoveryDiagnosticsTests' --minimum-expected-tests 20
dotnet test --project tests/unit/Kofte.Host.Tests --no-restore --no-build -c Release --minimum-expected-tests 1
```

The same native storage selection then passes with the additional real
foreign-installation, disposed-scope, cancelled-caller and expired-budget
cleanup refusals in
`recovery-sqlite-e3051861cc1e4ed7a4a3812d926f59cb.json`. It uses actual restored
complete identity data/private quarantine, proves gate/pending bytes untouched
for every refusal, then permits only a new live matching exclusive scope to
remove incomplete scratch while leaving the gate unchanged and active.

These are local results for this uncommitted increment on the `1c6448b`
baseline, not unchanged published source or new PostgreSQL proof. Incomplete
staging-key/initial quarantine/import writes, further scope/deadline/cleanup/
public operations cases and final feature gates remain pending. No broader
arbitrary-corruption resumption, accepted stage, full gate or feature acceptance
is inferred.

### Actual published key-increment CI limitation

Run **40703888949** tests merge
`46614c3c095fc5eb7a06350c8eb27d90c27c27b4` for published `1c6448b`.
Its SQLite host step fails in **275.706 seconds**:
`integration-sqlite-692d1c01176843c4944ec0b03e34dfdd.json` names the existing
enrollment ceremony case, two OIDC-budget cases and new
`KeyFailureCannotRepairItselfOrReplaceConsumedAuthority`. Key-test locations
include **IdentityKeyTests.cs:23/130/147/50**: enrollment setup/initial
login-start assertion, before the ring fault is applied. The unexpected status/
inner cause and individual theory row are not retained; do not assign a
certificate, timing or provider cause. Storage does not subsequently execute.
Quality/unit/privacy/browser/startup pass; new PostgreSQL/key, recovery,
contract and failure-propagation jobs are still running at this observation.
The local green subset does not waive this non-green hosted result.

Final journal review also requires the decision's exact current manifest
binding before any partial-scratch cleanup, even when no intent is durable.
The actual restored-store authority matrix rejects an unbound decision without
changing either file. Fresh final Release warnings-as-errors build, the same
SQLite recovery/storage selection, full host units and native formatting pass
in `recovery-sqlite-1bad02a7ce39445c8285e4c0f56dbda3.json`. Repository checks,
132 Python tests and whitespace checks pass.

Fetched tested key-head merge `46614c3c095fc5eb7a06350c8eb27d90c27c27b4`;
its actual parents are `3e416813233c9d291eb20cf72d5882dbddf3e1b6` and
`1c6448ba985b899e779c6cca93bf492800fe26c5`. Restore-store, login-endpoint and
fixture blobs match the published feature parent. Its completed both-provider
recovery suite now **passes**, including public PostgreSQL recovery, in
`recovery-both-6b7fa764fc994b43b626d64095d8e27c.json`. The separate
failure-propagation report
`failure-propagation-none-dc20cadfa03943ffbff672fe07e49903.json` still fails:
nested `baseline-postgres` reaches the original **900.315-second** bound;
the enclosing harness ends at **1028.559 seconds**. The previously observed
SQLite login/setup failure remains unresolved. Neither unchanged recovery
success nor this later observation erases prior failures or executes the
unpublished journal increment.

## T084/T086: rejected restore authority and safe failure evidence

The verified partial-journal increment is published as
`efac4657bc7a6bbd267ffb6b32e13ec14552d687` on the same draft PR 8; implementation
continues with **80/100** evidenced tasks. Added an actual current-gate change
after a previously read valid quarantine: live matching authority still refuses
partial-scratch cleanup with `Conflict`, preserving the changed gate and pending
bytes. Only explicit test-owned correction restores the original gate before
the legitimate retry. Fresh build, this actual SQLite restored-state/authority
case and native format verification pass in
`recovery-sqlite-79cba38a408b4eb7808a45a4e7084f83.json`:

```bash
dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityRecoveryTests' --filter-method '*CompleteIdentityRestoreRemainsQuarantinedOutsideTheSnapshotWithItsExactRecoveryPointBinding' --minimum-expected-tests 1
dotnet format Kofte.slnx --no-restore --verify-no-changes --verbosity quiet
```

The inherited PostgreSQL case additionally terminates only the actual exclusive
advisory session in its independently owned database/current role, using the
accepted lease-loss test's lock-key/query pattern. It requires failure,
cancelled authority, unchanged gate/pending bytes and a second cancelled refusal,
never automatic reacquisition. It is compiled but not yet executed locally:
the last owned PostgreSQL attempt fails at Compose creation before product
execution. No serving `ShareLock` probe is substituted for maintenance authority.

### Preserve finite native failure details instead of guessing

Previously published key-head `1c6448b` PostgreSQL report
`integration-postgres-01c2db4fbc6d4f2396896380d04f911e.json` is now downloaded.
Host integration fails the OIDC invalid-evidence case at initial login setup
(`OidcBudgetTests.cs:232/34/53`, **453.609 seconds**); subsequent storage and
composition PostgreSQL integrations pass. This does not establish the missing
actual response status or inner cause, nor prove every later increment.

Actual local combined reproduction on the `efac465` baseline, with the new
explicit login-start status assertion, builds successfully but fails the
certificate-overlap case at the restart call
(`IdentityKeyTests.cs:61/68`), not initial login:
`integration-sqlite-941f4639abcf46d682e26a2daa31004c.json`, **676.729 seconds**.
The same unchanged isolated certificate case subsequently passes in
`integration-sqlite-3a3ff9de19d242a2a1bf92e8ff12408b.json`, **22.543 seconds**.
That is an unchanged passing diagnostic attempt, not a root-cause fix.
Hosted journal-head SQLite report
`integration-sqlite-3455afd9720e4c909be5f38107cf23fb.json` also fails key cases
at initial login and certificate restart; later SQLite storage passes.
No provider, certificate, CPU, timing or networking cause is assigned.

Native projection now retains only closed expected/actual HTTP status
categories from complete framework equality assertions in source-known failed
host tests, at most 32 pairs. It also retains exact identity-fixture literals,
closed readiness-status/worker-phase combinations and valid in-file locations
from the known owned identity helper sources. Raw assertion/exception text,
arguments, private checkout prefixes and XML still disappear. Unknown, extra,
successful-test and unrecognized status/code data cannot be published; all
new fields are revalidated in isolated failure-propagation reports. OIDC test
setup now asserts its actual initial HTTP status before the generic fixture
challenge exception can erase it.

The new status-projection regression genuinely fails before implementation.
After implementation, focused runtime/verifier and full Python discovery pass
(**133 tests**). No product flow, timeout, retry, concurrency, endpoint,
migration, dependency, task marker or reviewer-owned checklist is changed by
this diagnostic increment. Original initial-login and readiness failures remain
open; another passing subset is not a full-gate/provider waiver.

All six actual SQLite key lifecycle cases also pass together with the new
finite diagnostic projection in
`integration-sqlite-7eeeae1d623b4f2fad08c6835cc4ffb0.json`:

```bash
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityKeyTests' --minimum-expected-tests 6
```

Final repository checker, complete 133 Python tests and whitespace checks pass.
The Release warnings-as-errors build and native format command above also pass.
These results do not diagnose the earlier combined or hosted failures.

Fetched journal-head tested merge
`517079e4457ad3d53a5d998b143134012e4d2812`, with actual parents
`3e416813233c9d291eb20cf72d5882dbddf3e1b6` and `efac4657bc7a6bbd267ffb6b32e13ec14552d687`.
Quarantine/host restore cases/storage recovery source blobs match the published
feature parent. Its both-provider recovery suite passes in
`recovery-both-8861841bbd8443a1abad45f4a4b28f95.json`, including actual PostgreSQL
storage/public recovery. Failure propagation still times out its nested
PostgreSQL baseline at **900.319 seconds** in
`failure-propagation-none-96c9c50be3a145a3860bc606815b44dc.json`; the enclosing
harness takes **1015.810 seconds**. This assesses published `efac465`, not the
later diagnostic/changed-gate/lost-session test increment. Full provider,
operations and final acceptance obligations remain open.

## T080/T084: actual-state negatives and interrupted key inventory

Continued on the same feature branch/draft PR after published
`ec0177013330e393984f78d74643dfd4344e53cd`; **80/100** task markers remain
unchanged. Five inherited real-provider recovery cases now alter an actual
format-2 CHECK constraint, migration history or consumed-bootstrap history and
rehash artifacts, or remove a module-bearing artifact with/without removing its
manifest entry. Verification must refuse, preserving exact manifest/remaining
artifact bytes and the separately retained removed artifact. SQLite passes all
five. The initial missing-artifact test instead failed fixture setup because its
private retained directory did not yet exist; that fixture error was corrected.
No initial product red is claimed for existing tamper refusal.

Real stopped-worker kills now cover owner creation, complete key material,
complete/half-flushed/empty pending owner inventory, and ready publication.
The half-flushed inventory genuinely prevented explicit retry before the fix:
`recovery-sqlite-da31af1a52954fabaa3e5d3470752369.json`. Recovery now requires
an already valid owned preparing ring, verifies recorded hashes and complete
unrecorded key XML, and reuses the quarantine's bounded JSON-prefix classifier.
Only incomplete unpublished owner inventory can be removed after live authority
and exact unchanged owner/pending-byte checks. Complete invalid/unknown/foreign/
hash-conflicting owners remain refused. Ready rings are never repaired.

Two new cleanup-interference tests initially failed because their diagnostic
observation disposed with a default success outcome after catching an expected
uncertain refusal (`unit-sqlite-2479a36445b1464eb5bfd6c05ce99d80.json`).
The diagnostic validator correctly refused that contradictory record. The tests
now select the actual expected failure before disposal and correlate their hook
with the ambient observation. Both pass; this was a new test-harness error,
not a product diagnostic fix. Full host units subsequently pass.

Fresh final SQLite report
`recovery-sqlite-a20072169b0646da8b5377334563e4dc.json` passes every command:

```bash
dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo
dotnet test --project tests/unit/Kofte.Host.Tests --no-restore --no-build -c Release --minimum-expected-tests 1
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityPreparationTests' --filter-method '*ActualKeyPreparation*' --minimum-expected-tests 6
dotnet test --project tests/integration/Kofte.Storage.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityRecoveryTests' '*SqliteRecoveryDiagnosticsTests' --minimum-expected-tests 25
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityPreparationTests' --filter-method '*ActualRestoreCommand*' '*LegacyRestore*' --minimum-expected-tests 16
dotnet format Kofte.slnx --verify-no-changes --no-restore
```

The six actual crash cases also pass with `*PostgresIdentityPreparationTests`
and the same method filter/minimum in
`recovery-postgres-0d5c6bfddebb43c2be6bc29447a145eb.json`. This used an isolated
loopback PostgreSQL **17.9**, built under ignored toolchain storage from the
official SHA-256-verified source after native tools were found missing. Its
private cluster/PID/port readiness and clean shutdown were verified; no shared
service or Homebrew dependency was changed. Earlier native recovery/key attempts
fail because backup/restore explicitly requires the owned pinned Docker image,
and one key case additionally encounters intermittent worker readiness. These
are not backup/restore passes; the guard is unchanged. Current new artifact
negatives still need owned-image PostgreSQL execution.

Native failure extraction also now admits uppercase characters before the
existing exact code allowlist, retaining source-known CamelCase transport
categories instead of silently losing them. The updated verifier regression
genuinely fails before that fix, then passes; unknown canaries stay discarded.
Repository checker, complete **133 Python tests** and whitespace checks pass.
No migration, dependency, deadline, retry, concurrency, reviewer checklist or
task marker changed. Partial initial owner/key XML writes and the remaining
operations/full-gate obligations are not claimed solved.

Published `ec01770` run **40713979466** completed: quality/unit/privacy/browser/
startup/recovery pass; both integrations, contract and failure propagation fail.
Tested merge `bc109a4f8a59564ba4c83794c4f904b07868ecde` has actual main parent
`3e416813233c9d291eb20cf72d5882dbddf3e1b6` and the published feature parent;
inspected OIDC/fixture/key/verifier blobs match. Both-provider recovery report
`recovery-both-0753709e131f473480390cb264fcd53b.json` passes native/storage/
public/composition/browser recovery on both providers. This assesses the
published diagnostic head, not these uncommitted key/artifact additions.
Its SQLite integration report
`integration-sqlite-e60d04219f4540749d7e2687e65cfbe2.json` retains
`expected_303_actual_503` for initial OIDC setup in the invalid-evidence and
provider-failure tests. The underlying cause remains unknown; composition
integration also fails. No unchanged passing subset is a fix or full-gate waiver.

## T086: real authority, ticket and required-record refusals

The key-inventory increment is published as
`d0fe6b6d9224bfea3e750fef4c007d1b95f999b2` on the same draft PR 8; implementation
continues without changing **80/100** task markers or the running ledger.
New `IdentityFailureTests.cs` exercises four inherited cases through actual
native production routes. After successful identity/actor requests, closing
only the fixture's serving lease/session or corrupting its durable ticket must
return private 503 on both routes, never cached identity or a success cookie.
Exact users/mappings/events and damaged ticket bytes remain retained.
Explicit restart, or stopped epoch revocation for the corrupt-ticket case,
followed by fresh deliberate login retains the original user/mapping and cannot
grant initial-administrator status.

The existing revocation-record injection helper is generalized with a closed
three-event allowlist, preserving its original wrapper. Actual SQLite and
PostgreSQL triggers now reject the required `session_created` or
`initial_administrator_enrolled` insert. Failed login retains its pending
attempt and rolls back user/mapping/session/events; `/me` remains 401.
Failed enrollment retains the exact nomination generation/authority and original
identity/session/events. Both return bounded private 503 with no success cookie.
Only explicit removal of the test-owned fault followed by a fresh deliberate
request commits once; enrollment replay conflicts and cannot repeat designation.
These exercise existing transaction behavior, not a newly fixed production bug
or a fabricated initial red.

Final SQLite report `integration-sqlite-f927b3428e6c47d5a32dd88b56665c5c.json`
passes:

```bash
dotnet build Kofte.slnx --no-restore -c Release -warnaserror --nologo
dotnet test --project tests/integration/Kofte.Host.IntegrationTests --no-restore --no-build -c Release --filter-class '*SqliteIdentityFailureTests' --minimum-expected-tests 4
dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore --no-build -c Release --filter-trait Provider=sqlite --filter-method '*RejectedRequiredRevocationRecord*' --minimum-expected-tests 1
dotnet format Kofte.slnx --verify-no-changes --no-restore
```

The same four cases and retained revocation regression also pass with
`*PostgresIdentityFailureTests` / `Provider=postgres` in
`integration-postgres-0d7bea744b0f4be9930aec83c8f77ff4.json`. The private native
PostgreSQL 17.9 cluster has verified readiness, PID/port ownership and clean
shutdown; it is not the supported pinned-image/full-suite profile. Required
repository checker, complete **133 Python tests** and whitespace checks pass.
No production endpoint, diagnostic, migration, dependency or budget changed.
T086 remains open for its complete outage/corruption/commit-uncertainty and
assessed supported-provider evidence. No cause or fix for historical initial
login 503/readiness failures is inferred from these passing cases.
