using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Modules.Alpha;
using Kofte.Storage.IntegrationTests.Providers;
using Kofte.Storage.IntegrationTests.Recovery;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Kofte.Storage.IntegrationTests.Providers;

[CollectionDefinition("Storage telemetry isolation", DisableParallelization = true)]
public sealed class StorageTelemetryCollection;

[Collection("Storage telemetry isolation")]
public abstract class StoragePrivacyTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task AllSevenResultsConsoleSpansEventsMetricsAndWorkerOutputsExcludeSeededSecrets()
    {
        var provider = Provider;
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await RegistryFixture.CreateAsync(provider, TestContext.Current.CancellationToken);
        var spans = new ConcurrentQueue<(string Trace, string Tags)>();
        var measurements = new ConcurrentQueue<(string Key, string? Value)[]>();
        using var activities = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kofte.Storage",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => spans.Enqueue((activity.TraceId.ToHexString(),
                JsonSerializer.Serialize(new
                {
                    tags = activity.TagObjects.ToDictionary(pair => pair.Key, pair => pair.Value),
                    events = activity.Events.Select(item => new { item.Name, tags = item.Tags.ToArray() })
                })))
        };
        ActivitySource.AddActivityListener(activities);
        using var meter = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "Kofte.Storage") listener.EnableMeasurementEvents(instrument);
            }
        };
        meter.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            var values = new (string Key, string? Value)[tags.Length];
            for (var i = 0; i < tags.Length; i++) values[i] = (tags[i].Key, tags[i].Value?.ToString());
            measurements.Enqueue(values);
        });
        meter.Start();
        using var stderr = new StringWriter();
        using var stdout = new StringWriter();
        var previousError = Console.Error;
        var previousOutput = Console.Out;
        var results = new List<StorageResult>();
        const string canary = "credential-canary-payload";
        var workerOutput = "";
        try
        {
            Console.SetError(stderr);
            Console.SetOut(stdout);
            var owner = RegistryFixture.Module("org.example.telemetry", display: "Köfte " + canary);
            await using (var scope = await fixture.Maintenance.AcquireAsync(token))
            {
                results.Add(await fixture.Registry.RegisterAsync(owner, scope, token));
                results.Add(await fixture.Registry.ResolveAsync(owner.Id, TestContext.Current.CancellationToken));
                results.Add(await fixture.Registry.RegisterAsync(RegistryFixture.Module(owner.Id.Value, relational: false),
                    scope, TestContext.Current.CancellationToken));
                var invalid = new StorageRegistry(fixture.Installation, _ => "/private/" + canary);
                results.Add(await invalid.RegisterAsync(RegistryFixture.Module("org.example.boundary"),
                    scope, TestContext.Current.CancellationToken));
                using var cancelled = new CancellationTokenSource();
                cancelled.Cancel();
                results.Add(await fixture.Registry.RegisterAsync(RegistryFixture.Module("org.example.cancelled"),
                    scope, cancelled.Token));
                results.Add(await fixture.Registry.RegisterAsync(
                    RegistryFixture.Module("org.example.telemetry-stateless", relational: false), scope, token));
                await using var binding = await fixture.Registry.BindAsync(owner.Id, token);
                results.Add(await binding.MigrateAsync(ModuleFixture.AlphaFactory, scope, cancellationToken: token));
                await using var context = await binding.CreateContextAsync(ModuleFixture.AlphaFactory, token);
                context.Records.Add(new AlphaRecord { Id = 1, Payload = canary + "\n\tprivate-control" });
                await context.SaveChangesAsync(token);
                var unexpected = await Assert.ThrowsAsync<StorageException>(() =>
                    binding.CreateContextAsync<AlphaContext>((_, _) =>
                        throw new InvalidOperationException("/private/" + canary), token));
                Assert.Equal(StorageCode.Unavailable, unexpected.Code);
            }
            await using (var blocker = await StorageReadBlock.CreateAsync(provider, fixture.Root, fixture.RuntimeConnection, token))
            {
                var busy = await new StorageRegistry(blocker.Reader).ResolveAsync(owner.Id, token);
                Assert.Equal("storage_busy", busy.Diagnostics[0].Code);
                results.Add(busy);
            }
            var interrupted = RegistryFixture.Module("org.example.telemetry-interrupted");
            const string phase = "reservation_committed";
            var output = await StorageCrashWorker.KillAtAsync(StorageCrashWorker.Start(Assembly.GetExecutingAssembly(),
                provider, "register", phase, interrupted.Id.Value,
                child => ProvisioningCrashTests.Configure(fixture, child)), phase, token);
            workerOutput = output.Stdout + output.Stderr;
            results.Add(await fixture.Registry.ResolveAsync(interrupted.Id, token));
            var missing = Assert.IsType<StorageResult.Ready>(results[0]).Assignment;
            if (fixture.Root is { } dataRoot)
            {
                var path = Path.Combine(dataRoot, "modules", missing.AllocationKey, "module.db");
                File.Move(path, path + ".held");
                try { results.Add(await fixture.Registry.ResolveAsync(owner.Id, token)); }
                finally { File.Move(path + ".held", path); }
            }
            else
            {
                await using var connection = new NpgsqlConnection(fixture.ProvisioningConnection);
                await connection.OpenAsync(token);
                await using var command = new NpgsqlCommand(
                    $"ALTER SCHEMA \"{missing.AllocationKey}\" RENAME TO \"{missing.AllocationKey}_held\"", connection);
                await command.ExecuteNonQueryAsync(token);
                try { results.Add(await fixture.Registry.ResolveAsync(owner.Id, token)); }
                finally
                {
                    command.CommandText = $"ALTER SCHEMA \"{missing.AllocationKey}_held\" RENAME TO \"{missing.AllocationKey}\"";
                    await command.ExecuteNonQueryAsync(token);
                }
            }
            var retried = await fixture.Registry.ResolveAsync(owner.Id, token);
            Assert.IsType<StorageResult.Ready>(retried);
            Assert.NotEqual(results[^1].CorrelationId, retried.CorrelationId);
            results.Add(retried);
            await using var retained = await fixture.Registry.BindAsync(owner.Id, token);
            await using var retainedContext = await retained.CreateContextAsync(ModuleFixture.AlphaFactory, token);
            Assert.Equal(canary + "\n\tprivate-control", (await retainedContext.Records.SingleAsync(token)).Payload);
        }
        finally { Console.SetError(previousError); Console.SetOut(previousOutput); }
        Assert.Equal(Enum.GetValues<StorageOutcome>().Order(), results.Select(result => result.Outcome).Distinct().Order());
        var logCorrelations = new HashSet<string>();
        foreach (var line in stderr.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            using var record = JsonDocument.Parse(line);
            var value = record.RootElement;
            Assert.Equal(7, value.EnumerateObject().Count());
            Assert.Equal("storage", value.GetProperty("component").GetString());
            Assert.Matches(@"\A[0-9a-f]{32}\z", value.GetProperty("correlationId").GetString()!);
            Assert.True(logCorrelations.Add(value.GetProperty("correlationId").GetString()!));
        }
        Assert.True(logCorrelations.Count >= results.Count);
        Assert.Equal(logCorrelations.Count, spans.Count);
        Assert.Equal(logCorrelations.Count, measurements.Count);
        foreach (var result in results)
        {
            Assert.Contains(result.CorrelationId, logCorrelations);
            Assert.Contains(spans, span => span.Trace == result.CorrelationId);
            Assert.True(JsonSerializer.SerializeToUtf8Bytes(result, result.GetType()).Length <= 4096);
            StorageResultChecks.Check(result);
        }
        foreach (var tags in measurements)
            Assert.Equal(["operation", "outcome", "provider"], tags.Select(tag => tag.Key).Order(StringComparer.Ordinal));
        Assert.Empty(stdout.ToString());
        var channels = stderr + stdout.ToString() + workerOutput + JsonSerializer.Serialize(spans) +
            JsonSerializer.Serialize(measurements) +
            string.Join('\n', results.Select(result => JsonSerializer.Serialize(result, result.GetType())));
        var forbidden = new List<string> { canary, "/private/", "CREATE TABLE", "SELECT ", "Exception", "StackTrace" };
        if (fixture.Root is { } root) forbidden.Add(root);
        if (fixture.RuntimeConnection is { } runtime)
        {
            forbidden.Add(runtime);
            var password = new NpgsqlConnectionStringBuilder(runtime).Password;
            if (!string.IsNullOrEmpty(password)) forbidden.Add(password);
        }
        foreach (var text in forbidden)
            Assert.True(!channels.Contains(text, StringComparison.Ordinal), "storage_telemetry_contains_sensitive_input");
    }
}

[Trait("Provider", "sqlite"), Trait("Suite", "privacy")]
public sealed class SqliteStoragePrivacyTests : StoragePrivacyTests
{ protected override string Provider => "sqlite"; }
[Trait("Provider", "postgres"), Trait("Suite", "privacy")]
public sealed class PostgresStoragePrivacyTests : StoragePrivacyTests
{ protected override string Provider => "postgres"; }
