using System.Diagnostics;
using System.Text;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.StorageMaintenance;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Host.Features.IdentityMaintenance;

internal static class IdentityMaintenanceSupervisor
{
    internal static async Task RunAsync(StorageMaintenanceConfiguration configuration, string operation,
        IReadOnlyList<string> arguments, CancellationToken token)
    {
        var start = new ProcessStartInfo(Environment.ProcessPath ??
            throw new BootstrapException(BootstrapError.ConfigurationInvalid))
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        if (Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(typeof(BootstrapProgram).Assembly.Location);
        else
        {
            start.FileName = Path.Combine(Environment.GetEnvironmentVariable("DOTNET_ROOT") ??
                throw new BootstrapException(BootstrapError.ConfigurationInvalid), "dotnet");
            start.ArgumentList.Add(typeof(BootstrapProgram).Assembly.Location);
        }
        start.ArgumentList.Add("--identity-worker");
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        configuration.ConfigureChild(start);
        start.Environment["KOFTE_HOST_URL"] = configuration.Runtime.HostUrl.AbsoluteUri;
        start.Environment["KOFTE_WEB_URL"] = configuration.Runtime.WebUrl.AbsoluteUri;
        start.Environment["KOFTE_BACKEND_ORIGIN"] = configuration.Runtime.BackendOrigin.AbsoluteUri;
        if (configuration.Runtime.UnsafeExposure) start.Environment["KOFTE_ALLOW_UNSAFE_EXPOSURE"] = "true";
        var provider = configuration.Runtime.Profile == "local-sqlite" ? "sqlite" : "postgresql";
        IReadOnlyList<IdentityDiagnosticRecord> records = [];
        Exception? failure = null;
        var exitVerified = false;
        try
        {
            await StorageMaintenanceProcess.RunAsync(start, token, async (reader, cancellation) =>
            {
                records = await ReadAsync(reader, operation, provider, cancellation);
            }, _ => exitVerified = true);
        }
        catch (Exception error) { failure = error; }
        var terminal = records.LastOrDefault(record => record.Outcome != "warning");
        if (failure is null && terminal?.Outcome != "ready")
            failure = new StorageException(StorageCode.Unavailable);
        foreach (var warning in records.Where(record => record.Outcome == "warning"))
            IdentityDiagnostics.Write(warning);
        if (failure is null)
            Console.WriteLine(Encoding.UTF8.GetString(IdentityDiagnostics.Serialize(terminal!)));
        else
        {
            if (!exitVerified || terminal is null || terminal.Outcome == "ready")
                terminal = Failure(terminal, operation, provider, uncertain: true);
            IdentityDiagnostics.Write(terminal);
            throw failure;
        }
    }

    internal static async Task<IReadOnlyList<IdentityDiagnosticRecord>> ReadAsync(TextReader reader,
        string operation, string provider, CancellationToken token)
    {
        var records = new List<IdentityDiagnosticRecord>();
        var buffer = new char[1024];
        var line = new StringBuilder(1024);
        var terminal = false;
        while (true)
        {
            var count = await reader.ReadAsync(buffer.AsMemory(), token);
            if (count == 0) break;
            for (var index = 0; index < count; index++)
            {
                if (buffer[index] != '\n')
                {
                    if (line.Length >= 1024) throw IdentityDiagnostics.Invalid();
                    line.Append(buffer[index]);
                    continue;
                }
                var record = IdentityDiagnostics.Parse(Encoding.UTF8.GetBytes(line.ToString().TrimEnd('\r')));
                if (terminal || records.Count >= 9 || record.Outcome == "warning" && records.Count >= 8 ||
                    record.Provider != provider ||
                    record.Mode is not ("oidc" or "development") ||
                    record.Operation != operation.Replace("-", "_", StringComparison.Ordinal) ||
                    records.Count > 0 && (record.CorrelationId != records[0].CorrelationId ||
                        record.Mode != records[0].Mode))
                    throw IdentityDiagnostics.Invalid();
                terminal = record.Outcome != "warning";
                records.Add(record);
                line.Clear();
            }
        }
        if (line.Length != 0) throw IdentityDiagnostics.Invalid();
        return records.AsReadOnly();
    }

    internal static IdentityDiagnosticRecord Failure(IdentityDiagnosticRecord? receipt, string operation,
        string provider, bool uncertain)
    {
        var (code, outcome, guidance) = IdentityDiagnostics.Rule(uncertain
            ? IdentityDiagnosticOutcome.OutcomeUncertain : IdentityDiagnosticOutcome.Unavailable);
        return new(1, code, "identity", operation.Replace("-", "_", StringComparison.Ordinal),
            receipt?.Mode ?? "unknown", provider, outcome, receipt?.CorrelationId ?? Guid.NewGuid().ToString("N"),
            guidance, uncertain ? "uncertain" : receipt?.Durability ?? "unchanged");
    }
}
