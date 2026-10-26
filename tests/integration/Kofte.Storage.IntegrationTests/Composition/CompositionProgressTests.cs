using Kofte.Infrastructure.Composition;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Providers;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kofte.Storage.IntegrationTests.Composition;

public abstract class CompositionProgressTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task ActualPreparationAndCurrentReadVerificationReportOrderedOwnerWorkAndOriginalResults()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync(Provider, token);
        await fixture.SeedUnrelatedNoStorageAsync(token);
        var installation = Installation(fixture);
        var first = ModuleFixture.Module("org.example.progress-first");
        var later = ModuleFixture.Module("org.example.progress-later");
        var noStore = RegistryFixture.Module("org.example.progress-none", relational: false);
        ModuleDescription[] enabled = [first, later, noStore];
        var recipes = Recipes(ModuleFixture.Module(first.Id.Value).Id)
            .Concat(Recipes(ModuleFixture.Module(later.Id.Value).Id)).ToArray();
        List<CompositionStorageProgress> progress = [];
        await using (var session = await CompositionStorageSession.OpenPrepareAsync(installation, token))
        {
            await session.InspectAsync(enabled, recipes, serving: false, token);
            Assert.Empty(progress);
            var bindings = await session.PrepareAsync(enabled, recipes, token, progress.Add);
            Assert.Equal([first.Id, later.Id], bindings.Keys);
        }
        Assert.Equal(enabled.SelectMany(owner => new[]
        {
            (owner.Id, CompositionStorageTransition.Entered),
            (owner.Id, CompositionStorageTransition.Completed)
        }), progress.Select(value => (value.Owner, value.Transition)));
        foreach (var value in progress.Where(value => value.Transition == CompositionStorageTransition.Completed &&
            value.Owner != noStore.Id))
        {
            var result = Assert.IsType<StorageResult.Ready>(value.Result);
            Assert.Equal(value.Owner.Value, result.ModuleId);
            Assert.Equal(StorageOperation.Migrate, result.Operation);
            Assert.Equal(StorageDurability.Committed, result.Durability);
            Assert.Matches("^[a-f0-9]{32}$", result.CorrelationId);
        }
        Assert.IsType<StorageResult.NoStorage>(progress[^1].Result);
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        progress.Clear();
        await using (var session = await CompositionStorageSession.OpenReadAsync(installation, token))
        {
            await session.InspectAsync(enabled, recipes, serving: true, token);
            await session.VerifyAsync(enabled, recipes, token, progress.Add);
        }
        Assert.Equal(6, progress.Count);
        foreach (var value in progress.Where(value => value.Transition == CompositionStorageTransition.Completed &&
            value.Owner != noStore.Id))
        {
            var result = Assert.IsType<StorageResult.Ready>(value.Result);
            Assert.Equal(StorageOperation.Resolve, result.Operation);
            Assert.Equal(StorageDurability.Unchanged, result.Durability);
        }
        Assert.Null(progress[^1].Result);
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
    }

    [Fact]
    public async Task LaterActualFactoryFailureDoesNotInventCompletionAndPreservesOriginalFailureEvidence()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync(Provider, token);
        await fixture.SeedUnrelatedNoStorageAsync(token);
        var installation = Installation(fixture);
        var first = ModuleFixture.Module("org.example.progress-first");
        var later = ModuleFixture.Module("org.example.progress-later");
        var recipes = Recipes(first.Id).Concat(Recipes(later.Id, failAssigned: true)).ToArray();
        List<CompositionStorageProgress> progress = [];
        CompositionStorageException failure;
        await using (var session = await CompositionStorageSession.OpenPrepareAsync(installation, token))
        {
            await session.InspectAsync([first, later], recipes, serving: false, token);
            failure = await Assert.ThrowsAsync<CompositionStorageException>(() =>
                session.PrepareAsync([first, later], recipes, token, progress.Add));
        }
        Assert.Equal(new[]
        {
            (first.Id, CompositionStorageTransition.Entered), (first.Id, CompositionStorageTransition.Completed),
            (later.Id, CompositionStorageTransition.Entered), (later.Id, CompositionStorageTransition.Failed)
        }, progress.Select(value => (value.Owner, value.Transition)));
        Assert.Same(failure.StorageFailure, progress[^1].Result);
        var failed = Assert.IsType<StorageResult.Failure>(progress[^1].Result);
        Assert.Equal(later.Id.Value, failed.ModuleId);
        Assert.Equal(StorageOperation.Migrate, failed.Operation);
        Assert.Matches("^[a-f0-9]{32}$", failed.CorrelationId);
        var earlier = Assert.IsType<StorageResult.Ready>(progress[1].Result);
        var retained = Assert.IsType<StorageResult.Ready>(await new StorageRegistry(installation).ResolveAsync(first.Id, token));
        Assert.Equal((earlier.Assignment.Owner, earlier.Assignment.InstallationId, earlier.Assignment.AllocationId,
            earlier.Assignment.AllocationKey, earlier.Assignment.AllocatorVersion, earlier.Assignment.Revision),
            (retained.Assignment.Owner, retained.Assignment.InstallationId, retained.Assignment.AllocationId,
                retained.Assignment.AllocationKey, retained.Assignment.AllocatorVersion, retained.Assignment.Revision));
        using var budget = new OperationBudget(OperationBudget.Resolve, token);
        await using var platform = await installation.OpenPlatformAsync(budget);
        var row = await new RegistryMigrationRunner(installation).FindAsync(platform, first.Id, budget);
        Assert.Equal("current", row!.MigrationState);
        Assert.Equal("202610240001_Initial", row.Checkpoint);
    }

    private static StorageInstallation Installation(CompositionFixture fixture) =>
        fixture.Storage.Postgres is { } postgres
            ? StorageInstallation.PostgreSql(postgres.ConnectionString, postgres.ProvisioningConnectionString)
            : StorageInstallation.Sqlite(fixture.Storage.DataRoot);

    private static ModuleStorageRecipe[] Recipes(ModuleId owner, bool failAssigned = false) =>
    [
        ModuleStorageRecipe.Create<SqlitePreflightContext>(owner, "sqlite", "202610240001_Initial", (options, boundary) =>
        {
            RefuseAssigned(options, failAssigned);
            return new(options, boundary);
        }),
        ModuleStorageRecipe.Create<PostgresPreflightContext>(owner, "postgresql", "202610240001_Initial", (options, boundary) =>
        {
            RefuseAssigned(options, failAssigned);
            return new(options, boundary);
        })
    ];

    private static void RefuseAssigned(Microsoft.EntityFrameworkCore.DbContextOptions options, bool fail)
    {
        if (fail && options.Extensions.OfType<RelationalOptionsExtension>()
            .Any(extension => extension.Connection?.State == System.Data.ConnectionState.Open))
            throw new InvalidOperationException("composition_test_assigned_factory_failed");
    }
}

[Trait("Provider", "sqlite"), Trait("Suite", "recovery")]
public sealed class SqliteCompositionProgressTests : CompositionProgressTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres"), Trait("Suite", "recovery")]
public sealed class PostgresCompositionProgressTests : CompositionProgressTests
{
    protected override string Provider => "postgres";
}
