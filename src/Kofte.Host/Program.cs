using System.Net;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.Bootstrap;
using Kofte.Host.Features.StorageMaintenance;
using Kofte.Host.Features.ModuleComposition;
using Kofte.Host.Features.IdentityMaintenance;
using Kofte.Host.Features.Identity;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Microsoft.Data.Sqlite;
using Npgsql;

return await BootstrapProgram.RunAsync(args);

public partial class Program;

public static class BootstrapProgram
{
    public static Task<int> RunAsync(string[] args) => ModuleCompositionHost.RunAsync(args, []);

    internal static async Task<int> RunUncomposedAsync(string[] args)
    {
        try
        {
            if (args.FirstOrDefault() == "--identity-worker")
                return await IdentityMaintenanceWorker.ExecuteAsync(args);
            if (args.FirstOrDefault() == "--identity")
            {
                await IdentityCommand.RunAsync(args);
                return 0;
            }
            if (args.FirstOrDefault() == "--registry-worker")
            {
                return await MaintenanceWorker.ExecuteAsync(args);
            }
            if (args.FirstOrDefault() == "--registry")
            {
                await RegistryCommand.RunAsync(args);
                return 0;
            }
            var config = BootstrapConfiguration.FromEnvironment();
            if (args.Length != 0)
            {
                await StoreCommand.RunAsync(config, args);
                return 0;
            }
            IPlatformStore store = config.Profile == "local-sqlite"
                ? new SqlitePlatformStore(config.DataRoot)
                : new PostgresPlatformStore(config.PostgresConnection ??
                    throw new BootstrapException(BootstrapError.ConfigurationInvalid));
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
            {
                Args = ["--hostBuilder:reloadConfigOnChange=false"]
            });
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection();
            builder.Logging.ClearProviders();
            using var lease = store is SqlitePlatformStore sqlite
                ? Kofte.Host.Bootstrap.StoreLease.Acquire(sqlite.Root) : null;
            using var startupBudget = new OperationBudget(OperationBudget.Resolve, CancellationToken.None);
            await using var postgresLease = store is PostgresPlatformStore postgres
                ? await postgres.AcquireHostLeaseAsync(startupBudget.Token) : null;
            await store.ProbeAsync(startupBudget.Token);
            var installation = StorageServices.Installation(config);
            using var identity = await IdentityRuntime.OpenAsync(config, builder.Environment.EnvironmentName,
                installation, startupBudget);
            var bindings = new List<string> { config.HostUrl.AbsoluteUri };
            if (!BootstrapConfiguration.Loopback(config.HostUrl.Host) &&
                IPAddress.TryParse(config.HostUrl.Host.Trim('[', ']'), out var boundAddress) &&
                !boundAddress.Equals(IPAddress.Any) && !boundAddress.Equals(IPAddress.IPv6Any))
                bindings.Add(config.BackendOrigin.AbsoluteUri);
            builder.WebHost.UseUrls([.. bindings])
                .UseKestrel(options => options.AddServerHeader = false);
            builder.Services.AddSingleton(config with { PostgresConnection = null });
            builder.Services.AddStorageServices(installation);
            builder.Services.AddSingleton<IPlatformStore>(store);
            builder.Services.AddSingleton<HealthHandler>();
            identity.Register(builder.Services, lease?.Authority, postgresLease);
            if (builder.Environment.IsDevelopment()) builder.Services.AddBootstrapOpenApi();
            BootstrapTelemetry.Add(builder.Services, config);
            await using var app = builder.Build();
            app.MapBootstrapHealth();
            app.UseIdentityBoundary();
            app.MapIdentityEndpoints();
            if (app.Environment.IsDevelopment())
                app.MapOpenApi("/openapi/v1.json").WithName("bootstrapOpenApi").AllowAnonymous();
            if (config.UnsafeExposure)
                Console.Error.WriteLine("""{"level":"warning","eventCode":"unsafe_local_exposure","component":"host"}""");
            await app.RunAsync();
            return 0;
        }
        catch (Exception error) when (error is IdentityStateException or IdentityConfigurationException or
            InvalidOperationException { Message: "identity_keys_invalid" })
        {
            return 1;
        }
        catch (BootstrapException error)
        {
            BootstrapDiagnostics.Failure(error.Code);
            return 1;
        }
        catch (StorageException)
        {
            BootstrapDiagnostics.Failure(BootstrapError.StorageInvalid);
            return 1;
        }
        catch (StorePathException)
        {
            BootstrapDiagnostics.Failure(BootstrapError.StorageInvalid);
            return 1;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            InvalidOperationException or SqliteException or NpgsqlException or OperationCanceledException)
        {
            BootstrapDiagnostics.Failure(BootstrapError.StorageUnavailable);
            return 1;
        }
    }
}
