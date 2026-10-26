using System.Globalization;
using System.Text.Json;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.Tests.Modules.Composition;

public sealed class CompositionResolverTests
{
    internal static ModuleDeployment Deploy(string id, string[]? dependencies = null, string version = "1.0.0",
        string range = "1.0.0", IEnumerable<ContributionClaim>? claims = null, string[]? capabilities = null,
        object[]? permissions = null, Action? callback = null) =>
        new(Bytes(id, dependencies, version, range, capabilities, permissions), claims,
            configureServices: (_, _) => callback?.Invoke(), createRuntime: (_, _) => throw new InvalidOperationException("must not execute"));

    internal static byte[] Bytes(string id, string[]? dependencies = null, string version = "1.0.0",
        string range = "1.0.0", string[]? capabilities = null, object[]? permissions = null) =>
        JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            id,
            version,
            displayName = "Test",
            hostContract = "1.0.0",
            capabilities = (capabilities ?? []).Select(kind => new { kind, contractVersion = "1.0.0" }),
            dependencies = (dependencies ?? []).Select(d => new { id = d, versionRange = range }),
            permissions = permissions ?? [],
            storage = new { relational = false }
        });

    internal static CompositionResolution Resolve(IEnumerable<ModuleDeployment> deployments, params string[] enabled) =>
        CompositionResolver.Resolve(deployments, enabled.Select(id => new ModuleSelection(id, true)), new("1.0.0"),
            TestContext.Current.CancellationToken);
    internal static CompositionResolution.Rejected Reject(CompositionResolution result, params string[] codes)
    {
        var rejected = Assert.IsType<CompositionResolution.Rejected>(result);
        Assert.Equal(codes, rejected.Diagnostics.Select(d => d.Code));
        return rejected;
    }
    internal static void Order(CompositionResolution result, params string[] expected) =>
        Assert.Equal(expected, Assert.IsType<CompositionResolution.Accepted>(result).Composition.Order.Select(id => id.Value));

    [Fact]
    public void EmptyIsValidAndDisabledDeclarationsDoNotRunCode()
    {
        Order(Resolve([]));
        var calls = 0;
        var result = Assert.IsType<CompositionResolution.Accepted>(Resolve([Deploy("org.test.a", callback: () => calls++)]));
        Assert.Equal(["org.test.a"], result.Composition.Disabled.Select(i => i.Value));
        Assert.Empty(result.Composition.Enabled);
        Assert.Equal(0, calls);
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    public void DiamondAlwaysUsesOrdinalSmallestReadyNode(string culture)
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            ModuleDeployment[] graph = [Deploy("org.test.d", ["org.test.b", "org.test.c"]),
                Deploy("org.test.c", ["org.test.a"]), Deploy("org.test.b", ["org.test.a"]),
                Deploy("org.test.a"), Deploy("org.test.aa")];
            for (var repeat = 0; repeat < 100; repeat++)
                for (var permutation = 0; permutation < 10; permutation++)
                {
                    var remaining = graph.ToList();
                    var shuffled = new List<ModuleDeployment>();
                    var rank = permutation;
                    while (remaining.Count > 0)
                    {
                        var index = rank % remaining.Count;
                        rank /= remaining.Count;
                        shuffled.Add(remaining[index]);
                        remaining.RemoveAt(index);
                    }
                    Order(Resolve(shuffled,
                        "org.test.a", "org.test.aa", "org.test.b", "org.test.c", "org.test.d"),
                        "org.test.a", "org.test.aa", "org.test.b", "org.test.c", "org.test.d");
                }
        }
        finally { CultureInfo.CurrentCulture = before; }
    }

    [Fact]
    public void EveryDeployedManifestIsValidatedEvenDisabledAndSelfCyclesStayManifestFailures()
    {
        var invalid = new ModuleDeployment("{\"secret\":123}"u8);
        var result = Reject(Resolve([invalid]), "composition_manifest_rejected");
        Assert.NotNull(result.Diagnostics[0].ManifestDiagnostic);
        var self = Reject(Resolve([Deploy("org.test.a", ["org.test.a"])], "org.test.a"),
            "composition_manifest_rejected");
        Assert.Equal("dependency_self", self.Diagnostics[0].ManifestDiagnostic!.Code);
        Assert.DoesNotContain("secret", JsonSerializer.Serialize(result));
    }

    [Fact]
    public void MissingDuplicateAndUnknownSelectionsDoNotBecomeEmptySuccess()
    {
        Reject(Resolve([], "org.test.a"), "composition_implementation_missing");
        Reject(Resolve([Deploy("org.test.a"), Deploy("org.test.a")], "org.test.a"), "composition_module_duplicate");
        Reject(Resolve([], "PRIVATE/secret"), "composition_configuration_invalid");
        Reject(CompositionResolver.Resolve([], [new("org.test.a", false)], new("1.0.0"), TestContext.Current.CancellationToken),
            "composition_configuration_invalid");
    }

    [Fact]
    public void MissingDisabledAndIncompatibleDependenciesHaveDistinctCodes()
    {
        Reject(Resolve([Deploy("org.test.a", ["org.test.b"])], "org.test.a"), "composition_dependency_missing");
        Reject(Resolve([Deploy("org.test.a", ["org.test.b"]), Deploy("org.test.b")], "org.test.a"),
            "composition_dependency_disabled");
        Reject(Resolve([Deploy("org.test.a", ["org.test.b"]), Deploy("org.test.b", version: "2.0.0")],
            "org.test.a", "org.test.b"), "composition_dependency_incompatible");
        Order(Resolve([Deploy("org.test.a", ["org.test.b"]), Deploy("org.test.b", version: "1.0.0+build")],
            "org.test.a", "org.test.b"), "org.test.b", "org.test.a");
        Reject(Resolve([Deploy("org.test.a", ["org.test.b"], range: ">=1.0.0 <2.0.0"),
            Deploy("org.test.b", version: "1.1.0-alpha")], "org.test.a", "org.test.b"),
            "composition_dependency_incompatible");
        Order(Resolve([Deploy("org.test.a", ["org.test.b"], range: ">=1.1.0-alpha <2.0.0"),
            Deploy("org.test.b", version: "1.1.0-beta")], "org.test.a", "org.test.b"),
            "org.test.b", "org.test.a");
    }

    [Fact]
    public void OneDiagnosticPerStronglyConnectedComponentNotPerDependentOrPath()
    {
        ModuleDeployment[] graph = [Deploy("org.test.a", ["org.test.b"]), Deploy("org.test.b", ["org.test.a"]),
            Deploy("org.test.c", ["org.test.d"]), Deploy("org.test.d", ["org.test.c"]),
            Deploy("org.test.e", ["org.test.a"])];
        var result = Reject(Resolve(graph, "org.test.a", "org.test.b", "org.test.c", "org.test.d", "org.test.e"),
            "composition_dependency_cycle", "composition_dependency_cycle");
        Assert.Equal(["org.test.a", "org.test.b"], result.Diagnostics[0].RelatedIds.Select(i => i.Value));
        Assert.Equal(["org.test.c", "org.test.d"], result.Diagnostics[1].RelatedIds.Select(i => i.Value));
        Order(Resolve([Deploy("org.test.a"), Deploy("org.test.b", ["org.test.a"])], "org.test.a", "org.test.b"),
            "org.test.a", "org.test.b");
    }

    [Fact]
    public void StructureGateAllowsSupervisedSettingsBeforeClaimsWithoutReturningPartialAcceptance()
    {
        ModuleDeployment[] catalog = [Deploy("org.test.a",
            claims: [new("org.test.a", "route", "read", Method: "GET", Route: "/health")], capabilities: ["backend"])];
        Assert.Empty(CompositionResolver.ValidateStructure(catalog, [new("org.test.a", true)], new("1.0.0"),
            TestContext.Current.CancellationToken));
        Reject(Resolve(catalog, "org.test.a"), "composition_contribution_conflict");
        var invalid = CompositionResolver.ValidateStructure([Deploy("org.test.a", ["org.test.b"])],
            [new("org.test.a", true)], new("1.0.0"), TestContext.Current.CancellationToken);
        Assert.Equal("composition_dependency_missing", Assert.Single(invalid).Code);
    }

    [Theory]
    [InlineData(null, "host_context_required")]
    [InlineData("1.0", "host_context_invalid")]
    public void InvalidHostContextRetainsAcceptedManifestDiagnosticSemantics(string? host, string expected)
    {
        var context = host is null ? null : new ManifestValidationContext(host);
        var rejected = Reject(CompositionResolver.Resolve([Deploy("org.test.a")], [], context!,
            TestContext.Current.CancellationToken), "composition_manifest_rejected");
        Assert.Equal(expected, Assert.Single(rejected.Diagnostics).ManifestDiagnostic!.Code);
    }
}
