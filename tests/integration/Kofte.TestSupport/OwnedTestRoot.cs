namespace Kofte.TestSupport;

public sealed class OwnedTestRoot : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(
        OwnedProcesses.FindRepository(), "artifacts", "test-runs", "kofte-test-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (!Directory.Exists(Path)) return;
        if (new DirectoryInfo(Path).LinkTarget is not null)
            throw new InvalidOperationException("test_root_cleanup_refused");
        Directory.Delete(Path, recursive: true);
    }
}
