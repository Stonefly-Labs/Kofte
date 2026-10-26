using System.Reflection;
using Kofte.Platform.Modules.Storage;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kofte.Infrastructure.Storage.Migrations;

public sealed class AssignedMigrationsAssembly : IMigrationsAssembly
{
    private readonly AssignedDbContext context;
    private ModelSnapshot? snapshot;
    private bool snapshotRead;
    public Assembly Assembly => context.GetType().Assembly;
    public IReadOnlyDictionary<string, TypeInfo> Migrations { get; }

    public AssignedMigrationsAssembly(ICurrentDbContext current)
    {
        context = current.Context as AssignedDbContext ??
            throw new StorageException(StorageCode.RequestInvalid);
        Migrations = Assembly.DefinedTypes
            .Where(type => !type.IsAbstract && typeof(Migration).IsAssignableFrom(type) &&
                type.GetCustomAttribute<DbContextAttribute>()?.ContextType == context.GetType())
            .Select(type => (Type: type, Id: type.GetCustomAttribute<MigrationAttribute>()?.Id ??
                throw new StorageException(StorageCode.RequestInvalid)))
            .OrderBy(entry => entry.Id, StringComparer.Ordinal)
            .ToDictionary(entry => entry.Id, entry => entry.Type, StringComparer.Ordinal);
    }

    public ModelSnapshot? ModelSnapshot
    {
        get
        {
            if (snapshotRead) return snapshot;
            var type = Assembly.DefinedTypes.SingleOrDefault(type => !type.IsAbstract &&
                typeof(ModelSnapshot).IsAssignableFrom(type) &&
                type.GetCustomAttribute<DbContextAttribute>()?.ContextType == context.GetType());
            if (type is not null)
            {
                snapshot = Activator.CreateInstance(type.AsType()) as AssignedModelSnapshot ??
                    throw new StorageException(StorageCode.RequestInvalid);
                ((AssignedModelSnapshot)snapshot).Bind(context.Boundary);
            }
            snapshotRead = true;
            return snapshot;
        }
    }

    public string? FindMigrationId(string nameOrId) => Migrations.Keys.FirstOrDefault(id =>
        id == nameOrId || id[(id.IndexOf('_') + 1)..] == nameOrId);

    public Migration CreateMigration(TypeInfo migrationClass, string activeProvider)
    {
        if (!Migrations.Values.Contains(migrationClass) ||
            Activator.CreateInstance(migrationClass.AsType()) is not AssignedMigration migration)
            throw new StorageException(StorageCode.RequestInvalid);
        migration.Bind(context.Boundary);
        migration.ActiveProvider = activeProvider;
        return migration;
    }
}
