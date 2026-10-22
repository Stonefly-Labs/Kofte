using System.Text;
using System.Text.Json.Nodes;
using Kofte.Platform.Modules.Manifest;
using static Kofte.Platform.Tests.Modules.Manifest.ValidationResultTests;

namespace Kofte.Platform.Tests.Modules.Manifest;

public sealed class ManifestParsingTests
{
    [Theory]
    [InlineData("schemaVersion")]
    [InlineData("id")]
    [InlineData("version")]
    [InlineData("displayName")]
    [InlineData("hostContract")]
    [InlineData("capabilities")]
    [InlineData("dependencies")]
    [InlineData("permissions")]
    [InlineData("storage")]
    public void AllRootDeclarationsAreRequired(string field)
    {
        var node = JsonNode.Parse(Minimal)!.AsObject();
        node.Remove(field);
        Reject(Check(node.ToJsonString()), "field_required", "");
        node[field] = null;
        Reject(Check(node.ToJsonString()), "field_type", "/" + field);
    }

    [Theory]
    [InlineData("capabilities", """[{"kind":"backend"}]""", "field_required", "/capabilities/0")]
    [InlineData("capabilities", """[{"kind":true,"contractVersion":"1.0.0"}]""", "field_type", "/capabilities/0/kind")]
    [InlineData("dependencies", """[{}]""", "field_required", "/dependencies/0")]
    [InlineData("permissions", """[{"name":"read","description":null}]""", "field_type", "/permissions/0/description")]
    [InlineData("storage", "{}", "field_required", "/storage")]
    [InlineData("storage", """{"relational":"false"}""", "field_type", "/storage/relational")]
    [InlineData("storage", """{"relational":false,"path":"secret"}""", "field_unknown", "/storage")]
    public void EveryObjectIsClosedAndTyped(string field, string value, string code, string path)
    {
        var node = JsonNode.Parse(Minimal)!.AsObject();
        node[field] = JsonNode.Parse(value);
        Reject(Check(node.ToJsonString()), code, path);
    }

    [Theory]
    [InlineData("""{"id":"org.example.notes","\u0069d":"org.other.notes"}""", "field_duplicate", "")]
    [InlineData("""{"storage":{"relational":false,"relational":true}}""", "field_duplicate", "/storage")]
    [InlineData("""{"capabilities":[{"kind":"backend","kind":"events"}]}""", "field_duplicate", "/capabilities/0")]
    [InlineData("""{"unexpected":{"hidden":1,"hidden":2}}""", "field_duplicate", "")]
    [InlineData("""{"schemaVersion":"1",}""", "document_invalid", "")]
    [InlineData("""/* hi */ {}""", "document_invalid", "")]
    [InlineData("{}{}", "document_invalid", "")]
    [InlineData("[]", "document_invalid", "")]
    [InlineData("", "document_invalid", "")]
    [InlineData("""{"displayName":"\uD800"}""", "encoding_invalid", "")]
    [InlineData("""{"displayName":"\uDC00"}""", "encoding_invalid", "")]
    public void RawBytesCannotBecomeLastWinsOrReplacementText(string raw, string code, string path) =>
        Reject(Check(raw), code, path);

    [Fact]
    public void UnsupportedSchemaStopsInterpretationAndEncodingPrecedesParsing()
    {
        var result = Reject(Check("""{"schemaVersion":"2","id":true,"unknown":1}"""), "schema_unsupported", "/schemaVersion");
        Assert.Single(result.Diagnostics);
        Reject(Check(new byte[] { 0xef, 0xbb, 0xbf, 0x7b, 0x7d }), "encoding_invalid", "");
        Reject(Check(Encoding.UTF8.GetBytes("""{"displayName":"\uD800\u0041"}""")), "encoding_invalid", "");
        Assert.IsType<ManifestValidationResult.Accepted>(Check(Json.Replace("Notes", @"\uD83E\uDDC6")));
    }

    [Fact]
    public void UnknownObjectsCannotInventRecognizedCollectionPathsOrDiagnosticChildren()
    {
        var node = JsonNode.Parse(Minimal)!.AsObject();
        node["untrusted"] = new JsonObject
        {
            ["dependencies"] = new JsonArray(Enumerable.Range(0, 65)
                .Select(_ => (JsonNode)new JsonObject()).ToArray())
        };
        var rejected = Reject(Check(node.ToJsonString()), "field_unknown", "");
        Assert.Single(rejected.Diagnostics);
        Reject(Check("""{"untrusted":{"storage":{"relational":false,"relational":true}}}"""),
            "field_duplicate", "");
    }

    [Fact]
    public void NestedRequiredUnknownAndDuplicateFieldsAreCheckedAtEveryObjectKind()
    {
        foreach (var (field, item) in new[] {
            ("capabilities", """{"kind":"backend","contractVersion":"1.0.0"}"""),
            ("dependencies", """{"id":"org.example.absent","versionRange":"1.0.0"}"""),
            ("permissions", """{"name":"read","description":"Read."}""")
        })
        {
            var original = JsonNode.Parse(item)!.AsObject();
            foreach (var child in original.Select(p => p.Key).ToArray())
            {
                var node = JsonNode.Parse(Minimal)!.AsObject();
                var missing = original.DeepClone().AsObject();
                missing.Remove(child);
                node[field] = new JsonArray(missing);
                Reject(Check(node.ToJsonString()), "field_required", "/" + field + "/0");
                missing[child] = null;
                Reject(Check(node.ToJsonString()), "field_type", "/" + field + "/0/" + child);
            }
            var unknown = JsonNode.Parse(Minimal)!.AsObject();
            original["untrusted"] = "secret";
            unknown[field] = new JsonArray(original);
            Reject(Check(unknown.ToJsonString()), "field_unknown", "/" + field + "/0");
            var raw = $$"""{"{{field}}":[{"name":"first","\u006eame":"second"}]}""";
            Reject(Check(raw), "field_duplicate", "/" + field + "/0");
        }
    }
}
