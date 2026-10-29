using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

public abstract class IdentityFailureTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("login")]
    [InlineData("current")]
    [InlineData("sign_out")]
    [InlineData("enrollment")]
    public async Task ActualLockedOrPausedStoreRefusesEveryIdentityOperationAndCorrectionPreservesAuthority(
        string operation)
    {
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token,
            productRoutes: true, sharePostgres: false);
        await LoginAsync(fixture);
        var authorization = operation == "enrollment" ? await fixture.NominateProductAsync(Token) : null;
        var csrf = await fixture.ProductContextAsync(Token);
        var before = await fixture.SnapshotAsync(Token);
        var bootstrap = await fixture.BootstrapAsync(Token);
        var providerRequests = fixture.ProductProviderRequests;
        using var request = operation switch
        {
            "login" => fixture.ProductLoginRequest("/identity", csrf),
            "current" => new(HttpMethod.Get, "/api/identity/v1/me"),
            "sign_out" => new(HttpMethod.Post, "/api/identity/v1/sign-out"),
            "enrollment" => Enrollment(fixture, csrf, authorization),
            _ => throw new InvalidOperationException("identity_failure_operation_invalid")
        };
        if (operation == "sign_out")
        {
            request.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
            request.Headers.Add("X-Kofte-CSRF", csrf);
        }
        await using (await fixture.InterruptProductStoreAsync(Token))
        {
            var elapsed = Stopwatch.StartNew();
            using var unavailable = await fixture.SendProductAsync(request, Token);
            await RequireUnavailableAsync(unavailable);
            Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(2));
        }
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(before.Events, after.Events);
        Assert.Equal(before.Attempts, after.Attempts);
        Assert.Equal(before.Sessions.Select(row => (row.Digest, row.UserId, row.State, row.LastSeenAt)),
            after.Sessions.Select(row => (row.Digest, row.UserId, row.State, row.LastSeenAt)));
        Assert.Equal(bootstrap, await fixture.BootstrapAsync(Token));
        Assert.Equal(providerRequests, fixture.ProductProviderRequests);
        using var currentRequest = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
        using var corrected = await fixture.SendProductAsync(currentRequest, Token);
        Assert.Equal(HttpStatusCode.OK, corrected.StatusCode);
        Assert.Equal(bootstrap, await fixture.BootstrapAsync(Token));
    }

    [Theory]
    [InlineData(LoginCommitFault.BeforeCommit, false)]
    [InlineData(LoginCommitFault.AfterCommit, true)]
    public async Task ActualProductLoginCommitRefusalNeverAcknowledgesSuccessAndExplicitRetryKeepsOneMapping(
        LoginCommitFault fault, bool committed)
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        using var request = fixture.ProductLoginRequest("/identity", await fixture.ProductContextAsync(Token));
        using var start = await fixture.SendProductAsync(request, Token);
        Assert.Equal(HttpStatusCode.SeeOther, start.StatusCode);
        var before = await fixture.SnapshotAsync(Token);
        fixture.SetProductLoginFault(fault);
        var elapsed = Stopwatch.StartNew();
        using (var unavailable = await fixture.CompleteProductAsync(start, Token))
            await RequireUnavailableAsync(unavailable);
        Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(5));
        fixture.SetProductLoginFault(LoginCommitFault.None);
        var refused = await fixture.SnapshotAsync(Token);
        Assert.Equal(committed ? 1 : 0, refused.Users.Count);
        Assert.Equal(committed ? 1 : 0, refused.Mappings.Count);
        Assert.Equal(committed ? 1 : 0, refused.Sessions.Count);
        Assert.Equal(committed ? "consumed" : "pending", Assert.Single(refused.Attempts).State);
        Assert.Equal(before.BootstrapState, refused.BootstrapState);
        using (var anonymousRequest = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me"))
        using (var anonymous = await fixture.SendProductAsync(anonymousRequest, Token))
            Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        await LoginAsync(fixture);
        var corrected = await fixture.SnapshotAsync(Token);
        Assert.Single(corrected.Users);
        Assert.Single(corrected.Mappings);
        Assert.Single(corrected.Events, row => row.Code == "user_created");
        Assert.Equal(committed ? 2 : 1, corrected.Events.Count(row => row.Code == "session_created"));
        Assert.Equal(before.BootstrapState, corrected.BootstrapState);
        Assert.DoesNotContain(corrected.Events, row => row.Code == "initial_administrator_enrolled");
        if (committed) Assert.Equal(Assert.Single(refused.Users).Id, Assert.Single(corrected.Users).Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LostStoreAuthorityOrCorruptTicketCannotServeAWarmedIdentityAndNeedsExplicitCorrection(
        bool corruptTicket)
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        await LoginAsync(fixture);
        foreach (var path in new[] { "/api/identity/v1/me", "/fixture-actor" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            using var response = await fixture.SendProductAsync(request, Token);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        var original = await fixture.SnapshotAsync(Token);
        var user = Assert.Single(original.Users);
        if (corruptTicket) await fixture.CorruptCurrentTicketAsync(Token);
        else await fixture.LoseProductStoreAuthorityAsync();
        var damaged = await fixture.SnapshotAsync(Token);
        foreach (var path in new[] { "/api/identity/v1/me", "/fixture-actor" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            var elapsed = Stopwatch.StartNew();
            using var unavailable = await fixture.SendProductAsync(request, Token);
            await RequireUnavailableAsync(unavailable);
            Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(2));
        }
        var refused = await fixture.SnapshotAsync(Token);
        Assert.Equal(damaged.Users, refused.Users);
        Assert.Equal(damaged.Mappings, refused.Mappings);
        Assert.Equal(damaged.Events, refused.Events);
        Assert.Equal(Assert.Single(damaged.Sessions).ProtectedTicket,
            Assert.Single(refused.Sessions).ProtectedTicket);
        if (corruptTicket) await fixture.RevokeProductSessionsAsync(Token);
        else Assert.True(await fixture.RestartProductAsync(Token) > 0);
        await LoginAsync(fixture);
        using var currentRequest = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
        using var current = await fixture.SendProductAsync(currentRequest, Token);
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        using var body = JsonDocument.Parse(await current.Content.ReadAsByteArrayAsync(Token));
        Assert.Equal(user.Id, body.RootElement.GetProperty("userId").GetGuid());
        Assert.False(body.RootElement.GetProperty("isInitialAdministrator").GetBoolean());
        var corrected = await fixture.SnapshotAsync(Token);
        Assert.Equal(user.Id, Assert.Single(corrected.Users).Id);
        Assert.Equal(original.Mappings, corrected.Mappings);
        Assert.Equal("disabled", corrected.BootstrapState);
        Assert.Single(corrected.Events, value => value.Code == "user_created");
    }

    [Fact]
    public async Task FailedRequiredSessionRecordRollsBackTheActualLoginAndExplicitCorrectionCanRetry()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var csrf = await fixture.ProductContextAsync(Token);
        using var request = fixture.ProductLoginRequest("/identity", csrf);
        using var start = await fixture.SendProductAsync(request, Token);
        Assert.Equal(303, (int)start.StatusCode);
        var before = await fixture.SnapshotAsync(Token);
        await fixture.SetIdentityRecordingFailureAsync("session_created", true, Token);
        try
        {
            var elapsed = Stopwatch.StartNew();
            using var refused = await fixture.CompleteProductAsync(start, Token);
            await RequireUnavailableAsync(refused);
            Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(5));
        }
        finally { await fixture.SetIdentityRecordingFailureAsync("session_created", false, Token); }
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(before.Sessions, after.Sessions);
        Assert.Equal(before.Events, after.Events);
        Assert.Equal(before.Attempts, after.Attempts);
        using (var anonymous = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me"))
        using (var response = await fixture.SendProductAsync(anonymous, Token))
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        await LoginAsync(fixture);
        var committed = await fixture.SnapshotAsync(Token);
        var user = Assert.Single(committed.Users);
        Assert.Equal(user.Id, Assert.Single(committed.Mappings).UserId);
        Assert.Equal(user.Id, Assert.Single(committed.Sessions).UserId);
        Assert.Single(committed.Events, value => value.Code == "user_created");
        Assert.Single(committed.Events, value => value.Code == "session_created");
        Assert.Equal("disabled", committed.BootstrapState);
        Assert.Equal(2, fixture.Protocol.TokenRequests);
    }

    [Fact]
    public async Task FailedRequiredEnrollmentRecordCannotConsumeTheNominationOrDesignateAnAdministrator()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        await LoginAsync(fixture);
        await fixture.SeedPendingBootstrapAsync(fixture.Protocol.Subject, false, Token);
        var csrf = await fixture.ProductContextAsync(Token);
        var before = await fixture.SnapshotAsync(Token);
        var bootstrap = await fixture.BootstrapAsync(Token);
        await fixture.SetIdentityRecordingFailureAsync("initial_administrator_enrolled", true, Token);
        try
        {
            using var request = Enrollment(fixture, csrf);
            var elapsed = Stopwatch.StartNew();
            using var refused = await fixture.SendProductAsync(request, Token);
            await RequireUnavailableAsync(refused);
            Assert.True(elapsed.Elapsed < TimeSpan.FromSeconds(2));
        }
        finally { await fixture.SetIdentityRecordingFailureAsync("initial_administrator_enrolled", false, Token); }
        Assert.Equal(bootstrap, await fixture.BootstrapAsync(Token));
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(before.Events, after.Events);
        Assert.Equal(before.Sessions.Select(value => (value.Digest, value.UserId, value.Epoch, value.State)),
            after.Sessions.Select(value => (value.Digest, value.UserId, value.Epoch, value.State)));
        using (var request = Enrollment(fixture, csrf))
        using (var corrected = await fixture.SendProductAsync(request, Token))
            Assert.Equal(HttpStatusCode.OK, corrected.StatusCode);
        var consumed = await fixture.BootstrapAsync(Token);
        Assert.Equal("consumed", consumed.State);
        Assert.Equal(bootstrap.Generation, consumed.ConsumedGeneration);
        Assert.Equal(Assert.Single(before.Users).Id, consumed.InitialUser);
        Assert.Single((await fixture.SnapshotAsync(Token)).Events,
            value => value.Code == "initial_administrator_enrolled");
        using var replay = Enrollment(fixture, csrf);
        using var rejected = await fixture.SendProductAsync(replay, Token);
        Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
        Assert.Equal(consumed, await fixture.BootstrapAsync(Token));
    }

    private async Task LoginAsync(LoginCommitFixture fixture)
    {
        var csrf = await fixture.ProductContextAsync(Token);
        using var request = fixture.ProductLoginRequest("/identity", csrf);
        using var start = await fixture.SendProductAsync(request, Token);
        Assert.Equal(303, (int)start.StatusCode);
        using var response = await fixture.CompleteProductAsync(start, Token);
        Assert.Equal(303, (int)response.StatusCode);
    }

    private static HttpRequestMessage Enrollment(LoginCommitFixture fixture, string csrf, string? authorization = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/first-administrator")
        {
            Content = JsonContent.Create(new { authorization = authorization ?? new string('A', 43) })
        };
        request.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
        request.Headers.Add("X-Kofte-CSRF", csrf);
        return request;
    }

    private async Task RequireUnavailableAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("no-store", response.Headers.CacheControl!.ToString());
        Assert.Equal("no-referrer", Assert.Single(response.Headers.GetValues("Referrer-Policy")));
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
            Assert.DoesNotContain(cookies, value => value.StartsWith("__Host-kofte-session=", StringComparison.Ordinal));
        using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
        Assert.Equal("identity_unavailable", body.RootElement.GetProperty("code").GetString());
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteIdentityFailureTests : IdentityFailureTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresIdentityFailureTests : IdentityFailureTests
{
    protected override string Provider => "postgres";
}
