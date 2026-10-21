using Kofte.Host.Bootstrap;

namespace Kofte.Host.Features.Bootstrap;

public static class HealthEndpoints
{
    public static void MapBootstrapHealth(this WebApplication app)
    {
        app.UseExceptionHandler(handler => handler.Run(context =>
        {
            BootstrapDiagnostics.Failure(BootstrapError.StorageUnavailable);
            context.Response.StatusCode = 500;
            context.Response.Headers.CacheControl = "no-store";
            return Task.CompletedTask;
        }));
        app.Use(async (context, next) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            var path = context.Request.Path.Value;
            if (path is not ("/health/live" or "/health/ready" or "/openapi/v1.json") ||
                path == "/openapi/v1.json" && !app.Environment.IsDevelopment())
            {
                context.Response.StatusCode = 404;
                return;
            }
            if (context.Request.Method != HttpMethods.Get)
            {
                context.Response.StatusCode = 405;
                context.Response.Headers.Allow = "GET";
                return;
            }
            if (context.Request.QueryString.HasValue || context.Request.ContentLength is > 0 ||
                context.Request.Headers.ContainsKey("Transfer-Encoding"))
            {
                context.Response.StatusCode = 400;
                return;
            }
            await next(context);
        });
        app.MapGet("/health/live", HealthHandler.Live)
            .WithName("bootstrapLiveness").AllowAnonymous()
            .WithSummary("Observe process liveness without checking storage")
            .Produces<BootstrapStatus>().Produces(400).Produces(405);
        app.MapGet("/health/ready", async (HealthHandler handler, IHostApplicationLifetime lifetime,
            CancellationToken token) =>
        {
            BootstrapStatus result;
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
            budget.CancelAfter(TimeSpan.FromSeconds(5));
            if (!lifetime.ApplicationStarted.IsCancellationRequested)
                result = new("1", "unavailable", "host", BootstrapDiagnostics.CorrelationId());
            else result = await handler.ReadyAsync(budget.Token);
            return Results.Json(result, statusCode: result.State == "ready" ? 200 : 503);
        }).WithName("bootstrapReadiness").AllowAnonymous()
            .WithSummary("Observe current selected-provider readiness without caching")
            .Produces<BootstrapStatus>().Produces<BootstrapStatus>(503).Produces(400).Produces(405);
    }
}
