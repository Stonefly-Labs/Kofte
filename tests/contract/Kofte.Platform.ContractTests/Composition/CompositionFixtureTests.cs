using System.Text;
using Kofte.Infrastructure.Storage;
using Kofte.TestSupport;

namespace Kofte.Platform.ContractTests.Composition;

public sealed class CompositionFixtureTests
{
    [Fact]
    [Trait("Provider", "sqlite")]
    public async Task SelectionReplacementIsPrivateAndOwnedWithoutLeakingFixtureResources()
    {
        var fixture = await CompositionFixture.CreateAsync("sqlite", TestContext.Current.CancellationToken);
        var selection = fixture.SelectionPath;
        var root = fixture.Storage.DataRoot;
        try
        {
            await fixture.WriteSelectionAsync(Encoding.UTF8.GetBytes(
                """{"schemaVersion":"1","modules":[]}"""), TestContext.Current.CancellationToken);
            UnixStorePaths.ValidateRoot(Path.GetDirectoryName(selection)!, allowMissing: false);
            Assert.True(UnixStorePaths.ValidateFile(selection));
            await fixture.WriteSelectionAsync(Encoding.UTF8.GetBytes(
                """{"schemaVersion":"1","modules":[{"id":"org.example.alpha","enabled":false,"settings":{}}]}"""),
                TestContext.Current.CancellationToken);
            Assert.Contains("\"enabled\":false", await File.ReadAllTextAsync(selection,
                TestContext.Current.CancellationToken));
            Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(selection)!, "modules.next")));
            Assert.Throws<InvalidOperationException>(() => fixture.Start("private-canary"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.WriteSelectionAsync(
                new byte[1_048_577], TestContext.Current.CancellationToken));
        }
        finally { await fixture.DisposeAsync(); }
        Assert.False(File.Exists(selection));
        Assert.False(Directory.Exists(root));
    }
}
