using System.Text;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Host.Features.ModuleComposition;

public sealed class CompositionPreflightResult
{
    public ResolvedComposition Composition { get; }
    public IReadOnlyDictionary<ModuleId, ModuleDeployment> Deployments { get; }
    public IReadOnlyDictionary<ModuleId, ModuleConfiguration> Configurations { get; }
    internal CompositionPreflightResult(ResolvedComposition composition,
        IDictionary<ModuleId, ModuleDeployment> deployments, IDictionary<ModuleId, ModuleConfiguration> configurations) =>
        (Composition, Deployments, Configurations) = (composition,
            new System.Collections.ObjectModel.ReadOnlyDictionary<ModuleId, ModuleDeployment>(deployments),
            new System.Collections.ObjectModel.ReadOnlyDictionary<ModuleId, ModuleConfiguration>(configurations));
}

public static class CompositionPreflight
{
    public static async Task<CompositionPreflightResult> ResolveAsync(IReadOnlyList<ModuleDeployment> catalog,
        ModuleSelectionDocument selection, CompositionAttempt attempt, CompositionDiagnostics diagnostics, CancellationToken token)
    {
        var callbacks = new CompositionCallbackRunner(attempt);
        attempt.EnterPhase(CompositionStage.Validated);
        var selected = selection.Modules.Select(m => new ModuleSelection(m.Owner.Value, m.Enabled)).ToArray();
        var context = new ManifestValidationContext("1.0.0");
        var structure = await callbacks.RunAsync(CompositionWork.Resolver, cancellation =>
            ValueTask.FromResult(CompositionResolver.ValidateStructure(catalog, selected, context, cancellation,
                attempt.Clip(CompositionWork.Resolver))), token);
        if (structure.Count != 0) Reject(structure);
        var deployments = new Dictionary<ModuleId, ModuleDeployment>();
        foreach (var deployment in catalog)
        {
            attempt.Check();
            var manifest = (ManifestValidationResult.Accepted)ManifestValidator.Validate(deployment.Manifest.Span, context);
            deployments.Add(manifest.Description.Id, deployment);
            attempt.EnterOwnerStage(manifest.Description.Id, CompositionStage.Discovered);
            attempt.CompleteOwnerStage(manifest.Description.Id, CompositionStage.Discovered);
            diagnostics.Emit(CompositionStage.Discovered, CompositionOutcome.Passed,
                "composition_progress", manifest.Description.Id);
        }
        var configurations = new Dictionary<ModuleId, ModuleConfiguration>();
        foreach (var entry in selection.Modules.Where(m => m.Enabled).OrderBy(m => m.Owner.Value, StringComparer.Ordinal))
        {
            var configuration = new ModuleConfiguration(entry.Owner, Encoding.UTF8.GetBytes(entry.Settings.GetRawText()));
            var issues = await callbacks.RunAsync(CompositionWork.Validator, cancellation =>
            {
                attempt.EnterOwnerStage(entry.Owner, CompositionStage.Validated);
                return ValueTask.FromResult(ModuleConfigurationValidator.Validate(deployments[entry.Owner], configuration, cancellation));
            }, token, entry.Owner);
            if (issues.Count != 0)
            {
                var settingsFailure = new CompositionPreflightException(entry.Owner, issues);
                attempt.FailPhase(CompositionStage.Validated, CompositionOutcome.Rejected, settingsFailure.Code);
                foreach (var issue in settingsFailure.Settings)
                    diagnostics.Emit(CompositionStage.Validated, CompositionOutcome.Rejected,
                        settingsFailure.Code, issue.Owner, fieldToken: issue.Field,
                        fieldReason: issue.Field is null ? null : issue.Reason.ToString().ToLowerInvariant());
                if (settingsFailure.SettingsTruncated)
                    diagnostics.Emit(CompositionStage.Validated, CompositionOutcome.Rejected,
                        "composition_diagnostics_truncated");
                throw settingsFailure;
            }
            attempt.CompleteOwnerStage(entry.Owner, CompositionStage.Validated);
            configurations.Add(entry.Owner, configuration);
        }
        var resolution = await callbacks.RunAsync(CompositionWork.Resolver, cancellation =>
            ValueTask.FromResult(CompositionResolver.Resolve(catalog, selected, context, cancellation,
                attempt.Clip(CompositionWork.Resolver))), token);
        if (resolution is CompositionResolution.Rejected rejected)
            Reject(rejected.Diagnostics);
        var composition = ((CompositionResolution.Accepted)resolution).Composition;
        var snapshot = new CompositionHealth(composition, attempt.TraceId).Snapshot;
        foreach (var observation in snapshot.Modules.Where(module => !module.DesiredEnabled))
        {
            attempt.Check();
            diagnostics.Emit(CompositionStage.Discovered, CompositionOutcome.Disabled,
                "composition_disabled", observation.Owner);
        }
        return new(composition, deployments, configurations);

        void Reject(IReadOnlyList<CompositionDiagnostic> rejected)
        {
            attempt.FailPhase(CompositionStage.Validated, CompositionOutcome.Rejected, rejected[0].Code);
            var known = CompositionSupervisor.CatalogOwners(catalog).ToHashSet();
            var prerequisites = CompositionSupervisor.CatalogPrerequisites(catalog).ToHashSet();
            var selectedOwners = selection.Modules.Select(module => module.Owner).ToHashSet();
            foreach (var diagnostic in rejected)
                diagnostics.Emit(CompositionStage.Validated, CompositionOutcome.Rejected, diagnostic.Code,
                    diagnostic.Owner is { } owner && (known.Contains(owner) ||
                        selectedOwners.Contains(owner) && diagnostic.Code is
                            "composition_implementation_missing" or "composition_configuration_invalid") ? owner : null,
                    diagnostic.RelatedIds.Where(id => known.Contains(id) ||
                        diagnostic.Code == "composition_dependency_missing" && prerequisites.Contains(id)).ToArray());
            throw new CompositionPreflightException(rejected);
        }
    }
}
