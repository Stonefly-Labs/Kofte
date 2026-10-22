using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.Consumer.Modules.Alpha;
using Kofte.Storage.Consumer.Modules.Beta;
using Kofte.TestSupport;
using Microsoft.EntityFrameworkCore;

namespace Kofte.Storage.Consumer.Scenarios;

internal static class CorrectedRetryScenario
{
    internal static async Task<int> RunAsync(StorageInstallation installation, CancellationToken token)
    {
        var registry = new StorageRegistry(installation);
        var maintenance = new StorageMaintenance(installation);
        var alpha = RegistrationScenarios.Module("org.example.consumer-corrected-alpha");
        var beta = RegistrationScenarios.Module("org.example.consumer-corrected-beta");
        var sqlite = installation.Provider == "sqlite";
        var root = Environment.GetEnvironmentVariable("KOFTE_DATA_ROOT");
        var runtime = Environment.GetEnvironmentVariable("KOFTE_POSTGRES_CONNECTION");
        await using (var scope = await maintenance.AcquireAsync(token))
        {
            RegistrationScenarios.Ready(await registry.RegisterAsync(beta, scope, token));
            await using var binding = await registry.BindAsync(beta.Id, token);
            RegistrationScenarios.Ready(await binding.MigrateAsync(BetaStorage.Create, scope, cancellationToken: token));
            await using var context = await binding.CreateContextAsync(BetaStorage.Create, token);
            context.Records.Add(new Modules.Beta.Record { Id = 1, Payload = "correction-beta-retained" });
            await context.SaveChangesAsync(token);
            using var cancelled = new CancellationTokenSource();
            cancelled.Cancel();
            RegistrationScenarios.Require(StorageResultChecks.Check(await registry.RegisterAsync(alpha, scope,
                cancelled.Token)).Diagnostics[0].Code == "storage_cancelled");
            if (sqlite) RegistrationScenarios.Ready(await registry.RegisterAsync(alpha, scope, token));
        }
        await using (var blocked = await StorageReadBlock.CreateAsync(sqlite ? "sqlite" : "postgres", root, runtime, token))
            RegistrationScenarios.Require(StorageResultChecks.Check(await new StorageRegistry(blocked.Reader)
                .ResolveAsync(beta.Id, token)).Outcome == StorageOutcome.Busy);
        StorageResult failed;
        if (sqlite)
        {
            var platform = Path.Combine(root!, "platform", "platform.db");
            var held = platform + ".unavailable";
            File.Move(platform, held);
            try
            {
                await using var scope = await maintenance.AcquireAsync(token);
                failed = await registry.RegisterAsync(alpha, scope, token);
            }
            finally { File.Move(held, platform); }
            RegistrationScenarios.Require(failed.Diagnostics.Single().Code == "storage_missing");
        }
        else
        {
            var denied = StorageInstallation.PostgreSql(runtime!);
            await using var scope = await new StorageMaintenance(denied).AcquireAsync(token);
            failed = await new StorageRegistry(denied).RegisterAsync(alpha, scope, token);
            RegistrationScenarios.Require(failed.Diagnostics.Single().Code == "storage_privilege_denied");
        }
        RegistrationScenarios.Require(failed is StorageResult.Failure);
        StorageResultChecks.Check(failed);
        var before = await StorageInspection.ReadAsync(sqlite ? "sqlite" : "postgres", root, runtime, alpha.Id, token)
            ?? throw new InvalidOperationException("storage_correction_reservation_missing");
        await using var correctedScope = await maintenance.AcquireAsync(token);
        var corrected = RegistrationScenarios.Ready(await registry.RegisterAsync(alpha, correctedScope, token));
        RegistrationScenarios.Require(corrected.Assignment.AllocationId == before.AllocationId &&
            corrected.Assignment.AllocationKey == before.Key && corrected.CorrelationId != failed.CorrelationId);
        await using (var binding = await registry.BindAsync(alpha.Id, token))
        {
            RegistrationScenarios.Ready(await binding.MigrateAsync(AlphaStorage.Create, correctedScope,
                cancellationToken: token));
            await using var context = await binding.CreateContextAsync(AlphaStorage.Create, token);
            context.Records.Add(new Modules.Alpha.Record { Id = 1, Payload = "correction-alpha-retained" });
            await context.SaveChangesAsync(token);
            RegistrationScenarios.Require((await context.Records.SingleAsync(token)).Payload == "correction-alpha-retained");
        }
        await using (var binding = await registry.BindAsync(beta.Id, token))
        await using (var context = await binding.CreateContextAsync(BetaStorage.Create, token))
            RegistrationScenarios.Require((await context.Records.SingleAsync(token)).Payload == "correction-beta-retained");
        return 8;
    }
}
