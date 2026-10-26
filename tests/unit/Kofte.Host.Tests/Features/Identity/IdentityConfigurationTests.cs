using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.TestSupport;
using Xunit;
using Paths = Kofte.Infrastructure.Storage.UnixStorePaths;

namespace Kofte.Host.Tests.Features.Identity;

public sealed class IdentityConfigurationTests : IDisposable
{
    private readonly OwnedTestRoot root = new();
    private string AuthFile => Path.Combine(root.Path, "identity.json");
    private string SecretFile => Path.Combine(root.Path, "secret");
    private string CertificateFile => Path.Combine(root.Path, "certificate.pem");
    private string PrivateKeyFile => Path.Combine(root.Path, "key.pem");
    private BootstrapConfiguration Bootstrap => BootstrapConfiguration.Parse(new Dictionary<string, string?>
    {
        ["KOFTE_DATA_ROOT"] = Path.Combine(root.Path, "data"),
        ["KOFTE_AUTH_CONFIG_FILE"] = AuthFile
    }, root.Path);

    public IdentityConfigurationTests()
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        Directory.CreateDirectory(root.Path, Paths.DirectoryMode);
    }

    [Fact]
    public void MissingModeOrConfigurationNeverSelectsAnonymousOrDevelopment()
    {
        var configuration = Development();
        configuration.Remove("mode");
        Reject(configuration);
        Assert.Throws<IdentityConfigurationException>(() => IdentityConfiguration.Load(
            BootstrapConfiguration.Parse(new Dictionary<string, string?>(), root.Path), "Development"));
    }

    [Theory]
    [InlineData("version", "\"2\"")]
    [InlineData("mode", "\"anonymous\"")]
    [InlineData("mode", "\"Development\"")]
    [InlineData("publicOrigin", "\"http://example.invalid\"")]
    [InlineData("publicOrigin", "\"http://127.0.0.1:3000/\"")]
    [InlineData("publicOrigin", "\"http://127.0.0.1:3000?x=y\"")]
    [InlineData("publicOrigin", "\"http://user@127.0.0.1:3000\"")]
    [InlineData("keyRingDirectory", "\"relative/keys\"")]
    [InlineData("keyRingDirectory", "\"/tmp/../keys\"")]
    [InlineData("skipTls", "true")]
    [InlineData("provider", "{}")]
    [InlineData("development", "null")]
    public void UnknownDuplicateWrongTypedAndUntrustedFieldsAreRejected(string field, string json)
    {
        var configuration = Development();
        configuration[field] = JsonNode.Parse(json);
        Reject(configuration);
    }

    [Fact]
    public void DuplicateFieldsAtEveryObjectLevelAndInvalidUtf8AreRefused()
    {
        var json = Development().ToJsonString();
        RejectBytes(Encoding.UTF8.GetBytes(json[..^1] + ",\"mode\":\"development\"}"));
        RejectBytes(Encoding.UTF8.GetBytes(json.Replace("\"enabled\":true",
            "\"enabled\":true,\"enabled\":true", StringComparison.Ordinal)));
        RejectBytes([0xc3, 0x28]);
        RejectBytes(Encoding.UTF8.GetBytes("[]"));
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("development")]
    public void DevelopmentRequiresTheExplicitDevelopmentEnvironment(string environment) =>
        Reject(Development(), environment);

    [Fact]
    public void UnsafeExposureCannotOverrideAnyInternalLoopbackBoundary()
    {
        var bootstrap = Bootstrap with { HostUrl = new("http://0.0.0.0:5080"), UnsafeExposure = true };
        Reject(Development(), bootstrap: bootstrap);
        Reject(Development(), bootstrap: Bootstrap with { WebUrl = new("http://example.invalid:3000") });
        Reject(Development(), bootstrap: Bootstrap with { BackendOrigin = new("http://example.invalid:5080") });
    }

    [Fact]
    public void KeyRingCannotOverlapEitherDirectionWithTheManagedRecoveryRoot()
    {
        var configuration = Development();
        configuration["keyRingDirectory"] = Bootstrap.DataRoot;
        Reject(configuration);
        configuration["keyRingDirectory"] = Path.Combine(Bootstrap.DataRoot, "keys");
        Reject(configuration);
        configuration["keyRingDirectory"] = root.Path;
        Reject(configuration);
    }

    [Fact]
    public void DevelopmentOptInIdentityCountsUnknownFieldsAndExactKeysAreEnforced()
    {
        var configuration = Development();
        configuration["development"]!["enabled"] = false;
        Reject(configuration);
        configuration = Development();
        configuration["development"]!["administrator"] = true;
        Reject(configuration);
        foreach (var count in new[] { 0, 9 })
        {
            configuration = Development();
            configuration["development"]!["identities"] = new JsonArray(Enumerable.Range(0, count)
                .Select(index => (JsonNode)new JsonObject { ["key"] = $"synthetic-{index}", ["displayName"] = null }).ToArray());
            Reject(configuration);
        }
        configuration = Development();
        configuration["development"]!["identities"]!.AsArray().Add(new JsonObject
        {
            ["key"] = "synthetic",
            ["displayName"] = "Different display"
        });
        Reject(configuration);
        configuration = Development();
        configuration["development"]!["identities"]![0]!["key"] = new string('a', 256);
        Reject(configuration);
        configuration["development"]!["identities"]![0]!["key"] = "synthetic\n";
        Reject(configuration);
    }

    [Fact]
    public void DisplayLimitsCountUnicodeScalarsAndNeverBecomeIdentityKeys()
    {
        var configuration = Development();
        var display = string.Concat(Enumerable.Repeat("\U0001f331", 128));
        configuration["development"]!["identities"]![0]!["displayName"] = display;
        using var first = Parse(configuration);
        configuration["development"]!["identities"]![0]!["displayName"] = "Changed display";
        using var changed = Parse(configuration);
        Assert.Equal(first.TrustDigest, changed.TrustDigest);
        configuration["development"]!["identities"]![0]!["displayName"] = display + "\U0001f331";
        Reject(configuration);
        configuration["development"]!["identities"]![0]!["displayName"] = "unsafe\n";
        Reject(configuration);
    }

    [Fact]
    public void ConfigurationSizeIsBoundedBeforeParsingOrReadingPastTheLimit()
    {
        var bytes = Encoding.UTF8.GetBytes(Development().ToJsonString());
        var atLimit = bytes.Concat(Enumerable.Repeat((byte)' ', 32_768 - bytes.Length)).ToArray();
        using var valid = IdentityConfiguration.Parse(atLimit, Bootstrap, "Development");
        PrivateTestFiles.WriteNew(AuthFile, atLimit);
        using var loaded = IdentityConfiguration.Load(Bootstrap, "Development");
        Assert.Equal(valid.TrustDigest, loaded.TrustDigest);
        RejectBytes(atLimit.Append((byte)' ').ToArray());
        File.AppendAllText(AuthFile, " ");
        Assert.Throws<IdentityConfigurationException>(() => IdentityConfiguration.Load(Bootstrap, "Development"));
    }

    [Fact]
    public void OidcValidationIsOfflineAndTrustDigestExcludesRotatableSecretsButNotTrust()
    {
        var configuration = Oidc();
        using var original = Parse(configuration, "Production");
        File.WriteAllText(SecretFile, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        using var rotated = Parse(configuration, "Production");
        Assert.Equal(original.TrustDigest, rotated.TrustDigest);
        foreach (var field in new[] { "issuer", "clientId" })
        {
            var changed = configuration.DeepClone().AsObject();
            changed["provider"]![field] = configuration["provider"]![field]!.GetValue<string>() + "different";
            using var parsed = Parse(changed, "Production");
            Assert.NotEqual(original.TrustDigest, parsed.TrustDigest);
        }
        configuration["publicOrigin"] = "https://different.invalid";
        using var changedOrigin = Parse(configuration, "Production");
        Assert.NotEqual(original.TrustDigest, changedOrigin.TrustDigest);
    }

    [Theory]
    [InlineData("issuer", "http://offline-issuer.invalid")]
    [InlineData("metadataAddress", "http://offline-issuer.invalid/discovery")]
    [InlineData("authorizationEndpoint", "https://user@offline-issuer.invalid/authorize")]
    [InlineData("tokenEndpoint", "https://offline-issuer.invalid/token#fragment")]
    [InlineData("jwksUri", "https://169.254.169.254/keys")]
    [InlineData("skipTls", "true")]
    public void OidcEndpointsAndFieldsCannotRelaxConfiguredTrust(string field, string value)
    {
        var configuration = Oidc();
        configuration["provider"]![field] = field == "skipTls" ? JsonValue.Create(true) : JsonValue.Create(value);
        Reject(configuration, "Production");
    }

    [Fact]
    public void OidcRequiresTheWholeExplicitEndpointAndNetworkAllowlist()
    {
        var configuration = Oidc();
        configuration["provider"]!["allowedEndpoints"] = new JsonArray("https://offline-issuer.invalid/token");
        Reject(configuration, "Production");
        configuration = OidcDocument();
        configuration["provider"]!["allowedNetworks"] = new JsonArray("any");
        Reject(configuration, "Production");
        configuration = OidcDocument();
        configuration["development"] = new JsonObject { ["enabled"] = true };
        Reject(configuration, "Production");
    }

    [Fact]
    public void ClientSecretAtLimitIsAcceptedAndOneOverIsRefusedWithoutLoggingIt()
    {
        var configuration = Oidc();
        File.WriteAllText(SecretFile, new string('x', 4096));
        using var valid = Parse(configuration, "Production");
        File.AppendAllText(SecretFile, "x");
        var error = Reject(configuration, "Production");
        Assert.DoesNotContain(new string('x', 32), error.Message);
    }

    [Fact]
    public void CertificateAndKeyMustActuallyMatchAndRemainBounded()
    {
        var configuration = Oidc();
        var original = File.ReadAllText(CertificateFile);
        File.WriteAllText(CertificateFile, original.PadRight(65_536));
        using var atLimit = Parse(configuration, "Production");
        File.AppendAllText(CertificateFile, " ");
        Reject(configuration, "Production");
        File.WriteAllText(CertificateFile, original);
        using var other = RSA.Create(2048);
        File.WriteAllText(PrivateKeyFile, other.ExportPkcs8PrivateKeyPem());
        Reject(configuration, "Production");
    }

    [Fact]
    public void PrivateReferencesRejectSymlinksSharedPermissionsDirectoriesAndMissingFiles()
    {
        var configuration = Oidc();
        var link = Path.Combine(root.Path, "secret-link");
        File.CreateSymbolicLink(link, SecretFile);
        configuration["provider"]!["clientSecretFile"] = link;
        Reject(configuration, "Production");
        configuration["provider"]!["clientSecretFile"] = SecretFile;
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        File.SetUnixFileMode(SecretFile, Paths.FileMode | UnixFileMode.GroupRead);
        Reject(configuration, "Production");
        File.SetUnixFileMode(SecretFile, Paths.FileMode);
        configuration["provider"]!["clientSecretFile"] = root.Path;
        Reject(configuration, "Production");
        configuration["provider"]!["clientSecretFile"] = Path.Combine(root.Path, "missing");
        Reject(configuration, "Production");
    }

    [Fact]
    public void RevocationCanValidateItsSubsetWhenServingSecretsHaveBeenLost()
    {
        var configuration = Oidc();
        using var before = Parse(configuration, "Production");
        File.Delete(SecretFile);
        File.Delete(CertificateFile);
        File.Delete(PrivateKeyFile);
        Reject(configuration, "Production");
        using var stopped = IdentityConfiguration.Parse(JsonSerializer.SerializeToUtf8Bytes(configuration),
            Bootstrap, "Production", IdentityOperation.RevokeSessions);
        Assert.Equal(before.TrustDigest, stopped.TrustDigest);
    }

    [Fact]
    public void BootstrapAuthorizationRequiresMatchingProtectionKeysButNotTheProviderSecret()
    {
        var configuration = Oidc();
        File.Delete(SecretFile);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(configuration);
        using var valid = IdentityConfiguration.Parse(bytes, Bootstrap, "Production",
            IdentityOperation.BootstrapAuthorize);
        using var other = RSA.Create(2048);
        File.WriteAllText(PrivateKeyFile, other.ExportPkcs8PrivateKeyPem());
        Assert.Throws<IdentityConfigurationException>(() => IdentityConfiguration.Parse(bytes,
            Bootstrap, "Production", IdentityOperation.BootstrapAuthorize));
        File.Delete(CertificateFile);
        Assert.Throws<IdentityConfigurationException>(() => IdentityConfiguration.Parse(bytes,
            Bootstrap, "Production", IdentityOperation.BootstrapAuthorize));
    }

    [Fact]
    public void OnlyTheSingleNewBootstrapEnvironmentKeyIsAdmitted()
    {
        _ = Bootstrap;
        Assert.Throws<BootstrapException>(() => BootstrapConfiguration.Parse(
            new Dictionary<string, string?> { ["KOFTE_AUTH_SKIP_TLS"] = "true" }, root.Path));
    }

    private JsonObject Development() => new()
    {
        ["version"] = "1",
        ["mode"] = "development",
        ["publicOrigin"] = "http://127.0.0.1:3000",
        ["keyRingDirectory"] = Path.Combine(root.Path, "keys"),
        ["development"] = new JsonObject
        {
            ["enabled"] = true,
            ["identities"] = new JsonArray(new JsonObject { ["key"] = "synthetic", ["displayName"] = null })
        }
    };

    private JsonObject Oidc()
    {
        PrivateTestFiles.WriteNew(SecretFile, Encoding.UTF8.GetBytes(
            Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))));
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=Kofte test-only key encryption", key,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(1));
        PrivateTestFiles.WriteNew(CertificateFile, Encoding.ASCII.GetBytes(certificate.ExportCertificatePem()));
        PrivateTestFiles.WriteNew(PrivateKeyFile, Encoding.ASCII.GetBytes(key.ExportPkcs8PrivateKeyPem()));
        return OidcDocument();
    }

    private JsonObject OidcDocument() => new()
    {
        ["version"] = "1",
        ["mode"] = "oidc",
        ["publicOrigin"] = "https://kofte.invalid",
        ["keyRingDirectory"] = Path.Combine(root.Path, "keys"),
        ["keyEncryptionCertificateFile"] = CertificateFile,
        ["keyEncryptionPrivateKeyFile"] = PrivateKeyFile,
        ["provider"] = new JsonObject
        {
            ["issuer"] = "https://offline-issuer.invalid",
            ["metadataAddress"] = "https://offline-issuer.invalid/discovery",
            ["authorizationEndpoint"] = "https://offline-issuer.invalid/authorize",
            ["tokenEndpoint"] = "https://offline-issuer.invalid/token",
            ["jwksUri"] = "https://offline-issuer.invalid/keys",
            ["clientId"] = "synthetic-client",
            ["clientSecretFile"] = SecretFile,
            ["allowedEndpoints"] = new JsonArray("https://offline-issuer.invalid/discovery",
                "https://offline-issuer.invalid/authorize", "https://offline-issuer.invalid/token",
                "https://offline-issuer.invalid/keys"),
            ["allowedNetworks"] = new JsonArray("203.0.113.0/24")
        }
    };

    private IdentityConfiguration Parse(JsonObject value, string environment = "Development") =>
        IdentityConfiguration.Parse(JsonSerializer.SerializeToUtf8Bytes(value), Bootstrap, environment);

    private IdentityConfigurationException Reject(JsonObject value, string environment = "Development",
        BootstrapConfiguration? bootstrap = null) =>
        RejectBytes(JsonSerializer.SerializeToUtf8Bytes(value), environment, bootstrap);

    private IdentityConfigurationException RejectBytes(byte[] bytes, string environment = "Development",
        BootstrapConfiguration? bootstrap = null)
    {
        var error = Assert.Throws<IdentityConfigurationException>(() =>
            IdentityConfiguration.Parse(bytes, bootstrap ?? Bootstrap, environment));
        Assert.StartsWith("identity_configuration_", error.Code, StringComparison.Ordinal);
        Assert.DoesNotContain(root.Path, error.Message, StringComparison.Ordinal);
        Assert.Null(error.InnerException);
        return error;
    }

    public void Dispose() => root.Dispose();
}
