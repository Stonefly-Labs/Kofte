using System.Security.Cryptography;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kofte.Host.Features.Identity.Login;

internal static class IdentityLoginRegistration
{
    internal static void Add(IServiceCollection services, IdentityConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IdentityAdmission>();
        services.AddSingleton<IdentityBrowserBinding>();
        services.AddSingleton<IdentityTicketStore>();
        services.AddScoped<ValidatedLogin>();
        var oidc = configuration.Mode == IdentityMode.Oidc;
        var authentication = services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = OidcConfiguration.SessionScheme;
            options.DefaultSignInScheme = OidcConfiguration.SessionScheme;
        }).AddCookie(OidcConfiguration.SessionScheme, _ => { });
        services.AddOptions<CookieAuthenticationOptions>(OidcConfiguration.SessionScheme)
            .Configure<IdentityTicketStore>((options, tickets) =>
            {
                options.SessionStore = tickets;
                options.Cookie.Name = oidc ? "__Host-kofte-session" : "kofte-dev-session";
                options.Cookie.Path = "/";
                options.Cookie.Domain = null;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = oidc ? CookieSecurePolicy.Always : CookieSecurePolicy.None;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.IsEssential = true;
                options.CookieManager = new IdentitySessionCookieManager();
                options.SlidingExpiration = false;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.Events.OnRedirectToLogin = context =>
                    IdentityProblem.WriteAsync(context.HttpContext, IdentityFailure.Unauthenticated);
                options.Events.OnRedirectToAccessDenied = context =>
                    IdentityProblem.WriteAsync(context.HttpContext, IdentityFailure.Forbidden);
            });
        if (!oidc) return;
        services.AddSingleton(services => new OidcBackchannel(configuration.Provider!,
            services.GetRequiredService<IHttpContextAccessor>()));
        authentication.AddOpenIdConnect(OidcConfiguration.ProviderScheme, _ => { });
        services.AddOptions<OpenIdConnectOptions>(OidcConfiguration.ProviderScheme)
            .Configure<OidcBackchannel>((options, backchannel) =>
            {
                OidcConfiguration.Apply(options, configuration.Provider!, configuration.PublicOrigin, backchannel);
                options.Events.OnTicketReceived = async context =>
                {
                    context.HandleResponse();
                    var http = context.HttpContext;
                    var browser = http.RequestServices.GetRequiredService<IdentityBrowserBinding>().Require(http);
                    var budget = Kofte.Infrastructure.Identity.IdentityDiagnostics.Current?.Budget ??
                        throw new InvalidOperationException("identity_request_budget_missing");
                    var result = await http.RequestServices.GetRequiredService<ValidatedLogin>()
                        .CompleteOidcAsync(http,
                            context.Principal ?? throw new IdentityAttemptException(), context.Properties, browser, budget);
                    http.Response.StatusCode = 303;
                    http.Response.Headers.Location = result.ReturnPath;
                    http.Response.ContentType = "text/plain; charset=utf-8";
                };
                options.Events.OnRemoteFailure = context =>
                {
                    context.HandleResponse();
                    var failure = Failure(context.Failure);
                    if (Kofte.Infrastructure.Identity.IdentityDiagnostics.Current?.Durability ==
                        Kofte.Platform.Modules.Storage.StorageDurability.Uncertain)
                        failure = IdentityFailure.OutcomeUncertain;
                    return IdentityProblem.WriteAsync(context.HttpContext, failure);
                };
            });
    }

    private static IdentityFailure Failure(Exception? error)
    {
        for (var depth = 0; error is not null && depth < 8; depth++, error = error.InnerException)
        {
            if (error is IdentityAdmissionException or IdentityEventCapacityException)
                return IdentityFailure.AdmissionExceeded;
            if (error is OidcProviderException provider)
                return provider.Unavailable ? IdentityFailure.Unavailable : IdentityFailure.Unauthenticated;
            if (error is StorageException or Microsoft.Data.Sqlite.SqliteException or Npgsql.NpgsqlException or
                OperationCanceledException or CryptographicException or
                IOException or InvalidOperationException)
                return IdentityFailure.Unavailable;
        }
        return IdentityFailure.Unauthenticated;
    }
}
