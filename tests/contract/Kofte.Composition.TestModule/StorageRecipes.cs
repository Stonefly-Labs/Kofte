using System.Text.Json;
using Kofte.Composition.TestModule.Modules.Alpha.Storage;
using Kofte.Composition.TestModule.Modules.Beta.Storage;
using Kofte.Infrastructure.Composition;
using Kofte.Infrastructure.Storage;
using Kofte.Platform.Modules.Manifest;
using Microsoft.EntityFrameworkCore;

namespace Kofte.Composition.TestModule;

public enum StorageRelease { Initial, Current }

public sealed record AlphaSentinel(int Id, string Payload, string? Note);
public sealed record BetaSentinel(int Id, string Payload, string? Label);

public static class StorageRecipes
{
    public const string AlphaOwner = "org.example.alpha";
    public const string BetaOwner = "org.example.beta";
    public const string NoStorageOwner = "org.example.no-storage";
    public const string AlphaInitial = "20261024000100_AlphaInitial";
    public const string AlphaCurrent = "20261024000200_AlphaAddNote";
    public const string BetaInitial = "20261024000100_BetaInitial";
    public const string BetaCurrent = "20261024000200_BetaAddLabel";

    public static IReadOnlyList<ModuleDescription> Descriptions { get; } = Array.AsReadOnly(new[]
    {
        Description(AlphaOwner, true), Description(BetaOwner, true, AlphaOwner),
        Description(NoStorageOwner, false)
    });

    public static IReadOnlyList<ModuleStorageRecipe> ForRelease(StorageRelease release = StorageRelease.Current,
        bool aliases = false)
    {
        ValidateRelease(release);
        var alpha = release == StorageRelease.Initial ? AlphaInitial : AlphaCurrent;
        var beta = release == StorageRelease.Initial ? BetaInitial : BetaCurrent;
        if (aliases)
        {
            alpha = alpha[(alpha.IndexOf('_') + 1)..];
            beta = beta[(beta.IndexOf('_') + 1)..];
        }
        return release == StorageRelease.Initial
            ? Array.AsReadOnly(new[]
            {
                ModuleStorageRecipe.Create<SqliteAlphaInitialContext>(Descriptions[0].Id, "sqlite", alpha,
                    (options, boundary) => new(options, boundary)),
                ModuleStorageRecipe.Create<PostgresAlphaInitialContext>(Descriptions[0].Id, "postgresql", alpha,
                    (options, boundary) => new(options, boundary)),
                ModuleStorageRecipe.Create<SqliteBetaInitialContext>(Descriptions[1].Id, "sqlite", beta,
                    (options, boundary) => new(options, boundary)),
                ModuleStorageRecipe.Create<PostgresBetaInitialContext>(Descriptions[1].Id, "postgresql", beta,
                    (options, boundary) => new(options, boundary))
            })
            : Array.AsReadOnly(new[]
            {
                ModuleStorageRecipe.Create<SqliteAlphaContext>(Descriptions[0].Id, "sqlite", alpha,
                    (options, boundary) => new(options, boundary)),
                ModuleStorageRecipe.Create<PostgresAlphaContext>(Descriptions[0].Id, "postgresql", alpha,
                    (options, boundary) => new(options, boundary)),
                ModuleStorageRecipe.Create<SqliteBetaContext>(Descriptions[1].Id, "sqlite", beta,
                    (options, boundary) => new(options, boundary)),
                ModuleStorageRecipe.Create<PostgresBetaContext>(Descriptions[1].Id, "postgresql", beta,
                    (options, boundary) => new(options, boundary))
            });
    }

    public static async Task WriteAlphaAsync(ModuleStorageBinding binding, AlphaSentinel value,
        StorageRelease release = StorageRelease.Current, CancellationToken token = default)
    {
        if (release == StorageRelease.Initial && value.Note is not null)
            throw new ArgumentException("composition_storage_release_invalid", nameof(value));
        await using var context = await CreateAlphaContextAsync(binding, release, token);
        context.Records.Add(new() { Id = value.Id, Payload = value.Payload, Note = value.Note });
        await context.SaveChangesAsync(token);
    }

    public static async Task WriteBetaAsync(ModuleStorageBinding binding, BetaSentinel value,
        StorageRelease release = StorageRelease.Current, CancellationToken token = default)
    {
        if (release == StorageRelease.Initial && value.Label is not null)
            throw new ArgumentException("composition_storage_release_invalid", nameof(value));
        await using var context = await CreateBetaContextAsync(binding, release, token);
        context.Records.Add(new() { Id = value.Id, Payload = value.Payload, Label = value.Label });
        await context.SaveChangesAsync(token);
    }

    public static async Task<IReadOnlyList<AlphaSentinel>> ReadAlphaAsync(ModuleStorageBinding binding,
        StorageRelease release = StorageRelease.Current, CancellationToken token = default)
    {
        await using var context = await CreateAlphaContextAsync(binding, release, token);
        var rows = await context.Records.AsNoTracking().OrderBy(row => row.Id).ToArrayAsync(token);
        return Array.AsReadOnly(rows.Select(row => new AlphaSentinel(row.Id, row.Payload, row.Note)).ToArray());
    }

    public static async Task<IReadOnlyList<BetaSentinel>> ReadBetaAsync(ModuleStorageBinding binding,
        StorageRelease release = StorageRelease.Current, CancellationToken token = default)
    {
        await using var context = await CreateBetaContextAsync(binding, release, token);
        var rows = await context.Records.AsNoTracking().OrderBy(row => row.Id).ToArrayAsync(token);
        return Array.AsReadOnly(rows.Select(row => new BetaSentinel(row.Id, row.Payload, row.Label)).ToArray());
    }

    public static Task<AlphaContext> CreateAlphaContextAsync(ModuleStorageBinding binding,
        StorageRelease release = StorageRelease.Current, CancellationToken token = default)
    {
        ValidateBinding(binding, AlphaOwner, release);
        return binding.CreateContextAsync(AlphaFactory(release), token);
    }

    public static Func<DbContextOptions, ModuleStorageBinding, AlphaContext> AlphaFactory(
        StorageRelease release = StorageRelease.Current)
    {
        ValidateRelease(release);
        return (options, assigned) =>
        {
            ValidateBinding(assigned, AlphaOwner, release);
            return (assigned.Provider, release) switch
            {
                ("sqlite", StorageRelease.Initial) => new SqliteAlphaInitialContext(options, assigned),
                ("postgresql", StorageRelease.Initial) => new PostgresAlphaInitialContext(options, assigned),
                ("sqlite", StorageRelease.Current) => new SqliteAlphaContext(options, assigned),
                ("postgresql", StorageRelease.Current) => new PostgresAlphaContext(options, assigned),
                _ => throw new ArgumentException("composition_storage_provider_invalid", nameof(assigned))
            };
        };
    }

    public static Task<BetaContext> CreateBetaContextAsync(ModuleStorageBinding binding,
        StorageRelease release = StorageRelease.Current, CancellationToken token = default)
    {
        ValidateBinding(binding, BetaOwner, release);
        return binding.CreateContextAsync(BetaFactory(release), token);
    }

    public static Func<DbContextOptions, ModuleStorageBinding, BetaContext> BetaFactory(
        StorageRelease release = StorageRelease.Current)
    {
        ValidateRelease(release);
        return (options, assigned) =>
        {
            ValidateBinding(assigned, BetaOwner, release);
            return (assigned.Provider, release) switch
            {
                ("sqlite", StorageRelease.Initial) => new SqliteBetaInitialContext(options, assigned),
                ("postgresql", StorageRelease.Initial) => new PostgresBetaInitialContext(options, assigned),
                ("sqlite", StorageRelease.Current) => new SqliteBetaContext(options, assigned),
                ("postgresql", StorageRelease.Current) => new PostgresBetaContext(options, assigned),
                _ => throw new ArgumentException("composition_storage_provider_invalid", nameof(assigned))
            };
        };
    }

    private static void ValidateBinding(ModuleStorageBinding binding, string owner, StorageRelease release)
    {
        ArgumentNullException.ThrowIfNull(binding);
        ValidateRelease(release);
        if (binding.Owner.Value != owner)
            throw new ArgumentException("composition_storage_owner_invalid", nameof(binding));
    }

    private static void ValidateRelease(StorageRelease release)
    {
        if (release is not (StorageRelease.Initial or StorageRelease.Current))
            throw new ArgumentOutOfRangeException(nameof(release), "composition_storage_release_invalid");
    }

    private static ModuleDescription Description(string owner, bool relational, string? prerequisite = null)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = "1",
            id = owner,
            version = "1.0.0",
            displayName = "Köfte relational author fixture",
            hostContract = "1.0.0",
            capabilities = Array.Empty<object>(),
            dependencies = prerequisite is null ? [] :
                new[] { new { id = prerequisite, versionRange = ">=1.0.0 <2.0.0" } },
            permissions = Array.Empty<object>(),
            storage = new { relational }
        });
        return ManifestValidator.Validate(bytes, new("1.0.0")) is ManifestValidationResult.Accepted accepted
            ? accepted.Description : throw new InvalidOperationException("composition_storage_manifest_invalid");
    }
}
