using System.Text;
using Kofte.Platform.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

namespace Kofte.Host.Features.Identity;

public sealed class IdentityRequestBoundary(RequestDelegate next, IdentityConfiguration configuration,
    IHostEnvironment environment)
{
    private sealed record Rule(string Method, bool Public, bool EmptyBody, bool Query = false);
    private static readonly IReadOnlyDictionary<string, Rule> Rules = new Dictionary<string, Rule>(StringComparer.Ordinal)
    {
        ["/health/live"] = new("GET", true, true),
        ["/health/ready"] = new("GET", true, true),
        ["/openapi/v1.json"] = new("GET", true, true),
        ["/api/identity/v1/browser-context"] = new("GET", true, true),
        ["/auth/login"] = new("POST", true, false),
        ["/auth/oidc/callback"] = new("GET", true, true, true),
        ["/api/identity/v1/development-session"] = new("POST", true, false),
        ["/api/identity/v1/me"] = new("GET", false, true),
        ["/api/identity/v1/sign-out"] = new("POST", false, true),
        ["/api/identity/v1/first-administrator"] = new("POST", false, false)
    };

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        var path = request.Path.Value ?? "";
        var endpoint = context.GetEndpoint();
        var health = path is "/health/live" or "/health/ready" or "/openapi/v1.json";
        if (!health)
        {
            IdentityProblem.Headers(context.Response);
            context.Response.OnStarting(() =>
            {
                IdentityProblem.Headers(context.Response);
                return Task.CompletedTask;
            });
        }
        Rules.TryGetValue(path, out var rule);
        if (rule is null && Rules.Keys.Contains(path.TrimEnd('/'), StringComparer.OrdinalIgnoreCase) ||
            endpoint is null || endpoint is RouteEndpoint route &&
            Rules.ContainsKey(route.RoutePattern.RawText ?? "") && rule is null ||
            endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null &&
                (rule?.Public != true || endpoint is not RouteEndpoint anonymous ||
                    anonymous.RoutePattern.RawText != path) ||
            path == "/openapi/v1.json" && !environment.IsDevelopment() ||
            path == "/api/identity/v1/development-session" && configuration.Mode != IdentityMode.Development ||
            path == "/auth/login" && configuration.Mode != IdentityMode.Oidc)
        {
            context.Response.StatusCode = 404;
            return;
        }
        if (rule is not null)
        {
            var target = context.Features.Get<IHttpRequestFeature>()?.RawTarget ?? path;
            var separator = target.IndexOf('?');
            if (request.PathBase.HasValue || (separator < 0 ? target : target[..separator]) != path)
            {
                context.Response.StatusCode = 404;
                return;
            }
            if (request.Method != rule.Method)
            {
                context.Response.StatusCode = 405;
                context.Response.Headers.Allow = rule.Method;
                return;
            }
            if (!rule.Query && request.QueryString.HasValue ||
                rule.EmptyBody && HasBody(context) || request.ContentLength is > 8192 ||
                rule.Query && Encoding.UTF8.GetByteCount(target) > 16_384)
            {
                await IdentityProblem.WriteAsync(context, IdentityFailure.RequestInvalid);
                return;
            }
        }
        if (!health && request.Headers.ContainsKey("Authorization"))
        {
            await IdentityProblem.WriteAsync(context, IdentityFailure.Unauthenticated);
            return;
        }
        if (!health && request.Headers.Sum(header => Encoding.UTF8.GetByteCount(header.Key) +
            header.Value.Sum(value => Encoding.UTF8.GetByteCount(value ?? "")) + 4) > 16_384)
        {
            await IdentityProblem.WriteAsync(context, IdentityFailure.RequestInvalid);
            return;
        }
        if (!health)
        {
            var admittedCookies = request.Headers.Cookie.SelectMany(header => (header ?? "").Split(';'))
                .Select(cookie => cookie.Trim().Split('=', 2)[0])
                .Where(name => name is "__Host-kofte-session" or "__Host-kofte-antiforgery" or
                    "__Host-kofte-browser" or "kofte-dev-session" or "kofte-dev-antiforgery" or
                    "kofte-dev-browser" || name.StartsWith("__Host-kofte-nonce.", StringComparison.Ordinal) ||
                    name.StartsWith("__Host-kofte-correlation.", StringComparison.Ordinal)).ToArray();
            if (admittedCookies.Length > 16 ||
                admittedCookies.Distinct(StringComparer.Ordinal).Count() != admittedCookies.Length)
            {
                await IdentityProblem.WriteAsync(context, IdentityFailure.RequestInvalid);
                return;
            }
        }
        if (context.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } size)
            size.MaxRequestBodySize = 8192;
        try { await next(context); }
        catch (BadHttpRequestException error) when (!context.Response.HasStarted && error.StatusCode is 400 or 413)
        {
            await IdentityProblem.WriteAsync(context, IdentityFailure.RequestInvalid);
        }
        catch (InvalidDataException) when (!context.Response.HasStarted)
        {
            await IdentityProblem.WriteAsync(context, IdentityFailure.RequestInvalid);
        }
    }

    public static async Task<bool> ValidateMutationAsync(HttpContext context)
    {
        var configuration = context.RequestServices.GetRequiredService<IdentityConfiguration>();
        var headers = context.Request.Headers;
        if (headers.Origin.Count != 1 || headers.Origin[0] != configuration.PublicOrigin.OriginalString ||
            headers.TryGetValue("Sec-Fetch-Site", out var site) &&
                (site.Count != 1 || site[0] is not ("same-origin" or "same-site" or "none")) ||
            headers.TryGetValue("X-Kofte-CSRF", out var token) && token.Count != 1)
        {
            await IdentityProblem.WriteAsync(context, IdentityFailure.Forbidden);
            return false;
        }
        try
        {
            await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context);
            return true;
        }
        catch (AntiforgeryValidationException)
        {
            await IdentityProblem.WriteAsync(context, IdentityFailure.Forbidden);
            return false;
        }
    }

    private static bool HasBody(HttpContext context) => context.Request.ContentLength is > 0 ||
        context.Request.Headers.ContainsKey("Transfer-Encoding") ||
        context.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody == true;
}
