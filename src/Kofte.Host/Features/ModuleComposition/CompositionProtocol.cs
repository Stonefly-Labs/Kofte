using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using Kofte.Infrastructure.Identity;
using Kofte.Platform.Modules.Lifecycle;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Host.Features.ModuleComposition;

public sealed class CompositionProtocol
{
    private const int MaximumCatalogPrerequisites = 32 * 64;
    private readonly string attempt, snapshot;
    private readonly HashSet<string> owners;
    private readonly HashSet<string> selectedOwners;
    private readonly HashSet<string> prerequisiteOwners;
    private readonly HashSet<string> enabledOwners;
    private readonly CompositionMode mode;
    private readonly SemaphoreSlim writes = new(1, 1);
    private readonly Dictionary<string, HashSet<string>> fieldTokens = new(StringComparer.Ordinal);

    public CompositionProtocol(string attempt, string snapshot, IEnumerable<ModuleId> owners, CompositionMode mode,
        IReadOnlyDictionary<ModuleId, IReadOnlyList<ModuleConfigurationField>>? configurationFields = null,
        IEnumerable<ModuleId>? selectedOwners = null, IEnumerable<ModuleId>? prerequisiteOwners = null,
        IEnumerable<ModuleId>? enabledOwners = null)
    {
        if (!CompositionDiagnostics.IsToken(attempt) || !CompositionDiagnostics.IsToken(snapshot) || !Enum.IsDefined(mode))
            throw Invalid();
        this.attempt = attempt;
        this.snapshot = snapshot;
        this.mode = mode;
        var catalog = owners.Take(33).ToArray();
        this.owners = catalog.Select(owner => owner.Value).ToHashSet(StringComparer.Ordinal);
        if (catalog.Length > 32 || this.owners.Count != catalog.Length) throw Invalid();
        var selected = (selectedOwners ?? []).Take(33).ToArray();
        if (selected.Length > 32 || selected.Any(owner => owner is null)) throw Invalid();
        this.selectedOwners = selected.Select(owner => owner.Value).ToHashSet(StringComparer.Ordinal);
        if (this.selectedOwners.Count != selected.Length) throw Invalid();
        var prerequisites = (prerequisiteOwners ?? []).Take(MaximumCatalogPrerequisites + 1).ToArray();
        if (prerequisites.Length > MaximumCatalogPrerequisites || prerequisites.Any(owner => owner is null)) throw Invalid();
        this.prerequisiteOwners = prerequisites.Select(owner => owner.Value).ToHashSet(StringComparer.Ordinal);
        var enabled = (enabledOwners ?? []).Take(33).ToArray();
        if (enabled.Length > 32 || enabled.Any(owner => owner is null)) throw Invalid();
        this.enabledOwners = enabled.Select(owner => owner.Value).ToHashSet(StringComparer.Ordinal);
        if (this.enabledOwners.Count != enabled.Length) throw Invalid();
        var declarations = (configurationFields ??
            new Dictionary<ModuleId, IReadOnlyList<ModuleConfigurationField>>()).Take(33).ToArray();
        if (declarations.Length > 32) throw Invalid();
        foreach (var declaration in declarations)
        {
            if (!this.owners.Contains(declaration.Key.Value) || declaration.Value is null) throw Invalid();
            var fields = declaration.Value.Take(129).ToArray();
            if (fields.Length > 128 || fields.Any(field => field is null)) throw Invalid();
            fieldTokens.Add(declaration.Key.Value, fields.Select(field => field.Token).ToHashSet(StringComparer.Ordinal));
        }
    }

    internal sealed record Frame(int Version, string Snapshot, CompositionRecord? Record,
        IdentityDiagnosticRecord? Identity = null);

    public static async Task DiscardRawAsync(TextReader source, CancellationToken token)
    {
        var buffer = new char[1024];
        while (await source.ReadAsync(buffer.AsMemory(), token) != 0) { }
    }

    public Task WriteAsync(Stream destination, CompositionRecord record, CancellationToken token)
    {
        Validate(record);
        return WriteFrameAsync(destination, new(1, snapshot, record), token);
    }

    internal Task WriteIdentityAsync(Stream destination, IdentityDiagnosticRecord record, CancellationToken token)
    {
        ValidateIdentity(record);
        return WriteFrameAsync(destination, new(1, snapshot, null, record), token);
    }

    private async Task WriteFrameAsync(Stream destination, Frame frame, CancellationToken token)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(frame, CompositionDiagnostics.JsonOptions);
        if (bytes.Length > 4096) throw new CompositionBoundaryException("composition_output_limit");
        var header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, bytes.Length);
        await writes.WaitAsync(token);
        try
        {
            await destination.WriteAsync(header, token);
            await destination.WriteAsync(bytes, token);
            await destination.FlushAsync(token);
        }
        finally { writes.Release(); }
    }

    public async Task<CompositionRecord?> ReadAsync(Stream source, CancellationToken token)
    {
        var frame = await ReadObservationAsync(source, token);
        if (frame?.Identity is not null) throw Invalid();
        return frame?.Record;
    }

    internal async Task<Frame?> ReadObservationAsync(Stream source, CancellationToken token)
    {
        var header = new byte[4];
        if (await source.ReadAsync(header.AsMemory(0, 1), token) == 0) return null;
        try
        {
            await source.ReadExactlyAsync(header.AsMemory(1), token);
            var length = BinaryPrimitives.ReadInt32LittleEndian(header);
            if (length is < 1 or > 4096) throw Invalid();
            var bytes = new byte[length];
            await source.ReadExactlyAsync(bytes, token);
            using var json = JsonDocument.Parse(bytes, new() { AllowDuplicateProperties = false, MaxDepth = 8 });
            var frame = json.RootElement.Deserialize<Frame>(CompositionDiagnostics.JsonOptions);
            if (frame is null || frame.Version != 1 || frame.Snapshot != snapshot) throw Invalid();
            if ((frame.Record is null) == (frame.Identity is null)) throw Invalid();
            if (frame.Record is not null) Validate(frame.Record);
            if (frame.Identity is not null)
            {
                if (Encoding.UTF8.GetByteCount(json.RootElement.GetProperty("identity").GetRawText()) > 1024)
                    throw Invalid();
                ValidateIdentity(frame.Identity);
            }
            return frame;
        }
        catch (Exception error) when (error is JsonException or EndOfStreamException or
            ArgumentException or InvalidOperationException)
        {
            throw Invalid();
        }
    }

    private void ValidateIdentity(IdentityDiagnosticRecord record)
    {
        if (mode != CompositionMode.Serve) throw Invalid();
        try { _ = IdentityDiagnostics.Serialize(record); }
        catch (InvalidOperationException) { throw Invalid(); }
    }

    private void Validate(CompositionRecord record)
    {
        if (record.Version != 1 || record.Attempt != attempt ||
            record.Mode != CompositionDiagnostics.ModeName(mode) ||
            !Enum.GetValues<CompositionStage>().Any(value => CompositionDiagnostics.StageName(value) == record.Stage) ||
            !Enum.GetValues<CompositionOutcome>().Any(value => CompositionDiagnostics.OutcomeName(value) == record.Outcome) ||
            record.Owner is not null && !owners.Contains(record.Owner) &&
                !(selectedOwners.Contains(record.Owner) && record.Stage == "validated" && record.Outcome == "rejected" &&
                    record.Code is "composition_implementation_missing" or "composition_configuration_invalid") ||
            record.RelatedIds is null || record.RelatedIds.Count > 8 ||
            record.RelatedIds.Any(owner => !owners.Contains(owner) &&
                !(prerequisiteOwners.Contains(owner) && record.Stage == "validated" &&
                    record.Outcome == "rejected" && record.Code == "composition_dependency_missing")) ||
            !record.RelatedIds.SequenceEqual(record.RelatedIds.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)) ||
            record.ElapsedMilliseconds < 0 || record.Provider is not ("sqlite" or "postgresql" or "none" or "unknown") ||
            record.StorageCorrelation is not null && !CompositionDiagnostics.IsToken(record.StorageCorrelation) ||
            record.Durability is not null && record.Durability is not ("unchanged" or "committed" or "uncertain") ||
            (record.StorageCorrelation is null) != (record.Durability is null))
            throw Invalid();
        if ((record.Code == "composition_disabled" || record.Outcome == "disabled") &&
            (record.Code != "composition_disabled" || record.Outcome != "disabled" ||
                record.Stage != "discovered" || record.Owner is null || enabledOwners.Contains(record.Owner) ||
                record.RelatedIds.Count != 0 || record.StorageCorrelation is not null || record.FieldToken is not null))
            throw Invalid();
        try
        {
            CompositionDiagnostics.RequireCode(record.Code);
            if (record.Guidance != CompositionDiagnostics.Guidance(record.Code)) throw Invalid();
            CompositionDiagnostics.ValidateFieldEvidence(record.Stage, record.Outcome, record.Code, record.Owner,
                record.FieldToken, record.FieldReason);
            if (record.FieldToken is not null &&
                (!fieldTokens.TryGetValue(record.Owner!, out var known) || !known.Contains(record.FieldToken)))
                throw Invalid();
        }
        catch (ArgumentOutOfRangeException) { throw Invalid(); }
    }

    private static CompositionBoundaryException Invalid() => new("composition_observation_invalid");
}
