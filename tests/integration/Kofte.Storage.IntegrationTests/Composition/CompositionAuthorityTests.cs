using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.Json;
using Kofte.Composition.TestModule;
using Kofte.Infrastructure.Composition;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Providers;
using Kofte.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Kofte.Storage.IntegrationTests.Composition;

[Trait("Feature", "composition")]
public abstract class CompositionAuthorityTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task KilledLaterMigrationRetainsEarlierCommitAndOriginalDataUntilExplicitAcceptedRecovery()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync(Provider, token);
        await fixture.SeedUnrelatedNoStorageAsync(token);
        var installation = Installation(fixture);
        var registry = new StorageRegistry(installation);
        var maintenance = new StorageMaintenance(installation);
        var unrelated = ModuleFixture.Module("org.example.composition-unrelated");
        await using (var scope = await maintenance.AcquireAsync(token))
        {
            Assert.IsType<StorageResult.Ready>(await registry.RegisterAsync(unrelated, scope, token));
            await using var binding = await registry.BindAsync(unrelated.Id, token);
            Assert.IsType<StorageResult.Ready>(await binding.MigrateAsync(ModuleFixture.AlphaFactory,
                scope, cancellationToken: token));
            await using var context = await binding.CreateContextAsync(ModuleFixture.AlphaFactory, token);
            context.Records.Add(new() { Id = 71, Payload = "unrelated-original", Note = "retain-unchanged" });
            await context.SaveChangesAsync(token);
        }
        var originalUnrelated = await ReadUnrelatedAsync(registry, unrelated.Id, token);
        var start = StorageCrashWorker.Start(Assembly.GetExecutingAssembly(), Provider, "compose",
            CompositionPreparationCrashWorker.Phase, StorageRecipes.Descriptions[1].Id.Value,
            child => fixture.Storage.ConfigureChild(child, offlineMaintenance: true));
        var started = Stopwatch.GetTimestamp();
        var killed = await StorageCrashWorker.KillAtAsync(start, CompositionPreparationCrashWorker.Phase, token);
        Assert.NotEqual(0, killed.ExitCode);
        Assert.True(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(15));
        var first = StorageRecipes.Descriptions[0].Id;
        var later = StorageRecipes.Descriptions[1].Id;
        var earlier = Assert.IsType<StorageResult.Ready>(await registry.ResolveAsync(first, token));
        await using (var binding = await registry.BindAsync(first, token))
        await using (var context = await StorageRecipes.CreateAlphaContextAsync(binding, token: token))
        {
            Assert.Equal(new[] { StorageRecipes.AlphaInitial, StorageRecipes.AlphaCurrent },
                await context.Database.GetAppliedMigrationsAsync(token));
            Assert.Empty(await context.Records.ToArrayAsync(token));
        }
        var interrupted = await RegistrationAsync(installation, later, token);
        Assert.Equal("in_progress", interrupted.MigrationState);
        Assert.Equal(StorageRecipes.BetaCurrent, interrupted.Target);
        Assert.Null(interrupted.Checkpoint);
        Assert.Equal(StorageOutcome.ReconciliationRequired, (await registry.ResolveAsync(later, token)).Outcome);
        Assert.Equal(originalUnrelated, await ReadUnrelatedAsync(registry, unrelated.Id, token));
        var beforeRefusal = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        await using (var refused = await CompositionStorageSession.OpenPrepareAsync(installation, token))
        {
            var error = await Assert.ThrowsAsync<StorageException>(() =>
                refused.InspectAsync(StorageRecipes.Descriptions, StorageRecipes.ForRelease(), serving: false, token));
            Assert.Equal(StorageCode.ReconciliationRequired, error.Code);
        }
        Assert.Equal(beforeRefusal, await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
        await using (var scope = await maintenance.AcquireAsync(token))
        {
            var recovered = Assert.IsType<StorageResult.Ready>(await ModuleMigrationRecovery.RecoverAsync(
                registry, later, StorageRecipes.BetaFactory(), scope, token));
            Assert.Equal(StorageOperation.Migrate, recovered.Operation);
            Assert.Equal(StorageDurability.Committed, recovered.Durability);
        }
        var recoveredRow = await RegistrationAsync(installation, later, token);
        Assert.Equal((interrupted.AllocationId, interrupted.Key, interrupted.AllocatorVersion, interrupted.Owner),
            (recoveredRow.AllocationId, recoveredRow.Key, recoveredRow.AllocatorVersion, recoveredRow.Owner));
        Assert.Equal("current", recoveredRow.MigrationState);
        Assert.Equal(StorageRecipes.BetaCurrent, recoveredRow.Checkpoint);
        await using (var binding = await registry.BindAsync(later, token))
        await using (var context = await StorageRecipes.CreateBetaContextAsync(binding, token: token))
            Assert.Equal(new[] { StorageRecipes.BetaInitial, StorageRecipes.BetaCurrent },
                await context.Database.GetAppliedMigrationsAsync(token));
        Assert.Equal(JsonSerializer.Serialize(earlier.Assignment),
            JsonSerializer.Serialize(Assert.IsType<StorageResult.Ready>(await registry.ResolveAsync(first, token)).Assignment));
        Assert.Equal(originalUnrelated, await ReadUnrelatedAsync(registry, unrelated.Id, token));
        var beforeFresh = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        await using (var fresh = await CompositionStorageSession.OpenReadAsync(installation, token))
        {
            var owners = StorageRecipes.Descriptions.Take(2).ToArray();
            var recipes = StorageRecipes.ForRelease();
            await fresh.InspectAsync(owners, recipes, serving: true, token);
            Assert.Equal(2, (await fresh.VerifyAsync(owners, recipes, token)).Count);
        }
        Assert.Equal(beforeFresh, await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ActualCommandsRefuseIncompatibleRetainedAuthorityWithoutChangingTheInstallation(bool readHeld)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync(Provider, token);
        await fixture.SeedUnrelatedNoStorageAsync(token);
        await fixture.WriteSelectionAsync("""{"schemaVersion":"1","modules":[]}"""u8.ToArray(), token);
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        var installation = Installation(fixture);
        await using (var authority = readHeld
            ? await CompositionStorageSession.OpenReadAsync(installation, token)
            : await CompositionStorageSession.OpenPrepareAsync(installation, token))
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var started = Stopwatch.GetTimestamp();
                await using var command = fixture.Start(readHeld ? "prepare" : "validate", CompositionTestCatalog.EmptyBoot);
                var result = await command.WaitAsync(token);
                var records = Records(result.Stdout);
                Assert.Equal("busy", records[^1].GetProperty("outcome").GetString());
                Assert.Equal(3, result.ExitCode);
                Assert.True(command.ExitVerified && command.DescendantExitsVerified);
                Assert.True(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(5));
                Assert.Empty(result.Stderr);
                Assert.Single(records, record => record.GetProperty("stage").GetString() == "admission");
                Assert.DoesNotContain(records, record => record.GetProperty("stage").GetString() is "registered" or "published");
                await authority.EnsureAuthorityAsync(token);
            }
        }
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
    }

    protected static StorageInstallation Installation(CompositionFixture fixture) =>
        fixture.Storage.Postgres is { } postgres
            ? StorageInstallation.PostgreSql(postgres.ConnectionString, postgres.ProvisioningConnectionString)
            : StorageInstallation.Sqlite(fixture.Storage.DataRoot);

    protected static JsonElement[] Records(string output) => output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .Select(line => JsonSerializer.Deserialize<JsonElement>(line)).ToArray();

    private static async Task<string> ReadUnrelatedAsync(StorageRegistry registry, ModuleId owner, CancellationToken token)
    {
        var ready = Assert.IsType<StorageResult.Ready>(await registry.ResolveAsync(owner, token));
        await using var binding = await registry.BindAsync(owner, token);
        await using var context = await binding.CreateContextAsync(ModuleFixture.AlphaFactory, token);
        return JsonSerializer.Serialize(new
        {
            ready.Assignment,
            history = await context.Database.GetAppliedMigrationsAsync(token),
            records = await context.Records.AsNoTracking().OrderBy(record => record.Id).ToArrayAsync(token)
        });
    }

    private static async Task<RegistrationRecord> RegistrationAsync(StorageInstallation installation,
        ModuleId owner, CancellationToken token)
    {
        using var budget = new OperationBudget(OperationBudget.Resolve, token);
        await using var connection = await installation.OpenPlatformAsync(budget);
        return await new RegistryMigrationRunner(installation).FindAsync(connection, owner, budget) ??
            throw new InvalidOperationException("composition_test_registration_missing");
    }
}

[Trait("Provider", "sqlite"), Trait("Suite", "recovery")]
public sealed class SqliteCompositionAuthorityTests : CompositionAuthorityTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres"), Trait("Suite", "recovery")]
public sealed class PostgresCompositionAuthorityTests : CompositionAuthorityTests
{
    protected override string Provider => "postgres";

    [Fact]
    public async Task ActualServingSessionLossTerminatesWithoutReacquiringAndOnlyExplicitFreshServingRetries()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync(Provider, token);
        await fixture.SeedUnrelatedNoStorageAsync(token);
        await fixture.WriteSelectionAsync("""{"schemaVersion":"1","modules":[]}"""u8.ToArray(), token);
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        string firstAttempt;
        await using (var serving = fixture.StartBoot(CompositionTestCatalog.EmptyBoot))
        {
            await WaitReadyAsync(fixture, serving, token);
            var postgres = fixture.Storage.Postgres!;
            var started = Stopwatch.GetTimestamp();
            await CompositionLeaseProbe.TerminateAsync(postgres, token);
            var result = await serving.WaitAsync(token);
            Assert.Equal(1, result.ExitCode);
            Assert.True(serving.ExitVerified && serving.DescendantExitsVerified);
            Assert.True(Stopwatch.GetElapsedTime(started) < TimeSpan.FromSeconds(10));
            var records = Records(result.Stdout);
            firstAttempt = records[0].GetProperty("attempt").GetString()!;
            Assert.Single(records, record => record.GetProperty("stage").GetString() == "admission");
            Assert.Equal("composition_cleanup_incomplete", records[^1].GetProperty("code").GetString());
            Assert.Equal("incomplete", records[^1].GetProperty("outcome").GetString());
            Assert.Equal(0, await CompositionLeaseProbe.CountAsync(postgres, token));
        }
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
        await using (var fresh = fixture.StartBoot(CompositionTestCatalog.EmptyBoot))
        {
            await WaitReadyAsync(fixture, fresh, token);
            await fresh.StopAsync();
            var result = await fresh.ResultAsync();
            Assert.Equal(0, result.ExitCode);
            Assert.NotEqual(firstAttempt, Records(result.Stdout)[0].GetProperty("attempt").GetString());
            Assert.Equal("passed", Records(result.Stdout)[^1].GetProperty("outcome").GetString());
        }
        Assert.Equal(before, await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
    }

    private static async Task WaitReadyAsync(CompositionFixture fixture, CompositionProcess process, CancellationToken token)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromSeconds(15));
        using var http = new HttpClient { BaseAddress = fixture.HostUrl, Timeout = TimeSpan.FromSeconds(2) };
        while (!process.HasExited)
        {
            try
            {
                using var response = await http.GetAsync("/health/ready", budget.Token);
                if (response.StatusCode == HttpStatusCode.OK) return;
            }
            catch (HttpRequestException) { }
            await Task.Delay(20, budget.Token);
        }
        throw new InvalidOperationException("composition_test_serving_exited");
    }

}
