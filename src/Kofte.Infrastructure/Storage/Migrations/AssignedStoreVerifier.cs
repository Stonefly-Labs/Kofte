using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using Kofte.Platform.Modules.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Kofte.Infrastructure.Storage.Migrations;

internal static class AssignedStoreVerifier
{
    internal static async Task VerifyAsync(AssignedDbContext context, IModel? checkpointModel,
        OperationBudget budget, CancellationToken token)
    {
        var binding = context.Storage;
        var expected = checkpointModel?.GetEntityTypes()
            .Where(entity => entity.GetTableName() is not null)
            .GroupBy(entity => entity.GetTableName()!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.SelectMany(entity =>
            {
                var table = StoreObjectIdentifier.Table(group.Key, binding.Schema);
                return entity.GetProperties().Select(property => (
                    Name: property.GetColumnName(table)!,
                    Type: property.GetColumnType()!,
                    Nullable: property.IsNullable,
                    Default: DefaultSql(property.FindAnnotation(RelationalAnnotationNames.DefaultValue)?.Value,
                        binding.Provider == "postgresql")));
            }).Distinct().ToDictionary(column => column.Name, StringComparer.Ordinal), StringComparer.Ordinal)
            ?? [];
        var connection = context.Database.GetDbConnection();
        await using (var objects = connection.CreateCommand())
        {
            objects.CommandTimeout = budget.CommandSeconds(sqlite: binding.Provider == "sqlite");
            objects.CommandText = binding.Provider == "postgresql" ? """
                SELECT (SELECT count(*) FROM pg_catalog.pg_class c
                  JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                  WHERE n.nspname=@schema AND c.relkind NOT IN ('r','i')) +
                  (SELECT count(*) FROM pg_catalog.pg_proc p
                    JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname=@schema) +
                  (SELECT count(*) FROM pg_catalog.pg_trigger t
                    JOIN pg_catalog.pg_class c ON c.oid=t.tgrelid
                    JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                    WHERE n.nspname=@schema AND NOT t.tgisinternal)
                """ : "SELECT count(*) FROM sqlite_master WHERE type NOT IN ('table','index') AND name NOT LIKE 'sqlite_%'";
            if (binding.Schema is { } schema) Parameter(objects, "schema", schema);
            if (await objects.ExecuteScalarAsync(token) is not 0L) Reject();
        }
        await using (var tables = connection.CreateCommand())
        {
            tables.CommandTimeout = budget.CommandSeconds(sqlite: binding.Provider == "sqlite");
            tables.CommandText = binding.Provider == "postgresql"
                ? "SELECT tablename FROM pg_catalog.pg_tables WHERE schemaname=@schema"
                : "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
            if (binding.Schema is { } schema) Parameter(tables, "schema", schema);
            await using var reader = await tables.ExecuteReaderAsync(token);
            var actual = new HashSet<string>(StringComparer.Ordinal);
            while (await reader.ReadAsync(token))
            {
                var name = reader.GetString(0);
                if (name is "__kofte_storage_owner" or "__EFMigrationsHistory" ||
                    binding.Provider == "sqlite" && name == "__EFMigrationsLock") continue;
                actual.Add(name);
            }
            if (!actual.SetEquals(expected.Keys)) Reject();
        }
        foreach (var (table, columns) in expected)
        {
            if (binding.Provider == "sqlite")
                await VerifySqliteDefinitionAsync(connection, table, budget, token);
            await using var command = connection.CreateCommand();
            command.CommandTimeout = budget.CommandSeconds(sqlite: binding.Provider == "sqlite");
            command.CommandText = binding.Provider == "postgresql"
                ? "SELECT column_name,data_type,is_nullable,column_default,is_identity,is_generated,collation_name FROM information_schema.columns WHERE table_schema=@schema AND table_name=@table"
                : $"PRAGMA table_xinfo(\"{table}\")";
            if (binding.Schema is { } schema)
            {
                Parameter(command, "schema", schema);
                Parameter(command, "table", table);
            }
            await using var reader = await command.ExecuteReaderAsync(token);
            var actual = new Dictionary<string, (string Type, bool Nullable, string? Default)>(StringComparer.Ordinal);
            while (await reader.ReadAsync(token))
            {
                if (binding.Provider == "postgresql")
                {
                    if (reader.GetString(4) != "NO" || reader.GetString(5) != "NEVER" || !reader.IsDBNull(6)) Reject();
                    actual.Add(reader.GetString(0), (reader.GetString(1), reader.GetString(2) == "YES",
                        reader.IsDBNull(3) ? null : reader.GetString(3)));
                }
                else
                {
                    if (reader.GetInt32(6) != 0) Reject();
                    actual.Add(reader.GetString(1), (reader.GetString(2), reader.GetInt32(3) == 0,
                        reader.IsDBNull(4) ? null : reader.GetString(4)));
                }
            }
            if (columns.Count != actual.Count) Reject();
            foreach (var (name, column) in columns)
                if (!actual.TryGetValue(name, out var found) || found.Type != column.Type ||
                    found.Nullable != column.Nullable || found.Default != column.Default) Reject();
            await reader.DisposeAsync();
            var entity = checkpointModel!.GetEntityTypes().First(item => item.GetTableName() == table);
            var store = StoreObjectIdentifier.Table(table, binding.Schema);
            var expectedPrimary = entity.FindPrimaryKey()?.Properties.Select(property => property.GetColumnName(store)!)
                .ToArray() ?? [];
            command.CommandText = binding.Provider == "postgresql" ? """
                SELECT a.attname FROM pg_catalog.pg_constraint k
                  JOIN pg_catalog.pg_class c ON c.oid=k.conrelid
                  JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                  CROSS JOIN LATERAL unnest(k.conkey) WITH ORDINALITY AS key(attnum,position)
                  JOIN pg_catalog.pg_attribute a ON a.attrelid=c.oid AND a.attnum=key.attnum
                  WHERE n.nspname=@schema AND c.relname=@table AND k.contype='p' ORDER BY key.position
                """ : $"SELECT name FROM pragma_table_info('{table}') WHERE pk>0 ORDER BY pk";
            var primary = new List<string>();
            await using (var keys = await command.ExecuteReaderAsync(token))
                while (await keys.ReadAsync(token)) primary.Add(keys.GetString(0));
            if (!primary.SequenceEqual(expectedPrimary, StringComparer.Ordinal)) Reject();
            command.CommandText = binding.Provider == "postgresql" ? """
                SELECT (SELECT count(*) FROM pg_catalog.pg_index i
                  JOIN pg_catalog.pg_class c ON c.oid=i.indrelid
                  JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                  WHERE n.nspname=@schema AND c.relname=@table AND NOT i.indisprimary) +
                  (SELECT count(*) FROM pg_catalog.pg_constraint k
                    JOIN pg_catalog.pg_class c ON c.oid=k.conrelid
                    JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace
                    WHERE n.nspname=@schema AND c.relname=@table AND k.contype<>'p')
                """ : $"SELECT (SELECT count(*) FROM pragma_index_list('{table}') WHERE origin<>'pk') + " +
                    $"(SELECT count(*) FROM pragma_foreign_key_list('{table}'))";
            if (await command.ExecuteScalarAsync(token) is not 0L) Reject();
        }
    }

    internal static async Task VerifySqliteDefinitionAsync(DbConnection connection, string table,
        OperationBudget budget, CancellationToken token, DbTransaction? transaction = null)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandTimeout = budget.CommandSeconds(sqlite: true);
        command.CommandText = "SELECT sql FROM sqlite_master WHERE type='table' AND name=@table";
        Parameter(command, "table", table);
        var definition = await command.ExecuteScalarAsync(token) as string ??
            throw new StorageException(StorageCode.ReconciliationRequired);
        if (definition.Length > 65536) Reject();
        // PRAGMA omits CHECK/collation/conflict clauses; inspect only unquoted SQL tokens.
        const RegexOptions options = RegexOptions.CultureInvariant | RegexOptions.Singleline |
            RegexOptions.NonBacktracking;
        var tokens = Regex.Replace(definition,
            """'(?:''|[^'])*'|"(?:""|[^"])*"|`(?:``|[^`])*`|\[[^\]]*\]|--[^\r\n]*|/\*.*?\*/""",
            " ", options);
        if (Regex.IsMatch(tokens,
            @"\b(?:CHECK|COLLATE|UNIQUE|REFERENCES|GENERATED|AUTOINCREMENT|STRICT|WITHOUT|CONFLICT|DEFERRABLE|VIRTUAL)\b",
            options | RegexOptions.IgnoreCase)) Reject();
    }

    private static string? DefaultSql(object? value, bool postgres) => value switch
    {
        null => null,
        int number => number.ToString(CultureInfo.InvariantCulture),
        string text => "'" + text.Replace("'", "''", StringComparison.Ordinal) + "'" + (postgres ? "::text" : ""),
        _ => throw new StorageException(StorageCode.ReconciliationRequired)
    };

    private static void Parameter(DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static void Reject() => throw new StorageException(StorageCode.ReconciliationRequired);
}
