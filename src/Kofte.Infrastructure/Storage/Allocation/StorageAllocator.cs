using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Storage.Allocation;

internal static class StorageAllocator
{
    internal static string Allocate(ModuleId owner)
    {
        var slug = Regex.Replace(owner.Value.Replace('.', '_').Replace('-', '_'), "_+", "_").Trim('_');
        slug = slug[..Math.Min(24, slug.Length)].TrimEnd('_');
        if (slug.Length == 0) slug = "module";
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(owner.Value)))[..32];
        var key = $"m_{slug}_{digest}";
        Validate(key);
        return key;
    }

    internal static void Validate(string key)
    {
        if (key.Length > 63 || Encoding.UTF8.GetByteCount(key) > 63 ||
            !Regex.IsMatch(key, @"\Am_[a-z0-9_]+_[0-9a-f]{32}\z", RegexOptions.CultureInvariant))
            throw new StorageException(StorageCode.TargetReserved);
    }

    internal static string Quote(string key)
    {
        Validate(key);
        return "\"" + key + "\"";
    }
}
