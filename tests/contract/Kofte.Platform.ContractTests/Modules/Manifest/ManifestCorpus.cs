using System.Text;
using System.Text.Json.Nodes;

namespace Kofte.Platform.ContractTests.Modules.Manifest;

internal sealed record ManifestCase(string Name, byte[] Bytes, string? Host = "1.0.0",
    string? Previous = null, string? Code = null, string Path = "", string Origin = "manifest");

internal static class ManifestCorpus
{
    internal static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
    internal static byte[] Minimal => File.ReadAllBytes(Fixture("examples/minimal.json"));
    internal static byte[] Change(Action<JsonObject> change)
    {
        var node = JsonNode.Parse(Minimal)!.AsObject();
        change(node);
        return Encoding.UTF8.GetBytes(node.ToJsonString());
    }

    internal static IReadOnlyList<ManifestCase> Cases()
    {
        var cases = new List<ManifestCase> {
            new("C01", Minimal),
            new("C02", File.ReadAllBytes(Fixture("examples/full.json"))),
            new("C03", Change(n => n.Remove("storage")), Code: "field_required"),
            new("C04", Change(n => n["capabilities"] = null), Code: "field_type", Path: "/capabilities"),
            new("C05", Change(n => n["unexpected"] = true), Code: "field_unknown"),
            new("C05-nested-collection", Change(n => n["unexpected"] = new JsonObject
            {
                ["dependencies"] = new JsonArray(Enumerable.Range(0, 65)
                    .Select(_ => (JsonNode)new JsonObject()).ToArray())
            }), Code: "field_unknown"),
            new("C06", Change(n => { n["schemaVersion"] = "2"; n["id"] = "BAD"; }), Code: "schema_unsupported", Path: "/schemaVersion"),
            new("C07", File.ReadAllBytes(Fixture("examples/duplicate-key.txt")), Code: "field_duplicate"),
            new("C07-unknown-parent", Encoding.UTF8.GetBytes(
                """{"unexpected":{"storage":{"relational":false,"relational":true}}}"""), Code: "field_duplicate"),
            new("C10", Change(n => { n["displayName"] = "Köfte Notes"; n["version"] = "0.2.0"; }), Previous: "org.example.notes"),
            new("C11", Minimal, Previous: "org.example.other", Code: "identity_changed", Path: "/id"),
            new("C12", Minimal, Previous: "ORG.example.notes", Code: "previous_id_invalid", Path: "/previousModuleId", Origin: "context"),
            new("C14", Change(n => n["version"] = "2147483648.0.0+001")),
            Field("C15", "hostContract", ">=1.0.0 <1.0.0", "range_invalid"),
            Field("C16", "hostContract", ">=1.02.0 <2.0.0", "range_invalid"),
            Field("C17", "hostContract", "^1.0.0", "range_invalid"),
            new("C18", Minimal, Host: "2.0.0", Code: "host_incompatible", Path: "/hostContract"),
            new("C19-empty", Minimal, Host: "", Code: "host_context_required", Path: "/hostContractVersion", Origin: "context"),
            new("C19-null", Minimal, Host: null, Code: "host_context_required", Path: "/hostContractVersion", Origin: "context"),
            new("C20", Minimal, Host: "1.0", Code: "host_context_invalid", Path: "/hostContractVersion", Origin: "context"),
            new("C21", Change(n => n["capabilities"] = JsonNode.Parse("""[{"kind":"backend","contractVersion":"1.0.0"},{"kind":"backend","contractVersion":"1.0.0+different"}]""")), Code: "capability_duplicate", Path: "/capabilities/1/kind"),
            new("C22", Change(n => n["capabilities"] = JsonNode.Parse("""[{"kind":"unknown","contractVersion":"1.0.0"}]""")), Code: "capability_unknown", Path: "/capabilities/0/kind"),
            new("C24", Change(n => n["dependencies"] = JsonNode.Parse("""[{"id":"org.example.absent","versionRange":"1.0.0"},{"id":"org.example.absent","versionRange":"2.0.0"}]""")), Code: "dependency_duplicate", Path: "/dependencies/1/id"),
            new("C25", Change(n => n["dependencies"] = JsonNode.Parse("""[{"id":"org.example.notes","versionRange":"1.0.0"}]""")), Code: "dependency_self", Path: "/dependencies/0/id"),
            new("C26", Change(n => n["dependencies"] = JsonNode.Parse("""[{"id":"org.other.notes","versionRange":"1.0.0"}]"""))),
            new("C27", Change(n => n["permissions"] = JsonNode.Parse("""[{"name":"notes.read","description":"Read."},{"name":"notes.read","description":"Read."}]""")), Code: "permission_duplicate", Path: "/permissions/1/name"),
            new("C28", Change(n => n["permissions"] = JsonNode.Parse("""[{"name":"Notes.Read","description":"Read."}]""")), Code: "permission_invalid", Path: "/permissions/0/name"),
            Field("C30", "displayName", " ", "display_name_invalid"),
            Field("C31", "displayName", "Köfte \U0001F9C6"),
            Field("C32-c0", "displayName", "Notes\nsecret", "display_name_invalid"),
            Field("C32-c1", "displayName", "Notes\u0085secret", "display_name_invalid"),
            new("C33", Change(n => n["unknown\nmarker"] = true), Code: "field_unknown"),
            new("C34-bom", new byte[] { 0xef, 0xbb, 0xbf }.Concat(Minimal).ToArray(), Code: "encoding_invalid"),
            new("C34-utf8", new byte[] { 0xff }, Code: "encoding_invalid"),
            new("C34-surrogate", Encoding.UTF8.GetBytes("""{"id":"\uD800"}"""), Code: "encoding_invalid")
        };
        var raw = Encoding.UTF8.GetString(Minimal);
        foreach (var (name, text) in new[] {
            ("truncated", raw[..^4]), ("comma", raw.TrimEnd()[..^1] + ",}"),
            ("comment", "/*comment*/" + raw), ("root", raw + "{}") })
            cases.Add(new("C08-" + name, Encoding.UTF8.GetBytes(text), Code: "document_invalid"));
        foreach (var id in new[] { "Org.example.notes", " org.example.notes", "org.exämple.notes",
            "org..notes", "org.example.-notes", "org/example/notes", "org.notes" })
            cases.Add(Field("C09-" + cases.Count, "id", id, "module_id_invalid"));
        foreach (var version in new[] { "1.02.0", "1.0", "1.0.0-01", "1.0.0+" })
            cases.Add(Field("C13-" + cases.Count, "version", version, "version_invalid"));
        foreach (var version in new[] { "1.1.0", "1.0.0-rc.1" })
            cases.Add(new("C23-" + version, Change(n => n["capabilities"] = new JsonArray(
                new JsonObject { ["kind"] = "backend", ["contractVersion"] = version })),
                Code: "capability_unsupported", Path: "/capabilities/0/contractVersion"));
        foreach (var field in new[] { "path", "schema" })
            cases.Add(new("C29-" + field, Change(n => n["storage"]![field] = "../forbidden"), Code: "field_unknown", Path: "/storage"));
        foreach (var field in new[] { "$ref", "hooks" })
            cases.Add(new("C29-" + field, Change(n => n[field] = "https://invalid.example/"), Code: "field_unknown"));
        foreach (var (range, host, accepted) in Ranges)
            cases.Add(new("range-" + cases.Count, Change(n => n["hostContract"] = range), Host: host,
                Code: accepted ? null : "host_incompatible", Path: accepted ? "" : "/hostContract"));
        foreach (var (lengths, valid) in new[] {
            (new[] { 63, 63, 63, 61 }, true), (new[] { 63, 63, 63, 62 }, false),
            (new[] { 3, 7, 63 }, true), (new[] { 3, 7, 64 }, false) })
            cases.Add(Field("id-limit-" + cases.Count, "id",
                string.Join('.', lengths.Select(n => new string('a', n))), valid ? null : "module_id_invalid"));
        cases.Add(new("dependency-id", Change(n => n["dependencies"] = JsonNode.Parse(
            """[{"id":"BAD","versionRange":"1.0.0"}]""")), Code: "module_id_invalid", Path: "/dependencies/0/id"));
        cases.Add(new("dependency-range", Change(n => n["dependencies"] = JsonNode.Parse(
            """[{"id":"org.example.absent","versionRange":"^1.0.0"}]""")), Code: "range_invalid", Path: "/dependencies/0/versionRange"));
        cases.Add(new("permission-description", Change(n => n["permissions"] = JsonNode.Parse(
            """[{"name":"read","description":" "}]""")), Code: "permission_invalid", Path: "/permissions/0/description"));
        cases.Add(new("byte-limit", new byte[65537], Code: "input_too_large"));
        cases.Add(new("depth-limit", Encoding.UTF8.GetBytes("""{"x":[[[[[[[[0]]]]]]]]}"""), Code: "depth_limit"));
        cases.Add(new("collection-limit", Change(n => n["capabilities"] = new JsonArray(
            Enumerable.Range(0, 10).Select(_ => (JsonNode)new JsonObject
            {
                ["kind"] = "backend",
                ["contractVersion"] = "1.0.0"
            }).ToArray())), Code: "collection_limit", Path: "/capabilities"));
        return cases;
    }

    internal static ManifestCase Field(string name, string field, string value, string? code = null) =>
        new(name, Change(n => n[field] = value), Code: code, Path: code is null ? "" : "/" + field);

    internal static readonly (string Range, string Host, bool Accepted)[] Ranges = [
        (">=1.0.0 <2.0.0", "1.0.0", true), (">=1.0.0 <2.0.0", "1.5.0", true),
        (">=1.0.0 <2.0.0", "1.999.0+build", true), (">=1.0.0 <2.0.0", "0.9.9", false),
        (">=1.0.0 <2.0.0", "2.0.0", false), (">=1.0.0 <2.0.0", "1.1.0-rc.1", false),
        (">1.0.0 <=2.0.0", "1.0.0", false), (">1.0.0 <=2.0.0", "2.0.0", true),
        (">=1.1.0-rc.1 <2.0.0", "1.1.0-rc.2", true),
        (">=1.1.0-rc.1 <2.0.0", "1.2.0-rc.1", false),
        ("1.0.0+one", "1.0.0+two", true), ("1.0.0+one", "1.0.0-rc.1", false),
        (">=1.0.0 <=1.0.0", "1.0.0", true)
    ];
}
