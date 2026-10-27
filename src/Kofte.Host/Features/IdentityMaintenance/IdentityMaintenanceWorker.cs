using Kofte.Host.Bootstrap;
using Kofte.Host.Features.Identity;
using Kofte.Host.Features.StorageMaintenance;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Identity;
using Kofte.Platform.Modules.Storage;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Kofte.Host.Features.IdentityMaintenance;

internal static class IdentityMaintenanceWorker
{
    internal static async Task<int> ExecuteAsync(string[] args, CancellationToken token = default)
    {
        try
        {
            var operation = IdentityCommand.Parse(args, "--identity-worker");
            await StorageMaintenanceProcess.AdmitAsync(token);
            var configuration = StorageMaintenanceConfiguration.FromEnvironment();
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ??
                Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";
            using var identity = IdentityConfiguration.Load(configuration.Runtime, environment, operation);
            var mode = identity.Mode == IdentityMode.Oidc ? "oidc" : "development";
            var installation = configuration.CreateInstallation();
            using var budget = new OperationBudget(TimeSpan.FromSeconds(116), token);
            await using var scope = await MaintenanceScope.AcquireAsync(installation, budget.Token,
                TimeSpan.FromTicks(Math.Min(OperationBudget.Acquire.Ticks, budget.Remaining.Ticks)));
            await StorageReconciliation.ValidateStartupAsync(installation, budget.Token);
            var registry = new RegistryMigrationRunner(installation);
            await using var connection = await installation.OpenPlatformAsync(budget);
            var owner = await registry.ReadInstallationAsync(connection, budget);
            var durability = "unchanged";
            if (operation == IdentityOperation.Prepare)
            {
                if (owner.Version == 2)
                {
                    await IdentityPreparation.VerifyAsync(installation, owner.Id, mode, identity.TrustDigest, budget);
                    using var keys = await IdentityKeys.PrepareAsync(identity, owner.Id, installation, scope, budget,
                        existingOnly: true);
                    if (keys.PreparationChanged) durability = "committed";
                }
                else
                {
                    using var keys = await IdentityKeys.PrepareAsync(identity, owner.Id, installation, scope, budget);
                    await IdentityPreparation.InitializeAsync(installation, scope, connection, owner.Id, mode,
                        identity.TrustDigest, budget);
                    durability = "committed";
                }
            }
            else
            {
                if (owner.Version != 2) throw new StorageException(StorageCode.VersionUnsupported);
                var reconcile = operation == IdentityOperation.ReconcileConfiguration;
                await IdentityPreparation.VerifyAsync(installation, owner.Id, mode, identity.TrustDigest, budget,
                    reconcile);
                if (reconcile)
                {
                    using var keys = await IdentityKeys.PrepareAsync(identity, owner.Id, installation, scope, budget);
                }
                await IdentityPreparation.RevokeAsync(installation, scope, owner.Id, mode, identity.TrustDigest,
                    reconcile, budget);
                durability = "committed";
            }
            await scope.VerifyAsync(installation, budget);
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new
            {
                eventCode = "identity_operator_completed",
                operation = args[1],
                durability,
                correlation = Guid.NewGuid().ToString("N")
            }));
            return 0;
        }
        catch (StorageException error) { return StorageMaintenanceProcess.FailureExitCode(error.Code); }
        catch (StorePathException) { return StorageMaintenanceProcess.FailureExitCode(StorageCode.PathRejected); }
        catch (Exception error) when (error is BootstrapException or IdentityConfigurationException or
            InvalidOperationException or ArgumentException or SqliteException or NpgsqlException or IOException or
            UnauthorizedAccessException or OperationCanceledException or OverflowException or IdentityEventCapacityException)
        {
            return StorageMaintenanceProcess.FailureExitCode(StorageDiagnostics.FailureCode(error,
                token.IsCancellationRequested));
        }
    }
}
