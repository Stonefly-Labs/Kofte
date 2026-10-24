using System.Text;
using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Host.Features.ModuleComposition;

public sealed class ModuleSelectionException : Exception
{
    public string Code => "composition_configuration_invalid";
    public ModuleSelectionException() : base("composition_configuration_invalid") { }
}

public sealed class ModuleSelectionDocument
{
    private readonly byte[] snapshot;
    public int ContractVersion => 1;
    public IReadOnlyList<ModuleSelectionEntry> Modules { get; }

    internal ModuleSelectionDocument(IEnumerable<ModuleSelectionEntry> modules, ReadOnlySpan<byte> snapshot = default)
    {
        Modules = Array.AsReadOnly(modules.ToArray());
        this.snapshot = snapshot.IsEmpty ? """{"schemaVersion":"1","modules":[]}"""u8.ToArray() : snapshot.ToArray();
    }

    internal byte[] CopySnapshot() => snapshot.ToArray();
}

public sealed class ModuleSelectionEntry
{
    private readonly JsonElement settings;
    public ModuleId Owner { get; }
    public bool Enabled { get; }
    internal JsonElement Settings => settings.Clone();

    internal ModuleSelectionEntry(ModuleId owner, bool enabled, JsonElement settings) =>
        (Owner, Enabled, this.settings) = (owner, enabled, settings.Clone());
}

public static class ModuleSelectionReader
{
    private const int FileLimit = 1_048_576;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static ModuleSelectionDocument Read(string? selectedFile, string workingDirectory)
    {
        if (selectedFile is null) return new([]);
        if (string.IsNullOrWhiteSpace(selectedFile) || !Path.IsPathFullyQualified(workingDirectory))
            throw new ModuleSelectionException();
        try
        {
            var path = Path.GetFullPath(selectedFile, workingDirectory);
            var parent = Path.GetDirectoryName(path) ?? throw new ModuleSelectionException();
            UnixStorePaths.ValidateRoot(parent, allowMissing: false);
            UnixStorePaths.ValidateFile(path);
            using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (file.Length > FileLimit) throw new ModuleSelectionException();
            var bytes = new byte[FileLimit + 1];
            var length = 0;
            while (length < bytes.Length)
            {
                var read = file.Read(bytes, length, bytes.Length - length);
                if (read == 0) break;
                length += read;
            }
            if (length > FileLimit) throw new ModuleSelectionException();
            UnixStorePaths.ValidateRoot(parent, allowMissing: false);
            UnixStorePaths.ValidateFile(path);
            return Parse(bytes.AsSpan(0, length));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            ArgumentException or NotSupportedException or StorePathException)
        {
            throw new ModuleSelectionException();
        }
    }

    public static ModuleSelectionDocument Parse(ReadOnlySpan<byte> utf8Json)
    {
        if (utf8Json.Length > FileLimit || utf8Json.StartsWith(new byte[] { 0xef, 0xbb, 0xbf }))
            throw new ModuleSelectionException();
        try
        {
            _ = StrictUtf8.GetCharCount(utf8Json);
            var reader = new Utf8JsonReader(utf8Json, new JsonReaderOptions { MaxDepth = 8 });
            var objects = new Stack<HashSet<string>?>();
            var modules = 0;
            var modulesDepth = -1;
            var settingsDepth = -1;
            long settingsStart = 0;
            var ownersSeen = new HashSet<ModuleId>();
            var first = true;
            while (reader.Read())
            {
                if (first)
                {
                    if (reader.TokenType != JsonTokenType.StartObject) throw new ModuleSelectionException();
                    first = false;
                }
                if (settingsDepth >= 0 && reader.BytesConsumed - settingsStart > 16_384)
                    throw new ModuleSelectionException();
                if (modulesDepth >= 0 && reader.CurrentDepth == modulesDepth + 1 &&
                    reader.TokenType is not (JsonTokenType.StartObject or JsonTokenType.EndObject))
                    throw new ModuleSelectionException();
                if (modulesDepth >= 0 && reader.CurrentDepth == modulesDepth + 1 &&
                    reader.TokenType is not (JsonTokenType.PropertyName or JsonTokenType.EndObject or JsonTokenType.EndArray) &&
                    ++modules > 32)
                    throw new ModuleSelectionException();
                if (reader.TokenType == JsonTokenType.StartObject)
                {
                    objects.Push(new(StringComparer.Ordinal));
                }
                else if (reader.TokenType == JsonTokenType.StartArray) objects.Push(null);
                else if (reader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
                {
                    var fields = objects.Pop();
                    if (reader.TokenType == JsonTokenType.EndObject &&
                        (reader.CurrentDepth == 0 && fields?.Count != 2 ||
                            reader.CurrentDepth == modulesDepth + 1 && modulesDepth >= 0 && fields?.Count != 3))
                        throw new ModuleSelectionException();
                    if (reader.CurrentDepth == settingsDepth) settingsDepth = -1;
                    if (reader.CurrentDepth == modulesDepth) modulesDepth = -1;
                }
                else if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var name = reader.GetString()!;
                    if (!objects.Peek()!.Add(name)) throw new ModuleSelectionException();
                    if (reader.CurrentDepth == 1 && name is not ("schemaVersion" or "modules") ||
                        modulesDepth >= 0 && reader.CurrentDepth == modulesDepth + 2 && settingsDepth < 0 &&
                            name is not ("id" or "enabled" or "settings"))
                        throw new ModuleSelectionException();
                    if (reader.CurrentDepth == 1 && name == "schemaVersion")
                    {
                        if (!reader.Read() || reader.TokenType != JsonTokenType.String || reader.GetString() != "1")
                            throw new ModuleSelectionException();
                    }
                    if (reader.CurrentDepth == 1 && name == "modules")
                    {
                        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
                            throw new ModuleSelectionException();
                        modulesDepth = reader.CurrentDepth;
                        objects.Push(null);
                    }
                    else if (modulesDepth >= 0 && reader.CurrentDepth == modulesDepth + 2 && settingsDepth < 0)
                    {
                        if (!reader.Read()) throw new ModuleSelectionException();
                        if (name == "settings")
                        {
                            if (reader.TokenType != JsonTokenType.StartObject) throw new ModuleSelectionException();
                            settingsDepth = reader.CurrentDepth;
                            settingsStart = reader.TokenStartIndex;
                            objects.Push(new(StringComparer.Ordinal));
                        }
                        else if (name == "id")
                        {
                            if (reader.TokenType != JsonTokenType.String ||
                                !ModuleId.TryParse(reader.GetString(), out var owner) || !ownersSeen.Add(owner))
                                throw new ModuleSelectionException();
                        }
                        else if (reader.TokenType is not (JsonTokenType.True or JsonTokenType.False))
                            throw new ModuleSelectionException();
                    }
                }
                else if (reader.TokenType == JsonTokenType.String)
                {
                    _ = reader.GetString();
                }
            }
            if (first || objects.Count != 0 || reader.TokenType != JsonTokenType.EndObject)
                throw new ModuleSelectionException();
            using var document = JsonDocument.Parse(utf8Json.ToArray(),
                new JsonDocumentOptions { MaxDepth = 8, AllowDuplicateProperties = false });
            var root = document.RootElement;
            Keys(root, ["schemaVersion", "modules"]);
            if (root.GetProperty("schemaVersion").ValueKind != JsonValueKind.String ||
                root.GetProperty("schemaVersion").GetString() != "1")
                throw new ModuleSelectionException();
            var entries = root.GetProperty("modules");
            if (entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() > 32)
                throw new ModuleSelectionException();
            var owners = new HashSet<ModuleId>();
            var result = new List<ModuleSelectionEntry>();
            foreach (var entry in entries.EnumerateArray())
            {
                Keys(entry, ["id", "enabled", "settings"]);
                var id = entry.GetProperty("id");
                var enabled = entry.GetProperty("enabled");
                var settings = entry.GetProperty("settings");
                if (id.ValueKind != JsonValueKind.String ||
                    !ModuleId.TryParse(id.GetString(), out var owner) || !owners.Add(owner) ||
                    enabled.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    settings.ValueKind != JsonValueKind.Object)
                    throw new ModuleSelectionException();
                result.Add(new(owner, enabled.GetBoolean(), settings));
            }
            return new(result, utf8Json);
        }
        catch (Exception error) when (error is JsonException or DecoderFallbackException or
            InvalidOperationException or KeyNotFoundException)
        {
            throw new ModuleSelectionException();
        }
    }

    private static void Keys(JsonElement value, string[] names)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new ModuleSelectionException();
        var fields = value.EnumerateObject().Select(property => property.Name).ToArray();
        if (fields.Length != names.Length || !fields.ToHashSet(StringComparer.Ordinal).SetEquals(names))
            throw new ModuleSelectionException();
    }
}
