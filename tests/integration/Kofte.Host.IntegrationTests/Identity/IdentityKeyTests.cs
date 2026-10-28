using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.TestSupport;
using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

public abstract class IdentityKeyTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("lost")]
    [InlineData("foreign")]
    [InlineData("undecryptable")]
    public async Task KeyFailureCannotRepairItselfOrReplaceConsumedAuthority(string fault)
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var user = await EnrollAsync(fixture);
        var before = await fixture.SnapshotAsync(Token);
        var bootstrap = await fixture.BootstrapAsync(Token);
        var custody = fixture.KeyRingFingerprint;
        fixture.DamageProductKeys(fault);
        var damaged = fixture.KeyRingFingerprint;
        using (var response = await CurrentAsync(fixture))
        {
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            using var problem = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
            Assert.Equal("identity_unavailable", problem.RootElement.GetProperty("code").GetString());
            Assert.False(response.Headers.TryGetValues("Set-Cookie", out _));
        }
        Assert.Equal(damaged, fixture.KeyRingFingerprint);
        await PreserveAuthorityAsync(fixture, before, bootstrap);
        await fixture.RepairProductKeysFromRetainedCustodyAsync(Token);
        Assert.Equal(custody, fixture.KeyRingFingerprint);
        await RequireActorAsync(fixture, user);
        await PreserveAuthorityAsync(fixture, before, bootstrap);
        await fixture.RevokeProductSessionsAsync(Token);
        using var revoked = await CurrentAsync(fixture);
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        var after = await fixture.SnapshotAsync(Token);
        Assert.NotEqual(before.Epoch, after.Epoch);
        Assert.All(after.Sessions, session => Assert.Equal("revoked", session.State));
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(bootstrap, await fixture.BootstrapAsync(Token));
    }

    [Fact]
    public async Task ActualCertificateOverlapAndRetainedKeyRotationKeepTheConsumedActorAcrossRestart()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var user = await EnrollAsync(fixture);
        var before = await fixture.SnapshotAsync(Token);
        var bootstrap = await fixture.BootstrapAsync(Token);
        Assert.Equal(1, fixture.RetainedKeyCount);
        await fixture.RotateProductCertificateAsync(Token);
        await RequireActorAsync(fixture, user);
        Assert.Equal(2, fixture.RetainedKeyCount);
        var keys = fixture.KeyRingFingerprint;
        await fixture.RestartProductAsync(Token);
        await RequireActorAsync(fixture, user);
        Assert.Equal(keys, fixture.KeyRingFingerprint);
        await PreserveAuthorityAsync(fixture, before, bootstrap);
    }

    [Fact]
    public async Task ClientSecretCorrectionChangesFutureExchangeWithoutResettingSessionsOrBootstrap()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var user = await EnrollAsync(fixture);
        var before = await fixture.SnapshotAsync(Token);
        var bootstrap = await fixture.BootstrapAsync(Token);
        var keys = fixture.KeyRingFingerprint;
        fixture.Protocol.ReplaceClientSecret();
        await RequireActorAsync(fixture, user);
        var exchanges = fixture.Protocol.TokenRequests;
        using (var rejected = await LoginAsync(fixture))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
            Assert.False(rejected.Headers.TryGetValues("Set-Cookie", out var cookies) &&
                cookies.Any(value => value.StartsWith("__Host-kofte-session=", StringComparison.Ordinal)));
        }
        Assert.Equal(exchanges + 1, fixture.Protocol.TokenRequests);
        await RequireActorAsync(fixture, user);
        await fixture.ReloadProductClientSecretAsync(Token);
        await RequireActorAsync(fixture, user);
        using (var accepted = await LoginAsync(fixture)) Assert.Equal(303, (int)accepted.StatusCode);
        await RequireActorAsync(fixture, user);
        Assert.Equal(keys, fixture.KeyRingFingerprint);
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Epoch, after.Epoch);
        Assert.Equal(before.Users.Select(value => (value.Id, value.CreatedAt)),
            after.Users.Select(value => (value.Id, value.CreatedAt)));
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(bootstrap, await fixture.BootstrapAsync(Token));
    }

    [Fact]
    public async Task ActualDatabaseRecoverySetDoesNotContainSeparatelyRetainedKeyCustody()
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(Provider, Token);
        var user = await EnrollAsync(fixture);
        var before = await fixture.SnapshotAsync(Token);
        var bootstrap = await fixture.BootstrapAsync(Token);
        var keys = fixture.KeyRingFingerprint;
        using var backup = new OwnedTestRoot();
        var manifest = await fixture.BackupProductAsync(backup.Path, Token);
        Assert.Equal(RecoveryManifest.IdentityFormat, manifest.Format);
        Assert.Equal("consumed", manifest.Identity!.BootstrapState);
        Assert.Equal(before.Epoch, manifest.Identity.SessionEpoch);
        Assert.Equal(manifest.Artifacts.Select(value => value.Name).Append(RecoveryManifest.FileName).Order(),
            Directory.GetFiles(backup.Path, "*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(backup.Path, file)).Order());
        Assert.DoesNotContain(manifest.Artifacts, value =>
            value.Name.EndsWith(".pem", StringComparison.Ordinal) ||
            value.Name.EndsWith(".xml", StringComparison.Ordinal) ||
            value.Name == "identity-owner.json");
        Assert.Equal(keys, fixture.KeyRingFingerprint);
        await RequireActorAsync(fixture, user);
        await PreserveAuthorityAsync(fixture, before, bootstrap);
    }

    private async Task<Guid> EnrollAsync(LoginCommitFixture fixture)
    {
        using (var response = await LoginAsync(fixture)) Assert.Equal(303, (int)response.StatusCode);
        var authorization = await fixture.NominateProductAsync(Token);
        var csrf = await fixture.ProductContextAsync(Token);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/first-administrator")
        { Content = JsonContent.Create(new { authorization }) };
        request.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
        request.Headers.Add("X-Kofte-CSRF", csrf);
        using var enrolled = await fixture.SendProductAsync(request, Token);
        Assert.Equal(HttpStatusCode.OK, enrolled.StatusCode);
        return Assert.Single((await fixture.SnapshotAsync(Token)).Users).Id;
    }

    private async Task<HttpResponseMessage> LoginAsync(LoginCommitFixture fixture)
    {
        var csrf = await fixture.ProductContextAsync(Token);
        using var request = fixture.ProductLoginRequest("/identity", csrf);
        using var start = await fixture.SendProductAsync(request, Token);
        Assert.Equal(303, (int)start.StatusCode);
        return await fixture.CompleteProductAsync(start, Token);
    }

    private async Task<HttpResponseMessage> CurrentAsync(LoginCommitFixture fixture)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
        return await fixture.SendProductAsync(request, Token);
    }

    private async Task RequireActorAsync(LoginCommitFixture fixture, Guid user)
    {
        using var response = await CurrentAsync(fixture);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(Token));
        Assert.Equal(user, body.RootElement.GetProperty("userId").GetGuid());
        Assert.True(body.RootElement.GetProperty("isInitialAdministrator").GetBoolean());
    }

    private async Task PreserveAuthorityAsync(LoginCommitFixture fixture, LoginCommitSnapshot before,
        LoginBootstrapObservation bootstrap)
    {
        var after = await fixture.SnapshotAsync(Token);
        Assert.Equal(before.Epoch, after.Epoch);
        Assert.Equal(before.Users, after.Users);
        Assert.Equal(before.Mappings, after.Mappings);
        Assert.Equal(bootstrap, await fixture.BootstrapAsync(Token));
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteIdentityKeyTests : IdentityKeyTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresIdentityKeyTests : IdentityKeyTests
{
    protected override string Provider => "postgres";
}
