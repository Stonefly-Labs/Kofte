using System.Security.Cryptography;
using System.Text.Json;
using Kofte.Host.Bootstrap;
using Npgsql;

namespace Kofte.TestSupport;

public sealed partial class PostgresResources : IAsyncDisposable
{
    private static readonly TimeSpan AdministrationBudget = TimeSpan.FromSeconds(30);
    private readonly OwnedTestRoot root = new();
    private readonly string database;
    private readonly string role;
    private readonly string provisioner;
    private readonly string password = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
    private readonly string provisioningPassword = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
    private readonly string ownership;
    private string adminConnection = "";
    private PostgresService? service;
    private uint databaseOid, roleOid, provisionerOid;
    private bool unconfirmedCreation;
    private bool disposed;
    public string RunId { get; } = Guid.NewGuid().ToString("N");
    public string ConnectionString { get; private set; } = "";
    public string ProvisioningConnectionString { get; private set; } = "";

    private PostgresResources()
    {
        database = "kofte_test_" + RunId;
        role = "kofte_app_" + RunId;
        provisioner = "kofte_provisioner_" + RunId;
        ownership = "kofte-bootstrap:" + RunId;
    }

    public static Task<PostgresResources> CreateFromEnvironmentAsync(CancellationToken token) =>
        CreateAsync(Environment.GetEnvironmentVariable("KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE") == "true",
            Environment.GetEnvironmentVariable("KOFTE_POSTGRES_CONNECTION"), token: token);

    public static async Task<PostgresResources> CreateAsync(bool disposable,
        string? externalService = null, string? requestedDatabase = null, CancellationToken token = default)
    {
        if (OperatingSystem.IsWindows() || !disposable || requestedDatabase is not null)
            throw new InvalidOperationException("postgres_disposable_new_target_required");
        NpgsqlConnectionStringBuilder? supplied = null;
        if (externalService is not null)
        {
            try { supplied = new NpgsqlConnectionStringBuilder(externalService); }
            catch (ArgumentException) { throw new InvalidOperationException("postgres_configuration_invalid"); }
            if (!BootstrapConfiguration.Loopback(supplied.Host ?? "") ||
                string.IsNullOrWhiteSpace(supplied.Database) || string.IsNullOrWhiteSpace(supplied.Username))
                throw new InvalidOperationException("postgres_configuration_invalid");
        }
        var fixture = new PostgresResources();
        Directory.CreateDirectory(fixture.root.Path, UnixStorePaths.DirectoryMode);
        UnixStorePaths.ValidateRoot(fixture.root.Path, allowMissing: false);
        try
        {
            if (supplied is null)
            {
                fixture.service = new PostgresService(fixture.RunId, fixture.password);
                await fixture.service.StartAsync(token);
                supplied = new NpgsqlConnectionStringBuilder
                {
                    Host = "127.0.0.1",
                    Port = fixture.service.Port,
                    Database = "postgres",
                    Username = "kofte_admin",
                    Password = fixture.password,
                    SslMode = SslMode.Disable
                };
            }
            supplied.Pooling = false;
            supplied.Timeout = (int)AdministrationBudget.TotalSeconds;
            supplied.CommandTimeout = (int)AdministrationBudget.TotalSeconds;
            supplied.IncludeErrorDetail = false;
            fixture.adminConnection = supplied.ConnectionString;
            await fixture.ProvisionAsync(token);
            return fixture;
        }
        catch
        {
            await fixture.DisposeAsync();
            throw;
        }
    }

    private async Task<object?> AdminAsync(string sql, CancellationToken token, bool target = false)
    {
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(AdministrationBudget);
        budget.Token.ThrowIfCancellationRequested();
        var builder = new NpgsqlConnectionStringBuilder(adminConnection);
        if (target) builder.Database = database;
        await using var connection = new NpgsqlConnection(builder.ConnectionString);
        await connection.OpenAsync(budget.Token);
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync(budget.Token);
    }

    private async Task RecordAsync(CancellationToken token)
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("postgres_platform_unsupported");
        UnixStorePaths.ValidateRoot(root.Path, allowMissing: false);
        var path = Path.Combine(root.Path, "ownership.json");
        UnixStorePaths.ValidateFile(path, allowMissing: true);
        await using var file = new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None,
            UnixCreateMode = UnixStorePaths.FileMode
        });
        await JsonSerializer.SerializeAsync(file, new
        {
            RunId,
            database,
            role,
            provisioner,
            databaseOid,
            roleOid,
            provisionerOid,
            unconfirmedCreation,
            resources = service?.Resources
        }, cancellationToken: token);
    }

    private async Task ProvisionAsync(CancellationToken token)
    {
        if (await AdminAsync("SELECT pg_catalog.current_setting('server_version_num')::integer / 10000", token) is not 17)
            throw new InvalidOperationException("postgres_server_version_invalid");
        await RecordAsync(token);
        if (await AdminAsync($"SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_database WHERE datname='{database}') OR EXISTS (SELECT 1 FROM pg_catalog.pg_roles WHERE rolname IN ('{role}','{provisioner}'))", token) is not false)
            throw new InvalidOperationException("postgres_target_preexists");
        unconfirmedCreation = true;
        await RecordAsync(token);
        await AdminAsync($"""
            BEGIN;
            CREATE ROLE "{role}" LOGIN PASSWORD '{password}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
            COMMENT ON ROLE "{role}" IS '{ownership}';
            COMMIT;
            """, token);
        roleOid = (uint)(await AdminAsync($"SELECT oid FROM pg_catalog.pg_roles WHERE rolname='{role}'", token)
            ?? throw new InvalidOperationException("postgres_role_record_missing"));
        unconfirmedCreation = false;
        await RecordAsync(token);
        unconfirmedCreation = true;
        await RecordAsync(token);
        await AdminAsync($"""
            BEGIN;
            CREATE ROLE "{provisioner}" LOGIN PASSWORD '{provisioningPassword}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS;
            COMMENT ON ROLE "{provisioner}" IS '{ownership}';
            GRANT "{role}" TO "{provisioner}" WITH INHERIT TRUE, SET TRUE;
            COMMIT;
            """, token);
        provisionerOid = (uint)(await AdminAsync($"SELECT oid FROM pg_catalog.pg_roles WHERE rolname='{provisioner}'", token)
            ?? throw new InvalidOperationException("postgres_role_record_missing"));
        unconfirmedCreation = false;
        await RecordAsync(token);
        unconfirmedCreation = true;
        await RecordAsync(token);
        await AdminAsync($"CREATE DATABASE \"{database}\"", token);
        databaseOid = (uint)(await AdminAsync($"SELECT oid FROM pg_catalog.pg_database WHERE datname='{database}'", token)
            ?? throw new InvalidOperationException("postgres_database_record_missing"));
        unconfirmedCreation = false;
        await RecordAsync(token);
        await SetDatabaseOwnershipAsync(valid: true, token);
        await AdminAsync($"""
            REVOKE ALL ON DATABASE "{database}" FROM PUBLIC;
            GRANT CONNECT ON DATABASE "{database}" TO "{role}";
            GRANT CONNECT, CREATE ON DATABASE "{database}" TO "{provisioner}";
            """, token);
        await AdminAsync($"""
            REVOKE ALL ON SCHEMA public FROM PUBLIC;
            CREATE SCHEMA kofte_system AUTHORIZATION "{role}";
            COMMENT ON SCHEMA kofte_system IS '{ownership}';
            """, token, target: true);
        var app = new NpgsqlConnectionStringBuilder(adminConnection)
        {
            Database = database,
            Username = role,
            Password = password,
            Timeout = 3,
            CommandTimeout = 3,
            ApplicationName = "kofte_bootstrap_" + RunId,
            SearchPath = "kofte_system"
        };
        ConnectionString = app.ConnectionString;
        app.Username = provisioner;
        app.Password = provisioningPassword;
        app.ApplicationName = "kofte_provisioning_" + RunId;
        ProvisioningConnectionString = app.ConnectionString;
        await ValidateOwnershipAsync(token);
        await new PostgresPlatformStore(ConnectionString).ProbeAsync(token);
    }

    public async Task<bool> DatabaseExistsAsync(CancellationToken token) =>
        await AdminAsync($"SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_database WHERE datname='{database}' AND oid={databaseOid})", token) is true;

    public async Task SetDatabaseOwnershipAsync(bool valid, CancellationToken token) =>
        _ = await AdminAsync($"COMMENT ON DATABASE \"{database}\" IS '{(valid ? ownership : "not-owned")}'", token);

    public async Task SetProvisionerOwnershipAsync(bool valid, CancellationToken token) =>
        _ = await AdminAsync($"COMMENT ON ROLE \"{provisioner}\" IS '{(valid ? ownership : "not-owned")}'", token);

    public async Task SetProvisioningPrivilegeAsync(bool allowed, CancellationToken token)
    {
        await ValidateOwnershipAsync(token);
        _ = await AdminAsync(allowed
            ? $"GRANT CREATE ON DATABASE \"{database}\" TO \"{provisioner}\""
            : $"REVOKE CREATE ON DATABASE \"{database}\" FROM \"{provisioner}\"", token);
    }

    public async Task SetSchemaCreationAsync(bool allowed, CancellationToken token) =>
        _ = await AdminAsync(allowed
            ? $"GRANT CREATE ON SCHEMA kofte_system TO \"{role}\""
            : $"REVOKE CREATE ON SCHEMA kofte_system FROM \"{role}\"", token, target: true);

    public async Task SetAvailableAsync(bool available, CancellationToken token)
    {
        if (service is null) throw new InvalidOperationException("postgres_owned_image_required");
        if (!available) await ValidateOwnershipAsync(token);
        await service.SetPausedAsync(!available, token);
        if (available) await ValidateOwnershipAsync(token);
    }

    public async Task ValidateOwnershipAsync(CancellationToken token)
    {
        if (databaseOid == 0 || roleOid == 0 || provisionerOid == 0 || await AdminAsync($"""
            SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_database d
              WHERE d.datname='{database}' AND d.oid={databaseOid}
              AND d.datdba=(SELECT oid FROM pg_catalog.pg_roles WHERE rolname=current_user)
              AND pg_catalog.shobj_description(d.oid,'pg_database')='{ownership}')
            AND EXISTS (SELECT 1 FROM pg_catalog.pg_roles r
              WHERE r.rolname='{role}' AND r.oid={roleOid}
              AND pg_catalog.shobj_description(r.oid,'pg_authid')='{ownership}')
            AND EXISTS (SELECT 1 FROM pg_catalog.pg_roles r
              WHERE r.rolname='{provisioner}' AND r.oid={provisionerOid}
              AND pg_catalog.shobj_description(r.oid,'pg_authid')='{ownership}')
            """, token) is not true)
            throw new InvalidOperationException("postgres_database_ownership_refused");
        if (service is not null) await service.ValidateOwnershipAsync(token);
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        if (unconfirmedCreation && service is null)
            throw new InvalidOperationException("postgres_unconfirmed_creation_preserved");
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        if (service is not null && service.Resources.Any(resource => resource.Kind == "container"))
            await service.SetPausedAsync(false, cleanup.Token);
        if (databaseOid != 0)
        {
            await ValidateOwnershipAsync(cleanup.Token);
            await AdminAsync($"DROP DATABASE \"{database}\" WITH (FORCE)", cleanup.Token);
            databaseOid = 0;
        }
        if (roleOid != 0)
        {
            if (await AdminAsync($"SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_roles r WHERE r.rolname='{role}' AND r.oid={roleOid} AND pg_catalog.shobj_description(r.oid,'pg_authid')='{ownership}')", cleanup.Token) is not true)
                throw new InvalidOperationException("postgres_role_ownership_refused");
            await AdminAsync($"DROP ROLE \"{role}\"", cleanup.Token);
            roleOid = 0;
        }
        if (provisionerOid != 0)
        {
            if (await AdminAsync($"SELECT EXISTS (SELECT 1 FROM pg_catalog.pg_roles r WHERE r.rolname='{provisioner}' AND r.oid={provisionerOid} AND pg_catalog.shobj_description(r.oid,'pg_authid')='{ownership}')", cleanup.Token) is not true)
                throw new InvalidOperationException("postgres_role_ownership_refused");
            await AdminAsync($"DROP ROLE \"{provisioner}\"", cleanup.Token);
            provisionerOid = 0;
        }
        if (service is not null) await service.CleanupAsync(cleanup.Token);
        root.Dispose();
        disposed = true;
    }

    public async Task<PostgresResources> CreateRecoveryTargetAsync(CancellationToken token)
    {
        await ValidateOwnershipAsync(token);
        return await CreateAsync(true, service is null ? adminConnection : null, token: token);
    }
}
