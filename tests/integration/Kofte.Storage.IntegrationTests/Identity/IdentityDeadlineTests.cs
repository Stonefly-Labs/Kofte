using System.Data.Common;
using System.Diagnostics;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Storage.IntegrationTests.Registry;
using Microsoft.Data.Sqlite;

namespace Kofte.Storage.IntegrationTests.Identity;

public abstract class IdentityDeadlineTests
{
    protected abstract string Provider { get; }
    protected CancellationToken Token => TestContext.Current.CancellationToken;
    private string Users => Provider == "sqlite" ? "__kofte_users" : "kofte_system.__kofte_users";

    [Fact]
    public async Task OneSecondBudgetCanOpenReadWriteAndCommitAfterEarlierWork()
    {
        await using var fixture = await PreparedAsync();
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        await Task.Delay(250, Token);
        await using var store = await DeadlineStore.OpenAsync(fixture.Installation, budget);
        await store.BeginWriteAsync();
        var id = Guid.NewGuid();
        await InsertAsync(store, id, "deadline-owned-value");
        await store.CommitAsync();
        Assert.Equal("deadline-owned-value",
            Assert.Single(await store.QueryAsync($"SELECT display_name FROM {Users} WHERE user_id=@id",
                ("id", id)))[0]);
        Assert.True(budget.Remaining > TimeSpan.Zero);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RealLockContentionIsBoundedAndCannotWriteAfterReturn(bool callerCancellation)
    {
        await using var fixture = await PreparedAsync();
        using var setup = new OperationBudget(OperationBudget.Register, Token);
        await using var blocker = await fixture.Installation.OpenPlatformAsync(setup);
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(Token);
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), caller.Token);
        var timer = Stopwatch.StartNew();
        await using var transaction = blocker is SqliteConnection ? null : await blocker.BeginTransactionAsync(Token);
        await using (var store = await DeadlineStore.OpenAsync(fixture.Installation, budget))
        {
            await ExecuteAsync(blocker, Provider == "sqlite" ? "BEGIN EXCLUSIVE" :
                $"LOCK TABLE {Users} IN ACCESS EXCLUSIVE MODE", transaction);
            await Task.Delay(250, Token);
            if (callerCancellation) caller.CancelAfter(100);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await store.BeginWriteAsync();
                await InsertAsync(store, Guid.NewGuid(), "must-not-appear-later");
                await store.CommitAsync();
            });
        }
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(1), $"elapsed_ms={timer.Elapsed.TotalMilliseconds:F0}");
        if (transaction is not null) await transaction.RollbackAsync(Token);
        else await ExecuteAsync(blocker, "ROLLBACK");
        using var verify = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        await using var reopened = await DeadlineStore.OpenAsync(fixture.Installation, verify);
        Assert.Equal(0L, Assert.Single(await reopened.QueryAsync($"SELECT COUNT(*) FROM {Users}"))[0]);
        await reopened.BeginWriteAsync();
        await InsertAsync(reopened, Guid.NewGuid(), "corrected-request");
        await reopened.CommitAsync();
        Assert.Equal("corrected-request", Assert.Single(
            await reopened.QueryAsync($"SELECT display_name FROM {Users}"))[0]);
    }

    [Fact]
    public async Task CancellationBeforeCommitRollsBackTheActualTransaction()
    {
        await using var fixture = await PreparedAsync();
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(Token);
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), caller.Token);
        await using (var store = await DeadlineStore.OpenAsync(fixture.Installation, budget))
        {
            await store.BeginWriteAsync();
            await InsertAsync(store, Guid.NewGuid(), "uncommitted");
            caller.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.CommitAsync());
        }
        using var verify = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        await using var reopened = await DeadlineStore.OpenAsync(fixture.Installation, verify);
        Assert.Equal(0L, Assert.Single(await reopened.QueryAsync($"SELECT COUNT(*) FROM {Users}"))[0]);
    }

    [Fact]
    public async Task ExecutionItselfObservesTheDeadlineRatherThanOnlyLockWaits()
    {
        await using var fixture = await PreparedAsync();
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        var timer = Stopwatch.StartNew();
        await using (var store = await DeadlineStore.OpenAsync(fixture.Installation, budget))
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.QueryAsync(Provider == "sqlite" ? """
                WITH RECURSIVE n(value) AS (SELECT 1 UNION ALL SELECT value+1 FROM n WHERE value<1000000000)
                SELECT SUM(value) FROM n
                """ : "SELECT pg_catalog.pg_sleep(5)"));
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(1), $"elapsed_ms={timer.Elapsed.TotalMilliseconds:F0}");
    }

    private protected async Task<RegistryFixture> PreparedAsync()
    {
        var fixture = await RegistryFixture.CreateAsync(Provider, Token);
        try
        {
            await using var scope = await fixture.Maintenance.AcquireAsync(Token);
            using var budget = new OperationBudget(OperationBudget.Register, Token);
            await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
            await using var transaction = await connection.BeginTransactionAsync(Token);
            await new RegistryMigrationRunner(fixture.Installation).ApplyIdentityFormatAsync(
                Assert.IsType<MaintenanceScope>(scope), connection, transaction, budget);
            await transaction.CommitAsync(Token);
            return fixture;
        }
        catch { await fixture.DisposeAsync(); throw; }
    }

    private protected Task InsertAsync(DeadlineStore store, Guid id, string display) => store.QueryAsync($"""
        INSERT INTO {Users} (user_id,mode,display_name,created_at_utc,updated_at_utc)
        VALUES (@id,'oidc',@display,@now,@now)
        """, ("id", id), ("display", display), ("now", DateTime.UtcNow));

    protected async Task ExecuteAsync(DbConnection connection, string sql, DbTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = transaction;
        command.CommandTimeout = 1;
        await command.ExecuteNonQueryAsync(Token);
    }
}

[Trait("Provider", "sqlite")]
[Collection("identity-deadlines")]
public sealed class SqliteIdentityDeadlineTests : IdentityDeadlineTests
{
    protected override string Provider => "sqlite";

    [Fact]
    public async Task TransientNativeHeaderContentionIsRetriedBeforeAnyTransactionWithinTheOriginalBudget()
    {
        await using var fixture = await PreparedAsync();
        using var setup = new OperationBudget(OperationBudget.Register, Token);
        await using var blocker = await fixture.Installation.OpenPlatformAsync(setup);
        await ExecuteAsync(blocker, "BEGIN EXCLUSIVE");
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        var opening = DeadlineStore.OpenAsync(fixture.Installation, budget);
        await Task.Delay(150, Token);
        var waitedForRelease = !opening.IsCompleted;
        await ExecuteAsync(blocker, "ROLLBACK");
        await using var store = await opening;
        Assert.True(waitedForRelease);
        await store.BeginWriteAsync();
        await InsertAsync(store, Guid.NewGuid(), "header-retry");
        await store.CommitAsync();
        Assert.Equal("header-retry", Assert.Single(
            await store.QueryAsync("SELECT display_name FROM __kofte_users"))[0]);
        Assert.True(budget.Remaining > TimeSpan.Zero);
    }

    [Fact]
    public async Task ExclusiveLockBeforeOpenFailsClosedWithinTheOriginalBudget()
    {
        await using var fixture = await PreparedAsync();
        using var setup = new OperationBudget(OperationBudget.Register, Token);
        await using var blocker = await fixture.Installation.OpenPlatformAsync(setup);
        await ExecuteAsync(blocker, "BEGIN EXCLUSIVE");
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        var timer = Stopwatch.StartNew();
        var error = await Record.ExceptionAsync(async () =>
        {
            await using var store = await DeadlineStore.OpenAsync(fixture.Installation, budget);
            await store.BeginWriteAsync();
            await InsertAsync(store, Guid.NewGuid(), "must-not-be-written");
            await store.CommitAsync();
        });
        Assert.True(error is OperationCanceledException or StorageException { Code: Kofte.Platform.Modules.Storage.StorageCode.Busy });
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(1), $"elapsed_ms={timer.Elapsed.TotalMilliseconds:F0}");
        await ExecuteAsync(blocker, "ROLLBACK");
        using var verify = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        await using var reopened = await DeadlineStore.OpenAsync(fixture.Installation, verify);
        Assert.Equal(0L, Assert.Single(await reopened.QueryAsync("SELECT COUNT(*) FROM __kofte_users"))[0]);
    }

    [Fact]
    public async Task ReadLockAtCommitRollsBackRatherThanLeavingALateWriter()
    {
        await using var fixture = await PreparedAsync();
        using var setup = new OperationBudget(OperationBudget.Register, Token);
        await using var reader = await fixture.Installation.OpenPlatformAsync(setup);
        await ExecuteAsync(reader, "BEGIN");
        await ExecuteAsync(reader, "SELECT COUNT(*) FROM __kofte_users");
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        var timer = Stopwatch.StartNew();
        await using (var store = await DeadlineStore.OpenAsync(fixture.Installation, budget))
        {
            await store.BeginWriteAsync();
            await InsertAsync(store, Guid.NewGuid(), "commit-must-not-arrive-later");
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.CommitAsync());
        }
        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(1), $"elapsed_ms={timer.Elapsed.TotalMilliseconds:F0}");
        await ExecuteAsync(reader, "ROLLBACK");
        using var verify = new OperationBudget(TimeSpan.FromSeconds(1), Token);
        await using var reopened = await DeadlineStore.OpenAsync(fixture.Installation, verify);
        Assert.Equal(0L, Assert.Single(await reopened.QueryAsync("SELECT COUNT(*) FROM __kofte_users"))[0]);
    }
}

[Trait("Provider", "postgres")]
[Collection("identity-deadlines")]
public sealed class PostgresIdentityDeadlineTests : IdentityDeadlineTests
{
    protected override string Provider => "postgres";
}

[CollectionDefinition("identity-deadlines", DisableParallelization = true)]
public sealed class IdentityDeadlineCollection;
