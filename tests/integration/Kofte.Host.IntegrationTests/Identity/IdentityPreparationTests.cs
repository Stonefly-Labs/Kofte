using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kofte.Host.Bootstrap;
using Kofte.Infrastructure.Storage;
using Kofte.TestSupport.Identity;
using Microsoft.Data.Sqlite;
using Npgsql;
using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

public abstract class IdentityPreparationTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ExplicitPreparationRequiresRegistryUpgradeAndPreservesCurrentStateOnRetry()
    {
        await using var fixture = await IdentityFixture.CreateAsync(Provider, Token);
        Configure(fixture);
        Assert.NotEqual(0, await RunAsync(fixture, "--identity", "prepare"));
        Assert.False(Directory.Exists(fixture.KeyRingDirectory));
        Assert.Equal(0, await RunAsync(fixture, "--registry", "upgrade"));
        var installation = await ScalarAsync(fixture, "SELECT installation_id FROM {0}__kofte_installation");
        Assert.Equal(0, await RunAsync(fixture, "--identity", "prepare"));
        Assert.Equal(installation, await ScalarAsync(fixture, "SELECT installation_id FROM {0}__kofte_installation"));
        Assert.Equal(2L, Convert.ToInt64(await ScalarAsync(fixture,
            "SELECT registry_version FROM {0}__kofte_installation"), CultureInfo.InvariantCulture));
        Assert.Equal("disabled", await ScalarAsync(fixture, "SELECT state FROM {0}__kofte_bootstrap"));
        Assert.Equal(0L, await ScalarAsync(fixture, "SELECT COUNT(*) FROM {0}__kofte_users"));
        var epoch = await ScalarAsync(fixture, "SELECT session_epoch FROM {0}__kofte_identity_metadata");
        var revision = await ScalarAsync(fixture, "SELECT security_revision FROM {0}__kofte_identity_metadata");
        var keyFiles = Directory.GetFiles(fixture.KeyRingDirectory).Order(StringComparer.Ordinal).ToArray();
        Assert.NotEmpty(keyFiles);
        Assert.Equal(0, await RunAsync(fixture, "--identity", "prepare"));
        Assert.Equal(epoch, await ScalarAsync(fixture, "SELECT session_epoch FROM {0}__kofte_identity_metadata"));
        Assert.Equal(revision, await ScalarAsync(fixture, "SELECT security_revision FROM {0}__kofte_identity_metadata"));
        Assert.Equal(keyFiles, Directory.GetFiles(fixture.KeyRingDirectory).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(1L, await ScalarAsync(fixture, "SELECT COUNT(*) FROM {0}__kofte_identity_events"));
        Assert.Equal(0L, await ScalarAsync(fixture, "SELECT registry_revision FROM {0}__kofte_installation"));
    }

    [Fact]
    public async Task DirectWorkerCannotAcquireOfflineAuthority()
    {
        await using var fixture = await IdentityFixture.CreateAsync(Provider, Token);
        Configure(fixture);
        Assert.Equal(0, await RunAsync(fixture, "--registry", "upgrade"));
        Assert.NotEqual(0, await RunAsync(fixture, "--identity-worker", "prepare"));
        Assert.Equal(1L, Convert.ToInt64(await ScalarAsync(fixture,
            "SELECT registry_version FROM {0}__kofte_installation"), CultureInfo.InvariantCulture));
        Assert.False(Directory.Exists(fixture.KeyRingDirectory));
    }

    [Fact]
    public async Task CurrentDatabaseCanResumeItsOwnedPreparingRingWithoutRecreatingIdentityState()
    {
        await using var fixture = await IdentityFixture.CreateAsync(Provider, Token);
        Configure(fixture);
        Assert.Equal(0, await RunAsync(fixture, "--registry", "upgrade"));
        Assert.Equal(0, await RunAsync(fixture, "--identity", "prepare"));
        var epoch = await ScalarAsync(fixture, "SELECT session_epoch FROM {0}__kofte_identity_metadata");
        var revision = await ScalarAsync(fixture, "SELECT security_revision FROM {0}__kofte_identity_metadata");
        var marker = Path.Combine(fixture.KeyRingDirectory, "identity-owner.json");
        var owner = JsonNode.Parse(await File.ReadAllTextAsync(marker, Token))!;
        owner["state"] = "preparing";
        await File.WriteAllTextAsync(marker, owner.ToJsonString(), Token);
        Assert.Equal(0, await RunAsync(fixture, "--identity", "prepare"));
        using var ready = JsonDocument.Parse(await File.ReadAllBytesAsync(marker, Token));
        Assert.Equal("ready", ready.RootElement.GetProperty("state").GetString());
        Assert.Equal(epoch, await ScalarAsync(fixture, "SELECT session_epoch FROM {0}__kofte_identity_metadata"));
        Assert.Equal(revision, await ScalarAsync(fixture, "SELECT security_revision FROM {0}__kofte_identity_metadata"));
        Assert.Equal(1L, await ScalarAsync(fixture, "SELECT COUNT(*) FROM {0}__kofte_identity_events"));
    }

    [Fact]
    public async Task TrustChangesRequireExplicitReconciliationAndEpochRevocationIsDurable()
    {
        await using var fixture = await IdentityFixture.CreateAsync(Provider, Token);
        Configure(fixture);
        Assert.Equal(0, await RunAsync(fixture, "--registry", "upgrade"));
        Assert.Equal(0, await RunAsync(fixture, "--identity", "prepare"));
        var epoch = await ScalarAsync(fixture, "SELECT session_epoch FROM {0}__kofte_identity_metadata");
        var trust = await ScalarAsync(fixture, "SELECT trust_digest FROM {0}__kofte_identity_metadata");
        Configure(fixture, "synthetic-other");
        Assert.NotEqual(0, await RunAsync(fixture, "--identity", "prepare"));
        Assert.Equal(epoch, await ScalarAsync(fixture, "SELECT session_epoch FROM {0}__kofte_identity_metadata"));
        Assert.Equal(0, await RunAsync(fixture, "--identity", "reconcile-configuration"));
        var reconciled = await ScalarAsync(fixture, "SELECT session_epoch FROM {0}__kofte_identity_metadata");
        Assert.NotEqual(epoch, reconciled);
        Assert.NotEqual(trust, await ScalarAsync(fixture, "SELECT trust_digest FROM {0}__kofte_identity_metadata"));
        Assert.Equal(0, await RunAsync(fixture, "--identity", "revoke-sessions"));
        Assert.NotEqual(reconciled, await ScalarAsync(fixture, "SELECT session_epoch FROM {0}__kofte_identity_metadata"));
        Assert.Equal(3L, await ScalarAsync(fixture, "SELECT COUNT(*) FROM {0}__kofte_identity_events"));
        Assert.Equal("disabled", await ScalarAsync(fixture, "SELECT state FROM {0}__kofte_bootstrap"));
    }

    [Fact]
    public async Task MissingReadyKeysAreNotRegeneratedByPrepareAndStoppedReconciliationIsExplicit()
    {
        await using var fixture = await IdentityFixture.CreateAsync(Provider, Token);
        Configure(fixture);
        Assert.Equal(0, await RunAsync(fixture, "--registry", "upgrade"));
        Assert.Equal(0, await RunAsync(fixture, "--identity", "prepare"));
        var epoch = await ScalarAsync(fixture, "SELECT session_epoch FROM {0}__kofte_identity_metadata");
        var retained = Path.Combine(fixture.PrivateDirectory, "retained-keys");
        Directory.Move(fixture.KeyRingDirectory, retained);
        Assert.NotEqual(0, await RunAsync(fixture, "--identity", "prepare"));
        Assert.False(Directory.Exists(fixture.KeyRingDirectory));
        Assert.Equal(epoch, await ScalarAsync(fixture, "SELECT session_epoch FROM {0}__kofte_identity_metadata"));
        Assert.Equal(0, await RunAsync(fixture, "--identity", "reconcile-configuration"));
        Assert.NotEqual(epoch, await ScalarAsync(fixture, "SELECT session_epoch FROM {0}__kofte_identity_metadata"));
        Assert.NotEmpty(Directory.GetFiles(fixture.KeyRingDirectory));
        Assert.NotEmpty(Directory.GetFiles(retained));
        Assert.Equal(2L, await ScalarAsync(fixture, "SELECT COUNT(*) FROM {0}__kofte_identity_events"));
    }

    [Fact]
    public async Task ReconciliationPreservesConsumedDesignationAndMappingAndInvalidatesSessionsAndAttempts()
    {
        await using var fixture = await IdentityFixture.CreateAsync(Provider, Token);
        Configure(fixture);
        Assert.Equal(0, await RunAsync(fixture, "--registry", "upgrade"));
        Assert.Equal(0, await RunAsync(fixture, "--identity", "prepare"));
        var now = DateTime.UtcNow.AddMinutes(-1);
        var user = Guid.NewGuid();
        var attempt = Guid.NewGuid();
        var installationId = await ScalarAsync(fixture, "SELECT installation_id FROM {0}__kofte_installation");
        var epoch = await ScalarAsync(fixture, "SELECT session_epoch FROM {0}__kofte_identity_metadata");
        var trust = await ScalarAsync(fixture, "SELECT trust_digest FROM {0}__kofte_identity_metadata");
        await using (var connection = await OpenAsync(fixture))
        {
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 3;
            command.CommandText = Format("""
                INSERT INTO {0}__kofte_users (user_id,mode,display_name,created_at_utc,updated_at_utc)
                VALUES (@user,'oidc',NULL,@now,@now);
                INSERT INTO {0}__kofte_external_identities (mode,issuer,subject,user_id,created_at_utc)
                VALUES ('oidc','https://synthetic.invalid','synthetic-subject',@user,@now);
                UPDATE {0}__kofte_bootstrap SET state='consumed',generation=1,initial_user_id=@user,
                    consumed_at_utc=@now,consumed_generation=1 WHERE singleton_key=1;
                INSERT INTO {0}__kofte_sessions
                (reference_digest,user_id,mode,session_epoch,protected_ticket,created_at_utc,last_seen_at_utc,
                 idle_expires_at_utc,absolute_expires_at_utc,state)
                VALUES (@reference,@user,'oidc',@epoch,@ticket,@now,@now,@idle,@absolute,'active');
                INSERT INTO {0}__kofte_login_attempts
                (attempt_id,browser_digest,mode,trust_digest,session_epoch,return_path,created_at_utc,expires_at_utc,state)
                VALUES (@attempt,@reference,'oidc',@trust,@epoch,'/identity',@now,@idle,'pending');
                """);
            foreach (var (name, value) in new (string, object)[]
            {
                ("user", user), ("attempt", attempt), ("epoch", epoch!), ("trust", trust!),
                ("now", Time(now)), ("idle", Time(now.AddMinutes(30))), ("absolute", Time(now.AddHours(8))),
                ("reference", new string('a', 64)), ("ticket", new byte[] { 1 })
            })
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = Provider == "sqlite" && value is Guid id ? id.ToString("D") : value;
                command.Parameters.Add(parameter);
            }
            await command.ExecuteNonQueryAsync(Token);
        }
        Configure(fixture, "changed-synthetic-key");
        Assert.Equal(0, await RunAsync(fixture, "--identity", "reconcile-configuration"));
        Assert.Equal(0, await RunAsync(fixture, "--identity", "revoke-sessions"));
        Assert.Equal(installationId, await ScalarAsync(fixture, "SELECT installation_id FROM {0}__kofte_installation"));
        Assert.Equal("consumed", await ScalarAsync(fixture, "SELECT state FROM {0}__kofte_bootstrap"));
        Assert.Equal(user.ToString("D"), Convert.ToString(await ScalarAsync(fixture,
            "SELECT initial_user_id FROM {0}__kofte_bootstrap"), CultureInfo.InvariantCulture));
        Assert.Equal(1L, await ScalarAsync(fixture, "SELECT consumed_generation FROM {0}__kofte_bootstrap"));
        Assert.Equal(user.ToString("D"), Convert.ToString(await ScalarAsync(fixture,
            "SELECT user_id FROM {0}__kofte_external_identities WHERE subject='synthetic-subject'"),
            CultureInfo.InvariantCulture));
        Assert.Equal("revoked", await ScalarAsync(fixture, "SELECT state FROM {0}__kofte_sessions"));
        Assert.Equal("consumed", await ScalarAsync(fixture, "SELECT state FROM {0}__kofte_login_attempts"));
        Assert.NotEqual(epoch, await ScalarAsync(fixture, "SELECT session_epoch FROM {0}__kofte_identity_metadata"));
        Assert.Equal(3L, await ScalarAsync(fixture, "SELECT security_revision FROM {0}__kofte_identity_metadata"));
        Assert.Equal(0L, await ScalarAsync(fixture, "SELECT registry_revision FROM {0}__kofte_installation"));
    }

    [Fact]
    public async Task ARealHeldHostLeaseRefusesEveryStoppedCommandWithoutChangingIdentity()
    {
        await using var fixture = await IdentityFixture.CreateAsync(Provider, Token);
        Configure(fixture);
        Assert.Equal(0, await RunAsync(fixture, "--registry", "upgrade"));
        Assert.Equal(0, await RunAsync(fixture, "--identity", "prepare"));
        var epoch = await ScalarAsync(fixture, "SELECT session_epoch FROM {0}__kofte_identity_metadata");
        var lease = Provider == "sqlite" ? Kofte.Host.Bootstrap.StoreLease.Acquire(fixture.Storage.DataRoot) : null;
        var postgres = Provider == "postgres"
            ? await new PostgresPlatformStore(fixture.Storage.Postgres!.ConnectionString).AcquireHostLeaseAsync(Token) : null;
        try
        {
            foreach (var operation in new[] { "prepare", "reconcile-configuration", "revoke-sessions" })
                Assert.NotEqual(0, await RunAsync(fixture, "--identity", operation));
            Assert.Equal(epoch, await ScalarAsync(fixture, "SELECT session_epoch FROM {0}__kofte_identity_metadata"));
            Assert.Equal(1L, await ScalarAsync(fixture, "SELECT security_revision FROM {0}__kofte_identity_metadata"));
        }
        finally
        {
            lease?.Dispose();
            if (postgres is not null) await postgres.DisposeAsync();
        }
        Assert.Equal(0, await RunAsync(fixture, "--identity", "prepare"));
    }

    private static void Configure(IdentityFixture fixture, string key = "synthetic") =>
        fixture.WriteConfiguration(JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = "1",
            mode = "development",
            publicOrigin = fixture.WebUrl.GetLeftPart(UriPartial.Authority),
            keyRingDirectory = fixture.KeyRingDirectory,
            development = new { enabled = true, identities = new[] { new { key, displayName = "Synthetic" } } }
        }));

    private async Task<int> RunAsync(IdentityFixture fixture, params string[] arguments)
    {
        await using var process = fixture.Start(arguments);
        var output = await process.WaitAsync(fixture.Storage.Token);
        Assert.DoesNotContain(fixture.PrivateDirectory, output.Stdout + output.Stderr, StringComparison.Ordinal);
        Assert.DoesNotContain(fixture.Canary, output.Stdout + output.Stderr, StringComparison.Ordinal);
        Assert.True(process.ExitVerified && process.DescendantExitsVerified);
        return output.ExitCode;
    }

    private async Task<object?> ScalarAsync(IdentityFixture fixture, string sql)
    {
        await using var connection = await OpenAsync(fixture);
        await using var command = connection.CreateCommand();
        command.CommandText = Format(sql);
        command.CommandTimeout = 3;
        return await command.ExecuteScalarAsync(Token);
    }

    private object Time(DateTime value) => Provider == "sqlite" ? value.ToString("O", CultureInfo.InvariantCulture) : value;
    private string Format(string sql) => string.Format(CultureInfo.InvariantCulture, sql,
        Provider == "sqlite" ? "" : "kofte_system.");

    private async Task<DbConnection> OpenAsync(IdentityFixture fixture)
    {
        DbConnection connection = Provider == "sqlite"
            ? new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(fixture.Storage.DataRoot, "platform", "platform.db"),
                Mode = SqliteOpenMode.ReadWrite,
                Pooling = false
            }.ConnectionString)
            : new NpgsqlConnection(fixture.Storage.Postgres!.ConnectionString);
        try { await connection.OpenAsync(Token); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteIdentityPreparationTests : IdentityPreparationTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresIdentityPreparationTests : IdentityPreparationTests
{
    protected override string Provider => "postgres";
}
