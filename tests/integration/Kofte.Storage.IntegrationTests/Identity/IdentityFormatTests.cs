using System.Data.Common;
using System.Security.Cryptography;
using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Modules.Alpha;
using Kofte.Storage.IntegrationTests.Providers;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kofte.Storage.IntegrationTests.Identity;

public abstract class IdentityFormatTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;
    private string OriginalDigest => Provider == "sqlite"
        ? "5d33f7aaa2c2c1af379af08816fc67dce412f53f538709adb9c233ebc43a4809"
        : "c129c9ab66618acd157ac7e4114073228463fa3880dbe1da4f863ce05648de6e";

    [Fact]
    public async Task FormatTwoPreservesExactRegistryAssignmentsMarkersHistoryAndModuleData()
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, Token);
        await using var scope = await fixture.Maintenance.AcquireAsync(Token);
        var alpha = RegistryFixture.Module("org.example.alpha");
        Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(alpha, scope, Token));
        Assert.IsType<StorageResult.NoStorage>(await fixture.Registry.RegisterAsync(
            RegistryFixture.Module("org.example.identity-stateless", relational: false), scope, Token));
        await using var binding = await fixture.Registry.BindAsync(alpha.Id, Token);
        Assert.IsType<StorageResult.Ready>(
            await binding.MigrateAsync(ModuleFixture.AlphaFactory, scope, cancellationToken: Token));
        await using (var context = await binding.CreateContextAsync(ModuleFixture.AlphaFactory, Token))
        {
            context.Records.Add(new AlphaRecord { Id = 1, Payload = "preserved-module-payload", Note = "preserved-note" });
            await context.SaveChangesAsync(Token);
            Assert.Equal(["202610210001_Initial", "202610210002_AddNote"],
                await context.Database.GetAppliedMigrationsAsync(Token));
        }
        var assignment = Assert.IsType<StorageResult.Ready>(
            await fixture.Registry.ResolveAsync(alpha.Id, Token)).Assignment;
        var before = await RecoverySetVerifier.InspectStoppedAsync(fixture.Installation, scope, Token);
        using var budget = new OperationBudget(OperationBudget.Register, Token);
        await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
        var runner = new RegistryMigrationRunner(fixture.Installation);
        Assert.Equal(OriginalDigest, runner.Digest);
        await ApplyAsync(runner, scope, connection, budget);
        var after = await runner.ReadInstallationAsync(connection, budget);
        Assert.Equal(2, after.Version);
        Assert.Equal(before.InstallationId, after.Id);
        Assert.Equal(before.RegistryRevision, after.Revision);
        Assert.Equal(JsonSerializer.Serialize(before with { RegistryVersion = 2 }),
            JsonSerializer.Serialize(await RecoverySetVerifier.InspectStoppedAsync(fixture.Installation, scope, Token)));
        await using (var context = await binding.CreateContextAsync(ModuleFixture.AlphaFactory, Token))
        {
            var row = await context.Records.SingleAsync(Token);
            Assert.Equal((1, "preserved-module-payload", "preserved-note"), (row.Id, row.Payload, row.Note));
            Assert.Equal(["202610210001_Initial", "202610210002_AddNote"],
                await context.Database.GetAppliedMigrationsAsync(Token));
        }
        var resolved = Assert.IsType<StorageResult.Ready>(
            await fixture.Registry.ResolveAsync(alpha.Id, Token)).Assignment;
        Assert.Equal(StorageAssignmentSnapshot.From(assignment), StorageAssignmentSnapshot.From(resolved));
        await ApplyAsync(runner, scope, connection, budget);
        Assert.Equal(after, await runner.ReadInstallationAsync(connection, budget));
        var prefix = Provider == "sqlite" ? "" : "kofte_system.";
        await using var history = RegistryMigrationRunner.Command(connection,
            $"SELECT version,sha256 FROM {prefix}__kofte_registry_migrations ORDER BY version", budget);
        await using var rows = await history.ExecuteReaderAsync(Token);
        Assert.True(await rows.ReadAsync(Token));
        Assert.Equal(1, rows.GetInt32(0));
        Assert.Equal(OriginalDigest, rows.GetString(1));
        Assert.True(await rows.ReadAsync(Token));
        Assert.Equal(2, rows.GetInt32(0));
        var source = Path.Combine(OwnedProcesses.FindRepository(), "src", "Kofte.Infrastructure", "Storage",
            "Registry", "Migrations", Provider == "sqlite" ? "Sqlite" : "Postgres", "002_identity.sql");
        Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(source, Token))),
            rows.GetString(1));
        Assert.False(await rows.ReadAsync(Token));
    }

    [Fact]
    public async Task MigrationTransactionRollsBackWithoutChangingFormatOne()
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, Token);
        await using var scope = await fixture.Maintenance.AcquireAsync(Token);
        using var budget = new OperationBudget(OperationBudget.Register, Token);
        await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
        var runner = new RegistryMigrationRunner(fixture.Installation);
        var before = await runner.ReadInstallationAsync(connection, budget);
        await using (var transaction = await connection.BeginTransactionAsync(Token))
        {
            await runner.ApplyIdentityFormatAsync(Assert.IsType<MaintenanceScope>(scope), connection, transaction, budget);
            await transaction.RollbackAsync(Token);
        }
        Assert.Equal(before, await runner.ReadInstallationAsync(connection, budget));
        Assert.Equal(OriginalDigest, runner.Digest);
    }

    [Fact]
    public async Task TableFreeVersionZeroRequiresTheSeparateRegistryUpgrade()
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, Token, upgrade: false);
        await using var scope = await fixture.Maintenance.AcquireAsync(Token);
        using var budget = new OperationBudget(OperationBudget.Register, Token);
        await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
        await Assert.ThrowsAsync<StorageException>(() =>
            ApplyAsync(new(fixture.Installation), scope, connection, budget));
        Assert.False(await new RegistryMigrationRunner(fixture.Installation).HasRegistryAsync(connection, budget));
    }

    [Fact]
    public async Task UnknownVersionIsPreservedRatherThanReinterpretedOrRepaired()
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, Token);
        await using var scope = await fixture.Maintenance.AcquireAsync(Token);
        using var budget = new OperationBudget(OperationBudget.Register, Token);
        await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
        var runner = new RegistryMigrationRunner(fixture.Installation);
        await ApplyAsync(runner, scope, connection, budget);
        var mutation = Provider == "sqlite" ? "PRAGMA user_version=9" : """
            DROP TRIGGER __kofte_installation_change ON kofte_system.__kofte_installation;
            ALTER TABLE kofte_system.__kofte_installation DROP CONSTRAINT __kofte_installation_registry_version_check;
            UPDATE kofte_system.__kofte_installation SET registry_version=9;
            """;
        await RegistryMigrationRunner.ExecuteAsync(connection, mutation, budget);
        await Assert.ThrowsAsync<StorageException>(() => ApplyAsync(runner, scope, connection, budget));
        var query = Provider == "sqlite" ? "PRAGMA user_version" :
            "SELECT registry_version FROM kofte_system.__kofte_installation";
        Assert.Equal(9, Convert.ToInt32(await RegistryMigrationRunner.ScalarAsync(connection, query, budget),
            System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task ForeignStoppedScopeCannotAuthorizeAnIdentityMigration()
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, Token);
        await using var foreign = await RegistryFixture.CreateAsync(Provider, Token);
        await using var scope = await fixture.Maintenance.AcquireAsync(Token);
        await using var foreignScope = await foreign.Maintenance.AcquireAsync(Token);
        using var budget = new OperationBudget(OperationBudget.Register, Token);
        await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
        var runner = new RegistryMigrationRunner(fixture.Installation);
        var before = await runner.ReadInstallationAsync(connection, budget);
        var error = await Assert.ThrowsAsync<StorageException>(() =>
            ApplyAsync(runner, foreignScope, connection, budget));
        Assert.Equal(StorageCode.RequestInvalid, error.Code);
        Assert.Equal(before, await runner.ReadInstallationAsync(connection, budget));
    }

    [Theory]
    [InlineData("table")]
    [InlineData("view")]
    [InlineData("column-default")]
    [InlineData("missing-index")]
    [InlineData("wrong-collation")]
    [InlineData("history")]
    [InlineData("trigger")]
    [InlineData("check")]
    [InlineData("foreign-key")]
    [InlineData("unique")]
    [InlineData("nullability")]
    [InlineData("generated")]
    public Task PhysicalOrHistoryTamperingIsRefusedWithoutRepair(string alteration) =>
        AssertAlterationAsync(alteration);

    protected async Task AssertAlterationAsync(string alteration)
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, Token);
        await using var scope = await fixture.Maintenance.AcquireAsync(Token);
        using var budget = new OperationBudget(OperationBudget.Register, Token);
        await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
        var runner = new RegistryMigrationRunner(fixture.Installation);
        await ApplyAsync(runner, scope, connection, budget);
        var p = Provider == "sqlite" ? "" : "kofte_system.";
        var sql = alteration switch
        {
            "table" => $"CREATE TABLE {p}unexpected_identity_data (payload TEXT)",
            "view" => $"CREATE VIEW {p}unexpected_identity_view AS SELECT user_id FROM {p}__kofte_users",
            "column-default" => $"ALTER TABLE {p}__kofte_users ADD COLUMN unexpected INTEGER DEFAULT 1",
            "missing-index" => $"DROP INDEX {p}__kofte_sessions_user",
            "wrong-collation" => $"DROP INDEX {p}__kofte_attempts_browser; CREATE INDEX __kofte_attempts_browser ON {p}__kofte_login_attempts " +
                (Provider == "sqlite" ? "(browser_digest COLLATE NOCASE,state,expires_at_utc)" :
                    "(browser_digest COLLATE \"POSIX\",state,expires_at_utc)"),
            "history" => $"UPDATE {p}__kofte_registry_migrations SET sha256='{new string('0', 64)}' WHERE version=2",
            "trigger" => Provider == "sqlite"
                ? "CREATE TRIGGER unexpected_identity_trigger BEFORE DELETE ON __kofte_users BEGIN SELECT 1; END"
                : "CREATE TRIGGER unexpected_identity_trigger BEFORE DELETE ON kofte_system.__kofte_users " +
                    "FOR EACH ROW EXECUTE FUNCTION kofte_system.__kofte_event_change()",
            "routine" => "CREATE FUNCTION kofte_system.unexpected_identity_routine() RETURNS integer LANGUAGE sql AS 'SELECT 1'",
            "aggregate" => "CREATE AGGREGATE kofte_system.unexpected_identity_aggregate(integer) " +
                "(SFUNC=int4pl,STYPE=integer,INITCOND='0')",
            "check" => $"ALTER TABLE {p}__kofte_users DROP CONSTRAINT __kofte_users_mode_check",
            "foreign-key" => $"ALTER TABLE {p}__kofte_external_identities DROP CONSTRAINT __kofte_external_identities_user_id_mode_fkey",
            "unique" => $"ALTER TABLE {p}__kofte_external_identities DROP CONSTRAINT __kofte_external_identities_user_id_key",
            "nullability" => $"ALTER TABLE {p}__kofte_identity_metadata ALTER COLUMN trust_digest DROP NOT NULL",
            "generated" => $"ALTER TABLE {p}__kofte_users ADD COLUMN unexpected INTEGER GENERATED ALWAYS AS (1) " +
                (Provider == "sqlite" ? "VIRTUAL" : "STORED"),
            _ => throw new InvalidOperationException("test_alteration_invalid")
        };
        if (Provider == "sqlite" && alteration is "check" or "foreign-key" or "unique" or "nullability")
        {
            var (table, original, changed) = alteration switch
            {
                "check" => ("__kofte_users", "CHECK (mode IN ('oidc', 'development'))", ""),
                "foreign-key" => ("__kofte_external_identities",
                    "FOREIGN KEY (user_id, mode) REFERENCES __kofte_users (user_id, mode),", ""),
                "unique" => ("__kofte_external_identities",
                    "user_id TEXT COLLATE BINARY NOT NULL UNIQUE", "user_id TEXT COLLATE BINARY NOT NULL"),
                "nullability" => ("__kofte_identity_metadata",
                    "trust_digest TEXT COLLATE BINARY NOT NULL", "trust_digest TEXT COLLATE BINARY"),
                _ => throw new InvalidOperationException("test_alteration_invalid")
            };
            await using var definition = RegistryMigrationRunner.Command(connection,
                "SELECT sql FROM sqlite_master WHERE type='table' AND name=@table", budget, null, ("table", table));
            var ddl = Assert.IsType<string>(await definition.ExecuteScalarAsync(Token));
            var altered = ddl.Replace(original, changed, StringComparison.Ordinal);
            Assert.NotEqual(ddl, altered);
            sql = $"DROP TABLE {table};{altered}";
        }
        await RegistryMigrationRunner.ExecuteAsync(connection, sql, budget);
        var code = await Assert.ThrowsAsync<StorageException>(() => runner.ReadInstallationAsync(connection, budget));
        Assert.Equal(StorageCode.Corrupt, code.Code);
        await Assert.ThrowsAsync<StorageException>(() => ApplyAsync(runner, scope, connection, budget));
    }

    [Fact]
    public async Task VersionZeroWithAnUnownedViewIsRefusedBeforeRegistryAllocation()
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, Token, upgrade: false);
        await using var scope = await fixture.Maintenance.AcquireAsync(Token);
        using var budget = new OperationBudget(OperationBudget.Register, Token);
        await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
        var p = Provider == "sqlite" ? "" : "kofte_system.";
        await RegistryMigrationRunner.ExecuteAsync(connection,
            $"CREATE VIEW {p}unexpected_identity_view AS SELECT 1 AS sentinel", budget);
        await Assert.ThrowsAsync<StorageException>(() => fixture.Maintenance.UpgradeAsync(scope, Token));
        Assert.False(await new RegistryMigrationRunner(fixture.Installation).HasRegistryAsync(connection, budget));
        Assert.Equal(1, Convert.ToInt32(await RegistryMigrationRunner.ScalarAsync(connection,
            $"SELECT sentinel FROM {p}unexpected_identity_view", budget), System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task IndependentInventoryHasExactlyTheThreeRegistryAndEightIdentityTables()
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, Token);
        await using var scope = await fixture.Maintenance.AcquireAsync(Token);
        using var budget = new OperationBudget(OperationBudget.Register, Token);
        await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
        await ApplyAsync(new(fixture.Installation), scope, connection, budget);
        var query = Provider == "sqlite"
            ? "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name"
            : "SELECT tablename FROM pg_catalog.pg_tables WHERE schemaname='kofte_system' ORDER BY tablename";
        await using var command = RegistryMigrationRunner.Command(connection, query, budget);
        await using var reader = await command.ExecuteReaderAsync(Token);
        var actual = new List<string>();
        while (await reader.ReadAsync(Token)) actual.Add(reader.GetString(0));
        Assert.Equal(new[]
        {
            "__kofte_bootstrap", "__kofte_external_identities", "__kofte_identity_admission",
            "__kofte_identity_events", "__kofte_identity_metadata", "__kofte_installation",
            "__kofte_login_attempts", "__kofte_module_registrations", "__kofte_registry_migrations",
            "__kofte_sessions", "__kofte_users"
        }, actual);
    }

    [Fact]
    public async Task NativeConstraintsEnforceExactKeysModesNullCombinationsOverflowAndImmutableEvidence()
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, Token);
        await using var scope = await fixture.Maintenance.AcquireAsync(Token);
        using var budget = new OperationBudget(OperationBudget.Register, Token);
        await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
        var runner = new RegistryMigrationRunner(fixture.Installation);
        await ApplyAsync(runner, scope, connection, budget);
        var installation = await runner.ReadInstallationAsync(connection, budget);
        var p = Provider == "sqlite" ? "" : "kofte_system.";
        var user = Guid.NewGuid();
        var other = Guid.NewGuid();
        var development = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var epoch = Guid.NewGuid();
        var digest = new string('a', 64);
        Task Execute(string sql, params (string Name, object Value)[] values) =>
            RegistryMigrationRunner.ExecuteAsync(connection, sql, budget, null, values);
        async Task Refuse(string sql, string postgresCode, params (string Name, object Value)[] values)
        {
            var error = await Assert.ThrowsAnyAsync<DbException>(() => Execute(sql, values));
            if (error is SqliteException sqlite) Assert.Equal(19, sqlite.SqliteErrorCode);
            else Assert.Equal(postgresCode, Assert.IsType<PostgresException>(error).SqlState);
        }
        await Execute($"""
            INSERT INTO {p}__kofte_identity_metadata VALUES (1,@installation,1,0,@epoch,'oidc',@digest,@now);
            INSERT INTO {p}__kofte_users (user_id,mode,display_name,created_at_utc,updated_at_utc)
              VALUES (@user,'oidc',@display,@now,@now),(@other,'oidc',NULL,@now,@now),
                     (@development,'development',NULL,@now,@now);
            INSERT INTO {p}__kofte_bootstrap
              (singleton_key,installation_id,state,generation,initial_user_mode)
              VALUES (1,@installation,'disabled',0,'oidc');
            """, ("installation", installation.Id), ("epoch", epoch), ("digest", digest), ("now", now),
            ("user", user), ("other", other), ("development", development),
            ("display", string.Concat(Enumerable.Repeat("\U0001f331", 128))));
        await Refuse($"UPDATE {p}__kofte_users SET display_name=@display WHERE user_id=@user", "23514",
            ("display", string.Concat(Enumerable.Repeat("\U0001f331", 129))), ("user", user));
        await Refuse($"UPDATE {p}__kofte_users SET display_name=@display WHERE user_id=@user", "23514",
            ("display", "unsafe\u0085display"), ("user", user));
        await Refuse($"UPDATE {p}__kofte_identity_metadata SET security_revision=-1", "23514");
        await Refuse($"UPDATE {p}__kofte_bootstrap SET state='pending'", "23514");
        await Refuse($"""
            UPDATE {p}__kofte_bootstrap SET state='consumed',generation=1,initial_user_id=@user,
              consumed_at_utc=@now,consumed_generation=1
            """, "23503", ("user", development), ("now", now));
        await Refuse($"""
            INSERT INTO {p}__kofte_external_identities (mode,issuer,subject,user_id,created_at_utc)
              VALUES ('oidc','https://fixture.invalid','mismatched-mode',@user,@now)
            """, "23503", ("user", development), ("now", now));
        await Execute($"""
            INSERT INTO {p}__kofte_external_identities (mode,issuer,subject,user_id,created_at_utc)
              VALUES ('oidc','https://fixture.invalid','Case',@user,@now),
                     ('oidc','https://fixture.invalid','case',@other,@now),
                     ('development','kofte:development:v1','Case',@development,@now);
            INSERT INTO {p}__kofte_login_attempts
              (attempt_id,browser_digest,mode,trust_digest,session_epoch,return_path,created_at_utc,expires_at_utc,state)
              VALUES (@attempt,@digest,'oidc',@digest,@epoch,'/identity',@now,@expires,'pending');
            INSERT INTO {p}__kofte_sessions
              (reference_digest,user_id,mode,session_epoch,protected_ticket,created_at_utc,last_seen_at_utc,
               idle_expires_at_utc,absolute_expires_at_utc,state)
              VALUES (@digest,@user,'oidc',@epoch,@ticket,@now,@now,@expires,@absolute,'active');
            INSERT INTO {p}__kofte_identity_admission VALUES ('login_installation',@now,@digest,@expires,@maximum);
            INSERT INTO {p}__kofte_identity_events
              (event_id,security_revision,event_code,occurred_at_utc,correlation_id,user_id,outcome,retention_class)
              VALUES (@event,0,'user_created',@now,@correlation,@user,'committed','installation');
            """, ("user", user), ("other", other), ("development", development), ("now", now),
            ("attempt", Guid.NewGuid()), ("digest", digest), ("epoch", epoch), ("ticket", new byte[] { 1 }),
            ("expires", now.AddMinutes(5)), ("absolute", now.AddHours(8)), ("maximum", long.MaxValue),
            ("event", Guid.NewGuid()), ("correlation", new string('b', 32)));
        await Refuse($"UPDATE {p}__kofte_external_identities SET subject='Case' WHERE user_id=@other",
            "23505", ("other", other));
        await Refuse($"UPDATE {p}__kofte_external_identities SET subject=@subject WHERE user_id=@other",
            "23514", ("other", other), ("subject", new string('a', 256)));
        await Refuse($"UPDATE {p}__kofte_login_attempts SET state='consumed'", "23514");
        await Refuse($"UPDATE {p}__kofte_sessions SET state='revoked'", "23514");
        await Refuse($"UPDATE {p}__kofte_identity_admission SET count=count+1", "22003");
        await Refuse($"UPDATE {p}__kofte_identity_events SET security_revision=1", "P0001");
        await Refuse($"DELETE FROM {p}__kofte_identity_events", "P0001");
        Assert.Equal(long.MaxValue, await RegistryMigrationRunner.ScalarAsync(connection,
            $"SELECT count FROM {p}__kofte_identity_admission", budget));
        Assert.Equal("pending", await RegistryMigrationRunner.ScalarAsync(connection,
            $"SELECT state FROM {p}__kofte_login_attempts", budget));
        Assert.Equal("active", await RegistryMigrationRunner.ScalarAsync(connection,
            $"SELECT state FROM {p}__kofte_sessions", budget));
        await runner.ReadInstallationAsync(connection, budget);
    }

    private async Task ApplyAsync(RegistryMigrationRunner runner, IStorageMaintenanceScope scope,
        DbConnection connection, OperationBudget budget)
    {
        await using var transaction = connection is SqliteConnection sqlite
            ? sqlite.BeginTransaction(deferred: false) : await connection.BeginTransactionAsync(Token);
        await runner.ApplyIdentityFormatAsync(Assert.IsType<MaintenanceScope>(scope), connection, transaction, budget);
        budget.Check();
        await transaction.CommitAsync(Token);
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteIdentityFormatTests : IdentityFormatTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresIdentityFormatTests : IdentityFormatTests
{
    protected override string Provider => "postgres";

    [Fact]
    public Task UnexpectedNativeRoutineIsRefused() => AssertAlterationAsync("routine");

    [Fact]
    public Task UnexpectedNativeAggregateIsRefused() => AssertAlterationAsync("aggregate");
}
