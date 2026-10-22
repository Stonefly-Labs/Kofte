namespace Kofte.Platform.Modules.Manifest;

public sealed class ManifestValidationContext(string? hostContractVersion, string? previousModuleId = null)
{
    public string? HostContractVersion { get; } = hostContractVersion;
    public string? PreviousModuleId { get; } = previousModuleId;
}
