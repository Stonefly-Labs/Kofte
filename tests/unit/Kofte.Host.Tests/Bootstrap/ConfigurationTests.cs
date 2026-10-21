using Kofte.Host.Bootstrap;
using Xunit;

namespace Kofte.Host.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public void DefaultsAreLocalAndDoNotInitializeStorage()
    {
        var config = BootstrapConfiguration.Parse(new Dictionary<string, string?>(), "/tmp/kofte-config");
        Assert.Equal("local-sqlite", config.Profile);
        Assert.Equal("http://127.0.0.1:5080/", config.HostUrl.AbsoluteUri);
        Assert.Equal("/tmp/kofte-config/data", config.DataRoot);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("0.0.0.0")]
    public void AdapterDefaultFollowsTheSelectedHostPort(string hostname)
    {
        var config = BootstrapConfiguration.Parse(new Dictionary<string, string?>
        {
            ["KOFTE_HOST_URL"] = $"http://{hostname}:5081",
            ["KOFTE_ALLOW_UNSAFE_EXPOSURE"] = "true"
        }, "/tmp/kofte-config");
        Assert.Equal("http://127.0.0.1:5081/", config.BackendOrigin.AbsoluteUri);
    }

    [Theory]
    [InlineData("KOFTE_UNKNOWN", "value")]
    [InlineData("KOFTE_PROFILE", "production")]
    [InlineData("KOFTE_ALLOW_UNSAFE_EXPOSURE", "yes")]
    [InlineData("KOFTE_HOST_URL", "http://secret:password@127.0.0.1:5080")]
    [InlineData("KOFTE_HOST_URL", "http://127.0.0.1:5080/../")]
    [InlineData("KOFTE_HOST_URL", "http://0.0.0.0:5080")]
    [InlineData("KOFTE_HOST_URL", "http://@127.0.0.1:5080")]
    [InlineData("KOFTE_HOST_URL", "http://127.0.0.1:5080\n")]
    [InlineData("KOFTE_BACKEND_ORIGIN", "http://example.com:5080")]
    [InlineData("KOFTE_BACKEND_ORIGIN", "http://127.0.0.1:5081")]
    [InlineData("KOFTE_BACKEND_ORIGIN", "http://127.0.0.2:5080")]
    [InlineData("KOFTE_DATA_ROOT", "../data")]
    [InlineData("KOFTE_PROFILE", "test-postgres")]
    [InlineData("KOFTE_POSTGRES_CONNECTION", "Password=credential-canary")]
    public void InvalidConfigurationIsSanitized(string key, string value)
    {
        var error = Assert.Throws<BootstrapException>(() =>
            BootstrapConfiguration.Parse(new Dictionary<string, string?> { [key] = value }, "/tmp/kofte-config"));
        Assert.Equal(BootstrapError.ConfigurationInvalid, error.Code);
        Assert.DoesNotContain(value, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnsafeBindNeverPermitsAnExternalAdapter()
    {
        var env = new Dictionary<string, string?>
        {
            ["KOFTE_ALLOW_UNSAFE_EXPOSURE"] = "true",
            ["KOFTE_HOST_URL"] = "http://0.0.0.0:5080",
            ["KOFTE_BACKEND_ORIGIN"] = "http://127.0.0.1:5080"
        };
        Assert.True(BootstrapConfiguration.Parse(env, "/tmp/kofte-config").UnsafeExposure);
        env["KOFTE_BACKEND_ORIGIN"] = "http://0.0.0.0:5080";
        Assert.Throws<BootstrapException>(() => BootstrapConfiguration.Parse(env, "/tmp/kofte-config"));
    }
}
