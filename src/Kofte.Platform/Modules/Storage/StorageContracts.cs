using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.Modules.Storage;

public interface IStorageRegistry
{
    Task<StorageResult> RegisterAsync(ModuleDescription module, IStorageMaintenanceScope scope,
        CancellationToken cancellationToken = default);
    Task<StorageResult> ResolveAsync(ModuleId owner, CancellationToken cancellationToken = default);
}

public interface IModuleStorage : IAsyncDisposable
{
    ModuleId Owner { get; }
    Task<StorageResult> ResolveAsync(CancellationToken cancellationToken = default);
}

public interface IStorageMaintenance
{
    Task<IStorageMaintenanceScope> AcquireAsync(CancellationToken cancellationToken = default);
    Task UpgradeAsync(IStorageMaintenanceScope scope,
        CancellationToken cancellationToken = default);
}

public interface IStorageMaintenanceScope : IAsyncDisposable
{
    CancellationToken Lifetime { get; }
}

public sealed class StorageAssignment
{
    public int ContractVersion => 1;
    public ModuleId Owner { get; }
    public string Provider { get; }
    public Guid InstallationId { get; }
    public Guid AllocationId { get; }
    public string AllocationKey { get; }
    public int AllocatorVersion { get; }
    public long Revision { get; }

    internal StorageAssignment(ModuleId owner, string provider, Guid installationId,
        Guid allocationId, string allocationKey, int allocatorVersion, long revision) =>
        (Owner, Provider, InstallationId, AllocationId, AllocationKey, AllocatorVersion, Revision) =
        (owner, provider, installationId, allocationId, allocationKey, allocatorVersion, revision);
}

public enum StorageOutcome { Ready, NoStorage, Busy, Rejected, Incomplete, ReconciliationRequired, Failed }
public enum StorageOperation { Resolve, Register, Upgrade, Migrate, Recover, Acquire }
public enum StorageDurability { Unchanged, Committed, Uncertain }
public enum StorageCode
{
    Ready, NoStorage, Busy, Conflict, TargetReserved, PathRejected, OwnerMismatch, Missing,
    Corrupt, Incomplete, ReconciliationRequired, VersionUnsupported, PrivilegeDenied,
    Unavailable, DeadlineExceeded, Cancelled, RequestInvalid
}

public sealed record StorageDiagnostic(string Code, string Message, string Guidance);

public abstract class StorageResult
{
    public int ContractVersion => 1;
    public StorageOutcome Outcome { get; }
    public StorageOperation Operation { get; }
    public string Provider { get; }
    public string? ModuleId { get; }
    public string CorrelationId { get; }
    public StorageDurability Durability { get; }
    public IReadOnlyList<StorageDiagnostic> Diagnostics { get; }

    private protected StorageResult(StorageOutcome outcome, StorageOperation operation,
        string provider, ModuleId? owner, string correlation, StorageDurability durability,
        StorageDiagnostic diagnostic)
    {
        Outcome = outcome;
        Operation = operation;
        Provider = provider;
        ModuleId = owner?.Value;
        CorrelationId = correlation;
        Durability = durability;
        Diagnostics = Array.AsReadOnly([diagnostic]);
    }

    public sealed class Ready : StorageResult
    {
        public StorageAssignment Assignment { get; }
        internal Ready(StorageOperation operation, StorageAssignment assignment, string correlation,
            StorageDurability durability, StorageDiagnostic diagnostic)
            : base(StorageOutcome.Ready, operation, assignment.Provider, assignment.Owner,
                correlation, durability, diagnostic) => Assignment = assignment;
    }

    public sealed class NoStorage : StorageResult
    {
        internal NoStorage(StorageOperation operation, string provider, ModuleId owner,
            string correlation, StorageDurability durability, StorageDiagnostic diagnostic)
            : base(StorageOutcome.NoStorage, operation, provider, owner, correlation, durability, diagnostic) { }
    }

    public sealed class Failure : StorageResult
    {
        internal Failure(StorageOutcome outcome, StorageOperation operation, string provider,
            ModuleId? owner, string correlation, StorageDurability durability, StorageDiagnostic diagnostic)
            : base(outcome, operation, provider, owner, correlation, durability, diagnostic)
        {
            if (outcome is StorageOutcome.Ready or StorageOutcome.NoStorage)
                throw new ArgumentOutOfRangeException(nameof(outcome));
        }
    }
}
