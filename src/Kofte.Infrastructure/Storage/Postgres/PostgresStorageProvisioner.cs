using Kofte.Infrastructure.Storage.Allocation;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Npgsql;

namespace Kofte.Infrastructure.Storage.Postgres;

internal sealed class PostgresStorageProvisioner(StorageInstallation installation)
{
    internal async Task ProvisionAsync(InstallationRecord metadata, RegistrationRecord record,
        OperationBudget budget, MutationEvidence mutation)
    {
        await using (var runtime = await installation.OpenPlatformAsync(budget))
        {
            await using var exists = RegistryMigrationRunner.Command(runtime,
                "SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname=@key)", budget, null,
                ("key", record.Key!));
            if (await exists.ExecuteScalarAsync(budget.Token) is true)
            {
                await VerifyAsync(metadata, record, budget);
                return;
            }
        }
        if (record.State != "reserved") throw new StorageException(StorageCode.Missing);
        if (installation.ProvisioningConnection is not { } provisioning)
            throw new StorageException(StorageCode.PrivilegeDenied);
        var identity = installation.Postgres!;
        await using var connection = new NpgsqlConnection(provisioning);
        await connection.OpenAsync(budget.Token);
        await using var verify = RegistryMigrationRunner.Command(connection, """
            SELECT current_database()=@database
              AND pg_catalog.has_database_privilege(current_user,current_database(),'CREATE')
              AND pg_catalog.pg_has_role(current_user,@role,'SET')
              AND EXISTS(SELECT 1 FROM pg_catalog.pg_database d WHERE datname=current_database()
                AND pg_catalog.shobj_description(d.oid,'pg_database')=@ownership)
              AND EXISTS(SELECT 1 FROM pg_catalog.pg_roles WHERE rolname=current_user
                AND NOT(rolsuper OR rolcreatedb OR rolcreaterole OR rolreplication OR rolbypassrls)
                AND pg_catalog.shobj_description(oid,'pg_authid')=@ownership)
            """, budget, null, ("database", identity.Database), ("role", identity.Role),
            ("ownership", identity.Ownership));
        if (await verify.ExecuteScalarAsync(budget.Token) is not true)
            throw new StorageException(StorageCode.PrivilegeDenied);
        await using var transaction = await connection.BeginTransactionAsync(budget.Token);
        var schema = StorageAllocator.Quote(record.Key!);
        var role = new NpgsqlCommandBuilder().QuoteIdentifier(identity.Role);
        await RegistryMigrationRunner.ExecuteAsync(connection, $"""
            CREATE SCHEMA {schema} AUTHORIZATION {role};
            SET LOCAL ROLE {role};
            REVOKE ALL ON SCHEMA {schema} FROM PUBLIC;
            """, budget, transaction);
        await StorageOwnerMarker.CreateAsync(installation, connection, transaction, metadata, record, budget);
        mutation.PossibleCommit();
        await transaction.CommitAsync(budget.Token);
        mutation.Committed();
        await VerifyAsync(metadata, record, budget);
    }

    internal async Task VerifyAsync(InstallationRecord metadata, RegistrationRecord record, OperationBudget budget)
    {
        await using var connection = await installation.OpenPlatformAsync(budget);
        await using var command = RegistryMigrationRunner.Command(connection, """
            SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_namespace n
              JOIN pg_catalog.pg_roles r ON r.oid=n.nspowner
              WHERE n.nspname=@key AND r.rolname=current_user
              AND NOT EXISTS (SELECT 1 FROM pg_catalog.aclexplode(
                COALESCE(n.nspacl,pg_catalog.acldefault('n',n.nspowner))) acl WHERE acl.grantee=0))
            """, budget, null, ("key", record.Key!));
        if (await command.ExecuteScalarAsync(budget.Token) is not true)
            throw new StorageException(StorageCode.OwnerMismatch);
        await using var marker = RegistryMigrationRunner.Command(connection, """
            SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_tables
              WHERE schemaname=@key AND tablename='__kofte_storage_owner')
            """, budget, null, ("key", record.Key!));
        if (await marker.ExecuteScalarAsync(budget.Token) is not true)
            throw new StorageException(StorageCode.OwnerMismatch);
        await StorageOwnerMarker.VerifyAsync(installation, connection, metadata, record, budget);
    }
}
