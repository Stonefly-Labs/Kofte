using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Composition;

public sealed class CompositionStorageException : Exception
{
    public string Code { get; }
    public StorageResult? StorageFailure { get; }

    internal CompositionStorageException(string code, StorageResult? failure = null) : base(code)
        => (Code, StorageFailure) = (code, failure);
}
