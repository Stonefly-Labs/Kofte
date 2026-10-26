using System.Text.Json;
using System.Text.Json.Nodes;
using Kofte.TestSupport;

namespace Kofte.Platform.ContractTests.Composition;

public sealed class CompositionConsumerHarnessTests
{
    [Theory]
    [InlineData("missing")]
    [InlineData("provider")]
    [InlineData("scenario")]
    [InlineData("extra")]
    [InlineData("worker")]
    public async Task UnsupportedArgumentsAndWorkerBypassFailWithoutInputEcho(string variant)
    {
        var arguments = variant switch
        {
            "provider" => new[] { "--provider", "private-canary", "--scenario", "all" },
            "scenario" => ["--provider", "sqlite", "--scenario", "/private/private-canary"],
            "extra" => ["--provider", "sqlite", "--scenario", "all", "private-canary"],
            "worker" => ["--composition-worker", "private-canary"],
            _ => []
        };
        var output = await CompositionConsumerHarness.InvokeAsync(arguments, TestContext.Current.CancellationToken);
        Assert.NotEqual(0, output.ExitCode);
        Assert.Empty(output.Stdout);
        Assert.DoesNotContain("private-canary", output.Stderr);
        using var document = JsonDocument.Parse(output.Stderr);
        Assert.Equal("failed", document.RootElement.GetProperty("status").GetString());
        Assert.Equal(0, document.RootElement.GetProperty("assertions").GetInt32());
    }

    [Theory]
    [InlineData("empty-boot")]
    [InlineData("valid-dependency")]
    [InlineData("disabled-missing")]
    [InlineData("invalid-composition")]
    [InlineData("deterministic-corpus")]
    [InlineData("ordered-migration")]
    [InlineData("disable-reenable")]
    [InlineData("failure-correction")]
    [InlineData("health-impact")]
    [InlineData("bounded-stop")]
    [InlineData("private-diagnostics")]
    [InlineData("all")]
    public void EveryRequiredSemanticObservationMustBePresentAndSufficient(string scenario)
    {
        var result = ValidResult(scenario);
        var output = new ProcessOutput(0, result.ToJsonString(), "");
        CompositionConsumerHarness.RequireSuccess(output, "sqlite", scenario);
        foreach (var item in result["scenarios"]!.AsObject())
        {
            foreach (var field in item.Value!["observations"]!.AsObject())
            {
                var omitted = result.DeepClone();
                omitted["scenarios"]![item.Key]!["observations"]!.AsObject().Remove(field.Key);
                Refused(omitted);
                var decremented = result.DeepClone();
                decremented["scenarios"]![item.Key]!["observations"]![field.Key] = field.Value!.GetValue<int>() - 1;
                Refused(decremented);
            }
            var missing = result.DeepClone();
            missing["scenarios"]!.AsObject().Remove(item.Key);
            Refused(missing);
        }

        void Refused(JsonNode changed) => Assert.Throws<InvalidOperationException>(() =>
            CompositionConsumerHarness.RequireSuccess(new(0, changed.ToJsonString(), ""), "sqlite", scenario));
    }

    [Theory]
    [InlineData("status", "\"failed\"")]
    [InlineData("provider", "\"postgres\"")]
    [InlineData("scenario", "\"private-canary\"")]
    [InlineData("schemaVersion", "2")]
    [InlineData("attempt", "\"private-canary\"")]
    [InlineData("assertions", "0")]
    [InlineData("assertions", "100000")]
    [InlineData("unknown", "\"private-canary\"")]
    public void InvalidEnvelopesCannotPass(string field, string value)
    {
        var result = ValidResult("all");
        result[field] = JsonNode.Parse(value);
        var error = Assert.Throws<InvalidOperationException>(() =>
            CompositionConsumerHarness.RequireSuccess(new(0, result.ToJsonString(), ""), "sqlite", "all"));
        Assert.DoesNotContain("private-canary", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"schemaVersion\":1,\"schemaVersion\":1}")]
    [InlineData("{}\n{}")]
    public void MalformedMissingDuplicateOrMultipleResultsCannotPass(string json) =>
        Assert.Throws<InvalidOperationException>(() =>
            CompositionConsumerHarness.RequireSuccess(new(0, json, ""), "sqlite", "all"));

    [Fact]
    public void NonzeroExitUnexpectedStderrAndExcessOutputCannotPass()
    {
        var json = ValidResult("all").ToJsonString();
        Assert.Throws<InvalidOperationException>(() =>
            CompositionConsumerHarness.RequireSuccess(new(1, json, ""), "sqlite", "all"));
        Assert.Throws<InvalidOperationException>(() =>
            CompositionConsumerHarness.RequireSuccess(new(0, json, "private-canary"), "sqlite", "all"));
        Assert.Throws<InvalidOperationException>(() =>
            CompositionConsumerHarness.RequireSuccess(new(0, json + new string(' ', 65_536), ""), "sqlite", "all"));
    }

    [Theory]
    [InlineData("sqlite", "unknown")]
    [InlineData("unknown", "all")]
    public void UnadmittedSelectorsRejectWithoutReflection(string provider, string scenario)
    {
        var error = Assert.Throws<InvalidOperationException>(() =>
            CompositionConsumerHarness.RequireSuccess(new(0, "{}", ""), provider, scenario));
        Assert.DoesNotContain("unknown", error.Message);
    }

    private static JsonObject ValidResult(string scenario)
    {
        IEnumerable<string> selected = scenario == "all" ? CompositionScenarioCatalog.Scenarios : [scenario];
        var cases = selected.ToDictionary(name => name, name => new
        {
            assertions = 100,
            observations = CompositionScenarioCatalog.RequiredObservations(name)
        }, StringComparer.Ordinal);
        return JsonSerializer.SerializeToNode(new
        {
            schemaVersion = 1,
            eventCode = "composition_scenarios_passed",
            attempt = new string('a', 32),
            provider = "sqlite",
            scenario,
            status = "passed",
            assertions = cases.Count * 100,
            scenarios = cases
        })!.AsObject();
    }
}
