using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Modules.Alpha;
using Kofte.Storage.IntegrationTests.Providers;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Capture = Kofte.Storage.IntegrationTests.Providers.StorageMaintenanceDiagnosticsTests.Capture;

namespace Kofte.Storage.IntegrationTests.Recovery;

[Collection("Storage telemetry isolation")]
public abstract class RecoveryDiagnosticsTests
{
    protected abstract string Provider { get; }
    private const string Canary = "/private/recovery-credential-payload\n\tException";

    [Theory]
    [InlineData("non_owned")]
    [InlineData("foreign")]
    [InlineData("disposed")]
    public async Task InspectionScopeRefusalsEmitCorrelatedRecordsAndPreserveTheInstallation(string kind)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync(Provider, token);
        await using var owned = await fixture.Maintenance.AcquireAsync(token);
        var before = await RecoverySetVerifier.InspectStoppedAsync(fixture.Installation, owned, token);
        var installation = kind == "foreign" ? fixture.NewInstallation() : fixture.Installation;
        IStorageMaintenanceScope scope = kind == "non_owned" ? new UnownedScope() : owned;
        if (kind == "disposed") await owned.DisposeAsync();
        using (var capture = new Capture())
        {
            var error = await Assert.ThrowsAsync<StorageException>(() =>
                RecoverySetVerifier.InspectStoppedAsync(installation, scope, token));
            Assert.Equal(StorageCode.RequestInvalid, error.Code);
            capture.AssertRecords(fixture, "recover",
                ("storage_request_invalid", "rejected", "correct_configuration", "unchanged"));
        }
        if (kind != "disposed") await owned.DisposeAsync();
        await using var reopened = await fixture.Maintenance.AcquireAsync(token);
        Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(
            await RecoverySetVerifier.InspectStoppedAsync(fixture.Installation, reopened, token)));
        await fixture.AssertInventoryAsync(0, 0, token);
    }

    [Fact]
    public async Task CompleteRealRecoverySetsAndRefusedBoundariesEmitPrivateCorrelatedRecords()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await ModuleFixture.CreateAsync(Provider, token);
        await SeedAsync(fixture, token);
        using var backup = new OwnedTestRoot();
        var manifest = await BackupAsync(fixture, backup.Path, token);
        await using var target = fixture.Resources.Postgres is { } postgres
            ? await postgres.CreateRecoveryTargetAsync(token) : null;
        if (target is not null)
            await fixture.Resources.Postgres!.RestoreStorageSetToFreshAsync(backup.Path, target, token);
        var installation = target is null ? fixture.Installation
            : StorageInstallation.PostgreSql(target.ConnectionString);
        await using var scope = await new StorageMaintenance(installation).AcquireAsync(token);
        await using var sourceScope = target is null ? null : await fixture.Maintenance.AcquireAsync(token);
        var privateValues = PrivateValues(fixture, backup.Path, target?.ConnectionString);
        var sourceBefore = await RecoverySetVerifier.InspectStoppedAsync(fixture.Installation,
            sourceScope ?? scope, token);
        var targetBefore = await RecoverySetVerifier.InspectStoppedAsync(installation, scope, token);
        var metadata = Path.Combine(backup.Path, RecoveryManifest.FileName);
        var original = await File.ReadAllTextAsync(metadata, token);
        using (var capture = new Capture())
        {
            var actual = await VerifyAsync(backup.Path, installation, scope, token);
            Assert.True(RecoverySetVerifier.SameInventory(actual, sourceBefore));
            capture.AssertRecords(manifest.Provider, "recover", privateValues,
                ("storage_ready", "ready", "retry_same_request", "unchanged"));
        }
        var module = manifest.Allocations.First(row => row.Relational);
        var artifact = manifest.Artifacts.First();
        var artifactPath = Path.Combine(backup.Path, artifact.Name);
        var originalArtifact = await File.ReadAllBytesAsync(artifactPath, token);
        var modulePath = Path.Combine(backup.Path, "modules", module.AllocationKey!, "module.db");
        var originalModule = target is null ? await File.ReadAllBytesAsync(modulePath, token) : null;
        foreach (var kind in new[] { "format", "version", "provider", "json", "path", "hash", "inventory",
            "extra", "owner", "marker", "physical", "scope" })
        {
            var json = JsonNode.Parse(original)!;
            var provider = manifest.Provider;
            var code = StorageCode.Corrupt;
            switch (kind)
            {
                case "format": json["format"] = Canary; break;
                case "version": json["registryVersion"] = 999; break;
                case "provider": json["provider"] = Canary; break;
                case "json": break;
                case "path": json["artifacts"]![0]!["name"] = Canary; break;
                case "hash": json["artifacts"]![0]!["sha256"] = new string('0', 64); break;
                case "inventory":
                    json["registryRevision"] = manifest.RegistryRevision + 1;
                    code = StorageCode.ReconciliationRequired; break;
                case "extra":
                    CreatePrivateFile(Path.Combine(backup.Path, "recovery-credential"), Canary);
                    code = StorageCode.ReconciliationRequired; break;
                case "owner":
                    json["allocations"]![0]!["allocationId"] = Guid.NewGuid();
                    code = StorageCode.ReconciliationRequired; break;
                case "marker":
                    await SetMarkerOwnerAsync(installation, backup.Path, module, Canary, token);
                    if (target is null)
                    {
                        var name = $"modules/{module.AllocationKey}/module.db";
                        var changed = await RecoverySetVerifier.DescribeArtifactAsync(backup.Path, name, token);
                        json["artifacts"]!.AsArray().Single(item => item!["name"]!.GetValue<string>() == name)!
                            ["sha256"] = changed.Sha256;
                    }
                    code = StorageCode.OwnerMismatch; break;
                case "physical":
                    var broken = originalArtifact.ToArray();
                    broken.AsSpan(0, 16).Fill(0);
                    await File.WriteAllBytesAsync(artifactPath, broken, token);
                    json["artifacts"]![0]!["sha256"] = Convert.ToHexStringLower(SHA256.HashData(broken));
                    break;
                case "scope": code = StorageCode.RequestInvalid; break;
            }
            if (target is null && kind is "format" or "version" or "provider" or "json" or "path")
                provider = "unknown";
            await File.WriteAllTextAsync(metadata, kind == "json" ? "{\"" + Canary : json.ToJsonString(), token);
            var filesBefore = SnapshotFiles(backup.Path);
            using (var capture = new Capture())
            {
                var error = await Assert.ThrowsAsync<StorageException>(() =>
                    kind == "scope"
                        ? RecoverySetVerifier.VerifyAsync(backup.Path, target is null ? null : installation,
                            new UnownedScope(), token)
                        : VerifyAsync(backup.Path, installation, scope, token));
                Assert.Equal(code, error.Code);
                capture.AssertRecords(provider, "recover", privateValues, code switch
                {
                    StorageCode.Corrupt => ("storage_corrupt", "reconciliationrequired", "restore_complete_set", "unchanged"),
                    StorageCode.OwnerMismatch => ("storage_owner_mismatch", "reconciliationrequired", "inspect_preserved_target", "unchanged"),
                    StorageCode.RequestInvalid => ("storage_request_invalid", "rejected", "correct_configuration", "unchanged"),
                    _ => ("storage_reconciliation_required", "reconciliationrequired", "inspect_preserved_target", "unchanged")
                });
            }
            Assert.Equal(filesBefore, SnapshotFiles(backup.Path));
            if (kind == "extra") File.Delete(Path.Combine(backup.Path, "recovery-credential"));
            if (kind == "marker")
            {
                if (originalModule is not null) await File.WriteAllBytesAsync(modulePath, originalModule, token);
                else await SetMarkerOwnerAsync(installation, backup.Path, module, module.ModuleId, token);
            }
            if (kind == "physical") await File.WriteAllBytesAsync(artifactPath, originalArtifact, token);
        }
        await File.WriteAllTextAsync(metadata, original, token);
        var sourceAfter = await RecoverySetVerifier.InspectStoppedAsync(fixture.Installation,
            sourceScope ?? scope, token);
        Assert.Equal(JsonSerializer.Serialize(sourceBefore), JsonSerializer.Serialize(sourceAfter));
        Assert.Equal(JsonSerializer.Serialize(targetBefore), JsonSerializer.Serialize(
            await RecoverySetVerifier.InspectStoppedAsync(installation, scope, token)));
        await AssertPayloadAsync(fixture, token);
        if (target is not null)
        {
            await using var restored = await new StorageRegistry(installation).BindAsync(fixture.Alpha.Owner, token);
            await using var context = await restored.CreateContextAsync(ModuleFixture.AlphaFactory, token);
            Assert.Equal(Canary, (await context.Records.SingleAsync(token)).Payload);
        }
    }

    [Fact]
    public async Task ManifestWritesReportCommittedRefusedAndInterruptedDurabilityWithoutDisclosingInput()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await ModuleFixture.CreateAsync(Provider, token);
        await SeedAsync(fixture, token);
        using var backup = new OwnedTestRoot();
        var manifest = await BackupAsync(fixture, backup.Path, token);
        using var output = new OwnedTestRoot();
        CreateDirectory(output.Path);
        var privateValues = PrivateValues(fixture, backup.Path, output.Path);
        using (var capture = new Capture())
        {
            await RecoverySetVerifier.WriteManifestAsync(output.Path, manifest, token);
            capture.AssertRecords(manifest.Provider, "recover", privateValues,
                ("storage_ready", "ready", "retry_same_request", "committed"));
        }
        var before = SnapshotFiles(output.Path);
        using (var capture = new Capture())
        {
            await Assert.ThrowsAsync<IOException>(() =>
                RecoverySetVerifier.WriteManifestAsync(output.Path, manifest, token));
            capture.AssertRecords(manifest.Provider, "recover", privateValues,
                ("storage_unavailable", "failed", "correct_configuration", "unchanged"));
        }
        Assert.Equal(before, SnapshotFiles(output.Path));
        using (var capture = new Capture())
        {
            var error = await Assert.ThrowsAsync<StorageException>(() =>
                RecoverySetVerifier.WriteManifestAsync(output.Path, manifest with { Provider = Canary }, token));
            Assert.Equal(StorageCode.Corrupt, error.Code);
            capture.AssertRecords("unknown", "recover", privateValues,
                ("storage_corrupt", "reconciliationrequired", "restore_complete_set", "unchanged"));
        }
        Assert.Equal(before, SnapshotFiles(output.Path));
        using var partial = new OwnedTestRoot();
        CreateDirectory(partial.Path);
        using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(token);
        using (var observer = new CancelAtCreation(cancelled))
        using (DiagnosticListener.AllListeners.Subscribe(observer))
        using (var capture = new Capture())
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                RecoverySetVerifier.WriteManifestAsync(partial.Path, manifest, cancelled.Token));
            capture.AssertRecords(manifest.Provider, "recover", privateValues.Append(partial.Path),
                ("storage_cancelled", "failed", "inspect_preserved_target", "uncertain"));
        }
        Assert.True(File.Exists(Path.Combine(partial.Path, RecoveryManifest.FileName)));
        Assert.Equal(before, SnapshotFiles(output.Path));
        await AssertPayloadAsync(fixture, token);
    }

    [Fact]
    public async Task InvalidPathsBeforeMetadataAndCancelledInspectionHaveBoundedPrivateRecords()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync(Provider, token);
        using (var capture = new Capture())
        {
            var error = await Assert.ThrowsAsync<StorageException>(() =>
                RecoverySetVerifier.VerifyAsync(Canary, cancellationToken: token));
            Assert.Equal(StorageCode.PathRejected, error.Code);
            capture.AssertRecords("unknown", "recover", [Canary],
                ("storage_path_rejected", "rejected", "inspect_preserved_target", "unchanged"));
        }
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        using (var capture = new Capture())
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                RecoverySetVerifier.InspectStoppedAsync(fixture.Installation, scope, cancelled.Token));
            capture.AssertRecords(fixture, "recover",
                ("storage_cancelled", "failed", "inspect_preserved_target", "unchanged"));
        }
        await fixture.AssertInventoryAsync(0, 0, token);
    }

    private static Task<RecoveryManifest> VerifyAsync(string directory, StorageInstallation installation,
        IStorageMaintenanceScope scope, CancellationToken token) => installation.Provider == "sqlite"
        ? RecoverySetVerifier.VerifyAsync(directory, cancellationToken: token)
        : RecoverySetVerifier.VerifyAsync(directory, installation, scope, token);

    private static Task<RecoveryManifest> BackupAsync(ModuleFixture fixture, string directory,
        CancellationToken token) => fixture.Resources.Postgres is { } postgres
        ? postgres.BackupStorageSetAsync(directory, token)
        : SqliteRecoveryExercise.BackupAsync(fixture.Root, directory, token);

    private static async Task SeedAsync(ModuleFixture fixture, CancellationToken token)
    {
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        Assert.IsType<StorageResult.Ready>(await fixture.Alpha.MigrateAsync(
            ModuleFixture.AlphaFactory, scope, cancellationToken: token));
        await using var context = await fixture.Alpha.CreateContextAsync(ModuleFixture.AlphaFactory, token);
        context.Records.Add(new AlphaRecord { Id = 1, Payload = Canary });
        await context.SaveChangesAsync(token);
    }

    private static async Task AssertPayloadAsync(ModuleFixture fixture, CancellationToken token)
    {
        await using var context = await fixture.Alpha.CreateContextAsync(ModuleFixture.AlphaFactory, token);
        Assert.Equal(Canary, (await context.Records.SingleAsync(token)).Payload);
    }

    private static async Task SetMarkerOwnerAsync(StorageInstallation installation, string backup,
        RecoveryAllocation row, string owner, CancellationToken token)
    {
        using var budget = new OperationBudget(OperationBudget.Resolve, token);
        await using var connection = installation.Provider == "sqlite"
            ? StorageInstallation.OpenSqlite(Path.Combine(backup, "modules", row.AllocationKey!, "module.db"), budget)
            : await installation.OpenPlatformAsync(budget);
        var prefix = installation.Provider == "sqlite" ? "" : $"\"{row.AllocationKey}\".";
        await RegistryMigrationRunner.ExecuteAsync(connection,
            $"UPDATE {prefix}__kofte_storage_owner SET module_id=@owner", budget, null, ("owner", owner));
    }

    private static IEnumerable<string> PrivateValues(ModuleFixture fixture, params string?[] other)
    {
        var values = new List<string> { Canary, "recovery-credential", fixture.Root };
        foreach (var value in other.Concat([fixture.Resources.Postgres?.ConnectionString,
            fixture.Resources.Postgres?.ProvisioningConnectionString]))
        {
            if (value is null) continue;
            values.Add(value);
            if (value.Contains("Host=", StringComparison.Ordinal))
                values.Add(new NpgsqlConnectionStringBuilder(value).Password!);
        }
        return values;
    }

    private static string SnapshotFiles(string directory) => JsonSerializer.Serialize(
        Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
            .Select(path => new
            {
                name = Path.GetRelativePath(directory, path),
                hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)))
            }));

    private static void CreateDirectory(string path)
    {
        if (OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        Directory.CreateDirectory(path, UnixStorePaths.DirectoryMode);
    }

    private static void CreatePrivateFile(string path, string content)
    {
        if (OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        using var file = new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            UnixCreateMode = UnixStorePaths.FileMode
        });
        using var writer = new StreamWriter(file);
        writer.Write(content);
    }

    private sealed class UnownedScope : IStorageMaintenanceScope
    {
        public CancellationToken Lifetime => throw new InvalidOperationException(Canary);
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class CancelAtCreation(CancellationTokenSource cancellation) : IObserver<DiagnosticListener>,
        IObserver<KeyValuePair<string, object?>>, IDisposable
    {
        private IDisposable? subscription;
        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "Kofte.Storage") subscription = listener.Subscribe(this);
        }
        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key == "storage.phase" &&
                value.Value?.GetType().GetProperty("phase")?.GetValue(value.Value) is "recovery_manifest_created")
                cancellation.Cancel();
        }
        public void OnCompleted() { }
        public void OnError(Exception error) => throw error;
        public void Dispose() => subscription?.Dispose();
    }
}

[Trait("Provider", "sqlite"), Trait("Suite", "privacy"), Trait("Suite", "recovery")]
public sealed class SqliteRecoveryDiagnosticsTests : RecoveryDiagnosticsTests
{ protected override string Provider => "sqlite"; }

[Trait("Provider", "postgres"), Trait("Suite", "privacy"), Trait("Suite", "recovery")]
public sealed class PostgresRecoveryDiagnosticsTests : RecoveryDiagnosticsTests
{ protected override string Provider => "postgres"; }
