using Xunit;

namespace Kofte.Host.IntegrationTests;

[Trait("Provider", "postgres")]
public sealed class PostgresOwnershipTests
{
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
