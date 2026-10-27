using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;
using static Kofte.Infrastructure.Identity.IdentityValues;

namespace Kofte.Infrastructure.Identity;

internal sealed record IdentityLoginAttempt(Guid AttemptId, string BrowserDigest, string Mode,
    string TrustDigest, Guid Epoch, string ReturnPath, DateTime CreatedAt, DateTime ExpiresAt);

internal sealed class IdentityAttemptException() : Exception("identity_attempt_invalid");

internal static class IdentityLoginAttempts
{
    private const string Columns = """
        attempt_id,browser_digest,mode,trust_digest,session_epoch,return_path,created_at_utc,expires_at_utc
        """;

    internal static async Task<IdentityLoginAttempt> StartAsync(IdentityMutation change, string browserDigest,
        string returnPath, DateTime now)
    {
        Validate(browserDigest, now);
        if (returnPath is not ("/" or "/identity") || now > DateTime.MaxValue.AddMinutes(-5))
            throw new StorageException(StorageCode.RequestInvalid);
        var database = change.Database;
        var table = database.Prefix + "__kofte_login_attempts";
        await database.QueryAsync($"""
            DELETE FROM {table} WHERE attempt_id IN
            (SELECT attempt_id FROM {table} WHERE state='consumed' OR expires_at_utc<=@now
             ORDER BY expires_at_utc,attempt_id LIMIT 128)
            """, ("now", now));
        if (DeadlineStore.AssertScalar(await database.QueryAsync($"""
                SELECT COUNT(*) FROM {table} WHERE state='pending' AND expires_at_utc>@now
                """, ("now", now))) is not long total || total < 0 ||
            DeadlineStore.AssertScalar(await database.QueryAsync($"""
                SELECT COUNT(*) FROM {table}
                WHERE state='pending' AND expires_at_utc>@now AND browser_digest=@browser
                """, ("now", now), ("browser", browserDigest))) is not long browser || browser < 0)
            throw new StorageException(StorageCode.Corrupt);
        if (total >= 256 || browser >= 8) throw new IdentityAdmissionException(null);
        var rows = await database.QueryAsync($"""
            INSERT INTO {table}
            (attempt_id,browser_digest,mode,trust_digest,session_epoch,return_path,created_at_utc,expires_at_utc,state)
            VALUES (@id,@browser,@mode,@trust,@epoch,@return,@now,@expiry,'pending') RETURNING {Columns}
            """, ("id", Guid.NewGuid()), ("browser", browserDigest), ("mode", change.Before.Mode),
            ("trust", change.Before.TrustDigest), ("epoch", change.Before.Epoch),
            ("return", returnPath), ("now", now), ("expiry", now.AddMinutes(5)));
        return rows.Count == 1 ? Parse(rows[0]) : throw new StorageException(StorageCode.Corrupt);
    }

    internal static async Task<IdentityLoginAttempt> ConsumeAsync(IdentityMutation change, Guid id,
        string browserDigest, DateTime now)
    {
        Validate(browserDigest, now);
        if (id == Guid.Empty) throw new StorageException(StorageCode.RequestInvalid);
        var database = change.Database;
        var rows = await database.QueryAsync($"""
            UPDATE {database.Prefix}__kofte_login_attempts SET state='consumed',consumed_at_utc=@now
            WHERE attempt_id=@id AND browser_digest=@browser AND mode=@mode AND trust_digest=@trust
              AND session_epoch=@epoch AND state='pending' AND created_at_utc<=@now AND expires_at_utc>@now
            RETURNING {Columns}
            """, ("id", id), ("browser", browserDigest), ("mode", change.Before.Mode),
            ("trust", change.Before.TrustDigest), ("epoch", change.Before.Epoch), ("now", now));
        return rows.Count switch
        {
            0 => throw new IdentityAttemptException(),
            1 => Parse(rows[0]),
            _ => throw new StorageException(StorageCode.Corrupt)
        };
    }

    private static void Validate(string browserDigest, DateTime now)
    {
        if (!IdentityStore.IsDigest(browserDigest) || now.Kind != DateTimeKind.Utc)
            throw new StorageException(StorageCode.RequestInvalid);
    }

    private static IdentityLoginAttempt Parse(object?[] row)
    {
        if (row.Length != 8 || row[1] is not string browser || !IdentityStore.IsDigest(browser) ||
            row[2] is not string mode || mode is not ("oidc" or "development") ||
            row[3] is not string trust || !IdentityStore.IsDigest(trust) ||
            row[5] is not string returnPath || returnPath is not ("/" or "/identity"))
            throw new StorageException(StorageCode.Corrupt);
        var created = Utc(row[6]);
        var expiry = Utc(row[7]);
        if (created > DateTime.MaxValue.AddMinutes(-5) || expiry != created.AddMinutes(5))
            throw new StorageException(StorageCode.Corrupt);
        return new(Id(row[0]), browser, mode, trust, Id(row[4]), returnPath, created, expiry);
    }
}
