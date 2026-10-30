using System.Security.Cryptography;
using System.Text;
using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Platform.ContractTests.Identity;

public abstract class LoginCommitContractTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RealNativeLoginCommitsTheExactIdentityAttemptSessionAndEvidenceBeforeOneOpaqueCookie()
    {
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token);
        var baseline = await fixture.SnapshotAsync(Token);
        Assert.Equal(1, baseline.Revision);
        Assert.Equal("keys_prepared", Assert.Single(baseline.Events).Code);
        var response = await fixture.LoginAsync(LoginCommitFault.None, Token);
        Assert.Equal(303, response.Status);
        Assert.True(response.SessionCookieIssued);
        Assert.True(response.CookieContainsOnlyReference);
        Assert.True(response.CookieHasRequiredFlags);
        var state = await fixture.SnapshotAsync(Token);
        var user = Assert.Single(state.Users);
        var mapping = Assert.Single(state.Mappings);
        var session = Assert.Single(state.Sessions);
        var attempt = Assert.Single(state.Attempts);
        Assert.Equal(3, state.Revision);
        Assert.Equal(0, state.RegistryRevision);
        Assert.Equal("disabled", state.BootstrapState);
        Assert.Equal(user.Id, mapping.UserId);
        Assert.Equal(user.Id, session.UserId);
        Assert.Equal(fixture.Issuer, mapping.Issuer);
        Assert.Equal("synthetic-subject", mapping.Subject);
        Assert.Equal("oidc", user.Mode);
        Assert.Equal("oidc", mapping.Mode);
        Assert.Equal("oidc", session.Mode);
        Assert.Equal("active", session.State);
        Assert.Equal(state.Epoch, session.Epoch);
        Assert.Equal(response.AttemptId, attempt.Id);
        Assert.Equal("consumed", attempt.State);
        Assert.Equal(state.Epoch, attempt.Epoch);
        Assert.Equal(session.CreatedAt.AddMinutes(30), session.IdleExpiresAt);
        Assert.Equal(session.CreatedAt.AddHours(8), session.AbsoluteExpiresAt);
        Assert.True(response.Reference.Length == 43 &&
            response.Reference.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'));
        var expectedDigest = Convert.ToHexStringLower(SHA256.HashData(
            Encoding.ASCII.GetBytes("kofte.identity.session.reference.v1\0" + response.Reference)));
        Assert.Equal(expectedDigest, session.Digest);
        Assert.True(session.ProtectedTicket.Length > 0);
        Assert.False(session.ProtectedTicket.AsSpan().IndexOf(Encoding.ASCII.GetBytes(response.Reference)) >= 0);
        Assert.Contains(Assert.Single(baseline.Events), state.Events);
        Assert.Equal(3, state.Events.Count);
        var loginEvents = state.Events.Where(item => item.Code != "keys_prepared").ToArray();
        Assert.Equal(new[] { "session_created", "user_created" },
            loginEvents.Select(item => item.Code).Order(StringComparer.Ordinal));
        Assert.All(loginEvents, item =>
        {
            Assert.Equal(3, item.Revision);
            Assert.Equal(user.Id, item.UserId);
            Assert.NotEqual(Guid.Empty, item.Id);
        });
        Assert.Single(loginEvents.Select(item => item.Correlation).Distinct(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(LoginCommitFault.BeforeCommit, false)]
    [InlineData(LoginCommitFault.AfterCommit, true)]
    [InlineData(LoginCommitFault.CookiePublication, true)]
    public async Task RealCommitBoundaryAndPublicationFailuresNeverPublishASessionCookie(
        LoginCommitFault fault, bool committed)
    {
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token);
        var baseline = await fixture.SnapshotAsync(Token);
        Assert.Equal(1, baseline.Revision);
        Assert.Equal("keys_prepared", Assert.Single(baseline.Events).Code);
        var response = await fixture.LoginAsync(fault, Token);
        Assert.Equal(503, response.Status);
        Assert.False(response.SessionCookieIssued);
        var state = await fixture.SnapshotAsync(Token);
        var attempt = Assert.Single(state.Attempts);
        Assert.Equal(response.AttemptId, attempt.Id);
        Assert.Equal(committed ? "consumed" : "pending", attempt.State);
        Assert.Equal(committed ? 3 : 2, state.Revision);
        Assert.Equal(0, state.RegistryRevision);
        Assert.Equal("disabled", state.BootstrapState);
        if (committed)
        {
            var user = Assert.Single(state.Users);
            Assert.Equal(user.Id, Assert.Single(state.Mappings).UserId);
            Assert.Equal(user.Id, Assert.Single(state.Sessions).UserId);
            Assert.Contains(Assert.Single(baseline.Events), state.Events);
            Assert.Equal(3, state.Events.Count);
            Assert.All(state.Events.Where(item => item.Code != "keys_prepared"), item =>
            {
                Assert.Equal(3, item.Revision);
                Assert.Equal(user.Id, item.UserId);
            });
        }
        else
        {
            Assert.Empty(state.Users);
            Assert.Empty(state.Mappings);
            Assert.Empty(state.Sessions);
            Assert.Equal(baseline.Events, state.Events);
        }
    }

    [Fact]
    public async Task FrameworkStoreCannotCreateAnotherCommitFromAValidButUnboundOrReusedTicket()
    {
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token);
        var response = await fixture.LoginAsync(LoginCommitFault.None, Token);
        Assert.Equal(303, response.Status);
        var before = await fixture.SnapshotAsync(Token);
        Assert.True(await fixture.UnboundAndReusedStoresRefuseAsync(Token));
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Revision, after.Revision);
        Assert.Equal(Assert.Single(before.Users), Assert.Single(after.Users));
        Assert.Equal(Assert.Single(before.Mappings), Assert.Single(after.Mappings));
        Assert.Equal(Assert.Single(before.Sessions).Digest, Assert.Single(after.Sessions).Digest);
        Assert.Equal(before.Events, after.Events);
    }

    [Theory]
    [InlineData("login_before_commit", false)]
    [InlineData("login_committed", true)]
    public async Task KillingTheActualNativeCallbackOwnerLosesAcknowledgementWithoutSplittingTheCommit(
        string phase, bool committed)
    {
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token);
        var baseline = await fixture.SnapshotAsync(Token);
        var crash = await fixture.CrashLoginAsync(phase, Token);
        Assert.True(crash.AcknowledgementLost);
        Assert.True(crash.ExitVerified);
        var state = await fixture.SnapshotAsync(Token);
        Assert.Equal(committed ? baseline.Revision + 2 : baseline.Revision + 1, state.Revision);
        var attempt = Assert.Single(state.Attempts);
        Assert.Equal(crash.AttemptId, attempt.Id);
        Assert.Equal(committed ? "consumed" : "pending", attempt.State);
        Assert.Equal(baseline.Epoch, state.Epoch);
        Assert.Equal(baseline.RegistryRevision, state.RegistryRevision);
        Assert.Equal(baseline.BootstrapState, state.BootstrapState);
        Assert.Contains(Assert.Single(baseline.Events), state.Events);
        if (committed)
        {
            var user = Assert.Single(state.Users);
            Assert.Equal(user.Id, Assert.Single(state.Mappings).UserId);
            Assert.Equal(user.Id, Assert.Single(state.Sessions).UserId);
            Assert.Equal(3, state.Events.Count);
            Assert.All(state.Events.Where(item => item.Code != "keys_prepared"), item =>
            {
                Assert.Equal(state.Revision, item.Revision);
                Assert.Equal(user.Id, item.UserId);
            });
        }
        else
        {
            Assert.Empty(state.Users);
            Assert.Empty(state.Mappings);
            Assert.Empty(state.Sessions);
            Assert.Equal(baseline.Events, state.Events);
        }
    }

    [Fact]
    public async Task ExistingCookieCannotTurnFreshLoginIntoRenewalOfThePreLoginReference()
    {
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token);
        var first = await fixture.LoginAsync(LoginCommitFault.None, Token);
        var original = await fixture.SnapshotAsync(Token);
        var second = await fixture.LoginAsync(LoginCommitFault.None, Token);
        Assert.Equal(303, first.Status);
        Assert.Equal(303, second.Status);
        Assert.True(first.SessionCookieIssued && second.SessionCookieIssued);
        Assert.False(string.Equals(first.Reference, second.Reference, StringComparison.Ordinal));
        var state = await fixture.SnapshotAsync(Token);
        Assert.Equal(5, state.Revision);
        Assert.Equal(Assert.Single(original.Users), Assert.Single(state.Users));
        Assert.Equal(Assert.Single(original.Mappings), Assert.Single(state.Mappings));
        Assert.Equal(2, state.Sessions.Count);
        Assert.Equal(2, state.Sessions.Select(item => item.Digest).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(state.Events, item => item == original.Events.Single(value => value.Code == "user_created"));
        Assert.Equal(4, state.Events.Count);
        Assert.Equal(second.AttemptId, Assert.Single(state.Attempts).Id);
        Assert.Equal("consumed", Assert.Single(state.Attempts).State);
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteLoginCommitContractTests : LoginCommitContractTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresLoginCommitContractTests : LoginCommitContractTests
{
    protected override string Provider => "postgres";
}
