using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using Kofte.TestSupport.Identity;
using Kofte.TestSupport;
using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

public abstract class IdentityStartupTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BothEntryPathsRequireExplicitPreparedIdentityAndServeReadOnlyWithoutDiscovery(bool composed)
    {
        await using var fixture = await IdentityFixture.CreateAsync(Provider, Token);
        await RefusedAsync(fixture, composed, "identity_configuration_invalid");
        Configure(fixture);
        await RefusedAsync(fixture, composed, "identity_prepare_required");
        await CommandAsync(fixture, "--registry", "upgrade");
        await RefusedAsync(fixture, composed, "identity_prepare_required");
        Assert.False(Directory.Exists(fixture.KeyRingDirectory));
        await CommandAsync(fixture, "--identity", "prepare");
        var before = await SnapshotAsync(fixture);
        await using (var process = fixture.Start([], composed: composed))
        {
            using var client = new HttpClient { BaseAddress = fixture.HostUrl };
            await ReadyAsync(client, process);
            using var unknown = await client.GetAsync("/not-an-operation", Token);
            Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
            using var method = await client.PostAsync("/health/live", null, Token);
            Assert.Equal(HttpStatusCode.MethodNotAllowed, method.StatusCode);
            await process.StopAsync();
            Assert.True(process.ExitVerified && process.DescendantExitsVerified);
        }
        Assert.Equal(before, await SnapshotAsync(fixture));
        Configure(fixture, "changed-synthetic");
        await RefusedAsync(fixture, composed, "identity_reconciliation_required");
        Assert.Equal(before, await SnapshotAsync(fixture));
        Configure(fixture);
        var marker = Path.Combine(fixture.KeyRingDirectory, "identity-restore-required");
        PrivateTestFiles.WriteNew(marker, "retained-quarantine"u8);
        var quarantined = await SnapshotAsync(fixture);
        await RefusedAsync(fixture, composed, "identity_reconciliation_required");
        Assert.Equal(quarantined, await SnapshotAsync(fixture));
        File.Delete(marker);
        Directory.Move(fixture.KeyRingDirectory, Path.Combine(fixture.PrivateDirectory, "retained-keys"));
        await RefusedAsync(fixture, composed, "identity_keys_invalid");
        Assert.False(Directory.Exists(fixture.KeyRingDirectory));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ProductionOidcStartupNeedsNoDiscoveryAndPreservesPreparedStorageAndEncryptedKeys(bool composed)
    {
        await using var fixture = await IdentityFixture.CreateAsync(Provider, Token);
        using var identity = new ServingIdentity("Production");
        await identity.PrepareAsync(start => fixture.Storage.ConfigureChild(start, offlineMaintenance: true), Token);
        fixture.WriteConfiguration(await File.ReadAllBytesAsync(identity.ConfigurationPath, Token));
        var before = await SnapshotAsync(fixture, identity.KeyRingDirectory);
        await using (var process = fixture.Start([], environment: "Production", composed: composed))
        {
            using var client = new HttpClient { BaseAddress = fixture.HostUrl };
            await ReadyAsync(client, process);
            using var openApi = await client.GetAsync("/openapi/v1.json", Token);
            Assert.Equal(HttpStatusCode.NotFound, openApi.StatusCode);
            await process.StopAsync();
            Assert.True(process.ExitVerified && process.DescendantExitsVerified);
        }
        Assert.Equal(before, await SnapshotAsync(fixture, identity.KeyRingDirectory));
    }

    private async Task ReadyAsync(HttpClient client, IdentityProcess process)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(Token);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        while (true)
        {
            Assert.False(process.HasExited, process.OutputSnapshot);
            try
            {
                using var response = await client.GetAsync("/health/ready", deadline.Token);
                if (response.StatusCode == HttpStatusCode.OK) return;
            }
            catch (HttpRequestException) { }
            await Task.Delay(25, deadline.Token);
        }
    }

    private async Task RefusedAsync(IdentityFixture fixture, bool composed, string code)
    {
        await using var process = fixture.Start([], composed: composed);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(Token);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        var output = await process.WaitAsync(deadline.Token);
        Assert.NotEqual(0, output.ExitCode);
        Assert.Contains("\"eventCode\":\"" + code + "\"", output.Stdout + output.Stderr, StringComparison.Ordinal);
        Assert.DoesNotContain(fixture.PrivateDirectory, output.Stdout + output.Stderr, StringComparison.Ordinal);
        Assert.True(process.ExitVerified && process.DescendantExitsVerified);
    }

    private async Task CommandAsync(IdentityFixture fixture, params string[] arguments)
    {
        await using var process = fixture.Start(arguments);
        Assert.Equal(0, (await process.WaitAsync(fixture.Storage.Token)).ExitCode);
    }

    private static void Configure(IdentityFixture fixture, string key = "synthetic") =>
        fixture.WriteConfiguration(JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = "1",
            mode = "development",
            publicOrigin = fixture.WebUrl.GetLeftPart(UriPartial.Authority),
            keyRingDirectory = fixture.KeyRingDirectory,
            development = new { enabled = true, identities = new[] { new { key } } }
        }));

    private async Task<string> SnapshotAsync(IdentityFixture fixture, string? keys = null)
    {
        var storage = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, Token);
        return storage + ":" + string.Join(":", Directory.GetFiles(keys ?? fixture.KeyRingDirectory)
            .Order(StringComparer.Ordinal).Select(path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)))));
    }
}

[Trait("Provider", "sqlite")]
[Collection("identity-startup")]
public sealed class SqliteIdentityStartupTests : IdentityStartupTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Collection("identity-startup")]
public sealed class PostgresIdentityStartupTests : IdentityStartupTests
{
    protected override string Provider => "postgres";
}

[CollectionDefinition("identity-startup", DisableParallelization = true)]
public sealed class IdentityStartupCollection;
