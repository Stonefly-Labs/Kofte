using System.Diagnostics;
using System.Text.Json;
using Kofte.Composition.TestModule;
using Kofte.Infrastructure.Storage.Migrations;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.TestSupport;

namespace Kofte.Composition.Consumer.Scenarios;

internal sealed record MigrationCorrectionProof(
    string FailedAttempt, string CorrectedAttempt, int FailedWorker, int CorrectedWorker);

internal static class MigrationFailureScenarios
{
    internal static async Task<MigrationCorrectionProof> UpgradeWithRecoveryAsync(
        ScenarioEvidence evidence, CompositionFixture fixture, IReadOnlyList<OwnerStoreState> originals,
        CancellationToken token)
    {
        string failedAttempt;
        int failedWorker;
        await using (var process = fixture.Start("prepare", CompositionTestCatalog.RelationalHoldMigration,
            observeAuthor: true))
        {
            await fixture.AuthorTrace.WaitForAsync(StorageRecipes.BetaOwner, "migration", token);
            var started = Stopwatch.GetTimestamp();
            await process.StopAsync();
            var result = await process.ResultAsync();
            MigrationScenarios.RequireExit(evidence, process, result, 1);
            evidence.Require(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(15),
                "composition_test_interrupted_prepare_exceeded_cleanup_budget");
            await fixture.AuthorTrace.EndAsync(token);
            var records = BootScenarios.HostRecords(result.Stdout);
            failedAttempt = records[0].GetProperty("attempt").GetString()!;
            failedWorker = fixture.AuthorTrace.Records.First().Worker;
            evidence.Require(records.All(record => record.GetProperty("stage").GetString() is not ("started" or "published")) &&
                fixture.AuthorTrace.Records.All(record => record.Phase is not ("start" or "compute" or "health")) &&
                fixture.AuthorTrace.Records.Count(record => record.Owner == StorageRecipes.BetaOwner &&
                    record.Phase == "migration") == 1,
                "composition_test_interrupted_prepare_started_or_retried");
            var committed = records.Single(record => record.GetProperty("stage").GetString() == "migrated" &&
                record.GetProperty("owner").GetString() == StorageRecipes.AlphaOwner);
            evidence.Require(committed.GetProperty("durability").GetString() == "committed" &&
                committed.GetProperty("storageCorrelation").GetString() is { Length: 32 } &&
                records.All(record => record.GetProperty("stage").GetString() != "migrated" ||
                    record.GetProperty("owner").GetString() != StorageRecipes.BetaOwner),
                "composition_test_earlier_commit_or_later_failure_misreported");
        }
        var earlier = (await MigrationScenarios.ReadStatesAsync(fixture, StorageRelease.Current, token,
            onlyOwner: StorageRecipes.AlphaOwner)).Single();
        var original = originals.Single(state => state.Owner == StorageRecipes.AlphaOwner);
        evidence.Require(earlier.Rows == original.Rows && earlier.Identity == original.Identity &&
            earlier.Revision > original.Revision &&
            earlier.History == JsonSerializer.Serialize(new[] { StorageRecipes.AlphaInitial, StorageRecipes.AlphaCurrent }),
            "composition_test_earlier_commit_or_original_data_lost");
        await MigrationScenarios.QuietAsync(async () =>
        {
            var registry = new StorageRegistry(MigrationScenarios.Installation(fixture));
            var interrupted = await registry.ResolveAsync(StorageRecipes.Descriptions[1].Id, token);
            evidence.Require(interrupted.Outcome == StorageOutcome.ReconciliationRequired &&
                interrupted.ModuleId == StorageRecipes.BetaOwner && interrupted.CorrelationId.Length == 32 &&
                interrupted.Durability == StorageDurability.Unchanged &&
                interrupted.Diagnostics.Any(value => value.Guidance == "inspect_preserved_target"),
                "composition_test_interrupted_intent_not_preserved");
            return true;
        });
        var beforeRefusal = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        await using (var rejected = fixture.Start("prepare", CompositionTestCatalog.RelationalCurrent))
        {
            var output = await rejected.WaitAsync(token);
            MigrationScenarios.RequireExit(evidence, rejected, output, 4);
            evidence.Require(BootScenarios.HostRecords(output.Stdout)[^1].GetProperty("code").GetString() ==
                "composition_reconciliation_required", "composition_test_uncertain_migration_automatically_replayed");
        }
        evidence.Require(beforeRefusal == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
            "composition_test_refused_retry_changed_complete_inventory");
        await MigrationScenarios.QuietAsync(async () =>
        {
            var installation = MigrationScenarios.Installation(fixture);
            var registry = new StorageRegistry(installation);
            await using var scope = await new Kofte.Infrastructure.Storage.StorageMaintenance(installation).AcquireAsync(token);
            var recovered = await ModuleMigrationRecovery.RecoverAsync(registry, StorageRecipes.Descriptions[1].Id,
                StorageRecipes.BetaFactory(), scope, token);
            evidence.Require(recovered is StorageResult.Ready && recovered.ModuleId == StorageRecipes.BetaOwner &&
                recovered.Durability == StorageDurability.Committed && recovered.CorrelationId.Length == 32,
                "composition_test_explicit_accepted_recovery_failed");
            return true;
        });
        var recoveredStates = await MigrationScenarios.ReadStatesAsync(fixture, StorageRelease.Current, token);
        evidence.Require(recoveredStates.Single(state => state.Owner == StorageRecipes.AlphaOwner) == earlier,
            "composition_test_recovery_rewrote_earlier_commit");
        foreach (var state in recoveredStates)
        {
            var before = originals.Single(value => value.Owner == state.Owner);
            evidence.Require(state.Identity == before.Identity && state.Rows == before.Rows,
                "composition_test_recovery_changed_original_identity_or_data");
        }
        var beforeFresh = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        await MigrationScenarios.PrepareAsync(evidence, fixture, StorageRelease.Current, token);
        evidence.Require(beforeFresh == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
            "composition_test_corrected_prepare_replayed_migrations");
        var corrected = fixture.AuthorTrace.Records;
        var correctedAttempt = corrected.First().Attempt;
        var correctedWorker = corrected.First().Worker;
        evidence.Require(correctedAttempt != failedAttempt && correctedWorker != failedWorker &&
            corrected.All(record => record.Attempt == correctedAttempt && record.Worker == correctedWorker),
            "composition_test_recovery_reused_failed_attempt");
        return new(failedAttempt, correctedAttempt, failedWorker, correctedWorker);
    }
}
