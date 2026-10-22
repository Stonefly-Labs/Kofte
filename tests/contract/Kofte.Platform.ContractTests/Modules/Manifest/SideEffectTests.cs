using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.ContractTests.Modules.Manifest;

public sealed class SideEffectTests
{
    [Fact]
    public async Task LocalObserversSeeNoResolutionActivationOrModuleDataChange()
    {
        ManifestValidator.Validate(ManifestCorpus.Minimal, new("1.0.0"));
        var directory = Path.Combine(Path.GetTempPath(), "kofte-manifest-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var sentinel = Path.Combine(directory, "platform-settings.txt");
        await File.WriteAllTextAsync(sentinel, "unchanged", TestContext.Current.CancellationToken);
        var original = SHA256.HashData(await File.ReadAllBytesAsync(sentinel, TestContext.Current.CancellationToken));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var activations = 0;
        AssemblyLoadEventHandler observer = (_, e) =>
        {
            if (e.LoadedAssembly.GetName().Name == "Kofte.Manifest.Consumer") activations++;
        };
        AppDomain.CurrentDomain.AssemblyLoad += observer;
        try
        {
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserExecute);
            foreach (var key in new[] { "$ref", "hooks", "type", "assembly", "schema", "connection", "credential" })
            {
                var bytes = ManifestCorpus.Change(n => n[key] = key == "$ref"
                    ? $"http://127.0.0.1:{port}/manifest" : directory);
                Assert.IsType<ManifestValidationResult.Rejected>(ManifestValidator.Validate(bytes, new("1.0.0")));
                var output = await ConsumerHarness.Author(bytes, "--host-contract", "1.0.0");
                Assert.Equal(2, output.ExitCode);
            }
            foreach (var key in new[] { "path", "schema", "file", "connection", "credential" })
            {
                var bytes = ManifestCorpus.Change(n => n["storage"]![key] = directory);
                Assert.IsType<ManifestValidationResult.Rejected>(ManifestValidator.Validate(bytes, new("1.0.0")));
            }
            Assert.False(listener.Pending());
            Assert.Equal(0, activations);
            Assert.Equal(new[] { sentinel }, Directory.GetFiles(directory, "*", SearchOption.AllDirectories));
            Assert.Equal(original, SHA256.HashData(await File.ReadAllBytesAsync(sentinel, TestContext.Current.CancellationToken)));
            var references = typeof(ManifestValidator).Assembly.GetReferencedAssemblies();
            Assert.DoesNotContain(references, a => a.Name!.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal)
                || a.Name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal)
                || a.Name.StartsWith("System.Net", StringComparison.Ordinal)
                || a.Name.StartsWith("System.IO", StringComparison.Ordinal)
                || a.Name == "Kofte.Host");
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyLoad -= observer;
            listener.Stop();
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            File.Delete(sentinel);
            Directory.Delete(directory);
        }
    }
}
