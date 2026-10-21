namespace Kofte.Platform.Modules.Manifest;

internal static class CompatibilityValidation
{
    internal static SemanticVersion? Host(ManifestValidationContext? context, DiagnosticList errors)
    {
        var text = context?.HostContractVersion;
        if (string.IsNullOrEmpty(text))
            errors.Add("host_context_required", "/hostContractVersion", "context");
        else if (SemanticVersion.TryParse(text, out var host)) return host;
        else errors.Add("host_context_invalid", "/hostContractVersion", "context");
        return null;
    }
}
