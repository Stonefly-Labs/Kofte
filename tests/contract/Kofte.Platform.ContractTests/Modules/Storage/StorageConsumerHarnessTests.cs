using System.Text.Json;
using Kofte.TestSupport;

namespace Kofte.Platform.ContractTests.Modules.Storage;

public sealed class StorageConsumerHarnessTests
{
    [Theory]
    [InlineData("")]
    [InlineData("missing-provider")]
    [InlineData("unknown-provider")]
    [InlineData("unknown-scenario")]
    [InlineData("extra-argument")]
    public async Task InvalidSelectorsFailWithoutEchoingInput(string variant)
    {
        var arguments = variant switch
        {
            "missing-provider" => new[] { "--scenario", "clean-install" },
            "unknown-provider" => ["--provider", "credential-canary", "--scenario", "clean-install"],
            "unknown-scenario" => ["--provider", "sqlite", "--scenario", "/private/credential-canary"],
            "extra-argument" => ["--provider", "sqlite", "--scenario", "clean-install", "credential-canary"],
            _ => []
        };
        var output = await StorageConsumerHarness.InvokeAsync(arguments, null, TestContext.Current.CancellationToken);
        Assert.NotEqual(0, output.ExitCode);
        Assert.Empty(output.Stdout);
        Assert.DoesNotContain("credential-canary", output.Stderr);
        using var result = JsonDocument.Parse(output.Stderr);
        Assert.Equal("failed", result.RootElement.GetProperty("status").GetString());
        Assert.Equal(0, result.RootElement.GetProperty("assertions").GetInt32());
    }

    [Theory]
    [InlineData("sqlite")]
    [InlineData("postgres")]
    public async Task RecognizedScenariosWithoutOwnedConfigurationExplicitlyFail(string provider)
    {
        var output = await StorageConsumerHarness.InvokeAsync(
            ["--provider", provider, "--scenario", "clean-install"], null, TestContext.Current.CancellationToken);
        Assert.NotEqual(0, output.ExitCode);
        using var result = JsonDocument.Parse(output.Stderr);
        Assert.Equal("storage_scenario_configuration_required", result.RootElement.GetProperty("eventCode").GetString());
        Assert.Throws<InvalidOperationException>(() =>
            StorageConsumerHarness.RequireSuccess(output, provider, "clean-install"));
    }

    [Theory]
    [InlineData(1, 1, "passed", "sqlite", "clean-install", "")]
    [InlineData(0, 0, "passed", "sqlite", "clean-install", "")]
    [InlineData(0, -1, "passed", "sqlite", "clean-install", "")]
    [InlineData(0, 1, "failed", "sqlite", "clean-install", "")]
    [InlineData(0, 1, "passed", "postgres", "clean-install", "")]
    [InlineData(0, 1, "passed", "sqlite", "no-storage", "")]
    [InlineData(0, 1, "passed", "sqlite", "clean-install", "unexpected")]
    public void BadExitZeroAssertionsAndMismatchedResultsCannotPass(int exit, int assertions,
        string status, string provider, string scenario, string stderr)
    {
        var json = JsonSerializer.Serialize(new
        {
            eventCode = "storage_scenario_passed",
            component = "storage-consumer",
            correlationId = new string('a', 32),
            provider,
            scenario,
            status,
            assertions
        });
        Assert.Throws<InvalidOperationException>(() =>
            StorageConsumerHarness.RequireSuccess(new ProcessOutput(exit, json, stderr), "sqlite", "clean-install"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("{\"status\":\"passed\"}\n{\"status\":\"passed\"}")]
    public void MissingMalformedAndMultipleResultsCannotPass(string json) =>
        Assert.Throws<InvalidOperationException>(() =>
            StorageConsumerHarness.RequireSuccess(new ProcessOutput(0, json, ""), "sqlite", "clean-install"));
}
