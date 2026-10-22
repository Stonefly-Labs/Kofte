using System.Globalization;

namespace Kofte.Platform.Modules.Manifest;

public sealed class ManifestDiagnostic
{
    public string Code { get; }
    public string Origin { get; }
    public string Path { get; }
    public string Message { get; }

    internal ManifestDiagnostic(string code, string path = "", string origin = "manifest")
    {
        Code = code;
        Path = path;
        Origin = origin;
        Message = code switch
        {
            "input_too_large" => "Reduce the manifest to at most 65536 bytes.",
            "encoding_invalid" => "Use well-formed UTF-8 and Unicode scalars without a BOM.",
            "document_invalid" => "Supply one strict JSON object without comments or trailing commas.",
            "field_duplicate" => "Declare each object field once.",
            "depth_limit" => "Reduce JSON nesting to at most eight containers.",
            "collection_limit" => "Reduce entries to the documented collection limit.",
            "field_required" => "Supply all required fields in this object.",
            "field_type" => "Use the documented JSON type for this field.",
            "field_unknown" => "Remove undeclared fields from this object.",
            "schema_unsupported" => "Use manifest schema version 1.",
            "module_id_invalid" => "Use a canonical lowercase reverse-DNS module ID.",
            "identity_changed" => "Keep the previously accepted module ID unchanged.",
            "previous_id_invalid" => "Supply a canonical previously accepted module ID.",
            "version_invalid" => "Supply a full semantic version within the documented limit.",
            "range_invalid" => "Use an exact semantic version or a nonempty lower and upper comparator pair.",
            "host_context_required" => "Supply an explicit host-contract semantic version.",
            "host_context_invalid" => "Supply a valid full host-contract semantic version.",
            "display_name_invalid" => "Supply a nonblank display name within the scalar limit without controls.",
            "capability_unknown" => "Use a documented capability kind.",
            "capability_unsupported" => "Use stable capability contract version 1.0.0.",
            "capability_duplicate" => "Declare each capability kind once.",
            "dependency_duplicate" => "Declare each dependency module once.",
            "dependency_self" => "Remove the dependency on the declaring module.",
            "permission_invalid" => "Use a documented local permission name and nonblank description without controls.",
            "permission_duplicate" => "Declare each local permission name once.",
            "host_incompatible" => "Target a compatible host-contract version.",
            "diagnostics_truncated" => "Fix the reported fields and retry to see remaining errors.",
            _ => throw new ArgumentOutOfRangeException(nameof(code))
        };
    }
}

internal sealed class DiagnosticList
{
    private readonly List<ManifestDiagnostic> items = [];
    internal bool Any => items.Count != 0;
    internal void Add(string code, string path = "", string origin = "manifest") =>
        items.Add(new(code, path, origin));
    internal ManifestValidationResult.Rejected Reject()
    {
        var ordered = items.OrderBy(d => d, DiagnosticComparer.Instance).ToArray();
        return new(ordered.Length <= 32 ? ordered :
            ordered.Take(31).Append(new ManifestDiagnostic("diagnostics_truncated")));
    }

    private sealed class DiagnosticComparer : IComparer<ManifestDiagnostic>
    {
        internal static readonly DiagnosticComparer Instance = new();
        private static readonly string[] Fields = [
            "schemaVersion", "id", "version", "displayName", "hostContract",
            "capabilities", "dependencies", "permissions", "storage",
            "kind", "contractVersion", "versionRange", "name", "description", "relational"
        ];
        public int Compare(ManifestDiagnostic? x, ManifestDiagnostic? y)
        {
            ArgumentNullException.ThrowIfNull(x);
            ArgumentNullException.ThrowIfNull(y);
            var origin = string.CompareOrdinal(x.Origin, y.Origin);
            if (origin != 0) return origin;
            var left = x.Path.Split('/');
            var right = y.Path.Split('/');
            for (var i = 1; i < Math.Min(left.Length, right.Length); i++)
            {
                var comparison = Rank(left[i]).CompareTo(Rank(right[i]));
                if (comparison != 0) return comparison;
            }
            var length = left.Length.CompareTo(right.Length);
            return length != 0 ? length : string.CompareOrdinal(x.Code, y.Code);
        }
        private static int Rank(string part) => part switch
        {
            "hostContractVersion" => -2,
            "previousModuleId" => -1,
            _ => int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var index)
                ? index : Array.IndexOf(Fields, part)
        };
    }
}
