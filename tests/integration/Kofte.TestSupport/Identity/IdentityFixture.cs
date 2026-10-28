using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text.Json;
using Kofte.Infrastructure.Storage;

namespace Kofte.TestSupport.Identity;

public sealed class IdentityFixture : IAsyncDisposable
{
    private readonly OwnedTestRoot privateRoot = new();
    private readonly List<IdentityProcess> processes = [];
    private bool disposed;

    private IdentityFixture(StorageProviderScope storage)
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        Storage = storage;
        Directory.CreateDirectory(privateRoot.Path, UnixStorePaths.DirectoryMode);
        UnixStorePaths.ValidateRoot(privateRoot.Path, allowMissing: false);
        HostUrl = LoopbackAddress();
        do { WebUrl = LoopbackAddress(); } while (WebUrl.Port == HostUrl.Port);
    }

    public StorageProviderScope Storage { get; }
    public Uri HostUrl { get; }
    public Uri WebUrl { get; }
    public string PrivateDirectory => privateRoot.Path;
    public string ConfigurationPath => Path.Combine(privateRoot.Path, "identity.json");
    public string KeyRingDirectory => Path.Combine(privateRoot.Path, "keys");
    public string Canary { get; } = "identity-private-" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
    public bool AllExitsVerified => processes.All(item => item.ExitVerified && item.DescendantExitsVerified);

    public static async Task<IdentityFixture> CreateAsync(string provider, CancellationToken token)
    {
        var storage = await StorageProviderScope.CreateAsync(provider, token);
        try { return new(storage); }
        catch { await storage.DisposeAsync(); throw; }
    }

    public void WriteConfiguration(ReadOnlySpan<byte> bytes)
    {
        RequireStopped();
        if (bytes.Length is < 2 or > 32_768) throw new InvalidOperationException("identity_fixture_configuration_invalid");
        var temporary = ConfigurationPath + "." + Guid.NewGuid().ToString("N");
        PrivateTestFiles.WriteNew(temporary, bytes);
        File.Move(temporary, ConfigurationPath, overwrite: true);
        UnixStorePaths.ValidateFile(ConfigurationPath);
    }

    public IdentityProcess Start(IReadOnlyList<string> arguments, string environment = "Development",
        IdentityCommitBarrier? barrier = null, bool composed = false, Action<ProcessStartInfo>? configure = null)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Storage.Token.ThrowIfCancellationRequested();
        if (arguments.Count > 12 || arguments.Any(value => value.Length > 4096))
            throw new InvalidOperationException("identity_fixture_arguments_invalid");
        var admission = Path.Combine(privateRoot.Path, "admission-" + Guid.NewGuid().ToString("N") + ".json");
        PrivateTestFiles.WriteNew(admission, JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = 1,
            arguments
        }));
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = OwnedProcesses.FindRepository(),
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        var configuration = AppContext.BaseDirectory.Contains("/Release/", StringComparison.Ordinal) ? "Release" : "Debug";
        var consumer = Path.Combine(OwnedProcesses.FindRepository(), "tests", "contract", "Kofte.Identity.Consumer",
            "bin", configuration, "net10.0", "Kofte.Identity.Consumer.dll");
        if (!File.Exists(consumer)) throw new InvalidOperationException("identity_fixture_consumer_missing");
        start.ArgumentList.Add(consumer);
        start.ArgumentList.Add("--identity-fixture-worker");
        Storage.ConfigureChild(start, offlineMaintenance: arguments.FirstOrDefault() is "--identity" or "--registry");
        start.Environment["DOTNET_ENVIRONMENT"] = environment;
        start.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
        start.Environment["KOFTE_HOST_URL"] = HostUrl.AbsoluteUri;
        start.Environment["KOFTE_BACKEND_ORIGIN"] = HostUrl.AbsoluteUri;
        start.Environment["KOFTE_WEB_URL"] = WebUrl.AbsoluteUri;
        start.Environment["IDENTITY_TEST_ADMISSION_FILE"] = admission;
        if (File.Exists(ConfigurationPath)) start.Environment["KOFTE_AUTH_CONFIG_FILE"] = ConfigurationPath;
        if (composed)
        {
            var selection = Path.Combine(privateRoot.Path, "modules.json");
            if (!File.Exists(selection))
                PrivateTestFiles.WriteNew(selection, "{\"schemaVersion\":\"1\",\"modules\":[]}"u8);
            UnixStorePaths.ValidateFile(selection);
            start.Environment["KOFTE_MODULES_FILE"] = selection;
        }
        if (barrier is not null)
        {
            start.Environment["DOTNET_STARTUP_HOOKS"] = typeof(IdentityCommitBarrier).Assembly.Location;
            start.Environment["IDENTITY_TEST_BARRIER_FILE"] = barrier.DescriptorPath;
        }
        else
        {
            start.Environment.Remove("DOTNET_STARTUP_HOOKS");
            start.Environment.Remove("IDENTITY_TEST_BARRIER_FILE");
        }
        configure?.Invoke(start);
        var process = new IdentityProcess(OwnedProcesses.Start(start, captureOutput: false), Canary);
        processes.Add(process);
        return process;
    }

    public void RequireStopped()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (processes.Any(item => !item.HasExited))
            throw new InvalidOperationException("identity_fixture_requires_stopped_processes");
    }

    public IdentityProcess StartDevelopmentWeb()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        Storage.Token.ThrowIfCancellationRequested();
        var repository = OwnedProcesses.FindRepository();
        if (!File.Exists(Path.Combine(repository, "apps", "web", ".next", "BUILD_ID")))
            throw new InvalidOperationException("identity_fixture_web_build_missing");
        var start = new ProcessStartInfo("node")
        {
            WorkingDirectory = repository,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in new[]
        {
            Path.Combine(repository, "node_modules", "next", "dist", "bin", "next"),
            "start", Path.Combine(repository, "apps", "web"), "--hostname", "127.0.0.1",
            "--port", WebUrl.Port.ToString(System.Globalization.CultureInfo.InvariantCulture)
        })
            start.ArgumentList.Add(argument);
        OwnedProcesses.ClearSensitiveEnvironment(start);
        start.Environment["KOFTE_HOST_URL"] = HostUrl.GetLeftPart(UriPartial.Authority);
        start.Environment["KOFTE_BACKEND_ORIGIN"] = HostUrl.GetLeftPart(UriPartial.Authority);
        start.Environment["KOFTE_WEB_URL"] = WebUrl.GetLeftPart(UriPartial.Authority);
        start.Environment["KOFTE_AUTH_CONFIG_FILE"] = ConfigurationPath;
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        start.Environment["DOTNET_ENVIRONMENT"] = "Development";
        start.Environment["NEXT_TELEMETRY_DISABLED"] = "1";
        var process = new IdentityProcess(OwnedProcesses.Start(start, captureOutput: false), Canary);
        processes.Add(process);
        return process;
    }

    public async Task<string> PersistentSnapshotAsync(CancellationToken token, string? keys = null)
    {
        var storage = await CompositionStoreSnapshot.CaptureAsync(Storage, token);
        return storage + ":" + string.Join(":", Directory.GetFiles(keys ?? KeyRingDirectory)
            .Order(StringComparer.Ordinal).Select(path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))));
    }

    private static Uri LoopbackAddress()
    {
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        return new Uri($"http://127.0.0.1:{((IPEndPoint)socket.LocalEndpoint).Port}");
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        foreach (var process in processes.AsEnumerable().Reverse()) await process.DisposeAsync();
        if (!AllExitsVerified) throw new InvalidOperationException("identity_fixture_exit_unverified");
        await Storage.DisposeAsync();
        privateRoot.Dispose();
        disposed = true;
    }
}
