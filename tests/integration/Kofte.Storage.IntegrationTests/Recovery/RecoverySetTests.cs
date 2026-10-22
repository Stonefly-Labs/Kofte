using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;
using Microsoft.Data.Sqlite;
using Kofte.Storage.IntegrationTests.Modules.Alpha;
using Kofte.Storage.IntegrationTests.Modules.Beta;

namespace Kofte.Storage.IntegrationTests.Recovery;

[Trait("Provider", "sqlite")]
[Trait("Suite", "recovery")]
public sealed class RecoverySetTests
{
    [Fact]
    public async Task ProviderBackupRestoresEveryAllocatedFileAndReadsOriginalRowsAfterRestart()
    {
        await using var fixture = await StorageProviderScope.CreateAsync("sqlite", TestContext.Current.CancellationToken);
        using var backup = new OwnedTestRoot();
        using var target = new OwnedTestRoot();
        var assignments = await RegisterAndWriteAsync(fixture);
        var originalHashes = assignments.Select(assignment =>
            Digest(ModulePath(fixture.DataRoot, assignment))).ToArray();
        var manifest = await SqliteRecoveryExercise.BackupAsync(
            fixture.DataRoot, backup.Path, TestContext.Current.CancellationToken);
        Assert.Equal(3, manifest.Artifacts.Count);
        Assert.Equal(3, manifest.Allocations.Count);
        Assert.Equal(2, manifest.Allocations[0].History.Count);
        Assert.Single(manifest.Allocations[1].History);
        await SqliteRecoveryExercise.RestoreFreshAsync(backup.Path, target.Path, TestContext.Current.CancellationToken);
        var registry = new Infrastructure.Storage.Registry.StorageRegistry(StorageInstallation.Sqlite(target.Path));
        foreach (var assignment in assignments)
        {
            var ready = Assert.IsType<StorageResult.Ready>(await registry.ResolveAsync(
                assignment.Owner, TestContext.Current.CancellationToken)).Assignment;
            Assert.Equal(assignment.AllocationId, ready.AllocationId);
            Assert.Equal(assignment.AllocationKey, ready.AllocationKey);
            Assert.Equal(assignment.Owner.Value, ReadPayload(ModulePath(target.Path, ready)));
        }
        Assert.Equal(originalHashes, assignments.Select(assignment =>
            Digest(ModulePath(fixture.DataRoot, assignment))).ToArray());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SqliteRecoveryExercise.RestoreFreshAsync(backup.Path, target.Path, TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("extra")]
    [InlineData("sidecar")]
    [InlineData("hash")]
    [InlineData("length")]
    [InlineData("format")]
    [InlineData("incomplete")]
    [InlineData("revision")]
    [InlineData("marker")]
    [InlineData("checkpoint")]
    [InlineData("inventory")]
    [InlineData("path")]
    public async Task CorruptMixedAndIncompleteSetsFailWithoutTouchingOriginalsOrCreatingTargets(string corruption)
    {
        await using var fixture = await StorageProviderScope.CreateAsync("sqlite", TestContext.Current.CancellationToken);
        using var backup = new OwnedTestRoot();
        using var target = new OwnedTestRoot();
        var assignments = await RegisterAndWriteAsync(fixture);
        var originals = assignments.Select(assignment => Digest(ModulePath(fixture.DataRoot, assignment))).ToArray();
        await SqliteRecoveryExercise.BackupAsync(fixture.DataRoot, backup.Path, TestContext.Current.CancellationToken);
        var metadata = Path.Combine(backup.Path, RecoveryManifest.FileName);
        var json = JsonNode.Parse(await File.ReadAllTextAsync(metadata, TestContext.Current.CancellationToken))!;
        var member = ModulePath(backup.Path, assignments[0]);
        switch (corruption)
        {
            case "missing": File.Delete(member); break;
            case "extra": CreatePrivate(Path.Combine(backup.Path, "extra"), "unlisted"); break;
            case "sidecar": CreatePrivate(member + "-wal", "preserved-sidecar"); break;
            case "hash": await File.AppendAllTextAsync(member, "corruption", TestContext.Current.CancellationToken); break;
            case "length": json["artifacts"]![0]!["length"] = 1; break;
            case "format": json["format"] = "kofte-storage-recovery-v999"; break;
            case "incomplete": json["complete"] = false; break;
            case "revision": json["registryRevision"] = 999999; break;
            case "marker": json["allocations"]![0]!["allocationId"] = Guid.NewGuid(); break;
            case "checkpoint": json["allocations"]![0]!["migrationCheckpoint"] = "not-applied"; break;
            case "inventory": json["allocations"]!.AsArray().RemoveAt(0); break;
            case "path": json["artifacts"]![0]!["name"] = "../outside.db"; break;
        }
        await File.WriteAllTextAsync(metadata, json.ToJsonString(), TestContext.Current.CancellationToken);
        var backupBefore = Directory.EnumerateFiles(backup.Path, "*", SearchOption.AllDirectories)
            .ToDictionary(path => path, Digest);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            SqliteRecoveryExercise.RestoreFreshAsync(backup.Path, target.Path, TestContext.Current.CancellationToken));
        Assert.False(Directory.Exists(target.Path));
        Assert.Equal(originals, assignments.Select(assignment => Digest(ModulePath(fixture.DataRoot, assignment))).ToArray());
        Assert.All(backupBefore, item => Assert.Equal(item.Value, Digest(item.Key)));
    }

    [Fact]
    public async Task ChangedPhysicalHistoryWithoutRegistryCheckpointCannotBecomeACompleteBackup()
    {
        await using var fixture = await StorageProviderScope.CreateAsync("sqlite", TestContext.Current.CancellationToken);
        using var old = new OwnedTestRoot();
        using var current = new OwnedTestRoot();
        var assignments = await RegisterAndWriteAsync(fixture);
        await SqliteRecoveryExercise.BackupAsync(fixture.DataRoot, old.Path, TestContext.Current.CancellationToken);
        var assignment = assignments[0];
        using (var connection = Open(ModulePath(fixture.DataRoot, assignment)))
        using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                INSERT INTO "__EFMigrationsHistory" VALUES ('unrecorded-physical-history','test-fixture')
                """;
            command.ExecuteNonQuery();
        }
        // A history change without a durable matching migration checkpoint is never a complete backup.
        await Assert.ThrowsAsync<StorageException>(() =>
            SqliteRecoveryExercise.BackupAsync(fixture.DataRoot, current.Path, TestContext.Current.CancellationToken));
        Assert.False(Directory.Exists(current.Path));
        Assert.Equal(assignment.Owner.Value, ReadPayload(ModulePath(old.Path, assignment)));
    }

    [Fact]
    public async Task MixedInstallationMemberFailsOwnerVerificationEvenWithItsCorrectDigest()
    {
        await using var fixture = await StorageProviderScope.CreateAsync("sqlite", TestContext.Current.CancellationToken);
        await using var foreign = await StorageProviderScope.CreateAsync("sqlite", TestContext.Current.CancellationToken);
        using var backup = new OwnedTestRoot();
        var assignments = await RegisterAndWriteAsync(fixture);
        var foreignAssignments = await RegisterAndWriteAsync(foreign);
        var manifest = await SqliteRecoveryExercise.BackupAsync(
            fixture.DataRoot, backup.Path, TestContext.Current.CancellationToken);
        File.Copy(ModulePath(foreign.DataRoot, foreignAssignments[0]), ModulePath(backup.Path, assignments[0]), overwrite: true);
        var name = $"modules/{assignments[0].AllocationKey}/module.db";
        var replacement = await RecoverySetVerifier.DescribeArtifactAsync(backup.Path, name,
            TestContext.Current.CancellationToken);
        File.Delete(Path.Combine(backup.Path, RecoveryManifest.FileName));
        await RecoverySetVerifier.WriteManifestAsync(backup.Path,
            manifest with { Artifacts = manifest.Artifacts.Select(row => row.Name == name ? replacement : row).ToArray() },
            TestContext.Current.CancellationToken);
        var before = Digest(ModulePath(backup.Path, assignments[0]));
        Assert.Equal(StorageCode.OwnerMismatch, (await Assert.ThrowsAsync<StorageException>(() =>
            RecoverySetVerifier.VerifyAsync(backup.Path, cancellationToken: TestContext.Current.CancellationToken))).Code);
        Assert.Equal(before, Digest(ModulePath(backup.Path, assignments[0])));
    }

    private static async Task<StorageAssignment[]> RegisterAndWriteAsync(StorageProviderScope fixture)
    {
        var assignments = new List<StorageAssignment>();
        var installation = StorageInstallation.Sqlite(fixture.DataRoot);
        var maintenance = new StorageMaintenance(installation);
        var registry = new Infrastructure.Storage.Registry.StorageRegistry(installation);
        await using var scope = await maintenance.AcquireAsync(TestContext.Current.CancellationToken);
        await maintenance.UpgradeAsync(scope, TestContext.Current.CancellationToken);
        foreach (var id in new[] { "org.example.alpha", "org.example.beta" })
        {
            var assignment = Assert.IsType<StorageResult.Ready>(await registry.RegisterAsync(
                RegistryFixture.Module(id), scope, TestContext.Current.CancellationToken)).Assignment;
            await using var binding = await registry.BindAsync(assignment.Owner, TestContext.Current.CancellationToken);
            if (id.EndsWith(".alpha", StringComparison.Ordinal))
            {
                Assert.IsType<StorageResult.Ready>(await binding.MigrateAsync(
                    (options, owned) => new SqliteAlphaContext(options, owned), scope,
                    cancellationToken: TestContext.Current.CancellationToken));
                await using var context = await binding.CreateContextAsync(
                    (options, owned) => new SqliteAlphaContext(options, owned), TestContext.Current.CancellationToken);
                context.Records.Add(new AlphaRecord { Id = 1, Payload = id });
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
            else
            {
                Assert.IsType<StorageResult.Ready>(await binding.MigrateAsync(
                    (options, owned) => new SqliteBetaContext(options, owned), scope,
                    cancellationToken: TestContext.Current.CancellationToken));
                await using var context = await binding.CreateContextAsync(
                    (options, owned) => new SqliteBetaContext(options, owned), TestContext.Current.CancellationToken);
                context.Records.Add(new BetaRecord { Id = 1, Payload = id });
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }
            assignments.Add(Assert.IsType<StorageResult.Ready>(await registry.ResolveAsync(
                assignment.Owner, TestContext.Current.CancellationToken)).Assignment);
        }
        Assert.IsType<StorageResult.NoStorage>(await registry.RegisterAsync(
            RegistryFixture.Module("org.example.stateless", relational: false), scope,
            TestContext.Current.CancellationToken));
        return [.. assignments];
    }

    private static string ModulePath(string root, StorageAssignment assignment) =>
        Path.Combine(root, "modules", assignment.AllocationKey, "module.db");

    private static SqliteConnection Open(string path)
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        { DataSource = path, Mode = SqliteOpenMode.ReadWrite, Pooling = false, DefaultTimeout = 1 }.ToString());
        connection.Open();
        return connection;
    }

    private static string ReadPayload(string path)
    {
        using var connection = Open(path);
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Payload FROM Records WHERE Id=1";
        return (string)command.ExecuteScalar()!;
    }

    private static string Digest(string path)
    {
        using var file = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(file));
    }

    private static void CreatePrivate(string path, string payload)
    {
        if (OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        using var file = new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            UnixCreateMode = UnixStorePaths.FileMode
        });
        using var writer = new StreamWriter(file);
        writer.Write(payload);
    }
}
