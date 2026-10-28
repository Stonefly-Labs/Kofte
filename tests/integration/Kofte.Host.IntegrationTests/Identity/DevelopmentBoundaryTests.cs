using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.Json.Nodes;
using Kofte.TestSupport.Identity;
using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

public abstract class DevelopmentBoundaryTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ActualStartupRefusesEveryDevelopmentEnvironmentAndExposureCaseWithoutChangingPreparedState(
        bool composed, bool unsafeExposure)
    {
        await using var fixture = await IdentityFixture.CreateAsync(Provider, Token);
        fixture.WriteConfiguration(Configuration(fixture));
        foreach (var command in new[] { new[] { "--registry", "upgrade" }, ["--identity", "prepare"] })
        {
            await using var prepare = fixture.Start(command);
            Assert.Equal(0, (await prepare.WaitAsync(fixture.Storage.Token)).ExitCode);
        }
        var before = await fixture.PersistentSnapshotAsync(Token);
        foreach (var fault in new[]
        {
            "production", "staging", "wrong-environment-case", "conflicting-environment",
            "missing-configuration", "missing-mode", "unknown-mode", "disabled", "missing-opt-in",
            "missing-choices", "empty-choices", "caller-authority", "wrong-public-origin",
            "host-wildcard", "host-ipv6", "host-public",
            "web-wildcard", "web-ipv6", "web-public",
            "backend-wildcard", "backend-ipv6", "backend-public"
        })
        {
            var configuration = JsonNode.Parse(Configuration(fixture))!.AsObject();
            var development = configuration["development"]!.AsObject();
            if (fault == "missing-mode") configuration.Remove("mode");
            if (fault == "unknown-mode") configuration["mode"] = "Development";
            if (fault == "disabled") development["enabled"] = false;
            if (fault == "missing-opt-in") development.Remove("enabled");
            if (fault == "missing-choices") development.Remove("identities");
            if (fault == "empty-choices") development["identities"] = new JsonArray();
            if (fault == "caller-authority") development["identities"]![0]!["isInitialAdministrator"] = true;
            if (fault == "wrong-public-origin")
                configuration["publicOrigin"] = "http://127.0.0.2:" + fixture.WebUrl.Port;
            fixture.WriteConfiguration(JsonSerializer.SerializeToUtf8Bytes(configuration));
            var environment = fault switch
            {
                "production" => "Production",
                "staging" => "Staging",
                "wrong-environment-case" => "development",
                _ => "Development"
            };
            await using var process = fixture.Start([], environment, composed: composed, configure: start =>
            {
                start.Environment["KOFTE_ALLOW_UNSAFE_EXPOSURE"] = unsafeExposure ? "true" : "false";
                if (fault == "missing-configuration") start.Environment.Remove("KOFTE_AUTH_CONFIG_FILE");
                if (fault == "conflicting-environment") start.Environment["DOTNET_ENVIRONMENT"] = "Production";
                foreach (var (prefix, variable, port) in new[]
                {
                    ("host", "KOFTE_HOST_URL", fixture.HostUrl.Port),
                    ("web", "KOFTE_WEB_URL", fixture.WebUrl.Port),
                    ("backend", "KOFTE_BACKEND_ORIGIN", fixture.HostUrl.Port)
                })
                {
                    var host = fault == prefix + "-wildcard" ? "0.0.0.0" :
                        fault == prefix + "-ipv6" ? "[::]" :
                        fault == prefix + "-public" ? "192.0.2.1" : null;
                    if (host is not null) start.Environment[variable] = $"http://{host}:{port}";
                }
            });
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(Token);
            deadline.CancelAfter(TimeSpan.FromSeconds(5));
            var result = await process.WaitAsync(deadline.Token);
            Assert.True(result.ExitCode != 0, "identity_development_startup_case_" + fault);
            var output = result.Stdout + result.Stderr;
            var bootstrapRefusal = fault.StartsWith("backend-", StringComparison.Ordinal) ||
                !unsafeExposure && (fault.StartsWith("host-", StringComparison.Ordinal) ||
                    fault.StartsWith("web-", StringComparison.Ordinal));
            var expected = bootstrapRefusal
                ? composed ? "composition_configuration_invalid" : "configuration_invalid"
                : "identity_configuration_invalid";
            var field = bootstrapRefusal && composed ? "code" : "eventCode";
            Assert.True(output.Contains("\"" + field + "\":\"" + expected + "\"", StringComparison.Ordinal),
                "identity_development_startup_case_" + fault + "_expected_" + expected);
            Assert.DoesNotContain(fixture.PrivateDirectory, output, StringComparison.Ordinal);
            Assert.True(process.ExitVerified && process.DescendantExitsVerified);
            using var listener = new TcpListener(IPAddress.Loopback, fixture.HostUrl.Port);
            listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Start();
        }
        Assert.Equal(before, await fixture.PersistentSnapshotAsync(Token));
        Assert.True(fixture.AllExitsVerified);
    }

    private static byte[] Configuration(IdentityFixture fixture) => JsonSerializer.SerializeToUtf8Bytes(new
    {
        version = "1",
        mode = "development",
        publicOrigin = fixture.WebUrl.GetLeftPart(UriPartial.Authority),
        keyRingDirectory = fixture.KeyRingDirectory,
        development = new { enabled = true, identities = new[] { new { key = "synthetic" } } }
    });
}

[Trait("Provider", "sqlite")]
[Collection("identity-deadlines")]
public sealed class SqliteDevelopmentBoundaryTests : DevelopmentBoundaryTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Collection("identity-deadlines")]
public sealed class PostgresDevelopmentBoundaryTests : DevelopmentBoundaryTests
{
    protected override string Provider => "postgres";
}
