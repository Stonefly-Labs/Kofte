using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.Modules.Composition;

internal static class ContributionValidator
{
    private static readonly Dictionary<string, string> Capabilities = new(StringComparer.Ordinal)
    {
        ["backend-service"] = "backend",
        ["route"] = "backend",
        ["resource"] = "resources",
        ["tool"] = "mcp-tools",
        ["frontend"] = "frontend",
        ["job"] = "jobs",
        ["event"] = "events",
        ["workflow"] = "workflows",
        ["provider"] = "providers"
    };

    internal static IReadOnlyList<ContributionReservation> Validate(
        IEnumerable<(ModuleDescription Description, ModuleDeployment Deployment)> modules,
        List<CompositionDiagnostic> errors, ResolutionWork work)
    {
        var result = new List<ContributionReservation>();
        var identities = new HashSet<(string Kind, string Owner, string Name)>();
        var serviceTypes = new HashSet<Type>();
        var routes = new List<(string Method, string[] Segments)>();
        foreach (var (module, deployment) in modules)
            foreach (var claim in deployment.Claims)
            {
                work.Check();
                var unsupported = claim is not null && claim.Mode == ContributionMode.Executable && claim.Kind != "backend-service";
                var capability = claim is not null && claim.Kind is not null ? Capabilities.GetValueOrDefault(claim.Kind) : null;
                var permission = claim?.Kind == "permission"
                    ? module.Permissions.FirstOrDefault(p => p.Name == claim.LocalName) : null;
                var valid = claim is not null && claim.Owner == module.Id.Value && Enum.IsDefined(claim.Mode)
                    && ValidName(claim.LocalName, claim.Kind == "permission" ? 64 : 128)
                    && SemanticVersion.TryParse(claim.ContractVersion, out var parsedVersion)
                    && parsedVersion.Major == "1" && parsedVersion.Minor == "0" && parsedVersion.Patch == "0"
                    && parsedVersion.Prerelease.Count == 0
                    && (claim.Kind == "permission" ? permission is not null
                        : capability is not null && module.Capabilities.Any(c => c.Kind == capability));
                if (unsupported)
                {
                    errors.Add(new("composition_contribution_unsupported", CompositionPhase.Contribution, module.Id));
                    continue;
                }
                if (!valid)
                {
                    errors.Add(new("composition_contribution_conflict", CompositionPhase.Contribution, module.Id));
                    continue;
                }
                var acceptedClaim = claim!;
                var key = (acceptedClaim.Kind!, module.Id.Value, acceptedClaim.LocalName!);
                if (!identities.Add(key)) valid = false;
                string? normalized = null;
                if (acceptedClaim.Kind == "route")
                {
                    if (!TryRoute(acceptedClaim.Method, acceptedClaim.Route, out var segments)) valid = false;
                    else
                    {
                        normalized = "/" + string.Join('/', segments);
                        if (Reserved(segments) || routes.Any(r => r.Method == acceptedClaim.Method
                            && Overlap(r.Segments, segments))) valid = false;
                        routes.Add((acceptedClaim.Method!, segments));
                    }
                    if (acceptedClaim.ServiceType is not null) valid = false;
                }
                else if (acceptedClaim.Method is not null || acceptedClaim.Route is not null) valid = false;
                if (acceptedClaim.Kind == "backend-service")
                {
                    var type = acceptedClaim.ServiceType;
                    if (type is null || !deployment.OwnedServiceTypes.Contains(type) || type.ContainsGenericParameters
                        || !(type.IsClass || type.IsInterface) || ReservedType(type) || !serviceTypes.Add(type)) valid = false;
                }
                else if (acceptedClaim.ServiceType is not null) valid = false;
                if (!valid)
                {
                    errors.Add(new("composition_contribution_conflict", CompositionPhase.Contribution, module.Id));
                    continue;
                }
                SemanticVersion.TryParse(acceptedClaim.ContractVersion, out var version);
                result.Add(new(module.Id, acceptedClaim, version!, normalized, permission?.Description));
            }
        return result.OrderBy(r => r.Kind, StringComparer.Ordinal).ThenBy(r => r.Owner.Value, StringComparer.Ordinal)
            .ThenBy(r => r.LocalName, StringComparer.Ordinal).ToArray();
    }

    private static bool ReservedType(Type type)
    {
        var assembly = type.Assembly.GetName().Name;
        return assembly is "Kofte.Platform" or "Kofte.Host" or "Kofte.Infrastructure" or "System.Private.CoreLib"
            || assembly?.StartsWith("Microsoft.", StringComparison.Ordinal) == true
            || assembly?.StartsWith("System.", StringComparison.Ordinal) == true;
    }

    private static bool ValidName(string? value, int limit)
    {
        if (value is null || value.Length is < 1 || value.Length > limit) return false;
        var start = true;
        foreach (var c in value)
        {
            if (c is '.' or '-')
            {
                if (start) return false;
                start = true;
            }
            else
            {
                if (start ? c is < 'a' or > 'z' : c is not (>= 'a' and <= 'z' or >= '0' and <= '9')) return false;
                start = false;
            }
        }
        return !start;
    }

    private static bool TryRoute(string? method, string? route, out string[] segments)
    {
        segments = [];
        if (method is not ("GET" or "POST" or "PUT" or "PATCH" or "DELETE")
            || route is null || route.Length is < 2 or > 256 || route[0] != '/') return false;
        var parts = route[1..].Split('/');
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.StartsWith('{') && part.EndsWith('}'))
            {
                if (!Literal(part[1..^1])) return false;
                parts[i] = "{}";
            }
            else if (!Literal(part)) return false;
        }
        segments = parts;
        return true;
    }
    private static bool Literal(string value) => value.Length > 0 && value[0] is >= 'a' and <= 'z'
        && value.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-');
    private static bool Overlap(string[] a, string[] b) => a.Length == b.Length
        && a.Zip(b).All(pair => pair.First == pair.Second || pair.First == "{}" || pair.Second == "{}");
    private static bool Reserved(string[] segments) =>
        segments[0] is "health" or "openapi" or "{}" ||
        segments.Length >= 2 && segments[0] is "api" or "{}" && segments[1] is "kofte" or "{}";
}
