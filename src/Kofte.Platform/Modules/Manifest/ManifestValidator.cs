using System.Globalization;
using System.Text.Json;

namespace Kofte.Platform.Modules.Manifest;

public static class ManifestValidator
{
    public static ManifestValidationResult Validate(ReadOnlySpan<byte> utf8Json, ManifestValidationContext? context)
    {
        var fatal = ManifestReader.Read(utf8Json, out var document);
        if (fatal is not null) return new ManifestValidationResult.Rejected([fatal]);
        using var parsed = document!;
        var root = parsed.RootElement;
        var errors = new DiagnosticList();
        if (!root.TryGetProperty("schemaVersion", out var schema))
        {
            errors.Add("field_required");
            return errors.Reject();
        }
        if (schema.ValueKind != JsonValueKind.String)
        {
            errors.Add("field_type", "/schemaVersion");
            return errors.Reject();
        }
        if (schema.GetString() != "1")
        {
            errors.Add("schema_unsupported", "/schemaVersion");
            return errors.Reject();
        }
        Shape(root, "", ManifestReader.RootFields, errors);
        if (errors.Any) return errors.Reject();
        var host = CompatibilityValidation.Host(context, errors);
        var prior = IdentityValidation.Prior(context, errors);
        if (!ModuleId.TryParse(root.GetProperty("id").GetString(), out var id))
            errors.Add("module_id_invalid", "/id");
        if (!SemanticVersion.TryParse(root.GetProperty("version").GetString(), out var version))
            errors.Add("version_invalid", "/version");
        var display = root.GetProperty("displayName").GetString()!;
        if (!MetadataValidation.Text(display, 128)) errors.Add("display_name_invalid", "/displayName");
        if (!VersionRange.TryParse(root.GetProperty("hostContract").GetString(), out var range))
            errors.Add("range_invalid", "/hostContract");
        IdentityValidation.Compare(id, prior, errors);
        var capabilities = DeclarationValidation.Capabilities(root.GetProperty("capabilities"), errors);
        var dependencies = DeclarationValidation.Dependencies(root.GetProperty("dependencies"), id, errors);
        var permissions = MetadataValidation.Permissions(root.GetProperty("permissions"), errors);
        if (errors.Any) return errors.Reject();
        if (!range!.Contains(host!))
        {
            errors.Add("host_incompatible", "/hostContract");
            return errors.Reject();
        }
        return new ManifestValidationResult.Accepted(new(id!, version!, display, range,
            capabilities, dependencies, permissions,
            new(root.GetProperty("storage").GetProperty("relational").GetBoolean())));
    }

    private static void Shape(JsonElement obj, string path, string[] fields, DiagnosticList errors)
    {
        foreach (var field in fields)
            if (!obj.TryGetProperty(field, out _)) errors.Add("field_required", path);
        foreach (var property in obj.EnumerateObject())
        {
            if (!fields.Contains(property.Name, StringComparer.Ordinal))
            {
                errors.Add("field_unknown", path);
                continue;
            }
            var child = path + "/" + property.Name;
            var value = property.Value;
            var expected = property.Name switch
            {
                "capabilities" or "dependencies" or "permissions" => JsonValueKind.Array,
                "storage" => JsonValueKind.Object,
                "relational" => JsonValueKind.True,
                _ => JsonValueKind.String
            };
            if (value.ValueKind != expected && !(expected == JsonValueKind.True && value.ValueKind == JsonValueKind.False))
            {
                errors.Add("field_type", child);
                continue;
            }
            if (expected == JsonValueKind.Object) Shape(value, child, ["relational"], errors);
            if (expected != JsonValueKind.Array) continue;
            var index = 0;
            foreach (var item in value.EnumerateArray())
            {
                var itemPath = child + "/" + (index++).ToString(CultureInfo.InvariantCulture);
                if (item.ValueKind != JsonValueKind.Object) errors.Add("field_type", itemPath);
                else Shape(item, itemPath, property.Name switch
                {
                    "capabilities" => ["kind", "contractVersion"],
                    "dependencies" => ["id", "versionRange"],
                    _ => ["name", "description"]
                }, errors);
            }
        }
    }
}
