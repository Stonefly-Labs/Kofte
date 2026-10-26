using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.TestSupport.Identity;

public static class StartupHook
{
    public static void Initialize()
    {
        try { IdentityCommitBarrier.InstallStartupHook(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            InvalidOperationException or JsonException or ArgumentException or KeyNotFoundException or StorePathException)
        {
            Console.Error.WriteLine("""{"eventCode":"identity_fixture_hook_refused","status":"failed"}""");
            Environment.Exit(42);
        }
    }
}
