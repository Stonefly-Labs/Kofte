using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Kofte.Platform.Modules.Manifest;

internal static class ManifestReader
{
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    internal static readonly string[] RootFields = [
        "schemaVersion", "id", "version", "displayName", "hostContract",
        "capabilities", "dependencies", "permissions", "storage"
    ];
    private sealed class Frame(bool array, string path, int limit, bool known)
    {
        internal bool Array { get; } = array;
        internal string Path { get; } = path;
        internal int Limit { get; } = limit;
        internal bool Known { get; } = known;
        internal int Count { get; set; }
        internal string NextPath { get; set; } = path;
        internal bool NextKnown { get; set; } = known;
        internal HashSet<string> Keys { get; } = new(StringComparer.Ordinal);
    }

    internal static ManifestDiagnostic? Read(ReadOnlySpan<byte> input, out JsonDocument? document)
    {
        document = null;
        if (input.Length > 65536) return new("input_too_large");
        if (input.StartsWith(new byte[] { 0xef, 0xbb, 0xbf })) return new("encoding_invalid");
        string text;
        try { text = StrictUtf8.GetString(input); }
        catch (DecoderFallbackException) { return new("encoding_invalid"); }
        if (!EscapedScalars(text)) return new("encoding_invalid");
        var stack = new Stack<Frame>();
        try
        {
            var reader = new Utf8JsonReader(input, new JsonReaderOptions { MaxDepth = 64 });
            var rootSeen = false;
            while (reader.Read())
            {
                if (!rootSeen)
                {
                    if (reader.TokenType != JsonTokenType.StartObject) return new("document_invalid");
                    rootSeen = true;
                }
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var frame = stack.Peek();
                    var key = reader.GetString()!;
                    if (!frame.Keys.Add(key)) return new("field_duplicate", frame.Path);
                    frame.NextKnown = frame.Known && KnownChild(frame.Path, key);
                    frame.NextPath = frame.NextKnown ? frame.Path + "/" + key : frame.Path;
                    continue;
                }
                if (reader.TokenType is JsonTokenType.EndObject or JsonTokenType.EndArray)
                {
                    stack.Pop();
                    continue;
                }
                var path = "";
                var known = true;
                if (stack.TryPeek(out var parent))
                {
                    path = parent.NextPath;
                    known = parent.NextKnown;
                    if (parent.Array)
                    {
                        known = parent.Known;
                        if (++parent.Count > parent.Limit && parent.Limit != 0)
                            return new("collection_limit", parent.Path);
                        path = parent.Limit == 0 ? parent.Path : parent.Path + "/" +
                            (parent.Count - 1).ToString(CultureInfo.InvariantCulture);
                    }
                }
                if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    if (stack.Count == 8) return new("depth_limit");
                    var array = reader.TokenType == JsonTokenType.StartArray;
                    stack.Push(new(array, path, array && known ? path switch
                    {
                        "/capabilities" => 9,
                        "/dependencies" => 64,
                        "/permissions" => 128,
                        _ => 0
                    } : 0, known));
                }
            }
            if (!rootSeen) return new("document_invalid");
            document = JsonDocument.Parse(input.ToArray(), new JsonDocumentOptions { MaxDepth = 8 });
            return null;
        }
        catch (JsonException) { return new("document_invalid"); }
    }

    private static bool KnownChild(string path, string key)
    {
        if (path.Length == 0) return RootFields.Contains(key, StringComparer.Ordinal);
        if (path == "/storage") return key == "relational";
        var parts = path.Split('/');
        if (parts.Length != 3) return false;
        return parts[1] switch
        {
            "capabilities" => key is "kind" or "contractVersion",
            "dependencies" => key is "id" or "versionRange",
            "permissions" => key is "name" or "description",
            _ => false
        };
    }

    private static bool EscapedScalars(string text)
    {
        var inString = false;
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '"') { inString = !inString; continue; }
            if (!inString || text[i] != '\\' || ++i >= text.Length) continue;
            if (text[i] != 'u' || i + 4 >= text.Length
                || !ushort.TryParse(text.AsSpan(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                continue;
            i += 4;
            if (code is >= 0xdc00 and <= 0xdfff) return false;
            if (code is < 0xd800 or > 0xdbff) continue;
            if (i + 6 >= text.Length || text[i + 1] != '\\' || text[i + 2] != 'u'
                || !ushort.TryParse(text.AsSpan(i + 3, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var low)
                || low is < 0xdc00 or > 0xdfff) return false;
            i += 6;
        }
        return true;
    }
}
