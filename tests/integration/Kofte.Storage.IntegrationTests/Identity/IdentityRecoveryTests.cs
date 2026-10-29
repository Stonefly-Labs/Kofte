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
using Kofte.TestSupport.Identity;
using Microsoft.AspNetCore.DataProtection;

namespace Kofte.Storage.IntegrationTests.Identity;

public abstract class IdentityRecoveryTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;
    private static readonly Guid UserId = Guid.Parse("10000000-0000-4000-8000-000000000001");

    [Fact]
    public async Task ActualAcceptedPreIdentityBinaryRefusesFormatTwoServingAndUpgradeWithoutChangingAnyState()
    {
        var legacy = IdentityLegacyHostFixture.Current;
        await legacy.PrepareAsync(Token);
        await using var fixture = await StorageProviderScope.CreateAsync(Provider, Token);
        var installation = await PrepareAsync(fixture);
        await PopulateModuleAsync(installation, fixture.Token);
        var before = await CaptureAsync();
        foreach (var arguments in new string[][] { [], ["--registry", "upgrade"] })
        {
            var result = await legacy.RefuseAsync(fixture, arguments, fixture.Token);
            Assert.Equal(1, result.ExitCode);
            Assert.DoesNotContain("registry_upgraded", result.Stdout);
            var observed = new List<string>();
            foreach (var line in result.Stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                using var record = JsonDocument.Parse(line);
                observed.Add(record.RootElement.GetProperty("eventCode").GetString()!);
            }
            Assert.Contains("storage_invalid", observed);
            Assert.Equal(before, await CaptureAsync());
        }

        async Task<string> CaptureAsync()
        {
            await using var stopped = await new StorageMaintenance(installation).AcquireAsync(fixture.Token);
            var identity = await IdentityRecoverySnapshot.CaptureStoppedAsync(installation, stopped, fixture.Token);
            var inventory = await RecoverySetVerifier.InspectStoppedAsync(installation, stopped, fixture.Token);
            await using var binding = await new StorageRegistry(installation).BindAsync(
                RegistryFixture.Module("org.example.alpha").Id, fixture.Token);
            await using var context = await binding.CreateContextAsync(ModuleFixture.AlphaFactory, fixture.Token);
            var record = Assert.Single(context.Records);
            return JsonSerializer.Serialize(new { identity, inventory, record.Id, record.Payload, record.Note });
        }
    }

    [Fact]
    public async Task CompleteFormatTwoBackupBindsActualConsumedIdentityAndPopulatedModuleState()
    {
        await using var fixture = await StorageProviderScope.CreateAsync(Provider, Token);
        var installation = await PrepareAsync(fixture);
        var maintenance = new StorageMaintenance(installation);
        await PopulateModuleAsync(installation, fixture.Token);
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
        await RehashManifestAsync(backup.Path, manifest, fixture.Token);
        var error = await Assert.ThrowsAsync<StorageException>(() => VerifyAsync(inspected, backup.Path, fixture.Token));
        Assert.Equal(StorageCode.ReconciliationRequired, error.Code);
    }

    [Theory]
    [InlineData("constraint")]
    [InlineData("migration-history")]
    [InlineData("bootstrap-history")]
    public async Task RehashedFormatTwoArtifactsCannotConcealInvalidConstraintsOrActualHistory(string fault)
    {
        await using var fixture = await StorageProviderScope.CreateAsync(Provider, Token);
        var installation = await PrepareAsync(fixture);
        using var backup = new OwnedTestRoot();
        var manifest = await BackupAsync(fixture, backup.Path);
        var inspected = Provider == "sqlite" ? StorageInstallation.Sqlite(backup.Path) : installation;
        await using (var scope = await new StorageMaintenance(inspected).AcquireAsync(fixture.Token))
        {
            using var budget = new OperationBudget(OperationBudget.Register, fixture.Token);
            await using var connection = await inspected.OpenPlatformAsync(budget);
            var prefix = Provider == "sqlite" ? "" : "kofte_system.";
            var sql = fault switch
            {
                "migration-history" => $"UPDATE {prefix}__kofte_registry_migrations SET sha256='{new string('0', 64)}' WHERE version=2",
                "bootstrap-history" => $"""
                    UPDATE {prefix}__kofte_bootstrap SET state='disabled',initial_user_id=NULL,
                      consumed_at_utc=NULL,consumed_generation=NULL
                    """,
                "constraint" => "ALTER TABLE kofte_system.__kofte_identity_admission " +
                    "DROP CONSTRAINT __kofte_identity_admission_count_check",
                _ => throw new InvalidOperationException("test_alteration_invalid")
            };
            if (Provider == "sqlite" && fault == "constraint")
            {
                Assert.Equal(0L, await RegistryMigrationRunner.ScalarAsync(connection,
                    "SELECT COUNT(*) FROM __kofte_identity_admission", budget));
                await using var definition = RegistryMigrationRunner.Command(connection,
                    "SELECT sql FROM sqlite_master WHERE type='table' AND name='__kofte_identity_admission'", budget);
                var ddl = Assert.IsType<string>(await definition.ExecuteScalarAsync(fixture.Token));
                var changed = ddl.Replace("count >= 0", "count >= -1", StringComparison.Ordinal);
                Assert.NotEqual(ddl, changed);
                sql = $"DROP TABLE __kofte_identity_admission;{changed};" +
                    "CREATE INDEX __kofte_admission_expiry ON __kofte_identity_admission (expires_at_utc)";
            }
            await RegistryMigrationRunner.ExecuteAsync(connection, sql, budget);
        }
        var rehashed = await RehashManifestAsync(backup.Path, manifest, fixture.Token);
        var manifestPath = Path.Combine(backup.Path, RecoveryManifest.FileName);
        var before = await File.ReadAllBytesAsync(manifestPath, fixture.Token);
        var error = await Assert.ThrowsAsync<StorageException>(() => VerifyAsync(inspected, backup.Path, fixture.Token));
        Assert.Equal(StorageCode.Corrupt, error.Code);
        Assert.Equal(before, await File.ReadAllBytesAsync(manifestPath, fixture.Token));
        foreach (var artifact in rehashed.Artifacts)
            Assert.Equal(artifact, await RecoverySetVerifier.DescribeArtifactAsync(backup.Path, artifact.Name, fixture.Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MissingModuleBearingArtifactsRemainRefusedEvenWhenOmittedFromTheManifest(bool omit)
    {
        await using var fixture = await StorageProviderScope.CreateAsync(Provider, Token);
        var installation = await PrepareAsync(fixture);
        await PopulateModuleAsync(installation, fixture.Token);
        using var backup = new OwnedTestRoot();
        var manifest = await BackupAsync(fixture, backup.Path);
        var artifact = Provider == "sqlite"
            ? Assert.Single(manifest.Artifacts, item => item.Name.StartsWith("modules/", StringComparison.Ordinal))
            : Assert.Single(manifest.Artifacts);
        var missing = Path.Combine(backup.Path, artifact.Name);
        using var retained = new OwnedTestRoot();
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        Directory.CreateDirectory(retained.Path, UnixStorePaths.DirectoryMode);
        File.Move(missing, Path.Combine(retained.Path, "retained-artifact"));
        var path = Path.Combine(backup.Path, RecoveryManifest.FileName);
        if (omit)
        {
            var json = JsonNode.Parse(await File.ReadAllTextAsync(path, fixture.Token))!;
            var artifacts = json["artifacts"]!.AsArray();
            artifacts.Remove(Assert.Single(artifacts, item => item!["name"]!.GetValue<string>() == artifact.Name));
            await File.WriteAllTextAsync(path, json.ToJsonString(), fixture.Token);
        }
        var before = await File.ReadAllBytesAsync(path, fixture.Token);
        var error = await Assert.ThrowsAsync<StorageException>(() => VerifyAsync(installation, backup.Path, fixture.Token));
        Assert.Equal(omit ? StorageCode.ReconciliationRequired : StorageCode.PathRejected, error.Code);
        Assert.Equal(before, await File.ReadAllBytesAsync(path, fixture.Token));
        Assert.False(File.Exists(missing));
        Assert.Equal(artifact with { Name = "retained-artifact" },
            await RecoverySetVerifier.DescribeArtifactAsync(retained.Path, "retained-artifact", fixture.Token));
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
    public async Task CompleteIdentityRestoreRemainsQuarantinedOutsideTheSnapshotWithItsExactRecoveryPointBinding()
    {
        await using var fixture = await StorageProviderScope.CreateAsync(Provider, Token);
        await PrepareAsync(fixture);
        using var backup = new OwnedTestRoot();
        var original = await BackupAsync(fixture, backup.Path);
        var manifestBytes = await File.ReadAllBytesAsync(Path.Combine(backup.Path, RecoveryManifest.FileName), fixture.Token);
        using var keys = new OwnedTestRoot();
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        Directory.CreateDirectory(keys.Path, UnixStorePaths.DirectoryMode);
        var keyDirectory = Path.Combine(keys.Path, "keys");
        using var sqliteTarget = new OwnedTestRoot();
        await using var postgresTarget = Provider == "postgres"
            ? await StorageProviderScope.CreateAsync(Provider, Token) : null;
        var restored = fixture.Postgres is { } postgres
            ? await postgres.RestoreStorageSetToFreshQuarantinedAsync(backup.Path,
                Assert.IsType<PostgresResources>(postgresTarget?.Postgres), keyDirectory, fixture.Token)
            : await SqliteRecoveryExercise.RestoreFreshQuarantinedAsync(
                backup.Path, sqliteTarget.Path, keyDirectory, fixture.Token);
        var quarantine = await IdentityRestoreQuarantine.ReadAsync(keyDirectory, fixture.Token);
        Assert.True(quarantine.Restored);
        Assert.Equal(original.RecoverySetId, quarantine.Manifest.RecoverySetId);
        Assert.Equal(original.InstallationId, quarantine.Manifest.InstallationId);
        Assert.Equal(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(manifestBytes)),
            quarantine.ManifestSha256);
        Assert.Equal([Path.Combine(keyDirectory, IdentityRestoreQuarantine.FileName)],
            Directory.GetFiles(keyDirectory));
        Assert.Equal(original.Identity, restored.Identity);
        var installation = postgresTarget?.Postgres is { } target
            ? StorageInstallation.PostgreSql(target.ConnectionString) : StorageInstallation.Sqlite(sqliteTarget.Path);
        await using var scope = await new StorageMaintenance(installation).AcquireAsync(fixture.Token);
        Assert.Equal(original.Identity,
            await IdentityRecoverySnapshot.CaptureStoppedAsync(installation, scope, fixture.Token));
        var overlapping = Path.Combine(backup.Path, "unowned-keys");
        var rejected = await Assert.ThrowsAsync<StorageException>(() =>
            IdentityRestoreQuarantine.CreateAsync(backup.Path, overlapping, original, token: fixture.Token));
        Assert.Equal(StorageCode.PathRejected, rejected.Code);
        Assert.False(Directory.Exists(overlapping));
        var marker = Path.Combine(keyDirectory, IdentityRestoreQuarantine.FileName);
        var intact = await File.ReadAllBytesAsync(marker, fixture.Token);
        try
        {
            foreach (var fault in new[] { "unknown", "duplicate", "recovery-set", "installation", "format" })
            {
                var json = JsonNode.Parse(intact)!;
                if (fault == "unknown") json["unapproved"] = true;
                else if (fault != "duplicate")
                {
                    var manifest = JsonNode.Parse(manifestBytes)!;
                    if (fault == "recovery-set") manifest["recoverySetId"] = Guid.NewGuid();
                    else if (fault == "installation") manifest["installationId"] = Guid.NewGuid();
                    else manifest["format"] = RecoveryManifest.CurrentFormat;
                    json["manifest"] = Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(manifest));
                }
                var changed = json.ToJsonString();
                if (fault == "duplicate") changed = changed[..^1] + ",\"version\":\"1\"}";
                await File.WriteAllTextAsync(marker, changed, fixture.Token);
                await Assert.ThrowsAsync<StorageException>(() =>
                    IdentityRestoreQuarantine.ReadAsync(keyDirectory, fixture.Token));
                Assert.True(IdentityRestoreQuarantine.IsRequired(keyDirectory));
            }
        }
        finally { await File.WriteAllBytesAsync(marker, intact, fixture.Token); }
        Assert.Equal(quarantine.ManifestSha256,
            (await IdentityRestoreQuarantine.ReadAsync(keyDirectory, fixture.Token)).ManifestSha256);
        var decisionPath = Path.Combine(keys.Path, "decision.json");
        var decision = JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = "1",
            recoverySetId = original.RecoverySetId.ToString("D"),
            installationId = original.InstallationId.ToString("D"),
            expectedManifestSha256 = quarantine.ManifestSha256,
            priorEnrollment = "not-established"
        });
        PrivateTestFiles.WriteNew(decisionPath, decision);
        foreach (var history in new[] { "known-consumed", "known-never-enrolled", "not-established" })
        {
            var json = JsonNode.Parse(decision)!;
            json["priorEnrollment"] = history;
            await File.WriteAllTextAsync(decisionPath, json.ToJsonString(), fixture.Token);
            Assert.Equal(history,
                (await IdentityRestoreDecision.ReadAsync(decisionPath, quarantine, fixture.Token)).PriorEnrollment);
        }
        foreach (var fault in new[] { "version", "recoverySetId", "installationId", "expectedManifestSha256",
            "priorEnrollment", "unknown", "duplicate", "missing" })
        {
            var json = JsonNode.Parse(decision)!;
            if (fault == "version") json["version"] = "2";
            else if (fault is "recoverySetId" or "installationId") json[fault] = Guid.NewGuid().ToString("D");
            else if (fault == "expectedManifestSha256") json[fault] = new string('0', 64);
            else if (fault == "priorEnrollment") json[fault] = "automatic";
            else if (fault == "unknown") json["nominee"] = "not-an-authority-source";
            else if (fault == "missing") json.AsObject().Remove("version");
            var changed = json.ToJsonString();
            if (fault == "duplicate") changed = changed[..^1] + ",\"version\":\"1\"}";
            await File.WriteAllTextAsync(decisionPath, changed, fixture.Token);
            await Assert.ThrowsAsync<StorageException>(() =>
                IdentityRestoreDecision.ReadAsync(decisionPath, quarantine, fixture.Token));
        }
        await File.WriteAllBytesAsync(decisionPath, decision, fixture.Token);
        File.SetUnixFileMode(decisionPath, UnixStorePaths.FileMode | UnixFileMode.GroupRead);
        try
        {
            await Assert.ThrowsAsync<StorePathException>(() =>
                IdentityRestoreDecision.ReadAsync(decisionPath, quarantine, fixture.Token));
        }
        finally { File.SetUnixFileMode(decisionPath, UnixStorePaths.FileMode); }
        var link = Path.Combine(keys.Path, "decision-link.json");
        File.CreateSymbolicLink(link, decisionPath);
        await Assert.ThrowsAsync<StorePathException>(() =>
            IdentityRestoreDecision.ReadAsync(link, quarantine, fixture.Token));
        foreach (var invalid in new[] { new byte[32_769], new byte[] { 0xff, 0xff } })
        {
            await File.WriteAllBytesAsync(decisionPath, invalid, fixture.Token);
            await Assert.ThrowsAsync<StorageException>(() =>
                IdentityRestoreDecision.ReadAsync(decisionPath, quarantine, fixture.Token));
        }
        await scope.DisposeAsync();
        await VerifyIncompleteJournalAuthorityAsync(installation, quarantine, keyDirectory, fixture.Token);
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

    [Theory]
    [InlineData("consumed", "known-never-enrolled", "consumed")]
    [InlineData("disabled", "known-never-enrolled", "disabled")]
    [InlineData("disabled", "known-consumed", "recovery_locked")]
    [InlineData("disabled", "not-established", "recovery_locked")]
    [InlineData("pending", "not-established", "recovery_locked")]
    [InlineData("recovery_locked", "known-never-enrolled", "recovery_locked")]
    public async Task ReconciliationInvalidatesRestoredAuthorityWithoutReopeningHistoricalEnrollment(
        string sourceState, string history, string expectedState)
    {
        await using var fixture = await StorageProviderScope.CreateAsync(Provider, Token);
        var source = await PrepareAsync(fixture, sourceState == "consumed");
        if (sourceState == "consumed") await PopulateModuleAsync(source, fixture.Token);
        using var authorization = new OwnedTestRoot();
        await using (var stopped = await new StorageMaintenance(source).AcquireAsync(fixture.Token))
        {
            using var seedBudget = new OperationBudget(OperationBudget.Migration, fixture.Token);
            await using var connection = await source.OpenPlatformAsync(seedBudget);
            var prefix = Provider == "sqlite" ? "" : "kofte_system.";
            var now = DateTime.UtcNow;
            object Time(DateTime value) => Provider == "sqlite"
                ? value.ToString("O", CultureInfo.InvariantCulture) : value;
            await RegistryMigrationRunner.ExecuteAsync(connection, $"""
                INSERT INTO {prefix}__kofte_login_attempts
                  (attempt_id,browser_digest,mode,trust_digest,session_epoch,return_path,
                   created_at_utc,expires_at_utc,state)
                  SELECT @id,@key,mode,trust_digest,session_epoch,'/',@now,@expiry,'pending'
                  FROM {prefix}__kofte_identity_metadata;
                INSERT INTO {prefix}__kofte_identity_admission
                  (category,window_start_utc,key_digest,expires_at_utc,count)
                  VALUES ('login_browser',@now,@key,@expiry,1);
                """, seedBudget, null, ("id", Guid.NewGuid()), ("key", new string('c', 64)),
                ("now", Time(now)), ("expiry", Time(now.AddMinutes(5))));
            if (sourceState == "pending")
            {
                if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
                Directory.CreateDirectory(authorization.Path, UnixStorePaths.DirectoryMode);
                var secret = new string('A', 43);
                var owner = (await RecoverySetVerifier.InspectStoppedAsync(source, stopped, fixture.Token)).InstallationId;
                await IdentityBootstrapStore.AuthorizeAsync(source, Assert.IsType<MaintenanceScope>(stopped),
                    owner, "oidc", new string('a', 64), "https://fixture.invalid", "retained-subject", secret,
                    () =>
                    {
                        PrivateTestFiles.WriteNew(Path.Combine(authorization.Path, "authorization"),
                            System.Text.Encoding.ASCII.GetBytes(secret));
                        return Task.CompletedTask;
                    }, seedBudget);
            }
            else if (sourceState == "recovery_locked")
                await RegistryMigrationRunner.ExecuteAsync(connection,
                    $"UPDATE {prefix}__kofte_bootstrap SET state='recovery_locked' WHERE singleton_key=1", seedBudget);
        }
        using var backup = new OwnedTestRoot();
        var manifest = await BackupAsync(fixture, backup.Path);
        using var keys = new OwnedTestRoot();
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        Directory.CreateDirectory(keys.Path, UnixStorePaths.DirectoryMode);
        var keyDirectory = Path.Combine(keys.Path, "keys");
        using var sqliteTarget = new OwnedTestRoot();
        await using var postgresTarget = Provider == "postgres"
            ? await StorageProviderScope.CreateAsync(Provider, Token) : null;
        if (fixture.Postgres is { } postgres)
            await postgres.RestoreStorageSetToFreshQuarantinedAsync(backup.Path,
                Assert.IsType<PostgresResources>(postgresTarget?.Postgres), keyDirectory, fixture.Token);
        else await SqliteRecoveryExercise.RestoreFreshQuarantinedAsync(
            backup.Path, sqliteTarget.Path, keyDirectory, fixture.Token);
        var installation = postgresTarget?.Postgres is { } target
            ? StorageInstallation.PostgreSql(target.ConnectionString) : StorageInstallation.Sqlite(sqliteTarget.Path);
        var quarantine = await IdentityRestoreQuarantine.ReadAsync(keyDirectory, fixture.Token);
        var decision = new IdentityRestoreDecision(manifest.RecoverySetId, manifest.InstallationId,
            quarantine.ManifestSha256, history);
        var before = Assert.IsType<IdentityRecoverySummary>(manifest.Identity);
        await using var scope = await new StorageMaintenance(installation).AcquireAsync(fixture.Token);
        using var budget = new OperationBudget(OperationBudget.Migration, fixture.Token);
        var epoch = Guid.NewGuid();
        var after = await IdentityRestoreStore.ReconcileAsync(installation, Assert.IsType<MaintenanceScope>(scope),
            quarantine, decision, "oidc", new string('a', 64), epoch, budget);
        Assert.Equal(before.SecurityRevision + 1, after.SecurityRevision);
        Assert.Equal(epoch, after.SessionEpoch);
        Assert.NotEqual(before.SessionEpoch, after.SessionEpoch);
        Assert.Equal(expectedState, after.BootstrapState);
        Assert.Equal(before.ConsumedGeneration, after.ConsumedGeneration);
        Assert.Equal(before.Counts with { AdmissionWindows = 0, Events = before.Counts.Events + 1 }, after.Counts);
        Assert.True(IdentityRestoreQuarantine.IsRequired(keyDirectory));
        Assert.Equal(after, await IdentityRestoreStore.ReconcileAsync(installation, Assert.IsType<MaintenanceScope>(scope),
            quarantine, decision, "oidc", new string('a', 64), epoch, budget));
        await using var database = await DeadlineStore.OpenAsync(installation, budget);
        Assert.Equal(0L, DeadlineStore.AssertScalar(await database.QueryAsync(
            $"SELECT COUNT(*) FROM {database.Prefix}__kofte_sessions WHERE state='active'")));
        Assert.Equal(0L, DeadlineStore.AssertScalar(await database.QueryAsync(
            $"SELECT COUNT(*) FROM {database.Prefix}__kofte_login_attempts WHERE state='pending'")));
        Assert.Equal("retained-profile", DeadlineStore.AssertScalar(await database.QueryAsync(
            $"SELECT display_name FROM {database.Prefix}__kofte_users WHERE user_id=@user", ("user", UserId))));
        Assert.Equal(UserId, IdentityValues.Id(DeadlineStore.AssertScalar(await database.QueryAsync(
            $"SELECT user_id FROM {database.Prefix}__kofte_external_identities WHERE subject='retained-subject'"))));
        if (sourceState == "consumed")
        {
            await using var binding = await new StorageRegistry(installation).BindAsync(
                RegistryFixture.Module("org.example.alpha").Id, fixture.Token);
            await using var context = await binding.CreateContextAsync(ModuleFixture.AlphaFactory, fixture.Token);
            var record = Assert.Single(context.Records);
            Assert.Equal(1, record.Id);
            Assert.Equal("retained-identity-module", record.Payload);
            Assert.Equal("retained-note", record.Note);
            Assert.Equal(manifest.RegistryRevision,
                (await RecoverySetVerifier.InspectStoppedAsync(installation, scope, fixture.Token)).RegistryRevision);
        }
        await Assert.ThrowsAsync<StorageException>(() => IdentityRestoreStore.ReconcileAsync(installation,
            Assert.IsType<MaintenanceScope>(scope), quarantine, decision, "oidc", new string('a', 64),
            Guid.NewGuid(), budget));
    }

    private static async Task PopulateModuleAsync(StorageInstallation installation, CancellationToken token)
    {
        await using var scope = await new StorageMaintenance(installation).AcquireAsync(token);
        var registry = new StorageRegistry(installation);
        var module = RegistryFixture.Module("org.example.alpha");
        Assert.IsType<StorageResult.Ready>(await registry.RegisterAsync(module, scope, token));
        await using var binding = await registry.BindAsync(module.Id, token);
        Assert.IsType<StorageResult.Ready>(await binding.MigrateAsync(
            ModuleFixture.AlphaFactory, scope, cancellationToken: token));
        await using var context = await binding.CreateContextAsync(ModuleFixture.AlphaFactory, token);
        context.Records.Add(new AlphaRecord { Id = 1, Payload = "retained-identity-module", Note = "retained-note" });
        await context.SaveChangesAsync(token);
    }

    private static async Task VerifyIncompleteJournalAuthorityAsync(StorageInstallation installation,
        IdentityRestoreQuarantine quarantine, string directory, CancellationToken token)
    {
        var marker = Path.Combine(directory, IdentityRestoreQuarantine.FileName);
        var original = await File.ReadAllBytesAsync(marker, token);
        var pending = marker + ".next";
        PrivateTestFiles.WriteNew(pending, "{"u8);
        var decision = new IdentityRestoreDecision(quarantine.Manifest.RecoverySetId,
            quarantine.Manifest.InstallationId, quarantine.ManifestSha256, "not-established");
        var foreign = installation.Postgres is { } postgres
            ? StorageInstallation.PostgreSql(postgres.ConnectionString) : StorageInstallation.Sqlite(installation.Root!);
        using var budget = new OperationBudget(OperationBudget.Register, token);
        await using (var scope = await MaintenanceScope.AcquireAsync(installation, token))
        {
            var unbound = decision with { ExpectedManifestSha256 = new string('0', 64) };
            var invalid = await Assert.ThrowsAsync<StorageException>(() => quarantine.ResumeAsync(unbound,
                "oidc", new string('a', 64), installation, scope, budget));
            Assert.Equal(StorageCode.ReconciliationRequired, invalid.Code);
            var rejected = await Assert.ThrowsAsync<StorageException>(() => quarantine.ResumeAsync(decision,
                "oidc", new string('a', 64), foreign, scope, budget));
            Assert.Equal(StorageCode.RequestInvalid, rejected.Code);
            await scope.DisposeAsync();
            rejected = await Assert.ThrowsAsync<StorageException>(() => quarantine.ResumeAsync(decision,
                "oidc", new string('a', 64), installation, scope, budget));
            Assert.Equal(StorageCode.RequestInvalid, rejected.Code);
        }
        Assert.Equal("{"u8.ToArray(), await File.ReadAllBytesAsync(pending, token));
        using (var caller = CancellationTokenSource.CreateLinkedTokenSource(token))
        {
            await using var cancelled = await MaintenanceScope.AcquireAsync(installation, caller.Token);
            caller.Cancel();
            var rejected = await Assert.ThrowsAsync<StorageException>(() => quarantine.ResumeAsync(decision,
                "oidc", new string('a', 64), installation, cancelled, budget));
            Assert.Equal(StorageCode.Cancelled, rejected.Code);
        }
        Assert.Equal("{"u8.ToArray(), await File.ReadAllBytesAsync(pending, token));
        if (installation.Postgres is { } postgresIdentity)
        {
            await using var lost = await MaintenanceScope.AcquireAsync(installation, token);
            await using var connection = new Npgsql.NpgsqlConnection(postgresIdentity.ConnectionString);
            await connection.OpenAsync(token);
            await using var terminate = new Npgsql.NpgsqlCommand("""
                SELECT pg_catalog.pg_terminate_backend(l.pid) FROM pg_catalog.pg_locks l
                  JOIN pg_catalog.pg_stat_activity a ON a.pid=l.pid
                  WHERE l.locktype='advisory' AND l.granted AND l.mode='ExclusiveLock'
                    AND l.classid=(($1::bigint >> 32) & 4294967295)::oid
                    AND l.objid=($1::bigint & 4294967295)::oid AND l.objsubid=1
                    AND a.datname=current_database() AND a.usename=current_user AND l.pid<>pg_backend_pid()
                """, connection) { CommandTimeout = 3 };
            terminate.Parameters.AddWithValue(Kofte.Infrastructure.Storage.Postgres.PostgresIdentity.LeaseKey);
            Assert.Equal(true, await terminate.ExecuteScalarAsync(token));
            var failure = await Record.ExceptionAsync(() => quarantine.ResumeAsync(decision,
                "oidc", new string('a', 64), installation, lost, budget));
            Assert.True(failure is Npgsql.NpgsqlException or StorageException { Code: StorageCode.Unavailable });
            Assert.True(lost.Lifetime.IsCancellationRequested);
            var rejected = await Assert.ThrowsAsync<StorageException>(() => quarantine.ResumeAsync(decision,
                "oidc", new string('a', 64), installation, lost, budget));
            Assert.Equal(StorageCode.Cancelled, rejected.Code);
            Assert.Equal("{"u8.ToArray(), await File.ReadAllBytesAsync(pending, token));
            Assert.Equal(original, await File.ReadAllBytesAsync(marker, token));
        }
        await using (var scope = await MaintenanceScope.AcquireAsync(installation, token))
        {
            using var expired = new OperationBudget(TimeSpan.FromMilliseconds(1), token);
            await Task.Delay(20, token);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => quarantine.ResumeAsync(decision,
                "oidc", new string('a', 64), installation, scope, expired));
            Assert.Equal("{"u8.ToArray(), await File.ReadAllBytesAsync(pending, token));
            Assert.Equal(original, await File.ReadAllBytesAsync(marker, token));
            await File.AppendAllTextAsync(marker, "\n", token);
            var changed = await File.ReadAllBytesAsync(marker, token);
            var conflict = await Assert.ThrowsAsync<StorageException>(() => quarantine.ResumeAsync(decision,
                "oidc", new string('a', 64), installation, scope, budget));
            Assert.Equal(StorageCode.Conflict, conflict.Code);
            Assert.Equal(changed, await File.ReadAllBytesAsync(marker, token));
            Assert.Equal("{"u8.ToArray(), await File.ReadAllBytesAsync(pending, token));
            await File.WriteAllBytesAsync(marker, original, token);
            _ = await quarantine.ResumeAsync(decision, "oidc", new string('a', 64), installation, scope, budget);
        }
        Assert.False(File.Exists(pending));
        Assert.Equal(original, await File.ReadAllBytesAsync(marker, token));
        Assert.True(IdentityRestoreQuarantine.IsRequired(directory));
    }

    private async Task<StorageInstallation> PrepareAsync(StorageProviderScope fixture, bool consumed = true)
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
              VALUES (@created,2,'user_created',@now,@correlation,@user,'committed','installation');
            UPDATE {prefix}__kofte_identity_metadata SET security_revision=2;
            """, budget, null, ("user", UserId), ("now", now),
            ("created", Guid.NewGuid()), ("correlation", Guid.NewGuid().ToString("N")));
        if (consumed)
            await RegistryMigrationRunner.ExecuteAsync(connection, $"""
                INSERT INTO {prefix}__kofte_identity_events
                  (event_id,security_revision,event_code,occurred_at_utc,correlation_id,user_id,outcome,retention_class)
                  VALUES (@enrolled,3,'initial_administrator_enrolled',@now,@correlation,@user,'committed','installation');
                UPDATE {prefix}__kofte_identity_metadata SET security_revision=3;
                UPDATE {prefix}__kofte_bootstrap SET state='consumed',generation=1,initial_user_id=@user,
                  consumed_at_utc=@now,consumed_generation=1;
                """, budget, null, ("user", UserId), ("now", now),
                ("enrolled", Guid.NewGuid()), ("correlation", Guid.NewGuid().ToString("N")));
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

    private static async Task<RecoveryManifest> RehashManifestAsync(string path, RecoveryManifest manifest,
        CancellationToken token)
    {
        var artifacts = new List<RecoveryArtifact>();
        foreach (var artifact in manifest.Artifacts)
            artifacts.Add(await RecoverySetVerifier.DescribeArtifactAsync(path, artifact.Name, token));
        File.Delete(Path.Combine(path, RecoveryManifest.FileName));
        var rehashed = manifest with { Artifacts = artifacts };
        await RecoverySetVerifier.WriteManifestAsync(path, rehashed, token);
        return rehashed;
    }

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
