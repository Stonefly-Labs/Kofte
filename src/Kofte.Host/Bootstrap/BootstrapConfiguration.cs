using System.Net;
using System.Text.RegularExpressions;
using Npgsql;

namespace Kofte.Host.Bootstrap;

public sealed record BootstrapConfiguration(
    string Profile, Uri HostUrl, Uri WebUrl, Uri BackendOrigin,
    string DataRoot, string? PostgresConnection, bool UnsafeExposure, Uri? ExportEndpoint)
{
    private static readonly HashSet<string> Keys =
    [
        "KOFTE_PROFILE", "KOFTE_HOST_URL", "KOFTE_WEB_URL", "KOFTE_BACKEND_ORIGIN",
        "KOFTE_DATA_ROOT", "KOFTE_POSTGRES_CONNECTION", "KOFTE_ALLOW_UNSAFE_EXPOSURE",
        "KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE", "KOFTE_MODULES_FILE", "KOFTE_AUTH_CONFIG_FILE"
    ];

    public string? AuthenticationConfigurationFile { get; init; }

    public static BootstrapConfiguration FromEnvironment() => Parse(
        Environment.GetEnvironmentVariables().Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(item => (string)item.Key, item => item.Value?.ToString()),
        Directory.GetCurrentDirectory());

    public static BootstrapConfiguration Parse(IReadOnlyDictionary<string, string?> env, string workingDirectory)
    {
        string Value(string key, string fallback) => env.GetValueOrDefault(key) ?? fallback;
        bool Flag(string key)
        {
            var value = Value(key, "false");
            return value switch
            {
                "true" => true,
                "false" => false,
                _ => throw new BootstrapException(BootstrapError.ConfigurationInvalid)
            };
        }

        if (env.Keys.Any(key => key.StartsWith("KOFTE_", StringComparison.Ordinal) && !Keys.Contains(key)))
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        var profile = Value("KOFTE_PROFILE", "local-sqlite");
        var unsafeExposure = Flag("KOFTE_ALLOW_UNSAFE_EXPOSURE");
        var disposablePostgres = Flag("KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE");
        var host = Address(Value("KOFTE_HOST_URL", "http://127.0.0.1:5080"), unsafeExposure);
        var web = Address(Value("KOFTE_WEB_URL", "http://127.0.0.1:3000"), unsafeExposure);
        var backend = Address(Value("KOFTE_BACKEND_ORIGIN",
            Loopback(host.Host) ? host.GetLeftPart(UriPartial.Authority) : $"http://127.0.0.1:{host.Port}"), false);
        var hostName = host.Host.Trim('[', ']');
        var backendName = backend.Host.Trim('[', ']');
        if (backend.Port != host.Port ||
            (Loopback(hostName) && backendName != hostName &&
                !(hostName == "localhost" && backendName is "127.0.0.1" or "::1")) ||
            (hostName == "0.0.0.0" &&
                (!IPAddress.TryParse(backendName, out var target) || target.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)))
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        if (host.Port == web.Port)
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        var root = Value("KOFTE_DATA_ROOT", Path.Combine(workingDirectory, "data"));
        if (!Path.IsPathFullyQualified(root) || root.Split(Path.DirectorySeparatorChar).Contains(".."))
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        var connection = env.GetValueOrDefault("KOFTE_POSTGRES_CONNECTION");
        if (profile == "test-postgres")
        {
            if (!disposablePostgres || string.IsNullOrWhiteSpace(connection))
                throw new BootstrapException(BootstrapError.ConfigurationInvalid);
            try
            {
                var parsed = new NpgsqlConnectionStringBuilder(connection);
                if (!Loopback(parsed.Host ?? "") || string.IsNullOrEmpty(parsed.Database) ||
                    string.IsNullOrEmpty(parsed.Username))
                    throw new BootstrapException(BootstrapError.ConfigurationInvalid);
            }
            catch (ArgumentException) { throw new BootstrapException(BootstrapError.ConfigurationInvalid); }
        }
        else if (profile != "local-sqlite" || connection is not null)
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);

        Uri? export = null;
        if (env.TryGetValue("OTEL_EXPORTER_OTLP_ENDPOINT", out var endpoint))
        {
            if (!Uri.TryCreate(endpoint, UriKind.Absolute, out export) ||
                export.Scheme is not ("http" or "https") || export.UserInfo.Length != 0)
                throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        }
        return new(profile, host, web, backend, Path.GetFullPath(root), connection, unsafeExposure, export)
        {
            AuthenticationConfigurationFile = env.GetValueOrDefault("KOFTE_AUTH_CONFIG_FILE")
        };
    }

    private static Uri Address(string value, bool allowUnsafe)
    {
        if (!Regex.IsMatch(value, @"^http://[^/?#@\s]+/?\z", RegexOptions.CultureInvariant) ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.UserInfo.Length != 0 || uri.Port <= 0 ||
            (!allowUnsafe && !Loopback(uri.Host)))
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        return uri;
    }

    public static bool Loopback(string host) => host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
        (IPAddress.TryParse(host.Trim('[', ']'), out var ip) && IPAddress.IsLoopback(ip));
}
