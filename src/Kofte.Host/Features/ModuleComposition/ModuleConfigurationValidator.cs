using System.Text.Json;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;

namespace Kofte.Host.Features.ModuleComposition;

public sealed record ModuleSettingIssue(string? Field, ModuleSettingFailureReason Reason);

public static class ModuleConfigurationValidator
{
    public static IReadOnlyList<ModuleSettingIssue> Validate(ModuleDeployment deployment,
        ModuleConfiguration configuration, CancellationToken token)
    {
        var fields = deployment.ConfigurationFields;
        if (fields.Count > 128 || fields.Select(f => f.Token).Distinct(StringComparer.Ordinal).Count() != fields.Count ||
            fields.Select(f => string.Join("/", f.Path)).Distinct(StringComparer.Ordinal).Count() != fields.Count)
            throw Invalid();
        var failures = new List<ModuleSettingIssue>();
        foreach (var field in fields)
        {
            token.ThrowIfCancellationRequested();
            if (!configuration.TryGetValue(field, out var value))
            {
                if (field.Required) failures.Add(new(field.Token, ModuleSettingFailureReason.Required));
            }
            else if (!Matches(field.Type, value.ValueKind))
                failures.Add(new(field.Token, ModuleSettingFailureReason.Type));
        }
        Unknown(configuration.Snapshot(), fields, [], null, failures);
        if (failures.Count == 0 && deployment.ConfigurationValidator is { } validator)
        {
            using var iterator = validator.Validate(configuration, token).GetEnumerator();
            while (iterator.MoveNext())
            {
                token.ThrowIfCancellationRequested();
                if (failures.Count == 128 || iterator.Current is not { } failure ||
                    !fields.Contains(failure.Field))
                    throw Invalid();
                failures.Add(new(failure.Field.Token, failure.Reason));
            }
        }
        return Array.AsReadOnly(failures.Distinct().OrderBy(f => f.Field, StringComparer.Ordinal)
            .ThenBy(f => f.Reason).ToArray());
    }

    private static void Unknown(JsonElement current, IReadOnlyList<ModuleConfigurationField> fields,
        IReadOnlyList<string> path, string? containing, List<ModuleSettingIssue> failures)
    {
        if (current.ValueKind != JsonValueKind.Object) return;
        foreach (var property in current.EnumerateObject())
        {
            var next = path.Append(property.Name).ToArray();
            var matching = fields.Where(f => f.Path.Count >= next.Length &&
                f.Path.Take(next.Length).SequenceEqual(next)).ToArray();
            if (matching.Length == 0)
            {
                failures.Add(new(containing, ModuleSettingFailureReason.Unknown));
                continue;
            }
            var parent = matching.FirstOrDefault(f => f.Path.Count == next.Length);
            Unknown(property.Value, fields, next, parent?.Token ?? containing, failures);
        }
    }

    private static bool Matches(ModuleSettingType type, JsonValueKind kind) => type switch
    {
        ModuleSettingType.String => kind == JsonValueKind.String,
        ModuleSettingType.Number => kind == JsonValueKind.Number,
        ModuleSettingType.Boolean => kind is JsonValueKind.True or JsonValueKind.False,
        ModuleSettingType.Object => kind == JsonValueKind.Object,
        ModuleSettingType.Array => kind == JsonValueKind.Array,
        _ => false
    };

    private static CompositionBoundaryException Invalid() => new("composition_setting_invalid");
}
