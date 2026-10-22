using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.TestSupport;

namespace Kofte.Platform.ContractTests.Modules.Storage;

public abstract class StorageDiagnosticsTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task PublicSerializationIsBoundedAndOnlyReadyExposesAssignmentMetadata()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await StorageProviderScope.CreateAsync(Provider, token);
        var installation = Provider == "sqlite" ? StorageInstallation.Sqlite(fixture.DataRoot) :
            StorageInstallation.PostgreSql(fixture.Postgres!.ConnectionString, fixture.Postgres.ProvisioningConnectionString);
        var registry = new StorageRegistry(installation);
        var maintenance = new StorageMaintenance(installation);
        var owner = StorageCrashWorker.Description("org.example.public-diagnostics");
        await using var scope = await maintenance.AcquireAsync(token);
        await maintenance.UpgradeAsync(scope, token);
        var ready = StorageResultChecks.Check(await registry.RegisterAsync(owner, scope, token));
        Assert.IsType<StorageResult.Ready>(ready);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var failed = StorageResultChecks.Check(await registry.RegisterAsync(owner, scope, cancelled.Token));
        Assert.Equal(StorageOutcome.Failed, failed.Outcome);
        Assert.Equal("storage_cancelled", Assert.Single(failed.Diagnostics).Code);
        Assert.NotEqual(ready.CorrelationId, failed.CorrelationId);
        await using var blocked = await StorageReadBlock.CreateAsync(Provider, fixture.DataRoot,
            fixture.Postgres?.ConnectionString, token);
        var busy = StorageResultChecks.Check(await new StorageRegistry(blocked.Reader).ResolveAsync(owner.Id, token));
        Assert.Equal(StorageOutcome.Busy, busy.Outcome);
        Assert.Equal("retry_same_request", Assert.Single(busy.Diagnostics).Guidance);
    }
}

[Trait("Provider", "sqlite"), Trait("Suite", "privacy")]
public sealed class SqliteStorageDiagnosticsTests : StorageDiagnosticsTests
{ protected override string Provider => "sqlite"; }
[Trait("Provider", "postgres"), Trait("Suite", "privacy")]
public sealed class PostgresStorageDiagnosticsTests : StorageDiagnosticsTests
{ protected override string Provider => "postgres"; }
