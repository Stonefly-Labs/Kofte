using Kofte.Host.Bootstrap;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Host.Features.StorageMaintenance;

public static class StorageServices
{
    public static StorageInstallation Installation(BootstrapConfiguration config) =>
        config.Profile == "local-sqlite" ? StorageInstallation.Sqlite(config.DataRoot) :
            StorageInstallation.PostgreSql(config.PostgresConnection!);

    public static void AddStorageServices(this IServiceCollection services, StorageInstallation installation)
    {
        services.AddSingleton<IStorageRegistry>(new StorageRegistry(installation));
    }
}
