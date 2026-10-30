using Kofte.Host.Features.Identity.Login;
using Kofte.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using OwnerDiagnostics = Kofte.Infrastructure.Identity.IdentityDiagnostics;

namespace Kofte.Host.Features.Identity.SignOut;

internal static class SignOutEndpoints
{
    internal static void Map(WebApplication app) =>
        app.MapPost("/api/identity/v1/sign-out", SignOutAsync)
            .WithName("identitySignOut").Produces(StatusCodes.Status204NoContent)
            .RequireAuthorization(IdentityRegistration.CurrentIdentityPolicy);

    private static async Task SignOutAsync(HttpContext context, IdentityStore store, TimeProvider clock,
        IOptionsMonitor<CookieAuthenticationOptions> options)
    {
        var current = IdentityTicketStore.Current(context) ??
            throw new InvalidOperationException("identity_verified_session_missing");
        var cookie = options.Get(OidcConfiguration.SessionScheme);
        bool revoked;
        try
        {
            await IdentityProtocolAdmission.AdmitAsync(context, current.Session.Digest);
            var budget = OwnerDiagnostics.Current?.Budget ??
                throw new InvalidOperationException("identity_request_budget_missing");
            revoked = await store.MutateAsync(budget, change =>
                IdentitySessionStore.RevokeAsync(change, current.Session.Digest, clock.GetUtcNow().UtcDateTime),
                IdentityCommitKind.SignOut);
        }
        finally
        {
            // Native SignOutAsync would invoke ITicketStore.RemoveAsync and mutate the owner a second time.
            cookie.CookieManager.DeleteCookie(context,
                cookie.Cookie.Name ?? throw new InvalidOperationException("identity_session_cookie_missing"),
                cookie.Cookie.Build(context));
        }
        if (revoked) context.Response.StatusCode = StatusCodes.Status204NoContent;
        else await IdentityProblem.WriteAsync(context, IdentityFailure.Unauthenticated);
    }
}
