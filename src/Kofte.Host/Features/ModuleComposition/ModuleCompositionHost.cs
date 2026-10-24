using Kofte.Infrastructure.Composition;
using Kofte.Platform.Modules.Composition;

namespace Kofte.Host.Features.ModuleComposition;

public static class ModuleCompositionHost
{
    public static Task<int> RunAsync(string[] args, IReadOnlyList<ModuleDeployment> catalog,
        IReadOnlyList<ModuleStorageRecipe>? recipes = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(catalog);
        if (args.FirstOrDefault() == "--modules-worker")
            return CompositionWorker.RunAsync(args, catalog, recipes ?? []);
        if (args.FirstOrDefault() == "--modules" ||
            args.Length == 0 && (catalog.Count != 0 || Environment.GetEnvironmentVariable("KOFTE_MODULES_FILE") is not null))
            return ModuleCommand.RunAsync(args, catalog, recipes ?? []);
        return BootstrapProgram.RunUncomposedAsync(args);
    }
}
