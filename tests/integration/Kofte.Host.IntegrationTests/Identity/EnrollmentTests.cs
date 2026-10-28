using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

public abstract class EnrollmentTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task OrdinaryProductionLoginCannotDesignateAndProtectedEnrollmentNeedsAnOperatorCeremony()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        await LoginAsync(fixture);
        var before = await fixture.SnapshotAsync(Token);
        Assert.Equal("disabled", before.BootstrapState);
        var csrf = await fixture.ProductContextAsync(Token);
        using var request = Enrollment(fixture, csrf);
        using var response = await fixture.SendProductAsync(request, Token);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await RequireProblemAsync(response, "identity_enrollment_unavailable");
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.BootstrapState, after.BootstrapState);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(before.Events, after.Events);
        using var currentRequest = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
        using var current = await fixture.SendProductAsync(currentRequest, Token);
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        using var identity = JsonDocument.Parse(await current.Content.ReadAsByteArrayAsync(Token));
        Assert.False(identity.RootElement.GetProperty("isInitialAdministrator").GetBoolean());
    }

    [Theory]
    [InlineData("anonymous", 401)]
    [InlineData("origin", 403)]
    [InlineData("csrf", 403)]
    [InlineData("unknown", 400)]
    [InlineData("mixed", 401)]
    public async Task EnrollmentRefusesMissingIdentityForgeryAndCallerChosenAuthorityBeforeEffects(
        string fault, int status)
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        if (fault != "anonymous") await LoginAsync(fixture);
        var csrf = await fixture.ProductContextAsync(Token);
        var before = await fixture.SnapshotAsync(Token);
        using var request = Enrollment(fixture, fault == "csrf" ? "invalid" : csrf);
        if (fault == "origin")
        {
            request.Headers.Remove("Origin");
            request.Headers.Add("Origin", "null");
        }
        if (fault == "mixed") request.Headers.Add("Authorization", "Bearer synthetic-unsupported");
        if (fault == "unknown")
            request.Content = JsonContent.Create(new { authorization = new string('A', 43), administrator = true });
        using var response = await fixture.SendProductAsync(request, Token);
        Assert.Equal(status, (int)response.StatusCode);
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.BootstrapState, after.BootstrapState);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(before.Events, after.Events);
    }

    [Theory]
    [InlineData("pending", 200)]
    [InlineData("wrong-nominee", 403)]
    [InlineData("wrong-secret", 409)]
    [InlineData("expired", 409)]
    [InlineData("consumed", 409)]
    [InlineData("recovery-locked", 409)]
    [InlineData("caller-mode", 400)]
    public async Task EnrollmentChecksCurrentCeremonyAndExactNomineeWithoutReopeningHistory(string state, int status)
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        await LoginAsync(fixture);
        if (state == "consumed") await fixture.SeedCurrentProfileAndDesignationAsync(Token);
        else if (state == "recovery-locked") await fixture.SeedRecoveryLockedBootstrapAsync(Token);
        else await fixture.SeedPendingBootstrapAsync(state == "wrong-nominee"
            ? "synthetic-other-nominee" : fixture.Protocol.Subject, state == "expired", Token);
        var csrf = await fixture.ProductContextAsync(Token);
        var before = await fixture.SnapshotAsync(Token);
        using var request = Enrollment(fixture, csrf);
        if (state == "wrong-secret")
            request.Content = JsonContent.Create(new { authorization = new string('Q', 43) });
        if (state == "caller-mode")
            request.Content = JsonContent.Create(new { authorization = new string('A', 43), identityMode = "development" });
        using var response = await fixture.SendProductAsync(request, Token);
        Assert.Equal(status, (int)response.StatusCode);
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        if (status == 200)
        {
            Assert.Equal("consumed", after.BootstrapState);
            var committed = Assert.Single(after.Events, value => !before.Events.Contains(value));
            Assert.Equal("initial_administrator_enrolled", committed.Code);
            Assert.Equal(Assert.Single(after.Users).Id, committed.UserId);
            using var currentRequest = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
            using var current = await fixture.SendProductAsync(currentRequest, Token);
            using var identity = JsonDocument.Parse(await current.Content.ReadAsByteArrayAsync(Token));
            Assert.True(identity.RootElement.GetProperty("isInitialAdministrator").GetBoolean());
            using var replay = Enrollment(fixture, await fixture.ProductContextAsync(Token));
            using var replayResponse = await fixture.SendProductAsync(replay, Token);
            Assert.Equal(HttpStatusCode.Conflict, replayResponse.StatusCode);
            Assert.Equal(after.Events, (await fixture.SnapshotAsync(Token)).Events);
        }
        else
        {
            Assert.Equal(before.BootstrapState, after.BootstrapState);
            Assert.Equal(before.Events, after.Events);
        }
    }

    [Fact]
    public async Task ActualStoppedReplacementRefusesTheOriginalSecretAndConsumesOnlyTheNewGeneration()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var original = await fixture.NominateProductAsync(Token);
        await LoginAsync(fixture);
        var replacement = await fixture.NominateProductAsync(Token);
        Assert.NotEqual(original, replacement);
        var pending = await fixture.BootstrapAsync(Token);
        Assert.Equal(2, pending.Generation);
        var csrf = await fixture.ProductContextAsync(Token);
        var before = await fixture.SnapshotAsync(Token);
        using (var request = Enrollment(fixture, csrf))
        {
            request.Content = JsonContent.Create(new { authorization = original });
            using var refused = await fixture.SendProductAsync(request, Token);
            Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
            await RequireProblemAsync(refused, "identity_enrollment_unavailable");
        }
        Assert.Equal(pending, await fixture.BootstrapAsync(Token));
        Assert.Equal(before.Events, (await fixture.SnapshotAsync(Token)).Events);
        using var corrected = Enrollment(fixture, csrf);
        corrected.Content = JsonContent.Create(new { authorization = replacement });
        using var response = await fixture.SendProductAsync(corrected, Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var consumed = await fixture.BootstrapAsync(Token);
        Assert.Equal("consumed", consumed.State);
        Assert.Equal(pending.Generation, consumed.ConsumedGeneration);
        Assert.Equal(Assert.Single(before.Users).Id, consumed.InitialUser);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnavailableVerificationOrEnrollmentCommitReturns503WithoutDesignation(bool corruptTicket)
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        await LoginAsync(fixture);
        await fixture.SeedPendingBootstrapAsync(fixture.Protocol.Subject, false, Token);
        var csrf = await fixture.ProductContextAsync(Token);
        if (corruptTicket) await fixture.CorruptCurrentTicketAsync(Token);
        else fixture.FailEnrollmentBeforeCommit();
        var before = await fixture.SnapshotAsync(Token);
        var bootstrap = await fixture.BootstrapAsync(Token);
        using var request = Enrollment(fixture, csrf);
        using var response = await fixture.SendProductAsync(request, Token);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        await RequireProblemAsync(response, "identity_unavailable");
        Assert.Equal(bootstrap, await fixture.BootstrapAsync(Token));
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(before.Events, after.Events);
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

    private static HttpRequestMessage Enrollment(LoginCommitFixture fixture, string csrf)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/first-administrator")
        {
            Content = JsonContent.Create(new { authorization = new string('A', 43) })
        };
        request.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
        request.Headers.Add("X-Kofte-CSRF", csrf);
        return request;
    }

    private async Task RequireProblemAsync(HttpResponseMessage response, string code)
    {
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString(), StringComparison.Ordinal);
        using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
        Assert.Equal(code, body.RootElement.GetProperty("code").GetString());
        Assert.Equal(new[] { "code", "correlationId", "status", "title", "type" },
            body.RootElement.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteEnrollmentTests : EnrollmentTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresEnrollmentTests : EnrollmentTests
{
    protected override string Provider => "postgres";
}
