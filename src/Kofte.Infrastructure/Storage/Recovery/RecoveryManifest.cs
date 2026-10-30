using System.Text.Json.Serialization;
using Kofte.Infrastructure.Identity;

namespace Kofte.Infrastructure.Storage.Recovery;

public sealed record RecoveryArtifact(string Name, long Length, string Sha256);

public sealed record RecoveryMigration(string Id, string ProductVersion);

public sealed record RecoveryAllocation(
    string ModuleId, bool Relational, string? AllocationKey, Guid? AllocationId,
    int? AllocatorVersion, int? MarkerVersion, string State, string MigrationState,
    string? MigrationTarget, string? MigrationCheckpoint, long Revision,
    IReadOnlyList<RecoveryMigration> History);

public sealed record RecoveryInventory(Guid InstallationId, string Provider, int RegistryVersion,
    long RegistryRevision, IReadOnlyList<RecoveryAllocation> Allocations);

public sealed record RecoveryManifest(
    string Format, bool Complete, Guid RecoverySetId, Guid InstallationId,
    string Provider, int RegistryVersion, long RegistryRevision,
    IReadOnlyList<RecoveryAllocation> Allocations, IReadOnlyList<RecoveryArtifact> Artifacts,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IdentityRecoverySummary? Identity = null)
{
    public const string CurrentFormat = "kofte-storage-recovery-v1";
    public const string IdentityFormat = "kofte-storage-recovery-v2";
    public const string FileName = "recovery.json";
}
