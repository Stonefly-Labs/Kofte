using System.Diagnostics;
using System.Text.Json;
using Kofte.TestSupport;

namespace Kofte.Platform.ContractTests.Composition;

public abstract class CompositionPublicContractTests(ITestOutputHelper output)
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task IndependentConsumerExecutesBothPublicDeterminismPaths()
    {
        var provider = Provider;
        var output = await CompositionConsumerHarness.InvokeAsync(
            ["--provider", provider, "--scenario", "deterministic-corpus"], TestContext.Current.CancellationToken);
        var result = CompositionConsumerHarness.RequireSuccess(output, provider, "deterministic-corpus");
        var observations = result.Observations["deterministic-corpus"];
        Assert.Equal(100, observations["repetitionsPerPermutation"]);
        Assert.Equal(10, observations["permutations"]);
        Assert.Equal(2, observations["cultures"]);
        Assert.Equal(2, observations["publicConsumerPaths"]);
        Assert.Equal(84_000, observations["exactDecisionComparisons"]);
    }

    [Fact]
    public async Task IndependentConsumerRejectsInvalidCompositionWithoutProviderEffects()
    {
        var provider = Provider;
        var execution = await CompositionConsumerHarness.InvokeAsync(
            ["--provider", provider, "--scenario", "invalid-composition"], TestContext.Current.CancellationToken);
        var result = CompositionConsumerHarness.RequireSuccess(execution, provider, "invalid-composition");
        var observations = result.Observations["invalid-composition"];
        foreach (var (name, minimum) in CompositionScenarioCatalog.RequiredObservations("invalid-composition"))
            Assert.True(observations[name] >= minimum);
        output.WriteLine("composition_invalid_validation_evidence " +
            JsonSerializer.Serialize(new { provider, assertions = result.Assertions, observations }));
    }

    [Fact]
    public async Task AllRequiresEveryActualStandaloneScenarioAndItsCompleteSemanticEvidence()
    {
        var provider = Provider;
        var output = await CompositionConsumerHarness.InvokeAsync(
            ["--provider", provider, "--scenario", "all"], TestContext.Current.CancellationToken);
        var result = CompositionConsumerHarness.RequireSuccess(output, provider, "all");
        Assert.Equal(CompositionScenarioCatalog.Scenarios.Count, result.Observations.Count);
    }

    [Fact]
    public async Task ActualDeploymentWorkerRefusesMissingAdmission()
    {
        var output = await CompositionConsumerHarness.InvokeAsync(
            ["--modules-worker", "validate"], TestContext.Current.CancellationToken);
        Assert.Equal(2, output.ExitCode);
        Assert.Contains("composition_admission_invalid", output.Stdout);
        Assert.DoesNotContain("secret", output.Stdout + output.Stderr);
    }

    [Fact]
    public async Task FixedFixtureCatalogSelectorRefusesUndeclaredVariants()
    {
        await using var fixture = await CompositionFixture.CreateAsync(Provider, TestContext.Current.CancellationToken);
        var error = Assert.Throws<InvalidOperationException>(() =>
            fixture.Start("validate", (CompositionTestCatalog)int.MaxValue));
        Assert.Equal("composition_fixture_catalog_invalid", error.Message);
    }

    [Fact]
    public async Task ValidateBoundaryAcceptRejectAndBusyHaveActualExitCodes()
    {
        var provider = Provider;
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync(provider, token);
        await fixture.SeedUnrelatedNoStorageAsync(token);
        await RunBoundaryAsync(fixture, CompositionTestCatalog.Default, 0, "passed", "composition_progress", token);
        await RunBoundaryAsync(fixture, CompositionTestCatalog.MalformedManifest, 2, "rejected",
            "composition_manifest_rejected", token);
        await using (await fixture.HoldMaintenanceAsync(token))
            await RunBoundaryAsync(fixture, CompositionTestCatalog.Default, 3, "busy", "composition_storage_rejected", token);
        await RunBoundaryAsync(fixture, CompositionTestCatalog.Default, 0, "passed", "composition_progress", token);
    }

    [Fact]
    public async Task ValidateBoundaryIgnoringValidatorIsTerminatedWithoutRetry()
    {
        var provider = Provider;
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync(provider, token);
        await fixture.SeedUnrelatedNoStorageAsync(token);
        var elapsed = await RunBoundaryAsync(fixture, CompositionTestCatalog.IgnoringValidator, 1, "incomplete",
            "composition_cleanup_incomplete", token);
        Assert.True(elapsed >= TimeSpan.FromMilliseconds(250));
    }

    [Fact]
    public async Task ValidateBoundaryBrokenPrivateEventSinkFailsAndDiscardsRawCanaries()
    {
        var provider = Provider;
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync(provider, token);
        await fixture.SeedUnrelatedNoStorageAsync(token);
        await RunBoundaryAsync(fixture, CompositionTestCatalog.RequiredOutputFault, 1, "failed",
            "composition_output_failed", token);
    }

    [Fact]
    public async Task ValidateBoundaryOriginalSnapshotSurvivesSelectionReplacementAfterAdmission()
    {
        var provider = Provider;
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync(provider, token);
        await fixture.SeedUnrelatedNoStorageAsync(token);
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        var result = await fixture.RunSnapshotTransportProbeAsync(token);
        Assert.True(result.RootExitVerified && result.AdmissionObserved && result.SelectionReplaced);
        Assert.Equal(0, result.Output.ExitCode);
        Assert.Empty(result.Output.Stderr);
        RequireBoundaryRecords(result.Output.Stdout, provider, "passed", "composition_progress");
        Assert.True(before == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
        Assert.True(result.Elapsed < TimeSpan.FromSeconds(45));
        output.WriteLine($"composition_validate_boundary provider={provider} case=snapshot exit=0 code=composition_progress elapsedMilliseconds={result.Elapsed.TotalMilliseconds:F0}");
    }

    [Fact]
    public async Task ValidateBoundaryOwnedOrphanRequiresReconciliationWithoutAdoption()
    {
        var provider = Provider;
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync(provider, token);
        await fixture.SeedUnrelatedNoStorageAsync(token);
        await fixture.SeedOwnedOrphanAsync(token);
        await RunBoundaryAsync(fixture, CompositionTestCatalog.Default, 4, "reconciliationrequired",
            "composition_reconciliation_required", token);
    }

    [Trait("Provider", "sqlite")]
    public sealed class SqliteCompositionPublicContractTests(ITestOutputHelper output) : CompositionPublicContractTests(output)
    {
        protected override string Provider => "sqlite";
    }

    [Trait("Provider", "postgres")]
    public sealed class PostgresCompositionPublicContractTests(ITestOutputHelper output) : CompositionPublicContractTests(output)
    {
        protected override string Provider => "postgres";
    }

    private async Task<TimeSpan> RunBoundaryAsync(CompositionFixture fixture, CompositionTestCatalog catalog,
        int expectedExit, string expectedOutcome, string expectedCode, CancellationToken token)
    {
        var selection = catalog == CompositionTestCatalog.MalformedManifest
            ? """{"schemaVersion":"1","modules":[]}"""u8.ToArray()
            : JsonSerializer.SerializeToUtf8Bytes(new
            {
                schemaVersion = "1",
                modules = new[] { new { id = "org.example.validation", enabled = true, settings = new { secret = fixture.Canary } } }
            });
        await fixture.WriteSelectionAsync(selection, token);
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(50));
        var started = Stopwatch.GetTimestamp();
        var process = fixture.Start("validate", catalog);
        var result = await process.WaitAsync(deadline.Token);
        var elapsed = Stopwatch.GetElapsedTime(started);
        Assert.True(process.ExitVerified);
        Assert.Equal(expectedExit, result.ExitCode);
        Assert.Empty(result.Stderr);
        RequireBoundaryRecords(result.Stdout, fixture.Storage.Provider, expectedOutcome, expectedCode);
        Assert.True(before == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
        Assert.True(elapsed < TimeSpan.FromSeconds(45));
        output.WriteLine($"composition_validate_boundary provider={fixture.Storage.Provider} case={catalog} exit={result.ExitCode} code={expectedCode} elapsedMilliseconds={elapsed.TotalMilliseconds:F0}");
        return elapsed;
    }

    private static void RequireBoundaryRecords(string text, string provider, string outcome, string code)
    {
        Assert.True(System.Text.Encoding.UTF8.GetByteCount(text) <= 65_536);
        var records = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line =>
        {
            using var document = JsonDocument.Parse(line,
                new JsonDocumentOptions { MaxDepth = 8, AllowDuplicateProperties = false });
            return document.RootElement.Clone();
        }).ToArray();
        Assert.NotEmpty(records);
        Assert.True(records.Length <= 257);
        var attempt = records[0].GetProperty("attempt").GetString();
        Assert.True(attempt is { Length: 32 } && attempt.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f'));
        Assert.Equal(1, records.Count(record => record.GetProperty("stage").GetString() == "admission"));
        Assert.All(records, record =>
        {
            Assert.Equal(1, record.GetProperty("version").GetInt32());
            Assert.Equal(attempt, record.GetProperty("attempt").GetString());
            Assert.Equal("validate", record.GetProperty("mode").GetString());
            Assert.Equal(provider == "sqlite" ? "sqlite" : "postgresql", record.GetProperty("provider").GetString());
            Assert.True(record.GetProperty("stage").GetString() is "input" or "admission" or "discovered" or "validated" or "cleanup");
            Assert.True(record.GetProperty("elapsedMilliseconds").GetInt64() is >= 0 and <= 45_000);
        });
        var terminal = records[^1];
        Assert.True(terminal.GetProperty("stage").GetString() is "cleanup" or "input");
        Assert.Equal(code, terminal.GetProperty("code").GetString());
        Assert.Equal(outcome, terminal.GetProperty("outcome").GetString());
        Assert.Equal(code is "composition_output_failed" ? "restore_private_diagnostics" :
            code is "composition_cleanup_incomplete" or "composition_storage_rejected" or
                "composition_reconciliation_required" ? "inspect_preserved_state" :
            code is "composition_progress" ? "observe_current_attempt" :
            "correct_selection_and_restart", terminal.GetProperty("guidance").GetString());
    }
}
