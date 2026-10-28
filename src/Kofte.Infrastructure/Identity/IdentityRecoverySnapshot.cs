using System.Buffers.Binary;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Identity;

public sealed record IdentityRecoveryCounts(long Users, long ExternalIdentities, long LoginAttempts,
    long Sessions, long Events, long AdmissionWindows);

public sealed record IdentityRecoverySummary(int SchemaVersion, long SecurityRevision, Guid SessionEpoch,
    string BootstrapState, long? ConsumedGeneration, IdentityRecoveryCounts Counts, string Sha256);

public static class IdentityRecoverySnapshot
{
    private sealed record Table(string Name, string Columns, string Order);

    private static readonly Table[] Tables =
    [
        new("identity_metadata",
            "singleton_key,installation_id,identity_schema_version,security_revision,session_epoch,mode,trust_digest,reconciled_at_utc",
            "singleton_key"),
        new("users", "user_id,mode,display_name,created_at_utc,updated_at_utc", "user_id"),
        new("external_identities", "mode,issuer,subject,user_id,created_at_utc", "mode,issuer,subject"),
        new("login_attempts",
            "attempt_id,browser_digest,mode,trust_digest,session_epoch,return_path,created_at_utc,expires_at_utc,state,consumed_at_utc",
            "attempt_id"),
        new("sessions",
            "reference_digest,user_id,mode,session_epoch,protected_ticket,created_at_utc,last_seen_at_utc,idle_expires_at_utc,absolute_expires_at_utc,state,terminal_at_utc",
            "reference_digest"),
        new("bootstrap",
            "singleton_key,installation_id,state,generation,nominee_issuer,nominee_subject,authorization_digest,expires_at_utc,initial_user_id,initial_user_mode,consumed_at_utc,consumed_generation",
            "singleton_key"),
        new("identity_events",
            "event_id,security_revision,event_code,occurred_at_utc,correlation_id,user_id,outcome,retention_class",
            "event_id"),
        new("identity_admission", "category,window_start_utc,key_digest,expires_at_utc,count",
            "category,window_start_utc,key_digest")
    ];

    public static Task<IdentityRecoverySummary> CaptureStoppedAsync(StorageInstallation installation,
        IStorageMaintenanceScope scope, CancellationToken cancellationToken = default) =>
        StorageDiagnostics.ObserveRecoveryAsync(installation.Provider, cancellationToken, async (budget, _, _) =>
        {
            if (scope is not MaintenanceScope owned) throw new StorageException(StorageCode.RequestInvalid);
            await owned.VerifyAsync(installation, budget);
            await using var platform = installation.Provider == "sqlite"
                ? RecoverySetVerifier.OpenReadOnly(installation.PlatformPath)
                : await installation.OpenPlatformAsync(budget);
            var result = await ReadAsync(installation, platform, budget);
            await owned.VerifyAsync(installation, budget);
            return result;
        });

    internal static async Task<IdentityRecoverySummary> ReadAsync(StorageInstallation installation,
        DbConnection platform, OperationBudget budget)
    {
        var owner = await new RegistryMigrationRunner(installation).ReadInstallationAsync(platform, budget);
        if (owner.Version != 2) throw new StorageException(StorageCode.VersionUnsupported);
        // Stopped ownership supplies consistency without an uncancellable read-transaction rollback.
        await using var database = DeadlineStore.Borrow(platform, null, budget);
        foreach (var table in Tables)
        {
            // Admitted text is at most 2048 characters; do not materialize corrupt oversized cells.
            var oversized = string.Join(" OR ", table.Columns.Split(',')
                .Where(column => column != "protected_ticket")
                .Select(column => $"length(CAST({column} AS TEXT))>2048"));
            if (DeadlineStore.AssertScalar(await database.QueryAsync(
                $"SELECT EXISTS(SELECT 1 FROM {database.Prefix}__kofte_{table.Name} WHERE {oversized})")) is true or 1L)
                throw new StorageException(StorageCode.Corrupt);
        }
        var metadata = await IdentityStore.ReadMetadataAsync(database);
        if (metadata.InstallationId != owner.Id) throw new StorageException(StorageCode.OwnerMismatch);
        var bootstrap = await IdentityBootstrapStore.ReadAsync(database, owner.Id);
        await IdentityUserStore.VerifyCorrespondenceAsync(database);
        var enrollment = await database.QueryAsync($"""
            SELECT user_id FROM {database.Prefix}__kofte_identity_events
            WHERE event_code='initial_administrator_enrolled'
            """);
        if (bootstrap.InitialUser is { } initial
            ? enrollment.Count != 1 || IdentityValues.Id(enrollment[0][0]) != initial
            : enrollment.Count != 0)
            throw new StorageException(StorageCode.Corrupt);
        if (DeadlineStore.AssertScalar(await database.QueryAsync($"""
            SELECT COUNT(*) FROM {database.Prefix}__kofte_identity_events WHERE security_revision>@revision
            """, ("revision", metadata.Revision))) is not 0L)
            throw new StorageException(StorageCode.Corrupt);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Text(hash, "kofte-identity-security-snapshot-v1");
        var counts = new long[Tables.Length];
        var buffer = new byte[8192];
        for (var tableIndex = 0; tableIndex < Tables.Length; tableIndex++)
        {
            var table = Tables[tableIndex];
            var name = database.Prefix + "__kofte_" + table.Name;
            Text(hash, table.Name);
            Text(hash, table.Columns);
            await using var command = RegistryMigrationRunner.Command(platform,
                $"SELECT {table.Columns} FROM {name} ORDER BY {table.Order}", budget);
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, budget.Token);
            while (await reader.ReadAsync(budget.Token))
            {
                budget.Check();
                hash.AppendData([1]);
                for (var column = 0; column < reader.FieldCount; column++)
                {
                    if (await reader.IsDBNullAsync(column, budget.Token)) hash.AppendData([0]);
                    else if (reader.GetFieldType(column) == typeof(byte[]))
                    {
                        hash.AppendData([2]);
                        var length = reader.GetBytes(column, 0, null, 0, 0);
                        Number(hash, length);
                        long offset = 0;
                        while (offset < length)
                        {
                            budget.Check();
                            var read = reader.GetBytes(column, offset, buffer, 0,
                                (int)Math.Min(buffer.Length, length - offset));
                            if (read <= 0) throw new StorageException(StorageCode.Corrupt);
                            hash.AppendData(buffer.AsSpan(0, checked((int)read)));
                            offset += read;
                        }
                    }
                    else
                    {
                        hash.AppendData([3]);
                        Text(hash, reader.GetValue(column) switch
                        {
                            DateTime value => value.ToString("O", CultureInfo.InvariantCulture),
                            Guid value => value.ToString("D"),
                            string value => value,
                            long value => value.ToString(CultureInfo.InvariantCulture),
                            int value => value.ToString(CultureInfo.InvariantCulture),
                            _ => throw new StorageException(StorageCode.Corrupt)
                        });
                    }
                }
                counts[tableIndex] = checked(counts[tableIndex] + 1);
            }
            hash.AppendData([0]);
            Number(hash, counts[tableIndex]);
        }
        if (counts[0] != 1 || counts[5] != 1) throw new StorageException(StorageCode.Corrupt);
        return new(1, metadata.Revision, metadata.Epoch, bootstrap.State, bootstrap.ConsumedGeneration,
            new(counts[1], counts[2], counts[3], counts[4], counts[6], counts[7]),
            Convert.ToHexStringLower(hash.GetHashAndReset()));
    }

    internal static void Validate(IdentityRecoverySummary summary)
    {
        if (summary.SchemaVersion != 1 || summary.SecurityRevision < 0 || summary.SessionEpoch == Guid.Empty ||
            summary.BootstrapState is not ("disabled" or "pending" or "consumed" or "recovery_locked") ||
            summary.Counts is not
            {
                Users: >= 0, ExternalIdentities: >= 0, LoginAttempts: >= 0,
                Sessions: >= 0, Events: >= 0, AdmissionWindows: >= 0
            } ||
            summary.ConsumedGeneration is <= 0 ||
            summary.BootstrapState == "consumed" && summary.ConsumedGeneration is null ||
            summary.BootstrapState is "disabled" or "pending" && summary.ConsumedGeneration is not null ||
            summary.Sha256 is null || !IdentityStore.IsDigest(summary.Sha256))
            throw new StorageException(StorageCode.Corrupt);
    }

    private static void Text(IncrementalHash hash, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        Number(hash, bytes.Length);
        hash.AppendData(bytes);
    }

    private static void Number(IncrementalHash hash, long number)
    {
        Span<byte> bytes = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(bytes, number);
        hash.AppendData(bytes);
    }
}
