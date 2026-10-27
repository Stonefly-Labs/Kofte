using Kofte.Platform.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kofte.Host.Features.Identity;

public static class IdentityRegistration
{
    public const string CurrentIdentityPolicy = "kofte.identity.self.v1";

    public static IServiceCollection AddIdentityBoundary(this IServiceCollection services,
        IdentityConfiguration configuration)
    {
        services.AddSingleton(configuration);
        services.TryAddSingleton(new IdentityDiagnosticContext("unknown",
            Kofte.Infrastructure.Identity.IdentityDiagnostics.Write));
        services.AddScoped<CurrentActor>();
        services.AddScoped<ICurrentActorV1>(provider => provider.GetRequiredService<CurrentActor>());
        services.AddAuthentication();
        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
                .RequireAssertion(_ => false).Build();
            options.FallbackPolicy = options.DefaultPolicy;
            options.AddPolicy(CurrentIdentityPolicy, policy => policy.RequireAuthenticatedUser()
                .RequireAssertion(context => context.Resource is HttpContext request &&
                    request.RequestServices.GetRequiredService<ICurrentActorV1>().Actor is not null));
        });
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, IdentityAuthorizationResult>();
        services.AddAntiforgery(options =>
        {
            var oidc = configuration.Mode == IdentityMode.Oidc;
            options.HeaderName = "X-Kofte-CSRF";
            options.FormFieldName = "__RequestVerificationToken";
            options.Cookie = new FixedOriginAntiforgeryCookie(oidc)
            {
                Name = oidc ? "__Host-kofte-antiforgery" : "kofte-dev-antiforgery",
                Path = "/",
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            };
        });
        return services;
    }

    public static void UseIdentityBoundary(this WebApplication app)
    {
        app.UseRouting();
        app.UseMiddleware<IdentityRequestDiagnostics>();
        app.UseMiddleware<IdentityRequestBoundary>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.Use(async (context, next) =>
        {
            if (context.Request.Method == HttpMethods.Post &&
                !await IdentityRequestBoundary.ValidateMutationAsync(context)) return;
            await next(context);
        });
    }

    internal sealed class CurrentActor : ICurrentActorV1
    {
        public AuthenticatedActorV1? Actor { get; internal set; }
    }

    private sealed class IdentityAuthorizationResult : IAuthorizationMiddlewareResultHandler
    {
        public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy,
            PolicyAuthorizationResult result) => result.Succeeded ? next(context) :
                IdentityProblem.WriteAsync(context, result.Forbidden
                    ? IdentityFailure.Forbidden : IdentityFailure.Unauthenticated);
    }

    private sealed class FixedOriginAntiforgeryCookie(bool secure) : CookieBuilder
    {
        public override CookieOptions Build(HttpContext context, DateTimeOffset expiresFrom)
        {
            var options = base.Build(context, expiresFrom);
            // TLS terminates at the fixed configured ingress, not a trusted forwarded scheme.
            options.Secure = secure;
            return options;
        }
    }
}
