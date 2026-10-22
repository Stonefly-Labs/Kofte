using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.ContractTests.Modules.Manifest;

public sealed class RetryTests
{
    [Fact]
    public async Task EveryCorpusRejectionCanBeCorrectedWithoutStateResetOrEarlierMutation()
    {
        var original = Assert.IsType<ManifestValidationResult.Accepted>(
            ManifestValidator.Validate(ManifestCorpus.Minimal, new("1.0.0"))).Description;
        foreach (var item in ManifestCorpus.Cases().Where(c => !c.IsAccepted))
        {
            ConsumerHarness.Expected(item, ConsumerHarness.Host(item));
            // Correct erroneous context as well as input; otherwise context errors must remain errors.
            var corrected = new ManifestCase("corrected", ManifestCorpus.Minimal);
            ConsumerHarness.Expected(corrected, ConsumerHarness.Host(corrected));
            var output = await ConsumerHarness.Author(corrected.Bytes, ConsumerHarness.Arguments(corrected));
            Assert.Equal(0, output.ExitCode);
            ConsumerHarness.Expected(corrected, ConsumerHarness.Parse(output.Stdout));
            Assert.Equal("org.example.notes", original.Id.Value);
            Assert.Empty(original.Dependencies);
        }
    }
}
