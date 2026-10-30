using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Identity;

internal static class IdentityMaintenance
{
    internal static Task<int> RunAsync(StorageInstallation installation, MaintenanceScope scope,
        Guid installationId, string mode, string trustDigest, DateTime now, OperationBudget budget)
    {
        if (now.Kind != DateTimeKind.Utc) throw new StorageException(StorageCode.RequestInvalid);
        return IdentityStore.MutateStoppedAsync(installation, scope, installationId, mode, trustDigest, false,
            budget, async change =>
            {
                var events = await IdentityEventStore.PruneAsync(change.Database, now);
                var attempts = await IdentityLoginAttempts.PruneAsync(change.Database, now);
                var windows = await IdentityAdmission.PruneAsync(change.Database, now);
                return checked(events + attempts + windows);
            });
    }
}
