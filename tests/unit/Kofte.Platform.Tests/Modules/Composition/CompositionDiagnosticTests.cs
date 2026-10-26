using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Manifest;
using static Kofte.Platform.Tests.Modules.Composition.CompositionResolverTests;

namespace Kofte.Platform.Tests.Modules.Composition;

public sealed class CompositionDiagnosticTests
{
    private static ModuleId Id(string value)
    {
        Assert.True(ModuleId.TryParse(value, out var id));
        return id;
    }

    // Exercise the bounded internal projection without adding public diagnostic constructors or friend assemblies.
    private static CompositionDiagnostic Diagnostic(string code, ModuleId? owner = null,
        string field = "", int index = 0, IEnumerable<ModuleId>? related = null) =>
        (CompositionDiagnostic)typeof(CompositionDiagnostic)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single()
            .Invoke([code, CompositionPhase.Dependency, owner, field, index, related, null, ""]);

    private static CompositionResolution.Rejected Project(IEnumerable<CompositionDiagnostic> diagnostics) =>
        (CompositionResolution.Rejected)typeof(CompositionDiagnostic)
            .GetMethod("Reject", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [diagnostics])!;

    [Fact]
    public void FieldOrderUsesTheAcceptedManifestSchemaNotAlphabeticalNames()
    {
        var owner = Id("org.test.a");
        CompositionDiagnostic Entry(string field, int index = 0) =>
            Diagnostic("composition_dependency_missing", owner, field, index);
        var result = Project([Entry("dependencies", 10), Entry("displayName"), Entry("dependencies", 2), Entry("version")]);
        Assert.Equal(new[] { ("version", 0), ("displayName", 0), ("dependencies", 2), ("dependencies", 10) },
            result.Diagnostics.Select(d => (d.Field, d.Index)));
    }

    private static ModuleId CanonicalId(int length, int salt)
    {
        var sizes = new[] { 1, 1, 1, 1 };
        var remaining = length - 7;
        for (var i = 0; i < sizes.Length; i++)
        {
            var increase = Math.Min(62, remaining);
            sizes[i] += increase;
            remaining -= increase;
        }
        Assert.Equal(0, remaining);
        var labels = sizes.Select(size =>
        {
            var label = Enumerable.Repeat('a', size).ToArray();
            label[0] = (char)('a' + salt % 26);
            salt /= 26;
            return new string(label);
        });
        return Id(string.Join('.', labels));
    }

    private static CompositionDiagnostic[] ProjectionOfExactSerializedSize(int targetBytes)
    {
        const int count = 32;
        var lengths = Enumerable.Repeat(7, count * 9).ToArray();
        CompositionDiagnostic[] Build() => Enumerable.Range(0, count).Select(i =>
            Diagnostic("composition_dependency_cycle", CanonicalId(lengths[i * 9], i * 9),
                "dependencies", related: Enumerable.Range(1, 8)
                    .Select(r => CanonicalId(lengths[i * 9 + r], i * 9 + r)))).ToArray();
        var remaining = targetBytes - JsonSerializer.SerializeToUtf8Bytes(Build()).Length;
        Assert.True(remaining >= 0);
        for (var i = 0; i < lengths.Length; i++)
        {
            // Each added canonical ASCII character contributes exactly one serialized UTF-8 byte.
            var increase = Math.Min(253 - lengths[i], remaining);
            lengths[i] += increase;
            remaining -= increase;
        }
        Assert.Equal(0, remaining);
        var diagnostics = Build();
        Assert.Equal(targetBytes, JsonSerializer.SerializeToUtf8Bytes(diagnostics).Length);
        return diagnostics;
    }

    [Fact]
    public void SerializedByteBudgetIsInclusiveAndOneOverReservesTheLargestFittingPrefix()
    {
        var exact = ProjectionOfExactSerializedSize(65_536);
        var acceptedProjection = Project(exact);
        Assert.Equal(32, acceptedProjection.Diagnostics.Count);
        Assert.DoesNotContain(acceptedProjection.Diagnostics, d => d.Code == "composition_diagnostics_truncated");
        Assert.Equal(65_536, JsonSerializer.SerializeToUtf8Bytes(acceptedProjection.Diagnostics).Length);

        var overflow = ProjectionOfExactSerializedSize(65_537);
        var ordered = overflow.OrderBy(d => d.Owner!.Value, StringComparer.Ordinal).ToArray();
        var marker = Diagnostic("composition_diagnostics_truncated");
        var truncated = Project(overflow);
        Assert.Equal(32, truncated.Diagnostics.Count);
        Assert.Equal(ordered.Take(31), truncated.Diagnostics.Take(31));
        Assert.Equal("composition_diagnostics_truncated", truncated.Diagnostics[^1].Code);
        Assert.True(JsonSerializer.SerializeToUtf8Bytes(truncated.Diagnostics).Length <= 65_536);
        Assert.True(JsonSerializer.SerializeToUtf8Bytes(ordered.Append(marker)).Length > 65_536);
    }

    [Fact]
    public void WrapperPreservesAcceptedManifestMultiFieldAndNumericIndexOrder()
    {
        var root = JsonNode.Parse(Bytes("org.test.a",
            Enumerable.Range(0, 12).Select(i => $"org.test.d{i}").ToArray()))!.AsObject();
        root["id"] = "PRIVATE/id";
        root["version"] = "invalid";
        root["displayName"] = " ";
        root["dependencies"]![2]!["id"] = "PRIVATE/two";
        root["dependencies"]![10]!["id"] = "PRIVATE/ten";
        root["dependencies"]![10]!["versionRange"] = "invalid";
        var rejected = Reject(Resolve([new ModuleDeployment(JsonSerializer.SerializeToUtf8Bytes(root))]),
            Enumerable.Repeat("composition_manifest_rejected", 6).ToArray());
        Assert.Equal(new[]
        {
            ("module_id_invalid", "/id"), ("version_invalid", "/version"), ("display_name_invalid", "/displayName"),
            ("module_id_invalid", "/dependencies/2/id"), ("module_id_invalid", "/dependencies/10/id"),
            ("range_invalid", "/dependencies/10/versionRange")
        }, rejected.Diagnostics.Select(d => (d.ManifestDiagnostic!.Code, d.ManifestDiagnostic.Path)));
        Assert.DoesNotContain("PRIVATE", JsonSerializer.Serialize(rejected.Diagnostics));
    }

    [Fact]
    public void InvalidManifestsUseFrozenDigestOrderIncludingDuplicateInvalidIdentities()
    {
        var id = new ModuleDeployment(
            """{"schemaVersion":"1","id":"PRIVATE/one","version":"1.0.0","displayName":"A","hostContract":"1.0.0","capabilities":[],"dependencies":[],"permissions":[],"storage":{"relational":false}}"""u8);
        var versionDisplay = new ModuleDeployment(
            """{"schemaVersion":"1","id":"org.test.b","version":"PRIVATE/version","displayName":" ","hostContract":"1.0.0","capabilities":[],"dependencies":[],"permissions":[],"storage":{"relational":false}}"""u8);
        // Frozen byte-fixture SHA-256 prefixes: version/display 7F618914, invalid ID E834E852.
        foreach (ModuleDeployment[] input in new[] { new[] { id, versionDisplay }, new[] { versionDisplay, id } })
        {
            var rejected = Assert.IsType<CompositionResolution.Rejected>(Resolve(input));
            Assert.Equal(["version_invalid", "display_name_invalid", "module_id_invalid"],
                rejected.Diagnostics.Select(d => d.ManifestDiagnostic!.Code));
            Assert.All(rejected.Diagnostics, d => Assert.Null(d.Owner));
            var text = JsonSerializer.Serialize(rejected.Diagnostics);
            Assert.DoesNotContain("PRIVATE", text);
            Assert.DoesNotContain("7F618914", text);
            Assert.DoesNotContain("E834E852", text);
        }
        foreach (ModuleDeployment[] input in new[] { new[] { id, versionDisplay, id }, new[] { id, id, versionDisplay } })
        {
            var rejected = Assert.IsType<CompositionResolution.Rejected>(Resolve(input));
            Assert.Equal(["version_invalid", "display_name_invalid", "module_id_invalid", "module_id_invalid"],
                rejected.Diagnostics.Select(d => d.ManifestDiagnostic!.Code));
        }
    }

    [Fact]
    public void ResolvedSnapshotHasAnInternalOpaqueFingerprintWithNoPublicProjection()
    {
        var first = Assert.IsType<CompositionResolution.Accepted>(Resolve([])).Composition;
        var second = Assert.IsType<CompositionResolution.Accepted>(Resolve([])).Composition;
        var fingerprint = typeof(ResolvedComposition).GetProperty("Fingerprint", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(fingerprint);
        Assert.NotNull(fingerprint.GetValue(first));
        Assert.NotSame(fingerprint.GetValue(first), fingerprint.GetValue(second));
        Assert.Null(typeof(ResolvedComposition).GetProperty("Fingerprint", BindingFlags.Instance | BindingFlags.Public));
        Assert.DoesNotContain("Fingerprint", JsonSerializer.Serialize(first));
    }
}
