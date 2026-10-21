using System.Diagnostics.CodeAnalysis;

namespace Kofte.Platform.Modules.Manifest;

public sealed class ModuleId : IEquatable<ModuleId>
{
    public string Value { get; }
    private ModuleId(string value) => Value = value;

    public static bool TryParse(string? text, [NotNullWhen(true)] out ModuleId? id)
    {
        id = null;
        if (text is null || text.Length is < 5 or > 253) return false;
        var labels = 0;
        var start = 0;
        for (var i = 0; i <= text.Length; i++)
        {
            if (i != text.Length && text[i] != '.')
            {
                if (!AlphaNumeric(text[i]) && text[i] != '-') return false;
                continue;
            }
            var length = i - start;
            if (length is < 1 or > 63 || !AlphaNumeric(text[start]) || !AlphaNumeric(text[i - 1]))
                return false;
            labels++;
            start = i + 1;
        }
        if (labels < 3) return false;
        id = new(text);
        return true;
    }

    private static bool AlphaNumeric(char value) => value is >= 'a' and <= 'z' or >= '0' and <= '9';
    public bool Equals(ModuleId? other) => other is not null && string.Equals(Value, other.Value, StringComparison.Ordinal);
    public override bool Equals(object? obj) => obj is ModuleId other && Equals(other);
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
    public override string ToString() => Value;
}
