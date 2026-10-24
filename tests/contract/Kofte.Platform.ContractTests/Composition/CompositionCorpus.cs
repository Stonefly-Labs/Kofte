using System.Globalization;
using System.Text.Json;
using Kofte.Composition.TestModule;
using Kofte.Platform.Modules.Composition;

namespace Kofte.Platform.ContractTests.Composition;

public sealed class CompositionCorpus
{
    public static IReadOnlyList<CompositionCorpusCase> Cases => FrozenCompositionCorpus.Cases;

    public static IReadOnlyList<ModuleDeployment> Permute(
        IReadOnlyList<ModuleDeployment> deployments, int permutation) =>
        FrozenCompositionCorpus.Permute(deployments, permutation);

    [Theory]
    [InlineData("en-US")]
    [InlineData("tr-TR")]
    public void FrozenPublicCorpusHasIdenticalDecisionsAcrossRepeatedDiscoveryPermutations(string culture)
    {
        var prior = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            foreach (var fixture in Cases)
            {
                string? firstDiagnostics = null;
                for (var repetition = 0; repetition < 100; repetition++)
                    for (var permutation = 0; permutation < 10; permutation++)
                    {
                        var shuffled = Permute(fixture.Deployments, permutation);
                        var result = CompositionResolver.Resolve(shuffled, fixture.Selection.Reverse(), new("1.0.0"),
                            TestContext.Current.CancellationToken);
                        if (fixture.ExpectedCodes.Count == 0)
                        {
                            var accepted = Assert.IsType<CompositionResolution.Accepted>(result);
                            Assert.Equal(fixture.ExpectedOrder, accepted.Composition.Order.Select(id => id.Value));
                        }
                        else
                        {
                            var rejected = Assert.IsType<CompositionResolution.Rejected>(result);
                            Assert.Equal(fixture.ExpectedCodes, rejected.Diagnostics.Select(d => d.Code));
                            Assert.Equal(fixture.ExpectedOwners, rejected.Diagnostics.Select(d => d.Owner?.Value));
                            Assert.Equal(fixture.ExpectedRelated.Select(ids => string.Join(',', ids)),
                                rejected.Diagnostics.Select(d => string.Join(',', d.RelatedIds.Select(id => id.Value))));
                            Assert.Equal(fixture.ExpectedManifestCodes,
                                rejected.Diagnostics.Where(d => d.ManifestDiagnostic is not null).Select(d => d.ManifestDiagnostic!.Code));
                            Assert.Equal(fixture.ExpectedManifestPaths,
                                rejected.Diagnostics.Where(d => d.ManifestDiagnostic is not null).Select(d => d.ManifestDiagnostic!.Path));
                            var diagnostics = JsonSerializer.Serialize(rejected.Diagnostics);
                            firstDiagnostics ??= diagnostics;
                            Assert.Equal(firstDiagnostics, diagnostics);
                            Assert.DoesNotContain("PRIVATE/path", diagnostics);
                        }
                    }
            }
        }
        finally { CultureInfo.CurrentCulture = prior; }
    }
}
