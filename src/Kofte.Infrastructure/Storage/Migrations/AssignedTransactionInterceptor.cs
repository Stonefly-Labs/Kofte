using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kofte.Infrastructure.Storage.Migrations;

internal sealed class AssignedTransactionInterceptor(ModuleStorageBinding binding) : DbTransactionInterceptor
{
    private void Checkpoint(string phase, DbContextEventData data, DbTransaction? transaction = null)
    {
        // EF's history-table initialization is not a module migration commit.
        if (data.Context is AssignedDbContext { IsMigrationContext: true } context &&
            (transaction is null || ReferenceEquals(context.AppliedMigrationTransaction, transaction)))
            StorageDiagnostics.Checkpoint(phase, binding.Provider, binding.Owner);
    }

    public override InterceptionResult<DbTransaction> TransactionStarting(DbConnection connection,
        TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result)
    { Checkpoint("migration_transaction_starting", eventData); return result; }

    public override ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection,
        TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result,
        CancellationToken cancellationToken = default)
    { Checkpoint("migration_transaction_starting", eventData); return ValueTask.FromResult(result); }

    public override InterceptionResult TransactionCommitting(DbTransaction transaction,
        TransactionEventData eventData, InterceptionResult result)
    { Checkpoint("migration_before_commit", eventData, transaction); return result; }

    public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction,
        TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
    { Checkpoint("migration_before_commit", eventData, transaction); return ValueTask.FromResult(result); }

    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData) =>
        Checkpoint("migration_committed", eventData, transaction);

    public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    { Checkpoint("migration_committed", eventData, transaction); return Task.CompletedTask; }
}
