using System.Text.Json;

namespace Kofte.Platform.Modules.Manifest;

internal static class MetadataValidation
{
    internal static bool Text(string value, int limit)
    {
        var count = 0;
        var nonWhitespace = false;
        foreach (var rune in value.EnumerateRunes())
        {
            if (++count > limit || rune.Value is <= 31 or >= 127 and <= 159) return false;
            if (!System.Text.Rune.IsWhiteSpace(rune)) nonWhitespace = true;
        }
        return count != 0 && nonWhitespace;
    }
    private static bool PermissionName(string value)
    {
        if (value.Length is < 1 or > 64) return false;
        var needsLetter = true;
        foreach (var character in value)
        {
            if (character is >= 'a' and <= 'z') needsLetter = false;
            else if (character is >= '0' and <= '9' && !needsLetter) { }
            else if (character is '.' or '-' && !needsLetter) needsLetter = true;
            else return false;
        }
        return !needsLetter;
    }
    internal static List<PermissionDeclaration> Permissions(JsonElement array, DiagnosticList errors)
    {
        var result = new List<PermissionDeclaration>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var index = 0;
        foreach (var item in array.EnumerateArray())
        {
            var path = "/permissions/" + (index++).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var name = item.GetProperty("name").GetString()!;
            var description = item.GetProperty("description").GetString()!;
            var valid = PermissionName(name);
            if (!valid) errors.Add("permission_invalid", path + "/name");
            else if (!seen.Add(name)) errors.Add("permission_duplicate", path + "/name");
            var textValid = Text(description, 256);
            if (!textValid) errors.Add("permission_invalid", path + "/description");
            if (valid && textValid) result.Add(new(name, description));
        }
        return result;
    }
}
