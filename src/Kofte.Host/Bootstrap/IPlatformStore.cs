namespace Kofte.Host.Bootstrap;

public interface IPlatformStore
{
    Task ProbeAsync(CancellationToken token);
}
