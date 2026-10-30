using Kofte.Host.Features.Identity.Login;
using Kofte.Infrastructure.Identity;
using Kofte.Platform.Identity;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Kofte.Host.Features.Identity.CurrentUser;

internal sealed record CurrentIdentityV1(string Version, Guid UserId, string? DisplayName,
    string IdentityMode, bool IsInitialAdministrator);

internal static class CurrentUserEndpoints
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.Default
    };

    internal static void Map(WebApplication app) =>
        app.MapGet("/api/identity/v1/me", CurrentAsync)
            .WithName("identityCurrent").Produces<CurrentIdentityV1>()
            .RequireAuthorization(IdentityRegistration.CurrentIdentityPolicy);

    private static async Task<IResult> CurrentAsync(HttpContext context, ICurrentActorV1 current)
    {
        var view = IdentityTicketStore.Current(context) ??
            throw new InvalidOperationException("identity_verified_session_missing");
        if (current.Actor?.UserId != view.User.UserId)
            throw new InvalidOperationException("identity_verified_actor_missing");
        await IdentityProtocolAdmission.AdmitAsync(context, view.Session.Digest);
        return CreateResponse(view.User, view.IsInitialAdministrator);
    }

    internal static IResult CreateResponse(IdentityUser user, bool designated) =>
        Results.Json(new CurrentIdentityV1("1", user.UserId, user.DisplayName, user.Mode, designated), Json);

    internal static void ProjectActor(HttpContext context)
    {
        var current = context.RequestServices.GetRequiredService<IdentityRegistration.CurrentActor>();
        current.Actor = null;
        if (IdentityTicketStore.Current(context) is not { } view) return;
        if (context.User.Identity?.IsAuthenticated != true)
            throw new InvalidOperationException("identity_verified_principal_missing");
        current.Actor = new(view.User.UserId,
            view.User.Mode == "oidc" ? IdentityMode.Oidc : IdentityMode.Development, view.IsInitialAdministrator);
    }
}
