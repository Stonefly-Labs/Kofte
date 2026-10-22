using System.Data.Common;
using Kofte.Infrastructure.Storage.Migrations;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Infrastructure.Storage.Sqlite;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kofte.Infrastructure.Storage;

public sealed class ModuleStorageBinding : IModuleStorage
{
    private bool disposed;
    private int migrationActive;
    internal StorageRegistry Registry { get; }
    internal StorageAssignment Assignment { get; private set; }
    internal MaintenanceScope? MigrationScope { get; set; }
    public ModuleId Owner => Assignment.Owner;
    public string Provider => Assignment.Provider;
    public string? Schema => Provider == "postgresql" ? Assignment.AllocationKey : null;

    internal ModuleStorageBinding(StorageRegistry registry, StorageAssignment assignment) =>
        (Registry, Assignment) = (registry, assignment);

    public Task<StorageResult> ResolveAsync(CancellationToken cancellationToken = default) =>
        BindingFailures.RunAsync(async () =>
        {
            await VerifyAsync(cancellationToken);
            var result = await Registry.ResolveAsync(Owner, cancellationToken);
            if (result is StorageResult.Ready ready) Match(Assignment, ready.Assignment);
            return result;
        });

    public Task<TContext> CreateContextAsync<TContext>(
        Func<DbContextOptions, ModuleStorageBinding, TContext> factory,
        CancellationToken cancellationToken = default) where TContext : AssignedDbContext =>
        BindingFailures.RunAsync(() => AssignedContextFactory.CreateAsync(this, factory, cancellationToken));

    public Task<StorageResult> MigrateAsync<TContext>(
        Func<DbContextOptions, ModuleStorageBinding, TContext> factory,
        IStorageMaintenanceScope scope, string? targetMigration = null,
        CancellationToken cancellationToken = default) where TContext : AssignedDbContext =>
        ModuleMigrationRunner.MigrateAsync(this, factory, scope, targetMigration, cancellationToken);

    internal async Task VerifyAsync(CancellationToken token, bool verifyPhysical = true,
        bool allowMigration = false)
    {
        if (disposed) throw new StorageException(StorageCode.RequestInvalid);
        using var budget = new OperationBudget(OperationBudget.Resolve, token);
        if (allowMigration)
            await (MigrationScope ?? throw new StorageException(StorageCode.RequestInvalid))
                .VerifyAsync(Registry.Installation, budget);
        var (metadata, record) = await Registry.ReadBindingAsync(Owner, budget, allowMigration, verifyPhysical);
        Match(Assignment, record.Assignment(metadata));
        if (disposed) throw new StorageException(StorageCode.RequestInvalid);
    }

    internal async Task VerifyCommandAsync(DbCommand pending, CancellationToken token, bool allowMigration)
    {
        await VerifyAsync(token, verifyPhysical: false, allowMigration);
        if (Provider == "sqlite")
        {
            var paths = new ModuleStorePaths(Registry.Installation, Assignment.AllocationKey);
            paths.ValidateAncestors(allowMissing: false);
            UnixStorePaths.ValidateFile(paths.DatabasePath);
        }

        await using var command = pending.Connection!.CreateCommand();
        command.Transaction = pending.Transaction;
        command.CommandTimeout = Provider == "sqlite" ? 1 : 3;
        var table = Provider == "postgresql" ? $"\"{Assignment.AllocationKey}\".__kofte_storage_owner"
            : "__kofte_storage_owner";
        command.CommandText = $"SELECT marker_version,installation_id,module_id,allocation_id,provider,allocation_key FROM {table}";
        await using var reader = await command.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token) || reader.GetInt32(0) != 1 ||
            reader.GetGuid(1) != Assignment.InstallationId || reader.GetString(2) != Owner.Value ||
            reader.GetGuid(3) != Assignment.AllocationId || reader.GetString(4) != Provider ||
            reader.GetString(5) != Assignment.AllocationKey || await reader.ReadAsync(token))
            throw new StorageException(StorageCode.OwnerMismatch);
    }

    internal bool TryBeginMigration() => Interlocked.CompareExchange(ref migrationActive, 1, 0) == 0;
    internal void EndMigration()
    {
        MigrationScope = null;
        Volatile.Write(ref migrationActive, 0);
    }

    internal static void Match(StorageAssignment expected, StorageAssignment actual)
    {
        if (!expected.Owner.Equals(actual.Owner) || expected.InstallationId != actual.InstallationId ||
            expected.Provider != actual.Provider || expected.AllocationId != actual.AllocationId ||
            expected.AllocationKey != actual.AllocationKey || expected.Revision != actual.Revision ||
            expected.AllocatorVersion != actual.AllocatorVersion)
            throw new StorageException(StorageCode.OwnerMismatch);
    }

    internal void Refresh(StorageAssignment assignment)
    {
        if (disposed) throw new StorageException(StorageCode.RequestInvalid);
        Assignment = assignment;
    }

    internal async Task<DbConnection> OpenAsync(CancellationToken token)
    {
        await VerifyAsync(token);
        using var budget = new OperationBudget(OperationBudget.Resolve, token);
        if (Registry.Installation.Postgres is { } postgres)
        {
            var connection = new NpgsqlConnection(postgres.ConnectionString);
            try { await connection.OpenAsync(budget.Token); return connection; }
            catch { await connection.DisposeAsync(); throw; }
        }
        var paths = new ModuleStorePaths(Registry.Installation, Assignment.AllocationKey);
        paths.ValidateAncestors(allowMissing: false);
        return StorageInstallation.OpenSqlite(paths.DatabasePath, budget);
    }

    public ValueTask DisposeAsync()
    {
        disposed = true;
        return ValueTask.CompletedTask;
    }
}
