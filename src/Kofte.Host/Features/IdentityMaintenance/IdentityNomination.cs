using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kofte.Host.Features.Identity;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;
using Microsoft.AspNetCore.WebUtilities;

namespace Kofte.Host.Features.IdentityMaintenance;

internal static class IdentityNomination
{
    internal static (string Issuer, string Subject) Read(string path, IdentityConfiguration configuration)
    {
        if (configuration.Provider is null)
            throw new IdentityStateException(IdentityDiagnosticOutcome.Forbidden);
        var bytes = IdentityConfiguration.ReadBounded(path, 32_768);
        try
        {
            using var document = JsonDocument.Parse(bytes,
                new() { AllowDuplicateProperties = false, MaxDepth = 2 });
            var value = document.RootElement;
            if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Count() != 3 ||
                value.EnumerateObject().Any(field => field.Name is not ("version" or "issuer" or "subject")) ||
                value.GetProperty("version").GetString() != "1")
                throw new IdentityConfigurationException("nominee_invalid");
            var issuer = value.GetProperty("issuer").GetString();
            var subject = value.GetProperty("subject").GetString();
            if (issuer is null || subject is null || issuer != configuration.Provider.Issuer ||
                !IdentityUserStore.ValidKey("oidc", issuer, subject))
                throw new IdentityConfigurationException("nominee_invalid");
            return (issuer, subject);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            throw new IdentityConfigurationException("nominee_invalid");
        }
    }

    internal static string NewAuthorization() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    internal static Task DeliverAsync(string path, string authorization, OperationBudget budget)
    {
        if (OperatingSystem.IsWindows()) throw new StorageException(StorageCode.PathRejected);
        budget.Check();
        UnixStorePaths.ValidateRoot(Path.GetDirectoryName(path)!, allowMissing: false);
        UnixStorePaths.ValidateFile(path, allowMissing: true);
        using var file = new FileStream(path, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            UnixCreateMode = UnixStorePaths.FileMode
        });
        IdentityDiagnostics.Current?.PossibleCommit();
        IdentityDiagnostics.Checkpoint("bootstrap_file_created");
        file.Write(Encoding.ASCII.GetBytes(authorization));
        file.Flush(flushToDisk: true);
        UnixStorePaths.ValidateFile(path);
        IdentityDiagnostics.Checkpoint("bootstrap_file_flushed");
        budget.Check();
        return Task.CompletedTask;
    }
}
