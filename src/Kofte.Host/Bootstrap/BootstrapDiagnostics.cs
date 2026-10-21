using System.Diagnostics;
using System.Text.Json;

namespace Kofte.Host.Bootstrap;

public enum BootstrapError
{
    ConfigurationInvalid, StorageMissing, StorageInvalid, StorageUnavailable,
    StoreBusy, ConfirmationRequired, DestinationExists, CommandInvalid
}

public sealed class BootstrapException(BootstrapError code) : Exception(BootstrapDiagnostics.Code(code))
{
    public BootstrapError Code { get; } = code;
}

public static class BootstrapDiagnostics
{
    public static string CorrelationId() =>
        Activity.Current is { IdFormat: ActivityIdFormat.W3C } activity && activity.TraceId != default
            ? activity.TraceId.ToHexString() : ActivityTraceId.CreateRandom().ToHexString();

    public static string Code(BootstrapError code) => code switch
    {
        BootstrapError.ConfigurationInvalid => "configuration_invalid",
        BootstrapError.StorageMissing => "storage_missing",
        BootstrapError.StorageInvalid => "storage_invalid",
        BootstrapError.StorageUnavailable => "storage_unavailable",
        BootstrapError.StoreBusy => "store_busy",
        BootstrapError.ConfirmationRequired => "confirmation_required",
        BootstrapError.DestinationExists => "destination_exists",
        _ => "command_invalid"
    };

    public static void Failure(BootstrapError error)
    {
        BootstrapTelemetry.Failures.Add(1,
            new KeyValuePair<string, object?>("component", "host"),
            new KeyValuePair<string, object?>("event.code", Code(error)));
        Console.Error.WriteLine(JsonSerializer.Serialize(new
        {
            level = "error",
            eventCode = Code(error),
            component = "host",
            correlationId = CorrelationId()
        }));
    }
}
