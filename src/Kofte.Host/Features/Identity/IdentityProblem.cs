using Kofte.Infrastructure.Identity;

namespace Kofte.Host.Features.Identity;

public enum IdentityFailure
{
    RequestInvalid, Unauthenticated, Forbidden, EnrollmentUnavailable,
    AdmissionExceeded, Unavailable, OutcomeUncertain
}

public static class IdentityProblem
{
    public static void Headers(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
        response.Headers["Referrer-Policy"] = "no-referrer";
    }

    public static Task WriteAsync(HttpContext context, IdentityFailure failure)
    {
        var (status, code, title) = failure switch
        {
            IdentityFailure.RequestInvalid => (400, "identity_request_invalid", "Invalid identity request"),
            IdentityFailure.Unauthenticated => (401, "identity_unauthenticated", "Authentication required"),
            IdentityFailure.Forbidden => (403, "identity_forbidden", "Identity request forbidden"),
            IdentityFailure.EnrollmentUnavailable => (409, "identity_enrollment_unavailable", "Enrollment unavailable"),
            IdentityFailure.AdmissionExceeded => (429, "identity_admission_exceeded", "Identity admission exceeded"),
            IdentityFailure.Unavailable => (503, "identity_unavailable", "Identity unavailable"),
            IdentityFailure.OutcomeUncertain => (503, "identity_outcome_uncertain", "Identity outcome uncertain"),
            _ => throw new ArgumentOutOfRangeException(nameof(failure))
        };
        var observation = Kofte.Infrastructure.Identity.IdentityDiagnostics.Current;
        observation?.Select(failure switch
        {
            IdentityFailure.RequestInvalid => IdentityDiagnosticOutcome.RequestInvalid,
            IdentityFailure.Unauthenticated => IdentityDiagnosticOutcome.Unauthenticated,
            IdentityFailure.Forbidden => IdentityDiagnosticOutcome.Forbidden,
            IdentityFailure.EnrollmentUnavailable => IdentityDiagnosticOutcome.EnrollmentUnavailable,
            IdentityFailure.AdmissionExceeded => IdentityDiagnosticOutcome.AdmissionExceeded,
            IdentityFailure.OutcomeUncertain => IdentityDiagnosticOutcome.OutcomeUncertain,
            _ => IdentityDiagnosticOutcome.Unavailable
        });
        Headers(context.Response);
        return Results.Json(new
        {
            type = "urn:kofte:problem:" + code,
            title,
            status,
            code,
            correlationId = observation?.CorrelationId ?? Guid.NewGuid().ToString("N")
        }, statusCode: status, contentType: "application/problem+json").ExecuteAsync(context);
    }
}
