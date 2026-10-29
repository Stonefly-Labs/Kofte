using System.Text;
using System.Text.Json;
using Kofte.Infrastructure.Storage;

namespace Kofte.TestSupport.Identity;

public sealed partial class LoginCommitFixture
{
    private readonly HashSet<string> browserTransportCanaries = new(StringComparer.Ordinal);
    private readonly List<string> browserNativeOutput = [];
    private int browserNativeOutputBytes;
    private bool browserCanariesDelivered;
    private bool browserTransportEnabled;

    public int BrowserActionCaptureCount => browserIngress?.ActionCaptureCount ?? 0;

    public void BeginBrowserActionCapture(string privateRoot)
    {
        if (!browserTransportEnabled || browserIngress is null)
            throw new InvalidOperationException("identity_browser_action_capture_scope_invalid");
        browserIngress.BeginActionCapture(privateRoot);
    }

    private void ProtectBrowserTransportValues()
    {
        if (!browserTransportEnabled) return;
        string[] values =
        [
            issuer.ClientSecret, issuer.ClientId, issuer.Subject, issuer.Issuer.OriginalString,
            issuer.IssuedIdToken ?? "", issuer.DisplayName ?? "",
            .. cookies.GetAllCookies().Cast<System.Net.Cookie>().Select(cookie => cookie.Value)
        ];
        foreach (var value in values.Where(value => value.Length >= 16))
            browserTransportCanaries.Add(value);
    }

    public void RequireBrowserTransportPrivacy(string privateRoot, params string[] additionalCanaries)
    {
        if (!browserTransportEnabled || webWorker is null || browserCanariesDelivered)
            throw new InvalidOperationException("identity_browser_transport_scope_invalid");
        UnixStorePaths.ValidateRoot(privateRoot, allowMissing: false);
        browserIngress?.RequireActionCapturesConsumed(privateRoot);
        var path = Path.Combine(privateRoot, "identity-browser-canaries.json");
        UnixStorePaths.ValidateFile(path);
        if (new FileInfo(path).Length is 0 or > 65_536)
            throw new InvalidOperationException("identity_browser_transport_canaries_invalid");
        var bytes = File.ReadAllBytes(path);
        try
        {
            foreach (var value in IdentityBrowserTransportPrivacy.ReadCanaries(bytes))
                browserTransportCanaries.Add(value);
            foreach (var value in additionalCanaries.Where(value => value.Length >= 16))
                browserTransportCanaries.Add(value);
            ProtectBrowserTransportValues();
            browserCanariesDelivered = true;
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes);
            File.Delete(path);
        }
    }

    private async Task CaptureBrowserWorkerAsync()
    {
        if (!browserTransportEnabled || worker is null) return;
        await worker.StopAsync();
        var result = await worker.ResultAsync();
        var output = result.Stdout + "\n" + result.Stderr;
        browserNativeOutputBytes = checked(browserNativeOutputBytes + Encoding.UTF8.GetByteCount(output));
        if (browserNativeOutputBytes > 4_000_000)
            throw new InvalidOperationException("identity_browser_transport_capture_limit");
        browserNativeOutput.Add(output);
    }

    private void VerifyBrowserTransportPrivacy(string nextOutput, IReadOnlyList<string> ingressRecords)
    {
        try
        {
            if (!browserTransportEnabled) return;
            ProtectBrowserTransportValues();
            var native = string.Join('\n', OperationsDiagnosticLines.Concat(browserNativeOutput));
            string[] channels = browserIngress is null
                ? [nextOutput, native]
                : [nextOutput, string.Join('\n', ingressRecords), native];
            IdentityBrowserTransportPrivacy.RequireClean(
                channels, browserTransportCanaries, withIngress: browserIngress is not null);
        }
        finally
        {
            browserTransportCanaries.Clear();
            browserNativeOutput.Clear();
        }
    }
}

public static class IdentityBrowserTransportPrivacy
{
    public static IReadOnlyList<string> ReadCanaries(byte[] bytes)
    {
        if (bytes.Length is 0 or > 65_536) throw InvalidCanaries();
        try
        {
            using var document = JsonDocument.Parse(bytes, new()
            { AllowDuplicateProperties = false, MaxDepth = 3 });
            var root = document.RootElement;
            if (!root.EnumerateObject().Select(property => property.Name)
                    .Order(StringComparer.Ordinal).SequenceEqual(["canaries", "version"]) ||
                !root.GetProperty("version").TryGetInt32(out var version) || version != 1)
                throw InvalidCanaries();
            var values = root.GetProperty("canaries").EnumerateArray()
                .Select(value => value.GetString()).ToArray();
            if (values.Length is < 3 or > 256 || values.Any(value => value is not { Length: >= 16 and <= 8192 }) ||
                values.Distinct(StringComparer.Ordinal).Count() != values.Length)
                throw InvalidCanaries();
            return values.Select(value => value!).ToArray();
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or
            InvalidOperationException && error.Message != "identity_browser_transport_canaries_invalid")
        { throw InvalidCanaries(); }
    }

    public static void RequireClean(IReadOnlyList<string> channels, IReadOnlyCollection<string> canaries,
        bool withIngress = true)
    {
        if (channels.Count != (withIngress ? 3 : 2) || channels.Any(string.IsNullOrWhiteSpace) ||
            canaries.Count < 3 || canaries.Any(value => value.Length is < 16 or > 8192))
            throw new InvalidOperationException("identity_browser_transport_capture_missing");
        foreach (var channel in channels)
        {
            RequireNoDisclosure(channel, canaries);
            var detected = false;
            try
            {
                RequireNoDisclosure(channel + canaries.First(), canaries);
            }
            catch (InvalidOperationException error) when (error.Message == "identity_browser_transport_disclosure")
            { detected = true; }
            if (!detected) throw new InvalidOperationException("identity_browser_transport_scanner_failed");
        }
    }

    private static void RequireNoDisclosure(string channel, IReadOnlyCollection<string> canaries)
    {
        if (canaries.Any(value => channel.Contains(value, StringComparison.Ordinal) ||
            channel.Contains(Uri.EscapeDataString(value), StringComparison.Ordinal)))
            throw new InvalidOperationException("identity_browser_transport_disclosure");
    }

    private static InvalidOperationException InvalidCanaries() =>
        new("identity_browser_transport_canaries_invalid");
}
