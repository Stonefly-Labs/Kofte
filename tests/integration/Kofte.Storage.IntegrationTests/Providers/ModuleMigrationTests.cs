using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Modules.Beta;
using Microsoft.EntityFrameworkCore;

namespace Kofte.Storage.IntegrationTests.Providers;

public abstract class ModuleMigrationTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task CleanInstallAndAlphaAdditiveUpgradeKeepBetaPlatformAndHistoryIsolated()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await ModuleFixture.CreateAsync(Provider, token);
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        var alpha = await fixture.Alpha.MigrateAsync(ModuleFixture.AlphaFactory, scope, "202610210001_Initial", token);
        Assert.IsType<StorageResult.Ready>(alpha);
        Assert.IsType<StorageResult.Ready>(await fixture.Beta.MigrateAsync(ModuleFixture.BetaFactory, scope,
            cancellationToken: token));
        await using (var context = await fixture.Alpha.CreateContextAsync(ModuleFixture.AlphaFactory, token))
        {
            var sql = fixture.Postgres
                ? $"INSERT INTO {RawSqlTarget(fixture.Alpha.Schema!)} VALUES (1, 'alpha-sentinel')"
                : "INSERT INTO Records (Id, Payload) VALUES (1, 'alpha-sentinel')";
            await context.Database.ExecuteSqlRawAsync(sql, token);
        }
        await using (var context = await fixture.Beta.CreateContextAsync(ModuleFixture.BetaFactory, token))
        {
            context.Records.Add(new BetaRecord { Id = 1, Payload = "beta-sentinel" });
            await context.SaveChangesAsync(token);
        }
        var betaAssignment = Assert.IsType<StorageResult.Ready>(await fixture.Beta.ResolveAsync(token)).Assignment;
        var platformBefore = await fixture.PlatformRevisionAsync(token);
        Assert.IsType<StorageResult.Ready>(await fixture.Alpha.MigrateAsync(ModuleFixture.AlphaFactory, scope,
            cancellationToken: token));
        await using (var context = await fixture.Alpha.CreateContextAsync(ModuleFixture.AlphaFactory, token))
        {
            var row = await context.Records.SingleAsync(token);
            Assert.Equal("alpha-sentinel", row.Payload);
            Assert.Null(row.Note);
            row.Note = "additive";
            await context.SaveChangesAsync(token);
            Assert.Equal(2, (await context.Database.GetAppliedMigrationsAsync(token)).Count());
        }
        await using (var context = await fixture.Beta.CreateContextAsync(ModuleFixture.BetaFactory, token))
        {
            Assert.Equal("beta-sentinel", (await context.Records.SingleAsync(token)).Payload);
            Assert.Single(await context.Database.GetAppliedMigrationsAsync(token));
            var after = Assert.IsType<StorageResult.Ready>(await fixture.Beta.ResolveAsync(token)).Assignment;
            Assert.Equal(betaAssignment.Revision, after.Revision);
            Assert.Equal(betaAssignment.AllocationId, after.AllocationId);
            Assert.DoesNotContain("Note", context.Model.FindEntityType(typeof(BetaRecord))!.GetProperties().Select(p => p.Name));
        }
        Assert.Equal(platformBefore + 2, await fixture.PlatformRevisionAsync(token));
        var contender = new StorageMaintenance(fixture.Installation);
        var busy = await Assert.ThrowsAsync<StorageException>(() => contender.AcquireAsync(token));
        Assert.Equal(StorageCode.Busy, busy.Code);
    }

    private static string RawSqlTarget(string schema) => $"\"{schema}\".\"Records\" (\"Id\", \"Payload\")";
}

[Trait("Provider", "sqlite")]
public sealed class SqliteModuleMigrationTests : ModuleMigrationTests
{ protected override string Provider => "sqlite"; }
[Trait("Provider", "postgres")]
public sealed class PostgresModuleMigrationTests : ModuleMigrationTests
{ protected override string Provider => "postgres"; }
