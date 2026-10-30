using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

[Trait("Provider", "sqlite")]
public sealed class IdentityFixtureTests
{
    [Fact]
    public async Task PrivateNativeHookAdmissionHoldsActualChildAndRequiresExplicitRelease()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await IdentityFixture.CreateAsync("sqlite", token);
        var barrier = new IdentityCommitBarrier(fixture.PrivateDirectory, "fixture_ready");
        await using var process = fixture.Start(["--fixture-probe"], barrier: barrier);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
        budget.CancelAfter(TimeSpan.FromSeconds(10));
        await barrier.WaitAsync(budget.Token);
        Assert.Equal(process.ProcessId, barrier.ObservedProcessId);
        Assert.False(process.HasExited);
        Assert.Throws<InvalidOperationException>(fixture.RequireStopped);
        barrier.Release();
        var output = await process.WaitAsync(budget.Token);
        Assert.Equal(0, output.ExitCode);
        Assert.Empty(output.Stdout);
        Assert.Empty(output.Stderr);
        Assert.True(process.ExitVerified && process.DescendantExitsVerified);
    }

    [Fact]
    public async Task DisposalVerifiesEveryOwnedChildExitedBeforeDeletingItsPrivateState()
    {
        var token = TestContext.Current.CancellationToken;
        var fixture = await IdentityFixture.CreateAsync("sqlite", token);
        await using (fixture)
        {
            var barrier = new IdentityCommitBarrier(fixture.PrivateDirectory, "fixture_ready");
            var process = fixture.Start(["--fixture-idle"], barrier: barrier);
            using var budget = CancellationTokenSource.CreateLinkedTokenSource(token);
            budget.CancelAfter(TimeSpan.FromSeconds(10));
            await barrier.WaitAsync(budget.Token);
            Assert.Equal(process.ProcessId, barrier.ObservedProcessId);
            barrier.Release();
            Assert.False(process.HasExited);
        }
        Assert.True(fixture.AllExitsVerified);
        Assert.False(Directory.Exists(fixture.PrivateDirectory));
        Assert.False(Directory.Exists(fixture.Storage.DataRoot));
    }
}
