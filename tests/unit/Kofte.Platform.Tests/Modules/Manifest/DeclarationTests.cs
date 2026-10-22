using Kofte.Platform.Modules.Manifest;
using static Kofte.Platform.Tests.Modules.Manifest.ValidationResultTests;

namespace Kofte.Platform.Tests.Modules.Manifest;

public sealed class DeclarationTests
{
    [Theory]
    [InlineData(null, "host_context_required")]
    [InlineData("", "host_context_required")]
    [InlineData("1.0", "host_context_invalid")]
    public void ContextNeverDefaults(string? host, string code) => Reject(Check(Json, host), code, "/hostContractVersion");

    [Fact]
    public void NullContextIsRejectionAndNotAnException() =>
        Reject(ManifestValidator.Validate(Minimal, null), "host_context_required", "/hostContractVersion");

    [Theory]
    [InlineData("backend")]
    [InlineData("frontend")]
    [InlineData("resources")]
    [InlineData("providers")]
    [InlineData("events")]
    [InlineData("workflows")]
    [InlineData("jobs")]
    [InlineData("permissions")]
    [InlineData("mcp-tools")]
    public void EveryKindSupportsStableBuildMetadata(string kind)
    {
        var json = Json.Replace("\"capabilities\": []",
            $"\"capabilities\": [{{\"kind\":\"{kind}\",\"contractVersion\":\"1.0.0+build\"}}]");
        Assert.IsType<ManifestValidationResult.Accepted>(Check(json));
    }

    [Theory]
    [InlineData("notes.read", true)]
    [InlineData("notes-read2", true)]
    [InlineData("a0.b1", true)]
    [InlineData("Notes.Read", false)]
    [InlineData("notes..read", false)]
    [InlineData("notes.1read", false)]
    [InlineData("notes-", false)]
    public void PermissionGrammarIsLocal(string name, bool valid)
    {
        var json = Json.Replace("\"permissions\": []", $"\"permissions\": [{{\"name\":\"{name}\",\"description\":\"Read.\"}}]");
        if (valid) Assert.IsType<ManifestValidationResult.Accepted>(Check(json));
        else Reject(Check(json), "permission_invalid", "/permissions/0/name");
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("\u0085")]
    [InlineData("\n")]
    public void PermissionDescriptionRejectsBlankAndControls(string text)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(Minimal)!;
        node["permissions"] = new System.Text.Json.Nodes.JsonArray(new System.Text.Json.Nodes.JsonObject
        {
            ["name"] = "read",
            ["description"] = text
        });
        Reject(Check(node.ToJsonString()), "permission_invalid", "/permissions/0/description");
    }
}
