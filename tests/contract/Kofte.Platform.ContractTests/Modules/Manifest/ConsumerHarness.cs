using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.ContractTests.Modules.Manifest;

internal sealed record Decision(string Status, (string Origin, string Path, string Code)[] Diagnostics);
internal sealed record ConsumerOutput(int ExitCode, string Stdout, string Stderr);

internal static class ConsumerHarness
{
    internal static string Root
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Kofte.slnx")))
                directory = directory.Parent;
            return directory?.FullName ?? throw new InvalidOperationException("repository_fixture_missing");
        }
    }
    internal static Decision Host(ManifestCase item) =>
        DecisionFor(ManifestValidator.Validate(item.Bytes, new(item.Host, item.Previous)));

    internal static Decision DecisionFor(ManifestValidationResult result) => result switch
    {
        ManifestValidationResult.Accepted => new("accepted", []),
        ManifestValidationResult.Rejected rejected => new("rejected",
            rejected.Diagnostics.Select(d => (d.Origin, d.Path, d.Code)).ToArray()),
        _ => throw new InvalidOperationException("result_variant_unknown")
    };

    internal static void Expected(ManifestCase item, Decision result)
    {
        Assert.Equal(item.IsAccepted ? "accepted" : "rejected", result.Status);
        Assert.Equal(item.ExpectedDiagnostics, result.Diagnostics);
    }

    internal static async Task<ConsumerOutput> Author(byte[] bytes, params string[] arguments)
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var dll = Path.Combine(Root, "tests/contract/Kofte.Manifest.Consumer/bin", configuration,
            "net10.0/Kofte.Manifest.Consumer.dll");
        Assert.True(File.Exists(dll), "author_consumer_not_built");
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Root
        };
        start.ArgumentList.Add(dll);
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        using var child = Process.Start(start) ?? throw new InvalidOperationException("consumer_start_failed");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var stdout = ReadBounded(child.StandardOutput, deadline.Token);
        var stderr = ReadBounded(child.StandardError, deadline.Token);
        try
        {
            try
            {
                await child.StandardInput.BaseStream.WriteAsync(bytes, deadline.Token);
                await child.StandardInput.BaseStream.FlushAsync(deadline.Token);
            }
            catch (IOException) when (bytes.Length > 65536) { /* Early bounded-read exit can close stdin. */ }
            child.StandardInput.Close();
            await child.WaitForExitAsync(deadline.Token);
            return new(child.ExitCode, await stdout, await stderr);
        }
        finally
        {
            if (!child.HasExited) child.Kill(entireProcessTree: true);
            await child.WaitForExitAsync();
        }
    }

    private static async Task<string> ReadBounded(StreamReader reader, CancellationToken token)
    {
        var chars = new char[1024];
        var text = new System.Text.StringBuilder();
        int count;
        while ((count = await reader.ReadAsync(chars.AsMemory(), token)) != 0)
        {
            if (text.Length + count > 1_000_000)
                throw new InvalidOperationException("consumer_output_limit");
            text.Append(chars, 0, count);
        }
        return text.ToString();
    }

    internal static string[] Arguments(ManifestCase item, string culture = "en-US", int repetitions = 1)
    {
        var args = new List<string> {
            "--host-contract", item.Host ?? "", "--culture", culture,
            "--repeat", repetitions.ToString(CultureInfo.InvariantCulture)
        };
        if (item.Previous is not null) args.AddRange(["--previous-id", item.Previous]);
        return args.ToArray();
    }

    internal static Decision Parse(string line)
    {
        using var doc = JsonDocument.Parse(line);
        var root = doc.RootElement;
        return new(root.GetProperty("status").GetString()!, root.TryGetProperty("diagnostics", out var diagnostics)
            ? diagnostics.EnumerateArray().Select(d => (d.GetProperty("origin").GetString()!,
                d.GetProperty("path").GetString()!, d.GetProperty("code").GetString()!)).ToArray() : []);
    }
}
