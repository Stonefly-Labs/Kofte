using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Kofte.Platform.Modules.Manifest;

namespace Kofte.Platform.ContractTests.Modules.Manifest;

public sealed class ManifestPerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public void EachOfOneHundredInitializedRepresentativeCallsIsBelowOneSecond()
    {
        var cases = new[] {
            new ManifestCase("minimal", ManifestCorpus.Minimal),
            new ManifestCase("full", File.ReadAllBytes(ManifestCorpus.Fixture("examples/full.json"))),
            ManifestCorpus.Field("invalid-id", "id", "BAD", "module_id_invalid"),
            new ManifestCase("oversized", new byte[65537], Code: "input_too_large"),
            new ManifestCase("deep", Encoding.UTF8.GetBytes("""{"x":[[[[[[[[0]]]]]]]]}"""), Code: "depth_limit")
        };
        foreach (var item in cases) ConsumerHarness.Host(item);
        var durations = new List<object>();
        for (var i = 0; i < 100; i++)
        {
            var item = cases[i % cases.Length];
            var start = Stopwatch.GetTimestamp();
            var result = ManifestValidator.Validate(item.Bytes, new("1.0.0"));
            var elapsed = Stopwatch.GetElapsedTime(start);
            Assert.True(elapsed < TimeSpan.FromSeconds(1), "initialized_call_exceeded_one_second");
            Assert.Equal(item.Code is null, result is ManifestValidationResult.Accepted);
            durations.Add(new { index = i, caseId = item.Name, milliseconds = elapsed.TotalMilliseconds });
        }
        var metadata = new
        {
            framework = RuntimeInformation.FrameworkDescription,
            os = RuntimeInformation.OSDescription,
            architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            logicalProcessors = Environment.ProcessorCount,
            availableMemoryBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes,
            durations
        };
        output.WriteLine(JsonSerializer.Serialize(metadata));
        var path = Environment.GetEnvironmentVariable("KOFTE_MANIFEST_TIMING_REPORT");
        if (path is not null)
        {
            // The test fixture owns this optional report; the production validator never performs IO.
            File.WriteAllText(path, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
