using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Modules.Alpha;
using Kofte.Storage.IntegrationTests.Modules.Beta;
using Kofte.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Kofte.Storage.IntegrationTests.Providers;

internal sealed class ModuleFixture(StorageProviderScope resources) : IAsyncDisposable
{
    internal StorageInstallation Installation { get; private set; } = null!;
    internal StorageRegistry Registry { get; private set; } = null!;
    internal StorageMaintenance Maintenance { get; private set; } = null!;
    internal ModuleStorageBinding Alpha { get; private set; } = null!;
    internal ModuleStorageBinding Beta { get; private set; } = null!;
    internal string Root => resources.DataRoot;
    internal bool Postgres => resources.Postgres is not null;
    internal static readonly Func<DbContextOptions, ModuleStorageBinding, AlphaContext> AlphaFactory =
        (options, binding) => binding.Provider == "postgresql"
            ? new PostgresAlphaContext(options, binding) : new SqliteAlphaContext(options, binding);
    internal static readonly Func<DbContextOptions, ModuleStorageBinding, BetaContext> BetaFactory =
        (options, binding) => binding.Provider == "postgresql"
            ? new PostgresBetaContext(options, binding) : new SqliteBetaContext(options, binding);

    internal static async Task<ModuleFixture> CreateAsync(string provider, CancellationToken token)
    {
        var fixture = new ModuleFixture(await StorageProviderScope.CreateAsync(provider, token));
        try
        {
            fixture.Installation = fixture.Postgres
                ? StorageInstallation.PostgreSql(resourcesConnection(fixture), provisioningConnection(fixture))
                : StorageInstallation.Sqlite(fixture.Root);
            fixture.Registry = new(fixture.Installation);
            fixture.Maintenance = new(fixture.Installation);
            await using var scope = await fixture.Maintenance.AcquireAsync(token);
            await fixture.Maintenance.UpgradeAsync(scope, token);
            foreach (var id in new[] { "org.example.alpha", "org.example.beta" })
                Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(Module(id), scope, token));
            fixture.Alpha = await fixture.Registry.BindAsync(Module("org.example.alpha").Id, token);
            fixture.Beta = await fixture.Registry.BindAsync(Module("org.example.beta").Id, token);
            return fixture;
        }
        catch { await fixture.DisposeAsync(); throw; }

        static string resourcesConnection(ModuleFixture value) => value.Resources.Postgres!.ConnectionString;
        static string provisioningConnection(ModuleFixture value) => value.Resources.Postgres!.ProvisioningConnectionString;
    }

    private StorageProviderScope Resources => resources;
    internal static ModuleDescription Module(string id)
    {
        var input = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            id,
            version = "1.0.0",
            displayName = "Köfte isolated test module",
            hostContract = "1.0.0",
            capabilities = Array.Empty<object>(),
            dependencies = Array.Empty<object>(),
            permissions = Array.Empty<object>(),
            storage = new { relational = true }
        });
        return Assert.IsType<ManifestValidationResult.Accepted>(
            ManifestValidator.Validate(input, new ManifestValidationContext("1.0.0"))).Description;
    }

    internal async Task<long> PlatformRevisionAsync(CancellationToken token)
    {
        using var budget = new OperationBudget(OperationBudget.Resolve, token);
        await using var connection = await Installation.OpenPlatformAsync(budget);
        return (await new RegistryMigrationRunner(Installation).ReadInstallationAsync(connection, budget)).Revision;
    }

    public async ValueTask DisposeAsync()
    {
        if (Alpha is not null) await Alpha.DisposeAsync();
        if (Beta is not null) await Beta.DisposeAsync();
        await resources.DisposeAsync();
    }
}
