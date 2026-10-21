using System.Diagnostics.CodeAnalysis;

namespace Kofte.Platform.Modules.Manifest;

public sealed class SemanticVersion : IComparable<SemanticVersion>
{
    public string Value { get; }
    public string Major { get; }
    public string Minor { get; }
    public string Patch { get; }
    public IReadOnlyList<string> Prerelease { get; }
    public IReadOnlyList<string> Build { get; }

    private SemanticVersion(string value, string[] core, string[] prerelease, string[] build)
    {
        Value = value;
        Major = core[0];
        Minor = core[1];
        Patch = core[2];
        Prerelease = Array.AsReadOnly(prerelease);
        Build = Array.AsReadOnly(build);
    }

    public static bool TryParse(string? text, [NotNullWhen(true)] out SemanticVersion? version)
    {
        version = null;
        if (text is null || text.Length is < 5 or > 128) return false;
        var plus = text.IndexOf('+');
        var release = plus < 0 ? text : text[..plus];
        var build = plus < 0 ? [] : text[(plus + 1)..].Split('.');
        var dash = release.IndexOf('-');
        var core = (dash < 0 ? release : release[..dash]).Split('.');
        var prerelease = dash < 0 ? [] : release[(dash + 1)..].Split('.');
        if (core.Length != 3 || core.Any(p => !Numeric(p) || Padded(p))
            || !Identifiers(prerelease, true) || !Identifiers(build, false))
            return false;
        version = new(text, core, prerelease, build);
        return true;
    }

    private static bool Identifiers(string[] parts, bool prerelease)
    {
        foreach (var part in parts)
        {
            if (part.Length == 0 || part.Any(c => c is not (>= 'a' and <= 'z'
                or >= 'A' and <= 'Z' or >= '0' and <= '9' or '-'))) return false;
            if (prerelease && Numeric(part) && Padded(part)) return false;
        }
        return true;
    }
    private static bool Numeric(string text) => text.Length > 0 && text.All(c => c is >= '0' and <= '9');
    private static bool Padded(string text) => text.Length > 1 && text[0] == '0';
    private static int DecimalCompare(string a, string b) =>
        a.Length != b.Length ? a.Length.CompareTo(b.Length) : string.CompareOrdinal(a, b);

    public int CompareTo(SemanticVersion? other)
    {
        if (other is null) return 1;
        foreach (var (left, right) in new[] { (Major, other.Major), (Minor, other.Minor), (Patch, other.Patch) })
        {
            var compare = DecimalCompare(left, right);
            if (compare != 0) return compare;
        }
        if (Prerelease.Count == 0 || other.Prerelease.Count == 0)
            return (Prerelease.Count == 0).CompareTo(other.Prerelease.Count == 0);
        for (var i = 0; i < Math.Min(Prerelease.Count, other.Prerelease.Count); i++)
        {
            var left = Prerelease[i];
            var right = other.Prerelease[i];
            var leftNumeric = Numeric(left);
            var rightNumeric = Numeric(right);
            var compare = leftNumeric && rightNumeric ? DecimalCompare(left, right)
                : leftNumeric != rightNumeric ? (leftNumeric ? -1 : 1) : string.CompareOrdinal(left, right);
            if (compare != 0) return compare;
        }
        return Prerelease.Count.CompareTo(other.Prerelease.Count);
    }

    internal bool SameCore(SemanticVersion other) =>
        Major == other.Major && Minor == other.Minor && Patch == other.Patch;
    public override string ToString() => Value;
}
