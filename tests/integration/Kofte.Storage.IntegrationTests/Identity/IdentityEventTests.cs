using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;

namespace Kofte.Storage.IntegrationTests.Identity;

public abstract class IdentityEventTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;
    private static readonly DateTime Now = new(2030, 1, 31, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task RequiredEvidenceCommitsWithItsOwnerRevisionAndCannotBeChangedOrDeleted()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var receipt = await RecordAsync(fixture, IdentityEventCode.KeysPrepared);
        Assert.NotEqual(Guid.Empty, receipt.EventId);
        Assert.Matches("^[0-9a-f]{32}$", receipt.Correlation);
        Assert.Equal(1, (await fixture.MetadataAsync()).Revision);
        Assert.Equal(receipt.EventId.ToString("D"), Convert.ToString(await fixture.ScalarAsync(
            "SELECT event_id FROM {0}__kofte_identity_events"), CultureInfo.InvariantCulture));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT security_revision FROM {0}__kofte_identity_events"));
        Assert.Equal("keys_prepared", await fixture.ScalarAsync("SELECT event_code FROM {0}__kofte_identity_events"));
        Assert.Equal("committed", await fixture.ScalarAsync("SELECT outcome FROM {0}__kofte_identity_events"));
        Assert.Equal("installation", await fixture.ScalarAsync("SELECT retention_class FROM {0}__kofte_identity_events"));
        Assert.Equal(receipt.Correlation, await fixture.ScalarAsync("SELECT correlation_id FROM {0}__kofte_identity_events"));
        await Assert.ThrowsAnyAsync<DbException>(() => fixture.ExecuteAsync(
            "UPDATE {0}__kofte_identity_events SET event_code='sessions_revoked'"));
        await Assert.ThrowsAnyAsync<DbException>(() => fixture.ExecuteAsync("DELETE FROM {0}__kofte_identity_events"));
        Assert.Equal(receipt.EventId.ToString("D"), Convert.ToString(await fixture.ScalarAsync(
            "SELECT event_id FROM {0}__kofte_identity_events"), CultureInfo.InvariantCulture));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT registry_revision FROM {0}__kofte_installation"));
    }

    [Fact]
    public async Task FailureAfterRecordingRollsBackBothEvidenceAndOwnerEffects()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Store().MutateAsync<bool>(budget, async change =>
        {
            await IdentityEventStore.AppendAsync(change, IdentityEventCode.KeysPrepared, Now);
            throw new InvalidOperationException("test_before_commit");
        }));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_events"));
        Assert.Equal(0, (await fixture.MetadataAsync()).Revision);
    }

    [Fact]
    public async Task MissingReferencedUserRefusesTheWholeTransaction()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        await Assert.ThrowsAnyAsync<DbException>(() =>
            RecordAsync(fixture, IdentityEventCode.UserCreated, Guid.NewGuid()));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_events"));
        Assert.Equal(0, (await fixture.MetadataAsync()).Revision);
    }

    [Fact]
    public async Task PruningIsOne128RowBatchAndOnlyAfterThirtyDays()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        await fixture.SeedEventsAsync(257, session: true, Now.AddDays(-30));
        await fixture.SeedEventsAsync(1, session: true, Now.AddDays(-30).AddTicks(10), offset: 257);
        await fixture.SeedEventsAsync(1, session: false, Now.AddYears(-1));
        await RecordAsync(fixture, IdentityEventCode.SessionCreated);
        Assert.Equal(132L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_events"));
        Assert.Equal(1L, await fixture.ScalarAsync(
            "SELECT COUNT(*) FROM {0}__kofte_identity_events WHERE event_code='user_created'"));
        Assert.Equal(1L, await fixture.ScalarAsync(
            "SELECT COUNT(*) FROM {0}__kofte_identity_events WHERE event_id='20000000-0000-0000-0000-000000000102'"));
    }

    [Fact]
    public async Task ExactCapacitiesWarnAtNinetyPercentAndRefuseWithoutCommittingEarlierEffects()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        await fixture.SeedEventsAsync(899_999, session: false, Now.AddDays(-1));
        await fixture.SeedEventsAsync(179_999, session: true, Now.AddDays(-1));
        var originalError = Console.Error;
        using var capture = new StringWriter(CultureInfo.InvariantCulture);
        try
        {
            Console.SetError(capture);
            await RecordAsync(fixture, IdentityEventCode.KeysPrepared);
            await RecordAsync(fixture, IdentityEventCode.SessionCreated);
        }
        finally { Console.SetError(originalError); }
        var warnings = capture.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, warnings.Length);
        foreach (var line in warnings)
        {
            Assert.True(System.Text.Encoding.UTF8.GetByteCount(line) <= 1024);
            using var json = JsonDocument.Parse(line);
            Assert.Equal("identity_event_capacity_warning", json.RootElement.GetProperty("eventCode").GetString());
            Assert.Matches("^[0-9a-f]{32}$", json.RootElement.GetProperty("correlation").GetString()!);
        }
        using var failedSink = new FailedWarningSink();
        try
        {
            Console.SetError(failedSink);
            await Assert.ThrowsAsync<IOException>(() => RecordAsync(fixture, IdentityEventCode.KeysPrepared));
        }
        finally { Console.SetError(originalError); }
        Assert.Equal(2, (await fixture.MetadataAsync()).Revision);
        await fixture.SeedEventsAsync(100_000, session: false, Now.AddDays(-1), offset: 899_999);
        await fixture.SeedEventsAsync(20_000, session: true, Now.AddDays(-1), offset: 179_999);
        foreach (var code in new[] { IdentityEventCode.UserCreated, IdentityEventCode.SessionCreated })
        {
            using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
            await Assert.ThrowsAsync<IdentityEventCapacityException>(() =>
                fixture.Store().MutateAsync(budget, async change =>
                {
                    var userId = Guid.NewGuid();
                    await change.Database.QueryAsync($"""
                        INSERT INTO {change.Database.Prefix}__kofte_users
                        (user_id,mode,display_name,created_at_utc,updated_at_utc)
                        VALUES (@id,'oidc',NULL,@now,@now)
                        """, ("id", userId), ("now", Now));
                    return await IdentityEventStore.AppendAsync(change, code, Now, userId);
                }));
        }

        Assert.Equal(1_200_000L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_events"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_users"));
        Assert.Equal(2, (await fixture.MetadataAsync()).Revision);
    }

    private async Task<IdentityEventReceipt> RecordAsync(AdmissionFixture fixture, IdentityEventCode code, Guid? user = null)
    {
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        return await fixture.Store().MutateAsync(budget,
            change => IdentityEventStore.AppendAsync(change, code, Now, user));
    }

    private sealed class FailedWarningSink : StringWriter
    {
        public override void WriteLine(string? value) => throw new IOException("test_warning_sink_failed");
    }
}

[Trait("Provider", "sqlite")]
[Collection("identity-deadlines")]
public sealed class SqliteIdentityEventTests : IdentityEventTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Collection("identity-deadlines")]
public sealed class PostgresIdentityEventTests : IdentityEventTests
{
    protected override string Provider => "postgres";
}
