using Kofte.Infrastructure.Storage;
using Microsoft.Data.Sqlite;

namespace Kofte.TestSupport.Identity;

public sealed partial class LoginCommitFixture
{
    public void SetProductLoginFault(LoginCommitFault value)
    {
        if (!productRoutes || application is null || worker is not null ||
            value is not (LoginCommitFault.None or LoginCommitFault.BeforeCommit or LoginCommitFault.AfterCommit))
            throw new InvalidOperationException("identity_fixture_login_fault_scope_invalid");
        fault = value;
    }

    public async Task<IAsyncDisposable> InterruptProductStoreAsync(CancellationToken token)
    {
        if (!productRoutes || scope is null)
            throw new InvalidOperationException("identity_fixture_product_required");
        if (scope.Postgres is { } postgres)
        {
            await postgres.SetAvailableAsync(false, token);
            return new StoreInterruption(postgres, null);
        }
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(scope.DataRoot, "platform", "platform.db"),
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false,
            DefaultTimeout = 1
        }.ConnectionString);
        try
        {
            await connection.OpenAsync(token);
            await using var command = connection.CreateCommand();
            command.CommandText = "BEGIN EXCLUSIVE";
            await command.ExecuteNonQueryAsync(token);
            return new StoreInterruption(null, connection);
        }
        catch { await connection.DisposeAsync(); throw; }
    }

    private sealed class StoreInterruption(PostgresResources? postgres, SqliteConnection? sqlite) : IAsyncDisposable
    {
        private bool disposed;

        public async ValueTask DisposeAsync()
        {
            if (disposed) return;
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            if (postgres is not null) await postgres.SetAvailableAsync(true, cleanup.Token);
            if (sqlite is not null)
            {
                await using var command = sqlite.CreateCommand();
                command.CommandText = "ROLLBACK";
                await command.ExecuteNonQueryAsync(cleanup.Token);
                await sqlite.DisposeAsync();
            }
            disposed = true;
        }
    }
}
