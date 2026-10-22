using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kofte.Infrastructure.Storage.Migrations;

internal sealed class AssignedCommandInterceptor(ModuleStorageBinding binding) : DbCommandInterceptor
{
    private void Verify(DbCommand command, CommandEventData eventData) =>
        binding.VerifyCommandAsync(command, CancellationToken.None,
            eventData.Context is AssignedDbContext { IsMigrationContext: true }).GetAwaiter().GetResult();

    public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result)
    { Verify(command, eventData); return result; }

    public override InterceptionResult<int> NonQueryExecuting(DbCommand command,
        CommandEventData eventData, InterceptionResult<int> result)
    { Verify(command, eventData); return result; }

    public override InterceptionResult<object> ScalarExecuting(DbCommand command,
        CommandEventData eventData, InterceptionResult<object> result)
    { Verify(command, eventData); return result; }

    public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        await binding.VerifyCommandAsync(command, cancellationToken,
            eventData.Context is AssignedDbContext { IsMigrationContext: true });
        return result;
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await binding.VerifyCommandAsync(command, cancellationToken,
            eventData.Context is AssignedDbContext { IsMigrationContext: true });
        return result;
    }

    public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        await binding.VerifyCommandAsync(command, cancellationToken,
            eventData.Context is AssignedDbContext { IsMigrationContext: true });
        return result;
    }
}
