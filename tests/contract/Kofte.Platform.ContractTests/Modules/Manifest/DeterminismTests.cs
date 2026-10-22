using System.Globalization;

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
            var cases = ManifestCorpus.Cases();
            foreach (var item in cases)
            {
                for (var repetition = 0; repetition < 100; repetition++)
                    ConsumerHarness.Expected(item, ConsumerHarness.Host(item));
                var output = await ConsumerHarness.Author(item.Bytes, ConsumerHarness.Arguments(item, name, 100));
                Assert.Equal(item.IsAccepted ? 0 : 2, output.ExitCode);
                Assert.Equal("culture_verified:" + name + ":" + decimalText, output.Stderr.Trim());
                var decisions = output.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                Assert.Equal(100, decisions.Length);
                foreach (var decision in decisions) ConsumerHarness.Expected(item, ConsumerHarness.Parse(decision));
            }
            output.WriteLine($"culture={name}; cases={cases.Count}; repetitions=100; consumers=2");
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
        foreach (var item in ManifestCorpus.CollectedDiagnosticCases().Where(c =>
            c.Name is "collected" or "collected-reversed"))
        {
            ConsumerHarness.Expected(item, ConsumerHarness.Host(item));
            var author = await ConsumerHarness.Author(item.Bytes, ConsumerHarness.Arguments(item));
            Assert.Equal(2, author.ExitCode);
            ConsumerHarness.Expected(item, ConsumerHarness.Parse(author.Stdout));
        }
    }
}
