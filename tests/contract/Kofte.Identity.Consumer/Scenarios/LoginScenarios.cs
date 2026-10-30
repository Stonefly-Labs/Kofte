using System.Net;
using System.Text.Json;
using Kofte.TestSupport.Identity;

namespace Kofte.Identity.Consumer.Scenarios;

internal static class LoginScenarios
{
    internal static async Task<IReadOnlyList<string>> RunAsync(string scenario, string provider,
        CancellationToken token)
    {
        await using var fixture = await LoginCommitFixture.CreateAsync(provider, token,
            productRoutes: true, withBrowser: true);
        var observations = new List<string>();
        var original = await fixture.SnapshotAsync(token);
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me"))
        using (var response = await fixture.SendProductAsync(request, token))
            Require(response.StatusCode == HttpStatusCode.Unauthorized, "missing_auth", observations);

        if (scenario == "trust-refusal")
        {
            fixture.Protocol.Mutation = IdTokenMutation.WrongSignature;
            using (var response = await LoginAsync(fixture, token))
                Require(response.StatusCode == HttpStatusCode.Unauthorized &&
                    !HasSessionCookie(response), "invalid_signature", observations);
            var refused = await fixture.SnapshotAsync(token);
            Require(refused.Users.Count == 0 && refused.Mappings.Count == 0 && refused.Sessions.Count == 0 &&
                original.Events.SequenceEqual(refused.Events), "no_refused_effects", observations);
            var requests = fixture.ProductProviderRequests;
            using (var request = new HttpRequestMessage(HttpMethod.Get, "/auth/oidc/callback?code=unsolicited"))
            using (var response = await fixture.SendProductAsync(request, token))
                Require(response.StatusCode == HttpStatusCode.Unauthorized &&
                    fixture.ProductProviderRequests == requests, "unsolicited_refusal", observations);
            fixture.Protocol.Mutation = IdTokenMutation.None;
        }

        fixture.Protocol.DisplayName = "<script>synthetic profile</script>";
        using (var response = await LoginAsync(fixture, token))
            Require((int)response.StatusCode == 303 && response.Headers.Location?.OriginalString == "/identity" &&
                HasSessionCookie(response), "native_login", observations);
        var current = await CurrentAsync(fixture, token);
        Require(current.Id != Guid.Empty && current.DisplayName == fixture.Protocol.DisplayName &&
            current.Mode == "oidc" && !current.Designated, "current_identity", observations);
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/fixture-actor"))
        using (var response = await fixture.SendProductAsync(request, token))
        {
            response.EnsureSuccessStatusCode();
            using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token));
            Require(body.RootElement.GetProperty("userId").GetGuid() == current.Id &&
                body.RootElement.GetProperty("identityMode").GetString() == current.Mode &&
                !body.RootElement.GetProperty("isInitialAdministrator").GetBoolean(),
                "public_actor_parity", observations);
        }
        fixture.Protocol.DisplayName = "Changed synthetic profile";
        using (var response = await LoginAsync(fixture, token))
            Require((int)response.StatusCode == 303, "fresh_login", observations);
        var refreshed = await CurrentAsync(fixture, token);
        var committed = await fixture.SnapshotAsync(token);
        Require(refreshed.Id == current.Id && refreshed.DisplayName == fixture.Protocol.DisplayName &&
            committed.Users.Count == 1 && committed.Mappings.Count == 1 &&
            committed.Events.Count(value => value.Code == "user_created") == 1 &&
            committed.RegistryRevision == original.RegistryRevision &&
            committed.BootstrapState == "disabled", "stable_mapping_no_designation", observations);
        var issued = fixture.ProductProviderRequests;
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me"))
        {
            request.Headers.Authorization = new("Bearer", "synthetic-unsupported-credential");
            using var response = await fixture.SendProductAsync(request, token);
            Require(response.StatusCode == HttpStatusCode.Unauthorized &&
                fixture.ProductProviderRequests == issued, "mixed_credentials_refused", observations);
        }
        fixture.Protocol.DisplayName = "<script>synthetic profile</script>";
        await fixture.RunBrowserAsync(current.Id, token);
        var afterBrowser = await fixture.SnapshotAsync(token);
        Require((await CurrentAsync(fixture, token)).Id == current.Id &&
            afterBrowser.Users.Count == 1 && afterBrowser.Mappings.Count == 1 &&
            afterBrowser.Events.Count(value => value.Code == "user_created") == 1 &&
            afterBrowser.BootstrapState == "disabled", "browser_adapter_consumer_parity", observations);
        return observations;
    }

    internal static async Task<HttpResponseMessage> LoginAsync(LoginCommitFixture fixture, CancellationToken token)
    {
        var csrf = await fixture.ProductContextAsync(token);
        using var request = fixture.ProductLoginRequest("/identity", csrf);
        using var start = await fixture.SendProductAsync(request, token);
        return await fixture.CompleteProductAsync(start, token);
    }

    internal static async Task<Current> CurrentAsync(LoginCommitFixture fixture, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
        using var response = await fixture.SendProductAsync(request, token);
        response.EnsureSuccessStatusCode();
        using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token));
        var value = body.RootElement;
        if (value.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal)
                .SequenceEqual(new[] { "displayName", "identityMode", "isInitialAdministrator", "userId", "version" }) &&
            value.GetProperty("version").GetString() == "1")
            return new(value.GetProperty("userId").GetGuid(), value.GetProperty("displayName").GetString(),
                value.GetProperty("identityMode").GetString()!, value.GetProperty("isInitialAdministrator").GetBoolean());
        throw new InvalidOperationException("identity_scenario_current_shape");
    }

    private static bool HasSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values)) return false;
        var session = values.SingleOrDefault(value => value.StartsWith("__Host-kofte-session=", StringComparison.Ordinal));
        if (session is null) return false;
        var flags = session.Split(';', StringSplitOptions.TrimEntries).Skip(1).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return flags.Contains("httponly") && flags.Contains("secure") && flags.Contains("samesite=lax") &&
            flags.Contains("path=/") && !flags.Any(value => value.StartsWith("domain=", StringComparison.OrdinalIgnoreCase));
    }

    internal static void Require(bool condition, string observation, ICollection<string> observations)
    {
        if (!condition) throw new InvalidOperationException("identity_scenario_" + observation);
        observations.Add(observation);
    }

    internal sealed record Current(Guid Id, string? DisplayName, string Mode, bool Designated);
}
