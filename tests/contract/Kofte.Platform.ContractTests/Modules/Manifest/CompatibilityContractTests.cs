namespace Kofte.Platform.ContractTests.Modules.Manifest;

public sealed class CompatibilityContractTests
{
    [Fact]
    public async Task CompleteBoundaryAndDeclarationCorpusUsesPublicConsumers()
    {
        foreach (var item in ManifestCorpus.Cases().Where(c => c.Name.StartsWith("range-", StringComparison.Ordinal)
            || int.TryParse(c.Name.AsSpan(1, Math.Min(2, c.Name.Length - 1)), out var id) && id is >= 13 and <= 28))
        {
            ConsumerHarness.Expected(item, ConsumerHarness.Host(item));
            var output = await ConsumerHarness.Author(item.Bytes, ConsumerHarness.Arguments(item));
            Assert.Equal(item.Code is null ? 0 : 2, output.ExitCode);
            ConsumerHarness.Expected(item, ConsumerHarness.Parse(output.Stdout));
        }
    }
}
