using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Storage.IntegrationTests.Identity;

public abstract class IdentityAttemptTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;
    private static readonly DateTime Now = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly string Browser = new('b', 64);

    [Fact]
    public async Task ConcurrentBrowserStartsAdmitExactlyEightIndependentBoundAttempts()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var contenders = Enumerable.Range(0, 12).Select(_ => Task.Run(async () =>
        {
            await start.Task;
            try { return await StartAsync(fixture, Now); }
            catch (IdentityAdmissionException) { return null; }
        }, Token)).ToArray();
        start.SetResult();
        var attempts = (await Task.WhenAll(contenders)).OfType<IdentityLoginAttempt>().ToArray();
        Assert.Equal(8, attempts.Length);
        Assert.Equal(8, attempts.Select(attempt => attempt.AttemptId).Distinct().Count());
        var metadata = await fixture.MetadataAsync();
        Assert.Equal(8, metadata.Revision);
        Assert.All(attempts, attempt =>
        {
            Assert.NotEqual(Guid.Empty, attempt.AttemptId);
            Assert.Equal(Browser, attempt.BrowserDigest);
            Assert.Equal("oidc", attempt.Mode);
            Assert.Equal(new string('a', 64), attempt.TrustDigest);
            Assert.Equal(metadata.Epoch, attempt.Epoch);
            Assert.Equal("/identity", attempt.ReturnPath);
            Assert.Equal(Now, attempt.CreatedAt);
            Assert.Equal(Now.AddMinutes(5), attempt.ExpiresAt);
        });
        Assert.Equal(8L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_login_attempts WHERE state='pending'"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_users"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_sessions"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_events"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT registry_revision FROM {0}__kofte_installation"));
    }

    [Fact]
    public async Task InstallationCapacityAndExpiredCleanupHaveExactIndependentBounds()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var metadata = await fixture.MetadataAsync();
        await fixture.ExecuteAsync(Provider == "sqlite" ? """
            WITH RECURSIVE n(value) AS (SELECT 1 UNION ALL SELECT value+1 FROM n WHERE value<255)
            INSERT INTO __kofte_login_attempts
            (attempt_id,browser_digest,mode,trust_digest,session_epoch,return_path,created_at_utc,expires_at_utc,state)
            SELECT printf('10000000-0000-0000-0000-%012x',value),printf('%064x',value),'oidc',
                   @trust,@epoch,'/',@now,@expiry,'pending' FROM n
            """ : """
            INSERT INTO kofte_system.__kofte_login_attempts
            (attempt_id,browser_digest,mode,trust_digest,session_epoch,return_path,created_at_utc,expires_at_utc,state)
            SELECT ('10000000-0000-0000-0000-' || lpad(to_hex(value),12,'0'))::uuid,
                   lpad(to_hex(value),64,'0'),'oidc',@trust,@epoch,'/',@now,@expiry,'pending'
            FROM generate_series(1,255) AS n(value)
            """, ("trust", metadata.TrustDigest), ("epoch", metadata.Epoch), ("now", Now), ("expiry", Now.AddMinutes(5)));
        await StartAsync(fixture, Now);
        var refused = await Assert.ThrowsAsync<IdentityAdmissionException>(() =>
            StartAsync(fixture, Now, new string('c', 64)));
        Assert.Null(refused.RetryAt);
        Assert.Equal(256L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_login_attempts"));
        Assert.Equal(1, (await fixture.MetadataAsync()).Revision);
        var fresh = await StartAsync(fixture, Now.AddMinutes(5));
        Assert.Equal(Now.AddMinutes(10), fresh.ExpiresAt);
        Assert.Equal(129L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_login_attempts"));
        Assert.Equal(1L, await fixture.ScalarAsync(
            "SELECT COUNT(*) FROM {0}__kofte_login_attempts WHERE expires_at_utc>@now", ("now", Now.AddMinutes(5))));
        Assert.Equal(2, (await fixture.MetadataAsync()).Revision);
    }

    [Fact]
    public async Task ExactBindingAndPendingStateConsumeOnceInsideTheOwnerMutation()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var attempt = await StartAsync(fixture, Now, returnPath: "/");
        await Assert.ThrowsAsync<IdentityAttemptException>(() =>
            ConsumeAsync(fixture, attempt.AttemptId, Now.AddSeconds(1), new string('c', 64)));
        await Assert.ThrowsAsync<IdentityAttemptException>(() =>
            ConsumeAsync(fixture, Guid.NewGuid(), Now.AddSeconds(1)));
        Assert.Equal(1, (await fixture.MetadataAsync()).Revision);
        var consumed = await ConsumeAsync(fixture, attempt.AttemptId, Now.AddMinutes(5).AddMilliseconds(-1));
        Assert.Equal(attempt, consumed);
        Assert.Equal("consumed", await fixture.ScalarAsync(
            "SELECT state FROM {0}__kofte_login_attempts WHERE attempt_id=@id", ("id", attempt.AttemptId)));
        await Assert.ThrowsAsync<IdentityAttemptException>(() =>
            ConsumeAsync(fixture, attempt.AttemptId, Now.AddMinutes(5).AddMilliseconds(-1)));
        Assert.Equal(2, (await fixture.MetadataAsync()).Revision);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(300_000)]
    [InlineData(300_001)]
    public async Task LocalLifetimeRejectsBeforeCreationAndAtTheExactFiveMinuteExpiry(int milliseconds)
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var attempt = await StartAsync(fixture, Now);
        await Assert.ThrowsAsync<IdentityAttemptException>(() =>
            ConsumeAsync(fixture, attempt.AttemptId, Now.AddMilliseconds(milliseconds)));
        Assert.Equal("pending", await fixture.ScalarAsync("SELECT state FROM {0}__kofte_login_attempts"));
        Assert.Equal(1, (await fixture.MetadataAsync()).Revision);
    }

    [Fact]
    public async Task LaterOwnerFailureRollsBackConsumptionMappingCreationAndRequiredEvidenceTogether()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var attempt = await StartAsync(fixture, Now);
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        var failure = await Assert.ThrowsAsync<StorageException>(() => fixture.Store().MutateAsync<bool>(budget, async change =>
        {
            await IdentityUserStore.ResolveAsync(change, "https://issuer.invalid", "subject", null, Now);
            await IdentityLoginAttempts.ConsumeAsync(change, attempt.AttemptId, Browser, Now);
            throw new StorageException(StorageCode.Unavailable);
        }));
        Assert.Equal(StorageCode.Unavailable, failure.Code);
        Assert.Equal("pending", await fixture.ScalarAsync("SELECT state FROM {0}__kofte_login_attempts"));
        Assert.Null(await fixture.ScalarAsync("SELECT consumed_at_utc FROM {0}__kofte_login_attempts"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_users"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_external_identities"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_events"));
        Assert.Equal(1, (await fixture.MetadataAsync()).Revision);
    }

    [Theory]
    [InlineData("epoch")]
    [InlineData("trust")]
    [InlineData("mode")]
    public async Task ChangedSecurityContextCannotConsumeAPriorAttempt(string field)
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var attempt = await StartAsync(fixture, Now);
        var mode = field == "mode" ? "development" : "oidc";
        var digest = field == "trust" ? new string('c', 64) : new string('a', 64);
        await fixture.ExecuteAsync("""
            UPDATE {0}__kofte_identity_metadata SET session_epoch=@epoch,mode=@mode,trust_digest=@trust
            """, ("epoch", field == "epoch" ? Guid.NewGuid() : attempt.Epoch), ("mode", mode), ("trust", digest));
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        await Assert.ThrowsAsync<IdentityAttemptException>(() =>
            fixture.Store(mode: mode, digest: digest).MutateAsync(budget, change =>
                IdentityLoginAttempts.ConsumeAsync(change, attempt.AttemptId, Browser, Now)));
        Assert.Equal("pending", await fixture.ScalarAsync("SELECT state FROM {0}__kofte_login_attempts"));
        Assert.Equal(1, (await fixture.MetadataAsync()).Revision);
    }

    [Fact]
    public async Task ConcurrentConsumptionCommitsExactlyOnceAndNextStartPrunesTheConsumedRow()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var attempt = await StartAsync(fixture, Now);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var contenders = Enumerable.Range(0, 12).Select(_ => Task.Run(async () =>
        {
            await start.Task;
            try { return await ConsumeAsync(fixture, attempt.AttemptId, Now.AddSeconds(1)); }
            catch (IdentityAttemptException) { return null; }
        }, Token)).ToArray();
        start.SetResult();
        Assert.Equal(attempt, Assert.Single((await Task.WhenAll(contenders)).OfType<IdentityLoginAttempt>()));
        Assert.Equal(2, (await fixture.MetadataAsync()).Revision);
        var next = await StartAsync(fixture, Now.AddSeconds(2));
        Assert.NotEqual(attempt.AttemptId, next.AttemptId);
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_login_attempts"));
        Assert.Equal(next.AttemptId.ToString("D"), Convert.ToString(await fixture.ScalarAsync(
            "SELECT attempt_id FROM {0}__kofte_login_attempts"), System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(3, (await fixture.MetadataAsync()).Revision);
    }

    [Fact]
    public async Task InvalidReturnAndRawOrMalformedBindingsLeaveNoAttemptOrRevision()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        foreach (var path in new[] { "", "//outside.invalid", "https://outside.invalid", "/identity?next=/", "/%69dentity" })
        {
            var failure = await Assert.ThrowsAsync<StorageException>(() => StartAsync(fixture, Now, returnPath: path));
            Assert.Equal(StorageCode.RequestInvalid, failure.Code);
        }
        foreach (var binding in new[] { "raw-browser-secret", new string('B', 64), new string('b', 63) })
        {
            var failure = await Assert.ThrowsAsync<StorageException>(() => StartAsync(fixture, Now, binding));
            Assert.Equal(StorageCode.RequestInvalid, failure.Code);
        }
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_login_attempts"));
        Assert.Equal(0, (await fixture.MetadataAsync()).Revision);
    }

    private async Task<IdentityLoginAttempt> StartAsync(AdmissionFixture fixture, DateTime now,
        string? browser = null, string returnPath = "/identity")
    {
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        return await fixture.Store().MutateAsync(budget, change =>
            IdentityLoginAttempts.StartAsync(change, browser ?? Browser, returnPath, now));
    }

    private async Task<IdentityLoginAttempt> ConsumeAsync(AdmissionFixture fixture, Guid id, DateTime now, string? browser = null)
    {
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        return await fixture.Store().MutateAsync(budget, change =>
            IdentityLoginAttempts.ConsumeAsync(change, id, browser ?? Browser, now));
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteIdentityAttemptTests : IdentityAttemptTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresIdentityAttemptTests : IdentityAttemptTests
{
    protected override string Provider => "postgres";
}
