using System.Text.Json;
using System.Text.Json.Serialization;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.Modules.Composition;

public enum CompositionPhase { Admission, Deployment, Dependency, Configuration, Contribution, Storage }

public sealed class CompositionDiagnostic
{
    public string Code { get; }
    public string Message { get; }
    public string Guidance { get; }
    public CompositionPhase Phase { get; }
    public ModuleId? Owner { get; }
    public string Field { get; }
    public int Index { get; }
    public IReadOnlyList<ModuleId> RelatedIds { get; }
    public int OmittedRelatedCount { get; }
    public ManifestDiagnostic? ManifestDiagnostic { get; }
    [JsonIgnore] internal string SortKey { get; }

    internal CompositionDiagnostic(string code, CompositionPhase phase = CompositionPhase.Admission,
        ModuleId? owner = null, string field = "", int index = 0,
        IEnumerable<ModuleId>? related = null, ManifestDiagnostic? manifestDiagnostic = null, string sortKey = "")
    {
        (Message, Guidance) = code switch
        {
            "composition_input_limit" => ("Composition input exceeds a supported limit.", "reduce_composition_input"),
            "composition_configuration_invalid" => ("The module selection is invalid.", "correct_module_selection"),
            "composition_contract_unsupported" => ("The composition contract is unsupported.", "use_composition_contract_1"),
            "composition_manifest_rejected" => ("A deployed manifest was rejected.", "correct_deployed_manifest"),
            "composition_module_duplicate" => ("A module has duplicate deployed implementations.", "deploy_one_implementation"),
            "composition_implementation_missing" => ("A selected module is not deployed.", "deploy_selected_module"),
            "composition_dependency_missing" => ("A required dependency is not deployed.", "deploy_required_dependency"),
            "composition_dependency_disabled" => ("A required dependency is disabled.", "enable_required_dependency"),
            "composition_dependency_incompatible" => ("A dependency version is incompatible.", "deploy_compatible_dependency"),
            "composition_dependency_cycle" => ("Required dependencies contain a cycle.", "remove_dependency_cycle"),
            "composition_contribution_conflict" => ("A contribution declaration is invalid or conflicts.", "correct_contribution_declaration"),
            "composition_contribution_unsupported" => ("A requested contribution cannot execute.", "use_supported_declarations"),
            "composition_deadline_exceeded" => ("Composition resolution exceeded its deadline.", "retry_within_resolution_budget"),
            "composition_cancelled" => ("Composition resolution was cancelled.", "retry_new_attempt"),
            "composition_diagnostics_truncated" => ("Additional composition diagnostics were omitted.", "correct_reported_errors_and_retry"),
            _ => throw new ArgumentOutOfRangeException(nameof(code))
        };
        Code = code;
        Phase = phase;
        Owner = owner;
        Field = field;
        Index = index;
        var ids = (related ?? []).OrderBy(i => i.Value, StringComparer.Ordinal).ToArray();
        RelatedIds = Array.AsReadOnly(ids.Take(8).ToArray());
        OmittedRelatedCount = Math.Max(0, ids.Length - 8);
        ManifestDiagnostic = manifestDiagnostic;
        SortKey = sortKey;
    }

    internal static CompositionResolution.Rejected Reject(IEnumerable<CompositionDiagnostic> diagnostics)
    {
        var ordered = diagnostics.OrderBy(d => d.Phase)
            .ThenBy(d => d.Owner?.Value, StringComparer.Ordinal)
            .ThenBy(d => d.SortKey, StringComparer.Ordinal)
            .ThenBy(d => Array.IndexOf(ManifestReader.RootFields, d.Field))
            .ThenBy(d => d.Field, StringComparer.Ordinal).ThenBy(d => d.Index)
            .ThenBy(d => d.RelatedIds.FirstOrDefault()?.Value, StringComparer.Ordinal)
            .ThenBy(d => d.Code, StringComparer.Ordinal).ToArray();
        if (ordered.Length == 0) throw new ArgumentException("Rejection requires diagnostics.");
        if (ordered.Length <= 64 && JsonSerializer.SerializeToUtf8Bytes(ordered).Length <= 65_536)
            return new(Array.AsReadOnly(ordered));
        var marker = new CompositionDiagnostic("composition_diagnostics_truncated");
        var prefix = new List<CompositionDiagnostic>();
        foreach (var item in ordered.Take(63))
        {
            if (JsonSerializer.SerializeToUtf8Bytes(prefix.Append(item).Append(marker)).Length > 65_536) break;
            prefix.Add(item);
        }
        prefix.Add(marker);
        return new(prefix.AsReadOnly());
    }
}
