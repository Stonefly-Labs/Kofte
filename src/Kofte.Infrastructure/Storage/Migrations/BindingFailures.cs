using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Storage.Migrations;

internal static class BindingFailures
{
    internal static async Task<T> RunAsync<T>(Func<Task<T>> action)
    {
        try { return await action(); }
        catch (StorageException) { throw; }
        catch (OperationCanceledException error)
        { throw new OperationCanceledException(error.CancellationToken); }
        catch (StorePathException error)
        {
            throw new StorageException(error.Code == StorePathError.StorageMissing ? StorageCode.Missing
                : error.Code == StorePathError.StoreBusy ? StorageCode.Busy : StorageCode.PathRejected);
        }
        catch (Exception) { throw new StorageException(StorageCode.Unavailable); }
    }
}
