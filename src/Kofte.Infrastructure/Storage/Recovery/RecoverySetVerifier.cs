using System.Data.Common;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Kofte.Infrastructure.Storage.Allocation;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;

namespace Kofte.Infrastructure.Storage.Recovery;

public static class RecoverySetVerifier
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        AllowDuplicateProperties = false
    };

    public static Task<RecoveryInventory> InspectStoppedAsync(StorageInstallation installation,
        IStorageMaintenanceScope scope, CancellationToken cancellationToken = default) =>
        StorageDiagnostics.ObserveRecoveryAsync(installation.Provider, cancellationToken, async (budget, _, _) =>
        {
            if (scope is not MaintenanceScope owned) throw new StorageException(StorageCode.RequestInvalid);
            await owned.VerifyAsync(installation, budget);
            var inventory = await ReadInventoryAsync(installation, budget);
            await owned.VerifyAsync(installation, budget);
            return inventory;
        });

    public static async Task<RecoveryArtifact> DescribeArtifactAsync(string directory, string name,
        CancellationToken cancellationToken = default)
    {
        var path = SafeArtifact(directory, name);
        using var file = File.OpenRead(path);
        return new(name, file.Length,
            Convert.ToHexStringLower(await SHA256.HashDataAsync(file, cancellationToken)));
    }

    public static Task WriteManifestAsync(string directory, RecoveryManifest manifest,
        CancellationToken cancellationToken = default) =>
        StorageDiagnostics.ObserveRecoveryAsync<object?>(null, cancellationToken, async (budget, mutation, setProvider) =>
        {
            ValidateManifest(manifest);
            setProvider(manifest.Provider);
            UnixStorePaths.ValidateRoot(directory, allowMissing: false);
            if (OperatingSystem.IsWindows()) throw new StorageException(StorageCode.PathRejected);
            budget.Check();
            await using (var file = new FileStream(Path.Combine(directory, RecoveryManifest.FileName), new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Share = FileShare.None,
                UnixCreateMode = UnixStorePaths.FileMode
            }))
            {
                mutation.PossibleCommit();
                StorageDiagnostics.Checkpoint("recovery_manifest_created", manifest.Provider);
                await JsonSerializer.SerializeAsync(file, manifest, Options, budget.Token);
                await file.FlushAsync(budget.Token);
            }
            mutation.Committed();
            StorageDiagnostics.Checkpoint("recovery_manifest_committed", manifest.Provider);
            return null;
        });

    public static Task<RecoveryManifest> VerifyAsync(string directory,
        StorageInstallation? restoredPostgres = null, IStorageMaintenanceScope? scope = null,
        CancellationToken cancellationToken = default) =>
        StorageDiagnostics.ObserveRecoveryAsync(restoredPostgres?.Provider, cancellationToken, async (budget, _, setProvider) =>
        {
            try { return await VerifyCoreAsync(directory, restoredPostgres, scope, budget, setProvider); }
            catch (StorePathException) { throw new StorageException(StorageCode.PathRejected); }
            catch (Exception error) when (error is SqliteException or Npgsql.NpgsqlException or IOException or
                InvalidOperationException or UnauthorizedAccessException)
            { throw new StorageException(StorageCode.Corrupt); }
        });

    private static async Task<RecoveryManifest> VerifyCoreAsync(string directory,
        StorageInstallation? restoredPostgres, IStorageMaintenanceScope? scope,
        OperationBudget budget, Action<string> setProvider)
    {
        UnixStorePaths.ValidateRoot(directory, allowMissing: false);
        var metadata = Path.Combine(directory, RecoveryManifest.FileName);
        UnixStorePaths.ValidateFile(metadata);
        if (new FileInfo(metadata).Length > 1024 * 1024)
            throw new StorageException(StorageCode.Corrupt);
        RecoveryManifest manifest;
        try
        {
            await using var json = File.OpenRead(metadata);
            manifest = await JsonSerializer.DeserializeAsync<RecoveryManifest>(json, Options, budget.Token)
                ?? throw new StorageException(StorageCode.Corrupt);
        }
        catch (JsonException) { throw new StorageException(StorageCode.Corrupt); }
        ValidateManifest(manifest);
        if (restoredPostgres is null) setProvider(manifest.Provider);
        foreach (var artifact in manifest.Artifacts)
        {
            budget.Check();
            if (await DescribeArtifactAsync(directory, artifact.Name, budget.Token) != artifact)
                throw new StorageException(StorageCode.Corrupt);
        }
        var expected = manifest.Artifacts.Select(item => item.Name).Append(RecoveryManifest.FileName)
            .ToHashSet(StringComparer.Ordinal);
        var directories = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in expected)
        {
            var parent = Path.GetDirectoryName(name);
            while (!string.IsNullOrEmpty(parent))
            {
                directories.Add(parent.Replace(Path.DirectorySeparatorChar, '/'));
                parent = Path.GetDirectoryName(parent);
            }
        }
        var actual = EnumerateFiles(directory, directory, directories)
            .Where(name => name != ".platform.lock").ToHashSet(StringComparer.Ordinal);
        if (!expected.SetEquals(actual)) throw new StorageException(StorageCode.ReconciliationRequired);
        StorageInstallation installation;
        if (manifest.Provider == "sqlite")
        {
            if (restoredPostgres is not null || scope is not null)
                throw new StorageException(StorageCode.RequestInvalid);
            installation = StorageInstallation.Sqlite(directory);
        }
        else
        {
            if (restoredPostgres?.Provider != "postgresql" || scope is not MaintenanceScope owned)
                throw new StorageException(StorageCode.RequestInvalid);
            await owned.VerifyAsync(restoredPostgres, budget);
            installation = restoredPostgres;
            using var dump = File.OpenRead(SafeArtifact(directory, manifest.Artifacts.Single().Name));
            var header = new byte[5];
            await dump.ReadExactlyAsync(header, budget.Token);
            if (!header.AsSpan().SequenceEqual("PGDMP"u8))
                throw new StorageException(StorageCode.Corrupt);
        }
        var inventory = await ReadInventoryAsync(installation, budget);
        if (!SameInventory(manifest, inventory)) throw new StorageException(StorageCode.ReconciliationRequired);
        if (scope is MaintenanceScope held) await held.VerifyAsync(installation, budget);
        return manifest;
    }

    public static bool SameInventory(RecoveryManifest left, RecoveryInventory right) =>
        left.InstallationId == right.InstallationId && left.Provider == right.Provider &&
        left.RegistryVersion == right.RegistryVersion && left.RegistryRevision == right.RegistryRevision &&
        JsonSerializer.Serialize(left.Allocations, Options) == JsonSerializer.Serialize(right.Allocations, Options);

    internal static async Task ValidateInventoryAsync(StorageInstallation installation, DbConnection platform,
        OperationBudget budget) => _ = await ReadInventoryAsync(installation, platform, budget);

    private static void ValidateManifest(RecoveryManifest manifest)
    {
        if (manifest.Format != RecoveryManifest.CurrentFormat || !manifest.Complete ||
            manifest.RecoverySetId == Guid.Empty || manifest.InstallationId == Guid.Empty ||
            manifest.Provider is not ("sqlite" or "postgresql") ||
            manifest.RegistryVersion != 1 || manifest.RegistryRevision < 0 ||
            manifest.Allocations is null || manifest.Artifacts is null ||
            manifest.Allocations.Count > 4096 || manifest.Artifacts.Count > 4097)
            throw new StorageException(StorageCode.Corrupt);
        var owners = new HashSet<string>(StringComparer.Ordinal);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var tokens = new HashSet<Guid>();
        foreach (var row in manifest.Allocations)
        {
            if (row is null || !Platform.Modules.Manifest.ModuleId.TryParse(row.ModuleId, out _) ||
                !owners.Add(row.ModuleId) || row.History is null || row.History.Count > 4096 ||
                row.Revision < 1)
                throw new StorageException(StorageCode.Corrupt);
            if (!row.Relational)
            {
                if (row.AllocationKey is not null || row.AllocationId is not null ||
                    row.AllocatorVersion is not null || row.MarkerVersion is not null ||
                    row.State != "no_storage" || row.MigrationState != "none" ||
                    row.MigrationTarget is not null || row.MigrationCheckpoint is not null || row.History.Count != 0)
                    throw new StorageException(StorageCode.Corrupt);
                continue;
            }
            StorageAllocator.Validate(row.AllocationKey ?? "");
            if (!keys.Add(row.AllocationKey!) || row.AllocationId is not { } token || token == Guid.Empty ||
                !tokens.Add(token) || row.AllocatorVersion != 1 || row.MarkerVersion != 1 ||
                row.State != "ready" || row.MigrationState is not ("none" or "current"))
                throw new StorageException(StorageCode.ReconciliationRequired);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var history in row.History)
                if (history is null || !Bounded(history.Id, 150) ||
                    !Regex.IsMatch(history.Id, @"\A[A-Za-z0-9][A-Za-z0-9_.-]*\z") ||
                    !Bounded(history.ProductVersion, 64) ||
                    !Regex.IsMatch(history.ProductVersion, @"\A[0-9]+(\.[0-9]+){1,3}([+-][A-Za-z0-9.-]+)?\z") ||
                    !ids.Add(history.Id))
                    throw new StorageException(StorageCode.Corrupt);
            if (!row.History.Select(item => item.Id).SequenceEqual(ids.Order(StringComparer.Ordinal)) ||
                (row.MigrationState == "none" && (row.History.Count != 0 ||
                    row.MigrationCheckpoint is not null || row.MigrationTarget is not null)) ||
                (row.MigrationState == "current" && (row.History.Count == 0 ||
                    row.MigrationCheckpoint != row.History[^1].Id ||
                    row.MigrationTarget != row.MigrationCheckpoint)))
                throw new StorageException(StorageCode.ReconciliationRequired);
        }
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var artifact in manifest.Artifacts)
            if (artifact is null || !names.Add(artifact.Name) || artifact.Length < 100 ||
                !Regex.IsMatch(artifact.Sha256 ?? "", @"\A[0-9a-f]{64}\z") ||
                !SafeName(artifact.Name))
                throw new StorageException(StorageCode.Corrupt);
        var expected = manifest.Provider == "sqlite"
            ? keys.Select(key => $"modules/{key}/module.db").Append("platform/platform.db")
            : ["installation.dump"];
        if (!names.SetEquals(expected)) throw new StorageException(StorageCode.ReconciliationRequired);
        if (!manifest.Allocations.Select(row => row.ModuleId)
            .SequenceEqual(owners.Order(StringComparer.Ordinal)))
            throw new StorageException(StorageCode.Corrupt);
    }

    private static bool Bounded(string? value, int maximum) => value is { Length: > 0 } &&
        value.Length <= maximum && value.All(c => c is >= ' ' and <= '~');

    private static bool SafeName(string? name) => name is { Length: > 0 and < 256 } &&
        !Path.IsPathFullyQualified(name) && !name.Contains('\\') &&
        name.Split('/').All(part => part is not ("" or "." or "..") &&
            part.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-' or '.'));

    private static string SafeArtifact(string directory, string name)
    {
        if (!SafeName(name)) throw new StorageException(StorageCode.PathRejected);
        UnixStorePaths.ValidateRoot(directory, allowMissing: false);
        var path = Path.Combine(directory, name);
        UnixStorePaths.ValidateRoot(Path.GetDirectoryName(path)!, allowMissing: false);
        UnixStorePaths.ValidateFile(path);
        return path;
    }

    private static IEnumerable<string> EnumerateFiles(string root, string directory, HashSet<string> directories)
    {
        UnixStorePaths.ValidateRoot(directory, allowMissing: false);
        foreach (var path in Directory.EnumerateFileSystemEntries(directory))
        {
            if (new FileInfo(path).LinkTarget is not null)
                throw new StorageException(StorageCode.PathRejected);
            if (Directory.Exists(path))
            {
                if (!directories.Contains(Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/')))
                    throw new StorageException(StorageCode.ReconciliationRequired);
                foreach (var file in EnumerateFiles(root, path, directories)) yield return file;
            }
            else
            {
                UnixStorePaths.ValidateFile(path);
                yield return Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
            }
        }
    }

    internal static SqliteConnection OpenReadOnly(string path)
    {
        UnixStorePaths.ValidateRoot(Path.GetDirectoryName(path)!, allowMissing: false);
        UnixStorePaths.ValidateFile(path);
        foreach (var suffix in new[] { "-journal", "-wal", "-shm" })
            if (UnixStorePaths.ValidateFile(path + suffix, allowMissing: true))
                throw new StorageException(StorageCode.ReconciliationRequired);
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false,
            ForeignKeys = true,
            DefaultTimeout = 1
        }.ToString());
        try { connection.Open(); return connection; }
        catch { connection.Dispose(); throw; }
    }

    internal static async Task<RecoveryInventory> ReadInventoryAsync(StorageInstallation installation,
        OperationBudget budget)
    {
        await using var platform = installation.Provider == "sqlite"
            ? OpenReadOnly(installation.PlatformPath) : await installation.OpenPlatformAsync(budget);
        return await ReadInventoryAsync(installation, platform, budget);
    }

    private static async Task<RecoveryInventory> ReadInventoryAsync(StorageInstallation installation,
        DbConnection platform, OperationBudget budget)
    {
        var runner = new RegistryMigrationRunner(installation);
        var metadata = await runner.ReadInstallationAsync(platform, budget);
        var rows = new List<RegistrationRecord>();
        await using (var command = RegistryMigrationRunner.Command(platform,
            $"SELECT * FROM {runner.Registrations} ORDER BY module_id", budget))
        await using (var reader = await command.ExecuteReaderAsync(budget.Token))
            while (await reader.ReadAsync(budget.Token)) rows.Add(RegistryMigrationRunner.Read(reader));
        var inventory = new List<RecoveryAllocation>();
        foreach (var row in rows)
        {
            budget.Check();
            row.Validate(metadata);
            var history = new List<RecoveryMigration>();
            if (row.Relational)
            {
                if (installation.Provider == "sqlite")
                {
                    await using var module = OpenReadOnly(Path.Combine(installation.Root!, "modules", row.Key!, "module.db"));
                    if (await RegistryMigrationRunner.ScalarAsync(module, "PRAGMA integrity_check", budget) is not "ok" ||
                        await RegistryMigrationRunner.ScalarAsync(module, "PRAGMA journal_mode", budget) is not "delete")
                        throw new StorageException(StorageCode.Corrupt);
                    await StorageOwnerMarker.VerifyAsync(installation, module, metadata, row, budget);
                    await ReadHistoryAsync(module, "", history, budget);
                }
                else
                {
                    await new StorageRegistry(installation).VerifyPhysicalAsync(metadata, row, budget);
                    await ReadHistoryAsync(platform, StorageAllocator.Quote(row.Key!) + ".", history, budget);
                }
            }
            inventory.Add(new(row.Owner.Value, row.Relational, row.Key, row.AllocationId, row.AllocatorVersion,
                row.Relational ? 1 : null, row.State, row.MigrationState, row.Target, row.Checkpoint,
                row.Revision, history));
        }
        var keys = rows.Where(row => row.Relational).Select(row => row.Key!).ToHashSet(StringComparer.Ordinal);
        if (installation.Root is { } root)
        {
            var modules = Path.Combine(root, "modules");
            UnixStorePaths.ValidateRoot(modules, allowMissing: true);
            var actual = Directory.Exists(modules) ? Directory.EnumerateFileSystemEntries(modules)
                .Select(path => Path.GetFileName(path)!).ToHashSet(StringComparer.Ordinal) : [];
            if (!keys.SetEquals(actual)) throw new StorageException(StorageCode.ReconciliationRequired);
            foreach (var key in keys)
            {
                var directory = Path.Combine(modules, key);
                UnixStorePaths.ValidateRoot(directory, allowMissing: false);
                if (!Directory.EnumerateFileSystemEntries(directory).Select(Path.GetFileName)
                    .SequenceEqual(["module.db"]))
                    throw new StorageException(StorageCode.ReconciliationRequired);
            }
        }
        else
        {
            if (await RegistryMigrationRunner.ScalarAsync(platform, """
                SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_class c
                  JOIN pg_catalog.pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='public')
                  OR EXISTS (SELECT 1 FROM pg_catalog.pg_proc p
                    JOIN pg_catalog.pg_namespace n ON n.oid=p.pronamespace WHERE n.nspname='public')
                  OR EXISTS (SELECT 1 FROM pg_catalog.pg_type t
                    JOIN pg_catalog.pg_namespace n ON n.oid=t.typnamespace WHERE n.nspname='public')
                """, budget) is true)
                throw new StorageException(StorageCode.ReconciliationRequired);
            var actual = new HashSet<string>(StringComparer.Ordinal);
            await using var command = RegistryMigrationRunner.Command(platform, """
                SELECT nspname FROM pg_catalog.pg_namespace
                WHERE nspname !~ '^pg_' AND nspname NOT IN ('information_schema','public','kofte_system')
                """, budget);
            await using var reader = await command.ExecuteReaderAsync(budget.Token);
            while (await reader.ReadAsync(budget.Token)) actual.Add(reader.GetString(0));
            if (!keys.SetEquals(actual)) throw new StorageException(StorageCode.ReconciliationRequired);
        }
        foreach (var row in inventory)
        {
            if (row.Relational && (row.State != "ready" || row.MigrationState is not ("none" or "current") ||
                row.MigrationState == "none" && (row.History.Count != 0 || row.MigrationCheckpoint is not null ||
                    row.MigrationTarget is not null) ||
                row.MigrationState == "current" && (row.History.Count == 0 ||
                    row.MigrationCheckpoint != row.History[^1].Id || row.MigrationTarget != row.MigrationCheckpoint)))
                throw new StorageException(StorageCode.ReconciliationRequired);
        }
        return new(metadata.Id, metadata.Provider, metadata.Version, metadata.Revision, inventory);
    }

    private static async Task ReadHistoryAsync(DbConnection connection, string prefix,
        List<RecoveryMigration> history, OperationBudget budget)
    {
        var query = connection is SqliteConnection
            ? "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory'"
            : $"SELECT CASE WHEN pg_catalog.to_regclass('{prefix.Replace("\"", "", StringComparison.Ordinal)}\"__EFMigrationsHistory\"') IS NULL THEN 0 ELSE 1 END";
        if (Convert.ToInt64(await RegistryMigrationRunner.ScalarAsync(connection, query, budget),
            System.Globalization.CultureInfo.InvariantCulture) != 0)
        {
            await using var command = RegistryMigrationRunner.Command(connection,
                $"SELECT \"MigrationId\",\"ProductVersion\" FROM {prefix}\"__EFMigrationsHistory\" ORDER BY \"MigrationId\"", budget);
            await using var reader = await command.ExecuteReaderAsync(budget.Token);
            while (await reader.ReadAsync(budget.Token)) history.Add(new(reader.GetString(0), reader.GetString(1)));
        }
        if (connection is SqliteConnection &&
            await RegistryMigrationRunner.ScalarAsync(connection,
                "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsLock'", budget) is 1L &&
            await RegistryMigrationRunner.ScalarAsync(connection,
                "SELECT count(*) FROM \"__EFMigrationsLock\"", budget) is not 0L)
            throw new StorageException(StorageCode.ReconciliationRequired);
    }
}
