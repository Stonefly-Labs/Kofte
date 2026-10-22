namespace Kofte.Infrastructure.Storage;

public enum StorePathError { ConfigurationInvalid, StorageMissing, StorageInvalid, StorageUnavailable, StoreBusy }

public sealed class StorePathException(StorePathError code) : Exception(code.ToString())
{
    public StorePathError Code { get; } = code;
}
