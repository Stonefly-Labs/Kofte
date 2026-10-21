using System.Runtime.InteropServices;
using System.Net;
using Kofte.Host.Bootstrap;
using Npgsql;
using Xunit;

namespace Kofte.Host.IntegrationTests;

[Trait("Provider", "sqlite")]
public sealed class StoreFailureTests
{
    [Fact]
    public async Task EmptyFilesAreNotImplicitlyInitialized()
    {
        using var root = new TempRoot();
        var store = new SqlitePlatformStore(root.Path);
        await store.InitializeAsync();
        File.WriteAllBytes(store.DatabasePath, []);
        Assert.Equal(BootstrapError.StorageInvalid, Assert.Throws<BootstrapException>(() => store.Open()).Code);
        Assert.Equal(0, new FileInfo(store.DatabasePath).Length);
    }

    [Fact]
    public async Task HardlinksAndSpecialFilesAreRejectedBeforeOpeningOrDeleting()
    {
        using var root = new TempRoot();
        using var outside = new TempRoot();
        var store = new SqlitePlatformStore(root.Path);
        var sentinel = new SqlitePlatformStore(outside.Path);
        await store.InitializeAsync();
        await sentinel.InitializeAsync();
        var original = File.ReadAllBytes(sentinel.DatabasePath);
        File.Delete(store.DatabasePath);
        if (Link(sentinel.DatabasePath, store.DatabasePath) != 0)
            throw new InvalidOperationException("test_hardlink_creation_failed");
        Assert.Equal(BootstrapError.StorageInvalid, Assert.Throws<BootstrapException>(() => store.Open()).Code);
        Assert.Equal(BootstrapError.StorageInvalid, Assert.Throws<BootstrapException>(() => store.Reset(true)).Code);
        Assert.True(original.AsSpan().SequenceEqual(File.ReadAllBytes(sentinel.DatabasePath)));
        File.Delete(store.DatabasePath);
        if (MakeFifo(store.DatabasePath, 0x180) != 0)
            throw new InvalidOperationException("test_fifo_creation_failed");
        Assert.Equal(BootstrapError.StorageInvalid, Assert.Throws<BootstrapException>(() => store.Open()).Code);
        Assert.Equal(BootstrapError.StorageInvalid, Assert.Throws<BootstrapException>(() => store.Reset(true)).Code);
    }

    [Fact]
    public async Task RestrictedUserRefusesUnwritableStoreWithoutChangingIt()
    {
        if (OperatingSystem.IsWindows() || EffectiveUser() == 0)
            throw new InvalidOperationException("permission_case_requires_supported_nonroot_user");
        using var root = new TempRoot();
        var store = new SqlitePlatformStore(root.Path);
        await store.InitializeAsync();
        var original = File.ReadAllBytes(store.DatabasePath);
        File.SetUnixFileMode(store.DatabasePath, UnixFileMode.UserRead);
        try
        {
            Assert.Equal(BootstrapError.StorageInvalid, Assert.Throws<BootstrapException>(() => store.Open()).Code);
            Assert.True(original.AsSpan().SequenceEqual(File.ReadAllBytes(store.DatabasePath)));
        }
        finally
        {
            File.SetUnixFileMode(store.DatabasePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
        File.SetUnixFileMode(root.Path, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            var failed = await StartupFixture.RunExitAsync(root.Path, [], TestContext.Current.CancellationToken);
            Assert.Equal(1, failed.ExitCode);
            Assert.Contains("storage_invalid", failed.Output);
            Assert.DoesNotContain(root.Path, failed.Output);
            Assert.True(original.AsSpan().SequenceEqual(File.ReadAllBytes(store.DatabasePath)));
        }
        finally
        {
            File.SetUnixFileMode(root.Path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    [DllImport("libc", EntryPoint = "link", SetLastError = true)]
    private static extern int Link(string existing, string link);
    [DllImport("libc", EntryPoint = "mkfifo", SetLastError = true)]
    private static extern int MakeFifo(string path, uint mode);
    [DllImport("libc", EntryPoint = "geteuid")]
    private static extern uint EffectiveUser();
}

[Trait("Provider", "postgres")]
public sealed class PostgresFailureTests
{
    [Fact]
    public async Task WrongCredentialsAndAnActualOwnedServiceOutageNeverSelectSqlite()
    {
        var token = TestContext.Current.CancellationToken;
        await using var database = await PostgresFixture.CreateAsync(
            Environment.GetEnvironmentVariable("KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE") == "true", token: token);
        var wrong = new NpgsqlConnectionStringBuilder(database.ConnectionString)
        {
            Password = "credential-canary-" + Guid.NewGuid().ToString("N")
        };
        await Assert.ThrowsAnyAsync<NpgsqlException>(
            () => new PostgresPlatformStore(wrong.ConnectionString).ProbeAsync(token));
        using var sqliteRoot = new TempRoot();
        var invalidStartup = await StartupFixture.RunExitAsync(sqliteRoot.Path, [], token,
            postgres: wrong.ConnectionString);
        Assert.Equal(1, invalidStartup.ExitCode);
        Assert.Contains("storage_unavailable", invalidStartup.Output);
        Assert.DoesNotContain(wrong.Password!, invalidStartup.Output);
        Assert.DoesNotContain(sqliteRoot.Path, invalidStartup.Output);
        Assert.DoesNotContain("Npgsql", invalidStartup.Output);
        Assert.False(Directory.Exists(sqliteRoot.Path));
        await using var host = await StartupFixture.StartAsync(sqliteRoot.Path, token,
            postgres: database.ConnectionString);
        await database.SetAvailableAsync(false, token);
        try
        {
            using var ready = await host.Client.GetAsync("/health/ready", token);
            using var live = await host.Client.GetAsync("/health/live", token);
            Assert.Equal(HttpStatusCode.ServiceUnavailable, ready.StatusCode);
            Assert.Equal(HttpStatusCode.OK, live.StatusCode);
            Assert.False(Directory.Exists(sqliteRoot.Path));
        }
        finally { await database.SetAvailableAsync(true, token); }
        using var recovered = await host.Client.GetAsync("/health/ready", token);
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        Assert.False(Directory.Exists(sqliteRoot.Path));
    }
}
