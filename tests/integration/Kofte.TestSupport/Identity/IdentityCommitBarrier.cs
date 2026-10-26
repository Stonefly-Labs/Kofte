using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kofte.Infrastructure.Storage;

namespace Kofte.TestSupport.Identity;

public sealed class IdentityCommitBarrier
{
    private static readonly HashSet<string> Phases =
    [
        "fixture_ready", "login_before_commit", "login_committed",
        "enrollment_before_commit", "enrollment_committed", "operator_before_commit",
        "operator_committed", "restore_quarantined", "restore_before_commit", "restore_committed"
    ];
    private static IDisposable? startupObserver;
    private readonly string nonce = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
    private readonly string phase;

    public IdentityCommitBarrier(string privateDirectory, string phase, bool maintenanceChild = false)
    {
        if (!Phases.Contains(phase)) throw new InvalidOperationException("identity_fixture_phase_invalid");
        UnixStorePaths.ValidateRoot(privateDirectory, allowMissing: false);
        this.phase = phase;
        DescriptorPath = Path.Combine(privateDirectory, "barrier-" + Guid.NewGuid().ToString("N") + ".json");
        PrivateTestFiles.WriteNew(DescriptorPath, JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = 1,
            phase,
            nonce,
            target = maintenanceChild ? "--identity-worker" : "--identity-fixture-worker"
        }));
    }

    public string DescriptorPath { get; }
    public int? ObservedProcessId { get; private set; }

    public async Task WaitAsync(CancellationToken token)
    {
        var path = DescriptorPath + ".reached";
        while (!File.Exists(path)) await Task.Delay(5, token);
        UnixStorePaths.ValidateFile(path);
        var bytes = await File.ReadAllBytesAsync(path, token);
        if (bytes.Length > 1024) throw new InvalidOperationException("identity_fixture_phase_invalid");
        using var document = JsonDocument.Parse(bytes, new() { MaxDepth = 2, AllowDuplicateProperties = false });
        var value = document.RootElement;
        if (value.GetProperty("nonce").GetString() != nonce || value.GetProperty("phase").GetString() != phase ||
            value.GetProperty("processId").GetInt32() is not > 0)
            throw new InvalidOperationException("identity_fixture_phase_invalid");
        ObservedProcessId = value.GetProperty("processId").GetInt32();
    }

    public void Release()
    {
        if (ObservedProcessId is null) throw new InvalidOperationException("identity_fixture_phase_not_reached");
        PrivateTestFiles.WriteNew(DescriptorPath + ".release", Encoding.ASCII.GetBytes(nonce));
    }

    public static void InstallStartupHook()
    {
        var path = Environment.GetEnvironmentVariable("IDENTITY_TEST_BARRIER_FILE");
        if (path is null) return;
        UnixStorePaths.ValidateRoot(Path.GetDirectoryName(path)!, allowMissing: false);
        UnixStorePaths.ValidateFile(path);
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length > 1024) throw new InvalidOperationException("identity_fixture_admission_invalid");
        using var document = JsonDocument.Parse(bytes, new() { MaxDepth = 2, AllowDuplicateProperties = false });
        var value = document.RootElement;
        var target = value.GetProperty("target").GetString();
        if (!value.EnumerateObject().Select(item => item.Name).ToHashSet(StringComparer.Ordinal)
            .SetEquals(["version", "phase", "nonce", "target"]) ||
            value.GetProperty("version").GetInt32() != 1 ||
            value.GetProperty("phase").GetString() is not { } phase || !Phases.Contains(phase) ||
            value.GetProperty("nonce").GetString() is not { Length: 32 } nonce ||
            !nonce.All(char.IsAsciiHexDigitLower) ||
            target is not ("--identity-worker" or "--identity-fixture-worker"))
            throw new InvalidOperationException("identity_fixture_admission_invalid");
        if (Environment.GetCommandLineArgs().ElementAtOrDefault(1) != target) return;
        if (startupObserver is not null) throw new InvalidOperationException("identity_fixture_admission_replayed");
        File.Move(path, path + ".claimed", overwrite: false);
        startupObserver = new Observer(path, phase, nonce);
    }

    private sealed class Observer : IObserver<DiagnosticListener>,
        IObserver<KeyValuePair<string, object?>>, IDisposable
    {
        private readonly string path;
        private readonly string phase;
        private readonly string nonce;
        private readonly List<IDisposable> subscriptions = [];
        private readonly IDisposable all;
        private int reached;

        internal Observer(string path, string phase, string nonce)
        {
            (this.path, this.phase, this.nonce) = (path, phase, nonce);
            all = DiagnosticListener.AllListeners.Subscribe(this);
        }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "Kofte.Identity") subscriptions.Add(listener.Subscribe(this));
        }

        public void OnNext(KeyValuePair<string, object?> item)
        {
            if (item.Key != "identity.phase" || item.Value is null) return;
            var value = JsonSerializer.SerializeToElement(item.Value);
            if (value.GetProperty("phase").GetString() != phase ||
                Interlocked.CompareExchange(ref reached, 1, 0) != 0) return;
            PrivateTestFiles.WriteNew(path + ".reached", JsonSerializer.SerializeToUtf8Bytes(new
            {
                phase,
                nonce,
                processId = Environment.ProcessId
            }));
            var started = Stopwatch.GetTimestamp();
            while (!File.Exists(path + ".release"))
            {
                if (Stopwatch.GetElapsedTime(started) > TimeSpan.FromSeconds(20))
                    throw new InvalidOperationException("identity_fixture_barrier_expired");
                Thread.Sleep(5);
            }
            UnixStorePaths.ValidateFile(path + ".release");
            if (File.ReadAllText(path + ".release", Encoding.ASCII) != nonce)
                throw new InvalidOperationException("identity_fixture_release_invalid");
            File.Move(path + ".release", path + ".released", overwrite: false);
        }

        public void OnCompleted() { }
        public void OnError(Exception error) => throw new InvalidOperationException("identity_fixture_observation_failed");
        public void Dispose()
        {
            all.Dispose();
            foreach (var subscription in subscriptions) subscription.Dispose();
        }
    }
}
