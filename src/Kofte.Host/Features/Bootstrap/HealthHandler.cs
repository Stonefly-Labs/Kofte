using System.Diagnostics;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.ModuleComposition;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Kofte.Host.Features.Bootstrap;

public sealed record BootstrapStatus(string Version, string State, string Component, string CorrelationId);

public sealed class HealthHandler(IPlatformStore store, CompositionHealth? composition = null)
{
    public static BootstrapStatus Live() => new("1", "live", "host", BootstrapDiagnostics.CorrelationId());

    public async Task<BootstrapStatus> ReadyAsync(CancellationToken token)
    {
        var parent = Activity.Current;
        var context = parent is { IdFormat: ActivityIdFormat.W3C }
            ? new ActivityContext(parent.TraceId, parent.SpanId, parent.ActivityTraceFlags)
            : default;
        using var activity = BootstrapTelemetry.Activities.StartActivity(
            "bootstrap.readiness", ActivityKind.Internal, context);
        activity?.SetTag("component", "storage");
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromSeconds(3));
        try
        {
            await Task.Run(() => store.ProbeAsync(budget.Token), budget.Token).WaitAsync(budget.Token);
            if (composition is not null && !composition.IsReady)
            {
                activity?.SetTag("component", "host");
                activity?.SetTag("event.code", "composition_unavailable");
                activity?.SetStatus(ActivityStatusCode.Error);
                return new("1", "unavailable", "host", BootstrapDiagnostics.CorrelationId());
            }
            activity?.SetTag("event.code", "storage_ready");
            return new("1", "ready", "storage", BootstrapDiagnostics.CorrelationId());
        }
        catch (Exception error) when (error is BootstrapException or SqliteException or NpgsqlException or
            IOException or UnauthorizedAccessException || error is OperationCanceledException && !token.IsCancellationRequested)
        {
            activity?.SetTag("event.code", "storage_unavailable");
            activity?.SetStatus(ActivityStatusCode.Error);
            BootstrapDiagnostics.Failure(BootstrapError.StorageUnavailable);
            return new("1", "unavailable", "storage", BootstrapDiagnostics.CorrelationId());
        }
    }
}
