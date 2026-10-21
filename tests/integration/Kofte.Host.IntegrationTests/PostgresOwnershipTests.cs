using Xunit;

namespace Kofte.Host.IntegrationTests;

[Trait("Provider", "postgres")]
public sealed class PostgresOwnershipTests
{
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
            Assert.True(await fixture.DatabaseExistsAsync(TestContext.Current.CancellationToken));
        }
        finally
        {
            await fixture.SetDatabaseOwnershipAsync(valid: true, TestContext.Current.CancellationToken);
        }
        await fixture.ValidateOwnershipAsync(TestContext.Current.CancellationToken);
    }
}
