using Kofte.Host.Bootstrap;
using Kofte.Infrastructure.Storage;

namespace Kofte.Host.Features.StorageMaintenance;

public sealed class StorageMaintenanceConfiguration
{
    public BootstrapConfiguration Runtime { get; }
    private readonly string? provisioningConnection;

    private StorageMaintenanceConfiguration(BootstrapConfiguration runtime, string? provisioning)
        => (Runtime, provisioningConnection) = (runtime, provisioning);

    public static StorageMaintenanceConfiguration FromEnvironment() => Parse(
        Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(item => (string)item.Key, item => item.Value?.ToString()),
        Directory.GetCurrentDirectory());

    public static StorageMaintenanceConfiguration Parse(IReadOnlyDictionary<string, string?> environment,
        string workingDirectory)
    {
        const string key = "KOFTE_POSTGRES_PROVISIONING_CONNECTION";
        var provisioning = environment.GetValueOrDefault(key);
        var runtime = BootstrapConfiguration.Parse(
            environment.Where(item => item.Key != key).ToDictionary(item => item.Key, item => item.Value),
            workingDirectory);
        if (provisioning is not null && (runtime.Profile != "test-postgres" ||
            string.IsNullOrWhiteSpace(provisioning)))
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        var result = new StorageMaintenanceConfiguration(runtime, provisioning);
        if (runtime.Profile == "test-postgres")
        {
            try { _ = result.CreateInstallation(); }
            catch (Exception error) when (error is ArgumentException or StorageException)
            { throw new BootstrapException(BootstrapError.ConfigurationInvalid); }
        }
        return result;
    }

    public StorageInstallation CreateInstallation() => Runtime.Profile == "local-sqlite"
        ? StorageInstallation.Sqlite(Runtime.DataRoot)
        : StorageInstallation.PostgreSql(Runtime.PostgresConnection!, provisioningConnection);

    internal void ConfigureChild(System.Diagnostics.ProcessStartInfo start)
    {
        foreach (var key in start.Environment.Keys.Where(key =>
            key.StartsWith("KOFTE_", StringComparison.Ordinal) ||
            key.StartsWith("OTEL_", StringComparison.Ordinal)).ToArray())
            start.Environment.Remove(key);
        start.Environment["KOFTE_PROFILE"] = Runtime.Profile;
        start.Environment["KOFTE_DATA_ROOT"] = Runtime.DataRoot;
        if (Runtime.Profile == "test-postgres")
        {
            start.Environment["KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE"] = "true";
            start.Environment["KOFTE_POSTGRES_CONNECTION"] = Runtime.PostgresConnection;
            if (provisioningConnection is not null)
                start.Environment["KOFTE_POSTGRES_PROVISIONING_CONNECTION"] = provisioningConnection;
        }
    }
}
