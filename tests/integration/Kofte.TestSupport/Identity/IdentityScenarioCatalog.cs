namespace Kofte.TestSupport.Identity;

public static class IdentityScenarioCatalog
{
    public static IReadOnlyList<string> Scenarios { get; } = Array.AsReadOnly(new[]
    {
        "oidc-login", "trust-refusal", "nominated-enrollment", "enrollment-race",
        "session-boundary", "development-isolation", "upgrade-preservation",
        "interrupted-recovery", "private-evidence"
    });

    public static string RequireScenario(string value) =>
        value == "all" || Scenarios.Contains(value, StringComparer.Ordinal)
            ? value : throw new InvalidOperationException("identity_scenario_required");

    public static string RequireProvider(string value) => StorageScenarioCatalog.RequireProvider(value);
}
