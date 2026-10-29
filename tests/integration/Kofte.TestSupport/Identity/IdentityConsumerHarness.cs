using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Kofte.TestSupport.Identity;

public sealed record IdentityConsumerResult(int Assertions,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Observations);

public static class IdentityConsumerHarness
{
    public static void RequireSources(string repository)
    {
        string[] required =
        [
            "tests/contract/Kofte.Identity.Consumer/Program.cs",
            "tests/contract/Kofte.Identity.Consumer/FixtureProgram.cs",
            "tests/contract/Kofte.Identity.Consumer/ActorProbe.cs",
            "tests/contract/Kofte.Identity.Consumer/Kofte.Identity.Consumer.csproj",
            "tests/integration/Kofte.TestSupport/Identity/IdentityScenarioCatalog.cs",
            "tests/integration/Kofte.TestSupport/Identity/IdentityConsumerFailureCodes.cs",
            "tests/integration/Kofte.TestSupport/Identity/LoginCommitFixture.Operations.cs",
            .. new[] { "Login", "Actor", "Enrollment", "Session", "Development", "Operations" }
                .Select(name => $"tests/contract/Kofte.Identity.Consumer/Scenarios/{name}Scenarios.cs")
        ];
        if (required.Any(path => !File.Exists(Path.Combine(repository, path))))
            throw new InvalidOperationException("identity_consumer_source_missing");
    }

    public static Task<ProcessOutput> InvokeAsync(string[] arguments, CancellationToken token)
    {
        var repository = OwnedProcesses.FindRepository();
        RequireSources(repository);
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(repository, "tests/contract/Kofte.Identity.Consumer/bin",
            configuration, "net10.0/Kofte.Identity.Consumer.dll");
        if (!File.Exists(dll)) throw new InvalidOperationException("identity_consumer_not_built");
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            WorkingDirectory = repository
        };
        start.ArgumentList.Add(dll);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        OwnedProcesses.ClearSensitiveEnvironment(start);
        if (Environment.GetEnvironmentVariable("KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE") == "true")
            start.Environment["KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE"] = "true";
        return RunOwnedAsync(start, arguments.Contains("all", StringComparer.Ordinal)
            ? TimeSpan.FromSeconds(600) : TimeSpan.FromSeconds(120), token);
    }

    public static async Task<ProcessOutput> RunOwnedAsync(ProcessStartInfo start, TimeSpan timeout,
        CancellationToken token)
    {
        if (timeout <= TimeSpan.Zero || timeout > TimeSpan.FromSeconds(600))
            throw new InvalidOperationException("identity_consumer_timeout_invalid");
        token.ThrowIfCancellationRequested();
        await using var child = new IdentityProcess(OwnedProcesses.Start(start, captureOutput: false),
            "identity-harness-" + Guid.NewGuid().ToString("N"));
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(timeout);
        try
        {
            var output = await child.WaitAsync(budget.Token);
            RequireCleanup(child.ExitVerified, child.DescendantExitsVerified);
            return output;
        }
        catch (OperationCanceledException)
        {
            await child.StopAsync();
            RequireCleanup(child.ExitVerified, child.DescendantExitsVerified);
            throw new InvalidOperationException("identity_consumer_timeout");
        }
    }

    public static void RequireCleanup(bool exitVerified, bool descendantExitsVerified)
    {
        if (!exitVerified || !descendantExitsVerified)
            throw new InvalidOperationException("identity_consumer_cleanup_unverified");
    }

    public static async Task<IdentityConsumerResult> RunAsync(string provider, string scenario,
        CancellationToken token)
    {
        IdentityScenarioCatalog.RequireProvider(provider);
        IdentityScenarioCatalog.RequireScenario(scenario);
        return RequireSuccess(await InvokeAsync(["--scenario", scenario, "--provider", provider], token),
            provider, scenario);
    }

    public static IdentityConsumerResult RequireSuccess(ProcessOutput output, string provider, string scenario)
    {
        IdentityScenarioCatalog.RequireProvider(provider);
        IdentityScenarioCatalog.RequireScenario(scenario);
        if (output.ExitCode != 0 || !string.IsNullOrEmpty(output.Stderr))
            throw new InvalidOperationException(SafeFailure(output, provider, scenario));
        if (Encoding.UTF8.GetByteCount(output.Stdout) > 65_536) throw InvalidResult();
        try
        {
            using var document = JsonDocument.Parse(output.Stdout, new()
            {
                AllowDuplicateProperties = false,
                MaxDepth = 8
            });
            var root = document.RootElement;
            RequireKeys(root, ["schemaVersion", "eventCode", "attempt", "provider", "scenario", "status",
                "assertions", "scenarios"]);
            if (root.GetProperty("schemaVersion").GetInt32() != 1 ||
                root.GetProperty("eventCode").GetString() != "identity_scenarios_passed" ||
                root.GetProperty("attempt").GetString() is not { Length: 32 } attempt ||
                !attempt.All(char.IsAsciiHexDigitLower) ||
                root.GetProperty("provider").GetString() != provider ||
                root.GetProperty("scenario").GetString() != scenario ||
                root.GetProperty("status").GetString() != "passed" ||
                !root.GetProperty("assertions").TryGetInt32(out var total))
                throw InvalidResult();
            var expected = scenario == "all" ? IdentityScenarioCatalog.Scenarios : [scenario];
            var cases = root.GetProperty("scenarios");
            RequireKeys(cases, expected);
            var results = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
            long sum = 0;
            foreach (var name in expected)
            {
                var item = cases.GetProperty(name);
                RequireKeys(item, ["assertions", "observations"]);
                var required = IdentityScenarioCatalog.RequiredObservations(name);
                if (!item.GetProperty("assertions").TryGetInt32(out var count) || count != required.Count)
                    throw InvalidResult();
                var evidence = item.GetProperty("observations");
                if (evidence.ValueKind != JsonValueKind.Array ||
                    !evidence.EnumerateArray().Select(value => value.GetString()).SequenceEqual(required))
                    throw InvalidResult();
                sum += count;
                results.Add(name, required);
            }
            if (sum != total) throw InvalidResult();
            return new(total, results.AsReadOnly());
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or
            KeyNotFoundException or FormatException or OverflowException)
        {
            throw InvalidResult();
        }
    }

    private static void RequireKeys(JsonElement element, IReadOnlyCollection<string> expected)
    {
        if (element.ValueKind != JsonValueKind.Object) throw InvalidResult();
        var actual = element.EnumerateObject().Select(value => value.Name).ToArray();
        if (actual.Length != expected.Count || !actual.ToHashSet(StringComparer.Ordinal).SetEquals(expected))
            throw InvalidResult();
    }

    private static InvalidOperationException InvalidResult() => new("identity_consumer_result_invalid");

    private static string SafeFailure(ProcessOutput output, string provider, string scenario)
    {
        if (output.ExitCode == 0 || output.Stdout.Length != 0 ||
            Encoding.UTF8.GetByteCount(output.Stderr) > 65_536) return "identity_consumer_failed";
        try
        {
            using var document = JsonDocument.Parse(output.Stderr, new()
            { AllowDuplicateProperties = false, MaxDepth = 2 });
            var value = document.RootElement;
            RequireKeys(value, ["schemaVersion", "eventCode", "attempt", "provider", "scenario", "status", "assertions"]);
            if (value.GetProperty("schemaVersion").GetInt32() == 1 &&
                value.GetProperty("attempt").GetString() is { Length: 32 } attempt &&
                attempt.All(char.IsAsciiHexDigitLower) && value.GetProperty("provider").GetString() == provider &&
                value.GetProperty("scenario").GetString() == scenario && value.GetProperty("status").GetString() == "failed" &&
                value.GetProperty("assertions").GetInt32() == 0 &&
                value.GetProperty("eventCode").GetString() is { } code && IdentityScenarioCatalog.IsSafeFailureCode(code))
                return code;
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or
            KeyNotFoundException or FormatException or OverflowException)
        { }
        return "identity_consumer_failed";
    }
}
