using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.Identity;
using Kofte.TestSupport.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Identity;
using Kofte.TestSupport;
using Microsoft.AspNetCore.DataProtection;
using Xunit;
using Paths = Kofte.Infrastructure.Storage.UnixStorePaths;

namespace Kofte.Host.Tests.Features.Identity;

[UnsupportedOSPlatform("windows")]
public sealed class IdentityKeysTests : IDisposable
{
    private readonly OwnedTestRoot root = new();
    private readonly Guid installationId = Guid.NewGuid();
    private string Ring => Path.Combine(root.Path, "keys");
    private string Marker => Path.Combine(Ring, "identity-owner.json");
    private string Data => Path.Combine(root.Path, "data");
    private CancellationToken Token => TestContext.Current.CancellationToken;

    public IdentityKeysTests()
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        Directory.CreateDirectory(root.Path, Paths.DirectoryMode);
        Directory.CreateDirectory(Data, Paths.DirectoryMode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task KeyProtectionCannotRestartAnExpiredOrCancelledEnclosingIdentityBudget(bool cancelled)
    {
        using var configuration = Configuration();
        await Prepare(configuration);
        using var keys = Open(configuration);
        var protector = keys.Protector("deadline");
        var ciphertext = protector.Protect("private-canary"u8.ToArray());
        var before = Snapshot();
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(Token);
        var records = new List<IdentityDiagnosticRecord>();
        using (var observation = IdentityDiagnostics.Begin(IdentityAction.Current, "development", "sqlite",
            cancelled ? TimeSpan.FromSeconds(1) : TimeSpan.FromMilliseconds(50), caller.Token, records.Add))
        {
            if (cancelled) caller.Cancel();
            else await Task.Delay(75, Token);
            var failure = Assert.ThrowsAny<OperationCanceledException>(() =>
                protector.Protect("new-ticket"u8.ToArray()));
            observation.Fail(failure);
            Assert.ThrowsAny<OperationCanceledException>(() => protector.Unprotect(ciphertext));
        }
        Assert.Equal(before, Snapshot());
        Assert.Equal(cancelled ? "cancelled" : "unavailable", Assert.Single(records).Outcome);
    }

    [Fact]
    public async Task PreparedRingSurvivesFreshInstancesAndSeparatesPurposesAndInstallations()
    {
        using var configuration = Configuration();
        await Prepare(configuration);
        byte[] ciphertext;
        using (var first = Open(configuration))
            ciphertext = first.Protector("session-v1").Protect("private-ticket"u8.ToArray());
        var before = Snapshot();
        using (var reopened = Open(configuration))
        {
            Assert.Equal("private-ticket"u8.ToArray(), reopened.Protector("session-v1").Unprotect(ciphertext));
            Assert.Throws<CryptographicException>(() => reopened.Protector("state-v1").Unprotect(ciphertext));
            Assert.Throws<CryptographicException>(() =>
                reopened.Provider.CreateProtector("session-v1").CreateProtector("nested").Unprotect(ciphertext));
            using var budget = Budget();
            reopened.Verify(budget);
        }
        Assert.Equal(before, Snapshot());
        Reject(() => Open(configuration, Guid.NewGuid()));
        Assert.Equal(before, Snapshot());
        Assert.Equal(Paths.DirectoryMode, File.GetUnixFileMode(Ring));
        Assert.All(Directory.GetFiles(Ring), file => Assert.Equal(Paths.FileMode, File.GetUnixFileMode(file)));
    }

    [Fact]
    public async Task OidcUsesActualCertificateEncryptionAndCannotFallBackWhenCertificateChanges()
    {
        WriteCertificate();
        using var configuration = Configuration(oidc: true);
        await Prepare(configuration);
        var encryptedXml = File.ReadAllText(Assert.Single(KeyFiles()));
        Assert.Contains("encryptedSecret", encryptedXml, StringComparison.Ordinal);
        Assert.DoesNotContain("<masterKey", encryptedXml, StringComparison.Ordinal);
        byte[] ciphertext;
        using (var first = Open(configuration))
            ciphertext = first.Protector("session").Protect("encrypted-ticket"u8.ToArray());
        using (var restarted = Open(configuration))
            Assert.Equal("encrypted-ticket"u8.ToArray(), restarted.Protector("session").Unprotect(ciphertext));
        var before = Snapshot();
        WriteCertificate(replace: true);
        using var wrongCertificate = Configuration(oidc: true);
        Reject(() => Open(wrongCertificate));
        Assert.Equal(before, Snapshot());
        await RejectPrepare(wrongCertificate);
        Assert.Equal(before, Snapshot());
    }

    [Fact]
    public async Task CertificateOverlapRetainsOldTicketsWhileNewKeysUseTheCurrentCertificate()
    {
        WriteCertificate();
        using var original = Configuration(oidc: true);
        await Prepare(original);
        var retiredKey = Assert.Single(KeyFiles());
        byte[] oldTicket;
        using (var first = Open(original))
            oldTicket = first.Protector("session").Protect("retained-ticket"u8.ToArray());
        var oldCertificate = Path.Combine(root.Path, "retained-certificate.pem");
        var oldPrivateKey = Path.Combine(root.Path, "retained-private-key.pem");
        PrivateTestFiles.WriteNew(oldCertificate, File.ReadAllBytes(Path.Combine(root.Path, "certificate.pem")));
        PrivateTestFiles.WriteNew(oldPrivateKey, File.ReadAllBytes(Path.Combine(root.Path, "private-key.pem")));
        AgeKey(DateTimeOffset.UtcNow.AddDays(-1));
        WriteCertificate(replace: true);
        using var overlap = Configuration(oidc: true, decryptors: new JsonArray(new JsonObject
        {
            ["certificateFile"] = oldCertificate,
            ["privateKeyFile"] = oldPrivateKey
        }));
        Assert.Equal(original.TrustDigest, overlap.TrustDigest);
        byte[] newTicket;
        using (var rotating = Open(overlap))
        {
            Assert.Equal("retained-ticket"u8.ToArray(), rotating.Protector("session").Unprotect(oldTicket));
            newTicket = rotating.Protector("session").Protect("new-ticket"u8.ToArray());
        }
        Assert.Equal(2, KeyFiles().Length);
        var generated = Assert.Single(KeyFiles(), file => file != retiredKey);
        Assert.Contains(XElement.Load(generated).Descendants(), element =>
            element.Name.LocalName == "X509Certificate" &&
            element.Value == Convert.ToBase64String(overlap.KeyEncryptionCertificate!.RawData));
        var before = Snapshot();
        using (var restarted = Open(overlap))
        {
            Assert.Equal("retained-ticket"u8.ToArray(), restarted.Protector("session").Unprotect(oldTicket));
            Assert.Equal("new-ticket"u8.ToArray(), restarted.Protector("session").Unprotect(newTicket));
        }
        using var withoutRetained = Configuration(oidc: true);
        Reject(() => Open(withoutRetained));
        Assert.Equal(before, Snapshot());
    }

    [Fact]
    public async Task OidcCannotAdoptADevelopmentRingOrACleartextRingWithEditedMode()
    {
        using var development = Configuration();
        await Prepare(development);
        WriteCertificate();
        using var production = Configuration(oidc: true);
        var before = Snapshot();
        Reject(() => Open(production));
        await RejectPrepare(production);
        Assert.Equal(before, Snapshot());
        var marker = ReadMarker();
        marker["mode"] = "oidc";
        marker["protection"] = "certificate";
        File.WriteAllText(Marker, marker.ToJsonString());
        Reject(() => Open(production));
    }

    [Fact]
    public void MissingOrUnexplainedDirectoriesNeverGetCreatedOrAdoptedOnServe()
    {
        using var configuration = Configuration();
        Reject(() => Open(configuration));
        Assert.False(Directory.Exists(Ring));
        Directory.CreateDirectory(Ring, Paths.DirectoryMode);
        PrivateTestFiles.WriteNew(Path.Combine(Ring, "unexplained"), "private-canary"u8.ToArray());
        var before = Snapshot();
        Reject(() => Open(configuration));
        Assert.Equal(before, Snapshot());
    }

    [Fact]
    public async Task PrepareRefusesAnExistingEvenEmptyUnownedDirectory()
    {
        using var configuration = Configuration();
        Directory.CreateDirectory(Ring, Paths.DirectoryMode);
        await RejectPrepare(configuration);
        Assert.Empty(Directory.GetFiles(Ring));
    }

    [Theory]
    [InlineData("key-missing")]
    [InlineData("marker-missing")]
    [InlineData("corrupt-key")]
    [InlineData("tampered-key")]
    [InlineData("extra-file")]
    [InlineData("foreign-installation")]
    [InlineData("foreign-purpose")]
    [InlineData("unknown-marker-field")]
    [InlineData("duplicate-marker-field")]
    [InlineData("oversized-marker")]
    [InlineData("directory-mode")]
    [InlineData("file-mode")]
    [InlineData("key-symlink")]
    [InlineData("marker-symlink")]
    [InlineData("key-hardlink")]
    [InlineData("pending-owner")]
    [InlineData("empty-pending-owner")]
    public async Task InvalidReadyRingsAreNeverRepairedEvenByPrepare(string damage)
    {
        using var configuration = Configuration();
        await Prepare(configuration);
        using var warmed = Open(configuration);
        var protector = warmed.Protector("session");
        var ciphertext = protector.Protect("private-canary"u8.ToArray());
        Damage(damage);
        var before = Snapshot();
        Reject(() => Open(configuration));
        using var budget = Budget();
        Reject(() => warmed.Verify(budget));
        Reject(() => protector.Protect("new-ticket"u8.ToArray()));
        Reject(() => protector.Unprotect(ciphertext));
        await RejectPrepare(configuration);
        Assert.Equal(before, Snapshot());
    }

    [Fact]
    public async Task ARemovedOrSymlinkedRingCannotBeRecreatedByAWarmedFrameworkProvider()
    {
        using var configuration = Configuration();
        await Prepare(configuration);
        using var warmed = Open(configuration);
        var protector = warmed.Provider.CreateProtector("cookie");
        _ = protector.Protect("ticket"u8.ToArray());
        var moved = Path.Combine(root.Path, "retained-keys");
        Directory.Move(Ring, moved);
        Reject(() => protector.Protect("new"u8.ToArray()));
        Assert.False(Directory.Exists(Ring));
        Directory.CreateSymbolicLink(Ring, moved);
        Reject(() => protector.Protect("new"u8.ToArray()));
        Reject(() => Open(configuration));
        Directory.Delete(Ring);
        Directory.Move(moved, Ring);
    }

    [Fact]
    public async Task PrepareIsIdempotentAndOnlyItsOwnedPreparingStateCanResume()
    {
        using var configuration = Configuration();
        await Prepare(configuration);
        var before = Snapshot();
        await Prepare(configuration);
        Assert.Equal(before, Snapshot());

        var marker = ReadMarker();
        marker["state"] = "preparing";
        marker["keys"] = new JsonObject();
        File.WriteAllText(Marker, marker.ToJsonString());
        Reject(() => Open(configuration));
        await Prepare(configuration);
        Assert.Equal(before, Snapshot());

        marker = ReadMarker();
        marker["state"] = "preparing";
        marker["keys"] = new JsonObject();
        File.WriteAllText(Marker, marker.ToJsonString());
        File.Delete(Assert.Single(KeyFiles()));
        Reject(() => Open(configuration));
        await Prepare(configuration);
        using var resumed = Open(configuration);
        Assert.Single(KeyFiles());
        Assert.NotEqual(before, Snapshot());
    }

    [Fact]
    public async Task PreparingStateCannotAdoptAKeyFromAnotherOwnedRing()
    {
        using var configuration = Configuration();
        await Prepare(configuration);
        var marker = ReadMarker();
        marker["state"] = "preparing";
        marker["keys"] = new JsonObject();
        File.WriteAllText(Marker, marker.ToJsonString());
        var keyFile = Assert.Single(KeyFiles());
        var xml = XElement.Load(keyFile);
        xml.SetAttributeValue("ringId", Guid.NewGuid().ToString("D"));
        File.WriteAllText(keyFile, xml.ToString());
        var before = Snapshot();
        await RejectPrepare(configuration);
        Assert.Equal(before, Snapshot());
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("foreign-ring")]
    [InlineData("foreign-installation")]
    [InlineData("wrong-key-hash")]
    [InlineData("invalid-json")]
    public async Task PreparingStateNeverDiscardsACompleteOrInvalidPendingOwner(string fault)
    {
        using var configuration = Configuration();
        await Prepare(configuration);
        var next = ReadMarker();
        var owner = ReadMarker();
        owner["state"] = "preparing";
        owner["keys"] = new JsonObject();
        File.WriteAllText(Marker, owner.ToJsonString());
        if (fault == "unknown") next["unexpected"] = true;
        if (fault == "foreign-ring") next["ringId"] = Guid.NewGuid().ToString("D");
        if (fault == "foreign-installation") next["installationId"] = Guid.NewGuid().ToString("D");
        if (fault == "wrong-key-hash") next["keys"]![Path.GetFileName(Assert.Single(KeyFiles()))] = new string('0', 64);
        PrivateTestFiles.WriteNew(Path.Combine(Ring, "identity-owner.next"),
            System.Text.Encoding.UTF8.GetBytes(fault == "invalid-json" ? "{\"version\":!" : next.ToJsonString()));
        var before = Snapshot();
        await RejectPrepare(configuration);
        Assert.Equal(before, Snapshot());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PreparingStateRefusesCleanupWhenTheOwnerOrPendingBytesChange(bool changeOwner)
    {
        using var configuration = Configuration();
        await Prepare(configuration);
        var owner = ReadMarker();
        owner["state"] = "preparing";
        File.WriteAllText(Marker, owner.ToJsonString());
        var pending = Path.Combine(Ring, "identity-owner.next");
        PrivateTestFiles.WriteNew(pending, "{"u8);
        string? changed = null;
        using var observation = IdentityDiagnostics.Begin(IdentityAction.Prepare, "development", "sqlite",
            TimeSpan.FromSeconds(10), Token, _ => { });
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kofte.Identity",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.OperationName != "identity.key_pending_incomplete" ||
                    !ReferenceEquals(IdentityDiagnostics.Current, observation)) return;
                File.AppendAllText(changeOwner ? Marker : pending, "\n");
                changed = Snapshot();
            }
        };
        ActivitySource.AddActivityListener(listener);
        observation.Fail(await RejectPrepare(configuration));
        Assert.NotNull(changed);
        Assert.Equal(changed, Snapshot());
    }

    [Fact]
    public async Task ForeignDisposedCancelledAndBusyStoppedAuthorityCannotProvision()
    {
        using var configuration = Configuration();
        var installation = StorageInstallation.Sqlite(Data);
        await using var scope = await MaintenanceScope.AcquireAsync(installation, Token);
        using var budget = Budget();
        await Assert.ThrowsAsync<StorageException>(() => IdentityKeys.PrepareAsync(configuration, installationId,
            StorageInstallation.Sqlite(Data), scope, budget));
        Assert.False(Directory.Exists(Ring));
        await scope.DisposeAsync();
        await Assert.ThrowsAsync<StorageException>(() => IdentityKeys.PrepareAsync(configuration, installationId,
            installation, scope, budget));
        Assert.False(Directory.Exists(Ring));
        using var cancelled = new CancellationTokenSource();
        await using var cancelledScope = await MaintenanceScope.AcquireAsync(installation, cancelled.Token);
        cancelled.Cancel();
        await Assert.ThrowsAsync<StorageException>(() => IdentityKeys.PrepareAsync(configuration, installationId,
            installation, cancelledScope, budget));
        Assert.False(Directory.Exists(Ring));
        using var acquisition = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            MaintenanceScope.AcquireAsync(installation, acquisition.Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExpiredUsableKeysAreRetainedAndRuntimeUsesNormalFrameworkRotation(bool oidc)
    {
        if (oidc) WriteCertificate();
        using var configuration = Configuration(oidc);
        await Prepare(configuration);
        byte[] oldTicket;
        using (var original = Open(configuration))
            oldTicket = original.Protector("session").Protect("original-ticket"u8.ToArray());
        AgeKey(DateTimeOffset.UtcNow.AddDays(-1));
        var before = Snapshot();
        using (var readOnlyStartup = Open(configuration))
        {
            using var budget = Budget();
            readOnlyStartup.Verify(budget);
            Assert.Equal(before, Snapshot());
            Assert.Equal("original-ticket"u8.ToArray(), readOnlyStartup.Protector("session").Unprotect(oldTicket));
            var newTicket = readOnlyStartup.Protector("session").Protect("new-ticket"u8.ToArray());
            Assert.Equal(2, KeyFiles().Length);
            using var restarted = Open(configuration);
            Assert.Equal("original-ticket"u8.ToArray(), restarted.Protector("session").Unprotect(oldTicket));
            Assert.Equal("new-ticket"u8.ToArray(), restarted.Protector("session").Unprotect(newTicket));
        }
        Assert.All(KeyFiles(), file => Assert.Equal(Paths.FileMode, File.GetUnixFileMode(file)));
    }

    [Fact]
    public async Task LossOfAnOldKeyIsRefusedEvenWhenAnotherUsableKeyRemains()
    {
        using var configuration = Configuration();
        await Prepare(configuration);
        var retired = Assert.Single(KeyFiles());
        AgeKey(DateTimeOffset.UtcNow.AddDays(-1));
        using (var active = Open(configuration))
            _ = active.Protector("session").Protect("new-ticket"u8.ToArray());
        Assert.Equal(2, KeyFiles().Length);
        File.Delete(retired);
        var before = Snapshot();
        Reject(() => Open(configuration));
        await RejectPrepare(configuration);
        Assert.Equal(before, Snapshot());
    }

    private void AgeKey(DateTimeOffset expiry) => IdentityKeyFixture.AgeRetainedKey(Ring, expiry);

    private void Damage(string damage)
    {
        var key = Assert.Single(KeyFiles());
        var marker = ReadMarker();
        switch (damage)
        {
            case "key-missing": File.Delete(key); return;
            case "marker-missing": File.Delete(Marker); return;
            case "corrupt-key": File.WriteAllText(key, "<malformed"); return;
            case "tampered-key": File.AppendAllText(key, " "); return;
            case "extra-file": PrivateTestFiles.WriteNew(Path.Combine(Ring, "extra"), [1]); return;
            case "pending-owner":
                PrivateTestFiles.WriteNew(Path.Combine(Ring, "identity-owner.next"), "{"u8); return;
            case "empty-pending-owner":
                PrivateTestFiles.WriteNew(Path.Combine(Ring, "identity-owner.next"), []); return;
            case "foreign-installation": marker["installationId"] = Guid.NewGuid().ToString("D"); break;
            case "foreign-purpose": marker["purpose"] = "different"; break;
            case "unknown-marker-field": marker["unknown"] = true; break;
            case "duplicate-marker-field":
                File.WriteAllText(Marker, File.ReadAllText(Marker)[..^1] + ",\"state\":\"ready\"}"); return;
            case "oversized-marker": File.WriteAllText(Marker, new string(' ', 65_537)); return;
            case "directory-mode": File.SetUnixFileMode(Ring, Paths.DirectoryMode | UnixFileMode.GroupRead); return;
            case "file-mode": File.SetUnixFileMode(key, Paths.FileMode | UnixFileMode.GroupRead); return;
            case "key-symlink":
            case "marker-symlink":
                var file = damage == "key-symlink" ? key : Marker;
                var retained = Path.Combine(root.Path, "retained");
                File.Move(file, retained);
                File.CreateSymbolicLink(file, retained);
                return;
            case "key-hardlink":
                var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ln")
                {
                    ArgumentList = { key, Path.Combine(root.Path, "hardlink") },
                    UseShellExecute = false
                })!;
                process.WaitForExit();
                Assert.Equal(0, process.ExitCode);
                process.Dispose();
                return;
            default: throw new InvalidOperationException("test_damage_invalid");
        }
        File.WriteAllText(Marker, marker.ToJsonString());
    }

    private async Task Prepare(IdentityConfiguration configuration)
    {
        var installation = StorageInstallation.Sqlite(Data);
        await using var scope = await MaintenanceScope.AcquireAsync(installation, Token);
        using var budget = Budget();
        using var prepared = await IdentityKeys.PrepareAsync(configuration, installationId, installation, scope, budget);
    }

    private async Task<InvalidOperationException> RejectPrepare(IdentityConfiguration configuration)
    {
        var installation = StorageInstallation.Sqlite(Data);
        await using var scope = await MaintenanceScope.AcquireAsync(installation, Token);
        using var budget = Budget();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            IdentityKeys.PrepareAsync(configuration, installationId, installation, scope, budget));
        Assert.Equal("identity_keys_invalid", error.Message);
        Assert.Null(error.InnerException);
        return error;
    }

    private static void Reject(Action operation)
    {
        var error = Assert.Throws<InvalidOperationException>(operation);
        Assert.Equal("identity_keys_invalid", error.Message);
        Assert.Null(error.InnerException);
    }

    private IdentityKeys Open(IdentityConfiguration configuration, Guid? id = null)
    {
        using var budget = Budget();
        return IdentityKeys.Open(configuration, id ?? installationId, budget);
    }

    private OperationBudget Budget() => new(OperationBudget.Resolve, Token);
    private JsonObject ReadMarker() => JsonNode.Parse(File.ReadAllText(Marker))!.AsObject();
    private string[] KeyFiles() => Directory.GetFiles(Ring, "key-*.xml");

    private string Snapshot() => string.Join("\n", Directory.GetFiles(Ring).Order(StringComparer.Ordinal)
        .Select(file => Path.GetFileName(file) + ":" + Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)))));

    private void WriteCertificate(bool replace = false)
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=Kofte test-only key encryption", key,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(1));
        Write("certificate.pem", certificate.ExportCertificatePem());
        Write("private-key.pem", key.ExportPkcs8PrivateKeyPem());
        if (!replace) Write("client-secret", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        return;

        void Write(string name, string text)
        {
            var path = Path.Combine(root.Path, name);
            if (replace) File.WriteAllText(path, text);
            else PrivateTestFiles.WriteNew(path, Encoding.ASCII.GetBytes(text));
        }
    }

    private IdentityConfiguration Configuration(bool oidc = false, JsonArray? decryptors = null)
    {
        var bootstrap = BootstrapConfiguration.Parse(new Dictionary<string, string?>
        {
            ["KOFTE_DATA_ROOT"] = Data
        }, root.Path);
        var value = new JsonObject
        {
            ["version"] = "1",
            ["mode"] = oidc ? "oidc" : "development",
            ["publicOrigin"] = oidc ? "https://kofte.invalid" : "http://127.0.0.1:3000",
            ["keyRingDirectory"] = Ring
        };
        if (oidc)
        {
            value["keyEncryptionCertificateFile"] = Path.Combine(root.Path, "certificate.pem");
            value["keyEncryptionPrivateKeyFile"] = Path.Combine(root.Path, "private-key.pem");
            if (decryptors is not null) value["keyDecryptionCertificates"] = decryptors;
            value["provider"] = new JsonObject
            {
                ["issuer"] = "https://offline-issuer.invalid",
                ["metadataAddress"] = "https://offline-issuer.invalid/discovery",
                ["authorizationEndpoint"] = "https://offline-issuer.invalid/authorize",
                ["tokenEndpoint"] = "https://offline-issuer.invalid/token",
                ["jwksUri"] = "https://offline-issuer.invalid/keys",
                ["clientId"] = "test-client",
                ["clientSecretFile"] = Path.Combine(root.Path, "client-secret"),
                ["allowedEndpoints"] = new JsonArray("https://offline-issuer.invalid/discovery",
                    "https://offline-issuer.invalid/authorize", "https://offline-issuer.invalid/token",
                    "https://offline-issuer.invalid/keys"),
                ["allowedNetworks"] = new JsonArray("203.0.113.0/24")
            };
        }
        else value["development"] = new JsonObject
        {
            ["enabled"] = true,
            ["identities"] = new JsonArray(new JsonObject { ["key"] = "synthetic" })
        };
        return IdentityConfiguration.Parse(JsonSerializer.SerializeToUtf8Bytes(value), bootstrap,
            oidc ? "Production" : "Development");
    }

    public void Dispose() => root.Dispose();
}
