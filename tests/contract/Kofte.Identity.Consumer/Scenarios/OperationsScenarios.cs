using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Recovery;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.Consumer.Modules.Alpha;
using Kofte.TestSupport;
using Kofte.TestSupport.Identity;
using Microsoft.EntityFrameworkCore;
using static Kofte.Identity.Consumer.Scenarios.LoginScenarios;

namespace Kofte.Identity.Consumer.Scenarios;

internal static class OperationsScenarios
{
    private const string ModuleId = "org.example.identity-operations";
    private const string Payload = "public-identity-operations-preserved-payload";

    internal static async Task<IReadOnlyList<string>> RunAsync(string scenario, string provider,
        CancellationToken token)
    {
        var observations = new List<string>();
        switch (scenario)
        {
            case "upgrade-preservation": await UpgradeAsync(provider, observations, token); break;
            case "interrupted-recovery": await RecoveryAsync(provider, observations, token); break;
            case "private-evidence": await PrivateAsync(provider, observations, token); break;
            default: throw new InvalidOperationException("identity_scenario_required");
        }
        return observations;
    }

    private static StorageInstallation Installation(StorageProviderScope storage) =>
        storage.Postgres is { } postgres ? StorageInstallation.PostgreSql(
            postgres.ConnectionString, postgres.ProvisioningConnectionString) :
            StorageInstallation.Sqlite(storage.DataRoot);

    private static async Task PopulateModuleAsync(StorageProviderScope storage, CancellationToken token)
    {
        var installation = Installation(storage);
        using var diagnostics = new StorageCapture(installation.Provider);
        var maintenance = new StorageMaintenance(installation);
        await using var stopped = await maintenance.AcquireAsync(token);
        await maintenance.UpgradeAsync(stopped, token);
        var json = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            id = ModuleId,
            version = "1.0.0",
            displayName = "Köfte operations guide fixture",
            hostContract = "1.0.0",
            capabilities = Array.Empty<object>(),
            dependencies = Array.Empty<object>(),
            permissions = Array.Empty<object>(),
            storage = new { relational = true }
        });
        if (ManifestValidator.Validate(json, new("1.0.0")) is not ManifestValidationResult.Accepted accepted)
            throw new InvalidOperationException("identity_recipe_module_invalid");
        var registry = new StorageRegistry(installation);
        if (await registry.RegisterAsync(accepted.Description, stopped, token) is not StorageResult.Ready)
            throw new InvalidOperationException("identity_recipe_module_registration_failed");
        await using var binding = await registry.BindAsync(accepted.Description.Id, token);
        if (await binding.MigrateAsync(AlphaStorage.Create, stopped, cancellationToken: token) is not StorageResult.Ready)
            throw new InvalidOperationException("identity_recipe_module_migration_failed");
        await using var context = await binding.CreateContextAsync(AlphaStorage.Create, token);
        context.Records.Add(new() { Id = 17, Payload = Payload, Note = "preserved-module-note" });
        await context.SaveChangesAsync(token);
    }

    private static async Task<string> ModuleSnapshotAsync(StorageInstallation installation, CancellationToken token)
    {
        using var diagnostics = new StorageCapture(installation.Provider);
        await using var stopped = await new StorageMaintenance(installation).AcquireAsync(token);
        var inventory = await RecoverySetVerifier.InspectStoppedAsync(installation, stopped, token);
        var allocation = inventory.Allocations.Single();
        if (allocation.ModuleId != ModuleId || !allocation.History.Select(row => row.Id).SequenceEqual(
            ["202610210001_Initial", "202610210002_AddNote"]))
            throw new InvalidOperationException("identity_recipe_module_history_changed");
        var registry = new StorageRegistry(installation);
        if (!Kofte.Platform.Modules.Manifest.ModuleId.TryParse(ModuleId, out var owner))
            throw new InvalidOperationException("identity_recipe_module_invalid");
        await using var binding = await registry.BindAsync(owner, token);
        await using var context = await binding.CreateContextAsync(AlphaStorage.Create, token);
        var rows = await context.Records.AsNoTracking().OrderBy(row => row.Id).ToArrayAsync(token);
        if (rows.Length != 1 || rows[0].Id != 17 || rows[0].Payload != Payload ||
            rows[0].Note != "preserved-module-note")
            throw new InvalidOperationException("identity_recipe_module_data_changed");
        return JsonSerializer.Serialize(new
        {
            inventory.InstallationId,
            inventory.Provider,
            inventory.RegistryRevision,
            inventory.Allocations,
            rows = rows.Select(row => new { row.Id, row.Payload, row.Note }),
            history = await context.Database.GetAppliedMigrationsAsync(token)
        });
    }

    private static async Task<int> FormatAsync(StorageInstallation installation, CancellationToken token)
    {
        using var diagnostics = new StorageCapture(installation.Provider);
        await using var stopped = await new StorageMaintenance(installation).AcquireAsync(token);
        return (await RecoverySetVerifier.InspectStoppedAsync(installation, stopped, token)).RegistryVersion;
    }

    private static async Task UpgradeAsync(string provider, ICollection<string> observations, CancellationToken token)
    {
        bool cleanReady;
        await using (var clean = await LoginCommitFixture.CreateProductAsync(provider, token))
        {
            var prepared = await clean.SnapshotAsync(token);
            var cleanKeys = clean.KeyRingFingerprint;
            await clean.PrepareProductAgainAsync(token);
            cleanReady = prepared.Users.Count == 0 && prepared.Mappings.Count == 0 &&
                prepared.Attempts.Count == 0 && prepared.Sessions.Count == 0 &&
                prepared.BootstrapState == "disabled" && prepared.Events.Count == 1 &&
                prepared.Events[0].Code == "keys_prepared" && cleanKeys.Length > 0 &&
                cleanKeys == clean.KeyRingFingerprint && IdentityExact(prepared, await clean.SnapshotAsync(token));
        }
        string? moduleBefore = null;
        await using var fixture = await LoginCommitFixture.CreateOperationsAsync(provider, async (storage, cancel) =>
        {
            await PopulateModuleAsync(storage, cancel);
            Require(await FormatAsync(Installation(storage), cancel) == 1,
                "upgrade_format_one_module_sentinels_history", observations);
            moduleBefore = await ModuleSnapshotAsync(Installation(storage), cancel);
        }, token);
        var original = await fixture.SnapshotAsync(token);
        Require(cleanReady && original.Users.Count == 0 && original.Mappings.Count == 0 && original.BootstrapState == "disabled" &&
            original.Events.Count == 1 && original.Events[0].Code == "keys_prepared" &&
            await fixture.ObserveStoppedOperationsAsync(FormatAsync, token) == 2,
            "upgrade_clean_current_identity_prepared", observations);
        Require(moduleBefore == await fixture.ObserveStoppedOperationsAsync(ModuleSnapshotAsync, token),
            "upgrade_exact_module_inventory_preserved", observations);
        using (var login = await LoginAsync(fixture, token))
            Require((int)login.StatusCode == 303 && (await CurrentAsync(fixture, token)).Id != Guid.Empty,
                "upgrade_native_user_mapping", observations);
        var authorization = await fixture.NominateProductAsync(token);
        using (var request = EnrollmentScenarios.Enrollment(fixture, await fixture.ProductContextAsync(token), authorization))
        using (var response = await fixture.SendProductAsync(request, token))
            Require(response.StatusCode == HttpStatusCode.OK, "upgrade_explicit_consumed_designation", observations);
        var before = await fixture.SnapshotAsync(token);
        var ceremony = await fixture.BootstrapAsync(token);
        var keys = fixture.KeyRingFingerprint;
        await fixture.PrepareProductAgainAsync(token);
        var after = await fixture.SnapshotAsync(token);
        Require(IdentityExact(before, after) && ceremony == await fixture.BootstrapAsync(token) &&
            keys == fixture.KeyRingFingerprint &&
            moduleBefore == await fixture.ObserveStoppedOperationsAsync(ModuleSnapshotAsync, token),
            "upgrade_idempotent_exact_identity_history_modules", observations);
        fixture.Protocol.Subject = "lost-original-idp-replacement";
        using (var login = await LoginAsync(fixture, token))
        {
            var replacement = await CurrentAsync(fixture, token);
            var changed = await fixture.SnapshotAsync(token);
            Require((int)login.StatusCode == 303 && !replacement.Designated &&
                replacement.Id != ceremony.InitialUser && before.Users.All(changed.Users.Contains) &&
                before.Mappings.All(changed.Mappings.Contains) && before.Events.All(changed.Events.Contains) &&
                ceremony == await fixture.BootstrapAsync(token),
                "upgrade_idp_loss_no_reassignment", observations);
        }
        Require(await fixture.RefuseRenominationOperationsAsync(token),
            "upgrade_consumed_nomination_refused", observations);
    }

    private static async Task RecoveryAsync(string provider, ICollection<string> observations, CancellationToken token)
    {
        using var diagnostics = new StorageCapture(provider == "sqlite" ? "sqlite" : "postgresql");
        await PrepareCrashAsync(provider, observations, token);
        await using var fixture = await LoginCommitFixture.CreateOperationsAsync(provider, PopulateModuleAsync, token);
        using (var login = await LoginAsync(fixture, token))
            if ((int)login.StatusCode != 303) throw new InvalidOperationException("identity_recipe_login_failed");
        var authorization = await fixture.NominateProductAsync(token);
        var before = await fixture.SnapshotAsync(token);
        var pending = await fixture.BootstrapAsync(token);
        var crash = await fixture.CrashEnrollmentAsync("enrollment_before_commit", authorization, token);
        Require(crash.AcknowledgementLost && crash.ExitVerified,
            "recovery_before_commit_owned_termination", observations);
        await fixture.RestartProductAsync(token);
        var rollback = await fixture.SnapshotAsync(token);
        Require(IdentityStable(before, rollback) && before.Attempts.SequenceEqual(rollback.Attempts) &&
            pending == await fixture.BootstrapAsync(token),
            "recovery_before_commit_exact_rollback", observations);
        crash = await fixture.CrashEnrollmentAsync("enrollment_committed", authorization, token);
        Require(crash.AcknowledgementLost && crash.ExitVerified,
            "recovery_after_commit_owned_termination", observations);
        await fixture.RestartProductAsync(token);
        var committed = await fixture.SnapshotAsync(token);
        var consumed = await fixture.BootstrapAsync(token);
        Require(consumed.State == "consumed" && consumed.InitialUser == before.Users.Single().Id &&
            consumed.Generation == pending.Generation && consumed.ConsumedGeneration == pending.Generation &&
            consumed.ConsumedAt is not null && before.Users.SequenceEqual(committed.Users) &&
            before.Mappings.SequenceEqual(committed.Mappings) && before.Events.All(committed.Events.Contains) &&
            committed.Events.Except(before.Events).Single() is { Code: "initial_administrator_enrolled" } enrolled &&
            enrolled.UserId == consumed.InitialUser && committed.RegistryRevision == before.RegistryRevision,
            "recovery_after_commit_exact_consumption", observations);
        using var backup = new OwnedTestRoot();
        var manifest = await fixture.BackupProductAsync(backup.Path, token);
        Require(manifest.Identity?.BootstrapState == "consumed" &&
            manifest.Allocations.Single().ModuleId == ModuleId &&
            manifest.Allocations[0].History.Count == 2 && manifest.Artifacts.Count == (provider == "sqlite" ? 2 : 1),
            "recovery_complete_set_exact_modules", observations);
        var restored = await fixture.RestoreOperationsAsync(backup.Path, "known-never-enrolled",
            "restore_committed", ModuleSnapshotAsync, token);
        Require(restored.ExposureRefused, "recovery_quarantine_refuses_exposure", observations);
        Require(restored.InterruptedExitVerified, "recovery_reconciliation_owned_termination", observations);
        Require(RestoredExact(restored) && restored.BootstrapAfter == restored.BootstrapBefore &&
            restored.BootstrapAfter == consumed,
            "recovery_explicit_retry_exact_identity_history_modules", observations);
        Require(restored.OldCookieRefused && restored.FreshUser == consumed.InitialUser && restored.FreshDesignation,
            "recovery_old_cookie_refused_fresh_login_same_designation", observations);
        Require(restored.ReassignmentRefused, "recovery_consumed_reassignment_refused", observations);

        await using var stale = await LoginCommitFixture.CreateOperationsAsync(provider, PopulateModuleAsync, token);
        using (var login = await LoginAsync(stale, token))
            if ((int)login.StatusCode != 303) throw new InvalidOperationException("identity_recipe_login_failed");
        var stalePoint = await stale.SnapshotAsync(token);
        var staleCeremony = await stale.BootstrapAsync(token);
        using var staleBackup = new OwnedTestRoot();
        await stale.BackupProductAsync(staleBackup.Path, token);
        LoginBootstrapObservation? laterConsumed = null;
        LoginEventObservation? laterRecord = null;
        // Verify and quarantine the copy before advancing its original; reconcile only after it is genuinely stale.
        var locked = await stale.RestoreOperationsAsync(staleBackup.Path, "not-established",
            null, ModuleSnapshotAsync, token, stalePoint, staleCeremony, async cancellation =>
            {
                var laterAuthorization = await stale.NominateProductAsync(cancellation);
                using (var request = EnrollmentScenarios.Enrollment(stale,
                    await stale.ProductContextAsync(cancellation), laterAuthorization))
                using (var response = await stale.SendProductAsync(request, cancellation))
                    if (response.StatusCode != HttpStatusCode.OK)
                        throw new InvalidOperationException("identity_recipe_later_enrollment_failed");
                laterConsumed = await stale.BootstrapAsync(cancellation);
                laterRecord = (await stale.SnapshotAsync(cancellation)).Events
                    .Single(row => row.Code == "initial_administrator_enrolled");
            });
        Require(locked.ExposureRefused && locked.ReassignmentRefused && locked.OldCookieRefused &&
            locked.BootstrapBefore.State == "disabled" && locked.BootstrapAfter.State == "recovery_locked" &&
            laterConsumed is { State: "consumed" } && laterConsumed.InitialUser == stalePoint.Users.Single().Id &&
            laterRecord is not null && !locked.After.Events.Contains(laterRecord) && locked.BootstrapAfter.InitialUser is null &&
            RestoredExact(locked), "recovery_stale_copy_unknown_history_locked", observations);
    }

    private static async Task PrepareCrashAsync(string provider, ICollection<string> observations, CancellationToken token)
    {
        await using var fixture = await IdentityFixture.CreateAsync(provider, token);
        fixture.WriteConfiguration(JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = "1",
            mode = "development",
            publicOrigin = fixture.WebUrl.GetLeftPart(UriPartial.Authority),
            keyRingDirectory = fixture.KeyRingDirectory,
            development = new { enabled = true, identities = new[] { new { key = "synthetic" } } }
        }));
        await PopulateModuleAsync(fixture.Storage, token);
        var installation = Installation(fixture.Storage);
        var before = await ModuleSnapshotAsync(installation, token);
        var barrier = new IdentityCommitBarrier(fixture.PrivateDirectory, "operator_committed", maintenanceChild: true);
        await using (var process = fixture.Start(["--identity", "prepare"], barrier: barrier))
        {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(15));
            await barrier.WaitAsync(deadline.Token);
            var output = await process.InterruptMaintenanceChildAsync(barrier, deadline.Token);
            Require(output.ExitCode != 0 && process.ExitVerified && process.DescendantExitsVerified &&
                output.Stderr.Contains("\"durability\":\"uncertain\"", StringComparison.Ordinal),
                "recovery_prepare_owned_termination", observations);
        }
        var committed = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        await using var retry = fixture.Start(["--identity", "prepare"]);
        var result = await retry.WaitAsync(token);
        Require(result.ExitCode == 0 && retry.ExitVerified && retry.DescendantExitsVerified &&
            await FormatAsync(installation, token) == 2 && before == await ModuleSnapshotAsync(installation, token) &&
            committed == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
            "recovery_prepare_explicit_retry_exact_state", observations);
    }

    private static bool IdentityExact(LoginCommitSnapshot before, LoginCommitSnapshot after) =>
        before.Revision == after.Revision && before.Epoch == after.Epoch &&
        before.RegistryRevision == after.RegistryRevision && before.BootstrapState == after.BootstrapState &&
        before.Users.SequenceEqual(after.Users) && before.Mappings.SequenceEqual(after.Mappings) &&
        before.Events.SequenceEqual(after.Events) && before.Attempts.SequenceEqual(after.Attempts) &&
        JsonSerializer.Serialize(before.Sessions) == JsonSerializer.Serialize(after.Sessions);

    private static bool IdentityStable(LoginCommitSnapshot before, LoginCommitSnapshot after) =>
        before.Epoch == after.Epoch && before.RegistryRevision == after.RegistryRevision &&
        before.BootstrapState == after.BootstrapState && before.Users.SequenceEqual(after.Users) &&
        before.Mappings.SequenceEqual(after.Mappings) && before.Events.SequenceEqual(after.Events) &&
        JsonSerializer.Serialize(before.Sessions.Select(row =>
            new
            {
                row.Digest,
                row.UserId,
                row.Mode,
                row.Epoch,
                row.ProtectedTicket,
                row.CreatedAt,
                row.AbsoluteExpiresAt,
                row.State,
                row.TerminalAt
            })) ==
        JsonSerializer.Serialize(after.Sessions.Select(row =>
            new
            {
                row.Digest,
                row.UserId,
                row.Mode,
                row.Epoch,
                row.ProtectedTicket,
                row.CreatedAt,
                row.AbsoluteExpiresAt,
                row.State,
                row.TerminalAt
            }));

    private static bool RestoredExact(IdentityRestoreRecipe restored) =>
        restored.ModuleBefore == restored.ModuleAfter &&
        restored.Before.Users.SequenceEqual(restored.After.Users) &&
        restored.Before.Mappings.SequenceEqual(restored.After.Mappings) &&
        restored.Before.RegistryRevision == restored.After.RegistryRevision &&
        restored.After.Epoch != restored.Before.Epoch &&
        restored.After.Revision == restored.Before.Revision + 1 &&
        restored.Before.Events.All(restored.After.Events.Contains) &&
        restored.After.Events.Except(restored.Before.Events).Single().Code == "restore_reconciled" &&
        restored.After.Sessions.All(session => session.State == "revoked") &&
        restored.Before.Sessions.Select(row => (row.Digest, row.UserId, row.CreatedAt))
            .SequenceEqual(restored.After.Sessions.Select(row => (row.Digest, row.UserId, row.CreatedAt))) &&
        restored.After.Attempts.All(attempt => attempt.State != "pending");

    private static async Task PrivateAsync(string provider, ICollection<string> observations, CancellationToken token)
    {
        var clock = new RecipeClock();
        await using var fixture = await LoginCommitFixture.CreateAsync(provider, token, clock, productRoutes: true);
        var canary = "private-recipe-" + Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
        fixture.Protocol.Subject = canary + "-subject";
        fixture.Protocol.DisplayName = canary + "-profile";
        var authorization = await fixture.NominateProductAsync(token);
        await fixture.UseInProcessOperationsAsync(token);
        var publicCaptures = new List<string>();
        var sensitive = new List<string> { canary, authorization, fixture.Protocol.ClientSecret, fixture.Issuer };
        using var capture = new IdentityOperationsCapture();
        using (var login = await LoginAsync(fixture, token))
        {
            publicCaptures.Add(await CaptureAsync(login, token));
            sensitive.Add(fixture.Protocol.IssuedIdToken ??
                throw new InvalidOperationException("identity_recipe_protocol_capture_missing"));
            var snapshot = await fixture.SnapshotAsync(token);
            Require((int)login.StatusCode == 303 && snapshot.Users.Count == 1 &&
                snapshot.Events.Where(row => row.Code is "user_created" or "session_created").All(row =>
                    capture.Records.Any(record => record.Operation == "callback" && record.Correlation == row.Correlation &&
                        record.Durability == "committed")),
                "private_native_login_correlated", observations);
        }
        var beforeReject = await fixture.SnapshotAsync(token);
        fixture.Protocol.Mutation = IdTokenMutation.WrongSignature;
        using (var refused = await LoginAsync(fixture, token))
        {
            publicCaptures.Add(await CaptureAsync(refused, token));
            sensitive.Add(fixture.Protocol.IssuedIdToken ??
                throw new InvalidOperationException("identity_recipe_protocol_capture_missing"));
            var afterReject = await fixture.SnapshotAsync(token);
            Require(refused.StatusCode == HttpStatusCode.Unauthorized &&
                beforeReject.Users.SequenceEqual(afterReject.Users) && beforeReject.Events.SequenceEqual(afterReject.Events) &&
                capture.Records.Any(record => record.Operation == "callback" && record.Outcome == "rejected" &&
                    record.Code == "identity_unauthenticated"), "private_rejected_login_unchanged", observations);
        }
        fixture.Protocol.Mutation = IdTokenMutation.None;
        using (var request = EnrollmentScenarios.Enrollment(fixture, await fixture.ProductContextAsync(token), authorization))
        using (var response = await fixture.SendProductAsync(request, token))
        {
            publicCaptures.Add(await CaptureAsync(response, token));
            var enrolled = (await fixture.SnapshotAsync(token)).Events.Single(row => row.Code == "initial_administrator_enrolled");
            Require(response.StatusCode == HttpStatusCode.OK && capture.Records.Any(record =>
                record.Operation == "enrollment" && record.Correlation == enrolled.Correlation && record.Durability == "committed"),
                "private_explicit_enrollment_correlated", observations);
        }
        await fixture.SetRevocationRecordingFailureAsync(true, token);
        var csrf = await fixture.ProductContextAsync(token);
        var beforeFailure = await fixture.SnapshotAsync(token);
        var correctionCookie = fixture.ProductCookieHeader;
        var consumed = await fixture.BootstrapAsync(token);
        using (var request = SignOut(fixture, csrf))
        using (var failed = await fixture.SendProductAsync(request, token))
        {
            publicCaptures.Add(await CaptureAsync(failed, token));
            var afterFailure = await fixture.SnapshotAsync(token);
            Require(failed.StatusCode == HttpStatusCode.ServiceUnavailable && IdentityStable(beforeFailure, afterFailure) &&
                beforeFailure.Attempts.SequenceEqual(afterFailure.Attempts) &&
                consumed == await fixture.BootstrapAsync(token) &&
                capture.Records.Any(record => record.Operation == "sign_out" && record.Outcome == "unavailable"),
                "private_required_record_failure_rollback", observations);
        }
        await fixture.SetRevocationRecordingFailureAsync(false, token);
        using (var request = SignOut(fixture, csrf))
        {
            request.Headers.Add("Cookie", correctionCookie);
            using var response = await fixture.SendProductAsync(request, token, includeCookies: false);
            publicCaptures.Add(await CaptureAsync(response, token));
            var revoked = (await fixture.SnapshotAsync(token)).Events.Single(row => row.Code == "session_revoked");
            Require(response.StatusCode == HttpStatusCode.NoContent && capture.Records.Any(record =>
                record.Operation == "sign_out" && record.Correlation == revoked.Correlation && record.Durability == "committed"),
                "private_confirmed_sign_out_correlated", observations);
        }
        using (var login = await LoginAsync(fixture, token))
        {
            if ((int)login.StatusCode != 303) throw new InvalidOperationException("identity_recipe_login_failed");
            sensitive.Add(fixture.Protocol.IssuedIdToken ??
                throw new InvalidOperationException("identity_recipe_protocol_capture_missing"));
        }
        var beforeExpiry = await fixture.SnapshotAsync(token);
        var active = beforeExpiry.Sessions.Single(row => row.State == "active");
        clock.Now = new(active.IdleExpiresAt);
        using (var request = new HttpRequestMessage(HttpMethod.Get, "/api/identity/v1/me"))
        using (var response = await fixture.SendProductAsync(request, token))
        {
            publicCaptures.Add(await CaptureAsync(response, token));
            var expired = await fixture.SnapshotAsync(token);
            var record = expired.Events.Except(beforeExpiry.Events).Single();
            Require(response.StatusCode == HttpStatusCode.Unauthorized && record.Code == "session_expired" &&
                expired.Sessions.Single(row => row.Digest == active.Digest).State == "expired" &&
                expired.Users.SequenceEqual(beforeExpiry.Users) && expired.Mappings.SequenceEqual(beforeExpiry.Mappings) &&
                consumed == await fixture.BootstrapAsync(token) &&
                capture.Records.Any(value => value.Operation == "current" && value.Correlation == record.Correlation &&
                    value.Outcome == "rejected" && value.Durability == "committed"),
                "private_session_expiry_correlated", observations);
        }
        Require(capture.HasCorrelatedChannels(provider == "sqlite" ? "sqlite" : "postgresql"),
            "private_captured_diagnostics_spans_metrics", []);
        var output = await fixture.OperatorOperationsAsync(["--identity", "revoke-sessions"], token);
        publicCaptures.Add(output.Stdout + output.Stderr);
        using var result = JsonDocument.Parse(output.Stdout);
        var correlation = result.RootElement.GetProperty("correlationId").GetString();
        Require(output.ExitCode == 0 && result.RootElement.GetProperty("durability").GetString() == "committed" &&
            (await fixture.SnapshotAsync(token)).Events.Any(row => row.Code == "sessions_revoked" && row.Correlation == correlation),
            "private_operator_result_correlated", observations);
        observations.Add("private_captured_diagnostics_spans_metrics");
        var events = JsonSerializer.Serialize((await fixture.SnapshotAsync(token)).Events);
        Require(capture.IsPrivate(sensitive) && sensitive.All(value =>
            !string.Join('\n', publicCaptures.Append(events)).Contains(value, StringComparison.Ordinal)),
            "private_all_captured_channels_canary_free", observations);
    }

    private static HttpRequestMessage SignOut(LoginCommitFixture fixture, string csrf)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/identity/v1/sign-out");
        request.Headers.Add("Origin", fixture.PublicOrigin.OriginalString);
        request.Headers.Add("X-Kofte-CSRF", csrf);
        return request;
    }

    private static async Task<string> CaptureAsync(HttpResponseMessage response, CancellationToken token)
    {
        var body = await response.Content.ReadAsStringAsync(token);
        if (Encoding.UTF8.GetByteCount(body) > 8192) throw new InvalidOperationException("identity_recipe_response_limit");
        var captured = body + "\n" + string.Join('\n', response.Headers.Concat(response.Content.Headers)
            .Select(header => header.Key + ":" + string.Join(';', header.Value)));
        if (Encoding.UTF8.GetByteCount(captured) > 32_768)
            throw new InvalidOperationException("identity_recipe_response_limit");
        return captured;
    }

    private sealed class StorageCapture : StringWriter
    {
        private readonly TextWriter previous;
        private readonly string provider;
        public StorageCapture(string provider)
        {
            this.provider = provider;
            previous = Console.Error;
            Console.SetError(this);
        }

        public override void WriteLine(string? value)
        {
            if (value is null || Encoding.UTF8.GetByteCount(value) > 1024)
                throw new InvalidOperationException("identity_recipe_storage_capture_invalid");
            using var document = JsonDocument.Parse(value, new() { AllowDuplicateProperties = false, MaxDepth = 2 });
            var item = document.RootElement;
            if (item.TryGetProperty("eventCode", out var eventCode) &&
                IdentityScenarioCatalog.IsSafeFailureCode("identity_recipe_" + eventCode.GetString()))
                throw new InvalidOperationException("identity_recipe_" + eventCode.GetString());
            var failure = !item.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal).SequenceEqual(
                    ["component", "correlationId", "durability", "eventCode", "guidance", "moduleId",
                     "operation", "outcome", "provider"]) ? "shape" :
                item.GetProperty("component").GetString() != "storage" ? "component" :
                item.GetProperty("provider").GetString() != provider ? "provider" :
                item.GetProperty("eventCode").GetString() != "storage_ready" ? "code" :
                item.GetProperty("moduleId").GetString() is { } owner && owner != ModuleId ? "owner" :
                item.GetProperty("correlationId").GetString() is not { Length: 32 } correlation ||
                !correlation.All(char.IsAsciiHexDigitLower) ? "correlation" :
                item.GetProperty("operation").GetString() is not ("register" or "resolve" or "migrate" or "upgrade" or "recover") ? "operation" :
                item.GetProperty("outcome").GetString() != "ready" ? "outcome" :
                item.GetProperty("guidance").GetString() != "retry_same_request" ? "guidance" :
                item.GetProperty("durability").GetString() is not ("unchanged" or "committed") ? "durability" : null;
            if (failure is not null) throw new InvalidOperationException("identity_recipe_storage_capture_" + failure);
        }

        protected override void Dispose(bool disposing)
        {
            Console.SetError(previous);
            base.Dispose(disposing);
        }

    }

    private sealed class RecipeClock : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
