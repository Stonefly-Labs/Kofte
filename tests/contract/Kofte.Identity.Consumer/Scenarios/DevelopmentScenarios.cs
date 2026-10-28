using System.Net;
using System.Text;
using System.Text.Json;
using Kofte.TestSupport.Identity;
using static Kofte.Identity.Consumer.Scenarios.LoginScenarios;

namespace Kofte.Identity.Consumer.Scenarios;

internal static class DevelopmentScenarios
{
    internal static async Task<IReadOnlyList<string>> RunAsync(string provider, CancellationToken token)
    {
        var observations = new List<string>();
        await using (var exposure = await IdentityFixture.CreateAsync(provider, token))
            foreach (var composed in new[] { false, true })
                foreach (var unsafeExposure in new[] { false, true })
                    observations.AddRange(await DevelopmentExposure.RunAsync(exposure, composed, unsafeExposure, token));
        await using (var local = await LoginCommitFixture.CreateAsync(provider, token,
            productRoutes: true, withBrowser: true, development: true, actorProbe: ActorProbe.Observe))
        {
            var selected = await SelectAsync(local, "development_browser_", observations, token);
            await CompareActorAsync(local, selected, "development_browser_", observations, token);
            await local.RunDevelopmentBrowserAsync(selected.Id, token);
            var after = await local.SnapshotAsync(token);
            Require(after.Users.Count == 1 && after.Mappings.Count == 1 &&
                after.Users[0].Id == selected.Id && after.Mappings[0].UserId == selected.Id &&
                after.Mappings[0] is { Mode: "development", Issuer: "kofte:development:v1", Subject: "synthetic" } &&
                local.ProductProviderRequests == 0 && after.BootstrapState == "disabled",
                "development_visible_browser_api_actor_parity_no_provider", observations);
        }
        await RunTransitionsAsync(provider, observations, token);
        return observations;
    }

    private static async Task RunTransitionsAsync(string provider, ICollection<string> observations,
        CancellationToken token)
    {
        foreach (var consumed in new[] { false, true })
        {
            await using var fixture = await LoginCommitFixture.CreateAsync(provider, token,
                productRoutes: true, actorProbe: ActorProbe.Observe);
            fixture.Protocol.Subject = "synthetic";
            fixture.Protocol.DisplayName = null;
            var label = consumed ? "development_consumed_" : "development_pending_";
            var authorization = await fixture.NominateProductAsync(token);
            using (var login = await LoginAsync(fixture, token))
                Require((int)login.StatusCode == 303, label + "production_login", observations);
            if (consumed)
            {
                using var request = EnrollmentScenarios.Enrollment(fixture,
                    await fixture.ProductContextAsync(token), authorization);
                using var response = await fixture.SendProductAsync(request, token);
                Require(response.StatusCode == HttpStatusCode.OK, label + "explicit_designation", observations);
            }
            var original = await fixture.SnapshotAsync(token);
            var production = await CurrentAsync(fixture, token);
            var originalUser = original.Users.Single(user => user.Id == production.Id);
            var originalMapping = original.Mappings.Single(mapping => mapping.UserId == production.Id);
            var ceremony = await fixture.BootstrapAsync(token);
            var productionCookie = fixture.ProductCookieHeader;
            await fixture.ReconcileProductModeAsync(development: true, token);
            await RefuseCookiesAsync(fixture, productionCookie, "__Host-kofte-session=", "kofte-dev-session=",
                label + "production_cookie_refused", observations, token);
            var development = await SelectAsync(fixture, label, observations, token);
            Require(development.Id != production.Id && development.DisplayName == production.DisplayName,
                label + "equal_profiles_separate_stable_users", observations);
            await CompareActorAsync(fixture, development, label + "local_", observations, token);
            var beforeRefusal = await fixture.SnapshotAsync(token);
            using (var request = EnrollmentScenarios.Enrollment(fixture,
                await fixture.ProductContextAsync(token), authorization))
            using (var response = await fixture.SendProductAsync(request, token))
                Require(response.StatusCode == HttpStatusCode.Forbidden &&
                    beforeRefusal.Events.SequenceEqual((await fixture.SnapshotAsync(token)).Events),
                    label + "development_cannot_consume_production_authority", observations);
            var developmentCookie = fixture.ProductCookieHeader;
            await fixture.ReconcileProductModeAsync(development: false, token);
            await RefuseCookiesAsync(fixture, developmentCookie, "kofte-dev-session=", "__Host-kofte-session=",
                label + "development_cookie_refused", observations, token);
            using (var login = await LoginAsync(fixture, token))
                Require((int)login.StatusCode == 303, label + "fresh_production_login", observations);
            var restored = await CurrentAsync(fixture, token);
            var after = await fixture.SnapshotAsync(token);
            var finalCeremony = await fixture.BootstrapAsync(token);
            Require(restored.Id == production.Id && restored.Mode == "oidc" && restored.Designated == consumed &&
                after.Users.Count == 2 && after.Mappings.Count == 2 &&
                after.Users.Single(user => user.Id == production.Id).CreatedAt == originalUser.CreatedAt &&
                after.Mappings.Single(mapping => mapping.UserId == production.Id) == originalMapping &&
                after.Mappings.Single(mapping => mapping.UserId == development.Id) is
                { Mode: "development", Issuer: "kofte:development:v1", Subject: "synthetic" } &&
                after.Epoch != original.Epoch && after.RegistryRevision == original.RegistryRevision &&
                original.Events.All(value => after.Events.Contains(value)) &&
                finalCeremony.State == (consumed ? "consumed" : "disabled") &&
                finalCeremony.InitialUser == ceremony.InitialUser &&
                finalCeremony.ConsumedAt == ceremony.ConsumedAt &&
                finalCeremony.ConsumedGeneration == ceremony.ConsumedGeneration,
                label + "exact_mapping_designation_history_preserved", observations);
            await CompareActorAsync(fixture, restored, label + "restored_", observations, token);
            using (var request = EnrollmentScenarios.Enrollment(fixture,
                await fixture.ProductContextAsync(token), authorization))
            using (var response = await fixture.SendProductAsync(request, token))
                Require(response.StatusCode == HttpStatusCode.Conflict,
                    label + "original_authority_replay_refused", observations);
        }
    }

    private static async Task<Current> SelectAsync(LoginCommitFixture fixture,
        string prefix, ICollection<string> observations, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/development-session")
        {
            Content = new StringContent("{\"identityKey\":\"synthetic\"}", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
        request.Headers.Add("X-Kofte-CSRF", await fixture.ProductContextAsync(token));
        using var response = await fixture.SendProductAsync(request, token);
        using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token));
        var identity = await CurrentAsync(fixture, token);
        Require(response.StatusCode == HttpStatusCode.OK && body.RootElement.GetProperty("version").GetString() == "1" &&
            body.RootElement.GetProperty("userId").GetGuid() == identity.Id &&
            identity.Id != Guid.Empty && identity.Mode == "development" && !identity.Designated &&
            fixture.ProductCookieHeader.Contains("kofte-dev-session=", StringComparison.Ordinal),
            prefix + "explicit_configured_selection", observations);
        return identity;
    }

    private static async Task CompareActorAsync(LoginCommitFixture fixture, Current identity,
        string prefix, ICollection<string> observations, CancellationToken token)
    {
        var actor = new List<string>();
        await ActorScenarios.CompareAsync(fixture, identity, actor, token);
        foreach (var observation in actor) observations.Add(prefix + observation);
    }

    private static async Task RefuseCookiesAsync(LoginCommitFixture fixture, string cookie,
        string original, string renamed, string observation, ICollection<string> observations, CancellationToken token)
    {
        Require(cookie.Contains(original, StringComparison.Ordinal), observation + "_namespace", observations);
        foreach (var (value, label) in new[]
        {
            (cookie, "_original"),
            (cookie.Replace(original, renamed, StringComparison.Ordinal), "_renamed")
        })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
            request.Headers.Add("Cookie", value);
            using var response = await fixture.SendProductAsync(request, token, includeCookies: false);
            Require(response.StatusCode == HttpStatusCode.Unauthorized && response.Headers.Location is null,
                observation + label, observations);
        }
    }
}
