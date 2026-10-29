using System.Security.Cryptography;
using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Npgsql;

namespace Kofte.TestSupport;

public sealed class PostgresAssemblyFixture : IAsyncDisposable
{
    private static PostgresAssemblyFixture? current;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly OwnedTestRoot root = new();
    private PostgresService? service;
    private string connection = "";
    private int borrowers;
    private bool disposed;

    public PostgresAssemblyFixture()
    {
        if (Interlocked.CompareExchange(ref current, this, null) is not null)
            throw new InvalidOperationException("postgres_assembly_fixture_preexists");
    }

    internal static PostgresAssemblyFixture? Current => Volatile.Read(ref current);

    internal async Task<(PostgresService Service, string Connection)> BorrowAsync(CancellationToken token)
    {
        await gate.WaitAsync(token);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (service is null)
            {
                if (OperatingSystem.IsWindows())
                    throw new InvalidOperationException("postgres_platform_unsupported");
                Directory.CreateDirectory(root.Path, UnixStorePaths.DirectoryMode);
                var password = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
                service = new(Guid.NewGuid().ToString("N"), password);
                await service.StartAsync(token);
                connection = new NpgsqlConnectionStringBuilder
                {
                    Host = "127.0.0.1",
                    Port = service.Port,
                    Database = "postgres",
                    Username = "kofte_admin",
                    Password = password,
                    Pooling = false,
                    SslMode = SslMode.Disable,
                    Timeout = 3,
                    CommandTimeout = 3,
                    IncludeErrorDetail = false
                }.ConnectionString;
                PrivateTestFiles.WriteNew(Path.Combine(root.Path, "service-ownership.json"),
                    JsonSerializer.SerializeToUtf8Bytes(new { service.RunId, service.Resources }));
            }
            if (connection.Length == 0)
                throw new InvalidOperationException("postgres_assembly_fixture_start_incomplete");
            Interlocked.Increment(ref borrowers);
            return (service, connection);
        }
        finally { gate.Release(); }
    }

    internal void Release()
    {
        if (Interlocked.Decrement(ref borrowers) < 0)
            throw new InvalidOperationException("postgres_assembly_fixture_release_invalid");
    }

    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (disposed) return;
            if (Volatile.Read(ref borrowers) != 0)
                throw new InvalidOperationException("postgres_assembly_fixture_cleanup_unverified");
            using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            if (service is not null)
            {
                await service.ValidateOwnershipAsync(cleanup.Token);
                if (connection.Length == 0)
                    throw new InvalidOperationException("postgres_assembly_fixture_start_incomplete");
                await using (var admin = new NpgsqlConnection(connection))
                {
                    await admin.OpenAsync(cleanup.Token);
                    await using var command = new NpgsqlCommand("""
                        SELECT NOT EXISTS (SELECT 1 FROM pg_catalog.pg_database
                          WHERE NOT datistemplate AND datname<>'postgres')
                        AND NOT EXISTS (SELECT 1 FROM pg_catalog.pg_roles
                          WHERE rolname !~ '^pg_' AND rolname<>'kofte_admin')
                        AND NOT EXISTS (SELECT 1 FROM pg_catalog.pg_stat_activity
                          WHERE backend_type='client backend' AND pid<>pg_backend_pid())
                        """, admin) { CommandTimeout = 3 };
                    if (await command.ExecuteScalarAsync(cleanup.Token) is not true)
                        throw new InvalidOperationException("postgres_assembly_fixture_state_preserved");
                }
                await service.CleanupAsync(cleanup.Token);
            }
            root.Dispose();
            if (!ReferenceEquals(Interlocked.CompareExchange(ref current, null, this), this))
                throw new InvalidOperationException("postgres_assembly_fixture_owner_changed");
            disposed = true;
        }
        finally { gate.Release(); }
    }
}
