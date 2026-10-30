using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text;
using System.Text.Json;
using Kofte.Host.Features.IdentityMaintenance;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Xunit;
using Diagnostics = Kofte.Infrastructure.Identity.IdentityDiagnostics;

namespace Kofte.Host.Tests.Features.Identity;

public sealed class IdentityDiagnosticsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DeliveredFileEffectCanBeTracedBeforeDatabaseCommitWithoutPrematureSuccess(bool committed)
    {
        var spans = new System.Collections.Concurrent.ConcurrentBag<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kofte.Identity",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = spans.Add
        };
        ActivitySource.AddActivityListener(listener);
        var records = new List<IdentityDiagnosticRecord>();
        using (var observation = Diagnostics.Begin(IdentityAction.BootstrapAuthorize, "oidc", "sqlite",
            TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken, records.Add))
        {
            observation.PossibleCommit();
            Diagnostics.Checkpoint("operator_before_commit");
            if (committed) observation.Committed();
            else observation.Fail(new IOException("test_commit_not_acknowledged"));
        }
        var terminal = Assert.Single(records);
        Assert.Equal(committed ? "identity_completed" : "identity_unavailable", terminal.EventCode);
        Assert.Equal(committed ? "committed" : "uncertain", terminal.Durability);
        var phase = Assert.Single(spans, span => span.TraceId.ToHexString() == terminal.CorrelationId &&
            span.OperationName == "identity.operator_before_commit");
        Assert.Equal("uncertain", phase.GetTagItem("identity.outcome"));
        Assert.Equal("uncertain", phase.GetTagItem("identity.durability"));
        Assert.Null(Diagnostics.Current);
    }

    [Fact]
    public void TerminalRecordsAreGeneratedBoundedPrivateAndDoNotReuseCallerTraceIdentity()
    {
        var spans = new System.Collections.Concurrent.ConcurrentBag<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kofte.Identity",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = spans.Add
        };
        ActivitySource.AddActivityListener(listener);
        using var caller = new Activity("test caller").SetIdFormat(ActivityIdFormat.W3C)
            .SetParentId("00-11111111111111111111111111111111-2222222222222222-01")
            .AddBaggage("private-canary", "private-canary").Start();
        var records = new List<IdentityDiagnosticRecord>();
        using (var observation = Diagnostics.Begin(IdentityAction.Callback, "oidc", "postgresql",
            TimeSpan.FromSeconds(4), TestContext.Current.CancellationToken, records.Add))
        {
            Assert.Throws<InvalidOperationException>(() => Diagnostics.Checkpoint("private-canary"));
            Diagnostics.Checkpoint("login_before_commit");
            observation.PossibleCommit();
            observation.Fail(new IOException("cookie=private-canary; path=/private-canary; subject=private-canary"));
        }
        var record = Assert.Single(records);
        Assert.Equal("identity_unavailable", record.EventCode);
        Assert.Equal("uncertain", record.Durability);
        Assert.Matches("^[0-9a-f]{32}$", record.CorrelationId);
        Assert.NotEqual(caller.TraceId.ToHexString(), record.CorrelationId);
        var bytes = Diagnostics.Serialize(record);
        Assert.InRange(bytes.Length, 1, 1024);
        Assert.DoesNotContain("private-canary", Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
        var owned = spans.Where(span => span.TraceId.ToHexString() == record.CorrelationId).ToArray();
        Assert.Equal(2, owned.Length);
        Assert.All(owned, span =>
        {
            Assert.Equal(record.CorrelationId, span.TraceId.ToHexString());
            Assert.Empty(span.Baggage);
            Assert.DoesNotContain("private-canary", JsonSerializer.Serialize(span.TagObjects), StringComparison.Ordinal);
        });
        Assert.Null(Diagnostics.Current);
    }

    [Fact]
    public async Task OperatorProjectionRequiresBoundedSameOperationContextAndAUniqueLastTerminal()
    {
        var records = new List<IdentityDiagnosticRecord>();
        using (var observation = Diagnostics.Begin(IdentityAction.Prepare, "development", "sqlite",
            TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken, records.Add))
        {
            observation.Warning("session");
            observation.Committed();
        }
        string Line(IdentityDiagnosticRecord record) => Encoding.UTF8.GetString(Diagnostics.Serialize(record)) + "\n";
        var text = string.Concat(records.Select(Line));
        using var valid = new StringReader(text);
        var projected = await IdentityMaintenanceSupervisor.ReadAsync(valid, "prepare", "sqlite",
            TestContext.Current.CancellationToken);
        Assert.Equal(records.ToArray(), projected.ToArray());
        foreach (var invalid in new[]
        {
            text + Line(records[^1]), text.TrimEnd('\n'), new string('x', 1025),
            Line(records[0]) + Line(records[^1] with { CorrelationId = Guid.NewGuid().ToString("N") }),
            string.Concat(Enumerable.Repeat(Line(records[0]), 9)),
            Line(records[^1] with { Operation = "revoke_sessions" }),
            Line(records[^1] with { Provider = "postgresql" }),
            Line(records[^1] with { Mode = "unknown" })
        })
        {
            using var reader = new StringReader(invalid);
            await Assert.ThrowsAsync<InvalidOperationException>(() => IdentityMaintenanceSupervisor.ReadAsync(
                reader, "prepare", "sqlite", TestContext.Current.CancellationToken));
        }
        var lost = IdentityMaintenanceSupervisor.Failure(records[^1], "prepare", "sqlite", uncertain: true);
        Assert.Equal("identity_outcome_uncertain", lost.EventCode);
        Assert.Equal("uncertain", lost.Durability);
        Assert.Equal(records[^1].CorrelationId, lost.CorrelationId);
    }

    [Fact]
    public void FailedTerminalSinkCannotReturnSuccessOrBeSilentlyRetried()
    {
        var attempts = 0;
        using var observation = Diagnostics.Begin(IdentityAction.Prepare, "development", "sqlite",
            TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken, _ =>
            {
                attempts++;
                throw new IOException("test_sink_failed");
            });
        Assert.Throws<InvalidOperationException>(() => observation.Warning("private-canary"));
        Assert.Equal(0, attempts);
        observation.Committed();
        Assert.Throws<IOException>(() => observation.Publish(IdentityDiagnosticOutcome.Ready));
        observation.Dispose();
        Assert.Equal(1, attempts);
        Assert.Null(Diagnostics.Current);
    }

    [Theory]
    [InlineData("sqlite")]
    [InlineData("postgresql")]
    public void ActualPhaseWarningAndMetricSinksStayBoundedAndNeverAcquireCallerIdentity(string provider)
    {
        var records = new List<IdentityDiagnosticRecord>();
        var spans = new List<Activity>();
        var measurements = new List<(long Value, KeyValuePair<string, object?>[] Tags)>();
        var canary = "private-" + Guid.NewGuid().ToString("N");
        string? correlation = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kofte.Identity",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.TraceId.ToHexString() == correlation) spans.Add(activity);
            }
        };
        ActivitySource.AddActivityListener(listener);
        using var meter = new MeterListener();
        meter.InstrumentPublished = (instrument, owner) =>
        {
            if (instrument.Meter.Name == "Kofte.Identity" &&
                instrument.Name == "kofte.identity.operations") owner.EnableMeasurementEvents(instrument);
        };
        meter.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            if (Diagnostics.Current?.CorrelationId == correlation) measurements.Add((value, tags.ToArray()));
        });
        meter.Start();
        using var caller = new Activity("private caller").AddBaggage("subject", canary)
            .SetTag("cookie", canary).Start();
        using (var observation = Diagnostics.Begin(IdentityAction.Callback, "oidc", provider,
            TimeSpan.FromSeconds(4), TestContext.Current.CancellationToken, records.Add))
        {
            correlation = observation.CorrelationId;
            for (var index = 0; index < 32; index++) Diagnostics.Checkpoint("login_before_commit");
            Assert.Throws<InvalidOperationException>(() => Diagnostics.Checkpoint("login_before_commit"));
            for (var index = 0; index < 8; index++) observation.Warning("session");
            Assert.Throws<InvalidOperationException>(() => observation.Warning("session"));
            observation.PossibleCommit();
            observation.Fail(new IOException(canary));
        }
        Assert.Equal(9, records.Count);
        Assert.All(records, record =>
        {
            var bytes = Diagnostics.Serialize(record);
            Assert.InRange(bytes.Length, 1, 1024);
            Assert.DoesNotContain(canary, Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
            Assert.Equal(correlation, record.CorrelationId);
        });
        Assert.Equal(("identity_unavailable", "uncertain", "retry_explicitly"),
            (records[^1].EventCode, records[^1].Durability, records[^1].Guidance));
        Assert.Equal(33, spans.Count);
        Assert.All(spans, span =>
        {
            Assert.Empty(span.Baggage);
            Assert.DoesNotContain(canary, JsonSerializer.Serialize(span.TagObjects), StringComparison.Ordinal);
        });
        var measurement = Assert.Single(measurements);
        Assert.Equal(1, measurement.Value);
        Assert.Equal(new[]
        {
            new KeyValuePair<string, object?>("operation", "callback"),
            new("mode", "oidc"), new("provider", provider), new("outcome", "unavailable")
        }, measurement.Tags);
        Assert.DoesNotContain(canary, JsonSerializer.Serialize(measurement.Tags), StringComparison.Ordinal);
        Assert.Null(Diagnostics.Current);
    }

    [Fact]
    public void ActualRejectedTerminalSinkEmitsNoSuccessfulTraceOrCounterAndRestoresItsParent()
    {
        var spans = new List<Activity>();
        var measurements = 0;
        var records = new List<IdentityDiagnosticRecord>();
        using var parent = Diagnostics.Begin(IdentityAction.Current, "oidc", "sqlite",
            TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken, records.Add);
        string? correlation = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "Kofte.Identity",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.TraceId.ToHexString() == correlation) spans.Add(activity);
            }
        };
        ActivitySource.AddActivityListener(listener);
        using var meter = new MeterListener();
        meter.InstrumentPublished = (instrument, owner) =>
        {
            if (instrument.Meter.Name == "Kofte.Identity") owner.EnableMeasurementEvents(instrument);
        };
        meter.SetMeasurementEventCallback<long>((_, _, _, _) =>
        {
            if (Diagnostics.Current?.CorrelationId == correlation) measurements++;
        });
        meter.Start();
        var writes = 0;
        using var observation = Diagnostics.Begin(IdentityAction.Enrollment, "oidc", "sqlite",
            TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken, _ =>
            {
                writes++;
                throw new IOException("private-terminal-sink-failed");
            });
        correlation = observation.CorrelationId;
        observation.Committed();
        Assert.Throws<IOException>(() => observation.Publish(IdentityDiagnosticOutcome.Ready));
        observation.Dispose();
        Assert.Equal(1, writes);
        Assert.Empty(spans);
        Assert.Equal(0, measurements);
        Assert.Same(parent, Diagnostics.Current);
    }

    [Fact]
    public void ReprojectionRejectsUnknownFieldsLabelsCorrelationsAndOversizedRecords()
    {
        var records = new List<IdentityDiagnosticRecord>();
        using (var observation = Diagnostics.Begin(IdentityAction.Current, "oidc", "sqlite",
            TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken, records.Add))
            observation.Publish(IdentityDiagnosticOutcome.Unauthenticated);
        var record = Assert.Single(records);
        var bytes = Diagnostics.Serialize(record);
        using var json = JsonDocument.Parse(bytes);
        Assert.Equal("identity_unauthenticated", json.RootElement.GetProperty("eventCode").GetString());
        Assert.Equal(record, Diagnostics.Parse(bytes));
        foreach (var invalid in new[]
        {
            record with { CorrelationId = "caller-selected" },
            record with { Provider = "private-canary" },
            record with { Mode = "private-canary" },
            record with { Guidance = "private-canary" },
            record with { EventCode = "private-canary" },
            record with { Operation = "private-canary" },
            record with { EventCode = "identity_completed", Outcome = "ready", Guidance = "none", Durability = "uncertain" }
        })
            Assert.Throws<InvalidOperationException>(() => Diagnostics.Serialize(invalid));
        var text = Encoding.UTF8.GetString(bytes);
        Assert.Throws<InvalidOperationException>(() =>
            Diagnostics.Parse(Encoding.UTF8.GetBytes(text[..^1] + ",\"private\":\"canary\"}")));
        Assert.Throws<InvalidOperationException>(() => Diagnostics.Parse(new byte[1025]));
    }
}
