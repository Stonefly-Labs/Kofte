using System.Text.Json;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.ContractTests.Modules.Manifest;

public sealed class PublicConsumerTests
{
    [Fact]
    public async Task EveryIndependentCorpusVariantHasExactPublicDecision()
    {
        foreach (var item in ManifestCorpus.Cases())
        {
            ConsumerHarness.Expected(item, ConsumerHarness.Host(item));
            var output = await ConsumerHarness.Author(item.Bytes, ConsumerHarness.Arguments(item));
            Assert.Equal(item.Code is null ? 0 : 2, output.ExitCode);
            ConsumerHarness.Expected(item, ConsumerHarness.Parse(output.Stdout));
            Assert.StartsWith("culture_verified:", output.Stderr);
        }
    }

    [Fact]
    public void FullDescriptorExposesAllVersionDomainsAndNonAuthoritativeMetadata()
    {
        var result = ManifestValidator.Validate(File.ReadAllBytes(ManifestCorpus.Fixture("examples/full.json")), new("1.0.0"));
        var module = Assert.IsType<ManifestValidationResult.Accepted>(result).Description;
        Assert.Equal("1", module.SchemaVersion);
        Assert.Equal("org.example.notes", module.Id.Value);
        Assert.Equal("0.2.0+example", module.Version.Value);
        Assert.Equal("Köfte Notes", module.DisplayName);
        Assert.Equal(">=1.0.0 <2.0.0", module.HostContract.Value);
        Assert.Equal(new[] { "backend", "frontend", "resources", "providers", "events",
            "workflows", "jobs", "permissions", "mcp-tools" }, module.Capabilities.Select(c => c.Kind));
        Assert.All(module.Capabilities, c => Assert.Equal("1.0.0", c.ContractVersion.Value));
        var dependency = Assert.Single(module.Dependencies);
        Assert.Equal("org.example.absent", dependency.Id.Value);
        Assert.Equal(">=0.1.0 <1.0.0", dependency.VersionRange.Value);
        var permission = Assert.Single(module.Permissions);
        Assert.Equal("notes.read", permission.Name);
        Assert.Equal("Read module notes.", permission.Description);
        Assert.True(module.Storage.Relational);
    }

    [Fact]
    public void ProjectReferencesAndFixtureBytesProvePublicDirectionAndParity()
    {
        var root = ConsumerHarness.Root;
        var host = System.Xml.Linq.XDocument.Load(Path.Combine(root, "src/Kofte.Host/Kofte.Host.csproj"));
        Assert.Contains(host.Descendants("ProjectReference"), p => (string?)p.Attribute("Include") == "../Kofte.Platform/Kofte.Platform.csproj");
        var author = System.Xml.Linq.XDocument.Load(Path.Combine(root, "tests/contract/Kofte.Manifest.Consumer/Kofte.Manifest.Consumer.csproj"));
        Assert.Single(author.Descendants("ProjectReference"));
        Assert.Empty(author.Descendants("PackageReference"));
        foreach (var relative in new[] { "module-manifest.schema.json", "examples/minimal.json", "examples/full.json", "examples/duplicate-key.txt" })
            Assert.Equal(File.ReadAllBytes(Path.Combine(root, "specs/002-platform-contracts/contracts", relative)),
                File.ReadAllBytes(ManifestCorpus.Fixture(relative)));
        using var schema = JsonDocument.Parse(File.ReadAllBytes(ManifestCorpus.Fixture("module-manifest.schema.json")));
        Assert.Equal("urn:kofte:module-manifest:1", schema.RootElement.GetProperty("$id").GetString());
    }

    [Fact]
    public async Task CollectedDiagnosticsAndTruncationAreIdenticalAcrossConsumersAndCleanRetry()
    {
        var retained = Assert.IsType<ManifestValidationResult.Accepted>(
            ManifestValidator.Validate(ManifestCorpus.Minimal, new("1.0.0"))).Description;
        foreach (var count in new[] { 32, 33 })
        {
            var bytes = ManifestCorpus.Change(n => n["dependencies"] = new System.Text.Json.Nodes.JsonArray(
                Enumerable.Range(0, count).Select(_ => (System.Text.Json.Nodes.JsonNode)new System.Text.Json.Nodes.JsonObject
                {
                    ["id"] = "BAD",
                    ["versionRange"] = "1.0.0"
                }).ToArray()));
            var expected = Enumerable.Range(0, count == 32 ? 32 : 31)
                .Select(i => ("manifest", $"/dependencies/{i}/id", "module_id_invalid")).ToList();
            if (count == 33) expected.Add(("manifest", "", "diagnostics_truncated"));
            Assert.Equal(expected, ConsumerHarness.Host(new("multi", bytes)).Diagnostics);
            var author = await ConsumerHarness.Author(bytes, "--host-contract", "1.0.0");
            Assert.Equal(2, author.ExitCode);
            Assert.Equal(expected, ConsumerHarness.Parse(author.Stdout).Diagnostics);
            Assert.Equal("accepted", ConsumerHarness.Host(new("retry", ManifestCorpus.Minimal)).Status);
            var retry = await ConsumerHarness.Author(ManifestCorpus.Minimal, "--host-contract", "1.0.0");
            Assert.Equal(0, retry.ExitCode);
            Assert.Equal("accepted", ConsumerHarness.Parse(retry.Stdout).Status);
            Assert.Empty(retained.Dependencies);
        }
    }
}
