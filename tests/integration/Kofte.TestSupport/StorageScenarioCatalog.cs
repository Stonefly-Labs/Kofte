namespace Kofte.TestSupport;

public static class StorageScenarioCatalog
{
    public static string RequireProvider(string? provider) => provider switch
    {
        "sqlite" or "postgres" => provider,
        _ => throw new InvalidOperationException("storage_test_provider_required")
    };

    public static string RequireScenario(string? scenario) => scenario switch
    {
        "all" or "clean-install" or "no-storage" or "same-id-retry" or "collision" or
        "path-rejection" or "interrupted-provisioning" or "corrected-retry" or
        "isolated-upgrade" or "stopped-restore" => scenario,
        _ => throw new InvalidOperationException("storage_scenario_required")
    };
}
