using System.Reflection;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Recovery;

public abstract class ProvisioningCrashTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task KillingEveryReservationPhysicalAndReadyBoundaryPreservesTheOriginalAllocation()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync(Provider, token);
        var phases = StorageCrashWorker.RegistrationPhases.Concat(
            Provider == "sqlite" ? StorageCrashWorker.SqliteCreationPhases : []).ToArray();
        foreach (var phase in phases)
        {
            var owner = RegistryFixture.Module("org.example.crash-" + phase.Replace('_', '-'));
            var start = StorageCrashWorker.Start(Assembly.GetExecutingAssembly(), Provider, "register", phase,
                owner.Id.Value, child => Configure(fixture, child));
            await StorageCrashWorker.KillAtAsync(start, phase, token);
            using var budget = new OperationBudget(OperationBudget.Resolve, token);
            await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
            var before = await new RegistryMigrationRunner(fixture.Installation).FindAsync(connection, owner.Id, budget);
            var resolved = await fixture.Registry.ResolveAsync(owner.Id, token);
            Assert.Equal(phase switch
            {
                "reservation_before_commit" => StorageOutcome.Rejected,
                "ready_committed" => StorageOutcome.Ready,
                _ => StorageOutcome.Incomplete
            }, resolved.Outcome);
            var preserved = Provider == "sqlite" && before is not null
                ? Directory.EnumerateFiles(Path.Combine(fixture.Root!, "modules"), "*", SearchOption.AllDirectories)
                    .ToDictionary(path => path, File.ReadAllBytes)
                : [];
            await using var scope = await fixture.Maintenance.AcquireAsync(token);
            var retried = await fixture.Registry.RegisterAsync(owner, scope, token);
            if (phase is "sqlite_directory_created" or "sqlite_file_created" ||
                Provider == "sqlite" && phase == "physical_before_commit")
            {
                Assert.Equal(StorageOutcome.ReconciliationRequired, retried.Outcome);
                var path = Path.Combine(fixture.Root!, "modules", before!.Key!);
                Assert.True(Directory.Exists(path));
                Assert.Equal(phase != "sqlite_directory_created", File.Exists(Path.Combine(path, "module.db")));
                Assert.All(preserved, item => Assert.Equal(item.Value, File.ReadAllBytes(item.Key)));
            }
            else
            {
                var actual = Assert.IsType<StorageResult.Ready>(retried).Assignment;
                if (before is not null)
                {
                    Assert.Equal(before.AllocationId, actual.AllocationId);
                    Assert.Equal(before.Key, actual.AllocationKey);
                }
                Assert.IsType<StorageResult.Ready>(await fixture.Registry.ResolveAsync(owner.Id, token));
            }
        }
    }

    internal static void Configure(RegistryFixture fixture, System.Diagnostics.ProcessStartInfo child)
    {
        OwnedProcesses.ClearSensitiveEnvironment(child);
        if (fixture.Root is { } root) child.Environment["KOFTE_DATA_ROOT"] = root;
        if (fixture.RuntimeConnection is { } runtime) child.Environment["KOFTE_POSTGRES_CONNECTION"] = runtime;
        if (fixture.ProvisioningConnection is { } provisioning)
            child.Environment["KOFTE_POSTGRES_PROVISIONING_CONNECTION"] = provisioning;
    }
}

[Trait("Provider", "sqlite"), Trait("Suite", "recovery")]
public sealed class SqliteProvisioningCrashTests : ProvisioningCrashTests
{ protected override string Provider => "sqlite"; }
[Trait("Provider", "postgres"), Trait("Suite", "recovery")]
public sealed class PostgresProvisioningCrashTests : ProvisioningCrashTests
{ protected override string Provider => "postgres"; }
