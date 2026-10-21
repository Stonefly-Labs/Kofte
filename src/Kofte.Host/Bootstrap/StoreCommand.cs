namespace Kofte.Host.Bootstrap;

public static class StoreCommand
{
    public static async Task RunAsync(BootstrapConfiguration config, string[] args)
    {
        if (config.Profile != "local-sqlite")
            throw new BootstrapException(BootstrapError.CommandInvalid);
        var store = new SqlitePlatformStore(config.DataRoot);
        string code;
        switch (args)
        {
            case ["--store", "init"]:
                await store.InitializeAsync(); code = "store_initialized"; break;
            case ["--store", "backup", "--output", var output]:
                store.Backup(output); code = "store_backed_up"; break;
            case ["--store", "restore", "--input", var input]:
                store.Restore(input); code = "store_restored"; break;
            case ["--store", "restore", "--input", var input, "--previous-backup", var previous, "--confirm-replace-platform-data"]:
                store.Restore(input, confirmReplacement: true, previousBackup: previous);
                code = "store_restored"; break;
            case ["--store", "reset", "--confirm-delete-platform-data"]:
                store.Reset(confirmed: true); code = "store_reset"; break;
            case ["--store", "reset"]:
                throw new BootstrapException(BootstrapError.ConfirmationRequired);
            default: throw new BootstrapException(BootstrapError.CommandInvalid);
        }
        Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new { eventCode = code, component = "storage" }));
    }
}
