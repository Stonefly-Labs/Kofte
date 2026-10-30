using System.Security.Cryptography;
using System.Text;
using Kofte.Platform.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace Kofte.Host.Features.Identity.Login;

internal sealed class IdentityBrowserBinding(IDataProtectionProvider protection,
    IdentityConfiguration configuration, TimeProvider clock)
{
    private readonly IDataProtector protector = protection.CreateProtector("kofte.identity.browser.v1");
    private string CookieName => configuration.Mode == IdentityMode.Oidc ? "__Host-kofte-browser" : "kofte-dev-browser";

    internal string GetOrCreate(HttpContext context)
    {
        var existing = Read(context);
        if (existing is not null) return existing;
        var bytes = new byte[40];
        RandomNumberGenerator.Fill(bytes.AsSpan(0, 32));
        System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(bytes.AsSpan(32), clock.GetUtcNow().UtcTicks);
        context.Response.Cookies.Append(CookieName, WebEncoders.Base64UrlEncode(protector.Protect(bytes)),
            new CookieOptions
            {
                Path = "/",
                HttpOnly = true,
                Secure = configuration.Mode == IdentityMode.Oidc,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromMinutes(5)
            });
        return Digest(bytes.AsSpan(0, 32));
    }

    internal string Require(HttpContext context) => Read(context) ?? throw new IdentityBrowserException();

    private string? Read(HttpContext context)
    {
        if (!context.Request.Cookies.TryGetValue(CookieName, out var value)) return null;
        if (value.Length is < 1 or > 1024) throw new IdentityBrowserException();
        byte[] bytes;
        try { bytes = protector.Unprotect(WebEncoders.Base64UrlDecode(value)); }
        catch (Exception error) when (error is CryptographicException or FormatException)
        {
            throw new IdentityBrowserException();
        }
        if (bytes.Length != 40) throw new IdentityBrowserException();
        var created = System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(32));
        var now = clock.GetUtcNow().UtcTicks;
        if (created < DateTime.MinValue.Ticks || created > now || now - created >= TimeSpan.TicksPerMinute * 5)
            return null;
        return Digest(bytes.AsSpan(0, 32));
    }

    private static string Digest(ReadOnlySpan<byte> reference) =>
        Convert.ToHexStringLower(SHA256.HashData(
            [.. Encoding.ASCII.GetBytes("kofte.identity.browser.reference.v1\0"), .. reference]));
}

internal sealed class IdentityBrowserException() : Exception("identity_browser_invalid");
