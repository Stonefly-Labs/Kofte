using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.Consumer.Scenarios;
using Kofte.TestSupport;

namespace Kofte.Storage.Consumer;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
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
                var installation = provider == "sqlite" ? StorageInstallation.Sqlite(root!) :
                    StorageInstallation.PostgreSql(runtime!,
                        Environment.GetEnvironmentVariable("KOFTE_POSTGRES_PROVISIONING_CONNECTION"));
                var maintenance = new StorageMaintenance(installation);
                await using var scope = await maintenance.AcquireAsync(deadline.Token);
                await maintenance.UpgradeAsync(scope, deadline.Token);
                var selected = scenario == "all" ? new[]
                {
                    "clean-install", "no-storage", "same-id-retry", "collision", "path-rejection",
                    "interrupted-provisioning", "corrected-retry", "isolated-upgrade", "stopped-restore"
                } : [scenario];
                var assertions = 0;
                foreach (var item in selected)
                    assertions += item switch
                    {
                        "no-storage" or "same-id-retry" or "corrected-retry" =>
                            await RegistrationScenarios.RunAsync(installation, scope, item, deadline.Token),
                        "collision" => await CollisionScenario.RunAsync(installation, scope, deadline.Token),
                        "clean-install" or "isolated-upgrade" or "path-rejection" =>
                            await ModuleScenarios.RunAsync(installation, scope, item, deadline.Token),
                        _ => throw new ScenarioException("storage_scenario_unimplemented")
                    };
                ValidateDiagnostics(captured.ToString(), installation.Provider);
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
                    assertions
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

    private static void ValidateDiagnostics(string captured, string provider)
    {
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
        }
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
