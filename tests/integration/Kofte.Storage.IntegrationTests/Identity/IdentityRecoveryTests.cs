using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Modules.Alpha;
using Kofte.Storage.IntegrationTests.Providers;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;
using Microsoft.AspNetCore.DataProtection;

namespace Kofte.Storage.IntegrationTests.Identity;

public abstract class IdentityRecoveryTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;
    private static readonly Guid UserId = Guid.Parse("10000000-0000-4000-8000-000000000001");

    [Fact]
    public async Task CompleteFormatTwoBackupBindsActualConsumedIdentityAndPopulatedModuleState()
    {
        await using var fixture = await StorageProviderScope.CreateAsync(Provider, Token);
        var installation = await PrepareAsync(fixture);
        var maintenance = new StorageMaintenance(installation);
        await using (var scope = await maintenance.AcquireAsync(fixture.Token))
        {
            var registry = new StorageRegistry(installation);
            var module = RegistryFixture.Module("org.example.alpha");
            Assert.IsType<StorageResult.Ready>(await registry.RegisterAsync(module, scope, fixture.Token));
            await using var binding = await registry.BindAsync(module.Id, fixture.Token);
            Assert.IsType<StorageResult.Ready>(await binding.MigrateAsync(
                ModuleFixture.AlphaFactory, scope, cancellationToken: fixture.Token));
            await using var context = await binding.CreateContextAsync(ModuleFixture.AlphaFactory, fixture.Token);
            context.Records.Add(new AlphaRecord { Id = 1, Payload = "retained-identity-module", Note = "retained-note" });
            await context.SaveChangesAsync(fixture.Token);
        }
        using var backup = new OwnedTestRoot();
        var manifest = await BackupAsync(fixture, backup.Path);
        Assert.Equal(RecoveryManifest.IdentityFormat, manifest.Format);
        Assert.Equal(2, manifest.RegistryVersion);
        var identity = Assert.IsType<IdentityRecoverySummary>(manifest.Identity);
        Assert.Equal(1, identity.SchemaVersion);
        Assert.Equal(4, identity.SecurityRevision);
        Assert.NotEqual(Guid.Empty, identity.SessionEpoch);
        Assert.Equal("consumed", identity.BootstrapState);
        Assert.Equal(1, identity.ConsumedGeneration);
        Assert.Equal(new IdentityRecoveryCounts(1, 1, 0, 1, 4, 0), identity.Counts);
        Assert.Matches("^[a-f0-9]{64}$", identity.Sha256);
        Assert.Equal(["202610210001_Initial", "202610210002_AddNote"],
            Assert.Single(manifest.Allocations).History.Select(row => row.Id));
        Assert.Equal(Provider == "sqlite" ? 2 : 1, manifest.Artifacts.Count);
        Assert.Equal(JsonSerializer.Serialize(manifest),
            JsonSerializer.Serialize(await VerifyAsync(installation, backup.Path, fixture.Token)));
        await using var stopped = await maintenance.AcquireAsync(fixture.Token);
        Assert.Equal(identity, await IdentityRecoverySnapshot.CaptureStoppedAsync(
            installation, stopped, fixture.Token));
    }

    [Theory]
    [InlineData("format")]
    [InlineData("digest")]
    [InlineData("counts")]
    [InlineData("consumed-history")]
    public async Task ManifestDescriptionsCannotOverrideActualFormatSecurityRowsOrConsumedHistory(string fault)
    {
        await using var fixture = await StorageProviderScope.CreateAsync(Provider, Token);
        var installation = await PrepareAsync(fixture);
        using var backup = new OwnedTestRoot();
        await BackupAsync(fixture, backup.Path);
        var path = Path.Combine(backup.Path, RecoveryManifest.FileName);
        var json = JsonNode.Parse(await File.ReadAllTextAsync(path, fixture.Token))!;
        switch (fault)
        {
            case "format": json["format"] = "kofte-storage-recovery-v1"; break;
            case "digest": json["identity"]!["sha256"] = new string('0', 64); break;
            case "counts": json["identity"]!["counts"]!["users"] = 0; break;
            case "consumed-history":
                json["identity"]!["bootstrapState"] = "disabled";
                json["identity"]!["consumedGeneration"] = null;
                break;
        }
        await File.WriteAllTextAsync(path, json.ToJsonString(), fixture.Token);
        var before = await File.ReadAllBytesAsync(path, fixture.Token);
        await Assert.ThrowsAsync<StorageException>(() => VerifyAsync(installation, backup.Path, fixture.Token));
        Assert.Equal(before, await File.ReadAllBytesAsync(path, fixture.Token));
    }

    [Theory]
    [InlineData("profile")]
    [InlineData("ticket")]
    public async Task RehashedArtifactsCannotHideChangedActualRowsBehindUnchangedCountsAndSecurityRevision(string fault)
    {
        await using var fixture = await StorageProviderScope.CreateAsync(Provider, Token);
        var installation = await PrepareAsync(fixture);
        using var backup = new OwnedTestRoot();
        var manifest = await BackupAsync(fixture, backup.Path);
        var inspected = Provider == "sqlite" ? StorageInstallation.Sqlite(backup.Path) : installation;
        var maintenance = new StorageMaintenance(inspected);
        await using (var scope = await maintenance.AcquireAsync(fixture.Token))
        {
            using var budget = new OperationBudget(OperationBudget.Register, fixture.Token);
            await using var connection = await inspected.OpenPlatformAsync(budget);
            var prefix = Provider == "sqlite" ? "" : "kofte_system.";
            if (fault == "profile")
                await RegistryMigrationRunner.ExecuteAsync(connection,
                    $"UPDATE {prefix}__kofte_users SET display_name=@display WHERE user_id=@user", budget,
                    null, ("display", "changed-but-same-identity"), ("user", UserId));
            else
                await RegistryMigrationRunner.ExecuteAsync(connection,
                    $"UPDATE {prefix}__kofte_sessions SET protected_ticket=@ticket WHERE user_id=@user", budget,
                    null, ("ticket", new byte[24_000]), ("user", UserId));
        }
        var artifacts = new List<RecoveryArtifact>();
        foreach (var artifact in manifest.Artifacts)
            artifacts.Add(await RecoverySetVerifier.DescribeArtifactAsync(backup.Path, artifact.Name, fixture.Token));
        File.Delete(Path.Combine(backup.Path, RecoveryManifest.FileName));
        await RecoverySetVerifier.WriteManifestAsync(backup.Path,
            manifest with { Artifacts = artifacts }, fixture.Token);
        var error = await Assert.ThrowsAsync<StorageException>(() => VerifyAsync(inspected, backup.Path, fixture.Token));
        Assert.Equal(StorageCode.ReconciliationRequired, error.Code);
    }

    [Fact]
    public async Task StoppedMaintenancePrunesOnlyOneExpiredBatchAndRetainsConsumedAuthorityAndActiveState()
    {
        await using var fixture = await StorageProviderScope.CreateAsync(Provider, Token);
        var installation = await PrepareAsync(fixture);
        var maintenance = new StorageMaintenance(installation);
        await using var scope = await maintenance.AcquireAsync(fixture.Token);
        using var budget = new OperationBudget(OperationBudget.Migration, fixture.Token);
        var now = DateTime.UtcNow;
        var old = now.AddDays(-31);
        await using (var connection = await installation.OpenPlatformAsync(budget))
        {
            var prefix = Provider == "sqlite" ? "" : "kofte_system.";
            object Time(DateTime value) => Provider == "sqlite"
                ? value.ToString("O", CultureInfo.InvariantCulture) : value;
            await RegistryMigrationRunner.ExecuteAsync(connection, $"""
                INSERT INTO {prefix}__kofte_identity_events
                  (event_id,security_revision,event_code,occurred_at_utc,correlation_id,outcome,retention_class)
                  VALUES (@id,0,'configuration_reconciled',@old,@correlation,'committed','installation')
                """, budget, null, ("id", Guid.NewGuid()), ("old", Time(old)),
                ("correlation", Guid.NewGuid().ToString("N")));
            for (var index = 0; index < 131; index++)
            {
                var recent = index >= 129;
                await RegistryMigrationRunner.ExecuteAsync(connection, $"""
                    INSERT INTO {prefix}__kofte_identity_events
                      (event_id,security_revision,event_code,occurred_at_utc,correlation_id,outcome,retention_class)
                      VALUES (@id,0,'session_created',@created,@correlation,'committed','session');
                    INSERT INTO {prefix}__kofte_login_attempts
                      (attempt_id,browser_digest,mode,trust_digest,session_epoch,return_path,
                       created_at_utc,expires_at_utc,state,consumed_at_utc)
                      SELECT @id,@key,mode,trust_digest,session_epoch,'/',@created,@expiry,@state,
                        CASE WHEN @state='pending' THEN NULL ELSE @created END
                      FROM {prefix}__kofte_identity_metadata;
                    INSERT INTO {prefix}__kofte_identity_admission
                      (category,window_start_utc,key_digest,expires_at_utc,count)
                      VALUES ('login_browser',@created,@key,@expiry,1);
                    """, budget, null, ("id", Guid.NewGuid()), ("key", index.ToString("x64", CultureInfo.InvariantCulture)),
                    ("created", Time(recent ? now : old)), ("expiry", Time(recent ? now.AddMinutes(5) : old.AddMinutes(5))),
                    ("state", index % 2 == 0 ? "pending" : "consumed"),
                    ("correlation", Guid.NewGuid().ToString("N")));
            }
        }
        var before = await IdentityRecoverySnapshot.CaptureStoppedAsync(installation, scope, fixture.Token);
        Assert.Equal(384, await IdentityMaintenance.RunAsync(installation, Assert.IsType<MaintenanceScope>(scope),
            (await RecoverySetVerifier.InspectStoppedAsync(installation, scope, fixture.Token)).InstallationId,
            "oidc", new string('a', 64), now, budget));
        var after = await IdentityRecoverySnapshot.CaptureStoppedAsync(installation, scope, fixture.Token);
        Assert.Equal(before.SecurityRevision + 1, after.SecurityRevision);
        Assert.Equal(before.SessionEpoch, after.SessionEpoch);
        Assert.Equal(before.BootstrapState, after.BootstrapState);
        Assert.Equal(before.ConsumedGeneration, after.ConsumedGeneration);
        Assert.Equal(before.Counts with
        {
            Events = before.Counts.Events - 128,
            LoginAttempts = before.Counts.LoginAttempts - 128,
            AdmissionWindows = before.Counts.AdmissionWindows - 128
        }, after.Counts);
        Assert.Equal(4, await IdentityMaintenance.RunAsync(installation, Assert.IsType<MaintenanceScope>(scope),
            (await RecoverySetVerifier.InspectStoppedAsync(installation, scope, fixture.Token)).InstallationId,
            "oidc", new string('a', 64), now, budget));
        var final = await IdentityRecoverySnapshot.CaptureStoppedAsync(installation, scope, fixture.Token);
        Assert.Equal(new IdentityRecoveryCounts(1, 1, 1, 1, 7, 2), final.Counts);
        Assert.Equal("consumed", final.BootstrapState);
        Assert.Equal(before.ConsumedGeneration, final.ConsumedGeneration);
        Assert.Equal(before.SessionEpoch, final.SessionEpoch);
    }

    [Fact]
    public async Task PreparedIdentityCannotUseTheLegacyUngatedRestorePath()
    {
        await using var fixture = await StorageProviderScope.CreateAsync(Provider, Token);
        await PrepareAsync(fixture);
        using var backup = new OwnedTestRoot();
        await BackupAsync(fixture, backup.Path);
        InvalidOperationException error;
        if (fixture.Postgres is { } postgres)
        {
            await using var target = await StorageProviderScope.CreateAsync(Provider, Token);
            var targetPostgres = Assert.IsType<PostgresResources>(target.Postgres);
            error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                postgres.RestoreStorageSetToFreshAsync(backup.Path, targetPostgres, fixture.Token));
            using var budget = new OperationBudget(OperationBudget.Register, fixture.Token);
            await using var connection = await StorageInstallation.PostgreSql(targetPostgres.ConnectionString)
                .OpenPlatformAsync(budget);
            Assert.False(Assert.IsType<bool>(await RegistryMigrationRunner.ScalarAsync(connection, """
                SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_tables WHERE schemaname='kofte_system')
                """, budget)));
        }
        else
        {
            using var target = new OwnedTestRoot();
            error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                SqliteRecoveryExercise.RestoreFreshAsync(backup.Path, target.Path, fixture.Token));
            Assert.False(Directory.Exists(target.Path));
        }
        Assert.Equal("identity_restore_reconciliation_required", error.Message);
    }

    private async Task<StorageInstallation> PrepareAsync(StorageProviderScope fixture)
    {
        var installation = fixture.Postgres is { } postgres
            ? StorageInstallation.PostgreSql(postgres.ConnectionString, postgres.ProvisioningConnectionString)
            : StorageInstallation.Sqlite(fixture.DataRoot);
        var maintenance = new StorageMaintenance(installation);
        await using var scope = await maintenance.AcquireAsync(fixture.Token);
        await maintenance.UpgradeAsync(scope, fixture.Token);
        using var budget = new OperationBudget(OperationBudget.Migration, fixture.Token);
        await using var connection = await installation.OpenPlatformAsync(budget);
        var registry = new RegistryMigrationRunner(installation);
        var owner = await registry.ReadInstallationAsync(connection, budget);
        await IdentityPreparation.InitializeAsync(installation, Assert.IsType<MaintenanceScope>(scope),
            connection, owner.Id, "oidc", new string('a', 64), budget);
        var prefix = Provider == "sqlite" ? "" : "kofte_system.";
        object now = Provider == "sqlite"
            ? DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture) : DateTime.UtcNow;
        await RegistryMigrationRunner.ExecuteAsync(connection, $"""
            INSERT INTO {prefix}__kofte_users (user_id,mode,display_name,created_at_utc,updated_at_utc)
              VALUES (@user,'oidc','retained-profile',@now,@now);
            INSERT INTO {prefix}__kofte_external_identities (mode,issuer,subject,user_id,created_at_utc)
              VALUES ('oidc','https://fixture.invalid','retained-subject',@user,@now);
            INSERT INTO {prefix}__kofte_identity_events
              (event_id,security_revision,event_code,occurred_at_utc,correlation_id,user_id,outcome,retention_class)
              VALUES (@created,2,'user_created',@now,@correlation,@user,'committed','installation'),
                     (@enrolled,3,'initial_administrator_enrolled',@now,@correlation,@user,'committed','installation');
            UPDATE {prefix}__kofte_identity_metadata SET security_revision=3;
            UPDATE {prefix}__kofte_bootstrap SET state='consumed',generation=1,initial_user_id=@user,
              consumed_at_utc=@now,consumed_generation=1;
            """, budget, null, ("user", UserId), ("now", now),
            ("created", Guid.NewGuid()), ("enrolled", Guid.NewGuid()), ("correlation", Guid.NewGuid().ToString("N")));
        var ticket = new EphemeralDataProtectionProvider().CreateProtector("kofte-test-recovery")
            .Protect(new byte[24_000]);
        await IdentityStore.MutateStoppedAsync(installation, Assert.IsType<MaintenanceScope>(scope),
            owner.Id, "oidc", new string('a', 64), false, budget, async change =>
                await IdentitySessionStore.CreateAsync(change,
                    await IdentityUserStore.ReadAsync(change.Database, UserId), new string('b', 64),
                    ticket, DateTime.UtcNow));
        return installation;
    }

    private Task<RecoveryManifest> BackupAsync(StorageProviderScope fixture, string path) =>
        fixture.Postgres is { } postgres
            ? postgres.BackupStorageSetAsync(path, fixture.Token)
            : SqliteRecoveryExercise.BackupAsync(fixture.DataRoot, path, fixture.Token);

    private async Task<RecoveryManifest> VerifyAsync(StorageInstallation installation, string path,
        CancellationToken token)
    {
        if (Provider == "sqlite") return await RecoverySetVerifier.VerifyAsync(path, cancellationToken: token);
        await using var scope = await new StorageMaintenance(installation).AcquireAsync(token);
        return await RecoverySetVerifier.VerifyAsync(path, installation, scope, token);
    }
}

[Trait("Provider", "sqlite")]
[Trait("Suite", "recovery")]
public sealed class SqliteIdentityRecoveryTests : IdentityRecoveryTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Trait("Suite", "recovery")]
public sealed class PostgresIdentityRecoveryTests : IdentityRecoveryTests
{
    protected override string Provider => "postgres";
}
