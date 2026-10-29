using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.IntegrationTests.Modules.Alpha;
using Kofte.Storage.IntegrationTests.Providers;
using Kofte.Storage.IntegrationTests.Registry;
using Kofte.TestSupport;
using Kofte.TestSupport.Identity;
using Microsoft.EntityFrameworkCore;

namespace Kofte.Storage.IntegrationTests.Identity;

public abstract class EnrollmentRestartTests
{
    protected abstract string Provider { get; }
    private CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task OriginalModuleDataAndConsumedDesignationSurviveRestartsPreparationAndUnavailableAdministrator()
    {
        StorageRegistry? moduleRegistry = null;
        var module = RegistryFixture.Module("org.example.alpha");
        await using var fixture = await LoginCommitFixture.CreateAsync(Provider, Token, productRoutes: true,
            prepareStorage: async (resources, token) =>
            {
                var installation = resources.Postgres is { } postgres
                    ? StorageInstallation.PostgreSql(postgres.ConnectionString, postgres.ProvisioningConnectionString)
                    : StorageInstallation.Sqlite(resources.DataRoot);
                moduleRegistry = new(installation);
                await using var scope = await new StorageMaintenance(installation).AcquireAsync(token);
                Assert.IsType<StorageResult.Ready>(await moduleRegistry.RegisterAsync(module, scope, token));
                await using var binding = await moduleRegistry.BindAsync(module.Id, token);
                Assert.IsType<StorageResult.Ready>(
                    await binding.MigrateAsync(ModuleFixture.AlphaFactory, scope, cancellationToken: token));
                await using var context = await binding.CreateContextAsync(ModuleFixture.AlphaFactory, token);
                context.Records.Add(new AlphaRecord
                {
                    Id = 37,
                    Payload = "unrelated-enrollment-sentinel",
                    Note = "original-owner-note"
                });
                await context.SaveChangesAsync(token);
            });
        var registry = Assert.IsType<StorageRegistry>(moduleRegistry);
        var assignment = await AssignmentAsync();
        await AssertModuleAsync();
        var authorization = await fixture.NominateProductAsync(Token);
        await LoginAsync(fixture);
        var before = await fixture.SnapshotAsync(Token);
        var administrator = Assert.Single(before.Users).Id;
        var csrf = await fixture.ProductContextAsync(Token);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var contenders = Enumerable.Range(0, 20).Select(async _ =>
        {
            using var request = Enrollment(fixture, csrf, authorization);
            await release.Task;
            using var response = await fixture.SendProductAsync(request, Token);
            Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict);
            return response.StatusCode;
        }).ToArray();
        release.SetResult();
        var outcomes = await Task.WhenAll(contenders);
        Assert.Equal(1, outcomes.Count(status => status == HttpStatusCode.OK));
        Assert.Equal(19, outcomes.Count(status => status == HttpStatusCode.Conflict));
        var consumed = await fixture.BootstrapAsync(Token);
        var committed = await fixture.SnapshotAsync(Token);
        var enrollment = Assert.Single(committed.Events, value => value.Code == "initial_administrator_enrolled");
        Assert.Equal(administrator, enrollment.UserId);
        Assert.Equal(administrator, consumed.InitialUser);
        var processes = new HashSet<int>();
        for (var restart = 0; restart < 3; restart++)
        {
            Assert.True(processes.Add(await fixture.RestartProductAsync(Token)));
            await AssertClosedAsync();
        }
        await fixture.PrepareProductAgainAsync(Token);
        await AssertClosedAsync();

        var originalIssuer = fixture.Issuer;
        await fixture.ReconcileProductProviderAsync(Token);
        Assert.NotEqual(originalIssuer, fixture.Issuer);
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me"))
        using (var refused = await fixture.SendProductAsync(request, Token))
            Assert.Equal(HttpStatusCode.Unauthorized, refused.StatusCode);
        await LoginAsync(fixture);
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me"))
        using (var current = await fixture.SendProductAsync(request, Token))
        {
            Assert.Equal(HttpStatusCode.OK, current.StatusCode);
            using var body = JsonDocument.Parse(await current.Content.ReadAsByteArrayAsync(Token));
            Assert.NotEqual(administrator, body.RootElement.GetProperty("userId").GetGuid());
            Assert.False(body.RootElement.GetProperty("isInitialAdministrator").GetBoolean());
        }
        await AssertClosedAsync();
        var refusedReplacement = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.NominateProductAsync(Token));
        Assert.Equal("identity_fixture_operator_bootstrap_authorize_bootstrap_authorize_" +
            "identity_enrollment_unavailable", refusedReplacement.Message);
        Assert.Equal(consumed, await fixture.BootstrapAsync(Token));
        Assert.Equal(enrollment, Assert.Single((await fixture.SnapshotAsync(Token)).Events,
            value => value.Code == "initial_administrator_enrolled"));
        await AssertModuleAsync();

        async Task<StorageAssignmentSnapshot> AssignmentAsync() =>
            StorageAssignmentSnapshot.From(Assert.IsType<StorageResult.Ready>(
                await registry.ResolveAsync(module.Id, Token)).Assignment);

        async Task AssertModuleAsync()
        {
            Assert.Equal(assignment, await AssignmentAsync());
            await using var binding = await registry.BindAsync(module.Id, Token);
            await using var context = await binding.CreateContextAsync(ModuleFixture.AlphaFactory, Token);
            var row = await context.Records.SingleAsync(Token);
            Assert.Equal((37, "unrelated-enrollment-sentinel", "original-owner-note"),
                (row.Id, row.Payload, row.Note));
            Assert.Equal(["202610210001_Initial", "202610210002_AddNote"],
                await context.Database.GetAppliedMigrationsAsync(Token));
        }

        async Task AssertClosedAsync()
        {
            using var replay = Enrollment(fixture, await fixture.ProductContextAsync(Token), authorization);
            using var response = await fixture.SendProductAsync(replay, Token);
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            Assert.Equal(consumed, await fixture.BootstrapAsync(Token));
            var after = await fixture.SnapshotAsync(Token);
            Assert.Equal(enrollment, Assert.Single(after.Events,
                value => value.Code == "initial_administrator_enrolled"));
            Assert.Equal(committed.RegistryRevision, after.RegistryRevision);
            Assert.All(committed.Users, user => Assert.Contains(user, after.Users));
            Assert.All(committed.Mappings, mapping => Assert.Contains(mapping, after.Mappings));
            await AssertModuleAsync();
        }
    }

    private async Task LoginAsync(LoginCommitFixture fixture)
    {
        using var request = fixture.ProductLoginRequest("/identity", await fixture.ProductContextAsync(Token));
        using var start = await fixture.SendProductAsync(request, Token);
        using var callback = await fixture.CompleteProductAsync(start, Token);
        Assert.Equal(303, (int)callback.StatusCode);
    }

    private static HttpRequestMessage Enrollment(LoginCommitFixture fixture, string csrf, string authorization)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/first-administrator")
        {
            Content = JsonContent.Create(new { authorization })
        };
        request.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
        request.Headers.Add("X-Kofte-CSRF", csrf);
        return request;
    }
}

[Trait("Provider", "sqlite")]
[Collection("identity-deadlines")]
public sealed class SqliteEnrollmentRestartTests : EnrollmentRestartTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
[Collection("identity-deadlines")]
public sealed class PostgresEnrollmentRestartTests : EnrollmentRestartTests
{
    protected override string Provider => "postgres";
}
