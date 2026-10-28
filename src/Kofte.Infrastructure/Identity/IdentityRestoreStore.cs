using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Identity;

internal static class IdentityRestoreStore
{
    internal static async Task<IdentityRecoverySummary> ReconcileAsync(StorageInstallation installation,
        MaintenanceScope scope, IdentityRestoreQuarantine quarantine, IdentityRestoreDecision decision,
        string mode, string digest, Guid epoch, OperationBudget budget)
    {
        decision.Validate(quarantine);
        var expected = quarantine.ExpectedIdentity
            ?? throw new StorageException(StorageCode.VersionUnsupported);
        if (epoch == Guid.Empty || epoch == expected.SessionEpoch)
            throw new StorageException(StorageCode.RequestInvalid);
        var revision = checked(expected.SecurityRevision + 1);
        var actual = await InspectAsync(installation, scope, quarantine.Manifest, budget);
        if (actual.SecurityRevision == revision && actual.SessionEpoch == epoch)
            return await VerifyResultAsync(installation, scope, quarantine.Manifest, decision, expected, mode, digest, epoch, budget);
        if (actual != expected) throw new StorageException(StorageCode.ReconciliationRequired);
        await IdentityStore.MutateStoppedAsync(installation, scope, decision.InstallationId, mode, digest,
            reconcile: true, budget, async change =>
            {
                if (change.Before.Revision != expected.SecurityRevision || change.Before.Epoch != expected.SessionEpoch)
                    throw new StorageException(StorageCode.Conflict);
                var now = DateTime.UtcNow;
                var database = change.Database;
                await IdentityPreparation.RevokeSessionsAndAttemptsAsync(database, now);
                await database.QueryAsync($"DELETE FROM {database.Prefix}__kofte_identity_admission");
                await database.QueryAsync($"""
                    UPDATE {database.Prefix}__kofte_bootstrap SET state=@state,nominee_issuer=NULL,
                      nominee_subject=NULL,authorization_digest=NULL,expires_at_utc=NULL
                    WHERE singleton_key=1 AND state=@previous
                    """, ("state", BootstrapState(expected, decision)), ("previous", expected.BootstrapState));
                await database.QueryAsync($"""
                    UPDATE {database.Prefix}__kofte_identity_metadata
                    SET session_epoch=@epoch,mode=@mode,trust_digest=@digest,reconciled_at_utc=@now
                    WHERE singleton_key=1
                    """, ("epoch", epoch), ("mode", mode), ("digest", digest), ("now", now));
                // Preserve recovered evidence; retention has its own bounded maintenance command.
                change.EventsPruned = true;
                return await IdentityEventStore.AppendAsync(change, IdentityEventCode.RestoreReconciled, now);
            });
        return await VerifyResultAsync(installation, scope, quarantine.Manifest, decision, expected, mode, digest, epoch, budget);
    }

    internal static async Task<IdentityRecoverySummary> VerifyReconciledAsync(StorageInstallation installation,
        MaintenanceScope scope, IdentityRestoreQuarantine quarantine, IdentityRestoreDecision decision,
        string mode, string digest, Guid epoch, OperationBudget budget)
    {
        decision.Validate(quarantine);
        var expected = quarantine.ExpectedIdentity
            ?? throw new StorageException(StorageCode.VersionUnsupported);
        return await VerifyResultAsync(installation, scope, quarantine.Manifest, decision, expected, mode, digest, epoch, budget);
    }

    internal static async Task<IdentityRecoverySummary> InspectAsync(StorageInstallation installation,
        MaintenanceScope scope, RecoveryManifest manifest, OperationBudget budget)
    {
        await scope.VerifyAsync(installation, budget);
        // Explicit stopped retry may encounter an inactive SQLite crash journal.
        // Keep clean-backup readers strict; verify the owned live connection instead.
        await using var connection = await installation.OpenPlatformAsync(budget);
        var inventory = await RecoverySetVerifier.ReadInventoryAsync(installation, connection, budget);
        if (!RecoverySetVerifier.SameInventory(manifest with { RegistryVersion = 2 }, inventory))
            throw new StorageException(StorageCode.ReconciliationRequired);
        var result = await IdentityRecoverySnapshot.ReadAsync(installation, connection, budget);
        await scope.VerifyAsync(installation, budget);
        return result;
    }

    internal static async Task<IdentityRecoverySummary> PrepareLegacyAsync(StorageInstallation installation,
        MaintenanceScope scope, IdentityRestoreQuarantine quarantine, string mode, string digest, OperationBudget budget)
    {
        if (quarantine.Manifest.RegistryVersion != 1 ||
            quarantine.Plan is not { Keys: not null, PreparedIdentity: null })
            throw new StorageException(StorageCode.RequestInvalid);
        await scope.VerifyAsync(installation, budget);
        await using var connection = await installation.OpenPlatformAsync(budget);
        var inventory = await RecoverySetVerifier.ReadInventoryAsync(installation, connection, budget);
        if (!RecoverySetVerifier.SameInventory(
                quarantine.Manifest with { RegistryVersion = inventory.RegistryVersion }, inventory))
            throw new StorageException(StorageCode.ReconciliationRequired);
        if (inventory.RegistryVersion == 1)
            await IdentityPreparation.InitializeAsync(installation, scope, connection,
                quarantine.Manifest.InstallationId, mode, digest, budget);
        else if (inventory.RegistryVersion != 2) throw new StorageException(StorageCode.VersionUnsupported);
        var prepared = await InspectAsync(installation, scope, quarantine.Manifest, budget);
        if (prepared.SecurityRevision != 1 || prepared.BootstrapState != "disabled" ||
            prepared.ConsumedGeneration is not null || prepared.Counts != new IdentityRecoveryCounts(0, 0, 0, 0, 1, 0))
            throw new StorageException(StorageCode.ReconciliationRequired);
        await using var database = await DeadlineStore.OpenAsync(installation, budget);
        IdentityStore.Match(await IdentityStore.ReadMetadataAsync(database), quarantine.Manifest.InstallationId, mode, digest);
        if (DeadlineStore.AssertScalar(await database.QueryAsync($"""
            SELECT COUNT(*) FROM {database.Prefix}__kofte_identity_events
            WHERE event_code='keys_prepared' AND security_revision=1
            """)) is not 1L)
            throw new StorageException(StorageCode.ReconciliationRequired);
        await scope.VerifyAsync(installation, budget);
        return prepared;
    }

    private static async Task<IdentityRecoverySummary> VerifyResultAsync(StorageInstallation installation,
        MaintenanceScope scope, RecoveryManifest manifest, IdentityRestoreDecision decision, IdentityRecoverySummary expected,
        string mode, string digest, Guid epoch, OperationBudget budget)
    {
        var actual = await InspectAsync(installation, scope, manifest, budget);
        if (actual.SecurityRevision != checked(expected.SecurityRevision + 1) || actual.SessionEpoch != epoch ||
            actual.BootstrapState != BootstrapState(expected, decision) ||
            actual.ConsumedGeneration != expected.ConsumedGeneration ||
            actual.Counts != expected.Counts with { AdmissionWindows = 0, Events = checked(expected.Counts.Events + 1) })
            throw new StorageException(StorageCode.ReconciliationRequired);
        await using var database = await DeadlineStore.OpenAsync(installation, budget);
        IdentityStore.Match(await IdentityStore.ReadMetadataAsync(database), decision.InstallationId, mode, digest);
        if (DeadlineStore.AssertScalar(await database.QueryAsync($"""
                SELECT COUNT(*) FROM {database.Prefix}__kofte_identity_events
                WHERE event_code='restore_reconciled' AND security_revision=@revision
                """, ("revision", actual.SecurityRevision))) is not 1L ||
            DeadlineStore.AssertScalar(await database.QueryAsync(
                $"SELECT COUNT(*) FROM {database.Prefix}__kofte_sessions WHERE state='active'")) is not 0L ||
            DeadlineStore.AssertScalar(await database.QueryAsync(
                $"SELECT COUNT(*) FROM {database.Prefix}__kofte_login_attempts WHERE state='pending'")) is not 0L)
            throw new StorageException(StorageCode.ReconciliationRequired);
        await scope.VerifyAsync(installation, budget);
        return actual;
    }

    internal static string BootstrapState(IdentityRecoverySummary expected, IdentityRestoreDecision decision) =>
        expected.BootstrapState switch
        {
            "consumed" => "consumed",
            "recovery_locked" => "recovery_locked",
            _ => decision.PriorEnrollment == "known-never-enrolled" ? "disabled" : "recovery_locked"
        };
}
