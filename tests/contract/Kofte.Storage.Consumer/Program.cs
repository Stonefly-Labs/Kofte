using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.Consumer.Scenarios;
using Kofte.Storage.Consumer.Modules.Alpha;
using Kofte.TestSupport;

namespace Kofte.Storage.Consumer;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.FirstOrDefault() == "--storage-crash-worker")
            return await StorageCrashWorker.RunAsync(args, AlphaStorage.Create);
        var worker = args.FirstOrDefault() == "--storage-worker";
        if (worker) args = args[1..];
        string? provider = null;
        string? scenario = null;
        var code = "storage_scenario_required";
        if (args is ["--provider", var selectedProvider, "--scenario", var selectedScenario])
        {
            try
            {
                provider = StorageScenarioCatalog.RequireProvider(selectedProvider);
                scenario = StorageScenarioCatalog.RequireScenario(selectedScenario);
            }
            catch (InvalidOperationException error) { code = error.Message; }
        }
        if (provider is not null && scenario is not null)
        {
            var originalError = Console.Error;
            using var captured = new DiagnosticCapture();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(110));
            try
            {
                Console.SetError(captured);
                var root = Environment.GetEnvironmentVariable("KOFTE_DATA_ROOT");
                var runtime = Environment.GetEnvironmentVariable("KOFTE_POSTGRES_CONNECTION");
                if (provider == "sqlite" ? string.IsNullOrEmpty(root) : string.IsNullOrEmpty(runtime))
                    throw new ScenarioException("storage_scenario_configuration_required");
                if (!worker)
                {
                    Console.SetError(originalError);
                    return await SuperviseAsync(provider, scenario, deadline.Token);
                }
                await StorageMaintenanceProcess.AdmitAsync(deadline.Token);
                var installation = provider == "sqlite" ? StorageInstallation.Sqlite(root!) :
                    StorageInstallation.PostgreSql(runtime!,
                        Environment.GetEnvironmentVariable("KOFTE_POSTGRES_PROVISIONING_CONNECTION"));
                var maintenance = new StorageMaintenance(installation);
                await using (var scope = await maintenance.AcquireAsync(deadline.Token))
                    await maintenance.UpgradeAsync(scope, deadline.Token);
                IReadOnlyList<string> selected = scenario == "all" ? StorageScenarioCatalog.Scenarios : [scenario];
                var assertions = 0;
                var completed = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var item in selected)
                {
                    var before = RegistrationScenarios.Assertions;
                    int count;
                    if (item is "interrupted-provisioning" or "stopped-restore")
                        count = await RecoveryScenarios.RunAsync(installation, item, deadline.Token);
                    else if (item == "corrected-retry")
                        count = await CorrectedRetryScenario.RunAsync(installation, deadline.Token);
                    else
                    {
                        await using var scope = await maintenance.AcquireAsync(deadline.Token);
                        count = item switch
                        {
                            "no-storage" or "same-id-retry" =>
                                await RegistrationScenarios.RunAsync(installation, scope, item, deadline.Token),
                            "collision" => await CollisionScenario.RunAsync(installation, scope, deadline.Token),
                            "clean-install" or "isolated-upgrade" or "path-rejection" =>
                                await ModuleScenarios.RunAsync(installation, scope, item, deadline.Token),
                            _ => throw new ScenarioException("storage_scenario_unimplemented")
                        };
                    }
                    if (count <= 0 || RegistrationScenarios.Assertions <= before)
                        throw new ScenarioException("storage_scenario_no_assertions");
                    count = RegistrationScenarios.Assertions - before;
                    completed.Add(item, count);
                    assertions += count;
                }
                ValidateDiagnostics(captured.ToString(), installation.Provider, scenario == "all");
                if (assertions <= 0) throw new ScenarioException("storage_scenario_no_assertions");
                Console.SetError(originalError);
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    eventCode = "storage_scenario_passed",
                    component = "storage-consumer",
                    correlationId = ActivityTraceId.CreateRandom().ToHexString(),
                    provider,
                    scenario,
                    status = "passed",
                    assertions,
                    scenarios = completed
                }));
                return 0;
            }
            catch (ScenarioException error) { code = error.Message; }
            catch (Exception) { code = "storage_scenario_failed"; }
            finally { Console.SetError(originalError); }
        }
        Console.Error.WriteLine(JsonSerializer.Serialize(new
        {
            eventCode = code,
            component = "storage-consumer",
            correlationId = ActivityTraceId.CreateRandom().ToHexString(),
            provider,
            scenario,
            status = "failed",
            assertions = 0
        }));
        return 1;
    }

    private static async Task<int> SuperviseAsync(string provider, string scenario, CancellationToken token)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = OwnedProcesses.FindRepository()
        };
        start.ArgumentList.Add(typeof(Program).Assembly.Location);
        foreach (var argument in new[] { "--storage-worker", "--provider", provider, "--scenario", scenario })
            start.ArgumentList.Add(argument);
        ConfigureChild(start);
        string? completed = null;
        await StorageMaintenanceProcess.RunAsync(start, token, async (reader, cancellation) =>
        {
            var buffer = new char[4097];
            var length = 0;
            while (length < buffer.Length)
            {
                var count = await reader.ReadAsync(buffer.AsMemory(length), cancellation);
                if (count == 0) break;
                length += count;
            }
            if (length == buffer.Length) throw new ScenarioException("storage_scenario_output_limit");
            using var document = JsonDocument.Parse(new string(buffer, 0, length));
            var result = document.RootElement;
            var names = result.EnumerateObject().Select(property => property.Name).ToArray();
            if (names.Length != 8 || names.Distinct(StringComparer.Ordinal).Count() != 8 ||
                names.Except(["eventCode", "component", "correlationId", "provider", "scenario", "status", "assertions", "scenarios"]).Any() ||
                result.GetProperty("eventCode").GetString() != "storage_scenario_passed" ||
                result.GetProperty("component").GetString() != "storage-consumer" ||
                !Regex.IsMatch(result.GetProperty("correlationId").GetString() ?? "", @"\A[0-9a-f]{32}\z") ||
                result.GetProperty("provider").GetString() != provider ||
                result.GetProperty("scenario").GetString() != scenario ||
                result.GetProperty("status").GetString() != "passed" ||
                !result.GetProperty("assertions").TryGetInt32(out var assertions) || assertions <= 0)
                throw new ScenarioException("storage_scenario_result_invalid");
            var cases = result.GetProperty("scenarios").EnumerateObject().ToArray();
            IEnumerable<string> expected = scenario == "all" ? StorageScenarioCatalog.Scenarios : [scenario];
            if (!cases.Select(item => item.Name).SequenceEqual(expected) ||
                cases.Any(item => !item.Value.TryGetInt32(out var count) || count <= 0) ||
                cases.Sum(item => item.Value.GetInt64()) != assertions)
                throw new ScenarioException("storage_scenario_result_invalid");
            completed = JsonSerializer.Serialize(new
            {
                eventCode = "storage_scenario_passed",
                component = "storage-consumer",
                correlationId = result.GetProperty("correlationId").GetString(),
                provider,
                scenario,
                status = "passed",
                assertions,
                scenarios = cases.ToDictionary(item => item.Name, item => item.Value.GetInt32(), StringComparer.Ordinal)
            });
        });
        Console.WriteLine(completed ?? throw new ScenarioException("storage_scenario_result_invalid"));
        return 0;
    }

    internal static void ConfigureChild(ProcessStartInfo start)
    {
        OwnedProcesses.ClearSensitiveEnvironment(start);
        foreach (var name in new[] { "KOFTE_PROFILE", "KOFTE_DATA_ROOT", "KOFTE_POSTGRES_CONNECTION",
            "KOFTE_POSTGRES_PROVISIONING_CONNECTION", "KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE" })
            if (Environment.GetEnvironmentVariable(name) is { } value) start.Environment[name] = value;
    }

    private static void ValidateDiagnostics(string captured, string provider, bool requireEveryOutcome)
    {
        if (string.IsNullOrWhiteSpace(captured)) throw new ScenarioException("storage_scenario_diagnostics_missing");
        var outcomes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in captured.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            using var record = JsonDocument.Parse(line);
            var value = record.RootElement;
            var names = value.EnumerateObject().Select(p => p.Name).ToArray();
            if (names.Length != 7 || names.Distinct(StringComparer.Ordinal).Count() != 7 ||
                names.Except(["eventCode", "component", "operation", "provider", "outcome", "moduleId", "correlationId"]).Any() ||
                value.GetProperty("component").GetString() != "storage" ||
                value.GetProperty("provider").GetString() != provider ||
                !Regex.IsMatch(value.GetProperty("correlationId").GetString() ?? "", @"\A[0-9a-f]{32}\z") ||
                value.GetProperty("operation").GetString() is not ("resolve" or "register" or "migrate" or "upgrade" or "recover" or "acquire") ||
                value.GetProperty("outcome").GetString() is not ("ready" or "nostorage" or "busy" or
                    "rejected" or "incomplete" or "reconciliationrequired" or "failed") ||
                value.GetProperty("eventCode").GetString() is not ("storage_ready" or "storage_no_storage" or
                    "storage_busy" or "storage_conflict" or "storage_target_reserved" or "storage_path_rejected" or
                    "storage_owner_mismatch" or "storage_missing" or "storage_corrupt" or "storage_incomplete" or
                    "storage_reconciliation_required" or "storage_version_unsupported" or "storage_privilege_denied" or
                    "storage_unavailable" or "storage_deadline_exceeded" or "storage_cancelled" or "storage_request_invalid") ||
                value.GetProperty("moduleId").ValueKind != JsonValueKind.Null &&
                    !ModuleId.TryParse(value.GetProperty("moduleId").GetString(), out _))
                throw new ScenarioException("storage_scenario_diagnostic_invalid");
            outcomes.Add(value.GetProperty("outcome").GetString()!);
        }
        if (requireEveryOutcome && !outcomes.SetEquals(Enum.GetValues<StorageOutcome>()
            .Select(outcome => outcome.ToString().ToLowerInvariant())))
            throw new ScenarioException("storage_scenario_outcome_missing");
    }

    private sealed class ScenarioException(string code) : Exception(code);

    private sealed class DiagnosticCapture : TextWriter
    {
        private readonly StringBuilder buffer = new();
        public override Encoding Encoding => Encoding.UTF8;
        public override void Write(char value)
        {
            if (buffer.Length >= 1_000_000) throw new ScenarioException("storage_scenario_output_limit");
            buffer.Append(value);
        }
        public override string ToString() => buffer.ToString();
    }
}
