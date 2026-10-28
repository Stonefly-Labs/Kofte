using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Identity;

internal sealed record IdentityRestorePlan(IdentityRestoreDecision Decision, string Mode, string TrustDigest,
    Guid Epoch, Guid RingId, SortedDictionary<string, string>? Keys = null, IdentityRecoverySummary? Result = null,
    IdentityRecoverySummary? PreparedIdentity = null);

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
    private sealed record State(string Version, byte[] Manifest, byte[]? RestoredManifest,
        IdentityRestorePlan? Plan = null);
    private readonly string directory;
    private readonly State state;
    private readonly string fingerprint;

    private IdentityRestoreQuarantine(string directory, State state, string fingerprint) =>
        (this.directory, this.state, this.fingerprint) =
            (Path.TrimEndingDirectorySeparator(directory), state, fingerprint);

    public RecoveryManifest Manifest => RecoverySetVerifier.ParseManifest(state.Manifest);
    public string ManifestSha256 => Convert.ToHexStringLower(SHA256.HashData(state.Manifest));
    public bool Restored => state.RestoredManifest is not null;
    internal IdentityRestorePlan? Plan => state.Plan;
    internal IdentityRecoverySummary? ExpectedIdentity => Manifest.Identity ?? Plan?.PreparedIdentity;
    internal string StagingDirectory => directory + ".identity-restore-keys";
    private string ReceiptPath => directory + ".identity-restore-receipt";

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

    internal static Task<IdentityRestoreQuarantine> ReadReceiptAsync(string keyDirectory, OperationBudget budget) =>
        ReadCoreAsync(keyDirectory, budget, receipt: true);

    internal async Task<IdentityRestoreQuarantine> ResumeAsync(IdentityRestoreDecision decision, string mode,
        string digest, OperationBudget budget)
    {
        MatchPlan(Plan, decision, mode, digest);
        var next = Path.Combine(directory, FileName + ".next");
        if (!UnixStorePaths.ValidateFile(next, allowMissing: true)) return this;
        var pending = await ReadStateAsync(directory, next, budget);
        MatchPlan(pending.Plan, decision, mode, digest);
        if (!state.Manifest.AsSpan().SequenceEqual(pending.state.Manifest) ||
            state.RestoredManifest is { } restored &&
                (pending.state.RestoredManifest is not { } target || !restored.AsSpan().SequenceEqual(target)) ||
            state.Plan is { } plan &&
                (pending.Plan is not { } after ||
                 plan with { Keys = null, Result = null, PreparedIdentity = null } !=
                    after with { Keys = null, Result = null, PreparedIdentity = null } ||
                 plan.Keys is not null && !SameKeys(plan.Keys, after.Keys) ||
                 plan.PreparedIdentity is not null && plan.PreparedIdentity != after.PreparedIdentity ||
                 plan.Result is not null && plan.Result != after.Result))
            throw new StorageException(StorageCode.Conflict);
        await VerifyCurrentAsync(budget);
        budget.Check();
        IdentityDiagnostics.Current?.PossibleCommit();
        File.Move(next, Path.Combine(directory, FileName), overwrite: true);
        IdentityDiagnostics.Current?.Committed();
        return await ReadCoreAsync(directory, budget);
    }

    internal async Task<IdentityRestoreQuarantine> BeginAsync(IdentityRestoreDecision decision,
        string mode, string digest, OperationBudget budget)
    {
        decision.Validate(this);
        if (Plan is { } plan)
        {
            MatchPlan(plan, decision, mode, digest);
            return this;
        }
        UnixStorePaths.ValidateRoot(StagingDirectory, allowMissing: true);
        if (Directory.Exists(StagingDirectory) || File.Exists(StagingDirectory) ||
            UnixStorePaths.ValidateFile(ReceiptPath, allowMissing: true))
            throw new StorageException(StorageCode.ReconciliationRequired);
        return await SaveAsync(state with
        {
            Plan = new(decision, mode, digest, Guid.NewGuid(), Guid.NewGuid())
        }, budget);
    }

    internal Task<IdentityRestoreQuarantine> RecordPreparedIdentityAsync(IdentityRecoverySummary prepared,
        OperationBudget budget)
    {
        var plan = Plan ?? throw new StorageException(StorageCode.ReconciliationRequired);
        if (Manifest.Identity is not null || plan.PreparedIdentity is not null && plan.PreparedIdentity != prepared)
            throw new StorageException(StorageCode.ReconciliationRequired);
        return plan.PreparedIdentity is not null ? Task.FromResult(this) :
            SaveAsync(state with { Plan = plan with { PreparedIdentity = prepared } }, budget);
    }

    internal Task<IdentityRestoreQuarantine> RecordKeysAsync(SortedDictionary<string, string> keys,
        OperationBudget budget)
    {
        var plan = Plan ?? throw new StorageException(StorageCode.ReconciliationRequired);
        if (plan.Keys is not null && !SameKeys(plan.Keys, keys))
            throw new StorageException(StorageCode.ReconciliationRequired);
        return plan.Keys is not null ? Task.FromResult(this) : SaveAsync(state with { Plan = plan with { Keys = keys } }, budget);
    }

    internal Task<IdentityRestoreQuarantine> RecordResultAsync(IdentityRecoverySummary result, OperationBudget budget)
    {
        var plan = Plan ?? throw new StorageException(StorageCode.ReconciliationRequired);
        if (plan.Result is not null && plan.Result != result)
            throw new StorageException(StorageCode.ReconciliationRequired);
        return plan.Result is not null ? Task.FromResult(this) :
            SaveAsync(state with { Plan = plan with { Result = result } }, budget);
    }

    internal async Task CompleteAsync(OperationBudget budget)
    {
        if (Plan is not { Keys: not null, Result: not null })
            throw new StorageException(StorageCode.ReconciliationRequired);
        await VerifyCurrentAsync(budget);
        UnixStorePaths.ValidateRoot(Path.GetDirectoryName(directory)!, allowMissing: false);
        UnixStorePaths.ValidateFile(ReceiptPath, allowMissing: true);
        budget.Check();
        IdentityDiagnostics.Current?.PossibleCommit();
        File.Move(Path.Combine(directory, FileName), ReceiptPath);
        IdentityDiagnostics.Current?.Committed();
    }

    private async Task<IdentityRestoreQuarantine> SaveAsync(State nextState, OperationBudget budget)
    {
        ValidatePlan(directory, nextState);
        await VerifyCurrentAsync(budget);
        var next = Path.Combine(directory, FileName + ".next");
        IdentityDiagnostics.Current?.PossibleCommit();
        WriteNew(next, JsonSerializer.SerializeToUtf8Bytes(nextState, Options), budget);
        IdentityDiagnostics.Checkpoint("restore_state_written");
        budget.Check();
        File.Move(next, Path.Combine(directory, FileName), overwrite: true);
        IdentityDiagnostics.Current?.Committed();
        return await ReadCoreAsync(directory, budget);
    }

    private async Task VerifyCurrentAsync(OperationBudget budget)
    {
        if ((await ReadCoreAsync(directory, budget)).fingerprint != fingerprint)
            throw new StorageException(StorageCode.Conflict);
    }

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

    private static Task<IdentityRestoreQuarantine> ReadCoreAsync(string directory, OperationBudget budget,
        bool receipt = false)
    {
        directory = Path.TrimEndingDirectorySeparator(directory);
        UnixStorePaths.ValidateRoot(directory, allowMissing: false);
        if (receipt) UnixStorePaths.ValidateRoot(Path.GetDirectoryName(directory)!, allowMissing: false);
        return ReadStateAsync(directory, receipt ? directory + ".identity-restore-receipt" :
            Path.Combine(directory, FileName), budget);
    }

    private static async Task<IdentityRestoreQuarantine> ReadStateAsync(string directory, string path, OperationBudget budget)
    {
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
            ValidatePlan(directory, state);
            return new(directory, state, Convert.ToHexStringLower(SHA256.HashData(bytes)));
        }
        catch (JsonException) { throw new StorageException(StorageCode.Corrupt); }
    }

    private static void ValidatePlan(string directory, State state)
    {
        if (state.Plan is not { } plan) return;
        var gate = new IdentityRestoreQuarantine(directory, state, "");
        plan.Decision.Validate(gate);
        if (plan.Mode is not ("oidc" or "development") ||
            !IdentityStore.IsDigest(plan.TrustDigest) || plan.Epoch == Guid.Empty ||
            plan.Epoch == gate.ExpectedIdentity?.SessionEpoch ||
            plan.RingId == Guid.Empty ||
            plan.Keys is { } keys && (keys.Count is < 2 or > 257 ||
                !keys.ContainsKey("identity-owner.json") ||
                keys.Any(pair => pair.Key != Path.GetFileName(pair.Key) ||
                    pair.Key != "identity-owner.json" && !(pair.Key.Length == 44 &&
                        pair.Key.StartsWith("key-", StringComparison.Ordinal) &&
                        pair.Key.EndsWith(".xml", StringComparison.Ordinal) &&
                        Guid.TryParseExact(pair.Key[4..^4], "D", out var id) && id != Guid.Empty) ||
                    pair.Value is null || !IdentityStore.IsDigest(pair.Value))))
            throw new StorageException(StorageCode.Corrupt);
        if (plan.PreparedIdentity is { } prepared)
        {
            IdentityRecoverySnapshot.Validate(prepared);
            if (gate.Manifest.Identity is not null || prepared.SecurityRevision != 1 ||
                prepared.BootstrapState != "disabled" || prepared.ConsumedGeneration is not null ||
                prepared.Counts != new IdentityRecoveryCounts(0, 0, 0, 0, 1, 0))
                throw new StorageException(StorageCode.Corrupt);
        }
        if (plan.Result is { } result)
        {
            IdentityRecoverySnapshot.Validate(result);
            if (plan.Keys is null || gate.ExpectedIdentity is not { } expected ||
                result.SecurityRevision != checked(expected.SecurityRevision + 1) ||
                result.SessionEpoch != plan.Epoch || result.ConsumedGeneration != expected.ConsumedGeneration ||
                result.BootstrapState != IdentityRestoreStore.BootstrapState(expected, plan.Decision) ||
                result.Counts != expected.Counts with { AdmissionWindows = 0, Events = checked(expected.Counts.Events + 1) })
                throw new StorageException(StorageCode.Corrupt);
        }
    }

    private static void MatchPlan(IdentityRestorePlan? plan, IdentityRestoreDecision decision, string mode, string digest)
    {
        if (plan is not null && (plan.Decision != decision || plan.Mode != mode || plan.TrustDigest != digest))
            throw new StorageException(StorageCode.ReconciliationRequired);
    }

    private static bool SameKeys(SortedDictionary<string, string> left, SortedDictionary<string, string>? right) =>
        right is not null && left.Count == right.Count &&
            left.All(pair => right.TryGetValue(pair.Key, out var value) && value == pair.Value);

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
