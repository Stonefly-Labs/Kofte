using System.Buffers;
using System.Globalization;
using System.Text;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Identity;

internal sealed record IdentityUser(Guid UserId, string Mode, string? DisplayName, DateTime CreatedAt, DateTime UpdatedAt);

internal static class IdentityUserStore
{
    private const string DevelopmentIssuer = "kofte:development:v1";
    private const string Columns = "user_id,mode,display_name,created_at_utc,updated_at_utc";
    private static readonly UTF8Encoding Utf8 = new(false, true);

    // The caller verifies the protocol evidence; this operation only owns its exact local mapping.
    internal static async Task<IdentityUser> ResolveAsync(IdentityMutation change, string issuer, string subject,
        string? displayName, DateTime now)
    {
        var mode = change.Before.Mode;
        if (now.Kind != DateTimeKind.Utc || !ValidKey(mode, issuer, subject) || !ValidDisplay(displayName))
            throw new StorageException(StorageCode.RequestInvalid);
        var database = change.Database;
        var existing = await FindAsync(database, mode, issuer, subject);
        if (existing is not null) return await UpdateDisplayAsync(database, existing, displayName, now);
        if (DeadlineStore.AssertScalar(await database.QueryAsync(
                $"SELECT COUNT(*) FROM {database.Prefix}__kofte_users")) is not long count || count < 0)
            throw new StorageException(StorageCode.Corrupt);
        if (count >= 10_000) throw new IdentityAdmissionException(null);
        var id = Guid.NewGuid();
        await database.QueryAsync($"""
            INSERT INTO {database.Prefix}__kofte_users
            (user_id,mode,display_name,created_at_utc,updated_at_utc)
            VALUES (@id,@mode,@display,@now,@now)
            """, ("id", id), ("mode", mode), ("display", displayName), ("now", now));
        var inserted = await database.QueryAsync($"""
            INSERT INTO {database.Prefix}__kofte_external_identities
            (mode,issuer,subject,user_id,created_at_utc) VALUES (@mode,@issuer,@subject,@id,@now)
            ON CONFLICT (mode,issuer,subject) DO NOTHING RETURNING user_id
            """, ("mode", mode), ("issuer", issuer), ("subject", subject), ("id", id), ("now", now));
        if (inserted.Count == 0)
        {
            // Only the named exact-key conflict is reconciled, never an arbitrary storage error.
            await database.QueryAsync($"DELETE FROM {database.Prefix}__kofte_users WHERE user_id=@id", ("id", id));
            existing = await FindAsync(database, mode, issuer, subject) ?? throw new StorageException(StorageCode.Conflict);
            return await UpdateDisplayAsync(database, existing, displayName, now);
        }
        if (inserted.Count != 1 || Id(inserted[0][0]) != id) throw new StorageException(StorageCode.Corrupt);
        await IdentityEventStore.AppendAsync(change, IdentityEventCode.UserCreated, now, id);
        return await FindAsync(database, mode, issuer, subject) ?? throw new StorageException(StorageCode.Corrupt);
    }

    internal static async Task VerifyCorrespondenceAsync(DeadlineStore database)
    {
        if (DeadlineStore.AssertScalar(await database.QueryAsync($"""
                SELECT COUNT(*) FROM {database.Prefix}__kofte_users u
                LEFT JOIN {database.Prefix}__kofte_external_identities e ON e.user_id=u.user_id AND e.mode=u.mode
                WHERE e.user_id IS NULL
                """)) is not 0L ||
            DeadlineStore.AssertScalar(await database.QueryAsync($"""
                SELECT COUNT(*) FROM {database.Prefix}__kofte_external_identities e
                LEFT JOIN {database.Prefix}__kofte_users u ON u.user_id=e.user_id AND u.mode=e.mode
                WHERE u.user_id IS NULL
                """)) is not 0L)
            throw new StorageException(StorageCode.Corrupt);
    }

    private static async Task<IdentityUser?> FindAsync(DeadlineStore database, string mode, string issuer, string subject)
    {
        var rows = await database.QueryAsync($"""
            SELECT u.user_id,u.mode,u.display_name,u.created_at_utc,u.updated_at_utc
            FROM {database.Prefix}__kofte_external_identities e
            JOIN {database.Prefix}__kofte_users u ON u.user_id=e.user_id AND u.mode=e.mode
            WHERE e.mode=@mode AND e.issuer=@issuer AND e.subject=@subject
            """, ("mode", mode), ("issuer", issuer), ("subject", subject));
        return rows.Count switch
        {
            0 => null,
            1 => Parse(rows[0]),
            _ => throw new StorageException(StorageCode.Corrupt)
        };
    }

    private static async Task<IdentityUser> UpdateDisplayAsync(DeadlineStore database, IdentityUser user,
        string? displayName, DateTime now)
    {
        if (now < user.CreatedAt || now < user.UpdatedAt) throw new StorageException(StorageCode.Unavailable);
        if (displayName == user.DisplayName) return user;
        var rows = await database.QueryAsync($"""
            UPDATE {database.Prefix}__kofte_users SET display_name=@display,updated_at_utc=@now
            WHERE user_id=@id AND mode=@mode RETURNING {Columns}
            """, ("display", displayName), ("now", now), ("id", user.UserId), ("mode", user.Mode));
        return rows.Count == 1 ? Parse(rows[0]) : throw new StorageException(StorageCode.Corrupt);
    }

    private static IdentityUser Parse(object?[] row)
    {
        if (row.Length != 5 || row[1] is not string mode || mode is not ("oidc" or "development") ||
            row[2] is not null and not string || !ValidDisplay((string?)row[2]))
            throw new StorageException(StorageCode.Corrupt);
        var created = Utc(row[3]);
        var updated = Utc(row[4]);
        if (updated < created) throw new StorageException(StorageCode.Corrupt);
        return new(Id(row[0]), mode, (string?)row[2], created, updated);
    }

    private static Guid Id(object? value) =>
        Guid.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var id) && id != Guid.Empty
            ? id : throw new StorageException(StorageCode.Corrupt);

    private static DateTime Utc(object? value) => value switch
    {
        DateTime { Kind: DateTimeKind.Utc } utc => utc,
        string text when DateTime.TryParseExact(text, "O", CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out var parsed) && parsed.Kind == DateTimeKind.Utc => parsed,
        _ => throw new StorageException(StorageCode.Corrupt)
    };

    private static bool ValidKey(string mode, string issuer, string subject)
    {
        if (mode is not ("oidc" or "development") || subject.Length is < 1 or > 255 ||
            subject.Any(character => character is < ' ' or > '~') ||
            (mode == "development" ? issuer != DevelopmentIssuer : issuer == DevelopmentIssuer) ||
            issuer.Length == 0 || issuer.Any(char.IsControl)) return false;
        try { return Utf8.GetByteCount(issuer) <= 2048; }
        catch (EncoderFallbackException) { return false; }
    }

    private static bool ValidDisplay(string? value)
    {
        if (value is null) return true;
        var count = 0;
        var remaining = value.AsSpan();
        while (!remaining.IsEmpty)
        {
            if (Rune.DecodeFromUtf16(remaining, out var rune, out var consumed) != OperationStatus.Done ||
                Rune.IsControl(rune) || ++count > 128) return false;
            remaining = remaining[consumed..];
        }
        return true;
    }
}
