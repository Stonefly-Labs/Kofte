using System.Text;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.Tests.Modules.Manifest;

public sealed class ValidationResultTests
{
    internal static byte[] Minimal => File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures/minimal.json"));
    internal static ManifestValidationResult Check(string json, string? host = "1.0.0", string? previous = null) =>
        ManifestValidator.Validate(Encoding.UTF8.GetBytes(json), new(host, previous));
    internal static ManifestValidationResult Check(byte[] bytes) => ManifestValidator.Validate(bytes, new("1.0.0"));
    internal static string Json => Encoding.UTF8.GetString(Minimal);
    internal static ManifestValidationResult.Rejected Reject(ManifestValidationResult result, string code, string path)
    {
        var rejected = Assert.IsType<ManifestValidationResult.Rejected>(result);
        Assert.Contains(rejected.Diagnostics, d => d.Code == code && d.Path == path);
        return rejected;
    }

    [Fact]
    public void OutcomesAreDisjointAndRejectionCannotBeMutated()
    {
        var accepted = Assert.IsType<ManifestValidationResult.Accepted>(Check(Minimal));
        var rejected = Reject(Check("{}"), "field_required", "");
        var list = Assert.IsAssignableFrom<IList<ManifestDiagnostic>>(rejected.Diagnostics);
        Assert.Throws<NotSupportedException>(() => list.Clear());
        Assert.Equal("org.example.notes", accepted.Description.Id.Value);
        Assert.NotEmpty(rejected.Diagnostics);
    }

    [Fact]
    public void CollectedDiagnosticsAreContextThenSchemaOrderNotInputOrder()
    {
        var json = Json.Replace("\"Notes\"", "\" \"").Replace("\"org.example.notes\"", "\"BAD\"");
        var rejected = Reject(Check(json, "1.0", "BAD"), "module_id_invalid", "/id");
        Assert.Equal(new[] {
            ("context", "/hostContractVersion", "host_context_invalid"),
            ("context", "/previousModuleId", "previous_id_invalid"),
            ("manifest", "/id", "module_id_invalid"),
            ("manifest", "/displayName", "display_name_invalid")
        }, rejected.Diagnostics.Select(d => (d.Origin, d.Path, d.Code)));
        Assert.All(rejected.Diagnostics, d => Assert.DoesNotContain("BAD", d.Message));
    }
}
