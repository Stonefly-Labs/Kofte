using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Npgsql;

namespace Kofte.TestSupport;

public static class CompositionStoreSnapshot
{
    public static async Task<string> CaptureAsync(StorageProviderScope storage, CancellationToken token)
    {
        var entries = new List<string>();
        if (storage.Postgres is null)
            await CaptureDirectoryAsync(storage.DataRoot, storage.DataRoot, entries, token);
        else
            await CapturePostgresAsync(storage.Postgres, entries, token);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            string.Join('\n', entries.Order(StringComparer.Ordinal)))));
    }

    private static async Task CaptureDirectoryAsync(string root, string directory,
        List<string> entries, CancellationToken token)
    {
        foreach (var path in Directory.EnumerateFileSystemEntries(directory).Order(StringComparer.Ordinal))
        {
            token.ThrowIfCancellationRequested();
            var name = Path.GetRelativePath(root, path);
            if (name == ".platform.lock") continue;
            var info = new FileInfo(path);
            if (info.LinkTarget is { } target)
            {
                entries.Add(JsonSerializer.Serialize(new { name, link = target }));
                continue;
            }
            if (Directory.Exists(path))
            {
                entries.Add(JsonSerializer.Serialize(new { directory = name }));
                await CaptureDirectoryAsync(root, path, entries, token);
            }
            else
            {
                await using var file = File.OpenRead(path);
                entries.Add(JsonSerializer.Serialize(new
                {
                    name,
                    sha256 = Convert.ToHexStringLower(await SHA256.HashDataAsync(file, token))
                }));
            }
        }
    }

    private static async Task CapturePostgresAsync(PostgresResources resources, List<string> entries,
        CancellationToken token)
    {
        await using var connection = new NpgsqlConnection(resources.ConnectionString);
        await connection.OpenAsync(token);
        const string schemaFilter = "n.nspname !~ '^pg_' AND n.nspname <> 'information_schema'";
        foreach (var sql in new[]
        {
            $"""
            SELECT n.nspname,n.nspowner,n.nspacl::text,pg_catalog.obj_description(n.oid,'pg_namespace')
            FROM pg_catalog.pg_namespace n WHERE {schemaFilter} ORDER BY n.nspname
            """,
            """
            SELECT datname,datdba,datacl::text,pg_catalog.shobj_description(oid,'pg_database')
            FROM pg_catalog.pg_database WHERE datname=current_database()
            """,
            """
            SELECT rolname,rolsuper,rolcreatedb,rolcreaterole,rolreplication,rolbypassrls,
              pg_catalog.shobj_description(oid,'pg_authid')
            FROM pg_catalog.pg_roles WHERE rolname=current_user
            """,
            $"""
            SELECT n.nspname,c.relname,c.relkind,a.attnum,a.attname,a.atttypid,a.atttypmod,
              a.attnotnull,a.attidentity,a.attgenerated,pg_catalog.pg_get_expr(d.adbin,d.adrelid)
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            LEFT JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum>0 AND NOT a.attisdropped
            LEFT JOIN pg_catalog.pg_attrdef d ON d.adrelid=c.oid AND d.adnum=a.attnum
            WHERE {schemaFilter} ORDER BY n.nspname,c.relname,a.attnum
            """,
            $"""
            SELECT n.nspname,c.relname,con.conname,pg_catalog.pg_get_constraintdef(con.oid)
            FROM pg_catalog.pg_constraint con JOIN pg_catalog.pg_class c ON c.oid=con.conrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE {schemaFilter}
            ORDER BY n.nspname,c.relname,con.conname
            """,
            $"""
            SELECT n.nspname,c.relname,pg_catalog.pg_get_indexdef(c.oid)
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE {schemaFilter} AND c.relkind='i' ORDER BY n.nspname,c.relname
            """,
            $"""
            SELECT n.nspname,p.proname,pg_catalog.pg_get_functiondef(p.oid)
            FROM pg_catalog.pg_proc p JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace
            WHERE {schemaFilter} ORDER BY n.nspname,p.proname,p.oid
            """,
            $"""
            SELECT n.nspname,c.relname,t.tgname,pg_catalog.pg_get_triggerdef(t.oid)
            FROM pg_catalog.pg_trigger t JOIN pg_catalog.pg_class c ON c.oid=t.tgrelid
            JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE {schemaFilter} AND NOT t.tgisinternal ORDER BY n.nspname,c.relname,t.tgname
            """,
            $"""
            SELECT n.nspname,c.relname,pg_catalog.pg_get_viewdef(c.oid)
            FROM pg_catalog.pg_class c JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
            WHERE {schemaFilter} AND c.relkind IN ('v','m') ORDER BY n.nspname,c.relname
            """
        })
            await AppendRowsAsync(connection, sql, entries, token);
        var tables = new List<(string Schema, string Table)>();
        await using (var command = new NpgsqlCommand("""
            SELECT schemaname,tablename FROM pg_catalog.pg_tables
            WHERE schemaname !~ '^pg_' AND schemaname <> 'information_schema'
            ORDER BY schemaname,tablename
            """, connection) { CommandTimeout = 3 })
        await using (var reader = await command.ExecuteReaderAsync(token))
            while (await reader.ReadAsync(token)) tables.Add((reader.GetString(0), reader.GetString(1)));
        foreach (var (schema, table) in tables)
        {
            entries.Add(JsonSerializer.Serialize(new { schema, table }));
            await AppendRowsAsync(connection, $"SELECT * FROM {Quote(schema)}.{Quote(table)}", entries, token);
        }
    }

    private static async Task AppendRowsAsync(NpgsqlConnection connection, string sql, List<string> entries,
        CancellationToken token)
    {
        await using var command = new NpgsqlCommand(sql, connection) { CommandTimeout = 3 };
        await using var reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token))
        {
            var row = new object?[reader.FieldCount];
            for (var index = 0; index < row.Length; index++)
                row[index] = await reader.IsDBNullAsync(index, token) ? null : reader.GetValue(index);
            entries.Add(JsonSerializer.Serialize(new { source = sql, values = row }));
        }
    }

    private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
