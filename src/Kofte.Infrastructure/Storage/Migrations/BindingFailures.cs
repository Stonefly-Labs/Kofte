using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Storage.Migrations;

internal static class BindingFailures
{
    internal static async Task<T> RunAsync<T>(string provider, ModuleId owner,
        Func<OperationBudget, Task<(T Value, StorageAssignment Assignment)>> action, CancellationToken token)
    {
        T? value = default;
        var result = await StorageDiagnostics.RunAsync(StorageOperation.Resolve, provider,
            owner, OperationBudget.Resolve, token, async (budget, _, correlation) =>
        {
            var verified = await action(budget);
            value = verified.Value;
            return new StorageResult.Ready(StorageOperation.Resolve, verified.Assignment, correlation,
                StorageDurability.Unchanged, StorageDiagnostics.Diagnostic(StorageCode.Ready));
        });
        if (result is StorageResult.Failure)
            throw new StorageException(Enum.GetValues<StorageCode>().Single(code =>
                StorageDiagnostics.Diagnostic(code).Code == result.Diagnostics[0].Code));
        return value!;
    }
}
