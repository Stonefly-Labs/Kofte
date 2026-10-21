using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Kofte.Host.IntegrationTests;

public sealed class StartupFixture : IAsyncDisposable
{
    private readonly Process process;
    private readonly Task<string> output;
    private readonly Task<string> error;
    private Task? stop;
    private bool disposed;
    public HttpClient Client { get; }

    private StartupFixture(Process process, Uri address)
    {
        this.process = process;
        output = process.StandardOutput.ReadToEndAsync();
        error = process.StandardError.ReadToEndAsync();
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
        var process = Process.Start(StartInfo(dataRoot, address, environment, postgres)) ??
            throw new InvalidOperationException("test_host_start_failed");
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
        var repository = new DirectoryInfo(AppContext.BaseDirectory);
        while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "Kofte.slnx")))
            repository = repository.Parent;
        if (repository is null) throw new InvalidOperationException("test_repository_missing");
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
        using var process = Process.Start(start) ?? throw new InvalidOperationException("test_host_start_failed");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromSeconds(10));
        try
        {
            await process.WaitForExitAsync(budget.Token);
            return (process.ExitCode, await output + await error);
        }
        finally
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
            await output;
            await error;
        }
    }

    public async Task<string> SafeOutputAsync()
    {
        if (stop?.IsCompletedSuccessfully is not true && !process.HasExited)
            throw new InvalidOperationException("test_host_still_running");
        return await output + await error;
    }

    public Task StopAsync() => stop ??= StopCoreAsync();

    private async Task StopCoreAsync()
    {
        if (!process.HasExited && Signal(process.Id, 15) != 0 && !process.HasExited)
            throw new InvalidOperationException("test_host_signal_failed");
        using var grace = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try { await process.WaitForExitAsync(grace.Token); }
        catch (OperationCanceledException) when (grace.IsCancellationRequested)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            using var forced = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await process.WaitForExitAsync(forced.Token);
        }
        await output;
        await error;
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        await StopAsync();
        Client.Dispose();
        process.Dispose();
        disposed = true;
    }

    [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static extern int Signal(int processId, int signal);
}
