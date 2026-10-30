using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kofte.Infrastructure.Storage;
using Kofte.TestSupport;
using Kofte.TestSupport.Identity;

namespace Kofte.Platform.ContractTests.Identity;

public sealed class IdentityConsumerHarnessTests
{
    [Theory]
    [InlineData("identity_scenario_upgrade_exact_module_inventory_preserved", true)]
    [InlineData("identity_fixture_operator_failed", true)]
    [InlineData("test_identity_preparation_failed", true)]
    [InlineData("identity_fixture_operator_prepare_prepare_identity_keys_invalid", true)]
    [InlineData("identity_fixture_operator_registry_upgrade_upgrade_storage_privilege_denied", true)]
    [InlineData("identity_fixture_child_readiness_http_503_configured", true)]
    [InlineData("identity_fixture_operator_prepare_prepare_identity_private_canary", false)]
    [InlineData("identity_fixture_private_canary", false)]
    [InlineData("private-canary", false)]
    public void ChildFailureRetainsOnlyStrictlyBoundClosedEvidence(string code, bool admitted)
    {
        var json = JsonSerializer.Serialize(new
        {
            schemaVersion = 1,
            eventCode = code,
            attempt = new string('a', 32),
            provider = "sqlite",
            scenario = "upgrade-preservation",
            status = "failed",
            assertions = 0
        });
        var error = Assert.Throws<InvalidOperationException>(() => IdentityConsumerHarness.RequireSuccess(
            new(1, "", json), "sqlite", "upgrade-preservation"));
        Assert.Equal(admitted ? code : "identity_consumer_failed", error.Message);
        Assert.DoesNotContain("private-canary", error.Message);
    }

    [Theory]
    [InlineData("stdout")]
    [InlineData("extra-record")]
    [InlineData("extra-field")]
    [InlineData("duplicate-field")]
    [InlineData("truncated")]
    [InlineData("wrong-provider")]
    [InlineData("wrong-scenario")]
    [InlineData("wrong-type")]
    public void KnownFailureStillRequiresOneExactBoundEnvelope(string variant)
    {
        var value = JsonSerializer.SerializeToNode(new
        {
            schemaVersion = 1,
            eventCode = "identity_fixture_operator_failed",
            attempt = new string('a', 32),
            provider = "sqlite",
            scenario = "upgrade-preservation",
            status = "failed",
            assertions = 0
        })!.AsObject();
        if (variant == "extra-field") value["private-canary"] = true;
        if (variant == "wrong-provider") value["provider"] = "postgres";
        if (variant == "wrong-scenario") value["scenario"] = "private-evidence";
        if (variant == "wrong-type") value["assertions"] = "0";
        var json = value.ToJsonString();
        if (variant == "extra-record") json += "\n" + json;
        if (variant == "duplicate-field") json = json.Insert(1, "\"eventCode\":\"identity_fixture_operator_failed\",");
        if (variant == "truncated") json = json[..^1];
        var error = Assert.Throws<InvalidOperationException>(() => IdentityConsumerHarness.RequireSuccess(
            new(1, variant == "stdout" ? "private-canary" : "", json), "sqlite", "upgrade-preservation"));
        Assert.Equal("identity_consumer_failed", error.Message);
    }

    [Theory]
    [InlineData("valid", "identity_fixture_operator_prepare_prepare_identity_keys_invalid")]
    [InlineData("ready-prefix", "identity_fixture_operator_prepare_prepare_identity_keys_invalid")]
    [InlineData("unknown", "identity_fixture_operator_failed")]
    [InlineData("duplicate", "identity_fixture_operator_failed")]
    [InlineData("malformed", "identity_fixture_operator_failed")]
    [InlineData("extra-field", "identity_fixture_operator_failed")]
    [InlineData("extra-record", "identity_fixture_operator_failed")]
    [InlineData("stdout", "identity_fixture_operator_failed")]
    public void StoppedOperationRetainsOnlyValidatedDeliveredFailure(string variant, string expected)
    {
        var record = JsonSerializer.SerializeToNode(new
        {
            version = 1,
            eventCode = "identity_keys_invalid",
            component = "identity",
            operation = "prepare",
            mode = "oidc",
            provider = "sqlite",
            outcome = "rejected",
            correlationId = new string('a', 32),
            guidance = "inspect_preserved_keys",
            durability = "unchanged"
        })!.AsObject();
        if (variant == "unknown") record["eventCode"] = "identity_private_canary";
        if (variant == "extra-field") record["private-canary"] = "private-canary";
        var json = record.ToJsonString();
        if (variant == "ready-prefix")
            json = JsonSerializer.Serialize(new
            {
                eventCode = "storage_ready",
                component = "storage",
                operation = "acquire",
                provider = "sqlite",
                outcome = "ready",
                moduleId = (string?)null,
                correlationId = new string('b', 32),
                guidance = "retry_same_request",
                durability = "unchanged"
            }) + "\n" + json;
        if (variant == "duplicate") json = json.Insert(1, "\"eventCode\":\"identity_keys_invalid\",");
        if (variant == "malformed") json = json[..^1];
        if (variant == "extra-record") json += "\nprivate-canary";
        var code = IdentityConsumerFailureCodes.StoppedOperation(["--identity", "prepare"],
            new(1, variant == "stdout" ? "private-canary" : "", json));
        Assert.Equal(expected, code);
        Assert.True(IdentityScenarioCatalog.IsSafeFailureCode(code));
        Assert.DoesNotContain("canary", code);
    }

    [Theory]
    [InlineData("identity_scenario_operations_line_")]
    [InlineData("identity_scenario_operations_line_0")]
    [InlineData("identity_scenario_operations_line_01")]
    [InlineData("identity_scenario_operations_line_999999")]
    [InlineData("identity_scenario_operations_fixture_line_-1")]
    [InlineData("identity_scenario_operations_fixture_line_1_private_canary")]
    public void SourceLocationsMustBeCanonicalAndBoundToActualSource(string code) =>
        Assert.False(IdentityScenarioCatalog.IsSafeFailureCode(code));

    [Theory]
    [InlineData("identity_scenario_operations_line_1")]
    [InlineData("identity_scenario_operations_fixture_line_1")]
    [InlineData("identity_scenario_development_line_1")]
    public void ActualBoundedSourceLocationsUseTheSameFailureContract(string code) =>
        Assert.True(IdentityScenarioCatalog.IsSafeFailureCode(code));

    [Theory]
    [InlineData(false, "identity_fixture_operator_registry_upgrade_upgrade_storage_privilege_denied")]
    [InlineData(true, "identity_fixture_operator_failed")]
    public void ValidStorageFailureSurvivesOnlyWithItsClosedDiagnosticTuple(bool privateCode, string expected)
    {
        var record = JsonSerializer.Serialize(new
        {
            eventCode = privateCode ? "storage_private_canary" : "storage_privilege_denied",
            component = "storage",
            operation = "upgrade",
            provider = "postgresql",
            outcome = "failed",
            moduleId = (string?)null,
            correlationId = new string('a', 32),
            guidance = "correct_configuration",
            durability = "unchanged"
        });
        var code = IdentityConsumerFailureCodes.StoppedOperation(["--registry", "upgrade"], new(1, "", record));
        Assert.Equal(expected, code);
        Assert.True(IdentityScenarioCatalog.IsSafeFailureCode(code));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"version\":1,\"version\":1,\"canaries\":[]}")]
    [InlineData("{\"version\":1,\"canaries\":[\"short\"]}")]
    [InlineData("{\"version\":1,\"canaries\":[null,null,null]}")]
    [InlineData("{\"version\":2,\"canaries\":[]}")]
    [InlineData("{\"version\":2147483648,\"canaries\":[]}")]
    public void MissingMalformedOrUnboundedBrowserCanaryDeliveryCannotPass(string json) =>
        Assert.Equal("identity_browser_transport_canaries_invalid",
            Assert.Throws<InvalidOperationException>(() =>
                IdentityBrowserTransportPrivacy.ReadCanaries(Encoding.UTF8.GetBytes(json))).Message);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EveryActualTransportChannelRequiresCaptureAndRefusesRawOrEncodedSecrets(bool withIngress)
    {
        string[] canaries = ["private/secret?canary", "private-identity-subject", "private-session-reference"];
        var delivered = JsonSerializer.SerializeToUtf8Bytes(new { version = 1, canaries });
        Assert.Equal(canaries, IdentityBrowserTransportPrivacy.ReadCanaries(delivered));
        string[] channels = withIngress
            ? ["Next ready", "ingress callback", "native callback committed"]
            : ["Next ready", "native development committed"];
        IdentityBrowserTransportPrivacy.RequireClean(channels, canaries, withIngress);
        if (!withIngress)
            Assert.Throws<InvalidOperationException>(() =>
                IdentityBrowserTransportPrivacy.RequireClean(channels, canaries));
        for (var index = 0; index < channels.Length; index++)
        {
            foreach (var value in new[] { canaries[0], Uri.EscapeDataString(canaries[0]) })
            {
                var leaked = channels.ToArray();
                leaked[index] += value;
                Assert.Equal("identity_browser_transport_disclosure",
                    Assert.Throws<InvalidOperationException>(() =>
                        IdentityBrowserTransportPrivacy.RequireClean(leaked, canaries, withIngress)).Message);
            }
            var absent = channels.ToArray();
            absent[index] = "";
            Assert.Equal("identity_browser_transport_capture_missing",
                Assert.Throws<InvalidOperationException>(() =>
                    IdentityBrowserTransportPrivacy.RequireClean(absent, canaries, withIngress)).Message);
        }
        Assert.Throws<InvalidOperationException>(() =>
            IdentityBrowserTransportPrivacy.ReadCanaries(JsonSerializer.SerializeToUtf8Bytes(
                new { version = 1, canaries = new[] { canaries[0], canaries[0], canaries[2] } })));
        Assert.Throws<InvalidOperationException>(() =>
            IdentityBrowserTransportPrivacy.ReadCanaries(new byte[65_537]));
    }

    [Theory]
    [InlineData("oidc-login")]
    [InlineData("trust-refusal")]
    [InlineData("nominated-enrollment")]
    [InlineData("enrollment-race")]
    [InlineData("session-boundary")]
    [InlineData("development-isolation")]
    [InlineData("upgrade-preservation")]
    [InlineData("interrupted-recovery")]
    [InlineData("private-evidence")]
    [InlineData("all")]
    public void MissingTruncatedExtraReorderedOrDuplicateEvidenceNeverPasses(string scenario)
    {
        var valid = Valid(scenario);
        IdentityConsumerHarness.RequireSuccess(new(0, valid.ToJsonString(), ""), "sqlite", scenario);
        foreach (var item in valid["scenarios"]!.AsObject())
        {
            var omitted = valid.DeepClone();
            omitted["scenarios"]!.AsObject().Remove(item.Key);
            Refused(omitted);
            foreach (var field in new[] { "assertions", "observations" })
            {
                var missing = valid.DeepClone();
                missing["scenarios"]![item.Key]!.AsObject().Remove(field);
                Refused(missing);
            }
            var original = item.Value!["observations"]!.AsArray();
            for (var index = 0; index < original.Count; index++)
            {
                var missing = valid.DeepClone();
                missing["scenarios"]![item.Key]!["observations"]!.AsArray().RemoveAt(index);
                Refused(missing);
            }
            var extra = valid.DeepClone();
            extra["scenarios"]![item.Key]!["observations"]!.AsArray().Add(original[0]!.DeepClone());
            Refused(extra);
            var unknown = valid.DeepClone();
            unknown["scenarios"]![item.Key]!["observations"]![0] = "private-canary";
            Refused(unknown);
            var reordered = valid.DeepClone();
            reordered["scenarios"]![item.Key]!["observations"]![0] = original[^1]!.DeepClone();
            Refused(reordered);
            var wrongCount = valid.DeepClone();
            wrongCount["scenarios"]![item.Key]!["assertions"] = original.Count + 1;
            Refused(wrongCount);
            var extraField = valid.DeepClone();
            extraField["scenarios"]![item.Key]!["extra"] = "private-canary";
            Refused(extraField);
        }

        void Refused(JsonNode json) => Assert.Equal("identity_consumer_result_invalid",
            Assert.Throws<InvalidOperationException>(() => IdentityConsumerHarness.RequireSuccess(
                new(0, json.ToJsonString(), ""), "sqlite", scenario)).Message);
    }

    [Theory]
    [InlineData("schemaVersion", "2")]
    [InlineData("eventCode", "\"private-canary\"")]
    [InlineData("provider", "\"postgres\"")]
    [InlineData("scenario", "\"all\"")]
    [InlineData("attempt", "\"private-canary\"")]
    [InlineData("status", "\"failed\"")]
    [InlineData("assertions", "0")]
    [InlineData("unknown", "null")]
    public void InvalidEnvelopesNeverPass(string field, string json)
    {
        var result = Valid("oidc-login");
        result[field] = JsonNode.Parse(json);
        Assert.Throws<InvalidOperationException>(() =>
            IdentityConsumerHarness.RequireSuccess(new(0, result.ToJsonString(), ""), "sqlite", "oidc-login"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"schemaVersion\":1,\"schemaVersion\":1}")]
    [InlineData("{}\n{}")]
    public void MalformedTruncatedOrDuplicateJsonCannotPass(string json) =>
        Assert.Throws<InvalidOperationException>(() =>
            IdentityConsumerHarness.RequireSuccess(new(0, json, ""), "sqlite", "all"));

    [Fact]
    public void DuplicateNestedKeysNonzeroStderrAndOverLimitCannotPass()
    {
        var json = Valid("all").ToJsonString();
        foreach (var output in new ProcessOutput[]
        {
            new(1, json, ""), new(0, json, " "), new(0, json + new string(' ', 65_536), ""),
            new(0, json.Replace("\"observations\":", "\"assertions\":1,\"observations\":", StringComparison.Ordinal), "")
        })
            Assert.Throws<InvalidOperationException>(() =>
                IdentityConsumerHarness.RequireSuccess(output, "sqlite", "all"));
    }

    [Fact]
    public void MissingSourceAndAbsentOrLeakingCaptureFailClosed()
    {
        using var root = new OwnedTestRoot();
        Assert.Equal("identity_consumer_source_missing", Assert.Throws<InvalidOperationException>(() =>
            IdentityConsumerHarness.RequireSources(root.Path)).Message);
        using var capture = new IdentityOperationsCapture();
        Assert.False(capture.HasCorrelatedChannels("sqlite"));
        capture.WriteLine("private-canary");
        Assert.False(capture.IsPrivate(["private-canary"]));
        Assert.Throws<InvalidOperationException>(() => capture.Records);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void AnyUnverifiedOwnedExitRejectsSuccess(bool root, bool descendants) =>
        Assert.Equal("identity_consumer_cleanup_unverified", Assert.Throws<InvalidOperationException>(() =>
            IdentityConsumerHarness.RequireCleanup(root, descendants)).Message);

    [Fact]
    public async Task TimeoutTerminatesTheActuallyAdmittedOwnedProcessBeforeReturning()
    {
        using var root = new OwnedTestRoot();
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        Directory.CreateDirectory(root.Path, UnixStorePaths.DirectoryMode);
        var admission = Path.Combine(root.Path, "admission");
        PrivateTestFiles.WriteNew(admission, JsonSerializer.SerializeToUtf8Bytes(
            new { version = 1, arguments = new[] { "--fixture-idle" } }));
        var barrier = new IdentityCommitBarrier(root.Path, "fixture_ready");
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = OwnedProcesses.FindRepository(),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        start.ArgumentList.Add(Path.Combine(OwnedProcesses.FindRepository(),
            "tests/contract/Kofte.Identity.Consumer/bin", configuration, "net10.0/Kofte.Identity.Consumer.dll"));
        start.ArgumentList.Add("--identity-fixture-worker");
        OwnedProcesses.ClearSensitiveEnvironment(start);
        start.Environment["IDENTITY_TEST_ADMISSION_FILE"] = admission;
        start.Environment["IDENTITY_TEST_BARRIER_FILE"] = barrier.DescriptorPath;
        start.Environment["DOTNET_STARTUP_HOOKS"] = typeof(IdentityCommitBarrier).Assembly.Location;
        var completion = IdentityConsumerHarness.RunOwnedAsync(start, TimeSpan.FromSeconds(3),
            TestContext.Current.CancellationToken);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await barrier.WaitAsync(deadline.Token);
        using var process = Process.GetProcessById(barrier.ObservedProcessId!.Value);
        barrier.Release();
        Assert.Equal("identity_consumer_timeout",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => completion)).Message);
        Assert.True(process.HasExited);
    }

    [Theory]
    [InlineData("provider")]
    [InlineData("scenario")]
    [InlineData("extra")]
    [InlineData("worker")]
    public async Task UnsupportedSelectorsAndWorkerBypassCannotEchoInputs(string variant)
    {
        var args = variant switch
        {
            "provider" => new[] { "--provider", "private-canary", "--scenario", "all" },
            "scenario" => ["--provider", "sqlite", "--scenario", "private-canary"],
            "extra" => ["--provider", "sqlite", "--scenario", "all", "private-canary"],
            _ => ["--identity-fixture-worker", "private-canary"]
        };
        var output = await IdentityConsumerHarness.InvokeAsync(args, TestContext.Current.CancellationToken);
        Assert.NotEqual(0, output.ExitCode);
        Assert.Empty(output.Stdout);
        Assert.DoesNotContain("private-canary", output.Stderr);
    }

    private static JsonObject Valid(string scenario)
    {
        var names = scenario == "all" ? IdentityScenarioCatalog.Scenarios : [scenario];
        return JsonSerializer.SerializeToNode(new
        {
            schemaVersion = 1,
            eventCode = "identity_scenarios_passed",
            attempt = new string('a', 32),
            provider = "sqlite",
            scenario,
            status = "passed",
            assertions = names.Sum(name => IdentityScenarioCatalog.RequiredObservations(name).Count),
            scenarios = names.ToDictionary(name => name, name => new
            {
                assertions = IdentityScenarioCatalog.RequiredObservations(name).Count,
                observations = IdentityScenarioCatalog.RequiredObservations(name)
            }, StringComparer.Ordinal)
        })!.AsObject();
    }
}
