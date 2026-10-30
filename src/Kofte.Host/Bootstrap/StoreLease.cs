using Kofte.Infrastructure.Storage;
using Lease = Kofte.Infrastructure.Storage.StoreLease;

namespace Kofte.Host.Bootstrap;

public sealed class StoreLease : IDisposable
{
    private readonly Lease lease;
    internal Lease Authority => lease;
    private StoreLease(Lease lease) => this.lease = lease;

    public static StoreLease Acquire(string root)
    {
        try { return new(Lease.Acquire(root)); }
        catch (StorePathException error) { throw UnixStorePaths.Translate(error); }
    }

    public void Dispose() => lease.Dispose();
}
