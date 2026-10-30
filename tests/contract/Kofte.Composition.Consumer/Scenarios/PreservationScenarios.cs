using Kofte.Composition.TestModule;
using Kofte.TestSupport;

namespace Kofte.Composition.Consumer.Scenarios;

internal static class PreservationScenarios
{
    internal static async Task<ScenarioEvidence> CyclesAsync(string provider, CancellationToken token)
    {
        var evidence = new ScenarioEvidence("disable-reenable");
        await using var fixture = await CompositionFixture.CreateAsync(provider, token);
        token = fixture.Storage.Token;
        await fixture.SeedUnrelatedNoStorageAsync(token);
        await fixture.WriteSelectionAsync(MigrationScenarios.Selection(), token);
        await MigrationScenarios.PrepareAsync(evidence, fixture, StorageRelease.Current, token);
        await MigrationScenarios.WriteOriginalsAsync(fixture, StorageRelease.Current, token);
        var originals = await MigrationScenarios.ReadStatesAsync(fixture, StorageRelease.Current, token);
        var installation = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        var roots = new HashSet<int>();
        var workers = new HashSet<int>();
        var attempts = new HashSet<string>(StringComparer.Ordinal);
        for (var cycle = 0; cycle < 3; cycle++)
        {
            foreach (var enabled in new[] { false, true })
            {
                await fixture.WriteSelectionAsync(MigrationScenarios.Selection(enabled), token);
                var active = enabled ? MigrationScenarios.Owners : [StorageRecipes.NoStorageOwner];
                var identity = await MigrationScenarios.ServeAsync(
                    evidence, fixture, StorageRelease.Current, token, active);
                evidence.Require(roots.Add(identity.RootProcess) && workers.Add(identity.WorkerProcess),
                    "composition_test_selection_cycle_reused_process");
                evidence.Require(attempts.Add(identity.Attempt), "composition_test_selection_cycle_reused_attempt");
                evidence.Observed("distinctProcesses");
                evidence.Observed("distinctAttempts");
                var after = await MigrationScenarios.ReadStatesAsync(fixture, StorageRelease.Current, token);
                foreach (var original in originals)
                {
                    var current = after.Single(state => state.Owner == original.Owner);
                    evidence.Require(current.Identity == original.Identity && current.Revision == original.Revision,
                        "composition_test_selection_changed_original_assignment");
                    evidence.Observed("originalAssignmentComparisons");
                    evidence.Require(current.Rows == original.Rows, "composition_test_selection_lost_original_data");
                    evidence.Observed("originalDataComparisons");
                    evidence.Require(current.History == original.History,
                        "composition_test_selection_replayed_original_history");
                    evidence.Observed("originalHistoryComparisons");
                }
                evidence.Require(installation == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
                    "composition_test_selection_changed_unrelated_or_platform_state");
                evidence.Observed("unrelatedStateComparisons");
            }
            evidence.Observed("completeCycles");
        }
        await fixture.WriteSelectionAsync(MigrationScenarios.Selection(enabled: false, invalidDependent: true), token);
        await using (var rejected = fixture.Start("prepare", CompositionTestCatalog.RelationalCurrent))
        {
            var result = await rejected.WaitAsync(token);
            MigrationScenarios.RequireExit(evidence, rejected, result, 2);
            var records = BootScenarios.HostRecords(result.Stdout);
            evidence.Require(records.Any(record =>
                    record.GetProperty("code").GetString() == "composition_dependency_disabled"),
                "composition_test_invalid_disable_reason_" + MigrationScenarios.ExitReason(result.Stdout));
            evidence.Require(records.All(record => record.GetProperty("stage").GetString() is not
                    ("registered" or "initialized" or "migrated" or "started" or "published")),
                "composition_test_invalid_disable_activation_executed");
        }
        evidence.Require(installation == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
            "composition_test_invalid_disable_mutated_complete_inventory");
        evidence.Observed("dependentDisableRefusals");
        return evidence;
    }
}
