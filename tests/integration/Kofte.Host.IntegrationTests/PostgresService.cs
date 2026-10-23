using Kofte.TestSupport;

namespace Kofte.Host.IntegrationTests;

internal static class PostgresService
{
    internal static string FindRepository() => OwnedProcesses.FindRepository();
}
