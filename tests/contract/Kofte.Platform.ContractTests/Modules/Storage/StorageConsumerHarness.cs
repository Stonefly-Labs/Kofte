using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kofte.TestSupport;

namespace Kofte.Platform.ContractTests.Modules.Storage;

internal sealed record StorageConsumerResult(string Provider, string Scenario, int Assertions,
    IReadOnlyDictionary<string, int> Scenarios, IReadOnlyDictionary<string, StorageExampleEvidence> Observations);

internal static class StorageConsumerHarness
{
    internal static async Task<StorageConsumerResult> RunAsync(StorageProviderScope scope, string scenario,
        CancellationToken token)
    {
        StorageScenarioCatalog.RequireScenario(scenario);
        var output = await InvokeAsync(["--provider", scope.Provider, "--scenario", scenario], scope, token);
        return RequireSuccess(output, scope.Provider, scenario);
    }

    internal static async Task<ProcessOutput> InvokeAsync(string[] arguments, StorageProviderScope? scope,
        CancellationToken token)
    {
        var root = OwnedProcesses.FindRepository();
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(root, "tests", "contract", "Kofte.Storage.Consumer", "bin",
            configuration, "net10.0", "Kofte.Storage.Consumer.dll");
        if (!File.Exists(dll)) throw new InvalidOperationException("storage_consumer_not_built");
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = root
        };
        start.ArgumentList.Add(dll);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        OwnedProcesses.ClearSensitiveEnvironment(start);
        scope?.ConfigureChild(start, offlineMaintenance: true);
        using var budget = scope is null
            ? CancellationTokenSource.CreateLinkedTokenSource(token)
            : CancellationTokenSource.CreateLinkedTokenSource(token, scope.Token);
        return await OwnedProcesses.RunAsync(start, TimeSpan.FromSeconds(120), budget.Token);
    }

    internal static StorageConsumerResult RequireSuccess(ProcessOutput output, string provider, string scenario)
    {
        StorageScenarioCatalog.RequireProvider(provider);
        StorageScenarioCatalog.RequireScenario(scenario);
        if (output.ExitCode != 0 || !string.IsNullOrWhiteSpace(output.Stderr))
            throw new InvalidOperationException("storage_consumer_failed");
        try
        {
            using var document = JsonDocument.Parse(output.Stdout);
            var result = document.RootElement;
            var names = result.EnumerateObject().Select(p => p.Name).ToArray();
            if (names.Length != 9 || names.Distinct(StringComparer.Ordinal).Count() != 9 ||
                names.Except(["eventCode", "component", "correlationId", "provider", "scenario", "status", "assertions", "scenarios", "observations"]).Any() ||
                result.GetProperty("eventCode").GetString() != "storage_scenario_passed" ||
                result.GetProperty("component").GetString() != "storage-consumer" ||
                !Regex.IsMatch(result.GetProperty("correlationId").GetString() ?? "", @"\A[0-9a-f]{32}\z") ||
                result.GetProperty("provider").GetString() != provider ||
                result.GetProperty("scenario").GetString() != scenario ||
                result.GetProperty("status").GetString() != "passed" ||
                !result.GetProperty("assertions").TryGetInt32(out var assertions) || assertions <= 0)
                throw new InvalidOperationException("storage_consumer_result_invalid");
            var cases = result.GetProperty("scenarios").EnumerateObject().ToArray();
            IEnumerable<string> expected = scenario == "all" ? StorageScenarioCatalog.Scenarios : [scenario];
            if (!cases.Select(item => item.Name).SequenceEqual(expected) ||
                cases.Any(item => !item.Value.TryGetInt32(out var count) || count <= 0) ||
                cases.Sum(item => item.Value.GetInt64()) != assertions)
                throw new InvalidOperationException("storage_consumer_result_invalid");
            return new(provider, scenario, assertions,
                cases.ToDictionary(item => item.Name, item => item.Value.GetInt32(), StringComparer.Ordinal),
                StorageExampleEvidence.Read(result.GetProperty("observations"), expected));
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new InvalidOperationException("storage_consumer_result_invalid");
        }
    }
}
