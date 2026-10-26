using Kofte.Composition.TestModule;
using Kofte.Infrastructure.Composition;
using Kofte.Infrastructure.Storage;
using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Composition;

internal static class CompositionPreparationCrashWorker
{
    internal const string Phase = "migration_in_progress_committed";

    internal static async Task<int> RunAsync(string[] args)
    {
        StoragePhaseBarrier? barrier = null;
        try
        {
            if (args is not ["--storage-crash-worker", "sqlite" or "postgres", "compose", Phase])
                throw new InvalidOperationException("composition_test_crash_selector_invalid");
            using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(110));
            await StorageMaintenanceProcess.AdmitAsync(budget.Token);
            var installation = args[1] == "sqlite"
                ? StorageInstallation.Sqlite(Environment.GetEnvironmentVariable("KOFTE_DATA_ROOT") ??
                    throw new InvalidOperationException("composition_test_crash_configuration_required"))
                : StorageInstallation.PostgreSql(Environment.GetEnvironmentVariable("KOFTE_POSTGRES_CONNECTION") ??
                    throw new InvalidOperationException("composition_test_crash_configuration_required"),
                    Environment.GetEnvironmentVariable("KOFTE_POSTGRES_PROVISIONING_CONNECTION"));
            var enabled = StorageRecipes.Descriptions;
            var recipes = StorageRecipes.ForRelease();
            var firstCompleted = false;
            await using var session = await CompositionStorageSession.OpenPrepareAsync(installation, budget.Token);
            await session.InspectAsync(enabled, recipes, serving: false, budget.Token);
            await session.PrepareAsync(enabled, recipes, budget.Token, progress =>
            {
                if (progress.Owner.Equals(enabled[0].Id) && progress.Transition == CompositionStorageTransition.Completed)
                    firstCompleted = true;
                if (!progress.Owner.Equals(enabled[1].Id) || progress.Transition != CompositionStorageTransition.Entered) return;
                if (!firstCompleted || barrier is not null)
                    throw new InvalidOperationException("composition_test_crash_order_invalid");
                barrier = new StoragePhaseBarrier(Phase, hold: true);
            });
            throw new InvalidOperationException("composition_test_crash_phase_not_reached");
        }
        catch (Exception)
        {
            Console.Error.WriteLine("""{"eventCode":"composition_test_crash_failed"}""");
            return 1;
        }
        finally { barrier?.Dispose(); }
    }
}
