using System.Data.Common;
using System.Runtime.CompilerServices;
using Kofte.Platform.Modules.Storage;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kofte.Infrastructure.Storage.Migrations;

internal sealed class AssignedCommandInterceptor(ModuleStorageBinding binding) : DbCommandInterceptor
{
    private readonly ConditionalWeakTable<DbCommand, OperationBudget> attempts = new();

    private void Verify(DbCommand command, CommandEventData eventData) =>
        VerifyAsync(command, eventData, CancellationToken.None).GetAwaiter().GetResult();

    private async Task VerifyAsync(DbCommand command, CommandEventData eventData, CancellationToken token)
    {
        var budget = new OperationBudget(OperationBudget.Resolve, token);
        attempts.Add(command, budget);
        try
        {
            await binding.VerifyCommandAsync(command, budget,
                eventData.Context is AssignedDbContext { IsMigrationContext: true });
            if (eventData.Context is AssignedDbContext { IsMigrationContext: true } context &&
                eventData.CommandSource == CommandSource.Migrations)
            {
                if (command.CommandText.Contains("INSERT INTO ", StringComparison.Ordinal) &&
                    command.CommandText.Contains("\"__EFMigrationsHistory\"", StringComparison.Ordinal))
                    context.AppliedMigrationTransaction = command.Transaction;
            }
        }
        catch
        {
            Finish(command, checkDeadline: false);
            throw;
        }
    }

    private void Finish(DbCommand command, bool checkDeadline = true)
    {
        if (!attempts.TryGetValue(command, out var budget)) return;
        attempts.Remove(command);
        using (budget)
        {
            if (!checkDeadline) return;
            try { budget.Check(); }
            catch (OperationCanceledException) when (!budget.CallerCancelled)
            { throw new StorageException(StorageCode.DeadlineExceeded); }
        }
    }

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
        await VerifyAsync(command, eventData, cancellationToken);
        return result;
    }

    public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await VerifyAsync(command, eventData, cancellationToken);
        return result;
    }

    public override async ValueTask<InterceptionResult<object>> ScalarExecutingAsync(DbCommand command,
        CommandEventData eventData, InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        await VerifyAsync(command, eventData, cancellationToken);
        return result;
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData,
        DbDataReader result)
    {
        try { Finish(command); return result; }
        catch { result.Dispose(); throw; }
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command,
        CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        try { Finish(command); return result; }
        catch { await result.DisposeAsync(); throw; }
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    { Finish(command); return result; }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
        int result, CancellationToken cancellationToken = default)
    { Finish(command); return ValueTask.FromResult(result); }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    { Finish(command); return result; }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
        object? result, CancellationToken cancellationToken = default)
    { Finish(command); return ValueTask.FromResult(result); }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData) =>
        Finish(command, checkDeadline: false);

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData,
        CancellationToken cancellationToken = default)
    { Finish(command, checkDeadline: false); return Task.CompletedTask; }

    public override void CommandCanceled(DbCommand command, CommandEndEventData eventData) =>
        Finish(command, checkDeadline: false);

    public override Task CommandCanceledAsync(DbCommand command, CommandEndEventData eventData,
        CancellationToken cancellationToken = default)
    { Finish(command, checkDeadline: false); return Task.CompletedTask; }
}
