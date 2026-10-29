using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kofte.TestSupport.Identity;

internal sealed partial class IdentityBrowserIngress : IAsyncDisposable
{
    private readonly HttpClient client = new(new SocketsHttpHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false,
        UseProxy = false
    })
    { Timeout = TimeSpan.FromSeconds(5) };
    private WebApplication? application;
    private int loseEnrollmentAcknowledgement;
    private int enrollmentAcknowledgementLost;
    private int loseSignOutAcknowledgement;
    private int signOutAcknowledgementLost;
    private readonly ConcurrentDictionary<string, int> responseStatuses = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<string> accessRecords = new();
    private int accessRecordCount;
    internal IReadOnlyList<string> TakeAccessRecords()
    {
        if (application is not null) throw new InvalidOperationException("identity_ingress_capture_not_stopped");
        var records = accessRecords.ToArray();
        accessRecords.Clear();
        return records;
    }
    internal Uri Origin { get; private set; } = null!;
    internal Uri Web { get; }
    internal Uri? DirectIdentityBackend { get; set; }
    internal bool OpaqueLoginOriginObserved { get; private set; }
    internal IReadOnlyDictionary<string, int> ResponseStatuses => responseStatuses;
    internal bool EnrollmentAcknowledgementLost => Volatile.Read(ref enrollmentAcknowledgementLost) == 1;
    internal void LoseNextEnrollmentAcknowledgement() => Interlocked.Exchange(ref loseEnrollmentAcknowledgement, 1);
    internal bool SignOutAcknowledgementLost => Volatile.Read(ref signOutAcknowledgementLost) == 1;
    internal void LoseNextSignOutAcknowledgement() => Interlocked.Exchange(ref loseSignOutAcknowledgement, 1);

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
            {
                server.Limits.MaxRequestLineSize = 16_384 + 32;
                server.Listen(IPAddress.Loopback, 0, listener => listener.UseHttps(certificate));
            });
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
        var captureAction = ActionCaptureEnabled && context.Request.Method == "POST" &&
            context.Request.Path == "/identity" &&
            context.Request.Headers.TryGetValue("Next-Action", out var action) &&
            action.Count == 1 && action[0] is { Length: > 0 and <= 256 };
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(captureAction
            ? application!.Lifetime.ApplicationStopping : context.RequestAborted);
        budget.CancelAfter(TimeSpan.FromSeconds(5));
        var directPath = context.Request.Path.Value switch
        {
            "/fixture-direct-identity" => "/api/identity/v1/me",
            "/fixture-direct-ungranted" => "/fixture-ungranted",
            _ => null
        };
        var direct = directPath is not null && DirectIdentityBackend is not null;
        using var request = new HttpRequestMessage(new(context.Request.Method), direct
            ? new Uri(DirectIdentityBackend!, directPath).AbsoluteUri
            : Web.GetLeftPart(UriPartial.Authority) + context.Request.GetEncodedPathAndQuery());
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
        var actionCaptureId = captureAction
            ? await CaptureActionResponseAsync(response, budget.Token) : null;
        var operation = context.Request.Path.Value switch
        {
            "/identity" => "identity_page",
            "/auth/oidc/callback" => "callback",
            "/api/identity/v1/browser-context" => "browser_context",
            "/api/identity/v1/me" => "current_identity",
            "/api/identity/v1/first-administrator" => "enrollment",
            "/api/identity/v1/sign-out" => "sign_out",
            _ => null
        };
        if (operation is not null)
        {
            responseStatuses[operation] = (int)response.StatusCode;
            if (Interlocked.Increment(ref accessRecordCount) > 1024)
                throw new InvalidOperationException("identity_ingress_capture_limit");
            accessRecords.Enqueue(JsonSerializer.Serialize(new
            {
                eventCode = "identity_ingress_access",
                operation,
                method = context.Request.Method is "GET" or "POST" ? context.Request.Method : "other",
                status = (int)response.StatusCode,
                outcome = (int)response.StatusCode switch
                {
                    >= 500 => "unavailable",
                    401 => "unauthenticated",
                    403 => "forbidden",
                    429 => "admission_exceeded",
                    >= 400 => "request_refused",
                    _ => "ready"
                },
                correlation = Guid.NewGuid().ToString("N"),
                durability = "unknown"
            }));
        }
        if (operation == "enrollment" && context.Request.Method == "POST" &&
            response.StatusCode == HttpStatusCode.OK &&
            Interlocked.Exchange(ref loseEnrollmentAcknowledgement, 0) == 1)
        {
            await response.Content.LoadIntoBufferAsync(8192, budget.Token);
            Volatile.Write(ref enrollmentAcknowledgementLost, 1);
            context.Abort();
            return;
        }
        if (operation == "sign_out" && context.Request.Method == "POST" &&
            response.StatusCode == HttpStatusCode.NoContent &&
            Interlocked.Exchange(ref loseSignOutAcknowledgement, 0) == 1)
        {
            await response.Content.LoadIntoBufferAsync(8192, budget.Token);
            Volatile.Write(ref signOutAcknowledgementLost, 1);
            context.Abort();
            return;
        }
        context.Response.StatusCode = (int)response.StatusCode;
        foreach (var header in response.Headers.Concat(response.Content.Headers))
            if (!HopByHop(header.Key) && !header.Key.Equals(ActionCaptureHeader, StringComparison.OrdinalIgnoreCase))
                context.Response.Headers[header.Key] = header.Value.ToArray();
        if (actionCaptureId is not null) context.Response.Headers[ActionCaptureHeader] = actionCaptureId;
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
            application = null;
        }
        client.Dispose();
        CleanupActionCaptures();
    }
}
