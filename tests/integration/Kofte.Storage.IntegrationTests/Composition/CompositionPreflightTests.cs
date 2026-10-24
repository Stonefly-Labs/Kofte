using System.Text.Json;
using Kofte.Infrastructure.Composition;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Platform.Modules.Manifest;
using Kofte.Storage.IntegrationTests.Providers;
using Kofte.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kofte.Storage.IntegrationTests.Composition;

public abstract class CompositionPreflightTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task NewOwnerModelsHaveNoUsableAssignmentAndInspectionPreservesCompleteInventory()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        var before = await InventoryAsync(fixture, token);
        var owner = ModuleFixture.Module("org.example.composition");
        var calls = 0;
        var recipes = Recipes(owner.Id, boundary =>
        {
            calls++;
            Assert.Equal(owner.Id, boundary.Owner);
        }, requireUnassigned: true);
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, token))
            await session.InspectAsync([owner], recipes, serving: false, token);
        Assert.Equal(2, calls);
        Assert.Equal(before, await InventoryAsync(fixture, token));
    }

    [Fact]
    public async Task InvalidLaterTargetDoesNotAllocateEarlierOwner()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        var before = await InventoryAsync(fixture, token);
        var first = ModuleFixture.Module("org.example.composition");
        var later = ModuleFixture.Module("org.example.later");
        var recipes = Recipes(ModuleFixture.Module(first.Id.Value).Id)
            .Concat(Recipes(ModuleFixture.Module(later.Id.Value).Id, target: "missing")).ToArray();
        await using (var session = await CompositionStorageSession.OpenPrepareAsync(fixture.Installation, token))
            await Assert.ThrowsAsync<CompositionStorageException>(() =>
                session.InspectAsync([first, later], recipes, serving: false, token));
        Assert.Equal(before, await InventoryAsync(fixture, token));
    }

    [Fact]
    public async Task BothProviderRecipesAreRequiredBeforeInvokingAnyFactory()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        var before = await InventoryAsync(fixture, token);
        var owner = ModuleFixture.Module("org.example.composition");
        var calls = 0;
        var recipes = Recipes(owner.Id, _ => calls++).Take(1).ToArray();
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, token))
        {
            var error = await Assert.ThrowsAsync<CompositionStorageException>(() =>
                session.InspectAsync([owner], recipes, serving: false, token));
            Assert.Equal("composition_recipe_invalid", error.Code);
        }
        Assert.Equal(0, calls);
        Assert.Equal(before, await InventoryAsync(fixture, token));
    }

    [Fact]
    public async Task ChangedPersistedRelationalDeclarationIsNotTreatedAsNewOwner()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        var before = await InventoryAsync(fixture, token);
        var description = NoStorageModule("org.example.alpha");
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, token))
        {
            var error = await Assert.ThrowsAsync<CompositionStorageException>(() =>
                session.InspectAsync([description], [], serving: false, token));
            Assert.Equal("composition_storage_reconciliation_required", error.Code);
        }
        Assert.Equal(before, await InventoryAsync(fixture, token));
    }

    [Fact]
    public async Task AdmissibleUnregisteredServingOwnerRequiresPreparationWithoutAllocating()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        var before = await InventoryAsync(fixture, token);
        var owner = ModuleFixture.Module("org.example.composition");
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, token))
        {
            var error = await Assert.ThrowsAsync<CompositionStorageException>(() =>
                session.InspectAsync([owner], Recipes(owner.Id), serving: true, token));
            Assert.Equal("composition_prepare_required", error.Code);
        }
        Assert.Equal(before, await InventoryAsync(fixture, token));
    }

    [Theory]
    [InlineData("provider")]
    [InlineData("version")]
    [InlineData("duplicate")]
    [InlineData("owner")]
    public async Task InvalidRecipeMetadataRefusesBeforeEveryContextFactory(string invalid)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token);
        var owner = ModuleFixture.Module("org.example.composition");
        var calls = 0;
        var recipes = Recipes(owner.Id, _ => calls++).ToList();
        if (invalid == "duplicate") recipes.Add(recipes[0]);
        else
            recipes[0] = ModuleStorageRecipe.Create<SqlitePreflightContext>(
                invalid == "owner" ? ModuleFixture.Module("org.example.other").Id : owner.Id,
                invalid == "provider" ? "unsupported" : "sqlite", "202610240001_Initial",
                (options, boundary) => { calls++; return new(options, boundary); },
                contractVersion: invalid == "version" ? 2 : 1);
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, token))
            await Assert.ThrowsAsync<CompositionStorageException>(() =>
                session.InspectAsync([owner], recipes, serving: false, token));
        Assert.Equal(0, calls);
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token));
    }

    [Theory]
    [InlineData("in_progress")]
    [InlineData("recovery_required")]
    [InlineData("reserved")]
    public async Task PersistedIncompleteStateIsNeverTreatedAsMissing(string state)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        using (var budget = new OperationBudget(OperationBudget.Resolve, token))
        await using (var connection = await fixture.Installation.OpenPlatformAsync(budget))
        {
            var runner = new RegistryMigrationRunner(fixture.Installation);
            await RegistryMigrationRunner.ExecuteAsync(connection,
                state == "reserved"
                    ? $"UPDATE {runner.Registrations} SET state='reserved',revision=revision+1 WHERE module_id='org.example.alpha'"
                    : $"UPDATE {runner.Registrations} SET migration_state=@state,revision=revision+1 WHERE module_id='org.example.alpha'",
                budget, null, ("state", state));
        }
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token);
        var owner = ModuleFixture.Module("org.example.composition");
        var calls = 0;
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, token))
            await Assert.ThrowsAsync<StorageException>(() =>
                session.InspectAsync([owner], Recipes(owner.Id, _ => calls++), serving: false, token));
        Assert.Equal(0, calls);
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token));
    }

    [Fact]
    public async Task ForeignExistingPhysicalOwnerIsRefusedBeforeNewOwnerFactories()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        using (var budget = new OperationBudget(OperationBudget.Resolve, token))
        await using (var connection = await fixture.Alpha.OpenAsync(budget))
        {
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 1;
            var schema = fixture.Alpha.Schema is { } value ? "\"" + value + "\"." : "";
            command.CommandText = $"UPDATE {schema}__kofte_storage_owner SET module_id='org.example.foreign'";
            await command.ExecuteNonQueryAsync(token);
        }
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token);
        var owner = ModuleFixture.Module("org.example.composition");
        var calls = 0;
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, token))
        {
            var error = await Assert.ThrowsAsync<StorageException>(() =>
                session.InspectAsync([owner], Recipes(owner.Id, _ => calls++), serving: false, token));
            Assert.Equal(StorageCode.OwnerMismatch, error.Code);
        }
        Assert.Equal(0, calls);
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token));
    }

    [Fact]
    public async Task MissingExistingPhysicalTargetIsNotReallocated()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        if (fixture.Postgres)
        {
            using var budget = new OperationBudget(OperationBudget.Resolve, token);
            await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
            await RegistryMigrationRunner.ExecuteAsync(connection,
                $"DROP SCHEMA \"{fixture.Alpha.Schema}\" CASCADE", budget);
        }
        else
        {
            var assignment = Assert.IsType<StorageResult.Ready>(await fixture.Alpha.ResolveAsync(token)).Assignment;
            File.Delete(Path.Combine(fixture.Root, "modules", assignment.AllocationKey, "module.db"));
        }
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token);
        var owner = ModuleFixture.Module("org.example.composition");
        var calls = 0;
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, token))
        {
            var error = await Record.ExceptionAsync(() =>
                session.InspectAsync([owner], Recipes(owner.Id, _ => calls++), serving: false, token));
            Assert.True(error is StorageException or StorePathException);
        }
        Assert.Equal(0, calls);
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token));
    }

    [Fact]
    public async Task LaterForeignModelAndRawSqlMigrationAreRejectedBeforeAnyAllocation()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        var before = await InventoryAsync(fixture, token);
        var first = ModuleFixture.Module("org.example.composition");
        var later = ModuleFixture.Module("org.example.later");
        foreach (var foreignModel in new[] { true, false })
        {
            var invalid = new[] { "sqlite", "postgresql" }.Select(provider => foreignModel
                ? ModuleStorageRecipe.Create<ForeignModelContext>(later.Id, provider, "202610240001_Initial",
                    (options, boundary) => new(options, boundary))
                : ModuleStorageRecipe.Create<UnsafeMigrationContext>(later.Id, provider, "202610240001_Initial",
                    (options, boundary) => new(options, boundary))).ToArray();
            var recipes = Recipes(first.Id).Concat(invalid).ToArray();
            await using (var session = await CompositionStorageSession.OpenPrepareAsync(fixture.Installation, token))
                await Assert.ThrowsAsync<CompositionStorageException>(() =>
                    session.InspectAsync([first, later], recipes, serving: false, token));
            Assert.Equal(before, await InventoryAsync(fixture, token));
        }
    }

    [Fact]
    public async Task NoStorageRecipeIsRejectedWithoutInvokingItsFactory()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        var before = await InventoryAsync(fixture, token);
        var owner = NoStorageModule("org.example.composition");
        var calls = 0;
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, token))
            await Assert.ThrowsAsync<CompositionStorageException>(() =>
                session.InspectAsync([owner], Recipes(owner.Id, _ => calls++), serving: false, token));
        Assert.Equal(0, calls);
        Assert.Equal(before, await InventoryAsync(fixture, token));
    }

    [Fact]
    public async Task PersistedNoStorageDeclarationCannotBecomeRelational()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        var noStorage = NoStorageModule("org.example.composition");
        await using (var scope = await fixture.Maintenance.AcquireAsync(token))
            Assert.IsType<StorageResult.NoStorage>(await fixture.Registry.RegisterAsync(noStorage, scope, token));
        var before = await InventoryAsync(fixture, token);
        var changed = ModuleFixture.Module(noStorage.Id.Value);
        var calls = 0;
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, token))
        {
            var error = await Assert.ThrowsAsync<CompositionStorageException>(() =>
                session.InspectAsync([changed], Recipes(changed.Id, _ => calls++), serving: false, token));
            Assert.Equal("composition_storage_reconciliation_required", error.Code);
        }
        Assert.Equal(0, calls);
        Assert.Equal(before, await InventoryAsync(fixture, token));
    }

    [Fact]
    public async Task UnownedPhysicalObjectsAreNotAdoptedOrDeleted()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        if (fixture.Postgres)
        {
            await using var connection = new Npgsql.NpgsqlConnection(fixture.Resources.Postgres!.ProvisioningConnectionString);
            await connection.OpenAsync(token);
            await using var command = new Npgsql.NpgsqlCommand("CREATE SCHEMA composition_orphan", connection);
            await command.ExecuteNonQueryAsync(token);
        }
        else
        {
            if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
            Directory.CreateDirectory(Path.Combine(fixture.Root, "modules", "composition_orphan"), UnixStorePaths.DirectoryMode);
        }
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token);
        var owner = ModuleFixture.Module("org.example.composition");
        var calls = 0;
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, token))
        {
            var error = await Assert.ThrowsAsync<StorageException>(() =>
                session.InspectAsync([owner], Recipes(owner.Id, _ => calls++), serving: false, token));
            Assert.Equal(StorageCode.ReconciliationRequired, error.Code);
        }
        Assert.Equal(0, calls);
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token));
    }

    [Fact]
    public async Task RegistryV0IsNotUpgradedByRelationalValidateOrPrepare()
    {
        var token = TestContext.Current.CancellationToken;
        await using var storage = await StorageProviderScope.CreateAsync(Provider, token);
        var installation = storage.Postgres is null ? StorageInstallation.Sqlite(storage.DataRoot)
            : StorageInstallation.PostgreSql(storage.Postgres.ConnectionString, storage.Postgres.ProvisioningConnectionString);
        var before = await CompositionStoreSnapshot.CaptureAsync(storage, token);
        var owner = ModuleFixture.Module("org.example.composition");
        var calls = 0;
        foreach (var prepare in new[] { false, true })
        {
            await using var session = prepare ? await CompositionStorageSession.OpenPrepareAsync(installation, token)
                : await CompositionStorageSession.OpenReadAsync(installation, token);
            var error = await Assert.ThrowsAsync<StorageException>(() =>
                session.InspectAsync([owner], Recipes(owner.Id, _ => calls++), serving: false, token));
            Assert.Equal(StorageCode.VersionUnsupported, error.Code);
        }
        Assert.Equal(0, calls);
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(storage, token));
    }

    [Fact]
    public async Task UnknownRegistryFormatRefusesWithoutRepair()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        using (var budget = new OperationBudget(OperationBudget.Resolve, token))
        await using (var connection = await fixture.Installation.OpenPlatformAsync(budget))
            await RegistryMigrationRunner.ExecuteAsync(connection,
                $"UPDATE {(fixture.Postgres ? "kofte_system." : "")}__kofte_registry_migrations SET sha256=@digest",
                budget, null, ("digest", new string('0', 64)));
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token);
        var owner = ModuleFixture.Module("org.example.composition");
        var calls = 0;
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, token))
        {
            var error = await Assert.ThrowsAsync<StorageException>(() =>
                session.InspectAsync([owner], Recipes(owner.Id, _ => calls++), serving: false, token));
            Assert.Equal(StorageCode.Corrupt, error.Code);
        }
        Assert.Equal(0, calls);
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token));
    }

    [Theory]
    [InlineData("model")]
    [InlineData("history")]
    public async Task ExistingPreparedModelAndHistoryMustMatchItsActualRecipe(string corrupt)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        var owner = ModuleFixture.Module("org.example.composition");
        var binding = await SeedPreparedAsync(fixture, owner, token);
        await using (binding)
        using (var budget = new OperationBudget(OperationBudget.Resolve, token))
        await using (var connection = await binding.OpenAsync(budget))
        {
            await using var command = connection.CreateCommand();
            command.CommandTimeout = 1;
            var prefix = binding.Schema is { } schema ? "\"" + schema + "\"." : "";
            command.CommandText = corrupt == "model"
                ? $"ALTER TABLE {prefix}\"Records\" ADD COLUMN \"ForeignColumn\" TEXT"
                : $"INSERT INTO {prefix}\"__EFMigrationsHistory\" VALUES ('209901010001_Unknown','10.0.13')";
            await command.ExecuteNonQueryAsync(token);
        }
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token);
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, token))
            await Assert.ThrowsAsync<StorageException>(() =>
                session.InspectAsync([owner], Recipes(owner.Id), serving: false, token));
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token));
    }

    [Fact]
    public async Task CurrentTargetAliasVerifiesReadOnlyAndIssuesOnlyItsActualOwnerBinding()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        var owner = ModuleFixture.Module("org.example.composition");
        await using (var seeded = await SeedPreparedAsync(fixture, owner, token)) { }
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token);
        var recipes = Recipes(owner.Id, target: "Initial");
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, token))
        {
            await session.InspectAsync([owner], recipes, serving: true, token);
            var bindings = await session.VerifyAsync([owner], recipes, token);
            Assert.Equal(owner.Id, Assert.Single(bindings).Key);
            Assert.Equal(owner.Id, bindings[owner.Id].Owner);
        }
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token));
    }

    [Fact]
    public async Task NoStorageInV0NeedsNoRecipesBindingsOrRegistryMutation()
    {
        var token = TestContext.Current.CancellationToken;
        await using var storage = await StorageProviderScope.CreateAsync(Provider, token);
        var installation = storage.Postgres is null ? StorageInstallation.Sqlite(storage.DataRoot)
            : StorageInstallation.PostgreSql(storage.Postgres.ConnectionString);
        var before = await CompositionStoreSnapshot.CaptureAsync(storage, token);
        var owner = NoStorageModule("org.example.composition");
        await using (var session = await CompositionStorageSession.OpenReadAsync(installation, token))
        {
            await session.InspectAsync([owner], [], serving: true, token);
            Assert.Empty(await session.VerifyAsync([owner], [], token));
        }
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(storage, token));
    }

    [Fact]
    public async Task AppliedCheckpointModelCannotReachAnotherSchemaOrIssueAnActualContext()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        var owner = ModuleFixture.Module("org.example.composition");
        await using (var seeded = await SeedPreparedAsync(fixture, owner, token)) { }
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token);
        var actualContexts = 0;
        var recipes = new[] { "sqlite", "postgresql" }.Select(provider =>
            ModuleStorageRecipe.Create<ForeignCheckpointContext>(owner.Id, provider, "202610240001_Initial",
                (options, boundary) =>
                {
                    if (boundary.Binding is not null) actualContexts++;
                    return new(options, boundary);
                })).ToArray();
        await using (var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, token))
        {
            var error = await Assert.ThrowsAsync<CompositionStorageException>(() =>
                session.InspectAsync([owner], recipes, serving: false, token));
            Assert.Equal("composition_recipe_invalid", error.Code);
        }
        Assert.Equal(0, actualContexts);
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token));
    }

    private static async Task<ModuleStorageBinding> SeedPreparedAsync(ModuleFixture fixture,
        ModuleDescription owner, CancellationToken token)
    {
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(owner, scope, token));
        var binding = await fixture.Registry.BindAsync(owner.Id, token);
        try
        {
            Assert.IsType<StorageResult.Ready>(await binding.MigrateAsync<PreflightContext>((options, assigned) =>
                assigned.Provider == "postgresql" ? new PostgresPreflightContext(options, new(assigned))
                    : new SqlitePreflightContext(options, new(assigned)), scope, cancellationToken: token));
            return binding;
        }
        catch
        {
            await binding.DisposeAsync();
            throw;
        }
    }

    private static ModuleDescription NoStorageModule(string id) =>
        Assert.IsType<ManifestValidationResult.Accepted>(ManifestValidator.Validate(
            JsonSerializer.SerializeToUtf8Bytes(new
            {
                schemaVersion = "1",
                id,
                version = "1.0.0",
                displayName = "Köfte test",
                hostContract = "1.0.0",
                capabilities = Array.Empty<object>(),
                dependencies = Array.Empty<object>(),
                permissions = Array.Empty<object>(),
                storage = new { relational = false }
            }), new ManifestValidationContext("1.0.0"))).Description;

    internal async Task<ModuleFixture> CreatePopulatedFixtureAsync(CancellationToken token)
    {
        var fixture = await ModuleFixture.CreateAsync(Provider, token);
        try
        {
            await using var scope = await fixture.Maintenance.AcquireAsync(token);
            Assert.IsType<StorageResult.Ready>(await fixture.Alpha.MigrateAsync(ModuleFixture.AlphaFactory,
                scope, cancellationToken: token));
            Assert.IsType<StorageResult.Ready>(await fixture.Beta.MigrateAsync(ModuleFixture.BetaFactory,
                scope, cancellationToken: token));
            await using (var alpha = await fixture.Alpha.CreateContextAsync(ModuleFixture.AlphaFactory, token))
            {
                alpha.Records.Add(new Modules.Alpha.AlphaRecord
                { Id = 1, Payload = "alpha-preflight-original", Note = "retain-alpha" });
                await alpha.SaveChangesAsync(token);
            }
            await using (var beta = await fixture.Beta.CreateContextAsync(ModuleFixture.BetaFactory, token))
            {
                beta.Records.Add(new Modules.Beta.BetaRecord { Id = 2, Payload = "beta-preflight-original" });
                await beta.SaveChangesAsync(token);
            }
            return fixture;
        }
        catch
        {
            await fixture.DisposeAsync();
            throw;
        }
    }

    internal static IReadOnlyList<ModuleStorageRecipe> Recipes(ModuleId owner,
        Action<ModuleStorageModelBoundary>? inspect = null, string target = "202610240001_Initial",
        bool requireUnassigned = false) =>
    [
        ModuleStorageRecipe.Create<SqlitePreflightContext>(owner, "sqlite", target, (options, boundary) =>
        {
            inspect?.Invoke(boundary);
            var context = new SqlitePreflightContext(options, boundary);
            if (requireUnassigned) Assert.Throws<StorageException>(() => context.Storage);
            return context;
        }),
        ModuleStorageRecipe.Create<PostgresPreflightContext>(owner, "postgresql", target, (options, boundary) =>
        {
            inspect?.Invoke(boundary);
            var context = new PostgresPreflightContext(options, boundary);
            if (requireUnassigned) Assert.Throws<StorageException>(() => context.Storage);
            return context;
        })
    ];

    internal static async Task<string> InventoryAsync(ModuleFixture fixture, CancellationToken token)
    {
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        return JsonSerializer.Serialize(await RecoverySetVerifier.InspectStoppedAsync(fixture.Installation, scope, token));
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteCompositionPreflightTests : CompositionPreflightTests
{ protected override string Provider => "sqlite"; }

[Trait("Provider", "postgres")]
public sealed class PostgresCompositionPreflightTests : CompositionPreflightTests
{
    protected override string Provider => "postgres";

    [Fact]
    public async Task LostReadAuthorityIsNeverReacquiredAndCannotBecomeSuccessfulCleanup()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CreatePopulatedFixtureAsync(token);
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token);
        await using var session = await CompositionStorageSession.OpenReadAsync(fixture.Installation, token);
        await session.InspectAsync([], [], serving: false, token);
        var runtime = new Npgsql.NpgsqlConnectionStringBuilder(fixture.Resources.Postgres!.ConnectionString);
        await using var admin = new Npgsql.NpgsqlConnection(fixture.Resources.Postgres.ProvisioningConnectionString);
        await admin.OpenAsync(token);
        var pids = new List<int>();
        await using (var find = new Npgsql.NpgsqlCommand("""
            SELECT DISTINCT a.pid FROM pg_catalog.pg_stat_activity a JOIN pg_catalog.pg_locks l ON l.pid=a.pid
            WHERE a.datname=current_database() AND a.usename=$1 AND a.application_name=$2
              AND l.locktype='advisory' AND l.granted AND l.mode='ShareLock'
              AND l.classid=(($3::bigint >> 32) & 4294967295)::oid
              AND l.objid=($3::bigint & 4294967295)::oid AND l.objsubid=1
            """, admin))
        {
            find.Parameters.AddWithValue(runtime.Username!);
            find.Parameters.AddWithValue(runtime.ApplicationName!);
            find.Parameters.AddWithValue(Kofte.Infrastructure.Storage.Postgres.PostgresIdentity.LeaseKey);
            await using var reader = await find.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token)) pids.Add(reader.GetInt32(0));
        }
        await using (var terminate = new Npgsql.NpgsqlCommand("""
            SELECT pg_catalog.pg_terminate_backend(pid) FROM pg_catalog.pg_stat_activity
            WHERE pid=$1 AND datname=current_database() AND usename=$2 AND application_name=$3
            """, admin))
        {
            terminate.Parameters.AddWithValue(Assert.Single(pids));
            terminate.Parameters.AddWithValue(runtime.Username!);
            terminate.Parameters.AddWithValue(runtime.ApplicationName!);
            Assert.Equal(true, await terminate.ExecuteScalarAsync(token));
        }
        var failure = await Record.ExceptionAsync(() => session.EnsureAuthorityAsync(token));
        Assert.True(failure is Npgsql.NpgsqlException or StorageException);
        var cleanup = await Assert.ThrowsAsync<StorageException>(async () => await session.DisposeAsync());
        Assert.Equal(StorageCode.Unavailable, cleanup.Code);
        await using (var replacement = await fixture.Maintenance.AcquireAsync(token)) { }
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Resources, token));
    }
}

public sealed class PreflightRecord
{
    public int Id { get; set; }
    public string Value { get; set; } = "";
}

public abstract class PreflightContext(DbContextOptions options, ModuleStorageModelBoundary boundary)
    : AssignedDbContext(options, boundary)
{
    protected override void ConfigureModel(ModelBuilder builder) =>
        Build(builder, ModelBoundary.Schema, ModelBoundary.Provider);

    internal static void Build(ModelBuilder builder, string? schema, string provider)
    {
        builder.HasAnnotation("ProductVersion", "10.0.13");
        if (schema is not null) builder.HasDefaultSchema(schema);
        builder.Entity<PreflightRecord>(entity =>
        {
            entity.ToTable("Records", schema);
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Id).ValueGeneratedNever()
                .HasColumnType(provider == "postgresql" ? "integer" : "INTEGER");
            entity.Property(record => record.Value).IsRequired()
                .HasColumnType(provider == "postgresql" ? "text" : "TEXT");
        });
    }
}

public sealed class SqlitePreflightContext(DbContextOptions options, ModuleStorageModelBoundary boundary)
    : PreflightContext(options, boundary);
public sealed class PostgresPreflightContext(DbContextOptions options, ModuleStorageModelBoundary boundary)
    : PreflightContext(options, boundary);

public abstract class PreflightInitial : AssignedMigration
{
    protected override void Up(MigrationBuilder builder) =>
        builder.CreateTable("Records", schema: AssignedSchema, columns: columns => new
        {
            Id = columns.Column<int>(type: AssignedProvider == "postgresql" ? "integer" : "INTEGER", nullable: false),
            Value = columns.Column<string>(type: AssignedProvider == "postgresql" ? "text" : "TEXT", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Records", record => record.Id));
    protected override void BuildTargetModel(ModelBuilder builder) =>
        PreflightContext.Build(builder, AssignedSchema, AssignedProvider);
}

[DbContext(typeof(SqlitePreflightContext)), Migration("202610240001_Initial")]
public sealed class SqlitePreflightInitial : PreflightInitial;
[DbContext(typeof(PostgresPreflightContext)), Migration("202610240001_Initial")]
public sealed class PostgresPreflightInitial : PreflightInitial;

public abstract class PreflightSnapshot : AssignedModelSnapshot
{
    protected override void BuildModel(ModelBuilder builder) =>
        PreflightContext.Build(builder, AssignedSchema, AssignedProvider);
}

[DbContext(typeof(SqlitePreflightContext))]
public sealed class SqlitePreflightSnapshot : PreflightSnapshot;
[DbContext(typeof(PostgresPreflightContext))]
public sealed class PostgresPreflightSnapshot : PreflightSnapshot;

public sealed class ForeignModelContext(DbContextOptions options, ModuleStorageModelBoundary boundary)
    : PreflightContext(options, boundary)
{
    protected override void ConfigureModel(ModelBuilder builder) =>
        PreflightContext.Build(builder, "kofte_system", ModelBoundary.Provider);
}

public sealed class UnsafeMigrationContext(DbContextOptions options, ModuleStorageModelBoundary boundary)
    : PreflightContext(options, boundary);

[DbContext(typeof(UnsafeMigrationContext)), Migration("202610240001_Initial")]
public sealed class UnsafePreflightMigration : PreflightInitial
{
    protected override void Up(MigrationBuilder builder) => builder.Sql("SELECT 1");
}

[DbContext(typeof(UnsafeMigrationContext))]
public sealed class UnsafePreflightSnapshot : PreflightSnapshot;

public sealed class ForeignCheckpointContext(DbContextOptions options, ModuleStorageModelBoundary boundary)
    : PreflightContext(options, boundary);

[DbContext(typeof(ForeignCheckpointContext)), Migration("202610240001_Initial")]
public sealed class ForeignCheckpointMigration : PreflightInitial
{
    protected override void BuildTargetModel(ModelBuilder builder) =>
        PreflightContext.Build(builder, "kofte_system", AssignedProvider);
}

[DbContext(typeof(ForeignCheckpointContext))]
public sealed class ForeignCheckpointSnapshot : PreflightSnapshot;
