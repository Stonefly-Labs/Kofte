using System.Text.Json;
using System.Runtime.InteropServices;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.StorageMaintenance;
using Kofte.Infrastructure.Composition;
using Kofte.Platform.Modules.Composition;

namespace Kofte.Host.Features.ModuleComposition;

public static class ModuleCommand
{
    internal static CompositionMode Mode(string[] args, bool worker = false) => args switch
    {
        [] when !worker => CompositionMode.Serve,
        ["--modules", "validate"] when !worker => CompositionMode.Validate,
        ["--modules", "prepare"] when !worker => CompositionMode.Prepare,
        ["--modules-worker", "validate"] when worker => CompositionMode.Validate,
        ["--modules-worker", "prepare"] when worker => CompositionMode.Prepare,
        ["--modules-worker", "serve"] when worker => CompositionMode.Serve,
        _ => throw new CompositionBoundaryException(worker
            ? "composition_admission_invalid" : "composition_configuration_invalid")
    };

    public static async Task<int> RunAsync(string[] args, IReadOnlyList<ModuleDeployment> catalog,
        IReadOnlyList<ModuleStorageRecipe> recipes, CancellationToken token = default)
    {
        CompositionDiagnostics? diagnostics = null;
        try
        {
            var mode = Mode(args);
            var configuration = StorageMaintenanceConfiguration.FromEnvironment();
            diagnostics = new(mode, configuration.Runtime.Profile == "local-sqlite" ? "sqlite" : "postgresql", Write);
            diagnostics.Emit(CompositionStage.Input, CompositionOutcome.Progress, "composition_progress");
            var selection = ModuleSelectionReader.Read(Environment.GetEnvironmentVariable("KOFTE_MODULES_FILE"),
                Directory.GetCurrentDirectory());
            if (catalog.Count > 32 || recipes.Count > 64)
                throw new CompositionBoundaryException("composition_input_limit");
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(token);
            using var terminate = PosixSignalRegistration.Create(PosixSignal.SIGTERM,
                context => { context.Cancel = true; stop.Cancel(); });
            void Cancel(object? sender, ConsoleCancelEventArgs args)
            {
                args.Cancel = true;
                stop.Cancel();
            }
            Console.CancelKeyPress += Cancel;
            try
            {
                return await CompositionSupervisor.RunAsync(configuration, catalog, selection, diagnostics, stop.Token);
            }
            finally { Console.CancelKeyPress -= Cancel; }
        }
        catch (Exception error)
        {
            var failure = CompositionFailure.From(error);
            try
            {
                diagnostics ??= new(CompositionMode.Validate, "unknown", Write);
                diagnostics.Emit(CompositionStage.Input, failure.Outcome, failure.Code, isTerminal: true);
            }
            catch (Exception) { return 1; }
            return failure.Exit;
        }
    }

    internal static void Write(CompositionRecord record) =>
        Console.Out.WriteLine(JsonSerializer.Serialize(record, CompositionDiagnostics.JsonOptions));
}
