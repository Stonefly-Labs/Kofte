using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using Kofte.Platform.Modules.Storage;

namespace Kofte.Infrastructure.Storage.Migrations;

public abstract class AssignedDbContext : DbContext
{
    public ModuleStorageBinding Storage => Boundary.Binding ??
        throw new StorageException(StorageCode.RequestInvalid);
    internal ModuleStorageModelBoundary Boundary { get; }
    protected ModuleStorageModelBoundary ModelBoundary => Boundary;
    internal bool IsMigrationContext { get; set; }
    internal DbTransaction? AppliedMigrationTransaction { get; set; }

    protected AssignedDbContext(DbContextOptions options, ModuleStorageBinding binding)
        : this(options, new ModuleStorageModelBoundary(binding)) { }

    protected AssignedDbContext(DbContextOptions options, ModuleStorageModelBoundary boundary)
        : base(options) => Boundary = boundary;

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (Boundary.Schema is { } schema) modelBuilder.HasDefaultSchema(schema);
        ConfigureModel(modelBuilder);
    }

    protected abstract void ConfigureModel(ModelBuilder modelBuilder);
}
