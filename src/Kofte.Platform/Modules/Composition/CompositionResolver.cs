using System.Diagnostics;
using System.Security.Cryptography;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.Modules.Composition;

public static class CompositionResolver
{
    /// <summary>Admission/manifest/graph gate before supervised settings validation. Returns no accepted subset.</summary>
    public static IReadOnlyList<CompositionDiagnostic> ValidateStructure(IEnumerable<ModuleDeployment> deployments,
        IEnumerable<ModuleSelection> selections, ManifestValidationContext? context,
        CancellationToken cancellationToken = default, TimeSpan? budget = null) =>
        ResolveCore(deployments, selections, context, cancellationToken, budget, false) is CompositionResolution.Rejected rejected
            ? rejected.Diagnostics : Array.AsReadOnly(Array.Empty<CompositionDiagnostic>());

    /// <summary>Pure structural and claim resolution only; acceptance grants no settings, storage or lifecycle authority.</summary>
    public static CompositionResolution Resolve(IEnumerable<ModuleDeployment> deployments,
        IEnumerable<ModuleSelection> selections, ManifestValidationContext? context,
        CancellationToken cancellationToken = default, TimeSpan? budget = null) =>
        ResolveCore(deployments, selections, context, cancellationToken, budget, true);

    private static CompositionResolution ResolveCore(IEnumerable<ModuleDeployment> deployments,
        IEnumerable<ModuleSelection> selections, ManifestValidationContext? context,
        CancellationToken cancellationToken, TimeSpan? budget, bool validateContributions)
    {
        ArgumentNullException.ThrowIfNull(deployments);
        ArgumentNullException.ThrowIfNull(selections);
        var work = new ResolutionWork(cancellationToken, budget);
        CompositionResolution.Rejected Reject(IEnumerable<CompositionDiagnostic> diagnostics)
        {
            work.Check();
            var result = CompositionDiagnostic.Reject(diagnostics);
            work.Check();
            return result;
        }
        CompositionResolution.Rejected Limit() => Reject([new("composition_input_limit")]);
        try
        {
            work.Check();
            var catalog = new List<ModuleDeployment>();
            var bytes = 0;
            using (var iterator = deployments.GetEnumerator())
                while (true)
                {
                    work.Check();
                    if (!iterator.MoveNext()) break;
                    work.Check();
                    var deployment = iterator.Current;
                    if (deployment is null || catalog.Count == 32 || deployment.ManifestBytes.Length > 65_536)
                        return Limit();
                    bytes += deployment.ManifestBytes.Length;
                    if (bytes > 2_097_152) return Limit();
                    catalog.Add(deployment);
                }
            var selection = new List<ModuleSelection>();
            var enabledCount = 0;
            using (var iterator = selections.GetEnumerator())
                while (true)
                {
                    work.Check();
                    if (!iterator.MoveNext()) break;
                    work.Check();
                    var entry = iterator.Current;
                    if (entry is null || selection.Count == 32 || (entry.Enabled && ++enabledCount > 16))
                        return Limit();
                    selection.Add(entry);
                }
            var errors = new List<CompositionDiagnostic>();
            if (catalog.Count == 0 && !SemanticVersion.TryParse(context?.HostContractVersion, out _))
                return Reject([new("composition_configuration_invalid")]);
            var definitions = new List<(ModuleDescription Description, ModuleDeployment Deployment)>();
            foreach (var deployment in catalog)
            {
                work.Check();
                if (deployment.ContractVersion != 1)
                {
                    errors.Add(new("composition_contract_unsupported", CompositionPhase.Deployment));
                    continue;
                }
                var manifest = ManifestValidator.Validate(deployment.ManifestBytes, context);
                if (manifest is ManifestValidationResult.Accepted accepted)
                    definitions.Add((accepted.Description, deployment));
                else
                {
                    var digest = Convert.ToHexString(SHA256.HashData(deployment.ManifestBytes));
                    var index = 0;
                    foreach (var diagnostic in ((ManifestValidationResult.Rejected)manifest).Diagnostics)
                        errors.Add(new("composition_manifest_rejected", CompositionPhase.Deployment,
                            field: "manifest", index: index++, manifestDiagnostic: diagnostic, sortKey: digest));
                }
            }
            foreach (var duplicate in definitions.GroupBy(d => d.Description.Id.Value, StringComparer.Ordinal).Where(g => g.Count() > 1))
                errors.Add(new("composition_module_duplicate", CompositionPhase.Deployment, duplicate.First().Description.Id));
            var selected = new Dictionary<string, bool>(StringComparer.Ordinal);
            foreach (var entry in selection)
            {
                work.Check();
                if (!ModuleId.TryParse(entry.Id, out var id))
                    errors.Add(new("composition_configuration_invalid", CompositionPhase.Deployment));
                else if (!selected.TryAdd(id.Value, entry.Enabled))
                    errors.Add(new("composition_configuration_invalid", CompositionPhase.Deployment, id));
            }
            // Missing implementation checks require a valid whole deployed set; never invent a descriptor from rejected bytes.
            if (errors.Count != 0) return Reject(errors);
            var deployed = definitions.ToDictionary(d => d.Description.Id.Value, StringComparer.Ordinal);
            foreach (var entry in selected.OrderBy(e => e.Key, StringComparer.Ordinal))
                if (!deployed.ContainsKey(entry.Key))
                {
                    ModuleId.TryParse(entry.Key, out var id);
                    errors.Add(new(entry.Value ? "composition_implementation_missing" : "composition_configuration_invalid",
                        CompositionPhase.Deployment, id));
                }
            if (errors.Count != 0) return Reject(errors);
            var enabled = definitions.Where(d => selected.GetValueOrDefault(d.Description.Id.Value))
                .OrderBy(d => d.Description.Id.Value, StringComparer.Ordinal).ToArray();
            if (enabled.Any(d => d.Deployment.ConfigurationFields.Count > 128 || d.Deployment.OwnedServiceTypes.Count > 128
                || d.Deployment.Claims.Count > 128) || enabled.Sum(d => d.Deployment.Claims.Count) > 1024)
                return Limit();
            var graph = CompositionGraph.Resolve(deployed.Values.Select(d => d.Description),
                enabled.Select(d => d.Description), errors, work);
            if (errors.Count != 0) return Reject(errors);
            var claims = validateContributions ? ContributionValidator.Validate(enabled, errors, work) : [];
            if (errors.Count != 0) return Reject(errors);
            work.Check();
            return new CompositionResolution.Accepted(new(
                definitions.Select(d => d.Description).OrderBy(d => d.Id.Value, StringComparer.Ordinal),
                enabled.Select(d => d.Description.Id),
                definitions.Where(d => !selected.GetValueOrDefault(d.Description.Id.Value)).Select(d => d.Description.Id)
                    .OrderBy(id => id.Value, StringComparer.Ordinal),
                graph.Order, graph.Dependencies, claims));
        }
        catch (ResolutionRefused refused)
        {
            return CompositionDiagnostic.Reject([new(refused.Code)]);
        }
    }
}

internal sealed class ResolutionRefused(string code) : Exception
{
    internal string Code { get; } = code;
}

internal sealed class ResolutionWork
{
    private readonly long started = Stopwatch.GetTimestamp();
    private readonly TimeSpan duration;
    private readonly CancellationToken cancellation;
    internal ResolutionWork(CancellationToken cancellation, TimeSpan? budget)
    {
        this.cancellation = cancellation;
        duration = budget is { } requested && requested < TimeSpan.FromSeconds(2) ? requested : TimeSpan.FromSeconds(2);
    }
    internal void Check()
    {
        if (cancellation.IsCancellationRequested) throw new ResolutionRefused("composition_cancelled");
        if (Stopwatch.GetElapsedTime(started) >= duration) throw new ResolutionRefused("composition_deadline_exceeded");
    }
}
