using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Identity;

internal enum IdentityEventCode
{
    UserCreated, InitialAdministratorEnrolled, BootstrapAuthorized, BootstrapDisabled,
    ConfigurationReconciled, SessionsRevoked, RestoreReconciled, KeysPrepared,
    SessionCreated, SessionRevoked, SessionExpired
}

internal sealed record IdentityEventReceipt(Guid EventId, string Correlation);

internal sealed class IdentityEventCapacityException() : Exception("identity_event_capacity_exceeded");

internal static class IdentityEventStore
{
    internal static async Task<IdentityEventReceipt> AppendAsync(IdentityMutation change, IdentityEventCode code,
        DateTime now, Guid? userId = null)
    {
        if (now.Kind != DateTimeKind.Utc || userId == Guid.Empty || change.Revision <= change.Before.Revision)
            throw new StorageException(StorageCode.RequestInvalid);
        var (name, retention) = Rule(code);
        var database = change.Database;
        var table = database.Prefix + "__kofte_identity_events";
        if (!change.EventsPruned)
        {
            await PruneAsync(database, now);
            change.EventsPruned = true;
        }
        var limit = retention == "installation" ? 1_000_000L : 200_000L;
        if (DeadlineStore.AssertScalar(await database.QueryAsync(
                $"SELECT COUNT(*) FROM {table} WHERE retention_class=@retention", ("retention", retention)))
            is not long count || count < 0)
            throw new StorageException(StorageCode.Corrupt);
        if (count >= limit) throw new IdentityEventCapacityException();
        var receipt = new IdentityEventReceipt(Guid.NewGuid(), change.Correlation);
        await database.QueryAsync($"""
            INSERT INTO {table}
            (event_id,security_revision,event_code,occurred_at_utc,correlation_id,user_id,outcome,retention_class)
            VALUES (@id,@revision,@code,@now,@correlation,@user,'committed',@retention)
            """, ("id", receipt.EventId), ("revision", change.Revision), ("code", name), ("now", now),
            ("correlation", receipt.Correlation), ("user", userId), ("retention", retention));
        if (count + 1 >= limit * 9 / 10)
            Console.Error.WriteLine(JsonSerializer.Serialize(new
            {
                eventCode = "identity_event_capacity_warning",
                correlation = receipt.Correlation,
                retention,
                capacity = "at_least_ninety_percent",
                durability = "unchanged"
            }));
        return receipt;
    }

    internal static async Task<int> PruneAsync(DeadlineStore database, DateTime now)
    {
        if (now.Kind != DateTimeKind.Utc) throw new StorageException(StorageCode.RequestInvalid);
        if (now.Ticks < TimeSpan.FromDays(30).Ticks) return 0;
        var table = database.Prefix + "__kofte_identity_events";
        var deleted = await database.QueryAsync($"""
            DELETE FROM {table} WHERE event_id IN
            (SELECT event_id FROM {table} WHERE retention_class='session' AND occurred_at_utc<=@cutoff
             ORDER BY occurred_at_utc,event_id LIMIT 128) RETURNING event_id
            """, ("cutoff", now.AddDays(-30)));
        return deleted.Count;
    }

    private static (string Name, string Retention) Rule(IdentityEventCode code) => code switch
    {
        IdentityEventCode.UserCreated => ("user_created", "installation"),
        IdentityEventCode.InitialAdministratorEnrolled => ("initial_administrator_enrolled", "installation"),
        IdentityEventCode.BootstrapAuthorized => ("bootstrap_authorized", "installation"),
        IdentityEventCode.BootstrapDisabled => ("bootstrap_disabled", "installation"),
        IdentityEventCode.ConfigurationReconciled => ("configuration_reconciled", "installation"),
        IdentityEventCode.SessionsRevoked => ("sessions_revoked", "installation"),
        IdentityEventCode.RestoreReconciled => ("restore_reconciled", "installation"),
        IdentityEventCode.KeysPrepared => ("keys_prepared", "installation"),
        IdentityEventCode.SessionCreated => ("session_created", "session"),
        IdentityEventCode.SessionRevoked => ("session_revoked", "session"),
        IdentityEventCode.SessionExpired => ("session_expired", "session"),
        _ => throw new StorageException(StorageCode.RequestInvalid)
    };
}
