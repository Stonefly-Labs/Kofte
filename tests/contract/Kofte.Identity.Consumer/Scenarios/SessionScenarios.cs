using System.Net;
using Kofte.TestSupport.Identity;
using static Kofte.Identity.Consumer.Scenarios.LoginScenarios;

namespace Kofte.Identity.Consumer.Scenarios;

internal static class SessionScenarios
{
    internal static async Task<IReadOnlyList<string>> RunAsync(string provider, CancellationToken token)
    {
        var observations = new List<string>();
        await RunSessionAsync(provider, observations, token);
        await using var designated = await LoginCommitFixture.CreateAsync(provider, token,
            productRoutes: true, actorProbe: ActorProbe.Observe);
        var authorization = await designated.NominateProductAsync(token);
        using (var login = await LoginAsync(designated, token))
            Require((int)login.StatusCode == 303, "session_nominee_login", observations);
        using (var request = EnrollmentScenarios.Enrollment(designated,
            await designated.ProductContextAsync(token), authorization))
        using (var response = await designated.SendProductAsync(request, token))
            Require(response.StatusCode == HttpStatusCode.OK, "session_explicit_designation", observations);
        var current = await CurrentAsync(designated, token);
        Require(current.Designated, "session_fresh_designation", observations);
        await ActorScenarios.CompareAsync(designated, current, observations, token);
        return observations;
    }

    private static async Task RunSessionAsync(string provider, ICollection<string> observations,
        CancellationToken token)
    {
        var clock = new SessionClock();
        var actorCalls = 0;
        await using var fixture = await LoginCommitFixture.CreateAsync(provider, token, clock,
            productRoutes: true, withBrowser: true, actorProbe: current =>
            {
                Interlocked.Increment(ref actorCalls);
                return ActorProbe.Observe(current);
            });
        foreach (var path in new[] { "/api/identity/v1/me", "/fixture-actor", "/fixture-ungranted" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            using var response = await fixture.SendProductAsync(request, token);
            Require(response.StatusCode == HttpStatusCode.Unauthorized && response.Headers.Location is null &&
                response.Content.Headers.ContentType?.MediaType == "application/problem+json",
                "session_anonymous_refusal", observations);
        }
        fixture.Protocol.DisplayName = "<script>synthetic profile</script>";
        using (var login = await LoginAsync(fixture, token))
            Require((int)login.StatusCode == 303, "session_native_login", observations);
        var identity = await CurrentAsync(fixture, token);
        Require(identity.Id != Guid.Empty && identity.Mode == "oidc" && !identity.Designated,
            "session_current_identity", observations);
        await ActorScenarios.CompareAsync(fixture, identity, observations, token);
        Require(actorCalls == 1, "session_separately_compiled_actor", observations);
        var before = await fixture.SnapshotAsync(token);
        using (var mixed = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me"))
        {
            mixed.Headers.Add("Authorization", "Bearer unsupported");
            mixed.Headers.Add("X-User-Id", Guid.NewGuid().ToString("D"));
            mixed.Headers.Add("X-Forwarded-Host", "foreign.example.invalid");
            using var response = await fixture.SendProductAsync(mixed, token);
            Require(response.StatusCode == HttpStatusCode.Unauthorized,
                "session_mixed_credentials_refused", observations);
        }
        var csrf = await fixture.ProductContextAsync(token);
        using (var rejected = SignOut(fixture, csrf))
        {
            rejected.Headers.Remove("Origin");
            rejected.Headers.Add("Origin", "null");
            using var response = await fixture.SendProductAsync(rejected, token);
            var unchanged = await fixture.SnapshotAsync(token);
            Require(response.StatusCode == HttpStatusCode.Forbidden &&
                unchanged.Sessions.Single().State == "active" && before.Events.SequenceEqual(unchanged.Events),
                "session_csrf_origin_no_effects", observations);
        }
        var cookie = fixture.ProductCookieHeader;
        using (var activity = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me"))
        using (var signOut = SignOut(fixture, csrf))
        {
            var replies = await Task.WhenAll(fixture.SendProductAsync(activity, token),
                fixture.SendProductAsync(signOut, token));
            using var active = replies[0];
            using var confirmed = replies[1];
            Require(confirmed.StatusCode == HttpStatusCode.NoContent &&
                active.StatusCode is HttpStatusCode.OK or HttpStatusCode.Unauthorized,
                "session_confirmed_revocation_racing_renewal", observations);
        }
        foreach (var path in new[] { "/api/identity/v1/me", "/fixture-actor" })
        {
            using var replay = new HttpRequestMessage(HttpMethod.Get, path);
            replay.Headers.Add("Cookie", cookie);
            using var response = await fixture.SendProductAsync(replay, token, includeCookies: false);
            Require(response.StatusCode == HttpStatusCode.Unauthorized,
                "session_next_request_denied", observations);
        }
        await fixture.RunBrowserAsync(identity.Id, token, sessionControl: async (phase, cancel) =>
        {
            if (phase == "switch-identity")
            {
                fixture.Protocol.Subject = "second-browser-subject";
                return;
            }
            var live = (await fixture.SnapshotAsync(cancel)).Sessions.Single(row => row.State == "active" &&
                row.IdleExpiresAt > clock.Now.UtcDateTime && row.AbsoluteExpiresAt > clock.Now.UtcDateTime);
            clock.Now = phase switch
            {
                "expire-idle" => new(live.IdleExpiresAt),
                "expire-absolute" => new(live.AbsoluteExpiresAt),
                _ when phase.StartsWith("activity-", StringComparison.Ordinal) => clock.Now.AddMinutes(29),
                _ => throw new InvalidOperationException("identity_scenario_session_control_invalid")
            };
        });
        var after = await fixture.SnapshotAsync(token);
        var other = after.Users.Single(user => user.Id != identity.Id);
        Require(after.Users.Count == 2 && after.Mappings.Count == 2 &&
            after.Mappings.Single(row => row.UserId == identity.Id).Subject == "synthetic-subject" &&
            after.Mappings.Single(row => row.UserId == other.Id).Subject == "second-browser-subject" &&
            after.Sessions.Count == 5 && after.Sessions.Count(row => row.State == "expired") == 2 &&
            after.Sessions.Count(row => row.State == "revoked") == 3 &&
            after.Events.Count(row => row.Code == "session_expired") == 2 &&
            after.Events.Count(row => row.Code == "session_revoked") == 3 &&
            after.RegistryRevision == before.RegistryRevision && fixture.UngrantedActions == 0,
            "session_browser_expiry_revocation_uncertainty_two_user", observations);
    }

    private static HttpRequestMessage SignOut(LoginCommitFixture fixture, string csrf)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/sign-out");
        request.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
        request.Headers.Add("X-Kofte-CSRF", csrf);
        return request;
    }

    private sealed class SessionClock : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
