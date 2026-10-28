using System.Net;
using System.Text.Json;
using Kofte.TestSupport.Identity;
using static Kofte.Identity.Consumer.Scenarios.LoginScenarios;

namespace Kofte.Identity.Consumer.Scenarios;

internal static class ActorScenarios
{
    internal static async Task CompareAsync(LoginCommitFixture fixture, Current expected,
        ICollection<string> observations, CancellationToken token)
    {
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/fixture-actor"))
        using (var response = await fixture.SendProductAsync(request, token))
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token));
            var actor = document.RootElement;
            Require(response.StatusCode == HttpStatusCode.OK &&
                actor.EnumerateObject().Select(value => value.Name).Order(StringComparer.Ordinal)
                    .SequenceEqual(["identityMode", "isInitialAdministrator", "userId"]) &&
                actor.GetProperty("userId").GetGuid() == expected.Id &&
                actor.GetProperty("identityMode").GetString() == expected.Mode &&
                actor.GetProperty("isInitialAdministrator").GetBoolean() == expected.Designated,
                "session_actor_parity", observations);
        }
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/fixture-ungranted"))
        using (var response = await fixture.SendProductAsync(request, token))
            Require(response.StatusCode == HttpStatusCode.Forbidden && fixture.UngrantedActions == 0,
                expected.Designated ? "session_designation_is_not_permission" : "session_ungranted_action_denied",
                observations);
    }
}
