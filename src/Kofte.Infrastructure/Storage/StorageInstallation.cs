using Kofte.Infrastructure.Storage.Postgres;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;
using Npgsql;
using static SQLitePCL.raw;

namespace Kofte.Infrastructure.Storage;

public sealed class StorageInstallation
{
    public string Provider { get; }
    internal string? Root { get; }
    internal PostgresIdentity? Postgres { get; }
    internal string? ProvisioningConnection { get; }
    internal string PlatformPath => Path.Combine(Root ??
        throw new InvalidOperationException("storage_provider_invalid"), "platform", "platform.db");

    private StorageInstallation(string provider, string? root, PostgresIdentity? postgres, string? provisioning)
        => (Provider, Root, Postgres, ProvisioningConnection) = (provider, root, postgres, provisioning);

    public static StorageInstallation Sqlite(string root)
    {
        UnixStorePaths.ValidateRoot(root, allowMissing: false);
        return new("sqlite", root, null, null);
    }

    public static StorageInstallation PostgreSql(string runtimeConnection, string? provisioningConnection = null)
    {
        var identity = new PostgresIdentity(runtimeConnection);
        if (provisioningConnection is not null)
        {
            var runtime = new NpgsqlConnectionStringBuilder(identity.ConnectionString);
            var provisioner = new NpgsqlConnectionStringBuilder(provisioningConnection);
            if (runtime.Host != provisioner.Host || runtime.Port != provisioner.Port ||
                runtime.Database != provisioner.Database || runtime.Username == provisioner.Username ||
                string.IsNullOrEmpty(provisioner.Username) ||
                provisioner.ApplicationName != "kofte_provisioning_" + runtime.ApplicationName!["kofte_bootstrap_".Length..])
                throw new ArgumentException("storage_provisioner_configuration_invalid");
            provisioner.Pooling = false;
            provisioner.Timeout = 3;
            provisioner.CommandTimeout = 3;
            provisioner.CancellationTimeout = -1;
            provisioner.IncludeErrorDetail = false;
            provisioningConnection = provisioner.ConnectionString;
        }
        return new("postgresql", null, identity, provisioningConnection);
    }

    internal async Task<System.Data.Common.DbConnection> OpenPlatformAsync(OperationBudget budget,
        bool preciseDeadline = false)
    {
        budget.Check();
        if (Postgres is { } identity)
        {
            var builder = new NpgsqlConnectionStringBuilder(identity.ConnectionString)
            {
                Timeout = preciseDeadline ? budget.AsyncCommandSeconds() : budget.CommandSeconds(),
                CommandTimeout = preciseDeadline ? budget.AsyncCommandSeconds() : budget.CommandSeconds()
            };
            var connection = new NpgsqlConnection(builder.ConnectionString);
            try
            {
                await connection.OpenAsync(budget.Token);
                await identity.VerifyAsync(connection, budget, preciseDeadline);
                return connection;
            }
            catch { await connection.DisposeAsync(); throw; }
        }
        UnixStorePaths.ValidateRoot(Root!, allowMissing: false);
        return OpenSqlite(PlatformPath, budget);
    }

    internal static SqliteConnection OpenSqlite(string path, OperationBudget budget, bool allowOwnedEmpty = false,
        bool readOnly = false, StorageCode invalidHeaderCode = StorageCode.Corrupt)
    {
        ValidateSqlite(path, budget, allowOwnedEmpty, invalidHeaderCode);
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite,
            Pooling = false,
            ForeignKeys = true,
            DefaultTimeout = budget.CommandSeconds(sqlite: true)
        }.ToString());
        try
        {
            connection.Open();
            ValidateSqliteHeader(connection, budget, invalidHeaderCode);
            return connection;
        }
        catch (SqliteException error) when (error.SqliteErrorCode == SQLITE_NOTADB)
        {
            connection.Dispose();
            throw new StorageException(invalidHeaderCode);
        }
        catch { connection.Dispose(); throw; }
    }

    internal SqliteConnection OpenSqliteHandle(OperationBudget budget)
    {
        ValidateSqlite(PlatformPath, budget, allowOwnedEmpty: false);
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = PlatformPath,
            Mode = SqliteOpenMode.ReadWrite,
            Cache = SqliteCacheMode.Private,
            Pooling = false,
            DefaultTimeout = 1
        }.ToString());
        // DeadlineStore enables and verifies foreign keys through its bounded native executor.
        try { connection.Open(); budget.Check(); return connection; }
        catch { connection.Dispose(); throw; }
    }

    private static void ValidateSqlite(string path, OperationBudget budget, bool allowOwnedEmpty,
        StorageCode invalidHeaderCode = StorageCode.Corrupt)
    {
        budget.Check();
        UnixStorePaths.ValidateRoot(Path.GetDirectoryName(path)!, allowMissing: false);
        UnixStorePaths.ValidateFile(path);
        foreach (var suffix in new[] { "-journal", "-wal", "-shm" })
            UnixStorePaths.ValidateFile(path + suffix, allowMissing: true);
        var length = new FileInfo(path).Length;
        if (!(allowOwnedEmpty && length == 0) && length < 100)
            throw new StorageException(invalidHeaderCode);
    }

    private static void ValidateSqliteHeader(SqliteConnection connection, OperationBudget budget,
        StorageCode invalidHeaderCode)
    {
        var handle = connection.Handle!;
        SqliteException.ThrowExceptionForRC(sqlite3_busy_timeout(handle, 0), handle);
        sqlite3_progress_handler(handle, 1000, _ =>
            budget.Token.IsCancellationRequested || budget.Remaining <= TimeSpan.Zero ? 1 : 0, null);
        try
        {
            budget.Check();
            // SQLite must own header reads: managed sharing locks reject valid writers, and closing an
            // independently opened descriptor can release this process's SQLite byte-range locks.
            var result = sqlite3_exec(handle, "PRAGMA schema_version");
            budget.Check();
            if (result == SQLITE_NOTADB) throw new StorageException(invalidHeaderCode);
            SqliteException.ThrowExceptionForRC(result, handle);
        }
        finally { sqlite3_progress_handler(handle, 0, null, null); }
    }
}
