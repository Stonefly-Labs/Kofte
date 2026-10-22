using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Registry;
using Npgsql;

namespace Kofte.Storage.IntegrationTests.Recovery;

[Trait("Provider", "postgres"), Trait("Suite", "recovery")]
public sealed class PostgresLeaseLossTests
{
    [Fact]
    public async Task TerminatedAdvisorySessionCancelsItsScopeAndCannotSilentlyReacquire()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync("postgres", token);
        var owner = RegistryFixture.Module("org.example.lease-original");
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        var original = Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(owner, scope, token)).Assignment;
        await using var connection = new NpgsqlConnection(fixture.RuntimeConnection);
        await connection.OpenAsync(token);
        await using var terminate = new NpgsqlCommand("""
            SELECT pg_catalog.pg_terminate_backend(l.pid) FROM pg_catalog.pg_locks l
              JOIN pg_catalog.pg_stat_activity a ON a.pid=l.pid
              WHERE l.locktype='advisory' AND l.granted AND l.mode='ExclusiveLock'
                AND l.classid=(($1::bigint >> 32) & 4294967295)::oid
                AND l.objid=($1::bigint & 4294967295)::oid AND l.objsubid=1
                AND a.datname=current_database() AND a.usename=current_user AND l.pid<>pg_backend_pid()
            """, connection) { CommandTimeout = 3 };
        terminate.Parameters.AddWithValue(0x6b6f667465000001L);
        Assert.Equal(true, await terminate.ExecuteScalarAsync(token));
        var rejected = RegistryFixture.Module("org.example.lease-rejected");
        Assert.Equal(StorageOutcome.Failed, (await fixture.Registry.RegisterAsync(rejected, scope, token)).Outcome);
        Assert.True(scope.Lifetime.IsCancellationRequested);
        Assert.Equal("storage_cancelled", (await fixture.Registry.RegisterAsync(rejected, scope, token)).Diagnostics[0].Code);
        await using var replacement = await fixture.Maintenance.AcquireAsync(token);
        var after = Assert.IsType<StorageResult.Ready>(await fixture.Registry.RegisterAsync(owner, replacement, token)).Assignment;
        Assert.Equal(original.AllocationId, after.AllocationId);
        await fixture.AssertInventoryAsync(1, 1, token);
    }
}
