using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Kofte.Host.Features.Identity.Login;

internal sealed class OidcProviderException(bool unavailable) : InvalidOperationException("identity_provider_refused")
{
    internal bool Unavailable { get; } = unavailable;
}

internal sealed class OidcBackchannel : DelegatingHandler
{
    private static readonly object BudgetKey = new();
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private static readonly HttpRequestOptionsKey<StrongBox<int>> Connections = new("kofte.identity.connection");
    private readonly IdentityProviderConfiguration provider;
    private readonly IHttpContextAccessor accessor;
    private readonly Func<string, CancellationToken, Task<IPAddress[]>> resolve;
    internal HttpClient Client { get; }

    internal OidcBackchannel(IdentityProviderConfiguration provider, IHttpContextAccessor accessor,
        SocketsHttpHandler? transport = null,
        Func<string, CancellationToken, Task<IPAddress[]>>? resolve = null)
    {
        (this.provider, this.accessor) = (provider, accessor);
        this.resolve = resolve ?? ((host, token) => Dns.GetHostAddressesAsync(host, token));
        transport ??= new();
        transport.AllowAutoRedirect = false;
        transport.AutomaticDecompression = DecompressionMethods.None;
        transport.UseCookies = false;
        transport.UseProxy = false;
        transport.Credentials = null;
        transport.PreAuthenticate = false;
        transport.ActivityHeadersPropagator = null;
        transport.PooledConnectionLifetime = TimeSpan.Zero;
        transport.ConnectCallback = ConnectAsync;
        InnerHandler = transport;
        Client = new(this, disposeHandler: false) { Timeout = Timeout.InfiniteTimeSpan };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var context = accessor.HttpContext ?? throw new OidcProviderException(true);
        var uri = request.RequestUri ?? throw new OidcProviderException(false);
        var endpoint = uri.OriginalString;
        var token = endpoint == provider.TokenEndpoint.OriginalString;
        if (uri.Scheme != "https" || !provider.AllowedEndpoints.Any(item => item.OriginalString == endpoint) ||
            (token ? request.Method != HttpMethod.Post :
                request.Method != HttpMethod.Get || endpoint != provider.MetadataAddress.OriginalString &&
                    endpoint != provider.JwksUri.OriginalString) ||
            request.Headers.Contains("Cookie") || request.Headers.Authorization is not null)
            throw new OidcProviderException(false);
        if (!context.Items.TryGetValue(BudgetKey, out var value))
            context.Items[BudgetKey] = value = new RequestBudget();
        if (value is not RequestBudget budget) throw new OidcProviderException(true);
        var remaining = budget.Admit();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, context.RequestAborted);
        timeout.CancelAfter(remaining);
        var started = Stopwatch.GetTimestamp();
        HttpResponseMessage? response = null;
        try
        {
            if (request.Content is not null)
            {
                var body = await request.Content.ReadAsByteArrayAsync(timeout.Token);
                if (body.Length > 8192) throw new OidcProviderException(false);
            }
            request.Version = HttpVersion.Version11;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;
            request.Headers.ConnectionClose = true;
            request.Options.Set(Connections, new(0));
            response = await base.SendAsync(request, timeout.Token);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new OidcProviderException((int)response.StatusCode >= 500 ||
                    response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests);
            var maximum = token ? 65_536 : 262_144;
            if (response.Content.Headers.ContentEncoding.Count != 0 ||
                response.Content.Headers.ContentLength > maximum ||
                !string.Equals(response.Content.Headers.ContentType?.MediaType, "application/json",
                    StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(response.Content.Headers.ContentType?.MediaType, "application/jwk-set+json",
                    StringComparison.OrdinalIgnoreCase))
                throw new OidcProviderException(false);
            using var original = response.Content;
            await using var stream = await original.ReadAsStreamAsync(timeout.Token);
            using var bytes = new MemoryStream();
            var buffer = new byte[8192];
            while (true)
            {
                var count = await stream.ReadAsync(buffer.AsMemory(0,
                    Math.Min(buffer.Length, maximum + 1 - checked((int)bytes.Length))), timeout.Token);
                if (count == 0) break;
                await bytes.WriteAsync(buffer.AsMemory(0, count), timeout.Token);
                if (bytes.Length > maximum) throw new OidcProviderException(false);
            }
            var payload = bytes.ToArray();
            using var document = Parse(payload);
            if (document.RootElement.ValueKind != JsonValueKind.Object) throw new OidcProviderException(false);
            if (token && (!document.RootElement.TryGetProperty("id_token", out var idToken) ||
                idToken.ValueKind != JsonValueKind.String || idToken.GetString() is not { Length: > 0 } encoded ||
                Encoding.UTF8.GetByteCount(encoded) > 16_384))
                throw new OidcProviderException(false);
            response.Content = new ByteArrayContent(payload);
            response.Content.Headers.ContentType = original.Headers.ContentType;
            timeout.Token.ThrowIfCancellationRequested();
            return response;
        }
        catch (Exception error) when (error is HttpRequestException or IOException or SocketException)
        {
            response?.Dispose();
            if (error is HttpRequestException { InnerException: OidcProviderException policy })
                throw new OidcProviderException(policy.Unavailable);
            throw new OidcProviderException(true);
        }
        catch
        {
            response?.Dispose();
            throw;
        }
        finally { budget.Complete(Stopwatch.GetElapsedTime(started)); }
    }

    private async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext context, CancellationToken token)
    {
        if (!context.InitialRequestMessage.Options.TryGetValue(Connections, out var attempts) ||
            Interlocked.Increment(ref attempts.Value) != 1)
            throw new OidcProviderException(true);
        var addresses = await resolve(context.DnsEndPoint.Host, token);
        if (addresses.Length == 0 || addresses.Any(address => !Allowed(address)))
            throw new OidcProviderException(false);
        var socket = new Socket(addresses[0].AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            await socket.ConnectAsync(new IPEndPoint(addresses[0], context.DnsEndPoint.Port), token);
            if (socket.RemoteEndPoint is not IPEndPoint peer || !Allowed(peer.Address))
                throw new OidcProviderException(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    private bool Allowed(IPAddress address) => provider.AllowedNetworks.Any(network =>
        network.Contains(address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address));

    internal static JsonDocument Parse(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            _ = StrictUtf8.GetCharCount(bytes.Span);
            return JsonDocument.Parse(bytes, new() { AllowDuplicateProperties = false, MaxDepth = 16 });
        }
        catch (Exception error) when (error is JsonException or DecoderFallbackException)
        {
            throw new OidcProviderException(false);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Client.Dispose();
        base.Dispose(disposing);
    }

    private sealed class RequestBudget
    {
        private int requests;
        private TimeSpan spent;
        private bool running;

        internal TimeSpan Admit()
        {
            lock (this)
            {
                var remaining = TimeSpan.FromSeconds(3) - spent;
                if (running || ++requests > 3 || remaining <= TimeSpan.Zero)
                    throw new OidcProviderException(true);
                running = true;
                return remaining < TimeSpan.FromSeconds(2) ? remaining : TimeSpan.FromSeconds(2);
            }
        }

        internal void Complete(TimeSpan elapsed)
        {
            lock (this)
            {
                spent += elapsed;
                running = false;
            }
        }
    }
}
