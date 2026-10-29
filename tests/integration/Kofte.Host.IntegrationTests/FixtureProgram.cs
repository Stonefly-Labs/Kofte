using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Kofte.Host.Bootstrap;
using Npgsql;

[assembly: Xunit.AssemblyFixture(typeof(Kofte.TestSupport.PostgresAssemblyFixture))]

namespace Kofte.Host.IntegrationTests;

public static class FixtureProgram
{
    public static async Task<int> Main(string[] args)
    {
        if (args.FirstOrDefault() == "--postgres-fixture")
            return await RunFixtureAsync(args);
        return args.Any(arg => arg is "--server" or "--internal-msbuild-node")
            ? await Xunit.MicrosoftTestingPlatform.TestPlatformTestFramework.RunAsync(args,
                SelfRegisteredExtensions.AddSelfRegisteredExtensions)
            : await Xunit.Runner.InProc.SystemConsole.ConsoleRunner.Run(args);
    }

    private static async Task<int> RunFixtureAsync(string[] args)
    {
        try
        {
            if (OperatingSystem.IsWindows() || args is not ["--postgres-fixture", var path] ||
                !Path.IsPathFullyQualified(path) || path != Path.GetFullPath(path))
                throw new InvalidOperationException("postgres_lease_path_invalid");
            UnixStorePaths.ValidateRoot(Path.GetDirectoryName(path)!, allowMissing: false);
            if (UnixStorePaths.ValidateFile(path, allowMissing: true))
                throw new InvalidOperationException("postgres_lease_exists");
            using var stop = new CancellationTokenSource();
            using var terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM,
                context => { context.Cancel = true; stop.Cancel(); });
            using var interrupt = PosixSignalRegistration.Create(PosixSignal.SIGINT,
                context => { context.Cancel = true; stop.Cancel(); });
            await using var fixture = await PostgresFixture.CreateFromEnvironmentAsync(stop.Token);
            await using (var file = new FileStream(path, new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                UnixCreateMode = UnixStorePaths.FileMode
            }))
                await JsonSerializer.SerializeAsync(file, new { fixture.RunId, fixture.ConnectionString },
                    cancellationToken: stop.Token);
            Console.WriteLine("""{"eventCode":"postgres_fixture_ready","component":"verification"}""");
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    var command = await Console.In.ReadLineAsync(stop.Token);
                    if (command is null or "" or "stop") break;
                    if (command is not ("pause" or "resume"))
                        throw new InvalidOperationException("postgres_fixture_command_invalid");
                    await fixture.SetAvailableAsync(command == "resume", stop.Token);
                    Console.WriteLine(JsonSerializer.Serialize(new
                    {
                        eventCode = command == "pause" ? "postgres_fixture_paused" : "postgres_fixture_resumed",
                        component = "verification"
                    }));
                }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
            return 0;
        }
        catch (Exception error) when (error is BootstrapException or NpgsqlException or IOException or
            UnauthorizedAccessException or InvalidOperationException or OperationCanceledException or
            System.ComponentModel.Win32Exception or ArgumentException)
        {
            Console.Error.WriteLine(JsonSerializer.Serialize(new
            {
                eventCode = "postgres_fixture_failed",
                component = "verification",
                correlationId = ActivityTraceId.CreateRandom().ToHexString()
            }));
            return 1;
        }
    }
}
