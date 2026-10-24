using System.Text.Json;
using Kofte.Composition.TestModule;
using Kofte.TestSupport;

namespace Kofte.Composition.Consumer.Scenarios;

internal static class CorrectionScenarios
{
    internal static async Task<ScenarioEvidence> CorrectAsync(string provider, CancellationToken token)
    {
        var evidence = new ScenarioEvidence("failure-correction");
        await using (var fixture = await CompositionFixture.CreateAsync(provider, token))
        {
            var work = fixture.Storage.Token;
            await fixture.SeedUnrelatedNoStorageAsync(work);
            await fixture.WriteSelectionAsync(MigrationScenarios.Selection(), work);
            await MigrationScenarios.PrepareAsync(evidence, fixture, StorageRelease.Current, work);
            await MigrationScenarios.WriteOriginalsAsync(fixture, StorageRelease.Current, work);
            var originals = await MigrationScenarios.ReadStatesAsync(fixture, StorageRelease.Current, work);
            var inventory = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, work);
            foreach (var (catalog, stage, code, observation) in new[]
            {
                (CompositionTestCatalog.RelationalConfigurationGate, "validated", "composition_setting_invalid", "configurationCorrections"),
                (CompositionTestCatalog.RelationalFailInitialize, "initialized", "composition_initialization_failed", "initializeCorrections"),
                (CompositionTestCatalog.RelationalFailStart, "started", "composition_activation_failed", "startCorrections"),
                (CompositionTestCatalog.RelationalFailHealth, "health", "composition_health_failed", "healthCorrections")
            })
            {
                var configuration = catalog == CompositionTestCatalog.RelationalConfigurationGate;
                await fixture.WriteSelectionAsync(Selection(fixture, configuration ? false : null), work);
                string failedAttempt;
                int failedRoot;
                await using (var failed = fixture.Start("serve", catalog, observeAuthor: !configuration))
                {
                    var output = await failed.WaitAsync(work);
                    MigrationScenarios.RequireExit(evidence, failed, output, configuration ? 2 : 1);
                    var records = BootScenarios.HostRecords(output.Stdout);
                    failedAttempt = records[0].GetProperty("attempt").GetString()!;
                    failedRoot = failed.RootProcessId;
                    evidence.Require(records.Any(record => record.GetProperty("stage").GetString() == stage &&
                            record.GetProperty("code").GetString() == code &&
                            record.GetProperty("owner").GetString() == StorageRecipes.BetaOwner &&
                            record.GetProperty("guidance").GetString() ==
                                (configuration ? "correct_selection_and_restart" :
                                    catalog == CompositionTestCatalog.RelationalFailHealth ? "inspect_current_health" :
                                        "correct_selection_and_restart")) &&
                        records.All(record => record.GetProperty("stage").GetString() != "published"),
                        "composition_test_failed_boundary_not_actionable");
                    if (configuration)
                        evidence.Require(records.Any(record => record.GetProperty("fieldToken").GetString() == "permit" &&
                            record.GetProperty("fieldReason").GetString() == "constraint"),
                            "composition_test_configuration_correction_field_missing");
                    else await fixture.AuthorTrace.EndAsync(work);
                    evidence.Observed("verifiedExits");
                }
                var failedStates = await MigrationScenarios.ReadStatesAsync(fixture, StorageRelease.Current, work);
                evidence.Require(inventory == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, work) &&
                    originals.SequenceEqual(failedStates),
                    "composition_test_failed_ephemeral_attempt_changed_originals");
                if (configuration) await fixture.WriteSelectionAsync(Selection(fixture, permit: true), work);
                var corrected = await MigrationScenarios.ServeAsync(evidence, fixture, StorageRelease.Current, work,
                    catalog: configuration ? CompositionTestCatalog.RelationalConfigurationGate : CompositionTestCatalog.RelationalCurrent);
                evidence.Require(corrected.Attempt != failedAttempt && corrected.RootProcess != failedRoot,
                    "composition_test_corrected_retry_reused_failed_attempt");
                var correctedStates = await MigrationScenarios.ReadStatesAsync(fixture, StorageRelease.Current, work);
                evidence.Require(inventory == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, work) &&
                    originals.SequenceEqual(correctedStates),
                    "composition_test_corrected_retry_repaired_or_replaced_originals");
                evidence.Observed("freshAttemptComparisons");
                evidence.Observed(observation);
            }
        }
        await using (var fixture = await CompositionFixture.CreateAsync(provider, token))
        {
            var work = fixture.Storage.Token;
            await fixture.SeedUnrelatedNoStorageAsync(work);
            await fixture.WriteSelectionAsync(MigrationScenarios.Selection(), work);
            await MigrationScenarios.PrepareAsync(evidence, fixture, StorageRelease.Initial, work);
            await MigrationScenarios.WriteOriginalsAsync(fixture, StorageRelease.Initial, work);
            var originals = await MigrationScenarios.ReadStatesAsync(fixture, StorageRelease.Initial, work);
            var proof = await MigrationFailureScenarios.UpgradeWithRecoveryAsync(evidence, fixture, originals, work);
            evidence.Require(proof.FailedAttempt != proof.CorrectedAttempt && proof.FailedWorker != proof.CorrectedWorker,
                "composition_test_recovered_migration_reused_failed_attempt");
            await MigrationScenarios.ServeAsync(evidence, fixture, StorageRelease.Current, work);
            evidence.Observed("migrationCorrections");
            evidence.Observed("freshAttemptComparisons");
            evidence.Observed("verifiedExits");
            evidence.Observed("retainedCommitComparisons");
            evidence.Observed("explicitRecoveryComparisons");
        }
        return evidence;
    }

    private static byte[] Selection(CompositionFixture fixture, bool? permit) => JsonSerializer.SerializeToUtf8Bytes(new
    {
        schemaVersion = "1",
        modules = MigrationScenarios.Owners.Select(owner => new
        {
            id = owner,
            enabled = true,
            settings = owner != StorageRecipes.BetaOwner ? (object)new { } :
                permit is { } allowed ? new { secret = fixture.Canary, permit = allowed } : (object)new { secret = fixture.Canary }
        })
    });
}
