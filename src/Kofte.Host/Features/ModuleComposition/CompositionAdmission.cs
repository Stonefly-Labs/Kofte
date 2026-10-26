using System.Buffers.Binary;
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kofte.Platform.Modules.Composition;

namespace Kofte.Host.Features.ModuleComposition;

internal sealed class CompositionAdmission : IDisposable
{
    internal const string InputKey = "KOFTE_COMPOSITION_INPUT_PIPE";
    internal const string EventKey = "KOFTE_COMPOSITION_EVENT_PIPE";
    internal const string TokenKey = "KOFTE_COMPOSITION_ADMISSION";
    internal string Attempt { get; }
    internal string Snapshot { get; }
    internal ModuleSelectionDocument Selection { get; }
    internal AnonymousPipeClientStream Events { get; }

    private CompositionAdmission(string attempt, string snapshot, ModuleSelectionDocument selection,
        AnonymousPipeClientStream events) => (Attempt, Snapshot, Selection, Events) = (attempt, snapshot, selection, events);

    internal static byte[] CatalogIdentity(IReadOnlyList<ModuleDeployment> catalog)
    {
        if (catalog.Count > 32) throw new CompositionBoundaryException("composition_input_limit");
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var deployment in catalog)
        {
            hash.AppendData(deployment.Manifest.Span);
            var shape = JsonSerializer.SerializeToUtf8Bytes(new
            {
                deployment.ContractVersion,
                claims = deployment.Claims.Select(c => new
                {
                    c.Owner,
                    c.Kind,
                    c.LocalName,
                    c.ContractVersion,
                    c.Mode,
                    c.Method,
                    c.Route,
                    type = c.ServiceType?.AssemblyQualifiedName
                }),
                fields = deployment.ConfigurationFields.Select(f => new { f.Token, f.Path, f.Type, f.Required }),
                services = deployment.OwnedServiceTypes.Select(t => t.AssemblyQualifiedName)
            });
            if (shape.Length > 1_048_576) throw new CompositionBoundaryException("composition_input_limit");
            hash.AppendData(shape);
        }
        return hash.GetHashAndReset();
    }

    internal static async Task WriteAsync(Stream destination, string admission, string attempt, string snapshot,
        CompositionMode mode, ModuleSelectionDocument selection, IReadOnlyList<ModuleDeployment> catalog, CancellationToken token)
    {
        var bytes = selection.CopySnapshot();
        var header = new byte[168];
        BinaryPrimitives.WriteInt32LittleEndian(header, 1);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4), (int)mode);
        Encoding.ASCII.GetBytes(attempt, header.AsSpan(8, 32));
        Encoding.ASCII.GetBytes(snapshot, header.AsSpan(40, 32));
        Encoding.ASCII.GetBytes(admission, header.AsSpan(72, 64));
        CatalogIdentity(catalog).CopyTo(header, 136);
        await destination.WriteAsync(header, token);
        var length = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(length, bytes.Length);
        await destination.WriteAsync(length, token);
        await destination.WriteAsync(bytes, token);
        await destination.FlushAsync(token);
    }

    internal static async Task<CompositionAdmission> ReadAsync(CompositionMode mode,
        IReadOnlyList<ModuleDeployment> catalog, CancellationToken token)
    {
        var input = Environment.GetEnvironmentVariable(InputKey);
        var output = Environment.GetEnvironmentVariable(EventKey);
        var admission = Environment.GetEnvironmentVariable(TokenKey);
        foreach (var key in new[] { InputKey, EventKey, TokenKey })
            Environment.SetEnvironmentVariable(key, null);
        if (!long.TryParse(input, out var inputHandle) || inputHandle <= 0 ||
            !long.TryParse(output, out var outputHandle) || outputHandle <= 0 ||
            admission is not { Length: 64 } || !admission.All(char.IsAsciiHexDigitLower))
            throw Invalid();
        using var source = new AnonymousPipeClientStream(PipeDirection.In, input!);
        var events = new AnonymousPipeClientStream(PipeDirection.Out, output!);
        try
        {
            var header = new byte[168];
            await source.ReadExactlyAsync(header, token);
            var attempt = Encoding.ASCII.GetString(header, 8, 32);
            var snapshot = Encoding.ASCII.GetString(header, 40, 32);
            if (BinaryPrimitives.ReadInt32LittleEndian(header) != 1 ||
                BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4)) != (int)mode ||
                !CompositionDiagnostics.IsToken(attempt) || !CompositionDiagnostics.IsToken(snapshot) ||
                !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(admission), header.AsSpan(72, 64)) ||
                !CryptographicOperations.FixedTimeEquals(CatalogIdentity(catalog), header.AsSpan(136, 32)))
                throw Invalid();
            var length = new byte[4];
            await source.ReadExactlyAsync(length, token);
            var size = BinaryPrimitives.ReadInt32LittleEndian(length);
            if (size is < 1 or > 1_048_576) throw Invalid();
            var bytes = new byte[size];
            await source.ReadExactlyAsync(bytes, token);
            if (await source.ReadAsync(new byte[1], token) != 0) throw Invalid();
            return new(attempt, snapshot, ModuleSelectionReader.Parse(bytes), events);
        }
        catch
        {
            events.Dispose();
            throw;
        }
    }

    public void Dispose() => Events.Dispose();
    private static CompositionBoundaryException Invalid() => new("composition_admission_invalid");
}
