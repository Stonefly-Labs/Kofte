using Microsoft.EntityFrameworkCore;

namespace Kofte.Infrastructure.Storage.Migrations;

public abstract class AssignedDbContext(DbContextOptions options, ModuleStorageBinding binding)
    : DbContext(options)
{
    public ModuleStorageBinding Storage { get; } = binding;
    internal bool IsMigrationContext { get; set; }

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (Storage.Schema is { } schema) modelBuilder.HasDefaultSchema(schema);
        ConfigureModel(modelBuilder);
    }

    protected abstract void ConfigureModel(ModelBuilder modelBuilder);
}
