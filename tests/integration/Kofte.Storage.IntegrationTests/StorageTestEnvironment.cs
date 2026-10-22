using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests;

internal static class StorageTestEnvironment
{
    internal static string Provider => Parse(Environment.GetEnvironmentVariable("KOFTE_STORAGE_TEST_PROVIDER"));

    internal static string Parse(string? value) => StorageScenarioCatalog.RequireProvider(value);

    internal static Task<StorageProviderScope> CreateAsync(string provider, CancellationToken token) =>
        StorageProviderScope.CreateAsync(Parse(provider), token);

    internal static Task<StorageProviderScope> CreateFromEnvironmentAsync(CancellationToken token) =>
        CreateAsync(Provider, token);
}
