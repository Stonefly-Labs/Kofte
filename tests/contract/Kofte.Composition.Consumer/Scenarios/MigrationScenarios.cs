using System.Text.Json;
using Kofte.Composition.TestModule;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.TestSupport;
using Kofte.TestSupport.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kofte.Composition.Consumer.Scenarios;

internal sealed record OwnerStoreState(string Owner, string Identity, long Revision, string History, string Rows);
internal sealed record ServingAttempt(string Attempt, int RootProcess, int WorkerProcess);

internal static class MigrationScenarios
{
    internal static readonly string[] Owners =
        [StorageRecipes.AlphaOwner, StorageRecipes.BetaOwner, StorageRecipes.NoStorageOwner];

    internal static async Task<ScenarioEvidence> OrderedAsync(string provider, CancellationToken token)
    {
        var evidence = new ScenarioEvidence("ordered-migration");
        await using var fixture = await CompositionFixture.CreateAsync(provider, token);
        token = fixture.Storage.Token;
        await fixture.SeedUnrelatedNoStorageAsync(token);
        await fixture.WriteSelectionAsync(Selection(), token);
        var original = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        foreach (var (mode, catalog, exit, code) in new[]
        {
            ("validate", CompositionTestCatalog.RelationalInitial, 0, "composition_progress"),
            ("serve", CompositionTestCatalog.RelationalInitial, 2, "composition_prepare_required"),
            ("prepare", CompositionTestCatalog.RelationalInvalidRecipe, 2, "composition_storage_rejected")
        })
        {
            await using var process = fixture.Start(mode, catalog);
            var result = await process.WaitAsync(token);
            RequireExit(evidence, process, result, exit);
            var records = BootScenarios.HostRecords(result.Stdout);
            evidence.Require(records[^1].GetProperty("code").GetString() == code &&
                records.All(record => record.GetProperty("stage").GetString() is not ("registered" or "published")),
                "composition_test_relational_preflight_not_pure");
            evidence.Require(original == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
                "composition_test_whole_recipe_preflight_changed_inventory");
        }
        evidence.Observed("wholeSetPreflightComparisons");
        await PrepareAsync(evidence, fixture, StorageRelease.Initial, token);
        var clean = await ReadStatesAsync(fixture, StorageRelease.Initial, token);
        foreach (var state in clean)
        {
            evidence.Require(state.History == JsonSerializer.Serialize(new[]
                { state.Owner == StorageRecipes.AlphaOwner ? StorageRecipes.AlphaInitial : StorageRecipes.BetaInitial }) &&
                state.Rows == "[]", "composition_test_clean_history_or_data_wrong");
            evidence.Observed("cleanHistoryComparisons");
        }
        await WriteOriginalsAsync(fixture, StorageRelease.Initial, token);
        var originals = await ReadStatesAsync(fixture, StorageRelease.Initial, token);
        await ServeAsync(evidence, fixture, StorageRelease.Initial, token);
        var initialRestarted = await ReadStatesAsync(fixture, StorageRelease.Initial, token);
        evidence.Require(originals.SequenceEqual(initialRestarted),
            "composition_test_initial_serving_changed_original_state");
        await MigrationFailureScenarios.UpgradeWithRecoveryAsync(evidence, fixture, originals, token);
        var upgraded = await ReadStatesAsync(fixture, StorageRelease.Current, token);
        foreach (var state in upgraded)
        {
            var before = originals.Single(value => value.Owner == state.Owner);
            var expected = state.Owner == StorageRecipes.AlphaOwner
                ? new[] { StorageRecipes.AlphaInitial, StorageRecipes.AlphaCurrent }
                : new[] { StorageRecipes.BetaInitial, StorageRecipes.BetaCurrent };
            evidence.Require(state.History == JsonSerializer.Serialize(expected) &&
                state.Identity == before.Identity && state.Revision > before.Revision,
                "composition_test_additive_history_or_identity_wrong");
            evidence.Observed("upgradeHistoryComparisons");
            evidence.Require(state.Rows == before.Rows, "composition_test_additive_original_data_lost");
            evidence.Observed("originalDataComparisons");
        }
        var current = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        await ServeAsync(evidence, fixture, StorageRelease.Current, token);
        var restarted = await ReadStatesAsync(fixture, StorageRelease.Current, token);
        foreach (var state in upgraded)
        {
            evidence.Require(state == restarted.Single(value => value.Owner == state.Owner),
                "composition_test_current_serving_rewrote_revision_or_history");
            evidence.Observed("unchangedServingRevisions");
        }
        evidence.Require(current == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
            "composition_test_serving_changed_full_inventory");
        await PrepareAsync(evidence, fixture, StorageRelease.Current, token);
        evidence.Require(current == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
            "composition_test_current_prepare_replayed_migration");
        var inventory = await fixture.InspectStoppedInventoryAsync(token);
        var noStore = inventory.Allocations.Single(row => row.ModuleId == StorageRecipes.NoStorageOwner);
        evidence.Require(!noStore.Relational && noStore.AllocationKey is null && noStore.AllocationId is null &&
            noStore.AllocatorVersion is null && noStore.MarkerVersion is null && noStore.State == "no_storage" &&
            noStore.MigrationState == "none" && noStore.MigrationTarget is null &&
            noStore.MigrationCheckpoint is null && noStore.History.Count == 0 &&
            inventory.Allocations.Count(row => row.Relational) == 2,
            "composition_test_no_storage_was_allocated");
        evidence.Observed("noStorageComparisons");
        return evidence;
    }

    internal static byte[] Selection(bool enabled = true, bool invalidDependent = false) =>
        DeploymentProgram.Selection(Owners.Select(owner => new Kofte.Platform.Modules.Composition.ModuleSelection(
            owner, owner == StorageRecipes.NoStorageOwner || enabled || invalidDependent && owner == StorageRecipes.BetaOwner)));

    internal static CompositionTestCatalog Catalog(StorageRelease release) => release == StorageRelease.Initial
        ? CompositionTestCatalog.RelationalInitial : CompositionTestCatalog.RelationalCurrent;

    internal static async Task PrepareAsync(ScenarioEvidence evidence, CompositionFixture fixture,
        StorageRelease release, CancellationToken token)
    {
        await using var process = fixture.Start("prepare", Catalog(release), observeAuthor: true);
        var result = await process.WaitAsync(token);
        RequireExit(evidence, process, result, 0);
        await fixture.AuthorTrace.CompleteAsync(token);
        var records = BootScenarios.HostRecords(result.Stdout);
        evidence.Require(records.All(record => record.GetProperty("stage").GetString() is not ("started" or "published")),
            "composition_test_prepare_started_module");
        var phases = new[] { "registered", "initialized", "migrated" };
        evidence.Require(records.Where(record => phases.Contains(record.GetProperty("stage").GetString()) &&
                record.GetProperty("owner").ValueKind == JsonValueKind.String)
            .Select(record => record.GetProperty("stage").GetString() + ":" + record.GetProperty("owner").GetString())
            .SequenceEqual(phases.SelectMany(phase => Owners.Select(owner => phase + ":" + owner))),
            "composition_test_prepare_phase_barrier_wrong");
        var trace = fixture.AuthorTrace.Records;
        evidence.Require(trace.Where(record => record.Phase is "register" or "initialize")
            .Select(record => record.Phase + ":" + record.Owner)
            .SequenceEqual(new[] { "register", "initialize" }.SelectMany(phase => Owners.Select(owner => phase + ":" + owner))) &&
            trace.All(record => record.Phase is not ("start" or "health" or "compute")),
            "composition_test_prepare_executed_business_callback");
        foreach (var owner in Owners.Take(2))
            evidence.Require(records.Single(record => record.GetProperty("stage").GetString() == "migrated" &&
                record.GetProperty("owner").GetString() == owner).GetProperty("storageCorrelation").GetString()
                is { Length: 32 }, "composition_test_original_storage_correlation_missing");
        BootScenarios.RequireCleanup(evidence, trace, Owners);
    }

    internal static async Task<ServingAttempt> ServeAsync(ScenarioEvidence evidence, CompositionFixture fixture,
        StorageRelease release, CancellationToken token, IReadOnlyList<string>? enabled = null,
        CompositionTestCatalog? catalog = null)
    {
        enabled ??= Owners;
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        await using var process = fixture.Start("serve", catalog ?? Catalog(release), observeAuthor: true);
        using var http = BootScenarios.Client(fixture);
        await BootScenarios.ReadyAsync(evidence, process, http, token);
        await BootScenarios.WaitForPublishedAsync(process, token);
        foreach (var owner in enabled) await fixture.AuthorTrace.WaitForAsync(owner, "health", token);
        var records = BootScenarios.HostRecords(process.OutputSnapshot);
        var attempt = records.First().GetProperty("attempt").GetString()!;
        var worker = fixture.AuthorTrace.Records.First().Worker;
        evidence.Require(fixture.AuthorTrace.Records.All(record =>
            enabled.Contains(record.Owner) && record.Attempt == attempt && record.Worker == worker) &&
            process.RootProcessId != worker, "composition_test_disabled_callback_or_reused_worker");
        if (enabled.Count == Owners.Length) BootScenarios.RequireHostOrder(evidence, records);
        evidence.Require(fixture.AuthorTrace.Records.Where(record => record.Phase == "start")
            .Select(record => record.Owner).SequenceEqual(enabled), "composition_test_relational_start_order_wrong");
        foreach (var owner in enabled.Where(owner => owner != StorageRecipes.NoStorageOwner))
            evidence.Require(fixture.AuthorTrace.Records.Single(record => record.Owner == owner &&
                record.Phase == "compute").Value == 1, "composition_test_runtime_did_not_read_original_owner_data");
        await BootScenarios.StopAsync(evidence, process, composed: true, token);
        await fixture.AuthorTrace.CompleteAsync(token);
        BootScenarios.RequireCleanup(evidence, fixture.AuthorTrace.Records, enabled);
        evidence.Require(before == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
            "composition_test_runtime_changed_full_inventory");
        foreach (var disabled in Owners.Except(enabled))
        {
            var observations = fixture.AuthorTrace.Records.Where(record => record.Owner == disabled).ToArray();
            evidence.Require(observations.Length == 6 && observations.All(record => record.Value == 0) &&
                observations.Select(record => record.Phase).ToHashSet(StringComparer.Ordinal).SetEquals(
                    ["callback-total", "constructor-total", "registrar-total", "runtime-total", "cleanup-total", "migration-total"]),
                "composition_test_disabled_owner_executed_callback");
        }
        return new(attempt, process.RootProcessId, worker);
    }

    internal static Task WriteOriginalsAsync(CompositionFixture fixture, StorageRelease release, CancellationToken token) =>
        QuietAsync(async () =>
        {
            var registry = new StorageRegistry(Installation(fixture));
            await using var alpha = await registry.BindAsync(StorageRecipes.Descriptions[0].Id, token);
            await StorageRecipes.WriteAlphaAsync(alpha, new(11, "alpha-original-sentinel", null), release, token);
            await using var beta = await registry.BindAsync(StorageRecipes.Descriptions[1].Id, token);
            await StorageRecipes.WriteBetaAsync(beta, new(11, "beta-original-sentinel", null), release, token);
            return true;
        });

    internal static Task<OwnerStoreState[]> ReadStatesAsync(CompositionFixture fixture, StorageRelease release,
        CancellationToken token, string? onlyOwner = null) => QuietAsync(async () =>
    {
        var registry = new StorageRegistry(Installation(fixture));
        var states = new List<OwnerStoreState>();
        foreach (var owner in StorageRecipes.Descriptions.Take(2).Where(owner =>
            onlyOwner is null || owner.Id.Value == onlyOwner))
        {
            var result = await registry.ResolveAsync(owner.Id, token);
            if (result is not StorageResult.Ready ready) throw new ScenarioFailure("composition_test_original_owner_unavailable");
            await using var binding = await registry.BindAsync(owner.Id, token);
            var rows = owner.Id.Value == StorageRecipes.AlphaOwner
                ? JsonSerializer.Serialize(await StorageRecipes.ReadAlphaAsync(binding, release, token))
                : JsonSerializer.Serialize(await StorageRecipes.ReadBetaAsync(binding, release, token));
            await using var context = owner.Id.Value == StorageRecipes.AlphaOwner
                ? (DbContext)await StorageRecipes.CreateAlphaContextAsync(binding, release, token)
                : await StorageRecipes.CreateBetaContextAsync(binding, release, token);
            var assignment = ready.Assignment;
            var identity = JsonSerializer.Serialize(new
            {
                owner = assignment.Owner.Value,
                assignment.Provider,
                assignment.InstallationId,
                assignment.AllocationId,
                assignment.AllocationKey,
                assignment.AllocatorVersion
            });
            states.Add(new(owner.Id.Value, identity, assignment.Revision,
                JsonSerializer.Serialize(await context.Database.GetAppliedMigrationsAsync(token)), rows));
        }
        return states.ToArray();
    });

    internal static StorageInstallation Installation(CompositionFixture fixture) => fixture.Storage.Postgres is { } postgres
        ? StorageInstallation.PostgreSql(postgres.ConnectionString, postgres.ProvisioningConnectionString)
        : StorageInstallation.Sqlite(fixture.Storage.DataRoot);

    internal static async Task<T> QuietAsync<T>(Func<Task<T>> operation)
    {
        var output = Console.Out;
        var error = Console.Error;
        try
        {
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);
            return await operation();
        }
        finally
        {
            Console.SetOut(output);
            Console.SetError(error);
        }
    }

    internal static void RequireExit(ScenarioEvidence evidence, CompositionProcess process, ProcessOutput result, int exit)
    {
        evidence.Require(result.ExitCode == exit && process.ExitVerified && process.DescendantExitsVerified &&
            IdentityDiagnosticOutput.IsSafeServingOutput(result.Stderr), "composition_test_command_exit_expected_" + exit + "_actual_" + result.ExitCode +
                "_" + ExitReason(result.Stdout));
    }

    internal static string ExitReason(string output)
    {
        var records = BootScenarios.HostRecords(output);
        var terminal = records.LastOrDefault();
        var code = terminal.ValueKind == JsonValueKind.Object ? terminal.GetProperty("code").GetString() : null;
        if (code == "composition_cleanup_incomplete")
        {
            var boundary = records.Select(record => record.GetProperty("code").GetString()).LastOrDefault(value =>
                value is "composition_host_stop_incomplete" or "composition_host_dispose_incomplete" or
                    "composition_services_dispose_incomplete" or "composition_storage_dispose_incomplete");
            if (boundary is not null)
            {
                var interruption = records.LastOrDefault(record => record.GetProperty("stage").GetString() == "stopping" &&
                    record.GetProperty("code").GetString() is "composition_deadline_exceeded" or "composition_cancelled");
                return code + "_" + boundary + (interruption.ValueKind == JsonValueKind.Object
                    ? "_" + interruption.GetProperty("code").GetString() : "");
            }
        }
        return code is "composition_deadline_exceeded" or "composition_output_failed" or "composition_cancelled" or
            "composition_cleanup_incomplete" or "composition_observation_invalid" or "composition_storage_rejected"
            ? code : "other";
    }
}
