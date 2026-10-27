using System.Data;
using System.Data.Common;
using System.Globalization;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Postgres;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Kofte.Infrastructure.Identity;

internal sealed record IdentityMetadata(Guid InstallationId, long Revision, Guid Epoch,
    string Mode, string TrustDigest, DateTime ReconciledAt);

internal sealed record IdentityMutation(DeadlineStore Database, IdentityMetadata Before, long Revision);

internal sealed class IdentityStore
{
    private readonly StorageInstallation installation;
    private readonly Guid installationId;
    private readonly string mode;
    private readonly string digest;
    private readonly StoreLease? fileLease;
    private readonly NpgsqlConnection? hostLease;
    private const string Columns = """
        installation_id,identity_schema_version,security_revision,session_epoch,mode,trust_digest,reconciled_at_utc
        """;

    internal IdentityStore(StorageInstallation installation, Guid installationId, string mode, string digest,
        StoreLease? fileLease, NpgsqlConnection? hostLease)
    {
        ValidateTrust(mode, digest);
        if (installationId == Guid.Empty || (installation.Provider == "sqlite"
                ? fileLease is null || hostLease is not null
                : hostLease is null || fileLease is not null))
            throw new StorageException(StorageCode.RequestInvalid);
        (this.installation, this.installationId, this.mode, this.digest, this.fileLease, this.hostLease) =
            (installation, installationId, mode, digest, fileLease, hostLease);
    }

    internal async Task<T> MutateAsync<T>(OperationBudget budget, Func<IdentityMutation, Task<T>> action)
    {
        budget.Check();
        var remaining = Math.Min(TimeSpan.FromSeconds(1).Ticks, budget.Remaining.Ticks);
        if (remaining <= 0) throw new OperationCanceledException(budget.Token);
        using var local = new OperationBudget(TimeSpan.FromTicks(remaining), budget.Token);
        VerifyLease();
        await using var database = await DeadlineStore.OpenAsync(installation, local);
        await VerifyAuthorityAsync(database);
        await database.BeginWriteAsync();
        var metadata = await ReadMetadataAsync(database, locked: true);
        Match(metadata, installationId, mode, digest);
        var revision = checked(metadata.Revision + 1);
        var result = await action(new(database, metadata, revision));
        if (DeadlineStore.AssertScalar(await database.QueryAsync($"""
            UPDATE {database.Prefix}__kofte_identity_metadata SET security_revision=@next
            WHERE singleton_key=1 AND security_revision=@previous RETURNING security_revision
            """, ("next", revision), ("previous", metadata.Revision))) is not long actual || actual != revision)
            throw new StorageException(StorageCode.Conflict);
        await VerifyAuthorityAsync(database);
        await database.CommitAsync();
        return result;
    }

    internal static async Task InitializeAsync(StorageInstallation installation, MaintenanceScope scope,
        DbConnection connection, DbTransaction transaction, string mode, string digest, DateTime now,
        OperationBudget budget)
    {
        ValidateTrust(mode, digest);
        if (transaction.Connection != connection || now.Kind != DateTimeKind.Utc)
            throw new StorageException(StorageCode.RequestInvalid);
        await scope.VerifyAsync(installation, budget);
        if (connection is SqliteConnection sqlite)
        {
            if (installation.Provider != "sqlite" || sqlite.DataSource != installation.PlatformPath)
                throw new StorageException(StorageCode.RequestInvalid);
        }
        else if (connection is NpgsqlConnection postgres && installation.Postgres is { } identity)
            await identity.VerifyAsync(postgres, budget);
        else throw new StorageException(StorageCode.RequestInvalid);
        var registry = new RegistryMigrationRunner(installation);
        var owner = await registry.ReadInstallationAsync(connection, budget, transaction);
        if (owner.Version != 2) throw new StorageException(StorageCode.VersionUnsupported);
        var table = registry.Prefix + "__kofte_identity_metadata";
        await using var count = RegistryMigrationRunner.Command(connection, $"SELECT COUNT(*) FROM {table}",
            budget, transaction);
        if (await count.ExecuteScalarAsync(budget.Token) is 0L)
            await RegistryMigrationRunner.ExecuteAsync(connection, $"""
                INSERT INTO {table}
                (singleton_key,installation_id,identity_schema_version,security_revision,session_epoch,mode,
                 trust_digest,reconciled_at_utc)
                VALUES (1,@installation,1,0,@epoch,@mode,@digest,@now)
                """, budget, transaction, ("installation", owner.Id), ("epoch", Guid.NewGuid()),
                ("mode", mode), ("digest", digest),
                ("now", installation.Provider == "sqlite" ? now.ToString("O", CultureInfo.InvariantCulture) : now));
        await using var command = RegistryMigrationRunner.Command(connection, $"SELECT {Columns} FROM {table}",
            budget, transaction);
        await using var reader = await command.ExecuteReaderAsync(budget.Token);
        if (!await reader.ReadAsync(budget.Token)) throw new StorageException(StorageCode.Corrupt);
        var values = new object[7];
        reader.GetValues(values);
        Match(Parse(values), owner.Id, mode, digest);
        if (await reader.ReadAsync(budget.Token)) throw new StorageException(StorageCode.Corrupt);
        await scope.VerifyAsync(installation, budget);
    }

    internal static async Task<IdentityMetadata> ReadMetadataAsync(DeadlineStore database, bool locked = false)
    {
        var rows = await database.QueryAsync($"SELECT {Columns} FROM {database.Prefix}__kofte_identity_metadata" +
            (locked && database.Prefix.Length != 0 ? " FOR UPDATE" : ""));
        if (rows.Count != 1) throw new StorageException(StorageCode.Corrupt);
        return Parse(rows[0]);
    }

    private void VerifyLease()
    {
        if (installation.Provider == "sqlite")
        {
            if (fileLease?.IsHeldFor(installation.Root!) != true) throw new StorageException(StorageCode.Unavailable);
            return;
        }
        if (hostLease?.State != ConnectionState.Open) throw new StorageException(StorageCode.Unavailable);
        var expected = new NpgsqlConnectionStringBuilder(installation.Postgres!.ConnectionString);
        var actual = new NpgsqlConnectionStringBuilder(hostLease.ConnectionString);
        if (expected.Host != actual.Host || expected.Port != actual.Port || expected.Database != actual.Database ||
            expected.Username != actual.Username || expected.ApplicationName != actual.ApplicationName)
            throw new StorageException(StorageCode.OwnerMismatch);
    }

    private async Task VerifyAuthorityAsync(DeadlineStore database)
    {
        database.Budget.Check();
        VerifyLease();
        if (hostLease is null) return;
        var pid = hostLease.ProcessID;
        if (DeadlineStore.AssertScalar(await database.QueryAsync("""
            SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_locks WHERE locktype='advisory'
              AND pid=@pid AND granted AND mode='ShareLock'
              AND database=(SELECT oid FROM pg_catalog.pg_database WHERE datname=current_database())
              AND classid=((@key::bigint >> 32) & 4294967295)::oid
              AND objid=(@key::bigint & 4294967295)::oid AND objsubid=1)
            """, ("pid", pid), ("key", PostgresIdentity.LeaseKey))) is not true)
            throw new StorageException(StorageCode.Unavailable);
        VerifyLease();
        if (hostLease.ProcessID != pid) throw new StorageException(StorageCode.Unavailable);
    }

    private static IdentityMetadata Parse(object?[] row)
    {
        if (row.Length != 7 || Convert.ToInt64(row[1], CultureInfo.InvariantCulture) != 1 ||
            row[2] is not long revision || revision < 0 ||
            !Guid.TryParse(Convert.ToString(row[0], CultureInfo.InvariantCulture), out var id) || id == Guid.Empty ||
            !Guid.TryParse(Convert.ToString(row[3], CultureInfo.InvariantCulture), out var epoch) || epoch == Guid.Empty ||
            row[4] is not string mode || row[5] is not string digest)
            throw new StorageException(StorageCode.Corrupt);
        ValidateTrust(mode, digest);
        var time = row[6] is DateTime { Kind: DateTimeKind.Utc } utc ? utc :
            row[6] is string text && DateTime.TryParseExact(text, "O", CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var parsed) && parsed.Kind == DateTimeKind.Utc ? parsed :
                throw new StorageException(StorageCode.Corrupt);
        return new(id, revision, epoch, mode, digest, time);
    }

    private static void Match(IdentityMetadata metadata, Guid id, string mode, string digest)
    {
        if (metadata.InstallationId != id || metadata.Mode != mode || metadata.TrustDigest != digest)
            throw new StorageException(StorageCode.ReconciliationRequired);
    }

    internal static bool IsDigest(string value) =>
        value.Length == 64 && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static void ValidateTrust(string mode, string digest)
    {
        if (mode is not ("oidc" or "development") || !IsDigest(digest))
            throw new StorageException(StorageCode.RequestInvalid);
    }
}
