using System.Text.Json;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;

namespace Kofte.Composition.TestModule.Modules;

public sealed class HealthControl
{
    private int health = (int)ModuleHealth.Healthy;
    private int checks;
    public ModuleHealth Alpha { get => (ModuleHealth)Volatile.Read(ref health); set => Volatile.Write(ref health, (int)value); }
    public int Checks => Volatile.Read(ref checks);
    internal ModuleHealthResult Check(string owner)
    {
        Interlocked.Increment(ref checks);
        return new(owner == BootModules.Alpha ? Alpha : ModuleHealth.Healthy);
    }
}

public static class HealthModules
{
    public const string Gamma = "org.example.gamma";
    public static IReadOnlyList<string> Owners { get; } =
        Array.AsReadOnly(new[] { BootModules.Alpha, BootModules.Beta, Gamma, BootModules.NoStorage });

    public static IReadOnlyList<ModuleDeployment> Catalog(HealthControl control) =>
        Owners.Reverse().Select(owner => new ModuleDeployment(JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            id = owner,
            version = "1.0.0",
            displayName = "Public health fixture",
            hostContract = "1.0.0",
            capabilities = Array.Empty<object>(),
            permissions = Array.Empty<object>(),
            dependencies = (owner == BootModules.Beta ? new[] { BootModules.Alpha } :
                owner == Gamma ? [BootModules.Beta] : []).Select(id => new { id, versionRange = ">=1.0.0 <2.0.0" }),
            storage = new { relational = false }
        }), createRuntime: (_, _) => new(new Runtime(owner, control), ModuleRuntimeOwnership.Attempt))).ToArray();

    private sealed class Runtime(string owner, HealthControl control) : IModuleRuntime
    {
        public ValueTask InitializeAsync(ModuleInitializationContext context, CancellationToken token) => ValueTask.CompletedTask;
        public ValueTask StartAsync(ModuleActivationContext context, CancellationToken token)
        {
            if (context.Storage is not null) throw new InvalidOperationException("composition_health_fixture_has_store");
            return ValueTask.CompletedTask;
        }
        public ValueTask<ModuleHealthResult> CheckHealthAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            return ValueTask.FromResult(control.Check(owner));
        }
        public ValueTask StopAsync(CancellationToken token) => ValueTask.CompletedTask;
    }
}
