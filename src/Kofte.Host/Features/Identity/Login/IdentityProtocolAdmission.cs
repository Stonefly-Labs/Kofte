using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Kofte.Infrastructure.Identity;
using Kofte.Platform.Modules.Storage;
using OwnerDiagnostics = Kofte.Infrastructure.Identity.IdentityDiagnostics;

namespace Kofte.Host.Features.Identity.Login;

internal sealed class IdentityProtocolAdmission(RequestDelegate next)
{
    internal static async Task AdmitAsync(HttpContext context, string browser,
        IdentityAdmissionCategory? installationCategory = null, IdentityAdmissionCategory? browserCategory = null)
    {
        var requests = new List<IdentityAdmissionRequest>
        {
            new(IdentityAdmissionCategory.IdentityInstallation, Hash("installation")),
            new(IdentityAdmissionCategory.IdentityClient, browser),
            new(IdentityAdmissionCategory.IdentityClient, Hash("peer\0" +
                (context.Connection.RemoteIpAddress?.ToString() ??
                    throw new InvalidOperationException("identity_peer_missing"))))
        };
        if (installationCategory is { } installation) requests.Add(new(installation, Hash("installation")));
        if (browserCategory is { } client) requests.Add(new(client, browser));
        var services = context.RequestServices;
        var budget = OwnerDiagnostics.Current?.Budget ??
            throw new InvalidOperationException("identity_request_budget_missing");
        await services.GetRequiredService<IdentityAdmission>().AdmitAsync(
            services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime, requests, budget);
    }

    public async Task InvokeAsync(HttpContext context, IdentityBrowserBinding binding, TimeProvider clock)
    {
        try
        {
            if (context.Request.Path.Value == "/auth/oidc/callback")
                await AdmitAsync(context, binding.Require(context),
                    IdentityAdmissionCategory.CallbackInstallation, IdentityAdmissionCategory.CallbackBrowser);
            await next(context);
        }
        catch (IdentityBrowserException)
        {
            await IdentityProblem.WriteAsync(context, IdentityFailure.Unauthenticated);
        }
        catch (IdentityAdmissionException error)
        {
            if (error.RetryAt is { } retry)
                context.Response.Headers.RetryAfter = Math.Clamp(
                    (int)Math.Ceiling((retry - clock.GetUtcNow().UtcDateTime).TotalSeconds), 1, 60)
                    .ToString(CultureInfo.InvariantCulture);
            await IdentityProblem.WriteAsync(context, IdentityFailure.AdmissionExceeded);
        }
    }

    private static string Hash(string value) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("kofte.identity.admission.v1\0" + value)));
}
