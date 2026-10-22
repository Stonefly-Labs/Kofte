using System.Data.Common;
using Kofte.Platform.Modules.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Kofte.Infrastructure.Storage.Migrations;

internal static class AssignedStoreVerifier
{
    internal static async Task VerifyAsync(AssignedDbContext context, IModel? checkpointModel,
        CancellationToken token)
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
                    Nullable: property.IsNullable));
            }).Distinct().ToDictionary(column => column.Name, StringComparer.Ordinal), StringComparer.Ordinal)
            ?? [];
        var connection = context.Database.GetDbConnection();
        await using (var tables = connection.CreateCommand())
        {
            tables.CommandTimeout = binding.Provider == "sqlite" ? 1 : 3;
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
            await using var command = connection.CreateCommand();
            command.CommandTimeout = binding.Provider == "sqlite" ? 1 : 3;
            command.CommandText = binding.Provider == "postgresql"
                ? "SELECT column_name,data_type,is_nullable FROM information_schema.columns WHERE table_schema=@schema AND table_name=@table"
                : $"PRAGMA table_info(\"{table}\")";
            if (binding.Schema is { } schema)
            {
                Parameter(command, "schema", schema);
                Parameter(command, "table", table);
            }
            await using var reader = await command.ExecuteReaderAsync(token);
            var actual = new Dictionary<string, (string Type, bool Nullable)>(StringComparer.Ordinal);
            while (await reader.ReadAsync(token))
            {
                if (binding.Provider == "postgresql")
                    actual.Add(reader.GetString(0), (reader.GetString(1), reader.GetString(2) == "YES"));
                else actual.Add(reader.GetString(1), (reader.GetString(2), reader.GetInt32(3) == 0));
            }
            if (columns.Count != actual.Count) Reject();
            foreach (var (name, column) in columns)
                if (!actual.TryGetValue(name, out var found) || found.Type != column.Type ||
                    found.Nullable != column.Nullable) Reject();
        }
    }

    private static void Parameter(DbCommand command, string name, string value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static void Reject() => throw new StorageException(StorageCode.ReconciliationRequired);
}
