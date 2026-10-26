using System.Collections.ObjectModel;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using Kofte.Host.Bootstrap;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Identity;
using Paths = Kofte.Infrastructure.Storage.UnixStorePaths;

namespace Kofte.Host.Features.Identity;

public enum IdentityOperation
{
    Serve, Prepare, BootstrapAuthorize, BootstrapDisablePending, ReconcileConfiguration,
    RevokeSessions, Maintain, ReconcileRestore
}

public sealed class IdentityConfigurationException : Exception
{
    internal IdentityConfigurationException(string category) : base("identity_configuration_" + category)
        => Code = "identity_configuration_" + category;
    public string Code { get; }
}

public sealed class IdentityProviderConfiguration
{
    internal IdentityProviderConfiguration(string issuer, Uri metadata, Uri authorization, Uri token, Uri keys,
        string clientId, string clientSecretFile, string? clientSecret, IReadOnlyList<Uri> endpoints,
        IReadOnlyList<IPNetwork> networks)
    {
        Issuer = issuer;
        MetadataAddress = metadata;
        AuthorizationEndpoint = authorization;
        TokenEndpoint = token;
        JwksUri = keys;
        ClientId = clientId;
        ClientSecretFile = clientSecretFile;
        ClientSecret = clientSecret;
        AllowedEndpoints = endpoints;
        AllowedNetworks = networks;
    }

    public string Issuer { get; }
    public Uri MetadataAddress { get; }
    public Uri AuthorizationEndpoint { get; }
    public Uri TokenEndpoint { get; }
    public Uri JwksUri { get; }
    public string ClientId { get; }
    internal string ClientSecretFile { get; }
    internal string? ClientSecret { get; }
    public IReadOnlyList<Uri> AllowedEndpoints { get; }
    public IReadOnlyList<IPNetwork> AllowedNetworks { get; }
    public override string ToString() => nameof(IdentityProviderConfiguration);
}

public sealed class IdentityConfiguration : IDisposable
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    private IdentityConfiguration(IdentityMode mode, Uri origin, string keys, IdentityProviderConfiguration? provider,
        IReadOnlyDictionary<string, string?> identities, X509Certificate2? certificate, string digest)
    {
        Mode = mode;
        PublicOrigin = origin;
        KeyRingDirectory = keys;
        Provider = provider;
        DevelopmentIdentities = identities;
        KeyEncryptionCertificate = certificate;
        TrustDigest = digest;
    }

    public IdentityMode Mode { get; }
    public Uri PublicOrigin { get; }
    public string KeyRingDirectory { get; }
    public IdentityProviderConfiguration? Provider { get; }
    public IReadOnlyDictionary<string, string?> DevelopmentIdentities { get; }
    public string TrustDigest { get; }
    internal X509Certificate2? KeyEncryptionCertificate { get; }
    public override string ToString() => nameof(IdentityConfiguration);
    public void Dispose() => KeyEncryptionCertificate?.Dispose();

    public static IdentityConfiguration Load(BootstrapConfiguration bootstrap, string environment,
        IdentityOperation operation = IdentityOperation.Serve)
    {
        try
        {
            var path = bootstrap.AuthenticationConfigurationFile ?? throw Invalid();
            return Parse(ReadBounded(path, 32_768), bootstrap, environment, operation);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or StorePathException or
            ArgumentException or NotSupportedException)
        {
            throw Invalid("file_invalid");
        }
    }

    public static IdentityConfiguration Parse(ReadOnlyMemory<byte> bytes, BootstrapConfiguration bootstrap,
        string environment, IdentityOperation operation = IdentityOperation.Serve)
    {
        try
        {
            if (bytes.Length is < 2 or > 32_768 || !Enum.IsDefined(operation)) throw Invalid();
            _ = StrictUtf8.GetCharCount(bytes.Span);
            using var document = JsonDocument.Parse(bytes,
                new() { AllowDuplicateProperties = false, MaxDepth = 8 });
            var root = document.RootElement;
            Object(root, "version", "mode", "publicOrigin", "keyRingDirectory",
                "keyEncryptionCertificateFile", "keyEncryptionPrivateKeyFile", "provider", "development");
            if (Text(root, "version") != "1") throw Invalid();
            var mode = Text(root, "mode") switch
            {
                "oidc" => IdentityMode.Oidc,
                "development" => IdentityMode.Development,
                _ => throw Invalid()
            };
            if (!BootstrapConfiguration.Loopback(bootstrap.HostUrl.Host) ||
                !BootstrapConfiguration.Loopback(bootstrap.WebUrl.Host) ||
                !BootstrapConfiguration.Loopback(bootstrap.BackendOrigin.Host)) throw Invalid();
            var originText = Text(root, "publicOrigin");
            var origin = Origin(originText, mode);
            if (mode == IdentityMode.Development && (environment != "Development" ||
                originText != bootstrap.WebUrl.GetLeftPart(UriPartial.Authority))) throw Invalid();
            var keys = Text(root, "keyRingDirectory");
            DirectoryReference(keys);
            if (Within(keys, bootstrap.DataRoot) || Within(bootstrap.DataRoot, keys)) throw Invalid();

            var readSecrets = operation is IdentityOperation.Serve or IdentityOperation.Prepare or
                IdentityOperation.ReconcileConfiguration or IdentityOperation.ReconcileRestore;
            var readCertificate = readSecrets || operation == IdentityOperation.BootstrapAuthorize;
            var certificateFile = OptionalText(root, "keyEncryptionCertificateFile");
            var keyFile = OptionalText(root, "keyEncryptionPrivateKeyFile");
            if ((certificateFile is null) != (keyFile is null) ||
                mode == IdentityMode.Oidc && certificateFile is null) throw Invalid();
            if (certificateFile is not null && keyFile is not null)
            {
                FileReference(certificateFile, allowMissing: !readCertificate);
                FileReference(keyFile, allowMissing: !readCertificate);
                if (Within(certificateFile, bootstrap.DataRoot) || Within(keyFile, bootstrap.DataRoot)) throw Invalid();
            }
            IdentityProviderConfiguration? provider = null;
            var identities = new Dictionary<string, string?>(StringComparer.Ordinal);
            if (mode == IdentityMode.Oidc)
            {
                if (root.TryGetProperty("development", out _)) throw Invalid();
                provider = ProviderConfiguration(root.GetProperty("provider"), bootstrap.DataRoot, readSecrets);
            }
            else
            {
                if (root.TryGetProperty("provider", out _)) throw Invalid();
                var development = root.GetProperty("development");
                Object(development, "enabled", "identities");
                if (development.GetProperty("enabled").ValueKind != JsonValueKind.True) throw Invalid();
                var entries = development.GetProperty("identities");
                if (entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() is < 1 or > 8) throw Invalid();
                foreach (var entry in entries.EnumerateArray())
                {
                    Object(entry, "key", "displayName");
                    var key = Text(entry, "key");
                    if (key.Length > 255 || key.Any(character => character is < ' ' or > '~')) throw Invalid();
                    var display = Display(entry);
                    if (!identities.TryAdd(key, display)) throw Invalid();
                }
            }
            var digest = Digest(mode, originText, provider, identities.Keys);
            X509Certificate2? certificate = null;
            if (readCertificate && certificateFile is not null && keyFile is not null)
                certificate = ReadCertificate(certificateFile, keyFile);
            return new(mode, origin, keys, provider, new ReadOnlyDictionary<string, string?>(identities),
                certificate, digest);
        }
        catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException or
            IOException or UnauthorizedAccessException or ArgumentException or StorePathException or
            DecoderFallbackException or EncoderFallbackException or NotSupportedException)
        {
            throw Invalid();
        }
    }

    private static IdentityProviderConfiguration ProviderConfiguration(JsonElement value, string dataRoot,
        bool readSecret)
    {
        Object(value, "issuer", "metadataAddress", "authorizationEndpoint", "tokenEndpoint", "jwksUri",
            "clientId", "clientSecretFile", "allowedEndpoints", "allowedNetworks");
        var issuer = Text(value, "issuer");
        if (StrictUtf8.GetByteCount(issuer) > 2048) throw Invalid();
        _ = Https(issuer, issuer: true);
        var metadata = Https(Text(value, "metadataAddress"));
        var authorization = Https(Text(value, "authorizationEndpoint"));
        var token = Https(Text(value, "tokenEndpoint"));
        var keys = Https(Text(value, "jwksUri"));
        var clientId = Text(value, "clientId");
        var secretPath = Text(value, "clientSecretFile");
        FileReference(secretPath, allowMissing: !readSecret);
        if (Within(secretPath, dataRoot)) throw Invalid();
        string? secret = null;
        if (readSecret)
        {
            secret = StrictUtf8.GetString(ReadBounded(secretPath, 4096));
            if (secret.Length == 0 || secret.Any(char.IsControl)) throw Invalid("secret_invalid");
        }
        var allowed = value.GetProperty("allowedEndpoints");
        if (allowed.ValueKind != JsonValueKind.Array || allowed.GetArrayLength() is < 1 or > 8) throw Invalid();
        var endpoints = allowed.EnumerateArray().Select(item => Https(Scalar(item))).ToArray();
        var unique = endpoints.Select(item => item.OriginalString).ToHashSet(StringComparer.Ordinal);
        if (unique.Count != endpoints.Length || new[] { metadata, authorization, token, keys }
            .Any(endpoint => !unique.Contains(endpoint.OriginalString))) throw Invalid();
        var networkValues = value.GetProperty("allowedNetworks");
        if (networkValues.ValueKind != JsonValueKind.Array || networkValues.GetArrayLength() == 0) throw Invalid();
        var networks = networkValues.EnumerateArray().Select(item =>
        {
            var text = Scalar(item);
            if (!IPNetwork.TryParse(text, out var network) ||
                network.ToString() != text) throw Invalid();
            return network;
        }).ToArray();
        if (networks.Distinct().Count() != networks.Length) throw Invalid();
        return new(issuer, metadata, authorization, token, keys, clientId, secretPath, secret,
            Array.AsReadOnly(endpoints), Array.AsReadOnly(networks));
    }

    private static string Digest(IdentityMode mode, string origin, IdentityProviderConfiguration? provider,
        IEnumerable<string> identities)
    {
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteNumber("version", 1);
            writer.WriteString("mode", mode == IdentityMode.Oidc ? "oidc" : "development");
            writer.WriteString("publicOrigin", origin);
            if (provider is not null)
            {
                writer.WriteString("issuer", provider.Issuer);
                writer.WriteString("clientId", provider.ClientId);
                writer.WriteString("metadataAddress", provider.MetadataAddress.OriginalString);
                writer.WriteString("authorizationEndpoint", provider.AuthorizationEndpoint.OriginalString);
                writer.WriteString("tokenEndpoint", provider.TokenEndpoint.OriginalString);
                writer.WriteString("jwksUri", provider.JwksUri.OriginalString);
                writer.WriteStartArray("endpoints");
                foreach (var endpoint in provider.AllowedEndpoints.Select(item => item.OriginalString)
                    .Order(StringComparer.Ordinal)) writer.WriteStringValue(endpoint);
                writer.WriteEndArray();
                writer.WriteStartArray("networks");
                foreach (var network in provider.AllowedNetworks.Select(item => item.ToString())
                    .Order(StringComparer.Ordinal)) writer.WriteStringValue(network);
                writer.WriteEndArray();
            }
            writer.WriteStartArray("developmentKeys");
            foreach (var identity in identities.Order(StringComparer.Ordinal)) writer.WriteStringValue(identity);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Convert.ToHexStringLower(SHA256.HashData(buffer.ToArray()));
    }

    private static Uri Origin(string text, IdentityMode mode)
    {
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || uri.UserInfo.Length != 0 ||
            text != uri.GetLeftPart(UriPartial.Authority) ||
            mode == IdentityMode.Oidc && uri.Scheme != "https" ||
            mode == IdentityMode.Development && (uri.Scheme != "http" || !BootstrapConfiguration.Loopback(uri.Host)))
            throw Invalid();
        return uri;
    }

    private static Uri Https(string text, bool issuer = false)
    {
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            uri.UserInfo.Length != 0 || uri.Fragment.Length != 0 || text.Any(char.IsWhiteSpace) ||
            issuer && uri.Query.Length != 0 || IPAddress.TryParse(uri.Host.Trim('[', ']'), out var address) &&
            ForbiddenAddress(address)) throw Invalid();
        return uri;
    }

    internal static bool ForbiddenAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) ||
            address.IsIPv6LinkLocal || address.IsIPv6Multicast) return true;
        var bytes = address.GetAddressBytes();
        return bytes.Length == 4 && (bytes[0] == 169 && bytes[1] == 254 || bytes[0] >= 224);
    }

    private static void Object(JsonElement value, params string[] fields)
    {
        if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject()
            .Any(item => !fields.Contains(item.Name, StringComparer.Ordinal))) throw Invalid();
    }

    private static string Scalar(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String) throw Invalid();
        var text = value.GetString();
        if (string.IsNullOrEmpty(text) || text.Any(char.IsControl)) throw Invalid();
        _ = StrictUtf8.GetByteCount(text);
        return text;
    }

    private static string Text(JsonElement value, string name) => Scalar(value.GetProperty(name));
    private static string? OptionalText(JsonElement value, string name) =>
        value.TryGetProperty(name, out var item) ? Scalar(item) : null;

    private static string? Display(JsonElement value)
    {
        if (!value.TryGetProperty("displayName", out var item) || item.ValueKind == JsonValueKind.Null) return null;
        if (item.ValueKind != JsonValueKind.String) throw Invalid();
        var text = item.GetString()!;
        _ = StrictUtf8.GetByteCount(text);
        if (text.EnumerateRunes().Count() > 128 || text.EnumerateRunes().Any(Rune.IsControl)) throw Invalid();
        return text;
    }

    private static bool Within(string path, string parent)
    {
        var relative = Path.GetRelativePath(parent, path);
        return relative == "." || !Path.IsPathFullyQualified(relative) && relative != ".." &&
            !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    private static void DirectoryReference(string path) => Paths.ValidateRoot(path, allowMissing: true);

    private static void FileReference(string path, bool allowMissing)
    {
        if (!Path.IsPathFullyQualified(path) || path != Path.GetFullPath(path)) throw Invalid();
        Paths.ValidateRoot(Path.GetDirectoryName(path)!, allowMissing);
        Paths.ValidateFile(path, allowMissing);
    }

    private static byte[] ReadBounded(string path, int maximum)
    {
        FileReference(path, allowMissing: false);
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length is < 1 || file.Length > maximum) throw Invalid("file_invalid");
        var bytes = new byte[maximum + 1];
        var length = 0;
        while (length < bytes.Length)
        {
            var count = file.Read(bytes, length, bytes.Length - length);
            if (count == 0) break;
            length += count;
        }
        if (length is < 1 || length > maximum) throw Invalid("file_invalid");
        return bytes[..length];
    }

    private static X509Certificate2 ReadCertificate(string certificatePath, string keyPath)
    {
        try
        {
            var certificate = X509Certificate2.CreateFromPem(
                StrictUtf8.GetString(ReadBounded(certificatePath, 65_536)),
                StrictUtf8.GetString(ReadBounded(keyPath, 65_536)));
            using var key = certificate.GetRSAPrivateKey();
            if (key is null || key.KeySize < 2048)
            {
                certificate.Dispose();
                throw Invalid("certificate_invalid");
            }
            return certificate;
        }
        catch (CryptographicException) { throw Invalid("certificate_invalid"); }
    }

    private static IdentityConfigurationException Invalid(string category = "invalid") => new(category);
}
