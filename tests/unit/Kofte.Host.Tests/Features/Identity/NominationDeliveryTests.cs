using Kofte.Host.Features.IdentityMaintenance;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;
using Kofte.TestSupport;
using Xunit;

namespace Kofte.Host.Tests.Features.Identity;

public sealed class NominationDeliveryTests
{
    [Fact]
    public async Task DeliveredPrivateAuthorizationIsCompleteAndCannotOverwriteAnExistingFile()
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        using var root = new OwnedTestRoot();
        Directory.CreateDirectory(root.Path, UnixStorePaths.DirectoryMode);
        var path = Path.Combine(root.Path, "authorization");
        var secret = new string('A', 43);
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
        await IdentityNomination.DeliverAsync(path, secret, budget);
        Assert.Equal(secret, await File.ReadAllTextAsync(path, budget.Token));
        Assert.Equal(UnixStorePaths.FileMode, File.GetUnixFileMode(path));
        await Assert.ThrowsAsync<IOException>(() => IdentityNomination.DeliverAsync(path, new string('Q', 43), budget));
        Assert.Equal(secret, await File.ReadAllTextAsync(path, budget.Token));
    }
}
