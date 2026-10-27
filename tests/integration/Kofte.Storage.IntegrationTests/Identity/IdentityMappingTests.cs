using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Kofte.Storage.IntegrationTests.Identity;

public abstract class IdentityMappingTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;
    private static readonly DateTime Now = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ConcurrentExactKeyCreatesOneStableUserAndOneTransactionalCreationEvent()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var contenders = Enumerable.Range(0, 12).Select(_ => Task.Run(async () =>
        {
            await start.Task;
            return await ResolveAsync(fixture, "https://issuer.invalid", "subject", "Original");
        }, Token)).ToArray();
        start.SetResult();
        var users = await Task.WhenAll(contenders);
        var id = users[0].UserId;
        Assert.NotEqual(Guid.Empty, id);
        Assert.All(users, user => Assert.Equal(id, user.UserId));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_users"));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_external_identities"));
        Assert.Equal(id.ToString("D"), Convert.ToString(await fixture.ScalarAsync(
            "SELECT user_id FROM {0}__kofte_identity_events WHERE event_code='user_created'"),
            System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(12, (await fixture.MetadataAsync()).Revision);
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT registry_revision FROM {0}__kofte_installation"));
        for (var index = 0; index < 3; index++)
        {
            var reopened = await ResolveAsync(fixture, "https://issuer.invalid", "subject", "Changed");
            Assert.Equal(id, reopened.UserId);
            Assert.Equal("Changed", reopened.DisplayName);
            Assert.Equal(Now, reopened.CreatedAt);
        }
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_events"));
    }

    [Fact]
    public async Task CapacityAcceptsTheTenThousandthUserButNeverRefusesAnEstablishedMappingAtCapacity()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        await fixture.ExecuteAsync(Provider == "sqlite" ? """
            WITH RECURSIVE n(value) AS (SELECT 1 UNION ALL SELECT value+1 FROM n WHERE value<9999)
            INSERT INTO __kofte_users (user_id,mode,created_at_utc,updated_at_utc)
            SELECT printf('10000000-0000-0000-0000-%012x',value),'oidc',@now,@now FROM n
            """ : """
            INSERT INTO kofte_system.__kofte_users (user_id,mode,created_at_utc,updated_at_utc)
            SELECT ('10000000-0000-0000-0000-' || lpad(to_hex(value),12,'0'))::uuid,'oidc',@now,@now
            FROM generate_series(1,9999) AS n(value)
            """, ("now", Now));
        await fixture.ExecuteAsync("""
            INSERT INTO {0}__kofte_external_identities (mode,issuer,subject,user_id,created_at_utc)
            SELECT mode,'https://seed.invalid',CAST(user_id AS TEXT),user_id,created_at_utc FROM {0}__kofte_users
            """);
        var last = await ResolveAsync(fixture, "https://issuer.invalid", "last", null);
        Assert.Equal(10_000L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_users"));
        var refused = await Assert.ThrowsAsync<IdentityAdmissionException>(() =>
            ResolveAsync(fixture, "https://issuer.invalid", "one-over", null));
        Assert.Null(refused.RetryAt);
        var established = await ResolveAsync(fixture, "https://issuer.invalid", "last", "Changed");
        Assert.Equal(last.UserId, established.UserId);
        Assert.Equal("Changed", established.DisplayName);
        Assert.Equal(10_000L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_users"));
        Assert.Equal(10_000L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_external_identities"));
        Assert.Equal(last.UserId.ToString("D"), Convert.ToString(await fixture.ScalarAsync(
            "SELECT user_id FROM {0}__kofte_identity_events"), CultureInfo.InvariantCulture));
        Assert.Equal(2, (await fixture.MetadataAsync()).Revision);
    }

    [Fact]
    public async Task ThreeFreshOwnerProcessesAndSequentialTrustChangesPreserveExactMappingAndConsumedDesignation()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var original = await ResolveAsync(fixture, "https://issuer.invalid", "subject", "Original");
        await fixture.ExecuteAsync("""
            INSERT INTO {0}__kofte_bootstrap
            (singleton_key,installation_id,state,generation,initial_user_id,initial_user_mode,
             consumed_at_utc,consumed_generation)
            VALUES (1,@installation,'consumed',7,@user,'oidc',@now,7)
            """, ("installation", fixture.InstallationId), ("user", original.UserId), ("now", Now));
        var creationEvent = await fixture.ScalarAsync(
            "SELECT event_id FROM {0}__kofte_identity_events WHERE user_id=@user", ("user", original.UserId));
        for (var restart = 0; restart < 3; restart++)
        {
            await fixture.ReleaseRuntimeLeaseAsync();
            var start = RegistrationConcurrencyTests.Start(fixture.RegistryFixture,
                ["--identity-mapping-worker", "https://issuer.invalid", "subject", "Restarted"]);
            start.Environment.Remove("KOFTE_POSTGRES_PROVISIONING_CONNECTION");
            var output = await OwnedProcesses.RunAsync(start, TimeSpan.FromSeconds(45), Token);
            await fixture.AcquireRuntimeLeaseAsync();
            Assert.Equal(0, output.ExitCode);
            AssertCommittedDiagnostic(output.Stderr);
            using var document = JsonDocument.Parse(output.Stdout);
            var value = document.RootElement;
            Assert.Equal("identity_mapping_worker_ready", value.GetProperty("eventCode").GetString());
            Assert.Equal(original.UserId, value.GetProperty("UserId").GetGuid());
            Assert.Equal("oidc", value.GetProperty("Mode").GetString());
            Assert.Equal("Restarted", value.GetProperty("DisplayName").GetString());
            Assert.Equal(Now, value.GetProperty("CreatedAt").GetDateTime());
            Assert.Equal(original.UserId.ToString("D"), value.GetProperty("initialUserId").GetString());
            Assert.Equal("consumed", value.GetProperty("state").GetString());
            Assert.Equal(7, value.GetProperty("generation").GetInt64());
            Assert.Equal(7, value.GetProperty("consumedGeneration").GetInt64());
            Assert.Equal(Now, value.GetProperty("consumedAt").GetDateTime());
            Assert.Equal(creationEvent, await fixture.ScalarAsync(
                "SELECT event_id FROM {0}__kofte_identity_events WHERE user_id=@user", ("user", original.UserId)));
        }
        await fixture.ReconcileAsync("oidc", new string('b', 64));
        var replacement = await ResolveAsync(fixture, "https://replacement.invalid", "subject", "Restarted",
            digest: new string('b', 64));
        Assert.NotEqual(original.UserId, replacement.UserId);
        await fixture.ReconcileAsync("development", new string('c', 64));
        var development = await ResolveAsync(fixture, "kofte:development:v1", "subject", "Restarted",
            "development", new string('c', 64));
        Assert.NotEqual(original.UserId, development.UserId);
        Assert.NotEqual(replacement.UserId, development.UserId);
        var badNamespace = await Assert.ThrowsAsync<StorageException>(() =>
            ResolveAsync(fixture, "https://issuer.invalid", "subject", "Restarted", "development", new string('c', 64)));
        Assert.Equal(StorageCode.RequestInvalid, badNamespace.Code);
        await fixture.ReconcileAsync("oidc", new string('a', 64));
        var returned = await ResolveAsync(fixture, "https://issuer.invalid", "subject", "Renamed");
        Assert.Equal(original.UserId, returned.UserId);
        Assert.Equal(original.CreatedAt, returned.CreatedAt);
        Assert.Equal(creationEvent, await fixture.ScalarAsync(
            "SELECT event_id FROM {0}__kofte_identity_events WHERE user_id=@user", ("user", original.UserId)));
        Assert.Equal(original.UserId.ToString("D"), Convert.ToString(await fixture.ScalarAsync(
            "SELECT initial_user_id FROM {0}__kofte_bootstrap"), CultureInfo.InvariantCulture));
        Assert.Equal("consumed", await fixture.ScalarAsync("SELECT state FROM {0}__kofte_bootstrap"));
        Assert.Equal(7L, await fixture.ScalarAsync("SELECT consumed_generation FROM {0}__kofte_bootstrap"));
        Assert.Equal(3L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_users"));
        Assert.Equal(3L, await fixture.ScalarAsync(
            "SELECT COUNT(*) FROM {0}__kofte_identity_events WHERE event_code='user_created'"));
        Assert.Equal(3L, await fixture.ScalarAsync(
            "SELECT COUNT(*) FROM {0}__kofte_identity_events WHERE event_code='configuration_reconciled'"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT registry_revision FROM {0}__kofte_installation"));
        await fixture.VerifyPreparedAsync();
    }

    [Fact]
    public async Task PreparedStateVerificationRefusesAUserWhoseMappingIsMissingWithoutRepairingIt()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var user = await ResolveAsync(fixture, "https://issuer.invalid", "subject", null);
        await fixture.ExecuteAsync("""
            INSERT INTO {0}__kofte_bootstrap (singleton_key,installation_id,state,generation,initial_user_mode)
            VALUES (1,@installation,'disabled',0,'oidc')
            """, ("installation", fixture.InstallationId));
        await fixture.VerifyPreparedAsync();
        await fixture.ExecuteAsync("DELETE FROM {0}__kofte_external_identities WHERE user_id=@user", ("user", user.UserId));
        var before = await fixture.MetadataAsync();
        var error = await Assert.ThrowsAsync<StorageException>(fixture.VerifyPreparedAsync);
        Assert.Equal(StorageCode.Corrupt, error.Code);
        Assert.Equal(before, await fixture.MetadataAsync());
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_users"));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_external_identities"));
        Assert.Equal(user.UserId.ToString("D"), Convert.ToString(await fixture.ScalarAsync(
            "SELECT user_id FROM {0}__kofte_identity_events"), CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ExactIssuerAndSubjectRemainDistinctDespiteIdenticalOrChangedProfiles()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var first = await ResolveAsync(fixture, "https://issuer.invalid/Path", "Subject", "same@example.invalid");
        var ids = new HashSet<Guid> { first.UserId };
        foreach (var (issuer, subject) in new[]
        {
            ("https://issuer.invalid/Path", "subject"),
            ("https://issuer.invalid/path", "Subject"),
            ("https://other.invalid/Path", "Subject"),
            ("https://issuer.invalid/Path", " Subject"),
            ("https://issuer.invalid/Path", "Subject ")
        })
            Assert.True(ids.Add((await ResolveAsync(fixture, issuer, subject, "same@example.invalid")).UserId));
        var changed = await ResolveAsync(fixture, "https://issuer.invalid/Path", "Subject", "<changed>");
        Assert.Equal(first.UserId, changed.UserId);
        Assert.Equal("<changed>", changed.DisplayName);
        Assert.Equal(6L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_events"));
        Assert.Equal(6L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_users"));
    }

    [Fact]
    public async Task AnUnrelatedUniqueFailureIsNotReinterpretedAsAnExistingExternalIdentity()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var original = await ResolveAsync(fixture, "https://issuer.invalid", "original", "Unique");
        await fixture.ExecuteAsync("""
            CREATE UNIQUE INDEX identity_test_duplicate_display ON {0}__kofte_users (display_name)
            """);
        var before = await fixture.MetadataAsync();
        var error = await Assert.ThrowsAnyAsync<DbException>(() =>
            ResolveAsync(fixture, "https://issuer.invalid", "different", "Unique"));
        if (error is SqliteException sqlite) Assert.Equal(19, sqlite.SqliteErrorCode);
        else
        {
            var postgres = Assert.IsType<PostgresException>(error);
            Assert.Equal("23505", postgres.SqlState);
            Assert.Equal("identity_test_duplicate_display", postgres.ConstraintName);
        }
        Assert.Equal(before, await fixture.MetadataAsync());
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_users"));
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_external_identities"));
        Assert.Equal(original.UserId.ToString("D"), Convert.ToString(await fixture.ScalarAsync(
            "SELECT user_id FROM {0}__kofte_identity_events"), CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task LaterOwnerFailureRollsBackUserMappingAndRequiredEventTogether()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Store().MutateAsync<bool>(budget, async change =>
        {
            await IdentityUserStore.ResolveAsync(change, "https://issuer.invalid", "subject", null, Now);
            throw new InvalidOperationException("test_later_login_effect_failed");
        }));
        foreach (var table in new[] { "__kofte_users", "__kofte_external_identities", "__kofte_identity_events" })
            Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}" + table));
        Assert.Equal(0, (await fixture.MetadataAsync()).Revision);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExactTextLimitsCountUnicodeScalarsAndRejectUnsupportedKeysWithoutEffects(bool multibyteIssuer)
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var issuer = "https://issuer.invalid/" +
            (multibyteIssuer ? new string('\u00e9', 1012) + "a" : new string('a', 2025));
        var subject = new string('s', 255);
        var display = string.Concat(Enumerable.Repeat(char.ConvertFromUtf32(0x1f642), 128));
        var accepted = await ResolveAsync(fixture, issuer, subject, display);
        Assert.Equal(display, accepted.DisplayName);
        foreach (var (badIssuer, badSubject, badDisplay) in new[]
        {
            (issuer + "a", subject, display),
            ("", subject, display),
            ("https://issuer.invalid/\ud800", subject, display),
            ("https://issuer.invalid/\n", subject, display),
            (issuer, subject + "s", display),
            (issuer, "", display),
            (issuer, "subject\u007f", display),
            (issuer, "subject\u00e9", display),
            (issuer, subject, display + "a"),
            (issuer, subject, "control\n"),
            (issuer, subject, "control\u0085"),
            (issuer, subject, "unpaired\ud800")
        })
        {
            var error = await Assert.ThrowsAsync<StorageException>(() =>
                ResolveAsync(fixture, badIssuer, badSubject, badDisplay));
            Assert.Equal(StorageCode.RequestInvalid, error.Code);
        }
        Assert.Equal(1, (await fixture.MetadataAsync()).Revision);
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_users"));
        Assert.Equal(display, await fixture.ScalarAsync("SELECT display_name FROM {0}__kofte_users"));
    }

    private void AssertCommittedDiagnostic(string output)
    {
        Assert.InRange(System.Text.Encoding.UTF8.GetByteCount(output.Trim()), 1, 1024);
        using var document = JsonDocument.Parse(output, new JsonDocumentOptions { AllowDuplicateProperties = false });
        var record = document.RootElement;
        Assert.Equal(new[]
        {
            "component", "correlationId", "durability", "eventCode", "guidance", "mode", "operation",
            "outcome", "provider", "version"
        }, record.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
        Assert.Equal(1, record.GetProperty("version").GetInt32());
        Assert.Equal("identity_completed", record.GetProperty("eventCode").GetString());
        Assert.Equal("identity", record.GetProperty("component").GetString());
        Assert.Equal("owner_mutation", record.GetProperty("operation").GetString());
        Assert.Equal("oidc", record.GetProperty("mode").GetString());
        Assert.Equal(Provider == "sqlite" ? "sqlite" : "postgresql", record.GetProperty("provider").GetString());
        Assert.Equal("ready", record.GetProperty("outcome").GetString());
        Assert.Equal("none", record.GetProperty("guidance").GetString());
        Assert.Equal("committed", record.GetProperty("durability").GetString());
        Assert.Matches("^[0-9a-f]{32}$", record.GetProperty("correlationId").GetString());
    }

    private async Task<IdentityUser> ResolveAsync(AdmissionFixture fixture, string issuer, string subject, string? display,
        string mode = "oidc", string? digest = null)
    {
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        return await fixture.Store(mode: mode, digest: digest).MutateAsync(budget,
            change => IdentityUserStore.ResolveAsync(change, issuer, subject, display, Now));
    }
}

[Trait("Provider", "sqlite")]
[Collection("identity-deadlines")]
public sealed class SqliteIdentityMappingTests : IdentityMappingTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Collection("identity-deadlines")]
public sealed class PostgresIdentityMappingTests : IdentityMappingTests
{
    protected override string Provider => "postgres";
}
