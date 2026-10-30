using Kofte.Infrastructure.Identity;
using Kofte.Platform.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;

namespace Kofte.Host.Features.Identity.Login;

internal sealed record BrowserContextV1(string Version, string IdentityMode, string CsrfToken);

internal static class LoginEndpoints
{
    internal static void Map(WebApplication app)
    {
        app.MapGet("/api/identity/v1/browser-context", BrowserContextAsync)
            .WithName("identityBrowserContext").Produces<BrowserContextV1>().AllowAnonymous();
        if (app.Services.GetRequiredService<IdentityConfiguration>().Mode != IdentityMode.Oidc) return;
        app.MapPost("/auth/login", LoginAsync).WithName("identityLogin").AllowAnonymous();
        app.MapGet("/auth/oidc/callback", (HttpContext context) =>
            IdentityProblem.WriteAsync(context, IdentityFailure.Unauthenticated))
            .WithName("identityCallback").AllowAnonymous();
    }

    private static async Task BrowserContextAsync(HttpContext context, IdentityBrowserBinding binding,
        IdentityConfiguration configuration, IAntiforgery antiforgery)
    {
        var browser = binding.GetOrCreate(context);
        await IdentityProtocolAdmission.AdmitAsync(context, IdentityTicketStore.Current(context)?.Session.Digest ?? browser);
        var tokens = antiforgery.GetAndStoreTokens(context);
        await context.Response.WriteAsJsonAsync(new BrowserContextV1("1",
            configuration.Mode == IdentityMode.Oidc ? "oidc" : "development",
            tokens.RequestToken ?? throw new InvalidOperationException("identity_csrf_missing")), context.RequestAborted);
    }

    private static async Task LoginAsync(HttpContext context, IdentityBrowserBinding binding,
        IdentityStore store, TimeProvider clock)
    {
        if (!context.Request.HasFormContentType ||
            context.Request.ContentType?.Split(';')[0].Trim() != "application/x-www-form-urlencoded")
        {
            await IdentityProblem.WriteAsync(context, IdentityFailure.RequestInvalid);
            return;
        }
        var fields = await context.Request.ReadFormAsync(context.RequestAborted);
        var path = fields.TryGetValue("returnPath", out var value) ? value.ToString() : "/identity";
        if (fields.Files.Count != 0 || fields.Any(field =>
            field.Key is not ("returnPath" or "__RequestVerificationToken") || field.Value.Count != 1) ||
            path is not ("/" or "/identity"))
        {
            await IdentityProblem.WriteAsync(context, IdentityFailure.RequestInvalid);
            return;
        }
        var browser = binding.Require(context);
        await IdentityProtocolAdmission.AdmitAsync(context, browser,
            IdentityAdmissionCategory.LoginInstallation, IdentityAdmissionCategory.LoginBrowser);
        var budget = Kofte.Infrastructure.Identity.IdentityDiagnostics.Current?.Budget ??
            throw new InvalidOperationException("identity_request_budget_missing");
        var attempt = await store.MutateAsync(budget, change =>
            IdentityLoginAttempts.StartAsync(change, browser, path, clock.GetUtcNow().UtcDateTime));
        await context.ChallengeAsync(OidcConfiguration.ProviderScheme, LoginAttemptBinding.Create(attempt));
        context.Response.StatusCode = 303;
        context.Response.ContentType = "text/plain; charset=utf-8";
    }
}
