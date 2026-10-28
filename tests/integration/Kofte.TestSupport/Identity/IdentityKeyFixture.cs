using System.Security.Cryptography;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Kofte.Infrastructure.Storage;

namespace Kofte.TestSupport.Identity;

public static class IdentityKeyFixture
{
    public static void AgeRetainedKey(string directory, DateTimeOffset expiry)
    {
        var file = Directory.GetFiles(directory, "key-*.xml").Single();
        var markerPath = Path.Combine(directory, "identity-owner.json");
        UnixStorePaths.ValidateFile(file);
        UnixStorePaths.ValidateFile(markerPath);
        var xml = XElement.Load(file);
        var key = xml.Element("key") ?? throw new InvalidOperationException("identity_fixture_key_invalid");
        key.Element("creationDate")!.Value = expiry.AddDays(-90).ToString("O");
        key.Element("activationDate")!.Value = expiry.AddDays(-90).ToString("O");
        key.Element("expirationDate")!.Value = expiry.ToString("O");
        File.WriteAllText(file, xml.ToString(SaveOptions.DisableFormatting));
        var marker = JsonNode.Parse(File.ReadAllText(markerPath)) ??
            throw new InvalidOperationException("identity_fixture_key_invalid");
        marker["keys"]![Path.GetFileName(file)] =
            Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(file)));
        File.WriteAllText(markerPath, marker.ToJsonString());
    }
}
