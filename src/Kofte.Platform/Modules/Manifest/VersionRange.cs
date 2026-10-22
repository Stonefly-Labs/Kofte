using System.Diagnostics.CodeAnalysis;

namespace Kofte.Platform.Modules.Manifest;

public sealed class VersionRange
{
    public string Value { get; }
    public SemanticVersion Lower { get; }
    public SemanticVersion Upper { get; }
    public bool LowerInclusive { get; }
    public bool UpperInclusive { get; }
    public bool IsExact { get; }

    private VersionRange(string value, SemanticVersion lower, SemanticVersion upper,
        bool lowerInclusive, bool upperInclusive, bool exact) =>
        (Value, Lower, Upper, LowerInclusive, UpperInclusive, IsExact) =
        (value, lower, upper, lowerInclusive, upperInclusive, exact);

    public static bool TryParse(string? text, [NotNullWhen(true)] out VersionRange? range)
    {
        range = null;
        if (text is null || text.Length is < 5 or > 512 || text.Any(c => c > 127)) return false;
        if (SemanticVersion.TryParse(text, out var exact))
        {
            range = new(text, exact, exact, true, true, true);
            return true;
        }
        var parts = text.Split(' ');
        if (parts.Length != 2 || !parts[0].StartsWith('>') || !parts[1].StartsWith('<')) return false;
        var lowerInclusive = parts[0].StartsWith(">=", StringComparison.Ordinal);
        var upperInclusive = parts[1].StartsWith("<=", StringComparison.Ordinal);
        if (!SemanticVersion.TryParse(parts[0][(lowerInclusive ? 2 : 1)..], out var lower)
            || !SemanticVersion.TryParse(parts[1][(upperInclusive ? 2 : 1)..], out var upper))
            return false;
        var compare = lower.CompareTo(upper);
        if (compare > 0 || compare == 0 && !(lowerInclusive && upperInclusive)) return false;
        range = new(text, lower, upper, lowerInclusive, upperInclusive, false);
        return true;
    }

    public bool Contains(SemanticVersion target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (target.Prerelease.Count != 0 && !(Lower.Prerelease.Count != 0 && target.SameCore(Lower))
            && !(Upper.Prerelease.Count != 0 && target.SameCore(Upper))) return false;
        var lower = target.CompareTo(Lower);
        var upper = target.CompareTo(Upper);
        return (lower > 0 || lower == 0 && LowerInclusive) && (upper < 0 || upper == 0 && UpperInclusive);
    }
    public override string ToString() => Value;
}
