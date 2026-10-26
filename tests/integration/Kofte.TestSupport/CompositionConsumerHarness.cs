using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Kofte.TestSupport;

public sealed record CompositionConsumerResult(int Assertions,
    IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> Observations);

public static class CompositionConsumerHarness
{
    public static async Task<ProcessOutput> InvokeAsync(string[] arguments, CancellationToken token)
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(OwnedProcesses.FindRepository(),
            "tests/contract/Kofte.Composition.Consumer/bin", configuration, "net10.0/Kofte.Composition.Consumer.dll");
        if (!File.Exists(dll)) throw new InvalidOperationException("composition_consumer_not_built");
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = OwnedProcesses.FindRepository()
        };
        start.ArgumentList.Add(dll);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        OwnedProcesses.ClearSensitiveEnvironment(start);
        if (Environment.GetEnvironmentVariable("KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE") == "true")
            start.Environment["KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE"] = "true";
        return await OwnedProcesses.RunAsync(start, TimeSpan.FromSeconds(600), token);
    }

    public static CompositionConsumerResult RequireSuccess(ProcessOutput output, string provider, string scenario)
    {
        CompositionScenarioCatalog.RequireProvider(provider);
        CompositionScenarioCatalog.RequireScenario(scenario);
        if (output.ExitCode != 0 || !string.IsNullOrWhiteSpace(output.Stderr))
            throw new InvalidOperationException("composition_consumer_failed");
        if (Encoding.UTF8.GetByteCount(output.Stdout) > 65_536)
            throw new InvalidOperationException("composition_consumer_output_limit");
        try
        {
            using var document = JsonDocument.Parse(output.Stdout, new JsonDocumentOptions
            {
                AllowDuplicateProperties = false,
                MaxDepth = 8
            });
            var root = document.RootElement;
            RequireKeys(root, ["schemaVersion", "eventCode", "attempt", "provider", "scenario",
                "status", "assertions", "scenarios"]);
            var attempt = root.GetProperty("attempt").GetString();
            if (root.GetProperty("schemaVersion").GetInt32() != 1 ||
                root.GetProperty("eventCode").GetString() != "composition_scenarios_passed" ||
                attempt is null || attempt.Length != 32 ||
                attempt.Any(value => value is not (>= '0' and <= '9' or >= 'a' and <= 'f')) ||
                root.GetProperty("provider").GetString() != provider ||
                root.GetProperty("scenario").GetString() != scenario ||
                root.GetProperty("status").GetString() != "passed" ||
                !root.GetProperty("assertions").TryGetInt32(out var total) || total <= 0)
                throw InvalidResult();
            var cases = root.GetProperty("scenarios");
            var expected = scenario == "all" ? CompositionScenarioCatalog.Scenarios : [scenario];
            RequireKeys(cases, expected);
            long sum = 0;
            var observations = new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.Ordinal);
            foreach (var name in expected)
            {
                var item = cases.GetProperty(name);
                RequireKeys(item, ["assertions", "observations"]);
                if (!item.GetProperty("assertions").TryGetInt32(out var count) || count <= 0)
                    throw InvalidResult();
                sum += count;
                var evidence = item.GetProperty("observations");
                var required = CompositionScenarioCatalog.RequiredObservations(name);
                RequireKeys(evidence, required.Keys.ToArray());
                var values = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var (field, minimum) in required)
                {
                    if (!evidence.GetProperty(field).TryGetInt32(out var measured) || measured < minimum)
                        throw InvalidResult();
                    values.Add(field, measured);
                }
                observations.Add(name, values.AsReadOnly());
            }
            if (sum != total) throw InvalidResult();
            return new(total, observations.AsReadOnly());
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or
            KeyNotFoundException or FormatException or OverflowException)
        {
            throw InvalidResult();
        }
    }

    private static void RequireKeys(JsonElement value, IReadOnlyCollection<string> names)
    {
        if (value.ValueKind != JsonValueKind.Object) throw InvalidResult();
        var actual = value.EnumerateObject().Select(property => property.Name).ToArray();
        if (actual.Length != names.Count || !actual.ToHashSet(StringComparer.Ordinal).SetEquals(names))
            throw InvalidResult();
    }

    private static InvalidOperationException InvalidResult() => new("composition_consumer_result_invalid");
}
