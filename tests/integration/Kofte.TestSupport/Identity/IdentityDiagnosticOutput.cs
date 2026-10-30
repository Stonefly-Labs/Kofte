using System.Text;
using System.Text.Json;

namespace Kofte.TestSupport.Identity;

public static class IdentityDiagnosticOutput
{
    public static bool IsSafeServingOutput(string output)
    {
        try
        {
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (Encoding.UTF8.GetByteCount(line) > 1024) return false;
                using var document = JsonDocument.Parse(line, new()
                { AllowDuplicateProperties = false, MaxDepth = 2 });
                var value = document.RootElement;
                if (value.ValueKind != JsonValueKind.Object || !value.EnumerateObject()
                    .Select(item => item.Name).ToHashSet(StringComparer.Ordinal).SetEquals(
                        ["version", "eventCode", "component", "operation", "mode", "provider", "outcome",
                         "correlationId", "guidance", "durability"]) ||
                    value.GetProperty("version").ValueKind != JsonValueKind.Number ||
                    !value.GetProperty("version").TryGetInt32(out var version) || version != 1 ||
                    value.EnumerateObject().Any(item => item.Name != "version" && item.Value.ValueKind != JsonValueKind.String))
                    return false;
                if (value.GetProperty("component").GetString() != "identity" ||
                    value.GetProperty("operation").GetString() is not ("startup" or "access") ||
                    value.GetProperty("mode").GetString() is not ("development" or "oidc") ||
                    value.GetProperty("provider").GetString() is not ("sqlite" or "postgresql") ||
                    value.GetProperty("durability").GetString() != "unchanged" ||
                    value.GetProperty("correlationId").GetString() is not { Length: 32 } correlation ||
                    !correlation.All(char.IsAsciiHexDigitLower)) return false;
                var result = (value.GetProperty("eventCode").GetString(), value.GetProperty("outcome").GetString(),
                    value.GetProperty("guidance").GetString());
                if (result is not (("identity_completed", "ready", "none") or
                    ("identity_request_invalid", "rejected", "correct_request"))) return false;
            }
            return true;
        }
        catch (JsonException) { return false; }
    }
}
