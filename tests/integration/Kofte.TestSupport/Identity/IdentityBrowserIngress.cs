using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kofte.TestSupport.Identity;

internal sealed class IdentityBrowserIngress : IAsyncDisposable
{
    private readonly HttpClient client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        UseProxy = false
    })
    { Timeout = TimeSpan.FromSeconds(5) };
    private WebApplication? application;
    private readonly ConcurrentDictionary<string, int> responseStatuses = new(StringComparer.Ordinal);
    internal Uri Origin { get; private set; } = null!;
    internal Uri Web { get; }
    internal bool OpaqueLoginOriginObserved { get; private set; }
    internal IReadOnlyDictionary<string, int> ResponseStatuses => responseStatuses;

    private IdentityBrowserIngress()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        Web = new($"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}");
    }

    internal static async Task<IdentityBrowserIngress> CreateAsync(X509Certificate2 certificate,
        CancellationToken token)
    {
        var fixture = new IdentityBrowserIngress();
        try
        {
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
            builder.Configuration.Sources.Clear();
            builder.Logging.ClearProviders();
            builder.WebHost.ConfigureKestrel(server =>
                server.Listen(IPAddress.Loopback, 0, listener => listener.UseHttps(certificate)));
            fixture.application = builder.Build();
            fixture.application.Run(fixture.ForwardAsync);
            await fixture.application.StartAsync(token);
            fixture.Origin = new(fixture.application.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single());
            return fixture;
        }
        catch { await fixture.DisposeAsync(); throw; }
    }

    private async Task ForwardAsync(HttpContext context)
    {
        if (context.Request.Path == "/auth/login" && context.Request.Method == "POST")
            OpaqueLoginOriginObserved |= context.Request.Headers.Origin == "null";
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        budget.CancelAfter(TimeSpan.FromSeconds(5));
        using var request = new HttpRequestMessage(new(context.Request.Method),
            Web.GetLeftPart(UriPartial.Authority) + context.Request.GetEncodedPathAndQuery());
        request.Headers.Host = context.Request.Host.Value;
        if (context.Request.ContentLength is > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
            request.Content = new StreamContent(context.Request.Body);
        foreach (var header in context.Request.Headers)
        {
            if (HopByHop(header.Key) || header.Key.Equals("Host", StringComparison.OrdinalIgnoreCase)) continue;
            if (!request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, budget.Token);
        var operation = context.Request.Path.Value switch
        {
            "/identity" => "identity_page",
            "/auth/oidc/callback" => "callback",
            "/api/identity/v1/browser-context" => "browser_context",
            "/api/identity/v1/me" => "current_identity",
            "/api/identity/v1/first-administrator" => "enrollment",
            _ => null
        };
        if (operation is not null) responseStatuses[operation] = (int)response.StatusCode;
        context.Response.StatusCode = (int)response.StatusCode;
        foreach (var header in response.Headers.Concat(response.Content.Headers))
            if (!HopByHop(header.Key)) context.Response.Headers[header.Key] = header.Value.ToArray();
        await response.Content.CopyToAsync(context.Response.Body, budget.Token);
    }

    private static bool HopByHop(string name) =>
        name.Equals("Connection", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Keep-Alive", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Proxy-Authenticate", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("TE", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Trailer", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("Upgrade", StringComparison.OrdinalIgnoreCase);

    public async ValueTask DisposeAsync()
    {
        if (application is not null)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await application.StopAsync(timeout.Token);
            await application.DisposeAsync();
        }
        client.Dispose();
    }
}
