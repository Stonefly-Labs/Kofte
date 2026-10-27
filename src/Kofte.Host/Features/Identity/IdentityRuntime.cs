using Kofte.Host.Bootstrap;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Identity;
using Microsoft.AspNetCore.DataProtection;
using Npgsql;
using Paths = Kofte.Infrastructure.Storage.UnixStorePaths;

namespace Kofte.Host.Features.Identity;

internal sealed class IdentityRuntime : IDisposable
{
    private readonly IdentityConfiguration configuration;
    private readonly IdentityKeys keys;
    private readonly StorageInstallation installation;
    private readonly Guid installationId;
    private readonly IdentityDiagnosticContext diagnostics;

    private IdentityRuntime(IdentityConfiguration configuration, IdentityKeys keys,
        StorageInstallation installation, Guid installationId, Action<IdentityDiagnosticRecord> sink)
    {
        (this.configuration, this.keys, this.installation, this.installationId) =
            (configuration, keys, installation, installationId);
        diagnostics = new(installation.Provider, sink);
    }

    internal static async Task<IdentityRuntime> OpenAsync(BootstrapConfiguration bootstrap, string environment,
        StorageInstallation installation, OperationBudget budget, Action<IdentityDiagnosticRecord>? sink = null)
    {
        budget.Check();
        sink ??= IdentityDiagnostics.Write;
        IdentityConfiguration configuration;
        try { configuration = IdentityConfiguration.Load(bootstrap, environment); }
        catch (IdentityConfigurationException)
        {
            budget.Check();
            using var invalid = IdentityDiagnostics.Begin(IdentityAction.Startup, "unknown", installation.Provider,
                budget.Remaining, budget.Token, sink);
            invalid.Select(IdentityDiagnosticOutcome.ConfigurationInvalid);
            throw;
        }
        IdentityKeys? keys = null;
        try
        {
            budget.Check();
            var mode = configuration.Mode == IdentityMode.Oidc ? "oidc" : "development";
            using var observation = IdentityDiagnostics.Begin(IdentityAction.Startup, mode, installation.Provider,
                budget.Remaining, budget.Token, sink);
            try
            {
                if (installation.ProvisioningConnection is not null)
                    throw new IdentityStateException(IdentityDiagnosticOutcome.ConfigurationInvalid);
                var owner = await StorageReconciliation.ReadStartupAsync(installation, budget);
                if (owner?.Version != 2)
                    throw new IdentityStateException(IdentityDiagnosticOutcome.PrepareRequired);
                await IdentityPreparation.VerifyAsync(installation, owner.Id, mode, configuration.TrustDigest, budget);
                var marker = Path.Combine(configuration.KeyRingDirectory, "identity-restore-required");
                Paths.ValidateFile(marker, allowMissing: true);
                if (File.Exists(marker))
                    throw new IdentityStateException(IdentityDiagnosticOutcome.ReconciliationRequired);
                keys = IdentityKeys.Open(configuration, owner.Id, budget);
                return new(configuration, keys, installation, owner.Id, sink);
            }
            catch (Exception error)
            {
                observation.Fail(error);
                throw;
            }
        }
        catch
        {
            try { keys?.Dispose(); }
            finally { configuration.Dispose(); }
            throw;
        }
    }

    internal void Register(IServiceCollection services, Kofte.Infrastructure.Storage.StoreLease? fileLease,
        NpgsqlConnection? postgresLease)
    {
        var mode = configuration.Mode == IdentityMode.Oidc ? "oidc" : "development";
        services.AddSingleton(new IdentityStore(installation, installationId, mode, configuration.TrustDigest,
            fileLease, postgresLease));
        services.AddSingleton<IDataProtectionProvider>(keys.Provider);
        services.AddSingleton(diagnostics);
        services.AddIdentityBoundary(configuration);
    }

    public void Dispose()
    {
        try { keys.Dispose(); }
        finally { configuration.Dispose(); }
    }
}
