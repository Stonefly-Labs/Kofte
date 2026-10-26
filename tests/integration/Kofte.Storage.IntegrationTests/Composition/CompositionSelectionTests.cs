using System.Text.Json;
using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Composition;

public abstract class CompositionSelectionTests
{
    protected abstract string Provider { get; }

    [Fact]
    public async Task EnabledDependentWithDisabledPrerequisiteRefusesBeforeAnyAuthorOrStorageMutation()
    {
        var token = TestContext.Current.CancellationToken;
        await using var fixture = await CompositionFixture.CreateAsync(Provider, token);
        await fixture.SeedUnrelatedNoStorageAsync(token);
        await fixture.WriteSelectionAsync("""
            {"schemaVersion":"1","modules":[
             {"id":"org.example.alpha","enabled":false,"settings":{}},
             {"id":"org.example.beta","enabled":true,"settings":{}},
             {"id":"org.example.no-storage","enabled":true,"settings":{}}]}
            """u8.ToArray(), token);
        var original = await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token);
        await using var process = fixture.Start("prepare", CompositionTestCatalog.RelationalCurrent);
        var result = await process.WaitAsync(token);
        Assert.Equal(2, result.ExitCode);
        Assert.True(process.ExitVerified && process.DescendantExitsVerified);
        Assert.Empty(result.Stderr);
        var records = result.Stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonSerializer.Deserialize<JsonElement>(line)).ToArray();
        Assert.Contains(records, record => record.GetProperty("code").GetString() == "composition_dependency_disabled");
        Assert.DoesNotContain(records, record => record.GetProperty("stage").GetString() is
            "registered" or "initialized" or "migrated" or "started" or "published");
        Assert.Equal(original, await CompositionStoreSnapshot.CaptureAsync(fixture.Storage, token));
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteCompositionSelectionTests : CompositionSelectionTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresCompositionSelectionTests : CompositionSelectionTests
{
    protected override string Provider => "postgres";
}
