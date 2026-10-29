using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Kofte.Storage.IntegrationTests.Identity;

public abstract class IdentityCapacityTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;
    private static readonly DateTime Now = new(2020, 1, 31, 12, 0, 15, DateTimeKind.Utc);
    private static readonly string Key = new('f', 64);
    private const string Issuer = "https://capacity.invalid";
    private const string Authorization = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Theory]
    [InlineData("LoginInstallation", "login_installation", 60)]
    [InlineData("LoginBrowser", "login_browser", 10)]
    [InlineData("CallbackInstallation", "callback_installation", 120)]
    [InlineData("CallbackBrowser", "callback_browser", 20)]
    [InlineData("EnrollmentInstallation", "enrollment_installation", 200)]
    [InlineData("EnrollmentSession", "enrollment_session", 40)]
    [InlineData("IdentityInstallation", "identity_installation", 1200)]
    [InlineData("IdentityClient", "identity_client", 120)]
    public async Task EveryRateAcceptsItsLastRequestAndRefusesTheNextUntilTheExactDurableWindowBoundary(
        string category, string storedCategory, int limit)
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var request = new IdentityAdmissionRequest(Enum.Parse<IdentityAdmissionCategory>(category), Key);
        await fixture.AdmitAsync(Now, request);
        await fixture.ExecuteAsync("UPDATE {0}__kofte_identity_admission SET count=@count", ("count", (long)limit - 1));
        await ReopenAsync(fixture);
        await fixture.AdmitAsync(Now, request);
        Assert.Equal((long)limit, await fixture.ScalarAsync("SELECT count FROM {0}__kofte_identity_admission"));
        Assert.Equal(storedCategory, await fixture.ScalarAsync("SELECT category FROM {0}__kofte_identity_admission"));
        var full = await SnapshotAsync(fixture);
        var expiry = Now.AddSeconds(45);
        await ReopenAsync(fixture);
        foreach (var time in new[] { Now, expiry.AddMicroseconds(-1) })
        {
            var refused = await Assert.ThrowsAsync<IdentityAdmissionException>(() => fixture.AdmitAsync(time, request));
            Assert.Equal(expiry, refused.RetryAt);
            Assert.Equal(full, await SnapshotAsync(fixture));
        }
        await fixture.AdmitAsync(expiry, request);
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT count FROM {0}__kofte_identity_admission"));
        Assert.Equal(expiry, IdentityValues.Utc(await fixture.ScalarAsync(
            "SELECT window_start_utc FROM {0}__kofte_identity_admission")));
        Assert.Equal(expiry.AddMinutes(1), IdentityValues.Utc(await fixture.ScalarAsync(
            "SELECT expires_at_utc FROM {0}__kofte_identity_admission")));
        await ReopenAsync(fixture);
        await fixture.AdmitAsync(expiry.AddMicroseconds(1), request);
        Assert.Equal(2L, await fixture.ScalarAsync("SELECT count FROM {0}__kofte_identity_admission"));
        Assert.Equal(4, (await fixture.MetadataAsync()).Revision);
    }

    [Fact]
    public async Task LastLiveAdmissionWindowIsDurableAndExistingKeysRemainUsableAtCapacity()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        await fixture.SeedAsync(4095, expired: false, Now);
        var request = new IdentityAdmissionRequest(IdentityAdmissionCategory.LoginBrowser, Key);
        await fixture.AdmitAsync(Now, request);
        Assert.Equal(4096L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_admission"));
        await ReopenAsync(fixture);
        var full = await SnapshotAsync(fixture);
        var refused = await Assert.ThrowsAsync<IdentityAdmissionException>(() => fixture.AdmitAsync(Now,
            new IdentityAdmissionRequest(IdentityAdmissionCategory.CallbackBrowser, Key)));
        Assert.Null(refused.RetryAt);
        Assert.Equal(full, await SnapshotAsync(fixture));
        await fixture.AdmitAsync(Now, request);
        Assert.Equal(2L, await fixture.ScalarAsync(
            "SELECT count FROM {0}__kofte_identity_admission WHERE category='login_browser'"));
        Assert.Equal(4096L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_admission"));
    }

    [Fact]
    public async Task SaturatedStoredCounterCannotWrapOrCommitAnEarlierPairedCounter()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var request = new IdentityAdmissionRequest(IdentityAdmissionCategory.IdentityClient, Key);
        await fixture.AdmitAsync(Now, request);
        await fixture.ExecuteAsync("UPDATE {0}__kofte_identity_admission SET count=@count", ("count", long.MaxValue));
        await ReopenAsync(fixture);
        var before = await SnapshotAsync(fixture);
        var refused = await Assert.ThrowsAsync<IdentityAdmissionException>(() => fixture.AdmitAsync(Now,
            new IdentityAdmissionRequest(IdentityAdmissionCategory.IdentityInstallation, Key), request));
        Assert.Equal(Now.AddSeconds(45), refused.RetryAt);
        Assert.Equal(before, await SnapshotAsync(fixture));
        await fixture.AdmitAsync(Now.AddSeconds(45), request);
        Assert.Equal(1L, await fixture.ScalarAsync("SELECT count FROM {0}__kofte_identity_admission"));
    }

    [Fact]
    public async Task FiftiethThousandthSessionCommitsButTheNextCannotEvictAnyUserOrReference()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var owner = await CreateUserSessionAsync(fixture, "owner", Key);
        await SeedSessionCapacityAsync(fixture);
        Assert.Equal(49_999L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_sessions"));
        var other = await RowsAsync(fixture, "__kofte_sessions",
            "WHERE reference_digest=@digest", ("digest", 1.ToString("x64", CultureInfo.InvariantCulture)));
        var ownerBefore = await RowsAsync(fixture, "__kofte_sessions", "WHERE reference_digest=@digest", ("digest", Key));
        var lastDigest = new string('e', 64);
        using (var budget = LocalBudget())
            await fixture.Store().MutateAsync(budget, change =>
                IdentitySessionStore.CreateAsync(change, owner.User, lastDigest, [1, 2, 3], Now));
        await ReopenAsync(fixture);
        var metadata = await fixture.MetadataAsync();
        var events = await RowsAsync(fixture, "__kofte_identity_events");
        using (var budget = LocalBudget())
        {
            var refused = await Assert.ThrowsAsync<IdentityAdmissionException>(() => fixture.Store().MutateAsync(budget,
                change => IdentitySessionStore.CreateAsync(change, owner.User, new string('d', 64), [4], Now)));
            Assert.Null(refused.RetryAt);
        }
        Assert.Equal(metadata, await fixture.MetadataAsync());
        Assert.Equal(events, await RowsAsync(fixture, "__kofte_identity_events"));
        Assert.Equal(50_000L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_sessions"));
        Assert.Equal(50_000L, await fixture.ScalarAsync(
            "SELECT COUNT(DISTINCT reference_digest) FROM {0}__kofte_sessions WHERE state='active' AND terminal_at_utc IS NULL"));
        Assert.Equal(6251L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_users"));
        Assert.Equal(6251L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_external_identities"));
        Assert.Equal(other, await RowsAsync(fixture, "__kofte_sessions",
            "WHERE reference_digest=@digest", ("digest", 1.ToString("x64", CultureInfo.InvariantCulture))));
        Assert.Equal(ownerBefore, await RowsAsync(fixture, "__kofte_sessions", "WHERE reference_digest=@digest", ("digest", Key)));
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT registry_revision FROM {0}__kofte_installation"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public async Task NominatedCeremonyExpiresAtExactlyFifteenMinutesWithoutPersistingFailedSessionActivity(int microseconds)
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var owner = await CreateUserSessionAsync(fixture, "owner", Key);
        await SeedPendingAsync(fixture);
        var before = await SnapshotAsync(fixture);
        using var budget = LocalBudget();
        var time = Now.AddMinutes(15).AddMicroseconds(microseconds);
        var operation = fixture.Store().MutateAsync(budget, change =>
            IdentityBootstrapStore.EnrollAsync(change, owner, Authorization, time));
        if (microseconds < 0)
        {
            var receipt = await operation;
            Assert.Equal("consumed", await fixture.ScalarAsync("SELECT state FROM {0}__kofte_bootstrap"));
            Assert.Equal(owner.User.UserId, IdentityValues.Id(await fixture.ScalarAsync(
                "SELECT initial_user_id FROM {0}__kofte_bootstrap")));
            Assert.Equal(time, IdentityValues.Utc(await fixture.ScalarAsync("SELECT consumed_at_utc FROM {0}__kofte_bootstrap")));
            Assert.Equal(receipt.EventId, IdentityValues.Id(await fixture.ScalarAsync(
                "SELECT event_id FROM {0}__kofte_identity_events WHERE event_code='initial_administrator_enrolled'")));
        }
        else
        {
            var refused = await Assert.ThrowsAsync<IdentityStateException>(() => operation);
            Assert.Equal(IdentityDiagnosticOutcome.EnrollmentUnavailable, refused.Outcome);
            Assert.Equal(before, await SnapshotAsync(fixture));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OneMicrosecondPastEitherSessionLifetimeIsDurablyTerminalAndCannotRenew(bool absolute)
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var owner = await CreateUserSessionAsync(fixture, "owner", Key);
        await SeedPendingAsync(fixture);
        var expiry = absolute ? Now.AddHours(8) : Now.AddMinutes(30);
        if (absolute)
            await fixture.ExecuteAsync("""
                UPDATE {0}__kofte_sessions SET last_seen_at_utc=@seen,idle_expires_at_utc=@expiry
                WHERE reference_digest=@digest
                """, ("seen", expiry.AddMinutes(-1)), ("expiry", expiry), ("digest", Key));
        using (var budget = LocalBudget())
            Assert.Null(await fixture.Store().MutateAsync(budget, change =>
                IdentitySessionStore.RetrieveAsync(change, Key, expiry.AddMicroseconds(1))));
        Assert.Equal("expired", await fixture.ScalarAsync("SELECT state FROM {0}__kofte_sessions"));
        Assert.Equal(expiry.AddMicroseconds(1), IdentityValues.Utc(await fixture.ScalarAsync(
            "SELECT terminal_at_utc FROM {0}__kofte_sessions")));
        Assert.Equal(owner.User.UserId, IdentityValues.Id(await fixture.ScalarAsync(
            "SELECT user_id FROM {0}__kofte_identity_events WHERE event_code='session_expired'")));
        var session = await RowsAsync(fixture, "__kofte_sessions");
        var events = await RowsAsync(fixture, "__kofte_identity_events");
        await ReopenAsync(fixture);
        using (var budget = LocalBudget())
            Assert.Null(await fixture.Store().MutateAsync(budget, change =>
                IdentitySessionStore.RetrieveAsync(change, Key, expiry.AddMicroseconds(2))));
        Assert.Equal(session, await RowsAsync(fixture, "__kofte_sessions"));
        Assert.Equal(events, await RowsAsync(fixture, "__kofte_identity_events"));
    }

    [Fact]
    public async Task LastCeremonyGenerationCommitsAndOverflowCannotDeliverOrReplaceItsAuthorization()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        await fixture.ExecuteAsync("""
            INSERT INTO {0}__kofte_bootstrap (singleton_key,installation_id,state,generation,initial_user_mode)
            VALUES (1,@installation,'disabled',@generation,'oidc')
            """, ("installation", fixture.InstallationId), ("generation", long.MaxValue - 1));
        await fixture.ReleaseRuntimeLeaseAsync();
        await using var scope = await fixture.RegistryFixture.Maintenance.AcquireAsync(Token);
        var deliveries = 0;
        Task DeliverAsync() { deliveries++; return Task.CompletedTask; }
        using (var budget = new OperationBudget(OperationBudget.Migration, Token))
            await IdentityBootstrapStore.AuthorizeAsync(fixture.RegistryFixture.Installation,
                Assert.IsType<MaintenanceScope>(scope), fixture.InstallationId, "oidc", new string('a', 64),
                Issuer, "owner", Authorization, DeliverAsync, budget);
        Assert.Equal(long.MaxValue, await fixture.ScalarAsync("SELECT generation FROM {0}__kofte_bootstrap"));
        var before = await SnapshotAsync(fixture);
        using (var budget = new OperationBudget(OperationBudget.Migration, Token))
            await Assert.ThrowsAsync<OverflowException>(() => IdentityBootstrapStore.AuthorizeAsync(
                fixture.RegistryFixture.Installation, Assert.IsType<MaintenanceScope>(scope), fixture.InstallationId,
                "oidc", new string('a', 64), Issuer, "replacement", Authorization, DeliverAsync, budget));
        Assert.Equal(1, deliveries);
        Assert.Equal(before, await SnapshotAsync(fixture));
    }

    [Theory]
    [InlineData("user_created")]
    [InlineData("session_created")]
    [InlineData("initial_administrator_enrolled")]
    [InlineData("session_expired")]
    public async Task RejectedRequiredRecordRollsBackItsEntireLoginEnrollmentOrExpiryAndCorrectionCanRetry(string code)
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var owner = await CreateUserSessionAsync(fixture, "owner", Key);
        await CreateUserSessionAsync(fixture, "other", new string('e', 64));
        await SeedPendingAsync(fixture);
        IdentityLoginAttempt attempt;
        using (var budget = LocalBudget())
            attempt = await fixture.Store().MutateAsync(budget, change =>
                IdentityLoginAttempts.StartAsync(change, Key, "/identity", Now));
        await RefuseEventAsync(fixture, code);
        var records = Assert.IsType<long>(await fixture.ScalarAsync(
            "SELECT COUNT(*) FROM {0}__kofte_identity_events WHERE event_code=@code", ("code", code)));
        var before = await SnapshotAsync(fixture);
        async Task MutateAsync()
        {
            using var budget = LocalBudget();
            await fixture.Store().MutateAsync(budget, async change =>
            {
                if (code is "user_created" or "session_created")
                {
                    var user = await IdentityUserStore.ResolveAsync(change, Issuer, "new-user", "New user", Now);
                    await IdentityLoginAttempts.ConsumeAsync(change, attempt.AttemptId, Key, Now);
                    await IdentitySessionStore.CreateAsync(change, user, new string('d', 64), [4, 5], Now);
                }
                else if (code == "initial_administrator_enrolled")
                    await IdentityBootstrapStore.EnrollAsync(change, owner, Authorization, Now.AddMinutes(1));
                else
                    Assert.Null(await IdentitySessionStore.RetrieveAsync(change, Key, Now.AddMinutes(30)));
                return true;
            });
        }
        AssertUniqueFailure(await Assert.ThrowsAnyAsync<DbException>(MutateAsync));
        await ReopenAsync(fixture);
        Assert.Equal(before, await SnapshotAsync(fixture));
        await AllowEventAsync(fixture);
        await MutateAsync();
        Assert.Equal(records + 1, await fixture.ScalarAsync(
            "SELECT COUNT(*) FROM {0}__kofte_identity_events WHERE event_code=@code", ("code", code)));
        Assert.Equal(1L, await fixture.ScalarAsync("""
            SELECT COUNT(*) FROM {0}__kofte_sessions WHERE reference_digest=@digest AND state='active'
            AND terminal_at_utc IS NULL AND last_seen_at_utc=@now
            """, ("digest", new string('e', 64)), ("now", Now)));
    }

    [Theory]
    [InlineData("bootstrap_authorized")]
    [InlineData("bootstrap_disabled")]
    [InlineData("configuration_reconciled")]
    [InlineData("sessions_revoked")]
    public async Task RejectedStoppedRecordRollsBackNominationEpochTrustSessionsAndAttemptsTogether(string code)
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        await CreateUserSessionAsync(fixture, "owner", Key);
        await CreateUserSessionAsync(fixture, "other", new string('e', 64));
        await SeedPendingAsync(fixture);
        using (var budget = LocalBudget())
            await fixture.Store().MutateAsync(budget, change =>
                IdentityLoginAttempts.StartAsync(change, Key, "/identity", Now));
        await RefuseEventAsync(fixture, code);
        var before = await SnapshotAsync(fixture);
        await fixture.ReleaseRuntimeLeaseAsync();
        await using var scope = await fixture.RegistryFixture.Maintenance.AcquireAsync(Token);
        async Task MutateAsync()
        {
            using var budget = new OperationBudget(OperationBudget.Migration, Token);
            var installation = fixture.RegistryFixture.Installation;
            var stopped = Assert.IsType<MaintenanceScope>(scope);
            if (code == "bootstrap_authorized")
                await IdentityBootstrapStore.AuthorizeAsync(installation, stopped, fixture.InstallationId,
                    "oidc", new string('a', 64), Issuer, "replacement", Authorization, () => Task.CompletedTask, budget);
            else if (code == "bootstrap_disabled")
                await IdentityBootstrapStore.DisablePendingAsync(installation, stopped, fixture.InstallationId,
                    "oidc", new string('a', 64), budget);
            else
                await IdentityPreparation.RevokeAsync(installation, stopped, fixture.InstallationId,
                    "oidc", new string(code == "configuration_reconciled" ? 'c' : 'a', 64),
                    reconcile: code == "configuration_reconciled", budget);
        }
        AssertUniqueFailure(await Assert.ThrowsAnyAsync<DbException>(MutateAsync));
        Assert.Equal(before, await SnapshotAsync(fixture));
        await AllowEventAsync(fixture);
        await MutateAsync();
        Assert.Equal(2L, await fixture.ScalarAsync(
            "SELECT COUNT(*) FROM {0}__kofte_identity_events WHERE event_code=@code", ("code", code)));
    }

    [Fact]
    public async Task StoppedPruningRetainsExactOtherUsersMappingsSessionsAndInstallationEvidenceAtTheThirtyDayBoundary()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var owner = await CreateUserSessionAsync(fixture, "owner", Key);
        await CreateUserSessionAsync(fixture, "other", new string('e', 64));
        await fixture.ExecuteAsync("""
            INSERT INTO {0}__kofte_bootstrap
            (singleton_key,installation_id,state,generation,initial_user_id,initial_user_mode,consumed_at_utc,consumed_generation)
            VALUES (1,@installation,'consumed',1,@user,'oidc',@now,1)
            """, ("installation", fixture.InstallationId), ("user", owner.User.UserId), ("now", Now));
        var cutoff = Now.AddDays(-30);
        await fixture.SeedEventsAsync(129, session: true, cutoff);
        await fixture.SeedEventsAsync(1, session: true, cutoff.AddMicroseconds(-1), offset: 129);
        await fixture.SeedEventsAsync(1, session: true, cutoff.AddMicroseconds(1), offset: 130);
        await fixture.SeedEventsAsync(1, session: false, Now.AddYears(-1));
        var preservedTables = new[] { "__kofte_users", "__kofte_external_identities", "__kofte_sessions", "__kofte_bootstrap" };
        var before = new List<string>();
        foreach (var table in preservedTables) before.Add(await RowsAsync(fixture, table));
        var installationEvents = await RowsAsync(fixture, "__kofte_identity_events", "WHERE retention_class='installation'");
        var retainedSessionEvents = await RowsAsync(fixture, "__kofte_identity_events",
            "WHERE retention_class='session' AND occurred_at_utc>@cutoff", ("cutoff", cutoff));
        var metadata = await fixture.MetadataAsync();
        await fixture.ReleaseRuntimeLeaseAsync();
        await using (var scope = await fixture.RegistryFixture.Maintenance.AcquireAsync(Token))
        {
            foreach (var expected in new[] { 128, 2, 0 })
            {
                using var budget = new OperationBudget(OperationBudget.Migration, Token);
                Assert.Equal(expected, await IdentityMaintenance.RunAsync(fixture.RegistryFixture.Installation,
                    Assert.IsType<MaintenanceScope>(scope), fixture.InstallationId, "oidc", new string('a', 64), Now, budget));
            }
        }
        await fixture.AcquireRuntimeLeaseAsync();
        for (var index = 0; index < preservedTables.Length; index++)
            Assert.Equal(before[index], await RowsAsync(fixture, preservedTables[index]));
        Assert.Equal(installationEvents, await RowsAsync(fixture, "__kofte_identity_events", "WHERE retention_class='installation'"));
        Assert.Equal(retainedSessionEvents, await RowsAsync(fixture, "__kofte_identity_events", "WHERE retention_class='session'"));
        Assert.Equal(metadata with { Revision = metadata.Revision + 3 }, await fixture.MetadataAsync());
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT registry_revision FROM {0}__kofte_installation"));
    }

    private OperationBudget LocalBudget() => new(TimeSpan.FromSeconds(1), Token);

    private static async Task ReopenAsync(AdmissionFixture fixture)
    {
        await fixture.ReleaseRuntimeLeaseAsync();
        await fixture.AcquireRuntimeLeaseAsync();
    }

    private async Task<IdentitySessionView> CreateUserSessionAsync(AdmissionFixture fixture, string subject, string digest)
    {
        using var budget = LocalBudget();
        return await fixture.Store().MutateAsync(budget, async change =>
        {
            var user = await IdentityUserStore.ResolveAsync(change, Issuer, subject, subject, Now);
            var session = await IdentitySessionStore.CreateAsync(change, user, digest, [1, 2, 3], Now);
            return new IdentitySessionView(session, user, false);
        });
    }

    private static Task SeedPendingAsync(AdmissionFixture fixture) => fixture.ExecuteAsync("""
        INSERT INTO {0}__kofte_bootstrap
        (singleton_key,installation_id,state,generation,nominee_issuer,nominee_subject,authorization_digest,expires_at_utc,initial_user_mode)
        VALUES (1,@installation,'pending',1,@issuer,'owner',@digest,@expiry,'oidc')
        """, ("installation", fixture.InstallationId), ("issuer", Issuer),
        ("digest", IdentityBootstrapStore.AuthorizationDigest(fixture.InstallationId, 1, Issuer, "owner", Authorization)),
        ("expiry", Now.AddMinutes(15)));

    private async Task RefuseEventAsync(AdmissionFixture fixture, string code)
    {
        var revision = (await fixture.MetadataAsync()).Revision;
        await fixture.ExecuteAsync("""
            INSERT INTO {0}__kofte_identity_events
            (event_id,security_revision,event_code,occurred_at_utc,correlation_id,outcome,retention_class)
            VALUES (@id,@revision,@code,@now,'0123456789abcdef0123456789abcdef','committed',@retention)
            """, ("id", Guid.NewGuid()), ("revision", revision), ("code", code), ("now", Now),
            ("retention", code.StartsWith("session_", StringComparison.Ordinal) ? "session" : "installation"));
        // A real provider UNIQUE violation rejects only the targeted required record, after owner writes.
        var prefix = Provider == "sqlite" ? "" : "kofte_system.";
        await fixture.ExecuteAsync($"""
            CREATE UNIQUE INDEX identity_test_record_failure ON {prefix}__kofte_identity_events (event_code)
            WHERE event_code='{code}' AND security_revision>={revision.ToString(CultureInfo.InvariantCulture)}
            """);
    }

    private Task AllowEventAsync(AdmissionFixture fixture) => fixture.ExecuteAsync(
        Provider == "sqlite" ? "DROP INDEX identity_test_record_failure" : "DROP INDEX kofte_system.identity_test_record_failure");

    private static void AssertUniqueFailure(DbException error)
    {
        if (error is SqliteException sqlite) Assert.Equal(19, sqlite.SqliteErrorCode);
        else
        {
            var postgres = Assert.IsType<PostgresException>(error);
            Assert.Equal("23505", postgres.SqlState);
            Assert.Equal("identity_test_record_failure", postgres.ConstraintName);
        }
    }

    private async Task<string> SnapshotAsync(AdmissionFixture fixture)
    {
        var snapshots = new List<string>();
        foreach (var table in new[]
        {
            "__kofte_identity_metadata", "__kofte_users", "__kofte_external_identities", "__kofte_sessions",
            "__kofte_login_attempts", "__kofte_bootstrap", "__kofte_identity_events", "__kofte_identity_admission"
        })
            snapshots.Add(await RowsAsync(fixture, table));
        return JsonSerializer.Serialize(snapshots);
    }

    private async Task<string> RowsAsync(AdmissionFixture fixture, string table, string where = "",
        params (string Name, object? Value)[] parameters)
    {
        using var budget = LocalBudget();
        await using var database = await DeadlineStore.OpenAsync(fixture.RegistryFixture.Installation, budget);
        var rows = new List<string>();
        while (true)
        {
            var page = await database.QueryAsync($"""
                SELECT * FROM {database.Prefix}{table} {where} ORDER BY 1,2,3 LIMIT 128 OFFSET @offset
                """, [.. parameters, ("offset", rows.Count)]);
            rows.AddRange(page.Select(row => JsonSerializer.Serialize(row)));
            if (page.Count < 128) break;
        }
        return JsonSerializer.Serialize(rows.Order(StringComparer.Ordinal));
    }

    private async Task SeedSessionCapacityAsync(AdmissionFixture fixture)
    {
        await fixture.ExecuteAsync(Provider == "sqlite" ? """
            WITH RECURSIVE n(value) AS (SELECT 1 UNION ALL SELECT value+1 FROM n WHERE value<6250)
            INSERT INTO __kofte_users (user_id,mode,created_at_utc,updated_at_utc)
            SELECT printf('10000000-0000-0000-0000-%012x',value),'oidc',@now,@now FROM n
            """ : """
            INSERT INTO kofte_system.__kofte_users (user_id,mode,created_at_utc,updated_at_utc)
            SELECT ('10000000-0000-0000-0000-' || lpad(to_hex(value),12,'0'))::uuid,'oidc',@now,@now
            FROM generate_series(1,6250) AS n(value)
            """, ("now", Now));
        await fixture.ExecuteAsync("""
            INSERT INTO {0}__kofte_external_identities (mode,issuer,subject,user_id,created_at_utc)
            SELECT mode,'https://seed.invalid',CAST(user_id AS TEXT),user_id,created_at_utc
            FROM {0}__kofte_users WHERE user_id<>@owner
            """, ("owner", IdentityValues.Id(await fixture.ScalarAsync(
                "SELECT user_id FROM {0}__kofte_sessions WHERE reference_digest=@digest", ("digest", Key)))));
        var epoch = (await fixture.MetadataAsync()).Epoch;
        await fixture.ExecuteAsync(Provider == "sqlite" ? """
            WITH RECURSIVE n(value) AS (SELECT 1 UNION ALL SELECT value+1 FROM n WHERE value<49998)
            INSERT INTO __kofte_sessions
            (reference_digest,user_id,mode,session_epoch,protected_ticket,created_at_utc,last_seen_at_utc,
             idle_expires_at_utc,absolute_expires_at_utc,state)
            SELECT printf('%064x',value),printf('10000000-0000-0000-0000-%012x',(value-1)/8+1),
                   'oidc',@epoch,@ticket,@now,@now,@idle,@absolute,'active' FROM n
            """ : """
            INSERT INTO kofte_system.__kofte_sessions
            (reference_digest,user_id,mode,session_epoch,protected_ticket,created_at_utc,last_seen_at_utc,
             idle_expires_at_utc,absolute_expires_at_utc,state)
            SELECT lpad(to_hex(value),64,'0'),('10000000-0000-0000-0000-' || lpad(to_hex((value-1)/8+1),12,'0'))::uuid,
                   'oidc',@epoch,@ticket,@now,@now,@idle,@absolute,'active'
            FROM generate_series(1,49998) AS n(value)
            """, ("epoch", epoch), ("ticket", new byte[] { 7 }), ("now", Now),
            ("idle", Now.AddMinutes(30)), ("absolute", Now.AddHours(8)));
    }
}

[Trait("Provider", "sqlite")]
[Collection("identity-deadlines")]
public sealed class SqliteIdentityCapacityTests : IdentityCapacityTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Collection("identity-deadlines")]
public sealed class PostgresIdentityCapacityTests : IdentityCapacityTests
{
    protected override string Provider => "postgres";
}
