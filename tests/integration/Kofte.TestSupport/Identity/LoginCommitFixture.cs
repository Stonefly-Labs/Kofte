using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Kofte.Host.Bootstrap;
using Kofte.Host.Features.Identity;
using Kofte.Host.Features.Identity.Login;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Postgres;
using Kofte.Platform.Modules.Storage;
using Kofte.Platform.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using StoreLease = Kofte.Infrastructure.Storage.StoreLease;
using UnixStorePaths = Kofte.Infrastructure.Storage.UnixStorePaths;
using static Kofte.Infrastructure.Identity.IdentityValues;

namespace Kofte.TestSupport.Identity;

public enum LoginCommitFault { None, BeforeCommit, AfterCommit, CookiePublication }

public sealed record LoginCommitResponse(int Status, Guid AttemptId, bool SessionCookieIssued,
    bool CookieContainsOnlyReference, bool CookieHasRequiredFlags, string Reference);

public sealed record LoginUserObservation(Guid Id, string Mode, string? DisplayName, DateTime CreatedAt, DateTime UpdatedAt);
public sealed record LoginMappingObservation(string Mode, string Issuer, string Subject, Guid UserId, DateTime CreatedAt);
public sealed record LoginAttemptObservation(Guid Id, string State, Guid Epoch);
public sealed record LoginSessionObservation(string Digest, Guid UserId, string Mode, Guid Epoch, byte[] ProtectedTicket,
    DateTime CreatedAt, DateTime IdleExpiresAt, DateTime AbsoluteExpiresAt, string State, DateTime LastSeenAt, DateTime? TerminalAt);
public sealed record LoginSessionProbe(int Status, Guid? UserId, string? Mode, bool IsInitialAdministrator);
public sealed record LoginCommitCrash(Guid AttemptId, bool AcknowledgementLost, bool ExitVerified);
public sealed record LoginCapacitySnapshot(long Revision, long Users, long Sessions, long LiveSessions,
    string OriginalDigest, string OriginalState, string AttemptState, IReadOnlyList<LoginEventObservation> Events);
public sealed record LoginEventObservation(Guid Id, long Revision, string Code, string Correlation, Guid? UserId);
public sealed record LoginBootstrapObservation(Guid InstallationId, string State, long Generation,
    string? Issuer, string? Subject, string? Digest, DateTime? Expires, Guid? InitialUser,
    DateTime? ConsumedAt, long? ConsumedGeneration);
public sealed record LoginCommitSnapshot(long Revision, Guid Epoch, long RegistryRevision, string BootstrapState,
    IReadOnlyList<LoginUserObservation> Users, IReadOnlyList<LoginMappingObservation> Mappings,
    IReadOnlyList<LoginAttemptObservation> Attempts, IReadOnlyList<LoginSessionObservation> Sessions,
    IReadOnlyList<LoginEventObservation> Events);

// This owned host exercises real native OIDC and cookie handlers.
public sealed class LoginCommitFixture : IAsyncDisposable
{
    private readonly CookieContainer cookies = new();
    private readonly IHttpContextAccessor accessor = new HttpContextAccessor();
    private StorageProviderScope? scope;
    private OidcIssuer issuer = null!;
    private ServingIdentity? serving;
    private IdentityConfiguration configuration = null!;
    private IdentityKeys? keys;
    private StorageInstallation installation = null!;
    private StoreLease? fileLease;
    private NpgsqlConnection? hostLease;
    private IdentityStore store = null!;
    private WebApplication? application;
    private HttpClient client = null!, providerClient = null!;
    private FaultObserver? observer;
    private OwnedProcess? worker;
    private OwnedProcess? webWorker;
    private IdentityBrowserIngress? browserIngress;
    private bool browserCompleted = true;
    private X509Certificate2? workerTrust;
    private int backendPort;
    private TimeProvider clock = TimeProvider.System;
    private LoginCommitFault fault;
    private Guid lastAttempt;
    private AuthenticationTicket? lastTicket;
    private bool reusedRefused;
    private string? capacityOriginal;
    private bool productRoutes;
    private bool failEnrollmentCommit;
    private string? productCallback;
    private static readonly string Browser = new('b', 64);

    public string Issuer => issuer.Issuer.AbsoluteUri;
    public Uri PublicOrigin => configuration.PublicOrigin;
    public OidcIssuer Protocol => issuer;
    public Func<string, CancellationToken, Task<IPAddress[]>>? ProtocolDns { get; set; }

    public static async Task<LoginCommitFixture> CreateAsync(string provider, CancellationToken token,
        TimeProvider? localClock = null, bool productRoutes = false, bool withBrowser = false,
        Func<StorageProviderScope, CancellationToken, Task>? prepareStorage = null, bool development = false)
    {
        var fixture = new LoginCommitFixture { clock = localClock ?? TimeProvider.System, productRoutes = productRoutes };
        try
        {
            fixture.scope = await StorageProviderScope.CreateAsync(provider, token);
            fixture.issuer = await OidcIssuer.CreateAsync(token);
            if (withBrowser)
            {
                if (!productRoutes) throw new InvalidOperationException("identity_fixture_product_required");
                fixture.browserIngress = await IdentityBrowserIngress.CreateAsync(fixture.issuer.ServerCertificate, token);
            }
            if (development && (withBrowser || !productRoutes))
                throw new InvalidOperationException("identity_fixture_development_scope_invalid");
            var environment = development ? "Development" : "Production";
            fixture.serving = new(environment, development ? null : fixture.issuer, fixture.browserIngress?.Origin);
            await fixture.serving.PrepareAsync(start => fixture.scope.ConfigureChild(start, offlineMaintenance: true), token);
            var bootstrap = new BootstrapConfiguration(provider == "sqlite" ? "local-sqlite" : "test-postgres",
                new("http://127.0.0.1:5080"), fixture.serving.WebUrl, new("http://127.0.0.1:5080"),
                fixture.scope.DataRoot, fixture.scope.Postgres?.ConnectionString, false, null)
            {
                AuthenticationConfigurationFile = fixture.serving.ConfigurationPath
            };
            fixture.configuration = IdentityConfiguration.Load(bootstrap, environment);
            fixture.installation = fixture.scope.Postgres is { } postgres
                ? StorageInstallation.PostgreSql(postgres.ConnectionString)
                : StorageInstallation.Sqlite(fixture.scope.DataRoot);
            if (prepareStorage is not null) await prepareStorage(fixture.scope, fixture.scope.Token);
            await fixture.OpenOwnerAsync(token);
            await fixture.StartAsync(token);
            if (withBrowser) await fixture.StartBrowserAsync(token);
            return fixture;
        }
        catch { await fixture.DisposeAsync(); throw; }
    }

    public static Task<LoginCommitFixture> CreateProductAsync(string provider, CancellationToken token) =>
            CreateAsync(provider, token, productRoutes: true);

    public async Task<Guid> CreateStoredDevelopmentSessionAsync(CancellationToken token)
    {
        if (configuration.Mode != IdentityMode.Development || !productRoutes || application is null)
            throw new InvalidOperationException("identity_fixture_development_required");
        using var services = application.Services.CreateScope();
        var context = new DefaultHttpContext { RequestServices = services.ServiceProvider, RequestAborted = token };
        var tickets = services.ServiceProvider.GetRequiredService<IdentityTicketStore>();
        var reference = IdentityTicketStore.NewReference();
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), token);
        var committed = await store.MutateAsync(budget, async change =>
        {
            var now = clock.GetUtcNow().UtcDateTime;
            var user = await IdentityUserStore.ResolveAsync(change, "kofte:development:v1", "synthetic", null, now);
            var ticket = IdentityTicketStore.CreateTicket(user, now);
            await IdentitySessionStore.CreateAsync(change, user, reference.Digest, tickets.Protect(ticket), now);
            return (User: user, Ticket: ticket);
        });
        IdentityTicketStore.BindCommitted(context, reference.Reference, committed.Ticket);
        await context.SignInAsync(OidcConfiguration.SessionScheme, committed.Ticket.Principal, committed.Ticket.Properties);
        foreach (var value in context.Response.Headers.SetCookie)
            cookies.SetCookies(configuration.PublicOrigin, value!);
        return committed.User.UserId;
    }

    public async Task RunBrowserAsync(Guid expectedUser, CancellationToken token, string? authorization = null)
    {
        if (browserIngress is null || webWorker is null || webWorker.HasExited || expectedUser == Guid.Empty)
            throw new InvalidOperationException("identity_fixture_browser_required");
        var root = Path.GetDirectoryName(serving!.ConfigurationPath)!;
        var authorityFile = Path.Combine(root, "browser-ca.pem");
        PrivateTestFiles.WriteNew(authorityFile, Encoding.ASCII.GetBytes(issuer.AuthorityCertificatePem));
        var start = NodeStart(Path.Combine("tests", "e2e", "identity.production.ts"));
        start.Environment["KOFTE_IDENTITY_WEB_URL"] = browserIngress.Origin.AbsoluteUri;
        start.Environment["KOFTE_IDENTITY_PRIVATE_ROOT"] = root;
        start.Environment["KOFTE_IDENTITY_AUTHORITY_FILE"] = authorityFile;
        start.Environment["KOFTE_IDENTITY_EXPECTED_USER"] = expectedUser.ToString("D");
        if (authorization is not null)
        {
            if (!IdentityBootstrapStore.ValidAuthorization(authorization))
                throw new InvalidOperationException("identity_fixture_private_delivery_invalid");
            var authorizationFile = Path.Combine(root, "browser-enrollment");
            PrivateTestFiles.WriteNew(authorizationFile, Encoding.ASCII.GetBytes(authorization));
            start.Environment["KOFTE_IDENTITY_ENROLLMENT_FILE"] = authorizationFile;
            browserIngress.LoseNextEnrollmentAcknowledgement();
        }
        browserCompleted = false;
        var providerRequests = issuer.Observations.Count;
        await using var child = OwnedProcesses.Start(start);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(45));
        ProcessOutput result;
        var timedOut = false;
        try { result = await child.WaitAsync(deadline.Token); }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested && !token.IsCancellationRequested)
        {
            timedOut = true;
            await child.StopAsync();
            result = await child.ResultAsync();
        }
        if (timedOut || result.ExitCode != 0 ||
            !result.Stdout.Split('\n', StringSplitOptions.TrimEntries)
                .Contains("identity_browser_stage_owned_browser_closed", StringComparer.Ordinal))
        {
            var observed = issuer.Observations.Skip(providerRequests).ToArray();
            Console.Error.WriteLine(JsonSerializer.Serialize(new
            {
                eventCode = "identity_browser_protocol_observation",
                authorizationRequests = observed.Count(value => value == "authorization"),
                tokenRequests = observed.Count(value => value == "token"),
                browserDeadlineExceeded = timedOut,
                responseStatuses = browserIngress.ResponseStatuses
            }));
            if (browserIngress.OpaqueLoginOriginObserved)
                throw new InvalidOperationException("identity_browser_opaque_origin_refused");
            var output = Regex.Replace(result.Stdout + "\n" + result.Stderr, @"\x1b\[[0-9;]*m", "");
            var failure = Regex.Match(output,
                @"\bidentity_browser_(?:(?:page|trust)_line_[1-9][0-9]{0,3}|(?!stage_)[a-z_]{1,70})\b");
            if (failure.Success) throw new InvalidOperationException(failure.Value);
            var location = Regex.Match(output,
                @"\bat [^\r\n]*\b(identity\.production|identityBrowser)\.ts:(\d{1,4}):\d+");
            if (location.Success)
                throw new InvalidOperationException("identity_browser_" +
                    (location.Groups[1].Value == "identity.production" ? "page" : "trust") +
                    "_line_" + location.Groups[2].Value);
            var stage = Regex.Matches(output,
                @"\bidentity_browser_stage_[a-z_]{1,40}\b").LastOrDefault();
            if (stage is not null) throw new InvalidOperationException(stage.Value);
            throw new InvalidOperationException("identity_fixture_browser_failed");
        }
        if (authorization is not null && !browserIngress.EnrollmentAcknowledgementLost)
            throw new InvalidOperationException("identity_browser_enrollment_acknowledgement_not_lost");
        browserCompleted = true;
    }

    private async Task StartBrowserAsync(CancellationToken token)
    {
        var start = NodeStart(Path.Combine("node_modules", "next", "dist", "bin", "next"));
        start.WorkingDirectory = Path.Combine(OwnedProcesses.FindRepository(), "apps", "web");
        start.ArgumentList.Add("start");
        start.ArgumentList.Add("--hostname");
        start.ArgumentList.Add("127.0.0.1");
        start.ArgumentList.Add("--port");
        start.ArgumentList.Add(browserIngress!.Web.Port.ToString(CultureInfo.InvariantCulture));
        start.Environment["NODE_ENV"] = "production";
        start.Environment["NEXT_TELEMETRY_DISABLED"] = "1";
        start.Environment["KOFTE_AUTH_CONFIG_FILE"] = serving!.ConfigurationPath;
        start.Environment["KOFTE_BACKEND_ORIGIN"] = client.BaseAddress!.AbsoluteUri;
        start.Environment["KOFTE_HOST_URL"] = client.BaseAddress.AbsoluteUri;
        start.Environment["KOFTE_WEB_URL"] = browserIngress.Web.AbsoluteUri;
        webWorker = OwnedProcesses.Start(start);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        using var probe = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        { Timeout = TimeSpan.FromSeconds(1) };
        while (true)
        {
            deadline.Token.ThrowIfCancellationRequested();
            if (webWorker.HasExited) throw new InvalidOperationException("identity_fixture_web_exited");
            try
            {
                using var response = await probe.GetAsync(new Uri(browserIngress.Web, "/identity"), deadline.Token);
                if (response.StatusCode == HttpStatusCode.OK) return;
            }
            catch (Exception error) when (error is HttpRequestException or
                OperationCanceledException && !deadline.IsCancellationRequested)
            { }
            await Task.Delay(25, deadline.Token);
        }
    }

    private static ProcessStartInfo NodeStart(string relative)
    {
        var root = OwnedProcesses.FindRepository();
        var start = new ProcessStartInfo("node")
        {
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        OwnedProcesses.ClearSensitiveEnvironment(start);
        start.ArgumentList.Add(Path.Combine(root, relative));
        return start;
    }

    public int ProductProviderRequests => issuer.Observations.Count;
    public string? ProductFailure { get; private set; }

    public async Task<HttpResponseMessage> SendProductAsync(HttpRequestMessage request, CancellationToken token,
        bool includeCookies = true)
    {
        var origin = new Uri(configuration.PublicOrigin, request.RequestUri!);
        if (includeCookies)
        {
            var cookie = cookies.GetCookieHeader(origin);
            if (cookie.Length != 0) request.Headers.Add("Cookie", cookie);
        }
        var response = await client.SendAsync(request, token);
        if (response.Headers.TryGetValues("Set-Cookie", out var values))
            foreach (var value in values) cookies.SetCookies(origin, value);
        return response;
    }

    public async Task<string> ProductContextAsync(CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/browser-context");
        using var response = await SendProductAsync(request, token);
        if ((int)response.StatusCode != 200)
            throw new InvalidOperationException("identity_fixture_browser_context_failed");
        using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token));
        return body.RootElement.GetProperty("csrfToken").GetString()!;
    }

    public HttpRequestMessage ProductLoginRequest(string? returnPath, string csrf, bool origin = true)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/auth/login")
        {
            Content = new FormUrlEncodedContent(returnPath is null ? [] :
                new[] { new KeyValuePair<string, string>("returnPath", returnPath) })
        };
        request.Headers.Add("X-Kofte-CSRF", csrf);
        if (origin) request.Headers.Add("Origin", configuration.PublicOrigin.OriginalString);
        return request;
    }

    public async Task<HttpResponseMessage> CompleteProductAsync(HttpResponseMessage start, CancellationToken token)
    {
        if ((int)start.StatusCode != 303 || start.Headers.Location is null)
            throw new InvalidOperationException("identity_fixture_product_challenge_failed");
        using var authorization = await providerClient.GetAsync(start.Headers.Location, token);
        if ((int)authorization.StatusCode != 302 || authorization.Headers.Location is null)
            throw new InvalidOperationException("identity_fixture_product_authorization_failed");
        productCallback = authorization.Headers.Location.PathAndQuery;
        using var request = new HttpRequestMessage(HttpMethod.Get, productCallback);
        return await SendProductAsync(request, token);
    }

    public async Task<HttpResponseMessage> ReplayProductCallbackAsync(CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, productCallback ??
            throw new InvalidOperationException("identity_fixture_callback_missing"));
        return await SendProductAsync(request, token);
    }

    public async Task<HttpResponseMessage> ProductCallbackWithoutBrowserBindingAsync(HttpResponseMessage start,
        CancellationToken token)
    {
        using var authorization = await providerClient.GetAsync(start.Headers.Location, token);
        using var request = new HttpRequestMessage(HttpMethod.Get, authorization.Headers.Location!.PathAndQuery);
        request.Headers.Add("Cookie", string.Join("; ", cookies.GetCookies(configuration.PublicOrigin)
            .Cast<Cookie>().Where(cookie => cookie.Name != "__Host-kofte-browser")
            .Select(cookie => cookie.Name + "=" + cookie.Value)));
        return await SendProductAsync(request, token, includeCookies: false);
    }
    private async Task OpenOwnerAsync(CancellationToken token)
    {
        using var budget = new OperationBudget(OperationBudget.Register, token);
        if (installation.Provider == "sqlite") fileLease = StoreLease.Acquire(installation.Root!);
        else
        {
            hostLease = (NpgsqlConnection)await installation.OpenPlatformAsync(budget);
            await using var command = new NpgsqlCommand(
                "SELECT pg_catalog.pg_try_advisory_lock_shared($1)", hostLease);
            command.Parameters.AddWithValue(PostgresIdentity.LeaseKey);
            if (await command.ExecuteScalarAsync(token) is not true)
                throw new InvalidOperationException("identity_fixture_lease_failed");
        }
        await using var database = await DeadlineStore.OpenAsync(installation, budget);
        var metadata = await IdentityStore.ReadMetadataAsync(database);
        var mode = configuration.Mode == IdentityMode.Oidc ? "oidc" : "development";
        await IdentityPreparation.VerifyAsync(installation, metadata.InstallationId,
            mode, configuration.TrustDigest, budget);
        store = new(installation, metadata.InstallationId, mode, configuration.TrustDigest, fileLease, hostLease);
        keys = IdentityKeys.Open(configuration, metadata.InstallationId, budget);
    }

    public static async Task<int> RunExistingChildAsync(bool productRoutes = false)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        await using var fixture = new LoginCommitFixture { productRoutes = productRoutes };
        var bootstrap = BootstrapConfiguration.FromEnvironment();
        fixture.configuration = IdentityConfiguration.Load(bootstrap, "Production");
        fixture.installation = bootstrap.PostgresConnection is { } connection
            ? StorageInstallation.PostgreSql(connection) : StorageInstallation.Sqlite(bootstrap.DataRoot);
        var directory = Path.GetDirectoryName(bootstrap.AuthenticationConfigurationFile!)!;
        UnixStorePaths.ValidateRoot(directory, allowMissing: false);
        var authority = Path.Combine(directory, "issuer-ca.pem");
        UnixStorePaths.ValidateFile(authority);
        if (new FileInfo(authority).Length > 16_384)
            throw new InvalidOperationException("identity_fixture_trust_invalid");
        fixture.workerTrust = X509Certificate2.CreateFromPem(await File.ReadAllTextAsync(authority, timeout.Token));
        fixture.backendPort = bootstrap.HostUrl.Port;
        await fixture.OpenOwnerAsync(timeout.Token);
        await fixture.StartAsync(timeout.Token);
        await fixture.application!.WaitForShutdownAsync(timeout.Token);
        return 0;
    }

    private async Task StartAsync(CancellationToken token)
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions
        {
            EnvironmentName = configuration.Mode == IdentityMode.Oidc ? "Production" : "Development",
            ContentRootPath = Path.GetDirectoryName(serving?.ConfigurationPath ?? configuration.KeyRingDirectory)
        });
        builder.Configuration.Sources.Clear();
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(server => server.Listen(IPAddress.Loopback, backendPort));
        builder.Services.AddSingleton(this);
        builder.Services.AddSingleton(configuration);
        builder.Services.AddSingleton(store);
        builder.Services.AddSingleton<IDataProtectionProvider>(keys!.Provider);
        builder.Services.AddSingleton(accessor);
        builder.Services.AddSingleton(clock);
        builder.Services.AddSingleton<IdentityTicketStore>();
        builder.Services.AddScoped<ValidatedLogin>();
        if (configuration.Provider is { } provider)
            builder.Services.AddSingleton(services => new OidcBackchannel(provider,
                services.GetRequiredService<IHttpContextAccessor>(), workerTrust is { } authority
                    ? OidcIssuer.CreateSocketsHandler(authority) : issuer.CreateSocketsHandler(), ResolveProtocolAsync));
        if (productRoutes)
        {
            builder.Services.AddIdentityBoundary(configuration);
            IdentityLoginRegistration.Add(builder.Services, configuration);
            if (configuration.Provider is { } configuredProvider)
                builder.Services.AddSingleton(services => new OidcBackchannel(configuredProvider,
                    services.GetRequiredService<IHttpContextAccessor>(), workerTrust is { } authority
                        ? OidcIssuer.CreateSocketsHandler(authority) : issuer.CreateSocketsHandler(), ResolveProtocolAsync));
        }
        else
        {
            builder.Services.AddAuthentication().AddCookie(OidcConfiguration.SessionScheme, _ => { })
            .AddOpenIdConnect(OidcConfiguration.ProviderScheme, _ => { });
            builder.Services.AddOptions<CookieAuthenticationOptions>(OidcConfiguration.SessionScheme)
                .Configure<IdentityTicketStore>((options, tickets) =>
                {
                    options.SessionStore = tickets;
                    options.Cookie.Name = "__Host-kofte-session";
                    options.Cookie.Path = "/";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.SlidingExpiration = false;
                    options.TimeProvider = clock;
                    options.CookieManager = new FaultCookies(this);
                });
            builder.Services.AddOptions<OpenIdConnectOptions>(OidcConfiguration.ProviderScheme)
                .Configure<OidcBackchannel>((options, backchannel) =>
                {
                    OidcConfiguration.Apply(options, configuration.Provider!, configuration.PublicOrigin, backchannel);
                    options.Events.OnTicketReceived = async context =>
                    {
                        context.HandleResponse();
                        using var budget = new OperationBudget(TimeSpan.FromSeconds(4), context.HttpContext.RequestAborted);
                        var result = await context.HttpContext.RequestServices.GetRequiredService<ValidatedLogin>()
                            .CompleteOidcAsync(context.HttpContext,
                                context.Principal ?? throw new InvalidOperationException("identity_fixture_principal_missing"),
                                context.Properties, Browser, budget);
                        lastTicket = result.Ticket;
                        reusedRefused = await RefusesAsync(() => context.HttpContext.RequestServices
                            .GetRequiredService<IdentityTicketStore>().StoreAsync(result.Ticket, context.HttpContext, token));
                        context.Response.StatusCode = 303;
                        context.Response.Headers.Location = result.ReturnPath;
                    };
                    options.Events.OnRemoteFailure = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    };
                });
        }
        application = builder.Build();
        application.Use(async (context, next) =>
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
            timeout.CancelAfter(TimeSpan.FromSeconds(4));
            context.RequestAborted = timeout.Token;
            try { await next(context); }
            catch (CommitInterruption) { context.Response.StatusCode = 503; }
            catch (IdentityAdmissionException) { context.Response.StatusCode = 429; }
            catch (StorageException) { context.Response.StatusCode = 503; }
            catch (CryptographicException) { context.Response.StatusCode = 503; }
            catch (Exception error) when (productRoutes)
            {
                ProductFailure = error.GetType().Name + ":" + string.Join(",", new StackTrace(error).GetFrames()
                    .Take(8).Select(frame => frame.GetMethod()?.DeclaringType?.Name + "." + frame.GetMethod()?.Name));
                throw;
            }
        });
        if (productRoutes)
        {
            application.UseIdentityBoundary();
            application.MapIdentityEndpoints();
            application.MapGet("/fixture-actor", (ICurrentActorV1 current) =>
                Results.Json(new
                {
                    userId = current.Actor!.UserId,
                    identityMode = current.Actor.IdentityMode == IdentityMode.Oidc ? "oidc" : "development",
                    isInitialAdministrator = current.Actor.IsInitialAdministrator
                })).RequireAuthorization(IdentityRegistration.CurrentIdentityPolicy);
            application.MapGet("/fixture-ungranted", () => Results.NoContent());
        }
        else
        {
            application.UseAuthentication();
            application.MapGet("/start", ChallengeAsync);
            application.MapGet("/probe", ProbeSessionAsync);
            application.MapGet("/fixture-live", () => Results.NoContent());
        }
        observer = new(this);
        await application.StartAsync(token);
        client = new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
        {
            BaseAddress = new(application.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single()),
            Timeout = TimeSpan.FromSeconds(6)
        };
        if (issuer is not null)
        {
            providerClient = issuer.CreateClient();
            issuer.RegisteredRedirectUri = new(configuration.PublicOrigin, "/auth/oidc/callback");
        }
    }

    private Task<IPAddress[]> ResolveProtocolAsync(string host, CancellationToken token) =>
        ProtocolDns is { } resolve ? resolve(host, token) : Dns.GetHostAddressesAsync(host, token);

    private async Task ChallengeAsync(HttpContext context)
    {
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), context.RequestAborted);
        var attempt = await store.MutateAsync(budget, change =>
            IdentityLoginAttempts.StartAsync(change, Browser, "/identity", clock.GetUtcNow().UtcDateTime));
        lastAttempt = attempt.AttemptId;
        await context.ChallengeAsync(OidcConfiguration.ProviderScheme, LoginAttemptBinding.Create(attempt));
    }

    private static async Task ProbeSessionAsync(HttpContext context)
    {
        var result = await context.AuthenticateAsync(OidcConfiguration.SessionScheme);
        if (!result.Succeeded)
        {
            context.Response.StatusCode = 401;
            return;
        }
        var view = IdentityTicketStore.Current(context) ??
            throw new InvalidOperationException("identity_fixture_verified_session_missing");
        await context.Response.WriteAsJsonAsync(new
        {
            userId = view.User.UserId,
            mode = view.User.Mode,
            isInitialAdministrator = view.IsInitialAdministrator
        }, context.RequestAborted);
    }

    public async Task<LoginSessionProbe> ProbeAsync(CancellationToken token)
    {
        using var response = await SendAsync("/probe", token);
        if ((int)response.StatusCode != 200) return new((int)response.StatusCode, null, null, false);
        using var body = JsonDocument.Parse(await response.Content.ReadAsByteArrayAsync(token));
        return new(200, body.RootElement.GetProperty("userId").GetGuid(),
            body.RootElement.GetProperty("mode").GetString(),
            body.RootElement.GetProperty("isInitialAdministrator").GetBoolean());
    }

    public Task RevokeCurrentAsync(CancellationToken token)
    {
        var reference = CurrentCookieTicket().Principal.Claims.Single().Value;
        return application!.Services.GetRequiredService<IdentityTicketStore>()
            .RemoveAsync(reference, new DefaultHttpContext(), token);
    }

    public async Task CorruptCurrentTicketAsync(CancellationToken token)
    {
        var session = (await SnapshotAsync(token)).Sessions.Single();
        var corrupt = session.ProtectedTicket.ToArray();
        corrupt[^1] ^= 0xff;
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), token);
        await using var database = await DeadlineStore.OpenAsync(installation, budget);
        await database.BeginWriteAsync();
        await database.QueryAsync($"""
            UPDATE {database.Prefix}__kofte_sessions SET protected_ticket=@ticket WHERE reference_digest=@digest
            """, ("ticket", corrupt), ("digest", session.Digest));
        await database.CommitAsync();
    }

    public async Task SeedCurrentProfileAndDesignationAsync(CancellationToken token)
    {
        var user = (await SnapshotAsync(token)).Users.Single();
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), token);
        await using var database = await DeadlineStore.OpenAsync(installation, budget);
        await database.BeginWriteAsync();
        var now = clock.GetUtcNow().UtcDateTime;
        await database.QueryAsync($"""
            UPDATE {database.Prefix}__kofte_users SET display_name=@display,updated_at_utc=@now WHERE user_id=@user
            """, ("display", "<changed-profile>"), ("now", now), ("user", user.Id));
        await database.QueryAsync($"""
            UPDATE {database.Prefix}__kofte_bootstrap SET state='consumed',generation=1,initial_user_id=@user,
            consumed_at_utc=@now,consumed_generation=1 WHERE singleton_key=1 AND state='disabled'
            """, ("user", user.Id), ("now", now));
        await database.CommitAsync();
    }

    public async Task SeedPendingBootstrapAsync(string subject, bool expired, CancellationToken token)
    {
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), token);
        await using var database = await DeadlineStore.OpenAsync(installation, budget);
        await database.BeginWriteAsync();
        var metadata = await IdentityStore.ReadMetadataAsync(database);
        var secret = new string('A', 43);
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"kofte.identity.bootstrap.authorization.v1\0{metadata.InstallationId:D}\0" +
            $"1\0{Issuer}\0{subject}\0{secret}")));
        await database.QueryAsync($"""
            UPDATE {database.Prefix}__kofte_bootstrap SET state='pending',generation=1,nominee_issuer=@issuer,
              nominee_subject=@subject,authorization_digest=@digest,expires_at_utc=@expires WHERE singleton_key=1
            """, ("issuer", Issuer), ("subject", subject), ("digest", digest),
            ("expires", clock.GetUtcNow().UtcDateTime.AddMinutes(expired ? -1 : 15)));
        await database.CommitAsync();
    }

    public void FailEnrollmentBeforeCommit() => failEnrollmentCommit = true;

    public async Task SeedRecoveryLockedBootstrapAsync(CancellationToken token)
    {
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), token);
        await using var database = await DeadlineStore.OpenAsync(installation, budget);
        await database.BeginWriteAsync();
        await database.QueryAsync(
            $"UPDATE {database.Prefix}__kofte_bootstrap SET state='recovery_locked' WHERE singleton_key=1");
        await database.CommitAsync();
    }

    public async Task SeedGlobalCapacityAsync(CancellationToken token)
    {
        capacityOriginal = (await SnapshotAsync(token)).Sessions.Single().Digest;
        using var cancel = CancellationTokenSource.CreateLinkedTokenSource(token, scope!.Token);
        using var budget = new OperationBudget(TimeSpan.FromSeconds(90), cancel.Token);
        await using var database = await DeadlineStore.OpenAsync(installation, budget);
        await database.BeginWriteAsync();
        var metadata = await IdentityStore.ReadMetadataAsync(database);
        var tickets = application!.Services.GetRequiredService<IdentityTicketStore>();
        var now = clock.GetUtcNow().UtcDateTime;
        var remaining = 49_999;
        for (var index = 0; remaining != 0; index++)
        {
            budget.Check();
            var user = new IdentityUser(Guid.NewGuid(), "oidc", null, now, now);
            await database.QueryAsync($"""
                INSERT INTO {database.Prefix}__kofte_users (user_id,mode,created_at_utc,updated_at_utc)
                VALUES (@id,'oidc',@now,@now)
                """, ("id", user.UserId), ("now", now));
            await database.QueryAsync($"""
                INSERT INTO {database.Prefix}__kofte_external_identities (mode,issuer,subject,user_id,created_at_utc)
                VALUES ('oidc',@issuer,@subject,@id,@now)
                """, ("issuer", Issuer), ("subject", "capacity-seed-" + index.ToString(CultureInfo.InvariantCulture)),
                ("id", user.UserId), ("now", now));
            var count = Math.Min(8, remaining);
            var parameters = new List<(string Name, object? Value)>
            {
                ("user", user.UserId), ("epoch", metadata.Epoch),
                ("ticket", tickets.Protect(IdentityTicketStore.CreateTicket(user, now))),
                ("now", now), ("idle", now.AddMinutes(30)), ("absolute", now.AddHours(8))
            };
            for (var session = 0; session < count; session++)
                parameters.Add(("digest" + session.ToString(CultureInfo.InvariantCulture),
                    IdentityTicketStore.NewReference().Digest));
            var values = string.Join(",", Enumerable.Range(0, count).Select(session =>
                $"(@digest{session},@user,'oidc',@epoch,@ticket,@now,@now,@idle,@absolute,'active')"));
            await database.QueryAsync($"""
                INSERT INTO {database.Prefix}__kofte_sessions
                (reference_digest,user_id,mode,session_epoch,protected_ticket,created_at_utc,last_seen_at_utc,
                 idle_expires_at_utc,absolute_expires_at_utc,state) VALUES {values}
                """, parameters.ToArray());
            remaining -= count;
        }
        await database.CommitAsync();
    }

    public async Task<LoginCapacitySnapshot> CapacitySnapshotAsync(CancellationToken token)
    {
        if (capacityOriginal is null) throw new InvalidOperationException("identity_fixture_capacity_not_seeded");
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), token);
        await using var database = await DeadlineStore.OpenAsync(installation, budget);
        var metadata = await IdentityStore.ReadMetadataAsync(database);
        var events = await database.QueryAsync($"""
            SELECT event_id,security_revision,event_code,correlation_id,user_id
            FROM {database.Prefix}__kofte_identity_events ORDER BY event_id
            """);
        return new(metadata.Revision,
            (long)DeadlineStore.AssertScalar(await database.QueryAsync(
                $"SELECT COUNT(*) FROM {database.Prefix}__kofte_users"))!,
            (long)DeadlineStore.AssertScalar(await database.QueryAsync(
                $"SELECT COUNT(*) FROM {database.Prefix}__kofte_sessions"))!,
            (long)DeadlineStore.AssertScalar(await database.QueryAsync($"""
                SELECT COUNT(*) FROM {database.Prefix}__kofte_sessions WHERE state='active'
                AND idle_expires_at_utc>@now AND absolute_expires_at_utc>@now
                """, ("now", clock.GetUtcNow().UtcDateTime)))!,
            capacityOriginal, (string)DeadlineStore.AssertScalar(await database.QueryAsync(
                $"SELECT state FROM {database.Prefix}__kofte_sessions WHERE reference_digest=@digest",
                ("digest", capacityOriginal)))!,
            (string)DeadlineStore.AssertScalar(await database.QueryAsync(
                $"SELECT state FROM {database.Prefix}__kofte_login_attempts"))!,
            events.Select(row => new LoginEventObservation(Id(row[0]), (long)row[1]!, (string)row[2]!,
                (string)row[3]!, row[4] is null ? null : Id(row[4]))).ToArray());
    }

    private AuthenticationTicket CurrentCookieTicket()
    {
        var cookie = cookies.GetCookies(configuration.PublicOrigin)["__Host-kofte-session"] ??
            throw new InvalidOperationException("identity_fixture_session_cookie_missing");
        return application!.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(OidcConfiguration.SessionScheme).TicketDataFormat.Unprotect(cookie.Value) ??
            throw new InvalidOperationException("identity_fixture_cookie_invalid");
    }

    public async Task<LoginCommitResponse> LoginAsync(LoginCommitFault selectedFault, CancellationToken token)
    {
        fault = selectedFault;
        using var start = await SendAsync("/start", token);
        if ((int)start.StatusCode != 302 || start.Headers.Location is null)
            throw new InvalidOperationException("identity_fixture_challenge_failed");
        if (worker is not null) lastAttempt = (await SnapshotAsync(token)).Attempts.Single().Id;
        using var authorization = await providerClient.GetAsync(start.Headers.Location, token);
        if ((int)authorization.StatusCode != 302 || authorization.Headers.Location is null)
            throw new InvalidOperationException("identity_fixture_authorization_failed");
        using var response = await SendAsync(authorization.Headers.Location.PathAndQuery, token);
        var issued = response.Headers.TryGetValues("Set-Cookie", out var headers)
            ? headers.SingleOrDefault(value => value.StartsWith("__Host-kofte-session=", StringComparison.Ordinal)) : null;
        if (issued is null) return new((int)response.StatusCode, lastAttempt, false, false, false, "");
        var cookie = cookies.GetCookies(configuration.PublicOrigin)["__Host-kofte-session"] ??
            throw new InvalidOperationException("identity_fixture_session_cookie_missing");
        var ticket = CurrentCookieTicket();
        var claims = ticket.Principal.Claims.ToArray();
        var opaque = claims.Length == 1 &&
            claims[0].Type == "Microsoft.AspNetCore.Authentication.Cookies-SessionId" &&
            ticket.Properties.Items.Count == 0;
        var flags = cookie.Secure && cookie.HttpOnly && cookie.Path == "/" &&
            issued.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase) &&
            !issued.Contains("domain=", StringComparison.OrdinalIgnoreCase);
        return new((int)response.StatusCode, lastAttempt, true, opaque, flags, opaque ? claims[0].Value : "");
    }

    public async Task<LoginCommitCrash> CrashLoginAsync(string phase, CancellationToken token)
    {
        if (worker is not null || phase is not ("login_before_commit" or "login_committed"))
            throw new InvalidOperationException("identity_fixture_crash_invalid");
        var port = client.BaseAddress!.Port;
        using (var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            await application!.StopAsync(stop.Token);
        await application!.DisposeAsync();
        application = null;
        fileLease?.Dispose();
        fileLease = null;
        if (hostLease is not null) await hostLease.DisposeAsync();
        hostLease = null;
        var directory = Path.GetDirectoryName(serving!.ConfigurationPath)!;
        var barrier = new IdentityCommitBarrier(directory, phase);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        await StartWorkerAsync("--fixture-login-bridge", port, deadline.Token, barrier);
        var callback = LoginAsync(LoginCommitFault.None, token);
        await barrier.WaitAsync(deadline.Token);
        if (barrier.ObservedProcessId != worker!.Id)
            throw new InvalidOperationException("identity_fixture_child_ownership_invalid");
        await worker.StopAsync(graceful: false);
        try { await callback; }
        catch (HttpRequestException) { return new(lastAttempt, true, worker.HasExited); }
        throw new InvalidOperationException("identity_fixture_callback_acknowledged");
    }

    public async Task<int> RestartProductAsync(CancellationToken token)
    {
        await StopProductAsync(token);
        await StartWorkerAsync("--fixture-product-login", client.BaseAddress!.Port, token);
        return worker!.Id;
    }

    public async Task<LoginCommitCrash> CrashEnrollmentAsync(string phase, string authorization, CancellationToken token)
    {
        if (phase is not ("enrollment_before_commit" or "enrollment_committed"))
            throw new InvalidOperationException("identity_fixture_crash_invalid");
        await StopProductAsync(token);
        var barrier = new IdentityCommitBarrier(Path.GetDirectoryName(serving!.ConfigurationPath)!, phase);
        await StartWorkerAsync("--fixture-product-login", client.BaseAddress!.Port, token, barrier);
        var csrf = await ProductContextAsync(token);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/first-administrator")
        {
            Content = System.Net.Http.Json.JsonContent.Create(new { authorization })
        };
        request.Headers.Add("Origin", configuration.PublicOrigin.OriginalString);
        request.Headers.Add("X-Kofte-CSRF", csrf);
        var completion = SendProductAsync(request, token);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        await barrier.WaitAsync(deadline.Token);
        if (barrier.ObservedProcessId != worker!.Id)
            throw new InvalidOperationException("identity_fixture_child_ownership_invalid");
        await worker.StopAsync(graceful: false);
        try { using var response = await completion; }
        catch (HttpRequestException) { return new(lastAttempt, true, worker.HasExited); }
        throw new InvalidOperationException("identity_fixture_enrollment_acknowledged");
    }

    public async Task PrepareProductAgainAsync(CancellationToken token)
    {
        await StopProductAsync(token);
        await RunStoppedHostAsync(["--registry", "upgrade"], token);
        await RunIdentityOperatorAsync(["prepare"], token);
        await StartWorkerAsync("--fixture-product-login", client.BaseAddress!.Port, token);
    }

    public async Task<string> NominateProductAsync(CancellationToken token)
    {
        await StopProductAsync(token);
        var directory = Path.GetDirectoryName(serving!.ConfigurationPath)!;
        var nominee = Path.Combine(directory, "nominee-" + Guid.NewGuid().ToString("N") + ".json");
        var authorization = Path.Combine(directory, "authorization-" + Guid.NewGuid().ToString("N"));
        PrivateTestFiles.WriteNew(nominee, JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = "1",
            issuer = Issuer,
            subject = issuer.Subject
        }));
        var result = await RunIdentityOperatorAsync(
            ["bootstrap", "authorize", "--nominee-file", nominee, "--authorization-file", authorization], token);
        var secret = await File.ReadAllTextAsync(authorization, token);
        if (secret.Length != 43 || new[] { secret, issuer.Subject, Issuer, directory }
            .Any(value => (result.Stdout + result.Stderr).Contains(value, StringComparison.Ordinal)))
            throw new InvalidOperationException("identity_fixture_private_delivery_invalid");
        await StartWorkerAsync("--fixture-product-login", client.BaseAddress!.Port, token);
        return secret;
    }

    public async Task ReconcileProductProviderAsync(CancellationToken token)
    {
        await StopProductAsync(token);
        await issuer.DisposeAsync();
        issuer = await OidcIssuer.CreateAsync(token);
        providerClient.Dispose();
        providerClient = issuer.CreateClient();
        issuer.RegisteredRedirectUri = new(configuration.PublicOrigin, "/auth/oidc/callback");
        var path = serving!.ConfigurationPath;
        var document = JsonNode.Parse(await File.ReadAllTextAsync(path, token))!;
        var provider = document["provider"]!;
        foreach (var (name, relative) in new[]
        {
            ("issuer", ""), ("metadataAddress", ".well-known/openid-configuration"),
            ("authorizationEndpoint", "authorize"), ("tokenEndpoint", "token"), ("jwksUri", "jwks")
        })
            provider[name] = new Uri(issuer.Issuer, relative).AbsoluteUri;
        provider["clientId"] = issuer.ClientId;
        var directory = Path.GetDirectoryName(path)!;
        var secret = Path.Combine(directory, "replacement-secret-" + Guid.NewGuid().ToString("N"));
        PrivateTestFiles.WriteNew(secret, Encoding.ASCII.GetBytes(issuer.ClientSecret));
        provider["clientSecretFile"] = secret;
        provider["allowedEndpoints"] = new JsonArray(new[] { ".well-known/openid-configuration", "authorize", "token", "jwks" }
            .Select(relative => (JsonNode?)JsonValue.Create(new Uri(issuer.Issuer, relative).AbsoluteUri)).ToArray());
        var temporary = path + "." + Guid.NewGuid().ToString("N");
        PrivateTestFiles.WriteNew(temporary, JsonSerializer.SerializeToUtf8Bytes(document));
        File.Move(temporary, path, overwrite: true);
        var authorityFile = Path.Combine(directory, "issuer-ca.pem");
        var authorityTemporary = authorityFile + "." + Guid.NewGuid().ToString("N");
        PrivateTestFiles.WriteNew(authorityTemporary, Encoding.ASCII.GetBytes(issuer.AuthorityCertificatePem));
        File.Move(authorityTemporary, authorityFile, overwrite: true);
        await RunIdentityOperatorAsync(["reconcile-configuration"], token);
        await StartWorkerAsync("--fixture-product-login", client.BaseAddress!.Port, token);
    }

    private Task<ProcessOutput> RunIdentityOperatorAsync(IReadOnlyList<string> arguments, CancellationToken token) =>
        RunStoppedHostAsync(["--identity", .. arguments], token);

    private async Task<ProcessOutput> RunStoppedHostAsync(IReadOnlyList<string> arguments, CancellationToken token)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = OwnedProcesses.FindRepository(),
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        var build = AppContext.BaseDirectory.Contains("/Release/", StringComparison.Ordinal) ? "Release" : "Debug";
        start.ArgumentList.Add(Path.Combine(OwnedProcesses.FindRepository(), "src", "Kofte.Host", "bin", build,
            "net10.0", "Kofte.Host.dll"));
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        scope!.ConfigureChild(start, offlineMaintenance: true);
        serving!.ConfigureChild(start);
        var result = await OwnedProcesses.RunAsync(start, TimeSpan.FromSeconds(120), token);
        if (result.ExitCode != 0) throw new InvalidOperationException("identity_fixture_operator_failed");
        return result;
    }

    private async Task StopProductAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!productRoutes) throw new InvalidOperationException("identity_fixture_product_required");
        if (application is not null)
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await application.StopAsync(stop.Token);
            await application.DisposeAsync();
            application = null;
        }
        if (worker is not null)
        {
            await worker.DisposeAsync();
            worker = null;
        }
        observer?.Dispose();
        observer = null;
        keys?.Dispose();
        keys = null;
        fileLease?.Dispose();
        fileLease = null;
        if (hostLease is not null) await hostLease.DisposeAsync();
        hostLease = null;
    }

    private async Task StartWorkerAsync(string operation, int port, CancellationToken token,
        IdentityCommitBarrier? barrier = null)
    {
        var directory = Path.GetDirectoryName(serving!.ConfigurationPath)!;
        var authorityFile = Path.Combine(directory, "issuer-ca.pem");
        if (!File.Exists(authorityFile))
            PrivateTestFiles.WriteNew(authorityFile, Encoding.ASCII.GetBytes(issuer.AuthorityCertificatePem));
        var admission = Path.Combine(directory, "login-child-" + Guid.NewGuid().ToString("N") + ".json");
        PrivateTestFiles.WriteNew(admission, JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = 1,
            arguments = new[] { operation }
        }));
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = OwnedProcesses.FindRepository(),
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        var build = AppContext.BaseDirectory.Contains("/Release/", StringComparison.Ordinal) ? "Release" : "Debug";
        start.ArgumentList.Add(Path.Combine(OwnedProcesses.FindRepository(), "tests", "contract",
            "Kofte.Identity.Consumer", "bin", build, "net10.0", "Kofte.Identity.Consumer.dll"));
        start.ArgumentList.Add("--identity-fixture-worker");
        scope!.ConfigureChild(start);
        serving.ConfigureChild(start);
        start.Environment["KOFTE_HOST_URL"] = $"http://127.0.0.1:{port}";
        start.Environment["KOFTE_BACKEND_ORIGIN"] = $"http://127.0.0.1:{port}";
        start.Environment["IDENTITY_TEST_ADMISSION_FILE"] = admission;
        if (barrier is not null)
        {
            start.Environment["IDENTITY_TEST_BARRIER_FILE"] = barrier.DescriptorPath;
            start.Environment["DOTNET_STARTUP_HOOKS"] = typeof(IdentityCommitBarrier).Assembly.Location;
        }
        worker = OwnedProcesses.Start(start);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        var observed = "no_response";
        try
        {
            while (true)
            {
                deadline.Token.ThrowIfCancellationRequested();
                if (worker.HasExited) throw new InvalidOperationException("identity_fixture_child_exited");
                try
                {
                    using var ready = await client.GetAsync(operation == "--fixture-product-login"
                        ? "/api/identity/v1/me" : "/fixture-live", deadline.Token);
                    observed = "http_" + ((int)ready.StatusCode).ToString(CultureInfo.InvariantCulture);
                    if ((int)ready.StatusCode == (operation == "--fixture-product-login" ? 401 : 204)) break;
                }
                catch (HttpRequestException error) { observed = "transport_" + error.HttpRequestError; }
                await Task.Delay(5, deadline.Token);
            }
        }
        catch (OperationCanceledException error) when (deadline.IsCancellationRequested && !token.IsCancellationRequested)
        {
            throw new InvalidOperationException("identity_fixture_child_readiness_" + observed, error);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(string path, CancellationToken token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        var cookie = cookies.GetCookieHeader(new Uri(configuration.PublicOrigin, path));
        if (cookie.Length != 0) request.Headers.Add("Cookie", cookie);
        var response = await client.SendAsync(request, token);
        if (response.Headers.TryGetValues("Set-Cookie", out var values))
            foreach (var value in values) cookies.SetCookies(new Uri(configuration.PublicOrigin, path), value);
        return response;
    }

    public async Task<bool> UnboundAndReusedStoresRefuseAsync(CancellationToken token)
    {
        var ticket = lastTicket ?? throw new InvalidOperationException("identity_fixture_ticket_missing");
        var tickets = application!.Services.GetRequiredService<IdentityTicketStore>();
        var previous = accessor.HttpContext;
        var context = new DefaultHttpContext();
        accessor.HttpContext = context;
        try
        {
            return reusedRefused &&
                await RefusesAsync(() => tickets.StoreAsync(ticket, context, token)) &&
                await RefusesAsync(() => tickets.StoreAsync(ticket, token)) &&
                await RefusesAsync(() => tickets.StoreAsync(ticket));
        }
        finally { accessor.HttpContext = previous; }
    }

    private static async Task<bool> RefusesAsync(Func<Task<string>> operation)
    {
        try { await operation(); return false; }
        catch (InvalidOperationException error) when (error.Message == "identity_ticket_bridge_invalid") { return true; }
    }

    public async Task<LoginCommitSnapshot> SnapshotAsync(CancellationToken token)
    {
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), token);
        await using var database = await DeadlineStore.OpenAsync(installation, budget);
        var metadata = await IdentityStore.ReadMetadataAsync(database);
        var users = await database.QueryAsync(
            $"SELECT user_id,mode,display_name,created_at_utc,updated_at_utc FROM {database.Prefix}__kofte_users ORDER BY user_id");
        var mappings = await database.QueryAsync(
            $"SELECT mode,issuer,subject,user_id,created_at_utc FROM {database.Prefix}__kofte_external_identities ORDER BY user_id");
        var attempts = await database.QueryAsync(
            $"SELECT attempt_id,state,session_epoch FROM {database.Prefix}__kofte_login_attempts ORDER BY attempt_id");
        var sessions = await database.QueryAsync($"""
            SELECT reference_digest,user_id,mode,session_epoch,protected_ticket,created_at_utc,
            idle_expires_at_utc,absolute_expires_at_utc,state,last_seen_at_utc,terminal_at_utc
            FROM {database.Prefix}__kofte_sessions ORDER BY reference_digest
            """);
        var events = await database.QueryAsync($"""
            SELECT event_id,security_revision,event_code,correlation_id,user_id
            FROM {database.Prefix}__kofte_identity_events ORDER BY event_id
            """);
        return new(metadata.Revision, metadata.Epoch,
            (long)DeadlineStore.AssertScalar(await database.QueryAsync(
                $"SELECT registry_revision FROM {database.Prefix}__kofte_installation"))!,
            (string)DeadlineStore.AssertScalar(await database.QueryAsync(
                $"SELECT state FROM {database.Prefix}__kofte_bootstrap"))!,
            users.Select(row => new LoginUserObservation(Id(row[0]), (string)row[1]!, (string?)row[2],
                Utc(row[3]), Utc(row[4]))).ToArray(),
            mappings.Select(row => new LoginMappingObservation((string)row[0]!, (string)row[1]!, (string)row[2]!,
                Id(row[3]), Utc(row[4]))).ToArray(),
            attempts.Select(row => new LoginAttemptObservation(Id(row[0]), (string)row[1]!, Id(row[2]))).ToArray(),
            sessions.Select(row => new LoginSessionObservation((string)row[0]!, Id(row[1]), (string)row[2]!, Id(row[3]),
                (byte[])row[4]!, Utc(row[5]), Utc(row[6]), Utc(row[7]), (string)row[8]!,
                Utc(row[9]), row[10] is null ? null : Utc(row[10]))).ToArray(),
            events.Select(row => new LoginEventObservation(Id(row[0]), (long)row[1]!, (string)row[2]!,
                (string)row[3]!, row[4] is null ? null : Id(row[4]))).ToArray());
    }

    public async Task<LoginBootstrapObservation> BootstrapAsync(CancellationToken token)
    {
        using var budget = new OperationBudget(TimeSpan.FromSeconds(1), token);
        await using var database = await DeadlineStore.OpenAsync(installation, budget);
        var rows = await database.QueryAsync($"""
            SELECT installation_id,state,generation,nominee_issuer,nominee_subject,authorization_digest,
              expires_at_utc,initial_user_id,consumed_at_utc,consumed_generation
            FROM {database.Prefix}__kofte_bootstrap WHERE singleton_key=1
            """);
        if (rows.Count != 1) throw new InvalidOperationException("identity_fixture_bootstrap_invalid");
        var row = rows[0];
        return new(Id(row[0]), (string)row[1]!, (long)row[2]!, (string?)row[3], (string?)row[4],
            (string?)row[5], row[6] is null ? null : Utc(row[6]), row[7] is null ? null : Id(row[7]),
            row[8] is null ? null : Utc(row[8]), (long?)row[9]);
    }

    public async ValueTask DisposeAsync()
    {
        if (webWorker is not null) await webWorker.DisposeAsync();
        if (browserIngress is not null) await browserIngress.DisposeAsync();
        if (worker is not null) await worker.DisposeAsync();
        client?.Dispose();
        providerClient?.Dispose();
        if (application is not null)
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await application.StopAsync(stop.Token);
            await application.DisposeAsync();
        }
        observer?.Dispose();
        workerTrust?.Dispose();
        keys?.Dispose();
        configuration?.Dispose();
        fileLease?.Dispose();
        if (hostLease is not null) await hostLease.DisposeAsync();
        // A failed browser may still own this private profile; retain it.
        if (browserCompleted) serving?.Dispose();
        if (issuer is not null) await issuer.DisposeAsync();
        if (scope is not null) await scope.DisposeAsync();
    }

    private sealed class CommitInterruption() : Exception("identity_fixture_commit_interrupted");

    private sealed class FaultCookies(LoginCommitFixture owner) : ICookieManager
    {
        private readonly IdentitySessionCookieManager inner = new();
        public string? GetRequestCookie(HttpContext context, string key) => inner.GetRequestCookie(context, key);
        public void DeleteCookie(HttpContext context, string key, CookieOptions options) => inner.DeleteCookie(context, key, options);
        public void AppendResponseCookie(HttpContext context, string key, string? value, CookieOptions options)
        {
            if (owner.fault == LoginCommitFault.CookiePublication) throw new CommitInterruption();
            inner.AppendResponseCookie(context, key, value, options);
        }
    }

    private sealed class FaultObserver : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
    {
        private readonly LoginCommitFixture owner;
        private readonly List<IDisposable> subscriptions = [];
        private readonly IDisposable all;
        internal FaultObserver(LoginCommitFixture owner)
        {
            this.owner = owner;
            all = DiagnosticListener.AllListeners.Subscribe(this);
        }

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "Kofte.Identity") subscriptions.Add(listener.Subscribe(this));
        }

        public void OnNext(KeyValuePair<string, object?> item)
        {
            if (item.Key != "identity.phase" || item.Value is null ||
                !ReferenceEquals(owner.accessor.HttpContext?.RequestServices.GetService<LoginCommitFixture>(), owner)) return;
            var phase = JsonSerializer.SerializeToElement(item.Value).GetProperty("phase").GetString();
            if (phase == "enrollment_before_commit" && owner.failEnrollmentCommit)
            {
                owner.failEnrollmentCommit = false;
                throw new StorageException(Kofte.Platform.Modules.Storage.StorageCode.Unavailable);
            }
            if (owner.fault is not (LoginCommitFault.BeforeCommit or LoginCommitFault.AfterCommit)) return;
            if (phase == (owner.fault == LoginCommitFault.BeforeCommit ? "login_before_commit" : "login_committed"))
                throw new CommitInterruption();
        }

        public void OnCompleted() { }
        public void OnError(Exception error) => throw new InvalidOperationException("identity_fixture_observer_failed");
        public void Dispose()
        {
            all.Dispose();
            foreach (var subscription in subscriptions) subscription.Dispose();
        }
    }
}
