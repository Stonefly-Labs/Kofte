using System.Data;
using System.Diagnostics;
using Kofte.Infrastructure.Storage.Postgres;
using Kofte.Platform.Modules.Storage;
using Npgsql;

namespace Kofte.Infrastructure.Storage;

internal sealed class MaintenanceScope : IStorageMaintenanceScope
{
    private readonly StoreLease? fileLease;
    private readonly NpgsqlConnection? session;
    private readonly CancellationTokenSource lifetime;
    private bool disposed;
    internal StorageInstallation Installation { get; }
    public CancellationToken Lifetime => lifetime.Token;

    private MaintenanceScope(StorageInstallation installation, StoreLease? lease,
        NpgsqlConnection? session, CancellationToken caller)
    {
        Installation = installation;
        fileLease = lease;
        this.session = session;
        lifetime = CancellationTokenSource.CreateLinkedTokenSource(caller);
    }

    internal static async Task<MaintenanceScope> AcquireAsync(StorageInstallation installation,
        CancellationToken token)
    {
        using var budget = new OperationBudget(OperationBudget.Acquire, token);
        var started = Stopwatch.GetTimestamp();
        NpgsqlConnection? session = null;
        var contended = false;
        try
        {
            if (installation.Postgres is { } identity)
            {
                var builder = new NpgsqlConnectionStringBuilder(identity.ConnectionString)
                { Timeout = budget.CommandSeconds(), CommandTimeout = budget.CommandSeconds() };
                session = new NpgsqlConnection(builder.ConnectionString);
                await session.OpenAsync(budget.Token);
                await identity.VerifyAsync(session, budget);
            }
            while (true)
            {
                token.ThrowIfCancellationRequested();
                if (Stopwatch.GetElapsedTime(started) >= OperationBudget.Acquire - OperationBudget.ProbeInterval ||
                    session is not null && contended && budget.Remaining < TimeSpan.FromSeconds(1))
                    throw new StorageException(StorageCode.Busy);
                if (session is not null)
                {
                    await using var command = new NpgsqlCommand(
                        "SELECT pg_catalog.pg_try_advisory_lock($1)", session)
                    { CommandTimeout = budget.CommandSeconds() };
                    command.Parameters.AddWithValue(PostgresIdentity.LeaseKey);
                    if (await command.ExecuteScalarAsync(budget.Token) is true)
                    {
                        var scope = new MaintenanceScope(installation, null, session, token);
                        session = null;
                        return scope;
                    }
                }
                else
                {
                    try { return new(installation, StoreLease.Acquire(installation.Root!), null, token); }
                    catch (StorePathException error) when (error.Code == StorePathError.StoreBusy) { }
                }
                contended = true;
                await Task.Delay(OperationBudget.ProbeInterval, budget.Token);
            }
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        { throw new StorageException(contended ? StorageCode.Busy : StorageCode.DeadlineExceeded); }
        finally { if (session is not null) await session.DisposeAsync(); }
    }

    internal async Task VerifyAsync(StorageInstallation installation, OperationBudget budget)
    {
        if (disposed || !ReferenceEquals(Installation, installation))
            throw new StorageException(StorageCode.RequestInvalid);
        if (lifetime.IsCancellationRequested) throw new StorageException(StorageCode.Cancelled);
        budget.Check();
        if (session is null) return;
        if (session.State != ConnectionState.Open)
        {
            lifetime.Cancel();
            throw new StorageException(StorageCode.Unavailable);
        }
        try
        {
            await using var command = new NpgsqlCommand("""
                SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_locks WHERE locktype='advisory'
                  AND pid=pg_catalog.pg_backend_pid() AND granted
                  AND mode='ExclusiveLock'
                  AND classid=(($1::bigint >> 32) & 4294967295)::oid
                  AND objid=($1::bigint & 4294967295)::oid AND objsubid=1)
                """, session) { CommandTimeout = budget.CommandSeconds() };
            command.Parameters.AddWithValue(PostgresIdentity.LeaseKey);
            if (await command.ExecuteScalarAsync(budget.Token) is not true)
            {
                lifetime.Cancel();
                throw new StorageException(StorageCode.Unavailable);
            }
        }
        catch (NpgsqlException) { lifetime.Cancel(); throw; }
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        lifetime.Cancel();
        try
        {
            if (session is not null) await session.DisposeAsync();
        }
        finally { fileLease?.Dispose(); lifetime.Dispose(); }
    }
}
