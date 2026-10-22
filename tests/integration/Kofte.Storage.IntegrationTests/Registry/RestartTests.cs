using System.Data.Common;
using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Allocation;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Infrastructure.Storage.Sqlite;
using Kofte.Platform.Modules.Storage;
using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Registry;

public abstract class RestartTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task ThreeFullProcessRestartsRetainExactAssignmentsAndBothSentinels()
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, TestContext.Current.CancellationToken);
        StorageAssignment alpha;
        StorageAssignment beta;
        await using (var scope = await fixture.Maintenance.AcquireAsync(TestContext.Current.CancellationToken))
        {
            alpha = Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(
                RegistryFixture.Module("org.example.alpha"), scope, TestContext.Current.CancellationToken)).Assignment;
            beta = Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(
                RegistryFixture.Module("org.example.beta"), scope, TestContext.Current.CancellationToken)).Assignment;
        }
        await RegistrySentinel.WriteAsync(fixture, alpha, "alpha-preserved");
        await RegistrySentinel.WriteAsync(fixture, beta, "beta-preserved");
        for (var restart = 0; restart < 3; restart++)
        {
            var output = await OwnedProcesses.RunAsync(RegistrationConcurrencyTests.Start(fixture,
                ["--registration-probe"]), TimeSpan.FromSeconds(45), TestContext.Current.CancellationToken);
            Assert.Equal(0, output.ExitCode);
            using var document = JsonDocument.Parse(output.Stdout);
            var value = document.RootElement;
            Assert.Equal("storage_worker_ready", value.GetProperty("eventCode").GetString());
            Assert.Equal(alpha.AllocationId, value.GetProperty("allocation").GetGuid());
            Assert.Equal(alpha.InstallationId, value.GetProperty("installation").GetGuid());
            Assert.Equal(alpha.AllocationKey, value.GetProperty("key").GetString());
            Assert.Equal(alpha.Revision, value.GetProperty("revision").GetInt64());
            Assert.Equal("alpha-preserved", await RegistrySentinel.ReadAsync(fixture, alpha));
            Assert.Equal("beta-preserved", await RegistrySentinel.ReadAsync(fixture, beta));
            await fixture.AssertInventoryAsync(2, 2, TestContext.Current.CancellationToken);
        }
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteRestartTests : RestartTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresRestartTests : RestartTests
{
    protected override string Provider => "postgres";
}

internal static class RegistrySentinel
{
    private static string Table(StorageAssignment assignment) =>
        (assignment.Provider == "postgresql" ? StorageAllocator.Quote(assignment.AllocationKey) + "." : "") + "Records";

    private static async Task<DbConnection> OpenAsync(RegistryFixture fixture, StorageAssignment assignment,
        OperationBudget budget) => assignment.Provider == "sqlite"
        ? StorageInstallation.OpenSqlite(new ModuleStorePaths(fixture.Installation, assignment.AllocationKey).DatabasePath, budget)
        : await fixture.Installation.OpenPlatformAsync(budget);

    internal static async Task WriteAsync(RegistryFixture fixture, StorageAssignment assignment, string value)
    {
        using var budget = new OperationBudget(OperationBudget.Register, TestContext.Current.CancellationToken);
        await using var connection = await OpenAsync(fixture, assignment, budget);
        await RegistryMigrationRunner.ExecuteAsync(connection, $"""
            CREATE TABLE {Table(assignment)} (Id INTEGER PRIMARY KEY, Value TEXT NOT NULL);
            INSERT INTO {Table(assignment)} (Id,Value) VALUES (1,@value)
            """, budget, null, ("value", value));
    }

    internal static async Task<string> ReadAsync(RegistryFixture fixture, StorageAssignment assignment)
    {
        using var budget = new OperationBudget(OperationBudget.Register, TestContext.Current.CancellationToken);
        await using var connection = await OpenAsync(fixture, assignment, budget);
        return Assert.IsType<string>(await RegistryMigrationRunner.ScalarAsync(connection,
            $"SELECT Value FROM {Table(assignment)} WHERE Id=1", budget));
    }
}
