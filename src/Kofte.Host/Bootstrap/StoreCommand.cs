namespace Kofte.Host.Bootstrap;

public static class StoreCommand
{
    public static async Task RunAsync(BootstrapConfiguration config, string[] args)
    {
        if (config.Profile != "local-sqlite")
            throw new BootstrapException(BootstrapError.CommandInvalid);
        var store = new SqlitePlatformStore(config.DataRoot);
        if (args is ["--store", "init"])
            await store.InitializeAsync();
        else throw new BootstrapException(BootstrapError.CommandInvalid);
        Console.WriteLine("""{"eventCode":"store_initialized","component":"storage"}""");
    }
}
