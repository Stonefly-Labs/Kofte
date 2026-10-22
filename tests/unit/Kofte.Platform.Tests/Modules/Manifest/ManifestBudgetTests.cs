using System.Text;
using System.Text.Json.Nodes;
using Kofte.Platform.Modules.Manifest;
using static Kofte.Platform.Tests.Modules.Manifest.ValidationResultTests;

namespace Kofte.Platform.Tests.Modules.Manifest;

public sealed class ManifestBudgetTests
{
    private static byte[] Change(string field, JsonNode value)
    {
        var node = JsonNode.Parse(Minimal)!.AsObject();
        node[field] = value;
        return Encoding.UTF8.GetBytes(node.ToJsonString());
    }

    [Fact]
    public void OriginalByteBudgetIsInclusiveAndBeforeEncoding()
    {
        var text = Json.Replace("Notes", "Köfte \U0001F9C6");
        var bytes = Encoding.UTF8.GetBytes(text);
        var exact = bytes.Concat(Enumerable.Repeat((byte)' ', 65536 - bytes.Length)).ToArray();
        Assert.IsType<ManifestValidationResult.Accepted>(Check(exact));
        Reject(Check(exact.Append((byte)0xff).ToArray()), "input_too_large", "");
    }

    [Theory]
    [InlineData(7, "field_unknown")]
    [InlineData(8, "depth_limit")]
    public void RootCountsAsDepthOne(int arrays, string code) =>
        Reject(Check("""{"schemaVersion":"1","unexpected":""" + new string('[', arrays)
            + "0" + new string(']', arrays) + "}"), code, "");

    [Theory]
    [InlineData("capabilities", 9)]
    [InlineData("dependencies", 64)]
    [InlineData("permissions", 128)]
    public void CollectionsAreNotSilentlyTruncated(string field, int limit)
    {
        JsonNode Item(int i) => field switch
        {
            "capabilities" => new JsonObject
            {
                ["kind"] = new[] { "backend", "frontend", "resources", "providers",
                "events", "workflows", "jobs", "permissions", "mcp-tools" }[i % 9],
                ["contractVersion"] = "1.0.0"
            },
            "dependencies" => new JsonObject { ["id"] = $"org.example.d{i}", ["versionRange"] = "1.0.0" },
            _ => new JsonObject { ["name"] = $"p{i}", ["description"] = "Read." }
        };
        Assert.IsType<ManifestValidationResult.Accepted>(Check(Change(field,
            new JsonArray(Enumerable.Range(0, limit).Select(Item).ToArray()))));
        var result = Reject(Check(Change(field,
            new JsonArray(Enumerable.Range(0, limit + 1).Select(Item).ToArray()))), "collection_limit", "/" + field);
        Assert.Single(result.Diagnostics);
    }

    [Theory]
    [InlineData("displayName", 128, "display_name_invalid")]
    [InlineData("version", 128, "version_invalid")]
    public void ScalarAndAsciiLimits(string field, int limit, string code)
    {
        string Value(int count) => field == "version" ? "1.0.0+" + new string('a', count - 6)
            : string.Concat(Enumerable.Repeat("\U0001F9C6", count));
        Assert.IsType<ManifestValidationResult.Accepted>(Check(Change(field, JsonValue.Create(Value(limit))!)));
        Reject(Check(Change(field, JsonValue.Create(Value(limit + 1))!)), code, "/" + field);
    }

    [Fact]
    public void PermissionAndDefensiveRangeBudgets()
    {
        byte[] Permission(int name, int description) => Change("permissions", new JsonArray(
            new JsonObject
            {
                ["name"] = new string('a', name),
                ["description"] = string.Concat(Enumerable.Repeat("\U0001F9C6", description))
            }));
        Assert.IsType<ManifestValidationResult.Accepted>(Check(Permission(64, 256)));
        Reject(Check(Permission(65, 256)), "permission_invalid", "/permissions/0/name");
        Reject(Check(Permission(64, 257)), "permission_invalid", "/permissions/0/description");
        foreach (var length in new[] { 512, 513 })
            Reject(Check(Change("hostContract", JsonValue.Create(new string('a', length))!)),
                "range_invalid", "/hostContract");
    }

    [Theory]
    [InlineData(32)]
    [InlineData(33)]
    public void DiagnosticTruncationUsesNumericOrderAfterSorting(int count)
    {
        var bytes = Change("dependencies", new JsonArray(Enumerable.Range(0, count).Select(_ =>
            (JsonNode)new JsonObject { ["id"] = "BAD", ["versionRange"] = "1.0.0" }).ToArray()));
        var result = Reject(Check(bytes), "module_id_invalid", "/dependencies/0/id");
        Assert.Equal(32, result.Diagnostics.Count);
        for (var i = 0; i < (count == 32 ? 32 : 31); i++)
            Assert.Equal($"/dependencies/{i}/id", result.Diagnostics[i].Path);
        Assert.Equal(count == 32 ? "module_id_invalid" : "diagnostics_truncated", result.Diagnostics[^1].Code);
        Assert.Equal(count == 32 ? "/dependencies/31/id" : "", result.Diagnostics[^1].Path);
    }

    [Fact]
    public void FatalThenShapeThenSemanticThenCompatibilityPrecedence()
    {
        Assert.Single(Reject(Check("""{"schemaVersion":"2","id":null,"id":"BAD"}"""), "field_duplicate", "").Diagnostics);
        Assert.Single(Reject(Check("""{"schemaVersion":"2","id":null}"""), "schema_unsupported", "/schemaVersion").Diagnostics);
        var shape = Reject(Check(Json.Replace("\"Notes\"", "null").Replace("org.example.notes", "BAD"), "2.0.0"),
            "field_type", "/displayName");
        Assert.Single(shape.Diagnostics);
        var semantic = Reject(Check(Json.Replace("org.example.notes", "BAD"), "2.0.0"), "module_id_invalid", "/id");
        Assert.Single(semantic.Diagnostics);
        Assert.Single(Reject(Check(Json, "2.0.0"), "host_incompatible", "/hostContract").Diagnostics);
    }
}
