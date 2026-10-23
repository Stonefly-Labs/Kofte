using System.Data.Common;
using Kofte.Infrastructure.Storage;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Kofte.TestSupport;

public sealed class StorageReadBlock : IAsyncDisposable
{
    private readonly DbConnection connection;
    private readonly DbTransaction? transaction;
    public StorageInstallation Reader { get; }

    private StorageReadBlock(DbConnection connection, DbTransaction? transaction, StorageInstallation reader) =>
        (this.connection, this.transaction, Reader) = (connection, transaction, reader);

    public static async Task<StorageReadBlock> CreateAsync(string provider, string? root, string? runtime,
        CancellationToken token)
    {
        StorageInstallation reader;
        DbConnection connection;
        if (provider == "sqlite")
        {
            reader = StorageInstallation.Sqlite(root ?? throw new InvalidOperationException("storage_root_required"));
            connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(root, "platform", "platform.db"),
                Mode = SqliteOpenMode.ReadWrite,
                Pooling = false,
                DefaultTimeout = 1
            }.ToString());
        }
        else if (provider == "postgres")
        {
            var config = new NpgsqlConnectionStringBuilder(runtime) { Options = "-c lock_timeout=100" };
            reader = StorageInstallation.PostgreSql(config.ConnectionString);
            connection = new NpgsqlConnection(runtime);
        }
        else throw new InvalidOperationException("storage_provider_required");
        try
        {
            await connection.OpenAsync(token);
            var transaction = connection is SqliteConnection ? null : await connection.BeginTransactionAsync(token);
            try
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandTimeout = 3;
                command.CommandText = provider == "sqlite" ? "BEGIN EXCLUSIVE"
                    : "LOCK TABLE kofte_system.__kofte_installation IN ACCESS EXCLUSIVE MODE";
                await command.ExecuteNonQueryAsync(token);
                return new(connection, transaction, reader);
            }
            catch { if (transaction is not null) await transaction.DisposeAsync(); throw; }
        }
        catch { await connection.DisposeAsync(); throw; }
    }

    public async ValueTask DisposeAsync()
    {
        try { if (transaction is not null) await transaction.DisposeAsync(); }
        finally { await connection.DisposeAsync(); }
    }
}
