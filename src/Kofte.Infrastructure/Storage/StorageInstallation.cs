using Kofte.Infrastructure.Storage.Postgres;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;
using Npgsql;

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

    internal async Task<System.Data.Common.DbConnection> OpenPlatformAsync(OperationBudget budget)
    {
        budget.Check();
        if (Postgres is { } identity)
        {
            var builder = new NpgsqlConnectionStringBuilder(identity.ConnectionString)
            {
                Timeout = budget.CommandSeconds(),
                CommandTimeout = budget.CommandSeconds()
            };
            var connection = new NpgsqlConnection(builder.ConnectionString);
            try
            {
                await connection.OpenAsync(budget.Token);
                await identity.VerifyAsync(connection, budget);
                return connection;
            }
            catch { await connection.DisposeAsync(); throw; }
        }
        UnixStorePaths.ValidateRoot(Root!, allowMissing: false);
        return OpenSqlite(PlatformPath, budget);
    }

    internal static SqliteConnection OpenSqlite(string path, OperationBudget budget, bool allowOwnedEmpty = false,
        bool readOnly = false)
    {
        budget.Check();
        UnixStorePaths.ValidateRoot(Path.GetDirectoryName(path)!, allowMissing: false);
        UnixStorePaths.ValidateFile(path);
        foreach (var suffix in new[] { "-journal", "-wal", "-shm" })
            UnixStorePaths.ValidateFile(path + suffix, allowMissing: true);
        try
        {
            using var file = File.OpenRead(path);
            Span<byte> header = stackalloc byte[16];
            if (!(allowOwnedEmpty && file.Length == 0) &&
                (file.Length < 100 || file.Read(header) != header.Length ||
                 !header.SequenceEqual("SQLite format 3\0"u8)))
                throw new StorageException(StorageCode.Corrupt);
        }
        catch (IOException error) when (error.HResult is 11 or 35)
        { throw new StorageException(StorageCode.Busy); }
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = readOnly ? SqliteOpenMode.ReadOnly : SqliteOpenMode.ReadWrite,
            Pooling = false,
            ForeignKeys = true,
            DefaultTimeout = budget.CommandSeconds(sqlite: true)
        }.ToString());
        try { connection.Open(); budget.Check(); return connection; }
        catch { connection.Dispose(); throw; }
    }
}
