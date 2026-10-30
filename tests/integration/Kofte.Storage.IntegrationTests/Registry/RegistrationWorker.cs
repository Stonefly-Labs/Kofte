using System.Diagnostics;
using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Providers;
using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Registry;

public static class RegistrationWorker
{
    public static async Task<int> Main(string[] args)
    {
        if (args.FirstOrDefault() == "--identity-mapping-worker")
            return await Identity.IdentityMappingWorker.RunAsync(args);
        if (args is ["--storage-crash-worker", _, "compose", _])
            return await Composition.CompositionPreparationCrashWorker.RunAsync(args);
        if (args.FirstOrDefault() == "--storage-crash-worker")
            return await StorageCrashWorker.RunAsync(args, ModuleFixture.AlphaFactory);
        if (args.FirstOrDefault() is "--registration-worker" or "--registration-probe")
            return await RunAsync(args);
        return args.Any(arg => arg is "--server" or "--internal-msbuild-node")
            ? await Xunit.MicrosoftTestingPlatform.TestPlatformTestFramework.RunAsync(args,
                SelfRegisteredExtensions.AddSelfRegisteredExtensions)
            : await Xunit.Runner.InProc.SystemConsole.ConsoleRunner.Run(args);
    }

    private static async Task<int> RunAsync(string[] args)
    {
        StorageOperation? operation = null;
        try
        {
            using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            var provider = StorageTestEnvironment.Provider;
            var installation = provider == "sqlite"
                ? StorageInstallation.Sqlite(Environment.GetEnvironmentVariable("KOFTE_DATA_ROOT") ??
                    throw new InvalidOperationException("storage_test_root_required"))
                : StorageInstallation.PostgreSql(Environment.GetEnvironmentVariable("KOFTE_POSTGRES_CONNECTION") ??
                    throw new InvalidOperationException("storage_test_provider_required"),
                    Environment.GetEnvironmentVariable("KOFTE_POSTGRES_PROVISIONING_CONNECTION"));
            var registry = new StorageRegistry(installation);
            StorageResult.Ready result;
            if (args is ["--registration-probe"])
            {
                var description = RegistryFixture.Module("org.example.alpha", version: "2.0.0",
                    display: "Renamed Köfte module");
                await using var scope = await new StorageMaintenance(installation).AcquireAsync(budget.Token);
                result = Assert.IsType<StorageResult.Ready>(
                    await registry.RegisterAsync(description, scope, budget.Token));
            }
            else if (args is ["--registration-worker", "same" or "distinct", var roundText, var indexText] &&
                int.TryParse(roundText, out var round) && round is >= 0 and < 20 &&
                int.TryParse(indexText, out var index) && index is >= 0 and < 10)
            {
                using (var warmup = new OperationBudget(OperationBudget.Acquire, budget.Token))
                await using (await installation.OpenPlatformAsync(warmup)) { }
                Console.WriteLine("""{"eventCode":"storage_worker_started"}""");
                if (await Console.In.ReadLineAsync(budget.Token) != "prepare")
                    throw new InvalidOperationException("storage_worker_preparation_required");
                // Warmup leases remain serial; only registration contends at the shared barrier.
                operation = StorageOperation.Acquire;
                await using (await new StorageMaintenance(installation).AcquireAsync(budget.Token)) { }
                Console.WriteLine("""{"eventCode":"storage_worker_waiting"}""");
                if (await Console.In.ReadLineAsync(budget.Token) != "start")
                    throw new InvalidOperationException("storage_worker_barrier_required");
                var module = RegistryFixture.Module(Owner(args[1] == "same", round, index));
                result = await RegisterWithRetryAsync(installation, module, budget.Token,
                    value => operation = value);
            }
            else throw new InvalidOperationException("storage_worker_scenario_invalid");
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                eventCode = "storage_worker_ready",
                key = result.Assignment.AllocationKey,
                allocation = result.Assignment.AllocationId,
                installation = result.Assignment.InstallationId,
                owner = result.Assignment.Owner.Value,
                revision = result.Assignment.Revision
            }));
            return 0;
        }
        catch (Exception error)
        {
            var failure = JsonSerializer.Serialize(new
            {
                eventCode = "storage_worker_failed",
                operation = operation switch
                {
                    StorageOperation.Acquire => "acquire",
                    StorageOperation.Register => "register",
                    _ => "setup"
                },
                code = StorageDiagnostics.Diagnostic(error switch
                {
                    StorageException storage => storage.Code,
                    OperationCanceledException => StorageCode.Cancelled,
                    _ => StorageCode.Unavailable
                }).Code,
                correlationId = ActivityTraceId.CreateRandom().ToHexString()
            });
            Console.WriteLine(failure);
            Console.Error.WriteLine(failure);
            return 1;
        }
    }

    internal static string Owner(bool same, int round, int index) =>
        same ? $"org.example.same-r{round}" : $"org.example.r{round}-m{index}";

    internal static async Task<StorageResult.Ready> RegisterWithRetryAsync(StorageInstallation installation,
        Platform.Modules.Manifest.ModuleDescription module, CancellationToken token,
        Action<StorageOperation>? observe = null)
    {
        var maintenance = new StorageMaintenance(installation);
        var registry = new StorageRegistry(installation);
        var attempts = 0;
        while (attempts++ < 20)
        {
            token.ThrowIfCancellationRequested();
            try
            {
                observe?.Invoke(StorageOperation.Acquire);
                await using var scope = await maintenance.AcquireAsync(token);
                observe?.Invoke(StorageOperation.Register);
                var result = await registry.RegisterAsync(module, scope, token);
                if (result is StorageResult.Ready ready) return ready;
                if (result.Outcome != StorageOutcome.Busy)
                    throw new StorageException(Enum.GetValues<StorageCode>().Single(code =>
                        StorageDiagnostics.Diagnostic(code).Code == result.Diagnostics[0].Code));
            }
            catch (StorageException error) when (error.Code == StorageCode.Busy) { }
            await Task.Delay(TimeSpan.FromMilliseconds(25), token);
        }
        throw new InvalidOperationException("storage_retry_budget_exhausted");
    }
}
