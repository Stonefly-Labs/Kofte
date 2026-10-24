using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text;
using Kofte.TestSupport;

namespace Kofte.Platform.ContractTests.Composition;

public sealed class CompositionBootContractTests
{
    [Theory]
    [InlineData("owner", "\"private-canary\"")]
    [InlineData("phase", "\"private-canary\"")]
    [InlineData("phase", "\"complete\"")]
    [InlineData("attempt", "\"private-canary\"")]
    [InlineData("worker", "0")]
    [InlineData("value", "1001")]
    [InlineData("sequence", "257")]
    [InlineData("version", "2")]
    [InlineData("unknown", "\"private-canary\"")]
    public void PrivateTraceRefusesUnboundedOrUndeclaredData(string field, string value)
    {
        var frame = JsonNode.Parse(
            """{"version":1,"owner":"org.example.alpha","phase":"compute","value":22,"attempt":"0123456789abcdef0123456789abcdef","worker":1,"sequence":1}""")!;
        frame[field] = JsonNode.Parse(value);
        var error = Assert.Throws<InvalidOperationException>(() =>
            CompositionFixture.CompositionAuthorTrace.ParseFrame(Encoding.UTF8.GetBytes(frame.ToJsonString())));
        Assert.Equal("composition_author_trace_invalid", error.Message);
        Assert.DoesNotContain("private-canary", error.Message);
    }

    [Fact]
    public void PrivateTraceRejectsDuplicateFieldsAndOversizedFrames()
    {
        Assert.Throws<InvalidOperationException>(() => CompositionFixture.CompositionAuthorTrace.ParseFrame(
            """{"version":1,"version":1}"""u8.ToArray()));
        Assert.Throws<InvalidOperationException>(() => CompositionFixture.CompositionAuthorTrace.ParseFrame(new byte[513]));
    }

}

public abstract class CompositionBootProviderContractTests(ITestOutputHelper output)
{
    protected abstract string Provider { get; }

    [Theory]
    [InlineData("empty-boot")]
    [InlineData("valid-dependency")]
    [InlineData("disabled-missing")]
    public async Task IndependentAuthorBootExecutesMeasuredPublicContract(string scenario)
    {
        var provider = Provider;
        var execution = await CompositionConsumerHarness.InvokeAsync(
            ["--provider", provider, "--scenario", scenario], TestContext.Current.CancellationToken);
        output.WriteLine(execution.Stdout);
        output.WriteLine(execution.Stderr);
        var result = CompositionConsumerHarness.RequireSuccess(execution, provider, scenario);
        foreach (var (name, minimum) in CompositionScenarioCatalog.RequiredObservations(scenario))
            Assert.True(result.Observations[scenario][name] >= minimum);
        output.WriteLine(JsonSerializer.Serialize(new { provider, scenario, result.Assertions, result.Observations }));
    }

    [Trait("Provider", "sqlite")]
    public sealed class SqliteCompositionBootProviderContractTests(ITestOutputHelper output) : CompositionBootProviderContractTests(output)
    {
        protected override string Provider => "sqlite";
    }

    [Trait("Provider", "postgres")]
    public sealed class PostgresCompositionBootProviderContractTests(ITestOutputHelper output) : CompositionBootProviderContractTests(output)
    {
        protected override string Provider => "postgres";
    }
}
