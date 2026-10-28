using System.Text;
using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Identity;

public sealed record IdentityRestoreDecision(Guid RecoverySetId, Guid InstallationId,
    string ExpectedManifestSha256, string PriorEnrollment)
{
    public static Task<IdentityRestoreDecision> ReadAsync(string path, IdentityRestoreQuarantine quarantine,
        CancellationToken token = default) =>
        StorageDiagnostics.ObserveRecoveryAsync(quarantine.Manifest.Provider, token, async (budget, _, _) =>
        {
            if (!quarantine.Restored) throw new StorageException(StorageCode.ReconciliationRequired);
            if (!Path.IsPathFullyQualified(path) || path != Path.GetFullPath(path))
                throw new StorageException(StorageCode.PathRejected);
            UnixStorePaths.ValidateRoot(Path.GetDirectoryName(path)!, allowMissing: false);
            UnixStorePaths.ValidateFile(path);
            await using var file = File.OpenRead(path);
            if (file.Length is < 2 or > 32_768) throw new StorageException(StorageCode.RequestInvalid);
            var bytes = new byte[checked((int)file.Length)];
            await file.ReadExactlyAsync(bytes, budget.Token);
            var extra = new byte[1];
            if (await file.ReadAsync(extra, budget.Token) != 0)
                throw new StorageException(StorageCode.RequestInvalid);
            try
            {
                _ = new UTF8Encoding(false, true).GetCharCount(bytes);
                using var document = JsonDocument.Parse(bytes,
                    new() { AllowDuplicateProperties = false, MaxDepth = 2 });
                var value = document.RootElement;
                if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Count() != 5 ||
                    value.EnumerateObject().Any(field =>
                        field.Name is not ("version" or "recoverySetId" or "installationId" or
                            "expectedManifestSha256" or "priorEnrollment") ||
                        field.Value.ValueKind != JsonValueKind.String) ||
                    value.GetProperty("version").GetString() != "1")
                    throw new StorageException(StorageCode.RequestInvalid);
                var manifest = quarantine.Manifest;
                if (value.GetProperty("recoverySetId").GetString() != manifest.RecoverySetId.ToString("D") ||
                    value.GetProperty("installationId").GetString() != manifest.InstallationId.ToString("D") ||
                    value.GetProperty("expectedManifestSha256").GetString() != quarantine.ManifestSha256)
                    throw new StorageException(StorageCode.ReconciliationRequired);
                var history = value.GetProperty("priorEnrollment").GetString();
                if (history is not ("known-consumed" or "known-never-enrolled" or "not-established"))
                    throw new StorageException(StorageCode.RequestInvalid);
                return new IdentityRestoreDecision(manifest.RecoverySetId, manifest.InstallationId,
                    quarantine.ManifestSha256, history);
            }
            catch (Exception error) when (error is JsonException or DecoderFallbackException)
            {
                throw new StorageException(StorageCode.RequestInvalid);
            }
        });
}
