using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Kofte.TestSupport;
using Kofte.TestSupport.Identity;

namespace Kofte.Host.IntegrationTests;

public sealed class StartupFixture : IAsyncDisposable
{
    private readonly OwnedProcess process;
    private Task? stop;
    private bool disposed;
    private readonly ServingIdentity identity;
    private readonly bool ownsIdentity;
    public HttpClient Client { get; }

    private StartupFixture(OwnedProcess process, Uri address, ServingIdentity identity, bool ownsIdentity)
    {
        this.process = process;
        this.identity = identity;
        this.ownsIdentity = ownsIdentity;
        Client = new HttpClient { BaseAddress = address, Timeout = TimeSpan.FromSeconds(6) };
    }

    public static async Task<StartupFixture> StartAsync(string dataRoot, CancellationToken token,
        string environment = "Development", string? postgres = null, string? modulesFile = null,
        ServingIdentity? preparedIdentity = null)
    {
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        var address = new Uri($"http://127.0.0.1:{port}");
        var identity = preparedIdentity ?? await PrepareIdentityAsync(dataRoot, token, environment, postgres);
        OwnedProcess process;
        try
        {
            var start = StartInfo(dataRoot, address, environment, postgres, modulesFile);
            identity.ConfigureChild(start);
            process = OwnedProcesses.Start(start);
        }
        catch { if (preparedIdentity is null) identity.Dispose(); throw; }
        var fixture = new StartupFixture(process, address, identity, preparedIdentity is null);
        try
        {
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
            budget.CancelAfter(TimeSpan.FromSeconds(20));
            while (!process.HasExited)
            {
                try
                {
                    using var response = await fixture.Client.GetAsync("/health/ready", budget.Token);
                    if (response.StatusCode == HttpStatusCode.OK) return fixture;
                }
                catch (HttpRequestException) { }
                await Task.Delay(50, budget.Token);
            }
            throw new InvalidOperationException("test_host_exited");
        }
        catch
        {
            await fixture.DisposeAsync();
            throw;
        }
    }

    public static async Task<ServingIdentity> PrepareIdentityAsync(string dataRoot, CancellationToken token,
        string environment = "Development", string? postgres = null)
    {
        var identity = new ServingIdentity(environment);
        try
        {
            await identity.PrepareAsync(start =>
            {
                var configured = StartInfo(dataRoot, new("http://127.0.0.1:5080"), environment, postgres);
                foreach (var (key, value) in configured.Environment) start.Environment[key] = value;
            }, token);
            return identity;
        }
        catch { identity.Dispose(); throw; }
    }

    private static ProcessStartInfo StartInfo(string dataRoot, Uri address, string environment, string? postgres,
        string? modulesFile = null)
    {
        var repository = OwnedProcesses.FindRepository();
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = repository
        };
        start.ArgumentList.Add(Path.Combine(repository,
            "src", "Kofte.Host", "bin", configuration, "net10.0", "Kofte.Host.dll"));
        OwnedProcesses.ClearSensitiveEnvironment(start);
        start.Environment["KOFTE_DATA_ROOT"] = dataRoot;
        start.Environment["KOFTE_HOST_URL"] = address.AbsoluteUri;
        start.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
        start.Environment["DOTNET_ENVIRONMENT"] = environment;
        if (modulesFile is not null) start.Environment["KOFTE_MODULES_FILE"] = modulesFile;
        if (postgres is not null)
        {
            start.Environment["KOFTE_PROFILE"] = "test-postgres";
            start.Environment["KOFTE_POSTGRES_CONNECTION"] = postgres;
            start.Environment["KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE"] = "true";
        }
        start.Environment["Kestrel__Endpoints__Unapproved__Url"] = "http://203.0.113.1:0";
        return start;
    }

    public static async Task<(int ExitCode, string Output)> RunExitAsync(string dataRoot,
        string[] args, CancellationToken token, string? postgres = null)
    {
        var start = StartInfo(dataRoot, new Uri("http://127.0.0.1:5080"), "Development", postgres);
        using var identity = args.Length == 0 ? new ServingIdentity() : null;
        identity?.ConfigureChild(start);
        foreach (var argument in args) start.ArgumentList.Add(argument);
        var result = await OwnedProcesses.RunAsync(start, TimeSpan.FromSeconds(10), token);
        return (result.ExitCode, result.Stdout + result.Stderr);
    }

    public async Task<ProcessOutput> SafeOutputAsync()
    {
        if (stop?.IsCompletedSuccessfully is not true && !process.HasExited)
            throw new InvalidOperationException("test_host_still_running");
        var result = await process.ResultAsync();
        return result;
    }

    public Task StopAsync() => stop ??= process.StopAsync(graceful: true);

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        await StopAsync();
        Client.Dispose();
        await process.DisposeAsync();
        if (ownsIdentity) identity.Dispose();
        disposed = true;
    }
}
