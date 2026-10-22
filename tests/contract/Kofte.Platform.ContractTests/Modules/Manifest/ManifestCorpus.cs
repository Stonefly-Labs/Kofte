using System.Text;
using System.Text.Json.Nodes;

namespace Kofte.Platform.ContractTests.Modules.Manifest;

internal sealed record ManifestCase(string Name, byte[] Bytes, string? Host = "1.0.0",
    string? Previous = null, string? Code = null, string Path = "", string Origin = "manifest",
    (string Origin, string Path, string Code)[]? Diagnostics = null)
{
    internal (string Origin, string Path, string Code)[] ExpectedDiagnostics =>
        Diagnostics ?? (Code is null ? [] : [(Origin, Path, Code)]);
    internal bool IsAccepted => ExpectedDiagnostics.Length == 0;
}

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
            Field("range-reversed", "hostContract", ">=2.0.0 <1.0.0", "range_invalid"),
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
        var precedence = new[] { "1.0.0-alpha", "1.0.0-alpha.1", "1.0.0-alpha.beta", "1.0.0-beta",
            "1.0.0-beta.2", "1.0.0-beta.11", "1.0.0-rc.1", "1.0.0" };
        for (var i = 0; i < precedence.Length - 1; i++)
        {
            var lower = precedence[i];
            var upper = precedence[i + 1];
            cases.Add(new($"range-precedence-{i}-higher",
                Change(n => n["hostContract"] = $">={lower} <2.0.0"), Host: upper));
            cases.Add(new($"range-precedence-{i}-lower",
                Change(n => n["hostContract"] = $">{upper} <2.0.0"), Host: lower,
                Code: "host_incompatible", Path: "/hostContract"));
        }
        cases.Add(new("range-huge-core", Change(n =>
            n["hostContract"] = ">=999999999999999999999999999999.0.0 <1000000000000000000000000000001.0.0"),
            Host: "1000000000000000000000000000000.0.0"));
        cases.Add(new("range-huge-prerelease", Change(n =>
            n["hostContract"] = ">=1.0.0-999999999999999999999999999999 <1.0.0"),
            Host: "1.0.0-1000000000000000000000000000000"));
        cases.AddRange(BoundaryCases());
        cases.AddRange(CollectedDiagnosticCases());
        cases.Add(new("phase-fatal-before-schema", Encoding.UTF8.GetBytes(
            """{"schemaVersion":"2","id":null,"id":"BAD"}"""), Host: "1.0",
            Code: "field_duplicate"));
        cases.Add(new("phase-schema-before-shape", Encoding.UTF8.GetBytes(
            """{"schemaVersion":"2","id":null}"""), Host: "1.0",
            Code: "schema_unsupported", Path: "/schemaVersion"));
        cases.Add(new("phase-shape-before-semantic", Change(n =>
        {
            n["displayName"] = null;
            n["id"] = "BAD";
        }), Host: "2.0.0", Code: "field_type", Path: "/displayName"));
        cases.Add(new("phase-semantic-before-compatibility", Change(n => n["id"] = "BAD"),
            Host: "2.0.0", Code: "module_id_invalid", Path: "/id"));
        return cases;
    }

    private static IEnumerable<ManifestCase> BoundaryCases()
    {
        foreach (var (name, bytes) in new[] {
            ("minimal", Minimal), ("multibyte", Encoding.UTF8.GetBytes(
                Encoding.UTF8.GetString(Minimal).Replace("Notes", "Köfte \U0001F9C6"))) })
        {
            var exact = bytes.Concat(Enumerable.Repeat((byte)' ', 65536 - bytes.Length)).ToArray();
            yield return new($"bytes-{name}-65536", exact);
            yield return new($"bytes-{name}-65537", exact.Append((byte)' ').ToArray(), Code: "input_too_large");
        }
        yield return new("bytes-before-encoding", Enumerable.Repeat((byte)0xff, 65537).ToArray(), Code: "input_too_large");
        foreach (var depth in new[] { 8, 9 })
        {
            var bytes = Change(n => n["unexpected"] = JsonNode.Parse(
                new string('[', depth - 1) + "0" + new string(']', depth - 1)));
            yield return new($"depth-{depth}", bytes, Code: depth == 8 ? "field_unknown" : "depth_limit");
        }
        var kinds = new[] { "backend", "frontend", "resources", "providers", "events",
            "workflows", "jobs", "permissions", "mcp-tools" };
        foreach (var (field, limit) in new[] { ("capabilities", 9), ("dependencies", 64), ("permissions", 128) })
            foreach (var count in new[] { limit, limit + 1 })
            {
                var bytes = Change(n => n[field] = new JsonArray(Enumerable.Range(0, count).Select(i =>
                    (JsonNode)(field switch
                    {
                        "capabilities" => new JsonObject { ["kind"] = kinds[i % 9], ["contractVersion"] = "1.0.0" },
                        "dependencies" => new JsonObject { ["id"] = $"org.example.d{i}", ["versionRange"] = "1.0.0" },
                        _ => new JsonObject { ["name"] = $"p{i}", ["description"] = "Read." }
                    })).ToArray()));
                yield return new($"{field}-{count}", bytes, Code: count == limit ? null : "collection_limit",
                    Path: count == limit ? "" : "/" + field);
            }
        foreach (var length in new[] { 128, 129 })
        {
            yield return Field($"display-scalars-{length}", "displayName",
                string.Concat(Enumerable.Repeat("\U0001F9C6", length)), length == 128 ? null : "display_name_invalid");
            yield return Field($"version-ascii-{length}", "version",
                "1.0.0+" + new string('a', length - 6), length == 128 ? null : "version_invalid");
        }
        foreach (var length in new[] { 64, 65 })
            yield return new($"permission-name-{length}", Change(n => n["permissions"] = new JsonArray(
                new JsonObject { ["name"] = new string('a', length), ["description"] = "Read." })),
                Code: length == 64 ? null : "permission_invalid", Path: length == 64 ? "" : "/permissions/0/name");
        foreach (var length in new[] { 256, 257 })
            yield return new($"permission-description-scalars-{length}", Change(n => n["permissions"] = new JsonArray(
                new JsonObject { ["name"] = "read", ["description"] = string.Concat(Enumerable.Repeat("\U0001F9C6", length)) })),
                Code: length == 256 ? null : "permission_invalid", Path: length == 256 ? "" : "/permissions/0/description");
        var boundedVersion = "1.0.0+" + new string('a', 122);
        yield return Field("range-two-128-character-endpoints", "hostContract", $">={boundedVersion} <={boundedVersion}");
        // The defensive range budget does not make a 512-character token valid syntax.
        foreach (var length in new[] { 512, 513 })
            yield return Field($"range-ascii-{length}", "hostContract", new string('a', length), "range_invalid");
    }

    internal static IReadOnlyList<ManifestCase> CollectedDiagnosticCases()
    {
        var cases = new List<ManifestCase>();
        var node = JsonNode.Parse(Minimal)!.AsObject();
        node["id"] = "BAD";
        node["version"] = "1.0";
        node["displayName"] = " ";
        node["hostContract"] = "^1.0.0";
        node["capabilities"] = JsonNode.Parse("""[{"kind":"unknown","contractVersion":"1.0"}]""");
        node["dependencies"] = new JsonArray(Enumerable.Range(0, 11).Select(_ =>
            (JsonNode)new JsonObject { ["id"] = "BAD", ["versionRange"] = "^1.0.0" }).ToArray());
        node["permissions"] = JsonNode.Parse("""[{"name":"BAD","description":" "}]""");
        var expected = new List<(string Origin, string Path, string Code)> {
            ("context", "/hostContractVersion", "host_context_invalid"),
            ("context", "/previousModuleId", "previous_id_invalid"),
            ("manifest", "/id", "module_id_invalid"),
            ("manifest", "/version", "version_invalid"),
            ("manifest", "/displayName", "display_name_invalid"),
            ("manifest", "/hostContract", "range_invalid"),
            ("manifest", "/capabilities/0/kind", "capability_unknown"),
            ("manifest", "/capabilities/0/contractVersion", "version_invalid")
        };
        for (var i = 0; i < 11; i++)
        {
            expected.Add(("manifest", $"/dependencies/{i}/id", "module_id_invalid"));
            expected.Add(("manifest", $"/dependencies/{i}/versionRange", "range_invalid"));
        }
        expected.Add(("manifest", "/permissions/0/name", "permission_invalid"));
        expected.Add(("manifest", "/permissions/0/description", "permission_invalid"));
        var reverse = ReverseObjects(node);
        foreach (var (name, value) in new[] { ("collected", node), ("collected-reversed", reverse) })
            cases.Add(new(name, Encoding.UTF8.GetBytes(value.ToJsonString()), Host: "1.0", Previous: "BAD",
                Diagnostics: expected.ToArray()));
        foreach (var count in new[] { 32, 33 })
        {
            var bytes = Change(n => n["dependencies"] = new JsonArray(Enumerable.Range(0, count).Select(_ =>
                (JsonNode)new JsonObject { ["id"] = "BAD", ["versionRange"] = "1.0.0" }).ToArray()));
            var diagnostics = Enumerable.Range(0, count == 32 ? 32 : 31)
                .Select(i => ("manifest", $"/dependencies/{i}/id", "module_id_invalid")).ToList();
            if (count == 33) diagnostics.Add(("manifest", "", "diagnostics_truncated"));
            cases.Add(new($"diagnostics-{count}", bytes, Diagnostics: diagnostics.ToArray()));
        }
        return cases;
    }

    [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNull(nameof(node))]
    private static JsonNode? ReverseObjects(JsonNode? node) => node switch
    {
        JsonObject obj => new JsonObject(obj.Reverse().Select(p =>
            new KeyValuePair<string, JsonNode?>(p.Key, ReverseObjects(p.Value)))),
        JsonArray array => new JsonArray(array.Select(ReverseObjects).ToArray()),
        _ => node?.DeepClone()
    };

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
