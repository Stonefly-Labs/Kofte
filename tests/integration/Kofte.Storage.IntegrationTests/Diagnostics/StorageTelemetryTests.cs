using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Registry;
using Npgsql;

namespace Kofte.Storage.IntegrationTests.Diagnostics;

[CollectionDefinition("Storage telemetry isolation", DisableParallelization = true)]
public sealed class StorageTelemetryCollection;

[Collection("Storage telemetry isolation")]
public sealed class StorageTelemetryTests
{
    [Theory]
    [InlineData("sqlite")]
    [InlineData("postgres")]
    public async Task ResultsStderrSpansAndMetricLabelsExcludeTrustedInputAndProviderCredentials(string provider)
    {
        await using var fixture = await RegistryFixture.CreateAsync(provider, TestContext.Current.CancellationToken);
        var spans = new ConcurrentQueue<(string Trace, string Tags)>();
        var measurements = new ConcurrentQueue<(string Key, string? Value)[]>();
        using var activities = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kofte.Storage",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => spans.Enqueue((activity.TraceId.ToHexString(),
                JsonSerializer.Serialize(activity.TagObjects.ToDictionary(pair => pair.Key, pair => pair.Value))))
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
        var previousError = Console.Error;
        var results = new List<StorageResult>();
        const string canary = "credential-canary-payload";
        try
        {
            Console.SetError(stderr);
            await using var scope = await fixture.Maintenance.AcquireAsync(TestContext.Current.CancellationToken);
            var owner = RegistryFixture.Module("org.example.telemetry", display: "Köfte " + canary);
            results.Add(await fixture.Registry.RegisterAsync(owner, scope, TestContext.Current.CancellationToken));
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
        }
        finally { Console.SetError(previousError); }
        Assert.Collection(results,
            result => Assert.IsType<StorageResult.Ready>(result),
            result => Assert.IsType<StorageResult.Ready>(result),
            result => Assert.Equal(StorageOutcome.Rejected, result.Outcome),
            result => Assert.Equal(StorageOutcome.Rejected, result.Outcome),
            result => Assert.Equal("storage_cancelled", Assert.Single(result.Diagnostics).Code));
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
        Assert.Equal(5, logCorrelations.Count);
        Assert.Equal(5, spans.Count);
        Assert.Equal(5, measurements.Count);
        foreach (var result in results)
        {
            Assert.Contains(result.CorrelationId, logCorrelations);
            Assert.Contains(spans, span => span.Trace == result.CorrelationId);
            Assert.True(JsonSerializer.SerializeToUtf8Bytes(result, result.GetType()).Length <= 4096);
        }
        foreach (var tags in measurements)
            Assert.Equal(["operation", "outcome", "provider"], tags.Select(tag => tag.Key).Order(StringComparer.Ordinal));
        var channels = stderr + JsonSerializer.Serialize(spans) +
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
