using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.StorageMaintenance;
using Kofte.Infrastructure.Storage;

namespace Kofte.Host.Features.IdentityMaintenance;

internal static class IdentityMaintenanceSupervisor
{
    internal static async Task RunAsync(StorageMaintenanceConfiguration configuration, string operation,
        CancellationToken token)
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
        start.ArgumentList.Add(operation);
        configuration.ConfigureChild(start);
        start.Environment["KOFTE_HOST_URL"] = configuration.Runtime.HostUrl.AbsoluteUri;
        start.Environment["KOFTE_WEB_URL"] = configuration.Runtime.WebUrl.AbsoluteUri;
        start.Environment["KOFTE_BACKEND_ORIGIN"] = configuration.Runtime.BackendOrigin.AbsoluteUri;
        if (configuration.Runtime.UnsafeExposure) start.Environment["KOFTE_ALLOW_UNSAFE_EXPOSURE"] = "true";
        string? receipt = null;
        await StorageMaintenanceProcess.RunAsync(start, token, async (reader, cancellation) =>
        {
            var buffer = new char[1025];
            var length = 0;
            while (true)
            {
                var count = await reader.ReadAsync(buffer.AsMemory(length), cancellation);
                if (count == 0) break;
                length += count;
                if (length == buffer.Length) throw new IOException("identity_worker_output_invalid");
            }
            if (length == 0) return;
            var text = new string(buffer, 0, length);
            if (Encoding.UTF8.GetByteCount(text) > 1024) throw new IOException("identity_worker_output_invalid");
            using var json = JsonDocument.Parse(text, new() { AllowDuplicateProperties = false, MaxDepth = 2 });
            var value = json.RootElement;
            if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Count() != 4 ||
                value.GetProperty("eventCode").GetString() != "identity_operator_completed" ||
                value.GetProperty("operation").GetString() != operation ||
                value.GetProperty("durability").GetString() is not ("committed" or "unchanged") ||
                value.GetProperty("correlation").GetString() is not { Length: 32 } correlation ||
                !correlation.All(char.IsAsciiHexDigitLower))
                throw new IOException("identity_worker_output_invalid");
            receipt = JsonSerializer.Serialize(value);
        });
        Console.WriteLine(receipt ?? throw new IOException("identity_worker_output_invalid"));
    }
}
