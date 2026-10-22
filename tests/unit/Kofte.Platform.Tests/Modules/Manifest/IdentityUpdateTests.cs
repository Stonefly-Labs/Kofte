using Kofte.Platform.Modules.Manifest;
using static Kofte.Platform.Tests.Modules.Manifest.ValidationResultTests;

namespace Kofte.Platform.Tests.Modules.Manifest;

public sealed class IdentityUpdateTests
{
    [Fact]
    public void MetadataAndReleaseDoNotRenameAndPriorIdIsExplicit()
    {
        var updated = Json.Replace("\"Notes\"", "\"Köfte Notes\"").Replace("0.1.0", "0.2.0");
        var accepted = Assert.IsType<ManifestValidationResult.Accepted>(Check(updated, previous: "org.example.notes"));
        Assert.Equal("org.example.notes", accepted.Description.Id.Value);
        Reject(Check(updated, previous: "org.example.other"), "identity_changed", "/id");
        Reject(Check(updated, previous: "ORG.example.notes"), "previous_id_invalid", "/previousModuleId");
    }

    [Fact]
    public void AcceptedGraphOwnsCollectionsAndSurvivesSubsequentCalls()
    {
        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures/full.json"));
        var description = Assert.IsType<ManifestValidationResult.Accepted>(Check(bytes)).Description;
        Array.Fill<byte>(bytes, 0);
        Assert.Throws<NotSupportedException>(() => ((IList<CapabilityDeclaration>)description.Capabilities).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<DependencyDeclaration>)description.Dependencies).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<PermissionDeclaration>)description.Permissions).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<string>)description.Version.Build).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<string>)description.Version.Prerelease).Clear());
        Reject(Check("{}"), "field_required", "");
        Assert.Equal(9, description.Capabilities.Count);
        Assert.Equal("org.example.absent", Assert.Single(description.Dependencies).Id.Value);
        Assert.Equal("notes.read", Assert.Single(description.Permissions).Name);
    }
}
