using System.Security.Cryptography;
using System.Text;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Allocation;
using Kofte.Platform.Modules.Manifest;
using Xunit;

namespace Kofte.Host.Tests.Storage;

public sealed class StorageAllocatorTests
{
    [Theory]
    [InlineData("a.b.c", "a_b_c")]
    [InlineData("org.example.some--module", "org_example_some_module")]
    public void CanonicalIdentityAloneDeterminesStableReadableKey(string identity, string slug)
    {
        Assert.True(ModuleId.TryParse(identity, out var id));
        var key = StorageAllocator.Allocate(id);
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..32];
        Assert.Equal($"m_{slug}_{digest}", key);
        StorageAllocator.Validate(key);
        Assert.NotEqual(key, StorageAllocator.Allocate(Parse(identity + "x")));
    }

    [Fact]
    public void MaximumIdentityIsBoundedWithoutPostgresTruncation()
    {
        var id = Parse(string.Join('.', new string('a', 63), new string('b', 63),
            new string('c', 63), new string('d', 61)));
        var key = StorageAllocator.Allocate(id);
        Assert.Equal(59, Encoding.UTF8.GetByteCount(key));
        StorageAllocator.Validate("m_" + new string('a', 28) + "_" + new string('f', 32));
        Assert.Throws<StorageException>(() =>
            StorageAllocator.Validate("m_" + new string('a', 29) + "_" + new string('f', 32)));
    }

    [Theory]
    [InlineData("public")]
    [InlineData("information_schema")]
    [InlineData("pg_owner")]
    [InlineData("kofte_system")]
    [InlineData("m_alpha_0000000000000000000000000000000G")]
    [InlineData("m_ö_00000000000000000000000000000000")]
    [InlineData("m_x;DROP_00000000000000000000000000000000")]
    [InlineData("../platform/platform.db")]
    public void PhysicalKeysRejectReservedNonAsciiSqlAndPaths(string value) =>
        Assert.Throws<StorageException>(() => StorageAllocator.Validate(value));

    private static ModuleId Parse(string value)
    {
        Assert.True(ModuleId.TryParse(value, out var id));
        return id;
    }
}
