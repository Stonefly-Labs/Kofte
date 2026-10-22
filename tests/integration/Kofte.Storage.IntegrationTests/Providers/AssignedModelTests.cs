using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Kofte.Storage.IntegrationTests.Modules.Alpha;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kofte.Storage.IntegrationTests.Providers;

public abstract class AssignedModelTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task SameContextTypeUsesDistinctAssignedModelsAndDesignTimeCacheKeys()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await ModuleFixture.CreateAsync(Provider, token);
        await using var alpha = await fixture.Alpha.CreateContextAsync(ModuleFixture.AlphaFactory, token);
        await using var beta = await fixture.Beta.CreateContextAsync(ModuleFixture.AlphaFactory, token);
        Assert.Equal(alpha.GetType(), beta.GetType());
        var factory = new AssignedModelCacheKeyFactory();
        Assert.NotEqual(factory.Create(alpha, false), factory.Create(alpha, true));
        Assert.Equal(factory.Create(alpha, false), factory.Create(alpha, false));
        if (fixture.Postgres)
        {
            Assert.NotSame(alpha.Model, beta.Model);
            Assert.NotEqual(factory.Create(alpha, false), factory.Create(beta, false));
            Assert.Equal(fixture.Alpha.Schema, alpha.Model.FindEntityType(typeof(AlphaRecord))!.GetSchema());
            Assert.Equal(fixture.Beta.Schema, beta.Model.FindEntityType(typeof(AlphaRecord))!.GetSchema());
        }
        else
        {
            Assert.Null(alpha.Model.GetDefaultSchema());
            Assert.Null(beta.Model.FindEntityType(typeof(AlphaRecord))!.GetSchema());
            Assert.NotEqual(alpha.Database.GetDbConnection().DataSource, beta.Database.GetDbConnection().DataSource);
        }
        var assembly = alpha.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrationsAssembly>();
        Assert.IsAssignableFrom<AssignedModelSnapshot>(assembly.ModelSnapshot);
        Assert.Equal(fixture.Alpha.Schema, assembly.ModelSnapshot!.Model.GetDefaultSchema());
        Assert.All(assembly.Migrations.Values, type =>
        {
            var migration = assembly.CreateMigration(type, alpha.Database.ProviderName!);
            Assert.All(migration.UpOperations, operation =>
                MigrationBoundaryValidator.ValidateOperation(operation, fixture.Alpha));
        });
        Assert.Equal(2, MigrationBoundaryValidator.Preflight(alpha, [], null).Count);
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteAssignedModelTests : AssignedModelTests
{ protected override string Provider => "sqlite"; }
[Trait("Provider", "postgres")]
public sealed class PostgresAssignedModelTests : AssignedModelTests
{
    protected override string Provider => "postgres";

    [Fact]
    public async Task SameContextTypeSeparatesProvidersAsWellAsDesignTimeModels()
    {
        var token = TestContext.Current.CancellationToken;
        await using var sqlite = await ModuleFixture.CreateAsync("sqlite", token);
        await using var postgres = await ModuleFixture.CreateAsync("postgres", token);
        await using var local = await sqlite.Alpha.CreateContextAsync(
            (options, binding) => new SharedContext(options, binding), token);
        await using var server = await postgres.Alpha.CreateContextAsync(
            (options, binding) => new SharedContext(options, binding), token);
        var factory = new AssignedModelCacheKeyFactory();
        Assert.Equal(local.GetType(), server.GetType());
        Assert.NotEqual(factory.Create(local, false), factory.Create(server, false));
        Assert.NotEqual(factory.Create(server, false), factory.Create(server, true));
        Assert.NotSame(local.Model, server.Model);
        Assert.Null(local.Model.GetDefaultSchema());
        Assert.Equal(postgres.Alpha.Schema, server.Model.GetDefaultSchema());
    }

    private sealed class SharedContext(DbContextOptions options, ModuleStorageBinding binding) : AlphaContext(options, binding);
}
