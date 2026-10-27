using System.Diagnostics;
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
