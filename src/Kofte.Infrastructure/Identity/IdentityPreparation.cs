using System.Data.Common;
using System.Globalization;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;

namespace Kofte.Infrastructure.Identity;

internal static class IdentityPreparation
{
    internal static async Task InitializeAsync(StorageInstallation installation, MaintenanceScope scope,
        DbConnection connection, Guid installationId, string mode, string digest, OperationBudget budget)
    {
        await using var transaction = connection is SqliteConnection sqlite
            ? sqlite.BeginTransaction(deferred: false)
            : await connection.BeginTransactionAsync(budget.Token);
        var registry = new RegistryMigrationRunner(installation);
        await registry.ApplyIdentityFormatAsync(scope, connection, transaction, budget);
        await IdentityStore.InitializeAsync(installation, scope, connection, transaction, mode, digest,
            DateTime.UtcNow, budget);
        await using (var database = DeadlineStore.Borrow(connection, transaction, budget))
        {
            var metadata = await IdentityStore.ReadMetadataAsync(database);
            IdentityStore.Match(metadata, installationId, mode, digest);
            await database.QueryAsync($"""
                INSERT INTO {database.Prefix}__kofte_bootstrap
                (singleton_key,installation_id,state,generation,initial_user_mode)
                VALUES (1,@installation,'disabled',0,'oidc')
                """, ("installation", installationId));
            await IdentityEventStore.AppendAsync(new(database, metadata, checked(metadata.Revision + 1)),
                IdentityEventCode.KeysPrepared, DateTime.UtcNow);
            await database.QueryAsync($"""
                UPDATE {database.Prefix}__kofte_identity_metadata SET security_revision=@revision
                WHERE singleton_key=1
                """, ("revision", checked(metadata.Revision + 1)));
        }
        await scope.VerifyAsync(installation, budget);
        IdentityDiagnostics.Checkpoint("operator_before_commit");
        budget.Check();
        IdentityDiagnostics.Current?.PossibleCommit();
        await transaction.CommitAsync(budget.Token);
        IdentityDiagnostics.Current?.Committed();
        IdentityDiagnostics.Checkpoint("operator_committed");
    }

    internal static async Task VerifyAsync(StorageInstallation installation, Guid installationId,
        string mode, string digest, OperationBudget budget, bool reconcile = false)
    {
        await using var database = await DeadlineStore.OpenAsync(installation, budget);
        var metadata = await IdentityStore.ReadMetadataAsync(database);
        if (metadata.InstallationId != installationId) throw new StorageException(StorageCode.OwnerMismatch);
        if (!reconcile) IdentityStore.Match(metadata, installationId, mode, digest);
        var rows = await database.QueryAsync($"""
            SELECT installation_id FROM {database.Prefix}__kofte_bootstrap WHERE singleton_key=1
            """);
        if (DeadlineStore.AssertScalar(rows) is not { } value ||
            !Guid.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var owner) || owner != installationId)
            throw new StorageException(StorageCode.Corrupt);
        await IdentityUserStore.VerifyCorrespondenceAsync(database);
    }

    internal static async Task RevokeAsync(StorageInstallation installation, MaintenanceScope scope,
        Guid installationId, string mode, string digest, bool reconcile, OperationBudget budget)
    {
        await IdentityStore.MutateStoppedAsync(installation, scope, installationId, mode, digest, reconcile, budget,
            async change =>
            {
                var now = DateTime.UtcNow;
                var database = change.Database;
                await database.QueryAsync($"""
                    UPDATE {database.Prefix}__kofte_sessions SET state='revoked',terminal_at_utc=@now
                    WHERE state='active'
                    """, ("now", now));
                await database.QueryAsync($"""
                    UPDATE {database.Prefix}__kofte_login_attempts SET state='consumed',consumed_at_utc=@now
                    WHERE state='pending'
                    """, ("now", now));
                await database.QueryAsync($"""
                    UPDATE {database.Prefix}__kofte_bootstrap
                    SET state='disabled',nominee_issuer=NULL,nominee_subject=NULL,authorization_digest=NULL,expires_at_utc=NULL
                    WHERE state='pending'
                    """);
                await database.QueryAsync($"""
                    UPDATE {database.Prefix}__kofte_identity_metadata
                    SET session_epoch=@epoch,mode=@mode,trust_digest=@digest,reconciled_at_utc=@now
                    WHERE singleton_key=1
                    """, ("epoch", Guid.NewGuid()), ("mode", mode), ("digest", digest), ("now", now));
                return await IdentityEventStore.AppendAsync(change, reconcile
                    ? IdentityEventCode.ConfigurationReconciled : IdentityEventCode.SessionsRevoked, now);
            });
    }
}
