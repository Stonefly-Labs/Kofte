using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Registry;
using Npgsql;

namespace Kofte.Storage.IntegrationTests.Providers;

[Collection("Storage telemetry isolation")]
public abstract class StorageMaintenanceDiagnosticsTests
{
    protected abstract string Provider { get; }
    private const string Canary = "/private/maintenance-credential-payload\n\tException";

    [Fact]
    public async Task UpgradeCommitAndIdempotentRetryEmitCorrelatedPrivateRecords()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync(Provider, token, upgrade: false);
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        using var capture = new Capture();
        await fixture.Maintenance.UpgradeAsync(scope, token);
        var before = await SnapshotAsync(fixture, token);
        await fixture.Maintenance.UpgradeAsync(scope, token);
        Assert.Equal(before, await SnapshotAsync(fixture, token));
        await fixture.AssertInventoryAsync(0, 0, token);
        capture.AssertRecords(fixture, "upgrade",
            ("storage_ready", "ready", "retry_same_request", "committed"),
            ("storage_ready", "ready", "retry_same_request", "unchanged"));
    }

    [Theory]
    [InlineData("non_owned")]
    [InlineData("foreign")]
    [InlineData("disposed")]
    public async Task InvalidScopesEmitRejectedRecordsWithoutChangingTheInstallation(string kind)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync(Provider, token);
        var before = await SnapshotAsync(fixture, token);
        await using var owned = await fixture.Maintenance.AcquireAsync(token);
        var maintenance = kind == "foreign" ? new StorageMaintenance(fixture.NewInstallation()) : fixture.Maintenance;
        IStorageMaintenanceScope scope = kind == "non_owned" ? new UnownedScope() : owned;
        if (kind == "disposed") await owned.DisposeAsync();
        using var capture = new Capture();
        var error = await Assert.ThrowsAsync<StorageException>(() => maintenance.UpgradeAsync(scope, token));
        Assert.Equal(StorageCode.RequestInvalid, error.Code);
        Assert.Equal(before, await SnapshotAsync(fixture, token));
        capture.AssertRecords(fixture, "upgrade",
            ("storage_request_invalid", "rejected", "correct_configuration", "unchanged"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnsupportedVersionAndCorruptHistoryEmitRefusalsAndPreserveAllRows(bool unsupported)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync(Provider, token);
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        using (var budget = new OperationBudget(OperationBudget.Register, token))
        await using (var connection = await fixture.Installation.OpenPlatformAsync(budget))
        {
            var prefix = new RegistryMigrationRunner(fixture.Installation).Prefix;
            // Corrupt only this owned fixture; ordinary writes must not bypass immutable metadata.
            var sql = unsupported
                ? (Provider == "sqlite" ? "DROP TRIGGER __kofte_installation_immutable; PRAGMA ignore_check_constraints=ON;" :
                    $"ALTER TABLE {prefix}__kofte_installation DISABLE TRIGGER __kofte_installation_change;" +
                    $"ALTER TABLE {prefix}__kofte_installation DROP CONSTRAINT __kofte_installation_registry_version_check;") +
                  $"UPDATE {prefix}__kofte_installation SET registry_version=9"
                : $"UPDATE {prefix}__kofte_registry_migrations SET sha256='{new string('0', 64)}'";
            await RegistryMigrationRunner.ExecuteAsync(connection, sql, budget);
        }
        var before = await SnapshotAsync(fixture, token);
        using var capture = new Capture();
        var error = await Assert.ThrowsAsync<StorageException>(() => fixture.Maintenance.UpgradeAsync(scope, token));
        Assert.Equal(unsupported ? StorageCode.VersionUnsupported : StorageCode.Corrupt, error.Code);
        Assert.Equal(before, await SnapshotAsync(fixture, token));
        capture.AssertRecords(fixture, "upgrade", unsupported
            ? ("storage_version_unsupported", "failed", "upgrade_supported_binary", "unchanged")
            : ("storage_corrupt", "reconciliationrequired", "restore_complete_set", "unchanged"));
    }

    [Theory]
    [InlineData("registry_before_commit")]
    [InlineData("registry_committed")]
    public async Task UnexpectedCommitBoundaryFailuresAreSanitizedWithoutClaimingRollback(string phase)
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync(Provider, token, upgrade: false);
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        using var capture = new Capture();
        using (var failure = new PhaseFailure(phase))
        using (DiagnosticListener.AllListeners.Subscribe(failure))
        {
            var error = await Assert.ThrowsAsync<StorageException>(() => fixture.Maintenance.UpgradeAsync(scope, token));
            Assert.Equal(StorageCode.Unavailable, error.Code);
            Assert.DoesNotContain("maintenance-credential", error.ToString());
        }
        using var budget = new OperationBudget(OperationBudget.Resolve, token);
        await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
        var count = await RegistryMigrationRunner.ScalarAsync(connection, Provider == "sqlite"
            ? "SELECT count(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'"
            : "SELECT count(*) FROM pg_catalog.pg_tables WHERE schemaname='kofte_system'", budget);
        Assert.Equal(phase == "registry_committed" ? 3L : 0L, count);
        capture.AssertRecords(fixture, "upgrade",
            ("storage_unavailable", "failed", "correct_configuration",
                phase == "registry_committed" ? "committed" : "uncertain"));
    }

    [Fact]
    public async Task ContendedAcquisitionEmitsSafeFailureWithoutASecondLease()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync(Provider, token);
        await using var scope = await fixture.Maintenance.AcquireAsync(token);
        using var capture = new Capture();
        var error = await Assert.ThrowsAsync<StorageException>(() => fixture.Maintenance.AcquireAsync(token));
        Assert.Equal(StorageCode.Busy, error.Code);
        capture.AssertRecords(fixture, "acquire",
            ("storage_busy", "busy", "retry_same_request", "unchanged"));
    }

    private static async Task<string> SnapshotAsync(RegistryFixture fixture, CancellationToken token)
    {
        using var budget = new OperationBudget(OperationBudget.Register, token);
        await using var connection = await fixture.Installation.OpenPlatformAsync(budget);
        var prefix = new RegistryMigrationRunner(fixture.Installation).Prefix;
        var rows = new List<object[]>();
        foreach (var table in new[] { "__kofte_installation", "__kofte_module_registrations", "__kofte_registry_migrations" })
        {
            await using var command = RegistryMigrationRunner.Command(connection, $"SELECT * FROM {prefix}{table} ORDER BY 1", budget);
            await using var reader = await command.ExecuteReaderAsync(token);
            while (await reader.ReadAsync(token))
            {
                var values = new object[reader.FieldCount];
                reader.GetValues(values);
                rows.Add(values);
            }
        }
        var bytes = fixture.Root is { } root
            ? Convert.ToBase64String(await File.ReadAllBytesAsync(Path.Combine(root, "platform", "platform.db"), token)) : null;
        return JsonSerializer.Serialize(new { rows, bytes });
    }

    private sealed class UnownedScope : IStorageMaintenanceScope
    {
        public CancellationToken Lifetime => throw new InvalidOperationException(Canary);
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class PhaseFailure(string phase) : IObserver<DiagnosticListener>,
        IObserver<KeyValuePair<string, object?>>, IDisposable
    {
        private IDisposable? subscription;
        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "Kofte.Storage") subscription = listener.Subscribe(this);
        }
        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key == "storage.phase" &&
                value.Value?.GetType().GetProperty("phase")?.GetValue(value.Value) is string observed && observed == phase)
                throw new InvalidOperationException(Canary);
        }
        public void OnCompleted() { }
        public void OnError(Exception error) => throw error;
        public void Dispose() => subscription?.Dispose();
    }

    private sealed class Capture : IDisposable
    {
        private readonly TextWriter previous = Console.Error;
        private readonly TextWriter previousOutput = Console.Out;
        private readonly StringWriter output = new();
        private readonly StringWriter stdout = new();
        private readonly ConcurrentQueue<(string Correlation, Dictionary<string, object?> Tags)> spans = new();
        private readonly ConcurrentQueue<Dictionary<string, object?>> measurements = new();
        private readonly ActivityListener activities;
        private readonly MeterListener meter;

        internal Capture()
        {
            activities = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "Kofte.Storage",
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity => spans.Enqueue((activity.TraceId.ToHexString(),
                    activity.TagObjects.ToDictionary(pair => pair.Key, pair => pair.Value)))
            };
            ActivitySource.AddActivityListener(activities);
            meter = new MeterListener
            {
                InstrumentPublished = (instrument, listener) =>
                {
                    if (instrument.Meter.Name == "Kofte.Storage") listener.EnableMeasurementEvents(instrument);
                }
            };
            meter.SetMeasurementEventCallback<long>((_, _, tags, _) =>
            {
                var values = new Dictionary<string, object?>();
                foreach (var tag in tags) values.Add(tag.Key, tag.Value);
                measurements.Enqueue(values);
            });
            meter.Start();
            Console.SetError(output);
            Console.SetOut(stdout);
        }

        internal void AssertRecords(RegistryFixture fixture, string operation,
            params (string Code, string Outcome, string Guidance, string Durability)[] expected)
        {
            var lines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(expected.Length, lines.Length);
            Assert.Equal(expected.Length, spans.Count);
            Assert.Equal(expected.Length, measurements.Count);
            var correlations = new HashSet<string>();
            for (var index = 0; index < lines.Length; index++)
            {
                using var document = JsonDocument.Parse(lines[index]);
                var record = document.RootElement;
                Assert.Equal(9, record.EnumerateObject().Count());
                Assert.Equal("storage", record.GetProperty("component").GetString());
                Assert.Equal(operation, record.GetProperty("operation").GetString());
                Assert.Equal(fixture.Installation.Provider, record.GetProperty("provider").GetString());
                Assert.Equal(JsonValueKind.Null, record.GetProperty("moduleId").ValueKind);
                Assert.Equal(expected[index].Code, record.GetProperty("eventCode").GetString());
                Assert.Equal(expected[index].Outcome, record.GetProperty("outcome").GetString());
                Assert.Equal(expected[index].Guidance, record.GetProperty("guidance").GetString());
                Assert.Equal(expected[index].Durability, record.GetProperty("durability").GetString());
                var correlation = record.GetProperty("correlationId").GetString()!;
                Assert.Matches(@"\A[0-9a-f]{32}\z", correlation);
                Assert.True(correlations.Add(correlation));
                var span = Assert.Single(spans, item => item.Correlation == correlation);
                Assert.Equal(expected[index].Code, span.Tags["storage.code"]);
                Assert.Equal(operation, span.Tags["storage.operation"]);
                Assert.Equal(fixture.Installation.Provider, span.Tags["storage.provider"]);
                Assert.Equal(expected[index].Outcome, span.Tags["storage.outcome"]);
                Assert.False(span.Tags.ContainsKey("storage.module_id"));
                Assert.Equal(expected[index].Guidance, span.Tags["storage.guidance"]);
                Assert.Equal(expected[index].Durability, span.Tags["storage.durability"]);
                Assert.True(System.Text.Encoding.UTF8.GetByteCount(lines[index]) <= 4096);
            }
            foreach (var tags in measurements)
            {
                Assert.Equal(["operation", "outcome", "provider"], tags.Keys.Order(StringComparer.Ordinal));
                Assert.Equal(operation, tags["operation"]);
                Assert.Equal(fixture.Installation.Provider, tags["provider"]);
                Assert.Contains(tags["outcome"], expected.Select(item => item.Outcome));
            }
            Assert.Empty(stdout.ToString());
            var channels = output + stdout.ToString() +
                JsonSerializer.Serialize(spans.Select(span => new { span.Correlation, span.Tags })) +
                JsonSerializer.Serialize(measurements);
            var forbidden = new List<string> { Canary, "maintenance-credential", "Exception", "SELECT ", "CREATE TABLE" };
            if (fixture.Root is { } root) forbidden.Add(root);
            if (fixture.RuntimeConnection is { } runtime)
            {
                forbidden.Add(runtime);
                forbidden.Add(new NpgsqlConnectionStringBuilder(runtime).Password!);
            }
            foreach (var text in forbidden)
                Assert.False(channels.Contains(text, StringComparison.Ordinal), "maintenance_telemetry_contains_sensitive_input");
        }

        public void Dispose()
        {
            Console.SetError(previous);
            Console.SetOut(previousOutput);
            activities.Dispose();
            meter.Dispose();
            output.Dispose();
            stdout.Dispose();
        }
    }
}

[Trait("Provider", "sqlite"), Trait("Suite", "privacy")]
public sealed class SqliteStorageMaintenanceDiagnosticsTests : StorageMaintenanceDiagnosticsTests
{ protected override string Provider => "sqlite"; }

[Trait("Provider", "postgres"), Trait("Suite", "privacy")]
public sealed class PostgresStorageMaintenanceDiagnosticsTests : StorageMaintenanceDiagnosticsTests
{ protected override string Provider => "postgres"; }
