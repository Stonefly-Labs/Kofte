using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kofte.Infrastructure.Storage;

namespace Kofte.TestSupport.Identity;

public sealed class ServingIdentity : IDisposable
{
    private readonly OwnedTestRoot root = new();
    private readonly string environment;

    public ServingIdentity(string environment = "Development", OidcIssuer? issuer = null, Uri? publicOrigin = null)
    {
        this.environment = environment;
        try
        {
            if (OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
            Directory.CreateDirectory(root.Path, UnixStorePaths.DirectoryMode);
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            WebUrl = new("http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port);
            var oidc = environment != "Development";
            var value = new JsonObject
            {
                ["version"] = "1",
                ["mode"] = oidc ? "oidc" : "development",
                ["publicOrigin"] = oidc && publicOrigin is null
                    ? "https://kofte-test.invalid" : (publicOrigin ?? WebUrl).GetLeftPart(UriPartial.Authority),
                ["keyRingDirectory"] = KeyRingDirectory
            };
            if (oidc)
            {
                using var key = RSA.Create(2048);
                var request = new CertificateRequest("CN=Kofte test-only offline key protection",
                    key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5),
                    DateTimeOffset.UtcNow.AddDays(1));
                var certificateFile = Path.Combine(root.Path, "certificate.pem");
                var keyFile = Path.Combine(root.Path, "private-key.pem");
                var secretFile = Path.Combine(root.Path, "client-secret");
                PrivateTestFiles.WriteNew(certificateFile, Encoding.ASCII.GetBytes(certificate.ExportCertificatePem()));
                PrivateTestFiles.WriteNew(keyFile, Encoding.ASCII.GetBytes(key.ExportPkcs8PrivateKeyPem()));
                PrivateTestFiles.WriteNew(secretFile, Encoding.ASCII.GetBytes(issuer?.ClientSecret ??
                    Convert.ToHexString(RandomNumberGenerator.GetBytes(32))));
                value["keyEncryptionCertificateFile"] = certificateFile;
                value["keyEncryptionPrivateKeyFile"] = keyFile;
                value["provider"] = new JsonObject
                {
                    ["issuer"] = issuer?.Issuer.AbsoluteUri ?? "https://offline-issuer.invalid",
                    ["metadataAddress"] = issuer is null ? "https://offline-issuer.invalid/discovery" :
                        new Uri(issuer.Issuer, ".well-known/openid-configuration").AbsoluteUri,
                    ["authorizationEndpoint"] = issuer is null ? "https://offline-issuer.invalid/authorize" :
                        new Uri(issuer.Issuer, "authorize").AbsoluteUri,
                    ["tokenEndpoint"] = issuer is null ? "https://offline-issuer.invalid/token" :
                        new Uri(issuer.Issuer, "token").AbsoluteUri,
                    ["jwksUri"] = issuer is null ? "https://offline-issuer.invalid/keys" :
                        new Uri(issuer.Issuer, "jwks").AbsoluteUri,
                    ["clientId"] = issuer?.ClientId ?? "test-client",
                    ["clientSecretFile"] = secretFile,
                    ["allowedEndpoints"] = issuer is null
                        ? new JsonArray("https://offline-issuer.invalid/discovery", "https://offline-issuer.invalid/authorize",
                            "https://offline-issuer.invalid/token", "https://offline-issuer.invalid/keys")
                        : new JsonArray(new[] { ".well-known/openid-configuration", "authorize", "token", "jwks" }
                            .Select(path => (JsonNode?)JsonValue.Create(new Uri(issuer.Issuer, path).AbsoluteUri)).ToArray()),
                    ["allowedNetworks"] = new JsonArray(issuer is null ? "203.0.113.0/24" : "127.0.0.0/8")
                };
            }
            else value["development"] = new JsonObject
            {
                ["enabled"] = true,
                ["identities"] = new JsonArray(new JsonObject { ["key"] = "synthetic" })
            };
            PrivateTestFiles.WriteNew(ConfigurationPath, JsonSerializer.SerializeToUtf8Bytes(value));
        }
        catch { root.Dispose(); throw; }
    }

    public Uri WebUrl { get; }
    public string ConfigurationPath => Path.Combine(root.Path, "identity.json");
    public string KeyRingDirectory => Path.Combine(root.Path, "keys");

    public void ConfigureChild(ProcessStartInfo start)
    {
        start.Environment["KOFTE_AUTH_CONFIG_FILE"] = ConfigurationPath;
        start.Environment["KOFTE_WEB_URL"] = WebUrl.AbsoluteUri;
        start.Environment["DOTNET_ENVIRONMENT"] = environment;
        start.Environment["ASPNETCORE_ENVIRONMENT"] = environment;
    }

    public async Task PrepareAsync(Action<ProcessStartInfo> configure, CancellationToken token)
    {
        foreach (var arguments in new[] { new[] { "--registry", "upgrade" }, ["--identity", "prepare"] })
        {
            var start = new ProcessStartInfo("dotnet")
            {
                WorkingDirectory = OwnedProcesses.FindRepository(),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
            start.ArgumentList.Add(Path.Combine(OwnedProcesses.FindRepository(),
                "src", "Kofte.Host", "bin", configuration, "net10.0", "Kofte.Host.dll"));
            foreach (var argument in arguments) start.ArgumentList.Add(argument);
            OwnedProcesses.ClearSensitiveEnvironment(start);
            configure(start);
            ConfigureChild(start);
            var result = await OwnedProcesses.RunAsync(start, TimeSpan.FromSeconds(120), token);
            if (result.ExitCode != 0)
                throw new InvalidOperationException("test_identity_preparation_failed");
        }
    }

    public void Dispose() => root.Dispose();
}
