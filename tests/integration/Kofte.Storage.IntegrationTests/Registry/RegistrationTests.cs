using System.Text.Json;
using Kofte.Host.Bootstrap;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Allocation;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Registry;

public abstract class RegistrationTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task DistinctOwnersNoStorageAndChangedMetadataKeepExactAssignmentAcrossThreeReopens()
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, TestContext.Current.CancellationToken);
        var alpha = RegistryFixture.Module("org.example.alpha");
        var beta = RegistryFixture.Module("org.example.beta");
        var noStorage = RegistryFixture.Module("org.example.stateless", relational: false);
        StorageAssignment original;
        await using (var scope = await fixture.Maintenance.AcquireAsync(TestContext.Current.CancellationToken))
        {
            original = Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(
                alpha, scope, TestContext.Current.CancellationToken)).Assignment;
            var other = Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(
                beta, scope, TestContext.Current.CancellationToken)).Assignment;
            Assert.NotEqual(original.AllocationKey, other.AllocationKey);
            Assert.NotEqual(original.AllocationId, other.AllocationId);
            Assert.IsType<StorageResult.NoStorage>(await fixture.Registry.RegisterAsync(
                noStorage, scope, TestContext.Current.CancellationToken));
        }
        for (var i = 0; i < 3; i++)
        {
            var installation = fixture.NewInstallation();
            var maintenance = new StorageMaintenance(installation);
            var registry = new StorageRegistry(installation);
            var resolved = Assert.IsType<StorageResult.Ready>(
                await registry.ResolveAsync(alpha.Id, TestContext.Current.CancellationToken)).Assignment;
            Assert.Equal(original.AllocationId, resolved.AllocationId);
            Assert.Equal(original.AllocationKey, resolved.AllocationKey);
            Assert.Equal(original.InstallationId, resolved.InstallationId);
            Assert.IsType<StorageResult.NoStorage>(await registry.ResolveAsync(
                noStorage.Id, TestContext.Current.CancellationToken));
            await using var scope = await maintenance.AcquireAsync(TestContext.Current.CancellationToken);
            var changed = RegistryFixture.Module(alpha.Id.Value, version: "2.0.0", display: "Renamed Köfte module");
            var retried = Assert.IsType<StorageResult.Ready>(await registry.RegisterAsync(
                changed, scope, TestContext.Current.CancellationToken)).Assignment;
            Assert.Equal(original.AllocationId, retried.AllocationId);
            Assert.Equal(original.AllocationKey, retried.AllocationKey);
            Assert.Equal(original.Revision, retried.Revision);
        }
        await fixture.AssertInventoryAsync(3, 2, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ConflictingStorageNeedAndForcedCollisionPreserveOriginalOwner()
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, TestContext.Current.CancellationToken);
        var alpha = RegistryFixture.Module("org.example.alpha");
        await using var scope = await fixture.Maintenance.AcquireAsync(TestContext.Current.CancellationToken);
        var original = Assert.IsType<StorageResult.Ready>(
            await fixture.Registry.RegisterAsync(alpha, scope, TestContext.Current.CancellationToken)).Assignment;
        var conflict = await fixture.Registry.RegisterAsync(RegistryFixture.Module(alpha.Id.Value, relational: false),
            scope, TestContext.Current.CancellationToken);
        Assert.Equal(StorageOutcome.Rejected, conflict.Outcome);
        Assert.Equal("storage_conflict", Assert.Single(conflict.Diagnostics).Code);
        var forced = new StorageRegistry(fixture.Installation, _ => original.AllocationKey);
        var collided = await forced.RegisterAsync(RegistryFixture.Module("org.example.beta"),
            scope, TestContext.Current.CancellationToken);
        Assert.Equal(StorageOutcome.Rejected, collided.Outcome);
        var after = Assert.IsType<StorageResult.Ready>(await fixture.Registry.ResolveAsync(
            alpha.Id, TestContext.Current.CancellationToken)).Assignment;
        Assert.Equal(original.AllocationId, after.AllocationId);
        await fixture.AssertInventoryAsync(1, 1, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ChangedAllocatorIsNeverInvokedForPersistedOwnerAndPreservesInventoryAcrossReopen()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync(Provider, token);
        var alpha = RegistryFixture.Module("org.example.alpha");
        var beta = RegistryFixture.Module("org.example.beta");
        StorageAssignment original;
        StorageAssignment other;
        RecoveryInventory before;
        await using (var scope = await fixture.Maintenance.AcquireAsync(token))
        {
            original = Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(
                alpha, scope, token)).Assignment;
            other = Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(
                beta, scope, token)).Assignment;
            await RegistrySentinel.WriteAsync(fixture, original, "alpha-before-allocator-change");
            await RegistrySentinel.WriteAsync(fixture, other, "beta-before-allocator-change");
            before = await RecoverySetVerifier.InspectStoppedAsync(fixture.Installation, scope, token);
        }
        var changedCandidate = StorageAllocator.Allocate(RegistryFixture.Module("org.example.reallocated-alpha").Id);
        Assert.NotEqual(original.AllocationKey, changedCandidate);
        var allocatorCalls = 0;
        var installation = fixture.NewInstallation();
        var registry = new StorageRegistry(installation, _ =>
        {
            allocatorCalls++;
            return changedCandidate;
        });
        await using var reopened = await new StorageMaintenance(installation).AcquireAsync(token);
        var changed = RegistryFixture.Module(alpha.Id.Value, version: "2.0.0", display: "Renamed Köfte module");
        var retried = Assert.IsType<StorageResult.Ready>(await registry.RegisterAsync(changed, reopened, token));
        Assert.Equal(StorageDurability.Unchanged, retried.Durability);
        Assert.Equal(StorageAssignmentSnapshot.From(original), StorageAssignmentSnapshot.From(retried.Assignment));
        var resolved = Assert.IsType<StorageResult.Ready>(await registry.ResolveAsync(alpha.Id, token)).Assignment;
        Assert.Equal(StorageAssignmentSnapshot.From(original), StorageAssignmentSnapshot.From(resolved));
        Assert.Equal(StorageAssignmentSnapshot.From(other), StorageAssignmentSnapshot.From(
            Assert.IsType<StorageResult.Ready>(await registry.ResolveAsync(beta.Id, token)).Assignment));
        Assert.Equal("alpha-before-allocator-change", await RegistrySentinel.ReadAsync(fixture, resolved));
        Assert.Equal("beta-before-allocator-change", await RegistrySentinel.ReadAsync(fixture, other));
        var after = await RecoverySetVerifier.InspectStoppedAsync(installation, reopened, token);
        Assert.Equal(before.RegistryRevision, after.RegistryRevision);
        Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(after));
        Assert.Equal(0, allocatorCalls);
    }

    [Fact]
    public async Task PersistedNoStorageCannotBecomeRelationalAndPreservesInventoryAcrossReopen()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync(Provider, token);
        var alpha = RegistryFixture.Module("org.example.alpha");
        var noStorage = RegistryFixture.Module("org.example.stateless", relational: false);
        StorageAssignment original;
        RecoveryInventory before;
        await using (var scope = await fixture.Maintenance.AcquireAsync(token))
        {
            original = Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(
                alpha, scope, token)).Assignment;
            Assert.IsType<StorageResult.NoStorage>(await fixture.Registry.RegisterAsync(noStorage, scope, token));
            await RegistrySentinel.WriteAsync(fixture, original, "alpha-before-rejected-storage-change");
            before = await RecoverySetVerifier.InspectStoppedAsync(fixture.Installation, scope, token);
        }
        var allocatorCalls = 0;
        var installation = fixture.NewInstallation();
        var registry = new StorageRegistry(installation, owner =>
        {
            allocatorCalls++;
            return StorageAllocator.Allocate(owner);
        });
        await using var reopened = await new StorageMaintenance(installation).AcquireAsync(token);
        var relational = RegistryFixture.Module(noStorage.Id.Value, version: "2.0.0", display: "Relational request");
        var rejected = await registry.RegisterAsync(relational, reopened, token);
        Assert.Equal(StorageOutcome.Rejected, rejected.Outcome);
        Assert.Equal(StorageDurability.Unchanged, rejected.Durability);
        var diagnostic = Assert.Single(rejected.Diagnostics);
        Assert.Equal("storage_conflict", diagnostic.Code);
        Assert.Equal("correct_configuration", diagnostic.Guidance);
        Assert.IsType<StorageResult.NoStorage>(await registry.ResolveAsync(noStorage.Id, token));
        var resolved = Assert.IsType<StorageResult.Ready>(await registry.ResolveAsync(alpha.Id, token)).Assignment;
        Assert.Equal(StorageAssignmentSnapshot.From(original), StorageAssignmentSnapshot.From(resolved));
        Assert.Equal("alpha-before-rejected-storage-change", await RegistrySentinel.ReadAsync(fixture, resolved));
        var after = await RecoverySetVerifier.InspectStoppedAsync(installation, reopened, token);
        var persisted = Assert.Single(after.Allocations, row => row.ModuleId == noStorage.Id.Value);
        Assert.False(persisted.Relational);
        Assert.Equal("no_storage", persisted.State);
        Assert.Null(persisted.AllocationKey);
        Assert.Null(persisted.AllocationId);
        Assert.Null(persisted.AllocatorVersion);
        Assert.Equal(before.RegistryRevision, after.RegistryRevision);
        Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(after));
        Assert.Equal(0, allocatorCalls);
    }

    [Fact]
    public async Task CancelledRegistrationHasNoMutationAndForeignScopeCannotWrite()
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, TestContext.Current.CancellationToken);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        await using var scope = await fixture.Maintenance.AcquireAsync(TestContext.Current.CancellationToken);
        var result = await fixture.Registry.RegisterAsync(RegistryFixture.Module("org.example.alpha"),
            scope, cancelled.Token);
        Assert.Equal(StorageOutcome.Failed, result.Outcome);
        Assert.Equal(StorageDurability.Unchanged, result.Durability);
        Assert.Equal("storage_cancelled", Assert.Single(result.Diagnostics).Code);
        var foreign = new StorageRegistry(fixture.NewInstallation());
        Assert.Equal(StorageOutcome.Rejected, (await foreign.RegisterAsync(
            RegistryFixture.Module("org.example.beta"), scope, TestContext.Current.CancellationToken)).Outcome);
        await fixture.AssertInventoryAsync(0, 0, TestContext.Current.CancellationToken);
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteRegistrationTests : RegistrationTests
{
    protected override string Provider => "sqlite";

    [Fact]
    public async Task MissingReadyFileIsNeverRecreated()
    {
        await using var fixture = await RegistryFixture.CreateAsync("sqlite", TestContext.Current.CancellationToken);
        var alpha = RegistryFixture.Module("org.example.alpha");
        StorageAssignment assignment;
        await using (var scope = await fixture.Maintenance.AcquireAsync(TestContext.Current.CancellationToken))
            assignment = Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(
                alpha, scope, TestContext.Current.CancellationToken)).Assignment;
        var path = System.IO.Path.Combine(fixture.Root!, "modules", assignment.AllocationKey, "module.db");
        var preserved = path + ".preserved";
        File.Move(path, preserved);
        var result = await fixture.Registry.ResolveAsync(alpha.Id, TestContext.Current.CancellationToken);
        Assert.Equal(StorageOutcome.ReconciliationRequired, result.Outcome);
        Assert.False(File.Exists(path));
        await using (var scope = await fixture.Maintenance.AcquireAsync(TestContext.Current.CancellationToken))
            Assert.Equal(StorageOutcome.ReconciliationRequired, (await fixture.Registry.RegisterAsync(
                alpha, scope, TestContext.Current.CancellationToken)).Outcome);
        Assert.False(File.Exists(path));
        File.Move(preserved, path);
        Assert.IsType<StorageResult.Ready>(await fixture.Registry.ResolveAsync(alpha.Id,
            TestContext.Current.CancellationToken));
    }
}

[Trait("Provider", "postgres")]
public sealed class PostgresRegistrationTests : RegistrationTests
{
    protected override string Provider => "postgres";
}

internal sealed class RegistryFixture : IAsyncDisposable
{
    private readonly RegistryRoot root = new();
    private PostgresResources? postgres;
    internal string? Root => postgres is null ? root.Path : null;
    internal StorageInstallation Installation { get; private set; } = null!;
    internal StorageMaintenance Maintenance { get; private set; } = null!;
    internal StorageRegistry Registry { get; private set; } = null!;
    internal string Provider => postgres is null ? "sqlite" : "postgres";
    internal string? RuntimeConnection => postgres?.ConnectionString;
    internal string? ProvisioningConnection => postgres?.ProvisioningConnectionString;

    internal static async Task<RegistryFixture> CreateAsync(string provider, CancellationToken token, bool upgrade = true)
    {
        var fixture = new RegistryFixture();
        try
        {
            if (provider == "postgres")
                fixture.postgres = await PostgresResources.CreateFromEnvironmentAsync(token);
            else if (provider == "sqlite")
                await new SqlitePlatformStore(fixture.root.Path).InitializeAsync();
            else throw new InvalidOperationException("storage_test_provider_required");
            fixture.Installation = fixture.NewInstallation();
            fixture.Maintenance = new(fixture.Installation);
            fixture.Registry = new(fixture.Installation);
            if (upgrade)
            {
                await using var scope = await fixture.Maintenance.AcquireAsync(token);
                await fixture.Maintenance.UpgradeAsync(scope, token);
            }
            return fixture;
        }
        catch { await fixture.DisposeAsync(); throw; }
    }

    internal StorageInstallation NewInstallation() => postgres is null
        ? StorageInstallation.Sqlite(root.Path)
        : StorageInstallation.PostgreSql(postgres.ConnectionString, postgres.ProvisioningConnectionString);

    internal static ModuleDescription Module(string id, bool relational = true, string version = "1.0.0",
        string display = "Köfte storage example")
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            id,
            version,
            displayName = display,
            hostContract = "1.0.0",
            capabilities = Array.Empty<object>(),
            dependencies = Array.Empty<object>(),
            permissions = Array.Empty<object>(),
            storage = new { relational }
        });
        return Assert.IsType<ManifestValidationResult.Accepted>(
            ManifestValidator.Validate(bytes, new ManifestValidationContext("1.0.0"))).Description;
    }

    internal async Task AssertInventoryAsync(int expectedRegistrations, int expectedStores, CancellationToken token)
    {
        using var budget = new OperationBudget(OperationBudget.Register, token);
        await using var connection = await Installation.OpenPlatformAsync(budget);
        var runner = new RegistryMigrationRunner(Installation);
        var metadata = await runner.ReadInstallationAsync(connection, budget);
        Assert.Equal(1, metadata.Version);
        await using var count = RegistryMigrationRunner.Command(connection,
            $"SELECT count(*) FROM {runner.Registrations}", budget);
        Assert.Equal((long)expectedRegistrations, await count.ExecuteScalarAsync(token));
        var query = postgres is null
            ? "SELECT count(*) FROM __kofte_module_registrations WHERE relational=1 AND state='ready'"
            : "SELECT count(*) FROM pg_catalog.pg_namespace WHERE nspname LIKE 'm\\_%' ESCAPE '\\'";
        Assert.Equal((long)expectedStores, await RegistryMigrationRunner.ScalarAsync(connection, query, budget));
        if (postgres is null)
        {
            var modules = System.IO.Path.Combine(root.Path, "modules");
            var files = Directory.Exists(modules) ? Directory.EnumerateFiles(modules, "module.db",
                SearchOption.AllDirectories).Count() : 0;
            Assert.Equal(expectedStores, files);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (postgres is not null) await postgres.DisposeAsync();
        root.Dispose();
    }
}
