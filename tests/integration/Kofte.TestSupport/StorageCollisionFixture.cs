using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;

namespace Kofte.TestSupport;

public static class StorageCollisionFixture
{
    public static Task<StorageResult> RegisterAtExistingCandidateAsync(StorageInstallation installation,
        ModuleDescription contender, StorageAssignment existing, IStorageMaintenanceScope scope, CancellationToken token)
    {
        if (contender.Id.Equals(existing.Owner))
            throw new InvalidOperationException("storage_collision_distinct_owner_required");
        return new StorageRegistry(installation, _ => existing.AllocationKey)
            .RegisterAsync(contender, scope, token);
    }
}
