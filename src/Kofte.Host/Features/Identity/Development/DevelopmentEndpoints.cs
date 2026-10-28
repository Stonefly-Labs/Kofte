using System.Text.Json;
using Kofte.Host.Features.Identity.CurrentUser;
using Kofte.Host.Features.Identity.Login;
using Kofte.Platform.Identity;

namespace Kofte.Host.Features.Identity.Development;

internal static class DevelopmentEndpoints
{
    internal static void Map(WebApplication app)
    {
        if (app.Services.GetRequiredService<IdentityConfiguration>().Mode != IdentityMode.Development) return;
        app.MapPost("/api/identity/v1/development-session", SelectAsync)
            .WithName("identityDevelopmentSession").Produces<CurrentIdentityV1>().AllowAnonymous();
    }

    private static async Task<IResult> SelectAsync(HttpContext context, IdentityConfiguration configuration,
        IdentityBrowserBinding binding, ValidatedLogin login)
    {
        string identityKey;
        try
        {
            if (!context.Request.HasJsonContentType()) throw new JsonException();
            using var document = await JsonDocument.ParseAsync(context.Request.Body,
                new() { AllowDuplicateProperties = false, MaxDepth = 2 }, context.RequestAborted);
            var value = document.RootElement;
            if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Count() != 1 ||
                !value.TryGetProperty("identityKey", out var field) || field.ValueKind != JsonValueKind.String ||
                field.GetString() is not { } key || !configuration.DevelopmentIdentities.ContainsKey(key))
                throw new JsonException();
            identityKey = key;
        }
        catch (JsonException)
        {
            await IdentityProblem.WriteAsync(context, IdentityFailure.RequestInvalid);
            return Results.Empty;
        }
        await IdentityProtocolAdmission.AdmitAsync(context, binding.Require(context),
            Kofte.Infrastructure.Identity.IdentityAdmissionCategory.LoginInstallation,
            Kofte.Infrastructure.Identity.IdentityAdmissionCategory.LoginBrowser);
        var budget = Kofte.Infrastructure.Identity.IdentityDiagnostics.Current?.Budget ??
            throw new InvalidOperationException("identity_request_budget_missing");
        var user = await login.CompleteDevelopmentAsync(context, identityKey, budget);
        return CurrentUserEndpoints.CreateResponse(user, designated: false);
    }
}
