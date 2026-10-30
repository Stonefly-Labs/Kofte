using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Kofte.Infrastructure.Storage.Sqlite;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Modules.Alpha;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Kofte.TestSupport;
using System.Diagnostics;
using System.Collections.Concurrent;
using Kofte.Infrastructure.Storage.Registry;

namespace Kofte.Storage.IntegrationTests.Providers;

public abstract class StorageBoundaryTests
{
    protected abstract string Provider { get; }

    [Theory]
    [InlineData("raw")]
    [InlineData("suppressed")]
    [InlineData("foreign")]
    [InlineData("reserved")]
    [InlineData("unknown")]
    [InlineData("foreign_model")]
    [InlineData("foreign_fk")]
    [InlineData("rename")]
    [InlineData("provider_annotation")]
    public async Task EntirePendingSetIsRejectedBeforeEvenTheFirstValidMigrationWrites(string failure)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await ModuleFixture.CreateAsync(Provider, token);
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        NegativeMigration.Failure.Value = failure;
        try
        {
            var result = await fixture.Alpha.MigrateAsync(
                (options, binding) => binding.Provider == "postgresql"
                    ? (NegativeContext)new PostgresNegativeContext(options, binding)
                    : new SqliteNegativeContext(options, binding), scope, cancellationToken: token);
            Assert.Equal(StorageOutcome.Rejected, result.Outcome);
            Assert.Equal("storage_request_invalid", Assert.Single(result.Diagnostics).Code);
            using var budget = new OperationBudget(OperationBudget.Resolve, token);
            await using var connection = fixture.Postgres
                ? await fixture.Installation.OpenPlatformAsync(budget)
                : StorageInstallation.OpenSqlite(new ModuleStorePaths(fixture.Installation,
                    fixture.Alpha.Assignment.AllocationKey).DatabasePath, budget);
            await using var command = connection.CreateCommand();
            command.CommandText = fixture.Postgres
                ? "SELECT count(*) FROM pg_catalog.pg_tables WHERE schemaname=@schema AND tablename IN ('Records','__EFMigrationsHistory')"
                : "SELECT count(*) FROM sqlite_master WHERE name IN ('Records','__EFMigrationsHistory')";
            if (fixture.Postgres)
            {
                var schema = command.CreateParameter();
                schema.ParameterName = "schema";
                schema.Value = fixture.Alpha.Schema!;
                command.Parameters.Add(schema);
            }
            Assert.Equal(0L, await command.ExecuteScalarAsync(token));
            Assert.Equal(StorageOutcome.ReconciliationRequired,
                (await fixture.Registry.ResolveAsync(fixture.Alpha.Owner, token)).Outcome);
            Assert.IsType<StorageResult.Ready>(await fixture.Beta.ResolveAsync(token));
        }
        finally { NegativeMigration.Failure.Value = null; }
    }

    [Fact]
    public async Task FactoryCannotSwitchOwnerAndDisposedOrStaleBindingsCannotExecute()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await ModuleFixture.CreateAsync(Provider, token);
        await Assert.ThrowsAsync<StorageException>(() => fixture.Alpha.CreateContextAsync(
            (options, _) => ModuleFixture.AlphaFactory(options, fixture.Beta), token));
        await using var stale = await fixture.Registry.BindAsync(fixture.Alpha.Owner, token);
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        Assert.IsType<StorageResult.Ready>(await fixture.Alpha.MigrateAsync(ModuleFixture.AlphaFactory, scope,
            cancellationToken: token));
        await Assert.ThrowsAsync<StorageException>(() => stale.CreateContextAsync(ModuleFixture.AlphaFactory, token));
        await using var context = await fixture.Alpha.CreateContextAsync(ModuleFixture.AlphaFactory, token);
        await fixture.Alpha.DisposeAsync();
        await Assert.ThrowsAsync<StorageException>(() => context.Records.CountAsync(token));
        await Assert.ThrowsAsync<StorageException>(() => fixture.Alpha.CreateContextAsync(ModuleFixture.AlphaFactory, token));
    }

    [Fact]
    public async Task ForeignInstallationScopeCannotAuthorizeMigration()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await ModuleFixture.CreateAsync(Provider, token);
        await using var foreign = await ModuleFixture.CreateAsync(Provider, token);
        await using var scope = await foreign.Maintenance.AcquireAsync(token);
        var result = await fixture.Alpha.MigrateAsync(ModuleFixture.AlphaFactory, scope, cancellationToken: token);
        Assert.Equal(StorageOutcome.Rejected, result.Outcome);
        Assert.Equal(StorageDurability.Unchanged, result.Durability);
        Assert.IsType<StorageResult.Ready>(await fixture.Alpha.ResolveAsync(token));
    }

    [Fact]
    public async Task UnboundSnapshotCannotAuthorizeEvenAValidInitialMigration()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await ModuleFixture.CreateAsync(Provider, token);
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        var result = await fixture.Alpha.MigrateAsync(
            (options, binding) => binding.Provider == "postgresql"
                ? (AlphaContext)new PostgresUnboundContext(options, binding)
                : new SqliteUnboundContext(options, binding), scope, cancellationToken: token);
        Assert.Equal(StorageOutcome.Rejected, result.Outcome);
        Assert.Equal("storage_request_invalid", Assert.Single(result.Diagnostics).Code);
        Assert.IsType<StorageResult.Ready>(await fixture.Beta.ResolveAsync(token));
    }

    [Fact]
    public async Task FactoryCannotReplaceTheInternallySelectedConnection()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await ModuleFixture.CreateAsync(Provider, token);
        await using var other = await fixture.Beta.CreateContextAsync(ModuleFixture.BetaFactory, token);
        var options = new DbContextOptionsBuilder();
        if (fixture.Postgres) options.UseNpgsql(other.Database.GetDbConnection());
        else options.UseSqlite(other.Database.GetDbConnection());
        await Assert.ThrowsAsync<StorageException>(() => fixture.Alpha.CreateContextAsync(
            (_, binding) => ModuleFixture.AlphaFactory(options.Options, binding), token));
        Assert.IsType<StorageResult.Ready>(await fixture.Beta.ResolveAsync(token));
    }

    [Fact]
    public async Task CommittedHistoryCannotMarkAnIncorrectPhysicalModelCurrent()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await ModuleFixture.CreateAsync(Provider, token);
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        Assert.IsType<StorageResult.Ready>(await fixture.Alpha.MigrateAsync(ModuleFixture.AlphaFactory,
            scope, "202610210001_Initial", token));
        NegativeMigration.Failure.Value = "model_mismatch";
        try
        {
            var result = await fixture.Alpha.MigrateAsync(
                (options, binding) => binding.Provider == "postgresql"
                    ? (NegativeContext)new PostgresNegativeContext(options, binding)
                    : new SqliteNegativeContext(options, binding), scope, cancellationToken: token);
            Assert.Equal(StorageOutcome.ReconciliationRequired, result.Outcome);
            Assert.Equal(StorageOutcome.ReconciliationRequired,
                (await fixture.Registry.ResolveAsync(fixture.Alpha.Owner, token)).Outcome);
            Assert.IsType<StorageResult.Ready>(await fixture.Beta.ResolveAsync(token));
        }
        finally { NegativeMigration.Failure.Value = null; }
    }

    [Fact]
    public async Task CancellationAfterCommittedIntentPreservesItsCodeAndReportsIncompleteReconciliation()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await ModuleFixture.CreateAsync(Provider, token);
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        var traces = new ConcurrentQueue<(string Correlation, string? Code, ActivityEvent[] Events)>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kofte.Storage",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => traces.Enqueue((activity.TraceId.ToHexString(),
                activity.GetTagItem("storage.code") as string, activity.Events.ToArray()))
        };
        ActivitySource.AddActivityListener(listener);
        NegativeMigration.Failure.Value = "cancel_after_intent";
        NegativeMigration.Cancel.Value = () =>
        {
            cancellation.Cancel();
            cancellation.Token.ThrowIfCancellationRequested();
        };
        try
        {
            var result = await fixture.Alpha.MigrateAsync(
                (options, binding) => binding.Provider == "postgresql"
                    ? (NegativeContext)new PostgresNegativeContext(options, binding)
                    : new SqliteNegativeContext(options, binding), scope, cancellationToken: cancellation.Token);
            Assert.Equal(StorageOutcome.Failed, result.Outcome);
            Assert.Equal("storage_cancelled", Assert.Single(result.Diagnostics).Code);
            Assert.Equal(StorageDurability.Committed, result.Durability);
            var trace = Assert.Single(traces, entry => entry.Correlation == result.CorrelationId);
            Assert.Equal("storage_cancelled", trace.Code);
            var secondary = Assert.Single(trace.Events,
                entry => entry.Name == "storage_migration_reconciliation_incomplete");
            Assert.Equal("storage_reconciliation_required",
                secondary.Tags.Single(tag => tag.Key == "storage.code").Value);
            using var budget = new OperationBudget(OperationBudget.Resolve, token);
            await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
            var registration = await new RegistryMigrationRunner(fixture.Installation)
                .FindAsync(connection, fixture.Alpha.Owner, budget);
            Assert.Equal("in_progress", registration!.MigrationState);
            Assert.Equal(StorageOutcome.ReconciliationRequired,
                (await fixture.Registry.ResolveAsync(fixture.Alpha.Owner, token)).Outcome);
        }
        finally
        {
            NegativeMigration.Failure.Value = null;
            NegativeMigration.Cancel.Value = null;
        }
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteStorageBoundaryTests : StorageBoundaryTests
{
    protected override string Provider => "sqlite";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HeaderInspectionDoesNotRejectAnAdmittedSqliteWriter(bool platform)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await ModuleFixture.CreateAsync("sqlite", token);
        using var budget = new OperationBudget(OperationBudget.Resolve, token);
        await using var writer = platform
            ? await fixture.Installation.OpenPlatformAsync(budget)
            : StorageInstallation.OpenSqlite(
                new ModuleStorePaths(fixture.Installation, fixture.Alpha.Assignment.AllocationKey).DatabasePath, budget);
        await using var transaction = await writer.BeginTransactionAsync(token);
        var resolved = Assert.IsType<StorageResult.Ready>(
            await fixture.Registry.ResolveAsync(fixture.Alpha.Owner, token));
        Assert.Equal(StorageAssignmentSnapshot.From(fixture.Alpha.Assignment),
            StorageAssignmentSnapshot.From(resolved.Assignment));
    }

    [Fact]
    public async Task MissingAndLinkedReadyFilesAreNeverCreatedOrFollowedAtActualOpen()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await ModuleFixture.CreateAsync("sqlite", token);
        var alpha = new ModuleStorePaths(fixture.Installation, fixture.Alpha.Assignment.AllocationKey).DatabasePath;
        var beta = new ModuleStorePaths(fixture.Installation, fixture.Beta.Assignment.AllocationKey).DatabasePath;
        var before = await File.ReadAllBytesAsync(beta, token);
        File.Delete(alpha);
        await Assert.ThrowsAsync<StorageException>(() => fixture.Alpha.CreateContextAsync(ModuleFixture.AlphaFactory, token));
        Assert.False(File.Exists(alpha));
        File.CreateSymbolicLink(alpha, beta);
        await Assert.ThrowsAsync<StorageException>(() => fixture.Alpha.CreateContextAsync(ModuleFixture.AlphaFactory, token));
        Assert.Equal(before, await File.ReadAllBytesAsync(beta, token));
        File.Delete(alpha);
    }

    [Theory]
    [InlineData("sidecar")]
    [InlineData("hardlink")]
    [InlineData("directory")]
    [InlineData("corrupt")]
    [InlineData("foreign-header")]
    [InlineData("ancestor")]
    public async Task ActualOpenRejectsAliasesAndCorruptionWithoutChangingTheOtherStore(string failure)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await ModuleFixture.CreateAsync("sqlite", token);
        var alpha = new ModuleStorePaths(fixture.Installation, fixture.Alpha.Assignment.AllocationKey).DatabasePath;
        var beta = new ModuleStorePaths(fixture.Installation, fixture.Beta.Assignment.AllocationKey).DatabasePath;
        var before = await File.ReadAllBytesAsync(beta, token);
        switch (failure)
        {
            case "sidecar": File.CreateSymbolicLink(alpha + "-journal", beta); break;
            case "hardlink":
                File.Delete(alpha);
                var start = new ProcessStartInfo("ln") { RedirectStandardError = true, RedirectStandardOutput = true };
                start.ArgumentList.Add(beta);
                start.ArgumentList.Add(alpha);
                Assert.Equal(0, (await OwnedProcesses.RunAsync(start, TimeSpan.FromSeconds(3), token)).ExitCode);
                break;
            case "directory":
                File.Delete(alpha);
                Directory.CreateDirectory(alpha);
                break;
            case "corrupt": await File.WriteAllBytesAsync(alpha, "not a database"u8.ToArray(), token); break;
            case "foreign-header":
                await File.WriteAllBytesAsync(alpha, Enumerable.Repeat((byte)'x', 100).ToArray(), token);
                break;
            case "ancestor":
                Directory.Move(Path.GetDirectoryName(alpha)!, Path.Combine(fixture.Root, "preserved-alpha"));
                Directory.CreateSymbolicLink(Path.GetDirectoryName(alpha)!, Path.GetDirectoryName(beta)!);
                break;
        }
        var rejectedBytes = failure is "corrupt" or "foreign-header" ? await File.ReadAllBytesAsync(alpha, token) : null;
        var rejected = await Assert.ThrowsAsync<StorageException>(() => fixture.Alpha.CreateContextAsync(ModuleFixture.AlphaFactory, token));
        Assert.DoesNotContain(fixture.Root, rejected.ToString(), StringComparison.Ordinal);
        if (rejectedBytes is not null) Assert.Equal(rejectedBytes, await File.ReadAllBytesAsync(alpha, token));
        Assert.Equal(before, await File.ReadAllBytesAsync(beta, token));
        if (failure == "sidecar") File.Delete(alpha + "-journal");
        if (failure == "hardlink") File.Delete(alpha);
        if (failure == "ancestor") Directory.Delete(Path.GetDirectoryName(alpha)!);
    }
}
[Trait("Provider", "postgres")]
public sealed class PostgresStorageBoundaryTests : StorageBoundaryTests
{ protected override string Provider => "postgres"; }

public abstract class NegativeContext(DbContextOptions options, ModuleStorageBinding binding)
    : AlphaContext(options, binding);
public sealed class SqliteNegativeContext(DbContextOptions options, ModuleStorageBinding binding) : NegativeContext(options, binding);
public sealed class PostgresNegativeContext(DbContextOptions options, ModuleStorageBinding binding) : NegativeContext(options, binding);
[DbContext(typeof(SqliteNegativeContext)), Migration("202610210001_Initial")]
public sealed class SqliteNegativeInitial : AlphaInitial;
[DbContext(typeof(PostgresNegativeContext)), Migration("202610210001_Initial")]
public sealed class PostgresNegativeInitial : AlphaInitial;

public abstract class NegativeMigration : AssignedMigration
{
    internal static readonly AsyncLocal<string?> Failure = new();
    internal static readonly AsyncLocal<Action?> Cancel = new();
    protected override void Up(MigrationBuilder builder)
    {
        switch (Failure.Value)
        {
            case "cancel_after_intent":
                (Cancel.Value ?? throw new InvalidOperationException("storage_cancellation_callback_required"))();
                break;
            case "raw": builder.Sql("CREATE TABLE forbidden (Id INTEGER)"); break;
            case "suppressed": builder.Sql("CREATE TABLE forbidden (Id INTEGER)", suppressTransaction: true); break;
            case "foreign": builder.AddColumn<string>("Foreign", "Records", schema: "kofte_system"); break;
            case "reserved": builder.AddColumn<string>("Value", "__kofte_storage_owner", schema: AssignedSchema); break;
            case "unknown": builder.Operations.Add(new UnknownOperation()); break;
            case "foreign_model": break;
            case "model_mismatch":
                builder.CreateTable("Unexpected", schema: AssignedSchema,
                    columns: table => new
                    {
                        Id = table.Column<int>(type: AssignedProvider == "postgresql" ? "integer" : "INTEGER", nullable: false)
                    });
                break;
            case "rename": builder.RenameTable("Records", schema: AssignedSchema, newSchema: "kofte_system"); break;
            case "provider_annotation":
                builder.AddColumn<string>("Extra", "Records", schema: AssignedSchema)
                    .Annotation("Npgsql:ArbitrarySql", "unsafe");
                break;
            case "foreign_fk":
                builder.CreateTable("Children", schema: AssignedSchema,
                    columns: table => new { Id = table.Column<int>(nullable: false) },
                    constraints: table => table.ForeignKey("FK_Children_Records", row => row.Id,
                        principalSchema: "kofte_system", principalTable: "Records", principalColumn: "Id"));
                break;
            default: throw new InvalidOperationException("storage_negative_case_required");
        }
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder) =>
        AlphaContext.BuildAssignedModel(modelBuilder, Failure.Value == "foreign_model" ? "kofte_system" : AssignedSchema,
            AssignedProvider, upgraded: true);
    private sealed class UnknownOperation : MigrationOperation;
}
[DbContext(typeof(SqliteNegativeContext)), Migration("202610210002_Unsafe")]
public sealed class SqliteNegativeMigration : NegativeMigration;
[DbContext(typeof(PostgresNegativeContext)), Migration("202610210002_Unsafe")]
public sealed class PostgresNegativeMigration : NegativeMigration;
[DbContext(typeof(SqliteNegativeContext))]
public sealed class SqliteNegativeSnapshot : AlphaSnapshot;
[DbContext(typeof(PostgresNegativeContext))]
public sealed class PostgresNegativeSnapshot : AlphaSnapshot;

public sealed class SqliteUnboundContext(DbContextOptions options, ModuleStorageBinding binding) : AlphaContext(options, binding);
public sealed class PostgresUnboundContext(DbContextOptions options, ModuleStorageBinding binding) : AlphaContext(options, binding);
[DbContext(typeof(SqliteUnboundContext)), Migration("202610210001_Initial")]
public sealed class SqliteUnboundInitial : AlphaInitial;
[DbContext(typeof(PostgresUnboundContext)), Migration("202610210001_Initial")]
public sealed class PostgresUnboundInitial : AlphaInitial;
[DbContext(typeof(SqliteUnboundContext))]
public sealed class SqliteUnboundSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) { }
}
[DbContext(typeof(PostgresUnboundContext))]
public sealed class PostgresUnboundSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) { }
}
