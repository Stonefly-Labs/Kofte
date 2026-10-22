using Kofte.Host.Bootstrap;

namespace Kofte.TestSupport;

public sealed class StorageProviderScope : IAsyncDisposable
{
    private readonly OwnedTestRoot root = new();
    private readonly CancellationTokenSource lifetime;
    private bool disposed;

    private StorageProviderScope(string provider, CancellationToken token)
    {
        Provider = provider;
        lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
        lifetime.CancelAfter(TimeSpan.FromSeconds(120));
    }

    public string Provider { get; }
    public string DataRoot => root.Path;
    public CancellationToken Token => lifetime.Token;
    public PostgresResources? Postgres { get; private set; }

    public static async Task<StorageProviderScope> CreateAsync(string provider, CancellationToken token)
    {
        StorageScenarioCatalog.RequireProvider(provider);
        token.ThrowIfCancellationRequested();
        var scope = new StorageProviderScope(provider, token);
        try
        {
            if (provider == "postgres")
                scope.Postgres = await PostgresResources.CreateFromEnvironmentAsync(scope.Token);
            else
            {
                await new SqlitePlatformStore(scope.DataRoot).InitializeAsync();
                await new SqlitePlatformStore(scope.DataRoot).ProbeAsync(scope.Token);
            }
            scope.Token.ThrowIfCancellationRequested();
            return scope;
        }
        catch
        {
            await scope.DisposeAsync();
            throw;
        }
    }

    public void ConfigureChild(System.Diagnostics.ProcessStartInfo start, bool offlineMaintenance = false)
    {
        OwnedProcesses.ClearSensitiveEnvironment(start);
        start.Environment["KOFTE_DATA_ROOT"] = DataRoot;
        if (Postgres is null) return;
        start.Environment["KOFTE_PROFILE"] = "test-postgres";
        start.Environment["KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE"] = "true";
        start.Environment["KOFTE_POSTGRES_CONNECTION"] = Postgres.ConnectionString;
        if (offlineMaintenance)
            start.Environment["KOFTE_POSTGRES_PROVISIONING_CONNECTION"] = Postgres.ProvisioningConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        await lifetime.CancelAsync();
        if (Postgres is not null) await Postgres.DisposeAsync();
        root.Dispose();
        lifetime.Dispose();
        disposed = true;
    }
}
