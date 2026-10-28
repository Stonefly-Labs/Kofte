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
        IdentityObservation? observation = null;
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
            observation = IdentityDiagnostics.Begin(operation switch
            {
                IdentityOperation.Prepare => IdentityAction.Prepare,
                IdentityOperation.BootstrapAuthorize => IdentityAction.BootstrapAuthorize,
                IdentityOperation.BootstrapDisablePending => IdentityAction.BootstrapDisablePending,
                IdentityOperation.ReconcileConfiguration => IdentityAction.ReconcileConfiguration,
                IdentityOperation.Maintain => IdentityAction.Maintain,
                IdentityOperation.RevokeSessions => IdentityAction.RevokeSessions,
                _ => throw new StorageException(StorageCode.RequestInvalid)
            }, mode, installation.Provider, TimeSpan.FromSeconds(116), token,
                record => Console.WriteLine(System.Text.Encoding.UTF8.GetString(IdentityDiagnostics.Serialize(record))));
            var budget = observation.Budget;
            await using var scope = await MaintenanceScope.AcquireAsync(installation, budget.Token,
                TimeSpan.FromTicks(Math.Min(OperationBudget.Acquire.Ticks, budget.Remaining.Ticks)));
            await StorageReconciliation.ValidateStartupAsync(installation, budget.Token);
            var registry = new RegistryMigrationRunner(installation);
            await using var connection = await installation.OpenPlatformAsync(budget);
            var owner = await registry.ReadInstallationAsync(connection, budget);
            if (operation == IdentityOperation.Prepare)
            {
                if (owner.Version == 2)
                {
                    await IdentityPreparation.VerifyAsync(installation, owner.Id, mode, identity.TrustDigest, budget);
                    using var keys = await IdentityKeys.PrepareAsync(identity, owner.Id, installation, scope, budget,
                        existingOnly: true);
                }
                else
                {
                    using var keys = await IdentityKeys.PrepareAsync(identity, owner.Id, installation, scope, budget);
                    await IdentityPreparation.InitializeAsync(installation, scope, connection, owner.Id, mode,
                        identity.TrustDigest, budget);
                }
            }
            else if (operation is IdentityOperation.BootstrapAuthorize or IdentityOperation.BootstrapDisablePending)
            {
                if (owner.Version != 2) throw new StorageException(StorageCode.VersionUnsupported);
                if (identity.Mode != IdentityMode.Oidc)
                    throw new IdentityStateException(IdentityDiagnosticOutcome.Forbidden);
                await IdentityPreparation.VerifyAsync(installation, owner.Id, mode, identity.TrustDigest, budget);
                if (operation == IdentityOperation.BootstrapAuthorize)
                {
                    using var keys = IdentityKeys.Open(identity, owner.Id, budget);
                    var nominee = IdentityNomination.Read(args[4], identity);
                    var authorization = IdentityNomination.NewAuthorization();
                    await IdentityBootstrapStore.AuthorizeAsync(installation, scope, owner.Id, mode,
                        identity.TrustDigest, nominee.Issuer, nominee.Subject, authorization,
                        () => IdentityNomination.DeliverAsync(args[6], authorization, budget), budget);
                }
                else await IdentityBootstrapStore.DisablePendingAsync(installation, scope, owner.Id, mode,
                    identity.TrustDigest, budget);
            }
            else if (operation == IdentityOperation.Maintain)
            {
                if (owner.Version != 2) throw new StorageException(StorageCode.VersionUnsupported);
                await IdentityPreparation.VerifyAsync(installation, owner.Id, mode, identity.TrustDigest, budget);
                await Kofte.Infrastructure.Identity.IdentityMaintenance.RunAsync(installation, scope, owner.Id,
                    mode, identity.TrustDigest, DateTime.UtcNow, budget);
            }
            else if (operation is IdentityOperation.ReconcileConfiguration or IdentityOperation.RevokeSessions)
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
            }
            else throw new StorageException(StorageCode.RequestInvalid);
            await scope.VerifyAsync(installation, budget);
            return 0;
        }
        catch (Exception error) when (error is StorageException or StorePathException or
            IdentityStateException or BootstrapException or IdentityConfigurationException or
            InvalidOperationException or ArgumentException or SqliteException or NpgsqlException or IOException or
            UnauthorizedAccessException or OperationCanceledException or OverflowException or IdentityEventCapacityException)
        {
            observation?.Fail(error);
            return StorageMaintenanceProcess.FailureExitCode(StorageDiagnostics.FailureCode(error,
                token.IsCancellationRequested));
        }
        finally { observation?.Dispose(); }
    }
}
