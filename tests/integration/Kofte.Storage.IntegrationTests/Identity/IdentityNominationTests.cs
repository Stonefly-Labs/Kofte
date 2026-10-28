using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;
using Kofte.TestSupport.Identity;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Kofte.Storage.IntegrationTests.Identity;

public abstract class IdentityNominationTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;
    private const string Issuer = "https://offline-issuer.invalid";
    private const string Subject = "synthetic-nominated-subject";

    [Theory]
    [InlineData("bootstrap_file_created", 0, false)]
    [InlineData("bootstrap_file_flushed", 43, false)]
    [InlineData("operator_before_commit", 43, false)]
    [InlineData("operator_committed", 43, true)]
    public async Task ActualPrivateFileAndDatabaseInterruptionRetainsEachSideAndRequiresStoppedReplacement(
        string phase, int deliveredLength, bool committed)
    {
        using var identity = new ServingIdentity("Production");
        await using var fixture = await PreparedAsync(identity);
        var nominee = Nominee(fixture);
        var authorization = Path.Combine(fixture.PrivateDirectory, "authorization");
        var before = await SnapshotAsync(fixture);
        var barrier = new IdentityCommitBarrier(fixture.PrivateDirectory, phase, maintenanceChild: true);
        await using var process = fixture.Start(["--identity", "bootstrap", "authorize", "--nominee-file",
            nominee, "--authorization-file", authorization], environment: "Production", barrier: barrier);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(Token);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        await barrier.WaitAsync(deadline.Token);
        Assert.NotEqual(process.ProcessId, barrier.ObservedProcessId);
        var output = await process.InterruptMaintenanceChildAsync(barrier, deadline.Token);
        Assert.NotEqual(0, output.ExitCode);
        Assert.True(process.ExitVerified && process.DescendantExitsVerified);
        Assert.True(File.Exists(authorization));
        var delivered = await File.ReadAllTextAsync(authorization, Token);
        Assert.Equal(deliveredLength, delivered.Length);
        var after = await SnapshotAsync(fixture);
        Assert.Equal(committed ? "pending" : before.State, after.State);
        Assert.Equal(before.Generation + (committed ? 1 : 0), after.Generation);
        Assert.Equal(before.Revision + (committed ? 1 : 0), after.Revision);
        Assert.Equal(before.Events + (committed ? 1 : 0), after.Events);
        if (!committed) Assert.Equal(before, after);
        if (delivered.Length != 0)
        {
            Assert.Matches("^[A-Za-z0-9_-]{43}$", delivered);
            Assert.DoesNotContain(delivered, output.Stdout + output.Stderr, StringComparison.Ordinal);
        }
        Assert.DoesNotContain(fixture.PrivateDirectory, output.Stdout + output.Stderr, StringComparison.Ordinal);
        Assert.DoesNotContain(Subject, output.Stdout + output.Stderr, StringComparison.Ordinal);
        var replacement = Path.Combine(fixture.PrivateDirectory, "replacement");
        var replaced = await RunAsync(fixture, "--identity", "bootstrap", "authorize", "--nominee-file",
            nominee, "--authorization-file", replacement);
        var safeCodes = Enum.GetValues<StorageCode>().Select(code => StorageDiagnostics.Diagnostic(code).Code)
            .Concat(Enum.GetValues<IdentityDiagnosticOutcome>().Select(outcome => IdentityDiagnostics.Rule(outcome).Code));
        Assert.True(replaced.ExitCode == 0, string.Join(",", safeCodes.Where(code =>
            (replaced.Stdout + replaced.Stderr).Contains(code, StringComparison.Ordinal))));
        Assert.Equal(after.Generation + 1, (await SnapshotAsync(fixture)).Generation);
        Assert.Equal(delivered, await File.ReadAllTextAsync(authorization, Token));
        Assert.NotEqual(delivered, await File.ReadAllTextAsync(replacement, Token));
    }

    [Fact]
    public async Task DirectUnadmittedBootstrapWorkerCannotDeliverOrMutate()
    {
        using var identity = new ServingIdentity("Production");
        await using var fixture = await PreparedAsync(identity);
        var nominee = Nominee(fixture);
        var authorization = Path.Combine(fixture.PrivateDirectory, "authorization");
        var before = await SnapshotAsync(fixture);
        var output = await RunAsync(fixture, "--identity-worker", "bootstrap", "authorize",
            "--nominee-file", nominee, "--authorization-file", authorization);
        Assert.NotEqual(0, output.ExitCode);
        Assert.Equal(before, await SnapshotAsync(fixture));
        Assert.False(File.Exists(authorization));
    }

    [Theory]
    [InlineData("consumed")]
    [InlineData("recovery_locked")]
    public async Task StoppedOperationsNeverReopenConsumedOrHistoricallyUncertainBootstrap(string state)
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        using (var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token))
            await fixture.Store().MutateAsync(budget, async change =>
            {
                var now = DateTime.UtcNow;
                var user = state == "consumed"
                    ? await IdentityUserStore.ResolveAsync(change, Issuer, Subject, null, now) : null;
                await change.Database.QueryAsync($"""
                    INSERT INTO {change.Database.Prefix}__kofte_bootstrap
                    (singleton_key,installation_id,state,generation,initial_user_mode,initial_user_id,
                     consumed_at_utc,consumed_generation)
                    VALUES (1,@installation,@state,@generation,'oidc',@user,@now,@consumed)
                    """, ("installation", fixture.InstallationId), ("state", state),
                    ("generation", user is null ? 0L : 1L), ("user", user?.UserId),
                    ("now", user is null ? null : now), ("consumed", user is null ? null : (long?)1));
                if (user is not null)
                    await IdentityEventStore.AppendAsync(change, IdentityEventCode.InitialAdministratorEnrolled,
                        now, user.UserId);
                return true;
            });
        var metadata = await fixture.MetadataAsync();
        var events = await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_events");
        await fixture.ReleaseRuntimeLeaseAsync();
        await using var scope = await fixture.RegistryFixture.Maintenance.AcquireAsync(Token);
        using var stopped = new OperationBudget(TimeSpan.FromSeconds(10), Token);
        var delivered = false;
        var authorize = await Assert.ThrowsAsync<IdentityStateException>(() =>
            IdentityBootstrapStore.AuthorizeAsync(fixture.RegistryFixture.Installation,
                Assert.IsType<MaintenanceScope>(scope), fixture.InstallationId, "oidc", new string('a', 64),
                Issuer, Subject, new string('A', 43), () => { delivered = true; return Task.CompletedTask; }, stopped));
        Assert.Equal(IdentityDiagnosticOutcome.EnrollmentUnavailable, authorize.Outcome);
        var disable = await Assert.ThrowsAsync<IdentityStateException>(() =>
            IdentityBootstrapStore.DisablePendingAsync(fixture.RegistryFixture.Installation,
                Assert.IsType<MaintenanceScope>(scope), fixture.InstallationId, "oidc", new string('a', 64), stopped));
        Assert.Equal(IdentityDiagnosticOutcome.EnrollmentUnavailable, disable.Outcome);
        Assert.False(delivered);
        Assert.Equal(metadata, await fixture.MetadataAsync());
        Assert.Equal(events, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_events"));
        Assert.Equal(state, await fixture.ScalarAsync("SELECT state FROM {0}__kofte_bootstrap"));
    }

    [Fact]
    public async Task AForeignStoppedScopeCannotReachDeliveryOrChangeIdentityState()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        await using var foreign = await RegistryFixture.CreateAsync("sqlite", Token);
        var before = await fixture.MetadataAsync();
        await using var scope = await foreign.Maintenance.AcquireAsync(Token);
        using var budget = new OperationBudget(TimeSpan.FromSeconds(10), Token);
        var delivered = false;
        await Assert.ThrowsAsync<StorageException>(() => IdentityBootstrapStore.AuthorizeAsync(
            fixture.RegistryFixture.Installation, Assert.IsType<MaintenanceScope>(scope), fixture.InstallationId,
            "oidc", new string('a', 64), Issuer, Subject, new string('A', 43),
            () => { delivered = true; return Task.CompletedTask; }, budget));
        Assert.False(delivered);
        Assert.Equal(before, await fixture.MetadataAsync());
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_events"));
    }

    [Fact]
    public async Task ARealLiveHostLeaseRefusesStoppedNominationAuthority()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        var before = await fixture.MetadataAsync();
        var refused = await Assert.ThrowsAsync<StorageException>(async () =>
        {
            await using var scope = await MaintenanceScope.AcquireAsync(
                fixture.RegistryFixture.Installation, Token);
        });
        Assert.Equal(StorageCode.Busy, refused.Code);
        Assert.Equal(before, await fixture.MetadataAsync());
        Assert.Equal(0L, await fixture.ScalarAsync("SELECT COUNT(*) FROM {0}__kofte_identity_events"));
    }

    [Fact]
    public async Task StoppedOwnerNominationBindsExactInstallationGenerationAndNominee()
    {
        await using var fixture = await AdmissionFixture.CreateAsync(Provider, Token);
        await fixture.ExecuteAsync("""
            INSERT INTO {0}__kofte_bootstrap
              (singleton_key,installation_id,state,generation,initial_user_mode)
            VALUES (1,@installation,'disabled',0,'oidc')
            """, ("installation", fixture.InstallationId));
        await fixture.ReleaseRuntimeLeaseAsync();
        await using var scope = await fixture.RegistryFixture.Maintenance.AcquireAsync(Token);
        using var budget = new OperationBudget(TimeSpan.FromSeconds(10), Token);
        var delivered = false;
        var secret = new string('A', 43);
        await IdentityBootstrapStore.AuthorizeAsync(fixture.RegistryFixture.Installation,
            Assert.IsType<MaintenanceScope>(scope), fixture.InstallationId, "oidc", new string('a', 64),
            Issuer, Subject, secret, () => { delivered = true; return Task.CompletedTask; }, budget);
        Assert.True(delivered);
        var expected = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"kofte.identity.bootstrap.authorization.v1\0{fixture.InstallationId:D}\0" +
            $"1\0{Issuer}\0{Subject}\0{secret}")));
        Assert.Equal(expected, await fixture.ScalarAsync("SELECT authorization_digest FROM {0}__kofte_bootstrap"));
        Assert.Equal("pending", await fixture.ScalarAsync("SELECT state FROM {0}__kofte_bootstrap"));
        Assert.Equal(1, (await fixture.MetadataAsync()).Revision);
        Assert.Equal("bootstrap_authorized",
            await fixture.ScalarAsync("SELECT event_code FROM {0}__kofte_identity_events"));
    }

    [Fact]
    public async Task ActualStoppedNominationPrivatelyDeliversReplacesAndDisablesOnlyPendingAuthority()
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        using var identity = new ServingIdentity("Production");
        await using var fixture = await PreparedAsync(identity);
        var nominee = Nominee(fixture);
        var authorization = Path.Combine(fixture.PrivateDirectory, "authorization");
        var before = await SnapshotAsync(fixture);
        var began = DateTime.UtcNow;
        var authorized = await RunAsync(fixture, "--identity", "bootstrap", "authorize",
            "--nominee-file", nominee, "--authorization-file", authorization);
        Assert.Equal(0, authorized.ExitCode);
        var secret = await File.ReadAllTextAsync(authorization, Token);
        Assert.Matches("^[A-Za-z0-9_-]{43}$", secret);
        Assert.Equal(32, Convert.FromBase64String(secret.Replace('-', '+').Replace('_', '/') + "=").Length);
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(authorization));
        Assert.DoesNotContain(secret, authorized.Stdout + authorized.Stderr, StringComparison.Ordinal);
        var pending = await SnapshotAsync(fixture);
        Assert.Equal("pending", pending.State);
        Assert.Equal(before.Generation + 1, pending.Generation);
        Assert.Equal(before.Revision + 1, pending.Revision);
        Assert.Equal(before.Events + 1, pending.Events);
        Assert.Equal(Issuer, pending.Issuer);
        Assert.Equal(Subject, pending.Subject);
        Assert.Matches("^[0-9a-f]{64}$", pending.Digest!);
        Assert.NotEqual(secret, pending.Digest);
        Assert.InRange(pending.Expires!.Value, began.AddMinutes(15), DateTime.UtcNow.AddMinutes(15));
        Assert.Null(pending.InitialUser);
        var replacement = Path.Combine(fixture.PrivateDirectory, "replacement");
        Assert.Equal(0, (await RunAsync(fixture, "--identity", "bootstrap", "authorize",
            "--nominee-file", nominee, "--authorization-file", replacement)).ExitCode);
        Assert.NotEqual(secret, await File.ReadAllTextAsync(replacement, Token));
        Assert.Equal(secret, await File.ReadAllTextAsync(authorization, Token));
        var replaced = await SnapshotAsync(fixture);
        Assert.Equal(pending.Generation + 1, replaced.Generation);
        Assert.NotEqual(pending.Digest, replaced.Digest);
        Assert.Equal(0, (await RunAsync(fixture, "--identity", "bootstrap", "disable-pending")).ExitCode);
        var disabled = await SnapshotAsync(fixture);
        Assert.Equal("disabled", disabled.State);
        Assert.Equal(replaced.Generation, disabled.Generation);
        Assert.Equal(replaced.Revision + 1, disabled.Revision);
        Assert.Equal(replaced.Events + 1, disabled.Events);
        Assert.Null(disabled.Issuer);
        Assert.Null(disabled.Subject);
        Assert.Null(disabled.Digest);
        Assert.Null(disabled.Expires);
        Assert.Null(disabled.InitialUser);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("unknown")]
    [InlineData("issuer")]
    [InlineData("subject")]
    [InlineData("public-file")]
    [InlineData("symlink")]
    [InlineData("existing-output")]
    public async Task InvalidNomineeOrPrivateDeliveryCannotChangeCeremonyRevisionOrEvents(string fault)
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        using var identity = new ServingIdentity("Production");
        await using var fixture = await PreparedAsync(identity);
        var nominee = Nominee(fixture, fault switch
        {
            "duplicate" => $$"""{"version":"1","version":"1","issuer":"{{Issuer}}","subject":"{{Subject}}"}""",
            "unknown" => $$"""{"version":"1","issuer":"{{Issuer}}","subject":"{{Subject}}","role":"administrator"}""",
            "issuer" => $$"""{"version":"1","issuer":"https://foreign.invalid","subject":"{{Subject}}"}""",
            "subject" => $$"""{"version":"1","issuer":"{{Issuer}}","subject":""}""",
            _ => null
        });
        if (fault == "public-file")
            File.SetUnixFileMode(nominee, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
        if (fault == "symlink")
        {
            var link = Path.Combine(fixture.PrivateDirectory, "nominee-link");
            File.CreateSymbolicLink(link, nominee);
            nominee = link;
        }
        var authorization = Path.Combine(fixture.PrivateDirectory, "authorization");
        if (fault == "existing-output") PrivateTestFiles.WriteNew(authorization, "synthetic-existing"u8);
        var before = await SnapshotAsync(fixture);
        var result = await RunAsync(fixture, "--identity", "bootstrap", "authorize",
            "--nominee-file", nominee, "--authorization-file", authorization);
        Assert.NotEqual(0, result.ExitCode);
        Assert.Equal(before, await SnapshotAsync(fixture));
        if (fault == "existing-output")
            Assert.Equal("synthetic-existing", await File.ReadAllTextAsync(authorization, Token));
        else Assert.False(File.Exists(authorization));
    }

    private async Task<IdentityFixture> PreparedAsync(ServingIdentity identity)
    {
        var fixture = await IdentityFixture.CreateAsync(Provider, Token);
        try
        {
            fixture.WriteConfiguration(await File.ReadAllBytesAsync(identity.ConfigurationPath, Token));
            Assert.Equal(0, (await RunAsync(fixture, "--registry", "upgrade")).ExitCode);
            Assert.Equal(0, (await RunAsync(fixture, "--identity", "prepare")).ExitCode);
            return fixture;
        }
        catch { await fixture.DisposeAsync(); throw; }
    }

    private static string Nominee(IdentityFixture fixture, string? json = null)
    {
        var path = Path.Combine(fixture.PrivateDirectory, "nominee.json");
        PrivateTestFiles.WriteNew(path, Encoding.UTF8.GetBytes(json ??
            $$"""{"version":"1","issuer":"{{Issuer}}","subject":"{{Subject}}"}"""));
        return path;
    }

    private static async Task<ProcessOutput> RunAsync(IdentityFixture fixture, params string[] arguments)
    {
        await using var process = fixture.Start(arguments, environment: "Production");
        var result = await process.WaitAsync(fixture.Storage.Token);
        Assert.True(process.ExitVerified && process.DescendantExitsVerified);
        Assert.DoesNotContain(fixture.PrivateDirectory, result.Stdout + result.Stderr, StringComparison.Ordinal);
        Assert.DoesNotContain(Subject, result.Stdout + result.Stderr, StringComparison.Ordinal);
        Assert.DoesNotContain(Issuer, result.Stdout + result.Stderr, StringComparison.Ordinal);
        return result;
    }

    private sealed record Snapshot(string State, long Generation, string? Issuer, string? Subject,
        string? Digest, DateTime? Expires, object? InitialUser, long Revision, long Events);

    private async Task<Snapshot> SnapshotAsync(IdentityFixture fixture)
    {
        await using DbConnection connection = Provider == "sqlite"
            ? new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(fixture.Storage.DataRoot, "platform", "platform.db"),
                Mode = SqliteOpenMode.ReadWrite,
                Pooling = false
            }.ConnectionString)
            : new NpgsqlConnection(fixture.Storage.Postgres!.ConnectionString);
        await connection.OpenAsync(Token);
        await using var command = connection.CreateCommand();
        var prefix = Provider == "sqlite" ? "" : "kofte_system.";
        command.CommandText = $"""
            SELECT b.state,b.generation,b.nominee_issuer,b.nominee_subject,b.authorization_digest,b.expires_at_utc,
              b.initial_user_id,m.security_revision,(SELECT COUNT(*) FROM {prefix}__kofte_identity_events)
            FROM {prefix}__kofte_bootstrap b CROSS JOIN {prefix}__kofte_identity_metadata m
            """;
        await using var reader = await command.ExecuteReaderAsync(Token);
        Assert.True(await reader.ReadAsync(Token));
        string? Optional(int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
        var expires = reader.IsDBNull(5) ? (DateTime?)null : reader.GetValue(5) is DateTime value
            ? value : DateTime.ParseExact(reader.GetString(5), "O", CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
        var result = new Snapshot(reader.GetString(0), reader.GetInt64(1), Optional(2), Optional(3),
            Optional(4), expires, reader.IsDBNull(6) ? null : reader.GetValue(6), reader.GetInt64(7), reader.GetInt64(8));
        Assert.False(await reader.ReadAsync(Token));
        return result;
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteIdentityNominationTests : IdentityNominationTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresIdentityNominationTests : IdentityNominationTests
{
    protected override string Provider => "postgres";
}
