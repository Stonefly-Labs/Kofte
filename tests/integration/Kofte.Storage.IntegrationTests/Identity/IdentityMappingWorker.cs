using System.Globalization;
using System.Text.Json;
using Kofte.Infrastructure.Identity;
using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Postgres;
using Kofte.Infrastructure.Storage.Registry;
using Kofte.Platform.Modules.Storage;
using Kofte.TestSupport;
using Npgsql;

namespace Kofte.Storage.IntegrationTests.Identity;

internal static class IdentityMappingWorker
{
    internal static async Task<int> RunAsync(string[] args)
    {
        try
        {
            if (args is not ["--identity-mapping-worker", var issuer, var subject, var display])
                throw new InvalidOperationException("identity_mapping_worker_arguments");
            using var budget = new OperationBudget(OperationBudget.Register, CancellationToken.None);
            var installation = StorageTestEnvironment.Provider == "sqlite"
                ? StorageInstallation.Sqlite(Environment.GetEnvironmentVariable("KOFTE_DATA_ROOT") ??
                    throw new InvalidOperationException("identity_mapping_worker_root"))
                : StorageInstallation.PostgreSql(Environment.GetEnvironmentVariable("KOFTE_POSTGRES_CONNECTION") ??
                    throw new InvalidOperationException("identity_mapping_worker_connection"));
            using var fileLease = installation.Provider == "sqlite" ? StoreLease.Acquire(installation.Root!) : null;
            await using var hostLease = installation.Provider == "postgresql"
                ? Assert.IsType<NpgsqlConnection>(await installation.OpenPlatformAsync(budget)) : null;
            if (hostLease is not null)
            {
                await using var command = new NpgsqlCommand(
                    "SELECT pg_catalog.pg_try_advisory_lock_shared($1)", hostLease);
                command.Parameters.AddWithValue(PostgresIdentity.LeaseKey);
                Assert.True(await command.ExecuteScalarAsync(budget.Token) is true);
            }
            var owner = await StorageReconciliation.ReadStartupAsync(installation, budget) ??
                throw new StorageException(StorageCode.Corrupt);
            await using var database = await DeadlineStore.OpenAsync(installation, budget);
            var metadata = await IdentityStore.ReadMetadataAsync(database);
            await IdentityPreparation.VerifyAsync(installation, owner.Id, metadata.Mode, metadata.TrustDigest, budget);
            var store = new IdentityStore(installation, owner.Id, metadata.Mode, metadata.TrustDigest, fileLease, hostLease);
            var user = await store.MutateAsync(budget, change => IdentityUserStore.ResolveAsync(change,
                issuer, subject, display, new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
            var designation = Assert.Single(await database.QueryAsync($"""
                SELECT initial_user_id,state,generation,consumed_generation,consumed_at_utc
                FROM {database.Prefix}__kofte_bootstrap WHERE singleton_key=1
                """));
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                eventCode = "identity_mapping_worker_ready",
                user.UserId,
                user.Mode,
                user.DisplayName,
                user.CreatedAt,
                initialUserId = Convert.ToString(designation[0], CultureInfo.InvariantCulture),
                state = designation[1],
                generation = designation[2],
                consumedGeneration = designation[3],
                consumedAt = designation[4] is DateTime time
                    ? time.ToString("O", CultureInfo.InvariantCulture) : designation[4]
            }));
            return 0;
        }
        catch (Exception)
        {
            Console.Error.WriteLine("""{"eventCode":"identity_mapping_worker_failed"}""");
            return 1;
        }
    }
}
