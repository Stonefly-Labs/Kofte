using System.Diagnostics;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Kofte.TestSupport;

public sealed class StoragePhaseBarrier : IObserver<DiagnosticListener>,
    IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private readonly string phase;
    private readonly bool hold;
    private readonly List<IDisposable> subscriptions = [];
    private readonly IDisposable all;
    private int reached;

    public StoragePhaseBarrier(string phase, bool hold = false)
    {
        this.phase = phase;
        this.hold = hold;
        all = DiagnosticListener.AllListeners.Subscribe(this);
    }

    public void OnNext(DiagnosticListener listener)
    {
        if (listener.Name == "Kofte.Storage") subscriptions.Add(listener.Subscribe(this));
    }

    public void OnNext(KeyValuePair<string, object?> item)
    {
        if (item.Key != "storage.phase" || item.Value is null) return;
        var value = JsonSerializer.SerializeToElement(item.Value);
        Hit(value.GetProperty("phase").GetString() ?? "");
    }

    public void ObserveNativeSql(SqliteConnection connection)
    {
        if (phase != "migration_sql_executing") return;
        SQLitePCL.raw.sqlite3_trace(connection.Handle, (SQLitePCL.strdelegate_trace)((_, sql) =>
        {
            if (sql.TrimStart().StartsWith("BEGIN", StringComparison.OrdinalIgnoreCase))
                Hit("migration_sql_executing");
        }), this);
    }

    private void Hit(string current)
    {
        if (current != phase || Interlocked.CompareExchange(ref reached, 1, 0) != 0) return;
        Console.WriteLine(JsonSerializer.Serialize(new { eventCode = "storage_worker_phase", phase }));
        Console.Out.Flush();
        if (hold) Thread.Sleep(Timeout.Infinite);
        if (Console.In.ReadLine() != "continue")
        {
            Console.Error.WriteLine("""{"eventCode":"storage_worker_control_failed"}""");
            Environment.Exit(42);
        }
    }

    public void OnCompleted() { }
    public void OnError(Exception error) => throw new InvalidOperationException("storage_phase_observer_failed");
    public void Dispose()
    {
        all.Dispose();
        foreach (var subscription in subscriptions) subscription.Dispose();
    }
}
