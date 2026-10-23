using Kofte.Infrastructure.Storage.Recovery;

namespace Kofte.Infrastructure.Storage.Registry;

public static class StorageReconciliation
{
    public static async Task ValidateStartupAsync(StorageInstallation installation,
        CancellationToken cancellationToken = default)
    {
        using var budget = new OperationBudget(OperationBudget.Resolve, cancellationToken);
        await using var connection = await installation.OpenPlatformAsync(budget);
        var query = installation.Provider == "sqlite"
            ? "SELECT count(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'"
            : "SELECT count(*) FROM pg_catalog.pg_tables WHERE schemaname='kofte_system'";
        var count = Convert.ToInt64(await RegistryMigrationRunner.ScalarAsync(connection, query, budget),
            System.Globalization.CultureInfo.InvariantCulture);
        if (count != 0)
        {
            await new RegistryMigrationRunner(installation).ReadInstallationAsync(connection, budget);
            await RecoverySetVerifier.ValidateInventoryAsync(installation, budget);
            return;
        }
        await new RegistryMigrationRunner(installation).ValidateV0Async(connection, budget);
    }
}
