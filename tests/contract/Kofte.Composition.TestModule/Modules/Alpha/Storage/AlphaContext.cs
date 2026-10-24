using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Kofte.Composition.TestModule.Modules.Alpha.Storage;

public sealed class AlphaRecord
{
    public int Id { get; set; }
    public string Payload { get; set; } = "";
    public string? Note { get; set; }
}

public abstract class AlphaContext : AssignedDbContext
{
    protected AlphaContext(DbContextOptions options, ModuleStorageModelBoundary boundary) : base(options, boundary) { }
    protected AlphaContext(DbContextOptions options, ModuleStorageBinding binding) : base(options, binding) { }
    protected abstract bool Upgraded { get; }
    public DbSet<AlphaRecord> Records => Set<AlphaRecord>();

    protected override void ConfigureModel(ModelBuilder builder) =>
        Build(builder, ModelBoundary.Schema, ModelBoundary.Provider, Upgraded);

    internal static void Build(ModelBuilder builder, string? schema, string provider, bool upgraded)
    {
        builder.HasAnnotation("ProductVersion", "10.0.13");
        if (provider == "postgresql")
        {
            builder.HasDefaultSchema(schema);
            builder.HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
        builder.Entity<AlphaRecord>(entity =>
        {
            entity.ToTable("Records", schema);
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Id).ValueGeneratedNever()
                .HasColumnType(provider == "postgresql" ? "integer" : "INTEGER");
            entity.Property(record => record.Payload).IsRequired()
                .HasColumnType(provider == "postgresql" ? "text" : "TEXT");
            if (upgraded)
                entity.Property(record => record.Note).HasColumnType(provider == "postgresql" ? "text" : "TEXT");
            else entity.Ignore(record => record.Note);
        });
    }
}

public sealed class SqliteAlphaInitialContext : AlphaContext
{
    public SqliteAlphaInitialContext(DbContextOptions options, ModuleStorageModelBoundary boundary) : base(options, boundary) { }
    public SqliteAlphaInitialContext(DbContextOptions options, ModuleStorageBinding binding) : base(options, binding) { }
    protected override bool Upgraded => false;
}

public sealed class PostgresAlphaInitialContext : AlphaContext
{
    public PostgresAlphaInitialContext(DbContextOptions options, ModuleStorageModelBoundary boundary) : base(options, boundary) { }
    public PostgresAlphaInitialContext(DbContextOptions options, ModuleStorageBinding binding) : base(options, binding) { }
    protected override bool Upgraded => false;
}

public sealed class SqliteAlphaContext : AlphaContext
{
    public SqliteAlphaContext(DbContextOptions options, ModuleStorageModelBoundary boundary) : base(options, boundary) { }
    public SqliteAlphaContext(DbContextOptions options, ModuleStorageBinding binding) : base(options, binding) { }
    protected override bool Upgraded => true;
}

public sealed class PostgresAlphaContext : AlphaContext
{
    public PostgresAlphaContext(DbContextOptions options, ModuleStorageModelBoundary boundary) : base(options, boundary) { }
    public PostgresAlphaContext(DbContextOptions options, ModuleStorageBinding binding) : base(options, binding) { }
    protected override bool Upgraded => true;
}

public abstract class AlphaInitialMigration : AssignedMigration
{
    protected override void Up(MigrationBuilder builder) =>
        builder.CreateTable("Records", schema: AssignedSchema, columns: table => new
        {
            Id = table.Column<int>(type: AssignedProvider == "postgresql" ? "integer" : "INTEGER", nullable: false),
            Payload = table.Column<string>(type: AssignedProvider == "postgresql" ? "text" : "TEXT", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Records", row => row.Id));

    protected override void BuildTargetModel(ModelBuilder builder) =>
        AlphaContext.Build(builder, AssignedSchema, AssignedProvider, upgraded: false);
}

public abstract class AlphaAddNoteMigration : AssignedMigration
{
    protected override void Up(MigrationBuilder builder) =>
        builder.AddColumn<string>("Note", "Records", schema: AssignedSchema,
            type: AssignedProvider == "postgresql" ? "text" : "TEXT", nullable: true);

    protected override void BuildTargetModel(ModelBuilder builder) =>
        AlphaContext.Build(builder, AssignedSchema, AssignedProvider, upgraded: true);
}

[DbContext(typeof(SqliteAlphaInitialContext)), Migration(StorageRecipes.AlphaInitial)]
public sealed class SqliteAlphaReleaseOneInitial : AlphaInitialMigration;
[DbContext(typeof(PostgresAlphaInitialContext)), Migration(StorageRecipes.AlphaInitial)]
public sealed class PostgresAlphaReleaseOneInitial : AlphaInitialMigration;
[DbContext(typeof(SqliteAlphaContext)), Migration(StorageRecipes.AlphaInitial)]
public sealed class SqliteAlphaInitial : AlphaInitialMigration;
[DbContext(typeof(PostgresAlphaContext)), Migration(StorageRecipes.AlphaInitial)]
public sealed class PostgresAlphaInitial : AlphaInitialMigration;
[DbContext(typeof(SqliteAlphaContext)), Migration(StorageRecipes.AlphaCurrent)]
public sealed class SqliteAlphaAddNote : AlphaAddNoteMigration;
[DbContext(typeof(PostgresAlphaContext)), Migration(StorageRecipes.AlphaCurrent)]
public sealed class PostgresAlphaAddNote : AlphaAddNoteMigration;

public abstract class AlphaInitialSnapshot : AssignedModelSnapshot
{
    protected override void BuildModel(ModelBuilder builder) =>
        AlphaContext.Build(builder, AssignedSchema, AssignedProvider, upgraded: false);
}

public abstract class AlphaSnapshot : AssignedModelSnapshot
{
    protected override void BuildModel(ModelBuilder builder) =>
        AlphaContext.Build(builder, AssignedSchema, AssignedProvider, upgraded: true);
}

[DbContext(typeof(SqliteAlphaInitialContext))]
public sealed class SqliteAlphaInitialSnapshot : AlphaInitialSnapshot;
[DbContext(typeof(PostgresAlphaInitialContext))]
public sealed class PostgresAlphaInitialSnapshot : AlphaInitialSnapshot;
[DbContext(typeof(SqliteAlphaContext))]
public sealed class SqliteAlphaSnapshot : AlphaSnapshot;
[DbContext(typeof(PostgresAlphaContext))]
public sealed class PostgresAlphaSnapshot : AlphaSnapshot;
