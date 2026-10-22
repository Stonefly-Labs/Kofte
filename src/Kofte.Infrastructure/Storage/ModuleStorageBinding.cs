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
        StorageDiagnostics.RunAsync(StorageOperation.Resolve, Provider, Owner, OperationBudget.Resolve,
            cancellationToken, async (budget, _, correlation) =>
        {
            await VerifyAsync(budget);
            return new StorageResult.Ready(StorageOperation.Resolve, Assignment, correlation,
                StorageDurability.Unchanged, StorageDiagnostics.Diagnostic(StorageCode.Ready));
        });

    public Task<TContext> CreateContextAsync<TContext>(
        Func<DbContextOptions, ModuleStorageBinding, TContext> factory,
        CancellationToken cancellationToken = default) where TContext : AssignedDbContext =>
        BindingFailures.RunAsync(Provider, Owner, async budget =>
            (await AssignedContextFactory.CreateAsync(this, factory, budget), Assignment), cancellationToken);

    public Task<StorageResult> MigrateAsync<TContext>(
        Func<DbContextOptions, ModuleStorageBinding, TContext> factory,
        IStorageMaintenanceScope scope, string? targetMigration = null,
        CancellationToken cancellationToken = default) where TContext : AssignedDbContext =>
        ModuleMigrationRunner.MigrateAsync(this, factory, scope, targetMigration, cancellationToken);

    internal async Task VerifyAsync(CancellationToken token, bool verifyPhysical = true,
        bool allowMigration = false)
    {
        using var budget = new OperationBudget(OperationBudget.Resolve, token);
        await VerifyAsync(budget, verifyPhysical, allowMigration);
    }

    internal async Task VerifyAsync(OperationBudget budget, bool verifyPhysical = true,
        bool allowMigration = false)
    {
        if (disposed) throw new StorageException(StorageCode.RequestInvalid);
        budget.Check();
        if (allowMigration)
            await (MigrationScope ?? throw new StorageException(StorageCode.RequestInvalid))
                .VerifyAsync(Registry.Installation, budget);
        var (metadata, record) = await Registry.ReadBindingAsync(Owner, budget, allowMigration, verifyPhysical);
        Match(Assignment, record.Assignment(metadata));
        budget.Check();
        if (disposed) throw new StorageException(StorageCode.RequestInvalid);
    }

    internal async Task VerifyCommandAsync(DbCommand pending, OperationBudget budget, bool allowMigration)
    {
        await VerifyAsync(budget, verifyPhysical: false, allowMigration);
        if (Provider == "sqlite")
        {
            var paths = new ModuleStorePaths(Registry.Installation, Assignment.AllocationKey);
            paths.ValidateAncestors(allowMissing: false);
            UnixStorePaths.ValidateFile(paths.DatabasePath);
        }

        await using var command = pending.Connection!.CreateCommand();
        command.Transaction = pending.Transaction;
        command.CommandTimeout = budget.CommandSeconds(Provider == "sqlite");
        var table = Provider == "postgresql" ? $"\"{Assignment.AllocationKey}\".__kofte_storage_owner"
            : "__kofte_storage_owner";
        command.CommandText = $"SELECT marker_version,installation_id,module_id,allocation_id,provider,allocation_key FROM {table}";
        await using var reader = await command.ExecuteReaderAsync(budget.Token);
        if (!await reader.ReadAsync(budget.Token) || reader.GetInt32(0) != 1 ||
            reader.GetGuid(1) != Assignment.InstallationId || reader.GetString(2) != Owner.Value ||
            reader.GetGuid(3) != Assignment.AllocationId || reader.GetString(4) != Provider ||
            reader.GetString(5) != Assignment.AllocationKey || await reader.ReadAsync(budget.Token))
            throw new StorageException(StorageCode.OwnerMismatch);
        pending.CommandTimeout = budget.CommandSeconds(Provider == "sqlite");
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

    internal async Task<DbConnection> OpenAsync(OperationBudget budget, bool allowMigration = false)
    {
        await VerifyAsync(budget, allowMigration: allowMigration);
        if (Registry.Installation.Postgres is { } postgres)
        {
            var configuration = new NpgsqlConnectionStringBuilder(postgres.ConnectionString)
            {
                Timeout = budget.CommandSeconds(),
                CommandTimeout = budget.CommandSeconds(),
                CancellationTimeout = -1
            };
            var connection = new NpgsqlConnection(configuration.ConnectionString);
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
