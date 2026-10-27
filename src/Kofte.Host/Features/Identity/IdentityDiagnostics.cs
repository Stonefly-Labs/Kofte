using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Identity;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;
using Npgsql;
using OwnerDiagnostics = Kofte.Infrastructure.Identity.IdentityDiagnostics;

namespace Kofte.Host.Features.Identity;

internal sealed record IdentityDiagnosticContext(string Provider, Action<IdentityDiagnosticRecord> Sink);

internal sealed class IdentityRequestDiagnostics(RequestDelegate next, IdentityConfiguration configuration,
    IdentityDiagnosticContext diagnostics)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (path is "/health/live" or "/health/ready" or "/openapi/v1.json")
        {
            await next(context);
            return;
        }
        using var observation = OwnerDiagnostics.Begin(Action(path),
            configuration.Mode == IdentityMode.Oidc ? "oidc" : "development", diagnostics.Provider,
            TimeSpan.FromSeconds(path == "/auth/oidc/callback" ? 4 : 1), context.RequestAborted, diagnostics.Sink);
        var originalAbort = context.RequestAborted;
        context.RequestAborted = observation.Budget.Token;
        context.Response.OnStarting(() =>
        {
            Publish();
            return Task.CompletedTask;
        });
        try { await next(context); }
        catch (Exception error) when (!context.Response.HasStarted && error is StorageException or
            IdentityStateException or IdentityEventCapacityException or SqliteException or NpgsqlException or
            IOException or UnauthorizedAccessException or OperationCanceledException or InvalidOperationException)
        {
            observation.Fail(error);
            await IdentityProblem.WriteAsync(context, observation.Durability == StorageDurability.Uncertain
                ? IdentityFailure.OutcomeUncertain : IdentityFailure.Unavailable);
        }
        catch (Exception error)
        {
            observation.Fail(error);
            throw;
        }
        finally
        {
            context.RequestAborted = originalAbort;
            Publish();
        }

        void Publish() => observation.Publish(observation.Outcome != IdentityDiagnosticOutcome.Ready
            ? observation.Outcome : context.Response.StatusCode switch
            {
                >= 200 and < 400 => IdentityDiagnosticOutcome.Ready,
                401 => IdentityDiagnosticOutcome.Unauthenticated,
                403 => IdentityDiagnosticOutcome.Forbidden,
                409 => IdentityDiagnosticOutcome.EnrollmentUnavailable,
                429 => IdentityDiagnosticOutcome.AdmissionExceeded,
                >= 500 => IdentityDiagnosticOutcome.Unavailable,
                _ => IdentityDiagnosticOutcome.RequestInvalid
            });
    }

    private static IdentityAction Action(string? path) => path switch
    {
        "/api/identity/v1/browser-context" => IdentityAction.BrowserContext,
        "/auth/login" => IdentityAction.Login,
        "/auth/oidc/callback" => IdentityAction.Callback,
        "/api/identity/v1/me" => IdentityAction.Current,
        "/api/identity/v1/sign-out" => IdentityAction.SignOut,
        "/api/identity/v1/first-administrator" => IdentityAction.Enrollment,
        "/api/identity/v1/development-session" => IdentityAction.DevelopmentSession,
        _ => IdentityAction.Access
    };
}
