using System.Text.Json;

namespace Kofte.Platform.Modules.Manifest;

internal static class DeclarationValidation
{
    private static readonly HashSet<string> Kinds = new(StringComparer.Ordinal) {
        "backend", "frontend", "resources", "providers", "events", "workflows", "jobs", "permissions", "mcp-tools"
    };
    internal static List<CapabilityDeclaration> Capabilities(JsonElement array, DiagnosticList errors)
    {
        var result = new List<CapabilityDeclaration>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;
        foreach (var item in array.EnumerateArray())
        {
            var path = "/capabilities/" + (index++).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var kind = item.GetProperty("kind").GetString()!;
            var known = Kinds.Contains(kind);
            if (!known) errors.Add("capability_unknown", path + "/kind");
            if (!seen.Add(kind)) errors.Add("capability_duplicate", path + "/kind");
            var valid = SemanticVersion.TryParse(item.GetProperty("contractVersion").GetString(), out var version);
            if (!valid) errors.Add("version_invalid", path + "/contractVersion");
            else if (known && (version!.Major != "1" || version.Minor != "0" || version.Patch != "0"
                || version.Prerelease.Count != 0))
                errors.Add("capability_unsupported", path + "/contractVersion");
            if (known && valid) result.Add(new(kind, version!));
        }
        return result;
    }

    internal static List<DependencyDeclaration> Dependencies(JsonElement array, ModuleId? owner, DiagnosticList errors)
    {
        var result = new List<DependencyDeclaration>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;
        foreach (var item in array.EnumerateArray())
        {
            var path = "/dependencies/" + (index++).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var valid = ModuleId.TryParse(item.GetProperty("id").GetString(), out var id);
            if (!valid) errors.Add("module_id_invalid", path + "/id");
            else
            {
                if (!seen.Add(id!.Value)) errors.Add("dependency_duplicate", path + "/id");
                if (id.Equals(owner)) errors.Add("dependency_self", path + "/id");
            }
            var rangeValid = VersionRange.TryParse(item.GetProperty("versionRange").GetString(), out var range);
            if (!rangeValid) errors.Add("range_invalid", path + "/versionRange");
            if (valid && rangeValid) result.Add(new(id!, range!));
        }
        return result;
    }
}
