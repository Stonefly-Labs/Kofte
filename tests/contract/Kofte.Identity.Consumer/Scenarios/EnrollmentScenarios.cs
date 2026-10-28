using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kofte.TestSupport.Identity;

namespace Kofte.Identity.Consumer.Scenarios;

internal static class EnrollmentScenarios
{
    internal static async Task<IReadOnlyList<string>> RunAsync(string scenario, string provider,
        CancellationToken token)
    {
        var observations = (await RunNativeAsync(scenario, provider, token)).ToList();
        if (scenario == "nominated-enrollment")
        {
            await using var fixture = await LoginCommitFixture.CreateAsync(provider, token,
                productRoutes: true, withBrowser: true);
            fixture.Protocol.DisplayName = "<script>synthetic profile</script>";
            using (var login = await LoginScenarios.LoginAsync(fixture, token))
                Require((int)login.StatusCode == 303, "native_nominee_login", observations);
            var before = await fixture.SnapshotAsync(token);
            var user = before.Users.Single().Id;
            var authorization = await fixture.NominateProductAsync(token);
            await fixture.RunBrowserAsync(user, token, authorization);
            var consumed = await fixture.BootstrapAsync(token);
            var record = (await fixture.SnapshotAsync(token)).Events
                .Single(value => value.Code == "initial_administrator_enrolled");
            Require(consumed.State == "consumed" && consumed.InitialUser == user &&
                consumed.ConsumedGeneration == 1 && record.UserId == user,
                "browser_lost_ack_fresh_designation_no_retry", observations);
        }
        return observations;
    }

    private static async Task<IReadOnlyList<string>> RunNativeAsync(string scenario, string provider,
        CancellationToken token)
    {
        await using var fixture = await LoginCommitFixture.CreateProductAsync(provider, token);
        var observations = new List<string>();
        var csrf = await fixture.ProductContextAsync(token);
        using (var login = fixture.ProductLoginRequest("/identity", csrf))
        using (var start = await fixture.SendProductAsync(login, token))
        using (var callback = await fixture.CompleteProductAsync(start, token))
            Require((int)callback.StatusCode == 303, "native_nominee_login", observations);
        var ordinary = await fixture.SnapshotAsync(token);
        var user = ordinary.Users.Single();
        Require(ordinary.BootstrapState == "disabled" && ordinary.Mappings.Single().UserId == user.Id &&
            ordinary.Mappings.Single().Issuer == fixture.Issuer &&
            ordinary.Mappings.Single().Subject == fixture.Protocol.Subject,
            "first_arrival_has_no_designation", observations);
        csrf = await fixture.ProductContextAsync(token);
        using (var absent = Enrollment(fixture, csrf, new string('A', 43)))
        using (var refused = await fixture.SendProductAsync(absent, token))
            Require(refused.StatusCode == HttpStatusCode.Conflict,
                "disabled_ceremony_refused", observations);

        var authorization = await fixture.NominateProductAsync(token);
        var pending = await fixture.BootstrapAsync(token);
        Require(authorization.Length == 43 && pending.State == "pending" && pending.Generation == 1 &&
            pending.Issuer == fixture.Issuer && pending.Subject == fixture.Protocol.Subject &&
            pending.InitialUser is null, "stopped_private_nomination", observations);
        csrf = await fixture.ProductContextAsync(token);
        var before = await fixture.SnapshotAsync(token);
        var count = scenario == "enrollment-race" ? 20 : 1;
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var contenders = Enumerable.Range(0, count).Select(async _ =>
        {
            using var request = Enrollment(fixture, csrf, authorization);
            await release.Task;
            using var response = await fixture.SendProductAsync(request, token);
            var bytes = await response.Content.ReadAsByteArrayAsync(token);
            if (bytes.Length > 8192) throw new InvalidOperationException("identity_scenario_response_bound");
            using var document = JsonDocument.Parse(bytes);
            var body = document.RootElement;
            if (response.StatusCode == HttpStatusCode.OK)
            {
                if (body.GetProperty("version").GetString() != "1" ||
                    body.GetProperty("status").GetString() != "enrolled")
                    throw new InvalidOperationException("identity_scenario_receipt_invalid");
            }
            else if (response.StatusCode != HttpStatusCode.Conflict ||
                body.GetProperty("code").GetString() != "identity_enrollment_unavailable")
                throw new InvalidOperationException("identity_scenario_contender_rejected");
            if (new[] { authorization, fixture.Issuer, fixture.Protocol.Subject }.Any(value =>
                    document.RootElement.GetRawText().Contains(value, StringComparison.Ordinal)))
                throw new InvalidOperationException("identity_scenario_enrollment_not_private");
            return (response.StatusCode, Correlation: body.GetProperty("correlationId").GetString());
        }).ToArray();
        release.SetResult();
        var outcomes = await Task.WhenAll(contenders);
        Require(outcomes.Count(value => value.StatusCode == HttpStatusCode.OK) == 1 &&
            outcomes.Count(value => value.StatusCode == HttpStatusCode.Conflict) == count - 1,
            count == 20 ? "twenty_admitted_one_winner_nineteen_conflicts" : "confirmed_protected_enrollment",
            observations);
        var after = await fixture.SnapshotAsync(token);
        var consumed = await fixture.BootstrapAsync(token);
        var receipt = after.Events.Except(before.Events).Single();
        Require(consumed.State == "consumed" && consumed.InitialUser == user.Id &&
            consumed.Generation == pending.Generation && consumed.ConsumedGeneration == pending.Generation &&
            consumed.ConsumedAt is not null && consumed.Issuer is null && consumed.Subject is null &&
            consumed.Digest is null && consumed.Expires is null &&
            receipt.Code == "initial_administrator_enrolled" && receipt.UserId == user.Id &&
            receipt.Correlation == outcomes.Single(value => value.StatusCode == HttpStatusCode.OK).Correlation &&
            receipt.Revision > before.Revision && receipt.Revision <= after.Revision &&
            before.Users.SequenceEqual(after.Users) && before.Mappings.SequenceEqual(after.Mappings) &&
            before.RegistryRevision == after.RegistryRevision && before.Epoch == after.Epoch,
            "atomic_exact_designation_consumption_event", observations);
        await RequireIdentityAsync(fixture, user.Id, token);
        observations.Add("fresh_identity_actor_parity");
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/fixture-ungranted"))
        using (var response = await fixture.SendProductAsync(request, token))
            Require(response.StatusCode == HttpStatusCode.Forbidden,
                "designation_grants_no_unrelated_action", observations);

        var processes = new HashSet<int>();
        for (var restart = 0; restart < 3; restart++)
        {
            Require(processes.Add(await fixture.RestartProductAsync(token)),
                "fresh_process_" + (restart + 1), observations);
            await RequireIdentityAsync(fixture, user.Id, token);
            using var replay = Enrollment(fixture, await fixture.ProductContextAsync(token), authorization);
            using var response = await fixture.SendProductAsync(replay, token);
            var unchanged = await fixture.SnapshotAsync(token);
            Require(response.StatusCode == HttpStatusCode.Conflict &&
                consumed == await fixture.BootstrapAsync(token) && after.Events.SequenceEqual(unchanged.Events) &&
                after.Users.SequenceEqual(unchanged.Users) && after.Mappings.SequenceEqual(unchanged.Mappings) &&
                after.RegistryRevision == unchanged.RegistryRevision,
                "restart_replay_closed_" + (restart + 1), observations);
        }
        await fixture.PrepareProductAgainAsync(token);
        Require(consumed == await fixture.BootstrapAsync(token) &&
            after.Events.SequenceEqual((await fixture.SnapshotAsync(token)).Events),
            "idempotent_upgrade_prepare_preserves_consumption", observations);
        return observations;
    }

    private static async Task RequireIdentityAsync(LoginCommitFixture fixture, Guid user, CancellationToken token)
    {
        foreach (var path in new[] { "/api/identity/v1/me", "/fixture-actor" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            using var response = await fixture.SendProductAsync(request, token);
            response.EnsureSuccessStatusCode();
            using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token));
            if (body.RootElement.GetProperty("userId").GetGuid() != user ||
                body.RootElement.GetProperty("identityMode").GetString() != "oidc" ||
                !body.RootElement.GetProperty("isInitialAdministrator").GetBoolean())
                throw new InvalidOperationException("identity_scenario_fresh_designation_invalid");
        }
    }

    internal static HttpRequestMessage Enrollment(LoginCommitFixture fixture, string csrf, string authorization)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/first-administrator")
        {
            Content = JsonContent.Create(new { authorization })
        };
        request.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
        request.Headers.Add("X-Kofte-CSRF", csrf);
        return request;
    }

    private static void Require(bool condition, string observation, ICollection<string> observations)
    {
        if (!condition) throw new InvalidOperationException("identity_scenario_" + observation);
        observations.Add(observation);
    }
}
