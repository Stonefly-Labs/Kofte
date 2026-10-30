using System.Security.Cryptography;
using Kofte.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;

namespace Kofte.Host.Features.Identity.Login;

internal static class LoginAttemptBinding
{
    private const string AttemptKey = "kofte.attempt.v1";
    private const string BrowserKey = "kofte.browser.v1";

    internal static AuthenticationProperties Create(IdentityLoginAttempt attempt)
    {
        if (attempt.AttemptId == Guid.Empty || !IdentityStore.IsDigest(attempt.BrowserDigest) ||
            attempt.ReturnPath is not ("/" or "/identity"))
            throw new IdentityAttemptException();
        return new(new Dictionary<string, string?>
        {
            [AttemptKey] = attempt.AttemptId.ToString("D"),
            [BrowserKey] = attempt.BrowserDigest
        })
        {
            RedirectUri = attempt.ReturnPath,
            IssuedUtc = new DateTimeOffset(attempt.CreatedAt),
            ExpiresUtc = new DateTimeOffset(attempt.ExpiresAt)
        };
    }

    internal static Guid Read(AuthenticationProperties? properties, string browserDigest)
    {
        if (properties is null || !IdentityStore.IsDigest(browserDigest) ||
            !properties.Items.TryGetValue(AttemptKey, out var text) ||
            !Guid.TryParseExact(text, "D", out var id) || id == Guid.Empty || text != id.ToString("D") ||
            !properties.Items.TryGetValue(BrowserKey, out var bound) || bound is null ||
            !IdentityStore.IsDigest(bound) || !CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(bound), Convert.FromHexString(browserDigest)))
            throw new IdentityAttemptException();
        return id;
    }
}
