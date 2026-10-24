using System.Data.Common;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;

namespace Kofte.Infrastructure.Storage.Registry;

internal sealed class RegistryMigrationRunner(StorageInstallation installation)
{
    internal string Prefix => installation.Provider == "postgresql" ? "kofte_system." : "";
    internal string Registrations => Prefix + "__kofte_module_registrations";
    private string Metadata => Prefix + "__kofte_installation";
    private string History => Prefix + "__kofte_registry_migrations";
    private static readonly string[] ExpectedTables =
        ["__kofte_installation", "__kofte_module_registrations", "__kofte_registry_migrations"];

    internal string Script
    {
        get
        {
            var provider = installation.Provider == "postgresql" ? "Postgres" : "Sqlite";
            using var resource = typeof(RegistryMigrationRunner).Assembly.GetManifestResourceStream(
                $"Kofte.Infrastructure.Storage.Registry.Migrations.{provider}.001_registry.sql")
                ?? throw new InvalidOperationException("storage_migration_resource_missing");
            using var reader = new StreamReader(resource);
            return reader.ReadToEnd();
        }
    }

    internal string Digest => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Script)));

    internal async Task UpgradeAsync(MaintenanceScope scope, OperationBudget budget, MutationEvidence mutation)
    {
        await scope.VerifyAsync(installation, budget);
        await using var connection = await installation.OpenPlatformAsync(budget);
        var tables = await TablesAsync(connection, budget);
        if (tables.Count != 0)
        {
            await ReadInstallationAsync(connection, budget);
            return;
        }
        await ValidateV0Async(connection, budget);
        if (connection is SqliteConnection sqlite)
        {
            using var transaction = sqlite.BeginTransaction(deferred: false);
            await InitializeAsync(connection, budget, transaction);
            await scope.VerifyAsync(installation, budget);
            budget.Check();
            mutation.PossibleCommit();
            StorageDiagnostics.Checkpoint("registry_before_commit", installation.Provider);
            transaction.Commit();
            mutation.Committed();
            StorageDiagnostics.Checkpoint("registry_committed", installation.Provider);
        }
        else
        {
            await using var transaction = await connection.BeginTransactionAsync(budget.Token);
            await InitializeAsync(connection, budget, transaction);
            await scope.VerifyAsync(installation, budget);
            mutation.PossibleCommit();
            StorageDiagnostics.Checkpoint("registry_before_commit", installation.Provider);
            await transaction.CommitAsync(budget.Token);
            mutation.Committed();
            StorageDiagnostics.Checkpoint("registry_committed", installation.Provider);
        }
        await ReadInstallationAsync(connection, budget);
    }

    internal async Task ValidateV0Async(DbConnection connection, OperationBudget budget)
    {
        if ((await TablesAsync(connection, budget)).Count != 0)
            throw new StorageException(StorageCode.Corrupt);
        if (connection is SqliteConnection)
        {
            if (await ScalarAsync(connection, "PRAGMA user_version", budget) is not 0L)
                throw new StorageException(StorageCode.VersionUnsupported);
            var modules = Path.Combine(installation.Root!, "modules");
            UnixStorePaths.ValidateRoot(modules, allowMissing: true);
            if (Directory.Exists(modules) && Directory.EnumerateFileSystemEntries(modules).Any())
                throw new StorageException(StorageCode.ReconciliationRequired);
        }
        else if (await ScalarAsync(connection, """
            SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace
              WHERE nspname !~ '^pg_' AND nspname NOT IN ('information_schema','public','kofte_system'))
              OR EXISTS (SELECT 1 FROM pg_catalog.pg_class c
                JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='public')
              OR EXISTS (SELECT 1 FROM pg_catalog.pg_proc p
                JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='public')
              OR EXISTS (SELECT 1 FROM pg_catalog.pg_type t
                JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace WHERE n.nspname='public')
            """, budget) is true)
            throw new StorageException(StorageCode.ReconciliationRequired);
    }

    private async Task InitializeAsync(DbConnection connection, OperationBudget budget, DbTransaction transaction)
    {
        await ExecuteAsync(connection, Script, budget, transaction);
        await ExecuteAsync(connection, $"""
            INSERT INTO {Metadata} (singleton_key, installation_id, provider, registry_version, registry_revision, created_at_utc)
            VALUES (1, @id, @provider, 1, 0, @created);
            INSERT INTO {History} (version, sha256, applied_at_utc) VALUES (1, @digest, @created);
            """, budget, transaction, ("id", Guid.NewGuid()), ("provider", installation.Provider),
            ("created", DateTime.UtcNow), ("digest", Digest));
    }

    private async Task<List<string>> TablesAsync(DbConnection connection, OperationBudget budget)
    {
        var query = installation.Provider == "postgresql"
            ? "SELECT tablename FROM pg_catalog.pg_tables WHERE schemaname='kofte_system' ORDER BY tablename"
            : "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name";
        await using var command = Command(connection, query, budget);
        await using var reader = await command.ExecuteReaderAsync(budget.Token);
        var result = new List<string>();
        while (await reader.ReadAsync(budget.Token)) result.Add(reader.GetString(0));
        return result;
    }

    internal async Task<bool> HasRegistryAsync(DbConnection connection, OperationBudget budget) =>
        (await TablesAsync(connection, budget)).Count != 0;

    internal async Task<InstallationRecord> ReadInstallationAsync(DbConnection connection, OperationBudget budget)
    {
        if (!(await TablesAsync(connection, budget)).SequenceEqual(ExpectedTables.Order(StringComparer.Ordinal)))
            throw new StorageException(StorageCode.Corrupt);
        await using var command = Command(connection,
            $"SELECT singleton_key, installation_id, provider, registry_version, registry_revision FROM {Metadata}", budget);
        InstallationRecord record;
        await using (var reader = await command.ExecuteReaderAsync(budget.Token))
        {
            if (!await reader.ReadAsync(budget.Token) || reader.GetInt32(0) != 1)
                throw new StorageException(StorageCode.Corrupt);
            record = new(reader.GetGuid(1), reader.GetString(2), reader.GetInt32(3), reader.GetInt64(4));
            if (await reader.ReadAsync(budget.Token) || record.Id == Guid.Empty || record.Revision < 0 ||
                record.Provider != installation.Provider)
                throw new StorageException(StorageCode.Corrupt);
            if (record.Version != 1) throw new StorageException(StorageCode.VersionUnsupported);
        }
        await using var history = Command(connection, $"SELECT version, sha256 FROM {History}", budget);
        await using (var reader = await history.ExecuteReaderAsync(budget.Token))
        {
            if (!await reader.ReadAsync(budget.Token) || reader.GetInt32(0) != 1 ||
                reader.GetString(1) != Digest || await reader.ReadAsync(budget.Token))
                throw new StorageException(StorageCode.Corrupt);
        }
        if (connection is SqliteConnection)
        {
            if (await ScalarAsync(connection, "PRAGMA user_version", budget) is not 1L ||
                await ScalarAsync(connection, "PRAGMA integrity_check", budget) is not "ok")
                throw new StorageException(StorageCode.Corrupt);
        }
        var owners = new HashSet<string>(StringComparer.Ordinal);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var allocations = new HashSet<Guid>();
        long revision = 0;
        await using var registrations = Command(connection, $"SELECT * FROM {Registrations}", budget);
        await using var rows = await registrations.ExecuteReaderAsync(budget.Token);
        while (await rows.ReadAsync(budget.Token))
        {
            var row = Read(rows);
            row.Validate(record);
            if (!owners.Add(row.Owner.Value) || row.Key is not null && !keys.Add(row.Key) ||
                row.AllocationId is { } id && !allocations.Add(id))
                throw new StorageException(StorageCode.Corrupt);
            if (row.Revision > long.MaxValue - revision) throw new StorageException(StorageCode.Corrupt);
            revision += row.Revision;
        }
        if (revision != record.Revision) throw new StorageException(StorageCode.Corrupt);
        return record;
    }

    internal async Task<RegistrationRecord?> FindAsync(DbConnection connection, ModuleId owner, OperationBudget budget)
    {
        await using var command = Command(connection,
            $"SELECT * FROM {Registrations} WHERE module_id=@owner", budget, null, ("owner", owner.Value));
        await using var reader = await command.ExecuteReaderAsync(budget.Token);
        if (!await reader.ReadAsync(budget.Token)) return null;
        var record = Read(reader);
        if (await reader.ReadAsync(budget.Token)) throw new StorageException(StorageCode.Corrupt);
        return record;
    }

    internal static RegistrationRecord Read(DbDataReader reader)
    {
        if (!ModuleId.TryParse(reader.GetString(reader.GetOrdinal("module_id")), out var owner))
            throw new StorageException(StorageCode.Corrupt);
        string? Text(string name) => reader.IsDBNull(reader.GetOrdinal(name)) ? null :
            reader.GetString(reader.GetOrdinal(name));
        var version = reader.GetOrdinal("allocator_version");
        var allocation = reader.GetOrdinal("allocation_id");
        return new(owner, reader.GetBoolean(reader.GetOrdinal("relational")), Text("provider")!,
            Text("allocation_key"), reader.IsDBNull(version) ? null : reader.GetInt32(version),
            reader.IsDBNull(allocation) ? null : reader.GetGuid(allocation), Text("state")!,
            Text("migration_state")!, Text("migration_target"), Text("migration_checkpoint"),
            Text("last_outcome_code"), reader.GetInt64(reader.GetOrdinal("revision")));
    }

    internal static DbCommand Command(DbConnection connection, string sql, OperationBudget budget,
        DbTransaction? transaction = null, params (string Name, object Value)[] parameters)
    {
        budget.Check();
        var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = budget.CommandSeconds(connection is SqliteConnection);
        command.Transaction = transaction;
        foreach (var (name, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = connection is SqliteConnection && value is Guid id ? id.ToString("D") : value;
            command.Parameters.Add(parameter);
        }
        return command;
    }

    internal static async Task ExecuteAsync(DbConnection connection, string sql, OperationBudget budget,
        DbTransaction? transaction = null, params (string Name, object Value)[] parameters)
    {
        await using var command = Command(connection, sql, budget, transaction, parameters);
        await command.ExecuteNonQueryAsync(budget.Token);
        budget.Check();
    }

    internal static async Task<object?> ScalarAsync(DbConnection connection, string sql, OperationBudget budget)
    {
        await using var command = Command(connection, sql, budget);
        return await command.ExecuteScalarAsync(budget.Token);
    }
}
