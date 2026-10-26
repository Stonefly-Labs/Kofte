using System.Diagnostics;
using Kofte.Composition.TestModule;
using Kofte.Infrastructure.Composition;
using Kofte.TestSupport;

namespace Kofte.Composition.Consumer.Scenarios;

internal static class StopScenarios
{
    private sealed record Fault(CompositionTestCatalog Catalog, string Mode, string Phase, string Stage,
        int TotalSeconds, bool Stop = false);

    private static readonly Fault[] faults = [
        new(CompositionTestCatalog.RelationalIgnoreValidator, "validate", "validate", "validated", 45),
        new(CompositionTestCatalog.RelationalIgnoreRegistrar, "prepare", "register", "registered", 120),
        new(CompositionTestCatalog.RelationalIgnoreInitialize, "prepare", "initialize", "initialized", 120),
        new(CompositionTestCatalog.RelationalIgnoreStart, "serve", "start", "started", 60),
        new(CompositionTestCatalog.RelationalIgnoreHealth, "serve", "health", "health", 60),
        new(CompositionTestCatalog.RelationalIgnoreStop, "serve", "stop", "stopping", 15, true),
        new(CompositionTestCatalog.RelationalIgnoreCleanup, "serve", "cleanup", "cleanup", 15, true)];

    internal static async Task<ScenarioEvidence> BoundedAsync(string provider, CancellationToken token)
    {
        var evidence = new ScenarioEvidence("bounded-stop");
        await using var fixture = await CompositionFixture.CreateAsync(provider, token);
        token = fixture.Storage.Token;
        await fixture.SeedUnrelatedNoStorageAsync(token);
        await fixture.WriteSelectionAsync(MigrationScenarios.Selection(), token);
        await MigrationScenarios.PrepareAsync(evidence, fixture, StorageRelease.Current, token);
        await MigrationScenarios.WriteOriginalsAsync(fixture, StorageRelease.Current, token);
        var original = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        var attempts = new HashSet<string>(StringComparer.Ordinal);
        foreach (var fault in faults)
        {
            var observed = fault.Catalog != CompositionTestCatalog.RelationalIgnoreValidator;
            await using var process = fixture.Start(fault.Mode, fault.Catalog, observed);
            var started = Stopwatch.GetTimestamp();
            if (fault.Stop)
            {
                using var http = BootScenarios.Client(fixture);
                await BootScenarios.ReadyAsync(evidence, process, http, token);
                await BootScenarios.WaitForPublishedAsync(process, token);
                started = Stopwatch.GetTimestamp();
                await process.StopAsync();
            }
            else if (observed)
            {
                await fixture.AuthorTrace.WaitForAsync(StorageRecipes.BetaOwner,
                    fault.Catalog == CompositionTestCatalog.RelationalIgnoreStart ? "descendant" : fault.Phase, token);
                if (fault.Catalog == CompositionTestCatalog.RelationalIgnoreStart)
                    process.TrackAuthorDescendant(fixture.AuthorTrace);
            }
            var output = await process.WaitAsync(token);
            evidence.Require(output.ExitCode == 1 && process.ExitVerified && process.DescendantExitsVerified,
                "composition_test_" + fault.Stage + "_exit_unverified");
            evidence.Require(output.Stderr.Length == 0, "composition_test_" + fault.Stage + "_stderr_not_private");
            evidence.Require(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(fault.TotalSeconds),
                "composition_test_fault_exceeded_shared_total");
            var records = BootScenarios.HostRecords(output.Stdout);
            evidence.Require(records.Any(record => record.GetProperty("stage").GetString() == fault.Stage &&
                    record.GetProperty("code").GetString() == "composition_deadline_exceeded" &&
                    record.GetProperty("outcome").GetString() == "deadline") &&
                records[^1].GetProperty("code").GetString() == "composition_cleanup_incomplete" &&
                records[^1].GetProperty("outcome").GetString() == "incomplete",
                "composition_test_timeout_" + fault.Stage + "_stage_or_outcome_missing");
            evidence.Require(attempts.Add(records[0].GetProperty("attempt").GetString()!),
                "composition_test_fault_reused_attempt");
            if (observed)
            {
                await fixture.AuthorTrace.EndAsync(token);
                var trace = fixture.AuthorTrace.Records;
                var boundary = trace.Select((record, index) => (record, index)).First(item =>
                    item.record.Owner == StorageRecipes.BetaOwner && item.record.Phase == fault.Phase).index;
                evidence.Require(trace.Skip(boundary + 1).All(record =>
                    record.Phase is not ("stop" or "cleanup" or "dispose")),
                    "composition_test_cleanup_ran_concurrently_with_unstopped_callback");
            }
            if (fault.Catalog == CompositionTestCatalog.RelationalIgnoreStart)
            {
                evidence.Require(process.TrackedDescendantCount == 1 && process.DescendantExitsVerified,
                    "composition_test_actual_descendant_exit_missing");
                evidence.Observed("verifiedDescendantExits");
            }
            evidence.Require(original == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
                "composition_test_timeout_changed_original_installation");
            evidence.Observed("ignoringTokenBoundaries");
            evidence.Observed("verifiedRootExits");
            evidence.Observed("elapsedBudgetComparisons");
        }
        await CancellationAsync(evidence, fixture, original, token);
        await AuthorityAsync(evidence, fixture, original, token);
        await CleanupFailuresAsync(evidence, fixture, original, token);
        await RefuseUnverifiedChildAsync(evidence, fixture, token);
        await MigrationScenarios.ServeAsync(evidence, fixture, StorageRelease.Current, token);
        evidence.Observed("reverseCleanupComparisons");
        evidence.Require(original == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
            "composition_test_fault_matrix_changed_original_installation");
        return evidence;
    }

    private static async Task CancellationAsync(ScenarioEvidence evidence, CompositionFixture fixture,
        string original, CancellationToken token)
    {
        foreach (var during in new[] { false, true })
        {
            var started = Stopwatch.GetTimestamp();
            await using var process = fixture.Start("prepare", during
                ? CompositionTestCatalog.RelationalCancelInitialize : CompositionTestCatalog.RelationalCancelBeforeWork,
                observeAuthor: during);
            if (during)
            {
                await fixture.AuthorTrace.WaitForAsync(StorageRecipes.BetaOwner, "initialize", token);
                await process.StopAsync();
            }
            var result = await process.WaitAsync(token);
            evidence.Require(result.ExitCode == 1 && process.ExitVerified && process.DescendantExitsVerified &&
                result.Stderr.Length == 0, "composition_test_cancellation_" + during + "_exit_" + result.ExitCode);
            evidence.Require(BootScenarios.HostRecords(result.Stdout).Any(record =>
                    record.GetProperty("code").GetString() == "composition_cancelled") &&
                Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(15),
                "composition_test_cancellation_not_bounded_or_recorded");
            if (during) await fixture.AuthorTrace.EndAsync(token);
            evidence.Require(original == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
                "composition_test_cancellation_changed_installation");
            evidence.Observed("cancellationBoundaries");
        }
    }

    private static Task AuthorityAsync(ScenarioEvidence evidence, CompositionFixture fixture,
        string original, CancellationToken token) => MigrationScenarios.QuietAsync(async () =>
    {
        foreach (var read in new[] { true, false })
        {
            await using (var held = read
                ? await CompositionStorageSession.OpenReadAsync(MigrationScenarios.Installation(fixture), token)
                : await CompositionStorageSession.OpenPrepareAsync(MigrationScenarios.Installation(fixture), token))
            {
                var started = Stopwatch.GetTimestamp();
                await using var refused = fixture.Start(read ? "prepare" : "validate", CompositionTestCatalog.RelationalCurrent);
                var result = await refused.WaitAsync(token);
                MigrationScenarios.RequireExit(evidence, refused, result, 3);
                evidence.Require(BootScenarios.HostRecords(result.Stdout)[^1].GetProperty("outcome").GetString() == "busy" &&
                    Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(5),
                    "composition_test_contention_unbounded_or_misreported");
            }
            evidence.Require(original == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
                "composition_test_busy_refusal_changed_installation");
            evidence.Observed("authorityRefusals");
        }
        if (fixture.Storage.Postgres is { } postgres)
        {
            await using var serving = fixture.StartBoot(CompositionTestCatalog.RelationalCurrent);
            using var http = BootScenarios.Client(fixture);
            await BootScenarios.ReadyAsync(evidence, serving, http, token);
            await BootScenarios.WaitForPublishedAsync(serving, token);
            var started = Stopwatch.GetTimestamp();
            await CompositionLeaseProbe.TerminateAsync(postgres, token);
            var result = await serving.WaitAsync(token);
            MigrationScenarios.RequireExit(evidence, serving, result, 1);
            var records = BootScenarios.HostRecords(result.Stdout);
            evidence.Require(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(10) &&
                records.Count(record => record.GetProperty("stage").GetString() == "admission") == 1 &&
                records[^1].GetProperty("code").GetString() == "composition_cleanup_incomplete" &&
                records[^1].GetProperty("outcome").GetString() == "incomplete" &&
                await CompositionLeaseProbe.CountAsync(postgres, token) == 0,
                "composition_test_lost_serving_lease_reacquired_or_unbounded");
            evidence.Require(original == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
                "composition_test_lost_serving_lease_changed_installation");
            evidence.Observed("authorityRefusals");
        }
        return true;
    });

    private static async Task CleanupFailuresAsync(ScenarioEvidence evidence, CompositionFixture fixture,
        string original, CancellationToken token)
    {
        foreach (var overflow in new[] { true, false })
        {
            await using var process = fixture.Start(overflow ? "prepare" : "serve", overflow
                ? CompositionTestCatalog.RelationalCleanupOverflow : CompositionTestCatalog.RelationalFailCleanup,
                observeAuthor: true);
            if (!overflow)
            {
                using var http = BootScenarios.Client(fixture);
                await BootScenarios.ReadyAsync(evidence, process, http, token);
                await BootScenarios.WaitForPublishedAsync(process, token);
                await process.StopAsync();
            }
            var result = await process.WaitAsync(token);
            evidence.Require(result.ExitCode == 1 && process.ExitVerified && process.DescendantExitsVerified &&
                result.Stderr.Length == 0, "composition_test_cleanup_" + overflow + "_exit_" + result.ExitCode);
            await fixture.AuthorTrace.EndAsync(token);
            var records = BootScenarios.HostRecords(result.Stdout);
            evidence.Require(records[^1].GetProperty("code").GetString() ==
                (overflow ? "composition_initialization_failed" : "composition_cleanup_incomplete"),
                "composition_test_cleanup_failure_became_success");
            evidence.Require(original == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
                "composition_test_cleanup_failure_changed_installation");
        }
    }

    private static async Task RefuseUnverifiedChildAsync(ScenarioEvidence evidence, CompositionFixture fixture,
        CancellationToken token)
    {
        await using var process = fixture.Start("validate", CompositionTestCatalog.RelationalCurrent);
        fixture.TrackIdleFixtureChild(process);
        try
        {
            await process.WaitAsync(token);
            throw new ScenarioFailure("composition_test_unverified_child_accepted_as_success");
        }
        catch (InvalidOperationException error) when (error.Message == "composition_descendants_still_running")
        {
            evidence.Require(process.ExitVerified && !process.DescendantExitsVerified &&
                process.TrackedDescendantCount == 1, "composition_test_unverified_child_refusal_not_real");
        }
        await process.StopAsync();
        evidence.Require(process.ExitVerified && process.DescendantExitsVerified,
            "composition_test_controlled_unverified_child_left_running");
        evidence.Observed("unverifiedTerminationRefusals");
    }
}
