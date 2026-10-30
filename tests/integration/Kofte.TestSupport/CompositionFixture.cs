using System.Diagnostics;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.TestSupport.Identity;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Npgsql;

namespace Kofte.TestSupport;

public enum CompositionTestCatalog
{
    Default, MalformedManifest, DuplicateManifest, IncompatibleManifest,
    MissingDependency, DisabledDependency, IncompatibleDependency, PrereleaseDependency, DependencyCycle,
    MismatchedClaim, ReservedClaim, OverlappingClaim, UnsupportedClaim, CorrectedClaim,
    CatalogLimit, EnabledLimit, ManifestByteLimit, ClaimLimit,
    IgnoringValidator, RequiredOutputFault, SnapshotTransport,
    EmptyBoot, BootDependency, BootDisabled, BootMissing,
    DisabledPrerequisitesAtLimit, DisabledPrerequisitesOverLimit, DisabledPrerequisitesWithEnabled,
    DisabledPrerequisitesMaximum, DisabledPrerequisitesMaximumWithEnabled,
    RelationalInitial, RelationalCurrent, RelationalInvalidRecipe, RelationalHoldMigration,
    RelationalConfigurationGate, RelationalFailInitialize, RelationalFailStart, RelationalFailHealth,
    RelationalIgnoreValidator, RelationalIgnoreRegistrar, RelationalIgnoreInitialize, RelationalIgnoreStart,
    RelationalIgnoreHealth, RelationalIgnoreStop, RelationalIgnoreCleanup, RelationalCancelBeforeWork,
    RelationalCancelInitialize, RelationalCleanupOverflow, RelationalFailCleanup
}

public sealed record CompositionSnapshotProbeResult(ProcessOutput Output, bool RootExitVerified,
    bool AdmissionObserved, bool SelectionReplaced, TimeSpan Elapsed);

public sealed class CompositionFixture : IAsyncDisposable
{
    private readonly OwnedTestRoot privateRoot = new();
    private readonly List<CompositionProcess> processes = [];
    private CompositionAuthorTrace? authorTrace;
    private bool probeRunning;
    private bool disposed;
    private ServingIdentity? identity;

    private CompositionFixture(StorageProviderScope storage)
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        Storage = storage;
        Directory.CreateDirectory(privateRoot.Path, UnixStorePaths.DirectoryMode);
        UnixStorePaths.ValidateRoot(privateRoot.Path, allowMissing: false);
    }

    public StorageProviderScope Storage { get; }
    public string SelectionPath => Path.Combine(privateRoot.Path, "modules.json");
    public string Canary { get; } = "composition-private-" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
    public Uri HostUrl { get; private set; } = new("http://127.0.0.1:1");
    public Uri? WebUrl => identity?.WebUrl;
    public CompositionAuthorTrace AuthorTrace => authorTrace ??
        throw new InvalidOperationException("composition_author_probe_not_started");

    public async Task PrepareIdentityAsync(CancellationToken token)
    {
        RequireStopped();
        if (identity is not null) return;
        var prepared = new ServingIdentity();
        try
        {
            await prepared.PrepareAsync(start => Storage.ConfigureChild(start, offlineMaintenance: true), token);
            identity = prepared;
        }
        catch { prepared.Dispose(); throw; }
    }

    public CompositionProcess StartBoot(CompositionTestCatalog catalog, bool explicitSelection = true,
        bool observeAuthor = false)
    {
        if (catalog is not (CompositionTestCatalog.EmptyBoot or CompositionTestCatalog.BootDependency or
            CompositionTestCatalog.BootDisabled or CompositionTestCatalog.BootMissing or
            CompositionTestCatalog.DisabledPrerequisitesAtLimit or CompositionTestCatalog.DisabledPrerequisitesOverLimit or
            CompositionTestCatalog.DisabledPrerequisitesWithEnabled or CompositionTestCatalog.DisabledPrerequisitesMaximum or
            CompositionTestCatalog.DisabledPrerequisitesMaximumWithEnabled or
            CompositionTestCatalog.RelationalInitial or CompositionTestCatalog.RelationalCurrent))
            throw new InvalidOperationException("composition_fixture_catalog_invalid");
        var start = CreateStart("serve", catalog, explicitSelection);
        if (observeAuthor)
        {
            BeginAuthorTrace(start);
        }
        var process = new CompositionProcess(OwnedProcesses.Start(start, captureOutput: false), Canary, observeOutput: true);
        processes.Add(process);
        return process;
    }

    public async Task<RecoveryInventory> InspectStoppedInventoryAsync(CancellationToken token)
    {
        RequireStopped();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);
            var installation = Installation();
            await using var scope = await new StorageMaintenance(installation).AcquireAsync(token);
            return await RecoverySetVerifier.InspectStoppedAsync(installation, scope, token);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    public static async Task<CompositionFixture> CreateAsync(string provider, CancellationToken token)
    {
        var storage = await StorageProviderScope.CreateAsync(provider, token);
        try { return new(storage); }
        catch { await storage.DisposeAsync(); throw; }
    }

    public async Task WriteSelectionAsync(ReadOnlyMemory<byte> document, CancellationToken token)
    {
        RequireStopped();
        await WriteSelectionCoreAsync(document, token);
    }

    private async Task WriteSelectionCoreAsync(ReadOnlyMemory<byte> document, CancellationToken token)
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        ObjectDisposedException.ThrowIf(disposed, this);
        if (document.Length > 1_048_576) throw new InvalidOperationException("composition_selection_limit");
        UnixStorePaths.ValidateRoot(privateRoot.Path, allowMissing: false);
        UnixStorePaths.ValidateFile(SelectionPath, allowMissing: true);
        var temporary = Path.Combine(privateRoot.Path, "modules.next");
        UnixStorePaths.ValidateFile(temporary, allowMissing: true);
        await using (var file = new FileStream(temporary, new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            Share = FileShare.None,
            UnixCreateMode = UnixStorePaths.FileMode
        }))
        {
            await file.WriteAsync(document, token);
            await file.FlushAsync(token);
        }
        UnixStorePaths.ValidateFile(temporary);
        File.Move(temporary, SelectionPath, overwrite: true);
        UnixStorePaths.ValidateFile(SelectionPath);
    }

    public CompositionProcess Start(string mode, CompositionTestCatalog catalog = CompositionTestCatalog.Default,
        bool observeAuthor = false, int preferredHostPort = 0)
    {
        var start = CreateStart(mode, catalog, preferredHostPort: preferredHostPort);
        if (observeAuthor) BeginAuthorTrace(start);
        var process = new CompositionProcess(OwnedProcesses.Start(start, captureOutput: !observeAuthor), Canary,
            observeOutput: observeAuthor);
        processes.Add(process);
        return process;
    }

    public async Task<T> InDeploymentEnvironmentAsync<T>(string mode, CompositionTestCatalog catalog, Func<Task<T>> run)
    {
        RequireStopped();
        var start = CreateStart(mode, catalog);
        var keys = Environment.GetEnvironmentVariables().Keys.Cast<string>().Concat(start.Environment.Keys)
            .Where(DeploymentKey).Distinct(StringComparer.Ordinal).ToArray();
        var previous = keys.ToDictionary(key => key, Environment.GetEnvironmentVariable, StringComparer.Ordinal);
        probeRunning = true;
        try
        {
            foreach (var key in keys)
                Environment.SetEnvironmentVariable(key, start.Environment.TryGetValue(key, out var value) ? value : null);
            return await run();
        }
        finally
        {
            foreach (var (key, value) in previous) Environment.SetEnvironmentVariable(key, value);
            probeRunning = false;
        }

        static bool DeploymentKey(string key) => key.StartsWith("KOFTE_", StringComparison.Ordinal) ||
            key.StartsWith("OTEL_", StringComparison.Ordinal) || key.StartsWith("COMPOSITION_TEST_", StringComparison.Ordinal) ||
            key is "ASPNETCORE_ENVIRONMENT" or "DOTNET_ENVIRONMENT";
    }

    public void TrackIdleFixtureChild(CompositionProcess owner)
    {
        if (!processes.Contains(owner) || owner.HasExited)
            throw new InvalidOperationException("composition_descendant_ownership_unverified");
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = OwnedProcesses.FindRepository()
        };
        start.ArgumentList.Add(ConsumerDll());
        start.ArgumentList.Add("--composition-fixture-descendant");
        OwnedProcesses.ClearSensitiveEnvironment(start);
        start.Environment.Remove("COMPOSITION_TEST_PROBE");
        owner.TrackFixtureDescendant(OwnedProcesses.Start(start));
    }

    private void BeginAuthorTrace(ProcessStartInfo start)
    {
        if (authorTrace is not null)
        {
            if (!authorTrace.EndOfStreamVerified)
                throw new InvalidOperationException("composition_author_probe_previous_exit_unverified");
            authorTrace.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        authorTrace = new(Path.GetRelativePath(OwnedProcesses.FindRepository(), privateRoot.Path), Storage.Token);
        start.Environment["COMPOSITION_TEST_PROBE"] = authorTrace.Path;
    }

    private ProcessStartInfo CreateStart(string mode, CompositionTestCatalog catalog, bool explicitSelection = true,
        int preferredHostPort = 0)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (probeRunning || processes.Any(process => !process.HasExited))
            throw new InvalidOperationException("composition_fixture_process_already_running");
        if (mode is not ("validate" or "prepare" or "serve"))
            throw new InvalidOperationException("composition_fixture_mode_invalid");
        if (!Enum.IsDefined(catalog))
            throw new InvalidOperationException("composition_fixture_catalog_invalid");
        var dll = ConsumerDll();
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = OwnedProcesses.FindRepository()
        };
        start.ArgumentList.Add(dll);
        start.ArgumentList.Add("--deployment");
        if (mode != "serve")
        {
            start.ArgumentList.Add("--modules");
            start.ArgumentList.Add(mode);
        }
        Storage.ConfigureChild(start, offlineMaintenance: mode == "prepare");
        start.Environment["COMPOSITION_TEST_CATALOG"] = catalog.ToString();
        start.Environment.Remove("COMPOSITION_TEST_PROBE");
        if (explicitSelection)
        {
            UnixStorePaths.ValidateFile(SelectionPath);
            start.Environment["KOFTE_MODULES_FILE"] = SelectionPath;
        }
        using (var port = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
            port.Bind(new IPEndPoint(IPAddress.Loopback, preferredHostPort));
            var selected = (IPEndPoint)port.LocalEndPoint!;
            if (selected.Port == identity?.WebUrl.Port)
            {
                // Keep the colliding candidate bound so the replacement cannot reuse it.
                using var replacement = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                replacement.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                selected = (IPEndPoint)replacement.LocalEndPoint!;
            }
            HostUrl = new("http://127.0.0.1:" + selected.Port);
        }
        start.Environment["KOFTE_HOST_URL"] = HostUrl.AbsoluteUri;
        identity?.ConfigureChild(start);
        return start;
    }

    private static string ConsumerDll()
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(OwnedProcesses.FindRepository(),
            "tests/contract/Kofte.Composition.Consumer/bin", configuration, "net10.0/Kofte.Composition.Consumer.dll");
        return File.Exists(dll) ? dll : throw new InvalidOperationException("composition_consumer_not_built");
    }

    public async Task SeedUnrelatedNoStorageAsync(CancellationToken token)
    {
        RequireStopped();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            Console.SetOut(TextWriter.Null);
            Console.SetError(TextWriter.Null);
            var installation = Installation();
            var maintenance = new StorageMaintenance(installation);
            await using var scope = await maintenance.AcquireAsync(token);
            await maintenance.UpgradeAsync(scope, token);
            var manifest = ManifestValidator.Validate("""
                {"schemaVersion":"1","id":"org.example.validation-unrelated","version":"1.0.0",
                 "displayName":"Unrelated validation fixture","hostContract":"1.0.0",
                 "capabilities":[],"dependencies":[],"permissions":[],"storage":{"relational":false}}
                """u8, new("1.0.0"));
            if (manifest is not ManifestValidationResult.Accepted accepted)
                throw new InvalidOperationException("composition_fixture_seed_failed");
            var registry = new StorageRegistry(installation);
            if (await registry.RegisterAsync(accepted.Description, scope, token) is not StorageResult.NoStorage ||
                await registry.ResolveAsync(accepted.Description.Id, token) is not StorageResult.NoStorage)
                throw new InvalidOperationException("composition_fixture_seed_failed");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception) { throw new InvalidOperationException("composition_fixture_seed_failed"); }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    public Task<IStorageMaintenanceScope> HoldMaintenanceAsync(CancellationToken token)
    {
        RequireStopped();
        return new StorageMaintenance(Installation()).AcquireAsync(token);
    }

    public async Task SeedOwnedOrphanAsync(CancellationToken token)
    {
        RequireStopped();
        try
        {
            await using var scope = await HoldMaintenanceAsync(token);
            if (Storage.Postgres is { } postgres)
            {
                await using var connection = new NpgsqlConnection(postgres.ProvisioningConnectionString);
                await connection.OpenAsync(token);
                await using var command = new NpgsqlCommand("CREATE SCHEMA composition_orphan", connection) { CommandTimeout = 3 };
                await command.ExecuteNonQueryAsync(token);
            }
            else
            {
                if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
                var modules = Path.Combine(Storage.DataRoot, "modules");
                Directory.CreateDirectory(modules, UnixStorePaths.DirectoryMode);
                UnixStorePaths.ValidateRoot(modules, allowMissing: false);
                Directory.CreateDirectory(Path.Combine(modules, "composition_orphan"), UnixStorePaths.DirectoryMode);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception) { throw new InvalidOperationException("composition_fixture_orphan_seed_failed"); }
    }

    private StorageInstallation Installation() => Storage.Postgres is null
        ? StorageInstallation.Sqlite(Storage.DataRoot)
        : StorageInstallation.PostgreSql(Storage.Postgres.ConnectionString, Storage.Postgres.ProvisioningConnectionString);

    private void RequireStopped()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (probeRunning || processes.Any(process => !process.HasExited))
            throw new InvalidOperationException("composition_selection_requires_stopped_processes");
    }

    public async Task<CompositionSnapshotProbeResult> RunSnapshotTransportProbeAsync(CancellationToken token)
    {
        RequireStopped();
        var original = Encoding.UTF8.GetBytes(
            "{\"schemaVersion\":\"1\",\"modules\":[{\"id\":\"org.example.validation\",\"enabled\":true,\"settings\":{ \"secret\" : \""
            + Canary + "\", \"escaped\" : \"\\u0130\", \"flag\" : true }}]}");
        var changed = """
            {"schemaVersion":"1","modules":[{"id":"org.example.validation","enabled":true,
             "settings":{"secret":"changed","escaped":"changed","flag":false}}]}
            """u8.ToArray();
        await WriteSelectionCoreAsync(original, token);
        var start = CreateStart("validate", CompositionTestCatalog.SnapshotTransport);
        var started = Stopwatch.GetTimestamp();
        var admitted = false;
        var replaced = false;
        probeRunning = true;
        try
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(50));
            await using var process = OwnedProcesses.Start(start, captureOutput: false);
            var capture = CaptureAsync(process, deadline.Token);
            var result = await process.WaitAsync(deadline.Token);
            var stdout = await capture;
            var stored = await File.ReadAllBytesAsync(SelectionPath, token);
            replaced &= stored.AsSpan().SequenceEqual(changed);
            if (stdout.Contains(Canary, StringComparison.Ordinal) || result.Stderr.Contains(Canary, StringComparison.Ordinal))
                throw new InvalidOperationException("composition_fixture_output_not_private");
            return new(result with { Stdout = stdout }, process.HasExited, admitted, replaced,
                Stopwatch.GetElapsedTime(started));
        }
        finally { probeRunning = false; }

        async Task<string> CaptureAsync(OwnedProcess process, CancellationToken cancellation)
        {
            using var reader = new StreamReader(process.StandardOutput, Encoding.UTF8, false, 1024, leaveOpen: true);
            var buffer = new char[1024];
            var line = new StringBuilder();
            var text = new StringBuilder();
            int count;
            while ((count = await reader.ReadAsync(buffer.AsMemory(), cancellation)) != 0)
                for (var index = 0; index < count; index++)
                {
                    var value = buffer[index];
                    text.Append(value);
                    if (text.Length > 65_536) throw new InvalidOperationException("composition_snapshot_probe_output_limit");
                    if (value == '\n')
                    {
                        await ObserveAsync(line.ToString(), cancellation);
                        line.Clear();
                    }
                    else
                    {
                        line.Append(value);
                        if (line.Length > 4096) throw new InvalidOperationException("composition_snapshot_probe_output_limit");
                    }
                }
            if (line.Length != 0) await ObserveAsync(line.ToString(), cancellation);
            var output = text.ToString();
            if (Encoding.UTF8.GetByteCount(output) > 65_536)
                throw new InvalidOperationException("composition_snapshot_probe_output_limit");
            return output;
        }

        async Task ObserveAsync(string line, CancellationToken cancellation)
        {
            if (Encoding.UTF8.GetByteCount(line) > 4096)
                throw new InvalidOperationException("composition_snapshot_probe_output_limit");
            try
            {
                using var document = JsonDocument.Parse(line,
                    new JsonDocumentOptions { MaxDepth = 8, AllowDuplicateProperties = false });
                var record = document.RootElement;
                if (record.GetProperty("stage").GetString() != "admission") return;
                if (admitted || record.GetProperty("version").GetInt32() != 1 ||
                    record.GetProperty("mode").GetString() != "validate" ||
                    record.GetProperty("outcome").GetString() != "passed")
                    throw new InvalidOperationException("composition_snapshot_probe_record_invalid");
                admitted = true;
                await WriteSelectionCoreAsync(changed, cancellation);
                replaced = true;
            }
            catch (JsonException) { throw new InvalidOperationException("composition_snapshot_probe_record_invalid"); }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
        foreach (var process in processes.AsEnumerable().Reverse()) await process.DisposeAsync();
        try
        {
            if (authorTrace is not null) await authorTrace.DisposeAsync();
        }
        finally
        {
            await Storage.DisposeAsync();
            identity?.Dispose();
            privateRoot.Dispose();
            disposed = true;
        }
    }

    public sealed record CompositionAuthorRecord(string Owner, string Phase, int Value, string Attempt, int Worker);

    public sealed class CompositionAuthorTrace : IAsyncDisposable
    {
        private readonly Socket listener = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        private readonly CancellationTokenSource lifetime;
        private readonly Task capture;
        private readonly object gate = new();
        private readonly List<CompositionAuthorRecord> records = [];
        private NetworkStream? connection;
        private readonly TaskCompletionSource connected = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private string? attempt;
        private int? worker;
        private readonly string socketFile;
        public string Path { get; }
        public bool EndOfStreamVerified { get; private set; }
        public IReadOnlyList<CompositionAuthorRecord> Records { get { lock (gate) return records.ToArray(); } }

        internal CompositionAuthorTrace(string directory, CancellationToken token)
        {
            Path = directory + "/p";
            socketFile = System.IO.Path.Combine(OwnedProcesses.FindRepository(), Path);
            lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
            // The test runner and child can have different working directories.
            listener.Bind(new UnixDomainSocketEndPoint(
                System.IO.Path.GetRelativePath(Environment.CurrentDirectory, socketFile)));
            listener.Listen(1);
            capture = CaptureAsync(lifetime.Token);
        }

        public async Task WaitForAsync(string owner, string phase, CancellationToken token)
        {
            while (!Records.Any(record => record.Owner == owner && record.Phase == phase))
            {
                if (capture.IsCompleted)
                {
                    await capture;
                    throw new InvalidOperationException("composition_author_observation_missing");
                }
                await Task.Delay(5, token);
            }
        }

        public async Task ReleaseStartAsync(CancellationToken token)
        {
            await connected.Task.WaitAsync(token);
            await connection!.WriteAsync(new byte[] { 1 }, token);
            await connection.FlushAsync(token);
        }

        public async Task CompleteAsync(CancellationToken token)
        {
            await EndAsync(token);
            if (Records.LastOrDefault() is not
                { Owner: "org.example.probe", Phase: "complete", Value: 0 })
                throw new InvalidOperationException("composition_author_exit_unverified");
        }

        public async Task EndAsync(CancellationToken token)
        {
            await capture.WaitAsync(token);
            if (!EndOfStreamVerified) throw new InvalidOperationException("composition_author_exit_unverified");
        }

        public static CompositionAuthorRecord ParseFrame(ReadOnlyMemory<byte> frame)
        {
            if (frame.Length is < 1 or > 512) throw Invalid();
            try
            {
                using var document = JsonDocument.Parse(frame, new() { MaxDepth = 2, AllowDuplicateProperties = false });
                var root = document.RootElement;
                var names = root.EnumerateObject().Select(item => item.Name).ToArray();
                if (names.Length != 7 || !names.ToHashSet(StringComparer.Ordinal).SetEquals(
                    ["version", "owner", "phase", "value", "attempt", "worker", "sequence"]) ||
                    root.GetProperty("version").GetInt32() != 1 || root.GetProperty("sequence").GetInt32() is < 1 or > 256)
                    throw Invalid();
                var owner = root.GetProperty("owner").GetString();
                var phase = root.GetProperty("phase").GetString();
                var attempt = root.GetProperty("attempt").GetString();
                var value = root.GetProperty("value").GetInt32();
                var worker = root.GetProperty("worker").GetInt32();
                if (owner is not ("org.example.alpha" or "org.example.beta" or "org.example.no-storage" or "org.example.probe") ||
                    phase is not ("validate" or "register" or "service-constructor" or "runtime-constructor" or
                        "initialize" or "cross-owner-denied" or "dependency-bound" or "boundary-dependency-bound" or
                        "complete-dependencies-bound" or
                        "compute" or "start-held" or
                        "no-storage" or "start" or "health" or "stop" or "cleanup" or "dispose" or "migration" or
                        "descendant" or "callback-total" or "constructor-total" or "registrar-total" or "runtime-total" or
                        "cleanup-total" or "migration-total" or "complete") ||
                    (owner == "org.example.probe") != (phase == "complete") ||
                    attempt is not { Length: 32 } || !attempt.All(char.IsAsciiHexDigitLower) ||
                    worker <= 0 || (phase == "descendant" ? value <= 0 : value is < -1_000 or > 1_000))
                    throw Invalid();
                return new(owner, phase, value, attempt, worker);
            }
            catch (Exception error) when (error is JsonException or InvalidOperationException or
                KeyNotFoundException or FormatException or OverflowException)
            {
                throw Invalid();
            }
        }

        private async Task CaptureAsync(CancellationToken token)
        {
            using var socket = await listener.AcceptAsync(token);
            await using var stream = new NetworkStream(socket, ownsSocket: false);
            connection = stream;
            connected.TrySetResult();
            var header = new byte[4];
            while (true)
            {
                var first = await stream.ReadAsync(header.AsMemory(0, 1), token);
                if (first == 0) break;
                await stream.ReadExactlyAsync(header.AsMemory(1), token);
                var length = BinaryPrimitives.ReadInt32LittleEndian(header);
                if (length is < 1 or > 512) throw Invalid();
                var bytes = new byte[length];
                await stream.ReadExactlyAsync(bytes, token);
                var record = ParseFrame(bytes);
                using var parsed = JsonDocument.Parse(bytes);
                lock (gate)
                {
                    if (records.Count >= 256 || records.LastOrDefault()?.Phase == "complete" ||
                        parsed.RootElement.GetProperty("sequence").GetInt32() != records.Count + 1 ||
                        attempt is not null && attempt != record.Attempt || worker is not null && worker != record.Worker)
                        throw Invalid();
                    (attempt, worker) = (record.Attempt, record.Worker);
                    records.Add(record);
                }
            }
            EndOfStreamVerified = true;
        }

        public async ValueTask DisposeAsync()
        {
            lifetime.Cancel();
            listener.Dispose();
            try { await capture; }
            catch (OperationCanceledException) when (lifetime.IsCancellationRequested) { }
            catch (ObjectDisposedException) when (lifetime.IsCancellationRequested) { }
            finally
            {
                lifetime.Dispose();
                if (File.Exists(socketFile)) File.Delete(socketFile);
            }
        }

        private static InvalidOperationException Invalid() => new("composition_author_trace_invalid");
    }
}
