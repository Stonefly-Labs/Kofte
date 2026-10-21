namespace Kofte.Platform.ContractTests.Modules.Manifest;

public sealed class IdentityContractTests
{
    [Fact]
    public async Task IdentityCorpusUsesBothPublicConsumers()
    {
        foreach (var item in ManifestCorpus.Cases().Where(c => c.Name.StartsWith("C09", StringComparison.Ordinal)
            || c.Name is "C10" or "C11" or "C12"))
        {
            ConsumerHarness.Expected(item, ConsumerHarness.Host(item));
            var output = await ConsumerHarness.Author(item.Bytes, ConsumerHarness.Arguments(item));
            Assert.Equal(item.Code is null ? 0 : 2, output.ExitCode);
            ConsumerHarness.Expected(item, ConsumerHarness.Parse(output.Stdout));
        }
    }
}
