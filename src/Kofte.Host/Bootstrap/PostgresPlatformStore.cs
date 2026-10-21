using System.Text.RegularExpressions;
using Npgsql;

namespace Kofte.Host.Bootstrap;

public sealed class PostgresPlatformStore : IPlatformStore
{
    internal const long OperationLeaseKey = 0x6b6f667465000001;
    private readonly string connectionString;
    private readonly string ownership;

    public PostgresPlatformStore(string connection)
    {
        var builder = new NpgsqlConnectionStringBuilder(connection);
        const string prefix = "kofte_bootstrap_";
        var applicationName = builder.ApplicationName ?? "";
        if (!BootstrapConfiguration.Loopback(builder.Host ?? "") ||
            !Regex.IsMatch(applicationName, @"\Akofte_bootstrap_[0-9a-f]{32}\z"))
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        ownership = "kofte-bootstrap:" + applicationName[prefix.Length..];
        builder.Timeout = 3;
        builder.CommandTimeout = 3;
        builder.Pooling = false;
        builder.IncludeErrorDetail = false;
        connectionString = builder.ConnectionString;
    }

    public async Task<NpgsqlConnection> AcquireHostLeaseAsync(CancellationToken token)
    {
        var connection = new NpgsqlConnection(connectionString);
        try
        {
            await connection.OpenAsync(token);
            await using var command = new NpgsqlCommand("SELECT pg_catalog.pg_try_advisory_lock_shared($1)", connection);
            command.Parameters.AddWithValue(OperationLeaseKey);
            if (await command.ExecuteScalarAsync(token) is not true)
                throw new BootstrapException(BootstrapError.StoreBusy);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async Task ProbeAsync(CancellationToken token)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand("""
            SELECT
              pg_catalog.pg_try_advisory_lock_shared($2)
              AND EXISTS (SELECT 1 FROM pg_catalog.pg_database d WHERE d.datname=current_database()
                AND pg_catalog.shobj_description(d.oid, 'pg_database')=$1)
              AND EXISTS (SELECT 1 FROM pg_catalog.pg_roles r WHERE r.rolname=current_user
                AND NOT (r.rolsuper OR r.rolcreatedb OR r.rolcreaterole OR r.rolreplication OR r.rolbypassrls)
                AND pg_catalog.shobj_description(r.oid, 'pg_authid')=$1)
              AND EXISTS (SELECT 1 FROM pg_catalog.pg_namespace n JOIN pg_catalog.pg_roles r ON r.oid=n.nspowner
                WHERE n.nspname='kofte_system' AND r.rolname=current_user
                AND pg_catalog.obj_description(n.oid, 'pg_namespace')=$1)
              AND pg_catalog.has_database_privilege(current_user, current_database(), 'CONNECT')
              AND NOT pg_catalog.has_database_privilege(current_user, current_database(), 'CREATE')
              AND pg_catalog.has_schema_privilege(current_user, 'kofte_system', 'USAGE')
              AND pg_catalog.has_schema_privilege(current_user, 'kofte_system', 'CREATE')
              AND pg_catalog.current_setting('transaction_read_only')='off'
            """, connection);
        command.Parameters.AddWithValue(ownership);
        command.Parameters.AddWithValue(OperationLeaseKey);
        if (await command.ExecuteScalarAsync(token) is not true)
            throw new BootstrapException(BootstrapError.StorageInvalid);
    }
}
