using System.Diagnostics;
using Kofte.Host.Bootstrap;
using Xunit;

namespace Kofte.Host.Tests;

public sealed class DiagnosticTests
{
    [Fact]
    public void CorrelationIsSafeWithOrWithoutAnActivity()
    {
        Assert.Matches("^[0-9a-f]{32}$", BootstrapDiagnostics.CorrelationId());
        using var activity = new Activity("bootstrap-test").SetIdFormat(ActivityIdFormat.W3C).Start();
        Assert.Equal(activity.TraceId.ToHexString(), BootstrapDiagnostics.CorrelationId());
    }

    [Fact]
    public void ExceptionsContainOnlyFixedCodes()
    {
        var error = new BootstrapException(BootstrapError.StorageUnavailable);
        Assert.Equal("storage_unavailable", error.Message);
        Assert.Null(error.InnerException);
    }
}
