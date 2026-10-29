using System.Diagnostics;
using System.Net;
using System.Text;
using Kofte.TestSupport;
using Kofte.TestSupport.Identity;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Kofte.Host.IntegrationTests.Identity;

public abstract class IdentityBrowserFailureTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RealDevelopmentSelectionAndSignOutRequirePrivateBrowserAndTransportEvidence()
    {
        var fixture = await LoginCommitFixture.CreateAsync(Provider, Token,
            productRoutes: true, withBrowser: true, development: true);
        List<Exception> failures = [];
        try
        {
            var user = await fixture.CreateStoredDevelopmentSessionAsync(Token);
            await fixture.RunDevelopmentBrowserAsync(user, Token);
            var after = await fixture.SnapshotAsync(Token);
            Assert.Equal(user, Assert.Single(after.Users).Id);
            Assert.Equal(user, Assert.Single(after.Mappings).UserId);
            Assert.Equal("development", after.Mappings[0].Mode);
            Assert.Equal("disabled", after.BootstrapState);
            Assert.Equal(0, fixture.ProductProviderRequests);
        }
        catch (Exception error) { failures.Add(error); }
        try { await fixture.DisposeAsync(); }
        catch (Exception error) { failures.Add(error); }
        if (failures.Count > 1)
            throw new AggregateException("identity_browser_failure_boundaries_failed", failures);
        if (failures.Count == 1)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
    }

    [Fact]
    public async Task RealCancelledRefusedAndUnavailableCallbacksHaveAccessiblePrivateExplicitRetry()
    {
        if (OperatingSystem.IsWindows()) throw new InvalidOperationException("test_platform_unsupported");
        var fixture = await LoginCommitFixture.CreateAsync(Provider, Token,
            productRoutes: true, withBrowser: true);
        var root = new OwnedTestRoot();
        var browserCleanupObserved = false;
        List<Exception> failures = [];
        try
        {
            fixture.Protocol.DisplayName = "<script>synthetic profile</script>";
            using (var request = fixture.ProductLoginRequest("/identity", await fixture.ProductContextAsync(Token)))
            using (var start = await fixture.SendProductAsync(request, Token))
            using (var completed = await fixture.CompleteProductAsync(start, Token))
                Assert.Equal(HttpStatusCode.SeeOther, completed.StatusCode);
            var before = await fixture.SnapshotAsync(Token);
            var user = Assert.Single(before.Users).Id;
            var authorizations = 0;
            var tokenRequests = fixture.Protocol.TokenRequests;
            var providerCanary = "identity-private-" + Guid.NewGuid().ToString("N");
            fixture.Protocol.BeforeResponse = (operation, _) =>
            {
                if (operation != "authorization") return Task.CompletedTask;
                var attempt = Interlocked.Increment(ref authorizations);
                fixture.Protocol.CancelAuthorization = attempt == 1;
                fixture.Protocol.Mutation = attempt == 2 ? IdTokenMutation.WrongSignature : IdTokenMutation.None;
                fixture.Protocol.ResponseOverride = (phase, _) => attempt == 3 && phase == "token"
                    ? Results.Json(new { error = "temporarily_unavailable", error_description = providerCanary },
                        statusCode: 503)
                    : null;
                return Task.CompletedTask;
            };
            Directory.CreateDirectory(root.Path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            fixture.BeginBrowserActionCapture(root.Path);
            var authorityFile = Path.Combine(root.Path, "browser-ca.pem");
            PrivateTestFiles.WriteNew(authorityFile, Encoding.ASCII.GetBytes(fixture.Protocol.AuthorityCertificatePem));
            var startInfo = new ProcessStartInfo("node")
            {
                WorkingDirectory = OwnedProcesses.FindRepository(),
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            OwnedProcesses.ClearSensitiveEnvironment(startInfo);
            startInfo.ArgumentList.Add(Path.Combine(startInfo.WorkingDirectory, "tests", "e2e", "identity.failures.ts"));
            startInfo.Environment["KOFTE_IDENTITY_WEB_URL"] = fixture.PublicOrigin.AbsoluteUri;
            startInfo.Environment["KOFTE_IDENTITY_PRIVATE_ROOT"] = root.Path;
            startInfo.Environment["KOFTE_IDENTITY_AUTHORITY_FILE"] = authorityFile;
            startInfo.Environment["NODE_EXTRA_CA_CERTS"] = authorityFile;
            startInfo.Environment["KOFTE_IDENTITY_EXPECTED_USER"] = user.ToString("D");
            startInfo.Environment["KOFTE_IDENTITY_PRIVACY_CANARY"] = providerCanary;
            startInfo.Environment["TMPDIR"] = root.Path;
            startInfo.Environment["TMP"] = root.Path;
            startInfo.Environment["TEMP"] = root.Path;
            await using var child = OwnedProcesses.Start(startInfo);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(Token);
            deadline.CancelAfter(TimeSpan.FromSeconds(45));
            ProcessOutput result;
            var timedOut = false;
            try { result = await child.WaitAsync(deadline.Token); }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested && !Token.IsCancellationRequested)
            {
                timedOut = true;
                await child.StopAsync();
                result = await child.ResultAsync();
            }
            Assert.False((result.Stdout + result.Stderr).Contains(providerCanary, StringComparison.Ordinal));
            if (timedOut || result.ExitCode != 0)
                throw new InvalidOperationException(IdentityBrowserFailureDiagnostics.DescribeFailure(result, timedOut));
            Assert.Contains("identity_browser_stage_failure_boundaries_verified",
                result.Stdout.Split('\n', StringSplitOptions.TrimEntries));
            browserCleanupObserved = true;
            Assert.DoesNotContain(providerCanary, result.Stdout + result.Stderr, StringComparison.Ordinal);
            Assert.Equal(4, authorizations);
            Assert.Equal(tokenRequests + 3, fixture.Protocol.TokenRequests);
            var after = await fixture.SnapshotAsync(Token);
            Assert.Equal(before.Users, after.Users);
            Assert.Equal(before.Mappings, after.Mappings);
            Assert.Equal(before.RegistryRevision, after.RegistryRevision);
            Assert.Equal("disabled", after.BootstrapState);
            Assert.Equal(before.Sessions.Count + 1, after.Sessions.Count);
            Assert.Single(after.Events, row => row.Code == "user_created");
            Assert.Equal(2, after.Events.Count(row => row.Code == "session_created"));
            fixture.RequireBrowserTransportPrivacy(root.Path, providerCanary);
            Assert.Equal(4, fixture.BrowserActionCaptureCount);
        }
        catch (Exception error) { failures.Add(error); }
        try { await fixture.DisposeAsync(); }
        catch (Exception error) { failures.Add(error); }
        // A failed child has not proved that its private profile can be removed.
        if (browserCleanupObserved)
        {
            try { root.Dispose(); }
            catch (Exception error) { failures.Add(error); }
        }
        if (failures.Count > 1)
            throw new AggregateException("identity_browser_failure_boundaries_failed", failures);
        if (failures.Count == 1)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failures[0]).Throw();
    }
}

[Trait("Provider", "sqlite")]
[Trait("Suite", "browser")]
public sealed class SqliteIdentityBrowserFailureTests : IdentityBrowserFailureTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Trait("Suite", "browser")]
public sealed class PostgresIdentityBrowserFailureTests : IdentityBrowserFailureTests
{
    protected override string Provider => "postgres";
}
