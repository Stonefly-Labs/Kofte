using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Storage;
using Kofte.TestSupport;

namespace Kofte.Storage.IntegrationTests.Registry;

[CollectionDefinition("registration_concurrency", DisableParallelization = true)]
public sealed class RegistrationConcurrencyCollection;

[Collection("registration_concurrency")]
public abstract class RegistrationConcurrencyTests
{
    protected abstract string Provider { get; }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TwentyRoundsOfTenIndependentConnectionsKeepActualOwnershipUnique(bool same)
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, TestContext.Current.CancellationToken);
        for (var round = 0; round < 20; round++)
        {
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var tasks = Enumerable.Range(0, 10).Select(index => Task.Run(async () =>
            {
                var installation = fixture.NewInstallation();
                await gate.Task;
                using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
                deadline.CancelAfter(TimeSpan.FromSeconds(45));
                return await RegistrationWorker.RegisterWithRetryAsync(installation,
                    RegistryFixture.Module(RegistrationWorker.Owner(same, round, index)), deadline.Token);
            }, TestContext.Current.CancellationToken)).ToArray();
            gate.SetResult();
            var results = await Task.WhenAll(tasks);
            Assert.Equal(same ? 1 : 10, results.Select(r => r.Assignment.AllocationId).Distinct().Count());
            Assert.Equal(same ? 1 : 10, results.Select(r => r.Assignment.AllocationKey).Distinct().Count());
            await AssertRoundAsync(fixture, same, round);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TwentyRoundsOfTenSeparateProcessesKeepActualOwnershipUnique(bool same)
    {
        await using var fixture = await RegistryFixture.CreateAsync(Provider, TestContext.Current.CancellationToken);
        for (var round = 0; round < 20; round++)
        {
            var children = new List<(OwnedProcess Child, StreamReader Reader)>();
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(60));
            try
            {
                for (var index = 0; index < 10; index++)
                {
                    var start = Start(fixture, ["--registration-worker", same ? "same" : "distinct",
                        round.ToString(CultureInfo.InvariantCulture), index.ToString(CultureInfo.InvariantCulture)]);
                    var child = OwnedProcesses.Start(start, captureOutput: false);
                    var reader = new StreamReader(child.StandardOutput);
                    children.Add((child, reader));
                }
                await Task.WhenAll(children.Select(async pair =>
                {
                    var line = await pair.Reader.ReadLineAsync(deadline.Token);
                    using var waiting = JsonDocument.Parse(line ?? throw new InvalidOperationException("storage_worker_missing"));
                    Assert.True(waiting.RootElement.GetProperty("eventCode").GetString() != "storage_worker_failed",
                        Failure(waiting.RootElement));
                    Assert.Equal("storage_worker_waiting", waiting.RootElement.GetProperty("eventCode").GetString());
                }));
                foreach (var (child, _) in children)
                {
                    await child.StandardInput.WriteLineAsync("start");
                    await child.StandardInput.FlushAsync(deadline.Token);
                }
                var results = await Task.WhenAll(children.Select(async pair =>
                {
                    var line = await pair.Reader.ReadLineAsync(deadline.Token);
                    var output = await pair.Child.WaitAsync(deadline.Token);
                    using var document = JsonDocument.Parse(line ?? throw new InvalidOperationException("storage_worker_missing"));
                    Assert.True(output.ExitCode == 0, Failure(document.RootElement));
                    Assert.Equal("storage_worker_ready", document.RootElement.GetProperty("eventCode").GetString());
                    return (Key: document.RootElement.GetProperty("key").GetString(),
                        Allocation: document.RootElement.GetProperty("allocation").GetGuid());
                }));
                Assert.Equal(same ? 1 : 10, results.Select(r => r.Allocation).Distinct().Count());
                Assert.Equal(same ? 1 : 10, results.Select(r => r.Key).Distinct().Count());
                await AssertRoundAsync(fixture, same, round);
            }
            finally
            {
                foreach (var (child, reader) in children)
                {
                    await child.DisposeAsync();
                    reader.Dispose();
                }
            }
        }
    }

    private static string Failure(JsonElement result)
    {
        var code = result.TryGetProperty("code", out var value) &&
            StorageDiagnostics.IsFailureCode(value.GetString()) ? value.GetString() : "storage_worker_failed";
        var operation = result.TryGetProperty("operation", out var phase) &&
            phase.GetString() is "acquire" or "register" ? phase.GetString() : "setup";
        return $"{code}; operation={operation}";
    }

    private static async Task AssertRoundAsync(RegistryFixture fixture, bool same, int round)
    {
        var expected = (round + 1) * (same ? 1 : 10);
        await fixture.AssertInventoryAsync(expected, expected, TestContext.Current.CancellationToken);
        var allocations = new HashSet<Guid>();
        for (var index = 0; index < (same ? 1 : 10); index++)
        {
            var module = RegistryFixture.Module(RegistrationWorker.Owner(same, round, index));
            var result = Assert.IsType<StorageResult.Ready>(
                await fixture.Registry.ResolveAsync(module.Id, TestContext.Current.CancellationToken));
            Assert.True(allocations.Add(result.Assignment.AllocationId));
        }
    }

    internal static ProcessStartInfo Start(RegistryFixture fixture, string[] args)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = OwnedProcesses.FindRepository()
        };
        start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        foreach (var argument in args) start.ArgumentList.Add(argument);
        OwnedProcesses.ClearSensitiveEnvironment(start);
        start.Environment["KOFTE_STORAGE_TEST_PROVIDER"] = fixture.Provider;
        if (fixture.Root is { } root) start.Environment["KOFTE_DATA_ROOT"] = root;
        if (fixture.RuntimeConnection is { } runtime) start.Environment["KOFTE_POSTGRES_CONNECTION"] = runtime;
        if (fixture.ProvisioningConnection is { } provisioner)
            start.Environment["KOFTE_POSTGRES_PROVISIONING_CONNECTION"] = provisioner;
        return start;
    }
}

[Trait("Provider", "sqlite")]
public sealed class SqliteRegistrationConcurrencyTests : RegistrationConcurrencyTests
{
    protected override string Provider => "sqlite";
}

[Trait("Provider", "postgres")]
public sealed class PostgresRegistrationConcurrencyTests : RegistrationConcurrencyTests
{
    protected override string Provider => "postgres";
}
