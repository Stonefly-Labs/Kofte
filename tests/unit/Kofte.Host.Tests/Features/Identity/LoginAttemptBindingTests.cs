using Kofte.Host.Features.Identity.Login;
using Kofte.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace Kofte.Host.Tests.Features.Identity;

public sealed class LoginAttemptBindingTests
{
    [Fact]
    public void MaintainedProtectedPropertiesCarryTheAttemptWithoutExposingOrTrustingCallerFields()
    {
        var now = DateTime.UtcNow;
        var attempt = new IdentityLoginAttempt(Guid.NewGuid(), new string('b', 64), "oidc",
            new string('a', 64), Guid.NewGuid(), "/", now, now.AddMinutes(5));
        var format = new PropertiesDataFormat(new EphemeralDataProtectionProvider()
            .CreateProtector("test-only.oidc.properties"));
        var state = format.Protect(LoginAttemptBinding.Create(attempt));
        Assert.DoesNotContain(attempt.AttemptId.ToString("D"), state, StringComparison.Ordinal);
        Assert.DoesNotContain(attempt.BrowserDigest, state, StringComparison.Ordinal);
        var restored = Assert.IsType<AuthenticationProperties>(format.Unprotect(state));
        Assert.Equal(attempt.AttemptId, LoginAttemptBinding.Read(restored, attempt.BrowserDigest));
        restored.RedirectUri = "//outside.invalid";
        Assert.Equal(attempt.AttemptId, LoginAttemptBinding.Read(restored, attempt.BrowserDigest));
        Assert.Throws<IdentityAttemptException>(() => LoginAttemptBinding.Read(restored, new string('c', 64)));
        Assert.Throws<IdentityAttemptException>(() => LoginAttemptBinding.Read(restored, "raw-binding"));
        Assert.Null(format.Unprotect(state[..^4] + "!!!!"));
    }

    [Fact]
    public void MissingMalformedOrUnboundPropertiesCannotSupplyAnAttempt()
    {
        Assert.Throws<IdentityAttemptException>(() =>
            LoginAttemptBinding.Read(new AuthenticationProperties(), new string('b', 64)));
        Assert.Throws<IdentityAttemptException>(() => LoginAttemptBinding.Read(null, new string('b', 64)));
    }
}
