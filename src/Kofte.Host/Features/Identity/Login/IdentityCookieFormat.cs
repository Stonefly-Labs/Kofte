using Microsoft.AspNetCore.Authentication;

namespace Kofte.Host.Features.Identity.Login;

internal sealed class IdentityCookieFormat(
    IdentityKeys keys, ISecureDataFormat<AuthenticationTicket> inner) : ISecureDataFormat<AuthenticationTicket>
{
    public string Protect(AuthenticationTicket data) => inner.Protect(data);
    public string Protect(AuthenticationTicket data, string? purpose) => inner.Protect(data, purpose);
    public AuthenticationTicket? Unprotect(string? protectedText) => Unprotect(protectedText, null);

    public AuthenticationTicket? Unprotect(string? protectedText, string? purpose)
    {
        // Framework decoding catches decryption failures; unavailable keys must instead remain a 503.
        keys.Verify();
        var ticket = inner.Unprotect(protectedText, purpose);
        keys.Verify();
        return ticket;
    }
}
