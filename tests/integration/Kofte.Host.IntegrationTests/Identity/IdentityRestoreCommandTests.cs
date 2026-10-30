using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.TestSupport;
using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

public abstract class IdentityRestoreCommandTests : IdentityCommandTests
{
    [Fact]
    public Task ActualRestoreCommandKeepsItsDecisionEpochAndFreshKeysAcrossRetry() => RestoreCommandAsync(null);

    [Fact]
    public Task LegacyRestoreRequiresExplicitUpgradeAndLocksUnestablishedHistory() =>
        RestoreCommandAsync(null, legacy: true);

    [Fact]
    public Task LegacyRestoreResumesItsCommittedUpgradeBeforeTheIdentityDecisionWasRecorded() =>
        RestoreCommandAsync("operator_committed", legacy: true);

    [Theory]
    [InlineData("restore_state_written")]
    [InlineData("restore_key_copied")]
    [InlineData("restore_before_commit")]
    [InlineData("operator_before_commit")]
    [InlineData("operator_committed")]
    [InlineData("restore_committed")]
    public Task ActualRestoreCommandKeepsItsDecisionEpochAndFreshKeysAcrossInterruptedRetry(string phase) =>
        RestoreCommandAsync(phase);

    [Theory]
    [InlineData("restore_intent_partial")]
    [InlineData("restore_keys_partial")]
    [InlineData("restore_result_partial")]
    [InlineData("restore_intent_empty")]
    public Task ActualRestoreCommandResumesARealPartiallyFlushedJournal(string phase) =>
        RestoreCommandAsync(phase);

    [Theory]
    [InlineData("unknown")]
    [InlineData("binding")]
    [InlineData("invalid")]
    public Task ActualRestoreCommandNeverDiscardsACompleteConflictingOrInvalidJournal(string fault) =>
        RestoreCommandAsync("restore_state_written", invalidPending: fault);

    private async Task RestoreCommandAsync(string? phase, bool legacy = false, string? invalidPending = null)
    {
        await using var fixture = await IdentityFixture.CreateAsync(Provider, Token);
        Configure(fixture);
        Assert.Equal(0, await RunAsync(fixture, "--registry", "upgrade"));
        if (!legacy) Assert.Equal(0, await RunAsync(fixture, "--identity", "prepare"));
        var oldOwner = legacy ? null :
            await File.ReadAllBytesAsync(Path.Combine(fixture.KeyRingDirectory, "identity-owner.json"), Token);
        using var backup = new OwnedTestRoot();
        var manifest = fixture.Storage.Postgres is { } source
            ? await source.BackupStorageSetAsync(backup.Path, fixture.Storage.Token)
            : await SqliteRecoveryExercise.BackupAsync(fixture.Storage.DataRoot, backup.Path, fixture.Storage.Token);
        var retained = Path.Combine(fixture.PrivateDirectory, "original-keys");
        if (!legacy) Directory.Move(fixture.KeyRingDirectory, retained);
        using var sqliteTarget = new OwnedTestRoot();
        await using var postgresTarget = Provider == "postgres"
            ? await StorageProviderScope.CreateAsync(Provider, Token) : null;
        if (fixture.Storage.Postgres is { } postgres)
            await postgres.RestoreStorageSetToFreshQuarantinedAsync(backup.Path,
                Assert.IsType<PostgresResources>(postgresTarget?.Postgres), fixture.KeyRingDirectory, fixture.Storage.Token);
        else await SqliteRecoveryExercise.RestoreFreshQuarantinedAsync(
            backup.Path, sqliteTarget.Path, fixture.KeyRingDirectory, fixture.Storage.Token);
        var quarantine = await IdentityRestoreQuarantine.ReadAsync(fixture.KeyRingDirectory, Token);
        var decision = Path.Combine(fixture.PrivateDirectory, "restore-decision.json");
        PrivateTestFiles.WriteNew(decision, JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = "1",
            recoverySetId = manifest.RecoverySetId.ToString("D"),
            installationId = manifest.InstallationId.ToString("D"),
            expectedManifestSha256 = quarantine.ManifestSha256,
            priorEnrollment = "not-established"
        }));
        void Target(ProcessStartInfo start)
        {
            start.Environment["KOFTE_DATA_ROOT"] = postgresTarget?.DataRoot ?? sqliteTarget.Path;
            if (postgresTarget?.Postgres is { } target)
            {
                start.Environment["KOFTE_POSTGRES_CONNECTION"] = target.ConnectionString;
                start.Environment["KOFTE_POSTGRES_PROVISIONING_CONNECTION"] = target.ProvisioningConnectionString;
            }
        }
        Guid? plannedEpoch = null, plannedRing = null;
        var copied = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        if (phase is not null)
        {
            var barrier = new IdentityCommitBarrier(fixture.PrivateDirectory, phase, maintenanceChild: true);
            await using var interrupted = fixture.Start(["--identity", "reconcile-restore", "--decision-file", decision],
                barrier: barrier, configure: Target);
            await barrier.WaitAsync(fixture.Storage.Token);
            using var worker = Process.GetProcessById(barrier.ObservedProcessId!.Value);
            worker.Kill(entireProcessTree: true);
            await worker.WaitForExitAsync(fixture.Storage.Token);
            var failed = await interrupted.WaitAsync(fixture.Storage.Token);
            Assert.NotEqual(0, failed.ExitCode);
            Assert.True(interrupted.ExitVerified && interrupted.DescendantExitsVerified);
            Assert.True(IdentityRestoreQuarantine.IsRequired(fixture.KeyRingDirectory));
            var marker = Path.Combine(fixture.KeyRingDirectory, IdentityRestoreQuarantine.FileName);
            var partial = phase.EndsWith("_partial", StringComparison.Ordinal) || phase == "restore_intent_empty";
            if (partial)
            {
                var incomplete = await File.ReadAllBytesAsync(marker + ".next", Token);
                Assert.ThrowsAny<JsonException>(() => JsonDocument.Parse(incomplete));
            }
            using var state = JsonDocument.Parse(await File.ReadAllBytesAsync(
                !partial && File.Exists(marker + ".next") ? marker + ".next" : marker, Token));
            if (state.RootElement.TryGetProperty("plan", out var plan) && plan.ValueKind == JsonValueKind.Object)
            {
                plannedEpoch = plan.GetProperty("epoch").GetGuid();
                plannedRing = plan.GetProperty("ringId").GetGuid();
            }
            foreach (var path in Directory.GetFiles(fixture.KeyRingDirectory)
                .Where(path => Path.GetFileName(path).StartsWith("key-", StringComparison.Ordinal) ||
                    Path.GetFileName(path) == "identity-owner.json"))
                copied.Add(Path.GetFileName(path), await File.ReadAllBytesAsync(path, Token));
            if (invalidPending is not null)
            {
                var pending = marker + ".next";
                var original = await File.ReadAllBytesAsync(pending, Token);
                var invalid = JsonNode.Parse(original)!;
                if (invalidPending == "unknown") invalid["unexpected"] = true;
                else if (invalidPending == "binding")
                    invalid["plan"]!["decision"]!["expectedManifestSha256"] = new string('0', 64);
                var damaged = invalidPending == "invalid" ? "{\"version\":!" : invalid.ToJsonString();
                await File.WriteAllTextAsync(pending, damaged, Token);
                var before = await File.ReadAllBytesAsync(marker, Token);
                await using (var refused = fixture.Start(["--identity", "reconcile-restore", "--decision-file", decision],
                    configure: Target))
                {
                    Assert.NotEqual(0, (await refused.WaitAsync(fixture.Storage.Token)).ExitCode);
                    Assert.True(refused.ExitVerified && refused.DescendantExitsVerified);
                }
                Assert.Equal(damaged, await File.ReadAllTextAsync(pending, Token));
                Assert.Equal(before, await File.ReadAllBytesAsync(marker, Token));
                Assert.True(IdentityRestoreQuarantine.IsRequired(fixture.KeyRingDirectory));
                await File.WriteAllBytesAsync(pending, original, Token);
            }
            if (plannedEpoch is not null)
            {
                var original = await File.ReadAllBytesAsync(decision, Token);
                var other = JsonNode.Parse(original)!;
                other["priorEnrollment"] = "known-never-enrolled";
                await File.WriteAllTextAsync(decision, other.ToJsonString(), Token);
                await using (var refused = fixture.Start(["--identity", "reconcile-restore", "--decision-file", decision],
                    configure: Target))
                {
                    var output = await refused.WaitAsync(fixture.Storage.Token);
                    Assert.NotEqual(0, output.ExitCode);
                    Assert.Contains("identity_reconciliation_required", output.Stdout + output.Stderr, StringComparison.Ordinal);
                }
                Assert.True(IdentityRestoreQuarantine.IsRequired(fixture.KeyRingDirectory));
                await File.WriteAllBytesAsync(decision, original, Token);
            }
        }
        var installation = postgresTarget?.Postgres is { } restored
            ? StorageInstallation.PostgreSql(restored.ConnectionString)
            : StorageInstallation.Sqlite(sqliteTarget.Path);
        async Task<IdentityRecoverySummary> Snapshot()
        {
            await using var stopped = await new StorageMaintenance(installation).AcquireAsync(Token);
            return await IdentityRecoverySnapshot.CaptureStoppedAsync(installation, stopped, Token);
        }
        await using (var retry = fixture.Start(["--identity", "reconcile-restore", "--decision-file", decision],
            configure: Target))
        {
            var output = await retry.WaitAsync(fixture.Storage.Token);
            Assert.True(output.ExitCode == 0, output.Stdout + output.Stderr);
            Assert.DoesNotContain(fixture.PrivateDirectory, output.Stdout + output.Stderr, StringComparison.Ordinal);
            Assert.True(retry.ExitVerified && retry.DescendantExitsVerified);
        }
        Assert.False(IdentityRestoreQuarantine.IsRequired(fixture.KeyRingDirectory));
        var after = await Snapshot();
        Assert.Equal(legacy ? 2 : manifest.Identity!.SecurityRevision + 1, after.SecurityRevision);
        if (!legacy) Assert.NotEqual(manifest.Identity!.SessionEpoch, after.SessionEpoch);
        Assert.Equal("recovery_locked", after.BootstrapState);
        if (plannedEpoch is not null) Assert.Equal(plannedEpoch, after.SessionEpoch);
        Assert.Equal(legacy ? new IdentityRecoveryCounts(0, 0, 0, 0, 2, 0) :
            manifest.Identity!.Counts with { Events = manifest.Identity.Counts.Events + 1 }, after.Counts);
        var newOwner = await File.ReadAllBytesAsync(Path.Combine(fixture.KeyRingDirectory, "identity-owner.json"), Token);
        Assert.NotEqual(oldOwner, newOwner);
        if (plannedRing is not null)
        {
            using var owner = JsonDocument.Parse(newOwner);
            Assert.Equal(plannedRing, owner.RootElement.GetProperty("ringId").GetGuid());
        }
        foreach (var file in copied)
            Assert.Equal(file.Value, await File.ReadAllBytesAsync(Path.Combine(fixture.KeyRingDirectory, file.Key), Token));
        await using (var unchanged = fixture.Start(["--identity", "reconcile-restore", "--decision-file", decision],
            configure: Target))
            Assert.Equal(0, (await unchanged.WaitAsync(fixture.Storage.Token)).ExitCode);
        Assert.Equal(after, await Snapshot());
        Assert.Equal(newOwner, await File.ReadAllBytesAsync(
            Path.Combine(fixture.KeyRingDirectory, "identity-owner.json"), Token));
        if (!legacy)
            Assert.Equal(oldOwner, await File.ReadAllBytesAsync(Path.Combine(retained, "identity-owner.json"), Token));
        var originalDecision = await File.ReadAllBytesAsync(decision, Token);
        var changedDecision = JsonNode.Parse(originalDecision)!;
        changedDecision["priorEnrollment"] = "known-never-enrolled";
        await File.WriteAllTextAsync(decision, changedDecision.ToJsonString(), Token);
        await using (var refused = fixture.Start(["--identity", "reconcile-restore", "--decision-file", decision],
            configure: Target))
        {
            var output = await refused.WaitAsync(fixture.Storage.Token);
            Assert.NotEqual(0, output.ExitCode);
            Assert.Contains("identity_reconciliation_required", output.Stdout + output.Stderr, StringComparison.Ordinal);
        }
        Assert.Equal(after, await Snapshot());
        await File.WriteAllBytesAsync(decision, originalDecision, Token);
    }
}

[Trait("Provider", "sqlite"), Trait("Suite", "recovery")]
public sealed class SqliteIdentityRestoreCommandTests : IdentityRestoreCommandTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres"), Trait("Suite", "recovery")]
public sealed class PostgresIdentityRestoreCommandTests : IdentityRestoreCommandTests
{
    protected override string Provider => "postgres";
}
