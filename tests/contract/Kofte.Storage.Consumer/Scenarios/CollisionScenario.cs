using System.Security.Cryptography;
using System.Text;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Npgsql;
using Kofte.TestSupport;

namespace Kofte.Storage.Consumer.Scenarios;

internal static class CollisionScenario
{
    internal static async Task<int> RunAsync(StorageInstallation installation, IStorageMaintenanceScope scope,
        CancellationToken token)
    {
        var owner = RegistrationScenarios.Module("org.example.collision");
        var key = "m_org_example_collision_" +
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(owner.Id.Value)))[..32];
        var sentinel = "outside-owner-preserved";
        string? path = null;
        if (installation.Provider == "sqlite")
        {
            if (OperatingSystem.IsWindows()) throw new InvalidOperationException("storage_consumer_unsupported_target");
            var root = Environment.GetEnvironmentVariable("KOFTE_DATA_ROOT") ??
                throw new InvalidOperationException("storage_consumer_target_required");
            Directory.CreateDirectory(Path.Combine(root, "modules"),
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            var directory = Path.Combine(root, "modules", key);
            Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            path = Path.Combine(directory, "module.db");
            await using var file = new FileStream(path, new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite
            });
            await file.WriteAsync(Encoding.UTF8.GetBytes(sentinel), token);
        }
        else
        {
            var runtime = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("KOFTE_POSTGRES_CONNECTION"));
            await using var provisioner = new NpgsqlConnection(
                Environment.GetEnvironmentVariable("KOFTE_POSTGRES_PROVISIONING_CONNECTION"));
            await provisioner.OpenAsync(token);
            var role = new NpgsqlCommandBuilder().QuoteIdentifier(runtime.Username!);
            await using var command = new NpgsqlCommand($"""
                CREATE SCHEMA "{key}" AUTHORIZATION {role};
                SET ROLE {role};
                CREATE TABLE "{key}".sentinel (value TEXT);
                INSERT INTO "{key}".sentinel VALUES (@sentinel)
                """, provisioner) { CommandTimeout = 3 };
            command.Parameters.AddWithValue("sentinel", sentinel);
            await command.ExecuteNonQueryAsync(token);
        }
        var registry = new StorageRegistry(installation);
        var result = StorageResultChecks.Check(await registry.RegisterAsync(owner, scope, token));
        RegistrationScenarios.Require(result.Outcome == StorageOutcome.ReconciliationRequired);
        RegistrationScenarios.Require(await registry.ResolveAsync(owner.Id, token) is not StorageResult.Ready);
        if (path is not null)
        {
            RegistrationScenarios.Require(await File.ReadAllTextAsync(path, token) == sentinel);
            RegistrationScenarios.Require(Directory.EnumerateDirectories(Path.GetDirectoryName(Path.GetDirectoryName(path))!,
                key + "*").Count() == 1);
        }
        else
        {
            await using var runtime = new NpgsqlConnection(Environment.GetEnvironmentVariable("KOFTE_POSTGRES_CONNECTION"));
            await runtime.OpenAsync(token);
            await using var read = new NpgsqlCommand($"""SELECT value FROM "{key}".sentinel""", runtime)
            { CommandTimeout = 3 };
            RegistrationScenarios.Require(await read.ExecuteScalarAsync(token) is string value && value == sentinel);
            await using var marker = new NpgsqlCommand("""
                SELECT count(*) FROM pg_catalog.pg_tables WHERE schemaname=@key AND tablename='__kofte_storage_owner'
                """, runtime) { CommandTimeout = 3 };
            marker.Parameters.AddWithValue("key", key);
            RegistrationScenarios.Require(await marker.ExecuteScalarAsync(token) is 0L);
        }
        return 4;
    }
}
