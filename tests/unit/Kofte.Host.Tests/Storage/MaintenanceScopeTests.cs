using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;
using Xunit;

namespace Kofte.Host.Tests.Storage;

public sealed class MaintenanceScopeTests
{
    [Fact]
    public async Task ForeignDisposedAndCancelledScopesCannotBeReused()
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("storage_target_unsupported");
        var root = Path.Combine(OperatingSystem.IsMacOS() ? "/private/tmp" : Path.GetTempPath(),
            "kofte-scope-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root, UnixStorePaths.DirectoryMode);
        try
        {
            var installation = StorageInstallation.Sqlite(root);
            var scope = await MaintenanceScope.AcquireAsync(installation, TestContext.Current.CancellationToken);
            using var budget = new OperationBudget(OperationBudget.Resolve, TestContext.Current.CancellationToken);
            await scope.VerifyAsync(installation, budget);
            await Assert.ThrowsAsync<StorageException>(() =>
                scope.VerifyAsync(StorageInstallation.Sqlite(root), budget));
            await scope.DisposeAsync();
            await Assert.ThrowsAsync<StorageException>(() => scope.VerifyAsync(installation, budget));
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                MaintenanceScope.AcquireAsync(installation, cancelled.Token));
            await using var reacquired = await MaintenanceScope.AcquireAsync(installation,
                TestContext.Current.CancellationToken);
            await reacquired.VerifyAsync(installation, budget);
            await reacquired.DisposeAsync();
            using var ownerLifetime = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            await using var owned = await MaintenanceScope.AcquireAsync(installation, ownerLifetime.Token);
            ownerLifetime.Cancel();
            var failure = await Assert.ThrowsAsync<StorageException>(() => owned.VerifyAsync(installation, budget));
            Assert.Equal(StorageCode.Cancelled, failure.Code);
            using var contender = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            contender.CancelAfter(TimeSpan.FromMilliseconds(100));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                MaintenanceScope.AcquireAsync(installation, contender.Token));
            await owned.DisposeAsync();
            await using var released = await MaintenanceScope.AcquireAsync(installation,
                TestContext.Current.CancellationToken);
            await released.VerifyAsync(installation, budget);
        }
        finally { Directory.Delete(root, recursive: true); }
    }
}
