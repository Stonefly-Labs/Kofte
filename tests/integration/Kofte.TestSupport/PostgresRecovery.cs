using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Kofte.Host.Bootstrap;
using Npgsql;

namespace Kofte.TestSupport;

public sealed partial class PostgresResources
{
    private sealed record BackupRecord(string Format, string RunId, string Schema, int Major, string Sha256);
    private static readonly JsonSerializerOptions BackupOptions = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true
    };

    private sealed class RecoveryLease(PostgresResources fixture, NpgsqlConnection connection,
        int previousLimit) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try { await fixture.AdminAsync($"ALTER DATABASE \"{fixture.database}\" CONNECTION LIMIT {previousLimit}", cleanup.Token); }
            finally { await connection.DisposeAsync(); }
        }
    }

    public async Task<IAsyncDisposable> AcquireRecoveryAsync(CancellationToken token)
    {
        await ValidateOwnershipAsync(token);
        if (service is null) throw new InvalidOperationException("postgres_owned_image_required");
        var builder = new NpgsqlConnectionStringBuilder(adminConnection) { Database = database };
        var connection = new NpgsqlConnection(builder.ConnectionString);
        RecoveryLease? lease = null;
        try
        {
            await connection.OpenAsync(token);
            await using var command = new NpgsqlCommand("SELECT pg_catalog.pg_try_advisory_lock($1)", connection);
            command.Parameters.AddWithValue(PostgresPlatformStore.OperationLeaseKey);
            if (await command.ExecuteScalarAsync(token) is not true)
            {
                await using var sessions = new NpgsqlCommand("""
                    SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_stat_activity
                      WHERE datid=(SELECT oid FROM pg_catalog.pg_database WHERE datname=current_database())
                        AND application_name LIKE 'kofte_bootstrap_%')
                    """, connection);
                throw new InvalidOperationException(await sessions.ExecuteScalarAsync(token) is true
                    ? "postgres_host_must_stop" : "postgres_operation_busy");
            }
            var previous = (int)(await AdminAsync($"SELECT datconnlimit FROM pg_catalog.pg_database WHERE oid={databaseOid}", token)
                ?? throw new InvalidOperationException("postgres_database_record_missing"));
            lease = new RecoveryLease(this, connection, previous);
            await AdminAsync($"ALTER DATABASE \"{database}\" CONNECTION LIMIT 0", token);
            await using var otherSessions = new NpgsqlCommand("""
                SELECT count(*) FROM pg_catalog.pg_stat_activity
                  WHERE datid=(SELECT oid FROM pg_catalog.pg_database WHERE datname=current_database())
                    AND pid<>pg_catalog.pg_backend_pid()
                """, connection);
            if (await otherSessions.ExecuteScalarAsync(token) is not 0L)
                throw new InvalidOperationException("postgres_host_must_stop");
            return lease;
        }
        catch
        {
            if (lease is not null) await lease.DisposeAsync();
            else await connection.DisposeAsync();
            throw;
        }
    }

    private static string BackupPath(string value, bool createDirectory)
    {
        if (OperatingSystem.IsWindows() || !Path.IsPathFullyQualified(value) ||
            value.Split(Path.DirectorySeparatorChar).Contains(".."))
            throw new InvalidOperationException("postgres_backup_path_invalid");
        var path = Path.GetFullPath(value);
        var directory = Path.GetDirectoryName(path) ??
            throw new InvalidOperationException("postgres_backup_path_invalid");
        UnixStorePaths.ValidateRoot(directory, allowMissing: createDirectory);
        if (createDirectory) Directory.CreateDirectory(directory, UnixStorePaths.DirectoryMode);
        UnixStorePaths.ValidateRoot(directory, allowMissing: false);
        return path;
    }

    private static FileStream CreateBackupFile(string path)
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("postgres_platform_unsupported");
        return new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            UnixCreateMode = UnixStorePaths.FileMode
        });
    }

    public async Task BackupAsync(string output, CancellationToken token)
    {
        await using var lease = await AcquireRecoveryAsync(token);
        if (await AdminAsync("""
            SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_tables
              WHERE schemaname='kofte_system' AND (lower(tablename) LIKE '\_\_kofte\_%' ESCAPE '\'
                OR lower(tablename) LIKE '\_\_efmigrations%' ESCAPE '\'))
              OR EXISTS (SELECT 1 FROM pg_catalog.pg_namespace
                WHERE nspname !~ '^pg_' AND nspname NOT IN ('kofte_system','public','information_schema'))
            """, token, target: true) is not false)
            throw new InvalidOperationException("postgres_registry_requires_complete_recovery_set");
        var tools = service ?? throw new InvalidOperationException("postgres_owned_image_required");
        var path = BackupPath(output, createDirectory: true);
        if (UnixStorePaths.ValidateFile(path, allowMissing: true) ||
            UnixStorePaths.ValidateFile(path + ".metadata.json", allowMissing: true))
            throw new InvalidOperationException("postgres_backup_destination_exists");
        var candidate = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var file = CreateBackupFile(candidate))
                await tools.RunToolAsync("pg_dump", database, role, file, token);
            await using var archive = File.OpenRead(candidate);
            var digest = Convert.ToHexStringLower(await SHA256.HashDataAsync(archive, token));
            archive.Close();
            await using (var metadata = CreateBackupFile(candidate + ".metadata.json"))
                await JsonSerializer.SerializeAsync(metadata,
                    new BackupRecord("kofte-postgres-backup-v1", RunId, "kofte_system", 17, digest),
                    BackupOptions, token);
            File.Move(candidate, path, overwrite: false);
            File.Move(candidate + ".metadata.json", path + ".metadata.json", overwrite: false);
        }
        finally
        {
            if (File.Exists(candidate)) File.Delete(candidate);
            if (File.Exists(candidate + ".metadata.json")) File.Delete(candidate + ".metadata.json");
        }
    }

    public async Task RestoreAsync(string input, CancellationToken token)
    {
        var path = BackupPath(input, createDirectory: false);
        UnixStorePaths.ValidateFile(path);
        UnixStorePaths.ValidateFile(path + ".metadata.json");
        BackupRecord record;
        try
        {
            record = JsonSerializer.Deserialize<BackupRecord>(
                await File.ReadAllTextAsync(path + ".metadata.json", token), BackupOptions)
                ?? throw new InvalidOperationException("postgres_backup_invalid");
        }
        catch (JsonException) { throw new InvalidOperationException("postgres_backup_invalid"); }
        await using var archive = File.OpenRead(path);
        var digest = Convert.ToHexStringLower(await SHA256.HashDataAsync(archive, token));
        archive.Position = 0;
        var header = new byte[5];
        if (archive.Length < header.Length) throw new InvalidOperationException("postgres_backup_invalid");
        await archive.ReadExactlyAsync(header, token);
        if (record.Format != "kofte-postgres-backup-v1" || record.Schema != "kofte_system" ||
            record.Major != 17 || !Regex.IsMatch(record.RunId ?? "", @"\A[0-9a-f]{32}\z") ||
            record.Sha256 != digest || !header.AsSpan().SequenceEqual("PGDMP"u8))
            throw new InvalidOperationException("postgres_backup_invalid");
        archive.Position = 0;
        var validationTools = service ?? throw new InvalidOperationException("postgres_owned_image_required");
        await validationTools.ValidateLegacyArchiveAsync(archive, token);
        archive.Position = 0;
        await using var lease = await AcquireRecoveryAsync(token);
        var tools = service ?? throw new InvalidOperationException("postgres_owned_image_required");
        if (await AdminAsync("""
            SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_depend
              WHERE refclassid='pg_catalog.pg_namespace'::regclass
                AND refobjid=(SELECT oid FROM pg_catalog.pg_namespace WHERE nspname='kofte_system'))
            """, token, target: true) is not false)
            throw new InvalidOperationException("postgres_restore_target_not_empty");
        await tools.RunToolAsync("pg_restore", database, provisioner, archive, token);
        await AdminAsync($"""
            REASSIGN OWNED BY "{provisioner}" TO "{role}";
            COMMENT ON SCHEMA kofte_system IS '{ownership}';
            """, token, target: true);
    }
}
