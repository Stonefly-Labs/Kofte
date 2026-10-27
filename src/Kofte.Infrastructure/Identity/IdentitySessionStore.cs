using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;
using static Kofte.Infrastructure.Identity.IdentityValues;

namespace Kofte.Infrastructure.Identity;

internal sealed record IdentitySession(string Digest, Guid UserId, string Mode, Guid Epoch, byte[] ProtectedTicket,
    DateTime CreatedAt, DateTime LastSeenAt, DateTime IdleExpiresAt, DateTime AbsoluteExpiresAt,
    string State, DateTime? TerminalAt);

internal sealed record IdentitySessionView(IdentitySession Session, IdentityUser User, bool IsInitialAdministrator);

internal static class IdentitySessionStore
{
    private const string Columns = """
        reference_digest,user_id,mode,session_epoch,protected_ticket,created_at_utc,last_seen_at_utc,
        idle_expires_at_utc,absolute_expires_at_utc,state,terminal_at_utc
        """;

    internal static async Task<IdentitySession> CreateAsync(IdentityMutation change, IdentityUser user,
        string digest, byte[] protectedTicket, DateTime now)
    {
        Validate(digest, now);
        if (user.UserId == Guid.Empty || user.Mode != change.Before.Mode || protectedTicket.Length == 0 ||
            now < user.CreatedAt || now > DateTime.MaxValue.AddHours(-8))
            throw new StorageException(StorageCode.RequestInvalid);
        var database = change.Database;
        var table = database.Prefix + "__kofte_sessions";
        if (DeadlineStore.AssertScalar(await database.QueryAsync($"""
                SELECT COUNT(*) FROM {table}
                WHERE state='active' AND idle_expires_at_utc>@now AND absolute_expires_at_utc>@now
                """, ("now", now))) is not long total || total < 0 ||
            DeadlineStore.AssertScalar(await database.QueryAsync($"""
                SELECT COUNT(*) FROM {table} WHERE user_id=@user AND state='active'
                AND idle_expires_at_utc>@now AND absolute_expires_at_utc>@now
                """, ("user", user.UserId), ("now", now))) is not long active || active < 0)
            throw new StorageException(StorageCode.Corrupt);
        if (total >= 50_000 || active >= 8) throw new IdentityAdmissionException(null);
        var rows = await database.QueryAsync($"""
            INSERT INTO {table}
            (reference_digest,user_id,mode,session_epoch,protected_ticket,created_at_utc,last_seen_at_utc,
             idle_expires_at_utc,absolute_expires_at_utc,state)
            VALUES (@digest,@user,@mode,@epoch,@ticket,@now,@now,@idle,@absolute,'active')
            RETURNING {Columns}
            """, ("digest", digest), ("user", user.UserId), ("mode", user.Mode), ("epoch", change.Before.Epoch),
            ("ticket", protectedTicket), ("now", now), ("idle", now.AddMinutes(30)), ("absolute", now.AddHours(8)));
        var session = rows.Count == 1 ? Parse(rows[0]) : throw new StorageException(StorageCode.Corrupt);
        await IdentityEventStore.AppendAsync(change, IdentityEventCode.SessionCreated, now, user.UserId);
        return session;
    }

    internal static async Task<IdentitySessionView?> RetrieveAsync(IdentityMutation change, string digest, DateTime now)
    {
        Validate(digest, now);
        var session = await FindAsync(change.Database, digest);
        if (session is null || session.State != "active" || session.Mode != change.Before.Mode ||
            session.Epoch != change.Before.Epoch) return null;
        if (now < session.CreatedAt || now < session.LastSeenAt)
            throw new StorageException(StorageCode.Unavailable);
        if (now >= session.IdleExpiresAt || now >= session.AbsoluteExpiresAt)
        {
            await TerminateAsync(change, session, "expired", now);
            return null;
        }
        var user = await IdentityUserStore.ReadAsync(change.Database, session.UserId);
        if (user.Mode != session.Mode) throw new StorageException(StorageCode.Corrupt);
        var bootstrap = await change.Database.QueryAsync(
            $"SELECT initial_user_id FROM {change.Database.Prefix}__kofte_bootstrap WHERE singleton_key=1");
        if (bootstrap.Count != 1) throw new StorageException(StorageCode.Corrupt);
        var initial = bootstrap[0][0] is not null && Id(bootstrap[0][0]) == user.UserId;
        var idle = now > session.AbsoluteExpiresAt.AddMinutes(-30)
            ? session.AbsoluteExpiresAt : now.AddMinutes(30);
        var rows = await change.Database.QueryAsync($"""
            UPDATE {change.Database.Prefix}__kofte_sessions SET last_seen_at_utc=@now,idle_expires_at_utc=@idle
            WHERE reference_digest=@digest AND state='active' AND session_epoch=@epoch
              AND last_seen_at_utc=@previous AND idle_expires_at_utc>@now AND absolute_expires_at_utc>@now
            RETURNING {Columns}
            """, ("now", now), ("idle", idle), ("digest", digest), ("epoch", change.Before.Epoch),
            ("previous", session.LastSeenAt));
        if (rows.Count != 1) throw new StorageException(StorageCode.Conflict);
        return new(Parse(rows[0]), user, initial);
    }

    internal static async Task<bool> RevokeAsync(IdentityMutation change, string digest, DateTime now)
    {
        Validate(digest, now);
        var session = await FindAsync(change.Database, digest);
        if (session is null || session.State != "active" || session.Mode != change.Before.Mode ||
            session.Epoch != change.Before.Epoch) return false;
        if (now < session.CreatedAt || now < session.LastSeenAt)
            throw new StorageException(StorageCode.Unavailable);
        var expired = now >= session.IdleExpiresAt || now >= session.AbsoluteExpiresAt;
        await TerminateAsync(change, session, expired ? "expired" : "revoked", now);
        return !expired;
    }

    private static async Task TerminateAsync(IdentityMutation change, IdentitySession session, string state, DateTime now)
    {
        var rows = await change.Database.QueryAsync($"""
            UPDATE {change.Database.Prefix}__kofte_sessions SET state=@state,terminal_at_utc=@now
            WHERE reference_digest=@digest AND state='active' AND session_epoch=@epoch RETURNING reference_digest
            """, ("state", state), ("now", now), ("digest", session.Digest), ("epoch", change.Before.Epoch));
        if (rows.Count != 1) throw new StorageException(StorageCode.Conflict);
        await IdentityEventStore.AppendAsync(change, state == "expired"
            ? IdentityEventCode.SessionExpired : IdentityEventCode.SessionRevoked, now, session.UserId);
    }

    private static async Task<IdentitySession?> FindAsync(DeadlineStore database, string digest)
    {
        var rows = await database.QueryAsync(
            $"SELECT {Columns} FROM {database.Prefix}__kofte_sessions WHERE reference_digest=@digest", ("digest", digest));
        return rows.Count switch
        {
            0 => null,
            1 => Parse(rows[0]),
            _ => throw new StorageException(StorageCode.Corrupt)
        };
    }

    private static void Validate(string digest, DateTime now)
    {
        if (!IdentityStore.IsDigest(digest) || now.Kind != DateTimeKind.Utc)
            throw new StorageException(StorageCode.RequestInvalid);
    }

    private static IdentitySession Parse(object?[] row)
    {
        if (row.Length != 11 || row[0] is not string digest || !IdentityStore.IsDigest(digest) ||
            row[2] is not string mode || mode is not ("oidc" or "development") ||
            row[4] is not byte[] { Length: > 0 } ticket ||
            row[9] is not string state || state is not ("active" or "revoked" or "expired"))
            throw new StorageException(StorageCode.Corrupt);
        var created = Utc(row[5]);
        var seen = Utc(row[6]);
        var idle = Utc(row[7]);
        var absolute = Utc(row[8]);
        var terminal = row[10] is null ? (DateTime?)null : Utc(row[10]);
        if (created > DateTime.MaxValue.AddHours(-8) || absolute != created.AddHours(8) ||
            seen < created || idle <= seen || idle > absolute || idle - seen > TimeSpan.FromMinutes(30) ||
            (state == "active" ? terminal is not null : terminal is null || terminal < created))
            throw new StorageException(StorageCode.Corrupt);
        return new(digest, Id(row[1]), mode, Id(row[3]), ticket, created, seen, idle, absolute, state, terminal);
    }
}
