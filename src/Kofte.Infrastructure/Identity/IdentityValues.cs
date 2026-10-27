using System.Globalization;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Identity;

internal static class IdentityValues
{
    internal static Guid Id(object? value) =>
        Guid.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture), out var id) && id != Guid.Empty
            ? id : throw new StorageException(StorageCode.Corrupt);

    internal static DateTime Utc(object? value) => value switch
    {
        DateTime { Kind: DateTimeKind.Utc } utc => utc,
        string text when DateTime.TryParseExact(text, "O", CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out var parsed) && parsed.Kind == DateTimeKind.Utc => parsed,
        _ => throw new StorageException(StorageCode.Corrupt)
    };
}
