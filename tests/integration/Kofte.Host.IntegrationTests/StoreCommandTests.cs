using Kofte.Host.Bootstrap;
using Xunit;

namespace Kofte.Host.IntegrationTests;

[Trait("Provider", "sqlite")]
[Trait("Suite", "recovery")]
public sealed class StoreCommandTests
{
    [Fact]
    public async Task ActualCliRequiresExplicitLifecycleArgumentsAndNeverInitializesDuringStartup()
    {
        var token = TestContext.Current.CancellationToken;
        using var root = new TempRoot();
        using var backups = new TempRoot();
        using var restoredRoot = new TempRoot();
        var missing = await StartupFixture.RunExitAsync(root.Path, [], token);
        Assert.Equal(1, missing.ExitCode);
        Assert.Contains("storage_missing", missing.Output);
        Assert.False(Directory.Exists(root.Path));
        var initialized = await StartupFixture.RunExitAsync(root.Path, ["--store", "init"], token);
        Assert.Equal(0, initialized.ExitCode);
        var store = new SqlitePlatformStore(root.Path);
        var original = await File.ReadAllBytesAsync(store.DatabasePath, token);
        var repeated = await StartupFixture.RunExitAsync(root.Path, ["--store", "init"], token);
        Assert.Equal(1, repeated.ExitCode);
        var unconfirmed = await StartupFixture.RunExitAsync(root.Path, ["--store", "reset"], token);
        Assert.Equal(1, unconfirmed.ExitCode);
        Assert.Contains("confirmation_required", unconfirmed.Output);
        var invalid = await StartupFixture.RunExitAsync(root.Path,
            ["--store", "reset", "--confirm-delete-platform-data", "--unexpected"], token);
        Assert.Equal(1, invalid.ExitCode);
        Assert.Contains("command_invalid", invalid.Output);
        var preserved = await File.ReadAllBytesAsync(store.DatabasePath, token);
        Assert.True(original.AsSpan().SequenceEqual(preserved));
        var backup = Path.Combine(backups.Path, "platform.db");
        Assert.Equal(0, (await StartupFixture.RunExitAsync(root.Path,
            ["--store", "backup", "--output", backup], token)).ExitCode);
        Assert.Equal(0, (await StartupFixture.RunExitAsync(restoredRoot.Path,
            ["--store", "restore", "--input", backup], token)).ExitCode);
        await new SqlitePlatformStore(restoredRoot.Path).ProbeAsync(token);
        Assert.Equal(0, (await StartupFixture.RunExitAsync(root.Path,
            ["--store", "reset", "--confirm-delete-platform-data"], token)).ExitCode);
        Assert.False(File.Exists(store.DatabasePath));
    }
}
