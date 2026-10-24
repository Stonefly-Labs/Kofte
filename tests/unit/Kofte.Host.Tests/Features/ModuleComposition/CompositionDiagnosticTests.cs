using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kofte.Host.Features.ModuleComposition;
using Kofte.Platform.Modules.Lifecycle;
using Kofte.Platform.Modules.Manifest;
using Xunit;

namespace Kofte.Host.Tests.Features.ModuleComposition;

[CollectionDefinition("Composition diagnostics", DisableParallelization = true)]
public sealed class CompositionDiagnosticsCollection { }

[Collection("Composition diagnostics")]
public sealed class CompositionDiagnosticTests
{
    [Fact]
    public async Task MissingPrerequisiteIsActionableButNeverAdmittedAsARuntimeOwnerOrHealthDependency()
    {
        var diagnostics = new CompositionDiagnostics(CompositionMode.Validate, "none", _ => { });
        var owner = Id("org.example.alpha");
        var missing = Id("org.example.beta");
        var prerequisites = new[] { missing };
        var protocol = new CompositionProtocol(diagnostics.Attempt, new string('b', 32), [owner],
            CompositionMode.Validate, prerequisiteOwners: prerequisites);
        prerequisites[0] = Id("org.example.other");
        using var stream = new MemoryStream();
        await protocol.WriteAsync(stream, diagnostics.Emit(CompositionStage.Validated,
            CompositionOutcome.Rejected, "composition_dependency_missing", owner, [missing]),
            TestContext.Current.CancellationToken);
        stream.Position = 0;
        Assert.Equal(missing.Value, Assert.Single(
            (await protocol.ReadAsync(stream, TestContext.Current.CancellationToken))!.RelatedIds));
        foreach (var record in new[]
        {
            diagnostics.Emit(CompositionStage.Started, CompositionOutcome.Passed, "composition_progress", missing),
            diagnostics.Emit(CompositionStage.Health, CompositionOutcome.Failed, "composition_health_failed", owner, [missing]),
            diagnostics.Emit(CompositionStage.Validated, CompositionOutcome.Rejected, "composition_dependency_missing", owner, prerequisites)
        })
            await Assert.ThrowsAsync<CompositionBoundaryException>(() =>
                protocol.WriteAsync(new MemoryStream(), record, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void EachSuccessfulRequiredWriteProducesOneSafeTransitionEvent()
    {
        var spans = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CompositionDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = spans.Add
        };
        ActivitySource.AddActivityListener(listener);
        var diagnostics = new CompositionDiagnostics(CompositionMode.Validate, "none", _ => { });
        var record = diagnostics.Emit(CompositionStage.Input, CompositionOutcome.Rejected,
            "composition_configuration_invalid", isTerminal: true);
        var transition = Assert.Single(Assert.Single(spans).Events);
        Assert.Equal("composition.transition", transition.Name);
        Assert.Equal(record.Code, transition.Tags.Single(tag => tag.Key == "composition.code").Value);
        Assert.Equal(record.Guidance, transition.Tags.Single(tag => tag.Key == "composition.guidance").Value);
        Assert.Equal(record.Outcome, transition.Tags.Single(tag => tag.Key == "composition.outcome").Value);
        Assert.Equal(3, transition.Tags.Count());
    }

    [Theory]
    [InlineData("composition_implementation_missing")]
    [InlineData("composition_configuration_invalid")]
    public async Task SelectedMissingIdentityIsPreservedOnlyForItsRejectedDeploymentBoundary(string code)
    {
        var diagnostics = new CompositionDiagnostics(CompositionMode.Validate, "none", _ => { });
        var missing = Id("org.example.missing");
        var selected = new[] { missing };
        var protocol = new CompositionProtocol(diagnostics.Attempt, new string('b', 32), [],
            CompositionMode.Validate, selectedOwners: selected);
        selected[0] = Id("org.example.other");
        using var stream = new MemoryStream();
        await protocol.WriteAsync(stream, diagnostics.Emit(CompositionStage.Validated,
            CompositionOutcome.Rejected, code, missing), TestContext.Current.CancellationToken);
        stream.Position = 0;
        Assert.Equal(missing.Value, (await protocol.ReadAsync(stream, TestContext.Current.CancellationToken))!.Owner);
        foreach (var record in new[]
        {
            diagnostics.Emit(CompositionStage.Started, CompositionOutcome.Passed, "composition_progress", missing),
            diagnostics.Emit(CompositionStage.Validated, CompositionOutcome.Rejected, code, selected[0]),
            diagnostics.Emit(CompositionStage.Validated, CompositionOutcome.Rejected, "composition_manifest_rejected", missing)
        })
            await Assert.ThrowsAsync<CompositionBoundaryException>(() =>
                protocol.WriteAsync(new MemoryStream(), record, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PrivateFramesRoundTripOnlyCurrentAttemptSnapshotModeAndKnownOwners()
    {
        var records = new List<CompositionRecord>();
        var diagnostics = new CompositionDiagnostics(CompositionMode.Validate, "none", records.Add);
        var owner = Id("org.example.alpha");
        var related = new[] { Id("org.example.beta") };
        var protocol = new CompositionProtocol(diagnostics.Attempt, new string('b', 32), [owner, .. related],
            CompositionMode.Validate);
        var record = diagnostics.Emit(CompositionStage.Validated, CompositionOutcome.Passed,
            "composition_ready", owner, related);
        related[0] = Id("org.example.other");
        using var pipe = new MemoryStream();
        await protocol.WriteAsync(pipe, record, TestContext.Current.CancellationToken);
        pipe.Position = 0;
        var projected = await protocol.ReadAsync(pipe, TestContext.Current.CancellationToken);
        Assert.NotNull(projected);
        Assert.Equal("org.example.beta", Assert.Single(projected.RelatedIds));
        Assert.Equal(diagnostics.Attempt, projected.Attempt);
        Assert.Equal(owner.Value, projected.Owner);
        Assert.Null(await protocol.ReadAsync(pipe, TestContext.Current.CancellationToken));
        Assert.DoesNotContain("alpha", projected.ToString());
    }

    [Fact]
    public async Task RawCallbackOutputIsDrainedWithoutBecomingAProjectedRecordOrUnboundedLine()
    {
        const string canary = "private-settings-path-exception-canary";
        var records = new List<CompositionRecord>();
        var diagnostics = new CompositionDiagnostics(CompositionMode.Validate, "unknown", records.Add);
        using var raw = new StringReader(new string('x', 100_000) + canary);
        await CompositionProtocol.DiscardRawAsync(raw, TestContext.Current.CancellationToken);
        diagnostics.Emit(CompositionStage.Input, CompositionOutcome.Rejected, "composition_configuration_invalid",
            isTerminal: true);
        Assert.DoesNotContain(canary, JsonSerializer.Serialize(Assert.Single(records)));
    }

    [Theory]
    [InlineData("version", "2")]
    [InlineData("attempt", "\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"")]
    [InlineData("mode", "\"prepare\"")]
    [InlineData("stage", "\"private-canary\"")]
    [InlineData("outcome", "\"private-canary\"")]
    [InlineData("owner", "\"org.private.canary\"")]
    [InlineData("relatedIds", "[\"org.private.canary\"]")]
    [InlineData("code", "\"private-canary\"")]
    [InlineData("guidance", "\"private-canary\"")]
    [InlineData("provider", "\"private-canary\"")]
    [InlineData("elapsedMilliseconds", "-1")]
    [InlineData("storageCorrelation", "\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\"")]
    [InlineData("durability", "\"committed\"")]
    [InlineData("private-canary", "\"private-canary\"")]
    public async Task MalformedOrForgedRecordFieldsRejectWithoutEchoingInput(string field, string value)
    {
        var (protocol, frame) = await ValidFrame();
        frame["record"]![field] = JsonNode.Parse(value);
        var error = await Assert.ThrowsAsync<CompositionBoundaryException>(() =>
            protocol.ReadAsync(Encode(frame.ToJsonString()), TestContext.Current.CancellationToken));
        Assert.Equal("composition_observation_invalid", error.Code);
        Assert.DoesNotContain("private", error.Message);
    }

    [Fact]
    public async Task BadFrameVersionSnapshotDuplicateFieldsMissingFieldsAndOversizeReject()
    {
        var (protocol, frame) = await ValidFrame();
        foreach (var field in new[] { "version", "snapshot", "record" })
        {
            var missing = frame.DeepClone();
            missing.AsObject().Remove(field);
            await Refused(Encode(missing.ToJsonString()));
        }

        var wrongVersion = frame.DeepClone();
        wrongVersion["version"] = 2;
        await Refused(Encode(wrongVersion.ToJsonString()));
        var wrongSnapshot = frame.DeepClone();
        wrongSnapshot["snapshot"] = new string('c', 32);
        await Refused(Encode(wrongSnapshot.ToJsonString()));
        await Refused(Encode(frame.ToJsonString().Replace("\"version\":1", "\"version\":1,\"version\":1", StringComparison.Ordinal)));
        var header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, 4097);
        await Refused(new MemoryStream(header));
        BinaryPrimitives.WriteInt32LittleEndian(header, 0);
        await Refused(new MemoryStream(header));
        await Refused(new MemoryStream(new byte[] { 1, 2 }));
        await Refused(Encode("{}"));

        async Task Refused(Stream source)
        {
            using (source)
                await Assert.ThrowsAsync<CompositionBoundaryException>(() =>
                    protocol.ReadAsync(source, TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task Exactly4096Utf8BytesAreAcceptedBut4097AreRefused()
    {
        var (protocol, frame) = await ValidFrame();
        var json = frame.ToJsonString();
        var padding = new string(' ', 4096 - System.Text.Encoding.UTF8.GetByteCount(json));
        using var exact = Encode(json + padding);
        Assert.NotNull(await protocol.ReadAsync(exact, TestContext.Current.CancellationToken));
        using var oversized = Encode(json + padding + " ");
        Assert.Equal("composition_observation_invalid", (await Assert.ThrowsAsync<CompositionBoundaryException>(() =>
            protocol.ReadAsync(oversized, TestContext.Current.CancellationToken))).Code);
    }

    [Theory]
    [InlineData("fieldToken", "\"private-unknown-key\"")]
    [InlineData("fieldToken", "\"other\"")]
    [InlineData("fieldToken", "null")]
    [InlineData("fieldReason", "\"private-value\"")]
    [InlineData("fieldReason", "null")]
    [InlineData("owner", "null")]
    [InlineData("owner", "\"org.example.beta\"")]
    [InlineData("stage", "\"initialized\"")]
    [InlineData("outcome", "\"passed\"")]
    public async Task SettingsFramesRequirePairedKnownOwnerFieldAndFixedReason(string field, string value)
    {
        var (protocol, frame) = await ValidFrame(settings: true);
        frame["record"]![field] = JsonNode.Parse(value);
        using var source = Encode(frame.ToJsonString());
        var failure = await Assert.ThrowsAsync<CompositionBoundaryException>(() =>
            protocol.ReadAsync(source, TestContext.Current.CancellationToken));
        Assert.Equal("composition_observation_invalid", failure.Code);
        Assert.DoesNotContain("private", failure.Message);
    }

    [Theory]
    [InlineData("required")]
    [InlineData("type")]
    [InlineData("unknown")]
    [InlineData("constraint")]
    public async Task KnownSettingsGuidanceRoundTripsWithOnlyTheFourSafeReasons(string reason)
    {
        var (protocol, frame) = await ValidFrame(settings: true);
        frame["record"]!["fieldReason"] = reason;
        using var source = Encode(frame.ToJsonString());
        var record = await protocol.ReadAsync(source, TestContext.Current.CancellationToken);
        Assert.NotNull(record);
        Assert.Equal("server", record.FieldToken);
        Assert.Equal(reason, record.FieldReason);
    }

    [Fact]
    public void AttemptLimitRetainsExactlyOneTerminalRecordAndSteadyStateHasItsOwnRateWindow()
    {
        var records = new List<CompositionRecord>();
        var clock = new ManualClock();
        var diagnostics = new CompositionDiagnostics(CompositionMode.Prepare, "sqlite", records.Add, clock);
        for (var index = 0; index < 256; index++)
            diagnostics.Emit(CompositionStage.Discovered, CompositionOutcome.Progress, "composition_progress");
        Assert.Equal("composition_output_limit", Assert.Throws<CompositionBoundaryException>(() =>
            diagnostics.Emit(CompositionStage.Discovered, CompositionOutcome.Progress, "composition_progress")).Code);
        diagnostics.Emit(CompositionStage.Cleanup, CompositionOutcome.Failed, "composition_output_limit", isTerminal: true);
        Assert.Equal(257, records.Count);
        Assert.Throws<CompositionBoundaryException>(() =>
            diagnostics.Emit(CompositionStage.Cleanup, CompositionOutcome.Failed, "composition_output_limit", isTerminal: true));

        diagnostics = new(CompositionMode.Serve, "none", records.Add, clock);
        diagnostics.BeginSteadyState();
        for (var index = 0; index < 64; index++)
            diagnostics.Emit(CompositionStage.Health, CompositionOutcome.Passed, "composition_ready");
        Assert.Throws<CompositionBoundaryException>(() =>
            diagnostics.Emit(CompositionStage.Health, CompositionOutcome.Passed, "composition_ready"));
        clock.Advance(TimeSpan.FromMinutes(1));
        diagnostics.Emit(CompositionStage.Health, CompositionOutcome.Passed, "composition_ready");
    }

    [Fact]
    public void RequiredSinkFailureCannotBecomeSuccessfulInvisibleWork()
    {
        var diagnostics = new CompositionDiagnostics(CompositionMode.Validate, "unknown",
            _ => throw new IOException("private-canary"));
        var error = Assert.Throws<CompositionBoundaryException>(() =>
            diagnostics.Emit(CompositionStage.Input, CompositionOutcome.Rejected, "composition_configuration_invalid",
                isTerminal: true));
        Assert.Equal("composition_output_failed", error.Code);
        Assert.DoesNotContain("private", error.Message);
    }

    [Fact]
    public void FailedIdentitylessBoundaryProducesUsefulCurrentTraceAndBoundedMetricDimensions()
    {
        var spans = new List<Activity>();
        var metricTags = new List<KeyValuePair<string, object?>[]>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == CompositionDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = spans.Add
        };
        ActivitySource.AddActivityListener(listener);
        using var meter = new MeterListener();
        meter.InstrumentPublished = (instrument, target) =>
        {
            if (instrument.Meter.Name == CompositionDiagnostics.SourceName) target.EnableMeasurementEvents(instrument);
        };
        meter.SetMeasurementEventCallback<long>((_, _, tags, _) => metricTags.Add(tags.ToArray()));
        meter.SetMeasurementEventCallback<double>((_, _, tags, _) => metricTags.Add(tags.ToArray()));
        meter.Start();
        var records = new List<CompositionRecord>();
        var diagnostics = new CompositionDiagnostics(CompositionMode.Validate, "unknown", records.Add);
        diagnostics.Emit(CompositionStage.Admission, CompositionOutcome.Rejected, "composition_admission_invalid",
            isTerminal: true);
        var record = Assert.Single(records);
        Assert.Null(record.Owner);
        Assert.Equal("use_supervised_entry", record.Guidance);
        var span = Assert.Single(spans);
        Assert.Equal(diagnostics.Attempt, span.TraceId.ToHexString());
        Assert.Equal("composition_admission_invalid", span.GetTagItem("composition.code"));
        Assert.Equal(2, metricTags.Count);
        diagnostics = new(CompositionMode.Validate, "unknown", _ => { });
        diagnostics.Emit(CompositionStage.Validated, CompositionOutcome.Rejected, "composition_setting_invalid",
            Id("org.example.alpha"), fieldToken: "server", fieldReason: "required");
        Assert.Equal(4, metricTags.Count);
        foreach (var tags in metricTags)
        {
            Assert.Equal(new[] { "mode", "outcome", "provider", "stage" },
                tags.Select(tag => tag.Key).Order(StringComparer.Ordinal));
            Assert.DoesNotContain(tags, tag => tag.Value?.ToString() == diagnostics.Attempt);
        }
    }

    private static ModuleId Id(string value)
    {
        Assert.True(ModuleId.TryParse(value, out var id));
        return id;
    }

    private static MemoryStream Encode(string json)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var stream = new MemoryStream();
        var header = new byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(header, bytes.Length);
        stream.Write(header);
        stream.Write(bytes);
        stream.Position = 0;
        return stream;
    }

    private static async Task<(CompositionProtocol Protocol, JsonNode Frame)> ValidFrame(bool settings = false)
    {
        var diagnostics = new CompositionDiagnostics(CompositionMode.Validate, "none", _ => { });
        var owner = Id("org.example.alpha");
        var other = Id("org.example.beta");
        var protocol = new CompositionProtocol(diagnostics.Attempt, new string('b', 32),
            [owner, other], CompositionMode.Validate,
            new Dictionary<ModuleId, IReadOnlyList<ModuleConfigurationField>>
            {
                [owner] = [new("server", ["server"], ModuleSettingType.Object)],
                [other] = [new("other", ["other"], ModuleSettingType.String)]
            });
        using var pipe = new MemoryStream();
        var record = settings
            ? diagnostics.Emit(CompositionStage.Validated, CompositionOutcome.Rejected,
                "composition_setting_invalid", owner, fieldToken: "server", fieldReason: "required")
            : diagnostics.Emit(CompositionStage.Input, CompositionOutcome.Progress, "composition_progress");
        await protocol.WriteAsync(pipe, record, TestContext.Current.CancellationToken);
        return (protocol, JsonNode.Parse(pipe.ToArray().AsSpan(4))!);
    }

    private sealed class ManualClock : TimeProvider
    {
        private long now;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => now;
        public void Advance(TimeSpan elapsed) => now += elapsed.Ticks;
    }
}
