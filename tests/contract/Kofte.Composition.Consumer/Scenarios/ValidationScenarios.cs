using System.Globalization;
using System.Text.Json;
using Kofte.Composition.TestModule;
using Kofte.Host.Features.ModuleComposition;
using Kofte.Platform.Modules.Composition;
using Kofte.Platform.Modules.Lifecycle;
using Kofte.TestSupport;

namespace Kofte.Composition.Consumer.Scenarios;

internal static class ValidationScenarios
{
    private sealed record Decision(ResolvedComposition? Accepted, IReadOnlyList<CompositionDiagnostic> Diagnostics)
    {
        internal string Snapshot => Accepted is null
            ? JsonSerializer.Serialize(Diagnostics) : JsonSerializer.Serialize(Accepted);
    }

    internal static async Task<ScenarioEvidence> DeterministicAsync(CancellationToken token)
    {
        var evidence = new ScenarioEvidence("deterministic-corpus");
        var corpus = FrozenCompositionCorpus.Cases;
        evidence.Require(corpus.Count == 21 && corpus.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == 21,
            "composition_test_frozen_corpus_changed");
        var snapshots = new Dictionary<string, string>(StringComparer.Ordinal);
        var selections = corpus.ToDictionary(item => item.Name,
            item => ModuleSelectionReader.Parse(DeploymentProgram.Selection(item.Selection.Reverse())), StringComparer.Ordinal);
        var beforeCulture = CultureInfo.CurrentCulture;
        var beforeUiCulture = CultureInfo.CurrentUICulture;
        var cultures = new HashSet<string>(StringComparer.Ordinal);
        var paths = new HashSet<string>(StringComparer.Ordinal);
        var minimumRepetitions = int.MaxValue;
        var minimumPermutations = int.MaxValue;
        try
        {
            foreach (var name in new[] { "en-US", "tr-TR" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
                evidence.Require(CultureInfo.CurrentCulture.Name == name && CultureInfo.CurrentUICulture.Name == name,
                    "composition_test_culture_not_applied");
                var completedPermutations = 0;
                for (var permutation = 0; permutation < 10; permutation++)
                {
                    var completedRepetitions = 0;
                    for (var repetition = 0; repetition < 100; repetition++)
                    {
                        foreach (var item in corpus)
                        {
                            token.ThrowIfCancellationRequested();
                            var catalog = FrozenCompositionCorpus.Permute(item.Deployments, permutation);
                            var author = ResolveAuthor(catalog, item.Selection.Reverse(), token);
                            RequireFrozen(evidence, item, author);
                            evidence.Observed("exactDecisionComparisons");
                            paths.Add("author");
                            var host = await ResolveHostAsync(catalog, selections[item.Name], token);
                            RequireFrozen(evidence, item, host);
                            evidence.Observed("exactDecisionComparisons");
                            paths.Add("host-preflight");
                            evidence.Require(author.Snapshot == host.Snapshot, "composition_test_public_paths_differ");
                            if (snapshots.TryGetValue(item.Name, out var original))
                                evidence.Require(original == host.Snapshot, "composition_test_decision_not_deterministic");
                            else snapshots.Add(item.Name, author.Snapshot);
                        }
                        completedRepetitions++;
                    }
                    minimumRepetitions = Math.Min(minimumRepetitions, completedRepetitions);
                    completedPermutations++;
                }
                minimumPermutations = Math.Min(minimumPermutations, completedPermutations);
                cultures.Add(CultureInfo.CurrentCulture.Name);
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = beforeCulture;
            CultureInfo.CurrentUICulture = beforeUiCulture;
        }
        evidence.Require(snapshots.Count == corpus.Count && paths.Count == 2 && cultures.Count == 2,
            "composition_test_determinism_incomplete");
        evidence.Observed("repetitionsPerPermutation", minimumRepetitions);
        evidence.Observed("permutations", minimumPermutations);
        evidence.Observed("cultures", cultures.Count);
        evidence.Observed("publicConsumerPaths", paths.Count);
        return evidence;
    }

    internal static async Task<ScenarioEvidence> InvalidAsync(string provider, CancellationToken token)
    {
        var evidence = new ScenarioEvidence("invalid-composition");
        await using var fixture = await CompositionFixture.CreateAsync(provider, token);
        await fixture.SeedUnrelatedNoStorageAsync(token);
        var before = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        var attempts = new HashSet<string>(StringComparer.Ordinal);
        var frozenCases = new[]
        {
            (CompositionTestCatalog.MalformedManifest, "manifestRefusals"),
            (CompositionTestCatalog.DuplicateManifest, "manifestRefusals"),
            (CompositionTestCatalog.IncompatibleManifest, "manifestRefusals"),
            (CompositionTestCatalog.MissingDependency, "dependencyRefusals"),
            (CompositionTestCatalog.DisabledDependency, "dependencyRefusals"),
            (CompositionTestCatalog.IncompatibleDependency, "dependencyRefusals"),
            (CompositionTestCatalog.PrereleaseDependency, "dependencyRefusals"),
            (CompositionTestCatalog.DependencyCycle, "dependencyRefusals")
        };
        foreach (var (catalog, observation) in frozenCases)
        {
            var item = DeploymentProgram.Frozen(DeploymentProgram.FrozenName(catalog)!);
            var selection = DeploymentProgram.Selection(catalog, fixture.Canary);
            var author = ResolveAuthor(item.Deployments, item.Selection, token);
            RequireFrozen(evidence, item, author);
            var host = await ResolveHostAsync(item.Deployments, ModuleSelectionReader.Parse(selection), token);
            RequireFrozen(evidence, item, host);
            evidence.Require(author.Snapshot == host.Snapshot, "composition_test_public_paths_differ");
            await SupervisedAsync(catalog, selection, item.ExpectedCodes);
            evidence.Observed(observation);
        }

        foreach (var (settings, field, reason) in new (object, string?, ModuleSettingFailureReason)[]
        {
            (new Dictionary<string, object> { ["secret"] = fixture.Canary, ["private-unknown-key"] = fixture.Canary },
                null, ModuleSettingFailureReason.Unknown),
            (new { }, "secret", ModuleSettingFailureReason.Required),
            (new { secret = 1 }, "secret", ModuleSettingFailureReason.Type),
            (new { secret = "" }, "secret", ModuleSettingFailureReason.Constraint)
        })
        {
            var selection = DeploymentProgram.ConfiguredSelection(settings, fixture.Canary);
            var catalog = DeploymentProgram.Catalog(CompositionTestCatalog.Default);
            var diagnostics = new CompositionDiagnostics(CompositionMode.Validate, "none", _ => { });
            var rejected = false;
            try
            {
                await CompositionPreflight.ResolveAsync(catalog, ModuleSelectionReader.Parse(selection),
                    new(diagnostics, token), diagnostics, token);
            }
            catch (CompositionPreflightException error)
            {
                evidence.Require(error.Code == "composition_setting_invalid" && error.Diagnostics.Count == 0 &&
                    !error.SettingsTruncated && error.Settings.Count == 1 &&
                    error.Settings[0].Owner.Value == DeploymentProgram.Owner && error.Settings[0].Field == field &&
                    error.Settings[0].Reason == reason, "composition_test_setting_diagnostic_mismatch");
                rejected = true;
            }
            evidence.Require(rejected, "composition_test_setting_accepted");
            await SupervisedAsync(CompositionTestCatalog.Default, selection, ["composition_setting_invalid"], field, reason);
            evidence.Observed("settingsRefusals");
            var corrected = DeploymentProgram.Selection(CompositionTestCatalog.Default, fixture.Canary);
            var correction = await ResolveHostAsync(catalog, ModuleSelectionReader.Parse(corrected), token);
            evidence.Require(correction.Accepted?.Order.Select(id => id.Value).SequenceEqual(new[] { DeploymentProgram.Owner }) == true,
                "composition_test_setting_correction_rejected");
            await SupervisedAsync(CompositionTestCatalog.Default, corrected, []);
        }

        foreach (var catalog in new[]
        {
            CompositionTestCatalog.MismatchedClaim, CompositionTestCatalog.ReservedClaim,
            CompositionTestCatalog.OverlappingClaim, CompositionTestCatalog.UnsupportedClaim,
            CompositionTestCatalog.CatalogLimit, CompositionTestCatalog.EnabledLimit,
            CompositionTestCatalog.ManifestByteLimit, CompositionTestCatalog.ClaimLimit
        })
        {
            var deployments = DeploymentProgram.Catalog(catalog);
            var selection = DeploymentProgram.Selection(catalog, fixture.Canary);
            var expected = catalog switch
            {
                CompositionTestCatalog.UnsupportedClaim => "composition_contribution_unsupported",
                CompositionTestCatalog.CatalogLimit or CompositionTestCatalog.EnabledLimit or
                    CompositionTestCatalog.ManifestByteLimit or CompositionTestCatalog.ClaimLimit => "composition_input_limit",
                _ => "composition_contribution_conflict"
            };
            var parsed = ModuleSelectionReader.Parse(selection);
            var author = ResolveAuthor(deployments, parsed.Modules.Select(entry =>
                new ModuleSelection(entry.Owner.Value, entry.Enabled)), token);
            RequireCodes(evidence, author, [expected]);
            var host = await ResolveHostAsync(deployments, parsed, token);
            RequireCodes(evidence, host, [expected]);
            evidence.Require(author.Snapshot == host.Snapshot, "composition_test_public_paths_differ");
            await SupervisedAsync(catalog, selection, [expected]);
            evidence.Observed(expected == "composition_input_limit" ? "limitRefusals" : "claimRefusals");
        }

        foreach (var invalidSelection in new[]
        {
            new byte[] { 0xff },
            DeploymentProgram.Selection([new(DeploymentProgram.Owner, true), new(DeploymentProgram.Owner, false)]),
            System.Text.Encoding.UTF8.GetBytes(
                $$$"""{"schemaVersion":"1","modules":[{"id":"{{{DeploymentProgram.Owner}}}","enabled":true,"settings":{"secret":"{{{fixture.Canary}}}","secret":"{{{fixture.Canary}}}"}}]}""")
        })
        {
            var rejected = false;
            try { ModuleSelectionReader.Parse(invalidSelection); }
            catch (ModuleSelectionException) { rejected = true; }
            evidence.Require(rejected, "composition_test_invalid_selection_accepted");
            await SupervisedAsync(CompositionTestCatalog.Default, invalidSelection, ["composition_configuration_invalid"]);
        }

        // Retry changes only prerequisite enablement or the conflicting route declaration.
        await SupervisedAsync(CompositionTestCatalog.DisabledDependency,
            DeploymentProgram.Selection([new("org.example.a", true), new("org.example.b", true)]), []);
        await SupervisedAsync(CompositionTestCatalog.CorrectedClaim,
            DeploymentProgram.Selection(CompositionTestCatalog.CorrectedClaim, fixture.Canary), []);
        await RequireAcceptedAsync(DeploymentProgram.Catalog(CompositionTestCatalog.CatalogLimit).Take(32).ToArray(), []);
        await RequireAcceptedAsync(DeploymentProgram.Catalog(CompositionTestCatalog.EnabledLimit).Take(16).ToArray(),
            Enumerable.Range(0, 16).Select(index => new ModuleSelection($"org.example.m{index}", true)).ToArray());
        await RequireAcceptedAsync([DeploymentProgram.Padded(65_536)], [new(DeploymentProgram.Owner, true)]);
        evidence.Require(DeploymentProgram.MutatingCallbacks == 0 && DeploymentProgram.DisabledValidators == 0,
            "composition_test_preflight_callback_invoked");
        return evidence;

        async Task RequireAcceptedAsync(IReadOnlyList<ModuleDeployment> catalog, ModuleSelection[] selected)
        {
            var author = ResolveAuthor(catalog, selected, token);
            var host = await ResolveHostAsync(catalog, ModuleSelectionReader.Parse(DeploymentProgram.Selection(selected)), token);
            evidence.Require(author.Accepted is not null && host.Accepted is not null &&
                author.Snapshot == host.Snapshot, "composition_test_inclusive_limit_rejected");
        }

        async Task SupervisedAsync(CompositionTestCatalog catalog, byte[] selection, IReadOnlyList<string> expected,
            string? fieldToken = null, ModuleSettingFailureReason? fieldReason = null)
        {
            await fixture.WriteSelectionAsync(selection, token);
            var process = fixture.Start("validate", catalog);
            var output = await process.WaitAsync(token);
            evidence.Require(process.ExitVerified && process.DescendantExitsVerified,
                "composition_test_validate_exit_unverified");
            evidence.Require(output.ExitCode == (expected.Count == 0 ? 0 : 2) && output.Stderr.Length == 0,
                "composition_test_validate_outcome_mismatch");
            var attempt = RequireRecords(evidence, output.Stdout, provider, expected, fieldToken, fieldReason);
            evidence.Require(attempts.Add(attempt), "composition_test_retry_attempt_reused");
            evidence.Require(before == await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token),
                "composition_test_provider_inventory_changed");
            evidence.Observed("zeroEffectComparisons");
        }
    }

    private static Decision ResolveAuthor(IReadOnlyList<ModuleDeployment> catalog,
        IEnumerable<ModuleSelection> selected, CancellationToken token) =>
        CompositionResolver.Resolve(catalog, selected, new("1.0.0"), token) switch
        {
            CompositionResolution.Accepted accepted => new(accepted.Composition, []),
            CompositionResolution.Rejected rejected => new(null, rejected.Diagnostics),
            _ => throw new ScenarioFailure("composition_test_author_decision_missing")
        };

    private static async Task<Decision> ResolveHostAsync(IReadOnlyList<ModuleDeployment> catalog,
        ModuleSelectionDocument selected, CancellationToken token)
    {
        var diagnostics = new CompositionDiagnostics(CompositionMode.Validate, "none", _ => { });
        try
        {
            var result = await CompositionPreflight.ResolveAsync(catalog, selected,
                new(diagnostics, token), diagnostics, token);
            return new(result.Composition, []);
        }
        catch (CompositionPreflightException error) when (error.Diagnostics.Count != 0 && error.Settings.Count == 0)
        {
            return new(null, error.Diagnostics);
        }
    }

    private static void RequireFrozen(ScenarioEvidence evidence, CompositionCorpusCase expected, Decision actual)
    {
        if (expected.ExpectedCodes.Count == 0)
        {
            evidence.Require(actual.Accepted is not null && actual.Diagnostics.Count == 0,
                "composition_test_expected_acceptance");
            var accepted = actual.Accepted!;
            evidence.Require(expected.ExpectedOrder.SequenceEqual(accepted.Order.Select(id => id.Value), StringComparer.Ordinal) &&
                accepted.Deployed.Count == expected.Deployments.Count &&
                expected.Selection.Where(item => item.Enabled).Select(item => item.Id).Order(StringComparer.Ordinal)
                    .SequenceEqual(accepted.Enabled.Select(id => id.Value).Order(StringComparer.Ordinal), StringComparer.Ordinal),
                "composition_test_canonical_order_mismatch");
        }
        else
        {
            RequireCodes(evidence, actual, expected.ExpectedCodes);
            evidence.Require(expected.ExpectedOwners.SequenceEqual(actual.Diagnostics.Select(item => item.Owner?.Value)) &&
                expected.ExpectedRelated.Select(ids => string.Join(',', ids)).SequenceEqual(
                    actual.Diagnostics.Select(item => string.Join(',', item.RelatedIds.Select(id => id.Value)))),
                "composition_test_ordered_diagnostics_mismatch");
            var nested = actual.Diagnostics.Where(item => item.ManifestDiagnostic is not null)
                .Select(item => item.ManifestDiagnostic!).ToArray();
            evidence.Require(expected.ExpectedManifestCodes.SequenceEqual(nested.Select(item => item.Code)) &&
                expected.ExpectedManifestPaths.SequenceEqual(nested.Select(item => item.Path)) &&
                nested.All(item => item.Origin == "manifest"),
                "composition_test_nested_manifest_diagnostics_mismatch");
        }
        evidence.Require(!actual.Snapshot.Contains("PRIVATE/path", StringComparison.Ordinal),
            "composition_test_diagnostic_input_leaked");
    }

    private static void RequireCodes(ScenarioEvidence evidence, Decision actual, IReadOnlyList<string> expected) =>
        evidence.Require(actual.Accepted is null && expected.SequenceEqual(actual.Diagnostics.Select(item => item.Code)),
            "composition_test_rejection_codes_mismatch");

    private static string RequireRecords(ScenarioEvidence evidence, string output, string provider,
        IReadOnlyList<string> expected, string? fieldToken = null, ModuleSettingFailureReason? fieldReason = null)
    {
        var records = new List<JsonElement>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            evidence.Require(line.Length <= 4096, "composition_test_operator_record_limit");
            using var document = JsonDocument.Parse(line, new JsonDocumentOptions { MaxDepth = 8, AllowDuplicateProperties = false });
            records.Add(document.RootElement.Clone());
        }
        evidence.Require(records.Count is > 0 and <= 257, "composition_test_operator_records_missing");
        var attempt = records[0].GetProperty("attempt").GetString()!;
        evidence.Require(attempt.Length == 32 && attempt.All(value => value is >= '0' and <= '9' or >= 'a' and <= 'f'),
            "composition_test_operator_attempt_invalid");
        foreach (var record in records)
        {
            evidence.Require(record.GetProperty("version").GetInt32() == 1 &&
                record.GetProperty("attempt").GetString() == attempt && record.GetProperty("mode").GetString() == "validate" &&
                record.GetProperty("provider").GetString() == (provider == "sqlite" ? "sqlite" : "postgresql") &&
                record.GetProperty("elapsedMilliseconds").GetInt64() is >= 0 and <= 45_000 &&
                record.GetProperty("stage").GetString() is "input" or "admission" or "discovered" or "validated" or "cleanup",
                "composition_test_operator_record_mismatch");
            var setting = record.GetProperty("stage").GetString() == "validated" &&
                record.GetProperty("outcome").GetString() == "rejected" &&
                record.GetProperty("code").GetString() == "composition_setting_invalid";
            evidence.Require(record.GetProperty("fieldToken").GetString() == (setting ? fieldToken : null) &&
                record.GetProperty("fieldReason").GetString() ==
                    (setting && fieldToken is not null ? fieldReason?.ToString().ToLowerInvariant() : null) &&
                (!setting || record.GetProperty("owner").GetString() == DeploymentProgram.Owner),
                "composition_test_operator_setting_fields_mismatch");
        }
        var refusals = records.Where(record => record.GetProperty("stage").GetString() is "validated" or "input" &&
            record.GetProperty("outcome").GetString() == "rejected").Select(record => record.GetProperty("code").GetString());
        evidence.Require(expected.SequenceEqual(refusals), "composition_test_supervised_rejections_mismatch");
        var terminal = records[^1];
        evidence.Require((terminal.GetProperty("stage").GetString() == "cleanup" ||
                terminal.GetProperty("stage").GetString() == "input" &&
                expected.Count == 1 && expected[0] is "composition_input_limit" or "composition_configuration_invalid") &&
            terminal.GetProperty("outcome").GetString() == (expected.Count == 0 ? "passed" : "rejected") &&
            terminal.GetProperty("code").GetString() == (expected.FirstOrDefault() ?? "composition_progress") &&
            !output.Contains("private-unknown-key", StringComparison.Ordinal),
            "composition_test_operator_terminal_mismatch");
        return attempt;
    }
}
