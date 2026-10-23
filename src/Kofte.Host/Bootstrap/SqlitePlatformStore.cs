using Microsoft.Data.Sqlite;

namespace Kofte.Host.Bootstrap;

public sealed partial class SqlitePlatformStore(string root) : IPlatformStore
{
    public string Root { get; } = root;
    public string DatabasePath => Path.Combine(Root, "platform", "platform.db");

    public async Task InitializeAsync()
    {
        if (OperatingSystem.IsWindows())
            throw new BootstrapException(BootstrapError.ConfigurationInvalid);
        UnixStorePaths.ValidateRoot(Root, allowMissing: true);
        Directory.CreateDirectory(Root, UnixStorePaths.DirectoryMode);
        UnixStorePaths.ValidateRoot(Root, allowMissing: false);
        using var lease = StoreLease.Acquire(Root);
        RejectModuleEvidence(Root);
        var directory = Path.GetDirectoryName(DatabasePath)!;
        UnixStorePaths.ValidateRoot(directory, allowMissing: true);
        Directory.CreateDirectory(directory, UnixStorePaths.DirectoryMode);
        UnixStorePaths.ValidateRoot(directory, allowMissing: false);
        if (UnixStorePaths.ValidateFile(DatabasePath, allowMissing: true))
            throw new BootstrapException(BootstrapError.DestinationExists);
        await using (var created = new FileStream(DatabasePath, new FileStreamOptions
        {
            Mode = System.IO.FileMode.CreateNew,
            Access = FileAccess.ReadWrite,
            Share = FileShare.None,
            UnixCreateMode = UnixStorePaths.FileMode
        })) { }
        await using var connection = OpenCore(allowEmpty: true);
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode=DELETE; PRAGMA user_version=0;";
        await command.ExecuteNonQueryAsync();
    }

    private static void ValidateSqliteHeader(string path)
    {
        using var file = File.OpenRead(path);
        Span<byte> header = stackalloc byte[16];
        if (file.Length < 100 || file.Read(header) != header.Length ||
            !header.SequenceEqual("SQLite format 3\0"u8))
            throw new BootstrapException(BootstrapError.StorageInvalid);
    }

    public SqliteConnection Open() => OpenCore(allowEmpty: false);

    private SqliteConnection OpenCore(bool allowEmpty)
    {
        UnixStorePaths.ValidateRoot(Root, allowMissing: false);
        UnixStorePaths.ValidateRoot(Path.GetDirectoryName(DatabasePath)!, allowMissing: false);
        UnixStorePaths.ValidateFile(DatabasePath);
        if (!allowEmpty) ValidateSqliteHeader(DatabasePath);
        foreach (var suffix in new[] { "-journal", "-wal", "-shm" })
            UnixStorePaths.ValidateFile(DatabasePath + suffix, allowMissing: true);
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false,
            ForeignKeys = true,
            DefaultTimeout = 1
        }.ToString());
        try
        {
            connection.Open();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    public async Task ProbeAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        await using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "BEGIN IMMEDIATE; PRAGMA schema_version; ROLLBACK;";
        await command.ExecuteScalarAsync(token);
        token.ThrowIfCancellationRequested();
    }
}
