using System.Diagnostics;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Modules.Alpha;
using Kofte.Storage.IntegrationTests.Modules.Beta;
using Kofte.Storage.IntegrationTests.Providers;
using Kofte.TestSupport;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kofte.Storage.IntegrationTests.Recovery;

[Trait("Provider", "postgres"), Trait("Suite", "recovery"), Trait("Suite", "privacy")]
public sealed class CorrectedRetryTests
{
    [Fact]
    public async Task RealProviderOutageIsBoundedAndCorrectedRetryPreservesBothOwnersRowsAndHistory()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await ModuleFixture.CreateAsync("postgres", token, sharePostgres: false);
        await using (var scope = await fixture.Maintenance.AcquireAsync(token))
        {
            Assert.IsType<StorageResult.Ready>(await fixture.Alpha.MigrateAsync(
                ModuleFixture.AlphaFactory, scope, cancellationToken: token));
            Assert.IsType<StorageResult.Ready>(await fixture.Beta.MigrateAsync(
                ModuleFixture.BetaFactory, scope, cancellationToken: token));
            await using var alpha = await fixture.Alpha.CreateContextAsync(ModuleFixture.AlphaFactory, token);
            await using var beta = await fixture.Beta.CreateContextAsync(ModuleFixture.BetaFactory, token);
            alpha.Records.Add(new AlphaRecord { Id = 1, Payload = "outage-alpha-retained" });
            beta.Records.Add(new BetaRecord { Id = 1, Payload = "outage-beta-retained" });
            await alpha.SaveChangesAsync(token);
            await beta.SaveChangesAsync(token);
        }
        var original = Assert.IsType<StorageResult.Ready>(await fixture.Alpha.ResolveAsync(token)).Assignment;
        StorageResult failed;
        var elapsed = TimeSpan.Zero;
        await fixture.Resources.Postgres!.SetAvailableAsync(false, token);
        try
        {
            var started = Stopwatch.GetTimestamp();
            failed = StorageResultChecks.Check(await fixture.Registry.ResolveAsync(original.Owner, token));
            elapsed = Stopwatch.GetElapsedTime(started);
        }
        finally
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            await fixture.Resources.Postgres.SetAvailableAsync(true, cleanup.Token);
        }
        Assert.Equal(StorageOutcome.Failed, failed.Outcome);
        Assert.Contains(failed.Diagnostics[0].Code, new[] { "storage_deadline_exceeded", "storage_unavailable" });
        Assert.True(elapsed < OperationBudget.Resolve);
        var retried = Assert.IsType<StorageResult.Ready>(await fixture.Alpha.ResolveAsync(token));
        Assert.Equal(original.AllocationId, retried.Assignment.AllocationId);
        Assert.Equal(original.Revision, retried.Assignment.Revision);
        Assert.NotEqual(failed.CorrelationId, retried.CorrelationId);
        await using var alphaRead = await fixture.Alpha.CreateContextAsync(ModuleFixture.AlphaFactory, token);
        await using var betaRead = await fixture.Beta.CreateContextAsync(ModuleFixture.BetaFactory, token);
        Assert.Equal("outage-alpha-retained", (await alphaRead.Records.SingleAsync(token)).Payload);
        Assert.Equal("outage-beta-retained", (await betaRead.Records.SingleAsync(token)).Payload);
        Assert.Equal(2, (await alphaRead.Database.GetAppliedMigrationsAsync(token)).Count());
        Assert.Single(await betaRead.Database.GetAppliedMigrationsAsync(token));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("ownership")]
    [InlineData("privilege")]
    public async Task CorrectedOfflineAuthorityFinishesTheOriginalReservationWithoutFallback(string fault)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await ModuleFixture.CreateAsync("postgres", token);
        var provider = fixture.Resources.Postgres!;
        var owner = ModuleFixture.Module("org.example.offline-correction");
        var installation = StorageInstallation.PostgreSql(provider.ConnectionString,
            fault == "missing" ? null : provider.ProvisioningConnectionString);
        var denied = new StorageRegistry(installation);
        if (fault == "ownership") await provider.SetProvisionerOwnershipAsync(false, token);
        if (fault == "privilege") await provider.SetProvisioningPrivilegeAsync(false, token);
        StorageResult failed;
        try
        {
            await using var scope = await new StorageMaintenance(installation).AcquireAsync(token);
            failed = StorageResultChecks.Check(await denied.RegisterAsync(owner, scope, token));
            Assert.Equal("storage_privilege_denied", failed.Diagnostics[0].Code);
        }
        finally
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            if (fault == "ownership") await provider.SetProvisionerOwnershipAsync(true, cleanup.Token);
            if (fault == "privilege") await provider.SetProvisioningPrivilegeAsync(true, cleanup.Token);
        }
        var before = await StorageInspection.ReadAsync("postgres", null, provider.ConnectionString, owner.Id, token);
        Assert.NotNull(before);
        Assert.Equal("reserved", before.State);
        await using var corrected = await fixture.Maintenance.AcquireAsync(token);
        var ready = Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(owner, corrected, token));
        Assert.Equal(before.AllocationId, ready.Assignment.AllocationId);
        Assert.Equal(before.Key, ready.Assignment.AllocationKey);
        Assert.NotEqual(failed.CorrelationId, ready.CorrelationId);
        var mismatch = new NpgsqlConnectionStringBuilder(provider.ProvisioningConnectionString)
        { Database = "kofte_test_mismatched" };
        Assert.Throws<ArgumentException>(() =>
            StorageInstallation.PostgreSql(provider.ConnectionString, mismatch.ConnectionString));
        Assert.IsType<StorageResult.Ready>(await fixture.Alpha.ResolveAsync(token));
        Assert.IsType<StorageResult.Ready>(await fixture.Beta.ResolveAsync(token));
    }
}
