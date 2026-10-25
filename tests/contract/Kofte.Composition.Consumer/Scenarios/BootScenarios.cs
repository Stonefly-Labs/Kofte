using System.Net;
using System.Text.Json;
using Kofte.Composition.TestModule.Modules;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.TestSupport;
using CompositionAuthorRecord = Kofte.TestSupport.CompositionFixture.CompositionAuthorRecord;

namespace Kofte.Composition.Consumer.Scenarios;

internal static class BootScenarios
{
    private static readonly string[] order = [BootModules.Alpha, BootModules.Beta, BootModules.NoStorage];

    internal static async Task<ScenarioEvidence> EmptyAsync(string provider, CancellationToken token)
    {
        var evidence = new ScenarioEvidence("empty-boot");
        await using var fixture = await CompositionFixture.CreateAsync(provider, token);
        token = fixture.Storage.Token;
        await fixture.WriteSelectionAsync(DeploymentProgram.Selection([]), token);
        foreach (var revision in new[] { 0, 1 })
        {
            if (revision == 1) await fixture.SeedUnrelatedNoStorageAsync(token);
            foreach (var explicitSelection in new[] { false, true })
            {
                var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
                await using var process = fixture.StartBoot(CompositionTestCatalog.EmptyBoot, explicitSelection);
                using var http = Client(fixture);
                await ReadyAsync(evidence, process, http, token);
                await HealthAsync(evidence, http, "live", HttpStatusCode.OK, token);
                evidence.Observed("coreEndpoints", 2);
                await NoBusinessRoutesAsync(evidence, http, token);
                await StopAsync(evidence, process, explicitSelection, token);
                if (explicitSelection)
                    evidence.Require(HostRecords((await process.ResultAsync()).Stdout)
                        .All(record => record.GetProperty("owner").ValueKind == JsonValueKind.Null),
                        "composition_test_empty_boot_activated_owner");
                evidence.Require(before == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
                    "composition_test_empty_inventory_changed");
                evidence.Observed("emptyInventoryComparisons");
            }
        }
        return evidence;
    }

    internal static async Task<ScenarioEvidence> DependencyAsync(string provider, CancellationToken token)
    {
        var evidence = new ScenarioEvidence("valid-dependency");
        await using var fixture = await CompositionFixture.CreateAsync(provider, token);
        token = fixture.Storage.Token;
        await fixture.SeedUnrelatedNoStorageAsync(token);
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        var inventoryBefore = await fixture.InspectStoppedInventoryAsync(token);
        await fixture.WriteSelectionAsync(Selection(disabled: false), token);
        await using var process = fixture.StartBoot(CompositionTestCatalog.BootDependency, observeAuthor: true);
        var trace = fixture.AuthorTrace;
        using var http = Client(fixture);
        await trace.WaitForAsync(BootModules.Beta, "start-held", token);
        await HealthAsync(evidence, http, "ready", HttpStatusCode.ServiceUnavailable, token);
        await HealthAsync(evidence, http, "live", HttpStatusCode.OK, token);
        evidence.Require(!HostRecords(process.OutputSnapshot).Any(record => record.GetProperty("stage").GetString() == "published") &&
            !trace.Records.Any(record => record.Phase == "health"), "composition_test_premature_publication");
        evidence.Observed("prepublicationComparisons");
        await trace.ReleaseStartAsync(token);
        await ReadyAsync(evidence, process, http, token);
        foreach (var owner in order) await trace.WaitForAsync(owner, "health", token);
        await WaitForPublishedAsync(process, token);
        var records = trace.Records;
        foreach (var (owner, result) in new[] { (BootModules.Alpha, 22), (BootModules.Beta, 55), (BootModules.NoStorage, 8) })
        {
            evidence.Require(records.Where(record => record.Owner == owner && record.Phase == "compute")
                .All(record => record.Value == result) &&
                records.Any(record => record.Owner == owner && record.Phase == "compute" && record.Value == result),
                "composition_test_public_service_result_wrong");
            evidence.Require(records.Count(record => record.Owner == owner && record.Phase == "service-constructor") == 1 &&
                records.Count(record => record.Owner == owner && record.Phase == "runtime-constructor") == 1,
                "composition_test_owner_scope_not_reused");
            evidence.Observed("publicServiceComparisons");
        }
        evidence.Require(records.Count(record => record.Owner == BootModules.Beta && record.Phase == "dependency-bound") == 1 &&
            records.Count(record => record.Owner == BootModules.Beta && record.Phase == "cross-owner-denied") == 1,
            "composition_test_public_binding_not_owner_scoped");
        var phases = records.Where(record => record.Phase is "register" or "initialize" or "start" or "health")
            .Select(record => record.Phase + ":" + record.Owner).ToArray();
        evidence.Require(phases.SequenceEqual(new[] { "register", "initialize", "start", "health" }
            .SelectMany(phase => order.Select(owner => phase + ":" + owner))), "composition_test_phase_barrier_wrong");
        RequireHostOrder(evidence, HostRecords(process.OutputSnapshot));
        evidence.Observed("phaseOrderComparisons");
        evidence.Require(records.Select(record => record.Attempt).Distinct(StringComparer.Ordinal).Count() == 1 &&
            records.Select(record => record.Worker).Distinct().Count() == 1 &&
            records[0].Worker != Environment.ProcessId &&
            records[0].Attempt == HostRecords(process.OutputSnapshot)[0].GetProperty("attempt").GetString(),
            "composition_test_author_identity_not_worker_attempt");
        await NoBusinessRoutesAsync(evidence, http, token);
        await StopAsync(evidence, process, composed: true, token);
        await trace.CompleteAsync(token);
        RequireCleanup(evidence, trace.Records, order);
        var inventoryAfter = await fixture.InspectStoppedInventoryAsync(token);
        foreach (var owner in order)
        {
            RequireNoAssignment(evidence, inventoryBefore, owner);
            RequireNoAssignment(evidence, inventoryAfter, owner);
            evidence.Require(trace.Records.Count(record => record.Owner == owner && record.Phase == "no-storage") == 1 &&
                trace.Records.Single(record => record.Owner == owner && record.Phase == "migration-total").Value == 0,
                "composition_test_no_storage_callback_wrong");
            evidence.Observed("ownerHistoryComparisons");
        }
        evidence.Require(before == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
            "composition_test_no_storage_inventory_changed");
        return evidence;
    }

    internal static async Task<ScenarioEvidence> DisabledMissingAsync(string provider, CancellationToken token)
    {
        var evidence = new ScenarioEvidence("disabled-missing");
        await using var fixture = await CompositionFixture.CreateAsync(provider, token);
        token = fixture.Storage.Token;
        await fixture.SeedUnrelatedNoStorageAsync(token);
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        var attempts = new HashSet<string>(StringComparer.Ordinal);
        foreach (var omitted in new[] { false, true })
        {
            await fixture.WriteSelectionAsync(omitted
                ? DeploymentProgram.Selection([new(BootModules.NoStorage, true)], new { seed = 13 })
                : Selection(disabled: true), token);
            await using var process = fixture.StartBoot(CompositionTestCatalog.BootDisabled, observeAuthor: true);
            using var http = Client(fixture);
            await ReadyAsync(evidence, process, http, token);
            await fixture.AuthorTrace.WaitForAsync(BootModules.NoStorage, "health", token);
            await WaitForPublishedAsync(process, token);
            await StopAsync(evidence, process, composed: true, token);
            await fixture.AuthorTrace.CompleteAsync(token);
            var host = HostRecords((await process.ResultAsync()).Stdout);
            var published = host.Single(record => record.GetProperty("stage").GetString() == "published");
            var attempt = published.GetProperty("attempt").GetString()!;
            evidence.Require(attempts.Add(attempt), "composition_test_disabled_attempt_reused");
            evidence.Observed("freshDisabledAttempts");
            var records = fixture.AuthorTrace.Records;
            foreach (var owner in new[] { BootModules.Alpha, BootModules.Beta })
            {
                var own = records.Where(record => record.Owner == owner).ToArray();
                evidence.Require(own.Length == 6 && own.All(record => record.Phase.EndsWith("-total", StringComparison.Ordinal) &&
                    record.Value == 0), "composition_test_disabled_callback_executed");
                var observations = host.TakeWhile(record => record.GetProperty("stage").GetString() != "published")
                    .Where(record => record.GetProperty("owner").GetString() == owner &&
                        record.GetProperty("code").GetString() == "composition_disabled").ToArray();
                evidence.Require(observations.Length == 1, "composition_test_disabled_observation_missing");
                var observation = observations[0];
                evidence.Require(observation.GetProperty("version").GetInt32() == 1 &&
                    observation.GetProperty("attempt").GetString() == attempt &&
                    observation.GetProperty("mode").GetString() == "serve" &&
                    observation.GetProperty("stage").GetString() == "discovered" &&
                    observation.GetProperty("outcome").GetString() == "disabled" &&
                    observation.GetProperty("guidance").GetString() == "edit_selection_while_stopped" &&
                    observation.GetProperty("elapsedMilliseconds").GetInt64() <=
                        published.GetProperty("elapsedMilliseconds").GetInt64() &&
                    host.Where(record => record.GetProperty("owner").GetString() == owner)
                        .All(record => record.GetProperty("stage").GetString() == "discovered"),
                    "composition_test_disabled_observation_invalid");
                evidence.Observed(omitted ? "omittedDisabledObservations" : "explicitDisabledObservations");
            }
            evidence.Require(host.Count(record => record.GetProperty("code").GetString() == "composition_disabled") == 2 &&
                records.Any(record => record.Owner == BootModules.NoStorage && record.Phase == "compute" && record.Value == 8),
                "composition_test_enabled_no_storage_not_executed");
            RequireCleanup(evidence, records, [BootModules.NoStorage]);
            evidence.Observed("disabledCallbackComparisons");
            evidence.Require(before == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
                "composition_test_disabled_inventory_changed");
            evidence.Observed("unchangedInventories");
        }

        await fixture.WriteSelectionAsync(DeploymentProgram.Selection(
            [new(BootModules.Alpha, true)], new { seed = 7 }), token);
        await using (var missing = fixture.StartBoot(CompositionTestCatalog.BootMissing))
        {
            var result = await missing.WaitAsync(token);
            var host = HostRecords(result.Stdout);
            evidence.Require(result.ExitCode == 2 && missing.ExitVerified && missing.DescendantExitsVerified &&
                result.Stderr.Length == 0 && host.Any(record => record.GetProperty("code").GetString() ==
                    "composition_implementation_missing" && record.GetProperty("outcome").GetString() == "rejected") &&
                host.All(record => record.GetProperty("stage").GetString() != "published"),
                "composition_test_missing_implementation_not_refused");
            evidence.Require(host.Any(record => record.GetProperty("code").GetString() ==
                "composition_implementation_missing" && record.GetProperty("owner").GetString() == BootModules.Alpha),
                "composition_test_missing_identity_not_reported");
            using var http = Client(fixture);
            evidence.Require(!await IsReadyAsync(http, token), "composition_test_missing_implementation_ready");
            evidence.Observed("missingRefusals");
        }
        evidence.Require(before == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
            "composition_test_missing_inventory_changed");
        evidence.Observed("unchangedInventories");
        return evidence;
    }

    private static byte[] Selection(bool disabled) => JsonSerializer.SerializeToUtf8Bytes(new
    {
        schemaVersion = "1",
        modules = new[]
        {
            new { id = BootModules.Beta, enabled = !disabled, settings = new { seed = disabled ? 99 : 11 } },
            new { id = BootModules.NoStorage, enabled = true, settings = new { seed = 13 } },
            new { id = BootModules.Alpha, enabled = !disabled, settings = new { seed = disabled ? 99 : 7 } }
        }
    });

    internal static HttpClient Client(CompositionFixture fixture) => new()
    {
        BaseAddress = fixture.HostUrl,
        Timeout = TimeSpan.FromSeconds(2)
    };

    internal static async Task ReadyAsync(ScenarioEvidence evidence, CompositionProcess process, HttpClient http,
        CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        while (!await IsReadyAsync(http, deadline.Token))
        {
            if (process.HasExited) throw new ScenarioFailure("composition_test_boot_exited_before_ready");
            await Task.Delay(15, deadline.Token);
        }
        await HealthAsync(evidence, http, "ready", HttpStatusCode.OK, token);
    }

    private static async Task<bool> IsReadyAsync(HttpClient http, CancellationToken token)
    {
        try
        {
            using var result = await http.GetAsync("/health/ready", token);
            return result.StatusCode == HttpStatusCode.OK;
        }
        catch (HttpRequestException) { return false; }
    }

    private static async Task HealthAsync(ScenarioEvidence evidence, HttpClient http, string endpoint,
        HttpStatusCode expected, CancellationToken token)
    {
        using var result = await http.GetAsync("/health/" + endpoint, token);
        evidence.Require(result.StatusCode == expected && result.Headers.CacheControl?.NoStore == true,
            "composition_test_health_status_wrong");
        using var document = JsonDocument.Parse(await result.Content.ReadAsByteArrayAsync(token));
        var root = document.RootElement;
        evidence.Require(root.EnumerateObject().Select(property => property.Name).ToHashSet(StringComparer.Ordinal)
                .SetEquals(["version", "state", "component", "correlationId"]) &&
            root.GetProperty("version").GetString() == "1" &&
            root.GetProperty("state").GetString() == (expected == HttpStatusCode.ServiceUnavailable ? "unavailable" : endpoint) &&
            root.GetProperty("correlationId").GetString() is { Length: 32 },
            "composition_test_anonymous_health_contract_wrong");
    }

    private static async Task NoBusinessRoutesAsync(ScenarioEvidence evidence, HttpClient http, CancellationToken token)
    {
        foreach (var path in new[] { "/api/modules", "/modules", "/org.example.alpha/operations" })
        {
            using var response = await http.GetAsync(path, token);
            evidence.Require(response.StatusCode == HttpStatusCode.NotFound, "composition_test_anonymous_business_route");
        }
    }

    internal static async Task StopAsync(ScenarioEvidence evidence, CompositionProcess process, bool composed,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await process.StopAsync();
        var result = await process.ResultAsync();
        MigrationScenarios.RequireExit(evidence, process, result, 0);
        evidence.Require(process.HasExited, "composition_test_owned_stop_not_clean");
        if (!composed) return;
        var records = HostRecords(result.Stdout);
        evidence.Require(records.Count(record => record.GetProperty("stage").GetString() == "cleanup" &&
                record.GetProperty("owner").ValueKind == JsonValueKind.Null) == 1 &&
            records[^1].GetProperty("stage").GetString() == "cleanup" &&
            records[^1].GetProperty("outcome").GetString() == "passed",
            "composition_test_terminal_exit_unverified");
    }

    internal static async Task WaitForPublishedAsync(CompositionProcess process, CancellationToken token)
    {
        while (!HostRecords(process.OutputSnapshot).Any(record => record.GetProperty("stage").GetString() == "published"))
        {
            if (process.HasExited) throw new ScenarioFailure("composition_test_boot_exited_before_publication");
            await Task.Delay(5, token);
        }
    }

    internal static IReadOnlyList<JsonElement> HostRecords(string output)
    {
        var end = output.LastIndexOf('\n');
        if (end < 0) return [];
        return output[..end].Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line =>
        {
            using var document = JsonDocument.Parse(line,
                new JsonDocumentOptions { MaxDepth = 8, AllowDuplicateProperties = false });
            return document.RootElement.Clone();
        }).ToArray();
    }

    internal static void RequireHostOrder(ScenarioEvidence evidence, IReadOnlyList<JsonElement> records)
    {
        var published = records.Single(record => record.GetProperty("stage").GetString() == "published");
        evidence.Require(published.GetProperty("outcome").GetString() == "passed" &&
            published.GetProperty("code").GetString() == "composition_ready", "composition_test_publication_record_wrong");
        var phases = new[] { "registered", "initialized", "migrated", "started", "health" };
        evidence.Require(records.TakeWhile(record => record.GetProperty("stage").GetString() != "published")
            .Where(record => phases.Contains(record.GetProperty("stage").GetString()) &&
                record.GetProperty("owner").ValueKind == JsonValueKind.String)
            .Select(record => record.GetProperty("stage").GetString() + ":" + record.GetProperty("owner").GetString())
            .SequenceEqual(phases.SelectMany(phase => order.Select(owner => phase + ":" + owner))),
            "composition_test_public_phase_order_wrong");
    }

    internal static void RequireCleanup(ScenarioEvidence evidence, IReadOnlyList<CompositionAuthorRecord> records,
        IReadOnlyList<string> owners)
    {
        var cleanup = records.Where(record => record.Phase is "stop" or "cleanup" or "dispose")
            .Select(record => record.Owner + ":" + record.Phase);
        evidence.Require(cleanup.SequenceEqual(owners.Reverse().SelectMany(owner =>
            new[] { owner + ":stop", owner + ":cleanup", owner + ":dispose" })),
            "composition_test_reverse_cleanup_or_disposal_wrong");
    }

    private static void RequireNoAssignment(ScenarioEvidence evidence, RecoveryInventory inventory, string owner)
    {
        var rows = inventory.Allocations.Where(row => row.ModuleId == owner).ToArray();
        evidence.Require(rows.Length == 0 && rows.Count(row => row.AllocationId is not null || row.AllocationKey is not null) == 0 &&
            rows.Sum(row => row.History.Count) == 0, "composition_test_no_storage_assignment_or_history");
    }
}
