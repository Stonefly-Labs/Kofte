using System.Collections.ObjectModel;
using System.Data;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Postgres;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Npgsql;

namespace Kofte.Infrastructure.Composition;

public sealed partial class CompositionStorageSession : IAsyncDisposable
{
    private readonly StorageInstallation installation;
    private readonly MaintenanceScope? maintenance;
    private readonly StoreLease? fileLease;
    private readonly NpgsqlConnection? leaseConnection;
    private readonly Dictionary<ModuleId, ModuleStorageBinding> bindings = [];
    private ModuleDescription[]? inspectedOwners;
    private ModuleStorageRecipe[]? inspectedRecipes;
    private Dictionary<(ModuleId Owner, string Provider), string> inspectedTargets = [];
    private bool operationStarted;
    private bool disposed;

    private CompositionStorageSession(StorageInstallation installation, MaintenanceScope? maintenance,
        StoreLease? fileLease, NpgsqlConnection? connection)
        => (this.installation, this.maintenance, this.fileLease, leaseConnection) =
            (installation, maintenance, fileLease, connection);

    public static async Task<CompositionStorageSession> OpenPrepareAsync(StorageInstallation installation,
        CancellationToken token = default)
    {
        // Return the classified storage result before the host's two-second acquisition boundary.
        var scope = await new StorageMaintenance(installation).AcquireWithinAsync(TimeSpan.FromMilliseconds(1500), token);
        return new(installation, (MaintenanceScope)scope, null, null);
    }

    public static async Task<CompositionStorageSession> OpenReadAsync(StorageInstallation installation,
        CancellationToken token = default)
    {
        using var budget = new OperationBudget(OperationBudget.Acquire, token);
        if (installation.Provider == "sqlite")
        {
            try
            {
                budget.Check();
                return new(installation, null, StoreLease.Acquire(installation.Root!), null);
            }
            catch (StorePathException error)
            {
                throw new StorageException(error.Code == StorePathError.StoreBusy ? StorageCode.Busy :
                    error.Code == StorePathError.ConfigurationInvalid ? StorageCode.RequestInvalid :
                    StorageCode.PathRejected);
            }
        }
        var connection = new NpgsqlConnection(installation.Postgres!.ConnectionString);
        try
        {
            await connection.OpenAsync(budget.Token);
            if (!await installation.Postgres.TryAcquireLeaseAsync(connection, budget, shared: true))
                throw new StorageException(StorageCode.Busy);
            return new(installation, null, null, connection);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private Task VerifyAuthorityAsync(OperationBudget budget)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return VerifyHeldAuthorityAsync(budget);
    }

    public async Task EnsureAuthorityAsync(CancellationToken token = default)
    {
        using var budget = new OperationBudget(OperationBudget.Resolve, token);
        await VerifyAuthorityAsync(budget);
    }

    private async Task VerifyHeldAuthorityAsync(OperationBudget budget)
    {
        budget.Check();
        if (maintenance is not null)
        {
            await maintenance.VerifyAsync(installation, budget);
            return;
        }
        if (leaseConnection is null) return;
        if (leaseConnection.State != ConnectionState.Open)
            throw new StorageException(StorageCode.Unavailable);
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_locks WHERE locktype='advisory'
              AND pid=pg_catalog.pg_backend_pid() AND granted AND mode='ShareLock'
              AND classid=(($1::bigint >> 32) & 4294967295)::oid
              AND objid=($1::bigint & 4294967295)::oid AND objsubid=1)
            """, leaseConnection) { CommandTimeout = budget.CommandSeconds() };
        command.Parameters.AddWithValue(PostgresIdentity.LeaseKey);
        if (await command.ExecuteScalarAsync(budget.Token) is not true)
            throw new StorageException(StorageCode.Unavailable);
        await installation.Postgres!.VerifyAsync(leaseConnection, budget);
    }

    private void BeginOperation(IReadOnlyList<ModuleDescription> enabled, IReadOnlyList<ModuleStorageRecipe> recipes)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (operationStarted || inspectedOwners is null || inspectedRecipes is null ||
            !inspectedOwners.SequenceEqual(enabled) || !inspectedRecipes.SequenceEqual(recipes))
            throw new CompositionStorageException("composition_recipe_invalid");
        operationStarted = true;
    }

    private IReadOnlyDictionary<ModuleId, ModuleStorageBinding> PublishedBindings() =>
        new ReadOnlyDictionary<ModuleId, ModuleStorageBinding>(new Dictionary<ModuleId, ModuleStorageBinding>(bindings));

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            foreach (var binding in bindings.Values.Reverse()) await binding.DisposeAsync();
            using var budget = new OperationBudget(OperationBudget.Resolve, CancellationToken.None);
            await VerifyHeldAuthorityAsync(budget);
        }
        finally
        {
            try
            {
                if (maintenance is not null) await maintenance.DisposeAsync();
                if (leaseConnection is not null) await leaseConnection.DisposeAsync();
            }
            finally { fileLease?.Dispose(); }
        }
    }
}
