using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Npgsql;

namespace Kofte.TestSupport;

public sealed partial class PostgresResources
{
    public async Task<RecoveryManifest> BackupStorageSetAsync(string directory, CancellationToken token)
    {
        await ValidateOwnershipAsync(token);
        var tools = service ?? throw new InvalidOperationException("postgres_owned_image_required");
        var installation = StorageInstallation.PostgreSql(ConnectionString);
        var maintenance = new StorageMaintenance(installation);
        await using var scope = await maintenance.AcquireAsync(token);
        var inventory = await RecoverySetVerifier.InspectStoppedAsync(installation, scope, token);
        var identity = inventory.RegistryVersion == 2
            ? await IdentityRecoverySnapshot.CaptureStoppedAsync(installation, scope, token) : null;
        var previous = (int)(await AdminAsync(
            $"SELECT datconnlimit FROM pg_catalog.pg_database WHERE oid={databaseOid}", token)
            ?? throw new InvalidOperationException("postgres_database_record_missing"));
        SqliteRecoveryExercise.CreateFreshDirectory(directory);
        try
        {
            await AdminAsync($"ALTER DATABASE \"{database}\" CONNECTION LIMIT 0", token);
            if (await AdminAsync("""
                SELECT count(*) FROM pg_catalog.pg_stat_activity
                WHERE datid=(SELECT oid FROM pg_catalog.pg_database WHERE datname=current_database())
                  AND pid<>pg_catalog.pg_backend_pid()
                """, token, target: true) is not 1L)
                throw new InvalidOperationException("postgres_host_must_stop");
            await using (var dump = CreateBackupFile(Path.Combine(directory, "installation.dump")))
                await tools.RunToolAsync("pg_dump", database, role, dump, token, fullDatabase: true);
        }
        finally
        {
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await AdminAsync($"ALTER DATABASE \"{database}\" CONNECTION LIMIT {previous}", cleanup.Token);
        }
        var artifact = await RecoverySetVerifier.DescribeArtifactAsync(directory, "installation.dump", token);
        var manifest = new RecoveryManifest(identity is null ? RecoveryManifest.CurrentFormat : RecoveryManifest.IdentityFormat,
            true, Guid.NewGuid(),
            inventory.InstallationId, inventory.Provider, inventory.RegistryVersion, inventory.RegistryRevision,
            inventory.Allocations, [artifact], identity);
        var after = await RecoverySetVerifier.InspectStoppedAsync(installation, scope, token);
        if (!RecoverySetVerifier.SameInventory(manifest, after) ||
            identity is not null && await IdentityRecoverySnapshot.CaptureStoppedAsync(installation, scope, token) != identity)
            throw new InvalidOperationException("recovery_source_changed");
        await RecoverySetVerifier.WriteManifestAsync(directory, manifest, token);
        await RecoverySetVerifier.VerifyAsync(directory, installation, scope, token);
        return manifest;
    }

    private async Task<RecoveryManifest> RestoreStorageSetAsync(string directory, CancellationToken token)
    {
        await ValidateOwnershipAsync(token);
        var tools = service ?? throw new InvalidOperationException("postgres_owned_image_required");
        await using (await AcquireRecoveryAsync(token))
        {
            if (await AdminAsync("""
                SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_tables
                  WHERE schemaname !~ '^pg_' AND schemaname<>'information_schema')
                  OR EXISTS (SELECT 1 FROM pg_catalog.pg_namespace
                    WHERE nspname !~ '^pg_' AND nspname NOT IN ('public','information_schema','kofte_system'))
                """, token, target: true) is not false)
                throw new InvalidOperationException("postgres_restore_target_not_empty");
            await AdminAsync($"""
                ALTER SCHEMA public OWNER TO "{provisioner}";
                ALTER SCHEMA kofte_system OWNER TO "{provisioner}";
                """, token, target: true);
            await using var dump = File.OpenRead(Path.Combine(directory, "installation.dump"));
            await tools.RunToolAsync("pg_restore", database, provisioner, dump, token, fullDatabase: true);
            await AdminAsync($"""
                REASSIGN OWNED BY "{provisioner}" TO "{role}";
                ALTER SCHEMA public OWNER TO pg_database_owner;
                REVOKE ALL ON SCHEMA public FROM PUBLIC;
                COMMENT ON SCHEMA kofte_system IS '{ownership}';
                """, token, target: true);
        }
        await ValidateOwnershipAsync(token);
        var installation = StorageInstallation.PostgreSql(ConnectionString);
        var maintenance = new StorageMaintenance(installation);
        await using var scope = await maintenance.AcquireAsync(token);
        var manifest = await RecoverySetVerifier.VerifyAsync(directory, installation, scope, token);
        await using var runtime = new NpgsqlConnection(ConnectionString);
        await runtime.OpenAsync(token);
        await using var privileges = new NpgsqlCommand("""
            SELECT NOT pg_catalog.has_database_privilege(current_user,current_database(),'CREATE')
              AND NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname=current_user
                AND (rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls))
            """, runtime);
        if (await privileges.ExecuteScalarAsync(token) is not true)
            throw new InvalidOperationException("postgres_runtime_privileges_invalid");
        return manifest;
    }

    public Task<RecoveryManifest> RestoreStorageSetToFreshAsync(string directory,
        PostgresResources freshTarget, CancellationToken token) =>
        RestoreStorageSetToFreshCoreAsync(directory, freshTarget, null, token);

    public Task<RecoveryManifest> RestoreStorageSetToFreshQuarantinedAsync(string directory,
        PostgresResources freshTarget, string keyDirectory, CancellationToken token) =>
        RestoreStorageSetToFreshCoreAsync(directory, freshTarget, keyDirectory, token);

    private async Task<RecoveryManifest> RestoreStorageSetToFreshCoreAsync(string directory,
        PostgresResources freshTarget, string? keyDirectory, CancellationToken token)
    {
        if (ReferenceEquals(this, freshTarget) || ConnectionString == freshTarget.ConnectionString)
            throw new InvalidOperationException("postgres_fresh_target_required");
        await ValidateOwnershipAsync(token);
        await freshTarget.ValidateOwnershipAsync(token);
        var installation = StorageInstallation.PostgreSql(ConnectionString);
        var maintenance = new StorageMaintenance(installation);
        await using var scope = await maintenance.AcquireAsync(token);
        var manifest = await RecoverySetVerifier.VerifyAsync(directory, installation, scope, token);
        if (manifest.Identity is not null && keyDirectory is null)
            throw new InvalidOperationException("identity_restore_reconciliation_required");
        var quarantine = keyDirectory is null ? null : await IdentityRestoreQuarantine.CreateAsync(
            directory, keyDirectory, manifest, token: token);
        var restored = await freshTarget.RestoreStorageSetAsync(directory, token);
        if (quarantine is not null) await quarantine.MarkRestoredAsync(restored, token);
        return restored;
    }
}
