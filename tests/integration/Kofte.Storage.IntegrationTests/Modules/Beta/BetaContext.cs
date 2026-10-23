using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kofte.Storage.IntegrationTests.Modules.Beta;

public sealed class BetaRecord
{
    public int Id { get; set; }
    public string Payload { get; set; } = "";
}

public abstract class BetaContext(DbContextOptions options, ModuleStorageBinding binding)
    : AssignedDbContext(options, binding)
{
    public DbSet<BetaRecord> Records => Set<BetaRecord>();
    protected override void ConfigureModel(ModelBuilder modelBuilder) =>
        BuildAssignedModel(modelBuilder, Storage.Schema, Storage.Provider);

    internal static void BuildAssignedModel(ModelBuilder builder, string? schema, string provider)
    {
        builder.HasAnnotation("ProductVersion", "10.0.13");
        if (provider == "postgresql")
        {
            builder.HasDefaultSchema(schema);
            builder.HasAnnotation("Npgsql:ValueGenerationStrategy",
                Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
        builder.Entity<BetaRecord>(entity =>
        {
            entity.ToTable("Records", schema);
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Id).ValueGeneratedNever()
                .HasColumnType(provider == "postgresql" ? "integer" : "INTEGER");
            entity.Property(record => record.Payload).IsRequired()
                .HasColumnType(provider == "postgresql" ? "text" : "TEXT");
        });
    }
}

public sealed class SqliteBetaContext(DbContextOptions options, ModuleStorageBinding binding) : BetaContext(options, binding);
public sealed class PostgresBetaContext(DbContextOptions options, ModuleStorageBinding binding) : BetaContext(options, binding);

public abstract class BetaInitial : AssignedMigration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateTable("Records", schema: AssignedSchema, columns: table => new
        {
            Id = table.Column<int>(type: AssignedProvider == "postgresql" ? "integer" : "INTEGER", nullable: false),
            Payload = table.Column<string>(type: AssignedProvider == "postgresql" ? "text" : "TEXT", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Records", record => record.Id));
    protected override void BuildTargetModel(ModelBuilder modelBuilder) =>
        BetaContext.BuildAssignedModel(modelBuilder, AssignedSchema, AssignedProvider);
}
[DbContext(typeof(SqliteBetaContext)), Migration("202610210001_Initial")]
public sealed class SqliteBetaInitial : BetaInitial;
[DbContext(typeof(PostgresBetaContext)), Migration("202610210001_Initial")]
public sealed class PostgresBetaInitial : BetaInitial;
public abstract class BetaSnapshot : AssignedModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) =>
        BetaContext.BuildAssignedModel(modelBuilder, AssignedSchema, AssignedProvider);
}
[DbContext(typeof(SqliteBetaContext))]
public sealed class SqliteBetaSnapshot : BetaSnapshot;
[DbContext(typeof(PostgresBetaContext))]
public sealed class PostgresBetaSnapshot : BetaSnapshot;
