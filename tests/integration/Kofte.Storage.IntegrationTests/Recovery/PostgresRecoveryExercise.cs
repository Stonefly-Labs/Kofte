using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;
using Npgsql;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Kofte.Storage.IntegrationTests.Modules.Alpha;
using Kofte.Storage.IntegrationTests.Modules.Beta;

namespace Kofte.Storage.IntegrationTests.Recovery;

[Trait("Provider", "postgres")]
[Trait("Suite", "recovery")]
public sealed class PostgresRecoveryExercise
{
    [Fact]
    public async Task CompleteCustomDumpRestoresBothSchemasToFreshOwnedTargetWithoutElevatingRuntime()
    {
        var token = TestContext.Current.CancellationToken;
        await using var provider = await StorageProviderScope.CreateAsync("postgres", token);
        var source = provider.Postgres!;
        var installation = StorageInstallation.PostgreSql(source.ConnectionString, source.ProvisioningConnectionString);
        var maintenance = new StorageMaintenance(installation);
        var registry = new StorageRegistry(installation);
        var assignments = new List<StorageAssignment>();
        await using (var scope = await maintenance.AcquireAsync(token))
        {
            await maintenance.UpgradeAsync(scope, token);
            foreach (var id in new[] { "org.example.alpha", "org.example.beta" })
            {
                var assignment = Assert.IsType<StorageResult.Ready>(await registry.RegisterAsync(
                    RegistryFixture.Module(id), scope, token)).Assignment;
                await using var binding = await registry.BindAsync(assignment.Owner, token);
                if (id.EndsWith(".alpha", StringComparison.Ordinal))
                {
                    Assert.IsType<StorageResult.Ready>(await binding.MigrateAsync(
                        (options, owned) => new PostgresAlphaContext(options, owned), scope, cancellationToken: token));
                    await using var context = await binding.CreateContextAsync(
                        (options, owned) => new PostgresAlphaContext(options, owned), token);
                    context.Records.Add(new AlphaRecord { Id = 1, Payload = id });
                    await context.SaveChangesAsync(token);
                }
                else
                {
                    Assert.IsType<StorageResult.Ready>(await binding.MigrateAsync(
                        (options, owned) => new PostgresBetaContext(options, owned), scope, cancellationToken: token));
                    await using var context = await binding.CreateContextAsync(
                        (options, owned) => new PostgresBetaContext(options, owned), token);
                    context.Records.Add(new BetaRecord { Id = 1, Payload = id });
                    await context.SaveChangesAsync(token);
                }
                assignments.Add(Assert.IsType<StorageResult.Ready>(await registry.ResolveAsync(assignment.Owner, token)).Assignment);
            }
        }
        using var backup = new OwnedTestRoot();
        var manifest = await source.BackupStorageSetAsync(backup.Path, token);
        Assert.Equal(2, manifest.Allocations.Count);
        Assert.Single(manifest.Artifacts);
        Assert.Equal(2, manifest.Allocations[0].History.Count);
        Assert.Single(manifest.Allocations[1].History);
        await using var target = await source.CreateRecoveryTargetAsync(token);
        var restored = await source.RestoreStorageSetToFreshAsync(backup.Path, target, token);
        Assert.Equal(manifest.InstallationId, restored.InstallationId);
        var targetRegistry = new StorageRegistry(StorageInstallation.PostgreSql(target.ConnectionString));
        foreach (var assignment in assignments)
        {
            var actual = Assert.IsType<StorageResult.Ready>(await targetRegistry.ResolveAsync(assignment.Owner, token)).Assignment;
            Assert.Equal(assignment.AllocationId, actual.AllocationId);
            await using var connection = new NpgsqlConnection(target.ConnectionString);
            await connection.OpenAsync(token);
            await using var read = new NpgsqlCommand($"""
                SELECT "Payload" FROM "{actual.AllocationKey}"."Records" WHERE "Id"=1
                """, connection);
            Assert.Equal(assignment.Owner.Value, await read.ExecuteScalarAsync(token));
        }
        Assert.Equal("postgres_registry_requires_complete_recovery_set",
            (await Assert.ThrowsAsync<InvalidOperationException>(() => source.BackupAsync(
                Path.Combine(backup.Path, "legacy.dump"), token))).Message);
        await using var rejectedTarget = await source.CreateRecoveryTargetAsync(token);
        var metadata = Path.Combine(backup.Path, RecoveryManifest.FileName);
        var originalMetadata = await File.ReadAllTextAsync(metadata, token);
        foreach (var corruption in new[] { "format", "incomplete", "revision", "marker", "checkpoint", "inventory", "path", "missing", "extra" })
        {
            var json = JsonNode.Parse(originalMetadata)!;
            switch (corruption)
            {
                case "format": json["format"] = "unsupported"; break;
                case "incomplete": json["complete"] = false; break;
                case "revision": json["registryRevision"] = 999999; break;
                case "marker": json["allocations"]![0]!["allocationId"] = Guid.NewGuid(); break;
                case "checkpoint": json["allocations"]![0]!["migrationCheckpoint"] = "not-applied"; break;
                case "inventory": json["allocations"]!.AsArray().RemoveAt(0); break;
                case "path": json["artifacts"]![0]!["name"] = "../outside.dump"; break;
                case "missing": json["artifacts"]!.AsArray().Clear(); break;
                case "extra": json["artifacts"]!.AsArray().Add(json["artifacts"]![0]!.DeepClone()); break;
            }
            await File.WriteAllTextAsync(metadata, json.ToJsonString(), token);
            await Assert.ThrowsAsync<StorageException>(() =>
                source.RestoreStorageSetToFreshAsync(backup.Path, rejectedTarget, token));
        }
        await File.WriteAllTextAsync(metadata, originalMetadata, token);
        var dumpPath = Path.Combine(backup.Path, "installation.dump");
        await File.AppendAllTextAsync(dumpPath, "mixed-set", token);
        using var dump = File.OpenRead(dumpPath);
        var digest = Convert.ToHexStringLower(SHA256.HashData(dump));
        await Assert.ThrowsAsync<StorageException>(() =>
            source.RestoreStorageSetToFreshAsync(backup.Path, rejectedTarget, token));
        dump.Position = 0;
        Assert.Equal(digest, Convert.ToHexStringLower(SHA256.HashData(dump)));
        await new Host.Bootstrap.PostgresPlatformStore(rejectedTarget.ConnectionString).ProbeAsync(token);
        await using var unchanged = new NpgsqlConnection(rejectedTarget.ConnectionString);
        await unchanged.OpenAsync(token);
        await using var empty = new NpgsqlCommand(
            "SELECT count(*) FROM pg_catalog.pg_tables WHERE schemaname='kofte_system'", unchanged);
        Assert.Equal(0L, await empty.ExecuteScalarAsync(token));
        await source.ValidateOwnershipAsync(token);
        await target.ValidateOwnershipAsync(token);
    }
}
