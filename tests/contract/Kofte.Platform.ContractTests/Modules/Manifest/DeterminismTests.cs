using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace Kofte.Platform.ContractTests.Modules.Manifest;

public sealed class DeterminismTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("en-US", "1.5")]
    [InlineData("tr-TR", "1,5")]
    public async Task CompleteCorpusHasOneHundredDecisionsPerRealCultureAndConsumer(string name, string decimalText)
    {
        var original = CultureInfo.CurrentCulture;
        var originalUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(name, false);
            CultureInfo.CurrentUICulture = new CultureInfo(name, false);
            Assert.Equal(name, CultureInfo.CurrentCulture.Name);
            Assert.Equal(decimalText, 1.5m.ToString(CultureInfo.CurrentCulture));
            foreach (var item in ManifestCorpus.Cases())
            {
                for (var repetition = 0; repetition < 100; repetition++)
                    ConsumerHarness.Expected(item, ConsumerHarness.Host(item));
                var output = await ConsumerHarness.Author(item.Bytes, ConsumerHarness.Arguments(item, name, 100));
                Assert.Equal(item.Code is null ? 0 : 2, output.ExitCode);
                Assert.Equal("culture_verified:" + name + ":" + decimalText, output.Stderr.Trim());
                var decisions = output.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                Assert.Equal(100, decisions.Length);
                foreach (var decision in decisions) ConsumerHarness.Expected(item, ConsumerHarness.Parse(decision));
            }
            output.WriteLine($"culture={name}; cases={ManifestCorpus.Cases().Count}; repetitions=100; consumers=2");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.CurrentUICulture = originalUi;
        }
    }

    [Fact]
    public async Task ReorderedPropertiesAndNumericIndicesHaveSchemaOrdering()
    {
        var node = JsonNode.Parse(ManifestCorpus.Minimal)!.AsObject();
        node["id"] = "BAD";
        node["displayName"] = " ";
        node["dependencies"] = new JsonArray(Enumerable.Range(0, 12).Select(_ =>
            (JsonNode)new JsonObject { ["id"] = "BAD", ["versionRange"] = "1.0.0" }).ToArray());
        var reverse = new JsonObject(node.Reverse().Select(p =>
            new KeyValuePair<string, JsonNode?>(p.Key, p.Value?.DeepClone())));
        var first = ConsumerHarness.Host(new("multi", Encoding.UTF8.GetBytes(node.ToJsonString()), Host: "1.0"));
        var second = ConsumerHarness.Host(new("multi", Encoding.UTF8.GetBytes(reverse.ToJsonString()), Host: "1.0"));
        Assert.Equal(first.Diagnostics, second.Diagnostics);
        Assert.Equal(("context", "/hostContractVersion", "host_context_invalid"), first.Diagnostics[0]);
        Assert.Equal(("manifest", "/id", "module_id_invalid"), first.Diagnostics[1]);
        Assert.Equal(("manifest", "/displayName", "display_name_invalid"), first.Diagnostics[2]);
        for (var i = 0; i < 12; i++)
            Assert.Equal(("manifest", $"/dependencies/{i}/id", "module_id_invalid"), first.Diagnostics[i + 3]);
        foreach (var value in new[] { node, reverse })
        {
            var author = await ConsumerHarness.Author(Encoding.UTF8.GetBytes(value.ToJsonString()),
                "--host-contract", "1.0");
            Assert.Equal(2, author.ExitCode);
            Assert.Equal(first.Diagnostics, ConsumerHarness.Parse(author.Stdout).Diagnostics);
        }
    }
}
