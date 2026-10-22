using System.Data.Common;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Kofte.Storage.IntegrationTests.Registry;

public abstract class RegistryConstraintTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task DirectConstraintBypassCannotDuplicateOwnershipOrPersistInvalidState()
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, TestContext.Current.CancellationToken);
        using var budget = new OperationBudget(OperationBudget.Register, TestContext.Current.CancellationToken);
        await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
        var runner = new RegistryMigrationRunner(fixture.Installation);
        var token = Guid.NewGuid();
        const string key = "m_alpha_00000000000000000000000000000000";
        const string other = "m_other_00000000000000000000000000000000";
        Task Insert(string owner, string? candidate, Guid? allocation, string state = "reserved",
            long revision = 1, bool relational = true, string migration = "none", string? target = null) =>
            RegistryMigrationRunner.ExecuteAsync(connection, $"""
                INSERT INTO {runner.Registrations}
                (module_id,relational,provider,allocation_key,allocator_version,allocation_id,state,migration_state,migration_target,revision)
                VALUES (@owner,@relational,@provider,@key,@version,@allocation,@state,@migration,@target,@revision)
                """, budget, null, ("owner", owner), ("relational", relational),
                ("provider", fixture.Installation.Provider), ("key", candidate is null ? DBNull.Value : candidate),
                ("version", candidate is null ? DBNull.Value : 1),
                ("allocation", allocation is null ? DBNull.Value : allocation.Value),
                ("state", state), ("migration", migration), ("target", target is null ? DBNull.Value : target),
                ("revision", revision));
        await Insert("org.example.alpha", key, token);
        await ConstraintAsync(() => Insert("org.example.alpha", other, Guid.NewGuid()));
        await ConstraintAsync(() => Insert("org.example.beta", key, Guid.NewGuid()));
        await ConstraintAsync(() => Insert("org.example.beta", other, token));
        await ConstraintAsync(() => Insert("org.example.beta", other, Guid.Empty));
        await ConstraintAsync(() => Insert("org.example.beta", other, Guid.NewGuid(), revision: 0));
        await ConstraintAsync(() => Insert("org.example.beta", other, Guid.NewGuid(), state: "invented"));
        await ConstraintAsync(() => Insert("org.example.beta", other, Guid.NewGuid(), migration: "invented"));
        await ConstraintAsync(() => Insert("org.example.beta", other, Guid.NewGuid(), target: new string('m', 151)));
        await ConstraintAsync(() => Insert("org.example.beta", other, Guid.NewGuid(), target: "\u0001"));
        await ConstraintAsync(() => Insert("org.example.beta", other, Guid.NewGuid(), relational: false));
        await ConstraintAsync(() => Insert("org.example.beta", null, null));
        foreach (var candidate in new[] { "pg_catalog", "kofte_system", key.ToUpperInvariant(),
                     "m_" + new string('a', 30) + "_" + new string('0', 32), "m_å_" + new string('0', 32) })
            await ConstraintAsync(() => Insert("org.example.beta", candidate, Guid.NewGuid()));
        foreach (var owner in new[] { "ORG.example.beta", "org..beta", "org.bad-.beta",
                     "org.-bad.beta", "org.two", "org.example.beta." })
            await ConstraintAsync(() => Insert(owner, other, Guid.NewGuid()));
        Assert.Equal(1L, await RegistryMigrationRunner.ScalarAsync(connection,
            $"SELECT count(*) FROM {runner.Registrations}", budget));
        Assert.Equal(1, (await runner.ReadInstallationAsync(connection, budget)).Revision);
        await ConstraintAsync(() => RegistryMigrationRunner.ExecuteAsync(connection,
            $"UPDATE {runner.Registrations} SET allocation_key=@key,revision=revision+1", budget, null, ("key", other)));
        await ConstraintAsync(() => RegistryMigrationRunner.ExecuteAsync(connection,
            $"UPDATE {runner.Registrations} SET state='ready',revision=revision+2", budget));
        await ConstraintAsync(() => RegistryMigrationRunner.ExecuteAsync(connection,
            $"DELETE FROM {runner.Registrations}", budget));
    }

    private static async Task ConstraintAsync(Func<Task> action)
    {
        var error = await Assert.ThrowsAnyAsync<DbException>(action);
        if (error is SqliteException sqlite) Assert.Equal(19, sqlite.SqliteErrorCode);
        else
        {
            var postgres = Assert.IsType<PostgresException>(error);
            Assert.Contains(postgres.SqlState, new[] { PostgresErrorCodes.UniqueViolation,
                PostgresErrorCodes.CheckViolation, PostgresErrorCodes.RaiseException });
        }
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteRegistryConstraintTests : RegistryConstraintTests
{
    protected override string Provider => "sqlite";

    [Fact]
    public async Task InconsistentReopenAndUnownedEmptyPlatformAreRejectedWithoutWrites()
    {
        await using var fixture = await RegistryFixture.CreateAsync("sqlite", TestContext.Current.CancellationToken);
        using (var budget = new OperationBudget(OperationBudget.Register, TestContext.Current.CancellationToken))
        await using (var connection = await fixture.Installation.OpenPlatformAsync(budget))
            await RegistryMigrationRunner.ExecuteAsync(connection, "PRAGMA user_version=2", budget);
        var path = System.IO.Path.Combine(fixture.Root!, "platform", "platform.db");
        var before = await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken);
        var result = await fixture.Registry.ResolveAsync(RegistryFixture.Module("org.example.alpha").Id,
            TestContext.Current.CancellationToken);
        Assert.Equal(StorageOutcome.ReconciliationRequired, result.Outcome);
        Assert.Equal(before, await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
        File.WriteAllBytes(path, []);
        await using var scope = await fixture.Maintenance.AcquireAsync(TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<StorageException>(() => fixture.Maintenance.UpgradeAsync(
            scope, TestContext.Current.CancellationToken));
        Assert.Empty(await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken));
    }
}

[Trait("Provider", "postgres")]
public sealed class PostgresRegistryConstraintTests : RegistryConstraintTests
{
    protected override string Provider => "postgres";
}
