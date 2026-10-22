using Kofte.Infrastructure.Storage;
using Kofte.Infrastructure.Storage.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Kofte.Storage.Consumer.Modules.Beta;

public sealed class Record
{
    public int Id { get; set; }
    public string Payload { get; set; } = "";
}

public abstract class BetaStorage(DbContextOptions options, ModuleStorageBinding storage) : AssignedDbContext(options, storage)
{
    public DbSet<Record> Records => Set<Record>();
    public static BetaStorage Create(DbContextOptions options, ModuleStorageBinding binding) =>
        binding.Provider == "postgresql" ? new PostgresBetaStorage(options, binding) : new SqliteBetaStorage(options, binding);
    protected override void ConfigureModel(ModelBuilder builder) => Describe(builder, Storage.Schema, Storage.Provider);
    internal static void Describe(ModelBuilder builder, string? schema, string provider)
    {
        builder.HasAnnotation("ProductVersion", "10.0.13");
        if (provider == "postgresql")
        {
            builder.HasDefaultSchema(schema);
            builder.HasAnnotation("Npgsql:ValueGenerationStrategy",
                Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
        builder.Entity<Record>(entity =>
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
public sealed class SqliteBetaStorage(DbContextOptions options, ModuleStorageBinding storage) : BetaStorage(options, storage);
public sealed class PostgresBetaStorage(DbContextOptions options, ModuleStorageBinding storage) : BetaStorage(options, storage);
public abstract class Initial : AssignedMigration
{
    protected override void Up(MigrationBuilder builder) => builder.CreateTable("Records", schema: AssignedSchema, columns: table => new
    {
        Id = table.Column<int>(type: AssignedProvider == "postgresql" ? "integer" : "INTEGER", nullable: false),
        Payload = table.Column<string>(type: AssignedProvider == "postgresql" ? "text" : "TEXT", nullable: false)
    }, constraints: table => table.PrimaryKey("PK_Records", record => record.Id));
    protected override void BuildTargetModel(ModelBuilder builder) => BetaStorage.Describe(builder, AssignedSchema, AssignedProvider);
}
[DbContext(typeof(SqliteBetaStorage)), Migration("202610210001_Initial")]
public sealed class SqliteInitial : Initial;
[DbContext(typeof(PostgresBetaStorage)), Migration("202610210001_Initial")]
public sealed class PostgresInitial : Initial;
public abstract class Snapshot : AssignedModelSnapshot
{
    protected override void BuildModel(ModelBuilder builder) => BetaStorage.Describe(builder, AssignedSchema, AssignedProvider);
}
[DbContext(typeof(SqliteBetaStorage))]
public sealed class SqliteSnapshot : Snapshot;
[DbContext(typeof(PostgresBetaStorage))]
public sealed class PostgresSnapshot : Snapshot;
