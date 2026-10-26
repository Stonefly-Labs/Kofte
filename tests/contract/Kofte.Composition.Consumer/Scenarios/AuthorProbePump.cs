using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kofte.Composition.TestModule.Modules;
using Kofte.Host.Features.ModuleComposition;
using Kofte.Infrastructure.Composition;
using Kofte.Platform.Modules.Composition;

namespace Kofte.Composition.Consumer.Scenarios;

internal static partial class AuthorProbePump
{
    [GeneratedRegex(@"\Aartifacts/test-runs/kofte-test-[a-f0-9]{32}/p\z", RegexOptions.CultureInvariant)]
    private static partial Regex OwnedSocket();

    internal static async Task<int> RunWorkerAsync(string[] args, IReadOnlyList<ModuleDeployment> catalog,
        IReadOnlyList<ModuleStorageRecipe>? recipes = null)
    {
        var path = Environment.GetEnvironmentVariable("COMPOSITION_TEST_PROBE");
        if (path is null || !OwnedSocket().IsMatch(path))
            return await ModuleCompositionHost.RunAsync(args, catalog, recipes ?? []);
        using var budget = new CancellationTokenSource(TimeSpan.FromSeconds(115));
        var finished = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var pump = PumpAsync(path, finished.Task, budget.Token);
        var exit = await ModuleCompositionHost.RunAsync(args, catalog, recipes ?? []);
        finished.TrySetResult(exit);
        if (!AuthorProbe.Admitted.IsCompleted)
        {
            budget.Cancel();
            try { await pump; }
            catch (OperationCanceledException) when (budget.IsCancellationRequested) { }
            return exit;
        }
        try { await pump; }
        catch (Exception) { return 1; }
        return exit;
    }

    private static async Task PumpAsync(string path, Task<int> finished, CancellationToken token)
    {
        // Only a host-admitted author callback can release this passive queue.
        await AuthorProbe.Admitted.WaitAsync(token);
        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        await socket.ConnectAsync(new UnixDomainSocketEndPoint(path), token);
        await using var stream = new NetworkStream(socket, ownsSocket: false);
        var release = AuthorProbe.HoldBeta ? ReleaseAsync(stream, token) : Task.CompletedTask;
        var offset = 0;
        var sequence = 0;
        while (true)
        {
            if (release.IsFaulted) await release;
            var batch = AuthorProbe.ReadFrom(offset);
            foreach (var record in batch) await WriteAsync(record);
            offset += batch.Count;
            if (finished.IsCompleted && AuthorProbe.ReadFrom(offset).Count == 0) break;
            await Task.Delay(2, token);
        }
        if (AuthorProbe.HoldBeta) await release;
        foreach (var record in AuthorProbe.Totals()) await WriteAsync(record);
        var last = AuthorProbe.ReadFrom(0).Last();
        await WriteAsync(new("org.example.probe", "complete", await finished, last.Attempt, Environment.ProcessId));

        async Task WriteAsync(AuthorObservation record)
        {
            if (++sequence > 256) throw new InvalidOperationException("composition_author_probe_limit");
            var bytes = JsonSerializer.SerializeToUtf8Bytes(new
            {
                version = 1,
                owner = record.Owner,
                phase = record.Phase,
                value = record.Value,
                attempt = record.Attempt,
                worker = record.Worker,
                sequence
            });
            if (bytes.Length > 512) throw new InvalidOperationException("composition_author_probe_limit");
            var header = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(header, bytes.Length);
            await stream.WriteAsync(header, token);
            await stream.WriteAsync(bytes, token);
            await stream.FlushAsync(token);
        }
    }

    private static async Task ReleaseAsync(NetworkStream stream, CancellationToken token)
    {
        var command = new byte[1];
        await stream.ReadExactlyAsync(command, token);
        if (command[0] != 1) throw new InvalidOperationException("composition_author_probe_command_invalid");
        AuthorProbe.Release();
    }
}
