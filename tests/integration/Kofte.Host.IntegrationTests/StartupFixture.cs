using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Kofte.Host.IntegrationTests;

public sealed class StartupFixture : IAsyncDisposable
{
    private readonly Process process;
    private readonly Task<string> output;
    private readonly Task<string> error;
    public HttpClient Client { get; }

    private StartupFixture(Process process, Uri address)
    {
        this.process = process;
        output = process.StandardOutput.ReadToEndAsync();
        error = process.StandardError.ReadToEndAsync();
        Client = new HttpClient { BaseAddress = address, Timeout = TimeSpan.FromSeconds(6) };
    }

    public static async Task<StartupFixture> StartAsync(string dataRoot, CancellationToken token,
        string environment = "Development")
    {
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Kofte.slnx")))
            repository = repository.Parent;
        if (repository is null) throw new InvalidOperationException("test_repository_missing");
        using var reservation = new TcpListener(IPAddress.Loopback, 0);
        reservation.Start();
        var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
        reservation.Stop();
        var address = new Uri($"http://127.0.0.1:{port}");
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = repository.FullName
        };
        start.ArgumentList.Add(Path.Combine(repository.FullName,
            "src", "Kofte.Host", "bin", configuration, "net10.0", "Kofte.Host.dll"));
        foreach (var key in start.Environment.Keys.Where(key =>
            key.StartsWith("KOFTE_", StringComparison.Ordinal) || key.StartsWith("OTEL_", StringComparison.Ordinal)).ToArray())
            start.Environment.Remove(key);
        start.Environment["KOFTE_DATA_ROOT"] = dataRoot;
        start.Environment["KOFTE_HOST_URL"] = address.AbsoluteUri;
        start.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
        start.Environment["Kestrel__Endpoints__Unapproved__Url"] = "http://203.0.113.1:0";
        var process = Process.Start(start) ?? throw new InvalidOperationException("test_host_start_failed");
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

    public async Task<string> SafeOutputAsync()
    {
        if (!process.HasExited) throw new InvalidOperationException("test_host_still_running");
        return await output + await error;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        if (!process.HasExited) process.Kill(entireProcessTree: true);
        await process.WaitForExitAsync();
        await output;
        await error;
        process.Dispose();
    }
}
