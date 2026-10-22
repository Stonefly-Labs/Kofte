using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kofte.Storage.IntegrationTests.Modules.Alpha;

public sealed class AlphaRecord
{
    public int Id { get; set; }
    public string Payload { get; set; } = "";
    public string? Note { get; set; }
}

public abstract class AlphaContext(DbContextOptions options, ModuleStorageBinding binding)
    : AssignedDbContext(options, binding)
{
    public DbSet<AlphaRecord> Records => Set<AlphaRecord>();
    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        BuildAssignedModel(modelBuilder, Storage.Schema, Storage.Provider, upgraded: true);

    internal static void BuildAssignedModel(ModelBuilder builder, string? schema, string provider, bool upgraded)
    {
        builder.HasAnnotation("ProductVersion", "10.0.13");
        if (provider == "postgresql")
        {
            builder.HasDefaultSchema(schema);
            builder.HasAnnotation("Npgsql:ValueGenerationStrategy",
                Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
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

public sealed class SqliteAlphaContext(DbContextOptions options, ModuleStorageBinding binding) : AlphaContext(options, binding);
public sealed class PostgresAlphaContext(DbContextOptions options, ModuleStorageBinding binding) : AlphaContext(options, binding);

public abstract class AlphaInitial : AssignedMigration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateTable("Records", schema: AssignedSchema, columns: table => new
        {
            Id = table.Column<int>(type: AssignedProvider == "postgresql" ? "integer" : "INTEGER", nullable: false),
            Payload = table.Column<string>(type: AssignedProvider == "postgresql" ? "text" : "TEXT", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Records", record => record.Id));

    protected override void BuildTargetModel(ModelBuilder modelBuilder) =>
        AlphaContext.BuildAssignedModel(modelBuilder, AssignedSchema, AssignedProvider, upgraded: false);
}

public abstract class AlphaUpgrade : AssignedMigration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>("Note", "Records", schema: AssignedSchema,
            type: AssignedProvider == "postgresql" ? "text" : "TEXT", nullable: true);
    protected override void BuildTargetModel(ModelBuilder modelBuilder) =>
        AlphaContext.BuildAssignedModel(modelBuilder, AssignedSchema, AssignedProvider, upgraded: true);
}

[DbContext(typeof(SqliteAlphaContext)), Migration("202610210001_Initial")]
public sealed class SqliteAlphaInitial : AlphaInitial;
[DbContext(typeof(SqliteAlphaContext)), Migration("202610210002_AddNote")]
public sealed class SqliteAlphaUpgrade : AlphaUpgrade;
[DbContext(typeof(PostgresAlphaContext)), Migration("202610210001_Initial")]
public sealed class PostgresAlphaInitial : AlphaInitial;
[DbContext(typeof(PostgresAlphaContext)), Migration("202610210002_AddNote")]
public sealed class PostgresAlphaUpgrade : AlphaUpgrade;

public abstract class AlphaSnapshot : AssignedModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) =>
        AlphaContext.BuildAssignedModel(modelBuilder, AssignedSchema, AssignedProvider, upgraded: true);
}
[DbContext(typeof(SqliteAlphaContext))]
public sealed class SqliteAlphaSnapshot : AlphaSnapshot;
[DbContext(typeof(PostgresAlphaContext))]
public sealed class PostgresAlphaSnapshot : AlphaSnapshot;
