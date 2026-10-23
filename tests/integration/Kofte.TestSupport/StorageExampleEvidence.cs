using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kofte.TestSupport;

public sealed record StorageExampleEvidence(int ProcessRestarts, int RetainedAssignments, int RetainedSentinels,
    int RegisteredOwnerConflicts, int PreservedInventories, int UnallocatedContenders, int UnownedTargetsPreserved)
{
    public static IReadOnlyDictionary<string, StorageExampleEvidence> Read(JsonElement value,
        IEnumerable<string> selected)
    {
        var expected = selected.Where(name => name is "same-id-retry" or "collision").ToArray();
        var properties = value.EnumerateObject().ToArray();
        if (!properties.Select(item => item.Name).SequenceEqual(expected))
            throw new InvalidOperationException("storage_scenario_observation_missing");
        var result = new Dictionary<string, StorageExampleEvidence>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            var evidence = property.Value.Deserialize<StorageExampleEvidence>(new JsonSerializerOptions
            {
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                RespectRequiredConstructorParameters = true,
                AllowDuplicateProperties = false
            });
            var required = property.Name == "same-id-retry" ? new(3, 6, 6, 0, 0, 0, 0)
                : new StorageExampleEvidence(0, 1, 1, 1, 1, 1, 1);
            if (evidence != required)
                throw new InvalidOperationException("storage_scenario_observation_missing");
            result.Add(property.Name, evidence);
        }
        return result;
    }
}
