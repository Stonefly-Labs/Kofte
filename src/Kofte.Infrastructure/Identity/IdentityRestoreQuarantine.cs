using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Identity;

public sealed class IdentityRestoreQuarantine
{
    public const string FileName = "identity-restore-required";
    private const int MaximumBytes = 3 * 1024 * 1024;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
        RespectNullableAnnotations = true,
        AllowDuplicateProperties = false
    };
    private sealed record State(string Version, byte[] Manifest, byte[]? RestoredManifest);
    private readonly string directory;
    private readonly State state;
    private readonly string fingerprint;

    private IdentityRestoreQuarantine(string directory, State state, string fingerprint) =>
        (this.directory, this.state, this.fingerprint) = (directory, state, fingerprint);

    public RecoveryManifest Manifest => RecoverySetVerifier.ParseManifest(state.Manifest);
    public string ManifestSha256 => Convert.ToHexStringLower(SHA256.HashData(state.Manifest));
    public bool Restored => state.RestoredManifest is not null;

    public static bool IsRequired(string directory) =>
        UnixStorePaths.ValidateFile(Path.Combine(directory, FileName), allowMissing: true);

    public static Task<IdentityRestoreQuarantine> CreateAsync(string recoveryDirectory, string keyDirectory,
        RecoveryManifest verifiedManifest, string? managedRoot = null, CancellationToken token = default) =>
        StorageDiagnostics.ObserveRecoveryAsync(verifiedManifest.Provider, token, async (budget, mutation, _) =>
        {
            if (OperatingSystem.IsWindows()) throw new StorageException(StorageCode.PathRejected);
            if (!Path.IsPathFullyQualified(keyDirectory) || Path.GetFullPath(keyDirectory) != keyDirectory ||
                Overlaps(keyDirectory, recoveryDirectory) || managedRoot is not null && Overlaps(keyDirectory, managedRoot))
                throw new StorageException(StorageCode.PathRejected);
            var (manifest, bytes) = await RecoverySetVerifier.ReadManifestAsync(recoveryDirectory, budget);
            if (!RecoverySetVerifier.SameManifest(manifest, verifiedManifest))
                throw new StorageException(StorageCode.Conflict);
            UnixStorePaths.ValidateRoot(keyDirectory, allowMissing: true);
            if (Directory.Exists(keyDirectory) || File.Exists(keyDirectory))
                throw new StorageException(StorageCode.ReconciliationRequired);
            UnixStorePaths.ValidateRoot(Path.GetDirectoryName(keyDirectory)!, allowMissing: false);
            budget.Check();
            mutation.PossibleCommit();
            Directory.CreateDirectory(keyDirectory, UnixStorePaths.DirectoryMode);
            WriteNew(Path.Combine(keyDirectory, FileName), JsonSerializer.SerializeToUtf8Bytes(
                new State("1", bytes, null), Options), budget);
            mutation.Committed();
            StorageDiagnostics.Checkpoint("identity_restore_quarantined", manifest.Provider);
            return await ReadCoreAsync(keyDirectory, budget);
        });

    public static Task<IdentityRestoreQuarantine> ReadAsync(string keyDirectory, CancellationToken token = default) =>
        StorageDiagnostics.ObserveRecoveryAsync(null, token, async (budget, _, setProvider) =>
        {
            var gate = await ReadCoreAsync(keyDirectory, budget);
            setProvider(gate.Manifest.Provider);
            return gate;
        });

    public Task<IdentityRestoreQuarantine> MarkRestoredAsync(RecoveryManifest restoredManifest,
        CancellationToken token = default) =>
        StorageDiagnostics.ObserveRecoveryAsync(restoredManifest.Provider, token, async (budget, mutation, _) =>
        {
            if (Restored || !RecoverySetVerifier.SameManifest(Manifest,
                    restoredManifest with { Artifacts = Manifest.Artifacts }))
                throw new StorageException(StorageCode.ReconciliationRequired);
            var current = await ReadCoreAsync(directory, budget);
            if (current.fingerprint != fingerprint) throw new StorageException(StorageCode.Conflict);
            var bytes = JsonSerializer.SerializeToUtf8Bytes(state with
            {
                RestoredManifest = JsonSerializer.SerializeToUtf8Bytes(restoredManifest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                })
            }, Options);
            var next = Path.Combine(directory, FileName + ".next");
            mutation.PossibleCommit();
            WriteNew(next, bytes, budget);
            budget.Check();
            File.Move(next, Path.Combine(directory, FileName), overwrite: true);
            mutation.Committed();
            StorageDiagnostics.Checkpoint("identity_restore_verified", restoredManifest.Provider);
            return await ReadCoreAsync(directory, budget);
        });

    private static async Task<IdentityRestoreQuarantine> ReadCoreAsync(string directory, OperationBudget budget)
    {
        UnixStorePaths.ValidateRoot(directory, allowMissing: false);
        var path = Path.Combine(directory, FileName);
        UnixStorePaths.ValidateFile(path);
        await using var file = File.OpenRead(path);
        if (file.Length is < 2 or > MaximumBytes) throw new StorageException(StorageCode.Corrupt);
        var bytes = new byte[checked((int)file.Length)];
        await file.ReadExactlyAsync(bytes, budget.Token);
        var extra = new byte[1];
        if (await file.ReadAsync(extra, budget.Token) != 0) throw new StorageException(StorageCode.Corrupt);
        try
        {
            var state = JsonSerializer.Deserialize<State>(bytes, Options)
                ?? throw new StorageException(StorageCode.Corrupt);
            if (state.Version != "1") throw new StorageException(StorageCode.VersionUnsupported);
            var manifest = RecoverySetVerifier.ParseManifest(state.Manifest);
            if (state.RestoredManifest is { } restored &&
                !RecoverySetVerifier.SameManifest(manifest,
                    RecoverySetVerifier.ParseManifest(restored) with { Artifacts = manifest.Artifacts }))
                throw new StorageException(StorageCode.ReconciliationRequired);
            return new(directory, state, Convert.ToHexStringLower(SHA256.HashData(bytes)));
        }
        catch (JsonException) { throw new StorageException(StorageCode.Corrupt); }
    }

    private static bool Overlaps(string left, string right)
    {
        var comparison = OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return left.Equals(right, comparison) ||
            left.StartsWith(right.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, comparison) ||
            right.StartsWith(left.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, comparison);
    }

    private static void WriteNew(string path, byte[] bytes, OperationBudget budget)
    {
        if (OperatingSystem.IsWindows()) throw new StorageException(StorageCode.PathRejected);
        if (bytes.Length is < 2 or > MaximumBytes) throw new StorageException(StorageCode.Corrupt);
        budget.Check();
        UnixStorePaths.ValidateFile(path, allowMissing: true);
        using var file = new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            UnixCreateMode = UnixStorePaths.FileMode
        });
        file.Write(bytes);
        file.Flush(flushToDisk: true);
        UnixStorePaths.ValidateFile(path);
        budget.Check();
    }
}
