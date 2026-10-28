using System.Diagnostics;
using System.Text.Json;
using Kofte.Infrastructure.Storage;

namespace Kofte.Identity.Consumer;

internal static class FixtureProgram
{
    internal static async Task<int> RunAsync()
    {
        try
        {
            var path = Environment.GetEnvironmentVariable("IDENTITY_TEST_ADMISSION_FILE") ??
                throw new InvalidOperationException("identity_fixture_admission_required");
            UnixStorePaths.ValidateRoot(Path.GetDirectoryName(path)!, allowMissing: false);
            UnixStorePaths.ValidateFile(path);
            var bytes = await File.ReadAllBytesAsync(path);
            if (bytes.Length > 8192) throw new InvalidOperationException("identity_fixture_admission_invalid");
            using var document = JsonDocument.Parse(bytes, new() { MaxDepth = 3, AllowDuplicateProperties = false });
            var value = document.RootElement;
            if (!value.EnumerateObject().Select(item => item.Name).ToHashSet(StringComparer.Ordinal)
                .SetEquals(["version", "arguments"]) || value.GetProperty("version").GetInt32() != 1 ||
                value.GetProperty("arguments").ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("identity_fixture_admission_invalid");
            var args = value.GetProperty("arguments").EnumerateArray()
                .Select(item => item.GetString() ?? throw new InvalidOperationException("identity_fixture_admission_invalid"))
                .ToArray();
            if (args.Length > 12 || args.Any(item => item.Length > 4096))
                throw new InvalidOperationException("identity_fixture_admission_invalid");
            File.Move(path, path + ".claimed", overwrite: false);
            Environment.SetEnvironmentVariable("IDENTITY_TEST_ADMISSION_FILE", null);
            var barrier = Environment.GetEnvironmentVariable("IDENTITY_TEST_BARRIER_FILE");
            if (barrier is not null && File.Exists(barrier + ".claimed"))
            {
                Environment.SetEnvironmentVariable("IDENTITY_TEST_BARRIER_FILE", null);
                Environment.SetEnvironmentVariable("DOTNET_STARTUP_HOOKS", null);
            }
            using var diagnostics = new DiagnosticListener("Kofte.Identity");
            diagnostics.Write("identity.phase", new { phase = "fixture_ready" });
            if (args is ["--fixture-probe"]) return 0;
            if (args is ["--fixture-idle"])
            {
                await Task.Delay(TimeSpan.FromSeconds(100));
                return 0;
            }
            if (args is ["--fixture-login-bridge"])
                return await Kofte.TestSupport.Identity.LoginCommitFixture.RunExistingChildAsync();
            if (args is ["--fixture-product-login"])
                return await Kofte.TestSupport.Identity.LoginCommitFixture.RunExistingChildAsync(
                    productRoutes: true, actorProbe: ActorProbe.Observe);
            return await BootstrapProgram.RunAsync(args);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            InvalidOperationException or JsonException or ArgumentException or StorePathException)
        {
            Console.Error.WriteLine("""{"eventCode":"identity_fixture_failed","status":"failed"}""");
            return 1;
        }
    }
}
