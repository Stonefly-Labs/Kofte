using Kofte.Infrastructure.Storage;
using System.Text.Json;
using Kofte.TestSupport;
using Npgsql;
using Xunit;

namespace Kofte.Host.IntegrationTests;

[Trait("Provider", "postgres")]
public sealed class PostgresOwnershipTests(PostgresAssemblyFixture service)
{
    [Fact]
    public async Task PrivateServiceLeaseKeepsFreshTargetsAndVerifiesTheirIndependentCleanup()
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        var token = TestContext.Current.CancellationToken;
        using var root = new OwnedTestRoot();
        Directory.CreateDirectory(root.Path, UnixStorePaths.DirectoryMode);
        var path = Path.Combine(root.Path, "service.json");
        await service.WriteLeaseAsync(path, token);
        Assert.True(UnixStorePaths.ValidateFile(path));
        using var lease = JsonDocument.Parse(await File.ReadAllTextAsync(path, token));
        var connection = lease.RootElement.GetProperty("ConnectionString").GetString()!;
        await using var first = await PostgresResources.CreateAsync(true, connection, token: token);
        await using var second = await PostgresResources.CreateAsync(true, connection, token: token);
        try
        {
            Assert.NotEqual(first.RunId, second.RunId);
            Assert.NotEqual(new NpgsqlConnectionStringBuilder(first.ConnectionString).Database,
                new NpgsqlConnectionStringBuilder(second.ConnectionString).Database);
            await first.DisposeAsync();
            await second.ValidateOwnershipAsync(token);
        }
        finally
        {
            await first.DisposeAsync();
            await second.DisposeAsync();
        }
        await using var admin = new NpgsqlConnection(connection);
        await admin.OpenAsync(token);
        await using var command = new NpgsqlCommand("""
            SELECT NOT EXISTS (SELECT 1 FROM pg_catalog.pg_database WHERE datname IN (@first, @second))
            AND NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles
              WHERE rolname IN (@firstApp, @secondApp, @firstProvisioner, @secondProvisioner))
            """, admin) { CommandTimeout = 3 };
        command.Parameters.AddWithValue("first", "kofte_test_" + first.RunId);
        command.Parameters.AddWithValue("second", "kofte_test_" + second.RunId);
        command.Parameters.AddWithValue("firstApp", "kofte_app_" + first.RunId);
        command.Parameters.AddWithValue("secondApp", "kofte_app_" + second.RunId);
        command.Parameters.AddWithValue("firstProvisioner", "kofte_provisioner_" + first.RunId);
        command.Parameters.AddWithValue("secondProvisioner", "kofte_provisioner_" + second.RunId);
        Assert.True(await command.ExecuteScalarAsync(token) is true);
    }

    [Fact]
    public async Task FixtureAdministrationUsesItsOwnBudgetAndStillHonorsCallerCancellation()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await PostgresFixture.CreateAsync(true, token: token);
        using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(token);
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fixture.ValidateOwnershipAsync(cancelled.Token));
        await fixture.SetAvailableAsync(false, token);
        Task validation;
        try
        {
            validation = fixture.ValidateOwnershipAsync(token);
            await Task.Delay(TimeSpan.FromSeconds(4), token);
        }
        finally
        {
            await fixture.SetAvailableAsync(true, token);
        }
        await validation;
        await fixture.ValidateOwnershipAsync(token);
    }

    [Fact]
    public async Task OptInLoopbackAndNewTargetAreRequiredBeforeProvisioning()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PostgresFixture.CreateAsync(false, token: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PostgresFixture.CreateAsync(true, "Host=example.com;Database=postgres;Username=admin",
                token: TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            PostgresFixture.CreateAsync(true, requestedDatabase: "postgres",
                token: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MissingOwnershipRefusesCleanupAndPreservesTheDatabase()
    {
        await using var fixture = await PostgresFixture.CreateFromEnvironmentAsync(TestContext.Current.CancellationToken);
        await fixture.SetDatabaseOwnershipAsync(valid: false, TestContext.Current.CancellationToken);
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.ValidateOwnershipAsync(TestContext.Current.CancellationToken));
            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.DisposeAsync().AsTask());
            Assert.True(await fixture.DatabaseExistsAsync(TestContext.Current.CancellationToken));
        }
        finally
        {
            await fixture.SetDatabaseOwnershipAsync(valid: true, TestContext.Current.CancellationToken);
        }
        await fixture.ValidateOwnershipAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MissingProvisionerOwnershipRefusesCleanupBeforeDeletingTheDatabase()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await PostgresFixture.CreateFromEnvironmentAsync(token);
        await fixture.SetProvisionerOwnershipAsync(valid: false, token);
        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.DisposeAsync().AsTask());
            Assert.True(await fixture.DatabaseExistsAsync(token));
        }
        finally { await fixture.SetProvisionerOwnershipAsync(valid: true, token); }
        await fixture.ValidateOwnershipAsync(token);
    }
}
