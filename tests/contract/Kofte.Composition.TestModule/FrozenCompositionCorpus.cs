using System.Text.Json;
using Kofte.Platform.Modules.Composition;

namespace Kofte.Composition.TestModule;

public sealed record CompositionCorpusCase(string Name, IReadOnlyList<ModuleDeployment> Deployments,
    IReadOnlyList<ModuleSelection> Selection, IReadOnlyList<string> ExpectedOrder,
    IReadOnlyList<string> ExpectedCodes, IReadOnlyList<string> ExpectedManifestCodes,
    IReadOnlyList<string?> ExpectedOwners, IReadOnlyList<IReadOnlyList<string>> ExpectedRelated,
    IReadOnlyList<string> ExpectedManifestPaths);

public static class FrozenCompositionCorpus
{
    private static ModuleDeployment Module(string id, string[]? dependencies = null, string version = "1.0.0",
        string range = "1.0.0", string schema = "1", string host = "1.0.0") =>
        new(JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = schema,
            id,
            version,
            displayName = "Corpus",
            hostContract = host,
            capabilities = Array.Empty<object>(),
            dependencies = (dependencies ?? []).Select(d => new { id = d, versionRange = range }),
            permissions = Array.Empty<object>(),
            storage = new { relational = false }
        }), configureServices: (_, _) => throw new InvalidOperationException("Resolver invoked registrar."),
            createRuntime: (_, _) => throw new InvalidOperationException("Resolver invoked runtime factory."));

    private static CompositionCorpusCase Case(string name, ModuleDeployment[] deployments, string[] enabled,
        string[]? order = null, string[]? codes = null, string[]? manifestCodes = null,
        string?[]? owners = null, string[][]? related = null, string[]? manifestPaths = null) =>
        new(name, Array.AsReadOnly(deployments), Array.AsReadOnly(enabled.Select(id => new ModuleSelection(id, true)).ToArray()),
            Array.AsReadOnly(order ?? []), Array.AsReadOnly(codes ?? []), Array.AsReadOnly(manifestCodes ?? []),
            Array.AsReadOnly(owners ?? new string?[codes?.Length ?? 0]),
            Array.AsReadOnly((related ?? Enumerable.Range(0, codes?.Length ?? 0).Select(_ => Array.Empty<string>()).ToArray())
                .Select(ids => (IReadOnlyList<string>)Array.AsReadOnly(ids)).ToArray()),
            Array.AsReadOnly(manifestPaths ?? []));

    // These expectations are intentionally authored literals, never produced by the resolver.
    public static IReadOnlyList<CompositionCorpusCase> Cases { get; } = Array.AsReadOnly(new[]
    {
        Case("empty", [], []),
        Case("disabled", [Module("org.example.a")], []),
        Case("chain", [Module("org.example.c", ["org.example.b"]), Module("org.example.b", ["org.example.a"]), Module("org.example.a")],
            ["org.example.a", "org.example.b", "org.example.c"], ["org.example.a", "org.example.b", "org.example.c"]),
        Case("diamond", [Module("org.example.d", ["org.example.b", "org.example.c"]), Module("org.example.c", ["org.example.a"]),
            Module("org.example.b", ["org.example.a"]), Module("org.example.a")],
            ["org.example.a", "org.example.b", "org.example.c", "org.example.d"],
            ["org.example.a", "org.example.b", "org.example.c", "org.example.d"]),
        Case("disconnected", [Module("org.example.z"), Module("org.example.b"), Module("org.example.a")],
            ["org.example.z", "org.example.b", "org.example.a"], ["org.example.a", "org.example.b", "org.example.z"]),
        Case("invalid-disabled", [new ModuleDeployment("!"u8)], [],
            codes: ["composition_manifest_rejected"], manifestCodes: ["document_invalid"], manifestPaths: [""]),
        Case("invalid-id", [Module("PRIVATE/path")], [],
            codes: ["composition_manifest_rejected"], manifestCodes: ["module_id_invalid"], manifestPaths: ["/id"]),
        Case("unsupported-schema", [Module("org.example.a", schema: "2")], [],
            codes: ["composition_manifest_rejected"], manifestCodes: ["schema_unsupported"], manifestPaths: ["/schemaVersion"]),
        Case("incompatible-host", [Module("org.example.a", host: "2.0.0")], [],
            codes: ["composition_manifest_rejected"], manifestCodes: ["host_incompatible"], manifestPaths: ["/hostContract"]),
        Case("unsupported-composition", [new ModuleDeployment("{}"u8, contractVersion: 2)], [],
            codes: ["composition_contract_unsupported"]),
        Case("duplicate", [Module("org.example.a"), Module("org.example.a", version: "2.0.0")], ["org.example.a"],
            codes: ["composition_module_duplicate"], owners: ["org.example.a"]),
        Case("missing-implementation", [], ["org.example.a"], codes: ["composition_implementation_missing"], owners: ["org.example.a"]),
        Case("missing-dependency", [Module("org.example.a", ["org.example.b"])], ["org.example.a"],
            codes: ["composition_dependency_missing"], owners: ["org.example.a"], related: [["org.example.b"]]),
        Case("disabled-dependency", [Module("org.example.a", ["org.example.b"]), Module("org.example.b")], ["org.example.a"],
            codes: ["composition_dependency_disabled"], owners: ["org.example.a"], related: [["org.example.b"]]),
        Case("incompatible-dependency", [Module("org.example.a", ["org.example.b"]), Module("org.example.b", version: "2.0.0")],
            ["org.example.a", "org.example.b"], codes: ["composition_dependency_incompatible"],
            owners: ["org.example.a"], related: [["org.example.b"]]),
        Case("prerelease-ineligible", [Module("org.example.a", ["org.example.b"], range: ">=1.0.0 <2.0.0"),
            Module("org.example.b", version: "1.1.0-alpha")], ["org.example.a", "org.example.b"],
            codes: ["composition_dependency_incompatible"], owners: ["org.example.a"], related: [["org.example.b"]]),
        Case("prerelease-eligible", [Module("org.example.a", ["org.example.b"], range: ">=1.1.0-alpha <2.0.0"),
            Module("org.example.b", version: "1.1.0-beta")], ["org.example.a", "org.example.b"],
            ["org.example.b", "org.example.a"]),
        Case("build-ignored", [Module("org.example.a", ["org.example.b"]), Module("org.example.b", version: "1.0.0+build")],
            ["org.example.a", "org.example.b"], ["org.example.b", "org.example.a"]),
        Case("self-is-manifest", [Module("org.example.a", ["org.example.a"])], ["org.example.a"],
            codes: ["composition_manifest_rejected"], manifestCodes: ["dependency_self"], manifestPaths: ["/dependencies/0/id"]),
        Case("multiple-scc", [Module("org.example.a", ["org.example.b"]), Module("org.example.b", ["org.example.a"]),
            Module("org.example.c", ["org.example.d"]), Module("org.example.d", ["org.example.c"]),
            Module("org.example.e", ["org.example.a"])],
            ["org.example.a", "org.example.b", "org.example.c", "org.example.d", "org.example.e"],
            codes: ["composition_dependency_cycle", "composition_dependency_cycle"],
            owners: ["org.example.a", "org.example.c"],
            related: [["org.example.a", "org.example.b"], ["org.example.c", "org.example.d"]]),
        Case("corrected-cycle", [Module("org.example.b", ["org.example.a"]), Module("org.example.a")],
            ["org.example.a", "org.example.b"], ["org.example.a", "org.example.b"])
    });

    public static IReadOnlyList<ModuleDeployment> Permute(IReadOnlyList<ModuleDeployment> deployments, int permutation)
    {
        var remaining = deployments.ToList();
        var result = new List<ModuleDeployment>();
        while (remaining.Count > 0)
        {
            var index = permutation % remaining.Count;
            permutation /= remaining.Count;
            result.Add(remaining[index]);
            remaining.RemoveAt(index);
        }
        return result.AsReadOnly();
    }

}
