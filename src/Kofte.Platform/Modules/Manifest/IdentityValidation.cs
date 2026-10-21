namespace Kofte.Platform.Modules.Manifest;

internal static class IdentityValidation
{
    internal static ModuleId? Prior(ManifestValidationContext? context, DiagnosticList errors)
    {
        if (context?.PreviousModuleId is not { } text) return null;
        if (ModuleId.TryParse(text, out var prior)) return prior;
        errors.Add("previous_id_invalid", "/previousModuleId", "context");
        return null;
    }

    internal static void Compare(ModuleId? candidate, ModuleId? prior, DiagnosticList errors)
    {
        if (candidate is not null && prior is not null && !candidate.Equals(prior))
            errors.Add("identity_changed", "/id");
    }
}
