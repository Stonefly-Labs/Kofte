using System.Text.Json;
using Kofte.Host.Features.Identity.Login;
using Kofte.Infrastructure.Identity;
using OwnerDiagnostics = Kofte.Infrastructure.Identity.IdentityDiagnostics;

namespace Kofte.Host.Features.Identity.Enrollment;

internal sealed record EnrollmentResultV1(string Version, string Status, string CorrelationId);

internal static class EnrollmentEndpoints
{
    internal static void Map(WebApplication app) =>
        app.MapPost("/api/identity/v1/first-administrator", EnrollAsync)
            .WithName("identityEnrollFirstAdministrator").Produces<EnrollmentResultV1>()
            .RequireAuthorization(IdentityRegistration.CurrentIdentityPolicy);

    private static async Task EnrollAsync(HttpContext context, IdentityStore store, TimeProvider clock)
    {
        var current = IdentityTicketStore.Current(context) ??
            throw new InvalidOperationException("identity_verified_session_missing");
        await IdentityProtocolAdmission.AdmitAsync(context, current.Session.Digest,
            IdentityAdmissionCategory.EnrollmentInstallation, IdentityAdmissionCategory.EnrollmentSession);
        string authorization;
        try
        {
            if (!context.Request.HasJsonContentType()) throw new JsonException();
            using var document = await JsonDocument.ParseAsync(context.Request.Body,
                new() { AllowDuplicateProperties = false, MaxDepth = 2 }, context.RequestAborted);
            var value = document.RootElement;
            if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Count() != 1 ||
                !value.TryGetProperty("authorization", out var field) || field.ValueKind != JsonValueKind.String ||
                field.GetString() is not { } secret || !IdentityBootstrapStore.ValidAuthorization(secret))
                throw new JsonException();
            authorization = secret;
        }
        catch (JsonException)
        {
            await IdentityProblem.WriteAsync(context, IdentityFailure.RequestInvalid);
            return;
        }
        try
        {
            var budget = OwnerDiagnostics.Current?.Budget ??
                throw new InvalidOperationException("identity_request_budget_missing");
            var receipt = await store.MutateAsync(budget, change => IdentityBootstrapStore.EnrollAsync(change,
                current, authorization, clock.GetUtcNow().UtcDateTime), IdentityCommitKind.Enrollment);
            await Results.Json(new EnrollmentResultV1("1", "enrolled", receipt.Correlation)).ExecuteAsync(context);
        }
        catch (IdentityStateException error) when (error.Outcome is IdentityDiagnosticOutcome.Unauthenticated or
            IdentityDiagnosticOutcome.Forbidden or IdentityDiagnosticOutcome.EnrollmentUnavailable)
        {
            await IdentityProblem.WriteAsync(context, error.Outcome switch
            {
                IdentityDiagnosticOutcome.Unauthenticated => IdentityFailure.Unauthenticated,
                IdentityDiagnosticOutcome.Forbidden => IdentityFailure.Forbidden,
                _ => IdentityFailure.EnrollmentUnavailable
            });
        }
    }
}
