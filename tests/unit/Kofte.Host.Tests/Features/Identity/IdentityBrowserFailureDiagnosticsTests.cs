using Kofte.TestSupport;
using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Host.Tests.Features.Identity;

public sealed class IdentityBrowserFailureDiagnosticsTests
{
    [Fact]
    public void RetainsOperationalStageAndMatcherWithoutReplacingThemWithCleanup()
    {
        var result = new ProcessOutput(1, """
            identity_browser_stage_trusted_browser_ready
            {"eventCode":"identity_browser_profile_phase","phase":"browser_owner_observed","launch":2,"elapsedMilliseconds":13500}
            identity_browser_stage_loading_signed_out_accessible
            identity_browser_stage_process_closed
            """, """
            identity.failures.ts:78:15
            identity_browser_assert_attribute_failed
            identity_browser_failure_boundaries_failed
            """);
        var observed = IdentityBrowserFailureDiagnostics.Project(result, timedOut: false);
        Assert.Equal("identity_browser_stage_loading_signed_out_accessible", observed.LastStage);
        Assert.Equal("identity_browser_assert_attribute_failed", observed.FailureCode);
        Assert.Equal("identity_browser_profile_browser_owner_observed", observed.ProfilePhase);
        Assert.Equal(2, observed.ProfileLaunch);
        Assert.Equal(13500, observed.ProfileElapsedMilliseconds);
        Assert.Equal("identity.failures.ts", observed.SourceFile);
        Assert.Equal(78, observed.SourceLine);
        Assert.True(observed.CleanupObserved);
        Assert.False(observed.TimedOut);
    }

    [Fact]
    public void RejectsUnknownPrivatePrefixesAndMalformedProfileEnvelopes()
    {
        var privateValue = "private-" + Guid.NewGuid().ToString("N");
        var result = new ProcessOutput(1, $$"""
            identity_browser_stage_initial_browser_ready
            identity_browser_stage_{{privateValue}}
            {"eventCode":"identity_browser_profile_phase","phase":"initial_close_started","launch":1,"elapsedMilliseconds":1200,"private":"{{privateValue}}"}
            {"eventCode":"identity_browser_profile_phase","phase":"launch_started","launch":3,"elapsedMilliseconds":1200}
            {"eventCode":"identity_browser_profile_phase","phase":"{{privateValue}}","launch":1,"elapsedMilliseconds":1200}
            {"eventCode":"identity_browser_profile_phase","phase":"launch_started","launch":1,"elapsedMilliseconds":-1}
            {"eventCode":"identity_browser_profile_phase","phase":"launch_started","launch":"1","elapsedMilliseconds":1200}
            """, $"identity_browser_{privateValue}\n{privateValue}\nidentity.failures.ts:99999:1");
        var observed = IdentityBrowserFailureDiagnostics.Project(result, timedOut: true);
        Assert.Equal("identity_browser_stage_initial_browser_ready", observed.LastStage);
        Assert.Equal("identity_browser_failure_boundaries_failed", observed.FailureCode);
        Assert.Null(observed.ProfilePhase);
        Assert.Null(observed.ProfileLaunch);
        Assert.Null(observed.ProfileElapsedMilliseconds);
        Assert.Null(observed.SourceFile);
        Assert.Null(observed.SourceLine);
        Assert.True(observed.TimedOut);
        Assert.False(observed.CleanupObserved);
        Assert.DoesNotContain(privateValue, System.Text.Json.JsonSerializer.Serialize(observed), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("identity_browser_capture_response_headers_failed")]
    [InlineData("identity_browser_capture_identity_body_failed")]
    [InlineData("identity_browser_capture_context_body_failed")]
    [InlineData("identity_browser_capture_handoff_body_failed")]
    [InlineData("identity_browser_capture_page_body_failed")]
    [InlineData("identity_browser_capture_response_aborted")]
    [InlineData("identity_browser_private_cache_invalid")]
    public void RetainsOriginalFailureAlongsideSpecificCaptureFailure(string captureCode)
    {
        var result = new ProcessOutput(1, """
            identity_browser_stage_loading_signed_out_accessible
            identity_browser_stage_process_closed
            """, "identity_browser_assert_text_failed\n" + captureCode +
            "\nidentity_browser_failure_boundaries_failed");
        var observed = IdentityBrowserFailureDiagnostics.Project(result, timedOut: false);
        Assert.Equal("identity_browser_assert_text_failed", observed.FailureCode);
        Assert.Equal(new[] { "identity_browser_assert_text_failed", captureCode }, observed.FailureCodes);
        Assert.Equal("identity_browser_stage_loading_signed_out_accessible", observed.LastStage);
        Assert.True(observed.CleanupObserved);
    }

    [Fact]
    public void RetainsTrustSnapshotAndCleanupTimelineWithoutPrivateOrUnknownPhases()
    {
        var privateValue = "private-" + Guid.NewGuid().ToString("N");
        var result = new ProcessOutput(-1, $$"""
            {"eventCode":"identity_browser_profile_phase","phase":"launch_started","launch":1,"elapsedMilliseconds":2}
            {"eventCode":"identity_browser_profile_phase","phase":"owned_close_finished","launch":1,"elapsedMilliseconds":1000}
            {"eventCode":"identity_browser_profile_phase","phase":"trusted_journey_started","launch":2,"elapsedMilliseconds":2000}
            {"eventCode":"identity_browser_profile_phase","phase":"{{privateValue}}","launch":2,"elapsedMilliseconds":2001}
            {"eventCode":"identity_browser_profile_phase","phase":"capture_snapshot_started","launch":2,"elapsedMilliseconds":3000}
            {"eventCode":"identity_browser_profile_phase","phase":"capture_page_read","launch":2,"elapsedMilliseconds":3002}
            {"eventCode":"identity_browser_profile_phase","phase":"owned_close_started","launch":2,"elapsedMilliseconds":7502}
            """, "identity_browser_privacy_capture_incomplete");
        var observed = IdentityBrowserFailureDiagnostics.Project(result, timedOut: true);
        Assert.Equal(6, observed.ProfileTimeline.Length);
        Assert.Equal(2, observed.ProfileTimeline[0].ElapsedMilliseconds);
        Assert.Equal("identity_browser_profile_owned_close_finished", observed.ProfileTimeline[1].Phase);
        Assert.Equal(2000, observed.ProfileTimeline[2].ElapsedMilliseconds);
        Assert.Equal("identity_browser_profile_capture_page_read", observed.ProfileTimeline[4].Phase);
        Assert.Equal(7502, observed.ProfileTimeline[5].ElapsedMilliseconds);
        Assert.False(observed.ProfileTimelineTruncated);
        Assert.DoesNotContain(privateValue, System.Text.Json.JsonSerializer.Serialize(observed), StringComparison.Ordinal);
        var failure = IdentityBrowserFailureDiagnostics.DescribeFailure(result, timedOut: true);
        Assert.Contains("identity_browser_profile_launch_started:1:2", failure, StringComparison.Ordinal);
        Assert.Contains("identity_browser_profile_capture_page_read:2:3002", failure, StringComparison.Ordinal);
        Assert.DoesNotContain(privateValue, failure, StringComparison.Ordinal);
    }

    [Fact]
    public void BoundsTimelineWithoutLosingFinalKnownPhase()
    {
        var lines = Enumerable.Range(0, 65).Select(index =>
            $$"""{"eventCode":"identity_browser_profile_phase","phase":"capture_page_read","launch":2,"elapsedMilliseconds":{{index}}}""");
        var observed = IdentityBrowserFailureDiagnostics.Project(
            new ProcessOutput(-1, string.Join('\n', lines), ""), timedOut: true);
        Assert.Equal(64, observed.ProfileTimeline.Length);
        Assert.True(observed.ProfileTimelineTruncated);
        Assert.Equal(64, observed.ProfileElapsedMilliseconds);
        Assert.Equal(64, observed.ProfileTimeline[^1].ElapsedMilliseconds);
    }

    [Fact]
    public void DistinguishesPendingCloseProtocolFromVerifiedProcessExit()
    {
        var result = new ProcessOutput(1, """
            identity_browser_stage_private_capture_verified
            {"eventCode":"identity_browser_profile_phase","phase":"owned_context_close_pending","launch":2,"elapsedMilliseconds":18000}
            {"eventCode":"identity_browser_profile_phase","phase":"owned_termination_started","launch":2,"elapsedMilliseconds":18001}
            {"eventCode":"identity_browser_profile_phase","phase":"owned_kill_started","launch":2,"elapsedMilliseconds":19000}
            {"eventCode":"identity_browser_profile_phase","phase":"owned_process_exit_observed","launch":2,"elapsedMilliseconds":19010}
            {"eventCode":"identity_browser_profile_phase","phase":"owned_context_close_pending_after_exit","launch":2,"elapsedMilliseconds":19760}
            """, "identity_browser_cleanup_unverified");
        var observed = IdentityBrowserFailureDiagnostics.Project(result, timedOut: false);
        Assert.False(observed.CleanupObserved);
        Assert.Equal("identity_browser_cleanup_unverified", observed.FailureCode);
        Assert.Equal("identity_browser_stage_private_capture_verified", observed.LastStage);
        Assert.Equal("identity_browser_profile_owned_process_exit_observed", observed.ProfileTimeline[3].Phase);
        Assert.Equal("identity_browser_profile_owned_context_close_pending_after_exit", observed.ProfilePhase);
        Assert.Equal(19760, observed.ProfileElapsedMilliseconds);
    }
}
