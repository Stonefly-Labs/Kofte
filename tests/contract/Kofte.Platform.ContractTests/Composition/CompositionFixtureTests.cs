using System.Text;
using Kofte.Infrastructure.Storage;
using Kofte.TestSupport;
using Npgsql;

namespace Kofte.Platform.ContractTests.Composition;

public sealed class CompositionFixtureTests
{
    [Fact]
    [Trait("Provider", "postgres")]
    public async Task IndependentConsumerBorrowsOwnedServiceWithoutSharingOrLeakingTargets()
    {
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        lifetime.CancelAfter(TimeSpan.FromSeconds(60));
        var token = lifetime.Token;
        await using var fixture = await CompositionFixture.CreateAsync("postgres", token);
        await fixture.SeedUnrelatedNoStorageAsync(token);
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        var postgres = fixture.Storage.Postgres ??
            throw new InvalidOperationException("composition_postgres_fixture_required");
        await using var connection = new NpgsqlConnection(postgres.ConnectionString);
        await connection.OpenAsync(token);
        async Task<long> CountAsync(string sql)
        {
            await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 3 };
            return (long)(await command.ExecuteScalarAsync(token) ??
                throw new InvalidOperationException("composition_service_inventory_missing"));
        }
        const string databases = "SELECT count(*) FROM pg_catalog.pg_database WHERE datname ~ '^kofte_test_'";
        const string roles = "SELECT count(*) FROM pg_catalog.pg_roles WHERE rolname ~ '^kofte_(app|provisioner)_'";
        var databaseCount = await CountAsync(databases);
        var roleCount = await CountAsync(roles);
        var execution = CompositionConsumerHarness.InvokeAsync(
            ["--provider", "postgres", "--scenario", "empty-boot"], token);
        try
        {
            var observed = false;
            while (!execution.IsCompleted)
            {
                if (await CountAsync(databases) > databaseCount)
                {
                    observed = true;
                    break;
                }
                await Task.Delay(TimeSpan.FromMilliseconds(25), token);
            }
            CompositionConsumerHarness.RequireSuccess(await execution, "postgres", "empty-boot");
            Assert.True(observed, "composition_consumer_did_not_borrow_owned_service");
            Assert.Equal(databaseCount, await CountAsync(databases));
            Assert.Equal(roleCount, await CountAsync(roles));
            Assert.True(before == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
        }
        finally
        {
            await lifetime.CancelAsync();
            await execution;
        }
    }

    [Fact]
    [Trait("Provider", "sqlite")]
    public async Task ReusedEphemeralWebPortCannotInvalidateAComposedHostConfiguration()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync("sqlite", token);
        await fixture.WriteSelectionAsync(Encoding.UTF8.GetBytes("""{"schemaVersion":"1","modules":[]}"""), token);
        await fixture.PrepareIdentityAsync(token);
        Assert.NotNull(fixture.WebUrl);
        await using var process = fixture.Start("validate", CompositionTestCatalog.EmptyBoot,
            preferredHostPort: fixture.WebUrl.Port);
        var result = await process.WaitAsync(token);
        Assert.Equal(0, result.ExitCode);
        Assert.NotEqual(fixture.WebUrl.Port, fixture.HostUrl.Port);
        Assert.DoesNotContain("composition_configuration_invalid", result.Stdout);
        Assert.True(process.ExitVerified && process.DescendantExitsVerified);
    }

    [Fact]
    [Trait("Provider", "sqlite")]
    public async Task SelectionReplacementIsPrivateAndOwnedWithoutLeakingFixtureResources()
    {
        var fixture = await CompositionFixture.CreateAsync("sqlite", TestContext.Current.CancellationToken);
        var selection = fixture.SelectionPath;
        var root = fixture.Storage.DataRoot;
        try
        {
            await fixture.WriteSelectionAsync(Encoding.UTF8.GetBytes(
                """{"schemaVersion":"1","modules":[]}"""), TestContext.Current.CancellationToken);
            UnixStorePaths.ValidateRoot(Path.GetDirectoryName(selection)!, allowMissing: false);
            Assert.True(UnixStorePaths.ValidateFile(selection));
            await fixture.WriteSelectionAsync(Encoding.UTF8.GetBytes(
                """{"schemaVersion":"1","modules":[{"id":"org.example.alpha","enabled":false,"settings":{}}]}"""),
                TestContext.Current.CancellationToken);
            Assert.Contains("\"enabled\":false", await File.ReadAllTextAsync(selection,
                TestContext.Current.CancellationToken));
            Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(selection)!, "modules.next")));
            Assert.Throws<InvalidOperationException>(() => fixture.Start("private-canary"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.WriteSelectionAsync(
                new byte[1_048_577], TestContext.Current.CancellationToken));
        }
        finally { await fixture.DisposeAsync(); }
        Assert.False(File.Exists(selection));
        Assert.False(Directory.Exists(root));
    }
}
