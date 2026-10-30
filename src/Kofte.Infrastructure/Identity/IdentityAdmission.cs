using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Identity;

internal enum IdentityAdmissionCategory
{
    LoginInstallation, LoginBrowser, CallbackInstallation, CallbackBrowser,
    EnrollmentInstallation, EnrollmentSession, IdentityInstallation, IdentityClient
}

internal sealed record IdentityAdmissionRequest(IdentityAdmissionCategory Category, string KeyDigest);

internal sealed class IdentityAdmissionException(DateTime? retryAt) : Exception("identity_admission_exceeded")
{
    internal DateTime? RetryAt { get; } = retryAt;
}

internal sealed class IdentityAdmission(IdentityStore store)
{
    internal Task AdmitAsync(DateTime now, IReadOnlyList<IdentityAdmissionRequest> requests, OperationBudget budget)
    {
        if (now.Kind != DateTimeKind.Utc || requests.Count is < 1 or > 8 ||
            requests.Any(request => !IdentityStore.IsDigest(request.KeyDigest)) ||
            requests.Distinct().Count() != requests.Count)
            throw new StorageException(StorageCode.RequestInvalid);
        foreach (var request in requests) _ = Rule(request.Category);
        return store.MutateAsync(budget, async change =>
        {
            var database = change.Database;
            var table = database.Prefix + "__kofte_identity_admission";
            await PruneAsync(database, now);
            var start = new DateTime(now.Ticks - now.Ticks % TimeSpan.TicksPerMinute, DateTimeKind.Utc);
            var expiry = start.AddMinutes(1);
            foreach (var request in requests)
            {
                var (category, limit) = Rule(request.Category);
                var parameters = new (string Name, object? Value)[]
                {
                    ("category", category), ("start", start), ("key", request.KeyDigest)
                };
                var rows = await database.QueryAsync($"""
                    SELECT count FROM {table} WHERE category=@category AND window_start_utc=@start AND key_digest=@key
                    """, parameters);
                if (rows.Count == 0)
                {
                    if (DeadlineStore.AssertScalar(await database.QueryAsync(
                            $"SELECT COUNT(*) FROM {table} WHERE expires_at_utc>@now", ("now", now))) is not long live)
                        throw new StorageException(StorageCode.Corrupt);
                    if (live >= 4096) throw new IdentityAdmissionException(null);
                    await database.QueryAsync($"""
                        INSERT INTO {table} (category,window_start_utc,key_digest,expires_at_utc,count)
                        VALUES (@category,@start,@key,@expiry,1)
                        """, [.. parameters, ("expiry", expiry)]);
                }
                else
                {
                    if (DeadlineStore.AssertScalar(rows) is not long count || count < 0)
                        throw new StorageException(StorageCode.Corrupt);
                    if (count >= limit) throw new IdentityAdmissionException(expiry);
                    await database.QueryAsync($"""
                        UPDATE {table} SET count=@count
                        WHERE category=@category AND window_start_utc=@start AND key_digest=@key
                        """, [.. parameters, ("count", checked(count + 1))]);
                }
            }
            return true;
        });
    }

    internal static async Task<int> PruneAsync(DeadlineStore database, DateTime now)
    {
        if (now.Kind != DateTimeKind.Utc) throw new StorageException(StorageCode.RequestInvalid);
        var table = database.Prefix + "__kofte_identity_admission";
        var rows = await database.QueryAsync($"""
            DELETE FROM {table} WHERE (category,window_start_utc,key_digest) IN
            (SELECT category,window_start_utc,key_digest FROM {table} WHERE expires_at_utc<=@now
             ORDER BY expires_at_utc,category,window_start_utc,key_digest LIMIT 128) RETURNING key_digest
            """, ("now", now));
        return rows.Count;
    }

    private static (string Category, long Limit) Rule(IdentityAdmissionCategory category) => category switch
    {
        IdentityAdmissionCategory.LoginInstallation => ("login_installation", 60),
        IdentityAdmissionCategory.LoginBrowser => ("login_browser", 10),
        IdentityAdmissionCategory.CallbackInstallation => ("callback_installation", 120),
        IdentityAdmissionCategory.CallbackBrowser => ("callback_browser", 20),
        IdentityAdmissionCategory.EnrollmentInstallation => ("enrollment_installation", 200),
        IdentityAdmissionCategory.EnrollmentSession => ("enrollment_session", 40),
        IdentityAdmissionCategory.IdentityInstallation => ("identity_installation", 1200),
        IdentityAdmissionCategory.IdentityClient => ("identity_client", 120),
        _ => throw new StorageException(StorageCode.RequestInvalid)
    };
}
