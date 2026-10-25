namespace Kofte.TestSupport;

public static class CompositionScenarioCatalog
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> requirements =
        new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.Ordinal)
        {
            ["empty-boot"] = Required(("coreEndpoints", 2), ("emptyInventoryComparisons", 2)),
            ["valid-dependency"] = Required(("publicServiceComparisons", 2), ("phaseOrderComparisons", 1),
                ("ownerHistoryComparisons", 2), ("prepublicationComparisons", 1), ("dependencyIdentityComparisons", 1),
                ("prerequisiteServiceComparisons", 127)),
            ["disabled-missing"] = Required(("disabledCallbackComparisons", 1), ("missingRefusals", 1),
                ("unchangedInventories", 1), ("explicitDisabledObservations", 2),
                ("omittedDisabledObservations", 2), ("freshDisabledAttempts", 2)),
            ["invalid-composition"] = Required(("manifestRefusals", 3), ("dependencyRefusals", 5),
                ("settingsRefusals", 1), ("claimRefusals", 4), ("limitRefusals", 1),
                ("zeroEffectComparisons", 1)),
            ["deterministic-corpus"] = Required(("repetitionsPerPermutation", 100),
                ("permutations", 10), ("cultures", 2), ("publicConsumerPaths", 2),
                ("exactDecisionComparisons", 4000)),
            ["ordered-migration"] = Required(("cleanHistoryComparisons", 2), ("upgradeHistoryComparisons", 2),
                ("originalDataComparisons", 2), ("wholeSetPreflightComparisons", 1),
                ("noStorageComparisons", 1), ("unchangedServingRevisions", 2)),
            ["disable-reenable"] = Required(("completeCycles", 3), ("distinctProcesses", 6),
                ("distinctAttempts", 6), ("originalAssignmentComparisons", 12),
                ("originalDataComparisons", 12), ("originalHistoryComparisons", 12),
                ("unrelatedStateComparisons", 6), ("dependentDisableRefusals", 1)),
            ["failure-correction"] = Required(("configurationCorrections", 1), ("initializeCorrections", 1),
                ("startCorrections", 1), ("healthCorrections", 1), ("migrationCorrections", 1),
                ("freshAttemptComparisons", 5), ("verifiedExits", 5), ("retainedCommitComparisons", 1),
                ("explicitRecoveryComparisons", 1)),
            ["health-impact"] = Required(("ownStateComparisons", 4), ("freshnessComparisons", 1),
                ("transitiveImpactComparisons", 1), ("independentRootComparisons", 1),
                ("aggregateResponseComparisons", 2), ("anonymousPrivacyComparisons", 1)),
            ["bounded-stop"] = Required(("ignoringTokenBoundaries", 7), ("cancellationBoundaries", 2),
                ("authorityRefusals", 2), ("reverseCleanupComparisons", 1), ("verifiedRootExits", 1),
                ("verifiedDescendantExits", 1), ("unverifiedTerminationRefusals", 1),
                ("elapsedBudgetComparisons", 1)),
            ["private-diagnostics"] = Required(("capturedChannels", 6), ("usefulRecordComparisons", 1),
                ("zeroLeakComparisons", 1), ("invalidFrameRefusals", 3), ("outputLimitRefusals", 2),
                ("sinkFailureRefusals", 1), ("storageDurabilityComparisons", 1),
                ("boundedLabelComparisons", 1))
        }.AsReadOnly();

    public static IReadOnlyList<string> Scenarios { get; } = Array.AsReadOnly(requirements.Keys.ToArray());

    public static string RequireProvider(string? provider) => provider switch
    {
        "sqlite" or "postgres" => provider,
        _ => throw new InvalidOperationException("composition_test_provider_required")
    };

    public static string RequireScenario(string? scenario) =>
        scenario is not null && (scenario == "all" || requirements.ContainsKey(scenario))
            ? scenario : throw new InvalidOperationException("composition_scenario_required");

    public static IReadOnlyDictionary<string, int> RequiredObservations(string scenario) =>
        requirements.TryGetValue(scenario, out var required)
            ? required : throw new InvalidOperationException("composition_scenario_required");

    private static IReadOnlyDictionary<string, int> Required(params (string Name, int Minimum)[] values) =>
        values.ToDictionary(item => item.Name, item => item.Minimum, StringComparer.Ordinal).AsReadOnly();
}
