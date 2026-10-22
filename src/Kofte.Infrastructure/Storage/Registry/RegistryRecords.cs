using Kofte.Infrastructure.Storage.Allocation;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Storage.Registry;

internal sealed record InstallationRecord(Guid Id, string Provider, int Version, long Revision);

internal sealed record RegistrationRecord(ModuleId Owner, bool Relational, string Provider,
    string? Key, int? AllocatorVersion, Guid? AllocationId, string State,
    string MigrationState, string? Target, string? Checkpoint, string? LastCode, long Revision)
{
    internal StorageAssignment Assignment(InstallationRecord installation) => new(Owner, Provider,
        installation.Id, AllocationId ?? throw new StorageException(StorageCode.Corrupt),
        Key ?? throw new StorageException(StorageCode.Corrupt),
        AllocatorVersion ?? throw new StorageException(StorageCode.Corrupt), Revision);

    internal void Validate(InstallationRecord installation)
    {
        if (Provider != installation.Provider || installation.Version != 1 || installation.Id == Guid.Empty ||
            installation.Revision < 0 || Revision < 1 ||
            State is not ("no_storage" or "reserved" or "ready" or "reconciliation_required") ||
            MigrationState is not ("none" or "in_progress" or "current" or "recovery_required") ||
            !MigrationIdValid(Target) || !MigrationIdValid(Checkpoint) || !StorageDiagnostics.IsFailureCode(LastCode))
            throw new StorageException(StorageCode.Corrupt);
        if (Relational)
        {
            if (Key is null || AllocationId is null || AllocationId == Guid.Empty ||
                AllocatorVersion != 1 || State == "no_storage")
                throw new StorageException(StorageCode.Corrupt);
            StorageAllocator.Validate(Key);
        }
        else if (Key is not null || AllocationId is not null || AllocatorVersion is not null ||
            State != "no_storage" || MigrationState != "none" || Target is not null || Checkpoint is not null)
            throw new StorageException(StorageCode.Corrupt);
    }

    private static bool MigrationIdValid(string? value) => value is null ||
        value.Length is > 0 and <= 150 && value.All(c => c is >= ' ' and <= '~');
}
