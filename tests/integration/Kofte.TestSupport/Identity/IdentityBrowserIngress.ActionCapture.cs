using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Kofte.Infrastructure.Storage;

namespace Kofte.TestSupport.Identity;

internal sealed partial class IdentityBrowserIngress
{
    internal const string ActionCaptureHeader = "X-Kofte-Test-Identity-Response";
    private readonly object actionCaptureGate = new();
    private readonly HashSet<string> actionCaptureFiles = new(StringComparer.Ordinal);
    private string? actionCaptureRoot;
    private int actionCaptureCount;
    private bool ActionCaptureEnabled { get { lock (actionCaptureGate) return actionCaptureRoot is not null; } }
    internal int ActionCaptureCount { get { lock (actionCaptureGate) return actionCaptureCount; } }

    internal void BeginActionCapture(string privateRoot)
    {
        UnixStorePaths.ValidateRoot(privateRoot, allowMissing: false);
        lock (actionCaptureGate)
        {
            if (actionCaptureRoot is not null ||
                Directory.EnumerateFileSystemEntries(privateRoot, "identity-action-*").Any())
                throw new InvalidOperationException("identity_browser_action_capture_scope_invalid");
            actionCaptureRoot = Path.GetFullPath(privateRoot);
            actionCaptureCount = 0;
            actionCaptureFiles.Clear();
        }
    }

    private async Task<string> CaptureActionResponseAsync(HttpResponseMessage response, CancellationToken token)
    {
        await response.Content.LoadIntoBufferAsync(65_536, token);
        var body = (await response.Content.ReadAsByteArrayAsync(token)).ToArray();
        byte[]? encoded = null;
        try
        {
            token.ThrowIfCancellationRequested();
            var encoding = response.Content.Headers.ContentEncoding.Count == 0 ? "identity" :
                string.Join(',', response.Content.Headers.ContentEncoding).ToLowerInvariant();
            if (body.Length > 65_536 || encoding is not ("identity" or "gzip" or "deflate" or "br"))
                throw new InvalidOperationException("identity_browser_action_capture_limit");
            var id = Guid.NewGuid().ToString("N");
            string root;
            lock (actionCaptureGate)
                root = actionCaptureRoot ?? throw new InvalidOperationException("identity_browser_action_capture_scope_invalid");
            encoded = JsonSerializer.SerializeToUtf8Bytes(new
            {
                version = 1,
                id,
                method = "POST",
                route = "identity-next-action",
                rootBinding = Convert.ToHexStringLower(SHA256.HashData(
                    Encoding.UTF8.GetBytes(Path.TrimEndingDirectorySeparator(root)))),
                status = (int)response.StatusCode,
                length = body.Length,
                sha256 = Convert.ToHexStringLower(SHA256.HashData(body)),
                contentEncoding = encoding,
                createdUnixMilliseconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                bodyBase64 = Convert.ToBase64String(body)
            }, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
            lock (actionCaptureGate)
            {
                if (actionCaptureRoot is null || actionCaptureCount >= 16 || encoded.Length > 90_000)
                    throw new InvalidOperationException("identity_browser_action_capture_scope_invalid");
                token.ThrowIfCancellationRequested();
                var path = Path.Combine(actionCaptureRoot, "identity-action-" + id + ".json");
                PrivateTestFiles.WriteNew(path, encoded);
                actionCaptureFiles.Add(path);
                actionCaptureCount++;
            }
            return id;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(body);
            if (encoded is not null) CryptographicOperations.ZeroMemory(encoded);
        }
    }

    internal void RequireActionCapturesConsumed(string privateRoot)
    {
        UnixStorePaths.ValidateRoot(privateRoot, allowMissing: false);
        lock (actionCaptureGate)
        {
            if (actionCaptureRoot != Path.GetFullPath(privateRoot) || actionCaptureCount == 0 ||
                Directory.EnumerateFileSystemEntries(privateRoot, "identity-action-*").Any())
                throw new InvalidOperationException("identity_browser_action_capture_incomplete");
            actionCaptureRoot = null;
            actionCaptureFiles.Clear();
        }
    }

    private void CleanupActionCaptures()
    {
        lock (actionCaptureGate)
        {
            if (actionCaptureRoot is not null && Directory.Exists(actionCaptureRoot))
            {
                UnixStorePaths.ValidateRoot(actionCaptureRoot, allowMissing: false);
                foreach (var path in actionCaptureFiles)
                    if (File.Exists(path))
                    {
                        UnixStorePaths.ValidateFile(path);
                        File.Delete(path);
                    }
            }
            actionCaptureRoot = null;
            actionCaptureFiles.Clear();
        }
    }
}
