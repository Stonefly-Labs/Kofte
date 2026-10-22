using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.TestSupport;
using Xunit;

namespace Kofte.Host.IntegrationTests;

[Trait("Provider", "postgres")]
public sealed class PostgresStorageCompatibilityTests
{
    [Fact]
    public async Task ModuleFreeOwnedPostgresKeepsV0AndExplicitV1StartupWithoutProvisioner()
    {
        var token = TestContext.Current.CancellationToken;
        await using var database = await PostgresResources.CreateFromEnvironmentAsync(token);
        using var unusedRoot = new TempRoot();
        await using (var host = await StartupFixture.StartAsync(unusedRoot.Path, token, postgres: database.ConnectionString))
        {
            using var ready = await host.Client.GetAsync("/health/ready", token);
            Assert.True(ready.IsSuccessStatusCode);
        }
        var installation = StorageInstallation.PostgreSql(database.ConnectionString);
        await StorageReconciliation.ValidateStartupAsync(installation, token);
        var upgrade = await StartupFixture.RunExitAsync(unusedRoot.Path, ["--registry", "upgrade"], token,
            postgres: database.ConnectionString);
        Assert.Equal(0, upgrade.ExitCode);
        await using (var host = await StartupFixture.StartAsync(unusedRoot.Path, token, postgres: database.ConnectionString))
        {
            using var ready = await host.Client.GetAsync("/health/ready", token);
            Assert.True(ready.IsSuccessStatusCode);
        }
        Assert.Equal(0, (await StartupFixture.RunExitAsync(unusedRoot.Path,
            ["--registry", "upgrade"], token, postgres: database.ConnectionString)).ExitCode);
        Assert.False(Directory.Exists(unusedRoot.Path));
    }
}
