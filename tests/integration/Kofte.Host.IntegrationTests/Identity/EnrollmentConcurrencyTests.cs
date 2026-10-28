using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

public abstract class EnrollmentConcurrencyTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task TwentyEligibleHttpContendersCommitExactlyOneNomineeGenerationAndImmutableEvent()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var authorization = await fixture.NominateProductAsync(Token);
        var anonymous = await fixture.ProductContextAsync(Token);
        using var login = fixture.ProductLoginRequest("/identity", anonymous);
        using var start = await fixture.SendProductAsync(login, Token);
        Assert.Equal(303, (int)start.StatusCode);
        using var callback = await fixture.CompleteProductAsync(start, Token);
        Assert.Equal(303, (int)callback.StatusCode);
        var csrf = await fixture.ProductContextAsync(Token);
        var before = await fixture.SnapshotAsync(Token);
        var pending = await fixture.BootstrapAsync(Token);
        Assert.Equal("pending", pending.State);
        Assert.Equal(1, pending.Generation);
        var user = Assert.Single(before.Users);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var contenders = Enumerable.Range(0, 20).Select(async _ =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/first-administrator")
            {
                Content = JsonContent.Create(new { authorization })
            };
            request.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
            request.Headers.Add("X-Kofte-CSRF", csrf);
            await release.Task;
            using var response = await fixture.SendProductAsync(request, Token);
            using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict);
            if (response.StatusCode == HttpStatusCode.Conflict)
                Assert.Equal("identity_enrollment_unavailable", body.RootElement.GetProperty("code").GetString());
            return (response.StatusCode, Correlation: body.RootElement.GetProperty("correlationId").GetString());
        }).ToArray();
        release.SetResult();
        var outcomes = await Task.WhenAll(contenders);
        var winner = Assert.Single(outcomes, outcome => outcome.StatusCode == HttpStatusCode.OK);
        Assert.Equal(19, outcomes.Count(outcome => outcome.StatusCode == HttpStatusCode.Conflict));
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(before.RegistryRevision, after.RegistryRevision);
        Assert.Equal(before.Epoch, after.Epoch);
        var committed = Assert.Single(after.Events, value => !before.Events.Contains(value));
        Assert.Equal("initial_administrator_enrolled", committed.Code);
        Assert.Equal(user.Id, committed.UserId);
        Assert.Equal(winner.Correlation, committed.Correlation);
        Assert.InRange(committed.Revision, before.Revision + 1, after.Revision);
        var consumed = await fixture.BootstrapAsync(Token);
        Assert.Equal("consumed", consumed.State);
        Assert.Equal(pending.InstallationId, consumed.InstallationId);
        Assert.Equal(pending.Generation, consumed.Generation);
        Assert.Equal(pending.Generation, consumed.ConsumedGeneration);
        Assert.Equal(user.Id, consumed.InitialUser);
        Assert.NotNull(consumed.ConsumedAt);
        Assert.Null(consumed.Issuer);
        Assert.Null(consumed.Subject);
        Assert.Null(consumed.Digest);
        Assert.Null(consumed.Expires);
        var processes = new HashSet<int>();
        for (var restart = 0; restart < 3; restart++)
        {
            Assert.True(processes.Add(await fixture.RestartProductAsync(Token)));
            using var currentRequest = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
            using var current = await fixture.SendProductAsync(currentRequest, Token);
            Assert.Equal(HttpStatusCode.OK, current.StatusCode);
            using var identity = JsonDocument.Parse(await current.Content.ReadAsByteArrayAsync(Token));
            Assert.Equal(user.Id, identity.RootElement.GetProperty("userId").GetGuid());
            Assert.True(identity.RootElement.GetProperty("isInitialAdministrator").GetBoolean());
            using var replay = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/first-administrator")
            {
                Content = JsonContent.Create(new { authorization })
            };
            replay.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
            replay.Headers.Add("X-Kofte-CSRF", await fixture.ProductContextAsync(Token));
            using var refused = await fixture.SendProductAsync(replay, Token);
            Assert.Equal(HttpStatusCode.Conflict, refused.StatusCode);
            Assert.Equal(consumed, await fixture.BootstrapAsync(Token));
            var unchanged = await fixture.SnapshotAsync(Token);
            Assert.Equal(after.Users, unchanged.Users);
            Assert.Equal(after.Mappings, unchanged.Mappings);
            Assert.Equal(after.Events, unchanged.Events);
            Assert.Equal(after.RegistryRevision, unchanged.RegistryRevision);
        }
        await fixture.PrepareProductAgainAsync(Token);
        Assert.Equal(consumed, await fixture.BootstrapAsync(Token));
        Assert.Equal(after.Events, (await fixture.SnapshotAsync(Token)).Events);
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteEnrollmentConcurrencyTests : EnrollmentConcurrencyTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresEnrollmentConcurrencyTests : EnrollmentConcurrencyTests
{
    protected override string Provider => "postgres";
}
