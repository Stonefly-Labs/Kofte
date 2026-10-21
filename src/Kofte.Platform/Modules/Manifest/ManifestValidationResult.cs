namespace Kofte.Platform.Modules.Manifest;

public abstract class ManifestValidationResult
{
    private ManifestValidationResult() { }

    public sealed class Accepted : ManifestValidationResult
    {
        public ModuleDescription Description { get; }
        internal Accepted(ModuleDescription description) => Description = description;
    }

    public sealed class Rejected : ManifestValidationResult
    {
        public IReadOnlyList<ManifestDiagnostic> Diagnostics { get; }
        internal Rejected(IEnumerable<ManifestDiagnostic> diagnostics)
        {
            var copy = diagnostics.ToArray();
            if (copy.Length is < 1 or > 32)
                throw new ArgumentOutOfRangeException(nameof(diagnostics));
            Diagnostics = Array.AsReadOnly(copy);
        }
    }
}
