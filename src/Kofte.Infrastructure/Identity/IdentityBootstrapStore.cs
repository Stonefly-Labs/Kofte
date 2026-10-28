using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;
using static Kofte.Infrastructure.Identity.IdentityValues;

namespace Kofte.Infrastructure.Identity;

internal sealed record IdentityBootstrap(Guid InstallationId, string State, long Generation,
    string? Issuer, string? Subject, string? Digest, DateTime? Expires, Guid? InitialUser,
    DateTime? ConsumedAt, long? ConsumedGeneration)
{
    public override string ToString() => nameof(IdentityBootstrap);
}

internal static class IdentityBootstrapStore
{
    internal static async Task<IdentityBootstrap> ReadAsync(DeadlineStore database, Guid installationId)
    {
        var rows = await database.QueryAsync($"""
            SELECT installation_id,state,generation,nominee_issuer,nominee_subject,authorization_digest,
              expires_at_utc,initial_user_id,consumed_at_utc,consumed_generation
            FROM {database.Prefix}__kofte_bootstrap WHERE singleton_key=1
            """);
        if (rows.Count != 1) throw new StorageException(StorageCode.Corrupt);
        var row = rows[0];
        var result = new IdentityBootstrap(Id(row[0]), (string)row[1]!, (long)row[2]!,
            (string?)row[3], (string?)row[4], (string?)row[5], row[6] is null ? null : Utc(row[6]),
            row[7] is null ? null : Id(row[7]), row[8] is null ? null : Utc(row[8]), (long?)row[9]);
        var nomination = result.Issuer is not null && result.Subject is not null &&
            IdentityUserStore.ValidKey("oidc", result.Issuer, result.Subject) &&
            result.Digest is not null && IdentityStore.IsDigest(result.Digest) && result.Expires is not null;
        var noNomination = result.Issuer is null && result.Subject is null && result.Digest is null &&
            result.Expires is null;
        var consumed = result.InitialUser is not null && result.ConsumedAt is not null &&
            result.ConsumedGeneration is > 0 && result.ConsumedGeneration <= result.Generation;
        var noConsumption = result.InitialUser is null && result.ConsumedAt is null &&
            result.ConsumedGeneration is null;
        if (result.InstallationId != installationId || result.Generation < 0 || !(result.State switch
        {
            "disabled" => noNomination && noConsumption,
            "pending" => result.Generation > 0 && nomination && noConsumption,
            "consumed" => noNomination && consumed,
            "recovery_locked" => noNomination && (consumed || noConsumption),
            _ => false
        })) throw new StorageException(StorageCode.Corrupt);
        return result;
    }

    internal static async Task AuthorizeAsync(StorageInstallation installation, MaintenanceScope scope,
        Guid installationId, string mode, string trustDigest, string issuer, string subject, string authorization,
        Func<Task> deliver, OperationBudget budget)
    {
        if (mode != "oidc") throw new IdentityStateException(IdentityDiagnosticOutcome.Forbidden);
        if (!IdentityUserStore.ValidKey(mode, issuer, subject) || !ValidAuthorization(authorization))
            throw new StorageException(StorageCode.RequestInvalid);
        await IdentityStore.MutateStoppedAsync(installation, scope, installationId, mode, trustDigest, false,
            budget, async change =>
            {
                var before = await ReadAsync(change.Database, change.Before.InstallationId);
                if (before.State is not ("disabled" or "pending"))
                    throw new IdentityStateException(IdentityDiagnosticOutcome.EnrollmentUnavailable);
                var generation = checked(before.Generation + 1);
                var now = DateTime.UtcNow;
                var expires = now.AddMinutes(15);
                var digest = AuthorizationDigest(installationId, generation, issuer, subject, authorization);
                await deliver();
                budget.Check();
                var database = change.Database;
                if (DeadlineStore.AssertScalar(await database.QueryAsync($"""
                    UPDATE {database.Prefix}__kofte_bootstrap
                    SET state='pending',generation=@next,nominee_issuer=@issuer,nominee_subject=@subject,
                      authorization_digest=@digest,expires_at_utc=@expires
                    WHERE singleton_key=1 AND generation=@previous AND state=@state RETURNING singleton_key
                    """, ("next", generation), ("issuer", issuer), ("subject", subject), ("digest", digest),
                    ("expires", expires), ("previous", before.Generation), ("state", before.State))) is not (1 or 1L))
                    throw new StorageException(StorageCode.Conflict);
                return await IdentityEventStore.AppendAsync(change, IdentityEventCode.BootstrapAuthorized, now);
            });
    }

    internal static async Task DisablePendingAsync(StorageInstallation installation, MaintenanceScope scope,
        Guid installationId, string mode, string trustDigest, OperationBudget budget)
    {
        if (mode != "oidc") throw new IdentityStateException(IdentityDiagnosticOutcome.Forbidden);
        await IdentityStore.MutateStoppedAsync(installation, scope, installationId, mode, trustDigest, false,
            budget, async change =>
            {
                var before = await ReadAsync(change.Database, change.Before.InstallationId);
                if (before.State is not ("disabled" or "pending"))
                    throw new IdentityStateException(IdentityDiagnosticOutcome.EnrollmentUnavailable);
                var database = change.Database;
                if (DeadlineStore.AssertScalar(await database.QueryAsync($"""
                    UPDATE {database.Prefix}__kofte_bootstrap SET state='disabled',nominee_issuer=NULL,
                      nominee_subject=NULL,authorization_digest=NULL,expires_at_utc=NULL
                    WHERE singleton_key=1 AND generation=@generation AND state=@state RETURNING singleton_key
                    """, ("generation", before.Generation), ("state", before.State))) is not (1 or 1L))
                    throw new StorageException(StorageCode.Conflict);
                return await IdentityEventStore.AppendAsync(change, IdentityEventCode.BootstrapDisabled, DateTime.UtcNow);
            });
    }

    internal static async Task<IdentityEventReceipt> EnrollAsync(IdentityMutation change,
        IdentitySessionView current, string authorization, DateTime now)
    {
        if (now.Kind != DateTimeKind.Utc || !ValidAuthorization(authorization))
            throw new StorageException(StorageCode.RequestInvalid);
        if (change.Before.Mode != "oidc" || current.User.Mode != "oidc" || current.Session.Mode != "oidc")
            throw new IdentityStateException(IdentityDiagnosticOutcome.Forbidden);
        var verified = await IdentitySessionStore.RetrieveAsync(change, current.Session.Digest, now);
        if (verified is null || verified.User.UserId != current.User.UserId ||
            verified.Session.Epoch != current.Session.Epoch)
            throw new IdentityStateException(IdentityDiagnosticOutcome.Unauthenticated);
        var database = change.Database;
        var before = await ReadAsync(database, change.Before.InstallationId);
        if (before.State != "pending" || before.Expires <= now)
            throw new IdentityStateException(IdentityDiagnosticOutcome.EnrollmentUnavailable);
        var mapping = await database.QueryAsync($"""
            SELECT issuer,subject FROM {database.Prefix}__kofte_external_identities
            WHERE user_id=@user AND mode='oidc'
            """, ("user", verified.User.UserId));
        if (mapping.Count != 1) throw new StorageException(StorageCode.Corrupt);
        if ((string?)mapping[0][0] != before.Issuer || (string?)mapping[0][1] != before.Subject)
            throw new IdentityStateException(IdentityDiagnosticOutcome.Forbidden);
        var digest = AuthorizationDigest(before.InstallationId, before.Generation, before.Issuer!,
            before.Subject!, authorization);
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(before.Digest!),
                Convert.FromHexString(digest)))
            throw new IdentityStateException(IdentityDiagnosticOutcome.EnrollmentUnavailable);
        var rows = await database.QueryAsync($"""
            UPDATE {database.Prefix}__kofte_bootstrap SET state='consumed',initial_user_id=@user,
              consumed_at_utc=@now,consumed_generation=generation,nominee_issuer=NULL,nominee_subject=NULL,
              authorization_digest=NULL,expires_at_utc=NULL
            WHERE singleton_key=1 AND state='pending' AND generation=@generation
              AND authorization_digest=@digest AND expires_at_utc>@now RETURNING initial_user_id
            """, ("user", verified.User.UserId), ("now", now), ("generation", before.Generation),
            ("digest", before.Digest));
        if (rows.Count != 1 || Id(rows[0][0]) != verified.User.UserId)
            throw new IdentityStateException(IdentityDiagnosticOutcome.EnrollmentUnavailable);
        return await IdentityEventStore.AppendAsync(change, IdentityEventCode.InitialAdministratorEnrolled,
            now, verified.User.UserId);
    }

    internal static bool ValidAuthorization(string value) =>
        value.Length == 43 && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_') &&
        Convert.ToBase64String(Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + "="))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_') == value;

    internal static string AuthorizationDigest(Guid installation, long generation, string issuer, string subject,
        string authorization) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"kofte.identity.bootstrap.authorization.v1\0{installation:D}\0" +
            $"{generation.ToString(CultureInfo.InvariantCulture)}\0{issuer}\0{subject}\0{authorization}")));
}
