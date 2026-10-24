using Kofte.Infrastructure.Storage.Postgres;
using Npgsql;

namespace Kofte.TestSupport;

public static class CompositionLeaseProbe
{
    public static async Task TerminateAsync(PostgresResources owned, CancellationToken token)
    {
        var runtime = new NpgsqlConnectionStringBuilder(owned.ConnectionString);
        await using var admin = new NpgsqlConnection(owned.ProvisioningConnectionString);
        await admin.OpenAsync(token);
        var pids = await PidsAsync(admin, runtime, token);
        if (pids.Count != 1) throw new InvalidOperationException("composition_serving_lease_not_unique");
        await using var terminate = new NpgsqlCommand("""
            SELECT pg_catalog.pg_terminate_backend(pid) FROM pg_catalog.pg_stat_activity
            WHERE pid=$1 AND datname=current_database() AND usename=$2 AND application_name=$3
            """, admin) { CommandTimeout = 3 };
        terminate.Parameters.AddWithValue(pids[0]);
        terminate.Parameters.AddWithValue(runtime.Username!);
        terminate.Parameters.AddWithValue(runtime.ApplicationName!);
        if (await terminate.ExecuteScalarAsync(token) is not true)
            throw new InvalidOperationException("composition_serving_lease_termination_unverified");
    }

    public static async Task<int> CountAsync(PostgresResources owned, CancellationToken token)
    {
        await using var admin = new NpgsqlConnection(owned.ProvisioningConnectionString);
        await admin.OpenAsync(token);
        return (await PidsAsync(admin, new(owned.ConnectionString), token)).Count;
    }

    private static async Task<List<int>> PidsAsync(NpgsqlConnection admin,
        NpgsqlConnectionStringBuilder runtime, CancellationToken token)
    {
        await using var query = new NpgsqlCommand("""
            SELECT DISTINCT a.pid FROM pg_catalog.pg_stat_activity a JOIN pg_catalog.pg_locks l ON l.pid=a.pid
            WHERE a.datname=current_database() AND a.usename=$1 AND a.application_name=$2
              AND l.locktype='advisory' AND l.granted AND l.mode='ShareLock'
              AND l.classid=(($3::bigint >> 32) & 4294967295)::oid
              AND l.objid=($3::bigint & 4294967295)::oid AND l.objsubid=1
            """, admin) { CommandTimeout = 3 };
        query.Parameters.AddWithValue(runtime.Username!);
        query.Parameters.AddWithValue(runtime.ApplicationName!);
        query.Parameters.AddWithValue(PostgresIdentity.LeaseKey);
        var pids = new List<int>();
        await using var reader = await query.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token)) pids.Add(reader.GetInt32(0));
        return pids;
    }
}
