using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kofte.Composition.TestModule;
using Kofte.Host.Features.ModuleComposition;
using Kofte.Infrastructure.Composition;
using Kofte.TestSupport;
using Kofte.TestSupport.Identity;
using Npgsql;

namespace Kofte.Composition.Consumer.Scenarios;

internal static class DiagnosticScenarios
{
    internal static async Task<ScenarioEvidence> PrivateAsync(string provider, CancellationToken token)
    {
        var evidence = new ScenarioEvidence("private-diagnostics");
        await using var fixture = await CompositionFixture.CreateAsync(provider, token);
        token = fixture.Storage.Token;
        await fixture.SeedUnrelatedNoStorageAsync(token);
        await fixture.PrepareIdentityAsync(token);
        var password = fixture.Storage.Postgres is { } postgres
            ? new NpgsqlConnectionStringBuilder(postgres.ConnectionString).Password! : "private-sqlite-credential";
        var secret = fixture.Canary + "|" + password + "|" + fixture.Storage.DataRoot;
        var unknown = "unknown-" + fixture.Canary;
        var responses = new List<string>();
        using var capture = new CompositionCapture();
        using var parent = new Activity("private-composition-caller")
            .SetIdFormat(ActivityIdFormat.W3C).Start();
        parent.TraceStateString = "private=" + fixture.Canary;
        parent.AddBaggage("private", secret);
        await fixture.WriteSelectionAsync(Selection(secret), token);
        await RunAsync(CompositionTestCatalog.RelationalCurrent, "prepare", 0, "migrated", "composition_progress");
        var prepared = BootScenarios.HostRecords(capture.Stdout).Where(record =>
            record.GetProperty("stage").GetString() == "migrated" &&
            record.GetProperty("durability").GetString() == "committed" &&
            record.GetProperty("owner").GetString() is StorageRecipes.AlphaOwner or StorageRecipes.BetaOwner).ToArray();
        evidence.Require(prepared.Length == 2 && prepared.All(record =>
                record.GetProperty("storageCorrelation").GetString() is { Length: 32 }) &&
            prepared.Select(record => record.GetProperty("storageCorrelation").GetString()).Distinct().Count() == 2,
            "composition_test_original_storage_durability_links_missing");
        evidence.Observed("storageDurabilityComparisons");
        var original = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        await RunAsync(CompositionTestCatalog.RelationalCurrent, "serve", 0, "published", "composition_ready", live: true);
        foreach (var (catalog, mode, stage, code, live) in new[]
        {
            (CompositionTestCatalog.RelationalFailInitialize, "serve", "initialized", "composition_initialization_failed", false),
            (CompositionTestCatalog.RelationalFailStart, "serve", "started", "composition_activation_failed", false),
            (CompositionTestCatalog.RelationalFailHealth, "serve", "health", "composition_health_failed", false),
            (CompositionTestCatalog.RelationalFailCleanup, "serve", "cleanup", "composition_cleanup_incomplete", true),
            (CompositionTestCatalog.RelationalIgnoreValidator, "validate", "validated", "composition_deadline_exceeded", false),
            (CompositionTestCatalog.RelationalIgnoreRegistrar, "prepare", "registered", "composition_deadline_exceeded", false),
            (CompositionTestCatalog.RelationalIgnoreInitialize, "prepare", "initialized", "composition_deadline_exceeded", false),
            (CompositionTestCatalog.RelationalIgnoreStart, "serve", "started", "composition_deadline_exceeded", false),
            (CompositionTestCatalog.RelationalIgnoreHealth, "serve", "health", "composition_deadline_exceeded", false),
            (CompositionTestCatalog.RelationalIgnoreStop, "serve", "stopping", "composition_deadline_exceeded", true),
            (CompositionTestCatalog.RelationalIgnoreCleanup, "serve", "cleanup", "composition_deadline_exceeded", true)
        })
            await RunAsync(catalog, mode, 1, stage, code, live);
        await fixture.WriteSelectionAsync(Selection(secret, unknown), token);
        await RunAsync(CompositionTestCatalog.RelationalCurrent, "validate", 2, "validated", "composition_setting_invalid");
        await fixture.WriteSelectionAsync(JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            modules = new[] { new { id = "INVALID/" + fixture.Canary, enabled = true, settings = new { } } }
        }), token);
        await RunAsync(CompositionTestCatalog.RelationalCurrent, "validate", 2, "input", "composition_configuration_invalid");
        await fixture.WriteSelectionAsync(DeploymentProgram.Selection(CompositionTestCatalog.MissingDependency, secret), token);
        var missing = await RunAsync(CompositionTestCatalog.MissingDependency, "validate", 2,
            "validated", "composition_dependency_missing");
        evidence.Require(missing.Any(record => record.GetProperty("relatedIds").EnumerateArray()
            .Any(id => id.GetString() == "org.example.b")), "composition_test_missing_prerequisite_identity_lost");
        await fixture.WriteSelectionAsync(Selection(secret), token);
        var held = await MigrationScenarios.QuietAsync(() =>
            CompositionStorageSession.OpenPrepareAsync(MigrationScenarios.Installation(fixture), token));
        try
        {
            await RunAsync(CompositionTestCatalog.RelationalCurrent, "validate", 3, "validated", "composition_storage_rejected");
        }
        finally
        {
            await MigrationScenarios.QuietAsync(async () => { await held.DisposeAsync(); return true; });
        }
        await RunAsync(CompositionTestCatalog.RelationalInvalidRecipe, "prepare", 2, "validated", "composition_storage_rejected");
        await fixture.WriteSelectionAsync(DeploymentProgram.Selection([new(DeploymentProgram.Owner, true)], new { secret }), token);
        await RunAsync(CompositionTestCatalog.RequiredOutputFault, "validate", 1, "input", "composition_output_failed");
        evidence.Observed("sinkFailureRefusals");
        var admissionStart = capture.Stdout.Length;
        var admissionExit = await ModuleCompositionHost.RunAsync(["--modules-worker", "validate"], []);
        var admission = BootScenarios.HostRecords(capture.Stdout[admissionStart..]);
        evidence.Require(admissionExit == 2 && admission.Any(record =>
            record.GetProperty("code").GetString() == "composition_admission_invalid" &&
            record.GetProperty("owner").ValueKind == JsonValueKind.Null),
            "composition_test_identityless_admission_missing");
        await InvalidFramesAsync(evidence, provider, token);
        OutputLimits(evidence, provider);
        evidence.Require(original == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
            "composition_test_private_diagnostic_faults_changed_installation");

        var records = BootScenarios.HostRecords(capture.Stdout);
        evidence.Require(records.Count > 0 && IdentityDiagnosticOutput.IsSafeServingOutput(capture.Stderr) &&
            records.All(record => record.GetProperty("attempt").GetString() is { Length: 32 } &&
                record.GetProperty("guidance").GetString() is { Length: > 0 }) &&
            records.Select(record => record.GetProperty("attempt").GetString()).Distinct().Count() >= 15,
            "composition_test_useful_current_records_missing");
        evidence.Observed("usefulRecordComparisons");
        evidence.Require(capture.Spans.Count > 0 && records.All(record => capture.Spans.Any(span =>
                span.Trace == record.GetProperty("attempt").GetString() &&
                span.Name == "composition." + record.GetProperty("stage").GetString())) &&
            capture.Spans.All(span => string.IsNullOrEmpty(span.TraceState) && span.Baggage.Length == 0 &&
                span.Tags.Select(tag => tag.Key).ToHashSet(StringComparer.Ordinal).SetEquals(
                    ["composition.code", "composition.guidance", "composition.outcome"])),
            "composition_test_private_span_contract_missing");
        evidence.Require(capture.Spans.All(span => span.Events.Length == 1 &&
            span.Events[0].Select(tag => tag.Key).ToHashSet(StringComparer.Ordinal).SetEquals(
                ["composition.code", "composition.guidance", "composition.outcome"])),
            "composition_test_actual_transition_events_missing");
        evidence.Require(capture.Metrics.Count > 0 && capture.Metrics.All(tags =>
            tags.Select(tag => tag.Key).ToHashSet(StringComparer.Ordinal).SetEquals(["mode", "stage", "provider", "outcome"]) &&
            tags.All(tag => tag.Value is string value && tag.Key switch
            {
                "mode" => Enum.GetValues<CompositionMode>().Any(mode => mode.ToString().ToLowerInvariant() == value),
                "stage" => Enum.GetValues<CompositionStage>().Any(stage => stage.ToString().ToLowerInvariant() == value),
                "provider" => value is "sqlite" or "postgresql" or "none" or "unknown",
                "outcome" => Enum.GetValues<CompositionOutcome>().Any(outcome => outcome.ToString().ToLowerInvariant() == value),
                _ => false
            })),
            "composition_test_private_metric_labels_unbounded");
        evidence.Observed("boundedLabelComparisons");
        var channels = new[]
        {
            capture.Stdout, capture.Stderr, JsonSerializer.Serialize(records), JsonSerializer.Serialize(capture.Spans),
            JsonSerializer.Serialize(capture.Metrics), JsonSerializer.Serialize(responses)
        };
        foreach (var channel in channels)
        {
            evidence.Require(new[] { fixture.Canary, password, fixture.Storage.DataRoot, fixture.SelectionPath, unknown }
                .All(canary => !channel.Contains(canary, StringComparison.Ordinal)),
                "composition_test_private_channel_leaked");
            evidence.Observed("zeroLeakComparisons");
            evidence.Observed("capturedChannels");
        }
        evidence.Require(responses.Count >= 2, "composition_test_actual_http_capture_missing");
        return evidence;

        async Task<IReadOnlyList<JsonElement>> RunAsync(CompositionTestCatalog catalog, string mode, int exit,
            string stage, string code, bool live = false)
        {
            var offset = capture.Stdout.Length;
            var actual = await fixture.InDeploymentEnvironmentAsync(mode, catalog, async () =>
            {
                using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
                var running = ModuleCommand.RunAsync(mode == "serve" ? [] : ["--modules", mode],
                    DeploymentProgram.Catalog(catalog), DeploymentProgram.Recipes(catalog), stop.Token);
                try
                {
                    if (live)
                    {
                        using var http = BootScenarios.Client(fixture);
                        using var ready = CancellationTokenSource.CreateLinkedTokenSource(token);
                        ready.CancelAfter(TimeSpan.FromSeconds(15));
                        while (true)
                        {
                            if (running.IsCompleted)
                                throw new ScenarioFailure("composition_test_private_host_exited_" + catalog +
                                    "_" + await running + "_" + MigrationScenarios.ExitReason(capture.Stdout[offset..]));
                            try
                            {
                                using var response = await http.GetAsync("/health/ready", ready.Token);
                                if (response.StatusCode == HttpStatusCode.OK &&
                                    BootScenarios.HostRecords(capture.Stdout[offset..]).Any(record =>
                                        record.GetProperty("stage").GetString() == "published"))
                                {
                                    evidence.Require(response.Headers.CacheControl?.NoStore == true,
                                        "composition_test_private_http_cacheable");
                                    responses.Add(await response.Content.ReadAsStringAsync(token));
                                    break;
                                }
                            }
                            catch (HttpRequestException) { }
                            await Task.Delay(10, ready.Token);
                        }
                        using var responseLive = await http.GetAsync("/health/live", token);
                        evidence.Require(responseLive.StatusCode == HttpStatusCode.OK,
                            "composition_test_private_http_live_failed");
                        responses.Add(await responseLive.Content.ReadAsStringAsync(token));
                        stop.Cancel();
                    }
                    return await running;
                }
                finally
                {
                    await stop.CancelAsync();
                    await running;
                }
            });
            var emitted = BootScenarios.HostRecords(capture.Stdout[offset..]);
            evidence.Require(actual == exit, "composition_test_private_" + stage + "_exit_" + actual +
                "_" + MigrationScenarios.ExitReason(capture.Stdout[offset..]));
            var outcome = exit == 0 ? "passed" : exit == 2 ? "rejected" : exit == 3 ? "busy" :
                code == "composition_deadline_exceeded" ? "deadline" :
                code == "composition_cleanup_incomplete" ? "incomplete" : "failed";
            var guidance = code == "composition_ready" ? "none" :
                code == "composition_progress" ? "observe_current_attempt" :
                code == "composition_health_failed" ? "inspect_current_health" :
                code is "composition_cleanup_incomplete" or "composition_storage_rejected" ? "inspect_preserved_state" :
                code == "composition_deadline_exceeded" ? "verify_owned_exit_before_retry" :
                code == "composition_output_failed" ? "restore_private_diagnostics" : "correct_selection_and_restart";
            evidence.Require(emitted.Any(record => record.GetProperty("stage").GetString() == stage &&
                record.GetProperty("code").GetString() == code &&
                record.GetProperty("outcome").GetString() == outcome &&
                record.GetProperty("guidance").GetString() == guidance),
                "composition_test_private_" + stage + "_evidence_missing");
            return emitted;
        }
    }

    private static byte[] Selection(string secret, string? unknown = null) => JsonSerializer.SerializeToUtf8Bytes(new
    {
        schemaVersion = "1",
        modules = MigrationScenarios.Owners.Select(owner => new
        {
            id = owner,
            enabled = true,
            settings = owner == StorageRecipes.BetaOwner
                ? unknown is null ? new Dictionary<string, string> { ["secret"] = secret } :
                    new Dictionary<string, string> { ["secret"] = secret, [unknown] = secret }
                : new Dictionary<string, string>()
        })
    });

    private static async Task InvalidFramesAsync(ScenarioEvidence evidence, string provider, CancellationToken token)
    {
        var diagnostics = new CompositionDiagnostics(CompositionMode.Validate, Provider(provider), CompositionCapture.Write);
        var protocol = new CompositionProtocol(diagnostics.Attempt, ActivityTraceId.CreateRandom().ToHexString(), [],
            CompositionMode.Validate);
        using var valid = new MemoryStream();
        await protocol.WriteAsync(valid, diagnostics.Emit(CompositionStage.Admission, CompositionOutcome.Passed,
            "composition_progress"), token);
        var frame = JsonNode.Parse(valid.ToArray().AsSpan(4))!;
        frame["record"]!["attempt"] = ActivityTraceId.CreateRandom().ToHexString();
        var oversized = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(oversized, 4097);
        foreach (var bytes in new[] { Encode("{}"), oversized, Encode(frame.ToJsonString()) })
        {
            try
            {
                await protocol.ReadAsync(new MemoryStream(bytes), token);
                throw new ScenarioFailure("composition_test_invalid_frame_accepted");
            }
            catch (CompositionBoundaryException error)
            {
                evidence.Require(error.Code == "composition_observation_invalid",
                    "composition_test_invalid_frame_not_safe");
                evidence.Observed("invalidFrameRefusals");
            }
        }
    }

    private static void OutputLimits(ScenarioEvidence evidence, string provider)
    {
        foreach (var steady in new[] { false, true })
        {
            var diagnostics = new CompositionDiagnostics(CompositionMode.Serve, Provider(provider), CompositionCapture.Write);
            if (steady) diagnostics.BeginSteadyState();
            for (var index = 0; index < (steady ? 64 : 256); index++)
                diagnostics.Emit(CompositionStage.Health, CompositionOutcome.Passed, "composition_ready");
            try
            {
                diagnostics.Emit(CompositionStage.Health, CompositionOutcome.Passed, "composition_ready");
                throw new ScenarioFailure("composition_test_output_limit_ignored");
            }
            catch (CompositionBoundaryException error)
            {
                evidence.Require(error.Code == "composition_output_limit", "composition_test_output_limit_not_safe");
                diagnostics.Emit(CompositionStage.Cleanup, CompositionOutcome.Failed, error.Code, isTerminal: true);
                evidence.Observed("outputLimitRefusals");
            }
        }
    }

    private static string Provider(string provider) => provider == "postgres" ? "postgresql" : "sqlite";

    private static byte[] Encode(string json)
    {
        var bytes = Encoding.UTF8.GetBytes(json);
        var result = new byte[bytes.Length + 4];
        BinaryPrimitives.WriteInt32LittleEndian(result, bytes.Length);
        bytes.CopyTo(result, 4);
        return result;
    }
}
