using Kofte.TestSupport;

namespace Kofte.Host.IntegrationTests;

public sealed class PostgresFixture : IAsyncDisposable
{
    private readonly PostgresResources resources;
    private PostgresFixture(PostgresResources resources) => this.resources = resources;

    public string RunId => resources.RunId;
    public string ConnectionString => resources.ConnectionString;
    public string ProvisioningConnectionString => resources.ProvisioningConnectionString;

    public static async Task<PostgresFixture> CreateFromEnvironmentAsync(CancellationToken token) =>
        new(await PostgresResources.CreateFromEnvironmentAsync(token, shareOwnedService: true));

    public static async Task<PostgresFixture> CreateAsync(bool disposable,
        string? externalService = null, string? requestedDatabase = null, CancellationToken token = default) =>
        new(await PostgresResources.CreateAsync(disposable, externalService, requestedDatabase, token));

    public Task<bool> DatabaseExistsAsync(CancellationToken token) => resources.DatabaseExistsAsync(token);
    public Task SetDatabaseOwnershipAsync(bool valid, CancellationToken token) => resources.SetDatabaseOwnershipAsync(valid, token);
    public Task SetProvisionerOwnershipAsync(bool valid, CancellationToken token) => resources.SetProvisionerOwnershipAsync(valid, token);
    public Task SetSchemaCreationAsync(bool allowed, CancellationToken token) => resources.SetSchemaCreationAsync(allowed, token);
    public Task SetAvailableAsync(bool available, CancellationToken token) => resources.SetAvailableAsync(available, token);
    public Task ValidateOwnershipAsync(CancellationToken token) => resources.ValidateOwnershipAsync(token);
    internal Task<IAsyncDisposable> AcquireRecoveryAsync(CancellationToken token) => resources.AcquireRecoveryAsync(token);
    public Task BackupAsync(string output, CancellationToken token) => resources.BackupAsync(output, token);
    public Task RestoreAsync(string input, CancellationToken token) => resources.RestoreAsync(input, token);
    public async Task<PostgresFixture> CreateRecoveryTargetAsync(CancellationToken token) =>
        new(await resources.CreateRecoveryTargetAsync(token));
    public ValueTask DisposeAsync() => resources.DisposeAsync();
}
