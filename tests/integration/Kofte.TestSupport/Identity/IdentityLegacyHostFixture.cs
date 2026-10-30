using System.Diagnostics;
using Kofte.Infrastructure.Storage;

namespace Kofte.TestSupport.Identity;

public sealed class IdentityLegacyHostFixture : IAsyncDisposable
{
    public const string AcceptedRevision = "3e416813233c9d291eb20cf72d5882dbddf3e1b6";
    private static IdentityLegacyHostFixture? current;
    private readonly OwnedTestRoot root = new();
    private readonly SemaphoreSlim gate = new(1, 1);
    private string? host;

    public IdentityLegacyHostFixture()
    {
        if (Interlocked.CompareExchange(ref current, this, null) is not null)
            throw new InvalidOperationException("identity_legacy_host_fixture_preexists");
    }

    public static IdentityLegacyHostFixture Current => Volatile.Read(ref current)
        ?? throw new InvalidOperationException("identity_legacy_host_fixture_missing");

    public async Task PrepareAsync(CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            if (host is not null) return;
            if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
            Directory.CreateDirectory(root.Path, UnixStorePaths.DirectoryMode);
            var source = Path.Combine(root.Path, "accepted-source");
            Directory.CreateDirectory(source, UnixStorePaths.DirectoryMode);
            var archive = Path.Combine(root.Path, "accepted-source.tar");
            PrivateTestFiles.WriteNew(archive, []);
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
            budget.CancelAfter(TimeSpan.FromSeconds(120));
            await RequireAsync("git", OwnedProcesses.FindRepository(),
                ["archive", "--format=tar", "--output=" + archive, AcceptedRevision], budget.Token);
            await RequireAsync("tar", source, ["-xf", archive, "-C", source], budget.Token);
            File.Delete(archive);
            await RequireAsync("dotnet", source,
                ["build", "src/Kofte.Host/Kofte.Host.csproj", "-c", "Release", "-warnaserror",
                 "-p:RestoreLockedMode=true"], budget.Token);
            var binary = Path.Combine(source, "src/Kofte.Host/bin/Release/net10.0/Kofte.Host.dll");
            if (!File.Exists(binary)) throw new InvalidOperationException("identity_legacy_host_binary_missing");
            host = binary;
        }
        finally { gate.Release(); }
    }

    public async Task<ProcessOutput> RefuseAsync(StorageProviderScope scope, string[] arguments,
        CancellationToken token)
    {
        if (host is null) throw new InvalidOperationException("identity_legacy_host_binary_missing");
        var start = Start("dotnet", Path.GetDirectoryName(host)!, [host, .. arguments]);
        scope.ConfigureChild(start, offlineMaintenance: arguments.Length != 0);
        start.Environment.Remove("DOTNET_STARTUP_HOOKS");
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        start.Environment["DOTNET_ENVIRONMENT"] = "Production";
        return await OwnedProcesses.RunAsync(start, TimeSpan.FromSeconds(15), token);
    }

    private static ProcessStartInfo Start(string command, string directory, string[] arguments)
    {
        var start = new ProcessStartInfo(command)
        {
            WorkingDirectory = directory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        OwnedProcesses.ClearSensitiveEnvironment(start);
        start.Environment.Remove("DOTNET_STARTUP_HOOKS");
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        return start;
    }

    private static async Task RequireAsync(string command, string directory, string[] arguments,
        CancellationToken token)
    {
        var result = await OwnedProcesses.RunAsync(Start(command, directory, arguments),
            TimeSpan.FromSeconds(120), token);
        if (result.ExitCode != 0) throw new InvalidOperationException("identity_legacy_host_build_failed");
    }

    public ValueTask DisposeAsync()
    {
        root.Dispose();
        if (!ReferenceEquals(Interlocked.CompareExchange(ref current, null, this), this))
            throw new InvalidOperationException("identity_legacy_host_owner_changed");
        return ValueTask.CompletedTask;
    }
}
