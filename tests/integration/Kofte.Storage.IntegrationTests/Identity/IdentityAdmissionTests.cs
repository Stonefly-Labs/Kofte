using System.Data.Common;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Postgres;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Registry;
using Npgsql;

namespace Kofte.Storage.IntegrationTests.Identity;

public abstract class IdentityAdmissionTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;
    private static readonly DateTime Now = new(2030, 1, 1, 12, 0, 15, DateTimeKind.Utc);
    private static readonly string Key = new('1', 64);

    [Fact]
    public async Task BrowserLimitPersistsAcrossFreshConnectionsAndPreservesTheEpochAndRegistry()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var before = await fixture.MetadataAsync();
        for (var index = 0; index < 10; index++)
            await fixture.AdmitAsync(Now, new IdentityAdmissionRequest(IdentityAdmissionCategory.LoginBrowser, Key));
        await Assert.ThrowsAsync<IdentityAdmissionException>(() =>
            fixture.AdmitAsync(Now, new IdentityAdmissionRequest(IdentityAdmissionCategory.LoginBrowser, Key)));
        var after = await fixture.MetadataAsync();
        Assert.Equal(before.InstallationId, after.InstallationId);
        Assert.Equal(before.Epoch, after.Epoch);
        Assert.NotEqual(Guid.Empty, after.Epoch);
        Assert.Equal("oidc", after.Mode);
        Assert.Equal(new string('a', 64), after.TrustDigest);
        Assert.Equal(10, after.Revision);
        Assert.Equal(10L, await fixture.ScalarAsync("SELECT count FROM {0}__kofte_identity_admission"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT registry_revision FROM {0}__kofte_installation"));
    }

    [Fact]
    public async Task PairedAdmissionRollsBackEarlierCountersWhenTheBrowserIsAlreadyAtItsLimit()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        for (var index = 0; index < 10; index++)
            await fixture.AdmitAsync(Now, new IdentityAdmissionRequest(IdentityAdmissionCategory.LoginBrowser, Key));
        await Assert.ThrowsAsync<IdentityAdmissionException>(() => fixture.AdmitAsync(Now,
            new IdentityAdmissionRequest(IdentityAdmissionCategory.LoginInstallation, new string('2', 64)),
            new IdentityAdmissionRequest(IdentityAdmissionCategory.LoginBrowser, Key)));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_admission"));
        Assert.Equal(10, (await fixture.MetadataAsync()).Revision);
    }

    [Fact]
    public async Task ConcurrentRequestsShareTheRealHostLeaseWithoutOverAdmittingTheWindow()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var admission = new IdentityAdmission(fixture.Store());
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var contenders = Enumerable.Range(0, 12).Select(_ => Task.Run(async () =>
        {
            await start.Task;
            using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
            try
            {
                await admission.AdmitAsync(Now, [new(IdentityAdmissionCategory.LoginBrowser, Key)], budget);
                return true;
            }
            catch (IdentityAdmissionException) { return false; }
        }, Token)).ToArray();
        start.SetResult();
        var outcomes = await Task.WhenAll(contenders);
        Assert.Equal(10, outcomes.Count(admitted => admitted));
        Assert.Equal(2, outcomes.Count(admitted => !admitted));
        Assert.Equal(10, (await fixture.MetadataAsync()).Revision);
        Assert.Equal(10L, await fixture.ScalarAsync("SELECT count FROM {0}__kofte_identity_admission"));
    }

    [Fact]
    public async Task Exactly4096LiveWindowsRefuseNewCapacityWithoutAnEffect()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        await fixture.SeedAsync(4096, expired: false, Now);
        await Assert.ThrowsAsync<IdentityAdmissionException>(() =>
            fixture.AdmitAsync(Now, new IdentityAdmissionRequest(IdentityAdmissionCategory.LoginBrowser, Key)));
        Assert.Equal(4096L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_admission"));
        Assert.Equal(0, (await fixture.MetadataAsync()).Revision);
    }

    [Fact]
    public async Task ExpiredWindowsArePrunedInOne128RowBatchAndTheNewWindowUsesUtc()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        await fixture.SeedAsync(257, expired: true, Now);
        await fixture.AdmitAsync(Now, new IdentityAdmissionRequest(IdentityAdmissionCategory.LoginBrowser, Key));
        Assert.Equal(130L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_admission"));
        Assert.Equal(1, (await fixture.MetadataAsync()).Revision);
        await fixture.AdmitAsync(Now.AddMinutes(1), new IdentityAdmissionRequest(IdentityAdmissionCategory.LoginBrowser, Key));
        Assert.Equal(3L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_admission"));
    }

    [Theory]
    [InlineData("installation")]
    [InlineData("mode")]
    [InlineData("trust")]
    public async Task UnacknowledgedIdentityChangesCannotMutateMetadataOrCounters(string changed)
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var store = fixture.Store(changed == "installation" ? Guid.NewGuid() : fixture.InstallationId,
            changed == "mode" ? "development" : "oidc",
            new string(changed == "trust" ? 'b' : 'a', 64));
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        var error = await Assert.ThrowsAsync<StorageException>(() => new IdentityAdmission(store)
            .AdmitAsync(Now, [new(IdentityAdmissionCategory.LoginBrowser, Key)], budget));
        Assert.Equal(StorageCode.ReconciliationRequired, error.Code);
        Assert.Equal(0, (await fixture.MetadataAsync()).Revision);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_admission"));
    }

    [Fact]
    public async Task RevisionOverflowRefusesTheMutationWithoutChangingAnyCounter()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        await fixture.ExecuteAsync("UPDATE {0}__kofte_identity_metadata SET security_revision=@maximum",
            ("maximum", long.MaxValue - 1));
        await fixture.AdmitAsync(Now, new IdentityAdmissionRequest(IdentityAdmissionCategory.LoginBrowser, Key));
        Assert.Equal(long.MaxValue, (await fixture.MetadataAsync()).Revision);
        await Assert.ThrowsAsync<OverflowException>(() =>
            fixture.AdmitAsync(Now, new IdentityAdmissionRequest(IdentityAdmissionCategory.LoginBrowser, Key)));
        Assert.Equal(long.MaxValue, (await fixture.MetadataAsync()).Revision);
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_admission"));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT count FROM {0}__kofte_identity_admission"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT registry_revision FROM {0}__kofte_installation"));
    }

    [Fact]
    public async Task RealConstraintFailureRollsBackEarlierRowsAndTheSecurityRevision()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        await Assert.ThrowsAnyAsync<DbException>(() => fixture.Store().MutateAsync(budget, async change =>
        {
            await change.Database.QueryAsync($"""
                INSERT INTO {change.Database.Prefix}__kofte_identity_admission
                (category,window_start_utc,key_digest,expires_at_utc,count)
                VALUES ('login_browser',@now,@key,@expiry,1)
                """, ("now", Now), ("key", Key), ("expiry", Now.AddMinutes(1)));
            await change.Database.QueryAsync($"""
                INSERT INTO {change.Database.Prefix}__kofte_identity_admission
                (category,window_start_utc,key_digest,expires_at_utc,count)
                VALUES ('not-an-admitted-category',@now,@key,@expiry,1)
                """, ("now", Now), ("key", Key), ("expiry", Now.AddMinutes(1)));
            return true;
        }));
        Assert.Equal(0, (await fixture.MetadataAsync()).Revision);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_admission"));
    }

    [Fact]
    public async Task AReleasedNormalHostLeaseCannotAuthorizeAnotherMutation()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        fixture.FileLease?.Dispose();
        if (fixture.HostLease is not null) await fixture.HostLease.DisposeAsync();
        var error = await Assert.ThrowsAsync<StorageException>(() =>
            fixture.AdmitAsync(Now, new IdentityAdmissionRequest(IdentityAdmissionCategory.LoginBrowser, Key)));
        Assert.Equal(StorageCode.Unavailable, error.Code);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_admission"));
    }

    [Fact]
    public async Task AnotherInstallationsNormalLeaseCannotAuthorizeThisStore()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        await using var other = await AdmissionFixture.CreateAsync(Provider, Token);
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        var error = await Assert.ThrowsAsync<StorageException>(() => new IdentityAdmission(fixture.StoreWithLease(other))
            .AdmitAsync(Now, [new(IdentityAdmissionCategory.LoginBrowser, Key)], budget));
        Assert.True(error.Code is StorageCode.Unavailable or StorageCode.OwnerMismatch);
        Assert.Equal(0, (await fixture.MetadataAsync()).Revision);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_admission"));
    }

    [Fact]
    public async Task RepeatedStoppedInitializationPreservesTheExactCommittedMetadata()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var before = await fixture.MetadataAsync();
        await fixture.InitializeAgainAsync();
        Assert.Equal(before, await fixture.MetadataAsync());
    }

    [Fact]
    public async Task AStoppedScopeCannotInitializeAnotherInstallationsConnection()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        await using var other = await AdmissionFixture.CreateAsync(Provider, Token);
        var before = await other.MetadataAsync();
        await Assert.ThrowsAsync<StorageException>(() => fixture.InitializeWithForeignConnectionAsync(other));
        Assert.Equal(before, await other.MetadataAsync());
    }
}

[Trait("Provider", "sqlite")]
[Collection("identity-deadlines")]
public sealed class SqliteIdentityAdmissionTests : IdentityAdmissionTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Collection("identity-deadlines")]
public sealed class PostgresIdentityAdmissionTests : IdentityAdmissionTests
{
    protected override string Provider => "postgres";

    [Fact]
    public async Task AnOpenConnectionWithoutItsSharedHostLockCannotAuthorizeAMutation()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await AdmissionFixture.CreateAsync("postgres", token);
        await using var command = new NpgsqlCommand(
            "SELECT pg_catalog.pg_advisory_unlock_shared($1)", fixture.HostLease);
        command.Parameters.AddWithValue(PostgresIdentity.LeaseKey);
        Assert.True(await command.ExecuteScalarAsync(token) is true);
        var error = await Assert.ThrowsAsync<StorageException>(() => fixture.AdmitAsync(
            new DateTime(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            new IdentityAdmissionRequest(IdentityAdmissionCategory.LoginBrowser, new string('1', 64))));
        Assert.Equal(StorageCode.Unavailable, error.Code);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_admission"));
    }
}

internal sealed class AdmissionFixture : IAsyncDisposable
{
    private readonly RegistryFixture fixture;
    private readonly CancellationToken token;
    internal Guid InstallationId { get; private set; }
    internal StoreLease? FileLease { get; private set; }
    internal NpgsqlConnection? HostLease { get; private set; }
    internal RegistryFixture RegistryFixture => fixture;

    private AdmissionFixture(RegistryFixture fixture, CancellationToken token)
        => (this.fixture, this.token) = (fixture, token);

    internal static async Task<AdmissionFixture> CreateAsync(string provider, CancellationToken token)
    {
        var owned = new AdmissionFixture(await RegistryFixture.CreateAsync(provider, token), token);
        try
        {
            using var budget = new OperationBudget(OperationBudget.Register, token);
            await using (var scope = await owned.fixture.Maintenance.AcquireAsync(token))
            await using (var connection = await owned.fixture.Installation.OpenPlatformAsync(budget))
            await using (var transaction = await connection.BeginTransactionAsync(token))
            {
                var runner = new RegistryMigrationRunner(owned.fixture.Installation);
                var stopped = Assert.IsType<MaintenanceScope>(scope);
                await runner.ApplyIdentityFormatAsync(stopped, connection, transaction, budget);
                owned.InstallationId = (await runner.ReadInstallationAsync(connection, budget, transaction)).Id;
                await IdentityStore.InitializeAsync(owned.fixture.Installation, stopped, connection, transaction,
                    "oidc", new string('a', 64), DateTime.UtcNow, budget);
                await transaction.CommitAsync(token);
            }
            await owned.AcquireRuntimeLeaseAsync();
            return owned;
        }
        catch { await owned.DisposeAsync(); throw; }
    }

    internal IdentityStore Store(Guid? installationId = null, string mode = "oidc", string? digest = null) =>
        new(fixture.Installation, installationId ?? InstallationId, mode, digest ?? new string('a', 64),
            FileLease, HostLease);

    internal IdentityStore StoreWithLease(AdmissionFixture other) =>
        new(fixture.Installation, InstallationId, "oidc", new string('a', 64), other.FileLease, other.HostLease);

    internal async Task ReleaseRuntimeLeaseAsync()
    {
        FileLease?.Dispose();
        FileLease = null;
        if (HostLease is not null) await HostLease.DisposeAsync();
        HostLease = null;
    }

    internal async Task AcquireRuntimeLeaseAsync()
    {
        Assert.Null(FileLease);
        Assert.Null(HostLease);
        if (fixture.Provider == "sqlite") FileLease = StoreLease.Acquire(fixture.Root!);
        else
        {
            using var budget = new OperationBudget(OperationBudget.Register, token);
            HostLease = Assert.IsType<NpgsqlConnection>(await fixture.Installation.OpenPlatformAsync(budget));
            await using var command = new NpgsqlCommand(
                "SELECT pg_catalog.pg_try_advisory_lock_shared($1)", HostLease);
            command.Parameters.AddWithValue(PostgresIdentity.LeaseKey);
            Assert.True(await command.ExecuteScalarAsync(token) is true);
        }
    }

    internal async Task ReconcileAsync(string mode, string digest)
    {
        await ReleaseRuntimeLeaseAsync();
        await using (var scope = await fixture.Maintenance.AcquireAsync(token))
        {
            using var budget = new OperationBudget(OperationBudget.Migration, token);
            await IdentityPreparation.RevokeAsync(fixture.Installation, Assert.IsType<MaintenanceScope>(scope),
                InstallationId, mode, digest, reconcile: true, budget);
        }
        await AcquireRuntimeLeaseAsync();
    }

    internal async Task VerifyPreparedAsync()
    {
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), token);
        var metadata = await MetadataAsync();
        await IdentityPreparation.VerifyAsync(fixture.Installation, InstallationId,
            metadata.Mode, metadata.TrustDigest, budget);
    }

    internal async Task InitializeAgainAsync()
    {
        FileLease?.Dispose();
        if (HostLease is not null) await HostLease.DisposeAsync();
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        using var budget = new OperationBudget(OperationBudget.Register, token);
        await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
        await using var transaction = await connection.BeginTransactionAsync(token);
        await IdentityStore.InitializeAsync(fixture.Installation, Assert.IsType<MaintenanceScope>(scope),
            connection, transaction, "oidc", new string('a', 64), DateTime.UtcNow, budget);
        await transaction.CommitAsync(token);
    }

    internal async Task InitializeWithForeignConnectionAsync(AdmissionFixture other)
    {
        FileLease?.Dispose();
        if (HostLease is not null) await HostLease.DisposeAsync();
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        using var budget = new OperationBudget(OperationBudget.Register, token);
        await using var connection = await other.fixture.Installation.OpenPlatformAsync(budget);
        await using var transaction = await connection.BeginTransactionAsync(token);
        await IdentityStore.InitializeAsync(fixture.Installation, Assert.IsType<MaintenanceScope>(scope),
            connection, transaction, "oidc", new string('a', 64), DateTime.UtcNow, budget);
    }

    internal async Task AdmitAsync(DateTime now, params IdentityAdmissionRequest[] requests)
    {
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), token);
        await new IdentityAdmission(Store()).AdmitAsync(now, requests, budget);
    }

    internal async Task<IdentityMetadata> MetadataAsync()
    {
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), token);
        await using var database = await DeadlineStore.OpenAsync(fixture.Installation, budget);
        return await IdentityStore.ReadMetadataAsync(database);
    }

    internal async Task<object?> ScalarAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), token);
        await using var database = await DeadlineStore.OpenAsync(fixture.Installation, budget);
        return DeadlineStore.AssertScalar(await database.QueryAsync(string.Format(
            System.Globalization.CultureInfo.InvariantCulture, sql, database.Prefix), parameters));
    }

    internal async Task ExecuteAsync(string sql, params (string Name, object? Value)[] parameters)
        => await ExecuteWithinAsync(sql, TimeSpan.FromSeconds(1), parameters);

    private async Task ExecuteWithinAsync(string sql, TimeSpan duration, params (string Name, object? Value)[] parameters)
    {
        using var budget = new OperationBudget(duration, token);
        await using var database = await DeadlineStore.OpenAsync(fixture.Installation, budget);
        await database.BeginWriteAsync();
        await database.QueryAsync(string.Format(System.Globalization.CultureInfo.InvariantCulture,
            sql, database.Prefix), parameters);
        await database.CommitAsync();
    }

    internal async Task SeedEventsAsync(int count, bool session, DateTime now, int offset = 0)
    {
        using var setup = new OperationBudget(OperationBudget.Migration, token);
        for (var seeded = 0; seeded < count;)
        {
            setup.Check();
            var batch = Math.Min(16_384, count - seeded);
            await ExecuteWithinAsync(fixture.Provider == "sqlite" ? """
        WITH RECURSIVE n(value) AS (SELECT @offset+1 UNION ALL SELECT value+1 FROM n WHERE value<@offset+@count)
        INSERT INTO __kofte_identity_events
        (event_id,security_revision,event_code,occurred_at_utc,correlation_id,user_id,outcome,retention_class)
        SELECT printf('%s0000000-0000-0000-0000-%012x',@prefix,value),0,@code,@now,
               '0123456789abcdef0123456789abcdef',NULL,'committed',@retention FROM n
        """ : """
        INSERT INTO kofte_system.__kofte_identity_events
        (event_id,security_revision,event_code,occurred_at_utc,correlation_id,user_id,outcome,retention_class)
        SELECT (@prefix || '0000000-0000-0000-0000-' || lpad(to_hex(value),12,'0'))::uuid,0,@code,@now,
               '0123456789abcdef0123456789abcdef',NULL,'committed',@retention
        FROM generate_series(@offset+1,@offset+@count) AS n(value)
        """, setup.Remaining, ("offset", offset + seeded), ("count", batch),
                ("prefix", session ? "2" : "1"), ("code", session ? "session_created" : "user_created"),
                ("now", now), ("retention", session ? "session" : "installation"));
            seeded += batch;
        }
    }

    internal Task SeedAsync(int rows, bool expired, DateTime now) => ExecuteAsync(fixture.Provider == "sqlite" ? """
        WITH RECURSIVE n(value) AS (SELECT 1 UNION ALL SELECT value+1 FROM n WHERE value<@rows)
        INSERT INTO __kofte_identity_admission (category,window_start_utc,key_digest,expires_at_utc,count)
        SELECT 'identity_client',@start,printf('%064x',value),@expiry,0 FROM n
        """ : """
        INSERT INTO kofte_system.__kofte_identity_admission (category,window_start_utc,key_digest,expires_at_utc,count)
        SELECT 'identity_client',@start,lpad(to_hex(value),64,'0'),@expiry,0 FROM generate_series(1,@rows) AS n(value)
        """, ("rows", rows), ("start", now.AddMinutes(expired ? -2 : 0)),
        ("expiry", now.AddMinutes(expired ? -1 : 1)));

    public async ValueTask DisposeAsync()
    {
        FileLease?.Dispose();
        if (HostLease is not null) await HostLease.DisposeAsync();
        await fixture.DisposeAsync();
    }
}
