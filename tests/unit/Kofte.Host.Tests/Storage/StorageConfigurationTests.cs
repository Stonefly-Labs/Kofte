using Kofte.Host.Bootstrap;
using Kofte.Host.Features.StorageMaintenance;
using Xunit;

namespace Kofte.Host.Tests.Storage;

public sealed class StorageConfigurationTests
{
    private const string Run = "0123456789abcdef0123456789abcdef";
    private static Dictionary<string, string?> Postgres() => new()
    {
        ["KOFTE_PROFILE"] = "test-postgres",
        ["KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE"] = "true",
        ["KOFTE_POSTGRES_CONNECTION"] =
            $"Host=127.0.0.1;Database=kofte_test_{Run};Username=kofte_app_{Run};Password=runtime-canary;Application Name=kofte_bootstrap_{Run}"
    };

    [Fact]
    public void ServingRejectsElevatedSettingRatherThanRegisteringItInConfiguration()
    {
        var env = Postgres();
        env["KOFTE_POSTGRES_PROVISIONING_CONNECTION"] = "provisioning-secret-canary";
        var failure = Assert.Throws<BootstrapException>(() =>
            BootstrapConfiguration.Parse(env, Directory.GetCurrentDirectory()));
        Assert.Equal(BootstrapError.ConfigurationInvalid, failure.Code);
        Assert.DoesNotContain("secret", failure.ToString());
    }

    [Fact]
    public void MaintenanceDoesNotRequireProvisioningForReadOrUpgrade()
    {
        var config = StorageMaintenanceConfiguration.Parse(Postgres(), Directory.GetCurrentDirectory());
        Assert.Equal("postgresql", config.CreateInstallation().Provider);
        Assert.DoesNotContain("runtime-canary", config.ToString());
    }

    [Theory]
    [InlineData("other_database", "kofte_provisioner", Run)]
    [InlineData("kofte_test_" + Run, "kofte_app_" + Run, Run)]
    [InlineData("kofte_test_" + Run, "kofte_provisioner", "ffffffffffffffffffffffffffffffff")]
    public void MaintenanceRejectsDifferentTargetRoleOrOwnershipIdentity(string database, string role, string run)
    {
        var env = Postgres();
        env["KOFTE_POSTGRES_PROVISIONING_CONNECTION"] =
            $"Host=127.0.0.1;Database={database};Username={role};Password=provisioner-canary;Application Name=kofte_provisioning_{run}";
        Assert.Equal(BootstrapError.ConfigurationInvalid,
            Assert.Throws<BootstrapException>(() =>
                StorageMaintenanceConfiguration.Parse(env, Directory.GetCurrentDirectory())).Code);
    }

    [Fact]
    public void MaintenanceAcceptsDistinctOwnedRoleIdentityButNeverSqliteElevationOrUnknownSettings()
    {
        var env = Postgres();
        env["KOFTE_POSTGRES_PROVISIONING_CONNECTION"] =
            $"Host=127.0.0.1;Database=kofte_test_{Run};Username=kofte_provisioner_{Run};Password=provisioner-canary;Application Name=kofte_provisioning_{Run}";
        var config = StorageMaintenanceConfiguration.Parse(env, Directory.GetCurrentDirectory());
        Assert.Equal("postgresql", config.CreateInstallation().Provider);
        Assert.DoesNotContain("provisioner-canary", config.ToString());
        env["KOFTE_OTHER"] = "not-accepted";
        Assert.Throws<BootstrapException>(() => StorageMaintenanceConfiguration.Parse(env, Directory.GetCurrentDirectory()));
        Assert.Throws<BootstrapException>(() => StorageMaintenanceConfiguration.Parse(
            new Dictionary<string, string?> { ["KOFTE_POSTGRES_PROVISIONING_CONNECTION"] = "secret" },
            Directory.GetCurrentDirectory()));
    }
}
