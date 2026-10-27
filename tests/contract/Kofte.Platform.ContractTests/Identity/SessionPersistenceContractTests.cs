using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Platform.ContractTests.Identity;

public abstract class SessionPersistenceContractTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task IdleBoundaryUsesTheDurableRowAndExpiresTerminallyAtExactlyThirtyMinutes(bool expired)
    {
        var clock = new SessionClock();
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token, clock);
        Assert.Equal(303, (await fixture.LoginAsync(LoginCommitFault.None, Token)).Status);
        var original = Assert.Single((await fixture.SnapshotAsync(Token)).Sessions);
        clock.Now = new DateTimeOffset(original.CreatedAt.AddMinutes(30).AddTicks(expired ? 0 : -1));
        var response = await fixture.ProbeAsync(Token);
        Assert.Equal(expired ? 401 : 200, response.Status);
        var state = await fixture.SnapshotAsync(Token);
        var current = Assert.Single(state.Sessions);
        Assert.Equal(original.Digest, current.Digest);
        Assert.Equal(original.CreatedAt, current.CreatedAt);
        Assert.Equal(original.AbsoluteExpiresAt, current.AbsoluteExpiresAt);
        if (expired)
        {
            Assert.Equal("expired", current.State);
            Assert.Equal(clock.Now.UtcDateTime, current.TerminalAt);
            Assert.Equal(original.LastSeenAt, current.LastSeenAt);
            var terminalEvent = Assert.Single(state.Events, item => item.Code == "session_expired");
            Assert.Equal(original.UserId, terminalEvent.UserId);
            Assert.Equal(state.Revision, terminalEvent.Revision);
            Assert.Equal(401, (await fixture.ProbeAsync(Token)).Status);
            var repeated = await fixture.SnapshotAsync(Token);
            Assert.Equal(current.TerminalAt, Assert.Single(repeated.Sessions).TerminalAt);
            Assert.Contains(terminalEvent, repeated.Events);
            Assert.Equal(state.Events.Count, repeated.Events.Count);
        }
        else
        {
            Assert.Equal("active", current.State);
            Assert.Null(current.TerminalAt);
            Assert.Equal(original.UserId, response.UserId);
            Assert.Equal("oidc", response.Mode);
            Assert.False(response.IsInitialAdministrator);
            Assert.True(current.LastSeenAt <= clock.Now.UtcDateTime &&
                clock.Now.UtcDateTime - current.LastSeenAt < TimeSpan.FromMicroseconds(1));
            Assert.Equal(current.LastSeenAt.AddMinutes(30), current.IdleExpiresAt);
        }
    }

    [Fact]
    public async Task RepeatedActivityCannotExtendTheEightHourAbsoluteLimit()
    {
        var clock = new SessionClock();
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token, clock);
        Assert.Equal(303, (await fixture.LoginAsync(LoginCommitFault.None, Token)).Status);
        var original = Assert.Single((await fixture.SnapshotAsync(Token)).Sessions);
        for (var index = 1; index <= 16; index++)
        {
            clock.Now = new DateTimeOffset(original.CreatedAt.AddMinutes(29 * index));
            Assert.Equal(200, (await fixture.ProbeAsync(Token)).Status);
        }
        clock.Now = new DateTimeOffset(original.AbsoluteExpiresAt.AddTicks(-1));
        Assert.Equal(200, (await fixture.ProbeAsync(Token)).Status);
        var renewed = Assert.Single((await fixture.SnapshotAsync(Token)).Sessions);
        Assert.Equal(original.AbsoluteExpiresAt, renewed.AbsoluteExpiresAt);
        Assert.Equal(original.AbsoluteExpiresAt, renewed.IdleExpiresAt);
        clock.Now = new DateTimeOffset(original.AbsoluteExpiresAt);
        Assert.Equal(401, (await fixture.ProbeAsync(Token)).Status);
        var expired = Assert.Single((await fixture.SnapshotAsync(Token)).Sessions);
        Assert.Equal("expired", expired.State);
        Assert.Equal(original.AbsoluteExpiresAt, expired.TerminalAt);
    }

    [Fact]
    public async Task ConfirmedRevocationRacingActivityIsTerminalOnTheNextRequest()
    {
        var clock = new SessionClock();
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token, clock);
        Assert.Equal(303, (await fixture.LoginAsync(LoginCommitFault.None, Token)).Status);
        var original = Assert.Single((await fixture.SnapshotAsync(Token)).Sessions);
        clock.Now = new DateTimeOffset(original.CreatedAt.AddMinutes(10));
        await Task.WhenAll(fixture.ProbeAsync(Token), fixture.RevokeCurrentAsync(Token));
        Assert.Equal(401, (await fixture.ProbeAsync(Token)).Status);
        var state = await fixture.SnapshotAsync(Token);
        var revoked = Assert.Single(state.Sessions);
        Assert.Equal(original.Digest, revoked.Digest);
        Assert.Equal("revoked", revoked.State);
        Assert.Equal(clock.Now.UtcDateTime, revoked.TerminalAt);
        Assert.Equal(original.AbsoluteExpiresAt, revoked.AbsoluteExpiresAt);
        Assert.Equal(original.UserId, Assert.Single(state.Events, item => item.Code == "session_revoked").UserId);
    }

    [Fact]
    public async Task CorruptProtectedTicketAbortsVerificationInsteadOfReturningAnAnonymousOrCachedActor()
    {
        var clock = new SessionClock();
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token, clock);
        Assert.Equal(303, (await fixture.LoginAsync(LoginCommitFault.None, Token)).Status);
        Assert.Equal(200, (await fixture.ProbeAsync(Token)).Status);
        await fixture.CorruptCurrentTicketAsync(Token);
        var before = await fixture.SnapshotAsync(Token);
        clock.Now = clock.Now.AddMinutes(1);
        var response = await fixture.ProbeAsync(Token);
        Assert.Equal(503, response.Status);
        Assert.Null(response.UserId);
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Revision, after.Revision);
        Assert.Equal(Assert.Single(before.Sessions).LastSeenAt, Assert.Single(after.Sessions).LastSeenAt);
        Assert.Equal(before.Events, after.Events);
    }

    [Fact]
    public async Task NinthSessionRefusesWithoutEvictingTheEightCommittedReferencesOrCreatingAnotherEvent()
    {
        var clock = new SessionClock();
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token, clock);
        for (var index = 0; index < 8; index++)
            Assert.Equal(303, (await fixture.LoginAsync(LoginCommitFault.None, Token)).Status);
        var before = await fixture.SnapshotAsync(Token);
        Assert.Equal(8, before.Sessions.Count);
        var refusal = await fixture.LoginAsync(LoginCommitFault.None, Token);
        Assert.Equal(429, refusal.Status);
        Assert.False(refusal.SessionCookieIssued);
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Revision + 1, after.Revision);
        Assert.Equal(before.Sessions.Select(item => item.Digest), after.Sessions.Select(item => item.Digest));
        Assert.All(after.Sessions, item =>
        {
            Assert.Equal("active", item.State);
            Assert.Null(item.TerminalAt);
        });
        Assert.Equal(before.Events, after.Events);
        Assert.Equal(Assert.Single(before.Users), Assert.Single(after.Users));
        Assert.Equal(Assert.Single(before.Mappings), Assert.Single(after.Mappings));
        var pending = Assert.Single(after.Attempts);
        Assert.Equal(refusal.AttemptId, pending.Id);
        Assert.Equal("pending", pending.State);
    }

    [Fact]
    public async Task InstallationLimitRefusesTheNextSessionButAdmitsFreshLoginAfterStoredSessionsExpire()
    {
        var clock = new SessionClock();
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token, clock);
        Assert.Equal(303, (await fixture.LoginAsync(LoginCommitFault.None, Token)).Status);
        await fixture.SeedGlobalCapacityAsync(Token);
        var before = await fixture.CapacitySnapshotAsync(Token);
        Assert.Equal(50_000, before.Sessions);
        Assert.Equal(50_000, before.LiveSessions);
        Assert.Equal(6_251, before.Users);
        var refusal = await fixture.LoginAsync(LoginCommitFault.None, Token);
        Assert.Equal(429, refusal.Status);
        Assert.False(refusal.SessionCookieIssued);
        var after = await fixture.CapacitySnapshotAsync(Token);
        Assert.Equal(before.Revision + 1, after.Revision);
        Assert.Equal(before.Sessions, after.Sessions);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.OriginalDigest, after.OriginalDigest);
        Assert.Equal("active", after.OriginalState);
        Assert.Equal(before.Events, after.Events);
        Assert.Equal("pending", after.AttemptState);
        clock.Now = clock.Now.AddMinutes(30);
        var corrected = await fixture.LoginAsync(LoginCommitFault.None, Token);
        Assert.Equal(303, corrected.Status);
        Assert.True(corrected.SessionCookieIssued);
        var fresh = await fixture.CapacitySnapshotAsync(Token);
        Assert.Equal(50_001, fresh.Sessions);
        Assert.Equal(1, fresh.LiveSessions);
        Assert.Equal(before.Users, fresh.Users);
        Assert.Equal(before.OriginalDigest, fresh.OriginalDigest);
        Assert.Equal("active", fresh.OriginalState);
        Assert.Equal("consumed", fresh.AttemptState);
        Assert.Equal(before.Events.Count + 1, fresh.Events.Count);
        Assert.Equal(200, (await fixture.ProbeAsync(Token)).Status);
    }

    private sealed class SessionClock : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteSessionPersistenceContractTests : SessionPersistenceContractTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresSessionPersistenceContractTests : SessionPersistenceContractTests
{
    protected override string Provider => "postgres";
}
