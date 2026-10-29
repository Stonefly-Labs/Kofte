using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Diagnostics.Metrics;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.Identity;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Microsoft.Extensions.DependencyInjection;

namespace Kofte.TestSupport.Identity;

public sealed record IdentityRestoreRecipe(LoginCommitSnapshot Before, LoginCommitSnapshot After,
    LoginBootstrapObservation BootstrapBefore, LoginBootstrapObservation BootstrapAfter,
    string ModuleBefore, string ModuleAfter, bool ExposureRefused, bool InterruptedExitVerified,
    bool OldCookieRefused, Guid? FreshUser, bool FreshDesignation, bool ReassignmentRefused);

public sealed partial class LoginCommitFixture
{
    private readonly List<string> operationsDiagnosticLines = [];
    private int operationsDiagnosticBytes;
    public IReadOnlyList<string> OperationsDiagnosticLines
    {
        get { lock (operationsDiagnosticLines) return operationsDiagnosticLines.ToArray(); }
    }

    private void ConfigureOperationsOwner(IServiceCollection services) =>
        services.AddSingleton(new IdentityDiagnosticContext(installation.Provider, record =>
        {
            var line = Encoding.UTF8.GetString(Kofte.Infrastructure.Identity.IdentityDiagnostics.Serialize(record));
            lock (operationsDiagnosticLines)
            {
                operationsDiagnosticBytes = checked(operationsDiagnosticBytes + Encoding.UTF8.GetByteCount(line));
                if (operationsDiagnosticBytes > 1_000_000)
                    throw new InvalidOperationException("identity_fixture_diagnostic_capture_limit");
                operationsDiagnosticLines.Add(line);
            }
            IdentityOperationsCapture.Record(line);
        }));

    public async Task UseInProcessOperationsAsync(CancellationToken token)
    {
        await StopProductAsync(token);
        await OpenOwnerAsync(token);
        await StartAsync(token);
    }

    public static async Task<LoginCommitFixture> CreateOperationsAsync(string provider,
        Func<StorageProviderScope, CancellationToken, Task> prepareStorage, CancellationToken token)
    {
        var fixture = new LoginCommitFixture { productRoutes = true };
        try
        {
            fixture.scope = await StorageProviderScope.CreateAsync(provider, token);
            fixture.issuer = await OidcIssuer.CreateAsync(token);
            fixture.serving = new("Production", fixture.issuer);
            await prepareStorage(fixture.scope, fixture.scope.Token);
            await fixture.serving.PrepareAsync(
                start => fixture.scope.ConfigureChild(start, offlineMaintenance: true), fixture.scope.Token);
            var bootstrap = new BootstrapConfiguration(provider == "sqlite" ? "local-sqlite" : "test-postgres",
                new("http://127.0.0.1:5080"), fixture.serving.WebUrl, new("http://127.0.0.1:5080"),
                fixture.scope.DataRoot, fixture.scope.Postgres?.ConnectionString, false, null)
            {
                AuthenticationConfigurationFile = fixture.serving.ConfigurationPath
            };
            fixture.configuration = IdentityConfiguration.Load(bootstrap, "Production");
            fixture.installation = fixture.scope.Postgres is { } postgres
                ? StorageInstallation.PostgreSql(postgres.ConnectionString)
                : StorageInstallation.Sqlite(fixture.scope.DataRoot);
            await fixture.OpenOwnerAsync(fixture.scope.Token);
            await fixture.StartAsync(fixture.scope.Token);
            return fixture;
        }

        catch { await fixture.DisposeAsync(); throw; }
    }

    public async Task<T> ObserveStoppedOperationsAsync<T>(
        Func<StorageInstallation, CancellationToken, Task<T>> observe, CancellationToken token)
    {
        await StopProductAsync(token);
        var value = await observe(installation, token);
        await StartWorkerAsync("--fixture-product-login", client.BaseAddress!.Port, token);
        return value;
    }

    public async Task<ProcessOutput> OperatorOperationsAsync(string[] arguments, CancellationToken token)
    {
        await StopProductAsync(token);
        var result = await RunOperationsCommandAsync(arguments, token);
        await StartWorkerAsync("--fixture-product-login", client.BaseAddress!.Port, token);
        return result;
    }

    private async Task<ProcessOutput> RunOperationsCommandAsync(string[] arguments, CancellationToken token)
    {
        var build = AppContext.BaseDirectory.Contains("/Release/", StringComparison.Ordinal) ? "Release" : "Debug";
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = OwnedProcesses.FindRepository(),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add(Path.Combine(OwnedProcesses.FindRepository(), "src/Kofte.Host/bin", build,
            "net10.0/Kofte.Host.dll"));
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        scope!.ConfigureChild(start, offlineMaintenance: true);
        serving!.ConfigureChild(start);
        return await OwnedProcesses.RunAsync(start, TimeSpan.FromSeconds(120), token);
    }

    public async Task<bool> RefuseRenominationOperationsAsync(CancellationToken token)
    {
        var directory = Path.GetDirectoryName(serving!.ConfigurationPath)!;
        var nominee = Path.Combine(directory, "lost-idp-nominee-" + Guid.NewGuid().ToString("N"));
        var authorization = Path.Combine(directory, "refused-authorization-" + Guid.NewGuid().ToString("N"));
        PrivateTestFiles.WriteNew(nominee, JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = "1",
            issuer = Issuer,
            subject = "replacement-idp-identity"
        }));
        var before = await SnapshotAsync(token);
        var bootstrap = await BootstrapAsync(token);
        var output = await OperatorOperationsAsync(
            ["--identity", "bootstrap", "authorize", "--nominee-file", nominee, "--authorization-file", authorization], token);
        var after = await SnapshotAsync(token);
        return output.ExitCode != 0 && !File.Exists(authorization) &&
            output.Stderr.Contains("\"eventCode\":\"identity_enrollment_unavailable\"", StringComparison.Ordinal) &&
            bootstrap == await BootstrapAsync(token) && before.Users.SequenceEqual(after.Users) &&
            before.Mappings.SequenceEqual(after.Mappings) && before.Events.SequenceEqual(after.Events) &&
            before.Revision == after.Revision && before.Epoch == after.Epoch;
    }

    public async Task<IdentityRestoreRecipe> RestoreOperationsAsync(string backup, string priorEnrollment,
        string? interruptPhase, Func<StorageInstallation, CancellationToken, Task<string>> moduleSnapshot,
        CancellationToken token, LoginCommitSnapshot? expectedRecoveryPoint = null,
        LoginBootstrapObservation? expectedBootstrap = null,
        Func<CancellationToken, Task>? advanceSourceAsync = null)
    {
        if (priorEnrollment is not ("known-never-enrolled" or "not-established"))
            throw new InvalidOperationException("identity_fixture_restore_decision_invalid");
        await StopProductAsync(token);
        var before = expectedRecoveryPoint ?? await SnapshotAsync(token);
        var ceremony = expectedBootstrap ?? await BootstrapAsync(token);
        var moduleBefore = await moduleSnapshot(installation, token);
        var oldCookie = ProductCookieHeader;
        await using var target = await IdentityFixture.CreateAsync(scope!.Provider, token);
        using var sqliteTarget = new OwnedTestRoot();
        var json = JsonNode.Parse(await File.ReadAllBytesAsync(serving!.ConfigurationPath, token))!;
        json["keyRingDirectory"] = target.KeyRingDirectory;
        target.WriteConfiguration(JsonSerializer.SerializeToUtf8Bytes(json));
        PrivateTestFiles.WriteNew(Path.Combine(target.PrivateDirectory, "issuer-ca.pem"),
            Encoding.ASCII.GetBytes(issuer.AuthorityCertificatePem));
        if (scope.Postgres is { } source)
            await source.RestoreStorageSetToFreshQuarantinedAsync(backup, target.Storage.Postgres!,
                target.KeyRingDirectory, token);
        else
            await SqliteRecoveryExercise.RestoreFreshQuarantinedAsync(backup, sqliteTarget.Path,
                target.KeyRingDirectory, token);
        if (advanceSourceAsync is not null)
        {
            await advanceSourceAsync(token);
            await StopProductAsync(token);
        }
        var targetInstallation = target.Storage.Postgres is { } targetPostgres
            ? StorageInstallation.PostgreSql(targetPostgres.ConnectionString)
            : StorageInstallation.Sqlite(sqliteTarget.Path);
        var quarantine = await IdentityRestoreQuarantine.ReadAsync(target.KeyRingDirectory, token);
        var decision = Path.Combine(target.PrivateDirectory, "restore-decision");
        PrivateTestFiles.WriteNew(decision, JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = "1",
            recoverySetId = quarantine.Manifest.RecoverySetId.ToString("D"),
            installationId = quarantine.Manifest.InstallationId.ToString("D"),
            expectedManifestSha256 = quarantine.ManifestSha256,
            priorEnrollment
        }));
        void Configure(ProcessStartInfo start)
        {
            if (target.Storage.Postgres is null) start.Environment["KOFTE_DATA_ROOT"] = sqliteTarget.Path;
        }
        bool exposureRefused;
        await using (var refused = target.Start([], "Production", configure: Configure))
        {
            var output = await refused.WaitAsync(token);
            exposureRefused = output.ExitCode != 0 && refused.ExitVerified && refused.DescendantExitsVerified &&
                output.Stderr.Contains("identity_reconciliation_required", StringComparison.Ordinal);
        }
        var interruptedExit = false;
        if (interruptPhase is not null)
        {
            var barrier = new IdentityCommitBarrier(target.PrivateDirectory, interruptPhase, maintenanceChild: true);
            await using var interrupted = target.Start(
                ["--identity", "reconcile-restore", "--decision-file", decision], "Production",
                barrier, configure: Configure);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            await barrier.WaitAsync(deadline.Token);
            var output = await interrupted.InterruptMaintenanceChildAsync(barrier, deadline.Token);
            interruptedExit = output.ExitCode != 0 && interrupted.ExitVerified && interrupted.DescendantExitsVerified &&
                IdentityRestoreQuarantine.IsRequired(target.KeyRingDirectory);
            if (!interruptedExit) throw new InvalidOperationException("identity_fixture_restore_interruption_invalid");
        }
        await using (var retry = target.Start(
            ["--identity", "reconcile-restore", "--decision-file", decision], "Production", configure: Configure))
        {
            var output = await retry.WaitAsync(token);
            if (output.ExitCode != 0 || !retry.ExitVerified || !retry.DescendantExitsVerified ||
                IdentityRestoreQuarantine.IsRequired(target.KeyRingDirectory))
                throw new InvalidOperationException("identity_fixture_restore_retry_failed");
        }
        var originalInstallation = installation;
        LoginCommitSnapshot after;
        LoginBootstrapObservation restoredCeremony;
        try
        {
            installation = targetInstallation;
            after = await SnapshotAsync(token);
            restoredCeremony = await BootstrapAsync(token);
        }
        finally { installation = originalInstallation; }
        var moduleAfter = await moduleSnapshot(targetInstallation, token);
        var nominee = Path.Combine(target.PrivateDirectory, "replacement-nominee");
        var authorization = Path.Combine(target.PrivateDirectory, "refused-authorization");
        PrivateTestFiles.WriteNew(nominee, JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = "1",
            issuer = Issuer,
            subject = "replacement-idp-identity"
        }));
        bool reassignmentRefused;
        await using (var refused = target.Start(
            ["--identity", "bootstrap", "authorize", "--nominee-file", nominee, "--authorization-file", authorization],
            "Production", configure: Configure))
        {
            var output = await refused.WaitAsync(token);
            reassignmentRefused = output.ExitCode != 0 && !File.Exists(authorization) &&
                output.Stderr.Contains("identity_enrollment_unavailable", StringComparison.Ordinal) &&
                refused.ExitVerified && refused.DescendantExitsVerified;
        }
        var originalClient = client;
        Guid? freshUser = null;
        var freshDesignation = false;
        var cookieRefused = false;
        try
        {
            client = new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
            { BaseAddress = target.HostUrl, Timeout = TimeSpan.FromSeconds(6) };
            await using var host = target.Start(["--fixture-product-login"], "Production", configure: Configure);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            while (true)
            {
                if (host.HasExited) throw new InvalidOperationException("identity_fixture_restored_host_failed");
                try
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
                    if (oldCookie.Length != 0) request.Headers.Add("Cookie", oldCookie);
                    using var response = await SendProductAsync(request, deadline.Token, includeCookies: false);
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        cookieRefused = true;
                        break;
                    }
                }
                catch (HttpRequestException) { }
                await Task.Delay(5, deadline.Token);
            }
            if (restoredCeremony.State == "consumed")
            {
                using (var staleBrowser = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/browser-context"))
                using (var refused = await SendProductAsync(staleBrowser, token))
                    if (refused.StatusCode != HttpStatusCode.Unauthorized)
                        throw new InvalidOperationException("identity_fixture_restored_stale_browser_not_refused");
                // Reconciliation replaces protection keys; a fresh login needs a fresh browser, not its old binding.
                foreach (Cookie cookie in cookies.GetCookies(configuration.PublicOrigin))
                    cookie.Expired = true;
                using var login = ProductLoginRequest("/identity", await ProductContextAsync(token));
                using var start = await SendProductAsync(login, token);
                using var completed = await CompleteProductAsync(start, token);
                if ((int)completed.StatusCode != 303)
                    throw new InvalidOperationException("identity_fixture_restored_login_failed");
                using var current = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me");
                using var response = await SendProductAsync(current, token);
                using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token));
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new InvalidOperationException("identity_fixture_restored_current_failed");
                freshUser = body.RootElement.GetProperty("userId").GetGuid();
                freshDesignation = body.RootElement.GetProperty("isInitialAdministrator").GetBoolean();
            }
            await host.StopAsync();
            if (!host.ExitVerified || !host.DescendantExitsVerified)
                throw new InvalidOperationException("identity_fixture_restored_cleanup_unverified");
        }
        finally { client.Dispose(); client = originalClient; }
        return new(before, after, ceremony, restoredCeremony, moduleBefore, moduleAfter, exposureRefused,
            interruptedExit, cookieRefused, freshUser, freshDesignation, reassignmentRefused);
    }
}

public sealed record IdentityRecipeDiagnostic(string Code, string Operation, string Provider,
    string Outcome, string Correlation, string Durability);

public sealed class IdentityOperationsCapture : TextWriter
{
    private static IdentityOperationsCapture? current;
    private readonly TextWriter original = Console.Error;
    private readonly object gate = new();
    private readonly StringBuilder logs = new();
    private readonly List<(string Correlation, string Tags)> traces = [];
    private readonly List<string> measurements = [];
    private readonly ActivityListener activities;
    private readonly MeterListener meter = new();
    private bool disposed;
    public override Encoding Encoding => Encoding.UTF8;

    public IdentityOperationsCapture()
    {
        if (Interlocked.CompareExchange(ref current, this, null) is not null)
            throw new InvalidOperationException("identity_recipe_capture_already_owned");
        activities = new()
        {
            ShouldListenTo = source => source.Name == "Kofte.Identity",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                lock (gate) traces.Add((activity.TraceId.ToHexString(),
                    JsonSerializer.Serialize(activity.TagObjects.ToDictionary(item => item.Key, item => item.Value))));
            }
        };
        ActivitySource.AddActivityListener(activities);
        meter.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "Kofte.Identity") listener.EnableMeasurementEvents(instrument);
        };
        meter.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            if (instrument.Name != "kofte.identity.operations" || value != 1)
                throw new InvalidOperationException("identity_recipe_metric_invalid");
            var values = tags.ToArray().ToDictionary(item => item.Key, item => item.Value);
            if (!values.Keys.Order(StringComparer.Ordinal).SequenceEqual(["mode", "operation", "outcome", "provider"]))
                throw new InvalidOperationException("identity_recipe_metric_invalid");
            lock (gate) measurements.Add(JsonSerializer.Serialize(values));
        });
        meter.Start();
        Console.SetError(this);
    }

    internal static void Record(string line) => Volatile.Read(ref current)?.WriteLine(line);

    public override void WriteLine(string? value)
    {
        lock (gate)
        {
            if (logs.Length + (value?.Length ?? 0) > 65_536)
                throw new InvalidOperationException("identity_recipe_capture_limit");
            logs.AppendLine(value);
        }
    }

    public IReadOnlyList<IdentityRecipeDiagnostic> Records
    {
        get
        {
            lock (gate)
                return logs.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(line =>
                {
                    var item = Kofte.Infrastructure.Identity.IdentityDiagnostics.Parse(Encoding.UTF8.GetBytes(line));
                    return new IdentityRecipeDiagnostic(item.EventCode, item.Operation, item.Provider,
                        item.Outcome, item.CorrelationId, item.Durability);
                }).ToArray();
        }
    }

    public bool HasCorrelatedChannels(string provider)
    {
        var records = Records;
        lock (gate)
            return records.Count > 0 && traces.Count > 0 && measurements.Count == records.Count &&
                records.All(record => record.Provider == provider &&
                    traces.Any(trace => trace.Correlation == record.Correlation)) &&
                traces.All(trace => records.Any(record => record.Correlation == trace.Correlation));
    }

    public bool IsPrivate(IEnumerable<string> canaries)
    {
        lock (gate)
        {
            var captured = logs + "\n" + string.Join('\n', traces.Select(item => item.Tags)) +
                "\n" + string.Join('\n', measurements);
            return canaries.Where(value => value.Length > 0).All(value =>
                !captured.Contains(value, StringComparison.Ordinal));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposed) return;
        Console.SetError(original);
        Interlocked.CompareExchange(ref current, null, this);
        activities.Dispose();
        meter.Dispose();
        lock (gate) { logs.Clear(); traces.Clear(); measurements.Clear(); }
        disposed = true;
        base.Dispose(disposing);
    }
}
