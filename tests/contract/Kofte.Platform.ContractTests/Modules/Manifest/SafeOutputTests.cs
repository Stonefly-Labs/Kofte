using System.Diagnostics;
using System.Text;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.ContractTests.Modules.Manifest;

public sealed class SafeOutputTests
{
    [Fact]
    public async Task UnpredictableValuesKeysAndArgumentsNeverEnterDiagnosticsOrOperationalOutput()
    {
        var marker = "private-" + Guid.NewGuid().ToString("N");
        var inputs = new[] {
            ManifestCorpus.Field("known", "displayName", marker + "\n", "display_name_invalid"),
            ManifestCorpus.Field("id", "id", marker, "module_id_invalid"),
            ManifestCorpus.Field("version", "version", marker, "version_invalid"),
            ManifestCorpus.Field("range", "hostContract", marker, "range_invalid"),
            new ManifestCase("key", ManifestCorpus.Change(n => n[marker + "\n"] = true), Code: "field_unknown"),
            new ManifestCase("context", ManifestCorpus.Minimal, Host: marker, Code: "host_context_invalid",
                Path: "/hostContractVersion", Origin: "context")
        };
        foreach (var item in inputs)
        {
            using var capture = new StringWriter();
            var stdout = Console.Out;
            var stderr = Console.Error;
            ManifestValidationResult result;
            try
            {
                Console.SetOut(capture);
                Console.SetError(capture);
                result = ManifestValidator.Validate(item.Bytes, new(item.Host));
            }
            finally { Console.SetOut(stdout); Console.SetError(stderr); }
            Assert.Equal("", capture.ToString());
            var rejected = Assert.IsType<ManifestValidationResult.Rejected>(result);
            foreach (var diagnostic in rejected.Diagnostics)
            {
                Assert.DoesNotContain(marker, diagnostic.Message + diagnostic.Path + diagnostic.Code);
                Assert.DoesNotContain('\n', diagnostic.Message);
                Assert.InRange(diagnostic.Message.Length, 1, 160);
            }
            var output = await ConsumerHarness.Author(item.Bytes, ConsumerHarness.Arguments(item));
            Assert.Equal(2, output.ExitCode);
            Assert.DoesNotContain(marker, output.Stdout + output.Stderr);
            Assert.DoesNotContain("Exception", output.Stdout + output.Stderr);
            Assert.DoesNotContain(ConsumerHarness.Root, output.Stdout + output.Stderr);
            ConsumerHarness.Expected(item, ConsumerHarness.Parse(output.Stdout));
        }
        var invalid = await ConsumerHarness.Author([], "--" + marker, marker);
        Assert.Equal(2, invalid.ExitCode);
        Assert.Equal("", invalid.Stdout);
        Assert.Equal("invocation_invalid", invalid.Stderr.Trim());
    }

    [Fact]
    public async Task InvocationAndBoundedReadFailuresRemainNonzero()
    {
        foreach (var arguments in new[] {
            Array.Empty<string>(), new[] { "--host-contract" },
            new[] { "--host-contract", "1.0.0", "--host-contract", "1.0.0" },
            new[] { "--host-contract", "1.0.0", "--repeat", "101" },
            new[] { "--host-contract", "1.0.0", "--culture", "unknown" } })
        {
            var output = await ConsumerHarness.Author([], arguments);
            Assert.Equal(2, output.ExitCode);
            Assert.Equal("invocation_invalid", output.Stderr.Trim());
        }
        var oversized = await ConsumerHarness.Author(Enumerable.Repeat((byte)' ', 65537).ToArray(),
            "--host-contract", "1.0.0");
        Assert.Equal(2, oversized.ExitCode);
        Assert.Equal(("manifest", "", "input_too_large"), Assert.Single(ConsumerHarness.Parse(oversized.Stdout).Diagnostics));
    }

    [Fact]
    public async Task ActualUnreadableInputIsAnOperationalIoFailure()
    {
        Assert.True(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS());
        var dll = Path.Combine(ConsumerHarness.Root, "tests/contract/Kofte.Manifest.Consumer/bin/Release/net10.0/Kofte.Manifest.Consumer.dll");
        // The shell only redirects this owned process's stdin to a directory; no input is evaluated.
        var start = new ProcessStartInfo("/bin/sh")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("exec dotnet \"$1\" --host-contract 1.0.0 < \"$2\"");
        start.ArgumentList.Add("manifest-io-fixture");
        start.ArgumentList.Add(dll);
        start.ArgumentList.Add(ConsumerHarness.Root);
        using var child = Process.Start(start)!;
        var output = child.StandardOutput.ReadToEndAsync(TestContext.Current.CancellationToken);
        var error = child.StandardError.ReadToEndAsync(TestContext.Current.CancellationToken);
        try
        {
            await child.WaitForExitAsync(TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);
        }
        finally { if (!child.HasExited) child.Kill(entireProcessTree: true); }
        Assert.Equal(1, child.ExitCode);
        Assert.Equal("", await output);
        Assert.Equal("consumer_io_failure", (await error).Trim());
    }
}
