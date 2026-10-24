using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Kofte.Composition.TestModule.Modules.Beta.Storage;

public sealed class BetaRecord
{
    public int Id { get; set; }
    public string Payload { get; set; } = "";
    public string? Label { get; set; }
}

public abstract class BetaContext : AssignedDbContext
{
    protected BetaContext(DbContextOptions options, ModuleStorageModelBoundary boundary) : base(options, boundary) { }
    protected BetaContext(DbContextOptions options, ModuleStorageBinding binding) : base(options, binding) { }
    protected abstract bool Upgraded { get; }
    public DbSet<BetaRecord> Records => Set<BetaRecord>();

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
        builder.Entity<BetaRecord>(entity =>
        {
            entity.ToTable("Records", schema);
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Id).ValueGeneratedNever()
                .HasColumnType(provider == "postgresql" ? "integer" : "INTEGER");
            entity.Property(record => record.Payload).IsRequired()
                .HasColumnType(provider == "postgresql" ? "text" : "TEXT");
            if (upgraded)
                entity.Property(record => record.Label).HasColumnType(provider == "postgresql" ? "text" : "TEXT");
            else entity.Ignore(record => record.Label);
        });
    }
}

public sealed class SqliteBetaInitialContext : BetaContext
{
    public SqliteBetaInitialContext(DbContextOptions options, ModuleStorageModelBoundary boundary) : base(options, boundary) { }
    public SqliteBetaInitialContext(DbContextOptions options, ModuleStorageBinding binding) : base(options, binding) { }
    protected override bool Upgraded => false;
}

public sealed class PostgresBetaInitialContext : BetaContext
{
    public PostgresBetaInitialContext(DbContextOptions options, ModuleStorageModelBoundary boundary) : base(options, boundary) { }
    public PostgresBetaInitialContext(DbContextOptions options, ModuleStorageBinding binding) : base(options, binding) { }
    protected override bool Upgraded => false;
}

public sealed class SqliteBetaContext : BetaContext
{
    public SqliteBetaContext(DbContextOptions options, ModuleStorageModelBoundary boundary) : base(options, boundary) { }
    public SqliteBetaContext(DbContextOptions options, ModuleStorageBinding binding) : base(options, binding) { }
    protected override bool Upgraded => true;
}

public sealed class PostgresBetaContext : BetaContext
{
    public PostgresBetaContext(DbContextOptions options, ModuleStorageModelBoundary boundary) : base(options, boundary) { }
    public PostgresBetaContext(DbContextOptions options, ModuleStorageBinding binding) : base(options, binding) { }
    protected override bool Upgraded => true;
}

public abstract class BetaInitialMigration : AssignedMigration
{
    protected override void Up(MigrationBuilder builder) =>
        builder.CreateTable("Records", schema: AssignedSchema, columns: table => new
        {
            Id = table.Column<int>(type: AssignedProvider == "postgresql" ? "integer" : "INTEGER", nullable: false),
            Payload = table.Column<string>(type: AssignedProvider == "postgresql" ? "text" : "TEXT", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Records", row => row.Id));

    protected override void BuildTargetModel(ModelBuilder builder) =>
        BetaContext.Build(builder, AssignedSchema, AssignedProvider, upgraded: false);
}

public abstract class BetaAddLabelMigration : AssignedMigration
{
    protected override void Up(MigrationBuilder builder) =>
        builder.AddColumn<string>("Label", "Records", schema: AssignedSchema,
            type: AssignedProvider == "postgresql" ? "text" : "TEXT", nullable: true);

    protected override void BuildTargetModel(ModelBuilder builder) =>
        BetaContext.Build(builder, AssignedSchema, AssignedProvider, upgraded: true);
}

[DbContext(typeof(SqliteBetaInitialContext)), Migration(StorageRecipes.BetaInitial)]
public sealed class SqliteBetaReleaseOneInitial : BetaInitialMigration;
[DbContext(typeof(PostgresBetaInitialContext)), Migration(StorageRecipes.BetaInitial)]
public sealed class PostgresBetaReleaseOneInitial : BetaInitialMigration;
[DbContext(typeof(SqliteBetaContext)), Migration(StorageRecipes.BetaInitial)]
public sealed class SqliteBetaInitial : BetaInitialMigration;
[DbContext(typeof(PostgresBetaContext)), Migration(StorageRecipes.BetaInitial)]
public sealed class PostgresBetaInitial : BetaInitialMigration;
[DbContext(typeof(SqliteBetaContext)), Migration(StorageRecipes.BetaCurrent)]
public sealed class SqliteBetaAddLabel : BetaAddLabelMigration;
[DbContext(typeof(PostgresBetaContext)), Migration(StorageRecipes.BetaCurrent)]
public sealed class PostgresBetaAddLabel : BetaAddLabelMigration;

public abstract class BetaInitialSnapshot : AssignedModelSnapshot
{
    protected override void BuildModel(ModelBuilder builder) =>
        BetaContext.Build(builder, AssignedSchema, AssignedProvider, upgraded: false);
}

public abstract class BetaSnapshot : AssignedModelSnapshot
{
    protected override void BuildModel(ModelBuilder builder) =>
        BetaContext.Build(builder, AssignedSchema, AssignedProvider, upgraded: true);
}

[DbContext(typeof(SqliteBetaInitialContext))]
public sealed class SqliteBetaInitialSnapshot : BetaInitialSnapshot;
[DbContext(typeof(PostgresBetaInitialContext))]
public sealed class PostgresBetaInitialSnapshot : BetaInitialSnapshot;
[DbContext(typeof(SqliteBetaContext))]
public sealed class SqliteBetaSnapshot : BetaSnapshot;
[DbContext(typeof(PostgresBetaContext))]
public sealed class PostgresBetaSnapshot : BetaSnapshot;
