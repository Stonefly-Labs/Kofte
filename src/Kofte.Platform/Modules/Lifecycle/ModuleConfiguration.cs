using System.Text;
using System.Text.Json;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.Modules.Lifecycle;

public enum ModuleSettingType { String, Number, Boolean, Object, Array }
public enum ModuleSettingFailureReason { Required, Type, Unknown, Constraint }

public sealed class ModuleConfigurationField
{
    public int ContractVersion => 1;
    public string Token { get; }
    public IReadOnlyList<string> Path { get; }
    public ModuleSettingType Type { get; }
    public bool Required { get; }
    public ModuleConfigurationField(string token, IEnumerable<string> path, ModuleSettingType type, bool required = false)
    {
        if (!ValidToken(token) || !Enum.IsDefined(type)) throw new ArgumentException("Invalid field definition.");
        var copy = CompositionCopies.Bounded(path, 8);
        if (copy.Count is < 1 or > 8 || copy.Any(p => !ValidToken(p)))
            throw new ArgumentException("Invalid field path.");
        (Token, Path, Type, Required) = (token, copy, type, required);
    }
    private static bool ValidToken(string? value) => value is { Length: > 0 and <= 64 }
        && value[0] is >= 'a' and <= 'z'
        && value.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_' or '.');
}

public sealed class ModuleConfiguration
{
    private readonly JsonElement settings;
    public int ContractVersion => 1;
    public ModuleId Owner { get; }
    public ModuleConfiguration(ModuleId owner, ReadOnlySpan<byte> utf8Settings)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (utf8Settings.Length > 16_384) throw new ArgumentException("Settings exceed the byte limit.");
        try
        {
            _ = new UTF8Encoding(false, true).GetCharCount(utf8Settings);
            using var document = JsonDocument.Parse(utf8Settings.ToArray(), new() { MaxDepth = 8 });
            if (document.RootElement.ValueKind != JsonValueKind.Object || !Unique(document.RootElement))
                throw new ArgumentException("Settings must be a strict object.");
            Owner = owner;
            settings = document.RootElement.Clone();
        }
        catch (JsonException) { throw new ArgumentException("Settings must be a strict object."); }
        catch (DecoderFallbackException) { throw new ArgumentException("Settings must use valid UTF-8."); }
        catch (InvalidOperationException) { throw new ArgumentException("Settings must use valid Unicode scalars."); }
    }
    private static bool Unique(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
                if (!names.Add(property.Name) || !Unique(property.Value)) return false;
        }
        else if (value.ValueKind == JsonValueKind.Array)
            foreach (var item in value.EnumerateArray()) if (!Unique(item)) return false;
        if (value.ValueKind == JsonValueKind.String) _ = value.GetString();
        return true;
    }
    public bool TryGetValue(ModuleConfigurationField field, out JsonElement value)
    {
        ArgumentNullException.ThrowIfNull(field);
        value = settings;
        foreach (var segment in field.Path)
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(segment, out value))
            {
                value = default;
                return false;
            }
        value = value.Clone();
        return true;
    }
    // Hosts validate unknown nested keys without exposing any other owner's settings.
    public JsonElement Snapshot() => settings.Clone();
}

public sealed class ModuleConfigurationFailure
{
    public ModuleConfigurationField Field { get; }
    public ModuleSettingFailureReason Reason { get; }
    public ModuleConfigurationFailure(ModuleConfigurationField field, ModuleSettingFailureReason reason)
    {
        ArgumentNullException.ThrowIfNull(field);
        if (!Enum.IsDefined(reason)) throw new ArgumentOutOfRangeException(nameof(reason));
        (Field, Reason) = (field, reason);
    }
}

public interface IModuleConfigurationValidator
{
    IEnumerable<ModuleConfigurationFailure> Validate(ModuleConfiguration configuration,
        CancellationToken cancellationToken);
}
