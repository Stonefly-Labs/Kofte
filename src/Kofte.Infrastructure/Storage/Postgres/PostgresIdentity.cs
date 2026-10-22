using System.Net;
using System.Text.RegularExpressions;
using Npgsql;

namespace Kofte.Infrastructure.Storage.Postgres;

internal sealed class PostgresIdentity
{
    internal const long LeaseKey = 0x6b6f667465000001;
    internal string ConnectionString { get; }
    internal string Ownership { get; }
    internal string Role { get; }
    internal string Database { get; }

    internal PostgresIdentity(string connection)
    {
        var builder = new NpgsqlConnectionStringBuilder(connection);
        var host = builder.Host ?? "";
        if (!(host == "localhost" || IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address)) ||
            !Regex.IsMatch(builder.ApplicationName ?? "", @"\Akofte_bootstrap_[0-9a-f]{32}\z") ||
            string.IsNullOrEmpty(builder.Database) || string.IsNullOrEmpty(builder.Username))
            throw new StorageException(Platform.Modules.Storage.StorageCode.RequestInvalid);
        Ownership = "kofte-bootstrap:" + builder.ApplicationName!["kofte_bootstrap_".Length..];
        Role = builder.Username;
        Database = builder.Database;
        builder.Timeout = 3;
        builder.CommandTimeout = 3;
        builder.CancellationTimeout = -1;
        builder.Pooling = false;
        builder.IncludeErrorDetail = false;
        ConnectionString = builder.ConnectionString;
    }

    internal async Task VerifyAsync(NpgsqlConnection connection, OperationBudget budget)
    {
        await using var command = new NpgsqlCommand("""
            SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_database d WHERE d.datname=current_database()
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
            """, connection) { CommandTimeout = budget.CommandSeconds() };
        command.Parameters.AddWithValue(Ownership);
        if (await command.ExecuteScalarAsync(budget.Token) is not true)
            throw new StorageException(Platform.Modules.Storage.StorageCode.OwnerMismatch);
    }
}
