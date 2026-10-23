using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Storage;

public sealed class StorageException : Exception
{
    public StorageCode Code { get; }
    internal StorageException(StorageCode code) : base(code.ToString()) => Code = code;
}
