using System.Security.Cryptography;
using Kofte.Host.Features.Identity;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Platform.Identity;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Host.Features.IdentityMaintenance;

internal static class IdentityRestore
{
    private const string CopyName = ".identity-restore-copy.next";

    internal static async Task RunAsync(IdentityConfiguration configuration, StorageInstallation installation,
        MaintenanceScope scope, string decisionPath, OperationBudget budget, string managedRoot)
    {
        await scope.VerifyAsync(installation, budget);
        var gated = IdentityRestoreQuarantine.IsRequired(configuration.KeyRingDirectory);
        var quarantine = gated
            ? await IdentityRestoreQuarantine.ReadAsync(configuration.KeyRingDirectory, budget.Token)
            : await IdentityRestoreQuarantine.ReadReceiptAsync(configuration.KeyRingDirectory, budget);
        var decision = await IdentityRestoreDecision.ReadAsync(decisionPath, quarantine, budget.Token);
        var mode = configuration.Mode == IdentityMode.Oidc ? "oidc" : "development";
        await scope.VerifyAsync(installation, budget);
        if (gated) quarantine = await quarantine.ResumeAsync(decision, mode, configuration.TrustDigest,
            installation, scope, budget);
        if (Overlaps(quarantine.StagingDirectory, managedRoot))
            throw new StorageException(StorageCode.PathRejected);
        if (!gated && quarantine.Plan is not { Keys: not null, Result: not null })
            throw new StorageException(StorageCode.ReconciliationRequired);
        if (quarantine.Plan is null)
        {
            if (quarantine.Manifest.Identity is { } identity
                ? await IdentityRestoreStore.InspectAsync(installation, scope, quarantine.Manifest, budget) != identity
                : !RecoverySetVerifier.SameInventory(quarantine.Manifest,
                    await RecoverySetVerifier.InspectStoppedAsync(installation, scope, budget.Token)))
                throw new StorageException(StorageCode.ReconciliationRequired);
        }
        await scope.VerifyAsync(installation, budget);
        quarantine = await quarantine.BeginAsync(decision, mode, configuration.TrustDigest, budget);
        var plan = quarantine.Plan ?? throw new StorageException(StorageCode.ReconciliationRequired);
        if (gated)
        {
            using (var staged = await IdentityKeys.PrepareAsync(configuration, decision.InstallationId,
                installation, scope, budget, directoryOverride: quarantine.StagingDirectory, restoreRingId: plan.RingId))
            {
                await scope.VerifyAsync(installation, budget);
                quarantine = await quarantine.RecordKeysAsync(staged.RecoveryInventory(budget), budget);
            }
            plan = quarantine.Plan!;
            if (quarantine.Manifest.Identity is null && plan.PreparedIdentity is null)
            {
                var prepared = await IdentityRestoreStore.PrepareLegacyAsync(installation, scope, quarantine,
                    mode, configuration.TrustDigest, budget);
                await scope.VerifyAsync(installation, budget);
                quarantine = await quarantine.RecordPreparedIdentityAsync(prepared, budget);
                plan = quarantine.Plan!;
            }
            await CopyKeysAsync(quarantine.StagingDirectory, configuration.KeyRingDirectory, plan.Keys!,
                installation, scope, budget);
            using var keys = IdentityKeys.OpenRestored(configuration, decision.InstallationId, budget);
            MatchKeys(keys.RecoveryInventory(budget), plan.Keys!);
            IdentityDiagnostics.Checkpoint("restore_before_commit");
        }
        else
        {
            using var keys = IdentityKeys.Open(configuration, decision.InstallationId, budget);
            MatchKeys(keys.RecoveryInventory(budget), plan.Keys!);
        }
        var result = !gated || plan.Result is not null
            ? await IdentityRestoreStore.VerifyReconciledAsync(installation, scope, quarantine, decision,
                mode, configuration.TrustDigest, plan.Epoch, budget)
            : await IdentityRestoreStore.ReconcileAsync(installation, scope, quarantine, decision,
                mode, configuration.TrustDigest, plan.Epoch, budget);
        if (!gated)
        {
            if (result != plan.Result) throw new StorageException(StorageCode.ReconciliationRequired);
            return;
        }
        await scope.VerifyAsync(installation, budget);
        quarantine = await quarantine.RecordResultAsync(result, budget);
        IdentityDiagnostics.Checkpoint("restore_committed");
        await scope.VerifyAsync(installation, budget);
        using (var keys = IdentityKeys.OpenRestored(configuration, decision.InstallationId, budget))
            MatchKeys(keys.RecoveryInventory(budget), quarantine.Plan!.Keys!);
        await scope.VerifyAsync(installation, budget);
        await quarantine.CompleteAsync(budget);
        using var ready = IdentityKeys.Open(configuration, decision.InstallationId, budget);
        ready.Verify(budget);
    }

    private static async Task CopyKeysAsync(string source, string destination, SortedDictionary<string, string> files,
        StorageInstallation installation, MaintenanceScope scope, OperationBudget budget)
    {
        UnixStorePaths.ValidateRoot(source, allowMissing: false);
        UnixStorePaths.ValidateRoot(destination, allowMissing: false);
        var entries = Directory.EnumerateFileSystemEntries(destination).Take(260).ToArray();
        if (entries.Length > files.Count + 2 || entries.Any(path =>
                Path.GetFileName(path) is not (IdentityRestoreQuarantine.FileName or CopyName) &&
                !files.ContainsKey(Path.GetFileName(path))))
            throw new StorageException(StorageCode.ReconciliationRequired);
        foreach (var path in entries) UnixStorePaths.ValidateFile(path);
        var temporary = Path.Combine(destination, CopyName);
        foreach (var file in files.OrderBy(pair => pair.Key == "identity-owner.json" ? 1 : 0)
            .ThenBy(pair => pair.Key, StringComparer.Ordinal))
        {
            await scope.VerifyAsync(installation, budget);
            var bytes = ReadFile(Path.Combine(source, file.Key), budget);
            if (Hash(bytes) != file.Value) throw new StorageException(StorageCode.ReconciliationRequired);
            var target = Path.Combine(destination, file.Key);
            if (UnixStorePaths.ValidateFile(target, allowMissing: true))
            {
                if (Hash(ReadFile(target, budget)) != file.Value)
                    throw new StorageException(StorageCode.ReconciliationRequired);
                continue;
            }
            if (UnixStorePaths.ValidateFile(temporary, allowMissing: true)) File.Delete(temporary);
            budget.Check();
            IdentityDiagnostics.Current?.PossibleCommit();
            WriteNew(temporary, bytes, budget);
            File.Move(temporary, target);
            IdentityDiagnostics.Current?.Committed();
            IdentityDiagnostics.Checkpoint("restore_key_copied");
        }
        if (UnixStorePaths.ValidateFile(temporary, allowMissing: true)) File.Delete(temporary);
        await scope.VerifyAsync(installation, budget);
    }

    private static byte[] ReadFile(string path, OperationBudget budget)
    {
        UnixStorePaths.ValidateFile(path);
        using var stream = File.OpenRead(path);
        if (stream.Length is < 1 or > 65_536) throw new StorageException(StorageCode.Corrupt);
        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        if (stream.ReadByte() != -1) throw new StorageException(StorageCode.Corrupt);
        budget.Check();
        return bytes;
    }

    private static void WriteNew(string path, byte[] bytes, OperationBudget budget)
    {
        if (OperatingSystem.IsWindows()) throw new StorageException(StorageCode.PathRejected);
        budget.Check();
        using var stream = new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            UnixCreateMode = UnixStorePaths.FileMode
        });
        stream.Write(bytes);
        stream.Flush(flushToDisk: true);
        budget.Check();
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private static void MatchKeys(SortedDictionary<string, string> actual, SortedDictionary<string, string> expected)
    {
        if (actual.Count != expected.Count || actual.Any(pair =>
            !expected.TryGetValue(pair.Key, out var value) || value != pair.Value))
            throw new StorageException(StorageCode.ReconciliationRequired);
    }

    private static bool Overlaps(string left, string right)
    {
        var comparison = OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return left.Equals(right, comparison) ||
            left.StartsWith(right.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, comparison) ||
            right.StartsWith(left.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, comparison);
    }
}
