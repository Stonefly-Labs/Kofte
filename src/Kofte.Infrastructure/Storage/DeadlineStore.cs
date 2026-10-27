using System.Data.Common;
using System.Globalization;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;
using Npgsql;
using SQLitePCL;
using static SQLitePCL.raw;

namespace Kofte.Infrastructure.Storage;

internal sealed class DeadlineStore : IAsyncDisposable
{
    private static readonly TimeSpan CleanupReserve = TimeSpan.FromMilliseconds(50);
    private readonly DbConnection connection;
    private readonly OperationBudget work;
    private readonly bool ownsConnection;
    private DbTransaction? transaction;
    private bool disposed;
    internal string Prefix => connection is NpgsqlConnection ? "kofte_system." : "";
    internal OperationBudget Budget => work;

    private DeadlineStore(DbConnection connection, OperationBudget work, bool ownsConnection = true)
        => (this.connection, this.work, this.ownsConnection) = (connection, work, ownsConnection);

    internal static DeadlineStore Borrow(DbConnection connection, DbTransaction transaction, OperationBudget budget)
    {
        if (transaction.Connection != connection) throw new StorageException(StorageCode.RequestInvalid);
        budget.Check();
        var store = new DeadlineStore(connection, new OperationBudget(budget.Remaining, budget.Token), false)
        { transaction = transaction };
        if (connection is SqliteConnection sqlite)
        {
            sqlite3_progress_handler(sqlite.Handle!, 1000, _ =>
                store.work.Token.IsCancellationRequested || store.work.Remaining <= TimeSpan.Zero ? 1 : 0, null);
        }
        return store;
    }

    internal static async Task<DeadlineStore> OpenAsync(StorageInstallation installation, OperationBudget budget)
    {
        budget.Check();
        if (budget.Remaining <= CleanupReserve) throw new OperationCanceledException(budget.Token);
        var work = new OperationBudget(budget.Remaining - CleanupReserve, budget.Token);
        DeadlineStore? store = null;
        try
        {
            DbConnection connection;
            if (installation.Provider == "sqlite")
            {
                while (true)
                {
                    work.Check();
                    try
                    {
                        connection = installation.OpenSqliteHandle(work);
                        break;
                    }
                    catch (StorageException error) when (error.Code == StorageCode.Busy)
                    {
                        // The header guard has not opened a SQLite handle or begun any transaction.
                        await Task.Delay(OperationBudget.ProbeInterval, work.Token);
                    }
                }
            }
            else connection = await installation.OpenPlatformAsync(work, preciseDeadline: true);
            store = new(connection, work);
            if (connection is SqliteConnection sqlite)
            {
                SqliteException.ThrowExceptionForRC(sqlite3_busy_timeout(sqlite.Handle!, 0), sqlite.Handle);
                sqlite3_progress_handler(sqlite.Handle!, 1000, _ =>
                    work.Token.IsCancellationRequested || work.Remaining <= TimeSpan.Zero ? 1 : 0, null);
                await store.QueryAsync("PRAGMA foreign_keys=ON");
                if (AssertScalar(await store.QueryAsync("PRAGMA foreign_keys")) is not 1L)
                    throw new StorageException(StorageCode.Corrupt);
            }
            return store;
        }
        catch
        {
            if (store is not null) await store.DisposeAsync();
            else work.Dispose();
            throw;
        }
    }

    internal async Task BeginWriteAsync()
    {
        Check();
        if (!ownsConnection) throw new StorageException(StorageCode.RequestInvalid);
        if (connection is SqliteConnection sqlite)
        {
            if (sqlite3_get_autocommit(sqlite.Handle!) == 0) throw new StorageException(StorageCode.RequestInvalid);
            await NativeAsync("BEGIN IMMEDIATE", []);
        }
        else
        {
            if (transaction is not null) throw new StorageException(StorageCode.RequestInvalid);
            transaction = await connection.BeginTransactionAsync(work.Token);
        }
    }

    internal async Task CommitAsync()
    {
        Check();
        if (!ownsConnection) throw new StorageException(StorageCode.RequestInvalid);
        if (connection is SqliteConnection sqlite)
        {
            if (sqlite3_get_autocommit(sqlite.Handle!) != 0) throw new StorageException(StorageCode.RequestInvalid);
            await NativeAsync("COMMIT", [], committing: true);
        }
        else
        {
            if (transaction is null) throw new StorageException(StorageCode.RequestInvalid);
            await transaction.CommitAsync(work.Token);
            await transaction.DisposeAsync();
            transaction = null;
        }
    }

    internal async Task<List<object?[]>> QueryAsync(string sql, params (string Name, object? Value)[] parameters)
    {
        Check();
        if (connection is SqliteConnection) return await NativeAsync(sql, parameters);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = work.AsyncCommandSeconds();
        command.Transaction = transaction;
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }
        await using var reader = await command.ExecuteReaderAsync(work.Token);
        var rows = new List<object?[]>();
        while (await reader.ReadAsync(work.Token))
        {
            Check();
            if (rows.Count == 128) throw new StorageException(StorageCode.RequestInvalid);
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            rows.Add(values.Select(value => value is DBNull ? null : value).ToArray());
        }
        return rows;
    }

    private async Task<List<object?[]>> NativeAsync(string sql, (string Name, object? Value)[] parameters,
        bool committing = false)
    {
        var handle = ((SqliteConnection)connection).Handle!;
        sqlite3_stmt statement;
        while (true)
        {
            Check();
            var prepared = sqlite3_prepare_v2(handle, sql, out statement, out var tail);
            if (prepared == SQLITE_BUSY && sqlite3_get_autocommit(handle) != 0)
            {
                statement?.Dispose();
                await Task.Delay(OperationBudget.ProbeInterval, work.Token);
                continue;
            }
            if (prepared != SQLITE_OK)
            {
                statement?.Dispose();
                ThrowNative(prepared);
            }
            if (!string.IsNullOrWhiteSpace(tail) || statement is null || statement.IsInvalid)
            {
                statement?.Dispose();
                throw new StorageException(StorageCode.RequestInvalid);
            }
            break;
        }
        using (statement)
        {
            if (sqlite3_bind_parameter_count(statement) != parameters.Length)
                throw new StorageException(StorageCode.RequestInvalid);
            var bound = new HashSet<int>();
            foreach (var (name, value) in parameters)
            {
                var index = sqlite3_bind_parameter_index(statement, "@" + name);
                if (index == 0 || !bound.Add(index)) throw new StorageException(StorageCode.RequestInvalid);
                ThrowNative(Bind(statement, index, value));
            }
            var rows = new List<object?[]>();
            while (true)
            {
                Check();
                var result = sqlite3_step(statement);
                if (result == SQLITE_BUSY && rows.Count == 0 &&
                    (committing || sqlite3_get_autocommit(handle) != 0))
                {
                    // Retry only SQLite's documented known-not-committed boundaries, never a transaction body.
                    sqlite3_reset(statement);
                    await Task.Delay(OperationBudget.ProbeInterval, work.Token);
                    continue;
                }
                if (result == SQLITE_DONE) return rows;
                ThrowNative(result);
                if (rows.Count == 128) throw new StorageException(StorageCode.RequestInvalid);
                var row = new object?[sqlite3_column_count(statement)];
                for (var index = 0; index < row.Length; index++)
                    row[index] = sqlite3_column_type(statement, index) switch
                    {
                        SQLITE_NULL => null,
                        SQLITE_INTEGER => sqlite3_column_int64(statement, index),
                        SQLITE_TEXT => sqlite3_column_text(statement, index).utf8_to_string(),
                        SQLITE_BLOB => sqlite3_column_blob(statement, index).ToArray(),
                        _ => throw new StorageException(StorageCode.Corrupt)
                    };
                rows.Add(row);
            }
        }
    }

    private static int Bind(sqlite3_stmt statement, int index, object? value) => value switch
    {
        null or DBNull => sqlite3_bind_null(statement, index),
        string text => sqlite3_bind_text(statement, index, text),
        Guid id => sqlite3_bind_text(statement, index, id.ToString("D")),
        DateTime { Kind: DateTimeKind.Utc } time => sqlite3_bind_text(statement, index,
            time.ToString("O", CultureInfo.InvariantCulture)),
        long number => sqlite3_bind_int64(statement, index, number),
        int number => sqlite3_bind_int(statement, index, number),
        bool flag => sqlite3_bind_int(statement, index, flag ? 1 : 0),
        byte[] bytes => sqlite3_bind_blob(statement, index, bytes),
        _ => throw new StorageException(StorageCode.RequestInvalid)
    };

    internal static object? AssertScalar(List<object?[]> rows)
    {
        if (rows.Count != 1 || rows[0].Length != 1) throw new StorageException(StorageCode.Corrupt);
        return rows[0][0];
    }

    private void Check()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        work.Check();
    }

    private void ThrowNative(int result)
    {
        if (result == SQLITE_INTERRUPT && (work.Token.IsCancellationRequested || work.Remaining <= TimeSpan.Zero))
            throw new OperationCanceledException(work.Token);
        SqliteException.ThrowExceptionForRC(result, ((SqliteConnection)connection).Handle);
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            if (connection is SqliteConnection sqlite)
            {
                sqlite3_progress_handler(sqlite.Handle!, 0, null, null);
                if (ownsConnection && sqlite3_get_autocommit(sqlite.Handle!) == 0)
                    SqliteException.ThrowExceptionForRC(sqlite3_exec(sqlite.Handle!, "ROLLBACK"), sqlite.Handle);
            }
        }
        finally
        {
            // Unpooled PostgreSQL closes the session; do not await an uncancellable rollback round trip.
            try { if (ownsConnection) await connection.DisposeAsync(); }
            finally { work.Dispose(); }
        }
    }
}
