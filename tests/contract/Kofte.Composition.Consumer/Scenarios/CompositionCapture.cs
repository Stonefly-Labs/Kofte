using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using Kofte.Host.Features.ModuleComposition;

namespace Kofte.Composition.Consumer.Scenarios;

internal sealed record CapturedSpan(string Trace, string Name, string? TraceState,
    KeyValuePair<string, string?>[] Baggage, KeyValuePair<string, object?>[] Tags,
    KeyValuePair<string, object?>[][] Events);

internal sealed class CompositionCapture : IDisposable
{
    private readonly TextWriter previousOutput = Console.Out, previousError = Console.Error;
    private readonly StringWriter output = new(), error = new();
    private readonly ActivityListener activities;
    private readonly MeterListener meter = new();
    private readonly ConcurrentQueue<CapturedSpan> spans = new();
    private readonly ConcurrentQueue<KeyValuePair<string, object?>[]> metrics = new();
    private bool disposed;

    internal string Stdout => output.ToString();
    internal string Stderr => error.ToString();
    internal IReadOnlyList<CapturedSpan> Spans => spans.ToArray();
    internal IReadOnlyList<KeyValuePair<string, object?>[]> Metrics => metrics.ToArray();

    internal CompositionCapture()
    {
        activities = new()
        {
            ShouldListenTo = source => source.Name == CompositionDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity =>
            {
                if (spans.Count >= 2048) throw new ScenarioFailure("composition_test_capture_limit");
                spans.Enqueue(new(activity.TraceId.ToHexString(), activity.OperationName, activity.TraceStateString,
                    activity.Baggage.ToArray(), activity.TagObjects.ToArray(),
                    activity.Events.Select(item => item.Tags.ToArray()).ToArray()));
            }
        };
        ActivitySource.AddActivityListener(activities);
        meter.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == CompositionDiagnostics.SourceName) listener.EnableMeasurementEvents(instrument);
        };
        meter.SetMeasurementEventCallback<long>((_, _, tags, _) => Measure(tags));
        meter.SetMeasurementEventCallback<double>((_, _, tags, _) => Measure(tags));
        meter.Start();
        Console.SetOut(output);
        Console.SetError(error);
    }

    private void Measure(ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        if (metrics.Count >= 4096) throw new ScenarioFailure("composition_test_capture_limit");
        metrics.Enqueue(tags.ToArray());
    }

    internal static void Write(CompositionRecord record) =>
        Console.WriteLine(JsonSerializer.Serialize(record, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));

    public void Dispose()
    {
        if (disposed) return;
        Console.SetOut(previousOutput);
        Console.SetError(previousError);
        activities.Dispose();
        meter.Dispose();
        output.Dispose();
        error.Dispose();
        disposed = true;
    }
}
