using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Kofte.Host.Bootstrap;

public static class BootstrapTelemetry
{
    public static readonly ActivitySource Activities = new("Kofte.Bootstrap");
    private static readonly Meter Meter = new("Kofte.Bootstrap");
    public static readonly Counter<long> Failures = Meter.CreateCounter<long>("kofte.bootstrap.failures");

    public static void Add(IServiceCollection services, BootstrapConfiguration config)
    {
        services.AddOpenTelemetry().ConfigureResource(resource => resource.AddService("kofte-bootstrap"))
            .WithTracing(tracing =>
            {
                tracing.AddSource("Kofte.Bootstrap");
                if (config.ExportEndpoint is not null)
                    tracing.AddOtlpExporter(options => options.Endpoint = config.ExportEndpoint);
            })
            .WithMetrics(metrics =>
            {
                metrics.AddMeter("Kofte.Bootstrap");
                if (config.ExportEndpoint is not null)
                    metrics.AddOtlpExporter(options => options.Endpoint = config.ExportEndpoint);
            });
    }
}
