using System.Data.Common;
using Kofte.Platform.Modules.Manifest;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Kofte.TestSupport;

public sealed record StorageRegistrationSnapshot(string Provider, string? Key, Guid? AllocationId,
    string State, string MigrationState, string? Target, string? Checkpoint, long Revision);

public static class StorageInspection
{
    public static async Task<StorageRegistrationSnapshot?> ReadAsync(string provider, string? root,
        string? runtime, ModuleId owner, CancellationToken token)
    {
        StorageScenarioCatalog.RequireProvider(provider);
        await using DbConnection connection = provider == "sqlite"
            ? new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(root ?? throw new InvalidOperationException("storage_root_required"),
                    "platform", "platform.db"),
                Mode = SqliteOpenMode.ReadOnly,
                Pooling = false,
                DefaultTimeout = 1
            }.ToString())
            : new NpgsqlConnection(runtime ?? throw new InvalidOperationException("storage_provider_required"));
        await connection.OpenAsync(token);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = provider == "sqlite" ? 1 : 3;
        var prefix = provider == "sqlite" ? "" : "kofte_system.";
        command.CommandText = $"""
            SELECT provider,allocation_key,allocation_id,state,migration_state,migration_target,migration_checkpoint,revision
            FROM {prefix}__kofte_module_registrations WHERE module_id=@owner
            """;
        var parameter = command.CreateParameter();
        parameter.ParameterName = "owner";
        parameter.Value = owner.Value;
        command.Parameters.Add(parameter);
        await using var reader = await command.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token)) return null;
        string? Text(int ordinal) => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        var row = new StorageRegistrationSnapshot(reader.GetString(0), Text(1),
            reader.IsDBNull(2) ? null : reader.GetGuid(2), reader.GetString(3), reader.GetString(4),
            Text(5), Text(6), reader.GetInt64(7));
        if (await reader.ReadAsync(token)) throw new InvalidOperationException("storage_owner_not_unique");
        return row;
    }
}
