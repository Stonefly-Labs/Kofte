using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Host.Features.ModuleComposition;

public sealed record CompositionSettingDiagnostic(ModuleId Owner, string? Field, ModuleSettingFailureReason Reason);

public sealed class CompositionPreflightException : CompositionBoundaryException
{
    public IReadOnlyList<CompositionDiagnostic> Diagnostics { get; }
    public IReadOnlyList<CompositionSettingDiagnostic> Settings { get; }
    public bool SettingsTruncated { get; }

    internal CompositionPreflightException(IReadOnlyList<CompositionDiagnostic> diagnostics)
        : base(diagnostics[0].Code)
    {
        if (diagnostics.Count is < 1 or > 64)
            throw new ArgumentOutOfRangeException(nameof(diagnostics));
        Diagnostics = Array.AsReadOnly(diagnostics.ToArray());
        Settings = Array.AsReadOnly(Array.Empty<CompositionSettingDiagnostic>());
    }

    internal CompositionPreflightException(ModuleId owner, IReadOnlyList<ModuleSettingIssue> issues)
        : base("composition_setting_invalid")
    {
        Diagnostics = Array.AsReadOnly(Array.Empty<CompositionDiagnostic>());
        SettingsTruncated = issues.Count > 64;
        Settings = Array.AsReadOnly(issues.Take(SettingsTruncated ? 63 : 64)
            .Select(issue => new CompositionSettingDiagnostic(owner, issue.Field, issue.Reason)).ToArray());
    }
}
