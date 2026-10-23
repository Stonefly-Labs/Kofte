using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Kofte.TestSupport;

namespace Kofte.Host.IntegrationTests;

public sealed class StartupFixture : IAsyncDisposable
{
    private readonly OwnedProcess process;
    private Task? stop;
    private bool disposed;
    public HttpClient Client { get; }

    private StartupFixture(OwnedProcess process, Uri address)
    {
        this.process = process;
        Client = new HttpClient { BaseAddress = address, Timeout = TimeSpan.FromSeconds(6) };
    }

    public static async Task<StartupFixture> StartAsync(string dataRoot, CancellationToken token,
        string environment = "Development", string? postgres = null)
    {
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        var address = new Uri($"http://127.0.0.1:{port}");
        var process = OwnedProcesses.Start(StartInfo(dataRoot, address, environment, postgres));
        var fixture = new StartupFixture(process, address);
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

    private static ProcessStartInfo StartInfo(string dataRoot, Uri address, string environment, string? postgres)
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
        foreach (var argument in args) start.ArgumentList.Add(argument);
        var result = await OwnedProcesses.RunAsync(start, TimeSpan.FromSeconds(10), token);
        return (result.ExitCode, result.Stdout + result.Stderr);
    }

    public async Task<string> SafeOutputAsync()
    {
        if (stop?.IsCompletedSuccessfully is not true && !process.HasExited)
            throw new InvalidOperationException("test_host_still_running");
        var result = await process.ResultAsync();
        return result.Stdout + result.Stderr;
    }

    public Task StopAsync() => stop ??= process.StopAsync(graceful: true);

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        await StopAsync();
        Client.Dispose();
        await process.DisposeAsync();
        disposed = true;
    }
}
