using System.Diagnostics;
using System.Text.Json;
using Kofte.Composition.TestModule;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Kofte.Storage.IntegrationTests.Composition;

public abstract class CompositionRecoveryTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task DisabledOwnerStoresRequireExplicitCompleteSetRecoveryAndNeverLegacyPlatformOnlyDestruction()
    {
        if (OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync(Provider, token);
        await fixture.SeedUnrelatedNoStorageAsync(token);
        var installation = Installation(fixture);
        var recipes = StorageRecipes.ForRelease();
        await using (var session = await Kofte.Infrastructure.Composition.CompositionStorageSession.OpenPrepareAsync(installation, token))
        {
            await session.InspectAsync(StorageRecipes.Descriptions, recipes, serving: false, token);
            await session.PrepareAsync(StorageRecipes.Descriptions, recipes, token);
        }
        var registry = new StorageRegistry(installation);
        await using (var alpha = await registry.BindAsync(StorageRecipes.Descriptions[0].Id, token))
            await StorageRecipes.WriteAlphaAsync(alpha, new(11, "alpha-complete-set-original", "retain-alpha"), token: token);
        await using (var beta = await registry.BindAsync(StorageRecipes.Descriptions[1].Id, token))
            await StorageRecipes.WriteBetaAsync(beta, new(11, "beta-complete-set-original", "retain-beta"), token: token);
        var originals = await ReadOwnerSetAsync(installation, token);
        await fixture.WriteSelectionAsync(JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            modules = StorageRecipes.Descriptions.Select(owner => new
            {
                id = owner.Id.Value,
                enabled = owner.Id.Value == StorageRecipes.NoStorageOwner,
                settings = new { }
            })
        }), token);
        var beforeDisabled = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        await using (var validate = fixture.Start("validate", CompositionTestCatalog.RelationalCurrent))
            Assert.Equal(0, (await validate.WaitAsync(token)).ExitCode);
        Assert.Equal(beforeDisabled, await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
        var inventory = await fixture.InspectStoppedInventoryAsync(token);
        Assert.Equal(4, inventory.Allocations.Count);
        Assert.Equal(2, inventory.Allocations.Count(row => row.Relational));
        Assert.All(inventory.Allocations.Where(row => row.Relational), row => Assert.Equal(2, row.History.Count));
        using var selectedOlderSet = new OwnedTestRoot();
        var manifest = fixture.Storage.Postgres is { } postgres
            ? await postgres.BackupStorageSetAsync(selectedOlderSet.Path, token)
            : await SqliteRecoveryExercise.BackupAsync(fixture.Storage.DataRoot, selectedOlderSet.Path, token);
        Assert.True(RecoverySetVerifier.SameInventory(manifest, inventory));
        Assert.Equal(beforeDisabled, await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
        using var legacyOutput = new OwnedTestRoot();
        if (fixture.Storage.Postgres is { } pg)
        {
            var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                pg.BackupAsync(Path.Combine(legacyOutput.Path, "platform-only.dump"), token));
            Assert.Equal("postgres_registry_requires_complete_recovery_set", refusal.Message);
        }
        else
        {
            var store = new Kofte.Host.Bootstrap.SqlitePlatformStore(fixture.Storage.DataRoot);
            Assert.Throws<Kofte.Host.Bootstrap.BootstrapException>(() =>
                store.Backup(Path.Combine(legacyOutput.Path, "platform-only.db")));
            Assert.Throws<Kofte.Host.Bootstrap.BootstrapException>(() => store.Reset(true));
            using var legacySource = new OwnedTestRoot();
            var legacy = new Kofte.Host.Bootstrap.SqlitePlatformStore(legacySource.Path);
            await legacy.InitializeAsync();
            Directory.CreateDirectory(legacyOutput.Path, UnixStorePaths.DirectoryMode);
            var legacyBackup = Path.Combine(legacyOutput.Path, "legacy.db");
            legacy.Backup(legacyBackup);
            Assert.Throws<Kofte.Host.Bootstrap.BootstrapException>(() => store.Restore(legacyBackup, true, legacyBackup));
        }
        Assert.Equal(beforeDisabled, await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
        await using (var alpha = await registry.BindAsync(StorageRecipes.Descriptions[0].Id, token))
            await StorageRecipes.WriteAlphaAsync(alpha, new(22, "post-backup-not-in-selected-set", "explicit-later-write"), token: token);
        var newerSource = await ReadOwnerSetAsync(installation, token);
        Assert.NotEqual(originals, newerSource);
        var beforeRestore = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        using var freshRoot = new OwnedTestRoot();
        await using var freshPostgres = fixture.Storage.Postgres is not null
            ? await PostgresResources.CreateFromEnvironmentAsync(token) : null;
        StorageInstallation restored;
        if (freshPostgres is not null)
        {
            await fixture.Storage.Postgres!.RestoreStorageSetToFreshAsync(selectedOlderSet.Path, freshPostgres, token);
            restored = StorageInstallation.PostgreSql(freshPostgres.ConnectionString, freshPostgres.ProvisioningConnectionString);
        }
        else
        {
            await SqliteRecoveryExercise.RestoreFreshAsync(selectedOlderSet.Path, freshRoot.Path, token);
            restored = StorageInstallation.Sqlite(freshRoot.Path);
        }
        Assert.Equal(originals, await ReadOwnerSetAsync(restored, token));
        Assert.Equal(newerSource, await ReadOwnerSetAsync(installation, token));
        Assert.Equal(beforeRestore, await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
        await using var stopped = await new StorageMaintenance(restored).AcquireAsync(token);
        var restoredInventory = await RecoverySetVerifier.InspectStoppedAsync(restored, stopped, token);
        Assert.True(RecoverySetVerifier.SameInventory(manifest, restoredInventory));
    }

    [Fact]
    public async Task SupervisedPreparationInterruptsOnlyLaterOwnerAndRequiresExplicitRecoveryBeforeFreshProcess()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync(Provider, token);
        await fixture.SeedUnrelatedNoStorageAsync(token);
        await fixture.WriteSelectionAsync(JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            modules = StorageRecipes.Descriptions.Select(owner => new { id = owner.Id.Value, enabled = true, settings = new { } })
        }), token);
        await using (var interrupted = fixture.Start("prepare", CompositionTestCatalog.RelationalHoldMigration,
            observeAuthor: true))
        {
            await fixture.AuthorTrace.WaitForAsync(StorageRecipes.BetaOwner, "migration", token);
            var started = Stopwatch.GetTimestamp();
            await interrupted.StopAsync();
            Assert.True(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(15));
            var output = await interrupted.ResultAsync();
            Assert.Equal(1, output.ExitCode);
            Assert.True(interrupted.ExitVerified && interrupted.DescendantExitsVerified);
            Assert.Empty(output.Stderr);
            var records = Records(output.Stdout);
            Assert.Contains(records, record => record.GetProperty("stage").GetString() == "migrated" &&
                record.GetProperty("owner").GetString() == StorageRecipes.AlphaOwner &&
                record.GetProperty("durability").GetString() == "committed");
            Assert.DoesNotContain(records, record => record.GetProperty("stage").GetString() is
                "started" or "published");
            await fixture.AuthorTrace.EndAsync(token);
            Assert.DoesNotContain(fixture.AuthorTrace.Records, record => record.Phase == "start");
        }
        var installation = Installation(fixture);
        var registry = new StorageRegistry(installation);
        var earlier = Assert.IsType<StorageResult.Ready>(await registry.ResolveAsync(StorageRecipes.Descriptions[0].Id, token));
        var later = StorageRecipes.Descriptions[1].Id;
        Assert.Equal(StorageOutcome.ReconciliationRequired, (await registry.ResolveAsync(later, token)).Outcome);
        var beforeRefusal = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        await using (var rejected = fixture.Start("prepare", CompositionTestCatalog.RelationalCurrent))
        {
            var output = await rejected.WaitAsync(token);
            Assert.Equal(4, output.ExitCode);
            Assert.Equal("composition_reconciliation_required", Records(output.Stdout)[^1].GetProperty("code").GetString());
        }
        Assert.Equal(beforeRefusal, await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
        await using (var scope = await new StorageMaintenance(installation).AcquireAsync(token))
            Assert.IsType<StorageResult.Ready>(await ModuleMigrationRecovery.RecoverAsync(registry, later,
                StorageRecipes.BetaFactory(), scope, token));
        Assert.Equal(JsonSerializer.Serialize(earlier.Assignment),
            JsonSerializer.Serialize(Assert.IsType<StorageResult.Ready>(
                await registry.ResolveAsync(StorageRecipes.Descriptions[0].Id, token)).Assignment));
        await using (var fresh = fixture.Start("prepare", CompositionTestCatalog.RelationalCurrent, observeAuthor: true))
        {
            Assert.Equal(0, (await fresh.WaitAsync(token)).ExitCode);
            await fixture.AuthorTrace.CompleteAsync(token);
        }
        foreach (var owner in StorageRecipes.Descriptions.Take(2))
        {
            await using var binding = await registry.BindAsync(owner.Id, token);
            await using var context = owner.Id.Value == StorageRecipes.AlphaOwner
                ? (DbContext)await StorageRecipes.CreateAlphaContextAsync(binding, token: token)
                : await StorageRecipes.CreateBetaContextAsync(binding, token: token);
            Assert.Equal(owner.Id.Value == StorageRecipes.AlphaOwner
                ? new[] { StorageRecipes.AlphaInitial, StorageRecipes.AlphaCurrent }
                : new[] { StorageRecipes.BetaInitial, StorageRecipes.BetaCurrent },
                await context.Database.GetAppliedMigrationsAsync(token));
        }
        var inventory = await fixture.InspectStoppedInventoryAsync(token);
        Assert.Equal(4, inventory.Allocations.Count);
        Assert.Equal(2, inventory.Allocations.Count(owner => owner.Relational));
    }

    private static JsonElement[] Records(string output) => output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(line => JsonSerializer.Deserialize<JsonElement>(line)).ToArray();

    private static StorageInstallation Installation(CompositionFixture fixture) => fixture.Storage.Postgres is { } postgres
        ? StorageInstallation.PostgreSql(postgres.ConnectionString, postgres.ProvisioningConnectionString)
        : StorageInstallation.Sqlite(fixture.Storage.DataRoot);

    private static async Task<string> ReadOwnerSetAsync(StorageInstallation installation, CancellationToken token)
    {
        var registry = new StorageRegistry(installation);
        List<object> values = [];
        foreach (var owner in StorageRecipes.Descriptions.Take(2))
        {
            var ready = Assert.IsType<StorageResult.Ready>(await registry.ResolveAsync(owner.Id, token));
            await using var binding = await registry.BindAsync(owner.Id, token);
            var rows = owner.Id.Value == StorageRecipes.AlphaOwner
                ? JsonSerializer.Serialize(await StorageRecipes.ReadAlphaAsync(binding, token: token))
                : JsonSerializer.Serialize(await StorageRecipes.ReadBetaAsync(binding, token: token));
            await using var context = owner.Id.Value == StorageRecipes.AlphaOwner
                ? (DbContext)await StorageRecipes.CreateAlphaContextAsync(binding, token: token)
                : await StorageRecipes.CreateBetaContextAsync(binding, token: token);
            values.Add(new
            {
                ready.Assignment,
                rows,
                history = await context.Database.GetAppliedMigrationsAsync(token)
            });
        }
        return JsonSerializer.Serialize(values);
    }
}

[Trait("Provider", "sqlite"), Trait("Suite", "recovery")]
public sealed class SqliteCompositionRecoveryTests : CompositionRecoveryTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres"), Trait("Suite", "recovery")]
public sealed class PostgresCompositionRecoveryTests : CompositionRecoveryTests
{
    protected override string Provider => "postgres";
}
