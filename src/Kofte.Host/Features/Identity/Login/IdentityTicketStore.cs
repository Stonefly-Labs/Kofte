using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace Kofte.Host.Features.Identity.Login;

internal sealed class IdentityTicketStore(IdentityStore store, IDataProtectionProvider protection,
    IHttpContextAccessor accessor, TimeProvider clock) : ITicketStore
{
    private static readonly object CommittedKey = new(), CurrentKey = new();
    private readonly IDataProtector protector = protection.CreateProtector("kofte.identity.session.ticket.v1");
    private sealed class Committed(string reference, byte[] serialized)
    {
        internal string Reference { get; } = reference;
        internal byte[] Serialized { get; } = serialized;
        internal int Used;
    }

    internal static AuthenticationTicket CreateTicket(IdentityUser user, DateTime now) =>
        new(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString("D")), new Claim("kofte.mode.v1", user.Mode)],
            OidcConfiguration.SessionScheme)), new AuthenticationProperties
            {
                IssuedUtc = new DateTimeOffset(now),
                ExpiresUtc = new DateTimeOffset(now.AddHours(8)),
                AllowRefresh = false
            }, OidcConfiguration.SessionScheme);

    internal byte[] Protect(AuthenticationTicket ticket) => protector.Protect(TicketSerializer.Default.Serialize(ticket));

    internal static (string Reference, string Digest) NewReference()
    {
        var reference = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return (reference, Digest(reference));
    }

    private static string Digest(string reference) => Convert.ToHexStringLower(SHA256.HashData(
        Encoding.ASCII.GetBytes("kofte.identity.session.reference.v1\0" + reference)));

    private static bool ValidReference(string reference) =>
        reference.Length == 43 &&
        reference.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_') &&
        WebEncoders.Base64UrlEncode(WebEncoders.Base64UrlDecode(reference)) == reference;

    internal static void BindCommitted(HttpContext context, string reference, AuthenticationTicket ticket)
    {
        if (!ValidReference(reference) || context.Items.ContainsKey(CommittedKey)) throw InvalidBridge();
        context.Items.Add(CommittedKey, new Committed(reference, TicketSerializer.Default.Serialize(ticket)));
    }

    internal static IdentitySessionView? Current(HttpContext context) =>
        context.Items.TryGetValue(CurrentKey, out var value) ? value as IdentitySessionView : null;

    public Task<string> StoreAsync(AuthenticationTicket ticket) =>
        StoreAsync(ticket, Context(), Context().RequestAborted);

    public Task<string> StoreAsync(AuthenticationTicket ticket, CancellationToken cancellationToken) =>
        StoreAsync(ticket, Context(), cancellationToken);

    public Task<string> StoreAsync(AuthenticationTicket ticket, HttpContext httpContext, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        httpContext.RequestAborted.ThrowIfCancellationRequested();
        if (!httpContext.Items.TryGetValue(CommittedKey, out var value) || value is not Committed committed ||
            !CryptographicOperations.FixedTimeEquals(committed.Serialized, TicketSerializer.Default.Serialize(ticket)) ||
            Interlocked.CompareExchange(ref committed.Used, 1, 0) != 0)
            throw InvalidBridge();
        return Task.FromResult(committed.Reference);
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket) => throw InvalidBridge();
    public Task RenewAsync(string key, AuthenticationTicket ticket, CancellationToken cancellationToken) => throw InvalidBridge();
    public Task RenewAsync(string key, AuthenticationTicket ticket, HttpContext httpContext,
        CancellationToken cancellationToken) => throw InvalidBridge();

    public Task<AuthenticationTicket?> RetrieveAsync(string key) =>
        RetrieveAsync(key, Context(), Context().RequestAborted);

    public Task<AuthenticationTicket?> RetrieveAsync(string key, CancellationToken cancellationToken) =>
        RetrieveAsync(key, Context(), cancellationToken);

    public async Task<AuthenticationTicket?> RetrieveAsync(string key, HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        httpContext.Items.Remove(CurrentKey);
        if (!ValidReference(key)) return null;
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(httpContext.RequestAborted, cancellationToken);
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), cancel.Token);
        var result = await store.MutateAsync(budget, async change =>
        {
            var view = await IdentitySessionStore.RetrieveAsync(change, Digest(key), clock.GetUtcNow().UtcDateTime);
            if (view is null) return null;
            var ticket = TicketSerializer.Default.Deserialize(protector.Unprotect(view.Session.ProtectedTicket))
                ?? throw new StorageException(StorageCode.Corrupt);
            Validate(ticket, view);
            return new Retrieved(ticket, view);
        });
        if (result is null) return null;
        httpContext.Items[CurrentKey] = result.View;
        return result.Ticket;
    }

    public Task RemoveAsync(string key) => RemoveAsync(key, Context(), Context().RequestAborted);
    public Task RemoveAsync(string key, CancellationToken cancellationToken) => RemoveAsync(key, Context(), cancellationToken);

    public async Task RemoveAsync(string key, HttpContext httpContext, CancellationToken cancellationToken)
    {
        if (!ValidReference(key)) throw InvalidBridge();
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(httpContext.RequestAborted, cancellationToken);
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), cancel.Token);
        await store.MutateAsync(budget, change =>
            IdentitySessionStore.RevokeAsync(change, Digest(key), clock.GetUtcNow().UtcDateTime));
        httpContext.Items.Remove(CurrentKey);
    }

    private static void Validate(AuthenticationTicket ticket, IdentitySessionView view)
    {
        var expected = CreateTicket(view.User, view.Session.CreatedAt);
        if (!CryptographicOperations.FixedTimeEquals(TicketSerializer.Default.Serialize(expected),
                TicketSerializer.Default.Serialize(ticket)))
            throw new StorageException(StorageCode.Corrupt);
    }

    private HttpContext Context() => accessor.HttpContext ?? throw InvalidBridge();
    private sealed record Retrieved(AuthenticationTicket Ticket, IdentitySessionView View);
    private static InvalidOperationException InvalidBridge() => new("identity_ticket_bridge_invalid");
}

internal sealed class IdentitySessionCookieManager : ICookieManager
{
    private readonly ChunkingCookieManager cookies = new();

    public string? GetRequestCookie(HttpContext context, string key) =>
        context.Request.Path.Value is "/auth/oidc/callback" or "/api/identity/v1/development-session" or
            "/health/live" or "/health/ready" or "/openapi/v1.json"
            ? null : cookies.GetRequestCookie(context, key);

    public void AppendResponseCookie(HttpContext context, string key, string? value, CookieOptions options) =>
        cookies.AppendResponseCookie(context, key, value, options);

    public void DeleteCookie(HttpContext context, string key, CookieOptions options) =>
        cookies.DeleteCookie(context, key, options);
}
