using System.Net;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.Bootstrap;
using Microsoft.Data.Sqlite;

return await BootstrapProgram.RunAsync(args);

public partial class Program;

public static class BootstrapProgram
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var config = BootstrapConfiguration.FromEnvironment();
            if (args.Length != 0)
            {
                await StoreCommand.RunAsync(config, args);
                return 0;
            }
            if (config.Profile != "local-sqlite")
                throw new BootstrapException(BootstrapError.ConfigurationInvalid);
            var store = new SqlitePlatformStore(config.DataRoot);
            using var lease = StoreLease.Acquire(store.Root);
            using var startupBudget = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await store.ProbeAsync(startupBudget.Token);
            var builder = WebApplication.CreateSlimBuilder();
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection();
            builder.Logging.ClearProviders();
            var bindings = new List<string> { config.HostUrl.AbsoluteUri };
            if (!BootstrapConfiguration.Loopback(config.HostUrl.Host) &&
                IPAddress.TryParse(config.HostUrl.Host.Trim('[', ']'), out var boundAddress) &&
                !boundAddress.Equals(IPAddress.Any) && !boundAddress.Equals(IPAddress.IPv6Any))
                bindings.Add(config.BackendOrigin.AbsoluteUri);
            builder.WebHost.UseUrls([.. bindings])
                .UseKestrel(options => options.AddServerHeader = false);
            builder.Services.AddSingleton(config);
            builder.Services.AddSingleton<IPlatformStore>(store);
            builder.Services.AddSingleton<HealthHandler>();
            BootstrapTelemetry.Add(builder.Services, config);
            await using var app = builder.Build();
            app.MapBootstrapHealth();
            if (config.UnsafeExposure)
                Console.Error.WriteLine("""{"level":"warning","eventCode":"unsafe_local_exposure","component":"host"}""");
            await app.RunAsync();
            return 0;
        }
        catch (BootstrapException error)
        {
            BootstrapDiagnostics.Failure(error.Code);
            return 1;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            InvalidOperationException or SqliteException or OperationCanceledException)
        {
            BootstrapDiagnostics.Failure(BootstrapError.StorageUnavailable);
            return 1;
        }
    }
}
