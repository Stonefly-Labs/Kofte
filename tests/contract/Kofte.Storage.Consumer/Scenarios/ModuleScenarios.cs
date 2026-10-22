using System.Text.Json;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Manifest;
using Kofte.Platform.Modules.Storage;
using Kofte.Storage.Consumer.Modules.Alpha;
using Kofte.Storage.Consumer.Modules.Beta;
using Microsoft.EntityFrameworkCore;

namespace Kofte.Storage.Consumer.Scenarios;

public static class ModuleScenarios
{
    public static async Task<int> RunAsync(StorageInstallation installation, IStorageMaintenanceScope scope,
        string scenario, CancellationToken token)
    {
        if (scenario is not ("clean-install" or "isolated-upgrade" or "path-rejection"))
            throw new InvalidOperationException("storage_scenario_invalid");
        var registry = new StorageRegistry(installation);
        var alphaModule = Module("org.example.consumer-alpha-" + scenario);
        var betaModule = Module("org.example.consumer-beta-" + scenario);
        Require(await registry.RegisterAsync(alphaModule, scope, token) is StorageResult.Ready);
        Require(await registry.RegisterAsync(betaModule, scope, token) is StorageResult.Ready);
        await using var alpha = await registry.BindAsync(alphaModule.Id, token);
        await using var beta = await registry.BindAsync(betaModule.Id, token);
        Require(alpha.Owner.Equals(alphaModule.Id) && beta.Owner.Equals(betaModule.Id));
        Require(alpha.Provider == installation.Provider);
        var assertions = 4;
        if (scenario == "path-rejection")
        {
            Require(await beta.MigrateAsync(BetaStorage.Create, scope, cancellationToken: token) is StorageResult.Ready);
            await using var context = await beta.CreateContextAsync(BetaStorage.Create, token);
            context.Records.Add(new Modules.Beta.Record { Id = 1, Payload = "preserved-boundary" });
            await context.SaveChangesAsync(token);
            var historyBefore = (await context.Database.GetAppliedMigrationsAsync(token)).ToArray();
            var assignmentBefore = ((StorageResult.Ready)await beta.ResolveAsync(token)).Assignment;
            var before = context.Model;
            try
            {
                await using var forbidden = await alpha.CreateContextAsync(
                    (options, _) => BetaStorage.Create(options, beta), token);
                throw new InvalidOperationException("storage_boundary_not_rejected");
            }
            catch (StorageException) { RegistrationScenarios.Require(true); assertions++; }
            try
            {
                await using var forbidden = await alpha.CreateContextAsync(
                    (options, binding) => new ForeignContext(options, binding), token);
                throw new InvalidOperationException("storage_boundary_not_rejected");
            }
            catch (StorageException) { RegistrationScenarios.Require(true); assertions++; }
            Require(ReferenceEquals(before, context.Model));
            Require(await beta.ResolveAsync(token) is StorageResult.Ready);
            Require((await context.Records.SingleAsync(token)).Payload == "preserved-boundary");
            Require((await context.Database.GetAppliedMigrationsAsync(token)).SequenceEqual(historyBefore));
            var assignmentAfter = ((StorageResult.Ready)await beta.ResolveAsync(token)).Assignment;
            Require(assignmentBefore.Revision == assignmentAfter.Revision &&
                assignmentBefore.AllocationId == assignmentAfter.AllocationId);
            return assertions + 6;
        }
        var isolated = scenario == "isolated-upgrade";
        Require(await alpha.MigrateAsync(AlphaStorage.Create, scope,
            isolated ? "202610210001_Initial" : null, token) is StorageResult.Ready);
        Require(await beta.MigrateAsync(BetaStorage.Create, scope, cancellationToken: token) is StorageResult.Ready);
        assertions += 2;
        await using (var context = await alpha.CreateContextAsync(AlphaStorage.Create, token))
        {
            if (isolated)
            {
                var table = alpha.Schema is { } schema ? $"\"{schema}\".\"Records\"" : "\"Records\"";
                var insert = "INSERT INTO " + table + " (\"Id\", \"Payload\") VALUES ({0}, {1})";
                await context.Database.ExecuteSqlRawAsync(
                    insert, [1, "consumer-alpha"], token);
                Require((await context.Database.GetAppliedMigrationsAsync(token)).Count() == 1);
            }
            else
            {
                context.Records.Add(new Modules.Alpha.Record { Id = 1, Payload = "consumer-alpha" });
                await context.SaveChangesAsync(token);
                Require((await context.Records.SingleAsync(token)).Payload == "consumer-alpha");
            }
            assertions++;
        }
        await using (var context = await beta.CreateContextAsync(BetaStorage.Create, token))
        {
            context.Records.Add(new Modules.Beta.Record { Id = 1, Payload = "consumer-beta" });
            await context.SaveChangesAsync(token);
        }
        var betaBefore = ((StorageResult.Ready)await beta.ResolveAsync(token)).Assignment;
        if (isolated)
        {
            Require(await alpha.MigrateAsync(AlphaStorage.Create, scope, cancellationToken: token) is StorageResult.Ready);
            assertions++;
        }
        await using (var context = await alpha.CreateContextAsync(AlphaStorage.Create, token))
        {
            var record = await context.Records.SingleAsync(token);
            Require(record.Payload == "consumer-alpha" && record.Note is null);
            Require((await context.Database.GetAppliedMigrationsAsync(token)).Count() == 2);
            Require(context.Model.FindEntityType(typeof(Modules.Alpha.Record))!.GetSchema() == alpha.Schema);
            assertions += 3;
        }
        await using (var context = await beta.CreateContextAsync(BetaStorage.Create, token))
        {
            Require((await context.Records.SingleAsync(token)).Payload == "consumer-beta");
            Require((await context.Database.GetAppliedMigrationsAsync(token)).Count() == 1);
            Require(context.Model.FindEntityType(typeof(Modules.Beta.Record))!.FindProperty("Note") is null);
            var after = ((StorageResult.Ready)await beta.ResolveAsync(token)).Assignment;
            Require(after.AllocationId == betaBefore.AllocationId && after.Revision == betaBefore.Revision);
            assertions += 4;
        }
        return assertions;
    }

    private static ModuleDescription Module(string id)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            id,
            version = "1.0.0",
            displayName = "Köfte public module example",
            hostContract = "1.0.0",
            capabilities = Array.Empty<object>(),
            dependencies = Array.Empty<object>(),
            permissions = Array.Empty<object>(),
            storage = new { relational = true }
        });
        return ManifestValidator.Validate(json, new ManifestValidationContext("1.0.0")) is ManifestValidationResult.Accepted accepted
            ? accepted.Description : throw new InvalidOperationException("storage_manifest_invalid");
    }

    private static void Require(bool condition)
        => RegistrationScenarios.Require(condition);

    private sealed class ForeignContext(DbContextOptions options, ModuleStorageBinding binding) : AssignedDbContext(options, binding)
    {
        protected override void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<ForeignRecord>().ToTable("Records", "kofte_system").HasKey(record => record.Id);
    }
    private sealed class ForeignRecord { public int Id { get; set; } }
}
